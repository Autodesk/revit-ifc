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
      /// The dictionary mapping from an ElementId to an  handle. 
      /// </summary>
      private Dictionary<ElementId, IFCAnyHandle> m_ElementIdToHandleDictionary = new Dictionary<ElementId, IFCAnyHandle>();
      private Dictionary<ElementId, IFCExportInfoPair> m_ELementIdAndExportType = new Dictionary<ElementId, IFCExportInfoPair>();

      /// <summary>
      /// Finds the handle from the dictionary.
      /// </summary>
      /// <param name="elementId">
      /// The element elementId.
      /// </param>
      /// <returns>
      /// The handle.
      /// </returns>
      public IFCAnyHandle Find(ElementId elementId)
      {
         IFCAnyHandle handle;
         if (m_ElementIdToHandleDictionary.TryGetValue(elementId, out handle))
         {
            return handle;
         }
         return null;
      }

      /// <summary>
      /// Find IFCExportInforPair of the Element with the ElementId. Used for applicable Pset
      /// </summary>
      /// <param name="elementId">The ElementId</param>
      /// <returns>return PredefinedType string or null</returns>
      public IFCExportInfoPair FindPredefinedType(ElementId elementId)
      {
         IFCExportInfoPair exportType;
         if (m_ELementIdAndExportType.TryGetValue(elementId, out exportType))
         {
            return exportType;
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
            IFCAnyHandle handle;
            if (m_ElementIdToHandleDictionary.TryGetValue(elementId, out handle))
            {
               try
               {
                  bool isType = IFCAnyHandleUtil.IsSubTypeOf(handle, expectedType);
                  if (!isType)
                  {
                     m_ElementIdToHandleDictionary.Remove(elementId);
                     m_ELementIdAndExportType.Remove(elementId);
                  }
               }
               catch
               {
                  m_ElementIdToHandleDictionary.Remove(elementId);
                  m_ELementIdAndExportType.Remove(elementId);
               }
            }
         }
      }

      /// <summary>
      /// Adds the handle to the dictionary.
      /// </summary>
      /// <param name="elementId">
      /// The element elementId.
      /// </param>
      /// <param name="handle">
      /// The handle.
      /// </param>
      public void Register(ElementId elementId, IFCAnyHandle handle, IFCExportInfoPair exportType = null)
      {
         if (m_ElementIdToHandleDictionary.ContainsKey(elementId))
            return;

         m_ElementIdToHandleDictionary[elementId] = handle;
         // Register also handle to elementid cache at the same time in order to make the two caches consistent
         ExporterCacheManager.HandleToElementCache.Register(handle, elementId);

         if (exportType != null)
            if (!m_ELementIdAndExportType.ContainsKey(elementId))
               m_ELementIdAndExportType.Add(elementId, exportType);
      }

      /// <summary>
      /// Delete an element from the cache
      /// </summary>
      /// <param name="element">the element</param>
      public void Delete(ElementId element)
      {
         if (m_ElementIdToHandleDictionary.ContainsKey(element))
         {
            IFCAnyHandle handle = m_ElementIdToHandleDictionary[element];
            m_ElementIdToHandleDictionary.Remove(element);
            ExporterCacheManager.HandleToElementCache.Delete(handle);
         }
      }
   }
}