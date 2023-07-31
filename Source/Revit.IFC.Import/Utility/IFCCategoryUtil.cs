//
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
      static ISet<Tuple<IFCEntityType, string>> m_EntityDontImportPredefinedType = null;

      // Used for entity types that have a simple mapping to a built-in catgory.
      static IDictionary<IFCEntityType, BuiltInCategory> m_EntityTypeToCategory = null;

      // Used for entity types and predefined type pairs that have a simple mapping to a built-in catgory.
      static IDictionary<Tuple<IFCEntityType, string>, BuiltInCategory> m_EntityPredefinedTypeToCategory = null;

      // Use for (EntityType, PredefinedType, ObjectType) that have mapping to a category.
      // This is useful if:
      // 1. There is not enough information in the Predefined Type to determine the correct category.
      // 2. The Entity is an IFCObject.
      //
      static IDictionary<Tuple<IFCEntityType, string, string>, BuiltInCategory> m_EntityPredefinedObjectTypesToCategory = null;

      // Maps entity types to the type contained in the dictionary above, to avoid duplicate instance/type mappings.
      static IDictionary<IFCEntityType, IFCEntityType> m_EntityTypeKey = null;

      // Sometimes we need to get the Category directly from the entity.  This identifies those special cases.
      static HashSet<IFCEntityType> m_RequestCategoryFromEntity = null;

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
         m_EntityPredefinedObjectTypesToCategory = null;
         m_RequestCategoryFromEntity = null;
      }

      private static void InitializeCategoryMaps()
      {
         m_EntityDontImport = new HashSet<IFCEntityType>();
         m_EntityDontImportPredefinedType = new HashSet<Tuple<IFCEntityType, string>>();
         m_EntityTypeToCategory = new Dictionary<IFCEntityType, BuiltInCategory>();
         m_EntityTypeKey = new Dictionary<IFCEntityType, IFCEntityType>();
         m_EntityPredefinedTypeToCategory = new Dictionary<Tuple<IFCEntityType, string>, BuiltInCategory>();
         m_EntityPredefinedObjectTypesToCategory = new Dictionary<Tuple<IFCEntityType, string, string>, BuiltInCategory>();
         m_RequestCategoryFromEntity = new HashSet<IFCEntityType>();

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

      private static ISet<Tuple<IFCEntityType, string>> EntityDontImportPredefinedType
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

      private static IDictionary<Tuple<IFCEntityType, string>, BuiltInCategory> EntityPredefinedTypeToCategory
      {
         get
         {
            if (m_EntityPredefinedTypeToCategory == null)
               InitializeCategoryMaps();
            return m_EntityPredefinedTypeToCategory;
         }
      }

      private static IDictionary<Tuple<IFCEntityType, string, string>, BuiltInCategory> EntityPredefinedObjectTypesToCategory
      {
         get
         {
            if (m_EntityPredefinedObjectTypesToCategory == null)
               InitializeCategoryMaps();
            return m_EntityPredefinedObjectTypesToCategory;
         }

      }

      private static HashSet<IFCEntityType> RequestCategoryFromEntity
      {
         get
         {
            if (m_RequestCategoryFromEntity == null)
            {
               InitializeCategoryMaps();
            }
            return m_RequestCategoryFromEntity;
         }
      }

      private static string GetTypeNameFromCategoryName(string categoryName)
      {
         if (categoryName == null)
            return null;

         string[] entityAndType = categoryName.Split('.');
         if (entityAndType == null)
            return null;

         return (entityAndType.Length > 1) ? entityAndType[1] : null;
      }

      private static Tuple<Color, int> GetPredefinedColorAndTransparencyForCategoryByName(string categoryName)
      {
         if (categoryName == null)
            return null;

         Func<string, string, bool> StringStartsWith = (origString, subsetString) =>
            origString.StartsWith(subsetString, StringComparison.OrdinalIgnoreCase);

         Func<string, string, bool> StringEquals = (origString, subsetString) =>
            origString.Equals(subsetString, StringComparison.OrdinalIgnoreCase);
         
         if (StringStartsWith(categoryName, "IfcDistributionPort"))
         {
            string optionalTypeValue = GetTypeNameFromCategoryName(categoryName);
            if (optionalTypeValue != null)
            {
               if (StringEquals(optionalTypeValue, "SourceAndSink"))
                  return Tuple.Create(new Color(0, 255, 0), 50);
               if (StringEquals(optionalTypeValue, "Source"))
                  return Tuple.Create(new Color(0, 0, 255), 50);
               if (StringEquals(optionalTypeValue, "Sink"))
                  return Tuple.Create(new Color(255, 0, 0), 50);
            }
            return Tuple.Create(new Color(0, 0, 0), 50);
         }

         if (StringStartsWith(categoryName, "IfcOpening"))
         {
            return Tuple.Create(new Color(255, 165, 0), 75);
         }
         
         // There are other entities that start with IfcSpace, such as IfcSpaceHeater.  But
         // we won't be creating subcategories for those, and we want to get IfcSpace and
         // IfcSpaceType with or without an optional predefined type.
         if (StringStartsWith(categoryName, "IfcSpace"))
         {
            string optionalTypeValue = GetTypeNameFromCategoryName(categoryName);
            if (optionalTypeValue != null && StringEquals(optionalTypeValue, "External"))
            {
               // A nice shade of green.
               return Tuple.Create(new Color(141, 184, 78), 75);
            }
            // Default is internal.
            // Similar to "Light Sky Blue"
            return Tuple.Create(new Color(164, 232, 232), 75); 
         }

         if (StringStartsWith(categoryName, "IfcZone"))
         {
            // (Teal Blue (Crayola)), according to Wikipedia.
            return Tuple.Create(new Color(24, 167, 181), 75);
         }

         if (StringEquals(categoryName, "Box"))
         {
            // Lemon chiffon, a lovely color for a bounding box.                             
            return Tuple.Create(new Color(255, 250, 205), 75);
         }

         return null;
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
      /// Get the strings corresponding to the entity name and the predefined type.
      /// </summary>
      /// <param name="entity">The entity.</param>
      /// <returns>The entity name, predefined type, and object type as strings.</returns>
      public static (string, string) GetEntityNameAndPredefinedType(IFCObjectDefinition entity)
      {
         IFCEntityType entityType = entity.EntityType;
         string predefinedType = entity.PredefinedType;

         GetAssociatedTypeEntityInfo(entity, out IFCEntityType? typeEntityType, out string typePredefinedType);

         if (typeEntityType.HasValue)
            entityType = typeEntityType.Value;
         if (string.IsNullOrWhiteSpace(predefinedType) && !string.IsNullOrWhiteSpace(typePredefinedType))
            predefinedType = typePredefinedType;

         string categoryName = entityType.ToString();
         return (categoryName, predefinedType);
      }

      /// <summary>
      /// Create a custom sub-category name for an entity.
      /// </summary>
      /// <param name="entity">The entity.</param>
      /// <returns>The category name.</returns>
      public static string GetCustomCategoryName(IFCObjectDefinition entity)
      {
         (string categoryName, string predefinedType) = GetEntityNameAndPredefinedType(entity);

         if (!string.IsNullOrEmpty(predefinedType))
         {
            // Object Type (if it exists) takes precedence if there is an entry in (Entity Type, Predefined Type, Object Type) table.
            // Then Predefined Type, if it exists.
            // Finally just the Entity Type.
            string objectType = GetObjectType(entity as IFCObject);
            if (!string.IsNullOrWhiteSpace(objectType) &&
                EntityPredefinedObjectTypesToCategory.ContainsKey(Tuple.Create(entity.EntityType, predefinedType, objectType)))
            {
               categoryName += "." + objectType;
            }
            else
            {
               categoryName += "." + predefinedType;
            }
         }

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
         m_EntityTypeToCategory[IFCEntityType.IfcAirToAirHeatRecovery] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcAirToAirHeatRecoveryType] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcAlarmType] = BuiltInCategory.OST_GenericModel;

         // NOTE: We do have BuiltInCategory.OST_Alignments, and you can create a DirectShape of that type, but the
         // Alignment update code isn't very happy about it.  So we will map to generic model for now.
         m_EntityTypeToCategory[IFCEntityType.IfcAlignment] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcAlignmentHorizontal] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcAlignmentSegment] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcAlignmentVertical] = BuiltInCategory.OST_GenericModel;
         
         m_EntityTypeToCategory[IFCEntityType.IfcAnnotation] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcAudioVisualAppliance] = BuiltInCategory.OST_AudioVisualDevices;
         m_EntityTypeToCategory[IFCEntityType.IfcAudioVisualApplianceType] = BuiltInCategory.OST_AudioVisualDevices;
         m_EntityTypeToCategory[IFCEntityType.IfcBeam] = BuiltInCategory.OST_StructuralFraming;
         m_EntityTypeToCategory[IFCEntityType.IfcBeamStandardCase] = BuiltInCategory.OST_StructuralFraming;
         m_EntityTypeToCategory[IFCEntityType.IfcBeamType] = BuiltInCategory.OST_StructuralFraming;
         m_EntityTypeToCategory[IFCEntityType.IfcBoiler] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcBoilerType] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcBuilding] = BuiltInCategory.OST_ProjectInformation;
         m_EntityTypeToCategory[IFCEntityType.IfcBuildingElementPart] = BuiltInCategory.OST_Parts;
         m_EntityTypeToCategory[IFCEntityType.IfcBuildingElementPartType] = BuiltInCategory.OST_Parts;
         m_EntityTypeToCategory[IFCEntityType.IfcBuildingElementProxy] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcBuildingElementProxyType] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcBuildingStorey] = BuiltInCategory.OST_Levels;
         m_EntityTypeToCategory[IFCEntityType.IfcBuiltElement] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcCableCarrierFitting] = BuiltInCategory.OST_CableTrayFitting;
         m_EntityTypeToCategory[IFCEntityType.IfcCableCarrierFittingType] = BuiltInCategory.OST_CableTrayFitting;
         m_EntityTypeToCategory[IFCEntityType.IfcCableCarrierSegment] = BuiltInCategory.OST_CableTray;
         m_EntityTypeToCategory[IFCEntityType.IfcCableCarrierSegmentType] = BuiltInCategory.OST_CableTray;
         m_EntityTypeToCategory[IFCEntityType.IfcCableSegment] = BuiltInCategory.OST_Conduit;
         m_EntityTypeToCategory[IFCEntityType.IfcCableSegmentType] = BuiltInCategory.OST_Conduit;
         m_EntityTypeToCategory[IFCEntityType.IfcChimney] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcChimneyType] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcCoil] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcCommunicationsAppliance] = BuiltInCategory.OST_DataDevices;
         m_EntityTypeToCategory[IFCEntityType.IfcCommunicationsApplianceType] = BuiltInCategory.OST_DataDevices;
         m_EntityTypeToCategory[IFCEntityType.IfcColumn] = BuiltInCategory.OST_Columns;
         m_EntityTypeToCategory[IFCEntityType.IfcColumnStandardCase] = BuiltInCategory.OST_Columns;
         m_EntityTypeToCategory[IFCEntityType.IfcController] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcControllerType] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcCovering] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcCoveringType] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcCurtainWall] = BuiltInCategory.OST_Walls;
         m_EntityTypeToCategory[IFCEntityType.IfcCurtainWallType] = BuiltInCategory.OST_Walls;
         m_EntityTypeToCategory[IFCEntityType.IfcDamper] = BuiltInCategory.OST_DuctAccessory;
         m_EntityTypeToCategory[IFCEntityType.IfcDamperType] = BuiltInCategory.OST_DuctAccessory;
         m_EntityTypeToCategory[IFCEntityType.IfcDiscreteAccessory] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcDiscreteAccessoryType] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcDistributionControlElement] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcDistributionElement] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcDistributionElementType] = BuiltInCategory.OST_GenericModel;
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
         m_EntityTypeToCategory[IFCEntityType.IfcDuctSilencer] = BuiltInCategory.OST_DuctCurves;
         m_EntityTypeToCategory[IFCEntityType.IfcDuctSilencerType] = BuiltInCategory.OST_DuctCurves;
         m_EntityTypeToCategory[IFCEntityType.IfcElectricAppliance] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcElectricApplianceType] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcElectricDistributionBoard] = BuiltInCategory.OST_ElectricalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcElectricDistributionBoardType] = BuiltInCategory.OST_ElectricalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcElementAssembly] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcEnergyConversionDevice] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcFacilityPart] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcFan] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcFanType] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcFastener] = BuiltInCategory.OST_StructConnections;
         m_EntityTypeToCategory[IFCEntityType.IfcFastenerType] = BuiltInCategory.OST_StructConnections;
         m_EntityTypeToCategory[IFCEntityType.IfcFilter] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcFilterType] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcFireSuppressionTerminal] = BuiltInCategory.OST_Sprinklers;
         m_EntityTypeToCategory[IFCEntityType.IfcFireSuppressionTerminalType] = BuiltInCategory.OST_Sprinklers;
         m_EntityTypeToCategory[IFCEntityType.IfcFlowController] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcFlowControllerType] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcFlowFitting] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcFlowMeter] = BuiltInCategory.OST_PipeAccessory;
         m_EntityTypeToCategory[IFCEntityType.IfcFlowMeterType] = BuiltInCategory.OST_PipeAccessory;
         m_EntityTypeToCategory[IFCEntityType.IfcFlowMovingDevice] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcFlowSegment] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcFlowStorageDevice] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcFlowTerminal] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcFlowTreatmentDevice] = BuiltInCategory.OST_PlumbingEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcFooting] = BuiltInCategory.OST_StructuralFoundation;
         m_EntityTypeToCategory[IFCEntityType.IfcFootingType] = BuiltInCategory.OST_StructuralFoundation;
         m_EntityTypeToCategory[IFCEntityType.IfcFurniture] = BuiltInCategory.OST_Furniture;
         m_EntityTypeToCategory[IFCEntityType.IfcFurnitureType] = BuiltInCategory.OST_Furniture;
         m_EntityTypeToCategory[IFCEntityType.IfcFurnishingElement] = BuiltInCategory.OST_Furniture;
         m_EntityTypeToCategory[IFCEntityType.IfcFurnishingElementType] = BuiltInCategory.OST_Furniture;
         m_EntityTypeToCategory[IFCEntityType.IfcGeographicElement] = BuiltInCategory.OST_Site;
         m_EntityTypeToCategory[IFCEntityType.IfcGeographicElementType] = BuiltInCategory.OST_Site;
         m_EntityTypeToCategory[IFCEntityType.IfcGrid] = BuiltInCategory.OST_Grids;
         m_EntityTypeToCategory[IFCEntityType.IfcHeatExchangerType] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcHumidifier] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcHumidifierType] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcInterceptor] = BuiltInCategory.OST_PlumbingEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcInterceptorType] = BuiltInCategory.OST_PlumbingEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcJunctionBox] = BuiltInCategory.OST_ElectricalFixtures;
         m_EntityTypeToCategory[IFCEntityType.IfcJunctionBoxType] = BuiltInCategory.OST_ElectricalFixtures;
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
         m_EntityTypeToCategory[IFCEntityType.IfcProjectionElement] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcProtectiveDeviceType] = BuiltInCategory.OST_ElectricalFixtures;
         m_EntityTypeToCategory[IFCEntityType.IfcProxy] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcPump] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcPumpType] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcRailing] = BuiltInCategory.OST_StairsRailing;
         m_EntityTypeToCategory[IFCEntityType.IfcRailingType] = BuiltInCategory.OST_StairsRailing;
         m_EntityTypeToCategory[IFCEntityType.IfcRamp] = BuiltInCategory.OST_Ramps;
         m_EntityTypeToCategory[IFCEntityType.IfcRampType] = BuiltInCategory.OST_Ramps;
         m_EntityTypeToCategory[IFCEntityType.IfcRampFlight] = BuiltInCategory.OST_Ramps;
         m_EntityTypeToCategory[IFCEntityType.IfcRampFlightType] = BuiltInCategory.OST_Ramps;

         // NOTE: We do have BuiltInCategory.OST_Alignments, and you can create a DirectShape of that type, but the
         // Alignment update code isn't very happy about it.  So we will map to generic model for now.
         m_EntityTypeToCategory[IFCEntityType.IfcReferent] = BuiltInCategory.OST_GenericModel;

         m_EntityTypeToCategory[IFCEntityType.IfcReinforcingBar] = BuiltInCategory.OST_Rebar;
         m_EntityTypeToCategory[IFCEntityType.IfcReinforcingBarType] = BuiltInCategory.OST_Rebar;
         m_EntityTypeToCategory[IFCEntityType.IfcReinforcingMesh] = BuiltInCategory.OST_FabricAreas;
         m_EntityTypeToCategory[IFCEntityType.IfcReinforcingMeshType] = BuiltInCategory.OST_FabricAreas;
         m_EntityTypeToCategory[IFCEntityType.IfcRoad] = BuiltInCategory.OST_Roads;
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
         m_EntityTypeToCategory[IFCEntityType.IfcGeotechnicalStratum] = BuiltInCategory.OST_Topography;
         m_EntityTypeToCategory[IFCEntityType.IfcSpace] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcSpaceType] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcSpaceHeater] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcSpaceHeaterType] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcStair] = BuiltInCategory.OST_Stairs;
         m_EntityTypeToCategory[IFCEntityType.IfcStairType] = BuiltInCategory.OST_Stairs;
         m_EntityTypeToCategory[IFCEntityType.IfcStairFlight] = BuiltInCategory.OST_Stairs;
         m_EntityTypeToCategory[IFCEntityType.IfcStairFlightType] = BuiltInCategory.OST_Stairs;
         m_EntityTypeToCategory[IFCEntityType.IfcStructuralCurveAction] = BuiltInCategory.OST_AnalyticalMember;
         m_EntityTypeToCategory[IFCEntityType.IfcStructuralCurveMember] = BuiltInCategory.OST_AnalyticalMember;
         m_EntityTypeToCategory[IFCEntityType.IfcStructuralCurveReaction] = BuiltInCategory.OST_AnalyticalMember;
         m_EntityTypeToCategory[IFCEntityType.IfcStructuralPointConnection] = BuiltInCategory.OST_AnalyticalNodes;
         m_EntityTypeToCategory[IFCEntityType.IfcStructuralPointReaction] = BuiltInCategory.OST_AnalyticalNodes;
         m_EntityTypeToCategory[IFCEntityType.IfcSwitchingDevice] = BuiltInCategory.OST_ElectricalFixtures;
         m_EntityTypeToCategory[IFCEntityType.IfcSwitchingDeviceType] = BuiltInCategory.OST_ElectricalFixtures;
         m_EntityTypeToCategory[IFCEntityType.IfcSystemFurnitureElement] = BuiltInCategory.OST_Furniture;
         m_EntityTypeToCategory[IFCEntityType.IfcSystemFurnitureElementType] = BuiltInCategory.OST_Furniture;
         m_EntityTypeToCategory[IFCEntityType.IfcTank] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcTankType] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcTransformer] = BuiltInCategory.OST_ElectricalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcTransformerType] = BuiltInCategory.OST_ElectricalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcTransportElement] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcTransportElementType] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcUnitaryControlElement] = BuiltInCategory.OST_MechanicalControlDevices;
         m_EntityTypeToCategory[IFCEntityType.IfcUnitaryControlElementType] = BuiltInCategory.OST_MechanicalControlDevices;
         m_EntityTypeToCategory[IFCEntityType.IfcUnitaryEquipment] = BuiltInCategory.OST_MechanicalEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcValve] = BuiltInCategory.OST_PipeAccessory;
         m_EntityTypeToCategory[IFCEntityType.IfcValveType] = BuiltInCategory.OST_PipeAccessory;
         m_EntityTypeToCategory[IFCEntityType.IfcVirtualElement] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcVoidingFeature] = BuiltInCategory.OST_GenericModel;
         m_EntityTypeToCategory[IFCEntityType.IfcWall] = BuiltInCategory.OST_Walls;
         m_EntityTypeToCategory[IFCEntityType.IfcWallStandardCase] = BuiltInCategory.OST_Walls;
         m_EntityTypeToCategory[IFCEntityType.IfcWallType] = BuiltInCategory.OST_Walls;
         m_EntityTypeToCategory[IFCEntityType.IfcWasteTerminal] = BuiltInCategory.OST_PlumbingEquipment;
         m_EntityTypeToCategory[IFCEntityType.IfcWasteTerminalType] = BuiltInCategory.OST_PlumbingEquipment;
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

         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcAnnotation, "USERDEFINED")] = BuiltInCategory.OST_Topography;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcCableSegmentType, "CABLESEGMENT")] = BuiltInCategory.OST_ElectricalEquipment;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcCableSegmentType, "CONDUCTORSEGMENT")] = BuiltInCategory.OST_ElectricalEquipment;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcColumn, "[LoadBearing]")] = BuiltInCategory.OST_StructuralColumns;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcColumn, "COLUMN")] = BuiltInCategory.OST_Columns;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcColumn, "USERDEFINED")] = BuiltInCategory.OST_Columns;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcColumn, "NOTDEFINED")] = BuiltInCategory.OST_Columns;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcColumnStandardCase, "[LoadBearing]")] = BuiltInCategory.OST_StructuralColumns;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcColumnStandardCase, "COLUMN")] = BuiltInCategory.OST_Columns;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcColumnStandardCase, "USERDEFINED")] = BuiltInCategory.OST_Columns;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcColumnStandardCase, "NOTDEFINED")] = BuiltInCategory.OST_Columns; 
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcCovering, "CEILING")] = BuiltInCategory.OST_Ceilings;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcCovering, "FLOORING")] = BuiltInCategory.OST_Floors;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcCovering, "ROOFING")] = BuiltInCategory.OST_Roofs;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcDistributionSystem, "UNDEFINED")] = BuiltInCategory.OST_PipingSystem;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcDistributionSystem, "USERDEFINED")] = BuiltInCategory.OST_PipingSystem;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcElectricAppliance, "DISHWASHER")] = BuiltInCategory.OST_PlumbingFixtures;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcElectricAppliance, "ELECTRICCOOKER")] = BuiltInCategory.OST_ElectricalFixtures;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcElectricAppliance, "FRIDGE_FREEZER")] = BuiltInCategory.OST_SpecialityEquipment;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcElectricAppliance, "TUMBLEDRYER")] = BuiltInCategory.OST_ElectricalFixtures;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcElectricAppliance, "WASHINGMACHINE")] = BuiltInCategory.OST_ElectricalFixtures;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcElectricAppliance, "USERDEFINED")] = BuiltInCategory.OST_ElectricalFixtures;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcFacilityPart, "ROADSEGMENT")] = BuiltInCategory.OST_Roads;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcFireSuppressionTerminal, "SPRINKLER")] = BuiltInCategory.OST_Sprinklers;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcFlowController, "CIRCUITBREAKER")] = BuiltInCategory.OST_ElectricalEquipment;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcFlowSegment, "CABLESEGMENT")] = BuiltInCategory.OST_ElectricalEquipment;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcFlowSegment, "CONDUCTORSEGMENT")] = BuiltInCategory.OST_ElectricalEquipment;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcFlowTerminal, "AUDIOVISUALOUTLET")] = BuiltInCategory.OST_DataDevices;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcFlowTerminal, "COMMUNICATIONSOUTLET")] = BuiltInCategory.OST_DataDevices;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcFlowTerminal, "NOTDEFINED")] = BuiltInCategory.OST_GenericModel;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcFlowTerminal, "POWEROUTLET")] = BuiltInCategory.OST_ElectricalFixtures;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcMember, "MULLION")] = BuiltInCategory.OST_CurtainWallMullions;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcMemberStandardCase, "MULLION")] = BuiltInCategory.OST_CurtainWallMullions;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcOutletType, "AUDIOVISUALOUTLET")] = BuiltInCategory.OST_DataDevices;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcOutletType, "COMMUNICATIONSOUTLET")] = BuiltInCategory.OST_DataDevices;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcOutletType, "POWEROUTLET")] = BuiltInCategory.OST_ElectricalFixtures;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcOutletType, "NOTDEFINED")] = BuiltInCategory.OST_GenericModel;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcPlate, "CURTAIN_PANEL")] = BuiltInCategory.OST_CurtainWallPanels;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcPlateStandardCase, "CURTAIN_PANEL")] = BuiltInCategory.OST_CurtainWallPanels;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcProtectiveDeviceType, "CIRCUITBREAKER")] = BuiltInCategory.OST_ElectricalEquipment;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcSlab, "BASESLAB")] = BuiltInCategory.OST_StructuralFoundation;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcSlab, "FLOOR")] = BuiltInCategory.OST_Floors;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcSlab, "LANDING")] = BuiltInCategory.OST_StairsLandings;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcSlab, "ROOF")] = BuiltInCategory.OST_Roofs;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcSlabStandardCase, "BASESLAB")] = BuiltInCategory.OST_StructuralFoundation;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcSlabStandardCase, "FLOOR")] = BuiltInCategory.OST_Floors;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcSlabStandardCase, "LANDING")] = BuiltInCategory.OST_StairsLandings;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcSlabStandardCase, "ROOF")] = BuiltInCategory.OST_Roofs; 
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcValve, "DRAWOFFCOCK")] = BuiltInCategory.OST_PlumbingFixtures;
         m_EntityPredefinedTypeToCategory[Tuple.Create(IFCEntityType.IfcValve, "FAUCET")] = BuiltInCategory.OST_PlumbingFixtures;

         // Using 3-Tuples for some Entities, PredefinedTypes, and ObjectTypes.
         m_EntityPredefinedObjectTypesToCategory[Tuple.Create(IFCEntityType.IfcAnnotation, "USERDEFINED", "CogoPoints")] = BuiltInCategory.OST_Topography;
         m_EntityPredefinedObjectTypesToCategory[Tuple.Create(IFCEntityType.IfcAnnotation, "USERDEFINED", "StringLine")] = BuiltInCategory.OST_Topography;
         m_EntityPredefinedObjectTypesToCategory[Tuple.Create(IFCEntityType.IfcAnnotation, "USERDEFINED", "BreakLine")] = BuiltInCategory.OST_Topography;

         m_RequestCategoryFromEntity.Add(IFCEntityType.IfcDistributionSystem);
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
            if (!Enum.TryParse(ifcClassName, true, out ifcClassType))
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
               alreadyPresent = m_EntityPredefinedTypeToCategory.ContainsKey(Tuple.Create(ifcClassType, ifcTypeName));

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
                  m_EntityDontImportPredefinedType.Add(Tuple.Create(ifcClassType, ifcTypeName));
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
               m_EntityTypeToCategory[ifcClassType] = category.BuiltInCategory;
            else
               m_EntityPredefinedTypeToCategory[Tuple.Create(ifcClassType, ifcTypeName)] = category.BuiltInCategory;
         }

         return true;
      }

      /// <summary>
      /// Gets the Object Type of the IFCObject.  This is needed since the caller may be using IFCObjectDescription.
      /// </summary>
      /// <param name="ifcObject">Passed in IFCObject.</param>
      /// <returns>Value of ObjectType.</returns>
      private static string GetObjectType(IFCObject ifcObject)
      {
         return (ifcObject?.ObjectType);
      }

      /// <summary>
      /// This is used in those cases where the ObjectDefinition itself will identify its category, rather than identifying
      /// every possible category in the maps above.
      /// Since the Entity actually knows its own Categories, this will keep the encapsulation there.
      /// </summary>
      /// <param name="ifcObjectDefinition">Object Definition for which category is desired.</param>
      /// <returns>ElementId representation of Category</returns>
      private static ElementId GetCategoryElementIdFromObjectDefinition(IFCObjectDefinition ifcObjectDefinition)
      {
         ElementId retVal = ElementId.InvalidElementId;
         if (ifcObjectDefinition != null)
         {
            IFCEntityType entityType;
            if (RequestCategoryFromEntity.TryGetValue(ifcObjectDefinition.EntityType, out entityType))
            {
               // Make sure that the Entity Type agrees.
               if (entityType == ifcObjectDefinition.EntityType)
               {
                  retVal = ifcObjectDefinition.GetCategoryElementId();
               }
            }
         }
         return retVal;
      }

      /// <summary>
      /// Retrieves Category for Entity Type from predefined mappings.  This will go from specific to general.
      /// If an objectType is defined, this will look into the (entityType, predefinedType, obnjectType) mapping.
      /// If that doesn't have any mapping defined, then this will look in (entityType, predefinedType) mapping.
      /// Finally, if that doesn't work, the mapping for just that entityType will be used.
      /// </summary>
      /// <param name="entityType">Enitity Type associated with IFCObjectDescription.</param>
      /// <param name="predefinedType">Predefined Type associated the IFCObjectDescription.</param>
      /// <param name="objectType">If the IFCObjectDescription is an IFCObject, this is the Object Type value.</param>
      /// <returns>ElementId corresponding to Category.</returns>
      private static ElementId GetCategoryElementId(IFCEntityType entityType, string predefinedType, string objectType = null)
      {
         BuiltInCategory catId;

         if (!string.IsNullOrWhiteSpace(objectType))
         {
            // Check to see if category is contained in Object itself.
            //

            // Check to see if (entity type, predefined type, object type) have a known mapping.
            //
            if (EntityPredefinedObjectTypesToCategory.TryGetValue(Tuple.Create(entityType, predefinedType, objectType), out catId))
               return new ElementId (catId);
         }

         // Check to see if the entity type and predefined type have a known mapping.
         // Otherwise special cases follow that could be cached later.
         if (EntityPredefinedTypeToCategory.TryGetValue(Tuple.Create(entityType, predefinedType), out catId))
            return new ElementId(catId);

         IFCEntityType key;
         if (EntityTypeKey.TryGetValue(entityType, out key) &&
             EntityPredefinedTypeToCategory.TryGetValue(Tuple.Create(key, predefinedType), out catId))
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

         if (!string.IsNullOrWhiteSpace(predefinedType) && EntityDontImportPredefinedType.Contains(Tuple.Create(type, predefinedType)))
            return false;

         return true;
      }

      private static Category GetOrCreateSubcategory(Document doc, int id, string subCategoryName, ElementId categoryId)
      {
         if (string.IsNullOrWhiteSpace(subCategoryName))
            return null;

         if (categoryId == ElementId.InvalidElementId)
            categoryId = new ElementId(BuiltInCategory.OST_GenericModel);

         Category subCategory = null;

         IDictionary<string, Category> createdSubcategories = Importer.TheCache.CreatedSubcategories;
         if (!createdSubcategories.TryGetValue(subCategoryName, out subCategory))
         {
            // Category may have been created by a previous action (probably a previous import).  Look first.
            // First check GenericModels.
            //
            try
            {
               CategoryNameMap subcategories = Importer.TheCache.GenericModelsCategory.SubCategories;
               subCategory = subcategories.get_Item(subCategoryName);
            }
            catch
            {
               subCategory = null;
            }

            // Then Topography.
            //
            if (subCategory == null)
            {
               try
               {
                  CategoryNameMap subcategories = Importer.TheCache.TopographyCategory.SubCategories;
                  subCategory = subcategories.get_Item(subCategoryName);
               }
               catch
               {
                  subCategory = null;
               }
            }

            //If not found, then create a Subcategory of the Category.
            if (subCategory == null)
            {
               Category parentCategory = Category.GetCategory(doc, categoryId);
               subCategory = Importer.TheCache.DocumentCategories.NewSubcategory(parentCategory, subCategoryName);
               CreateMaterialsForSpecialSubcategories(doc, id, subCategory, subCategoryName);

               // Copy the line weights of the parent category.
               foreach (GraphicsStyleType graphicsStyleType in Enum.GetValues(typeof(GraphicsStyleType)))
               {
                  try
                  {
                     int? lineWeight = parentCategory.GetLineWeight(graphicsStyleType);
                     if (lineWeight.HasValue)
                        subCategory.SetLineWeight(lineWeight.Value, graphicsStyleType);
                  }
                  catch
                  { 
                  }
               }
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
         Tuple<Color, int> colorAndTransparency =
            GetPredefinedColorAndTransparencyForCategoryByName(subCategoryName);

         if (colorAndTransparency == null)
            return;

         IFCMaterialInfo materialInfo = 
            IFCMaterialInfo.Create(colorAndTransparency.Item1, colorAndTransparency.Item2, null, null, ElementId.InvalidElementId);

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
      /// Determines if the category qualifies for a subcategory.
      /// Criteria:
      /// 1. Category is OST_GenericaModel.
      /// 2. Mapping exists in (EntityType, PredefinedType, ObjectType) table.
      /// </summary>
      /// <param name="categoryId">Category to test.</param>
      /// <param name="entityPredefinedObjectTypeKey">Tuple to look into Category mapping table.</param>
      /// <returns>Boolean to indicate whether we set the category or not.</returns>
      private static bool ShouldSetSubcategory (ElementId categoryId,
                                                Tuple <IFCEntityType, string, string> entityPredefinedObjectTypeKey)
      {
         if (categoryId == new ElementId(BuiltInCategory.OST_GenericModel))
            return true;

         return (entityPredefinedObjectTypeKey != null) && (EntityPredefinedObjectTypesToCategory.ContainsKey(entityPredefinedObjectTypeKey));
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
         if (IsSpecialColumnCase(entity))
            predefinedType = "[LoadBearing]";

         // Get Object Type for Entity.  This may be null if the entity is not an IFCObject.
         //
         string objectType = GetObjectType(entity as IFCObject);

         // First try on category:  if needed inspect the entity for a given category.
         //
         ElementId catElemId = GetCategoryElementIdFromObjectDefinition(entity);
         if (catElemId == ElementId.InvalidElementId)
         {
            catElemId = GetCategoryElementId(entityType, predefinedType, objectType);
         }
         ElementId genericModelId = new ElementId(BuiltInCategory.OST_GenericModel);

         // If we didn't find a category, or if we found the generic model category, try again with the IfcTypeObject, if there is one.

         if (catElemId == ElementId.InvalidElementId || catElemId == genericModelId)
         {
            // typeEntityType corresponds to an IFCTypeObject, so no objectType parameter.
            //
            if (typeEntityType.HasValue)
               catElemId = GetCategoryElementId(typeEntityType.Value, predefinedType);
         }

         Tuple <IFCEntityType, string, string> entityPredefinedObjectTypesKey = Tuple.Create (entityType, entity.PredefinedType, objectType);
         Category subCategory = null;
         if (ShouldSetSubcategory (catElemId, entityPredefinedObjectTypesKey))
         {
            string subCategoryName = GetCustomCategoryName(entity);

            subCategory = GetOrCreateSubcategory(doc, entity.Id, subCategoryName, catElemId);
            if (subCategory != null)
            {
               GraphicsStyle graphicsStyle = subCategory.GetGraphicsStyle(GraphicsStyleType.Projection);
               if (graphicsStyle != null)
                  gstyleId = graphicsStyle.Id;
            }
         }
         else if (catElemId == ElementId.InvalidElementId)
         {
            catElemId = genericModelId;

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

         if (catElemId <= ElementId.InvalidElementId)
            categoryToCheck = Importer.TheCache.DocumentCategories.get_Item((BuiltInCategory)catElemId.Value);
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
         if (repId == IFCRepresentationIdentifier.Body || repId == IFCRepresentationIdentifier.Other)
            return null;

         Category category = Importer.TheCache.GenericModelsCategory;
         if (category == null)
            return null;

         ElementId categoryId = new ElementId(BuiltInCategory.OST_GenericModel);
         string subCategoryName = repId.ToString();
         Category subCategory = GetOrCreateSubcategory(doc, entityId, subCategoryName, categoryId);
         return subCategory;
      }

      /// <summary>
      /// Determines if the ObjectDefinition is one of the special Load-bearing cases, which requires a Category of Structural Column in Revit.
      /// </summary>
      /// <param name="objectDefinition">Entity to check.</param>
      /// <returns>True if the parameter is a special load-bearing column, False otherwise.</returns>
      public static bool IsSpecialColumnCase(IFCObjectDefinition objectDefinition)
      {
         bool isSpecialCase = false;
         try
         {
            switch (objectDefinition.EntityType)
            {
               case IFCEntityType.IfcColumn:
               case IFCEntityType.IfcColumnStandardCase:
               case IFCEntityType.IfcColumnType:
                  if (IsColumnLoadBearing(objectDefinition))
                  {
                     isSpecialCase = true;
                  }
                  break;
            }
         }
         catch (Exception ex)
         {
            Importer.TheLog.LogWarning(objectDefinition.Id, ex.Message, false);
         }

         return isSpecialCase;
      }
   }
}