using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitMCP.Plugin.Extensions;

namespace RevitMCP.Plugin.Handlers
{
    public static class LocationHandler
    {
        public static object GetProjectLocation(Document doc)
        {
            SiteLocation site = doc.SiteLocation;
            
            // Convert angle to degrees
            double timeZone = site.TimeZone;
            double lat = site.Latitude * (180.0 / Math.PI); // Internal is radians
            double lon = site.Longitude * (180.0 / Math.PI); // Internal is radians

            return new 
            { 
                city = site.PlaceName,
                latitude = lat,
                longitude = lon,
                time_zone = timeZone
            };
        }

        public static object GetProjectBasePoint(Document doc)
        {
            // Get Project Base Point
            BasePoint pbp = new FilteredElementCollector(doc)
                .OfClass(typeof(BasePoint))
                .Cast<BasePoint>()
                .FirstOrDefault(e => !e.IsShared);

            // Get Survey Point
            BasePoint sp = new FilteredElementCollector(doc)
                .OfClass(typeof(BasePoint))
                .Cast<BasePoint>()
                .FirstOrDefault(e => e.IsShared);

            var pbpInfo = pbp != null ? GetBasePointInfo(pbp) : null;
            var spInfo = sp != null ? GetBasePointInfo(sp) : null;

            // Project Location (for Angle to True North)
            ProjectLocation projectLoc = doc.ActiveProjectLocation;
            double angleToTrueNorth = 0.0;
            if (projectLoc != null)
            {
                // The angle between Project North and True North
                // Accessing the Project Position
                ProjectPosition pos = projectLoc.GetProjectPosition(XYZ.Zero);
                angleToTrueNorth = pos.Angle * (180.0 / Math.PI);
            }

            return new 
            { 
                project_base_point = pbpInfo,
                survey_point = spInfo,
                angle_to_true_north = angleToTrueNorth
            };
        }

        private static object GetBasePointInfo(BasePoint bp)
        {
            double ns = bp.GetParameterDouble(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM);
            double ew = bp.GetParameterDouble(BuiltInParameter.BASEPOINT_EASTWEST_PARAM);
            double elev = bp.GetParameterDouble(BuiltInParameter.BASEPOINT_ELEVATION_PARAM);
            double angle = bp.GetParameterDouble(BuiltInParameter.BASEPOINT_ANGLETON_PARAM);

            return new 
            { 
                n_s = ns,
                e_w = ew,
                elevation = elev,
                angle_to_north = angle * (180.0 / Math.PI)
            };
        }
    }
}
