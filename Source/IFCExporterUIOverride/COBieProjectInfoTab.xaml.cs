using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Revit.IFC.Common.Utility;
using Autodesk.Revit.DB;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Interaction logic for COBieProjectInfoTab.xaml
   /// </summary>
   public partial class COBieProjectInfoTab : UserControl
   {
      COBieProjectInfo projectInfo;
      public string ProjectInfoStr
      {
         get
         {
            PopulateProjectInfo();
            return projectInfo.ToJsonString();
         }
      }

      public COBieProjectInfoTab(string projInfoStr)
      {
         InitializeComponent();

         ProjectInfo rvtProjectInfo = IFCCommandOverrideApplication.TheDocument.ProjectInformation;

         if (!string.IsNullOrEmpty(projInfoStr))
         {
            projectInfo = new COBieProjectInfo(projInfoStr);
         }
         else
         {
            projectInfo = new COBieProjectInfo();
         }

         if (string.IsNullOrEmpty(projectInfo.BuildingName_Number))
            projectInfo.BuildingName_Number = rvtProjectInfo.Number;

         if (string.IsNullOrEmpty(projectInfo.BuildingDescription))
            projectInfo.BuildingDescription = rvtProjectInfo.BuildingName;

         if (string.IsNullOrEmpty(projectInfo.ProjectName))
            projectInfo.ProjectName = rvtProjectInfo.Name;

         if (string.IsNullOrEmpty(projectInfo.ProjectPhase))
            projectInfo.ProjectPhase = rvtProjectInfo.Status;

         PopulatePage();
      }

      private void button_Source_Click(object sender, RoutedEventArgs e)
      {

      }

      private void textBox_TypeSource_TextChanged(object sender, TextChangedEventArgs e)
      {

      }

      private void PopulateProjectInfo()
      {
         projectInfo.BuildingName_Number = textBox_BuildingName.Text;
         projectInfo.BuildingType = buildingTypeVisited? textBox_BuildingType.Text : "";
         projectInfo.BuildingDescription = textBox_BuildingDesc.Text;
         projectInfo.ProjectName = textBox_ProjectName.Text;
         projectInfo.ProjectDescription = textBox_ProjectDesc.Text;
         projectInfo.ProjectPhase = textBox_ProjectPhase.Text;
         projectInfo.SiteLocation = textBox_SiteLocation.Text;
         projectInfo.SiteDescription = textBox_SiteDesc.Text;
      }

      private void PopulatePage()
      {
         textBox_BuildingName.Text = projectInfo.BuildingName_Number;
         if (!string.IsNullOrEmpty(projectInfo.BuildingType))
         {
            textBox_BuildingType.Text = projectInfo.BuildingType;
            textBox_BuildingType.GotFocus -= textBox_BuildingType_GotFocus;
            textBox_BuildingType.FontStyle = FontStyles.Normal;
            textBox_BuildingType.FontWeight = FontWeights.Normal;
            buildingTypeVisited = true;
         }
         textBox_BuildingDesc.Text = projectInfo.BuildingDescription;
         textBox_ProjectName.Text = projectInfo.ProjectName;
         textBox_ProjectDesc.Text = projectInfo.ProjectDescription;
         textBox_ProjectPhase.Text = projectInfo.ProjectPhase;
         textBox_SiteLocation.Text = projectInfo.SiteLocation;
         textBox_SiteDesc.Text = projectInfo.SiteDescription;

      }

      private void textBox_BuildingType_TextChanged(object sender, TextChangedEventArgs e)
      {

      }

      bool buildingTypeVisited = false;
      private void textBox_BuildingType_GotFocus(object sender, RoutedEventArgs e)
      {
         TextBox tb = sender as TextBox;
         tb.Text = string.Empty;
         tb.GotFocus -= textBox_BuildingType_GotFocus;
         tb.FontStyle = FontStyles.Normal;
         tb.FontWeight = FontWeights.Normal;
         buildingTypeVisited = true;
      }
   }
}
