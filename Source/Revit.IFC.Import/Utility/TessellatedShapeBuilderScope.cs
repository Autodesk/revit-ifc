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
using UnitSystem = Autodesk.Revit.DB.DisplayUnit;
using UnitName = Autodesk.Revit.DB.DisplayUnitType;

namespace Revit.IFC.Import.Utility
{
   /// <summary>
   /// Provides methods to manage creation of DirectShape elements using TessellatedShapeBuilder
   /// </summary>
   public class TessellatedShapeBuilderScope : BuilderScope
   {
      // stores all faces from the face set which will be built
      private TessellatedShapeBuilder m_TessellatedShapeBuilder = null;

      /// <summary>
      /// A map of IFCFuzzyXYZ to XYZ values.  In practice, the two values will be the same, but this allows us to
      /// "look up" an XYZ value and get the fuzzy equivalent.  Internally, this is represented by a SortedDictionary.
      /// </summary>
      private IDictionary<IFCFuzzyXYZ, XYZ> m_TessellatedFaceVertices = null;

      // stores the current face being input. After the face will be
      // completely set, it will be inserted into the resident shape builder.
      private IList<IList<XYZ>> m_TessellatedFaceBoundary = null;

      // The target geometry being created.  This may affect tolerances used to include or exclude vertices that are very close to one another,
      // or potentially degenerate faces.
      private TessellatedShapeBuilderTarget m_TargetGeometry = TessellatedShapeBuilderTarget.AnyGeometry;

      // The fallback geometry being created.
      private TessellatedShapeBuilderFallback m_FallbackGeometry = TessellatedShapeBuilderFallback.Mesh;

      // The number of created faces (so far).
      private int m_CreatedFacesCount = 0;

      /// <summary>
      /// The number of successfully created faces so far.
      /// </summary>
      public int CreatedFacesCount
      {
         get { return m_CreatedFacesCount; }
         protected set { m_CreatedFacesCount = value; }
      }

      // The target geometry being created.  This may affect tolerances used to include or exclude vertices that are very 
      // close to one another, or potentially degenerate faces.
      public TessellatedShapeBuilderTarget TargetGeometry
      {
         get { return m_TargetGeometry; }
         private set
         {
            m_TargetGeometry = value;
            SetIFCFuzzyXYZEpsilon();
         }
      }

      // The fallback geometry that will be created if we can't make the target geometry.
      public TessellatedShapeBuilderFallback FallbackGeometry
      {
         get { return m_FallbackGeometry; }
         private set { m_FallbackGeometry = value; }
      }

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
      public void StartCollectingFaceSet()
      {
         if (m_TessellatedShapeBuilder == null)
            m_TessellatedShapeBuilder = new TessellatedShapeBuilder();

         m_TessellatedShapeBuilder.OpenConnectedFaceSet(false);
         ResetCreatedFacesCount();

         if (m_TessellatedFaceVertices != null)
            m_TessellatedFaceVertices.Clear();

         if (m_TessellatedFaceBoundary != null)
            m_TessellatedFaceBoundary.Clear();

         FaceMaterialId = ElementId.InvalidElementId;
      }

      /// <summary>
      /// Stop collecting faces to create a BRep solid.
      /// </summary>
      public void StopCollectingFaceSet()
      {
         if (m_TessellatedShapeBuilder == null)
            throw new InvalidOperationException("StartCollectingFaceSet has not been called.");

         m_TessellatedShapeBuilder.CloseConnectedFaceSet();

         if (m_TessellatedFaceBoundary != null)
            m_TessellatedFaceBoundary.Clear();

         if (m_TessellatedFaceVertices != null)
            m_TessellatedFaceVertices.Clear();

         FaceMaterialId = ElementId.InvalidElementId;
      }

      /// <summary>
      /// Start collecting edges for a face to create a BRep solid.
      /// </summary>
      public void StartCollectingFace(ElementId materialId)
      {
         if (m_TessellatedShapeBuilder == null)
            throw new InvalidOperationException("StartCollectingFaceSet has not been called.");

         if (m_TessellatedFaceBoundary == null)
            m_TessellatedFaceBoundary = new List<IList<XYZ>>();
         else
            m_TessellatedFaceBoundary.Clear();

         if (m_TessellatedFaceVertices == null)
            m_TessellatedFaceVertices = new SortedDictionary<IFCFuzzyXYZ, XYZ>();

         FaceMaterialId = materialId;
      }

      private void AddFaceToTessellatedShapeBuilder(TessellatedFace theFace)
      {
         m_TessellatedShapeBuilder.AddFace(theFace);
         m_TessellatedFaceBoundary.Clear();
         FaceMaterialId = ElementId.InvalidElementId;
         CreatedFacesCount++;
      }

      /// <summary>
      /// Stop collecting edges for a face to create a BRep solid.
      /// </summary>
      public void StopCollectingFace()
      {
         if (m_TessellatedShapeBuilder == null || m_TessellatedFaceBoundary == null)
            throw new InvalidOperationException("StartCollectingFace has not been called.");

         TessellatedFace theFace = new TessellatedFace(m_TessellatedFaceBoundary, FaceMaterialId);
         AddFaceToTessellatedShapeBuilder(theFace);
      }

