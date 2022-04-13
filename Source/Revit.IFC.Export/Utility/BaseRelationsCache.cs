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

using System.Collections.Generic;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of the IfcRoot handles mapping to a generic handle.
   /// </summary>
   public class BaseRelationsCache : Dictionary<IFCAnyHandle, ISet<IFCAnyHandle>>
   {
      /// <summary>
      /// Adds the IfcRoot handle to the dictionary.
      /// </summary>
      /// <param name="handle">The handle.</param>
      /// <param name="product">The related product handle.</param>
      public void Add(IFCAnyHandle handle, IFCAnyHandle product)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(handle) || IFCAnyHandleUtil.IsNullOrHasNoValue(product))
            return;

         if (ContainsKey(handle))
         {
            this[handle].Add(product);
         }
         else
         {
            HashSet<IFCAnyHandle> products = new HashSet<IFCAnyHandle>();
            products.Add(product);
            this[handle] = products;
         }
      }

      /// <summary>
      /// To clean the set of reference objects from the cache in case of some 
      /// deleted entities.
      /// </summary>
      /// <param name="handle">The key handle.</param>
      /// <returns>The cleaned reference objects.</returns>
      public ISet<IFCAnyHandle> CleanRefObjects(IFCAnyHandle handle)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(handle))
            return null;

         if (!TryGetValue(handle, out ISet<IFCAnyHandle> cacheHandles))
            return null;

         IList<IFCAnyHandle> refObjToDel = new List<IFCAnyHandle>();
         foreach (IFCAnyHandle cacheHandle in cacheHandles)
         {

            if (ExporterCacheManager.HandleToDeleteCache.Contains(cacheHandle))
            {
               refObjToDel.Add(cacheHandle);
            }
            else if (IFCAnyHandleUtil.IsNullOrHasNoValue(cacheHandle))
            {
               // If we get to these lines of code, then there is an error somewhere
               // where we deleted a handle but didn't properly mark it as deleted.
               // This should be investigated, but this will at least not prevent
               // the export.
               ExporterCacheManager.HandleToDeleteCache.Add(cacheHandle);
               refObjToDel.Add(cacheHandle);
            }
         }

         foreach (IFCAnyHandle refObjHandle in refObjToDel)
         {
            cacheHandles.Remove(refObjHandle);
         }

         return cacheHandles;
      }
   }
}
