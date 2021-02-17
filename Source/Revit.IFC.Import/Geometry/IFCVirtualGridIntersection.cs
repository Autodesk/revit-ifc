//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
// Copyright (C) 2013-2020  Autodesk, Inc.
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
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Data;

namespace Revit.IFC.Import.Geometry
{
   /// <summary>
   /// Represents an IfcVirtualGridIntersection entity, used in 
   /// IfcGridLocalPlacement to provide positioning.
   /// </summary>
   public class IFCVirtualGridIntersection : IFCEntity
   {
      /// <summary>
      /// The calculated LCS determined by this entity.
      /// </summary>
      public Transform LocalCoordinateSystem { get; protected set; } = null;

      /// <summary>
      /// The 2 intersecting axes that help determine the location point of the 
      /// placement.
      /// </summary>
      public IList<IFCGridAxis> IntersectingAxes { get; protected set; } = null;

      /// <summary>
      /// The optional list of offset distances to the grid axes.  The list must
      /// either be null, or contain either 2 or 3 items.
      /// </summary>
      public IList<double> OffsetDistances { get; protected set; } = null;

      protected IFCVirtualGridIntersection()
      {
      }

      protected IFCVirtualGridIntersection(IFCAnyHandle item)
      {
         Process(item);
      }

      private XYZ GetReferenceVector(Curve curve)
      {
         XYZ referenceDir = null;

         // IfcVirtualGridIntersection is intended to be used for line or arc grid lines
         Line firstLine = curve as Line;
         if (firstLine != null)
         {
            XYZ firstDirection = firstLine.Direction;
            XYZ initialRefDir = XYZ.BasisZ;
            if (!MathUtil.VectorsAreParallel(firstDirection, initialRefDir))
            {
               XYZ secondDirection = initialRefDir.CrossProduct(firstDirection).Normalize();
               referenceDir = firstDirection.CrossProduct(secondDirection);
            }
         }
         else
         {
            Arc arc = curve as Arc;
            if (arc != null)
            {
               referenceDir = arc.Normal;
            }
         }

         if (referenceDir == null)
         {
            Importer.TheLog.LogError(Id, "Can't determine normal of grid intersection lines, assuming Z direction.", false);
            referenceDir = XYZ.BasisZ;
         }
         
         return referenceDir;
      }

      protected override void Process(IFCAnyHandle ifcVirtualGridIntersection)
      {
         base.Process(ifcVirtualGridIntersection);

         IList<IFCAnyHandle> intersectingAxes = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcVirtualGridIntersection, "IntersectingAxes");
         if (intersectingAxes == null || 
            intersectingAxes.Count != 2 || 
            IFCAnyHandleUtil.IsNullOrHasNoValue(intersectingAxes[0]) ||
            IFCAnyHandleUtil.IsNullOrHasNoValue(intersectingAxes[1]))
         {
            Importer.TheLog.LogError(ifcVirtualGridIntersection.StepId, "Missing or invalid intersecting axes.", false);
            return;
         }

         IFCGridAxis firstAxis = IFCGridAxis.ProcessIFCGridAxis(intersectingAxes[0]);
         if (firstAxis == null)
            return;
         
         IFCGridAxis secondAxis = IFCGridAxis.ProcessIFCGridAxis(intersectingAxes[1]);
         if (secondAxis == null)
            return;
       
         OffsetDistances = IFCAnyHandleUtil.GetAggregateDoubleAttribute<List<double>>(ifcVirtualGridIntersection, "OffsetDistances");
         double[] offsetDistances = new double[3];
         int offsetDistanceCount = (OffsetDistances != null) ? OffsetDistances.Count : 0;
         for (int ii = 0; ii < 3; ii++)
         {
            offsetDistances[ii] = (offsetDistanceCount > ii) ? OffsetDistances[ii] : 0;
         }

         Curve firstCurve = firstAxis.GetAxisCurveForGridPlacement();
         if (firstCurve == null)
            return;

         Curve secondCurve = secondAxis.GetAxisCurveForGridPlacement();
         if (secondCurve == null)
            return;

         // We need to figure out the reference vector to do the offset, but we can't get
         // the reference vector without getting the tangent at the intersection point.
         // We will use a heuristic in GetReferenceVector to guess based on the curve types.
         XYZ referenceVector = GetReferenceVector(firstCurve);
         Curve firstOffsetCurve = (!MathUtil.IsAlmostZero(offsetDistances[0])) ? 
            firstCurve.CreateOffset(offsetDistances[0], referenceVector) : firstCurve;
         Curve secondOffsetCurve = (!MathUtil.IsAlmostZero(offsetDistances[1])) ?
            secondCurve.CreateOffset(offsetDistances[1], referenceVector) : secondCurve;

         IntersectionResultArray resultArray;
         SetComparisonResult result = firstOffsetCurve.Intersect(secondOffsetCurve, out resultArray);
         if (result != SetComparisonResult.Overlap || resultArray == null || resultArray.Size == 0)
            return;

         IntersectionResult intersectionPoint = resultArray.get_Item(0);
         XYZ origin = intersectionPoint.XYZPoint + offsetDistances[2] * referenceVector;
         Transform derivatives = firstCurve.ComputeDerivatives(intersectionPoint.UVPoint.U, false);
         LocalCoordinateSystem = Transform.CreateTranslation(origin);
         LocalCoordinateSystem.BasisX = derivatives.BasisX;
         LocalCoordinateSystem.BasisY = referenceVector.CrossProduct(derivatives.BasisX);
         LocalCoordinateSystem.BasisZ = referenceVector;
      }

      /// <summary>
      /// Processes an IfcVirtualGridIntersection object.
      /// </summary>
      /// <param name="ifcVirtualGridIntersection">The IfcVirtualGridIntersection handle.</param>
      /// <returns>The IFCVirtualGridIntersection object.</returns>
      public static IFCVirtualGridIntersection ProcessIFCVirtualGridIntersection(IFCAnyHandle ifcVirtualGridIntersection)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcVirtualGridIntersection))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcVirtualGridIntersection);
            return null;
         }

         IFCEntity virtualGridIntersection;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcVirtualGridIntersection.StepId, out virtualGridIntersection))
            return (virtualGridIntersection as IFCVirtualGridIntersection);

         return new IFCVirtualGridIntersection(ifcVirtualGridIntersection);
      }

   }
}