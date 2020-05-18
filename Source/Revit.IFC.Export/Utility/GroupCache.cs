//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to import and export IFC files containing model geometry.
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
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of the created IfcGroups and related IfcElement handles.
   /// </summary>
   public class GroupCache : Dictionary<ElementId, GroupInfo>
   {
      /// <summary>
      /// Add the handle of a Group to the cache.  It will either add it
      /// to an existing entry, or create a new entry.
      /// </summary>
      /// <param name="groupId">The elementId of the Group.</param>
      /// <param name="groupHnd">The handle of the IfcGroup.</param>
      /// <returns>GroupInfo of the registered group</returns>
      public GroupInfo RegisterGroup(ElementId groupId, IFCAnyHandle groupHnd)
      {
         GroupInfo groupInfo;
         if (!TryGetValue(groupId, out groupInfo))
         {
            groupInfo = new GroupInfo();
         }
         groupInfo.GroupHandle = groupHnd;
         this[groupId] = groupInfo;
         return groupInfo;
      }

      /// <summary>
      /// Specify the export type of the cached group.
      /// </summary>
      /// <param name="groupId">The elementId of the Group.</param>
      /// <param name="type">The export type of the Group.</param>
      public void RegisterGroupType(ElementId groupId, IFCExportInfoPair type)
      {
         GroupInfo groupInfo;
         if (!TryGetValue(groupId, out groupInfo))
         {
            groupInfo = new GroupInfo();
         }
         groupInfo.GroupType = type;
         this[groupId] = groupInfo;
      }

      /// <summary>
      /// Add the handle of an element in a Group to the cache.  It will either add it
      /// to an existing entry, or create a new entry.
      /// </summary>
      /// <param name="elementId">The ElementId of the Group.</param>
      /// <param name="elementHnd">The IFC handle of the element.</param>
      public void RegisterElement(ElementId elementId, IFCAnyHandle elementHnd)
      {
         GroupInfo groupInfo;
         if (!TryGetValue(elementId, out groupInfo))
         {
            groupInfo = new GroupInfo();
         }
         groupInfo.ElementHandles.Add(elementHnd);
         this[elementId] = groupInfo;
      }

      /// <summary>
      /// Add the handle of one or more elements in an Group to the cache.  It will either add it
      /// to an existing entry, or create a new entry.
      /// </summary>
      /// <param name="elementId">The ElementId of the group.</param>
      /// <param name="elementHnds">The IFC handle of the elements.</param>
      public void RegisterElements(ElementId groupId, HashSet<IFCAnyHandle> elementHnds)
      {
         if (elementHnds.Count == 0)
            return;

         GroupInfo groupInfo;
         if (!TryGetValue(groupId, out groupInfo))
         {
            groupInfo = new GroupInfo();
         }

         groupInfo.ElementHandles.UnionWith(elementHnds);
         this[groupId] = groupInfo;
      }

      /// <summary>
      /// Registers all of the created handles in the product wrapper that are of the right type to a Group.
      /// </summary>
      /// <param name="groupId">The ElementId of the Group.</param>
      /// <param name="productWrapper">The product wrapper.</param>
      public void RegisterElements(ElementId groupId, ProductWrapper productWrapper)
      {
         ICollection<IFCAnyHandle> objects = productWrapper.GetAllObjects();
         HashSet<IFCAnyHandle> elementsToAdd = new HashSet<IFCAnyHandle>();
         foreach (IFCAnyHandle hnd in objects)
         {
            if (IFCAnyHandleUtil.IsSubTypeOf(hnd, IFCEntityType.IfcProduct) ||
                IFCAnyHandleUtil.IsSubTypeOf(hnd, IFCEntityType.IfcGroup))
               elementsToAdd.Add(hnd);
         }
         if (elementsToAdd.Count > 0)
            RegisterElements(groupId, elementsToAdd);
      }
   }
}