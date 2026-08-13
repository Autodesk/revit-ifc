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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
   public class DoorWindowInfo
   {
      const double ShortDist = 1.0 / (16.0 * 12.0);
      const double tolForArcCenter = 1.0 / 2;

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

      public DoorWindowInfo()
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

      static readonly Dictionary<NamingUtil.IFCStringKey, string> ReverseDoorStyleOperations = new()
      {
         { new NamingUtil.IFCStringKey("DOUBLESWINGLEFT"), "DOUBLE_SWING_RIGHT" },
         { new NamingUtil.IFCStringKey("DOUBLESWINGRIGHT"), "DOUBLE_SWING_LEFT" },
         { new NamingUtil.IFCStringKey("FOLDINGTOLEFT"), "FOLDING_TO_RIGHT" },
         { new NamingUtil.IFCStringKey("FOLDINGTORIGHT"), "FOLDING_TO_LEFT"},
         { new NamingUtil.IFCStringKey("SINGLESWINGLEFT"), "SINGLE_SWING_RIGHT"},
         { new NamingUtil.IFCStringKey("SINGLESWINGRIGHT"), "SINGLE_SWING_LEFT" },
         { new NamingUtil.IFCStringKey("SLIDINGTOLEFT"), "SLIDING_TO_RIGHT" },
         { new NamingUtil.IFCStringKey("SLIDINGTORIGHT"), "SLIDING_TO_LEFT"}
      };

      static readonly Dictionary<NamingUtil.IFCStringKey, string> ReverseDoorStyleOperationsPre4Dot3 = new()
      {
         { new NamingUtil.IFCStringKey("DOUBLEDOORSINGLESWINGOPPOSITELEFT"), "DOUBLE_DOOR_SINGLE_SWING_OPPOSITE_RIGHT" },
         { new NamingUtil.IFCStringKey("DOUBLEDOORSINGLESWINGOPPOSITERIGHT"), "DOUBLE_DOOR_SINGLE_SWING_OPPOSITE_LEFT" },
         { new NamingUtil.IFCStringKey("DOUBLEPANELSINGLESWINGOPPOSITELEFT"), "DOUBLE_DOOR_SINGLE_SWING_OPPOSITE_RIGHT" },
         { new NamingUtil.IFCStringKey("DOUBLEPANELSINGLESWINGOPPOSITERIGHT"), "DOUBLE_DOOR_SINGLE_SWING_OPPOSITE_LEFT"}
      };

      static readonly Dictionary<NamingUtil.IFCStringKey, string> ReverseDoorStyleOperations4Dot3 = new()
      {
         { new NamingUtil.IFCStringKey("DOUBLEDOORSINGLESWINGOPPOSITELEFT"), "DOUBLE_PANEL_SINGLE_SWING_OPPOSITE_RIGHT" },
         { new NamingUtil.IFCStringKey("DOUBLEDOORSINGLESWINGOPPOSITERIGHT"), "DOUBLE_PANEL_SINGLE_SWING_OPPOSITE_LEFT" },
         { new NamingUtil.IFCStringKey("DOUBLEPANELSINGLESWINGOPPOSITELEFT"), "DOUBLE_PANEL_SINGLE_SWING_OPPOSITE_RIGHT" },
         { new NamingUtil.IFCStringKey("DOUBLEPANELSINGLESWINGOPPOSITERIGHT"), "DOUBLE_PANEL_SINGLE_SWING_OPPOSITE_LEFT"},
         { new NamingUtil.IFCStringKey("LIFTINGVERTICALLEFT"), "LIFTING_VERTICAL_RIGHT" },
         { new NamingUtil.IFCStringKey("LIFTINGVERTICALRIGHT"), "LIFTING_VERTICAL_LEFT"}
      };

      private string ReverseDoorStyleOperation(string orig)
      {
         NamingUtil.IFCStringKey compName = new(orig);
         if (ReverseDoorStyleOperations.TryGetValue(compName, out string reverse))
            return reverse;

         if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4x3)
         {
            if (ReverseDoorStyleOperationsPre4Dot3.TryGetValue(compName, out reverse))
               return reverse;
         }
         else
         {
            if (ReverseDoorStyleOperations4Dot3.TryGetValue(compName, out reverse))
               return reverse;
         }

         return orig;
      }

      private string CalculateDoorOperationStyle(FamilyInstance currElem, Transform trf)
      {
         // TODO: See if we can support new IFC4.3 types.

         int leftPosYArcCount = 0;
         int leftNegYArcCount = 0;
         int rightPosYArcCount = 0;
         int rightNegYArcCount = 0;
         int fullCircleCount = 0;
         int leftHalfCircleCount = 0;
         int rightHalfCircleCount = 0;
         double allowance = 0.0001;

         if (currElem == null)
            return "NOTDEFINED";

         FamilySymbol famSymbol = currElem.Symbol;
         if (famSymbol == null)
            return "NOTDEFINED";
         Family fam = famSymbol.Family;
         if (fam == null)
            return "NOTDEFINED";

         Transform doorWindowTrf = ExporterIFCUtils.GetTransformForDoorOrWindow(currElem, famSymbol, FlippedX, FlippedY);

         IList<Curve> origArcs = GeometryUtil.Get2DArcOrLineFromSymbol(currElem, allCurveType: false, inclArc: true);
         if ((origArcs?.Count ?? 0) == 0)
            return "NOTDEFINED";

         BoundingBoxXYZ doorBB = GetBoundingBoxFromSolids(currElem);

         // Translate curtain door origin to have proper 2D arc position
         Wall wall = HostObject as Wall;
         if ((wall?.WallType.Kind ?? WallKind.Unknown) == WallKind.Curtain)
         {
            Transform offsetOrigTrf = Transform.CreateTranslation(-XYZ.BasisX * doorBB.Min.X);
            doorWindowTrf = offsetOrigTrf.Multiply(doorWindowTrf);
         }

         XYZ bbMin = doorWindowTrf.OfPoint(doorBB.Min);
         XYZ bbMax = doorWindowTrf.OfPoint(doorBB.Max);

         // Reorganize the bbox min and max after transform
         double xmin = bbMin.X, xmax = bbMax.X, ymin = bbMin.Y, ymax = bbMax.Y, zmin = bbMin.Z, zmax = bbMax.Z;
         if (bbMin.X > bbMax.X)
         {
            xmin = bbMax.X;
            xmax = bbMin.X;
         }
         if (bbMin.Y > bbMax.Y)
         {
            ymin = bbMax.Y;
            ymax = bbMin.Y;
         }
         if (bbMin.Z > bbMax.Z)
         {
            zmin = bbMax.Z;
            zmax = bbMin.Z;
         }
         bbMin = new(xmin - tolForArcCenter, ymin - tolForArcCenter, zmin - tolForArcCenter);
         bbMax = new(xmax + tolForArcCenter, ymax + tolForArcCenter, zmax + tolForArcCenter);

         List<XYZ> arcCenterLocations = new();
         SortedSet<double> arcRadii = new();

         double angleTolerance = currElem.Document.Application.AngleTolerance * 180.0 / Math.PI;

         foreach (Arc arc in origArcs)
         {
            Arc trfArc = GeometryUtil.CreateTransformedCurve(arc, doorWindowTrf) as Arc;

            // Filter only Arcs that is on XY plane and at the Z=0 of the Door/Window transform
            if (!(MathUtil.IsAlmostEqual(Math.Abs(trfArc.Normal.Z), 1.0) /*&& MathUtil.IsAlmostEqual(Math.Abs(trfArc.Center.Z), Math.Abs(doorWindowTrf.Origin.Z))*/))
               continue;

            // Filter only Arcs that have center within the bounding box
            if (trfArc.Center.X > bbMax.X || trfArc.Center.X < bbMin.X || trfArc.Center.Y > bbMax.Y || trfArc.Center.Y < bbMin.Y)
               continue;

            if (!trfArc.IsBound)
            {
               fullCircleCount++;
            }
            else
            {
               XYZ v1 = CorrectNearlyZeroValueToZero((trfArc.GetEndPoint(0) - trfArc.Center).Normalize());
               XYZ v2 = CorrectNearlyZeroValueToZero((trfArc.GetEndPoint(1) - trfArc.Center).Normalize());
               double angleOffOfXYInRadians = MathUtil.SafeAcos(v1.DotProduct(v2));
               double absAngleOffOfXYInDegress = Math.Abs(angleOffOfXYInRadians * 180.0 / Math.PI);
               bool alignedToX = MathUtil.IsAlmostEqual(Math.Abs(v1.X), 1.0, allowance) || MathUtil.IsAlmostEqual(Math.Abs(v2.X), 1.0, allowance);

               if (absAngleOffOfXYInDegress is >= 60.0 and <= 240.0 && Math.Sign(v1.Y) != Math.Sign(v2.Y) &&
                  (!alignedToX || absAngleOffOfXYInDegress >= 90.0 + angleTolerance))
               {
                  // Consider the opening swing between -30 to +30 up to -120 to +120 degree,
                  // where Y axes must be at the opposite sides.
                  // If it is alignedToX, we don't consider this a "half circle" unless we are significantly
                  // above 90 degrees.
                  if (trfArc.Center.X >= -tolForArcCenter && trfArc.Center.X <= tolForArcCenter)
                     leftHalfCircleCount++;
                  else
                     rightHalfCircleCount++;
               }
               else if (absAngleOffOfXYInDegress is >= 30.0 and <= 170.0 && alignedToX)
               {
                  // Consider the opening swing between 30 to 170 degree, beginning at X axis
                  XYZ yDir = MathUtil.IsAlmostEqual(Math.Abs(v1.Y), Math.Abs(Math.Sin(angleOffOfXYInRadians)), 0.01) ? v1 : v2;

                  // if the Normal is pointing to -Z, it is flipped. Flip the Y if it is
                  if (MathUtil.IsAlmostEqual(trfArc.Normal.Z, -1.0))
                     yDir = yDir.Negate();

                  // Check the center location in the X-direction to determine LEFT/RIGHT
                  if (Math.Abs(trfArc.Center.X) <= tolForArcCenter)
                  {
                     // on the LEFT
                     if ((yDir.Y > 0.0 && trfArc.YDirection.Y > 0.0) || (yDir.Y < 0.0 && trfArc.YDirection.Y < 0.0))
                        leftPosYArcCount++;
                     else if ((yDir.Y > 0.0 && trfArc.YDirection.Y < 0.0) || (yDir.Y < 0.0 && trfArc.YDirection.Y > 0.0))
                        leftNegYArcCount++;
                     else
                        continue;
                  }
                  else
                  {
                     // on the RIGHT
                     if ((yDir.Y > 0.0 && trfArc.YDirection.Y > 0.0) || (yDir.Y < 0.0 && trfArc.YDirection.Y < 0.0))
                        rightPosYArcCount++;
                     else if ((yDir.Y > 0.0 && trfArc.YDirection.Y < 0.0) || (yDir.Y < 0.0 && trfArc.YDirection.Y > 0.0))
                        rightNegYArcCount++;
                     else
                        continue;
                  }
               }
               else
               {
                  continue;
               }

               // Collect all distinct Arc Center if it is counted as the door opening, to ensure that for cases that there are more than 2 leafs, it is not worngly labelled
               bool foundExisting = false;
               foreach (XYZ existingCenter in arcCenterLocations)
               {
                  if ((trfArc.Center.X > existingCenter.X - tolForArcCenter) && (trfArc.Center.X <= existingCenter.X + tolForArcCenter)
                     && (trfArc.Center.Y > existingCenter.Y - tolForArcCenter) && (trfArc.Center.Y <= existingCenter.Y + tolForArcCenter))
                  {
                     foundExisting = true;
                     break;
                  }
               }
               if (!foundExisting)
               {
                  arcCenterLocations.Add(trfArc.Center);
                  arcRadii.Add(trfArc.Radius);
               }
            }
         }

         // When only full circle(s) exists
         if (fullCircleCount > 0
               && rightHalfCircleCount == 0 && leftHalfCircleCount == 0 && leftPosYArcCount == 0 && leftNegYArcCount == 0 && rightPosYArcCount == 0 && rightNegYArcCount == 0)
            return "REVOLVING";

         // There are more than 2 arc centers, no IFC Door operation type fits this, return NOTDEFINED
         if (arcCenterLocations.Count > 2)
            return "NOTDEFINED";

         // When half circle arc(s) exists
         if (leftHalfCircleCount > 0 && fullCircleCount == 0)
         {
            if (rightHalfCircleCount == 0 && leftPosYArcCount == 0 && leftNegYArcCount == 0 && rightPosYArcCount == 0 && rightNegYArcCount == 0)
               return "DOUBLE_SWING_LEFT";

            if ((rightHalfCircleCount > 0 || (rightPosYArcCount > 0 && rightNegYArcCount > 0)) && leftPosYArcCount == 0 && leftNegYArcCount == 0)
               return "DOUBLE_DOOR_DOUBLE_SWING";
         }

         if (rightHalfCircleCount > 0 && fullCircleCount == 0)
         {
            if (leftHalfCircleCount == 0 && leftPosYArcCount == 0 && leftNegYArcCount == 0 && rightPosYArcCount == 0 && rightNegYArcCount == 0)
               return "DOUBLE_SWING_RIGHT";

            if ((leftHalfCircleCount > 0 || (leftPosYArcCount > 0 && leftNegYArcCount > 0)) && rightPosYArcCount == 0 && rightNegYArcCount == 0)
               return "DOUBLE_DOOR_DOUBLE_SWING";
         }

         // When only 90-degree arc(s) exists
         if (leftPosYArcCount > 0
               && fullCircleCount == 0 && rightHalfCircleCount == 0 && leftHalfCircleCount == 0 && leftNegYArcCount == 0 && rightPosYArcCount == 0 && rightNegYArcCount == 0)
         {
            // if the arc is less than 50%of the boundingbox, treat this to be a door with partially fixed panel
            if (arcRadii.Max < (bbMax.X - bbMin.X) * 0.5)
            {
               return ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 ? "NOTDEFINED" : "SWING_FIXED_LEFT";
            }
            return "SINGLE_SWING_LEFT";
         }

         if (rightPosYArcCount > 0
               && fullCircleCount == 0 && rightHalfCircleCount == 0 && leftHalfCircleCount == 0 && leftNegYArcCount == 0 && leftPosYArcCount == 0 && rightNegYArcCount == 0)
         {
            // if the arc is less than 50%of the boundingbox, treat this to be a door with partially fixed panel
            if (arcRadii.Max < (bbMax.X - bbMin.X) * 0.5)
            {
               return ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 ? "NOTDEFINED" : "SWING_FIXED_RIGHT";
            }
            return "SINGLE_SWING_RIGHT";
         }

         if (leftPosYArcCount > 0 && leftNegYArcCount > 0
               && fullCircleCount == 0 && rightHalfCircleCount == 0 && leftHalfCircleCount == 0 && rightPosYArcCount == 0 && rightNegYArcCount == 0)
            return "DOUBLE_SWING_LEFT";

         if (rightPosYArcCount > 0 && rightNegYArcCount > 0
               && fullCircleCount == 0 && rightHalfCircleCount == 0 && leftHalfCircleCount == 0 && leftNegYArcCount == 0 && leftPosYArcCount == 0)
            return "DOUBLE_SWING_RIGHT";

         if (leftPosYArcCount > 0 && rightPosYArcCount > 0
               && fullCircleCount == 0 && rightHalfCircleCount == 0 && leftHalfCircleCount == 0 && leftNegYArcCount == 0 && rightNegYArcCount == 0)
            return "DOUBLE_DOOR_SINGLE_SWING";

         if (leftPosYArcCount > 0 && rightPosYArcCount > 0 && leftNegYArcCount > 0 && rightNegYArcCount > 0
               && fullCircleCount == 0 && rightHalfCircleCount == 0 && leftHalfCircleCount == 0)
            return "DOUBLE_DOOR_DOUBLE_SWING";

         if (leftPosYArcCount > 0 && rightNegYArcCount > 0
               && fullCircleCount == 0 && rightHalfCircleCount == 0 && leftHalfCircleCount == 0 && leftNegYArcCount == 0 && rightPosYArcCount == 0)
            return "DOUBLE_DOOR_SINGLE_SWING_OPPOSITE_RIGHT";

         if (leftNegYArcCount > 0 && rightPosYArcCount > 0
               && fullCircleCount == 0 && rightHalfCircleCount == 0 && leftHalfCircleCount == 0 && leftPosYArcCount == 0 && rightNegYArcCount == 0)
            return "DOUBLE_DOOR_SINGLE_SWING_OPPOSITE_LEFT";

         return "NOTDEFINED";
      }

      private void Initialize(bool isDoor, bool isWindow, FamilyInstance famInst, HostObject hostObject, IFCExportInfoPair exportType)
      {
         HostObject = hostObject;
         InsertInstance = famInst;

         ExportingDoor = isDoor;
         if (isDoor)
         {
            PreDefinedType = exportType.GetPredefinedTypeOrDefault("DOOR");
         }

         ExportingWindow = isWindow;
         if (isWindow)
         {
            PreDefinedType = exportType.GetPredefinedTypeOrDefault("WINDOW");
         }

         FlippedSymbol = false;

         DoorOperationTypeString = "NOTDEFINED";
         WindowPartitioningTypeString = "NOTDEFINED";

         FlippedX = (famInst == null) ? false : famInst.HandFlipped;
         FlippedY = (famInst == null) ? false : famInst.FacingFlipped;

         Wall wall = (hostObject == null) ? null : hostObject as Wall;
         Curve centerCurve = WallExporter.GetWallAxis(wall);
         HasRealWallHost = ((wall != null) && (centerCurve != null) && ((centerCurve is Line) || (centerCurve is Arc)));
      }

      static readonly Dictionary<NamingUtil.IFCStringKey, string> DoorTypes = new()
      {
         { new NamingUtil.IFCStringKey("DOUBLEDOORFOLDING"), "DOUBLE_DOOR_FOLDING" },
         { new NamingUtil.IFCStringKey("DOUBLEDOORSINGLESWING"), "DOUBLE_DOOR_SINGLE_SWING" },
         { new NamingUtil.IFCStringKey("DOUBLEDOORSINGLESWINGOPPOSITELEFT"), "DOUBLE_DOOR_SINGLE_SWING_OPPOSITE_LEFT" },
         { new NamingUtil.IFCStringKey("DOUBLEDOORSINGLESWINGOPPOSITERIGHT"), "DOUBLE_DOOR_SINGLE_SWING_OPPOSITE_RIGHT" },
         { new NamingUtil.IFCStringKey("DOUBLEDOORSLIDING"), "DOUBLE_DOOR_SLIDING" },
         { new NamingUtil.IFCStringKey("DOUBLESWINGLEFT"), "DOUBLE_SWING_LEFT" },
         { new NamingUtil.IFCStringKey("DOUBLESWINGRIGHT"), "DOUBLE_SWING_RIGHT" },
         { new NamingUtil.IFCStringKey("DOUBLEDOORDOUBLESWING"), "DOUBLE_DOOR_DOUBLE_SWING" },
         { new NamingUtil.IFCStringKey("FOLDINGTOLEFT"), "FOLDING_TO_LEFT" },
         { new NamingUtil.IFCStringKey("FOLDINGTORIGHT"), "FOLDING_TO_RIGHT" },
         { new NamingUtil.IFCStringKey("NOTDEFINED"), "NOTDEFINED" },
         { new NamingUtil.IFCStringKey("REVOLVING"), "REVOLVING" },
         { new NamingUtil.IFCStringKey("ROLLINGUP"), "ROLLINGUP" },
         { new NamingUtil.IFCStringKey("SINGLESWINGLEFT"), "SINGLE_SWING_LEFT" },
         { new NamingUtil.IFCStringKey("SINGLESWINGRIGHT"), "SINGLE_SWING_RIGHT" },
         { new NamingUtil.IFCStringKey("SLIDINGTOLEFT"), "SLIDING_TO_LEFT" },
         { new NamingUtil.IFCStringKey("SLIDINGTORIGHT"), "SLIDING_TO_RIGHT" },
         { new NamingUtil.IFCStringKey("SWINGFIXEDLEFT"), "SWING_FIXED_LEFT" },
         { new NamingUtil.IFCStringKey("SWINGFIXEDRIGHT"), "SWING_FIXED_RIGHT" },
         { new NamingUtil.IFCStringKey("USERDEFINED"), "USERDEFINED" }
      };

      static readonly Dictionary<NamingUtil.IFCStringKey, string> DoorStyles = new()
      {
         { new NamingUtil.IFCStringKey("DOUBLEDOORFOLDING"), "DOUBLE_DOOR_FOLDING" },
         { new NamingUtil.IFCStringKey("DOUBLEDOORSINGLESWING"), "DOUBLE_DOOR_SINGLE_SWING" },
         { new NamingUtil.IFCStringKey("DOUBLEDOORSINGLESWINGOPPOSITELEFT"), "DOUBLE_DOOR_SINGLE_SWING_OPPOSITE_LEFT" },
         { new NamingUtil.IFCStringKey("DOUBLEDOORSINGLESWINGOPPOSITERIGHT"), "DOUBLE_DOOR_SINGLE_SWING_OPPOSITE_RIGHT" },
         { new NamingUtil.IFCStringKey("DOUBLEDOORSLIDING"), "DOUBLE_DOOR_SLIDING" },
         { new NamingUtil.IFCStringKey("DOUBLESWINGLEFT"), "DOUBLE_SWING_LEFT" },
         { new NamingUtil.IFCStringKey("DOUBLESWINGRIGHT"), "DOUBLE_SWING_RIGHT" },
         { new NamingUtil.IFCStringKey("FOLDINGTOLEFT"), "FOLDING_TO_LEFT" },
         { new NamingUtil.IFCStringKey("FOLDINGTORIGHT"), "FOLDING_TO_RIGHT" },
         { new NamingUtil.IFCStringKey("NOTDEFINED"), "NOTDEFINED" },
         { new NamingUtil.IFCStringKey("REVOLVING"), "REVOLVING" },
         { new NamingUtil.IFCStringKey("ROLLINGUP"), "ROLLINGUP" },
         { new NamingUtil.IFCStringKey("SINGLESWINGLEFT"), "SINGLE_SWING_LEFT" },
         { new NamingUtil.IFCStringKey("SINGLESWINGRIGHT"), "SINGLE_SWING_RIGHT" },
         { new NamingUtil.IFCStringKey("SLIDINGTOLEFT"), "SLIDING_TO_LEFT" },
         { new NamingUtil.IFCStringKey("SLIDINGTORIGHT"), "SLIDING_TO_RIGHT" },
         { new NamingUtil.IFCStringKey("USERDEFINED"), "USERDEFINED" }
      };

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
               (_, doorOperationType) = ParameterUtil.GetStringValueFromElementOrSymbol(doorType, null, false, "Operation");
               if (string.IsNullOrWhiteSpace(doorOperationType))
                  (_, doorOperationType) = ParameterUtil.GetStringValueFromElement(doorType, BuiltInParameter.DOOR_OPERATION_TYPE);
            }

            DoorOperationTypeString = null;
            if (!string.IsNullOrWhiteSpace(doorOperationType))
            {
               NamingUtil.IFCStringKey compName = new(doorOperationType);
               if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
               {
                  if (DoorTypes.TryGetValue(compName, out string doorTypeString))
                     DoorOperationTypeString = doorTypeString;
               }
               else
               {
                  if (DoorStyles.TryGetValue(compName, out string doorStyleString))
                     DoorOperationTypeString = doorStyleString;
               }
            }

            if (DoorOperationTypeString == null)
            {
               // We are going to try to guess the hinge placement.
               DoorOperationTypeString = CalculateDoorOperationStyle(famInst, trf);
            }
            else
            {
               if (FlippedX ^ FlippedY)
                  DoorOperationTypeString = ReverseDoorStyleOperation(DoorOperationTypeString);
            }

            if (string.Compare(DoorOperationTypeString, "USERDEFINED", true) == 0)
            {
               (_, string userDefinedOperationType) = ParameterUtil.GetStringValueFromElementOrSymbol(doorType, null, false, 
                  "UserDefinedOperationType");
               if (!string.IsNullOrEmpty(userDefinedOperationType))
                  UserDefinedOperationType = userDefinedOperationType;
               else
                  DoorOperationTypeString = "NOTDEFINED";         //re-set to NotDefined if operation type is set to UserDefined but the userDefinedOperationType parameter is empty!
            }

            if (RepresentationUtil.DocumentMirrorState.IsExportingMirroredLink())
               DoorOperationTypeString = ReverseDoorStyleOperation(DoorOperationTypeString);
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

               XYZ wallZDir = WallExporter.GetWallExtrusionDirection(wall);
               if (wallZDir == null)
                  wallZDir = XYZ.BasisZ; // Right thing here?

               // famInst.HostParameter will fail if FamilyPlacementType is WorkPlaneBased, regardless of whether or not the reported host is a Wall.
               // In this case, just use the start parameter of the curve.
               bool hasHostParameter = famInst.Symbol.Family.FamilyPlacementType != FamilyPlacementType.WorkPlaneBased;
               double param = hasHostParameter ? famInst.HostParameter : curve.GetEndParameter(0);

               Transform wallTrf = curve.ComputeDerivatives(param, false);
               XYZ wallOrig = wallTrf.Origin;
               XYZ wallXDir = wallTrf.BasisX;
               XYZ wallYDir = wallZDir.CrossProduct(wallXDir);

               double eps = MathUtil.Eps;

               bboxCtr -= wallOrig;
               PosHingeSide = (bboxCtr.DotProduct(wallYDir) > -eps);

               XYZ famInstYDir = trf.BasisY;
               FlippedSymbol = (PosHingeSide != (wallYDir.DotProduct(famInstYDir) > -eps));
            }
         }
      }

      public static DoorWindowInfo CreateDoor(ExporterIFC exporterIFC, FamilyInstance famInst, HostObject hostObj,
          ElementId overrideLevelId, Transform trf, IFCExportInfoPair exportType)
      {
         DoorWindowInfo doorWindowInfo = new DoorWindowInfo();
         doorWindowInfo.Initialize(true, false, famInst, hostObj, exportType);
         doorWindowInfo.CalculateDoorWindowInformation(exporterIFC, famInst, overrideLevelId, trf);

         return doorWindowInfo;
      }

      public static DoorWindowInfo CreateWindow(ExporterIFC exporterIFC, FamilyInstance famInst, HostObject hostObj,
          ElementId overrideLevelId, Transform trf, IFCExportInfoPair exportType)
      {
         DoorWindowInfo doorWindowInfo = new DoorWindowInfo();
         doorWindowInfo.Initialize(false, true, famInst, hostObj, exportType);
         doorWindowInfo.CalculateDoorWindowInformation(exporterIFC, famInst, overrideLevelId, trf);

         return doorWindowInfo;
      }

      XYZ CorrectNearlyZeroValueToZero(XYZ input)
      {
         double xVal = 0.0;
         double yVal = 0.0;
         double zVal = 0.0;

         if (!MathUtil.IsAlmostZero(input.X))
            xVal = input.X;

         if (!MathUtil.IsAlmostZero(input.Y))
            yVal = input.Y;

         if (!MathUtil.IsAlmostZero(input.Z))
            zVal = input.Z;

         return new XYZ(xVal, yVal, zVal);
      }

      BoundingBoxXYZ GetBoundingBoxFromSolids(FamilyInstance element)
      {
         BoundingBoxXYZ bbox = new BoundingBoxXYZ();

         double xmin = double.MaxValue;
         double ymin = double.MaxValue;
         double zmin = double.MaxValue;
         double xmax = double.MinValue;
         double ymax = double.MinValue;
         double zmax = double.MinValue;

         IList<Solid> arcList = new List<Solid>();
         GeometryElement geoms = element.Symbol.get_Geometry(GeometryUtil.GetIFCExportGeometryOptions());
         foreach (GeometryObject geomObj in geoms)
         {
            if (geomObj is Solid)
            {
               Solid geomSolid = geomObj as Solid;
               if (geomSolid.Volume == 0.0)
                  continue;

               BoundingBoxXYZ solidBB = geomSolid.GetBoundingBox();
               if (solidBB.Min.X < xmin)
                  xmin = solidBB.Min.X;
               if (solidBB.Max.X > xmax)
                  xmax = solidBB.Max.X;
               if (solidBB.Min.Y < ymin)
                  ymin = solidBB.Min.Y;
               if (solidBB.Max.Y > ymax)
                  ymax = solidBB.Max.Y;
               if (solidBB.Min.Z < zmin)
                  zmin = solidBB.Min.Z;
               if (solidBB.Max.Z > zmax)
                  zmax = solidBB.Max.Z;
            }
         }

         bbox.Min = new XYZ(xmin, ymin, zmin);
         bbox.Max = new XYZ(xmax, ymax, zmax);
         return bbox;
      }
   }
}