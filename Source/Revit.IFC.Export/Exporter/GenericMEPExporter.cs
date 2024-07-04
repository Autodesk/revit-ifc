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

using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export generic MEP family instances.
   /// </summary>
   class GenericMEPExporter
   {
      /// <summary>
      /// Exports a MEP family instance.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="exportType">The export type of the element.
      /// <param name="ifcEnumType">The sub-type of the element.</param></param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      /// <returns>True if an entity was created, false otherwise.</returns>
      public static bool Export(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement,
          IFCExportInfoPair exportType, string ifcEnumType, ProductWrapper productWrapper)
      {
         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction tr = new IFCTransaction(file))
         {
            // CQ_TODO: Clean up this code by at least factoring it out.

            // If we are exporting a duct segment, we may need to split it into parts by level. Create a list of ranges.
            IList<ElementId> levels = new List<ElementId>();
            IList<IFCRange> ranges = new List<IFCRange>();

            // We will not split duct segments if the assemblyId is set, as we would like to keep the original duct segment
            // associated with the assembly, on the level of the assembly.
            if ((exportType.ExportType == IFCEntityType.IfcDuctSegmentType) &&
               (ExporterCacheManager.ExportOptionsCache.WallAndColumnSplitting) &&
               !ExporterUtil.IsContainedInAssembly(element))
            {
               LevelUtil.CreateSplitLevelRangesForElement(exporterIFC, exportType, element, out levels,
                                                          out ranges);
            }

            int numPartsToExport = ranges.Count;
            {
               ElementId catId = CategoryUtil.GetSafeCategoryId(element);

               BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
               if (0 == numPartsToExport)
               {
                  // Check for containment override
                  IFCAnyHandle overrideContainerHnd = null;
                  ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, element, out overrideContainerHnd);

                  using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element, null, null, overrideContainerId, overrideContainerHnd))
                  {
                     IFCAnyHandle localPlacementToUse = setter.LocalPlacement;
                     BodyData bodyData = null;
                     using (IFCExportBodyParams extraParams = new IFCExportBodyParams())
                     {
                        extraParams.SetLocalPlacement(localPlacementToUse);
                        IFCAnyHandle productRepresentation =
                            RepresentationUtil.CreateAppropriateProductDefinitionShape(
                                exporterIFC, element, catId, geometryElement, bodyExporterOptions, null, extraParams, out bodyData);
                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(productRepresentation))
                        {
                           extraParams.ClearOpenings();
                           return false;
                        }

                        ExportAsMappedItem(exporterIFC, element, exportType, ifcEnumType, 
                           extraParams, setter, false, localPlacementToUse,
                           productRepresentation, productWrapper);
                     }
                  }
               }
               else
               {
                  for (int ii = 0; ii < numPartsToExport; ii++)
                  {
                     // Check for containment override
                     IFCAnyHandle overrideContainerHnd = null;
                     ParameterUtil.OverrideContainmentParameter(exporterIFC, element, out overrideContainerHnd);

                     using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element, null, null, levels[ii], overrideContainerHnd))
                     {
                        IFCAnyHandle localPlacementToUse = setter.LocalPlacement;

                        using (IFCExportBodyParams extraParams = new IFCExportBodyParams())
                        {
                           SolidMeshGeometryInfo solidMeshCapsule =
                               GeometryUtil.GetClippedSolidMeshGeometry(geometryElement, ranges[ii]);

                           IList<Solid> solids = solidMeshCapsule.GetSolids();
                           IList<Mesh> polyMeshes = solidMeshCapsule.GetMeshes();

                           IList<GeometryObject> geomObjects =
                               FamilyExporterUtil.RemoveInvisibleSolidsAndMeshes(element.Document,
                               exporterIFC, ref solids, ref polyMeshes);

                           if (geomObjects.Count == 0 && (solids.Count > 0 || polyMeshes.Count > 0))
                              return false;

                           bool isColumn = exportType.ExportInstance == IFCEntityType.IfcColumn;
                           bool tryToExportAsExtrusion =
                              !ExporterCacheManager.ExportOptionsCache.ExportAs2x2 || isColumn;

                           if (isColumn)
                           {
                              extraParams.PossibleExtrusionAxes = IFCExtrusionAxes.TryZ;
                           }
                           else
                           {
                              extraParams.PossibleExtrusionAxes = IFCExtrusionAxes.TryXYZ;
                           }

                           BodyData bodyData = null;
                           if (geomObjects.Count > 0)
                           {
                              bodyData = BodyExporter.ExportBody(exporterIFC, element, catId,
                                                                 ElementId.InvalidElementId, geomObjects,
                                                                 bodyExporterOptions, extraParams);
                           }
                           else
                           {
                              IList<GeometryObject> exportedGeometries = new List<GeometryObject>();
                              exportedGeometries.Add(geometryElement);
                              bodyData = BodyExporter.ExportBody(exporterIFC, element, catId,
                                                                 ElementId.InvalidElementId,
                                                                 exportedGeometries, bodyExporterOptions,
                                                                 extraParams);
                           }

                           List<IFCAnyHandle> bodyReps = new List<IFCAnyHandle>();
                           bodyReps.Add(bodyData.RepresentationHnd);

                           IFCAnyHandle productRepresentation =
                               IFCInstanceExporter.CreateProductDefinitionShape(exporterIFC.GetFile(), null,
                                                                                null, bodyReps);
                           if (IFCAnyHandleUtil.IsNullOrHasNoValue(productRepresentation))
                           {
                              extraParams.ClearOpenings();
                              return false;
                           }

                           ExportAsMappedItem(exporterIFC, element, exportType, ifcEnumType,
                              extraParams, setter, true, localPlacementToUse, 
                              productRepresentation, productWrapper);
                        }
                     }
                  }
               }
            }

            tr.Commit();
         }
         return true;
      }

      private static void ExportAsMappedItem(ExporterIFC exporterIFC, Element element, 
         IFCExportInfoPair exportType, string ifcEnumType, IFCExportBodyParams extraParams,
         PlacementSetter setter, bool isSplitByLevel, IFCAnyHandle localPlacementToUse, 
         IFCAnyHandle productRepresentation, ProductWrapper productWrapper)
      {
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
         ElementId typeId = element.GetTypeId();
         ElementType type = element.Document.GetElement(typeId) as ElementType;
         IFCAnyHandle styleHandle = null;
         ElementId matId = ElementId.InvalidElementId;
         Options geomOptions = GeometryUtil.GetIFCExportGeometryOptions();
         bool hasMaterialAssociatedToType = false;

         if (type != null)
         {
            var typeKey = new TypeObjectKey(typeId, ElementId.InvalidElementId, false, exportType, ElementId.InvalidElementId);
            
            FamilyTypeInfo currentTypeInfo = 
               ExporterCacheManager.FamilySymbolToTypeInfoCache.Find(typeKey);

            if (!currentTypeInfo.IsValid())
            {
               HashSet<IFCAnyHandle> propertySetsOpt = new HashSet<IFCAnyHandle>();
               IList<IFCAnyHandle> repMapListOpt = new List<IFCAnyHandle>();

               string typeGuid = FamilyExporterUtil.GetGUIDForFamilySymbol(element as FamilyInstance, type, exportType);
               styleHandle = FamilyExporterUtil.ExportGenericType(exporterIFC, exportType, propertySetsOpt, repMapListOpt, element, type, typeGuid);
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(styleHandle))
               {
                  productWrapper.RegisterHandleWithElementType(type, exportType, styleHandle, null);
                  currentTypeInfo.Style = styleHandle;
                  ExporterCacheManager.FamilySymbolToTypeInfoCache.Register(typeKey, currentTypeInfo, false);

                  Element elementType = element.Document.GetElement(element.GetTypeId());
                  matId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(element.get_Geometry(geomOptions), elementType, element);

                  if (matId != ElementId.InvalidElementId)
                  {
                     currentTypeInfo.MaterialIdList = new List<ElementId>() { matId };
                     hasMaterialAssociatedToType = true;
                     CategoryUtil.CreateMaterialAssociation(exporterIFC, styleHandle, matId);
                  }
               }
            }
            else
            {
               styleHandle = currentTypeInfo.Style;
               if (currentTypeInfo.MaterialIdList != null && currentTypeInfo.MaterialIdList.Count > 0)
                  hasMaterialAssociatedToType = true;
            }
         }

         string instanceGUID;
         if (isSplitByLevel)
         {
            instanceGUID = GUIDUtil.GenerateIFCGuidFrom(
               GUIDUtil.CreateGUIDString(element, "Level: " + setter.LevelId.ToString()));
         }
         else
         {
            instanceGUID = GUIDUtil.CreateGUID(element);
         }

         bool roomRelated = !FamilyExporterUtil.IsDistributionFlowElementSubType(exportType);

         ElementId roomId = ElementId.InvalidElementId;
         if (roomRelated)
         {
            roomId = setter.UpdateRoomRelativeCoordinates(element, out localPlacementToUse);
         }

         // For MEP object creation
         IFCAnyHandle instanceHandle = IFCInstanceExporter.CreateGenericIFCEntity(exportType,
            exporterIFC, element, instanceGUID, ownerHistory, localPlacementToUse, 
            productRepresentation);
         
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(instanceHandle))
            return;
         if (matId == ElementId.InvalidElementId && !hasMaterialAssociatedToType)
         {
            matId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(element.get_Geometry(geomOptions), element);
            if (matId != ElementId.InvalidElementId)
               CategoryUtil.CreateMaterialAssociation(exporterIFC, instanceHandle, matId);
         }

         if (roomId != ElementId.InvalidElementId)
         {
            //exporterIFC.RelateSpatialElement(roomId, instanceHandle);
            ExporterCacheManager.SpaceInfoCache.RelateToSpace(roomId, instanceHandle);
            productWrapper.AddElement(element, instanceHandle, setter, extraParams, false, exportType);
         }
         else
         {
            productWrapper.AddElement(element, instanceHandle, setter, extraParams, true, exportType);
         }

         OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, element, extraParams, null, exporterIFC, localPlacementToUse, setter, productWrapper);

         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(styleHandle))
            ExporterCacheManager.TypeRelationsCache.Add(styleHandle, instanceHandle);

         ExporterCacheManager.MEPCache.Register(element, instanceHandle);

         // add to system export cache
         // SystemExporter.ExportSystem(exporterIFC, element, instanceHandle);
      }
   }
}
