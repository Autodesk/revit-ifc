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

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Manages caches necessary for caching already created property sets.
   /// </summary>
   public class PropertySetCache
   {
      private IDictionary<long, Tuple<ISet<IFCAnyHandle>, ISet<IFCAnyHandle>>> Cache
         { get; set; } = new SortedDictionary<long, Tuple<ISet<IFCAnyHandle>, ISet<IFCAnyHandle>>>();

      public PropertySetCache() 
      { 
      }

      /// <summary>
      /// Append new entity handles to an already existing set of properties.
      /// </summary>
      /// <param name="element">The base Revit element.</param>
      /// <param name="elementHandles">The new IFC entities.</param>
      /// <returns>True if the value was appended, false otherwise.</returns>
      public bool TryAppend(ElementId elementId, ISet<IFCAnyHandle> elementHandles)
      {
         if (!Cache.TryGetValue(elementId.IntegerValue, out var existingHandles))
            return false;

         existingHandles.Item2.UnionWith(elementHandles);
         return true;   
      }

      public void Add(ElementId elementId, ISet<IFCAnyHandle> propertySetHandles, 
         ISet<IFCAnyHandle> elementHandles)
      {
         Tuple<ISet<IFCAnyHandle>, ISet<IFCAnyHandle>> newHandles = 
            Tuple.Create(propertySetHandles, elementHandles);

         Cache[elementId.IntegerValue] = newHandles;
      }

      public void CreateRelations(IFCFile file)
      {
         IFCAnyHandle ownerHandle = ExporterCacheManager.OwnerHistoryHandle;
         foreach (var elementPropertySets in Cache.Values)
         {
            foreach (IFCAnyHandle propertySetHandle in elementPropertySets.Item1)
            {
               string psetRelGUID = GUIDUtil.GenerateIFCGuidFrom(
                  GUIDUtil.CreateGUIDString(IFCEntityType.IfcRelDefinesByProperties,
                  ExporterUtil.GetGlobalId(propertySetHandle)));
               ExporterUtil.CreateRelDefinesByProperties(file, psetRelGUID,
                  ownerHandle, null, null, elementPropertySets.Item2, propertySetHandle);
            }
         }
      }
   }
}
