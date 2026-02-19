using System;
using System.Linq;
using Autodesk.Revit.DB;

namespace RevitMCP.Plugin.Handlers
{
    public static class StructuralHandler
    {
        public static object CreateColumn(Document doc, dynamic args)
        {
            XYZ location = new XYZ((double)args.location.x, (double)args.location.y, (double)args.location.z);
            ElementId levelId = new ElementId(long.Parse((string)args.level_id));
            Level level = (Level)doc.GetElement(levelId);

            FamilySymbol symbol = null;
            if (args.type_name != null)
            {
                symbol = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_Columns) // Try Arch Columns first
                    .Cast<FamilySymbol>()
                    .FirstOrDefault(e => e.Name == (string)args.type_name);
                
                 if (symbol == null)
                 {
                    symbol = new FilteredElementCollector(doc)
                        .OfClass(typeof(FamilySymbol))
                        .OfCategory(BuiltInCategory.OST_StructuralColumns) // Then Structural
                        .Cast<FamilySymbol>()
                        .FirstOrDefault(e => e.Name == (string)args.type_name);
                 }
            }

            if (symbol == null)
            {
                symbol = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_Columns)
                    .Cast<FamilySymbol>()
                    .FirstOrDefault();
                
                 if (symbol == null)
                    symbol = new FilteredElementCollector(doc)
                        .OfClass(typeof(FamilySymbol))
                        .OfCategory(BuiltInCategory.OST_StructuralColumns)
                        .Cast<FamilySymbol>()
                        .FirstOrDefault();
            }

            if (symbol == null) return new { error = "No Column family found in project." };
            if (!symbol.IsActive) symbol.Activate();

            FamilyInstance instance = doc.Create.NewFamilyInstance(location, symbol, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
            return new { id = instance.Id.ToString(), name = instance.Name };
        }
        public static object CreateBeam(Document doc, dynamic args)
        {
            XYZ start = new XYZ((double)args.start.x, (double)args.start.y, (double)args.start.z);
            XYZ end = new XYZ((double)args.end.x, (double)args.end.y, (double)args.end.z);
            ElementId levelId = new ElementId(long.Parse((string)args.level_id));
            Level level = (Level)doc.GetElement(levelId);

            FamilySymbol symbol = null;
            if (args.type_name != null)
            {
                symbol = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .Cast<FamilySymbol>()
                    .FirstOrDefault(e => e.Name == (string)args.type_name);
            }

            if (symbol == null)
            {
                symbol = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .Cast<FamilySymbol>()
                    .FirstOrDefault();
            }

            if (symbol == null) return new { error = "No Structural Framing family found in project." };
            if (!symbol.IsActive) symbol.Activate();

            Curve curve = Line.CreateBound(start, end);
            FamilyInstance instance = doc.Create.NewFamilyInstance(curve, symbol, level, Autodesk.Revit.DB.Structure.StructuralType.Beam);
            return new { id = instance.Id.ToString(), name = instance.Name };
        }
    }
}
