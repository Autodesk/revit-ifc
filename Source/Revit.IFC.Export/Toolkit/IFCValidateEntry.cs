using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;

using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Toolkit
{
   public static class IFCValidateEntry
   {
      /// <summary>
      /// Get the IFC type from shared parameters, or from a type name.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="typeName">The original value.</param>
      /// <returns>The found value.</returns>
      public static string GetValidIFCPredefinedType(/*Element element, */string typeName, string theTypeEnumstr)
      {
         return GetValidIFCPredefinedTypeType(/*element,*/ typeName, null, theTypeEnumstr);
      }

      /// <summary>
      /// Get the IFC type from shared parameters, from a type name, or from a default value.
      /// </summary>
      /// <param name="typeName">The type value.</param>
      /// <param name="defaultValue">A default value that can be null.</param>
      /// <returns>The found value.</returns>
      public static string GetValidIFCPredefinedTypeType(string typeName, string defaultValue, string theTypeEnumStr)
      {
         string enumValue = null;

         if (typeName != null || defaultValue != null)
         {
            try
            {
               string toolkitName = "Revit.IFC.Export.Toolkit.";
               string desiredTypeExtra = null;
               if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
                  desiredTypeExtra = "IFC4.";
               else
               {
                  // For IFC2x3, the enum uses Ifc...Type, but in some cases there is no associated type.
                  if (!(theTypeEnumStr.Length > 4 && theTypeEnumStr.Substring(theTypeEnumStr.Length - 4, 4).Equals("TYPE", StringComparison.InvariantCultureIgnoreCase)))
                     theTypeEnumStr = theTypeEnumStr + "Type";
               }

               string desiredType = toolkitName + desiredTypeExtra + theTypeEnumStr;
               Type theTypeEnum = Type.GetType(desiredType, false, true);
               
               if (theTypeEnum == null)
               {
                  if (ProcessRuleExceptions(ref theTypeEnumStr))
                  {
                     desiredType = toolkitName + desiredTypeExtra + theTypeEnumStr;
                     theTypeEnum = Type.GetType(desiredType, false, true);
                  }

                  // In this case, the entity doesn't have a predefined type.
                  if (theTypeEnum == null)
                     return null;
               }
                 

               if (theTypeEnum != null && !string.IsNullOrEmpty(typeName))
                  enumValue = Enum.Parse(theTypeEnum, typeName, true).ToString();
            }
            catch
            {
            }
         }

         if (String.IsNullOrEmpty(enumValue) && !String.IsNullOrEmpty(defaultValue))
            return defaultValue;

         return enumValue;
      }

      /// <summary>
      /// Get the IFC type from shared parameters, from a type name, or from a default value.
      /// </summary>
      /// <typeparam name="TEnum">The type of Enum.</typeparam>
      /// <param name="element">The element.</param>
      /// <param name="typeName">The type value.</param>
      /// <param name="defaultValue">A default value that can be null.</param>
      /// <returns>The found value, or null.</returns>
      public static string GetValidIFCType<TEnum>(Element element, string typeName, string defaultValue) where TEnum : struct
      {
         BuiltInParameter paramId = (element is ElementType) ? BuiltInParameter.IFC_EXPORT_PREDEFINEDTYPE_TYPE :
            BuiltInParameter.IFC_EXPORT_PREDEFINEDTYPE;
         Parameter exportElementParameter = element.get_Parameter(paramId);
         string value = exportElementParameter?.AsString();
         if (ValidateStrEnum<TEnum>(value) != null)
            return value;
         
         if (paramId == BuiltInParameter.IFC_EXPORT_PREDEFINEDTYPE)
         {
            Element elementType = element.Document.GetElement(element.GetTypeId());
            exportElementParameter = elementType?.get_Parameter(BuiltInParameter.IFC_EXPORT_PREDEFINEDTYPE_TYPE);
            value = exportElementParameter?.AsString();
            if (ValidateStrEnum<TEnum>(value) != null)
               return value;
         }

         if (!string.IsNullOrEmpty(typeName) && 
            (string.Compare(typeName, "NotDefined", true) != 0 || string.IsNullOrEmpty(defaultValue)))
         {
            if (ValidateStrEnum<TEnum>(typeName) != null)
               return typeName;
         }

         if (!String.IsNullOrEmpty(defaultValue))
            return defaultValue;

         // We used to return "NotDefined" here.  However, that assumed that all types had "NotDefined" as a value.
         // It is better to return null.
         return null;
      }

      /// <summary>
      /// Validates that a string belongs to an Enum class.
      /// </summary>
      /// <typeparam name="TEnum">The type of Enum.</typeparam>
      /// <param name="strEnumCheck">The string to check.</param>
      /// <returns>The original string, if valid, or null.</returns>
      public static string ValidateStrEnum<TEnum>(string strEnumCheck) where TEnum : struct
      {
         TEnum enumValue;

         if (typeof(TEnum).IsEnum)
         {
            // We used to return "NotDefined" here.  However, that assumed that all types had "NotDefined" as a value.
            // It is better to return null.
            if (!Enum.TryParse(strEnumCheck, true, out enumValue))
               return null;
         }
         return strEnumCheck;
      }

      public static TEnum ValidateEntityEnum<TEnum>(TEnum entityCheck)
      {
         return entityCheck;
      }

      public static bool ProcessRuleExceptions(ref string theTypeEnumStr)
      {
         bool processed = false;
         // Particular case: the Predefined type of IfcDistributionCircuit is in IfcDistributionSystemEnum
         if (theTypeEnumStr == "IfcDistributionCircuit")
         {
            theTypeEnumStr = "IfcDistributionSystem";
            processed = true;
         }
         return processed;
      }
      
   }
}
