//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2013  Autodesk, Inc.
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

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Utility;
using System;
using System.Collections.Generic;
using System.Reflection;


namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export generic family instances and types.
   /// </summary>
   class FamilyExporterUtil
   {
      /// <summary>
      /// Checks if export type is distribution control element.
      /// </summary>
      /// <param name="exportType">
      /// The export type.
      /// </param>
      /// <returns>
      /// True if it is distribution control element, false otherwise.
      /// </returns>
      public static bool IsDistributionControlElementSubType(IFCExportInfoPair exportType)
      {
         return ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportInstance, IFCEntityType.IfcDistributionControlElement, strict: false) ||
            ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportType, IFCEntityType.IfcDistributionControlElementType, strict: false);
      }

      /// <summary>
      /// Checks if export type is distribution flow element.
      /// </summary>
      /// <param name="exportType">
      /// The export type.
      /// </param>
      /// <returns>
      /// True if it is distribution flow element, false otherwise.
      /// </returns>
      public static bool IsDistributionFlowElementSubType(IFCExportInfoPair exportType)
      {
         return ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportInstance, IFCEntityType.IfcDistributionFlowElement, strict: false) ||
            ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportType, IFCEntityType.IfcDistributionFlowElementType, strict: false);
      }

      /// <summary>
      /// Checks if export type is conversion device.
      /// </summary>
      /// <param name="exportType">
      /// The export type.
      /// </param>
      /// <returns>
      /// True if it is conversion device, false otherwise.
      /// </returns>
      public static bool IsEnergyConversionDeviceSubType(IFCExportInfoPair exportType)
      {
         return ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportInstance, IFCEntityType.IfcEnergyConversionDevice, strict: false) ||
            ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportType, IFCEntityType.IfcEnergyConversionDeviceType, strict: false);
      }

      /// <summary>
      /// Checks if export type is flow fitting.
      /// </summary>
      /// <param name="exportType">
      /// The export type.
      /// </param>
      /// <returns>
      /// True if it is flow fitting, false otherwise.
      /// </returns>
      public static bool IsFlowFittingSubType(IFCExportInfoPair exportType)
      {
         return ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportInstance, IFCEntityType.IfcFlowFitting, strict: false) ||
            ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportType, IFCEntityType.IfcFlowFittingType, strict: false);
      }

      /// <summary>
      /// Checks if export type is flow moving device.
      /// </summary>
      /// <param name="exportType">
      /// The export type.
      /// </param>
      /// <returns>
      /// True if it is flow moving device, false otherwise.
      /// </returns>
      public static bool IsFlowMovingDeviceSubType(IFCExportInfoPair exportType)
      {
         return ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportInstance, IFCEntityType.IfcFlowMovingDevice, strict: false) ||
            ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportType, IFCEntityType.IfcFlowMovingDeviceType, strict: false);
      }

      /// <summary>
      /// Checks if export type is flow segment.
      /// </summary>
      /// <param name="exportType">
      /// The export type.
      /// </param>
      /// <returns>
      /// True if it is flow segment, false otherwise.
      /// </returns>
      public static bool IsFlowSegmentSubType(IFCExportInfoPair exportType)
      {
         return ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportInstance, IFCEntityType.IfcFlowSegment, strict: false) ||
            ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportType, IFCEntityType.IfcFlowSegmentType, strict: false);
      }

      /// <summary>
      /// Checks if export type is flow storage device.
      /// </summary>
      /// <param name="exportType">
      /// The export type.
      /// </param>
      /// <returns>
      /// True if it is flow storage device, false otherwise.
      /// </returns>
      public static bool IsFlowStorageDeviceSubType(IFCExportInfoPair exportType)
      {
         return ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportInstance, IFCEntityType.IfcFlowStorageDevice, strict: false) ||
            ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportType, IFCEntityType.IfcFlowStorageDeviceType, strict: false);
      }

      /// <summary>
      /// Checks if export type is flow terminal.
      /// </summary>
      /// <param name="exportType">
      /// The export type.
      /// </param>
      /// <returns>
      /// True if it is flow terminal, false otherwise.
      /// </returns>
      public static bool IsFlowTerminalSubType(IFCExportInfoPair exportType)
      {
         return ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportInstance, IFCEntityType.IfcFlowTerminal, strict: false) ||
            ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportType, IFCEntityType.IfcFlowTerminalType, strict: false);
      }

      /// <summary>
      /// Checks if export type is flow treatment device.
      /// </summary>
      /// <param name="exportType">
      /// The export type.
      /// </param>
      /// <returns>
      /// True if it is flow treatment device, false otherwise.
      /// </returns>
      public static bool IsFlowTreatmentDeviceSubType(IFCExportInfoPair exportType)
      {
         return ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportInstance, IFCEntityType.IfcFlowTreatmentDevice, strict: false) ||
            ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportType, IFCEntityType.IfcFlowTreatmentDeviceType, strict: false);
      }

      /// <summary>
      /// Checks if export type is flow controller.
      /// </summary>
      /// <param name="exportType">
      /// The export type.
      /// </param>
      /// <returns>
      /// True if it is flow controller, false otherwise.
      /// </returns>
      public static bool IsFlowControllerSubType(IFCExportInfoPair exportType)
      {
         return ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportInstance, IFCEntityType.IfcFlowController, strict: false) ||
            ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportType, IFCEntityType.IfcFlowControllerType, strict: false);
      }

      /// <summary>
      /// Checks if export type is furnishing element.
      /// </summary>
      /// <param name="exportType">
      /// The export type.
      /// </param>
      /// <returns>
      /// True if it is furnishing element, false otherwise.
      /// </returns>
      public static bool IsFurnishingElementSubType(IFCExportInfoPair exportType)
      {
         return ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportInstance, IFCEntityType.IfcFurnishingElement, strict: false) ||
            ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportType, IFCEntityType.IfcFurnishingElementType, strict: false);
      }

      public static bool IsFurnitureSubType(IFCExportInfoPair exportType)
      {
         return ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportInstance, IFCEntityType.IfcFurniture, strict: false) ||
            ExporterCacheManager.IFCSchemaEntityTree.IsSubTypeOf(exportType.ExportType, IFCEntityType.IfcFurnitureType, strict: false);
      }

      /// <summary>
      /// Exports a generic family instance as IFC instance.
      /// </summary>
      /// <param name="type">The export type.</param>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="familyInstance">The element.</param>
      /// <param name="wrapper">The ProductWrapper.</param>
      /// <param name="setter">The PlacementSetter.</param>
      /// <param name="extraParams">The extrusion creation data.</param>
      /// <param name="typeHandle">The associated type handle.</param>
      /// <param name="instanceGUID">The guid.</param>
      /// <param name="ownerHistory">The owner history handle.</param>
      /// <param name="productRepresentation">The representation handle.</param>
      /// <param name="overrideLocalPlacement">The local placement to use instead of the one in the placement setter, if appropriate.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle ExportGenericInstance(IFCExportInfoPair type,
         ExporterIFC exporterIFC, Element familyInstance,
         ProductWrapper wrapper, PlacementSetter setter, IFCExportBodyParams extraParams,
         IFCAnyHandle typeHandle, string instanceGUID, IFCAnyHandle ownerHistory, IFCAnyHandle productRepresentation,
         IFCAnyHandle overrideLocalPlacement)
      {
         // NOTE: if overrideLocalPlacement is passed in, it is assumed that the entity to be
         // created is a child in a container.  This currently only happens when exporting curtain
         // walls, which is definitely the case.  If overrideLocalPlacement is used in other cases,
         // then this assumption will have to be reconsidered.
         bool useOverridePlacement = !IFCAnyHandleUtil.IsNullOrHasNoValue(overrideLocalPlacement);
         IFCAnyHandle localPlacementToUse =
            useOverridePlacement ? overrideLocalPlacement : setter.LocalPlacement;

         bool isChildInContainer = ExporterUtil.IsContainedInAssembly(familyInstance) || useOverridePlacement;

         ElementId roomId = ElementId.InvalidElementId;
         if (IsRoomRelated(type))
         {
            roomId = setter.UpdateRoomRelativeCoordinates(familyInstance, out localPlacementToUse);
         }

         //should remove the create method where there is no use of this handle for API methods
         //some places uses the return value of ExportGenericInstance as input parameter for API methods
         string defaultPreDefinedType = null;
         switch (type.ExportInstance)
         {
            case IFCEntityType.IfcBeam:
               defaultPreDefinedType = "BEAM";
               break;
            case IFCEntityType.IfcColumn:
               defaultPreDefinedType = "COLUMN";
               break;
            case IFCEntityType.IfcMember:
               defaultPreDefinedType = "BRACE";
               break;
            default:
               defaultPreDefinedType = "NOTDEFINED";
               break;
         }

         string preDefinedType = type.IsPredefinedTypeDefault ? defaultPreDefinedType : type.PredefinedType;

         IFCAnyHandle instanceHandle = null;
         IFCFile file = exporterIFC.GetFile();
         switch (type.ExportInstance)
         {
            case IFCEntityType.IfcBeam:
               {
                  instanceHandle = IFCInstanceExporter.CreateBeam(file, familyInstance, typeHandle, instanceGUID, ownerHistory,
                      localPlacementToUse, productRepresentation, preDefinedType);
                  break;
               }
            case IFCEntityType.IfcColumn:
               {
                  instanceHandle = IFCInstanceExporter.CreateColumn(file, familyInstance, typeHandle,
                     instanceGUID, ownerHistory, localPlacementToUse, productRepresentation,
                     preDefinedType);
                  break;
               }
            case IFCEntityType.IfcCurtainWall:
               {
                  instanceHandle = IFCInstanceExporter.CreateCurtainWall(file, familyInstance, typeHandle,
                     instanceGUID, ownerHistory, localPlacementToUse, productRepresentation,
                     preDefinedType);
                  break;
               }
            case IFCEntityType.IfcMember:
               {
                  instanceHandle = IFCInstanceExporter.CreateMember(file, familyInstance, typeHandle,
                     instanceGUID, ownerHistory, localPlacementToUse, productRepresentation,
                     preDefinedType);

                  // Register the members's IFC handle for later use by truss export.
                  ExporterCacheManager.ElementToHandleCache.Register(familyInstance.Id, instanceHandle, type);
                  break;
               }
            case IFCEntityType.IfcPlate:
               {
                  instanceHandle = IFCInstanceExporter.CreatePlate(file, familyInstance, typeHandle,
                     instanceGUID, ownerHistory, localPlacementToUse, productRepresentation,
                     preDefinedType);
                  break;
               }
            case IFCEntityType.IfcMechanicalFastener:
               {
                  double? nominalDiameter = null;
                  double? nominalLength = null;

                  (EvaluatedParameter parameter, double nominalDiameterVal) = ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "NominalDiameter");
                  if (parameter != null)
                  {
                     nominalDiameter = UnitUtil.ScaleLength(nominalDiameterVal);
                  }

                  (parameter, double nominalLengthVal) = ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "NominalLength");
                  if (parameter != null)
                  {
                     nominalLength = UnitUtil.ScaleLength(nominalLengthVal);
                  }

                  instanceHandle = IFCInstanceExporter.CreateMechanicalFastener(file,
                     familyInstance, typeHandle, instanceGUID, ownerHistory, localPlacementToUse,
                     productRepresentation, nominalDiameter, nominalLength, preDefinedType);
                  break;
               }
            case IFCEntityType.IfcRailing:
               {
                  instanceHandle = IFCInstanceExporter.CreateRailing(file, familyInstance, typeHandle,
                     instanceGUID, ownerHistory, localPlacementToUse, productRepresentation,
                     preDefinedType);
                  break;
               }
            case IFCEntityType.IfcSpace:
               {
                  IFCInternalOrExternal internalOrExternal = IFCInternalOrExternal.NotDefined;
                  if (CategoryUtil.IsElementExternal(familyInstance).HasValue)
                     internalOrExternal = CategoryUtil.IsElementExternal(familyInstance).Value ? IFCInternalOrExternal.External : IFCInternalOrExternal.Internal;

                  instanceHandle = IFCInstanceExporter.CreateSpace(exporterIFC, familyInstance,
                     instanceGUID, ownerHistory, localPlacementToUse, productRepresentation,
                     IFCElementComposition.Element, internalOrExternal, preDefinedType);
                  break;
               }
            default:
               {
                  if (type.ExportInstance != IFCEntityType.UnKnown)
                  {
                     instanceHandle = IFCInstanceExporter.CreateGenericIFCEntity(type, file, familyInstance, typeHandle, 
                        instanceGUID, ownerHistory, localPlacementToUse, productRepresentation);
                  }
                  break;
               }
         }

         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(instanceHandle) && type.ExportInstance != IFCEntityType.IfcSpace)
         {
            bool containedInSpace = !MathUtil.IsInvalidElementId(roomId);
            bool associateToLevel = !containedInSpace && !isChildInContainer;
            wrapper.AddElement(familyInstance, instanceHandle, setter, extraParams, associateToLevel, type);
            if (containedInSpace)
               ExporterCacheManager.SpaceInfoCache.RelateToSpace(roomId, instanceHandle);
         }
         return instanceHandle;
      }

      /// <summary>
      /// Gets the GUID for the IFC entity type handle associated with an element.
      /// </summary>
      /// <param name="familyInstance">The family instance, if it exists.</param>
      /// <param name="elementType">The element type to use for GUID generation if the family instance is null.</param>
      /// <returns>The GUID.</returns>
      public static string GetGUIDForFamilySymbol(FamilyInstance familyInstance, 
         ElementType elementType, IFCExportInfoPair exportType)
      {
         // GUID_TODO: Can this be called for doors and windows? If so, we need to check for
         // flipped status.
         Element elementTypeToUse = (familyInstance != null) ?
            ExporterIFCUtils.GetOriginalSymbol(familyInstance) : elementType;
         return GUIDUtil.GenerateIFCGuidFrom(elementTypeToUse, exportType);
      }

      /// <summary>
      /// Exports IFC type.
      /// </summary>
      /// <remarks>
      /// This method will override the default value of the elemId label for certain element types, and then pass it on
      /// to the generic routine.
      /// </remarks>
      /// <param name="file">The IFC file.</param>
      /// <param name="type">The export type.</param>
      /// <param name="propertySets">The property sets.</param>
      /// <param name="representationMapList">List of representations.</param>
      /// <param name="instance">The family instance.</param>
      /// <param name="elementType">The element type.</param>
      /// <param name="guid">The global id of the instance, if provided.</param>
      /// <returns>The handle.</returns>
      /// <remarks>If the guid is not provided, it will be generated from the elementType.</remarks>
      public static IFCAnyHandle ExportGenericType(IFCFile file,
         IFCExportInfoPair type,
         HashSet<IFCAnyHandle> propertySets,
         IList<IFCAnyHandle> representationMapList,
         Element instance,
         ElementType elementType,
         string guid)
      {
         IFCAnyHandle typeHandle = null;

         try
         {
            // Skip export type object that does not have associated IfcTypeObject
            if (type.ExportInstance != IFCEntityType.IfcSite && type.ExportInstance != IFCEntityType.IfcBuildingStorey && type.ExportInstance != IFCEntityType.IfcSystem
                     && type.ExportInstance != IFCEntityType.IfcZone && type.ExportInstance != IFCEntityType.IfcGroup && type.ExportInstance != IFCEntityType.IfcGrid)
            {
               string elemIdToUse = null;
               switch (type.ExportInstance)
               {
                  case IFCEntityType.IfcFurniture:
                  case IFCEntityType.IfcMember:
                  case IFCEntityType.IfcPlate:
                     {
                        elemIdToUse = NamingUtil.GetTagOverride(instance);
                        break;
                     }
               }

               if (guid == null)
                  guid = GUIDUtil.CreateGUID(elementType);

               // TODO_GUID: This is just a patch at the moment.  We should fix the callers of
               // this function so that we don't need to do this here.  Furthermore, we should
               // take into account the exportType into the guid generation.
               typeHandle = IFCInstanceExporter.CreateGenericIFCType(type, elementType, guid, file,
                  propertySets, representationMapList);
               if (!string.IsNullOrEmpty(elemIdToUse))
                  IFCAnyHandleUtil.SetAttribute(typeHandle, "Tag", elemIdToUse);
            }
         }
         catch
         {
         }

         return typeHandle;
      }

      /// <summary>
      /// Checks if export type is room related.
      /// </summary>
      /// <param name="exportType">The export type.</param>
      /// <returns>True if the export type is room related, false otherwise.</returns>
      private static bool IsRoomRelated(IFCExportInfoPair exportType)
      {
         return (IsFurnishingElementSubType(exportType) ||
            IsFurnitureSubType(exportType) ||
            IsDistributionControlElementSubType(exportType) ||
            IsDistributionFlowElementSubType(exportType) ||
            IsEnergyConversionDeviceSubType(exportType) ||
            IsFlowFittingSubType(exportType) ||
            IsFlowMovingDeviceSubType(exportType) ||
            IsFlowSegmentSubType(exportType) ||
            IsFlowStorageDeviceSubType(exportType) ||
            IsFlowTerminalSubType(exportType) ||
            IsFlowTreatmentDeviceSubType(exportType) ||
            IsFlowControllerSubType(exportType) ||
            exportType.ExportInstance == IFCEntityType.IfcBuildingElementProxy ||
            exportType.ExportType == IFCEntityType.IfcBuildingElementProxyType);
      }

      /// <summary>
      /// Generic check for the PreDefinedType string from IFC_EXPORT_PREDEFINEDTYPE*.
      /// </summary>
      /// <typeparam name="TEnum">The Enum to verify</typeparam>
      /// <param name="element">The element.</param>
      /// <param name="elementType">The optional element type.</param>
      /// <param name="ifcEnumTypeStr">Enum String if already obtained from IFC_EXPORT_ELEMENT*_AS or IFC_EXPORT_PREDEFINEDTYPE*</param>
      /// <returns>"NotDefined if the string is not defined as Enum</returns>
      public static TEnum GetPreDefinedType<TEnum>(Element element, Element elementType, string ifcEnumTypeStr) where TEnum : struct
      {
         TEnum enumValue;
         Enum.TryParse("NotDefined", true, out enumValue);

         string value = ExporterUtil.GetExportTypeFromTypeParameter(element, elementType);
         if (string.IsNullOrEmpty(value))
            value = ifcEnumTypeStr;

         if (!string.IsNullOrEmpty(value))
            Enum.TryParse(value, true, out enumValue);
         
         return enumValue;
      }

      /// <summary>
      /// Create a list of geometry objects to export from an initial list of solids and meshes, excluding invisible and not exported categories.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="solids">The list of solids, possibly empty.</param>
      /// <param name="meshes">The list of meshes, possibly empty.</param>
      /// <returns>The combined list of solids and meshes that are visible given category export settings and view visibility settings.</returns>
      public static List<GeometryObject> RemoveInvisibleSolidsAndMeshes(Document doc, ExporterIFC exporterIFC, 
         ref IList<Solid> solids, ref IList<Mesh> meshes, 
         IList<Solid> excludeSolids = null)
      {
         // Remove excluded solids from the original list of solids
         List<GeometryObject> geomObjectsIn = [.. RemoveExcludedSolid(solids, excludeSolids)];

         if (meshes != null && meshes.Count > 0)
            geomObjectsIn.AddRange(meshes);

         if (doc == null)
         {
            return geomObjectsIn;
         }

         List<GeometryObject> geomObjectsOut = [];

         View filterView = ExporterCacheManager.ExportOptionsCache.FilterViewForExport;

         foreach (GeometryObject obj in geomObjectsIn)
         {
            GraphicsStyle gStyle = doc.GetElement(obj.GraphicsStyleId) as GraphicsStyle;
            if (gStyle != null)
            {
               Category graphicsStyleCategory = gStyle.GraphicsStyleCategory;
               if (graphicsStyleCategory != null)
               {
                  bool removeObject = !ElementFilteringUtil.ShouldCategoryBeExported(graphicsStyleCategory, false);

                  if (removeObject)
                  {
                     if (obj is Solid)
                     {
                        solids.Remove(obj as Solid);
                     }
                     else if (obj is Mesh)
                     {
                        meshes.Remove(obj as Mesh);
                     }

                     continue;
                  }
               }
            }
            geomObjectsOut.Add(obj);
         }

         return geomObjectsOut;
      }

      static IList<Solid> RemoveExcludedSolid(IList<Solid> originalList, IList<Solid> excludeSolids)
      {
         IList<Solid> cleanedUpList = new List<Solid>();
         if (originalList == null)
            return cleanedUpList;

         if (excludeSolids == null || excludeSolids.Count == 0)
            return originalList;

         foreach (Solid solid in originalList)
         {
            int itemToRemove = -1;
            for (int ii = 0; ii < excludeSolids.Count; ++ii)
            {
               if (GeometryUtil.SolidsQuickEqualityCompare(solid, excludeSolids[ii]))
               {
                  itemToRemove = ii;
                  break;
               }
            }
            // If there is item to remove identified (means solid is equal to one of the exclude list), skip this solid and remove the equivalent one from the excludeSolids
            if (itemToRemove >= 0)
               excludeSolids.RemoveAt(itemToRemove);
            else
               cleanedUpList.Add(solid);
         }
         return cleanedUpList;
      }
   }
}