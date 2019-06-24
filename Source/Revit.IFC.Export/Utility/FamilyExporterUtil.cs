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
         return IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportInstance.ToString(), IFCEntityType.IfcDistributionControlElement.ToString(), strict: false) ||
            IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportType.ToString(), IFCEntityType.IfcDistributionControlElementType.ToString(), strict: false);
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
         return IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportInstance.ToString(), IFCEntityType.IfcDistributionFlowElement.ToString(), strict: false) ||
            IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportType.ToString(), IFCEntityType.IfcDistributionFlowElementType.ToString(), strict: false);
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
         return IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportInstance.ToString(), IFCEntityType.IfcEnergyConversionDevice.ToString(), strict: false) ||
            IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportType.ToString(), IFCEntityType.IfcEnergyConversionDeviceType.ToString(), strict: false);
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
         return IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportInstance.ToString(), IFCEntityType.IfcFlowFitting.ToString(), strict: false) ||
            IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportType.ToString(), IFCEntityType.IfcFlowFittingType.ToString(), strict: false);
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
         return IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportInstance.ToString(), IFCEntityType.IfcFlowMovingDevice.ToString(), strict: false) ||
            IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportType.ToString(), IFCEntityType.IfcFlowMovingDeviceType.ToString(), strict: false);
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
         return IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportInstance.ToString(), IFCEntityType.IfcFlowSegment.ToString(), strict: false) ||
            IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportType.ToString(), IFCEntityType.IfcFlowSegmentType.ToString(), strict: false);
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
         return IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportInstance.ToString(), IFCEntityType.IfcFlowStorageDevice.ToString(), strict: false) ||
            IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportType.ToString(), IFCEntityType.IfcFlowStorageDeviceType.ToString(), strict: false);
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
         return IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportInstance.ToString(), IFCEntityType.IfcFlowTerminal.ToString(), strict: false) ||
            IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportType.ToString(), IFCEntityType.IfcFlowTerminalType.ToString(), strict: false);
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
         return IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportInstance.ToString(), IFCEntityType.IfcFlowTreatmentDevice.ToString(), strict: false) ||
            IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportType.ToString(), IFCEntityType.IfcFlowTreatmentDeviceType.ToString(), strict: false);
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
         return IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportInstance.ToString(), IFCEntityType.IfcFlowController.ToString(), strict: false) ||
            IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportType.ToString(), IFCEntityType.IfcFlowControllerType.ToString(), strict: false);
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
         return IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportInstance.ToString(), IFCEntityType.IfcFurnishingElement.ToString(), strict: false) ||
            IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportType.ToString(), IFCEntityType.IfcFurnishingElementType.ToString(), strict: false);
      }

      public static bool IsFurnitureSubType(IFCExportInfoPair exportType)
      {
         return IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportInstance.ToString(), IFCEntityType.IfcFurniture.ToString(), strict: false) ||
            IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportType.ToString(), IFCEntityType.IfcFurnitureType.ToString(), strict: false);
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
      /// <param name="instanceGUID">The guid.</param>
      /// <param name="ownerHistory">The owner history handle.</param>
      /// <param name="instanceName">The name.</param>
      /// <param name="instanceDescription">The description.</param>
      /// <param name="instanceObjectType">The object type.</param>
      /// <param name="productRepresentation">The representation handle.</param>
      /// <param name="instanceTag">The tag for the entity, usually based on the element id.</param>
      /// <param name="ifcEnumType">The predefined type/shape type, if any, for the object.</param>
      /// <param name="overrideLocalPlacement">The local placement to use instead of the one in the placement setter, if appropriate.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle ExportGenericInstance(IFCExportInfoPair type,
         ExporterIFC exporterIFC, Element familyInstance,
         ProductWrapper wrapper, PlacementSetter setter, IFCExtrusionCreationData extraParams,
         string instanceGUID, IFCAnyHandle ownerHistory, IFCAnyHandle productRepresentation,
         string ifcEnumType, IFCAnyHandle overrideLocalPlacement)
      {
         IFCFile file = exporterIFC.GetFile();
         Document doc = familyInstance.Document;

         bool isRoomRelated = IsRoomRelated(type);
         bool isChildInContainer = familyInstance.AssemblyInstanceId != ElementId.InvalidElementId;

         IFCAnyHandle localPlacementToUse = setter.LocalPlacement;
         ElementId roomId = ElementId.InvalidElementId;
         if (isRoomRelated)
         {
            roomId = setter.UpdateRoomRelativeCoordinates(familyInstance, out localPlacementToUse);
         }

         //should remove the create method where there is no use of this handle for API methods
         //some places uses the return value of ExportGenericInstance as input parameter for API methods
         IFCAnyHandle instanceHandle = null;
         switch (type.ExportInstance)
         {
            case IFCEntityType.IfcBeam:
               {
                  string preDefinedType = string.IsNullOrWhiteSpace(type.ValidatedPredefinedType) ? "BEAM" : type.ValidatedPredefinedType;
                  instanceHandle = IFCInstanceExporter.CreateBeam(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                      localPlacementToUse, productRepresentation, preDefinedType);
                  break;
               }
            case IFCEntityType.IfcColumn:
               {
                  string preDefinedType = string.IsNullOrWhiteSpace(type.ValidatedPredefinedType) ? "COLUMN" : type.ValidatedPredefinedType;
                  instanceHandle = IFCInstanceExporter.CreateColumn(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                     localPlacementToUse, productRepresentation, preDefinedType);
                  break;
               }
            case IFCEntityType.IfcCurtainWall:
               {
                  instanceHandle = IFCInstanceExporter.CreateCurtainWall(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                     localPlacementToUse, productRepresentation, type.ValidatedPredefinedType);
                  break;
               }
            case IFCEntityType.IfcMember:
               {
                  string preDefinedType = string.IsNullOrWhiteSpace(type.ValidatedPredefinedType) ? "BRACE" : type.ValidatedPredefinedType;
                  instanceHandle = IFCInstanceExporter.CreateMember(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                     localPlacementToUse, productRepresentation, preDefinedType);

                  // Register the members's IFC handle for later use by truss export.
                  ExporterCacheManager.ElementToHandleCache.Register(familyInstance.Id, instanceHandle, type);
                  break;
               }
            case IFCEntityType.IfcPlate:
               {
                  IFCAnyHandle localPlacement = localPlacementToUse;
                  if (overrideLocalPlacement != null)
                  {
                     isChildInContainer = true;
                     localPlacement = overrideLocalPlacement;
                  }

                  string preDefinedType = string.IsNullOrWhiteSpace(type.ValidatedPredefinedType) ? "NOTDEFINED" : type.ValidatedPredefinedType;
                  instanceHandle = IFCInstanceExporter.CreatePlate(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                      localPlacement, productRepresentation, preDefinedType);
                  break;
               }
            case IFCEntityType.IfcMechanicalFastener:
               {
                  double? nominalDiameter = null;
                  double? nominalLength = null;

                  double nominalDiameterVal, nominalLengthVal;
                  if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "NominalDiameter", out nominalDiameterVal) != null)
                     nominalDiameter = UnitUtil.ScaleLength(nominalDiameterVal);
                  if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "NominalLength", out nominalLengthVal) != null)
                     nominalLength = UnitUtil.ScaleLength(nominalLengthVal);

                  string preDefinedType = string.IsNullOrWhiteSpace(type.ValidatedPredefinedType) ? "NOTDEFINED" : type.ValidatedPredefinedType;

                  instanceHandle = IFCInstanceExporter.CreateMechanicalFastener(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                     localPlacementToUse, productRepresentation, nominalDiameter, nominalLength, preDefinedType);
                  break;
               }
            case IFCEntityType.IfcRailing:
               {
                  //string strEnumType;
                  //IFCExportInfoPair exportAs = ExporterUtil.GetExportType(exporterIFC, familyInstance, out strEnumType);
                  //if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
                  //{
                  //   instanceHandle = IFCInstanceExporter.CreateRailing(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                  //       localPlacementToUse, productRepresentation, GetPreDefinedType<Toolkit.IFC4.IFCRailingType>(familyInstance, strEnumType).ToString());
                  //}
                  //else
                  //{
                  //   instanceHandle = IFCInstanceExporter.CreateRailing(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                  //       localPlacementToUse, productRepresentation, GetPreDefinedType<Toolkit.IFCRailingType>(familyInstance, strEnumType).ToString());
                  //}
                  string preDefinedType = string.IsNullOrWhiteSpace(type.ValidatedPredefinedType) ? "NOTDEFINED" : type.ValidatedPredefinedType;
                  instanceHandle = IFCInstanceExporter.CreateRailing(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                      localPlacementToUse, productRepresentation, preDefinedType);
                  break;
               }
            case IFCEntityType.IfcSpace:
               {
                  IFCInternalOrExternal internalOrExternal = CategoryUtil.IsElementExternal(familyInstance) ? IFCInternalOrExternal.External : IFCInternalOrExternal.Internal;

                  instanceHandle = IFCInstanceExporter.CreateSpace(exporterIFC, familyInstance, instanceGUID, ownerHistory,
                      localPlacementToUse, productRepresentation, IFCElementComposition.Element, internalOrExternal);
                  break;
               }
            default:
               {
                  // !!! These entities are deprecated in IFC4 and will be made abstract in the next version. 
                  //     It is still kept as it is because if we generate an IfcBuildingElementProxy, teh connectivity will be lost
                  if (ExporterCacheManager.ExportOptionsCache.ExportAs4 &&
                         (type.ExportInstance == IFCEntityType.IfcDistributionElement ||
                          type.ExportInstance == IFCEntityType.IfcEnergyConversionDevice ||
                          type.ExportInstance == IFCEntityType.IfcFlowController ||
                          type.ExportInstance == IFCEntityType.IfcFlowFitting ||
                          type.ExportInstance == IFCEntityType.IfcFlowMovingDevice ||
                          type.ExportInstance == IFCEntityType.IfcFlowSegment ||
                          type.ExportInstance == IFCEntityType.IfcFlowStorageDevice ||
                          type.ExportInstance == IFCEntityType.IfcFlowTerminal ||
                          type.ExportInstance == IFCEntityType.IfcFlowTreatmentDevice))
                  {
                     instanceHandle = IFCInstanceExporter.CreateGenericIFCEntity(type, exporterIFC, familyInstance, instanceGUID, ownerHistory,
                        localPlacementToUse, productRepresentation);
                  }
                  else
                  {
                     if (type.ExportInstance != IFCEntityType.UnKnown)
                     {
                        instanceHandle = IFCInstanceExporter.CreateGenericIFCEntity(type, exporterIFC, familyInstance, instanceGUID, ownerHistory,
                           localPlacementToUse, productRepresentation);
                     }
                  }
                  break;
               }
         }

         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(instanceHandle))
         {
            bool containedInSpace = (roomId != ElementId.InvalidElementId);
            bool associateToLevel = containedInSpace ? false : !isChildInContainer;
            wrapper.AddElement(familyInstance, instanceHandle, setter, extraParams, associateToLevel);
            if (containedInSpace)
               ExporterCacheManager.SpaceInfoCache.RelateToSpace(roomId, instanceHandle);
         }
         return instanceHandle;
      }

      /// <summary>
      /// Exports IFC type.
      /// </summary>
      /// <remarks>
      /// This method will override the default value of the elemId label for certain element types, and then pass it on
      /// to the generic routine.
      /// </remarks>
      /// <param name="exporterIFC">The ExporterIFC class.</param>
      /// <param name="type">The export type.</param>
      /// <param name="ifcEnumType">The string value represents the IFC type.</param>
      /// <param name="guid">The guid.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The optional data type of the entity.</param>
      /// <param name="propertySets">The property sets.</param>
      /// <param name="representationMapList">List of representations.</param>
      /// <param name="elemId">The element id label.</param>
      /// <param name="typeName">The type name.</param>
      /// <param name="instance">The family instance.</param>
      /// <param name="symbol">The element type.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle ExportGenericType(ExporterIFC exporterIFC,
         IFCExportInfoPair type,
         string ifcEnumType,
         HashSet<IFCAnyHandle> propertySets,
         IList<IFCAnyHandle> representationMapList,
         Element instance,
         ElementType symbol)
      {
         IFCFile file = exporterIFC.GetFile();
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
                        elemIdToUse = NamingUtil.GetTagOverride(instance, NamingUtil.CreateIFCElementId(instance));
                        break;
                     }
               }

               typeHandle = ExportGenericTypeBase(file, type, ifcEnumType, propertySets, representationMapList, instance, symbol);
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
      /// Exports IFC type.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="type">The export type.</param>
      /// <param name="ifcEnumType">The string value represents the IFC type.</param>
      /// <param name="guid">The guid.</param>
      /// <param name="ownerHistory">The owner history handle.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The optional data type of the entity.</param>
      /// <param name="propertySets">The property sets.</param>
      /// <param name="representationMapList">List of representations.</param>
      /// <param name="elementTag">The element tag.</param>
      /// <param name="typeName">The type name.</param>
      /// <param name="instance">The family instance.</param>
      /// <param name="symbol">The element type.</param>
      /// <returns>The handle.</returns>
      private static IFCAnyHandle ExportGenericTypeBase(IFCFile file,
         IFCExportInfoPair exportType,
         string ifcEnumType,
         HashSet<IFCAnyHandle> propertySets,
         IList<IFCAnyHandle> representationMapList,
         Element instance,
         ElementType symbol)
      {
         IFCExportInfoPair exportInfo = exportType;
         string typeAsString = exportType.ExportType.ToString();

         if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
         {
            // Handle special cases for upward compatibility
            switch (exportType.ExportType)
            {
               case Common.Enums.IFCEntityType.IfcBurnerType:
                  exportInfo.SetValueWithPair(Common.Enums.IFCEntityType.IfcGasTerminalType, ifcEnumType);
                  break;
               case Common.Enums.IFCEntityType.IfcDoorType:
                  exportInfo.SetValueWithPair(Common.Enums.IFCEntityType.IfcDoorStyle, ifcEnumType);
                  break;
               case Common.Enums.IFCEntityType.IfcWindowType:
                  exportInfo.SetValueWithPair(Common.Enums.IFCEntityType.IfcWindowStyle, ifcEnumType);
                  break;
            }
         }
         else
         {
            // Handle special cases of backward compatibility
            switch (exportType.ExportType)
            {
               // For compatibility with IFC2x3 and before. IfcGasTerminalType has been removed and IfcBurnerType replaces it in IFC4
               case Common.Enums.IFCEntityType.IfcGasTerminalType:
                  exportInfo.SetValueWithPair(Common.Enums.IFCEntityType.IfcBurnerType, ifcEnumType);
                  break;
               // For compatibility with IFC2x3 and before. IfcElectricHeaterType has been removed and IfcSpaceHeaterType replaces it in IFC4
               case Common.Enums.IFCEntityType.IfcElectricHeaterType:
                  exportInfo.SetValueWithPair(Common.Enums.IFCEntityType.IfcSpaceHeaterType, ifcEnumType);
                  break;
            }
         }

         return IFCInstanceExporter.CreateGenericIFCType(exportInfo, symbol, file, propertySets, representationMapList);
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
      /// Generic check for the PreDefinedType string from either IfcExportAs with (.predefinedtype), or IfcExportType param, or legacy IfcType param
      /// </summary>
      /// <typeparam name="TEnum">The Enum to verify</typeparam>
      /// <param name="element"></param>
      /// <param name="ifcEnumTypeStr">Enum String if already obtained from IfcExportAs or IfcExportType</param>
      /// <returns>"NotDeined if the string is not defined as Enum</returns>
      public static TEnum GetPreDefinedType<TEnum>(Element element, string ifcEnumTypeStr) where TEnum : struct
      {
         TEnum enumValue;
         Enum.TryParse("NotDefined", true, out enumValue);

         string value = null;
         if ((ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcExportType", out value) == null) &&
             (ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcType", out value) == null))
            value = ifcEnumTypeStr;

         if (String.IsNullOrEmpty(value))
            return enumValue;

         Enum.TryParse(value, true, out enumValue);
         return enumValue;
      }

      private static IFCAssemblyPlace GetAssemblyPlace(Element element, string ifcEnumType)
      {
         string value = null;
         if (ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcType", out value) == null)
            value = ifcEnumType;

         if (String.IsNullOrEmpty(value))
            return IFCAssemblyPlace.NotDefined;

         string newValue = NamingUtil.RemoveSpacesAndUnderscores(value);

         if (String.Compare(newValue, "SITE", true) == 0)
            return IFCAssemblyPlace.Site;
         if (String.Compare(newValue, "FACTORY", true) == 0)
            return IFCAssemblyPlace.Factory;

         return IFCAssemblyPlace.NotDefined;
      }

      /// <summary>
      /// Create a list of geometry objects to export from an initial list of solids and meshes, excluding invisible and not exported categories.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="solids">The list of solids, possibly empty.</param>
      /// <param name="meshes">The list of meshes, possibly empty.</param>
      /// <returns>The combined list of solids and meshes that are visible given category export settings and view visibility settings.</returns>
      public static List<GeometryObject> RemoveInvisibleSolidsAndMeshes(Document doc, ExporterIFC exporterIFC, ref IList<Solid> solids, ref IList<Mesh> meshes)
      {
         List<GeometryObject> geomObjectsIn = new List<GeometryObject>();
         if (solids != null && solids.Count > 0)
            geomObjectsIn.AddRange(solids);
         if (meshes != null && meshes.Count > 0)
            geomObjectsIn.AddRange(meshes);

         List<GeometryObject> geomObjectsOut = new List<GeometryObject>();

         View filterView = ExporterCacheManager.ExportOptionsCache.FilterViewForExport;

         foreach (GeometryObject obj in geomObjectsIn)
         {
            GraphicsStyle gStyle = doc.GetElement(obj.GraphicsStyleId) as GraphicsStyle;
            if (gStyle != null)
            {
               Category graphicsStyleCategory = gStyle.GraphicsStyleCategory;
               if (graphicsStyleCategory != null)
               {
                  // Remove the geometry that is not visible
                  if (!ElementFilteringUtil.IsCategoryVisible(graphicsStyleCategory, filterView))
                  {
                     if (obj is Solid)
                        solids.Remove(obj as Solid);
                     else if (obj is Mesh)
                        meshes.Remove(obj as Mesh);

                     continue;
                  }

                  ElementId catId = graphicsStyleCategory.Id;

                  string ifcClassName = ExporterUtil.GetIFCClassNameFromExportTable(exporterIFC, catId);
                  if (!string.IsNullOrEmpty(ifcClassName))
                  {
                     bool foundName = String.Compare(ifcClassName, "Default", true) != 0;
                     if (foundName)
                     {
                        IFCExportInfoPair exportType = ElementFilteringUtil.GetExportTypeFromClassName(ifcClassName);
                        if (exportType.ExportInstance == IFCEntityType.UnKnown)
                        {
                           if (obj is Solid)
                              solids.Remove(obj as Solid);
                           else if (obj is Mesh)
                              meshes.Remove(obj as Mesh);

                           continue;
                        }
                     }
                  }
               }
            }
            geomObjectsOut.Add(obj);
         }

         return geomObjectsOut;
      }
   }
}