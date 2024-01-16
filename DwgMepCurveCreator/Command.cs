using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FirstRevitPlugin.DwgMepCurveCreator
{
    [Autodesk.Revit.Attributes.TransactionAttribute(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    internal class Command : IExternalCommand
    {
        static AddInId addinId = new AddInId(new Guid("7AFAD35B-8F8D-4E0C-B30E-003FF34954B2"));
        private Document document;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            document = commandData.Application.ActiveUIDocument.Document;
            var importInstance = new FilteredElementCollector(document)
                .OfClass(typeof(ImportInstance))
                .Cast<ImportInstance>()
                .First();

            var options = new Options()
            {
                View = document.ActiveView
            };

            GeometryInstance geometryInstance = null;
            var geometryElement = importInstance.get_Geometry(options);
            foreach (var geometryObj in geometryElement)
            {
                if (geometryObj is GeometryInstance instance)
                {
                    geometryInstance = instance;
                    break;
                }
            }

            using (TransactionGroup transactionGroup = new TransactionGroup(document, "Создание труб"))
            {
                transactionGroup.Start();
                foreach (var line in geometryInstance.GetInstanceGeometry())
                {
                    if (line is PolyLine polyLine)
                    {
                        var style = (GraphicsStyle)document.GetElement(polyLine.GraphicsStyleId);
                        if (style.GraphicsStyleCategory.Name == "Pipes")
                        {
                            CreatePipes(polyLine);
                        }
                    }
                }
                transactionGroup.Assimilate();
            }

            return Result.Succeeded;
        }

        private void CreatePipes(PolyLine polyLine)
        {
            var points = polyLine.GetCoordinates().ToList();
            var systemType = new FilteredElementCollector(document).
                OfClass(typeof(PipingSystemType)).
                First().Id;
            var pipeType = new FilteredElementCollector(document).
                OfClass(typeof(PipeType)).
                First().Id;
            var level = new FilteredElementCollector(document).
                OfClass(typeof(Level)).
                First().Id;
            var pipes = new List<Pipe>();
            using (Transaction transaction = new Transaction(document, "Создание труб"))
            {
                transaction.Start();
                for (var i = 1; i < points.Count; i++)
                {
                    var point0 = points[i - 1];
                    var point1 = points[i];

                    var pipe = Pipe.Create(document, systemType, pipeType, level, point1, point0);
                    pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)
                        .Set(UnitUtils.ConvertToInternalUnits(10, UnitTypeId.Millimeters));
                    pipes.Add(pipe);

                }
                for (var i = 1; i < pipes.Count; i++)
                {
                    var pipe0 = pipes[i - 1];
                    var pipe1 = pipes[i];
                    var connectors = FindNearestConnectors(pipe0, pipe1);
                    try
                    {
                        document.Create.NewElbowFitting(connectors[0], connectors[1]);
                    }
                    catch { }
                }
                transaction.Commit();
            }
        }

        private Connector[] FindNearestConnectors(MEPCurve curve0, MEPCurve curve1)
        {
            Connector c1 = null;
            Connector c2 = null;
            foreach (Connector point0 in curve0.ConnectorManager.Connectors)
            {
                foreach (Connector point1 in curve1.ConnectorManager.Connectors)
                {
                    if (Math.Abs(point0.Origin.DistanceTo(point1.Origin)) < 0.001)
                    {
                        c1 = point0;
                        c2 = point1;
                    }
                }
            }
            return new Connector[] { c1, c2 };
        }
    }
}
