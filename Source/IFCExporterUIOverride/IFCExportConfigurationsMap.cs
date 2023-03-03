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
using System.Web.Script.Serialization;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.ExtensibleStorage;
using Revit.IFC.Common.Enums;
using Revit.IFC.Export.Utility;
using Revit.IFC.Common.Utility;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// The map to store BuiltIn and Saved configurations.
   /// </summary>
   public class IFCExportConfigurationsMap
   {
      private Dictionary<String, IFCExportConfiguration> m_configurations = new Dictionary<String, IFCExportConfiguration>();

      private Schema m_OldSchema = null;
      private static Guid s_OldSchemaId = new Guid("A1E672E5-AC88-4933-A019-F9068402CFA7");

      private Schema m_mapSchema = null;
      private static Guid s_mapSchemaId = new Guid("DCB88B13-594F-44F6-8F5D-AE9477305AC3");

      // New schema based on json for the entire configuration instead of individual item
      private Schema m_jsonSchema = null;
      private static Guid s_jsonSchemaId = new Guid("C2A3E6FE-CE51-4F35-8FF1-20C34567B687");

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
            AddOrReplace(value.Clone());
         }
      }

      /// <summary>
      /// Adds the built-in configurations to the map.
      /// </summary>
      public void AddBuiltInConfigurations()
      {
         // These are the built-in configurations.  Provide a more extensible means of storage.
         // Order of construction: name, version, space boundaries, QTO, split walls, internal sets, 2d elems, boundingBox
         AddOrReplace(IFCExportConfiguration.CreateBuiltInConfiguration(IFCVersion.IFC2x3CV2, 0, false, false, false, false, false, false, false, false, false, false, includeSteelElements: true));
         AddOrReplace(IFCExportConfiguration.CreateBuiltInConfiguration(IFCVersion.IFC2x3, 1, false, false, true, false, false, false, false, true, false, false, includeSteelElements: true));
         AddOrReplace(IFCExportConfiguration.CreateBuiltInConfiguration(IFCVersion.IFCCOBIE, 2, true, true, true, false, false, false, false, true, true, false, includeSteelElements: true));
         AddOrReplace(IFCExportConfiguration.CreateBuiltInConfiguration(IFCVersion.IFC2x3BFM, 1, true, true, false, false, false, false, false, true, false, false, includeSteelElements: true));
         AddOrReplace(IFCExportConfiguration.CreateBuiltInConfiguration(IFCVersion.IFC2x2, 1, false, false, true, false, false, false, false, false, false, false));
         AddOrReplace(IFCExportConfiguration.CreateBuiltInConfiguration(IFCVersion.IFC2x3FM, 1, true, false, false, false, true, true, false, true, true, false, includeSteelElements: true));
         AddOrReplace(IFCExportConfiguration.CreateBuiltInConfiguration(IFCVersion.IFC4RV, 0, true, false, false, false, false, false, false, false, false, false, includeSteelElements: true,
            exchangeRequirement:KnownERNames.Architecture));
         AddOrReplace(IFCExportConfiguration.CreateBuiltInConfiguration(IFCVersion.IFC4RV, 0, true, false, false, false, false, false, false, false, false, false, includeSteelElements: true,
            exchangeRequirement:KnownERNames.Structural));
         AddOrReplace(IFCExportConfiguration.CreateBuiltInConfiguration(IFCVersion.IFC4RV, 0, true, false, false, false, false, false, false, false, false, false, includeSteelElements: true,
            exchangeRequirement:KnownERNames.BuildingService));
         AddOrReplace(IFCExportConfiguration.CreateBuiltInConfiguration(IFCVersion.IFC4DTV, 0, true, false, false, false, false, false, false, false, false, false, includeSteelElements: true));
         //Handling the IFC4x3 format for using the IFC Extension with Revit versions older than 2023.1 which does not support IFC4x3.
         if(OptionsUtil.IsIFC4x3Supported())
            AddOrReplace(IFCExportConfiguration.CreateBuiltInConfiguration(OptionsUtil.GetIFCVersionByName("IFC4x3"), 0, true, false, false, false, false, false, false, false, false, false, includeSteelElements: true));
      }

      /// <summary>
      /// Adds the saved configuration from document to the map.
      /// </summary>
      public void AddSavedConfigurations()
      {
         try
         {
            // find the config in old schema.
            if (m_OldSchema == null)
            {
               m_OldSchema = Schema.Lookup(s_OldSchemaId);

               if (m_OldSchema != null)
               {
                  foreach (DataStorage storedSetup in GetSavedConfigurations(m_OldSchema))
                  {
                     Entity configEntity = storedSetup.GetEntity(m_OldSchema);
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
                     Field fieldIFCCommonPropertySets = m_OldSchema.GetField(s_setupExportIFCCommonProperty);
                     if (fieldIFCCommonPropertySets != null)
                        configuration.ExportIFCCommonPropertySets = configEntity.Get<bool>(s_setupExportIFCCommonProperty);
                     configuration.Use2DRoomBoundaryForVolume = configEntity.Get<bool>(s_setupUse2DForRoomVolume);
                     configuration.UseFamilyAndTypeNameForReference = configEntity.Get<bool>(s_setupUseFamilyAndTypeName);
                     Field fieldPartsAsBuildingElements = m_OldSchema.GetField(s_setupExportPartsAsBuildingElements);
                     if (fieldPartsAsBuildingElements != null)
                        configuration.ExportPartsAsBuildingElements = configEntity.Get<bool>(s_setupExportPartsAsBuildingElements);
                     Field fieldExportBoundingBox = m_OldSchema.GetField(s_setupExportBoundingBox);
                     if (fieldExportBoundingBox != null)
                        configuration.ExportBoundingBox = configEntity.Get<bool>(s_setupExportBoundingBox);
                     Field fieldExportSolidModelRep = m_OldSchema.GetField(s_setupExportSolidModelRep);
                     if (fieldExportSolidModelRep != null)
                        configuration.ExportSolidModelRep = configEntity.Get<bool>(s_setupExportSolidModelRep);
                     Field fieldExportMaterialPsets = m_OldSchema.GetField(s_setupExportMaterialPsets);
                     if (fieldExportMaterialPsets != null)
                        configuration.ExportMaterialPsets = configEntity.Get<bool>(s_setupExportMaterialPsets);
                     Field fieldExportSchedulesAsPsets = m_OldSchema.GetField(s_setupExportSchedulesAsPsets);
                     if (fieldExportSchedulesAsPsets != null)
                        configuration.ExportSchedulesAsPsets = configEntity.Get<bool>(s_setupExportSchedulesAsPsets);
                     Field fieldExportUserDefinedPsets = m_OldSchema.GetField(s_setupExportUserDefinedPsets);
                     if (fieldExportUserDefinedPsets != null)
                        configuration.ExportUserDefinedPsets = configEntity.Get<bool>(s_setupExportUserDefinedPsets);
                     Field fieldExportUserDefinedPsetsFileName = m_OldSchema.GetField(s_setupExportUserDefinedPsetsFileName);
                     if (fieldExportUserDefinedPsetsFileName != null)
                        configuration.ExportUserDefinedPsetsFileName = configEntity.Get<string>(s_setupExportUserDefinedPsetsFileName);

                     Field fieldExportUserDefinedParameterMapingTable = m_OldSchema.GetField(s_setupExportUserDefinedParameterMapping);
                     if (fieldExportUserDefinedParameterMapingTable != null)
                        configuration.ExportUserDefinedParameterMapping = configEntity.Get<bool>(s_setupExportUserDefinedParameterMapping);

                     Field fieldExportUserDefinedParameterMappingFileName = m_OldSchema.GetField(s_setupExportUserDefinedParameterMappingFileName);
                     if (fieldExportUserDefinedParameterMappingFileName != null)
                        configuration.ExportUserDefinedParameterMappingFileName = configEntity.Get<string>(s_setupExportUserDefinedParameterMappingFileName);

                     Field fieldExportLinkedFiles = m_OldSchema.GetField(s_setupExportLinkedFiles);
                     if (fieldExportLinkedFiles != null)
                        configuration.ExportLinkedFiles = configEntity.Get<bool>(s_setupExportLinkedFiles);
                     Field fieldIncludeSiteElevation = m_OldSchema.GetField(s_setupIncludeSiteElevation);
                     if (fieldIncludeSiteElevation != null)
                        configuration.IncludeSiteElevation = configEntity.Get<bool>(s_setupIncludeSiteElevation);
                     Field fieldStoreIFCGUID = m_OldSchema.GetField(s_setupStoreIFCGUID);
                     if (fieldStoreIFCGUID != null)
                        configuration.StoreIFCGUID = configEntity.Get<bool>(s_setupStoreIFCGUID);
                     Field fieldActivePhase = m_OldSchema.GetField(s_setupActivePhase);
                     if (fieldActivePhase != null)
                        configuration.ActivePhaseId = int.Parse(configEntity.Get<string>(s_setupActivePhase));
                     Field fieldExportRoomsInView = m_OldSchema.GetField(s_setupExportRoomsInView);
                     if (fieldExportRoomsInView != null)
                        configuration.ExportRoomsInView = configEntity.Get<bool>(s_setupExportRoomsInView);
                     Field fieldIncludeSteelElements = m_OldSchema.GetField(s_includeSteelElements);
                     if (fieldIncludeSteelElements != null)
                        configuration.IncludeSteelElements = configEntity.Get<bool>(s_includeSteelElements);
                     Field fieldUseOnlyTriangulation = m_OldSchema.GetField(s_useOnlyTriangulation);
                     if (fieldUseOnlyTriangulation != null)
                        configuration.UseOnlyTriangulation = configEntity.Get<bool>(s_useOnlyTriangulation);
                     Field fieldUseTypeNameOnlyForIfcType = m_OldSchema.GetField(s_useTypeNameOnlyForIfcType);
                     if (fieldUseTypeNameOnlyForIfcType != null)
                        configuration.UseTypeNameOnlyForIfcType = configEntity.Get<bool>(s_useTypeNameOnlyForIfcType);
                     Field fieldUseVisibleRevitNameAsEntityName = m_OldSchema.GetField(s_useVisibleRevitNameAsEntityName);
                     if (fieldUseVisibleRevitNameAsEntityName != null)
                        configuration.UseVisibleRevitNameAsEntityName = configEntity.Get<bool>(s_useVisibleRevitNameAsEntityName);
                     Field fieldTessellationLevelOfDetail = m_OldSchema.GetField(s_setupTessellationLevelOfDetail);
                     if (fieldTessellationLevelOfDetail != null)
                        configuration.TessellationLevelOfDetail = configEntity.Get<double>(s_setupTessellationLevelOfDetail);

                     AddOrReplace(configuration);
                  }
               }
            }

            // This is the newer schema
            if (m_mapSchema == null)
            {
               m_mapSchema = Schema.Lookup(s_mapSchemaId);

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
                        configuration.ActivePhaseId = int.Parse(configMap[s_setupActivePhase]);
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
                     if (configMap.ContainsKey(s_setupExportMaterialPsets))
                        configuration.ExportMaterialPsets = bool.Parse(configMap[s_setupExportMaterialPsets]);
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
                     if (configMap.ContainsKey(s_selectedSite))
                        configuration.SelectedSite = configMap[s_selectedSite];
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

                     AddOrReplace(configuration);
                  }
               }
            }

            // In this latest schema, the entire configuration for one config is stored as a json string in the entirety
            if (m_jsonSchema == null)
            {
               m_jsonSchema = Schema.Lookup(s_jsonSchemaId);
               if (m_jsonSchema != null)
               {
                  foreach (DataStorage storedSetup in GetSavedConfigurations(m_jsonSchema))
                  {
                     try
                     {
                        Entity configEntity = storedSetup.GetEntity(m_jsonSchema);
                        string configData = configEntity.Get<string>(s_configMapField);
                        JavaScriptSerializer ser = new JavaScriptSerializer();
                        ser.RegisterConverters(new JavaScriptConverter[] { new IFCExportConfigurationConverter() });
                        IFCExportConfiguration configuration = ser.Deserialize<IFCExportConfiguration>(configData);
                        AddOrReplace(configuration);
                     }
                     catch (Exception)
                     {
                        // don't skip all configurations if an exception occurs for one
                     }
                  }
               }
            }

            // Add the last selected configurations if any
            if (IFCExport.LastSelectedConfig != null && IFCExport.LastSelectedConfig.Count > 0)
            {
               foreach (KeyValuePair<string, IFCExportConfiguration> lastSelConfig in IFCExport.LastSelectedConfig)
               {
                  AddOrReplace(lastSelConfig.Value);
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
      // The following are the keys in the MapField in older schema. For oldest schema, they are simple fields.
      // In the latest schema (json based schema), there is no need for individual fields. The entire configuration is stored in one json string
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
      private const string s_setupExportMaterialPsets = "ExportMaterialPsets";
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
      private const string s_selectedSite = "SelectedSite";
      private const string s_geoRefCRSName = "GeoRefCRSName";
      private const string s_geoRefCRSDesc = "GeoRefCRSDesc";
      private const string s_geoRefEPSGCode = "GeoRefEPSGCode";
      private const string s_geoRefGeodeticDatum = "GeoRefGeodeticDatum";
      private const string s_geoRefMapUnit = "GeoRefMapUnit";

      /// <summary>
      /// Updates the setups to save into the document.
      /// </summary>
      public void UpdateSavedConfigurations(IFCExportConfigurationsMap initialConfigs)
      {
         // delete the old schema and the DataStorage.
         if (m_OldSchema == null)
         {
            m_OldSchema = Schema.Lookup(s_OldSchemaId);
         }
         if (m_OldSchema != null)
         {
            IList<DataStorage> oldSavedConfigurations = GetSavedConfigurations(m_OldSchema);
            if (oldSavedConfigurations.Count > 0)
            {
               Transaction deleteTransaction = new Transaction(IFCCommandOverrideApplication.TheDocument,
                   Properties.Resources.DeleteOldSetups);
               try
               {
                  deleteTransaction.Start(Properties.Resources.DeleteOldConfiguration);
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

         // delete the old schema and the DataStorage.
         if (m_mapSchema == null)
         {
            m_mapSchema = Schema.Lookup(s_mapSchemaId);
         }
         if (m_mapSchema != null)
         {
            IList<DataStorage> oldSavedConfigurations = GetSavedConfigurations(m_mapSchema);
            if (oldSavedConfigurations.Count > 0)
            {
               Transaction deleteTransaction = new Transaction(IFCCommandOverrideApplication.TheDocument,
                   Properties.Resources.DeleteOldSetups);
               try
               {
                  deleteTransaction.Start(Properties.Resources.DeleteOldConfiguration);
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
         if (m_jsonSchema == null)
         {
            m_jsonSchema = Schema.Lookup(s_jsonSchemaId);
         }

         // Are there any setups to save or resave?
         List<IFCExportConfiguration> setupsToSave = new List<IFCExportConfiguration>();
         foreach (IFCExportConfiguration configuration in m_configurations.Values)
         {
            // Store in-session settings in the cached in-session configuration
            if (configuration.IsInSession)
            {
               IFCExportConfiguration.SetInSession(configuration);
               continue;
            }

            // Only add to setupsToSave if it is a new or changed configuration
            if (initialConfigs.HasName(configuration.Name))
            {
               if (!ConfigurationComparer.ConfigurationsAreEqual(initialConfigs[configuration.Name], configuration))
                  setupsToSave.Add(configuration);
               else if (!configuration.IsBuiltIn)
                  setupsToSave.Add(configuration);
            }
            else
               setupsToSave.Add(configuration);
         }

         // If there are no setups to save, and if the schema is not present (which means there are no
         // previously existing setups which might have been deleted) we can skip the rest of this method.
         if (setupsToSave.Count <= 0 && m_jsonSchema == null)
            return;

         if (m_jsonSchema == null)
         {
            SchemaBuilder builder = new SchemaBuilder(s_jsonSchemaId);
            builder.SetSchemaName("IFCExportConfigurationMap");
            builder.AddSimpleField(s_configMapField, typeof(String));
            m_jsonSchema = builder.Finish();
         }

         // It won't start any transaction if there is no change to the configurations
         if (setupsToSave.Count > 0)
         {
            // Overwrite all saved configs with the new list
            Transaction transaction = new Transaction(IFCCommandOverrideApplication.TheDocument, Properties.Resources.UpdateExportSetups);
            try
            {
               transaction.Start(Properties.Resources.SaveConfigurationChanges);
               IList<DataStorage> savedConfigurations = GetSavedConfigurations(m_jsonSchema);
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

                  Entity mapEntity = new Entity(m_jsonSchema);
                  string configData = configuration.SerializeConfigToJson();
                  mapEntity.Set<string>(s_configMapField, configData);
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
      public void AddOrReplace(IFCExportConfiguration configuration)
      {
         if (m_configurations.ContainsKey(configuration.Name))
         {
            if (m_configurations[configuration.Name].IsBuiltIn)
               m_configurations[configuration.Name].UpdateBuiltInConfiguration(configuration);
            else
               m_configurations[configuration.Name] = configuration;
         }
         else
         {
            m_configurations.Add(configuration.Name, configuration);
         }
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