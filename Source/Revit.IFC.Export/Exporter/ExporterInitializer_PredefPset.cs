/********************************************************************************************************************************
** NOTE: This code is generated from IFC psd files automatically by RevitIFCTools.                                            **
**       DO NOT change it manually as it will be overwritten the next time this file is re-generated!!                        **
********************************************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.ApplicationServices;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Exporter.PropertySet.Calculators;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   partial class ExporterInitializer
   {
      public static void InitPreDefinedPropertySets(IList<IList<PreDefinedPropertySetDescription>> allPsetOrQtoSets)
      {
         IList<PreDefinedPropertySetDescription> theSets = new List<PreDefinedPropertySetDescription>();
         InitIfcDoorLiningProperties(theSets);
         InitIfcDoorPanelProperties(theSets);
         InitIfcFuelProperties(theSets);
         InitIfcGeneralMaterialProperties(theSets);
         InitIfcHygroscopicMaterialProperties(theSets);
         InitIfcMechanicalConcreteMaterialProperties(theSets);
         InitIfcMechanicalMaterialProperties(theSets);
         InitIfcMechanicalSteelMaterialProperties(theSets);
         InitIfcOpticalMaterialProperties(theSets);
         InitIfcPermeableCoveringProperties(theSets);
         InitIfcProductsOfCombustionProperties(theSets);
         InitIfcReinforcementDefinitionProperties(theSets);
         InitIfcThermalMaterialProperties(theSets);
         InitIfcWaterProperties(theSets);
         InitIfcWindowLiningProperties(theSets);
         InitIfcWindowPanelProperties(theSets);

         allPsetOrQtoSets.Add(theSets);
      }

      private static void InitIfcDoorLiningProperties(IList<PreDefinedPropertySetDescription> commonPropertySets)
      {
         PreDefinedPropertySetDescription IfcDoorLiningProperties = new PreDefinedPropertySetDescription();
         IfcDoorLiningProperties.Name = "IfcDoorLiningProperties";
         PreDefinedPropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcDoorLiningProperties"))
         {
            IfcDoorLiningProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            IfcDoorLiningProperties.EntityTypes.Add(IFCEntityType.IfcDoorStyle);
            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.LiningDepth", "LiningDepth");
            ifcPSE.PropertyName = "LiningDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.ThresholdDepth", "ThresholdDepth");
            ifcPSE.PropertyName = "ThresholdDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThresholdDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.TransomOffset", "TransomOffset");
            ifcPSE.PropertyName = "TransomOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.LiningOffset", "LiningOffset");
            ifcPSE.PropertyName = "LiningOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.ThresholdOffset", "ThresholdOffset");
            ifcPSE.PropertyName = "ThresholdOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThresholdOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.CasingThickness", "CasingThickness");
            ifcPSE.PropertyName = "CasingThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CasingThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.CasingDepth", "CasingDepth");
            ifcPSE.PropertyName = "CasingDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CasingDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.LiningThickness", "LiningThickness");
            ifcPSE.PropertyName = "LiningThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.ThresholdThickness", "ThresholdThickness");
            ifcPSE.PropertyName = "ThresholdThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThresholdThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.TransomThickness", "TransomThickness");
            ifcPSE.PropertyName = "TransomThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TransomThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcDoorLiningProperties"))
         {
            IfcDoorLiningProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            IfcDoorLiningProperties.EntityTypes.Add(IFCEntityType.IfcDoorStyle);
            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.LiningDepth", "LiningDepth");
            ifcPSE.PropertyName = "LiningDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.ThresholdDepth", "ThresholdDepth");
            ifcPSE.PropertyName = "ThresholdDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThresholdDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.TransomOffset", "TransomOffset");
            ifcPSE.PropertyName = "TransomOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.LiningOffset", "LiningOffset");
            ifcPSE.PropertyName = "LiningOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.ThresholdOffset", "ThresholdOffset");
            ifcPSE.PropertyName = "ThresholdOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThresholdOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.CasingThickness", "CasingThickness");
            ifcPSE.PropertyName = "CasingThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CasingThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.CasingDepth", "CasingDepth");
            ifcPSE.PropertyName = "CasingDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CasingDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.LiningThickness", "LiningThickness");
            ifcPSE.PropertyName = "LiningThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.ThresholdThickness", "ThresholdThickness");
            ifcPSE.PropertyName = "ThresholdThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThresholdThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.TransomThickness", "TransomThickness");
            ifcPSE.PropertyName = "TransomThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TransomThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcDoorLiningProperties"))
         {
            IfcDoorLiningProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            IfcDoorLiningProperties.EntityTypes.Add(IFCEntityType.IfcDoorType);
            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.LiningDepth", "LiningDepth");
            ifcPSE.PropertyName = "LiningDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.ThresholdDepth", "ThresholdDepth");
            ifcPSE.PropertyName = "ThresholdDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThresholdDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.TransomOffset", "TransomOffset");
            ifcPSE.PropertyName = "TransomOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.LiningOffset", "LiningOffset");
            ifcPSE.PropertyName = "LiningOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.ThresholdOffset", "ThresholdOffset");
            ifcPSE.PropertyName = "ThresholdOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThresholdOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.CasingThickness", "CasingThickness");
            ifcPSE.PropertyName = "CasingThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CasingThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.CasingDepth", "CasingDepth");
            ifcPSE.PropertyName = "CasingDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CasingDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.LiningThickness", "LiningThickness");
            ifcPSE.PropertyName = "LiningThickness";
            ifcPSE.PropertyType = PropertyType.NonNegativeLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.ThresholdThickness", "ThresholdThickness");
            ifcPSE.PropertyName = "ThresholdThickness";
            ifcPSE.PropertyType = PropertyType.NonNegativeLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThresholdThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.TransomThickness", "TransomThickness");
            ifcPSE.PropertyName = "TransomThickness";
            ifcPSE.PropertyType = PropertyType.NonNegativeLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TransomThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.LiningToPanelOffsetX", "LiningToPanelOffsetX");
            ifcPSE.PropertyName = "LiningToPanelOffsetX";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningToPanelOffsetXCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.LiningToPanelOffsetY", "LiningToPanelOffsetY");
            ifcPSE.PropertyName = "LiningToPanelOffsetY";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningToPanelOffsetYCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcDoorLiningProperties"))
         {
            IfcDoorLiningProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.LiningDepth", "LiningDepth");
            ifcPSE.PropertyName = "LiningDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.ThresholdDepth", "ThresholdDepth");
            ifcPSE.PropertyName = "ThresholdDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThresholdDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.TransomOffset", "TransomOffset");
            ifcPSE.PropertyName = "TransomOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.LiningOffset", "LiningOffset");
            ifcPSE.PropertyName = "LiningOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.ThresholdOffset", "ThresholdOffset");
            ifcPSE.PropertyName = "ThresholdOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThresholdOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.CasingThickness", "CasingThickness");
            ifcPSE.PropertyName = "CasingThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CasingThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorLiningProperties.CasingDepth", "CasingDepth");
            ifcPSE.PropertyName = "CasingDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CasingDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            commonPropertySets.Add(IfcDoorLiningProperties);
         }
      }


      private static void InitIfcDoorPanelProperties(IList<PreDefinedPropertySetDescription> commonPropertySets)
      {
         PreDefinedPropertySetDescription IfcDoorPanelProperties = new PreDefinedPropertySetDescription();
         IfcDoorPanelProperties.Name = "IfcDoorPanelProperties";
         PreDefinedPropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcDoorPanelProperties"))
         {
            IfcDoorPanelProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            IfcDoorPanelProperties.EntityTypes.Add(IFCEntityType.IfcDoorStyle);
            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorPanelProperties.PanelDepth", "PanelDepth");
            ifcPSE.PropertyName = "PanelDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorPanelProperties.PanelOperation", "PanelOperation");
            ifcPSE.PropertyName = "PanelOperation";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelOperationCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorPanelProperties.PanelWidth", "PanelWidth");
            ifcPSE.PropertyName = "PanelWidth";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelWidthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorPanelProperties.PanelPosition", "PanelPosition");
            ifcPSE.PropertyName = "PanelPosition";
            ifcPSE.PropertyType = PropertyType.Label;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelPositionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcDoorPanelProperties"))
         {
            IfcDoorPanelProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            IfcDoorPanelProperties.EntityTypes.Add(IFCEntityType.IfcDoorStyle);
            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorPanelProperties.PanelDepth", "PanelDepth");
            ifcPSE.PropertyName = "PanelDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorPanelProperties.PanelOperation", "PanelOperation");
            ifcPSE.PropertyName = "PanelOperation";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelOperationCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorPanelProperties.PanelWidth", "PanelWidth");
            ifcPSE.PropertyName = "PanelWidth";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelWidthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorPanelProperties.PanelPosition", "PanelPosition");
            ifcPSE.PropertyName = "PanelPosition";
            ifcPSE.PropertyType = PropertyType.Label;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelPositionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcDoorPanelProperties"))
         {
            IfcDoorPanelProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            IfcDoorPanelProperties.EntityTypes.Add(IFCEntityType.IfcDoorType);
            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorPanelProperties.PanelDepth", "PanelDepth");
            ifcPSE.PropertyName = "PanelDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorPanelProperties.PanelOperation", "PanelOperation");
            ifcPSE.PropertyName = "PanelOperation";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelOperationCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorPanelProperties.PanelWidth", "PanelWidth");
            ifcPSE.PropertyName = "PanelWidth";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelWidthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorPanelProperties.PanelPosition", "PanelPosition");
            ifcPSE.PropertyName = "PanelPosition";
            ifcPSE.PropertyType = PropertyType.Label;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelPositionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcDoorPanelProperties"))
         {
            IfcDoorPanelProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorPanelProperties.PanelDepth", "PanelDepth");
            ifcPSE.PropertyName = "PanelDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorPanelProperties.PanelOperation", "PanelOperation");
            ifcPSE.PropertyName = "PanelOperation";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelOperationCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorPanelProperties.PanelWidth", "PanelWidth");
            ifcPSE.PropertyName = "PanelWidth";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelWidthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcDoorPanelProperties.PanelPosition", "PanelPosition");
            ifcPSE.PropertyName = "PanelPosition";
            ifcPSE.PropertyType = PropertyType.Label;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelPositionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            commonPropertySets.Add(IfcDoorPanelProperties);
         }
      }


      private static void InitIfcFuelProperties(IList<PreDefinedPropertySetDescription> commonPropertySets)
      {
         PreDefinedPropertySetDescription IfcFuelProperties = new PreDefinedPropertySetDescription();
         IfcFuelProperties.Name = "IfcFuelProperties";
         PreDefinedPropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcFuelProperties"))
         {
            IfcFuelProperties.EntityTypes.Add(IFCEntityType.IfcMaterial);
            ifcPSE = new PreDefinedPropertySetEntry("IfcFuelProperties.CombustionTemperature", "CombustionTemperature");
            ifcPSE.PropertyName = "CombustionTemperature";
            ifcPSE.PropertyType = PropertyType.ThermodynamicTemperature;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CombustionTemperatureCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcFuelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcFuelProperties.CarbonContent", "CarbonContent");
            ifcPSE.PropertyName = "CarbonContent";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CarbonContentCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcFuelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcFuelProperties.LowerHeatingValue", "LowerHeatingValue");
            ifcPSE.PropertyName = "LowerHeatingValue";
            ifcPSE.PropertyType = PropertyType.HeatingValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LowerHeatingValueCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcFuelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcFuelProperties.HigherHeatingValue", "HigherHeatingValue");
            ifcPSE.PropertyName = "HigherHeatingValue";
            ifcPSE.PropertyType = PropertyType.HeatingValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.HigherHeatingValueCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcFuelProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcFuelProperties"))
         {
            IfcFuelProperties.EntityTypes.Add(IFCEntityType.IfcMaterial);
            ifcPSE = new PreDefinedPropertySetEntry("IfcFuelProperties.CombustionTemperature", "CombustionTemperature");
            ifcPSE.PropertyName = "CombustionTemperature";
            ifcPSE.PropertyType = PropertyType.ThermodynamicTemperature;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CombustionTemperatureCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcFuelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcFuelProperties.CarbonContent", "CarbonContent");
            ifcPSE.PropertyName = "CarbonContent";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CarbonContentCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcFuelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcFuelProperties.LowerHeatingValue", "LowerHeatingValue");
            ifcPSE.PropertyName = "LowerHeatingValue";
            ifcPSE.PropertyType = PropertyType.HeatingValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LowerHeatingValueCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcFuelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcFuelProperties.HigherHeatingValue", "HigherHeatingValue");
            ifcPSE.PropertyName = "HigherHeatingValue";
            ifcPSE.PropertyType = PropertyType.HeatingValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.HigherHeatingValueCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcFuelProperties.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            commonPropertySets.Add(IfcFuelProperties);
         }
      }


      private static void InitIfcGeneralMaterialProperties(IList<PreDefinedPropertySetDescription> commonPropertySets)
      {
         PreDefinedPropertySetDescription IfcGeneralMaterialProperties = new PreDefinedPropertySetDescription();
         IfcGeneralMaterialProperties.Name = "IfcGeneralMaterialProperties";
         PreDefinedPropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcGeneralMaterialProperties"))
         {
            IfcGeneralMaterialProperties.EntityTypes.Add(IFCEntityType.IfcMaterial);
            ifcPSE = new PreDefinedPropertySetEntry("IfcGeneralMaterialProperties.MolecularWeight", "MolecularWeight");
            ifcPSE.PropertyName = "MolecularWeight";
            ifcPSE.PropertyType = PropertyType.MolecularWeight;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.MolecularWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcGeneralMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcGeneralMaterialProperties.Porosity", "Porosity");
            ifcPSE.PropertyName = "Porosity";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PorosityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcGeneralMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcGeneralMaterialProperties.MassDensity", "MassDensity");
            ifcPSE.PropertyName = "MassDensity";
            ifcPSE.PropertyType = PropertyType.MassDensity;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.MassDensityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcGeneralMaterialProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcGeneralMaterialProperties"))
         {
            IfcGeneralMaterialProperties.EntityTypes.Add(IFCEntityType.IfcMaterial);
            ifcPSE = new PreDefinedPropertySetEntry("IfcGeneralMaterialProperties.MolecularWeight", "MolecularWeight");
            ifcPSE.PropertyName = "MolecularWeight";
            ifcPSE.PropertyType = PropertyType.MolecularWeight;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.MolecularWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcGeneralMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcGeneralMaterialProperties.Porosity", "Porosity");
            ifcPSE.PropertyName = "Porosity";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PorosityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcGeneralMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcGeneralMaterialProperties.MassDensity", "MassDensity");
            ifcPSE.PropertyName = "MassDensity";
            ifcPSE.PropertyType = PropertyType.MassDensity;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.MassDensityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcGeneralMaterialProperties.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            commonPropertySets.Add(IfcGeneralMaterialProperties);
         }
      }


      private static void InitIfcHygroscopicMaterialProperties(IList<PreDefinedPropertySetDescription> commonPropertySets)
      {
         PreDefinedPropertySetDescription IfcHygroscopicMaterialProperties = new PreDefinedPropertySetDescription();
         IfcHygroscopicMaterialProperties.Name = "IfcHygroscopicMaterialProperties";
         PreDefinedPropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcHygroscopicMaterialProperties"))
         {
            IfcHygroscopicMaterialProperties.EntityTypes.Add(IFCEntityType.IfcMaterial);
            ifcPSE = new PreDefinedPropertySetEntry("IfcHygroscopicMaterialProperties.UpperVaporResistanceFactor", "UpperVaporResistanceFactor");
            ifcPSE.PropertyName = "UpperVaporResistanceFactor";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.UpperVaporResistanceFactorCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcHygroscopicMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcHygroscopicMaterialProperties.LowerVaporResistanceFactor", "LowerVaporResistanceFactor");
            ifcPSE.PropertyName = "LowerVaporResistanceFactor";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LowerVaporResistanceFactorCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcHygroscopicMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcHygroscopicMaterialProperties.IsothermalMoistureCapacity", "IsothermalMoistureCapacity");
            ifcPSE.PropertyName = "IsothermalMoistureCapacity";
            ifcPSE.PropertyType = PropertyType.IsothermalMoistureCapacity;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.IsothermalMoistureCapacityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcHygroscopicMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcHygroscopicMaterialProperties.VaporPermeability", "VaporPermeability");
            ifcPSE.PropertyName = "VaporPermeability";
            ifcPSE.PropertyType = PropertyType.VaporPermeability;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.VaporPermeabilityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcHygroscopicMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcHygroscopicMaterialProperties.MoistureDiffusivity", "MoistureDiffusivity");
            ifcPSE.PropertyName = "MoistureDiffusivity";
            ifcPSE.PropertyType = PropertyType.MoistureDiffusivity;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.MoistureDiffusivityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcHygroscopicMaterialProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcHygroscopicMaterialProperties"))
         {
            IfcHygroscopicMaterialProperties.EntityTypes.Add(IFCEntityType.IfcMaterial);
            ifcPSE = new PreDefinedPropertySetEntry("IfcHygroscopicMaterialProperties.UpperVaporResistanceFactor", "UpperVaporResistanceFactor");
            ifcPSE.PropertyName = "UpperVaporResistanceFactor";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.UpperVaporResistanceFactorCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcHygroscopicMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcHygroscopicMaterialProperties.LowerVaporResistanceFactor", "LowerVaporResistanceFactor");
            ifcPSE.PropertyName = "LowerVaporResistanceFactor";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LowerVaporResistanceFactorCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcHygroscopicMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcHygroscopicMaterialProperties.IsothermalMoistureCapacity", "IsothermalMoistureCapacity");
            ifcPSE.PropertyName = "IsothermalMoistureCapacity";
            ifcPSE.PropertyType = PropertyType.IsothermalMoistureCapacity;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.IsothermalMoistureCapacityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcHygroscopicMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcHygroscopicMaterialProperties.VaporPermeability", "VaporPermeability");
            ifcPSE.PropertyName = "VaporPermeability";
            ifcPSE.PropertyType = PropertyType.VaporPermeability;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.VaporPermeabilityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcHygroscopicMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcHygroscopicMaterialProperties.MoistureDiffusivity", "MoistureDiffusivity");
            ifcPSE.PropertyName = "MoistureDiffusivity";
            ifcPSE.PropertyType = PropertyType.MoistureDiffusivity;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.MoistureDiffusivityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcHygroscopicMaterialProperties.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            commonPropertySets.Add(IfcHygroscopicMaterialProperties);
         }
      }


      private static void InitIfcMechanicalConcreteMaterialProperties(IList<PreDefinedPropertySetDescription> commonPropertySets)
      {
         PreDefinedPropertySetDescription IfcMechanicalConcreteMaterialProperties = new PreDefinedPropertySetDescription();
         IfcMechanicalConcreteMaterialProperties.Name = "IfcMechanicalConcreteMaterialProperties";
         PreDefinedPropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcMechanicalConcreteMaterialProperties"))
         {
            IfcMechanicalConcreteMaterialProperties.EntityTypes.Add(IFCEntityType.IfcMaterial);
            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalConcreteMaterialProperties.CompressiveStrength", "CompressiveStrength");
            ifcPSE.PropertyName = "CompressiveStrength";
            ifcPSE.PropertyType = PropertyType.Pressure;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CompressiveStrengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalConcreteMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalConcreteMaterialProperties.MaxAggregateSize", "MaxAggregateSize");
            ifcPSE.PropertyName = "MaxAggregateSize";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.MaxAggregateSizeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalConcreteMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalConcreteMaterialProperties.AdmixturesDescription", "AdmixturesDescription");
            ifcPSE.PropertyName = "AdmixturesDescription";
            ifcPSE.PropertyType = PropertyType.Text;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.AdmixturesDescriptionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalConcreteMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalConcreteMaterialProperties.Workability", "Workability");
            ifcPSE.PropertyName = "Workability";
            ifcPSE.PropertyType = PropertyType.Text;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.WorkabilityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalConcreteMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalConcreteMaterialProperties.ProtectivePoreRatio", "ProtectivePoreRatio");
            ifcPSE.PropertyName = "ProtectivePoreRatio";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ProtectivePoreRatioCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalConcreteMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalConcreteMaterialProperties.WaterImpermeability", "WaterImpermeability");
            ifcPSE.PropertyName = "WaterImpermeability";
            ifcPSE.PropertyType = PropertyType.Text;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.WaterImpermeabilityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalConcreteMaterialProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcMechanicalConcreteMaterialProperties"))
         {
            IfcMechanicalConcreteMaterialProperties.EntityTypes.Add(IFCEntityType.IfcMaterial);
            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalConcreteMaterialProperties.CompressiveStrength", "CompressiveStrength");
            ifcPSE.PropertyName = "CompressiveStrength";
            ifcPSE.PropertyType = PropertyType.Pressure;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CompressiveStrengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalConcreteMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalConcreteMaterialProperties.MaxAggregateSize", "MaxAggregateSize");
            ifcPSE.PropertyName = "MaxAggregateSize";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.MaxAggregateSizeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalConcreteMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalConcreteMaterialProperties.AdmixturesDescription", "AdmixturesDescription");
            ifcPSE.PropertyName = "AdmixturesDescription";
            ifcPSE.PropertyType = PropertyType.Text;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.AdmixturesDescriptionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalConcreteMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalConcreteMaterialProperties.Workability", "Workability");
            ifcPSE.PropertyName = "Workability";
            ifcPSE.PropertyType = PropertyType.Text;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.WorkabilityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalConcreteMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalConcreteMaterialProperties.ProtectivePoreRatio", "ProtectivePoreRatio");
            ifcPSE.PropertyName = "ProtectivePoreRatio";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ProtectivePoreRatioCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalConcreteMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalConcreteMaterialProperties.WaterImpermeability", "WaterImpermeability");
            ifcPSE.PropertyName = "WaterImpermeability";
            ifcPSE.PropertyType = PropertyType.Text;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.WaterImpermeabilityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalConcreteMaterialProperties.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            commonPropertySets.Add(IfcMechanicalConcreteMaterialProperties);
         }
      }


      private static void InitIfcMechanicalMaterialProperties(IList<PreDefinedPropertySetDescription> commonPropertySets)
      {
         PreDefinedPropertySetDescription IfcMechanicalMaterialProperties = new PreDefinedPropertySetDescription();
         IfcMechanicalMaterialProperties.Name = "IfcMechanicalMaterialProperties";
         PreDefinedPropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcMechanicalMaterialProperties"))
         {
            IfcMechanicalMaterialProperties.EntityTypes.Add(IFCEntityType.IfcMaterial);
            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalMaterialProperties.DynamicViscosity", "DynamicViscosity");
            ifcPSE.PropertyName = "DynamicViscosity";
            ifcPSE.PropertyType = PropertyType.DynamicViscosity;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.DynamicViscosityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalMaterialProperties.YoungModulus", "YoungModulus");
            ifcPSE.PropertyName = "YoungModulus";
            ifcPSE.PropertyType = PropertyType.ModulusOfElasticity;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.YoungModulusCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalMaterialProperties.ShearModulus", "ShearModulus");
            ifcPSE.PropertyName = "ShearModulus";
            ifcPSE.PropertyType = PropertyType.ModulusOfElasticity;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ShearModulusCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalMaterialProperties.PoissonRatio", "PoissonRatio");
            ifcPSE.PropertyName = "PoissonRatio";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PoissonRatioCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalMaterialProperties.ThermalExpansionCoefficient", "ThermalExpansionCoefficient");
            ifcPSE.PropertyName = "ThermalExpansionCoefficient";
            ifcPSE.PropertyType = PropertyType.ThermalExpansionCoefficient;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThermalExpansionCoefficientCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalMaterialProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcMechanicalMaterialProperties"))
         {
            IfcMechanicalMaterialProperties.EntityTypes.Add(IFCEntityType.IfcMaterial);
            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalMaterialProperties.DynamicViscosity", "DynamicViscosity");
            ifcPSE.PropertyName = "DynamicViscosity";
            ifcPSE.PropertyType = PropertyType.DynamicViscosity;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.DynamicViscosityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalMaterialProperties.YoungModulus", "YoungModulus");
            ifcPSE.PropertyName = "YoungModulus";
            ifcPSE.PropertyType = PropertyType.ModulusOfElasticity;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.YoungModulusCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalMaterialProperties.ShearModulus", "ShearModulus");
            ifcPSE.PropertyName = "ShearModulus";
            ifcPSE.PropertyType = PropertyType.ModulusOfElasticity;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ShearModulusCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalMaterialProperties.PoissonRatio", "PoissonRatio");
            ifcPSE.PropertyName = "PoissonRatio";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PoissonRatioCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalMaterialProperties.ThermalExpansionCoefficient", "ThermalExpansionCoefficient");
            ifcPSE.PropertyName = "ThermalExpansionCoefficient";
            ifcPSE.PropertyType = PropertyType.ThermalExpansionCoefficient;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThermalExpansionCoefficientCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalMaterialProperties.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            commonPropertySets.Add(IfcMechanicalMaterialProperties);
         }
      }


      private static void InitIfcMechanicalSteelMaterialProperties(IList<PreDefinedPropertySetDescription> commonPropertySets)
      {
         PreDefinedPropertySetDescription IfcMechanicalSteelMaterialProperties = new PreDefinedPropertySetDescription();
         IfcMechanicalSteelMaterialProperties.Name = "IfcMechanicalSteelMaterialProperties";
         PreDefinedPropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcMechanicalSteelMaterialProperties"))
         {
            IfcMechanicalSteelMaterialProperties.EntityTypes.Add(IFCEntityType.IfcMaterial);
            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalSteelMaterialProperties.YieldStress", "YieldStress");
            ifcPSE.PropertyName = "YieldStress";
            ifcPSE.PropertyType = PropertyType.Pressure;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.YieldStressCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalSteelMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalSteelMaterialProperties.UltimateStress", "UltimateStress");
            ifcPSE.PropertyName = "UltimateStress";
            ifcPSE.PropertyType = PropertyType.Pressure;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.UltimateStressCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalSteelMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalSteelMaterialProperties.UltimateStrain", "UltimateStrain");
            ifcPSE.PropertyName = "UltimateStrain";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.UltimateStrainCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalSteelMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalSteelMaterialProperties.HardeningModule", "HardeningModule");
            ifcPSE.PropertyName = "HardeningModule";
            ifcPSE.PropertyType = PropertyType.ModulusOfElasticity;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.HardeningModuleCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalSteelMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalSteelMaterialProperties.ProportionalStress", "ProportionalStress");
            ifcPSE.PropertyName = "ProportionalStress";
            ifcPSE.PropertyType = PropertyType.Pressure;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ProportionalStressCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalSteelMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalSteelMaterialProperties.PlasticStrain", "PlasticStrain");
            ifcPSE.PropertyName = "PlasticStrain";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PlasticStrainCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalSteelMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalSteelMaterialProperties.Relaxations", "Relaxations");
            ifcPSE.PropertyName = "Relaxations";
            ifcPSE.PropertyType = PropertyType.IfcRelaxation;
            ifcPSE.PropertyValueType = PropertyValueType.ListValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.RelaxationsCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalSteelMaterialProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcMechanicalSteelMaterialProperties"))
         {
            IfcMechanicalSteelMaterialProperties.EntityTypes.Add(IFCEntityType.IfcMaterial);
            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalSteelMaterialProperties.YieldStress", "YieldStress");
            ifcPSE.PropertyName = "YieldStress";
            ifcPSE.PropertyType = PropertyType.Pressure;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.YieldStressCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalSteelMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalSteelMaterialProperties.UltimateStress", "UltimateStress");
            ifcPSE.PropertyName = "UltimateStress";
            ifcPSE.PropertyType = PropertyType.Pressure;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.UltimateStressCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalSteelMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalSteelMaterialProperties.UltimateStrain", "UltimateStrain");
            ifcPSE.PropertyName = "UltimateStrain";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.UltimateStrainCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalSteelMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalSteelMaterialProperties.HardeningModule", "HardeningModule");
            ifcPSE.PropertyName = "HardeningModule";
            ifcPSE.PropertyType = PropertyType.ModulusOfElasticity;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.HardeningModuleCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalSteelMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalSteelMaterialProperties.ProportionalStress", "ProportionalStress");
            ifcPSE.PropertyName = "ProportionalStress";
            ifcPSE.PropertyType = PropertyType.Pressure;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ProportionalStressCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalSteelMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalSteelMaterialProperties.PlasticStrain", "PlasticStrain");
            ifcPSE.PropertyName = "PlasticStrain";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PlasticStrainCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalSteelMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcMechanicalSteelMaterialProperties.Relaxations", "Relaxations");
            ifcPSE.PropertyName = "Relaxations";
            ifcPSE.PropertyType = PropertyType.IfcRelaxation;
            ifcPSE.PropertyValueType = PropertyValueType.ListValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.RelaxationsCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcMechanicalSteelMaterialProperties.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            commonPropertySets.Add(IfcMechanicalSteelMaterialProperties);
         }
      }


      private static void InitIfcOpticalMaterialProperties(IList<PreDefinedPropertySetDescription> commonPropertySets)
      {
         PreDefinedPropertySetDescription IfcOpticalMaterialProperties = new PreDefinedPropertySetDescription();
         IfcOpticalMaterialProperties.Name = "IfcOpticalMaterialProperties";
         PreDefinedPropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcOpticalMaterialProperties"))
         {
            IfcOpticalMaterialProperties.EntityTypes.Add(IFCEntityType.IfcMaterial);
            ifcPSE = new PreDefinedPropertySetEntry("IfcOpticalMaterialProperties.VisibleTransmittance", "VisibleTransmittance");
            ifcPSE.PropertyName = "VisibleTransmittance";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.VisibleTransmittanceCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcOpticalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcOpticalMaterialProperties.SolarTransmittance", "SolarTransmittance");
            ifcPSE.PropertyName = "SolarTransmittance";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SolarTransmittanceCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcOpticalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcOpticalMaterialProperties.ThermalIrTransmittance", "ThermalIrTransmittance");
            ifcPSE.PropertyName = "ThermalIrTransmittance";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThermalIrTransmittanceCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcOpticalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcOpticalMaterialProperties.ThermalIrEmissivityBack", "ThermalIrEmissivityBack");
            ifcPSE.PropertyName = "ThermalIrEmissivityBack";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThermalIrEmissivityBackCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcOpticalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcOpticalMaterialProperties.ThermalIrEmissivityFront", "ThermalIrEmissivityFront");
            ifcPSE.PropertyName = "ThermalIrEmissivityFront";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThermalIrEmissivityFrontCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcOpticalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcOpticalMaterialProperties.VisibleReflectanceBack", "VisibleReflectanceBack");
            ifcPSE.PropertyName = "VisibleReflectanceBack";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.VisibleReflectanceBackCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcOpticalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcOpticalMaterialProperties.VisibleReflectanceFront", "VisibleReflectanceFront");
            ifcPSE.PropertyName = "VisibleReflectanceFront";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.VisibleReflectanceFrontCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcOpticalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcOpticalMaterialProperties.SolarReflectanceFront", "SolarReflectanceFront");
            ifcPSE.PropertyName = "SolarReflectanceFront";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SolarReflectanceFrontCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcOpticalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcOpticalMaterialProperties.SolarReflectanceBack", "SolarReflectanceBack");
            ifcPSE.PropertyName = "SolarReflectanceBack";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SolarReflectanceBackCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcOpticalMaterialProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcOpticalMaterialProperties"))
         {
            IfcOpticalMaterialProperties.EntityTypes.Add(IFCEntityType.IfcMaterial);
            ifcPSE = new PreDefinedPropertySetEntry("IfcOpticalMaterialProperties.VisibleTransmittance", "VisibleTransmittance");
            ifcPSE.PropertyName = "VisibleTransmittance";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.VisibleTransmittanceCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcOpticalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcOpticalMaterialProperties.SolarTransmittance", "SolarTransmittance");
            ifcPSE.PropertyName = "SolarTransmittance";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SolarTransmittanceCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcOpticalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcOpticalMaterialProperties.ThermalIrTransmittance", "ThermalIrTransmittance");
            ifcPSE.PropertyName = "ThermalIrTransmittance";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThermalIrTransmittanceCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcOpticalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcOpticalMaterialProperties.ThermalIrEmissivityBack", "ThermalIrEmissivityBack");
            ifcPSE.PropertyName = "ThermalIrEmissivityBack";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThermalIrEmissivityBackCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcOpticalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcOpticalMaterialProperties.ThermalIrEmissivityFront", "ThermalIrEmissivityFront");
            ifcPSE.PropertyName = "ThermalIrEmissivityFront";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThermalIrEmissivityFrontCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcOpticalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcOpticalMaterialProperties.VisibleReflectanceBack", "VisibleReflectanceBack");
            ifcPSE.PropertyName = "VisibleReflectanceBack";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.VisibleReflectanceBackCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcOpticalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcOpticalMaterialProperties.VisibleReflectanceFront", "VisibleReflectanceFront");
            ifcPSE.PropertyName = "VisibleReflectanceFront";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.VisibleReflectanceFrontCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcOpticalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcOpticalMaterialProperties.SolarReflectanceFront", "SolarReflectanceFront");
            ifcPSE.PropertyName = "SolarReflectanceFront";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SolarReflectanceFrontCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcOpticalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcOpticalMaterialProperties.SolarReflectanceBack", "SolarReflectanceBack");
            ifcPSE.PropertyName = "SolarReflectanceBack";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SolarReflectanceBackCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcOpticalMaterialProperties.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            commonPropertySets.Add(IfcOpticalMaterialProperties);
         }
      }


      private static void InitIfcPermeableCoveringProperties(IList<PreDefinedPropertySetDescription> commonPropertySets)
      {
         PreDefinedPropertySetDescription IfcPermeableCoveringProperties = new PreDefinedPropertySetDescription();
         IfcPermeableCoveringProperties.Name = "IfcPermeableCoveringProperties";
         PreDefinedPropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcPermeableCoveringProperties"))
         {
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcDoorStyle);
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcWindowStyle);
            ifcPSE = new PreDefinedPropertySetEntry("IfcPermeableCoveringProperties.OperationType", "OperationType");
            ifcPSE.PropertyName = "OperationType";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OperationTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcPermeableCoveringProperties.PanelPosition", "PanelPosition");
            ifcPSE.PropertyName = "PanelPosition";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelPositionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcPermeableCoveringProperties.FrameDepth", "FrameDepth");
            ifcPSE.PropertyName = "FrameDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcPermeableCoveringProperties.FrameThickness", "FrameThickness");
            ifcPSE.PropertyName = "FrameThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcPermeableCoveringProperties"))
         {
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcDoorStyle);
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcWindowStyle);
            ifcPSE = new PreDefinedPropertySetEntry("IfcPermeableCoveringProperties.OperationType", "OperationType");
            ifcPSE.PropertyName = "OperationType";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OperationTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcPermeableCoveringProperties.PanelPosition", "PanelPosition");
            ifcPSE.PropertyName = "PanelPosition";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelPositionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcPermeableCoveringProperties.FrameDepth", "FrameDepth");
            ifcPSE.PropertyName = "FrameDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcPermeableCoveringProperties.FrameThickness", "FrameThickness");
            ifcPSE.PropertyName = "FrameThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcPermeableCoveringProperties"))
         {
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcDoorType);
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcWindowType);
            ifcPSE = new PreDefinedPropertySetEntry("IfcPermeableCoveringProperties.OperationType", "OperationType");
            ifcPSE.PropertyName = "OperationType";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OperationTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcPermeableCoveringProperties.PanelPosition", "PanelPosition");
            ifcPSE.PropertyName = "PanelPosition";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelPositionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcPermeableCoveringProperties.FrameDepth", "FrameDepth");
            ifcPSE.PropertyName = "FrameDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcPermeableCoveringProperties.FrameThickness", "FrameThickness");
            ifcPSE.PropertyName = "FrameThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcPermeableCoveringProperties"))
         {
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            ifcPSE = new PreDefinedPropertySetEntry("IfcPermeableCoveringProperties.OperationType", "OperationType");
            ifcPSE.PropertyName = "OperationType";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OperationTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcPermeableCoveringProperties.PanelPosition", "PanelPosition");
            ifcPSE.PropertyName = "PanelPosition";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelPositionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcPermeableCoveringProperties.FrameDepth", "FrameDepth");
            ifcPSE.PropertyName = "FrameDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcPermeableCoveringProperties.FrameThickness", "FrameThickness");
            ifcPSE.PropertyName = "FrameThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            commonPropertySets.Add(IfcPermeableCoveringProperties);
         }
      }


      private static void InitIfcProductsOfCombustionProperties(IList<PreDefinedPropertySetDescription> commonPropertySets)
      {
         PreDefinedPropertySetDescription IfcProductsOfCombustionProperties = new PreDefinedPropertySetDescription();
         IfcProductsOfCombustionProperties.Name = "IfcProductsOfCombustionProperties";
         PreDefinedPropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcProductsOfCombustionProperties"))
         {
            IfcProductsOfCombustionProperties.EntityTypes.Add(IFCEntityType.IfcMaterial);
            ifcPSE = new PreDefinedPropertySetEntry("IfcProductsOfCombustionProperties.SpecificHeatCapacity", "SpecificHeatCapacity");
            ifcPSE.PropertyName = "SpecificHeatCapacity";
            ifcPSE.PropertyType = PropertyType.SpecificHeatCapacity;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SpecificHeatCapacityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcProductsOfCombustionProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcProductsOfCombustionProperties.N20Content", "N20Content");
            ifcPSE.PropertyName = "N20Content";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.N20ContentCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcProductsOfCombustionProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcProductsOfCombustionProperties.COContent", "COContent");
            ifcPSE.PropertyName = "COContent";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.COContentCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcProductsOfCombustionProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcProductsOfCombustionProperties.CO2Content", "CO2Content");
            ifcPSE.PropertyName = "CO2Content";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CO2ContentCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcProductsOfCombustionProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcProductsOfCombustionProperties"))
         {
            IfcProductsOfCombustionProperties.EntityTypes.Add(IFCEntityType.IfcMaterial);
            ifcPSE = new PreDefinedPropertySetEntry("IfcProductsOfCombustionProperties.SpecificHeatCapacity", "SpecificHeatCapacity");
            ifcPSE.PropertyName = "SpecificHeatCapacity";
            ifcPSE.PropertyType = PropertyType.SpecificHeatCapacity;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SpecificHeatCapacityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcProductsOfCombustionProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcProductsOfCombustionProperties.N20Content", "N20Content");
            ifcPSE.PropertyName = "N20Content";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.N20ContentCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcProductsOfCombustionProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcProductsOfCombustionProperties.COContent", "COContent");
            ifcPSE.PropertyName = "COContent";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.COContentCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcProductsOfCombustionProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcProductsOfCombustionProperties.CO2Content", "CO2Content");
            ifcPSE.PropertyName = "CO2Content";
            ifcPSE.PropertyType = PropertyType.PositiveRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CO2ContentCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcProductsOfCombustionProperties.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            commonPropertySets.Add(IfcProductsOfCombustionProperties);
         }
      }


      private static void InitIfcReinforcementDefinitionProperties(IList<PreDefinedPropertySetDescription> commonPropertySets)
      {
         PreDefinedPropertySetDescription IfcReinforcementDefinitionProperties = new PreDefinedPropertySetDescription();
         IfcReinforcementDefinitionProperties.Name = "IfcReinforcementDefinitionProperties";
         PreDefinedPropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcReinforcementDefinitionProperties"))
         {
            IfcReinforcementDefinitionProperties.EntityTypes.Add(IFCEntityType.IfcReinforcingElement);
            ifcPSE = new PreDefinedPropertySetEntry("IfcReinforcementDefinitionProperties.DefinitionType", "DefinitionType");
            ifcPSE.PropertyName = "DefinitionType";
            ifcPSE.PropertyType = PropertyType.Label;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.DefinitionTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcReinforcementDefinitionProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcReinforcementDefinitionProperties.ReinforcementSectionDefinitions", "ReinforcementSectionDefinitions");
            ifcPSE.PropertyName = "ReinforcementSectionDefinitions";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.ListValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ReinforcementSectionDefinitionsCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcReinforcementDefinitionProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcReinforcementDefinitionProperties"))
         {
            IfcReinforcementDefinitionProperties.EntityTypes.Add(IFCEntityType.IfcReinforcingElement);
            ifcPSE = new PreDefinedPropertySetEntry("IfcReinforcementDefinitionProperties.DefinitionType", "DefinitionType");
            ifcPSE.PropertyName = "DefinitionType";
            ifcPSE.PropertyType = PropertyType.Label;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.DefinitionTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcReinforcementDefinitionProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcReinforcementDefinitionProperties.ReinforcementSectionDefinitions", "ReinforcementSectionDefinitions");
            ifcPSE.PropertyName = "ReinforcementSectionDefinitions";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.ListValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ReinforcementSectionDefinitionsCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcReinforcementDefinitionProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcReinforcementDefinitionProperties"))
         {
            IfcReinforcementDefinitionProperties.EntityTypes.Add(IFCEntityType.IfcReinforcingElement);
            ifcPSE = new PreDefinedPropertySetEntry("IfcReinforcementDefinitionProperties.DefinitionType", "DefinitionType");
            ifcPSE.PropertyName = "DefinitionType";
            ifcPSE.PropertyType = PropertyType.Label;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.DefinitionTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcReinforcementDefinitionProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcReinforcementDefinitionProperties.ReinforcementSectionDefinitions", "ReinforcementSectionDefinitions");
            ifcPSE.PropertyName = "ReinforcementSectionDefinitions";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.ListValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ReinforcementSectionDefinitionsCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcReinforcementDefinitionProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcReinforcementDefinitionProperties"))
         {
            IfcReinforcementDefinitionProperties.EntityTypes.Add(IFCEntityType.IfcReinforcingElement);
            ifcPSE = new PreDefinedPropertySetEntry("IfcReinforcementDefinitionProperties.DefinitionType", "DefinitionType");
            ifcPSE.PropertyName = "DefinitionType";
            ifcPSE.PropertyType = PropertyType.Label;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.DefinitionTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcReinforcementDefinitionProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcReinforcementDefinitionProperties.ReinforcementSectionDefinitions", "ReinforcementSectionDefinitions");
            ifcPSE.PropertyName = "ReinforcementSectionDefinitions";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.ListValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ReinforcementSectionDefinitionsCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcReinforcementDefinitionProperties.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            commonPropertySets.Add(IfcReinforcementDefinitionProperties);
         }
      }


      private static void InitIfcThermalMaterialProperties(IList<PreDefinedPropertySetDescription> commonPropertySets)
      {
         PreDefinedPropertySetDescription IfcThermalMaterialProperties = new PreDefinedPropertySetDescription();
         IfcThermalMaterialProperties.Name = "IfcThermalMaterialProperties";
         PreDefinedPropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcThermalMaterialProperties"))
         {
            IfcThermalMaterialProperties.EntityTypes.Add(IFCEntityType.IfcMaterial);
            ifcPSE = new PreDefinedPropertySetEntry("IfcThermalMaterialProperties.SpecificHeatCapacity", "SpecificHeatCapacity");
            ifcPSE.PropertyName = "SpecificHeatCapacity";
            ifcPSE.PropertyType = PropertyType.SpecificHeatCapacity;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SpecificHeatCapacityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcThermalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcThermalMaterialProperties.BoilingPoint", "BoilingPoint");
            ifcPSE.PropertyName = "BoilingPoint";
            ifcPSE.PropertyType = PropertyType.ThermodynamicTemperature;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.BoilingPointCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcThermalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcThermalMaterialProperties.FreezingPoint", "FreezingPoint");
            ifcPSE.PropertyName = "FreezingPoint";
            ifcPSE.PropertyType = PropertyType.ThermodynamicTemperature;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FreezingPointCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcThermalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcThermalMaterialProperties.ThermalConductivity", "ThermalConductivity");
            ifcPSE.PropertyName = "ThermalConductivity";
            ifcPSE.PropertyType = PropertyType.ThermalConductivity;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThermalConductivityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcThermalMaterialProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcThermalMaterialProperties"))
         {
            IfcThermalMaterialProperties.EntityTypes.Add(IFCEntityType.IfcMaterial);
            ifcPSE = new PreDefinedPropertySetEntry("IfcThermalMaterialProperties.SpecificHeatCapacity", "SpecificHeatCapacity");
            ifcPSE.PropertyName = "SpecificHeatCapacity";
            ifcPSE.PropertyType = PropertyType.SpecificHeatCapacity;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SpecificHeatCapacityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcThermalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcThermalMaterialProperties.BoilingPoint", "BoilingPoint");
            ifcPSE.PropertyName = "BoilingPoint";
            ifcPSE.PropertyType = PropertyType.ThermodynamicTemperature;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.BoilingPointCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcThermalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcThermalMaterialProperties.FreezingPoint", "FreezingPoint");
            ifcPSE.PropertyName = "FreezingPoint";
            ifcPSE.PropertyType = PropertyType.ThermodynamicTemperature;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FreezingPointCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcThermalMaterialProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcThermalMaterialProperties.ThermalConductivity", "ThermalConductivity");
            ifcPSE.PropertyName = "ThermalConductivity";
            ifcPSE.PropertyType = PropertyType.ThermalConductivity;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThermalConductivityCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcThermalMaterialProperties.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            commonPropertySets.Add(IfcThermalMaterialProperties);
         }
      }


      private static void InitIfcWaterProperties(IList<PreDefinedPropertySetDescription> commonPropertySets)
      {
         PreDefinedPropertySetDescription IfcWaterProperties = new PreDefinedPropertySetDescription();
         IfcWaterProperties.Name = "IfcWaterProperties";
         PreDefinedPropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcWaterProperties"))
         {
            IfcWaterProperties.EntityTypes.Add(IFCEntityType.IfcMaterial);
            ifcPSE = new PreDefinedPropertySetEntry("IfcWaterProperties.IsPotable", "IsPotable");
            ifcPSE.PropertyName = "IsPotable";
            ifcPSE.PropertyType = PropertyType.Boolean;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.IsPotableCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWaterProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWaterProperties.Hardness", "Hardness");
            ifcPSE.PropertyName = "Hardness";
            ifcPSE.PropertyType = PropertyType.IonConcentration;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.HardnessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWaterProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWaterProperties.AlkalinityConcentration", "AlkalinityConcentration");
            ifcPSE.PropertyName = "AlkalinityConcentration";
            ifcPSE.PropertyType = PropertyType.IonConcentration;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.AlkalinityConcentrationCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWaterProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWaterProperties.AcidityConcentration", "AcidityConcentration");
            ifcPSE.PropertyName = "AcidityConcentration";
            ifcPSE.PropertyType = PropertyType.IonConcentration;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.AcidityConcentrationCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWaterProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWaterProperties.ImpuritiesContent", "ImpuritiesContent");
            ifcPSE.PropertyName = "ImpuritiesContent";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ImpuritiesContentCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWaterProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWaterProperties.PHLevel", "PHLevel");
            ifcPSE.PropertyName = "PHLevel";
            ifcPSE.PropertyType = PropertyType.PH;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PHLevelCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWaterProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWaterProperties.DissolvedSolidsContent", "DissolvedSolidsContent");
            ifcPSE.PropertyName = "DissolvedSolidsContent";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.DissolvedSolidsContentCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWaterProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcWaterProperties"))
         {
            IfcWaterProperties.EntityTypes.Add(IFCEntityType.IfcMaterial);
            ifcPSE = new PreDefinedPropertySetEntry("IfcWaterProperties.IsPotable", "IsPotable");
            ifcPSE.PropertyName = "IsPotable";
            ifcPSE.PropertyType = PropertyType.Boolean;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.IsPotableCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWaterProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWaterProperties.Hardness", "Hardness");
            ifcPSE.PropertyName = "Hardness";
            ifcPSE.PropertyType = PropertyType.IonConcentration;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.HardnessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWaterProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWaterProperties.AlkalinityConcentration", "AlkalinityConcentration");
            ifcPSE.PropertyName = "AlkalinityConcentration";
            ifcPSE.PropertyType = PropertyType.IonConcentration;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.AlkalinityConcentrationCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWaterProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWaterProperties.AcidityConcentration", "AcidityConcentration");
            ifcPSE.PropertyName = "AcidityConcentration";
            ifcPSE.PropertyType = PropertyType.IonConcentration;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.AcidityConcentrationCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWaterProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWaterProperties.ImpuritiesContent", "ImpuritiesContent");
            ifcPSE.PropertyName = "ImpuritiesContent";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ImpuritiesContentCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWaterProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWaterProperties.PHLevel", "PHLevel");
            ifcPSE.PropertyName = "PHLevel";
            ifcPSE.PropertyType = PropertyType.PH;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PHLevelCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWaterProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWaterProperties.DissolvedSolidsContent", "DissolvedSolidsContent");
            ifcPSE.PropertyName = "DissolvedSolidsContent";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.DissolvedSolidsContentCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWaterProperties.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            commonPropertySets.Add(IfcWaterProperties);
         }
      }


      private static void InitIfcWindowLiningProperties(IList<PreDefinedPropertySetDescription> commonPropertySets)
      {
         PreDefinedPropertySetDescription IfcWindowLiningProperties = new PreDefinedPropertySetDescription();
         IfcWindowLiningProperties.Name = "IfcWindowLiningProperties";
         PreDefinedPropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcWindowLiningProperties"))
         {
            IfcWindowLiningProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            IfcWindowLiningProperties.EntityTypes.Add(IFCEntityType.IfcWindowStyle);
            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.LiningDepth", "LiningDepth");
            ifcPSE.PropertyName = "LiningDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.FirstTransomOffset", "FirstTransomOffset");
            ifcPSE.PropertyName = "FirstTransomOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FirstTransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.SecondTransomOffset", "SecondTransomOffset");
            ifcPSE.PropertyName = "SecondTransomOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SecondTransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.FirstMullionOffset", "FirstMullionOffset");
            ifcPSE.PropertyName = "FirstMullionOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FirstMullionOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.SecondMullionOffset", "SecondMullionOffset");
            ifcPSE.PropertyName = "SecondMullionOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SecondMullionOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.LiningThickness", "LiningThickness");
            ifcPSE.PropertyName = "LiningThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.TransomThickness", "TransomThickness");
            ifcPSE.PropertyName = "TransomThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TransomThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.MullionThickness", "MullionThickness");
            ifcPSE.PropertyName = "MullionThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.MullionThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcWindowLiningProperties"))
         {
            IfcWindowLiningProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            IfcWindowLiningProperties.EntityTypes.Add(IFCEntityType.IfcWindowStyle);
            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.LiningDepth", "LiningDepth");
            ifcPSE.PropertyName = "LiningDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.FirstTransomOffset", "FirstTransomOffset");
            ifcPSE.PropertyName = "FirstTransomOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FirstTransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.SecondTransomOffset", "SecondTransomOffset");
            ifcPSE.PropertyName = "SecondTransomOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SecondTransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.FirstMullionOffset", "FirstMullionOffset");
            ifcPSE.PropertyName = "FirstMullionOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FirstMullionOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.SecondMullionOffset", "SecondMullionOffset");
            ifcPSE.PropertyName = "SecondMullionOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SecondMullionOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.LiningThickness", "LiningThickness");
            ifcPSE.PropertyName = "LiningThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.TransomThickness", "TransomThickness");
            ifcPSE.PropertyName = "TransomThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TransomThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.MullionThickness", "MullionThickness");
            ifcPSE.PropertyName = "MullionThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.MullionThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcWindowLiningProperties"))
         {
            IfcWindowLiningProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            IfcWindowLiningProperties.EntityTypes.Add(IFCEntityType.IfcWindowType);
            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.LiningDepth", "LiningDepth");
            ifcPSE.PropertyName = "LiningDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.FirstTransomOffset", "FirstTransomOffset");
            ifcPSE.PropertyName = "FirstTransomOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FirstTransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.SecondTransomOffset", "SecondTransomOffset");
            ifcPSE.PropertyName = "SecondTransomOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SecondTransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.FirstMullionOffset", "FirstMullionOffset");
            ifcPSE.PropertyName = "FirstMullionOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FirstMullionOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.SecondMullionOffset", "SecondMullionOffset");
            ifcPSE.PropertyName = "SecondMullionOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SecondMullionOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.LiningThickness", "LiningThickness");
            ifcPSE.PropertyName = "LiningThickness";
            ifcPSE.PropertyType = PropertyType.NonNegativeLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.TransomThickness", "TransomThickness");
            ifcPSE.PropertyName = "TransomThickness";
            ifcPSE.PropertyType = PropertyType.NonNegativeLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TransomThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.MullionThickness", "MullionThickness");
            ifcPSE.PropertyName = "MullionThickness";
            ifcPSE.PropertyType = PropertyType.NonNegativeLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.MullionThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.LiningOffset", "LiningOffset");
            ifcPSE.PropertyName = "LiningOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.LiningToPanelOffsetX", "LiningToPanelOffsetX");
            ifcPSE.PropertyName = "LiningToPanelOffsetX";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningToPanelOffsetXCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.LiningToPanelOffsetY", "LiningToPanelOffsetY");
            ifcPSE.PropertyName = "LiningToPanelOffsetY";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningToPanelOffsetYCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcWindowLiningProperties"))
         {
            IfcWindowLiningProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.LiningDepth", "LiningDepth");
            ifcPSE.PropertyName = "LiningDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.FirstTransomOffset", "FirstTransomOffset");
            ifcPSE.PropertyName = "FirstTransomOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FirstTransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.SecondTransomOffset", "SecondTransomOffset");
            ifcPSE.PropertyName = "SecondTransomOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SecondTransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.FirstMullionOffset", "FirstMullionOffset");
            ifcPSE.PropertyName = "FirstMullionOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FirstMullionOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowLiningProperties.SecondMullionOffset", "SecondMullionOffset");
            ifcPSE.PropertyName = "SecondMullionOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SecondMullionOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            commonPropertySets.Add(IfcWindowLiningProperties);
         }
      }


      private static void InitIfcWindowPanelProperties(IList<PreDefinedPropertySetDescription> commonPropertySets)
      {
         PreDefinedPropertySetDescription IfcWindowPanelProperties = new PreDefinedPropertySetDescription();
         IfcWindowPanelProperties.Name = "IfcWindowPanelProperties";
         PreDefinedPropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcWindowPanelProperties"))
         {
            IfcWindowPanelProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            IfcWindowPanelProperties.EntityTypes.Add(IFCEntityType.IfcWindowStyle);
            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowPanelProperties.OperationType", "OperationType");
            ifcPSE.PropertyName = "OperationType";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OperationTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowPanelProperties.PanelPosition", "PanelPosition");
            ifcPSE.PropertyName = "PanelPosition";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelPositionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowPanelProperties.FrameDepth", "FrameDepth");
            ifcPSE.PropertyName = "FrameDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowPanelProperties.FrameThickness", "FrameThickness");
            ifcPSE.PropertyName = "FrameThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcWindowPanelProperties"))
         {
            IfcWindowPanelProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            IfcWindowPanelProperties.EntityTypes.Add(IFCEntityType.IfcWindowStyle);
            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowPanelProperties.OperationType", "OperationType");
            ifcPSE.PropertyName = "OperationType";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OperationTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowPanelProperties.PanelPosition", "PanelPosition");
            ifcPSE.PropertyName = "PanelPosition";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelPositionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowPanelProperties.FrameDepth", "FrameDepth");
            ifcPSE.PropertyName = "FrameDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowPanelProperties.FrameThickness", "FrameThickness");
            ifcPSE.PropertyName = "FrameThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcWindowPanelProperties"))
         {
            IfcWindowPanelProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            IfcWindowPanelProperties.EntityTypes.Add(IFCEntityType.IfcWindowType);
            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowPanelProperties.OperationType", "OperationType");
            ifcPSE.PropertyName = "OperationType";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OperationTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowPanelProperties.PanelPosition", "PanelPosition");
            ifcPSE.PropertyName = "PanelPosition";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelPositionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowPanelProperties.FrameDepth", "FrameDepth");
            ifcPSE.PropertyName = "FrameDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowPanelProperties.FrameThickness", "FrameThickness");
            ifcPSE.PropertyName = "FrameThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4x3 && certifiedEntityAndPsetList.AllowPredefPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcWindowPanelProperties"))
         {
            IfcWindowPanelProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowPanelProperties.OperationType", "OperationType");
            ifcPSE.PropertyName = "OperationType";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OperationTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowPanelProperties.PanelPosition", "PanelPosition");
            ifcPSE.PropertyName = "PanelPosition";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelPositionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowPanelProperties.FrameDepth", "FrameDepth");
            ifcPSE.PropertyName = "FrameDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PreDefinedPropertySetEntry("IfcWindowPanelProperties.FrameThickness", "FrameThickness");
            ifcPSE.PropertyName = "FrameThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            commonPropertySets.Add(IfcWindowPanelProperties);
         }
      }


   }
}