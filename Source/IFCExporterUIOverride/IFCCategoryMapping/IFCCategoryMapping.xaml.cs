using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.UI.Windows;
using BIM.IFC.Export.UI.Properties;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using System;

namespace BIM.IFC.Export.UI
{
   public class IFCMappingInfo : INotifyPropertyChanged
   {
      private string m_IfcClass = null;

      private string m_PredefinedType = null;

      private bool? m_ExportFlag = false;

      /// <summary>
      /// Flag to determine if a category is exported or not.
      /// </summary>
      public bool? ExportFlag
      {
         get { return m_ExportFlag; }
         set
         {
            m_ExportFlag = value;
            OnPropertyChanged();
         }
      }

      /// <summary>
      /// The Revit category name.
      /// </summary>
      public string CategoryName { get; set; }

      /// <summary>
      /// The corresponding IFC entity name.
      /// </summary>
      public string IfcClass
      {
         get { return m_IfcClass; }
         set
         {
            m_IfcClass = value;
            OnPropertyChanged();
         }
      }

      public bool UserDefinedTypeEnabled
      {
         get
         {
            return (string.Compare(m_PredefinedType, "USERDEFINED") == 0);
         }
      }

      /// <summary>
      /// The predefined type associated to the IFC entity name.
      /// </summary>
      public string PredefinedType
      {
         get { return m_PredefinedType; }
         set
         {
            m_PredefinedType = value;
            if (string.Compare(m_PredefinedType, "USERDEFINED") != 0)
            {
               UserDefinedType = string.Empty;
            }
            OnPropertyChanged();
            OnPropertyChanged("UserDefinedTypeEnabled");
         }
      }

      private string m_UserDefinedType = null;

      /// <summary>
      /// An arbitrary type string, if PredefinedType is user defined.
      /// </summary>
      public string UserDefinedType 
      { 
         get 
         { 
            return m_UserDefinedType; 
         } 
         set
         {
            m_UserDefinedType = value;
            OnPropertyChanged();
         }
      }

      /// <summary>
      /// Custom subcategory Id for extra categories
      /// </summary>
      public CustomSubCategoryId CustomSubCategoryId { get; set; }


      public event PropertyChangedEventHandler PropertyChanged;

      protected void OnPropertyChanged([CallerMemberName] string name = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }
   }


   /// <summary>
   /// Interaction logic for IFCCategoryMapping.xaml
   /// </summary>
   public partial class IFCCategoryMapping : ChildWindow, INotifyPropertyChanged
   {
      private bool? m_ExportFlagAll = false;

      /// <summary>
      /// Flag to determine if all categories are exported or not.
      /// </summary>
      public bool? ExportFlagAll
      {
         get { return m_ExportFlagAll; }
         set
         {
            m_ExportFlagAll = value;
            OnPropertyChanged(nameof(ExportFlagAll));
            ExportFlagAllClick();
         }
      }

      public event PropertyChangedEventHandler PropertyChanged;

      protected void OnPropertyChanged([CallerMemberName] string name = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }

      private TransactionGroup groupTransaction;
      private Transaction templateTransaction;

      public IFCCategoryMapping()
      {
         Initialize();
      }

      public IFCCategoryMapping(Autodesk.Revit.UI.UIApplication app)
      {
         SetParent(app.MainWindowHandle);
         Initialize();
      }

      private void Initialize()
      {
         InitializeComponent();
         DataContext = this;
         groupTransaction = new TransactionGroup(IFCCommandOverrideApplication.TheDocument);
         templateTransaction = new Transaction(IFCCommandOverrideApplication.TheDocument, Properties.Resources.ModifyIFCCategoryMapping);
         StartTransactionGroup();

         textBox_Search.Focus();

         IFCCategoryTemplate currentTemplateName = IFCCategoryTemplate.GetActiveTemplate(IFCCommandOverrideApplication.TheDocument);
         if (currentTemplateName == null)
            currentTemplateName = IFCCategoryTemplate.GetOrCreateInSessionTemplate(IFCCommandOverrideApplication.TheDocument);

         InitializeTemplateList(currentTemplateName?.Name);
      }

      /// <summary>
      /// Initializes the mapping templates listbox.
      /// </summary>
      private void InitializeTemplateList(string activeTemplateName)
      {
         listBox_MappingTemplates.Items.Clear();

         IFCCategoryTemplate inSessionTemplate = IFCCategoryTemplate.GetOrCreateInSessionTemplate(IFCCommandOverrideApplication.TheDocument);
         if (inSessionTemplate == null)
            return;

         string inSessionName = inSessionTemplate.Name;
         listBox_MappingTemplates.Items.Add(inSessionName);
         IList<string> templateNames = IFCCategoryTemplate.ListNames(IFCCommandOverrideApplication.TheDocument);
         foreach (string name in templateNames)
            listBox_MappingTemplates.Items.Add(name);

         // Update category list to include possibly added categories
         inSessionTemplate.UpdateCategoryList(IFCCommandOverrideApplication.TheDocument);
         foreach (string name in templateNames)
         {
            IFCCategoryTemplate template = IFCCategoryTemplate.FindByName(IFCCommandOverrideApplication.TheDocument, name);
            if (template == null)
               return;

            template.UpdateCategoryList(IFCCommandOverrideApplication.TheDocument);
         }

         if (activeTemplateName == null || !templateNames.Contains(activeTemplateName))
            listBox_MappingTemplates.SelectedItem = inSessionName;
         else
            listBox_MappingTemplates.SelectedItem = activeTemplateName;
      }

