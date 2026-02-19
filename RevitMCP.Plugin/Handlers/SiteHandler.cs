using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;

namespace RevitMCP.Plugin.Handlers
{
    public static class SiteHandler
    {
        public static object CreateToposolid(Document doc, dynamic args)
        {
            JArray points = (JArray)args.points;
            ElementId levelId = new ElementId(long.Parse((string)args.level_id));
            
            // Create CurveLoop from points
            List<XYZ> xyzPoints = new List<XYZ>();
            foreach (var p in points)
            {
                xyzPoints.Add(new XYZ((double)p["x"], (double)p["y"], (double)p["z"]));
            }
            
            CurveLoop loop = new CurveLoop();
            for (int i = 0; i < xyzPoints.Count; i++)
            {
                XYZ p1 = xyzPoints[i];
                XYZ p2 = xyzPoints[(i + 1) % xyzPoints.Count];
                loop.Append(Line.CreateBound(p1, p2));
            }

            List<CurveLoop> profiles = new List<CurveLoop> { loop };

            // Get Toposolid Type
            ElementId typeId = new FilteredElementCollector(doc)
                .OfClass(typeof(ToposolidType))
                .FirstElementId();
            
            if (typeId == null) return new { error = "No ToposolidType found in project." };

            // Create Toposolid (Revit 2024+)
            Toposolid topo = Toposolid.Create(doc, profiles, typeId, levelId);

            return new { id = topo.Id.ToString(), name = topo.Name };
        }
    }
}
