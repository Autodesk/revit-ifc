﻿//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
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
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Import.Data;
using Revit.IFC.Import.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Import.Utility
{
   /// <summary>
   /// Utilities for mapping IFCEntityType to categories
   /// </summary>
   public class IFCCategoryUtil
   {
      // Determines which entity types should be ignored on import.
      static ISet<IFCEntityType> m_EntityDontImport = null;

      // Determines which entity and predefined type combinations should be ignored on import.
      static ISet<KeyValuePair<IFCEntityType, string>> m_EntityDontImportPredefinedType = null;

      // Used for entity types that have a simple mapping to a built-in catgory.
      static IDictionary<IFCEntityType, BuiltInCategory> m_EntityTypeToCategory = null;

      // Used for entity types and predefined type pairs that have a simple mapping to a built-in catgory.
      static IDictionary<KeyValuePair<IFCEntityType, string>, BuiltInCategory> m_EntityPredefinedTypeToCategory = null;

      // Maps entity types to the type contained in the dictionary above, to avoid duplicate instance/type mappings.
      static IDictionary<IFCEntityType, IFCEntityType> m_EntityTypeKey = null;

      static IDictionary<string, KeyValuePair<Color, int>> m_SubCategoryNameToMaterialData = null;

      /// <summary>
      /// Clear the maps at the start of import, to force reload of options.
      /// </summary>
      public static void Clear()
      {
         m_EntityDontImport = null;
         m_EntityDontImportPredefinedType = null;
         m_EntityTypeToCategory = null;
         m_EntityTypeKey = null;
         m_EntityPredefinedTypeToCategory = null;
      }

      private static void InitializeCategoryMaps()
      {
         m_EntityDontImport = new HashSet<IFCEntityType>();
         m_EntityDontImportPredefinedType = new HashSet<KeyValuePair<IFCEntityType, string>>();
         m_EntityTypeToCategory = new Dictionary<IFCEntityType, BuiltInCategory>();
         m_EntityTypeKey = new Dictionary<IFCEntityType, IFCEntityType>();
         m_EntityPredefinedTypeToCategory = new Dictionary<KeyValuePair<IFCEntityType, string>, BuiltInCategory>();

         if (!InitFromFile())
            InitEntityTypeToCategoryMaps();
      }

      private static ISet<IFCEntityType> EntityDontImport
      {
         get
         {
            if (m_EntityDontImport == null)
               InitializeCategoryMaps();
            return m_EntityDontImport;
         }
      }

      private static ISet<KeyValuePair<IFCEntityType, string>> EntityDontImportPredefinedType
      {
         get
         {
            if (m_EntityDontImportPredefinedType == null)
               InitializeCategoryMaps();
            return m_EntityDontImportPredefinedType;
         }
      }

      private static IDictionary<IFCEntityType, BuiltInCategory> EntityTypeToCategory
      {
         get
         {
            if (m_EntityTypeToCategory == null)
               InitializeCategoryMaps();
            return m_EntityTypeToCategory;
         }
      }

      private static IDictionary<IFCEntityType, IFCEntityType> EntityTypeKey
      {
         get
         {
            if (m_EntityTypeKey == null)
               InitializeCategoryMaps();
            return m_EntityTypeKey;
         }
      }

      private static IDictionary<KeyValuePair<IFCEntityType, string>, BuiltInCategory> EntityPredefinedTypeToCategory
      {
         get
         {
            if (m_EntityPredefinedTypeToCategory == null)
               InitializeCategoryMaps();
            return m_EntityPredefinedTypeToCategory;
         }
      }

      private static IDictionary<string, KeyValuePair<Color, int>> SubCategoryNameToMaterialData
      {
         get
         {
            if (m_SubCategoryNameToMaterialData == null)
            {
               m_SubCategoryNameToMaterialData = new Dictionary<string, KeyValuePair<Color, int>>(StringComparer.InvariantCultureIgnoreCase);
               m_SubCategoryNameToMaterialData["IfcDistributionPort"] = new KeyValuePair<Color, int>(new Color(0, 0, 0), 127);
               m_SubCategoryNameToMaterialData["IfcDistributionPort.SourceAndSink"] = new KeyValuePair<Color, int>(new Color(0, 255, 0), 127);
               m_SubCategoryNameToMaterialData["IfcDistributionPort.Source"] = new KeyValuePair<Color, int>(new Color(0, 0, 255), 127);
               m_SubCategoryNameToMaterialData["IfcDistributionPort.Sink"] = new KeyValuePair<Color, int>(new Color(255, 0, 0), 127);
               m_SubCategoryNameToMaterialData["IfcOpeningElement"] = new KeyValuePair<Color, int>(new Color(255, 165, 0), 64);
               m_SubCategoryNameToMaterialData["IfcOpeningStandardCase"] = new KeyValuePair<Color, int>(new Color(255, 165, 0), 64);
               m_SubCategoryNameToMaterialData["IfcSpace"] = new KeyValuePair<Color, int>(new Color(164, 232, 232), 64); // Default is "Internal".
               m_SubCategoryNameToMaterialData["IfcSpace.Internal"] = new KeyValuePair<Color, int>(new Color(164, 232, 232), 64); // Similar to "Light Sky Blue"
               m_SubCategoryNameToMaterialData["IfcSpace.External"] = new KeyValuePair<Color, int>(new Color(141, 184, 78), 64); // A nice shade of green.
               m_SubCategoryNameToMaterialData["IfcZone"] = new KeyValuePair<Color, int>(new Color(24, 167, 181), 64); // (Teal Blue (Crayola)), according to Wikipedia.
               m_SubCategoryNameToMaterialData["Box"] = new KeyValuePair<Color, int>(new Color(255, 250, 205), 64); // Lemon chiffon, a lovely color for a bounding box.
            }
            return m_SubCategoryNameToMaterialData;
         }
      }

      /// <summary>
      /// Checks if two strings are equal ignoring case, spaces, and apostrophes.
      /// </summary>
      /// <param name="string1">The string to be compared.</param>
      /// <param name="string2">The other string to be compared.</param>
      /// <returns>True if they are equal, false otherwise.</returns>
      public static bool IsEqualIgnoringCaseSpacesApostrophe(string string1, string string2)
      {
         string nospace1 = string1.Replace(" ", null).Replace("\'", null);
         string nospace2 = string2.Replace(" ", null).Replace("\'", null);
         return (string.Compare(nospace1, nospace2, true) == 0);
      }

      private static bool IsColumnLoadBearing(IFCObjectDefinition originalEntity)
      {
         IFCObjectDefinition entity = originalEntity;

         if (entity == null)
            throw new InvalidOperationException("Function called for null entity.");

         // If the entity is an IFCTypeObject, get an associated IFCObject.  IFCColumnType doesn't
         // know if it is load bearing or not.
         // TODO: Check that all of the DefinedObjects have the same load bearing property, and warn otherwise.
         if (!(entity is IFCObject))
         {
            if (entity is IFCTypeObject)
            {
               IFCTypeObject typeObject = entity as IFCTypeObject;
               if (typeObject.DefinedObjects.Count == 0)
                  return false;
               entity = typeObject.DefinedObjects.First();
            }
            else
               return false;
         }

         IFCObject columnEntity = entity as IFCObject;
         IDictionary<string, IFCPropertySetDefinition> columnPropertySets = columnEntity.PropertySets;
         IFCPropertySetDefinition psetColumnCommonDef = null;
         if (columnPropertySets == null || (!columnPropertySets.TryGetValue("Pset_ColumnCommon", out psetColumnCommonDef)))
            return false;

         if (!(psetColumnCommonDef is IFCPropertySet))
            throw new InvalidOperationException("Invalid Pset_ColumnCommon class.");

         IFCPropertySet psetColumnCommon = psetColumnCommonDef as IFCPropertySet;
         IDictionary<string, IFCProperty> columnCommonProperties = psetColumnCommon.IFCProperties;
         IFCProperty loadBearingPropertyBase = null;
         if (columnCommonProperties == null || (!columnCommonProperties.TryGetValue("LoadBearing", out loadBearingPropertyBase)))
            return false;

         if (!(loadBearingPropertyBase is IFCSimpleProperty))
            throw new InvalidOperationException("Invalid Pset_ColumnCommon::LoadBearing property.");

         IFCSimpleProperty loadBearingProperty = loadBearingPropertyBase as IFCSimpleProperty;
         IList<IFCPropertyValue> propertyValues = loadBearingProperty.IFCPropertyValues;
         if (propertyValues == null || propertyValues.Count == 0 || !propertyValues[0].HasValue())
            return false;

         return propertyValues[0].AsBoolean();
      }

      /// <summary>
      /// Get the entity type and predefined type for the IfcTypeObject of the entity.
      /// </summary>
      /// <param name="entity">The entity.</param>
      /// <param name="typeEntityType">The IfcTypeObject entity type, if it exists.</param>
      /// <param name="typePredefinedType">The IfcTypeObject predefined type, if it exists.</param>
      /// <remarks>This function is intended to give additional information for
      /// categorization in Revit.  As such, it may ignore the type if it is generic.</remarks>
      private static void GetAssociatedTypeEntityInfo(IFCObjectDefinition entity, out IFCEntityType? typeEntityType, out string typePredefinedType)
      {
         typeEntityType = null;
         typePredefinedType = null;
         if (entity is IFCObject)
         {
            IFCObject ifcObject = entity as IFCObject;
            if (ifcObject.TypeObjects != null && ifcObject.TypeObjects.Count > 0)
            {
               IFCTypeObject typeObject = ifcObject.TypeObjects.First();
               typeEntityType = typeObject.EntityType;

               // "IfcTypeObject" and "IfcTypeProduct" are generic entity types
               // that gives us no further information.  In this case, ignore these entity
               // types and use the instance entity information.
               if (typeEntityType == IFCEntityType.IfcTypeObject ||
                  typeEntityType == IFCEntityType.IfcTypeProduct)
               {
                  typeEntityType = null;
                  return;
               }

               typePredefinedType = typeObject.PredefinedType;
            }
         }
      }

      /// <summary>
      /// Create a custom sub-category name for an entity.
      /// </summary>
      /// <param name="entity">The entity.</param>
      /// <returns>The category name.</returns>
      /// <remarks>This will also be used to populate the IfcExportAs parameter.</remarks>
      public static string GetCustomCategoryName(IFCObjectDefinition entity)
      {
         IFCEntityType entityType = entity.EntityType;
         string predefinedType = entity.PredefinedType;

         IFCEntityType? typeEntityType = null;
         string typePredefinedType = null;
         GetAssociatedTypeEntityInfo(entity, out typeEntityType, out typePredefinedType);

         if (typeEntityType.HasValue)
            entityType = typeEntityType.Value;
         if (string.IsNullOrWhiteSpace(predefinedType) && !string.IsNullOrWhiteSpace(typePredefinedType))
            predefinedType = typePredefinedType;

         string categoryName = entityType.ToString();
         if (!string.IsNullOrWhiteSpace(predefinedType))
            categoryName += "." + predefinedType;
         return categoryName;
      }

      // TODO: this is duplicated in native code and .NET code.  Use this as "Standard" for .NET.
      private static void InitEntityTypeToCategoryMaps()
      {
         // GENERAL NOTE:
         // OST_PipeSegments is an available category that has no user controlled visibility.  As such, we use the more
         // supported OST_PipeCurves instead.

         // Map from entity type to built-in category id.
         m_EntityTypeToCategory[IFCEntityType.IfcAirTerminal] = BuiltInCategory.OST_DuctTerminal;
         m_EntityTypeToCategory[IFCEntityType.IfcAirTerminalType] = BuiltInCategory.OST_DuctTerminal;
         m_EntityTypeToCategory[IFCEntityType.IfcAnnotation] = BuiltInCategory.OST_GenericAnnotation;
         m_EntityTypeToCategory[IFCEntityType.IfcBeam] = BuiltInCategory.OST_StructuralFraming;
         m_EntityTypeToCategory[IFCEntityType.IfcBeamStandardCase] = BuiltInCategory.OST_StructuralFraming;
         m_EntityTypeToCategory[IFCEntityType.IfcBeamType] = BuiltInCategory.OST_StructuralFraming;
         m_EntityTypeToCategory[IFCEntityType.IfcBoiler] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcBoilerType] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcBuildingElementPart] = BuiltInCategory.OST_Parts;
         m_EntityTypeToCategory[IFCEntityType.IfcBuildingElementPartType] = BuiltInCategory.OST_Parts;
         m_EntityTypeToCategory[IFCEntityType.IfcBuildingElementProxy] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcBuildingElementProxyType] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcCableCarrierFitting] = BuiltInCategory.OST_CableTrayFitting;
         m_EntityTypeToCategory[IFCEntityType.IfcCableCarrierFittingType] = BuiltInCategory.OST_CableTrayFitting;
         m_EntityTypeToCategory[IFCEntityType.IfcCableCarrierSegment] = BuiltInCategory.OST_CableTray;
         m_EntityTypeToCategory[IFCEntityType.IfcCableCarrierSegmentType] = BuiltInCategory.OST_CableTray;
         m_EntityTypeToCategory[IFCEntityType.IfcColumn] = BuiltInCategory.OST_Columns;
         m_EntityTypeToCategory[IFCEntityType.IfcColumnStandardCase] = BuiltInCategory.OST_Columns;
         m_EntityTypeToCategory[IFCEntityType.IfcController] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcControllerType] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcCovering] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcCoveringType] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcCurtainWall] = BuiltInCategory.OST_CurtaSystem;
         m_EntityTypeToCategory[IFCEntityType.IfcCurtainWallType] = BuiltInCategory.OST_CurtaSystem;
         m_EntityTypeToCategory[IFCEntityType.IfcDiscreteAccessory] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcDiscreteAccessoryType] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcDistributionFlowElement] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcDistributionPort] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcDoor] = BuiltInCategory.OST_Doors;
         m_EntityTypeToCategory[IFCEntityType.IfcDoorStandardCase] = BuiltInCategory.OST_Doors;
         m_EntityTypeToCategory[IFCEntityType.IfcDoorStyle] = BuiltInCategory.OST_Doors;
         m_EntityTypeToCategory[IFCEntityType.IfcDoorType] = BuiltInCategory.OST_Doors;
         m_EntityTypeToCategory[IFCEntityType.IfcDuctFitting] = BuiltInCategory.OST_DuctFitting;
         m_EntityTypeToCategory[IFCEntityType.IfcDuctFittingType] = BuiltInCategory.OST_DuctFitting;
         m_EntityTypeToCategory[IFCEntityType.IfcDuctSegment] = BuiltInCategory.OST_DuctCurves;
         m_EntityTypeToCategory[IFCEntityType.IfcDuctSegmentType] = BuiltInCategory.OST_DuctCurves;
         m_EntityTypeToCategory[IFCEntityType.IfcEnergyConversionDevice] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcFan] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcFanType] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcFastener] = BuiltInCategory.OST_StructConnections;
         m_EntityTypeToCategory[IFCEntityType.IfcFastenerType] = BuiltInCategory.OST_StructConnections;
         m_EntityTypeToCategory[IFCEntityType.IfcFlowController] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcFlowControllerType] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcFlowMovingDevice] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcFlowStorageDevice] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcFlowTerminal] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcFooting] = BuiltInCategory.OST_StructuralFoundation;
         m_EntityTypeToCategory[IFCEntityType.IfcFootingType] = BuiltInCategory.OST_StructuralFoundation;
         m_EntityTypeToCategory[IFCEntityType.IfcFurniture] = BuiltInCategory.OST_Furniture;
         m_EntityTypeToCategory[IFCEntityType.IfcFurnitureType] = BuiltInCategory.OST_Furniture;
         m_EntityTypeToCategory[IFCEntityType.IfcFurnishingElement] = BuiltInCategory.OST_Furniture;
         m_EntityTypeToCategory[IFCEntityType.IfcFurnishingElementType] = BuiltInCategory.OST_Furniture;
         m_EntityTypeToCategory[IFCEntityType.IfcGeographicElement] = BuiltInCategory.OST_Site;
         m_EntityTypeToCategory[IFCEntityType.IfcGeographicElementType] = BuiltInCategory.OST_Site;
         m_EntityTypeToCategory[IFCEntityType.IfcGrid] = BuiltInCategory.OST_Grids;
         m_EntityTypeToCategory[IFCEntityType.IfcJunctionBoxType] = BuiltInCategory.OST_ElectricalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcLamp] = BuiltInCategory.OST_LightingDevices;
         m_EntityTypeToCategory[IFCEntityType.IfcLampType] = BuiltInCategory.OST_LightingDevices;
         m_EntityTypeToCategory[IFCEntityType.IfcLightFixture] = BuiltInCategory.OST_LightingFixtures;
         m_EntityTypeToCategory[IFCEntityType.IfcLightFixtureType] = BuiltInCategory.OST_LightingFixtures;
         m_EntityTypeToCategory[IFCEntityType.IfcMechanicalFastener] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcMechanicalFastenerType] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcMember] = BuiltInCategory.OST_StructuralFraming;
         m_EntityTypeToCategory[IFCEntityType.IfcMemberStandardCase] = BuiltInCategory.OST_StructuralFraming;
         m_EntityTypeToCategory[IFCEntityType.IfcMemberType] = BuiltInCategory.OST_StructuralFraming;
         m_EntityTypeToCategory[IFCEntityType.IfcOpeningElement] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcOpeningStandardCase] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcOutletType] = BuiltInCategory.OST_ElectricalFixtures;
         m_EntityTypeToCategory[IFCEntityType.IfcPile] = BuiltInCategory.OST_StructuralFoundation;
         m_EntityTypeToCategory[IFCEntityType.IfcPileType] = BuiltInCategory.OST_StructuralFoundation;
         m_EntityTypeToCategory[IFCEntityType.IfcPipeFitting] = BuiltInCategory.OST_PipeFitting;
         m_EntityTypeToCategory[IFCEntityType.IfcPipeFittingType] = BuiltInCategory.OST_PipeFitting;
         m_EntityTypeToCategory[IFCEntityType.IfcPipeSegment] = BuiltInCategory.OST_PipeCurves;
         m_EntityTypeToCategory[IFCEntityType.IfcPipeSegmentType] = BuiltInCategory.OST_PipeCurves;
         m_EntityTypeToCategory[IFCEntityType.IfcPlate] = BuiltInCategory.OST_StructuralFraming;
         m_EntityTypeToCategory[IFCEntityType.IfcPlateStandardCase] = BuiltInCategory.OST_StructuralFraming;
         m_EntityTypeToCategory[IFCEntityType.IfcPlateType] = BuiltInCategory.OST_StructuralFraming;
         m_EntityTypeToCategory[IFCEntityType.IfcProtectiveDeviceType] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcProxy] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcPump] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcPumpType] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcRailing] = BuiltInCategory.OST_StairsRailing;
         m_EntityTypeToCategory[IFCEntityType.IfcRailingType] = BuiltInCategory.OST_StairsRailing;
         m_EntityTypeToCategory[IFCEntityType.IfcRamp] = BuiltInCategory.OST_Ramps;
         m_EntityTypeToCategory[IFCEntityType.IfcRampType] = BuiltInCategory.OST_Ramps;
         m_EntityTypeToCategory[IFCEntityType.IfcRampFlight] = BuiltInCategory.OST_Ramps;
         m_EntityTypeToCategory[IFCEntityType.IfcRampFlightType] = BuiltInCategory.OST_Ramps;
         m_EntityTypeToCategory[IFCEntityType.IfcReinforcingBar] = BuiltInCategory.OST_Rebar;
         m_EntityTypeToCategory[IFCEntityType.IfcReinforcingBarType] = BuiltInCategory.OST_Rebar;
         m_EntityTypeToCategory[IFCEntityType.IfcReinforcingMesh] = BuiltInCategory.OST_FabricAreas;
         m_EntityTypeToCategory[IFCEntityType.IfcReinforcingMeshType] = BuiltInCategory.OST_FabricAreas;
         m_EntityTypeToCategory[IFCEntityType.IfcRoof] = BuiltInCategory.OST_Roofs;
         m_EntityTypeToCategory[IFCEntityType.IfcRoofType] = BuiltInCategory.OST_Roofs;
         m_EntityTypeToCategory[IFCEntityType.IfcSanitaryTerminal] = BuiltInCategory.OST_PlumbingFixtures;
         m_EntityTypeToCategory[IFCEntityType.IfcSanitaryTerminalType] = BuiltInCategory.OST_PlumbingFixtures;
         m_EntityTypeToCategory[IFCEntityType.IfcSensor] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcSensorType] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcSite] = BuiltInCategory.OST_Site;
         m_EntityTypeToCategory[IFCEntityType.IfcSlab] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcSlabStandardCase] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcSlabType] = BuiltInCategory.OST_GenericModel;
         // Importing space geometry for the Reference intent should create a generic model, as the Rooms category should
         // be used for a real Revit room.
         m_EntityTypeToCategory[IFCEntityType.IfcSpace] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcSpaceType] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcSpaceHeater] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcSpaceHeaterType] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcStair] = BuiltInCategory.OST_Stairs;
         m_EntityTypeToCategory[IFCEntityType.IfcStairType] = BuiltInCategory.OST_Stairs;
         m_EntityTypeToCategory[IFCEntityType.IfcStairFlight] = BuiltInCategory.OST_Stairs;
         m_EntityTypeToCategory[IFCEntityType.IfcStairFlightType] = BuiltInCategory.OST_Stairs;
         m_EntityTypeToCategory[IFCEntityType.IfcSwitchingDevice] = BuiltInCategory.OST_ElectricalFixtures;
         m_EntityTypeToCategory[IFCEntityType.IfcSwitchingDeviceType] = BuiltInCategory.OST_ElectricalFixtures;
         m_EntityTypeToCategory[IFCEntityType.IfcSystemFurnitureElement] = BuiltInCategory.OST_Furniture;
         m_EntityTypeToCategory[IFCEntityType.IfcSystemFurnitureElementType] = BuiltInCategory.OST_Furniture;
         m_EntityTypeToCategory[IFCEntityType.IfcTank] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcTankType] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcTransportElement] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcTransportElementType] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcValve] = BuiltInCategory.OST_PipeAccessory;
         m_EntityTypeToCategory[IFCEntityType.IfcValveType] = BuiltInCategory.OST_PipeAccessory;
         m_EntityTypeToCategory[IFCEntityType.IfcWall] = BuiltInCategory.OST_Walls;
         m_EntityTypeToCategory[IFCEntityType.IfcWallStandardCase] = BuiltInCategory.OST_Walls;
         m_EntityTypeToCategory[IFCEntityType.IfcWallType] = BuiltInCategory.OST_Walls;
         m_EntityTypeToCategory[IFCEntityType.IfcWindow] = BuiltInCategory.OST_Windows;
         m_EntityTypeToCategory[IFCEntityType.IfcWindowStandardCase] = BuiltInCategory.OST_Windows;
         m_EntityTypeToCategory[IFCEntityType.IfcWindowStyle] = BuiltInCategory.OST_Windows;
         m_EntityTypeToCategory[IFCEntityType.IfcWindowType] = BuiltInCategory.OST_Windows;
         m_EntityTypeToCategory[IFCEntityType.IfcZone] = BuiltInCategory.OST_GenericModel;

         // Entity type/predefined type pairs to categories.
         m_EntityTypeKey[IFCEntityType.IfcColumnType] = IFCEntityType.IfcColumn;
         m_EntityTypeKey[IFCEntityType.IfcCoveringType] = IFCEntityType.IfcCovering;
         m_EntityTypeKey[IFCEntityType.IfcElectricApplianceType] = IFCEntityType.IfcElectricAppliance;
         m_EntityTypeKey[IFCEntityType.IfcFireSuppressionTerminalType] = IFCEntityType.IfcFireSuppressionTerminal;
         m_EntityTypeKey[IFCEntityType.IfcMemberType] = IFCEntityType.IfcMember;
         m_EntityTypeKey[IFCEntityType.IfcPlateType] = IFCEntityType.IfcPlate;
         m_EntityTypeKey[IFCEntityType.IfcSlabType] = IFCEntityType.IfcSlab;
         m_EntityTypeKey[IFCEntityType.IfcValveType] = IFCEntityType.IfcValve;

         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcCableSegmentType, "CABLESEGMENT")] = BuiltInCategory.OST_ElectricalEquipment;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcCableSegmentType, "CONDUCTORSEGMENT")] = BuiltInCategory.OST_ElectricalEquipment;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcColumn, "[LoadBearing]")] = BuiltInCategory.OST_StructuralColumns;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcColumn, "COLUMN")] = BuiltInCategory.OST_Columns;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcColumn, "USERDEFINED")] = BuiltInCategory.OST_Columns;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcColumn, "NOTDEFINED")] = BuiltInCategory.OST_Columns;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcColumnStandardCase, "[LoadBearing]")] = BuiltInCategory.OST_StructuralColumns;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcColumnStandardCase, "COLUMN")] = BuiltInCategory.OST_Columns;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcColumnStandardCase, "USERDEFINED")] = BuiltInCategory.OST_Columns;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcColumnStandardCase, "NOTDEFINED")] = BuiltInCategory.OST_Columns; m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcCovering, "CEILING")] = BuiltInCategory.OST_Ceilings;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcCovering, "FLOORING")] = BuiltInCategory.OST_Floors;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcCovering, "ROOFING")] = BuiltInCategory.OST_Roofs;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcElectricAppliance, "DISHWASHER")] = BuiltInCategory.OST_PlumbingFixtures;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcElectricAppliance, "ELECTRICCOOKER")] = BuiltInCategory.OST_ElectricalFixtures;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcElectricAppliance, "FRIDGE_FREEZER")] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcElectricAppliance, "TUMBLEDRYER")] = BuiltInCategory.OST_ElectricalFixtures;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcElectricAppliance, "WASHINGMACHINE")] = BuiltInCategory.OST_ElectricalFixtures;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcElectricAppliance, "USERDEFINED")] = BuiltInCategory.OST_ElectricalFixtures;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcFireSuppressionTerminal, "SPRINKLER")] = BuiltInCategory.OST_Sprinklers;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcFlowController, "CIRCUITBREAKER")] = BuiltInCategory.OST_ElectricalEquipment;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcFlowSegment, "CABLESEGMENT")] = BuiltInCategory.OST_ElectricalEquipment;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcFlowSegment, "CONDUCTORSEGMENT")] = BuiltInCategory.OST_ElectricalEquipment;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcFlowTerminal, "AUDIOVISUALOUTLET")] = BuiltInCategory.OST_DataDevices;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcFlowTerminal, "COMMUNICATIONSOUTLET")] = BuiltInCategory.OST_DataDevices;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcFlowTerminal, "NOTDEFINED")] = BuiltInCategory.OST_GenericModel;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcFlowTerminal, "POWEROUTLET")] = BuiltInCategory.OST_ElectricalFixtures;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcMember, "MULLION")] = BuiltInCategory.OST_CurtainWallMullions;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcMemberStandardCase, "MULLION")] = BuiltInCategory.OST_CurtainWallMullions;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcOutletType, "AUDIOVISUALOUTLET")] = BuiltInCategory.OST_DataDevices;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcOutletType, "COMMUNICATIONSOUTLET")] = BuiltInCategory.OST_DataDevices;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcOutletType, "POWEROUTLET")] = BuiltInCategory.OST_ElectricalFixtures;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcOutletType, "NOTDEFINED")] = BuiltInCategory.OST_GenericModel;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcPlate, "CURTAIN_PANEL")] = BuiltInCategory.OST_CurtainWallPanels;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcPlateStandardCase, "CURTAIN_PANEL")] = BuiltInCategory.OST_CurtainWallPanels;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcProtectiveDeviceType, "CIRCUITBREAKER")] = BuiltInCategory.OST_ElectricalEquipment;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcSlab, "BASESLAB")] = BuiltInCategory.OST_StructuralFoundation;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcSlab, "FLOOR")] = BuiltInCategory.OST_Floors;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcSlab, "LANDING")] = BuiltInCategory.OST_StairsLandings;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcSlab, "ROOF")] = BuiltInCategory.OST_Roofs;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcSlabStandardCase, "BASESLAB")] = BuiltInCategory.OST_StructuralFoundation;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcSlabStandardCase, "FLOOR")] = BuiltInCategory.OST_Floors;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcSlabStandardCase, "LANDING")] = BuiltInCategory.OST_StairsLandings;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcSlabStandardCase, "ROOF")] = BuiltInCategory.OST_Roofs; m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcValve, "DRAWOFFCOCK")] = BuiltInCategory.OST_PlumbingFixtures;
         m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(IFCEntityType.IfcValve, "FAUCET")] = BuiltInCategory.OST_PlumbingFixtures;
      }

      private static bool InitFromFile()
      {
         string fileName = IFCImportFile.TheFile.Document.Application.ImportIFCCategoryTable;
         StreamReader inFile = null;

         if (!string.IsNullOrWhiteSpace(fileName))
         {
            try
            {
               inFile = new StreamReader(fileName);
            }
            catch
            {
               return false;
            }
         }

         if (inFile == null)
            return false;

         IDictionary<string, Category> createdSubcategories = Importer.TheCache.CreatedSubcategories;

         while (true)
         {
            string nextLine = inFile.ReadLine();
            if (nextLine == null)
               break;

            // Skip empty line.
            if (string.IsNullOrWhiteSpace(nextLine))
               continue;

            // Skip comment.
            if (nextLine.First() == '#')
               continue;

            string[] fields = nextLine.Split('\t');
            int numFields = fields.Count();

            // Too few fields, ignore.
            if (numFields < 3)
               continue;

            string ifcClassName = fields[0];
            if (string.IsNullOrWhiteSpace(ifcClassName))
               continue;
            IFCEntityType ifcClassType;
            if (!Enum.TryParse<IFCEntityType>(ifcClassName, true, out ifcClassType))
            {
               Importer.TheLog.LogWarning(-1, "Unknown class name in IFC entity to category mapping file: " + ifcClassName, true);
               continue;
            }

            bool hasTypeName = (numFields == 4);
            string ifcTypeName = null;
            if (hasTypeName)
               ifcTypeName = fields[1];

            // Skip entries we have already seen
            bool alreadyPresent = false;
            if (string.IsNullOrWhiteSpace(ifcTypeName))
               alreadyPresent = m_EntityTypeToCategory.ContainsKey(ifcClassType);
            else
               alreadyPresent = m_EntityPredefinedTypeToCategory.ContainsKey(new KeyValuePair<IFCEntityType, string>(ifcClassType, ifcTypeName));

            if (alreadyPresent)
               continue;

            int categoryField = hasTypeName ? 2 : 1;
            int subCategoryField = hasTypeName ? 3 : 2;

            // If set to "Don't Import", or some variant, ignore this entity.
            string categoryName = fields[categoryField];
            if (IsEqualIgnoringCaseSpacesApostrophe(categoryName, "DontImport"))
            {
               if (string.IsNullOrWhiteSpace(ifcTypeName))
                  m_EntityDontImport.Add(ifcClassType);
               else
                  m_EntityDontImportPredefinedType.Add(new KeyValuePair<IFCEntityType, string>(ifcClassType, ifcTypeName));
               continue;
            }

            // TODO: Use enum name, not category name, in file.
            ElementId categoryId = ElementId.InvalidElementId;
            Category category = null;

            try
            {
               category = Importer.TheCache.DocumentCategories.get_Item(categoryName);
               categoryId = category.Id;
            }
            catch
            {
               Importer.TheLog.LogWarning(-1, "Unknown top-level category in IFC entity to category mapping file: " + categoryName, true);
               continue;
            }

            string subCategoryName = null;
            if (numFields > 2)
            {
               subCategoryName = fields[subCategoryField];
               if (!string.IsNullOrWhiteSpace(subCategoryName))
               {
                  CategoryNameMap subCategories = category.SubCategories;

                  try
                  {
                     Category subcategory = subCategories.get_Item(subCategoryName);
                     categoryId = subcategory.Id;
                  }
                  catch
                  {
                     if (category.CanAddSubcategory)
                     {
                        Category subcategory = null;
                        if (!createdSubcategories.TryGetValue(subCategoryName, out subcategory))
                        {
                           subcategory = Importer.TheCache.DocumentCategories.NewSubcategory(category, subCategoryName);
                           createdSubcategories[subCategoryName] = subcategory;
                        }
                        categoryId = subcategory.Id;
                     }
                     else
                        Importer.TheLog.LogWarning(-1, "Can't add sub-category " + subCategoryName + " to top-level category " + categoryName + " in IFC entity to category mapping file.", true);
                  }
               }
            }

            if (string.IsNullOrWhiteSpace(ifcTypeName))
               m_EntityTypeToCategory[ifcClassType] = (BuiltInCategory)category.Id.IntegerValue;
            else
               m_EntityPredefinedTypeToCategory[new KeyValuePair<IFCEntityType, string>(ifcClassType, ifcTypeName)] = (BuiltInCategory)category.Id.IntegerValue;
         }

         return true;
      }

      private static ElementId GetCategoryElementId(IFCEntityType entityType, string predefinedType)
      {
         BuiltInCategory catId;

         // Check to see if the entity type and predefined type have a known mapping.
         // Otherwise special cases follow that could be cached later.
         if (EntityPredefinedTypeToCategory.TryGetValue(new KeyValuePair<IFCEntityType, string>(entityType, predefinedType), out catId))
            return new ElementId(catId);

         IFCEntityType key;
         if (EntityTypeKey.TryGetValue(entityType, out key) &&
             EntityPredefinedTypeToCategory.TryGetValue(new KeyValuePair<IFCEntityType, string>(key, predefinedType), out catId))
            return new ElementId(catId);

         // Check if there is a simple entity type to category mapping, and return if found.
         if (EntityTypeToCategory.TryGetValue(entityType, out catId))
            return new ElementId(catId);

         return ElementId.InvalidElementId;
      }

      /// <summary>
      /// Determines if a entity, with an optional predefined type, should be imported.
      /// </summary>
      /// <param name="type">The entity type.</param>
      /// <param name="predefinedType">The predefined type.</param>
      /// <returns>True if it is being imported, false otherwise.</returns>
      public static bool CanImport(IFCEntityType type, string predefinedType)
      {
         if (EntityDontImport.Contains(type))
            return false;

         if (!string.IsNullOrWhiteSpace(predefinedType) && EntityDontImportPredefinedType.Contains(new KeyValuePair<IFCEntityType, string>(type, predefinedType)))
            return false;

         return true;
      }

      private static Category GetOrCreateSubcategory(Document doc, int id, string subCategoryName)
      {
         if (string.IsNullOrWhiteSpace(subCategoryName))
            return null;

         Category subCategory = null;

         IDictionary<string, Category> createdSubcategories = Importer.TheCache.CreatedSubcategories;
         if (!createdSubcategories.TryGetValue(subCategoryName, out subCategory))
         {
            // Category may have been created by a previous action (probably a previous import).  Look first.
            try
            {
               CategoryNameMap subCategories = Importer.TheCache.GenericModelsCategory.SubCategories;
               subCategory = subCategories.get_Item(subCategoryName);
            }
            catch
            {
               subCategory = null;
            }

            if (subCategory == null)
            {
               subCategory = Importer.TheCache.DocumentCategories.NewSubcategory(Importer.TheCache.GenericModelsCategory, subCategoryName);
               CreateMaterialsForSpecialSubcategories(doc, id, subCategory, subCategoryName);
            }

            createdSubcategories[subCategoryName] = subCategory;
         }

         return subCategory;
      }

      /// <summary>
      /// Set a transparent material (by default) for spaces and openings.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="category">The category class.</param>
      /// <param name="id">The id of the generating entity.</param>
      /// <param name="subCategoryName">The name of the created (sub-)category.</param>
      private static void CreateMaterialsForSpecialSubcategories(Document doc, int id, Category category, string subCategoryName)
      {
         // A pair of material color (key) and transparency (value).
         KeyValuePair<Color, int> colorAndTransparency = new KeyValuePair<Color, int>(null, 127);

         if (!SubCategoryNameToMaterialData.TryGetValue(subCategoryName, out colorAndTransparency))
         {
            // Search for just the entity part of the subcategory name.
            int dot = subCategoryName.IndexOf('.');
            if (dot > 0 && dot < subCategoryName.Length)
            {
               string entityNameOnly = subCategoryName.Substring(dot);
               if (!SubCategoryNameToMaterialData.TryGetValue(subCategoryName, out colorAndTransparency))
                  return;
            }
         }

         IFCMaterialInfo materialInfo = null;
         if (colorAndTransparency.Key != null)
            materialInfo = IFCMaterialInfo.Create(colorAndTransparency.Key, colorAndTransparency.Value, null, null, ElementId.InvalidElementId);

         if (materialInfo != null)
         {
            ElementId createdElementId = IFCMaterial.CreateMaterialElem(doc, id, subCategoryName, materialInfo);
            if (createdElementId != ElementId.InvalidElementId)
            {
               Material material = doc.GetElement(createdElementId) as Material;
               category.Material = material;
            }
         }
      }

      /// <summary>
      /// Get the top-level Built-in category id for an IFC entity.
      /// </summary>
      /// <param name="doc">The doument.</param>
      /// <param name="entity">The entity.</param>
      /// <param name="gstyleId">The graphics style, if the returned category is not top level.  This allows shapes to have their visibility controlled by the sub-category.</param>
      /// <returns>The element id for the built-in category.</returns>
      public static ElementId GetCategoryIdForEntity(Document doc, IFCObjectDefinition entity, out ElementId gstyleId)
      {
         gstyleId = ElementId.InvalidElementId;

         IFCEntityType entityType = entity.EntityType;

         IFCEntityType? typeEntityType = null;
         string typePredefinedType = null;
         GetAssociatedTypeEntityInfo(entity, out typeEntityType, out typePredefinedType);

         // Use the IfcTypeObject predefined type if the IfcElement predefined type is either null, empty, white space, or not defined.
         string predefinedType = entity.PredefinedType;
         if ((string.IsNullOrWhiteSpace(predefinedType) || (string.Compare(predefinedType, "NOTDEFINED", true) == 0)) &&
             !string.IsNullOrWhiteSpace(typePredefinedType))
            predefinedType = typePredefinedType;

         // Set "special" predefined types 
         try
         {
            switch (entityType)
            {
               case IFCEntityType.IfcColumn:
               case IFCEntityType.IfcColumnStandardCase:
               case IFCEntityType.IfcColumnType:
                  if (IsColumnLoadBearing(entity))
                     predefinedType = "[LoadBearing]";
                  break;
            }
         }
         catch (Exception ex)
         {
            Importer.TheLog.LogWarning(entity.Id, ex.Message, false);
         }

         ElementId catElemId = GetCategoryElementId(entityType, predefinedType);

         // If we didn't find a category, or if we found the generic model category, try again with the IfcTypeObject, if there is one.
         if (catElemId == ElementId.InvalidElementId || catElemId.IntegerValue == (int)BuiltInCategory.OST_GenericModel)
         {
            if (typeEntityType.HasValue)
               catElemId = GetCategoryElementId(typeEntityType.Value, predefinedType);
         }

         Category subCategory = null;
         if (catElemId.IntegerValue == (int)BuiltInCategory.OST_GenericModel)
         {
            string subCategoryName = GetCustomCategoryName(entity);

            subCategory = GetOrCreateSubcategory(doc, entity.Id, subCategoryName);
            if (subCategory != null)
            {
               GraphicsStyle graphicsStyle = subCategory.GetGraphicsStyle(GraphicsStyleType.Projection);
               if (graphicsStyle != null)
                  gstyleId = graphicsStyle.Id;
            }
         }
         else if (catElemId == ElementId.InvalidElementId)
         {
            catElemId = new ElementId(BuiltInCategory.OST_GenericModel);

            // Top level entities that are OK to be here.
            if (entityType != IFCEntityType.IfcProject &&
                entityType != IFCEntityType.IfcBuilding &&
                entityType != IFCEntityType.IfcBuildingStorey &&
                entityType != IFCEntityType.IfcElementAssembly &&
                entityType != IFCEntityType.IfcSystem)
            {
               string msg = "Setting IFC entity ";
               if (string.IsNullOrWhiteSpace(predefinedType))
                  msg = entityType.ToString();
               else
                  msg = entityType.ToString() + "." + predefinedType;

               if (typeEntityType.HasValue)
                  msg += " (" + typeEntityType.Value.ToString() + ")";

               msg += " to Generic Models.";
               Importer.TheLog.LogWarning(entity.Id, msg, true);
            }
         }

         Category categoryToCheck = null;

         if (catElemId.IntegerValue < 0)
            categoryToCheck = Importer.TheCache.DocumentCategories.get_Item((BuiltInCategory)catElemId.IntegerValue);
         else
            categoryToCheck = subCategory;

         if (categoryToCheck != null)
         {
            // We'll assume that a negative value means a built-in category.  It may still be a sub-category, in which case we need to get the parent category and assign the gstyle.
            // We could optimize this, but this is safer.
            Category parentCategory = categoryToCheck.Parent;
            if (parentCategory != null)
               catElemId = parentCategory.Id;

            // Not already set by subcategory.
            if (gstyleId == ElementId.InvalidElementId)
            {
               GraphicsStyle graphicsStyle = categoryToCheck.GetGraphicsStyle(GraphicsStyleType.Projection);
               if (graphicsStyle != null)
                  gstyleId = graphicsStyle.Id;
            }
         }

         return catElemId;
      }

      /// <summary>
      /// Get or create the sub-category for a representation other than the body representation for a particular category.
      /// </summary>
      /// <param name="doc">The doument.</param>
      /// <param name="entityId">The entity id.</param>
      /// <param name="repId">The representation identifier.</param>
      /// <param name="category">The top-level category id for the element.</param>
      /// <returns>The sub-category.  This allows shapes to have their visibility controlled by the sub-category.</param></returns>
      public static Category GetSubCategoryForRepresentation(Document doc, int entityId, IFCRepresentationIdentifier repId)
      {
         if (repId == IFCRepresentationIdentifier.Body || repId == IFCRepresentationIdentifier.Unhandled)
            return null;

         Category category = Importer.TheCache.GenericModelsCategory;
         if (category == null)
            return null;

         string subCategoryName = repId.ToString();
         Category subCategory = GetOrCreateSubcategory(doc, entityId, subCategoryName);
         return subCategory;
      }
   }
}