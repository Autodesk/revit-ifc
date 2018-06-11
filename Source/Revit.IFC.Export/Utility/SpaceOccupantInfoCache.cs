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
using Revit.IFC.Export.Exporter;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of information to create zones.
   /// </summary>
   public class SpaceOccupantInfoCache : Dictionary<string, SpaceOccupantInfo>
   {
      /// <summary>
      /// Adds the SpaceOccupantInfo to the dictionary.
      /// </summary>
      /// <param name="name">
      /// The name of the zone.
      /// </param>
      /// <param name="spaceOccupantInfo">
      /// The SpaceOccupantInfo object.
      /// </param>
      public void Register(string name, SpaceOccupantInfo spaceOccupantInfo)
      {
         if (!string.IsNullOrEmpty(name))
            this[name] = spaceOccupantInfo;
      }

      /// <summary>
      /// Finds the spaceOccupantInfo from the dictionary.
      /// </summary>
      /// <param name="name">
      /// The name of the zone.
      /// </param>
      /// <returns>
      /// The spaceOccupantInfo object.
      /// </returns>
      public SpaceOccupantInfo Find(string name)
      {
         if (string.IsNullOrEmpty(name))
            return null;

         SpaceOccupantInfo spaceOccupantInfo;

         if (TryGetValue(name, out spaceOccupantInfo))
            return spaceOccupantInfo;

         return null;
      }
   }
}