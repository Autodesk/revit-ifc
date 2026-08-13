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

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Caches the ingredients needed to create IfcPropertySet handles for a Revit
   /// ElementType. Stores pset names and reusable individual IfcProperty handles
   /// rather than finished IfcPropertySet handles, because each IfcPropertySet
   /// must be unique to one IfcTypeObject per the IFC spec.
   /// </summary>
   public class TypePropertyInfo
   {
      /// <summary>
      /// The flag that determines if the type properties have been associated with an IfcTypeObject, and should not
      /// be associated with an IfcElement.
      /// </summary>
      public bool AssignedToType { get; set; }

      /// <summary>
      /// The property set ingredients: each entry is a pset display name paired with
      /// the individual IfcProperty handles that belong in that set. A fresh
      /// IfcPropertySet handle is created from these ingredients for each consuming
      /// IfcTypeObject.
      /// </summary>
      public IList<(string PsetName, HashSet<IFCAnyHandle> Properties)> PropertyInputs { get; }

      /// <summary>
      /// The IFC elements.
      /// </summary>
      public HashSet<IFCAnyHandle> Elements { get; }

      /// <summary>
      /// Constructs a TypePropertyInfo object.
      /// </summary>
      /// <param name="propertyInputs">The property set ingredients (name + individual property handles).</param>
      /// <param name="elements">The IFC element handles.</param>
      public TypePropertyInfo(
         IList<(string PsetName, HashSet<IFCAnyHandle> Properties)> propertyInputs,
         ICollection<IFCAnyHandle> elements)
      {
         PropertyInputs = propertyInputs;
         Elements = new HashSet<IFCAnyHandle>(elements);
      }
   }
}