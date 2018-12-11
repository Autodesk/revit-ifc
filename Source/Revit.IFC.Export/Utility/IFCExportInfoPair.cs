using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// A class that hold information for exporting what IfcEntity and its type pair
   /// </summary>
   public class IFCExportInfoPair
   {
      IFCEntityType m_ExportInstance = IFCEntityType.UnKnown;
      
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
         // Changed to read-only attribute. The value should not be set from outside to ensure integrity
         //set
         //{
         //   m_ExportInstance = value;
         //   CheckValidEntity();
         //}
      }

      IFCEntityType m_ExportType = IFCEntityType.UnKnown;

      /// <summary>
      /// The type for export
      /// </summary>
      public IFCEntityType ExportType {
         get
         {
            CheckValidEntity();
            return m_ExportType;
         }
         // Changed to read-only attribute. The value should not be set from outside to ensure integrity
         //set
         //{
         //   m_ExportType = value;
         //   CheckValidEntity();
         //}
      }

      /// <summary>
      /// Validated PredefinedType from IfcExportType (or IfcType for the old param), or from IfcExportAs
      /// </summary>
      public string ValidatedPredefinedType { get; set; } = null;

      /// <summary>
      /// Initialization of the class
      /// </summary>
      public IFCExportInfoPair()
      {
         // Set default value if not defined
         ValidatedPredefinedType = "NOTDEFINED";
      }

      /// <summary>
      /// Initialize the class with the entity and the type
      /// </summary>
      /// <param name="instance">the entity</param>
      /// <param name="type">the type</param>
      public IFCExportInfoPair(IFCEntityType instance, IFCEntityType type, string predefinedType)
      {
         instance = ElementFilteringUtil.GetValidIFCEntityType(instance);
         m_ExportInstance = instance;

         type = ElementFilteringUtil.GetValidIFCEntityType(type);
         m_ExportType = type;

         if (!string.IsNullOrEmpty(predefinedType))
         {
            string newValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedTypeType(predefinedType, ValidatedPredefinedType, m_ExportInstance.ToString());
            if (ExporterUtil.IsNotDefined(newValidatedPredefinedType))
               newValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedTypeType(predefinedType, ValidatedPredefinedType, m_ExportType.ToString());
            ValidatedPredefinedType = newValidatedPredefinedType;
         }
         else
            ValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedTypeType("NOTDEFINED", ValidatedPredefinedType, m_ExportType.ToString());
      }

      /// <summary>
      /// Check whether the export information is unknown type
      /// </summary>
      public bool IsUnKnown
      {
         get { return (m_ExportInstance == IFCEntityType.UnKnown); }
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

         if (!string.IsNullOrEmpty(predefinedType))
         {
            string newValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedTypeType(predefinedType, ValidatedPredefinedType, m_ExportInstance.ToString());
            if (ExporterUtil.IsNotDefined(newValidatedPredefinedType))
               newValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedTypeType(predefinedType, ValidatedPredefinedType, m_ExportType.ToString());
            ValidatedPredefinedType = newValidatedPredefinedType;
         }
         else
            ValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedTypeType("NOTDEFINED", ValidatedPredefinedType, m_ExportType.ToString());
      }

      /// <summary>
      /// Set the pair information using only either the entity or the type
      /// </summary>
      /// <param name="entityType">the entity or type</param>
      /// <param name="predefineType">predefinedtype string</param>
      public void SetValueWithPair(IFCEntityType entityType, string predefineType = null)
      {
         string entityTypeStr = entityType.ToString();
         int typeLen = 4;
         bool isType = entityTypeStr.Substring(entityTypeStr.Length - 4, 4).Equals("Type", StringComparison.CurrentCultureIgnoreCase);
         if (!isType)
         {
            if (entityType == IFCEntityType.IfcDoorStyle || entityType == IFCEntityType.IfcWindowStyle)
            {
               isType = true;
               typeLen = 5;
            }
         }

         if (isType)
         {
            // Get the instance
            string instName = entityTypeStr.Substring(0, entityTypeStr.Length - typeLen);
            IfcSchemaEntityNode node = IfcSchemaEntityTree.Find(instName);
            if (node != null && !node.isAbstract)
            {
               IFCEntityType instType = IFCEntityType.UnKnown;
               if (IFCEntityType.TryParse(instName, true, out instType))
                  m_ExportInstance = instType;
            }
            else
            {
               // If not found, try non-abstract supertype derived from the type
               node = IfcSchemaEntityTree.FindNonAbsInstanceSuperType(instName);
               if (node != null)
               {
                  IFCEntityType instType = IFCEntityType.UnKnown;
                  if (IFCEntityType.TryParse(node.Name, true, out instType))
                     m_ExportInstance = instType;
               }
            }

            // set the type
            entityType = ElementFilteringUtil.GetValidIFCEntityType(entityType);
            if (entityType != IFCEntityType.UnKnown)
               m_ExportType = entityType;
            else
            {
               node = IfcSchemaEntityTree.FindNonAbsInstanceSuperType(entityTypeStr);
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
            IFCEntityType instType = ElementFilteringUtil.GetValidIFCEntityType(entityType);
            if (instType != IFCEntityType.UnKnown)
               m_ExportInstance = instType;
            else
            {
               // If not found, try non-abstract supertype derived from the type
               IfcSchemaEntityNode node = IfcSchemaEntityTree.FindNonAbsInstanceSuperType(entityTypeStr);
               if (node != null)
               {
                  instType = IFCEntityType.UnKnown;
                  if (IFCEntityType.TryParse(node.Name, true, out instType))
                     m_ExportInstance = instType;
               }
            }

            // set the type pair
            string typeName = entityType.ToString();
            if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 && (entityType == IFCEntityType.IfcDoor || entityType == IFCEntityType.IfcWindow))
               typeName += "Style";
            else
               typeName += "Type";

            entityType = ElementFilteringUtil.GetValidIFCEntityType(typeName);
            if (entityType != IFCEntityType.UnKnown)
               m_ExportType = entityType;
            else
            {
               IfcSchemaEntityNode node = IfcSchemaEntityTree.FindNonAbsInstanceSuperType(typeName);
               if (node != null)
               {
                  instType = IFCEntityType.UnKnown;
                  if (IFCEntityType.TryParse(node.Name, true, out instType))
                     m_ExportType = instType;
               }
            }
         }

         if (string.IsNullOrEmpty(predefineType))
            predefineType = "NOTDEFINED";

         if (m_ExportType == IFCEntityType.UnKnown)
         {
            ValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedTypeType(predefineType, ValidatedPredefinedType, m_ExportInstance.ToString());
         }
         else
         {
            ValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedTypeType(predefineType, ValidatedPredefinedType, m_ExportType.ToString());
         }
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
                  SetValueWithPair(newInst);
            }
            else if (m_ExportInstance.ToString().EndsWith("ElementedCase", StringComparison.InvariantCultureIgnoreCase))
            {
               string newInstanceName = m_ExportInstance.ToString().Remove(m_ExportInstance.ToString().Length - 13);
               IFCEntityType newInst;
               if (Enum.TryParse<IFCEntityType>(newInstanceName, true, out newInst))
                  SetValueWithPair(newInst);
            }
         }

         if (!certEntAndPset.IsValidEntityInCurrentMVD(m_ExportType.ToString()))
            m_ExportType = IFCEntityType.UnKnown;

         if (!certEntAndPset.IsValidEntityInCurrentMVD(m_ExportInstance.ToString()))
            m_ExportInstance = IFCEntityType.UnKnown;

         // IfcProxy is deprecated, we will change it to IfcBuildingElementProxy
         if (m_ExportInstance == IFCEntityType.IfcProxy)
         {
            m_ExportInstance = IFCEntityType.IfcBuildingElementProxy;
            m_ExportType = IFCEntityType.IfcBuildingElementProxyType;
         }
      }
   }
}
