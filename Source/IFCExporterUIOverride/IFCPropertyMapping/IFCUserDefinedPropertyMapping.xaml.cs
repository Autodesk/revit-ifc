using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.UI.Windows;
using BIM.IFC.Export.UI.Properties;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Exporter.PropertySet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using static BIM.IFC.Export.UI.IFCPropertySetType;
using ComboBox = System.Windows.Controls.ComboBox;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Validates user-defined property sets to ensure correct creation and modification.
   /// </summary>
   public class UserDefinedPropertySetValidator
   {
      /// <summary>
      /// Modification allowed to properties within property set.
      /// </summary>
      public enum PropertyModificationOperation
      {
         AllowExport,
         RevitPropertyName,
         IFCPropertyName,
         PropertyApplicationType
      }

      /// <summary>
      /// String to identify reserved property set names.
      /// </summary>
      private static string PropertySetReservedString => "PSet_";

      /// <summary>
      /// Indicates if the property set is reserved or not.
      /// </summary>
      /// <remarks>
      /// Reserved property sets begin with "PSet_".
      /// </remarks>
      /// <param name="propertySet">Name of property set.</param>
      /// <returns>True if property set is reserved, false otherwise.</returns>
      public static bool IsReserved(string propertySet)
      {
         if (string.IsNullOrWhiteSpace(propertySet) || propertySet.Length < PropertySetReservedString.Length)
            return false;

         return (propertySet.StartsWith(PropertySetReservedString, StringComparison.OrdinalIgnoreCase));
      }

      /// <summary>
      /// Extends the property set name if it is a reserved property set (starts with "PSet_").
      /// </summary>
      /// <param name="propertySet">Name of the property set.</param>
      /// <returns>Extended property set name if the property set name is reserved, the original property set name otherwise.</returns>
      public (string, bool) ExtendPropertySetNameIfNeeded(string propertySet)
      {
         if (!IsReserved(propertySet))
            return (propertySet, false);

         string extendedPropertyset = $"e{propertySet}";
         DelayedValidationWarnings.Add(string.Format(Resources.IFCExportWarningCannotAddUserDefinedPropertySet, propertySet, PropertySetReservedString, extendedPropertyset));
         return (extendedPropertyset, true);
      }

      /// <summary>
      /// Indicates whether the property can be added to the property set.
      /// </summary>
      /// <param name="propertySet">Property set name.</param>
      /// <param name="property">New property name.</param>
      /// <returns>True if property can be added to property set, false otherwise.</returns>
      public bool CanAddProperty(string propertySet, string property)
      {
         if (!IsReserved(propertySet))
            return true;

         DelayedValidationWarnings.Add(string.Format(Resources.IFCExportWarningCannotAddPropertyToReservedPropertySet, property, propertySet));
         return false;
      }

      /// <summary>
      /// Indicates whether the property within the property set can be modified.
      /// </summary>
      /// <param name="propertySet">Property set name.</param>
      /// <param name="property">Property name.</param>
      /// <param name="operation">Operation to be performed on property set.</param>
      /// <returns>True if operation is allowed, false otherwise.</returns>
      public bool CanModifyProperty(string propertySet, string property, PropertyModificationOperation operation)
      {
         if (!IsReserved(propertySet) || (operation == PropertyModificationOperation.AllowExport))
            return true;

         DelayedValidationWarnings.Add(string.Format(Resources.IFCExportWarningCannotModifyPropertySetProperty, property, propertySet));
         return false;
      }

      /// <summary>
      /// Warnings that may be posted when Dialog is closed.
      /// </summary>
      public List<string> DelayedValidationWarnings { get; set; } = new List<string>();
   }

   /// <summary>
   /// User-defined property information.
   /// </summary>
   public class UserDefinedPropertyInfo : INotifyPropertyChanged
   {
      public static readonly string DefaultDataTypeProperty = "Text";
      public static readonly string DefaultDataTypeQuantity = "Length";

      private string _ifcPropertyName = string.Empty;
      /// <summary>
      /// The IFC property name.
      /// </summary>
      public string IFCPropertyName
      {
         get { return _ifcPropertyName; }
         set
         {
            if (string.Equals(_ifcPropertyName, value, StringComparison.Ordinal))
               return;

            _ifcPropertyName = value;
            OnPropertyChanged();
         }
      }

      private string _propertyDataType = DefaultDataTypeProperty;
      /// <summary>
      /// The property data type.
      /// </summary>
      public string PropertyDataType
      {
         get { return _propertyDataType; }
         set
         {
            if (string.Equals(_propertyDataType, value, StringComparison.Ordinal))
               return;

            _propertyDataType = value;
            OnPropertyChanged();
            ResetRevitParameterInfoIfAssigned();
         }
      }

      private IFCUserDefinedPropertyType _propertyType = IFCUserDefinedPropertyType.Single;
      /// <summary>
      /// The type of the property.
      /// </summary>
      public IFCUserDefinedPropertyType PropertyType
      {
         get { return _propertyType; }
         set
         {
            if (_propertyType == value)
               return;

            _propertyType = value;
            OnPropertyChanged();
            ResetRevitParameterInfoIfAssigned();
         }
      }

      private string _propertyDataTypeDefined = DefaultDataTypeProperty;
      /// <summary>
      /// The defined value data type of the table property.
      /// </summary>
      public string PropertyDataTypeDefined
      {
         get { return _propertyDataTypeDefined; }
         set
         {
            if (string.Equals(_propertyDataTypeDefined, value, StringComparison.Ordinal))
               return;

            _propertyDataTypeDefined = value;
            OnPropertyChanged();
            if (_propertyType == IFCUserDefinedPropertyType.Table)
               ResetRevitParameterInfoIfAssigned();
         }
      }

      // <summary>
      // The Revit parameter.
      // </summary>
      private RevitParameterInfo m_revitParameterInfo = new();
      public RevitParameterInfo RevitParameterInfo
      {
         get { return m_revitParameterInfo; }
         set
         {
            if (m_revitParameterInfo != null && value != null &&
               m_revitParameterInfo.Id == value.Id &&
               string.Equals(m_revitParameterInfo.Name, value.Name, StringComparison.Ordinal))
               return;

            m_revitParameterInfo = value;
            OnPropertyChanged();
         }
      }

      private void ResetRevitParameterInfoIfAssigned()
      {
         if (m_revitParameterInfo == null)
            return;

         bool parameterIsAssigned = m_revitParameterInfo.Id != ElementId.InvalidElementId ||
            !string.IsNullOrWhiteSpace(m_revitParameterInfo.Name);
         if (!parameterIsAssigned)
            return;

         m_revitParameterInfo = new RevitParameterInfo();
         OnPropertyChanged(nameof(RevitParameterInfo));
      }

      public event PropertyChangedEventHandler PropertyChanged;
      protected void OnPropertyChanged([CallerMemberName] string name = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }
   }


   /// <summary>
   /// Revit parameter information.
   /// </summary>
   public class RevitParameterInfo
   {
      public string Name = string.Empty;

      public ElementId Id = ElementId.InvalidElementId;

      public RevitParameterInfo()
      {
         Name = string.Empty;
         Id = ElementId.InvalidElementId;
      }

      public RevitParameterInfo(string name, ElementId id)
      {
         Name = name;
         Id = id;
      }
   }


   /// <summary>
   /// Interaction logic for IFCUserDefinedPropertyMapping.xaml
   /// </summary>
   public partial class IFCUserDefinedPropertyMapping : ChildWindow, INotifyPropertyChanged
   {
      public ObservableCollection<string> ObservablePropertySets { get; set; } = new();

      private ObservableCollection<UserDefinedPropertyInfo> _observableProperties = new();
      public ObservableCollection<UserDefinedPropertyInfo> ObservableProperties
      {
         get { return _observableProperties; }
         set
         {
            // Unsubscribe from old collection items
            if (_observableProperties != null)
            {
               foreach (var item in _observableProperties)
               {
                  item.PropertyChanged -= OnPropertyInfoChanged;
               }
               _observableProperties.CollectionChanged -= OnObservablePropertiesCollectionChanged;
            }

            _observableProperties = value;

            // Subscribe to new collection items
            if (_observableProperties != null)
            {
               foreach (var item in _observableProperties)
               {
                  item.PropertyChanged += OnPropertyInfoChanged;
               }
               _observableProperties.CollectionChanged += OnObservablePropertiesCollectionChanged;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(HasTableProperties));
         }
      }

      /// <summary>
      /// Property to trigger TablePropertyTypeVisibilityConverter when any PropertyType changes
      /// </summary>
      public bool HasTableProperties
      {
         get
         {
            return ObservableProperties?.Any(prop => prop.PropertyType == IFCUserDefinedPropertyType.Table) ?? false;
         }
      }

      /// <summary>
      /// Handle property changes on individual UserDefinedPropertyInfo items
      /// </summary>
      private void OnPropertyInfoChanged(object sender, PropertyChangedEventArgs e)
      {
         if (e.PropertyName == nameof(UserDefinedPropertyInfo.PropertyType))
         {
            OnPropertyChanged(nameof(HasTableProperties));
         }
      }

      /// <summary>
      /// Handle items added/removed from ObservableProperties collection
      /// </summary>
      private void OnObservablePropertiesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
      {
         // Subscribe to PropertyChanged for new items
         if (e.NewItems != null)
         {
            foreach (UserDefinedPropertyInfo item in e.NewItems)
            {
               item.PropertyChanged += OnPropertyInfoChanged;
            }
         }

         // Unsubscribe from PropertyChanged for removed items
         if (e.OldItems != null)
         {
            foreach (UserDefinedPropertyInfo item in e.OldItems)
            {
               item.PropertyChanged -= OnPropertyInfoChanged;
            }
         }

         OnPropertyChanged(nameof(HasTableProperties));
      }

      public ObservableCollection<string> ObservableApplicableEntities { get; set; } = new();

      private static Dictionary<bool, ObservableCollection<string>> _dataTypesCache = null;

      private ObservableCollection<string> _allDataTypes = new();
      public ObservableCollection<string> AllDataTypes
      {
         get { return _allDataTypes; }
         set
         {
            _allDataTypes = value;
            OnPropertyChanged();
         }
      }

      public ObservableCollection<IFCUserDefinedPropertyType> AvailablePropertyTypes { get; } = new ObservableCollection<IFCUserDefinedPropertyType>()
      {
         IFCUserDefinedPropertyType.Single,
         IFCUserDefinedPropertyType.Bounded,
         IFCUserDefinedPropertyType.List,
         IFCUserDefinedPropertyType.Table
      };

      UserDefinedPropertySetValidator validator = new();

      private string _selectedPropertySet;
      public string SelectedPropertySet
      {
         get { return _selectedPropertySet; }
         set
         {
            _selectedPropertySet = value;
            OnPropertyChanged();
         }
      }

      private IFCVersion EntityTreeVersion { get; set; } = IFCVersion.Default;

      private UserDefinedPropertyInfo _selectedProperty;
      public UserDefinedPropertyInfo SelectedProperty
      {
         get { return _selectedProperty; }
         set
         {
            _selectedProperty = value;
            OnPropertyChanged();
         }
      }

      public event PropertyChangedEventHandler PropertyChanged;
      protected void OnPropertyChanged([CallerMemberName] string name = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }

      public bool IsModified { get; private set; } = false;

      private Transaction _transaction;

      public IFCUserDefinedPropertyMapping()
      {
         InitializeComponent();
         DataContext = this;

         // Initialize collection change subscription
         _observableProperties.CollectionChanged += OnObservablePropertiesCollectionChanged;

         Document doc = IFCCommandOverrideApplication.TheDocument;
         _transaction = new Transaction(doc, Properties.Resources.ModifyIFCPropertyMapping);
         StartTransaction();

         InitializePropertySetList();
      }

      /// <summary>
      /// Initializes the mapping templates listbox.
      /// </summary>
      private void InitializePropertySetList()
      {
         ObservablePropertySets.Clear();

         Document document = IFCCommandOverrideApplication.TheDocument;
         IList<string> propertySetNames = IFCUserDefinedPropertySet.ListPropertySetNames(document);
         if ((propertySetNames?.Count ?? 0) == 0)
            return;

         foreach (var psetName in propertySetNames)
         {
            ObservablePropertySets.Add(psetName);
         }

         SelectedPropertySet = ObservablePropertySets.Count > 0 ? ObservablePropertySets[0] : string.Empty;
      }

      private void button_Add_Click(object sender, RoutedEventArgs e)
      {
         IFCTemplateData data = new(Properties.Resources.IFCNewPropertySet,
            IFCUserDefinedPropertySet.ListPropertySetNames(IFCCommandOverrideApplication.TheDocument),
            isCategoryMapping: false, IFCTemplateData.DialogTypeEnum.PropertySet);

         IFCNewTemplate newDialog = new(data);
         newDialog.Owner = this;
         bool? ret = newDialog.ShowDialog();
         if (ret.HasValue && ret.Value == true)
         {
            string newPSetName = newDialog.Data.NewName;
            if (!string.IsNullOrEmpty(newPSetName) && !ObservablePropertySets.Contains(newPSetName))
            {
               IFCUserDefinedPropertySet newParameterTemplate = IFCUserDefinedPropertySet.Create(IFCCommandOverrideApplication.TheDocument, newPSetName);
               if (newParameterTemplate == null)
                  return;

               PropertySetType selectedPropertySet = newDialog.SelectedIfcPropertySetType.CurrentPropertySetType;
               switch (selectedPropertySet)
               {
                  case PropertySetType.QuantitySet:
                     {
                        newParameterTemplate.PropertySetType = IFCUserDefinedPropertySetType.QuantitySet;
                        break;
                     }
                  case PropertySetType.IFCAttributes:
                     {
                        newParameterTemplate.PropertySetType = IFCUserDefinedPropertySetType.IFCAttributeSet;
                        break;
                     }
                  default:
                     {
                        newParameterTemplate.PropertySetType = IFCUserDefinedPropertySetType.PropertySet;
                        break;
                     }
               }

               ObservablePropertySets.Add(newPSetName);
               SelectedPropertySet = newPSetName;

               // Init attributes only after SelectedPropertySet is set, due to saving of current template before switching to new one
               // it is done in listBox_PropertySets_SelectionChanged()
               if (newParameterTemplate.PropertySetType == IFCUserDefinedPropertySetType.IFCAttributeSet)
                  InitializeIFCAttributes(newParameterTemplate);

               InitData();
            }
         }
      }

      public void InitializeIFCAttributes(IFCUserDefinedPropertySet newParameterTemplate)
      {
         if (newParameterTemplate == null)
            return;

         newParameterTemplate.AddProperty(new IFCUserDefinedProperty("Name", ElementId.InvalidElementId, string.Empty, "Label", IFCUserDefinedPropertyType.Single, string.Empty));
         newParameterTemplate.AddProperty(new IFCUserDefinedProperty("LongName", ElementId.InvalidElementId, string.Empty, "Label", IFCUserDefinedPropertyType.Single, string.Empty));
         newParameterTemplate.AddProperty(new IFCUserDefinedProperty("Description", ElementId.InvalidElementId, string.Empty, "Text", IFCUserDefinedPropertyType.Single, string.Empty));
         newParameterTemplate.AddProperty(new IFCUserDefinedProperty("ObjectType", ElementId.InvalidElementId, string.Empty, "Label", IFCUserDefinedPropertyType.Single, string.Empty));

         InitializeObservableProperties(newParameterTemplate.GetProperties());
      }

      private void button_Import_Click(object sender, RoutedEventArgs e)
      {
         FileOpenDialog openDialog = new FileOpenDialog(Properties.Resources.IFCUserDefinedPropertySetsFilter);
         openDialog.Title = Properties.Resources.ImportIFCUserDefinedMappingDialogName;

         if (openDialog.Show() == ItemSelectionDialogResult.Confirmed)
         {
            // TODO: Support cloud paths.
            ModelPath modelPath = openDialog.GetSelectedModelPath();
            string fileName = ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath);

            try
            {
               IList<IFCUserDefinedPropertySet> importedList = IFCUserDefinedPropertySet.ImportFromFile(IFCCommandOverrideApplication.TheDocument, fileName, out bool isValidFile);
               if (importedList == null)
                  return;

               if (!isValidFile)
               {
                  using (Autodesk.Revit.UI.TaskDialog taskDialog = new Autodesk.Revit.UI.TaskDialog(Properties.Resources.IFCExport))
                  {
                     taskDialog.MainInstruction = Properties.Resources.IFCInvalidUserDefinedMappingFile;
                     taskDialog.MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconWarning;
                     taskDialog.TitleAutoPrefix = false;
                     TaskDialogResult taskDialogResult = taskDialog.Show();
                     return;
                  }
               }

               importedList.ToList().ForEach(x =>
               { 
                  if(IFCTemplateData.ContainsInvalidTemplateCharacters(x.Name))
                     x.Name = IFCTemplateData.RemoveInvalidCharacters(x.Name);
                  }
               );

               bool containsReservedPSets = false;
               foreach (var item in importedList)
               {
                  (string psetName, bool changed) = validator.ExtendPropertySetNameIfNeeded(item.Name);
                  if (ObservablePropertySets.Contains(psetName))
                     continue;

                  containsReservedPSets |= changed;
                  ObservablePropertySets.Add(psetName);
               }
               
               if (containsReservedPSets)
               {
                  IFCNotificationMessageBox notificationMessageBox = new(Properties.Resources.ReservedPropertySetTooltip)
                  {
                     Owner = this,
                     Title = Properties.Resources.ImportIFCUserDefinedMappingDialogName
                  };
                  bool? ret = notificationMessageBox.ShowDialog();
                  if (!ret.HasValue || ret.Value == false)
                  {
                     return;
                  }
               }
                
               // Initialize observable collections with imported data before selection changes
               InitData();

               SelectedPropertySet = ObservablePropertySets.FirstOrDefault();
            }
            catch (Exception)
            {
               return;
            }
         }
      }

      /// <summary>
      /// Initializes data.
      /// </summary>
      private void InitData()
      {
         InitializeDataTypes();
         InitializePropertyGrid();
         InitializeApplicableEntitiesList();
      }

      private void button_Copy_Click(object sender, RoutedEventArgs e)
      {
         IFCUserDefinedPropertySet currentPSet = GetCurrentPropertySet();
         if (currentPSet == null)
            return;

         WritePropertyInfoToPropertySet(SelectedPropertySet);
         IFCTemplateData data = new(currentPSet.Name, IFCUserDefinedPropertySet.ListPropertySetNames(IFCCommandOverrideApplication.TheDocument),
            isCategoryMapping: false, IFCTemplateData.DialogTypeEnum.PropertySet);
         data.MakeUniqueName();

         IFCCopyTemplate copyDialog = new(data)
         {
            Owner = this
         };

         bool? ret = copyDialog.ShowDialog();
         if (ret.HasValue && ret.Value == true)
         {
            string copyName = copyDialog.Data.NewName;
            try
            {
               IFCUserDefinedPropertySet importedTemplate = currentPSet.CopyPropertySet(IFCCommandOverrideApplication.TheDocument, copyName);
            }
            catch (Exception)
            {
               return;
            }

            ObservablePropertySets.Add(copyName);
            SelectedPropertySet = copyName;
         }
      }

      private void button_Save_Click(object sender, RoutedEventArgs e)
      {
         SaveDialogChanges();
         CommitTransaction();
         StartTransaction();
      }

      private void button_Export_Click(object sender, RoutedEventArgs e)
      {
         SaveDialogChanges();

         FileSaveDialog saveDialog = new FileSaveDialog(Properties.Resources.IFCUserDefinedPropertySetsFilter);
         saveDialog.Title = Properties.Resources.ExportIFCUserDefinedMappingDialogName;

         if (saveDialog.Show() == ItemSelectionDialogResult.Confirmed)
         {
            try
            {
               // TODO: Support (or warn) on cloud paths.
               ModelPath modelPath = saveDialog.GetSelectedModelPath();
               string fileName = ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath);
               IFCUserDefinedPropertySet.ExportToFile(IFCCommandOverrideApplication.TheDocument, fileName);
            }
            catch (Exception)
            {
               return;
            }
         }
      }

      private void button_Delete_Click(object sender, RoutedEventArgs e)
      {
         IFCUserDefinedPropertySet currentPSet = GetCurrentPropertySet();
         if (currentPSet == null)
            return;

         IFCDeleteTemplate deleteTemplateDialog = new(currentPSet.Name)
         {
            Owner = this
         };
         bool? ret = deleteTemplateDialog.ShowDialog();
         if (ret.HasValue && ret.Value == true)
         {
            ObservablePropertySets.Remove(currentPSet.Name);
            IFCCommandOverrideApplication.TheDocument.Delete(currentPSet.Id);
            SelectedPropertySet = ObservablePropertySets.Count > 0 ? ObservablePropertySets[0] : string.Empty;
         }
      }

      private IFCUserDefinedPropertySet GetCurrentPropertySet()
      {
         if (string.IsNullOrEmpty(SelectedPropertySet))
            return null;

         return IFCUserDefinedPropertySet.FindPropertySetByName(IFCCommandOverrideApplication.TheDocument, SelectedPropertySet);
      }

      private void button_Ok_Click(object sender, RoutedEventArgs e)
      {
         foreach (string delayedWarning in validator.DelayedValidationWarnings)
            IFCExport.TheDocument.Application.WriteJournalComment(delayedWarning, true);

         SaveDialogChanges();
         CommitTransaction();
         Close();
      }

      private void button_Cancel_Click(object sender, RoutedEventArgs e)
      {
         DiscardTransactions();
         Close();
      }

      protected override bool OnContextHelp()
      {
         ContextualHelp help = new ContextualHelp(ContextualHelpType.ContextId, "HDialog_IFC_UserDefinedPropertyMapping");
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
         DiscardTransactions();
      }

      private void button_SelectEnitites_Click(object sender, RoutedEventArgs e)
      {
         IFCVersion? versionToUse = ObservableApplicableEntities.Count > 0 ? EntityTreeVersion : null;
         EntityTree entityTree = new(versionToUse,
            GetSelectedEnititesString(), desc: "", singleNodeSelection: false, EntityTree.SelectionStrategyType.Inclusion,
            synchronizeSelectionWithType: false, propagatePreselection: true)
         {
            Owner = this,
            Title = Properties.Resources.IFCEntitySelection
         };
         entityTree.PredefinedTypeTreeView.Visibility = System.Windows.Visibility.Hidden;

         bool? ret = entityTree.ShowDialog();

         if (ret.HasValue && ret.Value == true)
         {
            EntityTreeVersion = IfcSchemaEntityTree.VersionName(entityTree.CurrentIFCVersion);
            ObservableApplicableEntities.Clear();
            foreach (string entity in entityTree.GetSelectedEntityParents().Split(';'))
            {
               if (string.IsNullOrEmpty(entity))
                  continue;
               ObservableApplicableEntities.Add(entity);
            }
         }
      }

      private string GetSelectedEnititesString()
      {
         string selectedEntities = "";
         foreach (string entity in ObservableApplicableEntities)
         {
            selectedEntities += entity + ";";
         }
         return selectedEntities;
      }

      private void button_Rename_Click(object sender, RoutedEventArgs e)
      {
         IFCUserDefinedPropertySet currentPSet = GetCurrentPropertySet();
         if (currentPSet == null)
            return;

         string previousName = currentPSet.Name;
         IFCTemplateData data = new(previousName, IFCUserDefinedPropertySet.ListPropertySetNames(IFCCommandOverrideApplication.TheDocument),
            isCategoryMapping: false, IFCTemplateData.DialogTypeEnum.PropertySet);

         IFCRenameTemplate renamePSetDialog = new(data)
         {
            Owner = this
         };
         bool? ret = renamePSetDialog.ShowDialog();
         if (ret.HasValue && ret.Value && !string.IsNullOrWhiteSpace(renamePSetDialog.Data.NewName))
         {
            string newName = renamePSetDialog.Data.NewName;
            currentPSet.Name = newName;
            int ind = ObservablePropertySets.IndexOf(previousName);
            if (ind >= 0)
            {
               ObservablePropertySets.RemoveAt(ind);
               ObservablePropertySets.Insert(ind, newName);
               SelectedPropertySet = newName;
            }
         }

      }

      private void button_ResetEnitites_Click(object sender, RoutedEventArgs e)
      {
         ObservableApplicableEntities.Clear();
      }

      private void button_RevitPropertyEdit_Click(object sender, RoutedEventArgs e)
      {
         UserDefinedPropertyInfo propertyInfo = dataGrid_UserDefinedProperties.SelectedItem as UserDefinedPropertyInfo;
         if (propertyInfo == null)
            return;

         bool isTableProperty = propertyInfo.PropertyType == IFCUserDefinedPropertyType.Table;

         IFCRevitPropertySelector propertySelector = new(
           propertyInfo.RevitParameterInfo, SelectedPropertySet, SelectedProperty.IFCPropertyName, SelectedProperty.PropertyDataType,
           EntityTreeVersion, PropertySetupType.UserDefinedPropertySets, ObservableApplicableEntities.ToList(), isTableProperty)
         {
            Owner = this,
         };

         bool? ret = propertySelector.ShowDialog();
         if (ret.HasValue && ret.Value == true)
         {
            propertyInfo.RevitParameterInfo = propertySelector.SelectedRevitParameter;

            // Default to the Revit property name if it isn't set.
            if (string.IsNullOrEmpty(propertyInfo.IFCPropertyName))
               propertyInfo.IFCPropertyName = propertySelector.SelectedRevitParameter.Name;
         }
      }

      private void button_AddRow_Click(object sender, RoutedEventArgs e)
      {
         if (string.IsNullOrEmpty(SelectedPropertySet))
            return;

         ObservableProperties.Add(CreatePropertyInfoForSelectedSet());
      }

      private UserDefinedPropertyInfo CreatePropertyInfoForSelectedSet()
      {
         UserDefinedPropertyInfo newProperty = new();

         if (string.IsNullOrEmpty(SelectedPropertySet))
            return newProperty;

         if ((AllDataTypes?.Count ?? 0) == 0)
            return newProperty;

         bool? isQuantitySet = IsQuantitySetSelected(SelectedPropertySet);
         string defaultDataType = (isQuantitySet.HasValue && isQuantitySet.Value) ?
            UserDefinedPropertyInfo.DefaultDataTypeQuantity : UserDefinedPropertyInfo.DefaultDataTypeProperty;

         if (!AllDataTypes.Contains(defaultDataType))
            defaultDataType = AllDataTypes[0];

         newProperty.PropertyDataType = defaultDataType;
         newProperty.PropertyDataTypeDefined = defaultDataType;

         return newProperty;
      }

      public static bool IsValidPropertySetName(string propertySetName, IList<string> existingNames)
      {
         propertySetName = propertySetName?.TrimStart()?.TrimEnd();
         return (!(string.IsNullOrWhiteSpace(propertySetName)
            || (existingNames?.Contains(propertySetName, System.StringComparer.OrdinalIgnoreCase) ?? false)))
            && NamingUtils.IsValidName(propertySetName)
            && IFCUserDefinedPropertySet.IsValidName(IFCCommandOverrideApplication.TheDocument, propertySetName)
            && !UserDefinedPropertySetValidator.IsReserved(propertySetName);
      }

      public static bool IsValidPropertyName(string propertyName, IList<string> existingNames)
      {
         propertyName = propertyName?.TrimStart()?.TrimEnd();
         return (!(string.IsNullOrWhiteSpace(propertyName)
            || (existingNames?.Contains(propertyName, System.StringComparer.OrdinalIgnoreCase) ?? false)));
      }

      private void SaveDialogChanges()
      {
         WritePropertyInfoToPropertySet(SelectedPropertySet);
      }

      private void StartTransaction()
      {
         if (!_transaction.HasStarted())
            _transaction.Start();
      }

      private void CommitTransaction()
      {
         // Save template changes
         if (_transaction.HasStarted())
         {
            _transaction.Commit();
            IsModified = true;
         }
      }

      private void DiscardTransactions()
      {
         // Roll back template changes
         if (_transaction.HasStarted())
            _transaction.RollBack();
      }

      private void listBox_PropertySets_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         // Save current template before switching to new one
         if (e.RemovedItems.Count > 0)
         {
            string prevPSetName = e.RemovedItems[0] as string;
            if (!string.IsNullOrEmpty(prevPSetName))
            {
               WritePropertyInfoToPropertySet(prevPSetName);

               if (_transaction.HasStarted())
                  _transaction.Commit();

               if (_transaction.HasEnded())
                  _transaction.Start();
            }
         }

         InitData();
      }

      private void InitializeDataTypes()
      {
         AllDataTypes.Clear();

         if (SelectedPropertySet == null)
            return;

         bool? isQuantitySet = IsQuantitySetSelected(SelectedPropertySet);
         if (!isQuantitySet.HasValue)
            return;

         ObservableCollection<string> dataTypes = GetDataTypesFromCache(isQuantitySet.Value);

         // Sort the data types alphabetically before adding to AllDataTypes
         var sortedDataTypes = dataTypes.OrderBy(x => x).ToList();

         foreach (string dataType in sortedDataTypes)
         {
            AllDataTypes.Add(dataType);
         }
      }

      private ObservableCollection<string> GetDataTypesFromCache(bool isQuantitySet)
      {
         if (_dataTypesCache == null)
         {
            _dataTypesCache = new Dictionary<bool, ObservableCollection<string>>();

            _dataTypesCache.TryAdd(false, new(Enum.GetNames(typeof(PropertyType)).ToList()));
            _dataTypesCache.TryAdd(true, new(Enum.GetNames(typeof(QuantityType)).ToList()));
         }

         return (_dataTypesCache.ContainsKey(isQuantitySet)) ? _dataTypesCache[isQuantitySet] : new();
      }

      public static bool? IsQuantitySetSelected(string selectedSetName)
      {
         if (string.IsNullOrEmpty(selectedSetName))
            return null;

         IFCUserDefinedPropertySet propertySet =
            IFCUserDefinedPropertySet.FindPropertySetByName(IFCCommandOverrideApplication.TheDocument, selectedSetName);

         if (propertySet == null)
            return null;

         return propertySet.PropertySetType == IFCUserDefinedPropertySetType.QuantitySet;
      }

      public static bool? IsIFCAttributeSetSelected(string selectedSetName)
      {
         if (string.IsNullOrEmpty(selectedSetName))
            return null;

         IFCUserDefinedPropertySet propertySet =
            IFCUserDefinedPropertySet.FindPropertySetByName(IFCCommandOverrideApplication.TheDocument, selectedSetName);

         if (propertySet == null)
            return null;

         return propertySet.PropertySetType == IFCUserDefinedPropertySetType.IFCAttributeSet;
      }

      private void WritePropertyInfoToPropertySet(string psetName)
      {
         if (string.IsNullOrEmpty(psetName))
            return;

         IFCUserDefinedPropertySet userDefinedPSet = IFCUserDefinedPropertySet.FindPropertySetByName(IFCCommandOverrideApplication.TheDocument, psetName);
         if (userDefinedPSet == null)
            return;

         WritePropertyInfoToPropertySet(userDefinedPSet);
      }

      private void WritePropertyInfoToPropertySet(IFCUserDefinedPropertySet userDefinedPSet)
      {
         if (userDefinedPSet == null)
            return;

         userDefinedPSet.ClearPropertySet();

         foreach (UserDefinedPropertyInfo propertyInfo in ObservableProperties)
         {
            string propertyName = propertyInfo.IFCPropertyName;
            if (string.IsNullOrEmpty(propertyName) || userDefinedPSet.IsPropertyAMemberOfPropertySet(propertyName))
               continue;

            userDefinedPSet.AddProperty(new IFCUserDefinedProperty(propertyName, propertyInfo.RevitParameterInfo.Id,
               propertyInfo.RevitParameterInfo.Name, propertyInfo.PropertyDataType, propertyInfo.PropertyType,
               propertyInfo.PropertyDataTypeDefined));
         }
         userDefinedPSet.SetApplicableEntities(ObservableApplicableEntities.ToList());
      }

      private void InitializePropertyGrid()
      {
         if (string.IsNullOrEmpty(SelectedPropertySet))
            return;

         ObservableProperties.Clear();

         IList<IFCUserDefinedProperty> properties =
            IFCUserDefinedPropertySet.FindPropertySetByName(IFCCommandOverrideApplication.TheDocument, SelectedPropertySet)?.GetProperties();

         InitializeObservableProperties(properties);
      }

      private void InitializeObservableProperties(IList<IFCUserDefinedProperty> dataBaseProperties)
      {
         if ((dataBaseProperties?.Count ?? 0) == 0)
            return;

         foreach (var property in dataBaseProperties)
         {
            ObservableProperties.Add(new()
            {
               IFCPropertyName = property.IFCPropertyName,
               PropertyDataType = property.DataType,
               PropertyType = property.PropertyType,
               PropertyDataTypeDefined = property.DataTypeDefined,
               RevitParameterInfo = new RevitParameterInfo(property.RevitPropertyName, property.RevitPropertyId),
            });
         }
      }

      private void InitializeApplicableEntitiesList()
      {
         ObservableApplicableEntities.Clear();

         if (string.IsNullOrEmpty(SelectedPropertySet))
            return;

         IList<string> applicableEntities =
            IFCUserDefinedPropertySet.FindPropertySetByName(IFCCommandOverrideApplication.TheDocument, SelectedPropertySet)?.GetApplicableEntities();

         if (applicableEntities == null)
            return;

         foreach (var entity in applicableEntities)
         {
            ObservableApplicableEntities.Add(entity);
         }
      }

      private void button_RemoveRow_Click(object sender, RoutedEventArgs e)
      {
         if (SelectedProperty == null)
            return;

         ObservableProperties.Remove(SelectedProperty);
      }

      private void button_RemoveEntity_Click(object sender, RoutedEventArgs e)
      {
         if (sender is not Button button || button.DataContext is not string entity)
            return;

         ObservableApplicableEntities.Remove(entity);
      }

      #region ComboBox_DataType Event Handlers

      private bool _isFilteringInProgress = false; // Prevent cascade TextChanged during filtering

      private void FilterDataTypes(string filterText, ComboBox comboBox, bool forceFullList)
      {
         var targetFilteredCollection = ComboBoxFilteringUtilities.GetOrCreateCollection(comboBox, AllDataTypes);
         ComboBoxFilteringUtilities.FilterCollection(comboBox, targetFilteredCollection, AllDataTypes, filterText, forceFullList);
      }

      private void ComboBox_DataType_Loaded(object sender, RoutedEventArgs e)
      {
         if (sender is not ComboBox comboBox)
            return;

         ComboBoxFilteringUtilities.GetOrCreateCollection(comboBox, AllDataTypes);

         ComboBoxFilteringUtilities.AttachTextChangedHandler(comboBox, ComboBox_DataType_TextChanged);

         if (comboBox.DataContext is not UserDefinedPropertyInfo dataContext)
            return;

         // Set initial selection from appropriate property when ComboBox loads
         string initialValue = IsTableSpecificDataType(comboBox) ?
            dataContext.PropertyDataTypeDefined : dataContext.PropertyDataType;

         if (!string.IsNullOrEmpty(initialValue))
         {
            comboBox.SelectedItem = initialValue;
            comboBox.Text = initialValue;
         }
      }

      private void ComboBox_DataType_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         if (sender is not ComboBox comboBox ||
            e.AddedItems.Count == 0 || comboBox.SelectedItem == null ||
            comboBox.DataContext is not UserDefinedPropertyInfo dataContext)
            return;

         string newValue = comboBox.SelectedItem.ToString() ?? string.Empty;

         // Update the correct property based on ComboBox type
         if (IsTableSpecificDataType(comboBox))
            dataContext.PropertyDataTypeDefined = newValue;
         else
            dataContext.PropertyDataType = newValue;
      }

      private static bool IsTableSpecificDataType(ComboBox comboBox)
      {
         return comboBox?.Name == "comboBox_DataTypeDefined";
      }

      private void ComboBox_DataType_DropDownOpened(object sender, EventArgs e)
      {
         if (sender is not ComboBox comboBox)
            return;

         // If user manually opened dropdown (not programmatically by our TextChanged)
         // AND current selected item is an exact match, then show full list
         if (!_isFilteringInProgress && comboBox.SelectedItem != null)
         {
            string selectedItemText = comboBox.SelectedItem.ToString() ?? string.Empty;
            bool isExactMatch = AllDataTypes?.Any(item => string.Equals(item, selectedItemText, StringComparison.OrdinalIgnoreCase)) ?? false;
            if (isExactMatch)
            {
               FilterDataTypes(selectedItemText, comboBox, forceFullList: true);
            }
         }
      }

      private void ComboBox_DataType_TextChanged(ComboBox comboBox, TextChangedEventArgs e)
      {
         if (comboBox == null)
            return;

         // Prevent cascade: Ignore TextChanged events that occur during filtering
         if (_isFilteringInProgress)
            return;

         _isFilteringInProgress = true;

         try
         {
            string currentText = comboBox.Text ?? "";
            FilterDataTypes(currentText, comboBox, forceFullList: false);

            // Open dropdown to show filtered results - but no for programmatic text changes (like initial loading)
            if (comboBox.IsKeyboardFocusWithin)
            {
               ComboBoxFilteringUtilities.OpenDropDownSuppressingHighlight(comboBox);
            }
         }
         finally
         {
            // Always reset flag, even if exception occurs
            _isFilteringInProgress = false;
         }
      }
      #endregion
   }


   /// <summary>
   /// Extracts the row index from the DataGridRow item.
   /// It is used to set AutomationId for valid journal playback.
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

   /// <summary>
   /// Extracts the row index from the DataGridRow item.
   /// It is used too set AutomationId for valid journal playback.
   /// </summary>
   public class RowIndexSelectedConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter,
                            System.Globalization.CultureInfo culture)
      {
         return value != null;
      }

      public object ConvertBack(object value, Type targetType, object parameter,
                                System.Globalization.CultureInfo culture)
      {
         return false;
      }
   }

   #region Converters
   public class IsPropertySetSelectedConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         string mappedName = (string)value;
         if (string.IsNullOrEmpty(mappedName))
         {
            return false;
         }

         if (bool.TryParse((parameter as string), out bool isParameterVal) && isParameterVal)
         {
            IFCUserDefinedPropertySet propertySet = IFCUserDefinedPropertySet.FindPropertySetByName(IFCCommandOverrideApplication.TheDocument, mappedName);
            if (propertySet == null || (propertySet.PropertySetType == IFCUserDefinedPropertySetType.IFCAttributeSet))
            {
               return false;
            }
         }

         return true;
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }


   public class RevitParameterConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         if (value is RevitParameterInfo revitParameter)
         {
            return revitParameter.Name;
         }

         return string.Empty;
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         return value;
      }
   }

   public class QuantityLabelVisibilityConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         string selectedPropertySet = (string)value;

         if (string.IsNullOrEmpty(selectedPropertySet))
            return System.Windows.Visibility.Hidden;

         bool? isQuantitySet = IFCUserDefinedPropertyMapping.IsQuantitySetSelected(selectedPropertySet);

         return isQuantitySet.HasValue && isQuantitySet.Value ?
            System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
      }

      object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         return string.Empty;
      }
   }

   public class IFCAttributesLabelVisibilityConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         string selectedPropertySet = (string)value;

         if (string.IsNullOrEmpty(selectedPropertySet))
            return System.Windows.Visibility.Hidden;

         bool? isIFCAttributeSet = IFCUserDefinedPropertyMapping.IsIFCAttributeSetSelected(selectedPropertySet);

         return isIFCAttributeSet.HasValue && isIFCAttributeSet.Value ?
            System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
      }

      object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         return string.Empty;
      }
   }

   public class IFCAttributesColumnReadOnlyConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         string selectedPropertySet = (string)value;

         bool result;
         if (string.IsNullOrEmpty(selectedPropertySet))
            result = true;
         else
         {
            bool? isIFCAttributeSet = IFCUserDefinedPropertyMapping.IsIFCAttributeSetSelected(selectedPropertySet);
            result = (isIFCAttributeSet.HasValue && isIFCAttributeSet.Value);
         }

         // If parameter is "invert", return the opposite (for IsEnabled property)
         if (parameter != null &&
             parameter.ToString().Equals("invert", StringComparison.OrdinalIgnoreCase))
         {
            return !result;
         }

         return result;
      }

      object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }

   public class TablePropertyTypeVisibilityConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         if (value is bool hasTableProperties)
         {
            return hasTableProperties ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
         }

         return System.Windows.Visibility.Collapsed;
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }

   public class TablePropertyTypeEnabledConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         if (value is IFCUserDefinedPropertyType propertyType)
         {
            return propertyType == IFCUserDefinedPropertyType.Table;
         }

         return false;
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }
   #endregion


}
