using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Exporter.PropertySet;
using System.Linq;
using static BIM.IFC.Export.UI.IFCMaterialPropertyUtil;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// This class is used for extraction, holding mapping information and writing it to a mapping template.
   /// </summary>
   public class IFCPropertyMappingModel
   {
      /// <summary>
      /// Represents the types of property mapping.
      /// IfcToRevit - IFC property info is readonly and unique, Revit property info is arbitrary (e.g. IFCCommonPropertySets).
      /// RevitToIfc - Revit property info is readonly and unique, Revit property info is arbitrary (e.g. IFCCommonPropertySets).
      /// </summary>
      public enum MappingType
      {
         IfcToRevit,
         RevitToIfc
      }

      /// <summary>
      /// The model data. 
      /// </summary>
      public Dictionary<IFCPropertySetups.PropertySetup, Dictionary<string, PSetMappingInfo>> SetupInfos { get; set; } = new();

      /// <summary>
      /// The built-in Revit parameters cache.
      /// </summary>
      private static Dictionary<KeyValuePair<ForgeTypeId, string>, ForgeTypeId> AllBuiltInParamertersCache { get; set; }

      /// <summary>
      /// The hardcoded Property Setup to Mapping type matching
      /// </summary>
      static readonly Dictionary<IFCPropertySetups.PropertySetup, MappingType> SetupMappingTypes = new()
      {
         { IFCPropertySetups.PropertySetup.IFCCommonPropertySets, MappingType.IfcToRevit },
         { IFCPropertySetups.PropertySetup.RevitPropertySets, MappingType.RevitToIfc },
         { IFCPropertySetups.PropertySetup.BaseQuantities, MappingType.IfcToRevit },
         { IFCPropertySetups.PropertySetup.MaterialPropertySets, MappingType.RevitToIfc },
         { IFCPropertySetups.PropertySetup.Schedules, MappingType.RevitToIfc }
      };

      /// <summary>
      /// Get mapping type of a property setup.
      /// </summary>
      public static MappingType GetMappingType(IFCPropertySetups.PropertySetup propertySetup)
      {
         if (!SetupMappingTypes.ContainsKey(propertySetup))
         {
            throw new ArgumentException("Unexpected property setup type.", "propertySetup");
         }
         return SetupMappingTypes[propertySetup];
      }

      /// <summary>
      /// Initialize the property setup with default mappings and apply the template mapping to it.
      /// </summary>
      public void InitializeSetupIfNeeded(IFCPropertySetups.PropertySetup propertySetup, IFCVersion ifcVersion, IFCParameterTemplate parameterTemplate)
      {
         if (SetupInfos.TryGetValue(propertySetup, out Dictionary<string, PSetMappingInfo> setupInfo) && setupInfo != null)
            return;

         Initialize(propertySetup, ifcVersion);
         ApplyTemplate(propertySetup, parameterTemplate);
      }

      /// <summary>
      /// Save the property mappings of a property setup to a template
      /// </summary>
      public bool WriteToTemplate(IFCPropertySetups.PropertySetup propertySetup, IFCParameterTemplate parameterTemplate)
      {
         if (parameterTemplate == null)
            return false;

         if (!SetupInfos.TryGetValue(propertySetup, out Dictionary<string, PSetMappingInfo> setupInfo) || setupInfo == null)
            return false;

         bool result = false;
         PropertySetupType propertySetupType = IFCPropertySetups.ToPropertySetupType(propertySetup);
         parameterTemplate.ClearPropertySets(propertySetupType);

         foreach (PSetMappingInfo psetInfo in setupInfo.Values)
         {
            string psetName = psetInfo.Name;
            if (string.IsNullOrEmpty(psetName))
               continue;

            List<PropertyMappingInfo> propertyInfos = psetInfo.PropertyInfos;
            if (propertyInfos == null)
               continue;

            bool isExportingPset = psetInfo.ExportFlag;

            List<IFCPropertyMappingInfo> modifiedPropertyMappings = new();
            foreach (var propertyInfo in propertyInfos)
            {
               if (propertyInfo.IsDefault())
                  continue;

               modifiedPropertyMappings.Add(new IFCPropertyMappingInfo
               {
                  ExportFlag = propertyInfo.ExportFlag,
                  IFCPropertyName = propertyInfo.IFCPropertyName,
                  RevitPropertyId = propertyInfo.RevitPropertyId,
                  RevitPropertyName = propertyInfo.RevitPropertyName
               });
            }

            if (!isExportingPset || modifiedPropertyMappings.Count > 0)
            {
               parameterTemplate.AddPropertySet(propertySetupType, isExportingPset, psetName);

               foreach (var propertyMapping in modifiedPropertyMappings)
               {
                  if (!IFCPropertyMappingInfo.IsValidMappingInfo(propertyMapping))
                     continue;

                  parameterTemplate.AddPropertyMappingInfo(propertySetupType, psetName, propertyMapping);
               }
               result = true;
            }
         }

         return result;
      }

      /// <summary>
      /// Get the property set mappings of a property setup.
      /// </summary>
      public List<PSetMappingInfo> GetPropertySetList(IFCPropertySetups.PropertySetup propertySetup)
      {
         if (!SetupInfos.TryGetValue(propertySetup, out Dictionary<string, PSetMappingInfo> setupInfo) || setupInfo == null)
            return [];

         return [.. setupInfo.Values];
      }

      /// <summary>
      /// Whether the model contains the property set mapping info for a property setup.
      /// </summary>
      public bool ContainsPropertySet(IFCPropertySetups.PropertySetup propertySetup, string psetName)
      {
         if (!SetupInfos.TryGetValue(propertySetup, out Dictionary<string, PSetMappingInfo> setupInfo) || setupInfo == null)
            return false;

         if (!setupInfo.TryGetValue(psetName, out PSetMappingInfo modelPSetInfo) || modelPSetInfo == null)
            return false;

         return true;
      }

      /// <summary>
      /// Whether the model contains the property set mapping info for a property setup.
      /// </summary>
      public List<PropertyMappingInfo> GetPropertyList(IFCPropertySetups.PropertySetup propertySetup, string psetName)
      {
         if (!SetupInfos.TryGetValue(propertySetup, out Dictionary<string, PSetMappingInfo> setupInfo) || setupInfo == null)
            return [];

         if (!setupInfo.TryGetValue(psetName, out PSetMappingInfo modelPSetInfo) || modelPSetInfo == null)
            return [];

         return modelPSetInfo.PropertyInfos;
      }

      private void Initialize(IFCPropertySetups.PropertySetup propertySetup, IFCVersion ifcVersion)
      {
         if (!SetupInfos.TryGetValue(propertySetup, out Dictionary<string, PSetMappingInfo> setupInfo) || setupInfo == null)
         {
            setupInfo = new();
            SetupInfos[propertySetup] = setupInfo;
         }

         switch (propertySetup)
         {
            case IFCPropertySetups.PropertySetup.IFCCommonPropertySets:
               {
                  InitializeIFCCommonPropertySets(setupInfo, ifcVersion);
                  break;
               }
            case IFCPropertySetups.PropertySetup.RevitPropertySets:
               {
                  InitializeRevitPropertySetsList(setupInfo);
                  break;
               }
            case IFCPropertySetups.PropertySetup.BaseQuantities:
               {
                  InitializeBaseQuantities(setupInfo, ifcVersion);
                  break;
               }
            case IFCPropertySetups.PropertySetup.MaterialPropertySets:
               {
                  InitializeMaterialPropertySets(setupInfo);
                  break;
               }
            case IFCPropertySetups.PropertySetup.Schedules:
               {
                  InitializeSchedules(setupInfo);
                  break;
               }
            default:
               break;

         }
      }

      private void ApplyTemplate(IFCPropertySetups.PropertySetup propertySetup, IFCParameterTemplate parameterTemplate)
      {
         if (!SetupInfos.TryGetValue(propertySetup, out Dictionary<string, PSetMappingInfo> setupInfo) || setupInfo == null)
            return;

         PropertySetupType propertySetupType = IFCPropertySetups.ToPropertySetupType(propertySetup);

         IList<string> templatePSetNames = parameterTemplate.GetPropertySetNames(propertySetupType, PropertySelectionType.All);

         foreach (string templatePSetName in templatePSetNames)
         {
            if (!setupInfo.TryGetValue(templatePSetName, out PSetMappingInfo modelPSetInfo) || modelPSetInfo == null)
               continue;

            modelPSetInfo.ExportFlag = parameterTemplate.IsExportingPropertySet(propertySetupType, templatePSetName);

            IList<IFCPropertyMappingInfo> templatePropertyInfos =
               parameterTemplate.GetPropertyMappingInfos(propertySetupType, templatePSetName, PropertySelectionType.All);

            foreach (IFCPropertyMappingInfo info in templatePropertyInfos)
            {
               bool exportFlag = info.ExportFlag;
               string ifcPropertyName = info.IFCPropertyName;
               string revitPropertyName = info.RevitPropertyName;
               ElementId revitPropertyId = info.RevitPropertyId;

               if (!modelPSetInfo.TryGetProperty(ifcPropertyName, revitPropertyId, revitPropertyName, out PropertyMappingInfo modelPropertyInfo) || modelPropertyInfo == null)
                  continue;

               modelPropertyInfo.OverwriteMappingValues(exportFlag, ifcPropertyName, revitPropertyId, revitPropertyName);
            }
         }
      }

      private void InitializeIFCCommonPropertySets(Dictionary<string, PSetMappingInfo> setupInfo, IFCVersion ifcVersion)
      {
         if (setupInfo == null)
            return;

         setupInfo.Clear();
         List<IList<PropertySetDescription>> allPropertySets = new();
         ExporterInitializer.PopulateIFCCommonPropertySets(ifcVersion, allPropertySets);

         if ((allPropertySets?.Count ?? 0) == 0 || allPropertySets[0] == null)
            return;

         IFCPropertySetups.PropertySetup propertySetup = IFCPropertySetups.PropertySetup.IFCCommonPropertySets;

         // TODO: what about [ind] > 0 ?
         foreach (var setDescription in allPropertySets[0])
         {
            if ((setDescription?.Entries?.Count ?? 0) == 0)
               continue;

            string setName = setDescription.Name;
            List<PropertyMappingInfo> propertyInfos = new();
            foreach (var entry in setDescription.Entries)
            {
               propertyInfos.Add(new PropertyMappingInfo(entry.PropertyName, string.Empty, ElementId.InvalidElementId, propertySetup));
            }

            setupInfo.TryAdd(setName, new PSetMappingInfo(setName, propertySetup, propertyInfos));
         }
      }

      private void InitializeRevitPropertySetsList(Dictionary<string, PSetMappingInfo> setupInfo)
      {
         if (setupInfo == null)
            return;

         setupInfo.Clear();

         Dictionary<KeyValuePair<ForgeTypeId, string>, ForgeTypeId> allParamDict = GetAllBuiltInParameters()?.Concat(GetAllNonBuiltInParameters())?.ToDictionary();
         if (allParamDict == null)
            return;

         Dictionary<KeyValuePair<string, ForgeTypeId>, List<KeyValuePair<ForgeTypeId, string>>> GroupedParameters = new();

         foreach (var paramKeyValue in allParamDict)
         {
            ForgeTypeId groupTypeId = paramKeyValue.Value;
            if (groupTypeId == null || groupTypeId.Empty())
               continue;

            string groupName = LabelUtils.GetLabelForGroup(groupTypeId);
            if (string.IsNullOrEmpty(groupName))
               continue;

            KeyValuePair<string, ForgeTypeId> groupKeyValue = new(groupName, groupTypeId);

            if (GroupedParameters.ContainsKey(groupKeyValue))
               GroupedParameters[groupKeyValue].Add(paramKeyValue.Key);
            else
            {
               List<KeyValuePair<ForgeTypeId, string>> paramTypeIds = new() { paramKeyValue.Key };
               GroupedParameters.Add(groupKeyValue, paramTypeIds);
            }
         }

         IFCPropertySetups.PropertySetup propertySetup = IFCPropertySetups.PropertySetup.RevitPropertySets;

         foreach (var groupedParameter in GroupedParameters)
         {
            KeyValuePair<string, ForgeTypeId> paramGroup = groupedParameter.Key;
            if ((groupedParameter.Value?.Count ?? 0) == 0)
               continue;

            string groupName = paramGroup.Key;
            List<PropertyMappingInfo> propertyInfos = new();
            foreach (var param in groupedParameter.Value)
            {
               // TODO: Write Revit parameter ID
               propertyInfos.Add(new PropertyMappingInfo(string.Empty, param.Value, ElementId.InvalidElementId, propertySetup));
            }
            setupInfo.TryAdd(groupName, new PSetMappingInfo(groupName, propertySetup, propertyInfos));
         }
      }

      private void InitializeBaseQuantities(Dictionary<string, PSetMappingInfo> setupInfo, IFCVersion ifcVersion)
      {
         if (setupInfo == null)
            return;

         setupInfo.Clear();
         List<IList<QuantityDescription>> allQuantitySets = new();
         ExporterInitializer.PopulateBaseQuantitiesPropertySets(ifcVersion, allQuantitySets);

         if ((allQuantitySets?.Count ?? 0) == 0 || allQuantitySets[0] == null)
            return;

         IFCPropertySetups.PropertySetup propertySetup = IFCPropertySetups.PropertySetup.BaseQuantities;

         // TODO: what about [ind] > 0 ?
         foreach (var setDescription in allQuantitySets[0])
         {
            if ((setDescription?.Entries?.Count ?? 0) == 0)
               continue;

            string setName = setDescription.Name;
            List<PropertyMappingInfo> propertyInfos = new();
            foreach (var entry in setDescription.Entries)
            {
               propertyInfos.Add(new PropertyMappingInfo(entry.PropertyName, string.Empty, ElementId.InvalidElementId, propertySetup));
            }
            setupInfo.TryAdd(setName, new PSetMappingInfo(setName, propertySetup, propertyInfos));
         }
      }

      private void InitializeMaterialPropertySets(Dictionary<string, PSetMappingInfo> setupInfo)
      {
         if (setupInfo == null)
            return;

         setupInfo.Clear();

         IFCPropertySetups.PropertySetup propertySetup = IFCPropertySetups.PropertySetup.MaterialPropertySets;

         List<Element> containedElements = new List<Element>();
         FilteredElementCollector elementsInDocumentCollector = new FilteredElementCollector(IFCCommandOverrideApplication.TheDocument).WhereElementIsNotElementType();
         foreach (Element containedElement in elementsInDocumentCollector)
         {
            if (containedElement.Category != null && containedElement.Category.HasMaterialQuantities)
               containedElements.Add(containedElement);
         }

         // Collection of parameters without duplicates
         HashSet<KeyValuePair<ForgeTypeId, string>> identityParams = new();
         HashSet<KeyValuePair<ForgeTypeId, string>> structParams = new();
         HashSet<KeyValuePair<ForgeTypeId, string>> thermalParams = new();

         foreach (Element containedElement in containedElements)
         {
            ICollection<ElementId> matIds = containedElement.GetMaterialIds(false);
            if (matIds == null || matIds.Count == 0)
               continue;

            foreach (ElementId matId in matIds)
            {
               if (matId == ElementId.InvalidElementId)
                  continue;

               Material material = IFCCommandOverrideApplication.TheDocument.GetElement(matId) as Material;
               if (material == null)
                  continue;

               CollectIdentityParameters(material, identityParams);
               CollectStructuralParameters(material, structParams);
               CollectThermalParameters(material, thermalParams);
            }
         }

         // Add group of Identity material properties.
         List<PropertyMappingInfo> propertyInfos = GetMaterialPropertyMappingInfo(identityParams);
         string setName = MaterialParamTypesEnum.Identity.ToString();
         setupInfo.TryAdd(setName, new PSetMappingInfo(setName, propertySetup, propertyInfos));

         // Add group of Physical (Structural) material properties.
         propertyInfos = GetMaterialPropertyMappingInfo(structParams);
         setName = MaterialParamTypesEnum.Physical.ToString();
         setupInfo.TryAdd(setName, new PSetMappingInfo(setName, propertySetup, propertyInfos));

         // Add group of Thermal material properties.
         propertyInfos = GetMaterialPropertyMappingInfo(thermalParams);
         setName = MaterialParamTypesEnum.Thermal.ToString();
         setupInfo.TryAdd(setName, new PSetMappingInfo(setName, propertySetup, propertyInfos));
      }

      private void InitializeSchedules(Dictionary<string, PSetMappingInfo> setupInfo)
      {
         if (setupInfo == null)
            return;

         setupInfo.Clear();

         List<IList<PropertySetDescription>> allSchedules = new();
         ExporterInitializer.PopulateCustomPropertySets(IFCCommandOverrideApplication.TheDocument, allSchedules);

         if ((allSchedules?.Count ?? 0) == 0 || allSchedules[0] == null)
            return;

         IFCPropertySetups.PropertySetup propertySetup = IFCPropertySetups.PropertySetup.Schedules;

         foreach (var setDescription in allSchedules[0])
         {
            if ((setDescription?.Entries?.Count ?? 0) == 0)
               continue;

            string psetName = setDescription.Name;
            List<PropertyMappingInfo> propertyInfos = new();
            foreach (var entry in setDescription.Entries)
            {
               propertyInfos.Add(new PropertyMappingInfo(string.Empty, entry.PropertyName, ElementId.InvalidElementId, propertySetup));
            }
            setupInfo.TryAdd(psetName, new PSetMappingInfo(psetName, propertySetup, propertyInfos));
         }
      }

      private static Dictionary<KeyValuePair<ForgeTypeId, string>, ForgeTypeId> GetAllBuiltInParameters()
      {
         if (AllBuiltInParamertersCache == null)
         {
            AllBuiltInParamertersCache = new Dictionary<KeyValuePair<ForgeTypeId, string>, ForgeTypeId>();

            foreach (ForgeTypeId paramTypeId in ParameterUtils.GetAllBuiltInParameters())
            {
               if (paramTypeId == null || paramTypeId.Empty())
                  continue;

               ForgeTypeId groupTypeId = ParameterUtils.GetBuiltInParameterGroupTypeId(paramTypeId);
               if (groupTypeId == null || groupTypeId.Empty())
                  continue;

               string paramName = LabelUtils.GetLabelForBuiltInParameter(paramTypeId);
               if (string.IsNullOrEmpty(paramName))
                  continue;

               AllBuiltInParamertersCache.Add(new KeyValuePair<ForgeTypeId, string>(paramTypeId, paramName), groupTypeId);
            }
         }
         return AllBuiltInParamertersCache;
      }

      private static Dictionary<KeyValuePair<ForgeTypeId, string>, ForgeTypeId> GetAllNonBuiltInParameters()
      {
         Dictionary<KeyValuePair<ForgeTypeId, string>, ForgeTypeId> paramDict = new Dictionary<KeyValuePair<ForgeTypeId, string>, ForgeTypeId>();

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

            ForgeTypeId groupTypeId = paramDefinition.GetGroupTypeId();
            if (groupTypeId == null || groupTypeId.Empty())
               continue;

            ForgeTypeId paramTypeId = paramDefinition.GetParameterTypeId();
            if (paramTypeId == null || paramTypeId.Empty())
               continue;

            string paramName = paramDefinition.Name;
            if (string.IsNullOrEmpty(paramName))
               continue;

            paramDict.Add(new KeyValuePair<ForgeTypeId, string>(paramTypeId, paramName), groupTypeId);
         }
         return paramDict;
      }

      public void ResetAll(IFCPropertySetups.PropertySetup propertySetup, string name)
      {
         if (!SetupInfos.TryGetValue(propertySetup, out Dictionary<string, PSetMappingInfo> setupInfo) || setupInfo == null || string.IsNullOrEmpty(name))
            return;

         if (!setupInfo.TryGetValue(name, out PSetMappingInfo psetMappingInfo))
            return;

         if (psetMappingInfo == null)
            return;

         List<PropertyMappingInfo> propertyMappingInfos = psetMappingInfo.PropertyInfos;
         if (propertyMappingInfos == null)
            return;

         psetMappingInfo.ExportFlag = true;

         foreach (var mappingInfo in propertyMappingInfos)
            mappingInfo.ResetToDefault();
      }
   }


   /// <summary>
   /// Property Set mapping information
   /// </summary>
   public class PSetMappingInfo : INotifyPropertyChanged
   {
      public PSetMappingInfo(string name, IFCPropertySetups.PropertySetup propertySetup, List<PropertyMappingInfo> propertiesInfo)
      {
         Name = name;
         Type = IFCPropertyMappingModel.GetMappingType(propertySetup);
         PropertyInfos = propertiesInfo;
      }

      public string Name { get; set; }

      private bool m_ExportFlag = true;

      /// <summary>
      /// Flag to determine if a property set is exported or not.
      /// </summary>
      public bool ExportFlag
      {
         get { return m_ExportFlag; }
         set
         {
            m_ExportFlag = value;
            OnPropertyChanged();
         }
      }

      public List<string> ApplicableEntities { get; set; }
      public List<PropertyMappingInfo> PropertyInfos { get; set; }
      public IFCPropertyMappingModel.MappingType Type { get; set; }

      public bool TryGetProperty(string ifcPropertyName, ElementId revitPropertyId, string revitPropertyName, out PropertyMappingInfo modelPropertyInfo)
      {
         modelPropertyInfo = null;
         if ((PropertyInfos?.Count ?? 0) == 0)
            return false;

         Func<PropertyMappingInfo, bool> keyComparator = PropertyMappingInfo.GetPropertyMappingKeyComparator(Type, ifcPropertyName, revitPropertyId, revitPropertyName);
         modelPropertyInfo = PropertyInfos.FirstOrDefault(keyComparator);
         return modelPropertyInfo != null;
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
      public PropertyMappingInfo(IFCPropertySetups.PropertySetup propertySetup)
      {
         Type = IFCPropertyMappingModel.GetMappingType(propertySetup);
      }
      public PropertyMappingInfo(string ifcPropertyName, string revitPropertyName, ElementId revitPropertyId, IFCPropertySetups.PropertySetup propertySetup)
      {
         IFCPropertyName = ifcPropertyName;
         RevitPropertyName = revitPropertyName;
         RevitPropertyId = revitPropertyId;
         Type = IFCPropertyMappingModel.GetMappingType(propertySetup);
      }

      private bool m_ExportFlag = true;

      /// <summary>
      /// Flag to determine if a PropertyMappingInfo is exported or not.
      /// </summary>
      public bool ExportFlag
      {
         get { return m_ExportFlag; }
         set
         {
            m_ExportFlag = value;
            OnPropertyChanged();
         }
      }

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

      public IFCPropertyMappingModel.MappingType Type { get; set; }

      public static Func<PropertyMappingInfo, bool> GetPropertyMappingKeyComparator(IFCPropertyMappingModel.MappingType mappingType, string ifcPropertyName, ElementId revitPropertyId, string revitPropertyName)
      {
         return (mappingType == IFCPropertyMappingModel.MappingType.IfcToRevit) ?
            (x => x.IFCPropertyName == ifcPropertyName) :
            (x => x.RevitPropertyId == revitPropertyId && x.RevitPropertyName == revitPropertyName);
      }

      public bool OverwriteMappingValues(bool exportFlag, string ifcPropertyName, ElementId revitPropertyId, string revitPropertyName)
      {
         Func<PropertyMappingInfo, bool> keyComparator = GetPropertyMappingKeyComparator(Type, ifcPropertyName, revitPropertyId, revitPropertyName);
         if (!keyComparator(this))
            return false;

         ExportFlag = exportFlag;
         if (Type == IFCPropertyMappingModel.MappingType.IfcToRevit)
         {
            IFCPropertyName = ifcPropertyName;
         }
         else
         {
            RevitPropertyId = revitPropertyId;
            RevitPropertyName = revitPropertyName;
         }
         return true;
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
            if (!string.IsNullOrEmpty(IFCPropertyName))
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
            IFCPropertyName = string.Empty;
      }

      public event PropertyChangedEventHandler PropertyChanged;

      protected void OnPropertyChanged([CallerMemberName] string name = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }
   }
}
