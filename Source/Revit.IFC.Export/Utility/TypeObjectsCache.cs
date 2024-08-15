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
using Revit.IFC.Export.Exporter;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// TypeObjectKey has six components:
   /// 1. Symbol id.
   /// 2. Level id (if we are splitting by level).
   /// 3. Flipped (true if the symbol is flipped).
   /// 4. The corresponding IFC entity type.
   /// 5. The corresponding IFC predefined type.
   /// 6. Override material id.
   /// </summary>
   public sealed class TypeObjectKey : Tuple<ElementId, ElementId, bool, IFCEntityType, string, ElementId>
   {
      public TypeObjectKey(ElementId elementId, ElementId levelId, bool flipped,
         IFCExportInfoPair exportType, ElementId materialId) :
         base(elementId, levelId, flipped, exportType.ExportType, exportType.PredefinedType, materialId)
      { }

      public ElementId ElementId { get { return Item1; } }

      public ElementId LevelId { get { return Item2; } }

      public bool IsFlipped { get { return Item3; } }
      
      public IFCEntityType EntityType { get { return Item4; } }

      public string PredefinedType { get { return Item5; } }

      public ElementId MaterialId { get { return Item6; } }
   }

   /// <summary>
   /// Used to keep a cache of the FamilyTypeInfos mapping to a tuple of an ElementId, a Boolean and an IFCExportType.
   /// The ElementID is used to differentiate between elements of different family types.
   /// The Boolean is used solely for doors and windows to signal if the doors or windows are flipped or not, the default value is false
   /// for non-doors and windows.
   /// The export type is used to distinguish between two instances of the same family but have different export types, this can happen
   /// if user uses the IFC_EXPORT_ELEMENT_AS parameter at the instance level.
   /// </summary>
   public class TypeObjectsCache : Dictionary<TypeObjectKey, FamilyTypeInfo>
   {
      /// <summary>
      /// A dictionary for use for type objects that can't be shared but for whom we want
      /// stable GUIDS.
      /// </summary>
      /// <remarks>GUID_TODO: This is a workaround for types that have opening information,
      /// which we don't support reuse of.  this allows us to increment a counter when
      /// we make copies of the original type object.</remarks>
      IDictionary<TypeObjectKey, int> AlternateGUIDCounter = new Dictionary<TypeObjectKey, int>();

      /// <summary>
      /// Adds the FamilyTypeInfo to the dictionary.
      /// </summary>
      /// <param name="key">The information that identifies the type object.</param>
      /// <param name="typeInfo">The information that defines the type object.</param>
      /// <param name="hasOpenings">True if the type object contains openings.</param>
      /// <remarks>If the type object contains openings, we don't want to re-use it,
      /// but we do want to avoid having duplicate GUIDs.  This is less stable than
      /// we'd like to achieve, but requires a re-working of how we compare opening
      /// information.</remarks>
      public void Register(TypeObjectKey key, FamilyTypeInfo typeInfo, bool hasOpenings)
      {
         if (hasOpenings)
         {
            if (AlternateGUIDCounter.ContainsKey(key))
               AlternateGUIDCounter[key]++; 
            else
               AlternateGUIDCounter[key] = 1;
            return;
         }

         this[key] = typeInfo;
      }

      /// <summary>
      /// Finds the FamilyTypeInfo from the dictionary.
      /// </summary>
      /// <param name="key">The information that identifies the type object.</param>
      /// <returns>The FamilyTypeInfo object.</returns>
      public FamilyTypeInfo Find(TypeObjectKey key)
      {
         FamilyTypeInfo typeInfo;

         if (TryGetValue(key, out typeInfo))
            return typeInfo;

         return new FamilyTypeInfo();
      }

      /// <summary>
      /// Looks for the current alternate GUID index for a particular key, if it exists.
      /// </summary>
      /// <param name="key">The information that identifies the type object.</param>
      /// <returns>The index, or null if it doesn't exist.</returns>
      public int? GetAlternateGUIDIndex(TypeObjectKey key)
      {
         if (!AlternateGUIDCounter.TryGetValue(key, out int index))
            return null;
         return index;
      }
   }
}
