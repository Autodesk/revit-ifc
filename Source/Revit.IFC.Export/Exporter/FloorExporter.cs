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
using Autodesk.Revit.DB.Structure;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Extensions;
using Revit.IFC.Export.Exporter.PropertySet;


namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export floor elements.
   /// </summary>
   class FloorExporter
   {
      /// <summary>
      /// Exports a generic element as an IfcSlab.</summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="floor">The floor element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="ifcEnumType">The string value represents the IFC type.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      /// <returns>True if the floor is exported successfully, false otherwise.</returns>
      public static void ExportGenericSlab(ExporterIFC exporterIFC, Element slabElement, GeometryElement geometryElement, string ifcEnumType,
          ProductWrapper productWrapper)
      {
         if (geometryElement == null)
            return;

         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            using (IFCTransformSetter transformSetter = IFCTransformSetter.Create())
            {
               // Check for containment override
               IFCAnyHandle overrideContainerHnd = null;
               ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, slabElement, out overrideContainerHnd);

               using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, slabElement, null, null, overrideContainerId, overrideContainerHnd))
               {
                  using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                  {
                     bool exportParts = PartExporter.CanExportParts(slabElement);

                     IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
                     IFCAnyHandle localPlacement = placementSetter.LocalPlacement;

                     IFCAnyHandle prodDefHnd = null;
                     bool isBRepSlabHnd = false;

                     if (!exportParts)
                     {
                        ecData.SetLocalPlacement(localPlacement);

                        ElementId catId = CategoryUtil.GetSafeCategoryId(slabElement);

                        BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.Medium);
                        BodyData bodyData;
                        prodDefHnd = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC,
                            slabElement, catId, geometryElement, bodyExporterOptions, null, ecData, out bodyData);
                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(prodDefHnd))
                        {
                           ecData.ClearOpenings();
                           return;
                        }
                        isBRepSlabHnd = (bodyData.ShapeRepresentationType == ShapeRepresentationType.Brep || bodyData.ShapeRepresentationType == ShapeRepresentationType.Tessellation);
                     }

                     // Create the slab from either the extrusion or the BRep information.
                     string ifcGUID = GUIDUtil.CreateGUID(slabElement);

                     string entityType = IFCValidateEntry.GetValidIFCType<IFCSlabType>(slabElement, ifcEnumType, "FLOOR");

                     IFCAnyHandle slabHnd = IFCInstanceExporter.CreateSlab(exporterIFC, slabElement, ifcGUID, ownerHistory,
                             localPlacement, exportParts ? null : prodDefHnd, entityType);
                     IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcSlab, entityType);

                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(slabHnd))
                        return;

                     if (exportParts)
                        PartExporter.ExportHostPart(exporterIFC, slabElement, slabHnd, productWrapper, placementSetter, localPlacement, null);

                     productWrapper.AddElement(slabElement, slabHnd, placementSetter, ecData, true, exportInfo);

                     if (!exportParts)
                     {
                        IFCAnyHandle typeHnd = ExporterUtil.CreateGenericTypeFromElement(slabElement, exportInfo, file, ownerHistory, entityType, productWrapper);
                        ExporterCacheManager.TypeRelationsCache.Add(typeHnd, slabHnd);

                        if (slabElement is HostObject)
                        {
                           HostObject hostObject = slabElement as HostObject;

                           HostObjectExporter.ExportHostObjectMaterials(exporterIFC, hostObject, slabHnd,
                               geometryElement, productWrapper, ElementId.InvalidElementId, Toolkit.IFCLayerSetDirection.Axis3, isBRepSlabHnd, typeHnd);
                        }
                        else if (slabElement is FamilyInstance)
                        {
                           ElementId matId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(geometryElement, slabElement);
                           //Document doc = slabElement.Document;
                           if (typeHnd != null)
                              CategoryUtil.CreateMaterialAssociation(exporterIFC, typeHnd, matId);
                           else
                              CategoryUtil.CreateMaterialAssociation(exporterIFC, slabHnd, matId);
                        }

                        OpeningUtil.CreateOpeningsIfNecessary(slabHnd, slabElement, ecData, null,
                            exporterIFC, ecData.GetLocalPlacement(), placementSetter, productWrapper);

                        if (ecData.GetOpenings().Count == 0)
                           OpeningUtil.AddOpeningsToElement(exporterIFC, slabHnd, slabElement, null, ecData.ScaledHeight, null, placementSetter, localPlacement, productWrapper);
                     }
                  }
               }
               tr.Commit();

               return;
            }
         }
      }

      // At the moment, the expected input for this function is either an IfcProduct or
      // an IfcProductDefinitionShape.  We register IfcRepresentations inside IfcProductDefinitionShapes.
      private static void PotentiallyFixPresentationLayerAssignment(Element element, ICollection<IFCAnyHandle> entityHandles)
      {
         // Not supported for IFC2x2.  Avoid computations below.
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
            return;

         // We may have a situation where we have a presentation layer override, but the native code doesn't
         // have that functionality.  In this case, we will create an entry here, and reconcile the difference
         // during the end export operation.
         string ifcCADLayerOverride = RepresentationUtil.GetPresentationLayerOverride(element);
         if (string.IsNullOrWhiteSpace(ifcCADLayerOverride))
            return;

         foreach (IFCAnyHandle entityHandle in entityHandles)
         {
            IList<IFCAnyHandle> representations = null;
            if (IFCAnyHandleUtil.IsSubTypeOf(entityHandle, IFCEntityType.IfcProduct))
            {
               representations = IFCAnyHandleUtil.GetProductRepresentations(entityHandle);
            }
            else if (IFCAnyHandleUtil.IsSubTypeOf(entityHandle, IFCEntityType.IfcProductDefinitionShape))
            {
               representations = IFCAnyHandleUtil.GetRepresentations(entityHandle);
            }

            // Not currently supported.
            if (representations == null)
               continue;

            foreach (IFCAnyHandle representation in representations)
            {
               ExporterCacheManager.PresentationLayerSetCache.AddRepresentationToLayer(ifcCADLayerOverride, representation);
            }
         }
      }

      /// <summary>
      /// Exports a CeilingAndFloor element to IFC.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="floor">The floor element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportCeilingAndFloorElement(ExporterIFC exporterIFC, CeilingAndFloor floorElement, ref GeometryElement geometryElement,
          ProductWrapper productWrapper)
      {
         if (geometryElement == null)
            return;

         IFCFile file = exporterIFC.GetFile();

         string ifcEnumType;
         IFCExportInfoPair exportType = ExporterUtil.GetProductExportType(exporterIFC, floorElement, out ifcEnumType);
         IFCAnyHandle type = null;

         if (!ElementFilteringUtil.IsElementVisible(floorElement))
            return;

         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum;
         if (Enum.TryParse<Common.Enums.IFCEntityType>(exportType.ExportInstance.ToString(), out elementClassTypeEnum)
            || Enum.TryParse<Common.Enums.IFCEntityType>(exportType.ExportType.ToString(), out elementClassTypeEnum))
            if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
               return;

         Document doc = floorElement.Document;
         using (SubTransaction tempPartTransaction = new SubTransaction(doc))
         {
            MaterialLayerSetInfo layersetInfo = new MaterialLayerSetInfo(exporterIFC, floorElement, productWrapper);
            // For IFC4RV export, Element will be split into its parts(temporarily) in order to export the wall by its parts
            // If Parts are created by code and not by user then their names should be equal to Material names.
            bool setMaterialNameToPartName = ExporterUtil.CreateParts(floorElement, layersetInfo.MaterialIds.Count, ref geometryElement);
            ExporterUtil.ExportPartAs exportPartAs = ExporterUtil.CanExportByComponentsOrParts(floorElement);
            bool exportByComponents = exportPartAs == ExporterUtil.ExportPartAs.ShapeAspect;
            bool exportParts = exportPartAs == ExporterUtil.ExportPartAs.Part;

            if (exportParts && !PartExporter.CanExportElementInPartExport(floorElement, floorElement.LevelId, false))
               return;

            using (IFCTransaction tr = new IFCTransaction(file))
            {
               bool canExportAsContainerOrWithExtrusionAnalyzer = (!exportParts && (floorElement is Floor));

               if (canExportAsContainerOrWithExtrusionAnalyzer)
               {
                  // Try to export the Floor slab as a container.  If that succeeds, we are done.
                  // If we do export the floor as a container, it will take care of the local placement and transform there, so we need to leave
                  // this out of the IFCTransformSetter and PlacementSetter scopes below, or else we'll get double transforms.
                  IFCAnyHandle floorHnd = RoofExporter.ExportRoofOrFloorAsContainer(exporterIFC, floorElement, geometryElement, productWrapper);
                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(floorHnd))
                  {
                     tr.Commit();
                     return;
                  }
               }

               IList<IFCAnyHandle> slabHnds = new List<IFCAnyHandle>();
               IList<IFCAnyHandle> brepSlabHnds = new List<IFCAnyHandle>();
               IList<IFCAnyHandle> nonBrepSlabHnds = new List<IFCAnyHandle>();

               IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

               using (IFCTransformSetter transformSetter = IFCTransformSetter.Create())
               {
                  // Check for containment override
                  IFCAnyHandle overrideContainerHnd = null;
                  ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, floorElement, out overrideContainerHnd);

                  using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, floorElement, null, null, overrideContainerId, overrideContainerHnd))
                  {
                     IFCAnyHandle localPlacement = placementSetter.LocalPlacement;

                     // The routine ExportExtrudedSlabOpenings is called if exportedAsInternalExtrusion is true, and it requires having a valid level association.
                     // Disable calling ExportSlabAsExtrusion if we can't handle potential openings.
                     bool canExportAsInternalExtrusion = placementSetter.LevelInfo != null && !ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView;
                     bool exportedAsInternalExtrusion = false;

                     ElementId catId = CategoryUtil.GetSafeCategoryId(floorElement);

                     IList<IFCAnyHandle> prodReps = new List<IFCAnyHandle>();
                     IList<ShapeRepresentationType> repTypes = new List<ShapeRepresentationType>();
                     IList<IList<CurveLoop>> extrusionLoops = new List<IList<CurveLoop>>();
                     IList<IFCExtrusionCreationData> loopExtraParams = new List<IFCExtrusionCreationData>();
                     Plane floorPlane = GeometryUtil.CreateDefaultPlane();
                     IFCExtrusionCreationData ecData = new IFCExtrusionCreationData();

                     IList<IFCAnyHandle> localPlacements = new List<IFCAnyHandle>();

                     if (!exportParts)
                     {
                        if (canExportAsContainerOrWithExtrusionAnalyzer)
                        {
                           Floor floor = floorElement as Floor;

                           // Next, try to use the ExtrusionAnalyzer for the limited cases it handles - 1 solid, no openings, end clippings only.
                           // Also limited to cases with line and arc boundaries.
                           //
                           SolidMeshGeometryInfo solidMeshInfo = GeometryUtil.GetSplitSolidMeshGeometry(geometryElement);
                           IList<Solid> solids = solidMeshInfo.GetSolids();
                           IList<Mesh> meshes = solidMeshInfo.GetMeshes();
                           IList<GeometryObject> gObjs = FamilyExporterUtil.RemoveInvisibleSolidsAndMeshes(floorElement.Document, exporterIFC, ref solids, ref meshes);

                           if (solids.Count == 1 && meshes.Count == 0)
                           {
                              // floorExtrusionDirection is set to (0, 0, -1) because extrusionAnalyzerFloorPlane is computed from the top face of the floor
                              XYZ floorExtrusionDirection = new XYZ(0, 0, -1);
                              XYZ modelOrigin = XYZ.Zero;

                              XYZ floorOrigin = floor.GetVerticalProjectionPoint(modelOrigin, FloorFace.Top);
                              if (floorOrigin == null)
                              {
                                 // GetVerticalProjectionPoint may return null if FloorFace.Top is an edited face that doesn't 
                                 // go through the Revit model origin.  We'll try the midpoint of the bounding box instead.
                                 BoundingBoxXYZ boundingBox = floorElement.get_BoundingBox(null);
                                 modelOrigin = (boundingBox.Min + boundingBox.Max) / 2.0;
                                 floorOrigin = floor.GetVerticalProjectionPoint(modelOrigin, FloorFace.Top);
                              }

                              if (floorOrigin != null)
                              {
                                 XYZ floorDir = floor.GetNormalAtVerticalProjectionPoint(floorOrigin, FloorFace.Top);
                                 Plane extrusionAnalyzerFloorBasePlane = GeometryUtil.CreatePlaneByNormalAtOrigin(floorDir);

                                 GenerateAdditionalInfo additionalInfo = GenerateAdditionalInfo.GenerateBody;
                                 additionalInfo |= ExporterCacheManager.ExportOptionsCache.ExportAs4 ?
                                    GenerateAdditionalInfo.GenerateFootprint : GenerateAdditionalInfo.None;

                                 // Skip generate body item for IFC4RV. It will be handled later in PartExporter.ExportHostPartAsShapeAspects()
                                 if (exportByComponents)
                                    additionalInfo &= ~GenerateAdditionalInfo.GenerateBody;

                                 ExtrusionExporter.ExtraClippingData extraClippingData = null;
                                 HandleAndData floorAndProperties =
                                     ExtrusionExporter.CreateExtrusionWithClippingAndProperties(exporterIFC, floorElement, false,
                                     catId, solids[0], extrusionAnalyzerFloorBasePlane, floorOrigin, floorExtrusionDirection, null, 
                                     out extraClippingData,
                                     addInfo: additionalInfo);
                                 if (extraClippingData.CompletelyClipped)
                                    return;

                                 IList<IFCAnyHandle> representations = new List<IFCAnyHandle>();
                                 if (floorAndProperties.Handle != null)
                                 {
                                    representations.Add(floorAndProperties.Handle);
                                    repTypes.Add(ShapeRepresentationType.SweptSolid);
                                 }

                                 // Footprint representation will only be exported in export to IFC4
                                 if (((additionalInfo & GenerateAdditionalInfo.GenerateFootprint) != 0) && (floorAndProperties.FootprintInfo != null))
                                 {
                                    IFCAnyHandle footprintShapeRep = floorAndProperties.FootprintInfo.CreateFootprintShapeRepresentation(exporterIFC);
                                    representations.Add(footprintShapeRep);
                                 }

                                 if (exportByComponents)
                                 {
                                    IFCAnyHandle prodRep = RepresentationUtil.CreateProductDefinitionShapeWithoutBodyRep(exporterIFC, floorElement, catId, geometryElement, representations);
                                    prodReps.Add(prodRep);
                                 }
                                 else if (representations.Count > 0 && floorAndProperties.Handle != null)   // Only when at least the body rep exists will come here
                                 {
                                    IFCAnyHandle prodRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);
                                    prodReps.Add(prodRep);
                                 }

                                 if (floorAndProperties.Data != null)
                                    loopExtraParams.Add(floorAndProperties.Data);
                              }
                           }
                        }

                        // Use internal routine as backup that handles openings.
                        if (prodReps.Count == 0 && canExportAsInternalExtrusion && !exportByComponents)
                        {
                           exportedAsInternalExtrusion = ExporterIFCUtils.ExportSlabAsExtrusion(exporterIFC, floorElement,
                               geometryElement, transformSetter, localPlacement, out localPlacements, out prodReps,
                               out extrusionLoops, out loopExtraParams, floorPlane);
                           PotentiallyFixPresentationLayerAssignment(floorElement, prodReps);
                           for (int ii = 0; ii < prodReps.Count; ii++)
                           {
                              // all are extrusions
                              repTypes.Add(ShapeRepresentationType.SweptSolid);

                              // Footprint representation will only be exported in export to IFC4
                              if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
                              {
                                 if (extrusionLoops.Count > ii)
                                 {
                                    if (extrusionLoops[ii].Count > 0)
                                    {
                                       // Get the extrusion footprint using the first Curveloop. Transform needs to be obtained from the returned local placement
                                       Transform lcs = ExporterIFCUtils.GetUnscaledTransform(exporterIFC, localPlacements[ii]);
                                       IFCAnyHandle footprintGeomRepItem = GeometryUtil.CreateIFCCurveFromCurveLoop(exporterIFC, extrusionLoops[ii][0], lcs, floorPlane.Normal);

                                       IFCAnyHandle contextOfItemsFootprint = exporterIFC.Get3DContextHandle("FootPrint");
                                       ISet<IFCAnyHandle> repItem = new HashSet<IFCAnyHandle>();
                                       repItem.Add(footprintGeomRepItem);
                                       IFCAnyHandle footprintShapeRepresentation = RepresentationUtil.CreateBaseShapeRepresentation(exporterIFC, contextOfItemsFootprint, "FootPrint", "Curve2D", repItem);
                                       IList<IFCAnyHandle> reps = new List<IFCAnyHandle>();
                                       reps.Add(footprintShapeRepresentation);
                                       IFCAnyHandleUtil.AddRepresentations(prodReps[ii], reps);
                                    }
                                 }
                              }
                           }
                        }

                        IFCAnyHandle prodDefHnd;
                        if (prodReps.Count == 0)
                        {
                           if (exportByComponents)
                           {
                              prodDefHnd = RepresentationUtil.CreateProductDefinitionShapeWithoutBodyRep(exporterIFC, floorElement, catId, geometryElement, null);
                              prodReps.Add(prodDefHnd);
                           }
                           else
                           {
                              // Brep representation using tesellation after ExportSlabAsExtrusion does not return prodReps
                              BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.Medium);
                              BodyData bodyData;
                              prodDefHnd = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC,
                                    floorElement, catId, geometryElement, bodyExporterOptions, null, ecData, out bodyData);
                              if (IFCAnyHandleUtil.IsNullOrHasNoValue(prodDefHnd))
                              {
                                 ecData.ClearOpenings();
                                 return;
                              }

                              prodReps.Add(prodDefHnd);
                              repTypes.Add(bodyData.ShapeRepresentationType);
                           }
                        }
                     }

                     // Create the slab from either the extrusion or the BRep information.
                     string ifcGUID = GUIDUtil.CreateGUID(floorElement);

                     int numReps = exportParts ? 1 : prodReps.Count;

                     // Deal with a couple of cases that have non-standard defaults.
                     switch (exportType.ExportInstance)
                     {
                        case IFCEntityType.IfcCovering:
                           exportType.ValidatedPredefinedType = IFCValidateEntry.GetValidIFCType<IFCCoveringType>(floorElement, ifcEnumType, "FLOORING");
                           break;
                        case IFCEntityType.IfcSlab:
                           bool isBaseSlab = false;
#pragma warning disable CS0612, CS0618//AnalyticalModel is obsolette
                           AnalyticalModel analyticalModel = floorElement.GetAnalyticalModel();
                           if (analyticalModel != null)
                           {
                              AnalyzeAs slabFoundationType = analyticalModel.GetAnalyzeAs();
                              isBaseSlab = (slabFoundationType == AnalyzeAs.SlabOnGrade) || (slabFoundationType == AnalyzeAs.Mat);
                           }
                           exportType.ValidatedPredefinedType = IFCValidateEntry.GetValidIFCType<IFCSlabType>(floorElement, ifcEnumType, isBaseSlab ? "BASESLAB" : "FLOOR");
                           break;
#pragma warning restore CS0612, CS0618
                     }

                     int openingCreatedCount = 0;
                     for (int ii = 0; ii < numReps; ii++)
                     {
                        string ifcName = NamingUtil.GetNameOverride(floorElement, NamingUtil.GetIFCNamePlusIndex(floorElement, ii == 0 ? -1 : ii + 1));

                        string currentGUID = (ii == 0) ? ifcGUID : GUIDUtil.CreateGUID();
                        IFCAnyHandle localPlacementHnd = exportedAsInternalExtrusion ? localPlacements[ii] : localPlacement;

                        IFCAnyHandle slabHnd = null;
                        slabHnd = IFCInstanceExporter.CreateGenericIFCEntity(exportType, exporterIFC, floorElement, currentGUID, ownerHistory,
                           localPlacementHnd, exportParts ? null : prodReps[ii]);
                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(slabHnd))
                           return;

                        if (!string.IsNullOrEmpty(ifcName))
                           IFCAnyHandleUtil.OverrideNameAttribute(slabHnd, ifcName);

                        // Pre IFC4 Slab does not have PredefinedType
                        if (!string.IsNullOrEmpty(exportType.ValidatedPredefinedType) && !ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
                           IFCAnyHandleUtil.SetAttribute(slabHnd, "PredefinedType", exportType.ValidatedPredefinedType, true);
                        if(exportParts)
                        {
                           PartExporter.ExportHostPart(exporterIFC, floorElement, slabHnd, productWrapper, placementSetter, localPlacementHnd, null, setMaterialNameToPartName);
                        }
                        else if (exportByComponents)
                        {
                           IFCExtrusionCreationData partECData = new IFCExtrusionCreationData();
                           IFCAnyHandle hostShapeRepFromParts = PartExporter.ExportHostPartAsShapeAspects(exporterIFC, floorElement, prodReps[ii],
                              productWrapper, placementSetter, localPlacement, ElementId.InvalidElementId, layersetInfo, partECData);
                           loopExtraParams.Add(partECData);
                        }

                        slabHnds.Add(slabHnd);

                        // For IFC4RV, export of the geometry is already handled in PartExporter.ExportHostPartAsShapeAspects()
                        if (!exportParts && !exportByComponents)
                        {
                           if (repTypes[ii] == ShapeRepresentationType.Brep || repTypes[ii] == ShapeRepresentationType.Tessellation)
                              brepSlabHnds.Add(slabHnd);
                           else
                              nonBrepSlabHnds.Add(slabHnd);
                        }

                        OpeningUtil.CreateOpeningsIfNecessary(slabHnd, floorElement, ecData, null,
                           exporterIFC, localPlacement, placementSetter, productWrapper);

                        // Try get openings using OpeningUtil if ecData.GetOpening() used in the above call is 0
                        if (ecData.GetOpenings().Count == 0)
                           openingCreatedCount = OpeningUtil.AddOpeningsToElement(exporterIFC, slabHnd, floorElement, null, ecData.ScaledHeight, null, placementSetter, localPlacement, productWrapper);
                     }

                     for (int ii = 0; ii < numReps; ii++)
                     {
                        IFCExtrusionCreationData loopExtraParam = ii < loopExtraParams.Count ? loopExtraParams[ii] : null;
                        productWrapper.AddElement(floorElement, slabHnds[ii], placementSetter, loopExtraParam, true, exportType);

                        type = ExporterUtil.CreateGenericTypeFromElement(floorElement, exportType, file, ownerHistory, exportType.ValidatedPredefinedType, productWrapper);
                        ExporterCacheManager.TypeRelationsCache.Add(type, slabHnds[ii]);

                        ExporterUtil.AddIntoComplexPropertyCache(slabHnds[ii], layersetInfo);
                     }

                     // This call to the native function appears to create Brep opening also when appropriate. But the creation of the IFC instances is not
                     //   controllable from the managed code. Therefore in some cases BRep geometry for Opening will still be exported even in the Reference View
                     // Call this only if no opening created
                     if (exportedAsInternalExtrusion && openingCreatedCount == 0)
                     {
                        ISet<IFCAnyHandle> oldCreatedObjects = productWrapper.GetAllObjects();
                        ExporterIFCUtils.ExportExtrudedSlabOpenings(exporterIFC, floorElement, placementSetter.LevelInfo,
                           localPlacements[0], slabHnds, extrusionLoops, floorPlane, productWrapper.ToNative());
                        ISet<IFCAnyHandle> newCreatedObjects = productWrapper.GetAllObjects();
                        newCreatedObjects.ExceptWith(oldCreatedObjects);
                        PotentiallyFixPresentationLayerAssignment(floorElement, newCreatedObjects);
                     }
                  }

                  if (!exportParts)
                  {
                     if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
                     {
                        HostObjectExporter.ExportHostObjectMaterials(exporterIFC, floorElement, productWrapper.GetAnElement(),
                                 geometryElement, productWrapper, ElementId.InvalidElementId, Toolkit.IFCLayerSetDirection.Axis3, false, type);
                     }
                     else
                     {
                        if (nonBrepSlabHnds.Count > 0)
                        {
                           HostObjectExporter.ExportHostObjectMaterials(exporterIFC, floorElement, nonBrepSlabHnds,
                               geometryElement, productWrapper, ElementId.InvalidElementId, Toolkit.IFCLayerSetDirection.Axis3, false, type);
                        }

                        if (brepSlabHnds.Count > 0)
                        {
                           HostObjectExporter.ExportHostObjectMaterials(exporterIFC, floorElement, brepSlabHnds,
                               geometryElement, productWrapper, ElementId.InvalidElementId, Toolkit.IFCLayerSetDirection.Axis3, true, type);
                        }
                     }
                  }
               }

               tr.Commit();
               return;
            }
         }
      }

      /// <summary>
      /// Gets IFCSlabType from slab type name.
      /// </summary>
      /// <param name="ifcEnumType">The slab type name.</param>
      /// <returns>The IFCSlabType.</returns>
      public static IFCSlabType GetIFCSlabType(string ifcEnumType)
      {
         if (String.IsNullOrEmpty(ifcEnumType))
            return IFCSlabType.Floor;

         string ifcEnumTypeWithoutSpaces = NamingUtil.RemoveSpacesAndUnderscores(ifcEnumType);

         if (String.Compare(ifcEnumTypeWithoutSpaces, "USERDEFINED", true) == 0)
            return IFCSlabType.UserDefined;
         if (String.Compare(ifcEnumTypeWithoutSpaces, "FLOOR", true) == 0)
            return IFCSlabType.Floor;
         if (String.Compare(ifcEnumTypeWithoutSpaces, "ROOF", true) == 0)
            return IFCSlabType.Roof;
         if (String.Compare(ifcEnumTypeWithoutSpaces, "LANDING", true) == 0)
            return IFCSlabType.Landing;
         if (String.Compare(ifcEnumTypeWithoutSpaces, "BASESLAB", true) == 0)
            return IFCSlabType.BaseSlab;

         return IFCSlabType.Floor;
      }
   }
}