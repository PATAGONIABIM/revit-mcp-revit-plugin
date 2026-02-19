using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitMCP.Plugin.Extensions;

namespace RevitMCP.Plugin.Handlers
{
    public static class DynamoHandler
    {
        public static object CreateComplexSolid(Document doc, dynamic args)
        {
            try
            {
                // Emulating Dynamo "Script":
                // 1. Point1 = (0,0,0)
                // 2. Sphere1 = Sphere.ByCenterPointRadius(Point1, 1.0)
                // 3. Point2 = (1.5,0,0)
                // 4. Sphere2 = Sphere.ByCenterPointRadius(Point2, 1.0)
                // 5. Union = Solid.Union(Sphere1, Sphere2)
                
                XYZ p1 = new XYZ(0, 0, 0);
                double r1 = 1.0.MetersToFeet();
                Solid s1 = CreateSphere(p1, r1);

                XYZ p2 = new XYZ(1.5.MetersToFeet(), 0, 0);
                double r2 = 1.0.MetersToFeet();
                Solid s2 = CreateSphere(p2, r2);

                Solid union = BooleanUnion(doc, s1, s2);

                // Create DirectShape
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

        // --- Native "Nodes" Implementation ---

        private static Solid CreateSphere(XYZ center, double radius)
        {
            // Create a semi-circle profile for revolution
            // We revolve around the Z-axis of the locale coordinate system defined at 'center'
            
            // 1. Define local plane (X, Z)
            XYZ frameCenter = center;
            XYZ axis = XYZ.BasisZ; // Unit Z vector for revolution axis
            
            // 2. Create profile: Semi-circle in the XZ plane (actually any vertical plane works)
            // Arc starts at bottom (-Z) and goes to top (+Z)
            XYZ start = center - new XYZ(0, 0, radius);
            XYZ end = center + new XYZ(0, 0, radius);
            XYZ mid = center + new XYZ(radius, 0, 0);

            Arc arc = Arc.Create(start, end, mid);
            
            // Close the loop with a line along the axis
            Line axisLine = Line.CreateBound(end, start);
            
            List<Curve> profile = new List<Curve> { arc, axisLine };
            CurveLoop loop = CurveLoop.Create(profile);
            List<CurveLoop> loops = new List<CurveLoop> { loop };

            // 3. Revolve
            // Frame: Origin at center. Axis is Z.
            Frame frame = new Frame(center, XYZ.BasisX, XYZ.BasisY, XYZ.BasisZ);
            
            // Note: CreateRevolvedGeometry requires the loop to be coplanar with the axis?
            // Actually, simply: axis of revolution is the Line 'axisLine' infinite?
            // Use 0 to 2PI.
            
            // Correct approach:
            // The loop must be on the right side of the axis.
            // Our axis is vertical at 'center'.
            // Our arc is in X-Z plane relative to center?
            // Let's keep it simple: Create geometry at Origin, then translate?
            // Or careful construction.
            
            // Let's create at Global Origin (0,0,0) then translate.
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
            
            // Move to target center
            if (!center.IsZeroLength()) // If not zero
            {
                Transform tr = Transform.CreateTranslation(center);
                sphere = SolidUtils.CreateTransformed(sphere, tr);
            }

            return sphere;
        }

        private static Solid BooleanUnion(Document doc, Solid s1, Solid s2)
        {
            return BooleanOperationsUtils.ExecuteBooleanOperation(s1, s2, BooleanOperationsType.Union);
        }

        public static object CreateColumnGrid(Document doc, dynamic args)
        {
            try
            {
                // Parameters
                double width = (double?)args.width ?? 20.0;
                double depth = (double?)args.depth ?? 20.0;
                double spaceX = (double?)args.spacing_x ?? 1.0;
                double spaceY = (double?)args.spacing_y ?? 1.0;
                double colHeightMetric = (double?)args.height ?? 3.0;

                // Units conversion
                double widthFt = width.MetersToFeet();
                double depthFt = depth.MetersToFeet();
                double spaceXFt = spaceX.MetersToFeet();
                double spaceYFt = spaceY.MetersToFeet();
                double colHeightFt = colHeightMetric.MetersToFeet();

                // Get Dependents
                Level level = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .OrderBy(l => l.Elevation)
                    .FirstOrDefault();

                if (level == null) throw new Exception("No levels found in project.");

                FamilySymbol colSymbol = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_StructuralColumns)
                    .FirstOrDefault(q => q.Name == "300 x 300 mm" || q.Name == "30 x 30 cm") as FamilySymbol;

                if (colSymbol == null)
                {
                     // Fallback to any column
                     colSymbol = new FilteredElementCollector(doc)
                        .OfClass(typeof(FamilySymbol))
                        .OfCategory(BuiltInCategory.OST_StructuralColumns)
                        .FirstOrDefault() as FamilySymbol;
                }

                if (colSymbol == null) throw new Exception("No Structural Column families loaded.");
                if (!colSymbol.IsActive) colSymbol.Activate();

                int countGrids = 0;
                int countCols = 0;

                // 1. Create Vertical Grids (Constant X, verifying Y)
                for (double x = 0; x <= widthFt + 0.001; x += spaceXFt)
                {
                    XYZ start = new XYZ(x, -2, 0); 
                    XYZ end = new XYZ(x, depthFt + 2, 0);
                    Line line = Line.CreateBound(start, end);
                    try { Grid.Create(doc, line); countGrids++; } catch { }
                }

                // 2. Create Horizontal Grids (Constant Y, verifying X)
                for (double y = 0; y <= depthFt + 0.001; y += spaceYFt)
                {
                    XYZ start = new XYZ(-2, y, 0);
                    XYZ end = new XYZ(widthFt + 2, y, 0);
                    Line line = Line.CreateBound(start, end);
                    try { Grid.Create(doc, line); countGrids++; } catch { }
                }

                // 3. Create Columns at Intersections
                for (double x = 0; x <= widthFt + 0.001; x += spaceXFt)
                {
                    for (double y = 0; y <= depthFt + 0.001; y += spaceYFt)
                    {
                        XYZ point = new XYZ(x, y, 0);
                        FamilyInstance col = doc.Create.NewFamilyInstance(point, colSymbol, level, Autodesk.Revit.DB.Structure.StructuralType.Column);
                        
                        // Set Height constraints
                        // Strategy: Base Level = level, Top Level = level, Top Offset = height
                        Parameter baseLevelParam = col.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
                        Parameter topLevelParam = col.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
                        Parameter topOffsetParam = col.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
                        
                        if (baseLevelParam != null) baseLevelParam.Set(level.Id);
                        if (topLevelParam != null) topLevelParam.Set(level.Id); // Attach to same level
                        if (topOffsetParam != null) topOffsetParam.Set(colHeightFt); // Offset positively

                        countCols++;
                    }
                }

                return new 
                { 
                    status = "Parametric Grid Created", 
                    grids_created = countGrids, 
                    columns_created = countCols,
                    dimensions = $"{width}x{depth}m area, {countCols} pillars"
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message, stack = ex.StackTrace };
            }
        }

        public static object ExecuteScript(Document doc, dynamic args)
        {
             // Placeholder for running raw DesignScript if requested.
             return new { status = "Not implemented yet" };
        }
    }
}
