//
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
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export Part element.
   /// </summary>
   class PartExporter
   {
      /// <summary>
      /// An enumeration to define what to export from the Parts
      /// </summary>
      public enum PartExportMode
      {
         Standard,
         AsBuildingElement,
         ShapeRepresentationOnly
      }

      /// <summary>
      /// Export all the parts of the host element.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="hostElement">The host element having parts to export.</param>
      /// <param name="hostHandle">The host element handle.</param>
      /// <param name="setMaterialNameToPartName">If set to true then generated parts will have names same as their material names.</param>
      public static void ExportHostPart(ExporterIFC exporterIFC, Element hostElement, IFCAnyHandle hostHandle,
          PlacementSetter placementSetter, IFCAnyHandle originalPlacement, ElementId overrideLevelId, bool setMaterialNameToPartName = false)
      {
         using (ProductWrapper subWrapper = ProductWrapper.Create(exporterIFC, true))
         {
            IFCExtrusionAxes ifcExtrusionAxes = GetDefaultExtrusionAxesForHost(hostElement);
            bool isWallOrColumn = IsHostWallOrColumn(hostElement);            

            if (ExporterCacheManager.TemporaryPartsCache.Find(hostElement.Id, out List<GeometryElement> partGeometries))
            {
               List<PartOrGeometry> partsOrGeometries = [];
               foreach (var partGeometry in partGeometries)
               {
                  partsOrGeometries.Add(new PartOrGeometry(partGeometry));
               }

               SplitAndExportParts(exporterIFC, partsOrGeometries, isGeometry:true, hostElement, subWrapper, placementSetter, originalPlacement,
                  isWallOrColumn, overrideLevelId, ifcExtrusionAxes, setMaterialNameToPartName);
            }
            else
            {
               List<ElementId> associatedPartsList = PartUtils.GetAssociatedParts(hostElement.Document, hostElement.Id, false, true).ToList();
               if (associatedPartsList.Count == 0)
                  return;

               List<PartOrGeometry> partsOrGeometries = [];
               foreach (var partId in associatedPartsList)
               {
                  Part associatedPart = hostElement.Document.GetElement(partId) as Part;
                  partsOrGeometries.Add(new PartOrGeometry(associatedPart));
               }

               SplitAndExportParts(exporterIFC, partsOrGeometries, isGeometry: false, hostElement, subWrapper, placementSetter, originalPlacement,
                  isWallOrColumn, overrideLevelId, ifcExtrusionAxes, setMaterialNameToPartName);
            }

            // Create the relationship of Host and Parts.
            HashSet<IFCAnyHandle> relatedElementIds = new HashSet<IFCAnyHandle>(subWrapper.GetAllObjects());
            if (relatedElementIds.Count > 0)
            {
               string guid = GUIDUtil.GenerateIFCGuidFrom(
                  GUIDUtil.CreateGUIDString(IFCEntityType.IfcRelAggregates, hostHandle));               
               IFCInstanceExporter.CreateRelAggregates(exporterIFC.GetFile(), guid, ExporterCacheManager.OwnerHistoryHandle, null, null, hostHandle, relatedElementIds);
            }
         }
      }

      private static void SplitAndExportParts(ExporterIFC exporterIFC, List<PartOrGeometry> partsOrGeometries, bool isGeometry,  Element hostElement, ProductWrapper subWrapper, PlacementSetter placementSetter,
         IFCAnyHandle originalPlacement, bool isWallOrColumn, ElementId overrideLevelId, IFCExtrusionAxes ifcExtrusionAxes, bool setMaterialNameToPartName)
      {
         bool hasOverrideLevel = overrideLevelId != null && overrideLevelId != ElementId.InvalidElementId;
         // Split parts if wall or column is split by level, and then export; otherwise, export parts normally.
         if (isWallOrColumn && hasOverrideLevel && ExporterCacheManager.ExportOptionsCache.WallAndColumnSplitting)
         {
            if (!ExporterCacheManager.HostPartsCache.HasRegistered(hostElement.Id))
            {
               SplitParts(hostElement, partsOrGeometries);
            }

            // Find and export the parts that are split by specific level.
            List<KeyValuePair<PartOrGeometry, IFCRange>> splitPartRangeList = [];
            splitPartRangeList = ExporterCacheManager.HostPartsCache.Find(hostElement.Id, overrideLevelId);

            if (splitPartRangeList != null)
            {
               int index = 1;
               foreach (KeyValuePair<PartOrGeometry, IFCRange> partRange in splitPartRangeList)
               {
                  if (isGeometry)
                  {
                     ExportDummyPart(exporterIFC, partRange.Key.GeometryElement, index++, subWrapper, placementSetter, originalPlacement,
                        partRange.Value, ifcExtrusionAxes, hostElement, overrideLevelId, PartExportMode.Standard, setMaterialNameToPartName);
                  }
                  else
                  {
                     ExportPart(exporterIFC, partRange.Key.Part, subWrapper, placementSetter, originalPlacement,
                        partRange.Value, ifcExtrusionAxes, hostElement, overrideLevelId, PartExportMode.Standard, setMaterialNameToPartName);
                  }
               }
            }
         }
         else
         {
            int index = 1;
            foreach (PartOrGeometry partOrGeometry in partsOrGeometries)
            {
               if (isGeometry)
               {
                  ExportDummyPart(exporterIFC, partOrGeometry.GeometryElement, index++, subWrapper, placementSetter, originalPlacement,
                     null, ifcExtrusionAxes, hostElement, overrideLevelId, PartExportMode.Standard, setMaterialNameToPartName);
               }
               else
               {
                  ExportPart(exporterIFC, partOrGeometry.Part, subWrapper, placementSetter, originalPlacement, null, ifcExtrusionAxes,
                     hostElement, overrideLevelId, PartExportMode.Standard, setMaterialNameToPartName);
               }
            }
         }
      }

      public static bool IsAnyHostElementLocal(Part partElement)
      {
         if (partElement == null)
            return false;

         Document doc = partElement.Document;
         foreach (LinkElementId linkElementId in partElement.GetSourceElementIds())
         {
            if (linkElementId.HostElementId == ElementId.InvalidElementId)
               continue;

            Element parentPartAsElement = doc.GetElement(linkElementId.HostElementId);
            if (parentPartAsElement == null)
               continue;

            Part parentPartAsPart = parentPartAsElement as Part;
            if (parentPartAsPart == null)
               return true;

            if (IsAnyHostElementLocal(parentPartAsPart))
               return true;
         }

         return false;
      }

      /// <summary>
      /// Export the standalone parts:
      ///     - The parts made from originals in Links 
      ///     - The Orphan parts: the linked file where the original host element comes from is unloaded.
      ///     - The Zombie parts: the original host element is deleted from the linked file.
      /// </summary>
      /// <remarks>
      /// This is a temporary workaround to export the parts made from linked elements. It should be refined when linked are supported (LinkedInstance at least.)
      /// There are some limitations:
      /// The linked element will not export as host, including the relative elements: e.g. windows, doors, openings.
      /// The host part cannot export if visibility is set by linked view and has 'Show Original'.
      /// The standalone part will skip export if Base Level is set 'Non Associated'.
      /// The linked part export cannot be split even if its category is wall or column and 'Split wall or column by story' is checked.
      /// </remarks>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="partElement">The standalone part to export.</param>
      /// <param name="geometryElement">The goemetry of the part.</param>
      /// <param name="productWrapper">The ProductWrapper object.</param>
      public static void ExportStandalonePart(ExporterIFC exporterIFC, Element partElement, GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         Part part = partElement as Part;
         if (!ExporterCacheManager.ExportOptionsCache.ExportParts || part == null || geometryElement == null)
            return;

         if (IsAnyHostElementLocal(part))
         {
            // Has host element, so should export with host element.
            return;
         }

         ElementId overrideLevelId = null;
         if (part.LevelId == ElementId.InvalidElementId)
         {
            // If part's level is not associated, try to get the host's level with the same category.
            Element hostElement = FindRootParent(part, part.OriginalCategoryId);
            overrideLevelId = hostElement?.LevelId ?? ElementId.InvalidElementId;
         }

         IFCExtrusionAxes ifcExtrusionAxes = GetDefaultExtrusionAxesForPart(part);
         ExportPart(exporterIFC, partElement, productWrapper, null, null, null, ifcExtrusionAxes, null,
            overrideLevelId, PartExportMode.Standard, setMaterialNameToPartName: false);
      }

      /// <summary>
      /// Export the parts as independent building elements. 
      /// </summary>
      /// <remarks>
      /// The function works with AlternateIFCUI and it requires two conditions:
      /// 1. Allows export parts: 'current view only' is checked and 'show parts' is selected.
      /// 2. Allows export parts independent: 'Export parts as building elements' is checked in alternate UI dialog.
      /// </remarks>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="partElement">The standalone part to export.</param>
      /// <param name="geometryElement">The goemetry of the part.</param>
      /// <param name="productWrapper">The ProductWrapper object.</param>
      public static void ExportPartAsBuildingElement(ExporterIFC exporterIFC, Element partElement, GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         Part part = partElement as Part;
         if (!ExporterCacheManager.ExportOptionsCache.ExportParts || part == null || geometryElement == null)
            return;

         bool isWall = part.OriginalCategoryId == new ElementId(BuiltInCategory.OST_Walls);
         bool isColumn = part.OriginalCategoryId == new ElementId(BuiltInCategory.OST_Columns);
         bool isWallOrColumn = isWall || isColumn;
         IFCExtrusionAxes ifcExtrusionAxes = GetDefaultExtrusionAxesForPart(part);

         Element hostElement = null;
         ElementId overrideLevelId = null;

         // Find the host element of the part.
         hostElement = FindRootParent(part, part.OriginalCategoryId);

         // If part's level is not associated, try to get the host's level with the same category.
         if (part.LevelId != null && part.LevelId != ElementId.InvalidElementId)
         {
            overrideLevelId = part.LevelId;
         }
         else if (hostElement != null)
         {
            overrideLevelId = hostElement.LevelId;
         }

         // Split parts with original category is wall or column and the option wall or column is split by level is checked, and then export; 
         // otherwise, export separate parts normally.
         if (isWallOrColumn && ExporterCacheManager.ExportOptionsCache.WallAndColumnSplitting)
         {
            IList<ElementId> levels = new List<ElementId>();
            IList<IFCRange> ranges = new List<IFCRange>();
            IFCEntityType exportType = isWall ? IFCEntityType.IfcWall : IFCEntityType.IfcColumn;
            IFCExportInfoPair exportInfo = new IFCExportInfoPair(exportType);
            LevelUtil.CreateSplitLevelRangesForElement(exportInfo, part, out levels, out ranges);
            if (ranges.Count == 0)
            {
               PartExporter.ExportPart(exporterIFC, partElement, productWrapper, null, null, null, ifcExtrusionAxes, hostElement,
                  overrideLevelId, PartExportMode.AsBuildingElement, setMaterialNameToPartName: false);
            }
            else
            {
               for (int ii = 0; ii < ranges.Count; ii++)
               {
                  PartExporter.ExportPart(exporterIFC, partElement, productWrapper, null, null, ranges[ii], ifcExtrusionAxes,
                     hostElement, levels[ii], PartExportMode.AsBuildingElement, setMaterialNameToPartName: false);
               }
            }
         }
         else
            PartExporter.ExportPart(exporterIFC, partElement, productWrapper, null, null, null, ifcExtrusionAxes, hostElement,
               overrideLevelId, PartExportMode.AsBuildingElement, setMaterialNameToPartName: false);
      }

      private static IFCAnyHandle ExportPartCommon(ExporterIFC exporterIFC, Element partElement,
         GeometryElement dummyPartGeometry, int dummyPartGeometryIndex, ProductWrapper productWrapper,
         PlacementSetter placementSetter, IFCAnyHandle originalPlacement, IFCRange range,
         IFCExtrusionAxes ifcExtrusionAxes, Element hostElement, ElementId overrideLevelId, PartExportMode exportMode,
         bool setMaterialNameToPartName)
      {
         IFCAnyHandle shapeRepresentation = null;
         bool isDummyPart = partElement == null;
         
         Part part = null;
         ElementId partId = null;

         if (!isDummyPart)
         {
            if (!ElementFilteringUtil.IsElementVisible(partElement))
               return null;

            part = partElement as Part;
            if (part == null)
               return null;

            partId = part.Id;
         }
         else if (dummyPartGeometry == null)
         {
            return null;
         }

         // We don't know how to export a part as a building element if we don't know it's host.
         if ((exportMode == PartExportMode.AsBuildingElement) && (hostElement == null))
            return null;

         IFCExportInfoPair exportType = null;

         switch (exportMode)
         {
            case PartExportMode.Standard:
               {
                  // Check the intended IFC entity or type name is in the exclude list specified in the UI
                  IFCEntityType elementClassTypeEnum = IFCEntityType.IfcBuildingElementPart;
                  if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
                     return null;
                  break;
               }
            case PartExportMode.AsBuildingElement:
            case PartExportMode.ShapeRepresentationOnly:
               {
                  exportType = ExporterUtil.GetProductExportType(hostElement, out _);

                  // Check the intended IFC entity or type name is in the exclude list specified in the UI
                  IFCEntityType elementClassTypeEnum;
                  if (Enum.TryParse(exportType.ExportInstance.ToString(), out elementClassTypeEnum)
                     || Enum.TryParse(exportType.ExportType.ToString(), out elementClassTypeEnum))
                     if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
                        return null;
                  break;
               }
         }

         PlacementSetter partPlacementSetter = null;

         // If standaloneExport is true and part is null, we expect hostElement and dummyPartGeometry to be non-null.
         bool standaloneExport = (hostElement == null) || (exportMode == PartExportMode.AsBuildingElement);
         Element elementToUse = part ?? hostElement;
         if (elementToUse == null)
         {
            return null;
         }
         Document document = elementToUse.Document;

         ElementId partExportLevelId = (overrideLevelId != null) ? overrideLevelId : null;

         if (!isDummyPart && partExportLevelId == null && standaloneExport)
            partExportLevelId = part.LevelId;

         if (partExportLevelId == null || partExportLevelId == ElementId.InvalidElementId)
         {
            ElementId hostCategoryId = CategoryUtil.GetSafeCategoryId(hostElement);
            if (hostElement != null && ((part?.OriginalCategoryId ?? hostCategoryId) != hostCategoryId))
               return null;
            partExportLevelId = hostElement?.LevelId;
         }

         GeometryElement geometryElement = dummyPartGeometry;
         if (!isDummyPart)
         {
            if (ExporterCacheManager.PartExportedCache.HasExported(partId, partExportLevelId) &&
               (exportMode != PartExportMode.ShapeRepresentationOnly))
               return null;

            Options options = GeometryUtil.GetIFCExportGeometryOptions();
            View ownerView = document.GetElement(part.OwnerViewId) as View;
            if (ownerView != null)
               options.View = ownerView;

            geometryElement = part.get_Geometry(options);
         }

         if (geometryElement == null)
            return null;
         
         try
         {
            IFCFile file = exporterIFC.GetFile();
            using (IFCTransaction transaction = new IFCTransaction(file))
            {
               IFCAnyHandle partPlacement = null;
               Transform hostTrf = Transform.Identity;
               if (standaloneExport)
               {
                  Transform orientationTrf = Transform.Identity;
                  BoundingBoxXYZ partBbox = part?.get_BoundingBox(null) ?? dummyPartGeometry.GetBoundingBox();

                  if (partBbox != null)
                  {
                     orientationTrf.Origin = new XYZ(0, 0, partBbox.Min.Z);
                  }

                  IFCAnyHandle overrideContainerHnd = null;
                  ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(part, out overrideContainerHnd);
                  if (overrideContainerId != ElementId.InvalidElementId &&
                     (partExportLevelId == null || partExportLevelId == ElementId.InvalidElementId))
                  {
                     partExportLevelId = overrideContainerId;
                  }

                  partPlacementSetter = PlacementSetter.Create(exporterIFC, elementToUse, null, orientationTrf, partExportLevelId, overrideContainerHnd);
                  partPlacement = partPlacementSetter.LocalPlacement;
               }
               else
               {
                  // This part needs explanation:
                  // The geometry of the Part is against the Project base, while the host element already contains all the IFC transforms relative to its container
                  // To "correct" the placement so that the Part is correctly relative to the host, we need to inverse transform the Part to the host's placement 
                  IFCAnyHandle hostHandle = ExporterCacheManager.ElementToHandleCache.Find(hostElement.Id);
                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(hostHandle))
                  {
                     if (originalPlacement == null)
                     {
                        originalPlacement = IFCAnyHandleUtil.GetObjectPlacement(hostHandle);
                     }
                     hostTrf = ExporterUtil.GetTransformFromLocalPlacementHnd(originalPlacement, true);

                     geometryElement = SolidMeshGeometryInfo.GetTransformedGeometry(geometryElement, hostTrf.Inverse,
                        ExporterCacheManager.AllocatedGeometryObjectCache);
                  }
                  
                  // The host placement setter has registered the transform. Since the part geometry is transformed relative to the host, 
                  //   the placement for the part needs to be inversed
                  if (hostElement is FamilyInstance)
                  {
                     partPlacementSetter = PlacementSetter.Create(exporterIFC, elementToUse, hostTrf.Inverse, Transform.Identity, partExportLevelId, null);
                  }
                  partPlacement = ExporterUtil.CreateLocalPlacement(file, originalPlacement, null);
               }

               bool validRange = range != null && !MathUtil.IsAlmostZero(range.Start - range.End);

               SolidMeshGeometryInfo solidMeshInfo;
               if (validRange)
               {
                  solidMeshInfo = GeometryUtil.GetSplitClippedSolidMeshGeometry(geometryElement, range);
                  if (solidMeshInfo.GetSolids().Count == 0 && solidMeshInfo.GetMeshes().Count == 0)
                     return null;
               }
               else
               {
                  solidMeshInfo = GeometryUtil.GetSplitSolidMeshGeometry(geometryElement);
               }

               using (IFCExportBodyParams extrusionCreationData = new IFCExportBodyParams())
               {
                  extrusionCreationData.SetLocalPlacement(partPlacement);
                  extrusionCreationData.ReuseLocalPlacement = false;
                  extrusionCreationData.PossibleExtrusionAxes = ifcExtrusionAxes;

                  IList<Solid> solids = solidMeshInfo.GetSolids();
                  IList<Mesh> meshes = solidMeshInfo.GetMeshes();
                  IList<GeometryObject> gObjs = FamilyExporterUtil.RemoveInvisibleSolidsAndMeshes(document, 
                     exporterIFC, ref solids, ref meshes);

                  ElementId hostCatId = CategoryUtil.GetSafeCategoryId(hostElement);
                  ElementId catId = isDummyPart ? hostCatId : CategoryUtil.GetSafeCategoryId(part);

                  BodyData bodyData = null;
                  BodyExporterOptions bodyExporterOptions = new(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                  bodyExporterOptions.ExtrusionLocalCoordinateSystem = Transform.Identity;
                  if (solids.Count > 0 || meshes.Count > 0)
                  {
                     bodyData = BodyExporter.ExportBody(exporterIFC, elementToUse, catId, ElementId.InvalidElementId, solids, meshes,
                         bodyExporterOptions, extrusionCreationData);
                  }
                  else
                  {
                     IList<GeometryObject> geomlist = new List<GeometryObject>() { geometryElement };
                     bodyData = BodyExporter.ExportBody(exporterIFC, elementToUse, catId, ElementId.InvalidElementId, geomlist,
                         bodyExporterOptions, extrusionCreationData);
                  }

                  if (exportMode != PartExportMode.ShapeRepresentationOnly)
                  {
                     IFCAnyHandle bodyRep = bodyData.RepresentationHnd;
                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                     {
                        extrusionCreationData.ClearOpenings();
                        return null;
                     }

                     List<IFCAnyHandle> representations = [bodyRep];

                     IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geometryElement, Transform.Identity);
                     if (boundingBoxRep != null)
                        representations.Add(boundingBoxRep);

                     IFCAnyHandle prodRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);

                     IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

                     // Try to get the export type from the part's built-in parameters, and if that fails, from the host element.
                     if (hostElement == null)
                     {
                        exportType = ExporterUtil.GetProductExportType(part, out _);
                     }
                     else
                     {
                        IFCExportInfoPair exportTypeFromParam = ExporterUtil.GetExportTypeFromParameters(elementToUse, IFCEntityType.IfcProduct);
                        if (exportTypeFromParam.IsUnKnown)
                        {
                           exportType ??= ExporterUtil.GetProductExportType(hostElement, out _);
                        }
                        else
                        {
                           exportType = exportTypeFromParam;
                        }
                     }

                     string ifcEnumType = exportType.GetPredefinedTypeOrDefault();
                     string partGUID = null;
                     if (isDummyPart)
                     {
                        partGUID = GUIDUtil.GenerateIFCGuidFrom(GUIDUtil.CreateGUIDString(hostElement, "TemporaryPart:" + dummyPartGeometryIndex));
                     }
                     else
                     {
                        partGUID = GUIDUtil.GenerateIFCGuidFrom(part, exportType);
                     }

                     IFCAnyHandle ifcPart = null;
                     if (exportMode != PartExportMode.AsBuildingElement)
                     {
                        ifcPart = IFCInstanceExporter.CreateBuildingElementPart(exporterIFC, elementToUse, partGUID, ownerHistory,
                            extrusionCreationData.GetLocalPlacement(), prodRep);
                     }
                     else
                     {
                        switch (exportType.ExportInstance)
                        {
                           case IFCEntityType.IfcColumn:
                              ifcPart = IFCInstanceExporter.CreateColumn(exporterIFC, elementToUse, partGUID, ownerHistory,
                                  extrusionCreationData.GetLocalPlacement(), prodRep, ifcEnumType);
                              break;
                           case IFCEntityType.IfcCovering:
                              ifcPart = IFCInstanceExporter.CreateCovering(exporterIFC, elementToUse, partGUID, ownerHistory,
                                  extrusionCreationData.GetLocalPlacement(), prodRep, ifcEnumType);
                              break;
                           case IFCEntityType.IfcFooting:
                              ifcPart = IFCInstanceExporter.CreateFooting(exporterIFC, elementToUse, partGUID, ownerHistory,
                                  extrusionCreationData.GetLocalPlacement(), prodRep, ifcEnumType);
                              break;
                           case IFCEntityType.IfcPile:
                              ifcPart = IFCInstanceExporter.CreatePile(exporterIFC, elementToUse, partGUID, ownerHistory,
                                  extrusionCreationData.GetLocalPlacement(), prodRep, ifcEnumType, null);
                              break;
                           case IFCEntityType.IfcRoof:
                              ifcPart = IFCInstanceExporter.CreateRoof(exporterIFC, elementToUse, partGUID, ownerHistory,
                                  extrusionCreationData.GetLocalPlacement(), prodRep, ifcEnumType);
                              break;
                           case IFCEntityType.IfcSlab:
                              {
                                 // TODO: fix this elsewhere.
                                 if (ExporterUtil.IsNotDefined(ifcEnumType))
                                 {
                                    if (hostCatId == new ElementId(BuiltInCategory.OST_Floors))
                                       ifcEnumType = "FLOOR";
                                    else if (hostCatId == new ElementId(BuiltInCategory.OST_Roofs))
                                       ifcEnumType = "ROOF";
                                 }

                                 ifcPart = IFCInstanceExporter.CreateSlab(exporterIFC, elementToUse, partGUID, ownerHistory,
                                     extrusionCreationData.GetLocalPlacement(), prodRep, ifcEnumType);
                              }
                              break;
                           case IFCEntityType.IfcWall:
                              ifcPart = IFCInstanceExporter.CreateWall(exporterIFC, elementToUse, partGUID, ownerHistory,
                              extrusionCreationData.GetLocalPlacement(), prodRep, ifcEnumType);
                              break;
                           default:
                              ifcPart = IFCInstanceExporter.CreateBuildingElementProxy(exporterIFC, elementToUse, partGUID, ownerHistory,
                              extrusionCreationData.GetLocalPlacement(), prodRep, ifcEnumType);
                              break;
                        }
                     }

                     if (setMaterialNameToPartName && (bodyData?.MaterialIds?.Count ?? 0) > 0)
                     {
                        Material material = document.GetElement(bodyData.MaterialIds[0]) as Material;
                        if (material != null)
                        {
                           IFCAnyHandleUtil.OverrideNameAttribute(ifcPart, NamingUtil.GetMaterialLayerName(material));
                        }
                     }

                     bool containedInLevel = standaloneExport;
                     PlacementSetter whichPlacementSetter = containedInLevel ? partPlacementSetter : placementSetter;
                     if (!isDummyPart)
                     {
                        productWrapper.AddElement(part, ifcPart, whichPlacementSetter, extrusionCreationData, containedInLevel, exportType);

                        OpeningUtil.CreateOpeningsIfNecessary(ifcPart, part, extrusionCreationData, bodyData.OffsetTransform, exporterIFC,
                            extrusionCreationData.GetLocalPlacement(), whichPlacementSetter, productWrapper);

                        //Add the exported part to exported cache.
                        TraceExportedParts(part, partExportLevelId, standaloneExport ? ElementId.InvalidElementId : hostElement.Id);

                        CategoryUtil.CreateMaterialAssociation(exporterIFC, part, ifcPart, bodyData.MaterialIds);
                     }
                     else
                     {
                        productWrapper.AddPart(ifcPart, whichPlacementSetter, extrusionCreationData, containedInLevel);
                        if (ExporterCacheManager.TemporaryPartsCache.FindInfo(geometryElement, out TemporaryPartInfo partInfo))
                        {
                           OpeningUtil.CreateOpeningsIfNecessary(ifcPart, elementToUse, partInfo, extrusionCreationData, bodyData.OffsetTransform, exporterIFC,
                            extrusionCreationData.GetLocalPlacement(), whichPlacementSetter, productWrapper);
                           
                           CategoryUtil.CreateMaterialAssociation(exporterIFC, ifcPart, bodyData.MaterialIds, partInfo.MaterialToVolumeMap);

                           foreach (IFCAnyHandle propertySet in partInfo.InternalPropertySets)
                           {
                              HashSet<IFCAnyHandle> relatedObjects = new HashSet<IFCAnyHandle>() { ifcPart };
                              ExporterUtil.CreateRelDefinesByProperties(file, ExporterCacheManager.OwnerHistoryHandle, null, null, relatedObjects, propertySet);
                           }
                        }
                     }
                  }
                  else
                  {
                     // Special case for IFC4RV to export Element with material layer on its layer components as separate items
                     shapeRepresentation = bodyData.RepresentationHnd;
                  }
               }

               transaction.Commit();
            }
         }
         finally
         {
            if (partPlacementSetter != null)
               partPlacementSetter.Dispose();
         }

         return shapeRepresentation;
      }

      /// <summary>
      /// Export the individual part (IfcBuildingElementPart).
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="partElement">The part element.</param>
      /// <param name="productWrapper">The ProductWrapper object.</param>
      /// <param name="placementSetter">The PlacementSetter object.</param>
      /// <param name="originalPlacement">The local placement of the host.</param>
      /// <param name="range">The optional range, if the part will be split.</param>
      /// <param name="ifcExtrusionAxes">The directions to extrude the geometry, if possible.</param>
      /// <param name="hostElement">The optional host of the part.</param>
      /// <param name="overrideLevelId">The id of the level that the part is one, overridding other sources.</param>
      /// <param name="exportMode">Which entities to use when exporting the part.</param>
      /// <param name="setMaterialNameToPartName">If set to true then generated part will have name same as its material name.</param>
      public static IFCAnyHandle ExportPart(ExporterIFC exporterIFC, Element partElement,
         ProductWrapper productWrapper,
         PlacementSetter placementSetter, IFCAnyHandle originalPlacement, IFCRange range,
         IFCExtrusionAxes ifcExtrusionAxes, Element hostElement, ElementId overrideLevelId, PartExportMode exportMode,
         bool setMaterialNameToPartName)
      {
         return ExportPartCommon(exporterIFC, partElement, null, -1, productWrapper, placementSetter, originalPlacement,
            range, ifcExtrusionAxes, hostElement, overrideLevelId, exportMode, setMaterialNameToPartName);
      }

      /// <summary>
      /// Export the individual part (IfcBuildingElementPart by default).
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="dummyPartGeometry">The geometry of the virtual part.</param>
      /// <param name="dummyPartGeometryIndex">The index of the geometry of the virtual part, used to generate a GUID.</param>
      /// <param name="productWrapper">The ProductWrapper object.</param>
      /// <param name="placementSetter">The PlacementSetter object.</param>
      /// <param name="originalPlacement">The local placement of the host.</param>
      /// <param name="range">The optional range, if the part will be split.</param>
      /// <param name="ifcExtrusionAxes">The directions to extrude the geometry, if possible.</param>
      /// <param name="hostElement">The non-optional host of the virtual part.</param>
      /// <param name="overrideLevelId">The id of the level that the part is one, overridding other sources.</param>
      /// <param name="exportMode">Which entities to use when exporting the virtual part.</param>
      /// <param name="setMaterialNameToPartName">If set to true then generated part will have name same as its material name.</param>
      private static IFCAnyHandle ExportDummyPart(ExporterIFC exporterIFC, 
         GeometryElement dummyPartGeometry, int dummyPartGeometryIndex, ProductWrapper productWrapper,
         PlacementSetter placementSetter, IFCAnyHandle originalPlacement, IFCRange range,
         IFCExtrusionAxes ifcExtrusionAxes, Element hostElement, ElementId overrideLevelId, PartExportMode exportMode,
         bool setMaterialNameToPartName)
      {
         return ExportPartCommon(exporterIFC, null, dummyPartGeometry, dummyPartGeometryIndex, productWrapper,
            placementSetter, originalPlacement, range, ifcExtrusionAxes, hostElement, overrideLevelId, exportMode,
            setMaterialNameToPartName);
      }

      public static bool GetLayerIndex(Element part, out int layerIndex)
      {
         layerIndex = -1;
         string layerIndexStr;
         if (ParameterUtil.GetStringValueFromElement(part, BuiltInParameter.DPART_LAYER_INDEX, out layerIndexStr) != null)
         {
            int index = int.Parse(layerIndexStr) - 1; //The index starts at 1
            if (index >= 0)
            {
               layerIndex = index;
               return true;
            }
         }

         return false;
      }

      private static void AddGeometries(ExporterIFC exporterIFC, Element part, IFCRange range,
         ref List<GeometryObject> geometryObjects, IList<Solid> solidsToExclude)
      {
         Options options = GeometryUtil.GetIFCExportGeometryOptions();
         GeometryElement geometryElement = part.get_Geometry(options);
         if (geometryElement == null)
            return;

         AddGeometriesFromGeometryElement(exporterIFC, part.Document, geometryElement, range, ref geometryObjects, solidsToExclude);
      }

      private static void AddGeometriesFromGeometryElement(ExporterIFC exporterIFC, Document doc, GeometryElement geometryElement, IFCRange range,
         ref List<GeometryObject> geometryObjects, IList<Solid> solidsToExclude)
      {
         // GetSplitClippedSolidMeshGeometry will call GetSplitSolidMeshGeometry is range is null.
         SolidMeshGeometryInfo solidMeshInfo;
         if (range == null)
            solidMeshInfo = GeometryUtil.GetSplitSolidMeshGeometry(geometryElement);
         else
            solidMeshInfo = GeometryUtil.GetSplitClippedSolidMeshGeometry(geometryElement, range);

         if (solidMeshInfo.GetSolids().Count == 0 && solidMeshInfo.GetMeshes().Count == 0)
            return;

         IList<Solid> solids = solidMeshInfo.GetSolids();
         IList<Mesh> meshes = solidMeshInfo.GetMeshes();
         geometryObjects.AddRange(FamilyExporterUtil.RemoveInvisibleSolidsAndMeshes(doc, exporterIFC, ref solids, ref meshes, solidsToExclude));
      }

      private static ICollection<ElementId> m_AssociatedPartsCache = null;

      private static void ClearAssociatedParts()
      {
         m_AssociatedPartsCache = null;
      }

      public static ICollection<ElementId> GetAssociatedParts(Document document, ElementId elementId)
      {
         m_AssociatedPartsCache ??= PartUtils.GetAssociatedParts(document, elementId, false, true);
         return m_AssociatedPartsCache;
      }

      private static void AddGeometriesFromPartOrGeometry(ExporterIFC exporterIFC, PartOrGeometry partOrGeometry, IFCRange range,
         Element hostElement, IList<Solid> solidsToExclude, ref List<GeometryObject> geometryObjects, ref List<int> partMaterialLayerIndexList)
      {
         if (partOrGeometry.IsGeometry)
         {
            if (ExporterCacheManager.TemporaryPartsCache.FindInfo(partOrGeometry.GeometryElement, out TemporaryPartInfo partInfo))
               partMaterialLayerIndexList.Add(partInfo.LayerIndex);

            AddGeometriesFromGeometryElement(exporterIFC, hostElement.Document, partOrGeometry.GeometryElement, range, ref geometryObjects, solidsToExclude);
         }
         else
         {
            if (GetLayerIndex(partOrGeometry.Part, out int layerIndex))
               partMaterialLayerIndexList.Add(layerIndex);

            AddGeometries(exporterIFC, partOrGeometry.Part, range, ref geometryObjects, solidsToExclude);
         }
      }

      private static void SplitAndAddPartsGeometry(ExporterIFC exporterIFC, List<PartOrGeometry> partsOrGeometries, bool isGeometry, Element hostElement,
         ElementId overrideLevelId, IList<Solid> solidsToExclude, ref List<GeometryObject> geometryObjects, out List<int> partMaterialLayerIndexList)
      {
         partMaterialLayerIndexList = [];
         if (partsOrGeometries.Count == 0)
            return;

         bool isSplitWall = (hostElement is Wall) && ExporterCacheManager.ExportOptionsCache.WallAndColumnSplitting;

         SplitParts(hostElement, partsOrGeometries); // Split parts and associate them with host. 

         // Find and export the parts that are split by specific level.
         List<KeyValuePair<PartOrGeometry, IFCRange>> splitPartRangeList = [];
         splitPartRangeList = ExporterCacheManager.HostPartsCache.Find(hostElement.Id, overrideLevelId);

         if (splitPartRangeList != null)
         {
            foreach (KeyValuePair<PartOrGeometry, IFCRange> partRange in splitPartRangeList)
            {
               IFCRange range = partRange.Value;
               bool validRange = (range != null && !MathUtil.IsAlmostZero(range.Start - range.End));
               if (validRange)
               {
                  PartOrGeometry partOrGeometry = partRange.Key;
                  AddGeometriesFromPartOrGeometry(exporterIFC, partOrGeometry, range, hostElement, solidsToExclude,
                     ref geometryObjects, ref partMaterialLayerIndexList);
               }
            }
         }
         // If it is a split Wall, it should not come here. It may come here because the Wall is clipped 
         //   and therefore should not be processed if it does not return splitPartRangeList
         else if (!isSplitWall)
         {
            foreach (PartOrGeometry partOrGeometry in partsOrGeometries)
            {
               AddGeometriesFromPartOrGeometry(exporterIFC, partOrGeometry, range: null, hostElement, solidsToExclude,
                  ref geometryObjects, ref partMaterialLayerIndexList);
            }
         }
      }

      /// <summary>
      /// Export parts for IFC4RV. This will export the individual part representations as IfcShapeAspect, and return the main shape representation handle
      /// </summary>
      /// <returns>The host shape representation with multiple items from its parts.</returns>
      public static IFCAnyHandle ExportHostPartAsShapeAspects(ExporterIFC exporterIFC, Element hostElement, 
         IFCAnyHandle hostProdDefShape, ElementId overrideLevelId, MaterialLayerSetInfo layersetInfo, 
         IFCExportBodyParams extrusionCreationData, IList<Solid> solidsToExclude = null)
      {
         IFCAnyHandle hostShapeRep = null;
         IList<ElementId> materialIdsFromBodyData = new List<ElementId>();
         IFCFile file = exporterIFC.GetFile();
         BodyData bodyData = null;

         bool partIsNotObtainable = false;
         List<int> partMaterialLayerIndexList = [];
         List<MaterialLayerSetInfo.MaterialInfo> layersetInfoList = [];

         ClearAssociatedParts();

         Document document = hostElement.Document;
         List<GeometryObject> geometryObjects = [];

         if (ExporterCacheManager.TemporaryPartsCache.Find(hostElement.Id, out List<GeometryElement> geometryElements))
         {
            List<PartOrGeometry> partsOrGeometries = [];
            foreach (var partGeometry in geometryElements)
            {
               partsOrGeometries.Add(new PartOrGeometry(partGeometry));
            }
            SplitAndAddPartsGeometry(exporterIFC, partsOrGeometries, isGeometry: true, hostElement,
               overrideLevelId, solidsToExclude, ref geometryObjects, out partMaterialLayerIndexList);
         }
         else
         {
            if (ElementCanHaveMultipleComponents(hostElement))
            {
               List<ElementId> associatedPartsList = GetAssociatedParts(document, hostElement.Id).ToList();

               if (associatedPartsList.Count > 0)
               {
                  List<PartOrGeometry> partsOrGeometries = [];
                  foreach (var partId in associatedPartsList)
                  {
                     Part associatedPart = document.GetElement(partId) as Part;
                     partsOrGeometries.Add(new PartOrGeometry(associatedPart));
                  }
                  SplitAndAddPartsGeometry(exporterIFC, partsOrGeometries, isGeometry: false, hostElement,
                     overrideLevelId, solidsToExclude, ref geometryObjects, out partMaterialLayerIndexList);
               }
               else
               {
                  // Getting the Part seems to have problem (no Part obtained). We will then use the original geometry of the object
                  partIsNotObtainable = true;
                  geometryObjects.AddRange(GeomObjectsFromOriginalGeometry(exporterIFC, hostElement));
               }
            }
            else
            {
               // Return nothing if there is no material layer
               geometryObjects = [.. GeomObjectsFromOriginalGeometry(exporterIFC, hostElement)];
            }
         }

         hostShapeRep = ShapeRepFromListOfGeomObjects(exporterIFC, hostElement, geometryObjects, ref bodyData, ref materialIdsFromBodyData, extrusionCreationData);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(hostShapeRep))
            return null;

         HashSet<IFCAnyHandle> itemReps = IFCAnyHandleUtil.GetItems(hostShapeRep);
         IFCRepresentationIdentifier shapeIdent = IFCRepresentationIdentifier.Body;
         IFCAnyHandle contextOfItems = ExporterCacheManager.Get3DContextHandle(shapeIdent);
         string representationType = IFCAnyHandleUtil.GetRepresentationType(hostShapeRep);
         // If material layer indices are available, we can use the material sequence as is, 
         // but otherwise layersetInfoList must be collected manually in the right order using a geometric method
         if (partMaterialLayerIndexList.Count > 0)
         {
            layersetInfoList = layersetInfo.MaterialIds;
         }
         else
         {
            if (partIsNotObtainable || layersetInfo.IsEmpty)
            {
               if (partIsNotObtainable && layersetInfo != null)
               {
                  // Since Part cannot be obtained the geometry will be only one from the original object. In this case we will take only the first
                  // material. It is not very correct, but it will produce consistent output

                  var matList = (from x in layersetInfo.MaterialIds
                                 where !MathUtil.IsAlmostZero(x.Width)
                                 select new MaterialLayerSetInfo.MaterialInfo(x.BaseMatId, x.LayerName, x.Width, x.Function)).ToList();
                  if ((matList?.Count ?? 0) > 0)
                  {
                     layersetInfoList.Add(matList[0]);   // add only the first non-zero thickness
                  }
               }
               else
               {
                  ElementId matId = ElementId.InvalidElementId;
                  if (materialIdsFromBodyData != null && materialIdsFromBodyData.Count > 0)
                  {
                     matId = materialIdsFromBodyData.First();
                  }
                  Double width = 0.0;
                  if (extrusionCreationData != null)
                  {
                     width = extrusionCreationData.ScaledLength;
                  }
                  layersetInfo.SingleMaterialOverride(matId, width);
                  layersetInfoList.Add(layersetInfo.MaterialIds[0]);
               }
            }
            else
            {
               IList<ElementId> matInfoList = layersetInfo.MaterialIds.Where(x => !MathUtil.IsAlmostZero(x.Width)).Select(x => x.BaseMatId).ToList();
               // There is a chance that the material list is already correct or just in reverse order, so handle it before moving on to manual sequencing
               MaterialLayerSetInfo.CompareTwoLists compStat = MaterialLayerSetInfo.CompareMaterialInfoList(bodyData.MaterialIds, matInfoList);
               switch (compStat)
               {
                  case MaterialLayerSetInfo.CompareTwoLists.ListsSequentialEqual:
                     {
                        layersetInfoList = new List<MaterialLayerSetInfo.MaterialInfo>(layersetInfo.MaterialIds);
                        break;
                     }
                  case MaterialLayerSetInfo.CompareTwoLists.ListsReversedEqual:
                     {
                        layersetInfoList = new List<MaterialLayerSetInfo.MaterialInfo>(layersetInfo.MaterialIds);
                        layersetInfoList.Reverse();
                        break;
                     }
                  case MaterialLayerSetInfo.CompareTwoLists.ListsUnequal:
                     {
                        // Try manually collecting the material info list in the correct order
                        ICollection<ElementId> associatedPartsList = GetAssociatedParts(hostElement.Document, hostElement.Id);
                        layersetInfoList = CollectMaterialInfoList(hostElement, associatedPartsList, layersetInfo).ToList();

                        // There is a chance that the manually collected list will be reversed or incorrect, so check it again
                        IList<ElementId> newMatInfoList = layersetInfoList.Where(x => !MathUtil.IsAlmostZero(x.Width)).Select(x => x.BaseMatId).ToList();
                        compStat = MaterialLayerSetInfo.CompareMaterialInfoList(newMatInfoList, matInfoList);
                        if (compStat == MaterialLayerSetInfo.CompareTwoLists.ListsReversedEqual)
                           layersetInfoList.Reverse();
                        break;
                     }
                  default:
                     break;
               }

               if (compStat == MaterialLayerSetInfo.CompareTwoLists.ListsUnequal)
               {
                  // We still cannot match the layer, it could be no layer. Use only the first material
                  MaterialLayerSetInfo.MaterialInfo layer1 = layersetInfo.MaterialIds[0];
                  layersetInfo.SingleMaterialOverride(layer1.BaseMatId, layer1.Width);
                  layersetInfoList.Add(layer1);
               }
            }
         }

         // Scan through the prodReps and remove any existing "Body" rep if it is already in there, if there is, it will be removed and replaced with the parts
         List<IFCAnyHandle> prodReps;
         if (hostProdDefShape == null)
         {
            hostProdDefShape = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, null);
            prodReps = new List<IFCAnyHandle>();
         }
         else
         {
            prodReps = IFCAnyHandleUtil.GetRepresentations(hostProdDefShape);
         }

         int repToRemove = -1;
         for (int rCnt = 0; rCnt < prodReps.Count; ++rCnt)
         {
            if (IFCAnyHandleUtil.GetRepresentationIdentifier(prodReps[rCnt]).Equals("Body"))
            {
               repToRemove = rCnt;
               break;
            }
         }
         if (repToRemove < prodReps.Count && repToRemove > -1)
            prodReps.RemoveAt(repToRemove);

         // Add the body from the parts to replace the Body representation
         prodReps.Add(hostShapeRep);
         IFCAnyHandleUtil.SetAttribute(hostProdDefShape, "Representations", prodReps);

         // Create IfcShapeAspects for each of the ShapeRepresentation keeping UI order
         int partMatLayIdxCount = partMaterialLayerIndexList.Count;
         int layerSetInfoCount = layersetInfoList.Count;
         int cnt = 0;

         SortedDictionary<int, HashSet<IFCAnyHandle>> layerIndexToRepresentationsMap = new();
         foreach (IFCAnyHandle itemRep in itemReps)
         {
            int layerInfoIdx = -1;

            if (partMatLayIdxCount > 0 && cnt < partMatLayIdxCount)
            {
               if (partMaterialLayerIndexList[cnt] < layerSetInfoCount)
                  layerInfoIdx = partMaterialLayerIndexList[cnt];
            }
            else
            {
               if (cnt < layerSetInfoCount)
                  layerInfoIdx = cnt;
            }

            if (!layerIndexToRepresentationsMap.TryGetValue(layerInfoIdx, out HashSet <IFCAnyHandle> layerRepresentations))
            {
               layerRepresentations = new();
               layerIndexToRepresentationsMap.Add(layerInfoIdx, layerRepresentations);
            }
            layerRepresentations.Add(itemRep);

            cnt++;
         }
         
         HashSet<string> shapeAspectNameUsed = new HashSet<string>();
         foreach (var indexToRepresentationsPair in layerIndexToRepresentationsMap)
         {
            int layerIndex = indexToRepresentationsPair.Key;

            string shapeAspectName = (layerIndex != -1) ? layersetInfoList[layerIndex].ShapeAspectName : "Layer";
            if (string.IsNullOrEmpty(shapeAspectName))
               shapeAspectName = "Layer";

            // Ensure IfcShapeAspect name is unique and save it for consistensy to IfcMaterialConstituent.
            shapeAspectName = NamingUtil.GetUniqueNameWithinSet(shapeAspectName, shapeAspectNameUsed);
            if (layerIndex >= 0 && layerIndex < layerSetInfoCount)
            {
               layersetInfoList[layerIndex].ShapeAspectName = shapeAspectName;
            }

            RepresentationUtil.CreateRepForShapeAspect(exporterIFC, hostElement, hostProdDefShape, representationType,
               shapeAspectName, indexToRepresentationsPair.Value);
         }

         return hostShapeRep;
      }

      private static IList<MaterialLayerSetInfo.MaterialInfo> CollectMaterialInfoList(Element hostElement, 
         ICollection<ElementId> associatedPartsList, MaterialLayerSetInfo layersetInfo)
      {
         List<MaterialLayerSetInfo.MaterialInfo> layersetInfoList = [];

         if (hostElement is Wall)
         {
            Wall wall = hostElement as Wall;
            Curve locationCurve = ExporterIFCUtils.GetWallTrimmedCurve(wall) ?? WallExporter.GetWallAxis(wall);

            double curveParam = (locationCurve.GetEndParameter(1) - locationCurve.GetEndParameter(0)) * 0.5;
            Transform derivs = locationCurve.ComputeDerivatives(curveParam, false/*normalized*/);
            if (derivs.BasisX.IsZeroLength())
               return null;

            XYZ rightVec = derivs.BasisX.Normalize().CrossProduct(XYZ.BasisZ);
            XYZ rightLineOrigin = derivs.Origin;

            // Faces (and their widths) sorted by the parameter on the line that intersects them perpendicularly from left to right
            SortedList<double, Tuple<double, Face>> sortedFaces = new SortedList<double, Tuple<double, Face>>(Comparer<double>.Create((x, y) =>
            {
               if (MathUtil.IsAlmostEqual(x, y))
                  return 0;
               else if (x < y)
                  return -1;
               else
                  return 1;
            }));

            foreach (ElementId partId in associatedPartsList)
            {
               Part part = hostElement.Document.GetElement(partId) as Part;
               Options options = GeometryUtil.GetIFCExportGeometryOptions();
               GeometryElement geometryElement = part.get_Geometry(options);
               if (geometryElement == null)
                  continue;

               SolidMeshGeometryInfo solidMeshInfo = GeometryUtil.GetSplitSolidMeshGeometry(geometryElement);
               foreach (Solid solid in solidMeshInfo.GetSolids())
               {
                  foreach (Face sFace in solid.Faces)
                  {
                     PlanarFace face = sFace as PlanarFace;
                     if (face == null)
                        continue;      // Not a planar face

                     // Check only top faces
                     if (MathUtil.IsAlmostEqual(face.FaceNormal.DotProduct(XYZ.BasisZ), 1.0))
                     {
                        // Create a line perpendicular to the wall at face's height
                        // It may not be strictly necessary for the height to be the same if we use projections instead of intersections
                        Line rightLine = Line.CreateUnbound(rightLineOrigin + new XYZ(0.0, 0.0, face.Origin.Z), rightVec);

                        if (locationCurve is Line)
                        {
                           for (int i = 0; i < face.EdgeLoops.Size; i++)
                           {
                              EdgeArray loop = face.EdgeLoops.get_Item(i);
                              for (int j = 0; j < loop.Size; j++)
                              {
                                 Edge edge = loop.get_Item(j);
                                 Curve edgeCurve = edge.AsCurveFollowingFace(face);
                                 Line edgeLine = edgeCurve as Line;
                                 if (edgeLine == null)
                                    continue;

                                 // Walls may be trimmed at any degree, so side edges may not be parallel to the line
                                 // This is why side edges are determined as those not perpendicular to it
                                 if (MathUtil.IsAlmostEqual(Math.Abs(edgeLine.Direction.DotProduct(rightVec)), 0.0))
                                    continue;

                                 // Instead of intersecting the line with the face, which may be error-prone, project edge's endpoints onto it
                                 IntersectionResult param1 = rightLine.Project(edgeCurve.GetEndPoint(0));
                                 IntersectionResult param2 = rightLine.Project(edgeCurve.GetEndPoint(1));

                                 double param = Math.Min(param1.Parameter, param2.Parameter);
                                 if (sortedFaces.Keys.Contains(param))
                                    continue;

                                 sortedFaces.Add(param, new Tuple<double, Face>(Math.Abs(param2.Parameter - param1.Parameter), face));
                              }
                           }
                        }
                        else if (locationCurve is Arc)
                        {
                           List<double> arcWallRadii = new List<double>();
                           for (int i = 0; i < face.EdgeLoops.Size; i++)
                           {
                              EdgeArray loop = face.EdgeLoops.get_Item(i);
                              for (int j = 0; j < loop.Size; j++)
                              {
                                 Edge edge = loop.get_Item(j);
                                 Curve edgeCurve = edge.AsCurveFollowingFace(face);
                                 Arc edgeArc = edgeCurve as Arc;
                                 if (edgeArc == null)
                                    continue;

                                 Arc locationArc = locationCurve as Arc;
                                 if (locationArc.Center.IsAlmostEqualTo(new XYZ(edgeArc.Center.X, edgeArc.Center.Y, locationArc.Center.Z)))
                                    arcWallRadii.Add(edgeArc.Radius);
                              }
                           }

                           for (int idx = 0; idx < arcWallRadii.Count - 1; idx++)
                           {
                              if (sortedFaces.Keys.Contains(arcWallRadii[idx]) || MathUtil.IsAlmostEqual(arcWallRadii[idx + 1], arcWallRadii[idx]))
                                 continue;

                              sortedFaces.Add(arcWallRadii[idx], new Tuple<double, Face>(Math.Abs(arcWallRadii[idx + 1] - arcWallRadii[idx]), face));
                           }
                        }
                     }
                  }
               }
            }

            foreach (KeyValuePair<double, Tuple<double, Face>> faceInfo in sortedFaces)
            {
               foreach (MaterialLayerSetInfo.MaterialInfo layerInfo in layersetInfo.MaterialIds)
               {
                  // Face's material ID == layer's material ID && face's width == layer's width
                  if (faceInfo.Value.Item2.MaterialElementId == layerInfo.BaseMatId && MathUtil.IsAlmostEqual(faceInfo.Value.Item1, layerInfo.Width))
                  {
                     layersetInfoList.Add(layerInfo);
                     break;
                  }
               }
            }
         }

         return layersetInfoList;
      }

      /// <summary>
      /// Add the exported part to cache.
      /// </summary>
      /// <param name="partElement">The exported part.</param>
      /// <param name="partExportLevel">The level to which the part has exported.</param>
      /// <param name="hostElement">The host element of part exported.</param>
      private static void TraceExportedParts(Element partElement, ElementId partExportLevel, ElementId hostElementId)
      {
         if (!ExporterCacheManager.PartExportedCache.HasRegistered(partElement.Id))
         {
            Dictionary<ElementId, ElementId> hostOverrideLevels = new Dictionary<ElementId, ElementId>();

            partExportLevel ??= ElementId.InvalidElementId;
            if (!hostOverrideLevels.ContainsKey(partExportLevel))
               hostOverrideLevels.Add(partExportLevel, hostElementId);
            ExporterCacheManager.PartExportedCache.Register(partElement.Id, hostOverrideLevels);
         }
         else
         {
            ExporterCacheManager.PartExportedCache.Add(partElement.Id, partExportLevel, hostElementId);
         }
      }

      /// <summary>
      /// Identifies if the host element should be exported as parts.
      /// If MVD is older then IFC4 we rely on ExportOptionsCache.ExportParts option.
      /// If MVD is IFC4RV and if Exchange type is Structural we need to export Parts if they exist. If number of parts is 1 we need to export it as ShapeAspect and not as Part.
      /// </summary>
      /// <param name="hostElement">The host element.</param>
      /// <param name="layersCount">The number of layers or parts the hostElemnt consists of.</param>
      /// <returns>True if host element should be exported as parts, false otherwise.</returns>
      public static bool ShouldExportParts(Element hostElement, int layersCount)
      {
         return hostElement != null && (ExporterCacheManager.ExportOptionsCache.ExportParts ||
            (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView &&
            ExporterCacheManager.ExportOptionsCache.ExchangeRequirement == KnownERNames.Structural &&
            layersCount > 1));
      }
      /// <summary>
      /// Checks if element has associated parts and all conditions are met for exporting it as Parts.
      /// </summary>
      /// <param name="hostElement">The host element.</param>
      /// <returns>True if host element can export the parts, false otherwise.</returns>
      public static bool CanExportParts(Element hostElement)
      {
         if (ShouldExportParts(hostElement, PartUtils.GetAssociatedParts(hostElement.Document, hostElement.Id, false, true).Count))
         {
            return PartUtils.HasAssociatedParts(hostElement.Document, hostElement.Id);
         }
         return false;
      }

      /// <summary>
      /// Identifies if the host element can export when exporting parts.
      /// 1. If host element has non merged parts (>0), it can be export no matter if it has merged parts or not, and return true.
      /// 2. If host element has merged parts
      ///    - If the merged part is the right category and not export yet, return true.
      ///    - If the merged part is the right category but has been exported by other host, return false.
      ///    - If the merged part is not the right category, should not export and return false.
      /// </summary>
      /// <param name="hostElement">The host element having parts.</param>
      /// <param name="levelId">The level the part would export.</param>
      /// <Param name="isSplit">The bool flag identifies if the host element is split by story.</Param>
      /// <returns>True if the element can export, false otherwise.</returns>
      public static bool CanExportElementInPartExport(Element hostElement, ElementId levelId, bool isSplit,
         ICollection<ElementId> associatedPartsList)
      {
         foreach (ElementId partId in associatedPartsList)
         {
            Part part = hostElement.Document.GetElement(partId) as Part;
            if (PartUtils.IsMergedPart(part))
            {
               ElementId hostElementCategoryId = CategoryUtil.GetSafeCategoryId(hostElement);
               if (part.OriginalCategoryId == hostElementCategoryId)
               {
                  if (isSplit)
                  {
                     if (!ExporterCacheManager.PartExportedCache.HasExported(partId, levelId))
                     {
                        // has merged split part and not export yet.
                        return true;
                     }
                  }
                  else if (!ExporterCacheManager.PartExportedCache.HasRegistered(partId))
                  {
                     // has merged part and not export yet.
                     return true;
                  }
               }
            }
            else
            {
               return true;
            }
         }

         // has no merged parts or other parts or merged parts have been exported.
         return false;
      }

      public static bool CanExportElementInPartExport(Element hostElement, ElementId levelId, bool isSplit)
      {
         ElementId hostElementId = hostElement.Id;
         if (ExporterCacheManager.TemporaryPartsCache.HasTemporaryParts(hostElementId))
            return true;

         ICollection<ElementId> associatedPartsList = PartUtils.GetAssociatedParts(hostElement.Document, hostElementId, false, true);
         return CanExportElementInPartExport(hostElement, levelId, isSplit, associatedPartsList);
      }
      
      /// <summary>
      ///  Identifies if host element is a Wall or a Column
      /// </summary>
      /// <param name="hostElement">The host element having associated parts.</param>
      /// <returns>True if Wall or Column, false otherwise.</returns>
      private static bool IsHostWallOrColumn(Element hostElement)
      {
         string ifcEnumType;
         IFCExportInfoPair exportType = ExporterUtil.GetProductExportType(hostElement, out ifcEnumType);
         return (exportType.ExportInstance == IFCEntityType.IfcWall) || (exportType.ExportInstance == IFCEntityType.IfcColumn);
      }

      private static bool ElementCanHaveMultipleComponents(Element hostElement)
      {
         // Currently only objects with multi-layer/structure are supported
         return hostElement is Floor
            || hostElement is RoofBase
            || hostElement is Ceiling
            || hostElement is Wall
            || hostElement is FamilyInstance;
      }

      /// <summary>
      /// Get the Default IFCExtrusionAxes for part. 
      /// Simply having roof/floor/wall/column as Z and everything else as XY.
      /// </summary>
      /// <param name="part">The part.</param>
      /// <returns>TryZ for wall/column/floor/roof category and TryXY for other category.</returns>
      private static IFCExtrusionAxes GetDefaultExtrusionAxesForPart(Part part)
      {
         switch (part.OriginalCategoryId.Value)
         {
            case (long)BuiltInCategory.OST_Walls:
            case (long)BuiltInCategory.OST_Columns:
            case (long)BuiltInCategory.OST_Ceilings:
            case (long)BuiltInCategory.OST_Floors:
            case (long)BuiltInCategory.OST_Roofs:
               return IFCExtrusionAxes.TryZ;
            default:
               return IFCExtrusionAxes.TryXY;
         }
      }

      /// <summary>
      /// Get the Default IFCExtrusionAxes for host element. 
      /// Simply having roof/floor/wall/column as Z and everything else as XY.
      /// </summary>
      /// <param name="hostElement">The host element to get the IFCExtrusionAxes.</param>
      /// <returns>TryZ for wall/column/floor/roof elements and TryXY for other elements.</returns>
      private static IFCExtrusionAxes GetDefaultExtrusionAxesForHost(Element hostElement)
      {
         IFCExportInfoPair exportType = ExporterUtil.GetProductExportType(hostElement, out _);

         switch (exportType.ExportInstance)
         {
            case IFCEntityType.IfcWall:
            case IFCEntityType.IfcColumn:
            case IFCEntityType.IfcCovering:
            case IFCEntityType.IfcSlab:
            case IFCEntityType.IfcRoof:
            case IFCEntityType.IfcBuildingElementProxy:
               return IFCExtrusionAxes.TryZ;
            default:
               return IFCExtrusionAxes.TryXY;
         }
      }

      /// <summary>
      /// Split associated parts when host element is split by level.
      /// </summary>
      /// <param name="hostElement">The host element havign associtaed parts.</param>
      /// <param name="partsOrGeometries">The list of associtated parts or geometries.</param>
      private static void SplitParts(Element hostElement, List<PartOrGeometry> partsOrGeometries)
      {
         if (partsOrGeometries.Count == 0)
            return;

         IFCExportInfoPair exportType = ExporterUtil.GetProductExportType(hostElement, out _);

         // Split the host to find the orphan parts.
         IList<ElementId> orphanLevels = new List<ElementId>();
         IList<ElementId> hostLevels = new List<ElementId>();
         IList<IFCRange> hostRanges = new List<IFCRange>();
         LevelUtil.CreateSplitLevelRangesForElement(exportType, hostElement, out hostLevels, out hostRanges);
         orphanLevels = hostLevels;

         // Split each Parts
         IList<ElementId> levels = new List<ElementId>();
         IList<IFCRange> ranges = new List<IFCRange>();
         // Dictionary to storage the level and its parts.
         Dictionary<ElementId, List<KeyValuePair<PartOrGeometry, IFCRange>>> levelParts = new();

         bool isGeometry = partsOrGeometries[0].IsGeometry;

         foreach (var partOrGeometry in partsOrGeometries)
         {
            if (isGeometry)
               LevelUtil.CreateSplitLevelRangesForElementGeometry(exportType, hostElement, partOrGeometry.GeometryElement, out levels, out ranges);
            else
               LevelUtil.CreateSplitLevelRangesForElement(exportType, partOrGeometry.Part, out levels, out ranges);

            // if the parts are above top level, associate them with nearest bottom level.
            if (ranges.Count == 0)
            {
               BoundingBoxXYZ partBBox = isGeometry ?
                  partOrGeometry.GeometryElement.GetBoundingBox() : partOrGeometry.Part.get_BoundingBox(null);
               ElementId bottomLevelId = FindPartSplitLevel(partBBox);

               // Assign a single range from the bounding box of the Part, otherwise it may cause problem somewhere that expects the non null value
               IFCRange range = new IFCRange(partBBox.Min.Z, partBBox.Max.Z);
               if (bottomLevelId == ElementId.InvalidElementId)
                  bottomLevelId = isGeometry ? hostElement.LevelId : partOrGeometry.Part.LevelId;

               if (!levelParts.ContainsKey(bottomLevelId))
                  levelParts.Add(bottomLevelId, new List<KeyValuePair<PartOrGeometry, IFCRange>>());

               KeyValuePair<PartOrGeometry, IFCRange> splitPartRange = new KeyValuePair<PartOrGeometry, IFCRange>(partOrGeometry, range);
               levelParts[bottomLevelId].Add(splitPartRange);

               continue;
            }

            // The parts split by levels are stored in dictionary.
            for (int ii = 0; ii < ranges.Count; ii++)
            {
               if (!levelParts.ContainsKey(levels[ii]))
                  levelParts.Add(levels[ii], new List<KeyValuePair<PartOrGeometry, IFCRange>>());

               KeyValuePair<PartOrGeometry, IFCRange> splitPartRange = new KeyValuePair<PartOrGeometry, IFCRange>(partOrGeometry, ranges[ii]);
               levelParts[levels[ii]].Add(splitPartRange);
            }

            if (levels.Count > hostLevels.Count)
            {
               orphanLevels = orphanLevels.Union(levels).ToList();
            }
         }

         ExporterCacheManager.HostPartsCache.Register(hostElement.Id, levelParts);

         // The levels of orphan part.
         orphanLevels = orphanLevels.Where(number => !hostLevels.Contains(number)).ToList();
         List<KeyValuePair<ElementId, IFCRange>> levelRangePairList = new List<KeyValuePair<ElementId, IFCRange>>();
         foreach (ElementId orphanLevelId in orphanLevels)
         {
            IFCLevelInfo levelInfo = ExporterCacheManager.LevelInfoCache.GetLevelInfo(orphanLevelId);
            if (levelInfo == null)
               continue;
            double levelHeight = ExporterCacheManager.LevelInfoCache.FindHeight(orphanLevelId);
            IFCRange levelRange = new IFCRange(levelInfo.Elevation, levelInfo.Elevation + levelHeight);

            List<KeyValuePair<PartOrGeometry, IFCRange>> splitPartRangeList = new();
            splitPartRangeList = ExporterCacheManager.HostPartsCache.Find(hostElement.Id, orphanLevelId);
            IFCRange highestRange = levelRange;
            foreach (KeyValuePair<PartOrGeometry, IFCRange> partRange in splitPartRangeList)
            {
               if (partRange.Value.End > highestRange.End)
               {
                  highestRange = partRange.Value;
               }
            }
            levelRangePairList.Add(new KeyValuePair<ElementId, IFCRange>(orphanLevelId, highestRange));
         }

         if (levelRangePairList.Count > 0)
         {
            ExporterCacheManager.DummyHostCache.Register(hostElement.Id, levelRangePairList);
         }
      }

      /// <summary>
      /// Find the nearest bottom level for parts that are above top level.
      /// </summary>
      /// <param name="partBBox">The part above top level.</param>
      /// <returns>The ElementId of nearest bottom level.</returns>
      private static ElementId FindPartSplitLevel(BoundingBoxXYZ partBBox)
      {
         double extension = LevelUtil.GetLevelExtension();
         ElementId theSplitLevelId = ElementId.InvalidElementId;

         // The levels should have been sorted.
         IList<ElementId> levelIds = ExporterCacheManager.LevelInfoCache.GetBuildingStoriesByElevation();
         // Find the nearest bottom level.
         foreach (ElementId levelId in levelIds)
         {
            IFCLevelInfo levelInfo = ExporterCacheManager.LevelInfoCache.GetLevelInfo(levelId);
            if (levelInfo == null)
               continue;
            if (levelInfo.Elevation < partBBox.Min.Z + extension)
            {
               theSplitLevelId = levelId;
            }
         }

         // If there is no associated level id, it has to be linked to the lowest level
         if (theSplitLevelId == ElementId.InvalidElementId && levelIds.Count > 0)
         {
            theSplitLevelId = levelIds[0];
         }

         return theSplitLevelId;
      }

      /// <summary>
      /// Find the root element for a part with its original category. 
      /// </summary>
      /// <param name="part">The part element.</param>
      /// <param name="originalCategoryId">The category id to find the root element.</param>
      /// <returns>The root element that makes the part; returns null if fail to find the root parent.</returns>
      private static Element FindRootParent(Part part, ElementId originalCategoryId)
      {
         Element hostElement = null;

         foreach (LinkElementId linkElementId in part.GetSourceElementIds())
         {
            if (linkElementId.HostElementId == ElementId.InvalidElementId)
            {
               if (linkElementId.LinkInstanceId == ElementId.InvalidElementId)
                  continue;
               Element linkedElement = part.Document.GetElement(linkElementId.LinkInstanceId);

               RevitLinkInstance linkInstance = linkedElement as RevitLinkInstance;
               if (linkInstance != null)
               {
                  Document document = linkInstance.GetLinkDocument();
                  if (document != null)
                  {
                     ElementId id = linkElementId.LinkedElementId;
                     hostElement = document.GetElement(id);
                     return hostElement;
                  }
               }
               continue;
            }

            Element parentElement = part.Document.GetElement(linkElementId.HostElementId);
            // If the direct parent is a part, find its parent.
            if (parentElement is Part)
            {
               Part parentPart = parentElement as Part;
               hostElement = FindRootParent(parentPart, originalCategoryId);
               if (hostElement != null)
                  return hostElement;
            }
            else if (originalCategoryId == CategoryUtil.GetSafeCategoryId(parentElement))
            {
               hostElement = parentElement;
               return hostElement;
            }
         }

         return hostElement;
      }


      private static List<GeometryObject> GeomObjectsFromOriginalGeometry(ExporterIFC exporterIFC, Element hostElement)
      {
         List<GeometryObject> geometryObjects = new List<GeometryObject>();
         Options options = GeometryUtil.GetIFCExportGeometryOptions();

         // Getting the Part seems to have problem (no Part obtained). We will then use the original geometry of the object
         GeometryElement geometryElement = hostElement.get_Geometry(options);
         if (geometryElement != null)
         {
            SolidMeshGeometryInfo solidMeshInfo = GeometryUtil.GetSplitSolidMeshGeometry(geometryElement);
            IList<Solid> solids = solidMeshInfo.GetSolids();
            IList<Mesh> meshes = solidMeshInfo.GetMeshes();
            geometryObjects.AddRange(FamilyExporterUtil.RemoveInvisibleSolidsAndMeshes(hostElement.Document, exporterIFC, ref solids, ref meshes));
         }

         return geometryObjects;
      }

      private static IFCAnyHandle ShapeRepFromListOfGeomObjects(ExporterIFC exporterIFC, Element hostElement, List<GeometryObject> geometryObjects,
         ref BodyData bodyData, ref IList<ElementId> materialIds, IFCExportBodyParams extrusionCreationData)
      {
         if (geometryObjects.Count == 0)
         {
            return null;
         }

         ElementId catId = CategoryUtil.GetSafeCategoryId(hostElement);

         BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
         bodyData = BodyExporter.ExportBody(exporterIFC, hostElement, catId, ElementId.InvalidElementId, geometryObjects,
            bodyExporterOptions, extrusionCreationData);
         materialIds = bodyData.MaterialIds;

         return bodyData.RepresentationHnd;
      }
     
   }
}