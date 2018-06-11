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
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of the elements contained in another element.
   /// For example, by default, IFCPROJECT would have one item, IFCSITE.
   /// </summary>
   public class ContainmentCache : Dictionary<IFCAnyHandle, ICollection<IFCAnyHandle>>
   {
      Dictionary<IFCAnyHandle, string> m_ContainerGUIDs = new Dictionary<IFCAnyHandle, string>();

      /// <summary>
      /// Define the GUID for the IFCRELAGGREGATES.
      /// </summary>
      /// <param name="container">The container.</param>
      /// <param name="guid">The guid.</param>
      public void SetGUIDForRelation(IFCAnyHandle container, string guid)
      {
         string existingGUID;
         if (m_ContainerGUIDs.TryGetValue(container, out existingGUID))
            throw new InvalidOperationException("GUID is already set.");
         m_ContainerGUIDs[container] = guid;
      }

      /// <summary>
      /// Get the GUID for the IFCRELAGGREGATES.
      /// </summary>
      /// <param name="container">The container.</param>
      /// <returns>The GUID, if it exists.</returns>
      public string GetGUIDForRelation(IFCAnyHandle container)
      {
         string existingGUID = null;
         m_ContainerGUIDs.TryGetValue(container, out existingGUID);
         return existingGUID;
      }

      /// <summary>
      /// And an object to a container.
      /// </summary>
      /// <param name="container">The container.</param>
      /// <param name="objectHnd">The object to add.</param>
      public void AddRelation(IFCAnyHandle container, IFCAnyHandle objectHnd)
      {
         ICollection<IFCAnyHandle> containedItems;
         if (!TryGetValue(container, out containedItems))
         {
            containedItems = new HashSet<IFCAnyHandle>();
            this[container] = containedItems;
         }
         containedItems.Add(objectHnd);
      }

      /// <summary>
      /// And a collection of objects to a container.
      /// </summary>
      /// <param name="container">The container.</param>
      /// <param name="objectHnds">The objects to add.</param>
      public void AddRelations(IFCAnyHandle container, ICollection<IFCAnyHandle> objectHnds)
      {
         foreach (IFCAnyHandle objectHnd in objectHnds)
            AddRelation(container, objectHnd);
      }
   }
}