      /// <summary>
      /// Initializes the mapping data grid.
      /// </summary>
      private void InitializeMappingGrid()
      {
         IFCCategoryTemplate currentTemplate = GetCurrentTemplate();
         if (currentTemplate == null)
            return;

         IDictionary<ExportIFCCategoryKey, ExportIFCCategoryInfo> mappingTable = currentTemplate.GetCategoryMappingTable(IFCCommandOverrideApplication.TheDocument);
         CategoryMappingManager categoryManager = new CategoryMappingManager();

         // Save last category node to fill it with children
         // The dictionary is sorted (sub-categories are met after the corresponding category)
         Tuple<string, CategoryMappingNode> lastCategoryNode = null;
         ExportFlagAll = true;

         foreach (var mappingPair in mappingTable)
         {
            bool isCategory = string.IsNullOrEmpty(mappingPair.Key.SubCategoryName);
            bool isSpecialSubCategory = (mappingPair.Key.CustomSubCategoryId != CustomSubCategoryId.None);
            
            string categoryName;
            if (isCategory)
            {
               categoryName = mappingPair.Key.CategoryName;
            }
            else if (isSpecialSubCategory)
            {
               categoryName = mappingPair.Key.CategoryName + " (" + mappingPair.Key.SubCategoryName + ")";
            }
            else
            {
               categoryName = mappingPair.Key.SubCategoryName;
            }
            

            // Export all checkbox becomes indeterminate
            if ((!mappingPair.Value.IFCExportFlag) && (ExportFlagAll != null))
               ExportFlagAll = null;

            CategoryMappingNode createdNode = new CategoryMappingNode()
            {
               MappingInfo = new IFCMappingInfo
               {
                  ExportFlag = mappingPair.Value.IFCExportFlag,
                  CategoryName = categoryName,
                  IfcClass = mappingPair.Value.IFCEntityName,
                  PredefinedType = mappingPair.Value.IFCPredefinedType,
                  UserDefinedType = mappingPair.Value.IFCUserDefinedType,
                  CustomSubCategoryId = mappingPair.Key.CustomSubCategoryId
               },
               DataManager = categoryManager
            };

            if (isCategory)
            {
               categoryManager.Data.Add(createdNode);
               lastCategoryNode = Tuple.Create(mappingPair.Key.CategoryName, createdNode);
            }
            else
            {
               string lastCategoryName = lastCategoryNode?.Item1;
               if (lastCategoryName?.Equals(mappingPair.Key.CategoryName) ?? false)
               {
                  lastCategoryNode.Item2.AddChild(createdNode);
               }
            }
         }

         foreach (var node in categoryManager.Data)
         {
            node.IsVisible = true;
            node.IsExpanded = false;
         }

         categoryManager.Initialize();
         dataGrid_CategoryMapping.ItemsSource = categoryManager;
         textBox_Search.Text = string.Empty;
      }

      protected override bool OnContextHelp()
      {
         ContextualHelp help = new ContextualHelp(ContextualHelpType.ContextId, "HDialog_IFC_CategoryMapping");
         help.Launch();

         return true;
      }

      /// <summary>
      /// Returns true is the name is equal to in-session template name
      /// </summary>
      static bool IsInSessionTemplate(string templateName)
      {
         string inSessionName = GetInSessiontemplateName();
         return templateName.Equals(inSessionName);
      }

      /// <summary>
      /// Returns in-session template name
      /// </summary>
      static string GetInSessiontemplateName()
      {
         IFCCategoryTemplate inSessionTemplate = IFCCategoryTemplate.GetOrCreateInSessionTemplate(IFCCommandOverrideApplication.TheDocument);
         return inSessionTemplate?.Name;
      }

      /// <summary>
      /// Returns the list of templates in the document including the in-session one
      /// </summary>
      static IList<string> GetAllTemplateNames()
      {
         IList<string> templateNames = IFCCategoryTemplate.ListNames(IFCCommandOverrideApplication.TheDocument);
         templateNames?.Add(GetInSessiontemplateName());
         return templateNames;
      }

      /// <summary>
      /// Get template active in list
      /// </summary>
      private IFCCategoryTemplate GetCurrentTemplate()
      {
         return GetTemplateByName(listBox_MappingTemplates.SelectedItem as string);
      }

      private IFCCategoryTemplate GetTemplateByName(string templateName)
      {
         if (string.IsNullOrEmpty(templateName))
            return null;

         IFCCategoryTemplate foundTemplate = null;
         if (IsInSessionTemplate(templateName))
            foundTemplate = IFCCategoryTemplate.GetOrCreateInSessionTemplate(IFCCommandOverrideApplication.TheDocument);
         else
            foundTemplate = IFCCategoryTemplate.FindByName(IFCCommandOverrideApplication.TheDocument, templateName);

         return foundTemplate;
      }

