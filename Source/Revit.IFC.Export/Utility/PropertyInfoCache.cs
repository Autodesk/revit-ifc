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
      private Dictionary<PropertyType, DoublePropertyInfoCache> DoubleCacheMap { get; set; } = new();

      /// <summary>
      /// The StringPropertyInfoCache object.
      /// </summary>
      private Dictionary<PropertyType, StringPropertyInfoCache> StringCacheMap { get; set; } = new();

      /// <summary>
      /// The BooleanPropertyInfoCache object.
      /// </summary>
      public BooleanPropertyInfoCache BooleanCache { get; private set; } = new();

      /// <summary>
      /// The LogicalPropertyInfoCache object.
      /// </summary>
      public LogicalPropertyInfoCache LogicalCache { get; private set; } = new();
      /// <summary>
      /// The IntegerPropertyInfoCache object.
      /// </summary>
      public IntegerPropertyInfoCache IntegerCache { get; private set; } = new();

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
      /// Get DoublePropertyInfoCache object for the particular type
      /// </summary>
      public DoublePropertyInfoCache GetDoubleChache(PropertyType propertyType)
      {
         DoublePropertyInfoCache doublePropertyInfoCache;
         if (!DoubleCacheMap.TryGetValue(propertyType, out doublePropertyInfoCache))
         {
            doublePropertyInfoCache = new DoublePropertyInfoCache();
            DoubleCacheMap[propertyType] = doublePropertyInfoCache;
         }
         return doublePropertyInfoCache;
      }

      public void Clear()
      {
         DoubleCacheMap.Clear();
         StringCacheMap.Clear();
         BooleanCache.Clear();
         LogicalCache.Clear();
         IntegerCache.Clear();
      }
   }
}
