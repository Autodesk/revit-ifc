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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet
{
   /// <summary>
   /// A description mapping of a group of Revit parameters and/or calculated values to an IfcPropertySet or IfcElementQuantity.
   /// </summary>
   /// <remarks>
   /// A property or quantity set mapping is valid for only one entity type.
   /// </remarks>
   abstract public class Description
   {
      /// <summary>
      /// The name of the property or quantity set.
      /// </summary>
      public string Name { get; set; } = String.Empty;

      /// <summary>
      /// The optional description of the property set or quantity.  Null by default.
      /// </summary>
      public string DescriptionOfSet { get; set; } = null;

      /// <summary>
      /// The element id of the ViewSchedule that generatd this description.
      /// </summary>
      public ElementId ViewScheduleId { get; set; } = ElementId.InvalidElementId;

      /// <summary>
      /// The type of element appropriate for this property or quantity set.
      /// </summary>
      public HashSet<IFCEntityType> EntityTypes { get; } = new HashSet<IFCEntityType>();

      private string m_ObjectType = null;
      /// <summary>
      /// The object type of element appropriate for this property or quantity set.
      /// Primarily used for identifying proxies.
      /// </summary>
      /// <remarks>Only one ObjectType is supported.</remarks>
      public string ObjectType 
      { 
         private get 
         { 
            return m_ObjectType;  
         }
         set 
         {
            // The data in ObjectType is frequently wrong and should be fixed.  In the meantime, we will
            // fix the value here.
            // Note that we expect only one object type - we will revisit that assumption upon fixing the data.
            string[] objectTypeList = value.Split(',') ?? new string[] { };
            foreach (string objectType in objectTypeList)
            {
               if (!(objectType.StartsWith("IFC", StringComparison.InvariantCultureIgnoreCase)
                  || objectType.StartsWith("Pset", StringComparison.InvariantCultureIgnoreCase)))
               {
                  m_ObjectType = objectType;
                  return;
               }
            }
            m_ObjectType = string.Empty; 
         } 
      }

      /// <summary>
      /// The pre-defined type of element appropriate for this property or quantity set.
      /// Primarily used for identifying sub-types of MEP objects.
      /// </summary>
      /// <remarks>Currently limited to one entity type.</remarks>
      public string PredefinedType { get; set; } = string.Empty;

      /// <summary>
      /// The redirect calculator associated with this property or quantity set.
      /// </summary>
      public DescriptionCalculator DescriptionCalculator { get; set; }

      /// <summary>
      /// Identifies if the input handle is sub type of one IFCEntityType in the EntityTypes list.
      /// </summary>
      /// <param name="handle">The handle.</param>
      /// <returns>True if it is sub type, false otherwise.</returns>
      private bool IsSubTypeOfEntityTypes(IFCAnyHandle handle)
      {
         // Note that although EntityTypes is represented as a set, we still need to go through each item in the last to check for subtypes.
         foreach (IFCEntityType entityType in EntityTypes)
         {
            if (IFCAnyHandleUtil.IsSubTypeOf(handle, entityType))
               return true;
         }
         return false;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="handle"></param>
      /// <returns></returns>
      private bool IsSubTypeOfEntityTypes(IFCEntityType ifcEntityType)
      {
         IFCVersion ifcVersion = ExporterCacheManager.ExportOptionsCache.FileVersion;
         var ifcEntitySchemaTree = IfcSchemaEntityTree.GetEntityDictFor(ExporterCacheManager.ExportOptionsCache.FileVersion);
         if (ifcEntitySchemaTree == null || ifcEntitySchemaTree.IfcEntityDict == null || ifcEntitySchemaTree.IfcEntityDict.Count == 0)
            return false;

         // Note that although EntityTypes is represented as a set, we still need to go through each item in the last to check for subtypes.
         foreach (IFCEntityType entityType in EntityTypes)
         {
            if (IfcSchemaEntityTree.IsSubTypeOf(ifcVersion, ifcEntityType.ToString(), entityType.ToString(), strict: false))
               return true;
         }
         return false;
      }

      /// <summary>
      /// Identifies if the input handle matches the type of element, and optionally the object type, 
      /// to which this description applies.
      /// </summary>
      /// <param name="handle">The handle.</param>
      /// <returns>True if it matches, false otherwise.</returns>
      public bool IsAppropriateType(IFCAnyHandle handle)
      {
         if (handle == null || !IsSubTypeOfEntityTypes(handle))
            return false;
         if (string.IsNullOrEmpty(ObjectType))
            return true;

         IFCEntityType entityType = IFCAnyHandleUtil.GetEntityType(handle);
         return EntityTypes.Contains(entityType);
      }

      /// <summary>
      /// Identifies if either the entity or object type match this description.
      /// </summary>
      /// <param name="entity">the Entity</param>
      /// <returns>true if matches</returns>
      public bool IsAppropriateEntityAndObjectType(IFCEntityType entity, string objectType)
      {
         if (entity == IFCEntityType.UnKnown || !IsSubTypeOfEntityTypes(entity))
         {
            return false;
         }

         if (string.IsNullOrEmpty(ObjectType))
         {
            return true;
         }

         return string.Equals(ObjectType, objectType, StringComparison.InvariantCultureIgnoreCase);
      }

      /// <summary>
      /// Checks if the input string matches the non-empty object type of the description.
      /// </summary>
      /// <param name="objectType">The object type to check.</param>
      /// <returns></returns>
      public bool IsValidObjectType(string objectType)
      {
         return string.IsNullOrEmpty(ObjectType) ? false : 
            string.Equals(ObjectType, objectType, StringComparison.InvariantCultureIgnoreCase);
      }
   }
}
