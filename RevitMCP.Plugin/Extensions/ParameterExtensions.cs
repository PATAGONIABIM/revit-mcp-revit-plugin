using Autodesk.Revit.DB;

namespace RevitMCP.Plugin.Extensions
{
    public static class ParameterExtensions
    {
        public static double GetParameterDouble(this Element e, BuiltInParameter bip)
        {
            Parameter p = e.get_Parameter(bip);
            return p != null && p.HasValue ? p.AsDouble() : 0.0;
        }

        public static string GetParameterString(this Element e, BuiltInParameter bip)
        {
            Parameter p = e.get_Parameter(bip);
            return p != null && p.HasValue ? p.AsString() : string.Empty;
        }

        public static int GetParameterInteger(this Element e, BuiltInParameter bip)
        {
            Parameter p = e.get_Parameter(bip);
            return p != null && p.HasValue ? p.AsInteger() : 0;
        }
        
        public static ElementId GetParameterElementId(this Element e, BuiltInParameter bip)
        {
            Parameter p = e.get_Parameter(bip);
            return p != null && p.HasValue ? p.AsElementId() : ElementId.InvalidElementId;
        }

        public static bool SetParameter(this Element e, BuiltInParameter bip, double value)
        {
            Parameter p = e.get_Parameter(bip);
            if (p != null && !p.IsReadOnly)
            {
                return p.Set(value);
            }
            return false;
        }

        public static bool SetParameter(this Element e, BuiltInParameter bip, string value)
        {
            Parameter p = e.get_Parameter(bip);
            if (p != null && !p.IsReadOnly)
            {
                return p.Set(value);
            }
            return false;
        }
        
        public static bool SetParameter(this Element e, BuiltInParameter bip, int value)
        {
            Parameter p = e.get_Parameter(bip);
            if (p != null && !p.IsReadOnly)
            {
                return p.Set(value);
            }
            return false;
        }
        
        public static bool SetParameter(this Element e, BuiltInParameter bip, ElementId value)
        {
            Parameter p = e.get_Parameter(bip);
            if (p != null && !p.IsReadOnly)
            {
                return p.Set(value);
            }
            return false;
        }
    }
}
