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
   public class BaseRelationsCache
   {
      public class IFCAnyHandleComparer : IComparer<IFCAnyHandle>
      {
         /// <summary>
         /// A comparison for two IFCAnyHandles.
         /// </summary>
         /// <param name="hnd1">The first handle.</param>
         /// <param name="hnd2">The second handle.</param>
         /// <returns>-1 if the first handle is smaller, 1 if larger, 0 if equal.</returns>
         /// <remarks>This function assumes both handles are valid.</remarks>
         public int Compare(IFCAnyHandle hnd1, IFCAnyHandle hnd2)
         {
            int id1 = hnd1.Id;
            int id2 = hnd2.Id;
            return (id1 < id2) ? -1 : ((id1 > id2) ? 1 : 0);
         }
      }

      public IDictionary<IFCAnyHandle, ISet<IFCAnyHandle>> Cache { get; } =
         new SortedDictionary<IFCAnyHandle, ISet<IFCAnyHandle>>(new IFCAnyHandleComparer());

      public ICollection<IFCAnyHandle> Keys { get { return Cache.Keys; } }
      
      /// <summary>
      /// Adds the IfcRoot handle to the dictionary.
      /// </summary>
      /// <param name="handle">The handle.</param>
      /// <param name="product">The related product handle.</param>
      public void Add(IFCAnyHandle handle, IFCAnyHandle product)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(handle) || IFCAnyHandleUtil.IsNullOrHasNoValue(product))
            return;

         if (Cache.ContainsKey(handle))
         {
            Cache[handle].Add(product);
         }
         else
         {
            HashSet<IFCAnyHandle> products = new HashSet<IFCAnyHandle>();
            products.Add(product);
            Cache[handle] = products;
         }
      }

      /// <summary>
      /// Try to get the set of handles associated with a particular key.
      /// </summary>
      /// <param name="handle">The key.</param>
      /// <param name="values">The set of associated values.</param>
      /// <returns>True if the handle is in the dictionary.</returns>
      public bool TryGetValue(IFCAnyHandle handle, out ISet<IFCAnyHandle> values)
      {
         return Cache.TryGetValue(handle, out values);
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

         if (!Cache.TryGetValue(handle, out ISet<IFCAnyHandle> cacheHandles))
            return null;

         return ExporterUtil.CleanRefObjects(cacheHandles);
      }
   }
}
