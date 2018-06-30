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
using System.IO;
using Autodesk.Revit.DB;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Exporter.PropertySet.Calculators;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Autodesk.Revit.ApplicationServices;
using Newtonsoft.Json;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Initializes user defined parameters and quantities.
   /// </summary>
   partial class ExporterInitializer
   {
      class IFCPsetList
      {
         public string Version { get; set; }
         [JsonProperty("PropertySet List")]
         public HashSet<string> PsetList { get; set; } = new HashSet<string>();
         public bool PsetIsInTheList(string psetName)
         {
            // return true if there is no entry
            if (PsetList.Count == 0)
               return true;

            if (PsetList.Contains(psetName))
               return true;
            else
               return false;
         }
      }

      class IFCCertifiedPSets
      {
         public IDictionary<string,IFCPsetList> CertifiedPsetList { get; set; } = new Dictionary<string,IFCPsetList>();
         public IFCCertifiedPSets()
         {
            string fileLoc = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetCallingAssembly().Location);
            string filePath = Path.Combine(fileLoc, "IFCCertifiedPSets.json");

            if (File.Exists(filePath))
            {
               CertifiedPsetList = JsonConvert.DeserializeObject<IDictionary<string, IFCPsetList>>(File.ReadAllText(filePath));
            }
         }

         public bool AllowPsetToBeCreated(string mvdName, string psetName)
         {
            // OK to create if the list is empty (not defined)
            if (CertifiedPsetList.Count == 0)
               return true;
            IFCPsetList theList;
            if (CertifiedPsetList.TryGetValue(mvdName, out theList))
            {
               if (theList.PsetIsInTheList(psetName))
                  return true;
               else
                  return false;
            }
            else
               return true;
         }
      }

      static IFCCertifiedPSets certifiedPsetList;

      /// <summary>
      /// Initializes Pset_ProvisionForVoid.
      /// </summary>
      /// <param name="commonPropertySets">List to store property sets.</param>
      private static void InitPset_ProvisionForVoid2x(IList<PropertySetDescription> commonPropertySets)
      {
         // The IFC4 version is contained in ExporterInitializer_PsetDef.cs.
         if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
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
         if (certifiedPsetList == null)
            certifiedPsetList = new IFCCertifiedPSets();

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

         if (propertySetsToExport != null)
            propertySetsToExport(cache.PropertySets);
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
               quantitiesToExport = InitBaseQuantities;
            else
               quantitiesToExport += InitBaseQuantities;
         }

         if (ExporterCacheManager.ExportOptionsCache.ExportAsCOBIE)
         {
            if (quantitiesToExport == null)
               quantitiesToExport = InitCOBIEQuantities;
            else
               quantitiesToExport += InitCOBIEQuantities;
         }

         if (quantitiesToExport != null)
            quantitiesToExport(cache.Quantities);
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

         // get the Pset definitions (using the same file as PropertyMap)
         IList<PropertySetDef> userDefinedPsetDefs = new List<PropertySetDef>();
         userDefinedPsetDefs = PropertyMap.LoadUserDefinedPset();

         bool exportPre4 = (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 || ExporterCacheManager.ExportOptionsCache.ExportAs2x3);

         // Loop through each definition and add the Pset entries into Cache
         foreach (PropertySetDef psetDef in userDefinedPsetDefs)
         {
            // Add Propertyset entry
            PropertySetDescription userDefinedPropetySet = new PropertySetDescription();
            userDefinedPropetySet.Name = psetDef.propertySetName;
            foreach (string elem in psetDef.applicableElements)
            {
               Common.Enums.IFCEntityType ifcEntity;
               if (Enum.TryParse(elem, out ifcEntity))
               {
                  if (exportPre4)
                  {
                     IFCEntityType originalEntity = ifcEntity;
                     IFCCompatibilityType.checkCompatibleType(originalEntity, out ifcEntity);
                  }

                  userDefinedPropetySet.EntityTypes.Add(ifcEntity);
                  // This is intended mostly as a workaround in IFC2x3 for IfcElementType.  Not all elements have an associated type (e.g. IfcRoof),
                  // but we still want to be able to export type property sets for that element.  So we will manually add these extra types here without
                  // forcing the user to guess.  If this causes issues, we may come up with a different design.
                  ISet<IFCEntityType> relatedEntities = GetListOfRelatedEntities(ifcEntity);
                  if (relatedEntities != null)
                     userDefinedPropetySet.EntityTypes.UnionWith(relatedEntities);
               }
            }

            foreach (PropertyDef prop in psetDef.propertyDefs)
            {
               PropertyType dataType;

               if (!Enum.TryParse(prop.PropertyDataType, out dataType))
                  dataType = PropertyType.Text;           // force default to Text/string if the type does not match with any correct datatype

               PropertySetEntry pSE = PropertySetEntry.CreateGenericEntry(dataType, prop.PropertyName);
               if (string.Compare(prop.PropertyName, prop.ParameterDefinitions[0].RevitParameterName) != 0)
               {
                  pSE.SetRevitParameterName(prop.ParameterDefinitions[0].RevitParameterName);
               }
               userDefinedPropetySet.AddEntry(pSE);
            }
            userDefinedPropertySets.Add(userDefinedPropetySet);
         }

         propertySets.Add(userDefinedPropertySets);
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
            if (ElementFilteringUtil.IsIFCExportAsSetToDontExport(schedule))
               continue;

            PropertySetDescription customPSet = new PropertySetDescription();

            string scheduleName = schedule.Name;
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
            }
            ExporterCacheManager.ViewScheduleElementCache.Add(new KeyValuePair<ElementId, HashSet<ElementId>>(schedule.Id, containedElementIds));

            IDictionary<ElementId, Element> cachedElementTypes = new Dictionary<ElementId, Element>();

            for (int ii = 0; ii < fieldCount; ii++)
            {
               ScheduleField field = definition.GetField(ii);

               ScheduleFieldType fieldType = field.FieldType;
               if (fieldType != ScheduleFieldType.Instance && fieldType != ScheduleFieldType.ElementType)
                  continue;

               ElementId parameterId = field.ParameterId;
               if (parameterId == ElementId.InvalidElementId)
                  continue;

               // We use asBuiltInParameterId to get the parameter by id below.  We don't want to use it later, however, so
               // we store builtInParameterId only if it is a proper member of the enumeration.
               BuiltInParameter asBuiltInParameterId = (BuiltInParameter)parameterId.IntegerValue;
               BuiltInParameter builtInParameterId =
                   Enum.IsDefined(typeof(BuiltInParameter), asBuiltInParameterId) ? asBuiltInParameterId : BuiltInParameter.INVALID;

               Parameter containedElementParameter = null;

               // We could cache the actual elements when we store the element ids.  However, this would almost certainly take more
               // time than getting one of the first few elements in the collector.
               foreach (Element containedElement in elementsInViewScheduleCollector)
               {
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
                     break;
               }
               if (containedElementParameter == null)
                  continue;

               PropertySetEntry ifcPSE = PropertySetEntry.CreateParameterEntry(containedElementParameter, builtInParameterId);
               ifcPSE.PropertyName = field.ColumnHeading;
               customPSet.AddEntry(ifcPSE);
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
      /// Initializes ceiling base quantities.
      /// </summary>
      /// <param name="baseQuantities">List to store quantities.</param>
      private static void InitCeilingBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcCeilingQuantity = new QuantityDescription();
         QuantityEntry ifcQE;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcCeilingQuantity.Name = "Qto_CoveringBaseQuantities";
            ifcQE = new QuantityEntry("NetArea", BuiltInParameter.HOST_AREA_COMPUTED);
         }
         else
         {
            ifcCeilingQuantity.Name = "BaseQuantities";
            ifcQE = new QuantityEntry("GrossCeilingArea", BuiltInParameter.HOST_AREA_COMPUTED);
         }
         ifcCeilingQuantity.EntityTypes.Add(IFCEntityType.IfcCovering);

         ifcQE.QuantityType = QuantityType.Area;
         ifcCeilingQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcCeilingQuantity);
      }

      /// <summary>
      /// Initializes railing base quantities.
      /// </summary>
      /// <param name="baseQuantities">List to store quantities.</param>
      private static void InitRailingBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcRailingQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcRailingQuantity.Name = "Qto_RailingBaseQuantities";
         }
         else
         {
            ifcRailingQuantity.Name = "BaseQuantities";
         }
         ifcRailingQuantity.EntityTypes.Add(IFCEntityType.IfcRailing);

         QuantityEntry ifcQE = new QuantityEntry("Length", BuiltInParameter.CURVE_ELEM_LENGTH);
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcRailingQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcRailingQuantity);
      }

      /// <summary>
      /// Initializes slab base quantities.
      /// </summary>
      /// <param name="baseQuantities">List to store quantities.</param>
      private static void InitSlabBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcSlabQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcSlabQuantity.Name = "Qto_SlabBaseQuantities";
         }
         else
         {
            ifcSlabQuantity.Name = "BaseQuantities";
         }
         ifcSlabQuantity.EntityTypes.Add(IFCEntityType.IfcSlab);

         QuantityEntry ifcQE = new QuantityEntry("GrossArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = GrossAreaCalculator.Instance;
         ifcSlabQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = NetSurfaceAreaCalculator.Instance;
         ifcSlabQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossVolume");
         ifcQE.QuantityType = QuantityType.Volume;
         ifcQE.PropertyCalculator = GrossVolumeCalculator.Instance;
         ifcSlabQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetVolume");
         ifcQE.QuantityType = QuantityType.Volume;
         ifcQE.PropertyCalculator = NetVolumeCalculator.Instance;
         ifcSlabQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("Perimeter");
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcQE.PropertyCalculator = PerimeterCalculator.Instance;
         ifcSlabQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("Width");
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcQE.PropertyCalculator = WidthCalculator.Instance;
         ifcSlabQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossWeight");
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcQE.PropertyCalculator = GrossWeightCalculator.Instance;
         ifcSlabQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetWeight");
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcQE.PropertyCalculator = NetWeightCalculator.Instance;
         ifcSlabQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcSlabQuantity);
      }

      /// <summary>
      /// Initializes ramp flight base quantities.
      /// </summary>
      /// <param name="baseQuantities">List to store quantities.</param>
      private static void InitRampFlightBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBaseQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcBaseQuantity.Name = "Qto_RampFlightBaseQuantities";
         }
         else
         {
            ifcBaseQuantity.Name = "BaseQuantities";
         }
         ifcBaseQuantity.EntityTypes.Add(IFCEntityType.IfcRampFlight);

         QuantityEntry ifcQE = new QuantityEntry("Width", BuiltInParameter.STAIRS_ATTR_TREAD_WIDTH);
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBaseQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcBaseQuantity);
      }

      /// <summary>
      /// Initializes Stairflight base quantity
      /// </summary>
      /// <param name="baseQuantities">List to store quantities.</param>
      private static void InitStairFlightBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBaseQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcBaseQuantity.Name = "Qto_StairFlightBaseQuantities";
         }
         else
         {
            ifcBaseQuantity.Name = "BaseQuantities";
         }
         ifcBaseQuantity.EntityTypes.Add(IFCEntityType.IfcStairFlight);

         QuantityEntry ifcQE = new QuantityEntry("Length");
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcQE.PropertyCalculator = LengthCalculator.Instance;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossVolume");
         ifcQE.QuantityType = QuantityType.Volume;
         ifcQE.PropertyCalculator = GrossVolumeCalculator.Instance;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetVolume");
         ifcQE.QuantityType = QuantityType.Volume;
         ifcQE.PropertyCalculator = NetVolumeCalculator.Instance;
         ifcBaseQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcBaseQuantity);
      }

      /// <summary>
      /// Initializes Building Storey base quantity
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitBuildingStoreyBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBaseQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcBaseQuantity.Name = "Qto_BuildingStoreyBaseQuantities";
         }
         else
         {
            ifcBaseQuantity.Name = "BaseQuantities";
         }
         ifcBaseQuantity.EntityTypes.Add(IFCEntityType.IfcBuildingStorey);

         QuantityEntry ifcQE = new QuantityEntry("NetHeight", "IfcQtyNetHeight");
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossHeight", "IfcQtyGrossHeight");
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBaseQuantity.AddEntry(ifcQE);

         ExportOptionsCache exportOptionsCache = ExporterCacheManager.ExportOptionsCache;
         if (!ExporterCacheManager.ExportOptionsCache.ExportAs2x3COBIE24DesignDeliverable)   // FMHandOver view exclude NetArea, GrossArea, NetVolume and GrossVolumne
         {
            ifcQE = new QuantityEntry("NetFloorArea");
            ifcQE.QuantityType = QuantityType.Area;
            ifcQE.PropertyCalculator = SpaceLevelAreaCalculator.Instance;
            ifcBaseQuantity.AddEntry(ifcQE);

            ifcQE = new QuantityEntry("GrossFloorArea");
            ifcQE.QuantityType = QuantityType.Area;
            ifcQE.PropertyCalculator = SpaceLevelAreaCalculator.Instance;
            ifcBaseQuantity.AddEntry(ifcQE);

            ifcQE = new QuantityEntry("GrossPerimeter", "IfcQtyGrossPerimeter");
            ifcQE.QuantityType = QuantityType.PositiveLength;
            ifcBaseQuantity.AddEntry(ifcQE);

            ifcQE = new QuantityEntry("NetVolume", "IfcQtyNetVolume");
            ifcQE.QuantityType = QuantityType.Volume;
            ifcBaseQuantity.AddEntry(ifcQE);

            ifcQE = new QuantityEntry("GrossVolume", "IfcQtyGrossVolume");
            ifcQE.QuantityType = QuantityType.Volume;
            ifcBaseQuantity.AddEntry(ifcQE);
         }

         baseQuantities.Add(ifcBaseQuantity);
      }

      /// <summary>
      /// Initializes Space base quantity
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitSpaceBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBaseQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcBaseQuantity.Name = "Qto_SpaceBaseQuantities";
         }
         else
         {
            ifcBaseQuantity.Name = "BaseQuantities";
         }
         ifcBaseQuantity.EntityTypes.Add(IFCEntityType.IfcSpace);

         QuantityEntry ifcQE = new QuantityEntry("NetFloorArea");
         ifcQE.MethodOfMeasurement = "area measured in geometry";
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = AreaCalculator.Instance;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("FinishCeilingHeight", "IfcQtyFinishCeilingHeight");
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetCeilingArea", "IfcQtyNetCeilingArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossCeilingArea", "IfcQtyGrossCeilingArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetWallArea", "IfcQtyNetWallArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossWallArea", "IfcQtyGrossWallArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("Height");
         ifcQE.MethodOfMeasurement = "length measured in geometry";
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcQE.PropertyCalculator = HeightCalculator.Instance;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetPerimeter", "IfcQtyNetPerimeter");
         ifcQE.MethodOfMeasurement = "length measured in geometry";
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossPerimeter");
         ifcQE.MethodOfMeasurement = "length measured in geometry";
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcQE.PropertyCalculator = PerimeterCalculator.Instance;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossFloorArea");
         ifcQE.MethodOfMeasurement = "area measured in geometry";
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = AreaCalculator.Instance;
         ifcBaseQuantity.AddEntry(ifcQE);

         ExportOptionsCache exportOptionsCache = ExporterCacheManager.ExportOptionsCache;
         if (!ExporterCacheManager.ExportOptionsCache.ExportAs2x3COBIE24DesignDeliverable)   // FMHandOver view exclude GrossVolumne, FinishFloorHeight
         {
            ifcQE = new QuantityEntry("GrossVolume");
            ifcQE.MethodOfMeasurement = "volume measured in geometry";
            ifcQE.QuantityType = QuantityType.Volume;
            ifcQE.PropertyCalculator = VolumeCalculator.Instance;
            ifcBaseQuantity.AddEntry(ifcQE);

            ifcQE = new QuantityEntry("FinishFloorHeight", "IfcQtyFinishFloorHeight");
            ifcQE.QuantityType = QuantityType.PositiveLength;
            ifcBaseQuantity.AddEntry(ifcQE);
         }

         baseQuantities.Add(ifcBaseQuantity);
      }

      /// <summary>
      /// Initializes Covering base quantity
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitCoveringBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBaseQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcBaseQuantity.Name = "Qto_CoveringBaseQuantities";
         }
         else
         {
            ifcBaseQuantity.Name = "BaseQuantities";
         }
         ifcBaseQuantity.EntityTypes.Add(IFCEntityType.IfcCovering);

         QuantityEntry ifcQE = new QuantityEntry("GrossArea", "IfcQtyGrossArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetArea", "IfcQtyNetArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcBaseQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcBaseQuantity);
      }

      /// <summary>
      /// Initializes Window base quantity
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitWindowBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBaseQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcBaseQuantity.Name = "Qto_WindowBaseQuantities";
         }
         else
         {
            ifcBaseQuantity.Name = "BaseQuantities";
         }
         ifcBaseQuantity.EntityTypes.Add(IFCEntityType.IfcWindow);

         QuantityEntry ifcQE = new QuantityEntry("Height", BuiltInParameter.WINDOW_HEIGHT);
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("Width", BuiltInParameter.WINDOW_WIDTH);
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("Area");
         ifcQE.MethodOfMeasurement = "area measured in geometry";
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = AreaCalculator.Instance;
         ifcBaseQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcBaseQuantity);
      }

      /// <summary>
      /// Initializes Door base quantity
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitDoorBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBaseQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcBaseQuantity.Name = "Qto_DoorBaseQuantities";
         }
         else
         {
            ifcBaseQuantity.Name = "BaseQuantities";
         }
         ifcBaseQuantity.EntityTypes.Add(IFCEntityType.IfcDoor);

         QuantityEntry ifcQE = new QuantityEntry("Height", BuiltInParameter.DOOR_HEIGHT);
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("Width", BuiltInParameter.DOOR_WIDTH);
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcBaseQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("Area");
         ifcQE.MethodOfMeasurement = "area measured in geometry";
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = AreaCalculator.Instance;
         ifcBaseQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcBaseQuantity);
      }

      /// <summary>
      /// Initialize Beam Base Quantities
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitBeamBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBeamQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcBeamQuantity.Name = "Qto_BeamBaseQuantities";
         }
         else
         {
            ifcBeamQuantity.Name = "BaseQuantities";
         }
         ifcBeamQuantity.EntityTypes.Add(IFCEntityType.IfcBeam);

         QuantityEntry ifcQE = new QuantityEntry("Length");
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcQE.PropertyCalculator = LengthCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("CrossSectionArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = CrossSectionAreaCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("OuterSurfaceArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = OuterSurfaceAreaCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossSurfaceArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = GrossSurfaceAreaCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetSurfaceArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = NetSurfaceAreaCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossVolume");
         ifcQE.QuantityType = QuantityType.Volume;
         ifcQE.PropertyCalculator = GrossVolumeCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetVolume");
         ifcQE.QuantityType = QuantityType.Volume;
         ifcQE.PropertyCalculator = NetVolumeCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossWeight");
         ifcQE.QuantityType = QuantityType.Weight;
         ifcQE.PropertyCalculator = GrossWeightCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetWeight");
         ifcQE.QuantityType = QuantityType.Weight;
         ifcQE.PropertyCalculator = NetWeightCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcBeamQuantity);
      }

      /// <summary>
      /// Initialize Column Base Quantities
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitColumnBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBeamQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcBeamQuantity.Name = "Qto_ColumnBaseQuantities";
         }
         else
         {
            ifcBeamQuantity.Name = "BaseQuantities";
         }
         ifcBeamQuantity.EntityTypes.Add(IFCEntityType.IfcColumn);

         QuantityEntry ifcQE = new QuantityEntry("Length");
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcQE.PropertyCalculator = LengthCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("CrossSectionArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = CrossSectionAreaCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("OuterSurfaceArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = OuterSurfaceAreaCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossSurfaceArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = GrossSurfaceAreaCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetSurfaceArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = NetSurfaceAreaCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossVolume");
         ifcQE.QuantityType = QuantityType.Volume;
         ifcQE.PropertyCalculator = GrossVolumeCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetVolume");
         ifcQE.QuantityType = QuantityType.Volume;
         ifcQE.PropertyCalculator = NetVolumeCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossWeight");
         ifcQE.QuantityType = QuantityType.Weight;
         ifcQE.PropertyCalculator = GrossWeightCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetWeight");
         ifcQE.QuantityType = QuantityType.Weight;
         ifcQE.PropertyCalculator = NetWeightCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcBeamQuantity);
      }

      /// <summary>
      /// Initialize Member Base Quantities
      /// </summary>
      /// <param name="baseQuantities"></param>
      private static void InitMemberBaseQuantities(IList<QuantityDescription> baseQuantities)
      {
         QuantityDescription ifcBeamQuantity = new QuantityDescription();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            ifcBeamQuantity.Name = "Qto_MemberBaseQuantities";
         }
         else
         {
            ifcBeamQuantity.Name = "BaseQuantities";
         }
         ifcBeamQuantity.EntityTypes.Add(IFCEntityType.IfcMember);

         QuantityEntry ifcQE = new QuantityEntry("Length");
         ifcQE.QuantityType = QuantityType.PositiveLength;
         ifcQE.PropertyCalculator = LengthCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("CrossSectionArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = CrossSectionAreaCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("OuterSurfaceArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = OuterSurfaceAreaCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossSurfaceArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = GrossSurfaceAreaCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetSurfaceArea");
         ifcQE.QuantityType = QuantityType.Area;
         ifcQE.PropertyCalculator = NetSurfaceAreaCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossVolume");
         ifcQE.QuantityType = QuantityType.Volume;
         ifcQE.PropertyCalculator = GrossVolumeCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetVolume");
         ifcQE.QuantityType = QuantityType.Volume;
         ifcQE.PropertyCalculator = NetVolumeCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("GrossWeight");
         ifcQE.QuantityType = QuantityType.Weight;
         ifcQE.PropertyCalculator = GrossWeightCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         ifcQE = new QuantityEntry("NetWeight");
         ifcQE.QuantityType = QuantityType.Weight;
         ifcQE.PropertyCalculator = NetWeightCalculator.Instance;
         ifcBeamQuantity.AddEntry(ifcQE);

         baseQuantities.Add(ifcBeamQuantity);
      }

      /// <summary>
      /// Initializes base quantities.
      /// </summary>
      /// <param name="quantities">List to store quantities.</param>
      /// <param name="fileVersion">The file version, currently unused.</param>
      private static void InitBaseQuantities(IList<IList<QuantityDescription>> quantities)
      {
         IList<QuantityDescription> baseQuantities = new List<QuantityDescription>();
         InitCeilingBaseQuantities(baseQuantities);
         InitRailingBaseQuantities(baseQuantities);
         InitSlabBaseQuantities(baseQuantities);
         InitRampFlightBaseQuantities(baseQuantities);
         InitStairFlightBaseQuantities(baseQuantities);
         InitBuildingStoreyBaseQuantities(baseQuantities);
         InitSpaceBaseQuantities(baseQuantities);
         InitCoveringBaseQuantities(baseQuantities);
         InitWindowBaseQuantities(baseQuantities);
         InitDoorBaseQuantities(baseQuantities);
         InitBeamBaseQuantities(baseQuantities);

         // TODO: Make this work with split columns by wall.
         //InitColumnBaseQuantities(baseQuantities);
         InitMemberBaseQuantities(baseQuantities);

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