      /// <summary>
      /// Updates the controls.
      /// </summary>
      /// <param name="isInSession">Value of whether the configuration is in-session or not.</param>
      private void UpdateTemplateControls(bool isInSessionSelected)
      {
         button_Rename.IsEnabled = !isInSessionSelected;
         button_Delete.IsEnabled = !isInSessionSelected;
      }

      /// <summary>
      /// Returns true if the name is valid
      /// </summary>
      public static bool IsValidName(string templateName, IList<string> existingNames)
      {
         templateName = templateName?.TrimStart()?.TrimEnd();
         return (!(string.IsNullOrWhiteSpace(templateName)
            || (existingNames?.Contains(templateName) ?? false)))
            && IFCCategoryTemplate.IsValidName(IFCCommandOverrideApplication.TheDocument, templateName);
      }

      private void listBox_MappingTemplates_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
      {
         // Save current template before switching to new one
         if (e.RemovedItems.Count > 0)
         {
            string prevTemplateName = e.RemovedItems[0] as string;
            if (!string.IsNullOrEmpty(prevTemplateName))
            {
               UpdateTemplateFromGrid(prevTemplateName);

               if (templateTransaction.HasStarted())
                  templateTransaction.Commit();

               if (templateTransaction.HasEnded())
                  templateTransaction.Start();
            }
         }

         IFCCategoryTemplate currTemplate = GetCurrentTemplate();
         if (currTemplate == null)
            return;

         InitializeMappingGrid();

         bool isInSessionSelected = IsInSessionTemplate(currTemplate.Name);
         UpdateTemplateControls(isInSessionSelected);

         if (isInSessionSelected)
            IFCCategoryTemplate.ResetActiveTemplate(IFCCommandOverrideApplication.TheDocument);
         else
            currTemplate.SetActiveTemplate(IFCCommandOverrideApplication.TheDocument);
      }

