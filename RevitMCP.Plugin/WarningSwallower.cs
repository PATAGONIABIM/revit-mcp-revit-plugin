using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitMCP.Plugin
{
    public class WarningSwallower : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            IList<FailureMessageAccessor> failures = failuresAccessor.GetFailureMessages();
            
            foreach (FailureMessageAccessor failure in failures)
            {
                // Verify if it is a warning
                if (failure.GetSeverity() == FailureSeverity.Warning)
                {
                    // Dismiss the warning
                    failuresAccessor.DeleteWarning(failure);
                }
                else
                {
                    // If it's an error, we can't just delete it. 
                    // We might need to resolve it or let it fail.
                    // For now, let's try to resolve default failures if possible, or just proceed.
                    // If we return Continue, it might crash if error isn't resolved.
                    // But for now, we focus on swallowing WARNINGS which are the blockers.
                }
            }

            return FailureProcessingResult.Continue;
        }
    }
}
