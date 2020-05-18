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
using Revit.IFC.Import.Enums;

namespace Revit.IFC.Import.Utility
{
   /// <summary>
   /// Provides methods to manage creation of DirectShape elements.
   /// </summary>
   public class IFCImportShapeEditScope : IDisposable
   {
      private Document m_Document = null;

      private IFCProduct m_Creator = null;

      private IFCRepresentation m_ContainingRepresentation = null;

      private ElementId m_GraphicsStyleId = ElementId.InvalidElementId;

      private ElementId m_CategoryId = ElementId.InvalidElementId;

      // The names of the associated IfcPresentationLayerWithStyles
      private ISet<string> m_PresentationLayerNames = null;

      // A stack of material element id from IFCStyledItems and IFCPresentationLayerWithStyles.  The "current" material id should generally be used.
      private IList<ElementId> m_MaterialIdList = null;

      // store all curves for 2D plan representation.  
      private ViewShapeBuilder m_ViewShapeBuilder = null;

      private BuilderScope m_BuilderScope = null;

      // Prevent Instances if shape will be voided
      private bool m_PreventInstances = false;

      /// <summary>
      /// Returns the builder scope which contains the ShapeBuilder that is used to create the geometry
      /// </summary>
      public bool PreventInstances
      {
         get { return m_PreventInstances; }
         set { m_PreventInstances = value; }
      }

      /// <summary>
      /// Returns the builder scope which contains the ShapeBuilder that is used to create the geometry
      /// </summary>
      public BuilderScope BuilderScope
      {
         get { return m_BuilderScope; }
         set { m_BuilderScope = value; }
      }

      private IList<ElementId> MaterialIdList
      {
         get
         {
            if (m_MaterialIdList == null)
               m_MaterialIdList = new List<ElementId>();
            return m_MaterialIdList;
         }
      }

      private IFCShapeBuilderType m_BuilderType = IFCShapeBuilderType.Unknown;

      /// <summary>
      /// Returns the type of the shape builder that is used to create the geometry
      /// </summary>
      public IFCShapeBuilderType BuilderType
      {
         get { return m_BuilderType; }
         set { m_BuilderType = value; }
      }

      /// <summary>
      /// The id of the associated graphics style, if any.
      /// </summary>
      public ElementId GraphicsStyleId
      {
         get { return m_GraphicsStyleId; }
         set { m_GraphicsStyleId = value; }
      }

      /// <summary>
      /// The id of the associated category.
      /// </summary>
      public ElementId CategoryId
      {
         get { return m_CategoryId; }
         set { m_CategoryId = value; }
      }

      private void PushMaterialId(ElementId materialId)
      {
         MaterialIdList.Add(materialId);
      }

      private void PopMaterialId()
      {
         int count = MaterialIdList.Count;
         if (count > 0)
            MaterialIdList.RemoveAt(count - 1);
      }

      private BuildPreferenceType m_BuildPreference = BuildPreferenceType.AnyGeometry;

      /// <summary>
      /// Returns the BuildPreferenceType of the IFCImportShapeEditScope
      /// </summary>
      public BuildPreferenceType BuildPreference
      {
         get { return m_BuildPreference; }
         set { m_BuildPreference = value; }
      }

      /// <summary>
      /// The material id associated with the representation item currently being processed.
      /// </summary>
      /// <returns></returns>
      public ElementId GetCurrentMaterialId()
      {
         int count = MaterialIdList.Count;
         if (count == 0)
            return ElementId.InvalidElementId;
         return MaterialIdList[count - 1];
      }

      /// <summary>
      /// A class to responsibly set - and unset - ContainingRepresentation.  
      /// Intended to be used with the "using" keyword.
      /// </summary>
      public class IFCContainingRepresentationSetter : IDisposable
      {
         private IFCImportShapeEditScope m_Scope = null;
         private IFCRepresentation m_OldRepresentation = null;

         /// <summary>
         /// The constructor.
         /// </summary>
         /// <param name="scope">The associated shape edit scope.</param>
         /// <param name="item">The current styled item.</param>
         public IFCContainingRepresentationSetter(IFCImportShapeEditScope scope, IFCRepresentation containingRepresentation)
         {
            if (scope != null)
            {
               m_Scope = scope;
               m_OldRepresentation = scope.ContainingRepresentation;
               scope.ContainingRepresentation = containingRepresentation;
            }
         }

