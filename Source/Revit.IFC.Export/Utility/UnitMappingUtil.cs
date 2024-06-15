using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Utility
{
   public class UnitMappingUtil
   {
      /// Creates an apropriate ifc unit entity if it hasn't been already created.
      /// Cache the created unit handle in the UnitsCache.
      /// At the end of export these units are assigned to IfcProject
      public static UnitInfo GetOrCreateUnitInfo(ForgeTypeId specTypeId)
      {
         UnitInfo unitInfo = null;
         if (specTypeId == null)
            return unitInfo;

         if (ExporterCacheManager.UnitsCache.FindUnitInfo(specTypeId, out unitInfo))
            return unitInfo;

         IFCFile file = ExporterCacheManager.ExporterIFC?.GetFile();
         if (file == null)
            return unitInfo;

         unitInfo = CreateSpecialCases(file, specTypeId);

         if (unitInfo == null)
            unitInfo = CreateUnitFromMappings(file, specTypeId, byDefault: false);
         if (unitInfo == null)
            unitInfo = CreateUnitFromMappings(file, specTypeId, byDefault: true);

         ExporterCacheManager.UnitsCache.RegisterUnitInfo(specTypeId, unitInfo);
         return unitInfo;
      }

      /// <summary>
      /// Extracts the unit handles to assign to a project 
      /// </summary>
      /// <returns>Unit handles set</returns>
      public static HashSet<IFCAnyHandle> GetUnitsToAssign()
      {
         return ExporterCacheManager.UnitsCache.GetUnitsToAssign();
      }

      /// <summary>
      /// Creates units spesific to ExportAsCOBIE
      /// </summary>
      public static void CreateCobieUnits()
      {
         if (!ExporterCacheManager.ExportOptionsCache.ExportAsCOBIE)
            return;

         IFCFile file = ExporterCacheManager.ExporterIFC?.GetFile();
         if (file == null)
            return;

         // Derived imperial mass unit
         {
            IFCUnit unitType = IFCUnit.MassUnit;
            IFCAnyHandle dims = IFCInstanceExporter.CreateDimensionalExponents(file, 0, 1, 0, 0, 0, 0, 0);
            double factor = 0.45359237; // --> pound to kilogram
            string convName = "pound";

            IFCAnyHandle kilogramUnit = IFCInstanceExporter.CreateSIUnit(file, IFCUnit.MassUnit, IFCSIPrefix.Kilo, IFCSIUnitName.Gram);
            IFCAnyHandle convFactor = IFCInstanceExporter.CreateMeasureWithUnit(file, Toolkit.IFCDataUtil.CreateAsMassMeasure(factor), kilogramUnit);
            IFCAnyHandle massUnit = IFCInstanceExporter.CreateConversionBasedUnit(file, dims, unitType, convName, convFactor);
            ExporterCacheManager.UnitsCache.RegisterUnitInfo(SpecTypeId.Mass, new UnitInfo(massUnit, factor, 0.0));
         }

         // Air Changes per Hour
         {
            IFCUnit unitType = IFCUnit.FrequencyUnit;
            IFCAnyHandle dims = IFCInstanceExporter.CreateDimensionalExponents(file, 0, 0, -1, 0, 0, 0, 0);
            double factor = 1.0 / 3600.0; // --> seconds to hours
            string convName = "ACH";

            IFCAnyHandle secondUnit = IFCInstanceExporter.CreateSIUnit(file, IFCUnit.TimeUnit, null, IFCSIUnitName.Second);
            IFCAnyHandle convFactor = IFCInstanceExporter.CreateMeasureWithUnit(file, Toolkit.IFCDataUtil.CreateAsTimeMeasure(factor), secondUnit);
            IFCAnyHandle achUnit = IFCInstanceExporter.CreateConversionBasedUnit(file, dims, unitType, convName, convFactor);
            ExporterCacheManager.UnitsCache.RegisterUserDefinedUnit("ACH", achUnit);
            ExporterCacheManager.UnitsCache.RegisterUnitInfo(SpecTypeId.ElectricalFrequency, new UnitInfo(achUnit, factor, 0.0));
         }

      }

      /// <summary>
      /// Get mapped Revit data type by ifc measure name
      /// </summary>
      public static ForgeTypeId GetUnitSpecTypeFromString(string measureName)
      {
         if (IfcToRevitDataTypeMapping.TryGetValue(measureName, out ForgeTypeId forgeTypeId))
         {
            return forgeTypeId;
         }
         return null;
      }

      /// <summary>
      /// Create units for some special cases
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="specTypeId">Revit data type</param>
      /// <returns>Created unit handle</returns>
      static UnitInfo CreateSpecialCases(IFCFile file, ForgeTypeId specTypeId)
      {
         UnitInfo unitInfo = null;

         if (specTypeId.Equals(SpecTypeId.Currency))
         {
            // Specific IfcMonetaryUnit
            unitInfo = CreateCurrencyUnit(file);
         }
         else if (specTypeId.Equals(SpecTypeId.ColorTemperature))
         {
            // Color Temperature is in fact ThermoDynamicTemperature
            // Create is specific way to avoid conflict with real HVACTemperature 
            unitInfo = CreateColorTemperatureUnit(file);
         }
         else if (specTypeId.Equals(SpecTypeId.MassPerUnitArea))
         {
            // A single unit 'since ifc4'.
            // Make more generic improvement if there are more than one such unit
            if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
            {
               DerivedAttributes attrib;
               if (!DerivedUnitMapping.TryGetValue(specTypeId, out attrib))
                  return null;

               attrib.ifcDrivedUnit = Toolkit.IFC4.IFCDerivedUnit.AREADENSITYUNIT;
               attrib.userDefinedUnitName = string.Empty;
               
               return CreateUnitAsDerivedCommon(file, specTypeId, null, attrib);
            }
         }

         return unitInfo;
      }

      /// <summary>
      /// Creates spesific Currency unit
      /// </summary>
      static UnitInfo CreateCurrencyUnit(IFCFile file)
      {
         UnitInfo unitInfo = null;

         if (!ExporterCacheManager.ExportOptionsCache.ExportAs2x3CoordinationView2)
         {
            FormatOptions currencyFormatOptions = ExporterCacheManager.Document.GetUnits().GetFormatOptions(SpecTypeId.Currency);
            ForgeTypeId currencySymbol = currencyFormatOptions.GetSymbolTypeId();

            IFCAnyHandle currencyUnit = null;

            // Some of these are guesses for IFC2x3, since multiple currencies may use the same symbol, 
            // but no detail is given on which currency is being used.  For IFC4, we just use the label.
            if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
            {
               string currencyLabel = null;
               try
               {
                  currencyLabel = LabelUtils.GetLabelForSymbol(currencySymbol);
                  currencyUnit = IFCInstanceExporter.CreateMonetaryUnit4(file, currencyLabel);
               }
               catch
               {
                  currencyUnit = null;
               }
            }
            else
            {
               IFCCurrencyType? currencyType = null;

               if (currencySymbol.Equals(SymbolTypeId.UsDollar))
               {
                  currencyType = IFCCurrencyType.USD;
               }
               else if (currencySymbol.Equals(SymbolTypeId.EuroPrefix) ||
                  currencySymbol.Equals(SymbolTypeId.EuroSuffix))
               {
                  currencyType = IFCCurrencyType.EUR;
               }
               else if (currencySymbol.Equals(SymbolTypeId.UkPound))
               {
                  currencyType = IFCCurrencyType.GBP;
               }
               else if (currencySymbol.Equals(SymbolTypeId.ChineseHongKongDollar))
               {
                  currencyType = IFCCurrencyType.HKD;
               }
               else if (currencySymbol.Equals(SymbolTypeId.Krone))
               {
                  currencyType = IFCCurrencyType.NOK;
               }
               else if (currencySymbol.Equals(SymbolTypeId.Shekel))
               {
                  currencyType = IFCCurrencyType.ILS;
               }
               else if (currencySymbol.Equals(SymbolTypeId.Yen))
               {
                  currencyType = IFCCurrencyType.JPY;
               }
               else if (currencySymbol.Equals(SymbolTypeId.Won))
               {
                  currencyType = IFCCurrencyType.KRW;
               }
               else if (currencySymbol.Equals(SymbolTypeId.Baht))
               {
                  currencyType = IFCCurrencyType.THB;
               }
               else if (currencySymbol.Equals(SymbolTypeId.Dong))
               {
                  currencyType = IFCCurrencyType.VND;
               }

               if (currencyType.HasValue)
                  currencyUnit = IFCInstanceExporter.CreateMonetaryUnit2x3(file, currencyType.Value);
            }

            if (currencyUnit != null)
            {
               unitInfo = new UnitInfo(currencyUnit, 1.0, 0.0);
               ExporterCacheManager.UnitsCache.RegisterUserDefinedUnit("CURRENCY", currencyUnit);
            }
         }
         return unitInfo;
      }

      /// <summary>
      /// Creates spesific Color Temperature unit
      /// </summary>
      static UnitInfo CreateColorTemperatureUnit(IFCFile file)
      {
         UnitInfo unitInfo = null;
         if (file == null)
            return unitInfo;

         IFCAnyHandle colorTempUnit = IFCInstanceExporter.CreateSIUnit(file, IFCUnit.ThermoDynamicTemperatureUnit, null, IFCSIUnitName.Kelvin);
         (double scaleFactor, double offset) = GetScaleFactorAndOffset(UnitTypeId.Kelvin);
         ExporterCacheManager.UnitsCache.RegisterUserDefinedUnit("COLORTEMPERATURE", colorTempUnit);

         return new UnitInfo(colorTempUnit, scaleFactor, offset);
      }


      #region Classes to keep units attributes
      /// <summary>
      /// A structure to contain information about IfcSIUnit
      /// </summary>
      class SIUnitInfo
      {
         public SIUnitInfo(IFCSIUnitName inIfcSIUnitName, IFCSIPrefix? inIfcSIPrefix, bool inIsDefault)
         {
            ifcSIUnitName = inIfcSIUnitName;
            ifcSIPrefix = inIfcSIPrefix;
            isDefault = inIsDefault;
         }

         readonly public IFCSIUnitName ifcSIUnitName;
         readonly public IFCSIPrefix? ifcSIPrefix;
         readonly public bool isDefault;
      }

      /// <summary>
      /// A structure to contain information about IfcSIUnit
      /// </summary>
      class SIAttributes
      {
         public SIAttributes(IFCUnit inIfcUnitType, IDictionary<ForgeTypeId, SIUnitInfo> inSIUnitInfoDict)
         {
            ifcUnitType = inIfcUnitType;
            siUnitInfoDict = inSIUnitInfoDict;
         }

         readonly public IFCUnit ifcUnitType;
         readonly public IDictionary<ForgeTypeId, SIUnitInfo> siUnitInfoDict;
      };

      /// <summary>
      /// A structure to contain information about IfcConversionBasedUnit
      /// </summary>
      class ConversionUnitInfo
      {
         public ConversionUnitInfo(string inConversionName, bool inIsDefault)
         {
            conversionName = inConversionName;
            isDefault = inIsDefault;
         }

         readonly public string conversionName;
         readonly public bool isDefault;
      }

      /// <summary>
      /// A structure to contain information about IfcConversionBasedUnit
      /// </summary>
      class ConversionAttributes
      {
         public ConversionAttributes(IFCUnit inUnit, string inMeasureName, ForgeTypeId inBaseSI, int inLengthExponent, int inMassExponent,
          int inTimeExponent, int inElectricCurrentExponent, int inThermodynamicTemperatureExponent,
          int inAmountOfSubstanceExponent, int inLuminousIntensityExponent, IDictionary<ForgeTypeId, ConversionUnitInfo> inConversionInfoDict)
         {
            ifcUnitType = inUnit;
            measureName = inMeasureName;
            baseSI = inBaseSI;
            length = inLengthExponent;
            mass = inMassExponent;
            time = inTimeExponent;
            electricCurrent = inElectricCurrentExponent;
            thermodynamicTemperature = inThermodynamicTemperatureExponent;
            amountOfSubstance = inAmountOfSubstanceExponent;
            luminousIntensity = inLuminousIntensityExponent;
            conversionInfoDict = inConversionInfoDict;
         }

         readonly public IFCUnit ifcUnitType;
         readonly public string measureName;
         readonly public ForgeTypeId baseSI;
         readonly public int length;
         readonly public int mass;
         readonly public int time;
         readonly public int electricCurrent;
         readonly public int thermodynamicTemperature;
         readonly public int amountOfSubstance;
         readonly public int luminousIntensity;
         readonly public IDictionary<ForgeTypeId, ConversionUnitInfo> conversionInfoDict;

      }

      /// <summary>
      /// A structure to contain information about IfcDerivedUnit
      /// </summary>
      class DerivedInfo
      {
         public DerivedInfo(double? inExtraScale, bool inIsDefault, IList<Tuple<ForgeTypeId, int>> inDerivedElements)
         {
            extraScale = inExtraScale;
            isDefault = inIsDefault;
            derivedElements = inDerivedElements;
         }

         public double? extraScale;
         public bool isDefault;
         public IList<Tuple<ForgeTypeId, int>> derivedElements;
      }

      /// <summary>
      /// A structure to contain information about IfcDerivedUnit
      /// </summary>
      class DerivedAttributes
      {
         public DerivedAttributes(Enum inDerivedUnitType, string inUserDefinedUnitName, IDictionary<ForgeTypeId, DerivedInfo> inDerivedInfoDict)
         {
            ifcDrivedUnit = inDerivedUnitType;
            userDefinedUnitName = inUserDefinedUnitName;
            derivedInfoDict = inDerivedInfoDict;
         }


         public Enum ifcDrivedUnit;
         public string userDefinedUnitName;
         public IDictionary<ForgeTypeId, DerivedInfo> derivedInfoDict;
      }
      #endregion


      #region Unit creation methods
      /// <summary>
      /// Creates eighter selected in Revit or default ifc unit of apropriate type.
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="specTypeId">Revit data type</param>
      /// <param name="byDefault">False - look for the mapping for selected Revit displayed unit.</param>
      /// True - look for the default unit in the mappings
      /// <returns>Created unit handle</returns>
      static UnitInfo CreateUnitFromMappings(IFCFile file, ForgeTypeId specTypeId, bool byDefault)
      {
         ForgeTypeId selectedUnitTypeId = null;

         if (!byDefault)
         {
            FormatOptions formatOptions = ExporterCacheManager.Document.GetUnits().GetFormatOptions(specTypeId);
            selectedUnitTypeId = formatOptions.GetUnitTypeId();

            if (selectedUnitTypeId == null)
               return null;
         }

         UnitInfo createdUnit = CreateUnitAsSI(file, specTypeId, selectedUnitTypeId);

         if (createdUnit == null)
            createdUnit = CreateUnitAsConversionBased(file, specTypeId, selectedUnitTypeId);

         if (createdUnit == null)
            createdUnit = CreateUnitAsDerived(file, specTypeId, selectedUnitTypeId);

         return createdUnit;
      }

      /// <summary>
      /// Creates IfcSIUnit handle for input data/unit types if present in mapping.
      /// If unit type is null - look for default unit.
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="specTypeId">Revit data type</param>
      /// <param name="selectedUnitTypeId">Revit unit type</param>      
      /// <returns>Created unit handle</returns>
      static UnitInfo CreateUnitAsSI(IFCFile file, ForgeTypeId specTypeId, ForgeTypeId selectedUnitTypeId)
      {
         if (file == null)
            return null;

         SIAttributes attrib;
         if (!SIUnitMapping.TryGetValue(specTypeId, out attrib))
            return null;

         if (attrib.siUnitInfoDict == null)
            return null;

         ForgeTypeId unitTypeId = null;
         IFCSIUnitName ifcSIUnitName = 0;
         IFCSIPrefix? ifcSIPrefix = null;
         IFCUnit ifcUnit;

         if (selectedUnitTypeId == null)
         {
            // Look for default unit
            foreach (var pair in attrib.siUnitInfoDict)
            {
               if (pair.Value.isDefault)
               {
                  unitTypeId = pair.Key;
                  ifcSIUnitName = pair.Value.ifcSIUnitName;
                  ifcSIPrefix = pair.Value.ifcSIPrefix;
                  break;
               }
            }
         }
         else
         {
            SIUnitInfo siInfo = null;
            if (attrib.siUnitInfoDict.TryGetValue(selectedUnitTypeId, out siInfo))
            {
               unitTypeId = selectedUnitTypeId;
               ifcSIUnitName = siInfo.ifcSIUnitName;
               ifcSIPrefix = siInfo.ifcSIPrefix;
            }
         }
         ifcUnit = attrib.ifcUnitType;

         if (unitTypeId == null)
            return null;



         IFCAnyHandle siUnit = IFCInstanceExporter.CreateSIUnit(file, ifcUnit, ifcSIPrefix, ifcSIUnitName);
         (double scaleFactor, double offset) = GetScaleFactorAndOffset(unitTypeId);
         return new UnitInfo(siUnit, scaleFactor, offset);
      }

      /// <summary>
      /// Creates IfcConversionBasedUnit handle for input data/unit types if present in mapping.
      /// If unit type is null - look for default unit.
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="specTypeId">Revit data type</param>
      /// <param name="selectedUnitTypeId">Revit unit type</param>      
      /// <returns>Created unit handle</returns>
      static UnitInfo CreateUnitAsConversionBased(IFCFile file, ForgeTypeId specTypeId, ForgeTypeId selectedUnitTypeId)
      {
         if (file == null)
            return null;

         ConversionAttributes attrib;
         if (!ConversionBasedUnitMapping.TryGetValue(specTypeId, out attrib))
            return null;

         if (attrib.conversionInfoDict == null)
            return null;

         ForgeTypeId unitTypeId = null;
         IFCUnit ifcUnit;
         string ifcMeasureName = null;
         ForgeTypeId baseTypeId = null;

         int length = 0;
         int mass = 0;
         int time = 0;
         int electricCurrent = 0;
         int thermodynamicTemperature = 0;
         int amountOfSubstance = 0;
         int luminousIntensity = 0;

         string conversionName = null;

         if (selectedUnitTypeId == null)
         {
            // Look for default unit
            foreach (var pair in attrib.conversionInfoDict)
            {
               if (pair.Value.isDefault)
               {
                  unitTypeId = pair.Key;
                  conversionName = pair.Value.conversionName;
                  break;
               }
            }
         }
         else
         {
            ConversionUnitInfo conversionInfo = null;
            if (attrib.conversionInfoDict.TryGetValue(selectedUnitTypeId, out conversionInfo))
            {
               unitTypeId = selectedUnitTypeId;
               conversionName = conversionInfo.conversionName;
            }
         }
         ifcUnit = attrib.ifcUnitType;
         ifcMeasureName = attrib.measureName;
         baseTypeId = attrib.baseSI;

         length = attrib.length;
         mass = attrib.mass;
         time = attrib.time;
         electricCurrent = attrib.electricCurrent;
         thermodynamicTemperature = attrib.thermodynamicTemperature;
         amountOfSubstance = attrib.amountOfSubstance;
         luminousIntensity = attrib.luminousIntensity;

         conversionName = GetCobieUnitName(conversionName);

         if (unitTypeId == null)
            return null;

         IFCAnyHandle siBaseUnit = GetOrCreateAuxiliaryUnit(file, baseTypeId);
         if (siBaseUnit == null)
            return null;

         double siScaleFactor = UnitUtils.Convert(1.0, unitTypeId, baseTypeId);
         IFCAnyHandle dims = IFCInstanceExporter.CreateDimensionalExponents(file, length, mass, time, electricCurrent, thermodynamicTemperature, amountOfSubstance, luminousIntensity);
         IFCAnyHandle conversionFactor = IFCInstanceExporter.CreateMeasureWithUnit(file, IFCDataUtil.CreateAsMeasure(siScaleFactor, ifcMeasureName), siBaseUnit);
         IFCAnyHandle conversionUnit = IFCInstanceExporter.CreateConversionBasedUnit(file, dims, ifcUnit, conversionName, conversionFactor);

         double scaleFactor = UnitUtils.ConvertFromInternalUnits(1.0, unitTypeId);
         return new UnitInfo(conversionUnit, scaleFactor, 0.0);
      }

      /// <summary>
      /// Creates IfcDerivedUnit handle for input data/unit types if present in mapping.
      /// If unit type is null - look for default unit.
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="specTypeId">Revit data type</param>
      /// <param name="selectedUnitTypeId">Revit unit type</param>      
      /// <returns>Created unit handle</returns>
      static UnitInfo CreateUnitAsDerived(IFCFile file, ForgeTypeId specTypeId, ForgeTypeId selectedUnitTypeId)
      {
         DerivedAttributes attrib;
         if (!DerivedUnitMapping.TryGetValue(specTypeId, out attrib))
            return null;

         if (attrib.derivedInfoDict == null)
            return null;

         return CreateUnitAsDerivedCommon(file, specTypeId, selectedUnitTypeId, attrib);
      }

      /// <summary>
      /// Creates IfcDerivedUnit handle for input data/unit types if present in mapping.
      /// If unit type is null - look for default unit.
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="specTypeId">Revit data type</param>
      /// <param name="selectedUnitTypeId">Revit unit type</param>
      /// <param name="attrib">Derived unit attributes</param>
      /// <returns>Created unit handle</returns>
      static UnitInfo CreateUnitAsDerivedCommon(IFCFile file, ForgeTypeId specTypeId, ForgeTypeId selectedUnitTypeId, DerivedAttributes attrib)
      {
         if (file == null)
            return null;

         ForgeTypeId unitTypeId = null;
         string userDefName = null;
         double? extraScale = null;
         IList<Tuple<ForgeTypeId, int>> derivedElements = null;

         if (selectedUnitTypeId == null)
         {
            // Look for default unit
            foreach (var pair in attrib.derivedInfoDict)
            {
               if (pair.Value.isDefault)
               {
                  unitTypeId = pair.Key;
                  extraScale = pair.Value.extraScale;
                  derivedElements = pair.Value.derivedElements;
                  break;
               }
            }
         }
         else
         {
            DerivedInfo derivedInfo = null;
            if (attrib.derivedInfoDict.TryGetValue(selectedUnitTypeId, out derivedInfo))
            {
               unitTypeId = selectedUnitTypeId;
               extraScale = derivedInfo.extraScale;
               derivedElements = derivedInfo.derivedElements;
            }
         }
         Enum ifcDerivedUnit = attrib.ifcDrivedUnit;
         userDefName = attrib.userDefinedUnitName;

         if (unitTypeId == null || (derivedElements?.Count ?? 0) == 0)
            return null;

         ISet<IFCAnyHandle> elements = new HashSet<IFCAnyHandle>();
         foreach (var pair in derivedElements)
         {
            IFCAnyHandle baseSIUnit = GetOrCreateAuxiliaryUnit(file, pair.Item1);
            if (baseSIUnit == null)
               return null;

            elements.Add(GetOrCreateDerivedUnitElement(file, baseSIUnit, pair.Item2));
         }

         IFCAnyHandle derivedUnitHnd = IFCInstanceExporter.CreateDerivedUnit(file, elements, ifcDerivedUnit, userDefName);

         if (!string.IsNullOrEmpty(userDefName))
         {
            string capitalName = NamingUtil.RemoveSpaces(userDefName.ToUpper());
            ExporterCacheManager.UnitsCache.RegisterUserDefinedUnit(capitalName, derivedUnitHnd);
         }

         double scaleFactor = UnitUtils.ConvertFromInternalUnits(1.0, unitTypeId);
         scaleFactor *= extraScale.HasValue ? extraScale.Value : 1.0;
         return new UnitInfo(derivedUnitHnd, scaleFactor, 0.0);

      }

      /// <summary>
      /// Creates IfcDerivedUnitElement handle with caching
      /// If unit type is null - look for default unit.
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="unit">Unit handle</param>
      /// <param name="exponent">The exponent</param>      
      /// <returns>Created IfcDerivedUnitElement handle</returns>
      static IFCAnyHandle GetOrCreateDerivedUnitElement(IFCFile file, IFCAnyHandle unit, int exponent)
      {
         Tuple<IFCAnyHandle, int> pair = Tuple.Create(unit, exponent);
         IFCAnyHandle derivedUnitElement = null;
         if (!ExporterCacheManager.UnitsCache.FindDerivedUnitElement(pair, out derivedUnitElement))
         {
            derivedUnitElement = IFCInstanceExporter.CreateDerivedUnitElement(file, unit, exponent);
            ExporterCacheManager.UnitsCache.RegisterDerivedUnit(pair, derivedUnitElement);
         }
         return derivedUnitElement;
      }

      /// <summary>
      /// Creates auxiliary unit handle with caching if present in mapping 
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="unitTypeId">Revit unit type</param>      
      /// <returns>Auxiliary unit handle</returns>
      static IFCAnyHandle GetOrCreateAuxiliaryUnit(IFCFile file, ForgeTypeId unitTypeId)
      {
         IFCAnyHandle auxiliaryUnit = null;
         if (ExporterCacheManager.UnitsCache.FindAuxiliaryUnit(unitTypeId, out auxiliaryUnit))
            return auxiliaryUnit;

         Tuple<IFCSIUnitName, IFCSIPrefix?, IFCUnit> ifcAuxilaryValue = null;
         if (AuxiliaryUnitMapping.TryGetValue(unitTypeId, out ifcAuxilaryValue) == false)
            return null;

         auxiliaryUnit = IFCInstanceExporter.CreateSIUnit(file, ifcAuxilaryValue.Item3, ifcAuxilaryValue.Item2, ifcAuxilaryValue.Item1);

         ExporterCacheManager.UnitsCache.RegisterAuxiliaryUnit(unitTypeId, auxiliaryUnit);

         return auxiliaryUnit;
      }

      /// <summary>
      /// Calculates scaling and offset values for conversion 
      /// from Revit internal units
      /// </summary>
      /// <param name="unitTypeId">Revit unit type</param>      
      /// <returns>Auxiliary unit handle</returns>
      static (double offset, double scaleFactor) GetScaleFactorAndOffset(ForgeTypeId unitTypeId)
      {
         double value0 = UnitUtils.ConvertFromInternalUnits(0.0, unitTypeId);
         double value1 = UnitUtils.ConvertFromInternalUnits(1.0, unitTypeId);

         double offset = value0;
         double scaleFactor = (value1 - value0);
         if (MathUtil.IsAlmostZero(scaleFactor))
            scaleFactor = 1.0;

         return (scaleFactor, offset);
      }

      /// <summary>
      /// Get mapped unit name for ExportAsCOBIE
      /// </summary>
      static string GetCobieUnitName(string unitName)
      {
         if (!ExporterCacheManager.ExportOptionsCache.ExportAsCOBIE)
            return unitName;

         string cobieName;
         if (!CobieUnitNameMapping.TryGetValue(unitName, out cobieName))
            cobieName = unitName.ToLower();

         return cobieName;
      }

      #endregion


      #region Unit Mapping Tables
      /// <summary>
      /// The dictionary contains the information to create auxiliary ifc unit for a Revit unit type
      /// These are the auxiliary unit handles that don't go to IfcUnitAssignment
      /// </summary>
      private static readonly Dictionary<ForgeTypeId, Tuple<IFCSIUnitName, IFCSIPrefix?, IFCUnit>> AuxiliaryUnitMapping = new Dictionary<ForgeTypeId, Tuple<IFCSIUnitName, IFCSIPrefix?, IFCUnit>>()
      {
         { UnitTypeId.Meters,       Tuple.Create<IFCSIUnitName, IFCSIPrefix?, IFCUnit>(IFCSIUnitName.Metre, null, IFCUnit.LengthUnit) },
         { UnitTypeId.Decimeters,   Tuple.Create<IFCSIUnitName, IFCSIPrefix?, IFCUnit>(IFCSIUnitName.Metre, IFCSIPrefix.Deci, IFCUnit.LengthUnit) },
         { UnitTypeId.SquareMeters, Tuple.Create<IFCSIUnitName, IFCSIPrefix?, IFCUnit>(IFCSIUnitName.Square_Metre, null, IFCUnit.AreaUnit) },
         { UnitTypeId.CubicMeters,  Tuple.Create<IFCSIUnitName, IFCSIPrefix?, IFCUnit>(IFCSIUnitName.Cubic_Metre, null, IFCUnit.VolumeUnit) },
         { UnitTypeId.Kilograms,    Tuple.Create<IFCSIUnitName, IFCSIPrefix?, IFCUnit>(IFCSIUnitName.Gram, IFCSIPrefix.Kilo, IFCUnit.MassUnit) },
         { UnitTypeId.Seconds,      Tuple.Create<IFCSIUnitName, IFCSIPrefix?, IFCUnit>(IFCSIUnitName.Second, null, IFCUnit.TimeUnit) },
         { UnitTypeId.Amperes,      Tuple.Create<IFCSIUnitName, IFCSIPrefix?, IFCUnit>(IFCSIUnitName.Ampere, null, IFCUnit.ElectricCurrentUnit) },
         { UnitTypeId.Kelvin,       Tuple.Create<IFCSIUnitName, IFCSIPrefix?, IFCUnit>(IFCSIUnitName.Kelvin, null, IFCUnit.ThermoDynamicTemperatureUnit) },
         { UnitTypeId.Candelas,     Tuple.Create<IFCSIUnitName, IFCSIPrefix?, IFCUnit>(IFCSIUnitName.Candela, null, IFCUnit.LuminousIntensityUnit) },
         { UnitTypeId.Radians,      Tuple.Create<IFCSIUnitName, IFCSIPrefix?, IFCUnit>(IFCSIUnitName.Radian, null, IFCUnit.PlaneAngleUnit) },
         { UnitTypeId.Lumens,       Tuple.Create<IFCSIUnitName, IFCSIPrefix?, IFCUnit>(IFCSIUnitName.Lumen, null, IFCUnit.LuminousFluxUnit) },
         { UnitTypeId.Newtons,      Tuple.Create<IFCSIUnitName, IFCSIPrefix?, IFCUnit>(IFCSIUnitName.Newton, null, IFCUnit.ForceUnit) },

      };


      /// <summary>
      /// The dictionary contains the information to create IfcSIUnit for a Revit data type
      /// </summary>
      private static readonly Dictionary<ForgeTypeId, SIAttributes> SIUnitMapping = new Dictionary<ForgeTypeId, SIAttributes>()
      {
         { SpecTypeId.Mass, new SIAttributes(IFCUnit.MassUnit, new Dictionary<ForgeTypeId, SIUnitInfo>()
            {
               { UnitTypeId.Kilograms, new SIUnitInfo(IFCSIUnitName.Gram, IFCSIPrefix.Kilo, true) }
            } )
         },
         { SpecTypeId.Time, new SIAttributes(IFCUnit.TimeUnit, new Dictionary<ForgeTypeId, SIUnitInfo>()
            {
               { UnitTypeId.Seconds, new SIUnitInfo(IFCSIUnitName.Second, null, true) }
            } )
         },
         { SpecTypeId.Current, new SIAttributes(IFCUnit.ElectricCurrentUnit, new Dictionary<ForgeTypeId, SIUnitInfo>()
            {
               { UnitTypeId.Amperes, new SIUnitInfo(IFCSIUnitName.Ampere, null, true) }
            } )
         },
         { SpecTypeId.LuminousIntensity, new SIAttributes(IFCUnit.LuminousIntensityUnit, new Dictionary<ForgeTypeId, SIUnitInfo>()
            {
               { UnitTypeId.Candelas, new SIUnitInfo(IFCSIUnitName.Candela, null, true) }
            } )
         },
         { SpecTypeId.HvacTemperature, new SIAttributes(IFCUnit.ThermoDynamicTemperatureUnit, new Dictionary<ForgeTypeId, SIUnitInfo>()
            {
               { UnitTypeId.Celsius, new SIUnitInfo(IFCSIUnitName.Degree_Celsius, null, true) },
               { UnitTypeId.Kelvin, new SIUnitInfo(IFCSIUnitName.Kelvin, null, false) }
            } )
         },
         { SpecTypeId.Length, new SIAttributes(IFCUnit.LengthUnit, new Dictionary<ForgeTypeId, SIUnitInfo>()
            {
               { UnitTypeId.Meters, new SIUnitInfo(IFCSIUnitName.Metre, null, false) },
               { UnitTypeId.MetersCentimeters, new SIUnitInfo(IFCSIUnitName.Metre, null, false) },
               { UnitTypeId.Centimeters, new SIUnitInfo(IFCSIUnitName.Metre, IFCSIPrefix.Centi, false) },
               { UnitTypeId.Millimeters, new SIUnitInfo(IFCSIUnitName.Metre, IFCSIPrefix.Milli, false) }
            } )
         },
         { SpecTypeId.Area, new SIAttributes(IFCUnit.AreaUnit, new Dictionary<ForgeTypeId, SIUnitInfo>()
            {
               { UnitTypeId.SquareMeters, new SIUnitInfo(IFCSIUnitName.Square_Metre, null, false) },
               { UnitTypeId.SquareCentimeters, new SIUnitInfo(IFCSIUnitName.Square_Metre, IFCSIPrefix.Centi, false) },
               { UnitTypeId.Millimeters, new SIUnitInfo(IFCSIUnitName.Square_Metre, IFCSIPrefix.Milli, false) }
            } )
         },
         { SpecTypeId.Volume, new SIAttributes(IFCUnit.VolumeUnit, new Dictionary<ForgeTypeId, SIUnitInfo>()
            {
               { UnitTypeId.CubicMeters, new SIUnitInfo(IFCSIUnitName.Cubic_Metre, null, false) },
               { UnitTypeId.Liters, new SIUnitInfo(IFCSIUnitName.Cubic_Metre, IFCSIPrefix.Deci, false) },
               { UnitTypeId.CubicCentimeters, new SIUnitInfo(IFCSIUnitName.Cubic_Metre, IFCSIPrefix.Centi, false) },
               { UnitTypeId.CubicMillimeters, new SIUnitInfo(IFCSIUnitName.Cubic_Metre, IFCSIPrefix.Milli, false) }
            } )
         },
         { SpecTypeId.Angle, new SIAttributes(IFCUnit.PlaneAngleUnit, new Dictionary<ForgeTypeId, SIUnitInfo>()
            {
               { UnitTypeId.Radians, new SIUnitInfo(IFCSIUnitName.Radian, null, false) }
            } )
         },
         { SpecTypeId.Force, new SIAttributes(IFCUnit.ForceUnit, new Dictionary<ForgeTypeId, SIUnitInfo>()
            {
               { UnitTypeId.Newtons, new SIUnitInfo(IFCSIUnitName.Newton, null, true) },
               { UnitTypeId.Dekanewtons, new SIUnitInfo(IFCSIUnitName.Newton, IFCSIPrefix.Deca, false) },
               { UnitTypeId.Kilonewtons, new SIUnitInfo(IFCSIUnitName.Newton, IFCSIPrefix.Kilo, false) },
               { UnitTypeId.Meganewtons, new SIUnitInfo(IFCSIUnitName.Newton, IFCSIPrefix.Mega, false) }
            } )
         },
         { SpecTypeId.ElectricalFrequency, new SIAttributes(IFCUnit.FrequencyUnit, new Dictionary<ForgeTypeId, SIUnitInfo>()
            {
               { UnitTypeId.Hertz, new SIUnitInfo(IFCSIUnitName.Hertz, null, true) }
            } )
         },
         { SpecTypeId.ElectricalPotential, new SIAttributes(IFCUnit.ElectricVoltageUnit, new Dictionary<ForgeTypeId, SIUnitInfo>()
            {
               { UnitTypeId.Volts, new SIUnitInfo(IFCSIUnitName.Volt, null, true) }
            } )
         },
         { SpecTypeId.HvacPower, new SIAttributes(IFCUnit.PowerUnit, new Dictionary<ForgeTypeId, SIUnitInfo>()
            {
               { UnitTypeId.Watts, new SIUnitInfo(IFCSIUnitName.Watt, null, true) }
            } )
         },
         { SpecTypeId.Illuminance, new SIAttributes(IFCUnit.IlluminanceUnit, new Dictionary<ForgeTypeId, SIUnitInfo>()
            {
               { UnitTypeId.Lux, new SIUnitInfo(IFCSIUnitName.Lux, null, true) }
            } )
         },
         { SpecTypeId.LuminousFlux, new SIAttributes(IFCUnit.LuminousFluxUnit, new Dictionary<ForgeTypeId, SIUnitInfo>()
            {
               { UnitTypeId.Lumens, new SIUnitInfo(IFCSIUnitName.Lumen, null, true) }
            } )
         },
         { SpecTypeId.Energy, new SIAttributes(IFCUnit.EnergyUnit, new Dictionary<ForgeTypeId, SIUnitInfo>()
            {
               { UnitTypeId.Joules, new SIUnitInfo(IFCSIUnitName.Joule, null, true) }
            } )
         },
         { SpecTypeId.HvacPressure, new SIAttributes(IFCUnit.PressureUnit, new Dictionary<ForgeTypeId, SIUnitInfo>()
            {
               { UnitTypeId.Pascals, new SIUnitInfo(IFCSIUnitName.Pascal, null, true) },
               { UnitTypeId.Kilopascals, new SIUnitInfo(IFCSIUnitName.Pascal, IFCSIPrefix.Kilo, false) },
               { UnitTypeId.Megapascals, new SIUnitInfo(IFCSIUnitName.Pascal, IFCSIPrefix.Mega, false) }
            } )
         }
      };


      /// <summary>
      /// The dictionary contains the information to create IfcConversionBasedUnit for a Revit data type
      /// </summary>
      private static readonly Dictionary<ForgeTypeId, ConversionAttributes> ConversionBasedUnitMapping = new Dictionary<ForgeTypeId, ConversionAttributes>()
      {
         { SpecTypeId.Length,  new ConversionAttributes(IFCUnit.LengthUnit, "IfcLengthMeasure", UnitTypeId.Meters, 1, 0, 0, 0, 0, 0, 0, new Dictionary<ForgeTypeId, ConversionUnitInfo>() {
            { UnitTypeId.Feet, new ConversionUnitInfo("FOOT", true) },
            { UnitTypeId.FeetFractionalInches, new ConversionUnitInfo("FOOT", false)},
            { UnitTypeId.Inches, new ConversionUnitInfo("INCH", false)},
            { UnitTypeId.FractionalInches, new ConversionUnitInfo("INCH", false) } } )
         },
         { SpecTypeId.Area,  new ConversionAttributes(IFCUnit.AreaUnit, "IfcAreaMeasure", UnitTypeId.SquareMeters, 2, 0, 0, 0, 0, 0, 0, new Dictionary<ForgeTypeId, ConversionUnitInfo>() {
            { UnitTypeId.SquareFeet, new ConversionUnitInfo("SQUARE FOOT", true) },
            { UnitTypeId.SquareInches, new ConversionUnitInfo("SQUARE INCH", false) } } )
         },
         { SpecTypeId.Volume,  new ConversionAttributes(IFCUnit.VolumeUnit, "IfcVolumeMeasure", UnitTypeId.CubicMeters, 3, 0, 0, 0, 0, 0, 0, new Dictionary<ForgeTypeId, ConversionUnitInfo>() {
            { UnitTypeId.CubicFeet, new ConversionUnitInfo("CUBIC FOOT", true) },
            { UnitTypeId.CubicInches, new ConversionUnitInfo("CUBIC INCH", false) } } )
         },
         { SpecTypeId.Angle,  new ConversionAttributes(IFCUnit.PlaneAngleUnit, "IfcPlaneAngleMeasure", UnitTypeId.Radians, 0, 0, 0, 0, 0, 0, 0, new Dictionary<ForgeTypeId, ConversionUnitInfo>() {
            { UnitTypeId.Degrees, new ConversionUnitInfo("DEGREE", true) },
            { UnitTypeId.DegreesMinutes, new ConversionUnitInfo("DEGREE", false) },
            { UnitTypeId.Gradians, new ConversionUnitInfo("GRAD", false) } } )
         },
         { SpecTypeId.Force,  new ConversionAttributes(IFCUnit.ForceUnit, "IfcForceMeasure", UnitTypeId.Newtons, 1, 1, -2, 0, 0, 0, 0, new Dictionary<ForgeTypeId, ConversionUnitInfo>() {
            { UnitTypeId.KilogramsForce, new ConversionUnitInfo("KILOGRAM-FORCE", false) },
            { UnitTypeId.TonnesForce, new ConversionUnitInfo("TONN-FORCE", false) },
            { UnitTypeId.UsTonnesForce, new ConversionUnitInfo("USTONN-FORCE", false) },
            { UnitTypeId.PoundsForce, new ConversionUnitInfo("POUND-FORCE", false) },
            { UnitTypeId.Kips, new ConversionUnitInfo("CUBIC INCH", false) } } )
         }
      };

      /// <summary>
      /// The dictionary contains the information to create IfcDerivedUnit for a Revit data type
      /// </summary>
      private static readonly Dictionary<ForgeTypeId, DerivedAttributes> DerivedUnitMapping = new Dictionary<ForgeTypeId, DerivedAttributes>()
      {
         { SpecTypeId.MassDensity, new DerivedAttributes(IFCDerivedUnitEnum.MassDensityUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.KilogramsPerCubicMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, -3) } )
            } } )
         },
         { SpecTypeId.PipingDensity, new DerivedAttributes(IFCDerivedUnitEnum.IonConcentrationUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.KilogramsPerCubicMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, -3) } )
            } } )
         },
         { SpecTypeId.MomentOfInertia, new DerivedAttributes(IFCDerivedUnitEnum.MomentOfInertiaUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.MetersToTheFourthPower, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Meters, 4) } )
            } } )
         },
         { SpecTypeId.HeatTransferCoefficient, new DerivedAttributes(IFCDerivedUnitEnum.ThermalTransmittanceUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.WattsPerSquareMeterKelvin, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Kelvin, -1),
               Tuple.Create(UnitTypeId.Seconds, -3) } )
            } } )
         },
         { SpecTypeId.ThermalConductivity, new DerivedAttributes(IFCDerivedUnitEnum.ThermalConductanceUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.WattsPerMeterKelvin, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, 1),
               Tuple.Create(UnitTypeId.Kelvin, -1),
               Tuple.Create(UnitTypeId.Seconds, -3) } )
            } } )
         },
         { SpecTypeId.AirFlow, new DerivedAttributes(IFCDerivedUnitEnum.VolumetricFlowRateUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.CubicMetersPerSecond, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Meters, 3),
               Tuple.Create(UnitTypeId.Seconds, -1) } )
            },
            { UnitTypeId.LitersPerSecond, new DerivedInfo(null, false, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Decimeters, 3),
               Tuple.Create(UnitTypeId.Seconds, -1) } )
            } } )
         },
         { SpecTypeId.PipingMassPerTime, new DerivedAttributes(IFCDerivedUnitEnum.MassFlowRateUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.KilogramsPerSecond, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Seconds, -1) } )
            } } )
         },
         { SpecTypeId.AngularSpeed, new DerivedAttributes(IFCDerivedUnitEnum.RotationalFrequencyUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.RevolutionsPerSecond, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Seconds, -1) } )
            } } )
         },
         { SpecTypeId.Wattage, new DerivedAttributes(IFCDerivedUnitEnum.SoundPowerUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.Watts, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, 2),
               Tuple.Create(UnitTypeId.Seconds, -3) } )
            } } )
         },
         { SpecTypeId.HvacPressure, new DerivedAttributes(IFCDerivedUnitEnum.SoundPressureUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.Pascals, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, -1),
               Tuple.Create(UnitTypeId.Seconds, -2) } )
            } } )
         },
         { SpecTypeId.HvacVelocity, new DerivedAttributes(IFCDerivedUnitEnum.LinearVelocityUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.MetersPerSecond, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Meters, 1),
               Tuple.Create(UnitTypeId.Seconds, -1) } )
            } } )
         },
         { SpecTypeId.LinearForce, new DerivedAttributes(IFCDerivedUnitEnum.LinearForceUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.NewtonsPerMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Seconds, -2) } )
            } } )
         },
         { SpecTypeId.AreaForce, new DerivedAttributes(IFCDerivedUnitEnum.PlanarForceUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.NewtonsPerSquareMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, -1),
               Tuple.Create(UnitTypeId.Seconds, -2) } )
            } } )
         },
         { SpecTypeId.SpecificHeat, new DerivedAttributes(IFCDerivedUnitEnum.SpecificHeatCapacityUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.JoulesPerKilogramDegreeCelsius, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Meters, 2),
               Tuple.Create(UnitTypeId.Seconds, -2),
               Tuple.Create(UnitTypeId.Kelvin, -1) } )
            } } )
         },
         { SpecTypeId.HvacPowerDensity, new DerivedAttributes(IFCDerivedUnitEnum.HeatFluxDensityUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.WattsPerSquareMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Seconds, -3) } )
            } } )
         },
         { SpecTypeId.SpecificHeatOfVaporization, new DerivedAttributes(IFCDerivedUnitEnum.HeatingValueUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.JoulesPerGram, new DerivedInfo(1.0e+3, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Meters, 2),
               Tuple.Create(UnitTypeId.Seconds, -2) } )
            } } )
         },
         { SpecTypeId.Permeability, new DerivedAttributes(IFCDerivedUnitEnum.VaporPermeabilityUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.NanogramsPerPascalSecondSquareMeter, new DerivedInfo(1.0e-12, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Meters, -1),
               Tuple.Create(UnitTypeId.Seconds, 1) } )
            } } )
         },
         { SpecTypeId.HvacViscosity, new DerivedAttributes(IFCDerivedUnitEnum.DynamicViscosityUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.KilogramsPerMeterSecond, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, -1),
               Tuple.Create(UnitTypeId.Seconds, -1) } )
            } } )
         },
         { SpecTypeId.ThermalExpansionCoefficient, new DerivedAttributes(IFCDerivedUnitEnum.ThermalExpansionCoefficientUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.InverseDegreesCelsius, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kelvin, -1) } )
            } } )
         },
         { SpecTypeId.Stress, new DerivedAttributes(IFCDerivedUnitEnum.ModulusOfElasticityUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.Pascals, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, -1),
               Tuple.Create(UnitTypeId.Seconds, -2) } )
            } } )
         },
         { SpecTypeId.IsothermalMoistureCapacity, new DerivedAttributes(IFCDerivedUnitEnum.IsothermalMoistureCapacityUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.CubicMetersPerKilogram, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, -1),
               Tuple.Create(UnitTypeId.Meters, 3) } )
            } } )
         },

         { SpecTypeId.Diffusivity, new DerivedAttributes(IFCDerivedUnitEnum.MoistureDiffusivityUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.SquareMetersPerSecond, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Meters, 2),
               Tuple.Create(UnitTypeId.Seconds, -1) } )
            } } )
         },
         { SpecTypeId.MassPerUnitLength, new DerivedAttributes(IFCDerivedUnitEnum.MassPerLengthUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.KilogramsPerMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, -1) } )
            } } )
         },
         { SpecTypeId.ThermalResistance, new DerivedAttributes(IFCDerivedUnitEnum.ThermalResistanceUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.SquareMeterKelvinsPerWatt, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, -1),
               Tuple.Create(UnitTypeId.Seconds, 3),
               Tuple.Create(UnitTypeId.Kelvin, 1) } )
            } } )
         },
         { SpecTypeId.Acceleration, new DerivedAttributes(IFCDerivedUnitEnum.AccelerationUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.MetersPerSecondSquared, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Meters, 1),
               Tuple.Create(UnitTypeId.Seconds, -2) } )
            } } )
         },
         { SpecTypeId.Pulsation, new DerivedAttributes(IFCDerivedUnitEnum.AngularVelocityUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.RadiansPerSecond, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Radians, 1),
               Tuple.Create(UnitTypeId.Seconds, -1) } )
            } } )
         },
         { SpecTypeId.PointSpringCoefficient, new DerivedAttributes(IFCDerivedUnitEnum.LinearStiffnessUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.NewtonsPerMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Seconds, -2) } )
            } } )
         },
         { SpecTypeId.WarpingConstant, new DerivedAttributes(IFCDerivedUnitEnum.WarpingConstantUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.MetersToTheSixthPower, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Meters, 6) } )
            } } )
         },
         { SpecTypeId.LinearMoment, new DerivedAttributes(IFCDerivedUnitEnum.LinearMomentUnit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.NewtonMetersPerMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, 1),
               Tuple.Create(UnitTypeId.Seconds, -2) } )
            } } )
         },
         { SpecTypeId.Moment, new DerivedAttributes(IFCDerivedUnitEnum.Torqueunit, null, new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.NewtonMeters, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, 2),
               Tuple.Create(UnitTypeId.Seconds, -2) } )
            } } )
         },
         { SpecTypeId.CostPerArea, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Cost Per Area", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.CurrencyPerSquareMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Meters, -2) } )
            } } )
         },
         { SpecTypeId.ApparentPowerDensity, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Apparent Power Density", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.VoltAmperesPerSquareMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Seconds, -3) } )
            } } )
         },
         { SpecTypeId.CostRateEnergy, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Cost Rate Energy", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.CurrencyPerWattHour, new DerivedInfo(1.0 / 3600.0, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, -1),
               Tuple.Create(UnitTypeId.Meters, -2),
               Tuple.Create(UnitTypeId.Seconds, 2) } )
            } } )
         },
         { SpecTypeId.CostRatePower, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Cost Rate Power", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.CurrencyPerWatt, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, -1),
               Tuple.Create(UnitTypeId.Meters, -2),
               Tuple.Create(UnitTypeId.Seconds, 3) } )
            } } )
         },
         { SpecTypeId.Efficacy, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Luminous Efficacy", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.LumensPerWatt, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, -1),
               Tuple.Create(UnitTypeId.Meters, -2),
               Tuple.Create(UnitTypeId.Seconds, 3),
               Tuple.Create(UnitTypeId.Lumens, 1) } )
            } } )
         },
         { SpecTypeId.Luminance, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Luminance", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.CandelasPerSquareMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Meters, -2),
               Tuple.Create(UnitTypeId.Candelas, 1) } )
            } } )
         },
         { SpecTypeId.ElectricalPowerDensity, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Electrical Power Density", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.WattsPerSquareMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Seconds, -3) } )
            } } )
         },
         { SpecTypeId.PowerPerLength, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Power Per Length", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.WattsPerMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, 1),
               Tuple.Create(UnitTypeId.Seconds, -3) } )
            } } )
         },
         { SpecTypeId.ElectricalResistivity, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Electrical Resistivity", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.OhmMeters, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, 3),
               Tuple.Create(UnitTypeId.Seconds, -3),
               Tuple.Create(UnitTypeId.Amperes, -2) } )
            } } )
         },
         { SpecTypeId.HeatCapacityPerArea, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Heat Capacity Per Area", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.JoulesPerSquareMeterKelvin, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Seconds, -2),
               Tuple.Create(UnitTypeId.Kelvin, -1) } )
            } } )
         },
         { SpecTypeId.ThermalGradientCoefficientForMoistureCapacity, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Thermal Gradient Coefficient For Moisture Capacity", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.KilogramsPerKilogramKelvin, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kelvin, -1) } )
            } } )
         },
         { SpecTypeId.ThermalMass, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Thermal Mass", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.JoulesPerKelvin, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, 2),
               Tuple.Create(UnitTypeId.Seconds, -2),
               Tuple.Create(UnitTypeId.Kelvin, -1) } )
            } } )
         },
         { SpecTypeId.AirFlowDensity, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Air Flow Density", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.CubicMetersPerHourSquareMeter, new DerivedInfo(1.0 / 3600.0, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Meters, 1),
               Tuple.Create(UnitTypeId.Seconds, -1) } )
            } } )
         },
         { SpecTypeId.AirFlowDividedByCoolingLoad, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Air Flow Divided By Cooling Load", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.LitersPerSecondKilowatt, new DerivedInfo(1.0e-3 * 1.0e-3, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, -1),
               Tuple.Create(UnitTypeId.Meters, 1),
               Tuple.Create(UnitTypeId.Seconds, 2) } )
            } } )
         },
         { SpecTypeId.AirFlowDividedByVolume, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Air Flow Divided By Volume", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.CubicMetersPerHourCubicMeter, new DerivedInfo(1.0 / 3600.0, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Seconds, -1) } )
            } } )
         },
         { SpecTypeId.AreaDividedByCoolingLoad, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Area Divided By Cooling Load", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.SquareMetersPerKilowatt, new DerivedInfo(1.0e-3, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, -1),
               Tuple.Create(UnitTypeId.Seconds, 3) } )
            } } )
         },
         { SpecTypeId.AreaDividedByHeatingLoad, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Area Divided By Heating Load", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.SquareMetersPerKilowatt, new DerivedInfo(1.0e-3, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, -1),
               Tuple.Create(UnitTypeId.Seconds, 3) } )
            } } )
         },
         { SpecTypeId.CoolingLoadDividedByArea, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Cooling Load Divided By Area", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.WattsPerSquareMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Seconds, -3) } )
            } } )
         },
         { SpecTypeId.CoolingLoadDividedByVolume, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Cooling Load Divided By Volume", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.WattsPerCubicMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, -1),
               Tuple.Create(UnitTypeId.Seconds, -3) } )
            } } )
         },
         { SpecTypeId.FlowPerPower, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Flow Per Power", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.CubicMetersPerWattSecond, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, -1),
               Tuple.Create(UnitTypeId.Meters, 1),
               Tuple.Create(UnitTypeId.Seconds, 2) } )
            } } )
         },
         { SpecTypeId.HvacFriction, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Friction Loss", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.PascalsPerMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, -2),
               Tuple.Create(UnitTypeId.Seconds, -2) } )
            } } )
         },
         { SpecTypeId.HeatingLoadDividedByArea, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Heating Load Divided By Area", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.WattsPerSquareMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Seconds, -3) } )
            } } )
         },
         { SpecTypeId.HeatingLoadDividedByVolume, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Heating Load Divided By Volume", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.WattsPerCubicMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, -1),
               Tuple.Create(UnitTypeId.Seconds, -3) } )
            } } )
         },
         { SpecTypeId.PowerPerFlow, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Power Per Flow", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.WattsPerCubicMeterPerSecond, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, -1),
               Tuple.Create(UnitTypeId.Seconds, -2) } )
            } } )
         },
         { SpecTypeId.PipingFriction, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Piping Friction", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.PascalsPerMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, -2),
               Tuple.Create(UnitTypeId.Seconds, -2) } )
            } } )
         },
         { SpecTypeId.AreaSpringCoefficient, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Area Spring Coefficient", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.PascalsPerMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, -2),
               Tuple.Create(UnitTypeId.Seconds, -2) } )
            } } )
         },
         { SpecTypeId.LineSpringCoefficient, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Line Spring Coefficient", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.Pascals, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, -1),
               Tuple.Create(UnitTypeId.Seconds, -2) } )
            } } )
         },
         { SpecTypeId.MassPerUnitArea, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Mass Per Unit Area", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.KilogramsPerSquareMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, -2) } )
            } } )
         },
         { SpecTypeId.ReinforcementAreaPerUnitLength, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Reinforcement Area Per Unit Length", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.SquareMetersPerMeter, new DerivedInfo(null, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Meters, 2),
               Tuple.Create(UnitTypeId.Meters, -1) } )
            } } )
         },
         { SpecTypeId.RotationalLineSpringCoefficient, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Rotational Line Spring Coefficient", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.KilonewtonMetersPerDegreePerMeter, new DerivedInfo(1.0e+3 * 180.0 / Math.PI, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, 1),
               Tuple.Create(UnitTypeId.Seconds, -2),
               Tuple.Create(UnitTypeId.Radians, -1) } )
            } } )
         },
         { SpecTypeId.RotationalPointSpringCoefficient, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Rotational Point Spring Coefficient", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.KilonewtonMetersPerDegree, new DerivedInfo(1.0e+3 * 180.0 / Math.PI, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, 2),
               Tuple.Create(UnitTypeId.Seconds, -2),
               Tuple.Create(UnitTypeId.Radians, -1) } )
            } } )
         },
         { SpecTypeId.UnitWeight, new DerivedAttributes(IFCDerivedUnitEnum.UserDefined, "Unit Weight", new Dictionary<ForgeTypeId, DerivedInfo>() {
            { UnitTypeId.KilonewtonsPerCubicMeter, new DerivedInfo(1.0e+3, true, new List<Tuple<ForgeTypeId, int>>() {
               Tuple.Create(UnitTypeId.Kilograms, 1),
               Tuple.Create(UnitTypeId.Meters, -2),
               Tuple.Create(UnitTypeId.Seconds, -2) } )
            } } )
         }
      };

      /// <summary>
      /// The dictionary contains specific unit name mapping for ExportAsCOBIE
      /// </summary>
      private static readonly Dictionary<string, string> CobieUnitNameMapping = new Dictionary<string, string>()
      {
         { "SQUARE INCH", "inch" },
         { "SQUARE FOOT", "foot" },
         { "CUBIC INCH", "inch" },
         { "CUBIC FOOT", "foot" }
      };

      /// <summary>
      /// The dictionary contains Ifc to Revit data type mapping
      /// </summary>
      private static readonly Dictionary<string, ForgeTypeId> IfcToRevitDataTypeMapping = new Dictionary<string, ForgeTypeId>()
      {
         { "IfcAccelerationMeasure", SpecTypeId.Acceleration },
         { "IfcAngularVelocityMeasure", SpecTypeId.Pulsation },
         { "IfcAreaDensityMeasure", SpecTypeId.MassPerUnitArea },
         { "IfcAreaMeasure", SpecTypeId.Area },
         { "IfcDynamicViscosityMeasure", SpecTypeId.HvacViscosity },
         { "IfcElectricCurrentMeasure", SpecTypeId.Current },
         { "IfcElectricVoltageMeasure", SpecTypeId.ElectricalPotential },
         { "IfcEnergyMeasure", SpecTypeId.Energy },
         { "IfcForceMeasure", SpecTypeId.Force },
         { "IfcFrequencyMeasure", SpecTypeId.ElectricalFrequency },
         { "IfcHeatFluxDensityMeasure", SpecTypeId.HvacPowerDensity },
         { "IfcHeatingValueMeasure", SpecTypeId.SpecificHeatOfVaporization },
         { "IfcIlluminanceMeasure", SpecTypeId.Illuminance },
         { "IfcIonConcentrationMeasure", SpecTypeId.PipingDensity },
         { "IfcIsothermalMoistureCapacityMeasure", SpecTypeId.IsothermalMoistureCapacity },
         { "IfcLengthMeasure", SpecTypeId.Length },
         { "IfcLinearForceMeasure", SpecTypeId.LinearForce },
         { "IfcLinearMomentMeasure", SpecTypeId.LinearMoment },
         { "IfcLinearStiffnessMeasure", SpecTypeId.PointSpringCoefficient },
         { "IfcLinearVelocityMeasure", SpecTypeId.HvacVelocity },
         { "IfcLuminousFluxMeasure", SpecTypeId.LuminousFlux },
         { "IfcLuminousIntensityMeasure", SpecTypeId.LuminousIntensity },
         { "IfcMassDensityMeasure", SpecTypeId.MassDensity },
         { "IfcMassFlowRateMeasure", SpecTypeId.PipingMassPerTime },
         { "IfcMassMeasure", SpecTypeId.Mass },
         { "IfcMassPerLengthMeasure", SpecTypeId.MassPerUnitLength },
         { "IfcModulusOfElasticityMeasure", SpecTypeId.Stress },
         { "IfcMoistureDiffusivityMeasure", SpecTypeId.Diffusivity },
         { "IfcMomentOfInertiaMeasure", SpecTypeId.MomentOfInertia },
         { "IfcPlanarForceMeasure", SpecTypeId.AreaForce },
         { "IfcPlaneAngleMeasure", SpecTypeId.Angle },
         { "IfcPositiveLengthMeasure", SpecTypeId.Length },
         { "IfcPositivePlaneAngleMeasure", SpecTypeId.Angle },
         { "IfcPowerMeasure", SpecTypeId.HvacPower },
         { "IfcPressureMeasure", SpecTypeId.HvacPressure },
         { "IfcRotationalFrequencyMeasure", SpecTypeId.AngularSpeed },
         { "IfcSoundPowerMeasure", SpecTypeId.Wattage },
         { "IfcSoundPressureMeasure", SpecTypeId.HvacPressure },
         { "IfcSpecificHeatCapacityMeasure", SpecTypeId.SpecificHeat },
         { "IfcThermalConductivityMeasure", SpecTypeId.ThermalConductivity },
         { "IfcThermalExpansionCoefficientMeasure", SpecTypeId.ThermalExpansionCoefficient },
         { "IfcThermalResistanceMeasure", SpecTypeId.ThermalResistance },
         { "IfcThermalTransmittanceMeasure", SpecTypeId.HeatTransferCoefficient },
         { "IfcThermodynamicTemperatureMeasure", SpecTypeId.HvacTemperature },
         { "IfcTimeMeasure", SpecTypeId.Time },
         { "IfcTorqueMeasure", SpecTypeId.Moment },
         { "IfcVolumeMeasure", SpecTypeId.Volume },
         { "IfcVolumetricFlowRateMeasure", SpecTypeId.AirFlow },
         { "IfcWarpingConstantMeasure", SpecTypeId.WarpingConstant },
      };
      #endregion

   }
}
