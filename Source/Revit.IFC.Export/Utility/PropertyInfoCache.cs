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

using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Manages caches necessary for caching properties for IFC export.
   /// </summary>
   public class PropertyInfoCache
   {
      /// <summary>
      /// The DoublePropertyInfoCache object.
      /// </summary>
      IDictionary<PropertyType, DoublePropertyInfoCache> m_DoublePropertyInfoCacheMap = null;

      /// <summary>
      /// The StringPropertyInfoCache object.
      /// </summary>
      IDictionary<PropertyType, StringPropertyInfoCache> m_StringPropertyInfoCacheMap = null;

      /// <summary>
      /// The BooleanPropertyInfoCache object.
      /// </summary>
      BooleanPropertyInfoCache m_BooleanPropertyInfoCache = null;

      /// <summary>
      /// The LogicalPropertyInfoCache object.
      /// </summary>
      LogicalPropertyInfoCache m_LogicalPropertyInfoCache = null;

      /// <summary>
      /// The IntegerPropertyInfoCache object.
      /// </summary>
      IntegerPropertyInfoCache m_IntegerPropertyInfoCache = null;

      private IDictionary<PropertyType, DoublePropertyInfoCache> DoubleCacheMap
      {
         get
         {
            if (m_DoublePropertyInfoCacheMap == null)
               m_DoublePropertyInfoCacheMap = new Dictionary<PropertyType, DoublePropertyInfoCache>();
            return m_DoublePropertyInfoCacheMap;
         }
      }

      /// <summary>
      /// The StringPropertyInfoCache object.
      /// </summary>
      private IDictionary<PropertyType, StringPropertyInfoCache> StringCacheMap
      {
         get
         {
            if (m_StringPropertyInfoCacheMap == null)
               m_StringPropertyInfoCacheMap = new Dictionary<PropertyType, StringPropertyInfoCache>();
            return m_StringPropertyInfoCacheMap;
         }
      }

      /// <summary>
      /// The BooleanPropertyInfoCache object.
      /// </summary>
      public BooleanPropertyInfoCache BooleanCache
      {
         get
         {
            if (m_BooleanPropertyInfoCache == null)
               m_BooleanPropertyInfoCache = new BooleanPropertyInfoCache();
            return m_BooleanPropertyInfoCache;
         }
      }

      /// <summary>
      /// The LogicalPropertyInfoCache object.
      /// </summary>
      public LogicalPropertyInfoCache LogicalCache
      {
         get
         {
            if (m_LogicalPropertyInfoCache == null)
               m_LogicalPropertyInfoCache = new LogicalPropertyInfoCache();
            return m_LogicalPropertyInfoCache;
         }
      }

      /// <summary>
      /// The IntegerPropertyInfoCache object.
      /// </summary>
      public IntegerPropertyInfoCache IntegerCache
      {
         get
         {
            if (m_IntegerPropertyInfoCache == null)
               m_IntegerPropertyInfoCache = new IntegerPropertyInfoCache();
            return m_IntegerPropertyInfoCache;
         }
      }

      /// <summary>
      /// The StringPropertyInfoCache object for Text property type.
      /// </summary>
      public StringPropertyInfoCache TextCache
      {
         get
         {
            StringPropertyInfoCache textPropertyInfoCache;
            if (!StringCacheMap.TryGetValue(PropertyType.Text, out textPropertyInfoCache))
            {
               textPropertyInfoCache = new StringPropertyInfoCache();
               StringCacheMap[PropertyType.Text] = textPropertyInfoCache;
            }
            return textPropertyInfoCache;
         }
      }

      /// <summary>
      /// The StringPropertyInfoCache object for Label property type.
      /// </summary>
      public StringPropertyInfoCache LabelCache
      {
         get
         {
            StringPropertyInfoCache labelPropertyInfoCache;
            if (!StringCacheMap.TryGetValue(PropertyType.Label, out labelPropertyInfoCache))
            {
               labelPropertyInfoCache = new StringPropertyInfoCache();
               StringCacheMap[PropertyType.Label] = labelPropertyInfoCache;
            }
            return labelPropertyInfoCache;
         }
      }

      /// <summary>
      /// The StringPropertyInfoCache object for Label property type.
      /// </summary>
      public StringPropertyInfoCache IdentifierCache
      {
         get
         {
            StringPropertyInfoCache identifierCache;
            if (!StringCacheMap.TryGetValue(PropertyType.Identifier, out identifierCache))
            {
               identifierCache = new StringPropertyInfoCache();
               StringCacheMap[PropertyType.Identifier] = identifierCache;
            }
            return identifierCache;
         }
      }

      /// <summary>
      /// The DoublePropertyInfoCache object for Real parameter types.
      /// </summary>
      public DoublePropertyInfoCache RealCache
      {
         get
         {
            DoublePropertyInfoCache realPropertyInfoCache;
            if (!DoubleCacheMap.TryGetValue(PropertyType.Real, out realPropertyInfoCache))
            {
               realPropertyInfoCache = new DoublePropertyInfoCache();
               DoubleCacheMap[PropertyType.Real] = realPropertyInfoCache;
            }
            return realPropertyInfoCache;
         }
      }

      /// <summary>
      /// The DoublePropertyInfoCache object for Numeric parameter types.
      /// </summary>
      public DoublePropertyInfoCache NumericCache
      {
         get
         {
            DoublePropertyInfoCache numericPropertyInfoCache;
            if (!DoubleCacheMap.TryGetValue(PropertyType.Numeric, out numericPropertyInfoCache))
            {
               numericPropertyInfoCache = new DoublePropertyInfoCache();
               DoubleCacheMap[PropertyType.Numeric] = numericPropertyInfoCache;
            }
            return numericPropertyInfoCache;
         }
      }

      /// <summary>
      /// The DoublePropertyInfoCache object for LengthMeasure parameter types.
      /// </summary>
      public DoublePropertyInfoCache LengthMeasureCache
      {
         get
         {
            DoublePropertyInfoCache lengthMeasurePropertyInfoCache;
            if (!DoubleCacheMap.TryGetValue(PropertyType.Length, out lengthMeasurePropertyInfoCache))
            {
               lengthMeasurePropertyInfoCache = new DoublePropertyInfoCache();
               DoubleCacheMap[PropertyType.Length] = lengthMeasurePropertyInfoCache;
            }
            return lengthMeasurePropertyInfoCache;
         }
      }

      /// <summary>
      /// The DoublePropertyInfoCache object for PlaneAngle parameter types.
      /// </summary>
      public DoublePropertyInfoCache PlaneAngleCache
      {
         get
         {
            DoublePropertyInfoCache planeAngleCache;
            if (!DoubleCacheMap.TryGetValue(PropertyType.PlaneAngle, out planeAngleCache))
            {
               planeAngleCache = new DoublePropertyInfoCache();
               DoubleCacheMap[PropertyType.PlaneAngle] = planeAngleCache;
            }
            return planeAngleCache;
         }
      }

      /// <summary>
      /// The DoublePropertyInfoCache object for ElectricalCurrent parameter types.
      /// </summary>
      public DoublePropertyInfoCache ElectricCurrentCache
      {
         get
         {
            DoublePropertyInfoCache electricalCurrentCache;
            if (!DoubleCacheMap.TryGetValue(PropertyType.ElectricCurrent, out electricalCurrentCache))
            {
               electricalCurrentCache = new DoublePropertyInfoCache();
               DoubleCacheMap[PropertyType.ElectricCurrent] = electricalCurrentCache;
            }
            return electricalCurrentCache;
         }
      }

      /// <summary>
      /// The DoublePropertyInfoCache object for ElectricalVoltage parameter types.
      /// </summary>
      public DoublePropertyInfoCache ElectricVoltageCache
      {
         get
         {
            DoublePropertyInfoCache electricalVoltageCache;
            if (!DoubleCacheMap.TryGetValue(PropertyType.ElectricVoltage, out electricalVoltageCache))
            {
               electricalVoltageCache = new DoublePropertyInfoCache();
               DoubleCacheMap[PropertyType.ElectricVoltage] = electricalVoltageCache;
            }
            return electricalVoltageCache;
         }
      }

      /// <summary>
      /// The DoublePropertyInfoCache object for PositivePlaneAngle parameter types.
      /// </summary>
      public DoublePropertyInfoCache PositivePlaneAngleCache
      {
         get
         {
            DoublePropertyInfoCache planeAngleCache;
            if (!DoubleCacheMap.TryGetValue(PropertyType.PlaneAngle, out planeAngleCache))
            {
               planeAngleCache = new DoublePropertyInfoCache();
               DoubleCacheMap[PropertyType.PositivePlaneAngle] = planeAngleCache;
            }
            return planeAngleCache;
         }
      }

      /// <summary>
      /// The DoublePropertyInfoCache object for ThermodynamicTemperature parameter types.
      /// </summary>
      public DoublePropertyInfoCache ThermodynamicTemperatureCache
      {
         get
         {
            DoublePropertyInfoCache thermodynamicTemperaturePropertyInfoCache;
            if (!DoubleCacheMap.TryGetValue(PropertyType.ThermodynamicTemperature, out thermodynamicTemperaturePropertyInfoCache))
            {
               thermodynamicTemperaturePropertyInfoCache = new DoublePropertyInfoCache();
               DoubleCacheMap[PropertyType.ThermodynamicTemperature] = thermodynamicTemperaturePropertyInfoCache;
            }
            return thermodynamicTemperaturePropertyInfoCache;
         }
      }

      /// <summary>
      /// The DoublePropertyInfoCache object for ThermalTransmittance parameter types.
      /// </summary>
      public DoublePropertyInfoCache ThermalTransmittanceCache
      {
         get
         {
            DoublePropertyInfoCache thermalTransmittancePropertyInfoCache;
            if (!DoubleCacheMap.TryGetValue(PropertyType.ThermalTransmittance, out thermalTransmittancePropertyInfoCache))
            {
               thermalTransmittancePropertyInfoCache = new DoublePropertyInfoCache();
               DoubleCacheMap[PropertyType.ThermalTransmittance] = thermalTransmittancePropertyInfoCache;
            }
            return thermalTransmittancePropertyInfoCache;
         }
      }

      /// <summary>
      /// The DoublePropertyInfoCache object for Power parameter types.
      /// </summary>
      public DoublePropertyInfoCache PowerCache
      {
         get
         {
            DoublePropertyInfoCache powerPropertyInfoCache;
            if (!DoubleCacheMap.TryGetValue(PropertyType.Power, out powerPropertyInfoCache))
            {
               powerPropertyInfoCache = new DoublePropertyInfoCache();
               DoubleCacheMap[PropertyType.Power] = powerPropertyInfoCache;
            }
            return powerPropertyInfoCache;
         }
      }
   }
}
