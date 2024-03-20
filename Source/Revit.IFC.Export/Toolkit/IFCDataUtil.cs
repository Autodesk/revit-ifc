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
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Toolkit
{
   /// <summary>
   /// Represents IfcValue.
   /// </summary>
   class IFCDataUtil
   {
      /// <summary>
      /// Event is fired when code reduces length of string to maximal allowed size.
      /// It sends information string which can be logged or shown to user.
      /// </summary>
      /// /// <param name="warnText">Infromation string with diangostic info about truncation happened.</param>
      public delegate void Notify(string warnText);
      public static event Notify IFCStringTooLongWarn;
      private static void OnIFCStringTooLongWarn(string val, int reducedToSize)
      {
         string warnMsg = String.Format("IFC warning: Size of string \"{0}\" was reduced to {1}", val, reducedToSize);
         IFCStringTooLongWarn?.Invoke(warnMsg);
      }
      public static void EventClear()
      {
         IFCStringTooLongWarn = null;
      }
      /// <summary>
      /// Creates an IFCData object as IfcLabel.
      /// </summary>
      /// <param name="value">The string value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsLabel(string value)
      {
         if (value == null)
            return null;

         if (value.Length > IFCLimits.MAX_IFCLABEL_STR_LEN)
         {
            OnIFCStringTooLongWarn(value, IFCLimits.MAX_IFCLABEL_STR_LEN);
            value = value.Remove(IFCLimits.MAX_IFCLABEL_STR_LEN);
         }
         return IFCData.CreateStringOfType(value, "IfcLabel");
      }

      /// <summary>
      /// Creates an IFCData object as IfcText.
      /// </summary>
      /// <param name="value">The string value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsText(string value)
      {
         if (value == null)
            return null;

         int maxStrLen = IFCLimits.CalculateMaxAllowedSize(value);
         if (value.Length > maxStrLen)
         {
            OnIFCStringTooLongWarn(value, maxStrLen);
            value = value.Remove(maxStrLen);
         }
         return IFCData.CreateStringOfType(value, "IfcText");
      }

      /// <summary>
      /// Creates an IFCData object as IfcIdentifier.
      /// </summary>
      /// <param name="value">The string value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsIdentifier(string value)
      {
         if (value == null)
            return null;

         if (value.Length > IFCLimits.MAX_IFCIDENTIFIER_STR_LEN)
         {
            OnIFCStringTooLongWarn(value, IFCLimits.MAX_IFCIDENTIFIER_STR_LEN);
            value = value.Remove(IFCLimits.MAX_IFCIDENTIFIER_STR_LEN);
         }
         return IFCData.CreateStringOfType(value, "IfcIdentifier");
      }

      /// <summary>
      /// Creates an IFCData object as IfcBoolean.
      /// </summary>
      /// <param name="value">The boolean value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsBoolean(bool value)
      {
         return IFCData.CreateBooleanOfType(value, "IfcBoolean");
      }

      /// <summary>
      /// Creates an IFCData object as IfcLogical.
      /// </summary>
      /// <param name="value">The logical value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsLogical(IFCLogical value)
      {
         return IFCData.CreateLogicalOfType(value, "IfcLogical");
      }

      /// <summary>
      /// Creates an IFCData object as IfcInteger.
      /// </summary>
      /// <param name="value">The integer value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsInteger(int value)
      {
         return IFCData.CreateIntegerOfType(value, "IfcInteger");
      }

      /// <summary>
      /// Creates an IFCData object as IfcReal.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsReal(double value)
      {
         return CreateAsMeasure(value, "IfcReal");
      }

      /// <summary>
      /// Creates an IFCData object as IfcNumericMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsNumeric(double value)
      {
         return CreateAsMeasure(value, "IfcNumericMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcRatioMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsRatioMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcRatioMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcNormalisedRatioMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsNormalisedRatioMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcNormalisedRatioMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcSpecularExponent.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsSpecularExponent(double value)
      {
         return CreateAsMeasure(value, "IfcSpecularExponent");
      }

      /// <summary>
      /// Creates an IFCData object as IfcPositiveRatioMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsPositiveRatioMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcPositiveRatioMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcLengthMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsLengthMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcLengthMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcVolumeMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsVolumeMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcVolumeMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcPositiveLengthMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsPositiveLengthMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcPositiveLengthMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcPositivePlaneAngleMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsPositivePlaneAngleMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcPositivePlaneAngleMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcPlaneAngleMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsPlaneAngleMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcPlaneAngleMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcAreaMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsAreaMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcAreaMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcAccelerationMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsAccelerationMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcAccelerationMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcEnergyMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsEnergyMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcEnergyMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcLinearMomentMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsLinearMomentMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcLinearMomentMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcMassPerLengthMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsMassPerLengthMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcMassPerLengthMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcTorqueMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsTorqueMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcTorqueMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcLinearStiffnessMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsLinearStiffnessMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcLinearStiffnessMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcAngularVelocityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsAngularVelocityMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcAngularVelocityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcThermalResistanceMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsThermalResistanceMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcThermalResistanceMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcWarpingConstantMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsWarpingConstantMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcWarpingConstantMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcLinearVelocityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsLinearVelocityMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcLinearVelocityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcCountMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsCountMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcCountMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcCountMeasure. Since IFC4x3 the Count measure value has been changed to Integer
      /// </summary>
      /// <param name="value">The integer value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsCountMeasure(int value)
      {
         return CreateAsMeasure(value, "IfcCountMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcParameterValue.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsParameterValue(double value)
      {
         return CreateAsMeasure(value, "IfcParameterValue");
      }

      /// <summary>
      /// Creates an IFCData object as IfcPowerMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsPowerMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcPowerMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcSoundPowerMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsSoundPowerMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcSoundPowerMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcSoundPressureMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsSoundPressureMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcSoundPressureMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcFrequencyMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsFrequencyMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcFrequencyMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcElectricalCurrentMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsElectricCurrentMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcElectricCurrentMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcElectricalVoltageMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsElectricVoltageMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcElectricVoltageMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcThermodynamicTemperatureMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsThermodynamicTemperatureMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcThermodynamicTemperatureMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcDynamicViscosityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsDynamicViscosityMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcDynamicViscosityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcIsothermalMoistureCapacityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsIsothermalMoistureCapacityMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcIsothermalMoistureCapacityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcMassDensityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsMassDensityMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcMassDensityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcModulusOfElasticityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsModulusOfElasticityMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcModulusOfElasticityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcVaporPermeabilityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsVaporPermeabilityMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcVaporPermeabilityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcThermalExpansionCoefficientMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsThermalExpansionCoefficientMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcThermalExpansionCoefficientMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcPressureMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsPressureMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcPressureMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcSpecificHeatCapacityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsSpecificHeatCapacityMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcSpecificHeatCapacityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcHeatingValueMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsHeatingValueMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcHeatingValueMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcMoistureDiffusivityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsMoistureDiffusivityMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcMoistureDiffusivityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcIonConcentrationMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsIonConcentrationMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcIonConcentrationMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcMomentOfInertiaMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsMomentOfInertiaMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcMomentOfInertiaMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcHeatFluxDensityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsHeatFluxDensityMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcHeatFluxDensityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcAreaDensityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsAreaDensityMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcAreaDensityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcThermalConductivityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsThermalConductivityMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcThermalConductivityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcThermalTransmittanceMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsThermalTransmittanceMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcThermalTransmittanceMeasure");
      }

      /// <summary>
      /// Create a positive ratio measure data from value.
      /// </summary>
      /// <param name="value">The value of the property.</param>
      /// <returns>The created property data.</returns>
      public static IFCData CreatePositiveRatioMeasureData(double value)
      {
         return CreateRatioMeasureDataCommon(value, PropertyType.PositiveRatio);
      }

      /// <summary>
      /// Create a ratio measure data from value.
      /// </summary>
      /// <param name="value">The value of the property.</param>
      /// <returns>The created property data.</returns>
      public static IFCData CreateRatioMeasureData(double value)
      {
         return CreateRatioMeasureDataCommon(value, PropertyType.Ratio);
      }

      /// <summary>
      /// Create a normalised ratio measure data from value.
      /// </summary>
      /// <param name="value">The value of the property.</param>
      /// <returns>The created property data.</returns>
      public static IFCData CreateNormalisedRatioMeasureData(double value)
      {
         return CreateRatioMeasureDataCommon(value, PropertyType.NormalisedRatio);
      }

      public static IFCData CreateRatioMeasureDataCommon(double value, PropertyType propertyType)
      {
         IFCData ratioData = null;
         switch (propertyType)
         {
            case PropertyType.PositiveRatio:
               {
                  if (value < MathUtil.Eps())
                     return null;

                  ratioData = CreateAsPositiveRatioMeasure(value);
                  break;
               }
            case PropertyType.NormalisedRatio:
               {
                  if (value < -MathUtil.Eps() || value > 1.0 + MathUtil.Eps())
                     return null;

                  ratioData = CreateAsNormalisedRatioMeasure(value);
                  break;
               }
            default:
               {
                  ratioData = CreateAsRatioMeasure(value);
                  break;
               }
         }

         return ratioData;
      }

      /// <summary>
      /// Creates an IFCData object as IfcVolumetricFlowRate.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsVolumetricFlowRateMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcVolumetricFlowRateMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcIlluminanceMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsIlluminanceMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcIlluminanceMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcLuminousFluxMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsLuminousFluxMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcLuminousFluxMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcLuminousIntensityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsLuminousIntensityMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcLuminousIntensityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcForceMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsForceMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcForceMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcLinearForceMeasure
      /// </summary>
      /// <param name="value">the double value</param>
      /// <returns>the IFCData object</returns>
      public static IFCData CreateAsLinearForceMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcLinearForceMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcMassMeasure
      /// </summary>
      /// <param name="value">the double value</param>
      /// <returns>the IFCData object</returns>
      public static IFCData CreateAsMassMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcMassMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcTimeMeasure
      /// </summary>
      /// <param name="value">the double value</param>
      /// <returns>the IFCData object</returns>
      public static IFCData CreateAsTimeMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcTimeMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcPlanarForceMeasure
      /// </summary>
      /// <param name="value">the double value</param>
      /// <returns>the IFCData object</returns>
      public static IFCData CreateAsPlanarForceMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcPlanarForceMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as an IfcMeasure of the right type.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <param name="type">The type of IfcMeasure (e.g. IfcForceMeasure).</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsMeasure(double value, string type)
      {
         return IFCData.CreateDoubleOfType(value, type);
      }

      /// <summary>
      /// Creates an IFCData object as an IfcMeasure of the right type. The value type for Count Measure is changed to Integer from IFC4x3 onward
      /// </summary>
      /// <param name="value">The integer value.</param>
      /// <param name="type">The type of IfcMeasure (e.g. IfcForceMeasure).</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsMeasure(int value, string type)
      {
         return IFCData.CreateIntegerOfType(value, type);
      }

      public static string ValidateEnumeratedValue(string value, Type propertyEnumerationType)
      {
         if (propertyEnumerationType != null && propertyEnumerationType.IsEnum && !string.IsNullOrEmpty(value))
         {
            foreach (object enumeratedValue in Enum.GetValues(propertyEnumerationType))
            {
               string enumValue = enumeratedValue.ToString();
               if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, enumValue))
               {
                  return enumValue;
               }
            }
         }

         return null;
      }


      /// <summary>
      /// Creates an ThermodynamicTemperature IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateThermodynamicTemperatureMeasureFromElement(Element element, string parameterName)
      {
         IFCData data = null;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(element, null,  parameterName, out double propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleThermodynamicTemperature(propertyValue);
            data = CreateAsThermodynamicTemperatureMeasure(propertyValue);
         }
         return data;
      }

      /// <summary>
      /// Creates an DynamicViscosity IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateDynamicViscosityMeasureFromElement(Element element, string parameterName)
      {
         IFCData data = null;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(element, null, parameterName, out double propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleDynamicViscosity(propertyValue);
            data = CreateAsDynamicViscosityMeasure(propertyValue);
         }
         return data;
      }

      /// <summary>
      /// Creates an HeatingValue IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateHeatingValueMeasureFromElement(Element element, string parameterName)
      {
         IFCData data = null;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(element, null, parameterName, out double propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleHeatingValue(propertyValue);
            data = CreateAsHeatingValueMeasure(propertyValue);
         }
         return data;
      }

      /// <summary>
      /// Creates an IsothermalMoistureCapacity IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateIsothermalMoistureCapacityMeasureFromElement(Element element, string parameterName)
      {
         IFCData data = null;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(element, null, parameterName, out double propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleIsothermalMoistureCapacity(propertyValue);
            data = CreateAsIsothermalMoistureCapacityMeasure(propertyValue);
         }
         return data;
      }

      /// <summary>
      /// Creates an PositiveLength IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreatePositiveLengthMeasureFromElement(Element element, string parameterName)
      {
         IFCData data = null;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(element, null, parameterName, out double propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleLength(propertyValue);
            data = CreateAsPositiveLengthMeasure(propertyValue);
         }
         return data;
      }

      /// <summary>
      /// Creates an Ratio IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateRatioMeasureFromElement(Element element, string parameterName, PropertyType propertyType)
      {
         IFCData data = null;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(element, null, parameterName, out double propertyValue);
         if (param != null)
         {
            data = CreateRatioMeasureDataCommon(propertyValue, propertyType);
         }
         return data;
      }

      /// <summary>
      /// Creates an MassDensity IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateMassDensityMeasureFromElement(Element element, string parameterName)
      {
         IFCData data = null;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(element, null, parameterName, out double propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleMassDensity(propertyValue);
            data = CreateAsMassDensityMeasure(propertyValue);
         }
         return data;
      }

      /// <summary>
      /// Creates an ModulusOfElasticity IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateModulusOfElasticityMeasureFromElement(Element element, string parameterName)
      {
         IFCData data = null;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(element, null, parameterName, out double propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleModulusOfElasticity(propertyValue);
            data = CreateAsModulusOfElasticityMeasure(propertyValue);
         }
         return data;
      }

      /// <summary>
      /// Creates an MoistureDiffusivity IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateMoistureDiffusivityMeasureFromElement(Element element, string parameterName)
      {
         IFCData data = null;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(element, null, parameterName, out double propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleMoistureDiffusivity(propertyValue);
            data = CreateAsMoistureDiffusivityMeasure(propertyValue);
         }
         return data;
      }

      /// <summary>
      /// Creates an IonConcentration IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateIonConcentrationMeasureFromElement(Element element, string parameterName)
      {
         IFCData data = null;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(element, null, parameterName, out double propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleIonConcentration(propertyValue);
            data = CreateAsIonConcentrationMeasure(propertyValue);
         }
         return data;
      }

      /// <summary>
      /// Creates an VaporPermeability IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateVaporPermeabilityMeasureFromElement(Element element, string parameterName)
      {
         IFCData data = null;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(element, null, parameterName, out double propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleVaporPermeability(propertyValue);
            data = CreateAsVaporPermeabilityMeasure(propertyValue);
         }
         return data;
      }

      /// <summary>
      /// Creates an ThermalExpansionCoefficient IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateThermalExpansionCoefficientMeasureFromElement(Element element, string parameterName)
      {
         IFCData data = null;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(element, null, parameterName, out double propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleThermalExpansionCoefficient(propertyValue);
            data = CreateAsThermalExpansionCoefficientMeasure(propertyValue);
         }
         return data;
      }

      /// <summary>
      /// Creates an Pressure IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreatePressureMeasureFromElement(Element element, string parameterName)
      {
         IFCData data = null;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(element, null, parameterName, out double propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScalePressure(propertyValue);
            data = CreateAsPressureMeasure(propertyValue);
         }
         return data;
      }

      /// <summary>
      /// Creates an SpecificHeatCapacity IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateSpecificHeatCapacityMeasureFromElement(Element element, string parameterName)
      {
         IFCData data = null;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(element, null, parameterName, out double propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleSpecificHeatCapacity(propertyValue);
            data = CreateAsSpecificHeatCapacityMeasure(propertyValue);
         }
         return data;
      }

      /// <summary>
      /// Creates an ThermalConductivity IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateThermalConductivityMeasureFromElement(Element element, string parameterName)
      {
         IFCData data = null;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(element, null, parameterName, out double propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleThermalConductivity(propertyValue);
            data = CreateAsThermalConductivityMeasure(propertyValue);
         }
         return data;
      }

      /// <summary>
      /// Creates an Text IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateTextFromElement(Element element, string parameterName)
      {
         IFCData data = null;
         Parameter param = ParameterUtil.GetStringValueFromElement(element, parameterName, out string propertyValue);
         if (param != null)
            data = CreateAsText(propertyValue);
         return data;
      }

      /// <summary>
      /// Creates an Boolean IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateBooleanFromElement(Element element, string parameterName)
      {
         IFCData data = null;
         Parameter param = ParameterUtil.GetIntValueFromElement(element, parameterName, out int propertyValue);
         if (param != null)
            data = CreateAsBoolean(propertyValue != 0);
         return data;
      }

      /// <summary>
      /// Creates an Label IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateLabelFromElement(Element element, string parameterName, PropertyValueType valueType, Type propertyEnumerationType)
      {
         IFCData data = null;
         if (ParameterUtil.GetStringValueFromElement(element, parameterName, out string propertyValue) != null)
         {
            if (!string.IsNullOrEmpty(propertyValue))
            {
               if (valueType == PropertyValueType.EnumeratedValue)
               {
                  propertyValue = ValidateEnumeratedValue(propertyValue, propertyEnumerationType);
                  data = IFCData.CreateEnumeration(propertyValue);
               }
               else
               {
                  data = CreateAsLabel(propertyValue);
               }
            }
         }
         return data;
      }

   }
}
