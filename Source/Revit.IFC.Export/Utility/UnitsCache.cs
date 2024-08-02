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
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// The class contains information about created and mapped unit.
   /// </summary>
   public class UnitInfo
   {
      public UnitInfo(IFCAnyHandle handle, double scaleFactor, double offset)
      {
         Handle = handle;
         ScaleFactor = scaleFactor;
         Offset = offset;
      }

      public IFCAnyHandle Handle { get; private set; } = null;
      public double ScaleFactor { get; private set; } = 1.0;
      public double Offset { get; private set; } = 0.0;
   }


   /// <summary>
   /// Used to keep a cache of the created IfcUnits.
   /// </summary>
   public class UnitsCache : Dictionary<string, IFCAnyHandle>
   {
      /// <summary>
      /// The dictionary mapping from Revit data type (SpecTypeId)
      /// to created ifc unit handle with convesion values (scale and offset). 
      /// </summary>
      Dictionary<ForgeTypeId, UnitInfo> m_unitInfoTable =
          new Dictionary<ForgeTypeId, UnitInfo>();

      /// <summary>
      /// The dictionary mapping from Revit unit (UnitTypeId) to created ifc handle. 
      /// These are the auxiliary unit handles that don't go to IfcUnitAssignment
      /// </summary>
      Dictionary<ForgeTypeId, IFCAnyHandle> m_auxiliaryUnitCache = 
         new Dictionary<ForgeTypeId, IFCAnyHandle>();

      /// <summary>
      /// The dictionary mapping from a unit handle with exponent to IfcDerivedUnitElement handle. 
      /// </summary>
      Dictionary<Tuple<IFCAnyHandle, int>, IFCAnyHandle> m_derivedUnitElementCache = 
         new Dictionary<Tuple<IFCAnyHandle, int>, IFCAnyHandle>();

      /// <summary>
      /// Finds UnitInfo in dictionary
      /// </summary>
      public bool FindUnitInfo(ForgeTypeId specTypeId, out UnitInfo unitInfo)
      {
         return m_unitInfoTable.TryGetValue(specTypeId, out unitInfo);
      }

      /// <summary>
      /// Adds UnitInfo to dictionary
      /// </summary>
      public void RegisterUnitInfo(ForgeTypeId specTypeId, UnitInfo unitInfo)
      {
         m_unitInfoTable[specTypeId] = unitInfo;
      }

      /// <summary>
      /// Extracts the unit handles to assign to a project 
      /// </summary>
      /// <returns>Unit handles set</returns>
      public HashSet<IFCAnyHandle> GetUnitsToAssign()
      {
         HashSet<IFCAnyHandle> unitSet = new HashSet<IFCAnyHandle>();
         foreach (var unitInfo in m_unitInfoTable)
         {
            // Special case: SpecTypeId.ColorTemperature is mapped to SI IFCUnit.ThermoDynamicTemperatureUnit (Kelvin)
            // and mustn't be assigned to project to avoid conflict with ThermoDynamicTemperatureUnit of SpecTypeId.HvacTemperature
            if (unitInfo.Key.Equals(SpecTypeId.ColorTemperature))
               continue;

            IFCAnyHandle unitHnd = unitInfo.Value?.Handle;
            if (unitHnd != null)
               unitSet.Add(unitHnd);
         }
         return unitSet;
      }


      /// <summary>
      /// Finds auxiliary unit in dictionary
      /// </summary>
      public bool FindAuxiliaryUnit(ForgeTypeId unitTypeId, out IFCAnyHandle auxiliaryUnit)
      {
         return m_auxiliaryUnitCache.TryGetValue(unitTypeId, out auxiliaryUnit);
      }

      /// <summary>
      /// Adds auxiliary unit to dictionary
      /// </summary>
      public void RegisterAuxiliaryUnit(ForgeTypeId unitTypeId, IFCAnyHandle auxiliaryUnit)
      {
         m_auxiliaryUnitCache[unitTypeId] = auxiliaryUnit;
      }

      /// <summary>
      /// Finds derived unit element in dictionary
      /// </summary>
      public bool FindDerivedUnitElement(Tuple<IFCAnyHandle, int> unitWithExponent, out IFCAnyHandle derivedUnit)
      {
         return m_derivedUnitElementCache.TryGetValue(unitWithExponent, out derivedUnit);
      }

      /// <summary>
      /// Adds derived unit element to dictionary
      /// </summary>
      public void RegisterDerivedUnit(Tuple<IFCAnyHandle, int> unitWithExponent, IFCAnyHandle derivedUnit)
      {
         m_derivedUnitElementCache[unitWithExponent] = derivedUnit;
      }

      /// <summary>
      /// Finds user defined unit in dictionary
      /// </summary>
      public IFCAnyHandle FindUserDefinedUnit(string unitName)
      {
         return this.ContainsKey(unitName) ? this[unitName] : null;
      }

      /// <summary>
      /// Adds user defined unit to dictionary
      /// </summary>
      public void RegisterUserDefinedUnit(string unitName, IFCAnyHandle unitHnd)
      {
         this[unitName] = unitHnd;
      }

      #region Scale/unscale methods
      /// <summary>
      /// Convert from Revit internal units to Revit display units.
      /// </summary>
      /// <param name="specTypeId">Revit data type</param>
      /// <param name="unscaledValue">The value in Revit internal units.</param>
      /// <returns>The value in Revit display units.</returns>
      public double Scale(ForgeTypeId specTypeId, double unscaledValue)
      {
         UnitInfo unitInfo = UnitMappingUtil.GetOrCreateUnitInfo(specTypeId);
         if (unitInfo != null)
            return unscaledValue * unitInfo.ScaleFactor + unitInfo.Offset;
         return unscaledValue;
      }

      /// <summary>
      /// Convert from Revit display units to Revit internal units.
      /// </summary>
      /// <param name="specTypeId">Revit data type</param>
      /// <param name="scaledValue">The value in Revit display units.</param>
      /// <returns>The value in Revit internal units.</returns>
      /// <remarks>Ignores the offset component.</remarks>
      public XYZ Unscale(ForgeTypeId specTypeId, XYZ scaledValue)
      {
         UnitInfo unitInfo = UnitMappingUtil.GetOrCreateUnitInfo(specTypeId);
         if (unitInfo != null)
            return scaledValue / unitInfo.ScaleFactor;
         return scaledValue;
      }

      /// <summary>
      /// Convert from Revit display units to Revit internal units.
      /// </summary>
      /// <param name="specTypeId">Revit data type</param>
      /// <param name="scaledValue">The value in Revit display units.</param>
      /// <returns>The value in Revit internal units.</returns>
      public double Unscale(ForgeTypeId specTypeId, double scaledValue)
      {
         UnitInfo unitInfo = UnitMappingUtil.GetOrCreateUnitInfo(specTypeId);
         if (unitInfo != null)
            return (scaledValue - unitInfo.Offset) / unitInfo.ScaleFactor;
         return scaledValue;
      }

      /// <summary>
      /// Convert from Revit internal units to Revit display units.
      /// </summary>
      /// <param name="specTypeId">Revit data type</param>
      /// <param name="unscaledValue">The value in Revit internal units.</param>
      /// <returns>The value in Revit display units.</returns>
      /// <remarks>Ignores the offset component.</remarks>
      public UV Scale(ForgeTypeId specTypeId, UV unscaledValue)
      {
         UnitInfo unitInfo = UnitMappingUtil.GetOrCreateUnitInfo(specTypeId);
         if (unitInfo != null)
            return unscaledValue * unitInfo.ScaleFactor;
         return unscaledValue;
      }

      /// <summary>
      /// Convert from Revit internal units to Revit display units.
      /// </summary>
      /// <param name="specTypeId">Revit data type</param>
      /// <param name="unscaledValue">The value in Revit internal units.</param>
      /// <returns>The value in Revit display units.</returns>
      /// <remarks>Ignores the offset component.</remarks>
      public XYZ Scale(ForgeTypeId specTypeId, XYZ unscaledValue)
      {
         UnitInfo unitInfo = UnitMappingUtil.GetOrCreateUnitInfo(specTypeId);
         if (unitInfo != null)
            return unscaledValue * unitInfo.ScaleFactor;
         return unscaledValue;
      }
      #endregion
   }

}