      private void textBox_Search_TextChanged(object sender, TextChangedEventArgs e)
      {
         string partialWord = textBox_Search.Text ?? string.Empty;

         CategoryMappingManager currentManager = dataGrid_CategoryMapping.ItemsSource as CategoryMappingManager;

         currentManager.Reset(resetVisibility: true);

         foreach (CategoryMappingNode parentNode in currentManager.Data)
         {
            bool parentMatches = parentNode.MappingInfo.CategoryName.Contains(partialWord, StringComparison.InvariantCultureIgnoreCase);
            if (parentMatches)
            {
               // Show expanded parent with all its children
               parentNode.IsVisible = true;
               parentNode.IsExpanded = true;
               continue;
            }


            IList<CategoryMappingNode> matchedChildren = parentNode.Children
               .Where(x => x.MappingInfo.CategoryName.Contains(partialWord, StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (matchedChildren.Count > 0)
            {
               // Show expanded parent with matched children only
               parentNode.IsVisible = true;
               parentNode.IsExpanded = true;
               foreach (CategoryMappingNode child in parentNode.Children)
               {
                  if (!matchedChildren.Contains(child))
                  {
                     child.IsVisible = false;
                     child.IsHiddenByFilter = true;
                  }
               }
            }
            else
            {
               foreach (CategoryMappingNode child in parentNode.Children)
               {
                  child.IsHiddenByFilter = true;
               }
               parentNode.IsHiddenByFilter = true;
            }
         }

         currentManager.Initialize();
      }

      private void button_Add_Click(object sender, RoutedEventArgs e)
      {
         IFCCategoryTemplateData data = new IFCCategoryTemplateData(Properties.Resources.NewTemplateDefaultName, IFCCategoryTemplate.ListNames(IFCCommandOverrideApplication.TheDocument));
         IFCNewCategoryTemplate settingsTemplateDialog = new IFCNewCategoryTemplate(data);
         settingsTemplateDialog.Owner = this;
         bool? ret = settingsTemplateDialog.ShowDialog();
         if (ret.HasValue && ret.Value == true)
         {
            string templateName = settingsTemplateDialog.Data.NewName;
            if (!string.IsNullOrEmpty(templateName) && !listBox_MappingTemplates.Items.Contains(templateName))
            {
               IFCCategoryTemplate newCategoryTemplate = IFCCategoryTemplate.Create(IFCCommandOverrideApplication.TheDocument, templateName);
               newCategoryTemplate?.UpdateCategoryList(IFCCommandOverrideApplication.TheDocument);
               listBox_MappingTemplates.Items.Add(templateName);
               listBox_MappingTemplates.SelectedItem = newCategoryTemplate.Name;
            }
         }
      }

      private void ExportFlagClick(object sender, RoutedEventArgs e)
      {
         CategoryMappingManager currentManager = dataGrid_CategoryMapping.ItemsSource as CategoryMappingManager;

         CheckBox checkBox = sender as CheckBox;
         if (checkBox == null)
            return;

         CategoryMappingNode currMappingNode = checkBox.DataContext as CategoryMappingNode;
         if (currMappingNode == null)
            return;

         IFCMappingInfo mappingInfo = currMappingNode.MappingInfo;
         if (mappingInfo == null)
            return;

         bool exportFlag = mappingInfo.ExportFlag ?? true;

         if (currMappingNode.HasChildren)
         {
            // If the parent is expanded, then we will skip invisible children.
            // Otherwise we will include them in the search.
            bool isExpanded = currMappingNode.IsExpanded;

            foreach (CategoryMappingNode child in currMappingNode.Children)
            {
               IFCMappingInfo childMappingInfo = child.MappingInfo;
               if (childMappingInfo == null)
                  continue;

               if (isExpanded && !currentManager.Contains(child))
                  continue;

               childMappingInfo.ExportFlag = exportFlag;
            }
         }
         else if (currMappingNode.Parent != null && exportFlag)
         {
            IFCMappingInfo parentMappingInfo = currMappingNode.Parent.MappingInfo;
            if (parentMappingInfo != null)
               parentMappingInfo.ExportFlag = true;
         }

         if (currMappingNode.Parent != null)
         {
            // Set node's parent checkbox to indeterminate state if needed
            IFCMappingInfo parentMappingInfo = currMappingNode.Parent.MappingInfo;
            if (parentMappingInfo != null)
               parentMappingInfo.ExportFlag = UpdateCheckboxFlag(currMappingNode.Parent.Children);
         }
         
         ExportFlagAll = UpdateCheckboxFlag(currentManager.ToList());
      }

      /// <summary>
      /// Updates indeterminate state if needed.
      /// </summary>
      private bool? UpdateCheckboxFlag(List<CategoryMappingNode> nodesList)
      {
         bool existsUnchecked = nodesList?.Any(node => (node.MappingInfo?.ExportFlag == false)) ?? false;
         if (existsUnchecked)
            return null;
         
         return true;
      }

      private void ExportFlagAllClick()
      {
         bool? newState = ExportFlagAll;
         if (!newState.HasValue)
            return;

         CategoryMappingManager currentManager = dataGrid_CategoryMapping.ItemsSource as CategoryMappingManager;
         if (currentManager == null)
            return;

         foreach (CategoryMappingNode currMappingNode in currentManager.Data)
         {
            if (currMappingNode == null)
               continue;

            if (currMappingNode.IsVisible == false)
               continue;

            foreach (var childNode in currMappingNode.Children)
            {
               // Skip expanded but not visible children.
               // Note that in case of newState == false we don't skip any children
               // because unchecked parent can't have checked children
               if (newState.Value
                  && currMappingNode.IsExpanded && !currentManager.Contains(childNode))
                  continue;

               if (childNode?.MappingInfo != null)
                  childNode.MappingInfo.ExportFlag = newState.Value;
            }

            IFCMappingInfo mappingInfo = currMappingNode.MappingInfo;
            if (mappingInfo == null)
               continue;

            mappingInfo.ExportFlag = newState.Value;
         }
      }

      private void ExpandAllClick(object sender, RoutedEventArgs e)
      {
         bool? newState = (sender as System.Windows.Controls.Primitives.ToggleButton)?.IsChecked;
         if (!newState.HasValue)
            return;

         CategoryMappingManager currentManager = dataGrid_CategoryMapping.ItemsSource as CategoryMappingManager;

         foreach (CategoryMappingNode currMappingNode in currentManager.Data)
         {
            if (currMappingNode == null)
               continue;

            if (currMappingNode.IsVisible == false)
               continue;

            currMappingNode.IsExpanded = newState.Value;
         }
      }

      private void button_Save_Click(object sender, RoutedEventArgs e)
      {
         UpdateTemplateFromGrid();

         CommitTransactionGroup();
         StartTransactionGroup();
      }

      private void button_Ok_Click(object sender, RoutedEventArgs e)
      {
         UpdateTemplateFromGrid();
         CommitTransactionGroup();
         Close();
      }

      private void StartTransactionGroup()
      {
         // Restart the transactions
         if (!groupTransaction.HasStarted())
            groupTransaction.Start();

         if (!templateTransaction.HasStarted())
            templateTransaction.Start();
      }

      private void CommitTransactionGroup()
      {
         // Save template changes
         if (templateTransaction.HasStarted())
            templateTransaction.Commit();

         // Save all the dialog changes
         if (groupTransaction.HasStarted())
            groupTransaction.Assimilate();
      }

      private void DiscardTransactionsGroup()
      {
         // Roll back template changes
         if (templateTransaction.HasStarted())
            templateTransaction.RollBack();

         // Roll back all the dialog changes after the last 'Save' pressing
         if (groupTransaction.HasStarted())
            groupTransaction.RollBack();
      }

      private void button_Cancel_Click(object sender, RoutedEventArgs e)
      {
         DiscardTransactionsGroup();
         Close();
      }

      private void ChildWindow_Closing(object sender, CancelEventArgs e)
      {
         DiscardTransactionsGroup();
      }

      private void UpdateTemplateFromGrid(string templateName = null)
      {
         IFCCategoryTemplate templateToUpdate = null;

         // Update Current template is the name isn't specified
         if (templateName == null)
            templateToUpdate = GetCurrentTemplate();
         else
            templateToUpdate = GetTemplateByName(templateName);

         if (templateToUpdate == null)
            return;

         IDictionary<ExportIFCCategoryKey, ExportIFCCategoryInfo> gridMap = new Dictionary<ExportIFCCategoryKey, ExportIFCCategoryInfo>();

         // Not supported yet.
         string presentationLayerName = string.Empty;

         CategoryMappingManager currentManager = dataGrid_CategoryMapping.ItemsSource as CategoryMappingManager;
         foreach (CategoryMappingNode node in currentManager.Data)
         {
            IFCMappingInfo info = node.MappingInfo;
            gridMap.Add(new ExportIFCCategoryKey(info.CategoryName, "", info.CustomSubCategoryId),
               new ExportIFCCategoryInfo(info.ExportFlag ?? true, info.IfcClass, info.PredefinedType, info.UserDefinedType, presentationLayerName));
            foreach (CategoryMappingNode subNode in node.Children)
            {
               IFCMappingInfo subInfo = subNode.MappingInfo;
               string subCategoryNameToUse = (info.CustomSubCategoryId == CustomSubCategoryId.None) ?
                  subInfo.CategoryName : string.Empty;
               gridMap.Add(new ExportIFCCategoryKey(info.CategoryName, subCategoryNameToUse, subInfo.CustomSubCategoryId),
                  new ExportIFCCategoryInfo(subInfo.ExportFlag ?? true, subInfo.IfcClass, subInfo.PredefinedType, subInfo.UserDefinedType, presentationLayerName));
            }
         }
         templateToUpdate.SetMappingInfo(gridMap);
      }

      private void EntityPicker(object sender, RoutedEventArgs e)
      {
         CategoryMappingNode currMappingNode = dataGrid_CategoryMapping.SelectedItem as CategoryMappingNode;
         if (currMappingNode == null)
            return;

         IFCMappingInfo mappingInfo = currMappingNode.MappingInfo;
         if (mappingInfo == null)
            return;

         EntityTree entityTree = new EntityTree(currMappingNode)
         {
            Owner = this,
            Title = Properties.Resources.IFCEntitySelection
         };
         entityTree.PredefinedTypeTreeView.Visibility = System.Windows.Visibility.Visible;

         bool? ret = entityTree.ShowDialog();
         if (ret.HasValue && ret.Value == true)
         {
            mappingInfo.IfcClass = entityTree.GetSelectedEntity();
            mappingInfo.PredefinedType = entityTree.GetSelectedPredefinedType();
         }
      }

      private void button_Export_Click(object sender, RoutedEventArgs e)
      {
         IFCCategoryTemplate currTemplate = GetCurrentTemplate();
         if (currTemplate == null)
            return;

         UpdateTemplateFromGrid();

         FileSaveDialog saveDialog = new FileSaveDialog(Properties.Resources.ExportCategoryMappingFilter);
         saveDialog.Title = Properties.Resources.ExportCategoryMappingDialogName;
         saveDialog.InitialFileName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + currTemplate.Name + ".txt";

         if (saveDialog.Show() == ItemSelectionDialogResult.Confirmed)
         {
            try
            {
               // TODO: Support (or warn) on cloud paths.
               ModelPath modelPath = saveDialog.GetSelectedModelPath();
               string fileName = ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath);
               currTemplate.ExportToFile(IFCCommandOverrideApplication.TheDocument, fileName);
            }
            catch (Exception)
            {
               return;
            }
         }
      }

      private void button_Import_Click(object sender, RoutedEventArgs e)
      {
         FileOpenDialog openDialog = new FileOpenDialog(Properties.Resources.ExportCategoryMappingFilter);
         openDialog.Title = Properties.Resources.ImportCategoryMappingDialogName;
         
         if (openDialog.Show() == ItemSelectionDialogResult.Confirmed)
         {
            // TODO: Support cloud paths.
            ModelPath modelPath = openDialog.GetSelectedModelPath();
            string fileName = ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath);
            string fileNameOnly = Path.GetFileNameWithoutExtension(fileName);

            IFCCategoryTemplateData data = new IFCCategoryTemplateData(fileNameOnly, GetAllTemplateNames());
            string uniqueTemplateName = data.MakeUniqueTemplateName();
            if (string.IsNullOrWhiteSpace(uniqueTemplateName))
               return;

            try
            {
               IFCCategoryTemplate importedTemplate = IFCCategoryTemplate.ImportFromFile(IFCCommandOverrideApplication.TheDocument, fileName, uniqueTemplateName);
               
               if (importedTemplate == null)
               {
                  using (Autodesk.Revit.UI.TaskDialog taskDialog = new Autodesk.Revit.UI.TaskDialog(Properties.Resources.IFCExport))
                  {
                     taskDialog.MainInstruction = Properties.Resources.IFCInvalidCategoryMappingFile;
                     taskDialog.MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconWarning;
                     TaskDialogResult taskDialogResult = taskDialog.Show();
                  }
               }

               importedTemplate?.UpdateCategoryList(IFCCommandOverrideApplication.TheDocument);
            }
            catch (Exception)
            {
               return;
            }

            listBox_MappingTemplates.Items.Add(uniqueTemplateName);
            listBox_MappingTemplates.SelectedItem = uniqueTemplateName;
         }

      }

