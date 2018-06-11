using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Common.Utility
{
   public static class IFCCompatibilityType
   {
      private static Dictionary<IFCEntityType, IFCEntityType> m_SuperTypeCompatibility = new Dictionary<IFCEntityType, IFCEntityType>();

      private static bool m_initialized = false;
      /// <summary>
      /// Dictionary that keeps the mapping of which subtypes are to be created as the supertype in the earlier schema version prior to IFC4
      /// </summary>
      /// <remarks>
      /// Note that this class assumes that it is mapping from IFC4 to IFC2x/IFC2x2/IFC2x3.  It is the caller's responsibility to ensure that.
      /// Note also that as future versions are added, this routine will likely need to be generalized.</remarks>
      private static void Initialize()
      {
         if (m_initialized)
            return;

         m_SuperTypeCompatibility.Add(IFCEntityType.IfcActuator, IFCEntityType.IfcDistributionControlElement);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcAlarm, IFCEntityType.IfcDistributionControlElement);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcController, IFCEntityType.IfcDistributionControlElement);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcFlowInstrument, IFCEntityType.IfcDistributionControlElement);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcProtectiveDeviceTrippingUnit, IFCEntityType.IfcDistributionControlElement);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcSensor, IFCEntityType.IfcDistributionControlElement);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcUnitaryControlElement, IFCEntityType.IfcDistributionControlElement);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcDistributionControlElement, IFCEntityType.IfcDistributionControlElement);   //itself

         m_SuperTypeCompatibility.Add(IFCEntityType.IfcAirToAirHeatRecovery, IFCEntityType.IfcEnergyConversionDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcBoiler, IFCEntityType.IfcEnergyConversionDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcBurner, IFCEntityType.IfcEnergyConversionDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcChiller, IFCEntityType.IfcEnergyConversionDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcCoil, IFCEntityType.IfcEnergyConversionDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcCondenser, IFCEntityType.IfcEnergyConversionDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcCooledBeam, IFCEntityType.IfcEnergyConversionDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcCoolingTower, IFCEntityType.IfcEnergyConversionDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcElectricGenerator, IFCEntityType.IfcEnergyConversionDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcElectricMotor, IFCEntityType.IfcEnergyConversionDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcEngine, IFCEntityType.IfcEnergyConversionDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcEvaporativeCooler, IFCEntityType.IfcEnergyConversionDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcEvaporator, IFCEntityType.IfcEnergyConversionDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcHeatExchanger, IFCEntityType.IfcEnergyConversionDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcHumidifier, IFCEntityType.IfcEnergyConversionDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcMotorConnection, IFCEntityType.IfcEnergyConversionDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcSolarDevice, IFCEntityType.IfcEnergyConversionDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcTransformer, IFCEntityType.IfcEnergyConversionDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcTubeBundle, IFCEntityType.IfcEnergyConversionDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcUnitaryEquipment, IFCEntityType.IfcEnergyConversionDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcEnergyConversionDevice, IFCEntityType.IfcEnergyConversionDevice);   //itself

         m_SuperTypeCompatibility.Add(IFCEntityType.IfcAirTerminalBox, IFCEntityType.IfcFlowController);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcDamper, IFCEntityType.IfcFlowController);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcElectricDistributionBoard, IFCEntityType.IfcElectricDistributionPoint);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcElectricTimeControl, IFCEntityType.IfcFlowController);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcFlowMeter, IFCEntityType.IfcFlowController);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcProtectiveDevice, IFCEntityType.IfcFlowController);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcSwitchingDevice, IFCEntityType.IfcFlowController);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcValve, IFCEntityType.IfcFlowController);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcFlowController, IFCEntityType.IfcFlowController);   //itself

         m_SuperTypeCompatibility.Add(IFCEntityType.IfcCableCarrierFitting, IFCEntityType.IfcFlowFitting);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcCableFitting, IFCEntityType.IfcFlowFitting);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcDuctFitting, IFCEntityType.IfcFlowFitting);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcJunctionBox, IFCEntityType.IfcFlowFitting);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcPipeFitting, IFCEntityType.IfcFlowFitting);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcFlowFitting, IFCEntityType.IfcFlowFitting);     //itself

         m_SuperTypeCompatibility.Add(IFCEntityType.IfcCompressor, IFCEntityType.IfcFlowMovingDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcFan, IFCEntityType.IfcFlowMovingDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcPump, IFCEntityType.IfcFlowMovingDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcFlowMovingDevice, IFCEntityType.IfcFlowMovingDevice);   //itself

         m_SuperTypeCompatibility.Add(IFCEntityType.IfcCableCarrierSegment, IFCEntityType.IfcFlowSegment);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcCableSegment, IFCEntityType.IfcFlowSegment);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcDuctSegment, IFCEntityType.IfcFlowSegment);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcPipeSegment, IFCEntityType.IfcFlowSegment);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcFlowSegment, IFCEntityType.IfcFlowSegment);   //itself

         m_SuperTypeCompatibility.Add(IFCEntityType.IfcElectricFlowStorageDevice, IFCEntityType.IfcFlowStorageDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcTank, IFCEntityType.IfcFlowStorageDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcFlowStorageDevice, IFCEntityType.IfcFlowStorageDevice);   //itself

         m_SuperTypeCompatibility.Add(IFCEntityType.IfcAirTerminal, IFCEntityType.IfcFlowTerminal);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcAudioVisualAppliance, IFCEntityType.IfcFlowTerminal);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcCommunicationsAppliance, IFCEntityType.IfcFlowTerminal);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcElectricAppliance, IFCEntityType.IfcFlowTerminal);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcFireSuppressionTerminal, IFCEntityType.IfcFlowTerminal);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcLamp, IFCEntityType.IfcFlowTerminal);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcLightFixture, IFCEntityType.IfcFlowTerminal);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcMedicalDevice, IFCEntityType.IfcFlowTerminal);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcOutlet, IFCEntityType.IfcFlowTerminal);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcSanitaryTerminal, IFCEntityType.IfcFlowTerminal);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcSpaceHeater, IFCEntityType.IfcFlowTerminal);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcStackTerminal, IFCEntityType.IfcFlowTerminal);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcWasteTerminal, IFCEntityType.IfcFlowTerminal);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcFlowTerminal, IFCEntityType.IfcFlowTerminal);   //itself

         m_SuperTypeCompatibility.Add(IFCEntityType.IfcDuctSilencer, IFCEntityType.IfcFlowTreatmentDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcFilter, IFCEntityType.IfcFlowTreatmentDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcInterceptor, IFCEntityType.IfcFlowTreatmentDevice);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcFlowTreatmentDevice, IFCEntityType.IfcFlowTreatmentDevice);   //itself

         m_SuperTypeCompatibility.Add(IFCEntityType.IfcFurniture, IFCEntityType.IfcFurnishingElement);   //itself
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcSystemFurnitureElement, IFCEntityType.IfcFurnishingElement);
         m_SuperTypeCompatibility.Add(IFCEntityType.IfcFurnishingElement, IFCEntityType.IfcFurnishingElement);   //itself

         m_initialized = true;
      }

      /// <summary>
      /// Check compatible type (supertype) when the entity is exported prior to IFC4 schema version
      /// </summary>
      /// <param name="typeToCheck">IFC Entity Type Enum</param>
      /// <param name="typeToUse">IFC Entity Type Enum for compatibility with IFC2x3 and IFC2x3.</param>
      /// <returns></returns>
      public static bool checkCompatibleType(IFCEntityType typeToCheck, out IFCEntityType typeToUse)
      {
         Initialize();

         typeToUse = typeToCheck;

         if (m_SuperTypeCompatibility.ContainsKey(typeToCheck))
         {
            typeToUse = m_SuperTypeCompatibility[typeToCheck];
            return true;
         }

         return false;
      }
   }
}