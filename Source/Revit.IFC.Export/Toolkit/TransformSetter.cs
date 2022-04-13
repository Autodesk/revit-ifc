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
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Toolkit
{
   /// <summary>
   ///    A state-based class that forces an extra transformation applied to objects being exported.
   /// </summary>
   /// <remarks>
   ///    IFC has a system of local placements; these are created from a set of transforms in Revit.
   ///    Sometimes there is a need to create a 'fake' transform to get the right local placement structure for IFC.
   ///    This class is intended to maintain the transformation for the duration that it is needed.
   ///    To ensure that the lifetime of the object is correctly managed, you should declare an instance
   ///    of this class as a part of a 'using' statement in C# or
   ///    similar construct in other languages.
   /// </remarks>
   public class TransformSetter : IDisposable
   {
      bool m_Initialized = false;

      ExporterIFC m_ExporterIFC = null;

      /// <summary>
      ///    Creates a new instance of a transform setter.
      /// </summary>
      /// <returns>
      ///    The new transform setter.
      /// </returns>
      public static TransformSetter Create()
      {
         return new TransformSetter();
      }

      /// <summary>
      ///    Initializes the transformation in the transform setter.
      /// </summary>
      /// <param name="exporterIFC">
      ///    The exporter.
      /// </param>
      /// <param name="transform">
      ///    The transform.
      /// </param>
      public void Initialize(ExporterIFC exporterIFC, Transform trf)
      {
         Initialize(exporterIFC, trf.Origin, trf.BasisX, trf.BasisY);
      }

      void Initialize(ExporterIFC exporterIFC, XYZ origin, XYZ xDir, XYZ yDir)
      {
         if (!m_Initialized)
         {
            m_ExporterIFC = exporterIFC;
            Transform trf = Transform.Identity;
            trf.Origin = origin;
            trf.BasisX = xDir;
            trf.BasisY = yDir;
            m_ExporterIFC.PushTransform(trf);
            m_Initialized = true;
         }
      }

      /// <summary>
      ///    Initializes the transformation in the transform setter.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="geometryList">The set of geometry used to determine the bounding box.</param>
      /// <param name="ecData">The extrusion creation data which contains the local placement.</param>
      /// <returns>The transform corresponding to the movement, if any.</returns>
      /// <remarks>This method will eventually be obsoleted by the InitializeFromBoundingBox/CreateLocalPlacementFromOffset pair below, which delays creating or updating the local placement
      /// until we are certain we will use it, saving time and reducing wasted line numbers.</remarks>
      public Transform InitializeFromBoundingBox(ExporterIFC exporterIFC, IList<GeometryObject> geometryList, IFCExtrusionCreationData ecData)
      {
         if (ecData == null)
            return null;
         Transform trf = Transform.Identity;

         IFCAnyHandle localPlacement = ecData.GetLocalPlacement();
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(localPlacement))
         {
            IFCFile file = exporterIFC.GetFile();
            BoundingBoxXYZ bbox = GeometryUtil.GetBBoxOfGeometries(geometryList);

            // If the BBox passes through (0,0, 0), or no bbox, do nothing.
            if (bbox == null ||
                ((bbox.Min.X < MathUtil.Eps() && bbox.Max.X > -MathUtil.Eps()) &&
                 (bbox.Min.Y < MathUtil.Eps() && bbox.Max.Y > -MathUtil.Eps()) &&
                 (bbox.Min.Z < MathUtil.Eps() && bbox.Max.Z > -MathUtil.Eps())))
            {
               if (!ecData.ReuseLocalPlacement)
                  ecData.SetLocalPlacement(ExporterUtil.CopyLocalPlacement(file, localPlacement));
               return trf;
            }

            XYZ bboxMin = bbox.Min;
            XYZ scaledOrig = UnitUtil.ScaleLength(bboxMin);

            Transform scaledTrf = GeometryUtil.GetScaledTransform(exporterIFC);

            XYZ lpOrig = scaledTrf.OfPoint(scaledOrig);
            if (!ecData.AllowVerticalOffsetOfBReps)
               lpOrig = new XYZ(lpOrig.X, lpOrig.Y, 0.0);

            Transform scaledTrfInv = scaledTrf.Inverse;
            XYZ scaledInvOrig = scaledTrfInv.OfPoint(XYZ.Zero);

            XYZ unscaledInvOrig = UnitUtil.UnscaleLength(scaledInvOrig);

            XYZ trfOrig = unscaledInvOrig - bboxMin;
            if (!ecData.AllowVerticalOffsetOfBReps)
               trfOrig = new XYZ(trfOrig.X, trfOrig.Y, 0.0);

            if (!MathUtil.IsAlmostZero(trfOrig.DotProduct(trfOrig)))
            {
               Initialize(exporterIFC, trfOrig, XYZ.BasisX, XYZ.BasisY);
               if (!ecData.ReuseLocalPlacement)
                  ecData.SetLocalPlacement(ExporterUtil.CreateLocalPlacement(file, localPlacement, lpOrig, null, null));
               else
               {
                  IFCAnyHandle relativePlacement = GeometryUtil.GetRelativePlacementFromLocalPlacement(localPlacement);
                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(relativePlacement))
                  {
                     IFCAnyHandle newRelativePlacement = ExporterUtil.CreateAxis(file, lpOrig, null, null);
                     GeometryUtil.SetRelativePlacement(localPlacement, newRelativePlacement);
                  }
                  else
                  {
                     IFCAnyHandle oldOriginHnd, zDirHnd, xDirHnd;
                     xDirHnd = IFCAnyHandleUtil.GetInstanceAttribute(relativePlacement, "RefDirection");
                     zDirHnd = IFCAnyHandleUtil.GetInstanceAttribute(relativePlacement, "Axis");
                     oldOriginHnd = IFCAnyHandleUtil.GetInstanceAttribute(relativePlacement, "Location");

                     bool trfSet = false;
                     XYZ xDir = XYZ.BasisX; XYZ zDir = XYZ.BasisZ; XYZ oldCoords = XYZ.Zero;
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(xDirHnd))
                     {
                        xDir = GeometryUtil.GetDirectionRatios(xDirHnd);
                        trfSet = true;
                     }
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(zDirHnd))
                     {
                        zDir = GeometryUtil.GetDirectionRatios(zDirHnd);
                        trfSet = true;
                     }
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(oldOriginHnd))
                     {
                        oldCoords = GeometryUtil.GetCoordinates(oldOriginHnd);
                     }

                     if (trfSet)
                     {
                        XYZ yDir = zDir.CrossProduct(xDir);
                        Transform relPlacementTrf = Transform.Identity;
                        relPlacementTrf.Origin = oldCoords; relPlacementTrf.BasisX = xDir;
                        relPlacementTrf.BasisY = yDir; relPlacementTrf.BasisZ = zDir;
                        lpOrig = relPlacementTrf.OfPoint(lpOrig);
                     }
                     else
                        lpOrig = oldCoords + lpOrig;

                     IFCAnyHandle newOriginHnd = ExporterUtil.CreateCartesianPoint(file, lpOrig);
                     IFCAnyHandleUtil.SetAttribute(relativePlacement, "Location", newOriginHnd);
                  }
               }

               trf.Origin = lpOrig;
            }
            else if (ecData.ForceOffset)
               ecData.SetLocalPlacement(ExporterUtil.CreateLocalPlacement(file, localPlacement, null));
            else if (!ecData.ReuseLocalPlacement)
               ecData.SetLocalPlacement(ExporterUtil.CopyLocalPlacement(file, localPlacement));
         }
         return trf;
      }

      /// <summary>
      ///    Initializes the transformation in the transform setter.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="bbox">The bounding box.</param>
      /// <param name="ecData">The extrusion creation data which contains the local placement.</param>
      /// <param name="unscaledTrfOrig">The scaled local placement origin.</param>
      /// <param name="locationCurve">The optional location curve.</param>
      /// <returns>The transform corresponding to the movement, if any.</returns>
      public Transform InitializeFromBoundingBox(ExporterIFC exporterIFC, BoundingBoxXYZ bbox, IFCExtrusionCreationData ecData, LocationCurve locationCurve, out XYZ unscaledTrfOrig)
      {
         unscaledTrfOrig = new XYZ();
         if (ecData == null)
            return null;

         Transform trf = Transform.Identity;
         IFCAnyHandle localPlacement = ecData.GetLocalPlacement();
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(localPlacement))
         {
            IFCFile file = exporterIFC.GetFile();

            // If the BBox passes through (0,0, 0), or no bbox, do nothing.
            if (bbox == null ||
                ((bbox.Min.X < MathUtil.Eps() && bbox.Max.X > -MathUtil.Eps()) &&
                 (bbox.Min.Y < MathUtil.Eps() && bbox.Max.Y > -MathUtil.Eps()) &&
                 (bbox.Min.Z < MathUtil.Eps() && bbox.Max.Z > -MathUtil.Eps())))
               return trf;

            XYZ corner = bbox.Min;

            // Rise the origin to the top corner for some linear inclined geometries
            // to fix the problem of misalignment between Body and 'Curve2D' Axis
            if (locationCurve != null && locationCurve.Curve is Line)
            {
               XYZ lineDir = (locationCurve.Curve as Line).Direction;
               double angle = 0.0;
               if (!MathUtil.IsAlmostZero(lineDir.X))
                  angle = Math.Atan2(lineDir.Z, lineDir.X);
               else
                  angle = Math.Atan2(lineDir.Z, lineDir.Y);

               if (angle > 0.5 * Math.PI && angle < Math.PI || angle > -0.5 * Math.PI && angle < 0)
                  corner = new XYZ(corner.X, corner.Y, bbox.Max.Z);
            }

            XYZ scaledOrig = UnitUtil.ScaleLength(corner);

            Transform scaledTrf = GeometryUtil.GetScaledTransform(exporterIFC);

            XYZ lpOrig = scaledTrf.OfPoint(scaledOrig);
            if (!ecData.AllowVerticalOffsetOfBReps)
               lpOrig = new XYZ(lpOrig.X, lpOrig.Y, 0.0);

            Transform scaledTrfInv = scaledTrf.Inverse;
            XYZ scaledInvOrig = scaledTrfInv.OfPoint(XYZ.Zero);

            XYZ unscaledInvOrig = UnitUtil.UnscaleLength(scaledInvOrig);

            unscaledTrfOrig = unscaledInvOrig - corner;
            if (!ecData.AllowVerticalOffsetOfBReps)
               unscaledTrfOrig = new XYZ(unscaledTrfOrig.X, unscaledTrfOrig.Y, 0.0);

            if (!MathUtil.IsAlmostZero(unscaledTrfOrig.DotProduct(unscaledTrfOrig)))
            {
               Initialize(exporterIFC, unscaledTrfOrig, XYZ.BasisX, XYZ.BasisY);
               if (!ecData.ReuseLocalPlacement)
               {
               }
               else
               {
                  IFCAnyHandle relativePlacement = GeometryUtil.GetRelativePlacementFromLocalPlacement(localPlacement);
                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(relativePlacement))
                  {
                     IFCAnyHandle oldOriginHnd, zDirHnd, xDirHnd;
                     xDirHnd = IFCAnyHandleUtil.GetInstanceAttribute(relativePlacement, "RefDirection");
                     zDirHnd = IFCAnyHandleUtil.GetInstanceAttribute(relativePlacement, "Axis");
                     oldOriginHnd = IFCAnyHandleUtil.GetInstanceAttribute(relativePlacement, "Location");

                     bool trfSet = false;
                     XYZ xDir = XYZ.BasisX; XYZ zDir = XYZ.BasisZ; XYZ oldCoords = XYZ.Zero;
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(xDirHnd))
                     {
                        xDir = GeometryUtil.GetDirectionRatios(xDirHnd);
                        trfSet = true;
                     }
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(zDirHnd))
                     {
                        zDir = GeometryUtil.GetDirectionRatios(zDirHnd);
                        trfSet = true;
                     }
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(oldOriginHnd))
                     {
                        oldCoords = GeometryUtil.GetCoordinates(oldOriginHnd);
                     }

                     if (trfSet)
                     {
                        XYZ yDir = zDir.CrossProduct(xDir);
                        Transform relPlacementTrf = Transform.Identity;
                        relPlacementTrf.Origin = oldCoords; relPlacementTrf.BasisX = xDir;
                        relPlacementTrf.BasisY = yDir; relPlacementTrf.BasisZ = zDir;
                        lpOrig = relPlacementTrf.OfPoint(lpOrig);
                     }
                     else
                        lpOrig = oldCoords + lpOrig;
                  }
               }

               trf.Origin = UnitUtil.UnscaleLength(lpOrig);
            }
         }
         return trf;
      }

      /// <summary>
      /// Creates or updates the IfcLocalPlacement associated with the current origin offset.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="bbox">The bounding box.</param>
      /// <param name="ecData">The extrusion creation data which contains the local placement.</param>
      /// <param name="lpOrig">The local placement origin.</param>
      /// <param name="unscaledTrfOrig">The unscaled local placement origin.</param>
      public void CreateLocalPlacementFromOffset(ExporterIFC exporterIFC, BoundingBoxXYZ bbox, IFCExtrusionCreationData ecData, XYZ lpOrig, XYZ unscaledTrfOrig)
      {
         if (ecData == null)
            return;

         IFCAnyHandle localPlacement = ecData.GetLocalPlacement();
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(localPlacement))
         {
            IFCFile file = exporterIFC.GetFile();

            // If the BBox passes through (0,0, 0), or no bbox, do nothing.
            if (bbox == null ||
                ((bbox.Min.X < MathUtil.Eps() && bbox.Max.X > -MathUtil.Eps()) &&
                 (bbox.Min.Y < MathUtil.Eps() && bbox.Max.Y > -MathUtil.Eps()) &&
                 (bbox.Min.Z < MathUtil.Eps() && bbox.Max.Z > -MathUtil.Eps())))
            {
               if (!ecData.ReuseLocalPlacement)
                  ecData.SetLocalPlacement(ExporterUtil.CopyLocalPlacement(file, localPlacement));
               return;
            }

            if (!MathUtil.IsAlmostZero(unscaledTrfOrig.DotProduct(unscaledTrfOrig)))
            {
               if (!ecData.ReuseLocalPlacement)
                  ecData.SetLocalPlacement(ExporterUtil.CreateLocalPlacement(file, localPlacement, lpOrig, null, null));
               else
               {
                  IFCAnyHandle relativePlacement = GeometryUtil.GetRelativePlacementFromLocalPlacement(localPlacement);
                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(relativePlacement))
                  {
                     IFCAnyHandle newRelativePlacement = ExporterUtil.CreateAxis(file, lpOrig, null, null);
                     GeometryUtil.SetRelativePlacement(localPlacement, newRelativePlacement);
                  }
                  else
                  {
                     IFCAnyHandle newOriginHnd = ExporterUtil.CreateCartesianPoint(file, lpOrig);
                     IFCAnyHandleUtil.SetAttribute(relativePlacement, "Location", newOriginHnd);
                  }
               }
            }
            else if (ecData.ForceOffset)
               ecData.SetLocalPlacement(ExporterUtil.CreateLocalPlacement(file, localPlacement, null));
            else if (!ecData.ReuseLocalPlacement)
               ecData.SetLocalPlacement(ExporterUtil.CopyLocalPlacement(file, localPlacement));
         }
      }

      #region IDisposable Members

      public void Dispose()
      {
         if (m_Initialized)
         {
            m_ExporterIFC.PopTransform();
         }
      }

      #endregion
   }
}