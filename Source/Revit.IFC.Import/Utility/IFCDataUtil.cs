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
using Revit.IFC.Import.Data;

namespace Revit.IFC.Import.Utility
{
   /// <summary>
   /// Provides utility methods for IFCData class.
   /// </summary>
   public class IFCDataUtil
   {
      static private IDictionary<string, UnitType> m_MeasureCache = null;

      static private void InitializeMeasureCache()
      {
         m_MeasureCache = new SortedDictionary<string, UnitType>(StringComparer.InvariantCultureIgnoreCase);

         m_MeasureCache["IfcAreaMeasure"] = UnitType.UT_Area;
         m_MeasureCache["IfcCountMeasure"] = UnitType.UT_Number;
         m_MeasureCache["IfcElectricCurrentMeasure"] = UnitType.UT_Electrical_Current;
         m_MeasureCache["IfcElectricVoltageMeasure"] = UnitType.UT_Electrical_Potential;
         m_MeasureCache["IfcForceMeasure"] = UnitType.UT_Force;
         m_MeasureCache["IfcFrequencyMeasure"] = UnitType.UT_Electrical_Frequency;
         m_MeasureCache["IfcLengthMeasure"] = UnitType.UT_Length;
         m_MeasureCache["IfcIlluminanceMeasure"] = UnitType.UT_Electrical_Illuminance;
         m_MeasureCache["IfcInteger"] = UnitType.UT_Number;
         m_MeasureCache["IfcLinearVelocityMeasure"] = UnitType.UT_HVAC_Velocity;
         m_MeasureCache["IfcLuminousFluxMeasure"] = UnitType.UT_Electrical_Luminous_Flux;
         m_MeasureCache["IfcLuminousIntensityMeasure"] = UnitType.UT_Electrical_Luminous_Intensity;
         m_MeasureCache["IfcMassMeasure"] = UnitType.UT_Mass;
         m_MeasureCache["IfcMassDensityMeasure"] = UnitType.UT_MassDensity;
         m_MeasureCache["IfcMonetaryMeasure"] = UnitType.UT_Currency;
         m_MeasureCache["IfcNumericMeasure"] = UnitType.UT_Number;
         m_MeasureCache["IfcPositiveRatioMeasure"] = UnitType.UT_Number;
         m_MeasureCache["IfcPositiveLengthMeasure"] = UnitType.UT_Length;
         m_MeasureCache["IfcPlaneAngleMeasure"] = UnitType.UT_Angle;
         m_MeasureCache["IfcPositivePlaneAngleMeasure"] = UnitType.UT_Angle;
         m_MeasureCache["IfcPowerMeasure"] = UnitType.UT_HVAC_Power;
         m_MeasureCache["IfcPressureMeasure"] = UnitType.UT_HVAC_Pressure;
         m_MeasureCache["IfcRatioMeasure"] = UnitType.UT_Number;
         m_MeasureCache["IfcReal"] = UnitType.UT_Number;
         m_MeasureCache["IfcTimeMeasure"] = UnitType.UT_Number;  // No unit type for time in Revit.
         m_MeasureCache["IfcTimeStamp"] = UnitType.UT_Number;  // No unit type for time in Revit.
         m_MeasureCache["IfcThermalTransmittanceMeasure"] = UnitType.UT_HVAC_CoefficientOfHeatTransfer;
         m_MeasureCache["IfcThermodynamicTemperatureMeasure"] = UnitType.UT_HVAC_Temperature;
         m_MeasureCache["IfcVolumeMeasure"] = UnitType.UT_Volume;
         m_MeasureCache["IfcVolumetricFlowRateMeasure"] = UnitType.UT_HVAC_Airflow;
      }

      static public IDictionary<string, UnitType> MeasureCache
      {
         get
         {
            if (m_MeasureCache == null)
               InitializeMeasureCache();
            return m_MeasureCache;
         }
      }

      /// <summary>
      /// Gets the unit type from an IFC data.
      /// </summary>
      /// <param name="data">The IFC data.</param>
      /// <param name="defaultType">The default value, if no type is found.</param>
      /// <param name="propertyType">The string value of the simple type, returned for logging purposes.</param>
      /// <returns>The unit type.</returns>
      public static UnitType GetUnitTypeFromData(IFCData data, UnitType defaultType, out string propertyType)
      {
         UnitType unitType = UnitType.UT_Undefined;

         if (data.HasSimpleType())
         {
            propertyType = data.GetSimpleType();
            if (!MeasureCache.TryGetValue(propertyType, out unitType))
               unitType = defaultType;
         }
         else
         {
            propertyType = "";
            unitType = defaultType;
         }

         return unitType;
      }

      /// <summary>
      /// Gets the unit type from an IFC data.
      /// </summary>
      /// <param name="data">The IFC data.</param>
      /// <param name="defaultType">The default value, if no type is found.</param>
      /// <returns>The unit type.</returns>
      public static UnitType GetUnitTypeFromData(IFCData data, UnitType defaultType)
      {
         string propertyType;
         return GetUnitTypeFromData(data, defaultType, out propertyType);
      }
   }
}