         #region IDisposable Members

         public void Dispose()
         {
            if (m_Scope != null)
               m_Scope.ContainingRepresentation = m_OldRepresentation;
         }

         #endregion
      }

      /// <summary>
      /// The class containing all of the IfcStyledItems currently active.
      /// </summary>
      public class IFCMaterialStack : IDisposable
      {
         private IFCImportShapeEditScope m_Scope = null;
         private ElementId m_MaterialElementId = ElementId.InvalidElementId;

         /// <summary>
         /// The constructor.
         /// </summary>
         /// <param name="scope">The associated shape edit scope.</param>
         /// <param name="item">The current styled item.</param>
         public IFCMaterialStack(IFCImportShapeEditScope scope, IFCStyledItem styledItem, IFCPresentationLayerAssignment layerAssignment)
         {
            m_Scope = scope;
            if (styledItem != null)
               m_MaterialElementId = styledItem.GetMaterialElementId(scope);
            else if (layerAssignment != null)
               m_MaterialElementId = layerAssignment.GetMaterialElementId(scope);

            if (m_MaterialElementId != ElementId.InvalidElementId)
               m_Scope.PushMaterialId(m_MaterialElementId);
         }

         #region IDisposable Members

         public void Dispose()
         {
            if (m_MaterialElementId != ElementId.InvalidElementId)
               m_Scope.PopMaterialId();
         }

         #endregion
      }

      /// <summary>
      /// The names of the presentation layers created in this scope.
      /// </summary>
      public ISet<string> PresentationLayerNames
      {
         get
         {
            if (m_PresentationLayerNames == null)
               m_PresentationLayerNames = new SortedSet<string>();
            return m_PresentationLayerNames;
         }
      }

      /// <summary>
      /// The document associated with this element.
      /// </summary>
      public Document Document
      {
         get { return m_Document; }
         protected set { m_Document = value; }
      }

      /// <summary>
      /// Get the top-level IFC entity associated with this shape.
      /// </summary>
      public IFCProduct Creator
      {
         get { return m_Creator; }
         protected set { m_Creator = value; }
      }

      /// <summary>
      /// The IFCRepresentation that contains the currently processed IFC entity.
      /// </summary>
      public IFCRepresentation ContainingRepresentation
      {
         get { return m_ContainingRepresentation; }
         protected set { m_ContainingRepresentation = value; }
      }

      protected IFCImportShapeEditScope()
      {

      }

      protected IFCImportShapeEditScope(Document doc, IFCProduct creator)
      {
         Document = doc;
         Creator = creator;
      }

      /// <summary>
      /// Create a new edit scope.  Intended to be used with the "using" keyword.
      /// </summary>
      /// <param name="doc">The import document.</param>
      /// <param name="action">The name of the current action.</param>
      /// <param name="creator">The entity being processed.</param>
      /// <returns>The new edit scope.</returns>
      static public IFCImportShapeEditScope Create(Document doc, IFCProduct creator)
      {
         return new IFCImportShapeEditScope(doc, creator);
      }

      /// <summary>
      /// Safely get the top-level IFC entity associated with this shape, if it exists, or -1 otherwise.
      /// </summary>
      public int CreatorId()
      {
         if (Creator != null)
            return Creator.Id;
         return -1;
      }

      /// <summary>
      /// Returns the type of the IFCRepresentation that contains the currently processed IFC entity, if set.
      /// </summary>
      /// <returns>The representation identifier, or Unhandled if there is none.</returns>
      public IFCRepresentationIdentifier GetContainingRepresentationType()
      {
         if (ContainingRepresentation == null)
            return IFCRepresentationIdentifier.Unhandled;
         return ContainingRepresentation.Identifier;
      }

      // End temporary classes for holding BRep information.
      private void SetIFCFuzzyXYZEpsilon()
      {
         // Note that this tolerance is slightly larger than required, as it is a cube instead of a
         // sphere of equivalence.  In the case of AnyGeoemtry, we resort to the Solid tolerance as we are
         // generally trying to create Solids over Meshes.
         IFCFuzzyXYZ.IFCFuzzyXYZEpsilon = GetShortSegmentTolerance();
      }

