using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Revit.IFC.Common.Utility;
using Autodesk.UI.Windows;

namespace BIM.IFC.Export.UI
{ 
   /// <summary>
   /// Interaction logic for PredefinedTypeSelection.xaml
   /// </summary>
   public partial class PredefinedTypeSelection : ChildWindow
   {
      TreeView m_TreeView = new TreeView();
      TreeViewItem prevSelectedItem = null;

      /// <summary>
      /// Initialization for the class for PredefinedType selection without specific IFCVersion
      /// </summary>
      /// <param name="ifcEntitySelected">selected entity for predefinedtype</param>
      public PredefinedTypeSelection(string ifcEntitySelected)
      {
         string ifcSchema = IfcSchemaEntityTree.SchemaName(IFCVersion.Default);
         InitializePreDefinedTypeSelection(ifcSchema, ifcEntitySelected);
      }

      /// <summary>
      /// Initialization for the class for PredefinedType selection
      /// </summary>
      /// <param name="ifcVersion">the IFC version selected. If it is specific, it will be locked</param>
      /// <param name="ifcEntitySelected">the ifc entity to find the predefinedtype</param>
      public PredefinedTypeSelection(IFCVersion ifcVersion, string ifcEntitySelected)
      {
         string ifcSchema = IfcSchemaEntityTree.SchemaName(ifcVersion);
         InitializePreDefinedTypeSelection(ifcSchema, ifcEntitySelected);
      }

      /// <summary>
      /// Initialization for the class for PredefinedType selection using the IFC schema file name (must be correct or it will return null)
      /// </summary>
      /// <param name="ifcSchema">the supported schema file (without extension). Currently: IFC4, IFC2X3_TC1, IFC2X2_ADD1</param>
      /// <param name="ifcEntitySelected">the ifc entity to find the predefinedtype</param>
      public PredefinedTypeSelection(string ifcSchema, string ifcEntitySelected)
      {
         InitializePreDefinedTypeSelection(ifcSchema, ifcEntitySelected);
      }

      void InitializePreDefinedTypeSelection(string ifcSchema, string ifcEntitySelected)
      {
         if (string.IsNullOrEmpty(ifcEntitySelected))
            return;
         
         InitializeComponent();
         IfcSchemaEntityTree ifcEntityTree = IfcSchemaEntityTree.GetEntityDictFor(ifcSchema);

         IList<string> predefinedTypeList = IfcSchemaEntityTree.GetPredefinedTypeList(ifcEntityTree, ifcEntitySelected);

         if (predefinedTypeList != null && predefinedTypeList.Count > 0)
         {
            TreeViewItem ifcEntityViewItem = new TreeViewItem();
            ifcEntityViewItem.Name = ifcEntitySelected;
            ifcEntityViewItem.Header = ifcEntitySelected + ".PREDEFINEDTYPE";
            ifcEntityViewItem.IsExpanded = true;
            m_TreeView.Items.Add(ifcEntityViewItem);

            foreach(string predefItem in predefinedTypeList)
            {
               TreeViewItem childNode = new TreeViewItem();
               RadioButton childNodeItem = new RadioButton();
               childNode.Name = predefItem;
               childNodeItem.Name = predefItem;
               childNodeItem.Content = predefItem;
               childNodeItem.Checked += new RoutedEventHandler(PredefSelected_Checked);
               childNodeItem.Unchecked += new RoutedEventHandler(PredefSelected_Unchecked);
               childNode.Header = childNodeItem;
               ifcEntityViewItem.Items.Add(childNode);
            }
         }
         TreeView_PredefinedType.ItemsSource = m_TreeView.Items;
      }

      void PredefSelected_Checked(object sender, RoutedEventArgs e)
      {
         RadioButton cbElem = sender as RadioButton;
         if (cbElem != null)
         {
            // Clear previously selected item first
            if (prevSelectedItem != null)
            {
               RadioButton prevCBSel = prevSelectedItem.Header as RadioButton;
               if (prevCBSel != null)
                  prevCBSel.IsChecked = false;
            }
            cbElem.IsChecked = true;
            prevSelectedItem = cbElem.Parent as TreeViewItem;
         }
      }

      void PredefSelected_Unchecked(object sender, RoutedEventArgs e)
      {
         RadioButton cbElem = sender as RadioButton;
         if (cbElem != null)
            cbElem.IsChecked = false;
      }


      private void Button_Cancel_Click(object sender, RoutedEventArgs e)
      {
         DialogResult = false;
         Close();
      }

      private void Button_OK_Click(object sender, RoutedEventArgs e)
      {
         DialogResult = true;
         Close();
      }

      /// <summary>
      /// Get the selected Predefined Type
      /// </summary>
      /// <returns>the string containinf the predefinedtype</returns>
      public string GetSelectedPredefinedType()
      {
         if (!DialogResult.HasValue || DialogResult.Value == false)
            return null;

         foreach (TreeViewItem tvi in m_TreeView.Items)
         {
            foreach (TreeViewItem predefItem in tvi.Items)
            {
               RadioButton cbElem = predefItem.Header as RadioButton;
               if (cbElem != null && cbElem.IsChecked == true)
                  return cbElem.Name;
            }
         }
         return null;
      }

      /// <summary>
      /// Check that the selected entity actually has predefinedType
      /// </summary>
      /// <returns>true if it has predefinedtype</returns>
      public bool HasPredefinedType()
      {
         return m_TreeView.Items.Count > 0;
      }
   }
}
