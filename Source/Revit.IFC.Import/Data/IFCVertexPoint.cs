//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
// Copyright (C) 2013  Autodesk, Inc.
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
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Class that represents an IFCVertex entity
   /// </summary>
   public class IFCVertexPoint : IFCVertex
   {
      /// <summary>
      /// The geometric point, which defines the position in geometric space of the vertex
      /// </summary>
      public XYZ VertexGeometry { get; set; } = null;

      protected IFCVertexPoint()
      {
      }

      protected IFCVertexPoint(IFCAnyHandle item)
      {
         Process(item);
      }

      protected override void Process(IFCAnyHandle ifcVertexPoint)
      {
         base.Process(ifcVertexPoint);

         IFCAnyHandle vertexGeometry = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcVertexPoint, "VertexGeometry", true);
         XYZ unScaledVertexGeometry = IFCPoint.IFCPointToXYZ(vertexGeometry);
         VertexGeometry = IFCUnitUtil.ScaleLength(unScaledVertexGeometry);
      }

      /// <summary>
      /// Create an IFCVertexPoint object from a handle of type IfcVertexPoint.
      /// </summary>
      /// <param name="ifcVertexPoint">The IFC handle.</param>
      /// <returns>The IFCVertexPoint object.</returns>
      public static IFCVertex ProcessIFCVertexPoint(IFCAnyHandle ifcVertexPoint)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcVertexPoint))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcVertexPoint);
            return null;
         }

         IFCEntity vertexPoint;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcVertexPoint.StepId, out vertexPoint))
            vertexPoint = new IFCVertexPoint(ifcVertexPoint);
         return (vertexPoint as IFCVertexPoint);
      }

      /// <summary>
      /// Returns the coordinate of this vertex
      /// </summary>
      public override XYZ GetCoordinate()
      {
         return VertexGeometry;
      }
   }
}