      private void button_Copy_Click(object sender, RoutedEventArgs e)
      {
         IFCCategoryTemplate currentTemplate = GetCurrentTemplate();
         if (currentTemplate == null)
            return;

         UpdateTemplateFromGrid();

         IFCCategoryTemplateData data = new IFCCategoryTemplateData(currentTemplate.Name, GetAllTemplateNames());
         data.MakeUniqueTemplateName();

         IFCCopyCategoryTemplate copyTemplateDialog = new IFCCopyCategoryTemplate(data)
         {
            Owner = this
         };

         bool? ret = copyTemplateDialog.ShowDialog();
         if (ret.HasValue && ret.Value == true)
         {
            string copyTemplateName = copyTemplateDialog.Data.NewName;
            try
            {
               IFCCategoryTemplate importedTemplate = currentTemplate.CopyTemplate(IFCCommandOverrideApplication.TheDocument, copyTemplateName);
            }
            catch (Exception)
            {
               return;
            }
            
            listBox_MappingTemplates.Items.Add(copyTemplateName);
            listBox_MappingTemplates.SelectedItem = copyTemplateName;
         }
      }

      private void button_Rename_Click(object sender, RoutedEventArgs e)
      {
         IFCCategoryTemplate currentTemplate = GetCurrentTemplate();
         if (currentTemplate == null)
            return;

         string previousName = currentTemplate.Name;
         IFCCategoryTemplateData data = new IFCCategoryTemplateData(previousName, IFCCategoryTemplate.ListNames(IFCExport.TheDocument));

         IFCRenameCategoryTemplate renameTemplateDialog = new IFCRenameCategoryTemplate(data);
         renameTemplateDialog.Owner = this;
         bool? ret = renameTemplateDialog.ShowDialog();
         if (ret.HasValue && ret.Value && !string.IsNullOrWhiteSpace(renameTemplateDialog.Data.NewName))
         {
            string newName = renameTemplateDialog.Data.NewName;
            currentTemplate.Name = newName;
            int index = listBox_MappingTemplates.SelectedIndex;
            listBox_MappingTemplates.Items[index] = newName;
            listBox_MappingTemplates.SelectedItem = newName;
         }
      }

