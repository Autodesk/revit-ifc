//
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

      UnitType m_UnitType = UnitType.UT_Undefined;

      // only used if Spec = UnitType.UT_Custom.
      string m_CustomSpec = null;

      UnitSymbolType m_SymbolTypeId = UnitSymbolType.UST_NONE;

      DisplayUnitType m_UnitTypeId = DisplayUnitType.DUT_UNDEFINED;

      UnitSystem m_UnitSystem = UnitSystem.Metric;

      static IDictionary<string, double> m_sPrefixToScaleFactor = null;

      static IDictionary<UnitType, IDictionary<string, Tuple<DisplayUnitType, UnitSymbolType>>> m_sSupportedMetricUnitTypes = null;

      /// <summary>
      /// The type of unit, such as Length.
      /// </summary>
      public UnitType Spec
      {
         get { return m_UnitType; }
         protected set { m_UnitType = value; }
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
      public DisplayUnitType Unit
      {
         get { return m_UnitTypeId; }
         protected set { m_UnitTypeId = value; }
      }

      /// <summary>
      /// The unit symbols, such as "m" for meters.
      /// </summary>
      public UnitSymbolType Symbol
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

         return (unit.Spec == UnitType.UT_Undefined || unit.Unit == DisplayUnitType.DUT_UNDEFINED);
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

      private IDictionary<string, Tuple<DisplayUnitType, UnitSymbolType>> GetSupportedDisplayTypes(UnitType unitType)
      {
         if (m_sSupportedMetricUnitTypes == null)
            m_sSupportedMetricUnitTypes = new Dictionary<UnitType, IDictionary<string, Tuple<DisplayUnitType, UnitSymbolType>>>();

         IDictionary<string, Tuple<DisplayUnitType, UnitSymbolType>> supportedTypes = null;
         if (!m_sSupportedMetricUnitTypes.TryGetValue(unitType, out supportedTypes))
         {
            supportedTypes = new Dictionary<string, Tuple<DisplayUnitType, UnitSymbolType>>();
            switch (unitType)
            {
               case UnitType.UT_Area:
                  supportedTypes[""] = Tuple.Create(DisplayUnitType.DUT_SQUARE_METERS, UnitSymbolType.UST_M_SUP_2);
                  supportedTypes["CENTI"] = Tuple.Create(DisplayUnitType.DUT_SQUARE_CENTIMETERS, UnitSymbolType.UST_CM_SUP_2);
                  supportedTypes["MILLI"] = Tuple.Create(DisplayUnitType.DUT_SQUARE_MILLIMETERS, UnitSymbolType.UST_MM_SUP_2);
                  break;

               case UnitType.UT_Electrical_Current:
                  supportedTypes[""] = Tuple.Create(DisplayUnitType.DUT_AMPERES, UnitSymbolType.UST_AMPERE);
                  supportedTypes["KILO"] = Tuple.Create(DisplayUnitType.DUT_KILOAMPERES, UnitSymbolType.UST_KILOAMPERE);
                  supportedTypes["MILLI"] = Tuple.Create(DisplayUnitType.DUT_MILLIAMPERES, UnitSymbolType.UST_MILLIAMPERE);
                  break;

               case UnitType.UT_Electrical_Frequency:
                  supportedTypes[""] = Tuple.Create(DisplayUnitType.DUT_HERTZ, UnitSymbolType.UST_HZ);
                  break;

               case UnitType.UT_Electrical_Illuminance:
                  supportedTypes[""] = Tuple.Create(DisplayUnitType.DUT_LUX, UnitSymbolType.UST_LX);
                  break;

               case UnitType.UT_Electrical_Luminous_Flux:
                  supportedTypes[""] = Tuple.Create(DisplayUnitType.DUT_LUMENS, UnitSymbolType.UST_LM);
                  break;

               case UnitType.UT_Electrical_Luminous_Intensity:
                  supportedTypes[""] = Tuple.Create(DisplayUnitType.DUT_CANDELAS, UnitSymbolType.UST_CD);
                  break;

               case UnitType.UT_Electrical_Potential:
                  supportedTypes[""] = Tuple.Create(DisplayUnitType.DUT_VOLTS, UnitSymbolType.UST_VOLT);
                  supportedTypes["KILO"] = Tuple.Create(DisplayUnitType.DUT_KILOVOLTS, UnitSymbolType.UST_KILOVOLT);
                  supportedTypes["MILLI"] = Tuple.Create(DisplayUnitType.DUT_MILLIVOLTS, UnitSymbolType.UST_MILLIVOLT);
                  break;

               case UnitType.UT_Force:
                  supportedTypes[""] = Tuple.Create(DisplayUnitType.DUT_NEWTONS, UnitSymbolType.UST_N);    // Even if unit is grams, display kg.
                  supportedTypes["KILO"] = Tuple.Create(DisplayUnitType.DUT_KILONEWTONS, UnitSymbolType.UST_K_N);
                  break;

               case UnitType.UT_HVAC_Power:
                  supportedTypes[""] = Tuple.Create(DisplayUnitType.DUT_WATTS, UnitSymbolType.UST_WATT);
                  break;

               case UnitType.UT_HVAC_Pressure:
                  supportedTypes[""] = Tuple.Create(DisplayUnitType.DUT_PASCALS, UnitSymbolType.UST_PASCAL);
                  supportedTypes["KILO"] = Tuple.Create(DisplayUnitType.DUT_KILOPASCALS, UnitSymbolType.UST_PASCAL);
                  supportedTypes["MEGA"] = Tuple.Create(DisplayUnitType.DUT_MEGAPASCALS, UnitSymbolType.UST_MEGAPASCAL);
                  break;

               case UnitType.UT_Length:
                  supportedTypes[""] = Tuple.Create(DisplayUnitType.DUT_METERS, UnitSymbolType.UST_M);
                  supportedTypes["CENTI"] = Tuple.Create(DisplayUnitType.DUT_CENTIMETERS, UnitSymbolType.UST_CM);
                  supportedTypes["MILLI"] = Tuple.Create(DisplayUnitType.DUT_MILLIMETERS, UnitSymbolType.UST_MM);
                  break;

               case UnitType.UT_Mass:
                  supportedTypes[""] = Tuple.Create(DisplayUnitType.DUT_KILOGRAMS_MASS, UnitSymbolType.UST_KGM);    // Even if unit is grams, display kg.
                  supportedTypes["KILO"] = Tuple.Create(DisplayUnitType.DUT_KILOGRAMS_MASS, UnitSymbolType.UST_KGM);
                  break;

               case UnitType.UT_MassDensity:
                  supportedTypes[""] = Tuple.Create(DisplayUnitType.DUT_KILOGRAMS_PER_CUBIC_METER, UnitSymbolType.UST_KG_PER_CU_M);    // Even if unit is grams, display kg.
                  supportedTypes["KILO"] = Tuple.Create(DisplayUnitType.DUT_KILOGRAMS_PER_CUBIC_METER, UnitSymbolType.UST_KG_PER_CU_M);
                  break;

               case UnitType.UT_Volume:
                  supportedTypes[""] = Tuple.Create(DisplayUnitType.DUT_CUBIC_METERS, UnitSymbolType.UST_M_SUP_3);
                  supportedTypes["DECI"] = Tuple.Create(DisplayUnitType.DUT_LITERS, UnitSymbolType.UST_L);
                  supportedTypes["CENTI"] = Tuple.Create(DisplayUnitType.DUT_CUBIC_CENTIMETERS, UnitSymbolType.UST_CM_SUP_3);
                  supportedTypes["MILLI"] = Tuple.Create(DisplayUnitType.DUT_CUBIC_MILLIMETERS, UnitSymbolType.UST_MM_SUP_3);
                  break;

               default:
                  break;
            }
            m_sSupportedMetricUnitTypes[unitType] = supportedTypes;
         }

         return supportedTypes;
      }

      private double GetScaleFactorForUnitType(string prefix, UnitType unitType)
      {
         double scaleFactor = m_sPrefixToScaleFactor[prefix];

         const double lengthFactor = (1.0 / 0.3048);
         const double areaFactor = lengthFactor * lengthFactor;
         const double volumeFactor = areaFactor * lengthFactor;

         // length ^ -2
         if (unitType == UnitType.UT_Electrical_Illuminance)
         {
            return (scaleFactor * scaleFactor) / areaFactor;
         }
         // length ^ -1
         if (unitType == UnitType.UT_HVAC_Pressure)
         {
            return scaleFactor / lengthFactor;
         }
         // length
         if (unitType == UnitType.UT_Force ||
            unitType == UnitType.UT_Length)
         {
            return (scaleFactor * lengthFactor);
         }
         // length ^ 2
         if (unitType == UnitType.UT_Area ||
            unitType == UnitType.UT_Electrical_Potential ||
            unitType == UnitType.UT_HVAC_Power)
         {
            return (scaleFactor * scaleFactor) * areaFactor;
         }
         // length ^ 3
         if (unitType == UnitType.UT_Volume)
         {
            return (scaleFactor * scaleFactor * scaleFactor) * volumeFactor;
         }
         if (unitType == UnitType.UT_Mass)
         {
            return (scaleFactor / 1000.0);   // Standard internal scale is kg.
         }
         return scaleFactor;
      }

      /// <summary>
      /// Processes the metric prefix of a dimension.
      /// </summary>
      /// <param name="prefix">The prefix name.</param>
      /// <param name="UnitType.UT_">The spec identifier.</param>
      /// <returns>True if the prefix is supported, false if not.</returns>
      private bool ProcessMetricPrefix(string prefix, UnitType unitType)
      {
         if (prefix == null)
            prefix = "";

         IDictionary<string, Tuple<DisplayUnitType, UnitSymbolType>> supportedDisplayTypes = GetSupportedDisplayTypes(unitType);
         if (!supportedDisplayTypes.ContainsKey(prefix))
            return false;

         if (m_sPrefixToScaleFactor == null)
            InitPrefixToScaleFactor();

         if (!m_sPrefixToScaleFactor.ContainsKey(prefix))
            return false;

         Tuple<DisplayUnitType, UnitSymbolType> unitNameAndSymbol = supportedDisplayTypes[prefix];
         Unit = unitNameAndSymbol.Item1;
         Symbol = unitNameAndSymbol.Item2;
         ScaleFactor *= GetScaleFactorForUnitType(prefix, unitType);
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
         public DerivedUnitExpectedTypes(DisplayUnitType displayUnitTypeId, UnitSymbolType symbolTypeId)
         {
            Unit = displayUnitTypeId;
            Symbol = symbolTypeId;
         }

         /// <summary>
         /// The set of expected types.
         /// </summary>
         public ISet<Tuple<int, UnitType, string>> ExpectedTypes
         {
            get { return m_ExpectedTypes; }
         }

         /// <summary>
         /// The unit name of this set of expected types.
         /// </summary>
         public DisplayUnitType Unit { get; protected set; }

         /// <summary>
         /// The unit symbol type of this set of expected types.
         /// </summary>
         public UnitSymbolType Symbol { get; protected set; }

         private ISet<Tuple<int, UnitType, string>> ExpectedTypesCopy()
         {
            ISet<Tuple<int, UnitType, string>> expectedTypesCopy = new HashSet<Tuple<int, UnitType, string>>();
            foreach (Tuple<int, UnitType, string> expectedType in ExpectedTypes)
            {
               expectedTypesCopy.Add(expectedType);
            }
            return expectedTypesCopy;
         }

         /// <summary>
         /// Add a standard expected type.
         /// </summary>
         /// <param name="exponent">The exponent of the type.</param>
         /// <param name="UnitType.UT_">The spec identifier.</param>
         public void AddExpectedType(int exponent, UnitType unitType)
         {
            ExpectedTypes.Add(new Tuple<int, UnitType, string>(exponent, unitType, null));
         }

         /// <summary>
         /// Add a custom expected type.
         /// </summary>
         /// <param name="exponent">The exponent of the type.</param>
         /// <param name="unitName">The name of the base unit.</param>
         public void AddCustomExpectedType(int exponent, string unitName)
         {
            ExpectedTypes.Add(Tuple.Create(exponent, UnitType.UT_Custom, unitName));
         }

         public bool Matches(IList<Tuple<IFCUnit, int>> derivedElementUnitHnds, out double scaleFactor)
         {
            scaleFactor = 1.0;

            if (derivedElementUnitHnds.Count != ExpectedTypes.Count)
               return false;

            ISet<Tuple<int, UnitType, string>> expectedTypes = ExpectedTypesCopy();

            foreach (Tuple<IFCUnit, int> derivedElementUnitHnd in derivedElementUnitHnds)
            {
               int dimensionality = derivedElementUnitHnd.Item2;
               Tuple<int, UnitType, string> currKey = Tuple.Create(dimensionality, derivedElementUnitHnd.Item1.Spec, derivedElementUnitHnd.Item1.CustomSpec);
               if (expectedTypes.Contains(currKey))
               {
                  expectedTypes.Remove(currKey);
                  scaleFactor *= Math.Pow(derivedElementUnitHnd.Item1.ScaleFactor, dimensionality);
               }
               else
                  break;
            }

            // Found all supported units.
            if (expectedTypes.Count != 0)
               return false;

            return true;
         }

         private ISet<Tuple<int, UnitType, string>> m_ExpectedTypes = new HashSet<Tuple<int, UnitType, string>>();
      }

      /// <summary>
      /// Processes an IfcDerivedUnit.
      /// </summary>
      /// <param name="unitHnd">The unit handle.</param>
      void ProcessIFCDerivedUnit(IFCAnyHandle unitHnd)
      {
         List<IFCAnyHandle> elements =
             IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(unitHnd, "Elements");

         IList<Tuple<IFCUnit, int>> derivedElementUnitHnds = new List<Tuple<IFCUnit, int>>();
         foreach (IFCAnyHandle subElement in elements)
         {
            IFCAnyHandle derivedElementUnitHnd = IFCImportHandleUtil.GetRequiredInstanceAttribute(subElement, "Unit", false);
            IFCUnit subUnit = IFCAnyHandleUtil.IsNullOrHasNoValue(derivedElementUnitHnd) ? null : IFCUnit.ProcessIFCUnit(derivedElementUnitHnd);
            if (subUnit != null)
            {
               bool found;
               int exponent = IFCImportHandleUtil.GetRequiredIntegerAttribute(subElement, "Exponent", out found);
               if (found)
                  derivedElementUnitHnds.Add(Tuple.Create(subUnit, exponent));
            }
         }

         // the DerivedUnitExpectedTypes object is a description of one possible set of base units for a particular derived unit.
         // The IList allows for possible different interpretations.  For example, Volumetric Flow Rate could be defined by m^3/s (length ^ 3 / time) or L/s (volume / time).
         IList<DerivedUnitExpectedTypes> expectedTypesList = new List<DerivedUnitExpectedTypes>();

         string unitType = IFCAnyHandleUtil.GetEnumerationAttribute(unitHnd, "UnitType");
         switch (unitType)
         {
            case "LINEARVELOCITYUNIT":
               {
                  Spec = UnitType.UT_HVAC_Velocity;
                  UnitSystem = UnitSystem.Metric;

                  // Support only m / s.
                  DerivedUnitExpectedTypes expectedTypes = new DerivedUnitExpectedTypes(DisplayUnitType.DUT_METERS_PER_SECOND, UnitSymbolType.UST_M_PER_S);
                  expectedTypes.AddExpectedType(1, UnitType.UT_Length);
                  expectedTypes.AddCustomExpectedType(-1, "TIMEUNIT");
                  expectedTypesList.Add(expectedTypes);
               }
               break;
            case "THERMALTRANSMITTANCEUNIT":
               {
                  Spec = UnitType.UT_HVAC_CoefficientOfHeatTransfer;
                  UnitSystem = UnitSystem.Metric;

                  // Support W / (K * m^2) or kg / (K * s^3)
                  DerivedUnitExpectedTypes expectedTypesWinvKinvM2 = new DerivedUnitExpectedTypes(DisplayUnitType.DUT_WATTS_PER_SQUARE_METER_KELVIN, UnitSymbolType.UST_WATT_PER_SQ_M_K);
                  expectedTypesWinvKinvM2.AddExpectedType(1, UnitType.UT_HVAC_Power); // UT_Electrical_Wattage is similar, but UT_HVAC_Power is the one we map to.
                  expectedTypesWinvKinvM2.AddExpectedType(-1, UnitType.UT_HVAC_Temperature);
                  expectedTypesWinvKinvM2.AddExpectedType(-2, UnitType.UT_Length);
                  expectedTypesList.Add(expectedTypesWinvKinvM2);

                  DerivedUnitExpectedTypes expectedTypesWinvKinvArea = new DerivedUnitExpectedTypes(DisplayUnitType.DUT_WATTS_PER_SQUARE_METER_KELVIN, UnitSymbolType.UST_WATT_PER_SQ_M_K);
                  expectedTypesWinvKinvArea.AddExpectedType(1, UnitType.UT_HVAC_Power); // UT_Electrical_Wattage is similar, but UT_HVAC_Power is the one we map to.
                  expectedTypesWinvKinvArea.AddExpectedType(-1, UnitType.UT_HVAC_Temperature);
                  expectedTypesWinvKinvArea.AddExpectedType(-1, UnitType.UT_Area);
                  expectedTypesList.Add(expectedTypesWinvKinvArea);

                  DerivedUnitExpectedTypes expectedTypes = new DerivedUnitExpectedTypes(DisplayUnitType.DUT_WATTS_PER_SQUARE_METER_KELVIN, UnitSymbolType.UST_WATT_PER_SQ_M_K);
                  expectedTypes.AddExpectedType(1, UnitType.UT_Mass);
                  expectedTypes.AddExpectedType(-1, UnitType.UT_HVAC_Temperature);
                  expectedTypes.AddCustomExpectedType(-3, "TIMEUNIT");
                  expectedTypesList.Add(expectedTypes);
               }
               break;
            case "VOLUMETRICFLOWRATEUNIT":
               {
                  Spec = UnitType.UT_HVAC_Airflow;
                  UnitSystem = UnitSystem.Metric;

                  // Support L / s or m^3 / s in the IFC file.

                  // L / s
                  DerivedUnitExpectedTypes expectedTypesLPerS = new DerivedUnitExpectedTypes(DisplayUnitType.DUT_LITERS_PER_SECOND, UnitSymbolType.UST_LPS);
                  expectedTypesLPerS.AddExpectedType(1, UnitType.UT_Volume);
                  expectedTypesLPerS.AddCustomExpectedType(-1, "TIMEUNIT");
                  expectedTypesList.Add(expectedTypesLPerS);

                  // m^3 / s.
                  DerivedUnitExpectedTypes expectedTypes = new DerivedUnitExpectedTypes(DisplayUnitType.DUT_CUBIC_METERS_PER_SECOND, UnitSymbolType.UST_CU_M_PER_S);
                  expectedTypes.AddExpectedType(3, UnitType.UT_Length);
                  expectedTypes.AddCustomExpectedType(-1, "TIMEUNIT");
                  expectedTypesList.Add(expectedTypes);
               }
               break;
            case "MASSDENSITYUNIT":
               {
                  Spec = UnitType.UT_MassDensity;
                  UnitSystem = UnitSystem.Metric;

                  // Support kg / m^3 in the IFC file.

                  // kg / m^3.
                  DerivedUnitExpectedTypes expectedTypes = new DerivedUnitExpectedTypes(DisplayUnitType.DUT_KILOGRAMS_PER_CUBIC_METER, UnitSymbolType.UST_KG_PER_CU_M);
                  expectedTypes.AddExpectedType(1, UnitType.UT_Mass);
                  expectedTypes.AddExpectedType(-3, UnitType.UT_Length);
                  expectedTypesList.Add(expectedTypes);
               }
               break;
            case "LINEARFORCEUNIT":
               {
                  Spec = UnitType.UT_LinearForce;
                  UnitSystem = UnitSystem.Metric;

                  // Support N / m in the IFC file.
                  DerivedUnitExpectedTypes expectedTypes = new DerivedUnitExpectedTypes(DisplayUnitType.DUT_NEWTONS_PER_METER, UnitSymbolType.UST_N_PER_M);
                  expectedTypes.AddExpectedType(1, UnitType.UT_LinearForce);
                  expectedTypesList.Add(expectedTypes);

                  // Support N / m in basic units
                  DerivedUnitExpectedTypes expectedTypes2 = new DerivedUnitExpectedTypes(DisplayUnitType.DUT_NEWTONS_PER_METER, UnitSymbolType.UST_N_PER_M);
                  expectedTypes2.AddExpectedType(1, UnitType.UT_Mass);
                  expectedTypes2.AddExpectedType(1, UnitType.UT_Length);
                  expectedTypes2.AddCustomExpectedType(-2, "TIMEUNIT");
                  expectedTypes2.AddExpectedType(-1, UnitType.UT_Length);
                  expectedTypesList.Add(expectedTypes2);
               }
               break;
            case "PLANARFORCEUNIT":
               {
                  Spec = UnitType.UT_AreaForce;
                  UnitSystem = UnitSystem.Metric;

                  // Support N / m^2 in the IFC file.
                  DerivedUnitExpectedTypes expectedTypes = new DerivedUnitExpectedTypes(DisplayUnitType.DUT_NEWTONS_PER_SQUARE_METER, UnitSymbolType.UST_N_PER_M_SUP_2);
                  expectedTypes.AddExpectedType(1, UnitType.UT_AreaForce);
                  expectedTypesList.Add(expectedTypes);

                  // Support N / m in basic units
                  DerivedUnitExpectedTypes expectedTypes2 = new DerivedUnitExpectedTypes(DisplayUnitType.DUT_NEWTONS_PER_SQUARE_METER, UnitSymbolType.UST_N_PER_M_SUP_2);
                  expectedTypes2.AddExpectedType(1, UnitType.UT_Mass);
                  expectedTypes2.AddExpectedType(1, UnitType.UT_Length);
                  expectedTypes2.AddCustomExpectedType(-2, "TIMEUNIT");
                  expectedTypes2.AddExpectedType(-2, UnitType.UT_Length);
                  expectedTypesList.Add(expectedTypes2);
               }
               break;
            case "USERDEFINED":
               {
                  // Look at the sub-types to see what we support.
                  string userDefinedType = IFCImportHandleUtil.GetOptionalStringAttribute(unitHnd, "UserDefinedType", null);
                  if (!string.IsNullOrWhiteSpace(userDefinedType))
                  {
                     if (string.Compare(userDefinedType, "Luminous Efficacy", true) == 0)
                     {
                        Spec = UnitType.UT_Electrical_Efficacy;
                        UnitSystem = UnitSystem.Metric;

                        // Support only lm / W.
                        DerivedUnitExpectedTypes expectedTypes = new DerivedUnitExpectedTypes(DisplayUnitType.DUT_LUMENS_PER_WATT, UnitSymbolType.UST_LM_PER_W);
                        expectedTypes.AddExpectedType(-1, UnitType.UT_Mass);
                        expectedTypes.AddExpectedType(-2, UnitType.UT_Length);
                        expectedTypes.AddCustomExpectedType(3, "TIMEUNIT");
                        expectedTypes.AddExpectedType(1, UnitType.UT_Electrical_Luminous_Flux);
                        expectedTypesList.Add(expectedTypes);
                     }
                     else if (string.Compare(userDefinedType, "Friction Loss", true) == 0)
                     {
                        Spec = UnitType.UT_HVAC_Friction;
                        UnitSystem = UnitSystem.Metric;

                        // Support only Pa / m.
                        DerivedUnitExpectedTypes expectedTypes = new DerivedUnitExpectedTypes(DisplayUnitType.DUT_PASCALS_PER_METER, UnitSymbolType.UST_PASCAL_PER_M);
                        expectedTypes.AddExpectedType(-2, UnitType.UT_Length);
                        expectedTypes.AddExpectedType(1, UnitType.UT_Mass);
                        expectedTypes.AddCustomExpectedType(-2, "TIMEUNIT");
                        expectedTypesList.Add(expectedTypes);
                     }
                  }
               }
               break;
            default:
               break;
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

         switch (unitType)
         {
            case "AREAUNIT" :
               {
                  Spec = UnitType.UT_Area;
                  unitNameSupported = (string.Compare(unitName, "SQUARE_METRE", true) == 0) && ProcessMetricPrefix(prefix, Spec);
               }
               break;
            case "ELECTRICCURRENTUNIT" :
               {
                  Spec = UnitType.UT_Electrical_Current;
                  unitNameSupported = ProcessMetricPrefix(prefix, Spec);
               }
               break;
            case "ELECTRICVOLTAGEUNIT" :
               {
                  Spec = UnitType.UT_Electrical_Potential;
                  unitNameSupported = ProcessMetricPrefix(prefix, Spec);
               }
               break;
            case "FORCEUNIT" :
               {
                  Spec = UnitType.UT_Force;
                  unitNameSupported = ProcessMetricPrefix(prefix, Spec);
               }
               break;
            case "FREQUENCYUNIT" :
               {
                  Spec = UnitType.UT_Electrical_Frequency;
                  unitNameSupported = ProcessMetricPrefix(prefix, Spec);
               }
               break;
            case "ILLUMINANCEUNIT" :
               {
                  Spec = UnitType.UT_Electrical_Illuminance;
                  unitNameSupported = ProcessMetricPrefix(prefix, Spec);
               }
               break;
            case "LENGTHUNIT" :
               {
                  Spec = UnitType.UT_Length;
                  unitNameSupported = (string.Compare(unitName, "METRE", true) == 0) && ProcessMetricPrefix(prefix, UnitType.UT_Length);
               }
               break;
            case "LUMINOUSFLUXUNIT" :
               {
                  Spec = UnitType.UT_Electrical_Luminous_Flux;
                  unitNameSupported = ProcessMetricPrefix(prefix, Spec);
               }
               break;
            case "LUMINOUSINTENSITYUNIT" :
               {
                  Spec = UnitType.UT_Electrical_Luminous_Intensity;
                  unitNameSupported = ProcessMetricPrefix(prefix, Spec);
               }
               break;
            case "MASSUNIT" :
               {
                  Spec = UnitType.UT_Mass;
                  unitNameSupported = ProcessMetricPrefix(prefix, Spec);
               }
               break;
            case "PLANEANGLEUNIT" :
               {
                  Spec = UnitType.UT_Angle;
                  Unit = DisplayUnitType.DUT_RADIANS;
                  unitNameSupported = (string.Compare(unitName, "RADIAN", true) == 0) && (string.IsNullOrWhiteSpace(prefix));
               }
               break;
            case "POWERUNIT" :
               {
                  Spec = UnitType.UT_HVAC_Power;
                  unitNameSupported = ProcessMetricPrefix(prefix, Spec);
               }
               break;
            case "PRESSUREUNIT" :
               {
                  Spec = UnitType.UT_HVAC_Pressure;
                  unitNameSupported = ProcessMetricPrefix(prefix, Spec);
               }
               break;
            case "SOLIDANGLEUNIT" :
               {
                  // Will warn if not steridians.
                  Spec = UnitType.UT_Custom;
                  CustomSpec = unitType;
                  unitNameSupported = (string.Compare(unitName, "STERADIAN", true) == 0) && (string.IsNullOrWhiteSpace(prefix));
               }
               break;
            case "THERMODYNAMICTEMPERATUREUNIT" :
               {
                  Spec = UnitType.UT_HVAC_Temperature;
                  if (string.Compare(unitName, "DEGREE_CELSIUS", true) == 0 ||
                      string.Compare(unitName, "CELSIUS", true) == 0)
                  {
                     Unit = DisplayUnitType.DUT_CELSIUS;
                     Symbol = UnitSymbolType.UST_DEGREE_C;
                     OffsetFactor = -273.15;
                  }
                  else if (string.Compare(unitName, "KELVIN", true) == 0 ||
                      string.Compare(unitName, "DEGREE_KELVIN", true) == 0)
                  {
                     Unit = DisplayUnitType.DUT_KELVIN;
                     Symbol = UnitSymbolType.UST_KELVIN;
                  }
                  else if (string.Compare(unitName, "FAHRENHEIT", true) == 0 ||
                      string.Compare(unitName, "DEGREE_FAHRENHEIT", true) == 0)
                  {
                     UnitSystem = UnitSystem.Imperial;
                     Unit = DisplayUnitType.DUT_FAHRENHEIT;
                     Symbol = UnitSymbolType.UST_DEGREE_F;
                     ScaleFactor = 5.0 / 9.0;
                     OffsetFactor = (5.0 / 9.0) * 32 - 273.15;
                  }
                  else
                     unitNameSupported = false;
               }
               break;
            case "TIMEUNIT" :
               {
                  // Will warn if not seconds.
                  Spec = UnitType.UT_Custom;
                  CustomSpec = unitType;
                  unitNameSupported = (string.Compare(unitName, "SECOND", true) == 0) && (string.IsNullOrWhiteSpace(prefix));
               }
               break;
            case "VOLUMEUNIT" :
               {
                  Spec = UnitType.UT_Volume;
                  unitNameSupported = (string.Compare(unitName, "CUBIC_METRE", true) == 0) && ProcessMetricPrefix(prefix, UnitType.UT_Volume);
               }
               break;
            default:
               {
                  Importer.TheLog.LogUnhandledUnitTypeError(unitHnd, unitType);
               }
               break;
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
         string currencyType = (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC4Obsolete)) ?
            IFCImportHandleUtil.GetOptionalStringAttribute(monetaryUnitHnd, "Currency", string.Empty) :
            IFCAnyHandleUtil.GetEnumerationAttribute(monetaryUnitHnd, "Currency");
      
         Spec = UnitType.UT_Currency;
         Unit = DisplayUnitType.DUT_CURRENCY;

         Symbol = UnitSymbolType.UST_NONE;
         if ((string.Compare(currencyType, "CAD", true) == 0) ||
             (string.Compare(currencyType, "USD", true) == 0) ||
             (string.Compare(currencyType, "$", true) == 0))
            Symbol = UnitSymbolType.UST_DOLLAR;
         else if ((string.Compare(currencyType, "EUR", true) == 0) ||
            (string.Compare(currencyType, "€", true) == 0))
            Symbol = UnitSymbolType.UST_EURO_PREFIX;
         else if ((string.Compare(currencyType, "GBP", true) == 0) ||
            (string.Compare(currencyType, "£", true) == 0))
            Symbol = UnitSymbolType.UST_POUND;
         else if (string.Compare(currencyType, "HKD", true) == 0)
            Symbol = UnitSymbolType.UST_CHINESE_HONG_KONG_SAR;
         else if ((string.Compare(currencyType, "ICK", true) == 0) ||
             (string.Compare(currencyType, "NOK", true) == 0) ||
             (string.Compare(currencyType, "SEK", true) == 0))
            Symbol = UnitSymbolType.UST_KRONER;
         else if (string.Compare(currencyType, "ILS", true) == 0)
            Symbol = UnitSymbolType.UST_SHEQEL;
         else if ((string.Compare(currencyType, "JPY", true) == 0) ||
             (string.Compare(currencyType, "¥", true) == 0))
            Symbol = UnitSymbolType.UST_YEN;
         else if (string.Compare(currencyType, "KRW", true) == 0)
            Symbol = UnitSymbolType.UST_WON;
         else if ((string.Compare(currencyType, "THB", true) == 0) ||
             (string.Compare(currencyType, "฿", true) == 0))
            Symbol = UnitSymbolType.UST_BAHT;
         else if (string.Compare(currencyType, "VND", true) == 0)
            Symbol = UnitSymbolType.UST_DONG;
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
         switch(unitType)
         {
            case "LENGTHUNIT":
               {
                  Spec = UnitType.UT_Length;
                  string name = IFCAnyHandleUtil.GetStringAttribute(convUnitHnd, "Name");

                  if (string.Compare(name, "FOOT", true) == 0 ||
                     string.Compare(name, "FEET", true) == 0)
                  {
                     UnitSystem = UnitSystem.Imperial;
                     Unit = DisplayUnitType.DUT_FEET_FRACTIONAL_INCHES;
                     Symbol = UnitSymbolType.UST_NONE;
                  } 
                  else if (string.Compare(name, "INCH", true) == 0 ||
                     string.Compare(name, "INCHES", true) == 0)
                  {
                     UnitSystem = UnitSystem.Imperial;
                     Unit = DisplayUnitType.DUT_FRACTIONAL_INCHES;
                     Symbol = UnitSymbolType.UST_NONE;
                  }
               }
               break;
            case "PLANEANGLEUNIT" :
               {
                  Spec = UnitType.UT_Angle;
                  string name = IFCAnyHandleUtil.GetStringAttribute(convUnitHnd, "Name");

                  if (string.Compare(name, "GRAD", true) == 0 ||
                     string.Compare(name, "GRADIAN", true) == 0 ||
                      string.Compare(name, "GRADS", true) == 0 ||
                     string.Compare(name, "GRADIANS", true) == 0)
                  {
                     UnitSystem = UnitSystem.Metric;
                     Unit = DisplayUnitType.DUT_GRADS;
                     Symbol = UnitSymbolType.UST_GRAD;
                  }
                  else if (string.Compare(name, "DEGREE", true) == 0 ||
                     string.Compare(name, "DEGREES", true) == 0)
                  {
                     UnitSystem = UnitSystem.Imperial;
                     Unit = DisplayUnitType.DUT_DECIMAL_DEGREES;
                     Symbol = UnitSymbolType.UST_DEGREE_SYMBOL;
                  }
               }
               break;
            case "AREAUNIT" :
               {
                  Spec = UnitType.UT_Area;
                  string name = IFCAnyHandleUtil.GetStringAttribute(convUnitHnd, "Name");

                  if (string.Compare(name, "SQUARE FOOT", true) == 0 ||
                     string.Compare(name, "SQUARE_FOOT", true) == 0 ||
                     string.Compare(name, "SQUARE FEET", true) == 0 ||
                     string.Compare(name, "SQUARE_FEET", true) == 0)
                  {
                     UnitSystem = UnitSystem.Imperial;
                     Unit = DisplayUnitType.DUT_SQUARE_FEET;
                     Symbol = UnitSymbolType.UST_SF;
                  }
               }
               break;
            case "VOLUMEUNIT" :
               {
                  Spec = UnitType.UT_Volume;
                  string name = IFCAnyHandleUtil.GetStringAttribute(convUnitHnd, "Name");

                  if (string.Compare(name, "CUBIC FOOT", true) == 0 ||
                     string.Compare(name, "CUBIC_FOOT", true) == 0 ||
                     string.Compare(name, "CUBIC FEET", true) == 0 ||
                     string.Compare(name, "CUBIC_FEET", true) == 0)
                  {
                     UnitSystem = UnitSystem.Imperial;
                     Unit = DisplayUnitType.DUT_CUBIC_FEET;
                     Symbol = UnitSymbolType.UST_CF;
                  }
               }
               break;
            case "THERMODYNAMICMEASUREUNIT" :
               {
                  Spec = UnitType.UT_HVAC_Temperature;
                  string name = IFCAnyHandleUtil.GetStringAttribute(convUnitHnd, "Name");

                  if ((string.Compare(name, "F", true) == 0) ||
                     (string.Compare(name, "FAHRENHEIT", true) == 0))
                  {
                     UnitSystem = UnitSystem.Imperial;
                     Unit = DisplayUnitType.DUT_FAHRENHEIT;
                     Symbol = UnitSymbolType.UST_DEGREE_F;
                  }
                  else if ((string.Compare(name, "R", true) == 0) ||
                     (string.Compare(name, "RANKINE", true) == 0))
                  {
                     UnitSystem = UnitSystem.Imperial;
                     Unit = DisplayUnitType.DUT_RANKINE;
                     Symbol = UnitSymbolType.UST_DEGREE_R;
                  }
               }
               break;
            default:
               break;
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
      public static IFCUnit ProcessIFCDefaultUnit(UnitType unitType, UnitSystem unitSystem, DisplayUnitType unitTypeId, double? scaleFactor)
      {
         IFCUnit unit = new IFCUnit();

         unit.Spec = unitType;
         unit.Unit = unitTypeId;
         unit.UnitSystem = unitSystem;
         if (scaleFactor.HasValue)
            unit.ScaleFactor = scaleFactor.Value;
         unit.OffsetFactor = 0.0;

         return unit;
      }
   }
}