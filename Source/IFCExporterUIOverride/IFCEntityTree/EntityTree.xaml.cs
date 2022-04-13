using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Autodesk.Revit.DB;
using Revit.IFC.Common.Utility;
using Autodesk.UI.Windows;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Interaction logic for EntityTree.xaml
   /// </summary>
   public partial class EntityTree : ChildWindow
   {
      TreeView m_TreeView = new TreeView();
      TreeViewItem PrevSelPDefItem = null;
      IDictionary<string, TreeViewItem> m_TreeViewItemDict = new Dictionary<string, TreeViewItem>();
      IFCEntityTrie m_EntityTrie = new IFCEntityTrie();
      string m_IfcVersion = null;
      bool m_SingleNodeSelection = false;
      HashSet<string> ExclElementSet = null;
      TreeViewItem PrevSelEntityItem = null;
      string TreeSelectionDesc = null;

      bool m_ShowTypeNodeOnly = false;

      /// <summary>
      /// Flag to indicate the the null selection is meant to reset the selected entity
      /// </summary>
      public bool isReset { get; private set; } = false;

      /// <summary>
      /// Constructor for initializing EntityTree without IFCVersion and other defaults
      /// </summary>
      /// <param name="showTypeNodeOnly">option to show IfcTypeObject tree only</param>
      /// <param name="preSelectItem">pre-select an item (works for a single node selection only)</param>
      /// <param name="preSelectPdef">pre-select the predefined type</param>
      public EntityTree(bool showTypeNodeOnly = false, string preSelectEntity = null, string preSelectPdef = null)
      {
         m_IfcVersion = null;
         ExclElementSet = new HashSet<string>();
         m_SingleNodeSelection = true;
         TreeSelectionDesc = "";
         m_ShowTypeNodeOnly = showTypeNodeOnly;
         InitializeEntityTree(preSelectEntity, preSelectPdef);
      }

      /// <summary>
      /// Constructor for initializing EntityTree
      /// </summary>
      /// <param name="ifcVersion">the selected IFC version. This will "lock" the schema version in the dialog</param>
      /// <param name="excludeFilter">the initial list of the excluded entities. Can be used to initialize the setting</param>
      /// <param name="singleNodeSelection">true if the tree is used for a single node selection</param>
      /// <param name="showTypeNodeOnly">option to show IfcTypeObject tree only</param>
      /// <param name="preSelectItem">preselect an item (works for a single node selection only)</param>
      /// <param name="preSelectPdef">pre-select the predefined type</param>
      public EntityTree(IFCVersion ifcVersion, string excludeFilter, string desc, bool singleNodeSelection = false, bool showTypeNodeOnly = false, string preSelectEntity = null, string preSelectPdef = null)
      {
         m_IfcVersion = IfcSchemaEntityTree.SchemaName(ifcVersion);
         ExclElementSet = FillSetFromList(excludeFilter);
         m_SingleNodeSelection = singleNodeSelection;
         TreeSelectionDesc = desc;
         m_ShowTypeNodeOnly = showTypeNodeOnly;
         InitializeEntityTree(preSelectEntity, preSelectPdef);
      }

      void InitializeEntityTree(string preSelectEntity, string preSelectPDef)
      {
         IfcSchemaEntityTree.GetAllEntityDict();
         InitializeComponent();

         textBox_Search.Focus();

         if (m_SingleNodeSelection)
         {
            button_ShowAll.Visibility = System.Windows.Visibility.Hidden;
            button_ShowChecked.Visibility = System.Windows.Visibility.Hidden;
            button_ShowUnchecked.Visibility = System.Windows.Visibility.Hidden;
         }
         else
         {
            HelpRun.Text = Properties.Resources.HelpSelectEntityForExport;
         }

         // If the IFC schema version is selected for export, the combobox will be disabled for selection
         ComboBox_IFCSchema.IsEnabled = false;
         // Assign default
         if (string.IsNullOrEmpty(m_IfcVersion))
         {
            m_IfcVersion = IFCVersion.IFC4.ToString();
            ComboBox_IFCSchema.IsEnabled = true;
         }

         ComboBox_IFCSchema.ItemsSource = IfcSchemaEntityTree.GetAllCachedSchemaNames();
         ComboBox_IFCSchema.SelectedItem = m_IfcVersion;
         if (m_SingleNodeSelection)
         {
            Grid_Main.ColumnDefinitions[2].MinWidth = 200;
         }
         else
         {
            // In multi-selection mode, hide the TreView panel for the PredefinedType
            Grid_Main.ColumnDefinitions[2].MinWidth = 0;
            GridLengthConverter grLenConv = new GridLengthConverter();
            GridLength grLen = (GridLength)grLenConv.ConvertFrom(0);
            Grid_Main.ColumnDefinitions[2].Width = grLen;
         }

         LoadTreeviewFilterElement();
         PreSelectItem(preSelectEntity, preSelectPDef);
         IfcSchemaEntityTree.GenerateEntityTrie(ref m_EntityTrie);
      }

      /// <summary>
      /// Pre-select the Entity (and Predefined Type) if they are set by the server (only valid for single selection mode)
      /// </summary>
      /// <param name="preSelectEntity">Entity to be pre-selected</param>
      /// <param name="preSelectPDef">Predefined Type to be pre-selected</param>
      void PreSelectItem(string preSelectEntity, string preSelectPDef)
      {
         if (m_SingleNodeSelection && !string.IsNullOrEmpty(preSelectEntity))
         {
            if (m_TreeViewItemDict.TryGetValue(preSelectEntity, out TreeViewItem assocTypeItem))
            {
               (assocTypeItem.Header as ToggleButton).IsChecked = true;
               assocTypeItem.BringIntoView();

               if (!string.IsNullOrEmpty(preSelectPDef))
               {
                  foreach (TreeViewItem tvi in PredefinedTypeTreeView.Items)
                  {
                     foreach (TreeViewItem predefItem in tvi.Items)
                     {
                        if (predefItem.Name.Equals(preSelectPDef))
                        {
                           RadioButton cbElem = predefItem.Header as RadioButton;
                           cbElem.IsChecked = true;
                           return;
                        }
                     }
                  }
               }
            }
            else
            {
               PredefinedTypeTreeView.ItemsSource = null;
               PrevSelPDefItem = null;
            }
         }
         else if (!m_SingleNodeSelection && !string.IsNullOrEmpty(preSelectEntity))
         {
            if (m_TreeViewItemDict.TryGetValue(preSelectEntity, out TreeViewItem assocTypeItem))
               assocTypeItem.BringIntoView();
         }
      }

      void LoadTreeviewFilterElement()
      {
         button_Reset.IsEnabled = true;
         try
         {
            string schemaFile = m_IfcVersion + ".xsd";
            // Process IFCXml schema here, then search for IfcProduct and build TreeView beginning from that node. Allow checks for the tree nodes. Grey out (and Italic) the abstract entity
            schemaFile = System.IO.Path.Combine(DirectoryUtil.IFCSchemaLocation, schemaFile);
            FileInfo schemaFileInfo = new FileInfo(schemaFile);
            IFCEntityTrie entityTrie = new IFCEntityTrie();

            IfcSchemaEntityTree ifcEntityTree = IfcSchemaEntityTree.GetEntityDictFor(m_IfcVersion);
            if (ifcEntityTree != null || m_TreeView.Items.Count == 0)
            {
               m_TreeView.Items.Clear();
               m_TreeViewItemDict.Clear();

               if (!m_ShowTypeNodeOnly)
               {
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

               if (!m_ShowTypeNodeOnly)
               {
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
                     ToggleButton groupNodeItem;
                     if (m_SingleNodeSelection)
                        groupNodeItem = new RadioButton();
                     else
                        groupNodeItem = new CheckBox();
                     groupNode.Name = "IfcGroup";
                     groupNode.Header = groupNodeItem;
                     groupNode.IsExpanded = true;
                     m_TreeViewItemDict.Add(groupNode.Name, groupNode);
                     m_EntityTrie.AddIFCEntityToDict(groupNode.Name);

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
         // No need to do anything for single selection mode
         if (m_SingleNodeSelection)
            return;

         CheckBox chkbox = node.Header as CheckBox;
         if (chkbox != null)
         {
            if (exclElementSet.Contains(chkbox.Name))
               chkbox.IsChecked = false;
         }
         foreach (TreeViewItem nodeChld in node.Items)
            UnCheckSelectedNode(nodeChld, exclElementSet);
      }

      TreeViewItem GetNode(IfcSchemaEntityNode ifcNode, TreeViewItem thisNode, HashSet<string> exclSet)
      {
         foreach (IfcSchemaEntityNode ifcNodeChild in ifcNode.GetChildren())
         {
            // Skip deprecated entity
            if (IfcSchemaEntityTree.IsDeprecatedOrUnsupported(m_IfcVersion, ifcNodeChild.Name))
               continue;

            TreeViewItem childNode = new TreeViewItem();
            if (m_SingleNodeSelection)
            {
               childNode.Name = ifcNodeChild.Name;
               m_TreeViewItemDict.Add(childNode.Name, childNode);
               m_EntityTrie.AddIFCEntityToDict(ifcNodeChild.Name);

               if (ifcNodeChild.isAbstract)
               {
                  childNode.Header = ifcNodeChild.Name;
                  childNode.FontWeight = FontWeights.Normal;
               }
               else
               {
                  ToggleButton childNodeItem;
                  childNodeItem = new RadioButton();
                  childNodeItem.Name = ifcNodeChild.Name;
                  childNodeItem.Content = ifcNodeChild.Name;
                  childNodeItem.FontWeight = FontWeights.Normal;
                  childNodeItem.IsChecked = false;
                  childNodeItem.Checked += new RoutedEventHandler(TreeViewItem_HandleChecked);
                  childNodeItem.Unchecked += new RoutedEventHandler(TreeViewItem_HandleUnchecked);
                  childNode.Header = childNodeItem;
               }
            }
            else
            {
               childNode.Name = ifcNodeChild.Name;
               m_TreeViewItemDict.Add(childNode.Name, childNode);
               m_EntityTrie.AddIFCEntityToDict(ifcNodeChild.Name);

               ToggleButton childNodeItem;
               childNodeItem = new CheckBox();
               childNodeItem.Name = ifcNodeChild.Name;
               childNodeItem.Content = ifcNodeChild.Name;
               childNodeItem.FontWeight = FontWeights.Normal;
               childNodeItem.IsChecked = true;         // Default is always Checked
               if (exclSet.Contains(ifcNodeChild.Name))
                  childNodeItem.IsChecked = false;     // if the name is inside the excluded element hashset, UNcheck the checkbox (= remember the earlier choice)

               childNodeItem.Checked += new RoutedEventHandler(TreeViewItem_HandleChecked);
               childNodeItem.Unchecked += new RoutedEventHandler(TreeViewItem_HandleUnchecked);
               childNode.Header = childNodeItem;
            }
         
            childNode.IsExpanded = true;
            childNode = GetNode(ifcNodeChild, childNode, exclSet);
            thisNode.Items.Add(childNode);
         }
         return thisNode;
      }

      void TreeViewItem_HandleChecked(object sender, RoutedEventArgs eventArgs)
      {
         ToggleButton cbItem = sender as ToggleButton;
         TreeViewItem node = cbItem.Parent as TreeViewItem;
         if (m_SingleNodeSelection)
            CheckUncheckSingleSelection(node, true);
         else
            CheckOrUnCheckThisNodeAndBelow(node, isChecked: true);
      }

      void TreeViewItem_HandleUnchecked(object sender, RoutedEventArgs eventArgs)
      {
         ToggleButton cbItem = sender as ToggleButton;
         TreeViewItem node = cbItem.Parent as TreeViewItem;
         if (m_SingleNodeSelection)
            CheckOrUnCheckThisNodeAndBelow(node, false);
         else
            CheckOrUnCheckThisNodeAndBelow(node, isChecked: false);
      }

      void CheckUncheckSingleSelection(TreeViewItem thisNode, bool isChecked)
      {
         if (PrevSelEntityItem != null)
         {
            ToggleButton prevCBSel = (PrevSelEntityItem.Header as ToggleButton);
            if (prevCBSel != null)
               prevCBSel.IsChecked = false;    // Reset the previous selection if any
         }
         (thisNode.Header as ToggleButton).IsChecked = isChecked;
         if (isChecked)
         {
            PrevSelEntityItem = thisNode;
            InitializePreDefinedTypeSelection(m_IfcVersion, thisNode.Name);
         }
      }

      void CheckOrUnCheckThisNodeAndBelow(TreeViewItem thisNode, bool isChecked)
      {
         ToggleButton item = thisNode.Header as ToggleButton;
         if (item == null)
            return;

         item.IsChecked = isChecked;

         // Here, to make sure the exclusion/inclusion is consistent for IfcProduct and IfcTypeProduct, 
         // if the Type is checked/unchecked the associated Entity will be checked/unchecked too
         // and the other way round too: if the Entity is checked/unchecked the associated Type will be checked/unchecked
         string clName = thisNode.Name.Substring(thisNode.Name.Length - 4, 4).Equals("Type", StringComparison.CurrentCultureIgnoreCase) ? thisNode.Name.Substring(0, thisNode.Name.Length - 4) : thisNode.Name;
         string tyName = thisNode.Name.Substring(thisNode.Name.Length - 4, 4).Equals("Type", StringComparison.CurrentCultureIgnoreCase) ? thisNode.Name : thisNode.Name + "Type";
         if (thisNode.Name.Equals(clName))
         {
            TreeViewItem assocTypeItem;
            if (m_TreeViewItemDict.TryGetValue(tyName, out assocTypeItem))
            {
               ToggleButton assocType = assocTypeItem.Header as ToggleButton;
               if (assocType != null)
                  assocType.IsChecked = isChecked;
            } 
         }
         else if (thisNode.Name.Equals(tyName))
         {
            TreeViewItem assocEntityItem;
            if (m_TreeViewItemDict.TryGetValue(clName, out assocEntityItem))
            {
               ToggleButton assocType = assocEntityItem.Header as ToggleButton;
               if (assocType != null)
                  assocType.IsChecked = isChecked;
            }
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
         ToggleButton chkbox = thisNode.Header as ToggleButton;
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
            {
               ToggleButton assocType = assocTypeItem.Header as ToggleButton;
               if (assocType != null)
                  assocType.IsEnabled = toEnable;
            }
         }
         else if (thisNode.Name.Equals(tyName))
         {
            TreeViewItem assocEntityItem;
            if (m_TreeViewItemDict.TryGetValue(clName, out assocEntityItem))
            {
               ToggleButton assocType = assocEntityItem.Header as ToggleButton;
               if (assocType != null)
                  assocType.IsEnabled = toEnable;
            }
         }

         foreach (TreeViewItem tvItem in thisNode.Items)
            EnableOrDisableThisNodeAndBelow(tvItem, enable);
      }

      bool IsAllDescendantsChecked(TreeViewItem thisNode)
      {
         bool isAllChecked = true;

         foreach (TreeViewItem tvItem in thisNode.Items)
         {
            ToggleButton itemCheckBox = tvItem.Header as ToggleButton;
            if (itemCheckBox == null)
               continue;

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
            ToggleButton itemCheckBox = tvItem.Header as ToggleButton;
            if (itemCheckBox == null)
               continue;

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
         if (!DialogResult.HasValue || DialogResult.Value == false)
            return filteredElemList;

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
         if (!DialogResult.HasValue || DialogResult.Value == false)
            return filteredElemList;

         foreach (TreeViewItem tvChld in m_TreeView.Items)
            filteredElemList += GetSelectedEntity(tvChld, true);
         if (filteredElemList.EndsWith(";"))
            filteredElemList = filteredElemList.Remove(filteredElemList.Length - 1);
         return filteredElemList;
      }

      string GetSelectedEntity(TreeViewItem tvItem, bool isChecked)
      {
         string filteredElemList = string.Empty;
         ToggleButton cbElem = tvItem.Header as ToggleButton;
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

      /// <summary>
      /// Return the selected IFC schema version
      /// </summary>
      /// <returns>the IFC schema version</returns>
      public string IFCVersionUsed()
      {
         return m_IfcVersion;
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
            ToggleButton cbElem = tvItem.Header as ToggleButton;
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

      private void Button_Cancel_Click(object sender, RoutedEventArgs e)
      {
         DialogResult = false;
         isReset = false;
         Close();
      }

      private void Button_OK_Click(object sender, RoutedEventArgs e)
      {
         DialogResult = true;
         if (m_SingleNodeSelection)
         {
            if (string.IsNullOrEmpty(GetSelectedEntity()))
               isReset = true;
            else
               isReset = false;
         }
         Close();
      }

      private void ComboBox_IFCSchema_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         string currSelEntity = null;
         string currSelPdef = null;
         if (m_SingleNodeSelection)
         {
            currSelEntity = PrevSelEntityItem?.Name;
            currSelPdef = PrevSelPDefItem?.Name;
         }

         string selIFCSchema = ComboBox_IFCSchema.SelectedItem.ToString();
         if (!m_IfcVersion.Equals(selIFCSchema))
         {
            m_IfcVersion = selIFCSchema;
            m_EntityTrie = new IFCEntityTrie();
            LoadTreeviewFilterElement();

            if (m_SingleNodeSelection)
            {
               PreSelectItem(currSelEntity, currSelPdef);
            }
            else
            {
               PredefinedTypeTreeView.ItemsSource = null;
               PrevSelPDefItem = null;
            }
            IfcSchemaEntityTree.GenerateEntityTrie(ref m_EntityTrie);
         }
      }

      void InitializePreDefinedTypeSelection(string ifcSchema, string ifcEntitySelected)
      {
         if (string.IsNullOrEmpty(ifcEntitySelected))
            return;

         TreeView predefinedTypeTreeView = new TreeView();
         IfcSchemaEntityTree ifcEntityTree = IfcSchemaEntityTree.GetEntityDictFor(ifcSchema);
         IList<string> predefinedTypeList = IfcSchemaEntityTree.GetPredefinedTypeList(ifcEntityTree, ifcEntitySelected);

         if (predefinedTypeList != null && predefinedTypeList.Count > 0)
         {
            TreeViewItem ifcEntityViewItem = new TreeViewItem();
            ifcEntityViewItem.Name = ifcEntitySelected;
            ifcEntityViewItem.Header = ifcEntitySelected + ".PREDEFINEDTYPE";
            ifcEntityViewItem.IsExpanded = true;
            predefinedTypeTreeView.Items.Add(ifcEntityViewItem);

            foreach (string predefItem in predefinedTypeList)
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
         else
         {
            TreeViewItem ifcEntityViewItem = new TreeViewItem();
            ifcEntityViewItem.Name = ifcEntitySelected;
            ifcEntityViewItem.Header = Properties.Resources.NoPredefinedType;
            predefinedTypeTreeView.Items.Add(ifcEntityViewItem);
         }
         PredefinedTypeTreeView.ItemsSource = predefinedTypeTreeView.Items;
      }

      void PredefSelected_Checked(object sender, RoutedEventArgs e)
      {
         RadioButton cbElem = sender as RadioButton;
         if (cbElem != null)
         {
            // Clear previously selected item first
            if (PrevSelPDefItem != null)
            {
               RadioButton prevCBSel = PrevSelPDefItem.Header as RadioButton;
               if (prevCBSel != null)
                  prevCBSel.IsChecked = false;
            }
            cbElem.IsChecked = true;
            PrevSelPDefItem = cbElem.Parent as TreeViewItem;
         }
      }

      void PredefSelected_Unchecked(object sender, RoutedEventArgs e)
      {
         RadioButton cbElem = sender as RadioButton;
         if (cbElem != null)
            cbElem.IsChecked = false;
      }

      /// <summary>
      /// Get the selected Predefined Type
      /// </summary>
      /// <returns>The selected Predefined Type string</returns>
      public string GetSelectedPredefinedType()
      {
         if (PredefinedTypeTreeView.Items == null)
            return null;

         foreach (TreeViewItem tvi in PredefinedTypeTreeView.Items)
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

      private void TreeViewReturnToDefault()
      {
         IFCEntityTreeView.ItemsSource = m_TreeView.Items;
         button_Reset.IsEnabled = true;
         button_CollapseAll.IsEnabled = true;
         button_ExpandAll.IsEnabled = true;
      }

      private void textBox_Search_TextChanged(object sender, TextChangedEventArgs e)
      {
         string partialWord = textBox_Search.Text;
         if (string.IsNullOrWhiteSpace(partialWord))
         {
            TreeViewReturnToDefault();
            return;
         }

         TreeView searchTreeView = new TreeView();
         IList<string> searchResults = m_EntityTrie.PartialWordSearch(partialWord);

         if (searchResults != null && searchResults.Count > 0)
         {
            foreach (string item in searchResults)
            {
               ToggleButton origItem = m_TreeViewItemDict[item].Header as ToggleButton;
               if (origItem == null)
                  continue;   // Skip non-ToggleButton item

               TreeViewItem childNode = new TreeViewItem();
               ToggleButton childNodeItem;
               if (m_SingleNodeSelection)
                  childNodeItem = new RadioButton();
               else
                  childNodeItem = new CheckBox();

               childNode.Name = item;
               childNodeItem.Name = item;
               childNodeItem.Content = item;

               // set check status following the original selection           
               childNodeItem.IsChecked = origItem.IsChecked;
               if (m_SingleNodeSelection && origItem.IsChecked == true)
                  prevSelItem = childNode;

               childNodeItem.Checked += new RoutedEventHandler(SearchItem_Checked);
               childNodeItem.Unchecked += new RoutedEventHandler(SearchItem_Unchecked);
               childNode.Header = childNodeItem;
               searchTreeView.Items.Add(childNode);
            }
            IFCEntityTreeView.ItemsSource = searchTreeView.Items;
         }
         else
            IFCEntityTreeView.ItemsSource = null;

         button_Reset.IsEnabled = false;
         button_CollapseAll.IsEnabled = false;
         button_ExpandAll.IsEnabled = false;
      }

      TreeViewItem prevSelItem;

      void SearchItem_Checked(object sender, RoutedEventArgs e)
      {
         ToggleButton cbElem = sender as ToggleButton;
         if (cbElem != null)
         {
            // Clear previously selected item first
            if (prevSelItem != null && m_SingleNodeSelection)
            {
               ToggleButton prevCBSel = prevSelItem.Header as ToggleButton;
               if (prevCBSel != null)
                  prevCBSel.IsChecked = false;
            }
            cbElem.IsChecked = true;
            prevSelItem = cbElem.Parent as TreeViewItem;

            // Set the selection status of the node in the original tree as well
            TreeViewItem origTreeNode = m_TreeViewItemDict[prevSelItem.Name];
            ToggleButton origTreeNodeTB = origTreeNode.Header as ToggleButton;
            if (origTreeNodeTB != null)
               origTreeNodeTB.IsChecked = true;
         }
         if (m_SingleNodeSelection)
            InitializePreDefinedTypeSelection(m_IfcVersion, PrevSelEntityItem.Name);
      }

      void SearchItem_Unchecked(object sender, RoutedEventArgs e)
      {
         ToggleButton cbElem = sender as ToggleButton;
         if (cbElem != null)
            cbElem.IsChecked = false;
         PredefinedTypeTreeView.ItemsSource = null;

         // Set the selection status of the node in the original tree as well
         TreeViewItem origTreeNode = m_TreeViewItemDict[(cbElem.Parent as TreeViewItem).Name];
         ToggleButton origTreeNodeTB = origTreeNode.Header as ToggleButton;
         if (origTreeNodeTB != null)
            origTreeNodeTB.IsChecked = false;
      }

      private void button_Reset_Click(object sender, RoutedEventArgs e)
      {
         LoadTreeviewFilterElement();
         PredefinedTypeTreeView.ItemsSource = null;
         PrevSelPDefItem = null;
         prevSelItem = null;
      }

      void ExpandOrCollapseThisNodeAndBelow(TreeViewItem thisNode, bool expand)
      {
         thisNode.IsExpanded = expand;
         foreach (TreeViewItem tvItem in thisNode.Items)
            ExpandOrCollapseThisNodeAndBelow(tvItem, expand);
      }

      private void button_ExpandAll_Click(object sender, RoutedEventArgs e)
      {
         foreach (TreeViewItem item in m_TreeView.Items)
            ExpandOrCollapseThisNodeAndBelow(item, true);
      }

      private void button_CollapseAll_Click(object sender, RoutedEventArgs e)
      {
         foreach (TreeViewItem item in m_TreeView.Items)
            ExpandOrCollapseThisNodeAndBelow(item, false);
      }

      private void ShowCheckedOrUnChecked(bool checkFlag)
      {
         TreeView searchTreeView = new TreeView();
         // Essentially collect all entities (containing ifc)
         IList<string> searchResults = m_EntityTrie.PartialWordSearch("ifc");

         if (searchResults != null && searchResults.Count > 0)
         {
            foreach (string item in searchResults)
            {
               ToggleButton origItem = m_TreeViewItemDict[item].Header as ToggleButton;
               if (origItem == null)
                  continue;   // Skip non-ToggleButton item

               if (!origItem.IsChecked.HasValue 
                  || (checkFlag && origItem.IsChecked.Value == !checkFlag)
                  || (!checkFlag && origItem.IsChecked.Value == !checkFlag))
                  continue;

               TreeViewItem childNode = new TreeViewItem();
               ToggleButton childNodeItem;
               childNodeItem = new CheckBox();

               childNode.Name = item;
               childNodeItem.Name = item;
               childNodeItem.Content = item;

               // set check status following the original selection           
               childNodeItem.IsChecked = origItem.IsChecked;

               childNodeItem.Checked += new RoutedEventHandler(SearchItem_Checked);
               childNodeItem.Unchecked += new RoutedEventHandler(SearchItem_Unchecked);
               childNode.Header = childNodeItem;
               searchTreeView.Items.Add(childNode);
            }
            IFCEntityTreeView.ItemsSource = searchTreeView.Items;
         }
         else
            IFCEntityTreeView.ItemsSource = null;

         button_Reset.IsEnabled = false;
         button_CollapseAll.IsEnabled = false;
         button_ExpandAll.IsEnabled = false;
      }

      private void button_ShowChecked_Click(object sender, RoutedEventArgs e)
      {
         ShowCheckedOrUnChecked(true);
      }

      private void button_ShowUnchecked_Click(object sender, RoutedEventArgs e)
      {
         ShowCheckedOrUnChecked(false);
      }

      private void button_ShowAll_Click(object sender, RoutedEventArgs e)
      {
         TreeViewReturnToDefault();
      }

      protected override bool OnContextHelp()
      {
         string contextIdName = null;
         if (m_SingleNodeSelection)
            contextIdName = "IFC_EntityAndPredefinedType";
         else
            contextIdName = "IFC_EntitiesToExport";

         // launch help
         Autodesk.Revit.UI.ContextualHelp help = new Autodesk.Revit.UI.ContextualHelp(Autodesk.Revit.UI.ContextualHelpType.ContextId, "HDialog_" + contextIdName);
         help.Launch();

         return true;
      }
   }
}
