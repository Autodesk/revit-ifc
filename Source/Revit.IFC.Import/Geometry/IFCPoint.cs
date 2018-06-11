﻿//
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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Data;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Geometry
{
   /// <summary>
   /// Provides methods to process IfcPoints and IfcDirection.
   /// IfcPoint can be of type IfcCartesianPoint, IfcPointOnCurve or IfcPointOnSurface.
   /// Only IfcCartesianPoint and IfcDirection is supported currently.
   /// </summary>
   public class IFCPoint
   {
      private enum IFCPointType
      {
         DontCare,
         UVPoint,
         XYZPoint
      }

      private static XYZ ListToXYZ(IList<double> coordinates)
      {
         int numCoordinates = coordinates.Count;

         switch (numCoordinates)
         {
            case 0: return new XYZ(0, 0, 0);
            case 1: return new XYZ(coordinates[0], 0, 0);
            case 2: return new XYZ(coordinates[0], coordinates[1], 0);
            default: return new XYZ(coordinates[0], coordinates[1], coordinates[2]);
         }
      }

      private static void AddToCaches(int stepId, IFCEntityType entityType, XYZ xyz)
      {
         if (xyz != null)
            IFCImportFile.TheFile.XYZMap[stepId] = xyz;
         Importer.TheLog.AddProcessedEntity(entityType);
      }

      // This routine does no validity checking on the point, but does on attributes.
      private static XYZ ProcessIFCCartesianPointInternal(IFCAnyHandle point, IFCPointType expectedCoordinates)
      {
         IList<double> coordinates = IFCAnyHandleUtil.GetCoordinates(point);
         int numCoordinates = coordinates.Count;
         if (numCoordinates < 2)
         {
            //LOG: Warning: Expected at least 2 coordinates for IfcCartesianPoint, found {numCoordinates}.
         }
         else if (numCoordinates > 3)
         {
            //LOG: Warning: Expected at most 3 coordinates for IfcCartesianPoint, found {numCoordinates}.
         }

         if (expectedCoordinates != IFCPointType.DontCare)
         {
            if ((expectedCoordinates == IFCPointType.UVPoint) && (numCoordinates != 2))
            {
               //LOG: Warning: Expected 2 coordinates for IfcCartesianPoint, found {numCoordinates}.
               if (numCoordinates > 2)
                  numCoordinates = 2;
            }
            else if ((expectedCoordinates == IFCPointType.XYZPoint) && (numCoordinates != 3))
            {
               //LOG: Warning: Expected 3 coordinates for IfcCartesianPoint, found {numCoordinates}.
               if (numCoordinates > 3)
                  numCoordinates = 3;
            }
         }

         return ListToXYZ(coordinates);
      }

      /// <summary>
      /// Converts an IfcCartesianPoint into a UV or XYZ value.
      /// </summary>
      /// <param name="point">The handle to the IfcPoint.</param>
      /// <returns>An XYZ value corresponding to the value in the file.  There are no transformations done in this routine.
      /// If the return is an XY point, the Z value will be set to 0.</returns>
      public static XYZ ProcessIFCCartesianPoint(IFCAnyHandle point)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(point))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcCartesianPoint);
            return null;
         }

         XYZ xyz;
         int stepId = point.StepId;
         if (IFCImportFile.TheFile.XYZMap.TryGetValue(stepId, out xyz))
            return xyz;

         if (IFCAnyHandleUtil.IsTypeOf(point, IFCEntityType.IfcCartesianPoint))
            xyz = ProcessIFCCartesianPointInternal(point, IFCPointType.DontCare);
         else
         {
            Importer.TheLog.LogUnhandledSubTypeError(point, IFCEntityType.IfcCartesianPoint, false);
            return null;
         }

         AddToCaches(stepId, IFCEntityType.IfcCartesianPoint, xyz);
         return xyz;
      }

      /// <summary>
      /// Get the XYZ corresponding to the IfcCartesianPoint, scaled by the length scale.
      /// </summary>
      /// <param name="point">The IfcCartesianPoint entity handle.</param>
      /// <returns>The scaled XYZ value.</returns>
      public static XYZ ProcessScaledLengthIFCCartesianPoint(IFCAnyHandle point)
      {
         XYZ xyz = ProcessIFCCartesianPoint(point);
         if (xyz != null)
            xyz = IFCUnitUtil.ScaleLength(xyz);
         return xyz;
      }

      /// <summary>
      /// Get the XYZ values corresponding to a list of IfcCartesianPoints, scaled by the length scale.
      /// </summary>
      /// <param name="points">The IfcCartesianPoint entity handles.</param>
      /// <returns>The scaled XYZ values.</returns>
      public static IList<XYZ> ProcessScaledLengthIFCCartesianPoints(IList<IFCAnyHandle> points)
      {
         if (points == null)
            return null;

         IList<XYZ> xyzs = new List<XYZ>();
         foreach (IFCAnyHandle point in points)
         {
            XYZ xyz = ProcessIFCCartesianPoint(point);
            if (xyz == null)
               continue;   // TODO: WARN
            xyzs.Add(xyz);
         }

         IFCUnitUtil.ScaleLengths(xyzs);
         return xyzs;
      }

      /// <summary>
      /// Converts an IfcCartesianPoint into an UV value.
      /// </summary>
      /// <param name="point">The handle to the IfcPoint.</param>
      /// <returns>A UV value corresponding to the value in the file.  There are no transformations done in this routine.</returns>
      public static UV ProcessIFCCartesianPoint2D(IFCAnyHandle point)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(point))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcCartesianPoint);
            return null;
         }

         XYZ xyz;
         int stepId = point.StepId;
         if (IFCImportFile.TheFile.XYZMap.TryGetValue(stepId, out xyz))
            return new UV(xyz.X, xyz.Y);

         if (IFCAnyHandleUtil.IsTypeOf(point, IFCEntityType.IfcCartesianPoint))
         {
            xyz = ProcessIFCCartesianPointInternal(point, IFCPointType.UVPoint);
         }
         else
         {
            Importer.TheLog.LogUnhandledSubTypeError(point, IFCEntityType.IfcCartesianPoint, false);
            return null;
         }

         AddToCaches(stepId, IFCEntityType.IfcCartesianPoint, xyz);
         return new UV(xyz.X, xyz.Y);
      }

      private static XYZ ProcessIFCPointInternal(IFCAnyHandle point, IFCPointType expectedCoordinates)
      {
         if (IFCAnyHandleUtil.IsTypeOf(point, IFCEntityType.IfcCartesianPoint))
            return ProcessIFCCartesianPointInternal(point, IFCPointType.DontCare);

         Importer.TheLog.LogUnhandledSubTypeError(point, IFCEntityType.IfcCartesianPoint, false);
         return null;
      }

      /// <summary>
      /// Converts an IfcPoint into a UV or XYZ value.
      /// </summary>
      /// <param name="point">The handle to the IfcPoint.</param>
      /// <returns>An XYZ value corresponding to the value in the file.  If the IfcPoint is 2D, the Z value will be 0.
      /// There are no transformations done in this routine.</returns>
      public static XYZ ProcessIFCPoint(IFCAnyHandle point)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(point))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcCartesianPoint);
            return null;
         }

         XYZ xyz;
         int stepId = point.StepId;
         if (IFCImportFile.TheFile.XYZMap.TryGetValue(stepId, out xyz))
            return xyz;

         xyz = ProcessIFCPointInternal(point, IFCPointType.DontCare);
         AddToCaches(stepId, IFCEntityType.IfcCartesianPoint, xyz);
         return xyz;
      }

      /// <summary>
      /// Converts an IfcPoint into an UV value.
      /// </summary>
      /// <param name="point">The handle to the IfcPoint.</param>
      /// <returns>A UV value corresponding to the value in the file.  There are no transformations done in this routine.</returns>
      public static UV ProcessIFCPoint2D(IFCAnyHandle point)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(point))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcCartesianPoint);
            return null;
         }

         XYZ xyz;
         int stepId = point.StepId;
         if (IFCImportFile.TheFile.XYZMap.TryGetValue(stepId, out xyz))
            return new UV(xyz.X, xyz.Y);

         xyz = ProcessIFCPointInternal(point, IFCPointType.UVPoint);
         if (xyz == null)
            return null;

         AddToCaches(stepId, IFCEntityType.IfcCartesianPoint, xyz);
         return new UV(xyz.X, xyz.Y);
      }

      /// <summary>
      /// Converts an IfcPoint into an XYZ value.
      /// </summary>
      /// <param name="point">The handle to the IfcPoint.</param>
      /// <returns>An XYZ value corresponding to the value in the file.  There are no transformations done in this routine.</returns>
      public static XYZ ProcessIFCPoint3D(IFCAnyHandle point)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(point))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcCartesianPoint);
            return null;
         }

         XYZ xyz;
         int stepId = point.StepId;
         if (IFCImportFile.TheFile.XYZMap.TryGetValue(stepId, out xyz))
            return xyz;

         xyz = ProcessIFCPointInternal(point, IFCPointType.XYZPoint);
         AddToCaches(stepId, IFCEntityType.IfcCartesianPoint, xyz);
         return xyz;
      }

      private static XYZ ProcessIFCDirectionBase(IFCAnyHandle direction, bool normalize)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(direction))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcDirection);
            return null;
         }

         if (!IFCAnyHandleUtil.IsSubTypeOf(direction, IFCEntityType.IfcDirection))
         {
            Importer.TheLog.LogUnexpectedTypeError(direction, IFCEntityType.IfcDirection, false);
            return null;
         }

         XYZ xyz = null;
         int stepId = direction.StepId;
         if (normalize)
         {
            if (IFCImportFile.TheFile.NormalizedXYZMap.TryGetValue(stepId, out xyz))
               return xyz;
         }
         else
         {
            if (IFCImportFile.TheFile.XYZMap.TryGetValue(stepId, out xyz))
               return xyz;
         }

         List<double> directionRatios = IFCAnyHandleUtil.GetAggregateDoubleAttribute<List<double>>(direction, "DirectionRatios");
         xyz = ListToXYZ(directionRatios);
         XYZ normalizedXYZ = null;
         if (xyz != null)
         {
            AddToCaches(stepId, IFCEntityType.IfcDirection, xyz);
            if (normalize)
            {
               normalizedXYZ = xyz.Normalize();
               if (normalizedXYZ.IsZeroLength())
               {
                  Importer.TheLog.LogError(stepId, "Local transform contains 0 length vectors", true);
               }
               IFCImportFile.TheFile.NormalizedXYZMap[direction.StepId] = normalizedXYZ;
            }
         }
         return normalize ? normalizedXYZ : xyz;
      }

      /// <summary>
      /// Converts an IfcDirection into a UV or XYZ value.
      /// </summary>
      /// <param name="direction">The handle to the IfcDirection.</param>
      /// <returns>An XYZ value corresponding to the value in the file.  There are no transformations done in this routine.
      /// If the return is an XY point, the Z value will be set to 0.</returns>
      public static XYZ ProcessIFCDirection(IFCAnyHandle direction)
      {
         return ProcessIFCDirectionBase(direction, false);
      }

      /// <summary>
      /// Converts an IfcDirection into a normalized UV or XYZ value.
      /// </summary>
      /// <param name="direction">The handle to the IfcDirection.</param>
      /// <returns>An XYZ value corresponding to the value in the file.  There are no transformations done in this routine.
      /// If the return is an XY point, the Z value will be set to 0.</returns>
      public static XYZ ProcessNormalizedIFCDirection(IFCAnyHandle direction)
      {
         return ProcessIFCDirectionBase(direction, true);
      }

      /// <summary>
      /// Converts an IfcVector into a UV or XYZ value.
      /// </summary>
      /// <param name="vector">The handle to the IfcVector.</param>
      /// <returns>An XYZ value corresponding to the value in the file.  There are no transformations done in this routine.
      /// If the return is an XY point, the Z value will be set to 0.</returns>
      public static XYZ ProcessIFCVector(IFCAnyHandle vector)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(vector))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcVector);
            return null;
         }

         if (!IFCAnyHandleUtil.IsSubTypeOf(vector, IFCEntityType.IfcVector))
         {
            Importer.TheLog.LogUnexpectedTypeError(vector, IFCEntityType.IfcVector, false);
            return null;
         }

         XYZ xyz;
         int stepId = vector.StepId;
         if (IFCImportFile.TheFile.XYZMap.TryGetValue(stepId, out xyz))
            return xyz;

         IFCAnyHandle direction = IFCImportHandleUtil.GetRequiredInstanceAttribute(vector, "Orientation", false);
         if (direction == null)
            return null;

         bool found = false;
         double magnitude = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(vector, "Magnitude", out found);
         if (!found)
            magnitude = 1.0;

         XYZ directionXYZ = IFCPoint.ProcessIFCDirection(direction);
         if (directionXYZ == null)
            return null;

         xyz = directionXYZ * magnitude;
         AddToCaches(stepId, IFCEntityType.IfcVector, xyz);
         return xyz;
      }
   }
}