      /// <summary>
      /// Add a Solid to the current DirectShape element.
      /// </summary>
      /// <param name="solidInfo">The IFCSolidInfo class describing the solid.</param>
      public void AddGeometry(IFCSolidInfo solidInfo)
      {
         if (solidInfo == null || solidInfo.GeometryObject == null)
            return;

         solidInfo.RepresentationType = GetContainingRepresentationType();
         Creator.Solids.Add(solidInfo);
      }

      /// <summary>
      /// Add a curve to the Footprint reprensentation of the object in scope.
      /// </summary>
      /// <param name="curve">The curve.</param>
      public void AddFootprintCurve(Curve curve)
      {
         if (curve == null)
            return;

         Creator.FootprintCurves.Add(curve);
      }

      /// <summary>
      /// Indicates whether we are required to create a solid
      /// </summary>
      /// <returns>True if we are required to create a solid, false otherwise</returns>
      public bool MustCreateSolid()
      {
         if (BuilderType == IFCShapeBuilderType.TessellatedShapeBuilder)
         {
            TessellatedShapeBuilderScope bs = BuilderScope as TessellatedShapeBuilderScope;
            return (bs.TargetGeometry == TessellatedShapeBuilderTarget.Solid && bs.FallbackGeometry == TessellatedShapeBuilderFallback.Abort);
         }
         if (BuilderType == IFCShapeBuilderType.BrepBuilder)
         {
            // Currently for BrepBuilder, we hard code that BrepType is Solid, so this must return true
            return true;
         }

         return false;
      }

      /// <summary>
      /// Get the tolerance to be used when determining if a polygon has too short an edge
      /// for either solid or mesh output.</summary>
      /// <returns>The tolerance value.</returns>
      public double GetShortSegmentTolerance()
      {
         if (BuilderType != IFCShapeBuilderType.TessellatedShapeBuilder)
            return Document.Application.ShortCurveTolerance;

         TessellatedShapeBuilderScope bs = BuilderScope as TessellatedShapeBuilderScope;
         return (bs.TargetGeometry == TessellatedShapeBuilderTarget.Mesh) ?
            MathUtil.Eps() : Document.Application.ShortCurveTolerance;
      }

      /// <summary>
      /// Add curves to represent the plan view of the created object.
      /// </summary>
      /// <param name="curves">The list of curves, to be validated.</param>
      /// <param name="id">The id of the object being created, for error logging.</param>
      /// <returns>True if any curves were added to the plan view representation.</returns>
      public bool AddPlanViewCurves(IList<Curve> curves, int id)
      {
         m_ViewShapeBuilder = null;
         int numCurves = curves.Count;
         if (numCurves > 0)
         {
            m_ViewShapeBuilder = new ViewShapeBuilder(DirectShapeTargetViewType.Plan);

            // Ideally we'd form these curves into a CurveLoop and get the Plane of the CurveLoop.  However, there is no requirement
            // that the plan view curves form one contiguous loop.
            foreach (Curve curve in curves)
            {
               if (m_ViewShapeBuilder.ValidateCurve(curve))
                  m_ViewShapeBuilder.AddCurve(curve);
               else
               {
                  // We will move the origin to Z=0 if necessary, since the VSB requires all curves to be in the Z=0 plane.
                  // This only works if the curves are in a plane parallel to the Z=0 plane.
                  // NOTE: We could instead project the curves to the Z=0 plane, which could have the effect of changing their geometry.
                  // Until we see such cases, we will take the easier route here.
                  try
                  {
                     // If the end points aren't equal in Z, then the curve isn't parallel to Z.
                     bool isBound = curve.IsBound;
                     XYZ startPoint = isBound ? curve.GetEndPoint(0) : curve.Evaluate(0, false);
                     XYZ endPoint = isBound ? curve.GetEndPoint(1) : startPoint;
                     if (!MathUtil.IsAlmostEqual(startPoint.Z, endPoint.Z))
                        throw new InvalidOperationException("Non-planar curve in footprint representation.");

                     // Lines won't have a non-zero BasisZ value, so don't bother computing.
                     if (!(curve is Line))
                     {
                        Transform coordinatePlane = curve.ComputeDerivatives(0, true);
                        if (coordinatePlane != null && coordinatePlane.BasisZ != null && !coordinatePlane.BasisZ.IsZeroLength())
                        {
                           XYZ normalizedZ = coordinatePlane.BasisZ.Normalize();
                           if (!MathUtil.IsAlmostEqual(Math.Abs(normalizedZ.Z), 1.0))
                              throw new InvalidOperationException("Non-planar curve in footprint representation.");
                        }
                     }

                     // We expect startPoint.Z to be non-zero, otherwise ValidateCurve would have accepted the curve in the first place.
                     Transform offsetTransform = Transform.CreateTranslation(-startPoint.Z * XYZ.BasisZ);
                     Curve projectedCurve = curve.CreateTransformed(offsetTransform);

                     // We may have missed a case above - for example, a curve whose end points have the same Z value, and whose normal at the
                     // start point is in +/-Z, but is regardless non-planar.  ValidateCurve has a final chance to reject such curves here.
                     if (projectedCurve == null || !m_ViewShapeBuilder.ValidateCurve(projectedCurve))
                        throw new InvalidOperationException("Invalid curve in footprint representation.");
                     m_ViewShapeBuilder.AddCurve(projectedCurve);
                     continue;
                  }
                  catch
                  {
                  }

                  Importer.TheLog.LogError(id, "Invalid curve in FootPrint representation, ignoring.", false);
                  numCurves--;
               }
            }

            if (numCurves == 0)
               m_ViewShapeBuilder = null;
         }

         return (m_ViewShapeBuilder != null);
      }

