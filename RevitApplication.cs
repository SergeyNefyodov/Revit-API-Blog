using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FirstRevitPlugin
{
    internal class RevitApplication : IExternalApplication
    {
        static AddInId addInId = new AddInId(new Guid("5E608034-E054-4A50-9844-8E808BE66244"));        

        public Result OnStartup(UIControlledApplication application)
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;            

            string tabName = "My Custom Tab";
            application.CreateRibbonTab(tabName);
            RibbonPanel ribbonPanel = application.CreateRibbonPanel(tabName, "Автоматизация");
            AddButton(ribbonPanel, "Button 1", assemblyPath, "FirstRevitPlugin.FirstRevitCommand", "Всплывающая подсказка");
            return Result.Succeeded;
        }

        private void AddButton(RibbonPanel ribbonPanel, string buttonName, string path, string linkToCommand, string toolTip)
        {
            PushButtonData buttonData = new PushButtonData(
               buttonName,
               buttonName,
               path,
               linkToCommand);            
            PushButton Button = ribbonPanel.AddItem(buttonData) as PushButton;
            Button.ToolTip = toolTip;
            Button.LargeImage = (ImageSource) new BitmapImage(new Uri(@"/FirstRevitPlugin;component/Resources/Icon.png", UriKind.RelativeOrAbsolute));
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
