using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Common.Utility
{
   public static class IFCCompatibilityType
   {
      /// <summary>
      /// Dictionary that keeps the mapping of which subtypes are to be created as the supertype in the earlier schema version prior to IFC4
      /// </summary>
      /// <remarks>
      /// Note that this class assumes that it is mapping from IFC4 to IFC2x/IFC2x2/IFC2x3.  It is the caller's responsibility to ensure that.
      /// Note also that as future versions are added, this routine will likely need to be generalized.</remarks>
      private static Dictionary<IFCEntityType, IFCEntityType> SuperTypeCompatibility =
         new Dictionary<IFCEntityType, IFCEntityType>() {
            { IFCEntityType.IfcActuator, IFCEntityType.IfcDistributionControlElement },
            { IFCEntityType.IfcAlarm, IFCEntityType.IfcDistributionControlElement },
            { IFCEntityType.IfcController, IFCEntityType.IfcDistributionControlElement },
            { IFCEntityType.IfcFlowInstrument, IFCEntityType.IfcDistributionControlElement },
            { IFCEntityType.IfcProtectiveDeviceTrippingUnit, IFCEntityType.IfcDistributionControlElement },
            { IFCEntityType.IfcSensor, IFCEntityType.IfcDistributionControlElement },
            { IFCEntityType.IfcUnitaryControlElement, IFCEntityType.IfcDistributionControlElement },
            { IFCEntityType.IfcDistributionControlElement, IFCEntityType.IfcDistributionControlElement },   //itself
            { IFCEntityType.IfcAirToAirHeatRecovery, IFCEntityType.IfcEnergyConversionDevice },
            { IFCEntityType.IfcBoiler, IFCEntityType.IfcEnergyConversionDevice },
            { IFCEntityType.IfcBurner, IFCEntityType.IfcEnergyConversionDevice },
            { IFCEntityType.IfcChiller, IFCEntityType.IfcEnergyConversionDevice },
            { IFCEntityType.IfcCoil, IFCEntityType.IfcEnergyConversionDevice },
            { IFCEntityType.IfcCondenser, IFCEntityType.IfcEnergyConversionDevice },
            { IFCEntityType.IfcCooledBeam, IFCEntityType.IfcEnergyConversionDevice },
            { IFCEntityType.IfcCoolingTower, IFCEntityType.IfcEnergyConversionDevice },
            { IFCEntityType.IfcElectricGenerator, IFCEntityType.IfcEnergyConversionDevice },
            { IFCEntityType.IfcElectricMotor, IFCEntityType.IfcEnergyConversionDevice },
            { IFCEntityType.IfcEngine, IFCEntityType.IfcEnergyConversionDevice },
            { IFCEntityType.IfcEvaporativeCooler, IFCEntityType.IfcEnergyConversionDevice },
            { IFCEntityType.IfcEvaporator, IFCEntityType.IfcEnergyConversionDevice },
            { IFCEntityType.IfcHeatExchanger, IFCEntityType.IfcEnergyConversionDevice },
            { IFCEntityType.IfcHumidifier, IFCEntityType.IfcEnergyConversionDevice },
            { IFCEntityType.IfcMotorConnection, IFCEntityType.IfcEnergyConversionDevice },
            { IFCEntityType.IfcSolarDevice, IFCEntityType.IfcEnergyConversionDevice },
            { IFCEntityType.IfcTransformer, IFCEntityType.IfcEnergyConversionDevice },
            { IFCEntityType.IfcTubeBundle, IFCEntityType.IfcEnergyConversionDevice },
            { IFCEntityType.IfcUnitaryEquipment, IFCEntityType.IfcEnergyConversionDevice },
            { IFCEntityType.IfcEnergyConversionDevice, IFCEntityType.IfcEnergyConversionDevice },   //itself
            { IFCEntityType.IfcAirTerminalBox, IFCEntityType.IfcFlowController },
            { IFCEntityType.IfcDamper, IFCEntityType.IfcFlowController },
            { IFCEntityType.IfcElectricDistributionBoard, IFCEntityType.IfcElectricDistributionPoint },
            { IFCEntityType.IfcElectricTimeControl, IFCEntityType.IfcFlowController },
            { IFCEntityType.IfcFlowMeter, IFCEntityType.IfcFlowController },
            { IFCEntityType.IfcProtectiveDevice, IFCEntityType.IfcFlowController },
            { IFCEntityType.IfcSwitchingDevice, IFCEntityType.IfcFlowController },
            { IFCEntityType.IfcValve, IFCEntityType.IfcFlowController },
            { IFCEntityType.IfcFlowController, IFCEntityType.IfcFlowController },   //itself   
            { IFCEntityType.IfcCableCarrierFitting, IFCEntityType.IfcFlowFitting },
            { IFCEntityType.IfcCableFitting, IFCEntityType.IfcFlowFitting },
            { IFCEntityType.IfcDuctFitting, IFCEntityType.IfcFlowFitting },
            { IFCEntityType.IfcJunctionBox, IFCEntityType.IfcFlowFitting },
            { IFCEntityType.IfcPipeFitting, IFCEntityType.IfcFlowFitting },
            { IFCEntityType.IfcFlowFitting, IFCEntityType.IfcFlowFitting },     //itself
            { IFCEntityType.IfcCompressor, IFCEntityType.IfcFlowMovingDevice },
            { IFCEntityType.IfcFan, IFCEntityType.IfcFlowMovingDevice },
            { IFCEntityType.IfcPump, IFCEntityType.IfcFlowMovingDevice },
            { IFCEntityType.IfcFlowMovingDevice, IFCEntityType.IfcFlowMovingDevice },   //itself
            { IFCEntityType.IfcCableCarrierSegment, IFCEntityType.IfcFlowSegment },
            { IFCEntityType.IfcCableSegment, IFCEntityType.IfcFlowSegment },
            { IFCEntityType.IfcDuctSegment, IFCEntityType.IfcFlowSegment },
            { IFCEntityType.IfcPipeSegment, IFCEntityType.IfcFlowSegment },
            { IFCEntityType.IfcFlowSegment, IFCEntityType.IfcFlowSegment },   //itself
            { IFCEntityType.IfcElectricFlowStorageDevice, IFCEntityType.IfcFlowStorageDevice },
            { IFCEntityType.IfcTank, IFCEntityType.IfcFlowStorageDevice },
            { IFCEntityType.IfcFlowStorageDevice, IFCEntityType.IfcFlowStorageDevice },   //itself
            { IFCEntityType.IfcAirTerminal, IFCEntityType.IfcFlowTerminal },
            { IFCEntityType.IfcAudioVisualAppliance, IFCEntityType.IfcFlowTerminal },
            { IFCEntityType.IfcCommunicationsAppliance, IFCEntityType.IfcFlowTerminal },
            { IFCEntityType.IfcElectricAppliance, IFCEntityType.IfcFlowTerminal },
            { IFCEntityType.IfcFireSuppressionTerminal, IFCEntityType.IfcFlowTerminal },
            { IFCEntityType.IfcLamp, IFCEntityType.IfcFlowTerminal },
            { IFCEntityType.IfcLightFixture, IFCEntityType.IfcFlowTerminal },
            { IFCEntityType.IfcMedicalDevice, IFCEntityType.IfcFlowTerminal },
            { IFCEntityType.IfcOutlet, IFCEntityType.IfcFlowTerminal },
            { IFCEntityType.IfcSanitaryTerminal, IFCEntityType.IfcFlowTerminal },
            { IFCEntityType.IfcSpaceHeater, IFCEntityType.IfcFlowTerminal },
            { IFCEntityType.IfcStackTerminal, IFCEntityType.IfcFlowTerminal },
            { IFCEntityType.IfcWasteTerminal, IFCEntityType.IfcFlowTerminal },
            { IFCEntityType.IfcFlowTerminal, IFCEntityType.IfcFlowTerminal },   //itself
            { IFCEntityType.IfcDuctSilencer, IFCEntityType.IfcFlowTreatmentDevice },
            { IFCEntityType.IfcFilter, IFCEntityType.IfcFlowTreatmentDevice },
            { IFCEntityType.IfcInterceptor, IFCEntityType.IfcFlowTreatmentDevice },
            { IFCEntityType.IfcFlowTreatmentDevice, IFCEntityType.IfcFlowTreatmentDevice },   //itself
            { IFCEntityType.IfcFurniture, IFCEntityType.IfcFurnishingElement },   //itself
            { IFCEntityType.IfcSystemFurnitureElement, IFCEntityType.IfcFurnishingElement },
            { IFCEntityType.IfcFurnishingElement, IFCEntityType.IfcFurnishingElement },   //itself
            { IFCEntityType.IfcElementAssemblyType, IFCEntityType.IfcTypeProduct },
            { IFCEntityType.IfcReinforcingBarType, IFCEntityType.IfcTypeProduct },
            { IFCEntityType.IfcReinforcingMeshType, IFCEntityType.IfcTypeProduct }
         };


      /// <summary>
      /// Check compatible type (supertype) when the entity is exported prior to IFC4 schema version
      /// </summary>
      /// <param name="typeToCheck">IFC Entity Type Enum</param>
      /// <param name="typeToUse">IFC Entity Type Enum for compatibility with IFC2x3 and IFC2x3.</param>
      /// <returns></returns>
      public static bool CheckCompatibleType(IFCEntityType typeToCheck, out IFCEntityType typeToUse)
      {
         typeToUse = typeToCheck;

         if (SuperTypeCompatibility.TryGetValue(typeToCheck, out IFCEntityType overrideValue))
         {
            typeToUse = overrideValue;
            return true;
         }

         return false;
      }
   }
}