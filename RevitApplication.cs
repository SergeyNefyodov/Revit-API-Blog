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
        private readonly string assemblyPath = Assembly.GetExecutingAssembly().Location;
        public Result OnStartup(UIControlledApplication application)
        {            
            string tabName = "My Custom Tab";
            application.CreateRibbonTab(tabName);
            RibbonPanel ribbonPanel = application.CreateRibbonPanel(tabName, "Автоматизация");

            var button = AddPushButton(ribbonPanel, "Button 1", assemblyPath, "FirstRevitPlugin.FirstRevitCommand", "Всплывающая подсказка");
            button.AvailabilityClassName = $"{nameof(FirstRevitPlugin)}.{nameof(CommandAvailibilityManager)}";

            AddPullDownButton(ribbonPanel, "Список команд");
            AddRadioButtonGroup(ribbonPanel, "Переключатели");
            ribbonPanel.AddSeparator();
            AddSplitButton(ribbonPanel, "Cписок команд \nSplit");
            AddStackedButtons(ribbonPanel, "Small button", assemblyPath, "FirstRevitPlugin.HelloWorldCommand", "Всплывающая подсказка");
            ribbonPanel.AddSlideOut();
            AddComboBox(ribbonPanel);
            AddTextBox(ribbonPanel);


            //application.DialogBoxShowing += Application_DialogBoxShowing;


            return Result.Succeeded;
        }

        private void Application_DialogBoxShowing(object sender, Autodesk.Revit.UI.Events.DialogBoxShowingEventArgs e)
        {
            //if (e.DialogId == "Dialog_API_MacroManager")
            //{
                //e.OverrideResult(2); // it should close macro manager right after its opening, be careful
                TaskDialog.Show("Yes", "You opened a macro manager, cool!");
            //}
        }

        private PushButton AddPushButton(RibbonPanel ribbonPanel, string buttonName, string path, string linkToCommand, string toolTip)
        {
            var buttonData = new PushButtonData(buttonName, buttonName, path, linkToCommand);            
            var button = ribbonPanel.AddItem(buttonData) as PushButton;
            button.ToolTip = toolTip;
            button.LargeImage = (ImageSource) new BitmapImage(new Uri(@"/FirstRevitPlugin;component/Resources/engineer4.png", UriKind.RelativeOrAbsolute));
            return button;
        }

        private void AddStackedButtons(RibbonPanel ribbonPanel, string buttonName, string path, string linkToCommand, string toolTip)
        {
            var list = new List<PushButtonData>();
            for (var i = 0; i<3; i++)
            {
                var buttonData = new PushButtonData(buttonName + $" {i.ToString()}", buttonName + $" {i.ToString()}", path, linkToCommand);
                list.Add(buttonData);
            }
            var result = ribbonPanel.AddStackedItems(list[0], list[1], list[2]);
            foreach (PushButton button in result)
            {
                button.ToolTip = toolTip;
                button.LargeImage = (ImageSource)new BitmapImage(new Uri(@"/FirstRevitPlugin;component/Resources/wheel32.png", UriKind.RelativeOrAbsolute));
                button.Image = (ImageSource)new BitmapImage(new Uri(@"/FirstRevitPlugin;component/Resources/wheel16.png", UriKind.RelativeOrAbsolute));
            }
        }

        private void AddRadioButtonGroup(RibbonPanel ribbonPanel, string name)
        {
            var data = new RadioButtonGroupData(name);
            var item = ribbonPanel.AddItem(data) as RadioButtonGroup;
            AddToggleButton(item, "ToggleButton 1 ", assemblyPath, "FirstRevitPlugin.FirstRevitCommand", "Кнопка-переключатель 1", 1);
            AddToggleButton(item, "ToggleButton 2", assemblyPath, "FirstRevitPlugin.FirstRevitCommand", "Кнопка-переключатель 2", 3);
        }

        private void AddToggleButton(RadioButtonGroup group, string buttonName, string path, string linkToCommand, string toolTip, int i)
        {
            ToggleButtonData data = new ToggleButtonData(buttonName, buttonName, path, linkToCommand);
            var item = group.AddItem(data) as ToggleButton;
            item.ToolTip = toolTip;
            item.LargeImage = (ImageSource) new BitmapImage(
                new Uri(@"/FirstRevitPlugin;component/Resources/engineer"+i.ToString()+".png", UriKind.RelativeOrAbsolute));
        }

        private void AddPullDownButton(RibbonPanel ribbonPanel, string name)
        {
            var data = new PulldownButtonData(name, name);
            FillPullDown(ribbonPanel, data);
        }

        private void AddSplitButton(RibbonPanel ribbonPanel, string name)
        {
            var data = new SplitButtonData(name, name);
            FillPullDown(ribbonPanel, data);
        }

        private void FillPullDown(RibbonPanel ribbonPanel, PulldownButtonData data)
        {
            var item = ribbonPanel.AddItem(data) as PulldownButton;
            AddButtonToPullDownButton(item, "Команда 1", assemblyPath, "FirstRevitPlugin.FirstRevitCommand", "Команда 1 — подсказка");
            AddButtonToPullDownButton(item, "Команда 2", assemblyPath, "FirstRevitPlugin.FirstRevitCommand", "Команда 2 — подсказка");
            AddButtonToPullDownButton(item, "Команда 3", assemblyPath, "FirstRevitPlugin.FirstRevitCommand", "Команда 3 — подсказка");
            item.AddSeparator();
            AddButtonToPullDownButton(item, "Команда 4", assemblyPath, "FirstRevitPlugin.FirstRevitCommand", "Команда 4 — подсказка");
            item.LargeImage = (ImageSource)new BitmapImage(new Uri(@"/FirstRevitPlugin;component/Resources/engineer2.png", UriKind.RelativeOrAbsolute));
        }

        private void AddButtonToPullDownButton (PulldownButton button, string name, string path, string linkToCommand, string toolTip)
        {
            var data = new PushButtonData(name, name, path, linkToCommand);
            var pushButton = button.AddPushButton(data) as PushButton;
            pushButton.ToolTip = toolTip;
            pushButton.Image = (ImageSource)new BitmapImage(new Uri(@"/FirstRevitPlugin;component/Resources/wheel16.png", UriKind.RelativeOrAbsolute));
            pushButton.LargeImage = (ImageSource)new BitmapImage(new Uri(@"/FirstRevitPlugin;component/Resources/wheel32.png", UriKind.RelativeOrAbsolute));
        }

        private void AddComboBox(RibbonPanel ribbonPanel)
        {
            var data = new ComboBoxData("Combo Box");
            var item = ribbonPanel.AddItem(data) as ComboBox;
            AddComboBoxMember(item, "Value 1");
            AddComboBoxMember(item, "Value 2");
            AddComboBoxMember(item, "Value 3");
            item.CurrentChanged += ComboBoxChanged;
            HelloWorldCommand.ComboBoxValue = item.Current.ItemText;
        }        

        private void AddComboBoxMember(ComboBox box, string name)
        {
            var data = new ComboBoxMemberData(name, name);
            var member = box.AddItem(data) as ComboBoxMember;
            member.Image = (ImageSource)new BitmapImage(new Uri(@"/FirstRevitPlugin;component/Resources/wheel16.png", UriKind.RelativeOrAbsolute));
        }

        private void ComboBoxChanged(object sender, Autodesk.Revit.UI.Events.ComboBoxCurrentChangedEventArgs e)
        {
            var box = sender as ComboBox;
            HelloWorldCommand.ComboBoxValue = box.Current.ItemText;
        }

        private void AddTextBox(RibbonPanel ribbonPanel)
        {
            var data = new TextBoxData("Text");
            var item = ribbonPanel.AddItem(data) as TextBox;
            item.EnterPressed += TextBoxChanged;
        }

        private void TextBoxChanged(object sender, Autodesk.Revit.UI.Events.TextBoxEnterPressedEventArgs e)
        {
            var textBox = (TextBox)sender;
            HelloWorldCommand.TextBoxValue = textBox.Value.ToString();            
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
