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
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Class that represents IFCIndexedPolyCurve entity
   /// </summary>
   public class IFCIndexedPolyCurve : IFCBoundedCurve
   {
      protected IFCIndexedPolyCurve()
      {
      }

      protected IFCIndexedPolyCurve(IFCAnyHandle indexedPolyCurve)
      {
         Process(indexedPolyCurve);
      }

      /// <summary>
      /// Check that an IFCData is properly formatted to potentially be an IfcSegmentIndexSelect.
      /// </summary>
      /// <param name="segment"></param>
      /// <returns>The type of IfcSegmentIndexSelect, or null if invalid.</returns>
      /// <remarks>The calling function is responsible for logging errors.</remarks>
      private string ValidateSegment(IFCData segment)
      {
         if (segment.PrimitiveType != IFCDataPrimitiveType.Aggregate)
         {
            return null;
         }

         if (!segment.HasSimpleType())
         {
            return null;
         }

         return segment.GetSimpleType();
      }

      private int? GetValidIndex(IFCData segmentInfoIndex, int maxValue)
      {
         // Index starts at 1.
         int currentIndex = segmentInfoIndex.AsInteger() - 1;
         if (currentIndex < 0 || currentIndex >= maxValue)
         {
            // TODO: warn.
            return null;
         }
         return currentIndex;
      }

      private void CreateLineSegments(CurveLoop curveLoop, IList<XYZ> currentSegments)
      {
         if (currentSegments.Count > 0)
         {
            IFCGeometryUtil.AppendPolyCurveToCurveLoop(curveLoop, currentSegments, null, Id, false);
            currentSegments.Clear();
         }
      }

      protected override void Process(IFCAnyHandle ifcCurve)
      {
         base.Process(ifcCurve);

         IFCAnyHandle points = IFCAnyHandleUtil.GetInstanceAttribute(ifcCurve, "Points");
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(points))
         {
            Importer.TheLog.LogMissingRequiredAttributeError(ifcCurve, "Points", true);
            return;
         }

         IList<IFCData> segments = null;
         try
         {
            // The Segments attribute is new to IFC4 Add1, and we don't know that we may have a 
            // vanilla IFC4 file.  If we can't find the attribute, we will assume the points represent 
            // the vertices of a polyline.
            segments = IFCAnyHandleUtil.GetAggregateAttribute<List<IFCData>>(ifcCurve, "Segments");
         }
         catch (Exception ex)
         {
            if (IFCImportFile.HasUndefinedAttribute(ex))
               IFCImportFile.TheFile.DowngradeIFC4SchemaTo(IFCSchemaVersion.IFC4);
            else
               throw ex;
         }

         IFCCartesianPointList pointList = IFCCartesianPointList.ProcessIFCCartesianPointList(points);
         IList<XYZ> pointListXYZs = pointList.CoordList;
         int numPoints = pointListXYZs.Count;
         
         CurveLoop curveLoop = null;
         IList<XYZ> pointXYZs = null;

         if (segments == null)
         {
            // Simple case: no segment information, just treat the curve as a polyline.
            pointXYZs = pointListXYZs;
            curveLoop = IFCGeometryUtil.CreatePolyCurveLoop(pointXYZs, null, Id, false);
         }
         else
         {
            curveLoop = new CurveLoop();

            // Assure that we don't add the same point twice for a polyline segment.  This could
            // happen by error, or, e.g., there are two IfcLineIndex segments in a row (although
            // this could also be considered an error condition.)
            int lastIndex = -1;

            // The list of all of the points, in the order that they are added.  This can be
            // used as a backup representation.
            pointXYZs = new List<XYZ>();

            IList<XYZ> currentLineSegmentPoints = new List<XYZ>();
            foreach (IFCData segment in segments)
            {
               string indexType = ValidateSegment(segment);
               if (indexType == null)
               {
                  Importer.TheLog.LogError(Id, "Unknown segment type in IfcIndexedPolyCurve.", false);
                  continue;
               }

               IFCAggregate segmentInfo = segment.AsAggregate();

               if (indexType.Equals("IfcLineIndex", StringComparison.OrdinalIgnoreCase))
               {
                  foreach (IFCData segmentInfoIndex in segmentInfo)
                  {
                     int? currentIndex = GetValidIndex(segmentInfoIndex, numPoints);
                     if (currentIndex == null)
                        continue;

                     int validCurrentIndex = currentIndex.Value;
                     if (lastIndex != validCurrentIndex)
                     {
                        pointXYZs.Add(pointListXYZs[validCurrentIndex]);
                        currentLineSegmentPoints.Add(pointListXYZs[validCurrentIndex]);
                        lastIndex = validCurrentIndex;
                     }
                  }
               }
               else if (indexType.Equals("IfcArcIndex", StringComparison.OrdinalIgnoreCase))
               {
                  // Create any line segments that haven't been already created.
                  CreateLineSegments(curveLoop, currentLineSegmentPoints);
                  
                  if (segmentInfo.Count != 3)
                  {
                     Importer.TheLog.LogError(Id, "Invalid IfcArcIndex in IfcIndexedPolyCurve.", false);
                     continue;
                  }

                  int? startIndex = GetValidIndex(segmentInfo[0], numPoints);
                  int? pointIndex = GetValidIndex(segmentInfo[1], numPoints);
                  int? endIndex = GetValidIndex(segmentInfo[2], numPoints);

                  if (startIndex == null || pointIndex == null || endIndex == null)
                     continue;

                  Arc arcSegment = null;
                  XYZ startPoint = pointListXYZs[startIndex.Value];
                  XYZ pointOnArc = pointListXYZs[pointIndex.Value];
                  XYZ endPoint = pointListXYZs[endIndex.Value];
                  try
                  {
                     arcSegment = Arc.Create(startPoint, pointOnArc, endPoint);
                     if (arcSegment != null)
                        curveLoop.Append(arcSegment);
                  }
                  catch
                  {
                     // We won't do anything here; it may be that the arc is very small, and can
                     // be repaired as a gap in the curve loop.  If it can't, this will fail later.
                     // We will monitor usage to see if anything more needs to be done here.
                  }

                  if (lastIndex != startIndex.Value)
                     pointXYZs.Add(startPoint);
                  pointXYZs.Add(pointOnArc);
                  pointXYZs.Add(endPoint);
                  lastIndex = endIndex.Value;
               }
               else
               {
                  Importer.TheLog.LogError(Id, "Unsupported segment type in IfcIndexedPolyCurve.", false);
                  continue;
               }
            }

            // Create any line segments that haven't been already created.
            CreateLineSegments(curveLoop, currentLineSegmentPoints);
         }

         SetCurveLoop(curveLoop, pointXYZs);
      }

      /// <summary>
      /// Create an IFCIndexedPolyCurve object from a handle of type IfcIndexedPolyCurve
      /// </summary>
      /// <param name="ifcIndexedPolyCurve">The IFC handle</param>
      /// <returns>The IFCIndexedPolyCurve object</returns>
      public static IFCIndexedPolyCurve ProcessIFCIndexedPolyCurve(IFCAnyHandle ifcIndexedPolyCurve)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcIndexedPolyCurve))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcIndexedPolyCurve);
            return null;
         }

         IFCEntity indexedPolyCurve = null;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcIndexedPolyCurve.StepId, out indexedPolyCurve))
            indexedPolyCurve = new IFCIndexedPolyCurve(ifcIndexedPolyCurve);

         return (indexedPolyCurve as IFCIndexedPolyCurve);
      }
   }
}