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
using System.Printing;
using Revit.IFC.Export.Utility;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Interaction logic for EntityTree.xaml
   /// </summary>
   public partial class EntityTree : ChildWindow
   {
      private TreeView TreeView { get; } = new TreeView();

      private TreeViewItem PrevSelPDefItem { get; set; } = null;

      private IDictionary<string, TreeViewItem> TreeViewItemDict { get; set; } = new Dictionary<string, TreeViewItem>(StringComparer.OrdinalIgnoreCase);

      private IFCEntityTrie m_EntityTrie = null;

      private string CurrentIFCVersion { get; set; } = null;

      private HashSet<string> ExclElementSet { get; set; } = null;
      
      private TreeViewItem PrevSelEntityItem { get; set; } = null;
      
      private string TreeSelectionDesc { get; set; } = null;

      private bool SingleNodeSelection { get; set; } = false;

      private bool ShowTypeNodeOnly { get; set; } = false;

      private bool AllowIfcAnnotation { get; set; } = false;

      /// <summary>
      /// Flag to indicate that the null selection is meant to reset the selected entity
      /// </summary>
      public bool IsReset { get; private set; } = false;

      /// <summary>
      /// A list of localized names for categories that allow mapping to IfcAnnotation.
      /// </summary>
      private static ISet<BuiltInCategory> AllowedCategoryIdsForIfcAnnotation = new HashSet<BuiltInCategory>();

      /// <summary>
      /// A list of localized names for categories that allow mapping to IfcAnnotation.
      /// </summary>
      private static ISet<string> AllowedCategoriesForIfcAnnotation = new HashSet<string>();

      private static void InitAllowedCategoryIdsForIfcAnnotation()
      {
         if (AllowedCategoryIdsForIfcAnnotation.Count == 0)
         {
            AllowedCategoryIdsForIfcAnnotation = new HashSet<BuiltInCategory>()
            {
               BuiltInCategory.OST_Lines,
               BuiltInCategory.OST_SiteProperty
            };
         }
      }

      private static void InitAllowedCategoriesForIfcAnnotation(Document document)
      {
         if (AllowedCategoriesForIfcAnnotation.Count == 0)
         {
            AllowedCategoriesForIfcAnnotation = new HashSet<string>();
            foreach (BuiltInCategory categoryId in AllowedCategoryIdsForIfcAnnotation)
            {
               AllowedCategoriesForIfcAnnotation.Add(
                  Category.GetCategory(document, categoryId).Name);
            };
         }
      }

      private static bool IsIfcAnnotationAllowedForCategory(Document document, string categoryName)
      {
         if (categoryName == null)
         {
            return false;
         }

         InitAllowedCategoryIdsForIfcAnnotation();

         InitAllowedCategoriesForIfcAnnotation(document);

         return AllowedCategoriesForIfcAnnotation.Contains(categoryName);
      }

      private static bool IsIfcAnnotationAllowedForCategoryId(BuiltInCategory categoryId)
      {
         InitAllowedCategoryIdsForIfcAnnotation();

         return AllowedCategoryIdsForIfcAnnotation.Contains(categoryId);
      }

      /// <summary>
      /// Constructor for initializing EntityTree
      /// </summary>
      /// <param name="showTypeNodeOnly">Option to show IfcTypeObject tree only</param>
      /// <param name="preSelectItem">Pre-select an item (works for a single node selection only)</param>
      /// <param name="preSelectPdef">Pre-select the predefined type</param>
      /// <param name="byCategory">Show the "By Category" checkbox if not null, and set value appropriately.</param>
      public EntityTree(IList<ElementId> elementIds, bool showTypeNodeOnly, string preSelectEntity, string preSelectPdef, 
         bool? byCategory)
      {
         AllowIfcAnnotation = true;
         Document document = IFCCommandOverrideApplication.TheDocument;
         foreach (ElementId elementId in elementIds)
         {
            Element element = document.GetElement(elementId);
            (_, ElementId categoryId) = ExporterUtil.GetSpecificCategoryForElement(element);

            BuiltInCategory builtInCategory = (BuiltInCategory)categoryId.Value;
            if (!IsIfcAnnotationAllowedForCategoryId(builtInCategory))
            {
               AllowIfcAnnotation = false;
               break;
            }
         }

         CurrentIFCVersion = null;
         ExclElementSet = FillSetFromList(null);
         SingleNodeSelection = true;
         TreeSelectionDesc = null;
         ShowTypeNodeOnly = showTypeNodeOnly;
         InitializeEntityTree(preSelectEntity, preSelectPdef, byCategory);
      }

      /// <summary>
      /// Constructor for initializing EntityTree
      /// </summary>
      /// <param name="showTypeNodeOnly">Option to show IfcTypeObject tree only</param>
      /// <param name="preSelectItem">Pre-select an item (works for a single node selection only)</param>
      /// <param name="preSelectPdef">Pre-select the predefined type</param>
      /// <param name="byCategory">Show the "By Category" checkbox if not null, and set value appropriately.</param>
      public EntityTree(CategoryMappingNode currMappingNode)
      {
         IFCMappingInfo mappingInfo = currMappingNode.MappingInfo;
         CategoryMappingNode parentNode = currMappingNode.Parent;

         bool? byCategory = null;

         Document document = IFCCommandOverrideApplication.TheDocument;
         AllowIfcAnnotation = IsIfcAnnotationAllowedForCategory(document, mappingInfo.CategoryName);
         
         if (parentNode != null)
         {
            byCategory = string.IsNullOrEmpty(mappingInfo.IfcClass);
            AllowIfcAnnotation = AllowIfcAnnotation || IsIfcAnnotationAllowedForCategory(document, parentNode.MappingInfo.CategoryName);
         }

         CurrentIFCVersion = null;
         ExclElementSet = FillSetFromList(null);
         SingleNodeSelection = true;
         TreeSelectionDesc = null;
         ShowTypeNodeOnly = false;
         InitializeEntityTree(mappingInfo.IfcClass, mappingInfo.PredefinedType, byCategory);
      }

      /// <summary>
      /// Constructor for initializing EntityTree
      /// </summary>
      /// <param name="ifcVersion">the selected IFC version, if given. This will "lock" the schema version in the dialog</param>
      /// <param name="excludeFilter">the initial list of the excluded entities. Can be used to initialize the setting</param>
      /// <param name="singleNodeSelection">true if the tree is used for a single node selection</param>
      /// <param name="showTypeNodeOnly">option to show IfcTypeObject tree only</param>
      /// <param name="preSelectItem">preselect an item (works for a single node selection only)</param>
      /// <param name="preSelectPdef">pre-select the predefined type</param>
      /// <param name="byCategory">Show the "By Category" checkbox if not null, and set value appropriately.</param>
      public EntityTree(IFCVersion ifcVersion, string excludeFilter, string desc, 
         bool singleNodeSelection)
      {
         CurrentIFCVersion = IfcSchemaEntityTree.SchemaName(ifcVersion);
         ExclElementSet = FillSetFromList(excludeFilter);
         SingleNodeSelection = singleNodeSelection;
         TreeSelectionDesc = desc;
         ShowTypeNodeOnly = false;
         InitializeEntityTree(null, null, null);
      }

      void InitializeEntityTree(string preSelectEntity, string preSelectPDef, bool? byCategory)
      {
         IfcSchemaEntityTree.GetAllEntityDict();
         InitializeComponent();

         textBox_Search.Focus();

         CheckBox_ByCategory.Visibility = (byCategory != null) ? System.Windows.Visibility.Visible :
            System.Windows.Visibility.Hidden;
         CheckBox_ByCategory.IsChecked = byCategory.GetValueOrDefault(false);

         if (SingleNodeSelection)
         {
            label_Show.Visibility = System.Windows.Visibility.Hidden;
            comboBox_ShowItems.Visibility = System.Windows.Visibility.Hidden;
         }
         else
         {
            HelpRun.Text = Properties.Resources.HelpSelectEntityForExport;
            comboBox_ShowItems.ItemsSource = new List<string>() { Properties.Resources.ShowAll, Properties.Resources.ShowChecked, Properties.Resources.ShowUnchecked };
            comboBox_ShowItems.SelectedIndex = 0;  // Default selection to show All
         }

         // If the IFC schema version is selected for export, the combobox will be disabled for selection
         ComboBox_IFCSchema.IsEnabled = false;
         // Assign default
         if (string.IsNullOrEmpty(CurrentIFCVersion))
         {
            CurrentIFCVersion = IFCVersion.IFC4.ToString();
            ComboBox_IFCSchema.IsEnabled = true;
         }

         ComboBox_IFCSchema.ItemsSource = IfcSchemaEntityTree.GetAllCachedSchemaNames();
         ComboBox_IFCSchema.SelectedItem = CurrentIFCVersion;
         if (SingleNodeSelection)
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
         if (SingleNodeSelection && !string.IsNullOrEmpty(preSelectEntity))
         {
            if (TreeViewItemDict.TryGetValue(preSelectEntity, out TreeViewItem assocTypeItem))
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
         else if (!SingleNodeSelection && !string.IsNullOrEmpty(preSelectEntity))
         {
            if (TreeViewItemDict.TryGetValue(preSelectEntity, out TreeViewItem assocTypeItem))
               assocTypeItem.BringIntoView();
         }
      }

      private bool AllowSelection()
      {
         if (CheckBox_ByCategory.Visibility == System.Windows.Visibility.Hidden)
            return true;

         return !CheckBox_ByCategory.IsChecked.GetValueOrDefault(false);
      }

      private (TreeViewItem, IfcSchemaEntityNode) AddTreeViewItem(bool add, string name, 
         IfcSchemaEntityTree ifcEntityTree, bool recurse)
      {
         if (!add)
            return (null, null);

         IfcSchemaEntityNode entityNode;
         if (!ifcEntityTree.IfcEntityDict.TryGetValue(name, out entityNode))
            return (null, null);

         // From IfcProductNode, recursively get all the children nodes and assign them into the treeview node (they are similar in the form)
         TreeViewItem treeViewItem = new TreeViewItem();
         treeViewItem.Name = name;
         if (SingleNodeSelection)
         {
            treeViewItem.Header = entityNode.Name + " " + TreeSelectionDesc;
         }
         else
         {
            ToggleButton itemNode = new CheckBox();
            itemNode.Name = name;
            itemNode.Content = name;
            itemNode.IsChecked = true;
            treeViewItem.Header = itemNode;
            itemNode.Checked += new RoutedEventHandler(TreeViewItem_HandleChecked);
            itemNode.Unchecked += new RoutedEventHandler(TreeViewItem_HandleUnchecked);
         }
         treeViewItem.IsExpanded = true;
         treeViewItem.FontWeight = FontWeights.Bold;

         if (recurse)
         {
            TreeView.Items.Add(GetNode(entityNode, treeViewItem, ExclElementSet));
         }
         else
         {
            TreeView.Items.Add(treeViewItem);
         }

         return (treeViewItem, entityNode);
      }

      void LoadTreeviewFilterElement()
      {
         button_Reset.IsEnabled = true;
         try
         {
            string schemaFile = CurrentIFCVersion + ".xsd";
            // Process ifcXML schema here, then search for IfcProduct and build TreeView beginning
            // from that node. Allow checks for the tree nodes. Grey out (and Italic) abstract
            // entities.
            schemaFile = Path.Combine(DirectoryUtil.IFCSchemaLocation, schemaFile);
            FileInfo schemaFileInfo = new FileInfo(schemaFile);
            m_EntityTrie = new IFCEntityTrie();

            IfcSchemaEntityTree ifcEntityTree = IfcSchemaEntityTree.GetEntityDictFor(CurrentIFCVersion);
            if (ifcEntityTree != null || TreeView.Items.Count == 0)
            {
               TreeView.Items.Clear();
               TreeViewItemDict.Clear();

               AddTreeViewItem(!ShowTypeNodeOnly, "IfcProduct", ifcEntityTree, true);
               AddTreeViewItem(true,"IfcTypeProduct", ifcEntityTree, true);

               TreeViewItem groupHeader;
               IfcSchemaEntityNode ifcGroupNode;
               (groupHeader, ifcGroupNode)= AddTreeViewItem(!ShowTypeNodeOnly, "IfcGroup", ifcEntityTree, false);

               if (groupHeader != null && ifcGroupNode != null)
               {
                  // From IfcGroup Node, recursively get all the children nodes and assign them
                  // into the treeview node (they are similar in the form)
                  TreeViewItem groupNode = new TreeViewItem();
                  ToggleButton groupNodeItem;
                  if (SingleNodeSelection)
                     groupNodeItem = new RadioButton();
                  else
                     groupNodeItem = new CheckBox();
                  groupNode.Name = "IfcGroup";
                  groupNode.Header = groupNodeItem;
                  groupNode.IsExpanded = true;
                  TreeViewItemDict.Add(groupNode.Name, groupNode);
                  m_EntityTrie.AddIFCEntityToDict(groupNode.Name);

                  groupNodeItem.Name = "IfcGroup";
                  groupNodeItem.Content = "IfcGroup";
                  groupNodeItem.FontWeight = FontWeights.Normal;
                  groupNodeItem.IsChecked = true;         // Default is always Checked
                  if (ExclElementSet.Contains(groupNode.Name) || SingleNodeSelection)
                     groupNodeItem.IsChecked = false;     // if the name is inside the excluded element hashset, UNcheck the checkbox (= remember the earlier choice)

                  groupNodeItem.Checked += new RoutedEventHandler(TreeViewItem_HandleChecked);
                  groupNodeItem.Unchecked += new RoutedEventHandler(TreeViewItem_HandleUnchecked);

                  groupHeader.Items.Add(GetNode(ifcGroupNode, groupNode, ExclElementSet));
               }
            }
            else
            {
               // Check all elements that have been excluded before for this configuration
               foreach (TreeViewItem tvItem in TreeView.Items)
               {
                  UnCheckSelectedNode(tvItem, ExclElementSet);
               }
            }

            bool isEnabled = AllowSelection();
            TreeView.IsEnabled = isEnabled;
            IFCEntityTreeView.IsEnabled = isEnabled;
         }
         catch
         {
            // Error above in processing - disable the tree view.
            CheckBox_ByCategory.IsEnabled = false;
            IFCEntityTreeView.IsEnabled = false;
            TreeView.IsEnabled = false;
         }

         IFCEntityTreeView.ItemsSource = TreeView.Items;
      }

      void UnCheckSelectedNode(TreeViewItem node, HashSet<string> exclElementSet)
      {
         // No need to do anything for single selection mode
         if (SingleNodeSelection)
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
         Document document = IFCCommandOverrideApplication.TheDocument;
         bool foundIfcAnnotation = false;
         foreach (IfcSchemaEntityNode ifcNodeChild in ifcNode.GetChildren())
         {
            string ifcClassName = ifcNodeChild.Name;
            
            // Skip deprecated entity
            if (IfcSchemaEntityTree.IsDeprecatedOrUnsupported(CurrentIFCVersion, ifcClassName))
            {
               continue;
            }

            if (!AllowIfcAnnotation && !foundIfcAnnotation && string.Compare(ifcClassName, "IfcAnnotation") == 0)
            {
               foundIfcAnnotation = true;
               continue;
            }

            TreeViewItem childNode = new TreeViewItem();
            if (SingleNodeSelection)
            {
               childNode.Name = ifcClassName;
               TreeViewItemDict.Add(ifcClassName, childNode);
               m_EntityTrie.AddIFCEntityToDict(ifcClassName);

               if (ifcNodeChild.isAbstract)
               {
                  childNode.Header = ifcClassName;
                  childNode.FontWeight = FontWeights.Normal;
               }
               else
               {
                  ToggleButton childNodeItem = new RadioButton();
                  childNodeItem.Name = ifcClassName;
                  childNodeItem.Content = ifcClassName;
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
               TreeViewItemDict.Add(childNode.Name, childNode);
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
         if (SingleNodeSelection)
            CheckUncheckSingleSelection(node, true);
         else
            CheckOrUnCheckThisNodeAndBelow(node, isChecked: true);
      }

      void TreeViewItem_HandleUnchecked(object sender, RoutedEventArgs eventArgs)
      {
         ToggleButton cbItem = sender as ToggleButton;
         TreeViewItem node = cbItem.Parent as TreeViewItem;
         if (SingleNodeSelection)
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
            InitializePreDefinedTypeSelection(CurrentIFCVersion, thisNode.Name);
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
            if (TreeViewItemDict.TryGetValue(tyName, out assocTypeItem))
            {
               ToggleButton assocType = assocTypeItem.Header as ToggleButton;
               if (assocType != null)
                  assocType.IsChecked = isChecked;
            }
         }
         else if (thisNode.Name.Equals(tyName))
         {
            TreeViewItem assocEntityItem;
            if (TreeViewItemDict.TryGetValue(clName, out assocEntityItem))
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
            if (TreeViewItemDict.TryGetValue(tyName, out assocTypeItem))
            {
               ToggleButton assocType = assocTypeItem.Header as ToggleButton;
               if (assocType != null)
                  assocType.IsEnabled = toEnable;
            }
         }
         else if (thisNode.Name.Equals(tyName))
         {
            TreeViewItem assocEntityItem;
            if (TreeViewItemDict.TryGetValue(clName, out assocEntityItem))
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

         foreach (TreeViewItem tvChld in TreeView.Items)
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

         foreach (TreeViewItem tvChld in TreeView.Items)
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

      void ClearTreeViewChecked()
      {
         foreach (TreeViewItem tvItem in TreeView.Items)
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
         IsReset = false;
         Close();
      }

      private void Button_OK_Click(object sender, RoutedEventArgs e)
      {
         DialogResult = true;
         if (SingleNodeSelection)
         {
            IsReset = string.IsNullOrEmpty(GetSelectedEntity()) ||
               string.IsNullOrEmpty(GetSelectedPredefinedType());
         }
         Close();
      }
      private void CheckBox_ByCategory_Clicked(object sender, RoutedEventArgs e)
      {
         bool byCategory = !AllowSelection();
         if (byCategory)
         {
            ClearTreeViewChecked();
            PredefinedTypeTreeView.ItemsSource = null;
            PrevSelPDefItem = null;
         }
         IFCEntityTreeView.IsEnabled = !byCategory;
         PredefinedTypeTreeView.IsEnabled = !byCategory;
      }

      private void ComboBox_IFCSchema_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         string currSelEntity = null;
         string currSelPdef = null;
         if (SingleNodeSelection)
         {
            currSelEntity = PrevSelEntityItem?.Name;
            currSelPdef = PrevSelPDefItem?.Name;
         }

         string selIFCSchema = ComboBox_IFCSchema.SelectedItem.ToString();
         if (!CurrentIFCVersion.Equals(selIFCSchema))
         {
            CurrentIFCVersion = selIFCSchema;
            m_EntityTrie = new IFCEntityTrie();
            LoadTreeviewFilterElement();

            if (SingleNodeSelection)
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
         return "";
      }

      private void TreeViewReturnToDefault()
      {
         IFCEntityTreeView.ItemsSource = TreeView.Items;
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
               ToggleButton origItem = TreeViewItemDict[item].Header as ToggleButton;
               if (origItem == null)
                  continue;   // Skip non-ToggleButton item

               TreeViewItem childNode = new TreeViewItem();
               ToggleButton childNodeItem;
               if (SingleNodeSelection)
                  childNodeItem = new RadioButton();
               else
                  childNodeItem = new CheckBox();

               childNode.Name = item;
               childNodeItem.Name = item;
               childNodeItem.Content = item;

               // set check status following the original selection           
               childNodeItem.IsChecked = origItem.IsChecked;
               if (SingleNodeSelection && origItem.IsChecked == true)
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
            if (prevSelItem != null && SingleNodeSelection)
            {
               ToggleButton prevCBSel = prevSelItem.Header as ToggleButton;
               if (prevCBSel != null)
                  prevCBSel.IsChecked = false;
            }
            cbElem.IsChecked = true;
            prevSelItem = cbElem.Parent as TreeViewItem;

            // Set the selection status of the node in the original tree as well
            TreeViewItem origTreeNode = TreeViewItemDict[prevSelItem.Name];
            ToggleButton origTreeNodeTB = origTreeNode.Header as ToggleButton;
            if (origTreeNodeTB != null)
               origTreeNodeTB.IsChecked = true;
         }
         if (SingleNodeSelection)
            InitializePreDefinedTypeSelection(CurrentIFCVersion, PrevSelEntityItem.Name);
      }

      void SearchItem_Unchecked(object sender, RoutedEventArgs e)
      {
         ToggleButton cbElem = sender as ToggleButton;
         if (cbElem != null)
            cbElem.IsChecked = false;
         PredefinedTypeTreeView.ItemsSource = null;

         // Set the selection status of the node in the original tree as well
         // Note that when we change schema, the selected item may no longer exist.
         string name = (cbElem.Parent as TreeViewItem).Name;
         if (TreeViewItemDict.TryGetValue(name, out TreeViewItem origTreeNode))
         {
            ToggleButton origTreeNodeTB = origTreeNode.Header as ToggleButton;
            if (origTreeNodeTB != null)
               origTreeNodeTB.IsChecked = false;
         }
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
         foreach (TreeViewItem item in TreeView.Items)
            ExpandOrCollapseThisNodeAndBelow(item, true);
      }

      private void button_CollapseAll_Click(object sender, RoutedEventArgs e)
      {
         foreach (TreeViewItem item in TreeView.Items)
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
               ToggleButton origItem = TreeViewItemDict[item].Header as ToggleButton;
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

      protected override bool OnContextHelp()
      {
         string contextIdName = null;
         if (SingleNodeSelection)
            contextIdName = "IFC_EntityAndPredefinedType";
         else
            contextIdName = "IFC_EntitiesToExport";

         // launch help
         Autodesk.Revit.UI.ContextualHelp help = new Autodesk.Revit.UI.ContextualHelp(Autodesk.Revit.UI.ContextualHelpType.ContextId, "HDialog_" + contextIdName);
         help.Launch();

         return true;
      }

      private void comboBox_ShowItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         switch (comboBox_ShowItems.SelectedIndex)
         {
            case 0:
               TreeViewReturnToDefault();
               break;
            case 1:
               ShowCheckedOrUnChecked(true);
               break;
            case 2:
               ShowCheckedOrUnChecked(false);
               break;
         }
      }
   }
}
