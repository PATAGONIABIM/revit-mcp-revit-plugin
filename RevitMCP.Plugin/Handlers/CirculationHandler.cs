using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace RevitMCP.Plugin.Handlers
{
    public static class CirculationHandler
    {
        public static object CreateStairs(Document doc, dynamic args)
        {
            // Stairs creation requires its own transaction management via StairsEditScope
            // The helper MUST be called when NO transaction is active
            
            XYZ p1 = new XYZ((double)args.p1.x, (double)args.p1.y, (double)args.p1.z);
            XYZ p2 = new XYZ((double)args.p2.x, (double)args.p2.y, (double)args.p2.z);
            ElementId bottomLevelId = new ElementId(int.Parse((string)args.bottom_level_id));
            ElementId topLevelId = new ElementId(int.Parse((string)args.top_level_id));

            ElementId newStairsId = ElementId.InvalidElementId;

            // StairsEditScope handles the transaction group
            using (StairsEditScope newStairsScope = new StairsEditScope(doc, "Create Stairs"))
            {
                newStairsId = newStairsScope.Start(bottomLevelId, topLevelId);

                using (Transaction t = new Transaction(doc, "Add Stairs Run"))
                {
                    t.Start();
                    
                    // Create a straight run
                    // Calculate simplified width/depth? Defaulting.
                    // StairsRun.CreateStraightRun(doc, stairsId, path, justification)
                    
                    // define path
                    Line path = Line.CreateBound(p1, p2);
                    
                    StairsRun.CreateStraightRun(doc, newStairsId, path, StairsRunJustification.Center);
                    
                    t.Commit();
                }

                newStairsScope.Commit(new FailuresPreprocessor());
            }

            return new { id = newStairsId.ToString(), status = "Created Stairs" };
        }
    }

    // Reuse the warning swallower if needed for StairsScope commit
    public class FailuresPreprocessor : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            failuresAccessor.DeleteAllWarnings();
            return FailureProcessingResult.Continue;
        }
    }
}
