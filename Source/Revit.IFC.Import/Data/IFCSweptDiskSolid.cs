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
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public class IFCSweptDiskSolid : IFCSolidModel
   {
      /// <summary>
      /// The curve used for the sweep.
      /// </summary>
      public IFCCurve Directrix { get; protected set; } = null;

      /// <summary>
      /// The outer radius of the swept disk.
      /// </summary>
      public double Radius { get; protected set; } = 0.0;

      /// <summary>
      /// The optional inner radius of the swept disk.
      /// </summary>
      public double? InnerRadius { get; protected set; } = null;

      /// <summary>
      /// The start parameter of the sweep, as measured along the length of the Directrix.
      /// </summary>
      /// <remarks>This is not optional in IFC, but we will default to 0.0 if not set.</remarks>
      public double StartParameter { get; protected set; } = 0.0;

      /// <summary>
      /// The optional end parameter of the sweep, as measured along the length of the Directrix.
      /// </summary>
      /// <remarks>This is not optional in IFC, but we will default to ParametricLength(curve) if not set.</remarks>
      public double? EndParameter { get; protected set; } = null;

      protected IFCSweptDiskSolid()
      {
      }

      override protected void Process(IFCAnyHandle solid)
      {
         base.Process(solid);

         IFCAnyHandle directrix = IFCImportHandleUtil.GetRequiredInstanceAttribute(solid, "Directrix", true);
         Directrix = IFCCurve.ProcessIFCCurve(directrix);

         bool found = false;
         Radius = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(solid, "Radius", out found);
         if (!found || !Application.IsValidThickness(Radius))
            Importer.TheLog.LogError(solid.StepId, "IfcSweptDiskSolid radius is invalid, aborting.", true);

         double innerRadius = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(solid, "InnerRadius", 0.0);
         if (Application.IsValidThickness(innerRadius))
         {
            if (!Application.IsValidThickness(Radius - innerRadius))
               Importer.TheLog.LogError(solid.StepId, "IfcSweptDiskSolid inner radius is too large, aborting.", true);
            InnerRadius = innerRadius;
         }

         StartParameter = IFCImportHandleUtil.GetOptionalDoubleAttribute(solid, "StartParam", 0.0);
         if (StartParameter < MathUtil.Eps())
            StartParameter = 0.0;

         double endParameter = IFCImportHandleUtil.GetOptionalDoubleAttribute(solid, "EndParam", -1.0);
         if (!MathUtil.IsAlmostEqual(endParameter, -1.0))
         {
            if (endParameter < StartParameter + MathUtil.Eps())
            {
               Importer.TheLog.LogWarning(solid.StepId, "IfcSweptDiskSolid swept curve end parameter less than or equal to start parameter, ignoring both.", true);
               StartParameter = 0.0;
            }
            else
            {
               EndParameter = endParameter;
            }
         }
      }

      private IList<CurveLoop> CreateProfileCurveLoopsForDirectrix(Curve directrix, out double startParam)
      {
         startParam = 0.0;

         if (directrix == null)
            return null;

         if (directrix.IsBound)
            startParam = directrix.GetEndParameter(0);

         Transform originTrf = directrix.ComputeDerivatives(startParam, false);

         if (originTrf == null)
            return null;

         // The X-dir of the transform of the start of the directrix will form the normal of the disk.
         // We are not using the origin in Plane.CreateByNormalAndOrigin because that has
         // limits of [-30K,30K].
         Plane diskPlaneAxes = Plane.CreateByNormalAndOrigin(originTrf.BasisX, XYZ.Zero);

         IList<CurveLoop> profileCurveLoops = new List<CurveLoop>();

         CurveLoop diskOuterCurveLoop = new CurveLoop();
         diskOuterCurveLoop.Append(Arc.Create(originTrf.Origin, Radius, 0, Math.PI, diskPlaneAxes.XVec, diskPlaneAxes.YVec));
         diskOuterCurveLoop.Append(Arc.Create(originTrf.Origin, Radius, Math.PI, 2.0 * Math.PI, diskPlaneAxes.XVec, diskPlaneAxes.YVec));
         profileCurveLoops.Add(diskOuterCurveLoop);

         if (InnerRadius.HasValue)
         {
            CurveLoop diskInnerCurveLoop = new CurveLoop();
            diskInnerCurveLoop.Append(Arc.Create(originTrf.Origin, InnerRadius.Value, 0, Math.PI, diskPlaneAxes.XVec, diskPlaneAxes.YVec));
            diskInnerCurveLoop.Append(Arc.Create(originTrf.Origin, InnerRadius.Value, Math.PI, 2.0 * Math.PI, diskPlaneAxes.XVec, diskPlaneAxes.YVec));
            profileCurveLoops.Add(diskInnerCurveLoop);
         }

         return profileCurveLoops;
      }

      /// <summary>
      /// Create as much of the swept disk solid geometry as possible.
      /// </summary>
      /// <param name="trimmedDirectrixInWCS">The directrix.</param>
      /// <param name="profileCurveLoops">The original profile curves.</param>
      /// <param name="solidOptions">The options for creating the solids.</param>
      /// <returns>The created geometry, and true if it represents all of the original pieces.</returns>
      private (IList<GeometryObject>, bool) SplitSweptDiskIntoValidPieces(CurveLoop trimmedDirectrixInWCS, IList<CurveLoop> profileCurveLoops, SolidOptions solidOptions)
      {
         // If we have 0 or 1 curves, there is nothing we can do here.
         int numCurves = trimmedDirectrixInWCS.Count();
         if (numCurves < 2)
            return (null, false);

         // We will attempt to represent the original description in as few pieces as possible.  
         IList<Curve> directrixCurves = new List<Curve>();
         foreach (Curve directrixCurve in trimmedDirectrixInWCS)
         {
            if (directrixCurve == null)
            {
               numCurves--;
               if (numCurves < 2)
                  return (null, false);
               continue;
            }
            directrixCurves.Add(directrixCurve);
         }

         IList<GeometryObject> sweptDiskPieces = new List<GeometryObject>();

         // We will march along the directrix one curve at a time, trying to build a bigger piece of the sweep.  At the point that we throw an exception,
         // we will take the last biggest piece and start over.
         CurveLoop currentCurveLoop = new CurveLoop();
         Solid bestSolidSoFar = null;
         double pathAttachmentParam = directrixCurves[0].GetEndParameter(0);
         bool missedGeometry = false;

         for (int ii = 0; ii < numCurves; ii++)
         {
            currentCurveLoop.Append(directrixCurves[ii]);
            try
            {
               Solid currentSolid = GeometryCreationUtilities.CreateSweptGeometry(currentCurveLoop, 0, pathAttachmentParam, profileCurveLoops,
                  solidOptions);
               bestSolidSoFar = currentSolid;
            }
            catch
            {
               if (bestSolidSoFar != null)
               {
                  sweptDiskPieces.Add(bestSolidSoFar);
                  bestSolidSoFar = null;
               }
            }

            // This should only happen as a result of the catch loop above.  We want to
            // protect against the case where one or more pieces of the sweep are completely
            // invalid.
            while (bestSolidSoFar == null && (ii < numCurves))
            {
               try
               {
                  currentCurveLoop = new CurveLoop();
                  currentCurveLoop.Append(directrixCurves[ii]);
                  profileCurveLoops = CreateProfileCurveLoopsForDirectrix(directrixCurves[ii], out pathAttachmentParam);

                  Solid currentSolid = GeometryCreationUtilities.CreateSweptGeometry(currentCurveLoop, 0, pathAttachmentParam, profileCurveLoops,
                     solidOptions);
                  bestSolidSoFar = currentSolid;
                  break;
               }
               catch
               {
                  ii++;
                  missedGeometry = true;
               }
            }
         }

         if (bestSolidSoFar != null)
            sweptDiskPieces.Add(bestSolidSoFar);

         return (sweptDiskPieces, !missedGeometry);
      }

      /// <summary>
      /// Return geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      /// <returns>Zero or more created geometries.</returns>
      protected override IList<GeometryObject> CreateGeometryInternal(
            IFCImportShapeEditScope shapeEditScope, Transform scaledLcs, string guid)
      {
         List<GeometryObject> myObjs = null;
         Transform scaledSweptDiskPosition = scaledLcs ?? Transform.Identity;

         IList<CurveLoop> trimmedDirectrices = IFCGeometryUtil.TrimCurveLoops(Id, Directrix, StartParameter, EndParameter);
         if (trimmedDirectrices == null)
            return null;

         foreach (CurveLoop trimmedDirectrix in trimmedDirectrices)
         {
            CurveLoop trimmedDirectrixInWCS = IFCGeometryUtil.CreateTransformed(trimmedDirectrix, Id, scaledSweptDiskPosition);

            // Create the disk.
            Curve firstCurve = null;
            foreach (Curve curve in trimmedDirectrixInWCS)
            {
               firstCurve = curve;
               break;
            }

            double startParam = 0.0;
            IList<CurveLoop> profileCurveLoops = CreateProfileCurveLoopsForDirectrix(firstCurve, out startParam);
            if (profileCurveLoops == null)
               return null;

            SolidOptions solidOptions = new SolidOptions(GetMaterialElementId(shapeEditScope), shapeEditScope.GraphicsStyleId);
            myObjs = new List<GeometryObject>();

            try
            {
               Solid sweptDiskSolid = GeometryCreationUtilities.CreateSweptGeometry(trimmedDirectrixInWCS, 0, startParam, profileCurveLoops,
                  solidOptions);
               if (sweptDiskSolid != null)
                  myObjs.Add(sweptDiskSolid);
            }
            catch (Exception ex)
            {
               // If we can't create a solid, we will attempt to split the Solid into valid pieces (that will likely have some overlap).
               if (ex.Message.Contains("self-intersections") || ex.Message.Contains("Failed to create"))
               {
                  (IList<GeometryObject>, bool) solidSegments = SplitSweptDiskIntoValidPieces(trimmedDirectrixInWCS, profileCurveLoops, solidOptions);
                  if (solidSegments.Item1 != null)
                     myObjs.AddRange(solidSegments.Item1);

                  // If Item2 is true, that means that the backup SplitSweptDiskIntoValidPieces routine was
                  // able to create (probably slightly self-intersecting) geometry for all of the pieces
                  // of the directrix.  If it is false, then there was some part of the directrix that we
                  // couldn't create geometry for.  Log a warning in the first case and an error in the
                  // second.
                  if (solidSegments.Item2)
                     Importer.TheLog.LogWarning(Id, "The IfcSweptDiskSolid definition does not define a valid solid, likely due to self-intersections or other such problems; the profile probably extends too far toward the inner curvature of the sweep path. Creating the minimum number of solids possible to represent the geometry.", false);
                  else
                     Importer.TheLog.LogError(Id, "The IfcSweptDiskSolid definition does not define a valid solid, likely due to self-intersections or other such problems; the profile probably extends too far toward the inner curvature of the sweep path. Creating as much of the geometry as possible.", false);

               }
               else
                  throw ex;
            }
         }

         return myObjs;
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
         base.CreateShapeInternal(shapeEditScope, scaledLcs, guid);

         IList<GeometryObject> sweptDiskGeometries = CreateGeometryInternal(shapeEditScope, scaledLcs, guid);
         if (sweptDiskGeometries != null)
         {
            foreach (GeometryObject sweptDiskGeometry in sweptDiskGeometries)
            {
               shapeEditScope.AddGeometry(IFCSolidInfo.Create(Id, sweptDiskGeometry));
            }
         }
      }

      protected IFCSweptDiskSolid(IFCAnyHandle solid)
      {
         Process(solid);
      }

      /// <summary>
      /// Create an IFCSweptDiskSolid object from a handle of type IfcSweptDiskSolid.
      /// </summary>
      /// <param name="ifcSolid">The IFC handle.</param>
      /// <returns>The IFCSweptDiskSolid object.</returns>
      public static IFCSweptDiskSolid ProcessIFCSweptDiskSolid(IFCAnyHandle ifcSolid)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSolid))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcSweptDiskSolid);
            return null;
         }

         IFCEntity solid;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSolid.StepId, out solid))
            solid = new IFCSweptDiskSolid(ifcSolid);
         return (solid as IFCSweptDiskSolid);
      }
   }
}