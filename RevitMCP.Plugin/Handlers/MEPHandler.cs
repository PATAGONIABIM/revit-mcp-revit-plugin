using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;

namespace RevitMCP.Plugin.Handlers
{
    public static class MEPHandler
    {
        public static object CreateDuct(Document doc, dynamic args)
        {
            XYZ p1 = new XYZ((double)args.p1.x, (double)args.p1.y, (double)args.p1.z);
            XYZ p2 = new XYZ((double)args.p2.x, (double)args.p2.y, (double)args.p2.z);
            ElementId levelId = new ElementId(long.Parse((string)args.level_id));

            // System Type
            ElementId systemTypeId = GetMEPSystemTypeId(doc, (string)args.system_type, typeof(MechanicalSystemType));
            if (systemTypeId == null) return new { error = "Duct System Type not found" };

            // Duct Type
            ElementId ductTypeId = GetMEPTypeId(doc, (string)args.duct_type, typeof(DuctType));
            if (ductTypeId == null) return new { error = "Duct Type not found" };

            Duct duct = Duct.Create(doc, systemTypeId, ductTypeId, levelId, p1, p2);
            return new { id = duct.Id.ToString(), name = duct.Name };
        }

        public static object CreatePipe(Document doc, dynamic args)
        {
            XYZ p1 = new XYZ((double)args.p1.x, (double)args.p1.y, (double)args.p1.z);
            XYZ p2 = new XYZ((double)args.p2.x, (double)args.p2.y, (double)args.p2.z);
            ElementId levelId = new ElementId(long.Parse((string)args.level_id));

            // System Type
            ElementId systemTypeId = GetMEPSystemTypeId(doc, (string)args.system_type, typeof(PipingSystemType));
            if (systemTypeId == null) return new { error = "Piping System Type not found" };

            // Pipe Type
            ElementId pipeTypeId = GetMEPTypeId(doc, (string)args.pipe_type, typeof(PipeType));
            if (pipeTypeId == null) return new { error = "Pipe Type not found" };

            Pipe pipe = Pipe.Create(doc, systemTypeId, pipeTypeId, levelId, p1, p2);
            return new { id = pipe.Id.ToString(), name = pipe.Name };
        }

        private static ElementId GetMEPSystemTypeId(Document doc, string name, Type systemTypeClass)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new FilteredElementCollector(doc).OfClass(systemTypeClass).FirstElementId();
            }
            
            var type = new FilteredElementCollector(doc)
                .OfClass(systemTypeClass)
                .FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            
            return type?.Id ?? new FilteredElementCollector(doc).OfClass(systemTypeClass).FirstElementId();
        }

        private static ElementId GetMEPTypeId(Document doc, string name, Type typeClass)
        {
             if (string.IsNullOrEmpty(name))
            {
                return new FilteredElementCollector(doc).OfClass(typeClass).FirstElementId();
            }

            var type = new FilteredElementCollector(doc)
                .OfClass(typeClass)
                .FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            
            return type?.Id ?? new FilteredElementCollector(doc).OfClass(typeClass).FirstElementId();
        }
    }
}
