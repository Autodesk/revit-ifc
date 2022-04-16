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
      /// The optional unique name of this space boundary.
      /// </summary>
      public string Name { get; private set; } = null;

      /// <summary>
      /// The identifier of the spatial element represented by this space boundary.
      /// </summary>
      public ElementId SpatialElementId { get; private set; } = ElementId.InvalidElementId;

      /// <summary>
      /// The id of the element which forms the boundary.
      /// </summary>
      public ElementId BuildingElementId { get; private set; } = ElementId.InvalidElementId;

      /// <summary>
      /// The level id.
      /// </summary>
      public ElementId LevelId { get; private set; } = ElementId.InvalidElementId;

      /// <summary>
      /// The IfcConnectionGeometry handle.
      /// </summary>
      public IFCAnyHandle ConnectionGeometryHandle { get; private set; } = null;

      /// <summary>
      /// The type of the space boundary.
      /// </summary>
      public IFCPhysicalOrVirtual SpaceBoundaryType { get; private set; } = IFCPhysicalOrVirtual.NotDefined;

      /// <summary>
      /// Indicates if the space boundary is external or not.
      /// </summary>
      public IFCInternalOrExternal InternalOrExternal { get; private set; } = IFCInternalOrExternal.Internal;

      /// <summary>
      /// Constructs a default SpaceBoundary object.
      /// </summary>
      public SpaceBoundary() { }

      /// <summary>
      /// Constructs a SpaceBoundary object.
      /// </summary>
      /// <param name="name">The optional name of the space boundary.</param>
      /// <param name="spatialElementId">The spatial element id.</param>
      /// <param name="buildingElementId">The building element id.</param>
      /// <param name="levelId">The level element id.</param>
      /// <param name="connectionGeometry">The connection geometry handle.</param>
      /// <param name="type">The type of the space boundary.</param>
      /// <param name="isExternal">Indicates if the space boundary is external or not.</param>
      public SpaceBoundary(string name, ElementId spatialElementId, ElementId buildingElementId,
         ElementId levelId, IFCAnyHandle connectionGeometry, IFCPhysicalOrVirtual type,
         IFCInternalOrExternal internalOrExternal)
      {
         Name = name;
         SpatialElementId = spatialElementId;
         BuildingElementId = buildingElementId;
         ConnectionGeometryHandle = connectionGeometry;
         SpaceBoundaryType = type;
         InternalOrExternal = internalOrExternal;
         LevelId = levelId;
      }
   }
}