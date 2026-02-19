using System.Linq;
using Autodesk.Revit.DB;

namespace RevitMCP.Plugin.Extensions
{
    public static class DocumentExtensions
    {
        /// <summary>
        /// Finds the nearest Level to the given elevation.
        /// </summary>
        public static Level FindNearestLevel(this Document doc, double elevation)
        {
            if (doc == null) return null;

            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>();

            Level nearest = null;
            double minDiff = double.MaxValue;

            foreach (Level lvl in collector)
            {
                double diff = System.Math.Abs(lvl.Elevation - elevation);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    nearest = lvl;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Safely retrieves an Element by its Integer ID or UniqueId. 
        /// (Revit 2024+ uses long for ElementId, previous versions use int).
        /// This implementation assumes 2024+ usage of long/ElementId constructor.
        /// </summary>
        public static Element GetElementById(this Document doc, string idString)
        {
            if (string.IsNullOrEmpty(idString)) return null;

            if (long.TryParse(idString, out long idLong))
            {
                ElementId eid = new ElementId(idLong);
                return doc.GetElement(eid);
            }
            // Fallback: try by UniqueId
            return doc.GetElement(idString);
        }
    }
}