      /// <summary>
      /// Check if we have started building a face.
      /// </summary>
      /// <returns>True if we have collected at least one face boundary, false otherwise.
      public bool HaveActiveFace()
      {
         return (m_TessellatedFaceBoundary != null && m_TessellatedFaceBoundary.Count > 0);
      }

      /// <summary>
      /// Remove the current invalid face from the list of faces to create a BRep solid.
      /// </summary>
      override public void AbortCurrentFace()
      {
         if (m_TessellatedFaceBoundary != null)
            m_TessellatedFaceBoundary.Clear();

         FaceMaterialId = ElementId.InvalidElementId;
      }

      /// <summary>
      /// Add one loop of vertices that will define a boundary loop of the current face.
      /// </summary>
      /// <param name="id">The id of the IFCEntity, for error reporting.</param>
      /// <param name="loopVertices">The list of vertices.</param>
      /// <returns>True if the operation succeeded, false oherwise.</returns>
      public bool AddLoopVertices(int id, IList<XYZ> loopVertices)
      {
         int vertexCount = (loopVertices == null) ? 0 : loopVertices.Count;
         if (vertexCount < 3)
         {
            Importer.TheLog.LogComment(id, "Too few distinct loop vertices, ignoring.", false);
            return false;
         }

         IList<XYZ> adjustedLoopVertices = new List<XYZ>();
         IDictionary<IFCFuzzyXYZ, int> createdVertices = new SortedDictionary<IFCFuzzyXYZ, int>();

         int numCreated = 0;
         for (int ii = 0; ii < vertexCount; ii++)
         {
            IFCFuzzyXYZ fuzzyXYZ = new IFCFuzzyXYZ(loopVertices[ii]);

            int createdVertexIndex = -1;
            if (createdVertices.TryGetValue(fuzzyXYZ, out createdVertexIndex))
            {
               // We will allow the first and last point to be equivalent, or the current and last point.  Otherwise we will throw.
               if (((createdVertexIndex == 0) && (ii == vertexCount - 1)) || (createdVertexIndex == numCreated - 1))
                  continue;

               Importer.TheLog.LogComment(id, "Loop is self-intersecting, ignoring.", false);
               return false;
            }

            XYZ adjustedXYZ;
            if (!m_TessellatedFaceVertices.TryGetValue(fuzzyXYZ, out adjustedXYZ))
               adjustedXYZ = m_TessellatedFaceVertices[fuzzyXYZ] = loopVertices[ii];

            adjustedLoopVertices.Add(adjustedXYZ);
            createdVertices[new IFCFuzzyXYZ(adjustedXYZ)] = numCreated;
            numCreated++;
         }

         // Checking start and end points should be covered above.
         if (numCreated < 3)
         {
            Importer.TheLog.LogComment(id, "Loop has less than 3 distinct vertices, ignoring.", false);
            return false;
         }

         m_TessellatedFaceBoundary.Add(adjustedLoopVertices);
         return true;
      }

      private void ClearTessellatedShapeBuilder()
      {
         m_TessellatedShapeBuilder.Clear();
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
            m_TessellatedShapeBuilder.CloseConnectedFaceSet();

            // The OwnerInfo is currently unused; the value doesn't really matter.
            m_TessellatedShapeBuilder.LogString = IFCImportFile.TheFileName;
            m_TessellatedShapeBuilder.LogInteger = IFCImportFile.TheBrepCounter;
            m_TessellatedShapeBuilder.OwnerInfo = guid != null ? guid : "Temporary Element";

            m_TessellatedShapeBuilder.Target = TargetGeometry;
            m_TessellatedShapeBuilder.Fallback = FallbackGeometry;
            m_TessellatedShapeBuilder.GraphicsStyleId = GraphicsStyleId;

            m_TessellatedShapeBuilder.Build();

            TessellatedShapeBuilderResult result = m_TessellatedShapeBuilder.GetBuildResult();

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

         // We won't log a message here as we expect the receiver to warn as necessary.
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

         // We won't log a message here as we expect the receiver to warn as necessary.
         return geomObjects;
      }

      /// <summary>
      /// Create geometry with the TessellatedShapeBuilder based on already existing settings.
      /// </summary>
      /// <param name="guid">The Guid associated with the geometry.</param>
      /// <returns>A list of GeometryObjects, possibly empty.</returns>
      public IList<GeometryObject> CreateGeometry(string guid)
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
      private void SetIFCFuzzyXYZEpsilon()
      {
         // Note that this tolerance is slightly larger than required, as it is a cube instead of a
         // sphere of equivalence.  In the case of AnyGeometry, we resort to the Solid tolerance as we are
         // generally trying to create Solids over Meshes.
         IFCFuzzyXYZ.IFCFuzzyXYZEpsilon = (TargetGeometry == TessellatedShapeBuilderTarget.Mesh) ?
             IFCImportFile.TheFile.Document.Application.VertexTolerance :
             IFCImportFile.TheFile.Document.Application.ShortCurveTolerance;
      }

      /// <summary>
      /// Indicates if the geometry can be created as a mesh instead
      /// </summary>
      /// <returns></returns>
      public bool CanRevertToMesh()
      {
         return TargetGeometry == TessellatedShapeBuilderTarget.AnyGeometry && FallbackGeometry == TessellatedShapeBuilderFallback.Mesh;
      }
   }
}