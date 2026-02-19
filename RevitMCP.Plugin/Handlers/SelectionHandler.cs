using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitMCP.Plugin.Handlers
{
    public static class SelectionHandler
    {
        public static object SelectFace(UIDocument uidoc, dynamic args)
        {
            string prompt = args.prompt != null ? (string)args.prompt : "Select a face";
            
            try
            {
                Reference refFace = uidoc.Selection.PickObject(ObjectType.Face, prompt);
                string stableRef = refFace.ConvertToStableRepresentation(uidoc.Document);
                
                return new 
                { 
                    status = "success", 
                    reference = stableRef,
                    element_id = refFace.ElementId.ToString()
                };
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return new { status = "cancelled" };
            }
        }

        public static object SelectElement(UIDocument uidoc, dynamic args)
        {
            string prompt = args.prompt != null ? (string)args.prompt : "Select an element";
            
            try
            {
                Reference refElem = uidoc.Selection.PickObject(ObjectType.Element, prompt);
                Element elem = uidoc.Document.GetElement(refElem);
                
                return new 
                { 
                    status = "success", 
                    element_id = elem.Id.ToString(),
                    category = elem.Category?.Name,
                    name = elem.Name
                };
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return new { status = "cancelled" };
            }
        }
    }
}
