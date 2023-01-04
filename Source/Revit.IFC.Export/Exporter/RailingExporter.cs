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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using System.Linq;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export ceilings.
   /// </summary>
   class RailingExporter
   {
      private static ElementId GetStairOrRampHostId(ExporterIFC exporterIFC, Railing railingElem)
      {
         ElementId returnHostId = ElementId.InvalidElementId;

         if (railingElem == null)
            return returnHostId;

         ElementId hostId = railingElem.HostId;
         if (hostId == ElementId.InvalidElementId)
            return returnHostId;

         if (!ExporterCacheManager.StairRampContainerInfoCache.ContainsStairRampContainerInfo(hostId))
            return returnHostId;

         Element host = railingElem.Document.GetElement(hostId);
         if (host == null)
            return returnHostId;

         if (!(host is Stairs) && !StairsExporter.IsLegacyStairs(host) && !RampExporter.IsRamp(host))
            return returnHostId;

         returnHostId = hostId;
         return returnHostId;
      }

      private static IFCAnyHandle CopyRailingHandle(ExporterIFC exporterIFC, Element elem, 
         ElementId catId, IFCAnyHandle origLocalPlacement, IFCAnyHandle origRailing, int index)
      {
         IFCFile file = exporterIFC.GetFile();

         IFCAnyHandle origRailingObjectPlacement = IFCAnyHandleUtil.GetObjectPlacement(origRailing);
         IFCAnyHandle railingRelativePlacement = IFCAnyHandleUtil.GetInstanceAttribute(origRailingObjectPlacement, "RelativePlacement");
         IFCAnyHandle parentRelativePlacement = IFCAnyHandleUtil.GetInstanceAttribute(origLocalPlacement, "RelativePlacement");

         IFCAnyHandle newRelativePlacement = null;
         IFCAnyHandle parentRelativeOrig = IFCAnyHandleUtil.GetInstanceAttribute(parentRelativePlacement, "Location");

         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(parentRelativeOrig))
         {
            IList<double> parentVec = IFCAnyHandleUtil.GetCoordinates(parentRelativeOrig);
            IFCAnyHandle railingRelativeOrig = IFCAnyHandleUtil.GetInstanceAttribute(railingRelativePlacement, "Location");
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(railingRelativeOrig))
            {
               IList<double> railingVec = IFCAnyHandleUtil.GetCoordinates(railingRelativeOrig);

               IList<double> newMeasure = new List<double>();
               newMeasure.Add(railingVec[0] - parentVec[0]);
               newMeasure.Add(railingVec[1] - parentVec[1]);
               newMeasure.Add(railingVec[2]);

               IFCAnyHandle locPtHnd = ExporterUtil.CreateCartesianPoint(file, newMeasure);
               newRelativePlacement = IFCInstanceExporter.CreateAxis2Placement3D(file, locPtHnd, null, null);
            }
            else
            {
               IList<double> railingMeasure = new List<double>();
               railingMeasure.Add(-parentVec[0]);
               railingMeasure.Add(-parentVec[1]);
               railingMeasure.Add(0.0);
               IFCAnyHandle locPtHnd = ExporterUtil.CreateCartesianPoint(file, railingMeasure);
               newRelativePlacement = IFCInstanceExporter.CreateAxis2Placement3D(file, locPtHnd, null, null);
            }
         }

         IFCAnyHandle newLocalPlacement = IFCInstanceExporter.CreateLocalPlacement(file, origLocalPlacement, newRelativePlacement);
         IFCAnyHandle origRailingRep = IFCAnyHandleUtil.GetInstanceAttribute(origRailing, "Representation");
         IFCAnyHandle newProdRep = ExporterUtil.CopyProductDefinitionShape(exporterIFC, elem, catId, origRailingRep);

         string ifcEnumTypeAsString = IFCAnyHandleUtil.GetEnumerationAttribute(origRailing, "PredefinedType");

         string copyGUID = GUIDUtil.GenerateIFCGuidFrom(
            GUIDUtil.CreateGUIDString(IFCEntityType.IfcRailing, index.ToString(), origRailing));
         IFCAnyHandle copyOwnerHistory = IFCAnyHandleUtil.GetInstanceAttribute(origRailing, "OwnerHistory");

         return IFCInstanceExporter.CreateRailing(exporterIFC, elem, copyGUID, copyOwnerHistory, newLocalPlacement, newProdRep, ifcEnumTypeAsString);
      }

      /// <summary>
      /// Exports a railing to IFC railing
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="railing">
      /// The ceiling element to be exported.
      /// </param>
      /// <param name="geomElement">
      /// The geometry element.
      /// </param>
      /// <param name="productWrapper">
      /// The ProductWrapper.
      /// </param>
      public static void ExportRailingElement(ExporterIFC exporterIFC, Railing railing, ProductWrapper productWrapper)
      {
         if (railing == null)
            return;

         Options geomOptions = GeometryUtil.GetIFCExportGeometryOptions();
         var oneLevelGeom = GeometryUtil.GetOneLevelGeometryElement(railing.get_Geometry(geomOptions), 0);
         GeometryElement geomElement = oneLevelGeom.element;

         // If this is a multistory railing, the geometry will contain all of the levels of railing.  We only want one.
         if (geomElement == null)
            return;

         string ifcEnumType;
         IFCExportInfoPair exportType = ExporterUtil.GetProductExportType(exporterIFC, railing, out ifcEnumType);
         if (exportType.IsUnKnown)
         {
            ifcEnumType = ExporterUtil.GetIFCTypeFromExportTable(exporterIFC, railing);
         }
         
         ExportRailing(exporterIFC, railing, geomElement, ifcEnumType, productWrapper);
      }

      private static IList<ElementId> CollectSubElements(Railing railingElem)
      {
         IList<ElementId> subElementIds = new List<ElementId>();
         if (railingElem != null)
         {
            ElementId topRailId = railingElem.TopRail;
            if (topRailId != ElementId.InvalidElementId)
               subElementIds.Add(topRailId);
            IList<ElementId> handRailIds = railingElem.GetHandRails();
            if (handRailIds != null)
            {
               foreach (ElementId handRailId in handRailIds)
               {
                  HandRail handRail = railingElem.Document.GetElement(handRailId) as HandRail;
                  if (handRail != null)
                  {
                     subElementIds.Add(handRailId);
                     IList<ElementId> supportIds = handRail.GetSupports();
                     foreach (ElementId supportId in supportIds)
                     {
                        subElementIds.Add(supportId);
                     }
                  }
               }
            }
         }
         return subElementIds;
      }

      /// <summary>
      /// Collects the sub-elements of a Railing, to prevent double export.
      /// </summary>
      /// <param name="railingElem">
      /// The railing.
      /// </param>
      public static void AddSubElementsToCache(Railing railingElem)
      {
         IList<ElementId> subElementIds = CollectSubElements(railingElem);
         foreach (ElementId subElementId in subElementIds)
            ExporterCacheManager.RailingSubElementCache.Add(subElementId);
      }

      /// <summary>
      /// Exports an element as IFC railing.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="element">
      /// The element to be exported.
      /// </param>
      /// <param name="geometryElement">
      /// The geometry element.
      /// </param>
      /// <param name="productWrapper">
      /// The ProductWrapper.
      /// </param>
      public static void ExportRailing(ExporterIFC exporterIFC, Element element, GeometryElement geomElem, string ifcEnumType, ProductWrapper productWrapper)
      {
         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcRailing;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return;

         ElementType elemType = element.Document.GetElement(element.GetTypeId()) as ElementType;
         IFCFile file = exporterIFC.GetFile();
         Options geomOptions = GeometryUtil.GetIFCExportGeometryOptions();

         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            // Check for containment override
            IFCAnyHandle overrideContainerHnd = null;
            ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, element, out overrideContainerHnd);

            using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element, null, null, overrideContainerId, overrideContainerHnd))
            {
               using (IFCExportBodyParams ecData = new IFCExportBodyParams())
               {
                  IFCAnyHandle localPlacement = setter.LocalPlacement;
                  StairRampContainerInfo stairRampInfo = null;
                  ElementId hostId = GetStairOrRampHostId(exporterIFC, element as Railing);
                  Transform inverseTrf = Transform.Identity;
                  if (hostId != ElementId.InvalidElementId)
                  {
                     stairRampInfo = ExporterCacheManager.StairRampContainerInfoCache.GetStairRampContainerInfo(hostId);
                     IFCAnyHandle stairRampLocalPlacement = stairRampInfo.LocalPlacements[0];
                     Transform relTrf = ExporterIFCUtils.GetRelativeLocalPlacementOffsetTransform(stairRampLocalPlacement, localPlacement);
                     inverseTrf = relTrf.Inverse;

                     IFCAnyHandle railingLocalPlacement = ExporterUtil.CreateLocalPlacement(file, stairRampLocalPlacement,
                         inverseTrf.Origin, inverseTrf.BasisZ, inverseTrf.BasisX);
                     localPlacement = railingLocalPlacement;
                  }
                  ecData.SetLocalPlacement(localPlacement);

                  SolidMeshGeometryInfo solidMeshInfo = GeometryUtil.GetSplitSolidMeshGeometry(geomElem);
                  IList<Solid> solids = solidMeshInfo.GetSolids();
                  IList<Mesh> meshes = solidMeshInfo.GetMeshes();
                  IList<GeometryObject> gObjs = FamilyExporterUtil.RemoveInvisibleSolidsAndMeshes(element.Document, exporterIFC, ref solids, ref meshes);

                  Railing railingElem = element as Railing;
                  IList<ElementId> subElementIds = CollectSubElements(railingElem);

                  foreach (ElementId subElementId in subElementIds)
                  {
                     Element subElement = railingElem.Document.GetElement(subElementId);
                     if (subElement != null)
                     {
                        GeometryElement allLevelsGeometry = subElement.get_Geometry(geomOptions);
                        var oneLevelGeom = GeometryUtil.GetOneLevelGeometryElement(allLevelsGeometry, 0);
                        GeometryElement subElementGeom = oneLevelGeom.element;
                        // Get rail terminations geometry
                        List<GeometryElement> overallGeometry = GeometryUtil.GetAdditionalOneLevelGeometry(allLevelsGeometry, oneLevelGeom.symbolId);
                        overallGeometry.Add(subElementGeom);

                        foreach (GeometryElement subGeomentry in overallGeometry)
                        {
                           SolidMeshGeometryInfo subElementSolidMeshInfo = GeometryUtil.GetSplitSolidMeshGeometry(subGeomentry);
                           IList<Solid> subElemSolids = subElementSolidMeshInfo.GetSolids();
                           IList<Mesh> subElemMeshes = subElementSolidMeshInfo.GetMeshes();
                           IList<GeometryObject> partGObjs = FamilyExporterUtil.RemoveInvisibleSolidsAndMeshes(element.Document, exporterIFC, ref subElemSolids, ref subElemMeshes);

                           foreach (Solid subElSolid in subElemSolids)
                              solids.Add(subElSolid);
                           foreach (Mesh subElMesh in subElemMeshes)
                              meshes.Add(subElMesh);
                        }
                     }
                  }

                  ElementId catId = CategoryUtil.GetSafeCategoryId(element);
                  BodyData bodyData = null;
                  BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.Medium);

                  if (solids.Count > 0 || meshes.Count > 0)
                  {
                     bodyData = BodyExporter.ExportBody(exporterIFC, element, catId, ElementId.InvalidElementId, solids, meshes, bodyExporterOptions, ecData);
                  }
                  else
                  {
                     IList<GeometryObject> geomlist = new List<GeometryObject>();
                     geomlist.Add(geomElem);
                     bodyData = BodyExporter.ExportBody(exporterIFC, element, catId, ElementId.InvalidElementId, geomlist, bodyExporterOptions, ecData);
                  }

                  IFCAnyHandle bodyRep = bodyData.RepresentationHnd;
                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                  {
                     if (ecData != null)
                        ecData.ClearOpenings();
                     return;
                  }

                  IList<IFCAnyHandle> representations = new List<IFCAnyHandle>();
                  representations.Add(bodyRep);

                  IList<GeometryObject> geomObjects = new List<GeometryObject>(solids);
                  foreach (Mesh mesh in meshes)
                     geomObjects.Add(mesh);

                  Transform boundingBoxTrf = (bodyData.OffsetTransform != null) ? bodyData.OffsetTransform.Inverse : Transform.Identity;
                  boundingBoxTrf = inverseTrf.Multiply(boundingBoxTrf);
                  IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geomObjects, boundingBoxTrf);
                  if (boundingBoxRep != null)
                     representations.Add(boundingBoxRep);

                  IFCAnyHandle prodRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);

                  IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

                  string instanceGUID = GUIDUtil.CreateGUID(element);

                  IFCExportInfoPair exportInfo = ExporterUtil.GetProductExportType(exporterIFC, element, out ifcEnumType);

                  IFCAnyHandle railing = IFCInstanceExporter.CreateGenericIFCEntity(exportInfo, exporterIFC, element, instanceGUID, ownerHistory,
                            ecData.GetLocalPlacement(), prodRep);

                  bool associateToLevel = (hostId == ElementId.InvalidElementId);

                  productWrapper.AddElement(element, railing, setter, ecData, associateToLevel, exportInfo);
                  OpeningUtil.CreateOpeningsIfNecessary(railing, element, ecData, bodyData.OffsetTransform,
                      exporterIFC, ecData.GetLocalPlacement(), setter, productWrapper);

                  IFCAnyHandle singleMaterialOverrideHnd = null;
                  IList<ElementId> matIds = null;
                  ElementId defaultMatId = ElementId.InvalidElementId;
                  ElementId matId = CategoryUtil.GetBaseMaterialIdForElement(element);

                  // Get IfcSingleMaterialOverride to work for railing
                  singleMaterialOverrideHnd = ExporterUtil.GetSingleMaterial(exporterIFC, element, matId);
                  if (singleMaterialOverrideHnd != null)
                  {
                     matIds = new List<ElementId> { matId };
                  }
                  else
                  {
                     matIds = bodyData.MaterialIds;
                     defaultMatId = matIds[0];

                     // Check if all the items are the same, then get the first material id
                     if (matIds.All(x => x == defaultMatId))
                     {
                        matIds = new List<ElementId> { defaultMatId };
                     }
                  }

                  CategoryUtil.CreateMaterialAssociationWithShapeAspect(exporterIFC, element, railing, bodyData.RepresentationItemInfo);

                  // Create multi-story duplicates of this railing.
                  if (stairRampInfo != null)
                  {
                     stairRampInfo.AddComponent(0, railing);

                     List<IFCAnyHandle> stairHandles = stairRampInfo.StairOrRampHandles;
                     int levelCount = stairHandles.Count;

                     if (levelCount > 0 && railingElem != null)
                     {
                        Stairs stairs = railingElem.Document.GetElement(railingElem.HostId) as Stairs;
                        if ((stairs?.MultistoryStairsId ?? ElementId.InvalidElementId) != ElementId.InvalidElementId)
                        {
                           // If the railing is hosted by stairs, don't use stairHandles.Count,
                           // use ids (count) of levels the railing is placed on.
                           ISet<ElementId> multistoryStairsPlacementLevels = railingElem.GetMultistoryStairsPlacementLevels();
                           if (multistoryStairsPlacementLevels != null)
                              levelCount = multistoryStairsPlacementLevels.Count;
                        }
                     }
                        
                     for (int ii = 1; ii < levelCount; ii++)
                     {
                        IFCAnyHandle railingLocalPlacement = stairRampInfo.LocalPlacements[ii];
                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(railingLocalPlacement))
                        {
                           IFCAnyHandle railingHndCopy = CopyRailingHandle(exporterIFC, element, catId,
                              railingLocalPlacement, railing, ii);
                           stairRampInfo.AddComponent(ii, railingHndCopy);
                           productWrapper.AddElement(element, railingHndCopy, (IFCLevelInfo)null, ecData, false, exportInfo);
                           CategoryUtil.CreateMaterialAssociationWithShapeAspect(exporterIFC, element, railingHndCopy, bodyData.RepresentationItemInfo);
                        }
                     }

                     ExporterCacheManager.StairRampContainerInfoCache.AddStairRampContainerInfo(hostId, stairRampInfo);
                  }
               }
               transaction.Commit();
            }
         }
      }
   }
}
