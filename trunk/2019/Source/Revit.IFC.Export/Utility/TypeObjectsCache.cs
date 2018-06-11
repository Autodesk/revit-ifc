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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of the FamilyTypeInfos mapping to a tuple of an ElementId, a Boolean and an IFCExportType.
   /// The ElementID is used to differentiate between elements of different family types.
   /// The Boolean is used solely for doors and windows to signal if the doors or windows are flipped or not, the default value is false
   /// for non-doors and windows.
   /// The export type is used to distinguish between two instances of the same family but have different export types, this can happen
   /// if user uses the IfcExportAs shared parameter at the instance level.
   /// </summary>
   public class TypeObjectsCache : Dictionary<Tuple<ElementId, bool, IFCEntityType>, FamilyTypeInfo>
   {
      /// <summary>
      /// Adds the FamilyTypeInfo to the dictionary.
      /// </summary>
      /// <param name="elementId">
      /// The element elementId.
      /// </param>
      /// <param name="flipped">
      /// Indicates if the element is flipped.
      /// </param>
      /// <param name="exportType">
      /// The export type of the element.
      /// </param>
      public void Register(ElementId elementId, bool flipped, IFCEntityType exportType, FamilyTypeInfo typeInfo)
      {
         Tuple<ElementId, bool, IFCEntityType> key = new Tuple<ElementId, bool, IFCEntityType>(elementId, flipped, exportType);
         this[key] = typeInfo;
      }

      /// <summary>
      /// Finds the FamilyTypeInfo from the dictionary.
      /// </summary>
      /// <param name="elementId">
      /// The element elementId.
      /// </param>
      /// <param name="flipped">
      /// Indicates if the element is flipped.
      /// </param>
      /// <param name="exportType">
      /// The export type of the element.
      /// </param>
      /// <returns>
      /// The FamilyTypeInfo object.
      /// </returns>
      public FamilyTypeInfo Find(ElementId elementId, bool flipped, IFCEntityType exportType)
      {
         Tuple<ElementId, bool, IFCEntityType> key = new Tuple<ElementId, bool, IFCEntityType>(elementId, flipped, exportType);
         FamilyTypeInfo typeInfo;

         if (TryGetValue(key, out typeInfo))
            return typeInfo;

         return new FamilyTypeInfo();
      }
   }
}
