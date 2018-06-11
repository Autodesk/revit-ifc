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

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of IFC string properties.
   /// </summary>
   public class StringPropertyInfoCache
   {
      private IDictionary<KeyValuePair<ElementId, string>, IFCAnyHandle> m_PropertiesByIdCache = new Dictionary<KeyValuePair<ElementId, string>, IFCAnyHandle>();

      private IDictionary<KeyValuePair<string, string>, IFCAnyHandle> m_NamedPropertiesCache = new Dictionary<KeyValuePair<string, string>, IFCAnyHandle>();

      /// <summary>
      /// Finds if it contains the property with the specified string value.
      /// </summary>
      /// <param name="parameterId">The parameter id.  Can be null or InvalidElementId if propertyName != null.</param>
      /// <param name="propertyName">The property name.  Can be null if elementId != InvalidElementId.</param>
      /// <param name="value">The value.</param>
      /// <returns>True if it has, false otherwise.</returns>
      public IFCAnyHandle Find(ElementId parameterId, string propertyName, string value)
      {
         IFCAnyHandle propertyHandle = null;
         if ((parameterId != null) && (parameterId != ElementId.InvalidElementId))
         {
            ElementId parameterIdToUse = ParameterUtil.MapParameterId(parameterId);
            if (m_PropertiesByIdCache.TryGetValue(new KeyValuePair<ElementId, string>(parameterIdToUse, value), out propertyHandle))
               return propertyHandle;
         }
         else
         {
            if (m_NamedPropertiesCache.TryGetValue(new KeyValuePair<string, string>(propertyName, value), out propertyHandle))
               return propertyHandle;
         }

         return null;
      }

      /// <summary>
      /// Adds a new property of a string value to the cache.
      /// </summary>
      /// <param name="parameterId">The parameter id.  Can be null or InvalidElementId if propertyName != null.</param>
      /// <param name="propertyName">The property name.  Can be null if elementId != InvalidElementId.</param>
      /// <param name="value">The value.</param>
      /// <param name="propertyHandle">The property handle.</param>
      public void Add(ElementId parameterId, string propertyName, string value, IFCAnyHandle propertyHandle)
      {
         if ((parameterId != null) && (parameterId != ElementId.InvalidElementId))
         {
            ElementId parameterIdToUse = ParameterUtil.MapParameterId(parameterId);
            m_PropertiesByIdCache[new KeyValuePair<ElementId, string>(parameterIdToUse, value)] = propertyHandle;
         }
         else
            m_NamedPropertiesCache[new KeyValuePair<string, string>(propertyName, value)] = propertyHandle;
      }
   }
}