using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstRevitPlugin.Extensions
{
    public static class IdExtensions
    {
        public static void SetTypeParameter(this ElementId id, string parameterName, string value)
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
