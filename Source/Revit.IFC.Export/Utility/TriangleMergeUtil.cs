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
   /// This is a special class to be used to merge triangles into polygonal faces (only work for planar faces of course)
   /// </summary>
   public class TriangleMergeUtil
   {
      static TriangulatedShellComponent _geom;
      static Mesh _meshGeom;
      static IDictionary<int, XYZ> _meshVertices = new Dictionary<int, XYZ>();

      HashSet<int> _mergedFaceList = new HashSet<int>();

      IDictionary<int, IndexFace> facesColl = new Dictionary<int, IndexFace>();

      // These two must be hand in hand
      static double _tol = 1e-6;
      static int _tolNoDecPrecision = 6;
      // ------

      //IDictionary<int, HashSet<int>> sortedFVert = new Dictionary<int, HashSet<int>>();

      public static bool IsMesh { get { return (_meshGeom != null && _geom == null); } }

      public class SegmentComparer : EqualityComparer<IndexSegment>
      {
         private bool _compareBothDirections = false;

         public SegmentComparer(bool compareBothDirections)
         {
            _compareBothDirections = compareBothDirections;
         }

         public override bool Equals(IndexSegment o1, IndexSegment o2)
         {
            return o1.coincide(o2, _compareBothDirections);
         }

         public override int GetHashCode(IndexSegment obj)
         {
            // Simple hashcode implementation as described in Joshua Bloch's "Effective Java" book
            int a = _compareBothDirections ? Math.Min(obj.startPindex, obj.endPIndex) : obj.startPindex;
            int b = _compareBothDirections ? Math.Max(obj.startPindex, obj.endPIndex) : obj.endPIndex;
            int hash = 23;
            hash = hash * 31 + a;
            hash = hash * 31 + b;
            return hash;
         }
      }

      /// <summary>
      /// Function called before and after triangle merging.
      /// Before merging it calculates the Euler characteristic of the original mesh.
      /// After merging it calculates the Euler characteristic of the merged mesh.
      /// </summary>
      private int CalculateEulerCharacteristic()
      {
         int noVertices = (IsMesh) ? _meshGeom.Vertices.Count : _geom.VertexCount;
         int noHoles = 0; // Stays zero if mesh is triangular
         int noFaces;
         HashSet<IndexSegment> edges = new HashSet<IndexSegment>(new SegmentComparer(true/*compareBothDirections*/));
         if (_mergedFaceList.Count != 0)
         {
            // Merging already occurred, calculate new Euler characteristic
            noFaces = _mergedFaceList.Count;
            foreach (int mergedFace in _mergedFaceList)
            {
               edges.UnionWith(facesColl[mergedFace].outerAndInnerBoundaries);
               if (facesColl[mergedFace].indexedInnerBoundaries != null)
                  noHoles += facesColl[mergedFace].indexedInnerBoundaries.Count;
            }
         }
         else
         {
            if (IsMesh)
            {
               noFaces = _meshGeom.NumTriangles;
               for (int faceIdx = 0; faceIdx < noFaces; faceIdx++)
               {
                  MeshTriangle tri = _meshGeom.get_Triangle(faceIdx);
                  edges.Add(new IndexSegment((int)tri.get_Index(0), (int)tri.get_Index(1)));
                  edges.Add(new IndexSegment((int)tri.get_Index(1), (int)tri.get_Index(2)));
                  edges.Add(new IndexSegment((int)tri.get_Index(2), (int)tri.get_Index(0)));
               }
            }
            else
            {
               noFaces = _geom.TriangleCount;
               for (int faceIdx = 0; faceIdx < noFaces; faceIdx++)
               {
                  TriangleInShellComponent tri = _geom.GetTriangle(faceIdx);
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
         _geom = triangulatedBody;
         _meshGeom = null;
      }

      /// <summary>
      /// Constructor for the class, accepting a Mesh
      /// </summary>
      /// <param name="triangulatedMesh">the Mesh</param>
      public TriangleMergeUtil(Mesh triangulatedMesh)
      {
         _geom = null;
         _meshGeom = triangulatedMesh;
         // A Dictionary is created for the mesh vertices due to performance issue for very large mesh if the vertex is accessed via its index
         _meshVertices.Clear();
         int idx = 0;
         foreach (XYZ vert in _meshGeom.Vertices)
         {
            _meshVertices.Add(idx, vert);
            idx++;
         }
      }

      /// <summary>
      /// Custom IEqualityComparer for a vector with tolerance for use with Dictionary
      /// </summary>
      public class vectorCompare : IEqualityComparer<XYZ>
      {
         public bool Equals(XYZ o1, XYZ o2)
         {
            bool xdiff = Math.Abs((o1 as XYZ).X - (o2 as XYZ).X) < _tol;
            bool ydiff = Math.Abs((o1 as XYZ).Y - (o2 as XYZ).Y) < _tol;
            bool zdiff = Math.Abs((o1 as XYZ).Z - (o2 as XYZ).Z) < _tol;
            if (xdiff && ydiff && zdiff)
               return true;
            else
               return false;
         }

         public int GetHashCode(XYZ obj)
         {
            // Uses the precision set in MathUtils to round the values so that the HashCode will be consistent with the Equals method
            double X = Math.Round(obj.X, _tolNoDecPrecision);
            double Y = Math.Round(obj.Y, _tolNoDecPrecision);
            double Z = Math.Round(obj.Z, _tolNoDecPrecision);

            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
         }
      }

      /// <summary>
      /// Private class that defines a line segment defined by index to the vertices
      /// </summary>
      public class IndexSegment
      {
         /// <summary>
         /// Vertex index of the starting point
         /// </summary>
         public int startPindex { get; set; }
         /// <summary>
         /// Vertex index of the end point
         /// </summary>
         public int endPIndex { get; set; }
         /// <summary>
         /// Constructor for generating the class
         /// </summary>
         /// <param name="startIndex">Vertex index of the starting point</param>
         /// <param name="endIndex">Vertex index of the end point</param>
         public IndexSegment(int startIndex, int endIndex)
         {
            startPindex = startIndex;
            endPIndex = endIndex;
         }

         /// <summary>
         /// Extent size (length) of the line segment
         /// </summary>
         public double extent
         {
            get
            {
               if (IsMesh)
                  return _meshVertices[startPindex].DistanceTo(_meshVertices[endPIndex]);
               else
                  return _geom.GetVertex(startPindex).DistanceTo(_geom.GetVertex(endPIndex));
            }
         }

         /// <summary>
         /// Test whether a line segment coincides with this one (must be exactly the same start - end, or end - start) 
         /// </summary>
         /// <param name="inputSegment">Line segment to test for coincidence</param>
         /// <param name="compareBothDirections">Whether to check if the input segment is the reverse of this one</param>
         /// <returns>True if coincide</returns>
         public bool coincide(IndexSegment inputSegment, bool compareBothDirections)
         {
            return ((startPindex == inputSegment.startPindex && endPIndex == inputSegment.endPIndex)
               || (compareBothDirections && (endPIndex == inputSegment.startPindex && startPindex == inputSegment.endPIndex)));
         }

         /// <summary>
         /// Reverse the order of the line segment (end to start)
         /// </summary>
         /// <returns></returns>
         public IndexSegment reverse()
         {
            return new IndexSegment(endPIndex, startPindex);
         }
      }

      /// <summary>
      /// Private class to creates a face based on the vertex indices
      /// </summary>
      class IndexFace
      {
         /// <summary>
         /// Vertex indices for the outer boundary of the face
         /// </summary>
         public IList<int> indexOuterBoundary { get; set; }
         /// <summary>
         /// List of vertex indices for the inner boundaries
         /// </summary>
         public IList<IList<int>> indexedInnerBoundaries { get; set; }
         /// <summary>
         /// Collection of all the vertices (outer and inner)
         /// </summary>
         public IList<IndexSegment> outerAndInnerBoundaries { get; set; }
         IDictionary<IndexSegment, int> boundaryLinesDict;
         /// <summary>
         /// The normal vector of the face
         /// </summary>
         public XYZ normal { get; set; }

         /// <summary>
         /// Constructor taking a list of vertex indices (face without hole) 
         /// </summary>
         /// <param name="vertxIndices">the list of vertex indices (face without hole)</param>
         public IndexFace(IList<int> vertxIndices)
         {
            indexOuterBoundary = vertxIndices;
            outerAndInnerBoundaries = setupEdges(indexOuterBoundary);

            IList<XYZ> vertices = new List<XYZ>();
            foreach (int idx in indexOuterBoundary)
            {
               if (IsMesh)
                  vertices.Add(_meshVertices[idx]);
               else
                  vertices.Add(_geom.GetVertex(idx));
            }
            normal = NormalByNewellMethod(vertices);
         }

         /// <summary>
         /// Constructor taking in List of List of vertices. The first list will be the outer boundary and the rest are the inner boundaries
         /// </summary>
         /// <param name="vertxIndices">List of List of vertices. The first list will be the outer boundary and the rest are the inner boundaries</param>
         public IndexFace(IList<IList<int>> vertxIndices)
         {
            if (vertxIndices == null)
               return;
            if (vertxIndices.Count == 0)
               return;

            indexOuterBoundary = vertxIndices[0];
            vertxIndices.RemoveAt(0);
            outerAndInnerBoundaries = setupEdges(indexOuterBoundary);

            if (vertxIndices.Count != 0)
            {
               indexedInnerBoundaries = vertxIndices;

               List<IndexSegment> innerBIndexList = new List<IndexSegment>();
               foreach (IList<int> innerBound in indexedInnerBoundaries)
               {
                  innerBIndexList.AddRange(setupEdges(innerBound));
               }

               foreach (IndexSegment seg in innerBIndexList)
                  outerAndInnerBoundaries.Add(seg);
            }

            // Create normal from only the outer boundary
            IList<XYZ> vertices = new List<XYZ>();
            foreach (int idx in indexOuterBoundary)
            {
               if (IsMesh)
                  vertices.Add(_meshVertices[idx]);
               else
                  vertices.Add(_geom.GetVertex(idx));
            }
            normal = NormalByNewellMethod(vertices);
         }

         /// <summary>
         /// Reverse the order of the vertices. Only operates on the outer boundary 
         /// </summary>
         public void Reverse()
         {
            // This is used in the process of combining triangles and therefore will work only with the face without holes
            List<int> revIdxOuter = indexOuterBoundary.ToList();
            revIdxOuter.Reverse();
            outerAndInnerBoundaries.Clear();
            boundaryLinesDict.Clear();
            outerAndInnerBoundaries = setupEdges(revIdxOuter);
         }

         /// <summary>
         /// Find matched line segment in the face boundaries
         /// </summary>
         /// <param name="inpSeg">Input line segment as vertex indices</param>
         /// <returns>Return index of the matched segment</returns>
         public int findMatchedIndexSegment(IndexSegment inpSeg)
         {
            int idx;
            if (boundaryLinesDict.TryGetValue(inpSeg, out idx))
               return idx;
            else
               return -1;
         }

         IList<IndexSegment> setupEdges(IList<int> vertxIndices)
         {
            IList<IndexSegment> indexList = new List<IndexSegment>();
            int boundLinesDictOffset = 0;

            if (boundaryLinesDict == null)
            {
               IEqualityComparer<IndexSegment> segCompare = new SegmentComparer(false/*compareBothDirections*/);
               boundaryLinesDict = new Dictionary<IndexSegment, int>(segCompare);
            }
            else
               boundLinesDictOffset = boundaryLinesDict.Count();

            for (int ii = 0; ii < vertxIndices.Count; ++ii)
            {
               IndexSegment segm;
               if (ii == vertxIndices.Count - 1)
               {
                  segm = new IndexSegment(vertxIndices[ii], vertxIndices[0]);

               }
               else
               {
                  segm = new IndexSegment(vertxIndices[ii], vertxIndices[ii + 1]);
               }
               indexList.Add(segm);
               boundaryLinesDict.Add(segm, ii + boundLinesDictOffset);       // boundaryLinesDict is a dictionary for the combined outer and inner boundaries, the values should be sequential
            }

            return indexList;
         }
      }

      /// <summary>
      /// Number of faces in this merged faces
      /// </summary>
      public int NoOfFaces
      {
         get { return _mergedFaceList.Count; }
      }

      /// <summary>
      /// Get the specific outer boundary (index of vertices)
      /// </summary>
      /// <param name="fIdx">the index face</param>
      /// <returns>return index of vertices</returns>
      public IList<int> IndexOuterboundOfFaceAt(int fIdx)
      {
         return facesColl[_mergedFaceList.ElementAt(fIdx)].indexOuterBoundary;
      }

      /// <summary>
      /// Number of holes in a specific face
      /// </summary>
      /// <param name="fIdx">index of the face</param>
      /// <returns>number of the holes in the face</returns>
      public int NoOfHolesInFace(int fIdx)
      {
         if (facesColl[_mergedFaceList.ElementAt(fIdx)].indexedInnerBoundaries == null)
            return 0;
         if (facesColl[_mergedFaceList.ElementAt(fIdx)].indexedInnerBoundaries.Count == 0)
            return 0;
         return facesColl[_mergedFaceList.ElementAt(fIdx)].indexedInnerBoundaries.Count;
      }

      /// <summary>
      /// Get the inner boundaries of the merged faces for a specific face
      /// </summary>
      /// <param name="fIdx">the index of the face</param>
      /// <return>List of list of the inner boundaries</returns>
      public IList<IList<int>> IndexInnerBoundariesOfFaceAt(int fIdx)
      {
         return facesColl[_mergedFaceList.ElementAt(fIdx)].indexedInnerBoundaries;
      }

      static XYZ NormalByNewellMethod(IList<XYZ> vertices)
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
            for (int ii = 0; ii < vertices.Count; ii++)
            {
               if (ii == vertices.Count - 1)
               {
                  //The last vertex
                  normX += (vertices[ii].Y - vertices[0].Y) * (vertices[ii].Z + vertices[0].Z);
                  normY += (vertices[ii].Z - vertices[0].Z) * (vertices[ii].X + vertices[0].X);
                  normZ += (vertices[ii].X - vertices[0].X) * (vertices[ii].Y + vertices[0].Y);
               }
               else
               {
                  normX += (vertices[ii].Y - vertices[ii + 1].Y) * (vertices[ii].Z + vertices[ii + 1].Z);
                  normY += (vertices[ii].Z - vertices[ii + 1].Z) * (vertices[ii].X + vertices[ii + 1].X);
                  normZ += (vertices[ii].X - vertices[ii + 1].X) * (vertices[ii].Y + vertices[ii + 1].Y);
               }
            }
            normal = new XYZ(normX, normY, normZ);
         }
         return normal.Normalize();
      }

      /// <summary>
      /// Check if the merged mesh is closed by verifying that all its edges have a corresponding reversed edge.
      /// </summary>
      /// <returns>Boolean value that indicates whether the merged mesh is closed or not</returns>
      public bool IsClosed()
      {
         // All edges should have at least one corresponding reversed edge for the mesh to be closed
         if (_mergedFaceList.Count == 0)
            throw new InvalidOperationException("IsClosed called before coplanar triangle merge");

         // This isn't the most optimized approach, but it's intended to be used only after merging 
         // Meshes as those don't have any API data that indicates whether they are closed or not.
         // Ideally we'd want to have an appropriate API method in the future, or use a better mesh 
         // structure for analysis, like a proper half-edge representation.
         IEnumerable<IndexSegment> edges = _mergedFaceList.SelectMany(x => facesColl[x].outerAndInnerBoundaries);
         return edges.Select(x => x.reverse()).All(x => edges.Any(y => x.coincide(y, false)));
      }

      /// <summary>
      /// Combine coplanar triangles from the faceted body if they share the edge. From this process, polygonal faces (with or without holes) will be created
      /// </summary>
      public void SimplifyAndMergeFaces()
      {
         int eulerBefore = CalculateEulerCharacteristic();

         int noTriangle = (IsMesh) ? _meshGeom.NumTriangles : _geom.TriangleCount;
         int noVertices = (IsMesh) ? _meshGeom.Vertices.Count : _geom.VertexCount;
         IEqualityComparer<XYZ> normalComparer = new vectorCompare();
         Dictionary<XYZ, List<int>> faceSortedByNormal = new Dictionary<XYZ, List<int>>(normalComparer);

         for (int ef = 0; ef < noTriangle; ++ef)
         {
            IList<int> vertIndex = new List<int>();

            if (IsMesh)
            {
               MeshTriangle f = _meshGeom.get_Triangle(ef);
               vertIndex.Add((int)f.get_Index(0));
               vertIndex.Add((int)f.get_Index(1));
               vertIndex.Add((int)f.get_Index(2));
            }
            else
            {
               TriangleInShellComponent f = _geom.GetTriangle(ef);
               vertIndex.Add(f.VertexIndex0);
               vertIndex.Add(f.VertexIndex1);
               vertIndex.Add(f.VertexIndex2);
            }

            IndexFace intF = new IndexFace(vertIndex);
            facesColl.Add(ef, intF);         // Keep faces in a dictionary and assigns ID
            List<int> fIDList;

            if (!faceSortedByNormal.TryGetValue(intF.normal, out fIDList))
            {
               fIDList = new List<int>();
               fIDList.Add(ef);
               faceSortedByNormal.Add(intF.normal, fIDList);
            }
            else
            {
               if (!fIDList.Contains(ef))
               {
                  fIDList.Add(ef);
               }
            }
         }

         foreach (KeyValuePair<XYZ, List<int>> fListDict in faceSortedByNormal)
         {
            List<int> mergedFaceList = null;
            if (fListDict.Value.Count > 1)
            {
               TryMergeFaces(fListDict.Value, out mergedFaceList);
               if (mergedFaceList != null && mergedFaceList.Count > 0)
               {
                  // insert only new face indexes as the mergedlist from different vertices can be duplicated
                  foreach (int fIdx in mergedFaceList)
                     if (!_mergedFaceList.Contains(fIdx))
                        _mergedFaceList.Add(fIdx);
               }
            }
            else if (!_mergedFaceList.Contains(fListDict.Value[0]))
               _mergedFaceList.Add(fListDict.Value[0]);    // No pair face, add it into the mergedList
         }

         int eulerAfter = CalculateEulerCharacteristic();
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
         IndexFace firstF = facesColl[inputFaceList[0]];
         int currProcFace = inputFaceList[0];
         inputFaceList.RemoveAt(0);  // remove the first face from the list
         bool merged = false;

         IEqualityComparer<IndexSegment> segCompare = new SegmentComparer(false/*compareBothDirections*/);
         IDictionary<IndexSegment, Tuple<IndexFace, int, int>> segmentOfFaceDict = new Dictionary<IndexSegment, Tuple<IndexFace, int, int>>(segCompare);
         IList<int> discardList = new List<int>();
         for (int iFace = 0; iFace < inputFaceList.Count; ++iFace)
         {
            int fidx = inputFaceList[iFace];
            IndexFace IdxFace = facesColl[fidx];
            if (!segmentOfFaceToDict(ref segmentOfFaceDict, ref IdxFace, fidx))
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
            for (int currEdgeIdx = 0; currEdgeIdx < firstF.outerAndInnerBoundaries.Count; currEdgeIdx++)
            {
               IndexSegment currEdge = firstF.outerAndInnerBoundaries[currEdgeIdx];
               IndexSegment reversedEdge = currEdge.reverse();

               IndexFace currFace = null;
               int currFaceIdx = -1;
               int idx = -1;
               Tuple<IndexFace, int, int> pairedFace = null;

               if (!segmentOfFaceDict.TryGetValue(reversedEdge, out pairedFace))
               {
                  if (!segmentOfFaceDict.TryGetValue(currEdge, out pairedFace))
                  {
                     merged = false;
                     continue;
                  }
                  else
                  {
                     currFace = pairedFace.Item1;
                     currFaceIdx = pairedFace.Item2;

                     // Need to reverse the face boundaries. Remove the entries in the Dict first, reverse the face, and add them back
                     for (int cidx = 0; cidx < currFace.outerAndInnerBoundaries.Count; ++cidx)
                     {
                        segmentOfFaceDict.Remove(currFace.outerAndInnerBoundaries[cidx]);
                     }
                     currFace.Reverse();
                     if (!segmentOfFaceToDict(ref segmentOfFaceDict, ref currFace, currFaceIdx))
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
                     idx = pairedFace.Item3;
                  }
               }
               else
               {
                  currFace = pairedFace.Item1;
                  currFaceIdx = pairedFace.Item2;
                  idx = pairedFace.Item3;
               }

               // Now we need to check other edges of this face whether there is other coincide edge (this is in the case of hole(s))
               List<int> fFaceIdxList = new List<int>();
               List<int> currFaceIdxList = new List<int>();
               for (int ci = 0; ci < currFace.outerAndInnerBoundaries.Count; ci++)
               {
                  if (ci == idx)
                     continue;   // skip already known coincide edge
                  int ffIdx = -1;
                  IndexSegment reL = new IndexSegment(currFace.outerAndInnerBoundaries[ci].endPIndex, currFace.outerAndInnerBoundaries[ci].startPindex);
                  ffIdx = firstF.findMatchedIndexSegment(reL);
                  if (ffIdx > 0)
                  {
                     fFaceIdxList.Add(ffIdx);        // List of edges to skip when merging
                     currFaceIdxList.Add(ci);        // List of edges to skip when merging
                  }
               }

               // Now we will remove the paired edges and merge the faces
               List<IndexSegment> newFaceEdges = new List<IndexSegment>();
               for (int ii = 0; ii < currEdgeIdx; ii++)
               {
                  bool toSkip = false;
                  if (fFaceIdxList.Count > 0)
                     toSkip = fFaceIdxList.Contains(ii);
                  if (!toSkip)
                     newFaceEdges.Add(firstF.outerAndInnerBoundaries[ii]);     // add the previous edges from the firstF faces first. This will skip the currEdge
               }

               // Add the next-in-sequence edges from the second face
               for (int ii = idx + 1; ii < currFace.outerAndInnerBoundaries.Count; ii++)
               {
                  bool toSkip = false;
                  if (currFaceIdxList.Count > 0)
                     toSkip = currFaceIdxList.Contains(ii);
                  if (!toSkip)
                     newFaceEdges.Add(currFace.outerAndInnerBoundaries[ii]);
               }
               for (int ii = 0; ii < idx; ii++)
               {
                  bool toSkip = false;
                  if (currFaceIdxList.Count > 0)
                     toSkip = currFaceIdxList.Contains(ii);
                  if (!toSkip)
                     newFaceEdges.Add(currFace.outerAndInnerBoundaries[ii]);
               }

               for (int ii = currEdgeIdx + 1; ii < firstF.outerAndInnerBoundaries.Count; ii++)
               {
                  bool toSkip = false;
                  if (fFaceIdxList.Count > 0)
                     toSkip = fFaceIdxList.Contains(ii);
                  if (!toSkip)
                     newFaceEdges.Add(firstF.outerAndInnerBoundaries[ii]);
               }

               // Build a new face
               // Important to note that the list of edges may not be continuous if there is a hole. We need to go through the list here to identify whether there is any such
               //   discontinuity and collect the edges into their respective loops
               List<List<IndexSegment>> loops = new List<List<IndexSegment>>();

               List<IndexSegment> loopEdges = new List<IndexSegment>();
               loops.Add(loopEdges);
               for (int ii = 0; ii < newFaceEdges.Count; ii++)
               {
                  if (ii == 0)
                  {
                     loopEdges.Add(newFaceEdges[ii]);
                  }
                  else
                  {
                     if (newFaceEdges[ii].startPindex == newFaceEdges[ii - 1].endPIndex)
                        loopEdges.Add(newFaceEdges[ii]);
                     else
                     {
                        // Discontinuity detected
                        loopEdges = new List<IndexSegment>();   // start new loop
                        loops.Add(loopEdges);
                        loopEdges.Add(newFaceEdges[ii]);
                     }
                  }
               }

               List<List<IndexSegment>> finalLoops = new List<List<IndexSegment>>();
               {
                  while (loops.Count > 1)
                  {
                     // There are more than 1 loops, need to consolidate if there are fragments to combine due to their continuity between the fragments
                     int toDelIdx = -1;
                     for (int ii = 1; ii < loops.Count; ii++)
                     {
                        if (loops[0][loops[0].Count - 1].endPIndex == loops[ii][0].startPindex)
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
                        loopPerimeter += line.extent;
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
                  for (int ii = 0; ii < loop.Count; ii++)
                  {
                     if (ii == 0)
                     {
                        newFaceVerts.Add(loop[ii].startPindex);
                        newFaceVerts.Add(loop[ii].endPIndex);
                     }
                     else if (ii == loop.Count - 1)   // Last
                     {
                        // Add nothing as the last segment ends at the first vertex
                     }
                     else
                     {
                        newFaceVerts.Add(loop[ii].endPIndex);
                     }
                  }
                  // close the loop with end point from the starting point (it is important to mark the end of loop and if there is any other vertex follow, they start the inner loop)
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

               mergedFace = new IndexFace(newFaceVertsLoops);
               inputFaceList.Remove(currFaceIdx);

               // Remove the merged face from segmentOfFaceDict
               IList<IndexSegment> rem = facesColl[currFaceIdx].outerAndInnerBoundaries;
               for (int cidx = 0; cidx < rem.Count; ++cidx)
               {
                  segmentOfFaceDict.Remove(rem[cidx]);
               }

               merged = true;
               break;      // Once there is an edge merged, create a new face and continue the process of merging
            }

            int lastFaceID = facesColl.Count;   // The new index is always the next one in the collection was inserted based on the seq order
            if (mergedFace != null)
               facesColl.Add(lastFaceID, mergedFace);

            if (!merged)
            {
               // No edge match for this face, add the face into the output face list and move to the next face in the input list
               outputFaceList.Add(currProcFace);

               if (inputFaceList.Count > 0)
               {
                  firstF = facesColl[inputFaceList[0]];

                  // Remove the merged face from segmentOfFaceDict
                  IList<IndexSegment> rem = firstF.outerAndInnerBoundaries;
                  for (int cidx = 0; cidx < rem.Count; ++cidx)
                  {
                     segmentOfFaceDict.Remove(rem[cidx]);
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
            foreach (KeyValuePair<IndexSegment, Tuple<IndexFace, int, int>> segmentFace in segmentOfFaceDict)
            {
               indexFaces.Add(segmentFace.Value.Item2);
            }
            foreach (int idxFace in indexFaces)
               outputFaceList.Add(idxFace);
         }
      }

      bool segmentOfFaceToDict(ref IDictionary<IndexSegment, Tuple<IndexFace, int, int>> segmentOfFaceDict, ref IndexFace theFace, int indexFace)
      {
         IList<IndexSegment> entriesToRollback = new List<IndexSegment>();
         try
         {
            for (int idx = 0; idx < theFace.outerAndInnerBoundaries.Count; ++idx)
            {
               segmentOfFaceDict.Add(theFace.outerAndInnerBoundaries[idx], new Tuple<IndexFace, int, int>(theFace, indexFace, idx));
               entriesToRollback.Add(theFace.outerAndInnerBoundaries[idx]);
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
            for (int idx = 0; idx < theFace.outerAndInnerBoundaries.Count; ++idx)
            {
               segmentOfFaceDict.Add(theFace.outerAndInnerBoundaries[idx], new Tuple<IndexFace, int, int>(theFace, indexFace, idx));
               entriesToRollback.Add(theFace.outerAndInnerBoundaries[idx]);
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


