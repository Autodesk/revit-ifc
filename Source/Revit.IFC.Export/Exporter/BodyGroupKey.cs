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
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// The class contains key information for GroupElementGeometryCache in ExportBody if UseGroupsIfPossible option is used.
   /// </summary>
   public class BodyGroupKey
   {
      /// <summary>
      /// The element id of the containing group type
      /// </summary>
      ElementId m_GroupTypeId = ElementId.InvalidElementId;

      /// <summary>
      /// The index in the group
      /// </summary>
      int m_GroupMemberIndex = -1;

      /// <summary>
      /// The element id of the containing group type
      /// </summary>
      public ElementId GroupTypeId
      {
         get { return m_GroupTypeId; }
         set { m_GroupTypeId = value; }
      }

      /// <summary>
      /// The element id of the containing group type
      /// </summary>
      public int GroupMemberIndex
      {
         get { return m_GroupMemberIndex; }
         set { m_GroupMemberIndex = value; }
      }

      static public bool operator ==(BodyGroupKey first, BodyGroupKey second)
      {
         Object lhsObject = first;
         Object rhsObject = second;
         if (null == lhsObject)
         {
            if (null == rhsObject)
               return true;
            return false;
         }
         if (null == rhsObject)
            return false;

         if (first.GroupMemberIndex != second.GroupMemberIndex)
            return false;

         if (first.GroupTypeId != second.GroupTypeId)
            return false;

         return true;
      }

      static public bool operator !=(BodyGroupKey first, BodyGroupKey second)
      {
         return !(first == second);
      }

      public override bool Equals(object obj)
      {
         if (obj == null)
            return false;

         BodyGroupKey second = obj as BodyGroupKey;
         return (this == second);
      }

      public override int GetHashCode()
      {
         return GroupMemberIndex.GetHashCode() ^ GroupTypeId.GetHashCode();
      }
   }
}