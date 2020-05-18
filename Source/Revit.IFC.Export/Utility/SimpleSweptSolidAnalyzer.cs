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
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// This geometry utility allows you to attempt to “fit” a given piece of geometry into
   /// the shape of a simple swept solid.
   /// </summary>
   /// <remarks>
   /// It now only supports an open sweep with no opening or clippings and with one path curve of a line or arc.
   /// </remarks>
   class SimpleSweptSolidAnalyzer
   {
      PlanarFace m_ProfileFace;

      Curve m_PathCurve;

      XYZ m_ReferencePlaneNormal;

      List<Face> m_UnalignedFaces;

      /// <summary>
      /// The face that represents the profile of the swept solid.
      /// </summary>
      public PlanarFace ProfileFace
      {
         get { return m_ProfileFace; }
      }

      /// <summary>
      /// The edge that represents the path of the swept solid.
      /// </summary>
      public Curve PathCurve
      {
         get { return m_PathCurve; }
      }

      /// <summary>
      /// The normal of the reference plane that the path lies on.
      /// </summary>
      public XYZ ReferencePlaneNormal
      {
         get { return m_ReferencePlaneNormal; }
      }

      /// <summary>
      /// The unaligned faces, maybe openings or recesses.
      /// </summary>
      public List<Face> UnalignedFaces
      {
         get { return m_UnalignedFaces; }
      }

      /// <summary>
      /// Creates a SimpleSweptSolidAnalyzer and computes the swept solid.
      /// </summary>
      /// <param name="solid">The solid geometry.</param>
      /// <param name="normal">The normal of the reference plane that a path might lie on.  If it is null, try to guess based on the geometry.</param>
      /// <returns>The analyzer.</returns>
      public static SimpleSweptSolidAnalyzer Create(Solid solid, XYZ normal, GeometryObject potentialPathGeom = null)
      {
         if (solid == null)
            return null;

         ICollection<Face> faces = new List<Face>();
         foreach (Face face in solid.Faces)
         {
            faces.Add(face);
         }

         return Create(faces, normal, potentialPathGeom);
      }

      /// <summary>
      /// Creates a SimpleSweptSolidAnalyzer and computes the swept solid. This method should be used when a swept curve (directrix) is already known. Even when it is missing (null)
      /// it will simply call the original one where it will try to determine the swept curve (directrix) using the connecting faces
      /// </summary>
      /// <param name="faces">The faces of a solid.</param>
      /// <param name="normal">The normal of the reference plane that a path might lie on.  If it is null, try to guess based on the geometry.</param>
      /// <param name="potentialPathGeom">The potential swept path (e.g. in Revit MEP pipe/duct/fitting may already have this defined as part of the model)</param>
      /// <returns>The analyzer.</returns>
      /// <remarks>This is a simple analyzer, and is not intended to be general - it works in some simple, real-world cases.</remarks>
      public static SimpleSweptSolidAnalyzer Create(ICollection<Face> faces, XYZ normal, GeometryObject potentialPathGeom)
      {
         SimpleSweptSolidAnalyzer simpleSweptSolidAnalyzer = null;
         IList<Tuple<PlanarFace, XYZ>> potentialSweptAreaFaces = new List<Tuple<PlanarFace, XYZ>>();
         Curve directrix = potentialPathGeom as Curve;

         if (potentialPathGeom == null)
            return Create(faces, normal);

         XYZ directrixStartPt = directrix.GetEndPoint(0);
         XYZ directrixEndPt = directrix.GetEndPoint(1);

         // Collect planar faces as candidates for the swept area
         foreach (Face face in faces)
         {
            if (!(face is PlanarFace))
               continue;

            PlanarFace planarFace = face as PlanarFace;
            // Candidate face must be Orthogonal to the plane where the directrix curve is
            if (MathUtil.VectorsAreOrthogonal(normal, planarFace.FaceNormal))
            {
               // We are also interested to get only end faces where the Curve intersect the Face at the same point as the Curve start or end point
               IntersectionResultArray intersectResults;
               if (planarFace.Intersect(directrix, out intersectResults) == SetComparisonResult.Overlap)
               {
                  foreach (IntersectionResult res in intersectResults)
                  {
                     if (res.XYZPoint.IsAlmostEqualTo(directrixStartPt)
                         || res.XYZPoint.IsAlmostEqualTo(directrixEndPt))
                     {
                        Tuple<PlanarFace, XYZ> potentialEndFaceAndPoint = new Tuple<PlanarFace, XYZ>(planarFace, res.XYZPoint);
                        potentialSweptAreaFaces.Add(potentialEndFaceAndPoint);
                     }
                  }
               }
            }
         }

         // If there is more than 1 candidate, we need to find the congruent faces, 
         // and they cannot be on the same plane.
         PlanarFace sweptEndStartFace = null;

         while (potentialSweptAreaFaces.Count > 1 && (sweptEndStartFace == null))
         {
            PlanarFace face0 = potentialSweptAreaFaces[0].Item1;
            XYZ ptDirectrix = potentialSweptAreaFaces[0].Item2;
            potentialSweptAreaFaces.RemoveAt(0);    // remove the item from the List

            IList<Tuple<PlanarFace, XYZ>> potentialPairList = potentialSweptAreaFaces;

            foreach (Tuple<PlanarFace, XYZ> potentialPair in potentialPairList)
            {
               PlanarFace face1 = potentialPair.Item1;

               // Cannot handle faces that are on the same plane or intersecting (will cause self-intersection when being swept)
               // -- Can't do the intersection way because Revit returns intersection of the planes where those faces are defines (unbound)
               //if (face0.Intersect(face1) == FaceIntersectionFaceResult.Intersecting)
               //    continue;
               // If the faces are facing the same direction (or opposite) they may be of the same plane. Skip those of the same plane
               if (face0.FaceNormal.IsAlmostEqualTo(face1.FaceNormal) || face0.FaceNormal.IsAlmostEqualTo(face1.FaceNormal.Negate()))
               {
                  // chose any point in face0 and face1
                  XYZ pF0TopF1 = (face0.EdgeLoops.get_Item(0).get_Item(0).AsCurve().GetEndPoint(0)
                                  - face1.EdgeLoops.get_Item(0).get_Item(0).AsCurve().GetEndPoint(0)).Normalize();

                  if (pF0TopF1 == null || pF0TopF1.IsZeroLength())
                     continue;
                  // If the vector created from a point in Face0 and a point in Face1 against the face normal is orthogonal, it means the faces are on the same plane
                  if (MathUtil.VectorsAreOrthogonal(face0.FaceNormal, pF0TopF1))
                     continue;
               }

               if (AreFacesSimpleCongruent(face0, face1))
               {
                  if (ptDirectrix.IsAlmostEqualTo(directrixStartPt))
                     sweptEndStartFace = face0;
                  else
                     sweptEndStartFace = face1;
                  break;
               }
            }
         }

         if (sweptEndStartFace != null)
         {
            simpleSweptSolidAnalyzer = new SimpleSweptSolidAnalyzer();
            simpleSweptSolidAnalyzer.m_ProfileFace = sweptEndStartFace;
            simpleSweptSolidAnalyzer.m_PathCurve = directrix;
            simpleSweptSolidAnalyzer.m_ReferencePlaneNormal = normal;
         }

         return simpleSweptSolidAnalyzer;
      }

      /// <summary>
      /// Creates a SimpleSweptSolidAnalyzer and computes the swept solid.
      /// </summary>
      /// <param name="faces">The faces of a solid.</param>
      /// <param name="normal">The normal of the reference plane that a path might lie on.  If it is null, try to guess based on the geometry.</param>
      /// <returns>The analyzer.</returns>
      /// <remarks>This is a simple analyzer, and is not intended to be general - it works in some simple, real-world cases.</remarks>
      public static SimpleSweptSolidAnalyzer Create(ICollection<Face> faces, XYZ normal)
      {
         if (faces == null || faces.Count < 3)
         {
            // Invalid faces.
            return null;
         }

         if (normal == null)
         {
            foreach (Face face in faces)
            {
               if (face is RevolvedFace)
               {
                  XYZ faceNormal = (face as RevolvedFace).Axis;
                  if (normal == null)
                     normal = faceNormal;
                  else if (!MathUtil.VectorsAreParallel(normal, faceNormal))
                  {
                     // Couldn't calculate swept solid normal.
                     return null;
                  }
               }
            }
         }

         // find potential profile faces, their normal vectors must be orthogonal to the input normal
         List<PlanarFace> potentialSweepEndFaces = new List<PlanarFace>();
         foreach (Face face in faces)
         {
            PlanarFace planarFace = face as PlanarFace;
            if (planarFace == null)
               continue;
            if (MathUtil.VectorsAreOrthogonal(normal, planarFace.FaceNormal))
               potentialSweepEndFaces.Add(planarFace);
         }

         if (potentialSweepEndFaces.Count < 2)
         {
            // Can't find enough potential end faces.
            return null;
         }

         int ii = 0;
         PlanarFace candidateProfileFace = null; // the potential profile face for the swept solid
         PlanarFace candidateProfileFace2 = null;
         Edge candidatePathEdge = null;
         bool foundCandidateFace = false;
         do
         {
            candidateProfileFace = potentialSweepEndFaces[ii++];

            // find edges on the candidate profile face and the side faces with the edges
            // later find edges on the other candidate profile face with same side faces
            // they will be used to compare if the edges are congruent
            // to make sure the two faces are the potential profile faces

            Dictionary<Face, Edge> sideFacesWithCandidateEdges = new Dictionary<Face, Edge>();
            EdgeArrayArray candidateFaceEdgeLoops = candidateProfileFace.EdgeLoops;
            foreach (EdgeArray edgeArray in candidateFaceEdgeLoops)
            {
               foreach (Edge candidateEdge in edgeArray)
               {
                  Face sideFace = candidateEdge.GetFace(0);
                  if (sideFace == candidateProfileFace)
                     sideFace = candidateEdge.GetFace(1);

                  if (sideFacesWithCandidateEdges.ContainsKey(sideFace)) // should not happen
                     throw new InvalidOperationException("Failed");

                  sideFacesWithCandidateEdges[sideFace] = candidateEdge;
               }
            }

            double candidateProfileFaceArea = candidateProfileFace.Area;
            foreach (PlanarFace theOtherCandidateFace in potentialSweepEndFaces)
            {
               if (theOtherCandidateFace.Equals(candidateProfileFace))
                  continue;

               if (!MathUtil.IsAlmostEqual(candidateProfileFaceArea, theOtherCandidateFace.Area))
                  continue;

               EdgeArrayArray theOtherCandidateFaceEdgeLoops = theOtherCandidateFace.EdgeLoops;

               bool failToFindTheOtherCandidateFace = false;
               Dictionary<Face, Edge> sideFacesWithTheOtherCandidateEdges = new Dictionary<Face, Edge>();
               foreach (EdgeArray edgeArray in theOtherCandidateFaceEdgeLoops)
               {
                  foreach (Edge theOtherCandidateEdge in edgeArray)
                  {
                     Face sideFace = theOtherCandidateEdge.GetFace(0);
                     if (sideFace == theOtherCandidateFace)
                        sideFace = theOtherCandidateEdge.GetFace(1);

                     if (!sideFacesWithCandidateEdges.ContainsKey(sideFace)) // should already have
                     {
                        failToFindTheOtherCandidateFace = true;
                        break;
                     }

                     if (sideFacesWithTheOtherCandidateEdges.ContainsKey(sideFace)) // should not happen
                        throw new InvalidOperationException("Failed");

                     sideFacesWithTheOtherCandidateEdges[sideFace] = theOtherCandidateEdge;
                  }
               }

               if (failToFindTheOtherCandidateFace)
                  continue;

               if (sideFacesWithCandidateEdges.Count != sideFacesWithTheOtherCandidateEdges.Count)
                  continue;

               // side faces with candidate profile face edges
               Dictionary<Face, List<Edge>> sideFacesWithEdgesDic = new Dictionary<Face, List<Edge>>();
               foreach (Face sideFace in sideFacesWithCandidateEdges.Keys)
               {
                  sideFacesWithEdgesDic[sideFace] = new List<Edge>();
                  sideFacesWithEdgesDic[sideFace].Add(sideFacesWithCandidateEdges[sideFace]);
                  sideFacesWithEdgesDic[sideFace].Add(sideFacesWithTheOtherCandidateEdges[sideFace]);
               }

               if (!AreFacesSimpleCongruent(sideFacesWithEdgesDic))
                  continue;

               // find candidate path edges
               Dictionary<Face, List<Edge>> candidatePathEdgesWithFace = new Dictionary<Face, List<Edge>>();
               foreach (KeyValuePair<Face, List<Edge>> sideFaceAndEdges in sideFacesWithEdgesDic)
               {
                  List<Edge> pathEdges = FindCandidatePathEdge(sideFaceAndEdges.Key, sideFaceAndEdges.Value[0], sideFaceAndEdges.Value[1]);
                  // maybe we found two faces of an opening or a recess on the swept solid, skip in this case
                  if (pathEdges.Count < 2)
                  {
                     failToFindTheOtherCandidateFace = true;
                     break;
                  }
                  candidatePathEdgesWithFace[sideFaceAndEdges.Key] = pathEdges;
               }

               if (failToFindTheOtherCandidateFace)
                  continue;

               // check if these edges are congruent
               if (!AreEdgesSimpleCongruent(candidatePathEdgesWithFace))
                  continue;

               candidatePathEdge = candidatePathEdgesWithFace.Values.ElementAt(0).ElementAt(0);

               foundCandidateFace = true;
               candidateProfileFace2 = theOtherCandidateFace;
               break;
            }

            if (foundCandidateFace)
               break;
         } while (ii < potentialSweepEndFaces.Count);

         SimpleSweptSolidAnalyzer simpleSweptSolidAnalyzer = null;

         if (foundCandidateFace)
         {
            simpleSweptSolidAnalyzer = new SimpleSweptSolidAnalyzer();
            Curve pathCurve = candidatePathEdge.AsCurve();
            XYZ endPoint0 = pathCurve.GetEndPoint(0);

            bool foundProfileFace = false;
            List<PlanarFace> profileFaces = new List<PlanarFace>();
            profileFaces.Add(candidateProfileFace);
            profileFaces.Add(candidateProfileFace2);

            foreach (PlanarFace profileFace in profileFaces)
            {
               IntersectionResultArray intersectionResults;
               profileFace.Intersect(pathCurve, out intersectionResults);
               if (intersectionResults != null)
               {
                  foreach (IntersectionResult intersectionResult in intersectionResults)
                  {
                     XYZ intersectPoint = intersectionResult.XYZPoint;
                     if (intersectPoint.IsAlmostEqualTo(endPoint0))
                     {
                        simpleSweptSolidAnalyzer.m_ProfileFace = profileFace;
                        foundProfileFace = true;
                        break;
                     }
                  }
               }

               if (foundProfileFace)
                  break;
            }

            if (!foundProfileFace)
            {
               // Failed to find profile face.
               return null;
            }

            // TODO: consider one profile face has an opening extrusion inside while the other does not
            List<Face> alignedFaces = FindAlignedFaces(profileFaces.ToList<Face>());

            List<Face> unalignedFaces = new List<Face>();
            foreach (Face face in faces)
            {
               if (profileFaces.Contains(face) || alignedFaces.Contains(face))
                  continue;

               unalignedFaces.Add(face);
            }

            simpleSweptSolidAnalyzer.m_UnalignedFaces = unalignedFaces;

            simpleSweptSolidAnalyzer.m_PathCurve = pathCurve;
            simpleSweptSolidAnalyzer.m_ReferencePlaneNormal = normal;
         }

         return simpleSweptSolidAnalyzer;
      }

      /// <summary>
      /// Finds faces aligned with the swept solid.
      /// </summary>
      /// <param name="profileFaces">The profile faces.</param>
      /// <returns>The aligned faces.</returns>
      private static List<Face> FindAlignedFaces(ICollection<Face> profileFaces)
      {
         List<Face> alignedFaces = new List<Face>();
         foreach (Face face in profileFaces)
         {
            EdgeArrayArray edgeLoops = face.EdgeLoops;
            int edgeLoopCount = edgeLoops.Size;

            for (int ii = 0; ii < edgeLoopCount; ii++)
            {
               foreach (Edge edge in edgeLoops.get_Item(ii))
               {
                  Face alignedFace = edge.GetFace(0);
                  if (alignedFace == face)
                     alignedFace = edge.GetFace(1);
                  alignedFaces.Add(alignedFace);
               }
            }
         }
         return alignedFaces;
      }

      private static bool AreFacesSimpleCongruent(Face face0, Face face1)
      {
         if (!MathUtil.IsAlmostEqual(face0.Area, face1.Area))
            return false;

         BoundingBoxUV BB0 = face0.GetBoundingBox();
         BoundingBoxUV BB1 = face1.GetBoundingBox();
         if (!(face0.GetBoundingBox().Min.IsAlmostEqualTo(face0.GetBoundingBox().Min)
             && face0.GetBoundingBox().Max.IsAlmostEqualTo(face0.GetBoundingBox().Max)))
            return false;

         EdgeArrayArray EArrArr0 = face0.EdgeLoops;
         EdgeArrayArray EArrArr1 = face1.EdgeLoops;
         if (EArrArr0.Size != EArrArr1.Size)
            return false;

         // Collect all the edges in a simple list. To be congruent both list must be exactly the same (number of edges, edge type, edge properties)
         IList<Edge> simpleEdgeList0 = new List<Edge>();
         foreach (EdgeArray EArr in EArrArr0)
            foreach (Edge edge in EArr)
               simpleEdgeList0.Add(edge);

         IList<Edge> simpleEdgeList1 = new List<Edge>();
         foreach (EdgeArray EArr in EArrArr1)
            foreach (Edge edge in EArr)
               simpleEdgeList1.Add(edge);

         if (simpleEdgeList0.Count != simpleEdgeList1.Count)
            return false;

         for (int ii = 0; ii < simpleEdgeList0.Count; ++ii)
         {
            Curve curve0 = simpleEdgeList0[ii].AsCurve();
            Curve curve1 = simpleEdgeList1[ii].AsCurve();

            if (curve0 is Line)
            {
               if (!(curve1 is Line))
                  return false;
               if (!MathUtil.IsAlmostEqual(curve0.Length, curve1.Length))
                  return false;
               continue;
            }
            else if (curve0 is Arc)
            {
               if (!(curve1 is Arc))
                  return false;
               if (!MathUtil.IsAlmostEqual(curve0.Length, curve1.Length))
                  return false;
               if (!MathUtil.IsAlmostEqual((curve0 as Arc).Radius, (curve1 as Arc).Radius))
                  return false;
               continue;
            }

            // not support other types of curves for now
            return false;
         }

         return true;
      }

      /// <summary>
      /// Checks if two faces are congruent.
      /// </summary>
      /// <param name="faceEdgeDic">The collection contains edges on the candidate faces combined with same side faces.</param>
      /// <returns>True if congruent, false otherwise.</returns>
      private static bool AreFacesSimpleCongruent(Dictionary<Face, List<Edge>> faceEdgeDic)
      {
         if (faceEdgeDic == null || faceEdgeDic.Count == 0)
            return false;

         foreach (Face face in faceEdgeDic.Keys)
         {
            List<Edge> edges = faceEdgeDic[face];
            if (edges.Count != 2)
               return false;

            Curve curve0 = edges[0].AsCurve();
            Curve curve1 = edges[1].AsCurve();

            if (curve0 is Line)
            {
               if (!(curve1 is Line))
                  return false;
               if (!MathUtil.IsAlmostEqual(curve0.Length, curve1.Length))
                  return false;
               continue;
            }
            else if (curve0 is Arc)
            {
               if (!(curve1 is Arc))
                  return false;
               if (!MathUtil.IsAlmostEqual(curve0.Length, curve1.Length))
                  return false;
               if (!MathUtil.IsAlmostEqual((curve0 as Arc).Radius, (curve1 as Arc).Radius))
                  return false;
               continue;
            }

            // not support other types of curves for now
            return false;
         }

         return true;
      }

      /// <summary>
      /// Checks if edges are congruent.
      /// </summary>
      /// <param name="faceEdgeDic">The collection contains potential path edges on the side faces.</param>
      /// <returns>True if congruent, false otherwise.</returns>
      private static bool AreEdgesSimpleCongruent(Dictionary<Face, List<Edge>> faceEdgeDic)
      {
         if (faceEdgeDic == null || faceEdgeDic.Count == 0)
            return false;

         foreach (Face face in faceEdgeDic.Keys)
         {
            List<Edge> edges = faceEdgeDic[face];
            if (edges.Count != 2)
               return false;

            Curve curve0 = edges[0].AsCurveFollowingFace(face);
            Curve curve1 = edges[1].AsCurveFollowingFace(face);

            if (curve0 is Line)
            {
               if (!(curve1 is Line))
                  return false;
               XYZ moveDir = curve1.GetEndPoint(1) - curve0.GetEndPoint(0);
               Curve movedCurve = GeometryUtil.MoveCurve(curve0, moveDir);
               if (movedCurve.Intersect(curve1) != SetComparisonResult.Equal)
                  return false;
               continue;
            }
            else if (curve0 is Arc)
            {
               if (!(curve1 is Arc))
                  return false;
               Arc arc0 = curve0 as Arc;
               XYZ offsetVec = curve1.GetEndPoint(1) - curve0.GetEndPoint(0);
               Arc offsetArc = OffsetArc(arc0, curve0.GetEndPoint(0), offsetVec);
               if (offsetArc.Intersect(curve1) != SetComparisonResult.Equal)
                  return false;
               continue;
            }

            // not support other types of curves for now
            return false;
         }

         return true;
      }

      /// <summary>
      /// Offsets an arc along the offset direction from the point on the arc.
      /// </summary>
      /// <param name="arc">The arc.</param>
      /// <param name="offsetPntOnArc">The point on the arc.</param>
      /// <param name="offset">The offset vector.</param>
      /// <returns>The offset Arc.</returns>
      private static Arc OffsetArc(Arc arc, XYZ offsetPntOnArc, XYZ offset)
      {
         if (arc == null || offset == null)
            throw new ArgumentNullException();

         if (offset.IsZeroLength())
            return arc;

         XYZ axis = arc.Normal.Normalize();

         XYZ offsetAlongAxis = axis.Multiply(offset.DotProduct(axis));
         XYZ offsetOrthAxis = offset - offsetAlongAxis;
         XYZ offsetPntToCenter = (arc.Center - offsetPntOnArc).Normalize();

         double signedOffsetLengthTowardCenter = offsetOrthAxis.DotProduct(offsetPntToCenter);
         double newRadius = arc.Radius - signedOffsetLengthTowardCenter; // signedOffsetLengthTowardCenter > 0, minus, < 0, add

         Arc offsetArc = Arc.Create(arc.Center, newRadius, arc.GetEndParameter(0), arc.GetEndParameter(1), arc.XDirection, arc.YDirection);

         offsetArc = GeometryUtil.MoveCurve(offsetArc, offsetAlongAxis) as Arc;

         return offsetArc;
      }

      /// <summary>
      /// Finds candidate path edges from a side face with two edges on the profile face.
      /// </summary>
      /// <param name="face">The side face.</param>
      /// <param name="edge0">The edge on the profile face and the side face.</param>
      /// <param name="edge1">The edge on the profile face and the side face.</param>
      /// <returns>The potential path edges. Should at least have two path on one face</returns>
      private static List<Edge> FindCandidatePathEdge(Face face, Edge edge0, Edge edge1)
      {
         double vertexEps = ExporterCacheManager.Document.Application.VertexTolerance;

         Curve curve0 = edge0.AsCurveFollowingFace(face);
         Curve curve1 = edge1.AsCurveFollowingFace(face);

         XYZ[,] endPoints = new XYZ[2, 2] { { curve0.GetEndPoint(0), curve1.GetEndPoint(1) }, { curve0.GetEndPoint(1), curve1.GetEndPoint(0) } };

         List<Edge> candidatePathEdges = new List<Edge>();
         EdgeArray outerEdgeLoop = face.EdgeLoops.get_Item(0);
         foreach (Edge edge in outerEdgeLoop)
         {
            XYZ endPoint0 = edge.Evaluate(0);
            XYZ endPoint1 = edge.Evaluate(1);

            for (int ii = 0; ii < 2; ii++)
            {
               bool found = false;
               for (int jj = 0; jj < 2; jj++)
               {
                  if (endPoint0.IsAlmostEqualTo(endPoints[ii, jj], vertexEps))
                  {
                     int kk = 1 - jj;
                     if (endPoint1.IsAlmostEqualTo(endPoints[ii, kk], vertexEps))
                     {
                        candidatePathEdges.Add(edge);
                        found = true;
                        break;
                     }
                  }
               }
               if (found)
                  break;
            }
         }

         return candidatePathEdges;
      }
   }
}