      private ExportIFCCategoryKey CreateKeyFromMappingNode(CategoryMappingNode currMappingNode)
      {
         IFCMappingInfo info = currMappingNode?.MappingInfo;
         if (info == null)
         {
            return null;
         }

         IFCMappingInfo parentInfo = currMappingNode.Parent?.MappingInfo;

         string categoryName = (parentInfo == null) ? info.CategoryName : parentInfo.CategoryName;
         string subCategoryName = (parentInfo == null || info.CustomSubCategoryId != CustomSubCategoryId.None) ? string.Empty : info.CategoryName;

         return new ExportIFCCategoryKey(categoryName, subCategoryName, info.CustomSubCategoryId);
      }

      private void ResetOneRow(IFCCategoryTemplate currentTemplate, CategoryMappingNode currMappingNode, bool canExport)
      {
         IFCMappingInfo info = currMappingNode?.MappingInfo;
         if (info == null)
         {
            return;
         }
         
         ExportIFCCategoryKey key = CreateKeyFromMappingNode(currMappingNode);
         if (key == null)
         {
            return;
         }

         ExportIFCCategoryInfo resetInfo = currentTemplate?.ResetCategoryToDefault(key) ?? null;
         if (resetInfo == null)
         {
            return;
         }

         info.ExportFlag = canExport && resetInfo.IFCExportFlag;
         info.IfcClass = resetInfo.IFCEntityName;
         info.PredefinedType = resetInfo.IFCPredefinedType;
      }

