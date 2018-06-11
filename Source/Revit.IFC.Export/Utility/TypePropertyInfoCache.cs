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
using Revit.IFC.Export.Exporter;
using Autodesk.Revit.DB.IFC;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of the type property infos.
   /// </summary>
   public class TypePropertyInfoCache : Dictionary<ElementId, TypePropertyInfo>
   {
      /// <summary>
      /// Checks if the element has type properties.
      /// </summary>
      /// <param name="elementId">The element id.</param>
      /// <returns>True if it has, false if not.</returns>
      public bool HasTypeProperties(ElementId elementId)
      {
         return this.ContainsKey(elementId);
      }

      /// <summary>
      /// Adds new IFC element handles to the existing element type info.
      /// </summary>
      /// <param name="elementId">The element id.</param>
      /// <param name="elements">The IFC elements.</param>
      public void AddNewElementHandles(ElementId elementId, ICollection<IFCAnyHandle> elements)
      {
         TypePropertyInfo typePropertyInfo;
         if (TryGetValue(elementId, out typePropertyInfo))
         {
            foreach (IFCAnyHandle element in elements)
               typePropertyInfo.Elements.Add(element);
         }
      }

      /// <summary>
      /// Adds a new type info of an element.
      /// </summary>
      /// <param name="elementId">The element id.</param>
      /// <param name="propertySets">The property sets.</param>
      /// <param name="elements">The IFC elements.</param>
      public void AddNewTypeProperties(ElementId elementId, ICollection<IFCAnyHandle> propertySets,
          ICollection<IFCAnyHandle> elements)
      {
         TypePropertyInfo typePropertyInfo = new TypePropertyInfo(propertySets, elements);
         Add(elementId, typePropertyInfo);
      }
   }
}