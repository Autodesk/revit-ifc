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
   /// The class contains information for creating IFC space boundary.
   /// </summary>
   public class SpaceBoundary
   {
      /// <summary>
      /// The IfcConnectionGeometry handle.
      /// </summary>
      private IFCAnyHandle m_ConnectionGeometry;

      /// <summary>
      /// The type of the space boundary.
      /// </summary>
      private IFCPhysicalOrVirtual m_Type = IFCPhysicalOrVirtual.NotDefined;

      /// <summary>
      /// Indicates if the space boundary is external or not.
      /// </summary>
      private IFCInternalOrExternal m_internalOrExternal = IFCInternalOrExternal.Internal;

      /// <summary>
      /// The identifier of the spatial element represented by this space boundary.
      /// </summary>
      private ElementId m_SpatialElementId = ElementId.InvalidElementId;

      /// <summary>
      /// The id of the element which forms the boundary.
      /// </summary>
      private ElementId m_BuildingElementId = ElementId.InvalidElementId;

      /// <summary>
      /// The id of the level.
      /// </summary>
      private ElementId m_LevelId = ElementId.InvalidElementId;

      /// <summary>
      /// Constructs a default SpaceBoundary object.
      /// </summary>
      public SpaceBoundary() { }

      /// <summary>
      /// Constructs a SpaceBoundary object.
      /// </summary>
      /// <param name="spatialElementId">
      /// The spatial element id.
      /// </param>
      /// <param name="buildingElementId">
      /// The building element id.
      /// </param>
      /// <param name="levelId">
      /// The level element id.
      /// </param>
      /// <param name="connectionGeometry">
      /// The connection geometry handle.
      /// </param>
      /// <param name="type">
      /// The type of the space boundary.
      /// </param>
      /// <param name="isExternal">
      /// Indicates if the space boundary is external or not.
      /// </param>
      public SpaceBoundary(ElementId spatialElementId, ElementId buildingElementId, ElementId levelId, IFCAnyHandle connectionGeometry, IFCPhysicalOrVirtual type, IFCInternalOrExternal internalOrExternal)
      {
         this.m_SpatialElementId = spatialElementId;
         this.m_BuildingElementId = buildingElementId;
         this.m_ConnectionGeometry = connectionGeometry;
         this.m_Type = type;
         this.m_internalOrExternal = internalOrExternal;
         this.m_LevelId = levelId;
      }

      /// <summary>
      /// The identifier of the spatial element represented by this space boundary.
      /// </summary>
      public ElementId SpatialElementId
      {
         get { return m_SpatialElementId; }
         set { m_SpatialElementId = value; }
      }

      /// <summary>
      /// The id of the element which forms the boundary.
      /// </summary>
      public ElementId BuildingElementId
      {
         get { return m_BuildingElementId; }
         set { m_BuildingElementId = value; }
      }

      /// <summary>
      /// The level id.
      /// </summary>
      public ElementId LevelId
      {
         get { return m_LevelId; }
         set { m_LevelId = value; }
      }

      /// <summary>
      /// The IfcConnectionGeometry handle.
      /// </summary>
      public IFCAnyHandle ConnectGeometryHandle
      {
         get { return m_ConnectionGeometry; }
         set { m_ConnectionGeometry = value; }
      }

      /// <summary>
      /// The type of the space boundary.
      /// </summary>
      public IFCPhysicalOrVirtual SpaceBoundaryType
      {
         get { return m_Type; }
         set { m_Type = value; }
      }

      /// <summary>
      /// Indicates if the space boundary is external or not.
      /// </summary>
      public IFCInternalOrExternal InternalOrExternal
      {
         get { return m_internalOrExternal; }
         set { m_internalOrExternal = value; }
      }
   }
}