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
using Autodesk.Revit.DB;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Data;

namespace Revit.IFC.Import.Utility
{
   /// <summary>
   /// Provides methods to manage creation of DirectShape elements using TessellatedShapeBuilder
   /// </summary>
   public class TessellatedShapeBuilderScope : BuilderScope
   {
      /// <summary>
      /// A class that contains a grouping of arbitary XYZ values that are all "distinct",
      /// based on a tolerance.
      /// </summary>
      /// <remarks>Note: We cannot assume that we won't have duplicate entries.
      /// For example, assume tolerance=1.0, and we have values (10.2,10.2,10.2) and 
      /// (11.5,11.5,11.5) in the set.
      /// If we look for (11.0, 11.0, 11.0) then either of the 2 values above would match, 
      /// even though they are distinct values from one another.  This means that as the set
      /// grows, a second duplicate (11.0, 11.0, 11.0) entry could take a different path
      /// and not find the original match.  Aside from performance issues, this is harmless
      /// for current usage, but should be taken into account if the use is expanded.</remarks>
      private class IFCFuzzyXYZSet
      {
         /// <summary>
         /// The constructor. 
         /// </summary>
         /// <param name="tol">The tolerance at which 2 XYZ values are considered equal.</param>
         public IFCFuzzyXYZSet(double tol)
         {
            Tolerance = tol;
            VertexSet = new SortedSet<XYZ>(new IFCXYZFuzzyComparer(tol));
         }

         /// <summary>
         /// Clear the existing set. 
         /// </summary>
         public void Clear()
         {
            if (VertexSet != null)
               VertexSet.Clear();
         }

         /// <summary>
         /// Looks for a possibly adjusted vertex value in the current set.
         /// </summary>
         /// <param name="vertex">The original vertex value.</param>
         /// <returns>The possibly adjusted vertex value.</returns>
         public XYZ FindOrAdd(XYZ vertex)
         {
            XYZ adjustedVertex = null;
            if (!VertexSet.TryGetValue(vertex, out adjustedVertex))
            {
               adjustedVertex = vertex;
               VertexSet.Add(adjustedVertex);
            }
            return adjustedVertex;
         }

         /// <summary>
         /// Lowers the tolerance used for vertex matching.
         /// </summary>
         /// <param name="tol">The new tolerance, that must be lower than the old one.</param>
         public void ResetTolerance(double tol)
         {
            if (tol > Tolerance)
               throw new ArgumentException("The tolerance can only be reset to be stricter.");
            
            Tolerance = tol;
            var newVertexSet = new SortedSet<XYZ>(VertexSet, new IFCXYZFuzzyComparer(tol));
            VertexSet = newVertexSet;
         }

         private double Tolerance { get; set; } = 0.0;

         private SortedSet<XYZ> VertexSet { get; set; } = null;
      }

      // stores all faces from the face set which will be built
      private TessellatedShapeBuilder TessellatedShapeBuilder { get; set; } = null;

      /// <summary>
      /// A set of "disjoint" XYZ.  This allows us to "look up" an XYZ value and get 
      /// the fuzzy equivalent.
      /// </summary>
      private IFCFuzzyXYZSet TessellatedFaceVertices { get; set; } = null;

      // Stores the current face being input. After the face will be
      // completely set, it will be inserted into the resident shape builder.
      private IList<IList<XYZ>> TessellatedFaceBoundary { get; set; } = null;

      /// <summary>
      /// Stores the one outer boundary for a facet that has issues that may be
      /// healed by splitting into triangles.  This is currently limited to one
      /// quadrilateral.
      /// </summary>
      public IList<XYZ> DelayedFaceBoundary { get; set; } = null;

      /// <summary>
      /// If this is true, then it is possible to triangulate bad boundary data later. 
      /// </summary>
      public bool CanProcessDelayedFaceBoundary { get; set; } = false;


      /// <summary>
      /// The number of successfully created faces so far, not including extra faces from
      /// potential triangulation.
      /// </summary>
      public int CreatedFacesCount { get; protected set; } = 0;

