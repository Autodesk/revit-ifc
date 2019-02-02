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
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
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
      /// The name to be used to create property set or quantity.
      /// </summary>
      string m_Name = String.Empty;

      /// <summary>
      /// The optional description of the property set or quantity.  Null by default.
      /// </summary>
      string m_Description = null;

      /// <summary>
      /// The element id of the view schedule generating this Description, if appropriate.
      /// </summary>
      ElementId m_ViewScheduleId = ElementId.InvalidElementId;

      /// <summary>
      /// The types of element appropriate for this property or quantity set.
      /// </summary>
      HashSet<IFCEntityType> m_IFCEntityTypes = new HashSet<IFCEntityType>();

      /// <summary>
      /// The object type of element appropriate for this property or quantity set.
      /// </summary>
      string m_ObjectType = String.Empty;

      /// <summary>
      /// The predefined or shape type of element appropriate for this property or quantity set.
      /// </summary>
      string m_PredefinedType = String.Empty;

      /// <summary>
      /// The index used to create a consistent GUID for this item.
      /// It is expected that this index will come from the list in IFCSubElementEnums.cs.
      /// </summary>
      int m_SubElementIndex = -1;

      /// <summary>
      /// The redirect calculator associated with this property or quantity set.
      /// </summary>
      DescriptionCalculator m_DescriptionCalculator;

      /// <summary>
      /// Identifies if the input handle is sub type of one IFCEntityType in the EntityTypes list.
      /// </summary>
      /// <param name="handle">The handle.</param>
      /// <returns>True if it is sub type, false otherwise.</returns>
      public bool IsSubTypeOfEntityTypes(IFCAnyHandle handle)
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
      public bool IsSubTypeOfEntityTypes(IFCEntityType ifcEntityType)
      {
         var ifcEntitySchemaTree = IfcSchemaEntityTree.GetEntityDictFor(ExporterCacheManager.ExportOptionsCache.FileVersion);
         if (ifcEntitySchemaTree == null || ifcEntitySchemaTree.Count == 0)
            return false;

         // Note that although EntityTypes is represented as a set, we still need to go through each item in the last to check for subtypes.
         foreach (IFCEntityType entityType in EntityTypes)
         {
            if (IfcSchemaEntityTree.IsSubTypeOf(ifcEntityType.ToString(), entityType.ToString(), strict: false))
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
         if (ObjectType == "")
            return true;

         string objectType = IFCAnyHandleUtil.GetObjectType(handle);
         return (NamingUtil.IsEqualIgnoringCaseAndSpaces(ObjectType, objectType));
      }

      /// <summary>
      /// Identifies if the input handle matches the type of element only to which this description applies.
      /// </summary>
      /// <param name="handle">
      /// The handle.
      /// </param>
      /// <returns>
      /// True if it matches, false otherwise.
      /// </returns>
      public bool IsAppropriateEntityType(IFCAnyHandle handle)
      {
         if (handle == null || !IsSubTypeOfEntityTypes(handle))
            return false;
         return true;
      }

      /// <summary>
      /// Identifies if the input type matches the type of element only to which this description applies.
      /// </summary>
      /// <param name="entity">the Entity</param>
      /// <returns>true if matches</returns>
      public bool IsAppropriateEntityType(IFCEntityType entity)
      {
         if (entity == IFCEntityType.UnKnown || !IsSubTypeOfEntityTypes(entity))
            return false;
         return true;
      }

      /// <summary>
      /// Identifies if the input handle matches the object type only to which this description applies.
      /// </summary>
      /// <param name="handle">
      /// The handle.
      /// </param>
      /// <returns>
      /// True if it matches, false otherwise.
      /// </returns>
      public bool IsAppropriateObjectType(IFCAnyHandle handle)
      {
         if (handle == null)
            return false;
         if (ObjectType == "")
            return true;

         // ObjectType information comes from PSD's Applicable Type. This may be a comma separated list of applicable type
         //string objectType = IFCAnyHandleUtil.GetObjectType(handle);
         IFCEntityType hndEntity = IFCAnyHandleUtil.GetEntityType(handle);
         if (ObjectType.IndexOf(hndEntity.ToString(), StringComparison.InvariantCultureIgnoreCase) < 0)
            return false;
         else
            return true;
         //return (NamingUtil.IsEqualIgnoringCaseAndSpaces(ObjectType, objectType));
      }

      /// <summary>
      /// Identifies if the input handle matches the object type only to which this description applies.
      /// </summary>
      /// <param name="entityType">the entity type</param>
      /// <returns>true if found match</returns>
      public bool IsAppropriateObjectType(IFCEntityType entityType)
      {
         if (ObjectType == "")
            return true;
         if (entityType == IFCEntityType.UnKnown)
            return false;

         // ObjectType information comes from PSD's Applicable Type. This may be a comma separated list of applicable type
         if (ObjectType.IndexOf(entityType.ToString(), StringComparison.InvariantCultureIgnoreCase) < 0)
            return false;
         else
            return true;

         //string objectType = IFCAnyHandleUtil.GetObjectType(handle);
         //return (NamingUtil.IsEqualIgnoringCaseAndSpaces(ObjectType, objectType));
      }

      /// <summary>
      /// Identifies if the input handle matches the predefined type only to which this description applies.
      /// </summary>
      /// <param name="handle">The handle.</param>
      /// <param name="predefinedType">Optional predefined type.  Will be set if null.</param>
      /// <returns>True if it matches, false otherwise. </returns>
      /// <remarks>Currently only works with types that have "PredefinedType", not "ShapeType".</remarks>
      public bool IsAppropriatePredefinedType(IFCAnyHandle handle, string predefinedType)
      {
         if (handle == null)
            return false;
         if (PredefinedType == "")
            return true;

         if (string.IsNullOrEmpty(predefinedType))
         {
            try
            {
               predefinedType = IFCAnyHandleUtil.GetEnumerationAttribute(handle, "PredefinedType");
            }
            catch
            {
               return false;
            }
         }

         return (NamingUtil.IsEqualIgnoringCaseAndSpaces(PredefinedType, predefinedType));
      }

      /// <summary>
      /// The name of the property or quantity set.
      /// </summary>
      public string Name
      {
         get { return m_Name; }
         set
         {
            m_Name = value;

            // We will try to set the SubElementIndex based on the name of the PSet.  Only a few have entries.
            IFCCommonPSets psetName;
            if (Enum.TryParse<IFCCommonPSets>(m_Name, out psetName))
               SubElementIndex = (int)psetName;
            else
               SubElementIndex = -1;
         }
      }

      public string DescriptionOfSet
      {
         get { return m_Description; }
         set { m_Description = value; }
      }

      /// <summary>
      /// The element id of the ViewSchedule that generatd this description.
      /// </summary>
      public ElementId ViewScheduleId
      {
         get { return m_ViewScheduleId; }
         set { m_ViewScheduleId = value; }
      }

      /// <summary>
      /// The type of element appropriate for this property or quantity set.
      /// </summary>
      public HashSet<IFCEntityType> EntityTypes
      {
         get
         {
            return m_IFCEntityTypes;
         }
      }

      /// <summary>
      /// The object type of element appropriate for this property or quantity set.
      /// Primarily used for identifying proxies.
      /// </summary>
      /// <remarks>Currently limited to one entity type.</remarks>
      public string ObjectType
      {
         get
         {
            return m_ObjectType;
         }
         set
         {
            m_ObjectType = value;
         }
      }

      /// <summary>
      /// The pre-defined type of element appropriate for this property or quantity set.
      /// Primarily used for identifying sub-types of MEP objects.
      /// </summary>
      /// <remarks>Currently limited to one entity type.</remarks>
      public string PredefinedType
      {
         get
         {
            return m_PredefinedType;
         }
         set
         {
            m_PredefinedType = value;
         }
      }

      /// <summary>
      /// The index used to create a consistent GUID for this item.
      /// It is expected that this index will come from the list in IFCSubElementEnums.cs.
      /// </summary>
      public int SubElementIndex
      {
         get { return m_SubElementIndex; }
         set { m_SubElementIndex = value; }
      }

      /// <summary>
      /// The redirect calculator associated with this property or quantity set.
      /// </summary>
      public DescriptionCalculator DescriptionCalculator
      {
         get
         {
            return m_DescriptionCalculator;
         }
         set
         {
            m_DescriptionCalculator = value;
         }
      }
   }
}
