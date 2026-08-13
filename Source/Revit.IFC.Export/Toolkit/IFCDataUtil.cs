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

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Utility;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
      /// Creates an IFCData object as IfcURIReference.
      /// </summary>
      /// <param name="value">The URI string.</param>
      /// <returns>The IFCData object, or null if the value is null/empty.</returns>
      public static IFCData CreateAsURIReference(string value)
      {
         if (string.IsNullOrEmpty(value))
            return null;

         return IFCData.CreateStringOfType(value, "IfcURIReference");
      }

      /// <summary>
      /// Creates an IFCData object as IfcDate.
      /// Validates against the XML Schema xs:date lexical format (ISO 8601).
      /// </summary>
      /// <param name="value">The date string (e.g. "2026-05-14", "2026-05-14Z", "2026-05-14+02:00").</param>
      /// <returns>The IFCData object, or null if the value is null/empty or not a valid IfcDate format.</returns>
      public static IFCData CreateAsDate(string value)
      {
         if (string.IsNullOrEmpty(value))
            return null;

         if (!IsValidIfcDate(value))
            return null;

         return IFCData.CreateStringOfType(value, "IfcDate");
      }

      /// <summary>
      /// Validates a string against the XML Schema Part 2 xs:date lexical format.
      /// Format: '-'? yyyy '-' mm '-' dd zzzzzz?
      /// where zzzzzz is 'Z' or [+-]hh:mm.
      /// </summary>
      private static bool IsValidIfcDate(string value)
      {
         int i = 0;

         if (!ParseDatePortion(value, ref i))
            return false;

         if (i < value.Length && !IsValidTimeZoneSuffix(value, ref i))
            return false;

         return i == value.Length;
      }

      /// <summary>
      /// Validates a string against the XML Schema Part 2 xs:dateTime lexical format.
      /// Format: '-'? yyyy '-' mm '-' dd 'T' hh ':' mm ':' ss ('.' s+)? zzzzzz?
      /// </summary>
      private static bool IsValidIfcDateTime(string value)
      {
         int i = 0;
         int len = value.Length;

         if (!ParseDatePortion(value, ref i))
            return false;

         if (i >= len || value[i] != 'T')
            return false;
         i++;

         // hh: 00-24 (24 valid only for 24:00:00 per spec, but we don't enforce that constraint)
         if (!TryParseTwoDigitInt(value, ref i, 0, 24))
            return false;

         if (i >= len || value[i] != ':')
            return false;
         i++;

         // mm: 00-59
         if (!TryParseTwoDigitInt(value, ref i, 0, 59))
            return false;

         if (i >= len || value[i] != ':')
            return false;
         i++;

         // ss: 00-59
         if (!TryParseTwoDigitInt(value, ref i, 0, 59))
            return false;

         // Optional fractional seconds: '.' followed by one or more digits
         if (i < len && value[i] == '.')
         {
            i++;
            int fracStart = i;
            while (i < len && value[i] >= '0' && value[i] <= '9')
               i++;
            if (i == fracStart)
               return false;
         }

         if (i < len && !IsValidTimeZoneSuffix(value, ref i))
            return false;

         return i == len;
      }

      /// <summary>
      /// Parses the date portion: '-'? yyyy '-' mm '-' dd.
      /// Shared by IsValidIfcDate and IsValidIfcDateTime.
      /// </summary>
      private static bool ParseDatePortion(string value, ref int i)
      {
         int len = value.Length;

         if (i < len && value[i] == '-')
            i++;

         int yearStart = i;
         while (i < len && value[i] >= '0' && value[i] <= '9')
            i++;
         if (i - yearStart < 4)
            return false;

         if (i >= len || value[i] != '-')
            return false;
         i++;

         if (!TryParseTwoDigitInt(value, ref i, 1, 12))
            return false;

         if (i >= len || value[i] != '-')
            return false;
         i++;

         if (!TryParseTwoDigitInt(value, ref i, 1, 31))
            return false;

         return true;
      }

      /// <summary>
      /// Parses exactly two ASCII digits at position i, validates the value
      /// is within [min, max], and advances i by 2. Returns false on failure.
      /// </summary>
      private static bool TryParseTwoDigitInt(string value, ref int i, int min, int max)
      {
         if (i + 2 > value.Length)
            return false;

         int d1 = value[i] - '0';
         int d2 = value[i + 1] - '0';
         if (d1 < 0 || d1 > 9 || d2 < 0 || d2 > 9)
            return false;

         int result = d1 * 10 + d2;
         if (result < min || result > max)
            return false;

         i += 2;
         return true;
      }

      /// <summary>
      /// Validates an optional timezone suffix: 'Z' or [+-]hh:mm (minutes 00-59).
      /// Advances i past the suffix. Returns false if the suffix is present but malformed.
      /// </summary>
      private static bool IsValidTimeZoneSuffix(string value, ref int i)
      {
         if (value[i] == 'Z')
         {
            i++;
            return true;
         }

         if (value[i] == '+' || value[i] == '-')
         {
            i++;
            if (!TryParseTwoDigitInt(value, ref i, 0, 14))
               return false;
            if (i >= value.Length || value[i] != ':')
               return false;
            i++;
            if (!TryParseTwoDigitInt(value, ref i, 0, 59))
               return false;
            return true;
         }

         return false;
      }

      /// <summary>
      /// Validates a string against the XML Schema Part 2 xs:duration lexical format (ISO 8601).
      /// Format: '-'? 'P' (nY)? (nM)? (nD)? ('T' (nH)? (nM)? (n('.'n+)?S)?)?
      /// At least one component must be present; if 'T' is present, at least one time component must follow.
      /// </summary>
      private static bool IsValidIfcDuration(string value)
      {
         int i = 0;
         int len = value.Length;

         if (i < len && value[i] == '-')
            i++;

         if (i >= len || value[i] != 'P')
            return false;
         i++;

         bool hasAnyComponent = false;

         if (TryParseDurationComponent(value, ref i, 'Y'))
            hasAnyComponent = true;

         if (TryParseDurationComponent(value, ref i, 'M'))
            hasAnyComponent = true;

         if (TryParseDurationComponent(value, ref i, 'D'))
            hasAnyComponent = true;

         if (i < len && value[i] == 'T')
         {
            i++;
            bool hasTimeComponent = false;

            if (TryParseDurationComponent(value, ref i, 'H'))
               hasTimeComponent = true;

            if (TryParseDurationComponent(value, ref i, 'M'))
               hasTimeComponent = true;

            if (TryParseDurationComponent(value, ref i, 'S', allowFraction: true))
               hasTimeComponent = true;

            if (!hasTimeComponent)
               return false;

            hasAnyComponent = true;
         }

         return hasAnyComponent && i == len;
      }

      /// <summary>
      /// Tries to parse a duration component: one or more digits, an optional fractional part
      /// (if allowFraction is true), followed by the specified designator character.
      /// Restores the position if the component is not found.
      /// </summary>
      private static bool TryParseDurationComponent(string value, ref int i, char designator, bool allowFraction = false)
      {
         int len = value.Length;
         int saved = i;

         int digitStart = i;
         while (i < len && value[i] >= '0' && value[i] <= '9')
            i++;
         if (i == digitStart)
            return false;

         if (allowFraction && i < len && value[i] == '.')
         {
            i++;
            int fracStart = i;
            while (i < len && value[i] >= '0' && value[i] <= '9')
               i++;
            if (i == fracStart)
            {
               i = saved;
               return false;
            }
         }

         if (i >= len || value[i] != designator)
         {
            i = saved;
            return false;
         }
         i++;
         return true;
      }

      /// <summary>
      /// Creates an IFCData object as IfcDateTime.
      /// Validates against the XML Schema xs:dateTime lexical format (ISO 8601).
      /// </summary>
      /// <param name="value">The date-time string (e.g. "2026-05-15T19:59:00", "2026-05-15T19:59:00.123Z").</param>
      /// <returns>The IFCData object, or null if the value is null/empty or not a valid IfcDateTime format.</returns>
      public static IFCData CreateAsDateTime(string value)
      {
         if (string.IsNullOrEmpty(value))
            return null;

         if (!IsValidIfcDateTime(value))
            return null;

         return IFCData.CreateStringOfType(value, "IfcDateTime");
      }

      /// <summary>
      /// Creates an IFCData object as IfcDuration.
      /// Validates against the XML Schema Part 2 xs:duration lexical format (ISO 8601).
      /// </summary>
      /// <param name="value">The duration string (e.g. "P1Y2M3DT4H5M6S").</param>
      /// <returns>The IFCData object, or null if the value is null/empty or not a valid IfcDuration format.</returns>
      public static IFCData CreateAsDuration(string value)
      {
         if (string.IsNullOrEmpty(value))
            return null;

         if (!IsValidIfcDuration(value))
            return null;

         return IFCData.CreateStringOfType(value, "IfcDuration");
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
         return CreateAsMeasureWithUnit(value, "IfcReal");
      }

      /// <summary>
      /// Creates an IFCData object as IfcNumericMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsNumeric(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcNumericMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcRatioMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsRatioMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcRatioMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcNormalisedRatioMeasure.
      /// Returns null if the value is outside the valid IFC range [0.0, 1.0] (WR1).
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object, or null if the value violates the WR1 constraint.</returns>
      public static IFCData CreateAsNormalisedRatioMeasure(double value)
      {
         if (value < -MathUtil.Eps || value > 1.0 + MathUtil.Eps)
            return null;

         return CreateAsMeasureWithUnit(Math.Clamp(value, 0.0, 1.0), "IfcNormalisedRatioMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcSpecularExponent.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsSpecularExponent(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcSpecularExponent");
      }

      /// <summary>
      /// Creates an IFCData object as IfcPositiveRatioMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object, or null if the value violates the WR1 constraint.</returns>
      public static IFCData CreateAsPositiveRatioMeasure(double value)
      {
         if (value < MathUtil.Eps)
            return null;

         return CreateAsMeasureWithUnit(value, "IfcPositiveRatioMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcLengthMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsLengthMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcLengthMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcVolumeMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsVolumeMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcVolumeMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcNonNegativeLengthMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsNonNegativeLengthMeasure(double value)
      {
         if (value > -MathUtil.Eps)
            return CreateAsMeasureWithUnit(Math.Max(value, 0.0), "IfcNonNegativeLengthMeasure");
         else
            return null;
      }

      /// <summary>
      /// Creates an IFCData object as IfcPositiveLengthMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsPositiveLengthMeasure(double value)
      {
         if (value > MathUtil.Eps)
            return CreateAsMeasureWithUnit(value, "IfcPositiveLengthMeasure");
         else
            return null;
      }

      /// <summary>
      /// Creates an IFCData object as IfcPositivePlaneAngleMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsPositivePlaneAngleMeasure(double value)
      {
         if (value > MathUtil.Eps)
            return CreateAsMeasureWithUnit(value, "IfcPositivePlaneAngleMeasure");
         else
            return null;
      }

      /// <summary>
      /// Creates an IFCData object as IfcPlaneAngleMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsPlaneAngleMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcPlaneAngleMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcAreaMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsAreaMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcAreaMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcAccelerationMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsAccelerationMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcAccelerationMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcEnergyMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsEnergyMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcEnergyMeasure");
      }


      /// <summary>
      /// Creates corresponding ifc unit for a measure name
      /// </summary>
      public static void CreateCorrespondingUnit(string measureName)
      {
         ForgeTypeId specType = UnitMappingUtil.GetUnitSpecTypeFromString(measureName);
         UnitMappingUtil.GetOrCreateUnitInfo(specType);
      }

      /// <summary>
      /// Creates an IFCData object as an IfcMeasure of the right type
      /// and creates the corresponding Revit unit.
      /// </summary>
      /// <param name="value">The int value.</param>
      /// <param name="measureName">The type of IfcMeasure (e.g. IfcForceMeasure).</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsMeasureWithUnit(int value, string measureName)
      {
         CreateCorrespondingUnit(measureName);
         return CreateAsMeasure(value, measureName);
      }

      /// <summary>
      /// Creates an IFCData object as an IfcMeasure of the right type
      /// and creates the corresponding Revit unit.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <param name="measureName">The type of IfcMeasure (e.g. IfcForceMeasure).</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsMeasureWithUnit(double value, string measureName)
      {
         CreateCorrespondingUnit(measureName);
         return CreateAsMeasure(value, measureName);
      }

      /// <summary>
      /// Creates an IFCData object as IfcLinearMomentMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsLinearMomentMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcLinearMomentMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcMassPerLengthMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsMassPerLengthMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcMassPerLengthMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcTorqueMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsTorqueMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcTorqueMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcLinearStiffnessMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsLinearStiffnessMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcLinearStiffnessMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcAngularVelocityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsAngularVelocityMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcAngularVelocityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcThermalResistanceMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsThermalResistanceMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcThermalResistanceMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcWarpingConstantMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsWarpingConstantMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcWarpingConstantMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcLinearVelocityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsLinearVelocityMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcLinearVelocityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcCountMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsCountMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcCountMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcCountMeasure. Since IFC4x3 the Count measure value has been changed to Integer
      /// </summary>
      /// <param name="value">The integer value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsCountMeasure(int value)
      {
         return CreateAsMeasureWithUnit(value, "IfcCountMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcParameterValue.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsParameterValue(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcParameterValue");
      }

      /// <summary>
      /// Creates an IFCData object as IfcPowerMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsPowerMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcPowerMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcSoundPowerMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsSoundPowerMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcSoundPowerMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcSoundPowerLevelMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsSoundPowerLevelMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcSoundPowerLevelMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcSoundPressureMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsSoundPressureMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcSoundPressureMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcFrequencyMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsFrequencyMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcFrequencyMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcElectricCurrentMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsElectricCurrentMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcElectricCurrentMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcElectricVoltageMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsElectricVoltageMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcElectricVoltageMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcThermodynamicTemperatureMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsThermodynamicTemperatureMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcThermodynamicTemperatureMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcDynamicViscosityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsDynamicViscosityMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcDynamicViscosityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcIsothermalMoistureCapacityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsIsothermalMoistureCapacityMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcIsothermalMoistureCapacityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcMassDensityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsMassDensityMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcMassDensityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcModulusOfElasticityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsModulusOfElasticityMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcModulusOfElasticityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcVaporPermeabilityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsVaporPermeabilityMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcVaporPermeabilityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcThermalExpansionCoefficientMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsThermalExpansionCoefficientMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcThermalExpansionCoefficientMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcPressureMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsPressureMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcPressureMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcMonetaryMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsMonetaryMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcMonetaryMeasure");
      }
      
      /// <summary>
      /// Creates an IFCData object as IfcSpecificHeatCapacityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsSpecificHeatCapacityMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcSpecificHeatCapacityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcHeatingValueMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object, or null if the value violates the WR1 constraint.</returns>
      public static IFCData CreateAsHeatingValueMeasure(double value)
      {
         if (value < MathUtil.Eps)
            return null;

         return CreateAsMeasureWithUnit(value, "IfcHeatingValueMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcMoistureDiffusivityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsMoistureDiffusivityMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcMoistureDiffusivityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcIonConcentrationMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsIonConcentrationMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcIonConcentrationMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcMomentOfInertiaMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsMomentOfInertiaMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcMomentOfInertiaMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcSectionModulusMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsSectionModulusMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcSectionModulusMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcHeatFluxDensityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsHeatFluxDensityMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcHeatFluxDensityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcAreaDensityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsAreaDensityMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcAreaDensityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcThermalConductivityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsThermalConductivityMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcThermalConductivityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcRotationalFrequencyMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsRotationalFrequencyMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcRotationalFrequencyMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcMassFlowRateMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsMassFlowRateMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcMassFlowRateMeasure");
      }
      

      /// <summary>
      /// Creates an IFCData object as IfcThermalTransmittanceMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsThermalTransmittanceMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcThermalTransmittanceMeasure");
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
                  ratioData = CreateAsPositiveRatioMeasure(value);
                  break;
               }
            case PropertyType.NormalisedRatio:
               {

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
         return CreateAsMeasureWithUnit(value, "IfcVolumetricFlowRateMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcIlluminanceMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsIlluminanceMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcIlluminanceMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcLuminousFluxMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsLuminousFluxMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcLuminousFluxMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcLuminousIntensityMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsLuminousIntensityMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcLuminousIntensityMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcForceMeasure.
      /// </summary>
      /// <param name="value">The double value.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateAsForceMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcForceMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcLinearForceMeasure
      /// </summary>
      /// <param name="value">the double value</param>
      /// <returns>the IFCData object</returns>
      public static IFCData CreateAsLinearForceMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcLinearForceMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcMassMeasure
      /// </summary>
      /// <param name="value">the double value</param>
      /// <returns>the IFCData object</returns>
      public static IFCData CreateAsMassMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcMassMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcTimeMeasure
      /// </summary>
      /// <param name="value">the double value</param>
      /// <returns>the IFCData object</returns>
      public static IFCData CreateAsTimeMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcTimeMeasure");
      }

      /// <summary>
      /// Creates an IFCData object as IfcPlanarForceMeasure
      /// </summary>
      /// <param name="value">the double value</param>
      /// <returns>the IFCData object</returns>
      public static IFCData CreateAsPlanarForceMeasure(double value)
      {
         return CreateAsMeasureWithUnit(value, "IfcPlanarForceMeasure");
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

      private static Dictionary<Type, Dictionary<NamingUtil.IFCStringKey, string>> EnumStrings = [];

      public static string ValidateEnumeratedValue(string value, Type propertyEnumerationType)
      {
         if (propertyEnumerationType == null || !propertyEnumerationType.IsEnum || string.IsNullOrEmpty(value))
            return null;

         ref Dictionary<NamingUtil.IFCStringKey, string> enumStrings = 
            ref CollectionsMarshal.GetValueRefOrAddDefault(EnumStrings, propertyEnumerationType, out bool exists);
         if (!exists)
         {
            enumStrings = new();
            foreach (object enumeratedValue in Enum.GetValues(propertyEnumerationType))
            {
               string originalString = enumeratedValue.ToString();
               NamingUtil.IFCStringKey keyString = new(originalString);
               enumStrings[keyString] = originalString;
            }
         }

         NamingUtil.IFCStringKey compValue = new(value);
         if (enumStrings.TryGetValue(compValue, out string enumValue))
            return enumValue;

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
         (EvaluatedParameter param, double propertyValue) = ParameterUtil.GetDoubleValueFromElement(element, parameterName);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleThermodynamicTemperature(propertyValue);
            return CreateAsThermodynamicTemperatureMeasure(propertyValue);
         }
         return null;
      }

      /// <summary>
      /// Creates an DynamicViscosity IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateDynamicViscosityMeasureFromElement(Element element, string parameterName)
      {
         (EvaluatedParameter param, double propertyValue) = ParameterUtil.GetDoubleValueFromElement(element, parameterName);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleDynamicViscosity(propertyValue);
            return CreateAsDynamicViscosityMeasure(propertyValue);
         }
         return null;
      }

      /// <summary>
      /// Creates an HeatingValue IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateHeatingValueMeasureFromElement(Element element, string parameterName)
      {
         (EvaluatedParameter param, double propertyValue) = ParameterUtil.GetDoubleValueFromElement(element, parameterName);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleHeatingValue(propertyValue);
            return CreateAsHeatingValueMeasure(propertyValue);
         }
         return null;
      }

      /// <summary>
      /// Creates an IsothermalMoistureCapacity IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateIsothermalMoistureCapacityMeasureFromElement(Element element, string parameterName)
      {
         (EvaluatedParameter param, double propertyValue) = ParameterUtil.GetDoubleValueFromElement(element, parameterName);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleIsothermalMoistureCapacity(propertyValue);
            return CreateAsIsothermalMoistureCapacityMeasure(propertyValue);
         }
         return null;
      }

      /// <summary>
      /// Creates an PositiveLength IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreatePositiveLengthMeasureFromElement(Element element, string parameterName)
      {
         (EvaluatedParameter param, double propertyValue) = ParameterUtil.GetDoubleValueFromElement(element, parameterName);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleLength(propertyValue);
            return CreateAsPositiveLengthMeasure(propertyValue);
         }
         return null;
      }

      /// <summary>
      /// Creates an Ratio IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateRatioMeasureFromElement(Element element, string parameterName, PropertyType propertyType)
      {
         (EvaluatedParameter param, double propertyValue) = ParameterUtil.GetDoubleValueFromElement(element, parameterName);
         if (param != null)
         {
            return CreateRatioMeasureDataCommon(propertyValue, propertyType);
         }
         return null;
      }

      /// <summary>
      /// Creates an MassDensity IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateMassDensityMeasureFromElement(Element element, string parameterName)
      {
         (EvaluatedParameter param, double propertyValue) = ParameterUtil.GetDoubleValueFromElement(element, parameterName);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleMassDensity(propertyValue);
            return CreateAsMassDensityMeasure(propertyValue);
         }
         return null;
      }

      /// <summary>
      /// Creates an ModulusOfElasticity IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateModulusOfElasticityMeasureFromElement(Element element, string parameterName)
      {
         (EvaluatedParameter param, double propertyValue) = ParameterUtil.GetDoubleValueFromElement(element, parameterName);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleModulusOfElasticity(propertyValue);
            return CreateAsModulusOfElasticityMeasure(propertyValue);
         }
         return null;
      }

      /// <summary>
      /// Creates an MoistureDiffusivity IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateMoistureDiffusivityMeasureFromElement(Element element, string parameterName)
      {
         (EvaluatedParameter param, double propertyValue) = ParameterUtil.GetDoubleValueFromElement(element, parameterName);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleMoistureDiffusivity(propertyValue);
            return CreateAsMoistureDiffusivityMeasure(propertyValue);
         }
         return null;
      }

      /// <summary>
      /// Creates an IonConcentration IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateIonConcentrationMeasureFromElement(Element element, string parameterName)
      {
         (EvaluatedParameter param, double propertyValue) = ParameterUtil.GetDoubleValueFromElement(element, parameterName);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleIonConcentration(propertyValue);
            return CreateAsIonConcentrationMeasure(propertyValue);
         }
         return null;
      }

      /// <summary>
      /// Creates an VaporPermeability IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateVaporPermeabilityMeasureFromElement(Element element, string parameterName)
      {
         (EvaluatedParameter param, double propertyValue) = ParameterUtil.GetDoubleValueFromElement(element, parameterName);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleVaporPermeability(propertyValue);
            return CreateAsVaporPermeabilityMeasure(propertyValue);
         }
         return null;
      }

      /// <summary>
      /// Creates an ThermalExpansionCoefficient IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateThermalExpansionCoefficientMeasureFromElement(Element element, string parameterName)
      {
         (EvaluatedParameter param, double propertyValue) = ParameterUtil.GetDoubleValueFromElement(element, parameterName);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleThermalExpansionCoefficient(propertyValue);
            return CreateAsThermalExpansionCoefficientMeasure(propertyValue);
         }
         return null;
      }

      /// <summary>
      /// Creates an Pressure IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreatePressureMeasureFromElement(Element element, string parameterName)
      {
         (EvaluatedParameter param, double propertyValue) = ParameterUtil.GetDoubleValueFromElement(element, parameterName);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScalePressure(propertyValue);
            return CreateAsPressureMeasure(propertyValue);
         }
         return null;
      }

      /// <summary>
      /// Creates an SpecificHeatCapacity IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateSpecificHeatCapacityMeasureFromElement(Element element, string parameterName)
      {
         (EvaluatedParameter param, double propertyValue) = ParameterUtil.GetDoubleValueFromElement(element, parameterName);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleSpecificHeatCapacity(propertyValue);
            return CreateAsSpecificHeatCapacityMeasure(propertyValue);
         }
         return null;
      }

      /// <summary>
      /// Creates an ThermalConductivity IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateThermalConductivityMeasureFromElement(Element element, string parameterName)
      {
         (EvaluatedParameter param, double propertyValue) = ParameterUtil.GetDoubleValueFromElement(element, parameterName);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleThermalConductivity(propertyValue);
            return CreateAsThermalConductivityMeasure(propertyValue);
         }
         return null;
      }

      /// <summary>
      /// Creates an Text IFCData object from element parameter by name
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <returns>The IFCData object.</returns>
      public static IFCData CreateTextFromElement(Element element, string parameterName)
      {
         (_, string propertyValue) = ParameterUtil.GetStringValueFromElement(element, false, parameterName);
         return propertyValue != null ? CreateAsText(propertyValue) : null;
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
         (EvaluatedParameter parameter, int propertyValue) = ParameterUtil.GetIntValueFromElement(element, parameterName);
         if (parameter != null)
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
         (_, string propertyValue) = ParameterUtil.GetStringValueFromElement(element, false, parameterName);
         if (string.IsNullOrEmpty(propertyValue))
            return null;

         if (valueType == PropertyValueType.EnumeratedValue)
         {
            propertyValue = ValidateEnumeratedValue(propertyValue, propertyEnumerationType);
            return IFCData.CreateEnumeration(propertyValue);
         }

         return CreateAsLabel(propertyValue);
      }
   }
}
