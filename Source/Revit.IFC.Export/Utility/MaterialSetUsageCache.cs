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

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of the IfcRoot handles mapping to a IfcMaterial___SetUsage handle. It includes IfcMaterialLayerSetUsage, IfcMaterialProfileSetUsage in IFC4
   /// </summary>
   public class MaterialSetUsageCache : BaseRelationsCache
   {
      private IDictionary<IFCAnyHandle, string> UsageToHashCache { get; } = new Dictionary<IFCAnyHandle, string>();

      private IDictionary<string, IFCAnyHandle> HashToUsageCache { get; } = new Dictionary<string, IFCAnyHandle>();

      public void AddHash(IFCAnyHandle handle, string hash)
      {
         UsageToHashCache[handle] = hash;
         HashToUsageCache[hash] = handle;
      }

      /// <summary>
      /// Get the previously stored hash value for a particular handle, if it exists.
      /// </summary>
      /// <param name="handle">The handle.</param>
      /// <returns>The hash value, or the empty string if it can't be found.</returns>
      /// <remarks>Strictly speaking, it is a bug if the hash value can't be found,
      /// but the rest of the logic should compensate for that.</remarks>
      public string GetHash(IFCAnyHandle handle)
      {
         if (!UsageToHashCache.TryGetValue(handle, out string hash))
            return string.Empty;
         return hash;
      }

      /// <summary>
      /// Get the previously stored handle value for a particular hash, if it exists.
      /// </summary>
      /// <param name="hash">The hash.</param>
      /// <returns>The handle, or null if it can't be found.</returns>
      public IFCAnyHandle GetHandle(string hash)
      {
         if (!HashToUsageCache.TryGetValue(hash, out IFCAnyHandle handle))
            return null;
         return handle;
      }
   }
}
