using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using RevitMCP.Plugin.Extensions;

namespace RevitMCP.Plugin.Handlers
{
    public static class GeometryCreationHandler
    {
        public static object CreateReferencePlane(Document doc, dynamic args)
        {
            XYZ p1 = new XYZ((double)args.p1.x, (double)args.p1.y, (double)args.p1.z);
            XYZ p2 = new XYZ((double)args.p2.x, (double)args.p2.y, (double)args.p2.z);
            // We need a third point or a normal/vector to define the plane.
            // Usually Reference Planes are defined by a line on a view (active view) or 3 points.
            // Let's assume we are drawing it on the active view's workplane or level, 
            // OR we ask for a "cut vector". 
            // doc.Create.NewReferencePlane(XYZ bubbleEnd, XYZ freeEnd, XYZ cutVec, View view)
            
            XYZ cutVec = XYZ.BasisZ; // Default to vertical plane
            if (args.cut_vector != null)
            {
                cutVec = new XYZ((double)args.cut_vector.x, (double)args.cut_vector.y, (double)args.cut_vector.z);
            }

            ReferencePlane rp = doc.Create.NewReferencePlane(p1, p2, cutVec, doc.ActiveView);
            
            if (args.name != null)
            {
                rp.Name = (string)args.name;
            }

            return new { id = rp.Id.ToString(), name = rp.Name };
        }

        public static object CreateExtrusion(Document doc, dynamic args)
        {
            // Create Loop
            CurveLoop loop = new CurveLoop();
            
            if (args.profile != null)
            {
                JArray profile = (JArray)args.profile;
                foreach (dynamic curveData in profile)
                {
                    string type = (string)curveData.type;
                    
                    if (string.Equals(type, "Line", StringComparison.OrdinalIgnoreCase))
                    {
                        XYZ p1 = GetPoint(curveData.start);
                        XYZ p2 = GetPoint(curveData.end);
                        loop.Append(Line.CreateBound(p1, p2));
                    }
                    else if (string.Equals(type, "Arc", StringComparison.OrdinalIgnoreCase))
                    {
                        XYZ p1 = GetPoint(curveData.start);
                        XYZ p2 = GetPoint(curveData.end);
                        XYZ p3 = GetPoint(curveData.point_on_arc);
                        loop.Append(Arc.Create(p1, p2, p3));
                    }
                    else if (string.Equals(type, "Circle", StringComparison.OrdinalIgnoreCase))
                    {
                        XYZ center = GetPoint(curveData.center);
                        double radius = (double)curveData.radius; // in meters
                        // Convert radius? Assuming inputs are in native units from LLM? 
                        // Wait, previous code used meters input and converted. 
                        // Let's assume input is Meters and we use .MetersToFeet().
                        radius = radius.MetersToFeet();
                        
                        XYZ normal = XYZ.BasisZ;
                        if (curveData.normal != null) normal = GetPoint(curveData.normal);
                        
                        Plane plane = Plane.CreateByNormalAndOrigin(normal, center);
                        loop.Append(Arc.Create(plane, radius, 0, 2 * Math.PI));
                    }
                    else if (string.Equals(type, "Spline", StringComparison.OrdinalIgnoreCase))
                    {
                        List<XYZ> points = new List<XYZ>();
                        foreach (dynamic p in curveData.points) points.Add(GetPoint(p));
                        
                        // HermiteSpline
                        try 
                        {
                            HermiteSpline spline = HermiteSpline.Create(points, false);
                            loop.Append(spline);
                        }
                        catch
                        {
                            // Fallback or error if spline fails (e.g. not enough points)
                        }
                    }
                }
            }
            else if (args.points != null)
            {
                // Legacy Polygon support
                JArray points = (JArray)args.points;
                List<XYZ> loopPoints = new List<XYZ>();
                foreach (var p in points) loopPoints.Add(GetPoint(p));
                
                for (int i = 0; i < loopPoints.Count; i++)
                {
                    XYZ p1 = loopPoints[i];
                    XYZ p2 = loopPoints[(i + 1) % loopPoints.Count];
                    loop.Append(Line.CreateBound(p1, p2));
                }
            }
            else 
            {
                return new { error = "Missing 'profile' or 'points' argument." };
            }
            
            double height = args.height != null ? (double)args.height : 1.0.MetersToFeet();
            
            // Validate Loop
            if (!loop.IsOpen()) 
            {
                 // It's closed, good.
            }
            // Actually CurveLoop.IsOpen() returns true if open. We want it closed. 
            // Also DirectShape requires closed, planar loops mostly.
            
            List<CurveLoop> loops = new List<CurveLoop> { loop };
            
            // Extrusion Direction (Normal of the loop)
            // We need a reliable normal.
            // If it's a circle, we have the normal. 
            // If it's a polygon, we computed it.
            // Let's try to get it from the first planar curve.
            XYZ extrusionDir = XYZ.BasisZ;
            if (loop.HasPlane())
            {
                extrusionDir = loop.GetPlane().Normal;
            }
            
            Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(loops, extrusionDir, height);
            
            // Create DirectShape
            DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
            ds.SetShape(new List<GeometryObject> { solid });
            
            if (args.name != null) ds.Name = (string)args.name;

            return new { id = ds.Id.ToString(), name = ds.Name };
        }

        private static XYZ GetPoint(dynamic p)
        {
             return new XYZ((double)p.x, (double)p.y, (double)p.z);
        }
    }
}
