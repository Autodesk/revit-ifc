﻿//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012-2016  Autodesk, Inc.
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
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Extensions;
using System.Text.RegularExpressions;


// CQ_TODO: Better storage of pipe insulation options

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// The cache which holds all export options.
   /// </summary>
   public class ExportOptionsCache
   {
      /// <summary>
      /// The pointer to the host document, set when exporting links as separate
      /// IFC files.
      /// </summary>
      /// <remarks>
      /// For active-view only export to work when exporting links as separate files,
      /// we need access to the host document.  However, this can't be passed using
      /// standard methods.  So we have a static pointer that can be set.  It is only
      /// expected to be valid when exporting links as separate files.
      /// </remarks>
      public static Document HostDocument { get; set; } = null;

      public SiteTransformBasis SiteTransformation { get; set; } = SiteTransformBasis.Shared;

      public enum ExportTessellationLevel
      {
         ExtraLow = 1,
         Low = 2,
         Medium = 3,
         High = 4
      }

      public COBieCompanyInfo COBieCompanyInfo { get; set; }

      public COBieProjectInfo COBieProjectInfo { get; set; }

      public IFCFileHeaderItem FileHeaderItem 
      { 
         get 
         { 
            return OptionsUtil.FileHeaderIFC; 
         } 
      }

      public KnownERNames ExchangeRequirement { get; set; } = KnownERNames.NotDefined;

      public string GeoRefCRSName { get; private set; }

      public string GeoRefCRSDesc { get; private set; }

      public string GeoRefEPSGCode { get; private set; }

      public string GeoRefGeodeticDatum { get; private set; }

      public string GeoRefMapUnit { get; private set; }

      /// <summary>
      /// If we are exporting a linked file as a separate document and using a filter view, 
      /// contains the element id of the filter view in the host document.
      /// </summary>
      public ElementId HostViewId { get; private set; } = ElementId.InvalidElementId;

      public bool IncludeSteelElements { get; set; }

      public IDictionary<int, string> FederatedLinkInfo { get; set; } = null;

      /// Private default constructor.
      /// </summary>
      private ExportOptionsCache()
      {
      }


      /// <summary>
      /// de-serialize vector passed from UI trough options 
      /// </summary>
      private static XYZ ParseXYZ(string value)
      {
         XYZ retVal = null;

         //split string to components by removing seprator characters
         string[] separator = new string[] { ",", "(", ")", " " };
         string[] sList = new string[3] { "", "", "" };
         sList = value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
         //should remain only 3 values if everything is OK

         try
         {
            // XYZ values are serialized in Revit using C++ format, which seems to use invariant culture.
            // Better yet would be not to pass doubles as strings, but this is a fairly limited use and
            // easily worked around here.
            double valX = double.Parse(sList[0], CultureInfo.InvariantCulture); //parsing values
            double valY = double.Parse(sList[1], CultureInfo.InvariantCulture);
            double valZ = double.Parse(sList[2], CultureInfo.InvariantCulture);
            //if no exception then put it in return value
            retVal = new XYZ(valX, valY, valZ);
         }
         catch (FormatException)
         {

         }
         //return null if there is a problem or a value 
         return retVal;
      }

      /// <summary>
      /// de-serialize transform passed from UI trough options 
      /// </summary>
      private static Transform ParseTransform(string value)
      {
         Transform retVal = null;

         try
         {
            //spit string by separator; it should remain 4 items
            string[] separator = new string[] { ";" };
            string[] sList = new string[4] { "", "", "", "" };

            sList = value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            Transform tr = new Transform(Transform.Identity);
            // parse each item in part
            tr.Origin = ParseXYZ(sList[0]);
            tr.BasisX = ParseXYZ(sList[1]);
            tr.BasisY = ParseXYZ(sList[2]);
            tr.BasisZ = ParseXYZ(sList[3]);
            // verify if value was correctly parsed
            if (tr.Origin != null && tr.BasisX != null &&
                tr.BasisY != null && tr.BasisZ != null)
               retVal = tr;
         }
         catch
         {
            retVal = null;
         }

         //return value
         return retVal;
      }

      private static ElementId ParseElementId(String singleElementValue)
      {
         ElementId elementId;
         if (ElementId.TryParse(singleElementValue, out elementId))
         {
            return elementId;
         }
         else
         {
            // Error - the option supplied could not be mapped to int.
            // TODO: consider logging this error later and handling results better.
            throw new Exception("String did not map to a usable element id");
         }
      }

      private static IList<ElementId> ParseElementIds(string elementsToExportValue)
      {
         string[] elements = elementsToExportValue.Split(';');
         List<ElementId> ids = new List<ElementId>();
         foreach (string element in elements)
         {
            ElementId elementId;
            if (ElementId.TryParse(element, out elementId))
            {
               ids.Add(elementId);
            }
            else
            {
               // Error - the option supplied could not be mapped to int.
               // TODO: consider logging this error later and handling results better.
               throw new Exception("Substring " + element + " did not map to a usable element id");
            }
         }
         return ids;
      }

      private static IDictionary<int, string> ParseFederatedLinkInfo(
         string federatedInfoString)
      {
         if (federatedInfoString == null)
            return null;

         IDictionary<int, string> federatedLinkInfo = 
            new SortedDictionary<int, string>();

         string[] idsAndGuids = federatedInfoString.Split(';');
         foreach (string idAndGuid in idsAndGuids)
         {
            if (idAndGuid == null)
               continue;

            string[] idGuidPair = idAndGuid.Split(',');
            if (idGuidPair.Count() != 2)
               continue;

            ElementId elementId;
            if (!ElementId.TryParse(idGuidPair[0], out elementId))
               continue;

            if (federatedLinkInfo.ContainsKey(elementId.IntegerValue))
               continue;

            if (string.IsNullOrWhiteSpace(idGuidPair[1]))
               continue;

            federatedLinkInfo[elementId.IntegerValue] = idGuidPair[1];
         }

         return federatedLinkInfo;
      }

      /// <summary>
      /// Creates a new export options cache from the data in the ExporterIFC passed from Revit.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC handle passed during export.</param>
      /// <returns>The new cache.</returns>
      public static ExportOptionsCache Create(ExporterIFC exporterIFC, View filterView)
      {
         IDictionary<string, string> options = exporterIFC.GetOptions();

         ExportOptionsCache cache = new ExportOptionsCache();
         cache.FileVersion = exporterIFC.FileVersion;
         cache.FileName = exporterIFC.FileName;
         cache.ExportBaseQuantities = exporterIFC.ExportBaseQuantities;
         cache.WallAndColumnSplitting = exporterIFC.WallAndColumnSplitting;
         cache.SpaceBoundaryLevel = exporterIFC.SpaceBoundaryLevel;
         // Export Part element only if 'Current View Only' is checked and 'Show Parts' is selected. Or if it is exported as IFC4RV
         cache.ExportParts = (filterView != null && filterView.PartsVisibility == PartsVisibility.ShowPartsOnly);
         cache.ExportPartsAsBuildingElementsOverride = null;
         cache.ExportAnnotationsOverride = null;

         // We are going to default to "true" for IncludeSteelElements to allow the default API
         // export to match the default UI.
         bool? includeSteelElements = OptionsUtil.GetNamedBooleanOption(options, "IncludeSteelElements");
         cache.IncludeSteelElements = includeSteelElements.HasValue && includeSteelElements.Value;

         if (filterView == null)
         {
            // if the filter view is null, but we have a HostViewId set, that means that we are
            // exporting a link as a separate file, and need to get the view from the host
            // document.
            int? filterViewIdInt = OptionsUtil.GetNamedIntOption(options, "HostViewId");
            if (filterViewIdInt.HasValue)
            {
               cache.HostViewId = new ElementId(filterViewIdInt.Value);
               cache.FilterViewForExport = HostDocument?.GetElement(cache.HostViewId) as View;
            }
         }
         else
         {
            // There is a bug in the native code that doesn't allow us to cast the filterView to
            // any sub-type of View.  Work around this by re-getting the element pointer.
            cache.FilterViewForExport = filterView?.Document.GetElement(filterView.Id) as View;
         }

         cache.ExportBoundingBoxOverride = null;
         cache.IncludeSiteElevation = false;

         cache.PropertySetOptions = PropertySetOptions.Create(exporterIFC, cache);

         string use2DRoomBoundary = Environment.GetEnvironmentVariable("Use2DRoomBoundaryForRoomVolumeCalculationOnIFCExport");
         bool? use2DRoomBoundaryOption = OptionsUtil.GetNamedBooleanOption(options, "Use2DRoomBoundaryForVolume");
         cache.Use2DRoomBoundaryForRoomVolumeCreation =
             ((use2DRoomBoundary != null && use2DRoomBoundary == "1") ||
             cache.ExportAs2x2 ||
             (use2DRoomBoundaryOption != null && use2DRoomBoundaryOption.GetValueOrDefault()));

         bool? exportAdvancedSweptSolids = OptionsUtil.GetNamedBooleanOption(options, "ExportAdvancedSweptSolids");
         cache.ExportAdvancedSweptSolids = (exportAdvancedSweptSolids.HasValue) ? exportAdvancedSweptSolids.Value : false;

         // Set GUIDOptions here.
         {
            // This option should be rarely used, and is only for consistency with old files.  As such, it is set by environment variable only.
            string use2009GUID = Environment.GetEnvironmentVariable("Assign2009GUIDToBuildingStoriesOnIFCExport");
            cache.GUIDOptions.Use2009BuildingStoreyGUIDs = (use2009GUID != null && use2009GUID == "1");

            bool? allowGUIDParameterOverride = OptionsUtil.GetNamedBooleanOption(options, "AllowGUIDParameterOverride");
            if (allowGUIDParameterOverride != null)
               cache.GUIDOptions.AllowGUIDParameterOverride = allowGUIDParameterOverride.Value;

            bool? storeIFCGUID = OptionsUtil.GetNamedBooleanOption(options, "StoreIFCGUID");
            if (storeIFCGUID != null)
               cache.GUIDOptions.StoreIFCGUID = storeIFCGUID.Value;
         }

         // Set NamingOptions here.
         cache.NamingOptions = new NamingOptions();
         {
            bool? useFamilyAndTypeNameForReference = OptionsUtil.GetNamedBooleanOption(options, "UseFamilyAndTypeNameForReference");
            cache.NamingOptions.UseFamilyAndTypeNameForReference =
                (useFamilyAndTypeNameForReference != null) && useFamilyAndTypeNameForReference.GetValueOrDefault();

            bool? useVisibleRevitNameAsEntityName = OptionsUtil.GetNamedBooleanOption(options, "UseVisibleRevitNameAsEntityName");
            cache.NamingOptions.UseVisibleRevitNameAsEntityName =
                (useVisibleRevitNameAsEntityName != null) && useVisibleRevitNameAsEntityName.GetValueOrDefault();

            bool? useOnlyTypeNameForIfcType = OptionsUtil.GetNamedBooleanOption(options, "UseTypeNameOnlyForIfcType");
            cache.NamingOptions.UseTypeNameOnlyForIfcType =
                (useOnlyTypeNameForIfcType != null) && useOnlyTypeNameForIfcType.GetValueOrDefault();
         }

         // "SingleElement" export option - useful for debugging - only one input element will be processed for export
         if (options.TryGetValue("SingleElement", out string singleElementValue))
         {
            ElementId elementId = ParseElementId(singleElementValue);

            List<ElementId> ids = new List<ElementId>();
            ids.Add(elementId);
            cache.ElementsForExport = ids;
         }
         else if (options.TryGetValue("ElementsForExport", out string elementsToExportValue))
         {
            IList<ElementId> ids = ParseElementIds(elementsToExportValue);
            cache.ElementsForExport = ids;
         }
         else
         {
            cache.ElementsForExport = new List<ElementId>();
         }

         // "ExportAnnotations" override
         cache.ExportAnnotationsOverride = OptionsUtil.GetNamedBooleanOption(options, "Export2DElements");

         // "ExportSeparateParts" override
         cache.ExportPartsAsBuildingElementsOverride = OptionsUtil.GetNamedBooleanOption(options, "ExportPartsAsBuildingElements");

         // "ExportBoundingBox" override
         cache.ExportBoundingBoxOverride = OptionsUtil.GetNamedBooleanOption(options, "ExportBoundingBox");

         bool? exportRoomsInView = OptionsUtil.GetNamedBooleanOption(options, "ExportRoomsInView");
         cache.ExportRoomsInView = exportRoomsInView != null ? exportRoomsInView.Value : false;

         // Include IFCSITE elevation in the site local placement origin
         bool? includeIfcSiteElevation = OptionsUtil.GetNamedBooleanOption(options, "IncludeSiteElevation");
         cache.IncludeSiteElevation = includeIfcSiteElevation != null ? includeIfcSiteElevation.Value : false;

         string siteTransformation = OptionsUtil.GetNamedStringOption(options, "SitePlacement");
         if (!string.IsNullOrEmpty(siteTransformation))
         {
            if (Enum.TryParse(siteTransformation, out SiteTransformBasis trfBasis))
               cache.SiteTransformation = trfBasis;
         }
         // We have two ways to get information about level of detail:
         // 1. The old Boolean "UseCoarseTessellation".
         // 2. The new double "TessellationLevelOfDetail".
         // We will combine these both into a LevelOfDetail integer that can be used by different elements differently.
         // The scale is from 1 (Extra Low) to 4 (High), where :
         // UseCoarseTessellation = true -> 1, UseCoarseTessellation = false -> 4
         // TessellationLevelOfDetail * 4 = LevelOfDetail
         // TessellationLevelOfDetail takes precedence over UseCoarseTessellation.

         cache.LevelOfDetail = ExportTessellationLevel.Low;

         bool? useCoarseTessellation = OptionsUtil.GetNamedBooleanOption(options, "UseCoarseTessellation");
         if (useCoarseTessellation.HasValue)
            cache.LevelOfDetail = useCoarseTessellation.Value ? ExportTessellationLevel.ExtraLow : ExportTessellationLevel.High;

         double? tessellationLOD = OptionsUtil.GetNamedDoubleOption(options, "TessellationLevelOfDetail");
         if (tessellationLOD.HasValue)
         {
            int levelOfDetail = (int)(tessellationLOD.Value * 4.0 + 0.5);
            // Ensure LOD is between 1 to 4, inclusive.
            levelOfDetail = Math.Min(Math.Max(levelOfDetail, 1), 4);
            cache.LevelOfDetail = (ExportTessellationLevel)levelOfDetail;
         }

         bool? useOnlyTriangulation = OptionsUtil.GetNamedBooleanOption(options, "UseOnlyTriangulation");
         cache.UseOnlyTriangulation = useOnlyTriangulation.HasValue ? useOnlyTriangulation.Value : false;

         /// Allow exporting a mix of extrusions and BReps as a solid model, if possible.
         bool? canExportSolidModelRep = OptionsUtil.GetNamedBooleanOption(options, "ExportSolidModelRep");
         cache.CanExportSolidModelRep = canExportSolidModelRep != null ? canExportSolidModelRep.Value : false;

         // Set the phase we are exporting
         cache.ActivePhaseId = ElementId.InvalidElementId;

         String activePhaseElementValue;
         if (options.TryGetValue("ActivePhaseId", out activePhaseElementValue))
            cache.ActivePhaseId = ParseElementId(activePhaseElementValue);

         if ((cache.ActivePhaseId == ElementId.InvalidElementId) && (cache.FilterViewForExport != null))
         {
            Parameter currPhase = cache.FilterViewForExport.get_Parameter(BuiltInParameter.VIEW_PHASE);
            if (currPhase != null)
               cache.ActivePhaseId = currPhase.AsElementId();
         }

         // "FileType" - note - setting is not respected yet
         ParseFileType(options, cache);

         cache.SelectedConfigName = OptionsUtil.GetNamedStringOption(options, "ConfigName");

         cache.SelectedParametermappingTableName = OptionsUtil.GetNamedStringOption(options, "ExportUserDefinedParameterMappingFileName");

         bool? bExportLinks = OptionsUtil.GetNamedBooleanOption(options, "ExportingLinks");
         cache.ExportingLink = (bExportLinks.HasValue && bExportLinks.Value == true);

         if (cache.ExportingLink)
         {
            int? numInstances = OptionsUtil.GetNamedIntOption(options, "NumberOfExportedLinkInstances");
            for (int ii = 0; ii < numInstances; ii++)
            {
               string optionName = (ii == 0) ? "ExportLinkInstanceTransform" : "ExportLinkInstanceTransform" + (ii + 1).ToString();
               string aLinkInstanceTransform = OptionsUtil.GetNamedStringOption(options, optionName);

               // We don't expect this to fail.  But in case it does, all it means is that we
               // can't filter out hidden elements.
               optionName = (ii == 0) ? "ExportLinkId" : "ExportLinkId" + (ii + 1).ToString();
               int? linkIdInt = OptionsUtil.GetNamedIntOption(options, optionName);
               ExporterStateManager.CurrentLinkId =
                  new ElementId(linkIdInt.HasValue ? linkIdInt.Value : -1);

               Transform currTransform = null;
               if (!string.IsNullOrEmpty(aLinkInstanceTransform))
               {
                  //reconstruct transform
                  Transform tr = ParseTransform(aLinkInstanceTransform);
                  //set to cache
                  if (tr != null)
                     currTransform = tr;
               }

               string fileName = null;

               if (ii > 0)
               {
                  optionName = "ExportLinkInstanceFileName" + (ii + 1).ToString();
                  fileName = OptionsUtil.GetNamedStringOption(options, optionName);
               }

               if (currTransform == null)
                  cache.LinkInstanceInfos.Add(new Tuple<string, Transform>(fileName, Transform.Identity));
               else
                  cache.LinkInstanceInfos.Add(new Tuple<string, Transform>(fileName, currTransform));
            }
         }

         cache.ExcludeFilter = OptionsUtil.GetNamedStringOption(options, "ExcludeFilter");

         cache.GeoRefCRSName = OptionsUtil.GetNamedStringOption(options, "GeoRefCRSName");
         cache.GeoRefCRSDesc = OptionsUtil.GetNamedStringOption(options, "GeoRefCRSDesc");
         cache.GeoRefEPSGCode = OptionsUtil.GetNamedStringOption(options, "GeoRefEPSGCode");
         cache.GeoRefGeodeticDatum = OptionsUtil.GetNamedStringOption(options, "GeoRefGeodeticDatum");
         cache.GeoRefMapUnit = OptionsUtil.GetNamedStringOption(options, "GeoRefMapUnit");

         return cache;
      }

      public void UpdateForDocument(ExporterIFC exporterIFC, Document document, string guid)
      {
         ExporterCacheManager.BaseLinkedDocumentGUID = guid;
         ExporterCacheManager.Document = document;

         IDictionary<string, string> options = exporterIFC.GetOptions();

         if (ActivePhaseId == ElementId.InvalidElementId)
         {
            PhaseArray phaseArray = document.Phases;
            Phase lastPhase = phaseArray.get_Item(phaseArray.Size - 1);
            ActivePhaseId = lastPhase.Id;
            ActivePhaseElement = lastPhase;
         }
         else
         {
            ActivePhaseElement = document.GetElement(ActivePhaseId) as Phase;
         }

         bool? useActiveViewGeometry = OptionsUtil.GetNamedBooleanOption(options, "UseActiveViewGeometry");
         UseActiveViewGeometry = useActiveViewGeometry.HasValue ? useActiveViewGeometry.Value : false;

         if (UseActiveViewGeometry)
         {
            int? viewId = OptionsUtil.GetNamedIntOption(options, "ActiveViewId");
            ElementId activeViewId = viewId.HasValue ? new ElementId(viewId.Value) : ElementId.InvalidElementId;
            View activeView = null;
            try
            {
               activeView = document.GetElement(activeViewId) as View;
            }
            catch
            {
            }
            ActiveView = activeView;
         }

         // Geo Reference info
         ExporterCacheManager.SelectedSiteProjectLocation = null;
         string selSite = OptionsUtil.GetNamedStringOption(options, "SelectedSite");
         foreach (ProjectLocation pLoc in document.ProjectLocations.Cast<ProjectLocation>().ToList())
         {
            if (pLoc.Name.Equals(selSite))
            {
               ExporterCacheManager.SelectedSiteProjectLocation = pLoc;
               break;
            }
         }
         // Ensure the cache is set to the default (ActiveProjectLocation) if not set
         if (ExporterCacheManager.SelectedSiteProjectLocation == null)
            ExporterCacheManager.SelectedSiteProjectLocation = document.ActiveProjectLocation;
      }

      /// <summary>
      /// Utility for parsing IFC file type.
      /// </summary>
      /// <remarks>
      /// If the file type can't be retrieved from the options collection, it will parse the file name extension.
      /// </remarks>
      /// <param name="options">The collection of named options for IFC export.</param>
      /// <param name="cache">The export options cache.</param>
      private static void ParseFileType(IDictionary<String, String> options, ExportOptionsCache cache)
      {
         String fileTypeString;
         if (options.TryGetValue("IFCFileType", out fileTypeString))
         {
            IFCFileFormat fileType;
            if (Enum.TryParse<IFCFileFormat>(fileTypeString, true, out fileType))
            {
               cache.IFCFileFormat = fileType;
            }
            else
            {
               // Error - the option supplied could not be mapped to ExportFileType.
               // TODO: consider logging this error later and handling results better.
               throw new Exception("Option 'FileType' did not match an existing IFCFileFormat value");
            }
         }
         else if (!string.IsNullOrEmpty(cache.FileName))
         {
            if (cache.FileName.EndsWith(".ifcXML")) //localization?
            {
               cache.IFCFileFormat = IFCFileFormat.IfcXML;
            }
            else if (cache.FileName.EndsWith(".ifcZIP"))
            {
               cache.IFCFileFormat = IFCFileFormat.IfcZIP;
            }
            else
            {
               cache.IFCFileFormat = IFCFileFormat.Ifc;
            }
         }
      }

      /// <summary>
      /// The property set options.
      /// </summary>
      public PropertySetOptions PropertySetOptions
      {
         get;
         set;
      }

      /// <summary>
      /// The file version.
      /// Used in ExportIntializer to define the Property Sets.
      /// Try not to use it outside of ExportOptionsCache except to initialize the Property Sets.
      /// </summary>
      public IFCVersion FileVersion { get; set; }

      /// <summary>
      /// The file name.
      /// </summary>
      public string FileName
      {
         get;
         set;
      }

      /// <summary>
      /// Identifies if the schema version being exported is IFC 2x2.
      /// </summary>
      public bool ExportAs2x2
      {
         get
         {
            return OptionsUtil.ExportAs2x2(FileVersion);
         }
      }

      /// <summary>
      /// Identifies if the schema version being exported is IFC 2x3 Coordination View 1.0.
      /// </summary>
      public bool ExportAs2x3CoordinationView1
      {
         get
         {
            return OptionsUtil.ExportAs2x3CoordinationView1(FileVersion);
         }
      }

      /// <summary>
      /// Identifies if the schema version being exported is IFC 2x3 Coordination View 2.0.
      /// </summary>
      public bool ExportAs2x3CoordinationView2
      {
         get
         {
            return OptionsUtil.ExportAs2x3CoordinationView2(FileVersion);
         }
      }

      /// <summary>
      /// Identifies if the schema version being exported is IFC 2x3 Extended FM Handover View (e.g., UK COBie).
      /// </summary>
      public bool ExportAs2x3ExtendedFMHandoverView
      {
         get
         {
            return OptionsUtil.ExportAs2x3ExtendedFMHandoverView(FileVersion);
         }
      }

      /// <summary>
      /// Identifies if the schema version and MVD being exported is IFC 2x3 Coordination View 2.0 or any IFC 4 MVD.
      /// </summary>
      /// <remarks>IFC 4 Coordination View 2.0 is not a real MVD; this was a placeholder and is obsolete.</remarks>
      public bool ExportAsCoordinationView2
      {
         get
         {
            return OptionsUtil.ExportAsCoordinationView2(FileVersion);
         }
      }

      /// <summary>
      /// Identifies if the IFC schema version is older than IFC 4.
      /// </summary>
      public bool ExportAsOlderThanIFC4
      {
         get
         {
            return OptionsUtil.ExportAsOlderThanIFC4(FileVersion);
         }
      }

      /// <summary>
      /// Identifies if the IFC schema version is older than IFC 4x3.
      /// </summary>
      public bool ExportAsOlderThanIFC4x3
      {
         get
         {
            return OptionsUtil.ExportAsOlderThanIFC4x3(FileVersion);
         }
      }

      /// <summary>
      /// Identifies if the IFC schema version being exported is IFC 4.
      /// </summary>
      public bool ExportAs4
      {
         get
         {
            return OptionsUtil.ExportAs4(FileVersion);
         }
      }

      /// <summary>
      /// Identifies if the schema used is IFC 2x3.
      /// </summary>
      public bool ExportAs2x3
      {
         get
         {
            return OptionsUtil.ExportAs2x3(FileVersion);
         }
      }

      /// <summary>
      /// Identifies if the schema and MVD used is the IFC 2x3 GSA 2010 COBie specification.
      /// </summary>
      public bool ExportAsCOBIE
      {
         get
         {
            return OptionsUtil.ExportAsCOBIE(FileVersion);
         }
      }

      /// <summary>
      /// Identifies if the schema and MVD used is the IFC 4 Reference View.
      /// </summary>
      public bool ExportAs4ReferenceView
      {
         get
         {
            return OptionsUtil.ExportAs4ReferenceView(FileVersion);
         }
      }

      /// <summary>
      /// Identifies if the schema and MVD used is the IFC 4 Design Transfer View.
      /// </summary>
      public bool ExportAs4DesignTransferView
      {
         get
         {
            return OptionsUtil.ExportAs4DesignTransferView(FileVersion);
         }
      }

      /// <summary>
      /// Option to be used for general IFC4 export (not specific to RV or DTV MVDs). Useful when there is a need to export entities that are not strictly valid within RV or DTV
      /// It should work like IFC2x3, except that it will use IFC4 tessellated geometry instead of IFC2x3 BREP
      /// </summary>
      public bool ExportAs4General
      {
         get
         {
            return OptionsUtil.ExportAs4General(FileVersion);
         }
      }

      /// <summary>
      /// Option for IFC4x3 export option
      /// </summary>
      public bool ExportAs4x3
      {
         get
         {
            return OptionsUtil.ExportAs4x3(FileVersion);
         }
      }

      /// <summary>
      /// Identifies if the schema and MVD used is the IFC 2x3 COBie 2.4 Design Deliverable.
      /// </summary>
      public bool ExportAs2x3COBIE24DesignDeliverable
      {
         get
         {
            return OptionsUtil.ExportAs2x3COBIE24DesignDeliverable(FileVersion);
         }
      }

      /// <summary>
      /// Cache variable for the export annotations override (if set independently via the UI or API inputs)
      /// </summary>
      private bool? ExportAnnotationsOverride
      {
         get;
         set;
      }

      /// <summary>
      /// Identifies if the file version being exported supports annotations.
      /// </summary>
      public bool ExportAnnotations
      {
         get
         {
            if (ExportAnnotationsOverride != null)
               return (bool)ExportAnnotationsOverride;
            return (!ExportAs2x2 && !ExportAsCoordinationView2);
         }
      }

      /// <summary>
      /// Identifies if we allow exporting advanced swept solids (vs. BReps if false).
      /// </summary>
      public bool ExportAdvancedSweptSolids
      {
         get;
         set;
      }

      /// <summary>
      /// Whether or not split walls and columns.
      /// </summary>
      public bool WallAndColumnSplitting
      {
         get;
         set;
      }

      /// <summary>
      /// Whether or not export base quantities.
      /// </summary>
      public bool ExportBaseQuantities
      {
         get;
         set;
      }

      /// <summary>
      /// The space boundary level.
      /// </summary>
      public int SpaceBoundaryLevel
      {
         get;
         set;
      }

      /// <summary>
      /// True to use the active view when generating geometry.
      /// False to use default export options.
      /// </summary>
      public bool UseActiveViewGeometry
      {
         get;
         set;
      }

      /// <summary>
      /// Whether or not export the Part element from host.
      /// Export Part element only if 'Current View Only' is checked and 'Show Parts' is selected. 
      /// </summary>
      public bool ExportParts
      {
         get;
         set;
      }

      /// <summary>
      /// Cache variable for the ExportPartsAsBuildingElements override (if set independently via the UI)
      /// </summary>
      public bool? ExportPartsAsBuildingElementsOverride
      {
         get;
         set;
      }

      /// <summary>
      /// Whether or not export the Parts as independent building elements.
      /// Only if allows export parts and 'Export parts as building elements' is selected. 
      /// </summary>
      public bool ExportPartsAsBuildingElements
      {
         get
         {
            if (ExportPartsAsBuildingElementsOverride != null)
               return (bool)ExportPartsAsBuildingElementsOverride;
            return false;
         }
      }


      /// <summary>
      /// Cache variable for the ExportBoundingBox override (if set independently via the UI)
      /// </summary>
      public bool? ExportBoundingBoxOverride
      {
         get;
         set;
      }

      /// <summary>
      /// Whether or not export the bounding box.
      /// </summary>
      public bool ExportBoundingBox
      {
         get
         {
            // if the option is set by alternate UI, return the setting in UI.
            if (ExportBoundingBoxOverride != null)
               return (bool)ExportBoundingBoxOverride;
            // otherwise export the bounding box only if it is GSA export.
            else if (FileVersion == IFCVersion.IFCCOBIE)
               return true;
            return false;
         }
      }

      /// <summary>
      /// Whether or not include IFCSITE elevation in the site local placement origin.
      /// </summary>
      public bool IncludeSiteElevation
      {
         get;
         set;
      }

      /// <summary>
      /// The level of detail to use when exporting geometry.  Different elements will use this differently.
      /// </summary>
      public ExportTessellationLevel LevelOfDetail
      {
         get;
         set;
      }

      /// <summary>
      /// The option to leave tessellation results as triangulation and not optimized into polygonal faceset (supported from IFC4_ADD2)
      /// </summary>
      public bool UseOnlyTriangulation
      {
         get;
         set;
      }

      /// <summary>
      /// The version of the exporter.
      /// </summary>
      public string ExporterVersion
      {
         get
         {
            string assemblyFile = typeof(Revit.IFC.Export.Exporter.Exporter).Assembly.Location;
            string exporterVersion = "Unknown Exporter version";
            if (File.Exists(assemblyFile))
            {
               exporterVersion = "IFC " + FileVersionInfo.GetVersionInfo(assemblyFile).FileVersion;
            }
            return exporterVersion;
         }
      }

      /// <summary>
      /// A collection of elements from which to export (before filtering is applied).  If empty, all elements in the document
      /// are used as the initial set of elements before filtering is applied.
      /// </summary>
      public IList<ElementId> ElementsForExport
      {
         get;
         set;
      }

      /// <summary>
      /// The filter view for export.  
      /// </summary>
      /// <remarks>This is the optional view that determines which elements to
      /// export based on visibility settings for the view.  It does not control
      /// what geometry is exported for the element.</remarks>
      public View FilterViewForExport
      {
         get;
         set;
      }

      /// <summary>
      /// Determines how to generate space volumes on export.  True means that we use the 2D room boundary and extrude it upwards based
      /// on the room height.  This is the method used in 2x2 and by user option.  False means using the room geometry.  The user option
      /// is needed for certain governmental requirements, such as in Korea for non-residental buildings.
      /// </summary>
      public bool Use2DRoomBoundaryForRoomVolumeCreation
      {
         get;
         set;
      }

      /// <summary>
      /// Contains options for controlling how IFC GUIDs are generated on export.
      /// </summary>
      public GUIDOptions GUIDOptions { get; } = new GUIDOptions();

      /// <summary>
      /// Contains options for setting how entity names are generated.
      /// </summary>
      public NamingOptions NamingOptions
      {
         get;
         set;
      }

      /// <summary>
      /// The file format to export.  Not used currently.
      /// </summary>
      // TODO: Connect this to the output file being written by the client.
      public IFCFileFormat IFCFileFormat
      {
         get;
         set;
      }

      /// <summary>
      /// Select export Config Name from the UI
      /// </summary>
      public String SelectedConfigName
      {
         get;
         set;
      }

      /// <summary>
      /// Select export Config Name from the UI
      /// </summary>
      public String SelectedParametermappingTableName
      {
         get;
         set;
      }

      /// <summary>
      /// Allow exporting a mix of extrusions and BReps as a solid model, if possible.
      /// </summary>
      public bool CanExportSolidModelRep { get; set; }

      /// <summary>
      /// Specifies which phase id to export.  May be expanded to phases.
      /// </summary>
      public ElementId ActivePhaseId
      {
         get;
         protected set;
      }

      /// <summary>
      /// The phase element corresponding to the phase id.
      /// </summary>
      public Phase ActivePhaseElement
      {
         get;
         protected set;
      }

      /// <summary>
      /// The status of how to handle Revit link instances.
      /// </summary>
      public LinkedFileExportAs ExportLinkedFileAs { get; set; } = LinkedFileExportAs.DontExport;
      
      ///<summary>
      /// The ExportingLink flag.
      /// This stores the flag telling if the current export is for a linked document.
      /// </summary>
      public bool ExportingLink
      {
         get;
         set;
      }

      private IList<Tuple<string, Transform>> LinkInstanceInfos { get; } = new List<Tuple<string, Transform>>();

      /// <summary>
      /// Get the number of RevitLinkInstance transforms for this export.
      /// </summary>
      /// <returns>The number of Revit Link Instance transforms for this export.</returns>
      public int GetNumLinkInstanceInfos()
      {
         return LinkInstanceInfos?.Count ?? 0;
      }

      /// <summary>
      /// Gets the file name of the link corresponding to the given index.
      /// </summary>
      /// <param name="idx">The index</param>
      /// <returns>The transform corresponding to the given index, or the Identity transform if out of range.</returns>
      /// <remarks>Note that the file name for index 0 is not stored here, and returns null.</remarks>
      public string GetLinkInstanceFileName(int idx)
      {
         if (idx < 1 || idx >= GetNumLinkInstanceInfos())
            return null;

         return LinkInstanceInfos[idx].Item1;
      }

      /// <summary>
      /// Gets the transform corresponding to the given index.
      /// </summary>
      /// <param name="idx">The index</param>
      /// <returns>The transform corresponding to the given index, or the Identity transform if out of range.</returns>
      public Transform GetUnscaledLinkInstanceTransform(int idx)
      {
         if (idx < 0 || idx >= GetNumLinkInstanceInfos())
            return Transform.Identity;

         Transform unscaledTransform = new Transform(LinkInstanceInfos[idx].Item2);
         unscaledTransform.Origin = UnitUtil.UnscaleLength(unscaledTransform.Origin);
         return unscaledTransform;
      }


      /// <summary>
      /// Whether or not to export all the rooms in the view.
      /// This option is only enabled if "export elements visible in view" is selected.
      /// </summary>
      /// <remarks>
      /// If ExportRoomsInView is true and section box is active: then every room whose bounding box intersects with the section box is exported,
      /// otherwise if ExportRoomsInView is false and section box is not active then every room is exported.
      /// However, if Room is set to "Not Exported" in IFC Option then none of the room will be exported whether ExportRoomsInView is true or not.
      /// </remarks>
      public bool ExportRoomsInView
      {
         get;
         set;
      }

      /// <summary>
      /// The active view
      /// </summary>
      public View ActiveView
      {
         get;
         set;
      }

      /// <summary>
      /// To check whether a specified IFC Entity is listed in the Exclude Filter (from configuration)
      /// </summary>
      /// <param name="entity">IFCEntityType enumeration representing the IFC entity concerned</param>
      /// <returns>true if the entity found in the set</returns>
      public bool IsElementInExcludeList(IFCEntityType entity)
      {
         return (ExcludeElementSet.Contains(entity.ToString()));
      }

      /// <summary>
      /// The exclude filter from the UI/configuration
      /// </summary>
      public string ExcludeFilter { get; set; }
      HashSet<string> _excludesElementSet = null;

      HashSet<string> ExcludeElementSet
      {
         get
         {
            if (_excludesElementSet != null)
               return _excludesElementSet;

            HashSet<string> exclSet = new HashSet<string>();
            if (!string.IsNullOrEmpty(ExcludeFilter))
            {
               string[] eList = ExcludeFilter.Split(';');
               foreach (string elem in eList)
                  exclSet.Add(elem);
            }
            _excludesElementSet = exclSet;
            return _excludesElementSet;
         }
      }
   }
}