using Autodesk.Revit.ApplicationServices;
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
            Element el = null;
            double d = 0;
            d = el.get_Parameter(BuiltInParameter.SSE_POINT_ElEVATION).AsDouble(); // будет исключение
            var parameter = el.get_Parameter(BuiltInParameter.SSE_POINT_ElEVATION);
            if (parameter != null)
                d = parameter.AsDouble(); // тут всё окей
            else
                return Result.Failed;
            return Result.Succeeded;
        }
        private static void ManageParameters(ExternalCommandData commandData)
        {
            Application application = commandData.Application.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            string parameterName1 = "SN_Параметр 1";
            string parameterName2 = "SN_Параметр 2";
            string parameterName3 = "SN_Параметр 3";

            var parametersFile = application.OpenSharedParameterFile();
            Definition def2 = FindDefinition(parameterName2, parametersFile);
            Definition def3 = FindDefinition(parameterName3, parametersFile);
            var sharedParameters = new FilteredElementCollector(doc).OfClass(typeof(SharedParameterElement)).ToElements().Cast<SharedParameterElement>();
            var bindings = doc.ParameterBindings;
            CategorySet categorySet = CreateFullCategorySet(doc);
            InstanceBinding binding = new InstanceBinding(categorySet);
            using (Transaction t = new Transaction(doc, "Изменение категорий параметра"))
            {
                t.Start();
                foreach (var parameter in sharedParameters)
                {
                    if (parameter.GetDefinition().Name == parameterName1)
                    {
                        bindings.ReInsert(parameter.GetDefinition(), binding);
                        parameter.GetDefinition().SetAllowVaryBetweenGroups(doc, true);
                    }
                }
                var instanceBinding = new InstanceBinding(categorySet);
                var typeBinding = new TypeBinding(categorySet);
                bindings.Insert(def2, instanceBinding);
                bindings.Insert(def3, typeBinding);
                t.Commit();
            }
        }
        private static Definition FindDefinition(string parameterName, DefinitionFile parametersFile)
        {            
            foreach (var definitionGroup in parametersFile.Groups)
            {
                foreach (ExternalDefinition definition in definitionGroup.Definitions)
                {
                    if (definition.Name == parameterName) return definition;                   
                }
            }

            return null;
        }

        private static CategorySet CreateFullCategorySet(Document doc)
        {
            var categorySet = new CategorySet();
            foreach (Category category in doc.Settings.Categories)
            {
                if (category.AllowsBoundParameters)
                {
                    categorySet.Insert(category);
                }
                else continue;
                if (category.SubCategories.IsEmpty == false)
                {
                    foreach (var value in category.SubCategories)
                    {
                        if (value is Category subCategory)
                        {
                            if (subCategory.AllowsBoundParameters) categorySet.Insert(subCategory);
                        }
                    }
                }
            }

            return categorySet;
        }

        private static void WriteToFile(ExternalCommandData commandData)
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
        }

        private void Numerate(ExternalCommandData commandData)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            int i = 1;
            string prefix = "PS"; // поменяйте на свой или оставьте пустым
            string parameterName = "Марка"; // выберите свой текстовый параметр
            using (TransactionGroup group = new TransactionGroup(doc, "Нумерация элементов")) 
            {
                group.Start();
                try
                {
                    using (Transaction t = new Transaction(doc, "Нумерация элементов"))
                    {
                        t.Start();
                        Reference reference = uidoc.Selection.PickObject(ObjectType.Element, $"Выберите элемент {i}");
                        Parameter parameter = doc.GetElement(reference).LookupParameter(parameterName);
                        if (parameter != null)
                        {
                            parameter.Set(prefix + i.ToString());
                            i++;
                            t.Commit();
                        }
                        else
                        {
                            TaskDialog.Show("Ошибка", $"У элемента {reference.ElementId} нет параметра {parameterName})");
                            t.Commit();
                            group.Assimilate();
                        }
                    }                       
                }
                catch
                {
                    group.Assimilate();
                }
            }
        }
    }
}
