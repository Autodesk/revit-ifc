using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using BIM.IFC.Export.UI.Properties;
using static Revit.IFC.Export.Utility.ParameterUtil;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// This class is used for extraction of material properties information.
   /// </summary>
   public static class IFCMaterialPropertyUtil
   {
      /// <summary>
      /// Contains material property types.
      /// </summary>
      public enum MaterialParamTypesEnum
      {
         Identity,
         // Skip Appearance material parameters from mapping
         // Appearance,
         Physical,
         Thermal
      }

      /// <summary>
      /// Converts enum to localized string value.
      /// </summary>
      public static string ToString(MaterialParamTypesEnum materialType)
      {
         switch (materialType)
         {
            case MaterialParamTypesEnum.Identity:
               return Resources.IdentityMaterialParams;
            case MaterialParamTypesEnum.Physical:
               return Resources.PhysicalMaterialParams;
            case MaterialParamTypesEnum.Thermal:
               return Resources.ThermalMaterialParams;
            default:
               return null;
         }
      }

      /// <summary>
      /// Hardcoded list of identity params used for mapping.
      /// </summary>
      static readonly List<BuiltInParameter> IdentityParams = new()
      {
         // "Description"
         BuiltInParameter.ALL_MODEL_DESCRIPTION,
         // "Class"
         BuiltInParameter.PHY_MATERIAL_PARAM_CLASS,
         // "Comments"
         BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS,
         // "Keywords"
         BuiltInParameter.PROPERTY_SET_KEYWORDS,
         // "Manufacturer"
         BuiltInParameter.ALL_MODEL_MANUFACTURER,
         // "Model"
         BuiltInParameter.ALL_MODEL_MODEL,
         // "Cost"
         BuiltInParameter.ALL_MODEL_COST,
         // "URL"
         BuiltInParameter.ALL_MODEL_URL,
         // "Keynote"
         BuiltInParameter.KEYNOTE_PARAM,
         // "Mark"
         BuiltInParameter.ALL_MODEL_MARK
      };

      public static void CollectIdentityParameters(Material material, HashSet<KeyValuePair<ForgeTypeId, string>> allParams)
      {
         foreach (var identityParam in IdentityParams)
         {
            ForgeTypeId paramTypeId = ParameterUtils.GetParameterTypeId(identityParam);
            if (paramTypeId == null || paramTypeId.Empty())
               continue;

            string paramName = LabelUtils.GetLabelForBuiltInParameter(paramTypeId);
            if (string.IsNullOrEmpty(paramName))
               continue;

            allParams.Add(new KeyValuePair<ForgeTypeId, string>(paramTypeId, paramName));
         }
      }

      /// <summary>
      /// Collect Physical (Structural) Asset Parameters.
      /// </summary>
      public static void CollectStructuralParameters(Material material, HashSet<KeyValuePair<ForgeTypeId, string>> allParams)
      {
         PropertySetElement structuralPropSet = IFCCommandOverrideApplication.TheDocument.GetElement(material?.StructuralAssetId)
            as PropertySetElement;

         if (structuralPropSet == null)
            return;

         StructuralAsset structuralAsset = structuralPropSet.GetStructuralAsset();
         if (structuralAsset == null)
            return;

         StructuralAssetClass assetClass = structuralAsset.StructuralAssetClass;
         if (assetClass == StructuralAssetClass.Undefined)
            return;

         ICollection<Parameter> structuralParameters = structuralPropSet.GetOrderedParameters();
         if (structuralParameters == null || structuralParameters.Count == 0)
            return;

         foreach (Parameter param in structuralParameters)
         {
            if (param == null)
               continue;

            Definition paramDefinition = param.Definition;
            if (paramDefinition == null)
               continue;

            // Add parameters that are visible to the user.
            InternalDefinition internalDefinition = paramDefinition as InternalDefinition;
            if (internalDefinition != null && internalDefinition.Visible == false)
               continue;

            ForgeTypeId paramTypeId = internalDefinition.GetParameterTypeId();
            if (paramTypeId == null || paramTypeId.Empty())
               continue;

            allParams.Add(new KeyValuePair<ForgeTypeId, string>(paramTypeId, internalDefinition.Name));
         }
      }

      /// <summary>
      /// Collect Thermal Asset Parameters.
      /// </summary>
      public static void CollectThermalParameters(Material material, HashSet<KeyValuePair<ForgeTypeId, string>> allParams)
      {
         PropertySetElement thermalPropSet = IFCCommandOverrideApplication.TheDocument.GetElement(material?.ThermalAssetId)
            as PropertySetElement;
         if (thermalPropSet == null)
            return;

         ThermalAsset thermalAsset = thermalPropSet.GetThermalAsset();
         if (thermalAsset == null)
            return;

         ThermalMaterialType materialType = thermalAsset.ThermalMaterialType;
         if (materialType == ThermalMaterialType.Undefined)
            return;

         ICollection<Parameter> thermalParameters = thermalPropSet.GetOrderedParameters();
         if (thermalParameters == null)
            return;

         foreach (Parameter param in thermalParameters)
         {
            if (param == null)
               continue;

            Definition paramDefinition = param.Definition;
            if (paramDefinition == null)
               continue;

            // Add parameters that are visible to the user.
            InternalDefinition internalDefinition = paramDefinition as InternalDefinition;
            if (internalDefinition != null && internalDefinition.Visible == false)
               continue;

            ForgeTypeId paramTypeId = internalDefinition.GetParameterTypeId();
            if (paramTypeId == null || paramTypeId.Empty())
               continue;

            allParams.Add(new KeyValuePair<ForgeTypeId, string>(paramTypeId, internalDefinition.Name));
         }
      }

      /// <summary>
      /// Get material property mapping info.
      /// </summary>
      public static List<PropertyMappingInfo> GetMaterialPropertyMappingInfo(HashSet<KeyValuePair<ForgeTypeId, string>> parameters)
      {
         if (parameters == null || parameters.Count == 0)
            return null;

         List<PropertyMappingInfo> propertyInfos = new();
         foreach (var parameter in parameters)
            propertyInfos.Add(new PropertyMappingInfo(string.Empty, parameter.Value, ElementId.InvalidElementId, IFCPropertySetups.PropertySetup.MaterialPropertySets));

         return propertyInfos;
      }
   }
}
