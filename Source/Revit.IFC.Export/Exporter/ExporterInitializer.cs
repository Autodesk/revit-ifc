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
using GeometryGym.Ifc;

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
         propertySetProvisionForVoid.ObjectType = "ProvisionForVoid";

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
      /// <param name="propertySetsToExport">Existing functions to call for property set initialization.</param>
      public static void InitPropertySets(Exporter.PropertySetsToExport propertySetsToExport)
      {
         ParameterCache cache = ExporterCacheManager.ParameterCache;
         certifiedEntityAndPsetList = ExporterCacheManager.CertifiedEntitiesAndPsetsCache;

         if (ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportIFCCommon)
         {
            if (propertySetsToExport == null)
               propertySetsToExport = InitCommonPropertySets;
            else
               propertySetsToExport += InitCommonPropertySets;

            propertySetsToExport += InitExtraCommonPropertySets;
         }

         if (ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportSchedulesAsPsets)
         {
            if (propertySetsToExport == null)
               propertySetsToExport = InitCustomPropertySets;
            else
               propertySetsToExport += InitCustomPropertySets;
         }

         if (ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportUserDefinedPsets)
         {
            if (propertySetsToExport == null)
               propertySetsToExport = InitUserDefinedPropertySets;
            else
               propertySetsToExport += InitUserDefinedPropertySets;
         }

         if (ExporterCacheManager.ExportOptionsCache.ExportAsCOBIE)
         {
            if (propertySetsToExport == null)
               propertySetsToExport = InitCOBIEPropertySets;
            else
               propertySetsToExport += InitCOBIEPropertySets;
         }

         propertySetsToExport?.Invoke(cache.PropertySets);
      }

      /// <summary>
      /// Initializes predefined property sets.
      /// </summary>
      /// <param name="propertySetsToExport">Existing functions to call for property set initialization.</param>
      public static void InitPreDefinedPropertySets(Exporter.PreDefinedPropertySetsToExport propertySetsToExport)
      {
         ParameterCache cache = ExporterCacheManager.ParameterCache;

         if (ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportIFCCommon)
         {
            if (propertySetsToExport == null)
               propertySetsToExport = InitPreDefinedPropertySets;
            else
               propertySetsToExport += InitPreDefinedPropertySets;
         }

         propertySetsToExport?.Invoke(cache.PreDefinedPropertySets);
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
            if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
            {
               if (quantitiesToExport == null)
                  quantitiesToExport = InitBaseQuantities;
               else
                  quantitiesToExport += InitBaseQuantities;
            }
            else
            {
               if (quantitiesToExport == null)
                  quantitiesToExport = InitQtoSets;
               else
                  quantitiesToExport += InitQtoSets;
            }
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
         IEnumerable<IfcPropertySetTemplate> userDefinedPsetDefs = PropertyMap.LoadUserDefinedPset();

         bool exportPre4 = (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 || ExporterCacheManager.ExportOptionsCache.ExportAs2x3);

         // Loop through each definition and add the Pset entries into Cache
         foreach (IfcPropertySetTemplate psetDef in userDefinedPsetDefs)
         {
            // Add Propertyset entry
            Description description = null;
            BuiltInParameter builtInParameter = BuiltInParameter.INVALID;
            if (string.Compare(psetDef.Name, "Attribute Mapping", true) == 0)
            {
               AttributeSetDescription attributeDescription = new AttributeSetDescription();
               ExporterCacheManager.AttributeCache.AddAttributeSet(attributeDescription);
               foreach (IfcPropertyTemplate prop in psetDef.HasPropertyTemplates.Values)
               {
                  IfcSimplePropertyTemplate template = prop as IfcSimplePropertyTemplate;
                  if (template != null)
                  {
                     PropertyType dataType;
                     if (!Enum.TryParse(template.PrimaryMeasureType.ToLower().Replace("ifc", ""), true, out dataType))
                     {
                        dataType = PropertyType.Text;
                     }
                     List<AttributeEntryMap> mappings = new List<AttributeEntryMap>();
                     foreach (IfcRelAssociates associates in template.HasAssociations)
                     {
                        IfcRelAssociatesClassification associatesClassification = associates as IfcRelAssociatesClassification;
                        if (associatesClassification != null)
                        {
                           IfcClassificationReference classificationReference = associatesClassification.RelatingClassification as IfcClassificationReference;
                           if (classificationReference != null)
                           {
                              string id = classificationReference.Identification;
                              if (id.ToLower().StartsWith("builtinparameter."))
                              {
                                 id = id.Substring("BuiltInParameter.".Length);
                                 if (Enum.TryParse<Autodesk.Revit.DB.BuiltInParameter>(id, out builtInParameter) && builtInParameter != Autodesk.Revit.DB.BuiltInParameter.INVALID)
                                 {
                                    mappings.Add(new AttributeEntryMap(template.Name, builtInParameter));
                                 }
                                 else
                                 {
                                    // report as error in log when we create log file.
                                 }
                              }
                              else
                                 mappings.Add(new AttributeEntryMap(id, BuiltInParameter.INVALID));
                           }
                        }
                     }

                     AttributeEntry aSE = new AttributeEntry(template.Name, dataType, mappings);
                     attributeDescription.AddEntry(aSE);
                  }
               }
               description = attributeDescription;

            }
            else if (psetDef.TemplateType == IfcPropertySetTemplateTypeEnum.QTO_OCCURRENCEDRIVEN || psetDef.TemplateType == IfcPropertySetTemplateTypeEnum.QTO_TYPEDRIVENONLY || psetDef.TemplateType == IfcPropertySetTemplateTypeEnum.QTO_TYPEDRIVENOVERRIDE)
            {
               QuantityDescription quantityDescription = new QuantityDescription();
               quantityDescriptions.Add(quantityDescription);
               description = quantityDescription;
               foreach (IfcPropertyTemplate prop in psetDef.HasPropertyTemplates.Values)
               {
                  IfcSimplePropertyTemplate template = prop as IfcSimplePropertyTemplate;
                  if (template != null)
                  {
                     List<QuantityEntryMap> mappings = new List<QuantityEntryMap>();
                     foreach (IfcRelAssociates associates in template.HasAssociations)
                     {
                        IfcRelAssociatesClassification associatesClassification = associates as IfcRelAssociatesClassification;
                        if (associatesClassification != null)
                        {
                           IfcClassificationReference classificationReference = associatesClassification.RelatingClassification as IfcClassificationReference;
                           if (classificationReference != null)
                           {
                              string id = classificationReference.Identification;
                              if (id.ToLower().StartsWith("builtinparameter."))
                              {
                                 id = id.Substring("BuiltInParameter.".Length);
                                 if (Enum.TryParse<Autodesk.Revit.DB.BuiltInParameter>(id, out builtInParameter) && builtInParameter != Autodesk.Revit.DB.BuiltInParameter.INVALID)
                                 {
                                    mappings.Add(new QuantityEntryMap(template.Name, builtInParameter));
                                 }
                                 else
                                 {
                                    // report as error in log when we create log file.
                                 }
                              }
                              else
                                 mappings.Add(new QuantityEntryMap(id, BuiltInParameter.INVALID));
                           }
                        }
                     }
                     QuantityType quantityType = QuantityType.Real;
                     switch (template.TemplateType)
                     {
                        case IfcSimplePropertyTemplateTypeEnum.Q_AREA:
                           quantityType = QuantityType.Area;
                           break;
                        case IfcSimplePropertyTemplateTypeEnum.Q_LENGTH:
                           quantityType = QuantityType.PositiveLength;
                           break;
                        case IfcSimplePropertyTemplateTypeEnum.Q_VOLUME:
                           quantityType = QuantityType.Volume;
                           break;
                        case IfcSimplePropertyTemplateTypeEnum.Q_WEIGHT:
                           quantityType = QuantityType.Weight;
                           break;
                        default:
                           quantityType = QuantityType.Real;
                           break;
                     }
                     QuantityEntry quantityEntry = new QuantityEntry(prop.Name, mappings) { QuantityType = quantityType };
                     quantityDescription.AddEntry(quantityEntry);
                  }
               }
            }
            else
            {
               PropertySetDescription userDefinedPropertySet = new PropertySetDescription();
               description = userDefinedPropertySet;
               foreach (IfcPropertyTemplate prop in psetDef.HasPropertyTemplates.Values)
               {
                  IfcSimplePropertyTemplate template = prop as IfcSimplePropertyTemplate;
                  if (template != null)
                  {
                     IfcValue defaultValue = null;
                     PropertyType dataType;
                     if (!Enum.TryParse(template.PrimaryMeasureType.ToLower().Replace("ifc", ""), true, out dataType))
                     {
                        dataType = PropertyType.Text;           // force default to Text/string if the type does not match with any correct datatype
                     }
                     List<PropertySetEntryMap> mappings = new List<PropertySetEntryMap>();
                     foreach (IfcRelAssociates associates in template.HasAssociations)
                     {
                        IfcRelAssociatesClassification associatesClassification = associates as IfcRelAssociatesClassification;
                        if (associatesClassification != null)
                        {
                           IfcClassificationReference classificationReference = associatesClassification.RelatingClassification as IfcClassificationReference;
                           if (classificationReference != null)
                           {
                              string id = classificationReference.Identification;
                              if (id.ToLower().StartsWith("builtinparameter."))
                              {
                                 id = id.Substring("BuiltInParameter.".Length);
                                 if (Enum.TryParse<Autodesk.Revit.DB.BuiltInParameter>(id, out builtInParameter) && builtInParameter != Autodesk.Revit.DB.BuiltInParameter.INVALID)
                                 {
                                    mappings.Add(new PropertySetEntryMap(template.Name, builtInParameter));
                                 }
                                 else
                                 {
                                    // report as error in log when we create log file.
                                 }
                              }
                              else
                                 mappings.Add(new PropertySetEntryMap(id, BuiltInParameter.INVALID));
                           }
                        }
                        else
                        {
                           IfcRelAssociatesConstraint associatesConstraint = associates as IfcRelAssociatesConstraint;
                           if (associatesConstraint != null)
                           {
                              IfcMetric metric = associatesConstraint.RelatingConstraint as IfcMetric;
                              if (metric != null)
                              {
                                 defaultValue = metric.DataValue as IfcValue;
                              }
                           }
                        }
                     }
                     if (mappings.Count > 0)
                     {
                        PropertySetEntry pSE = new PropertySetEntry(dataType, prop.Name, mappings);
                        pSE.DefaultValue = defaultValue;
                        userDefinedPropertySet.AddEntry(pSE);
                     }
                     else
                     {
                        PropertySetEntry pSE = new PropertySetEntry(prop.Name);
                        pSE.PropertyName = prop.Name;
                        pSE.PropertyType = dataType;
                        pSE.DefaultValue = defaultValue;
                        userDefinedPropertySet.AddEntry(pSE);
                     }
                  }
               }
               userDefinedPropertySets.Add(userDefinedPropertySet);
            }
            description.Name = psetDef.Name;
            description.DescriptionOfSet = psetDef.Description;

            string[] applicableElements = psetDef.ApplicableEntity.Split(",".ToCharArray());
            foreach (string elem in applicableElements)
            {
               Common.Enums.IFCEntityType ifcEntity;
               if (Enum.TryParse(elem, out ifcEntity))
               {
                  if (exportPre4)
                  {
                     IFCEntityType originalEntity = ifcEntity;
                     IFCCompatibilityType.checkCompatibleType(originalEntity, out ifcEntity);
                  }

                  description.EntityTypes.Add(ifcEntity);
                  // This is intended mostly as a workaround in IFC2x3 for IfcElementType.  Not all elements have an associated type (e.g. IfcRoof),
                  // but we still want to be able to export type property sets for that element.  So we will manually add these extra types here without
                  // forcing the user to guess.  If this causes issues, we may come up with a different design.
                  ISet<IFCEntityType> relatedEntities = GetListOfRelatedEntities(ifcEntity);
                  if (relatedEntities != null)
                     description.EntityTypes.UnionWith(relatedEntities);
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
            // Since 2018, schedules can have shared parameters.  Allow schedules to be skipped if IfcExportAs is set to DontExport.
            if (ElementFilteringUtil.IsIFCExportAsSetToDontExport(schedule, out _))
               continue;

            // ViewSchedule may be a template view and it will not have the associated view and elements. Skip this type of schedule
            if (schedule.IsTemplate)
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
                        BuiltInParameter asBuiltInParameterId = (BuiltInParameter)parameterId.IntegerValue;
                        BuiltInParameter builtInParameterId =
                            Enum.IsDefined(typeof(BuiltInParameter), asBuiltInParameterId) ? asBuiltInParameterId : BuiltInParameter.INVALID;

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
      /// Initializes railing base quantities.
      /// </summary>
      /// <param name="baseQuantities">List to store quantities.</param>
      private static void InitRailingBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcRailingQuantity = new QuantityDescription("Railing", IFCEntityType.IfcRailing);
         
         ifcRailingQuantity.AddEntry("Length", BuiltInParameter.CURVE_ELEM_LENGTH, QuantityType.PositiveLength, null);
         
         baseQuantities.Add(ifcRailingQuantity);
      }

      /// <summary>
      /// Initializes slab base quantities.
      /// </summary>
      /// <param name="baseQuantities">List to store quantities.</param>
      private static void InitSlabBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcSlabQuantity = new QuantityDescription("Slab", IFCEntityType.IfcSlab);

         AddCommonAreaQuantities(ifcSlabQuantity);
         AddCommonVolumeQuantities(ifcSlabQuantity);
         AddCommonWeightQuantities(ifcSlabQuantity);

         ifcSlabQuantity.AddEntry("Perimeter", QuantityType.PositiveLength, PerimeterCalculator.Instance);
         ifcSlabQuantity.AddEntry("Width", QuantityType.PositiveLength, WidthCalculator.Instance);
         
         baseQuantities.Add(ifcSlabQuantity);
      }

      /// <summary>
      /// Initializes ramp flight base quantities.
      /// </summary>
      /// <param name="baseQuantities">List to store quantities.</param>
      private static void InitRampFlightBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBaseQuantity = new QuantityDescription("RampFlight", IFCEntityType.IfcRampFlight);

         ifcBaseQuantity.AddEntry("Width", BuiltInParameter.STAIRS_ATTR_TREAD_WIDTH, QuantityType.PositiveLength, null);

         baseQuantities.Add(ifcBaseQuantity);
      }

      /// <summary>
      /// Initializes Stairflight base quantity
      /// </summary>
      /// <param name="baseQuantities">List to store quantities.</param>
      private static void InitStairFlightBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBaseQuantity = new QuantityDescription("StairFlight", IFCEntityType.IfcStairFlight);

         AddCommonVolumeQuantities(ifcBaseQuantity);
         ifcBaseQuantity.AddEntry("Length", QuantityType.PositiveLength, LengthCalculator.Instance);

         baseQuantities.Add(ifcBaseQuantity);
      }

      /// <summary>
      /// Initializes Building Storey base quantity
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitBuildingStoreyBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBaseQuantity = new QuantityDescription("BuildingStorey", IFCEntityType.IfcBuildingStorey);

         ifcBaseQuantity.AddEntry("NetHeight", "IfcQtyNetHeight", QuantityType.PositiveLength, null);
         ifcBaseQuantity.AddEntry("GrossHeight", "IfcQtyGrossHeight", QuantityType.PositiveLength, null);

         ExportOptionsCache exportOptionsCache = ExporterCacheManager.ExportOptionsCache;
         if (!ExporterCacheManager.ExportOptionsCache.ExportAs2x3COBIE24DesignDeliverable)   // FMHandOver view exclude NetArea, GrossArea, NetVolume and GrossVolumne
         {
            AddCommonVolumeQuantities(ifcBaseQuantity);
            ifcBaseQuantity.AddEntry("NetFloorArea", QuantityType.Area, SpaceLevelAreaCalculator.Instance);
            ifcBaseQuantity.AddEntry("GrossFloorArea", QuantityType.Area, SpaceLevelAreaCalculator.Instance);
            ifcBaseQuantity.AddEntry("GrossPerimeter", "IfcQtyGrossPerimeter", QuantityType.PositiveLength, null);
         }

         baseQuantities.Add(ifcBaseQuantity);
      }

      /// <summary>
      /// Initializes Space base quantity
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitSpaceBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBaseQuantity = new QuantityDescription("Space", IFCEntityType.IfcSpace);

         QuantityEntry ifcQE = ifcBaseQuantity.AddEntry("NetFloorArea", QuantityType.Area, AreaCalculator.Instance);
         ifcQE.MethodOfMeasurement = "area measured in geometry";

         ifcBaseQuantity.AddEntry("FinishCeilingHeight", "IfcQtyFinishCeilingHeight", QuantityType.PositiveLength, null);
         ifcBaseQuantity.AddEntry("NetCeilingArea", "IfcQtyNetCeilingArea", QuantityType.Area, null);
         ifcBaseQuantity.AddEntry("GrossCeilingArea", "IfcQtyGrossCeilingArea", QuantityType.Area, null);
         ifcBaseQuantity.AddEntry("NetWallArea", "IfcQtyNetWallArea", QuantityType.Area, null);
         ifcBaseQuantity.AddEntry("GrossWallArea", "IfcQtyGrossWallArea", QuantityType.Area, null);
    
         ifcQE = ifcBaseQuantity.AddEntry("Height", QuantityType.PositiveLength, HeightCalculator.Instance);
         ifcQE.MethodOfMeasurement = "length measured in geometry";

         ifcQE = ifcBaseQuantity.AddEntry("NetPerimeter", "IfcQtyNetPerimeter", QuantityType.PositiveLength, null);
         ifcQE.MethodOfMeasurement = "length measured in geometry";
         
         ifcQE = ifcBaseQuantity.AddEntry("GrossPerimeter", QuantityType.PositiveLength, PerimeterCalculator.Instance);
         ifcQE.MethodOfMeasurement = "length measured in geometry";
         
         ifcQE = ifcBaseQuantity.AddEntry("GrossFloorArea", QuantityType.Area, AreaCalculator.Instance);
         ifcQE.MethodOfMeasurement = "area measured in geometry";
         
         ExportOptionsCache exportOptionsCache = ExporterCacheManager.ExportOptionsCache;
         if (!ExporterCacheManager.ExportOptionsCache.ExportAs2x3COBIE24DesignDeliverable)   // FMHandOver view exclude GrossVolumne, FinishFloorHeight
         {
            ifcQE = ifcBaseQuantity.AddEntry("GrossVolume", QuantityType.Volume, VolumeCalculator.Instance);
            ifcQE.MethodOfMeasurement = "volume measured in geometry";

            ifcBaseQuantity.AddEntry("FinishFloorHeight", "IfcQtyFinishFloorHeight", QuantityType.PositiveLength, null);
         }

         baseQuantities.Add(ifcBaseQuantity);
      }

      /// <summary>
      /// Initializes Covering base quantity
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitCoveringBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBaseQuantity = new QuantityDescription("Covering", IFCEntityType.IfcCovering);

         if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
         {
            ifcBaseQuantity.AddEntry("GrossCeilingArea", BuiltInParameter.HOST_AREA_COMPUTED, QuantityType.Area, null);
         }
         else
         {
            AddCommonAreaQuantities(ifcBaseQuantity);
            ifcBaseQuantity.AddEntry("Width", QuantityType.PositiveLength, WidthCalculator.Instance);
         }

         baseQuantities.Add(ifcBaseQuantity);
      }

      private static void AddDoorWindowBaseQuantities(QuantityDescription ifcBaseQuantity,
         BuiltInParameter heightParameter, BuiltInParameter widthParameter)
      {
         ifcBaseQuantity.AddEntry("Height", heightParameter, QuantityType.PositiveLength, null);
         ifcBaseQuantity.AddEntry("Width", widthParameter, QuantityType.PositiveLength, null);
         
         QuantityEntry ifcQE = ifcBaseQuantity.AddEntry("Area", QuantityType.Area, AreaCalculator.Instance, true);
         ifcQE.MethodOfMeasurement = "area measured in geometry";
      }

      /// <summary>
      /// Initializes Window base quantity
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitWindowBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBaseQuantity = new QuantityDescription("Window", IFCEntityType.IfcWindow);

         AddDoorWindowBaseQuantities(ifcBaseQuantity, BuiltInParameter.WINDOW_HEIGHT, BuiltInParameter.WINDOW_WIDTH);
      
         baseQuantities.Add(ifcBaseQuantity);
      }

      /// <summary>
      /// Initializes Door base quantity
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitDoorBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBaseQuantity = new QuantityDescription("Door", IFCEntityType.IfcDoor);

         AddDoorWindowBaseQuantities(ifcBaseQuantity, BuiltInParameter.DOOR_HEIGHT, BuiltInParameter.DOOR_WIDTH);
         
         baseQuantities.Add(ifcBaseQuantity);
      }

      private static void AddCommonVolumeQuantities(QuantityDescription ifcQuantityDescription)
      {
         ifcQuantityDescription.AddEntry("GrossVolume", "IfcQtyGrossVolume", QuantityType.Volume, GrossVolumeCalculator.Instance);
         ifcQuantityDescription.AddEntry("NetVolume", "IfcQtyNetVolume", QuantityType.Volume, NetVolumeCalculator.Instance);
      }

      private static void AddCommonAreaQuantities(QuantityDescription ifcQuantityDescription)
      {
         ifcQuantityDescription.AddEntry("GrossArea", "IfcQtyGrossArea", QuantityType.Area, GrossAreaCalculator.Instance);
         ifcQuantityDescription.AddEntry("NetArea", BuiltInParameter.HOST_AREA_COMPUTED, QuantityType.Area, NetSurfaceAreaCalculator.Instance);
      }

      private static void AddCommonWeightQuantities(QuantityDescription ifcQuantityDescription)
      {
         ifcQuantityDescription.AddEntry("GrossWeight", QuantityType.Weight, GrossWeightCalculator.Instance);
         ifcQuantityDescription.AddEntry("NetWeight", QuantityType.Weight, NetWeightCalculator.Instance);
      }
         
      private static void AddCommonStructuralBaseQuantities(QuantityDescription ifcQuantityDescription)
      {
         ifcQuantityDescription.AddEntry("Length", QuantityType.PositiveLength, LengthCalculator.Instance);
         ifcQuantityDescription.AddEntry("CrossSectionArea", QuantityType.Area, CrossSectionAreaCalculator.Instance);
         ifcQuantityDescription.AddEntry("OuterSurfaceArea", QuantityType.Area, OuterSurfaceAreaCalculator.Instance);
         ifcQuantityDescription.AddEntry("GrossSurfaceArea", QuantityType.Area, GrossSurfaceAreaCalculator.Instance);
         AddCommonVolumeQuantities(ifcQuantityDescription);
         AddCommonWeightQuantities(ifcQuantityDescription);
      }

      /// <summary>
      /// Initialize Beam Base Quantities
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitBeamBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBeamQuantity = new QuantityDescription("Beam", IFCEntityType.IfcBeam);

         AddCommonStructuralBaseQuantities(ifcBeamQuantity);
         ifcBeamQuantity.AddEntry("NetSurfaceArea", QuantityType.Area, NetSurfaceAreaCalculator.Instance);

         baseQuantities.Add(ifcBeamQuantity);
      }

      /// <summary>
      /// Initialize Member Base Quantities
      /// </summary>
      /// <param name="baseQuantities">The list of all of the quantity sets.</param>
      private static void InitMemberBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcMemberQuantity = new QuantityDescription("Member", IFCEntityType.IfcMember);

         AddCommonStructuralBaseQuantities(ifcMemberQuantity);
         ifcMemberQuantity.AddEntry("NetSurfaceArea", QuantityType.Area, NetSurfaceAreaCalculator.Instance);

         baseQuantities.Add(ifcMemberQuantity);
      }

      /// <summary>
      /// Initialize Pile Base Quantities
      /// </summary>
      /// <param name="baseQuantities">The list of all of the quantity sets.</param>
      private static void InitPileBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcPileQuantity = new QuantityDescription("Pile", IFCEntityType.IfcPile);

         AddCommonStructuralBaseQuantities(ifcPileQuantity);

         baseQuantities.Add(ifcPileQuantity);
      }

      /// <summary>
      /// Initialize Plate Base Quantities
      /// </summary>
      /// <param name="baseQuantities">The list of all of the quantity sets.</param>
      private static void InitPlateBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcPlateQuantity = new QuantityDescription("Plate", IFCEntityType.IfcPlate);

         AddCommonVolumeQuantities(ifcPlateQuantity);

         baseQuantities.Add(ifcPlateQuantity);
      }

      /// <summary>
      /// Init Duct Fitting Quantities
      /// </summary>
      /// <param name="baseQuantities">The list of all of the quantity sets.</param>
      private static void InitDuctFittingBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcDuctFittingQuantities = new QuantityDescription("DuctFitting", IFCEntityType.IfcDuctFitting);
         ifcDuctFittingQuantities.AddEntry("Length", QuantityType.PositiveLength, LengthCalculator.Instance);
         ifcDuctFittingQuantities.AddEntry("GrossCrossSectionArea", QuantityType.Area, GrossCrossSectionAreaCalculator.Instance);
         ifcDuctFittingQuantities.AddEntry("NetCrossSectionArea", QuantityType.Area, NetCrossSectionAreaCalculator.Instance);
         ifcDuctFittingQuantities.AddEntry("OuterSurfaceArea", QuantityType.Area, OuterSurfaceAreaCalculator.Instance);
         ifcDuctFittingQuantities.AddEntry("GrossWeight", QuantityType.Weight, GrossWeightCalculator.Instance);

         baseQuantities.Add(ifcDuctFittingQuantities);
      }

      private static void InitElectricApplianceBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcElectricApplianceQuantities = new QuantityDescription("ElectricAppliance", IFCEntityType.IfcElectricAppliance);
         ifcElectricApplianceQuantities.AddEntry("GrossWeight", QuantityType.Weight, GrossWeightCalculator.Instance);

         baseQuantities.Add(ifcElectricApplianceQuantities);
      }

      private static void InitCableCarrierFittingBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcCableCarrierFittingQuantities = new QuantityDescription("CableCarrierFitting", IFCEntityType.IfcCableCarrierFitting);
         ifcCableCarrierFittingQuantities.AddEntry("GrossWeight", QuantityType.Weight, GrossWeightCalculator.Instance);

         baseQuantities.Add(ifcCableCarrierFittingQuantities);
      }

      private static void InitCableCarrierSegmentQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcCableCarrierSegmentQuantities = new QuantityDescription("CableCarrierSegment", IFCEntityType.IfcCableCarrierSegment);
         ifcCableCarrierSegmentQuantities.AddEntry("Length", QuantityType.PositiveLength, LengthCalculator.Instance);
         ifcCableCarrierSegmentQuantities.AddEntry("CrossSectionArea", QuantityType.Area, CrossSectionAreaCalculator.Instance);
         ifcCableCarrierSegmentQuantities.AddEntry("OuterSurfaceArea", QuantityType.Area, OuterSurfaceAreaCalculator.Instance);
         ifcCableCarrierSegmentQuantities.AddEntry("GrossWeight", QuantityType.Weight, GrossWeightCalculator.Instance);

         baseQuantities.Add(ifcCableCarrierSegmentQuantities);
      }

      private static void InitCableSegmentQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcCableSegmentQuantities = new QuantityDescription("CableSegment", IFCEntityType.IfcCableSegment);
         ifcCableSegmentQuantities.AddEntry("Length", QuantityType.PositiveLength, LengthCalculator.Instance);
         ifcCableSegmentQuantities.AddEntry("CrossSectionArea", QuantityType.Area, CrossSectionAreaCalculator.Instance);
         ifcCableSegmentQuantities.AddEntry("OuterSurfaceArea", QuantityType.Area, OuterSurfaceAreaCalculator.Instance);
         ifcCableSegmentQuantities.AddEntry("GrossWeight", QuantityType.Weight, GrossWeightCalculator.Instance);

         baseQuantities.Add(ifcCableSegmentQuantities);
      }

      /// <summary>
      /// Initializes base quantities.
      /// </summary>
      /// <param name="quantities">List to store quantities.</param>
      /// <param name="fileVersion">The file version, currently unused.</param>
      private static void InitBaseQuantities(IList<IList<QuantityDescription>> quantities)
      {
         IList<QuantityDescription> baseQuantities = new List<QuantityDescription>();

         // TODO: understand why beams seem to be handled twice - here and CreateBeamColumnBaseQuantities
         InitBeamBaseQuantities(baseQuantities);
         InitBuildingStoreyBaseQuantities(baseQuantities);
         //InitColumnBaseQuantities is handled by CreateBeamColumnBaseQuantities
         InitCoveringBaseQuantities(baseQuantities);
         InitDoorBaseQuantities(baseQuantities);
         InitMemberBaseQuantities(baseQuantities);
         InitRailingBaseQuantities(baseQuantities);
         InitPileBaseQuantities(baseQuantities);
         InitPlateBaseQuantities(baseQuantities);
         InitRampFlightBaseQuantities(baseQuantities);
         InitSlabBaseQuantities(baseQuantities);
         InitSpaceBaseQuantities(baseQuantities);
         InitStairFlightBaseQuantities(baseQuantities);
         InitWindowBaseQuantities(baseQuantities);

         // MEP Quantities
         InitDuctFittingBaseQuantities(baseQuantities);
         InitElectricApplianceBaseQuantities(baseQuantities);
         InitCableCarrierFittingBaseQuantities(baseQuantities);
         InitCableCarrierSegmentQuantities(baseQuantities);
         InitCableSegmentQuantities(baseQuantities);
         
         quantities.Add(baseQuantities);
      }

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
