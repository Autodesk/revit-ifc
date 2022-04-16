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
      Dictionary<ForgeTypeId, IFCUnit> ProjectUnitsDictionary { get; set; }  = new Dictionary<ForgeTypeId, IFCUnit>();

      /// <summary>
      /// Gets the unit of a type.
      /// </summary>
      /// <param name="specTypeId">Identifier of the spec.</param>
      /// <returns>The Unit object.</returns>
      public IFCUnit GetIFCProjectUnit(ForgeTypeId specTypeId)
      {
         IFCUnit projectUnit = null;
         if (ProjectUnitsDictionary.TryGetValue(specTypeId, out projectUnit))
         {
            return projectUnit;
         }
         else
         {
            //default units
            if (specTypeId.Equals(SpecTypeId.Length))
            {
               IFCUnit unit = IFCUnit.ProcessIFCDefaultUnit(specTypeId, UnitSystem.Metric, UnitTypeId.Meters, 1.0 / 0.3048);
               ProjectUnitsDictionary[specTypeId] = unit;
               return unit;
            }
            else if (specTypeId.Equals(SpecTypeId.Area))
            {
               IFCUnit projectLengthUnit = GetIFCProjectUnit(SpecTypeId.Length);

               UnitSystem unitSystem = projectLengthUnit.UnitSystem;
               ForgeTypeId unitName = unitSystem == UnitSystem.Metric ?
                   UnitTypeId.SquareMeters : UnitTypeId.SquareFeet;
               double scaleFactor = unitSystem == UnitSystem.Metric ?
                   (1.0 / 0.3048) * (1.0 / 0.3048) : 1.0;

               IFCUnit unit = IFCUnit.ProcessIFCDefaultUnit(specTypeId, unitSystem, unitName, scaleFactor);
               ProjectUnitsDictionary[specTypeId] = unit;
               return unit;
            }
            else if (specTypeId.Equals(SpecTypeId.Volume))
            {
               IFCUnit projectLengthUnit = GetIFCProjectUnit(SpecTypeId.Length);

               UnitSystem unitSystem = projectLengthUnit.UnitSystem;
               ForgeTypeId unitName = unitSystem == UnitSystem.Metric ?
                   UnitTypeId.CubicMeters : UnitTypeId.CubicFeet;
               double scaleFactor = unitSystem == UnitSystem.Metric ?
                   (1.0 / 0.3048) * (1.0 / 0.3048) * (1.0 / 0.3048) : 1.0;

               IFCUnit unit = IFCUnit.ProcessIFCDefaultUnit(specTypeId, unitSystem, unitName, scaleFactor);
               ProjectUnitsDictionary[specTypeId] = unit;
               return unit;
            }
            else if (specTypeId.Equals(SpecTypeId.Angle))
            {
               IFCUnit unit = IFCUnit.ProcessIFCDefaultUnit(specTypeId, UnitSystem.Metric, UnitTypeId.Degrees, Math.PI / 180);
               ProjectUnitsDictionary[specTypeId] = unit;
               return unit;
            }
            else if (specTypeId.Equals(SpecTypeId.HvacTemperature))
            {
               IFCUnit unit = IFCUnit.ProcessIFCDefaultUnit(specTypeId, UnitSystem.Metric, UnitTypeId.Kelvin, 1.0);
               ProjectUnitsDictionary[specTypeId] = unit;
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
            ProjectUnitsDictionary[unit.Spec] = unit;

         return unit;
      }
   }
}