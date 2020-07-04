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
   /// Used to keep a cache of the created IfcUnits.
   /// </summary>
   public class UnitsCache : Dictionary<string, IFCAnyHandle>
   {
      Dictionary<UnitType, Tuple<IFCAnyHandle, double, double>> m_UnitConversionTable =
          new Dictionary<UnitType, Tuple<IFCAnyHandle, double, double>>();

      /// <summary>
      /// Convert from Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unitType">The unit type.</param>
      /// <param name="unscaledValue">The value in Revit internal units.</param>
      /// <returns>The value in Revit display units.</returns>
      public double Scale(UnitType unitType, double unscaledValue)
      {
         Tuple<IFCAnyHandle, double, double> scale;
         if (m_UnitConversionTable.TryGetValue(unitType, out scale))
            return unscaledValue * scale.Item2 + scale.Item3;
         return unscaledValue;
      }

      /// <summary>
      /// Convert from Revit display units to Revit internal units.
      /// </summary>
      /// <param name="unitType">The unit type.</param>
      /// <param name="unscaledValue">The value in Revit display units.</param>
      /// <returns>The value in Revit internal units.</returns>
      /// <remarks>Ignores the offset component.</remarks>
      public XYZ Unscale(UnitType unitType, XYZ scaledValue)
      {
         Tuple<IFCAnyHandle, double, double> scale;
         if (m_UnitConversionTable.TryGetValue(unitType, out scale))
            return scaledValue / scale.Item2;
         return scaledValue;
      }

      /// <summary>
      /// Convert from Revit display units to Revit internal units.
      /// </summary>
      /// <param name="unitType">The unit type.</param>
      /// <param name="scaledValue">The value in Revit display units.</param>
      /// <returns>The value in Revit internal units.</returns>
      public double Unscale(UnitType unitType, double scaledValue)
      {
         Tuple<IFCAnyHandle, double, double> scale;
         if (m_UnitConversionTable.TryGetValue(unitType, out scale))
            return (scaledValue - scale.Item3) / scale.Item2;
         return scaledValue;
      }

      /// <summary>
      /// Convert from Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unitType">The unit type.</param>
      /// <param name="unscaledValue">The value in Revit internal units.</param>
      /// <returns>The value in Revit display units.</returns>
      /// <remarks>Ignores the offset component.</remarks>
      public UV Scale(UnitType unitType, UV unscaledValue)
      {
         Tuple<IFCAnyHandle, double, double> scale;
         if (m_UnitConversionTable.TryGetValue(unitType, out scale))
            return unscaledValue * scale.Item2;
         return unscaledValue;
      }

      /// <summary>
      /// Convert from Revit internal units to Revit display units.
      /// </summary>
      /// <param name="unitType">The unit type.</param>
      /// <param name="unscaledValue">The value in Revit internal units.</param>
      /// <returns>The value in Revit display units.</returns>
      /// <remarks>Ignores the offset component.</remarks>
      public XYZ Scale(UnitType unitType, XYZ unscaledValue)
      {
         Tuple<IFCAnyHandle, double, double> scale;
         if (m_UnitConversionTable.TryGetValue(unitType, out scale))
            return unscaledValue * scale.Item2;
         return unscaledValue;
      }

      /// <summary>
      /// Sets the conversion factors to convert Revit internal units to Revit display units for the specified unit type, and stores the IFC handle.
      /// </summary>
      /// <param name="unitType">The unit type.</param>
      /// <param name="unitHandle">The IFCUnit handle.</param>
      /// <param name="scale">The scaling factor.</param>
      public void AddUnit(UnitType unitType, IFCAnyHandle unitHandle, double scale, double offset)
      {
         m_UnitConversionTable[unitType] = new Tuple<IFCAnyHandle, double, double>(unitHandle, scale, offset);
      }
   }
}