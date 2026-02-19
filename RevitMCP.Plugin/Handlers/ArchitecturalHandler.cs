using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using RevitMCP.Plugin.Extensions;

namespace RevitMCP.Plugin.Handlers
{
    public static class ArchitecturalHandler
    {
        public static object CreateLevel(Document doc, dynamic args)
        {
            double elevation = (double)args.elevation;
            string name = (string)args.name;
            
            Level level = Level.Create(doc, elevation);
            if (!string.IsNullOrEmpty(name)) level.Name = name;
            
            return new { id = level.Id.ToString(), name = level.Name, elevation = level.Elevation };
        }

        public static object CreateGrid(Document doc, dynamic args)
        {
            XYZ p1 = new XYZ((double)args.p1.x, (double)args.p1.y, (double)args.p1.z);
            XYZ p2 = new XYZ((double)args.p2.x, (double)args.p2.y, (double)args.p2.z);
            string name = (string)args.name;

            Line line = Line.CreateBound(p1, p2);
            Grid grid = Grid.Create(doc, line);
            
            if (!string.IsNullOrEmpty(name))
            {
                try { grid.Name = name; } catch { /* Ignore naming conflicts */ }
            }

            return new { id = grid.Id.ToString(), name = grid.Name };
        }

        public static object CreateWall(Document doc, dynamic args)
        {
            dynamic startObj = args.start ?? args.start_point;
            dynamic endObj = args.end ?? args.end_point;

            if (startObj == null || endObj == null)
            {
                return new { error = "Missing 'start' or 'end' points for wall creation." };
            }

            XYZ start = new XYZ((double)startObj.x, (double)startObj.y, (double)startObj.z);
            XYZ end = new XYZ((double)endObj.x, (double)endObj.y, (double)endObj.z);
            
            string levelIdStr = (string)args.level_id;
            if (string.IsNullOrEmpty(levelIdStr)) return new { error = "Missing 'level_id'." };
            ElementId levelId = new ElementId(long.Parse(levelIdStr));
            
            ElementId wallTypeId = GetWallTypeId(doc, args);

            double height = args.height != null ? (double)args.height : 3.0.MetersToFeet(); // Default to 3m (~9.84 ft)

            Line curve = Line.CreateBound(start, end);
            Wall wall = Wall.Create(doc, curve, wallTypeId, levelId, height, 0.0, false, false);
            
            return new { id = wall.Id.ToString(), type = wall.WallType.Name };
        }

        public static object CreateArcWall(Document doc, dynamic args)
        {
            XYZ p1 = new XYZ((double)args.p1.x, (double)args.p1.y, (double)args.p1.z);
            XYZ p2 = new XYZ((double)args.p2.x, (double)args.p2.y, (double)args.p2.z);
            XYZ p3 = new XYZ((double)args.p3.x, (double)args.p3.y, (double)args.p3.z);
            
            ElementId levelId = new ElementId(long.Parse((string)args.level_id));
            ElementId wallTypeId = GetWallTypeId(doc, args);
            double height = args.height != null ? (double)args.height : 3.0.MetersToFeet();

            Arc arc = Arc.Create(p1, p2, p3); // Start, End, Point on Arc
            Wall wall = Wall.Create(doc, arc, wallTypeId, levelId, height, 0.0, false, false);

            return new { id = wall.Id.ToString(), type = wall.WallType.Name };
        }

        public static object CreateEllipseWall(Document doc, dynamic args)
        {
            XYZ center = new XYZ((double)args.center.x, (double)args.center.y, (double)args.center.z);
            double radX = (double)args.radius_x;
            double radY = (double)args.radius_y;
            
            ElementId levelId = new ElementId(long.Parse((string)args.level_id));
            ElementId wallTypeId = GetWallTypeId(doc, args);
            double height = args.height != null ? (double)args.height : 3.0.MetersToFeet();

            Curve ellipse = Ellipse.CreateCurve(center, radX, radY, XYZ.BasisX, XYZ.BasisY, 0, 2 * Math.PI);
            Wall wall = Wall.Create(doc, ellipse, wallTypeId, levelId, height, 0.0, false, false);

            return new { id = wall.Id.ToString(), type = wall.WallType.Name };
        }

        private static ElementId GetWallTypeId(Document doc, dynamic args)
        {
            ElementId wallTypeId = null;
            if (args.wall_type_id != null)
            {
                wallTypeId = new ElementId(long.Parse((string)args.wall_type_id));
            }
            else if (args.type_name != null)
            {
                wallTypeId = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .FirstOrDefault(e => e.Name == (string)args.type_name)?.Id;
            }

            if (wallTypeId == null)
            {
                wallTypeId = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .FirstElementId();
            }
            return wallTypeId;
        }

        public static object CreateFloor(Document doc, dynamic args)
        {
            JArray points = (JArray)args.points;
            List<XYZ> xyzPoints = new List<XYZ>();
            foreach (var p in points)
            {
                xyzPoints.Add(new XYZ((double)p["x"], (double)p["y"], (double)p["z"]));
            }

            ElementId levelId = new ElementId(long.Parse((string)args.level_id));
            
            // Create loop
            CurveLoop loop = new CurveLoop();
            for (int i = 0; i < xyzPoints.Count; i++)
            {
                XYZ p1 = xyzPoints[i];
                XYZ p2 = xyzPoints[(i + 1) % xyzPoints.Count];
                loop.Append(Line.CreateBound(p1, p2));
            }

