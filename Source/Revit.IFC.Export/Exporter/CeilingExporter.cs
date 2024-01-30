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
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export ceilings.
   /// </summary>
   class CeilingExporter
   {
      /// <summary>
      /// Exports a ceiling to IFC covering.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="ceiling">
      /// The ceiling element to be exported.
      /// </param>
      /// <param name="geomElement">
      /// The geometry element.
      /// </param>
      /// <param name="productWrapper">
      /// The ProductWrapper.
      /// </param>
      public static void ExportCeilingElement(ExporterIFC exporterIFC, Ceiling ceiling, ref GeometryElement geomElement, ProductWrapper productWrapper)
      {
         string ifcEnumType = ExporterUtil.GetIFCTypeFromExportTable(exporterIFC, ceiling);
         if (String.IsNullOrEmpty(ifcEnumType))
            ifcEnumType = "CEILING";
         ExportCovering(exporterIFC, ceiling, ref geomElement, ifcEnumType, productWrapper);
      }

      /// <summary>
      /// Exports an element as IFC covering.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element to be exported.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportCovering(ExporterIFC exporterIFC, Element element, ref GeometryElement geomElem, string ifcEnumType, ProductWrapper productWrapper)
      {
         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         IFCEntityType elementClassTypeEnum = IFCEntityType.IfcCovering;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return;

         IFCFile file = exporterIFC.GetFile();
         MaterialLayerSetInfo layersetInfo = new MaterialLayerSetInfo(exporterIFC, element, productWrapper);

         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            // For IFC4RV export, Element will be split into its parts(temporarily) in order to export the wall by its parts
            // If Parts are created by code and not by user then their name should be equal to Material name.
            bool setMaterialNameToPartName = ExporterUtil.CreateParts(element, layersetInfo.MaterialIds.Count, ref geomElem);
            ExporterUtil.ExportPartAs exportPartAs = ExporterUtil.CanExportByComponentsOrParts(element);
            bool exportByComponents = exportPartAs == ExporterUtil.ExportPartAs.ShapeAspect;
            bool exportParts = exportPartAs == ExporterUtil.ExportPartAs.Part;

            if (exportParts && !PartExporter.CanExportElementInPartExport(element, element.LevelId, false))
               return;

            // Check for containment override
            IFCAnyHandle overrideContainerHnd = null;
            ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, element, out overrideContainerHnd);
            IList<IFCAnyHandle> representations = new List<IFCAnyHandle>();

            using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element, null, null, overrideContainerId, overrideContainerHnd))
            {
               using (IFCExportBodyParams ecData = new IFCExportBodyParams())
               {
                  ElementId categoryId = CategoryUtil.GetSafeCategoryId(element);

                  IFCAnyHandle prodRep = null;
                  if (!exportParts)
                  {
                     ecData.SetLocalPlacement(setter.LocalPlacement);
                     ecData.PossibleExtrusionAxes = (element is FamilyInstance) ? IFCExtrusionAxes.TryXYZ : IFCExtrusionAxes.TryZ;

                     BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                     if (exportByComponents)
                     {
                        prodRep = RepresentationUtil.CreateProductDefinitionShapeWithoutBodyRep(exporterIFC, element, categoryId, geomElem, representations);
                     }
                     else
                     {
                        prodRep = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC, element,
                            categoryId, geomElem, bodyExporterOptions, null, ecData, true);
                     }

                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(prodRep))
                     {
                        ecData.ClearOpenings();
                        return;
                     }
                  }

                  // We will use the category of the element to set a default value for the covering.
                  string defaultCoveringEnumType = null;

                  if (categoryId == new ElementId(BuiltInCategory.OST_Ceilings))
                     defaultCoveringEnumType = "CEILING";
                  else if (categoryId == new ElementId(BuiltInCategory.OST_Floors))
                     defaultCoveringEnumType = "FLOORING";
                  else if (categoryId == new ElementId(BuiltInCategory.OST_Roofs))
                     defaultCoveringEnumType = "ROOFING";

                  string instanceGUID = GUIDUtil.CreateGUID(element);
                  string coveringType = IFCValidateEntry.GetValidIFCPredefinedTypeType(ifcEnumType, defaultCoveringEnumType, "IfcCoveringType");

                  IFCAnyHandle covering = IFCInstanceExporter.CreateCovering(exporterIFC, element, instanceGUID, ExporterCacheManager.OwnerHistoryHandle,
                      setter.LocalPlacement, prodRep, coveringType);

                  if (exportParts)
                  {
                     PartExporter.ExportHostPart(exporterIFC, element, covering, productWrapper, setter, setter.LocalPlacement, null, setMaterialNameToPartName);
                  }
                  else if (exportByComponents)
                  {
                     IFCAnyHandle hostShapeRepFromParts = PartExporter.ExportHostPartAsShapeAspects(exporterIFC, element, prodRep,
                        productWrapper, setter, setter.LocalPlacement, ElementId.InvalidElementId, layersetInfo, ecData);
                  }

                  ExporterUtil.AddIntoComplexPropertyCache(covering, layersetInfo);

                  IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcCovering, IFCEntityType.IfcCoveringType, coveringType);
                  IFCAnyHandle typeHnd = ExporterUtil.CreateGenericTypeFromElement(element, exportInfo, file, productWrapper);
                  ExporterCacheManager.TypeRelationsCache.Add(typeHnd, covering);

                  bool containInSpace = false;
                  IFCAnyHandle localPlacementToUse = setter.LocalPlacement;

                  // Ceiling containment in Space is generally required and not specific to any view
                  if (ExporterCacheManager.CeilingSpaceRelCache.ContainsKey(element.Id))
                  {
                     IList<ElementId> roomlist = ExporterCacheManager.CeilingSpaceRelCache[element.Id];

                     // Process Ceiling to be contained in a Space only when it is exactly bounding one Space
                     if (roomlist.Count == 1)
                     {
                        productWrapper.AddElement(element, covering, setter, ecData, false, exportInfo);

                        // Modify the Ceiling placement to be relative to the Space that it bounds 
                        IFCAnyHandle roomPlacement = IFCAnyHandleUtil.GetObjectPlacement(ExporterCacheManager.SpaceInfoCache.FindSpaceHandle(roomlist[0]));
                        Transform relTrf = ExporterIFCUtils.GetRelativeLocalPlacementOffsetTransform(roomPlacement, localPlacementToUse);
                        Transform inverseTrf = relTrf.Inverse;
                        IFCAnyHandle relLocalPlacement = ExporterUtil.CreateAxis2Placement3D(file, inverseTrf.Origin, inverseTrf.BasisZ, inverseTrf.BasisX);
                        IFCAnyHandleUtil.SetAttribute(localPlacementToUse, "PlacementRelTo", roomPlacement);
                        GeometryUtil.SetRelativePlacement(localPlacementToUse, relLocalPlacement);

                        ExporterCacheManager.SpaceInfoCache.RelateToSpace(roomlist[0], covering);
                        containInSpace = true;
                     }
                  }

                  // if not contained in Space, assign it to default containment in Level
                  if (!containInSpace)
                     productWrapper.AddElement(element, covering, setter, ecData, true, exportInfo);

                  if (!exportParts)
                  {
                     Ceiling ceiling = element as Ceiling;
                     if (ceiling != null)
                     {
                        HostObjectExporter.ExportHostObjectMaterials(exporterIFC, ceiling, covering,
                            geomElem, productWrapper, ElementId.InvalidElementId, Toolkit.IFCLayerSetDirection.Axis3, null, null);
                     }
                     else
                     {
                        ElementId matId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(geomElem, element);
                        CategoryUtil.CreateMaterialAssociation(exporterIFC, covering, matId);
                     }
                  }

                  OpeningUtil.CreateOpeningsIfNecessary(covering, element, ecData, null,
                      exporterIFC, ecData.GetLocalPlacement(), setter, productWrapper);
               }
            }
            transaction.Commit();
         }
      }
   }
}