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
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Utilities to work with ExporterCacheManager.UnitsCache.
   /// </summary>
   public class UnitUtil
   {
      /// <summary>
      /// Returns the scaling factor for length from Revit internal units to IFC units.
      /// </summary>
      /// <returns>The scaling factors.</returns>
      /// <remarks>This routine is intended to be used for API routines that expect a scale parameter.
      /// For .NET routines, use ScaleLength() instead.</remarks>
      static public double ScaleLengthForRevitAPI()
      {
         return ExporterCacheManager.UnitsCache.Scale(SpecTypeId.Length, 1.0);
      }

      /// <summary>
      /// Converts a position in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledUV">The position in Revit internal units.</param>
      /// <returns>The position in IFC units.</returns>
      static public UV ScaleLength(UV unscaledUV)
      {
         return ExporterCacheManager.UnitsCache.Scale(SpecTypeId.Length, unscaledUV);
      }

      /// <summary>
      /// Converts a position in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledXYZ">The position in Revit internal units.</param>
      /// <returns>The position in IFC units.</returns>
      static public XYZ ScaleLength(XYZ unscaledXYZ)
      {
         return ExporterCacheManager.UnitsCache.Scale(SpecTypeId.Length, unscaledXYZ);
      }

      /// <summary>
      /// Converts an unscaled value in Revit internal units to IFC units, given the unit type.
      /// </summary>
      /// <param name="specTypeId">Identifier of the spec.</param>
      /// <param name="unscaledValue">The value in Revit internal units.</param>
      /// <returns>The value in IFC units.</returns>
      static public double ScaleDouble(ForgeTypeId specTypeId, double unscaledValue)
      {
         return ExporterCacheManager.UnitsCache.Scale(specTypeId, unscaledValue);
      }

      /// <summary>
      /// Converts a length in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledLength">The length in Revit internal units.</param>
      /// <returns>The length in IFC units.</returns>
      static public double ScaleLength(double unscaledLength)
      {
         return ScaleDouble(SpecTypeId.Length, unscaledLength);
      }

      /// <summary>
      /// Converts a force value in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledForce">The force value in Revit internal units.</param>
      /// <returns>The force in IFC units.</returns>
      static public double ScaleForce(double unscaledForce)
      {
         return ScaleDouble(SpecTypeId.Force, unscaledForce);
      }

      static public double ScaleLinearForce(double unscaledLinearForce)
      {
         return UnitUtils.ConvertFromInternalUnits(unscaledLinearForce, UnitTypeId.NewtonsPerMeter);
      }

      static public double ScalePlanarForce(double unscaledPlanarForce)
      {
         return UnitUtils.ConvertFromInternalUnits(unscaledPlanarForce, UnitTypeId.NewtonsPerSquareMeter);
      }

      /// <summary>
      /// Converts a power value in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledPower">The power value in Revit internal units.</param>
      /// <returns>The power in IFC units.</returns>
      static public double ScalePower(double unscaledPower)
      {
         return ScaleDouble(SpecTypeId.HvacPower, unscaledPower);
      }

      /// <summary>
      /// Converts a sound power value in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledPower">The power value in Revit internal units.</param>
      /// <returns>The power in IFC units.</returns>
      static public double ScaleSoundPower(double unscaledPower)
      {
         return ScaleDouble(SpecTypeId.Wattage, unscaledPower);
      }

      /// <summary>
      /// Converts a sound pressure value in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledPressure">The pressure value in Revit internal units.</param>
      /// <returns>The pressure in IFC units.</returns>
      static public double ScaleSoundPressure(double unscaledPressure)
      {
         return ScaleDouble(SpecTypeId.HvacPressure, unscaledPressure);
      }

      /// <summary>
      /// Converts a thermal transmittance value in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledThermalTransmittance">The thermal transmittance value in Revit internal units.</param>
      /// <returns>The thermal transmittance in IFC units.</returns>
      static public double ScaleThermalTransmittance(double unscaledThermalTransmittance)
      {
         return ScaleDouble(SpecTypeId.HeatTransferCoefficient, unscaledThermalTransmittance);
      }

      /// <summary>
      /// Converts an area in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledArea">The area in Revit internal units.</param>
      /// <returns>The area in IFC units.</returns>
      static public double ScaleArea(double unscaledArea)
      {
         return ScaleDouble(SpecTypeId.Area, unscaledArea);
      }

      /// <summary>
      /// Converts an area in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledAcceleration">The area in Revit internal units.</param>
      /// <returns>The area in IFC units.</returns>
      static public double ScaleAcceleration(double unscaledAcceleration)
      {
         return ScaleDouble(SpecTypeId.Acceleration, unscaledAcceleration);
      }

      /// <summary>
      /// Converts an area in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledEnergy">The area in Revit internal units.</param>
      /// <returns>The area in IFC units.</returns>
      static public double ScaleEnergy(double unscaledEnergy)
      {
         return ScaleDouble(SpecTypeId.Energy, unscaledEnergy);
      }

      /// <summary>
      /// Converts an area in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledLinearMoment">The area in Revit internal units.</param>
      /// <returns>The area in IFC units.</returns>
      static public double ScaleLinearMoment(double unscaledLinearMoment)
      {
         return ScaleDouble(SpecTypeId.LinearMoment, unscaledLinearMoment);
      }

      /// <summary>
      /// Converts an area in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledMassPerLength">The area in Revit internal units.</param>
      /// <returns>The area in IFC units.</returns>
      static public double ScaleMassPerLength(double unscaledMassPerLength)
      {
         return ScaleDouble(SpecTypeId.MassPerUnitLength, unscaledMassPerLength);
      }

      /// <summary>
      /// Converts an area in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledTorque">The area in Revit internal units.</param>
      /// <returns>The area in IFC units.</returns>
      static public double ScaleTorque(double unscaledTorque)
      {
         return ScaleDouble(SpecTypeId.Moment, unscaledTorque);
      }

      /// <summary>
      /// Converts an area in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledLinearStiffness">The area in Revit internal units.</param>
      /// <returns>The area in IFC units.</returns>
      static public double ScaleLinearStiffness(double unscaledLinearStiffness)
      {
         return ScaleDouble(SpecTypeId.PointSpringCoefficient, unscaledLinearStiffness);
      }

      /// <summary>
      /// Converts an area in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledAngularVelocity">The area in Revit internal units.</param>
      /// <returns>The area in IFC units.</returns>
      static public double ScaleAngularVelocity(double unscaledAngularVelocity)
      {
         return ScaleDouble(SpecTypeId.Pulsation, unscaledAngularVelocity);
      }

      /// <summary>
      /// Converts an area in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledThermalResistance">The area in Revit internal units.</param>
      /// <returns>The area in IFC units.</returns>
      static public double ScaleThermalResistance(double unscaledThermalResistance)
      {
         return ScaleDouble(SpecTypeId.ThermalResistance, unscaledThermalResistance);
      }

      /// <summary>
      /// Converts an area in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledWarpingConstant">The area in Revit internal units.</param>
      /// <returns>The area in IFC units.</returns>
      static public double ScaleWarpingConstant(double unscaledWarpingConstant)
      {
         return ScaleDouble(SpecTypeId.WarpingConstant, unscaledWarpingConstant);
      }

      /// <summary>
      /// Converts a volume in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledVolume">The volume in Revit internal units.</param>
      /// <returns>The volume in IFC units.</returns>
      static public double ScaleVolume(double unscaledVolume)
      {
         return ScaleDouble(SpecTypeId.Volume, unscaledVolume);
      }

      /// <summary>
      /// Converts a VolumetricFlowRate in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledVolumetricFlowRate">The volumetric flow rate in Revit internal units.</param>
      /// <returns>The volumetric flow rate in IFC units.</returns>
      static public double ScaleVolumetricFlowRate(double unscaledVolumetricFlowRate)
      {
         return ScaleDouble(SpecTypeId.AirFlow, unscaledVolumetricFlowRate);
      }

      /// <summary>
      /// Converts a LuminousFlux in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledLuminousFlux">The luminous flux in Revit internal units.</param>
      /// <returns>The luminous flux in IFC units.</returns>
      static public double ScaleLuminousFlux(double unscaledLuminousFlux)
      {
         return ScaleDouble(SpecTypeId.LuminousFlux, unscaledLuminousFlux);
      }

      /// <summary>
      /// Converts a LuminousIntensity in Revit internal units to IFC units.
      /// </summary>
      /// <param name="unscaledIntensityFlux">The luminous intensity in Revit internal units.</param>
      /// <returns>The luminous intensity in IFC units.</returns>
      static public double ScaleLuminousIntensity(double unscaledLuminousIntensity)
      {
         return ScaleDouble(SpecTypeId.LuminousIntensity, unscaledLuminousIntensity);
      }

      /// <summary>
      /// Converts an angle in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledArea">The angle in Revit internal units.</param>
      /// <returns>The angle in Revit display units.</returns>
      static public double ScaleAngle(double unscaledAngle)
      {
         return ScaleDouble(SpecTypeId.Angle, unscaledAngle);
      }

      // <summary>
      /// Converts an electrical current in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledCurrent">The electrical current in Revit internal units.</param>
      /// <returns>The electrical current in Revit display units.</returns>
      static public double ScaleElectricCurrent(double unscaledCurrent)
      {
         return ScaleDouble(SpecTypeId.Current, unscaledCurrent);
      }

      // <summary>
      /// Converts an electrical illuminance in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledVoltage">The elecrical illuminance in Revit internal units.</param>
      /// <returns>The electrical illuminance in Revit display units.</returns>
      static public double ScaleIlluminance(double unscaledIlluminance)
      {
         return ScaleDouble(SpecTypeId.Illuminance, unscaledIlluminance);
      }

      // <summary>
      /// Converts an electrical voltage in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledVoltage">The elecrical voltage in Revit internal units.</param>
      /// <returns>The electrical current in Revit display units.</returns>
      static public double ScaleElectricVoltage(double unscaledVoltage)
      {
         return ScaleDouble(SpecTypeId.ElectricalPotential, unscaledVoltage);
      }

      /// <summary>
      /// Converts thermodynamic temperature in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The thermodynamic temperature in Revit internal units.</param>
      /// <returns>The thermodynamic temperature in Revit display units.</returns>
      static public double ScaleThermodynamicTemperature(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.HvacTemperature, unscaledValue);
      }

      /// <summary>
      /// Converts DynamicViscosity in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The DynamicViscosity in Revit internal units.</param>
      /// <returns>The DynamicViscosity in Revit display units.</returns>
      static public double ScaleDynamicViscosity(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.HvacViscosity, unscaledValue);
      }

      /// <summary>
      /// Converts IsothermalMoistureCapacity in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The IsothermalMoistureCapacity in Revit internal units.</param>
      /// <returns>The IsothermalMoistureCapacity in Revit display units.</returns>
      static public double ScaleIsothermalMoistureCapacity(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.IsothermalMoistureCapacity, unscaledValue);
      }

      /// <summary>
      /// Converts MassDensity in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The MassDensity in Revit internal units.</param>
      /// <returns>The MassDensity in Revit display units.</returns>
      static public double ScaleMassDensity(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.MassDensity, unscaledValue);
      }

      /// <summary>
      /// Converts Mass in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The Mass in Revit internal units.</param>
      /// <returns>The Mass in Revit display units.</returns>
      static public double ScaleMass(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.Mass, unscaledValue);
      }

      /// <summary>
      /// Converts LinearVelocity in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The LinearVelocity in Revit internal units.</param>
      /// <returns>The LinearVelocity in Revit display units.</returns>
      static public double ScaleLinearVelocity(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.HvacVelocity, unscaledValue);
      }
      
      /// <summary>
      /// Converts ModulusOfElasticity in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The ModulusOfElasticity in Revit internal units.</param>
      /// <returns>The ModulusOfElasticity in Revit display units.</returns>
      static public double ScaleModulusOfElasticity(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.Stress, unscaledValue);
      }

      /// <summary>
      /// Converts VaporPermeability in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The VaporPermeability in Revit internal units.</param>
      /// <returns>The VaporPermeability in Revit display units.</returns>
      static public double ScaleVaporPermeability(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.Permeability, unscaledValue);
      }

      /// <summary>
      /// Converts ThermalExpansionCoefficient in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The ThermalExpansionCoefficient in Revit internal units.</param>
      /// <returns>The ThermalExpansionCoefficient in Revit display units.</returns>
      static public double ScaleThermalExpansionCoefficient(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.ThermalExpansionCoefficient, unscaledValue);
      }

      /// <summary>
      /// Converts Pressure in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The Pressure in Revit internal units.</param>
      /// <returns>The Pressure in Revit display units.</returns>
      static public double ScalePressure(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.HvacPressure, unscaledValue);
      }

      /// <summary>
      /// Converts SpecificHeatCapacity in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The Pressure in Revit internal units.</param>
      /// <returns>The Pressure in Revit display units.</returns>
      static public double ScaleSpecificHeatCapacity(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.SpecificHeat, unscaledValue);
      }

      /// <summary>
      /// Converts HeatingValue in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The HeatingValue in Revit internal units.</param>
      /// <returns>The HeatingValue in Revit display units.</returns>
      static public double ScaleHeatingValue(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.SpecificHeatOfVaporization, unscaledValue);
      }

      /// <summary>
      /// Converts MoistureDiffusivity in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The MoistureDiffusivity in Revit internal units.</param>
      /// <returns>The MoistureDiffusivity in Revit display units.</returns>
      static public double ScaleMoistureDiffusivity(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.Diffusivity, unscaledValue);
      }

      /// <summary>
      /// Converts IonConcentration in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The IonConcentration in Revit internal units.</param>
      /// <returns>The IonConcentration in Revit display units.</returns>
      static public double ScaleIonConcentration(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.PipingDensity, unscaledValue);
      }

      /// <summary>
      /// Converts HeatFluxDensity in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The HeatFluxDensity in Revit internal units.</param>
      /// <returns>The HeatFluxDensity in Revit display units.</returns>
      static public double ScaleHeatFluxDensity(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.HvacPowerDensity, unscaledValue);
      }

      /// <summary>
      /// Converts AreaDensity in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The AreaDensity in Revit internal units.</param>
      /// <returns>The AreaDensity in Revit display units.</returns>
      static public double ScaleAreaDensity(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.MassPerUnitArea, unscaledValue);
      }

      /// <summary>
      /// Converts Time in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The Time in Revit internal units.</param>
      /// <returns>The Time in Revit display units.</returns>
      static public double ScaleTime(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.Time, unscaledValue);
      }

      /// <summary>
      /// Converts MomentOfInertia in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The MomentOfInertia in Revit internal units.</param>
      /// <returns>The MomentOfInertia in Revit display units.</returns>
      static public double ScaleMomentOfInertia(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.MomentOfInertia, unscaledValue);
      }

      /// <summary>
      /// Converts ThermalConductivity in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The ThermalConductivity in Revit internal units.</param>
      /// <returns>The ThermalConductivity in Revit display units.</returns>
      static public double ScaleThermalConductivity(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.ThermalConductivity, unscaledValue);
      }

      /// <summary>
      /// Converts RotationalFrequency in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The RotationalFrequency in Revit internal units.</param>
      /// <returns>The RotationalFrequency in Revit display units.</returns>
      static public double ScaleRotationalFrequency(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.AngularSpeed, unscaledValue);
      }

      /// <summary>
      /// Converts ScaleMassFlowRate in Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unscaledValue">The ScaleMassFlowRate in Revit internal units.</param>
      /// <returns>The ScaleMassFlowRate in Revit display units.</returns>
      static public double ScaleMassFlowRate(double unscaledValue)
      {
         return ScaleDouble(SpecTypeId.PipingMassPerTime, unscaledValue);
      }

      /// <summary>
      /// Converts a position in IFC units to Revit internal units.
      /// </summary>
      /// <param name="unscaledArea">The position in IFC units.</param>
      /// <returns>The position in Revit internal units.</returns>
      static public XYZ UnscaleLength(XYZ scaledXYZ)
      {
         return ExporterCacheManager.UnitsCache.Unscale(SpecTypeId.Length, scaledXYZ);
      }

      /// <summary>
      /// Converts a position in IFC units to Revit internal units.
      /// </summary>
      /// <param name="scaledLength">The length in IFC units.</param>
      /// <returns>The length in Revit internal units.</returns>
      static public double UnscaleLength(double scaledLength)
      {
         return ExporterCacheManager.UnitsCache.Unscale(SpecTypeId.Length, scaledLength);
      }

      /// <summary>
      /// Converts an area in IFC units to Revit internal units.
      /// </summary>
      /// <param name="scaledArea">The area in IFC units.</param>
      /// <returns>The area in Revit internal units.</returns>
      static public double UnscaleArea(double scaledArea)
      {
         return ExporterCacheManager.UnitsCache.Unscale(SpecTypeId.Area, scaledArea);
      }
   }
}
