using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstRevitPlugin
{
    public class CommandAvailibilityManager : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            foreach (Category category in selectedCategories)
            {
                if (category.BuiltInCategory == BuiltInCategory.OST_Walls)
                    return true;
            }
            return false;
        }
    }
}
