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
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
   public class DoorWindowInfo
   {
      const double ShortDist = 1.0 / (16.0 * 12.0);

      /// <summary>
      /// The host object associated with the door or window.
      /// </summary>
      public HostObject HostObject { get; protected set; }

      /// <summary>
      /// The door or window element.
      /// </summary>
      public FamilyInstance InsertInstance { get; protected set; }

      /// <summary>
      /// True if the HostObject is a real Revit wall.
      /// </summary>
      public bool HasRealWallHost { get; set; }

      /// <summary>
      /// True if we are exporting a door.
      /// </summary>
      public bool ExportingDoor { get; set; }

      /// <summary>
      /// True if we are exporting a window.
      /// </summary>
      public bool ExportingWindow { get; set; }

      /// <summary>
      /// True if the door or window element is flipped in the X direction.
      /// </summary>
      public bool FlippedX { get; set; }

      /// <summary>
      /// True if the door or window element is flipped in the Y direction.
      /// </summary>
      public bool FlippedY { get; set; }

      /// <summary>
      /// True if the door or window element is flipped.
      /// </summary>
      public bool FlippedSymbol { get; set; }

      /// <summary>
      /// True if the door or window hinge is on the "right-hand" side of the door.
      /// </summary>
      public bool PosHingeSide { get; set; }

      /// <summary>
      /// The Window partitioning type enum
      /// </summary>
      public string WindowPartitioningTypeString { get; set; }

      /// <summary>
      /// Captures user defined partitioning type from parameter
      /// </summary>
      public string UserDefinedPartitioningType { get; set; }

      /// <summary>
      /// The door operation type enum.
      /// </summary>
      public string DoorOperationTypeString { get; set; }

      /// <summary>
      /// Captures user defined operation type from parameter
      /// </summary>
      public string UserDefinedOperationType { get; set; }

      /// <summary>
      /// PreDefinedType Enum
      /// </summary>
      public string PreDefinedType { get; set; }

      private DoorWindowInfo()
      {
         HostObject = null;
         InsertInstance = null;
         HasRealWallHost = false;
         PosHingeSide = true;
         ExportingDoor = false;
         ExportingWindow = false;
         FlippedX = false;
         FlippedY = false;
         FlippedSymbol = false;
         DoorOperationTypeString = "NOTDEFINED";
         WindowPartitioningTypeString = "NOTDEFINED";
         UserDefinedOperationType = null;
         UserDefinedPartitioningType = null;
      }

      private KeyValuePair<double, double> GetAdjustedEndParameters(Arc arc, bool flipped, double offset)
      {
         double endParam0 = (flipped ? arc.GetEndParameter(1) : arc.GetEndParameter(0)) - offset;
         double endParam1 = (flipped ? arc.GetEndParameter(0) : arc.GetEndParameter(1)) - offset;
         double angle = endParam1 - endParam0;
         endParam0 = MathUtil.PutInRange(endParam0, Math.PI, 2 * Math.PI);
         endParam1 = endParam0 + angle;

         return new KeyValuePair<double, double>(endParam0, endParam1);
      }

      private string ReverseDoorStyleOperation(string orig)
      {
         if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(orig, "DoubleDoorSingleSwingOppositeLeft"))
            return "DOUBLE_DOOR_SINGLE_SWING_OPPOSITE_RIGHT";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(orig, "DoubleDoorSingleSwingOppositeRight"))
            return "DOUBLE_DOOR_SINGLE_SWING_OPPOSITE_LEFT";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(orig, "DoubleSwingLeft"))
            return "DOUBLE_SWING_RIGHT";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(orig, "DoubleSwingRight"))
            return "DOUBLE_SWING_LEFT";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(orig, "FoldingToLeft"))
            return "FOLDING_TO_RIGHT";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(orig, "FoldingToRight"))
            return "FOLDING_TO_LEFT";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(orig, "SingleSwingLeft"))
            return "SINGLE_SWING_RIGHT";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(orig, "SingleSwingRight"))
            return "SINGLE_SWING_LEFT";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(orig, "SlidingToLeft"))
            return "SLIDING_TO_RIGHT";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(orig, "SlidingToRight"))
            return "SLIDING_TO_LEFT";
         else
            return orig;
      }

      private string CalculateDoorOperationStyle(FamilyInstance currElem)
      {
         const double smallAngle = Math.PI / 36;

         if (currElem == null)
            return "NOTDEFINED";

         FamilySymbol famSymbol = currElem.Symbol;
         if (famSymbol == null)
            return "NOTDEFINED";
         Family fam = famSymbol.Family;
         if (fam == null)
            return "NOTDEFINED";

         IList<Arc> origArcs = ExporterIFCUtils.GetDoor2DArcsFromFamily(fam);
         if (origArcs == null || (origArcs.Count == 0))
            return "NOTDEFINED";

         IList<Arc> filteredArcs = new List<Arc>();
         IList<bool> flippedArcs = new List<bool>();
         IList<double> offsetAngles = new List<double>();
         foreach (Arc arc in origArcs)
         {
            XYZ zVec = arc.Normal;
            if (!MathUtil.IsAlmostEqual(Math.Abs(zVec.Z), 1.0))
               continue;

            double angleOffOfXY = 0;
            bool flipped = false;

            if (arc.IsBound)
            {
               flipped = MathUtil.IsAlmostEqual(Math.Abs(zVec.Z), -1.0);
               XYZ xVec = flipped ? -arc.XDirection : arc.XDirection;
               angleOffOfXY = Math.Atan2(xVec.Y, xVec.X);
            }

            filteredArcs.Add(arc);
            flippedArcs.Add(flipped);
            offsetAngles.Add(angleOffOfXY);
         }

         int numArcs = filteredArcs.Count;
         if (numArcs == 0)
            return "NOTDEFINED";

         double angleEps = ExporterCacheManager.Document.Application.AngleTolerance;

         if (numArcs == 1)
         {
            // single swing or revolving.
            if (!filteredArcs[0].IsBound)
               return "REVOLVING";

            KeyValuePair<double, double> endParams = GetAdjustedEndParameters(filteredArcs[0], flippedArcs[0], offsetAngles[0]);
            if ((endParams.Value - endParams.Key) <= Math.PI + angleEps)
            {
               if ((Math.Abs(endParams.Key) <= angleEps) || (Math.Abs(endParams.Key - Math.PI) <= angleEps))
                  return "SINGLE_SWING_LEFT";
               if ((Math.Abs(endParams.Value - Math.PI) <= angleEps) || (Math.Abs(endParams.Value - 2.0 * Math.PI) <= angleEps))
                  return "SINGLE_SWING_RIGHT";
            }
         }
         else if (numArcs == 2)
         {
            if (filteredArcs[0].IsBound && filteredArcs[1].IsBound)
            {
               XYZ ctrDiff = filteredArcs[1].Center - filteredArcs[0].Center;

               bool sameX = (Math.Abs(ctrDiff.X) < ShortDist);
               bool sameY = (Math.Abs(ctrDiff.Y) < ShortDist);

               if (sameX ^ sameY)
               {
                  KeyValuePair<double, double> endParams1 = GetAdjustedEndParameters(filteredArcs[0], flippedArcs[0], offsetAngles[0]);
                  double angle1 = endParams1.Value - endParams1.Key;
                  if (angle1 <= Math.PI + 2.0 * smallAngle)
                  {
                     KeyValuePair<double, double> endParams2 = GetAdjustedEndParameters(filteredArcs[1], flippedArcs[1], offsetAngles[1]);
                     double angle2 = endParams2.Value - endParams2.Key;
                     if (angle2 <= Math.PI + 2.0 * smallAngle)
                     {
                        if (sameX)
                        {
                           if (((Math.Abs(endParams1.Value - Math.PI) < smallAngle) && (Math.Abs(endParams2.Key - Math.PI) < smallAngle)) ||
                               ((Math.Abs(endParams1.Key - Math.PI) < smallAngle) && (Math.Abs(endParams2.Value - Math.PI) < smallAngle)))
                           {
                              return "DOUBLE_SWING_RIGHT";
                           }
                           else if (((Math.Abs(endParams1.Value - 2.0 * Math.PI) < smallAngle) && (Math.Abs(endParams2.Key) < smallAngle)) ||
                               ((Math.Abs(endParams1.Key) < smallAngle) && (Math.Abs(endParams2.Value - 2.0 * Math.PI) < smallAngle)))
                           {
                              return "DOUBLE_SWING_LEFT";
                           }
                        }
                        else // if (sameY)
                        {
                           return "DOUBLE_DOOR_SINGLE_SWING";
                        }
                     }
                  }
               }
            }
         }
         else if (numArcs == 4)
         {
            IList<XYZ> ctrs = new List<XYZ>();
            IList<KeyValuePair<double, double>> endParams = new List<KeyValuePair<double, double>>();
            bool canContinue = true;

            // "sort" by quadrant.
            IList<int> whichQuadrant = new List<int>();
            for (int ii = 0; ii < 4; ii++)
               whichQuadrant.Add(-1);

            for (int ii = 0; (ii < 4) && canContinue; ii++)
            {
               ctrs.Add(filteredArcs[ii].Center);
               if (filteredArcs[ii].IsBound)
               {
                  endParams.Add(GetAdjustedEndParameters(filteredArcs[ii], flippedArcs[ii], offsetAngles[ii]));
                  double angle = endParams[ii].Value - endParams[ii].Key;
                  if (angle > Math.PI + 2.0 * smallAngle)
                     canContinue = false;
                  else if ((Math.Abs(endParams[ii].Key) < smallAngle) && whichQuadrant[0] == -1)
                     whichQuadrant[0] = ii;
                  else if ((Math.Abs(endParams[ii].Value - Math.PI) < smallAngle) && whichQuadrant[1] == -1)
                     whichQuadrant[1] = ii;
                  else if ((Math.Abs(endParams[ii].Key - Math.PI) < smallAngle) && whichQuadrant[2] == -1)
                     whichQuadrant[2] = ii;
                  else if ((Math.Abs(endParams[ii].Value - 2.0 * Math.PI) < smallAngle) && whichQuadrant[3] == -1)
                     whichQuadrant[3] = ii;
                  else
                     canContinue = false;
               }
               else
                  canContinue = false;
            }

            if (canContinue)
            {
               XYZ ctrDiff1 = ctrs[whichQuadrant[3]] - ctrs[whichQuadrant[0]];
               XYZ ctrDiff2 = ctrs[whichQuadrant[2]] - ctrs[whichQuadrant[1]];
               XYZ ctrDiff3 = ctrs[whichQuadrant[1]] - ctrs[whichQuadrant[0]];

               if ((Math.Abs(ctrDiff1[0]) < ShortDist) &&
                   (Math.Abs(ctrDiff2[0]) < ShortDist) &&
                   (Math.Abs(ctrDiff3[1]) < ShortDist))
               {
                  return "DOUBLE_DOOR_DOUBLE_SWING";
               }
            }
         }

         return "NOTDEFINED";
      }

      private void Initialize(bool isDoor, bool isWindow, FamilyInstance famInst, HostObject hostObject)
      {
         HostObject = hostObject;
         InsertInstance = famInst;

         ExportingDoor = isDoor;
         if (isDoor)
            PreDefinedType = "DOOR";

         ExportingWindow = isWindow;
         if (isWindow)
            PreDefinedType = "WINDOW";

         FlippedSymbol = false;

         DoorOperationTypeString = "NOTDEFINED";
         WindowPartitioningTypeString = "NOTDEFINED";

         FlippedX = (famInst == null) ? false : famInst.HandFlipped;
         FlippedY = (famInst == null) ? false : famInst.FacingFlipped;

         Wall wall = (hostObject == null) ? null : hostObject as Wall;
         Curve centerCurve = WallExporter.GetWallAxis(wall);
         HasRealWallHost = ((wall != null) && (centerCurve != null) && ((centerCurve is Line) || (centerCurve is Arc)));
      }

      private void CalculateDoorWindowInformation(ExporterIFC exporterIFC, FamilyInstance famInst,
          ElementId overrideLevelId, Transform trf)
      {
         IFCFile file = exporterIFC.GetFile();

         if (ExportingDoor)
         {
            string doorOperationType = null;

            Element doorType = famInst.Document.GetElement(famInst.GetTypeId());
            if (doorType != null)
            {
               // Look at the "Operation" override first, then the built-in parameter.
               ParameterUtil.GetStringValueFromElementOrSymbol(doorType, "Operation", out doorOperationType);
               if (!string.IsNullOrWhiteSpace(doorOperationType))
                  ParameterUtil.GetStringValueFromElement(doorType, BuiltInParameter.DOOR_OPERATION_TYPE, out doorOperationType);
            }

            DoorOperationTypeString = "NOTDEFINED";
            if (!string.IsNullOrWhiteSpace(doorOperationType))
            {
               Type enumType = null;
               if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
                  enumType = typeof(Toolkit.IFC4.IFCDoorStyleOperation);
               else
                  enumType = typeof(Toolkit.IFCDoorStyleOperation);

               foreach (Enum ifcDoorStyleOperation in Enum.GetValues(enumType))
               {
                  string enumAsString = ifcDoorStyleOperation.ToString();
                  if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(enumAsString, doorOperationType))
                  {
                     DoorOperationTypeString = enumAsString;
                     break;
                  }
               }
            }

            if (DoorOperationTypeString == "NOTDEFINED")
            {
               // We are going to try to guess the hinge placement.
               DoorOperationTypeString = CalculateDoorOperationStyle(famInst);
            }

            if (FlippedX ^ FlippedY)
               DoorOperationTypeString = ReverseDoorStyleOperation(DoorOperationTypeString);

            if (String.Compare(DoorOperationTypeString, "USERDEFINED", true) == 0)
            {
               string userDefinedOperationType;
               ParameterUtil.GetStringValueFromElementOrSymbol(doorType, "UserDefinedOperationType", out userDefinedOperationType);
               if (!string.IsNullOrEmpty(userDefinedOperationType))
                  UserDefinedOperationType = userDefinedOperationType;
               else
                  DoorOperationTypeString = "NOTDEFINED";         //re-set to NotDefined if operation type is set to UserDefined but the userDefinedOperationType parameter is empty!
            }
         }

         if (HasRealWallHost)
         {
            // do hingeside calculation
            Wall wall = HostObject as Wall;
            PosHingeSide = true;

            BoundingBoxXYZ famBBox = null;
            Options options = GeometryUtil.GetIFCExportGeometryOptions();
            GeometryElement geomElement = famInst.GetOriginalGeometry(options);
            if (geomElement != null)
               famBBox = geomElement.GetBoundingBox();

            if (famBBox != null)
            {
               XYZ bboxCtr = trf.OfPoint((famBBox.Min + famBBox.Max) / 2.0);

               Curve curve = WallExporter.GetWallAxis(wall);

               XYZ wallZDir = WallExporter.GetWallHeightDirection(wall);

               // famInst.HostParameter will fail if FamilyPlacementType is WorkPlaneBased, regardless of whether or not the reported host is a Wall.
               // In this case, just use the start parameter of the curve.
               bool hasHostParameter = famInst.Symbol.Family.FamilyPlacementType != FamilyPlacementType.WorkPlaneBased;
               double param = hasHostParameter ? famInst.HostParameter : curve.GetEndParameter(0);

               Transform wallTrf = curve.ComputeDerivatives(param, false);
               XYZ wallOrig = wallTrf.Origin;
               XYZ wallXDir = wallTrf.BasisX;
               XYZ wallYDir = wallZDir.CrossProduct(wallXDir);

               double eps = MathUtil.Eps();

               bboxCtr -= wallOrig;
               PosHingeSide = (bboxCtr.DotProduct(wallYDir) > -eps);

               XYZ famInstYDir = trf.BasisY;
               FlippedSymbol = (PosHingeSide != (wallYDir.DotProduct(famInstYDir) > -eps));
            }
         }
      }

      public static DoorWindowInfo CreateDoor(ExporterIFC exporterIFC, FamilyInstance famInst, HostObject hostObj,
          ElementId overrideLevelId, Transform trf)
      {
         DoorWindowInfo doorWindowInfo = new DoorWindowInfo();
         doorWindowInfo.Initialize(true, false, famInst, hostObj);
         doorWindowInfo.CalculateDoorWindowInformation(exporterIFC, famInst, overrideLevelId, trf);

         return doorWindowInfo;
      }

      public static DoorWindowInfo CreateWindow(ExporterIFC exporterIFC, FamilyInstance famInst, HostObject hostObj,
          ElementId overrideLevelId, Transform trf)
      {
         DoorWindowInfo doorWindowInfo = new DoorWindowInfo();
         doorWindowInfo.Initialize(false, true, famInst, hostObj);
         doorWindowInfo.CalculateDoorWindowInformation(exporterIFC, famInst, overrideLevelId, trf);

         return doorWindowInfo;
      }
   }
}