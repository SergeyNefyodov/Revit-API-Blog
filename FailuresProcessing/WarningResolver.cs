using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstRevitPlugin.FailuresProcessing
{
    public class WarningResolver : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
        {
            IList<FailureMessageAccessor> failures = accessor.GetFailureMessages();
            foreach (FailureMessageAccessor failureMessageAccessor in failures)
            {
                if (failureMessageAccessor.HasResolutionOfType(FailureResolutionType.DetachElements))
                {
                    failureMessageAccessor.SetCurrentResolutionType(FailureResolutionType.DetachElements);
                    var id = failureMessageAccessor.GetFailureDefinitionId();
                    var failureSeverity = accessor.GetSeverity();
                    if (failureSeverity == FailureSeverity.Error || failureSeverity == FailureSeverity.Warning)
                    {
                        accessor.ResolveFailure(failureMessageAccessor);
                        return FailureProcessingResult.ProceedWithCommit;
                    }
                    else
                    {
                        return FailureProcessingResult.Continue;
                    }
                }
            }
            return FailureProcessingResult.Continue;
        }
    }
}