      /// <summary>
      /// Set the plan view representation of the given DirectShape or DirectShapeType given the information created by AddPlanViewCurves.
      /// </summary>
      /// <param name="shape">The DirectShape or DirectShapeType.</param>
      public void SetPlanViewRep(Element shape)
      {
         if (m_ViewShapeBuilder != null)
         {
            if (shape is DirectShape)
            {
               DirectShape ds = shape as DirectShape;
               ds.SetShape(m_ViewShapeBuilder);
            }
            else if (shape is DirectShapeType)
            {
               DirectShapeType dst = shape as DirectShapeType;
               dst.SetShape(m_ViewShapeBuilder);
            }
            else
               throw new ArgumentException("SetPlanViewRep only works on DirectShape and DirectShapeType.");
         }
      }



      #region IDisposable Members

      public void Dispose()
      {
      }

      #endregion

      /// <summary>
      /// Initialize the builder that will be used to create the geometry
      /// </summary>
      /// <param name="builderType">The type of the builder</param>
      public BuilderScope InitializeBuilder(IFCShapeBuilderType builderType)
      {
         if (BuilderScope != null)
         {
            throw new InvalidOperationException("BuilderScope has already been initialized");
         }

         BuilderType = builderType;
         BuildPreference = BuildPreference;

         if (builderType == IFCShapeBuilderType.BrepBuilder)
         {
            BuilderScope = new BrepBuilderScope(this);
         }
         else if (builderType == IFCShapeBuilderType.TessellatedShapeBuilder)
         {
            BuilderScope = new TessellatedShapeBuilderScope(this);
         }

         SetIFCFuzzyXYZEpsilon();
         return BuilderScope;
      }

      /// <summary>
      /// A class that allows temporarily changing the BuildPreferenceType of IFCImportShapeEditScope
      /// </summary>
      public class BuildPreferenceSetter : IDisposable
      {
         BuildPreferenceType m_PreferenceType;
         IFCImportShapeEditScope m_Scope;

         public BuildPreferenceSetter(IFCImportShapeEditScope scope, BuildPreferenceType buildPreferenceType)
         {
            m_Scope = scope;
            m_PreferenceType = scope.BuildPreference;
            scope.BuildPreference = buildPreferenceType;
         }

         public void Dispose()
         {
            m_Scope.BuildPreference = m_PreferenceType;
         }
      }

      /// <summary>
      /// The type of geometry that can be built
      /// </summary>
      public enum BuildPreferenceType
      {
         ForceSolid,
         AnyMesh,
         AnyGeometry
      }
   }
}