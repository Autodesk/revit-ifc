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

using TemporaryVerboseLogging = Revit.IFC.Import.Utility.IFCImportOptions.TemporaryVerboseLogging;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Class that represents IFCCompositeCurve entity
   /// </summary>
   public class IFCCompositeCurve : IFCBoundedCurve
   {
      private IList<IFCCurve> m_Segments = null;

      /// <summary>
      /// The list of curve segments for this IfcCompositeCurve.
      /// </summary>
      public IList<IFCCurve> Segments
      {
         get
         {
            if (m_Segments == null)
               m_Segments = new List<IFCCurve>();
            return m_Segments;
         }
      }

      protected IFCCompositeCurve()
      {
      }

      protected IFCCompositeCurve(IFCAnyHandle compositeCurve)
      {
         Process(compositeCurve);
      }

      /// <summary>
      /// Class to manage replacing tiny consecutive curve segments with a single line segment.
      /// </summary>
      class ShortGapRepairer
      {
         /// <summary>
         /// Constructor.
         /// </summary>
         /// <param name="shortCurveTolerance">Minimum allowed length for a Revit Curve.</param>
         public ShortGapRepairer(double shortCurveTolerance)
         {
            ShortCurveTolerance = shortCurveTolerance;
         }

         /// <summary>
         /// Add another short curve segment to a new or existing gap, and create a line segment if possible.
         /// </summary>
         /// <param name="startPoint">The start point of the new segment.</param>
         /// <param name="endPoint">The end point of the new segment.</param>
         /// <returns>A Revit Line, if the new segment makes the existing gap large enough to repair, or null.</returns>
         public Line AddToGap(int id, XYZ startPoint, XYZ endPoint)
         {
            if (startPoint == null || endPoint == null)
            {
               ClearGapInformation();
               return null;
            }

            if (StartGapPoint == null)
            {
               FirstSegmentStepId = id;
               StartGapPoint = startPoint;
               EndGapPoint = endPoint;
               return null;
            }

            double distGapEndPtToNextStartPt = EndGapPoint.DistanceTo(startPoint);
            double distGapEndPtToNextEndPt = EndGapPoint.DistanceTo(endPoint);

            double distGapStartPtToNextEndPt = StartGapPoint.DistanceTo(endPoint);
            double distGapStartPtToNextStartPt = StartGapPoint.DistanceTo(startPoint);

            double minStartGap = Math.Min(distGapStartPtToNextEndPt, distGapStartPtToNextStartPt);
            double minEndGap = Math.Min(distGapEndPtToNextStartPt, distGapEndPtToNextEndPt);

            double minGap = Math.Min(minStartGap, minEndGap);
            if (minGap > MathUtil.SmallGap())
            {
               ClearGapInformation();
               return null;
            }

            if (minEndGap < minStartGap)
            {
               if (distGapEndPtToNextStartPt < distGapEndPtToNextEndPt)
                  EndGapPoint = endPoint;
               else
                  EndGapPoint = startPoint;
            }
            else
            {
               if (distGapStartPtToNextStartPt < distGapStartPtToNextEndPt)
                  StartGapPoint = endPoint;
               else
                  StartGapPoint = startPoint;
            }

            if (StartGapPoint.DistanceTo(EndGapPoint) < ShortCurveTolerance + MathUtil.Eps())
               return null;

            Line gapLine = Line.CreateBound(StartGapPoint, EndGapPoint);
            ClearGapInformation(true);
            return gapLine;
         }

         /// <summary>
         /// Clear any existing gap information.
         /// </summary>
         /// <param name="dontShowError">If true, never show an error message.</param>
         /// <returns>True if any information was removed, false otherwise.</returns>
         public void ClearGapInformation(bool dontShowError = false)
         {
            StartGapPoint = null;
            EndGapPoint = null;
            if (!dontShowError && FirstSegmentStepId.HasValue)
               Importer.TheLog.LogError(FirstSegmentStepId.Value, "Curve segment is too short, ignoring.", false);
            FirstSegmentStepId = null;
         }

         private int? FirstSegmentStepId { get; set; } = null;

         private double ShortCurveTolerance { get; set; } = 0.0;

         private XYZ StartGapPoint { get; set; } = null;

         private XYZ EndGapPoint { get; set; } = null;
      }

      protected override void Process(IFCAnyHandle ifcCurve)
      {
         base.Process(ifcCurve);

         // We are going to attempt minor repairs for small but reasonable gaps between Line/Line and Line/Arc pairs.  As such, we want to collect the
         // curves before we create the curve loop.

         IList<IFCAnyHandle> segments = IFCAnyHandleUtil.GetValidAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcCurve, "Segments");
         if (segments == null)
            Importer.TheLog.LogError(Id, "Invalid IfcCompositeCurve with no segments.", true);

         double shortCurveTol = IFCImportFile.TheFile.Document.Application.ShortCurveTolerance;

         // need List<> so that we can AddRange later.
         List<Curve> curveSegments = new List<Curve>();
         Segments.Clear();

         ShortGapRepairer shortGapRepairer = new ShortGapRepairer(shortCurveTol);

         foreach (IFCAnyHandle segment in segments)
         {
            IFCCurve currCurve = ProcessIFCCompositeCurveSegment(segment);

            if (currCurve != null)
            {
               Segments.Add(currCurve);
               IList<Curve> newCurves = currCurve.GetCurves();
               if (newCurves != null && newCurves.Count != 0)
               {
                  curveSegments.AddRange(newCurves);
                  // If we had a gap, we weren't able to correct it before getting new curves.
                  shortGapRepairer.ClearGapInformation();
               }
               else
               {
                  Line gapRepairLine = shortGapRepairer.AddToGap(currCurve.Id,
                     currCurve.BackupCurveStartLocation, currCurve.BackupCurveEndLocation);
                  if (gapRepairLine != null)
                     curveSegments.Add(gapRepairLine);
               }
            }
         }

         int numSegments = curveSegments.Count;
         if (numSegments == 0)
            Importer.TheLog.LogError(Id, "Invalid IfcCompositeCurve with no segments.", true);

         try
         {
            // We are going to try to reverse or tweak segments as necessary to make the CurveLoop.
            // For each curve, it is acceptable if it can be appended to the end of the existing loop, or prepended to its start, 
            // possibly after reversing the curve, and possibly with some tweaking.

            // NOTE: we do not do any checks yet to repair the endpoints of the curveloop to make them closed.
            // NOTE: this is not expected to be perfect with dirty data, but is expected to not change already valid data.

            // curveLoopStartPoint and curveLoopEndPoint will change over time as we add new curves to the start or end of the CurveLoop.
            XYZ curveLoopStartPoint = curveSegments[0].GetEndPoint(0);
            XYZ curveLoopEndPoint = curveSegments[0].GetEndPoint(1);

            double vertexEps = IFCImportFile.TheFile.Document.Application.VertexTolerance;

            // This is intended to be "relatively large".  The idea here is that the user would rather have the information presented
            // than thrown away because of a gap that is architecturally insignificant.
            double gapVertexEps = Math.Max(vertexEps, 0.01); // 1/100th of a foot, or 3.048 mm.
            
            // canRepairFirst may change over time, as we may potentially add curves to the start of the CurveLoop.
            bool canRepairFirst = (curveSegments[0] is Line);
            for (int ii = 1; ii < numSegments; ii++)
            {
               XYZ nextStartPoint = curveSegments[ii].GetEndPoint(0);
               XYZ nextEndPoint = curveSegments[ii].GetEndPoint(1);

               // These will be set below.
               bool attachNextSegmentToEnd = false;
               bool reverseNextSegment = false;
               double minGap = 0.0;

               // Scoped to prevent distLoopEndPtToNextStartPt and others from being used later on.
               {
                  // Find the minimum gap between the current curve segment and the existing curve loop.  If it is too large, we will give up.
                  double distLoopEndPtToNextStartPt = curveLoopEndPoint.DistanceTo(nextStartPoint);
                  double distLoopEndPtToNextEndPt = curveLoopEndPoint.DistanceTo(nextEndPoint);

                  double distLoopStartPtToNextEndPt = curveLoopStartPoint.DistanceTo(nextEndPoint);
                  double distLoopStartPtToNextStartPt = curveLoopStartPoint.DistanceTo(nextStartPoint);

                  // Determine the minimum gap between the two curves.  If it is too large, we'll give up before trying anything.
                  double minStartGap = Math.Min(distLoopStartPtToNextEndPt, distLoopStartPtToNextStartPt);
                  double minEndGap = Math.Min(distLoopEndPtToNextStartPt, distLoopEndPtToNextEndPt);

                  minGap = Math.Min(minStartGap, minEndGap);

                  // If the minimum distance between the two curves is greater than gapVertexEps (which is the larger of our two tolerances), 
                  // we can't fix the issue.
                  if (minGap > gapVertexEps)
                  {
                     string lengthAsString = IFCUnitUtil.FormatLengthAsString(minGap);
                     string maxGapAsString = IFCUnitUtil.FormatLengthAsString(gapVertexEps);
                     throw new InvalidOperationException("IfcCompositeCurve contains a gap of " + lengthAsString +
                        " that is greater than the maximum gap size of " + maxGapAsString +
                        " and cannot be repaired.");
                  }

                  // We have a possibility to add the segment.  What we do depends on the gap distance.

                  // If the current curve loop's closest end to the next segment is its end (vs. start) point, set attachNextSegmentToEnd to true.
                  attachNextSegmentToEnd = (MathUtil.IsAlmostEqual(distLoopEndPtToNextStartPt, minGap)) ||
                     (MathUtil.IsAlmostEqual(distLoopEndPtToNextEndPt, minGap));

                  // We need to reverse the next segment if:
                  // 1. We are attaching the next segment to the end of the curve loop, and the next segment's closest end to the current curve loop is its end (vs. start) point.
                  // 2. We are attaching the next segment to the start of the curve loop, and the next segment's closest end to the current curve loop is its start (vs. end) point.
                  reverseNextSegment = (MathUtil.IsAlmostEqual(distLoopEndPtToNextEndPt, minGap)) ||
                     (MathUtil.IsAlmostEqual(distLoopStartPtToNextStartPt, minGap));
               }

               if (reverseNextSegment)
               {
                  curveSegments[ii] = curveSegments[ii].CreateReversed();
                  MathUtil.Swap<XYZ>(ref nextStartPoint, ref nextEndPoint);
               }

               // If minGap is less than vertexEps, we won't need to do any repairing - just fix the orientation if necessary.
               if (minGap < vertexEps)
               {
                  if (attachNextSegmentToEnd)
                  {
                     // Update the curve loop end point to be the end point of the next segment after potentially being reversed.
                     curveLoopEndPoint = nextEndPoint;
                  }
                  else
                  {
                     canRepairFirst = curveSegments[ii] is Line;
                     curveLoopStartPoint = nextStartPoint;

                     // Update the curve loop start point to be the start point of the next segment, now at the beginning of the loop,
                     // after potentially being reversed.
                     Curve tmpCurve = curveSegments[ii];
                     curveSegments.RemoveAt(ii);
                     curveSegments.Insert(0, tmpCurve);
                  }

                  continue;
               }

               // The gap is too big for CurveLoop, but smaller than our maximum tolerance - we will try to fix the gap by extending
               // one of the line segments around the gap.  If the gap is between two Arcs, we will try to introduce a short
               // segment between them, as long as the gap is larger than the short curve tolerance.

               bool canRepairNext = curveSegments[ii] is Line;
               bool createdRepairLine = false;

               if (attachNextSegmentToEnd)
               {
                  // Update the curve loop end point to be the end point of the next segment after potentially being reversed.
                  XYZ originalCurveLoopEndPoint = curveLoopEndPoint;
                  curveLoopEndPoint = nextEndPoint;
                  if (canRepairNext)
                     curveSegments[ii] = RepairLineAndReport(Id, originalCurveLoopEndPoint, curveLoopEndPoint, minGap);
                  else if (curveSegments[ii - 1] is Line)  // = canRepairCurrent, only used here.
                     curveSegments[ii - 1] = RepairLineAndReport(Id, curveSegments[ii - 1].GetEndPoint(0), curveSegments[ii].GetEndPoint(0), minGap);
                  else
                  {
                     // Can't add a line to fix a gap that is smaller than the short curve tolerance.
                     // This will result in mutliple curve loops.
                     if (minGap < shortCurveTol + MathUtil.Eps())
                     {
                        Importer.TheLog.LogError(Id, "IfcCompositeCurve contains a gap between two non-linear segments that is too short to be repaired by a connecting segment.", false);
                     }
                     else
                     {
                        try
                        {
                           Line repairLine = Line.CreateBound(originalCurveLoopEndPoint, curveSegments[ii].GetEndPoint(0));
                           curveSegments.Insert(ii, repairLine);
                           ii++; // Skip the repair line as we've already "added" it and the non-linear segment to our growing loop.
                           numSegments++;
                           createdRepairLine = true;
                        }
                        catch
                        {
                           Importer.TheLog.LogError(Id, "IfcCompositeCurve contains a gap between two non-linear segments that can't be fixed.", false);
                        }
                     }
                  }
               }
               else
               {
                  XYZ originalCurveLoopStartPoint = curveLoopStartPoint;
                  curveLoopStartPoint = nextStartPoint;

                  if (canRepairNext)
                  {
                     curveSegments[ii] = RepairLineAndReport(Id, curveLoopStartPoint, originalCurveLoopStartPoint, minGap);
                  }
                  else if (canRepairFirst)
                     curveSegments[0] = RepairLineAndReport(Id, curveSegments[ii].GetEndPoint(1), curveSegments[0].GetEndPoint(1), minGap);
                  else
                  {
                     // Can't add a line to fix a gap that is smaller than the short curve tolerance.
                     // In the future, we may fix this gap by intersecting the two curves and extending one of them.
                     if (minGap < shortCurveTol + MathUtil.Eps())
                        Importer.TheLog.LogError(Id, "IfcCompositeCurve contains a gap between two non-linear segments that is too short to be repaired by a connecting segment.", true);

                     Line repairLine = Line.CreateBound(curveSegments[ii].GetEndPoint(1), originalCurveLoopStartPoint);
                     curveSegments.Insert(0, repairLine);
                     ii++; // Skip the repair line as we've already "added" it and the non-linear curve to our growing loop.
                     numSegments++;
                  }

                  // Either canRepairFirst was already true, or canRepairNext was true and we added it to the front of the loop, 
                  // or we added a short repair line to the front of the loop.  In any of these cases, the front curve segement of the
                  // loop is now a line segment.
                  if (!canRepairFirst && !canRepairNext && !createdRepairLine)
                     Importer.TheLog.LogError(Id, "IfcCompositeCurve contains a gap between two non-linear segments that can't be fixed.", true);

                  canRepairFirst = true;

                  // Move the curve to the front of the loop.
                  Curve tmpCurve = curveSegments[ii];
                  curveSegments.RemoveAt(ii);
                  curveSegments.Insert(0, tmpCurve);
               }
            }

            SetCurveLoops(curveSegments);
         }
         catch (Exception ex)
         {
            Importer.TheLog.LogError(Id, ex.Message, true);
         }
      }

      /// <summary>
      /// Create an IFCCompositeCurve object from a handle of type IfcCompositeCurve
      /// </summary>
      /// <param name="ifcCompositeCurve">The IFC handle</param>
      /// <returns>The IFCCompositeCurve object</returns>
      public static IFCCompositeCurve ProcessIFCCompositeCurve(IFCAnyHandle ifcCompositeCurve)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcCompositeCurve))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcCompositeCurve);
            return null;
         }

         IFCEntity compositeCurve = null;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcCompositeCurve.StepId, out compositeCurve))
            compositeCurve = new IFCCompositeCurve(ifcCompositeCurve);

         return (compositeCurve as IFCCompositeCurve);
      }

      private IFCCurve ProcessIFCCompositeCurveSegment(IFCAnyHandle ifcCurveSegment)
      {
         bool found = false;

         bool sameSense = IFCImportHandleUtil.GetRequiredBooleanAttribute(ifcCurveSegment, "SameSense", out found);
         if (!found)
            sameSense = true;

         IFCAnyHandle ifcParentCurve = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcCurveSegment, "ParentCurve", true);
         IFCCurve parentCurve = null;

         using (TemporaryVerboseLogging logger = new TemporaryVerboseLogging())
         {
            parentCurve = IFCCurve.ProcessIFCCurve(ifcParentCurve);
         }

         if (parentCurve == null)
         {
            Importer.TheLog.LogWarning(ifcCurveSegment.StepId, "Error processing ParentCurve (#" + ifcParentCurve.StepId + ") for IfcCompositeCurveSegment; this may be repairable.", false);
            return null;
         }

         if (parentCurve.IsEmpty())
         {
            Importer.TheLog.LogWarning(ifcCurveSegment.StepId, "Error processing ParentCurve (#" + ifcParentCurve.StepId + ") for IfcCompositeCurveSegment; this may be repairable.", false);
            return null;
         }

         return parentCurve;
      }

      private Line RepairLineAndReport(int id, XYZ startPoint, XYZ endPoint, double gap)
      {
         string gapAsString = IFCUnitUtil.FormatLengthAsString(gap);
         Importer.TheLog.LogWarning(id, "Repaired gap of size " + gapAsString + " in IfcCompositeCurve.", false);
         return Line.CreateBound(startPoint, endPoint);
      }
   }
}