      /// <summary>
      /// The number of extra faces created, generally as a result of triangulation.
      /// </summary>
      public int ExtraCreatedFacesCount { get; protected set; } = 0;
      
      // The target geometry being created.  This may affect tolerances used to include or exclude vertices that are very 
      // close to one another, or potentially degenerate faces.
      public TessellatedShapeBuilderTarget TargetGeometry { get; private set; } = TessellatedShapeBuilderTarget.AnyGeometry;


      // The fallback geometry that will be created if we can't make the target geometry.
      public TessellatedShapeBuilderFallback FallbackGeometry { get; private set; } = TessellatedShapeBuilderFallback.Mesh;
 
      public TessellatedShapeBuilderScope(IFCImportShapeEditScope container)
          : base(container)
      {
         IFCImportShapeEditScope.BuildPreferenceType BuildPreference = container.BuildPreference;
         if (BuildPreference == IFCImportShapeEditScope.BuildPreferenceType.ForceSolid)
         {
            SetTargetAndFallbackGeometry(TessellatedShapeBuilderTarget.Solid, TessellatedShapeBuilderFallback.Abort);
         }
         else if (BuildPreference == IFCImportShapeEditScope.BuildPreferenceType.AnyMesh)
         {
            SetTargetAndFallbackGeometry(TessellatedShapeBuilderTarget.Mesh, TessellatedShapeBuilderFallback.Salvage);
         }
         else if (BuildPreference == IFCImportShapeEditScope.BuildPreferenceType.AnyGeometry)
         {
            SetTargetAndFallbackGeometry(TessellatedShapeBuilderTarget.AnyGeometry, TessellatedShapeBuilderFallback.Mesh);
         }
      }

      /// <summary>
      /// Reset the number of created faces to 0.
      /// </summary>
      public void ResetCreatedFacesCount()
      {
         CreatedFacesCount = 0;
         ExtraCreatedFacesCount = 0;
      }

      /// <summary>
      /// Set the target and fallback geometry for this scope.
      /// </summary>
      /// <param name="targetGeometry">The target geometry.</param>
      /// <param name="fallbackGeometry">The fallback geometry.</param>
      /// <remarks>This should not be directly called, but instead set with the IFCTargetSetter and the "using" scope.</remarks>
      public void SetTargetAndFallbackGeometry(TessellatedShapeBuilderTarget targetGeometry, TessellatedShapeBuilderFallback fallbackGeometry)
      {
         TargetGeometry = targetGeometry;
         FallbackGeometry = fallbackGeometry;
      }

      /// <summary>
      /// Start collecting faces to create a BRep solid.
      /// </summary>
      public override void StartCollectingFaceSet(BRepType brepType = BRepType.OpenShell)
      {
         if (TessellatedShapeBuilder == null)
            TessellatedShapeBuilder = new TessellatedShapeBuilder();

         TessellatedShapeBuilder.OpenConnectedFaceSet(false);
         ResetCreatedFacesCount();

         if (TessellatedFaceVertices != null)
            TessellatedFaceVertices.Clear();

         if (TessellatedFaceBoundary != null)
            TessellatedFaceBoundary.Clear();

         FaceMaterialId = ElementId.InvalidElementId;
      }

      private double GetVertexTolerance()
      {
         // Note that this tolerance is slightly larger than required, as it is a cube instead of a
         // sphere of equivalence.  In the case of AnyGeometry, we resort to the Solid tolerance as we are
         // generally trying to create Solids over Meshes.
         return (TargetGeometry == TessellatedShapeBuilderTarget.Mesh) ?
            MathUtil.Eps() : IFCImportFile.TheFile.ShortCurveTolerance;
      }

      /// <summary>
      /// Stop collecting faces to create a BRep solid.
      /// </summary>
      public void StopCollectingFaceSet()
      {
         if (TessellatedShapeBuilder == null)
            throw new InvalidOperationException("StartCollectingFaceSet has not been called.");

         TessellatedShapeBuilder.CloseConnectedFaceSet();

         if (TessellatedFaceBoundary != null)
            TessellatedFaceBoundary.Clear();

         if (TessellatedFaceVertices != null)
            TessellatedFaceVertices.Clear();

         FaceMaterialId = ElementId.InvalidElementId;
      }

