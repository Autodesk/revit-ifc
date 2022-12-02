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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Class that represents IfcEdgeLoop entity
   /// </summary>
   public class IFCEdgeLoop : IFCLoop
   {
      IList<IFCOrientedEdge> m_EdgeList = null;

      /// <summary>
      /// A list of oriented edge entities which are concatenated together to form this path.
      /// </summary>
      public IList<IFCOrientedEdge> EdgeList
      {
         get
         {
            if (m_EdgeList == null)
            {
               m_EdgeList = new List<IFCOrientedEdge>();
            }
            return m_EdgeList;
         }
         set { m_EdgeList = value; }
      }

      protected IFCEdgeLoop()
      {
      }

      protected IFCEdgeLoop(IFCAnyHandle ifcEdgeLoop)
      {
         Process(ifcEdgeLoop);
      }

      override protected void Process(IFCAnyHandle ifcEdgeLoop)
      {
         base.Process(ifcEdgeLoop);

         // TODO in REVIT-61368: checks that edgeList is closed and continuous
         IList<IFCAnyHandle> edgeList = IFCAnyHandleUtil.GetValidAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcEdgeLoop, "EdgeList");
         if (edgeList == null)
         {
            Importer.TheLog.LogError(Id, "Cannot find the EdgeList of this loop", true);
         }
         IFCOrientedEdge orientedEdge = null;
         foreach (IFCAnyHandle edge in edgeList)
         {
            orientedEdge = IFCOrientedEdge.ProcessIFCOrientedEdge(edge);
            EdgeList.Add(orientedEdge);
         }
      }

      protected override CurveLoop GenerateLoop()
      {
         CurveLoop curveLoop = new CurveLoop();
         foreach (IFCOrientedEdge edge in EdgeList)
         {
            if (edge != null)
               curveLoop.Append(edge.GetGeometry());
         }
         return curveLoop;
      }

      protected override IList<XYZ> GenerateLoopVertices()
      {
         return null;
      }

      /// <summary>
      /// Create an IFCEdgeLoop object from a handle of type IfcEdgeLoop.
      /// </summary>
      /// <param name="ifcEdgeLoop">The IFC handle.</param>
      /// <returns>The IFCEdgeLoop object.</returns>
      public static IFCEdgeLoop ProcessIFCEdgeLoop(IFCAnyHandle ifcEdgeLoop)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcEdgeLoop))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcFace);
            return null;
         }

         IFCEntity edgeLoop;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcEdgeLoop.StepId, out edgeLoop))
            edgeLoop = new IFCEdgeLoop(ifcEdgeLoop);
         return (edgeLoop as IFCEdgeLoop);
      }

      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, 
         Transform scaledLcs, string guid)
      {
         if (shapeEditScope.BuilderScope == null)
         {
            throw new InvalidOperationException("BuilderScope hasn't been initialized yet");
         }

         BrepBuilderScope brepBuilderScope = null;
         TessellatedShapeBuilderScope tsbScope = null;

         if (shapeEditScope.BuilderType == IFCShapeBuilderType.BrepBuilder)
            brepBuilderScope = shapeEditScope.BuilderScope as BrepBuilderScope;
         else if (shapeEditScope.BuilderType == IFCShapeBuilderType.TessellatedShapeBuilder)
            tsbScope = shapeEditScope.BuilderScope as TessellatedShapeBuilderScope;

         if (brepBuilderScope == null && tsbScope == null)
            throw new InvalidOperationException("The wrong BuilderScope is created");

         List<XYZ> vertices = new List<XYZ>();

         foreach (IFCOrientedEdge edge in EdgeList)
         {
            if (edge == null || edge.EdgeStart == null || edge.EdgeEnd == null)
            {
               Importer.TheLog.LogError(Id, "Invalid edge loop", true);
               return;
            }

            edge.CreateShape(shapeEditScope, scaledLcs, guid);

            IFCEdge edgeElement = edge.EdgeElement;
            Curve edgeGeometry = (edgeElement is IFCEdgeCurve) ? edgeElement.GetGeometry() : null;

            if (edgeGeometry == null)
               Importer.TheLog.LogError(edgeElement.Id, "Cannot get the edge geometry of this edge", true);

            XYZ edgeStart = edgeElement.EdgeStart.GetCoordinate();
            XYZ edgeEnd = edgeElement.EdgeEnd.GetCoordinate();

            if (edgeStart == null || edgeEnd == null)
               Importer.TheLog.LogError(Id, "Invalid start or end vertices", true);

            bool orientation = scaledLcs.HasReflection ? !edge.Orientation : edge.Orientation;

            XYZ transformedEdgeStart = scaledLcs.OfPoint(edgeStart);
            XYZ transformedEdgeEnd = scaledLcs.OfPoint(edgeEnd);
            Curve transformedEdgeGeometry = edgeGeometry.CreateTransformed(scaledLcs);

            if (brepBuilderScope != null)
            {
               if (!brepBuilderScope.AddOrientedEdgeToTheBoundary(edgeElement.Id,
                  transformedEdgeGeometry, transformedEdgeStart, transformedEdgeEnd,
                  orientation))
               {
                  Importer.TheLog.LogWarning(edge.Id, "Cannot add this edge to the edge loop with Id: " + Id, false);
                  IsValidForCreation = false;
                  return;
               }
            }
            else
            {
               bool firstEdge = (vertices.Count == 0);
               if (edgeGeometry is Line)
               {
                  if (firstEdge)
                     vertices.Add(orientation ? transformedEdgeStart : transformedEdgeEnd);
                  vertices.Add(orientation ? transformedEdgeEnd : transformedEdgeStart);
               }
               else
               {
                  IList<XYZ> newPoints = transformedEdgeGeometry.Tessellate();
                  vertices.AddRange(firstEdge ? newPoints : newPoints.Skip(1));
               }
            }
         }
 
         if (vertices.Count > 0)
            tsbScope.AddLoopVertices(Id, vertices);
        
         base.CreateShapeInternal(shapeEditScope, scaledLcs, guid);
      }
   }
}