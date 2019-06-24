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
   /// This maps an IFC handle to the Element that created it.
   /// </summary>
   /// <remarks>
   /// This is used to identify which element should be used for properties, for elements 
   /// (e.g. Stairs) that contain other elements.
   /// </remarks>
   public class HandleToElementCache
   {
      /// <summary>
      /// The dictionary mapping from an IFC handle to ElementId. 
      /// </summary>
      private Dictionary<IFCAnyHandle, ElementId> m_HandleToElementCache = new Dictionary<IFCAnyHandle, ElementId>();

      /// <summary>
      /// Finds the ElementId from the dictionary.
      /// </summary>
      /// <param name="hnd">
      /// The handle.
      /// </param>
      /// <returns>
      /// The ElementId.
      /// </returns>
      public ElementId Find(IFCAnyHandle hnd)
      {
         ElementId id;
         if (m_HandleToElementCache.TryGetValue(hnd, out id))
         {
            return id;
         }
         return ElementId.InvalidElementId;
      }

      /// <summary>
      /// Adds the handle to the dictionary.
      /// </summary>
      /// <param name="handle">
      /// The handle.
      /// </param>
      /// <param name="elementId">
      /// The material element elementId.
      /// </param>
      public void Register(IFCAnyHandle handle, ElementId elementId)
      {
         if (m_HandleToElementCache.ContainsKey(handle))
            return;

         m_HandleToElementCache[handle] = elementId;
      }

      /// <summary>
      /// Delete a handle from the cache
      /// </summary>
      /// <param name="handle">the handle</param>
      public void Delete(IFCAnyHandle handle)
      {
         if (m_HandleToElementCache.ContainsKey(handle))
         {
            ElementId elem = m_HandleToElementCache[handle];
            m_HandleToElementCache.Remove(handle);
            ExporterCacheManager.ElementToHandleCache.Delete(elem);
         }
      }
   }
}