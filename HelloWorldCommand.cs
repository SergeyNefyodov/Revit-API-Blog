using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstRevitPlugin
{
    [Autodesk.Revit.Attributes.TransactionAttribute(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class HelloWorldCommand : IExternalCommand
    {
        public static string TextBoxValue {  get; set; } = string.Empty;
        public static string ComboBoxValue { get; set; } = string.Empty;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            TaskDialog.Show("Title", $"ComboBox value is {ComboBoxValue}, TextBoxValue is {TextBoxValue}");
            return Result.Succeeded;
        }
    }
}
