﻿//
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
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public class IFCFaceBound : IFCRepresentationItem
   {
      IFCLoop m_Bound = null;

      bool m_Orientation = true;

      bool m_IsOuter = false;

      /// <summary>
      /// Return the defining loop of the face boundary.
      /// </summary>
      public IFCLoop Bound
      {
         get { return m_Bound; }
         protected set { m_Bound = value; }
      }

      /// <summary>
      /// Return the orientation of the defining loop of the face boundary.
      /// </summary>
      public bool Orientation
      {
         get { return m_Orientation; }
         protected set { m_Orientation = value; }
      }

      /// <summary>
      /// Returns whether this is an outer boundary (TRUE) or an inner boundary (FALSE).
      /// </summary>
      public bool IsOuter
      {
         get { return m_IsOuter; }
         protected set { m_IsOuter = value; }
      }

      protected IFCFaceBound()
      {
      }

      override protected void Process(IFCAnyHandle ifcFaceBound)
      {
         base.Process(ifcFaceBound);

         IFCAnyHandle ifcLoop = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcFaceBound, "Bound", true);

         Bound = IFCLoop.ProcessIFCLoop(ifcLoop);

         IsOuter = (IFCAnyHandleUtil.IsSubTypeOf(ifcFaceBound, IFCEntityType.IfcFaceOuterBound));
      }

      private void CreateTessellatedShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform scaledLcs)
      {
         TessellatedShapeBuilderScope tsBuilderScope = shapeEditScope.BuilderScope as TessellatedShapeBuilderScope;

         if (tsBuilderScope == null)
         {
            throw new InvalidOperationException("Expect a TessellatedShapeBuilderScope, but get a BrepBuilderScope instead");
         }

         IList<XYZ> loopVertices = Bound.LoopVertices;
         int count = 0;
         if (loopVertices == null || ((count = loopVertices.Count) == 0))
            throw new InvalidOperationException("#" + Id + ": missing loop vertices, ignoring.");

         if (count < 3)
            throw new InvalidOperationException("#" + Id + ": too few loop vertices (" + count + "), ignoring.");

         if (!Orientation)
            loopVertices.Reverse();

         // Apply the transform
         IList<XYZ> transformedVertices = new List<XYZ>();
         foreach (XYZ vertex in loopVertices)
         {
            transformedVertices.Add(scaledLcs.OfPoint(vertex));
         }

         // Check that the loop vertices don't contain points that are very close to one another;
         // if so, throw the point away and hope that the TessellatedShapeBuilder can repair the result.
         // Warn in this case.  If the entire boundary is bad, report an error and don't add the loop vertices.

         IList<XYZ> validVertices;
         IFCGeometryUtil.CheckAnyDistanceVerticesWithinTolerance(Id, shapeEditScope, transformedVertices, out validVertices);

         // We are going to catch any exceptions if the loop is invalid.  
         // We are going to hope that we can heal the parent object in the TessellatedShapeBuilder.
         bool bPotentiallyAbortFace = false;

         count = validVertices.Count;
         if (count < 3)
         {
            Importer.TheLog.LogComment(Id, "Too few distinct loop vertices (" + count + "), ignoring.", false);
            bPotentiallyAbortFace = true;
         }
         else
         {
            // Last check: check to see if the vertices are actually planar.  If not, for the vertices to be planar.
            // We are not going to be particularly fancy about how we pick the plane.
            XYZ planeNormal = null;
            bool foundNormal = false;

            XYZ firstPoint = validVertices[0];
            XYZ secondPoint = validVertices[1];
            XYZ firstDir = secondPoint - firstPoint;

            int thirdPointIndex = 2;
            for (; thirdPointIndex < count; thirdPointIndex++)
            {
               XYZ thirdPoint = validVertices[thirdPointIndex];
               planeNormal = firstDir.CrossProduct(thirdPoint - firstPoint);
               if (!planeNormal.IsZeroLength())
               {
                  planeNormal = planeNormal.Normalize();
                  foundNormal = true;
                  break;
               }
            }

            if (!foundNormal)
            {
               Importer.TheLog.LogComment(Id, "Loop is degenerate, ignoring.", false);
               bPotentiallyAbortFace = true;
            }
            else
            {
               double vertexEps = IFCImportFile.TheFile.Document.Application.VertexTolerance;

               for (++thirdPointIndex; thirdPointIndex < count; thirdPointIndex++)
               {
                  XYZ pointOnPlane = validVertices[thirdPointIndex] -
                     (validVertices[thirdPointIndex] - firstPoint).DotProduct(planeNormal) * planeNormal;
                  if (pointOnPlane.DistanceTo(validVertices[thirdPointIndex]) > vertexEps)
                  {
                     Importer.TheLog.LogComment(Id, "Bounded loop plane is slightly non-planar, correcting.", false);
                     validVertices[thirdPointIndex] = pointOnPlane;
                  }
               }

               if (!tsBuilderScope.AddLoopVertices(Id, validVertices))
                  bPotentiallyAbortFace = true;
            }
         }

         if (bPotentiallyAbortFace && IsOuter)
            tsBuilderScope.AbortCurrentFace();
      }

      /// <summary>
      /// Create geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
      {
         if (shapeEditScope.BuilderScope == null)
         {
            throw new InvalidOperationException("BuilderScope has not been initialised");
         }
         base.CreateShapeInternal(shapeEditScope, lcs, scaledLcs, guid);
         Bound.CreateShape(shapeEditScope, lcs, scaledLcs, guid);
         IsValidForCreation = Bound.IsValidForCreation;

         if (shapeEditScope.BuilderType == IFCShapeBuilderType.TessellatedShapeBuilder)
            CreateTessellatedShapeInternal(shapeEditScope, scaledLcs);
      }

      protected IFCFaceBound(IFCAnyHandle ifcFaceBound)
      {
         Process(ifcFaceBound);
      }

      /// <summary>
      /// Create an IFCFaceBound object from a handle of type IfcFaceBound.
      /// </summary>
      /// <param name="ifcFaceBound">The IFC handle.</param>
      /// <returns>The IFCFaceBound object.</returns>
      public static IFCFaceBound ProcessIFCFaceBound(IFCAnyHandle ifcFaceBound)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcFaceBound))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcFaceBound);
            return null;
         }

         IFCEntity faceBound;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcFaceBound.StepId, out faceBound))
            faceBound = new IFCFaceBound(ifcFaceBound);
         return (faceBound as IFCFaceBound);
      }
   }
}