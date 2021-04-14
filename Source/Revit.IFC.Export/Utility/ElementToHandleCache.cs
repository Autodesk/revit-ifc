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
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of a mapping of an ElementId to a handle.
   /// </summary>
   public class ElementToHandleCache
   {
      /// <summary>
      /// The dictionary mapping from an ElementId to a handle and its export information. 
      /// </summary>
      private Dictionary<ElementId, Tuple<IFCAnyHandle, IFCExportInfoPair>> ElementIdToHandleAndInfo
      { get; set; } = new Dictionary<ElementId, Tuple<IFCAnyHandle, IFCExportInfoPair>>();

      /// <summary>
      /// Finds the handle from the dictionary.
      /// </summary>
      /// <param name="elementId">The element elementId.</param>
      /// <returns>The handle.</returns>
      public IFCAnyHandle Find(ElementId elementId)
      {
         IFCAnyHandle handle = null;
         Tuple<IFCAnyHandle, IFCExportInfoPair> handleAndInfo = null;
         if (ElementIdToHandleAndInfo.TryGetValue(elementId, out handleAndInfo))
         {
            // We need to make sure the handle isn't stale.  If it is, remove it. 
            try
            {
               handle = handleAndInfo.Item1;
               if (!IFCAnyHandleUtil.IsValidHandle(handle))
               {
                  ElementIdToHandleAndInfo.Remove(elementId);
                  handle = null;
               }
            }
            catch
            {
               ElementIdToHandleAndInfo.Remove(elementId);
               handle = null;
            }
         }
         return handle;
      }

      /// <summary>
      /// Find IFCExportInforPair of the Element with the ElementId. Used for applicable Pset
      /// </summary>
      /// <param name="matchingHandle">The handle associated with the elementId.</param>
      /// <param name="elementId">The ElementId</param>
      /// <returns>PredefinedType string or null if not found, or the handle doesn't match.</returns>
      public IFCExportInfoPair FindPredefinedType(IFCAnyHandle matchingHandle, ElementId elementId)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(matchingHandle))
            return null;

         Tuple<IFCAnyHandle, IFCExportInfoPair> handleAndInfo = null;
         if (ElementIdToHandleAndInfo.TryGetValue(elementId, out handleAndInfo))
         {
            // It is possible that the handle associated to the element id is not the same as
            // the handle for which we are looking for information.  As such, do a match first.
            if (matchingHandle.Id == handleAndInfo.Item1.Id)
               return handleAndInfo.Item2;
         }

         return null;
      }

      /// <summary>
      /// Removes invalid handles from the cache.
      /// </summary>
      /// <param name="elementIds">The element ids.</param>
      /// <param name="expectedType">The expected type of the handles.</param>
      public void RemoveInvalidHandles(ISet<ElementId> elementIds, IFCEntityType expectedType)
      {
         foreach (ElementId elementId in elementIds)
         {
            Tuple<IFCAnyHandle, IFCExportInfoPair> handleAndInfo = null;
            if (ElementIdToHandleAndInfo.TryGetValue(elementId, out handleAndInfo))
            {
               try
               {
                  if (!IFCAnyHandleUtil.IsSubTypeOf(handleAndInfo.Item1, expectedType))
                  {
                     ElementIdToHandleAndInfo.Remove(elementId);
                  }
               }
               catch
               {
                  ElementIdToHandleAndInfo.Remove(elementId);
               }
            }
         }
      }

      /// <summary>
      /// Adds the handle to the dictionary.
      /// </summary>
      /// <param name="elementId">The element elementId.</param>
      /// <param name="handle">The handle.</param>
      public void Register(ElementId elementId, IFCAnyHandle handle, IFCExportInfoPair exportType = null)
      {
         if (ElementIdToHandleAndInfo.ContainsKey(elementId))
            return;

         ElementIdToHandleAndInfo[elementId] = Tuple.Create(handle, exportType);
         // Register also handle to elementid cache at the same time in order to make the two caches consistent
         ExporterCacheManager.HandleToElementCache.Register(handle, elementId);
      }

      /// <summary>
      /// Delete an element from the cache
      /// </summary>
      /// <param name="element">the element</param>
      public void Delete(ElementId elementId)
      {
         if (ElementIdToHandleAndInfo.ContainsKey(elementId))
         {
            IFCAnyHandle handle = ElementIdToHandleAndInfo[elementId].Item1;
            ElementIdToHandleAndInfo.Remove(elementId);
            ExporterCacheManager.HandleToElementCache.Delete(handle);
         }
      }
   }
}