      /// <summary>
      /// Start collecting edges for a face to create a BRep solid.
      /// </summary>
      /// <param name="materialId">The material id of the face.</param>
      /// <param name="canTriangulate">Whether we can delay processing bad boundary data.</param>
      public void StartCollectingFace(ElementId materialId, bool canTriangulate)
      {
         if (TessellatedShapeBuilder == null)
            throw new InvalidOperationException("StartCollectingFaceSet has not been called.");

         if (TessellatedFaceBoundary == null)
            TessellatedFaceBoundary = new List<IList<XYZ>>();
         else
            TessellatedFaceBoundary.Clear();

         if (TessellatedFaceVertices == null)
         {
            TessellatedFaceVertices = new IFCFuzzyXYZSet(GetVertexTolerance());
         }

         DelayedFaceBoundary = null;
         CanProcessDelayedFaceBoundary = canTriangulate;
         FaceMaterialId = materialId;
      }

      private void AddFaceToTessellatedShapeBuilder(TessellatedFace theFace, bool extraFace)
      {
         TessellatedShapeBuilder.AddFace(theFace);
         TessellatedFaceBoundary.Clear();
         FaceMaterialId = ElementId.InvalidElementId;
         if (extraFace)
            ExtraCreatedFacesCount++;
         else
            CreatedFacesCount++;
      }

      /// <summary>
      /// Stop collecting edges for a face to create a BRep solid.
      /// </summary>
      /// <param name="addFace">If true, adds the face, otherwise aborts.</param>
      public void StopCollectingFace(bool addFace, bool isExtraFace)
      {
         if (TessellatedShapeBuilder == null || TessellatedFaceBoundary == null)
            throw new InvalidOperationException("StartCollectingFace has not been called.");

         if (addFace)
         {
            TessellatedFace theFace = new TessellatedFace(TessellatedFaceBoundary, FaceMaterialId);
            AddFaceToTessellatedShapeBuilder(theFace, isExtraFace);
         }
         else
         {
            AbortCurrentFace();
         }
      }

      /// <summary>
      /// Check if we have started building a face.
      /// </summary>
      /// <returns>True if we have collected at least one face boundary, false otherwise.
      public bool HaveActiveFace()
      {
         return (TessellatedFaceBoundary != null && TessellatedFaceBoundary.Count > 0);
      }

      /// <summary>
      /// Remove the current invalid face from the list of faces to create a BRep solid.
      /// </summary>
      override public void AbortCurrentFace()
      {
         if (TessellatedFaceBoundary != null)
            TessellatedFaceBoundary.Clear();

         FaceMaterialId = ElementId.InvalidElementId;
      }

