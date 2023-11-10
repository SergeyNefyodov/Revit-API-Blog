using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstRevitPlugin.FailuresProcessing
{
    public static class TransactionHandler
    {
        public static void SetWarningResolver(Transaction transaction)
        {
            FailureHandlingOptions failOptions = transaction.GetFailureHandlingOptions();
            failOptions.SetFailuresPreprocessor(new WarningResolver());
            transaction.SetFailureHandlingOptions(failOptions);
        }
    }
}
