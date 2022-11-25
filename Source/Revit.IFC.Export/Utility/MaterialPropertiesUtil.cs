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
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Toolkit;

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

               ExportMappedMaterialProperties(file, exporterIFC, material, materialHnd);
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

         // Name
         string name = material.Name;
         properties.Add(PropertyUtil.CreateLabelProperty(file, "Name", name, PropertyValueType.SingleValue, null));

         // Category
         name = material.MaterialCategory;
         properties.Add(PropertyUtil.CreateLabelProperty(file, "Category", name, PropertyValueType.SingleValue, null));

         // Class
         name = material.MaterialClass;
         properties.Add(PropertyUtil.CreateLabelProperty(file, "Class", name, PropertyValueType.SingleValue, null));

         // Description
         string strValue;
         ParameterUtil.GetStringValueFromElement(material, BuiltInParameter.ALL_MODEL_DESCRIPTION, out strValue);
         if (!string.IsNullOrEmpty(strValue))
            properties.Add(PropertyUtil.CreateLabelProperty(file, "Description", strValue, PropertyValueType.SingleValue, null));

         // Comments
         ParameterUtil.GetStringValueFromElement(material, BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS, out strValue);
         if (!string.IsNullOrEmpty(strValue))
            properties.Add(PropertyUtil.CreateLabelProperty(file, "Comments", strValue, PropertyValueType.SingleValue, null));

         // Manufacturer
         ParameterUtil.GetStringValueFromElement(material, BuiltInParameter.ALL_MODEL_MANUFACTURER, out strValue);
         if (!string.IsNullOrEmpty(strValue))
            properties.Add(PropertyUtil.CreateLabelProperty(file, "Manufacturer", strValue, PropertyValueType.SingleValue, null));

         // Model
         ParameterUtil.GetStringValueFromElement(material, BuiltInParameter.ALL_MODEL_MODEL, out strValue);
         if (!string.IsNullOrEmpty(strValue))
            properties.Add(PropertyUtil.CreateLabelProperty(file, "Model", strValue, PropertyValueType.SingleValue, null));

         // Cost
         ParameterUtil.GetStringValueFromElement(material, BuiltInParameter.ALL_MODEL_COST, out strValue);
         if (!string.IsNullOrEmpty(strValue))
            properties.Add(PropertyUtil.CreateLabelProperty(file, "Cost", strValue, PropertyValueType.SingleValue, null));

         // URL
         ParameterUtil.GetStringValueFromElement(material, BuiltInParameter.ALL_MODEL_URL, out strValue);
         if (!string.IsNullOrEmpty(strValue))
            properties.Add(PropertyUtil.CreateLabelProperty(file, "URL", strValue, PropertyValueType.SingleValue, null));

         // Keynote
         ParameterUtil.GetStringValueFromElement(material, BuiltInParameter.KEYNOTE_PARAM, out strValue);
         if (!string.IsNullOrEmpty(strValue))
            properties.Add(PropertyUtil.CreateLabelProperty(file, "Keynote", strValue, PropertyValueType.SingleValue, null));

         // Mark
         ParameterUtil.GetStringValueFromElement(material, BuiltInParameter.ALL_MODEL_MARK, out strValue);
         if (!string.IsNullOrEmpty(strValue))
            properties.Add(PropertyUtil.CreateLabelProperty(file, "Mark", strValue, PropertyValueType.SingleValue, null));

         return properties;
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

         // Name
         string strValue;
         ParameterUtil.GetStringValueFromElement(structuralSet, BuiltInParameter.PROPERTY_SET_NAME, out strValue);
         if (!string.IsNullOrEmpty(strValue))
            properties.Add(PropertyUtil.CreateLabelProperty(file, "Name", strValue, PropertyValueType.SingleValue, null));

         // Description
         ParameterUtil.GetStringValueFromElement(structuralSet, BuiltInParameter.PROPERTY_SET_DESCRIPTION, out strValue);
         if (!string.IsNullOrEmpty(strValue))
            properties.Add(PropertyUtil.CreateLabelProperty(file, "Description", strValue, PropertyValueType.SingleValue, null));

         // Keywords
         ParameterUtil.GetStringValueFromElement(structuralSet, BuiltInParameter.PROPERTY_SET_KEYWORDS, out strValue);
         if (!string.IsNullOrEmpty(strValue))
            properties.Add(PropertyUtil.CreateLabelProperty(file, "Keywords", strValue, PropertyValueType.SingleValue, null));

         // Type
         string type = assetClass.ToString();
         properties.Add(PropertyUtil.CreateLabelProperty(file, "Type", type, PropertyValueType.SingleValue, null));

         // SubClass
         string subClass = structuralAsset.SubClass;
         properties.Add(PropertyUtil.CreateLabelProperty(file, "SubClass", subClass, PropertyValueType.SingleValue, null));


         if (assetClass == StructuralAssetClass.Concrete || assetClass == StructuralAssetClass.Metal || assetClass == StructuralAssetClass.Generic
            || assetClass == StructuralAssetClass.Plastic || assetClass == StructuralAssetClass.Wood)
         {
            // Source
            ParameterUtil.GetStringValueFromElement(structuralSet, BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE, out strValue);
            if (!string.IsNullOrEmpty(strValue))
               properties.Add(PropertyUtil.CreateLabelProperty(file, "Source", strValue, PropertyValueType.SingleValue, null));

            // Source URL
            ParameterUtil.GetStringValueFromElement(structuralSet, BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE_URL, out strValue);
            if (!string.IsNullOrEmpty(strValue))
               properties.Add(PropertyUtil.CreateLabelProperty(file, "Source URL", strValue, PropertyValueType.SingleValue, null));
         }

         // Behavior
         string behaviourStr = behaviour.ToString();
         properties.Add(PropertyUtil.CreateLabelProperty(file, "Behavior", behaviourStr, PropertyValueType.SingleValue, null));

         if (assetClass != StructuralAssetClass.Basic)
         {
            // ThermalExpansionCoefficient X
            XYZ thermalExpansionCoefficientXYZ = structuralAsset.ThermalExpansionCoefficient;
            if ((assetClass == StructuralAssetClass.Metal || assetClass == StructuralAssetClass.Concrete || assetClass == StructuralAssetClass.Generic || assetClass == StructuralAssetClass.Plastic || assetClass == StructuralAssetClass.Wood) && behaviour != StructuralBehavior.Isotropic)
            {
               string thermalExpansionCoefficientName = (behaviour == StructuralBehavior.Orthotropic) ? "ThermalExpansionCoefficientX" : "ThermalExpansionCoefficient1";
               properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.ThermalExpansionCoefficient, thermalExpansionCoefficientName, thermalExpansionCoefficientXYZ.X, PropertyValueType.SingleValue));
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
               properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.ThermalExpansionCoefficient, thermalExpansionCoefficientName, thermalExpansionCoefficientXYZ.Y, PropertyValueType.SingleValue));
            }

            // ThermalExpansionCoefficient Z
            if (behaviour == StructuralBehavior.Orthotropic)
               properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.ThermalExpansionCoefficient, "ThermalExpansionCoefficientZ", thermalExpansionCoefficientXYZ.Z, PropertyValueType.SingleValue));

            // YoungModulus X
            XYZ youngModulusXYZ = structuralAsset.YoungModulus;
            if (behaviour != StructuralBehavior.Isotropic)
            {
               string youngModulusNameX = (behaviour == StructuralBehavior.Orthotropic) ? "YoungModulusX" : "YoungModulus1";
               properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.Stress, youngModulusNameX, youngModulusXYZ.X, PropertyValueType.SingleValue));
            }
            // YoungModulus Y
            if (behaviour == StructuralBehavior.Orthotropic || behaviour == StructuralBehavior.TransverseIsotropic)
            {
               string youngModulusNameY = (behaviour == StructuralBehavior.Orthotropic) ? "YoungModulusY" : "YoungModulus2";
               properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.Stress, youngModulusNameY, youngModulusXYZ.Y, PropertyValueType.SingleValue));
            }

            // YoungModulus Z
            if (behaviour == StructuralBehavior.Orthotropic)
               properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.Stress, "YoungModulusZ", youngModulusXYZ.Z, PropertyValueType.SingleValue));

            XYZ poissonRatioXYZ = structuralAsset.PoissonRatio;
            if (behaviour != StructuralBehavior.Isotropic)
            {
               // PoissonRatio X
               string poissonRatioNameX = (behaviour == StructuralBehavior.Orthotropic) ? "PoissonRatioX" : "PoissonRatio12";
               properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.Number, poissonRatioNameX, poissonRatioXYZ.X, PropertyValueType.SingleValue));
            }

            // PoissonRatio Y
            if (behaviour == StructuralBehavior.Orthotropic || behaviour == StructuralBehavior.TransverseIsotropic)
            {
               string poissonRatioNameY = (behaviour == StructuralBehavior.Orthotropic) ? "PoissonRatioY" : "PoissonRatio23";
               properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.Number, poissonRatioNameY, poissonRatioXYZ.Y, PropertyValueType.SingleValue));
            }

            // PoissonRatio Z
            if (behaviour == StructuralBehavior.Orthotropic)
               properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.Number, "PoissonRatioZ", poissonRatioXYZ.Z, PropertyValueType.SingleValue));

            // ShearModulus X
            XYZ shearModulusXYZ = structuralAsset.ShearModulus;
            if (behaviour != StructuralBehavior.Isotropic)
            {
               string shearModulusName = (behaviour == StructuralBehavior.Orthotropic) ? "ShearModulusX" : "ShearModulus12";
               properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.Stress, shearModulusName, shearModulusXYZ.X, PropertyValueType.SingleValue));
            }

            // ShearModulus Y
            if (behaviour == StructuralBehavior.Orthotropic)
               properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.Stress, "ShearModulusY", shearModulusXYZ.Y, PropertyValueType.SingleValue));

            // ShearModulus Z
            if (behaviour == StructuralBehavior.Orthotropic)
               properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.Stress, "ShearModulusZ", shearModulusXYZ.Z, PropertyValueType.SingleValue));

            // TensileStrength
            double minimumTensileStrength = structuralAsset.MinimumTensileStrength;
            properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.Stress, "TensileStrength", minimumTensileStrength, PropertyValueType.SingleValue));

         }

         if (assetClass == StructuralAssetClass.Metal)
         {
            // MetalThermallyTreated
            bool metalThermallyTreated = structuralAsset.MetalThermallyTreated;
            properties.Add(PropertyUtil.CreateBooleanProperty(file, "ThermallyTreated", metalThermallyTreated, PropertyValueType.SingleValue));
         }

         if (assetClass == StructuralAssetClass.Wood)
         {
            // WoodSpecies
            string woodSpecies = structuralAsset.WoodSpecies;
            properties.Add(PropertyUtil.CreateLabelProperty(file, "Species", woodSpecies, PropertyValueType.SingleValue, null));

            // WoodGrade
            string woodGrade = structuralAsset.WoodGrade;
            properties.Add(PropertyUtil.CreateLabelProperty(file, "WoodGrade", woodGrade, PropertyValueType.SingleValue, null));

            // WoodBendingStrength
            double woodBendingStrength = structuralAsset.WoodBendingStrength;
            properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.Stress, "Bending", woodBendingStrength, PropertyValueType.SingleValue));

            // WoodParallelCompressionStrength
            double woodParallelCompressionStrength = structuralAsset.WoodParallelCompressionStrength;
            properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.Stress, "CompressionParalleltoGrain", woodParallelCompressionStrength, PropertyValueType.SingleValue));

            // WoodPerpendicularCompressionStrength
            double woodPerpendicularCompressionStrength = structuralAsset.WoodPerpendicularCompressionStrength;
            properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.Stress, "CompressionPerpendiculartoGrain", woodPerpendicularCompressionStrength, PropertyValueType.SingleValue));

            // WoodParallelShearStrength
            double woodParallelShearStrength = structuralAsset.WoodParallelShearStrength;
            properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.Stress, "ShearParallelToGrain", woodParallelShearStrength, PropertyValueType.SingleValue));

            // TensionParallelToGrain
            double tensionParallelToGrain;
            Parameter param = ParameterUtil.GetDoubleValueFromElement(structuralSet, BuiltInParameter.PHY_MATERIAL_PARAM_TENSION_PARALLEL, out tensionParallelToGrain);
            if (param != null)
               properties.Add(PropertyUtil.CreateRealPropertyBasedOnParameterType(file, param, "TensionParallelToGrain", tensionParallelToGrain, PropertyValueType.SingleValue));

            // TensionPerpendicularToGrain
            double tensionPerpendicularToGrain;
            param = ParameterUtil.GetDoubleValueFromElement(structuralSet, BuiltInParameter.PHY_MATERIAL_PARAM_TENSION_PERPENDICULAR, out tensionPerpendicularToGrain);
            if (param != null)
               properties.Add(PropertyUtil.CreateRealPropertyBasedOnParameterType(file, param, "TensionPerpendicularToGrain", tensionPerpendicularToGrain, PropertyValueType.SingleValue));

            // AverageModulus
            double averageModulus;
            param = ParameterUtil.GetDoubleValueFromElement(structuralSet, BuiltInParameter.PHY_MATERIAL_PARAM_AVERAGE_MODULUS, out averageModulus);
            if (param != null)
               properties.Add(PropertyUtil.CreateRealPropertyBasedOnParameterType(file, param, "AverageModulus", averageModulus, PropertyValueType.SingleValue));

            // Construction
            int construction;
            if (ParameterUtil.GetIntValueFromElement(structuralSet, BuiltInParameter.PHY_MATERIAL_PARAM_WOOD_CONSTRUCTION, out construction) != null)
            {
               string constructionStr = GetConstructionString(construction);
               if (!string.IsNullOrEmpty(strValue))
                  properties.Add(PropertyUtil.CreateLabelProperty(file, "Construction", constructionStr, PropertyValueType.SingleValue, null));
            }
         }

         if (assetClass == StructuralAssetClass.Concrete)
         {
            // ConcreteCompression
            double concreteCompression = structuralAsset.ConcreteCompression;
            properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.Stress, "ConcreteCompression", concreteCompression, PropertyValueType.SingleValue));

            // ConcreteShearStrengthReduction
            double woodParallelCompressionStrength = structuralAsset.ConcreteShearStrengthReduction;
            properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.Number, "ShearStrengthModification", woodParallelCompressionStrength, PropertyValueType.SingleValue));

            // Lightweight
            bool lightweight = structuralAsset.Lightweight;
            properties.Add(PropertyUtil.CreateBooleanProperty(file, "ThermallyTreated", lightweight, PropertyValueType.SingleValue));
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

         // Name        
         string name = thermalAsset.Name;
         properties.Add(PropertyUtil.CreateLabelProperty(file, "Name", name, PropertyValueType.SingleValue, null));

         // Description
         string strValue;
         ParameterUtil.GetStringValueFromElement(thermalSet, BuiltInParameter.PROPERTY_SET_DESCRIPTION, out strValue);
         if (!string.IsNullOrEmpty(strValue))
            properties.Add(PropertyUtil.CreateLabelProperty(file, "Description", strValue, PropertyValueType.SingleValue, null));

         // Keywords
         ParameterUtil.GetStringValueFromElement(thermalSet, BuiltInParameter.PROPERTY_SET_KEYWORDS, out strValue);
         if (!string.IsNullOrEmpty(strValue))
            properties.Add(PropertyUtil.CreateLabelProperty(file, "Keywords", strValue, PropertyValueType.SingleValue, null));

         // Type
         string type = materialType.ToString();
         if (!string.IsNullOrEmpty(strValue))
            properties.Add(PropertyUtil.CreateLabelProperty(file, "Type", type, PropertyValueType.SingleValue, null));

         // SubClass 
         ParameterUtil.GetStringValueFromElement(thermalSet, BuiltInParameter.PHY_MATERIAL_PARAM_SUBCLASS, out strValue);
         if (!string.IsNullOrEmpty(strValue))
            properties.Add(PropertyUtil.CreateLabelProperty(file, "SubClass", strValue, PropertyValueType.SingleValue, null));

         // Source
         ParameterUtil.GetStringValueFromElement(thermalSet, BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE, out strValue);
         if (!string.IsNullOrEmpty(strValue))
            properties.Add(PropertyUtil.CreateLabelProperty(file, "Source", strValue, PropertyValueType.SingleValue, null));

         // Source URL
         ParameterUtil.GetStringValueFromElement(thermalSet, BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE_URL, out strValue);
         if (!string.IsNullOrEmpty(strValue))
            properties.Add(PropertyUtil.CreateLabelProperty(file, "Source URL", strValue, PropertyValueType.SingleValue, null));


         if (materialType == ThermalMaterialType.Solid && behaviour == StructuralBehavior.Orthotropic)
         {
            // ThermalConductivityX
            double thermalConductivityX;
            Parameter param = ParameterUtil.GetDoubleValueFromElement(thermalSet, BuiltInParameter.PHY_MATERIAL_PARAM_THERMAL_CONDUCTIVITY_X, out thermalConductivityX);
            if (param != null)
               properties.Add(PropertyUtil.CreateRealPropertyBasedOnParameterType(file, param, "ThermalConductivityX", thermalConductivityX, PropertyValueType.SingleValue));

            // ThermalConductivityY
            double thermalConductivityY;
            param = ParameterUtil.GetDoubleValueFromElement(thermalSet, BuiltInParameter.PHY_MATERIAL_PARAM_THERMAL_CONDUCTIVITY_Y, out thermalConductivityY);
            if (param != null)
               properties.Add(PropertyUtil.CreateRealPropertyBasedOnParameterType(file, param, "ThermalConductivityY", thermalConductivityY, PropertyValueType.SingleValue));

            // ThermalConductivityZ
            double thermalConductivityZ;
            param = ParameterUtil.GetDoubleValueFromElement(thermalSet, BuiltInParameter.PHY_MATERIAL_PARAM_THERMAL_CONDUCTIVITY_Z, out thermalConductivityZ);
            if (param != null)
               properties.Add(PropertyUtil.CreateRealPropertyBasedOnParameterType(file, param, "ThermalConductivityZ", thermalConductivityZ, PropertyValueType.SingleValue));
         }

         // Density
         double density = thermalAsset.Density;
         properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.MassDensity, "Density", density, PropertyValueType.SingleValue));

         // Emissivity
         double emissivity = thermalAsset.Emissivity;
         properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.Number, "Emissivity", emissivity, PropertyValueType.SingleValue));


         if (materialType == ThermalMaterialType.Gas || materialType == ThermalMaterialType.Liquid)
         {
            // Compressibility
            double compressibility = thermalAsset.Compressibility;
            properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.Number, "Compressibility", compressibility, PropertyValueType.SingleValue));
         }

         if (materialType == ThermalMaterialType.Solid)
         {
            // Behavior
            string behaviourStr = behaviour.ToString();
            properties.Add(PropertyUtil.CreateLabelProperty(file, "Behavior", behaviourStr, PropertyValueType.SingleValue, null));

            // TransmitsLight
            bool transmitsLight = thermalAsset.TransmitsLight;
            properties.Add(PropertyUtil.CreateBooleanProperty(file, "TransmitsLight", transmitsLight, PropertyValueType.SingleValue));

            // Permeability
            double permeability = thermalAsset.Permeability;
            properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.Permeability, "Permeability", permeability, PropertyValueType.SingleValue));

            // Reflectivity
            double reflectivity = thermalAsset.Reflectivity;
            properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.Number, "Reflectivity", reflectivity, PropertyValueType.SingleValue));

            // ElectricalResistivity
            double electricalResistivity = thermalAsset.ElectricalResistivity;
            properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.ElectricalResistivity, "ElectricalResistivity", electricalResistivity, PropertyValueType.SingleValue));
         }

         if (materialType == ThermalMaterialType.Gas)
         {
            // GasViscosity
            double gasViscosity = thermalAsset.GasViscosity;
            properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.HvacViscosity, "GasViscosity", gasViscosity, PropertyValueType.SingleValue));
         }

         if (materialType == ThermalMaterialType.Liquid)
         {
            // LiquidViscosity
            double liquidViscosity = thermalAsset.LiquidViscosity;
            properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.HvacViscosity, "LiquidViscosity", liquidViscosity, PropertyValueType.SingleValue));

            // SpecificHeatOfVaporization
            double specificHeatOfVaporization = thermalAsset.SpecificHeatOfVaporization;
            properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.SpecificHeatOfVaporization, "SpecificHeatOfVaporization", specificHeatOfVaporization, PropertyValueType.SingleValue));

            // VaporPressure
            double vaporPressure = thermalAsset.VaporPressure;
            properties.Add(PropertyUtil.CreateRealPropertyByType(file, SpecTypeId.HvacPressure, "VaporPressure", vaporPressure, PropertyValueType.SingleValue));
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
      static void ExportGenericMaterialPropertySet(IFCFile file, IFCAnyHandle materialHnd, ISet<IFCAnyHandle> properties, string description, string name)
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
            IList<IList<PreDefinedPropertySetDescription>> psetsToCreate = ExporterCacheManager.ParameterCache.PreDefinedPropertySets;
            IList<PreDefinedPropertySetDescription> currPsetsToCreate = ExporterUtil.GetCurrPreDefinedPSetsToCreate(materialHnd, psetsToCreate);

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
            IList<IList<PropertySetDescription>> psetsToCreate = ExporterCacheManager.ParameterCache.PropertySets;
            IList<PropertySetDescription> currPsetsToCreate = ExporterUtil.GetCurrPSetsToCreate(materialHnd, psetsToCreate);

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
   /// Provides static methods for export buildIn material properties to specifict ifc entities.
   /// </summary>
   public class MaterialBuildInParameterUtil
   {
      // Dictionary of properties to export to specific IFC entities
      // Each property has: list of property sets and function to extract the value
      static readonly Dictionary<string, Tuple<IList<string>, Func<Material, double?>>> materialBuildInSet = new Dictionary<string, Tuple<IList<string>, Func<Material, double?>>>
      {
         { "MassDensity",          new Tuple<IList<string>, Func<Material, double?>>(new List<string>{ "Pset_MaterialCommon", "IfcGeneralMaterialProperties"}, getBuildInMassDensity) },
         { "Porosity",             new Tuple<IList<string>, Func<Material, double?>>(new List<string>{ "Pset_MaterialCommon", "IfcGeneralMaterialProperties"}, getBuildInPorosity) },
         { "SpecificHeatCapacity", new Tuple<IList<string>, Func<Material, double?>>(new List<string>{ "Pset_MaterialThermal", "IfcThermalMaterialProperties"}, getBuildInSpecificHeatCapacity) },
         { "ThermalConductivity",  new Tuple<IList<string>, Func<Material, double?>>(new List<string>{ "Pset_MaterialThermal", "IfcThermalMaterialProperties"}, getBuildInThermalConductivity) },
         { "YieldStress",          new Tuple<IList<string>, Func<Material, double?>>(new List<string>{ "Pset_MaterialSteel", "IfcMechanicalSteelMaterialProperties"}, getBuildInYieldStress) },
         { "PoissonRatio",         new Tuple<IList<string>, Func<Material, double?>>(new List<string>{ "Pset_MaterialMechanical", "IfcMechanicalMaterialProperties"}, getBuildInPoissonRatio) },
         { "YoungModulus",         new Tuple<IList<string>, Func<Material, double?>>(new List<string>{ "Pset_MaterialMechanical", "IfcMechanicalMaterialProperties"}, getBuildInYoungModulus) },
         { "ShearModulus",         new Tuple<IList<string>, Func<Material, double?>>(new List<string>{ "Pset_MaterialMechanical", "IfcMechanicalMaterialProperties"}, getBuildInShearModulus) },
         { "ThermalExpansionCoefficient", new Tuple<IList<string>, Func<Material, double?>>(new List<string>{ "Pset_MaterialMechanical", "IfcMechanicalMaterialProperties"}, getBuildInThermalExpansionCoefficient) }
      };

      /// <summary>
      /// Get MassDensity value from material
      /// </summary>
      /// <param name="material">The material.</param>
      /// <returns>nullable value.</returns>
      static double? getBuildInMassDensity(Material material)
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
      static double? getBuildInPorosity(Material material)
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
      static double? getBuildInSpecificHeatCapacity(Material material)
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
      static double? getBuildInThermalConductivity(Material material)
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
      static double? getBuildInYieldStress(Material material)
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
      static double? getBuildInPoissonRatio(Material material)
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
      static double? getBuildInYoungModulus(Material material)
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
      static double? getBuildInShearModulus(Material material)
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
      static double? getBuildInThermalExpansionCoefficient(Material material)
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
      /// <returns>True if it is to export as material buildIn parameter.</returns>
      public static bool isMaterialBuildInParameter(string propertyName)
      {
         return materialBuildInSet.ContainsKey(propertyName);
      }

      /// <summary>
      /// Create material property data if it is built in
      /// </summary>
      /// <param name="psetName">The material.</param>
      /// <param name="propertyName">The material.</param>
      /// <param name="propertyType">The material.</param>
      /// <param name="element">The material.</param>
      /// <returns>Material data.</returns>
      public static IList<IFCData> CreatePredefinedDataIfBuildIn(string psetName, string propertyName, PropertyType propertyType, Element element)
      {
         IList<IFCData> data = null;
         if (isMaterialBuildInParameter(propertyName))
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
      public static IFCAnyHandle CreateMaterialPropertyIfBuildIn(string psetName, string propertyName, PropertyType propertyType, Element element, IFCFile file)
      {
         IFCAnyHandle hnd = null;
         if (isMaterialBuildInParameter(propertyName))
         {
            IFCData data = CreateMaterialDataFromParameter(psetName, propertyName, propertyType, element);
            if (data != null)
               hnd = PropertyUtil.CreateCommonProperty(file, propertyName, data, PropertyValueType.SingleValue, null);
         }
         return hnd;
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
         if (materialBuildInSet.TryGetValue(propertyName, out var parameterInfo))
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