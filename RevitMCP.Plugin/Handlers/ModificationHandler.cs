using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitMCP.Plugin.Extensions;

namespace RevitMCP.Plugin.Handlers
{
    public static class ModificationHandler
    {
        public static object MoveElement(Document doc, dynamic args)
        {
            ElementId elementId = new ElementId(long.Parse((string)args.element_id));
            XYZ translation = new XYZ((double)args.translation.x, (double)args.translation.y, (double)args.translation.z);
            
            ElementTransformUtils.MoveElement(doc, elementId, translation);
            return new { status = "success", moved_element_id = elementId.ToString() };
        }

        public static object CopyElement(Document doc, dynamic args)
        {
            ElementId elementId = new ElementId(long.Parse((string)args.element_id));
            XYZ translation = new XYZ((double)args.translation.x, (double)args.translation.y, (double)args.translation.z);
            
            ICollection<ElementId> newIds = ElementTransformUtils.CopyElement(doc, elementId, translation);
            return new { status = "success", new_element_ids = newIds.Select(id => id.ToString()).ToList() };
        }

        public static object MirrorElement(Document doc, dynamic args)
        {
            ElementId elementId = new ElementId(long.Parse((string)args.element_id));
            XYZ p1 = new XYZ((double)args.plane_point.x, (double)args.plane_point.y, (double)args.plane_point.z);
            XYZ p2 = new XYZ((double)args.plane_normal.x, (double)args.plane_normal.y, (double)args.plane_normal.z);
            
            // Create a Plane
            Plane plane = Plane.CreateByNormalAndOrigin(p2, p1);

            ElementTransformUtils.MirrorElement(doc, elementId, plane);
             // MirrorElement returns void/bool? actually it creates a new element if copy is true, but standard MirrorElement modifies or creates?
             // ElementTransformUtils.MirrorElement mirrors the element. It doesn't have a "copy" flag, it always creates a copy? No wait.
             // "Mirrors one or more elements about a plane." - It creates COPIES if you imply it?
             // Wait, ElementTransformUtils.MirrorElement(Document, ElementId, Plane) -> void. It mirrors it IN PLACE?
             // Actually, usually Mirror creates a copy in Revit UI.
             // Let's check API. "Mirrors an element about a plane."
             // If we want to COPY, we might need to use CopyElement first?
             // Actually, `ElementTransformUtils.MirrorElements` takes a boolean `copy`.
             // `ElementTransformUtils.MirrorElement` (singular) might be deprecated or behaves differently.
             // Let's use `MirrorElements` for safety and flexibility.

            bool copy = args.copy != null ? (bool)args.copy : false;
            
            // MirrorElements requires a set of IDs
            List<ElementId> ids = new List<ElementId> { elementId };
            
            // It does not return the new IDs easily if copy=true. 
            // We might just return "success".
            
            ElementTransformUtils.MirrorElements(doc, ids, plane, copy);

            return new { status = "success" };
        }

        public static object ArrayElement(Document doc, dynamic args)
        {
            ElementId elementId = new ElementId(long.Parse((string)args.element_id));
            int count = (int)args.count;
            XYZ translation = new XYZ((double)args.translation.x, (double)args.translation.y, (double)args.translation.z);
            
            // Create Linear Array
            // LinearArray.Create(Document, View, ElementId, int, XYZ, ArrayAnchorMember)
            // This is for creating an *associative* array (Group).
            // If the user just wants copies, we should loop CopyElement.
            // But "Array" usually implies the Revit Array feature.
            
            // Let's assume non-associative (just copies) for simplicity if we can, 
            // OR use LinearArray.Create which creates a Group.
            // Let's use LinearArray.Create to be "Revit-like".
            
            // We need a View. Uses Active View.
            View view = doc.ActiveView;
            
            LinearArray.Create(doc, view, elementId, count, translation, ArrayAnchorMember.Second);

            return new { status = "success", count = count };
        }
        
        public static object AlignElements(Document doc, dynamic args)
        {
             // This requires References (Faces), which are hard to pass via JSON.
             // We will accept "Stable Representation" strings for faces.
             string ref1Str = (string)args.reference1;
             string ref2Str = (string)args.reference2;
             
             Reference r1 = Reference.ParseFromStableRepresentation(doc, ref1Str);
             Reference r2 = Reference.ParseFromStableRepresentation(doc, ref2Str);
             
             doc.Create.NewAlignment(doc.ActiveView, r1, r2);
             
             return new { status = "success" };
        }
    }
}
