//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012-2016  Autodesk, Inc.
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
using Autodesk.Revit.DB;
using System.Diagnostics;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Private class to creates a face based on the vertex indices
   /// </summary>
   class IndexFace
   {
      /// <summary>
      /// Vertex indices for the outer boundary of the face
      /// </summary>
      public IList<int> IndexOuterBoundary { get; set; }
      /// <summary>
      /// List of vertex indices for the inner boundaries
      /// </summary>
      public IList<IList<int>> IndexedInnerBoundaries { get; set; }
      /// <summary>
      /// Collection of all the vertices (outer and inner)
      /// </summary>
      public IDictionary<int, IndexSegment> OuterAndInnerBoundaries { get; set; } = new Dictionary<int, IndexSegment>();
      IDictionary<IndexSegment, int> BoundaryLinesDict;
      /// <summary>
      /// The normal vector of the face
      /// </summary>
      public XYZ Normal { get; set; }

      /// <summary>
      /// Constructor taking a list of vertex indices (face without hole) 
      /// </summary>
      /// <param name="vertxIndices">the list of vertex indices (face without hole)</param>
      public IndexFace(IList<int> vertxIndices, ref IDictionary<int, XYZ> meshVertices)
      {
         IndexOuterBoundary = vertxIndices;
         SetupEdges(IndexOuterBoundary, 0);

         IList<XYZ> vertices = new List<XYZ>();
         foreach (int idx in IndexOuterBoundary)
         {
            vertices.Add(meshVertices[idx]);
         }
         Normal = TriangleMergeUtil.NormalByNewellMethod(vertices);
      }

      /// <summary>
      /// Constructor taking in List of List of vertices. The first list will be the outer boundary and the rest are the inner boundaries
      /// </summary>
      /// <param name="vertxIndices">List of List of vertices. The first list will be the outer boundary and the rest are the inner boundaries</param>
      public IndexFace(IList<IList<int>> vertxIndices, ref IDictionary<int, XYZ> meshVertices)
      {
         if (vertxIndices == null)
            return;
         if (vertxIndices.Count == 0)
            return;

         IndexOuterBoundary = vertxIndices[0];
         vertxIndices.RemoveAt(0);
         SetupEdges(IndexOuterBoundary, 0);

         if (vertxIndices.Count != 0)
         {
            IndexedInnerBoundaries = vertxIndices;

            foreach (IList<int> innerBound in IndexedInnerBoundaries)
            {
               int idxOffset = OuterAndInnerBoundaries.Count;
               SetupEdges(innerBound, idxOffset);
            }
         }

         // Create normal from only the outer boundary
         IList<XYZ> vertices = new List<XYZ>();
         foreach (int idx in IndexOuterBoundary)
         {
            vertices.Add(meshVertices[idx]);
         }
         Normal = TriangleMergeUtil.NormalByNewellMethod(vertices);
      }

      /// <summary>
      /// Reverse the order of the vertices. Only operates on the outer boundary 
      /// </summary>
      public void Reverse()
      {
         // This is used in the process of combining triangles and therefore will work only with the face without holes
         List<int> revIdxOuter = IndexOuterBoundary.ToList();
         revIdxOuter.Reverse();
         OuterAndInnerBoundaries.Clear();
         BoundaryLinesDict.Clear();
         SetupEdges(revIdxOuter, 0);
      }

      /// <summary>
      /// Find matched line segment in the face boundaries
      /// </summary>
      /// <param name="inpSeg">Input line segment as vertex indices</param>
      /// <returns>Return index of the matched segment</returns>
      public int FindMatchedIndexSegment(IndexSegment inpSeg)
      {
         int idx;
         if (BoundaryLinesDict.TryGetValue(inpSeg, out idx))
            return idx;
         else
            return -1;
      }

      void SetupEdges(IList<int> vertxIndices, int idxOffset)
      {
         int boundLinesDictOffset = 0;

         if (BoundaryLinesDict == null)
         {
            IEqualityComparer<IndexSegment> segCompare = new SegmentComparer(false/*compareBothDirections*/);
            BoundaryLinesDict = new Dictionary<IndexSegment, int>(segCompare);
         }
         else
            boundLinesDictOffset = BoundaryLinesDict.Count();

         int prevIdx = 0;
         int idx = 0;
         int vertCount = vertxIndices.Count;
         foreach (int vIdx in vertxIndices)
         {
            IndexSegment segm = null;
            if (idx > 0)
            {
               segm = new IndexSegment(prevIdx, vIdx);
               OuterAndInnerBoundaries.Add(idx - 1 + idxOffset, segm);
               BoundaryLinesDict.Add(segm, (idx - 1 + boundLinesDictOffset));       // boundaryLinesDict is a dictionary for the combined outer and inner boundaries, the values should be sequential
            }
            if (idx == vertCount - 1)  // The last index. Close the loop by connecing it to the first index
            {
               segm = new IndexSegment(vIdx, vertxIndices[0]);
               OuterAndInnerBoundaries.Add((idx + idxOffset), segm);
               BoundaryLinesDict.Add(segm, (idx + boundLinesDictOffset));       // boundaryLinesDict is a dictionary for the combined outer and inner boundaries, the values should be sequential
            }
            prevIdx = vIdx;
            idx++;
         }
      }
   }

   /// <summary>
   /// Private class that defines a line segment defined by index to the vertices
   /// </summary>
   class IndexSegment
   {
      /// <summary>
      /// Vertex index of the starting point
      /// </summary>
      public int StartPindex { get; set; }
      /// <summary>
      /// Vertex index of the end point
      /// </summary>
      public int EndPIndex { get; set; }
      /// <summary>
      /// Constructor for generating the class
      /// </summary>
      /// <param name="startIndex">Vertex index of the starting point</param>
      /// <param name="endIndex">Vertex index of the end point</param>
      public IndexSegment(int startIndex, int endIndex)
      {
         StartPindex = startIndex;
         EndPIndex = endIndex;
      }

      /// <summary>
      /// Extent size (length) of the line segment
      /// </summary>
      public double Extent(ref IDictionary<int, XYZ> meshVertices)
      {
         return meshVertices[StartPindex].DistanceTo(meshVertices[EndPIndex]);
      }

      /// <summary>
      /// Test whether a line segment coincides with this one (must be exactly the same start - end, or end - start) 
      /// </summary>
      /// <param name="inputSegment">Line segment to test for coincidence</param>
      /// <param name="compareBothDirections">Whether to check if the input segment is the reverse of this one</param>
      /// <returns>True if coincide</returns>
      public bool Coincide(IndexSegment inputSegment, bool compareBothDirections)
      {
         return ((StartPindex == inputSegment.StartPindex && EndPIndex == inputSegment.EndPIndex)
            || (compareBothDirections && (EndPIndex == inputSegment.StartPindex && StartPindex == inputSegment.EndPIndex)));
      }

      /// <summary>
      /// Reverse the order of the line segment (end to start)
      /// </summary>
      /// <returns></returns>
      public IndexSegment Reverse()
      {
         return new IndexSegment(EndPIndex, StartPindex);
      }
   }


   class SegmentComparer : EqualityComparer<IndexSegment>
   {
      private bool m_CompareBothDirections = false;

      public SegmentComparer(bool compareBothDirections)
      {
         m_CompareBothDirections = compareBothDirections;
      }

      public override bool Equals(IndexSegment o1, IndexSegment o2)
      {
         return o1.Coincide(o2, m_CompareBothDirections);
      }

      public override int GetHashCode(IndexSegment obj)
      {
         // Simple hashcode implementation as described in Joshua Bloch's "Effective Java" book
         int a = m_CompareBothDirections ? Math.Min(obj.StartPindex, obj.EndPIndex) : obj.StartPindex;
         int b = m_CompareBothDirections ? Math.Max(obj.StartPindex, obj.EndPIndex) : obj.EndPIndex;
         int hash = 23;
         hash = hash * 31 + a;
         hash = hash * 31 + b;
         return hash;
      }
   }


   /// <summary>
   /// Custom IEqualityComparer for a vector with tolerance for use with Dictionary
   /// </summary>
   
   class VectorCompare : IEqualityComparer<XYZ>
   {
      public bool Equals(XYZ o1, XYZ o2)
      {
         return (Math.Abs((o1 as XYZ).X - (o2 as XYZ).X) < TriangleMergeUtil.Tolerance) &&
         (Math.Abs((o1 as XYZ).Y - (o2 as XYZ).Y) < TriangleMergeUtil.Tolerance) &&
         (Math.Abs((o1 as XYZ).Z - (o2 as XYZ).Z) < TriangleMergeUtil.Tolerance);
      }

      public int GetHashCode(XYZ obj)
      {
         // Uses the precision set in MathUtils to round the values so that the HashCode 
         // will be consistent with the Equals method.
         double X = Math.Round(obj.X, TriangleMergeUtil.DecimalPrecision);
         double Y = Math.Round(obj.Y, TriangleMergeUtil.DecimalPrecision);
         double Z = Math.Round(obj.Z, TriangleMergeUtil.DecimalPrecision);

         return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
      }
   }

   /// <summary>
   /// This is a special class to be used to merge triangles into polygonal faces (only work for planar faces of course)
   /// </summary>
   public class TriangleMergeUtil
   {
      protected TriangulatedShellComponent m_Geom = null;
      protected Mesh m_MeshGeom = null;
      protected IDictionary<int, XYZ> m_MeshVertices = new Dictionary<int, XYZ>();

      HashSet<int> m_MergedFaceSet = new HashSet<int>();

      IDictionary<int, IndexFace> m_FacesCollDict = new Dictionary<int, IndexFace>();
      int faceIdxOffset = 0;

      // These two must be hand in hand
      public const double Tolerance = 1e-6;
      public const int DecimalPrecision = 6;

      public bool IsMesh { get { return (m_MeshGeom != null && m_Geom == null); } }

      /// <summary>
      /// Function called before and after triangle merging.
      /// Before merging it calculates the Euler characteristic of the original mesh.
      /// After merging it calculates the Euler characteristic of the merged mesh.
      /// </summary>
      private int CalculateEulerCharacteristic()
      {
         int noVertices = 0;
         int noHoles = 0; // Stays zero if mesh is triangular
         int noFaces;
         HashSet<IndexSegment> edges = new HashSet<IndexSegment>(new SegmentComparer(true/*compareBothDirections*/));
         HashSet<int> vertices = new HashSet<int>();

         if (m_MergedFaceSet.Count != 0)
         {
            // Merging already occurred, calculate new Euler characteristic
            noFaces = m_MergedFaceSet.Count;
            foreach (int mergedFace in m_MergedFaceSet)
            {
               m_FacesCollDict[mergedFace].IndexOuterBoundary.ToList().ForEach(vt => vertices.Add(vt));
               if (m_FacesCollDict[mergedFace].IndexedInnerBoundaries != null)
               {
                  foreach (IList<int> innerB in m_FacesCollDict[mergedFace].IndexedInnerBoundaries)
                     innerB.ToList().ForEach(vt => vertices.Add(vt));
               }

               m_FacesCollDict[mergedFace].OuterAndInnerBoundaries.ToList().ForEach(vp => edges.Add(vp.Value));
               if (m_FacesCollDict[mergedFace].IndexedInnerBoundaries != null)
                  noHoles += m_FacesCollDict[mergedFace].IndexedInnerBoundaries.Count;
            }
            noVertices = vertices.Count;  // Vertices must be counted from the final merged faces as some of the inner vertices may disappear after stitching
         }
         else
         {
            if (IsMesh)
            {
               noVertices = m_MeshGeom.Vertices.Count;
               noFaces = m_MeshGeom.NumTriangles;
               for (int faceIdx = 0; faceIdx < noFaces; faceIdx++)
               {
                  MeshTriangle tri = m_MeshGeom.get_Triangle(faceIdx);
                  edges.Add(new IndexSegment((int)tri.get_Index(0), (int)tri.get_Index(1)));
                  edges.Add(new IndexSegment((int)tri.get_Index(1), (int)tri.get_Index(2)));
                  edges.Add(new IndexSegment((int)tri.get_Index(2), (int)tri.get_Index(0)));
               }
            }
            else
            {
               noVertices = m_Geom.VertexCount;
               noFaces = m_Geom.TriangleCount;
               for (int faceIdx = 0; faceIdx < noFaces; faceIdx++)
               {
                  TriangleInShellComponent tri = m_Geom.GetTriangle(faceIdx);
                  edges.Add(new IndexSegment(tri.VertexIndex0, tri.VertexIndex1));
                  edges.Add(new IndexSegment(tri.VertexIndex1, tri.VertexIndex2));
                  edges.Add(new IndexSegment(tri.VertexIndex2, tri.VertexIndex0));
               }
            }
         }

         // V - E + F - I
         return noVertices - edges.Count + noFaces - noHoles;
      }

      /// <summary>
      /// Constructor for the class, accepting the TriangulatedShellComponent from the result of body tessellation
      /// </summary>
      /// <param name="triangulatedBody"></param>
      public TriangleMergeUtil(TriangulatedShellComponent triangulatedBody)
      {
         m_Geom = triangulatedBody;
         m_MeshGeom = null;
      }

      /// <summary>
      /// Constructor for the class, accepting a Mesh
      /// </summary>
      /// <param name="triangulatedMesh">the Mesh</param>
      public TriangleMergeUtil(Mesh triangulatedMesh)
      {
         m_Geom = null;
         m_MeshGeom = triangulatedMesh;
         // A Dictionary is created for the mesh vertices due to performance issue for very large mesh if the vertex is accessed via its index
         m_MeshVertices.Clear();
         int idx = 0;
         foreach (XYZ vert in m_MeshGeom.Vertices)
         {
            m_MeshVertices.Add(idx, vert);
            idx++;
         }
      }

      /// <summary>
      /// Number of faces in this merged faces
      /// </summary>
      public int NoOfFaces
      {
         get { return m_MergedFaceSet.Count; }
      }

      /// <summary>
      /// Get the specific outer boundary (index of vertices)
      /// </summary>
      /// <param name="fIdx">the index face</param>
      /// <returns>return index of vertices</returns>
      public IList<int> IndexOuterboundOfFaceAt(int fIdx)
      {
         return m_FacesCollDict[m_MergedFaceSet.ElementAt(fIdx)].IndexOuterBoundary;
      }

      /// <summary>
      /// Number of holes in a specific face
      /// </summary>
      /// <param name="fIdx">index of the face</param>
      /// <returns>number of the holes in the face</returns>
      public int NoOfHolesInFace(int fIdx)
      {
         if (m_FacesCollDict[m_MergedFaceSet.ElementAt(fIdx)].IndexedInnerBoundaries == null)
            return 0;
         if (m_FacesCollDict[m_MergedFaceSet.ElementAt(fIdx)].IndexedInnerBoundaries.Count == 0)
            return 0;
         return m_FacesCollDict[m_MergedFaceSet.ElementAt(fIdx)].IndexedInnerBoundaries.Count;
      }

      /// <summary>
      /// Get the inner boundaries of the merged faces for a specific face
      /// </summary>
      /// <param name="fIdx">the index of the face</param>
      /// <return>List of list of the inner boundaries</returns>
      public IList<IList<int>> IndexInnerBoundariesOfFaceAt(int fIdx)
      {
         return m_FacesCollDict[m_MergedFaceSet.ElementAt(fIdx)].IndexedInnerBoundaries;
      }

      public static XYZ NormalByNewellMethod(IList<XYZ> vertices)
      {
         XYZ normal;
         if (vertices.Count == 3)
         {
            // If there are only 3 vertices, which is definitely a plannar face, we will use directly 2 vectors and calculate the cross product for the normal vector
            XYZ v1 = vertices[1] - vertices[0];
            XYZ v2 = vertices[2] - vertices[1];
            normal = v1.CrossProduct(v2);
         }
         else
         {
            double normX = 0;
            double normY = 0;
            double normZ = 0;

            // Use Newell algorithm only when there are more than 3 vertices to handle non-convex face and colinear edges
            int idx = 0;
            XYZ prevVert = XYZ.Zero;
            int vertCount = vertices.Count;
            foreach (XYZ vert in vertices)
            {
               if (idx > 0 && idx <= vertCount - 1)
               {
                  normX += (prevVert.Y - vert.Y) * (prevVert.Z + vert.Z);
                  normY += (prevVert.Z - vert.Z) * (prevVert.X + vert.X);
                  normZ += (prevVert.X - vert.X) * (prevVert.Y + vert.Y);
               }
               if (idx == vertCount - 1)
               {
                  normX += (vert.Y - vertices[0].Y) * (vert.Z + vertices[0].Z);
                  normY += (vert.Z - vertices[0].Z) * (vert.X + vertices[0].X);
                  normZ += (vert.X - vertices[0].X) * (vert.Y + vertices[0].Y);
               }

               prevVert = vert;
               idx++;
            }
            normal = new XYZ(normX, normY, normZ);
         }
         return normal.Normalize();
      }

      /// <summary>
      /// Combine coplanar triangles from the faceted body if they share the edge. From this process, polygonal faces (with or without holes) will be created
      /// </summary>
      public void SimplifyAndMergeFaces(bool ignoreMerge = false)
      {
         int eulerBefore = ignoreMerge ? 0 : CalculateEulerCharacteristic();

         int noTriangle = (IsMesh) ? m_MeshGeom.NumTriangles : m_Geom.TriangleCount;
         IEqualityComparer<XYZ> normalComparer = new VectorCompare();
         Dictionary<XYZ, List<int>> faceSortedByNormal = new Dictionary<XYZ, List<int>>(normalComparer);

         for (int ef = 0; ef < noTriangle; ++ef)
         {
            IList<int> vertIndex = new List<int>();

            if (IsMesh)
            {
               MeshTriangle f = m_MeshGeom.get_Triangle(ef);
               vertIndex = new List<int>(3) { (int)f.get_Index(0), (int)f.get_Index(1), (int)f.get_Index(2) };
            }
            else
            {
               TriangleInShellComponent f = m_Geom.GetTriangle(ef);
               vertIndex = new List<int>(3) { f.VertexIndex0, f.VertexIndex1, f.VertexIndex2 };
            }

            IndexFace intF = new IndexFace(vertIndex, ref m_MeshVertices);
            m_FacesCollDict.Add(faceIdxOffset++, intF);         // Keep faces in a dictionary and assigns ID
            List<int> fIDList;

            if (!faceSortedByNormal.TryGetValue(intF.Normal, out fIDList))
            {
               fIDList = new List<int>(1) { ef };
               faceSortedByNormal.Add(intF.Normal, fIDList);
            }
            else if (!fIDList.Contains(ef))
            {
               fIDList.Add(ef);
            }
         }

         foreach (KeyValuePair<XYZ, List<int>> fListDict in faceSortedByNormal)
         {
            List<int> mergedFaceList = null;
            if (fListDict.Value.Count > 1)
            {
               if (!ignoreMerge)
                  TryMergeFaces(fListDict.Value, out mergedFaceList);
               else
               {
                  // keep original face list
                  mergedFaceList = fListDict.Value;
               }
               if (mergedFaceList != null && mergedFaceList.Count > 0)
               {
                  // insert only new face indexes as the mergedlist from different vertices can be duplicated
                  foreach (int fIdx in mergedFaceList)
                  {
                     if (!m_MergedFaceSet.Contains(fIdx))
                        m_MergedFaceSet.Add(fIdx);
                  }
               }
            }
            else if (!m_MergedFaceSet.Contains(fListDict.Value[0]))
               m_MergedFaceSet.Add(fListDict.Value[0]);    // No pair face, add it into the mergedList
         }

         int eulerAfter = ignoreMerge ? 0 : CalculateEulerCharacteristic();
         if (eulerBefore != eulerAfter)
            throw new InvalidOperationException(); // Coplanar merge broke the mesh in some way, so we need to fall back to exporting a triangular mesh
      }

      /// <summary>
      /// Go through the input face list that share the same vertex and has the same normal (coplannar).
      /// </summary>
      /// <param name="inputFaceList"></param>
      /// <param name="outputFaceList"></param>
      /// <returns>True if done successfully</returns>
      void TryMergeFaces(List<int> inputFaceList, out List<int> outputFaceList)
      {
         outputFaceList = new List<int>();
         IndexFace firstF = m_FacesCollDict[inputFaceList[0]];
         int currProcFace = inputFaceList[0];
         inputFaceList.RemoveAt(0);  // remove the first face from the list
         bool merged = false;

         IEqualityComparer<IndexSegment> segCompare = new SegmentComparer(false/*compareBothDirections*/);
         IDictionary<IndexSegment, Tuple<int, int>> segmentOfFaceDict = new Dictionary<IndexSegment, Tuple<int, int>>(segCompare);
         IList<int> discardList = new List<int>();
         foreach (int fidx in inputFaceList)
         {
            if (!SegmentOfFaceToDict(ref segmentOfFaceDict, fidx))
               discardList.Add(fidx);
         }

         // Remove bad face from the input list, if any
         if (discardList.Count > 0)
            foreach (int fidx in discardList)
               inputFaceList.Remove(fidx);
         discardList.Clear();

         while (inputFaceList.Count > 0)
         {
            IndexFace mergedFace = null;
            for (int currEdgeIdx = 0; currEdgeIdx < firstF.OuterAndInnerBoundaries.Count; currEdgeIdx++)
            {
               IndexSegment currEdge = firstF.OuterAndInnerBoundaries[currEdgeIdx];
               IndexSegment reversedEdge = currEdge.Reverse();

               IndexFace currFace = null;
               int currFaceIdx = -1;
               int idx = -1;
               Tuple<int, int> pairedFace = null;

               if (!segmentOfFaceDict.TryGetValue(reversedEdge, out pairedFace))
               {
                  if (!segmentOfFaceDict.TryGetValue(currEdge, out pairedFace))
                  {
                     merged = false;
                     continue;
                  }
                  else
                  {
                     currFaceIdx = pairedFace.Item1;
                     currFace = m_FacesCollDict[currFaceIdx];

                     // Need to reverse the face boundaries. Remove the entries in the Dict first, reverse the face, and add them back
                     for (int cidx = 0; cidx < currFace.OuterAndInnerBoundaries.Count; ++cidx)
                     {
                        segmentOfFaceDict.Remove(currFace.OuterAndInnerBoundaries[cidx]);
                     }
                     currFace.Reverse();
                     if (!SegmentOfFaceToDict(ref segmentOfFaceDict, currFaceIdx))
                     {
                        // Something is wrong with this face (should not be here in the normal circumstance), discard it and continue
                        inputFaceList.Remove(currFaceIdx);
                        merged = false;
                        continue;
                     }
                     if (!segmentOfFaceDict.TryGetValue(reversedEdge, out pairedFace))
                        if (!segmentOfFaceDict.TryGetValue(currEdge, out pairedFace))
                        {
                           // Should not be here. If somehow here, discard the face and continue
                           inputFaceList.Remove(currFaceIdx);
                           merged = false;
                           continue;
                        }
                     idx = pairedFace.Item2;
                  }
               }
               else
               {
                  currFaceIdx = pairedFace.Item1;
                  currFace = m_FacesCollDict[currFaceIdx];
                  idx = pairedFace.Item2;
               }

               // Now we need to check other edges of this face whether there is other coincide edge (this is in the case of hole(s))
               List<int> fFaceIdxList = new List<int>();
               List<int> currFaceIdxList = new List<int>();
               int ci = 0;
               foreach (KeyValuePair<int,IndexSegment> idxSeg in currFace.OuterAndInnerBoundaries)
               {
                  if (ci == idx)
                     continue;   // skip already known coincide edge
                  int ffIdx = -1;
                  IndexSegment reL = new IndexSegment(idxSeg.Value.EndPIndex, idxSeg.Value.StartPindex);
                  ffIdx = firstF.FindMatchedIndexSegment(reL);
                  if (ffIdx > 0)
                  {
                     fFaceIdxList.Add(ffIdx);        // List of edges to skip when merging
                     currFaceIdxList.Add(ci);        // List of edges to skip when merging
                  }
                  ci++;
               }

               // Now we will remove the paired edges and merge the faces
               List<IndexSegment> newFaceEdges = new List<IndexSegment>();
               for (int ii = 0; ii < currEdgeIdx; ii++)
               {
                  bool toSkip = false;
                  if (fFaceIdxList.Count > 0)
                     toSkip = fFaceIdxList.Contains(ii);
                  if (!toSkip)
                     newFaceEdges.Add(firstF.OuterAndInnerBoundaries[ii]);     // add the previous edges from the firstF faces first. This will skip the currEdge
               }

               // Add the next-in-sequence edges from the second face
               for (int ii = idx + 1; ii < currFace.OuterAndInnerBoundaries.Count; ii++)
               {
                  bool toSkip = false;
                  if (currFaceIdxList.Count > 0)
                     toSkip = currFaceIdxList.Contains(ii);
                  if (!toSkip)
                     newFaceEdges.Add(currFace.OuterAndInnerBoundaries[ii]);
               }
               for (int ii = 0; ii < idx; ii++)
               {
                  bool toSkip = false;
                  if (currFaceIdxList.Count > 0)
                     toSkip = currFaceIdxList.Contains(ii);
                  if (!toSkip)
                     newFaceEdges.Add(currFace.OuterAndInnerBoundaries[ii]);
               }

               for (int ii = currEdgeIdx + 1; ii < firstF.OuterAndInnerBoundaries.Count; ii++)
               {
                  bool toSkip = false;
                  if (fFaceIdxList.Count > 0)
                     toSkip = fFaceIdxList.Contains(ii);
                  if (!toSkip)
                     newFaceEdges.Add(firstF.OuterAndInnerBoundaries[ii]);
               }

               // Build a new face
               // Important to note that the list of edges may not be continuous if there is a hole. We need to go through the list here to identify whether there is any such
               //   discontinuity and collect the edges into their respective loops
               List<List<IndexSegment>> loops = new List<List<IndexSegment>>();

               List<IndexSegment> loopEdges = new List<IndexSegment>();
               loops.Add(loopEdges);

               IndexSegment prevSegm = newFaceEdges[0];
               foreach (IndexSegment idxSeg in newFaceEdges)
               {
                  if (prevSegm == idxSeg)
                  {
                     loopEdges.Add(idxSeg);
                  }
                  else
                  {
                     if (idxSeg.StartPindex == prevSegm.EndPIndex)
                        loopEdges.Add(idxSeg);
                     else
                     {
                        // Discontinuity detected
                        loopEdges = new List<IndexSegment>();   // start new loop
                        loops.Add(loopEdges);
                        loopEdges.Add(idxSeg);
                     }
                  }
                  prevSegm = idxSeg;
               }

               List<List<IndexSegment>> finalLoops = new List<List<IndexSegment>>();
               {
                  while (loops.Count > 1)
                  {
                     // There are more than 1 loops, need to consolidate if there are fragments to combine due to their continuity between the fragments
                     int toDelIdx = -1;
                     for (int ii = 1; ii < loops.Count; ii++)
                     {
                        if (loops[0][loops[0].Count - 1].EndPIndex == loops[ii][0].StartPindex)
                        {
                           // found continuity, merge the loops
                           List<IndexSegment> newLoop = new List<IndexSegment>(loops[0]);
                           newLoop.AddRange(loops[ii]);
                           finalLoops.Add(newLoop);
                           toDelIdx = ii;
                           break;
                        }
                     }
                     if (toDelIdx > 0)
                     {
                        loops.RemoveAt(toDelIdx);   // !!!! Important to remove the later member first before removing the first one 
                        loops.RemoveAt(0);
                     }
                     else
                     {
                        // No continuity found, copy the first loop to the final loop
                        List<IndexSegment> newLoop = new List<IndexSegment>(loops[0]);
                        finalLoops.Add(newLoop);
                        loops.RemoveAt(0);
                     }
                  }
                  if (loops.Count > 0)
                  {
                     // Add remaining list into the final loops
                     finalLoops.AddRange(loops);
                  }
               }

               if (finalLoops.Count > 1)
               {
                  // Find the largest loop and put it in the first position signifying the outer loop and the rest are the inner loops
                  int largestPerimeterIdx = 0;
                  double largestPerimeter = 0.0;
                  for (int i = 0; i < finalLoops.Count; i++)
                  {
                     double loopPerimeter = 0.0;
                     foreach (IndexSegment line in finalLoops[i])
                        loopPerimeter += line.Extent (ref m_MeshVertices);
                     if (loopPerimeter > largestPerimeter)
                     {
                        largestPerimeter = loopPerimeter;
                        largestPerimeterIdx = i;
                     }
                  }
                  // We need to move the largest loop into the head if it is not
                  if (largestPerimeterIdx > 0)
                  {
                     List<IndexSegment> largestLoop = new List<IndexSegment>(finalLoops[largestPerimeterIdx]);
                     finalLoops.RemoveAt(largestPerimeterIdx);
                     finalLoops.Insert(0, largestLoop);
                  }
               }

               // Collect the vertices from the list of Edges into list of list of vertices starting with the outer loop (largest loop) following the finalLoop
               IList<IList<int>> newFaceVertsLoops = new List<IList<int>>();
               foreach (List<IndexSegment> loop in finalLoops)
               {
                  IList<int> newFaceVerts = new List<int>();
                  foreach (IndexSegment idxSeg in loop)
                  {
                     newFaceVerts.Add(idxSeg.StartPindex);
                  }

                  if (newFaceVerts.Count > 0)
                  {
                     if (newFaceVerts.Count >= 3)
                        newFaceVertsLoops.Add(newFaceVerts);
                     else
                     {
                        // Something wrong, a face cannot have less than 3 vertices
                        Debug.WriteLine("Something went wrong when merging faces resulting with a loop that has less than 3 vertices");
                     }
                  }
               }

               mergedFace = new IndexFace(newFaceVertsLoops, ref m_MeshVertices);
               inputFaceList.Remove(currFaceIdx);

               // Remove the merged face from segmentOfFaceDict
               foreach (KeyValuePair<int, IndexSegment> idxSeg in m_FacesCollDict[currFaceIdx].OuterAndInnerBoundaries)
               {
                  segmentOfFaceDict.Remove(idxSeg.Value);
               }
               if (m_FacesCollDict.ContainsKey(currFaceIdx))
                  m_FacesCollDict.Remove(currFaceIdx);

               merged = true;
               break;      // Once there is an edge merged, create a new face and continue the process of merging
            }

            int lastFaceID = faceIdxOffset++;   // The new index is always the next one in the collection was inserted based on the seq order
            if (mergedFace != null)
               m_FacesCollDict.Add(lastFaceID, mergedFace);

            if (!merged)
            {
               // No edge match for this face, add the face into the output face list and move to the next face in the input list
               outputFaceList.Add(currProcFace);

               if (inputFaceList.Count > 0)
               {
                  firstF = m_FacesCollDict[inputFaceList[0]];

                  // Remove the merged face from segmentOfFaceDict
                  foreach (KeyValuePair<int, IndexSegment> idxSeg in firstF.OuterAndInnerBoundaries)
                  {
                     segmentOfFaceDict.Remove(idxSeg.Value);
                  }
                  currProcFace = inputFaceList[0];  // keep the last processed item
                  inputFaceList.RemoveAt(0);  // remove the first face from the list
                  merged = false;

                  // If there is no more face to process, add the merged face into the output list
                  if (inputFaceList.Count == 0)
                     outputFaceList.Add(currProcFace);
               }
            }
            else
            {
               // If there is no more face to process, add the merged face into the output list
               if (inputFaceList.Count == 0)
                  outputFaceList.Add(lastFaceID);

               // Remove merged face from the Dict
               if (m_FacesCollDict.ContainsKey(currProcFace))
                  m_FacesCollDict.Remove(currProcFace);

               if (inputFaceList.Count > 0)
               {
                  // use the current face as the next face as a merge candidate
                  firstF = mergedFace;
                  currProcFace = lastFaceID;
                  merged = false;
               }
            }
         }

         // Finally, there may be multiple faces left because there are multiple disconnected faces at the same normal. Collect them and return
         if (segmentOfFaceDict.Count > 0)
         {
            HashSet<int> indexFaces = new HashSet<int>();
            foreach (KeyValuePair<IndexSegment, Tuple<int, int>> segmentFace in segmentOfFaceDict)
            {
               indexFaces.Add(segmentFace.Value.Item1);
            }
            foreach (int idxFace in indexFaces)
               outputFaceList.Add(idxFace);
         }
      }

      bool SegmentOfFaceToDict(ref IDictionary<IndexSegment, Tuple<int, int>> segmentOfFaceDict, int indexFace)
      {
         IList<IndexSegment> entriesToRollback = new List<IndexSegment>();
         IndexFace theFace = m_FacesCollDict[indexFace];
         try
         {
            int idx = 0;
            foreach (KeyValuePair<int, IndexSegment> idxSeg in theFace.OuterAndInnerBoundaries)
            {
               segmentOfFaceDict.Add(idxSeg.Value, new Tuple<int, int>(indexFace, idx++));
               entriesToRollback.Add(idxSeg.Value);
            }
            return true;
         }
         catch
         {
            // If exception, it is likely that there is duplicate. Remove all segments of this face first to rollback
            foreach (IndexSegment segDel in entriesToRollback)
               segmentOfFaceDict.Remove(segDel);
            entriesToRollback.Clear();
         }

         theFace.Reverse();
         try
         {
            int idx = 0;
            foreach (KeyValuePair<int,IndexSegment> idxSeg in theFace.OuterAndInnerBoundaries)
            {
               segmentOfFaceDict.Add(idxSeg.Value, new Tuple<int, int>(indexFace, idx++));
               entriesToRollback.Add(idxSeg.Value);
            }
            return true;
         }
         catch
         {
            // If still raises an exception (that shouldn't be). It is likely there is simple duplicate face. Cleanup, and return false;
            foreach (IndexSegment segDel in entriesToRollback)
               segmentOfFaceDict.Remove(segDel);
            return false;
         }
      }
   }
}


