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

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Contains information about IFC property sets and IFC type elements.
   /// </summary>
   public class TypePropertyInfo
   {
      bool m_assignedToType = false;
      HashSet<IFCAnyHandle> m_PropertySets;
      HashSet<IFCAnyHandle> m_Elements;

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected TypePropertyInfo()
      {
      }

      /// <summary>
      /// The flag that determines if the type properties have been associated with an IfcTypeObject, and should not
      /// be associated with an IfcElement.
      /// </summary>
      public bool AssignedToType
      {
         get { return m_assignedToType; }
         set { m_assignedToType = value; }
      }

      /// <summary>
      /// Constructs a TypePropertyInfo objects.
      /// </summary>
      /// <param name="propertySets">The property sets.</param>
      /// <param name="elements">The elements.</param>
      public TypePropertyInfo(ICollection<IFCAnyHandle> propertySets, ICollection<IFCAnyHandle> elements)
      {
         m_PropertySets = new HashSet<IFCAnyHandle>(propertySets);
         m_Elements = new HashSet<IFCAnyHandle>(elements);
      }

      /// <summary>
      /// The property sets.
      /// </summary>
      public HashSet<IFCAnyHandle> PropertySets
      {
         get { return m_PropertySets; }
      }

      /// <summary>
      /// The IFC elements.
      /// </summary>
      public HashSet<IFCAnyHandle> Elements
      {
         get { return m_Elements; }
      }
   }
}