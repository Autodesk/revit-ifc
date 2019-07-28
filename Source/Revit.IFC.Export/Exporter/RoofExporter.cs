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
   /// Provides methods to export Roof elements.
   /// </summary>
   class RoofExporter
   {
      const string slabRoofPredefinedType = "ROOF";

      /// <summary>
      /// Exports a roof to IfcRoof.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="ifcEnumType">The roof type.</param>
      /// <param name="roof">The roof element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportRoof(ExporterIFC exporterIFC, string ifcEnumType, Element roof, GeometryElement geometryElement,
          ProductWrapper productWrapper)
      {
         if (roof == null || geometryElement == null)
            return;

         IFCFile file = exporterIFC.GetFile();

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
                  BodyData bodyData;
                  IFCAnyHandle representation = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC, roof,
                      categoryId, geometryElement, bodyExporterOptions, null, ecData, out bodyData);

                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(representation))
                  {
                     ecData.ClearOpenings();
                     return;
                  }

                  bool exportSlab = ecData.ScaledLength > MathUtil.Eps();

                  string guid = GUIDUtil.CreateGUID(roof);
                  IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
                  IFCAnyHandle localPlacement = ecData.GetLocalPlacement();
                  //string predefinedType = GetIFCRoofType(ifcEnumType);
                  string predefinedType = IFCValidateEntry.GetValidIFCPredefinedTypeType(ifcEnumType, "NOTDEFINED", IFCEntityType.IfcRoofType.ToString());

                  IFCAnyHandle roofHnd = IFCInstanceExporter.CreateRoof(exporterIFC, roof, guid, ownerHistory,
                      localPlacement, exportSlab ? null : representation, predefinedType);

                  // Export IfcRoofType
                  IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcRoof, IFCEntityType.IfcRoofType, predefinedType);
                  if (exportInfo.ExportType != IFCEntityType.UnKnown)
                  {
                     string overridePDefType;
                     if (ParameterUtil.GetStringValueFromElementOrSymbol(roof, "IfcExportType", out overridePDefType) == null) // change IFCType to consistent parameter of IfcExportType
                        if (ParameterUtil.GetStringValueFromElementOrSymbol(roof, "IfcType", out overridePDefType) == null)  // support IFCType for legacy support
                           if (string.IsNullOrEmpty(predefinedType))
                              predefinedType = "NOTDEFINED";

                     if (!string.IsNullOrEmpty(overridePDefType))
                        exportInfo.ValidatedPredefinedType = overridePDefType;
                     else
                        exportInfo.ValidatedPredefinedType = predefinedType;

                     IFCAnyHandle typeHnd = ExporterUtil.CreateGenericTypeFromElement(roof, exportInfo, file, ownerHistory, predefinedType, productWrapper);
                     ExporterCacheManager.TypeRelationsCache.Add(typeHnd, roofHnd);
                  }

                  productWrapper.AddElement(roof, roofHnd, placementSetter.LevelInfo, ecData, true, exportInfo);

                  // will export its host object materials later if it is a roof
                  if (!(roof is RoofBase))
                     CategoryUtil.CreateMaterialAssociation(exporterIFC, roofHnd, bodyData.MaterialIds);

                  if (exportSlab)
                  {
                     string slabGUID = GUIDUtil.CreateSubElementGUID(roof, (int)IFCRoofSubElements.RoofSlabStart);
                     string slabName = IFCAnyHandleUtil.GetStringAttribute(roofHnd, "Name") + ":1";
                     IFCAnyHandle slabLocalPlacementHnd = ExporterUtil.CopyLocalPlacement(file, localPlacement);

                     IFCAnyHandle slabHnd = IFCInstanceExporter.CreateSlab(exporterIFC, roof, slabGUID, ownerHistory,
                        slabLocalPlacementHnd, representation, slabRoofPredefinedType);
                     IFCAnyHandleUtil.OverrideNameAttribute(slabHnd, slabName);
                     Transform offsetTransform = (bodyData != null) ? bodyData.OffsetTransform : Transform.Identity;
                     OpeningUtil.CreateOpeningsIfNecessary(slabHnd, roof, ecData, offsetTransform,
                         exporterIFC, slabLocalPlacementHnd, placementSetter, productWrapper);

                     ExporterUtil.RelateObject(exporterIFC, roofHnd, slabHnd);
                     IFCExportInfoPair slabRoofExportType = new IFCExportInfoPair(IFCEntityType.IfcSlab, slabRoofPredefinedType);

                     productWrapper.AddElement(null, slabHnd, placementSetter.LevelInfo, ecData, false, slabRoofExportType);
                     CategoryUtil.CreateMaterialAssociation(exporterIFC, slabHnd, bodyData.MaterialIds);

                     // Create type

                     IFCAnyHandle slabRoofTypeHnd = ExporterUtil.CreateGenericTypeFromElement(roof, slabRoofExportType, exporterIFC.GetFile(), ownerHistory, slabRoofPredefinedType, productWrapper);
                     ExporterCacheManager.TypeRelationsCache.Add(slabRoofTypeHnd, slabHnd);
                  }
               }
               tr.Commit();
            }
         }
      }

      /// <summary>
      /// Exports a roof to IfcRoof.
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
               string ifcEnumType = ExporterUtil.GetIFCTypeFromExportTable(exporterIFC, roof);

               IFCAnyHandle roofHnd = ExportRoofOrFloorAsContainer(exporterIFC, ifcEnumType, roof,
                   geometryElement, productWrapper);
               if (IFCAnyHandleUtil.IsNullOrHasNoValue(roofHnd))
                  ExportRoof(exporterIFC, ifcEnumType, roof, geometryElement, productWrapper);

               // call for host objects; curtain roofs excused from call (no material information)
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
      /// <param name="ifcEnumType">The roof type.</param>
      /// <param name="element">The roof or floor element.</param>
      /// <param name="geometry">The geometry of the element.</param>
      /// <param name="productWrapper">The product wrapper.</param>
      /// <returns>The roof handle.</returns>
      /// <remarks>For floors, if there is only one component, return null, as we do not want to create a container.</remarks>
      public static IFCAnyHandle ExportRoofOrFloorAsContainer(ExporterIFC exporterIFC, string ifcEnumType, Element element, GeometryElement geometry, ProductWrapper productWrapper)
      {
         IFCFile file = exporterIFC.GetFile();

         // We support ExtrusionRoofs, FootPrintRoofs, and Floors only.
         bool elementIsRoof = (element is ExtrusionRoof) || (element is FootPrintRoof);
         bool elementIsFloor = (element is Floor);
         if (!elementIsRoof && !elementIsFloor)
            return null;

         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcRoof;
         if (elementIsFloor)
            elementClassTypeEnum = Common.Enums.IFCEntityType.IfcSlab;
         IFCExportInfoPair roofExportType = new IFCExportInfoPair(elementClassTypeEnum, ifcEnumType);

         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return null;

         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            // Check for containment override
            IFCAnyHandle overrideContainerHnd = null;
            ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, element, out overrideContainerHnd);

            using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element, null, null, overrideContainerId, overrideContainerHnd))
            {
               IFCAnyHandle localPlacement = setter.LocalPlacement;
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

                        //string hostObjectType = IFCValidateEntry.GetValidIFCPredefinedType(element, ifcEnumType);

                        if (elementIsRoof)
                           hostObjectHandle = IFCInstanceExporter.CreateRoof(exporterIFC, element, elementGUID, ownerHistory,
                            localPlacement, prodRepHnd, ifcEnumType);
                        else
                           hostObjectHandle = IFCInstanceExporter.CreateSlab(exporterIFC, element, elementGUID, ownerHistory,
                           localPlacement, prodRepHnd, ifcEnumType);

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
                           string subSlabType = elementIsRoof ? "ROOF" : ifcEnumType;

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
                              IFCAnyHandle shapeRep = ExtrusionExporter.CreateExtrudedSolidFromCurveLoop(exporterIFC, null, curveLoops, lcs, extrusionDir, scaledExtrusionDepth, false);
                              if (IFCAnyHandleUtil.IsNullOrHasNoValue(shapeRep))
                              {
                                 productWrapper.ClearInternalHandleWrapperData(element);
                                 return null;
                              }

                              ElementId matId = HostObjectExporter.GetFirstLayerMaterialId(element as HostObject);
                              BodyExporter.CreateSurfaceStyleForRepItem(exporterIFC, element.Document, shapeRep, matId);

                              HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
                              bodyItems.Add(shapeRep);
                              shapeRep = RepresentationUtil.CreateSweptSolidRep(exporterIFC, element, catId, exporterIFC.Get3DContextHandle("Body"), bodyItems, null);
                              IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
                              shapeReps.Add(shapeRep);

                              IFCAnyHandle repHnd = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapeReps);

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

                              ExporterUtil.RelateObject(exporterIFC, hostObjectHandle, slabHnd);
                              IFCExportInfoPair slabRoofExportType = new IFCExportInfoPair(IFCEntityType.IfcSlab, slabRoofPredefinedType);
                              productWrapper.AddElement(null, slabHnd, setter, slabExtrusionCreationData, false, slabRoofExportType);
                              CategoryUtil.CreateMaterialAssociation(exporterIFC, slabHnd, matId);

                              // Create type
                              IFCAnyHandle slabRoofTypeHnd = ExporterUtil.CreateGenericTypeFromElement(element, slabRoofExportType, exporterIFC.GetFile(), ownerHistory, slabRoofPredefinedType, productWrapper);
                              ExporterCacheManager.TypeRelationsCache.Add(slabRoofTypeHnd, slabHnd);

                              elementHandles.Add(slabHnd);
                              slabHandles.Add(slabHnd);

                              hostObjectOpeningLoops.Add(slabCurveLoop);
                              maximumScaledDepth = Math.Max(maximumScaledDepth, scaledDepth);
                              loopNum++;
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

      /// <summary>
      /// Export the roof to IfcRoof containing its parts.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The roof element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportRoofAsParts(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcRoof;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
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


               //need to convert the string to enum
               string ifcEnumType = ExporterUtil.GetIFCTypeFromExportTable(exporterIFC, element);
               //ifcEnumType = IFCValidateEntry.GetValidIFCPredefinedType(element, ifcEnumType);

               IFCAnyHandle roofHandle = IFCInstanceExporter.CreateRoof(exporterIFC, element, elementGUID, ownerHistory,
                   localPlacement, prodRepHnd, ifcEnumType);
               IFCExportInfoPair exportType = new IFCExportInfoPair(elementClassTypeEnum, ifcEnumType);

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