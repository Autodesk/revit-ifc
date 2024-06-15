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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents the object placement.
   /// </summary>
   public class IFCLocation : IFCEntity
   {
      /// <summary>
      /// The IFCLocation that this IFCLocation is relative to. 
      /// </summary>
      public IFCLocation RelativeTo { get; set; } = null;

      /// <summary>
      /// The total transform.
      /// </summary>
      public Transform TotalTransform
      {
         get { return RelativeTo != null ? RelativeTo.TotalTransform.Multiply(RelativeTransform) : RelativeTransform; }
      }

      /// <summary>
      /// The total transform, taking into account any large coordinate offset.
      /// </summary>
      public Transform TotalTransformAfterOffset
      {
         get 
         {
            Transform totalTransform = TotalTransform ?? Transform.Identity;
            totalTransform.Origin += (Importer.TheHybridInfo?.LargeCoordinateOriginOffset ?? XYZ.Zero);
            return totalTransform;
         }
      }

      /// <summary>
      /// The relative transform.
      /// </summary>
      public Transform RelativeTransform { get; set; } = Transform.Identity;

      /// <summary>
      /// Determines if this IfcLocation is relative to the IfcSite's location.
      /// </summary>
      /// <remarks>
      /// This is not part of the IFC definition of an IfcLocation, but is necessary for Revit in case
      /// 1. The IfcSite has a non-identity IfcLocation and 
      /// 2. An object has an IfcLocation that is incorrectly not associated to IfcSite.
      /// We will warn about this but correct it.
      /// </remarks>
      public bool RelativeToSite { get; set; } = false;

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCLocation()
      {

      }

      /// <summary>
      /// Create a dummy IFCLocation that contains only a relative transform.
      /// </summary>
      /// <param name="relativeTransform">The transform associated with the location.</param>
      /// <returns>The new IFCLocation.</returns>
      /// <remarks>
      /// This is intended for use for IFCSites, whose location has either been modified
      /// by the RefElevation parameter, or by being moved far from the origin.
      /// </remarks>
      static public IFCLocation CreateDummyLocation(Transform relativeTransform)
      {
         IFCLocation dummyLocation = new IFCLocation();
         dummyLocation.RelativeTransform = relativeTransform;
         return dummyLocation;
      }

      /// <summary>
      /// Constructs an IFCLocation from the IfcObjectPlacement handle.
      /// </summary>
      /// <param name="ifcObjectPlacement">The IfcObjectPlacement handle.</param>
      protected IFCLocation(IFCAnyHandle ifcObjectPlacement)
      {
         Process(ifcObjectPlacement);
      }

      static Transform ProcessPlacementBase(IFCAnyHandle placement)
      {
         IFCAnyHandle location = IFCAnyHandleUtil.GetInstanceAttribute(placement, "Location");
         XYZ origin = IFCPoint.ProcessScaledLengthIFCCartesianPoint(location);
         if (origin == null)
         {
            Importer.TheLog.LogError(placement.StepId, "Missing or invalid location attribute.", false);
            origin = XYZ.Zero;
         }
         return Transform.CreateTranslation(origin);
      }

      static Transform ProcessAxis2Placement2D(IFCAnyHandle placement)
      {
         IFCAnyHandle refDirection = IFCAnyHandleUtil.GetInstanceAttribute(placement, "RefDirection");
         XYZ refDirectionX =
             IFCAnyHandleUtil.IsNullOrHasNoValue(refDirection) ? XYZ.BasisX : IFCPoint.ProcessNormalizedIFCDirection(refDirection);
         XYZ refDirectionY = new XYZ(-refDirectionX.Y, refDirectionX.X, 0.0);

         Transform lcs = ProcessPlacementBase(placement);
         lcs.BasisX = refDirectionX;
         lcs.BasisY = refDirectionY;
         lcs.BasisZ = refDirectionX.CrossProduct(refDirectionY);

         return lcs;
      }

      static Transform ProcessAxis2Placement3D(IFCAnyHandle placement)
      {
         IFCAnyHandle axis = IFCAnyHandleUtil.GetInstanceAttribute(placement, "Axis");
         IFCAnyHandle refDirection = IFCAnyHandleUtil.GetInstanceAttribute(placement, "RefDirection");

         XYZ axisXYZ = IFCAnyHandleUtil.IsNullOrHasNoValue(axis) ?
             XYZ.BasisZ : IFCPoint.ProcessNormalizedIFCDirection(axis, false);
         XYZ refDirectionXYZ = IFCAnyHandleUtil.IsNullOrHasNoValue(refDirection) ?
             XYZ.BasisX : IFCPoint.ProcessNormalizedIFCDirection(refDirection, false);

         if (axisXYZ.IsZeroLength())
         {
            Importer.TheLog.LogError(axis.StepId, "Local transform contains 0 length axis vector, reverting to Z-axis.", false);
            axisXYZ = XYZ.BasisZ;
         }
         if (refDirectionXYZ.IsZeroLength())
         {
            Importer.TheLog.LogError(refDirection.StepId, "Local transform contains 0 length reference vector, reverting to X-axis.", false);
            refDirectionXYZ = XYZ.BasisX;
         }

         Transform lcs = ProcessPlacementBase(placement);

         XYZ lcsX = (refDirectionXYZ - refDirectionXYZ.DotProduct(axisXYZ) * axisXYZ).Normalize();
         XYZ lcsY = axisXYZ.CrossProduct(lcsX).Normalize();

         if (lcsX.IsZeroLength() || lcsY.IsZeroLength())
         {
            Importer.TheLog.LogError(placement.StepId, "Local transform contains 0 length vectors.", true);
         }

         lcs.BasisX = lcsX;
         lcs.BasisY = lcsY;
         lcs.BasisZ = axisXYZ;
         return lcs;
      }

      /// <summary>
      /// Convert an IfcAxis1Placement into a transform.
      /// </summary>
      /// <param name="placement">The placement handle.</param>
      /// <returns>The transform.</returns>
      public static Transform ProcessIFCAxis1Placement(IFCAnyHandle ifcPlacement)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcPlacement))
            return Transform.Identity;

         Transform transform;
         if (IFCImportFile.TheFile.TransformMap.TryGetValue(ifcPlacement.StepId, out transform))
            return transform;

         if (!IFCAnyHandleUtil.IsValidSubTypeOf(ifcPlacement, IFCEntityType.IfcAxis1Placement))
         {
            Importer.TheLog.LogUnhandledSubTypeError(ifcPlacement, "IfcAxis1Placement", false);
            transform = Transform.Identity;
         }

         IFCAnyHandle ifcAxis = IFCAnyHandleUtil.GetInstanceAttribute(ifcPlacement, "Axis");
         XYZ norm = IFCAnyHandleUtil.IsNullOrHasNoValue(ifcAxis) ? XYZ.BasisZ : IFCPoint.ProcessNormalizedIFCDirection(ifcAxis);

         transform = ProcessPlacementBase(ifcPlacement);
         Plane arbitraryPlane = Plane.CreateByNormalAndOrigin(norm, transform.Origin);

         transform.BasisX = arbitraryPlane.XVec;
         transform.BasisY = arbitraryPlane.YVec;
         transform.BasisZ = norm;

         IFCImportFile.TheFile.TransformMap[ifcPlacement.StepId] = transform;
         return transform;
      }

      /// <summary>
      /// Convert an IfcAxis2Placement into a transform.
      /// </summary>
      /// <param name="placement">The placement handle.</param>
      /// <returns>The transform.</returns>
      public static Transform ProcessIFCAxis2Placement(IFCAnyHandle ifcPlacement)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcPlacement))
            return Transform.Identity;

         Transform transform;
         if (IFCImportFile.TheFile.TransformMap.TryGetValue(ifcPlacement.StepId, out transform))
            return transform;

         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcPlacement, IFCEntityType.IfcAxis2Placement2D))
            transform = ProcessAxis2Placement2D(ifcPlacement);
         else if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcPlacement, IFCEntityType.IfcAxis2Placement3D))
            transform = ProcessAxis2Placement3D(ifcPlacement);
         else
         {
            Importer.TheLog.LogUnhandledSubTypeError(ifcPlacement, "IfcAxis2Placement", false);
            transform = Transform.Identity;
         }

         IFCImportFile.TheFile.TransformMap[ifcPlacement.StepId] = transform;
         return transform;
      }

      protected void ProcessLocalPlacement(IFCAnyHandle objectPlacement)
      {
         IFCAnyHandle placementRelTo = IFCAnyHandleUtil.GetInstanceAttribute(objectPlacement, "PlacementRelTo");
         IFCAnyHandle relativePlacement = IFCAnyHandleUtil.GetInstanceAttribute(objectPlacement, "RelativePlacement");

         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(placementRelTo))
         {
            RelativeTo = ProcessIFCObjectPlacement(placementRelTo);
            // If the location that this is relative to is relative to the site location, then
            // so is this.  This relies on RelativeToSite for the IfcSite local placement to be
            // set to true before any other entities are processed.
            RelativeToSite = RelativeTo.RelativeToSite;
         }

         RelativeTransform = ProcessIFCAxis2Placement(relativePlacement);
      }

      protected void ProcessGridPlacement(IFCAnyHandle gridPlacement)
      {
         Importer.TheCache.PreProcessGrids();

         IFCAnyHandle placementLocation = IFCImportHandleUtil.GetRequiredInstanceAttribute(gridPlacement, "PlacementLocation", true);

         IFCVirtualGridIntersection virtualGridIntersection = IFCVirtualGridIntersection.ProcessIFCVirtualGridIntersection(placementLocation);

         IFCAnyHandle placementRefDirection = IFCAnyHandleUtil.GetInstanceAttribute(gridPlacement, "PlacementRefDirection");

         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(placementRefDirection))
         {
            // TODO: Handle later, if we see examples of use.
            Importer.TheLog.LogError(gridPlacement.Id, "placementRefDirection attribute not handled.", false);
         }
 
         RelativeTransform = virtualGridIntersection.LocalCoordinateSystem;
      }

      protected override void Process(IFCAnyHandle objectPlacement)
      {
         base.Process(objectPlacement);

         // Various TODOs here.
         // 1. We should create IFCLocalPlacement and IFCGridPlacement, and have them inherit
         //    from IFCLocation.
         // 2. IFCGridPlacement and IFCVirtualGridIntersection implementation is incomplete; 
         //    we will let the user know if they get to an unsupported case. 
         if (IFCAnyHandleUtil.IsValidSubTypeOf(objectPlacement, IFCEntityType.IfcLocalPlacement))
            ProcessLocalPlacement(objectPlacement);
         else if (IFCAnyHandleUtil.IsValidSubTypeOf(objectPlacement, IFCEntityType.IfcGridPlacement))
            ProcessGridPlacement(objectPlacement);
         else
            Importer.TheLog.LogUnhandledSubTypeError(objectPlacement, "IfcObjectPlacement", false);         
      }

      /// <summary>
      /// Processes an IfcObjectPlacement object.
      /// </summary>
      /// <param name="objectPlacement">The IfcObjectPlacement handle.</param>
      /// <returns>The IFCLocation object.</returns>
      public static IFCLocation ProcessIFCObjectPlacement(IFCAnyHandle ifcObjectPlacement)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcObjectPlacement))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcObjectPlacement);
            return null;
         }

         IFCEntity location;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcObjectPlacement.StepId, out location))
            return (location as IFCLocation);

         return new IFCLocation(ifcObjectPlacement);
      }

      public static void WarnIfFaraway(IFCProduct product)
      {
         XYZ origin = product?.ObjectLocation?.TotalTransformAfterOffset?.Origin;
         if (origin != null && !XYZ.IsWithinLengthLimits(origin))
         {
            Importer.TheLog.LogWarning(product.Id, "This entity has an origin that is outside of Revit's creation limits.  This could result in bad graphical display of geometry.", false);
         }
      }
}
}