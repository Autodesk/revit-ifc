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
using Autodesk.Revit.DB.Structure;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using System.Reflection;


namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export rebar couplers.
   /// </summary>
   class RebarCouplerExporter
   {
      /// <summary>
      /// Exports a Rebar Coupler,
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="coupler">The RebarCoupler element.</param>
      /// <param name="productWrapper">The product wrapper.</param>
      public static void ExportCoupler(ExporterIFC exporterIFC, RebarCoupler coupler, ProductWrapper productWrapper)
      {
         if (coupler == null)
            return;

         ElementId typeId = coupler.GetTypeId();
         FamilySymbol familySymbol = coupler.Document.GetElement(typeId) as FamilySymbol;
         if (familySymbol == null)
            return;

         IFCExportInfoPair exportType = ExporterUtil.GetProductExportType(coupler, out string ifcEnumType);

         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(exportType.ExportInstance))
            return;

         ElementId categoryId = CategoryUtil.GetSafeCategoryId(coupler);

         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
         Options options = GeometryUtil.GetIFCExportGeometryOptions();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            bool containedInAssembly = ExporterUtil.IsContainedInAssembly(coupler);
            TypeObjectKey typeKey = new(typeId, ElementId.InvalidElementId, false, exportType, ElementId.InvalidElementId, containedInAssembly);
            
            FamilyTypeInfo currentTypeInfo = ExporterCacheManager.FamilySymbolToTypeInfoCache.Find(typeKey);
            bool found = currentTypeInfo.IsValid();
            if (!found)
            {
               string typeObjectType = NamingUtil.CreateIFCObjectName(exporterIFC, familySymbol);

               HashSet<IFCAnyHandle> propertySetsOpt = new();

               GeometryElement exportGeometry = familySymbol.get_Geometry(options);

               BodyExporterOptions bodyExporterOptions = new(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
               BodyData bodyData = BodyExporter.ExportBody(exporterIFC, coupler, categoryId, ElementId.InvalidElementId, exportGeometry, bodyExporterOptions, null);

               IFCAnyHandle origin = ExporterUtil.CreateAxis2Placement3D(file);
               List<IFCAnyHandle> repMap = [ IFCInstanceExporter.CreateRepresentationMap(file, origin, bodyData.RepresentationHnd) ];

               string typeGuid = GUIDUtil.GenerateIFCGuidFrom(familySymbol, exportType);
               IFCAnyHandle styleHandle = FamilyExporterUtil.ExportGenericType(file, exportType, propertySetsOpt, repMap, coupler, familySymbol, typeGuid);
               productWrapper.RegisterHandleWithElementType(familySymbol, exportType, styleHandle, propertySetsOpt);

               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(styleHandle))
               {
                  currentTypeInfo.Style = styleHandle;
                  ExporterCacheManager.FamilySymbolToTypeInfoCache.Register(typeKey, currentTypeInfo, false);
               }
            }

            int nCouplerQuantity = coupler.GetCouplerQuantity();
            if (nCouplerQuantity <= 0)
               return;

            HashSet<IFCAnyHandle> createdRebarCouplerHandles = new();
            string origInstanceName = NamingUtil.GetNameOverride(coupler, NamingUtil.GetIFCName(coupler));

            bool hasTypeInfo = !IFCAnyHandleUtil.IsNullOrHasNoValue(currentTypeInfo.Style);
            bool bExportAsSingleIFCEntity = !ExporterCacheManager.ExportOptionsCache.ExportBarsInUniformSetsAsSeparateIFCEntities;
            HashSet<IFCAnyHandle> representations = new();
            for (int idx = 0; idx < nCouplerQuantity; idx++)
            {
               string extraId = bExportAsSingleIFCEntity ? string.Empty : ":" + (idx + 1).ToString();
               string instanceGUID = GUIDUtil.GenerateIFCGuidFrom(GUIDUtil.CreateGUIDString(coupler, "Fastener" + extraId));

               IFCAnyHandle style = currentTypeInfo.Style;
               if (IFCAnyHandleUtil.IsNullOrHasNoValue(style))
                  return;

               IList<IFCAnyHandle> repMapList = GeometryUtil.GetRepresentationMaps(style);
               if ((repMapList?.Count ?? 0) == 0)
                  return;

               IFCAnyHandle contextOfItems3d = ExporterCacheManager.Get3DContextHandle(IFCRepresentationIdentifier.Body);

               Transform trf = coupler.GetCouplerPositionTransform(idx);

               if (!bExportAsSingleIFCEntity)
               {
                  representations = new() { ExporterUtil.CreateDefaultMappedItem(file, repMapList[0], XYZ.Zero) };
               }
               else
               {
                  IFCAnyHandle axis1 = ExporterUtil.CreateDirection(file, trf.BasisX);
                  IFCAnyHandle axis2 = ExporterUtil.CreateDirection(file, trf.BasisY);
                  IFCAnyHandle axis3 = ExporterUtil.CreateDirection(file, trf.BasisZ);
                  IFCAnyHandle origin = ExporterUtil.CreateCartesianPoint(file, UnitUtil.ScaleLength(trf.Origin));
                  double scale = 1.0;
                  IFCAnyHandle mappingTarget = IFCInstanceExporter.CreateCartesianTransformationOperator3D(file, axis1, axis2, origin, scale, axis3);
                  representations.Add(IFCInstanceExporter.CreateMappedItem(file, repMapList[0], mappingTarget));
               }

               if (!bExportAsSingleIFCEntity || (bExportAsSingleIFCEntity && representations.Count == nCouplerQuantity))
               {
                  List<IFCAnyHandle> shapeReps = 
                     [ RepresentationUtil.CreateBodyMappedItemRep(exporterIFC, coupler, categoryId, contextOfItems3d, representations) ];

                  IFCAnyHandle productRepresentation = IFCInstanceExporter.CreateProductDefinitionShape(exporterIFC.GetFile(), null, null, shapeReps);

                  if (bExportAsSingleIFCEntity)
                     trf = Transform.Identity;

                  using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, coupler, trf, null))
                  {
                     IFCAnyHandle instanceHandle = IFCInstanceExporter.CreateGenericIFCEntity(exportType, file, 
                        coupler, instanceGUID, ownerHistory,setter.LocalPlacement, productRepresentation);

                     string extraName = bExportAsSingleIFCEntity ? string.Empty : (": " + idx);
                     string instanceName = NamingUtil.GetNameOverride(instanceHandle, coupler, origInstanceName + extraName);

                     IFCAnyHandleUtil.OverrideNameAttribute(instanceHandle, instanceName);
                     createdRebarCouplerHandles.Add(instanceHandle);

                     productWrapper.AddElement(coupler, instanceHandle, setter, null, true, exportType);

                     if (hasTypeInfo)
                     {
                        ExporterCacheManager.TypeRelationsCache.Add(currentTypeInfo.Style, instanceHandle);
                     }
                  }
               }
            }

            string couplerGUID = GUIDUtil.CreateGUID(coupler);

            if (nCouplerQuantity > 1 && !bExportAsSingleIFCEntity)
            {
               // Create a group to hold all of the created IFC entities, if the coupler aren't already in an assembly.  
               // We want to avoid nested groups of groups of couplers.
               if (coupler.AssemblyInstanceId == ElementId.InvalidElementId)
               {
                  string revitObjectType = NamingUtil.GetFamilyAndTypeName(coupler);
                  string name = NamingUtil.GetNameOverride(coupler, revitObjectType);
                  string description = NamingUtil.GetDescriptionOverride(coupler, null);
                  string objectType = NamingUtil.GetObjectTypeOverride(coupler, revitObjectType);

                  IFCAnyHandle rebarGroup = IFCInstanceExporter.CreateGroup(file, couplerGUID,
                      ownerHistory, name, description, objectType);

                  productWrapper.AddElement(coupler, rebarGroup, exportType);

                  string groupGuid = GUIDUtil.GenerateIFCGuidFrom(
                     GUIDUtil.CreateGUIDString(IFCEntityType.IfcRelAssignsToGroup, string.Empty, rebarGroup));
                  IFCInstanceExporter.CreateRelAssignsToGroup(file, groupGuid, ownerHistory,
                      null, null, createdRebarCouplerHandles, null, rebarGroup);
               }
            }
            else
            {
               // We will update the GUID of the one created element to be the element GUID.
               // This will allow the IfcGUID parameter to be use/set if appropriate.
               ExporterUtil.SetGlobalId(createdRebarCouplerHandles.ElementAt(0), couplerGUID, coupler);
            }

            tr.Commit();
         }
      }
   }
}