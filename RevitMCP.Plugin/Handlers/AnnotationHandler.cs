using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;

namespace RevitMCP.Plugin.Handlers
{
    public static class AnnotationHandler
    {
        public static object CreateTextNote(Document doc, dynamic args)
        {
            string text = (string)args.text;
            XYZ location = new XYZ((double)args.location.x, (double)args.location.y, (double)args.location.z);
            
            ElementId viewId = args.view_id != null ? new ElementId(long.Parse((string)args.view_id)) : doc.ActiveView.Id;
            
            // TextTypeId
            ElementId textTypeId = args.type_id != null ? new ElementId(long.Parse((string)args.type_id)) : doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);
            
            if (textTypeId == null || textTypeId == ElementId.InvalidElementId)
            {
                textTypeId = new FilteredElementCollector(doc).OfClass(typeof(TextNoteType)).FirstElementId();
            }

            TextNote note = TextNote.Create(doc, viewId, location, text, textTypeId);
            return new { id = note.Id.ToString(), text = note.Text };
        }

        public static object CreateTag(Document doc, dynamic args)
        {
            ElementId elementId = new ElementId(long.Parse((string)args.element_id));
            Element element = doc.GetElement(elementId);
            if (element == null) return new { error = "Element not found" };

            XYZ location = new XYZ((double)args.location.x, (double)args.location.y, (double)args.location.z);
            
            ElementId viewId = args.view_id != null ? new ElementId(long.Parse((string)args.view_id)) : doc.ActiveView.Id;
            View view = (View)doc.GetElement(viewId);

            // Create Tag (IndependentTag)
            // Revit 2018+ Create method
            IndependentTag tag = IndependentTag.Create(doc, viewId, new Reference(element), false, TagMode.TM_ADDBY_CATEGORY, TagOrientation.Horizontal, location);
            
            return new { id = tag.Id.ToString(), family = tag.Name };
        }

        public static object CreateDimension(Document doc, dynamic args)
        {
            XYZ p1 = new XYZ((double)args.p1.x, (double)args.p1.y, (double)args.p1.z);
            XYZ p2 = new XYZ((double)args.p2.x, (double)args.p2.y, (double)args.p2.z);
            Line line = Line.CreateBound(p1, p2);

            ReferenceArray refArray = new ReferenceArray();
            JArray elementIds = (JArray)args.element_ids;
            
            if (elementIds.Count < 2) return new { error = "Need at least 2 elements to dimension." };

            foreach (var idToken in elementIds)
            {
                ElementId id = new ElementId(long.Parse((string)idToken));
                Element elem = doc.GetElement(id);
                
                if (elem is Grid grid)
                {
                    // Grids are Datum elements, their curve reference is usually reliable for dimensioning
                     // Note: Grid.Curve might be unbounded, but we can try getting the reference from the underlying line
                     // However, often passing the Grid reference itself works or we need to find the specific GeometryObject
                     
                     // Try 1: Specific reference from Geometry
                     // Options opt = new Options();
                     // opt.ComputeReferences = true;
                     // opt.IncludeNonVisibleObjects = true;
                     // GeometryElement geo = grid.get_Geometry(opt);
                     // ... iterating geometry is hard for Datum elements.
                     
                     // Try 2: Simple Reference(grid) - often works for Grids
                     refArray.Append(new Reference(grid));
                }
                else if (elem is Wall wall)
                {
                     // Placeholder for Wall dimensioning (requires more logic)
                }
            }

            if (refArray.Size < 2) return new { error = "Could not resolve at least 2 references from provided elements." };

            ElementId viewId = args.view_id != null ? new ElementId(long.Parse((string)args.view_id)) : doc.ActiveView.Id;
            View view = (View)doc.GetElement(viewId);

            Dimension dim = doc.Create.NewDimension(view, line, refArray);
            return new { id = dim.Id.ToString(), value = dim.Value };
        }
    }
}
