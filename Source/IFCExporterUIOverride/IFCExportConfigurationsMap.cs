//
// BIM IFC export alternate UI library: this library works with Autodesk(R) Revit(R) to provide an alternate user interface for the export of IFC files from Revit.
// Copyright (C) 2016  Autodesk, Inc.
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
using System.Text;

using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.ExtensibleStorage;
using BIM.IFC.Export.UI.Properties;
using Revit.IFC.Common.Enums;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// The map to store BuiltIn and Saved configurations.
   /// </summary>
   public class IFCExportConfigurationsMap
   {
      private Dictionary<String, IFCExportConfiguration> m_configurations = new Dictionary<String, IFCExportConfiguration>();
      private Schema m_schema = null;
      private Schema m_mapSchema = null;
      private static Guid s_schemaId = new Guid("A1E672E5-AC88-4933-A019-F9068402CFA7");
      private static Guid s_mapSchemaId = new Guid("DCB88B13-594F-44F6-8F5D-AE9477305AC3");

      /// <summary>
      /// Constructs a default map.
      /// </summary>
      public IFCExportConfigurationsMap()
      {
      }

      /// <summary>
      /// Constructs a new map as a copy of an existing one.
      /// </summary>
      /// <param name="map">The specific map to copy.</param>
      public IFCExportConfigurationsMap(IFCExportConfigurationsMap map)
      {
         // Deep copy
         foreach (IFCExportConfiguration value in map.Values)
         {
            Add(value.Clone());
         }
      }

      /// <summary>
      /// Adds the built-in configurations to the map.
      /// </summary>
      public void AddBuiltInConfigurations()
      {
         // These are the built-in configurations.  Provide a more extensible means of storage.
         // Order of construction: name, version, space boundaries, QTO, split walls, internal sets, 2d elems, boundingBox
         Add(IFCExportConfiguration.CreateBuiltInConfiguration("IFC2x3 Coordination View 2.0", IFCVersion.IFC2x3CV2, 0, false, false, false, false, false, false, false, false, false, includeSteelElements: true));
         Add(IFCExportConfiguration.CreateBuiltInConfiguration("IFC2x3 Coordination View", IFCVersion.IFC2x3, 1, false, false, true, false, false, false, true, false, false, includeSteelElements: true));
         Add(IFCExportConfiguration.CreateBuiltInConfiguration("IFC2x3 GSA Concept Design BIM 2010", IFCVersion.IFCCOBIE, 2, true, true, true, false, false, false, true, true, false, includeSteelElements: true));
         Add(IFCExportConfiguration.CreateBuiltInConfiguration("IFC2x3 Basic FM Handover View", IFCVersion.IFC2x3BFM, 1, true, true, false, false, false, false, true, false, false, includeSteelElements: true));
         Add(IFCExportConfiguration.CreateBuiltInConfiguration("IFC2x2 Coordination View", IFCVersion.IFC2x2, 1, false, false, true, false, false, false, false, false, false));
         Add(IFCExportConfiguration.CreateBuiltInConfiguration("IFC2x3 COBie 2.4 Design Deliverable", IFCVersion.IFC2x3FM, 1, true, false, false, true, true, false, true, true, false, includeSteelElements: true));
         Add(IFCExportConfiguration.CreateBuiltInConfiguration("IFC4 Reference View [Architecture]", IFCVersion.IFC4RV, 0, true, false, false, false, false, false, false, false, false, includeSteelElements: true,
            exchangeRequirement:KnownERNames.Architecture));
         Add(IFCExportConfiguration.CreateBuiltInConfiguration("IFC4 Reference View [Structural]", IFCVersion.IFC4RV, 0, true, false, false, false, false, false, false, false, false, includeSteelElements: true,
            exchangeRequirement:KnownERNames.Structural));
         Add(IFCExportConfiguration.CreateBuiltInConfiguration("IFC4 Reference View [BuildingService]", IFCVersion.IFC4RV, 0, true, false, false, false, false, false, false, false, false, includeSteelElements: true,
            exchangeRequirement:KnownERNames.BuildingService));
         Add(IFCExportConfiguration.CreateBuiltInConfiguration("IFC4 Design Transfer View", IFCVersion.IFC4DTV, 0, true, false, false, false, false, false, false, false, false, includeSteelElements: true));
      }

      /// <summary>
      /// Adds the saved configuration from document to the map.
      /// </summary>
      public void AddSavedConfigurations()
      {
         try
         {
            if (m_schema == null)
            {
               m_schema = Schema.Lookup(s_schemaId);
            }
            if (m_mapSchema == null)
            {
               m_mapSchema = Schema.Lookup(s_mapSchemaId);
            }

            if (m_mapSchema != null)
            {
               foreach (DataStorage storedSetup in GetSavedConfigurations(m_mapSchema))
               {
                  Entity configEntity = storedSetup.GetEntity(m_mapSchema);
                  IDictionary<string, string> configMap = configEntity.Get<IDictionary<string, string>>(s_configMapField);
                  IFCExportConfiguration configuration = IFCExportConfiguration.CreateDefaultConfiguration();
                  if (configMap.ContainsKey(s_setupName))
                     configuration.Name = configMap[s_setupName];
                  if (configMap.ContainsKey(s_setupVersion))
                     configuration.IFCVersion = (IFCVersion)Enum.Parse(typeof(IFCVersion), configMap[s_setupVersion]);
                  if (configMap.ContainsKey(s_exchangeRequirement))
                     configuration.ExchangeRequirement = IFCExchangeRequirements.ParseEREnum(configMap[s_exchangeRequirement]);
                  if (configMap.ContainsKey(s_setupFileFormat))
                     configuration.IFCFileType = (IFCFileFormat)Enum.Parse(typeof(IFCFileFormat), configMap[s_setupFileFormat]);
                  if (configMap.ContainsKey(s_setupSpaceBoundaries))
                     configuration.SpaceBoundaries = int.Parse(configMap[s_setupSpaceBoundaries]);
                  if (configMap.ContainsKey(s_setupActivePhase))
                     configuration.ActivePhaseId = new ElementId(int.Parse(configMap[s_setupActivePhase]));
                  if (configMap.ContainsKey(s_setupQTO))
                     configuration.ExportBaseQuantities = bool.Parse(configMap[s_setupQTO]);
                  if (configMap.ContainsKey(s_setupCurrentView))
                     configuration.VisibleElementsOfCurrentView = bool.Parse(configMap[s_setupCurrentView]);
                  if (configMap.ContainsKey(s_splitWallsAndColumns))
                     configuration.SplitWallsAndColumns = bool.Parse(configMap[s_splitWallsAndColumns]);
                  if (configMap.ContainsKey(s_setupExport2D))
                     configuration.Export2DElements = bool.Parse(configMap[s_setupExport2D]);
                  if (configMap.ContainsKey(s_setupExportRevitProps))
                     configuration.ExportInternalRevitPropertySets = bool.Parse(configMap[s_setupExportRevitProps]);
                  if (configMap.ContainsKey(s_setupExportIFCCommonProperty))
                     configuration.ExportIFCCommonPropertySets = bool.Parse(configMap[s_setupExportIFCCommonProperty]);
                  if (configMap.ContainsKey(s_setupUse2DForRoomVolume))
                     configuration.Use2DRoomBoundaryForVolume = bool.Parse(configMap[s_setupUse2DForRoomVolume]);
                  if (configMap.ContainsKey(s_setupUseFamilyAndTypeName))
                     configuration.UseFamilyAndTypeNameForReference = bool.Parse(configMap[s_setupUseFamilyAndTypeName]);
                  if (configMap.ContainsKey(s_setupExportPartsAsBuildingElements))
                     configuration.ExportPartsAsBuildingElements = bool.Parse(configMap[s_setupExportPartsAsBuildingElements]);
                  if (configMap.ContainsKey(s_useActiveViewGeometry))
                     configuration.UseActiveViewGeometry = bool.Parse(configMap[s_useActiveViewGeometry]);
                  if (configMap.ContainsKey(s_setupExportSpecificSchedules))
                     configuration.ExportSpecificSchedules = bool.Parse(configMap[s_setupExportSpecificSchedules]);
                  if (configMap.ContainsKey(s_setupExportBoundingBox))
                     configuration.ExportBoundingBox = bool.Parse(configMap[s_setupExportBoundingBox]);
                  if (configMap.ContainsKey(s_setupExportSolidModelRep))
                     configuration.ExportSolidModelRep = bool.Parse(configMap[s_setupExportSolidModelRep]);
                  if (configMap.ContainsKey(s_setupExportSchedulesAsPsets))
                     configuration.ExportSchedulesAsPsets = bool.Parse(configMap[s_setupExportSchedulesAsPsets]);
                  if (configMap.ContainsKey(s_setupExportUserDefinedPsets))
                     configuration.ExportUserDefinedPsets = bool.Parse(configMap[s_setupExportUserDefinedPsets]);
                  if (configMap.ContainsKey(s_setupExportUserDefinedPsetsFileName))
                     configuration.ExportUserDefinedPsetsFileName = configMap[s_setupExportUserDefinedPsetsFileName];
                  if (configMap.ContainsKey(s_setupExportUserDefinedParameterMapping))
                     configuration.ExportUserDefinedParameterMapping = bool.Parse(configMap[s_setupExportUserDefinedParameterMapping]);
                  if (configMap.ContainsKey(s_setupExportUserDefinedParameterMappingFileName))
                     configuration.ExportUserDefinedParameterMappingFileName = configMap[s_setupExportUserDefinedParameterMappingFileName];
                  if (configMap.ContainsKey(s_setupExportLinkedFiles))
                     configuration.ExportLinkedFiles = bool.Parse(configMap[s_setupExportLinkedFiles]);
                  if (configMap.ContainsKey(s_setupIncludeSiteElevation))
                     configuration.IncludeSiteElevation = bool.Parse(configMap[s_setupIncludeSiteElevation]);
                  if (configMap.ContainsKey(s_setupStoreIFCGUID))
                     configuration.StoreIFCGUID = bool.Parse(configMap[s_setupStoreIFCGUID]);
                  if (configMap.ContainsKey(s_setupExportRoomsInView))
                     configuration.ExportRoomsInView = bool.Parse(configMap[s_setupExportRoomsInView]);
                  if (configMap.ContainsKey(s_includeSteelElements))
                     configuration.IncludeSteelElements = bool.Parse(configMap[s_includeSteelElements]);
                  if (configMap.ContainsKey(s_useTypeNameOnlyForIfcType))
                     configuration.UseTypeNameOnlyForIfcType = bool.Parse(configMap[s_useTypeNameOnlyForIfcType]);
                  if (configMap.ContainsKey(s_useVisibleRevitNameAsEntityName))
                     configuration.UseVisibleRevitNameAsEntityName = bool.Parse(configMap[s_useVisibleRevitNameAsEntityName]);
                  if (configMap.ContainsKey(s_useOnlyTriangulation))
                     configuration.UseOnlyTriangulation = bool.Parse(configMap[s_useOnlyTriangulation]);
                  if (configMap.ContainsKey(s_setupTessellationLevelOfDetail))
                     configuration.TessellationLevelOfDetail = double.Parse(configMap[s_setupTessellationLevelOfDetail]);
                  if (configMap.ContainsKey(s_setupSitePlacement))
                  {
                     SiteTransformBasis siteTrfBasis = SiteTransformBasis.Shared;
                     if (Enum.TryParse(configMap[s_setupSitePlacement], out siteTrfBasis))
                        configuration.SitePlacement = siteTrfBasis;
                  }
                  // Geo Reference info
                  if (configMap.ContainsKey(s_geoRefCRSName))
                     configuration.GeoRefCRSName = configMap[s_geoRefCRSName];
                  if (configMap.ContainsKey(s_geoRefCRSDesc))
                     configuration.GeoRefCRSDesc = configMap[s_geoRefCRSDesc];
                  if (configMap.ContainsKey(s_geoRefEPSGCode))
                     configuration.GeoRefEPSGCode = configMap[s_geoRefEPSGCode];
                  if (configMap.ContainsKey(s_geoRefGeodeticDatum))
                     configuration.GeoRefGeodeticDatum = configMap[s_geoRefGeodeticDatum];
                  if (configMap.ContainsKey(s_geoRefMapUnit))
                     configuration.GeoRefMapUnit = configMap[s_geoRefMapUnit];

                  Add(configuration);
               }
               return; // if finds the config in map schema, return and skip finding the old schema.
            }

            // find the config in old schema.
            if (m_schema != null)
            {
               foreach (DataStorage storedSetup in GetSavedConfigurations(m_schema))
               {
                  Entity configEntity = storedSetup.GetEntity(m_schema);
                  IFCExportConfiguration configuration = IFCExportConfiguration.CreateDefaultConfiguration();
                  configuration.Name = configEntity.Get<String>(s_setupName);
                  configuration.IFCVersion = (IFCVersion)configEntity.Get<int>(s_setupVersion);
                  configuration.ExchangeRequirement = IFCExchangeRequirements.ParseEREnum(configEntity.Get<String>(s_exchangeRequirement));
                  configuration.IFCFileType = (IFCFileFormat)configEntity.Get<int>(s_setupFileFormat);
                  configuration.SpaceBoundaries = configEntity.Get<int>(s_setupSpaceBoundaries);
                  configuration.ExportBaseQuantities = configEntity.Get<bool>(s_setupQTO);
                  configuration.SplitWallsAndColumns = configEntity.Get<bool>(s_splitWallsAndColumns);
                  configuration.Export2DElements = configEntity.Get<bool>(s_setupExport2D);
                  configuration.ExportInternalRevitPropertySets = configEntity.Get<bool>(s_setupExportRevitProps);
                  Field fieldIFCCommonPropertySets = m_schema.GetField(s_setupExportIFCCommonProperty);
                  if (fieldIFCCommonPropertySets != null)
                     configuration.ExportIFCCommonPropertySets = configEntity.Get<bool>(s_setupExportIFCCommonProperty);
                  configuration.Use2DRoomBoundaryForVolume = configEntity.Get<bool>(s_setupUse2DForRoomVolume);
                  configuration.UseFamilyAndTypeNameForReference = configEntity.Get<bool>(s_setupUseFamilyAndTypeName);
                  Field fieldPartsAsBuildingElements = m_schema.GetField(s_setupExportPartsAsBuildingElements);
                  if (fieldPartsAsBuildingElements != null)
                     configuration.ExportPartsAsBuildingElements = configEntity.Get<bool>(s_setupExportPartsAsBuildingElements);
                  Field fieldExportBoundingBox = m_schema.GetField(s_setupExportBoundingBox);
                  if (fieldExportBoundingBox != null)
                     configuration.ExportBoundingBox = configEntity.Get<bool>(s_setupExportBoundingBox);
                  Field fieldExportSolidModelRep = m_schema.GetField(s_setupExportSolidModelRep);
                  if (fieldExportSolidModelRep != null)
                     configuration.ExportSolidModelRep = configEntity.Get<bool>(s_setupExportSolidModelRep);
                  Field fieldExportSchedulesAsPsets = m_schema.GetField(s_setupExportSchedulesAsPsets);
                  if (fieldExportSchedulesAsPsets != null)
                     configuration.ExportSchedulesAsPsets = configEntity.Get<bool>(s_setupExportSchedulesAsPsets);
                  Field fieldExportUserDefinedPsets = m_schema.GetField(s_setupExportUserDefinedPsets);
                  if (fieldExportUserDefinedPsets != null)
                     configuration.ExportUserDefinedPsets = configEntity.Get<bool>(s_setupExportUserDefinedPsets);
                  Field fieldExportUserDefinedPsetsFileName = m_schema.GetField(s_setupExportUserDefinedPsetsFileName);
                  if (fieldExportUserDefinedPsetsFileName != null)
                     configuration.ExportUserDefinedPsetsFileName = configEntity.Get<string>(s_setupExportUserDefinedPsetsFileName);

                  Field fieldExportUserDefinedParameterMapingTable = m_schema.GetField(s_setupExportUserDefinedParameterMapping);
                  if (fieldExportUserDefinedParameterMapingTable != null)
                     configuration.ExportUserDefinedParameterMapping = configEntity.Get<bool>(s_setupExportUserDefinedParameterMapping);

                  Field fieldExportUserDefinedParameterMappingFileName = m_schema.GetField(s_setupExportUserDefinedParameterMappingFileName);
                  if (fieldExportUserDefinedParameterMappingFileName != null)
                     configuration.ExportUserDefinedParameterMappingFileName = configEntity.Get<string>(s_setupExportUserDefinedParameterMappingFileName);

                  Field fieldExportLinkedFiles = m_schema.GetField(s_setupExportLinkedFiles);
                  if (fieldExportLinkedFiles != null)
                     configuration.ExportLinkedFiles = configEntity.Get<bool>(s_setupExportLinkedFiles);
                  Field fieldIncludeSiteElevation = m_schema.GetField(s_setupIncludeSiteElevation);
                  if (fieldIncludeSiteElevation != null)
                     configuration.IncludeSiteElevation = configEntity.Get<bool>(s_setupIncludeSiteElevation);
                  Field fieldStoreIFCGUID = m_schema.GetField(s_setupStoreIFCGUID);
                  if (fieldStoreIFCGUID != null)
                     configuration.StoreIFCGUID = configEntity.Get<bool>(s_setupStoreIFCGUID);
                  Field fieldActivePhase = m_schema.GetField(s_setupActivePhase);
                  if (fieldActivePhase != null)
                     configuration.ActivePhaseId = new ElementId(int.Parse(configEntity.Get<string>(s_setupActivePhase)));
                  Field fieldExportRoomsInView = m_schema.GetField(s_setupExportRoomsInView);
                  if (fieldExportRoomsInView != null)
                     configuration.ExportRoomsInView = configEntity.Get<bool>(s_setupExportRoomsInView);
                  Field fieldIncludeSteelElements = m_schema.GetField(s_includeSteelElements);
                  if (fieldIncludeSteelElements != null)
                     configuration.IncludeSteelElements = configEntity.Get<bool>(s_includeSteelElements);
                  Field fieldUseOnlyTriangulation = m_schema.GetField(s_useOnlyTriangulation);
                  if (fieldUseOnlyTriangulation != null)
                     configuration.UseOnlyTriangulation = configEntity.Get<bool>(s_useOnlyTriangulation);
                  Field fieldUseTypeNameOnlyForIfcType = m_schema.GetField(s_useTypeNameOnlyForIfcType);
                  if (fieldUseTypeNameOnlyForIfcType != null)
                     configuration.UseTypeNameOnlyForIfcType = configEntity.Get<bool>(s_useTypeNameOnlyForIfcType);
                  Field fieldUseVisibleRevitNameAsEntityName = m_schema.GetField(s_useVisibleRevitNameAsEntityName);
                  if (fieldUseVisibleRevitNameAsEntityName != null)
                     configuration.UseVisibleRevitNameAsEntityName = configEntity.Get<bool>(s_useVisibleRevitNameAsEntityName);
                  Field fieldTessellationLevelOfDetail = m_schema.GetField(s_setupTessellationLevelOfDetail);
                  if (fieldTessellationLevelOfDetail != null)
                     configuration.TessellationLevelOfDetail = configEntity.Get<double>(s_setupTessellationLevelOfDetail);

                  Add(configuration);
               }
            }
         }
         catch (System.Exception)
         {
            // to avoid fail to show the dialog if any exception throws in reading schema.
         }
      }

      // The MapField is to defined the map<string,string> in schema. 
      // Please don't change the name values, it affects the schema.
      private const string s_configMapField = "MapField";
      // The following are the keys in the MapField in new schema. For old schema, they are simple fields.
      private const string s_setupName = "Name";
      private const string s_setupVersion = "Version";
      private const string s_exchangeRequirement = "ExchangeRequirement";
      private const string s_setupFileFormat = "FileFormat";
      private const string s_setupSpaceBoundaries = "SpaceBoundaryLevel";
      private const string s_setupQTO = "ExportBaseQuantities";
      private const string s_splitWallsAndColumns = "SplitWallsAndColumns";
      private const string s_setupCurrentView = "VisibleElementsInCurrentView";
      private const string s_setupExport2D = "Export2DElements";
      private const string s_setupExportRevitProps = "ExportInternalRevitPropertySets";
      private const string s_setupExportIFCCommonProperty = "ExportIFCCommonPropertySets";
      private const string s_setupUse2DForRoomVolume = "Use2DBoundariesForRoomVolume";
      private const string s_setupUseFamilyAndTypeName = "UseFamilyAndTypeNameForReference";
      private const string s_setupExportPartsAsBuildingElements = "ExportPartsAsBuildingElements";
      private const string s_useActiveViewGeometry = "UseActiveViewGeometry";
      private const string s_setupExportSpecificSchedules = "ExportSpecificSchedules";
      private const string s_setupExportBoundingBox = "ExportBoundingBox";
      private const string s_setupExportSolidModelRep = "ExportSolidModelRep";
      private const string s_setupExportSchedulesAsPsets = "ExportSchedulesAsPsets";
      private const string s_setupExportUserDefinedPsets = "ExportUserDefinedPsets";
      private const string s_setupExportUserDefinedPsetsFileName = "ExportUserDefinedPsetsFileName";
      private const string s_setupExportUserDefinedParameterMapping = "ExportUserDefinedParameterMapping";
      private const string s_setupExportUserDefinedParameterMappingFileName = "ExportUserDefinedParameterMappingFileName";
      private const string s_setupExportLinkedFiles = "ExportLinkedFiles";
      private const string s_setupIncludeSiteElevation = "IncludeSiteElevation";
      private const string s_setupTessellationLevelOfDetail = "TessellationLevelOfDetail";
      private const string s_useOnlyTriangulation = "UseOnlyTriangulation";
      private const string s_setupStoreIFCGUID = "StoreIFCGUID";
      private const string s_setupActivePhase = "ActivePhase";
      private const string s_setupExportRoomsInView = "ExportRoomsInView";
      private const string s_excludeFilter = "ExcludeFilter";
      private const string s_setupSitePlacement = "SitePlacement";
      private const string s_useTypeNameOnlyForIfcType = "UseTypeNameOnlyForIfcType";
      private const string s_useVisibleRevitNameAsEntityName = "UseVisibleRevitNameAsEntityName";
      // Used for COBie 2.4
      private const string s_cobieCompanyInfo = "COBieCompanyInfo";
      private const string s_cobieProjectInfo = "COBieProjectInfo";
      private const string s_includeSteelElements = "IncludeSteelElements";
      // Geo Reference info
      private const string s_geoRefCRSName = "GeoRefCRSName";
      private const string s_geoRefCRSDesc = "GeoRefCRSDesc";
      private const string s_geoRefEPSGCode = "GeoRefEPSGCode";
      private const string s_geoRefGeodeticDatum = "GeoRefGeodeticDatum";
      private const string s_geoRefMapUnit = "GeoRefMapUnit";

      /// <summary>
      /// Updates the setups to save into the document.
      /// </summary>
      public void UpdateSavedConfigurations()
      {
         // delete the old schema and the DataStorage.
         if (m_schema == null)
         {
            m_schema = Schema.Lookup(s_schemaId);
         }
         if (m_schema != null)
         {
            IList<DataStorage> oldSavedConfigurations = GetSavedConfigurations(m_schema);
            if (oldSavedConfigurations.Count > 0)
            {
               Transaction deleteTransaction = new Transaction(IFCCommandOverrideApplication.TheDocument,
                   Properties.Resources.DeleteOldSetups);
               try
               {
                  deleteTransaction.Start();
                  List<ElementId> dataStorageToDelete = new List<ElementId>();
                  foreach (DataStorage dataStorage in oldSavedConfigurations)
                  {
                     dataStorageToDelete.Add(dataStorage.Id);
                  }
                  IFCCommandOverrideApplication.TheDocument.Delete(dataStorageToDelete);
                  deleteTransaction.Commit();
               }
               catch (System.Exception)
               {
                  if (deleteTransaction.HasStarted())
                     deleteTransaction.RollBack();
               }
            }
         }

         // update the configurations to new map schema.
         if (m_mapSchema == null)
         {
            m_mapSchema = Schema.Lookup(s_mapSchemaId);
         }

         // Are there any setups to save or resave?
         List<IFCExportConfiguration> setupsToSave = new List<IFCExportConfiguration>();
         foreach (IFCExportConfiguration configuration in m_configurations.Values)
         {
            if (configuration.IsBuiltIn)
               continue;

            // Store in-session settings in the cached in-session configuration
            if (configuration.IsInSession)
            {
               IFCExportConfiguration.SetInSession(configuration);
               continue;
            }

            setupsToSave.Add(configuration);
         }

         // If there are no setups to save, and if the schema is not present (which means there are no
         // previously existing setups which might have been deleted) we can skip the rest of this method.
         if (setupsToSave.Count <= 0 && m_mapSchema == null)
            return;

         if (m_mapSchema == null)
         {
            SchemaBuilder builder = new SchemaBuilder(s_mapSchemaId);
            builder.SetSchemaName("IFCExportConfigurationMap");
            builder.AddMapField(s_configMapField, typeof(String), typeof(String));
            m_mapSchema = builder.Finish();
         }

         // Overwrite all saved configs with the new list
         Transaction transaction = new Transaction(IFCCommandOverrideApplication.TheDocument, Properties.Resources.UpdateExportSetups);
         try
         {
            transaction.Start();
            IList<DataStorage> savedConfigurations = GetSavedConfigurations(m_mapSchema);
            int savedConfigurationCount = savedConfigurations.Count<DataStorage>();
            int savedConfigurationIndex = 0;
            foreach (IFCExportConfiguration configuration in setupsToSave)
            {
               DataStorage configStorage;
               if (savedConfigurationIndex >= savedConfigurationCount)
               {
                  configStorage = DataStorage.Create(IFCCommandOverrideApplication.TheDocument);
               }
               else
               {
                  configStorage = savedConfigurations[savedConfigurationIndex];
                  savedConfigurationIndex++;
               }

               Entity mapEntity = new Entity(m_mapSchema);
               IDictionary<string, string> mapData = new Dictionary<string, string>();
               mapData.Add(s_setupName, configuration.Name);
               mapData.Add(s_setupVersion, configuration.IFCVersion.ToString());
               mapData.Add(s_exchangeRequirement, configuration.ExchangeRequirement.ToString());
               mapData.Add(s_setupFileFormat, configuration.IFCFileType.ToString());
               mapData.Add(s_setupSpaceBoundaries, configuration.SpaceBoundaries.ToString());
               mapData.Add(s_setupQTO, configuration.ExportBaseQuantities.ToString());
               mapData.Add(s_setupCurrentView, configuration.VisibleElementsOfCurrentView.ToString());
               mapData.Add(s_splitWallsAndColumns, configuration.SplitWallsAndColumns.ToString());
               mapData.Add(s_setupExport2D, configuration.Export2DElements.ToString());
               mapData.Add(s_setupExportRevitProps, configuration.ExportInternalRevitPropertySets.ToString());
               mapData.Add(s_setupExportIFCCommonProperty, configuration.ExportIFCCommonPropertySets.ToString());
               mapData.Add(s_setupUse2DForRoomVolume, configuration.Use2DRoomBoundaryForVolume.ToString());
               mapData.Add(s_setupUseFamilyAndTypeName, configuration.UseFamilyAndTypeNameForReference.ToString());
               mapData.Add(s_setupExportPartsAsBuildingElements, configuration.ExportPartsAsBuildingElements.ToString());
               mapData.Add(s_useActiveViewGeometry, configuration.UseActiveViewGeometry.ToString());
               mapData.Add(s_setupExportSpecificSchedules, configuration.ExportSpecificSchedules.ToString());
               mapData.Add(s_setupExportBoundingBox, configuration.ExportBoundingBox.ToString());
               mapData.Add(s_setupExportSolidModelRep, configuration.ExportSolidModelRep.ToString());
               mapData.Add(s_setupExportSchedulesAsPsets, configuration.ExportSchedulesAsPsets.ToString());
               mapData.Add(s_setupExportUserDefinedPsets, configuration.ExportUserDefinedPsets.ToString());
               mapData.Add(s_setupExportUserDefinedPsetsFileName, configuration.ExportUserDefinedPsetsFileName);
               mapData.Add(s_setupExportUserDefinedParameterMapping, configuration.ExportUserDefinedParameterMapping.ToString());
               mapData.Add(s_setupExportUserDefinedParameterMappingFileName, configuration.ExportUserDefinedParameterMappingFileName);
               mapData.Add(s_setupExportLinkedFiles, configuration.ExportLinkedFiles.ToString());
               mapData.Add(s_setupIncludeSiteElevation, configuration.IncludeSiteElevation.ToString());
               mapData.Add(s_setupStoreIFCGUID, configuration.StoreIFCGUID.ToString());
               mapData.Add(s_setupActivePhase, configuration.ActivePhaseId.ToString());
               mapData.Add(s_setupExportRoomsInView, configuration.ExportRoomsInView.ToString());
               mapData.Add(s_useOnlyTriangulation, configuration.UseOnlyTriangulation.ToString());
               mapData.Add(s_excludeFilter, configuration.ExcludeFilter.ToString());
               mapData.Add(s_setupSitePlacement, configuration.SitePlacement.ToString());
               mapData.Add(s_useTypeNameOnlyForIfcType, configuration.UseTypeNameOnlyForIfcType.ToString());
               mapData.Add(s_useVisibleRevitNameAsEntityName, configuration.UseVisibleRevitNameAsEntityName.ToString());
               mapData.Add(s_setupTessellationLevelOfDetail, configuration.TessellationLevelOfDetail.ToString());
               // For COBie v2.4
               mapData.Add(s_cobieCompanyInfo, configuration.COBieCompanyInfo);
               mapData.Add(s_cobieProjectInfo, configuration.COBieProjectInfo);
               mapData.Add(s_includeSteelElements, configuration.IncludeSteelElements.ToString());
               // Geo Reference info
               mapData.Add(s_geoRefCRSName, configuration.GeoRefCRSName);
               mapData.Add(s_geoRefCRSDesc, configuration.GeoRefCRSDesc);
               mapData.Add(s_geoRefEPSGCode, configuration.GeoRefEPSGCode);
               mapData.Add(s_geoRefGeodeticDatum, configuration.GeoRefGeodeticDatum);
               mapData.Add(s_geoRefMapUnit, configuration.GeoRefMapUnit);

               mapEntity.Set<IDictionary<string, String>>(s_configMapField, mapData);
               configStorage.SetEntity(mapEntity);
            }

            List<ElementId> elementsToDelete = new List<ElementId>();
            for (; savedConfigurationIndex < savedConfigurationCount; savedConfigurationIndex++)
            {
               DataStorage configStorage = savedConfigurations[savedConfigurationIndex];
               elementsToDelete.Add(configStorage.Id);
            }
            if (elementsToDelete.Count > 0)
               IFCCommandOverrideApplication.TheDocument.Delete(elementsToDelete);

            transaction.Commit();
         }
         catch (System.Exception)
         {
            if (transaction.HasStarted())
               transaction.RollBack();
         }
      }

      /// <summary>
      /// Gets the saved setups from the document.
      /// </summary>
      /// <returns>The saved configurations.</returns>
      private IList<DataStorage> GetSavedConfigurations(Schema schema)
      {
         FilteredElementCollector collector = new FilteredElementCollector(IFCCommandOverrideApplication.TheDocument);
         collector.OfClass(typeof(DataStorage));
         Func<DataStorage, bool> hasTargetData = ds => (ds.GetEntity(schema) != null && ds.GetEntity(schema).IsValid());

         return collector.Cast<DataStorage>().Where<DataStorage>(hasTargetData).ToList<DataStorage>();
      }

      /// <summary>
      /// Adds a configuration to the map.
      /// </summary>
      /// <param name="configuration">The configuration.</param>
      public void Add(IFCExportConfiguration configuration)
      {
         m_configurations.Add(configuration.Name, configuration);
      }

      /// <summary>
      /// Remove a configuration by name.
      /// </summary>
      /// <param name="name">The name of configuration.</param>
      public void Remove(String name)
      {
         m_configurations.Remove(name);
      }

      /// <summary>
      /// Whether the map has the name of a configuration.
      /// </summary>
      /// <param name="name">The configuration name.</param>
      /// <returns>True for having the name, false otherwise.</returns>
      public bool HasName(String name)
      {
         if (name == null) return false;
         return m_configurations.ContainsKey(name);
      }

      /// <summary>
      /// The configuration by name.
      /// </summary>
      /// <param name="name">The name of a configuration.</param>
      /// <returns>The configuration of looking by name.</returns>
      public IFCExportConfiguration this[String name]
      {
         get
         {
            return m_configurations[name];
         }
      }

      /// <summary>
      /// The configurations in the map.
      /// </summary>
      public IEnumerable<IFCExportConfiguration> Values
      {
         get
         {
            return m_configurations.Values;
         }
      }
   }
}