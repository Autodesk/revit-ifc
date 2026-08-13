using Autodesk.Revit.DB;
using Autodesk.Windows;
using BIM.IFC.Export.UI.Properties;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using UIFramework;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// This class is used for extraction, holding mapping information and writing it to a mapping template.
   /// </summary>
   public class IFCPropertyMappingModel : INotifyPropertyChanged
   {
      /// <summary>
      /// Represents the types of property mapping.
      /// IfcToRevit - IFC property info is readonly and unique, Revit property info is arbitrary (e.g. IFCCommonPropertySets).
      /// RevitToIfc - Revit property info is readonly and unique, IFC propertyproperty info is arbitrary (e.g. RevitPropertySets).
      /// </summary>
      public enum MappingType
      {
         IfcToRevit,
         RevitToIfc
      }

      /// <summary>
      /// The IFC Common property set cache.
      /// </summary>
      private static Dictionary<IFCVersion, IList<IList<PropertySetDescription>>> IfcCommonPropertySetCache { get; set; } = new();

      /// <summary>
      /// The Base Quantities cache.
      /// </summary>
      private static Dictionary<IFCVersion, IList<IList<QuantityDescription>>> BaseQuantitiesCache { get; set; } = new();

      /// <summary>
      /// The built-in Revit parameters cache.
      /// </summary>
      private static SortedDictionary<string, List<(ElementId parameterId, (string parameterName, string dataTypeName))>> BuiltInParametersCache { get; set; }

      /// <summary>
      /// The all Revit parameters cache.
      /// </summary>
      private static SortedDictionary<string, List<(ElementId parameterId, (string parameterName, string dataTypeName))>> AllParametersCache { get; set; }

      /// <summary>
      /// The non built-in Revit parameters cache.
      /// </summary>
      private static SortedDictionary<string, List<(string, string)>> NonBuiltInParametersCache { get; set; }

      /// <summary>
      /// The built-in parameter tooltip cache.
      /// </summary>
      private static Dictionary<ElementId, string> ParameterTooltipsCache { get; set; }

      /// <summary>
      /// The hardcoded Property Setup to Mapping type matching
      /// </summary>
      static readonly Dictionary<PropertySetupType, MappingType> SetupMappingTypes = new()
      {
         { PropertySetupType.IfcCommonPropertySets, MappingType.IfcToRevit },
         { PropertySetupType.RevitElementParameters, MappingType.RevitToIfc },
         { PropertySetupType.IfcBaseQuantities, MappingType.IfcToRevit },
         { PropertySetupType.RevitMaterialParameters, MappingType.RevitToIfc },
         { PropertySetupType.RevitSchedules, MappingType.RevitToIfc },
         { PropertySetupType.UserDefinedPropertySets, MappingType.IfcToRevit }
      };

      public event PropertyChangedEventHandler PropertyChanged;
      protected void OnPropertyChanged([CallerMemberName] string name = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }

      /// <summary>
      /// Get mapping type of a property setup.
      /// </summary>
      public static MappingType GetMappingType(PropertySetupType propertySetup)
      {
         if (SetupMappingTypes.ContainsKey(propertySetup))
            return SetupMappingTypes[propertySetup];

         return MappingType.IfcToRevit;
      }

      private static IList<IList<PropertySetDescription>> GetOrCreateCachedIfcCommonPropertySets(IFCVersion ifcVersion)
      {
         if (IfcCommonPropertySetCache.TryGetValue(ifcVersion, out IList<IList<PropertySetDescription>> allPropertySets))
            return allPropertySets;

         allPropertySets = new List<IList<PropertySetDescription>>();
         ExporterInitializer.PopulateIFCCommonPropertySets(ifcVersion, allPropertySets);
         IfcCommonPropertySetCache[ifcVersion] = allPropertySets;

         return allPropertySets;
      }

      public static PropertySetDescription GetCachedIfcCommonPSetDescription(string psetName, IFCVersion ifcVersion)
      {
         IList<IList<PropertySetDescription>> allPropertySets = GetOrCreateCachedIfcCommonPropertySets(ifcVersion);

         if (allPropertySets == null)
            return null;

         foreach (var psetList in allPropertySets)
         {
            foreach (var psetDescription in psetList)
            {
               if (psetDescription.Name == psetName)
                  return psetDescription;
            }
         }

         return null;
      }

      private static IList<IList<QuantityDescription>> GetOrCreateCachedBaseQuantities(IFCVersion ifcVersion)
      {
         if (BaseQuantitiesCache.TryGetValue(ifcVersion, out IList<IList<QuantityDescription>> allQuantitySets))
            return allQuantitySets;

         allQuantitySets = new List<IList<QuantityDescription>>();
         ExporterInitializer.PopulateBaseQuantitiesPropertySets(ifcVersion, allQuantitySets);
         BaseQuantitiesCache[ifcVersion] = allQuantitySets;

         return allQuantitySets;
      }

      public static QuantityDescription GetCachedBaseQuantityDescription(string psetName, IFCVersion ifcVersion)
      {
         IList<IList<QuantityDescription>> allQuantitySets = GetOrCreateCachedBaseQuantities(ifcVersion);

         if (allQuantitySets == null)
            return null;

         foreach (var quantitiesList in allQuantitySets)
         {
            foreach (var quantitiyDescription in quantitiesList)
            {
               if (quantitiyDescription.Name == psetName)
                  return quantitiyDescription;
            }
         }

         return null;
      }

      public SetupMappingInfo InitializeIFCCommonPropertySets(IFCVersion ifcVersion)
      {
         PropertySetupType propertySetup = PropertySetupType.IfcCommonPropertySets;

         SetupMappingInfo setupInfo = new()
         {
            SetupName = GetPropertySetupName(propertySetup),
            PropertySetup = propertySetup,
            IfcVersion = ifcVersion
         };

         IList<IList<PropertySetDescription>> allPropertySets = GetOrCreateCachedIfcCommonPropertySets(ifcVersion);
         if ((allPropertySets?.Count ?? 0) == 0 || allPropertySets[0] == null)
            return setupInfo;

         foreach (var setDescription in allPropertySets[0])
         {
            if ((setDescription?.Entries?.Count ?? 0) == 0)
               continue;

            string setName = setDescription.Name;
            List<PropertyMappingInfo> propertyInfos = new();
            foreach (var entry in setDescription.Entries)
            {
               propertyInfos.Add(new PropertyMappingInfo(entry.PropertyName, string.Empty, ElementId.InvalidElementId, propertySetup,
                  entry.PropertyType.ToString()));
            }

            setupInfo.PSetMappingInfos.Add(new PSetMappingInfo(setName, propertySetup, propertyInfos, setupInfo));
         }
         return setupInfo;
      }

      public SetupMappingInfo InitializeRevitPropertySetsList()
      {
         PropertySetupType propertySetup = PropertySetupType.RevitElementParameters;

         SetupMappingInfo setupInfo = new()
         {
            SetupName = GetPropertySetupName(propertySetup),
            PropertySetup = propertySetup,
         };

         SortedDictionary<string, List<(ElementId parameterId, (string parameterName, string dataType))>> allParameters = GetGroupedRevitParameters();

         foreach (var group in allParameters)
         {
            List<PropertyMappingInfo> propertyInfos = group.Value
               .Select(param => new PropertyMappingInfo(string.Empty, param.Item2.parameterName, param.parameterId, propertySetup,
               param.Item2.dataType))
               .ToList();
            setupInfo.PSetMappingInfos.Add(new PSetMappingInfo(group.Key, propertySetup, propertyInfos, setupInfo));
         }

         return setupInfo;
      }

      public SetupMappingInfo InitializeBaseQuantities(IFCVersion ifcVersion)
      {
         PropertySetupType propertySetup = PropertySetupType.IfcBaseQuantities;

         SetupMappingInfo setupInfo = new()
         {
            SetupName = GetPropertySetupName(propertySetup),
            PropertySetup = propertySetup,
            IfcVersion = ifcVersion
         };

         IList<IList<QuantityDescription>> allQuantitySets = GetOrCreateCachedBaseQuantities(ifcVersion);
         if ((allQuantitySets?.Count ?? 0) == 0 || allQuantitySets[0] == null)
            return setupInfo;

         foreach (var setDescription in allQuantitySets[0])
         {
            if ((setDescription?.Entries?.Count ?? 0) == 0)
               continue;

            string setName = setDescription.Name;
            List<PropertyMappingInfo> propertyInfos = new();
            foreach (var entry in setDescription.Entries)
            {
               propertyInfos.Add(new PropertyMappingInfo(entry.PropertyName, string.Empty, ElementId.InvalidElementId, propertySetup,
                  entry.QuantityType.ToString()));
            }
            setupInfo.PSetMappingInfos.Add(new PSetMappingInfo(setName, propertySetup, propertyInfos, setupInfo));
         }

         return setupInfo;
      }

      public SetupMappingInfo InitializeMaterialPropertySets()
      {
         PropertySetupType propertySetup = PropertySetupType.RevitMaterialParameters;

         SetupMappingInfo setupInfo = new()
         {
            SetupName = GetPropertySetupName(propertySetup),
            PropertySetup = propertySetup,
         };

         Dictionary<string, List<(BuiltInParameter parameterId, string parameterName, string dataType)>> allParameters =
            MaterialPropertiesUtil.GetGroupedMaterialParameters(IFCCommandOverrideApplication.TheDocument);

         if (allParameters == null)
            return setupInfo;

         foreach (var group in allParameters)
         {
            Dictionary<BuiltInParameter, (string parameterName, string dataType)> sortedParameters = new();
            foreach ((BuiltInParameter parameterId, string parameterName, string dataType) parameterInfo in group.Value)
            {
               if (sortedParameters.TryGetValue(parameterInfo.parameterId, out var parameterValue))
               {
                  // If there are duplicate built-in parameters, keep the one with data type.
                  if (string.IsNullOrEmpty(parameterValue.dataType) && !string.IsNullOrEmpty(parameterInfo.dataType))
                     sortedParameters[parameterInfo.parameterId] = (parameterInfo.parameterName, parameterInfo.dataType);
               }
               else
               {
                  sortedParameters[parameterInfo.parameterId] = (parameterInfo.parameterName, parameterInfo.dataType);
               }
            }

            List<PropertyMappingInfo> propertyInfos = sortedParameters
               .Select(param => new PropertyMappingInfo(string.Empty, param.Value.parameterName, new ElementId(param.Key), propertySetup,
               param.Value.dataType))
               .ToList();
            setupInfo.PSetMappingInfos.Add(new PSetMappingInfo(group.Key, propertySetup, propertyInfos, setupInfo,
               GetLocalizedMaterialPropertySetName(group.Key)));
         }

         return setupInfo;
      }

      /// <summary>
      /// Returns the localized display name for a material property set.
      /// </summary>
      private static string GetLocalizedMaterialPropertySetName(string materialPropertyType)
      {
         return materialPropertyType switch
         {
            nameof(MaterialPropertiesUtil.MaterialPropertyType.Identity) => Resources.IdentityMaterialParams,
            nameof(MaterialPropertiesUtil.MaterialPropertyType.Structural) => Resources.StructuralMaterialParams,
            nameof(MaterialPropertiesUtil.MaterialPropertyType.Thermal) => Resources.ThermalMaterialParams,
            _ => materialPropertyType
         };
      }

      public SetupMappingInfo InitializeSchedules()
      {
         Document document = IFCCommandOverrideApplication.TheDocument;
         PropertySetupType propertySetup = PropertySetupType.RevitSchedules;
         SetupMappingInfo setupInfo = new()
         {
            SetupName = GetPropertySetupName(propertySetup),
            PropertySetup = propertySetup,
         };

         List<(string, ScheduleDefinition)> collectedSchedules = ExporterUtil.CollectSchedules(IFCCommandOverrideApplication.TheDocument);
         if ((collectedSchedules?.Count ?? 0) == 0)
            return setupInfo;

         foreach ((string scheduleName, ScheduleDefinition scheduleDefinition) in collectedSchedules)
         {
            if (string.IsNullOrWhiteSpace(scheduleName) || scheduleDefinition == null)
               continue;

            int fieldCount = scheduleDefinition.GetFieldCount();
            if (fieldCount == 0)
               continue;

            List<PropertyMappingInfo> propertyInfos = [];
            HashSet<ElementId> processedScheduleIds = new();

            for (int ii = 0; ii < fieldCount; ii++)
            {
               ScheduleField field = scheduleDefinition.GetField(ii);
               if (!ExporterInitializer.IsSupportedScheduleField(field))
                  continue;

               string propertyName = field.ColumnHeading;
               if (string.IsNullOrEmpty(propertyName))
                  continue;

               string typeString = string.Empty;
               ElementId parameterId = field.ParameterId;

               switch (field.FieldType)
               {
                  case ScheduleFieldType.CombinedParameter:
                     {
                        typeString = "Text";
                        break;
                     }
                  default:
                     {
                        if (parameterId == ElementId.InvalidElementId)
                           continue;

                        if (processedScheduleIds.Contains(parameterId))
                           continue;

                        ForgeTypeId proxyDataTypeId = null;
                        InternalDefinition paramDefinition = null;
                        if (ParameterUtils.IsBuiltInParameter(parameterId))
                        {
                           BuiltInParameter builtInParameterId = (BuiltInParameter)parameterId.Value;
                           if (PropertyUtil.ProxyParameter.IsProxyParameter(builtInParameterId))
                           {
                              proxyDataTypeId = new ForgeTypeId("autodesk.spec:spec.string-2.0.0");
                           }
                           else
                           {
                              ForgeTypeId paramTypeId = ParameterUtils.GetParameterTypeId(builtInParameterId);
                              if (paramTypeId?.Empty() ?? true)
                                 continue;

                              paramDefinition = ParameterUtils.GetDefinition(paramTypeId);
                           }
                        }
                        else
                        {
                           Element element = document.GetElement(new ElementId(parameterId.Value));
                           if (element is not ParameterElement paramElement)
                              continue;

                           paramDefinition = paramElement?.GetDefinition();
                        }

                        ForgeTypeId dataTypeId = proxyDataTypeId ?? paramDefinition?.GetDataType();
                        if ((dataTypeId?.Empty() ?? true) == false)
                           typeString = LabelUtils.GetLabelForSpec(dataTypeId);

                        processedScheduleIds.Add(parameterId);
                        break;
                     }
               }

               propertyInfos.Add(new PropertyMappingInfo(string.Empty, propertyName, parameterId,
                  propertySetup, typeString));
            }

            if (propertyInfos.Count > 0)
               setupInfo.PSetMappingInfos.Add(new PSetMappingInfo(scheduleName, propertySetup, propertyInfos, setupInfo));
         }
         
         return setupInfo;
      }

      public SetupMappingInfo InitializeUserDefinedPropertySets()
      {
         PropertySetupType propertySetup = PropertySetupType.UserDefinedPropertySets;

         SetupMappingInfo setupInfo = new()
         {
            SetupName = GetPropertySetupName(propertySetup),
            PropertySetup = propertySetup,
         };

         Document document = IFCCommandOverrideApplication.TheDocument;

         IList<string> propertySetNames = IFCUserDefinedPropertySet.ListPropertySetNames(document);
         if ((propertySetNames?.Count ?? 0) == 0)
            return setupInfo;

         foreach (var psetName in propertySetNames)
         {
            IFCUserDefinedPropertySet userDefinedPSet = IFCUserDefinedPropertySet.FindPropertySetByName(document, psetName);
            if (userDefinedPSet == null)
               continue;

            IList<IFCUserDefinedProperty> properties = userDefinedPSet.GetProperties();
            if ((properties?.Count ?? 0) == 0)
               continue;

            List<PropertyMappingInfo> propertyInfos = new();

            foreach (var property in properties)
            {
               if (property == null)
                  continue;

               string propertyName = property.IFCPropertyName;
               if (string.IsNullOrEmpty(propertyName))
                  continue;

               string revitPropertyName = property.RevitPropertyName ?? string.Empty;
               ElementId revitPropertyId = property.RevitPropertyId ?? ElementId.InvalidElementId;

               propertyInfos.Add(new PropertyMappingInfo(propertyName, revitPropertyName, revitPropertyId, propertySetup, property.DataType));
            }

            setupInfo.PSetMappingInfos.Add(new PSetMappingInfo(psetName, propertySetup, propertyInfos, setupInfo));
         }

         return setupInfo;
      }


      public static SortedDictionary<string, List<(ElementId parameterId, (string parameterName, string dataTypeName))>> GetGroupedRevitParameters()
      {
         if (AllParametersCache != null)
            return AllParametersCache;

         AllParametersCache = GetBuiltInParameters();
         SortedDictionary<string, List<(string parameterName, string dataTypeName)>> nonBuiltInParameters = GetNonBuiltInParameters();

         foreach (var group in nonBuiltInParameters)
         {
            string groupName = group.Key;
            var parametersToAdd = group.Value.Select(x => (ElementId.InvalidElementId, x)).ToList();
            if (AllParametersCache.ContainsKey(groupName))
            {
               AllParametersCache[groupName] = AllParametersCache[groupName].Union(parametersToAdd).ToList();
            }
            else
            {
               AllParametersCache.Add(groupName, parametersToAdd);
            }
         }

         // Sort parameter list
         foreach (var group in AllParametersCache)
         {
            group.Value.Sort((a, b) =>
            {
               int namesCmp = string.Compare(a.Item2.parameterName, b.Item2.parameterName, false);
               return namesCmp != 0 ? namesCmp : a.parameterId.Value.CompareTo(b.parameterId.Value);
            });
         }

         return AllParametersCache;
      }

      public static SortedDictionary<string, List<(ElementId parameterId, (string parameterName, string dataTypeName))>> GetBuiltInParameters()
      {
         if (BuiltInParametersCache != null)
            return BuiltInParametersCache;

         BuiltInParametersCache = new();

         foreach (ForgeTypeId paramTypeId in ParameterUtils.GetAllBuiltInParameters())
         {
            if (paramTypeId?.Empty() ?? true)
               continue;

            string paramName = LabelUtils.GetLabelForBuiltInParameter(paramTypeId);
            if (string.IsNullOrEmpty(paramName))
               continue;

            ElementId paramId = new(ParameterUtils.GetBuiltInParameter(paramTypeId));
            if (paramId.Equals(ElementId.InvalidElementId))
               continue;

            string dataTypeName = null;
            InternalDefinition paramDefinition = null;
            if (PropertyUtil.ProxyParameter.IsProxyParameter((BuiltInParameter)paramId.Value))
            {
               dataTypeName = LabelUtils.GetLabelForSpec(new ForgeTypeId("autodesk.spec:spec.string-2.0.0"));
            }
            else
            {
               paramDefinition = ParameterUtils.GetDefinition(paramTypeId);
               if (paramDefinition == null)
                  continue;   
               ForgeTypeId dataTypeId = paramDefinition.GetDataType();
               if (!dataTypeId?.Empty() ?? false)
                  dataTypeName = LabelUtils.GetLabelForSpec(dataTypeId);
            }
               
            ForgeTypeId groupTypeId = ParameterUtils.GetBuiltInParameterGroupTypeId(paramTypeId);
            if (groupTypeId == null)
               continue;

            string groupName = LabelUtils.GetLabelForGroup(groupTypeId);
            if (string.IsNullOrEmpty(groupName))
               continue;

            BuiltInParametersCache.TryGetValue(groupName, out var parameterList);
            if (parameterList == null)
            {
               parameterList = [];
               BuiltInParametersCache.Add(groupName, parameterList);
            }
            parameterList.Add((paramId, (paramName, dataTypeName)));
         }

         return BuiltInParametersCache;
      }

      public static SortedDictionary<string, List<(string, string)>> GetNonBuiltInParameters()
      {
         if (NonBuiltInParametersCache != null)
            return NonBuiltInParametersCache;

         NonBuiltInParametersCache = new();

         FilteredElementCollector collectorParam = new FilteredElementCollector(IFCCommandOverrideApplication.TheDocument);
         FilteredElementCollector parameterFilter = collectorParam.OfClass(typeof(ParameterElement));

         FilteredElementCollector collectorGlobalParam = new FilteredElementCollector(IFCCommandOverrideApplication.TheDocument);
         FilteredElementCollector globalParameterFilter = collectorGlobalParam.OfClass(typeof(GlobalParameter));
         if ((globalParameterFilter?.ToElementIds()?.Count ?? 0) > 0)
            parameterFilter = parameterFilter.Excluding(globalParameterFilter.ToElementIds());

         foreach (var filteredElement in parameterFilter)
         {
            ParameterElement parameterElement = filteredElement as ParameterElement;
            if (parameterElement == null)
               continue;

            InternalDefinition paramDefinition = parameterElement.GetDefinition();
            if (paramDefinition == null)
               continue;

            string dataTypeName = string.Empty;
            ForgeTypeId dataTypeId = paramDefinition.GetDataType();
            if ((dataTypeId?.Empty() ?? true) == false)
               dataTypeName = LabelUtils.GetLabelForSpec(dataTypeId);

            ForgeTypeId groupTypeId = paramDefinition?.GetGroupTypeId();
            if (groupTypeId?.Empty() ?? true)
               continue;

            string groupName = LabelUtils.GetLabelForGroup(groupTypeId);
            if (string.IsNullOrEmpty(groupName))
               continue;

            string paramName = paramDefinition.Name;
            if (string.IsNullOrEmpty(paramName))
               continue;

            NonBuiltInParametersCache.TryGetValue(groupName, out var parameterList);
            if (parameterList == null)
            {
               parameterList = new();
               NonBuiltInParametersCache.Add(groupName, parameterList);
            }
            parameterList.Add((paramName, dataTypeName));
         }

         return NonBuiltInParametersCache;
      }

      /// <summary>
      /// Clear the cache of data that can be changed between dialog openings.
      /// </summary>
      public void ClearCache()
      {
         NonBuiltInParametersCache = null;
         AllParametersCache = null;
         ParameterTooltipsCache = null;
      }


      /// <summary>
      /// Gets tooltip text for a parameter by its ElementId.
      /// </summary>
      public static string GetParameterTooltip(ElementId paramId, string parameterName)
      {
         if (paramId == ElementId.InvalidElementId)
            return parameterName;

         if (ParameterTooltipsCache == null)
            ParameterTooltipsCache = new Dictionary<ElementId, string>();

         if (ParameterTooltipsCache.TryGetValue(paramId, out string cachedTooltip))
            return cachedTooltip;

         string tooltipText = GetTooltipFromParameterId(paramId.Value);
         if (string.IsNullOrEmpty(tooltipText))
            tooltipText = parameterName;

         ParameterTooltipsCache[paramId] = tooltipText;

         return tooltipText;
      }


      public static string ExtractTextFromTooltip(RibbonToolTip tooltip)
      {
         if (tooltip?.Content == null)
            return string.Empty;

         if (tooltip.Content is TextBlock directTextBlock)
         {
            return directTextBlock.Text;
         }

         if (tooltip.Content is StackPanel stackPanel)
         {
            var textBlocks = FindTextBlocks(stackPanel);
            return string.Join(" ", textBlocks.Select(tb => tb.Text).Where(text => !string.IsNullOrEmpty(text)));
         }

         return string.Empty;
      }

      /// <summary>
      /// Helper method to recursively find TextBlocks in a DependencyObject.
      /// </summary>
      private static List<TextBlock> FindTextBlocks(DependencyObject parent)
      {
         List<TextBlock> textBlocks = new();

         for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
         {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is TextBlock textBlock)
            {
               textBlocks.Add(textBlock);
            }
         }

         return textBlocks;
      }

      /// <summary>
      /// Gets tooltip text from a parameter id using Revit's built-in tooltip system.
      /// </summary>
      public static string GetTooltipFromParameterId(long paramId)
      {
         if (!Enum.IsDefined(typeof(BuiltInParameter), paramId))
            return string.Empty;

         string builtInName = Enum.GetName(typeof(BuiltInParameter), paramId) ?? string.Empty;
         RibbonToolTip tooltip = RvtTooltip.LoadToolTip(builtInName);
         return ExtractTextFromTooltip(tooltip);
      }

      private static string GetPropertySetupName(PropertySetupType propertySetup)
      {
         switch (propertySetup)
         {
            case PropertySetupType.IfcCommonPropertySets:
               return Resources.IFCCommonPropertySets;
            case PropertySetupType.RevitElementParameters:
               return Resources.RevitPropertySets;
            case PropertySetupType.IfcBaseQuantities:
               return Resources.BaseQuantities;
            case PropertySetupType.RevitMaterialParameters:
               return Resources.MaterialPropertySets;
            case PropertySetupType.RevitSchedules:
               return Resources.Schedules;
            case PropertySetupType.UserDefinedPropertySets:
               return Resources.UserDefinedPropertySets;
            default:
               return string.Empty;
         }
      }
   }

   /// <summary>
   /// Property Setup mapping information
   /// </summary>
   public class SetupMappingInfo : INotifyPropertyChanged
   {
      /// <summary>
      /// The localized name of the property setup.
      /// </summary>
      public string SetupName { get; set; } = String.Empty;

      /// <summary>
      /// The property setup type.
      /// </summary>
      public PropertySetupType PropertySetup { get; set; } = new();
      /// <summary>
      /// The IFC version of the property setup.
      /// </summary>
      public IFCVersion IfcVersion { get; set; } = IFCVersion.Default;

      /// <summary>
      /// List of property set mapping information.
      /// </summary>
      public List<PSetMappingInfo> PSetMappingInfos { get; set; } = new();

      /// <summary>
      /// Flag to determine if a property setup is exported or not.
      /// </summary>
      private bool? m_ExportSetup = true;
      public bool? ExportSetup
      {
         get { return m_ExportSetup; }
         set
         {
            if (m_ExportSetup != value)
            {
               m_ExportSetup = value;
               OnPropertyChanged();
               if (value != null && !ParentUpdateInProgress)
               {
                  UpdateChildren(value.Value);
               }
            }
         }
      }

      // Flags to avoid recursive calls when updating children and parent checkboxes
      public static bool ChildrenUpdateInProgress { get; private set; } = false;
      public static bool ParentUpdateInProgress { get; private set; } = false;

      private void UpdateChildren(bool value)
      {
         ChildrenUpdateInProgress = true;
         foreach (var child in PSetMappingInfos)
         {
            child.ExportFlag = value;
         }
         ChildrenUpdateInProgress = false;
      }

      public void UpdateParent()
      {
         if ((PSetMappingInfos?.Count ?? 0) == 0)
            return;

         ParentUpdateInProgress = true;
         bool? newParentState = PSetMappingInfos[0].ExportFlag;
         foreach (var psetInfo in PSetMappingInfos)
         {
            if (newParentState != psetInfo.ExportFlag)
            {
               newParentState = null;
               break;
            }
         }
         ExportSetup = newParentState;
         ParentUpdateInProgress = false;
      }

      public ICollectionView PropertySetCollection => CollectionViewSource.GetDefaultView(PSetMappingInfos);

      public event PropertyChangedEventHandler PropertyChanged;
      protected void OnPropertyChanged([CallerMemberName] string name = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }
   }

   /// <summary>
   /// Property Set mapping information
   /// </summary>
   public class PSetMappingInfo : INotifyPropertyChanged
   {
      /// <summary>
      /// The name of the property set (used as internal identifier for template matching).
      /// </summary>
      public string Name { get; set; }

      /// <summary>
      /// The localized display name of the property set (used for UI display).
      /// Falls back to Name if not explicitly set.
      /// </summary>
      public string DisplayName { get; set; }

      /// <summary>
      /// Flag to determine if a property set is exported or not.
      /// </summary>
      private bool m_ExportFlag = true;
      public bool ExportFlag
      {
         get { return m_ExportFlag; }
         set
         {
            if (m_ExportFlag != value)
            {
               m_ExportFlag = value;
               OnPropertyChanged();
               if (!SetupMappingInfo.ChildrenUpdateInProgress)
                  ParentSetup?.UpdateParent();
            }
         }
      }

      /// <summary>
      /// List of property mapping information.
      /// </summary>
      public List<PropertyMappingInfo> PropertyInfos { get; set; }

      /// <summary>
      /// The mapping type of the property set.
      /// </summary>
      public IFCPropertyMappingModel.MappingType Type { get; set; }

      /// <summary>
      /// The parent property setup mapping information.
      /// </summary>
      public SetupMappingInfo ParentSetup { get; set; }

      public string AutomationId { get; set; }

      public PSetMappingInfo(string name, PropertySetupType propertySetup, List<PropertyMappingInfo> propertyInfos, SetupMappingInfo parentSetup,
         string displayName = null)
      {
         Name = name;
         DisplayName = displayName ?? name;
         Type = IFCPropertyMappingModel.GetMappingType(propertySetup);
         PropertyInfos = propertyInfos;
         ParentSetup = parentSetup;
         AutomationId = "checkBox_PSet_" + (int)propertySetup + @"\" + name;
      }

      public bool TryGetProperty(IFCPropertyMappingInfo templatePropertyInfo, out PropertyMappingInfo modelPropertyInfo)
      {
         return TryGetProperty(templatePropertyInfo.IFCPropertyName, templatePropertyInfo.RevitPropertyId, templatePropertyInfo.RevitPropertyName, out modelPropertyInfo);
      }

      public bool TryGetProperty(string ifcPropertyName, ElementId revitPropertyId, string revitPropertyName, out PropertyMappingInfo modelPropertyInfo)
      {
         modelPropertyInfo = null;
         if ((PropertyInfos?.Count ?? 0) == 0)
            return false;

         Func<PropertyMappingInfo, bool> keyComparator = PropertyMappingInfo.GetPropertyMappingKeyComparator(Type, ifcPropertyName, revitPropertyId, revitPropertyName);
         modelPropertyInfo = PropertyInfos.FirstOrDefault(keyComparator);
         return modelPropertyInfo != null;
      }
      public void ResetToDefault()
      {
         ExportFlag = true;

         if ((PropertyInfos?.Count ?? 0) == 0)
            return;

         foreach (var mappingInfo in PropertyInfos)
            mappingInfo.ResetToDefault();
      }

      // Converts IfcBeam.BaseQuantities to Qto_BeamBaseQuantities
      public static string ConvertQuantitySetNameFrom2x3(string quantitySet2x3Name)
      {
         string pattern = @"Ifc(\w+)\.BaseQuantities";
         string replacement = @"Qto_$1BaseQuantities";

         return Regex.Replace(quantitySet2x3Name, pattern, replacement);
      }

      // Converts Qto_BeamBaseQuantities to IfcBeam.BaseQuantities
      public static string ConvertQuantitySetNameTo2x3(string quantitySetName)
      {
         string pattern = @"Qto_(\w+)BaseQuantities";
         string replacement = @"Ifc$1.BaseQuantities";

         return Regex.Replace(quantitySetName, pattern, replacement);
      }

      public event PropertyChangedEventHandler PropertyChanged;

      protected void OnPropertyChanged([CallerMemberName] string name = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }
   }


   /// <summary>
   /// Property mapping information
   /// </summary>
   public class PropertyMappingInfo : INotifyPropertyChanged
   {
      /// <summary>
      /// The default IFC property name for the mapping.
      /// </summary>
      private readonly string _defaultIfcPropertyName;

      /// <summary>
      /// Flag to determine if a PropertyMappingInfo is exported or not.
      /// </summary>
      private bool m_ExportFlag = true;
      public bool ExportFlag
      {
         get { return m_ExportFlag; }
         set
         {
            m_ExportFlag = value;
            OnPropertyChanged();
         }
      }

      /// <summary>
      /// The IFC property name.
      /// </summary>
      private string m_IFCPropertyName = null;
      public string IFCPropertyName
      {
         get { return m_IFCPropertyName; }
         set
         {
            m_IFCPropertyName = value;
            OnPropertyChanged();
         }
      }

      /// <summary>
      /// The Revit property name.
      /// </summary>
      private string m_RevitPropertyName = null;
      public string RevitPropertyName
      {
         get { return m_RevitPropertyName; }
         set
         {
            m_RevitPropertyName = value;
            OnPropertyChanged();
         }
      }

      /// <summary>
      /// The Revit property id.
      /// </summary>
      private ElementId m_RevitPropertyId = ElementId.InvalidElementId;
      public ElementId RevitPropertyId
      {
         get { return m_RevitPropertyId; }
         set
         {
            m_RevitPropertyId = value;
            OnPropertyChanged();
         }
      }

      /// <summary>
      /// The property data type.
      /// </summary>
      public string PropertyDataType { get; set; } = string.Empty;

      /// <summary>
      /// The property mapping type.
      /// </summary>
      public IFCPropertyMappingModel.MappingType Type { get; set; }

      public PropertyMappingInfo(string ifcPropertyName, string revitPropertyName, ElementId revitPropertyId, PropertySetupType propertySetup, string propertyDataType)
      {
         string cleanedIfcPropertyName = ifcPropertyName ?? string.Empty;
         string cleanedRevitPropertyName = revitPropertyName ?? string.Empty;

         Type = IFCPropertyMappingModel.GetMappingType(propertySetup);

         RevitPropertyName = cleanedRevitPropertyName;
         RevitPropertyId = revitPropertyId;
         PropertyDataType = propertyDataType ?? string.Empty;

         if (Type == IFCPropertyMappingModel.MappingType.RevitToIfc)
         {
            _defaultIfcPropertyName = cleanedRevitPropertyName;
            IFCPropertyName = string.IsNullOrEmpty(cleanedIfcPropertyName) ? _defaultIfcPropertyName : cleanedIfcPropertyName;
         }
         else
         {
            _defaultIfcPropertyName = cleanedIfcPropertyName;
            IFCPropertyName = cleanedIfcPropertyName;
         }
      }

      public void Assign(IFCPropertyMappingInfo templatePropertyInfo)
      {
         IFCPropertyName = templatePropertyInfo.IFCPropertyName;
         RevitPropertyName = templatePropertyInfo.RevitPropertyName;
         RevitPropertyId = templatePropertyInfo.RevitPropertyId;
         ExportFlag = templatePropertyInfo.ExportFlag;
      }

      public static Func<PropertyMappingInfo, bool> GetPropertyMappingKeyComparator(IFCPropertyMappingModel.MappingType mappingType, string ifcPropertyName, ElementId revitPropertyId, string revitPropertyName)
      {
         return (mappingType == IFCPropertyMappingModel.MappingType.IfcToRevit) ?
            (x => x.IFCPropertyName == ifcPropertyName) :
            (x => x.RevitPropertyId == revitPropertyId && x.RevitPropertyName == revitPropertyName);
      }

      /// <summary>
      /// Returns true if mapping info is not modified.
      /// </summary>
      public bool IsDefault()
      {
         if (!ExportFlag)
            return false;

         if (Type == IFCPropertyMappingModel.MappingType.IfcToRevit)
         {
            if (RevitPropertyId != ElementId.InvalidElementId ||
               !string.IsNullOrEmpty(RevitPropertyName))
               return false;
         }
         else
         {
            if (!string.Equals(IFCPropertyName ?? string.Empty, _defaultIfcPropertyName ?? string.Empty, StringComparison.Ordinal))
               return false;
         }
         return true;
      }

      /// <summary>
      /// Resets current mapping info to default values.
      /// </summary>
      public void ResetToDefault()
      {
         if (!ExportFlag)
            ExportFlag = true;

         if (Type == IFCPropertyMappingModel.MappingType.IfcToRevit)
         {
            RevitPropertyName = string.Empty;
            RevitPropertyId = ElementId.InvalidElementId;
         }
         else
            IFCPropertyName = _defaultIfcPropertyName ?? string.Empty;
      }

      public event PropertyChangedEventHandler PropertyChanged;

      protected void OnPropertyChanged([CallerMemberName] string name = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }
   }
}
