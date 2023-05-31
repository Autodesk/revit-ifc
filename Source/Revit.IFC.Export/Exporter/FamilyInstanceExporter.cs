﻿//
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

      public static IList<IFCAnyHandle> CreateShapeRepresentations(ExporterIFC exporterIFC,
         IFCFile file, Element instance, ElementId categoryId, FamilyTypeInfo typeInfo,
         XYZ scaledMapOrigin)
      {
         IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();

         IFCAnyHandle contextOfItems2d = ExporterCacheManager.Get2DContextHandle(IFCRepresentationIdentifier.Annotation);
         IFCAnyHandle contextOfItems1d = ExporterCacheManager.Get3DContextHandle(IFCRepresentationIdentifier.Axis);


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
               return null;

            HashSet<IFCAnyHandle> representations = new HashSet<IFCAnyHandle>();
            representations.Add(ExporterUtil.CreateDefaultMappedItem(file, repMap, scaledMapOrigin));
            IFCAnyHandle shapeRep = null;
            switch (dimRepMap)
            {
               case 3:
                  {
                     IFCAnyHandle mapRep = IFCAnyHandleUtil.GetInstanceAttribute(repMap, "MappedRepresentation");
                     IFCAnyHandle context = IFCAnyHandleUtil.GetInstanceAttribute(mapRep, "ContextOfItems");
                     shapeRep = RepresentationUtil.CreateBodyMappedItemRep(exporterIFC, instance, categoryId, context,
                           representations);
                     break;
                  }
               case 2:
                  {
                     shapeRep = RepresentationUtil.CreatePlanMappedItemRep(exporterIFC, instance, categoryId, contextOfItems2d,
                     representations);
                     break;
                  }
               case 1:
                  {
                     shapeRep = RepresentationUtil.CreateGraphMappedItemRep(exporterIFC, instance, categoryId, contextOfItems1d,
                     representations);
                     break;
                  }
            }

            if (IFCAnyHandleUtil.IsNullOrHasNoValue(shapeRep))
               return null;

            shapeReps.Add(shapeRep);
         }

         return shapeReps;
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
      FamilyInstance familyInstance, ref GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         // Don't export family if it is invisible, or has a null geometry.
         if (familyInstance.Invisible || geometryElement == null)
            return;

         // Don't export family instance if it has a curtain grid host; the host will be in charge of exporting.
         Element host = familyInstance.Host;
         if (CurtainSystemExporter.IsCurtainSystem(host) || 
            CurtainSystemExporter.IsLegacyCurtainElement(host))
            return;

         FamilySymbol familySymbol = familyInstance.Symbol;
         Family family = familySymbol.Family;
         if (family == null)
            return;

         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            string ifcEnumType;
            IFCExportInfoPair exportType = ExporterUtil.GetProductExportType(exporterIFC, familyInstance, out ifcEnumType);

            if (exportType.IsUnKnown)
               return;

            // TODO: This step now appears to be redundant with the rest of the steps, but to change it is too much of risk of regression. Reserve it for future refactoring
            if (ExportGenericToSpecificElement(exporterIFC, familyInstance, ref geometryElement, exportType, ifcEnumType, productWrapper))
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

      /// <summary>
      /// Create an IfcTypeEntity handle with a mapped representation.
      /// </summary>
      /// <param name="exporterIFC">The ExportIFC class.</param>
      /// <param name="typeKey">The information that identifies the type object.</param>
      /// <param name="typeInfo">The type information.</param>
      /// <param name="doorWindowInfo">Optional extra information for doors and windows.</param>
      /// <param name="representations3D">The 3D representation items.</param>
      /// <param name="trfRepMapList">An optional list of transforms.</param>
      /// <param name="representations2D">The 2D representation items.</param>
      /// <param name="familyInstance">The instance element.</param>
      /// <param name="familySymbol">The type element.</param>
      /// <param name="originalFamilySymbol">For FamilyInstances, the primary symbol.</param>
      /// <param name="useInstanceGeometry">True if we should ues the instance geometry.</param>
      /// <param name="exportParts">True if we are exporting parts.</param>
      /// <param name="exportType">The entity type information.</param>
      /// <param name="propertySets">The created list of property sets.</param>
      /// <returns>An IfcTypeEntity handle.</returns>
      public static IFCAnyHandle CreateTypeEntityHandle(ExporterIFC exporterIFC,
         TypeObjectKey typeKey, ref FamilyTypeInfo typeInfo, DoorWindowInfo doorWindowInfo,
         IList<IFCAnyHandle> representations3D, IList<Transform> trfRepMapList,
         IList<IFCAnyHandle> representations2D, Element familyInstance, ElementType familySymbol,
         ElementType originalFamilySymbol, ElementId overrideLevelId, bool useInstanceGeometry, 
         bool exportParts, IFCExportInfoPair exportType, out HashSet<IFCAnyHandle> propertySets)
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

         // The GUID calculations have a number of exceptions that we determine here.
         Element instanceOrSymbol = useInstanceGeometry ? familyInstance : originalFamilySymbol;
         int? index = ExporterCacheManager.FamilySymbolToTypeInfoCache.GetAlternateGUIDIndex(typeKey);
         string guid = GUIDUtil.GenerateIFCGuidFrom(useInstanceGeometry, instanceOrSymbol,
            exportType, typeKey, index);

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(typeStyle))
         {
            switch (exportType.ExportInstance)
            {
               case IFCEntityType.IfcBeam:
                  {
                     string beamType = exportType.ValidatedPredefinedType;
                     if (string.IsNullOrEmpty(beamType) || beamType.Equals("NOTDEFINED", StringComparison.InvariantCultureIgnoreCase))
                        beamType = "Beam";
                     typeStyle = IFCInstanceExporter.CreateBeamType(file, familySymbol, guid,
                        propertySets, repMapList, beamType);
                     break;
                  }
               case IFCEntityType.IfcColumn:
                  {
                     string columnType = exportType.ValidatedPredefinedType;
                     if (string.IsNullOrEmpty(columnType) || columnType.Equals("NOTDEFINED", StringComparison.InvariantCultureIgnoreCase))
                        columnType = "Column";
                     typeStyle = IFCInstanceExporter.CreateColumnType(file, familySymbol, guid,
                        propertySets, repMapList, columnType);
                     break;
                  }
               case IFCEntityType.IfcMember:
                  {
                     string memberType = exportType.ValidatedPredefinedType;
                     if (string.IsNullOrEmpty(memberType) || memberType.Equals("NOTDEFINED", StringComparison.InvariantCultureIgnoreCase))
                        memberType = "Brace";
                     typeStyle = IFCInstanceExporter.CreateMemberType(file, familySymbol, guid,
                        propertySets, repMapList, memberType);
                     break;
                  }
               case IFCEntityType.IfcDoor:
                  {
                     IFCAnyHandle doorLining = DoorWindowUtil.CreateDoorLiningProperties(exporterIFC,
                        familyInstance);
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(doorLining))
                        propertySets.Add(doorLining);

                     IList<IFCAnyHandle> doorPanels = DoorWindowUtil.CreateDoorPanelProperties(exporterIFC,
                        doorWindowInfo, familyInstance);
                     propertySets.UnionWith(doorPanels);

                     if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
                     {
                        typeStyle = IFCInstanceExporter.CreateDoorStyle(file, familySymbol,
                           guid, propertySets, repMapList, doorWindowInfo.DoorOperationTypeString,
                           DoorWindowUtil.GetDoorStyleConstruction(familyInstance),
                           paramTakesPrecedence, sizeable);
                     }
                     else
                     {
                        typeStyle = IFCInstanceExporter.CreateDoorType(file, familySymbol,
                           guid, propertySets, repMapList, doorWindowInfo.PreDefinedType,
                           doorWindowInfo.DoorOperationTypeString, paramTakesPrecedence,
                           doorWindowInfo.UserDefinedOperationType);
                     }
                     break;
                  }
               case IFCEntityType.IfcSpace:
                  {
                     typeStyle = IFCInstanceExporter.CreateSpaceType(file, familySymbol, guid,
                        propertySets, repMapList, exportType.ValidatedPredefinedType);
                     break;
                  }
               case IFCEntityType.IfcSystemFurnitureElement:
                  {
                     typeStyle = IFCInstanceExporter.CreateSystemFurnitureElementType(file,
                        familySymbol, guid, propertySets, repMapList, exportType.ValidatedPredefinedType);
                     break;
                  }
               case IFCEntityType.IfcWindow:
                  {
                     IFCWindowStyleOperation operationType = DoorWindowUtil.GetIFCWindowStyleOperation(originalFamilySymbol);
                     IFCWindowStyleConstruction constructionType = DoorWindowUtil.GetIFCWindowStyleConstruction(familyInstance);

                     IFCAnyHandle windowLining = DoorWindowUtil.CreateWindowLiningProperties(exporterIFC, familyInstance, null);
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(windowLining))
                        propertySets.Add(windowLining);

                     IList<IFCAnyHandle> windowPanels =
                        DoorWindowUtil.CreateWindowPanelProperties(exporterIFC, familyInstance, null);
                     propertySets.UnionWith(windowPanels);

                     if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
                     {
                        typeStyle = IFCInstanceExporter.CreateWindowStyle(file, familySymbol,
                           guid, propertySets, repMapList, constructionType, operationType,
                           paramTakesPrecedence, sizeable);
                     }
                     else
                     {
                        typeStyle = IFCInstanceExporter.CreateWindowType(file, familySymbol,
                           guid, propertySets, repMapList, doorWindowInfo.PreDefinedType,
                           DoorWindowUtil.GetIFCWindowPartitioningType(originalFamilySymbol),
                           paramTakesPrecedence, doorWindowInfo.UserDefinedOperationType);
                     }
                     break;
                  }
               case IFCEntityType.IfcBuildingElementProxy:
                  {
                     typeStyle = IFCInstanceExporter.CreateGenericIFCType(exportType, familySymbol,
                        guid, file, propertySets, repMapList);
                     break;
                  }
               case IFCEntityType.IfcFurniture:
               case IFCEntityType.IfcFurnishingElement:
                  {
                     typeStyle = IFCInstanceExporter.CreateFurnitureType(file, familySymbol,
                        guid, propertySets, repMapList, null, null, null,
                        exportType.ValidatedPredefinedType);
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
            typeStyle = FamilyExporterUtil.ExportGenericType(exporterIFC, exportType,
               exportType.ValidatedPredefinedType, propertySets, repMapList, familyInstance,
               familySymbol, guid);
         }

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(typeStyle))
            return null;

         typeInfo.Style = typeStyle;

         return typeStyle;
      }

      private static readonly HashSet<ElementId> s_InsulatedOrLinedSet = new HashSet<ElementId>()
      {
         new ElementId(BuiltInCategory.OST_DuctAccessory)
         , new ElementId(BuiltInCategory.OST_DuctCurves)
         , new ElementId(BuiltInCategory.OST_DuctFitting)
         , new ElementId(BuiltInCategory.OST_FlexDuctCurves)
         , new ElementId(BuiltInCategory.OST_FlexPipeCurves)
         , new ElementId(BuiltInCategory.OST_PipeAccessory)
         , new ElementId(BuiltInCategory.OST_PipeCurves)
         , new ElementId(BuiltInCategory.OST_PipeFitting)
      };
      private static bool CanHaveInsulationOrLining(IFCExportInfoPair exportType, ElementId categoryId)
      {
         // This is intended to reduce the number of exceptions thrown in GetLiningIds and GetInsulationIds.
         // There may still be some exceptions thrown as the category list below is still too large for GetLiningIds.
         if (exportType.ExportType != IFCEntityType.IfcDuctFittingType && exportType.ExportType != IFCEntityType.IfcPipeFittingType &&
            exportType.ExportType != IFCEntityType.IfcDuctSegmentType && exportType.ExportType != IFCEntityType.IfcPipeSegmentType)
            return false;

         return s_InsulatedOrLinedSet.Contains(categoryId);
      }

      private static bool CanHaveSystemDefinition(IFCExportInfoPair exportType, ElementId categoryId)
      {
         if (exportType.ExportType != IFCEntityType.IfcCableSegmentType && exportType.ExportType != IFCEntityType.IfcCableCarrierSegmentType &&
            exportType.ExportType != IFCEntityType.IfcCableFittingType && exportType.ExportType != IFCEntityType.IfcCableCarrierFittingType)
            return false;

         return (categoryId == new ElementId(BuiltInCategory.OST_CableTrayFitting) || categoryId == new ElementId(BuiltInCategory.OST_ConduitFitting));
      }

      private static bool CreateMaterialAssociation(IFCFile file, IFCAnyHandle instanceHandle, IFCAnyHandle materialProfileSet,
         int? cardinalPoint)
      {
         if (materialProfileSet == null)
            return false;

         // RV does not support IfcMaterialProfileSetUsage, material assignment should be directly to
         // the MaterialProfileSet.
         if (!ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
            CategoryUtil.CreateMaterialProfileSetUsage(file, materialProfileSet, cardinalPoint);
         else
            CategoryUtil.CreateMaterialAssociation(instanceHandle, materialProfileSet);

         return true;
      }

      private static bool IsTransformValid(Transform transform)
      {
         // There are no good API functions that check if a
         // Transform has invalid information in it.  So just
         // try assignment.
         try
         {
            XYZ checkTransform = transform.OfPoint(XYZ.Zero);
         }
         catch (Autodesk.Revit.Exceptions.ArgumentException)
         {
            return false;
         }

         return true;
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

         // A Family Instance can have its own copy of geometry, or use the symbol's copy with a transform.
         // The routine below tells us whether to use the Instance's copy or the Symbol's copy.
         bool useInstanceGeometry = GeometryUtil.UsesInstanceGeometry(familyInstance);
         Transform trf = familyInstance.GetTransform();
         if (!IsTransformValid(trf))
         {
            // We have found cases where there are family instances with invalid transform
            // information.  If we find them, ignore them (they won't be visible in Revit,
            // either.)
            return;
         }

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

         MaterialAndProfile materialAndProfile = null;
         IFCAnyHandle materialProfileSet = null;
         IFCAnyHandle materialLayerSet = null;
         IFCExportBodyParams extrusionData = null;

         IList<Transform> repMapTrfList = new List<Transform>();

         XYZ orig = XYZ.Zero;
         XYZ extrudeDirection = null;

         BodyData bodyData = null;

         // Extra information if we are exporting a door or a window.
         DoorWindowInfo doorWindowInfo = null;
         if (exportType.ExportType == IFCEntityType.IfcDoorType || exportType.ExportInstance == IFCEntityType.IfcDoor)
            doorWindowInfo = DoorWindowExporter.CreateDoor(exporterIFC, familyInstance, hostElement, overrideLevelId, trf, exportType);
         else if (exportType.ExportType == IFCEntityType.IfcWindowType || exportType.ExportInstance == IFCEntityType.IfcWindow)
            doorWindowInfo = DoorWindowExporter.CreateWindow(exporterIFC, familyInstance, hostElement, overrideLevelId, trf, exportType);

         FamilyTypeInfo typeInfo = new FamilyTypeInfo();
         IFCExportBodyParams extraParams = typeInfo.extraParams;

         exportType = FamilyExporterUtil.AdjustExportTypeForSchema(exportType, exportType.ValidatedPredefinedType);

         bool flipped = doorWindowInfo?.FlippedSymbol ?? false;
         ElementId overrideMaterialId = ExporterUtil.GetSingleMaterial(familyInstance);

         var typeKey = new TypeObjectKey(originalFamilySymbol.Id,
            overrideLevelId, flipped, exportType, overrideMaterialId);

         FamilyTypeInfo currentTypeInfo = 
            ExporterCacheManager.FamilySymbolToTypeInfoCache.Find(typeKey);
         bool foundNotEmpty = currentTypeInfo.IsValid();
         bool foundButEmpty = false;
         // Even though the type may be defined previously (found), the type may not be
         // complete in case the type is created against an instance that has no geometry in
         // it (e.g. Column that is split into Parts). In this case, we will conditionally
         // create a new type.  If the new type also has no geometry, we will use the old
         // type.
         if (foundNotEmpty && !IFCAnyHandleUtil.IsNullOrHasNoValue(currentTypeInfo.Style))
         {
            IList<IFCAnyHandle> repMaps = GeometryUtil.GetRepresentationMaps(currentTypeInfo.Style);
            if ((repMaps?.Count ?? 0) == 0)
            {
               foundNotEmpty = false;
               foundButEmpty = true;
            }
         }

         IList<GeometryObject> geomObjects = new List<GeometryObject>();
         Transform offsetTransform = Transform.Identity;

         Transform doorWindowTrf = Transform.Identity;

         // We will create a new mapped type if:
         // 1.  We are using the instance's copy of the geometry (that it, it has unique geometry), OR
         // 2.  We haven't already created the type.
         bool creatingType = (useInstanceGeometry || !foundNotEmpty);
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
                  {
                     geomObjects.Add(exportGeometry);
                  }

                  bool isExtrusionFriendlyType = IsExtrusionFriendlyType(exportType.ExportInstance);
                  bool tryToExportAsExtrusion = (!ExporterCacheManager.ExportOptionsCache.ExportAs2x2
                     || isExtrusionFriendlyType);

                  if (isExtrusionFriendlyType)
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

                        if (solids.Count > 0)
                        {
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
                           ExtrusionExporter.ExtraClippingData extraClippingData = null;
                           IFCAnyHandle bodyRepresentation = ExtrusionExporter.CreateExtrusionWithClipping(exporterIFC, exportGeometryElement,
                                 categoryId, false, solids, basePlaneToUse, orig, extrusionDirectionToUse, null, out extraClippingData,
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

                           typeInfo.MaterialIdList = extraClippingData.MaterialIds;
                           if (!IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRepresentation))
                           {
                              representations3D.Add(bodyRepresentation);
                              repMapTrfList.Add(null);
                              if (materialAndProfile != null)
                                 typeInfo.MaterialAndProfile = materialAndProfile;   // Keep material and profile information in the type info for later creation

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
                  else
                  {
                     extraParams.PossibleExtrusionAxes = IFCExtrusionAxes.TryXYZ;
                  }

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
                        bodyExporterOptions.CollectFootprintHandle = !ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4;

                     GeometryObject potentialPathGeom = GetPotentialCurveOrPolyline(exportGeometryElement, options);  
                     bodyData = BodyExporter.ExportBody(exporterIFC, familyInstance, categoryId, ExporterUtil.GetSingleMaterial(familyInstance),
                           geomObjects, bodyExporterOptions, extraParams, potentialPathGeom, profileName: profileName, instanceGeometry:useInstanceGeometry);
                     typeInfo.MaterialIdList = bodyData.MaterialIds;
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
                              // When it is the case of NOT using instance geometry (i.e. using the
                              // original family symbol), use the transform of the familyInstance as
                              // the new LCS.  This transform will be set as the Object LCS later on.
                              newLCS = trf.Multiply(offsetTransform);
                           }
                           else
                           {
                              IFCAnyHandle lcsHnd = extraParams.GetLocalPlacement();
                              // It appears that the local placement is already scaled. Unscale it here
                              // because axisInfo is based on unscaled information.
                              newLCS = ExporterUtil.UnscaleTransformOrigin(ExporterUtil.GetTransformFromLocalPlacementHnd(lcsHnd));
                           }

                           ElementId catId = CategoryUtil.GetSafeCategoryId(familyInstance);
                           IFCAnyHandle axisRep = StructuralMemberExporter.CreateStructuralMemberAxis(exporterIFC,
                              familyInstance, catId, axisInfo, newLCS);
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

                     // Keep Material and Profile information in the typeinfo for creation of MaterialSet later on
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
                     curveOffset = -(offsetTransform.Origin);

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
                        IFCAnyHandle contextOfItems2d = ExporterCacheManager.Get2DContextHandle(IFCRepresentationIdentifier.Annotation);
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
               //TODO: Need to find out if ScaledArea and CrossSectionArea and others have same values and meaning.
               //      If yes then need to refactor code and eliminate duplication.
               if (typeInfo.MaterialAndProfile.CrossSectionArea.HasValue)
                  typeInfo.extraParams.ScaledArea = typeInfo.MaterialAndProfile.CrossSectionArea.Value;
               if (typeInfo.MaterialAndProfile.ExtrusionDepth.HasValue)
                  typeInfo.extraParams.ScaledLength = typeInfo.MaterialAndProfile.ExtrusionDepth.Value;
               if (typeInfo.MaterialAndProfile.InnerPerimeter.HasValue)
                  typeInfo.extraParams.ScaledInnerPerimeter = typeInfo.MaterialAndProfile.InnerPerimeter.Value;
               if (typeInfo.MaterialAndProfile.OuterPerimeter.HasValue)
                  typeInfo.extraParams.ScaledOuterPerimeter = typeInfo.MaterialAndProfile.OuterPerimeter.Value;
            }

            HashSet<IFCAnyHandle> propertySets = null;
            IFCAnyHandle typeStyle = null;

            // If we found something already that was empty, and we didn't find anything
            // this time around, don't create it again.
            if (!foundButEmpty || representations2D.Count != 0 || representations3D.Count != 0)
            {
               typeStyle = CreateTypeEntityHandle(exporterIFC, typeKey, ref typeInfo,
                  doorWindowInfo, representations3D, repMapTrfList, representations2D,
                  familyInstance, familySymbol, originalFamilySymbol, overrideLevelId,
                  useInstanceGeometry, exportParts, exportType, out propertySets);
            }

            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(typeStyle))
            {
               wrapper.RegisterHandleWithElementType(familySymbol, exportType, typeStyle, propertySets);

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
                  MaterialLayerSetInfo mlsInfo = new MaterialLayerSetInfo(exporterIFC, familyInstance, wrapper);
                  materialLayerSet = mlsInfo.MaterialLayerSetHandle;
                  if (CategoryUtil.CreateMaterialAssociation(typeStyle, materialLayerSet))
                     addedMaterialAssociation = true;
               }
               else
               {
                  Element elementType = doc.GetElement(familyInstance.GetTypeId());
                  CategoryUtil.TryToCreateMaterialAssocation(exporterIFC, bodyData, elementType,
                     familyInstance, exportGeometry, typeStyle, typeInfo);
                  addedMaterialAssociation = true;
               }

               if (!addedMaterialAssociation)
                  CategoryUtil.CreateMaterialAssociation(exporterIFC, typeStyle, typeInfo.MaterialIdList);

               ClassificationUtil.CreateClassification(exporterIFC, file, familySymbol, typeStyle);        // Create other generic classification from ClassificationCode(s)
               ClassificationUtil.CreateUniformatClassification(exporterIFC, file, originalFamilySymbol, typeStyle);
            }
         }

         if ((foundNotEmpty || foundButEmpty) && !typeInfo.IsValid())
            typeInfo = currentTypeInfo;

         // we'll pretend we succeeded, but we'll do nothing.
         if (!typeInfo.IsValid())
            return;

         extraParams = typeInfo.extraParams;

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

         // add to the map, as long as we are not using instance geometry, and don't have extra openings.
         if (!useInstanceGeometry)
         {
            bool hasOpenings = (extraParams.GetOpenings().Count != 0);
            ExporterCacheManager.FamilySymbolToTypeInfoCache.Register(typeKey, typeInfo, hasOpenings);
         }

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

         // Create instance.  
         IList<IFCAnyHandle> shapeReps = CreateShapeRepresentations(exporterIFC, file, familyInstance,
            categoryId, typeInfo, scaledMapOrigin);
         if (shapeReps == null)
            return;

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

         if (familyInstance.AssemblyInstanceId != null && familyInstance.AssemblyInstanceId != ElementId.InvalidElementId)
         {
            if (ExporterCacheManager.AssemblyInstanceCache.TryGetValue(familyInstance.AssemblyInstanceId, out AssemblyInstanceInfo assInfo))
            {
               if (overrideLevelId == ElementId.InvalidElementId)
                  overrideLevelId = assInfo.AssignedLevelId;

               double newOffset = trf.Origin.Z;
               string shapeType = null;
               foreach (IFCAnyHandle shapeRep in shapeReps)
               {
                  if (IFCAnyHandleUtil.GetRepresentationIdentifier(shapeRep).Equals("Body"))
                  {
                     shapeType = IFCAnyHandleUtil.GetBaseRepresentationType(shapeRep);
                  }                  
               }

               if (!string.IsNullOrEmpty(shapeType) && (shapeType.Contains("Brep") || shapeType.Equals("Tessellation")))
               {
                  // Use LocationPoint for the offset if any as the Brep/Tessellation will have reference to it
                  LocationPoint loc = familyInstance.Location as LocationPoint;
                  if (loc != null)
                  {
                     newOffset = loc.Point.Z;
                  }
                  else
                  {
                     BoundingBoxXYZ bbox = familyInstance.get_BoundingBox(null);
                     if (bbox != null)
                     {
                        newOffset = bbox.Min.Z;
                     }
                  }
               }

               trf.Origin = new XYZ(trf.Origin.X, trf.Origin.Y, newOffset);
            }
         }

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
                  instanceGUID = GUIDUtil.GenerateIFCGuidFrom(
                     GUIDUtil.CreateGUIDString(familyInstance, subElementIndex.ToString()));

               IFCAnyHandle overrideLocalPlacement = null;
               bool isChildInContainer = familyInstance.AssemblyInstanceId != ElementId.InvalidElementId;

               if (parentLocalPlacement != null)
               {
                  Transform relTrf = ExporterIFCUtils.GetRelativeLocalPlacementOffsetTransform(parentLocalPlacement, localPlacement);
                  Transform inverseTrf = relTrf.Inverse;

                  IFCAnyHandle childLocalPlacement = ExporterUtil.CreateLocalPlacement(file, parentLocalPlacement,
                        inverseTrf.Origin, inverseTrf.BasisZ, inverseTrf.BasisX);
                  overrideLocalPlacement = childLocalPlacement;
               }

               switch (exportType.ExportInstance)
               {
                  case IFCEntityType.IfcBeam:
                     {
                        if (exportType.HasUndefinedPredefinedType())
                           exportType.ValidatedPredefinedType = "BEAM";
                        instanceHandle = FamilyExporterUtil.ExportGenericInstance(exportType, exporterIFC, familyInstance,
                           wrapper, setter, extraParams, instanceGUID, ownerHistory, exportParts ? null : repHnd, overrideLocalPlacement);
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
                           if (CreateMaterialAssociation(file, instanceHandle, materialProfileSet, cardinalPoint))
                              materialAlreadyAssociated = true;
                        }
                        break;
                     }
                  case IFCEntityType.IfcColumn:
                     {
                        if (exportType.HasUndefinedPredefinedType())
                           exportType.ValidatedPredefinedType = "COLUMN";
                        instanceHandle = FamilyExporterUtil.ExportGenericInstance(exportType, exporterIFC, familyInstance,
                           wrapper, setter, extraParams, instanceGUID, ownerHistory, exportParts ? null : repHnd, overrideLocalPlacement);

                        IFCAnyHandle placementToUse = localPlacement;
                        if (!useInstanceGeometry)
                        {
                           bool needToCreateOpenings = OpeningUtil.NeedToCreateOpenings(instanceHandle, extraParams);
                           if (needToCreateOpenings)
                           {
                              Transform openingTrf = new Transform(originalTrf);
                              Transform extraRot = new Transform(originalTrf) { Origin = XYZ.Zero };
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

                        if (CreateMaterialAssociation(file, instanceHandle, materialProfileSet, null))
                           materialAlreadyAssociated = true;

                        //export Base Quantities.
                        // This is necessary for now as it deals properly with split columns by level.
                        PropertyUtil.CreateBeamColumnBaseQuantities(exporterIFC, instanceHandle, familyInstance, typeInfo, geomObjects);
                        break;
                     }
                  case IFCEntityType.IfcDoor:
                  case IFCEntityType.IfcWindow:
                     {
                        (double doorWidth, double doorHeight) = GetDoorWindowDimensionFromSymbol(originalFamilySymbol, familyInstance);

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
                              Transform openingTrf = trf;
                              openingTrf.Origin = new XYZ(0, 0, setter.Offset);
                              openingTrf = doorWindowTrf.Multiply(openingTrf);
                              XYZ scaledOrigin = UnitUtil.ScaleLength(openingTrf.Origin);

                              // This copy is used for new IFCLocalPlacement with new RelativePlacement and original objectplacement attribute.
                              IFCAnyHandle copiedLocalPlacement = ExporterUtil.CopyLocalPlacement(file, doorWindowLocalPlacement);

                              // This placement will be used for further logic (openings etc).
                              // If there is an opening, doorWindow's ObjectPlacement will be deleted and opening's ObjectPlacement will be used instead.
                              IFCAnyHandle openingLocalPlacement = ExporterUtil.CreateLocalPlacement(file, copiedLocalPlacement,
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
                           wrapper, setter, extraParams, instanceGUID, ownerHistory, exportParts ? null : repHnd, overrideLocalPlacement);

                        OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, offsetTransform,
                              exporterIFC, localPlacement, setter, wrapper);
                        wrapper.AddElement(familyInstance, instanceHandle, setter, extraParams, true, exportType);

                        if (CreateMaterialAssociation(file, instanceHandle, materialProfileSet, null))
                           materialAlreadyAssociated = true;

                        break;
                     }
                  case IFCEntityType.IfcPlate:
                     {
                        instanceHandle = FamilyExporterUtil.ExportGenericInstance(exportType, exporterIFC, familyInstance,
                           wrapper, setter, extraParams, instanceGUID, ownerHistory, exportParts ? null : repHnd, overrideLocalPlacement);

                        OpeningUtil.CreateOpeningsIfNecessary(instanceHandle, familyInstance, extraParams, offsetTransform,
                              exporterIFC, localPlacement, setter, wrapper);

                        if (RepresentationUtil.RepresentationForStandardCaseFromProduct(exportType.ExportInstance, instanceHandle))
                        {
                           wrapper.AddElement(familyInstance, instanceHandle, setter, extraParams, true, exportType);

                           if (CreateMaterialAssociation(file, instanceHandle, materialProfileSet, null))
                              materialAlreadyAssociated = true;
                        }
                        break;
                     }
                  case IFCEntityType.IfcTransportElement:
                     {
                        IFCAnyHandle localPlacementToUse;
                        ElementId roomId = setter.UpdateRoomRelativeCoordinates(familyInstance, out localPlacementToUse);

                        Enum operationType;
                        if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
                        {
                           // It is PreDefinedType attribute in IFC4
                           operationType = FamilyExporterUtil.GetPreDefinedType<Toolkit.IFC4.IFCTransportElementType>(familyInstance, familySymbol, ifcEnumType);
                        }
                        else
                        {
                           operationType = FamilyExporterUtil.GetPreDefinedType<Toolkit.IFCTransportElementType>(familyInstance, familySymbol, ifcEnumType);
                        }
                        string operationTypeStr = operationType.ToString();

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

                           bool associateToLevel = !containedInSpace && !isChildInContainer;
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
                              Transform extraRot = new Transform(originalTrf) { Origin = XYZ.Zero };
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
                     // For cable trays and conduits, we might create systems during the end of export.
                     if (CanHaveSystemDefinition(exportType, categoryId))
                        ExporterCacheManager.MEPCache.CableElementsCache.Add(familyInstance.Id);
                  }

                  ExporterCacheManager.HandleToElementCache.Register(instanceHandle, familyInstance.Id);

                  if (!exportParts && !materialAlreadyAssociated)
                  {
                     // Create material association for the instance only if the instance
                     // geometry is different from the type or the type does not have any
                     // material association
                     IFCAnyHandle constituentSetHnd = ExporterCacheManager.MaterialConstituentSetCache.Find(familyInstance.GetTypeId());
                     if ((useInstanceGeometry || IFCAnyHandleUtil.IsNullOrHasNoValue(constituentSetHnd))
                        && bodyData != null && bodyData.RepresentationItemInfo != null && bodyData.RepresentationItemInfo.Count > 0)
                     {
                        CategoryUtil.CreateMaterialAssociationWithShapeAspect(exporterIFC, familyInstance, instanceHandle, bodyData.RepresentationItemInfo);
                     }
                     else
                     {
                        // Create material association in case if bodyData is null
                        CategoryUtil.CreateMaterialAssociation(exporterIFC, instanceHandle, typeInfo.MaterialIdList);
                     }
                  }

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
      static public bool ExportGenericToSpecificElement(ExporterIFC exporterIFC, Element element, ref GeometryElement geometryElement, IFCExportInfoPair exportType,
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
               CeilingExporter.ExportCovering(exporterIFC, element, ref geometryElement, ifcEnumTypeString, productWrapper);
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
               RoofExporter.ExportRoof(exporterIFC, element, ref geometryElement, productWrapper);
               return true;
            case IFCEntityType.IfcSlab:
               FloorExporter.ExportGenericSlab(exporterIFC, element, geometryElement, ifcEnumTypeString, productWrapper);
               //TODO
               return true;
            case IFCEntityType.IfcStair:
               StairsExporter.ExportStairAsSingleGeometry(exporterIFC, ifcEnumTypeString, element, geometryElement, new List<double>() { 0 }, productWrapper);
               return true;
            case IFCEntityType.IfcWall:
               WallExporter.ExportWall(exporterIFC, ifcEnumTypeString, element, null, ref geometryElement, productWrapper);
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
      /// Get width and height of door/window based on host type.
      /// </summary>
      /// <param name="instance">The instance element.</param>
      /// <param name="symbol">The type element.</param>
      /// <returns>Width and height values.</returns>
      public static (double width, double height) GetDoorWindowDimensionFromSymbol(FamilySymbol symbol, FamilyInstance instance)
      {
         // symbol width in X direction
         double width = GetMinSymbolWidth(symbol);

         double height = 0.0;
         if (instance?.Host is Wall || instance?.Host is FaceWall)
         {
            // symbol height in Z direction
            height = GetMinSymbolHeight(symbol);
         }
         else
         {
            // The height of the non-wall hosted families is measured in Y direction
            Options options = GeometryUtil.GetIFCExportGeometryOptions();
            GeometryElement windowGeom = symbol.get_Geometry(options);
            BoundingBoxXYZ bbox = windowGeom?.GetBoundingBox();
            if (bbox != null)
               height = bbox.Max.Y - bbox.Min.Y;
         }

         return (width, height);
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