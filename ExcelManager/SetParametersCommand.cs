using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ClosedXML.Excel;

namespace FirstRevitPlugin.ExcelManager
{
    [Autodesk.Revit.Attributes.TransactionAttribute(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class SetParametersCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var document = commandData.Application.ActiveUIDocument.Document;
            var dictionary = GetValues();
            SetValues(document, dictionary);

            return Result.Succeeded;
        }

        private void SetValues(Document document, Dictionary<string, string> dictionary)
        {
            var elements = new FilteredElementCollector(document)
                .WhereElementIsNotElementType()
                .ToElements();

            using var transaction = new Transaction(document, "Set parameters");
            transaction.Start();
            foreach (var element in elements)
            {
                var category = element.Category?.Name ?? string.Empty;
                if (!dictionary.TryGetValue(category, out var value)) continue;
                
                element.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set(value);
            }

            transaction.Commit();
        }

        private Dictionary<string, string> GetValues()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream("FirstRevitPlugin.ExcelManager.Resources.table.xlsx");
            
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet is null) return [];

            var rows = sheet.RowsUsed().ToArray();
            var dictionary = new Dictionary<string, string>();
            foreach (var row in rows)
            {
                var cells = row.CellsUsed().ToArray();
                if (cells.Length != 2) continue;

                dictionary.Add(cells[0].Value.ToString(), cells[1].Value.ToString());
            }

            return dictionary;
        }
    }
}