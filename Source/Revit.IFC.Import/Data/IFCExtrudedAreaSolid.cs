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
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public class IFCExtrudedAreaSolid : IFCSweptAreaSolid
   {
      /// <summary>
      /// The direction of the extrusion in the local coordinate system.
      /// </summary>
      public XYZ Direction { get; protected set; } = null;

      /// <summary>
      /// The depth of the extrusion, along the extrusion direction.
      /// </summary>
      public double Depth { get; protected set; } = 0.0;

      protected IFCExtrudedAreaSolid()
      {
      }

      override protected void Process(IFCAnyHandle solid)
      {
         base.Process(solid);

         // We will not fail if the direction is not given, but instead assume it to be normal to the swept area.
         IFCAnyHandle direction = IFCImportHandleUtil.GetRequiredInstanceAttribute(solid, "ExtrudedDirection", false);
         if (direction != null)
            Direction = IFCPoint.ProcessNormalizedIFCDirection(direction);
         else
            Direction = XYZ.BasisZ;

         bool found = false;
         Depth = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(solid, "Depth", out found);
         if (!found || MathUtil.IsAlmostZero(Depth))
         {
            string depthAsString = IFCUnitUtil.FormatLengthAsString(Depth);
            Importer.TheLog.LogError(solid.StepId, "extrusion depth of " + depthAsString + " is invalid, aborting.", true);
         }

         if (Depth < 0.0)
         {
            // Reverse depth and orientation.
            Depth = -Depth;
            Direction = -Direction;
            Importer.TheLog.LogWarning(solid.StepId, "negative extrusion depth is invalid, reversing direction.", false);
         }
      }

      /// <summary>
      /// Get the curve from the Axis representation of the given IfcProduct, transformed to the current local coordinate system.
      /// </summary>
      /// <param name="creator">The IfcProduct that may or may not contain a valid axis curve.</param>
      /// <param name="lcs">The local coordinate system.</param>
      /// <returns>The axis curve, if found, and valid.</returns>
      /// <remarks>In this case, we only allow bounded curves to be valid axis curves.
      /// The Curve may be contained as either a single Curve in the IFCCurve representation item,
      /// or it could be an open CurveLoop that could be represented as a single curve.</remarks>
      private Curve GetAxisCurve(IFCProduct creator, Transform lcs)
      {
         // We need an axis curve to clip the extrusion profiles; if we can't get one, fail
         IFCProductRepresentation productRepresentation = creator.ProductRepresentation;
         if (productRepresentation == null)
            return null;

         IList<IFCRepresentation> representations = productRepresentation.Representations;
         if (representations == null)
            return null;

         foreach (IFCRepresentation representation in representations)
         {
            // Go through the list of representations for this product, to find the Axis representation.
            if (representation == null || representation.Identifier != IFCRepresentationIdentifier.Axis)
               continue;

            IList<IFCRepresentationItem> items = representation.RepresentationItems;
            if (items == null)
               continue;

            // Go through the list of representation items in the Axis representation, to look for the IfcCurve.
            foreach (IFCRepresentationItem item in items)
            {
               if (item is IFCCurve)
               {
                  // We will accept a bounded curve, or an open CurveLoop that can be represented
                  // as one curve.
                  IFCCurve ifcCurve = item as IFCCurve;
                  Curve axisCurve = ifcCurve.Curve;
                  if (axisCurve == null)
                     axisCurve = ifcCurve.ConvertCurveLoopIntoSingleCurve();

                  if (axisCurve != null)
                     return axisCurve.CreateTransformed(lcs);
               }
            }
         }

         return null;
      }

      // Determines if two curves are oriented in generally opposite directions. Currently only handles lines and arcs.
      // This is intended to determine if two curves have reverse parametrization, so isn't intended to be exhaustive.
      private bool? CurvesHaveOppositeOrientation(Curve firstCurve, Curve secondCurve)
      {
         if (firstCurve == null || secondCurve == null)
            return null;

         if ((firstCurve is Line) && (secondCurve is Line))
            return ((firstCurve as Line).Direction.DotProduct((secondCurve as Line).Direction) < 0.0);

         if ((firstCurve is Arc) && (secondCurve is Arc))
            return ((firstCurve as Arc).Normal.DotProduct((secondCurve as Arc).Normal) < 0.0);

         return null;
      }

      /// <summary>
      /// Returns a list of curves that represent the profile of an extrusion to be split into material layers, for simple cases.
      /// </summary>
      /// <param name="loops">The original CurveLoops representing the extrusion profile.</param>
      /// <param name="axisCurve">The axis curve used by IfcMaterialLayerUsage to place the IfcMaterialLayers.</param>
      /// <param name="offsetNorm">The normal used for calculating curve offsets.</param>
      /// <param name="offset">The offset distance from the axis curve to the material layers, as defined in the IfcMaterialLayerSetUsage "OffsetFromReferenceLine" parameter.</param>
      /// <param name="totalThickness">The total thickness of all of the generated material layers.</param>
      /// <returns>A list of curves oriented according to the axis curve.</returns>
      /// <remarks>The list will contain 4 curves in this order:
      /// 1. The curve (a bounded Line or an Arc) at the boundary of the first material layer, oriented in the same direction as the Axis curve.
      /// 2. The line, possibly slanted, representing an end cap and connecting the 1st curve to the 3rd curve.
      /// 3. The curve (of the same type as the first) at the boundary of the last material layer, oriented in the opposite direction as the Axis curve.
      /// 4. The line, possibly slanted, representing an end cap and connecting the 3rd curve to the 1st curve.
      /// Over time, we may increase the number of cases suported.</remarks>
      private IList<Curve> GetOrientedCurveList(IList<CurveLoop> loops, Curve axisCurve, XYZ offsetNorm, double offset, double totalThickness)
      {
         // We are going to limit our attempts to a fairly simple but common case:
         // 1. 2 bounded curves parallel to the axis curve, of the same type, and either Lines or Arcs.
         // 2. 2 other edges connecting the two other curves, which are Lines.
         // This covers most cases and gets rid of issues with highly irregular edges.
         if (loops.Count() != 1 || loops[0].Count() != 4)
            return null;

         // The CreateOffset routine works opposite what IFC expects.  For a line, the Revit API offset direction
         // is the (line tangent) X (the normal of the plane containing the line).  In IFC, the (local) line tangent is +X,
         // the local normal of the plane is +Z, and the positive direction of the offset is +Y.  As such, we need to
         // reverse the normal of the plane to offset in the right direction.
         Curve offsetAxisCurve = axisCurve.CreateOffset(offset, -offsetNorm);

         Curve unboundOffsetAxisCurve = offsetAxisCurve.Clone();
         unboundOffsetAxisCurve.MakeUnbound();

         IList<Curve> originalCurveList = new List<Curve>();
         IList<Curve> unboundCurveList = new List<Curve>();
         foreach (Curve loopCurve in loops[0])
         {
            originalCurveList.Add(loopCurve);
            Curve unboundCurve = loopCurve.Clone();
            unboundCurve.MakeUnbound();
            unboundCurveList.Add(unboundCurve);
         }

         int startIndex = -1;
         bool flipped = false;
         for (int ii = 0; ii < 4; ii++)
         {
            // The offset axis curve should match one of the curves of the extrusion profile.  
            // We check that here by seeing if a point on the offset axis curve is on the current unbounded curve.
            if (unboundCurveList[ii].Intersect(unboundOffsetAxisCurve) != SetComparisonResult.Overlap &&
                MathUtil.IsAlmostZero(unboundCurveList[ii].Distance(offsetAxisCurve.GetEndPoint(0))))
            {
               startIndex = ii;

               Transform originalCurveLCS = originalCurveList[ii].ComputeDerivatives(0.0, true);
               Transform axisCurveLCS = axisCurve.ComputeDerivatives(0.0, true);

               // We want the first curve to have the same orientation as the axis curve. We will flip the resulting
               // "curve loop" if not.
               bool? maybeFlipped = CurvesHaveOppositeOrientation(originalCurveList[ii], axisCurve);
               if (!maybeFlipped.HasValue)
                  return null;

               flipped = maybeFlipped.Value;

               // Now check that startIndex and startIndex+2 are parallel, and totalThickness apart.
               if ((unboundCurveList[ii].Intersect(unboundCurveList[(ii + 2) % 4]) == SetComparisonResult.Overlap) ||
                   !MathUtil.IsAlmostEqual(unboundCurveList[ii].Distance(originalCurveList[(ii + 2) % 4].GetEndPoint(0)), totalThickness))
                  return null;

               break;
            }
         }

         // We may want to consider loosening the IsAlmostEqual check above if this fails a lot for seemingly good cases.
         if (startIndex == -1)
            return null;

         IList<Curve> orientedCurveList = new List<Curve>();
         for (int ii = 0, currentIndex = startIndex; ii < 4; ii++)
         {
            Curve currentCurve = originalCurveList[currentIndex];
            if (flipped)
               currentCurve = currentCurve.CreateReversed();
            orientedCurveList.Add(currentCurve);
            currentIndex = flipped ? (currentIndex + 3) % 4 : (currentIndex + 1) % 4;
         }
         return orientedCurveList;
      }

      // This routine may return null geometry for one of three reasons:
      // 1. Invalid input.
      // 2. No IfcMaterialLayerUsage.
      // 3. The IfcMaterialLayerUsage isn't handled.
      // If the reason is #1 or #3, we want to warn the user.  If it is #2, we don't.  Pass back shouldWarn to let the caller know.
      private IList<GeometryObject> CreateGeometryFromMaterialLayerUsage(IFCImportShapeEditScope shapeEditScope, Transform extrusionPosition,
          IList<CurveLoop> loops, XYZ extrusionDirection, double currDepth, out ElementId materialId, out bool shouldWarn)
      {
         IList<GeometryObject> extrusionSolids = null;
         materialId = ElementId.InvalidElementId;

         try
         {
            shouldWarn = true;  // Invalid input.

            // Check for valid input.
            if (shapeEditScope == null ||
                extrusionPosition == null ||
                loops == null ||
                loops.Count() == 0 ||
                extrusionDirection == null ||
                !extrusionPosition.IsConformal ||
                !Application.IsValidThickness(currDepth))
               return null;

            IFCProduct creator = shapeEditScope.Creator;
            if (creator == null)
               return null;

            shouldWarn = false;  // Missing, empty, or optimized out IfcMaterialLayerSetUsage - valid reason to stop.

            IIFCMaterialSelect materialSelect = creator.MaterialSelect;
            if (materialSelect == null)
               return null;

            IFCMaterialLayerSetUsage materialLayerSetUsage = materialSelect as IFCMaterialLayerSetUsage;
            if (materialLayerSetUsage == null)
               return null;

            IFCMaterialLayerSet materialLayerSet = materialLayerSetUsage.MaterialLayerSet;
            if (materialLayerSet == null)
               return null;

            IList<IFCMaterialLayer> materialLayers = materialLayerSet.MaterialLayers;
            if (materialLayers == null || materialLayers.Count == 0)
               return null;

            // Optimization: if there is only one layer, use the standard method, with possibly an overloaded material.
            ElementId baseMaterialId = GetMaterialElementId(shapeEditScope);
            if (materialLayers.Count == 1)
            {
               IFCMaterial oneMaterial = materialLayers[0].Material;
               if (oneMaterial == null)
                  return null;

               materialId = oneMaterial.GetMaterialElementId();
               if (materialId != ElementId.InvalidElementId)
               {
                  // We will not override the material of the element if the layer material has no color.
                  if (Importer.TheCache.MaterialsWithNoColor.Contains(materialId))
                     materialId = ElementId.InvalidElementId;
               }

               return null;
            }

            // Anything below here is something we should report to the user, with the exception of the total thickness
            // not matching the extrusion thickness.  This would require more analysis to determine that it is actually
            // an error condition.
            shouldWarn = true;

            IList<IFCMaterialLayer> realMaterialLayers = new List<IFCMaterialLayer>();
            double totalThickness = 0.0;
            foreach (IFCMaterialLayer materialLayer in materialLayers)
            {
               double depth = materialLayer.LayerThickness;
               if (MathUtil.IsAlmostZero(depth))
                  continue;

               if (depth < 0.0)
                  return null;

               realMaterialLayers.Add(materialLayer);
               totalThickness += depth;
            }

            // Axis3 means that the material layers are stacked in the Z direction.  This is common for floor slabs.
            bool isAxis3 = (materialLayerSetUsage.Direction == IFCLayerSetDirection.Axis3);

            // For elements extruded in the Z direction, if the extrusion layers don't have the same thickness as the extrusion,
            // this could be one of two reasons:
            // 1. There is a discrepancy between the extrusion depth and the material layer set usage calculated depth.
            // 2. There are multiple extrusions in the body definition.
            // In either case, we will use the extrusion geometry over the calculated material layer set usage geometry.
            // In the future, we may decide to allow for case #1 by passing in a flag to allow for this.
            if (isAxis3 && !MathUtil.IsAlmostEqual(totalThickness, currDepth))
            {
               shouldWarn = false;
               return null;
            }

            int numLayers = realMaterialLayers.Count();
            if (numLayers == 0)
               return null;
            // We'll use this initial value for the Axis2 case, so read it here.
            double baseOffsetForLayer = materialLayerSetUsage.Offset;

            // Needed for Axis2 case only.  The axisCurve is the curve defined in the product representation representing
            // a base curve (an axis) for the footprint of the element.
            Curve axisCurve = null;

            // The oriented cuve list represents the 4 curves of supported Axis2 footprint in the following order:
            // 1. curve along length of object closest to the first material layer with the orientation of the axis curve
            // 2. connecting end curve
            // 3. curve along length of object closest to the last material layer with the orientation opposite of the axis curve
            // 4. connecting end curve.
            IList<Curve> orientedCurveList = null;

            if (!isAxis3)
            {
               // Axis2 means that the material layers are stacked inthe Y direction.  This is by definition for IfcWallStandardCase,
               // which has a local coordinate system whose Y direction is orthogonal to the length of the wall.
               if (materialLayerSetUsage.Direction == IFCLayerSetDirection.Axis2)
               {
                  axisCurve = GetAxisCurve(creator, extrusionPosition);
                  if (axisCurve == null)
                     return null;

                  orientedCurveList = GetOrientedCurveList(loops, axisCurve, extrusionPosition.BasisZ, baseOffsetForLayer, totalThickness);
                  if (orientedCurveList == null)
                     return null;
               }
               else
                  return null;    // Not handled.
            }

            extrusionSolids = new List<GeometryObject>();

            bool positiveOrientation = (materialLayerSetUsage.DirectionSense == IFCDirectionSense.Positive);

            // Always extrude in the positive direction for Axis2.
            XYZ materialExtrusionDirection = (positiveOrientation || !isAxis3) ? extrusionDirection : -extrusionDirection;

            // Axis2 repeated values.
            // The IFC concept of offset direction is reversed from Revit's.
            XYZ normalDirectionForAxis2 = positiveOrientation ? -extrusionPosition.BasisZ : extrusionPosition.BasisZ;
            bool axisIsCyclic = (axisCurve == null) ? false : axisCurve.IsCyclic;
            double axisCurvePeriod = axisIsCyclic ? axisCurve.Period : 0.0;

            Transform curveLoopTransform = Transform.Identity;

            IList<CurveLoop> currLoops = null;
            double depthSoFar = 0.0;

            for (int ii = 0; ii < numLayers; ii++)
            {
               IFCMaterialLayer materialLayer = materialLayers[ii];

               // Ignore 0 thickness layers.  No need to warn.
               double depth = materialLayer.LayerThickness;
               if (MathUtil.IsAlmostZero(depth))
                  continue;

               // If the thickness is non-zero but invalid, fail.
               if (!Application.IsValidThickness(depth))
                  return null;

               double extrusionDistance = 0.0;
               if (isAxis3)
               {
                  // Offset the curve loops if necessary, using the base extrusionDirection, regardless of the direction sense
                  // of the MaterialLayerSetUsage.
                  double offsetForLayer = positiveOrientation ? baseOffsetForLayer + depthSoFar : baseOffsetForLayer - depthSoFar;
                  if (!MathUtil.IsAlmostZero(offsetForLayer))
                  {
                     curveLoopTransform.Origin = offsetForLayer * extrusionDirection;

                     currLoops = new List<CurveLoop>();
                     foreach (CurveLoop loop in loops)
                     {
                        CurveLoop newLoop = CurveLoop.CreateViaTransform(loop, curveLoopTransform);
                        if (newLoop == null)
                           return null;

                        currLoops.Add(newLoop);
                     }
                  }
                  else
                     currLoops = loops;

                  extrusionDistance = depth;
               }
               else
               {
                  // startClipCurve, firstEndCapCurve, endClipCurve, secondEndCapCurve.
                  Curve[] outline = new Curve[4];
                  double[][] endParameters = new double[4][];

                  double startClip = depthSoFar;
                  double endClip = depthSoFar + depth;

                  outline[0] = orientedCurveList[0].CreateOffset(startClip, normalDirectionForAxis2);
                  outline[1] = orientedCurveList[1].Clone();
                  outline[2] = orientedCurveList[2].CreateOffset(totalThickness - endClip, normalDirectionForAxis2);
                  outline[3] = orientedCurveList[3].Clone();

                  for (int jj = 0; jj < 4; jj++)
                  {
                     outline[jj].MakeUnbound();
                     endParameters[jj] = new double[2];
                     endParameters[jj][0] = 0.0;
                     endParameters[jj][1] = 0.0;
                  }

                  // Trim/Extend the curves so that they make a closed loop.
                  for (int jj = 0; jj < 4; jj++)
                  {
                     IntersectionResultArray resultArray = null;
                     outline[jj].Intersect(outline[(jj + 1) % 4], out resultArray);
                     if (resultArray == null || resultArray.Size == 0)
                        return null;

                     int numResults = resultArray.Size;
                     if ((numResults > 1 && !axisIsCyclic) || (numResults > 2))
                        return null;

                     UV intersectionPoint = resultArray.get_Item(0).UVPoint;
                     endParameters[jj][1] = intersectionPoint.U;
                     endParameters[(jj + 1) % 4][0] = intersectionPoint.V;

                     if (numResults == 2)
                     {
                        // If the current result is closer to the end of the curve, keep it.
                        UV newIntersectionPoint = resultArray.get_Item(1).UVPoint;

                        int endParamIndex = (jj % 2);
                        double newParamToCheck = newIntersectionPoint[endParamIndex];
                        double oldParamToCheck = (endParamIndex == 0) ? endParameters[jj][1] : endParameters[(jj + 1) % 4][0];
                        double currentEndPoint = (endParamIndex == 0) ?
                            orientedCurveList[jj].GetEndParameter(1) : orientedCurveList[(jj + 1) % 4].GetEndParameter(0);

                        // Put in range of [-Period/2, Period/2].
                        double newDist = (currentEndPoint - newParamToCheck) % axisCurvePeriod;
                        if (newDist < -axisCurvePeriod / 2.0) newDist += axisCurvePeriod;
                        if (newDist > axisCurvePeriod / 2.0) newDist -= axisCurvePeriod;

                        double oldDist = (currentEndPoint - oldParamToCheck) % axisCurvePeriod;
                        if (oldDist < -axisCurvePeriod / 2.0) oldDist += axisCurvePeriod;
                        if (oldDist > axisCurvePeriod / 2.0) oldDist -= axisCurvePeriod;

                        if (Math.Abs(newDist) < Math.Abs(oldDist))
                        {
                           endParameters[jj][1] = newIntersectionPoint.U;
                           endParameters[(jj + 1) % 4][0] = newIntersectionPoint.V;
                        }
                     }
                  }

                  CurveLoop newCurveLoop = new CurveLoop();
                  for (int jj = 0; jj < 4; jj++)
                  {
                     if (endParameters[jj][1] < endParameters[jj][0])
                     {
                        if (!outline[jj].IsCyclic)
                           return null;
                        endParameters[jj][1] += Math.Floor(endParameters[jj][0] / axisCurvePeriod + 1.0) * axisCurvePeriod;
                     }

                     outline[jj].MakeBound(endParameters[jj][0], endParameters[jj][1]);
                     newCurveLoop.Append(outline[jj]);
                  }

                  currLoops = new List<CurveLoop>();
                  currLoops.Add(newCurveLoop);

                  extrusionDistance = currDepth;
               }

               // Determine the material id.
               IFCMaterial material = materialLayer.Material;
               ElementId layerMaterialId = (material == null) ? ElementId.InvalidElementId : material.GetMaterialElementId();

               // The second option here is really for Referencing.  Without a UI (yet) to determine whether to show the base
               // extusion or the layers for objects with material layer sets, we've chosen to display the base material if the layer material
               // has no color information.  This means that the layer is assigned the "wrong" material, but looks better on screen.
               // We will reexamine this decision (1) for the Open case, (2) if there is UI to toggle between layers and base extrusion, or
               // (3) based on user feedback.
               if (layerMaterialId == ElementId.InvalidElementId || Importer.TheCache.MaterialsWithNoColor.Contains(layerMaterialId))
                  layerMaterialId = baseMaterialId;

               SolidOptions solidOptions = new SolidOptions(layerMaterialId, shapeEditScope.GraphicsStyleId);

               // Create the extrusion for the material layer.
               GeometryObject extrusionSolid = GeometryCreationUtilities.CreateExtrusionGeometry(
                      currLoops, materialExtrusionDirection, extrusionDistance, solidOptions);

               if (extrusionSolid == null)
                  return null;

               extrusionSolids.Add(extrusionSolid);
               depthSoFar += depth;
            }
         }
         catch
         {
            // Ignore the specific exception, but let the user know there was a problem processing the IfcMaterialLayerSetUsage.
            shouldWarn = true;
            return null;
         }

         return extrusionSolids;
      }

      private GeometryObject CreateGeometryFromMaterialProfile(IFCImportShapeEditScope shapeEditScope,
         IList<CurveLoop> loops, XYZ extrusionDirection, double currDepth, SolidOptions solidOptions, out bool shouldWarn)
      {
         GeometryObject extrusionSolid = null;

         try
         {
            shouldWarn = true;   // invalid input

            IIFCMaterialSelect materialSelect = shapeEditScope.Creator.MaterialSelect;
            if (materialSelect == null)
               return null;

            IFCMaterialProfileSetUsage matProfSetUsage = materialSelect as IFCMaterialProfileSetUsage;
            if (matProfSetUsage == null)
               return null;

            IFCMaterialProfileSet matProfSet = matProfSetUsage.ForProfileSet;
            if (matProfSet == null)
               return null;

            IList<IFCMaterialProfile> matProfList = matProfSet.MaterialProfileSet;
            if (matProfList.Count == 0)
               return null;

            Transform transformByOffset = Transform.Identity;
            IList<CurveLoop> newloops = new List<CurveLoop>();

            ElementId materialId = null;
            foreach (IFCMaterialProfile matProf in matProfList)
            {
               if (this.SweptArea.Id == matProf.Profile.Id)
               {
                  // This is the same id (same profile), use the material name for creation of this geometry
                  IFCMaterial theMaterial = matProf.Material;
                  if (theMaterial != null)
                  {
                     materialId = theMaterial.GetMaterialElementId();
                     solidOptions.MaterialId = materialId;    // Override the original option if the profile has a specific material id
                  }

                  // Here we will handle special case if the Material Profile has Offset
                  if (matProf is IFCMaterialProfileWithOffsets)
                  {
                     IFCMaterialProfileWithOffsets matProfOffset = matProf as IFCMaterialProfileWithOffsets;
                     double startOffset = matProfOffset.OffsetValues[0];
                     double endOffset = 0;
                     if (matProfOffset.OffsetValues.Count > 1)
                        endOffset = matProfOffset.OffsetValues[1];

                     // To handle offset, we need to move the start point (extrusion position) to the startOffset value along the axis (extrusion direction)
                     // For the end offset, we will have to re-calculate the extrusion
                     currDepth = currDepth - startOffset + endOffset;
                     transformByOffset.Origin += startOffset * extrusionDirection;
                     foreach (CurveLoop loop in loops)
                     {
                        CurveLoop newLoop = CurveLoop.CreateViaTransform(loop, transformByOffset);
                        newloops.Add(newLoop);
                     }
                  }
                  break;
               }
            }

            if (newloops.Count == 0)
               extrusionSolid = GeometryCreationUtilities.CreateExtrusionGeometry(loops, extrusionDirection, currDepth, solidOptions);
            else
               extrusionSolid = GeometryCreationUtilities.CreateExtrusionGeometry(newloops, extrusionDirection, currDepth, solidOptions);
         }
         catch
         {
            // Ignore the specific exception, but let the user know there was a problem processing the IfcMaterialLayerSetUsage.
            shouldWarn = true;
            return null;
         }

         return extrusionSolid;
      }
      
      private GeometryObject GetMeshBackup(IFCImportShapeEditScope shapeEditScope, IList<CurveLoop> loops,
         XYZ scaledExtrusionDirection, double currDepth, string guid)
      {
         if (shapeEditScope.MustCreateSolid())
            return null;
            
         try
         {
            MeshFromGeometryOperationResult meshResult = TessellatedShapeBuilder.CreateMeshByExtrusion(
               loops, scaledExtrusionDirection, currDepth, GetMaterialElementId(shapeEditScope));

            // Will throw if mesh is not available
            Mesh mesh = meshResult.GetMesh();
            Importer.TheLog.LogError(Id, "Extrusion has an invalid definition for a solid; reverting to mesh.", false);

            return mesh;
         }
         catch
         {
            Importer.TheLog.LogError(Id, "Extrusion has an invalid definition for a solid or mesh, ignoring.", false);
         }

         return null;
      }

      /// <summary>
      /// Return geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The shape edit scope.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      /// <returns>One or more created geometries.</returns>
      /// <remarks>The scaledLcs is only partially supported in this routine; it allows scaling the depth of the extrusion,
      /// which is commonly found in ACA files.</remarks>
      protected override IList<GeometryObject> CreateGeometryInternal(
         IFCImportShapeEditScope shapeEditScope, Transform scaledLcs, string guid)
      {
         if (Direction == null)
         {
            Importer.TheLog.LogError(Id, "Error processing IfcExtrudedAreaSolid, can't create geometry.", false);
            return null;
         }

         Transform origScaledLCS = (scaledLcs == null) ? Transform.Identity : scaledLcs;

         Transform scaledExtrusionPosition = (Position == null) ? origScaledLCS : origScaledLCS.Multiply(Position);

         XYZ scaledExtrusionDirection = scaledExtrusionPosition.OfVector(Direction);

         ISet<IList<CurveLoop>> disjointLoops = GetTransformedCurveLoops(scaledExtrusionPosition);
         if (disjointLoops == null || disjointLoops.Count() == 0)
            return null;

         IList<GeometryObject> extrusions = new List<GeometryObject>();
         double shortCurveTol = IFCImportFile.TheFile.ShortCurveTolerance;

         foreach (IList<CurveLoop> originalLoops in disjointLoops)
         {
            SolidOptions solidOptions = new SolidOptions(GetMaterialElementId(shapeEditScope), shapeEditScope.GraphicsStyleId);
            XYZ scaledDirection = scaledExtrusionPosition.OfVector(Direction);
            double currDepth = Depth * scaledDirection.GetLength();

            IList<CurveLoop> loops = new List<CurveLoop>();
            foreach (CurveLoop originalLoop in originalLoops)
            {
               if (!originalLoop.IsOpen())
               {
                  loops.Add(originalLoop);
                  continue;
               }

               int numOriginalCurves = originalLoop.Count();
               if (numOriginalCurves > 0)
               {
                  Curve firstSegment = originalLoop.First();
                  Curve lastSegment = originalLoop.Last();
                  Curve modifiedLastSegment = lastSegment;

                  XYZ startPoint = firstSegment.GetEndPoint(0);
                  XYZ endPoint = lastSegment.GetEndPoint(1);

                  double gap = endPoint.DistanceTo(startPoint);
                  if (gap < shortCurveTol)
                  {
                     // We will "borrow" some of the last segment to make space for the
                     // repair.  This could be done in a slightly better way, but this should
                     // be good enough for the cases we've seen.  If we need to improve
                     // the heuristic, we can.
                     IList<XYZ> lastPoints = lastSegment.Tessellate();
                     int count = lastPoints.Count();
                     for (int jj = count - 2; jj >= 0; jj--)
                     {
                        if (lastPoints[jj].DistanceTo(startPoint) < shortCurveTol)
                           continue;

                        try
                        {
                           if (jj > 0)
                           {
                              IntersectionResult result = lastSegment.Project(lastPoints[jj]);
                              modifiedLastSegment = lastSegment.Clone();
                              modifiedLastSegment.MakeBound(lastSegment.GetEndParameter(0), result.Parameter);
                           }
                           else
                           {
                              modifiedLastSegment = null;
                           }
                           endPoint = lastPoints[jj];
                           break;
                        }
                        catch
                        {
                        }
                     }
                  }

                  try
                  {
                     // We will attempt to close the loop to make it usable.
                     CurveLoop healedCurveLoop = null;
                     if (modifiedLastSegment == lastSegment)
                     {
                        healedCurveLoop = CurveLoop.CreateViaCopy(originalLoop);
                     }
                     else
                     {
                        int loopIndex = 0;
                        healedCurveLoop = new CurveLoop();
                        foreach (Curve originalCurve in originalLoop)
                        {
                           if (loopIndex < numOriginalCurves - 1)
                           {
                              healedCurveLoop.Append(originalCurve);
                              loopIndex++;
                              continue;
                           }

                           if (modifiedLastSegment == null)
                              break;

                           healedCurveLoop.Append(modifiedLastSegment);
                        }
                     }

                     Line closingLine = Line.CreateBound(endPoint, startPoint);
                     healedCurveLoop.Append(closingLine);
                     loops.Add(healedCurveLoop);
                     Importer.TheLog.LogWarning(Id, "Extrusion has an open profile loop, fixing.", false);
                     continue;
                  }
                  catch
                  {
                  }
               }

               Importer.TheLog.LogError(Id, "Extrusion has an open profile loop, ignoring.", false);
            }

            if (loops.Count == 0)
               continue;

            GeometryObject extrusionObject = null;
            try
            {
               // We may try to create separate extrusions, one per layer here.
               bool shouldWarn = false;
               ElementId overrideMaterialId = ElementId.InvalidElementId;

               if (shapeEditScope.Creator.MaterialSelect != null)
               {
                  if (shapeEditScope.Creator.MaterialSelect is IFCMaterialLayerSetUsage)
                  {
                     IList<GeometryObject> extrusionLayers = CreateGeometryFromMaterialLayerUsage(shapeEditScope, scaledExtrusionPosition, loops,
                        scaledExtrusionDirection, currDepth, out overrideMaterialId, out shouldWarn);
                     if (extrusionLayers == null || extrusionLayers.Count == 0)
                     {
                        if (shouldWarn)
                           Importer.TheLog.LogWarning(Id, "Couldn't process associated IfcMaterialLayerSetUsage, using body geometry instead.", false);
                        if (overrideMaterialId != ElementId.InvalidElementId)
                           solidOptions.MaterialId = overrideMaterialId;
                        extrusionObject = GeometryCreationUtilities.CreateExtrusionGeometry(loops, scaledExtrusionDirection, currDepth, solidOptions);
                     }
                     else
                     {
                        foreach (GeometryObject extrusionLayer in extrusionLayers)
                           extrusions.Add(extrusionLayer);
                     }
                  }
                  else if (shapeEditScope.Creator.MaterialSelect is IFCMaterialProfileSetUsage)
                  {
                     extrusionObject = CreateGeometryFromMaterialProfile(shapeEditScope, loops, scaledExtrusionDirection, currDepth, solidOptions, out shouldWarn);
                     if (extrusionObject == null)
                        extrusionObject = GeometryCreationUtilities.CreateExtrusionGeometry(loops, scaledExtrusionDirection, currDepth, solidOptions);
                  }
                  else
                  {
                     extrusionObject = GeometryCreationUtilities.CreateExtrusionGeometry(loops, scaledExtrusionDirection, currDepth, solidOptions);
                  }
               }
               else
               {
                  extrusionObject = GeometryCreationUtilities.CreateExtrusionGeometry(loops, scaledExtrusionDirection, currDepth, solidOptions);
               }
            }
            catch (Exception ex)
            {
               extrusionObject = GetMeshBackup(shapeEditScope, loops, scaledExtrusionDirection,
                  currDepth, guid);
               if (extrusionObject == null)
                  throw ex;
            }

            if (extrusionObject != null)
            {
               if (!(extrusionObject is Solid) || IFCGeometryUtil.ValidateGeometry(extrusionObject as Solid))
               {
                  extrusions.Add(extrusionObject);
               }
               else
               {
                  GeometryObject meshBackup = GetMeshBackup(shapeEditScope, loops, 
                     scaledExtrusionDirection, currDepth, guid);
                  if (meshBackup != null)
                     extrusions.Add(meshBackup);
               }
            }
         }

         return extrusions;
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

         IList<GeometryObject> extrudedGeometries = CreateGeometryInternal(shapeEditScope, scaledLcs, guid);
         if (extrudedGeometries != null)
         {
            foreach (GeometryObject extrudedGeometry in extrudedGeometries)
            {
               shapeEditScope.AddGeometry(IFCSolidInfo.Create(Id, extrudedGeometry));
            }
         }
      }

      protected IFCExtrudedAreaSolid(IFCAnyHandle solid)
      {
         Process(solid);
      }

      /// <summary>
      /// Create an IFCExtrudedAreaSolid object from a handle of type IfcExtrudedAreaSolid.
      /// </summary>
      /// <param name="ifcSolid">The IFC handle.</param>
      /// <returns>The IFCExtrudedAreaSolid object.</returns>
      public static IFCExtrudedAreaSolid ProcessIFCExtrudedAreaSolid(IFCAnyHandle ifcSolid)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSolid))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcExtrudedAreaSolid);
            return null;
         }

         IFCEntity solid;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSolid.StepId, out solid))
            solid = new IFCExtrudedAreaSolid(ifcSolid);
         return (solid as IFCExtrudedAreaSolid);
      }

      /// <summary>
      /// In case of a Boolean operation failure, provide a recommended direction to shift the geometry in for a second attempt.
      /// </summary>
      /// <param name="lcs">The local transform for this entity.</param>
      /// <returns>An XYZ representing a unit direction vector, or null if no direction is suggested.</returns>
      /// <remarks>If the 2nd attempt fails, a third attempt will be done with a shift in the opposite direction.</remarks>
      public override XYZ GetSuggestedShiftDirection(Transform lcs)
      {
         if (Position == null)
         {
            return (lcs == null) ? Direction : lcs.OfVector(Direction);
         }
         
         Transform extrusionLCS = (lcs == null) ? Position : lcs.Multiply(Position);
         return extrusionLCS.OfVector(Direction);
      }
   }
}