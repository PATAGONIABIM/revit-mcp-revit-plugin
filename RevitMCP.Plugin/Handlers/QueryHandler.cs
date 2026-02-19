using System;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System.Collections.Generic;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;

namespace RevitMCP.Plugin.Handlers
{
    public static class QueryHandler
    {
        public static object GetLevels(Document doc)
        {
             return new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .ToElements()
                .Select(e => new { id = e.Id.ToString(), name = e.Name, elevation = ((Level)e).Elevation })
                .ToList();
        }

        public static object GetGrids(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(Grid))
                .ToElements()
                .Select(e => new { id = e.Id.ToString(), name = e.Name })
                .ToList();
        }

        public static object GetWallTypes(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .ToElements()
                .Select(e => new { id = e.Id.ToString(), name = e.Name })
                .ToList();
        }

        public static object GetRoofTypes(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(RoofType))
                .ToElements()
                .Select(e => new { id = e.Id.ToString(), name = e.Name, family = ((RoofType)e).FamilyName })
                .ToList();
        }

        public static object GetRailingTypes(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(RailingType))
                .ToElements()
                .Select(e => new { id = e.Id.ToString(), name = e.Name })
                .ToList();
        }

        public static object GetFamilies(Document doc, dynamic args)
        {
            string categoryName = (string)args.category;
            BuiltInCategory bic = BuiltInCategory.INVALID;
            
            if (categoryName == "Doors") bic = BuiltInCategory.OST_Doors;
            else if (categoryName == "Windows") bic = BuiltInCategory.OST_Windows;
            else if (categoryName == "Columns") bic = BuiltInCategory.OST_Columns;
            else if (categoryName == "StructuralFraming") bic = BuiltInCategory.OST_StructuralFraming;
            else return new { error = "Invalid category. Use Doors, Windows, Columns, or StructuralFraming." };

            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(bic)
                .Cast<FamilySymbol>()
                .Select(e => new { name = e.Name, family = e.FamilyName })
                .ToList();
        }

        public static object GetViews(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => !v.IsTemplate && (v.ViewType == ViewType.FloorPlan || v.ViewType == ViewType.ThreeD))
                .Select(v => new { id = v.Id.ToString(), name = v.Name, type = v.ViewType.ToString() })
                .ToList();
        }

        public static object GetMEPTypes(Document doc)
        {
            var ductTypes = new FilteredElementCollector(doc).OfClass(typeof(DuctType)).ToElements().Select(e => e.Name).ToList();
            var pipeTypes = new FilteredElementCollector(doc).OfClass(typeof(PipeType)).ToElements().Select(e => e.Name).ToList();
            var mechSystems = new FilteredElementCollector(doc).OfClass(typeof(MechanicalSystemType)).ToElements().Select(e => e.Name).ToList();
            var pipeSystems = new FilteredElementCollector(doc).OfClass(typeof(PipingSystemType)).ToElements().Select(e => e.Name).ToList();
            
            return new { 
                duct_types = ductTypes, 
                pipe_types = pipeTypes, 
                mechanical_systems = mechSystems, 
                piping_systems = pipeSystems 
            };
        }
    }
}
