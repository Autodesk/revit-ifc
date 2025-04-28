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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Newtonsoft.Json.Linq;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Toolkit;
using static Revit.IFC.Export.Utility.ParameterUtil;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Provides static methods material properties related manipulations.
   /// </summary>
   class MaterialPropertiesUtil
   {
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
            if (material != null)
            {
               // Export material properties from 3 tabs in generic fashion
               ExportIdentityParameters(file, material, materialHnd);
               ExportStructuralParameters(file, document, material, materialHnd);
               ExportThermalParameters(file, document, material, materialHnd);

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
      }

      /// <summary>
      /// Exports structural material properties from 'Identity' tab
      /// </summary>
      /// <param name="file"> The IFC file.</param>
      /// <param name="material">The material.</param>
      /// <param name="materialHnd">The tha material handle object.</param>
      static void ExportIdentityParameters(IFCFile file, Material material, IFCAnyHandle materialHnd)
      {
         HashSet<IFCAnyHandle> properties = CreateIdentityProperties(file, material);
         ExportGenericMaterialPropertySet(file, materialHnd, properties, null, "Identity");
      }

      /// <summary>
      /// Exports structural material properties from 'Physical' tab
      /// </summary>
      /// <param name="file"> The IFC file.</param>
      /// <param name="document">The document to export.</param>
      /// <param name="material">The material.</param>
      /// <param name="materialHnd">The tha material handle object.</param>
      static void ExportStructuralParameters(IFCFile file, Document document, Material material, IFCAnyHandle materialHnd)
      {
         if (material?.StructuralAssetId == null)
            return;

         PropertySetElement structuralSet = document.GetElement(material.StructuralAssetId) as PropertySetElement;

         HashSet<IFCAnyHandle> properties = CreateStructuralProperties(file, structuralSet);

         ExportGenericMaterialPropertySet(file, materialHnd, properties, null, "Structural");
      }

      /// <summary>
      /// Exports thermal material properties from 'Thermal' tab
      /// </summary>
      /// <param name="file"> The IFC file.</param>
      /// <param name="document">The document to export.</param>
      /// <param name="material">The material.</param>
      /// <param name="materialHnd">The tha material handle object.</param>
      static void ExportThermalParameters(IFCFile file, Document document, Material material, IFCAnyHandle materialHnd)
      {
         if (material?.ThermalAssetId == null)
            return;

         PropertySetElement thermalSet = document.GetElement(material.ThermalAssetId) as PropertySetElement;

         HashSet<IFCAnyHandle> properties = CreateThermalProperties(file, thermalSet);

         ExportGenericMaterialPropertySet(file, materialHnd, properties, null, "Thermal");
      }

      /// <summary>
      /// Creates Identity material properties
      /// </summary>
      /// <param name="file"> The IFC file.</param>
      /// <param name="material">The material.</param>
      /// <returns>Set of exported properties.</returns>
      static HashSet<IFCAnyHandle> CreateIdentityProperties(IFCFile file, Material material)
      {
         if (file == null || material == null)
            return null;

         HashSet<IFCAnyHandle> properties = new HashSet<IFCAnyHandle>();

         // Category
         PropertyDescription catPropertyDescription = new PropertyDescription("Category");
         string name = material.MaterialCategory;
         properties.Add(PropertyUtil.CreateLabelProperty(file, catPropertyDescription, name, PropertyValueType.SingleValue, null));

         // Class
         PropertyDescription classPropertyDescription = new PropertyDescription("Class");
         name = material.MaterialClass;
         properties.Add(PropertyUtil.CreateLabelProperty(file, classPropertyDescription, name, PropertyValueType.SingleValue, null));

         // The rest of identity parameters are exported automatically in PropertyUtil.CreateInternalRevitPropertySets
         return properties;
      }

      static private void SetSimpleMaterialProperty(IFCFile file, ISet<IFCAnyHandle> properties, string propertyName, 
         string value)
      {
         PropertyDescription propertyDescription = new PropertyDescription(propertyName);
         properties.Add(PropertyUtil.CreateLabelProperty(file, propertyDescription, value, PropertyValueType.SingleValue, null));
      }

      static private void SetSimpleMaterialProperty(IFCFile file, ISet<IFCAnyHandle> properties, 
         PropertySetElement structuralSet, BuiltInParameter builtInParameter, string propertyName)
      {
         string strValue;
         GetStringValueFromElement(structuralSet, builtInParameter, out strValue);
         if (string.IsNullOrEmpty(strValue))
         {
            return;
         }

         PropertyDescription propertyDescription = new PropertyDescription(propertyName);
         properties.Add(PropertyUtil.CreateLabelProperty(file, propertyDescription, strValue, PropertyValueType.SingleValue, null));
      }

      static private void SetSimpleMaterialProperty(IFCFile file, ISet<IFCAnyHandle> properties, 
         ForgeTypeId specTypeId, string propertyName, double value)
      {
         PropertyDescription propertyDescription = new PropertyDescription(propertyName);
         properties.Add(PropertyUtil.CreateRealPropertyByType(file, specTypeId, propertyDescription, value, PropertyValueType.SingleValue));
      }

      static private void SetSimpleMaterialProperty(IFCFile file, ISet<IFCAnyHandle> properties, string propertyName, bool value)
      {
         PropertyDescription propertyDescription = new PropertyDescription(propertyName);
         properties.Add(PropertyUtil.CreateBooleanProperty(file, propertyDescription, value, PropertyValueType.SingleValue));
      }

      /// <summary>
      /// Creates Identity material properties
      /// </summary>
      /// <param name="file"> The IFC file.</param>
      /// <param name="structuralSet">The structural properety set element.</param>
      /// <returns>Set of exported properties.</returns>
      static HashSet<IFCAnyHandle> CreateStructuralProperties(IFCFile file, PropertySetElement structuralSet)
      {
         if (file == null || structuralSet == null)
            return null;

         StructuralAsset structuralAsset = structuralSet?.GetStructuralAsset();
         if (structuralAsset == null)
            return null;

         StructuralAssetClass assetClass = structuralAsset.StructuralAssetClass;
         if (assetClass == StructuralAssetClass.Undefined)
            return null;

         HashSet<IFCAnyHandle> properties = new HashSet<IFCAnyHandle>();

         StructuralBehavior behaviour = structuralAsset.Behavior;

         SetSimpleMaterialProperty(file, properties, structuralSet, BuiltInParameter.PROPERTY_SET_NAME, "Name");
         SetSimpleMaterialProperty(file, properties, structuralSet, BuiltInParameter.PROPERTY_SET_DESCRIPTION, "Description");
         SetSimpleMaterialProperty(file, properties, structuralSet, BuiltInParameter.PROPERTY_SET_KEYWORDS, "Keywords");

         // Type
         SetSimpleMaterialProperty(file, properties, "Type", assetClass.ToString());
         SetSimpleMaterialProperty(file, properties, "SubClass", structuralAsset.SubClass);

         if (assetClass == StructuralAssetClass.Concrete || assetClass == StructuralAssetClass.Metal || assetClass == StructuralAssetClass.Generic
            || assetClass == StructuralAssetClass.Plastic || assetClass == StructuralAssetClass.Wood)
         {
            SetSimpleMaterialProperty(file, properties, structuralSet, BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE, "Source");
            SetSimpleMaterialProperty(file, properties, structuralSet, BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE_URL, "Source URL");
         }

         // Behavior
         SetSimpleMaterialProperty(file, properties, "Behavior", behaviour.ToString());
         
         if (assetClass != StructuralAssetClass.Basic)
         {
            // ThermalExpansionCoefficient X
            XYZ thermalExpansionCoefficientXYZ = structuralAsset.ThermalExpansionCoefficient;
            if ((assetClass == StructuralAssetClass.Metal || assetClass == StructuralAssetClass.Concrete || assetClass == StructuralAssetClass.Generic || assetClass == StructuralAssetClass.Plastic || assetClass == StructuralAssetClass.Wood) && behaviour != StructuralBehavior.Isotropic)
            {
               string thermalExpansionCoefficientName = (behaviour == StructuralBehavior.Orthotropic) ? "ThermalExpansionCoefficientX" : "ThermalExpansionCoefficient1";
               SetSimpleMaterialProperty(file, properties, SpecTypeId.ThermalExpansionCoefficient, thermalExpansionCoefficientName, thermalExpansionCoefficientXYZ.X);
            }
         }

         if (assetClass == StructuralAssetClass.Metal || assetClass == StructuralAssetClass.Concrete || assetClass == StructuralAssetClass.Generic
            || assetClass == StructuralAssetClass.Wood || assetClass == StructuralAssetClass.Plastic)
         {
            // ThermalExpansionCoefficient Y
            XYZ thermalExpansionCoefficientXYZ = structuralAsset.ThermalExpansionCoefficient;
            if (behaviour == StructuralBehavior.Orthotropic || behaviour == StructuralBehavior.TransverseIsotropic)
            {
               string thermalExpansionCoefficientName = (behaviour == StructuralBehavior.Orthotropic) ? "ThermalExpansionCoefficientY" : "ThermalExpansionCoefficient2";
               SetSimpleMaterialProperty(file, properties, SpecTypeId.ThermalExpansionCoefficient, thermalExpansionCoefficientName, thermalExpansionCoefficientXYZ.Y);
            }

            // ThermalExpansionCoefficient Z
            if (behaviour == StructuralBehavior.Orthotropic)
            {
               SetSimpleMaterialProperty(file, properties, SpecTypeId.ThermalExpansionCoefficient, "ThermalExpansionCoefficientZ", thermalExpansionCoefficientXYZ.Z);
            }

            // YoungModulus X
            XYZ youngModulusXYZ = structuralAsset.YoungModulus;
            if (behaviour != StructuralBehavior.Isotropic)
            {
               string youngModulusNameX = (behaviour == StructuralBehavior.Orthotropic) ? "YoungModulusX" : "YoungModulus1";
               SetSimpleMaterialProperty(file, properties, SpecTypeId.Stress, youngModulusNameX, youngModulusXYZ.X);
            }
            // YoungModulus Y
            if (behaviour == StructuralBehavior.Orthotropic || behaviour == StructuralBehavior.TransverseIsotropic)
            {
               string youngModulusNameY = (behaviour == StructuralBehavior.Orthotropic) ? "YoungModulusY" : "YoungModulus2";
               SetSimpleMaterialProperty(file, properties, SpecTypeId.Stress, youngModulusNameY, youngModulusXYZ.Y);
            }

            // YoungModulus Z
            if (behaviour == StructuralBehavior.Orthotropic)
            {
               SetSimpleMaterialProperty(file, properties, SpecTypeId.Stress, "YoungModulusZ", youngModulusXYZ.Z);
            }

            XYZ poissonRatioXYZ = structuralAsset.PoissonRatio;
            if (behaviour != StructuralBehavior.Isotropic)
            {
               // PoissonRatio X
               string poissonRatioNameX = (behaviour == StructuralBehavior.Orthotropic) ? "PoissonRatioX" : "PoissonRatio12";
               SetSimpleMaterialProperty(file, properties, SpecTypeId.Number, poissonRatioNameX, poissonRatioXYZ.X);
            }

            // PoissonRatio Y
            if (behaviour == StructuralBehavior.Orthotropic || behaviour == StructuralBehavior.TransverseIsotropic)
            {
               string poissonRatioNameY = (behaviour == StructuralBehavior.Orthotropic) ? "PoissonRatioY" : "PoissonRatio23";
               SetSimpleMaterialProperty(file, properties, SpecTypeId.Number, poissonRatioNameY, poissonRatioXYZ.Y);
            }

            // PoissonRatio Z
            if (behaviour == StructuralBehavior.Orthotropic)
            {
               SetSimpleMaterialProperty(file, properties, SpecTypeId.Number, "PoissonRatioZ", poissonRatioXYZ.Z);
            }

            // ShearModulus X
            XYZ shearModulusXYZ = structuralAsset.ShearModulus;
            if (behaviour != StructuralBehavior.Isotropic)
            {
               string shearModulusName = (behaviour == StructuralBehavior.Orthotropic) ? "ShearModulusX" : "ShearModulus12";
               SetSimpleMaterialProperty(file, properties, SpecTypeId.Stress, shearModulusName, shearModulusXYZ.X);
            }

            // ShearModulus Y
            if (behaviour == StructuralBehavior.Orthotropic)
            {
               SetSimpleMaterialProperty(file, properties, SpecTypeId.Stress, "ShearModulusY", shearModulusXYZ.Y);
            }

            // ShearModulus Z
            if (behaviour == StructuralBehavior.Orthotropic)
            {
               SetSimpleMaterialProperty(file, properties, SpecTypeId.Stress, "ShearModulusZ", shearModulusXYZ.Z);
            }

            SetSimpleMaterialProperty(file, properties, SpecTypeId.Stress, "TensileStrength", structuralAsset.MinimumTensileStrength);
         }

         if (assetClass == StructuralAssetClass.Metal)
         {
            SetSimpleMaterialProperty(file, properties, "ThermallyTreated", structuralAsset.MetalThermallyTreated);
         }

         if (assetClass == StructuralAssetClass.Wood)
         {
            SetSimpleMaterialProperty(file, properties, "Species", structuralAsset.WoodSpecies);
            SetSimpleMaterialProperty(file, properties, "WoodGrade", structuralAsset.WoodGrade);

            SetSimpleMaterialProperty(file, properties, SpecTypeId.Stress, "Bending", structuralAsset.WoodBendingStrength);
            SetSimpleMaterialProperty(file, properties, SpecTypeId.Stress, "CompressionParalleltoGrain", structuralAsset.WoodParallelCompressionStrength);
            SetSimpleMaterialProperty(file, properties, SpecTypeId.Stress, "CompressionPerpendiculartoGrain", structuralAsset.WoodPerpendicularCompressionStrength);
            SetSimpleMaterialProperty(file, properties, SpecTypeId.Stress, "ShearParallelToGrain", structuralAsset.WoodParallelShearStrength);

            // TensionParallelToGrain
            double tensionParallelToGrain;
            Parameter param = GetDoubleValueFromElement(structuralSet, BuiltInParameter.PHY_MATERIAL_PARAM_TENSION_PARALLEL, out tensionParallelToGrain);
            if (param != null)
            {
               SetSimpleMaterialProperty(file, properties, SpecTypeId.Stress, "TensionParallelToGrain", tensionParallelToGrain);
            }

            // TensionPerpendicularToGrain
            double tensionPerpendicularToGrain;
            param = GetDoubleValueFromElement(structuralSet, BuiltInParameter.PHY_MATERIAL_PARAM_TENSION_PERPENDICULAR, out tensionPerpendicularToGrain);
            if (param != null)
            {
               SetSimpleMaterialProperty(file, properties, SpecTypeId.Stress, "TensionPerpendicularToGrain", tensionPerpendicularToGrain);
            }

            // AverageModulus
            double averageModulus;
            param = GetDoubleValueFromElement(structuralSet, BuiltInParameter.PHY_MATERIAL_PARAM_AVERAGE_MODULUS, out averageModulus);
            if (param != null)
            {
               SetSimpleMaterialProperty(file, properties, SpecTypeId.Stress, "AverageModulus", averageModulus);
            }

            // Construction
            int construction;
            if (GetIntValueFromElement(structuralSet, BuiltInParameter.PHY_MATERIAL_PARAM_WOOD_CONSTRUCTION, out construction) != null)
            {
               string constructionStr = GetConstructionString(construction);
               if (!string.IsNullOrEmpty(constructionStr))
                  SetSimpleMaterialProperty(file, properties, "Construction", constructionStr);
            }
         }

         if (assetClass == StructuralAssetClass.Concrete)
         {
            SetSimpleMaterialProperty(file, properties, SpecTypeId.Stress, "ConcreteCompression", structuralAsset.ConcreteCompression);
            SetSimpleMaterialProperty(file, properties, SpecTypeId.Number, "ShearStrengthModification", structuralAsset.ConcreteShearStrengthReduction);

            SetSimpleMaterialProperty(file, properties, "ThermallyTreated", structuralAsset.Lightweight);
         }

         return properties;
      }

      /// <summary>
      /// Creates Thermal material properties
      /// </summary>
      /// <param name="file"> The IFC file.</param>
      /// <param name="thermalSet">The thermal properety set element.</param>
      /// <returns>Set of exported properties.</returns>
      static HashSet<IFCAnyHandle> CreateThermalProperties(IFCFile file, PropertySetElement thermalSet)
      {
         if (file == null || thermalSet == null)
            return null;

         ThermalAsset thermalAsset = thermalSet?.GetThermalAsset();
         if (thermalAsset == null)
            return null;

         ThermalMaterialType materialType = thermalAsset.ThermalMaterialType;
         if (materialType == ThermalMaterialType.Undefined)
            return null;

         HashSet<IFCAnyHandle> properties = new HashSet<IFCAnyHandle>();

         StructuralBehavior behaviour = thermalAsset.Behavior;

         SetSimpleMaterialProperty(file, properties, "Name", thermalAsset.Name);
         SetSimpleMaterialProperty(file, properties, thermalSet, BuiltInParameter.PROPERTY_SET_DESCRIPTION, "Description");
         SetSimpleMaterialProperty(file, properties, thermalSet, BuiltInParameter.PROPERTY_SET_KEYWORDS, "Keywords");
         SetSimpleMaterialProperty(file, properties, "Type", materialType.ToString());
         SetSimpleMaterialProperty(file, properties, thermalSet, BuiltInParameter.PHY_MATERIAL_PARAM_SUBCLASS, "SubClass");
         SetSimpleMaterialProperty(file, properties, thermalSet, BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE, "Source");
         SetSimpleMaterialProperty(file, properties, thermalSet, BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE_URL, "Source URL");

         if (materialType == ThermalMaterialType.Solid && behaviour == StructuralBehavior.Orthotropic)
         {
            // ThermalConductivityX
            double thermalConductivityX;
            Parameter param = GetDoubleValueFromElement(thermalSet, BuiltInParameter.PHY_MATERIAL_PARAM_THERMAL_CONDUCTIVITY_X, out thermalConductivityX);
            if (param != null)
            {
               properties.Add(PropertyUtil.CreateRealPropertyBasedOnParameterType(file, param, 
                  new PropertyDescription("ThermalConductivityX"), thermalConductivityX, PropertyValueType.SingleValue));
            }

            // ThermalConductivityY
            double thermalConductivityY;
            param = GetDoubleValueFromElement(thermalSet, BuiltInParameter.PHY_MATERIAL_PARAM_THERMAL_CONDUCTIVITY_Y, out thermalConductivityY);
            if (param != null)
            {
               properties.Add(PropertyUtil.CreateRealPropertyBasedOnParameterType(file, param,
                  new PropertyDescription("ThermalConductivityY"), thermalConductivityY, PropertyValueType.SingleValue));
            }

            // ThermalConductivityZ
            double thermalConductivityZ;
            param = GetDoubleValueFromElement(thermalSet, BuiltInParameter.PHY_MATERIAL_PARAM_THERMAL_CONDUCTIVITY_Z, out thermalConductivityZ);
            if (param != null)
            {
               properties.Add(PropertyUtil.CreateRealPropertyBasedOnParameterType(file, param,
                  new PropertyDescription("ThermalConductivityZ"), thermalConductivityZ, PropertyValueType.SingleValue));
            }
         }

         SetSimpleMaterialProperty(file, properties, SpecTypeId.MassDensity, "Density", thermalAsset.Density);
         SetSimpleMaterialProperty(file, properties, SpecTypeId.Number, "Emissivity", thermalAsset.Emissivity);

         if (materialType == ThermalMaterialType.Gas || materialType == ThermalMaterialType.Liquid)
         {
            SetSimpleMaterialProperty(file, properties, SpecTypeId.Number, "Compressibility", thermalAsset.Compressibility);
         }

         if (materialType == ThermalMaterialType.Solid)
         {
            SetSimpleMaterialProperty(file, properties, "Behavior", behaviour.ToString());

            SetSimpleMaterialProperty(file, properties, "TransmitsLight", thermalAsset.TransmitsLight);

            SetSimpleMaterialProperty(file, properties, SpecTypeId.Permeability, "Permeability", thermalAsset.Permeability);
            SetSimpleMaterialProperty(file, properties, SpecTypeId.Number, "Reflectivity", thermalAsset.Reflectivity);
            SetSimpleMaterialProperty(file, properties, SpecTypeId.ElectricalResistivity, "ElectricalResistivity", thermalAsset.ElectricalResistivity);
         }

         if (materialType == ThermalMaterialType.Gas)
         {
            SetSimpleMaterialProperty(file, properties, SpecTypeId.HvacViscosity, "GasViscosity", thermalAsset.GasViscosity);
         }

         if (materialType == ThermalMaterialType.Liquid)
         {
            SetSimpleMaterialProperty(file, properties, SpecTypeId.HvacViscosity, "LiquidViscosity", thermalAsset.LiquidViscosity);
            SetSimpleMaterialProperty(file, properties, SpecTypeId.SpecificHeatOfVaporization, "SpecificHeatOfVaporization", thermalAsset.SpecificHeatOfVaporization);
            SetSimpleMaterialProperty(file, properties, SpecTypeId.HvacPressure, "VaporPressure", thermalAsset.VaporPressure);
         }

         return properties;
      }

      /// <summary>
      /// Creates material properties
      /// </summary>
      /// <param name="construction"> The construction number.</param>
      /// <returns>The construction string.</returns>
      static string GetConstructionString(int construction)
      {
         string constructionString = null;

         switch (construction)
         {
            case 0: constructionString = "Natural"; break;
            case 1: constructionString = "Glued"; break;
            case 2: constructionString = "Glued KertoS"; break;
            case 3: constructionString = "Glued KertoQ"; break;
            case 4: constructionString = "LVL"; break;
         }
         return constructionString;
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
         if (file == null || materialHnd == null || properties == null || properties.Count < 1)
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