      private void ResetOneRowWithChildren(IFCCategoryTemplate currentTemplate, CategoryMappingNode currMappingNode)
      {
         // If currMappingNode represents a parent node, then parentFlag controls
         // the reset of the children, so that a child isn't set to export if the
         // parent isn't set to export.

         // If currMappingNode represents a child node, then originalParentFlag
         // controls the reset of this node, so that it isn't set to export if the
         // parent isn't set to export.

         bool originalParentFlag = currMappingNode.Parent?.MappingInfo?.ExportFlag ?? true;
         ResetOneRow(currentTemplate, currMappingNode, originalParentFlag);

         bool parentFlag = currMappingNode.MappingInfo?.ExportFlag ?? true;

         if (currMappingNode.HasChildren)
         {
            CategoryMappingManager currentManager = dataGrid_CategoryMapping.ItemsSource as CategoryMappingManager;

            // If the parent is expanded, then we will skip invisible children.
            // Otherwise we will include them in the search.
            bool isExpanded = currMappingNode.IsExpanded;

            foreach (CategoryMappingNode child in currMappingNode.Children)
            {
               IFCMappingInfo childMappingInfo = child.MappingInfo;
               if (childMappingInfo == null)
                  continue;

               if (isExpanded && !currentManager.Contains(child))
                  continue;

               ResetOneRow(currentTemplate, child, parentFlag);
            }
         }

         if (currMappingNode.Parent != null)
         {
            // Set node's parent checkbox to indeterminate state if needed
            IFCMappingInfo parentMappingInfo = currMappingNode.Parent.MappingInfo;
            if (parentMappingInfo != null)
               parentMappingInfo.ExportFlag = UpdateCheckboxFlag(currMappingNode.Parent.Children);
         }
      }
         
      private void button_Reset_Click(object sender, RoutedEventArgs e)
      {
         IFCCategoryTemplate currentTemplate = GetCurrentTemplate();
         if (currentTemplate == null)
            return;

         CategoryMappingNode currMappingNode = dataGrid_CategoryMapping.SelectedItem as CategoryMappingNode;
         if (currMappingNode == null)
            return;

         ResetOneRowWithChildren(currentTemplate, currMappingNode);

         CategoryMappingManager currentManager = dataGrid_CategoryMapping.ItemsSource as CategoryMappingManager;
         if (currentManager == null)
            return;

         ExportFlagAll = UpdateCheckboxFlag(currentManager.ToList());
      }

      private void button_ResetAll_Click(object sender, RoutedEventArgs e)
      {
         IFCCategoryTemplate currentTemplate = GetCurrentTemplate();
         if (currentTemplate == null)
         {
            return;
         }

         CategoryMappingManager currentManager = dataGrid_CategoryMapping.ItemsSource as CategoryMappingManager;
         foreach (CategoryMappingNode currMappingNode in currentManager.Data)
         {
            if ((currMappingNode?.IsVisible ?? false) == false)
               continue;

            ResetOneRowWithChildren(currentTemplate, currMappingNode);
         }

         ExportFlagAll = UpdateCheckboxFlag(currentManager.ToList());
      }

      private void button_Delete_Click(object sender, RoutedEventArgs e)
      {
         IFCCategoryTemplate currentTemplate = GetCurrentTemplate();
         if (currentTemplate == null)
            return;
         
         IFCDeleteCategoryTemplate deleteTemplateDialog = new IFCDeleteCategoryTemplate(currentTemplate.Name);
         deleteTemplateDialog.Owner = this;
         bool? ret = deleteTemplateDialog.ShowDialog();
         if (ret.HasValue && ret.Value == true)
         {
            listBox_MappingTemplates.Items.Remove(currentTemplate.Name);
            IFCCommandOverrideApplication.TheDocument.Delete(currentTemplate.Id);
            listBox_MappingTemplates.SelectedIndex = 0;
         }
      }
   }


   /// <summary>
   /// Represents the row in category mapping table
   /// </summary>
   public class CategoryMappingNode : INotifyPropertyChanged
   {
      /// <summary>
      /// Default constructor
      /// </summary>
      public CategoryMappingNode()
      {
         Children = new List<CategoryMappingNode>();
      }

      /// <summary>
      /// The mapping info from Revit category to Ifc entity
      /// </summary>
      public IFCMappingInfo MappingInfo { get; set; }

      /// <summary>
      /// Parent node
      /// </summary>
      public CategoryMappingNode Parent { get; set; }

      /// <summary>
      /// Children nodes
      /// </summary>
      public List<CategoryMappingNode> Children { get; set; }

      /// <summary>
      /// Reference to data manager
      /// </summary>
      public CategoryMappingManager DataManager { get; set; }

      /// <summary>
      /// The node level (number of parents)
      /// </summary>
      private int _level = -1;
      public int Level
      {
         get
         {
            if (_level == -1)
            {
               _level = (Parent != null) ? Parent.Level + 1 : 0;
            }
            return _level;
         }
      }

      /// <summary>
      /// If the node is expanded (otherwise - it is collapsed)
      /// </summary>
      private bool _expanded = true;
      public bool IsExpanded
      {
         get { return _expanded; }
         set
         {
            if (_expanded != value)
            {
               _expanded = value;
               if (_expanded == true)
                  Expand();
               else
                  Collapse();

               OnPropertyChanged();
            }
         }
      }

      /// <summary>
      /// If the node is visible (otherwise - its parent is collapsed)
      /// </summary>
      private bool _visible = false;

      public bool IsVisible
      {
         get { return _visible; }
         set
         {
            if (_visible != value)
            {
               _visible = value;
               if (_visible)
                  ShowChildren();
               else
                  HideChildren();
            }
         }
      }

      /// <summary>
      /// If the node is hidden by filter
      /// </summary>
      public bool IsHiddenByFilter { get; set; } = false;

      public event PropertyChangedEventHandler PropertyChanged;
      protected void OnPropertyChanged([CallerMemberName] string name = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }

