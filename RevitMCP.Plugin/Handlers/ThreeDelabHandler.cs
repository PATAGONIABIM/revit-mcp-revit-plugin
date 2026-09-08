using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Autodesk.Revit.DB;
using RevitMCP.Plugin.Extensions;
using ThreeDelabTools.Models;
using ThreeDelabTools.Services;

namespace RevitMCP.Plugin.Handlers
{
    public static class ThreeDelabHandler
    {
        public static object GetPluginInfo(Document doc)
        {
            string timerPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "3DELAB", "timer_data.json");

            var panels = new List<object>
            {
                new
                {
                    name = "Exportar",
                    description = "Herramientas de exportación de vistas e imágenes",
                    commands = new[]
                    {
                        new { name = "ExportImagesCommand", title = "Exportar Vistas", description = "Exporta vistas configuradas como imágenes (One-Click)" },
                        new { name = "ExportImagesConfigCommand", title = "Config Export", description = "Configuración de formato, resolución y vistas a exportar" },
                        new { name = "OpenExportFolderCommand", title = "Abrir Carpeta", description = "Abre la carpeta de imágenes exportadas" }
                    }
                },
                new
                {
                    name = "Material",
                    description = "Herramientas de inspección, copia y remoción de pintura en caras",
                    commands = new[]
                    {
                        new { name = "MaterialPickerCommand", title = "Copiar Material", description = "Selecciona una cara para copiar su material y aplicarlo a otra" },
                        new { name = "RemovePaintCommand", title = "Remover Pintura", description = "Remueve la pintura de caras seleccionadas" }
                    }
                },
                new
                {
                    name = "Tiempo",
                    description = "Control y temporizador de tiempos de proyecto y facturación",
                    commands = new[]
                    {
                        new { name = "SessionTimerCommand", title = "Temporizador", description = "Muestra el temporizador de sesión, tiempo acumulado y costo del proyecto" }
                    }
                },
                new
                {
                    name = "Utilidades",
                    description = "Herramientas de automatización geométrica y conversión",
                    commands = new[]
                    {
                        new { name = "InPlaceToFamilyCommand", title = "In-Place a Familia", description = "Convierte un modelo in-situ en un archivo de familia .rfa" },
                        new { name = "BatchWallJoinCommand", title = "Unir Muros", description = "Une la geometría de muros paralelos/adyacentes en batch usando hashing espacial" }
                    }
                }
            };

            return new
            {
                plugin_name = "3DELAB Tools",
                vendor = "3DELAB",
                location = @"D:\REVIT_3DELAB_TOOLS",
                assembly = @"C:\Users\chris\AppData\Roaming\Autodesk\Revit\Addins\2026\ThreeDelabTools\ThreeDelabTools.dll",
                panels = panels,
                timer_data_file = timerPath,
                timer_data_exists = File.Exists(timerPath)
            };
        }

        public static object GetTimerInfo(Document doc)
        {
            string timerPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "3DELAB", "timer_data.json");

            if (!File.Exists(timerPath))
            {
                return new
                {
                    message = "No se ha encontrado archivo de datos del temporizador (3DELAB timer_data.json).",
                    timer_path = timerPath
                };
            }

