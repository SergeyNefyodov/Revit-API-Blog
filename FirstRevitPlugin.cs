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
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using FirstRevitPlugin.Extensions;

namespace FirstRevitPlugin
{
    [Autodesk.Revit.Attributes.TransactionAttribute(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class FirstRevitCommand : IExternalCommand
    {
        static AddInId addinId = new AddInId(new Guid("0F296157-A2DC-4532-BB1B-6D6D3462F15A"));
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (RevitAPI.UiApplication == null)
            {
                RevitAPI.Initialize(commandData);
            }
            ElementId id = null;
            id.SetTypeParameter("Name", "Value");
            DivideWallsIntoParts(commandData);
            DivideParts(commandData);
            //DrawArcWall(commandData);
            //Numerate()
            //ManageParameters(commandData);
            //FindIntersection(commandData);
            return Result.Succeeded;
        }

        private static void DivideWallsIntoParts(ExternalCommandData commandData)
        {
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = commandData.Application.ActiveUIDocument.Document;
            var wallIds = new FilteredElementCollector(document).
                OfClass(typeof(Wall)).
                ToElementIds();

            using (Transaction transaction = new Transaction(document, "Создание частей"))
            {
                transaction.Start();
                foreach (var id in wallIds)
                {
                    var array = new ElementId[] { id };
                    if (PartUtils.AreElementsValidForCreateParts(document, array))
                    {
                        PartUtils.CreateParts(document, array);
                    }
                }
                transaction.Commit();
            }
        }

        private static void DivideParts(ExternalCommandData commandData)
        {
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = commandData.Application.ActiveUIDocument.Document;

            var parts = new FilteredElementCollector(document).
                OfClass(typeof(Part)).
                Where(part => part.get_Parameter(BuiltInParameter.DPART_MATERIAL_ID_PARAM).AsValueString() == "Штукатурка").
                Cast<Part>();

            using (Transaction transaction = new Transaction(document, "Разделение частей"))
            {
                transaction.Start();

                foreach (var part in parts)
                {
                    var array = new ElementId[] { part.Id };
                    var wallId = part.GetSourceElementIds().FirstOrDefault().HostElementId;                    
                    {
                        if (document.GetElement(wallId) is Wall wall)
                        {
                            var levelArray = new ElementId[] { wall.LevelId };
                            var horizStep = UnitUtils.ConvertToInternalUnits(1000, UnitTypeId.Millimeters);
                            var vertStep = UnitUtils.ConvertToInternalUnits(500, UnitTypeId.Millimeters);
                            var height = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble();                            

                            var locationCurve = (LocationCurve)wall.Location;
                            var wallLine = (Line)locationCurve.Curve;
                            var length = wallLine.Length;

                            var startPoint = wallLine.GetEndPoint(0);
                            var endPoint = wallLine.GetEndPoint(1);

                            int n = 0; //number of horizontal lines
                            int m = 0; //number of vertical lines

                            var horizDiff = (endPoint - startPoint) * horizStep / (startPoint.DistanceTo(endPoint));
                            startPoint -= horizDiff / 2;
                            endPoint += horizDiff / 2;

                            n = (int)Math.Ceiling(height / vertStep);
                            m = (int)Math.Ceiling(length / horizStep)+1;

                            var curves = new List<Curve>();

                            for (int i = 0; i < m; i++)
                            {
                                var point = startPoint + i * horizDiff-XYZ.BasisZ;
                                curves.Add(Line.CreateBound(point, new XYZ(point.X, point.Y, point.Z + height+4)));
                            }

                            for (int i = 0; i < n; i++)
                            {
                                var point0 = new XYZ(startPoint.X, startPoint.Y, startPoint.Z + i * vertStep);
                                var point1 = new XYZ(endPoint.X, endPoint.Y, endPoint.Z + i * vertStep);
                                curves.Add(Line.CreateBound(point0, point1));
                            }

                            document.Regenerate();
                            var plane = Plane.CreateByThreePoints(curves.First().GetEndPoint(0), curves.Last().GetEndPoint(1),
                                curves.Last().GetEndPoint(0) + XYZ.BasisZ);
                            var sketchPlane = SketchPlane.Create(document, plane);
                            document.Regenerate();

                            PartUtils.DivideParts(document, array, levelArray, curves, sketchPlane.Id);
                        }
                    }
                }

                transaction.Commit();
            }
        }
        private static void DrawArcWall(ExternalCommandData commandData)
        {
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = commandData.Application.ActiveUIDocument.Document;
            var level = new FilteredElementCollector(document).
                OfClass(typeof(Level))
                .First();
            var name = "Витраж";

            var wallType = new FilteredElementCollector(document).
                OfClass(typeof(WallType)).
                FirstOrDefault(type => type.Name == name);

            var point0 = uiDocument.Selection.PickPoint("Укажите начало дуги");
            var point1 = uiDocument.Selection.PickPoint("Укажите конец дуги");
            var point2 = uiDocument.Selection.PickPoint("Укажите точку на дуге");

            var arc = Arc.Create(point0, point1, point2);

            using (Transaction transaction = new Transaction(document, "Создание стены"))
            {
                transaction.Start();
                Wall.Create(document, arc, wallType.Id, level.Id, 3000 / 304.8, 200 / 304.8, false, false);
                transaction.Commit();
            }
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
                var ducts = new FilteredElementCollector(doc).WherePasses(filter).WherePasses(bbFilter).OfClass(typeof(Duct)).ToElements();
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
                while (true)
                {
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
                                break;
                            }
                        }
                    }
                    catch
                    {
                        group.Assimilate();
                        break;
                    }
                }
            }
        }

        private void SetTypeParameter(ElementId id, string parameterName, string value)
        {
            var element = RevitAPI.Document.GetElement(id);
            var typeId = element.GetTypeId();
            var type = RevitAPI.Document.GetElement(typeId);
            var parameter = type.LookupParameter(parameterName);
            if (parameter != null)
            {
                using (var transaction = new Transaction(RevitAPI.Document, $"Set parameter value for type {type.Name}"))
                {
                    transaction.Start();
                    parameter.Set(value);
                    transaction.Commit();
                }
            }
        }
    }
}
