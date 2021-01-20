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
   /// Used to keep a cache of the IfcObject handles mapping to a IfcTypeObject handle.
   /// </summary>
   public class TypeRelationsCache : Dictionary<IFCAnyHandle, HashSet<IFCAnyHandle>>
   {
      /// <summary>
      /// Adds the IfcObject to the dictionary.
      /// </summary>
      /// <param name="typeObj">The IfcTypeObject handle.</param>
      /// <param name="obj">The IfcObject handle.</param>
      public void Add(IFCAnyHandle typeObj, IFCAnyHandle obj)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(typeObj))
            return;

         if (ContainsKey(typeObj))
         {
            this[typeObj].Add(obj);
         }
         else
         {
            HashSet<IFCAnyHandle> objs = new HashSet<IFCAnyHandle>();
            objs.Add(obj);
            this[typeObj] = objs;
         }
      }

      /// <summary>
      /// To clean the set of reference objects from the type in the case of some deleted entity
      /// </summary>
      /// <param name="typeObj">Thpe handle</param>
      public void CleanRefObjects(IFCAnyHandle typeObj)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(typeObj))
            return;

         if (ContainsKey(typeObj))
         {
            IList<IFCAnyHandle> refObjToDel = new List<IFCAnyHandle>();
            foreach (IFCAnyHandle handle in this[typeObj])
            {
               if (ExporterCacheManager.HandleToDeleteCache.Contains(handle))
                  refObjToDel.Add(handle);
               else if (!IFCAnyHandleUtil.IsValidHandle(typeObj) || !IFCAnyHandleUtil.IsValidHandle(handle))
                  refObjToDel.Add(handle);
            }
            foreach (IFCAnyHandle handle in refObjToDel)
               this[typeObj].Remove(handle);
         }
         else
            return;
      }
   }
}
