using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RevitMCP.Plugin.Handlers
{
    public static class MaterialHandler
    {
        public static object CreateMaterial(Document doc, dynamic args)
        {
            string name = (string)args.name;
            
            // Check if exists
            Material existing = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .Cast<Material>()
                .FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existing != null) return new { id = existing.Id.ToString(), name = existing.Name, status = "Exists" };

            ElementId matId = Material.Create(doc, name);
            Material mat = (Material)doc.GetElement(matId);

            if (args.color != null)
            {
                Color color = new Color((byte)args.color.r, (byte)args.color.g, (byte)args.color.b);
                mat.Color = color;
                mat.SurfaceForegroundPatternColor = color; // Approximation for "Color"
            }

            return new { id = mat.Id.ToString(), name = mat.Name, status = "Created" };
        }
    }
}