      /// <summary>
      /// Add one loop of vertices that will define a boundary loop of the current face.
      /// </summary>
      /// <param name="id">The id of the IFCEntity, for error reporting.</param>
      /// <param name="loopVertices">The list of vertices.</param>
      /// <returns>True if the operation succeeded, false oherwise.</returns>
      public bool AddLoopVertices(int id, List<XYZ> loopVertices)
      {
         int vertexCount = (loopVertices == null) ? 0 : loopVertices.Count;
         if (vertexCount < 3)
         {
            Importer.TheLog.LogComment(id, "Too few distinct loop vertices, ignoring.", false);
            return false;
         }

         List<XYZ> adjustedLoopVertices = null;
         IList<Tuple<int,int>> interiorLoops = null;

         int numOuterCreated = 0;

         bool succeeded = false;
         for (int pass = 0; pass < 2 && !succeeded; pass++)
         {
            // If we have AnyGeometry as a target, we are using Solid tolerances on a first pass.
            // If that would fail, try again using Mesh tolerances.
            if (pass == 1 && !RevertToMeshIfPossible())
               break;
         
            succeeded = true;

            // numOuterCreated is the size of the main "outer" loop after removing duplicates
            // and self-intersecting loops.  In all valid cases, numOuterCreated = numTotalCreated.
            numOuterCreated = 0;

            // The total number of non-duplicate loops.  This can differ if we are trying to create
            // a solid vs. a mesh.
            int numTotalCreated = 0;

            // The vertices of the main (presumably outer) loop.
            adjustedLoopVertices = new List<XYZ>();

            // The list of vertices of the self-intersecting loops.
            // Note that we will check that the self-interecting loops do not themselves self-intersect.
            interiorLoops = new List<Tuple<int, int>>();
            int lastInteriorLoopIndex = -1;

            IDictionary<XYZ, int> createdVertices = 
               new SortedDictionary<XYZ, int>(new IFCXYZFuzzyComparer(GetVertexTolerance()));

            for (int ii = 0; ii < vertexCount; ii++)
            {
               XYZ loopVertex = loopVertices[ii];

               int createdVertexIndex = -1;
               if (createdVertices.TryGetValue(loopVertex, out createdVertexIndex))
               {
                  // We will allow the first and last point to be equivalent, or the current and last point.  Otherwise we will throw.
                  if (((createdVertexIndex == 0) && (ii == vertexCount - 1)) || (createdVertexIndex == numTotalCreated - 1))
                     continue;

                  // If we have a real self-intersection, mark the loop created by the intersection
                  // for removal later.
                  if (loopVertex.DistanceTo(loopVertices[createdVertexIndex]) < MathUtil.SmallGap())
                  {
                     if (lastInteriorLoopIndex > createdVertexIndex)
                     {
                        // The interior loops overlap; this is probably too much to try to fix.
                        succeeded = false;
                        break;
                     }
                     // Sorted in reverse order so we can more easily create the interior loops later.
                     int numToRemove = ii - createdVertexIndex;
                     interiorLoops.Insert(0, Tuple.Create(createdVertexIndex, numToRemove));
                     lastInteriorLoopIndex = ii;
                     numOuterCreated -= numToRemove;
                     continue;
                  }

                  // Note that if pass == 1, CanRevertToMesh will be false.
                  if (!CanRevertToMesh())
                     Importer.TheLog.LogWarning(id, "Loop is self-intersecting, truncating.", false);
                  succeeded = false;
                  break;
               }

               XYZ adjustedXYZ = TessellatedFaceVertices.FindOrAdd(loopVertex);
                  
               adjustedLoopVertices.Add(adjustedXYZ);
               createdVertices[adjustedXYZ] = numTotalCreated;
               numTotalCreated++;
               numOuterCreated++;
            }

            if (numOuterCreated < 3)
               succeeded = false;
         }

         // Checking start and end points should be covered above.
         if (numOuterCreated < 3)
         {
            Importer.TheLog.LogComment(id, "Loop has less than 3 distinct vertices, ignoring.", false);
            return false;
         }

         // Remove the interior loops from the loop boundary, in reverse order, and add them
         // to the tessellated face boundary.
         foreach (Tuple<int, int> interiorLoop in interiorLoops)
         {
            int startIndex = interiorLoop.Item1;
            int count = interiorLoop.Item2;
            if (count >= 3)
               TessellatedFaceBoundary.Add(loopVertices.GetRange(startIndex, count));
            if (startIndex + count > adjustedLoopVertices.Count)
            {
               count = adjustedLoopVertices.Count - startIndex;
            }
            adjustedLoopVertices.RemoveRange(startIndex, count);
         }

         if (interiorLoops.Count > 0)
            Importer.TheLog.LogWarning(id, "Loop is self-intersecting, fixing.", false);
         
         TessellatedFaceBoundary.Add(adjustedLoopVertices);
         return true;
      }

      private void ClearTessellatedShapeBuilder()
      {
         TessellatedShapeBuilder.Clear();
         CreatedFacesCount = 0;
      }

