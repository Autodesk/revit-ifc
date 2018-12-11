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
      /// Export all the parts of the host element.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="hostElement">The host element having parts to export.</param>
      /// <param name="hostHandle">The host element handle.</param>
      /// <param name="originalWrapper">The ProductWrapper object.</param>
      public static void ExportHostPart(ExporterIFC exporterIFC, Element hostElement, IFCAnyHandle hostHandle,
          ProductWrapper originalWrapper, PlacementSetter placementSetter, IFCAnyHandle originalPlacement, ElementId overrideLevelId)
      {
         using (ProductWrapper subWrapper = ProductWrapper.Create(exporterIFC, true))
         {
            List<ElementId> associatedPartsList = PartUtils.GetAssociatedParts(hostElement.Document, hostElement.Id, false, true).ToList();
            if (associatedPartsList.Count == 0)
               return;

            bool isWallOrColumn = IsHostWallOrColumn(exporterIFC, hostElement);
            bool hasOverrideLevel = overrideLevelId != null && overrideLevelId != ElementId.InvalidElementId;

            IFCExtrusionAxes ifcExtrusionAxes = GetDefaultExtrusionAxesForHost(exporterIFC, hostElement);

            // Split parts if wall or column is split by level, and then export; otherwise, export parts normally.
            if (isWallOrColumn && hasOverrideLevel && ExporterCacheManager.ExportOptionsCache.WallAndColumnSplitting)
            {
               if (!ExporterCacheManager.HostPartsCache.HasRegistered(hostElement.Id))
                  SplitParts(exporterIFC, hostElement, associatedPartsList); // Split parts and associate them with host.                   

               // Find and export the parts that are split by specific level.
               List<KeyValuePair<Part, IFCRange>> splitPartRangeList = new List<KeyValuePair<Part, IFCRange>>();
               splitPartRangeList = ExporterCacheManager.HostPartsCache.Find(hostElement.Id, overrideLevelId);

               if (splitPartRangeList != null)
               {
                  foreach (KeyValuePair<Part, IFCRange> partRange in splitPartRangeList)
                  {
                     PartExporter.ExportPart(exporterIFC, partRange.Key, subWrapper, placementSetter, originalPlacement,
                        partRange.Value, ifcExtrusionAxes, hostElement, overrideLevelId, false);
                  }
               }
            }
            else
            {
               foreach (ElementId partId in associatedPartsList)
               {
                  Part part = hostElement.Document.GetElement(partId) as Part;
                  PartExporter.ExportPart(exporterIFC, part, subWrapper, placementSetter, originalPlacement, null, ifcExtrusionAxes,
                     hostElement, overrideLevelId, false);
               }
            }

            // Create the relationship of Host and Parts.
            ICollection<IFCAnyHandle> relatedElementIds = subWrapper.GetAllObjects();
            if (relatedElementIds.Count > 0)
            {
               string guid = GUIDUtil.CreateGUID();
               HashSet<IFCAnyHandle> relatedElementIdSet = new HashSet<IFCAnyHandle>(relatedElementIds);
               IFCInstanceExporter.CreateRelAggregates(exporterIFC.GetFile(), guid, ExporterCacheManager.OwnerHistoryHandle, null, null, hostHandle, relatedElementIdSet);
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
            if (hostElement == null)
               return;

            overrideLevelId = hostElement.LevelId;
            if (overrideLevelId == ElementId.InvalidElementId)
               return;
         }

         IFCExtrusionAxes ifcExtrusionAxes = GetDefaultExtrusionAxesForPart(part);
         PartExporter.ExportPart(exporterIFC, partElement, productWrapper, null, null, null, ifcExtrusionAxes, null,
            overrideLevelId, false);
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
            IFCExportInfoPair exportInfo = new IFCExportInfoPair();
            exportInfo.SetValueWithPair(exportType);
            LevelUtil.CreateSplitLevelRangesForElement(exporterIFC, exportInfo, part, out levels, out ranges);
            if (ranges.Count == 0)
            {
               PartExporter.ExportPart(exporterIFC, partElement, productWrapper, null, null, null, ifcExtrusionAxes, hostElement,
                  overrideLevelId, true);
            }
            else
            {
               for (int ii = 0; ii < ranges.Count; ii++)
               {
                  PartExporter.ExportPart(exporterIFC, partElement, productWrapper, null, null, ranges[ii], ifcExtrusionAxes,
                     hostElement, levels[ii], true);
               }
            }
         }
         else
            PartExporter.ExportPart(exporterIFC, partElement, productWrapper, null, null, null, ifcExtrusionAxes, hostElement,
               overrideLevelId, true);
      }

      /// <summary>
      /// Export the individual part (IfcBuildingElementPart).
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="partElement">The part element to export.</param>
      /// <param name="geometryElement">The geometry of part.</param>
      /// <param name="productWrapper">The ProductWrapper object.</param>
      /// <param name="placementSetter"></param>
      /// <param name="originalPlacement"></param>
      /// <param name="range"></param>
      /// <param name="ifcExtrusionAxes"></param>
      /// <param name="hostElement">The host of the part.  This can be null.</param>
      /// <param name="overrideLevelId">The id of the level that the part is one, overridding other sources.</param>
      /// <param name="asBuildingElement">If true, export the Part as a building element instead of an IfcElementPart.</param>
      public static void ExportPart(ExporterIFC exporterIFC, Element partElement, ProductWrapper productWrapper,
          PlacementSetter placementSetter, IFCAnyHandle originalPlacement, IFCRange range, IFCExtrusionAxes ifcExtrusionAxes,
          Element hostElement, ElementId overrideLevelId, bool asBuildingElement)
      {
         if (!ElementFilteringUtil.IsElementVisible(partElement))
            return;

         Part part = partElement as Part;
         if (part == null)
            return;

         // We don't know how to export a part as a building element if we don't know it's host.
         if (asBuildingElement && (hostElement == null))
            return;

         if (!asBuildingElement)
         {
            // Check the intended IFC entity or type name is in the exclude list specified in the UI
            Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcBuildingElementPart;
            if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
               return;
         }
         else
         {
            string ifcEnumType = null;
            IFCExportInfoPair exportType = ExporterUtil.GetExportType(exporterIFC, hostElement, out ifcEnumType);

            // Check the intended IFC entity or type name is in the exclude list specified in the UI
            Common.Enums.IFCEntityType elementClassTypeEnum;
            if (Enum.TryParse<Common.Enums.IFCEntityType>(exportType.ToString(), out elementClassTypeEnum))
               if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
                  return;
         }

         PlacementSetter standalonePlacementSetter = null;
         bool standaloneExport = hostElement == null || asBuildingElement;

         ElementId partExportLevelId = (overrideLevelId != null) ? overrideLevelId : null;

         if (partExportLevelId == null && standaloneExport)
            partExportLevelId = partElement.LevelId;

         if (partExportLevelId == null)
         {
            if (hostElement == null || (part.OriginalCategoryId != hostElement.Category.Id))
               return;
            partExportLevelId = hostElement.LevelId;
         }

         if (ExporterCacheManager.PartExportedCache.HasExported(partElement.Id, partExportLevelId))
            return;

         Options options = GeometryUtil.GetIFCExportGeometryOptions();
         View ownerView = partElement.Document.GetElement(partElement.OwnerViewId) as View;
         if (ownerView != null)
            options.View = ownerView;

         GeometryElement geometryElement = partElement.get_Geometry(options);
         if (geometryElement == null)
            return;

         try
         {
            IFCFile file = exporterIFC.GetFile();
            using (IFCTransaction transaction = new IFCTransaction(file))
            {
               IFCAnyHandle partPlacement = null;
               if (standaloneExport)
               {
                  Transform orientationTrf = Transform.Identity;
                  IFCAnyHandle overrideContainerHnd = null;
                  ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, partElement, out overrideContainerHnd);
                  if (overrideContainerId != ElementId.InvalidElementId && (partExportLevelId == null || partExportLevelId == ElementId.InvalidElementId))
                     partExportLevelId = overrideContainerId;

                  standalonePlacementSetter = PlacementSetter.Create(exporterIFC, partElement, null, orientationTrf, partExportLevelId, overrideContainerHnd);
                  partPlacement = standalonePlacementSetter.LocalPlacement;
               }
               else
               {
                  partPlacement = ExporterUtil.CreateLocalPlacement(file, originalPlacement, null);
               }

               bool validRange = (range != null && !MathUtil.IsAlmostZero(range.Start - range.End));

               SolidMeshGeometryInfo solidMeshInfo;
               if (validRange)
               {
                  solidMeshInfo = GeometryUtil.GetSplitClippedSolidMeshGeometry(geometryElement, range);
                  if (solidMeshInfo.GetSolids().Count == 0 && solidMeshInfo.GetMeshes().Count == 0)
                     return;
               }
               else
               {
                  solidMeshInfo = GeometryUtil.GetSplitSolidMeshGeometry(geometryElement);
               }

               using (IFCExtrusionCreationData extrusionCreationData = new IFCExtrusionCreationData())
               {
                  extrusionCreationData.SetLocalPlacement(partPlacement);
                  extrusionCreationData.ReuseLocalPlacement = false;
                  extrusionCreationData.PossibleExtrusionAxes = ifcExtrusionAxes;

                  IList<Solid> solids = new List<Solid>(); ;
                  IList<Mesh> meshes = new List<Mesh>();
                  IList<GeometryObject> gObjs = FamilyExporterUtil.RemoveInvisibleSolidsAndMeshes(partElement.Document, exporterIFC, solidMeshInfo.GetSolids(), solidMeshInfo.GetMeshes());
                  foreach (GeometryObject gObj in gObjs)
                  {
                     if (gObj is Solid)
                        solids.Add(gObj as Solid);
                     else if (gObj is Mesh)
                        meshes.Add(gObj as Mesh);
                  }

                  ElementId catId = CategoryUtil.GetSafeCategoryId(partElement);
                  ElementId hostCatId = CategoryUtil.GetSafeCategoryId(hostElement);

                  BodyData bodyData = null;
                  BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                  if (solids.Count > 0 || meshes.Count > 0)
                  {
                     bodyData = BodyExporter.ExportBody(exporterIFC, partElement, catId, ElementId.InvalidElementId, solids, meshes,
                         bodyExporterOptions, extrusionCreationData);
                  }
                  else
                  {
                     IList<GeometryObject> geomlist = new List<GeometryObject>();
                     geomlist.Add(geometryElement);
                     bodyData = BodyExporter.ExportBody(exporterIFC, partElement, catId, ElementId.InvalidElementId, geomlist,
                         bodyExporterOptions, extrusionCreationData);
                  }

                  IFCAnyHandle bodyRep = bodyData.RepresentationHnd;
                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                  {
                     extrusionCreationData.ClearOpenings();
                     return;
                  }

                  IList<IFCAnyHandle> representations = new List<IFCAnyHandle>();
                  representations.Add(bodyRep);

                  IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geometryElement, Transform.Identity);
                  if (boundingBoxRep != null)
                     representations.Add(boundingBoxRep);

                  IFCAnyHandle prodRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);

                  IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

                  string partGUID = GUIDUtil.CreateGUID(partElement);

                  IFCAnyHandle ifcPart = null;
                  if (!asBuildingElement)
                  {
                     ifcPart = IFCInstanceExporter.CreateBuildingElementPart(exporterIFC, partElement, partGUID, ownerHistory,
                         extrusionCreationData.GetLocalPlacement(), prodRep);
                  }
                  else
                  {
                     string ifcEnumType = null;
                     IFCExportInfoPair exportType = ExporterUtil.GetExportType(exporterIFC, hostElement, out ifcEnumType);
                     
                     switch (exportType.ExportInstance)
                     {
                        case IFCEntityType.IfcColumn:
                           ifcPart = IFCInstanceExporter.CreateColumn(exporterIFC, partElement, partGUID, ownerHistory,
                               extrusionCreationData.GetLocalPlacement(), prodRep, ifcEnumType);
                           break;
                        case IFCEntityType.IfcCovering:
                           ifcPart = IFCInstanceExporter.CreateCovering(exporterIFC, partElement, partGUID, ownerHistory,
                               extrusionCreationData.GetLocalPlacement(), prodRep, ifcEnumType);
                           break;
                        case IFCEntityType.IfcFooting:
                           ifcPart = IFCInstanceExporter.CreateFooting(exporterIFC, partElement, partGUID, ownerHistory,
                               extrusionCreationData.GetLocalPlacement(), prodRep, ifcEnumType);
                           break;
                        case IFCEntityType.IfcPile:
                           ifcPart = IFCInstanceExporter.CreatePile(exporterIFC, partElement, partGUID, ownerHistory,
                               extrusionCreationData.GetLocalPlacement(), prodRep, ifcEnumType, null);
                           break;
                        case IFCEntityType.IfcRoof:
                           ifcPart = IFCInstanceExporter.CreateRoof(exporterIFC, partElement, partGUID, ownerHistory,
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

                              ifcPart = IFCInstanceExporter.CreateSlab(exporterIFC, partElement, partGUID, ownerHistory,
                                  extrusionCreationData.GetLocalPlacement(), prodRep, ifcEnumType);
                           }
                           break;
                        case IFCEntityType.IfcWall:
                           ifcPart = IFCInstanceExporter.CreateWall(exporterIFC, partElement, partGUID, ownerHistory,
                           extrusionCreationData.GetLocalPlacement(), prodRep, ifcEnumType);
                           break;
                        default:
                           ifcPart = IFCInstanceExporter.CreateBuildingElementProxy(exporterIFC, partElement, partGUID, ownerHistory,
                               extrusionCreationData.GetLocalPlacement(), prodRep, null);
                           break;
                     }
                  }

                  bool containedInLevel = standaloneExport;
                  PlacementSetter whichPlacementSetter = containedInLevel ? standalonePlacementSetter : placementSetter;
                  productWrapper.AddElement(partElement, ifcPart, whichPlacementSetter, extrusionCreationData, containedInLevel);

                  OpeningUtil.CreateOpeningsIfNecessary(ifcPart, partElement, extrusionCreationData, bodyData.OffsetTransform, exporterIFC,
                      extrusionCreationData.GetLocalPlacement(), whichPlacementSetter, productWrapper);

                  //Add the exported part to exported cache.
                  TraceExportedParts(partElement, partExportLevelId, standaloneExport ? ElementId.InvalidElementId : hostElement.Id);

                  CategoryUtil.CreateMaterialAssociation(exporterIFC, ifcPart, bodyData.MaterialIds);

                  transaction.Commit();
               }
            }
         }
         finally
         {
            if (standalonePlacementSetter != null)
               standalonePlacementSetter.Dispose();
         }
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
            Dictionary<ElementId, ElementId> hostOverideLevels = new Dictionary<ElementId, ElementId>();

            if (!hostOverideLevels.ContainsKey(partExportLevel))
               hostOverideLevels.Add(partExportLevel, hostElementId);
            ExporterCacheManager.PartExportedCache.Register(partElement.Id, hostOverideLevels);
         }
         else
         {
            ExporterCacheManager.PartExportedCache.Add(partElement.Id, partExportLevel, hostElementId);
         }
      }

      /// <summary>
      /// Identifies if the host element can export the associated parts.
      /// </summary>
      /// <param name="hostElement">The host element.</param>
      /// <returns>True if host element can export the parts and have any associated parts, false otherwise.</returns>
      public static bool CanExportParts(Element hostElement)
      {
         if (hostElement != null && ExporterCacheManager.ExportOptionsCache.ExportParts)
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
      /// <Param name="IsSplit">The bool flag identifies if the host element is split by story.</Param>
      /// <returns>True if the element can export, false otherwise.</returns>
      public static bool CanExportElementInPartExport(Element hostElement, ElementId levelId, bool IsSplit)
      {
         List<ElementId> associatedPartsList = PartUtils.GetAssociatedParts(hostElement.Document, hostElement.Id, false, true).ToList();

         foreach (ElementId partId in associatedPartsList)
         {
            Part part = hostElement.Document.GetElement(partId) as Part;
            if (PartUtils.IsMergedPart(part))
            {
               if (part.OriginalCategoryId == hostElement.Category.Id)
               {
                  if (IsSplit)
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

      /// <summary>
      ///  Identifies if host element is a Wall or a Column
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="hostElement">The host element having associated parts.</param>
      /// <returns>True if Wall or Column, false otherwise.</returns>
      private static bool IsHostWallOrColumn(ExporterIFC exporterIFC, Element hostElement)
      {
         string ifcEnumType;
         IFCExportInfoPair exportType = ExporterUtil.GetExportType(exporterIFC, hostElement, out ifcEnumType);
         return (exportType.ExportInstance == IFCEntityType.IfcWall) || (exportType.ExportInstance == IFCEntityType.IfcColumn);
      }

      /// <summary>
      /// Get the Default IFCExtrusionAxes for part. 
      /// Simply having roof/floor/wall/column as Z and everything else as XY.
      /// </summary>
      /// <param name="part">The part.</param>
      /// <returns>TryZ for wall/column/floor/roof category and TryXY for other category.</returns>
      private static IFCExtrusionAxes GetDefaultExtrusionAxesForPart(Part part)
      {
         switch ((BuiltInCategory)part.OriginalCategoryId.IntegerValue)
         {
            case BuiltInCategory.OST_Walls:
            case BuiltInCategory.OST_Columns:
            case BuiltInCategory.OST_Floors:
            case BuiltInCategory.OST_Roofs:
               return IFCExtrusionAxes.TryZ;
            default:
               return IFCExtrusionAxes.TryXY;
         }
      }

      /// <summary>
      /// Get the Default IFCExtrusionAxes for host element. 
      /// Simply having roof/floor/wall/column as Z and everything else as XY.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="hostElement">The host element to get the IFCExtrusionAxes.</param>
      /// <returns>TryZ for wall/column/floor/roof elements and TryXY for other elements.</returns>
      private static IFCExtrusionAxes GetDefaultExtrusionAxesForHost(ExporterIFC exporterIFC, Element hostElement)
      {
         string ifcEnumType;
         IFCExportInfoPair exportType = ExporterUtil.GetExportType(exporterIFC, hostElement, out ifcEnumType);

         switch (exportType.ExportInstance)
         {
            case IFCEntityType.IfcWall:
            case IFCEntityType.IfcColumn:
            case IFCEntityType.IfcSlab:
            case IFCEntityType.IfcRoof:
               return IFCExtrusionAxes.TryZ;
            default:
               return IFCExtrusionAxes.TryXY;
         }
      }

      /// <summary>
      /// Split associated parts when host element is split by level.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="hostElement">The host element havign associtaed parts.</param>
      /// <param name="associatedPartsList">The list of associtated parts.</param>
      private static void SplitParts(ExporterIFC exporterIFC, Element hostElement, List<ElementId> associatedPartsList)
      {
         string ifcEnumType;
         IFCExportInfoPair exportType = ExporterUtil.GetExportType(exporterIFC, hostElement, out ifcEnumType);

         // Split the host to find the orphan parts.
         IList<ElementId> orphanLevels = new List<ElementId>();
         IList<ElementId> hostLevels = new List<ElementId>();
         IList<IFCRange> hostRanges = new List<IFCRange>();
         LevelUtil.CreateSplitLevelRangesForElement(exporterIFC, exportType, hostElement, out hostLevels, out hostRanges);
         orphanLevels = hostLevels;

         // Split each Parts
         IList<ElementId> levels = new List<ElementId>();
         IList<IFCRange> ranges = new List<IFCRange>();
         // Dictionary to storage the level and its parts.
         Dictionary<ElementId, List<KeyValuePair<Part, IFCRange>>> levelParts = new Dictionary<ElementId, List<KeyValuePair<Part, IFCRange>>>();

         foreach (ElementId partId in associatedPartsList)
         {
            Part part = hostElement.Document.GetElement(partId) as Part;
            LevelUtil.CreateSplitLevelRangesForElement(exporterIFC, exportType, part, out levels, out ranges);

            // if the parts are above top level, associate them with nearest bottom level.
            if (ranges.Count == 0)
            {
               ElementId bottomLevelId = FindPartSplitLevel(exporterIFC, part);

               if (bottomLevelId == ElementId.InvalidElementId)
                  bottomLevelId = part.LevelId;

               if (!levelParts.ContainsKey(bottomLevelId))
                  levelParts.Add(bottomLevelId, new List<KeyValuePair<Part, IFCRange>>());

               KeyValuePair<Part, IFCRange> splitPartRange = new KeyValuePair<Part, IFCRange>(part, null);
               levelParts[bottomLevelId].Add(splitPartRange);

               continue;
            }

            // The parts split by levels are stored in dictionary.
            for (int ii = 0; ii < ranges.Count; ii++)
            {
               if (!levelParts.ContainsKey(levels[ii]))
                  levelParts.Add(levels[ii], new List<KeyValuePair<Part, IFCRange>>());

               KeyValuePair<Part, IFCRange> splitPartRange = new KeyValuePair<Part, IFCRange>(part, ranges[ii]);
               levelParts[levels[ii]].Add(splitPartRange);
            }

            if (levels.Count > hostLevels.Count)
            {
               orphanLevels = orphanLevels.Union<ElementId>(levels).ToList();
            }
         }

         ExporterCacheManager.HostPartsCache.Register(hostElement.Id, levelParts);

         // The levels of orphan part.
         orphanLevels = orphanLevels.Where(number => !hostLevels.Contains(number)).ToList();
         List<KeyValuePair<ElementId, IFCRange>> levelRangePairList = new List<KeyValuePair<ElementId, IFCRange>>();
         foreach (ElementId orphanLevelId in orphanLevels)
         {
            IFCLevelInfo levelInfo = ExporterCacheManager.LevelInfoCache.GetLevelInfo(exporterIFC, orphanLevelId);
            if (levelInfo == null)
               continue;
            double levelHeight = ExporterCacheManager.LevelInfoCache.FindHeight(orphanLevelId);
            IFCRange levelRange = new IFCRange(levelInfo.Elevation, levelInfo.Elevation + levelHeight);

            List<KeyValuePair<Part, IFCRange>> splitPartRangeList = new List<KeyValuePair<Part, IFCRange>>();
            splitPartRangeList = ExporterCacheManager.HostPartsCache.Find(hostElement.Id, orphanLevelId);
            IFCRange highestRange = levelRange;
            foreach (KeyValuePair<Part, IFCRange> partRange in splitPartRangeList)
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
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="part">The part above top level.</param>
      /// <returns>The ElementId of nearest bottom level.</returns>
      private static ElementId FindPartSplitLevel(ExporterIFC exporterIFC, Part part)
      {
         double extension = LevelUtil.GetLevelExtension();
         ElementId theSplitLevelId = ElementId.InvalidElementId;
         BoundingBoxXYZ boundingBox = part.get_BoundingBox(null);

         // The levels should have been sorted.
         IList<ElementId> levelIds = ExporterCacheManager.LevelInfoCache.BuildingStoreysByElevation;
         // Find the nearest bottom level.
         foreach (ElementId levelId in levelIds)
         {
            IFCLevelInfo levelInfo = ExporterCacheManager.LevelInfoCache.GetLevelInfo(exporterIFC, levelId);
            if (levelInfo == null)
               continue;
            if (levelInfo.Elevation < boundingBox.Min.Z + extension)
            {
               theSplitLevelId = levelId;
            }
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
            else if (originalCategoryId == parentElement.Category.Id)
            {
               hostElement = parentElement;
               return hostElement;
            }
         }

         return hostElement;
      }
   }
}