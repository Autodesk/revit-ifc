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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export hosted sweeps.
   /// </summary>
   class HostedSweepExporter
   {
      /// <summary>
      /// Exports a hosted weep.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="hostedSweep">The hosted sweep element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void Export(ExporterIFC exporterIFC, HostedSweep hostedSweep, GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         ElementId catId = CategoryUtil.GetSafeCategoryId(hostedSweep);
         if (catId == new ElementId(BuiltInCategory.OST_Gutter))
            ExportGutter(exporterIFC, hostedSweep, geometryElement, productWrapper);
         else
            ProxyElementExporter.Export(exporterIFC, hostedSweep, geometryElement, productWrapper);
      }

      /// <summary>
      /// Exports a gutter element.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportGutter(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcPipeSegmentType;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return;

         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            // Check for containment override
            IFCAnyHandle overrideContainerHnd = null;
            ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, element, out overrideContainerHnd);

            using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element, null, null, overrideContainerId, overrideContainerHnd))
            {
               using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
               {
                  ecData.SetLocalPlacement(setter.LocalPlacement);

                  ElementId categoryId = CategoryUtil.GetSafeCategoryId(element);

                  BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                  IFCAnyHandle bodyRep = BodyExporter.ExportBody(exporterIFC, element, categoryId, ElementId.InvalidElementId,
                      geometryElement, bodyExporterOptions, ecData).RepresentationHnd;
                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
                  {
                     if (ecData != null)
                        ecData.ClearOpenings();
                     return;
                  }
                  string originalTag = NamingUtil.CreateIFCElementId(element);

                  // In Revit, we don't have a corresponding type, so we create one for every gutter.
                  IFCAnyHandle origin = ExporterUtil.CreateAxis2Placement3D(file);
                  IFCAnyHandle repMap3dHnd = IFCInstanceExporter.CreateRepresentationMap(file, origin, bodyRep);
                  List<IFCAnyHandle> repMapList = new List<IFCAnyHandle>();
                  repMapList.Add(repMap3dHnd);
                  string elementTypeName = NamingUtil.CreateIFCObjectName(exporterIFC, element);

                  string typeGuid = GUIDUtil.CreateSubElementGUID(element, (int)IFCHostedSweepSubElements.PipeSegmentType);
                  IFCAnyHandle style = IFCInstanceExporter.CreatePipeSegmentType(file, null, typeGuid,
                     null, repMapList, IFCPipeSegmentType.Gutter);
                  IFCAnyHandleUtil.OverrideNameAttribute(style, elementTypeName);
                  IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcPipeSegmentType, IFCPipeSegmentType.Gutter.ToString());

                  IFCAnyHandleUtil.SetAttribute(style, "Tag", originalTag);
                  IFCAnyHandleUtil.SetAttribute(style, "ElementType", elementTypeName);

                  List<IFCAnyHandle> representationMaps = GeometryUtil.GetRepresentationMaps(style);
                  IFCAnyHandle mappedItem = ExporterUtil.CreateDefaultMappedItem(file, representationMaps[0]);

                  ISet<IFCAnyHandle> representations = new HashSet<IFCAnyHandle>();
                  representations.Add(mappedItem);

                  IFCAnyHandle bodyMappedItemRep = RepresentationUtil.CreateBodyMappedItemRep(exporterIFC,
                      element, categoryId, exporterIFC.Get3DContextHandle("Body"), representations);
                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyMappedItemRep))
                     return;

                  List<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
                  shapeReps.Add(bodyMappedItemRep);

                  IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geometryElement, Transform.Identity);
                  if (boundingBoxRep != null)
                     shapeReps.Add(boundingBoxRep);

                  IFCAnyHandle prodRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapeReps);
                  IFCAnyHandle localPlacementToUse;
                  ElementId roomId = setter.UpdateRoomRelativeCoordinates(element, out localPlacementToUse);
                  if (roomId == ElementId.InvalidElementId)
                     localPlacementToUse = ecData.GetLocalPlacement();

                  string guid = GUIDUtil.CreateGUID(element);

                  IFCAnyHandle elemHnd = IFCInstanceExporter.CreateFlowSegment(exporterIFC, element, guid,
                      ExporterCacheManager.OwnerHistoryHandle, localPlacementToUse, prodRep);

                  bool containedInSpace = (roomId != ElementId.InvalidElementId);
                  productWrapper.AddElement(element, elemHnd, setter.LevelInfo, ecData, !containedInSpace, exportInfo);

                  if (containedInSpace)
                     ExporterCacheManager.SpaceInfoCache.RelateToSpace(roomId, elemHnd);

                  // Associate segment with type.
                  ExporterCacheManager.TypeRelationsCache.Add(style, elemHnd);

                  OpeningUtil.CreateOpeningsIfNecessary(elemHnd, element, ecData, null,
                      exporterIFC, localPlacementToUse, setter, productWrapper);
               }

               tr.Commit();
            }
         }
      }
   }
}