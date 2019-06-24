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
   /// Used to keep a cache of the IfcRoot handles mapping to an IfcMaterial or IfcMaterialList handle.
   /// </summary>
   public class MaterialRelationsCache : Dictionary<IFCAnyHandle, HashSet<IFCAnyHandle>>
   {
      /// <summary>
      /// Adds the IfcRoot handle to the dictionary.
      /// </summary>
      /// <param name="material">The material handle.</param>
      /// <param name="product">The product handle.</param>
      public void Add(IFCAnyHandle material, IFCAnyHandle product)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(material))
            return;

         if (ContainsKey(material))
         {
            this[material].Add(product);
         }
         else
         {
            HashSet<IFCAnyHandle> products = new HashSet<IFCAnyHandle>();
            products.Add(product);
            this[material] = products;
         }
      }

      /// <summary>
      /// To clean the set of reference objects from the material in the case of some deleted entity
      /// </summary>
      /// <param name="material">the material</param>
      public void CleanRefObjects(IFCAnyHandle material)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(material))
            return;

         if (ContainsKey(material))
         {
            IList<IFCAnyHandle> refObjToDel = new List<IFCAnyHandle>();
            foreach (IFCAnyHandle handle in this[material])
            {
               if (ExporterCacheManager.HandleToDeleteCache.Contains(handle))
                  refObjToDel.Add(handle);
            }
            foreach (IFCAnyHandle handle in refObjToDel)
               this[material].Remove(handle);
         }
         else
            return;
      }
   }
}
