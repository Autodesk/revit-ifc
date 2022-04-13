using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Autodesk.Revit.DB;
using Revit.IFC.Common.Utility;
using Autodesk.UI.Windows;

namespace Revit.IFC.EntityTree
{
   /// <summary>
   /// Interaction logic for EntityTree.xaml
   /// </summary>
   public partial class EntityTree : ChildWindow
   {
      TreeView m_TreeView = new TreeView();
      IDictionary<string, TreeViewItem> m_TreeViewItemDict = new Dictionary<string, TreeViewItem>();
      string m_IfcVersion = null;
      bool m_SingleNodeSelection = false;
      HashSet<string> ExclElementSet = null;
      TreeViewItem PrevSelectedNode = null;
      string TreeSelectionDesc = null;

      /// <summary>
      /// Constructor for initializing EntityTree
      /// </summary>
      /// <param name="ifcVersion">the selected IFC version. This will "lock" the schema version in the dialog</param>
      /// <param name="excludeFilter">the initial list of the excluded entities. Can be used to initialize the setting</param>
      /// <param name="singleNodeSelection">true if the tree is used for a single node selection</param>
      public EntityTree(IFCVersion ifcVersion, string excludeFilter, string desc, bool singleNodeSelection = false)
      {
         m_IfcVersion = IfcSchemaEntityTree.SchemaName(ifcVersion);
         ExclElementSet = FillSetFromList(excludeFilter);
         m_SingleNodeSelection = singleNodeSelection;
         IfcSchemaEntityTree.GetAllEntityDict();
         TreeSelectionDesc = desc;

         InitializeComponent();

         // If the IFC schema version is selected for export, the combobox will be disabled for selection
         ComboBox_IFCSchema.IsEnabled = false;
         // Assign default
         if (string.IsNullOrEmpty(m_IfcVersion) || ifcVersion == IFCVersion.Default)
         {
            m_IfcVersion = IFCVersion.IFC4.ToString();
            ComboBox_IFCSchema.IsEnabled = true;
         }

         ComboBox_IFCSchema.ItemsSource = IfcSchemaEntityTree.GetAllCachedSchemaTrees();
         ComboBox_IFCSchema.SelectedItem = m_IfcVersion;

         LoadTreeviewFilterElement();
      }