      /// <summary>
      /// If the node has any children (controls expand button visibility)
      /// </summary>
      public bool HasChildren { get { return Children.Count > 0; } }

      /// <summary>
      /// Get the list of visible children nodes
      /// </summary>
      public IEnumerable<CategoryMappingNode> VisibleDescendants
      {
         get
         {
            return Children
                .Where(x => x.IsVisible)
                .SelectMany(x => (new[] { x }).Concat(x.VisibleDescendants));
         }
      }

      /// <summary>
      /// Add child node
      /// </summary>
      public void AddChild(CategoryMappingNode node)
      {
         node.Parent = this;
         Children.Add(node);
      }


      /// <summary>
      /// Collapse this node (IsExpanded has been switched to false)
      /// </summary>
      private void Collapse()
      {
         DataManager.RemoveChildren(this);
         foreach (CategoryMappingNode child in Children)
            child.IsVisible = false;
      }

      /// <summary>
      /// Collapse this node (IsExpanded has been switched to true)
      /// </summary>
      private void Expand()
      {
         DataManager.AddChildren(this);
         foreach (CategoryMappingNode child in Children)
         {
            if (!child.IsHiddenByFilter)
               child.IsVisible = true;
         }
      }

      /// <summary>
      /// Hide children of expanded node (IsVisible has been switched to false)
      /// </summary>
      private void HideChildren()
      {
         if (IsExpanded)
         {
            DataManager.RemoveChildren(this);
            foreach (CategoryMappingNode child in Children)
               child.IsVisible = false;
         }
      }

      /// <summary>
      /// Show children of expanded node (IsVisible has been switched to true)
      /// </summary>
      private void ShowChildren()
      {
         if (IsExpanded)
         {
            DataManager.AddChildren(this);
            foreach (CategoryMappingNode child in Children)
               child.IsVisible = true;
         }
      }
   }

   /// <summary>
   /// This class implements the logic to support hierarchical structure of the data grid
   /// Controls the data to show basing on node's IsExpanded property
   /// </summary>
   public class CategoryMappingManager : ObservableCollection<CategoryMappingNode>
   {
      /// <summary>
      /// Default constructor
      /// </summary>
      public CategoryMappingManager() { Data = new List<CategoryMappingNode>(); }

      /// <summary>
      /// Entire category tree
      /// </summary>
      public List<CategoryMappingNode> Data { get; set; }

      /// <summary>
      /// Fill the observable collection with all the visible entries
      /// </summary>
      public void Initialize()
      {
         this.Clear();
         foreach (CategoryMappingNode node in Data.Where(x => x.IsVisible).SelectMany(y => new[] { y }.Concat(y.VisibleDescendants)))
         {
            this.Add(node);
         }
      }

      /// <summary>
      /// Clear the observable collection
      /// </summary>
      public void Reset(bool resetVisibility)
      {
         this.Clear();

         if (resetVisibility)
         {
            foreach (CategoryMappingNode node in Data)
            {
               node.IsVisible = false;
               node.IsExpanded = false;
               node.IsHiddenByFilter = false;

               foreach (CategoryMappingNode child in node.Children)
               {
                  child.IsHiddenByFilter = false;
               }
            }

         }
      }

      /// <summary>
      /// Add children of the node to observable collection
      /// </summary>
      public void AddChildren(CategoryMappingNode node)
      {
         if (!this.Contains(node))
            return;

         int parentIndex = this.IndexOf(node);
         foreach (CategoryMappingNode child in node.Children)
         {
            if (!child.IsHiddenByFilter)
            {
               parentIndex += 1;
               this.Insert(parentIndex, child);
            }
         }
      }

      /// <summary>
      /// Remove children of the node from observable collection
      /// </summary>
      public void RemoveChildren(CategoryMappingNode node)
      {
         foreach (CategoryMappingNode child in node.Children)
         {
            if (this.Contains(child))
               this.Remove(child);
         }
      }
   }

   #region Converters
   /// <summary>
   /// Converts node level value to row indent
   /// </summary>
   public class LevelToMarginConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         if (value is int)
            return new Thickness((int)value * 15, 0, 0, 0);

         return null;
      }

      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }

   /// <summary>
   /// Converts node level value to row indent
   /// </summary>
   public class CheckBoxLevelToMarginConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         if (value is int)
            return new Thickness((int)value * 12, 0, 0, 0);

         return null;
      }

      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }

   /// <summary>
   /// Extracts the row index from the DataGridRow item.
   /// It is used too set AutomationId for valid journal playback.
   /// </summary>
   public class RowIndexConverter : IValueConverter
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

   /// <summary>
   /// Replaces the ifc category name with <By Category> if it is empty
   /// </summary>
   public class IFCClassNameConverter : IValueConverter
   {
      object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         string ifcClassName = (string)value;
         if (string.IsNullOrEmpty(ifcClassName))
         {
            return Resources.ByCategory;
         }

         return ifcClassName;
      }

      object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         return string.Empty;
      }
   }
   #endregion

}