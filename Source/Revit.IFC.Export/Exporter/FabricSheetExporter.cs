//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012  Autodesk, Inc.
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
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.Structure;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export a Revit element as IfcReinforcingMesh.
   /// </summary>
   class FabricSheetExporter
   {
      /// <summary>
      /// Exports a FabricArea as an IfcGroup.  There is no geometry to export.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      /// <returns>True if exported successfully, false otherwise.</returns>
      public static bool ExportFabricArea(ExporterIFC exporterIFC, Element element, ProductWrapper productWrapper)
      {
         if (element == null)
            return false;

         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcGroup;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return false;
         
         HashSet<IFCAnyHandle> fabricSheetHandles = null;
         if (!ExporterCacheManager.FabricAreaHandleCache.TryGetValue(element.Id, out fabricSheetHandles))
            return false;

         if (fabricSheetHandles == null || fabricSheetHandles.Count == 0)
            return false;

         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            string guid = GUIDUtil.CreateGUID(element);
            IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
            string revitObjectType = NamingUtil.GetFamilyAndTypeName(element);
            string name = NamingUtil.GetNameOverride(element, revitObjectType);
            string description = NamingUtil.GetDescriptionOverride(element, null);
            string objectType = NamingUtil.GetObjectTypeOverride(element, revitObjectType);

            IFCAnyHandle fabricArea = IFCInstanceExporter.CreateGroup(file, guid,
                ownerHistory, name, description, objectType);

            IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcGroup);
            productWrapper.AddElement(element, fabricArea, exportInfo);

            IFCInstanceExporter.CreateRelAssignsToGroup(file, GUIDUtil.CreateGUID(), ownerHistory,
                null, null, fabricSheetHandles, null, fabricArea);

            tr.Commit();
            return true;
         }
      }

      /// <summary>
      /// Exports an element as an IfcReinforcingMesh.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      /// <returns>True if exported successfully, false otherwise.</returns>
      public static bool ExportFabricSheet(ExporterIFC exporterIFC, FabricSheet sheet,
          GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         if (sheet == null || geometryElement == null)
            return false;

         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcReinforcingMesh;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return false;

         Document doc = sheet.Document;
         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            // Check for containment override
            IFCAnyHandle overrideContainerHnd = null;
            ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, sheet, out overrideContainerHnd);

            using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, sheet, null, null, overrideContainerId, overrideContainerHnd))
            {
               using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
               {
                  ecData.SetLocalPlacement(placementSetter.LocalPlacement);

                  ElementId categoryId = CategoryUtil.GetSafeCategoryId(sheet);

                  ElementId materialId = ElementId.InvalidElementId;
                  ParameterUtil.GetElementIdValueFromElementOrSymbol(sheet, BuiltInParameter.MATERIAL_ID_PARAM, out materialId);

                  string guid = GUIDUtil.CreateGUID(sheet);
                  IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
                  string revitObjectType = NamingUtil.GetFamilyAndTypeName(sheet);

                  IFCAnyHandle localPlacement = ecData.GetLocalPlacement();

                  string steelGrade = NamingUtil.GetOverrideStringValue(sheet, "SteelGrade", null);
                  double? meshLength = sheet.CutOverallLength;
                  double? meshWidth = sheet.CutOverallWidth;

                  Element fabricSheetTypeElem = doc.GetElement(sheet.GetTypeId());
                  FabricSheetType fabricSheetType = (fabricSheetTypeElem == null) ? null : (fabricSheetTypeElem as FabricSheetType);

                  double longitudinalBarNominalDiameter = 0.0;
                  double transverseBarNominalDiameter = 0.0;
                  double longitudinalBarCrossSectionArea = 0.0;
                  double transverseBarCrossSectionArea = 0.0;
                  double longitudinalBarSpacing = 0.0;
                  double transverseBarSpacing = 0.0;
                  if (fabricSheetType != null)
                  {
                     Element majorFabricWireTypeElem = doc.GetElement(fabricSheetType.MajorDirectionWireType);
                     FabricWireType majorFabricWireType = (majorFabricWireTypeElem == null) ? null : (majorFabricWireTypeElem as FabricWireType);
                     if (majorFabricWireType != null)
                     {
                        longitudinalBarNominalDiameter = UnitUtil.ScaleLength(majorFabricWireType.WireDiameter);
                        double localRadius = longitudinalBarNominalDiameter / 2.0;
                        longitudinalBarCrossSectionArea = localRadius * localRadius * Math.PI;
                     }

                     Element minorFabricWireTypeElem = doc.GetElement(fabricSheetType.MinorDirectionWireType);
                     FabricWireType minorFabricWireType = (minorFabricWireTypeElem == null) ? null : (minorFabricWireTypeElem as FabricWireType);
                     if (minorFabricWireType != null)
                     {
                        transverseBarNominalDiameter = UnitUtil.ScaleLength(minorFabricWireType.WireDiameter);
                        double localRadius = transverseBarNominalDiameter / 2.0;
                        transverseBarCrossSectionArea = localRadius * localRadius * Math.PI;
                     }

                     longitudinalBarSpacing = UnitUtil.ScaleLength(fabricSheetType.MajorSpacing);
                     transverseBarSpacing = UnitUtil.ScaleLength(fabricSheetType.MinorSpacing);
                  }

                  ISet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();

                  IList<Curve> wireCenterlines = sheet.GetWireCenterlines(WireDistributionDirection.Major);
                  foreach (Curve wireCenterline in wireCenterlines)
                  {
                     IFCAnyHandle bodyItem = GeometryUtil.CreateSweptDiskSolid(exporterIFC, file, wireCenterline, longitudinalBarNominalDiameter, null);
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(bodyItem))
                        bodyItems.Add(bodyItem);
                  }

                  wireCenterlines = sheet.GetWireCenterlines(WireDistributionDirection.Minor);
                  foreach (Curve wireCenterline in wireCenterlines)
                  {
                     IFCAnyHandle bodyItem = GeometryUtil.CreateSweptDiskSolid(exporterIFC, file, wireCenterline, transverseBarNominalDiameter, null);
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(bodyItem))
                        bodyItems.Add(bodyItem);
                  }

                  IFCAnyHandle shapeRep = (bodyItems.Count > 0) ?
                      RepresentationUtil.CreateAdvancedSweptSolidRep(exporterIFC, sheet, categoryId, exporterIFC.Get3DContextHandle("Body"), bodyItems, null) :
                      null;
                  IList<IFCAnyHandle> shapeReps = null;
                  if (shapeRep != null)
                  {
                     shapeReps = new List<IFCAnyHandle>();
                     shapeReps.Add(shapeRep);
                  }
                  IFCAnyHandle prodRep = (shapeReps != null) ? IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapeReps) : null;

                  IFCAnyHandle fabricSheet = IFCInstanceExporter.CreateReinforcingMesh(exporterIFC, sheet, guid, ownerHistory, localPlacement,
                      prodRep, steelGrade, meshLength, meshWidth, longitudinalBarNominalDiameter, transverseBarNominalDiameter,
                      longitudinalBarCrossSectionArea, transverseBarCrossSectionArea, longitudinalBarSpacing, transverseBarSpacing);
                  IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcReinforcingMesh);

                  ElementId fabricAreaId = sheet.FabricAreaOwnerId;
                  if (fabricAreaId != ElementId.InvalidElementId)
                  {
                     HashSet<IFCAnyHandle> fabricSheets = null;
                     if (!ExporterCacheManager.FabricAreaHandleCache.TryGetValue(fabricAreaId, out fabricSheets))
                     {
                        fabricSheets = new HashSet<IFCAnyHandle>();
                        ExporterCacheManager.FabricAreaHandleCache[fabricAreaId] = fabricSheets;
                     }
                     fabricSheets.Add(fabricSheet);
                  }

                  productWrapper.AddElement(sheet, fabricSheet, placementSetter.LevelInfo, ecData, true, exportInfo);

                  CategoryUtil.CreateMaterialAssociation(exporterIFC, fabricSheet, materialId);
               }
            }
            tr.Commit();
            return true;
         }
      }
   }
}