      /// <summary>
      /// Create a geometry object(s) described by stored face sets, if possible.
      /// Usually a single-element IList conatining either Solid or Mesh is returned.
      /// A two-elemant IList containing a Solid as the 1st element and a Mesh as
      /// the 2nd is returned if while building multiple face sets, a fallback
      /// was used for some but not all sets.
      /// </summary>
      /// <returns>The IList created, or null. The IList can contain a Solid and/or a Mesh.
      /// If Solid is present, it always the 1st element.</returns>
      private IList<GeometryObject> CreateGeometryObjects(string guid,
         out bool hasInvalidData, out TessellatedShapeBuilderOutcome outcome)
      {
         try
         {
            TessellatedShapeBuilder.CloseConnectedFaceSet();

            // The OwnerInfo is currently unused; the value doesn't really matter.
            TessellatedShapeBuilder.LogString = IFCImportFile.TheFileName;
            TessellatedShapeBuilder.LogInteger = IFCImportFile.TheBrepCounter;
            TessellatedShapeBuilder.OwnerInfo = guid != null ? guid : "Temporary Element";

            TessellatedShapeBuilder.Target = TargetGeometry;
            TessellatedShapeBuilder.Fallback = FallbackGeometry;
            TessellatedShapeBuilder.GraphicsStyleId = GraphicsStyleId;

            TessellatedShapeBuilder.Build();

            TessellatedShapeBuilderResult result = TessellatedShapeBuilder.GetBuildResult();

            // It is important that we clear the TSB after we build above, otherwise we will "collect" geometries
            // in the DirectShape and create huge files with redundant data.
            ClearTessellatedShapeBuilder();
            hasInvalidData = result.HasInvalidData;
            outcome = result.Outcome;
            return result.GetGeometricalObjects();
         }
         catch (Exception ex)
         {
            Importer.TheLog.LogError(CreatorId(), ex.Message, false);

            ClearTessellatedShapeBuilder();
            hasInvalidData = true;
            outcome = TessellatedShapeBuilderOutcome.Nothing;
            return null;
         }
      }


      /// <summary>
      /// Create a closed Solid if possible. If the face sets have unusable faces
      /// or describe an open Solid, then nothing is created.
      /// </summary>
      /// <returns>The Solid created, or null.</returns>
      private IList<GeometryObject> CreateClosedSolid(string guid)
      {
         if (TargetGeometry != TessellatedShapeBuilderTarget.Solid || FallbackGeometry != TessellatedShapeBuilderFallback.Abort)
            throw new ArgumentException("CreateClosedSolid expects TessellatedShapeBuilderTarget.Solid and TessellatedShapeBuilderFallback.Abort.");

         bool invalidData;
         TessellatedShapeBuilderOutcome outcome;
         IList<GeometryObject> geomObjs = CreateGeometryObjects(guid, out invalidData, out outcome);

         bool createdClosedSolid = (outcome == TessellatedShapeBuilderOutcome.Solid);
         if (!createdClosedSolid)
            Importer.TheLog.LogWarning(CreatorId(), "Couldn't create closed solid.", false);

         if (!createdClosedSolid || geomObjs == null || geomObjs.Count != 1 || geomObjs[0] == null)
            return new List<GeometryObject>();

         if (geomObjs[0] is Solid)
            return geomObjs;

         // TessellatedShapeBuilder is only allowed to return a Solid, or nothing in this case.  If it returns something else, throw.
         throw new InvalidOperationException("Unexpected object was created");
      }

      /// <summary>
      /// Indicates whether we are required to create a Solid.
      /// </summary>
      /// <returns>True if we are required to create a Solid, false otherwise.</returns>
      public bool MustCreateSolid()
      {
         return (TargetGeometry == TessellatedShapeBuilderTarget.Solid &&
             FallbackGeometry == TessellatedShapeBuilderFallback.Abort);
      }

      /// <summary>
      /// Indicates whether we are attempting to create a Solid as our primary target.
      /// </summary>
      /// <returns>True if we are first trying to create a Solid, false otherwise.</returns>
      public bool TryToCreateSolid()
      {
         return (TargetGeometry == TessellatedShapeBuilderTarget.AnyGeometry ||
             TargetGeometry == TessellatedShapeBuilderTarget.Solid);
      }

