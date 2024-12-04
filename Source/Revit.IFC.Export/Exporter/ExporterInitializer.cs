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
   partial class ExporterInitializer
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
         propertySetProvisionForVoid.PredefinedType = "USERDEFINED";
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
      /// Initializes property sets.
      /// </summary>
      public static void InitPropertySets()
      {
         ParameterCache cache = ExporterCacheManager.ParameterCache;
         certifiedEntityAndPsetList = ExporterCacheManager.CertifiedEntitiesAndPsetsCache;

         // Some properties, particularly the common properties, apply to both instance
         // and type parameters.  It's actually probably a little more complicated than
         // this, but this preserves current behavioe.
         // TODO: Don't have this extra level which can easily be out of sync and is
         // potentially too generic.
         IList<int> instanceAndTypePsetIndices = new List<int>();
         if (ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportIFCCommon)
         {
            instanceAndTypePsetIndices.Add(cache.PropertySets.Count);
            InitCommonPropertySets(cache.PropertySets);

            instanceAndTypePsetIndices.Add(cache.PropertySets.Count);
            InitExtraCommonPropertySets(cache.PropertySets);
            
            InitPreDefinedPropertySets(cache.PreDefinedPropertySets);
         }

         if (ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportSchedulesAsPsets)
         {
            InitCustomPropertySets(cache.PropertySets);
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
      /// Initialize user-defined property sets (from external file)
      /// </summary>
      /// <param name="propertySets">List of Psets</param>
      /// <param name="fileVersion">file version - (not used)</param>
      private static void InitUserDefinedPropertySets(IList<IList<PropertySetDescription>> propertySets)
      {
         Document document = ExporterCacheManager.Document;
         IList<PropertySetDescription> userDefinedPropertySets = new List<PropertySetDescription>();
         IList<QuantityDescription> quantityDescriptions = new List<QuantityDescription>();

         // get the Pset definitions (using the same file as PropertyMap)
         IEnumerable<UserDefinedPropertySet> userDefinedPsetDefs = PropertyMap.LoadUserDefinedPset();
         bool exportPre4 = (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 || ExporterCacheManager.ExportOptionsCache.ExportAs2x3);

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
                  QuantityEntry quantityEntry = new QuantityEntry(property.Name, entryMap) { QuantityType = quantityType };
                  quantityDescription.AddEntry(quantityEntry);
               }
            }
            else
            {
               PropertySetDescription userDefinedPropertySet = new PropertySetDescription();
               userDefinedPropertySet.AddTypePropertiesToInstance = ExporterCacheManager.ExportOptionsCache.PropertySetOptions.UseTypePropertiesInInstacePSets;
               description = userDefinedPropertySet;
               foreach(UserDefinedProperty property in propertySet.Properties)
               {
                  PropertyType primaryType = property.FirstIfcPropertyTypeOrDefault(PropertyType.Text); // force default to Text/string if the type does not match with any correct datatype
                  PropertyType secondaryType = property.GetIfcPropertyAtOrDefault(1, PropertyType.Text);
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

            foreach (string elem in propertySet.IfcEntities)
            {
               if (Enum.TryParse(elem, out IFCEntityType ifcEntity))
               {
                  bool usedCompatibleType = false;

                  if (exportPre4)
                  {
                     IFCEntityType originalEntity = ifcEntity;
                     IFCCompatibilityType.CheckCompatibleType(originalEntity, out ifcEntity);
                     usedCompatibleType = (originalEntity != ifcEntity);
                  }

                  description.EntityTypes.Add(ifcEntity);

                  // This is intended mostly as a workaround in IFC2x3 for IfcElementType.  Not all elements have an associated type (e.g. IfcRoof),
                  // but we still want to be able to export type property sets for that element.  So we will manually add these extra types here without
                  // forcing the user to guess.  If this causes issues, we may come up with a different design.
                  if (!usedCompatibleType)
                  {
                     ISet<IFCEntityType> relatedEntities = GetListOfRelatedEntities(ifcEntity);
                     if (relatedEntities != null)
                     {
                        description.EntityTypes.UnionWith(relatedEntities);
                     }
                  }
               }
            }

         }

         propertySets.Add(userDefinedPropertySets);
         if (quantityDescriptions.Count > 0)
            ExporterCacheManager.ParameterCache.Quantities.Add(quantityDescriptions);
      }

      private static bool IsSupportedFieldType(ScheduleFieldType fieldType)
      {
         return (fieldType == ScheduleFieldType.Instance ||
            fieldType == ScheduleFieldType.ElementType ||
            fieldType == ScheduleFieldType.CombinedParameter);
      }

      /// <summary>
      /// Initializes custom property sets from schedules.
      /// </summary>
      /// <param name="propertySets">List to store property sets.</param>
      /// <param name="fileVersion">The IFC file version.</param>
      private static void InitCustomPropertySets(IList<IList<PropertySetDescription>> propertySets)
      {
         Document document = ExporterCacheManager.Document;
         IList<PropertySetDescription> customPropertySets = new List<PropertySetDescription>();

         // Collect all ViewSchedules from the document to use as custom property sets.
         FilteredElementCollector viewScheduleElementCollector = new FilteredElementCollector(document);

         ElementFilter viewScheduleElementFilter = new ElementClassFilter(typeof(ViewSchedule));
         viewScheduleElementCollector.WherePasses(viewScheduleElementFilter);
         List<ViewSchedule> filteredSchedules = viewScheduleElementCollector.Cast<ViewSchedule>().ToList();

         int unnamedScheduleIndex = 1;

         string includePattern = "PSET|IFC|COMMON";

         if (ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportSpecificSchedules)
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

            PropertySetDescription customPSet = new PropertySetDescription();

            string scheduleName = NamingUtil.GetNameOverride(schedule, schedule.Name);
            if (string.IsNullOrWhiteSpace(scheduleName))
            {
               scheduleName = "Unnamed Schedule " + unnamedScheduleIndex;
               unnamedScheduleIndex++;
            }
            customPSet.Name = scheduleName;

            ScheduleDefinition definition = schedule.Definition;
            if (definition == null)
               continue;

            // The schedule will be responsible for determining which elements to actually export.
            customPSet.ViewScheduleId = schedule.Id;
            customPSet.EntityTypes.Add(IFCEntityType.IfcProduct);

            int fieldCount = definition.GetFieldCount();
            if (fieldCount == 0)
               continue;

            HashSet<ElementId> containedElementIds = new HashSet<ElementId>();
            FilteredElementCollector elementsInViewScheduleCollector = new FilteredElementCollector(document, schedule.Id);
            foreach (Element containedElement in elementsInViewScheduleCollector)
            {
               containedElementIds.Add(containedElement.Id);
               ElementId typeId = containedElement.GetTypeId();
               if (typeId != ElementId.InvalidElementId)
                  containedElementIds.Add(typeId);
            }
            ExporterCacheManager.ViewScheduleElementCache.Add(new KeyValuePair<ElementId, HashSet<ElementId>>(schedule.Id, containedElementIds));

            IDictionary<ElementId, Element> cachedElementTypes = new Dictionary<ElementId, Element>();

            for (int ii = 0; ii < fieldCount; ii++)
            {
               ScheduleField field = definition.GetField(ii);

               ScheduleFieldType fieldType = field.FieldType;
               if (field.IsHidden || !IsSupportedFieldType(fieldType))
                  continue;

               // Check if it is a combined parameter.  If so, calculate the formula later 
               // as necessary.
               PropertySetEntry ifcPSE = null;

               switch (fieldType)
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
                            ParameterUtils.IsBuiltInParameter(parameterId) ? asBuiltInParameterId : BuiltInParameter.INVALID;

                        // We could cache the actual elements when we store the element ids.  However,
                        // this would almost certainly take more time than getting one of the first
                        // few elements in the collector.
                        foreach (Element containedElement in elementsInViewScheduleCollector)
                        {
                           Parameter containedElementParameter = null;

                           if (fieldType == ScheduleFieldType.Instance)
                              containedElementParameter = containedElement.get_Parameter(asBuiltInParameterId);

                           // shared parameters can return ScheduleFieldType.Instance, even if they are type parameters, so take a look.
                           if (containedElementParameter == null)
                           {
                              ElementId containedElementTypeId = containedElement.GetTypeId();
                              Element containedElementType = null;
                              if (containedElementTypeId != ElementId.InvalidElementId)
                              {
                                 if (!cachedElementTypes.TryGetValue(containedElementTypeId, out containedElementType))
                                 {
                                    containedElementType = document.GetElement(containedElementTypeId);
                                    cachedElementTypes[containedElementTypeId] = containedElementType;
                                 }
                              }

                              if (containedElementType != null)
                                 containedElementParameter = containedElementType.get_Parameter(asBuiltInParameterId);
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
                  ifcPSE.PropertyName = field.ColumnHeading;
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
         QuantityDescription ifcCOBIEQuantity = new QuantityDescription();
         ifcCOBIEQuantity.Name = "BaseQuantities";
         ifcCOBIEQuantity.EntityTypes.Add(IFCEntityType.IfcSpace);

         QuantityEntry ifcQE = new QuantityEntry("Height");
         ifcQE.MethodOfMeasurement = "length measured in geometry";
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcQE.PropertyCalculator = HeightCalculator.Instance;
         ifcCOBIEQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossPerimeter");
         ifcQE.MethodOfMeasurement = "length measured in geometry";
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcQE.PropertyCalculator = PerimeterCalculator.Instance;
         ifcCOBIEQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossFloorArea");
         ifcQE.MethodOfMeasurement = "area measured in geometry";
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = AreaCalculator.Instance;
         ifcCOBIEQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetFloorArea");
         ifcQE.MethodOfMeasurement = "area measured in geometry";
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = AreaCalculator.Instance;
         ifcCOBIEQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossVolume");
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
