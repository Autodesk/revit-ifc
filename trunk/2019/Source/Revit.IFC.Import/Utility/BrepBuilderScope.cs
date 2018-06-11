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
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Data;
using Revit.IFC.Import.Geometry;
using UnitSystem = Autodesk.Revit.DB.DisplayUnit;
using UnitName = Autodesk.Revit.DB.DisplayUnitType;

namespace Revit.IFC.Import.Utility
{
   /// <summary>
   /// Provides methods to manage creation of geometry using BrepBuilder
   /// </summary>
   public class BrepBuilderScope : BuilderScope
   {
      private BRepBuilder m_BrepBuilder = null;

      private BRepBuilderGeometryId m_CurrentBrepBuilderFace = null;

      private BRepBuilderGeometryId m_CurrentBrepBuilderLoop = null;

      private Dictionary<int, BRepBuilderGeometryId> m_EdgeIdToBrepId = new Dictionary<int, BRepBuilderGeometryId>();

      /// <summary>
      /// The BrepBuilder that is used to build the IfcAdvancedBrep geometry
      /// </summary>
      public BRepBuilder BrepBuilder
      {
         get { return m_BrepBuilder; }
         protected set { m_BrepBuilder = value; }
      }

      public BrepBuilderScope(IFCImportShapeEditScope container)
         : base(container)
      {

      }

      /// <summary>
      /// Indicates if there exists an active face
      /// </summary>
      /// <returns>True if there exists an active face, false otherwise</returns>
      public bool HaveActiveFace()
      {
         return m_CurrentBrepBuilderFace != null;
      }

      /// <summary>
      /// Start collecting faces to create a BRep solid.
      /// </summary>
      /// <param name="brepType">The expected type of the geometry being built.</param>
      public void StartCollectingFaceSet(BRepType brepType)
      {
         BrepBuilder = new BRepBuilder(brepType);
         BrepBuilder.SetAllowShortEdges();
      }

      /// <summary>
      /// Add a new face to the BrepBuilder
      /// </summary>
      /// <param name="surface">The surface that is used to construct the brepbuilder face</param>
      /// <param name="orientation">The flag that indicates (TRUE) if the surface's normal agree with the face's normal or not (FALSE)</param>
      /// <param name="materialId">The face's material ID</param>
      /// <param name="localTransform">The local transform</param>
      public void StartCollectingFace(IFCSurface surface, Transform localTransform, bool orientation, ElementId materialId)
      {
         if (m_CurrentBrepBuilderFace != null)
         {
            throw new InvalidOperationException("StopCollectingFaceForBrepBuilder for previous face hasn't been called yet");
         }

         bool bReversed = !orientation;
         BRepBuilderSurfaceGeometry surfaceGeometry;

         if (surface is IFCBSplineSurfaceWithKnots)
         {
            surfaceGeometry = StartCollectingNURBSFace(surface as IFCBSplineSurfaceWithKnots, localTransform);
         }
         else
         {
            Surface transformedSurface = surface.GetSurface(localTransform);
            if (transformedSurface == null)
               throw new InvalidOperationException("Couldn't create surface for the current face.");
            surfaceGeometry = BRepBuilderSurfaceGeometry.Create(transformedSurface, null);
         }
         m_CurrentBrepBuilderFace = m_BrepBuilder.AddFace(surfaceGeometry, bReversed);
         FaceMaterialId = materialId;
         BrepBuilder.SetFaceMaterialId(m_CurrentBrepBuilderFace, FaceMaterialId);
      }