      /// <summary>
      /// If possible, create a Mesh representing all faces in all face sets.</summary>
      /// <returns>null or an IList containing 1 or more GeometryObjects of type Mesh.</returns>
      /// <remarks>Usually a single-element IList is returned, but multiple GeometryObjects may be created.</remarks>
      private IList<GeometryObject> CreateMesh(string guid)
      {
         if (TargetGeometry != TessellatedShapeBuilderTarget.Mesh || FallbackGeometry != TessellatedShapeBuilderFallback.Salvage)
            throw new ArgumentException("CreateMesh expects TessellatedShapeBuilderTarget.Mesh and TessellatedShapeBuilderFallback.Salvage.");

         bool invalidData;
         TessellatedShapeBuilderOutcome outcome;
         IList<GeometryObject> geomObjects = CreateGeometryObjects(guid, out invalidData, out outcome);

         if(invalidData)
            Importer.TheLog.LogWarning(CreatorId(), "Couldn't create mesh.", false);

         return geomObjects;
      }

      /// <summary>
      /// If possible, create a Solid representing all faces in all face sets. If a Solid can't be created,
      /// then a Mesh is created instead.</summary>
      /// <returns>null or an IList containing 1 or more GeometryObjects of type Solid or Mesh.</returns>
      /// <remarks>Usually a single-element IList is returned, but multiple GeometryObjects may be created.</remarks>
      private IList<GeometryObject> CreateSolidOrMesh(string guid)
      {
         if (TargetGeometry != TessellatedShapeBuilderTarget.AnyGeometry || FallbackGeometry != TessellatedShapeBuilderFallback.Mesh)
            throw new ArgumentException("CreateSolidOrMesh expects TessellatedShapeBuilderTarget.AnyGeometry and TessellatedShapeBuilderFallback.Mesh.");

         bool invalidData;
         TessellatedShapeBuilderOutcome outcome;
         IList<GeometryObject> geomObjects = CreateGeometryObjects(guid, out invalidData, out outcome);

         if (invalidData)
            Importer.TheLog.LogWarning(CreatorId(), "Couldn't create solid or mesh.", false);

         return geomObjects;
      }

      /// <summary>
      /// Create geometry with the TessellatedShapeBuilder based on already existing settings.
      /// </summary>
      /// <param name="guid">The Guid associated with the geometry.</param>
      /// <returns>A list of GeometryObjects, possibly empty.</returns>
      public override IList<GeometryObject> CreateGeometry(string guid)
      {
         if (TargetGeometry == TessellatedShapeBuilderTarget.AnyGeometry && FallbackGeometry == TessellatedShapeBuilderFallback.Mesh)
            return CreateSolidOrMesh(guid);

         if (TargetGeometry == TessellatedShapeBuilderTarget.Solid && FallbackGeometry == TessellatedShapeBuilderFallback.Abort)
            return CreateClosedSolid(guid);

         if (TargetGeometry == TessellatedShapeBuilderTarget.Mesh && FallbackGeometry == TessellatedShapeBuilderFallback.Salvage)
            return CreateMesh(guid);

         throw new ArgumentException("Unhandled TessellatedShapeBuilderTarget and TessellatedShapeBuilderFallback for CreateGeometry.");
      }

      // End temporary classes for holding BRep information.
      
      /// <summary>
      /// Indicates if the geometry can be created as a mesh as a fallback.
      /// </summary>
      /// <returns>True if it can be.</returns>
      public bool CanRevertToMesh()
      {
         return FallbackGeometry == TessellatedShapeBuilderFallback.Mesh;
      }

      /// <summary>
      /// Revert to using a mesh representation if that's allowed.
      /// </summary>
      /// <returns>True if the change is made.</returns>
      public bool RevertToMeshIfPossible()
      {
         // Note that CanRevertToMesh() is redundant, but a trivial enough check.
         if (!CanRevertToMesh())
            return false;

         SetTargetAndFallbackGeometry(TessellatedShapeBuilderTarget.Mesh, TessellatedShapeBuilderFallback.Salvage);

         // We also need to reset the Comparer for TessellatedFaceVertices to a new tolerance.
         // Note that since we are always lowering the tolerance, there should be no concern that 
         // previous entries would disappear.  That isn't always true; see the remarks of
         // IFCFuzzyXYZSet.
         TessellatedFaceVertices.ResetTolerance(GetVertexTolerance());
         return true;
      }
   }
}