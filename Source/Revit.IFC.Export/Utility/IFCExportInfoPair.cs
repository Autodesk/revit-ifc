using Autodesk.Revit.DB;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Toolkit;
using System;
using System.Collections.Generic;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// A class that hold information for exporting what IfcEntity and its type pair
   /// </summary>
   public class IFCExportInfoPair
   {
      IFCEntityType m_ExportInstance = IFCEntityType.UnKnown;

      IFCEntityType m_ExportType = IFCEntityType.UnKnown;

      private string m_PredefinedType = null;

      private string m_UserdefinedType = null;

      /// <summary>
      /// The IfcEntity for export
      /// </summary>
      public IFCEntityType ExportInstance
      {
         get
         {
            return m_ExportInstance;
         }
      }

      /// <summary>
      /// The type for export
      /// </summary>
      public IFCEntityType ExportType 
      {
         get
         {
            return m_ExportType;
         }
      }

      /// <summary>
      /// Validated PredefinedType from IfcExportType (or IfcType for the old param), 
      /// or from IFC_EXPORT_ELEMENT*_AS
      /// </summary>
      public string PredefinedType
      {
         get
         {
            return m_PredefinedType;
         }
         set
         {
            if (string.IsNullOrWhiteSpace(value))
            {
               // always set to null if value is null or empty to make it possible indicate that PredefinedType is default
               m_PredefinedType = null;
               return;
            }

            string instanceName = IFCAnyHandleUtil.GetIFCEntityTypeName(m_ExportInstance);
            string newValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedType(value, instanceName);
            if (ExporterUtil.IsNotDefined(newValidatedPredefinedType))
            {
               // if the ExportType is unknown, i.e. Entity without type (e.g. IfcGrid),
               // must try the enum type from the instance type + "Type" generally, but
               // there are exceptions.
               newValidatedPredefinedType = (m_ExportType == IFCEntityType.UnKnown) ?
                  IFCValidateEntry.GetValidIFCPredefinedType(value, IfcSchemaEntityTree.GetTypeNameFromInstanceName(instanceName,
                     ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)) :
                  IFCValidateEntry.GetValidIFCPredefinedType(value, IFCAnyHandleUtil.GetIFCEntityTypeName(m_ExportType));

               // If the value is still unknown, this can come from legacy code that set the predefined type improperly.  Set it to userdefined
               // (if possible) and set the userdefined value to predefinedType value.
               if (ExporterUtil.IsNotDefined(newValidatedPredefinedType))
               {
                  newValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedType("USERDEFINED", IFCAnyHandleUtil.GetIFCEntityTypeName(m_ExportType));
                  m_UserdefinedType = value;
               }
            }

            m_PredefinedType = newValidatedPredefinedType;
         }
      }

      /// <summary>
      /// Gets a value indicating whether the <see cref="PredefinedType"/> is default.
      /// </summary>
      public bool IsPredefinedTypeDefault
      {
         get { return string.IsNullOrWhiteSpace(m_PredefinedType); }
      }

      /// <summary>
      /// Retrieves the current <see cref="PredefinedType"/>, or the <c>NOTDEFINED</c> value
      /// if the <see cref="PredefinedType"/> is default.
      /// </summary>
      /// <returns>
      /// The value of the <see cref="PredefinedType"/> property if set; otherwise the <c>NOTDEFINED</c> value.
      /// </returns>
      public string GetPredefinedTypeOrDefault()
      {
         return GetPredefinedTypeOrDefault("NOTDEFINED");
      }

      /// <summary>
      /// Retrieves the current <see cref="PredefinedType"/>, or the specified default value
      /// if the <see cref="PredefinedType"/> is default.
      /// </summary>
      /// <param name="defaultPredefinedType">
      /// A value to return if the <see cref="PredefinedType"/> is default, by default "NOTDEFINED".
      /// </param>
      /// <returns>
      /// The value of the <see cref="PredefinedType"/> property if set;
      /// otherwise the <paramref name="defaultPredefinedType"/> parameter.
      /// </returns>
      public string GetPredefinedTypeOrDefault(string defaultPredefinedType)
      {
         if (IsPredefinedTypeDefault)
         {
            return defaultPredefinedType;
         }

         return m_PredefinedType;
      }

      /// <summary>
      /// Set the <see cref="PredefinedType"/> property if property value is not initialized or "NOTDEFINED".
      /// </summary>
      /// <param name="predefinedType">A new predefined type value.</param>
      public void SetPredefinedTypeIfNotDefined(string predefinedType)
      {
         if (ExporterUtil.IsNotDefined(m_PredefinedType))
         {
            PredefinedType = predefinedType;
         }
      }

      /// <summary>
      /// The user-defined type, if the predefined type is set to USERDEFINED.
      /// </summary>
      public string UserDefinedType
      {
         get
         {
            if (string.Compare(PredefinedType, "USERDEFINED", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
               return m_UserdefinedType;
            }
            return null;
         }
         set
         {
            m_UserdefinedType = value;
         }
      }

      /// <summary>
      /// Initialization of the class
      /// </summary>
      public IFCExportInfoPair()
      {
      }

      /// <summary>
      /// Initialize the class with one entity by name.
      /// </summary>
      /// <param name="entityType">The instance or type entity class.</param>
      public IFCExportInfoPair(string entityTypeName)
      {
         IFCEntityType entityType = IFCAnyHandleUtil.GetIFCEntityTypeFromName(entityTypeName);
         if (entityType != IFCEntityType.UnKnown)
         {
            SetByType(entityType);
            return;
         }

         // We allowed in the UI to set the name of IFC2x3 entities that didn't exist.  Try this as a backup.
         entityType = IFCAnyHandleUtil.GetIFCEntityTypeFromName(entityTypeName + "Type");
         SetByType(entityType);
      }

      /// <summary>
      /// Initialize the class with one entity.
      /// </summary>
      /// <param name="entityType">The instance or type entity class.</param>
      public IFCExportInfoPair(IFCEntityType entityType)
      {
         SetByType(entityType);
      }

      /// <summary>
      /// Initialize the class with one entity.
      /// </summary>
      /// <param name="entityType">The instance or type entity class.</param>
      /// <param name="predefinedType">The optional predefined type.</param>
      public IFCExportInfoPair(IFCEntityType entityType, string predefinedType)
      {
         SetByTypeAndPredefinedType(entityType, predefinedType);
      }

      /// <summary>
      /// Initialize the class with the entity and the type.
      /// </summary>
      /// <param name="instance">The instance entity class.</param>
      /// <param name="type">The type entity class.</param>
      public IFCExportInfoPair(IFCEntityType instance, IFCEntityType type, string predefinedType)
      {
         SetValue(instance, type, predefinedType);
      }

      /// <summary>
      /// Initialize the class with the entity and optional predefinedType and userDefinedType..
      /// </summary>
      /// <param name="entity">The entity class.</param>
      /// <param name="predefinedType">The optional predefined type.</param>
      /// <param name="userDefinedType">The optional user defined type.</param>
      public IFCExportInfoPair(IFCEntityType entity, string predefinedType, string userDefinedType)
      {
         SetByTypeAndPredefinedType(entity, predefinedType);

         if (!string.IsNullOrEmpty(userDefinedType))
            UserDefinedType = userDefinedType;
      }

      /// <summary>
      /// Check whether the export information is unknown type
      /// </summary>
      public bool IsUnKnown
      {
         get { return m_ExportInstance == IFCEntityType.UnKnown; }
      }

      /// <summary>
      /// set an static class to this object with default value unknown
      /// </summary>
      public static IFCExportInfoPair UnKnown
      {
         get { return new IFCExportInfoPair(); }
      }

      /// <summary>
      /// Assign the entity and the type pair
      /// </summary>
      /// <param name="instance">the entity</param>
      /// <param name="type">the type</param>
      public void SetValue(IFCEntityType instance, IFCEntityType type, string predefinedType)
      {
         instance = CorrectEntityType(instance);
         type = CorrectEntityType(type);

         instance = ElementFilteringUtil.GetValidIFCEntityType(instance);
         m_ExportInstance = instance;

         type = ElementFilteringUtil.GetValidIFCEntityType(type);
         m_ExportType = type;

         CheckValidEntity();

         PredefinedType = predefinedType;
      }

      /// <summary>
      /// Set the export type info by given entity type.
      /// </summary>
      /// <param name="entityType">The entity type.</param>
      public void SetByType(IFCEntityType entityType)
      {
         entityType = CorrectEntityType(entityType);

         IfcSchemaEntityTree theTree = ExporterCacheManager.IFCSchemaEntityTree;
         IFCVersion ifcVersion = ExporterCacheManager.ExportOptionsCache.FileVersion;

         (IFCEntityType, IFCEntityType) matchPair = IFCAnyHandleUtil.GetMatchingPair(entityType, theTree, ifcVersion);
         m_ExportInstance = matchPair.Item1;
         m_ExportType = matchPair.Item2;

         CheckValidEntity();
      }

      /// <summary>
      /// Set the export type info by given entity type and predefined type.
      /// </summary>
      /// <param name="entityType">The entinty type.</param>
      /// <param name="predefinedTypeName">The PredefinedType attribute value.</param>
      public void SetByTypeAndPredefinedType(IFCEntityType entityType, string predefinedTypeName)
      {
         SetByType(entityType);

         PredefinedType = predefinedTypeName;
      }

      private IFCEntityType CorrectEntityType(IFCEntityType originalEntityType)
      {
         // IfcElectricDistributionBoard and IfcElectricDistributionBoardType were deprecated in IFC4x3,
         // replaced by IfcDistributionBoard / IfcDistributionBoardType.  A deprecated entity must not be
         // exported (normative rule IFC102 - Absence of deprecated entities), so for IFC4x3 and onward
         // remap them - as well as the legacy IFC2x3 IfcElectricDistributionPoint - to the current entities.
         // Their PredefinedType values (CONSUMERUNIT, DISTRIBUTIONBOARD, MOTORCONTROLCENTRE, SWITCHBOARD, ...)
         // are all valid IfcDistributionBoardTypeEnum values, so the PredefinedType stays valid after the remap.
         if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4x3)
         {
            switch (originalEntityType)
            {
               case IFCEntityType.IfcElectricDistributionPoint:
               case IFCEntityType.IfcElectricDistributionBoard:
                  return IFCEntityType.IfcDistributionBoard;
               case IFCEntityType.IfcElectricDistributionBoardType:
                  return IFCEntityType.IfcDistributionBoardType;
            }
         }

         // We allow user to input entities from any schema.  Remap as needed based on the actual schema chosen.
         if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
         {
            switch (originalEntityType)
            {
               case IFCEntityType.IfcBeamStandardCase:
                  return IFCEntityType.IfcBeam;
               case IFCEntityType.IfcColumnStandardCase:
                  return IFCEntityType.IfcColumn;
               case IFCEntityType.IfcDoorStandardCase:
                  return IFCEntityType.IfcDoor;
               case IFCEntityType.IfcDoorStyle:
                  return IFCEntityType.IfcDoorType;
               case IFCEntityType.IfcElectricDistributionPoint:
                  return IFCEntityType.IfcElectricDistributionBoard;
               case IFCEntityType.IfcElectricHeaterType:
                  return IFCEntityType.IfcSpaceHeaterType;
               case IFCEntityType.IfcGasTerminalType:
                  return IFCEntityType.IfcBurnerType;
               case IFCEntityType.IfcMemberStandardCase:
                  return IFCEntityType.IfcMember;
               case IFCEntityType.IfcOpeningStandardCase:
                  return IFCEntityType.IfcOpeningElement;
               case IFCEntityType.IfcPlateStandardCase:
                  return IFCEntityType.IfcPlate;
               case IFCEntityType.IfcProxy:
                  return IFCEntityType.IfcBuildingElementProxy;
               case IFCEntityType.IfcSlabStandardCase:
               case IFCEntityType.IfcSlabElementedCase:
                  return IFCEntityType.IfcSlab;
               case IFCEntityType.IfcWallStandardCase:
               case IFCEntityType.IfcWallElementedCase:
                  return IFCEntityType.IfcWall;
               case IFCEntityType.IfcWindowStandardCase:
                  return IFCEntityType.IfcWindow;
               case IFCEntityType.IfcWindowStyle:
                  return IFCEntityType.IfcWindowType;
            }
         }
         else
         {
            switch (originalEntityType)
            {
               case IFCEntityType.IfcAudioVisualAppliance:
                  return IFCEntityType.IfcElectricAppliance;
               case IFCEntityType.IfcBuildingElementPartType:
                  return IFCEntityType.IfcBuildingElementPart;
               case IFCEntityType.IfcBurnerType:
                  return IFCEntityType.IfcGasTerminalType;
               case IFCEntityType.IfcDoorType:
                  return IFCEntityType.IfcDoorStyle;
               case IFCEntityType.IfcElectricDistributionBoard:
                  return IFCEntityType.IfcElectricDistributionPoint;
               case IFCEntityType.IfcFootingType:
                  return IFCEntityType.IfcFooting;
               case IFCEntityType.IfcMedicalDevice:
                  return IFCEntityType.IfcBuildingElementProxy;
               case IFCEntityType.IfcMedicalDeviceType:
                  return IFCEntityType.IfcBuildingElementProxyType;
               case IFCEntityType.IfcRampType:
                  return IFCEntityType.IfcRamp;
               case IFCEntityType.IfcRoofType:
                  return IFCEntityType.IfcRoof;
               case IFCEntityType.IfcStairType:
                  return IFCEntityType.IfcStair;
               case IFCEntityType.IfcWindowType:
                  return IFCEntityType.IfcWindowStyle;
            }
         }

         return originalEntityType;
      }

      // Check valid entity and type set according to the MVD used in the export
      // Also check and correct older standardcase entities and change it without StandardCase for IFC4 and onward
      void CheckValidEntity()
      {
         // TODO: Incorporate this into the setter.
         IFCCertifiedEntitiesAndPSets certEntAndPset = ExporterCacheManager.CertifiedEntitiesAndPsetsCache;

         if (!certEntAndPset.IsValidEntityInCurrentMVD(m_ExportInstance))
         {
            if (certEntAndPset.IsValidEntityInCurrentMVD(IFCEntityType.IfcBuildingElementProxy) &&
               certEntAndPset.IsValidEntityInCurrentMVD(IFCEntityType.IfcBuildingElementProxyType))
            {
               m_ExportInstance = IFCEntityType.IfcBuildingElementProxy;
               m_ExportType = IFCEntityType.IfcBuildingElementProxyType;
            }
            else
            {
               m_ExportInstance = IFCEntityType.UnKnown;
               m_ExportType = IFCEntityType.UnKnown;
            }
         }
         else if (!certEntAndPset.IsValidEntityInCurrentMVD(m_ExportType))
         {
            m_ExportType = IFCEntityType.UnKnown;
         }
      }
   }
}
