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
      static private IDictionary<string, ForgeTypeId> m_MeasureCache = null;

      static private void InitializeMeasureCache()
      {
         m_MeasureCache = new SortedDictionary<string, ForgeTypeId>(StringComparer.InvariantCultureIgnoreCase);

         m_MeasureCache["IfcAreaMeasure"] = SpecTypeId.Area;
         m_MeasureCache["IfcCountMeasure"] = SpecTypeId.Number;
         m_MeasureCache["IfcElectricCurrentMeasure"] = SpecTypeId.Current;
         m_MeasureCache["IfcElectricVoltageMeasure"] = SpecTypeId.ElectricalPotential;
         m_MeasureCache["IfcForceMeasure"] = SpecTypeId.Force;
         m_MeasureCache["IfcFrequencyMeasure"] = SpecTypeId.ElectricalFrequency;
         m_MeasureCache["IfcLengthMeasure"] = SpecTypeId.Length;
         m_MeasureCache["IfcIlluminanceMeasure"] = SpecTypeId.Illuminance;
         m_MeasureCache["IfcInteger"] = SpecTypeId.Number;
         m_MeasureCache["IfcLinearVelocityMeasure"] = SpecTypeId.HvacVelocity;
         m_MeasureCache["IfcLuminousFluxMeasure"] = SpecTypeId.LuminousFlux;
         m_MeasureCache["IfcLuminousIntensityMeasure"] = SpecTypeId.LuminousIntensity;
         m_MeasureCache["IfcMassFlowRateMeasure"] = SpecTypeId.PipingMassPerTime;
         m_MeasureCache["IfcMassMeasure"] = SpecTypeId.Mass;
         m_MeasureCache["IfcMassDensityMeasure"] = SpecTypeId.MassDensity;
         m_MeasureCache["IfcMonetaryMeasure"] = SpecTypeId.Currency;
         m_MeasureCache["IfcNormalisedRatioMeasure"] = SpecTypeId.Number;
         m_MeasureCache["IfcNumericMeasure"] = SpecTypeId.Number;
         m_MeasureCache["IfcPositiveRatioMeasure"] = SpecTypeId.Number;
         m_MeasureCache["IfcPositiveLengthMeasure"] = SpecTypeId.Length;
         m_MeasureCache["IfcPlaneAngleMeasure"] = SpecTypeId.Angle;
         m_MeasureCache["IfcPositivePlaneAngleMeasure"] = SpecTypeId.Angle;
         m_MeasureCache["IfcPowerMeasure"] = SpecTypeId.HvacPower;
         m_MeasureCache["IfcPressureMeasure"] = SpecTypeId.HvacPressure;
         m_MeasureCache["IfcRatioMeasure"] = SpecTypeId.Number;
         m_MeasureCache["IfcReal"] = SpecTypeId.Number;
         m_MeasureCache["IfcRotationalFrequencyMeasure"] = SpecTypeId.AngularSpeed;
         m_MeasureCache["IfcSoundPowerMeasure"] = SpecTypeId.Wattage;
         m_MeasureCache["IfcSoundPressureMeasure"] = SpecTypeId.HvacPressure;
         m_MeasureCache["IfcTimeMeasure"] = SpecTypeId.Time;
         m_MeasureCache["IfcTimeStamp"] = SpecTypeId.Number;  // No unit type for time in Revit.
         m_MeasureCache["IfcThermalTransmittanceMeasure"] = SpecTypeId.HeatTransferCoefficient;
         m_MeasureCache["IfcThermodynamicTemperatureMeasure"] = SpecTypeId.HvacTemperature;
         m_MeasureCache["IfcVolumeMeasure"] = SpecTypeId.Volume;
         m_MeasureCache["IfcVolumetricFlowRateMeasure"] = SpecTypeId.AirFlow;
      }

      static public IDictionary<string, ForgeTypeId> MeasureCache
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
      /// <param name="defaultSpec">The default spec, if no spec is found.</param>
      /// <param name="propertyType">The string value of the simple type, returned for logging purposes.</param>
      /// <returns>The unit type.</returns>
      public static ForgeTypeId GetUnitTypeFromData(IFCData data, ForgeTypeId defaultSpec, out string propertyType)
      {
         ForgeTypeId specTypeId = new ForgeTypeId();

         if (data.HasSimpleType())
         {
            propertyType = data.GetSimpleType();
            if (!MeasureCache.TryGetValue(propertyType, out specTypeId))
               specTypeId = defaultSpec;
         }
         else
         {
            propertyType = "";
            specTypeId = defaultSpec;
         }

         return specTypeId;
      }

      /// <summary>
      /// Gets the unit type from an IFC data.
      /// </summary>
      /// <param name="data">The IFC data.</param>
      /// <param name="defaultSpec">The default spec, if no spec is found.</param>
      /// <returns>The unit type.</returns>
      public static ForgeTypeId GetUnitTypeFromData(IFCData data, ForgeTypeId defaultSpec)
      {
         string propertyType;
         return GetUnitTypeFromData(data, defaultSpec, out propertyType);
      }
   }
}
