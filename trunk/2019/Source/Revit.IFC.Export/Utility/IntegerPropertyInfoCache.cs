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
using Autodesk.Revit.DB.IFC;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of IFC integer properties.
   /// </summary>
   public class IntegerPropertyInfoCache : Dictionary<KeyValuePair<string, int>, IFCAnyHandle>
   {
      /// <summary>
      /// Finds if it contains the property with the specified int value.
      /// </summary>
      /// <param name="propertyName">The property name.</param>
      /// <param name="value">The value.</param>
      /// <returns>True if it has, false otherwise.</returns>
      public IFCAnyHandle Find(string propertyName, int value)
      {
         KeyValuePair<string, int> key = new KeyValuePair<string, int>(propertyName, value);

         IFCAnyHandle propertyHandle;
         if (TryGetValue(key, out propertyHandle))
            return propertyHandle;

         return null;
      }

      /// <summary>
      /// Adds a new property of a int value to the cache.
      /// </summary>
      /// <param name="propertyName">The property name.</param>
      /// <param name="value">The value.</param>
      /// <param name="propertyHandle">The property handle.</param>
      public void Add(string propertyName, int value, IFCAnyHandle propertyHandle)
      {
         KeyValuePair<string, int> key = new KeyValuePair<string, int>(propertyName, value);
         this[key] = propertyHandle;
      }
   }
}