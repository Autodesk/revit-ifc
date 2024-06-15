using System;
using System.Collections.Generic;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Export.Toolkit;
using Autodesk.Revit.DB;

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
            CheckValidEntity();
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
            CheckValidEntity();
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
            }

            string newValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedType(value, m_ExportInstance.ToString());
            if (ExporterUtil.IsNotDefined(newValidatedPredefinedType))
            {
               // if the ExportType is unknown, i.e. Entity without type (e.g. IfcGrid),
               // must try the enum type from the instance type + "Type" generally, but
               // there are exceptions.
               if (m_ExportType == IFCEntityType.UnKnown)
               {
                  newValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedType(value, IfcSchemaEntityTree.GetTypeNameFromInstanceName(m_ExportInstance.ToString()));
               }
               else
               {
                  newValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedType(value, m_ExportType.ToString());
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
      public IFCExportInfoPair(IFCEntityType entity, string predefinedType = null, string userDefinedType = null)
      {
         if (!string.IsNullOrEmpty(predefinedType))
            PredefinedType = predefinedType;

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
         instance = ElementFilteringUtil.GetValidIFCEntityType(instance);
         m_ExportInstance = instance;

         type = ElementFilteringUtil.GetValidIFCEntityType(type);
         m_ExportType = type;

         PredefinedType = predefinedType;
      }

      /// <summary>
      /// Set the export type info by given entity type.
      /// </summary>
      /// <param name="entityType">The entinty type.</param>
      public void SetByType(IFCEntityType entityType)
      {
         SetByTypeName(entityType.ToString());
      }

      /// <summary>
      /// Set the export type info by given entity type name.
      /// </summary>
      /// <param name="entityTypeName">The entinty type name.</param>
      public void SetByTypeName(string entityTypeName)
      {
         IFCVersion ifcVersion = ExporterCacheManager.ExportOptionsCache.FileVersion;
         IfcSchemaEntityTree theTree = IfcSchemaEntityTree.GetEntityDictFor(ifcVersion);
         int typeLen = 4;
         bool isType = entityTypeName.EndsWith("Type", StringComparison.CurrentCultureIgnoreCase);
         if (!isType)
         {
            if (entityTypeName.Equals("IfcDoorStyle", StringComparison.InvariantCultureIgnoreCase)
               || entityTypeName.Equals("IfcWindowStyle", StringComparison.InvariantCultureIgnoreCase))
            {
               isType = true;
               typeLen = 5;
            }
         }

         if (isType)
         {
            // Get the instance
            string instName = entityTypeName.Substring(0, entityTypeName.Length - typeLen);
            IfcSchemaEntityNode node = theTree.Find(instName);
            if (node != null && !node.isAbstract)
            {
               IFCEntityType instType = IFCEntityType.UnKnown;
               if (IFCEntityType.TryParse(instName, true, out instType))
                  m_ExportInstance = instType;
            }
            else
            {
               // If not found, try non-abstract supertype derived from the type
               node = IfcSchemaEntityTree.FindNonAbsInstanceSuperType(ifcVersion, instName);
               if (node != null)
               {
                  IFCEntityType instType = IFCEntityType.UnKnown;
                  if (IFCEntityType.TryParse(node.Name, true, out instType))
                     m_ExportInstance = instType;
               }
            }

            // set the type
            IFCEntityType entityType = ElementFilteringUtil.GetValidIFCEntityType(entityTypeName);
            if (entityType != IFCEntityType.UnKnown)
               m_ExportType = entityType;
            else
            {
               node = IfcSchemaEntityTree.FindNonAbsInstanceSuperType(ifcVersion, entityTypeName);
               if (node != null)
               {
                  IFCEntityType instType = IFCEntityType.UnKnown;
                  if (IFCEntityType.TryParse(node.Name, true, out instType))
                     m_ExportType = instType;
               }
            }
         }
         else
         {
            // set the instance
            IFCEntityType instType = ElementFilteringUtil.GetValidIFCEntityType(entityTypeName);
            if (instType != IFCEntityType.UnKnown)
               m_ExportInstance = instType;
            else
            {
               // If not found, try non-abstract supertype derived from the type
               IfcSchemaEntityNode node = IfcSchemaEntityTree.FindNonAbsInstanceSuperType(ifcVersion, entityTypeName);
               if (node != null)
               {
                  instType = IFCEntityType.UnKnown;
                  if (IFCEntityType.TryParse(node.Name, true, out instType))
                     m_ExportInstance = instType;
               }
            }

            // set the type pair
            string typeName = entityTypeName;
            if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 &&
               (entityTypeName.Equals("IfcDoor", StringComparison.InvariantCultureIgnoreCase)
               || entityTypeName.Equals("IfcWindow", StringComparison.InvariantCultureIgnoreCase)))
               typeName += "Style";
            else
               typeName += "Type";

            IFCEntityType entityType = ElementFilteringUtil.GetValidIFCEntityType(typeName);

            if (entityType != IFCEntityType.UnKnown)
               m_ExportType = entityType;
            else
            {
               // If the type name is not found, likely it does not have the pair at this level,
               // needs to get the supertype of the instance to get the type pair
               IList<IfcSchemaEntityNode> instNodes = IfcSchemaEntityTree.FindAllSuperTypes(ifcVersion, entityTypeName, "IfcProduct", "IfcGroup");
               foreach (IfcSchemaEntityNode instNode in instNodes)
               {
                  typeName = IfcSchemaEntityTree.GetTypeNameFromInstanceName(instNode.Name);
                  IfcSchemaEntityNode node = theTree.Find(typeName);
                  if (node == null)
                     node = IfcSchemaEntityTree.FindNonAbsInstanceSuperType(ifcVersion, typeName);

                  if (node != null && !node.isAbstract)
                  {
                     instType = IFCEntityType.UnKnown;
                     if (IFCEntityType.TryParse(node.Name, true, out instType))
                     {
                        m_ExportType = instType;
                        break;
                     }
                  }
               }
            }
         }
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

      // Check valid entity and type set according to the MVD used in the export
      // Also check and correct older standardcase entities and change it without StandardCase for IFC4 and onward
      void CheckValidEntity()
      {
         IFCCertifiedEntitiesAndPSets certEntAndPset = ExporterCacheManager.CertifiedEntitiesAndPsetsCache;

         // Special handling for *StandardCase entities that are not used anymore in IFC4
         if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
         {
            if (m_ExportInstance.ToString().EndsWith("StandardCase", StringComparison.InvariantCultureIgnoreCase))
            {
               string newInstanceName = m_ExportInstance.ToString().Remove(m_ExportInstance.ToString().Length - 12);

               // Special handling for IfcOpeningStandardCase to turn it to IfcOpeningElement
               if (newInstanceName.Equals("IfcOpening", StringComparison.InvariantCultureIgnoreCase))
                  newInstanceName = newInstanceName + "Element";

               IFCEntityType newInst;
               if (Enum.TryParse<IFCEntityType>(newInstanceName, true, out newInst))
                  //m_ExportInstance = newInst;
                  SetByType(newInst);
            }
            else if (m_ExportInstance.ToString().EndsWith("ElementedCase", StringComparison.InvariantCultureIgnoreCase))
            {
               string newInstanceName = m_ExportInstance.ToString().Remove(m_ExportInstance.ToString().Length - 13);
               IFCEntityType newInst;
               if (Enum.TryParse<IFCEntityType>(newInstanceName, true, out newInst))
                  SetByType(newInst);
            }
         }

         if (!certEntAndPset.IsValidEntityInCurrentMVD(m_ExportType.ToString()))
            m_ExportType = IFCEntityType.UnKnown;

         if (!certEntAndPset.IsValidEntityInCurrentMVD(m_ExportInstance.ToString()))
            m_ExportInstance = IFCEntityType.UnKnown;

         // IfcProxy is deprecated, we will change it to IfcBuildingElementProxy
         if (m_ExportInstance == IFCEntityType.IfcProxy && !ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
         {
            m_ExportInstance = IFCEntityType.IfcBuildingElementProxy;
            m_ExportType = IFCEntityType.IfcBuildingElementProxyType;
         }
      }
   }
}
