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
      public static void InitPredefinedPropertySets(IList<IList<PropertySetDescription>> allPsetOrQtoSets)
      {
         IList<PropertySetDescription> theSets = new List<PropertySetDescription>();
         InitIfcDoorLiningProperties(theSets);
         InitIfcDoorPanelProperties(theSets);
         InitIfcPermeableCoveringProperties(theSets);
         InitIfcReinforcementDefinitionProperties(theSets);
         InitIfcWindowLiningProperties(theSets);
         InitIfcWindowPanelProperties(theSets);

         allPsetOrQtoSets.Add(theSets);
      }

      private static void InitIfcDoorLiningProperties(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription IfcDoorLiningProperties = new PropertySetDescription();
         IfcDoorLiningProperties.Name = "IfcDoorLiningProperties";
         PropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcDoorLiningProperties"))
         {
            IfcDoorLiningProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            IfcDoorLiningProperties.EntityTypes.Add(IFCEntityType.IfcDoorStyle);
            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.LiningDepth", "LiningDepth");
            ifcPSE.PropertyName = "LiningDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.ThresholdDepth", "ThresholdDepth");
            ifcPSE.PropertyName = "ThresholdDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThresholdDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.TransomOffset", "TransomOffset");
            ifcPSE.PropertyName = "TransomOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.LiningOffset", "LiningOffset");
            ifcPSE.PropertyName = "LiningOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.ThresholdOffset", "ThresholdOffset");
            ifcPSE.PropertyName = "ThresholdOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThresholdOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.CasingThickness", "CasingThickness");
            ifcPSE.PropertyName = "CasingThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CasingThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.CasingDepth", "CasingDepth");
            ifcPSE.PropertyName = "CasingDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CasingDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.LiningThickness", "LiningThickness");
            ifcPSE.PropertyName = "LiningThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.ThresholdThickness", "ThresholdThickness");
            ifcPSE.PropertyName = "ThresholdThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThresholdThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.TransomThickness", "TransomThickness");
            ifcPSE.PropertyName = "TransomThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TransomThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcDoorLiningProperties"))
         {
            IfcDoorLiningProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            IfcDoorLiningProperties.EntityTypes.Add(IFCEntityType.IfcDoorStyle);
            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.LiningDepth", "LiningDepth");
            ifcPSE.PropertyName = "LiningDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.ThresholdDepth", "ThresholdDepth");
            ifcPSE.PropertyName = "ThresholdDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThresholdDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.TransomOffset", "TransomOffset");
            ifcPSE.PropertyName = "TransomOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.LiningOffset", "LiningOffset");
            ifcPSE.PropertyName = "LiningOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.ThresholdOffset", "ThresholdOffset");
            ifcPSE.PropertyName = "ThresholdOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThresholdOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.CasingThickness", "CasingThickness");
            ifcPSE.PropertyName = "CasingThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CasingThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.CasingDepth", "CasingDepth");
            ifcPSE.PropertyName = "CasingDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CasingDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.LiningThickness", "LiningThickness");
            ifcPSE.PropertyName = "LiningThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.ThresholdThickness", "ThresholdThickness");
            ifcPSE.PropertyName = "ThresholdThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThresholdThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.TransomThickness", "TransomThickness");
            ifcPSE.PropertyName = "TransomThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TransomThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcDoorLiningProperties"))
         {
            IfcDoorLiningProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            IfcDoorLiningProperties.EntityTypes.Add(IFCEntityType.IfcDoorType);
            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.LiningDepth", "LiningDepth");
            ifcPSE.PropertyName = "LiningDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.ThresholdDepth", "ThresholdDepth");
            ifcPSE.PropertyName = "ThresholdDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThresholdDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.TransomOffset", "TransomOffset");
            ifcPSE.PropertyName = "TransomOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.LiningOffset", "LiningOffset");
            ifcPSE.PropertyName = "LiningOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.ThresholdOffset", "ThresholdOffset");
            ifcPSE.PropertyName = "ThresholdOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThresholdOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.CasingThickness", "CasingThickness");
            ifcPSE.PropertyName = "CasingThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CasingThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.CasingDepth", "CasingDepth");
            ifcPSE.PropertyName = "CasingDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CasingDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.LiningThickness", "LiningThickness");
            ifcPSE.PropertyName = "LiningThickness";
            ifcPSE.PropertyType = PropertyType.NonNegativeLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.ThresholdThickness", "ThresholdThickness");
            ifcPSE.PropertyName = "ThresholdThickness";
            ifcPSE.PropertyType = PropertyType.NonNegativeLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ThresholdThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.TransomThickness", "TransomThickness");
            ifcPSE.PropertyName = "TransomThickness";
            ifcPSE.PropertyType = PropertyType.NonNegativeLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TransomThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.LiningToPanelOffsetX", "LiningToPanelOffsetX");
            ifcPSE.PropertyName = "LiningToPanelOffsetX";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningToPanelOffsetXCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorLiningProperties.LiningToPanelOffsetY", "LiningToPanelOffsetY");
            ifcPSE.PropertyName = "LiningToPanelOffsetY";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningToPanelOffsetYCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorLiningProperties.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            commonPropertySets.Add(IfcDoorLiningProperties);
         }
      }


      private static void InitIfcDoorPanelProperties(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription IfcDoorPanelProperties = new PropertySetDescription();
         IfcDoorPanelProperties.Name = "IfcDoorPanelProperties";
         PropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcDoorPanelProperties"))
         {
            IfcDoorPanelProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            IfcDoorPanelProperties.EntityTypes.Add(IFCEntityType.IfcDoorStyle);
            ifcPSE = new PropertySetEntry("IfcDoorPanelProperties.PanelDepth", "PanelDepth");
            ifcPSE.PropertyName = "PanelDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorPanelProperties.PanelOperation", "PanelOperation");
            ifcPSE.PropertyName = "PanelOperation";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelOperationCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorPanelProperties.PanelWidth", "PanelWidth");
            ifcPSE.PropertyName = "PanelWidth";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelWidthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorPanelProperties.PanelPosition", "PanelPosition");
            ifcPSE.PropertyName = "PanelPosition";
            ifcPSE.PropertyType = PropertyType.Label;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelPositionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcDoorPanelProperties"))
         {
            IfcDoorPanelProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            IfcDoorPanelProperties.EntityTypes.Add(IFCEntityType.IfcDoorStyle);
            ifcPSE = new PropertySetEntry("IfcDoorPanelProperties.PanelDepth", "PanelDepth");
            ifcPSE.PropertyName = "PanelDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorPanelProperties.PanelOperation", "PanelOperation");
            ifcPSE.PropertyName = "PanelOperation";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelOperationCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorPanelProperties.PanelWidth", "PanelWidth");
            ifcPSE.PropertyName = "PanelWidth";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelWidthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorPanelProperties.PanelPosition", "PanelPosition");
            ifcPSE.PropertyName = "PanelPosition";
            ifcPSE.PropertyType = PropertyType.Label;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelPositionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcDoorPanelProperties"))
         {
            IfcDoorPanelProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            IfcDoorPanelProperties.EntityTypes.Add(IFCEntityType.IfcDoorType);
            ifcPSE = new PropertySetEntry("IfcDoorPanelProperties.PanelDepth", "PanelDepth");
            ifcPSE.PropertyName = "PanelDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorPanelProperties.PanelOperation", "PanelOperation");
            ifcPSE.PropertyName = "PanelOperation";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelOperationCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorPanelProperties.PanelWidth", "PanelWidth");
            ifcPSE.PropertyName = "PanelWidth";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelWidthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcDoorPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcDoorPanelProperties.PanelPosition", "PanelPosition");
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


      private static void InitIfcPermeableCoveringProperties(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription IfcPermeableCoveringProperties = new PropertySetDescription();
         IfcPermeableCoveringProperties.Name = "IfcPermeableCoveringProperties";
         PropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcPermeableCoveringProperties"))
         {
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcDoorStyle);
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcWindowStyle);
            ifcPSE = new PropertySetEntry("IfcPermeableCoveringProperties.OperationType", "OperationType");
            ifcPSE.PropertyName = "OperationType";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OperationTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcPermeableCoveringProperties.PanelPosition", "PanelPosition");
            ifcPSE.PropertyName = "PanelPosition";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelPositionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcPermeableCoveringProperties.FrameDepth", "FrameDepth");
            ifcPSE.PropertyName = "FrameDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcPermeableCoveringProperties.FrameThickness", "FrameThickness");
            ifcPSE.PropertyName = "FrameThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcPermeableCoveringProperties"))
         {
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcDoorStyle);
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcWindowStyle);
            ifcPSE = new PropertySetEntry("IfcPermeableCoveringProperties.OperationType", "OperationType");
            ifcPSE.PropertyName = "OperationType";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OperationTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcPermeableCoveringProperties.PanelPosition", "PanelPosition");
            ifcPSE.PropertyName = "PanelPosition";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelPositionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcPermeableCoveringProperties.FrameDepth", "FrameDepth");
            ifcPSE.PropertyName = "FrameDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcPermeableCoveringProperties.FrameThickness", "FrameThickness");
            ifcPSE.PropertyName = "FrameThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcPermeableCoveringProperties"))
         {
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcDoor);
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcDoorType);
            IfcPermeableCoveringProperties.EntityTypes.Add(IFCEntityType.IfcWindowType);
            ifcPSE = new PropertySetEntry("IfcPermeableCoveringProperties.OperationType", "OperationType");
            ifcPSE.PropertyName = "OperationType";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OperationTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcPermeableCoveringProperties.PanelPosition", "PanelPosition");
            ifcPSE.PropertyName = "PanelPosition";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelPositionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcPermeableCoveringProperties.FrameDepth", "FrameDepth");
            ifcPSE.PropertyName = "FrameDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcPermeableCoveringProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcPermeableCoveringProperties.FrameThickness", "FrameThickness");
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


      private static void InitIfcReinforcementDefinitionProperties(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription IfcReinforcementDefinitionProperties = new PropertySetDescription();
         IfcReinforcementDefinitionProperties.Name = "IfcReinforcementDefinitionProperties";
         PropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcReinforcementDefinitionProperties"))
         {
            IfcReinforcementDefinitionProperties.EntityTypes.Add(IFCEntityType.IfcReinforcingElement);
            ifcPSE = new PropertySetEntry("IfcReinforcementDefinitionProperties.DefinitionType", "DefinitionType");
            ifcPSE.PropertyName = "DefinitionType";
            ifcPSE.PropertyType = PropertyType.Label;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.DefinitionTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcReinforcementDefinitionProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcReinforcementDefinitionProperties.ReinforcementSectionDefinitions", "ReinforcementSectionDefinitions");
            ifcPSE.PropertyName = "ReinforcementSectionDefinitions";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.ListValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ReinforcementSectionDefinitionsCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcReinforcementDefinitionProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcReinforcementDefinitionProperties"))
         {
            IfcReinforcementDefinitionProperties.EntityTypes.Add(IFCEntityType.IfcReinforcingElement);
            ifcPSE = new PropertySetEntry("IfcReinforcementDefinitionProperties.DefinitionType", "DefinitionType");
            ifcPSE.PropertyName = "DefinitionType";
            ifcPSE.PropertyType = PropertyType.Label;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.DefinitionTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcReinforcementDefinitionProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcReinforcementDefinitionProperties.ReinforcementSectionDefinitions", "ReinforcementSectionDefinitions");
            ifcPSE.PropertyName = "ReinforcementSectionDefinitions";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.ListValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ReinforcementSectionDefinitionsCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcReinforcementDefinitionProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcReinforcementDefinitionProperties"))
         {
            IfcReinforcementDefinitionProperties.EntityTypes.Add(IFCEntityType.IfcReinforcingElement);
            ifcPSE = new PropertySetEntry("IfcReinforcementDefinitionProperties.DefinitionType", "DefinitionType");
            ifcPSE.PropertyName = "DefinitionType";
            ifcPSE.PropertyType = PropertyType.Label;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.DefinitionTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcReinforcementDefinitionProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcReinforcementDefinitionProperties.ReinforcementSectionDefinitions", "ReinforcementSectionDefinitions");
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


      private static void InitIfcWindowLiningProperties(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription IfcWindowLiningProperties = new PropertySetDescription();
         IfcWindowLiningProperties.Name = "IfcWindowLiningProperties";
         PropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcWindowLiningProperties"))
         {
            IfcWindowLiningProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            IfcWindowLiningProperties.EntityTypes.Add(IFCEntityType.IfcWindowStyle);
            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.LiningDepth", "LiningDepth");
            ifcPSE.PropertyName = "LiningDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.FirstTransomOffset", "FirstTransomOffset");
            ifcPSE.PropertyName = "FirstTransomOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FirstTransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.SecondTransomOffset", "SecondTransomOffset");
            ifcPSE.PropertyName = "SecondTransomOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SecondTransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.FirstMullionOffset", "FirstMullionOffset");
            ifcPSE.PropertyName = "FirstMullionOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FirstMullionOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.SecondMullionOffset", "SecondMullionOffset");
            ifcPSE.PropertyName = "SecondMullionOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SecondMullionOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.LiningThickness", "LiningThickness");
            ifcPSE.PropertyName = "LiningThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.TransomThickness", "TransomThickness");
            ifcPSE.PropertyName = "TransomThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TransomThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.MullionThickness", "MullionThickness");
            ifcPSE.PropertyName = "MullionThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.MullionThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcWindowLiningProperties"))
         {
            IfcWindowLiningProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            IfcWindowLiningProperties.EntityTypes.Add(IFCEntityType.IfcWindowStyle);
            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.LiningDepth", "LiningDepth");
            ifcPSE.PropertyName = "LiningDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.FirstTransomOffset", "FirstTransomOffset");
            ifcPSE.PropertyName = "FirstTransomOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FirstTransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.SecondTransomOffset", "SecondTransomOffset");
            ifcPSE.PropertyName = "SecondTransomOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SecondTransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.FirstMullionOffset", "FirstMullionOffset");
            ifcPSE.PropertyName = "FirstMullionOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FirstMullionOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.SecondMullionOffset", "SecondMullionOffset");
            ifcPSE.PropertyName = "SecondMullionOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SecondMullionOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.LiningThickness", "LiningThickness");
            ifcPSE.PropertyName = "LiningThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.TransomThickness", "TransomThickness");
            ifcPSE.PropertyName = "TransomThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TransomThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.MullionThickness", "MullionThickness");
            ifcPSE.PropertyName = "MullionThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.MullionThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcWindowLiningProperties"))
         {
            IfcWindowLiningProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            IfcWindowLiningProperties.EntityTypes.Add(IFCEntityType.IfcWindowType);
            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.LiningDepth", "LiningDepth");
            ifcPSE.PropertyName = "LiningDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.FirstTransomOffset", "FirstTransomOffset");
            ifcPSE.PropertyName = "FirstTransomOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FirstTransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.SecondTransomOffset", "SecondTransomOffset");
            ifcPSE.PropertyName = "SecondTransomOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SecondTransomOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.FirstMullionOffset", "FirstMullionOffset");
            ifcPSE.PropertyName = "FirstMullionOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FirstMullionOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.SecondMullionOffset", "SecondMullionOffset");
            ifcPSE.PropertyName = "SecondMullionOffset";
            ifcPSE.PropertyType = PropertyType.NormalisedRatio;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.SecondMullionOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.LiningThickness", "LiningThickness");
            ifcPSE.PropertyName = "LiningThickness";
            ifcPSE.PropertyType = PropertyType.NonNegativeLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.TransomThickness", "TransomThickness");
            ifcPSE.PropertyName = "TransomThickness";
            ifcPSE.PropertyType = PropertyType.NonNegativeLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TransomThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.MullionThickness", "MullionThickness");
            ifcPSE.PropertyName = "MullionThickness";
            ifcPSE.PropertyType = PropertyType.NonNegativeLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.MullionThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.LiningOffset", "LiningOffset");
            ifcPSE.PropertyName = "LiningOffset";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningOffsetCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.LiningToPanelOffsetX", "LiningToPanelOffsetX");
            ifcPSE.PropertyName = "LiningToPanelOffsetX";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningToPanelOffsetXCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowLiningProperties.LiningToPanelOffsetY", "LiningToPanelOffsetY");
            ifcPSE.PropertyName = "LiningToPanelOffsetY";
            ifcPSE.PropertyType = PropertyType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LiningToPanelOffsetYCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowLiningProperties.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            commonPropertySets.Add(IfcWindowLiningProperties);
         }
      }


      private static void InitIfcWindowPanelProperties(IList<PropertySetDescription> commonPropertySets)
      {
         PropertySetDescription IfcWindowPanelProperties = new PropertySetDescription();
         IfcWindowPanelProperties.Name = "IfcWindowPanelProperties";
         PropertySetEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcWindowPanelProperties"))
         {
            IfcWindowPanelProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            IfcWindowPanelProperties.EntityTypes.Add(IFCEntityType.IfcWindowStyle);
            ifcPSE = new PropertySetEntry("IfcWindowPanelProperties.OperationType", "OperationType");
            ifcPSE.PropertyName = "OperationType";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OperationTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowPanelProperties.PanelPosition", "PanelPosition");
            ifcPSE.PropertyName = "PanelPosition";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelPositionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowPanelProperties.FrameDepth", "FrameDepth");
            ifcPSE.PropertyName = "FrameDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowPanelProperties.FrameThickness", "FrameThickness");
            ifcPSE.PropertyName = "FrameThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcWindowPanelProperties"))
         {
            IfcWindowPanelProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            IfcWindowPanelProperties.EntityTypes.Add(IFCEntityType.IfcWindowStyle);
            ifcPSE = new PropertySetEntry("IfcWindowPanelProperties.OperationType", "OperationType");
            ifcPSE.PropertyName = "OperationType";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OperationTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowPanelProperties.PanelPosition", "PanelPosition");
            ifcPSE.PropertyName = "PanelPosition";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelPositionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowPanelProperties.FrameDepth", "FrameDepth");
            ifcPSE.PropertyName = "FrameDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowPanelProperties.FrameThickness", "FrameThickness");
            ifcPSE.PropertyName = "FrameThickness";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameThicknessCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "IfcWindowPanelProperties"))
         {
            IfcWindowPanelProperties.EntityTypes.Add(IFCEntityType.IfcWindow);
            IfcWindowPanelProperties.EntityTypes.Add(IFCEntityType.IfcWindowType);
            ifcPSE = new PropertySetEntry("IfcWindowPanelProperties.OperationType", "OperationType");
            ifcPSE.PropertyName = "OperationType";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OperationTypeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowPanelProperties.PanelPosition", "PanelPosition");
            ifcPSE.PropertyName = "PanelPosition";
            ifcPSE.PropertyType = PropertyType.Label;
            ifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PanelPositionCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowPanelProperties.FrameDepth", "FrameDepth");
            ifcPSE.PropertyName = "FrameDepth";
            ifcPSE.PropertyType = PropertyType.PositiveLength;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FrameDepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            IfcWindowPanelProperties.AddEntry(ifcPSE);

            ifcPSE = new PropertySetEntry("IfcWindowPanelProperties.FrameThickness", "FrameThickness");
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
