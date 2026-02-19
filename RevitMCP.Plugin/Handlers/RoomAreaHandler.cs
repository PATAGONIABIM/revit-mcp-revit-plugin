using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Newtonsoft.Json.Linq;

namespace RevitMCP.Plugin.Handlers
{
    public static class RoomAreaHandler
    {
        public static object CreateRoom(Document doc, dynamic args)
        {
            XYZ location = new XYZ((double)args.location.x, (double)args.location.y, (double)args.location.z);
            ElementId levelId = new ElementId(long.Parse((string)args.level_id));
            Level level = (Level)doc.GetElement(levelId);

            // Create Room
            Room room = doc.Create.NewRoom(level, new UV(location.X, location.Y));
            
            if (args.name != null) room.Name = (string)args.name;
            if (args.number != null) room.Number = (string)args.number;

            return new { id = room.Id.ToString(), name = room.Name, number = room.Number };
        }

        public static object CreateRoomSeparator(Document doc, dynamic args)
        {
            JArray points = (JArray)args.points;
            ElementId levelId = new ElementId(long.Parse((string)args.level_id));
            Level level = (Level)doc.GetElement(levelId);
            
            CurveArray curves = new CurveArray();
            List<XYZ> xyzPoints = new List<XYZ>();
            foreach (var p in points)
            {
                xyzPoints.Add(new XYZ((double)p["x"], (double)p["y"], (double)p["z"]));
            }

            for (int i = 0; i < xyzPoints.Count - 1; i++)
            {
                curves.Append(Line.CreateBound(xyzPoints[i], xyzPoints[i+1]));
            }

            SketchPlane sketchPlane = SketchPlane.Create(doc, level.Id);
            ModelCurveArray array = doc.Create.NewRoomBoundaryLines(sketchPlane, curves, doc.ActiveView);

            List<string> ids = new List<string>();
            foreach (ModelCurve c in array) ids.Add(c.Id.ToString());

            return new { ids = ids };
        }

        public static object TagRoom(Document doc, dynamic args)
        {
            ElementId roomId = new ElementId(long.Parse((string)args.room_id));
            // In Revit 2026, creating room tags might be IndependentTag or RoomTag. 
            // NewRoomTag might be deprecated or changed.
            // Let's use Create.NewRoomTag for simple room tagging if available, or IndependentTag.Create
            
            // Note: Revit 2026 likely uses IndependentTag.Create for everything.
            // But rooms act differently. Let's try `doc.Create.NewRoomTag` (classic) first, if it exists. 
            // Actually, IndependentTag is the modern way.
            
            XYZ location = new XYZ((double)args.location.x, (double)args.location.y, (double)args.location.z);
            ElementId viewId = args.view_id != null ? new ElementId(long.Parse((string)args.view_id)) : doc.ActiveView.Id;
            
            RoomTag tag = doc.Create.NewRoomTag(new LinkElementId(roomId), new UV(location.X, location.Y), viewId);
            
            return new { id = tag.Id.ToString(), name = tag.Name };
        }
    }
}