            // Using default floor type
            ElementId floorTypeId = new FilteredElementCollector(doc)
                .OfClass(typeof(FloorType))
                .FirstElementId();
                
            Floor floor = Floor.Create(doc, new List<CurveLoop> { loop }, floorTypeId, levelId);

            return new { id = floor.Id.ToString() };
        }

        public static object CreateWindow(Document doc, dynamic args)
        {
            XYZ location = new XYZ((double)args.location.x, (double)args.location.y, (double)args.location.z);
            ElementId levelId = new ElementId(long.Parse((string)args.level_id));
            Level level = (Level)doc.GetElement(levelId);
            ElementId wallId = new ElementId(long.Parse((string)args.wall_id));
            Element wall = doc.GetElement(wallId);

            FamilySymbol symbol = GetFamilySymbol(doc, BuiltInCategory.OST_Windows, (string)args.type_name);
            if (symbol == null) return new { error = "No Window family found." };
            if (!symbol.IsActive) symbol.Activate();

            FamilyInstance instance = doc.Create.NewFamilyInstance(location, symbol, wall, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
            return new { id = instance.Id.ToString(), name = instance.Name };
        }

        public static object CreateDoor(Document doc, dynamic args)
        {
            XYZ location = new XYZ((double)args.location.x, (double)args.location.y, (double)args.location.z);
            ElementId levelId = new ElementId(long.Parse((string)args.level_id));
            Level level = (Level)doc.GetElement(levelId);
            ElementId wallId = new ElementId(long.Parse((string)args.wall_id));
            Element wall = doc.GetElement(wallId);

            FamilySymbol symbol = GetFamilySymbol(doc, BuiltInCategory.OST_Doors, (string)args.type_name);
            if (symbol == null) return new { error = "No Door family found." };
            if (!symbol.IsActive) symbol.Activate();

            FamilyInstance instance = doc.Create.NewFamilyInstance(location, symbol, wall, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
            return new { id = instance.Id.ToString(), name = instance.Name };
        }

        public static object CreateOpening(Document doc, dynamic args)
        {
            XYZ p1 = new XYZ((double)args.p1.x, (double)args.p1.y, (double)args.p1.z);
            XYZ p2 = new XYZ((double)args.p2.x, (double)args.p2.y, (double)args.p2.z);
            ElementId wallId = new ElementId(long.Parse((string)args.wall_id));
            Wall wall = (Wall)doc.GetElement(wallId);

            Opening opening = doc.Create.NewOpening(wall, p1, p2);
            return new { id = opening.Id.ToString() };
        }

        public static object CreateRoof(Document doc, dynamic args)
        {
            JArray points = (JArray)args.points;
            List<XYZ> xyzPoints = new List<XYZ>();
            foreach (var p in points)
            {
                xyzPoints.Add(new XYZ((double)p["x"], (double)p["y"], (double)p["z"]));
            }
            
            ElementId levelId = new ElementId(long.Parse((string)args.level_id));
            Level level = (Level)doc.GetElement(levelId);

            CurveArray profile = new CurveArray();
            for (int i = 0; i < xyzPoints.Count; i++)
            {
                XYZ p1 = xyzPoints[i];
                XYZ p2 = xyzPoints[(i + 1) % xyzPoints.Count];
                profile.Append(Line.CreateBound(p1, p2));
            }

            // Select Roof Type
             RoofType roofType = null;
             if (args.type_name != null)
             {
                 roofType = new FilteredElementCollector(doc)
                     .OfClass(typeof(RoofType))
                     .Cast<RoofType>()
                     .FirstOrDefault(e => e.Name == (string)args.type_name);
             }

            if (roofType == null)
            {
                var types = new FilteredElementCollector(doc)
                    .OfClass(typeof(RoofType))
                    .Cast<RoofType>()
                    .ToList();
                
                // Prefer one that has "Basic", "Básic", "Generic", "Genérico" or "Cubierta"
                roofType = types.FirstOrDefault(t => 
                    t.FamilyName.IndexOf("Basic", StringComparison.OrdinalIgnoreCase) >= 0 || 
                    t.FamilyName.IndexOf("Básic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    t.FamilyName.IndexOf("Generic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    t.FamilyName.IndexOf("Genérico", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    t.FamilyName.IndexOf("Cubierta", StringComparison.OrdinalIgnoreCase) >= 0
                ) ?? types.FirstOrDefault();
            }

            if (roofType == null) return new { error = "No RoofType found." };

            ModelCurveArray curveArray = new ModelCurveArray();
            FootPrintRoof roof = doc.Create.NewFootPrintRoof(profile, level, roofType, out curveArray);

            // Apply Slope if provided
            if (args.slope != null)
            {
                double slopePercentage = (double)args.slope; // e.g., 0.03 for 3%
                double slopeAngle = Math.Atan(slopePercentage);

                foreach (ModelCurve modelCurve in curveArray)
                {
                    roof.set_DefinesSlope(modelCurve, true);
                    roof.set_SlopeAngle(modelCurve, slopeAngle);
                }
            }

            return new { id = roof.Id.ToString(), name = roof.Name, type = roof.RoofType.Name };
        }

        private static FamilySymbol GetFamilySymbol(Document doc, BuiltInCategory bic, string typeName)
        {
            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(bic)
                .Cast<FamilySymbol>();

            if (!string.IsNullOrEmpty(typeName))
            {
                var symbol = collector.FirstOrDefault(e => e.Name == typeName);
                if (symbol != null) return symbol;
            }
            return collector.FirstOrDefault();
        }
    }
}
