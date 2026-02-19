using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture; // For Railing
using Newtonsoft.Json.Linq;

namespace RevitMCP.Plugin.Handlers
{
    public static class GeneralHandler
    {
        public static object CreateRailing(Document doc, dynamic args)
        {
             JArray points = (JArray)args.points;
             List<XYZ> xyzPoints = new List<XYZ>();
             foreach (var p in points)
             {
                 xyzPoints.Add(new XYZ((double)p["x"], (double)p["y"], (double)p["z"]));
             }

             ElementId levelId = new ElementId(long.Parse((string)args.level_id));
             
             CurveLoop loop = new CurveLoop();
             // Assuming open or closed loop. For Railing.Create, loop must be continuous.
             for (int i = 0; i < xyzPoints.Count - 1; i++)
             {
                 XYZ p1 = xyzPoints[i];
                 XYZ p2 = xyzPoints[i+1];
                 loop.Append(Line.CreateBound(p1, p2));
             }
             
             // RailingType
             ElementId railingTypeId = null;
             if (args.type_name != null)
             {
                 railingTypeId = new FilteredElementCollector(doc)
                     .OfClass(typeof(RailingType))
                     .Cast<RailingType>()
                     .FirstOrDefault(e => e.Name == (string)args.type_name)?.Id;
             }
             
             if (railingTypeId == null)
             {
                 railingTypeId = new FilteredElementCollector(doc)
                     .OfClass(typeof(RailingType))
                     .FirstElementId();
             }

             if (railingTypeId == null) return new { error = "No RailingType found." };

             // Create Railing
             Railing railing = Railing.Create(doc, loop, railingTypeId, levelId);
             
             return new { id = railing.Id.ToString(), type = railing.Name }; 
        }

        public static object RotateElement(Document doc, dynamic args)
        {
            ElementId id = new ElementId(long.Parse((string)args.id));
            Element element = doc.GetElement(id);
            if (element == null) return new { error = "Element not found." };

            double angle = (double)args.angle_degrees * (Math.PI / 180.0);
            
            // Rotation axis (default to Z-axis at element center or provided point)
            XYZ axisPt = XYZ.Zero;
            if (args.axis_point != null)
            {
                axisPt = new XYZ((double)args.axis_point.x, (double)args.axis_point.y, (double)args.axis_point.z);
            }
            else
            {
                // Try to get element location
                Location loc = element.Location;
                if (loc is LocationPoint lp) axisPt = lp.Point;
                else if (loc is LocationCurve lc) axisPt = (lc.Curve.GetEndPoint(0) + lc.Curve.GetEndPoint(1)) / 2.0;
            }

            Line axis = Line.CreateBound(axisPt, axisPt + XYZ.BasisZ);
            ElementTransformUtils.RotateElement(doc, id, axis, angle);

            return new { success = true, rotated_id = id.ToString() };
        }

        public static object DeleteElement(Document doc, dynamic args)
        {
            ElementId id = new ElementId(long.Parse((string)args.id));
            doc.Delete(id);
            return new { success = true, deleted_id = id.ToString() };
        }
    }
}