      private BRepBuilderSurfaceGeometry StartCollectingNURBSFace(IFCBSplineSurfaceWithKnots bSplineSurfaceWithKnots, Transform localTransform)
      {
         if (bSplineSurfaceWithKnots == null)
            return null;

         IFCRationalBSplineSurfaceWithKnots rationalBSplineSurfaceWithKnots = (bSplineSurfaceWithKnots as IFCRationalBSplineSurfaceWithKnots);

         IList<double> knotsU = IFCGeometryUtil.ConvertIFCKnotsToRevitKnots(bSplineSurfaceWithKnots.UMultiplicities, bSplineSurfaceWithKnots.UKnots);
         if (knotsU == null || knotsU.Count == 0)
         {
            throw new InvalidOperationException("No knots in u-direction");
         }

         IList<double> knotsV = IFCGeometryUtil.ConvertIFCKnotsToRevitKnots(bSplineSurfaceWithKnots.VMultiplicities, bSplineSurfaceWithKnots.VKnots);
         if (knotsV == null || knotsV.Count == 0)
         {
            throw new InvalidOperationException("No knots in v-direction");
         }

         IList<double> weights = (rationalBSplineSurfaceWithKnots != null) ? rationalBSplineSurfaceWithKnots.WeightsList : null;

         IList<XYZ> controlPoints = new List<XYZ>();
         foreach (XYZ point in bSplineSurfaceWithKnots.ControlPointsList)
         {
            controlPoints.Add(localTransform.OfPoint(point));
         }

         int uDegree = bSplineSurfaceWithKnots.UDegree;
         int vDegree = bSplineSurfaceWithKnots.VDegree;

         BRepBuilderSurfaceGeometry surfaceGeometry = null;
         if (weights == null)
            surfaceGeometry = BRepBuilderSurfaceGeometry.CreateNURBSSurface(uDegree, vDegree, knotsU, knotsV, controlPoints, false, null);
         else
            surfaceGeometry = BRepBuilderSurfaceGeometry.CreateNURBSSurface(uDegree, vDegree, knotsU, knotsV, controlPoints, weights, false, null);

         return surfaceGeometry;
      }

      private void StopCollectingFaceInternal(bool isValid)
      {
         if (m_BrepBuilder == null)
            throw new InvalidOperationException("StartCollectingFaceSet has not been called");

         if (isValid)
         {
            if (m_CurrentBrepBuilderFace == null)
               throw new InvalidOperationException("StartCollectingFaceForBrepBuilder hasn't been called yet");

            m_BrepBuilder.FinishFace(m_CurrentBrepBuilderFace);
         }

         m_CurrentBrepBuilderFace = null;
      }

      /// <summary>
      /// Nullify the current face after we finish processing it
      /// </summary>
      /// <param name="isValid">We are finishing a valid face if true.</param>
      public void StopCollectingFace(bool isValid)
      {
         StopCollectingFaceInternal(isValid);
      }

      /// <summary>
      /// Remove the current invalid face from the list of faces to create a BRep solid.
      /// </summary>
      override public void AbortCurrentFace()
      {
         StopCollectingFaceInternal(false);
      }

      /// <summary>
      /// Initialize a new loop for the current face
      /// </summary>
      public void InitializeNewLoop()
      {
         if (m_BrepBuilder == null)
         {
            throw new InvalidOperationException("StartCollectingFaceSet has not been called");
         }
         if (m_CurrentBrepBuilderFace == null)
         {
            throw new InvalidOperationException("StartCollectingFaceForBrepBuilder has not been called");
         }

         if (m_CurrentBrepBuilderLoop != null)
         {
            // We could allow several active faces, with each face having a loop being actively constructed, but the current
            // design, with a single m_CurrentBrepBuilderFace and a single m_CurrentBrepBuilderLoop, apparently imposes the
            // stricter requirement that faces are constructed one at a time, and for each face, its loops are constructed
            // one at a time.
            throw new InvalidOperationException("InitializeNewLoop has already been called - only one loop may be active at a time.");
         }

         m_CurrentBrepBuilderLoop = m_BrepBuilder.AddLoop(m_CurrentBrepBuilderFace);
      }

      /// <summary>
      /// Finish constructing a loop for the current face.
      /// </summary>
      /// <param name="isValid">We are finishing a valid loop if true.</param>
      public void StopConstructingLoop(bool isValid)
      {
         if (m_BrepBuilder == null)
         {
            throw new InvalidOperationException("StartCollectingFaceSet has not been called");
         }
         if (m_CurrentBrepBuilderFace == null)
         {
            throw new InvalidOperationException("StartCollectingFaceForBrepBuilder has not been called");
         }

         if (m_CurrentBrepBuilderLoop == null)
         {
            throw new InvalidOperationException("InitializeNewLoop has not been called");
         }

         if (isValid)
            m_BrepBuilder.FinishLoop(m_CurrentBrepBuilderLoop);

         m_CurrentBrepBuilderLoop = null;
      }

