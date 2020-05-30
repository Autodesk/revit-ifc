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
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit;
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
   /// Provides methods to export family instances.
   /// </summary>
   public class FamilyInstanceExporter
   {
      private static bool IsExtrusionFriendlyType(IFCEntityType entityType)
      {
         return (entityType == IFCEntityType.IfcColumn)
            || (entityType == IFCEntityType.IfcBeam)
            || (entityType == IFCEntityType.IfcMember)
            || (entityType == IFCEntityType.IfcPile);
      }

      /// <summary>
      /// Exports a family instance to corresponding IFC object.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="familyInstance">
      /// The family instance to be exported.
      /// </param>
      /// <param name="geometryElement">
      /// The geometry element.
      /// </param>
      /// <param name="productWrapper">
      /// The ProductWrapper.
      /// </param>
      public static void ExportFamilyInstanceElement(ExporterIFC exporterIFC,
         FamilyInstance familyInstance, GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         // Don't export family if it is invisible, or has a null geometry.
         if (familyInstance.Invisible || geometryElement == null)
            return;

         // Don't export family instance if it has a curtain grid host; the host will be in charge of exporting.
         Element host = familyInstance.Host;
         if (CurtainSystemExporter.IsCurtainSystem(host))
            return;

         FamilySymbol familySymbol = familyInstance.Symbol;
         Family family = familySymbol.Family;
         if (family == null)
            return;

         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            string ifcEnumType;
            IFCExportInfoPair exportType = ExporterUtil.GetExportType(exporterIFC, familyInstance, out ifcEnumType);

            if (exportType.IsUnKnown)
               return;

            // TODO: This step now appears to be redundant with the rest of the steps, but to change it is too much of risk of regression. Reserve it for future refactoring
            if (ExportGenericToSpecificElement(exporterIFC, familyInstance, geometryElement, exportType, ifcEnumType, productWrapper))
            {
               tr.Commit();
               return;
            }

            // If we are exporting a column, we may need to split it into parts by level.  Create a list of ranges.
            IList<ElementId> levels = new List<ElementId>();
            IList<IFCRange> ranges = new List<IFCRange>();

            // We will not split walls and columns if the assemblyId is set, as we would like to keep the original wall
            // associated with the assembly, on the level of the assembly.
            bool splitColumn = (exportType.ExportInstance == IFCEntityType.IfcColumn) && (ExporterCacheManager.ExportOptionsCache.WallAndColumnSplitting) &&
                (familyInstance.AssemblyInstanceId == ElementId.InvalidElementId);
            if (splitColumn)
            {
               LevelUtil.CreateSplitLevelRangesForElement(exporterIFC, exportType, familyInstance, out levels, out ranges);
            }

            int numPartsToExport = ranges.Count;
            if (numPartsToExport == 0)
            {
               ExportFamilyInstanceAsMappedItem(exporterIFC, familyInstance, exportType, ifcEnumType, productWrapper,
                  ElementId.InvalidElementId, null, null);
            }
            else
            {
               using (ExporterStateManager.RangeIndexSetter rangeSetter = new ExporterStateManager.RangeIndexSetter())
               {
                  for (int ii = 0; ii < numPartsToExport; ii++)
                  {
                     rangeSetter.IncreaseRangeIndex();
                     ExportFamilyInstanceAsMappedItem(exporterIFC, familyInstance, exportType, ifcEnumType, productWrapper,
                       levels[ii], ranges[ii], null);
                  }
               }

               if (ExporterCacheManager.DummyHostCache.HasRegistered(familyInstance.Id))
               {
                  using (ExporterStateManager.RangeIndexSetter rangeSetter = new ExporterStateManager.RangeIndexSetter())
                  {
                     List<KeyValuePair<ElementId, IFCRange>> levelRangeList = ExporterCacheManager.DummyHostCache.Find(familyInstance.Id);
                     foreach (KeyValuePair<ElementId, IFCRange> levelRange in levelRangeList)
                     {
                        rangeSetter.IncreaseRangeIndex();
                        ExportFamilyInstanceAsMappedItem(exporterIFC, familyInstance, exportType, ifcEnumType, productWrapper, levelRange.Key, levelRange.Value, null);
                     }
                  }
               }
            }

            tr.Commit();
         }
      }

      private static IFCAnyHandle CreateFamilyTypeHandle(ExporterIFC exporterIFC, ref FamilyTypeInfo typeInfo, DoorWindowInfo doorWindowInfo,
          IList<IFCAnyHandle> representations3D, IList<Transform> trfRepMapList, IList<IFCAnyHandle> representations2D,
          Element familyInstance, ElementType familySymbol, ElementType originalFamilySymbol, bool useInstanceGeometry, bool exportParts,
          IFCExportInfoPair exportType, out HashSet<IFCAnyHandle> propertySets)
      {
         // for many
         propertySets = new HashSet<IFCAnyHandle>();

         IFCFile file = exporterIFC.GetFile();

         IFCAnyHandle repMap2dHnd = null;
         IFCAnyHandle repMap3dHnd = null;

         IList<IFCAnyHandle> repMapList = new List<IFCAnyHandle>();
         {
            IFCAnyHandle origin = null;
            if (representations3D != null)
            {
               //if (origin == null)
               //   origin = ExporterUtil.CreateAxis2Placement3D(file);
               int num = 0;
               foreach (IFCAnyHandle rep in representations3D)
               {
                  if (trfRepMapList[num] == null)
                     origin = ExporterUtil.CreateAxis2Placement3D(file);
                  else
                     origin = ExporterUtil.CreateAxis2Placement3D(file, trfRepMapList[num].Origin, trfRepMapList[num].BasisZ, trfRepMapList[num].BasisX);   // Used by 'Axis' MappedRepresentation

                  repMap3dHnd = IFCInstanceExporter.CreateRepresentationMap(file, origin, rep);
                  repMapList.Add(repMap3dHnd);
                  num++;
               }
            }

            if (representations2D != null)
            {
               if (origin == null)
                  origin = ExporterUtil.CreateAxis2Placement3D(file);
               foreach (IFCAnyHandle rep in representations2D)
               {
                  repMap2dHnd = IFCInstanceExporter.CreateRepresentationMap(file, origin, rep);
                  repMapList.Add(repMap2dHnd);
               }
            }
         }

         // We won't allow creating a type if we aren't creating an instance.
         // We won't create the instance if: we are exporting to CV2.0/RV, we have no 2D, 3D, or bounding box geometry, and we aren't exporting parts.
         bool willCreateInstance = !(repMapList.Count == 0 
            && !ExporterCacheManager.ExportOptionsCache.ExportBoundingBox && !exportParts
             && (ExporterCacheManager.ExportOptionsCache.ExportAsCoordinationView2
                     || ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView));
         if (!willCreateInstance)
            return null;

         IFCAnyHandle typeStyle = null;

         // for Door, Window
         bool paramTakesPrecedence = false; // For Revit, this is currently always false.
         bool sizeable = false;

         // Only set GUID if we need to override the default.
         string guid = null;
         if (useInstanceGeometry)
         {
            int subElementIndex = ExporterStateManager.GetCurrentRangeIndex();
            if (subElementIndex == 0)
               guid = GUIDUtil.CreateSubElementGUID(familyInstance, (int)IFCFamilyInstanceSubElements.InstanceAsType);
            else if (subElementIndex <= ExporterStateManager.RangeIndexSetter.GetMaxStableGUIDs())
               guid = GUIDUtil.CreateSubElementGUID(familyInstance, (int)IFCGenericSubElements.SplitTypeStart + subElementIndex - 1);
         }

         // Cover special cases not covered above.
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(typeStyle))
         {
            switch (exportType.ExportInstance)
            {
               case IFCEntityType.IfcBeam:
                  {
                     string beamType = exportType.ValidatedPredefinedType;
                     if (string.IsNullOrEmpty(beamType) || beamType.Equals("NOTDEFINED", StringComparison.InvariantCultureIgnoreCase))
                        beamType = "Beam";
                     typeStyle = IFCInstanceExporter.CreateBeamType(file, familySymbol,
                           propertySets, repMapList, beamType);
                     break;
                  }
               case IFCEntityType.IfcColumn:
                  {
                     string columnType = exportType.ValidatedPredefinedType;
                     if (string.IsNullOrEmpty(columnType) || columnType.Equals("NOTDEFINED", StringComparison.InvariantCultureIgnoreCase))
                        columnType = "Column";
                     typeStyle = IFCInstanceExporter.CreateColumnType(file, familySymbol,
                           propertySets, repMapList, columnType);
                     break;
                  }
               case IFCEntityType.IfcMember:
                  {
                     string memberType = exportType.ValidatedPredefinedType;
                     if (string.IsNullOrEmpty(memberType) || memberType.Equals("NOTDEFINED", StringComparison.InvariantCultureIgnoreCase))
                        memberType = "Brace";
                     typeStyle = IFCInstanceExporter.CreateMemberType(file, familySymbol,
                           propertySets, repMapList, memberType);
                     break;
                  }
               case IFCEntityType.IfcDoor:
                  {
                     IFCAnyHandle doorLining = DoorWindowUtil.CreateDoorLiningProperties(exporterIFC, familyInstance);
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(doorLining))
                        propertySets.Add(doorLining);

                     IList<IFCAnyHandle> doorPanels = DoorWindowUtil.CreateDoorPanelProperties(exporterIFC, doorWindowInfo,
                        familyInstance);
                     propertySets.UnionWith(doorPanels);

                     guid = GUIDUtil.CreateSubElementGUID(originalFamilySymbol, (int)IFCDoorSubElements.DoorStyle);

                     if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
                     {
                        typeStyle = IFCInstanceExporter.CreateDoorStyle(file, familySymbol,
                           propertySets, repMapList, doorWindowInfo.DoorOperationTypeString, DoorWindowUtil.GetDoorStyleConstruction(familyInstance),
                           paramTakesPrecedence, sizeable);
                     }
                     else
                     {
                        typeStyle = IFCInstanceExporter.CreateDoorType(file, familySymbol,
                           propertySets, repMapList, doorWindowInfo.PreDefinedType, doorWindowInfo.DoorOperationTypeString,
                           paramTakesPrecedence, doorWindowInfo.UserDefinedOperationType);
                     }
                     break;
                  }
               case IFCEntityType.IfcSpace:
                  {
                     typeStyle = IFCInstanceExporter.CreateSpaceType(file, familySymbol, propertySets, repMapList, exportType.ValidatedPredefinedType);
                     break;
                  }
               case IFCEntityType.IfcSystemFurnitureElement:
                  {
                     typeStyle = IFCInstanceExporter.CreateSystemFurnitureElementType(file, familySymbol, propertySets, repMapList, exportType.ValidatedPredefinedType);

                     break;
                  }
               case IFCEntityType.IfcWindow:
                  {
                     Toolkit.IFCWindowStyleOperation operationType = DoorWindowUtil.GetIFCWindowStyleOperation(originalFamilySymbol);
                     IFCWindowStyleConstruction constructionType = DoorWindowUtil.GetIFCWindowStyleConstruction(familyInstance);

                     IFCAnyHandle windowLining = DoorWindowUtil.CreateWindowLiningProperties(exporterIFC, familyInstance, null);
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(windowLining))
                        propertySets.Add(windowLining);

                     IList<IFCAnyHandle> windowPanels =
                        DoorWindowUtil.CreateWindowPanelProperties(exporterIFC, familyInstance, null);
                     propertySets.UnionWith(windowPanels);

                     guid = GUIDUtil.CreateSubElementGUID(originalFamilySymbol, (int)IFCWindowSubElements.WindowStyle);

                     if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
                     {
                        typeStyle = IFCInstanceExporter.CreateWindowStyle(file, originalFamilySymbol,
                           propertySets, repMapList, constructionType, operationType,
                           paramTakesPrecedence, sizeable);
                     }
                     else
                     {
                        typeStyle = IFCInstanceExporter.CreateWindowType(file, originalFamilySymbol,
                           propertySets, repMapList, doorWindowInfo.PreDefinedType, 
                           DoorWindowUtil.GetIFCWindowPartitioningType(originalFamilySymbol),
                           paramTakesPrecedence, doorWindowInfo.UserDefinedOperationType);
                     }
                     break;
                  }
               case IFCEntityType.IfcBuildingElementProxy:
                  {
                     typeStyle = IFCInstanceExporter.CreateGenericIFCType(exportType, familySymbol, file, propertySets, repMapList);
                     break;
                  }
               case IFCEntityType.IfcFurniture:
                  {
                     typeStyle = IFCInstanceExporter.CreateFurnitureType(file, familySymbol, propertySets, repMapList, null, null, null, exportType.ValidatedPredefinedType);
                     break;
                  }
            }

            if (IFCAnyHandleUtil.IsNullOrHasNoValue(typeStyle))
            {
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(repMap2dHnd))
                  typeInfo.Map2DHandle = repMap2dHnd;
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(repMap3dHnd))
                  typeInfo.Map3DHandle = repMap3dHnd;
            }
         }

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(typeStyle))
         {
            // This covers many generic types.  If we can't find it in the list here, do custom exports.
            typeStyle = FamilyExporterUtil.ExportGenericType(exporterIFC, exportType, exportType.ValidatedPredefinedType,
               propertySets, repMapList, familyInstance, familySymbol);
         }

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(typeStyle))
            return null;

         if (guid != null)
            ExporterUtil.SetGlobalId(typeStyle, guid);

         typeInfo.Style = typeStyle;

         return typeStyle;
      }

      private static bool CanHaveInsulationOrLining(IFCExportInfoPair exportType, ElementId categoryId)
      {
         // This is intended to reduce the number of exceptions thrown in GetLiningIds and GetInsulationIds.
         // There may still be some exceptions thrown as the category list below is still too large for GetLiningIds.
         if (exportType.ExportType != IFCEntityType.IfcDuctFittingType && exportType.ExportType != IFCEntityType.IfcPipeFittingType &&
            exportType.ExportType != IFCEntityType.IfcDuctSegmentType && exportType.ExportType != IFCEntityType.IfcPipeSegmentType)
            return false;

         int catIdAsInt = categoryId.IntegerValue;
         if ((catIdAsInt == (int)BuiltInCategory.OST_DuctAccessory) ||
            (catIdAsInt == (int)BuiltInCategory.OST_DuctCurves) ||
            (catIdAsInt == (int)BuiltInCategory.OST_DuctFitting) ||
            (catIdAsInt == (int)BuiltInCategory.OST_FlexDuctCurves) ||
            (catIdAsInt == (int)BuiltInCategory.OST_FlexPipeCurves) ||
            (catIdAsInt == (int)BuiltInCategory.OST_PipeAccessory) ||
            (catIdAsInt == (int)BuiltInCategory.OST_PipeCurves) ||
            (catIdAsInt == (int)BuiltInCategory.OST_PipeFitting))
            return true;

         return false;
      }

      /// <summary>
      /// Exports a family instance as a mapped item.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="familyInstance">The family instance to be exported.</param>
      /// <param name="exportType">The export type.</param>
      /// <param name="ifcEnumType">The string value represents the IFC type.</param>
      /// <param name="wrapper">The ProductWrapper.</param>
      /// <param name="overrideLevelId">The level id.</param>
      /// <param name="range">The range of this family instance to be exported.</param>
      public static void ExportFamilyInstanceAsMappedItem(ExporterIFC exporterIFC, FamilyInstance familyInstance, IFCExportInfoPair exportType,
          string ifcEnumType, ProductWrapper wrapper, ElementId overrideLevelId, IFCRange range, IFCAnyHandle parentLocalPlacement)
      {
         bool exportParts = PartExporter.CanExportParts(familyInstance);
         bool isSplit = range != null;
         if (exportParts && !PartExporter.CanExportElementInPartExport(familyInstance, isSplit ? overrideLevelId : familyInstance.LevelId, isSplit))
            return;

         Document doc = familyInstance.Document;
         IFCFile file = exporterIFC.GetFile();

         // The "originalFamilySymbol" has the right geometry, but should be used as little as possible.
         FamilySymbol originalFamilySymbol = ExporterIFCUtils.GetOriginalSymbol(familyInstance);
         FamilySymbol familySymbol = familyInstance.Symbol;
         if (originalFamilySymbol == null || familySymbol == null)
            return;

         ProductWrapper familyProductWrapper = ProductWrapper.Create(wrapper);
         Options options = GeometryUtil.GetIFCExportGeometryOptions();

         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

         HostObject hostElement = familyInstance.Host as HostObject; //hostElement could be null
         ElementId categoryId = CategoryUtil.GetSafeCategoryId(familySymbol);

         string familyName = familySymbol.Name;

         // A Family Instance can have its own copy of geometry, or use the symbol's copy with a transform.
         // The routine below tells us whether to use the Instance's copy or the Symbol's copy.
         bool useInstanceGeometry = GeometryUtil.UsesInstanceGeometry(familyInstance);
         Transform trf = familyInstance.GetTransform();

         MaterialAndProfile materialAndProfile = null;
         IFCAnyHandle materialProfileSet = null;
         IFCAnyHandle materialLayerSet = null;
         IFCExtrusionCreationData extrusionData = null;

         IList<Transform> repMapTrfList = new List<Transform>();

         XYZ orig = XYZ.Zero;
         XYZ extrudeDirection = null;
         
         using (IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData())
         {
            // Extra information if we are exporting a door or a window.
            DoorWindowInfo doorWindowInfo = null;
            if (exportType.ExportType == IFCEntityType.IfcDoorType || exportType.ExportInstance == IFCEntityType.IfcDoor)
               doorWindowInfo = DoorWindowExporter.CreateDoor(exporterIFC, familyInstance, hostElement, overrideLevelId, trf, exportType);
            else if (exportType.ExportType == IFCEntityType.IfcWindowType || exportType.ExportInstance == IFCEntityType.IfcWindow)
               doorWindowInfo = DoorWindowExporter.CreateWindow(exporterIFC, familyInstance, hostElement, overrideLevelId, trf, exportType);

            FamilyTypeInfo typeInfo = new FamilyTypeInfo();

            bool flipped = doorWindowInfo != null ? doorWindowInfo.FlippedSymbol : false;
            FamilyTypeInfo currentTypeInfo = ExporterCacheManager.FamilySymbolToTypeInfoCache.Find(originalFamilySymbol.Id, flipped, exportType);
            bool found = currentTypeInfo.IsValid();

            Family family = familySymbol.Family;

            IList<GeometryObject> geomObjects = new List<GeometryObject>();
            Transform offsetTransform = null;

            Transform doorWindowTrf = Transform.Identity;

            // We will create a new mapped type if:
            // 1.  We are exporting part of a column or in-place wall (range != null), OR
            // 2.  We are using the instance's copy of the geometry (that it, it has unique geometry), OR
            // 3.  We haven't already created the type.
            bool creatingType = ((range != null) || useInstanceGeometry || !found);
            if (creatingType)
            {
               IList<IFCAnyHandle> representations3D = new List<IFCAnyHandle>();
               IList<IFCAnyHandle> representations2D = new List<IFCAnyHandle>();

               IFCAnyHandle dummyPlacement = null;
               if (doorWindowInfo != null)
               {
                  doorWindowTrf = ExporterIFCUtils.GetTransformForDoorOrWindow(familyInstance, originalFamilySymbol,
                      doorWindowInfo.FlippedX, doorWindowInfo.FlippedY);
               }
               else
               {
                  dummyPlacement = ExporterUtil.CreateLocalPlacement(file, null, null);
                  extraParams.SetLocalPlacement(dummyPlacement);
               }

               Element exportGeometryElement = useInstanceGeometry ? (Element)familyInstance : (Element)originalFamilySymbol;
               GeometryElement exportGeometry = exportGeometryElement.get_Geometry(options);

               // There are 2 possible paths for a Family Instance to be exported as a Swept Solid.
               // 1. Below here through ExtrusionExporter.CreateExtrusionWithClipping
               // 2. Through BodyExporter.ExportBody
               if (!exportParts)
               {
                  using (TransformSetter trfSetter = TransformSetter.Create())
                  {
                     if (doorWindowInfo != null)
                     {
                        trfSetter.Initialize(exporterIFC, doorWindowTrf);
                     }

                     if (exportGeometry == null)
                        return;

                     SolidMeshGeometryInfo solidMeshCapsule = null;

                     if (range == null)
                     {
                        solidMeshCapsule = GeometryUtil.GetSplitSolidMeshGeometry(exportGeometry);
                     }
                     else
                     {
                        solidMeshCapsule = GeometryUtil.GetSplitClippedSolidMeshGeometry(exportGeometry, range);
                     }

                     IList<Solid> solids = solidMeshCapsule.GetSolids();
                     IList<Mesh> polyMeshes = solidMeshCapsule.GetMeshes();

                     // If we are exporting parts, it is OK to have no geometry here - it will be added by the host Part.
                     bool hasSolidsOrMeshesInSymbol = (solids.Count > 0 || polyMeshes.Count > 0);

                     if (range != null && !hasSolidsOrMeshesInSymbol)
                        return; // no proper split geometry to export.

                     if (hasSolidsOrMeshesInSymbol)
                     {
                        geomObjects = FamilyExporterUtil.RemoveInvisibleSolidsAndMeshes(doc, exporterIFC, ref solids, ref polyMeshes);
                        if ((geomObjects.Count == 0))
                           return; // no proper visible split geometry to export.
                     }
                     else
                        geomObjects.Add(exportGeometry);

                     bool tryToExportAsExtrusion = (!ExporterCacheManager.ExportOptionsCache.ExportAs2x2
                                                     || IsExtrusionFriendlyType(exportType.ExportInstance));

                     if (IsExtrusionFriendlyType(exportType.ExportInstance))
                     {
                        // Get a profile name. 
                        string profileName = NamingUtil.GetProfileName(familySymbol);

                        StructuralMemberAxisInfo axisInfo = StructuralMemberExporter.GetStructuralMemberAxisTransform(familyInstance);
                        if (axisInfo != null)
                        {
                           orig = axisInfo.LCSAsTransform.Origin;
                           extrudeDirection = axisInfo.AxisDirection;

                           extraParams.CustomAxis = extrudeDirection;
                           extraParams.PossibleExtrusionAxes = IFCExtrusionAxes.TryXY;
                        }
                        else
                        {
                           extraParams.PossibleExtrusionAxes = IFCExtrusionAxes.TryZ;
                           LocationPoint point = familyInstance.Location as LocationPoint;

                           if (point != null)
                              orig = point.Point;

                           // TODO: Is this a useful default?  Should it be based
                           // on the instance transform in some way?
                           extrudeDirection = XYZ.BasisZ;
                        }

                        if (solids.Count > 0)
                        {
                           bool completelyClipped = false;
                           IList<ElementId> materialIds = null;
                           FootPrintInfo footprintInfo = null;
                           // The "extrudeDirection" passed in is in global coordinates if it represents
                           // a custom axis, while the geometry is in either the FamilyInstance or 
                           // FamilySymbol coordinate system, depending on the useInstanceGeometry
                           // flag.  If we aren't using instance geometry, convert the extrusion direction
                           // and base plane to be in the symbol/geometry space.
                           XYZ extrusionDirectionToUse = (useInstanceGeometry || !extraParams.HasCustomAxis) ? 
                              extrudeDirection : trf.Inverse.OfVector(extrudeDirection);
                           Plane basePlaneToUse = GeometryUtil.CreatePlaneByNormalAtOrigin(extrusionDirectionToUse);

                           GenerateAdditionalInfo addInfo = GenerateAdditionalInfo.GenerateBody | GenerateAdditionalInfo.GenerateProfileDef;
                           IFCAnyHandle bodyRepresentation = ExtrusionExporter.CreateExtrusionWithClipping(exporterIFC, exportGeometryElement,
                               categoryId, solids, basePlaneToUse, orig, extrusionDirectionToUse, null, out completelyClipped, out materialIds,
                               out footprintInfo, out materialAndProfile, out extrusionData, addInfo, profileName: profileName);
                           if (extrusionData != null)
                           {
                              extraParams.Slope = extrusionData.Slope;
                              extraParams.ScaledLength = extrusionData.ScaledLength;
                              extraParams.ExtrusionDirection = extrusionData.ExtrusionDirection;
                              extraParams.ScaledHeight = extrusionData.ScaledHeight;
                              extraParams.ScaledWidth = extrusionData.ScaledWidth;

                              extraParams.ScaledArea = extrusionData.ScaledArea;
                              extraParams.ScaledInnerPerimeter = extrusionData.ScaledInnerPerimeter;
                              extraParams.ScaledOuterPerimeter = extrusionData.ScaledOuterPerimeter;
                           }

                           typeInfo.MaterialIdList = materialIds;
                           if (!IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRepresentation))
                           {
                              representations3D.Add(bodyRepresentation);
                              repMapTrfList.Add(null);
                              if (materialAndProfile != null)
                                 typeInfo.MaterialAndProfile = materialAndProfile;   // Keep material and profile information in the type info for later creation

                              if (IsExtrusionFriendlyType(exportType.ExportInstance))
                              {
                                 if (axisInfo != null)
                                 {
                                    Transform newLCS = Transform.Identity;
                                    Transform offset = Transform.Identity;
                                    if (materialAndProfile != null)
                                    {
                                       if (materialAndProfile.LCSTransformUsed != null)
                                       {
                                          // If the Solid creation uses a different LCS, we will use the same LCS for the Axis and transform the Axis to this new LCS
                                          newLCS = new Transform(materialAndProfile.LCSTransformUsed);
                                          // The Axis will be offset later to compensate the shift
                                          offset = newLCS;
                                       }
                                    }

                                    if (!useInstanceGeometry)
                                    {
                                       // If the extrusion is created from the FamilySymbol, the new LCS will be the FamilyIntance transform
                                       newLCS = trf;
                                       offset = Transform.Identity;
                                    }

                                    ElementId catId = CategoryUtil.GetSafeCategoryId(familyInstance);
                                    IFCAnyHandle axisRep = StructuralMemberExporter.CreateStructuralMemberAxis(exporterIFC, familyInstance, catId, axisInfo, newLCS);
                                    if (!IFCAnyHandleUtil.IsNullOrHasNoValue(axisRep))
                                    {
                                       representations3D.Add(axisRep);
                                       // This offset is going to be applied later. Need to scale the coordinate into the correct unit scale
                                       offset.Origin = UnitUtil.ScaleLength(offset.Origin);
                                       repMapTrfList.Add(offset);
                                    }
                                 }
                              }
                           }
                        }
                     }
                     else
                     {
                        extraParams.PossibleExtrusionAxes = IFCExtrusionAxes.TryXYZ;
                     }

                     BodyData bodyData = null;
                     if (representations3D.Count == 0)
                     {
                        string profileName = null;
                        BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(tryToExportAsExtrusion, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                        if (IsExtrusionFriendlyType(exportType.ExportInstance))
                        {
                           if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
                              bodyExporterOptions.CollectMaterialAndProfile = false;
                           else
                           bodyExporterOptions.CollectMaterialAndProfile = true;
                           // Get a profile name. 
                           profileName = NamingUtil.GetProfileName(familySymbol);
                        }

                        if (exportType.ExportInstance == IFCEntityType.IfcSlab || exportType.ExportInstance == IFCEntityType.IfcPlate)
                           bodyExporterOptions.CollectFootprintHandle = ExporterCacheManager.ExportOptionsCache.ExportAs4;

                        GeometryObject potentialPathGeom = GetPotentialCurveOrPolyline(exportGeometryElement, options);
                        bodyData = BodyExporter.ExportBody(exporterIFC, familyInstance, categoryId, ElementId.InvalidElementId,
                            geomObjects, bodyExporterOptions, extraParams, potentialPathGeom, profileName: profileName);
                        typeInfo.MaterialIdList = bodyData.MaterialIds;
                        //if (!bodyData.OffsetTransform.IsIdentity)
                        offsetTransform = bodyData.OffsetTransform;

                        IFCAnyHandle bodyRepHnd = bodyData.RepresentationHnd;
                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRepHnd))
                        {
                           representations3D.Add(bodyRepHnd);
                           repMapTrfList.Add(null);
                        }

                        if (IsExtrusionFriendlyType(exportType.ExportInstance))
                        {
                           StructuralMemberAxisInfo axisInfo = StructuralMemberExporter.GetStructuralMemberAxisTransform(familyInstance);
                           if (axisInfo != null)
                           {
                              Transform newLCS = Transform.Identity;
                              Transform offset = Transform.Identity;
                              if (!useInstanceGeometry)
                              {
                                 // When it is the case of NOT using instance geometry (i.e. using the original family symbol), use the transform of the familyInstance as the new LCS
                                 // This transform will be set as the Object LCS later on
                                 newLCS = new Transform(trf);
                              }
                              else
                              {
                                 IFCAnyHandle lcsHnd = extraParams.GetLocalPlacement();
                                 if (bodyData.ShapeRepresentationType == ShapeRepresentationType.Tessellation)
                                 {
                                    // For Tessellation, it appears that the local placement is already scaled. Unscale it here because axisInfo is based on unscaled information
                                    newLCS = ExporterUtil.UnscaleTransformOrigin(ExporterUtil.GetTransformFromLocalPlacementHnd(lcsHnd));
                                 }
                                 else
                                 {
                                    // This is when a structural member is identified to use its instance geometry and it has an extrusion geometry, but it failed the first attempt to create extrusion. 
                                    // The new LCS for the structural member needs to be set to the base object LCS consistent with the body created by BodyExporter.ExportBody()
                                    newLCS = ExporterUtil.GetTransformFromLocalPlacementHnd(lcsHnd);
                                 }
                              }

                              ElementId catId = CategoryUtil.GetSafeCategoryId(familyInstance);
                              IFCAnyHandle axisRep = StructuralMemberExporter.CreateStructuralMemberAxis(exporterIFC, familyInstance, catId, axisInfo, newLCS);
                              if (!IFCAnyHandleUtil.IsNullOrHasNoValue(axisRep))
                              {
                                 representations3D.Add(axisRep);
                                 repMapTrfList.Add(null);
                              }
                           }
                        }

                        if (bodyData.FootprintInfo != null)
                        {
                           IFCAnyHandle footprintShapeRep = bodyData.FootprintInfo.CreateFootprintShapeRepresentation(exporterIFC);
                           representations3D.Add(footprintShapeRep);
                           repMapTrfList.Add(bodyData.FootprintInfo.ExtrusionBaseLCS);
                        }

                        // Keep Material and Profile informartion in the typeinfo for creation of MaterialSet later on
                        if (bodyExporterOptions.CollectMaterialAndProfile && bodyData.MaterialAndProfile != null)
                           typeInfo.MaterialAndProfile = bodyData.MaterialAndProfile;
                     }

                     // We will allow a door or window to be exported without any geometry, or an element with parts.
                     // Anything else doesn't really make sense.
                     if (representations3D.Count == 0 && (doorWindowInfo == null))
                     {
                        extraParams.ClearOpenings();
                        return;
                     }
                  }

                  // By default: if exporting IFC2x3 or later, export 2D plan rep of family, if it exists, unless we are exporting Coordination View V2.
                  // This default can be overridden in the export options.
                  bool needToCreate2d = ExporterCacheManager.ExportOptionsCache.ExportAnnotations;
                  if (needToCreate2d)
                  {
                     XYZ curveOffset = new XYZ(0, 0, 0);
                     if (offsetTransform != null)
                        curveOffset = -UnitUtil.UnscaleLength(offsetTransform.Origin);

                     HashSet<IFCAnyHandle> curveSet = new HashSet<IFCAnyHandle>();
                     {
                        Transform planeTrf = doorWindowTrf.Inverse;
                        XYZ projDir = XYZ.BasisZ;

                        if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
                        {
                           // TODO: Check that this isn't overkill here.
                           IList<Curve> export2DGeometry = GeometryUtil.Get2DArcOrLineFromSymbol(familyInstance, allCurveType: true);

                           foreach (Curve curveGeom in export2DGeometry)
                           {
                              Curve curve = curveGeom;

                              if (doorWindowTrf != null)
                              {
                                 Transform flipTrf = Transform.Identity;
                                 double yTrf = 0.0;

                                 if (familyInstance.FacingFlipped ^ familyInstance.HandFlipped)
                                 {
                                    flipTrf.BasisY = flipTrf.BasisY.Negate();
                                 }

                                 // We will move the curve into Z=0
                                 if (curve is Arc)
                                    flipTrf.Origin = new XYZ(0, yTrf, -(curve as Arc).Center.Z);
                                 else if (curve is Ellipse)
                                    flipTrf.Origin = new XYZ(0, yTrf, -(curve as Ellipse).Center.Z);
                                 else
                                 {
                                    if (curve.IsBound)
                                       flipTrf.Origin = new XYZ(0, yTrf, -curve.GetEndPoint(0).Z);
                                 }

                                 curve = curve.CreateTransformed(doorWindowTrf.Multiply(flipTrf));
                              }

                              IFCAnyHandle curveHnd = GeometryUtil.CreatePolyCurveFromCurve(exporterIFC, curve);
                              if (curveSet == null)
                                 curveSet = new HashSet<IFCAnyHandle>();
                              if (!IFCAnyHandleUtil.IsNullOrHasNoValue(curveHnd))
                                 curveSet.Add(curveHnd);
                           }
                        }
                        else
                        {
                           IFCGeometryInfo IFCGeometryInfo = IFCGeometryInfo.CreateCurveGeometryInfo(exporterIFC, planeTrf, projDir, true);
                           ExporterIFCUtils.CollectGeometryInfo(exporterIFC, IFCGeometryInfo, exportGeometry, curveOffset, false);

                           IList<IFCAnyHandle> curves = IFCGeometryInfo.GetCurves();
                           foreach (IFCAnyHandle curve in curves)
                              curveSet.Add(curve);
                        }

                        if (curveSet.Count > 0)
                        {
                           IFCAnyHandle contextOfItems2d = exporterIFC.Get2DContextHandle();
                           IFCAnyHandle curveRepresentationItem = IFCInstanceExporter.CreateGeometricSet(file, curveSet);
                           HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
                           bodyItems.Add(curveRepresentationItem);
                           IFCAnyHandle planRepresentation = RepresentationUtil.CreateGeometricSetRep(exporterIFC, familyInstance, categoryId, "FootPrint",
                              contextOfItems2d, bodyItems);
                           if (!IFCAnyHandleUtil.IsNullOrHasNoValue(planRepresentation))
                              representations2D.Add(planRepresentation);
                        }
                     }
                  }
               }

               if (doorWindowInfo != null)
                  typeInfo.StyleTransform = doorWindowTrf.Inverse;
               else
                  typeInfo.StyleTransform = ExporterIFCUtils.GetUnscaledTransform(exporterIFC, extraParams.GetLocalPlacement());

               if (typeInfo.MaterialAndProfile != null)
               {
                  typeInfo.ScaledArea = typeInfo.MaterialAndProfile.CrossSectionArea.HasValue ? typeInfo.MaterialAndProfile.CrossSectionArea.Value : 0.0;
                  typeInfo.ScaledDepth = typeInfo.MaterialAndProfile.ExtrusionDepth.HasValue ? typeInfo.MaterialAndProfile.ExtrusionDepth.Value : 0.0;
                  typeInfo.ScaledInnerPerimeter = typeInfo.MaterialAndProfile.InnerPerimeter.HasValue ? typeInfo.MaterialAndProfile.InnerPerimeter.Value : 0.0;
                  typeInfo.ScaledOuterPerimeter = typeInfo.MaterialAndProfile.OuterPerimeter.HasValue ? typeInfo.MaterialAndProfile.OuterPerimeter.Value : 0.0;
               }

               HashSet<IFCAnyHandle> propertySets = null;
               IFCAnyHandle typeStyle = CreateFamilyTypeHandle(exporterIFC, ref typeInfo, doorWindowInfo, representations3D, repMapTrfList, representations2D,
                   familyInstance, familySymbol, originalFamilySymbol, useInstanceGeometry, exportParts,
                   exportType, out propertySets);

               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(typeStyle))
               {
                  wrapper.RegisterHandleWithElementType(familySymbol as ElementType, exportType, typeStyle, propertySets);

                  typeInfo.Style = typeStyle;

                  bool addedMaterialAssociation = false;
                  if (IsExtrusionFriendlyType(exportType.ExportInstance)
                     && !ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4
                     && !ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
                  {
                     if (typeInfo.MaterialAndProfile != null)
                     {
                        materialProfileSet = CategoryUtil.GetOrCreateMaterialSet(exporterIFC, familySymbol, typeInfo.MaterialAndProfile);
                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(materialProfileSet))
                        {
                           CategoryUtil.CreateMaterialAssociation(exporterIFC, familySymbol, typeStyle, typeInfo.MaterialAndProfile);
                           addedMaterialAssociation = true;
                        }
                     }
                     else if (extrudeDirection != null && orig != null)
                     {
                        if (!ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
                        {
                           // If Material Profile information is somehow missing (e.g. the geometry is exported as Tessellation or BRep. In IFC4 where geometry is restricted
                           //   the materialprofile information may still be needed), it will try to get the information here:
                           MaterialAndProfile matNProf = GeometryUtil.GetProfileAndMaterial(exporterIFC, familyInstance, extrudeDirection, orig);
                           if (matNProf.GetKeyValuePairs().Count > 0)
                           {
                              materialProfileSet = CategoryUtil.GetOrCreateMaterialSet(exporterIFC, familySymbol, matNProf);
                              if (!IFCAnyHandleUtil.IsNullOrHasNoValue(materialProfileSet))
                              {
                                 CategoryUtil.CreateMaterialAssociation(exporterIFC, familySymbol, typeStyle, matNProf);
                                 addedMaterialAssociation = true;
                              }
                           }
                        }
                     }
                  }
                  else if (exportType.ExportInstance == IFCEntityType.IfcPlate || exportType.ExportInstance == IFCEntityType.IfcSlab || exportType.ExportInstance == IFCEntityType.IfcWall)
                  {
                     //List<ElementId> matIds;
                     //IFCAnyHandle primaryMaterialHnd;
                     //                     materialLayerSet = ExporterUtil.CollectMaterialLayerSet(exporterIFC, familyInstance, wrapper, out matIds, out primaryMaterialHnd);
                     MaterialLayerSetInfo mlsInfo = new MaterialLayerSetInfo(exporterIFC, familyInstance, wrapper);
                     materialLayerSet = mlsInfo.MaterialLayerSetHandle;
                     if (materialLayerSet != null)
                     {
                        CategoryUtil.CreateMaterialAssociation(exporterIFC, typeStyle, materialLayerSet);
                        addedMaterialAssociation = true;
                     }
                  }
                  else
                  {
                     Element elementType = familyInstance.Document.GetElement(familyInstance.GetTypeId());
                     ElementId bestMatId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(exportGeometry, exporterIFC, elementType);
                     if (bestMatId == ElementId.InvalidElementId)
                        bestMatId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(exportGeometry, exporterIFC, familyInstance);

                     // Also get the materials from Parameters
                     IList<ElementId> matIds = ParameterUtil.FindMaterialParameters(elementType);
                     if (matIds.Count == 0)
                        matIds = ParameterUtil.FindMaterialParameters(familyInstance);

                     // Combine the material ids
                     if (bestMatId != ElementId.InvalidElementId && !matIds.Contains(bestMatId))
                        matIds.Add(bestMatId);

                     if (matIds.Count > 0)
                     {
                        CategoryUtil.CreateMaterialAssociation(exporterIFC, typeStyle, matIds);
                        addedMaterialAssociation = true;
                     }
                  }

                  if (!addedMaterialAssociation)
                     CategoryUtil.CreateMaterialAssociation(exporterIFC, typeStyle, typeInfo.MaterialIdList);

                  ClassificationUtil.CreateClassification(exporterIFC, file, familySymbol, typeStyle);        // Create other generic classification from ClassificationCode(s)
                  ClassificationUtil.CreateUniformatClassification(exporterIFC, file, originalFamilySymbol, typeStyle);
               }
            }

            if (found && !typeInfo.IsValid())
               typeInfo = currentTypeInfo;

            // we'll pretend we succeeded, but we'll do nothing.
            if (!typeInfo.IsValid())
               return;

            if (!ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
            {
            // If the type is obtained from the cache (not the first instance), materialProfileSet will be null and needs to be obtained from the cache
               if (IsExtrusionFriendlyType(exportType.ExportInstance)
                 && materialProfileSet == null)
               {
                  materialProfileSet = ExporterCacheManager.MaterialSetCache.FindProfileSet(familySymbol.Id);
                  if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 && !ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView 
                     && materialProfileSet == null && extrudeDirection != null && orig != null)
                  {
                     // If Material Profile information is somehow missing (e.g. the geometry is exported as Tessellation or BRep. In IFC4 where geometry is restricted
                     //   the materialprofile information may still be needed), it will try to get the information here:
                     MaterialAndProfile matNProf = GeometryUtil.GetProfileAndMaterial(exporterIFC, familyInstance, extrudeDirection, orig);
                     if (matNProf.GetKeyValuePairs().Count > 0)
                     {
                        materialProfileSet = CategoryUtil.GetOrCreateMaterialSet(exporterIFC, familySymbol, matNProf);
                        CategoryUtil.CreateMaterialAssociation(exporterIFC, familySymbol, typeInfo.Style, matNProf);
                     }
                  }
               }
            }

            if ((exportType.ExportInstance == IFCEntityType.IfcSlab || exportType.ExportInstance == IFCEntityType.IfcPlate || exportType.ExportInstance == IFCEntityType.IfcWall)
                && materialLayerSet == null)
            {
               materialLayerSet = ExporterCacheManager.MaterialSetCache.FindLayerSet(familySymbol.Id);
            }

            // add to the map, as long as we are not using range, not using instance geometry, and don't have extra openings.
            if ((range == null) && !useInstanceGeometry && (extraParams.GetOpenings().Count == 0))
               ExporterCacheManager.FamilySymbolToTypeInfoCache.Register(originalFamilySymbol.Id, flipped, exportType, typeInfo);

            // If we are using the instance geometry, ignore the transformation.
            if (useInstanceGeometry)
               trf = Transform.Identity;

            if ((range != null) && exportParts)
            {
               XYZ rangeOffset = trf.Origin;
               rangeOffset += new XYZ(0, 0, range.Start);
               trf.Origin = rangeOffset;
            }

            Transform originalTrf = new Transform(trf);
            XYZ scaledMapOrigin = XYZ.Zero;

            trf = trf.Multiply(typeInfo.StyleTransform);

            // create instance.  
            IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
            {
               IFCAnyHandle contextOfItems2d = exporterIFC.Get2DContextHandle();
               IFCAnyHandle contextOfItems3d = exporterIFC.Get3DContextHandle("Body");
               IFCAnyHandle contextOfItems1d = exporterIFC.Get3DContextHandle("Axis");

               // for proxies, we store the IfcRepresentationMap directly since there is no style.
               IFCAnyHandle style = typeInfo.Style;
               IList<IFCAnyHandle> repMapList = !IFCAnyHandleUtil.IsNullOrHasNoValue(style) ?
                   GeometryUtil.GetRepresentationMaps(style) : null;
               if (repMapList == null)
               {
                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(typeInfo.Map3DHandle))
                  {
                     repMapList = new List<IFCAnyHandle>();
                     repMapList.Add(typeInfo.Map3DHandle);
                  }

                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(typeInfo.Map2DHandle))
                  {
                     if (repMapList == null)
                        repMapList = new List<IFCAnyHandle>();
                     repMapList.Add(typeInfo.Map2DHandle);
                  }
               }

               int numReps = repMapList != null ? repMapList.Count : 0;

               // Note that repMapList may be null here, so we use numReps instead.
               for (int ii = 0; ii < numReps; ii++)
               {
                  IFCAnyHandle repMap = repMapList[ii];
                  int dimRepMap = RepresentationUtil.DimOfRepresentationContext(repMap);
                  if (dimRepMap < 1 || dimRepMap > 3)
                     return;

                  HashSet<IFCAnyHandle> representations = new HashSet<IFCAnyHandle>();
                  representations.Add(ExporterUtil.CreateDefaultMappedItem(file, repMap, scaledMapOrigin));
                  IFCAnyHandle shapeRep = null;
                  switch (dimRepMap)
                  {
                     case 3:
                        {
                           shapeRep = RepresentationUtil.CreateBodyMappedItemRep(exporterIFC, familyInstance, categoryId, contextOfItems3d,
                               representations);
                           break;
                        }
                     case 2:
                        {
                           shapeRep = RepresentationUtil.CreatePlanMappedItemRep(exporterIFC, familyInstance, categoryId, contextOfItems2d,
                         representations);
                           break;
                        }
                     case 1:
                        {
                           shapeRep = RepresentationUtil.CreateGraphMappedItemRep(exporterIFC, familyInstance, categoryId, contextOfItems1d,
                         representations);
                           break;
                        }
                  }

                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(shapeRep))
                     return;
                  shapeReps.Add(shapeRep);
               }
            }

            IFCAnyHandle boundingBoxRep = null;
            Transform boundingBoxTrf = (offsetTransform != null) ? offsetTransform.Inverse : Transform.Identity;
            if (geomObjects.Count > 0)
            {
               boundingBoxTrf = boundingBoxTrf.Multiply(doorWindowTrf);
               boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geomObjects, boundingBoxTrf);
            }
            else
            {
               boundingBoxTrf = boundingBoxTrf.Multiply(trf.Inverse);
               boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, familyInstance.get_Geometry(options), boundingBoxTrf);
            }

            if (boundingBoxRep != null)
               shapeReps.Add(boundingBoxRep);

            IFCAnyHandle repHnd = (shapeReps.Count > 0) ? IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapeReps) : null;

            // Check for containment override
            IFCAnyHandle overrideContainerHnd = null;
            ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, familyInstance, out overrideContainerHnd);
            if ((overrideLevelId == null || overrideLevelId == ElementId.InvalidElementId) && overrideContainerId != ElementId.InvalidElementId)
               overrideLevelId = overrideContainerId;

            using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, familyInstance, trf, null, overrideLevelId, overrideContainerHnd))
            {
               IFCAnyHandle instanceHandle = null;
               IFCAnyHandle localPlacement = setter.LocalPlacement;
               bool materialAlreadyAssociated = false;

               // We won't create the instance if: 
               // (1) we are exporting to CV2.0/RV, (2) we have no 2D, 3D, or bounding box geometry, and (3) we aren't exporting parts.
               if (!(repHnd == null && !exportParts
                     && (ExporterCacheManager.ExportOptionsCache.ExportAsCoordinationView2
                     || ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)))
               {
                  string instanceGUID = null;

                  int subElementIndex = ExporterStateManager.GetCurrentRangeIndex();
                  if (subElementIndex == 0)
                     instanceGUID = GUIDUtil.CreateGUID(familyInstance);
                  else if (subElementIndex <= ExporterStateManager.RangeIndexSetter.GetMaxStableGUIDs())
                     instanceGUID = GUIDUtil.CreateSubElementGUID(familyInstance, subElementIndex + (int)IFCGenericSubElements.SplitInstanceStart - 1);
                  else
                     instanceGUID = GUIDUtil.CreateGUID();

                  IFCAnyHandle overrideLocalPlacement = null;
                  bool isChildInContainer = familyInstance.AssemblyInstanceId != ElementId.InvalidElementId;

                  if (parentLocalPlacement != null)
                  {
                     Transform relTrf = ExporterIFCUtils.GetRelativeLocalPlacementOffsetTransform(parentLocalPlacement, localPlacement);
                     Transform inverseTrf = relTrf.Inverse;

                     IFCAnyHandle plateLocalPlacement = ExporterUtil.CreateLocalPlacement(file, parentLocalPlacement,
                         inverseTrf.Origin, inverseTrf.BasisZ, inverseTrf.BasisX);
                     overrideLocalPlacement = plateLocalPlacement;
                  }

                  switch (exportType.ExportInstance)
                  {
                     case IFCEntityType.IfcBeam:
                        {
                           if (exportType.HasUndefinedPredefinedType())
                              exportType.ValidatedPredefinedType = "BEAM";
                           instanceHandle = FamilyExporterUtil.ExportGenericInstance(exportType, exporterIFC, familyInstance,
                              wrapper, setter, extraParams, instanceGUID, ownerHistory, exportParts ? null : repHnd, ifcEnumType, overrideLocalPlacement);
                           IFCAnyHandle placementToUse = localPlacement;

                           // NOTE: We do not expect openings here, as they are created as part of creating an extrusion in ExportBody above.
                           // However, if this were the case, we would have exported this beam in ExportBeamAsStandardElement above.

                           OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, offsetTransform,
                               exporterIFC, placementToUse, setter, wrapper);
                           wrapper.AddElement(familyInstance, instanceHandle, setter, extraParams, true, exportType);

                           // Register the beam's IFC handle for later use by truss and beam system export.
                           ExporterCacheManager.ElementToHandleCache.Register(familyInstance.Id, instanceHandle, exportType);

                           if (!IFCAnyHandleUtil.IsNullOrHasNoValue(materialProfileSet) && RepresentationUtil.RepresentationForStandardCaseFromProduct(exportType.ExportInstance, instanceHandle))
                           {
                              int? cardinalPoint = BeamCardinalPoint(familyInstance);

                              if (materialProfileSet != null)
                              {
                                 // RV does not support IfcMaterialProfileSetUsage, material assignment should be directly to the MaterialProfileSet
                                 if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
                                 {
                                    CategoryUtil.CreateMaterialAssociation(exporterIFC, instanceHandle, materialProfileSet);
                                 }
                                 else
                                 {
                                    IFCAnyHandle matSetUsage = IFCInstanceExporter.CreateMaterialProfileSetUsage(file, materialProfileSet, cardinalPoint, null);
                                    CategoryUtil.CreateMaterialAssociation(exporterIFC, instanceHandle, matSetUsage);
                                 }

                                 materialAlreadyAssociated = true;
                              }
                           }
                           break;
                        }
                     case IFCEntityType.IfcColumn:
                        {
                           if (exportType.HasUndefinedPredefinedType())
                              exportType.ValidatedPredefinedType = "COLUMN";
                           instanceHandle = FamilyExporterUtil.ExportGenericInstance(exportType, exporterIFC, familyInstance,
                              wrapper, setter, extraParams, instanceGUID, ownerHistory, exportParts ? null : repHnd, ifcEnumType, overrideLocalPlacement);

                           IFCAnyHandle placementToUse = localPlacement;
                           if (!useInstanceGeometry)
                           {
                              bool needToCreateOpenings = OpeningUtil.NeedToCreateOpenings(instanceHandle, extraParams);
                              if (needToCreateOpenings)
                              {
                                 Transform openingTrf = new Transform(originalTrf);
                                 Transform extraRot = new Transform(originalTrf);
                                 extraRot.Origin = XYZ.Zero;
                                 openingTrf = openingTrf.Multiply(extraRot);
                                 openingTrf = openingTrf.Multiply(typeInfo.StyleTransform);

                                 XYZ scaledOrigin = UnitUtil.ScaleLength(openingTrf.Origin);
                                 IFCAnyHandle openingRelativePlacement = ExporterUtil.CreateAxis2Placement3D(file, scaledOrigin,
                                    openingTrf.get_Basis(2), openingTrf.get_Basis(0));
                                 IFCAnyHandle openingPlacement = ExporterUtil.CopyLocalPlacement(file, localPlacement);
                                 GeometryUtil.SetRelativePlacement(openingPlacement, openingRelativePlacement);
                                 placementToUse = openingPlacement;
                              }
                           }

                           OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, offsetTransform,
                               exporterIFC, placementToUse, setter, wrapper);
                           wrapper.AddElement(familyInstance, instanceHandle, setter, extraParams, true, exportType);

                           // Not all columns are space bounding, but it doesn't really hurt to have "extra" handles here, other
                           // than a little extra memory usage.
                           SpaceBoundingElementUtil.RegisterSpaceBoundingElementHandle(exporterIFC, instanceHandle, familyInstance.Id,
                              setter.LevelId);

                           if (!IFCAnyHandleUtil.IsNullOrHasNoValue(materialProfileSet) && RepresentationUtil.RepresentationForStandardCaseFromProduct(exportType.ExportInstance, instanceHandle))
                           {
                              // RV does not support IfcMaterialProfileSetUsage, material assignment should be directly to the MaterialProfileSet
                              if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
                              {
                                 CategoryUtil.CreateMaterialAssociation(exporterIFC, instanceHandle, materialProfileSet);
                              }
                              else
                              {
                                 IFCAnyHandle matSetUsage = IFCInstanceExporter.CreateMaterialProfileSetUsage(file, materialProfileSet, null, null);
                                 CategoryUtil.CreateMaterialAssociation(exporterIFC, instanceHandle, matSetUsage);
                              }
                              materialAlreadyAssociated = true;
                           }

                           //export Base Quantities.
                           // This is necessary for now as it deals properly with split columns by level.
                           PropertyUtil.CreateBeamColumnBaseQuantities(exporterIFC, instanceHandle, familyInstance, typeInfo, geomObjects);
                           break;
                        }
                     case IFCEntityType.IfcDoor:
                     case IFCEntityType.IfcWindow:
                        {
                           double doorHeight = GetMinSymbolHeight(originalFamilySymbol);
                           double doorWidth = GetMinSymbolWidth(originalFamilySymbol);

                           double height = UnitUtil.ScaleLength(doorHeight);
                           double width = UnitUtil.ScaleLength(doorWidth);

                           IFCAnyHandle doorWindowLocalPlacement = !IFCAnyHandleUtil.IsNullOrHasNoValue(overrideLocalPlacement) ?
                               overrideLocalPlacement : localPlacement;
                           if (exportType.ExportType == IFCEntityType.IfcDoorType || exportType.ExportInstance == IFCEntityType.IfcDoor)
                              instanceHandle = IFCInstanceExporter.CreateDoor(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                                 doorWindowLocalPlacement, repHnd, height, width, doorWindowInfo.PreDefinedType,
                                 doorWindowInfo.DoorOperationTypeString, doorWindowInfo.UserDefinedOperationType);
                           else
                              instanceHandle = IFCInstanceExporter.CreateWindow(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                                 doorWindowLocalPlacement, repHnd, height, width, doorWindowInfo.PreDefinedType, DoorWindowUtil.GetIFCWindowPartitioningType(originalFamilySymbol),
                                 doorWindowInfo.UserDefinedPartitioningType);
                           wrapper.AddElement(familyInstance, instanceHandle, setter, extraParams, true, exportType);

                           SpaceBoundingElementUtil.RegisterSpaceBoundingElementHandle(exporterIFC, instanceHandle, familyInstance.Id,
                               setter.LevelId);

                           IFCAnyHandle placementToUse = doorWindowLocalPlacement;
                           if (!useInstanceGeometry)
                           {
                              // correct the placement to the symbol space
                              bool needToCreateOpenings = OpeningUtil.NeedToCreateOpenings(instanceHandle, extraParams);
                              if (needToCreateOpenings)
                              {
                                 Transform openingTrf = Transform.Identity;
                                 openingTrf.Origin = new XYZ(0, 0, setter.Offset);
                                 openingTrf = openingTrf.Multiply(doorWindowTrf);
                                 XYZ scaledOrigin = UnitUtil.ScaleLength(openingTrf.Origin);
                                 IFCAnyHandle openingLocalPlacement = ExporterUtil.CreateLocalPlacement(file, doorWindowLocalPlacement,
                                     scaledOrigin, openingTrf.BasisZ, openingTrf.BasisX);
                                 placementToUse = openingLocalPlacement;
                              }
                           }

                           OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, offsetTransform,
                               exporterIFC, placementToUse, setter, wrapper);
                           break;
                        }
                     case IFCEntityType.IfcMember:
                        {
                           if (exportType.HasUndefinedPredefinedType())
                              exportType.ValidatedPredefinedType = "BRACE";

                           instanceHandle = FamilyExporterUtil.ExportGenericInstance(exportType, exporterIFC, familyInstance,
                              wrapper, setter, extraParams, instanceGUID, ownerHistory, exportParts ? null : repHnd, ifcEnumType, overrideLocalPlacement);

                           OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, offsetTransform,
                               exporterIFC, localPlacement, setter, wrapper);
                           wrapper.AddElement(familyInstance, instanceHandle, setter, extraParams, true, exportType);

                           if (!IFCAnyHandleUtil.IsNullOrHasNoValue(materialProfileSet) && RepresentationUtil.RepresentationForStandardCaseFromProduct(exportType.ExportInstance, instanceHandle))
                           {
                              // RV does not support IfcMaterialProfileSetUsage, material assignment should be directly to the MaterialProfileSet
                              if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
                              {
                                 CategoryUtil.CreateMaterialAssociation(exporterIFC, instanceHandle, materialProfileSet);
                              }
                              else
                              {
                                 IFCAnyHandle matSetUsage = IFCInstanceExporter.CreateMaterialProfileSetUsage(file, materialProfileSet, null, null);
                                 CategoryUtil.CreateMaterialAssociation(exporterIFC, instanceHandle, matSetUsage);
                              }
                              materialAlreadyAssociated = true;
                           }

                           break;
                        }
                     case IFCEntityType.IfcPlate:
                        {
                           instanceHandle = FamilyExporterUtil.ExportGenericInstance(exportType, exporterIFC, familyInstance,
                              wrapper, setter, extraParams, instanceGUID, ownerHistory, exportParts ? null : repHnd, ifcEnumType, overrideLocalPlacement);

                           OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, offsetTransform,
                               exporterIFC, localPlacement, setter, wrapper);

                           if (RepresentationUtil.RepresentationForStandardCaseFromProduct(exportType.ExportInstance, instanceHandle))
                           {
                              double maxOffset = 0.0;
                              Parameter offsetPar = familySymbol.get_Parameter(BuiltInParameter.CURTAIN_WALL_SYSPANEL_OFFSET);
                              if (offsetPar == null)
                              {
                                 maxOffset = ParameterUtil.GetSpecialOffsetParameter(familySymbol);
                              }
                              else
                                 maxOffset = offsetPar.AsDouble();
                              wrapper.AddElement(familyInstance, instanceHandle, setter, extraParams, true, exportType);

                              if (materialLayerSet != null)
                              {
                                 if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
                                 {
                                    CategoryUtil.CreateMaterialAssociation(exporterIFC, instanceHandle, materialLayerSet);
                                 }
                                 else
                                 {
                                    IFCAnyHandle matSetUsage = IFCInstanceExporter.CreateMaterialLayerSetUsage(file, materialLayerSet, IFCLayerSetDirection.Axis3, IFCDirectionSense.Positive, maxOffset);
                                    CategoryUtil.CreateMaterialAssociation(exporterIFC, instanceHandle, matSetUsage);
                                 }
                                 materialAlreadyAssociated = true;
                              }
                           }
                           break;
                        }
                     case IFCEntityType.IfcTransportElement:
                        {
                           IFCAnyHandle localPlacementToUse;
                           ElementId roomId = setter.UpdateRoomRelativeCoordinates(familyInstance, out localPlacementToUse);

                           string operationTypeStr;
                           if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
                           {
                              // It is PreDefinedType attribute in IFC4
                              Toolkit.IFC4.IFCTransportElementType operationType = FamilyExporterUtil.GetPreDefinedType<Toolkit.IFC4.IFCTransportElementType>(familyInstance, ifcEnumType);
                              operationTypeStr = operationType.ToString();
                           }
                           else
                           {
                              Toolkit.IFCTransportElementType operationType = FamilyExporterUtil.GetPreDefinedType<Toolkit.IFCTransportElementType>(familyInstance, ifcEnumType);
                              operationTypeStr = operationType.ToString();
                           }

                           double capacityByWeight = 0.0;
                           ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "IfcCapacityByWeight", out capacityByWeight);
                           double capacityByNumber = 0.0;
                           ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "IfcCapacityByNumber", out capacityByNumber);

                           instanceHandle = IFCInstanceExporter.CreateTransportElement(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                              localPlacementToUse, repHnd, operationTypeStr, capacityByWeight, capacityByNumber);

                           bool containedInSpace = (roomId != ElementId.InvalidElementId);
                           wrapper.AddElement(familyInstance, instanceHandle, setter, extraParams, !containedInSpace, exportType);
                           if (containedInSpace)
                              ExporterCacheManager.SpaceInfoCache.RelateToSpace(roomId, instanceHandle);

                           break;
                        }
                     default:
                        {
                           if (IFCAnyHandleUtil.IsNullOrHasNoValue(instanceHandle))
                           {
                              bool isBuildingElementProxy =
                                  ((exportType.ExportInstance == IFCEntityType.IfcBuildingElementProxy) ||
                                  (exportType.ExportType == IFCEntityType.IfcBuildingElementProxyType));

                              IFCAnyHandle localPlacementToUse = null;
                              ElementId roomId = setter.UpdateRoomRelativeCoordinates(familyInstance, out localPlacementToUse);
                              bool containedInSpace = (roomId != ElementId.InvalidElementId) && (exportType.ExportInstance != IFCEntityType.IfcSystemFurnitureElement);

                              if (!isBuildingElementProxy)
                              {
                                 instanceHandle = IFCInstanceExporter.CreateGenericIFCEntity(exportType, exporterIFC, familyInstance, instanceGUID,
                                    ownerHistory, localPlacementToUse, repHnd);
                              }
                              else
                              {
                                 instanceHandle = IFCInstanceExporter.CreateBuildingElementProxy(exporterIFC, familyInstance, instanceGUID,
                                    ownerHistory, localPlacementToUse, repHnd, exportType.ValidatedPredefinedType);
                              }

                              bool associateToLevel = containedInSpace ? false : !isChildInContainer;
                              wrapper.AddElement(familyInstance, instanceHandle, setter, extraParams, associateToLevel, exportType);
                              if (containedInSpace)
                                 ExporterCacheManager.SpaceInfoCache.RelateToSpace(roomId, instanceHandle);
                           }

                           IFCAnyHandle placementToUse = localPlacement;
                           if (!useInstanceGeometry)
                           {
                              bool needToCreateOpenings = OpeningUtil.NeedToCreateOpenings(instanceHandle, extraParams);
                              if (needToCreateOpenings)
                              {
                                 Transform openingTrf = new Transform(originalTrf);
                                 Transform extraRot = new Transform(originalTrf);
                                 extraRot.Origin = XYZ.Zero;
                                 openingTrf = openingTrf.Multiply(extraRot);
                                 openingTrf = openingTrf.Multiply(typeInfo.StyleTransform);

                                 XYZ scaledOrigin = UnitUtil.ScaleLength(openingTrf.Origin);
                                 IFCAnyHandle openingRelativePlacement = ExporterUtil.CreateAxis2Placement3D(file, scaledOrigin,
                                    openingTrf.get_Basis(2), openingTrf.get_Basis(0));
                                 IFCAnyHandle openingPlacement = ExporterUtil.CopyLocalPlacement(file, localPlacement);
                                 GeometryUtil.SetRelativePlacement(openingPlacement, openingRelativePlacement);
                                 placementToUse = openingPlacement;
                              }
                           }

                           Transform offsetTransformToUse = null;
                           if (useInstanceGeometry && !MathUtil.IsAlmostZero(setter.Offset))
                           {
                              XYZ offsetOrig = -XYZ.BasisZ * setter.Offset;
                              Transform setterOffset = Transform.CreateTranslation(offsetOrig);
                              offsetTransformToUse = offsetTransform.Multiply(setterOffset);
                           }
                           else
                           {
                              offsetTransformToUse = offsetTransform;
                           }
                           OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, offsetTransformToUse,
                              exporterIFC, placementToUse, setter, wrapper);
                           break;
                        }
                  }

                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(instanceHandle))
                  {
                     if (exportParts)
                        PartExporter.ExportHostPart(exporterIFC, familyInstance, instanceHandle, familyProductWrapper, setter, null, overrideLevelId);
                        //PartExporter.ExportHostPart(exporterIFC, familyInstance, instanceHandle, familyProductWrapper, setter, setter.LocalPlacement, overrideLevelId);

                     if (ElementFilteringUtil.IsMEPType(exportType) || ElementFilteringUtil.ProxyForMEPType(familyInstance, exportType))
                     {
                        ExporterCacheManager.MEPCache.Register(familyInstance, instanceHandle);
                        // For ducts and pipes, check later if there is an associated duct or pipe.
                        if (CanHaveInsulationOrLining(exportType, categoryId))
                           ExporterCacheManager.MEPCache.CoveredElementsCache.Add(familyInstance.Id);
                     }

                     ExporterCacheManager.HandleToElementCache.Register(instanceHandle, familyInstance.Id);

                     if (!exportParts && !materialAlreadyAssociated)
                        CategoryUtil.CreateMaterialAssociation(exporterIFC, instanceHandle, typeInfo.MaterialIdList);

                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(typeInfo.Style))
                        ExporterCacheManager.TypeRelationsCache.Add(typeInfo.Style, instanceHandle);
                  }
               }

               if (doorWindowInfo != null)
               {
                  DoorWindowDelayedOpeningCreator delayedCreator = DoorWindowDelayedOpeningCreator.Create(exporterIFC, doorWindowInfo, instanceHandle, setter.LevelId);
                  if (delayedCreator != null)
                     ExporterCacheManager.DoorWindowDelayedOpeningCreatorCache.Add(delayedCreator);
               }
            }
         }
      }

      /// <summary>
      /// Exports a generic element as one of a few IFC building element entity types.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element to be exported.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="familyType">The export type.</param>
      /// <param name="ifcEnumTypeString">The string value represents the IFC type.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      /// <returns>True if the elements was exported, false otherwise.</returns>
      static public bool ExportGenericToSpecificElement(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement, IFCExportInfoPair exportType,
          string ifcEnumTypeString, ProductWrapper productWrapper)
      {
         // This function is here because it was originally used exclusive by FamilyInstances.  Moving forward, this will be combined with some other
         // functions to attempt to create a way to export any element as any IFC entity.  There will still be functions that do a better job of mapping
         // specific Revit element types to specific IFC entity types (e.g., a Revit Wall to an IFC IfcWallStandardCase), but most elements will use generic
         // handling.
         // Note that this function doesn't support creating types - it exports a simple IFC instance only of a few possible types.
         switch (exportType.ExportInstance)
         {
            case IFCEntityType.IfcBeam:
               {
                  // We will say that we exported the beam if either we generated an IfcBeam, or if we determined that there
                  // was nothing to export, either because the beam had no geometry to export, or it was completely clipped.

                  // The regular Beam has been moved to the ExportFamilyInstanceAsMappedItem, to be able to export its types and also as a mapped geometry
                  // standard building elements

                  // Limit this to IFC4, as beams no longer get axes exported if we use the code in ExportFamilyInstanceAsMappedItem.
                  if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 || (element is DirectShape))
                  {
                     bool dontExport;
                     IFCAnyHandle beamHnd = BeamExporter.ExportBeamAsStandardElement(exporterIFC, element, exportType, geometryElement, productWrapper, out dontExport);
                     return (dontExport || !IFCAnyHandleUtil.IsNullOrHasNoValue(beamHnd));
                  }
                  else
                     return false;
               }
            case IFCEntityType.IfcCovering:
               CeilingExporter.ExportCovering(exporterIFC, element, geometryElement, ifcEnumTypeString, productWrapper);
               return true;
            case IFCEntityType.IfcRamp:
               RampExporter.ExportRamp(exporterIFC, ifcEnumTypeString, element, geometryElement, 1, productWrapper);
               return true;
            case IFCEntityType.IfcRailing:
               if (ExporterCacheManager.RailingCache.Contains(element.Id))
               {
                  // Don't export this object if it is part of a parent railing.
                  if (!ExporterCacheManager.RailingSubElementCache.Contains(element.Id))
                  {
                     // RailingExporter.ExportRailing(exporterIFC, element, geometryElement, ifcEnumTypeString, productWrapper);
                     // Allow railing code to create instance and type.
                     return false;
                  }
               }
               else
               {
                  ExporterCacheManager.RailingCache.Add(element.Id);
               }
               return true;
            case IFCEntityType.IfcRoof:
               RoofExporter.ExportRoof(exporterIFC, element, geometryElement, productWrapper);
               return true;
            case IFCEntityType.IfcSlab:
               FloorExporter.ExportGenericSlab(exporterIFC, element, geometryElement, ifcEnumTypeString, productWrapper);
               //TODO
               return true;
            case IFCEntityType.IfcStair:
               StairsExporter.ExportStairAsSingleGeometry(exporterIFC, ifcEnumTypeString, element, geometryElement, new List<double>() { 0 }, productWrapper);
               return true;
            case IFCEntityType.IfcWall:
               WallExporter.ExportWall(exporterIFC, ifcEnumTypeString, element, null, geometryElement, productWrapper);
               return true;
         }
         return false;
      }

      /// <summary>
      /// Gets minimum height of a family symbol.
      /// </summary>
      /// <param name="symbol">
      /// The family symbol.
      /// </param>
      static double GetMinSymbolHeight(FamilySymbol symbol)
      {
         return ExporterIFCUtils.GetMinSymbolHeight(symbol);
      }

      /// <summary>
      /// Gets minimum width of a family symbol.
      /// </summary>
      /// <param name="symbol">
      /// The family symbol.
      /// </param>
      static double GetMinSymbolWidth(FamilySymbol symbol)
      {
         return ExporterIFCUtils.GetMinSymbolWidth(symbol);
      }

      /// <summary>
      /// Gets IFCBeamType from beam type name.
      /// </summary>
      /// <param name="element">The beam element.</param>
      /// <param name="defaultBeamType">The default beam type name.</param>
      /// <returns>The IFCBeamType.</returns>
      static IFCBeamType GetBeamType(Element element, string beamType)
      {
         string value = null;
         if (ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcType", out value) == null)
            value = beamType;

         if (String.IsNullOrEmpty(value))
            return IFCBeamType.Beam;

         string newValue = NamingUtil.RemoveSpacesAndUnderscores(value);

         if (String.Compare(newValue, "USERDEFINED", true) == 0)
            return IFCBeamType.UserDefined;

         return IFCBeamType.Beam;
      }


      /// <summary>
      /// Gets IFCColumnType from column type name.
      /// </summary>
      /// <param name="element">The column element.</param>
      /// <param name="columnType">The column type name.</param>
      /// <returns>The IFCColumnType.</returns>
      /// <remarks>This function appears incomplete, and should probably be removed and replaced.</remarks>
      public static IFCColumnType GetColumnType(Element element, string columnType)
      {
         string value = null;
         if (ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcType", out value) == null)
            value = columnType;

         if (String.IsNullOrEmpty(value))
            return IFCColumnType.Column;

         string newValue = NamingUtil.RemoveSpacesAndUnderscores(value);

         if (String.Compare(newValue, "USERDEFINED", true) == 0)
            return IFCColumnType.UserDefined;

         return IFCColumnType.Column;
      }

      public static IFCMemberType GetMemberType(Element element, string memberType)
      {
         string value = null;
         if (ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcType", out value) == null)
            value = memberType;

         if (String.IsNullOrEmpty(value))
            return IFCMemberType.Member;

         string newValue = NamingUtil.RemoveSpacesAndUnderscores(value);

         if (String.Compare(newValue, "USERDEFINED", true) == 0)
            return IFCMemberType.UserDefined;

         return IFCMemberType.Member;
      }

      /// <summary>
      /// Map the cardinal point index from Revit H and V justification. The mapping is not complete since they are not 1-1
      /// </summary>
      /// <param name="familyInstance"></param>
      /// <returns></returns>
      static int? BeamCardinalPoint(FamilyInstance familyInstance)
      {
         Parameter yz_just = familyInstance.get_Parameter(BuiltInParameter.YZ_JUSTIFICATION);
         if (yz_just == null)
            return null;

         // Independent justification on the start and end is not supported in IFC, only the Uniform one is
         if (yz_just.AsInteger() == (int)Autodesk.Revit.DB.Structure.YZJustificationOption.Independent)
            return null;
         Parameter y_just = familyInstance.get_Parameter(BuiltInParameter.Y_JUSTIFICATION);
         if (y_just == null)
            return null;

         int yJustification = y_just.AsInteger();
         Parameter z_just = familyInstance.get_Parameter(BuiltInParameter.Z_JUSTIFICATION);
         if (z_just == null)
            return null;

         int zJustification = z_just.AsInteger();

         int? cardinalPoint = null;
         if (yJustification == (int)Autodesk.Revit.DB.Structure.YJustification.Left && zJustification == (int)Autodesk.Revit.DB.Structure.ZJustification.Bottom)
            cardinalPoint = 1;
         else if (yJustification == (int)Autodesk.Revit.DB.Structure.YJustification.Left && zJustification == (int)Autodesk.Revit.DB.Structure.ZJustification.Origin)
            cardinalPoint = 4;
         else if (yJustification == (int)Autodesk.Revit.DB.Structure.YJustification.Left && zJustification == (int)Autodesk.Revit.DB.Structure.ZJustification.Top)
            cardinalPoint = 7;
         else if (yJustification == (int)Autodesk.Revit.DB.Structure.YJustification.Left && zJustification == (int)Autodesk.Revit.DB.Structure.ZJustification.Center)
            cardinalPoint = 5;
         else if (yJustification == (int)Autodesk.Revit.DB.Structure.YJustification.Origin && zJustification == (int)Autodesk.Revit.DB.Structure.ZJustification.Bottom)
            cardinalPoint = 2;
         else if (yJustification == (int)Autodesk.Revit.DB.Structure.YJustification.Origin && zJustification == (int)Autodesk.Revit.DB.Structure.ZJustification.Origin)
            cardinalPoint = 5;
         else if (yJustification == (int)Autodesk.Revit.DB.Structure.YJustification.Origin && zJustification == (int)Autodesk.Revit.DB.Structure.ZJustification.Top)
            cardinalPoint = 8;
         else if (yJustification == (int)Autodesk.Revit.DB.Structure.YJustification.Origin && zJustification == (int)Autodesk.Revit.DB.Structure.ZJustification.Center)
            cardinalPoint = 5;
         else if (yJustification == (int)Autodesk.Revit.DB.Structure.YJustification.Right && zJustification == (int)Autodesk.Revit.DB.Structure.ZJustification.Bottom)
            cardinalPoint = 3;
         else if (yJustification == (int)Autodesk.Revit.DB.Structure.YJustification.Right && zJustification == (int)Autodesk.Revit.DB.Structure.ZJustification.Origin)
            cardinalPoint = 6;
         else if (yJustification == (int)Autodesk.Revit.DB.Structure.YJustification.Right && zJustification == (int)Autodesk.Revit.DB.Structure.ZJustification.Top)
            cardinalPoint = 9;
         else if (yJustification == (int)Autodesk.Revit.DB.Structure.YJustification.Right && zJustification == (int)Autodesk.Revit.DB.Structure.ZJustification.Center)
            cardinalPoint = 6;
         else if (yJustification == (int)Autodesk.Revit.DB.Structure.YJustification.Center && zJustification == (int)Autodesk.Revit.DB.Structure.ZJustification.Bottom)
            cardinalPoint = 1;
         else if (yJustification == (int)Autodesk.Revit.DB.Structure.YJustification.Center && zJustification == (int)Autodesk.Revit.DB.Structure.ZJustification.Origin)
            cardinalPoint = 4;
         else if (yJustification == (int)Autodesk.Revit.DB.Structure.YJustification.Center && zJustification == (int)Autodesk.Revit.DB.Structure.ZJustification.Top)
            cardinalPoint = 1;
         else if (yJustification == (int)Autodesk.Revit.DB.Structure.YJustification.Center && zJustification == (int)Autodesk.Revit.DB.Structure.ZJustification.Center)
            cardinalPoint = 10;
         else
            cardinalPoint = null;       // not supported combination
         return cardinalPoint;
      }

      /// <summary>
      /// This function is to check whether a GeometryObject is empty (no Face/Edge for Solid, or no Triangle/Vert for Mesh). This may occur for certain geometry parts
      /// </summary>
      /// <param name="geom"></param>
      /// <returns></returns>
      private static bool GeometryIsEmpty(IList<Solid> solids, IList<Mesh> meshes)
      {
         if (solids.Count == 0 && meshes.Count == 0)
            return true;

         // We will return false (not empty) if any of the geometry is not empty
         foreach (Solid solidGeom in solids)
         {
            if (!solidGeom.Faces.IsEmpty || !solidGeom.Edges.IsEmpty)
               return false;
         }

         foreach (Mesh meshGeom in meshes)
         {
            if (meshGeom.NumTriangles > 0 || meshGeom.Vertices.Count > 0)
               return false;
         }

         return true;
      }

      private static GeometryObject GetPotentialCurveOrPolyline(Element element, Options options)
      {
         IList<GeometryObject> potentialCurves = new List<GeometryObject>();

         // Try to get curve geometry (for MEPCurve especially), to assist the geometry creation, e.g. SweptSolid
         // We get the coarse specification specifically because MEP objects are exported as curves in coarse.
         Options optionsCoarse = options;
         optionsCoarse.DetailLevel = ViewDetailLevel.Coarse;
         //

         GeometryElement potentialCurveGeom = element.get_Geometry(options);
         if (potentialCurveGeom == null)
            return null;

         foreach (GeometryObject geom in potentialCurveGeom)
         {
            if (geom is Curve || geom is PolyLine)
               potentialCurves.Add(geom);
         }

         // Only one curve can be supported
         if (potentialCurves.Count != 1)
            return null;

         return potentialCurves[0];
      }

#if DEBUG
      static StreamWriter outFile = null;
      public static void PrintDbgInfo(params string[] inputArgs)
      {
         if (outFile == null)
         {
            outFile = new StreamWriter(@"e:\temp\debug2dinfo.txt");
         }

         string data = "\t\t";
         foreach (string inputArg in inputArgs)
            data += " " + inputArg;
         outFile.WriteLine(data);
         outFile.Flush();
      }
#else
      public static void PrintDbgInfo(params string[] inputArgs)
      {
         return;
      }
#endif
   }
}