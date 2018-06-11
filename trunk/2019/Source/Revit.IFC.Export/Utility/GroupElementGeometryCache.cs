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

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of the geometry of elements in groups.
   /// This may be used by BodyExporter to create a set of mapped items to an already existing geometry.
   /// </summary>
   public class GroupElementGeometryCache : Dictionary<BodyGroupKey, BodyGroupData>
   {
      /// <summary>
      /// Adds a new geometry to the cache.
      /// </summary>
      /// <param name="groupKey">
      ///  The group key.
      /// </param>
      /// <param name="groupData">
      /// The group data.
      /// </param>
      public void Register(BodyGroupKey groupKey, BodyGroupData groupData)
      {
         this[groupKey] = groupData;
      }

      /// <summary>
      /// Retrieves the group data from the dictionary.
      /// </summary>
      /// <param name="groupKey">
      /// The group key.
      /// </param>
      /// <returns>
      /// The group data.
      /// </returns>
      public BodyGroupData Find(BodyGroupKey groupKey)
      {
         BodyGroupData groupData = null;

         if (TryGetValue(groupKey, out groupData))
            return groupData;

         return null;
      }
   }
}