      /// <summary>
      /// Add the oriented edge to the current loop
      /// </summary>
      /// <param name="id">the id of the edge, corresponding to the StepID of the IfcOrientedEdge</param>
      /// <param name="curve">the curve, which represents the geometry of the edge</param>
      /// <param name="startPoint">the start point of the curve</param>
      /// <param name="endPoint">the end point of the curve</param>
      /// <param name="orientation">the orientation of the edge</param>
      /// <returns>true if the edge is successfully added to the boundary</returns>
      public bool AddOrientedEdgeToTheBoundary(int id, Curve curve, XYZ startPoint, XYZ endPoint, bool orientation)
      {
         if (m_CurrentBrepBuilderLoop == null)
            throw new InvalidOperationException("StartCollectingLoopForBrepBuilder hasn't been called");

         BRepBuilderGeometryId edgeId = null;

         if (m_EdgeIdToBrepId.ContainsKey(id) && m_EdgeIdToBrepId[id] != null)
         {
            edgeId = m_EdgeIdToBrepId[id];
         }
         else
         {
            //TODO: create an utility function MakeBound(Curve, XYZ, XYZ) and factor out this code
            BRepBuilderEdgeGeometry edge = null;
            if (curve is Line)
            {
               edge = BRepBuilderEdgeGeometry.Create(startPoint, endPoint);
            }
            else if (curve is Arc)
            {
               Arc arc = curve as Arc;

               // The curve we receive is an unbound arc, so we have to bound it by the startPoint and the endPoint
               IntersectionResult start = arc.Project(startPoint);
               IntersectionResult end = arc.Project(endPoint);

               double startParameter = start.Parameter;
               double endParameter = end.Parameter;

               if (endParameter < startParameter)
                  endParameter += Math.PI * 2;

               arc.MakeBound(startParameter, endParameter);

               edge = BRepBuilderEdgeGeometry.Create(arc);
            }
            else if (curve is Ellipse)
            {
               Ellipse ellipse = curve as Ellipse;

               IntersectionResult start = ellipse.Project(startPoint);
               IntersectionResult end = ellipse.Project(endPoint);

               double startParameter = start.Parameter;
               double endParameter = end.Parameter;

               if (endParameter < startParameter)
                  endParameter += Math.PI * 2;

               ellipse.MakeBound(startParameter, endParameter);
               edge = BRepBuilderEdgeGeometry.Create(ellipse);
            }
            else if (curve is NurbSpline)
            {
               NurbSpline nurbs = curve as NurbSpline;

               // Bound the NurbSpline based on the start and end points.
               // As mentioned above, there should be a function to bound
               // a curve based on two 3D points and it should be used here
               // instead of duplicating manual code.
               IntersectionResult start = nurbs.Project(startPoint);
               IntersectionResult end = nurbs.Project(endPoint);
               double startParameter = start.Parameter;
               double endParameter = end.Parameter;
               if (endParameter < startParameter)
               {
                  Importer.TheLog.LogError(id, "Inverted start/end parameters for NurbSpline.", false/*throwError*/);
                  return false;
               }
               else
               {
                  nurbs.MakeBound(startParameter, endParameter);
               }

               edge = BRepBuilderEdgeGeometry.Create(nurbs);
            }
            else
            {
               Importer.TheLog.LogError(id, "Unsupported edge curve type: " + curve.GetType().ToString(), false);
               return false;
            }

            edgeId = m_BrepBuilder.AddEdge(edge);
            m_EdgeIdToBrepId.Add(id, edgeId);
         }
         try
         {
            m_BrepBuilder.AddCoEdge(m_CurrentBrepBuilderLoop, edgeId, !orientation);
         }
         catch
         {
            return false;
         }
         return true;
      }

      /// <summary>
      /// Create geometry with the BrepBuilder based on already existing settings.
      /// </summary>
      /// <param name="guid">The Guid associated with the geometry.</param>
      /// <returns>A list of GeometryObjects, possibly empty.</returns>
      public IList<GeometryObject> CreateGeometry()
      {
         BRepBuilderOutcome outcome = BRepBuilderOutcome.Failure;
         try
         {
            outcome = m_BrepBuilder.Finish();
         }
         catch
         {
            outcome = BRepBuilderOutcome.Failure;
         }

         if (outcome != BRepBuilderOutcome.Success)
            return null;

         IList<GeometryObject> geomObjects = new List<GeometryObject>();
         geomObjects.Add(m_BrepBuilder.GetResult());

         return geomObjects;
      }
   }
}