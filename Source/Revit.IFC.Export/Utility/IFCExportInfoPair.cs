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
         set
         {
            m_ExportInstance = value;
            CheckValidEntity();
         }
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
         set
         {
            m_ExportType = value;
            CheckValidEntity();
         }
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
      public void SetValueWithPair(IFCEntityType entityType)
      {
         string entityTypeStr = entityType.ToString();
         bool isType = entityTypeStr.Substring(entityTypeStr.Length - 4, 4).Equals("Type", StringComparison.CurrentCultureIgnoreCase);

         if (isType)
         {
            // Get the instance
            string instName = entityTypeStr.Substring(0, entityTypeStr.Length - 4);
            IfcSchemaEntityNode node = IfcSchemaEntityTree.Find(instName);
            if (node != null && !node.isAbstract)
            {
               IFCEntityType instType = IFCEntityType.UnKnown;
               if (IFCEntityType.TryParse(instName, out instType))
                  m_ExportInstance = instType;
            }
            // If not found, try non-abstract supertype derived from the type
            node = IfcSchemaEntityTree.FindNonAbsInstanceSuperType(instName);
            if (node != null)
            {
               IFCEntityType instType = IFCEntityType.UnKnown;
               if (IFCEntityType.TryParse(node.Name, out instType))
                  m_ExportInstance = instType;
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
                  if (IFCEntityType.TryParse(node.Name, out instType))
                     m_ExportType = instType;
               }
            }
         }
         else
         {
            // set the instance
            entityType = ElementFilteringUtil.GetValidIFCEntityType(entityType);
            if (entityType != IFCEntityType.UnKnown)
               m_ExportInstance = entityType;
            else
            {
               // If not found, try non-abstract supertype derived from the type
               IfcSchemaEntityNode node = IfcSchemaEntityTree.FindNonAbsInstanceSuperType(entityTypeStr);
               if (node != null)
               {
                  IFCEntityType instType = IFCEntityType.UnKnown;
                  if (IFCEntityType.TryParse(node.Name, out instType))
                     m_ExportInstance = instType;
               }
            }

            // set the type pair
            string typeName = entityType.ToString() + "Type";
            entityType = ElementFilteringUtil.GetValidIFCEntityType(typeName);
            if (entityType != IFCEntityType.UnKnown)
               m_ExportType = entityType;
            else
            {
               IfcSchemaEntityNode node = IfcSchemaEntityTree.FindNonAbsInstanceSuperType(typeName);
               if (node != null)
               {
                  IFCEntityType instType = IFCEntityType.UnKnown;
                  if (IFCEntityType.TryParse(node.Name, out instType))
                     m_ExportType = instType;
               }
            }
         }

         ValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedTypeType("NOTDEFINED", ValidatedPredefinedType, m_ExportType.ToString());
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
               IFCEntityType newInst;
               if (Enum.TryParse<IFCEntityType>(newInstanceName, out newInst))
                  m_ExportInstance = newInst;
            }
         }

         if (!certEntAndPset.IsValidEntityInCurrentMVD(m_ExportType.ToString()))
            m_ExportType = IFCEntityType.UnKnown;

         if (!certEntAndPset.IsValidEntityInCurrentMVD(m_ExportInstance.ToString()))
            m_ExportInstance = IFCEntityType.UnKnown;
      }
   }
}
