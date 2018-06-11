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

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Maps IFC property to Revit built in parameter.
   /// </summary>
   class IFCPropertyMapping
   {
      static Dictionary<Tuple<string, string>, BuiltInParameter> m_Parameters = new Dictionary<Tuple<string, string>, BuiltInParameter>();

      static string m_SpacePropertySet = "Pset_SpaceCommon";
      static string m_WallPropertySet = "Pset_WallCommon";
      static string m_BeamPropertySet = "Pset_BeamCommon";
      static string m_ColumnPropertySet = "Pset_ColumnCommon";
      static string m_MemberPropertySet = "Pset_MemberCommon";
      static string m_RoofPropertySet = "Pset_RoofCommon";
      static string m_SlabPropertySet = "Pset_SlabCommon";
      static string m_RampPropertySet = "Pset_RampCommon";
      static string m_StairPropertySet = "Pset_StairCommon";

      static IFCPropertyMapping()
      {
         m_Parameters.Add(new Tuple<string, string>(m_SpacePropertySet, "CeilingCovering"), BuiltInParameter.ROOM_FINISH_CEILING);
         m_Parameters.Add(new Tuple<string, string>(m_SpacePropertySet, "FloorCovering"), BuiltInParameter.ROOM_FINISH_FLOOR);
         m_Parameters.Add(new Tuple<string, string>(m_SpacePropertySet, "WallCovering"), BuiltInParameter.ROOM_FINISH_WALL);

         m_Parameters.Add(new Tuple<string, string>(m_WallPropertySet, "FireRating"), BuiltInParameter.FIRE_RATING);

         m_Parameters.Add(new Tuple<string, string>(m_BeamPropertySet, "FireRating"), BuiltInParameter.FIRE_RATING);
         m_Parameters.Add(new Tuple<string, string>(m_BeamPropertySet, "Roll"), BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE);

         m_Parameters.Add(new Tuple<string, string>(m_ColumnPropertySet, "Roll"), BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE);

         m_Parameters.Add(new Tuple<string, string>(m_MemberPropertySet, "Roll"), BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE);

         m_Parameters.Add(new Tuple<string, string>(m_RoofPropertySet, "FireRating"), BuiltInParameter.FIRE_RATING);

         m_Parameters.Add(new Tuple<string, string>(m_SlabPropertySet, "FireRating"), BuiltInParameter.FIRE_RATING);

         m_Parameters.Add(new Tuple<string, string>(m_RampPropertySet, "FireRating"), BuiltInParameter.FIRE_RATING);

         m_Parameters.Add(new Tuple<string, string>(m_StairPropertySet, "FireRating"), BuiltInParameter.FIRE_RATING);
      }

      /// <summary>
      /// Gets the built in parameter from property group and name.
      /// </summary>
      /// <param name="group">The group.</param>
      /// <param name="name">The name.</param>
      /// <returns>The built in parameter.</returns>
      public static BuiltInParameter GetBuiltInParameter(string group, string name)
      {
         BuiltInParameter builtInParameter = BuiltInParameter.INVALID;

         m_Parameters.TryGetValue(new Tuple<string, string>(group, name), out builtInParameter);

         return builtInParameter;
      }
   }
}