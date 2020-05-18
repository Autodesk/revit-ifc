﻿//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
// Copyright (C) 2013  Autodesk, Inc.
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
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Utility;
using Revit.IFC.Import.Enums;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Presents an IFC unit.
   /// </summary>
   public class IFCUnit : IFCEntity
   {
      /// <summary>
      double m_ScaleFactor = 1.0;

      double m_OffsetFactor = 0.0;

      ForgeTypeId m_SpecTypeId = new ForgeTypeId();

      // only used if Spec = SpecTypeId.Custom.
      string m_CustomSpec = null;

      ForgeTypeId m_SymbolTypeId = new ForgeTypeId();

      ForgeTypeId m_UnitTypeId = new ForgeTypeId();

      UnitSystem m_UnitSystem = UnitSystem.Metric;

      static IDictionary<string, double> m_sPrefixToScaleFactor = null;

      static IDictionary<ForgeTypeId, IDictionary<string, KeyValuePair<ForgeTypeId, ForgeTypeId>>> m_sSupportedMetricUnitTypes = null;

      /// <summary>
      /// The type of unit, such as Length.
      /// </summary>
      public ForgeTypeId Spec
      {
         get { return m_SpecTypeId; }
         protected set { m_SpecTypeId = value; }
      }

      /// <summary>
      /// The type of unit, if UnitType = UT_Custom.
      /// </summary>
      public string CustomSpec
      {
         get { return m_CustomSpec; }
         protected set { m_CustomSpec = value; }
      }

      /// <summary>
      /// The unit system, metric or imperial.
      /// </summary>
      public UnitSystem UnitSystem
      {
         get { return m_UnitSystem; }
         protected set { m_UnitSystem = value; }
      }

      /// <summary>
      /// The unit identifier, such as Meters.
      /// </summary>
      public ForgeTypeId Unit
      {
         get { return m_UnitTypeId; }
         protected set { m_UnitTypeId = value; }
      }

      /// <summary>
      /// The unit symbols, such as "m" for meters.
      /// </summary>
      public ForgeTypeId Symbol
      {
         get { return m_SymbolTypeId; }
         protected set { m_SymbolTypeId = value; }
      }

      /// <summary>
      /// The scale factor to Revit internal unit.
      /// </summary>
      public double ScaleFactor
      {
         get { return m_ScaleFactor; }
         protected set { m_ScaleFactor = value; }
      }

      /// <summary>
      /// The offset factor to Revit internal unit.
      /// </summary>
      public double OffsetFactor
      {
         get { return m_OffsetFactor; }
         protected set { m_OffsetFactor = value; }
      }

      /// <summary>
      /// Constructs a Unit object.
      /// </summary>
      protected IFCUnit()
      {
      }

      protected IFCUnit(IFCAnyHandle unit)
      {
         Process(unit);
      }

      /// <summary>
      /// Checks that the unit definition is valid for use.
      /// </summary>
      /// <param name="unit">The IFCUnit to check.</param>
      /// <returns>True if the IFCUnit is null, or has invalid parameters.</returns>
      public static bool IsNullOrInvalid(IFCUnit unit)
      {
         if (unit == null)
            return true;

         return (unit.Spec.Empty() || unit.Unit.Empty());
      }

      /// <summary>
      /// Converts the value to this unit type.
      /// </summary>
      /// <param name="inValue">The value to convert.</param>
      /// <returns>The converted value.</returns>
      public double Convert(double inValue)
      {
         return inValue * ScaleFactor - OffsetFactor;
      }

      /// <summary>
      /// Converts the value to this unit type.
      /// </summary>
      /// <param name="inValue">The value to convert.</param>
      /// <returns>The converted value.</returns>
      public int Convert(int inValue)
      {
         return (int)(inValue * ScaleFactor - OffsetFactor);
      }

      protected override void Process(IFCAnyHandle item)
      {
         base.Process(item);

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(item))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcSIUnit);
            return;
         }

         if (IFCAnyHandleUtil.IsValidSubTypeOf(item, IFCEntityType.IfcDerivedUnit))
            ProcessIFCDerivedUnit(item);
         else if (IFCAnyHandleUtil.IsValidSubTypeOf(item, IFCEntityType.IfcMeasureWithUnit))
            ProcessIFCMeasureWithUnit(item);
         else if (IFCAnyHandleUtil.IsValidSubTypeOf(item, IFCEntityType.IfcMonetaryUnit))
            ProcessIFCMonetaryUnit(item);
         else if (IFCAnyHandleUtil.IsValidSubTypeOf(item, IFCEntityType.IfcNamedUnit))
            ProcessIFCNamedUnit(item);
         else
            Importer.TheLog.LogUnhandledSubTypeError(item, "IfcUnit", true);
      }

      /// <summary>
      /// Processes a named unit.
      /// </summary>
      /// <param name="unitHnd">The unit handle.</param>
      void ProcessIFCNamedUnit(IFCAnyHandle unitHnd)
      {
         // Only called from ProcessIFCUnit, which already does a null check.

         if (IFCAnyHandleUtil.IsValidSubTypeOf(unitHnd, IFCEntityType.IfcSIUnit))
            ProcessIFCSIUnit(unitHnd);
         else if (IFCAnyHandleUtil.IsValidSubTypeOf(unitHnd, IFCEntityType.IfcConversionBasedUnit))
            ProcessIFCConversionBasedUnit(unitHnd);
         else
            Importer.TheLog.LogUnhandledSubTypeError(unitHnd, IFCEntityType.IfcNamedUnit, true);
      }

      private void InitPrefixToScaleFactor()
      {
         m_sPrefixToScaleFactor = new Dictionary<string, double>();
         m_sPrefixToScaleFactor["EXA"] = 1e+18;
         m_sPrefixToScaleFactor["PETA"] = 1e+15;
         m_sPrefixToScaleFactor["TERA"] = 1e+12;
         m_sPrefixToScaleFactor["GIGA"] = 1e+9;
         m_sPrefixToScaleFactor["MEGA"] = 1e+6;
         m_sPrefixToScaleFactor["KILO"] = 1e+3;
         m_sPrefixToScaleFactor["HECTO"] = 1e+2;
         m_sPrefixToScaleFactor["DECA"] = 1e+1;
         m_sPrefixToScaleFactor[""] = 1e+0;
         m_sPrefixToScaleFactor["DECI"] = 1e-1;
         m_sPrefixToScaleFactor["CENTI"] = 1e-2;
         m_sPrefixToScaleFactor["MILLI"] = 1e-3;
         m_sPrefixToScaleFactor["MICRO"] = 1e-6;
         m_sPrefixToScaleFactor["NANO"] = 1e-9;
         m_sPrefixToScaleFactor["PICO"] = 1e-12;
         m_sPrefixToScaleFactor["FEMTO"] = 1e-15;
         m_sPrefixToScaleFactor["ATTO"] = 1e-18;
      }

      private IDictionary<string, KeyValuePair<ForgeTypeId, ForgeTypeId>> GetSupportedDisplayTypes(ForgeTypeId specTypeId)
      {
         if (m_sSupportedMetricUnitTypes == null)
            m_sSupportedMetricUnitTypes = new Dictionary<ForgeTypeId, IDictionary<string, KeyValuePair<ForgeTypeId, ForgeTypeId>>>();

         IDictionary<string, KeyValuePair<ForgeTypeId, ForgeTypeId>> supportedTypes = null;
         if (!m_sSupportedMetricUnitTypes.TryGetValue(specTypeId, out supportedTypes))
         {
            supportedTypes = new Dictionary<string, KeyValuePair<ForgeTypeId, ForgeTypeId>>();
            if (specTypeId.Equals(SpecTypeId.Area))
            {
               supportedTypes[""] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.SquareMeters, SymbolTypeId.MSup2);
               supportedTypes["CENTI"] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.SquareCentimeters, SymbolTypeId.CmSup2);
               supportedTypes["MILLI"] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.SquareMillimeters, SymbolTypeId.MmSup2);
            }
            else if (specTypeId.Equals(SpecTypeId.Current))
            {
               supportedTypes[""] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Amperes, SymbolTypeId.Ampere);
               supportedTypes["KILO"] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Kiloamperes, SymbolTypeId.KA);
               supportedTypes["MILLI"] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Milliamperes, SymbolTypeId.MA);
            }
            else if (specTypeId.Equals(SpecTypeId.ElectricalFrequency))
            {
               supportedTypes[""] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Hertz, SymbolTypeId.Hz);
            }
            else if (specTypeId.Equals(SpecTypeId.Illuminance))
            {
               supportedTypes[""] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Lux, SymbolTypeId.Lx);
            }
            else if (specTypeId.Equals(SpecTypeId.LuminousFlux))
            {
               supportedTypes[""] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Lumens, SymbolTypeId.Lm);
            }
            else if (specTypeId.Equals(SpecTypeId.LuminousIntensity))
            {
               supportedTypes[""] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Candelas, SymbolTypeId.Cd);
            }
            else if (specTypeId.Equals(SpecTypeId.ElectricalPotential))
            {
               supportedTypes[""] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Volts, SymbolTypeId.Volt);
               supportedTypes["KILO"] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Kilovolts, SymbolTypeId.KV);
               supportedTypes["MILLI"] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Millivolts, SymbolTypeId.MV);
            }
            else if (specTypeId.Equals(SpecTypeId.Force))
            {
               supportedTypes[""] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Newtons, SymbolTypeId.Newton);    // Even if unit is grams, display kg.
               supportedTypes["KILO"] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Kilonewtons, SymbolTypeId.KN);
            }
            else if (specTypeId.Equals(SpecTypeId.HvacPower))
            {
               supportedTypes[""] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Watts, SymbolTypeId.Watt);
            }
            else if (specTypeId.Equals(SpecTypeId.HvacPressure))
            {
               supportedTypes[""] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Pascals, SymbolTypeId.Pa);
               supportedTypes["KILO"] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Kilopascals, SymbolTypeId.KPa);
               supportedTypes["MEGA"] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Megapascals, SymbolTypeId.MPa);
            }
            else if (specTypeId.Equals(SpecTypeId.Length))
            {
               supportedTypes[""] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Meters, SymbolTypeId.Meter);
               supportedTypes["CENTI"] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Centimeters, SymbolTypeId.Cm);
               supportedTypes["MILLI"] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Millimeters, SymbolTypeId.Mm);
            }
            else if (specTypeId.Equals(SpecTypeId.Mass))
            {
               supportedTypes[""] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Kilograms, SymbolTypeId.Kg);    // Even if unit is grams, display kg.
               supportedTypes["KILO"] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Kilograms, SymbolTypeId.Kg);
            }
            else if (specTypeId.Equals(SpecTypeId.MassDensity))
            {
               supportedTypes[""] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.KilogramsPerCubicMeter, SymbolTypeId.KgPerMSup3);    // Even if unit is grams, display kg.
               supportedTypes["KILO"] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.KilogramsPerCubicMeter, SymbolTypeId.KgPerMSup3);
            }
            else if (specTypeId.Equals(SpecTypeId.Volume))
            {
               supportedTypes[""] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.CubicMeters, SymbolTypeId.MSup3);
               supportedTypes["DECI"] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.Liters, SymbolTypeId.Liter);
               supportedTypes["CENTI"] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.CubicCentimeters, SymbolTypeId.CmSup3);
               supportedTypes["MILLI"] = new KeyValuePair<ForgeTypeId, ForgeTypeId>(UnitTypeId.CubicMillimeters, SymbolTypeId.MmSup3);
            }
            m_sSupportedMetricUnitTypes[specTypeId] = supportedTypes;
         }

         return supportedTypes;
      }

      private double GetScaleFactorForUnitType(string prefix, ForgeTypeId specTypeId)
      {
         double scaleFactor = m_sPrefixToScaleFactor[prefix];

         const double lengthFactor = (1.0 / 0.3048);
         const double areaFactor = lengthFactor * lengthFactor;
         const double volumeFactor = areaFactor * lengthFactor;

         // length ^ -2
         if (specTypeId.Equals(SpecTypeId.Illuminance))
         {
            return (scaleFactor * scaleFactor) / areaFactor;
         }
         // length ^ -1
         if (specTypeId.Equals(SpecTypeId.HvacPressure))
         {
            return scaleFactor / lengthFactor;
         }
         // length
         if (specTypeId.Equals(SpecTypeId.Force) ||
            specTypeId.Equals(SpecTypeId.Length))
         {
            return (scaleFactor * lengthFactor);
         }
         // length ^ 2
         if (specTypeId.Equals(SpecTypeId.Area) ||
            specTypeId.Equals(SpecTypeId.ElectricalPotential) ||
            specTypeId.Equals(SpecTypeId.HvacPower))
         {
            return (scaleFactor * scaleFactor) * areaFactor;
         }
         // length ^ 3
         if (specTypeId.Equals(SpecTypeId.Volume))
         {
            return (scaleFactor * scaleFactor * scaleFactor) * volumeFactor;
         }
         if (specTypeId.Equals(SpecTypeId.Mass))
         {
            return (scaleFactor / 1000.0);   // Standard internal scale is kg.
         }
         return scaleFactor;
      }

      /// <summary>
      /// Processes the metric prefix of a dimension.
      /// </summary>
      /// <param name="prefix">The prefix name.</param>
      /// <param name="specTypeId">The spec identifier.</param>
      /// <returns>True if the prefix is supported, false if not.</returns>
      private bool ProcessMetricPrefix(string prefix, ForgeTypeId specTypeId)
      {
         if (prefix == null)
            prefix = "";

         IDictionary<string, KeyValuePair<ForgeTypeId, ForgeTypeId>> supportedDisplayTypes = GetSupportedDisplayTypes(specTypeId);
         if (!supportedDisplayTypes.ContainsKey(prefix))
            return false;

         if (m_sPrefixToScaleFactor == null)
            InitPrefixToScaleFactor();

         if (!m_sPrefixToScaleFactor.ContainsKey(prefix))
            return false;

         KeyValuePair<ForgeTypeId, ForgeTypeId> unitNameAndSymbol = supportedDisplayTypes[prefix];
         Unit = unitNameAndSymbol.Key;
         Symbol = unitNameAndSymbol.Value;
         ScaleFactor *= GetScaleFactorForUnitType(prefix, specTypeId);
         return true;
      }

      /// <summary>
      /// A private container for ProcessIFCDerivedUnit to store expected type definitions for derived types.
      /// </summary>
      class DerivedUnitExpectedTypes
      {
         /// <summary>
         /// DerivedUnitExpectedTypes constructor.
         /// </summary>
         public DerivedUnitExpectedTypes(ForgeTypeId unitTypeId, ForgeTypeId symbolTypeId)
         {
            Unit = unitTypeId;
            Symbol = symbolTypeId;
         }

         /// <summary>
         /// The set of expected types.
         /// </summary>
         public ISet<Tuple<int, ForgeTypeId, string>> ExpectedTypes
         {
            get { return m_ExpectedTypes; }
         }

         /// <summary>
         /// The unit name of this set of expected types.
         /// </summary>
         public ForgeTypeId Unit { get; protected set; }

         /// <summary>
         /// The unit symbol type of this set of expected types.
         /// </summary>
         public ForgeTypeId Symbol { get; protected set; }

         private ISet<Tuple<int, ForgeTypeId, string>> ExpectedTypesCopy()
         {
            ISet<Tuple<int, ForgeTypeId, string>> expectedTypesCopy = new HashSet<Tuple<int, ForgeTypeId, string>>();
            foreach (Tuple<int, ForgeTypeId, string> expectedType in ExpectedTypes)
            {
               expectedTypesCopy.Add(expectedType);
            }
            return expectedTypesCopy;
         }

         /// <summary>
         /// Add a standard expected type.
         /// </summary>
         /// <param name="exponent">The exponent of the type.</param>
         /// <param name="specTypeId">The spec identifier.</param>
         public void AddExpectedType(int exponent, ForgeTypeId specTypeId)
         {
            ExpectedTypes.Add(new Tuple<int, ForgeTypeId, string>(exponent, specTypeId, null));
         }

         /// <summary>
         /// Add a custom expected type.
         /// </summary>
         /// <param name="exponent">The exponent of the type.</param>
         /// <param name="unitName">The name of the base unit.</param>
         public void AddCustomExpectedType(int exponent, string unitName)
         {
            ExpectedTypes.Add(new Tuple<int, ForgeTypeId, string>(exponent, SpecTypeId.Custom, unitName));
         }

         public bool Matches(IList<KeyValuePair<IFCUnit, int>> derivedElementUnitHnds, out double scaleFactor)
         {
            scaleFactor = 1.0;

            if (derivedElementUnitHnds.Count != ExpectedTypes.Count)
               return false;

            ISet<Tuple<int, ForgeTypeId, string>> expectedTypes = ExpectedTypesCopy();

            foreach (KeyValuePair<IFCUnit, int> derivedElementUnitHnd in derivedElementUnitHnds)
            {
               int dimensionality = derivedElementUnitHnd.Value;
               Tuple<int, ForgeTypeId, string> currKey = new Tuple<int, ForgeTypeId, string>(dimensionality, derivedElementUnitHnd.Key.Spec, derivedElementUnitHnd.Key.CustomSpec);
               if (expectedTypes.Contains(currKey))
               {
                  expectedTypes.Remove(currKey);
                  scaleFactor *= Math.Pow(derivedElementUnitHnd.Key.ScaleFactor, dimensionality);
               }
               else
                  break;
            }

            // Found all supported units.
            if (expectedTypes.Count != 0)
               return false;

            return true;
         }

         private ISet<Tuple<int, ForgeTypeId, string>> m_ExpectedTypes = new HashSet<Tuple<int, ForgeTypeId, string>>();
      }

      /// <summary>
      /// Processes an IfcDerivedUnit.
      /// </summary>
      /// <param name="unitHnd">The unit handle.</param>
      void ProcessIFCDerivedUnit(IFCAnyHandle unitHnd)
      {
         List<IFCAnyHandle> elements =
             IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(unitHnd, "Elements");

         IList<KeyValuePair<IFCUnit, int>> derivedElementUnitHnds = new List<KeyValuePair<IFCUnit, int>>();
         foreach (IFCAnyHandle subElement in elements)
         {
            IFCAnyHandle derivedElementUnitHnd = IFCImportHandleUtil.GetRequiredInstanceAttribute(subElement, "Unit", false);
            IFCUnit subUnit = IFCAnyHandleUtil.IsNullOrHasNoValue(derivedElementUnitHnd) ? null : IFCUnit.ProcessIFCUnit(derivedElementUnitHnd);
            if (subUnit != null)
            {
               bool found;
               int exponent = IFCImportHandleUtil.GetRequiredIntegerAttribute(subElement, "Exponent", out found);
               if (found)
                  derivedElementUnitHnds.Add(new KeyValuePair<IFCUnit, int>(subUnit, exponent));
            }
         }

         // the DerivedUnitExpectedTypes object is a description of one possible set of base units for a particular derived unit.
         // The IList allows for possible different interpretations.  For example, Volumetric Flow Rate could be defined by m^3/s (length ^ 3 / time) or L/s (volume / time).
         IList<DerivedUnitExpectedTypes> expectedTypesList = new List<DerivedUnitExpectedTypes>();

         string unitType = IFCAnyHandleUtil.GetEnumerationAttribute(unitHnd, "UnitType");
         if (string.Compare(unitType, "LINEARVELOCITYUNIT", true) == 0)
         {
            Spec = SpecTypeId.HvacVelocity;
            UnitSystem = UnitSystem.Metric;

            // Support only m / s.
            DerivedUnitExpectedTypes expectedTypes = new DerivedUnitExpectedTypes(UnitTypeId.MetersPerSecond, SymbolTypeId.MPerS);
            expectedTypes.AddExpectedType(1, SpecTypeId.Length);
            expectedTypes.AddCustomExpectedType(-1, "TIMEUNIT");
            expectedTypesList.Add(expectedTypes);
         }
         else if (string.Compare(unitType, "THERMALTRANSMITTANCEUNIT", true) == 0)
         {
            Spec = SpecTypeId.HeatTransferCoefficient;
            UnitSystem = UnitSystem.Metric;

            // Support W / (K * m^2) or kg / (K * s^3)
            DerivedUnitExpectedTypes expectedTypesWinvKinvM2 = new DerivedUnitExpectedTypes(UnitTypeId.WattsPerSquareMeterKelvin, SymbolTypeId.WPerMSup2K);
            expectedTypesWinvKinvM2.AddExpectedType(1, SpecTypeId.HvacPower); // UT_Electrical_Wattage is similar, but UT_HVAC_Power is the one we map to.
            expectedTypesWinvKinvM2.AddExpectedType(-1, SpecTypeId.HvacTemperature);
            expectedTypesWinvKinvM2.AddExpectedType(-2, SpecTypeId.Length);
            expectedTypesList.Add(expectedTypesWinvKinvM2);

            DerivedUnitExpectedTypes expectedTypesWinvKinvArea = new DerivedUnitExpectedTypes(UnitTypeId.WattsPerSquareMeterKelvin, SymbolTypeId.WPerMSup2K);
            expectedTypesWinvKinvArea.AddExpectedType(1, SpecTypeId.HvacPower); // UT_Electrical_Wattage is similar, but UT_HVAC_Power is the one we map to.
            expectedTypesWinvKinvArea.AddExpectedType(-1, SpecTypeId.HvacTemperature);
            expectedTypesWinvKinvArea.AddExpectedType(-1, SpecTypeId.Area);
            expectedTypesList.Add(expectedTypesWinvKinvArea);

            DerivedUnitExpectedTypes expectedTypes = new DerivedUnitExpectedTypes(UnitTypeId.WattsPerSquareMeterKelvin, SymbolTypeId.WPerMSup2K);
            expectedTypes.AddExpectedType(1, SpecTypeId.Mass);
            expectedTypes.AddExpectedType(-1, SpecTypeId.HvacTemperature);
            expectedTypes.AddCustomExpectedType(-3, "TIMEUNIT");
            expectedTypesList.Add(expectedTypes);
         }
         else if (string.Compare(unitType, "VOLUMETRICFLOWRATEUNIT", true) == 0)
         {
            Spec = SpecTypeId.AirFlow;
            UnitSystem = UnitSystem.Metric;

            // Support L / s or m^3 / s in the IFC file.

            // L / s
            DerivedUnitExpectedTypes expectedTypesLPerS = new DerivedUnitExpectedTypes(UnitTypeId.LitersPerSecond, SymbolTypeId.LPerS);
            expectedTypesLPerS.AddExpectedType(1, SpecTypeId.Volume);
            expectedTypesLPerS.AddCustomExpectedType(-1, "TIMEUNIT");
            expectedTypesList.Add(expectedTypesLPerS);

            // m^3 / s.
            DerivedUnitExpectedTypes expectedTypes = new DerivedUnitExpectedTypes(UnitTypeId.CubicMetersPerSecond, SymbolTypeId.MSup3PerS);
            expectedTypes.AddExpectedType(3, SpecTypeId.Length);
            expectedTypes.AddCustomExpectedType(-1, "TIMEUNIT");
            expectedTypesList.Add(expectedTypes);
         }
         else if (string.Compare(unitType, "MASSDENSITYUNIT", true) == 0)
         {
            Spec = SpecTypeId.MassDensity;
            UnitSystem = UnitSystem.Metric;

            // Support kg / m^3 in the IFC file.

            // kg / m^3.
            DerivedUnitExpectedTypes expectedTypes = new DerivedUnitExpectedTypes(UnitTypeId.KilogramsPerCubicMeter, SymbolTypeId.KgPerMSup3);
            expectedTypes.AddExpectedType(1, SpecTypeId.Mass);
            expectedTypes.AddExpectedType(-3, SpecTypeId.Length);
            expectedTypesList.Add(expectedTypes);
         }
         else if (string.Compare(unitType, "LINEARFORCEUNIT", true) == 0)
         {
            Spec = SpecTypeId.LinearForce;
            UnitSystem = UnitSystem.Metric;

            // Support N / m in the IFC file.
            DerivedUnitExpectedTypes expectedTypes = new DerivedUnitExpectedTypes(UnitTypeId.NewtonsPerMeter, SymbolTypeId.NPerM);
            expectedTypes.AddExpectedType(1, SpecTypeId.LinearForce);
            expectedTypesList.Add(expectedTypes);

            // Support N / m in basic units
            DerivedUnitExpectedTypes expectedTypes2 = new DerivedUnitExpectedTypes(UnitTypeId.NewtonsPerMeter, SymbolTypeId.NPerM);
            expectedTypes2.AddExpectedType(1, SpecTypeId.Mass);
            expectedTypes2.AddExpectedType(1, SpecTypeId.Length);
            expectedTypes2.AddCustomExpectedType(-2, "TIMEUNIT");
            expectedTypes2.AddExpectedType(-1, SpecTypeId.Length);
            expectedTypesList.Add(expectedTypes2);
         }
         else if (string.Compare(unitType, "PLANARFORCEUNIT", true) == 0)
         {
            Spec = SpecTypeId.AreaForce;
            UnitSystem = UnitSystem.Metric;

            // Support N / m^2 in the IFC file.
            DerivedUnitExpectedTypes expectedTypes = new DerivedUnitExpectedTypes(UnitTypeId.NewtonsPerSquareMeter, SymbolTypeId.NPerMSup2);
            expectedTypes.AddExpectedType(1, SpecTypeId.AreaForce);
            expectedTypesList.Add(expectedTypes);

            // Support N / m in basic units
            DerivedUnitExpectedTypes expectedTypes2 = new DerivedUnitExpectedTypes(UnitTypeId.NewtonsPerSquareMeter, SymbolTypeId.NPerMSup2);
            expectedTypes2.AddExpectedType(1, SpecTypeId.Mass);
            expectedTypes2.AddExpectedType(1, SpecTypeId.Length);
            expectedTypes2.AddCustomExpectedType(-2, "TIMEUNIT");
            expectedTypes2.AddExpectedType(-2, SpecTypeId.Length);
            expectedTypesList.Add(expectedTypes2);
         }
         else if (string.Compare(unitType, "USERDEFINED", true) == 0)
         {
            // Look at the sub-types to see what we support.
            string userDefinedType = IFCImportHandleUtil.GetOptionalStringAttribute(unitHnd, "UserDefinedType", null);
            if (!string.IsNullOrWhiteSpace(userDefinedType))
            {
               if (string.Compare(userDefinedType, "Luminous Efficacy", true) == 0)
               {
                  Spec = SpecTypeId.Efficacy;
                  UnitSystem = UnitSystem.Metric;

                  // Support only lm / W.
                  DerivedUnitExpectedTypes expectedTypes = new DerivedUnitExpectedTypes(UnitTypeId.LumensPerWatt, SymbolTypeId.LmPerW);
                  expectedTypes.AddExpectedType(-1, SpecTypeId.Mass);
                  expectedTypes.AddExpectedType(-2, SpecTypeId.Length);
                  expectedTypes.AddCustomExpectedType(3, "TIMEUNIT");
                  expectedTypes.AddExpectedType(1, SpecTypeId.LuminousFlux);
                  expectedTypesList.Add(expectedTypes);
               }
               else if (string.Compare(userDefinedType, "Friction Loss", true) == 0)
               {
                  Spec = SpecTypeId.HvacFriction;
                  UnitSystem = UnitSystem.Metric;

                  // Support only Pa / m.
                  DerivedUnitExpectedTypes expectedTypes = new DerivedUnitExpectedTypes(UnitTypeId.PascalsPerMeter, SymbolTypeId.PaPerM);
                  expectedTypes.AddExpectedType(-2, SpecTypeId.Length);
                  expectedTypes.AddExpectedType(1, SpecTypeId.Mass);
                  expectedTypes.AddCustomExpectedType(-2, "TIMEUNIT");
                  expectedTypesList.Add(expectedTypes);
               }
            }
         }

         foreach (DerivedUnitExpectedTypes derivedUnitExpectedTypes in expectedTypesList)
         {
            double scaleFactor = 1.0;
            if (derivedUnitExpectedTypes.Matches(derivedElementUnitHnds, out scaleFactor))
            {
               // Found a match.
               Unit = derivedUnitExpectedTypes.Unit;
               Symbol = derivedUnitExpectedTypes.Symbol;
               ScaleFactor = scaleFactor;
               return;
            }
         }

         Importer.TheLog.LogUnhandledUnitTypeError(unitHnd, unitType);
      }

      /// <summary>
      /// Processes an SI unit.
      /// </summary>
      /// <param name="unitHnd">The unit handle.</param>
      void ProcessIFCSIUnit(IFCAnyHandle unitHnd)
      {
         UnitSystem = UnitSystem.Metric;

         string unitType = IFCAnyHandleUtil.GetEnumerationAttribute(unitHnd, "UnitType");
         string unitName = IFCAnyHandleUtil.GetEnumerationAttribute(unitHnd, "Name");
         string prefix = IFCAnyHandleUtil.GetEnumerationAttribute(unitHnd, "Prefix");
         bool unitNameSupported = true;

         if (string.Compare(unitType, "AREAUNIT", true) == 0)
         {
            Spec = SpecTypeId.Area;
            unitNameSupported = (string.Compare(unitName, "SQUARE_METRE", true) == 0) && ProcessMetricPrefix(prefix, Spec);
         }
         else if (string.Compare(unitType, "ELECTRICCURRENTUNIT", true) == 0)
         {
            Spec = SpecTypeId.Current;
            unitNameSupported = ProcessMetricPrefix(prefix, Spec);
         }
         else if (string.Compare(unitType, "ELECTRICVOLTAGEUNIT", true) == 0)
         {
            Spec = SpecTypeId.ElectricalPotential;
            unitNameSupported = ProcessMetricPrefix(prefix, Spec);
         }
         else if (string.Compare(unitType, "FORCEUNIT", true) == 0)
         {
            Spec = SpecTypeId.Force;
            unitNameSupported = ProcessMetricPrefix(prefix, Spec);
         }
         else if (string.Compare(unitType, "FREQUENCYUNIT", true) == 0)
         {
            Spec = SpecTypeId.ElectricalFrequency;
            unitNameSupported = ProcessMetricPrefix(prefix, Spec);
         }
         else if (string.Compare(unitType, "ILLUMINANCEUNIT", true) == 0)
         {
            Spec = SpecTypeId.Illuminance;
            unitNameSupported = ProcessMetricPrefix(prefix, Spec);
         }
         else if (string.Compare(unitType, "LENGTHUNIT", true) == 0)
         {
            Spec = SpecTypeId.Length;
            unitNameSupported = (string.Compare(unitName, "METRE", true) == 0) && ProcessMetricPrefix(prefix, SpecTypeId.Length);
         }
         else if (string.Compare(unitType, "LUMINOUSFLUXUNIT", true) == 0)
         {
            Spec = SpecTypeId.LuminousFlux;
            unitNameSupported = ProcessMetricPrefix(prefix, Spec);
         }
         else if (string.Compare(unitType, "LUMINOUSINTENSITYUNIT", true) == 0)
         {
            Spec = SpecTypeId.LuminousIntensity;
            unitNameSupported = ProcessMetricPrefix(prefix, Spec);
         }
         else if (string.Compare(unitType, "MASSUNIT", true) == 0)
         {
            Spec = SpecTypeId.Mass;
            unitNameSupported = ProcessMetricPrefix(prefix, Spec);
         }
         else if (string.Compare(unitType, "PLANEANGLEUNIT", true) == 0)
         {
            Spec = SpecTypeId.Angle;
            Unit = UnitTypeId.Radians;
            unitNameSupported = (string.Compare(unitName, "RADIAN", true) == 0) && (string.IsNullOrWhiteSpace(prefix));
         }
         else if (string.Compare(unitType, "POWERUNIT", true) == 0)
         {
            Spec = SpecTypeId.HvacPower;
            unitNameSupported = ProcessMetricPrefix(prefix, Spec);
         }
         else if (string.Compare(unitType, "PRESSUREUNIT", true) == 0)
         {
            Spec = SpecTypeId.HvacPressure;
            unitNameSupported = ProcessMetricPrefix(prefix, Spec);
         }
         else if (string.Compare(unitType, "SOLIDANGLEUNIT", true) == 0)
         {
            // Will warn if not steridians.
            Spec = SpecTypeId.Custom;
            CustomSpec = unitType;
            unitNameSupported = (string.Compare(unitName, "STERADIAN", true) == 0) && (string.IsNullOrWhiteSpace(prefix));
         }
         else if (string.Compare(unitType, "THERMODYNAMICTEMPERATUREUNIT", true) == 0)
         {
            Spec = SpecTypeId.HvacTemperature;
            if (string.Compare(unitName, "DEGREE_CELSIUS", true) == 0 ||
                string.Compare(unitName, "CELSIUS", true) == 0)
            {
               Unit = UnitTypeId.Celsius;
               Symbol = SymbolTypeId.DegreeC;
               OffsetFactor = -273.15;
            }
            else if (string.Compare(unitName, "KELVIN", true) == 0 ||
                string.Compare(unitName, "DEGREE_KELVIN", true) == 0)
            {
               Unit = UnitTypeId.Kelvin;
               Symbol = SymbolTypeId.Kelvin;
            }
            else if (string.Compare(unitName, "FAHRENHEIT", true) == 0 ||
                string.Compare(unitName, "DEGREE_FAHRENHEIT", true) == 0)
            {
               UnitSystem = UnitSystem.Imperial;
               Unit = UnitTypeId.Fahrenheit;
               Symbol = SymbolTypeId.DegreeF;
               ScaleFactor = 5.0 / 9.0;
               OffsetFactor = (5.0 / 9.0) * 32 - 273.15;
            }
            else
               unitNameSupported = false;
         }
         else if (string.Compare(unitType, "TIMEUNIT", true) == 0)
         {
            // Will warn if not seconds.
            Spec = SpecTypeId.Custom;
            CustomSpec = unitType;
            unitNameSupported = (string.Compare(unitName, "SECOND", true) == 0) && (string.IsNullOrWhiteSpace(prefix));
         }
         else if (string.Compare(unitType, "VOLUMEUNIT", true) == 0)
         {
            Spec = SpecTypeId.Volume;
            unitNameSupported = (string.Compare(unitName, "CUBIC_METRE", true) == 0) && ProcessMetricPrefix(prefix, SpecTypeId.Volume);
         }
         else
         {
            Importer.TheLog.LogUnhandledUnitTypeError(unitHnd, unitType);
         }

         if (unitName != null && !unitNameSupported)
         {
            if (prefix != null)
               Importer.TheLog.LogError(unitHnd.StepId, "Unhandled type of " + unitType + ": " + prefix + unitName, false);
            else
               Importer.TheLog.LogError(unitHnd.StepId, "Unhandled type of " + unitType + ": " + unitName, false);
         }
      }

      // Note: the ScaleFactor will be likely overwritten.
      void CopyUnit(IFCUnit unit)
      {
         Spec = unit.Spec;
         Unit = unit.Unit;
         UnitSystem = unit.UnitSystem;
         Symbol = unit.Symbol;
         ScaleFactor = unit.ScaleFactor;
         OffsetFactor = unit.OffsetFactor;
      }

      /// <summary>
      /// Processes measure with unit.
      /// </summary>
      /// <param name="measureUnitHnd">The measure unit handle.</param>
      void ProcessIFCMeasureWithUnit(IFCAnyHandle measureUnitHnd)
      {
         double baseScale = 0.0;

         IFCData ifcData = measureUnitHnd.GetAttribute("ValueComponent");
         if (!ifcData.HasValue)
            throw new InvalidOperationException("#" + measureUnitHnd.StepId + ": Missing required attribute ValueComponent.");

         if (ifcData.PrimitiveType == IFCDataPrimitiveType.Double)
            baseScale = ifcData.AsDouble();
         else if (ifcData.PrimitiveType == IFCDataPrimitiveType.Integer)
            baseScale = (double)ifcData.AsInteger();

         if (MathUtil.IsAlmostZero(baseScale))
            throw new InvalidOperationException("#" + measureUnitHnd.StepId + ": ValueComponent should not be almost zero.");

         IFCAnyHandle unitHnd = IFCImportHandleUtil.GetRequiredInstanceAttribute(measureUnitHnd, "UnitComponent", true);

         IFCUnit unit = ProcessIFCUnit(unitHnd);
         CopyUnit(unit);
         ScaleFactor = unit.ScaleFactor * baseScale;
      }

      /// <summary>
      /// Processes monetary unit.
      /// </summary>
      /// <param name="monetaryUnitHnd">The monetary unit handle.</param>
      void ProcessIFCMonetaryUnit(IFCAnyHandle monetaryUnitHnd)
      {
         string currencyType = (IFCImportFile.TheFile.SchemaVersion < IFCSchemaVersion.IFC4) ?
            IFCAnyHandleUtil.GetEnumerationAttribute(monetaryUnitHnd, "Currency") :
            IFCImportHandleUtil.GetOptionalStringAttribute(monetaryUnitHnd, "Currency", string.Empty);

         Spec = SpecTypeId.Currency;
         Unit = UnitTypeId.Currency;

         Symbol = new ForgeTypeId();
         if ((string.Compare(currencyType, "CAD", true) == 0) ||
             (string.Compare(currencyType, "USD", true) == 0) ||
             (string.Compare(currencyType, "$", true) == 0))
            Symbol = SymbolTypeId.UsDollar;
         else if ((string.Compare(currencyType, "EUR", true) == 0) ||
            (string.Compare(currencyType, "€", true) == 0))
            Symbol = SymbolTypeId.EuroPrefix;
         else if ((string.Compare(currencyType, "GBP", true) == 0) ||
            (string.Compare(currencyType, "£", true) == 0))
            Symbol = SymbolTypeId.UkPound;
         else if (string.Compare(currencyType, "HKD", true) == 0)
            Symbol = SymbolTypeId.ChineseHongKongDollar;
         else if ((string.Compare(currencyType, "ICK", true) == 0) ||
             (string.Compare(currencyType, "NOK", true) == 0) ||
             (string.Compare(currencyType, "SEK", true) == 0))
            Symbol = SymbolTypeId.Krone;
         else if (string.Compare(currencyType, "ILS", true) == 0)
            Symbol = SymbolTypeId.Shekel;
         else if ((string.Compare(currencyType, "JPY", true) == 0) ||
             (string.Compare(currencyType, "¥", true) == 0))
            Symbol = SymbolTypeId.Yen;
         else if (string.Compare(currencyType, "KRW", true) == 0)
            Symbol = SymbolTypeId.Won;
         else if ((string.Compare(currencyType, "THB", true) == 0) ||
             (string.Compare(currencyType, "฿", true) == 0))
            Symbol = SymbolTypeId.Baht;
         else if (string.Compare(currencyType, "VND", true) == 0)
            Symbol = SymbolTypeId.Dong;
         else
            Importer.TheLog.LogWarning(Id, "Unhandled type of currency: " + currencyType, true);
      }

      /// <summary>
      /// Processes a conversion based unit.
      /// </summary>
      /// <param name="convUnitHnd">The unit handle.</param>
      void ProcessIFCConversionBasedUnit(IFCAnyHandle convUnitHnd)
      {
         IFCAnyHandle measureWithUnitHnd = IFCAnyHandleUtil.GetInstanceAttribute(convUnitHnd, "ConversionFactor");
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(measureWithUnitHnd))
            throw new InvalidOperationException("#" + convUnitHnd.StepId + ": Missing required attribute ConversionFactor.");

         IFCUnit measureWithUnit = IFCUnit.ProcessIFCUnit(measureWithUnitHnd);
         if (measureWithUnit == null)
            throw new InvalidOperationException("#" + convUnitHnd.StepId + ": Invalid base ConversionFactor, aborting.");

         CopyUnit(measureWithUnit);

         // For some common cases, get the units correct.
         string unitType = IFCAnyHandleUtil.GetEnumerationAttribute(convUnitHnd, "UnitType");
         if (string.Compare(unitType, "LENGTHUNIT", true) == 0)
         {
            Spec = SpecTypeId.Length;
            string name = IFCAnyHandleUtil.GetStringAttribute(convUnitHnd, "Name");

            if (string.Compare(name, "FOOT", true) == 0 ||
               string.Compare(name, "FEET", true) == 0)
            {
               UnitSystem = UnitSystem.Imperial;
               Unit = UnitTypeId.FeetFractionalInches;
               Symbol = new ForgeTypeId();
            }
            else if (string.Compare(name, "INCH", true) == 0 ||
               string.Compare(name, "INCHES", true) == 0)
            {
               UnitSystem = UnitSystem.Imperial;
               Unit = UnitTypeId.FractionalInches;
               Symbol = new ForgeTypeId();
            }
         }
         else if (string.Compare(unitType, "PLANEANGLEUNIT", true) == 0)
         {
            Spec = SpecTypeId.Angle;
            string name = IFCAnyHandleUtil.GetStringAttribute(convUnitHnd, "Name");

            if (string.Compare(name, "GRAD", true) == 0 ||
               string.Compare(name, "GRADIAN", true) == 0 ||
                string.Compare(name, "GRADS", true) == 0 ||
               string.Compare(name, "GRADIANS", true) == 0)
            {
               UnitSystem = UnitSystem.Metric;
               Unit = UnitTypeId.Gradians;
               Symbol = SymbolTypeId.Grad;
            }
            else if (string.Compare(name, "DEGREE", true) == 0 ||
               string.Compare(name, "DEGREES", true) == 0)
            {
               UnitSystem = UnitSystem.Imperial;
               Unit = UnitTypeId.Degrees;
               Symbol = SymbolTypeId.Degree;
            }
         }
         else if (string.Compare(unitType, "AREAUNIT", true) == 0)
         {
            Spec = SpecTypeId.Area;
            string name = IFCAnyHandleUtil.GetStringAttribute(convUnitHnd, "Name");

            if (string.Compare(name, "SQUARE FOOT", true) == 0 ||
               string.Compare(name, "SQUARE_FOOT", true) == 0 ||
               string.Compare(name, "SQUARE FEET", true) == 0 ||
               string.Compare(name, "SQUARE_FEET", true) == 0)
            {
               UnitSystem = UnitSystem.Imperial;
               Unit = UnitTypeId.SquareFeet;
               Symbol = SymbolTypeId.FtSup2;
            }
         }
         else if (string.Compare(unitType, "VOLUMEUNIT", true) == 0)
         {
            Spec = SpecTypeId.Volume;
            string name = IFCAnyHandleUtil.GetStringAttribute(convUnitHnd, "Name");

            if (string.Compare(name, "CUBIC FOOT", true) == 0 ||
               string.Compare(name, "CUBIC_FOOT", true) == 0 ||
               string.Compare(name, "CUBIC FEET", true) == 0 ||
               string.Compare(name, "CUBIC_FEET", true) == 0)
            {
               UnitSystem = UnitSystem.Imperial;
               Unit = UnitTypeId.CubicFeet;
               Symbol = SymbolTypeId.FtSup3;
            }
         }
         else if (string.Compare(unitType, "THERMODYNAMICMEASUREUNIT", true) == 0)
         {
            Spec = SpecTypeId.HvacTemperature;
            string name = IFCAnyHandleUtil.GetStringAttribute(convUnitHnd, "Name");

            if ((string.Compare(name, "F", true) == 0) ||
               (string.Compare(name, "FAHRENHEIT", true) == 0))
            {
               UnitSystem = UnitSystem.Imperial;
               Unit = UnitTypeId.Fahrenheit;
               Symbol = SymbolTypeId.DegreeF;
            }
            else if ((string.Compare(name, "R", true) == 0) ||
               (string.Compare(name, "RANKINE", true) == 0))
            {
               UnitSystem = UnitSystem.Imperial;
               Unit = UnitTypeId.Rankine;
               Symbol = SymbolTypeId.DegreeR;
            }
         }
      }

      /// <summary>
      /// Processes a unit.
      /// </summary>
      /// <param name="unitHnd">The unit handle.</param>
      /// <returns>The Unit object.</returns>
      public static IFCUnit ProcessIFCUnit(IFCAnyHandle unitHnd)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(unitHnd))
         {
            //LOG: ERROR: IfcUnit is null or has no value.
            return null;
         }

         try
         {
            IFCEntity ifcUnit;
            if (!IFCImportFile.TheFile.EntityMap.TryGetValue(unitHnd.StepId, out ifcUnit))
               ifcUnit = new IFCUnit(unitHnd);
            return (ifcUnit as IFCUnit);
         }
         catch (InvalidOperationException ex)
         {
            Importer.TheLog.LogError(unitHnd.StepId, ex.Message, false);
         }

         return null;
      }

      /// <summary>
      /// Constructs a default IFCUnit of a specific type.
      /// </summary>
      /// <param name="unitType">The unit type.</param>
      /// <param name="unitSystem">The unit system.</param>
      /// <param name="unitName">The unit name.</param>
      /// <remarks>This is only intended to create a unit container for units that are necessary for the file,
      /// but are not found in the file.  It should not be used for IfcUnit entities in the file.</remarks>
      public static IFCUnit ProcessIFCDefaultUnit(ForgeTypeId specTypeId, UnitSystem unitSystem, ForgeTypeId unitTypeId, double? scaleFactor)
      {
         IFCUnit unit = new IFCUnit();

         unit.Spec = specTypeId;
         unit.Unit = unitTypeId;
         unit.UnitSystem = unitSystem;
         if (scaleFactor.HasValue)
            unit.ScaleFactor = scaleFactor.Value;
         unit.OffsetFactor = 0.0;

         return unit;
      }
   }
}