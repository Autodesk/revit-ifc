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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export Roof elements.
   /// </summary>
   class RoofExporter
   {
      const string slabRoofPredefinedType = "ROOF";

      /// <summary>
      /// Exports a roof to IfcRoof.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="roof">The roof element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportRoof(ExporterIFC exporterIFC, Element roof, GeometryElement geometryElement,
          ProductWrapper productWrapper)
      {
         if (roof == null || geometryElement == null)
            return;

         string ifcEnumType;
         IFCExportInfoPair roofExportType = ExporterUtil.GetExportType(exporterIFC, roof, out ifcEnumType);
         if (roofExportType.IsUnKnown)
         {
            roofExportType = new IFCExportInfoPair(IFCEntityType.IfcRoof, "");
         }

         MaterialLayerSetInfo layersetInfo = null;
         IFCFile file = exporterIFC.GetFile();
         Document doc = roof.Document;

         using (SubTransaction tempPartTransaction = new SubTransaction(doc))
         {
            // For IFC4RV export, Roof will be split into its parts(temporarily) in order to export the roof by its parts
            bool exportByComponents = ExporterUtil.ShouldExportByComponents(roof);

            using (IFCTransaction tr = new IFCTransaction(file))
            {
               // Check for containment override
               IFCAnyHandle overrideContainerHnd = null;
               ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, roof, out overrideContainerHnd);

               using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, roof, null, null, overrideContainerId, overrideContainerHnd))
               {
                  using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                  {
                     // If the roof is an in-place family, we will allow any arbitrary orientation.  While this may result in some
                     // in-place "cubes" exporting with the wrong direction, it is unlikely that an in-place family would be
                     // used for this reason in the first place.
                     ecData.PossibleExtrusionAxes = (roof is FamilyInstance) ? IFCExtrusionAxes.TryXYZ : IFCExtrusionAxes.TryZ;
                     ecData.AreInnerRegionsOpenings = true;
                     ecData.SetLocalPlacement(placementSetter.LocalPlacement);

                     ElementId categoryId = CategoryUtil.GetSafeCategoryId(roof);

                     BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                     BodyData bodyData = null;
                     IFCAnyHandle prodRep = null;
                     IList<IFCAnyHandle> representations = new List<IFCAnyHandle>();

                     if (!exportByComponents)
                     {
                        prodRep = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC, roof,
                            categoryId, geometryElement, bodyExporterOptions, null, ecData, out bodyData);
                     }
                     else
                     {
                        prodRep = RepresentationUtil.CreateProductDefinitionShapeWithoutBodyRep(exporterIFC, roof, categoryId, geometryElement, representations);
                     }
                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(prodRep))
                     {
                        ecData.ClearOpenings();
                        return;
                     }

                     bool exportSlab = ((ecData.ScaledLength > MathUtil.Eps() || exportByComponents) &&
                        roofExportType.ExportInstance == IFCEntityType.IfcRoof);

                     string guid = GUIDUtil.CreateGUID(roof);
                     IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
                     IFCAnyHandle localPlacement = ecData.GetLocalPlacement();

                     IFCAnyHandle roofHnd = IFCInstanceExporter.CreateGenericIFCEntity(
                        roofExportType, exporterIFC, roof, guid, ownerHistory,
                        localPlacement, exportSlab ? null : prodRep);

                     IFCAnyHandle typeHnd = ExporterUtil.CreateGenericTypeFromElement(roof,
                        roofExportType, file, ownerHistory, roofExportType.ValidatedPredefinedType,
                        productWrapper);
                     ExporterCacheManager.TypeRelationsCache.Add(typeHnd, roofHnd);

                     productWrapper.AddElement(roof, roofHnd, placementSetter.LevelInfo, ecData, true, roofExportType);

                     // will export its host object materials later if it is a roof
                     if (!(roof is RoofBase))
                        CategoryUtil.CreateMaterialAssociation(exporterIFC, roofHnd, bodyData.MaterialIds);

                     if (exportSlab)
                     {
                        string slabGUID = GUIDUtil.CreateSubElementGUID(roof, (int)IFCRoofSubElements.RoofSlabStart);
                        string slabName = IFCAnyHandleUtil.GetStringAttribute(roofHnd, "Name") + ":1";
                        IFCAnyHandle slabLocalPlacementHnd = ExporterUtil.CopyLocalPlacement(file, localPlacement);

                        if (exportByComponents)
                        {
                           IFCAnyHandle hostShapeRepFromParts = PartExporter.ExportHostPartAsShapeAspects(exporterIFC, roof, prodRep,
                              productWrapper, placementSetter, localPlacement, ElementId.InvalidElementId, out layersetInfo, ecData);
                        }

                        IFCAnyHandle slabHnd = IFCInstanceExporter.CreateSlab(exporterIFC, roof, slabGUID, ownerHistory,
                           slabLocalPlacementHnd, prodRep, slabRoofPredefinedType);
                        IFCAnyHandleUtil.OverrideNameAttribute(slabHnd, slabName);

                        Transform offsetTransform = (bodyData != null) ? bodyData.OffsetTransform : Transform.Identity;
                        OpeningUtil.CreateOpeningsIfNecessary(slabHnd, roof, ecData, offsetTransform,
                            exporterIFC, slabLocalPlacementHnd, placementSetter, productWrapper);

                        ExporterUtil.RelateObject(exporterIFC, roofHnd, slabHnd);
                        IFCExportInfoPair slabRoofExportType = new IFCExportInfoPair(IFCEntityType.IfcSlab, slabRoofPredefinedType);

                        productWrapper.AddElement(null, slabHnd, placementSetter.LevelInfo, ecData, false, slabRoofExportType);

                        // Create type

                        IFCAnyHandle slabRoofTypeHnd = ExporterUtil.CreateGenericTypeFromElement(roof, slabRoofExportType, exporterIFC.GetFile(), ownerHistory, slabRoofPredefinedType, productWrapper);
                        ExporterCacheManager.TypeRelationsCache.Add(slabRoofTypeHnd, slabHnd);

                        if (!ExporterUtil.AddIntoComplexPropertyCache(slabHnd, layersetInfo) && bodyData != null)
                           CategoryUtil.CreateMaterialAssociation(exporterIFC, slabHnd, bodyData.MaterialIds);

                        // Create material association here
                        if (layersetInfo != null && !IFCAnyHandleUtil.IsNullOrHasNoValue(layersetInfo.MaterialLayerSetHandle))
                        {
                           CategoryUtil.CreateMaterialAssociation(exporterIFC, slabHnd, layersetInfo.MaterialLayerSetHandle);
                        }
                     }
                  }
                  tr.Commit();
               }
            }
         }
      }

      /// <summary>
      /// Exports a roof element to the appropriate IFC entity.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="roof">The roof element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void Export(ExporterIFC exporterIFC, RoofBase roof, GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction tr = new IFCTransaction(file))
         {
            // export parts or not
            bool exportParts = PartExporter.CanExportParts(roof);
            bool exportAsCurtainRoof = CurtainSystemExporter.IsCurtainSystem(roof);

            if (exportParts)
            {
               if (!PartExporter.CanExportElementInPartExport(roof, roof.LevelId, false))
                  return;
               ExportRoofAsParts(exporterIFC, roof, geometryElement, productWrapper); // Right now, only flat roof could have parts.
            }
            else if (exportAsCurtainRoof)
            {
               CurtainSystemExporter.ExportCurtainRoof(exporterIFC, roof, productWrapper);
            }
            else
            {
               IFCAnyHandle roofHnd = ExportRoofOrFloorAsContainer(exporterIFC, roof,
                   geometryElement, productWrapper);
               if (IFCAnyHandleUtil.IsNullOrHasNoValue(roofHnd))
               {
                  ExportRoof(exporterIFC, roof, geometryElement, productWrapper);
               }

               // call for host objects; curtain roofs excused from call (no material information)
               if (!ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
                  HostObjectExporter.ExportHostObjectMaterials(exporterIFC, roof, productWrapper.GetAnElement(),
                      geometryElement, productWrapper, ElementId.InvalidElementId, IFCLayerSetDirection.Axis3, null, null);
            }
            tr.Commit();
         }
      }

      /// <summary>
      ///  Exports a roof or floor as a container of multiple roof slabs.  Returns the handle, if successful.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="element">The roof or floor element.</param>
      /// <param name="geometry">The geometry of the element.</param>
      /// <param name="productWrapper">The product wrapper.</param>
      /// <returns>The roof handle.</returns>
      /// <remarks>For floors, if there is only one component, return null, as we do not want to create a container.</remarks>
      public static IFCAnyHandle ExportRoofOrFloorAsContainer(ExporterIFC exporterIFC, 
         Element element, GeometryElement geometry, ProductWrapper productWrapper)
      {
         IFCFile file = exporterIFC.GetFile();

         // We support ExtrusionRoofs, FootPrintRoofs, and Floors only.
         bool elementIsRoof = (element is ExtrusionRoof) || (element is FootPrintRoof);
         bool elementIsFloor = (element is Floor);
         if (!elementIsRoof && !elementIsFloor)
            return null;

         string ifcEnumType;
         IFCExportInfoPair roofExportType = ExporterUtil.GetExportType(exporterIFC, element, out ifcEnumType);
         if (roofExportType.IsUnKnown)
         {
            IFCEntityType elementClassTypeEnum = 
               elementIsFloor ? IFCEntityType.IfcSlab: IFCEntityType.IfcRoof;
            roofExportType = new IFCExportInfoPair(elementClassTypeEnum, "");
         }

         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(roofExportType.ExportType))
            return null;

         Document doc = element.Document;
         using (SubTransaction tempPartTransaction = new SubTransaction(doc))
         {
            MaterialLayerSetInfo layersetInfo = new MaterialLayerSetInfo(exporterIFC, element, productWrapper);
            bool hasLayers = false;
            if (layersetInfo.MaterialIds.Count > 1)
               hasLayers = true;
            bool exportByComponents = ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView && hasLayers;

            using (IFCTransaction transaction = new IFCTransaction(file))
            {
               // Check for containment override
               IFCAnyHandle overrideContainerHnd = null;
               ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, element, out overrideContainerHnd);

               // We want to delay creating entity handles until as late as possible, so that if we abort the IFC transaction,
               // we don't have to delete elements.  This is both for performance reasons and to potentially extend the number
               // of projects that can be exported by reducing (a small amount) of waste.
               IList<HostObjectSubcomponentInfo> hostObjectSubcomponents = null;
               try
               {
                  hostObjectSubcomponents = ExporterIFCUtils.ComputeSubcomponents(element as HostObject);
               }
               catch
               {
                  return null;
               }

               if (hostObjectSubcomponents == null)
                  return null;

               int numSubcomponents = hostObjectSubcomponents.Count;
               if (numSubcomponents == 0 || (elementIsFloor && numSubcomponents == 1))
                  return null;

               using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element, null, null, overrideContainerId, overrideContainerHnd))
               {
                  IFCAnyHandle localPlacement = setter.LocalPlacement;


                  IFCAnyHandle hostObjectHandle = null;
                  try
                  {
                     using (IFCExtrusionCreationData extrusionCreationData = new IFCExtrusionCreationData())
                     {
                        IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
                        extrusionCreationData.SetLocalPlacement(localPlacement);
                        extrusionCreationData.ReuseLocalPlacement = true;

                        using (TransformSetter trfSetter = TransformSetter.Create())
                        {
                           IList<GeometryObject> geometryList = new List<GeometryObject>();
                           geometryList.Add(geometry);
                           trfSetter.InitializeFromBoundingBox(exporterIFC, geometryList, extrusionCreationData);

                           IFCAnyHandle prodRepHnd = null;

                           string elementGUID = GUIDUtil.CreateGUID(element);

                           hostObjectHandle = IFCInstanceExporter.CreateGenericIFCEntity(
                              roofExportType, exporterIFC, element, elementGUID, ownerHistory,
                              localPlacement, prodRepHnd);

                           if (IFCAnyHandleUtil.IsNullOrHasNoValue(hostObjectHandle))
                              return null;

                           IList<IFCAnyHandle> elementHandles = new List<IFCAnyHandle>();
                           elementHandles.Add(hostObjectHandle);

                           // If element is floor, then the profile curve loop of hostObjectSubComponent is computed from the top face of the floor
                           // else if element is roof, then the profile curve loop is taken from the bottom face of the roof instead 
                           XYZ extrusionDir = elementIsFloor ? new XYZ(0, 0, -1) : new XYZ(0, 0, 1);

                           ElementId catId = CategoryUtil.GetSafeCategoryId(element);

                           IList<IFCAnyHandle> slabHandles = new List<IFCAnyHandle>();

                           IList<CurveLoop> hostObjectOpeningLoops = new List<CurveLoop>();
                           double maximumScaledDepth = 0.0;

                           using (IFCExtrusionCreationData slabExtrusionCreationData = new IFCExtrusionCreationData())
                           {
                              slabExtrusionCreationData.SetLocalPlacement(extrusionCreationData.GetLocalPlacement());
                              slabExtrusionCreationData.ReuseLocalPlacement = false;
                              slabExtrusionCreationData.ForceOffset = true;

                              int loopNum = 0;
                              int subElementStart = elementIsRoof ? (int)IFCRoofSubElements.RoofSlabStart : (int)IFCSlabSubElements.SubSlabStart;
                              string subSlabType = roofExportType.ValidatedPredefinedType;

                              foreach (HostObjectSubcomponentInfo hostObjectSubcomponent in hostObjectSubcomponents)
                              {
                                 trfSetter.InitializeFromBoundingBox(exporterIFC, geometryList, slabExtrusionCreationData);
                                 Plane plane = hostObjectSubcomponent.GetPlane();
                                 Transform lcs = GeometryUtil.CreateTransformFromPlane(plane);

                                 IList<CurveLoop> curveLoops = new List<CurveLoop>();

                                 CurveLoop slabCurveLoop = hostObjectSubcomponent.GetCurveLoop();
                                 curveLoops.Add(slabCurveLoop);
                                 double slope = Math.Abs(plane.Normal.Z);
                                 double scaledDepth = UnitUtil.ScaleLength(hostObjectSubcomponent.Depth);
                                 double scaledExtrusionDepth = scaledDepth * slope;
                                 IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
                                 IFCAnyHandle repHnd = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapeReps);
                                 string shapeIdent = "Body";
                                 IFCAnyHandle contextOfItems = exporterIFC.Get3DContextHandle(shapeIdent);
                                 string representationType = ShapeRepresentationType.SweptSolid.ToString();

                                 // Create representation items based on the layers
                                 // Because in this case, the Roof components are not derived from Parts, but by "splitting" geometry part that can be extruded,
                                 //    the creation of the Items for IFC4RV will be different by using "manual" split based on the layer thickness
                                 HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
                                 if (!exportByComponents)
                                 {
                                    IFCAnyHandle itemShapeRep = ExtrusionExporter.CreateExtrudedSolidFromCurveLoop(exporterIFC, null, curveLoops, lcs, extrusionDir, scaledExtrusionDepth, false);
                                    if (IFCAnyHandleUtil.IsNullOrHasNoValue(itemShapeRep))
                                    {
                                       productWrapper.ClearInternalHandleWrapperData(element);
                                       return null;
                                    }
                                    ElementId matId = HostObjectExporter.GetFirstLayerMaterialId(element as HostObject);
                                    BodyExporter.CreateSurfaceStyleForRepItem(exporterIFC, element.Document, itemShapeRep, matId);
                                    bodyItems.Add(itemShapeRep);
                                 }
                                 else
                                 {
                                    double scaleProj = extrusionDir.DotProduct(plane.Normal);
                                    foreach (Tuple<ElementId, string, double> matLayerInfo in layersetInfo.MaterialIds)
                                    {
                                       double itemExtrDepth = matLayerInfo.Item3;
                                       IFCAnyHandle itemShapeRep = ExtrusionExporter.CreateExtrudedSolidFromCurveLoop(exporterIFC, null, curveLoops, lcs, extrusionDir, itemExtrDepth, false);
                                       if (IFCAnyHandleUtil.IsNullOrHasNoValue(itemShapeRep))
                                       {
                                          productWrapper.ClearInternalHandleWrapperData(element);
                                          return null;
                                       }

                                       //ElementId matId = HostObjectExporter.GetFirstLayerMaterialId(element as HostObject);
                                       BodyExporter.CreateSurfaceStyleForRepItem(exporterIFC, element.Document, itemShapeRep, matLayerInfo.Item1);

                                       bodyItems.Add(itemShapeRep);
                                       IFCAnyHandle representationOfItem = RepresentationUtil.CreateShapeRepresentation(exporterIFC, element, catId, contextOfItems, shapeIdent,
                                          representationType, new HashSet<IFCAnyHandle>() { itemShapeRep });
                                       IFCAnyHandle shapeAspect = IFCInstanceExporter.CreateShapeAspect(file, new List<IFCAnyHandle>() { representationOfItem }, matLayerInfo.Item2, null, null, repHnd);

                                       XYZ offset = new XYZ(0, 0, itemExtrDepth / scaleProj);   // offset is calculated as extent in the direction of extrusion
                                       lcs.Origin += offset;
                                    }
                                 }

                                 IFCAnyHandle shapeRep = RepresentationUtil.CreateSweptSolidRep(exporterIFC, element, catId, exporterIFC.Get3DContextHandle("Body"), bodyItems, null);
                                 shapeReps.Add(shapeRep);
                                 IFCAnyHandleUtil.SetAttribute(repHnd, "Representations", shapeReps);

                                 // Allow support for up to 256 named IfcSlab components, as defined in IFCSubElementEnums.cs.
                                 string slabGUID = (loopNum < 256) ? GUIDUtil.CreateSubElementGUID(element, subElementStart + loopNum) : GUIDUtil.CreateGUID();

                                 IFCAnyHandle slabPlacement = ExporterUtil.CreateLocalPlacement(file, slabExtrusionCreationData.GetLocalPlacement(), null);
                                 IFCAnyHandle slabHnd = IFCInstanceExporter.CreateSlab(exporterIFC, element, slabGUID, ownerHistory,
                                    slabPlacement, repHnd, subSlabType);
                                 IFCExportInfoPair exportType = new IFCExportInfoPair(IFCEntityType.IfcSlab, subSlabType);

                                 //slab quantities
                                 slabExtrusionCreationData.ScaledLength = scaledExtrusionDepth;
                                 slabExtrusionCreationData.ScaledArea = UnitUtil.ScaleArea(UnitUtil.ScaleArea(hostObjectSubcomponent.AreaOfCurveLoop));
                                 slabExtrusionCreationData.ScaledOuterPerimeter = UnitUtil.ScaleLength(curveLoops[0].GetExactLength());
                                 slabExtrusionCreationData.Slope = UnitUtil.ScaleAngle(MathUtil.SafeAcos(Math.Abs(slope)));

                                 IFCExportInfoPair slabRoofExportType = new IFCExportInfoPair(IFCEntityType.IfcSlab, slabRoofPredefinedType);
                                 productWrapper.AddElement(null, slabHnd, setter, slabExtrusionCreationData, false, slabRoofExportType);

                                 // Create type
                                 IFCAnyHandle slabRoofTypeHnd = ExporterUtil.CreateGenericTypeFromElement(element, slabRoofExportType, exporterIFC.GetFile(), ownerHistory, slabRoofPredefinedType, productWrapper);
                                 ExporterCacheManager.TypeRelationsCache.Add(slabRoofTypeHnd, slabHnd);

                                 elementHandles.Add(slabHnd);
                                 slabHandles.Add(slabHnd);

                                 hostObjectOpeningLoops.Add(slabCurveLoop);
                                 maximumScaledDepth = Math.Max(maximumScaledDepth, scaledDepth);
                                 loopNum++;

                                 ExporterUtil.AddIntoComplexPropertyCache(slabHnd, layersetInfo);

                                 // Create material association here
                                 if (layersetInfo != null && !IFCAnyHandleUtil.IsNullOrHasNoValue(layersetInfo.MaterialLayerSetHandle))
                                 {
                                    CategoryUtil.CreateMaterialAssociation(exporterIFC, slabHnd, layersetInfo.MaterialLayerSetHandle);
                                 }
                              }
                           }

                           productWrapper.AddElement(element, hostObjectHandle, setter, extrusionCreationData, true, roofExportType);

                           ExporterUtil.RelateObjects(exporterIFC, null, hostObjectHandle, slabHandles);

                           OpeningUtil.AddOpeningsToElement(exporterIFC, elementHandles, hostObjectOpeningLoops, element, null, maximumScaledDepth,
                               null, setter, localPlacement, productWrapper);

                           
                           transaction.Commit();
                           return hostObjectHandle;
                        }
                     }
                  }
                  catch
                  {
                     // SOmething wrong with the above process, unable to create the extrusion data. Reset any internal handles that may have been partially created since they are not committed
                     productWrapper.ClearInternalHandleWrapperData(element);
                     return null;
                  }
                  finally
                  {
                     exporterIFC.ClearFaceWithElementHandleMap();
                  }
               }
            }
         }
      }

      /// <summary>
      /// Export the roof to IfcRoof containing its parts.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The roof element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportRoofAsParts(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         string ifcEnumType;
         IFCExportInfoPair exportType = ExporterUtil.GetExportType(exporterIFC, element, out ifcEnumType);
         if (exportType.IsUnKnown)
            exportType = new IFCExportInfoPair(IFCEntityType.IfcRoof);

         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(exportType.ExportType))
            return;

         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            // Check for containment override
            IFCAnyHandle overrideContainerHnd = null;
            ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, element, out overrideContainerHnd);

            using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element, null, null, overrideContainerId, overrideContainerHnd))
            {
               IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
               IFCAnyHandle localPlacement = setter.LocalPlacement;

               IFCAnyHandle prodRepHnd = null;

               string elementGUID = GUIDUtil.CreateGUID(element);

               IFCAnyHandle roofHandle = IFCInstanceExporter.CreateGenericIFCEntity(
                  exportType, exporterIFC, 
                  element, elementGUID, ownerHistory,
                  localPlacement, prodRepHnd);
               
               // Export the parts
               PartExporter.ExportHostPart(exporterIFC, element, roofHandle, productWrapper, setter, localPlacement, null);

               productWrapper.AddElement(element, roofHandle, setter, null, true, exportType);

               transaction.Commit();
            }
         }
      }

      /// <summary>
      /// Gets IFCRoofType from roof type name.
      /// </summary>
      /// <param name="roofTypeName">The roof type name.</param>
      /// <returns>The IFCRoofType.</returns>
      public static string GetIFCRoofType(string roofTypeName)
      {
         string typeName = NamingUtil.RemoveSpacesAndUnderscores(roofTypeName);

         if (String.Compare(typeName, "ROOFTYPEENUM", true) == 0 ||
             String.Compare(typeName, "ROOFTYPEENUMFREEFORM", true) == 0)
            return "FREEFORM";
         if (String.Compare(typeName, "FLAT", true) == 0 ||
             String.Compare(typeName, "FLATROOF", true) == 0)
            return "FLAT_ROOF";
         if (String.Compare(typeName, "SHED", true) == 0 ||
             String.Compare(typeName, "SHEDROOF", true) == 0)
            return "SHED_ROOF";
         if (String.Compare(typeName, "GABLE", true) == 0 ||
             String.Compare(typeName, "GABLEROOF", true) == 0)
            return "GABLE_ROOF";
         if (String.Compare(typeName, "HIP", true) == 0 ||
             String.Compare(typeName, "HIPROOF", true) == 0)
            return "HIP_ROOF";
         if (String.Compare(typeName, "HIPPED_GABLE", true) == 0 ||
             String.Compare(typeName, "HIPPED_GABLEROOF", true) == 0)
            return "HIPPED_GABLE_ROOF";
         if (String.Compare(typeName, "MANSARD", true) == 0 ||
             String.Compare(typeName, "MANSARDROOF", true) == 0)
            return "MANSARD_ROOF";
         if (String.Compare(typeName, "BARREL", true) == 0 ||
             String.Compare(typeName, "BARRELROOF", true) == 0)
            return "BARREL_ROOF";
         if (String.Compare(typeName, "BUTTERFLY", true) == 0 ||
             String.Compare(typeName, "BUTTERFLYROOF", true) == 0)
            return "BUTTERFLY_ROOF";
         if (String.Compare(typeName, "PAVILION", true) == 0 ||
             String.Compare(typeName, "PAVILIONROOF", true) == 0)
            return "PAVILION_ROOF";
         if (String.Compare(typeName, "DOME", true) == 0 ||
             String.Compare(typeName, "DOMEROOF", true) == 0)
            return "DOME_ROOF";

         return typeName;        //return unchanged. Validation for ENUM will be done later specific to schema version
      }
   }
}