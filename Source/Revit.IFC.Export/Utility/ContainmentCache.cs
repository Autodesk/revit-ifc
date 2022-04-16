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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of the elements contained in another element.
   /// For example, by default, IFCPROJECT would have one item, IFCSITE.
   /// </summary>
   public class ContainmentCache 
   {
      public Dictionary<IFCAnyHandle, HashSet<IFCAnyHandle>> Cache { get; set; } = 
         new Dictionary<IFCAnyHandle, HashSet<IFCAnyHandle>>();

      Dictionary<IFCAnyHandle, string> ContainerGUIDs { get; set; }  = 
         new Dictionary<IFCAnyHandle, string>();

      /// <summary>
      /// Get the GUID for the IFCRELAGGREGATES.
      /// </summary>
      /// <param name="container">The container.</param>
      /// <returns>The GUID, if it exists.</returns>
      public string GetGUIDForRelation(IFCAnyHandle container)
      {
         return ContainerGUIDs.TryGetValue(container, out string existingGUID) ? existingGUID : null;
      }

      private HashSet<IFCAnyHandle> GetContainedItemsForHandle(IFCAnyHandle container, string guid)
      {
         if (!Cache.TryGetValue(container, out HashSet<IFCAnyHandle> containedItems))
         {
            ContainerGUIDs[container] = guid ?? 
               GUIDUtil.GenerateIFCGuidFrom(IFCEntityType.IfcRelContainedInSpatialStructure, container);
            containedItems = new HashSet<IFCAnyHandle>();
            Cache[container] = containedItems;
         }
         return containedItems;
      }

      /// <summary>
      /// And an object to a container.
      /// </summary>
      /// <param name="container">The container.</param>
      /// <param name="objectHnd">The object to add.</param>
      public void AddRelation(IFCAnyHandle container, IFCAnyHandle objectHnd)
      {
         HashSet<IFCAnyHandle> containedItems = GetContainedItemsForHandle(container, null);
         containedItems.Add(objectHnd);
      }

      /// <summary>
      /// And a collection of objects to a container.
      /// </summary>
      /// <param name="container">The container.</param>
      /// <param name="objectHnds">The objects to add.</param>
      public void AddRelations(IFCAnyHandle container, string guid, ICollection<IFCAnyHandle> objectHnds)
      {
         HashSet<IFCAnyHandle> containedItems = GetContainedItemsForHandle(container, guid);
         containedItems.UnionWith(objectHnds);
      }
   }
}