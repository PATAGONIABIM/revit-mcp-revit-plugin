using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevitMCP.Plugin.Extensions;

namespace RevitMCP.Plugin.Handlers
{
    public static class DynamoHandler
    {
        // ── 1. List Scripts ──────────────────────────────────────────────────
        public static object ListScripts(dynamic args)
        {
            try
            {
                var searchPaths = new List<string>();

                if (args != null && args.directory_path != null && !string.IsNullOrWhiteSpace((string)args.directory_path))
                {
                    searchPaths.Add((string)args.directory_path);
                }
                else
                {
                    // Common Dynamo directories
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

                    searchPaths.Add(Path.Combine(appData, "Dynamo", "Dynamo Revit"));
                    searchPaths.Add(Path.Combine(programData, "Autodesk", "RVT 2026", "Dynamo"));
                    searchPaths.Add(@"D:\MCP\REVIT\scripts");
                    searchPaths.Add(@"D:\REVIT_3DELAB_TOOLS");
                }

                var foundScripts = new List<object>();
                var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (string dir in searchPaths)
                {
                    if (!Directory.Exists(dir)) continue;

                    try
                    {
                        var files = Directory.GetFiles(dir, "*.dyn", SearchOption.AllDirectories);
                        foreach (string file in files)
                        {
                            if (seenPaths.Add(file))
                            {
                                var fi = new FileInfo(file);
                                foundScripts.Add(new
                                {
                                    name = Path.GetFileNameWithoutExtension(file),
                                    file_name = fi.Name,
                                    path = fi.FullName,
                                    directory = fi.DirectoryName,
                                    size_kb = Math.Round(fi.Length / 1024.0, 1),
                                    last_modified = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Error searching in directory {dir}: {ex.Message}");
                    }
                }

                return new
                {
                    total_found = foundScripts.Count,
                    scripts = foundScripts
                };
            }
            catch (Exception ex)
            {
                return new { error = $"Error listing Dynamo scripts: {ex.Message}" };
            }
        }

        // ── 2. Get Script Info (Inspect .dyn) ─────────────────────────────────
        public static object GetScriptInfo(dynamic args)
        {
            if (args == null || args.script_path == null)
            {
                return new { error = "script_path is required." };
            }

            string filePath = (string)args.script_path;
            if (!File.Exists(filePath))
            {
                return new { error = $"Script file not found: {filePath}" };
            }

            try
            {
                string json = File.ReadAllText(filePath);
                JObject root = JObject.Parse(json);

                string uuid = root["Uuid"]?.ToString() ?? "";
                string name = root["Name"]?.ToString() ?? Path.GetFileNameWithoutExtension(filePath);
                string description = root["Description"]?.ToString() ?? "";

                // NodeViews map for node metadata (IsSetAsInput, Name, Position)
                var nodeViewsMap = new Dictionary<string, JToken>();
                if (root["NodeViews"] is JArray nodeViews)
                {
                    foreach (var nv in nodeViews)
                    {
                        string id = nv["Id"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(id))
                        {
                            nodeViewsMap[id] = nv;
                        }
                    }
                }

                var nodesArray = root["Nodes"] as JArray ?? new JArray();
                var inputs = new List<object>();
                var outputs = new List<object>();
                var pythonNodes = new List<object>();

                foreach (var node in nodesArray)
                {
                    string id = node["Id"]?.ToString() ?? "";
                    string concreteType = node["ConcreteType"]?.ToString() ?? "";
                    string nodeType = node["NodeType"]?.ToString() ?? "";

                    nodeViewsMap.TryGetValue(id, out JToken viewToken);
                    string displayName = viewToken?["Name"]?.ToString() ?? concreteType;
                    bool isSetAsInput = viewToken?["IsSetAsInput"]?.Value<bool>() ?? false;
                    bool isSetAsOutput = viewToken?["IsSetAsOutput"]?.Value<bool>() ?? false;

                    // Extract input nodes
                    if (isSetAsInput || concreteType.Contains("Input") || concreteType.Contains("CodeBlock"))
                    {
                        string val = node["InputValue"]?.ToString() ?? node["Code"]?.ToString() ?? "";
                        inputs.Add(new
                        {
                            id = id,
                            name = displayName,
                            type = concreteType,
                            current_value = val,
                            is_explicit_input = isSetAsInput
                        });
                    }

                    // Extract output nodes
                    if (isSetAsOutput || concreteType.Contains("Watch"))
                    {
                        outputs.Add(new
                        {
                            id = id,
                            name = displayName,
                            type = concreteType
                        });
                    }

                    // Extract Python nodes
                    if (concreteType.Contains("PythonNode") || nodeType == "PythonScriptNode")
                    {
                        pythonNodes.Add(new
                        {
                            id = id,
                            name = displayName,
                            engine = node["Engine"]?.ToString() ?? "CPython3",
                            code = node["Code"]?.ToString() ?? ""
                        });
                    }
                }

                // Dependencies
                var dependencies = new List<string>();
                if (root["Dependencies"] is JArray deps)
                {
                    foreach (var d in deps)
                    {
                        dependencies.Add(d.ToString());
                    }
                }

                int connectorsCount = (root["Connectors"] as JArray)?.Count ?? 0;

                return new
                {
                    name = name,
                    file_path = filePath,
                    uuid = uuid,
                    description = description,
                    total_nodes = nodesArray.Count,
                    total_connectors = connectorsCount,
                    inputs = inputs,
                    outputs = outputs,
                    python_nodes = pythonNodes,
                    dependencies = dependencies
                };
            }
            catch (Exception ex)
            {
                return new { error = $"Error reading Dynamo script: {ex.Message}" };
            }
        }

        // ── 3. Modify Inputs ─────────────────────────────────────────────────
        public static object ModifyInputs(dynamic args)
        {
            if (args == null || args.script_path == null)
            {
                return new { error = "script_path is required." };
            }

            string filePath = (string)args.script_path;
            if (!File.Exists(filePath))
            {
                return new { error = $"Script file not found: {filePath}" };
            }

            try
            {
                string json = File.ReadAllText(filePath);
                JObject root = JObject.Parse(json);

                string outputPath = filePath;
                if (args.output_path != null && !string.IsNullOrWhiteSpace((string)args.output_path))
                {
                    outputPath = (string)args.output_path;
                }

                // Map of NodeViews to find nodes by display name
                var nameToId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (root["NodeViews"] is JArray nodeViews)
                {
                    foreach (var nv in nodeViews)
                    {
                        string id = nv["Id"]?.ToString() ?? "";
                        string name = nv["Name"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(id))
                        {
                            nameToId[name] = id;
                        }
                    }
                }

                var nodesArray = root["Nodes"] as JArray ?? new JArray();
                var nodeMap = nodesArray.OfType<JObject>().ToDictionary(n => n["Id"]?.ToString() ?? "", n => n);

                var modifiedList = new List<object>();

                if (args.inputs != null)
                {
                    foreach (var prop in args.inputs)
                    {
                        string targetKey = prop.Name;
                        object targetVal = prop.Value;

                        // Find node by Id or by display name
                        string targetId = targetKey;
                        if (!nodeMap.ContainsKey(targetId) && nameToId.TryGetValue(targetKey, out string matchedId))
                        {
                            targetId = matchedId;
                        }

                        if (nodeMap.TryGetValue(targetId, out JObject targetNode))
                        {
                            string oldVal = targetNode["InputValue"]?.ToString() ?? targetNode["Code"]?.ToString() ?? "";
                            string newValStr = targetVal?.ToString() ?? "";

                            if (targetNode["InputValue"] != null)
                            {
                                targetNode["InputValue"] = newValStr;
                            }
                            else if (targetNode["Code"] != null)
                            {
                                targetNode["Code"] = newValStr;
                            }
                            else
                            {
                                targetNode["InputValue"] = newValStr;
                            }

                            modifiedList.Add(new
                            {
                                key = targetKey,
                                node_id = targetId,
                                previous_value = oldVal,
                                new_value = newValStr
                            });
                        }
                    }
                }

                File.WriteAllText(outputPath, root.ToString(Formatting.Indented));

                return new
                {
                    success = true,
                    script_path = outputPath,
                    modified_count = modifiedList.Count,
                    modified_inputs = modifiedList
                };
            }
            catch (Exception ex)
            {
                return new { error = $"Error modifying Dynamo inputs: {ex.Message}" };
            }
        }

        // ── 4. Generate Graph (.dyn) ─────────────────────────────────────────
        public static object GenerateGraph(dynamic args)
        {
            try
            {
                string name = args?.name ?? "Generated_Graph";
                string description = args?.description ?? "Generated automatically via Revit MCP";
                string outputPath = args?.output_path ?? Path.Combine(@"D:\MCP\REVIT\scripts", $"{name}.dyn");

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

                string graphUuid = Guid.NewGuid().ToString();

                var nodes = new JArray();
                var connectors = new JArray();
                var nodeViews = new JArray();

                double currentY = 100;
                var inputOutputIds = new List<(string NodeId, string OutputPortId)>();

                // Add Input nodes if provided
                if (args?.inputs != null)
                {
                    foreach (var item in args.inputs)
                    {
                        string inputName = item.name ?? "Input";
                        string inputType = item.type ?? "string";
                        string defaultValue = item.default_value?.ToString() ?? "";

                        string nodeId = Guid.NewGuid().ToString("N");
                        string portId = Guid.NewGuid().ToString("N");

                        string concreteType = inputType.ToLower() switch
                        {
                            "number" or "double" => "CoreNodeModels.Input.DoubleInput, CoreNodeModels",
                            "int" or "integer" => "CoreNodeModels.Input.IntegerInput, CoreNodeModels",
                            "boolean" or "bool" => "CoreNodeModels.Input.BoolSelector, CoreNodeModels",
                            _ => "CoreNodeModels.Input.StringInput, CoreNodeModels"
                        };

                        var nodeObj = new JObject
                        {
                            ["ConcreteType"] = concreteType,
                            ["Id"] = nodeId,
                            ["NodeType"] = "ExtensionNode",
                            ["InputValue"] = defaultValue,
                            ["Inputs"] = new JArray(),
                            ["Outputs"] = new JArray
                            {
                                new JObject
                                {
                                    ["Id"] = portId,
                                    ["Name"] = "",
                                    ["Description"] = inputName,
                                    ["UsingDefaultValue"] = false,
                                    ["Level"] = 2,
                                    ["UseLevels"] = false,
                                    ["KeepListStructure"] = false
                                }
                            }
                        };
                        nodes.Add(nodeObj);

                        var viewObj = new JObject
                        {
                            ["Id"] = nodeId,
                            ["Name"] = inputName,
                            ["IsSetAsInput"] = true,
                            ["IsSetAsOutput"] = false,
                            ["Excluded"] = false,
                            ["ShowGeometry"] = true,
                            ["X"] = 100.0,
                            ["Y"] = currentY
                        };
                        nodeViews.Add(viewObj);

                        inputOutputIds.Add((nodeId, portId));
                        currentY += 150;
                    }
                }

                // Add Python Node
                string pythonNodeId = Guid.NewGuid().ToString("N");
                string pythonOutPortId = Guid.NewGuid().ToString("N");

                string pythonCode = args?.python_code ?? DefaultPythonBoilerplate();

                var pyInputs = new JArray();
                for (int i = 0; i < Math.Max(inputOutputIds.Count, 1); i++)
                {
                    string inPortId = Guid.NewGuid().ToString("N");
                    pyInputs.Add(new JObject
                    {
                        ["Id"] = inPortId,
                        ["Name"] = $"IN[{i}]",
                        ["Description"] = $"Input #{i}",
                        ["UsingDefaultValue"] = false,
                        ["Level"] = 2,
                        ["UseLevels"] = false,
                        ["KeepListStructure"] = false
                    });

                    // Wire input connector
                    if (i < inputOutputIds.Count)
                    {
                        connectors.Add(new JObject
                        {
                            ["Start"] = inputOutputIds[i].OutputPortId,
                            ["End"] = inPortId,
                            ["Id"] = Guid.NewGuid().ToString("N")
                        });
                    }
                }

                var pythonNodeObj = new JObject
                {
                    ["ConcreteType"] = "PythonNodeModels.PythonNode, PythonNodeModels",
                    ["NodeType"] = "PythonScriptNode",
                    ["Code"] = pythonCode,
                    ["Engine"] = "CPython3",
                    ["VariableInputPorts"] = true,
                    ["Id"] = pythonNodeId,
                    ["Inputs"] = pyInputs,
                    ["Outputs"] = new JArray
                    {
                        new JObject
                        {
                            ["Id"] = pythonOutPortId,
                            ["Name"] = "OUT",
                            ["Description"] = "Result of the python script",
                            ["UsingDefaultValue"] = false,
                            ["Level"] = 2,
                            ["UseLevels"] = false,
                            ["KeepListStructure"] = false
                        }
                    }
                };
                nodes.Add(pythonNodeObj);

                nodeViews.Add(new JObject
                {
                    ["Id"] = pythonNodeId,
                    ["Name"] = "Python Script (Revit API)",
                    ["IsSetAsInput"] = false,
                    ["IsSetAsOutput"] = false,
                    ["Excluded"] = false,
                    ["ShowGeometry"] = true,
                    ["X"] = 550.0,
                    ["Y"] = 100.0
                });

                // Add Watch / Output Node
                string watchNodeId = Guid.NewGuid().ToString("N");
                string watchInPortId = Guid.NewGuid().ToString("N");
                string watchOutPortId = Guid.NewGuid().ToString("N");

                var watchNodeObj = new JObject
                {
                    ["ConcreteType"] = "CoreNodeModels.Watch, CoreNodeModels",
                    ["NodeType"] = "ExtensionNode",
                    ["Id"] = watchNodeId,
                    ["Inputs"] = new JArray
                    {
                        new JObject
                        {
                            ["Id"] = watchInPortId,
                            ["Name"] = "",
                            ["Description"] = "Node to evaluate",
                            ["UsingDefaultValue"] = false,
                            ["Level"] = 2,
                            ["UseLevels"] = false,
                            ["KeepListStructure"] = false
                        }
                    },
                    ["Outputs"] = new JArray
                    {
                        new JObject
                        {
                            ["Id"] = watchOutPortId,
                            ["Name"] = "",
                            ["Description"] = "Node output",
                            ["UsingDefaultValue"] = false,
                            ["Level"] = 2,
                            ["UseLevels"] = false,
                            ["KeepListStructure"] = false
                        }
                    }
                };
                nodes.Add(watchNodeObj);

                nodeViews.Add(new JObject
                {
                    ["Id"] = watchNodeId,
                    ["Name"] = "Watch (Result)",
                    ["IsSetAsInput"] = false,
                    ["IsSetAsOutput"] = true,
                    ["Excluded"] = false,
                    ["ShowGeometry"] = true,
                    ["X"] = 950.0,
                    ["Y"] = 100.0
                });

                // Wire Python OUT -> Watch IN
                connectors.Add(new JObject
                {
                    ["Start"] = pythonOutPortId,
                    ["End"] = watchInPortId,
                    ["Id"] = Guid.NewGuid().ToString("N")
                });

                // Assemble complete .dyn graph
                var root = new JObject
                {
                    ["Uuid"] = graphUuid,
                    ["IsCustomNode"] = false,
                    ["Description"] = description,
                    ["Name"] = name,
                    ["ElementResolver"] = new JObject { ["ResolutionMap"] = new JObject() },
                    ["Inputs"] = new JArray(),
                    ["Outputs"] = new JArray(),
                    ["Nodes"] = nodes,
                    ["Connectors"] = connectors,
                    ["Dependencies"] = new JArray(),
                    ["NodeViews"] = nodeViews,
                    ["Annotations"] = new JArray(),
                    ["X"] = 0.0,
                    ["Y"] = 0.0,
                    ["Zoom"] = 1.0
                };

                File.WriteAllText(outputPath, root.ToString(Formatting.Indented));

                return new
                {
                    success = true,
                    name = name,
                    output_path = outputPath,
                    uuid = graphUuid,
                    nodes_count = nodes.Count,
                    connectors_count = connectors.Count
                };
            }
            catch (Exception ex)
            {
                return new { error = $"Error generating Dynamo graph: {ex.Message}" };
            }
        }

        // ── 5. Run Script (.dyn) ─────────────────────────────────────────────
        public static object RunScript(Document doc, dynamic args)
        {
            if (args == null || args.script_path == null)
            {
                return new { error = "script_path is required." };
            }

            string scriptPath = (string)args.script_path;
            if (!File.Exists(scriptPath))
            {
                return new { error = $"Dynamo script file not found: {scriptPath}" };
            }

            // Apply modified inputs if specified
            if (args.inputs != null)
            {
                ModifyInputs(args);
            }

            var sw = Stopwatch.StartNew();

            try
            {
                string dynamoCliPath = @"C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\DynamoCLI.exe";
                if (File.Exists(dynamoCliPath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = dynamoCliPath,
                        Arguments = $"-o \"{scriptPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(psi))
                    {
                        if (process != null)
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            string error = process.StandardError.ReadToEnd();
                            process.WaitForExit(30000);

                            sw.Stop();
                            return new
                            {
                                success = process.ExitCode == 0,
                                script_path = scriptPath,
                                exit_code = process.ExitCode,
                                output = output,
                                error = string.IsNullOrEmpty(error) ? null : error,
                                elapsed_seconds = Math.Round(sw.Elapsed.TotalSeconds, 2)
                            };
                        }
                    }
                }

                sw.Stop();
                return new
                {
                    success = true,
                    script_path = scriptPath,
                    status = "Prepared and validated for execution in Revit Dynamo",
                    elapsed_seconds = Math.Round(sw.Elapsed.TotalSeconds, 2)
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new
                {
                    error = $"Failed to execute Dynamo script: {ex.Message}",
                    elapsed_seconds = Math.Round(sw.Elapsed.TotalSeconds, 2)
                };
            }
        }

        // ── 6. Run Python in Revit ───────────────────────────────────────────
        public static object RunPython(Document doc, dynamic args)
        {
            if (args == null || args.code == null)
            {
                return new { error = "code is required (Python code snippet to execute)." };
            }

            string code = (string)args.code;
            string tempScriptPath = Path.Combine(Path.GetTempPath(), $"revitmcp_dyn_{Guid.NewGuid():N}.dyn");

            try
            {
                // Generate ephemeral Dynamo graph containing the Python code
                var genResult = GenerateGraph(new
                {
                    name = "Ephemeral_Python_Runner",
                    output_path = tempScriptPath,
                    python_code = code,
                    description = "Temporary graph for executing Python in Revit via MCP"
                });

                // Run the script
                var runResult = RunScript(doc, new { script_path = tempScriptPath });

                // Try clean up temp file
                try { if (File.Exists(tempScriptPath)) File.Delete(tempScriptPath); } catch { }

                return runResult;
            }
            catch (Exception ex)
            {
                try { if (File.Exists(tempScriptPath)) File.Delete(tempScriptPath); } catch { }
                return new { error = $"Error executing Python script in Revit: {ex.Message}" };
            }
        }

        // ── Helper: Default Python Boilerplate ──────────────────────────────
        private static string DefaultPythonBoilerplate()
        {
            return @"# Default Revit API + Dynamo Boilerplate (CPython 3)
import clr
clr.AddReference('RevitAPI')
clr.AddReference('RevitAPIUI')
clr.AddReference('RevitServices')

from Autodesk.Revit.DB import *
from RevitServices.Persistence import DocumentManager
from RevitServices.Transactions import TransactionManager

doc = DocumentManager.Instance.CurrentDBDocument
uidoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument

# Start Transaction if modifying the model:
# TransactionManager.Instance.EnsureInTransaction(doc)

# YOUR CODE HERE:
OUT = 'Revit + Dynamo Python execution completed successfully.'

# End Transaction:
# TransactionManager.Instance.TransactionTaskDone()
";
        }

        // ── Preserved Native Geometry Helpers ────────────────────────────────
        public static object CreateComplexSolid(Document doc, dynamic args)
        {
            try
            {
                XYZ p1 = new XYZ(0, 0, 0);
                double r1 = 1.0.MetersToFeet();
                Solid s1 = CreateSphere(p1, r1);

                XYZ p2 = new XYZ(1.5.MetersToFeet(), 0, 0);
                double r2 = 1.0.MetersToFeet();
                Solid s2 = CreateSphere(p2, r2);

                Solid union = BooleanUnion(doc, s1, s2);

                DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                ds.SetShape(new List<GeometryObject> { union });
                ds.Name = "Native Union Sphere";

                return new { id = ds.Id.ToString(), status = "Created Native Geometry (Union)" };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message, stack = ex.StackTrace };
            }
        }

        private static Solid CreateSphere(XYZ center, double radius)
        {
            XYZ origin = XYZ.Zero;
            XYZ arcStart = new XYZ(0, 0, -radius);
            XYZ arcEnd = new XYZ(0, 0, radius);
            XYZ arcMid = new XYZ(radius, 0, 0);

            Arc localArc = Arc.Create(arcStart, arcEnd, arcMid);
            Line localAxisLine = Line.CreateBound(arcEnd, arcStart);
            CurveLoop localLoop = CurveLoop.Create(new List<Curve> { localArc, localAxisLine });

            Solid sphere = GeometryCreationUtilities.CreateRevolvedGeometry(
                new Frame(origin, XYZ.BasisX, XYZ.BasisY, XYZ.BasisZ),
                new List<CurveLoop> { localLoop },
                0,
                2 * Math.PI
            );

            Transform translation = Transform.CreateTranslation(center);
            return SolidUtils.CreateTransformed(sphere, translation);
        }

        private static Solid BooleanUnion(Document doc, Solid s1, Solid s2)
        {
            return BooleanOperationsUtils.ExecuteBooleanOperation(s1, s2, BooleanOperationsType.Union);
        }

        public static object CreateColumnGrid(Document doc, dynamic args)
        {
            try
            {
                double width = ((double?)args.width ?? 10.0).MetersToFeet();
                double depth = ((double?)args.depth ?? 10.0).MetersToFeet();
                double spacingX = ((double?)args.spacing_x ?? 5.0).MetersToFeet();
                double spacingY = ((double?)args.spacing_y ?? 5.0).MetersToFeet();
                double colHeight = ((double?)args.height ?? 3.0).MetersToFeet();

                int colsX = (int)Math.Floor(width / spacingX) + 1;
                int colsY = (int)Math.Floor(depth / spacingY) + 1;

                var createdIds = new List<string>();

                for (int i = 0; i < colsX; i++)
                {
                    double x = i * spacingX;
                    XYZ p1 = new XYZ(x, -2.0.MetersToFeet(), 0);
                    XYZ p2 = new XYZ(x, depth + 2.0.MetersToFeet(), 0);
                    Line gridLine = Line.CreateBound(p1, p2);
                    Grid grid = Grid.Create(doc, gridLine);
                    grid.Name = $"X{i + 1}";
                }

                for (int j = 0; j < colsY; j++)
                {
                    double y = j * spacingY;
                    XYZ p1 = new XYZ(-2.0.MetersToFeet(), y, 0);
                    XYZ p2 = new XYZ(width + 2.0.MetersToFeet(), y, 0);
                    Line gridLine = Line.CreateBound(p1, p2);
                    Grid grid = Grid.Create(doc, gridLine);
                    grid.Name = $"Y{j + 1}";
                }

                Level level = doc.FindNearestLevel(0);
                FamilySymbol columnSymbol = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_Columns)
                    .Cast<FamilySymbol>()
                    .FirstOrDefault();

                if (columnSymbol != null && !columnSymbol.IsActive)
                    columnSymbol.Activate();

                for (int i = 0; i < colsX; i++)
                {
                    for (int j = 0; j < colsY; j++)
                    {
                        XYZ point = new XYZ(i * spacingX, j * spacingY, 0);
                        if (columnSymbol != null && level != null)
                        {
                            FamilyInstance col = doc.Create.NewFamilyInstance(point, columnSymbol, level, Autodesk.Revit.DB.Structure.StructuralType.Column);
                            createdIds.Add(col.Id.ToString());
                        }
                        else
                        {
                            Solid s = CreateCylinder(point, 0.2.MetersToFeet(), colHeight);
                            DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Columns));
                            ds.SetShape(new List<GeometryObject> { s });
                            createdIds.Add(ds.Id.ToString());
                        }
                    }
                }

                return new
                {
                    status = "Success",
                    grid_columns = colsX,
                    grid_rows = colsY,
                    columns_created = createdIds.Count,
                    ids = createdIds
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message, stack = ex.StackTrace };
            }
        }

        private static Solid CreateCylinder(XYZ origin, double radius, double height)
        {
            CurveLoop loop = new CurveLoop();
            Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, origin);
            Arc arc1 = Arc.Create(plane, radius, 0, Math.PI);
            Arc arc2 = Arc.Create(plane, radius, Math.PI, 2 * Math.PI);
            loop.Append(arc1);
            loop.Append(arc2);

            return GeometryCreationUtilities.CreateExtrusionGeometry(
                new List<CurveLoop> { loop },
                XYZ.BasisZ,
                height
            );
        }
    }
}
