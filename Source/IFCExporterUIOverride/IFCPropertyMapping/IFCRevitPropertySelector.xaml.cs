using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExternalData;
using Autodesk.UI.Windows;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Utility;
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
using static BIM.IFC.Export.UI.PropertySelectorModel;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// The class represents the item of property selector list.
   /// </summary>
   public class ParameterListItem : INotifyPropertyChanged
   {  
      public ParameterListItem() { }
      public ParameterListItem(string name, ElementId id, bool isChecked)
      {
         Name = name;
         Id = id;
         IsChecked = isChecked;
      }
      public string Name { get; set; }
      public ElementId Id { get; set; }

      private bool _isChecked;
      public bool IsChecked
      {
         get { return _isChecked; }
         set
         {
            _isChecked = value;
            OnPropertyChanged(nameof(IsChecked));
         }
      }

      public event PropertyChangedEventHandler PropertyChanged;
      protected void OnPropertyChanged([CallerMemberName] string name = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }
   }

   /// <summary>
   /// An enumeration that filters the type of parameter as built-in, instance or type.
   /// </summary>
   /// <remarks>
   /// The Revit API does not provide a performant way to determine if built-in parameters are instace or type
   /// parameters, so we group them separately.</remarks>
   public enum ParameterType
   {
      Builtin,
      Extended,
      Instance,
      Type,
      Unknown
   };

   /// <summary>
   /// The class keeps the Revit parameter data that is used in the property selector.
   /// </summary>
   public class RevitParameterData
   {
      public string Name { get; set; } = string.Empty;
      public ElementId Id { get; set; } = ElementId.InvalidElementId;
      public ParameterType ParameterType { get; set; } = ParameterType.Unknown;
      public StorageType StorageType { get; set; } = StorageType.None;
      public ForgeTypeId DataType { get; set; } = null;
   }

   /// <summary>
   /// The model that contains the property selector data.
   /// </summary>
   public class PropertySelectorModel
   {
      /// <summary>
      /// Used to indicate which parameters to display in UI
      /// </summary>
      public enum PropertyFilterEnum
      {
         [Description("All")]
         All,
         [Description("Built-in")]
         Builtin,
         [Description("Extended")]
         Extended,
         [Description("Instance")]
         Instance,
         [Description("Type")]
         Type
      }

      public SortedDictionary<string, Dictionary<PropertyFilterEnum, List<ParameterListItem>>> Data = new();

      /// <summary>
      /// Initializes the model from the parameter data grouped by categories.
      /// </summary>
      public void InitializeFromParameterData(Dictionary<ElementId, List<RevitParameterData>> categoryParametersData)
      {
         Data.Clear();

         if ((categoryParametersData?.Count ?? 0) == 0)
            return;

         Document doc = IFCCommandOverrideApplication.TheDocument;

         foreach ((ElementId categoryId, List<RevitParameterData> parameterData) in categoryParametersData)
         {
            Category category = Category.GetCategory(doc, categoryId);

            string categoryName = category?.Name;
            if (string.IsNullOrEmpty(categoryName) || (parameterData?.Count ?? 0) == 0)
               continue;

            string categoryParentName = category?.Parent?.Name;
            string fullCategoryName = string.IsNullOrEmpty(categoryParentName) ?
               categoryName : categoryParentName + ": " + categoryName;

            // Create parameter items from the data
            List<ParameterListItem> typeParameterItems =
               parameterData.Where(param => !string.IsNullOrEmpty(param?.Name) && param.ParameterType == ParameterType.Type)
               .Select(data => new ParameterListItem(data.Name, data.Id, isChecked: false))
               .OrderBy(x => x.Name, StringComparer.CurrentCulture).ToList();

            List<ParameterListItem> instanceParameterItems =
               parameterData.Where(param => !string.IsNullOrEmpty(param?.Name) && param.ParameterType == ParameterType.Instance)
               .Select(data => new ParameterListItem(data.Name, data.Id, isChecked: false))
               .OrderBy(x => x.Name, StringComparer.CurrentCulture).ToList();

            List<ParameterListItem> builtinParameterItems =
               parameterData.Where(param => !string.IsNullOrEmpty(param?.Name) && param.ParameterType == ParameterType.Builtin)
               .Select(data => new ParameterListItem(data.Name, data.Id, isChecked: false))
               .OrderBy(x => x.Name, StringComparer.CurrentCulture).ToList();

            List<ParameterListItem> extendedParameterItems =   
               parameterData.Where(param => !string.IsNullOrEmpty(param?.Name) && param.ParameterType == ParameterType.Extended)
               .Select(data => new ParameterListItem(data.Name, data.Id, isChecked: false))
               .OrderBy(x => x.Name, StringComparer.CurrentCulture).ToList();

            List<ParameterListItem> allParameterItems = typeParameterItems.Union(instanceParameterItems).Union(builtinParameterItems)
               .Union(extendedParameterItems)
               .OrderBy(x => x.Name, StringComparer.CurrentCulture).ToList();

            Dictionary<PropertyFilterEnum, List<ParameterListItem>> filterOptionPairs = new()
            {
               { PropertyFilterEnum.All, allParameterItems },
               { PropertyFilterEnum.Builtin, builtinParameterItems},
               { PropertyFilterEnum.Extended, extendedParameterItems},
               { PropertyFilterEnum.Instance, instanceParameterItems },
               { PropertyFilterEnum.Type, typeParameterItems }
            };

            Data.TryAdd(fullCategoryName, filterOptionPairs);
         }
      }
   }

   /// <summary>
   /// Interaction logic for IFCRevitPropertySelector.xaml
   /// </summary>
   public partial class IFCRevitPropertySelector : ChildWindow, INotifyPropertyChanged
   {
      private PropertySelectorModel _model = new();

      public string SelectedCategory { get; set; } = Properties.Settings.Default.LastSelectedCategory;

      public RevitParameterInfo SelectedRevitParameter { get; set; }

      public bool DisableParameterFilter { get; set; } = false;

      private readonly string SelectedProperty = string.Empty;

      private readonly bool IsTableProperty = false;

      private static Dictionary<BuiltInCategory, string> sSpecificMappings = new()
      {
         { BuiltInCategory.OST_StructuralFraming, "IfcBeam" }
      };

      /// <summary>
      /// The map that keeps the IFC entity type to Revit categories mapping.
      /// </summary>
      private static Dictionary<string, HashSet<ElementId>> _ifcEntityToCategoriesMap = new();

      private static ElementId _defaultCategoryId = null;

      /// <summary>
      /// The cache that keeps the Revit parameters data of each category.
      /// </summary>
      private static Dictionary<ElementId, List<RevitParameterData>> _categoryParametersCache = new();

      private ObservableCollection<RevitParameterFilter> _parametersFilter = new();
      public ObservableCollection<RevitParameterFilter> ParametersFilter
      {
         get { return _parametersFilter; }
         set
         {
            _parametersFilter = value;
            OnPropertyChanged(nameof(ParametersFilter));
         }
      }

      public event PropertyChangedEventHandler PropertyChanged;
      protected void OnPropertyChanged([CallerMemberName] string name = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }

      public IFCRevitPropertySelector(RevitParameterInfo selectedRevitParameter, string selectedPSet,
         string selectedProperty, string propertyDataType, IFCSchemaFileVersion ifcSchemaVersion, PropertySetupType propertySetup,
         IList<string> applicableEntities, bool isTableProperty)
      {
         SelectedRevitParameter = selectedRevitParameter;
         SelectedProperty = selectedProperty;
         IsTableProperty = isTableProperty;
         IFCVersion ifcVersion = GetCanonicalIFCVersion(ifcSchemaVersion);


         InitializeComponent();
         DataContext = this;
         Document doc = IFCCommandOverrideApplication.TheDocument;
         if (doc is null)
            return;

         // 1. Get the information that defines the applicable Revit parameters for this property/quantity.
         ApplicableParameterInfo applicableParamInfo =
            CreateApplicableParameterInfo(propertySetup, selectedPSet, SelectedProperty, propertyDataType, ifcVersion, applicableEntities);

         if (applicableParamInfo is null)
            return;

         // The list of Revit categories exported to applicable IFC entity types.
         HashSet<ElementId> applicableCategories = GetApplicableCategories(applicableParamInfo.EntityTypes, ifcVersion);
         if ((applicableCategories?.Count ?? 0) == 0)
            return;

         // 2. Get all parameters of each category
         Dictionary<ElementId, List<RevitParameterData>> categoryParametersData = GetCategoryParameters(applicableCategories, applicableParamInfo);

         // 3. Filter parameters by applicable info.
         Dictionary<ElementId, List<RevitParameterData>> filteredCategoryParametersData = categoryParametersData?
            .Where(pair => pair.Value is not null && pair.Value.Count > 0)
            .ToDictionary(pair => pair.Key, pair => pair.Value
               .Where(param => applicableParamInfo.IsApplicableParameter(param))
               .ToList());

         // 4. Populate the model with parameters of each category.
         _model.InitializeFromParameterData(filteredCategoryParametersData);

         comboBox_Category.ItemsSource = _model.Data.Keys;

         if (!PreselectParameter() && _model.Data.Count > 0)
         {
            if (String.IsNullOrEmpty(SelectedCategory) || !_model.Data.ContainsKey(SelectedCategory))
               SelectedCategory = _model.Data.Keys.First();
         }
      }

      public ApplicableParameterInfo CreateApplicableParameterInfo(PropertySetupType propertySetup,
         string propertySetName, string propertyName, string propertyDataType, IFCVersion ifcVersion, IList<string> applicableEntities)
      {
         ApplicableParameterInfo applicableInfo = new();
         switch (propertySetup)
         {
            case PropertySetupType.IfcCommonPropertySets:
               {
                  PropertySetDescription psetDescription =
                     IFCPropertyMappingModel.GetCachedIfcCommonPSetDescription(propertySetName, ifcVersion);

                  if (psetDescription is null)
                     return null;

                  foreach (PropertySetEntry entry in psetDescription.Entries)
                  {
                     if (entry.PropertyName != propertyName)
                        continue;

                     var (success, storageType, dataTypes) = GetApplicableDataType(entry.PropertyType);
                     if (!success)
                        return null;

                     applicableInfo.StorageType = storageType;
                     applicableInfo.DataTypes = dataTypes;

                     break;
                  }

                  applicableInfo.EntityTypes = psetDescription.EntityTypes;
                  break;
               }
            case PropertySetupType.IfcBaseQuantities:
               {
                  QuantityDescription quantityDescription =
                     IFCPropertyMappingModel.GetCachedBaseQuantityDescription(propertySetName, ifcVersion);

                  if (quantityDescription is null)
                     return null;

                  foreach (QuantityEntry entry in quantityDescription.Entries)
                  {
                     if (entry.PropertyName != propertyName)
                        continue;

                     QuantityType quantityType = entry.QuantityType;
                     if (!UnitMappingUtil.GetRevitDataTypeFromIfcQuantityType(quantityType,
                        out var applicableStorageType, out var applicableDataTypes))
                        return null;

                     applicableInfo.StorageType = applicableStorageType;
                     applicableInfo.DataTypes = applicableDataTypes;
                     break;
                  }

                  applicableInfo.EntityTypes = quantityDescription.EntityTypes;
                  break;
               }
            case PropertySetupType.UserDefinedPropertySets:
               {
                  if ((applicableEntities?.Count ?? 0) == 0)
                     return null;

                  applicableInfo.EntityTypes =
                     ExporterInitializer.GetIfcEntityTypesFromStrings(applicableEntities, exportPre4: false);

                  if (!Enum.TryParse(propertyDataType, true, out PropertyType propertyType))
                     return null;

                  var (success, storageType, dataTypes) = GetApplicableDataType(propertyType);
                  if (!success)
                     return null;

                  applicableInfo.StorageType = storageType;
                  applicableInfo.DataTypes = dataTypes;

                  break;
               }
         }

         return applicableInfo;
      }

      public (bool success, StorageType, HashSet<ForgeTypeId>) GetApplicableDataType(PropertyType propertyType)
      {
         StorageType storageType = StorageType.None;
         HashSet<ForgeTypeId> dataTypes = new HashSet<ForgeTypeId>();

         if (IsTableProperty)
         {
            storageType = StorageType.String;
            dataTypes = [SpecTypeId.String.MultilineText];
         }
         else
         {
            if (!UnitMappingUtil.GetRevitDataTypeFromIfcPropertyType(propertyType,
               out var applicableStorageType, out var applicableDataTypes))
               return (false, storageType, dataTypes);

            storageType = applicableStorageType;
            dataTypes = applicableDataTypes;
         }
         return (true, storageType, dataTypes);
      }

      private HashSet<ElementId> GetApplicableCategories(HashSet<IFCEntityType> applicableEntityTypes, IFCVersion ifcVersion)
      {
         if ((applicableEntityTypes?.Count ?? 0) == 0)
            return null;

         HashSet<ElementId> applicableCategories = new();
         foreach (IFCEntityType applicableEntityType in applicableEntityTypes)
         {
            IFCEntityType entityType = GetEntityFromEntityType(applicableEntityType, ifcVersion);
            if (entityType == IFCEntityType.UnKnown)
               continue;

            // Add categories that are exported to exact ifc entity type 
            if (_ifcEntityToCategoriesMap.TryGetValue(entityType.ToString(), out HashSet<ElementId> categories) && categories is not null)
               applicableCategories.UnionWith(categories);

            // Add categories that are exported to subtypes of the ifc entity type
            foreach ((string entityName, HashSet<ElementId> categorySet) in _ifcEntityToCategoriesMap)
            {
               if (!Enum.TryParse(entityName, out IFCEntityType mappedEntityType) ||
                  !IfcSchemaEntityTree.IsSubTypeOf(ifcVersion, mappedEntityType, entityType, strict: true))
                  continue;

               applicableCategories.UnionWith(categorySet);
            }
         }

         if (applicableCategories.Count == 0 && _defaultCategoryId is not null)
            applicableCategories.Add(_defaultCategoryId);

         return applicableCategories;
      }

      private static IFCVersion GetCanonicalIFCVersion(IFCSchemaFileVersion schemaFileVersion)
      {
         return schemaFileVersion switch
         {
            IFCSchemaFileVersion.IFC2X2 => IFCVersion.IFC2x2,
            IFCSchemaFileVersion.IFC2X3 => IFCVersion.IFC2x3,
            IFCSchemaFileVersion.IFC4 => IFCVersion.IFC4,
            IFCSchemaFileVersion.IFC4RV => IFCVersion.IFC4RV,
            IFCSchemaFileVersion.IFC4X3 => IFCVersion.IFC4x3,
            _ => IFCVersion.Default
         };
      }

      private IFCEntityType GetEntityFromEntityType(IFCEntityType applicableEntityType, IFCVersion ifcVersion)
      {
         string entityName = applicableEntityType.ToString();
         IfcSchemaEntityTree theTree = IfcSchemaEntityTree.GetEntityDictFor(ifcVersion, null);
         int typeLen = 4;
         bool isType = entityName.EndsWith("Type", StringComparison.CurrentCultureIgnoreCase);
         if (!isType)
         {
            if (entityName.Equals("IfcDoorStyle", StringComparison.InvariantCultureIgnoreCase)
               || entityName.Equals("IfcWindowStyle", StringComparison.InvariantCultureIgnoreCase))
            {
               isType = true;
               typeLen = 5;
            }
         }

         if (!isType)
            return applicableEntityType;

         IFCEntityType instanceType = IFCEntityType.UnKnown;

         // Get the instance
         string instName = entityName.Substring(0, entityName.Length - typeLen);
         IfcSchemaEntityNode node = theTree.Find(instName);
         if (node != null && !node.IsAbstract)
         {
            IFCEntityType instType = IFCEntityType.UnKnown;
            if (IFCEntityType.TryParse(instName, true, out instType))
               instanceType = instType;
         }
         else
         {
            // If not found, try non-abstract supertype derived from the type
            node = IfcSchemaEntityTree.FindNonAbsInstanceSuperType(theTree, ifcVersion, instName);
            if (node != null)
            {
               IFCEntityType instType = IFCEntityType.UnKnown;
               if (IFCEntityType.TryParse(node.Name, true, out instType))
                  instanceType = instType;
            }
         }

         return instanceType;
      }

      private Dictionary<ElementId, List<RevitParameterData>> GetCategoryParameters(HashSet<ElementId> applicableCategories, ApplicableParameterInfo applicableParamInfo)
      {
         if ((applicableCategories?.Count ?? 0) == 0 || applicableParamInfo is null)
            return null;

         Dictionary<ElementId, List<RevitParameterData>> categoryParameters = new();

         Document doc = IFCCommandOverrideApplication.TheDocument;
         foreach (ElementId categoryId in applicableCategories)
         {
            Category category = Category.GetCategory(doc, categoryId);
            if (category is null)
               continue;

            List<RevitParameterData> applicableParameters = GetSingleCategoryParameters(category);

            // Get parameters from parent category if nothing was found 
            Category categoryToAdd = category;
            if ((applicableParameters?.Count ?? 0) == 0)
            {
               Category parentCategory = category.Parent;
               if (parentCategory is not null && !categoryParameters.ContainsKey(parentCategory.Id))
               {
                  applicableParameters = GetSingleCategoryParameters(parentCategory);
                  categoryToAdd = parentCategory;
               }
            }

            if ((applicableParameters?.Count ?? 0) == 0)
               continue;
            categoryParameters.Add(categoryToAdd.Id, applicableParameters);
         }

         return categoryParameters;
      }

      private static List<RevitParameterData> GetSingleCategoryParameters(Category category)
      {
         if (_categoryParametersCache.TryGetValue(category.Id, out List<RevitParameterData> applicableParameters))
            return applicableParameters;

         applicableParameters = [];

         Document document = IFCCommandOverrideApplication.TheDocument;

         // We do this in two passes.
         // 1. Get all project parameter elements.
         // 2. Get all parameters in families.

         BindingMap map = IFCCommandOverrideApplication.TheBindings;

         List<ElementId> categories = [category.Id];
         ICollection<ElementId> parameterIds = ParameterFilterUtilities.GetFilterableParametersInCommon(document, categories);
         foreach (ElementId parameterId in parameterIds)
         {
            long parameterIdValue = parameterId.Value;
            if (parameterIdValue > 0)
            {
               ParameterElement parameterElement = document.GetElement(parameterId) as ParameterElement;

               InternalDefinition parameterDefinition = parameterElement?.GetDefinition();
               if (parameterDefinition is null)
                  continue;
               // Shared or extended parameter.	
               Autodesk.Revit.DB.Binding binding = map.get_Item(parameterDefinition);
               if (binding is null && parameterElement is not ExtendedParameterElement)
                  continue;

               ParameterType parameterType = (binding is not null) ?
                  ((binding is TypeBinding) ? ParameterType.Type : ParameterType.Instance) :
                  ParameterType.Extended;

               applicableParameters.Add(new RevitParameterData()
               {
                  Name = parameterDefinition.Name,
                  Id = parameterId,
                  ParameterType = parameterType,
                  DataType = parameterDefinition.GetDataType(),
                  StorageType = StorageType.None
               });

               continue;
            }

            StorageType storageType = StorageType.None;

            BuiltInParameter builtInParameter = (BuiltInParameter)parameterIdValue;
            if (PropertyUtil.ProxyParameter.IsProxyParameter(builtInParameter))
            {
               storageType = StorageType.String;
            }
            else
            {
               ForgeTypeId forgeTypeId = ParameterUtils.GetParameterTypeId(builtInParameter);
               if (forgeTypeId?.Empty() ?? true)
                  continue;
               storageType = document.GetTypeOfStorage(forgeTypeId);
            }

            applicableParameters.Add(new RevitParameterData()
            {
               Name = LabelUtils.GetLabelFor(builtInParameter),
               Id = parameterId,
               ParameterType = ParameterType.Builtin,
               StorageType = storageType,
               DataType = null
            });
         }

         FilteredElementCollector familySymbolCollector = new(document);
         IList<Element> familySymbols = familySymbolCollector.OfClass(typeof(FamilySymbol)).ToElements();
         long categoryIdValue = category.Id.Value;
         HashSet<ElementId> visitedFamilies = [];
         HashSet<string> foundNames = [];
         foreach (FamilySymbol familySymbol in familySymbols)
         {
            if ((long) familySymbol?.Category?.Id?.Value != categoryIdValue)
               continue;

            ElementId familyId = familySymbol.Family.Id;
            if (visitedFamilies.Contains(familyId))
               continue;
            visitedFamilies.Add(familyId);

            ParameterSet parameterSet = familySymbol.Parameters;
            foreach (Parameter parameter in parameterSet)
            {
               ElementId parameterId = parameter?.Id ?? ElementId.InvalidElementId;
               if (parameterId.Value < 0 || parameter.IsShared)
                  continue;

               InternalDefinition parameterDefinition = parameter.Definition as InternalDefinition;
               if (parameterDefinition is null)
                  continue;

               // We will only include each one once.
               string name = parameterDefinition.Name;
               if (foundNames.Contains(name))
                  continue;
               foundNames.Add(name);

               applicableParameters.Add(new RevitParameterData()
               {
                  Name = name,
                  Id = parameterId,
                  ParameterType = ParameterType.Type,
                  DataType = parameterDefinition.GetDataType(),
                  StorageType = parameter.StorageType
               });
            }
         }

         _categoryParametersCache.Add(category.Id, applicableParameters);

         return applicableParameters;
      }

      private bool PreselectParameter()
      {
         if (SelectedRevitParameter is null ||
            SelectedRevitParameter.Id is null || string.IsNullOrEmpty(SelectedRevitParameter.Name))
            return false;

         PropertyFilterEnum filterEnum = PropertyFilterEnum.All;
         foreach (var group in _model.Data)
         {
            if (!group.Value.ContainsKey(filterEnum))
               continue;

            var initialSelection = SelectedRevitParameter.Id.Equals(ElementId.InvalidElementId) ?
                group.Value[filterEnum].FirstOrDefault(x => x.Name == SelectedRevitParameter.Name) :
                group.Value[filterEnum].FirstOrDefault(x => x.Id == SelectedRevitParameter.Id);

            if (initialSelection is null)
               continue;

            initialSelection.IsChecked = true;
            SelectedCategory = group.Key;
            listBox_Parameters.ScrollIntoView(initialSelection);
            return true;
         }
         return false;
      }

      private void comboBox_Category_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         if (!_model.Data.TryGetValue(SelectedCategory, out var parameters) ||
            !parameters.TryGetValue(PropertyFilterEnum.All, out var allParameters) ||
            !parameters.TryGetValue(PropertyFilterEnum.Builtin, out var builtinParameters) ||
            !parameters.TryGetValue(PropertyFilterEnum.Extended, out var extendedParameters) ||
            !parameters.TryGetValue(PropertyFilterEnum.Instance, out var instanceParameters) ||
            !parameters.TryGetValue(PropertyFilterEnum.Type, out var typeParameters))
            return;

         ParametersFilter.Clear();
         ParametersFilter.Add(new RevitParameterFilter { Name = Properties.Resources.FilterAllProperties, Items = allParameters });
         ParametersFilter.Add(new RevitParameterFilter { Name = Properties.Resources.FilterBuiltinProperties, Items = builtinParameters });
         ParametersFilter.Add(new RevitParameterFilter { Name = Properties.Resources.FilterExtendedProperties, Items = extendedParameters });
         ParametersFilter.Add(new RevitParameterFilter { Name = Properties.Resources.FilterInstanceProperties, Items = instanceParameters });
         ParametersFilter.Add(new RevitParameterFilter { Name = Properties.Resources.FilterTypeProperties, Items = typeParameters });

         comboBox_PropertyFilter.SelectedIndex = 0;
         comboBox_PropertyFilter.Items.Refresh();
      }

      internal static IReadOnlyDictionary<ElementId, List<RevitParameterData>> GetCategoryParametersCacheSnapshot()
      {
         EnsureCategoryParametersCacheInitialized();
         return _categoryParametersCache;
      }

      private static void EnsureCategoryParametersCacheInitialized()
      {
         Document document = IFCCommandOverrideApplication.TheDocument;
         if (document is null)
            return;

         HashSet<long> processedCategoryIds = new();
         List<Category> categoriesToProcess = new();

         foreach (var categorySet in _ifcEntityToCategoriesMap.Values)
         {
            if (categorySet is null)
               continue;

            foreach (ElementId categoryId in categorySet)
            {
               if (categoryId is null || categoryId == ElementId.InvalidElementId)
                  continue;

               long categoryKey = categoryId?.Value ?? -1;
               if (categoryKey == -1 || processedCategoryIds.Contains(categoryKey))
                  continue;

               Category category = Category.GetCategory(document, categoryId);
               if (category is null)
                  continue;

               processedCategoryIds.Add(categoryKey);
               categoriesToProcess.Add(category);
            }
         }

         if (categoriesToProcess.Count == 0)
         {
            List<Category> exportableCategories = GetExportableCategories();
            if (exportableCategories is not null)
            {
               foreach (Category category in exportableCategories)
               {
                  if (category is null)
                     continue;

                  long categoryId = category.Id?.Value ?? -1;
                  if (categoryId == -1 || processedCategoryIds.Contains(categoryId))
                     continue;

                  processedCategoryIds.Add(categoryId);
                  categoriesToProcess.Add(category);
               }
            }
         }

         foreach (Category category in categoriesToProcess)
         {
            ElementId categoryId = category?.Id;
            if (category is null || categoryId is null || categoryId == ElementId.InvalidElementId || _categoryParametersCache.ContainsKey(categoryId))
               continue;

            List<RevitParameterData> parameters = GetSingleCategoryParameters(category);
            _categoryParametersCache[categoryId] = parameters ?? new List<RevitParameterData>();
         }
      }

      private void button_Ok_Click(object sender, RoutedEventArgs e)
      {
         bool foundSelected = false;
         PropertyFilterEnum filterEnum = PropertyFilterEnum.All;
         foreach (var groupedParameters in _model.Data.Values)
         {
            if (!groupedParameters.ContainsKey(filterEnum))
               continue;

            foreach (var parameter in groupedParameters[filterEnum])
            {
               if (parameter.IsChecked)
               {
                  SelectedRevitParameter = new RevitParameterInfo(parameter.Name, parameter.Id);
                  foundSelected = true;
                  break;
               }
            }
            if (foundSelected)
               break;
         }

         if (SelectedRevitParameter is null ||
            SelectedRevitParameter.Id is not null && !string.IsNullOrEmpty(SelectedRevitParameter.Name))
            DialogResult = true;

         Close();
      }

      private void button_Cancel_Click(object sender, RoutedEventArgs e)
      {
         Close();
      }

      private void radioButton_Parameter_Checked(object sender, RoutedEventArgs e)
      {
         RadioButton radioButton = sender as RadioButton;
         if (e is null || radioButton is null)
            return;

         ParameterListItem checkedListItem = radioButton.DataContext as ParameterListItem;
         if (checkedListItem is null)
            return;

         ElementId checkedItemId = checkedListItem.Id;
         string checkedItemName = checkedListItem.Name;
         bool hasValidId = checkedItemId != ElementId.InvalidElementId;

         foreach (var groupedParameters in _model.Data.Values)
         {
            if (!groupedParameters.ContainsKey(PropertyFilterEnum.All))
               continue;

            foreach (var parameter in groupedParameters[PropertyFilterEnum.All])
            {
               parameter.IsChecked =
                  (hasValidId && parameter.Id == checkedItemId ||
                  !hasValidId && parameter.Name == checkedItemName);
            }
         }
      }

      private void PropertySelectorWindow_Closing(object sender, CancelEventArgs e)
      {
         Properties.Settings.Default.LastSelectedCategory = SelectedCategory;
         Properties.Settings.Default.Save();
      }

      private void comboBox_PropertyFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         if (SelectedRevitParameter is null || (SelectedRevitParameter.Id is not null
            && !string.IsNullOrEmpty(SelectedRevitParameter.Name))
            || string.IsNullOrEmpty(SelectedProperty))
            return;

         RevitParameterFilter revitParameters = comboBox_PropertyFilter.SelectedItem as RevitParameterFilter;
         if (revitParameters is null)
            return;

         ParameterListItem parameterItem = revitParameters.Items?.FirstOrDefault(x => string.Compare(x.Name, SelectedProperty) == 0);
         if (parameterItem is null)
            return;

         parameterItem.IsChecked = true;
         listBox_Parameters.ScrollIntoView(parameterItem);
      }

      /// <summary>
      /// Initializes the cache that maps IFC entity types to Revit categories.
      public static void InitEntityToCategoriesCache(string categoryMappingTemplateName)
      {
         _categoryParametersCache.Clear();
         _ifcEntityToCategoriesMap.Clear();

         List<Category> categories = GetExportableCategories();
         if ((categories?.Count ?? 0) == 0)
            return;

         GetCategoryEntityAndPopulateCache(categories, categoryMappingTemplateName);
      }

      private static List<Category> GetExportableCategories()
      {
         List<Category> categories = new();

         Categories settingsCategories = IFCCommandOverrideApplication.TheDocument.Settings.Categories;
         if (settingsCategories is null)
            return categories;

         // Get top-level categories from the settings
         List<Category> topLevelCategories = settingsCategories.Cast<Category>().ToList();
         if (topLevelCategories is null)
            return categories;

         // Add subcategories to the list
         List<Category> allCategories = new();
         foreach (Category category in topLevelCategories)
         {
            if (category is null || category.Id == ElementId.InvalidElementId)
               continue;

            allCategories.Add(category);

            CategoryNameMapIterator it = category.SubCategories.ForwardIterator();
            while (it.MoveNext())
            {
               Category subCategory = it.Current as Category;

               if (subCategory is null || subCategory.Id == ElementId.InvalidElementId)
                  continue;

               allCategories.Add(subCategory);
            }
         }

         // Filter categories based on exportability to IFC
         return allCategories.Where(c =>
            c is not null &&
            c.IsValid &&
            c.IsVisibleInUI &&
            !c.IsTagCategory &&
            c.CategoryType != CategoryType.Invalid &&
            c.CategoryType != CategoryType.AnalyticalModel ||
            IsValidCategoryForParameterMapping(c))
            .ToList();
      }

      private static void GetCategoryEntityAndPopulateCache(List<Category> categories, string categoryMappingTemplateName)
      {
         if ((categories?.Count ?? 0) == 0)
            return;

         ExporterCacheManager.Clear(fullClear: true);
         ExporterCacheManager.ExportOptionsCache.CategoryMappingTemplateName = categoryMappingTemplateName;
         ExporterCacheManager.Document = IFCCommandOverrideApplication.TheDocument;

         foreach (Category category in categories)
         {
            if (category is null || category.Id == ElementId.InvalidElementId)
               continue;

            if (category.BuiltInCategory == BuiltInCategory.OST_GenericModel)
               _defaultCategoryId = category.Id;

            // Get export info for the category
            if (!ExporterUtil.GetCategoryInfoById(category.Id, null, out ExportIFCCategoryInfo exportInfo))
               ExporterUtil.GetCategoryInfoById(category, out exportInfo);

            ProcessSpecialCases(category, ref exportInfo);

            if (string.IsNullOrEmpty(exportInfo?.IFCEntityName ?? null))
               continue;

            // Add to the cache
            if (!_ifcEntityToCategoriesMap.TryGetValue(exportInfo.IFCEntityName, out HashSet<ElementId> categorySet))
            {
               categorySet = new HashSet<ElementId>();
               _ifcEntityToCategoriesMap.Add(exportInfo.IFCEntityName, categorySet);
            }
            categorySet.Add(category.Id);
         }
      }

      private static void ProcessSpecialCases(Category category, ref ExportIFCCategoryInfo exportInfo)
      {
         if (category?.Parent is null || exportInfo is null)
            return;

         string entityName = exportInfo.IFCEntityName;

         if (!sSpecificMappings.TryGetValue(category.Parent.BuiltInCategory, out string specificEntityName))
            return;

         if (string.IsNullOrEmpty(entityName) || entityName == "IfcBuildingElementProxy")
         {
            exportInfo.IFCEntityName = specificEntityName;
         }
      }

      private static bool IsValidCategoryForParameterMapping(Category category)
      {
         if (category is null)
            return false;

         if (category.BuiltInCategory == BuiltInCategory.OST_ProjectInformation)
            return true;

         return false;
      }
   }

   /// <summary>
   /// The class that keeps groups of All/Instance/Type Revit parameters for UI filter option
   /// </summary>
   public class RevitParameterFilter
   {
      public string Name { get; set; }
      public List<ParameterListItem> Items { get; set; }
   }

   /// <summary>
   /// The class keeps the Revit parameter criterias and is used 
   /// in Revit parameter to Ifc property mapping.
   /// </summary>
   public class ApplicableParameterInfo
   {
      public HashSet<IFCEntityType> EntityTypes { get; set; }
      public StorageType StorageType { get; set; }
      public HashSet<ForgeTypeId> DataTypes { get; set; }


      public bool IsApplicableParameter(RevitParameterData parameterData)
      {
         if (parameterData is null)
            return false;

         bool unsetDataType = parameterData.DataType is null || parameterData.DataType.Empty();
         bool unsetStorageType = parameterData.StorageType == StorageType.None;

         if (unsetDataType && unsetStorageType)
            return false;

         bool elementIdMatchesString = StorageType == StorageType.String && parameterData.StorageType == StorageType.ElementId;

         if (!unsetStorageType && parameterData.StorageType != StorageType && !elementIdMatchesString)
            return false;

         if (elementIdMatchesString)
            return true;

         bool anyDataType = (DataTypes?.Count ?? 0) == 0 || unsetDataType;
         if (anyDataType)
            return true;

         if (DataTypes.Contains(parameterData.DataType))
            return true;

         return false;
      }

   }

   public class ListBoxIndexConverter : IValueConverter
   {
      public object Convert(object value, Type TargetType, object parameter, CultureInfo culture)
      {
         ListBoxItem item = (ListBoxItem)value;
         ListBox listBox = ItemsControl.ItemsControlFromItemContainer(item) as ListBox;
         int index = listBox.ItemContainerGenerator.IndexFromContainer(item);
         return index.ToString();
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }
}
