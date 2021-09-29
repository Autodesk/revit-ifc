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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;

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

         if(value.Length > IFCLimits.MAX_IFCLABEL_STR_LEN)
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
      /// <param name="value">The integer value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsCountMeasure(double value)
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
      /// Creates an IFCData object as IfcThermalTransmittanceMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsThermalTransmittanceMeasure(double value)
      {
         return CreateAsMeasure(value, "IfcThermalTransmittanceMeasure");
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
   }
}
