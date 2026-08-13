using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.UI.Windows;
using BIM.IFC.Export.UI.Properties;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Input;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Interaction logic for IFCPropertyMapping.xaml
   /// </summary>
   public partial class IFCPropertyMapping : ChildWindow, INotifyPropertyChanged
   {
      /// <summary>
      /// The Mapping Model
      /// </summary>
      private IFCPropertyMappingModel _model = new();

      /// <summary>
      /// The IFC Export Configuration
      /// </summary>
      private IFCExportConfiguration _ifcExportConfiguration = null;

      /// <summary>
      /// The Property Setup info used by TreeView
      /// </summary>
      public ObservableCollection<SetupMappingInfo> SetupInfos { get; set; } = new();

      /// <summary>
      /// Selected Property Setup.
      /// </summary>
      private PropertySetupType _selectedPropertySetup;

      /// <summary>
      /// Property Setups.
      /// </summary>
      private List<PropertySetupType> _propertySetups = new();

      /// <summary>
      /// Property Set list.
      /// </summary>
      private static PSetMappingInfo _selectedPropertySet;
      public static PSetMappingInfo SelectedPropertySet
      {
         get => _selectedPropertySet;
         private set => _selectedPropertySet = value;
      }

      /// <summary>
      /// Property mappings
      /// </summary>
      public ObservableCollection<PropertyMappingInfo> ObservableProperties { get; set; } = new();

      /// <summary>
      /// IFC schema list.
      /// </summary>
      private IFCVersion _selectedIfcSchema = IFCVersion.IFC2x3;
      public IFCVersion SelectedIfcSchema
      {
         get { return _selectedIfcSchema; }
         set
         {
            _selectedIfcSchema = value;
            OnPropertyChanged();
         }
      }
      private List<IFCVersion> _ifcSchemas;
      public List<IFCVersion> IfcSchemas
      {
         get { return _ifcSchemas; }
      }

      private bool? m_ExportFlagAll = true;

      /// <summary>
      /// Flag to determine if all properties are exported or not.
      /// </summary>
      public bool? ExportFlagAll
      {
         get { return m_ExportFlagAll; }
         set
         {
            m_ExportFlagAll = value;
            OnPropertyChanged();
            ExportFlagAllClick();
         }
      }

      private string m_filterTextPropertySet;
      public string FilterTextPropertySet
      {
         get { return m_filterTextPropertySet; }
         set
         {
            m_filterTextPropertySet = value;
            OnPropertyChanged();
            foreach (var setupInfo in SetupInfos)
               setupInfo.PropertySetCollection.Refresh();
         }
      }

      private bool _isRevitThemeDark = false;

      /// <summary>
      /// Flag to determine if Dark theme is applied based on the Revit and system settings.
      /// </summary>
      public bool IsRevitThemeDark
      {
         get { return _isRevitThemeDark; }
         set
         {
            if (_isRevitThemeDark != value)
            {
               _isRevitThemeDark = value;
               OnPropertyChanged();
            }
         }
      }

      private bool _isCustomConfiguration = true;

      /// <summary>
      /// Flag to determine if the current configuration is custom (modifiable).
      /// True when the configuration can be modified, false when it's built-in.
      /// </summary>
      public bool IsCustomConfiguration
      {
         get { return _isCustomConfiguration; }
         set
         {
            if (_isCustomConfiguration != value)
            {
               _isCustomConfiguration = value;
               OnPropertyChanged();
            }
         }
      }

      public event PropertyChangedEventHandler PropertyChanged;
      protected void OnPropertyChanged([CallerMemberName] string name = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }

      private TransactionGroup groupTransaction;
      private Transaction templateTransaction;

      /// <summary>
      /// Cache for IFCParameterTemplate objects for built-in configurations.
      /// </summary>
      private readonly Dictionary<string, IFCParameterTemplate> _builtInTemplateCache = new();

      public ObservableCollection<string> RevitCategoryFilters { get; } = new();
      private readonly Dictionary<string, CategoryParameterSet> _categoryParameterLookup = new(StringComparer.CurrentCultureIgnoreCase);
      private readonly List<PropertyMappingInfo> _subscribedPropertyMappings = new();
      private readonly PropertyChangedEventHandler _propertyMappingChangedHandler;
      private string _currentCategoryFilter = Properties.Resources.DefaultCategoryFilterTxt;
      private bool _suppressCategoryFilterSelectionChanged;
      private bool _suppressPropertyMappingChanged;

      /// <summary>
      /// Constructor.
      /// </summary>
      public IFCPropertyMapping(Window owner, IFCExportConfiguration configuration)
      {
         InitializeComponent();
         _propertyMappingChangedHandler = OnPropertyMappingChanged;
         Owner = owner;
         _ifcExportConfiguration = configuration;
         IsCustomConfiguration = !configuration.IsBuiltIn;
         IsRevitThemeDark = UIThemeManager.CurrentTheme == UITheme.Dark;
         DataContext = this;

         Document doc = IFCCommandOverrideApplication.TheDocument;
         groupTransaction = new TransactionGroup(doc);
         templateTransaction = new Transaction(doc, Properties.Resources.ModifyIFCPropertyMapping);
         StartTransactionGroup();

         IFCRevitPropertySelector.InitEntityToCategoriesCache(_ifcExportConfiguration.CategoryMapping);

         _model.ClearCache();

         InitializeSchemaList();
         InitializePropertySetupsList();

         IFCParameterTemplate currentTemplate = GetMappingTemplateFromConfiguration(doc, configuration);

         InitializeTemplateList(currentTemplate?.Name);
         InitializeCategoryFilter();

         UpdateControlsEnabledState();
      }

      /// <summary>
      /// Initializes the mapping templates listbox.
      /// </summary>
      private void InitializeTemplateList(string activeTemplateName)
      {
         listBox_MappingTemplates.Items.Clear();

         if (!IsCustomConfiguration)
         {
            listBox_MappingTemplates.Items.Add(activeTemplateName);
            listBox_MappingTemplates.SelectedItem = activeTemplateName;
            return;
         }

         IFCParameterTemplate inSessionTemplate = IFCParameterTemplate.GetOrCreateInSessionTemplate(IFCCommandOverrideApplication.TheDocument);
         if (inSessionTemplate == null)
            return;

         string inSessionName = inSessionTemplate.Name;
         listBox_MappingTemplates.Items.Add(inSessionName);
         IList<string> templateNames = IFCParameterTemplate.ListNames(IFCCommandOverrideApplication.TheDocument);
         foreach (string name in templateNames)
            listBox_MappingTemplates.Items.Add(name);

         if (activeTemplateName == null || !templateNames.Contains(activeTemplateName))
            listBox_MappingTemplates.SelectedItem = inSessionName;
         else
            listBox_MappingTemplates.SelectedItem = activeTemplateName;
      }

      private void InitializeCategoryFilter()
      {
         _suppressCategoryFilterSelectionChanged = true;

         string previousSelection = _currentCategoryFilter;

         RevitCategoryFilters.Clear();
         _categoryParameterLookup.Clear();

         RevitCategoryFilters.Add(Properties.Resources.DefaultCategoryFilterTxt);

         if (SelectedPropertySet?.PropertyInfos != null &&
            SelectedPropertySet.ParentSetup?.PropertySetup == PropertySetupType.RevitElementParameters)
         {
            IReadOnlyDictionary<ElementId, List<RevitParameterData>> categoryParameters =
               IFCRevitPropertySelector.GetCategoryParametersCacheSnapshot();

            if (categoryParameters != null)
            {
               foreach (var entry in categoryParameters)
               {
                  string categoryName = GetCategoryDisplayName(entry.Key);
                  if (string.IsNullOrEmpty(categoryName))
                     continue;

                  if (_categoryParameterLookup.TryGetValue(categoryName, out CategoryParameterSet existingSet))
                     existingSet.AddParameters(entry.Value);
                  else
                     _categoryParameterLookup.Add(categoryName, new CategoryParameterSet(entry.Value));
               }

               List<string> categoryNames = new();
               foreach (var entry in _categoryParameterLookup.ToList())
               {
                  if (!(SelectedPropertySet.PropertyInfos?.Any(entry.Value.Contains) ?? false))
                  {
                     _categoryParameterLookup.Remove(entry.Key);
                     continue;
                  }

                  categoryNames.Add(entry.Key);
               }

               foreach (string categoryName in categoryNames.OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase))
                  RevitCategoryFilters.Add(categoryName);
            }
         }

         comboBox_CategoryName.ItemsSource = RevitCategoryFilters;
         string selectionToApply = (!string.IsNullOrEmpty(previousSelection) && RevitCategoryFilters.Contains(previousSelection)) ?
            previousSelection : Properties.Resources.DefaultCategoryFilterTxt;
         comboBox_CategoryName.SelectedItem = selectionToApply;

         _currentCategoryFilter = selectionToApply;
         _suppressCategoryFilterSelectionChanged = false;
      }

      private void button_Add_Click(object sender, RoutedEventArgs e)
      {
         IFCTemplateData data = new(Properties.Resources.NewTemplateDefaultName,
            IFCParameterTemplate.ListNames(IFCCommandOverrideApplication.TheDocument),
            isCategoryMapping: false, IFCTemplateData.DialogTypeEnum.Template);
         IFCNewTemplate newTempalteDialog = new(data);
         newTempalteDialog.Owner = this;
         bool? ret = newTempalteDialog.ShowDialog();
         if (ret.HasValue && ret.Value == true)
         {
            string templateName = newTempalteDialog.Data.NewName;
            if (!string.IsNullOrEmpty(templateName) && !listBox_MappingTemplates.Items.Contains(templateName))
            {
               IFCParameterTemplate newParameterTemplate = IFCParameterTemplate.Create(IFCCommandOverrideApplication.TheDocument, templateName);
               if (newParameterTemplate == null)
                  return;

               listBox_MappingTemplates.Items.Add(templateName);
               listBox_MappingTemplates.SelectedItem = newParameterTemplate.Name;
            }
         }
      }

      private void button_Import_Click(object sender, RoutedEventArgs e)
      {
         FileOpenDialog openDialog = new FileOpenDialog(Properties.Resources.ExportPropertyMappingFilter);
         openDialog.Title = Properties.Resources.ImportPropertyMappingDialogName;

         if (openDialog.Show() == ItemSelectionDialogResult.Confirmed)
         {
            // TODO: Support cloud paths.
            string fileName = ModelPathUtils.ConvertModelPathToUserVisiblePath(openDialog.GetSelectedModelPath());
            string uniqueTemplateName = GetUniqueNameFromFile(fileName, isCategoryMapping: false);
            if (string.IsNullOrWhiteSpace(uniqueTemplateName))
               return;

            try
            {
               IFCParameterTemplate importedTemplate = IFCParameterTemplate.ImportFromFile(IFCCommandOverrideApplication.TheDocument, fileName, uniqueTemplateName);

               if (importedTemplate != null && !importedTemplate.IsValidParameterMappingFile)
               {
                  using (Autodesk.Revit.UI.TaskDialog taskDialog = new Autodesk.Revit.UI.TaskDialog(Properties.Resources.IFCExport))
                  {
                     taskDialog.MainInstruction = Properties.Resources.IFCInvalidPropertyMappingFile;
                     taskDialog.MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconWarning;
                     taskDialog.TitleAutoPrefix = false;
                     taskDialog.CommonButtons = Autodesk.Revit.UI.TaskDialogCommonButtons.Cancel;
                     TaskDialogResult taskDialogResult = taskDialog.Show();
                     if (taskDialogResult == TaskDialogResult.Cancel)
                        return;
                  }
               }
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
         IFCParameterTemplate currentTemplate = GetCurrentTemplate();
         if (currentTemplate == null)
            return;

         WriteMappingInfoToTemplate(currentTemplate);
         IFCTemplateData data = new(currentTemplate.Name, GetAllTemplateNames(),
            isCategoryMapping: false, IFCTemplateData.DialogTypeEnum.Template);
         data.MakeUniqueName();

         IFCCopyTemplate copyTemplateDialog = new IFCCopyTemplate(data)
         {
            Owner = this
         };

         bool? ret = copyTemplateDialog.ShowDialog();
         if (ret.HasValue && ret.Value == true)
         {
            string copyTemplateName = copyTemplateDialog.Data.NewName;
            try
            {
               IFCParameterTemplate importedTemplate = currentTemplate.CopyTemplate(IFCCommandOverrideApplication.TheDocument, copyTemplateName);
            }
            catch (Exception)
            {
               return;
            }

            listBox_MappingTemplates.Items.Add(copyTemplateName);
            listBox_MappingTemplates.SelectedItem = copyTemplateName;
         }
      }

      private void button_Save_Click(object sender, RoutedEventArgs e)
      {
         SaveDialogChanges();
         StartTransactionGroup();
      }

      private void button_Export_Click(object sender, RoutedEventArgs e)
      {
         IFCParameterTemplate currTemplate = GetCurrentTemplate();
         if (currTemplate == null)
            return;

         WriteMappingInfoToTemplate(currTemplate);

         FileSaveDialog saveDialog = new FileSaveDialog(Properties.Resources.ExportPropertyMappingFilter);
         saveDialog.Title = Properties.Resources.ExportPropertyMappingDialogName;
         saveDialog.InitialFileName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + currTemplate.Name + ".txt";

         if (saveDialog.Show() == ItemSelectionDialogResult.Confirmed)
         {
            try
            {
               // TODO: Support (or warn) on cloud paths.
               ModelPath modelPath = saveDialog.GetSelectedModelPath();
               string fileName = ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath);
               currTemplate.ExportToFile(fileName);
            }
            catch (Exception)
            {
               return;
            }
         }
      }

      private void button_Rename_Click(object sender, RoutedEventArgs e)
      {
         IFCParameterTemplate currentTemplate = GetCurrentTemplate();
         if (currentTemplate == null)
            return;

         string previousName = currentTemplate.Name;
         IFCTemplateData data = new(previousName, IFCParameterTemplate.ListNames(IFCExport.TheDocument),
            isCategoryMapping: false, IFCTemplateData.DialogTypeEnum.Template);

         IFCRenameTemplate renameTemplateDialog = new(data);
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

      private void button_Delete_Click(object sender, RoutedEventArgs e)
      {
         IFCParameterTemplate currentTemplate = GetCurrentTemplate();
         if (currentTemplate == null)
            return;

         IFCDeleteTemplate deleteTemplateDialog = new IFCDeleteTemplate(currentTemplate.Name);
         deleteTemplateDialog.Owner = this;
         bool? ret = deleteTemplateDialog.ShowDialog();
         if (ret.HasValue && ret.Value == true)
         {
            listBox_MappingTemplates.Items.Remove(currentTemplate.Name);
            IFCCommandOverrideApplication.TheDocument.Delete(currentTemplate.Id);
            listBox_MappingTemplates.SelectedIndex = 0;
         }
      }

      /// <summary>
      /// Returns true if the name is valid
      /// </summary>
      public static bool IsValidName(string templateName, IList<string> existingNames)
      {
         templateName = templateName?.TrimStart()?.TrimEnd();
         return (!(string.IsNullOrWhiteSpace(templateName)
            || (existingNames?.Contains(templateName, System.StringComparer.OrdinalIgnoreCase) ?? false)))
            && IFCParameterTemplate.IsValidName(IFCCommandOverrideApplication.TheDocument, templateName);
      }

      private void listBox_MappingTemplates_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
      {
         // Save current template before switching to new one
         if (e.RemovedItems.Count > 0)
         {
            string prevTemplateName = e.RemovedItems[0] as string;
            if (!string.IsNullOrEmpty(prevTemplateName))
            {
               WriteMappingInfoToTemplate(prevTemplateName);

               if (templateTransaction.HasStarted())
                  templateTransaction.Commit();

               if (templateTransaction.HasEnded())
                  templateTransaction.Start();
            }
         }

         SetSelectedPropertySet(null);

         IFCParameterTemplate currTemplate = GetCurrentTemplate();
         if (currTemplate == null)
            return;

         UpdateTemplateControls(currTemplate);

         InitializeSetupInfo(currTemplate);

         UpdateControlsVisibilityState(VisibilityState.NothingSelected);
         UpdateSelectedTemplateCaption();
      }

      private void UpdateSelectedTemplateCaption()
      {
         string templateName = listBox_MappingTemplates.SelectedItem?.ToString();
         textBlock_SelectedTemplateName.Text = string.Format("{0} {1}", templateName, Properties.Resources.TemplateSelections);
      }

      public void InitializeSetupInfo(IFCParameterTemplate template)
      {
         PopulateSetupInfo(SelectedIfcSchema);
         ReadMappingInfoFromTemplate(template);
      }

      private Dictionary<PropertySetupType, HashSet<string>> CreateTemplateDataCache(IFCParameterTemplate template)
      {
         Dictionary<PropertySetupType, HashSet<string>> _templatePsets = new();
         if (template == null)
            return _templatePsets;

         foreach (PropertySetupType propertySetup in _propertySetups)
         {
            IList<string> templatePSetNames = template.GetPropertySetNames(propertySetup, PropertySelectionType.All);
            _templatePsets.Add(propertySetup, templatePSetNames.ToHashSet());
         }

         return _templatePsets;
      }

      public void PopulateSetupInfo(IFCVersion ifcVersion)
      {
         Dictionary<string, bool> expandedStates = SaveExpandedState();
         string previouslySelectedPsetName = SelectedPropertySet?.Name;
         PropertySetupType? previouslySelectedSetup = SelectedPropertySet?.ParentSetup?.PropertySetup;

         SetSelectedPropertySet(null);
         SetupInfos.Clear();

         foreach (PropertySetupType propertySetup in _propertySetups)
            SetupInfos.Add(CreateSetupInfo(propertySetup, ifcVersion));

         // The SetupInfos was reinitialized, so we need to restore the selected property set
         // and the expanded states of the tree view items
         RestoreSelectedPSet(previouslySelectedSetup, previouslySelectedPsetName);
         RestoreExpandedState(expandedStates);
      }

      private void RestoreSelectedPSet(PropertySetupType? previousSetupType, string previousPsetName)
      {
         if (!previousSetupType.HasValue || string.IsNullOrEmpty(previousPsetName))
            return;

         foreach (var setupInfo in SetupInfos)
         {
            if (setupInfo.PropertySetup != previousSetupType.Value)
               continue;

            foreach (var psetInfo in setupInfo.PSetMappingInfos)
            {
               if (psetInfo.Name == previousPsetName)
               {
                  SetSelectedPropertySet(psetInfo);
                  return;
               }
            }
         }
      }

      private Dictionary<string, bool> SaveExpandedState()
      {
         var expandedStates = new Dictionary<string, bool>();
         foreach (SetupMappingInfo setupInfo in treeView_PropertySetups.Items)
         {
            if (setupInfo == null)
               continue;

            TreeViewItem treeViewItem = treeView_PropertySetups.ItemContainerGenerator.ContainerFromItem(setupInfo) as TreeViewItem;
            if (treeViewItem == null)
               continue;

            expandedStates[setupInfo.SetupName] = treeViewItem.IsExpanded;
         }

         return expandedStates;
      }

      private void RestoreExpandedState(Dictionary<string, bool> expandedStates)
      {
         if (expandedStates == null)
            return;

         foreach (SetupMappingInfo setupInfo in treeView_PropertySetups.Items)
         {
            if (setupInfo == null)
               continue;

            TreeViewItem treeViewItem = treeView_PropertySetups.ItemContainerGenerator.ContainerFromItem(setupInfo) as TreeViewItem;
            if (treeViewItem == null)
               continue;

            if (expandedStates.TryGetValue(setupInfo.SetupName, out var isExpanded))
               treeViewItem.IsExpanded = isExpanded;
         }
      }

      private void SetSelectedPropertySet(PSetMappingInfo newSelectedSet)
      {
         if (ReferenceEquals(SelectedPropertySet, newSelectedSet))
            return;

         UnsubscribeFromPropertyMappingChanges();

         _currentCategoryFilter = Properties.Resources.DefaultCategoryFilterTxt;
         SelectedPropertySet = newSelectedSet;

         if (SelectedPropertySet?.ParentSetup != null)
            _selectedPropertySetup = SelectedPropertySet.ParentSetup.PropertySetup;

         if (SelectedPropertySet?.PropertyInfos == null)
         {
            InitializePropertyDataGrid();
            return;
         }

         foreach (PropertyMappingInfo propertyInfo in SelectedPropertySet.PropertyInfos)
         {
            if (propertyInfo == null)
               continue;

            propertyInfo.PropertyChanged += _propertyMappingChangedHandler;
            _subscribedPropertyMappings.Add(propertyInfo);
         }

         InitializePropertyDataGrid();
      }

      private void UnsubscribeFromPropertyMappingChanges()
      {
         if (_subscribedPropertyMappings.Count == 0)
            return;

         foreach (PropertyMappingInfo propertyInfo in _subscribedPropertyMappings)
         {
            if (propertyInfo == null)
               continue;

            propertyInfo.PropertyChanged -= _propertyMappingChangedHandler;
         }

         _subscribedPropertyMappings.Clear();
      }

      private void OnPropertyMappingChanged(object sender, PropertyChangedEventArgs e)
      {
         if (_suppressPropertyMappingChanged)
            return;

         if (e == null)
            return;

         if (!string.Equals(e.PropertyName, nameof(PropertyMappingInfo.RevitPropertyName), StringComparison.Ordinal) &&
             !string.Equals(e.PropertyName, nameof(PropertyMappingInfo.RevitPropertyId), StringComparison.Ordinal))
            return;

         InitializePropertyDataGrid();
      }

      public SetupMappingInfo CreateSetupInfo(PropertySetupType propertySetup, IFCVersion ifcVersion)
      {
         SetupMappingInfo setupInfo = null;
         switch (propertySetup)
         {
            case PropertySetupType.IfcCommonPropertySets:
               setupInfo = _model.InitializeIFCCommonPropertySets(ifcVersion); break;
            case PropertySetupType.RevitElementParameters:
               setupInfo = _model.InitializeRevitPropertySetsList(); break;
            case PropertySetupType.IfcBaseQuantities:
               setupInfo = _model.InitializeBaseQuantities(ifcVersion); break;
            case PropertySetupType.RevitMaterialParameters:
               setupInfo = _model.InitializeMaterialPropertySets(); break;
            case PropertySetupType.RevitSchedules:
               setupInfo = _model.InitializeSchedules(); break;
            case PropertySetupType.UserDefinedPropertySets:
               setupInfo = _model.InitializeUserDefinedPropertySets(); break;
         }
         if (setupInfo != null)
            setupInfo.PropertySetCollection.Filter = FilterPropertySet;

         return setupInfo ?? new SetupMappingInfo();
      }

      public void ReadMappingInfoFromTemplate(IFCParameterTemplate template)
      {
         if (template == null)
            return;

         _suppressPropertyMappingChanged = true;
         try
         {
            Dictionary<PropertySetupType, HashSet<string>> templatePsetsNames = CreateTemplateDataCache(template);

            foreach (SetupMappingInfo setupInfo in SetupInfos)
            {
               PropertySetupType propertySetup = setupInfo.PropertySetup;

               setupInfo.ExportSetup = ReadSetupExportFlagFromTemplate(template, propertySetup);

               templatePsetsNames.TryGetValue(propertySetup, out HashSet<string> templateModifiedPSets);
               if ((templateModifiedPSets?.Count ?? 0) == 0)
                  continue;

               foreach (PSetMappingInfo psetInfo in setupInfo.PSetMappingInfos)
               {
                  string psetName =
                     (setupInfo.IfcVersion == IFCVersion.IFC2x3 && propertySetup == PropertySetupType.IfcBaseQuantities) ?
                     PSetMappingInfo.ConvertQuantitySetNameFrom2x3(psetInfo.Name) : psetInfo.Name;

                  if (string.IsNullOrEmpty(psetName))
                     continue;

                  if (!templateModifiedPSets.Contains(psetName))
                     continue;

                  psetInfo.ExportFlag = template.IsExportingPropertySet(propertySetup, psetName);

                  IList<IFCPropertyMappingInfo> templatePropertyInfos =
                     template.GetPropertyMappingInfos(propertySetup, psetName, PropertySelectionType.All);

                  if ((templatePropertyInfos?.Count ?? 0) == 0)
                     continue;

                  foreach (IFCPropertyMappingInfo templatePropertyInfo in templatePropertyInfos)
                  {
                     if (!psetInfo.TryGetProperty(templatePropertyInfo, out PropertyMappingInfo modelPropertyInfo))
                        continue;

                     modelPropertyInfo.Assign(templatePropertyInfo);
                  }
               }
            }
         }
         finally
         {
            _suppressPropertyMappingChanged = false;
         }

         InitializePropertyDataGrid();
      }


      private void treeView_PropertySetups_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
      {
         object newSelectedItem = e.NewValue;
         if (newSelectedItem is SetupMappingInfo newSelectedSetup)
         {
            _selectedPropertySetup = newSelectedSetup.PropertySetup;
            if (_selectedPropertySetup != PropertySetupType.RevitElementParameters)
               ResetCategoryFilterSelection();
            SetSelectedPropertySet(null);

            VisibilityState visibilityState = VisibilityState.NothingSelected;
            if (newSelectedSetup.PSetMappingInfos.Count == 0 &&
               (_selectedPropertySetup == PropertySetupType.RevitMaterialParameters ||
               _selectedPropertySetup == PropertySetupType.RevitSchedules))
            {
               visibilityState = _selectedPropertySetup == PropertySetupType.RevitMaterialParameters ?
                  VisibilityState.EmptyStateMaterials : VisibilityState.EmptyStateSchedules;
            }
            UpdateControlsVisibilityState(visibilityState);
         }
         else if (newSelectedItem is PSetMappingInfo newSelectedPset)
         {
            if (newSelectedPset.ParentSetup == null)
               return;

            _selectedPropertySetup = newSelectedPset.ParentSetup.PropertySetup;
            if (_selectedPropertySetup != PropertySetupType.RevitElementParameters)
               ResetCategoryFilterSelection();
            SetSelectedPropertySet(newSelectedPset);
            UpdateControlsVisibilityState(VisibilityState.Default);
         }
      }

      private void checkBox_PropertySet_Click(object sender, RoutedEventArgs e)
      {
         if (sender is CheckBox checkBox)
         {
            // Find the parent TreeViewItem and select it
            DependencyObject parent = VisualTreeHelper.GetParent(checkBox);
            while (parent != null && parent is not TreeViewItem)
            {
               parent = VisualTreeHelper.GetParent(parent);
            }

            if (parent is TreeViewItem treeViewItem)
            {
               // Clicking the checkbox now triggers treeView_PropertySetups_SelectedItemChanged, which in turn updates the data grid.
               treeViewItem.IsSelected = true;
            }
         }
      }

      /// <summary>
      /// Updates the controls.
      /// </summary>
      /// <param name="template">The current template being used.</param>
      private void UpdateTemplateControls(IFCParameterTemplate template)
      {
         // If configuration is built-in, disable all controls
         if (!IsCustomConfiguration)
         {
            UpdateControlsEnabledState();
            return;
         }

         bool isInSessionSelected = IsInSessionTemplate(template?.Name);

         button_Delete.IsEnabled = !isInSessionSelected;
         button_Rename.IsEnabled = !isInSessionSelected;
      }

      private void UpdateControlsEnabledState()
      {
         if (!IsCustomConfiguration)
         {
            button_Add.IsEnabled = false;
            button_Import.IsEnabled = false;
            button_Copy.IsEnabled = false;
            button_Save.IsEnabled = false;
            button_Export.IsEnabled = false;
            button_Rename.IsEnabled = false;
            button_Delete.IsEnabled = false;

            comboBox_IFCSchema.IsEnabled = false;
            button_PropertySetClean.IsEnabled = false;

            button_ResetAll.IsEnabled = false;
         }
      }

      private void InitializeSchemaList()
      {
         _ifcSchemas = new()
         {
         IFCVersion.IFC2x2,
         IFCVersion.IFC2x3,
         IFCVersion.IFC4,
         IFCVersion.IFC4x3
         };

         switch (_ifcExportConfiguration.IFCVersion)
         {
            case IFCVersion.IFC2x2:
               {
                  SelectedIfcSchema = IFCVersion.IFC2x2;
                  break;
               }
            case IFCVersion.IFC2x3:
            case IFCVersion.IFC2x3CV2:
            case IFCVersion.IFCCOBIE:
            case IFCVersion.IFC2x3BFM:
            case IFCVersion.IFC2x3FM:
               {
                  SelectedIfcSchema = IFCVersion.IFC2x3;
                  break;
               }
            case IFCVersion.IFC4RV:
            case IFCVersion.IFC4DTV:
            case IFCVersion.IFC4:
            case IFCVersion.IFCSG:
               {
                  SelectedIfcSchema = IFCVersion.IFC4;
                  break;
               }
            case IFCVersion.IFC4x3:
            case IFCVersion.IFC4x3RV:
            case IFCVersion.IFC4x3DTV:
               {
                  SelectedIfcSchema = IFCVersion.IFC4x3;
                  break;
               }
            default:
               break;
         }
      }

      /// <summary>
      /// Initialize listBox with Property Setups list
      /// </summary>
      private void InitializePropertySetupsList()
      {
         _propertySetups.Add(PropertySetupType.IfcCommonPropertySets);
         _propertySetups.Add(PropertySetupType.RevitElementParameters);
         _propertySetups.Add(PropertySetupType.IfcBaseQuantities);
         _propertySetups.Add(PropertySetupType.RevitMaterialParameters);
         _propertySetups.Add(PropertySetupType.RevitSchedules);
         _propertySetups.Add(PropertySetupType.UserDefinedPropertySets);
         _selectedPropertySetup = _propertySetups.FirstOrDefault();
      }

      #region Visibility states
      public enum VisibilityState
      {
         // Show all controls except empty states
         Default = 0,
         // Display empty state image for Schedules
         EmptyStateSchedules = 1,
         // Display empty state image for Materials
         EmptyStateMaterials = 2,
         // Display empty state image for not selected setup
         NothingSelected
      }

      private void UpdateControlsVisibilityState(VisibilityState state)
      {
         switch (state)
         {
            case VisibilityState.Default:
               {
                  dataGrid_PropertyMapping.Visibility = System.Windows.Visibility.Visible;
                  button_ResetAll.Visibility = System.Windows.Visibility.Visible;
                  image_EmptyStatePsets.Visibility = System.Windows.Visibility.Collapsed;
                  textBlock_EmptyStatePsetsInfo.Visibility = System.Windows.Visibility.Collapsed;
                  textBlock_EmptyStatePsets.Visibility = System.Windows.Visibility.Collapsed;
                  textBlock_EmptyStatePsets.Visibility = System.Windows.Visibility.Collapsed;
                  break;
               }
            case VisibilityState.EmptyStateSchedules:
               {
                  dataGrid_PropertyMapping.Visibility = System.Windows.Visibility.Collapsed;
                  image_EmptyStatePsets.Visibility = System.Windows.Visibility.Visible;
                  textBlock_EmptyStatePsetsInfo.Visibility = System.Windows.Visibility.Visible;
                  textBlock_EmptyStatePsets.Visibility = System.Windows.Visibility.Visible;
                  textBlock_EmptyStatePsetsInfo.Text = Properties.Resources.EmptyStateScheduleInfo;
                  textBlock_EmptyStatePsets.Text = Properties.Resources.EmptyStateSchedulePsets;
                  button_ResetAll.Visibility = System.Windows.Visibility.Hidden;
                  textBlock_PropertySetName.Text = string.Empty;
                  break;
               }
            case VisibilityState.EmptyStateMaterials:
               {
                  dataGrid_PropertyMapping.Visibility = System.Windows.Visibility.Collapsed;
                  image_EmptyStatePsets.Visibility = System.Windows.Visibility.Visible;
                  textBlock_EmptyStatePsetsInfo.Visibility = System.Windows.Visibility.Visible;
                  textBlock_EmptyStatePsets.Visibility = System.Windows.Visibility.Visible;
                  textBlock_EmptyStatePsetsInfo.Text = Properties.Resources.EmptyStateMaterialPsets;
                  textBlock_EmptyStatePsets.Text = Properties.Resources.EmptyStateMaterialInfo;
                  button_ResetAll.Visibility = System.Windows.Visibility.Hidden;
                  textBlock_PropertySetName.Text = string.Empty;
                  break;
               }
            case VisibilityState.NothingSelected:
               {
                  dataGrid_PropertyMapping.Visibility = System.Windows.Visibility.Collapsed;
                  image_EmptyStatePsets.Visibility = System.Windows.Visibility.Visible;
                  textBlock_EmptyStatePsetsInfo.Visibility = System.Windows.Visibility.Collapsed;
                  textBlock_EmptyStatePsets.Visibility = System.Windows.Visibility.Visible;
                  textBlock_EmptyStatePsets.Text = Properties.Resources.EmptyStatePropertySet;
                  button_ResetAll.Visibility = System.Windows.Visibility.Hidden;
                  textBlock_PropertySetName.Text = string.Empty;
                  break;
               }
            default:
               break;
         }
      }

      private VisibilityState GetVisibilityStateForCurrentSelection()
      {
         if (SelectedPropertySet != null)
            return VisibilityState.Default;

         if (treeView_PropertySetups.SelectedItem is SetupMappingInfo selectedSetup)
         {
            _selectedPropertySetup = selectedSetup.PropertySetup;

            if (selectedSetup.PSetMappingInfos.Count == 0 &&
               (_selectedPropertySetup == PropertySetupType.RevitMaterialParameters ||
               _selectedPropertySetup == PropertySetupType.RevitSchedules))
            {
               return _selectedPropertySetup == PropertySetupType.RevitMaterialParameters ?
                  VisibilityState.EmptyStateMaterials : VisibilityState.EmptyStateSchedules;
            }
         }

         return VisibilityState.NothingSelected;
      }
      #endregion

      private void InitializePropertyDataGrid()
      {
         textBlock_PropertySetName.Text = SelectedPropertySet?.Name ?? string.Empty;

         InitializeCategoryFilter();
         RefreshPropertyGridForCategoryFilter();
      }

      private void RefreshPropertyGridForCategoryFilter()
      {
         if (ObservableProperties.Count > 0)
            ObservableProperties.Clear();

         if (SelectedPropertySet == null)
         {
            ExportFlagAll = UpdateHeaderCheckboxFlag(ObservableProperties);
            return;
         }

         foreach (PropertyMappingInfo propertyInfo in GetPropertyInfosWithCategoryFilter())
            ObservableProperties.Add(propertyInfo);

         ExportFlagAll = UpdateHeaderCheckboxFlag(ObservableProperties);
      }

      private IEnumerable<PropertyMappingInfo> GetPropertyInfosWithCategoryFilter()
      {
         if (SelectedPropertySet == null)
            yield break;

         List<PropertyMappingInfo> propertyInfos = SelectedPropertySet.PropertyInfos ?? new List<PropertyMappingInfo>();

         if (SelectedPropertySet?.ParentSetup?.PropertySetup != PropertySetupType.RevitElementParameters)
         {
            foreach (PropertyMappingInfo propertyInfo in propertyInfos)
               yield return propertyInfo;
            yield break;
         }

         if (string.IsNullOrEmpty(_currentCategoryFilter) ||
            string.Equals(_currentCategoryFilter, Properties.Resources.DefaultCategoryFilterTxt, StringComparison.CurrentCultureIgnoreCase) ||
            !_categoryParameterLookup.TryGetValue(_currentCategoryFilter, out CategoryParameterSet parameterSet))
         {
            foreach (PropertyMappingInfo propertyInfo in propertyInfos)
               yield return propertyInfo;
            yield break;
         }

         foreach (PropertyMappingInfo propertyInfo in propertyInfos)
         {
            if (parameterSet.Contains(propertyInfo))
               yield return propertyInfo;
         }
      }

      private static string GetCategoryDisplayName(ElementId categoryId)
      {
         if (categoryId == null || categoryId == ElementId.InvalidElementId)
            return string.Empty;
         Document document = IFCCommandOverrideApplication.TheDocument;
         if (document == null)
            return string.Empty;
         Category category = Category.GetCategory(document, categoryId);
         if (category == null)
            return string.Empty;

         string categoryName = category.Name ?? string.Empty;
         string parentName = category.Parent?.Name;

         if (!string.IsNullOrEmpty(parentName) &&
            !string.Equals(parentName, categoryName, StringComparison.CurrentCultureIgnoreCase))
            return $"{parentName}: {categoryName}";

         return categoryName;
      }

      private void ResetCategoryFilterSelection()
      {
         _suppressCategoryFilterSelectionChanged = true;
         _currentCategoryFilter = Properties.Resources.DefaultCategoryFilterTxt;

         if (comboBox_CategoryName.ItemsSource != null && comboBox_CategoryName.Items.Count > 0)
            comboBox_CategoryName.SelectedIndex = 0;

         _suppressCategoryFilterSelectionChanged = false;
      }

      private void comboBox_CategoryName_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         if (_suppressCategoryFilterSelectionChanged)
            return;

         string selectedFilter = comboBox_CategoryName.SelectedItem as string ?? Properties.Resources.DefaultCategoryFilterTxt;
         if (string.Equals(_currentCategoryFilter, selectedFilter, StringComparison.CurrentCultureIgnoreCase))
            return;

         _currentCategoryFilter = selectedFilter;
         RefreshPropertyGridForCategoryFilter();
      }

      private sealed class CategoryParameterSet
      {
         private readonly HashSet<long> _parameterIds = new();
         private readonly HashSet<string> _parameterNames = new(StringComparer.CurrentCultureIgnoreCase);

         public CategoryParameterSet(IEnumerable<RevitParameterData> parameters)
         {
            AddParameters(parameters);
         }

         public void AddParameters(IEnumerable<RevitParameterData> parameters)
         {
            if (parameters == null)
               return;

            foreach (RevitParameterData parameter in parameters)
            {
               if (parameter == null)
                  continue;

               ElementId parameterId = parameter.Id;
               if (parameterId != null && parameterId != ElementId.InvalidElementId)
                  _parameterIds.Add(parameterId.Value);

               string parameterName = parameter.Name;
               if (!string.IsNullOrEmpty(parameterName))
                  _parameterNames.Add(parameterName);
            }
         }

         public bool Contains(PropertyMappingInfo propertyInfo)
         {
            if (propertyInfo == null)
               return false;

            ElementId propertyId = propertyInfo.RevitPropertyId;
            if (propertyId != ElementId.InvalidElementId && _parameterIds.Contains(propertyId.Value))
               return true;

            string propertyName = propertyInfo.RevitPropertyName;
            if (!string.IsNullOrEmpty(propertyName) && _parameterNames.Contains(propertyName))
               return true;

            return false;
         }
      }

      /// <summary>
      /// Returns true is the name is equal to in-session template name
      /// </summary>
      static bool IsInSessionTemplate(string templateName)
      {
         if (string.IsNullOrEmpty(templateName))
            return false;

         string inSessionName = GetInSessionTemplateName();
         return templateName.Equals(inSessionName);
      }

      /// <summary>
      /// Returns in-session template name
      /// </summary>
      static string GetInSessionTemplateName()
      {
         IFCParameterTemplate inSessionTemplate = IFCParameterTemplate.GetOrCreateInSessionTemplate(IFCCommandOverrideApplication.TheDocument);
         return inSessionTemplate?.Name ?? string.Empty;
      }

      /// <summary>
      /// Returns the list of templates in the document including the in-session one
      /// </summary>
      static IList<string> GetAllTemplateNames()
      {
         IList<string> templateNames = IFCParameterTemplate.ListNames(IFCCommandOverrideApplication.TheDocument) ?? new List<string>();
         templateNames.Add(GetInSessionTemplateName());

         return templateNames;
      }

      private static string CleanTemplateName(string templateName)
      {
         // Use regex to remove angle brackets and square brackets in one operation
         return string.IsNullOrEmpty(templateName) ? templateName :
                Regex.Replace(templateName, @"[<>\[\]]", "");
      }

      /// <summary>
      /// Get template active in list
      /// </summary>
      private IFCParameterTemplate GetCurrentTemplate()
      {
         return GetTemplateByName(listBox_MappingTemplates.SelectedItem as string);
      }

      private IFCParameterTemplate GetTemplateByName(string templateName)
      {
         if (string.IsNullOrEmpty(templateName))
            return null;

         if (!IsCustomConfiguration)
         {
            if (_builtInTemplateCache.TryGetValue(templateName, out IFCParameterTemplate cachedTemplate))
            {
               return cachedTemplate;
            }
            return null;
         }

         IFCParameterTemplate foundTemplate = IsInSessionTemplate(templateName) ?
            IFCParameterTemplate.GetOrCreateInSessionTemplate(IFCCommandOverrideApplication.TheDocument) :
            IFCParameterTemplate.FindByName(IFCCommandOverrideApplication.TheDocument, templateName);

         return foundTemplate;
      }

      private IFCParameterTemplate GetMappingTemplateFromConfiguration(Document doc, IFCExportConfiguration configuration)
      {
         if (configuration == null || doc == null)
            return null;

         IFCParameterTemplate currentTemplate = null;

         if (!IsCustomConfiguration)
         {
            string builtInTemplateName = CleanTemplateName(configuration.Name);
            if (!IFCParameterTemplate.IsValidName(doc, builtInTemplateName))
               builtInTemplateName = CleanTemplateName(Properties.Resources.IFCDefaultSetup);

            // Check cache first
            if (_builtInTemplateCache.TryGetValue(builtInTemplateName, out currentTemplate))
            {
               return currentTemplate;
            }

            // Create new template and cache it
            currentTemplate = new IFCParameterTemplate(doc);
            try
            {
               currentTemplate.Name = builtInTemplateName;
               _builtInTemplateCache[builtInTemplateName] = currentTemplate;
            }
            catch (Exception)
            {
            }
            return currentTemplate;
         }

         string configurationTemplateName = configuration.PropertyMapping;
         if (!string.IsNullOrEmpty(configurationTemplateName))
            currentTemplate = IFCParameterTemplate.FindByName(doc, configurationTemplateName);

         currentTemplate ??= IFCParameterTemplate.GetOrCreateInSessionTemplate(doc);

         return currentTemplate;
      }

      private void WriteMappingInfoToTemplate(string templateName)
      {
         if (string.IsNullOrEmpty(templateName))
            return;

         IFCParameterTemplate template = GetTemplateByName(templateName);
         WriteMappingInfoToTemplate(template);
      }

      private void WriteMappingInfoToTemplate(IFCParameterTemplate template)
      {
         if (template == null || !IsCustomConfiguration)
            return;

         // After this clean the template contain only the mappings that don't exist in
         // current SetupInfos, so now simply write to template each not default mapping.
         CleanMappingTemplate(template);

         HashSet<ElementId> usedRevitPropertyIds = new();

         foreach (SetupMappingInfo setupInfo in SetupInfos)
         {
            PropertySetupType propertySetup = setupInfo.PropertySetup;

            foreach (PSetMappingInfo psetInfo in setupInfo.PSetMappingInfos)
            {
               string psetName =
                  (setupInfo.IfcVersion == IFCVersion.IFC2x3 && propertySetup == PropertySetupType.IfcBaseQuantities) ?
                  PSetMappingInfo.ConvertQuantitySetNameFrom2x3(psetInfo.Name) : psetInfo.Name;

               if (string.IsNullOrEmpty(psetName))
                  continue;

               List<PropertyMappingInfo> propertyInfos = psetInfo.PropertyInfos;
               if (propertyInfos == null)
                  continue;

               bool isExportingPset = psetInfo.ExportFlag;

               List<IFCPropertyMappingInfo> modifiedPropertyMappings = [];
               foreach (var propertyInfo in propertyInfos)
               {
                  if (propertyInfo.IsDefault())
                     continue;

                  // Hopefully this is redundant, but not checking could case a crash in the Add function.
                  ElementId revitPropertyId = propertyInfo.RevitPropertyId;
                  if (usedRevitPropertyIds.Contains(revitPropertyId))
                     continue;

                  modifiedPropertyMappings.Add(new IFCPropertyMappingInfo
                  {
                     ExportFlag = propertyInfo.ExportFlag,
                     IFCPropertyName = propertyInfo.IFCPropertyName,
                     RevitPropertyId = revitPropertyId,
                     RevitPropertyName = propertyInfo.RevitPropertyName
                  });
                  usedRevitPropertyIds.Add(revitPropertyId);
               }

               if (!NeedToWritePSetToTemplate(isExportingPset, modifiedPropertyMappings, setupInfo))
               {
                  // The property set is in default state
                  continue;
               }

               if (template.IsPropertySetAMemberOfTemplate(propertySetup, psetName))
               {
                  template.SetPropertySetExportingFlag(propertySetup, psetName, isExportingPset);
               }
               else
               {
                  template.AddPropertySet(propertySetup, isExportingPset, psetName);
               }

               foreach (IFCPropertyMappingInfo modifiedPropertyMapping in modifiedPropertyMappings)
               {
                  if (!IFCPropertyMappingInfo.IsValidMappingInfo(modifiedPropertyMapping))
                     continue;

                  template.AddPropertyMappingInfo(propertySetup, psetName, modifiedPropertyMapping);
               }
            }

            bool exportSetup = !setupInfo.ExportSetup.HasValue || setupInfo.ExportSetup.Value;
            WriteSetupExportFlagToTemplate(template, propertySetup, exportSetup);
         }
      }

      private static bool NeedToWritePSetToTemplate(bool isExportingPset, List<IFCPropertyMappingInfo> modifiedPropertyMappings,
         SetupMappingInfo setupInfo)
      {
         // If there are modified Properties in the Property Set, we need to write it to the template
         if (modifiedPropertyMappings.Count > 0)
            return true;

         // If the Property Set is not exporting, we need to write it to the template,
         // but only when the Setup is enabled (when the setup is disabled all its Property Sets
         // are disabled too, no need to keep all of them in the Template in this case)
         bool setupIsDisabled = setupInfo.ExportSetup.HasValue && !setupInfo.ExportSetup.Value;
         if (setupIsDisabled)
            return false;

         return !isExportingPset;
      }

      private static void WriteSetupExportFlagToTemplate(IFCParameterTemplate template, PropertySetupType propertySetup, bool exportFlag)
      {
         switch (propertySetup)
         {
            case PropertySetupType.IfcCommonPropertySets:
               template.ExportIFCCommonPropertySets = exportFlag;
               break;
            case PropertySetupType.RevitElementParameters:
               template.ExportRevitElementParameters = exportFlag;
               break;
            case PropertySetupType.IfcBaseQuantities:
               template.ExportIFCBaseQuantities = exportFlag;
               break;
            case PropertySetupType.RevitMaterialParameters:
               template.ExportRevitMaterialParameters = exportFlag;
               break;
            case PropertySetupType.RevitSchedules:
               template.ExportRevitSchedules = exportFlag;
               break;
            case PropertySetupType.UserDefinedPropertySets:
               template.ExportUserDefinedPropertySets = exportFlag;
               break;
         }
      }

      private static bool ReadSetupExportFlagFromTemplate(IFCParameterTemplate template, PropertySetupType propertySetup)
      {
         switch (propertySetup)
         {
            case PropertySetupType.IfcCommonPropertySets:
               return template.ExportIFCCommonPropertySets;
            case PropertySetupType.RevitElementParameters:
               return template.ExportRevitElementParameters;
            case PropertySetupType.IfcBaseQuantities:
               return template.ExportIFCBaseQuantities;
            case PropertySetupType.RevitMaterialParameters:
               return template.ExportRevitMaterialParameters;
            case PropertySetupType.RevitSchedules:
               return template.ExportRevitSchedules;
            case PropertySetupType.UserDefinedPropertySets:
               return template.ExportUserDefinedPropertySets;
            default:
               return true;
         }
      }

      // A template can already store property mappings including 
      // the property mappings for other IFC schemas.
      // Here we want to preserve the existing in template property mappings
      // if they are not represented in the current SetupInfos
      private void CleanMappingTemplate(IFCParameterTemplate template)
      {
         if (template == null)
            return;

         foreach (PropertySetupType propertySetup in _propertySetups)
         {
            IList<string> templatePSetNames = template.GetPropertySetNames(propertySetup, PropertySelectionType.All);

            foreach (string templatePsetName in templatePSetNames)
            {
               PSetMappingInfo psetInfo = FindPSetInfo(propertySetup, templatePsetName);
               if (psetInfo == null)
                  continue;

               IList<IFCPropertyMappingInfo> templatePropertyInfos =
                  template.GetPropertyMappingInfos(propertySetup, templatePsetName, PropertySelectionType.All);

               List<IFCPropertyMappingInfo> templatePropertiesToKeep = [];
               foreach (IFCPropertyMappingInfo templatePropertyInfo in templatePropertyInfos)
               {
                  if (!psetInfo.TryGetProperty(templatePropertyInfo, out _))
                     templatePropertiesToKeep.Add(templatePropertyInfo);
               }

               template.RemovePropertySet(propertySetup, templatePsetName);

               if (templatePropertiesToKeep.Count > 0)
               {
                  template.AddPropertySet(propertySetup, psetInfo.ExportFlag, templatePsetName);

                  foreach (IFCPropertyMappingInfo templatePropertyToKeep in templatePropertiesToKeep)
                  {
                     if (!IFCPropertyMappingInfo.IsValidMappingInfo(templatePropertyToKeep))
                        continue;

                     template.AddPropertyMappingInfo(propertySetup, templatePsetName, templatePropertyToKeep);
                  }
               }
            }
         }
      }

      public PSetMappingInfo FindPSetInfo(PropertySetupType propertySetup, string psetName)
      {
         foreach (SetupMappingInfo setupInfo in SetupInfos)
         {
            if (setupInfo.PropertySetup != propertySetup)
               continue;

            string displayedPsetName =
               (setupInfo.IfcVersion == IFCVersion.IFC2x3 && propertySetup == PropertySetupType.IfcBaseQuantities) ?
               PSetMappingInfo.ConvertQuantitySetNameTo2x3(psetName) : psetName;

            foreach (PSetMappingInfo psetInfo in setupInfo.PSetMappingInfos)
            {
               if (psetInfo.Name == displayedPsetName)
                  return psetInfo;
            }
         }
         return null;
      }

      private void button_Ok_Click(object sender, RoutedEventArgs e)
      {
         SaveDialogChanges();
         Close();
      }

      private void button_Cancel_Click(object sender, RoutedEventArgs e)
      {
         DiscardTransactionsGroup();
         Close();
      }

      protected override bool OnContextHelp()
      {
         ContextualHelp help = new ContextualHelp(ContextualHelpType.ContextId, "HDialog_IFC_PropertyMapping");
         help.Launch();

         return true;
      }

      private void ChildWindow_PreviewKeyDown(object sender, KeyEventArgs e)
      {
         if (e.Key == Key.Escape)
         {
            e.Handled = true;
            button_Cancel_Click(button_Cancel, new RoutedEventArgs());
         }
      }

      private void ChildWindow_Closing(object sender, CancelEventArgs e)
      {
         UnsubscribeFromPropertyMappingChanges();
         DiscardTransactionsGroup();
      }

      private void SaveDialogChanges()
      {
         IFCParameterTemplate currentTemplate = GetCurrentTemplate();
         if (currentTemplate == null)
            return;

         WriteMappingInfoToTemplate(currentTemplate);
         if (IsCustomConfiguration)
         {
            _ifcExportConfiguration.PropertyMapping = (string)listBox_MappingTemplates.SelectedItem;
         }
         CommitTransactionGroup();
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

      private void comboBox_IFCSchema_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         IFCParameterTemplate currentTemplate = GetCurrentTemplate();
         if (currentTemplate == null)
            return;

         if (e.RemovedItems.Count > 0)
            WriteMappingInfoToTemplate(currentTemplate);

         InitializeSetupInfo(currentTemplate);

         UpdateControlsVisibilityState(GetVisibilityStateForCurrentSelection());
      }

      private void textBox_Search_TextChanged(object sender, TextChangedEventArgs e)
      {
         string partialWord = textBox_Search.Text ?? string.Empty;

         if (FilterTextPropertySet != partialWord)
            FilterTextPropertySet = partialWord;
      }

      private void button_PropertySetClean_Click(object sender, RoutedEventArgs e)
      {
         ResetSearch();
      }

      /// <summary>
      /// This modifies ExportFlag for all PropertyMappingInfos 
      /// </summary>
      private void ExportFlagAllClick()
      {
         bool? newState = ExportFlagAll;
         if (!newState.HasValue)
            return;

         foreach (PropertyMappingInfo currMappingInfo in ObservableProperties)
         {
            if (currMappingInfo == null)
               continue;

            currMappingInfo.ExportFlag = newState.Value;
         }
      }

      private void ExportFlagClick(object sender, RoutedEventArgs e)
      {
         ExportFlagAll = UpdateHeaderCheckboxFlag(ObservableProperties);
      }

      private void button_Reset_Click(object sender, RoutedEventArgs e)
      {
         PropertyMappingInfo propertyMappingInfo = dataGrid_PropertyMapping.SelectedItem as PropertyMappingInfo;
         if (propertyMappingInfo == null || propertyMappingInfo.IsDefault())
            return;

         // Resets one property mapping raw
         propertyMappingInfo.ResetToDefault();
         ExportFlagAll = UpdateHeaderCheckboxFlag(ObservableProperties);

         // Refresh bindings (updates tooltips)
         dataGrid_PropertyMapping.Items.Refresh();
      }

      private void button_ResetAll_Click(object sender, RoutedEventArgs e)
      {
         if (SelectedPropertySet == null)
            return;

         SelectedPropertySet.ResetToDefault();

         if (_selectedPropertySetup == PropertySetupType.RevitElementParameters)
            ResetCategoryFilterSelection();

         InitializePropertyDataGrid();
         ResetSearch();
      }

      /// <summary>
      /// Clears the text in the search field 
      /// </summary>
      private void ResetSearch()
      {
         textBox_Search.Text = string.Empty;
      }

      /// <summary>
      /// Updates indeterminate state if needed.
      /// </summary>
      private bool? UpdateHeaderCheckboxFlag(ObservableCollection<PropertyMappingInfo> propertyMappings)
      {
         if ((propertyMappings?.Count ?? 0) == 0)
            return true;

         bool foundChecked = propertyMappings.Any(node => node.ExportFlag);
         bool foundUnchecked = propertyMappings.Any(node => !node.ExportFlag);
         if (foundChecked && foundUnchecked)
            return null;

         return foundChecked;
      }

      private void button_RevitPropertyEdit_Click(object sender, RoutedEventArgs e)
      {
         PropertyMappingInfo mappingInfo = dataGrid_PropertyMapping.SelectedItem as PropertyMappingInfo;
         if (mappingInfo == null || mappingInfo.Type != IFCPropertyMappingModel.MappingType.IfcToRevit)
            return;

         if (SelectedPropertySet == null)
            return;

         IList<string> applicableEntities = GetApplicableEntitiesIfExist(SelectedPropertySet);
         bool isTableProperty = IsTableUserDefinedProperty(SelectedPropertySet.Name, mappingInfo.IFCPropertyName);

         IFCRevitPropertySelector propertySelector = new(
            new RevitParameterInfo(mappingInfo.RevitPropertyName, mappingInfo.RevitPropertyId),
            SelectedPropertySet.Name, mappingInfo.IFCPropertyName, mappingInfo.PropertyDataType, IfcSchemaEntityTree.GetSchemaVersion(SelectedIfcSchema),
            _selectedPropertySetup, applicableEntities, isTableProperty)
         {
            Owner = this,
         };

         bool? ret = propertySelector.ShowDialog();
         if (ret.HasValue && ret.Value == true)
         {
            if (propertySelector.SelectedRevitParameter == null)
               return;

            (mappingInfo.RevitPropertyId, mappingInfo.RevitPropertyName) =
               (propertySelector.SelectedRevitParameter.Id, propertySelector.SelectedRevitParameter.Name);

            // Refresh bindings (updates tooltips)
            dataGrid_PropertyMapping.Items.Refresh();
         }
      }

      private static bool IsTableUserDefinedProperty(string propertySetName, string propertyName)
      {
         if (string.IsNullOrEmpty(propertySetName) || string.IsNullOrEmpty(propertyName))
            return false;

         IFCUserDefinedPropertySet propertySet =
            IFCUserDefinedPropertySet.FindPropertySetByName(IFCCommandOverrideApplication.TheDocument, propertySetName);

         IFCUserDefinedProperty foundProperty = propertySet?.FindPropertyByName(propertyName);
         if (foundProperty == null)
            return false;

         return foundProperty.PropertyType == IFCUserDefinedPropertyType.Table;
      }

      private IList<string> GetApplicableEntitiesIfExist(PSetMappingInfo propertySetInfo)
      {
         if (string.IsNullOrEmpty(propertySetInfo?.Name))
            return null;

         PropertySetupType? setupType = propertySetInfo?.ParentSetup?.PropertySetup;
         if (!setupType.HasValue || setupType != PropertySetupType.UserDefinedPropertySets)
            return null;

         IFCUserDefinedPropertySet userDefinedSet =
            IFCUserDefinedPropertySet.FindPropertySetByName(IFCCommandOverrideApplication.TheDocument, propertySetInfo?.Name);
         if (userDefinedSet == null)
            return null;

         return userDefinedSet.GetApplicableEntities();
      }

      public bool FilterPropertySet(object obj)
      {
         if (!string.IsNullOrEmpty(FilterTextPropertySet))
         {
            PSetMappingInfo psetItem = obj as PSetMappingInfo;
            if (psetItem != null)
            {
               // Process Property Set Filter text
               if (!string.IsNullOrEmpty(FilterTextPropertySet))
               {
                  bool passPSetFilter = psetItem.Name.Contains(FilterTextPropertySet, StringComparison.OrdinalIgnoreCase);
                  if (!passPSetFilter)
                     return false;
               }
            }
         }
         return true;
      }

      private void button_AddPropertySet_Click(object sender, RoutedEventArgs e)
      {
         IFCParameterTemplate currTemplate = GetCurrentTemplate();
         if (currTemplate == null)
            return;

         WriteMappingInfoToTemplate(currTemplate);

         if (templateTransaction.HasStarted())
            templateTransaction.Commit();

         IFCUserDefinedPropertyMapping userDefinedPropertyMapping = new()
         {
            Owner = this
         };

         userDefinedPropertyMapping.ShowDialog();
         if (userDefinedPropertyMapping.IsModified)
         {
            InitializeSetupInfo(currTemplate);
         }

         if (templateTransaction.HasEnded())
            templateTransaction.Start();
      }

      private static string GetCaseSensitiveFileNameOnDisk(string filePath)
      {
         if (string.IsNullOrEmpty(filePath))
            return filePath;

         string directory = Path.GetDirectoryName(filePath);
         string fileName = Path.GetFileName(filePath);

         if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
            return filePath;

         try
         {
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
            {
               string entryName = Path.GetFileName(entry);
               if (string.Equals(entryName, fileName, StringComparison.OrdinalIgnoreCase))
                  return entry;
            }
         }
         catch
         {
         }
         return filePath;
      }

      public static string GetUniqueNameFromFile(string fullFileName, bool isCategoryMapping)
      {
         if (fullFileName == null)
            return null;

         string caseSensitiveFileName = GetCaseSensitiveFileNameOnDisk(fullFileName);
         string fileNameOnly = Path.GetFileNameWithoutExtension(caseSensitiveFileName);

         IFCTemplateData data = new(fileNameOnly, GetAllTemplateNames(),
            isCategoryMapping, IFCTemplateData.DialogTypeEnum.Template);

         return data.MakeUniqueName();
      }
   }

   #region Converters
   public class EditButtonVisibilityConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         if (!bool.TryParse((parameter as string), out bool isRevitParameterColumn))
            return null;

         IFCPropertyMappingModel.MappingType mappingType;

         PSetMappingInfo mappingInfo = value as PSetMappingInfo;
         if (mappingInfo != null)
            mappingType = mappingInfo.Type;
         else
         {
            SetupMappingInfo setupInfo = value as SetupMappingInfo;
            if (setupInfo == null)
            {
               // special case when closing User-defined Property Set dialog
               setupInfo = IFCPropertyMapping.SelectedPropertySet?.ParentSetup;
               if (setupInfo == null)
                  return null;
            }

            mappingType = IFCPropertyMappingModel.GetMappingType(setupInfo.PropertySetup);
         }

         return ((mappingType == IFCPropertyMappingModel.MappingType.IfcToRevit) ^ isRevitParameterColumn) ?
               System.Windows.Visibility.Hidden : System.Windows.Visibility.Visible;
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }

   public class IsIFCColumnReadOnlyConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         IFCPropertyMappingModel.MappingType mappingType;

         PSetMappingInfo mappingInfo = value as PSetMappingInfo;
         if (mappingInfo != null)
            mappingType = mappingInfo.Type;
         else
         {
            SetupMappingInfo setupInfo = value as SetupMappingInfo;
            if (setupInfo == null)
               return null;

            mappingType = IFCPropertyMappingModel.GetMappingType(setupInfo.PropertySetup);
         }

         return mappingType == IFCPropertyMappingModel.MappingType.IfcToRevit;
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }

   /// <summary>
   /// Replaces empty mapping with <default> string.
   /// </summary>
   public class EmptyToDefaulConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         string mappedName = (string)value;
         if (string.IsNullOrEmpty(mappedName))
         {
            return Resources.DefaultMapping;
         }

         return mappedName;
      }

      object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         return value;
      }
   }

   public class SetupNameToButtonVisibilityConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         PropertySetupType setupType = (PropertySetupType)value;
         if (setupType == PropertySetupType.UserDefinedPropertySets)
         {
            return System.Windows.Visibility.Visible;
         }

         return System.Windows.Visibility.Collapsed;
      }

      object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         return string.Empty;
      }
   }

   public class SelectedSetupToFilterVisibilityConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         PropertySetupType? setupType = null;

         if (value is SetupMappingInfo setupInfo)
            setupType = setupInfo.PropertySetup;
         else if (value is PSetMappingInfo pSetInfo)
            setupType = pSetInfo.ParentSetup?.PropertySetup;

         if (!setupType.HasValue)
            return System.Windows.Visibility.Collapsed;

         return setupType.Value == PropertySetupType.RevitElementParameters ?
            System.Windows.Visibility.Visible :
            System.Windows.Visibility.Collapsed;
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }

   /// <summary>
   /// Replaces focused color of TreeViewItem based on Revit Theme.
   /// </summary>
   public class BoolToRowBackgroundColorConverter : IMultiValueConverter
   {
      public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
      {
         SolidColorBrush defaultColorBrush = Brushes.Transparent;

         if (values == null || values[0] == null || values[1] == null)
            return defaultColorBrush;

         bool isRevitThemeDark = (bool)values[0];
         FrameworkElement callingElement = (FrameworkElement)values[1];

         if (isRevitThemeDark)
         {
            var resourceColor = callingElement.TryFindResource("RowBackgroundFocusedColorDark");
            if (resourceColor != null)
               return resourceColor;
         }
         else
         {
            var resourceColor = callingElement.TryFindResource("RowBackgroundFocusedColor");
            if (resourceColor != null)
               return resourceColor;
         }
         return defaultColorBrush;
      }

      public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }

   /// <summary>
   /// Replaces glyph color of ToggleButton based on Revit Theme.
   /// </summary>
   public class BoolToGlyphColorConverter : IMultiValueConverter
   {
      public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
      {
         SolidColorBrush defaultColorBrush = Brushes.Transparent;

         if (values == null || values[0] == null || values[1] == null)
            return null;

         bool isRevitThemeDark = (bool)values[0];
         FrameworkElement callingElement = (FrameworkElement)values[1];


         if (isRevitThemeDark)
         {
            var resourceColor = callingElement.TryFindResource("GlyphColorDark");
            if (resourceColor != null)
               return resourceColor;
         }
         else
         {
            var resourceColor = callingElement.TryFindResource("GlyphColor");
            if (resourceColor != null)
               return resourceColor;
         }
         return defaultColorBrush;
      }

      public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }

   public class TreeViewIndexConverter : IValueConverter
   {
      public object Convert(object value, Type TargetType, object parameter, CultureInfo culture)
      {
         TreeViewItem item = (TreeViewItem)value;
         TreeView treeView = ItemsControl.ItemsControlFromItemContainer(item) as TreeView;
         int index = treeView?.ItemContainerGenerator?.IndexFromContainer(item) ?? 0;
         return index.ToString();
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }

   /// <summary>
   /// Converts ElementId to parameter tooltip text.
   /// </summary>
   public class ElementIdToTooltipConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         string parameterTooltip = null;
         if (value is PropertyMappingInfo propertyInfo)
         {
            parameterTooltip = IFCPropertyMappingModel.GetParameterTooltip(propertyInfo.RevitPropertyId, propertyInfo.RevitPropertyName);
         }

         return string.IsNullOrEmpty(parameterTooltip) ? Resources.DefaultMapping : parameterTooltip;
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }

   #endregion
}
