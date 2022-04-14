using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Revit.IFC.Common.Utility;
using Autodesk.UI.Windows;

namespace Revit.IFC.EntityTree
{ 
   /// <summary>
   /// Interaction logic for PredefinedTypeSelection.xaml
   /// </summary>
   public partial class PredefinedTypeSelection : ChildWindow
   {
      TreeView m_TreeView = new TreeView();
      TreeViewItem prevSelectedItem = null;

      /// <summary>
      /// Initialization for the class for PredefinedType selection
      /// </summary>
      /// <param name="ifcVersion">the IFC version selected. If it is specific, it will be locked</param>
      /// <param name="ifcEntitySelected">the ifc entity to find the predefinedtype</param>
      public PredefinedTypeSelection(IFCVersion ifcVersion, string ifcEntitySelected)
      {
         if (string.IsNullOrEmpty(ifcEntitySelected))
            return;

         InitializeComponent();
         IfcSchemaEntityTree ifcEntityTree = IfcSchemaEntityTree.GetEntityDictFor(ifcVersion);

         IList<string> predefinedTypeList = IfcSchemaEntityTree.GetPredefinedTypeList(ifcVersion, ifcEntitySelected);

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
               CheckBox childNodeItem = new CheckBox();
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

      /// <summary>
      /// Override OK button content from caller since DLL does not seem to allow static resource value
      /// </summary>
      public string OKLabelOverride
      {
         set
         {
            Button_OK.Content = value;
         }
      }

      /// <summary>
      /// Override Cancel button content from caller since DLL does not seem to allow static resource value
      /// </summary>
      public string CancelLabelOverride
      {
         set
         {
            Button_Cancel.Content = value;
         }
      }

      /// <summary>
      /// Override Title from caller since DLL does not seem to allow static resource value
      /// </summary>
      public string TitleLabelOverride
      {
         set
         {
            Title = value;
         }
      }

      void PredefSelected_Checked(object sender, RoutedEventArgs e)
      {
         //ClearAllChecked();
         CheckBox cbElem = sender as CheckBox;
         if (cbElem != null)
         {
            // Clear previously selected item first
            if (prevSelectedItem != null)
            {
               CheckBox prevCBSel = prevSelectedItem.Header as CheckBox;
               if (prevCBSel != null)
                  prevCBSel.IsChecked = false;
            }
            cbElem.IsChecked = true;
            prevSelectedItem = cbElem.Parent as TreeViewItem;
         }
      }

      void PredefSelected_Unchecked(object sender, RoutedEventArgs e)
      {
         CheckBox cbElem = sender as CheckBox;
         if (cbElem != null)
            cbElem.IsChecked = false;
      }


      private void Button_Cancel_Click(object sender, RoutedEventArgs e)
      {
         Close();  
      }

      private void Button_OK_Click(object sender, RoutedEventArgs e)
      {
         Close();
      }

      /// <summary>
      /// Get the selected Predefined Type
      /// </summary>
      /// <returns>the string containinf the predefinedtype</returns>
      public string GetSelectedPredefinedType()
      {
         foreach (TreeViewItem tvi in m_TreeView.Items)
         {
            foreach (TreeViewItem predefItem in tvi.Items)
            {
               CheckBox cbElem = predefItem.Header as CheckBox;
               if (cbElem != null && cbElem.IsChecked == true)
                  return cbElem.Name;
            }
         }
         return null;
      }
   }
}
