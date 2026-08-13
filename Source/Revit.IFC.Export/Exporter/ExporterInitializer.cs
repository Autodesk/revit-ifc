//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2013  Autodesk, Inc.
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Exporter.PropertySet.Calculators;
using Revit.IFC.Export.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Initializes user defined parameters and quantities.
   /// </summary>
   public partial class ExporterInitializer
   {
      static IFCCertifiedEntitiesAndPSets certifiedEntityAndPsetList;

      /// <summary>
      /// Initializes Pset_ProvisionForVoid.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPset_ProvisionForVoid2x(IList<PropertySetDescription> commonPropertySets)
      {
         // The IFC4 version is contained in ExporterInitializer_PsetDef.cs.
         if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4
            || !certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Pset_ProvisionForVoid"))
            return;

         PropertySetDescription propertySetProvisionForVoid = new PropertySetDescription();
         propertySetProvisionForVoid.Name = "Pset_ProvisionForVoid";

         propertySetProvisionForVoid.EntityTypes.Add(IFCEntityType.IfcBuildingElementProxy);
         propertySetProvisionForVoid.PredefinedTypes.Add("USERDEFINED");
         propertySetProvisionForVoid.ObjectType = "PROVISIONFORVOID";

         // The Shape value must be determined first, as other calculators will use the value stored.
         PropertySetEntry ifcPSE = PropertySetEntry.CreateLabel("Shape");
         ifcPSE.PropertyCalculator = ShapeCalculator.Instance;
         propertySetProvisionForVoid.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntry.CreatePositiveLength("Width");
         ifcPSE.PropertyCalculator = WidthCalculator.Instance;
         propertySetProvisionForVoid.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntry.CreatePositiveLength("Height");
         ifcPSE.PropertyCalculator = HeightCalculator.Instance;
         propertySetProvisionForVoid.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntry.CreatePositiveLength("Diameter");
         ifcPSE.PropertyCalculator = DiameterCalculator.Instance;
         propertySetProvisionForVoid.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntry.CreatePositiveLength("Depth");
         ifcPSE.PropertyCalculator = DepthCalculator.Instance;
         propertySetProvisionForVoid.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntry.CreateLabel("System");
         propertySetProvisionForVoid.AddEntry(ifcPSE);

         commonPropertySets.Add(propertySetProvisionForVoid);
      }

      /// <summary>
      /// Get the list of property sets that are common but not included in the base set.
      /// </summary>
      /// <param name="propertySets">The list of lists of property sets.</param>
      public static void InitExtraCommonPropertySets(IList<IList<PropertySetDescription>> propertySets)
      {
         IList<PropertySetDescription> commonPropertySets = new List<PropertySetDescription>();
         InitPset_ProvisionForVoid2x(commonPropertySets);
         propertySets.Add(commonPropertySets);
      }

      /// <summary>
      /// This will traverse all PropertySetDescriptions and filter PropertySetDiscriptions accordingly:
      /// If a PropertySetDescription has more than one entity associated with it, then it may apply to both Instance and Type entities.
      /// If a PropertySetDescription has only one entity associated with it, then it cannot apply to both Instance and Type entities.
      /// This is to reduce the amount of noise in the InstanceAndTypePSetIndices list.
      /// </summary>
      /// <param name="propertySetListLists">List of list of PropertySetDescrptions to parse..</param>
      /// <param name="multipleEntityPropertySetListLists">List of list of PropertySetDescriptions that have multiple entities assigned.</param>
      /// <param name="singleEntityPropertySetListLists">List of list of PropertySetDescriptions that have only one entity assigned.</param>
      public static void FilterPropertySets(IList<IList<PropertySetDescription>> propertySetListLists,
         out IList<IList<PropertySetDescription>> multipleEntityPropertySetListLists,
         out IList<IList<PropertySetDescription>> singleEntityPropertySetListLists)
      {
         multipleEntityPropertySetListLists = null;
         singleEntityPropertySetListLists = null;
         if (propertySetListLists == null)
            return;

         multipleEntityPropertySetListLists = new List<IList<PropertySetDescription>>();
         singleEntityPropertySetListLists = new List<IList<PropertySetDescription>>();
         if (propertySetListLists.Count == 0)
            return;

         foreach (IList<PropertySetDescription> pSetList in propertySetListLists)
         {
            IList<PropertySetDescription> multipleEntityPropertySetList = new List<PropertySetDescription>();
            IList<PropertySetDescription> singleEntityPropertySetList = new List<PropertySetDescription>();

            foreach (PropertySetDescription pSetDesc in pSetList)
            {
               int numEntities = pSetDesc?.EntityTypes?.Count ?? 0;
               if (numEntities == 0)
                  continue;

               if (numEntities == 1)
               {
                  string entity = pSetDesc.EntityTypes.FirstOrDefault().ToString();
                  if (string.IsNullOrWhiteSpace(entity))
                     continue;

                  if (entity.EndsWith("Type"))
                  {
                     singleEntityPropertySetList.Add(pSetDesc);
                     continue;
                  }
               }

               multipleEntityPropertySetList.Add(pSetDesc);
            }

            multipleEntityPropertySetListLists.Add(multipleEntityPropertySetList);
            singleEntityPropertySetListLists.Add(singleEntityPropertySetList);
         }            
      }

      /// <summary>
      /// Initializes property sets.
      /// </summary>
      public static void InitPropertySets()
      {
         ParameterCache cache = ExporterCacheManager.ParameterCache;

         // Some properties, particularly the common properties, apply to both instance
         // and type parameters.  It's actually probably a little more complicated than
         // this, but this preserves current behavior.
         // TODO: Don't have this extra level which can easily be out of sync and is
         // potentially too generic.
         IList<int> instanceAndTypePsetIndices = new List<int>();

         if (ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportIFCCommon)
         {
            IList<IList<PropertySetDescription>> allCommonPropertySets = new List<IList<PropertySetDescription>>();

            // Even though this populates a List<List<PropertySetDescription>>, in this instance the outer loop should have only one entry.
            // This is by design, to have a uniform pattern with all the other Init methods.
            // But in the case where the outer loop contains more than one entry, process that correctly as well.
            InitCommonPropertySets(allCommonPropertySets);
            ExcludeNotExportingPropertySets(allCommonPropertySets.LastOrDefault(), PropertySetupType.IfcCommonPropertySets);
            ExcludeNotExportingProperties(allCommonPropertySets.LastOrDefault());

            IList<IList<PropertySetDescription>> multipleEntityPropertySetListLists = null;
            IList<IList<PropertySetDescription>> singleEntiyPropertySetListLists = null;
            FilterPropertySets(allCommonPropertySets, out multipleEntityPropertySetListLists, out singleEntiyPropertySetListLists);

            foreach (IList<PropertySetDescription> psetDescList in multipleEntityPropertySetListLists)
            {
               instanceAndTypePsetIndices.Add(cache.PropertySets.Count);
               cache.PropertySets.Add(psetDescList);
            }

            instanceAndTypePsetIndices.Add(cache.PropertySets.Count);
            InitExtraCommonPropertySets(cache.PropertySets);

            InitPreDefinedPropertySets(cache.PreDefinedPropertySets);

            // These property sets should not be pointed to by the instanceAndTypePsetIndicies array.
            foreach (IList<PropertySetDescription> psetDescList in singleEntiyPropertySetListLists)
            {
               cache.PropertySets.Add(psetDescList);
            }
         }

         if (ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportSchedulesAsPsets)
         {
            InitCustomPropertySets(ExporterCacheManager.Document, cache.PropertySets);
            ExcludeNotExportingPropertySets(cache.PropertySets.LastOrDefault(), PropertySetupType.RevitSchedules);
         }

         if (ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportUserDefinedPsets)
         {
            InitUserDefinedPropertySets(cache.PropertySets);
         }

         if (ExporterCacheManager.ExportOptionsCache.ExportAsCOBIE)
         {
            instanceAndTypePsetIndices.Add(cache.PropertySets.Count);
            InitCOBIEPropertySets(cache.PropertySets);
         }

         cache.InstanceAndTypePsetIndices = instanceAndTypePsetIndices;
      }

      private static void ExcludeNotExportingPropertySets(IList<PropertySetDescription> propertySets, PropertySetupType propertySetup)
      {  
         IFCParameterTemplate parameterTemplate = ExporterCacheManager.ParameterMappingTemplate;
         if (parameterTemplate == null)
            return;

         IList<string> nonExportingPropertySets = parameterTemplate.GetPropertySetNames(propertySetup, PropertySelectionType.NonExporting);
         if ((nonExportingPropertySets?.Count ?? 0) == 0)
            return;

         var setsToExclude = propertySets.Where(set => nonExportingPropertySets.Contains(set?.Name, StringComparer.InvariantCultureIgnoreCase)).ToList();
         foreach (var setToExclude in setsToExclude)
            propertySets.Remove(setToExclude);
      }

      private static void ExcludeNotExportingProperties(IList<PropertySetDescription> propertySets)
      {
         if ((propertySets?.Count ?? 0) == 0)
            return;
      
         foreach (var propertySet in propertySets)
         {
            var propertiesToExclude = propertySet.Entries.Where(entry => entry.IsExcluded).ToList();
            foreach (var propertyToExclude in propertiesToExclude)
               propertySet.RemoveEntry(propertyToExclude);
         }
      }

      private static void ExcludeNotExportingQuantitySets(IList<IList<QuantityDescription>> quantitiesToExport)
      {
         IFCParameterTemplate parameterTemplate = ExporterCacheManager.ParameterMappingTemplate;
         if (parameterTemplate == null)
            return;

         IList<string> nonExportingQuantitySets = parameterTemplate.GetPropertySetNames(PropertySetupType.IfcBaseQuantities, PropertySelectionType.NonExporting);
         if (nonExportingQuantitySets == null || nonExportingQuantitySets.Count == 0)
            return;


         if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
         {
            List<string> nonExportingQuantitySetsTypes = nonExportingQuantitySets
               .Select(name => name.Replace("Qto_", "Ifc"))
               .Select(name => name.Replace("BaseQuantities", "")).ToList();

            foreach (var quantitySetList in quantitiesToExport)
            {
               var setsToExclude = quantitySetList.Where(set => nonExportingQuantitySetsTypes.Contains(set?.EntityTypes.First().ToString(), StringComparer.InvariantCultureIgnoreCase)).ToList();
               foreach (var setToExclude in setsToExclude)
                  quantitySetList.Remove(setToExclude);
            }
         }
         else
         {
            foreach (var quantitySetList in quantitiesToExport)
            {
               var setsToExclude = quantitySetList.Where(set => nonExportingQuantitySets.Contains(set?.Name, StringComparer.InvariantCultureIgnoreCase)).ToList();
               foreach (var setToExclude in setsToExclude)
                  quantitySetList.Remove(setToExclude);
            }
         }
      }

      private static void ExcludeNotExportingQuantities(IList<IList<QuantityDescription>> quantitiesToExport)
      {
         if ((quantitiesToExport?.Count ?? 0) == 0)
            return;

         foreach (var quantitySets in quantitiesToExport)
         {
            ExcludeNotExportingQuantities(quantitySets);
         }
      }

      private static void ExcludeNotExportingQuantities(IList<QuantityDescription> quantitySets)
      {
         if ((quantitySets?.Count ?? 0) == 0)
            return;

         foreach (var quantitySet in quantitySets)
         {
            var quantitiesToExclude = quantitySet.Entries.Where(entry => entry.IsExcluded).ToList();
            foreach (var quantityToExclude in quantitiesToExclude)
               quantitySet.RemoveEntry(quantityToExclude);
         }
      }

      private static void ExcludeNotExportingAttributes()
      {        
         if ((ExporterCacheManager.AttributeCache.AttributeSets?.Count ?? 0) == 0)
            return;

         foreach (var attributeSetDescription in ExporterCacheManager.AttributeCache.AttributeSets)
         {
            var attributesToExclude = attributeSetDescription.Entries.Where(entry => entry.IsExcluded).ToList();
            foreach (var attributeToExclude in attributesToExclude)
               attributeSetDescription.RemoveEntry(attributeToExclude);
         }
      }

      /// <summary>
      /// Default constructor that initializes certifiedEntityAndPsetList for IFC Parameter Mapping UI anf for Export IFC.
      /// </summary>
      static ExporterInitializer()
      {
         certifiedEntityAndPsetList = ExporterCacheManager.CertifiedEntitiesAndPsetsCache;
      }

      /// <summary>
      /// Populates common property sets depending on IFC Schema.
      /// </summary>
      /// <param name="fileVersion">The IFC file version.</param>
      /// <param name="allPsetOrQtoSets">Property sets.</param>
      public static void PopulateIFCCommonPropertySets(IFCVersion fileVersion, IList<IList<PropertySetDescription>> allPsetOrQtoSets)
      {
         ExporterCacheManager.ExportOptionsCache.FileVersion = fileVersion;
         InitCommonPropertySets(allPsetOrQtoSets);
      }

      /// <summary>
      /// Populates common property sets depending on IFC Schema.
      /// </summary>
      /// <param name="fileVersion">The IFC file version.</param>
      /// <param name="allPsetOrQtoSets">Property sets.</param>
      public static void PopulateBaseQuantitiesPropertySets(IFCVersion fileVersion, IList<IList<QuantityDescription>> allPsetOrQtoSets)
      {
         ExporterCacheManager.ExportOptionsCache.FileVersion = fileVersion;
         InitQtoSets(allPsetOrQtoSets);

         if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
            return;

         foreach (List<QuantityDescription> propertySet in allPsetOrQtoSets)
         {
            if (propertySet == null)
               continue;

            propertySet.ForEach(x => x.Name = x.EntityTypes.First().ToString() + ".BaseQuantities");
         }
      }

      /// <summary>
      /// Initializes quantities.
      /// </summary>
      /// <param name="fileVersion">The IFC file version.</param>
      /// <param name="exportBaseQuantities">True if export base quantities.</param>
      public static void InitQuantities(Exporter.QuantitiesToExport quantitiesToExport, bool exportBaseQuantities)
      {
         ParameterCache cache = ExporterCacheManager.ParameterCache;

         if (exportBaseQuantities)
         {
            if (quantitiesToExport == null)
               quantitiesToExport = InitQtoSets;
            else
               quantitiesToExport += InitQtoSets;

            quantitiesToExport += ExcludeNotExportingQuantitySets;
            quantitiesToExport += ExcludeNotExportingQuantities;
         }

         if (ExporterCacheManager.ExportOptionsCache.ExportAsCOBIE)
         {
            if (quantitiesToExport == null)
               quantitiesToExport = InitCOBIEQuantities;
            else
               quantitiesToExport += InitCOBIEQuantities;
         }

         quantitiesToExport?.Invoke(cache.Quantities);
      }

      private static ISet<IFCEntityType> GetListOfRelatedEntities(IFCEntityType entityType)
      {
         // Check IfcElementType and its parent types.
         if (entityType == IFCEntityType.IfcElementType ||
            entityType == IFCEntityType.IfcTypeProduct ||
            entityType == IFCEntityType.IfcTypeObject)
         {
            return PropertyUtil.EntitiesWithNoRelatedType;
         }

         return null;
      }

      /// <summary>
      /// Initialize user-defined property and quantity sets
      /// </summary>
      /// <param name="propertySets">List of Psets</param>
      private static void InitUserDefinedPropertySets(IList<IList<PropertySetDescription>> propertySets)
      {         
         IList<PropertySetDescription> userDefinedPropertySets = null;
         IList<QuantityDescription> quantityDescriptions = null;

         if (!OptionsUtil.UseLegacyParameterMapping())
            CollectUserDefinedDescriptionsFromDocument(out userDefinedPropertySets, out quantityDescriptions);
         else
            CollectUserDefinedDescriptionsFromTxt(out userDefinedPropertySets, out quantityDescriptions);

         propertySets.Add(userDefinedPropertySets);

         if (quantityDescriptions.Count > 0)
            ExporterCacheManager.ParameterCache.Quantities.Add(quantityDescriptions);
      }

      private static void CollectUserDefinedDescriptionsFromTxt(out IList<PropertySetDescription> userDefinedPropertySets, 
         out IList<QuantityDescription> quantityDescriptions)
      {
         userDefinedPropertySets = new List<PropertySetDescription>();
         quantityDescriptions = new List<QuantityDescription>();

         // get the Pset definitions (using the same file as PropertyMap)
         bool exportPre4 = (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 || ExporterCacheManager.ExportOptionsCache.ExportAs2x3);
         IEnumerable<UserDefinedPropertySet> userDefinedPsetDefs = PropertyMap.LoadUserDefinedPset();

         // Loop through each definition and add the Pset entries into Cache
         foreach (UserDefinedPropertySet propertySet in userDefinedPsetDefs)
         {
            // Add Propertyset entry
            Description description = null;
            if (string.Compare(propertySet.Name, "Attribute Mapping", true) == 0)
            {
               AttributeSetDescription attributeDescription = new AttributeSetDescription();
               ExporterCacheManager.AttributeCache.AddAttributeSet(attributeDescription);
               foreach (UserDefinedProperty property in propertySet.Properties)
               {
                  // Data types to export is not provided or invalid.
                  if ((property.IfcPropertyTypes?.Count ?? 0) == 0)
                     continue;

                  PropertyType dataType = property.FirstIfcPropertyTypeOrDefault(PropertyType.Text);
                  List<AttributeEntryMap> entryMap = property.GetEntryMap((name, parameter) => new AttributeEntryMap(name, parameter));
                  AttributeEntry aSE = new AttributeEntry(property.Name, dataType, entryMap);
                  attributeDescription.AddEntry(aSE);
               }

               description = attributeDescription;
            }
            else if (propertySet.Type?.StartsWith("Qto_", StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
               QuantityDescription quantityDescription = new QuantityDescription();
               quantityDescriptions.Add(quantityDescription);
               description = quantityDescription;
               foreach (UserDefinedProperty property in propertySet.Properties)
               {
                  // Data types to export is not provided or invalid.
                  if ((property.IfcPropertyTypes?.Count ?? 0) == 0)
                     continue;

                  QuantityType quantityType = property.FirstIfcPropertyTypeOrDefault(QuantityType.Real);
                  IList<QuantityEntryMap> entryMap = property.GetEntryMap((name, parameter) => new QuantityEntryMap(name, parameter));
                  QuantityEntry quantityEntry = new(quantityType, property.Name, entryMap);
                  quantityDescription.AddEntry(quantityEntry);
               }
            }
            else
            {
               PropertySetDescription userDefinedPropertySet = new PropertySetDescription();
               userDefinedPropertySet.AddTypePropertiesToInstance = ExporterCacheManager.ExportOptionsCache.PropertySetOptions.UseTypePropertiesInInstacePSets;
               description = userDefinedPropertySet;
               foreach (UserDefinedProperty property in propertySet.Properties)
               {
                  PropertyValueType valueType = property.IfcPropertyValueType;
                  PropertyType primaryType = property.FirstIfcPropertyTypeOrDefault(PropertyType.Text); // force default to Text/string if the type does not match with any correct datatype
                  PropertyType secondaryType = property.GetIfcPropertyAtOrDefault(1, PropertyType.Text);
                  if (valueType == PropertyValueType.TableValue)
                     (primaryType, secondaryType) = (secondaryType, primaryType);

                  IList<PropertySetEntryMap> entryMap = property.GetEntryMap((name, parameter) => new PropertySetEntryMap(name, parameter));
                  if (entryMap.Count > 0)
                  {
                     PropertySetEntry propertySetEntry = new PropertySetEntry(primaryType, property.Name, entryMap);
                     userDefinedPropertySet.AddEntry(propertySetEntry);
                  }
                  else
                  {
                     PropertySetEntry propertySetEntry = new PropertySetEntry(property.Name)
                     {
                        PropertyName = property.Name,
                        PropertyType = primaryType,
                        PropertyArgumentType = secondaryType,
                        PropertyValueType = property.IfcPropertyValueType
                     };
                     userDefinedPropertySet.AddEntry(propertySetEntry);
                  }
               }

               userDefinedPropertySets.Add(userDefinedPropertySet);
            }

            description.Name = propertySet.Name;
            description.DescriptionOfSet = string.Empty;

            HashSet<IFCEntityType> entityTypes = GetIfcEntityTypesFromStrings(propertySet.IfcEntities, exportPre4);
            foreach (IFCEntityType entityType in entityTypes)
            {
               description.EntityTypes.Add(entityType);
            }
         }
      }

      private static void CollectUserDefinedDescriptionsFromDocument(out IList<PropertySetDescription> userDefinedPropertySets,
         out IList<QuantityDescription> userDefinedQuantitySets)
      {
         userDefinedPropertySets = new List<PropertySetDescription>();
         userDefinedQuantitySets = new List<QuantityDescription>();
         Document document = ExporterCacheManager.Document;
         bool exportPre4 = (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 || ExporterCacheManager.ExportOptionsCache.ExportAs2x3);

         IList<string> propertySetNames = IFCUserDefinedPropertySet.ListPropertySetNames(document);
         foreach (string psetName in propertySetNames)
         {
            if (PropertyUtil.IsPropertySetExcluded(PropertySetupType.UserDefinedPropertySets, psetName))
               continue;

            IFCUserDefinedPropertySet userDefinedSet = IFCUserDefinedPropertySet.FindPropertySetByName(document, psetName);
            if (userDefinedSet == null)
               continue;

            Description description = null;

            switch (userDefinedSet.PropertySetType)
            {
               case IFCUserDefinedPropertySetType.QuantitySet:
                  {
                     description = CreateAndAddQuantitySetDescription(userDefinedSet, ref userDefinedQuantitySets);
                     break;
                  }
                  case IFCUserDefinedPropertySetType.IFCAttributeSet:
                  {
                     description = CreateAndAddAttributeSetDescription(userDefinedSet);
                     break;
                  }
                  default:
                  {
                     description = CreateAndAddPropertySetDescription(userDefinedSet, ref userDefinedPropertySets);
                     break;
                  }
            }

            if (description == null)
               continue;

            description.Name = psetName;
            description.DescriptionOfSet = string.Empty;

            var applicableEntities = userDefinedSet.GetApplicableEntities();
            description.EntityTypes.UnionWith(GetIfcEntityTypesFromStrings(applicableEntities, exportPre4));
         }

         ExcludeNotExportingProperties(userDefinedPropertySets);
         ExcludeNotExportingQuantities(userDefinedQuantitySets);
         ExcludeNotExportingAttributes();
      }

      private static PropertySetDescription CreateAndAddPropertySetDescription(IFCUserDefinedPropertySet propertySet,
         ref IList<PropertySetDescription> propertyDescriptions)
      {
         PropertySetDescription propertyDescription = new()
         {
            Name = propertySet.Name,
            IsUserDefined = true,
            AddTypePropertiesToInstance = true
         };

         foreach (IFCUserDefinedProperty property in propertySet.GetProperties())
         {
            if (property == null)
               continue;

            string ifcPropertyName = property.IFCPropertyName;
            string revitParameterName = property.RevitPropertyName;            
            ElementId revitParameterId = property.RevitPropertyId;
            if (string.IsNullOrEmpty(revitParameterName) && MathUtil.IsInvalidElementId(revitParameterId))
               revitParameterName = ifcPropertyName;

            BuiltInParameter revitBuiltInParameter = ParameterUtils.IsBuiltInParameter(revitParameterId) ?
               (BuiltInParameter)revitParameterId.Value : BuiltInParameter.INVALID;

            PropertyValueType valueType = property.PropertyType switch
            {
               IFCUserDefinedPropertyType.Single => PropertyValueType.SingleValue,
               IFCUserDefinedPropertyType.Bounded => PropertyValueType.BoundedValue,
               IFCUserDefinedPropertyType.List => PropertyValueType.ListValue,
               IFCUserDefinedPropertyType.Table => PropertyValueType.TableValue,
               _ => PropertyValueType.SingleValue
            };

            if (!Enum.TryParse(property.DataType, out PropertyType primaryType))
               primaryType = PropertyType.Text;

            if (!Enum.TryParse(property.DataTypeDefined, out PropertyType secondaryType))
               secondaryType = PropertyType.Text;

            if (valueType == PropertyValueType.TableValue)
               (primaryType, secondaryType) = (secondaryType, primaryType);

            IList<PropertySetEntryMap> entryMap = [new(revitParameterName, revitBuiltInParameter)];
            PropertySetEntry propertySetEntry = new(primaryType, ifcPropertyName, entryMap)
            {
               PropertyArgumentType = secondaryType,
               PropertyValueType = valueType
            };

            propertyDescription.AddEntry(propertySetEntry);
         }

         propertyDescriptions.Add(propertyDescription);

         return propertyDescription;
      }

      private static QuantityDescription CreateAndAddQuantitySetDescription(IFCUserDefinedPropertySet quantitySet,
         ref IList<QuantityDescription> quantityDescriptions)
      {
         if (quantitySet == null || (quantitySet.PropertySetType != IFCUserDefinedPropertySetType.QuantitySet))
            return null;

         QuantityDescription quantityDescription = new()
         {
            Name = quantitySet.Name,
            IsUserDefined = true
         };

         foreach (IFCUserDefinedProperty property in quantitySet.GetProperties())
         {
            if (property == null)
               continue;

            string ifcQuantityName = property.IFCPropertyName;
            string revitParameterName = property.RevitPropertyName;
            ElementId revitParameterId = property.RevitPropertyId;
            BuiltInParameter revitBuiltInParameter = ParameterUtils.IsBuiltInParameter(revitParameterId) ?
               (BuiltInParameter)revitParameterId.Value : BuiltInParameter.INVALID;

            IFCPropertyMappingInfo mappingInfo = PropertyUtil.GetParameterMappingInfoFromCache(PropertySetupType.UserDefinedPropertySets,
               quantitySet.Name, ElementId.InvalidElementId, ifcQuantityName);
            if ((mappingInfo?.ExportFlag ?? true) == false)
               continue;

            if (!Enum.TryParse(property.DataType, out QuantityType quantityType))
            {
               // force default to Real if the type does not match with any correct datatype
               quantityType = QuantityType.Real;
            }

            IList<QuantityEntryMap> entryMap = [new(revitParameterName, revitBuiltInParameter)];
            QuantityEntry quantityEntry = new(quantityType, ifcQuantityName, entryMap);

            quantityDescription.AddEntry(quantityEntry);
         }

         quantityDescriptions.Add(quantityDescription);

         return quantityDescription;
      }

      private static AttributeSetDescription CreateAndAddAttributeSetDescription(IFCUserDefinedPropertySet propertySet)
      {
         if (propertySet == null || (propertySet.PropertySetType != IFCUserDefinedPropertySetType.IFCAttributeSet))
            return null;

         AttributeSetDescription attributeSetDescription = new()
         {
            Name = propertySet.Name,
         };

         foreach (IFCUserDefinedProperty property in propertySet.GetProperties())
         {
            if (property == null)
               continue;

            string ifcAttributeName = property.IFCPropertyName;
            string revitParameterName = property.RevitPropertyName;
            ElementId revitParameterId = property.RevitPropertyId;
            BuiltInParameter revitBuiltInParameter = ParameterUtils.IsBuiltInParameter(revitParameterId) ?
               (BuiltInParameter)revitParameterId.Value : BuiltInParameter.INVALID;

            IFCPropertyMappingInfo mappingInfo = PropertyUtil.GetParameterMappingInfoFromCache(PropertySetupType.UserDefinedPropertySets,
               propertySet.Name, ElementId.InvalidElementId, ifcAttributeName);
            if ((mappingInfo?.ExportFlag ?? true) == false)
               continue;
            
            if (!Enum.TryParse(property.DataType, out PropertyType propertyType))
            {
               // force default to Text if the type does not match with any correct datatype
               propertyType = PropertyType.Text;
            }
            
            List<AttributeEntryMap> entryMap = [new(revitParameterName, revitBuiltInParameter)];
            AttributeEntry attributeEntry = new(ifcAttributeName, propertyType, entryMap);
            
            attributeSetDescription.AddEntry(attributeEntry);
         }
         ExporterCacheManager.AttributeCache.AddAttributeSet(attributeSetDescription);

         return attributeSetDescription;
      }

      public static HashSet<IFCEntityType> GetIfcEntityTypesFromStrings(IList<string> entityStrings, bool exportPre4)
      {
         HashSet<IFCEntityType> entityTypes = new();
         if ((entityStrings?.Count ?? 0) == 0)
            return entityTypes;

         foreach (string elem in entityStrings)
         {
            if (Enum.TryParse(elem, true, out IFCEntityType ifcEntity))
            {
               bool usedCompatibleType = false;

               if (exportPre4)
               {
                  IFCEntityType originalEntity = ifcEntity;
                  IFCCompatibilityType.CheckCompatibleType(originalEntity, out ifcEntity);
                  usedCompatibleType = (originalEntity != ifcEntity);
               }

               entityTypes.Add(ifcEntity);

               // This is intended mostly as a workaround in IFC2x3 for IfcElementType.  Not all elements have an associated type (e.g. IfcRoof),
               // but we still want to be able to export type property sets for that element.  So we will manually add these extra types here without
               // forcing the user to guess.  If this causes issues, we may come up with a different design.
               if (!usedCompatibleType)
               {
                  ISet<IFCEntityType> relatedEntities = GetListOfRelatedEntities(ifcEntity);
                  if (relatedEntities != null)
                  {
                     entityTypes.UnionWith(relatedEntities);
                  }
               }
            }
         }
         return entityTypes;
      }

      public static bool IsSupportedScheduleField(ScheduleField field)
      {
         if (field == null)
            return false;

         ScheduleFieldType fieldType = field.FieldType;

         if (fieldType == ScheduleFieldType.Instance ||
            fieldType == ScheduleFieldType.ElementType ||
            fieldType == ScheduleFieldType.CombinedParameter)
            return true;

         if (fieldType == ScheduleFieldType.ViewBased)
         {
            ElementId paramId = field.ParameterId;
            return paramId == new ElementId(BuiltInParameter.ROOM_AREA) ||
               paramId == new ElementId(BuiltInParameter.ROOM_PERIMETER);
         }

         return false;
      }

      /// <summary>
      /// Initializes custom property sets from schedules.
      /// </summary>
      /// <param name="propertySets">List to store property sets.</param>
      /// <param name="propertySets">The list of lists of property sets.</param>
      /// <param name="ignoreMappingTemplate">Whether to add property if it's excluded in mapping template</param>
      private static void InitCustomPropertySets(Document document, IList<IList<PropertySetDescription>> propertySets)
      {
         IList<PropertySetDescription> customPropertySets = new List<PropertySetDescription>();

         // Collect all ViewSchedules from the document to use as custom property sets.
         FilteredElementCollector viewScheduleElementCollector = new FilteredElementCollector(document);

         ElementFilter viewScheduleElementFilter = new ElementClassFilter(typeof(ViewSchedule));
         viewScheduleElementCollector.WherePasses(viewScheduleElementFilter);
         List<ViewSchedule> filteredSchedules = viewScheduleElementCollector.Cast<ViewSchedule>().ToList();

         int unnamedScheduleIndex = 1;

         string includePattern = "PSET|IFC|COMMON";

         bool exportSpecificSchedules = false;
         if (ExporterCacheManager.ExportOptionsCache.PropertySetOptions != null)
            exportSpecificSchedules = ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportSpecificSchedules;

         if (exportSpecificSchedules)
         {
            var resultQuery =
                from viewSchedule in viewScheduleElementCollector
                where viewSchedule.Name != null &&
                System.Text.RegularExpressions.Regex.IsMatch(viewSchedule.Name, includePattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                select viewSchedule;
            filteredSchedules = resultQuery.Cast<ViewSchedule>().ToList();
         }

         foreach (ViewSchedule schedule in filteredSchedules)
         {
            // ViewSchedule may be a template view and it will not have the associated view and elements. Skip this type of schedule
            if (schedule.IsTemplate)
               continue;

            // Allow schedules to be skipped if set to not export via built-in or shared parameters.
            IFCExportElement? exportSchedule = ElementFilteringUtil.GetExportElementState(schedule, null);
            if (exportSchedule.GetValueOrDefault(IFCExportElement.Yes) == IFCExportElement.No)
               continue;

            ScheduleDefinition definition = schedule.Definition;
            if (definition == null)
               continue;

            int fieldCount = definition.GetFieldCount();
            if (fieldCount == 0)
               continue;

            PropertySetDescription customPSet = new();

            string scheduleName = NamingUtil.GetNameOverride(schedule, schedule.Name);
            if (string.IsNullOrWhiteSpace(scheduleName))
            {
               scheduleName = "Unnamed Schedule " + unnamedScheduleIndex;
               unnamedScheduleIndex++;
            }
            customPSet.Name = scheduleName;

            // The schedule will be responsible for determining which elements to actually export.
            // Note that this currently only works for schedules in the host document.
            customPSet.ViewScheduleId = schedule.Id;
            customPSet.EntityTypes.Add(IFCEntityType.IfcProduct);

            HashSet<ElementId> containedElementIds = new();
            List<Element> elementsInViewSchedule = new FilteredElementCollector(document, schedule.Id).ToList();
            foreach (Element containedElement in elementsInViewSchedule)
            {
               containedElementIds.Add(containedElement.Id);
               ElementId typeId = containedElement.GetTypeId();
               if (!MathUtil.IsInvalidElementId(typeId))
                  containedElementIds.Add(typeId);
            }
            ExporterCacheManager.ViewScheduleElementCache.TryAdd(schedule.Id, containedElementIds);

            IDictionary<ElementId, Element> cachedElementTypes = new Dictionary<ElementId, Element>();

            for (int ii = 0; ii < fieldCount; ii++)
            {
               ScheduleField field = definition.GetField(ii);
               if (!IsSupportedScheduleField(field))
                  continue;

               string propertyName = field.ColumnHeading;

               // Process parameter mapping info
               IFCPropertyMappingInfo mappingInfo = PropertyUtil.GetParameterMappingInfoFromCache(PropertySetupType.RevitSchedules, scheduleName, field.ParameterId, propertyName);
               if ((mappingInfo?.ExportFlag ?? true) == false)
                  continue;

               propertyName = string.IsNullOrEmpty(mappingInfo?.IFCPropertyName) ? propertyName : mappingInfo?.IFCPropertyName;


               // Check if it is a combined parameter.  If so, calculate the formula later 
               // as necessary.
               PropertySetEntry ifcPSE = null;

               switch (field.FieldType)
               {
                  case ScheduleFieldType.CombinedParameter:
                     {
                        ifcPSE = PropertySetEntry.CreateParameterEntry(field.ColumnHeading, field.GetCombinedParameters());
                        break;
                     }
                  default:
                     {
                        ElementId parameterId = field.ParameterId;
                        if (parameterId == ElementId.InvalidElementId)
                           continue;

                        // We use asBuiltInParameterId to get the parameter by id below.  We don't want to use it later, however, so
                        // we store builtInParameterId only if it is a proper member of the enumeration.
                        BuiltInParameter asBuiltInParameterId = (BuiltInParameter)parameterId.Value;
                        BuiltInParameter builtInParameterId =
                            ParameterUtils.IsBuiltInParameter(parameterId) ? (BuiltInParameter)parameterId.Value : BuiltInParameter.INVALID;

                        // We could cache the actual elements when we store the element ids.  However,
                        // this would almost certainly take more time than getting one of the first
                        // few elements in the collector.
                        foreach (Element containedElement in elementsInViewSchedule)
                        {
                           Parameter containedElementParameter = null;

                           if (field.FieldType == ScheduleFieldType.Instance ||
                              field.FieldType == ScheduleFieldType.ViewBased)
                              containedElementParameter = containedElement.get_Parameter(asBuiltInParameterId);

                           // shared parameters can return ScheduleFieldType.Instance, even if they are type parameters, so take a look.
                           if (containedElementParameter == null)
                           {
                              ElementId containedElementTypeId = containedElement.GetTypeId();
                              Element containedElementType = null;
                              if (!MathUtil.IsInvalidElementId(containedElementTypeId))
                              {
                                 if (!cachedElementTypes.TryGetValue(containedElementTypeId, out containedElementType))
                                 {
                                    containedElementType = document.GetElement(containedElementTypeId);
                                    cachedElementTypes[containedElementTypeId] = containedElementType;
                                 }
                              }

                              containedElementParameter = containedElementType?.get_Parameter(asBuiltInParameterId);
                           }

                           if (containedElementParameter != null)
                           {
                              ifcPSE = PropertySetEntry.CreateParameterEntry(containedElementParameter, builtInParameterId);
                              break;
                           }
                        }

                        break;
                     }
               }

               if (ifcPSE != null)
               {
                  ifcPSE.PropertyName = propertyName;
                  customPSet.AddEntry(ifcPSE);
               }
            }

            customPropertySets.Add(customPSet);
         }

         propertySets.Add(customPropertySets);
      }

      #region COBie propertysets
      /// <summary>
      /// Initializes COBIE property sets.
      /// </summary>
      /// <param name="propertySets">List to store property sets.</param>
      private static void InitCOBIEPropertySets(IList<IList<PropertySetDescription>> propertySets)
      {
         IList<PropertySetDescription> cobiePSets = new List<PropertySetDescription>();
         InitCOBIEPSetSpaceThermalSimulationProperties(cobiePSets);
         InitCOBIEPSetSpaceVentilationCriteria(cobiePSets);
         InitCOBIEPSetBuildingEnergyTarget(cobiePSets);
         InitCOBIEPSetGlazingPropertiesEnergyAnalysis(cobiePSets);
         InitCOBIEPSetPhotovoltaicArray(cobiePSets);
         propertySets.Add(cobiePSets);
      }

      /// <summary>
      /// Initializes COBIE space thermal simulation property sets.
      /// </summary>
      /// <param name="cobiePropertySets">List to store property sets.</param>
      private static void InitCOBIEPSetSpaceThermalSimulationProperties(IList<PropertySetDescription> cobiePropertySets)
      {
         PropertySetDescription propertySetSpaceThermalSimulationProperties = new PropertySetDescription();
         propertySetSpaceThermalSimulationProperties.Name = "ePset_SpaceThermalSimulationProperties";
         propertySetSpaceThermalSimulationProperties.EntityTypes.Add(IFCEntityType.IfcSpace);

         PropertySetEntry ifcPSE = PropertySetEntry.CreateLabel("Space Thermal Simulation Type");
         ifcPSE.PropertyName = "SpaceThermalSimulationType";
         propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntry.CreateLabel("Space Conditioning Requirement");
         ifcPSE.PropertyName = "SpaceConditioningRequirement";
         propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntry.CreateReal("Space Occupant Density");
         ifcPSE.PropertyName = "SpaceOccupantDensity";
         propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntry.CreateReal("Space Occupant Heat Rate");
         ifcPSE.PropertyName = "SpaceOccupantHeatRate";
         propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntry.CreateReal("Space Occupant Load");
         ifcPSE.PropertyName = "SpaceOccupantLoad";
         propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntry.CreateReal("Space Equipment Load");
         ifcPSE.PropertyName = "SpaceEquipmentLoad";
         propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntry.CreateReal("Space Lighting Load");
         ifcPSE.PropertyName = "SpaceLightingLoad";
         propertySetSpaceThermalSimulationProperties.AddEntry(ifcPSE);

         cobiePropertySets.Add(propertySetSpaceThermalSimulationProperties);
      }

      /// <summary>
      /// Initializes COBIE space ventilation criteria property sets.
      /// </summary>
      /// <param name="cobiePropertySets">List to store property sets.</param>
      private static void InitCOBIEPSetSpaceVentilationCriteria(IList<PropertySetDescription> cobiePropertySets)
      {
         PropertySetDescription propertySetSpaceVentilationCriteria = new PropertySetDescription();
         propertySetSpaceVentilationCriteria.Name = "ePset_SpaceVentilationCriteria";
         propertySetSpaceVentilationCriteria.EntityTypes.Add(IFCEntityType.IfcSpace);

         PropertySetEntry ifcPSE = PropertySetEntry.CreateLabel("Ventilation Type");
         ifcPSE.PropertyName = "VentilationType";
         propertySetSpaceVentilationCriteria.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntry.CreateReal("Outside Air Per Person");
         ifcPSE.PropertyName = "OutsideAirPerPerson";
         propertySetSpaceVentilationCriteria.AddEntry(ifcPSE);

         cobiePropertySets.Add(propertySetSpaceVentilationCriteria);
      }

      /// <summary>
      /// Initializes COBIE building energy target property sets.
      /// </summary>
      /// <param name="cobiePropertySets">List to store property sets.</param>
      private static void InitCOBIEPSetBuildingEnergyTarget(IList<PropertySetDescription> cobiePropertySets)
      {
         PropertySetDescription propertySetBuildingEnergyTarget = new PropertySetDescription();
         propertySetBuildingEnergyTarget.Name = "ePset_BuildingEnergyTarget";
         propertySetBuildingEnergyTarget.EntityTypes.Add(IFCEntityType.IfcBuilding);

         PropertySetEntry ifcPSE = PropertySetEntry.CreateReal("Building Energy Target Value");
         ifcPSE.PropertyName = "BuildingEnergyTargetValue";
         propertySetBuildingEnergyTarget.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntry.CreateLabel("Building Energy Target Units");
         ifcPSE.PropertyName = "BuildingEnergyTargetUnits";
         propertySetBuildingEnergyTarget.AddEntry(ifcPSE);

         cobiePropertySets.Add(propertySetBuildingEnergyTarget);
      }

      /// <summary>
      /// Initializes COBIE glazing properties energy analysis property sets.
      /// </summary>
      /// <param name="cobiePropertySets">List to store property sets.</param>
      private static void InitCOBIEPSetGlazingPropertiesEnergyAnalysis(IList<PropertySetDescription> cobiePropertySets)
      {
         PropertySetDescription propertySetGlazingPropertiesEnergyAnalysis = new PropertySetDescription();
         propertySetGlazingPropertiesEnergyAnalysis.Name = "ePset_GlazingPropertiesEnergyAnalysis";
         propertySetGlazingPropertiesEnergyAnalysis.EntityTypes.Add(IFCEntityType.IfcCurtainWall);

         PropertySetEntry ifcPSE = PropertySetEntry.CreateLabel("Windows 6 Glazing System Name");
         ifcPSE.PropertyName = "Windows6GlazingSystemName";
         propertySetGlazingPropertiesEnergyAnalysis.AddEntry(ifcPSE);

         cobiePropertySets.Add(propertySetGlazingPropertiesEnergyAnalysis);
      }

      /// <summary>
      /// Initializes COBIE photo voltaic array property sets.
      /// </summary>
      /// <param name="cobiePropertySets">List to store property sets.</param>
      private static void InitCOBIEPSetPhotovoltaicArray(IList<PropertySetDescription> cobiePropertySets)
      {
         PropertySetDescription propertySetPhotovoltaicArray = new PropertySetDescription();
         propertySetPhotovoltaicArray.Name = "ePset_PhotovoltaicArray";
         propertySetPhotovoltaicArray.EntityTypes.Add(IFCEntityType.IfcRoof);
         propertySetPhotovoltaicArray.EntityTypes.Add(IFCEntityType.IfcWall);
         propertySetPhotovoltaicArray.EntityTypes.Add(IFCEntityType.IfcSlab);

         PropertySetEntry ifcPSE = PropertySetEntry.CreateBoolean("Hosts Photovoltaic Array");
         ifcPSE.PropertyName = "HostsPhotovoltaicArray";
         propertySetPhotovoltaicArray.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntry.CreateReal("Active Area Ratio");
         ifcPSE.PropertyName = "ActiveAreaRatio";
         propertySetPhotovoltaicArray.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntry.CreateReal("DC to AC Conversion Efficiency");
         ifcPSE.PropertyName = "DcToAcConversionEfficiency";
         propertySetPhotovoltaicArray.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntry.CreateLabel("Photovoltaic Surface Integration");
         ifcPSE.PropertyName = "PhotovoltaicSurfaceIntegration";
         propertySetPhotovoltaicArray.AddEntry(ifcPSE);

         ifcPSE = PropertySetEntry.CreateReal("Photovoltaic Cell Efficiency");
         ifcPSE.PropertyName = "PhotovoltaicCellEfficiency";
         propertySetPhotovoltaicArray.AddEntry(ifcPSE);

         cobiePropertySets.Add(propertySetPhotovoltaicArray);
      }
      #endregion

      #region QuantitySets
      // Quantities (including COBie QuantitySets)

      /// <summary>
      /// Initializes COBIE quantities.
      /// </summary>
      /// <param name="quantities">List to store quantities.</param>
      /// <param name="fileVersion">The file version, currently unused.</param>
      private static void InitCOBIEQuantities(IList<IList<QuantityDescription>> quantities)
      {
         IList<QuantityDescription> cobieQuantities = new List<QuantityDescription>();
         InitCOBIESpaceQuantities(cobieQuantities);
         InitCOBIESpaceLevelQuantities(cobieQuantities);
         InitCOBIEPMSpaceQuantities(cobieQuantities);
         quantities.Add(cobieQuantities);
      }

      /// <summary>
      /// Initializes COBIE space quantities.
      /// </summary>
      /// <param name="cobieQuantities">List to store quantities.</param>
      private static void InitCOBIESpaceQuantities(IList<QuantityDescription> cobieQuantities)
      {
         QuantityDescription ifcCOBIEQuantity = new();
         ifcCOBIEQuantity.Name = "BaseQuantities";
         ifcCOBIEQuantity.EntityTypes.Add(IFCEntityType.IfcSpace);

         QuantityEntry ifcQE = new("Height");
         ifcQE.MethodOfMeasurement = "length measured in geometry";
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcQE.PropertyCalculator = HeightCalculator.Instance;
         ifcCOBIEQuantity.AddEntry(ifcQE);

         ifcQE = new("GrossPerimeter");
         ifcQE.MethodOfMeasurement = "length measured in geometry";
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcQE.PropertyCalculator = GrossPerimeterCalculator.Instance;
         ifcCOBIEQuantity.AddEntry(ifcQE);

         ifcQE = new("GrossFloorArea");
         ifcQE.MethodOfMeasurement = "area measured in geometry";
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = AreaCalculator.Instance;
         ifcCOBIEQuantity.AddEntry(ifcQE);

         ifcQE = new("NetFloorArea");
         ifcQE.MethodOfMeasurement = "area measured in geometry";
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = AreaCalculator.Instance;
         ifcCOBIEQuantity.AddEntry(ifcQE);

         ifcQE = new("GrossVolume");
         ifcQE.MethodOfMeasurement = "volume measured in geometry";
         ifcQE.QuantityType = QuantityType.Volume;
         ifcQE.PropertyCalculator = VolumeCalculator.Instance;
         ifcCOBIEQuantity.AddEntry(ifcQE);

         cobieQuantities.Add(ifcCOBIEQuantity);
      }

      /// <summary>
      /// Initializes COBIE space level quantities.
      /// </summary>
      /// <param name="cobieQuantities">List to store quantities.</param>
      private static void InitCOBIESpaceLevelQuantities(IList<QuantityDescription> cobieQuantities)
      {
         QuantityDescription ifcCOBIEQuantity = new QuantityDescription();
         ifcCOBIEQuantity.Name = "BaseQuantities";
         ifcCOBIEQuantity.EntityTypes.Add(IFCEntityType.IfcSpace);
         ifcCOBIEQuantity.DescriptionCalculator = SpaceLevelDescriptionCalculator.Instance;

         QuantityEntry ifcQE = new QuantityEntry("GrossFloorArea");
         ifcQE.MethodOfMeasurement = "area measured in geometry";
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = SpaceLevelAreaCalculator.Instance;
         ifcCOBIEQuantity.AddEntry(ifcQE);

         cobieQuantities.Add(ifcCOBIEQuantity);
      }

      /// <summary>
      /// Initializes COBIE BM space quantities.
      /// </summary>
      /// <param name="cobieQuantities">List to store quantities.</param>
      private static void InitCOBIEPMSpaceQuantities(IList<QuantityDescription> cobieQuantities)
      {
         QuantityDescription ifcCOBIEQuantity = new QuantityDescription();
         ifcCOBIEQuantity.Name = "Space Quantities (Property Management)";
         ifcCOBIEQuantity.MethodOfMeasurement = "As defined by BOMA (see www.boma.org)";
         ifcCOBIEQuantity.EntityTypes.Add(IFCEntityType.IfcSpace);

         QuantityEntry ifcQE = new QuantityEntry("NetFloorArea_BOMA");
         ifcQE.MethodOfMeasurement = "area measured in geometry";
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = AreaCalculator.Instance;
         ifcCOBIEQuantity.AddEntry(ifcQE);

         cobieQuantities.Add(ifcCOBIEQuantity);
      }
#endregion

   }
}
