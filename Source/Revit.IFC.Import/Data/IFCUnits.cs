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
using UnitName = Autodesk.Revit.DB.DisplayUnitType;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Provides methods to process IFC units.
   /// </summary>
   public class IFCUnits
   {
      /// <summary>
      /// The IFC project units.
      /// </summary>
      Dictionary<UnitType, IFCUnit> m_ProjectUnitsDictionary = new Dictionary<UnitType, IFCUnit>();

      /// <summary>
      /// Gets the unit of a type.
      /// </summary>
      /// <param name="unitType">The unit type.</param>
      /// <returns>The Unit object.</returns>
      public IFCUnit GetIFCProjectUnit(UnitType unitType)
      {
         IFCUnit projectUnit = null;
         if (m_ProjectUnitsDictionary.TryGetValue(unitType, out projectUnit))
         {
            return projectUnit;
         }
         else
         {
            //default units
            if (unitType == UnitType.UT_Length)
            {
               IFCUnit unit = IFCUnit.ProcessIFCDefaultUnit(unitType, UnitSystem.Metric, UnitName.DUT_METERS, 1.0 / 0.3048);
               m_ProjectUnitsDictionary[unitType] = unit;
               return unit;
            }
            else if (unitType == UnitType.UT_Area)
            {
               IFCUnit projectLengthUnit = GetIFCProjectUnit(UnitType.UT_Length);

               UnitSystem unitSystem = projectLengthUnit.UnitSystem;
               UnitName unitName = unitSystem == UnitSystem.Metric ?
                   UnitName.DUT_SQUARE_METERS : UnitName.DUT_SQUARE_FEET;
               double scaleFactor = unitSystem == UnitSystem.Metric ?
                   (1.0 / 0.3048) * (1.0 / 0.3048) : 1.0;

               IFCUnit unit = IFCUnit.ProcessIFCDefaultUnit(unitType, unitSystem, unitName, scaleFactor);
               m_ProjectUnitsDictionary[unitType] = unit;
               return unit;
            }
            else if (unitType == UnitType.UT_Volume)
            {
               IFCUnit projectLengthUnit = GetIFCProjectUnit(UnitType.UT_Length);

               UnitSystem unitSystem = projectLengthUnit.UnitSystem;
               UnitName unitName = unitSystem == UnitSystem.Metric ?
                   UnitName.DUT_CUBIC_METERS : UnitName.DUT_CUBIC_FEET;
               double scaleFactor = unitSystem == UnitSystem.Metric ?
                   (1.0 / 0.3048) * (1.0 / 0.3048) * (1.0 / 0.3048) : 1.0;

               IFCUnit unit = IFCUnit.ProcessIFCDefaultUnit(unitType, unitSystem, unitName, scaleFactor);
               m_ProjectUnitsDictionary[unitType] = unit;
               return unit;
            }
            else if (unitType == UnitType.UT_Angle)
            {
               IFCUnit unit = IFCUnit.ProcessIFCDefaultUnit(unitType, UnitSystem.Metric, UnitName.DUT_DECIMAL_DEGREES, Math.PI / 180);
               m_ProjectUnitsDictionary[unitType] = unit;
               return unit;
            }
            else if (unitType == UnitType.UT_HVAC_Temperature)
            {
               IFCUnit unit = IFCUnit.ProcessIFCDefaultUnit(unitType, UnitSystem.Metric, UnitName.DUT_KELVIN, 1.0);
               m_ProjectUnitsDictionary[unitType] = unit;
               return unit;
            }
         }
         return null;
      }

      /// <summary>
      /// Processes a project unit.
      /// </summary>
      /// <param name="unitHnd">The unit handle.</param>
      /// <returns>The Unit object.</returns>
      public IFCUnit ProcessIFCProjectUnit(IFCAnyHandle unitHnd)
      {
         IFCUnit unit = IFCUnit.ProcessIFCUnit(unitHnd);
         if (unit != null)
            m_ProjectUnitsDictionary[unit.UnitType] = unit;

         return unit;
      }
   }
}