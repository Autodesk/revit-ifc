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
      public static void InitQtoSets(IList<IList<QuantityDescription>> allPsetOrQtoSets)
      {
         IList<QuantityDescription> theSets = new List<QuantityDescription>();
         InitQto_ActuatorBaseQuantities(theSets);
         InitQto_AirTerminalBaseQuantities(theSets);
         InitQto_AirTerminalBoxTypeBaseQuantities(theSets);
         InitQto_AirToAirHeatRecoveryBaseQuantities(theSets);
         InitQto_AlarmBaseQuantities(theSets);
         InitQto_AudioVisualApplianceBaseQuantities(theSets);
         InitQto_BeamBaseQuantities(theSets);
         InitQto_BoilerBaseQuantities(theSets);
         InitQto_BuildingBaseQuantities(theSets);
         InitQto_BuildingElementProxyQuantities(theSets);
         InitQto_BuildingStoreyBaseQuantities(theSets);
         InitQto_BurnerBaseQuantities(theSets);
         InitQto_CableCarrierFittingBaseQuantities(theSets);
         InitQto_CableCarrierSegmentBaseQuantities(theSets);
         InitQto_CableFittingBaseQuantities(theSets);
         InitQto_CableSegmentBaseQuantities(theSets);
         InitQto_ChillerBaseQuantities(theSets);
         InitQto_ChimneyBaseQuantities(theSets);
         InitQto_CoilBaseQuantities(theSets);
         InitQto_ColumnBaseQuantities(theSets);
         InitQto_CommunicationsApplianceBaseQuantities(theSets);
         InitQto_CompressorBaseQuantities(theSets);
         InitQto_CondenserBaseQuantities(theSets);
         InitQto_ConstructionEquipmentResourceBaseQuantities(theSets);
         InitQto_ConstructionMaterialResourceBaseQuantities(theSets);
         InitQto_ControllerBaseQuantities(theSets);
         InitQto_CooledBeamBaseQuantities(theSets);
         InitQto_CoolingTowerBaseQuantities(theSets);
         InitQto_CoveringBaseQuantities(theSets);
         InitQto_CurtainWallQuantities(theSets);
         InitQto_DamperBaseQuantities(theSets);
         InitQto_DistributionChamberElementBaseQuantities(theSets);
         InitQto_DoorBaseQuantities(theSets);
         InitQto_DuctFittingBaseQuantities(theSets);
         InitQto_DuctSegmentBaseQuantities(theSets);
         InitQto_DuctSilencerBaseQuantities(theSets);
         InitQto_ElectricApplianceBaseQuantities(theSets);
         InitQto_ElectricDistributionBoardBaseQuantities(theSets);
         InitQto_ElectricFlowStorageDeviceBaseQuantities(theSets);
         InitQto_ElectricGeneratorBaseQuantities(theSets);
         InitQto_ElectricMotorBaseQuantities(theSets);
         InitQto_ElectricTimeControlBaseQuantities(theSets);
         InitQto_EvaporativeCoolerBaseQuantities(theSets);
         InitQto_EvaporatorBaseQuantities(theSets);
         InitQto_FanBaseQuantities(theSets);
         InitQto_FilterBaseQuantities(theSets);
         InitQto_FireSuppressionTerminalBaseQuantities(theSets);
         InitQto_FlowInstrumentBaseQuantities(theSets);
         InitQto_FlowMeterBaseQuantities(theSets);
         InitQto_FootingBaseQuantities(theSets);
         InitQto_HeatExchangerBaseQuantities(theSets);
         InitQto_HumidifierBaseQuantities(theSets);
         InitQto_InterceptorBaseQuantities(theSets);
         InitQto_JunctionBoxBaseQuantities(theSets);
         InitQto_LaborResourceBaseQuantities(theSets);
         InitQto_LampBaseQuantities(theSets);
         InitQto_LightFixtureBaseQuantities(theSets);
         InitQto_MemberBaseQuantities(theSets);
         InitQto_MotorConnectionBaseQuantities(theSets);
         InitQto_OpeningElementBaseQuantities(theSets);
         InitQto_OutletBaseQuantities(theSets);
         InitQto_PileBaseQuantities(theSets);
         InitQto_PipeFittingBaseQuantities(theSets);
         InitQto_PipeSegmentBaseQuantities(theSets);
         InitQto_PlateBaseQuantities(theSets);
         InitQto_ProjectionElementBaseQuantities(theSets);
         InitQto_ProtectiveDeviceBaseQuantities(theSets);
         InitQto_ProtectiveDeviceTrippingUnitBaseQuantities(theSets);
         InitQto_PumpBaseQuantities(theSets);
         InitQto_RailingBaseQuantities(theSets);
         InitQto_RampFlightBaseQuantities(theSets);
         InitQto_ReinforcingElementBaseQuantities(theSets);
         InitQto_RoofBaseQuantities(theSets);
         InitQto_SanitaryTerminalBaseQuantities(theSets);
         InitQto_SensorBaseQuantities(theSets);
         InitQto_SiteBaseQuantities(theSets);
         InitQto_SlabBaseQuantities(theSets);
         InitQto_SolarDeviceBaseQuantities(theSets);
         InitQto_SpaceBaseQuantities(theSets);
         InitQto_SpaceHeaterBaseQuantities(theSets);
         InitQto_StackTerminalBaseQuantities(theSets);
         InitQto_StairFlightBaseQuantities(theSets);
         InitQto_SwitchingDeviceBaseQuantities(theSets);
         InitQto_TankBaseQuantities(theSets);
         InitQto_TransformerBaseQuantities(theSets);
         InitQto_TubeBundleBaseQuantities(theSets);
         InitQto_UnitaryControlElementBaseQuantities(theSets);
         InitQto_UnitaryEquipmentBaseQuantities(theSets);
         InitQto_ValveBaseQuantities(theSets);
         InitQto_VibrationIsolatorBaseQuantities(theSets);
         InitQto_WallBaseQuantities(theSets);
         InitQto_WasteTerminalBaseQuantities(theSets);
         InitQto_WindowBaseQuantities(theSets);

         allPsetOrQtoSets.Add(theSets);
      }

      private static void InitQto_ActuatorBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetActuatorBaseQuantities = new QuantityDescription();
         qtoSetActuatorBaseQuantities.Name = "Qto_ActuatorBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_ActuatorBaseQuantities"))
         {
            qtoSetActuatorBaseQuantities.EntityTypes.Add(IFCEntityType.IfcActuator);
            qtoSetActuatorBaseQuantities.ObjectType = "IfcActuator";
            ifcPSE = new QuantityEntry("Qto_ActuatorBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetActuatorBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetActuatorBaseQuantities);
         }
      }


      private static void InitQto_AirTerminalBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetAirTerminalBaseQuantities = new QuantityDescription();
         qtoSetAirTerminalBaseQuantities.Name = "Qto_AirTerminalBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_AirTerminalBaseQuantities"))
         {
            qtoSetAirTerminalBaseQuantities.EntityTypes.Add(IFCEntityType.IfcAirTerminal);
            qtoSetAirTerminalBaseQuantities.ObjectType = "IfcAirTerminal";
            ifcPSE = new QuantityEntry("Qto_AirTerminalBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetAirTerminalBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_AirTerminalBaseQuantities.Perimeter", "Perimeter");
            ifcPSE.PropertyName = "Perimeter";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Perimeter");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PerimeterCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetAirTerminalBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_AirTerminalBaseQuantities.TotalSurfaceArea", "TotalSurfaceArea");
            ifcPSE.PropertyName = "TotalSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Total Surface Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TotalSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetAirTerminalBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetAirTerminalBaseQuantities);
         }
      }


      private static void InitQto_AirTerminalBoxTypeBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetAirTerminalBoxTypeBaseQuantities = new QuantityDescription();
         qtoSetAirTerminalBoxTypeBaseQuantities.Name = "Qto_AirTerminalBoxTypeBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_AirTerminalBoxTypeBaseQuantities"))
         {
            qtoSetAirTerminalBoxTypeBaseQuantities.EntityTypes.Add(IFCEntityType.IfcAirTerminalBox);
            qtoSetAirTerminalBoxTypeBaseQuantities.ObjectType = "IfcAirTerminalBox";
            ifcPSE = new QuantityEntry("Qto_AirTerminalBoxTypeBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetAirTerminalBoxTypeBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetAirTerminalBoxTypeBaseQuantities);
         }
      }


      private static void InitQto_AirToAirHeatRecoveryBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetAirToAirHeatRecoveryBaseQuantities = new QuantityDescription();
         qtoSetAirToAirHeatRecoveryBaseQuantities.Name = "Qto_AirToAirHeatRecoveryBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_AirToAirHeatRecoveryBaseQuantities"))
         {
            qtoSetAirToAirHeatRecoveryBaseQuantities.EntityTypes.Add(IFCEntityType.IfcAirToAirHeatRecovery);
            qtoSetAirToAirHeatRecoveryBaseQuantities.ObjectType = "IfcAirToAirHeatRecovery";
            ifcPSE = new QuantityEntry("Qto_AirToAirHeatRecoveryBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetAirToAirHeatRecoveryBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetAirToAirHeatRecoveryBaseQuantities);
         }
      }


      private static void InitQto_AlarmBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetAlarmBaseQuantities = new QuantityDescription();
         qtoSetAlarmBaseQuantities.Name = "Qto_AlarmBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_AlarmBaseQuantities"))
         {
            qtoSetAlarmBaseQuantities.EntityTypes.Add(IFCEntityType.IfcAlarm);
            qtoSetAlarmBaseQuantities.ObjectType = "IfcAlarm";
            ifcPSE = new QuantityEntry("Qto_AlarmBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetAlarmBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetAlarmBaseQuantities);
         }
      }


      private static void InitQto_AudioVisualApplianceBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetAudioVisualApplianceBaseQuantities = new QuantityDescription();
         qtoSetAudioVisualApplianceBaseQuantities.Name = "Qto_AudioVisualApplianceBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_AudioVisualApplianceBaseQuantities"))
         {
            qtoSetAudioVisualApplianceBaseQuantities.EntityTypes.Add(IFCEntityType.IfcAudioVisualAppliance);
            qtoSetAudioVisualApplianceBaseQuantities.ObjectType = "IfcAudioVisualAppliance";
            ifcPSE = new QuantityEntry("Qto_AudioVisualApplianceBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetAudioVisualApplianceBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetAudioVisualApplianceBaseQuantities);
         }
      }


      private static void InitQto_BeamBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetBeamBaseQuantities = new QuantityDescription();
         qtoSetBeamBaseQuantities.Name = "Qto_BeamBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_BeamBaseQuantities"))
         {
            qtoSetBeamBaseQuantities.EntityTypes.Add(IFCEntityType.IfcBeam);
            qtoSetBeamBaseQuantities.ObjectType = "IfcBeam";
            ifcPSE = new QuantityEntry("Qto_BeamBaseQuantities.Length", "Length");
            ifcPSE.PropertyName = "Length";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Länge");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Length");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "長さ");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBeamBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BeamBaseQuantities.CrossSectionArea", "CrossSectionArea");
            ifcPSE.PropertyName = "CrossSectionArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Querschnittsfläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Cross Section Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "断面面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CrossSectionAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBeamBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BeamBaseQuantities.OuterSurfaceArea", "OuterSurfaceArea");
            ifcPSE.PropertyName = "OuterSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Mantelfläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Outer Surface Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "外表面面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OuterSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBeamBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BeamBaseQuantities.GrossSurfaceArea", "GrossSurfaceArea");
            ifcPSE.PropertyName = "GrossSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Gesamtoberfläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Surface Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "表面面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBeamBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BeamBaseQuantities.NetSurfaceArea", "NetSurfaceArea");
            ifcPSE.PropertyName = "NetSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettooberfläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Surface Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBeamBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BeamBaseQuantities.GrossVolume", "GrossVolume");
            ifcPSE.PropertyName = "GrossVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttovolumen");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Volume");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "体積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBeamBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BeamBaseQuantities.NetVolume", "NetVolume");
            ifcPSE.PropertyName = "NetVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettovolumen");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Volume");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味体積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBeamBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BeamBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttogewicht");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "重量");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBeamBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BeamBaseQuantities.NetWeight", "NetWeight");
            ifcPSE.PropertyName = "NetWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettogewicht");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Weight");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味重量");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBeamBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetBeamBaseQuantities);
         }
      }


      private static void InitQto_BoilerBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetBoilerBaseQuantities = new QuantityDescription();
         qtoSetBoilerBaseQuantities.Name = "Qto_BoilerBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_BoilerBaseQuantities"))
         {
            qtoSetBoilerBaseQuantities.EntityTypes.Add(IFCEntityType.IfcBoiler);
            qtoSetBoilerBaseQuantities.ObjectType = "IfcBoiler";
            ifcPSE = new QuantityEntry("Qto_BoilerBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBoilerBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BoilerBaseQuantities.NetWeight", "NetWeight");
            ifcPSE.PropertyName = "NetWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBoilerBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BoilerBaseQuantities.TotalSurfaceArea", "TotalSurfaceArea");
            ifcPSE.PropertyName = "TotalSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Total Surface Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TotalSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBoilerBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetBoilerBaseQuantities);
         }
      }


      private static void InitQto_BuildingBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetBuildingBaseQuantities = new QuantityDescription();
         qtoSetBuildingBaseQuantities.Name = "Qto_BuildingBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_BuildingBaseQuantities"))
         {
            qtoSetBuildingBaseQuantities.EntityTypes.Add(IFCEntityType.IfcBuilding);
            qtoSetBuildingBaseQuantities.ObjectType = "IfcBuilding";
            ifcPSE = new QuantityEntry("Qto_BuildingBaseQuantities.Height", "Height");
            ifcPSE.PropertyName = "Height";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Firsthöhe");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Height");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "高さ");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.HeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBuildingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BuildingBaseQuantities.EavesHeight", "EavesHeight");
            ifcPSE.PropertyName = "EavesHeight";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Traufhöhe");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Eaves Height");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.EavesHeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBuildingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BuildingBaseQuantities.FootprintArea", "FootprintArea");
            ifcPSE.PropertyName = "FootprintArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bebaute Fläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Footprint Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "建築面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FootprintAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBuildingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BuildingBaseQuantities.GrossFloorArea", "GrossFloorArea");
            ifcPSE.PropertyName = "GrossFloorArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttogrundfläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Floor Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "延べ床面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossFloorAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBuildingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BuildingBaseQuantities.NetFloorArea", "NetFloorArea");
            ifcPSE.PropertyName = "NetFloorArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettogrundfläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Floor Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味床面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetFloorAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBuildingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BuildingBaseQuantities.GrossVolume", "GrossVolume");
            ifcPSE.PropertyName = "GrossVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttorauminhalt");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Volume");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "建物体積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBuildingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BuildingBaseQuantities.NetVolume", "NetVolume");
            ifcPSE.PropertyName = "NetVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettorauminhalt");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Volume");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味建物体積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBuildingBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetBuildingBaseQuantities);
         }
      }


      private static void InitQto_BuildingElementProxyQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetBuildingElementProxyQuantities = new QuantityDescription();
         qtoSetBuildingElementProxyQuantities.Name = "Qto_BuildingElementProxyQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_BuildingElementProxyQuantities"))
         {
            qtoSetBuildingElementProxyQuantities.EntityTypes.Add(IFCEntityType.IfcBuildingElementProxy);
            qtoSetBuildingElementProxyQuantities.ObjectType = "IfcBuildingElementProxy";
            ifcPSE = new QuantityEntry("Qto_BuildingElementProxyQuantities.NetSurfaceArea", "NetSurfaceArea");
            ifcPSE.PropertyName = "NetSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBuildingElementProxyQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BuildingElementProxyQuantities.NetVolume", "NetVolume");
            ifcPSE.PropertyName = "NetVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBuildingElementProxyQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetBuildingElementProxyQuantities);
         }
      }


      private static void InitQto_BuildingStoreyBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetBuildingStoreyBaseQuantities = new QuantityDescription();
         qtoSetBuildingStoreyBaseQuantities.Name = "Qto_BuildingStoreyBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_BuildingStoreyBaseQuantities"))
         {
            qtoSetBuildingStoreyBaseQuantities.EntityTypes.Add(IFCEntityType.IfcBuildingStorey);
            qtoSetBuildingStoreyBaseQuantities.ObjectType = "IfcBuildingStorey";
            ifcPSE = new QuantityEntry("Qto_BuildingStoreyBaseQuantities.GrossHeight", "GrossHeight");
            ifcPSE.PropertyName = "GrossHeight";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Systemhöhe");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Height");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "階高");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossHeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBuildingStoreyBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BuildingStoreyBaseQuantities.NetHeigtht", "NetHeigtht");
            ifcPSE.PropertyName = "NetHeigtht";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Lichte Höhe");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Heigtht");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味階高");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetHeigthtCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBuildingStoreyBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BuildingStoreyBaseQuantities.GrossPerimeter", "GrossPerimeter");
            ifcPSE.PropertyName = "GrossPerimeter";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Umfang");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Perimeter");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "周辺長");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossPerimeterCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBuildingStoreyBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BuildingStoreyBaseQuantities.GrossFloorArea", "GrossFloorArea");
            ifcPSE.PropertyName = "GrossFloorArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttogrundfläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Floor Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "床面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossFloorAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBuildingStoreyBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BuildingStoreyBaseQuantities.NetFloorArea", "NetFloorArea");
            ifcPSE.PropertyName = "NetFloorArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettogrundfläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Floor Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味床面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetFloorAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBuildingStoreyBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BuildingStoreyBaseQuantities.GrossVolume", "GrossVolume");
            ifcPSE.PropertyName = "GrossVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttorauminhalt");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Volume");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "建物階体積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBuildingStoreyBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_BuildingStoreyBaseQuantities.NetVolume", "NetVolume");
            ifcPSE.PropertyName = "NetVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettorauminhalt");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Volume");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味建物階体積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBuildingStoreyBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetBuildingStoreyBaseQuantities);
         }
      }


      private static void InitQto_BurnerBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetBurnerBaseQuantities = new QuantityDescription();
         qtoSetBurnerBaseQuantities.Name = "Qto_BurnerBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_BurnerBaseQuantities"))
         {
            qtoSetBurnerBaseQuantities.EntityTypes.Add(IFCEntityType.IfcBurner);
            qtoSetBurnerBaseQuantities.ObjectType = "IfcBurner";
            ifcPSE = new QuantityEntry("Qto_BurnerBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetBurnerBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetBurnerBaseQuantities);
         }
      }


      private static void InitQto_CableCarrierFittingBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetCableCarrierFittingBaseQuantities = new QuantityDescription();
         qtoSetCableCarrierFittingBaseQuantities.Name = "Qto_CableCarrierFittingBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_CableCarrierFittingBaseQuantities"))
         {
            qtoSetCableCarrierFittingBaseQuantities.EntityTypes.Add(IFCEntityType.IfcCableCarrierFitting);
            qtoSetCableCarrierFittingBaseQuantities.ObjectType = "IfcCableCarrierFitting";
            ifcPSE = new QuantityEntry("Qto_CableCarrierFittingBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCableCarrierFittingBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetCableCarrierFittingBaseQuantities);
         }
      }


      private static void InitQto_CableCarrierSegmentBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetCableCarrierSegmentBaseQuantities = new QuantityDescription();
         qtoSetCableCarrierSegmentBaseQuantities.Name = "Qto_CableCarrierSegmentBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_CableCarrierSegmentBaseQuantities"))
         {
            qtoSetCableCarrierSegmentBaseQuantities.EntityTypes.Add(IFCEntityType.IfcCableCarrierSegment);
            qtoSetCableCarrierSegmentBaseQuantities.ObjectType = "IfcCableCarrierSegment";
            ifcPSE = new QuantityEntry("Qto_CableCarrierSegmentBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCableCarrierSegmentBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_CableCarrierSegmentBaseQuantities.Length", "Length");
            ifcPSE.PropertyName = "Length";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Length");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCableCarrierSegmentBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_CableCarrierSegmentBaseQuantities.CrossSectionArea", "CrossSectionArea");
            ifcPSE.PropertyName = "CrossSectionArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Cross Section Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CrossSectionAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCableCarrierSegmentBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_CableCarrierSegmentBaseQuantities.OuterSurfaceArea", "OuterSurfaceArea");
            ifcPSE.PropertyName = "OuterSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Outer Surface Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OuterSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCableCarrierSegmentBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetCableCarrierSegmentBaseQuantities);
         }
      }


      private static void InitQto_CableFittingBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetCableFittingBaseQuantities = new QuantityDescription();
         qtoSetCableFittingBaseQuantities.Name = "Qto_CableFittingBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_CableFittingBaseQuantities"))
         {
            qtoSetCableFittingBaseQuantities.EntityTypes.Add(IFCEntityType.IfcCableFitting);
            qtoSetCableFittingBaseQuantities.ObjectType = "IfcCableFitting";
            ifcPSE = new QuantityEntry("Qto_CableFittingBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCableFittingBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetCableFittingBaseQuantities);
         }
      }


      private static void InitQto_CableSegmentBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetCableSegmentBaseQuantities = new QuantityDescription();
         qtoSetCableSegmentBaseQuantities.Name = "Qto_CableSegmentBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_CableSegmentBaseQuantities"))
         {
            qtoSetCableSegmentBaseQuantities.EntityTypes.Add(IFCEntityType.IfcCableSegment);
            qtoSetCableSegmentBaseQuantities.ObjectType = "IfcCableSegment";
            ifcPSE = new QuantityEntry("Qto_CableSegmentBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCableSegmentBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_CableSegmentBaseQuantities.Length", "Length");
            ifcPSE.PropertyName = "Length";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Length");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCableSegmentBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_CableSegmentBaseQuantities.CrossSectionArea", "CrossSectionArea");
            ifcPSE.PropertyName = "CrossSectionArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Cross Section Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CrossSectionAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCableSegmentBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_CableSegmentBaseQuantities.OuterSurfaceArea", "OuterSurfaceArea");
            ifcPSE.PropertyName = "OuterSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Outer Surface Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OuterSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCableSegmentBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetCableSegmentBaseQuantities);
         }
      }


      private static void InitQto_ChillerBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetChillerBaseQuantities = new QuantityDescription();
         qtoSetChillerBaseQuantities.Name = "Qto_ChillerBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_ChillerBaseQuantities"))
         {
            qtoSetChillerBaseQuantities.EntityTypes.Add(IFCEntityType.IfcChiller);
            qtoSetChillerBaseQuantities.ObjectType = "IfcChiller";
            ifcPSE = new QuantityEntry("Qto_ChillerBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetChillerBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetChillerBaseQuantities);
         }
      }


      private static void InitQto_ChimneyBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetChimneyBaseQuantities = new QuantityDescription();
         qtoSetChimneyBaseQuantities.Name = "Qto_ChimneyBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_ChimneyBaseQuantities"))
         {
            qtoSetChimneyBaseQuantities.EntityTypes.Add(IFCEntityType.IfcChimney);
            qtoSetChimneyBaseQuantities.ObjectType = "IfcChimney";
            ifcPSE = new QuantityEntry("Qto_ChimneyBaseQuantities.Length", "Length");
            ifcPSE.PropertyName = "Length";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Länge");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Length");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "長さ");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetChimneyBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetChimneyBaseQuantities);
         }
      }


      private static void InitQto_CoilBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetCoilBaseQuantities = new QuantityDescription();
         qtoSetCoilBaseQuantities.Name = "Qto_CoilBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_CoilBaseQuantities"))
         {
            qtoSetCoilBaseQuantities.EntityTypes.Add(IFCEntityType.IfcCoil);
            qtoSetCoilBaseQuantities.ObjectType = "IfcCoil";
            ifcPSE = new QuantityEntry("Qto_CoilBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCoilBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetCoilBaseQuantities);
         }
      }


      private static void InitQto_ColumnBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetColumnBaseQuantities = new QuantityDescription();
         qtoSetColumnBaseQuantities.Name = "Qto_ColumnBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_ColumnBaseQuantities"))
         {
            qtoSetColumnBaseQuantities.EntityTypes.Add(IFCEntityType.IfcColumn);
            qtoSetColumnBaseQuantities.ObjectType = "IfcColumn";
            ifcPSE = new QuantityEntry("Qto_ColumnBaseQuantities.Length", "Length");
            ifcPSE.PropertyName = "Length";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Länge");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Length");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "長さ");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetColumnBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_ColumnBaseQuantities.CrossSectionArea", "CrossSectionArea");
            ifcPSE.PropertyName = "CrossSectionArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Querschnittsfläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Cross Section Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "断面面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CrossSectionAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetColumnBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_ColumnBaseQuantities.OuterSurfaceArea", "OuterSurfaceArea");
            ifcPSE.PropertyName = "OuterSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Mantelfläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Outer Surface Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "外表面面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OuterSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetColumnBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_ColumnBaseQuantities.GrossSurfaceArea", "GrossSurfaceArea");
            ifcPSE.PropertyName = "GrossSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Gesamtoberfläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Surface Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "表面面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetColumnBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_ColumnBaseQuantities.NetSurfaceArea", "NetSurfaceArea");
            ifcPSE.PropertyName = "NetSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettooberfläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Surface Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetColumnBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_ColumnBaseQuantities.GrossVolume", "GrossVolume");
            ifcPSE.PropertyName = "GrossVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttovolumen");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Volume");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "体積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetColumnBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_ColumnBaseQuantities.NetVolume", "NetVolume");
            ifcPSE.PropertyName = "NetVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettovolumen");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Volume");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味体積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetColumnBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_ColumnBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttogewicht");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "重量");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetColumnBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_ColumnBaseQuantities.NetWeight", "NetWeight");
            ifcPSE.PropertyName = "NetWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettogewicht");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Weight");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味重量");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetColumnBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetColumnBaseQuantities);
         }
      }


      private static void InitQto_CommunicationsApplianceBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetCommunicationsApplianceBaseQuantities = new QuantityDescription();
         qtoSetCommunicationsApplianceBaseQuantities.Name = "Qto_CommunicationsApplianceBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_CommunicationsApplianceBaseQuantities"))
         {
            qtoSetCommunicationsApplianceBaseQuantities.EntityTypes.Add(IFCEntityType.IfcCommunicationsAppliance);
            qtoSetCommunicationsApplianceBaseQuantities.ObjectType = "IfcCommunicationsAppliance";
            ifcPSE = new QuantityEntry("Qto_CommunicationsApplianceBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCommunicationsApplianceBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetCommunicationsApplianceBaseQuantities);
         }
      }


      private static void InitQto_CompressorBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetCompressorBaseQuantities = new QuantityDescription();
         qtoSetCompressorBaseQuantities.Name = "Qto_CompressorBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_CompressorBaseQuantities"))
         {
            qtoSetCompressorBaseQuantities.EntityTypes.Add(IFCEntityType.IfcCompressor);
            qtoSetCompressorBaseQuantities.ObjectType = "IfcCompressor";
            ifcPSE = new QuantityEntry("Qto_CompressorBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCompressorBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetCompressorBaseQuantities);
         }
      }


      private static void InitQto_CondenserBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetCondenserBaseQuantities = new QuantityDescription();
         qtoSetCondenserBaseQuantities.Name = "Qto_CondenserBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_CondenserBaseQuantities"))
         {
            qtoSetCondenserBaseQuantities.EntityTypes.Add(IFCEntityType.IfcCondenser);
            qtoSetCondenserBaseQuantities.ObjectType = "IfcCondenser";
            ifcPSE = new QuantityEntry("Qto_CondenserBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCondenserBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetCondenserBaseQuantities);
         }
      }


      private static void InitQto_ConstructionEquipmentResourceBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetConstructionEquipmentResourceBaseQuantities = new QuantityDescription();
         qtoSetConstructionEquipmentResourceBaseQuantities.Name = "Qto_ConstructionEquipmentResourceBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_ConstructionEquipmentResourceBaseQuantities"))
         {
            qtoSetConstructionEquipmentResourceBaseQuantities.EntityTypes.Add(IFCEntityType.IfcConstructionEquipmentResource);
            qtoSetConstructionEquipmentResourceBaseQuantities.ObjectType = "IfcConstructionEquipmentResource";
            ifcPSE = new QuantityEntry("Qto_ConstructionEquipmentResourceBaseQuantities.UsageTime", "UsageTime");
            ifcPSE.PropertyName = "UsageTime";
            ifcPSE.QuantityType = QuantityType.Time;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Usage Time");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.UsageTimeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetConstructionEquipmentResourceBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_ConstructionEquipmentResourceBaseQuantities.OperatingTime", "OperatingTime");
            ifcPSE.PropertyName = "OperatingTime";
            ifcPSE.QuantityType = QuantityType.Time;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Operating Time");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OperatingTimeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetConstructionEquipmentResourceBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetConstructionEquipmentResourceBaseQuantities);
         }
      }


      private static void InitQto_ConstructionMaterialResourceBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetConstructionMaterialResourceBaseQuantities = new QuantityDescription();
         qtoSetConstructionMaterialResourceBaseQuantities.Name = "Qto_ConstructionMaterialResourceBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_ConstructionMaterialResourceBaseQuantities"))
         {
            qtoSetConstructionMaterialResourceBaseQuantities.EntityTypes.Add(IFCEntityType.IfcConstructionMaterialResource);
            qtoSetConstructionMaterialResourceBaseQuantities.ObjectType = "IfcConstructionMaterialResource";
            ifcPSE = new QuantityEntry("Qto_ConstructionMaterialResourceBaseQuantities.GrossVolume", "GrossVolume");
            ifcPSE.PropertyName = "GrossVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Volume");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetConstructionMaterialResourceBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_ConstructionMaterialResourceBaseQuantities.NetVolume", "NetVolume");
            ifcPSE.PropertyName = "NetVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Volume");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetConstructionMaterialResourceBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_ConstructionMaterialResourceBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetConstructionMaterialResourceBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_ConstructionMaterialResourceBaseQuantities.NetWeight", "NetWeight");
            ifcPSE.PropertyName = "NetWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetConstructionMaterialResourceBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetConstructionMaterialResourceBaseQuantities);
         }
      }


      private static void InitQto_ControllerBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetControllerBaseQuantities = new QuantityDescription();
         qtoSetControllerBaseQuantities.Name = "Qto_ControllerBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_ControllerBaseQuantities"))
         {
            qtoSetControllerBaseQuantities.EntityTypes.Add(IFCEntityType.IfcController);
            qtoSetControllerBaseQuantities.ObjectType = "IfcController";
            ifcPSE = new QuantityEntry("Qto_ControllerBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetControllerBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetControllerBaseQuantities);
         }
      }


      private static void InitQto_CooledBeamBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetCooledBeamBaseQuantities = new QuantityDescription();
         qtoSetCooledBeamBaseQuantities.Name = "Qto_CooledBeamBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_CooledBeamBaseQuantities"))
         {
            qtoSetCooledBeamBaseQuantities.EntityTypes.Add(IFCEntityType.IfcCooledBeam);
            qtoSetCooledBeamBaseQuantities.ObjectType = "IfcCooledBeam";
            ifcPSE = new QuantityEntry("Qto_CooledBeamBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCooledBeamBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetCooledBeamBaseQuantities);
         }
      }


      private static void InitQto_CoolingTowerBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetCoolingTowerBaseQuantities = new QuantityDescription();
         qtoSetCoolingTowerBaseQuantities.Name = "Qto_CoolingTowerBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_CoolingTowerBaseQuantities"))
         {
            qtoSetCoolingTowerBaseQuantities.EntityTypes.Add(IFCEntityType.IfcCoolingTower);
            qtoSetCoolingTowerBaseQuantities.ObjectType = "IfcCoolingTower";
            ifcPSE = new QuantityEntry("Qto_CoolingTowerBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCoolingTowerBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetCoolingTowerBaseQuantities);
         }
      }


      private static void InitQto_CoveringBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetCoveringBaseQuantities = new QuantityDescription();
         qtoSetCoveringBaseQuantities.Name = "Qto_CoveringBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_CoveringBaseQuantities"))
         {
            qtoSetCoveringBaseQuantities.EntityTypes.Add(IFCEntityType.IfcCovering);
            qtoSetCoveringBaseQuantities.ObjectType = "IfcCovering";
            ifcPSE = new QuantityEntry("Qto_CoveringBaseQuantities.Width", "Width");
            ifcPSE.PropertyName = "Width";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Dicke");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Width");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "幅");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.WidthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCoveringBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_CoveringBaseQuantities.GrossArea", "GrossArea");
            ifcPSE.PropertyName = "GrossArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttofläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCoveringBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_CoveringBaseQuantities.NetArea", "NetArea");
            ifcPSE.PropertyName = "NetArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettofläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCoveringBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetCoveringBaseQuantities);
         }
      }


      private static void InitQto_CurtainWallQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetCurtainWallQuantities = new QuantityDescription();
         qtoSetCurtainWallQuantities.Name = "Qto_CurtainWallQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_CurtainWallQuantities"))
         {
            qtoSetCurtainWallQuantities.EntityTypes.Add(IFCEntityType.IfcCurtainWall);
            qtoSetCurtainWallQuantities.ObjectType = "IfcCurtainWall";
            ifcPSE = new QuantityEntry("Qto_CurtainWallQuantities.Length", "Length");
            ifcPSE.PropertyName = "Length";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Länge");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Length");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "長さ");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCurtainWallQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_CurtainWallQuantities.Height", "Height");
            ifcPSE.PropertyName = "Height";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Höhe");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Height");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "高さ");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.HeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCurtainWallQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_CurtainWallQuantities.Width", "Width");
            ifcPSE.PropertyName = "Width";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Dicke");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Width");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "幅");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.WidthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCurtainWallQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_CurtainWallQuantities.GrossSideArea", "GrossSideArea");
            ifcPSE.PropertyName = "GrossSideArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttofläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Side Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "側面面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossSideAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCurtainWallQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_CurtainWallQuantities.NetSideArea", "NetSideArea");
            ifcPSE.PropertyName = "NetSideArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettofläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Side Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味側面面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetSideAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetCurtainWallQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetCurtainWallQuantities);
         }
      }


      private static void InitQto_DamperBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetDamperBaseQuantities = new QuantityDescription();
         qtoSetDamperBaseQuantities.Name = "Qto_DamperBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_DamperBaseQuantities"))
         {
            qtoSetDamperBaseQuantities.EntityTypes.Add(IFCEntityType.IfcDamper);
            qtoSetDamperBaseQuantities.ObjectType = "IfcDamper";
            ifcPSE = new QuantityEntry("Qto_DamperBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetDamperBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetDamperBaseQuantities);
         }
      }


      private static void InitQto_DistributionChamberElementBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetDistributionChamberElementBaseQuantities = new QuantityDescription();
         qtoSetDistributionChamberElementBaseQuantities.Name = "Qto_DistributionChamberElementBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_DistributionChamberElementBaseQuantities"))
         {
            qtoSetDistributionChamberElementBaseQuantities.EntityTypes.Add(IFCEntityType.IfcDistributionChamberElement);
            qtoSetDistributionChamberElementBaseQuantities.ObjectType = "IfcDistributionChamberElement";
            ifcPSE = new QuantityEntry("Qto_DistributionChamberElementBaseQuantities.GrossSurfaceArea", "GrossSurfaceArea");
            ifcPSE.PropertyName = "GrossSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Surface Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetDistributionChamberElementBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_DistributionChamberElementBaseQuantities.NetSurfaceArea", "NetSurfaceArea");
            ifcPSE.PropertyName = "NetSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Surface Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetDistributionChamberElementBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_DistributionChamberElementBaseQuantities.GrossVolume", "GrossVolume");
            ifcPSE.PropertyName = "GrossVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Volume");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetDistributionChamberElementBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_DistributionChamberElementBaseQuantities.NetVolume", "NetVolume");
            ifcPSE.PropertyName = "NetVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Volume");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetDistributionChamberElementBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetDistributionChamberElementBaseQuantities);
         }
      }


      private static void InitQto_DoorBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetDoorBaseQuantities = new QuantityDescription();
         qtoSetDoorBaseQuantities.Name = "Qto_DoorBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_DoorBaseQuantities"))
         {
            qtoSetDoorBaseQuantities.EntityTypes.Add(IFCEntityType.IfcDoor);
            qtoSetDoorBaseQuantities.ObjectType = "IfcDoor";
            ifcPSE = new QuantityEntry("Qto_DoorBaseQuantities.Width", "Width");
            ifcPSE.PropertyName = "Width";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Breite");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Width");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "幅");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.WidthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetDoorBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_DoorBaseQuantities.Height", "Height");
            ifcPSE.PropertyName = "Height";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Höhe");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Height");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "高さ");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.HeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetDoorBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_DoorBaseQuantities.Perimeter", "Perimeter");
            ifcPSE.PropertyName = "Perimeter";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Umfang");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Perimeter");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "周囲長");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PerimeterCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetDoorBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_DoorBaseQuantities.Area", "Area");
            ifcPSE.PropertyName = "Area";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Fläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.AreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetDoorBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetDoorBaseQuantities);
         }
      }


      private static void InitQto_DuctFittingBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetDuctFittingBaseQuantities = new QuantityDescription();
         qtoSetDuctFittingBaseQuantities.Name = "Qto_DuctFittingBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_DuctFittingBaseQuantities"))
         {
            qtoSetDuctFittingBaseQuantities.EntityTypes.Add(IFCEntityType.IfcDuctFitting);
            qtoSetDuctFittingBaseQuantities.ObjectType = "IfcDuctFitting";
            ifcPSE = new QuantityEntry("Qto_DuctFittingBaseQuantities.Length", "Length");
            ifcPSE.PropertyName = "Length";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Length");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetDuctFittingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_DuctFittingBaseQuantities.GrossCrossSectionArea", "GrossCrossSectionArea");
            ifcPSE.PropertyName = "GrossCrossSectionArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Cross Section Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossCrossSectionAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetDuctFittingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_DuctFittingBaseQuantities.NetCrossSectionArea", "NetCrossSectionArea");
            ifcPSE.PropertyName = "NetCrossSectionArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Cross Section Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetCrossSectionAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetDuctFittingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_DuctFittingBaseQuantities.OuterSurfaceArea", "OuterSurfaceArea");
            ifcPSE.PropertyName = "OuterSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Outer Surface Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OuterSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetDuctFittingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_DuctFittingBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetDuctFittingBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetDuctFittingBaseQuantities);
         }
      }


      private static void InitQto_DuctSegmentBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetDuctSegmentBaseQuantities = new QuantityDescription();
         qtoSetDuctSegmentBaseQuantities.Name = "Qto_DuctSegmentBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_DuctSegmentBaseQuantities"))
         {
            qtoSetDuctSegmentBaseQuantities.EntityTypes.Add(IFCEntityType.IfcDuctSegment);
            qtoSetDuctSegmentBaseQuantities.ObjectType = "IfcDuctSegment";
            ifcPSE = new QuantityEntry("Qto_DuctSegmentBaseQuantities.Length", "Length");
            ifcPSE.PropertyName = "Length";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Length");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetDuctSegmentBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_DuctSegmentBaseQuantities.GrossCrossSectionArea", "GrossCrossSectionArea");
            ifcPSE.PropertyName = "GrossCrossSectionArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Cross Section Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossCrossSectionAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetDuctSegmentBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_DuctSegmentBaseQuantities.NetCrossSectionArea", "NetCrossSectionArea");
            ifcPSE.PropertyName = "NetCrossSectionArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Cross Section Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetCrossSectionAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetDuctSegmentBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_DuctSegmentBaseQuantities.OuterSurfaceArea", "OuterSurfaceArea");
            ifcPSE.PropertyName = "OuterSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Outer Surface Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OuterSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetDuctSegmentBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_DuctSegmentBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetDuctSegmentBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetDuctSegmentBaseQuantities);
         }
      }


      private static void InitQto_DuctSilencerBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetDuctSilencerBaseQuantities = new QuantityDescription();
         qtoSetDuctSilencerBaseQuantities.Name = "Qto_DuctSilencerBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_DuctSilencerBaseQuantities"))
         {
            qtoSetDuctSilencerBaseQuantities.EntityTypes.Add(IFCEntityType.IfcDuctSilencer);
            qtoSetDuctSilencerBaseQuantities.ObjectType = "IfcDuctSilencer";
            ifcPSE = new QuantityEntry("Qto_DuctSilencerBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetDuctSilencerBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetDuctSilencerBaseQuantities);
         }
      }


      private static void InitQto_ElectricApplianceBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetElectricApplianceBaseQuantities = new QuantityDescription();
         qtoSetElectricApplianceBaseQuantities.Name = "Qto_ElectricApplianceBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_ElectricApplianceBaseQuantities"))
         {
            qtoSetElectricApplianceBaseQuantities.EntityTypes.Add(IFCEntityType.IfcElectricAppliance);
            qtoSetElectricApplianceBaseQuantities.ObjectType = "IfcElectricAppliance";
            ifcPSE = new QuantityEntry("Qto_ElectricApplianceBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetElectricApplianceBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetElectricApplianceBaseQuantities);
         }
      }


      private static void InitQto_ElectricDistributionBoardBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetElectricDistributionBoardBaseQuantities = new QuantityDescription();
         qtoSetElectricDistributionBoardBaseQuantities.Name = "Qto_ElectricDistributionBoardBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_ElectricDistributionBoardBaseQuantities"))
         {
            qtoSetElectricDistributionBoardBaseQuantities.EntityTypes.Add(IFCEntityType.IfcElectricDistributionBoard);
            qtoSetElectricDistributionBoardBaseQuantities.ObjectType = "IfcElectricDistributionBoard";
            ifcPSE = new QuantityEntry("Qto_ElectricDistributionBoardBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetElectricDistributionBoardBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_ElectricDistributionBoardBaseQuantities.NumberOfCircuits", "NumberOfCircuits");
            ifcPSE.PropertyName = "NumberOfCircuits";
            ifcPSE.QuantityType = QuantityType.Count;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Number Of Circuits");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NumberOfCircuitsCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetElectricDistributionBoardBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetElectricDistributionBoardBaseQuantities);
         }
      }


      private static void InitQto_ElectricFlowStorageDeviceBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetElectricFlowStorageDeviceBaseQuantities = new QuantityDescription();
         qtoSetElectricFlowStorageDeviceBaseQuantities.Name = "Qto_ElectricFlowStorageDeviceBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_ElectricFlowStorageDeviceBaseQuantities"))
         {
            qtoSetElectricFlowStorageDeviceBaseQuantities.EntityTypes.Add(IFCEntityType.IfcElectricFlowStorageDevice);
            qtoSetElectricFlowStorageDeviceBaseQuantities.ObjectType = "IfcElectricFlowStorageDevice";
            ifcPSE = new QuantityEntry("Qto_ElectricFlowStorageDeviceBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetElectricFlowStorageDeviceBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetElectricFlowStorageDeviceBaseQuantities);
         }
      }


      private static void InitQto_ElectricGeneratorBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetElectricGeneratorBaseQuantities = new QuantityDescription();
         qtoSetElectricGeneratorBaseQuantities.Name = "Qto_ElectricGeneratorBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_ElectricGeneratorBaseQuantities"))
         {
            qtoSetElectricGeneratorBaseQuantities.EntityTypes.Add(IFCEntityType.IfcElectricGenerator);
            qtoSetElectricGeneratorBaseQuantities.ObjectType = "IfcElectricGenerator";
            ifcPSE = new QuantityEntry("Qto_ElectricGeneratorBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetElectricGeneratorBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetElectricGeneratorBaseQuantities);
         }
      }


      private static void InitQto_ElectricMotorBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetElectricMotorBaseQuantities = new QuantityDescription();
         qtoSetElectricMotorBaseQuantities.Name = "Qto_ElectricMotorBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_ElectricMotorBaseQuantities"))
         {
            qtoSetElectricMotorBaseQuantities.EntityTypes.Add(IFCEntityType.IfcElectricMotor);
            qtoSetElectricMotorBaseQuantities.ObjectType = "IfcElectricMotor";
            ifcPSE = new QuantityEntry("Qto_ElectricMotorBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetElectricMotorBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetElectricMotorBaseQuantities);
         }
      }


      private static void InitQto_ElectricTimeControlBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetElectricTimeControlBaseQuantities = new QuantityDescription();
         qtoSetElectricTimeControlBaseQuantities.Name = "Qto_ElectricTimeControlBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_ElectricTimeControlBaseQuantities"))
         {
            qtoSetElectricTimeControlBaseQuantities.EntityTypes.Add(IFCEntityType.IfcElectricTimeControl);
            qtoSetElectricTimeControlBaseQuantities.ObjectType = "IfcElectricTimeControl";
            ifcPSE = new QuantityEntry("Qto_ElectricTimeControlBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetElectricTimeControlBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetElectricTimeControlBaseQuantities);
         }
      }


      private static void InitQto_EvaporativeCoolerBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetEvaporativeCoolerBaseQuantities = new QuantityDescription();
         qtoSetEvaporativeCoolerBaseQuantities.Name = "Qto_EvaporativeCoolerBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_EvaporativeCoolerBaseQuantities"))
         {
            qtoSetEvaporativeCoolerBaseQuantities.EntityTypes.Add(IFCEntityType.IfcEvaporativeCooler);
            qtoSetEvaporativeCoolerBaseQuantities.ObjectType = "IfcEvaporativeCooler";
            ifcPSE = new QuantityEntry("Qto_EvaporativeCoolerBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetEvaporativeCoolerBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetEvaporativeCoolerBaseQuantities);
         }
      }


      private static void InitQto_EvaporatorBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetEvaporatorBaseQuantities = new QuantityDescription();
         qtoSetEvaporatorBaseQuantities.Name = "Qto_EvaporatorBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_EvaporatorBaseQuantities"))
         {
            qtoSetEvaporatorBaseQuantities.EntityTypes.Add(IFCEntityType.IfcEvaporator);
            qtoSetEvaporatorBaseQuantities.ObjectType = "IfcEvaporator";
            ifcPSE = new QuantityEntry("Qto_EvaporatorBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetEvaporatorBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetEvaporatorBaseQuantities);
         }
      }


      private static void InitQto_FanBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetFanBaseQuantities = new QuantityDescription();
         qtoSetFanBaseQuantities.Name = "Qto_FanBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_FanBaseQuantities"))
         {
            qtoSetFanBaseQuantities.EntityTypes.Add(IFCEntityType.IfcFan);
            qtoSetFanBaseQuantities.ObjectType = "IfcFan";
            ifcPSE = new QuantityEntry("Qto_FanBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetFanBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetFanBaseQuantities);
         }
      }


      private static void InitQto_FilterBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetFilterBaseQuantities = new QuantityDescription();
         qtoSetFilterBaseQuantities.Name = "Qto_FilterBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_FilterBaseQuantities"))
         {
            qtoSetFilterBaseQuantities.EntityTypes.Add(IFCEntityType.IfcFilter);
            qtoSetFilterBaseQuantities.ObjectType = "IfcFilter";
            ifcPSE = new QuantityEntry("Qto_FilterBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetFilterBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetFilterBaseQuantities);
         }
      }


      private static void InitQto_FireSuppressionTerminalBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetFireSuppressionTerminalBaseQuantities = new QuantityDescription();
         qtoSetFireSuppressionTerminalBaseQuantities.Name = "Qto_FireSuppressionTerminalBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_FireSuppressionTerminalBaseQuantities"))
         {
            qtoSetFireSuppressionTerminalBaseQuantities.EntityTypes.Add(IFCEntityType.IfcFireSuppressionTerminal);
            qtoSetFireSuppressionTerminalBaseQuantities.ObjectType = "IfcFireSuppressionTerminal";
            ifcPSE = new QuantityEntry("Qto_FireSuppressionTerminalBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetFireSuppressionTerminalBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetFireSuppressionTerminalBaseQuantities);
         }
      }


      private static void InitQto_FlowInstrumentBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetFlowInstrumentBaseQuantities = new QuantityDescription();
         qtoSetFlowInstrumentBaseQuantities.Name = "Qto_FlowInstrumentBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_FlowInstrumentBaseQuantities"))
         {
            qtoSetFlowInstrumentBaseQuantities.EntityTypes.Add(IFCEntityType.IfcFlowInstrument);
            qtoSetFlowInstrumentBaseQuantities.ObjectType = "IfcFlowInstrument";
            ifcPSE = new QuantityEntry("Qto_FlowInstrumentBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetFlowInstrumentBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetFlowInstrumentBaseQuantities);
         }
      }


      private static void InitQto_FlowMeterBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetFlowMeterBaseQuantities = new QuantityDescription();
         qtoSetFlowMeterBaseQuantities.Name = "Qto_FlowMeterBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_FlowMeterBaseQuantities"))
         {
            qtoSetFlowMeterBaseQuantities.EntityTypes.Add(IFCEntityType.IfcFlowMeter);
            qtoSetFlowMeterBaseQuantities.ObjectType = "IfcFlowMeter";
            ifcPSE = new QuantityEntry("Qto_FlowMeterBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetFlowMeterBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetFlowMeterBaseQuantities);
         }
      }


      private static void InitQto_FootingBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetFootingBaseQuantities = new QuantityDescription();
         qtoSetFootingBaseQuantities.Name = "Qto_FootingBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_FootingBaseQuantities"))
         {
            qtoSetFootingBaseQuantities.EntityTypes.Add(IFCEntityType.IfcFooting);
            qtoSetFootingBaseQuantities.ObjectType = "IfcFooting";
            ifcPSE = new QuantityEntry("Qto_FootingBaseQuantities.Length", "Length");
            ifcPSE.PropertyName = "Length";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Länge");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Length");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetFootingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_FootingBaseQuantities.Width", "Width");
            ifcPSE.PropertyName = "Width";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Dicke");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Width");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.WidthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetFootingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_FootingBaseQuantities.Height", "Height");
            ifcPSE.PropertyName = "Height";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Höhe");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Height");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.HeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetFootingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_FootingBaseQuantities.CrossSectionArea", "CrossSectionArea");
            ifcPSE.PropertyName = "CrossSectionArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Cross Section Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CrossSectionAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetFootingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_FootingBaseQuantities.OuterSurfaceArea", "OuterSurfaceArea");
            ifcPSE.PropertyName = "OuterSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Outer Surface Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OuterSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetFootingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_FootingBaseQuantities.GrossSurfaceArea", "GrossSurfaceArea");
            ifcPSE.PropertyName = "GrossSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Surface Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetFootingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_FootingBaseQuantities.GrossVolume", "GrossVolume");
            ifcPSE.PropertyName = "GrossVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Volume");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetFootingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_FootingBaseQuantities.NetVolume", "NetVolume");
            ifcPSE.PropertyName = "NetVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Volume");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetFootingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_FootingBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetFootingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_FootingBaseQuantities.NetWeight", "NetWeight");
            ifcPSE.PropertyName = "NetWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetFootingBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetFootingBaseQuantities);
         }
      }


      private static void InitQto_HeatExchangerBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetHeatExchangerBaseQuantities = new QuantityDescription();
         qtoSetHeatExchangerBaseQuantities.Name = "Qto_HeatExchangerBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_HeatExchangerBaseQuantities"))
         {
            qtoSetHeatExchangerBaseQuantities.EntityTypes.Add(IFCEntityType.IfcHeatExchanger);
            qtoSetHeatExchangerBaseQuantities.ObjectType = "IfcHeatExchanger";
            ifcPSE = new QuantityEntry("Qto_HeatExchangerBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetHeatExchangerBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetHeatExchangerBaseQuantities);
         }
      }


      private static void InitQto_HumidifierBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetHumidifierBaseQuantities = new QuantityDescription();
         qtoSetHumidifierBaseQuantities.Name = "Qto_HumidifierBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_HumidifierBaseQuantities"))
         {
            qtoSetHumidifierBaseQuantities.EntityTypes.Add(IFCEntityType.IfcHumidifier);
            qtoSetHumidifierBaseQuantities.ObjectType = "IfcHumidifier";
            ifcPSE = new QuantityEntry("Qto_HumidifierBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetHumidifierBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetHumidifierBaseQuantities);
         }
      }


      private static void InitQto_InterceptorBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetInterceptorBaseQuantities = new QuantityDescription();
         qtoSetInterceptorBaseQuantities.Name = "Qto_InterceptorBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_InterceptorBaseQuantities"))
         {
            qtoSetInterceptorBaseQuantities.EntityTypes.Add(IFCEntityType.IfcInterceptor);
            qtoSetInterceptorBaseQuantities.ObjectType = "IfcInterceptor";
            ifcPSE = new QuantityEntry("Qto_InterceptorBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetInterceptorBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetInterceptorBaseQuantities);
         }
      }


      private static void InitQto_JunctionBoxBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetJunctionBoxBaseQuantities = new QuantityDescription();
         qtoSetJunctionBoxBaseQuantities.Name = "Qto_JunctionBoxBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_JunctionBoxBaseQuantities"))
         {
            qtoSetJunctionBoxBaseQuantities.EntityTypes.Add(IFCEntityType.IfcJunctionBox);
            qtoSetJunctionBoxBaseQuantities.ObjectType = "IfcJunctionBox";
            ifcPSE = new QuantityEntry("Qto_JunctionBoxBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetJunctionBoxBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_JunctionBoxBaseQuantities.NumberOfGangs", "NumberOfGangs");
            ifcPSE.PropertyName = "NumberOfGangs";
            ifcPSE.QuantityType = QuantityType.Count;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Number Of Gangs");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NumberOfGangsCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetJunctionBoxBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetJunctionBoxBaseQuantities);
         }
      }


      private static void InitQto_LaborResourceBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetLaborResourceBaseQuantities = new QuantityDescription();
         qtoSetLaborResourceBaseQuantities.Name = "Qto_LaborResourceBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_LaborResourceBaseQuantities"))
         {
            qtoSetLaborResourceBaseQuantities.EntityTypes.Add(IFCEntityType.IfcLaborResource);
            qtoSetLaborResourceBaseQuantities.ObjectType = "IfcLaborResource";
            ifcPSE = new QuantityEntry("Qto_LaborResourceBaseQuantities.StandardWork", "StandardWork");
            ifcPSE.PropertyName = "StandardWork";
            ifcPSE.QuantityType = QuantityType.Time;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Standard Work");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.StandardWorkCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetLaborResourceBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_LaborResourceBaseQuantities.OvertimeWork", "OvertimeWork");
            ifcPSE.PropertyName = "OvertimeWork";
            ifcPSE.QuantityType = QuantityType.Time;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Overtime Work");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OvertimeWorkCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetLaborResourceBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetLaborResourceBaseQuantities);
         }
      }


      private static void InitQto_LampBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetLampBaseQuantities = new QuantityDescription();
         qtoSetLampBaseQuantities.Name = "Qto_LampBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_LampBaseQuantities"))
         {
            qtoSetLampBaseQuantities.EntityTypes.Add(IFCEntityType.IfcLamp);
            qtoSetLampBaseQuantities.ObjectType = "IfcLamp";
            ifcPSE = new QuantityEntry("Qto_LampBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetLampBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetLampBaseQuantities);
         }
      }


      private static void InitQto_LightFixtureBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetLightFixtureBaseQuantities = new QuantityDescription();
         qtoSetLightFixtureBaseQuantities.Name = "Qto_LightFixtureBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_LightFixtureBaseQuantities"))
         {
            qtoSetLightFixtureBaseQuantities.EntityTypes.Add(IFCEntityType.IfcLightFixture);
            qtoSetLightFixtureBaseQuantities.ObjectType = "IfcLightFixture";
            ifcPSE = new QuantityEntry("Qto_LightFixtureBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetLightFixtureBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetLightFixtureBaseQuantities);
         }
      }


      private static void InitQto_MemberBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetMemberBaseQuantities = new QuantityDescription();
         qtoSetMemberBaseQuantities.Name = "Qto_MemberBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_MemberBaseQuantities"))
         {
            qtoSetMemberBaseQuantities.EntityTypes.Add(IFCEntityType.IfcMember);
            qtoSetMemberBaseQuantities.ObjectType = "IfcMember";
            ifcPSE = new QuantityEntry("Qto_MemberBaseQuantities.Length", "Length");
            ifcPSE.PropertyName = "Length";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Länge");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Length");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetMemberBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_MemberBaseQuantities.CrossSectionArea", "CrossSectionArea");
            ifcPSE.PropertyName = "CrossSectionArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Querschnittsfläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Cross Section Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CrossSectionAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetMemberBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_MemberBaseQuantities.OuterSurfaceArea", "OuterSurfaceArea");
            ifcPSE.PropertyName = "OuterSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Mantelfläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Outer Surface Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OuterSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetMemberBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_MemberBaseQuantities.GrossSurfaceArea", "GrossSurfaceArea");
            ifcPSE.PropertyName = "GrossSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Gesamtoberfläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Surface Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetMemberBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_MemberBaseQuantities.NetSurfaceArea", "NetSurfaceArea");
            ifcPSE.PropertyName = "NetSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettooberfläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Surface Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetMemberBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_MemberBaseQuantities.GrossVolume", "GrossVolume");
            ifcPSE.PropertyName = "GrossVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttovolumen");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Volume");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetMemberBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_MemberBaseQuantities.NetVolume", "NetVolume");
            ifcPSE.PropertyName = "NetVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettovolumen");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Volume");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetMemberBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_MemberBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttogewicht");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetMemberBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_MemberBaseQuantities.NetWeight", "NetWeight");
            ifcPSE.PropertyName = "NetWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettogewicht");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetMemberBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetMemberBaseQuantities);
         }
      }


      private static void InitQto_MotorConnectionBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetMotorConnectionBaseQuantities = new QuantityDescription();
         qtoSetMotorConnectionBaseQuantities.Name = "Qto_MotorConnectionBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_MotorConnectionBaseQuantities"))
         {
            qtoSetMotorConnectionBaseQuantities.EntityTypes.Add(IFCEntityType.IfcMotorConnection);
            qtoSetMotorConnectionBaseQuantities.ObjectType = "IfcMotorConnection";
            ifcPSE = new QuantityEntry("Qto_MotorConnectionBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetMotorConnectionBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetMotorConnectionBaseQuantities);
         }
      }


      private static void InitQto_OpeningElementBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetOpeningElementBaseQuantities = new QuantityDescription();
         qtoSetOpeningElementBaseQuantities.Name = "Qto_OpeningElementBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_OpeningElementBaseQuantities"))
         {
            qtoSetOpeningElementBaseQuantities.EntityTypes.Add(IFCEntityType.IfcOpeningElement);
            qtoSetOpeningElementBaseQuantities.ObjectType = "IfcOpeningElement";
            ifcPSE = new QuantityEntry("Qto_OpeningElementBaseQuantities.Width", "Width");
            ifcPSE.PropertyName = "Width";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Breite");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Width");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "開口幅");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.WidthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetOpeningElementBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_OpeningElementBaseQuantities.Height", "Height");
            ifcPSE.PropertyName = "Height";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Höhe");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Height");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "開口高さ");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.HeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetOpeningElementBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_OpeningElementBaseQuantities.Depth", "Depth");
            ifcPSE.PropertyName = "Depth";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Tiefe");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Depth");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "開口奥行き");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.DepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetOpeningElementBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_OpeningElementBaseQuantities.Area", "Area");
            ifcPSE.PropertyName = "Area";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Fläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.AreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetOpeningElementBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_OpeningElementBaseQuantities.Volume", "Volume");
            ifcPSE.PropertyName = "Volume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Volumen");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Volume");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "体積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.VolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetOpeningElementBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetOpeningElementBaseQuantities);
         }
      }


      private static void InitQto_OutletBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetOutletBaseQuantities = new QuantityDescription();
         qtoSetOutletBaseQuantities.Name = "Qto_OutletBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_OutletBaseQuantities"))
         {
            qtoSetOutletBaseQuantities.EntityTypes.Add(IFCEntityType.IfcOutlet);
            qtoSetOutletBaseQuantities.ObjectType = "IfcOutlet";
            ifcPSE = new QuantityEntry("Qto_OutletBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetOutletBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetOutletBaseQuantities);
         }
      }


      private static void InitQto_PileBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetPileBaseQuantities = new QuantityDescription();
         qtoSetPileBaseQuantities.Name = "Qto_PileBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_PileBaseQuantities"))
         {
            qtoSetPileBaseQuantities.EntityTypes.Add(IFCEntityType.IfcPile);
            qtoSetPileBaseQuantities.ObjectType = "IfcPile";
            ifcPSE = new QuantityEntry("Qto_PileBaseQuantities.Length", "Length");
            ifcPSE.PropertyName = "Length";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Length");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPileBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PileBaseQuantities.CrossSectionArea", "CrossSectionArea");
            ifcPSE.PropertyName = "CrossSectionArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Cross Section Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CrossSectionAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPileBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PileBaseQuantities.OuterSurfaceArea", "OuterSurfaceArea");
            ifcPSE.PropertyName = "OuterSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Outer Surface Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OuterSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPileBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PileBaseQuantities.GrossSurfaceArea", "GrossSurfaceArea");
            ifcPSE.PropertyName = "GrossSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Surface Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPileBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PileBaseQuantities.GrossVolume", "GrossVolume");
            ifcPSE.PropertyName = "GrossVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Volume");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPileBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PileBaseQuantities.NetVolume", "NetVolume");
            ifcPSE.PropertyName = "NetVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Volume");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPileBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PileBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPileBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PileBaseQuantities.NetWeight", "NetWeight");
            ifcPSE.PropertyName = "NetWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPileBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetPileBaseQuantities);
         }
      }


      private static void InitQto_PipeFittingBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetPipeFittingBaseQuantities = new QuantityDescription();
         qtoSetPipeFittingBaseQuantities.Name = "Qto_PipeFittingBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_PipeFittingBaseQuantities"))
         {
            qtoSetPipeFittingBaseQuantities.EntityTypes.Add(IFCEntityType.IfcPipeFitting);
            qtoSetPipeFittingBaseQuantities.ObjectType = "IfcPipeFitting";
            ifcPSE = new QuantityEntry("Qto_PipeFittingBaseQuantities.Length", "Length");
            ifcPSE.PropertyName = "Length";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Length");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPipeFittingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PipeFittingBaseQuantities.GrossCrossSectionArea", "GrossCrossSectionArea");
            ifcPSE.PropertyName = "GrossCrossSectionArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Cross Section Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossCrossSectionAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPipeFittingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PipeFittingBaseQuantities.NetCrossSectionArea", "NetCrossSectionArea");
            ifcPSE.PropertyName = "NetCrossSectionArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Cross Section Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetCrossSectionAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPipeFittingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PipeFittingBaseQuantities.OuterSurfaceArea", "OuterSurfaceArea");
            ifcPSE.PropertyName = "OuterSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Outer Surface Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OuterSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPipeFittingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PipeFittingBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPipeFittingBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PipeFittingBaseQuantities.NetWeight", "NetWeight");
            ifcPSE.PropertyName = "NetWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPipeFittingBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetPipeFittingBaseQuantities);
         }
      }


      private static void InitQto_PipeSegmentBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetPipeSegmentBaseQuantities = new QuantityDescription();
         qtoSetPipeSegmentBaseQuantities.Name = "Qto_PipeSegmentBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_PipeSegmentBaseQuantities"))
         {
            qtoSetPipeSegmentBaseQuantities.EntityTypes.Add(IFCEntityType.IfcPipeSegment);
            qtoSetPipeSegmentBaseQuantities.ObjectType = "IfcPipeSegment";
            ifcPSE = new QuantityEntry("Qto_PipeSegmentBaseQuantities.Length", "Length");
            ifcPSE.PropertyName = "Length";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Length");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPipeSegmentBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PipeSegmentBaseQuantities.GrossCrossSectionArea", "GrossCrossSectionArea");
            ifcPSE.PropertyName = "GrossCrossSectionArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Cross Section Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossCrossSectionAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPipeSegmentBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PipeSegmentBaseQuantities.NetCrossSectionArea", "NetCrossSectionArea");
            ifcPSE.PropertyName = "NetCrossSectionArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Cross Section Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetCrossSectionAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPipeSegmentBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PipeSegmentBaseQuantities.OuterSurfaceArea", "OuterSurfaceArea");
            ifcPSE.PropertyName = "OuterSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Outer Surface Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.OuterSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPipeSegmentBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PipeSegmentBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPipeSegmentBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PipeSegmentBaseQuantities.NetWeight", "NetWeight");
            ifcPSE.PropertyName = "NetWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPipeSegmentBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetPipeSegmentBaseQuantities);
         }
      }


      private static void InitQto_PlateBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetPlateBaseQuantities = new QuantityDescription();
         qtoSetPlateBaseQuantities.Name = "Qto_PlateBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_PlateBaseQuantities"))
         {
            qtoSetPlateBaseQuantities.EntityTypes.Add(IFCEntityType.IfcPlate);
            qtoSetPlateBaseQuantities.ObjectType = "IfcPlate";
            ifcPSE = new QuantityEntry("Qto_PlateBaseQuantities.Width", "Width");
            ifcPSE.PropertyName = "Width";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Dicke");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Width");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.WidthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPlateBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PlateBaseQuantities.Perimeter", "Perimeter");
            ifcPSE.PropertyName = "Perimeter";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Umfang");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Perimeter");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PerimeterCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPlateBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PlateBaseQuantities.GrossArea", "GrossArea");
            ifcPSE.PropertyName = "GrossArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttofläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPlateBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PlateBaseQuantities.NetArea", "NetArea");
            ifcPSE.PropertyName = "NetArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettofläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPlateBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PlateBaseQuantities.GrossVolume", "GrossVolume");
            ifcPSE.PropertyName = "GrossVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttovolumen");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Volume");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPlateBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PlateBaseQuantities.NetVolume", "NetVolume");
            ifcPSE.PropertyName = "NetVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettovolumen");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Volume");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPlateBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PlateBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttogewicht");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPlateBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_PlateBaseQuantities.NetWeight", "NetWeight");
            ifcPSE.PropertyName = "NetWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettogewicht");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPlateBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetPlateBaseQuantities);
         }
      }


      private static void InitQto_ProjectionElementBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetProjectionElementBaseQuantities = new QuantityDescription();
         qtoSetProjectionElementBaseQuantities.Name = "Qto_ProjectionElementBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_ProjectionElementBaseQuantities"))
         {
            qtoSetProjectionElementBaseQuantities.EntityTypes.Add(IFCEntityType.IfcProjectionElement);
            qtoSetProjectionElementBaseQuantities.ObjectType = "IfcProjectionElement";
            ifcPSE = new QuantityEntry("Qto_ProjectionElementBaseQuantities.Area", "Area");
            ifcPSE.PropertyName = "Area";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Fläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.AreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetProjectionElementBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_ProjectionElementBaseQuantities.Volume", "Volume");
            ifcPSE.PropertyName = "Volume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Volumen");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Volume");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.VolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetProjectionElementBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetProjectionElementBaseQuantities);
         }
      }


      private static void InitQto_ProtectiveDeviceBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetProtectiveDeviceBaseQuantities = new QuantityDescription();
         qtoSetProtectiveDeviceBaseQuantities.Name = "Qto_ProtectiveDeviceBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_ProtectiveDeviceBaseQuantities"))
         {
            qtoSetProtectiveDeviceBaseQuantities.EntityTypes.Add(IFCEntityType.IfcProtectiveDevice);
            qtoSetProtectiveDeviceBaseQuantities.ObjectType = "IfcProtectiveDevice";
            ifcPSE = new QuantityEntry("Qto_ProtectiveDeviceBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetProtectiveDeviceBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetProtectiveDeviceBaseQuantities);
         }
      }


      private static void InitQto_ProtectiveDeviceTrippingUnitBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetProtectiveDeviceTrippingUnitBaseQuantities = new QuantityDescription();
         qtoSetProtectiveDeviceTrippingUnitBaseQuantities.Name = "Qto_ProtectiveDeviceTrippingUnitBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_ProtectiveDeviceTrippingUnitBaseQuantities"))
         {
            qtoSetProtectiveDeviceTrippingUnitBaseQuantities.EntityTypes.Add(IFCEntityType.IfcProtectiveDeviceTrippingUnit);
            qtoSetProtectiveDeviceTrippingUnitBaseQuantities.ObjectType = "IfcProtectiveDeviceTrippingUnit";
            ifcPSE = new QuantityEntry("Qto_ProtectiveDeviceTrippingUnitBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetProtectiveDeviceTrippingUnitBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetProtectiveDeviceTrippingUnitBaseQuantities);
         }
      }


      private static void InitQto_PumpBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetPumpBaseQuantities = new QuantityDescription();
         qtoSetPumpBaseQuantities.Name = "Qto_PumpBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_PumpBaseQuantities"))
         {
            qtoSetPumpBaseQuantities.EntityTypes.Add(IFCEntityType.IfcPump);
            qtoSetPumpBaseQuantities.ObjectType = "IfcPump";
            ifcPSE = new QuantityEntry("Qto_PumpBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetPumpBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetPumpBaseQuantities);
         }
      }


      private static void InitQto_RailingBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetRailingBaseQuantities = new QuantityDescription();
         qtoSetRailingBaseQuantities.Name = "Qto_RailingBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_RailingBaseQuantities"))
         {
            qtoSetRailingBaseQuantities.EntityTypes.Add(IFCEntityType.IfcRailing);
            qtoSetRailingBaseQuantities.ObjectType = "IfcRailing";
            ifcPSE = new QuantityEntry("Qto_RailingBaseQuantities.Length", "Length");
            ifcPSE.PropertyName = "Length";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Länge");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Length");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "長さ");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetRailingBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetRailingBaseQuantities);
         }
      }


      private static void InitQto_RampFlightBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetRampFlightBaseQuantities = new QuantityDescription();
         qtoSetRampFlightBaseQuantities.Name = "Qto_RampFlightBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_RampFlightBaseQuantities"))
         {
            qtoSetRampFlightBaseQuantities.EntityTypes.Add(IFCEntityType.IfcRampFlight);
            qtoSetRampFlightBaseQuantities.ObjectType = "IfcRampFlight";
            ifcPSE = new QuantityEntry("Qto_RampFlightBaseQuantities.Length", "Length");
            ifcPSE.PropertyName = "Length";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Länge");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Length");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetRampFlightBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_RampFlightBaseQuantities.Width", "Width");
            ifcPSE.PropertyName = "Width";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Dicke");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Width");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.WidthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetRampFlightBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_RampFlightBaseQuantities.GrossArea", "GrossArea");
            ifcPSE.PropertyName = "GrossArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttofläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetRampFlightBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_RampFlightBaseQuantities.NetArea", "NetArea");
            ifcPSE.PropertyName = "NetArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettofläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetRampFlightBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_RampFlightBaseQuantities.GrossVolume", "GrossVolume");
            ifcPSE.PropertyName = "GrossVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttovolumen");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Volume");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetRampFlightBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_RampFlightBaseQuantities.NetVolume", "NetVolume");
            ifcPSE.PropertyName = "NetVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettovolumen");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Volume");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetRampFlightBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetRampFlightBaseQuantities);
         }
      }


      private static void InitQto_ReinforcingElementBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetReinforcingElementBaseQuantities = new QuantityDescription();
         qtoSetReinforcingElementBaseQuantities.Name = "Qto_ReinforcingElementBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_ReinforcingElementBaseQuantities"))
         {
            qtoSetReinforcingElementBaseQuantities.EntityTypes.Add(IFCEntityType.IfcReinforcingElement);
            qtoSetReinforcingElementBaseQuantities.ObjectType = "IfcReinforcingElement";
            ifcPSE = new QuantityEntry("Qto_ReinforcingElementBaseQuantities.Count", "Count");
            ifcPSE.PropertyName = "Count";
            ifcPSE.QuantityType = QuantityType.Count;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.CountCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetReinforcingElementBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_ReinforcingElementBaseQuantities.Length", "Length");
            ifcPSE.PropertyName = "Length";
            ifcPSE.QuantityType = QuantityType.Length;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetReinforcingElementBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_ReinforcingElementBaseQuantities.Weight", "Weight");
            ifcPSE.PropertyName = "Weight";
            ifcPSE.QuantityType = QuantityType.Mass;
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.WeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetReinforcingElementBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetReinforcingElementBaseQuantities);
         }
      }


      private static void InitQto_RoofBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetRoofBaseQuantities = new QuantityDescription();
         qtoSetRoofBaseQuantities.Name = "Qto_RoofBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_RoofBaseQuantities"))
         {
            qtoSetRoofBaseQuantities.EntityTypes.Add(IFCEntityType.IfcRoof);
            qtoSetRoofBaseQuantities.ObjectType = "IfcRoof";
            ifcPSE = new QuantityEntry("Qto_RoofBaseQuantities.GrossArea", "GrossArea");
            ifcPSE.PropertyName = "GrossArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttofläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetRoofBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_RoofBaseQuantities.NetArea", "NetArea");
            ifcPSE.PropertyName = "NetArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettofläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetRoofBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_RoofBaseQuantities.ProjectedArea", "ProjectedArea");
            ifcPSE.PropertyName = "ProjectedArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "projizierte Fläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Projected Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.ProjectedAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetRoofBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetRoofBaseQuantities);
         }
      }


      private static void InitQto_SanitaryTerminalBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetSanitaryTerminalBaseQuantities = new QuantityDescription();
         qtoSetSanitaryTerminalBaseQuantities.Name = "Qto_SanitaryTerminalBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_SanitaryTerminalBaseQuantities"))
         {
            qtoSetSanitaryTerminalBaseQuantities.EntityTypes.Add(IFCEntityType.IfcSanitaryTerminal);
            qtoSetSanitaryTerminalBaseQuantities.ObjectType = "IfcSanitaryTerminal";
            ifcPSE = new QuantityEntry("Qto_SanitaryTerminalBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSanitaryTerminalBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetSanitaryTerminalBaseQuantities);
         }
      }


      private static void InitQto_SensorBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetSensorBaseQuantities = new QuantityDescription();
         qtoSetSensorBaseQuantities.Name = "Qto_SensorBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_SensorBaseQuantities"))
         {
            qtoSetSensorBaseQuantities.EntityTypes.Add(IFCEntityType.IfcSensor);
            qtoSetSensorBaseQuantities.ObjectType = "IfcSensor";
            ifcPSE = new QuantityEntry("Qto_SensorBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSensorBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetSensorBaseQuantities);
         }
      }


      private static void InitQto_SiteBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetSiteBaseQuantities = new QuantityDescription();
         qtoSetSiteBaseQuantities.Name = "Qto_SiteBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_SiteBaseQuantities"))
         {
            qtoSetSiteBaseQuantities.EntityTypes.Add(IFCEntityType.IfcSite);
            qtoSetSiteBaseQuantities.ObjectType = "IfcSite";
            ifcPSE = new QuantityEntry("Qto_SiteBaseQuantities.GrossPerimeter", "GrossPerimeter");
            ifcPSE.PropertyName = "GrossPerimeter";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Umfang");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Perimeter");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "周辺長");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossPerimeterCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSiteBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SiteBaseQuantities.GrossArea", "GrossArea");
            ifcPSE.PropertyName = "GrossArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Grundfläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "敷地面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSiteBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetSiteBaseQuantities);
         }
      }


      private static void InitQto_SlabBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetSlabBaseQuantities = new QuantityDescription();
         qtoSetSlabBaseQuantities.Name = "Qto_SlabBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_SlabBaseQuantities"))
         {
            qtoSetSlabBaseQuantities.EntityTypes.Add(IFCEntityType.IfcSlab);
            qtoSetSlabBaseQuantities.ObjectType = "IfcSlab";
            ifcPSE = new QuantityEntry("Qto_SlabBaseQuantities.Width", "Width");
            ifcPSE.PropertyName = "Width";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Dicke");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Width");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "幅");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.WidthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSlabBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SlabBaseQuantities.Length", "Length");
            ifcPSE.PropertyName = "Length";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Länge");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Length");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSlabBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SlabBaseQuantities.Depth", "Depth");
            ifcPSE.PropertyName = "Depth";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Breite");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Depth");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.DepthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSlabBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SlabBaseQuantities.Perimeter", "Perimeter");
            ifcPSE.PropertyName = "Perimeter";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Umfang");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Perimeter");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "周囲長");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PerimeterCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSlabBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SlabBaseQuantities.GrossArea", "GrossArea");
            ifcPSE.PropertyName = "GrossArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttofläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSlabBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SlabBaseQuantities.NetArea", "NetArea");
            ifcPSE.PropertyName = "NetArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettofläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSlabBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SlabBaseQuantities.GrossVolume", "GrossVolume");
            ifcPSE.PropertyName = "GrossVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttovolumen");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Volume");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "体積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSlabBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SlabBaseQuantities.NetVolume", "NetVolume");
            ifcPSE.PropertyName = "NetVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettovolumen");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Volume");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味体積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSlabBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SlabBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttogewicht");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "重量");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSlabBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SlabBaseQuantities.NetWeight", "NetWeight");
            ifcPSE.PropertyName = "NetWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettogewicht");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Weight");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味重量");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSlabBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetSlabBaseQuantities);
         }
      }


      private static void InitQto_SolarDeviceBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetSolarDeviceBaseQuantities = new QuantityDescription();
         qtoSetSolarDeviceBaseQuantities.Name = "Qto_SolarDeviceBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_SolarDeviceBaseQuantities"))
         {
            qtoSetSolarDeviceBaseQuantities.EntityTypes.Add(IFCEntityType.IfcSolarDevice);
            qtoSetSolarDeviceBaseQuantities.ObjectType = "IfcSolarDevice";
            ifcPSE = new QuantityEntry("Qto_SolarDeviceBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSolarDeviceBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SolarDeviceBaseQuantities.GrossArea", "GrossArea");
            ifcPSE.PropertyName = "GrossArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSolarDeviceBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetSolarDeviceBaseQuantities);
         }
      }


      private static void InitQto_SpaceBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetSpaceBaseQuantities = new QuantityDescription();
         qtoSetSpaceBaseQuantities.Name = "Qto_SpaceBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_SpaceBaseQuantities"))
         {
            qtoSetSpaceBaseQuantities.EntityTypes.Add(IFCEntityType.IfcSpace);
            qtoSetSpaceBaseQuantities.ObjectType = "IfcSpace";
            ifcPSE = new QuantityEntry("Qto_SpaceBaseQuantities.Height", "Height");
            ifcPSE.PropertyName = "Height";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Höhe");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Height");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "高さ");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.HeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSpaceBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SpaceBaseQuantities.FinishCeilingHeight", "FinishCeilingHeight");
            ifcPSE.PropertyName = "FinishCeilingHeight";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Lichte Höhe");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Finish Ceiling Height");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "天井仕上げ高さ");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FinishCeilingHeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSpaceBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SpaceBaseQuantities.FinishFloorHeight", "FinishFloorHeight");
            ifcPSE.PropertyName = "FinishFloorHeight";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Fussboden Höhe");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Finish Floor Height");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "床仕上げ高さ");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.FinishFloorHeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSpaceBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SpaceBaseQuantities.GrossPerimeter", "GrossPerimeter");
            ifcPSE.PropertyName = "GrossPerimeter";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Umfang Brutto");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Perimeter");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "周辺長");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossPerimeterCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSpaceBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SpaceBaseQuantities.NetPerimeter", "NetPerimeter");
            ifcPSE.PropertyName = "NetPerimeter";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Umfang Netto");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Perimeter");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味周辺長");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetPerimeterCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSpaceBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SpaceBaseQuantities.GrossFloorArea", "GrossFloorArea");
            ifcPSE.PropertyName = "GrossFloorArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bodenfläche Brutto");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Floor Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "床面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossFloorAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSpaceBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SpaceBaseQuantities.NetFloorArea", "NetFloorArea");
            ifcPSE.PropertyName = "NetFloorArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bodenfläche Netto");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Floor Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味床面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetFloorAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSpaceBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SpaceBaseQuantities.GrossWallArea", "GrossWallArea");
            ifcPSE.PropertyName = "GrossWallArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Senkrechte Fläche Brutto");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Wall Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "壁面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWallAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSpaceBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SpaceBaseQuantities.NetWallArea", "NetWallArea");
            ifcPSE.PropertyName = "NetWallArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Senkrechte Fläche Netto");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Wall Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味壁面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetWallAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSpaceBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SpaceBaseQuantities.GrossCeilingArea", "GrossCeilingArea");
            ifcPSE.PropertyName = "GrossCeilingArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Deckenfläche Brutto");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Ceiling Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "天井面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossCeilingAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSpaceBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SpaceBaseQuantities.NetCeilingArea", "NetCeilingArea");
            ifcPSE.PropertyName = "NetCeilingArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Deckenfläche Netto");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Ceiling Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味天井面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetCeilingAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSpaceBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SpaceBaseQuantities.GrossVolume", "GrossVolume");
            ifcPSE.PropertyName = "GrossVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttovolumen");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Volume");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "体積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSpaceBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SpaceBaseQuantities.NetVolume", "NetVolume");
            ifcPSE.PropertyName = "NetVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettovolumen");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Volume");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味体積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSpaceBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetSpaceBaseQuantities);
         }
      }


      private static void InitQto_SpaceHeaterBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetSpaceHeaterBaseQuantities = new QuantityDescription();
         qtoSetSpaceHeaterBaseQuantities.Name = "Qto_SpaceHeaterBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_SpaceHeaterBaseQuantities"))
         {
            qtoSetSpaceHeaterBaseQuantities.EntityTypes.Add(IFCEntityType.IfcSpaceHeater);
            qtoSetSpaceHeaterBaseQuantities.ObjectType = "IfcSpaceHeater";
            ifcPSE = new QuantityEntry("Qto_SpaceHeaterBaseQuantities.Length", "Length");
            ifcPSE.PropertyName = "Length";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Length");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSpaceHeaterBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SpaceHeaterBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSpaceHeaterBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_SpaceHeaterBaseQuantities.NetWeight", "NetWeight");
            ifcPSE.PropertyName = "NetWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSpaceHeaterBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetSpaceHeaterBaseQuantities);
         }
      }


      private static void InitQto_StackTerminalBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetStackTerminalBaseQuantities = new QuantityDescription();
         qtoSetStackTerminalBaseQuantities.Name = "Qto_StackTerminalBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_StackTerminalBaseQuantities"))
         {
            qtoSetStackTerminalBaseQuantities.EntityTypes.Add(IFCEntityType.IfcStackTerminal);
            qtoSetStackTerminalBaseQuantities.ObjectType = "IfcStackTerminal";
            ifcPSE = new QuantityEntry("Qto_StackTerminalBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetStackTerminalBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetStackTerminalBaseQuantities);
         }
      }


      private static void InitQto_StairFlightBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetStairFlightBaseQuantities = new QuantityDescription();
         qtoSetStairFlightBaseQuantities.Name = "Qto_StairFlightBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_StairFlightBaseQuantities"))
         {
            qtoSetStairFlightBaseQuantities.EntityTypes.Add(IFCEntityType.IfcStairFlight);
            qtoSetStairFlightBaseQuantities.ObjectType = "IfcStairFlight";
            ifcPSE = new QuantityEntry("Qto_StairFlightBaseQuantities.Length", "Length");
            ifcPSE.PropertyName = "Length";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Länge");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Length");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetStairFlightBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_StairFlightBaseQuantities.GrossVolume", "GrossVolume");
            ifcPSE.PropertyName = "GrossVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttovolumen");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Volume");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetStairFlightBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_StairFlightBaseQuantities.NetVolume", "NetVolume");
            ifcPSE.PropertyName = "NetVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettovolumen");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Volume");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetStairFlightBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetStairFlightBaseQuantities);
         }
      }


      private static void InitQto_SwitchingDeviceBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetSwitchingDeviceBaseQuantities = new QuantityDescription();
         qtoSetSwitchingDeviceBaseQuantities.Name = "Qto_SwitchingDeviceBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_SwitchingDeviceBaseQuantities"))
         {
            qtoSetSwitchingDeviceBaseQuantities.EntityTypes.Add(IFCEntityType.IfcSwitchingDevice);
            qtoSetSwitchingDeviceBaseQuantities.ObjectType = "IfcSwitchingDevice";
            ifcPSE = new QuantityEntry("Qto_SwitchingDeviceBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetSwitchingDeviceBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetSwitchingDeviceBaseQuantities);
         }
      }


      private static void InitQto_TankBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetTankBaseQuantities = new QuantityDescription();
         qtoSetTankBaseQuantities.Name = "Qto_TankBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_TankBaseQuantities"))
         {
            qtoSetTankBaseQuantities.EntityTypes.Add(IFCEntityType.IfcTank);
            qtoSetTankBaseQuantities.ObjectType = "IfcTank";
            ifcPSE = new QuantityEntry("Qto_TankBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetTankBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_TankBaseQuantities.NetWeight", "NetWeight");
            ifcPSE.PropertyName = "NetWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetTankBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_TankBaseQuantities.TotalSurfaceArea", "TotalSurfaceArea");
            ifcPSE.PropertyName = "TotalSurfaceArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Total Surface Area");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.TotalSurfaceAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetTankBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetTankBaseQuantities);
         }
      }


      private static void InitQto_TransformerBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetTransformerBaseQuantities = new QuantityDescription();
         qtoSetTransformerBaseQuantities.Name = "Qto_TransformerBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_TransformerBaseQuantities"))
         {
            qtoSetTransformerBaseQuantities.EntityTypes.Add(IFCEntityType.IfcTransformer);
            qtoSetTransformerBaseQuantities.ObjectType = "IfcTransformer";
            ifcPSE = new QuantityEntry("Qto_TransformerBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetTransformerBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetTransformerBaseQuantities);
         }
      }


      private static void InitQto_TubeBundleBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetTubeBundleBaseQuantities = new QuantityDescription();
         qtoSetTubeBundleBaseQuantities.Name = "Qto_TubeBundleBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_TubeBundleBaseQuantities"))
         {
            qtoSetTubeBundleBaseQuantities.EntityTypes.Add(IFCEntityType.IfcTubeBundle);
            qtoSetTubeBundleBaseQuantities.ObjectType = "IfcTubeBundle";
            ifcPSE = new QuantityEntry("Qto_TubeBundleBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetTubeBundleBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_TubeBundleBaseQuantities.NetWeight", "NetWeight");
            ifcPSE.PropertyName = "NetWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetTubeBundleBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetTubeBundleBaseQuantities);
         }
      }


      private static void InitQto_UnitaryControlElementBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetUnitaryControlElementBaseQuantities = new QuantityDescription();
         qtoSetUnitaryControlElementBaseQuantities.Name = "Qto_UnitaryControlElementBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_UnitaryControlElementBaseQuantities"))
         {
            qtoSetUnitaryControlElementBaseQuantities.EntityTypes.Add(IFCEntityType.IfcUnitaryControlElement);
            qtoSetUnitaryControlElementBaseQuantities.ObjectType = "IfcUnitaryControlElement";
            ifcPSE = new QuantityEntry("Qto_UnitaryControlElementBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetUnitaryControlElementBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetUnitaryControlElementBaseQuantities);
         }
      }


      private static void InitQto_UnitaryEquipmentBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetUnitaryEquipmentBaseQuantities = new QuantityDescription();
         qtoSetUnitaryEquipmentBaseQuantities.Name = "Qto_UnitaryEquipmentBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_UnitaryEquipmentBaseQuantities"))
         {
            qtoSetUnitaryEquipmentBaseQuantities.EntityTypes.Add(IFCEntityType.IfcUnitaryEquipment);
            qtoSetUnitaryEquipmentBaseQuantities.ObjectType = "IfcUnitaryEquipment";
            ifcPSE = new QuantityEntry("Qto_UnitaryEquipmentBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetUnitaryEquipmentBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetUnitaryEquipmentBaseQuantities);
         }
      }


      private static void InitQto_ValveBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetValveBaseQuantities = new QuantityDescription();
         qtoSetValveBaseQuantities.Name = "Qto_ValveBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_ValveBaseQuantities"))
         {
            qtoSetValveBaseQuantities.EntityTypes.Add(IFCEntityType.IfcValve);
            qtoSetValveBaseQuantities.ObjectType = "IfcValve";
            ifcPSE = new QuantityEntry("Qto_ValveBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetValveBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetValveBaseQuantities);
         }
      }


      private static void InitQto_VibrationIsolatorBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetVibrationIsolatorBaseQuantities = new QuantityDescription();
         qtoSetVibrationIsolatorBaseQuantities.Name = "Qto_VibrationIsolatorBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_VibrationIsolatorBaseQuantities"))
         {
            qtoSetVibrationIsolatorBaseQuantities.EntityTypes.Add(IFCEntityType.IfcVibrationIsolator);
            qtoSetVibrationIsolatorBaseQuantities.ObjectType = "IfcVibrationIsolator";
            ifcPSE = new QuantityEntry("Qto_VibrationIsolatorBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetVibrationIsolatorBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetVibrationIsolatorBaseQuantities);
         }
      }


      private static void InitQto_WallBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetWallBaseQuantities = new QuantityDescription();
         qtoSetWallBaseQuantities.Name = "Qto_WallBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_WallBaseQuantities"))
         {
            qtoSetWallBaseQuantities.EntityTypes.Add(IFCEntityType.IfcWall);
            qtoSetWallBaseQuantities.ObjectType = "IfcWall";
            ifcPSE = new QuantityEntry("Qto_WallBaseQuantities.Length", "Length");
            ifcPSE.PropertyName = "Length";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Länge");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Length");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "長さ");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.LengthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetWallBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_WallBaseQuantities.Width", "Width");
            ifcPSE.PropertyName = "Width";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Dicke");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Width");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "幅");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.WidthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetWallBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_WallBaseQuantities.Height", "Height");
            ifcPSE.PropertyName = "Height";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Höhe");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Height");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "高さ");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.HeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetWallBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_WallBaseQuantities.GrossFootprintArea", "GrossFootprintArea");
            ifcPSE.PropertyName = "GrossFootprintArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttogrundfläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Footprint Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "フットプリント面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossFootprintAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetWallBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_WallBaseQuantities.NetFootprintArea", "NetFootprintArea");
            ifcPSE.PropertyName = "NetFootprintArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettogrundfläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Footprint Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味フットプリント面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetFootprintAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetWallBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_WallBaseQuantities.GrossSideArea", "GrossSideArea");
            ifcPSE.PropertyName = "GrossSideArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttofläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Side Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "側面面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossSideAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetWallBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_WallBaseQuantities.NetSideArea", "NetSideArea");
            ifcPSE.PropertyName = "NetSideArea";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettofläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Side Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味側面面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetSideAreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetWallBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_WallBaseQuantities.GrossVolume", "GrossVolume");
            ifcPSE.PropertyName = "GrossVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttovolumen");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Volume");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "体積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetWallBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_WallBaseQuantities.NetVolume", "NetVolume");
            ifcPSE.PropertyName = "NetVolume";
            ifcPSE.QuantityType = QuantityType.Volume;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettovolumen");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Volume");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味体積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetVolumeCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetWallBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_WallBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Bruttogewicht");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "重量");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetWallBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_WallBaseQuantities.NetWeight", "NetWeight");
            ifcPSE.PropertyName = "NetWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Nettogewicht");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Net Weight");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "正味重量");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.NetWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetWallBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetWallBaseQuantities);
         }
      }


      private static void InitQto_WasteTerminalBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetWasteTerminalBaseQuantities = new QuantityDescription();
         qtoSetWasteTerminalBaseQuantities.Name = "Qto_WasteTerminalBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_WasteTerminalBaseQuantities"))
         {
            qtoSetWasteTerminalBaseQuantities.EntityTypes.Add(IFCEntityType.IfcWasteTerminal);
            qtoSetWasteTerminalBaseQuantities.ObjectType = "IfcWasteTerminal";
            ifcPSE = new QuantityEntry("Qto_WasteTerminalBaseQuantities.GrossWeight", "GrossWeight");
            ifcPSE.PropertyName = "GrossWeight";
            ifcPSE.QuantityType = QuantityType.Mass;
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Gross Weight");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.GrossWeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetWasteTerminalBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetWasteTerminalBaseQuantities);
         }
      }


      private static void InitQto_WindowBaseQuantities(IList<QuantityDescription> quantitySets)
      {
         QuantityDescription qtoSetWindowBaseQuantities = new QuantityDescription();
         qtoSetWindowBaseQuantities.Name = "Qto_WindowBaseQuantities";
         QuantityEntry ifcPSE = null;
         Type calcType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), "Qto_WindowBaseQuantities"))
         {
            qtoSetWindowBaseQuantities.EntityTypes.Add(IFCEntityType.IfcWindow);
            qtoSetWindowBaseQuantities.ObjectType = "IfcWindow";
            ifcPSE = new QuantityEntry("Qto_WindowBaseQuantities.Width", "Width");
            ifcPSE.PropertyName = "Width";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Breite");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Width");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "幅");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.WidthCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetWindowBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_WindowBaseQuantities.Height", "Height");
            ifcPSE.PropertyName = "Height";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Höhe");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Height");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "高さ");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.HeightCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetWindowBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_WindowBaseQuantities.Perimeter", "Perimeter");
            ifcPSE.PropertyName = "Perimeter";
            ifcPSE.QuantityType = QuantityType.Length;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Umfang");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Perimeter");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "周囲長");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.PerimeterCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetWindowBaseQuantities.AddEntry(ifcPSE);

            ifcPSE = new QuantityEntry("Qto_WindowBaseQuantities.Area", "Area");
            ifcPSE.PropertyName = "Area";
            ifcPSE.QuantityType = QuantityType.Area;
            ifcPSE.AddLocalizedParameterName(LanguageType.German, "Fläche");
            ifcPSE.AddLocalizedParameterName(LanguageType.English_USA, "Area");
            ifcPSE.AddLocalizedParameterName(LanguageType.Japanese, "面積");
            calcType = System.Reflection.Assembly.GetExecutingAssembly().GetType("Revit.IFC.Export.Exporter.PropertySet.Calculators.AreaCalculator");
            if (calcType != null)
               ifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});
            qtoSetWindowBaseQuantities.AddEntry(ifcPSE);

         }
         if (ifcPSE != null)
         {
            quantitySets.Add(qtoSetWindowBaseQuantities);
         }
      }


   }
}
