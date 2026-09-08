using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using RevitMCP.Plugin.Handlers;

namespace RevitMCP.Plugin
{
    public class ModelingCommandHandler : IExternalEventHandler
    {
        public ConcurrentQueue<McpRequest> RequestQueue { get; } = new ConcurrentQueue<McpRequest>();
        public ConcurrentDictionary<string, TaskCompletionSource<object>> ResponseMap { get; } = new ConcurrentDictionary<string, TaskCompletionSource<object>>();

        public void Execute(UIApplication app)
        {
            Logger.Log("Execute entered.");
            // Drain the queue to prevent hanging connections
            while (RequestQueue.TryDequeue(out var request))
            {
                Logger.Log($"Processing request: {request.Command} ({request.Id})");
                object result = null;
                TaskCompletionSource<object> tcs = null;
                ResponseMap.TryGetValue(request.Id, out tcs);

                try
                {
                    if (app.ActiveUIDocument == null || app.ActiveUIDocument.Document == null)
                    {
                        Logger.Log("Error: No active document.");
                        result = new { error = "No active document found in Revit. Please open a project." };
                    }
                    else
                    {
                        var doc = app.ActiveUIDocument.Document;

                        if (request.Command == "create_stairs")
                        {
                            // Stairs creation manages its own transaction group
                            Logger.Log("Creating Stairs (Self-Contained Transaction)...");
                            result = CirculationHandler.CreateStairs(doc, request.Args);
                        }
                        else if (request.Command == "threedelab_batch_wall_join")
                        {
                            Logger.Log("Running 3DELAB Batch Wall Join (Self-Contained Transaction)...");
                            result = ThreeDelabHandler.BatchWallJoin(doc, request.Args);
                        }
                        else if (request.Command == "threedelab_export_views")
                        {
                            Logger.Log("Running 3DELAB Export Views (No Transaction)...");
                            result = ThreeDelabHandler.ExportViews(doc, request.Args);
                        }
                        else
                        {
                            Logger.Log("Starting transaction...");
                            using (Transaction t = new Transaction(doc, "MCP: " + request.Command))
                            {
                                // Setup Failure Handling to swallow warnings
                                FailureHandlingOptions options = t.GetFailureHandlingOptions();
                                options.SetFailuresPreprocessor(new WarningSwallower());
                                options.SetClearAfterRollback(true);
                                t.SetFailureHandlingOptions(options);

                                if (t.Start() == TransactionStatus.Started)
                                {
                                    switch (request.Command)
                                    {
                                        // Architectural
                                        case "create_level":
                                            result = ArchitecturalHandler.CreateLevel(doc, request.Args);
                                            break;
                                        case "create_grid":
                                            result = ArchitecturalHandler.CreateGrid(doc, request.Args);
                                            break;
                                        case "create_wall":
                                            result = ArchitecturalHandler.CreateWall(doc, request.Args);
                                            break;
                                        case "create_arc_wall":
                                            result = ArchitecturalHandler.CreateArcWall(doc, request.Args);
                                            break;
                                        case "create_ellipse_wall":
                                            result = ArchitecturalHandler.CreateEllipseWall(doc, request.Args);
                                            break;
                                        case "create_floor":
                                            result = ArchitecturalHandler.CreateFloor(doc, request.Args);
                                            break;
                                        case "create_window":
                                            result = ArchitecturalHandler.CreateWindow(doc, request.Args);
                                            break;
                                        case "create_door":
                                            result = ArchitecturalHandler.CreateDoor(doc, request.Args);
                                            break;
                                        case "create_opening":
                                            result = ArchitecturalHandler.CreateOpening(doc, request.Args);
                                            break;
                                        case "create_roof":
                                            result = ArchitecturalHandler.CreateRoof(doc, request.Args);
                                            break;

                                        // Structural
                                        case "create_column":
                                            result = StructuralHandler.CreateColumn(doc, request.Args);
                                            break;
                                        case "create_beam":
                                            result = StructuralHandler.CreateBeam(doc, request.Args);
                                            break;
                                        case "create_railing":
                                            result = GeneralHandler.CreateRailing(doc, request.Args);
                                            break;
                                        case "rotate_element":
                                            result = GeneralHandler.RotateElement(doc, request.Args);
                                            break;
                                        case "delete_element":
                                            result = GeneralHandler.DeleteElement(doc, request.Args);
                                            break;

                                        // MEP
                                        case "create_duct":
                                            result = MEPHandler.CreateDuct(doc, request.Args);
                                            break;
                                        case "create_pipe":
                                            result = MEPHandler.CreatePipe(doc, request.Args);
                                            break;

                                        // Rooms & Areas
                                        case "create_room":
                                            result = RoomAreaHandler.CreateRoom(doc, request.Args);
                                            break;
                                        case "create_room_separator":
                                            result = RoomAreaHandler.CreateRoomSeparator(doc, request.Args);
                                            break;
                                        case "tag_room":
                                            result = RoomAreaHandler.TagRoom(doc, request.Args);
                                            break;
                                        case "create_material":
                                            result = MaterialHandler.CreateMaterial(doc, request.Args);
                                            break;

                                        // Site & Massing
                                        case "create_toposolid":
                                            result = SiteHandler.CreateToposolid(doc, request.Args);
                                            break;
                                        case "create_mass":
                                            result = MassingHandler.CreateMass(doc, request.Args);
                                            break;

                                        // Queries
                                        case "get_levels":
                                            result = QueryHandler.GetLevels(doc);
                                            break;
                                        case "get_grids":
                                            result = QueryHandler.GetGrids(doc);
                                            break;
                                        case "get_mep_types":
                                            result = QueryHandler.GetMEPTypes(doc);
                                            break;

                                        // Location
                                        case "get_project_location":
                                            result = LocationHandler.GetProjectLocation(doc);
                                            break;
                                        case "get_project_base_point":
                                            result = LocationHandler.GetProjectBasePoint(doc);
                                            break;
                                        case "get_views":
                                            result = QueryHandler.GetViews(doc);
                                            break;
                                        case "get_wall_types":
                                            result = QueryHandler.GetWallTypes(doc);
                                            break;
                                        case "get_roof_types":
                                            result = QueryHandler.GetRoofTypes(doc);
                                            break;
                                        case "get_railing_types":
                                            result = QueryHandler.GetRailingTypes(doc);
                                            break;
                                        case "get_families":
                                            result = QueryHandler.GetFamilies(doc, request.Args);
                                            break;

                                        // Annotation
                                        case "create_text_note":
                                            result = AnnotationHandler.CreateTextNote(doc, request.Args);
                                            break;
                                        case "create_tag":
                                            result = AnnotationHandler.CreateTag(doc, request.Args);
                                            break;
                                        case "create_dimension":
                                            result = AnnotationHandler.CreateDimension(doc, request.Args);
                                            break;

                                        // Modification
                                        case "move_element":
                                            result = ModificationHandler.MoveElement(doc, request.Args);
                                            break;
                                        case "copy_element":
                                            result = ModificationHandler.CopyElement(doc, request.Args);
                                            break;
                                        case "mirror_element":
                                            result = ModificationHandler.MirrorElement(doc, request.Args);
                                            break;
                                        case "array_element":
                                            result = ModificationHandler.ArrayElement(doc, request.Args);
                                            break;
                                        case "align_elements":
                                            result = ModificationHandler.AlignElements(doc, request.Args);
                                            break;

                                        // Selection
                                        case "select_face":
                                            result = SelectionHandler.SelectFace(app.ActiveUIDocument, request.Args);
                                            break;
                                        case "select_element":
                                            result = SelectionHandler.SelectElement(app.ActiveUIDocument, request.Args);
                                            break;

                                        // Geometry Creation
                                        case "create_reference_plane":
                                            result = GeometryCreationHandler.CreateReferencePlane(doc, request.Args);
                                            break;
                                        case "create_extrusion":
                                            result = GeometryCreationHandler.CreateExtrusion(doc, request.Args);
                                            break;
                                        
                                        // Dynamo Integration
                                        case "create_dynamo_geometry":
                                            result = DynamoHandler.CreateComplexSolid(doc, request.Args);
                                            break;
                                        case "create_column_grid":
                                            result = DynamoHandler.CreateColumnGrid(doc, request.Args);
                                            break;
                                        case "dynamo_list_scripts":
                                            result = DynamoHandler.ListScripts(request.Args);
                                            break;
                                        case "dynamo_get_script_info":
                                            result = DynamoHandler.GetScriptInfo(request.Args);
                                            break;
                                        case "dynamo_modify_inputs":
                                            result = DynamoHandler.ModifyInputs(request.Args);
                                            break;
                                        case "dynamo_generate_graph":
                                            result = DynamoHandler.GenerateGraph(request.Args);
                                            break;
                                        case "dynamo_run_script":
                                            result = DynamoHandler.RunScript(doc, request.Args);
                                            break;
                                        case "dynamo_run_python":
                                            result = DynamoHandler.RunPython(doc, request.Args);
                                            break;

                                        // Project Information
                                        case "get_project_info":
                                            result = ProjectInfoHandler.GetProjectInfo(doc);
                                            break;
                                        case "set_project_info":
                                            result = ProjectInfoHandler.SetProjectInfo(doc, request.Args);
                                            break;

                                        // 3DELAB Tools Integration
                                        case "threedelab_get_info":
                                            result = ThreeDelabHandler.GetPluginInfo(doc);
                                            break;
                                        case "threedelab_get_timer_info":
                                            result = ThreeDelabHandler.GetTimerInfo(doc);
                                            break;
                                        case "threedelab_check_inplace_family":
                                            result = ThreeDelabHandler.CheckInPlaceFamily(doc, request.Args);
                                            break;
                                        case "threedelab_remove_paint":
                                            result = ThreeDelabHandler.RemovePaint(doc, request.Args);
                                            break;

                                        default:
                                            result = new { error = "Unknown command" };
                                            break;
                                    }

                                    // Only commit if we didn't return an error/null
                                    t.Commit();
                                }
                                else
                                {
                                     Logger.Log("Transaction failed to start.");
                                     result = new { error = "Could not start transaction." };
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Exception in Execute: {ex}");
                    result = new { error = ex.Message + "\n" + ex.StackTrace };
                }
                finally
                {
                   Logger.Log($"Setting result for {request.Id}");
                   if (tcs != null) tcs.TrySetResult(result);
                   ResponseMap.TryRemove(request.Id, out _);
                }
            }
            Logger.Log("Execute finished.");
        }

        public string GetName() => "MCP Modeling Handler";
    }
}
