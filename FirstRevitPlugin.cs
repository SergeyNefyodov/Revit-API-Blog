using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FirstRevitPlugin
{
    [Autodesk.Revit.Attributes.TransactionAttribute(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class FirstRevitCommand : IExternalCommand
    {
        static AddInId addinId = new AddInId(new Guid("0F296157-A2DC-4532-BB1B-6D6D3462F15A"));
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            var walls = new FilteredElementCollector(doc)
                .OfClass(typeof(Wall))
                .ToElements()
                .Cast<Wall>();

            string filename = $@"C:\Users\{Environment.UserName}\Documents\Revit API Blog\Data.txt";
            if (File.Exists(filename)) 
            {
                File.Delete(filename);
            }
            using (StreamWriter writer = new StreamWriter(filename))
            {
                foreach (Wall wall in walls)
                {
                    var lengthParam = wall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
                    string data = $"Element ID: {wall.Id.ToString()}, Type name: {wall.Name}, Length: {lengthParam.AsValueString()}";
                    writer.WriteLine(data);
                }
                writer.Close();
            }

            return Result.Succeeded;
        }

        public void FindIntersection(ExternalCommandData commandData)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            uidoc.Selection.PickObject(ObjectType.Element);
            int counter = 0;
            string result = "";
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var walls = new FilteredElementCollector(doc)
                .OfClass(typeof(Wall))
                .ToElements();
            foreach (Wall wall in walls)
            {
                var bb = wall.get_BoundingBox(null);
                Outline outline = new Outline(bb.Min, bb.Max);
                BoundingBoxIntersectsFilter bbFilter = new BoundingBoxIntersectsFilter(outline);
                ElementIntersectsElementFilter filter = new ElementIntersectsElementFilter(wall);
                var ducts = new FilteredElementCollector(doc).OfClass(typeof(Duct)).WherePasses(bbFilter).WherePasses(filter).ToElements();
                foreach (var el in ducts)
                {
                    result += wall.Id.ToString() + ";" + el.Id.ToString() + Environment.NewLine;
                    counter++;
                }
            }
            stopwatch.Stop();
            TimeSpan elapsedTime = stopwatch.Elapsed;
            TaskDialog.Show("Время работы", elapsedTime.TotalSeconds.ToString());
            TaskDialog.Show("Найдено пересечений " + counter.ToString(), result);

            double meters = 1000;
            double feet = 20;
            UnitUtils.ConvertFromInternalUnits(feet, UnitTypeId.Millimeters); // из футов в метры
            UnitUtils.ConvertToInternalUnits(meters, UnitTypeId.Millimeters); // из метров во внутренние единицы (футы)

        }
    }
}
