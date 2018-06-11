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
   /// The class contains wall connection information.
   /// </summary>
   public class WallConnectionData
   {
      /// <summary>
      /// The first connection element id.
      /// </summary>
      private ElementId m_FirstId;

      /// <summary>
      /// The second connection element id.
      /// </summary>
      private ElementId m_SecondId;

      /// <summary>
      /// The first connection type.
      /// </summary>
      private IFCConnectionType m_FirstConnectionType;

      /// <summary>
      /// The second connection type.
      /// </summary>
      private IFCConnectionType m_SecondConnectionType;

      /// <summary>
      /// The connection geometry handle.
      /// </summary>
      private IFCAnyHandle m_ConnectionGeometry;

      /// <summary>
      /// Registers a connection between a wall and another element.
      /// </summary>
      /// <param name="firstId">
      /// The first element id. This can be a wall or a column.
      /// </param>
      /// <param name="secondId">
      /// The second element id.  This can be a wall or a column.
      /// </param>
      /// <param name="firstConnectionType">
      /// The connection type for the first connected element.
      /// </param>
      /// <param name="secondConnectionType">
      /// The connection type for the second connected element.
      /// </param>
      /// <param name="connectionGeometry">
      /// The IfcConnectionGeometry handle related to the connection.  A valueless handle is also permitted.
      /// </param>
      public WallConnectionData(ElementId firstId, ElementId secondId, IFCConnectionType firstConnectionType,
         IFCConnectionType secondConnectionType, IFCAnyHandle connectionGeometry)
      {
         if (firstId < secondId)
         {
            this.m_FirstId = firstId;
            this.m_SecondId = secondId;
            this.m_FirstConnectionType = firstConnectionType;
            this.m_SecondConnectionType = secondConnectionType;
         }
         else
         {
            this.m_FirstId = secondId;
            this.m_SecondId = firstId;
            this.m_FirstConnectionType = secondConnectionType;
            this.m_SecondConnectionType = firstConnectionType;
         }

         this.m_ConnectionGeometry = connectionGeometry;
      }

      /// <summary>
      /// The first connection element id.
      /// </summary>
      public ElementId FirstId
      {
         get { return m_FirstId; }
      }

      /// <summary>
      /// The second connection element id.
      /// </summary>
      public ElementId SecondId
      {
         get { return m_SecondId; }
      }

      /// <summary>
      /// The first connection type.
      /// </summary>
      public IFCConnectionType FirstConnectionType
      {
         get { return m_FirstConnectionType; }
      }

      /// <summary>
      /// The second connection type.
      /// </summary>
      public IFCConnectionType SecondConnectionType
      {
         get { return m_SecondConnectionType; }
      }

      /// <summary>
      /// The connection geometry handle.
      /// </summary>
      public IFCAnyHandle ConnectionGeometry
      {
         get { return m_ConnectionGeometry; }
      }

      /// <summary>
      /// Override operator ==.
      /// </summary>
      /// <param name="first">
      /// The WallConnectionData.
      /// </param>
      /// <param name="second">
      /// The other WallConnectionData.
      /// </param>
      /// <returns>
      /// True if they are equal, false otherwise.
      /// </returns>
      static public bool operator ==(WallConnectionData first, WallConnectionData second)
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

         return first.m_FirstId == second.m_FirstId && first.m_SecondId == second.m_SecondId &&
            first.m_FirstConnectionType == second.m_FirstConnectionType && first.m_SecondConnectionType == second.m_SecondConnectionType;
      }

      /// <summary>
      /// Override operator !=.
      /// </summary>
      /// <param name="first">
      /// The WallConnectionData.
      /// </param>
      /// <param name="second">
      /// The other WallConnectionData.
      /// </param>
      /// <returns>
      /// True if they are not equal, false otherwise.
      /// </returns>
      static public bool operator !=(WallConnectionData first, WallConnectionData second)
      {
         return !(first == second);
      }

      /// <summary>
      /// Override method Equals.
      /// </summary>
      /// <param name="obj">
      /// The WallConnectionData object.
      /// </param>
      /// <returns>
      /// True if they are equal, false otherwise.
      /// </returns>
      public override bool Equals(object obj)
      {
         if (null == obj)
         {
            return false;
         }

         WallConnectionData wallConnectionData = obj as WallConnectionData;

         if (null == wallConnectionData)
         {
            return false;
         }

         return (this == wallConnectionData);
      }

      /// <summary>
      /// Override method GetHashCode.
      /// </summary>
      /// <returns>
      /// The hash code.
      /// </returns>
      public override int GetHashCode()
      {
         return m_FirstId.GetHashCode() ^ m_SecondId.GetHashCode();
      }
   }
}