      /// <summary>
      /// Override OK button content from caller since DLL does not seem to allow static resource value
      /// </summary>
      public string OKLabelOverride {
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

      /// <summary>
      /// Override IFC Schema label from caller since DLL does not seem to allow static resource value
      /// </summary>
      public string IFCSchemaLabelOverride
      {
         set
         {
            Label_IFCSchema.Content = value;
         }
      }

      void LoadTreeviewFilterElement()
      {
         try
         {
            string schemaFile = m_IfcVersion + ".xsd";
            // Process IFCXml schema here, then search for IfcProduct and build TreeView beginning from that node. Allow checks for the tree nodes. Grey out (and Italic) the abstract entity
            schemaFile = System.IO.Path.Combine(DirectoryUtil.IFCSchemaLocation, schemaFile);
            FileInfo schemaFileInfo = new FileInfo(schemaFile);

            IfcSchemaEntityTree ifcEntityTree = IfcSchemaEntityTree.GetEntityDictFor(m_IfcVersion);
            //bool newLoad = ProcessIFCXMLSchema.ProcessIFCSchema(schemaFileInfo);
            if (ifcEntityTree != null || m_TreeView.Items.Count == 0)
            {
               m_TreeView.Items.Clear();
               m_TreeViewItemDict.Clear();

               IfcSchemaEntityNode ifcProductNode;
               if (ifcEntityTree.IfcEntityDict.TryGetValue("IfcProduct", out ifcProductNode))
               {
                  // From IfcProductNode, recursively get all the children nodes and assign them into the treeview node (they are similar in the form)
                  TreeViewItem prod = new TreeViewItem();
                  prod.Name = "IfcProduct";
                  prod.Header = ifcProductNode.Name + " " + TreeSelectionDesc;
                  prod.IsExpanded = true;
                  prod.FontWeight = FontWeights.Bold;
                  m_TreeView.Items.Add(GetNode(ifcProductNode, prod, ExclElementSet));
               }

               IfcSchemaEntityNode ifcTypeProductNode;
               if (ifcEntityTree.IfcEntityDict.TryGetValue("IfcTypeProduct", out ifcTypeProductNode))
               {
                  // From IfcTypeProductNode, recursively get all the children nodes and assign them into the treeview node (they are similar in the form)
                  TreeViewItem typeProd = new TreeViewItem();
                  typeProd.Name = "IfcTypeProduct";
                  typeProd.Header = ifcTypeProductNode.Name + " " + TreeSelectionDesc;
                  typeProd.IsExpanded = true;
                  typeProd.FontWeight = FontWeights.Bold;
                  m_TreeView.Items.Add(GetNode(ifcTypeProductNode, typeProd, ExclElementSet));
               }

               IfcSchemaEntityNode ifcGroupNode;
               if (ifcEntityTree.IfcEntityDict.TryGetValue("IfcGroup", out ifcGroupNode))
               {
                  // For IfcGroup, a header is neaded because the IfcGroup itself is not a Abstract entity
                  TreeViewItem groupHeader = new TreeViewItem();
                  groupHeader.Name = "IfcGroupHeader";
                  groupHeader.Header = "IfcGroup" + " " + TreeSelectionDesc;
                  groupHeader.IsExpanded = true;
                  groupHeader.FontWeight = FontWeights.Bold;
                  m_TreeView.Items.Add(groupHeader);

                  // From IfcGroup Node, recursively get all the children nodes and assign them into the treeview node (they are similar in the form)
                  TreeViewItem groupNode = new TreeViewItem();
                  CheckBox groupNodeItem = new CheckBox();
                  groupNode.Name = "IfcGroup";
                  groupNode.Header = groupNodeItem;
                  groupNode.IsExpanded = true;
                  m_TreeViewItemDict.Add(groupNode.Name, groupNode);

                  groupNodeItem.Name = "IfcGroup";
                  groupNodeItem.Content = "IfcGroup";
                  groupNodeItem.FontWeight = FontWeights.Normal;
                  groupNodeItem.IsChecked = true;         // Default is always Checked
                  if (ExclElementSet.Contains(groupNode.Name) || m_SingleNodeSelection)
                     groupNodeItem.IsChecked = false;     // if the name is inside the excluded element hashset, UNcheck the checkbox (= remember the earlier choice)

                  groupNodeItem.Checked += new RoutedEventHandler(TreeViewItem_HandleChecked);
                  groupNodeItem.Unchecked += new RoutedEventHandler(TreeViewItem_HandleUnchecked);

                  groupHeader.Items.Add(GetNode(ifcGroupNode, groupNode, ExclElementSet));
               }
            }
            else
            {
               // Check all elements that have been excluded before for this configuration
               foreach (TreeViewItem tvItem in m_TreeView.Items)
                  UnCheckSelectedNode(tvItem, ExclElementSet);
            }
         }
         catch
         {
            // Error above in processing - disable the tree view.
            m_TreeView.IsEnabled = false;
         }
         IFCEntityTreeView.ItemsSource = m_TreeView.Items;
      }

      void UnCheckSelectedNode(TreeViewItem node, HashSet<string> exclElementSet)
      {
         CheckBox chkbox = node.Header as CheckBox;
         if (chkbox != null)
         {
            if (exclElementSet.Contains(chkbox.Name) || m_SingleNodeSelection)
               chkbox.IsChecked = false;
         }
         foreach (TreeViewItem nodeChld in node.Items)
            UnCheckSelectedNode(nodeChld, exclElementSet);
      }

      TreeViewItem GetNode(IfcSchemaEntityNode ifcNode, TreeViewItem thisNode, HashSet<string> exclSet)
      {
         foreach (IfcSchemaEntityNode ifcNodeChild in ifcNode.GetChildren())
         {
            bool alwaysDisable = false;

            // Disable selection for the *StandardCase entities to avoid this type of confusion "what it means when IfcWall is selected but not the IfcWallStandardCase?"
            if (ifcNodeChild.Name.Length > 12)
               if (ifcNodeChild.Name.EndsWith("StandardCase", StringComparison.CurrentCultureIgnoreCase) 
                  || ifcNodeChild.Name.EndsWith("ElementedCase", StringComparison.CurrentCultureIgnoreCase))
                  alwaysDisable = true;

            // Skip the spatial structure element because of its impact to containment and containment structure
            if (ifcNodeChild.Name.Equals("IfcSpatialStructureElement") || ifcNodeChild.IsSubTypeOf("IfcSpatialStructureElement")
               || ifcNodeChild.Name.Equals("IfcSpatialStructureElementType") || ifcNodeChild.IsSubTypeOf("IfcSpatialStructureElementType"))
               continue;

            TreeViewItem childNode = new TreeViewItem();
            CheckBox childNodeItem = new CheckBox();
            childNode.Name = ifcNodeChild.Name;
            m_TreeViewItemDict.Add(childNode.Name, childNode);
            childNodeItem.Name = ifcNodeChild.Name;
            if (ifcNodeChild.isAbstract)
            {
               childNodeItem.FontStyle = FontStyles.Italic;
               childNodeItem.Foreground = Brushes.Gray;
               childNodeItem.Content = "(ABS) " + ifcNodeChild.Name;
               // Disable selection on abstract entity in a single node selection
               if (m_SingleNodeSelection)
                  childNodeItem.IsEnabled = false;
            }
            else
               childNodeItem.Content = ifcNodeChild.Name;

            childNodeItem.FontWeight = FontWeights.Normal;
            if (m_SingleNodeSelection)
               childNodeItem.IsChecked = false;
            else
               childNodeItem.IsChecked = true;         // Default is always Checked
            if (exclSet.Contains(ifcNodeChild.Name))
               childNodeItem.IsChecked = false;     // if the name is inside the excluded element hashset, UNcheck the checkbox (= remember the earlier choice)

            if (alwaysDisable)
               childNodeItem.IsEnabled = false;

            childNodeItem.Checked += new RoutedEventHandler(TreeViewItem_HandleChecked);
            childNodeItem.Unchecked += new RoutedEventHandler(TreeViewItem_HandleUnchecked);
            childNode.Header = childNodeItem;
            childNode.IsExpanded = true;
            childNode = GetNode(ifcNodeChild, childNode, exclSet);
            thisNode.Items.Add(childNode);
         }
         return thisNode;
      }

      void TreeViewItem_HandleChecked(object sender, RoutedEventArgs eventArgs)
      {
         CheckBox cbItem = sender as CheckBox;
         TreeViewItem node = cbItem.Parent as TreeViewItem;
         if (m_SingleNodeSelection)
            CheckUncheckSingleSelection(node, true);
         else
            CheckOrUnCheckThisNodeAndBelow(node, isChecked: true);
      }

      void TreeViewItem_HandleUnchecked(object sender, RoutedEventArgs eventArgs)
      {
         CheckBox cbItem = sender as CheckBox;
         TreeViewItem node = cbItem.Parent as TreeViewItem;
         if (m_SingleNodeSelection)
            CheckOrUnCheckThisNodeAndBelow(node, false);
         else
            CheckOrUnCheckThisNodeAndBelow(node, isChecked: false);
      }

      void CheckUncheckSingleSelection(TreeViewItem thisNode, bool isChecked)
      {
         if (PrevSelectedNode != null)
         {
            CheckBox prevCBSel = (PrevSelectedNode.Header as CheckBox);
            if (prevCBSel != null)
               prevCBSel.IsChecked = false;    // Reset the previous selection if any
         }
         (thisNode.Header as CheckBox).IsChecked = isChecked;
         if (isChecked)
            PrevSelectedNode = thisNode;
      }

      void CheckOrUnCheckThisNodeAndBelow(TreeViewItem thisNode, bool isChecked)
      {
         (thisNode.Header as CheckBox).IsChecked = isChecked;

         // Here, to make sure the exclusion/inclusion is consistent for IfcProduct and IfcTypeProduct, 
         // if the Type is checked/unchecked the associated Entity will be checked/unchecked too
         // and the other way round too: if the Entity is checked/unchecked the associated Type will be checked/unchecked
         string clName = thisNode.Name.Substring(thisNode.Name.Length - 4, 4).Equals("Type", StringComparison.CurrentCultureIgnoreCase) ? thisNode.Name.Substring(0, thisNode.Name.Length - 4) : thisNode.Name;
         string tyName = thisNode.Name.Substring(thisNode.Name.Length - 4, 4).Equals("Type", StringComparison.CurrentCultureIgnoreCase) ? thisNode.Name : thisNode.Name + "Type";
         if (thisNode.Name.Equals(clName))
         {
            TreeViewItem assocTypeItem;
            if (m_TreeViewItemDict.TryGetValue(tyName, out assocTypeItem))
               (assocTypeItem.Header as CheckBox).IsChecked = isChecked;
         }
         else if (thisNode.Name.Equals(tyName))
         {
            TreeViewItem assocEntityItem;
            if (m_TreeViewItemDict.TryGetValue(clName, out assocEntityItem))
               (assocEntityItem.Header as CheckBox).IsChecked = isChecked;
         }

         foreach (TreeViewItem tvItem in thisNode.Items)
            CheckOrUnCheckThisNodeAndBelow(tvItem, isChecked);
      }

      void EnableOrDisableThisNodeAndBelow(TreeViewItem thisNode, bool enable)
      {
         bool toEnable = enable;

         // Always disable selection for the *StandardCase entities to avoid this type of confusion "what it means when IfcWall is selected but not the IfcWallStandardCase?"
         if (thisNode.Name.Length > 12)
            if (string.Compare(thisNode.Name, (thisNode.Name.Length - 12), "StandardCase", 0, 12, true) == 0)
               toEnable = false;

         // Must check if it is null (the first level in the tree is not a checkbox)
         CheckBox chkbox = thisNode.Header as CheckBox;
         if (chkbox != null)
            chkbox.IsEnabled = toEnable;

         // Here, to make sure the exclusion/inclusion is consistent for IfcProduct and IfcTypeProduct, 
         // if the Type is checked/unchecked the associated Entity will be checked/unchecked too
         // and the other way round too: if the Entity is checked/unchecked the associated Type will be checked/unchecked
         string clName = thisNode.Name.Substring(thisNode.Name.Length - 4, 4).Equals("Type", StringComparison.CurrentCultureIgnoreCase) ? thisNode.Name.Substring(0, thisNode.Name.Length - 4) : thisNode.Name;
         string tyName = thisNode.Name.Substring(thisNode.Name.Length - 4, 4).Equals("Type", StringComparison.CurrentCultureIgnoreCase) ? thisNode.Name : thisNode.Name + "Type";
         if (thisNode.Name.Equals(clName))
         {
            TreeViewItem assocTypeItem;
            if (m_TreeViewItemDict.TryGetValue(tyName, out assocTypeItem))
               (assocTypeItem.Header as CheckBox).IsEnabled = toEnable;
         }
         else if (thisNode.Name.Equals(tyName))
         {
            TreeViewItem assocEntityItem;
            if (m_TreeViewItemDict.TryGetValue(clName, out assocEntityItem))
               (assocEntityItem.Header as CheckBox).IsEnabled = toEnable;
         }

         foreach (TreeViewItem tvItem in thisNode.Items)
            EnableOrDisableThisNodeAndBelow(tvItem, enable);
      }

      bool IsAllDescendantsChecked(TreeViewItem thisNode)
      {
         bool isAllChecked = true;

         foreach (TreeViewItem tvItem in thisNode.Items)
         {
            CheckBox itemCheckBox = tvItem.Header as CheckBox;
            bool checkBoxIsChecked = false;
            if (itemCheckBox.IsChecked.HasValue)
               checkBoxIsChecked = itemCheckBox.IsChecked.Value;

            isAllChecked = isAllChecked && checkBoxIsChecked;
            if (!isAllChecked)
               return false;

            isAllChecked = isAllChecked && IsAllDescendantsChecked(tvItem);   // Do recursive check
            if (!isAllChecked)
               return false;
         }
         return true;
      }

      bool IsAllDescendantsUnhecked(TreeViewItem thisNode)
      {
         bool hasSomechecked = false;

         foreach (TreeViewItem tvItem in thisNode.Items)
         {
            CheckBox itemCheckBox = tvItem.Header as CheckBox;
            bool checkBoxIsChecked = false;
            if (itemCheckBox.IsChecked.HasValue)
               checkBoxIsChecked = itemCheckBox.IsChecked.Value;

            hasSomechecked = hasSomechecked || checkBoxIsChecked;
            if (hasSomechecked)
               return false;

            hasSomechecked = hasSomechecked || IsAllDescendantsUnhecked(tvItem);    // Do recursive check
            if (hasSomechecked)
               return false;
         }
         return true;
      }

      /// <summary>
      /// Get the list of entities that have been unselected
      /// </summary>
      /// <returns>the string containing the list of entity separated by ";"</returns>
      public string GetUnSelectedEntity()
      {
         string filteredElemList = string.Empty;
         foreach (TreeViewItem tvChld in m_TreeView.Items)
            filteredElemList += GetSelectedEntity(tvChld, false);
         return filteredElemList;
      }

      /// <summary>
      /// Get selected entity in the string. It should be returning a single entity
      /// </summary>
      /// <returns>the string of the selected entity</returns>
      public string GetSelectedEntity()
      {
         string filteredElemList = string.Empty;
         foreach (TreeViewItem tvChld in m_TreeView.Items)
            filteredElemList += GetSelectedEntity(tvChld, true);
         return filteredElemList;
      }

      string GetSelectedEntity(TreeViewItem tvItem, bool isChecked)
      {
         string filteredElemList = string.Empty;
         CheckBox cbElem = tvItem.Header as CheckBox;
         if (cbElem != null)
         {
            if (cbElem.IsChecked.HasValue)
               if (cbElem.IsChecked.Value == isChecked)
                  filteredElemList += cbElem.Name + ";";
         }
         foreach (TreeViewItem tvChld in tvItem.Items)
            filteredElemList += GetSelectedEntity(tvChld, isChecked);

         return filteredElemList;
      }

      void ClearTreeViewChecked()
      {
         foreach (TreeViewItem tvItem in m_TreeView.Items)
            ClearTreeviewChecked(tvItem);
      }

      /// <summary>
      /// This will clear any select/unselect and returns to the default which is ALL checked
      /// </summary>
      /// <param name="tv"></param>
      void ClearTreeviewChecked(TreeViewItem tv)
      {
         foreach (TreeViewItem tvItem in tv.Items)
         {
            CheckBox cbElem = tvItem.Header as CheckBox;
            if (cbElem != null)
               cbElem.IsChecked = false;

            ClearTreeviewChecked(tvItem);
         }
      }

      HashSet<string> FillSetFromList(string elemList)
      {
         HashSet<string> exclSet = new HashSet<string>();
         if (!string.IsNullOrEmpty(elemList))
         {
            elemList = elemList.TrimEnd(';');   // Remove the ending semicolon ';'
            string[] eList = elemList.Split(';');
            foreach (string elem in eList)
               exclSet.Add(elem);
         }
         return exclSet;
      }

      private void IFCEntityTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
      {

      }

      public bool Status { get; set; } = false;

      private void Button_Cancel_Click(object sender, RoutedEventArgs e)
      {
         Status = false;
         Close();
      }

      private void Button_OK_Click(object sender, RoutedEventArgs e)
      {
         Status = true;
         Close();
      }

      private void ComboBox_IFCSchema_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         string selIFCSchema = ComboBox_IFCSchema.SelectedItem.ToString();
         if (!m_IfcVersion.Equals(selIFCSchema))
         {
            m_IfcVersion = selIFCSchema;
            LoadTreeviewFilterElement();
         } 
      }
   }
}
