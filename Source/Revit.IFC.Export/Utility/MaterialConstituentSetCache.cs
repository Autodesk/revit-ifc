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
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Class to define the quality comparer for the sets
   /// </summary>
   public class ConstituentSetComparer : IEqualityComparer<ISet<IFCAnyHandle>>
   {
      /// <summary>
      /// Whether the two Sets are equal
      /// </summary>
      public bool Equals(ISet<IFCAnyHandle> set1, ISet<IFCAnyHandle> set2)
      {
         return set1.SetEquals(set2);
      }

      /// <summary>
      /// Return the hash code for this Set.
      /// </summary>
      public int GetHashCode(ISet<IFCAnyHandle> theSet)
      {
         // Stores the result.
         int result = 0;

         // Don't compute hash code on null object.
         if (theSet == null)
            return 0;

         if (theSet.Count == 0)
            return 0;

         foreach (IFCAnyHandle setMember in theSet)
         {
            // Compute hash for the set using the member Ids and a prime number 251
            result += result * 251 + setMember.Id;
         }
         return result;
      }
   }

   /// <summary>
   /// Used to keep a cache of MaterialConstituentSet (new in IFC4). Since objects may be 
   /// linked to the same set (but not necessarily in the same order), this cache is chiefly 
   /// responsible to keep the unique set regardless of the order inside it.
   /// </summary>
   public class MaterialConstituentSetCache : BaseRelationsCache
   {
      /// <summary>
      /// The dictionary mapping the IfcMaterialConstituentSet to its handle. 
      /// </summary>
      private IDictionary<ISet<IFCAnyHandle>, IFCAnyHandle> MatConstituentSetDictionary
      { get; set; } = new Dictionary<ISet<IFCAnyHandle>, IFCAnyHandle>(new ConstituentSetComparer());

      /// <summary>
      /// The dictionary mapping from an ElementId to an IfcMaterialConstituentSet
      /// </summary>
      private IDictionary<long, IFCAnyHandle> ElementIdToHandle
      { get; set; } = new SortedDictionary<long, IFCAnyHandle>();

      /// <summary>
      /// Finds the appopriate Handle for the IfcMaterialConstituentSet from the dictionary.
      /// </summary>
      /// <param name="constituentSet">The element id.</param>
      /// <returns>The HashSet of the IfcMaterialConstituentSet.</returns>
      public IFCAnyHandle Find(ISet<IFCAnyHandle> constituentSet)
      {
         if (MatConstituentSetDictionary.TryGetValue(constituentSet, out IFCAnyHandle constituentSetHandle))
            return constituentSetHandle;
         
         return null;
      }

      /// <summary>
      /// Finds the IfcMaterialConstituentSet handle from the dictionary.
      /// </summary>
      /// <param name="id">The element id.</param>
      /// <returns>The IfcMaterial handle.</returns>
      public IFCAnyHandle Find(ElementId id)
      {
         if (ElementIdToHandle.TryGetValue(id.Value, out IFCAnyHandle handle))
            return handle;

         return null;
      }

      /// <summary>
      /// Adds the IfcMaterialConstituentSet and its handle to the dictionary.
      /// </summary>
      /// <param name="elementId">The element elementId.</param>
      /// <param name="instanceHandle">The element instance handle.</param>
      /// <param name="constituentSetHandle">The IfcMaterialConstituentSet.</param>
      /// <param name="constituents">The set of IfcMaterialConstituents.</param>
      public void Register(ElementId elementId, IFCAnyHandle instanceHandle,
         IFCAnyHandle constituentSetHandle, ISet<IFCAnyHandle> constituents)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(constituentSetHandle))
            return;

         if (elementId != ElementId.InvalidElementId && !ElementIdToHandle.ContainsKey(elementId.Value))
            ElementIdToHandle[elementId.Value] = constituentSetHandle;

         if (!MatConstituentSetDictionary.ContainsKey(constituents))
            MatConstituentSetDictionary[constituents] = constituentSetHandle;

         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(instanceHandle))
         {
            if (!Cache.TryGetValue(constituentSetHandle, out ISet<IFCAnyHandle> relatedObjects))
            {
               relatedObjects = new HashSet<IFCAnyHandle>();
               Cache[constituentSetHandle] = relatedObjects;
            }
            relatedObjects.Add(instanceHandle);
         }
      }
   }
}