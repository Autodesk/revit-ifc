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
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Class that represents IfcEdge entity
   /// </summary>
   public class IFCEdge : IFCTopologicalRepresentationItem
   {
      private IFCVertex m_EdgeStart;

      private IFCVertex m_EdgeEnd;

      /// <summary>
      /// Start point of the edge
      /// </summary>
      public IFCVertex EdgeStart
      {
         get { return m_EdgeStart; }
         set { m_EdgeStart = value; }
      }

      /// <summary>
      /// End point of the edge
      /// </summary>
      public IFCVertex EdgeEnd
      {
         get { return m_EdgeEnd; }
         set { m_EdgeEnd = value; }
      }

      protected IFCEdge()
      {
      }

      protected IFCEdge(IFCAnyHandle item)
      {
         Process(item);
      }

      protected override void Process(IFCAnyHandle ifcEdge)
      {
         base.Process(ifcEdge);

         IFCAnyHandle edgeStart = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcEdge, "EdgeStart", false);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(edgeStart))
         {
            Importer.TheLog.LogError(ifcEdge.StepId, "Cannot find the starting vertex", true);
            return;
         }

         IFCAnyHandle edgeEnd = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcEdge, "EdgeEnd", false);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(edgeEnd))
         {
            Importer.TheLog.LogError(ifcEdge.StepId, "Cannot find the ending vertex", true);
            return;
         }

         EdgeStart = IFCVertex.ProcessIFCVertex(edgeStart);
         EdgeEnd = IFCVertex.ProcessIFCVertex(edgeEnd);
      }

      /// <summary>
      /// Returns the curve which defines the shape and spatial location of this edge.
      /// </summary>
      /// <returns>The curve which defines the shape and spatial location of this edge.</returns>
      public virtual Curve GetGeometry()
      {
         if (EdgeStart == null || EdgeEnd == null)
         {
            Importer.TheLog.LogError(Id, "Invalid edge", true);
            return null;
         }
         return Line.CreateBound(EdgeStart.GetCoordinate(), EdgeEnd.GetCoordinate());
      }

      /// <summary>
      /// Create an IFCEdge object from a handle of type IfcEdge
      /// </summary>
      /// <param name="ifcEdge">The IFC handle</param>
      /// <returns>The IfcEdge object</returns>
      public static IFCEdge ProcessIFCEdge(IFCAnyHandle ifcEdge)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcEdge))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcEdge);
            return null;
         }

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcEdge, IFCEntityType.IfcOrientedEdge))
            return IFCOrientedEdge.ProcessIFCOrientedEdge(ifcEdge);
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcEdge, IFCEntityType.IfcEdgeCurve))
            return IFCEdgeCurve.ProcessIFCEdgeCurve(ifcEdge);

         IFCEntity edge;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcEdge.StepId, out edge))
            edge = new IFCEdge(ifcEdge);
         return (edge as IFCEdge);
      }
   }
}