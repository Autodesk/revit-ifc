using Autodesk.UI.Windows;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;
using Autodesk.Revit.DB;
using System.Collections.ObjectModel;
using System.Linq;

namespace BIM.IFC.Export.UI
{

   public class UserDefinedPropertyInfo
   {
      /// <summary>
      /// Whether the Revit property is checked for export.
      /// </summary>
      public bool ExportFlag { get; set; }

      /// <summary>
      /// The Revit property name.
      /// </summary>
      public string RevitPropertyName { get; set; }

      /// <summary>
      /// The IFC property name.
      /// </summary>
      public string IFCPropertyName { get; set; }

      /// <summary>
      /// The type of entity to which the property is applied.
      /// </summary>
      public PropertyApplicationEnum PropertyApplicationType { get; set; }
   }


   /// <summary>
   /// Interaction logic for IFCUserDefinedPropertyMapping.xaml
   /// </summary>
   public partial class IFCUserDefinedPropertyMapping : ChildWindow
   {
      List<UserDefinedPropertyInfo> userDefInfos = new();

      ObservableCollection<string> selectedIFCEntities = new();

      public IFCUserDefinedPropertyMapping()
      {
         InitializeComponent();
         DataContext = this;

         listBox_PropertySets.Items.Add("Test Property Set 1");
         listBox_PropertySets.Items.Add("Test Property Set 2");
         listBox_PropertySets.Items.Add("Test Property Set 3");
         listBox_PropertySets.Items.Add("Test Property Set 4");
         listBox_PropertySets.Items.Add("Test Property Set 5");

         userDefInfos.Add(new UserDefinedPropertyInfo() { ExportFlag = true, RevitPropertyName = "Revit Property 1", IFCPropertyName = "IFC Property 1", PropertyApplicationType = PropertyApplicationEnum.Instance });
         userDefInfos.Add(new UserDefinedPropertyInfo() { ExportFlag = false, RevitPropertyName = "Revit Property 2", IFCPropertyName = "IFC Property 2", PropertyApplicationType = PropertyApplicationEnum.Type });
         userDefInfos.Add(new UserDefinedPropertyInfo() { ExportFlag = true, RevitPropertyName = "Revit Property 3", IFCPropertyName = "IFC Property 3", PropertyApplicationType = PropertyApplicationEnum.Instance });
         userDefInfos.Add(new UserDefinedPropertyInfo() { ExportFlag = true, RevitPropertyName = "Revit Property 4", IFCPropertyName = "IFC Property 4", PropertyApplicationType = PropertyApplicationEnum.Type });
         userDefInfos.Add(new UserDefinedPropertyInfo() { ExportFlag = false, RevitPropertyName = "Revit Property 5", IFCPropertyName = "IFC Property 5", PropertyApplicationType = PropertyApplicationEnum.Instance });
          
         dataGrid_UserDefinedProperties.ItemsSource = userDefInfos;
         listBox_SelectedIFCEntities.ItemsSource = selectedIFCEntities;
      }


      private void button_Add_Click(object sender, RoutedEventArgs e)
      {

      }


      private void button_Import_Click(object sender, RoutedEventArgs e)
      {
         FileOpenDialog openDialog = new FileOpenDialog(Properties.Resources.IFCPropertyMappingSetupsFilter);
         openDialog.Title = Properties.Resources.ImportIFCPropertyMappingDialogName;

         if (openDialog.Show() == ItemSelectionDialogResult.Confirmed)
         {
            // TODO.
         }
      }

      private void button_Copy_Click(object sender, RoutedEventArgs e)
      {

      }

      private void button_Save_Click(object sender, RoutedEventArgs e)
      {

      }

      private void button_Export_Click(object sender, RoutedEventArgs e)
      {
         FileSaveDialog saveDialog = new FileSaveDialog(Properties.Resources.IFCPropertyMappingSetupsFilter);
         saveDialog.Title = Properties.Resources.ExportIFCPropertyMappingDialogName;
         
         if (saveDialog.Show() == ItemSelectionDialogResult.Confirmed)
         {
            // TODO.
         }
      }

      private void button_Delete_Click(object sender, RoutedEventArgs e)
      {

      }

      private void button_Ok_Click(object sender, RoutedEventArgs e)
      {
         Close();
      }

      private void button_Cancel_Click(object sender, RoutedEventArgs e)
      {
         Close();
      }

      private void textBox_Search_TextChanged(object sender, TextChangedEventArgs e)
      {

      }

      private void listBox_MappingTemplates_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {

      }

      private void button_SelectEnitites_Click(object sender, RoutedEventArgs e)
      {
         // TODO: do we want to allow schema selection?
         IFCVersion ifcVersion = IFCVersion.IFC4;

         EntityTree entityTree = new EntityTree(ifcVersion,
            GetSelectedEnititesString(), desc: "", singleNodeSelection: false, EntityTree.SelectionStrategyType.Inclusion,
            synchronizeSelectionWithType: false)
         {
            Owner = this,
            Title = Properties.Resources.IFCEntitySelection
         };
         entityTree.PredefinedTypeTreeView.Visibility = System.Windows.Visibility.Hidden;

         bool? ret = entityTree.ShowDialog();

         if (ret.HasValue && ret.Value == true)
         {
            selectedIFCEntities.Clear();
            foreach (string entity in entityTree.GetSelectedEntity().Split(';'))
            {
               selectedIFCEntities.Add(entity);
            }
         }
      }

      private string GetSelectedEnititesString()
      {
         string selectedEntities = "";
         foreach (string entity in selectedIFCEntities)
         {
            selectedEntities += entity + ";";
         }
         return selectedEntities;
      }

      private void listBox_SelectedIFCEntities_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {

      }
   }

   /// <summary>
   /// Extracts the row index from the DataGridRow item.
   /// It is used too set AutomationId for valid journal playback.
   /// </summary>
   public class RowIndexValueConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter,
                            System.Globalization.CultureInfo culture)
      {
         DependencyObject item = (DependencyObject)value;
         ItemsControl ic = ItemsControl.ItemsControlFromItemContainer(item);

         return ic.ItemContainerGenerator.IndexFromContainer(item);
      }

      public object ConvertBack(object value, Type targetType, object parameter,
                                System.Globalization.CultureInfo culture)
      {
         return null;
      }
   }

   public enum PropertyApplicationEnum
   {
      [Description("Instance")]
      Instance,
      [Description("Type")]
      Type
   }

}