            try
            {
                string json = File.ReadAllText(timerPath);
                var data = JsonSerializer.Deserialize<TimerData>(json);

                if (data == null)
                {
                    return new { error = "No se pudo deserializar timer_data.json" };
                }

                string docPath = doc?.PathName ?? "";
                string docTitle = doc?.Title ?? "Untitled";

                // Look for matching project
                ProjectTimer project = null;
                if (!string.IsNullOrEmpty(docPath) && data.Projects.TryGetValue(docPath, out var projByPath))
                {
                    project = projByPath;
                }
                else
                {
                    project = data.Projects.Values.FirstOrDefault(p =>
                        p.ProjectName.Equals(docTitle, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrEmpty(docPath) && p.ProjectName.Equals(Path.GetFileNameWithoutExtension(docPath), StringComparison.OrdinalIgnoreCase)));
                }

                long totalSeconds = project?.TotalSeconds ?? 0;
                long hours = totalSeconds / 3600;
                long minutes = (totalSeconds % 3600) / 60;
                decimal cost = (totalSeconds / 3600m) * data.HourlyRate;

                return new
                {
                    project_found = project != null,
                    project_name = project?.ProjectName ?? docTitle,
                    total_seconds = totalSeconds,
                    formatted_time = $"{hours}h {minutes:D2}m",
                    hourly_rate = data.HourlyRate,
                    currency = data.Currency,
                    accumulated_cost = Math.Round(cost, 2),
                    total_sessions = project?.Sessions?.Count ?? 0,
                    sessions = project?.Sessions?.TakeLast(5).Select(s => new
                    {
                        start = s.Start.ToString("yyyy-MM-dd HH:mm"),
                        end = s.End.ToString("yyyy-MM-dd HH:mm"),
                        duration_minutes = Math.Round(s.DurationSeconds / 60.0, 1),
                        active_minutes = Math.Round(s.ActiveSeconds / 60.0, 1)
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                return new { error = $"Error al leer temporizador: {ex.Message}" };
            }
        }

        public static object BatchWallJoin(Document doc, dynamic args)
        {
            if (doc == null)
            {
                return new { error = "No active document." };
            }

            var wallTypeIds = new HashSet<ElementId>();

            if (args != null && args.wall_type_names != null)
            {
                var requestedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var n in args.wall_type_names)
                {
                    requestedNames.Add((string)n);
                }

                var matchedTypes = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .Where(wt => requestedNames.Contains(wt.Name))
                    .Select(wt => wt.Id);

                foreach (var id in matchedTypes)
                {
                    wallTypeIds.Add(id);
                }
            }
            else
            {
                // Process all wall types present in instances
                var allInstanceTypeIds = new FilteredElementCollector(doc)
                    .OfClass(typeof(Wall))
                    .WhereElementIsNotElementType()
                    .Cast<Wall>()
                    .Select(w => w.WallType.Id)
                    .Distinct();

                foreach (var id in allInstanceTypeIds)
                {
                    wallTypeIds.Add(id);
                }
            }

            if (wallTypeIds.Count == 0)
            {
                return new { error = "No se encontraron muros o tipos de muros para unir." };
            }

            // WallJoinService.JoinWalls creates and commits its own transaction internally
            WallJoinResult result = WallJoinService.JoinWalls(doc, wallTypeIds);

            return new
            {
                success = true,
                total_walls = result.TotalWalls,
                evaluated_pairs = result.TotalEvaluated,
                joined_pairs = result.Joined,
                already_joined_pairs = result.AlreadyJoined,
                skipped_pairs = result.Skipped,
                errors = result.Errors,
                elapsed_seconds = Math.Round(result.Elapsed.TotalSeconds, 2)
            };
        }

        public static object ExportViews(Document doc, dynamic args)
        {
            if (doc == null)
            {
                return new { error = "No active document." };
            }

            var settings = ImageExportService.LoadSettings(doc.PathName);

            if (args != null)
            {
                if (args.output_folder != null && !string.IsNullOrWhiteSpace((string)args.output_folder))
                {
                    settings.OutputFolder = (string)args.output_folder;
                }

                if (args.format != null && !string.IsNullOrWhiteSpace((string)args.format))
                {
                    settings.Format = (string)args.format;
                }

                if (args.pixel_size != null)
                {
                    settings.PixelSize = (int)args.pixel_size;
                }

                if (args.resolution != null)
                {
                    settings.Resolution = (int)args.resolution;
                }
            }

            if (string.IsNullOrEmpty(settings.OutputFolder))
            {
                string projectDir = Path.GetDirectoryName(doc.PathName) ?? "";
                if (string.IsNullOrEmpty(projectDir))
                {
                    projectDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RevitExports");
                }
                settings.OutputFolder = Path.Combine(projectDir, "ExportedViews");
            }

            var allExportable = ImageExportService.GetExportableViews(doc);
            var viewsToExport = new List<View>();

            if (args != null && args.view_names != null)
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var v in args.view_names)
                {
                    names.Add((string)v);
                }
                viewsToExport = allExportable.Where(v => names.Contains(v.Name)).ToList();
            }
            else
            {
                // Default: Active view if exportable, otherwise all exportable
                if (doc.ActiveView != null && !doc.ActiveView.IsTemplate && doc.ActiveView.CanBePrinted)
                {
                    viewsToExport.Add(doc.ActiveView);
                }
                else
                {
                    viewsToExport = allExportable;
                }
            }

            if (viewsToExport.Count == 0)
            {
                return new { error = "No se encontraron vistas exportables que cumplan los criterios." };
            }

            int count = ImageExportService.ExportViews(doc, settings, viewsToExport);

            return new
            {
                success = true,
                exported_count = count,
                output_folder = settings.OutputFolder,
                views = viewsToExport.Select(v => v.Name).ToList()
            };
        }

        public static object CheckInPlaceFamily(Document doc, dynamic args)
        {
            if (doc == null)
            {
                return new { error = "No active document." };
            }

            if (args == null || args.element_id == null)
            {
                return new { error = "element_id es requerido." };
            }

            Element element = doc.GetElementById((string)args.element_id.ToString());
            if (element == null)
            {
                return new { error = $"Elemento con ID {args.element_id} no encontrado." };
            }

            bool isInPlace = InPlaceConversionService.IsInPlaceFamily(element);
            string categoryName = element.Category?.Name ?? "Desconocida";
            string suggestedTemplate = "";

            if (element.Category != null)
            {
                suggestedTemplate = InPlaceConversionService.GetFamilyTemplatePath(element.Category.BuiltInCategory) ?? "";
            }

            return new
            {
                element_id = element.Id.ToString(),
                element_name = element.Name,
                is_in_place = isInPlace,
                category = categoryName,
                suggested_family_template = suggestedTemplate
            };
        }

        public static object RemovePaint(Document doc, dynamic args)
        {
            if (doc == null)
            {
                return new { error = "No active document." };
            }

            if (args == null || args.element_id == null)
            {
                return new { error = "element_id es requerido." };
            }

            Element element = doc.GetElementById((string)args.element_id.ToString());
            if (element == null)
            {
                return new { error = $"Elemento con ID {args.element_id} no encontrado." };
            }

            var opt = new Options
            {
                DetailLevel = ViewDetailLevel.Fine,
                ComputeReferences = true
            };

            var geoElem = element.get_Geometry(opt);
            int removedFaces = 0;

            if (geoElem != null)
            {
                foreach (var geoObj in geoElem)
                {
                    IEnumerable<Solid> solids = geoObj switch
                    {
                        Solid s when s.Volume > 0 => new[] { s },
                        GeometryInstance gi => gi.GetInstanceGeometry()?.OfType<Solid>().Where(s => s.Volume > 0) ?? Enumerable.Empty<Solid>(),
                        _ => Enumerable.Empty<Solid>()
                    };

                    foreach (var solid in solids)
                    {
                        foreach (Face face in solid.Faces)
                        {
                            try
                            {
                                if (doc.IsPainted(element.Id, face))
                                {
                                    doc.RemovePaint(element.Id, face);
                                    removedFaces++;
                                }
                            }
                            catch
                            {
                                // Skip individual face errors
                            }
                        }
                    }
                }
            }

            return new
            {
                success = true,
                element_id = element.Id.ToString(),
                removed_painted_faces = removedFaces
            };
        }
    }
}
