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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Utility;
using Revit.IFC.Import.Enums;

namespace Revit.IFC.Import.Data
{
   public class IFCFace : IFCTopologicalRepresentationItem
   {
      /// <summary>
      /// Return the bounding loops of the face.
      /// </summary>
      public ISet<IFCFaceBound> Bounds { get; set; } = new HashSet<IFCFaceBound>();
      
      protected IFCFace()
      {
      }

      override protected void Process(IFCAnyHandle ifcFace)
      {
         base.Process(ifcFace);

         HashSet<IFCAnyHandle> ifcBounds =
             IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcFace, "Bounds");
         if (ifcBounds == null || ifcBounds.Count == 0)
            throw new InvalidOperationException("#" + ifcFace.StepId + ": no face boundaries, aborting.");

         foreach (IFCAnyHandle ifcBound in ifcBounds)
         {
            try
            {
               Bounds.Add(IFCFaceBound.ProcessIFCFaceBound(ifcBound));
            }
            catch
            {
               Importer.TheLog.LogWarning(ifcFace.StepId, "Invalid face boundary, ignoring", false);
            }
         }

         if (Bounds.Count == 0)
            throw new InvalidOperationException("#" + ifcFace.StepId + ": no face boundaries, aborting.");

         // Give warning if too many outer bounds.  We won't care how they are designated, regardless.
         bool hasOuter = false;
         foreach (IFCFaceBound faceBound in Bounds)
         {
            if (faceBound.IsOuter)
            {
               if (hasOuter)
               {
                  Importer.TheLog.LogWarning(ifcFace.StepId, "Too many outer boundary loops for IfcFace.", false);
                  break;
               }
               hasOuter = true;
            }
         }
      }

      /// <summary>
      /// Checks if the Face definition represents a non-empty boundary.
      /// </summary>
      /// <returns>True if the face contains any information.</returns>
      public bool IsEmpty()
      {
         if (Bounds == null)
            return true;

         foreach (IFCFaceBound bound in Bounds)
         {
            if (!bound.IsEmpty())
               return false;
         }

         return true;
      }

      private IList<List<XYZ>> CreateTriangulation(IList<XYZ> boundary)
      {
         if (boundary == null)
            return null;

         if (boundary.Count != 4)
            return null;

         IList<List<XYZ>> loops = new List<List<XYZ>>();

         int firstPoint = 0;
         Func<int, XYZ> boundaryPoint = index => boundary[(firstPoint + index) % 4];

         // Future TODO: replace this very simple method with Delauney triangulation or the
         // like.  Or better yet, improve the TessellatedShapeBuilder so that it isn't
         // necessary.
         // Want to ensure that either (1) the quadrilateral is convex or (2) we split at the
         // concave point.
         XYZ referenceCross = null;
         Tuple<int, int> posNegIndex = Tuple.Create(0, -1);
         int sum = 1;

         for (int ii = 0; ii < 4; ii++)
         {
            XYZ vector1 = (boundaryPoint(ii+3) - boundaryPoint(ii)).Normalize();
            XYZ vector2 = (boundaryPoint(ii+1) - boundaryPoint(ii)).Normalize();
            XYZ cross = vector1.CrossProduct(vector2);
            
            if (cross.IsZeroLength())
            {
               // This is a degenerate quadrilateral; return the triangle.
               loops.Add(new List<XYZ>() { boundaryPoint(ii+1), boundaryPoint(ii+2), boundaryPoint(ii+3) });
               return loops;
            }

            if (referenceCross == null)
            {
               referenceCross = cross;
               continue;
            }

            double dot = referenceCross.DotProduct(cross);
            sum += ((dot > 0) ? 1 : -1);
            posNegIndex = (dot > 0) ? Tuple.Create(ii, posNegIndex.Item2) : Tuple.Create(posNegIndex.Item1, ii);
         }

         firstPoint = (sum == -3) ? posNegIndex.Item1 : ((sum == 3) ? posNegIndex.Item2 : 0);
         
         loops.Add(new List<XYZ>() { boundaryPoint(0), boundaryPoint(1), boundaryPoint(2) });
         loops.Add(new List<XYZ>() { boundaryPoint(2), boundaryPoint(3), boundaryPoint(0) });
         return loops;
      }

      /// <summary>
      /// Create geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, 
         Transform scaledLcs, string guid)
      {
         if (shapeEditScope.BuilderType != IFCShapeBuilderType.TessellatedShapeBuilder)
            throw new InvalidOperationException("Currently BrepBuilder is only used to support IFCAdvancedFace");

         base.CreateShapeInternal(shapeEditScope, scaledLcs, guid);

         // we would only be in this code if we are not processing and IfcAdvancedBrep, since IfcAdvancedBrep must have IfcAdvancedFace
         if (shapeEditScope.BuilderScope == null)
         {
            throw new InvalidOperationException("BuilderScope has not been initialized");
         }
         TessellatedShapeBuilderScope tsBuilderScope = shapeEditScope.BuilderScope as TessellatedShapeBuilderScope;

         bool addFace = true;
         bool canTriangulate = (Bounds.Count == 1);
         ElementId materialId = GetMaterialElementId(shapeEditScope);

         // We can only really triangulate faces with one boundary with 4 vertices,
         // but we don't really know how many vertices the boundary has until later.
         // So this is just the first block.  Later, we can try to extend to generic
         // polygons.
         tsBuilderScope.StartCollectingFace(materialId, canTriangulate);
         
         foreach (IFCFaceBound faceBound in Bounds)
         {
            faceBound.CreateShape(shapeEditScope, scaledLcs, guid);

            // If we can't create the outer face boundary, we will abort the creation of this face.  
            // In that case, return, unless we can triangulate it.
            if (!tsBuilderScope.HaveActiveFace())
            {
               addFace = false;
               break;
            }
         }

         tsBuilderScope.StopCollectingFace(addFace, false);

         IList<List<XYZ>> delayedFaceBoundaries = CreateTriangulation(tsBuilderScope.DelayedFaceBoundary);
         if (delayedFaceBoundaries != null)
         {
            bool extraFace = false;
            foreach (List<XYZ> delayedBoundary in delayedFaceBoundaries)
            {
               bool addTriangulatedFace = true;
               tsBuilderScope.StartCollectingFace(GetMaterialElementId(shapeEditScope), false);
               if (!tsBuilderScope.AddLoopVertices(Id, delayedBoundary))
               {
                  Importer.TheLog.LogComment(Id, "Bounded loop plane is slightly non-planar, couldn't triangulate.", false);
                  addTriangulatedFace = false;
               }
               tsBuilderScope.StopCollectingFace(addTriangulatedFace, extraFace);
               extraFace = true;
            }
         }
      }

      protected IFCFace(IFCAnyHandle ifcFace)
      {
         Process(ifcFace);
      }

      /// <summary>
      /// Create an IFCFace object from a handle of type IfcFace.
      /// </summary>
      /// <param name="ifcFace">The IFC handle.</param>
      /// <returns>The IFCFace object.</returns>
      public static IFCFace ProcessIFCFace(IFCAnyHandle ifcFace)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcFace))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcFace);
            return null;
         }

         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcFace, IFCEntityType.IfcFaceSurface))
            return IFCFaceSurface.ProcessIFCFaceSurface(ifcFace);

         IFCEntity face;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcFace.StepId, out face))
            face = new IFCFace(ifcFace);
         return (face as IFCFace);
      }
   }
}