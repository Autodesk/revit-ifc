//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012  Autodesk, Inc.
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
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Toolkit;
using static Revit.IFC.Export.Utility.ParameterUtil;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Provides static methods material properties related manipulations.
   /// </summary>
   public class MaterialPropertiesUtil
   {
      /// <summary>
      /// Enumeration for material property types.
      /// </summary>
      public enum MaterialPropertyType
      {
         Identity,
         Structural,
         Thermal
      }

      /// <summary>
      /// Caches of parameters (id + name + dataType name) for each material property type.
      /// </summary>
      private static List<(BuiltInParameter, string, string)> m_identityParameters = new(); 
      private static Dictionary<ThermalMaterialType, List<(BuiltInParameter, string, string)>> m_thermalParameters = new();
      private static Dictionary<(StructuralAssetClass, StructuralBehavior), List<(BuiltInParameter, string, string)>> m_structuralParameters = new();

      /// <summary>
      /// Structural material parameters for each asset class.
      /// </summary>
      private static readonly Dictionary<StructuralAssetClass, List<BuiltInParameter>> StructuralParameters = new()
      {
         [StructuralAssetClass.Basic] = new List<BuiltInParameter>()
         {
            BuiltInParameter.PROPERTY_SET_DESCRIPTION,
            BuiltInParameter.PROPERTY_SET_KEYWORDS,
            BuiltInParameter.PHY_MATERIAL_PARAM_SUBCLASS,
            BuiltInParameter.PHY_MATERIAL_PARAM_STRUCTURAL_DENSITY
         },

         [StructuralAssetClass.Concrete] = new List<BuiltInParameter>()
         {
            BuiltInParameter.PHY_MATERIAL_PARAM_BEHAVIOR,
            BuiltInParameter.PROPERTY_SET_DESCRIPTION,
            BuiltInParameter.PROPERTY_SET_KEYWORDS,
            BuiltInParameter.PHY_MATERIAL_PARAM_SUBCLASS,
            BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE,
            BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE_URL,
            BuiltInParameter.PHY_MATERIAL_PARAM_STRUCTURAL_DENSITY,
            BuiltInParameter.PHY_MATERIAL_PARAM_CONCRETE_COMPRESSION,
            BuiltInParameter.PHY_MATERIAL_PARAM_SHEAR_STRENGTH_REDUCTION,
            BuiltInParameter.PHY_MATERIAL_PARAM_LIGHT_WEIGHT,
            BuiltInParameter.PHY_MATERIAL_PARAM_MINIMUM_YIELD_STRESS,
            BuiltInParameter.PHY_MATERIAL_PARAM_MINIMUM_TENSILE_STRENGTH
         },

         [StructuralAssetClass.Gas] = new List<BuiltInParameter>()
         {
            BuiltInParameter.PROPERTY_SET_DESCRIPTION,
            BuiltInParameter.PROPERTY_SET_KEYWORDS,
            BuiltInParameter.PHY_MATERIAL_PARAM_SUBCLASS,
            BuiltInParameter.PHY_MATERIAL_PARAM_STRUCTURAL_DENSITY,
            BuiltInParameter.PHY_MATERIAL_PARAM_EXP_COEFF
         },

         [StructuralAssetClass.Generic] = new List<BuiltInParameter>()
         {
            BuiltInParameter.PHY_MATERIAL_PARAM_BEHAVIOR,
            BuiltInParameter.PROPERTY_SET_DESCRIPTION,
            BuiltInParameter.PROPERTY_SET_KEYWORDS,
            BuiltInParameter.PHY_MATERIAL_PARAM_SUBCLASS,
            BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE,
            BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE_URL,
            BuiltInParameter.PHY_MATERIAL_PARAM_STRUCTURAL_DENSITY,
            BuiltInParameter.PHY_MATERIAL_PARAM_MINIMUM_YIELD_STRESS,
            BuiltInParameter.PHY_MATERIAL_PARAM_MINIMUM_TENSILE_STRENGTH
         },

         [StructuralAssetClass.Liquid] = new List<BuiltInParameter>()
         {
            BuiltInParameter.PROPERTY_SET_DESCRIPTION,
            BuiltInParameter.PROPERTY_SET_KEYWORDS,
            BuiltInParameter.PHY_MATERIAL_PARAM_SUBCLASS,
            BuiltInParameter.PHY_MATERIAL_PARAM_STRUCTURAL_DENSITY,
            BuiltInParameter.PHY_MATERIAL_PARAM_EXP_COEFF
         },

         [StructuralAssetClass.Metal] = new List<BuiltInParameter>()
         {
            BuiltInParameter.PHY_MATERIAL_PARAM_BEHAVIOR,
            BuiltInParameter.PROPERTY_SET_DESCRIPTION,
            BuiltInParameter.PROPERTY_SET_KEYWORDS,
            BuiltInParameter.PHY_MATERIAL_PARAM_SUBCLASS,
            BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE,
            BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE_URL,
            BuiltInParameter.PHY_MATERIAL_PARAM_STRUCTURAL_DENSITY,
            BuiltInParameter.PHY_MATERIAL_PARAM_MINIMUM_YIELD_STRESS,
            BuiltInParameter.PHY_MATERIAL_PARAM_MINIMUM_TENSILE_STRENGTH,
            BuiltInParameter.PHY_MATERIAL_PARAM_STRUCTURAL_THERMAL_TREATED
         },

         [StructuralAssetClass.Plastic] = new List<BuiltInParameter>()
         {
            BuiltInParameter.PHY_MATERIAL_PARAM_BEHAVIOR,
            BuiltInParameter.PROPERTY_SET_DESCRIPTION,
            BuiltInParameter.PROPERTY_SET_KEYWORDS,
            BuiltInParameter.PHY_MATERIAL_PARAM_SUBCLASS,
            BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE,
            BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE_URL,
            BuiltInParameter.PHY_MATERIAL_PARAM_STRUCTURAL_DENSITY,
            BuiltInParameter.PHY_MATERIAL_PARAM_MINIMUM_YIELD_STRESS,
            BuiltInParameter.PHY_MATERIAL_PARAM_MINIMUM_TENSILE_STRENGTH
         },

         [StructuralAssetClass.Wood] = new List<BuiltInParameter>()
         {
            BuiltInParameter.PHY_MATERIAL_PARAM_BEHAVIOR,
            BuiltInParameter.PROPERTY_SET_DESCRIPTION,
            BuiltInParameter.PROPERTY_SET_KEYWORDS,
            BuiltInParameter.PHY_MATERIAL_PARAM_SUBCLASS,
            BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE,
            BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE_URL,
            BuiltInParameter.PHY_MATERIAL_PARAM_STRUCTURAL_DENSITY,
            BuiltInParameter.PHY_MATERIAL_PARAM_MINIMUM_YIELD_STRESS,
            BuiltInParameter.PHY_MATERIAL_PARAM_MINIMUM_TENSILE_STRENGTH,
            BuiltInParameter.PHY_MATERIAL_PARAM_SPECIES,
            BuiltInParameter.PHY_MATERIAL_PARAM_GRADE,
            BuiltInParameter.PHY_MATERIAL_PARAM_BENDING,
            BuiltInParameter.PHY_MATERIAL_PARAM_COMPRESSION_PARALLEL,
            BuiltInParameter.PHY_MATERIAL_PARAM_COMPRESSION_PERPENDICULAR,
            BuiltInParameter.PHY_MATERIAL_PARAM_TENSION_PARALLEL,
            BuiltInParameter.PHY_MATERIAL_PARAM_SHEAR_PARALLEL,
            BuiltInParameter.PHY_MATERIAL_PARAM_TENSION_PERPENDICULAR,
            BuiltInParameter.PHY_MATERIAL_PARAM_AVERAGE_MODULUS,
            BuiltInParameter.PHY_MATERIAL_PARAM_WOOD_CONSTRUCTION
         }
      };

      /// <summary>
      /// Structural material parameters for each behaviour type.
      /// </summary>
      private static readonly Dictionary<StructuralBehavior, List<BuiltInParameter>> StructuralBehaviorParameters = new()
      {
         [StructuralBehavior.Isotropic] = new List<BuiltInParameter>()
         {
            BuiltInParameter.PHY_MATERIAL_PARAM_EXP_COEFF,
            BuiltInParameter.PHY_MATERIAL_PARAM_YOUNG_MOD,
            BuiltInParameter.PHY_MATERIAL_PARAM_POISSON_MOD,
            BuiltInParameter.PHY_MATERIAL_PARAM_SHEAR_MOD
         },
         [StructuralBehavior.Orthotropic] = new List<BuiltInParameter>()
         {
            BuiltInParameter.PHY_MATERIAL_PARAM_EXP_COEFF1,
            BuiltInParameter.PHY_MATERIAL_PARAM_EXP_COEFF2,
            BuiltInParameter.PHY_MATERIAL_PARAM_EXP_COEFF3,

            BuiltInParameter.PHY_MATERIAL_PARAM_YOUNG_MOD1,
            BuiltInParameter.PHY_MATERIAL_PARAM_YOUNG_MOD2,
            BuiltInParameter.PHY_MATERIAL_PARAM_YOUNG_MOD3,

            BuiltInParameter.PHY_MATERIAL_PARAM_POISSON_MOD1,
            BuiltInParameter.PHY_MATERIAL_PARAM_POISSON_MOD2,
            BuiltInParameter.PHY_MATERIAL_PARAM_POISSON_MOD3,

            BuiltInParameter.PHY_MATERIAL_PARAM_SHEAR_MOD1,
            BuiltInParameter.PHY_MATERIAL_PARAM_SHEAR_MOD2,
            BuiltInParameter.PHY_MATERIAL_PARAM_SHEAR_MOD3,
         },
         [StructuralBehavior.TransverseIsotropic] = new List<BuiltInParameter>()
         {
            BuiltInParameter.PHY_MATERIAL_PARAM_EXP_COEFF_1,
            BuiltInParameter.PHY_MATERIAL_PARAM_EXP_COEFF_2,

            BuiltInParameter.PHY_MATERIAL_PARAM_YOUNG_MOD_1,
            BuiltInParameter.PHY_MATERIAL_PARAM_YOUNG_MOD_2,

            BuiltInParameter.PHY_MATERIAL_PARAM_POISSON_MOD_12,
            BuiltInParameter.PHY_MATERIAL_PARAM_POISSON_MOD_23,

            BuiltInParameter.PHY_MATERIAL_PARAM_SHEAR_MOD_12
         },
      };

      /// <summary>
      /// Collects the parameter list for a particular assert class and behaviour type.
      /// </summary>
      private static List<BuiltInParameter> GetStructuralParametersFromMap(StructuralAssetClass materialType, StructuralBehavior materialBehaviour)
      {
         if (!StructuralParameters.TryGetValue(materialType, out var parameters))
            return null;

         if (parameters.FirstOrDefault() != BuiltInParameter.PHY_MATERIAL_PARAM_BEHAVIOR)
            return parameters;

         if (!StructuralBehaviorParameters.TryGetValue(materialBehaviour, out var behaviourParameters))
            return parameters;

         return parameters.Union(behaviourParameters).ToList();
      }


      /// <summary>
      /// Exports material properties.
      /// </summary>
      /// <param name="file"> The IFC file.</param>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      public static void ExportMaterialProperties(IFCFile file, ExporterIFC exporterIFC)
      {
         bool materialPropertiesAreAllowed =
           !ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 ||
           ExporterCacheManager.CertifiedEntitiesAndPsetsCache.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcExtendedMaterialProperties");

         if (!materialPropertiesAreAllowed)
            return;

         Document document = ExporterCacheManager.Document;

         foreach (KeyValuePair<ElementId, Tuple<IFCAnyHandle, IFCExportInfoPair>> cachedMaterial in ExporterCacheManager.MaterialHandleCache.ElementIdToHandleAndInfo)
         {
            ElementId materialId = cachedMaterial.Key;
            IFCAnyHandle materialHnd = cachedMaterial.Value?.Item1;

            if (IFCAnyHandleUtil.IsNullOrHasNoValue(materialHnd))
               continue;

            Material material = document?.GetElement(materialId) as Material;
            if (material == null)
               continue;

            // Export material properties from 3 tabs in generic fashion
            ExportMaterialSetParameters(file, document, material, materialHnd, MaterialPropertyType.Identity);
            ExportMaterialSetParameters(file, document, material, materialHnd, MaterialPropertyType.Structural);
            ExportMaterialSetParameters(file, document, material, materialHnd, MaterialPropertyType.Thermal);

            // 1. Maps project/shared parameters to 'built-in material properties'
            // For example, export IfcMechanicalMaterialProperties.DynamicViscosity Revit material project/shared parameter to IfcMechanicalMaterialProperties.DynamicViscosity attribute
            // 2. Exports some hardcoded mapped Revit material parameters (see MaterialBuildInParameterUtil class) to 'built-in material properties'
            // For example, export Revit material parameter Density('Physical' tab) to IfcGeneralMaterialProperties.MassDensity attribute
            ExportMappedMaterialProperties(file, exporterIFC, material, materialHnd);

            // Export internal Revit properties
            // For example, non-ifc project parameters to IfcExtendedMaterialProperties 
            PropertyUtil.CreateInternalRevitPropertySets(exporterIFC, material, new HashSet<IFCAnyHandle>() { materialHnd }, true);
         }
      }

      /// <summary>
      /// Exports material properties of the specified property type.
      /// </summary>
      /// <param name="file"> The IFC file.</param>
      /// <param name="document"> The document.</param>
      /// <param name="material">The material.</param>
      /// <param name="materialHnd">The tha material handle object.</param>
      /// <param name="materialPropertyType">The tha material set type.</param>
      public static void ExportMaterialSetParameters(IFCFile file, Document document, Material material, IFCAnyHandle materialHnd, MaterialPropertyType materialPropertyType)
      {
         IFCParameterTemplate parameterTemplate = ExporterCacheManager.ParameterMappingTemplate;
         string materialSetName = materialPropertyType.ToString();

         // Skip property groups excluded from export
         if (parameterTemplate != null &&
            parameterTemplate.IsPropertySetAMemberOfTemplate(PropertySetupType.RevitMaterialParameters, materialSetName) &&
            !parameterTemplate.IsExportingPropertySet(PropertySetupType.RevitMaterialParameters, materialSetName))
         {
            return;
         }

         HashSet<IFCAnyHandle> properties = CreateSetProperties(file, document, material, materialPropertyType);
         ExportGenericMaterialPropertySet(file, materialHnd, properties, description: null, materialSetName);
      }

      /// <summary>
      /// Creates a set of material properties of the specified property type.
      /// </summary>
      /// <param name="file"> The IFC file.</param>
      /// <param name="document"> The document.</param>
      /// <param name="material">The material.</param>
      /// <param name="materialPropertyType">The material set type.</param>
      public static HashSet<IFCAnyHandle> CreateSetProperties(IFCFile file, Document document, Material material, MaterialPropertyType materialPropertyType)
      {
         if (file == null || document == null || material == null)
            return null;

         HashSet<IFCAnyHandle> properties = new();

         List<(BuiltInParameter, string, string)> materialParameters = null;
         Element assertElement = null;

         switch (materialPropertyType)
         {
            case MaterialPropertyType.Identity:
               {
                  materialParameters = GetIdentityParameters(material);
                  assertElement = material;
                  break;
               }
            case MaterialPropertyType.Structural:
               {
                  materialParameters = GetStructuralParameters(document, material, out assertElement);
                  break;
               }
            case MaterialPropertyType.Thermal:
               {
                  materialParameters = GetThermalParameters(document, material, out assertElement);
                  break;
               }
         }

         if ((materialParameters?.Count ?? 0) == 0 || assertElement == null)
            return properties;

         string materialSetName = materialPropertyType.ToString();

         foreach (var paramPair in materialParameters)
         {
            Parameter param = assertElement.get_Parameter(paramPair.Item1);
            if (param == null)
               continue;

            string parameterName = paramPair.Item2;

            // Skip properties exluded from export
            IFCPropertyMappingInfo mappingInfo = PropertyUtil.GetParameterMappingInfoFromCache(
               PropertySetupType.RevitMaterialParameters, materialSetName, param.Id, parameterName);
            if ((mappingInfo?.ExportFlag ?? true) == false)
               continue;

            if (!string.IsNullOrEmpty(mappingInfo?.IFCPropertyName))
               parameterName = mappingInfo.IFCPropertyName;

            IFCAnyHandle propertyHnd = PropertyUtil.CreatePropertyByParameterStorageType(file, param, parameterName);
            if (propertyHnd != null)
               properties.Add(propertyHnd);
         }

         return properties;
      }

      /// <summary>
      /// Collects and caches Identity material parameters
      /// </summary>
      public static List<(BuiltInParameter, string, string)> GetIdentityParameters(Material material)
      {
         if (m_identityParameters.Count != 0)
            return m_identityParameters;
         
         IList<ElementId> identityParamIds = Material.GetIdentityParameterIds();
         if (identityParamIds == null)
            return m_identityParameters;

         foreach (var identityParam in identityParamIds)
         {
            if (identityParam == null)
               continue;

            BuiltInParameter builtInParameter = (BuiltInParameter)identityParam.Value;

            string dataTypeName = string.Empty;
            Parameter param = material.get_Parameter(builtInParameter);
            if(param != null && param.Definition != null)
            {
               ForgeTypeId dataTypeId = param.Definition.GetDataType();
               if ((dataTypeId?.Empty() ?? true) == false)
                  dataTypeName = LabelUtils.GetLabelForSpec(dataTypeId);
            }

            ForgeTypeId paramTypeId = ParameterUtils.GetParameterTypeId(builtInParameter);
            if ((paramTypeId?.Empty() ?? true) == true)
               continue;

            string paramName = LabelUtils.GetLabelForBuiltInParameter(paramTypeId);
            if (string.IsNullOrEmpty(paramName))
               continue;

            m_identityParameters.Add((builtInParameter, paramName, dataTypeName));
         }

         return m_identityParameters;
      }

      /// <summary>
      /// Collects and caches Structural material parameters
      /// </summary>
      public static List<(BuiltInParameter, string, string)> GetStructuralParameters(Document document, Material material, out Element assetElement)
      {
         assetElement = null;

         if (document == null || material == null)
            return null;

         PropertySetElement structuralSet = document.GetElement(material.StructuralAssetId) as PropertySetElement;
         if (structuralSet == null)
            return null;

         StructuralAsset structuralAsset = structuralSet.GetStructuralAsset();
         if (structuralAsset == null)
            return null;

         assetElement = structuralSet;
         StructuralAssetClass materialType = structuralAsset.StructuralAssetClass;
         StructuralBehavior materialBehaviour = structuralAsset.Behavior;

         if (m_structuralParameters.TryGetValue((materialType, materialBehaviour), out var cachedParameters))
            return cachedParameters;

         cachedParameters = new();
         m_structuralParameters[(materialType, materialBehaviour)] = cachedParameters;

         // Get parameters from the map. We don't use the same logic as for Thermal parameters 
         // because for unknown reason for Structural parameters the GetOrderedParameters always returns
         // an entire list of parameters without filtering it according to the asset class.
         List<BuiltInParameter> structuralParameters = GetStructuralParametersFromMap(materialType, materialBehaviour);
         if ((structuralParameters?.Count ?? 0) == 0)
            return cachedParameters;

         foreach (BuiltInParameter paramId in structuralParameters)
         {
            if (paramId == BuiltInParameter.INVALID)
               continue;

            ForgeTypeId paramTypeId = ParameterUtils.GetParameterTypeId(paramId);
            if ((paramTypeId?.Empty() ?? true) == true)
               continue;

            string paramName = LabelUtils.GetLabelForBuiltInParameter(paramTypeId);
            if (string.IsNullOrEmpty(paramName))
               continue;

            string dataTypeName = string.Empty;
            Parameter param = material.get_Parameter(paramId);
            if (param != null && param.Definition != null)
            {
               ForgeTypeId dataTypeId = param.Definition.GetDataType();
               if ((dataTypeId?.Empty() ?? true) == false)
                  dataTypeName = LabelUtils.GetLabelForSpec(dataTypeId);
            }

            cachedParameters.Add((paramId, paramName, dataTypeName));
         }

         return cachedParameters;
      }

      /// <summary>
      /// Collects and caches Thermal material parameters
      /// </summary>
      public static List<(BuiltInParameter, string, string)> GetThermalParameters(Document document, Material material, out Element assetElement)
      {
         assetElement = null;

         if (document == null || material == null)
            return null;

         PropertySetElement thermalSet = document.GetElement(material.ThermalAssetId) as PropertySetElement;
         if (thermalSet == null)
            return null;

         ThermalAsset thermalAsset = thermalSet.GetThermalAsset();
         if (thermalAsset == null)
            return null;

         assetElement = thermalSet;
         ThermalMaterialType materialType = thermalAsset.ThermalMaterialType;

         if (m_thermalParameters.TryGetValue(materialType, out var cachedParameters))
            return cachedParameters;

         cachedParameters = new();
         m_thermalParameters[materialType] = cachedParameters;

         ICollection<Parameter> thermalParameters = thermalSet.GetOrderedParameters();
         if (thermalParameters == null)
            return cachedParameters;

         foreach (Parameter param in thermalParameters)
         {
            if (param == null || param.Definition == null)
               continue;

            string dataTypeName = string.Empty;
            ForgeTypeId dataTypeId = param.Definition.GetDataType();
            if ((dataTypeId?.Empty() ?? true) == false)
               dataTypeName = LabelUtils.GetLabelForSpec(dataTypeId);

            cachedParameters.Add(((BuiltInParameter)param.Id.Value, param.Definition.Name, dataTypeName));
         }

         return cachedParameters;
      }


      /// <summary>
      /// Creates generic material properties
      /// </summary>
      /// <param name="file"> The IFC file.</param>
      /// <param name="materialHnd"> The material handle.</param>
      /// <param name="properties"> The properties set.</param>
      /// <param name="description"> The description.</param>
      /// <param name="name">The name.</param>
      public static void ExportGenericMaterialPropertySet(IFCFile file, IFCAnyHandle materialHnd, ISet<IFCAnyHandle> properties, string description, string name)
      {
         if (file == null || materialHnd == null || (properties?.Count ?? 0) == 0)
            return;

         if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
            IFCInstanceExporter.CreateExtendedMaterialProperties(file, materialHnd, properties, description, name);
         else
            IFCInstanceExporter.CreateMaterialProperties(file, materialHnd, properties, description, name);
      }

      /// <summary>
      /// Exports material properties according ot mapping table
      /// </summary>
      /// <param name="file"> The IFC file.</param>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      /// <param name="material">The material.</param>
      /// <param name="materialHnd">The tha material handle object.</param>
      static void ExportMappedMaterialProperties(IFCFile file, ExporterIFC exporterIFC, Material material, IFCAnyHandle materialHnd)
      {
         if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
         {
            IList<IList<PreDefinedPropertySetDescription>> psetsToCreate = 
               ExporterCacheManager.ParameterCache.PreDefinedPropertySets;
            IList<PreDefinedPropertySetDescription> currPsetsToCreate =
               ExporterUtil.GetCurrPreDefinedPSetsToCreate(materialHnd, psetsToCreate,
               PSetsToProcess.Both);
            
            foreach (PreDefinedPropertySetDescription currDesc in currPsetsToCreate)
            {
               // Create list of IFCData attributes using mapped parameter name
               IList<(string name, PropertyValueType type,  IList<IFCData> data)> createdAttributes = currDesc.ProcessEntries(file, material);

               if ((createdAttributes?.Count ?? 0) == 0)
                  continue;

               // Create IfcMaterialProperties derived entity
               IFCAnyHandle propertyHndl = null;
               if (Enum.TryParse(currDesc.Name, out Common.Enums.IFCEntityType propertyType))
                  propertyHndl = IFCAnyHandleUtil.CreateInstance(file, propertyType);

               if (IFCAnyHandleUtil.IsNullOrHasNoValue(propertyHndl))
                  return;

               IFCAnyHandleUtil.ValidateSubTypeOf(materialHnd, false, Common.Enums.IFCEntityType.IfcMaterial);
               IFCAnyHandleUtil.SetAttribute(propertyHndl, "Material", materialHnd);
               foreach (var createdAttribute in createdAttributes)
               {
                  if ((createdAttribute.data?.Count ?? 0) == 0)
                     continue;

                  if (createdAttribute.type == PropertyValueType.ListValue)
                     IFCAnyHandleUtil.SetAttribute(propertyHndl, createdAttribute.name, createdAttribute.data);
                  else
                     IFCAnyHandleUtil.SetAttribute(propertyHndl, createdAttribute.name, createdAttribute.data[0]);
               }
            }
         }
         else
         {
            IList<PropertySetDescription> currPsetsToCreate =
               ExporterUtil.GetCurrPSetsToCreate(materialHnd, PSetsToProcess.Instance);

            foreach (PropertySetDescription currDesc in currPsetsToCreate)
            {
               ElementOrConnector elementOrConnector = new ElementOrConnector(material);
               ISet<IFCAnyHandle> props = currDesc.ProcessEntries(file, exporterIFC, null, elementOrConnector, null, materialHnd);
               if (props.Count > 0)
                  IFCInstanceExporter.CreateMaterialProperties(file, materialHnd, props, currDesc.DescriptionOfSet, currDesc.Name);
            }
         }
      }

      /// <summary>
      /// Collects parameters for all the materials of the document.
      /// </summary>
      public static Dictionary<string, List<(BuiltInParameter, string, string)>> GetGroupedMaterialParameters(Document document)
      {
         if (document == null)
            return null;

         FilteredElementCollector materialElementCollector = new(document);
         ElementFilter materialElementFilter = new ElementClassFilter(typeof(Material));
         materialElementCollector.WherePasses(materialElementFilter);
         List<Material> materials = materialElementCollector.Cast<Material>().ToList();
         if ((materials?.Count ?? 0) == 0)
            return null;

         foreach (Material material in materials)
         {
            // Cache Identity parameters if needed
            if (m_identityParameters.Count == 0)
               GetIdentityParameters(material);

            // Cache Structural parameters if needed
            PropertySetElement structuralSet = document.GetElement(material.StructuralAssetId) as PropertySetElement;
            StructuralAsset structuralAsset = structuralSet?.GetStructuralAsset();
            if (structuralAsset != null)
            {
               StructuralAssetClass structuralType = structuralAsset.StructuralAssetClass;
               StructuralBehavior structuralBehaviour = structuralAsset.Behavior;
               if (!m_structuralParameters.ContainsKey((structuralType, structuralBehaviour)))
                  GetStructuralParameters(document, material, out _);
            }

            // Cache Thermal parameters if needed
            PropertySetElement thermalSet = document?.GetElement(material.ThermalAssetId) as PropertySetElement;
            ThermalAsset thermalAsset = thermalSet?.GetThermalAsset();
            if (thermalAsset != null)
            {
               ThermalMaterialType thermalType = thermalAsset.ThermalMaterialType;
               if (!m_thermalParameters.ContainsKey(thermalType))
                  GetThermalParameters(document, material, out _);
            }
         }

         // Add Identity parameters
         Dictionary<string, List<(BuiltInParameter, string, string)>> groupedMaterialParameters = new();
         if (m_identityParameters.Any())
            groupedMaterialParameters.TryAdd(MaterialPropertyType.Identity.ToString(), m_identityParameters);

         // Add Structural parameters 
         List<(BuiltInParameter, string, string)> allStructuralParameters = new();
         foreach (var structuralParameters in m_structuralParameters.Values)
            allStructuralParameters = allStructuralParameters.Union(structuralParameters).ToList();

         if (allStructuralParameters.Any())
            groupedMaterialParameters.TryAdd(MaterialPropertyType.Structural.ToString(), allStructuralParameters);

         // Add Thermal parameters 
         List<(BuiltInParameter, string, string)> allThermalParameters = new();
         foreach (var thermalParameters in m_thermalParameters.Values)
            allThermalParameters = allThermalParameters.Union(thermalParameters).ToList();

         if (allThermalParameters.Any())
            groupedMaterialParameters.TryAdd(MaterialPropertyType.Thermal.ToString(), allThermalParameters);

         return groupedMaterialParameters;
      }
   }


   /// <summary>
   /// Provides static methods for export builtIn material properties to specifict ifc entities.
   /// </summary>
   public class MaterialBuiltInParameterUtil
   {
      // Dictionary of properties to export to specific IFC entities
      // Each property has: list of property sets and function to extract the value
      static readonly Dictionary<string, Tuple<IList<string>, Func<Material, double?>>> materialBuiltInSet = new Dictionary<string, Tuple<IList<string>, Func<Material, double?>>>
      {
         { "MassDensity",          new Tuple<IList<string>, Func<Material, double?>>(new List<string>{ "Pset_MaterialCommon", "IfcGeneralMaterialProperties"}, getBuiltInMassDensity) },
         { "Porosity",             new Tuple<IList<string>, Func<Material, double?>>(new List<string>{ "Pset_MaterialCommon", "IfcGeneralMaterialProperties"}, getBuiltInPorosity) },
         { "SpecificHeatCapacity", new Tuple<IList<string>, Func<Material, double?>>(new List<string>{ "Pset_MaterialThermal", "IfcThermalMaterialProperties"}, getBuiltInSpecificHeatCapacity) },
         { "ThermalConductivity",  new Tuple<IList<string>, Func<Material, double?>>(new List<string>{ "Pset_MaterialThermal", "IfcThermalMaterialProperties"}, getBuiltInThermalConductivity) },
         { "YieldStress",          new Tuple<IList<string>, Func<Material, double?>>(new List<string>{ "Pset_MaterialSteel", "IfcMechanicalSteelMaterialProperties"}, getBuiltInYieldStress) },
         { "PoissonRatio",         new Tuple<IList<string>, Func<Material, double?>>(new List<string>{ "Pset_MaterialMechanical", "IfcMechanicalMaterialProperties"}, getBuiltInPoissonRatio) },
         { "YoungModulus",         new Tuple<IList<string>, Func<Material, double?>>(new List<string>{ "Pset_MaterialMechanical", "IfcMechanicalMaterialProperties"}, getBuiltInYoungModulus) },
         { "ShearModulus",         new Tuple<IList<string>, Func<Material, double?>>(new List<string>{ "Pset_MaterialMechanical", "IfcMechanicalMaterialProperties"}, getBuiltInShearModulus) },
         { "ThermalExpansionCoefficient", new Tuple<IList<string>, Func<Material, double?>>(new List<string>{ "Pset_MaterialMechanical", "IfcMechanicalMaterialProperties"}, getBuiltInThermalExpansionCoefficient) }
      };

      /// <summary>
      /// Get MassDensity value from material
      /// </summary>
      /// <param name="material">The material.</param>
      /// <returns>nullable value.</returns>
      static double? getBuiltInMassDensity(Material material)
      {
         StructuralAsset structuralAsset = getMaterialStructuralAssert(material);
         if (structuralAsset == null)
            return null;

         return structuralAsset.Density;
      }

      /// <summary>
      /// Get Porosity value from material
      /// </summary>
      /// <param name="material">The material.</param>
      /// <returns>nullable value.</returns>
      static double? getBuiltInPorosity(Material material)
      {
         ThermalAsset thermalAsset = getMaterialThermalAssert(material);
         if (thermalAsset == null)
            return null;
         ThermalMaterialType materialType = thermalAsset.ThermalMaterialType;

         if (materialType == ThermalMaterialType.Solid)
            return thermalAsset.Porosity;
         else
            return null;
      }

      /// <summary>
      /// Get SpecificHeatCapacity value from material
      /// </summary>
      /// <param name="material">The material.</param>
      /// <returns>nullable value.</returns>
      static double? getBuiltInSpecificHeatCapacity(Material material)
      {
         ThermalAsset thermalAsset = getMaterialThermalAssert(material);
         if (thermalAsset == null)
            return null;

         return thermalAsset.SpecificHeat;
      }

      /// <summary>
      /// Get ThermalConductivity value from material
      /// </summary>
      /// <param name="material">The material.</param>
      /// <returns>nullable value.</returns>
      static double? getBuiltInThermalConductivity(Material material)
      {
         ThermalAsset thermalAsset = getMaterialThermalAssert(material);
         if (thermalAsset == null)
            return null;
         ThermalMaterialType materialType = thermalAsset.ThermalMaterialType;

         if (thermalAsset.Behavior != StructuralBehavior.Orthotropic || materialType != ThermalMaterialType.Solid)
            return thermalAsset.ThermalConductivity;
         else
            return null;
      }

      /// <summary>
      /// Get YieldStress value from material
      /// </summary>
      /// <param name="material">The material.</param>
      /// <returns>nullable value.</returns>
      static double? getBuiltInYieldStress(Material material)
      {
         StructuralAsset structuralAsset = getMaterialStructuralAssert(material);
         if (structuralAsset == null)
            return null;
         StructuralAssetClass assetClass = structuralAsset.StructuralAssetClass;

         if (assetClass == StructuralAssetClass.Metal || assetClass == StructuralAssetClass.Concrete || assetClass == StructuralAssetClass.Generic
            || assetClass == StructuralAssetClass.Wood || assetClass == StructuralAssetClass.Plastic)
            return structuralAsset.MinimumYieldStress;
         else
            return null;
      }

      /// <summary>
      /// Get PoissonRatio value from material
      /// </summary>
      /// <param name="material">The material.</param>
      /// <returns>nullable value.</returns>
      static double? getBuiltInPoissonRatio(Material material)
      {
         StructuralAsset structuralAsset = getMaterialStructuralAssert(material);
         if (structuralAsset == null)
            return null;
         StructuralAssetClass assetClass = structuralAsset.StructuralAssetClass;

         if (structuralAsset.Behavior == StructuralBehavior.Isotropic && (assetClass == StructuralAssetClass.Metal || assetClass == StructuralAssetClass.Concrete
            || assetClass == StructuralAssetClass.Generic || assetClass == StructuralAssetClass.Wood || assetClass == StructuralAssetClass.Plastic))
            return structuralAsset.PoissonRatio?.X;
         else
            return null;
      }

      /// <summary>
      /// Get YoungModulus value from material
      /// </summary>
      /// <param name="material">The material.</param>
      /// <returns>nullable value.</returns>
      static double? getBuiltInYoungModulus(Material material)
      {
         StructuralAsset structuralAsset = getMaterialStructuralAssert(material);
         if (structuralAsset == null)
            return null;
         StructuralAssetClass assetClass = structuralAsset.StructuralAssetClass;

         if (structuralAsset.Behavior == StructuralBehavior.Isotropic && (assetClass == StructuralAssetClass.Metal || assetClass == StructuralAssetClass.Concrete
            || assetClass == StructuralAssetClass.Generic || assetClass == StructuralAssetClass.Wood || assetClass == StructuralAssetClass.Plastic))
            return structuralAsset.YoungModulus?.X;
         else
            return null;
      }

      /// <summary>
      /// Get ShearModulus value from material
      /// </summary>
      /// <param name="material">The material.</param>
      /// <returns>nullable value.</returns>
      static double? getBuiltInShearModulus(Material material)
      {
         StructuralAsset structuralAsset = getMaterialStructuralAssert(material);
         if (structuralAsset == null)
            return null;
         StructuralAssetClass assetClass = structuralAsset.StructuralAssetClass;

         if (structuralAsset.Behavior == StructuralBehavior.Isotropic && (assetClass == StructuralAssetClass.Metal || assetClass == StructuralAssetClass.Concrete
            || assetClass == StructuralAssetClass.Generic || assetClass == StructuralAssetClass.Wood || assetClass == StructuralAssetClass.Plastic))
            return structuralAsset.ShearModulus?.X;
         else
            return null;
      }

      /// <summary>
      /// Get ThermalExpansionCoefficient value from material
      /// </summary>
      /// <param name="material">The material.</param>
      /// <returns>nullable value.</returns>
      static double? getBuiltInThermalExpansionCoefficient(Material material)
      {
         StructuralAsset structuralAsset = getMaterialStructuralAssert(material);
         if (structuralAsset == null)
            return null;
         StructuralAssetClass assetClass = structuralAsset.StructuralAssetClass;

         if (structuralAsset.Behavior == StructuralBehavior.Isotropic && (assetClass == StructuralAssetClass.Metal || assetClass == StructuralAssetClass.Concrete
            || assetClass == StructuralAssetClass.Generic || assetClass == StructuralAssetClass.Plastic || assetClass == StructuralAssetClass.Wood)
            || assetClass == StructuralAssetClass.Gas || assetClass == StructuralAssetClass.Liquid)
            return structuralAsset.ThermalExpansionCoefficient?.X;
         else
            return null;
      }

      /// <summary>
      /// Check if the property must be exported 
      /// </summary>
      /// <param name="propertyName">The property name.</param>
      /// <returns>True if it is to export as material builtIn parameter.</returns>
      public static bool isMaterialBuiltInParameter(string propertyName)
      {
         return materialBuiltInSet.ContainsKey(propertyName);
      }

      /// <summary>
      /// Create material property data if it is built in
      /// </summary>
      /// <param name="psetName">The material.</param>
      /// <param name="propertyName">The material.</param>
      /// <param name="propertyType">The material.</param>
      /// <param name="element">The material.</param>
      /// <returns>Material data.</returns>
      public static IList<IFCData> CreatePredefinedDataIfBuiltIn(string psetName, string propertyName, PropertyType propertyType, Element element)
      {
         IList<IFCData> data = null;
         if (isMaterialBuiltInParameter(propertyName))
         {
            IFCData singleData = CreateMaterialDataFromParameter(psetName, propertyName, propertyType, element);
            if (singleData != null)
               data = new List<IFCData>() { singleData };
         }

         return data;
      }

      /// <summary>
      /// Get thermal assert from material
      /// </summary>
      /// <param name="material">The material.</param>
      /// <returns>The thermal assert.</returns>
      static ThermalAsset getMaterialThermalAssert(Material material)
      {
         if (material == null)
            return null;
         Document document = ExporterCacheManager.Document;
         PropertySetElement thermalSet = document?.GetElement(material.ThermalAssetId) as PropertySetElement;
         return thermalSet?.GetThermalAsset();
      }

      /// <summary>
      /// Get thermal structural from material
      /// </summary>
      /// <param name="material">The material.</param>
      /// <returns>The structural assert.</returns>
      static StructuralAsset getMaterialStructuralAssert(Material material)
      {
         if (material == null)
            return null;
         Document document = ExporterCacheManager.Document;
         PropertySetElement structuralSet = document?.GetElement(material.StructuralAssetId) as PropertySetElement;
         return structuralSet?.GetStructuralAsset();
      }

      /// <summary>
      /// Create material property handle
      /// </summary>
      /// <param name="psetName">The material.</param>
      /// <param name="propertyName">The material.</param>
      /// <param name="propertyType">The material.</param>
      /// <param name="element">The material.</param>
      /// <param name="file">The file.</param>
      /// <returns>Material property handle.</returns>
      public static IFCAnyHandle CreateMaterialPropertyIfBuiltIn(string psetName, string propertyName, PropertyType propertyType, Element element, IFCFile file)
      {
         if (!isMaterialBuiltInParameter(propertyName))
         {
            return null;
         }
         
         IFCData data = CreateMaterialDataFromParameter(psetName, propertyName, propertyType, element);
         if (data == null)
         {
            return null;
         }

         PropertyDescription propertyDescription = new PropertyDescription(propertyName);
         return PropertyUtil.CreateCommonProperty(file, propertyDescription, data, PropertyValueType.SingleValue, null);
      }

      /// <summary>
      /// Create material property data
      /// </summary>
      /// <param name="psetName">The material.</param>
      /// <param name="propertyName">The material.</param>
      /// <param name="propertyType">The material.</param>
      /// <param name="element">The material.</param>
      /// <returns>Material data.</returns>
      public static IFCData CreateMaterialDataFromParameter(string psetName, string propertyName, PropertyType propertyType, Element element)
      {
         IFCData data = null;
         if (materialBuiltInSet.TryGetValue(propertyName, out var parameterInfo))
         {
            if (!parameterInfo.Item1.Contains(psetName) || parameterInfo.Item2 == null)
               return data;

            double? paramValue = parameterInfo.Item2.Invoke(element as Material);
            if (!paramValue.HasValue)
               return data;

            switch (propertyType)
            {
               case PropertyType.MassDensity:
                  {
                     paramValue = UnitUtil.ScaleMassDensity(paramValue.Value);
                     data = IFCDataUtil.CreateAsMassDensityMeasure(paramValue.Value);
                     break;
                  }
               case PropertyType.Ratio:
               case PropertyType.NormalisedRatio:
               case PropertyType.PositiveRatio:
                  {
                     data = IFCDataUtil.CreateRatioMeasureDataCommon(paramValue.Value, propertyType);
                     break;
                  }
               case PropertyType.SpecificHeatCapacity:
                  {
                     paramValue = UnitUtil.ScaleSpecificHeatCapacity(paramValue.Value);
                     data = IFCDataUtil.CreateAsSpecificHeatCapacityMeasure(paramValue.Value);
                     break;
                  }
               case PropertyType.ThermalConductivity:
                  {
                     paramValue = UnitUtil.ScaleThermalConductivity(paramValue.Value);
                     data = IFCDataUtil.CreateAsThermalConductivityMeasure(paramValue.Value);
                     break;
                  }
               case PropertyType.Pressure:
                  {
                     paramValue = UnitUtil.ScalePressure(paramValue.Value);
                     data = IFCDataUtil.CreateAsPressureMeasure(paramValue.Value);
                     break;
                  }
               case PropertyType.ModulusOfElasticity:
                  {
                     paramValue = UnitUtil.ScaleModulusOfElasticity(paramValue.Value);
                     data = IFCDataUtil.CreateAsModulusOfElasticityMeasure(paramValue.Value);
                     break;
                  }
               case PropertyType.ThermalExpansionCoefficient:
                  {
                     paramValue = UnitUtil.ScaleThermalExpansionCoefficient(paramValue.Value);
                     data = IFCDataUtil.CreateAsThermalExpansionCoefficientMeasure(paramValue.Value);
                     break;
                  }
            }
         }

         return data;
      }
   }
}