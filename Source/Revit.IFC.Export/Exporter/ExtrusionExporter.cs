//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012-2016  Autodesk, Inc.
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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   public enum GenerateAdditionalInfo
   {
      GenerateFootprint = 0x001,    // for backward compatibility, generate footprint implies generate body
      GenerateProfileDef = 0x010,   // for backward compatibility, generate profiledef implies generate body
      GenerateBody = 0x100,          // explicit info to generate body. In IFC4RV if the body will be skipped, it will be set to ~0x100
      None = 0x00
   }

   /// <summary>
   /// Provides methods to export extrusions.
   /// </summary>
   class ExtrusionExporter
   {
      // This is intended to sort already known unique PlanarFaces by FaceNormal, so that
      // we can control the order that clip planes are created in.
      // If we do have multiple faces with the same normal, use Origin as a tie-breaker.
      // If we do have mutliple faces with the same normal and origin, the user will have to check for this condition.
      public class PlanarFaceClipPlaneComparer : IComparer<PlanarFace>
      {
         public int Compare(PlanarFace face1, PlanarFace face2)
         {
            if (face1 == null)
            {
               if (face2 == null)
                  return 0;
               else
                  return -1;
            }
            else if (face2 == null)
               return 1;
            else
            {
               // Check normal first.
               XYZ faceNormal1 = face1.FaceNormal;
               XYZ faceNormal2 = face2.FaceNormal;
               if (!MathUtil.IsAlmostEqual(faceNormal1.Z, faceNormal2.Z))
                  return faceNormal1.Z < faceNormal2.Z ? -1 : 1;
               if (!MathUtil.IsAlmostEqual(faceNormal1.X, faceNormal2.X))
                  return faceNormal1.X < faceNormal2.X ? -1 : 1;
               if (!MathUtil.IsAlmostEqual(faceNormal1.Y, faceNormal2.Y))
                  return faceNormal1.Y < faceNormal2.Y ? -1 : 1;

               // Unexpected (unless the faces are the same), but check origin if normal is the same.
               XYZ faceOrigin1 = face1.Origin;
               XYZ faceOrigin2 = face2.Origin;
               if (!MathUtil.IsAlmostEqual(faceOrigin1.Z, faceOrigin2.Z))
                  return faceOrigin1.Z < faceOrigin2.Z ? -1 : 1;
               if (!MathUtil.IsAlmostEqual(faceOrigin1.X, faceOrigin2.X))
                  return faceOrigin1.X < faceOrigin2.X ? -1 : 1;
               if (!MathUtil.IsAlmostEqual(faceOrigin1.Y, faceOrigin2.Y))
                  return faceOrigin1.Y < faceOrigin2.Y ? -1 : 1;

               return 0;
            }
         }
      }

      // Sort collections of PlanarFaces so that we can create clip planes in consistent orders.
      private class PlanarFaceCollectionComparer : IComparer<ICollection<PlanarFace>>
      {
         public int Compare(ICollection<PlanarFace> faceCollection1, ICollection<PlanarFace> faceCollection2)
         {
            if (faceCollection1 == null)
            {
               if (faceCollection2 == null)
                  return 0;
               else
                  return -1;
            }
            else if (faceCollection2 == null)
               return 1;

            // Check count first.
            if (faceCollection1.Count != faceCollection2.Count)
               return (faceCollection1.Count < faceCollection2.Count) ? -1 : 1;

            // Check the first PlanarFace.
            PlanarFaceClipPlaneComparer comparer = new PlanarFaceClipPlaneComparer();
            return comparer.Compare(faceCollection1.First(), faceCollection2.First());
         }
      }

      private static bool CurveLoopIsARectangle(CurveLoop curveLoop, out IList<int> cornerIndices)
      {
         cornerIndices = new List<int>(4);

         // looking for four orthogonal lines in one curve loop.
         int sz = curveLoop.Count();
         if (sz < 4)
            return false;

         IList<Line> lines = new List<Line>();
         foreach (Curve curve in curveLoop)
         {
            if (!(curve is Line))
               return false;

            lines.Add(curve as Line);
         }

         sz = lines.Count;
         int numAngles = 0;

         // Must have 4 right angles found, and all other lines collinear -- if not, not a rectangle.
         for (int ii = 0; ii < sz; ii++)
         {
            double dot = lines[ii].Direction.DotProduct(lines[(ii + 1) % sz].Direction);
            if (MathUtil.IsAlmostZero(dot))
            {
               if (numAngles > 3)
                  return false;
               cornerIndices.Add(ii);
               numAngles++;
            }
            else if (MathUtil.IsAlmostEqual(dot, 1.0))
            {
               XYZ line0End1 = lines[ii].GetEndPoint(1);
               XYZ line1End0 = lines[(ii + 1) % sz].GetEndPoint(0);
               if (!line0End1.IsAlmostEqualTo(line1End0))
                  return false;
            }
            else
               return false;
         }

         return (numAngles == 4);
      }

      private static IFCAnyHandle CreateRectangleProfileDefIfPossible(ExporterIFC exporterIFC, string profileName, CurveLoop curveLoop, Transform lcs,
          XYZ projDir)
      {
         IList<int> cornerIndices = null;
         if (!CurveLoopIsARectangle(curveLoop, out cornerIndices))
            return null;

         IFCFile file = exporterIFC.GetFile();

         // for the RectangleProfileDef, we have a special requirement that if the profile is an opening
         // in a wall, then the "X" direction of the profile corresponds to the global Z direction (so
         // that reading applications can easily figure out height and width of the opening by reading the
         // X and Y lengths).  As such, we will look at the projection direction; if it is not wholly in the
         // Z direction (as it would be in the case of an opening in the floor, where this is irrelevant), then
         // we will modify the plane's X and Y axes as necessary to ensure that X corresponds to the "most" Z
         // direction (and Y still forms a right-handed coordinate system).
         XYZ xDir = lcs.BasisX;
         XYZ yDir = lcs.BasisY;
         XYZ zDir = lcs.BasisZ;
         XYZ orig = lcs.Origin;

         // if in Z-direction, or |x[2]| > |y[2]|, just use old plane.
         bool flipX = !MathUtil.IsAlmostEqual(Math.Abs(zDir[2]), 1.0) && (Math.Abs(xDir[2]) < Math.Abs(yDir[2]));

         IList<UV> polylinePts = new List<UV>();
         polylinePts.Add(new UV());

         int idx = -1, whichCorner = 0;
         foreach (Curve curve in curveLoop)
         {
            idx++;
            if (cornerIndices[whichCorner] != idx)
               continue;

            whichCorner++;
            Line line = curve as Line;

            XYZ point = line.GetEndPoint(1);
            UV pointProjUV = GeometryUtil.ProjectPointToXYPlaneOfLCS(lcs, projDir, point);
            if (pointProjUV == null)
               return null;
            pointProjUV = UnitUtil.ScaleLength(pointProjUV);

            if (whichCorner == 4)
            {
               polylinePts[0] = pointProjUV;
               break;
            }
            else
               polylinePts.Add(pointProjUV);
         }

         if (polylinePts.Count != 4)
            return null;

         // get the x and y length vectors.  We may have to reverse them.
         UV xLenVec = polylinePts[1] - polylinePts[0];
         UV yLenVec = polylinePts[3] - polylinePts[0];
         if (flipX)
         {
            UV tmp = xLenVec;
            xLenVec = yLenVec;
            yLenVec = tmp;
         }

         double xLen = xLenVec.GetLength();
         double yLen = yLenVec.GetLength();
         if (MathUtil.IsAlmostZero(xLen) || MathUtil.IsAlmostZero(yLen))
            return null;

         IList<double> middlePt = new List<double>();
         middlePt.Add((polylinePts[0].U + polylinePts[2].U) / 2);
         middlePt.Add((polylinePts[0].V + polylinePts[2].V) / 2);
         IFCAnyHandle location = IFCInstanceExporter.CreateCartesianPoint(file, middlePt);

         xLenVec = xLenVec.Normalize();
         IList<double> measure = new List<double>();
         measure.Add(xLenVec.U);
         measure.Add(xLenVec.V);
         IFCAnyHandle refDirectionOpt = ExporterUtil.CreateDirection(file, measure);

         IFCAnyHandle positionHnd = IFCInstanceExporter.CreateAxis2Placement2D(file, location, null, refDirectionOpt);

         IFCAnyHandle rectangularProfileDef = IFCInstanceExporter.CreateRectangleProfileDef(file, IFCProfileType.Area, profileName, positionHnd, xLen, yLen);
         return rectangularProfileDef;
      }

      private static bool GetCenterAndRadiusOfCurveLoop(CurveLoop curveLoop, out XYZ center, out double radius)
      {
         IList<Arc> arcs = new List<Arc>();
         center = new XYZ();
         radius = 0.0;

         foreach (Curve curve in curveLoop)
         {
            if (!(curve is Arc))
               return false;

            arcs.Add(curve as Arc);
         }

         int numArcs = arcs.Count;
         if (numArcs == 0)
            return false;

         radius = arcs[0].Radius;
         center = arcs[0].Center;

         for (int ii = 1; ii < numArcs; ii++)
         {
            XYZ newCenter = arcs[ii].Center;
            if (!newCenter.IsAlmostEqualTo(center))
               return false;
         }

         return true;
      }

      private static IFCAnyHandle CreateCircleBasedProfileDefIfPossible(ExporterIFC exporterIFC, string profileName, CurveLoop curveLoop, Transform lcs,
          XYZ projDir)
      {
         IList<CurveLoop> curveLoops = new List<CurveLoop>();
         curveLoops.Add(curveLoop);
         return CreateCircleBasedProfileDefIfPossible(exporterIFC, profileName, curveLoops, lcs, projDir);
      }

      private static IFCAnyHandle CreateCircleBasedProfileDefIfPossible(ExporterIFC exporterIFC, string profileName, IList<CurveLoop> curveLoops, Transform lcs,
          XYZ projDir)
      {
         int numLoops = curveLoops.Count;
         if (numLoops > 2)
            return null;

         IFCFile file = exporterIFC.GetFile();

         if (curveLoops[0].IsOpen() || (numLoops == 2 && curveLoops[1].IsOpen()))
            return null;

         XYZ origPlaneNorm = lcs.BasisZ;
         Plane curveLoopPlane = null;
         try
         {
            curveLoopPlane = curveLoops[0].GetPlane();
         }
         catch
         {
            return null;
         }

         XYZ curveLoopPlaneNorm = curveLoopPlane.Normal;
         if (!MathUtil.IsAlmostEqual(Math.Abs(origPlaneNorm.DotProduct(curveLoopPlaneNorm)), 1.0))
            return null;

         if (numLoops == 2)
         {
            Plane secondCurveLoopPlane = null;
            try
            {
               secondCurveLoopPlane = curveLoops[1].GetPlane();
            }
            catch
            {
               return null;
            }

            XYZ secondCurveLoopPlaneNorm = secondCurveLoopPlane.Normal;
            if (!MathUtil.IsAlmostEqual(Math.Abs(curveLoopPlaneNorm.DotProduct(secondCurveLoopPlaneNorm)), 1.0))
               return null;
         }

         XYZ ctr;
         double radius, innerRadius = 0.0;
         if (!GetCenterAndRadiusOfCurveLoop(curveLoops[0], out ctr, out radius))
            return null;

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
         {
            XYZ xDir = lcs.BasisX;
            XYZ yDir = lcs.BasisY;
            XYZ zDir = lcs.BasisZ;
            XYZ orig = lcs.Origin;

            ctr -= orig;

            IList<double> newCtr = new List<double>();
            newCtr.Add(UnitUtil.ScaleLength(xDir.DotProduct(ctr)));
            newCtr.Add(UnitUtil.ScaleLength(yDir.DotProduct(ctr)));
            newCtr.Add(UnitUtil.ScaleLength(zDir.DotProduct(ctr)));

            IFCAnyHandle location = IFCInstanceExporter.CreateCartesianPoint(file, newCtr);

            XYZ projDirToUse = projDir;
            XYZ refDirToUse = new XYZ(1.0, 0.0, 0.0);
            if (curveLoops[0].HasPlane())
            {
               projDirToUse = curveLoops[0].GetPlane().Normal;
               refDirToUse = curveLoops[0].GetPlane().XVec;
            }

            IList<double> axisDir = new List<double>();
            axisDir.Add(projDirToUse.X);
            axisDir.Add(projDirToUse.Y);
            axisDir.Add(projDirToUse.Z);
            IFCAnyHandle axisDirectionOpt = ExporterUtil.CreateDirection(file, axisDir);

            IList<double> refDir = new List<double>();
            refDir.Add(1.0);
            refDir.Add(0.0);
            refDir.Add(0.0);
            IFCAnyHandle refDirectionOpt = ExporterUtil.CreateDirection(file, refDirToUse);

            IFCAnyHandle defPosition = IFCInstanceExporter.CreateAxis2Placement3D(file, location, axisDirectionOpt, refDirectionOpt);

            IFCAnyHandle outerCurve = GeometryUtil.CreateIFCCurveFromCurveLoop(exporterIFC, curveLoops[0], lcs, projDirToUse);
            //if (MathUtil.IsAlmostZero(innerRadius))
            if (numLoops == 1)
               return IFCInstanceExporter.CreateArbitraryClosedProfileDef(file, IFCProfileType.Area, profileName, outerCurve);
            else
            {
               IFCAnyHandle innerCurve = GeometryUtil.CreateIFCCurveFromCurveLoop(exporterIFC, curveLoops[1], lcs, projDirToUse);
               HashSet<IFCAnyHandle> innerCurves = new HashSet<IFCAnyHandle>();
               innerCurves.Add(innerCurve);
               return IFCInstanceExporter.CreateArbitraryProfileDefWithVoids(file, IFCProfileType.Area, profileName, outerCurve, innerCurves);
            }
         }
         else
         {
            IList<Arc> arcs = new List<Arc>();

            if (numLoops == 2)
            {
               XYZ checkCtr;
               if (!GetCenterAndRadiusOfCurveLoop(curveLoops[1], out checkCtr, out innerRadius))
                  return null;
               if (!ctr.IsAlmostEqualTo(checkCtr))
                  return null;
            }

            radius = UnitUtil.ScaleLength(radius);
            innerRadius = UnitUtil.ScaleLength(innerRadius);

            XYZ xDir = lcs.BasisX;
            XYZ yDir = lcs.BasisY;
            XYZ orig = lcs.Origin;

            ctr -= orig;

            IList<double> newCtr = new List<double>();
            newCtr.Add(UnitUtil.ScaleLength(xDir.DotProduct(ctr)));
            newCtr.Add(UnitUtil.ScaleLength(yDir.DotProduct(ctr)));

            IFCAnyHandle location = IFCInstanceExporter.CreateCartesianPoint(file, newCtr);

            IList<double> refDir = new List<double>();
            refDir.Add(1.0);
            refDir.Add(0.0);
            IFCAnyHandle refDirectionOpt = ExporterUtil.CreateDirection(file, refDir);

            IFCAnyHandle defPosition = IFCInstanceExporter.CreateAxis2Placement2D(file, location, null, refDirectionOpt);

            if (MathUtil.IsAlmostZero(innerRadius))
               return IFCInstanceExporter.CreateCircleProfileDef(file, IFCProfileType.Area, profileName, defPosition, radius);
            else
               return IFCInstanceExporter.CreateCircleHollowProfileDef(file, IFCProfileType.Area, profileName, defPosition, radius, radius - innerRadius);
         }
      }

      /// <summary>
      /// Determines if a curveloop can be exported as an I-Shape profile.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="profileName">The name of the profile.</param>
      /// <param name="curveLoop">The curve loop.</param>
      /// <param name="lcs">The local coordinate system whose XY plane contains the curve loop.</param>
      /// <param name="projDir">The projection direction.</param>
      /// <returns>The IfcIShapeProfileDef, or null if not possible.</returns>
      /// <remarks>This routine works with I-shaped curveloops projected onto origPlane, in either orientation;
      /// it does not work with H-shaped curveloops.</remarks>
      private static IFCAnyHandle CreateIShapeProfileDefIfPossible(ExporterIFC exporterIFC, string profileName, CurveLoop curveLoop, Transform lcs,
          XYZ projDir)
      {
         IFCFile file = exporterIFC.GetFile();

         if (curveLoop.IsOpen())
            return null;

         if (curveLoop.Count() != 12 && curveLoop.Count() != 16)
            return null;

         // All curves must be lines, except for 4 optional fillets; get direction vectors and start points.
         XYZ xDir = lcs.BasisX;
         XYZ yDir = lcs.BasisY;

         // The list of vertices, in order.  startVertex below is the upper-right hand vertex, in UV-space.
         IList<UV> vertices = new List<UV>();
         // The directions in UV of the line segments. directions[ii] is the direction of the line segment starting with vertex[ii].
         IList<UV> directions = new List<UV>();
         // The lengths in UV of the line segments.  lengths[ii] is the length of the line segment starting with vertex[ii].
         IList<double> lengths = new List<double>();
         // turnsCCW[ii] is true if directions[ii+1] is clockwise relative to directions[ii] in UV-space.
         IList<bool> turnsCCW = new List<bool>();

         IList<Arc> fillets = new List<Arc>();
         IList<int> filletPositions = new List<int>();

         int idx = 0;
         int startVertex = -1;
         int startFillet = -1;
         UV upperRight = null;
         double lowerBoundU = 1e+30;
         double upperBoundU = -1e+30;

         foreach (Curve curve in curveLoop)
         {
            if (!(curve is Line))
            {
               if (!(curve is Arc))
                  return null;
               fillets.Add(curve as Arc);
               filletPositions.Add(idx);   // share the index of the next line segment.
               continue;
            }

            Line line = curve as Line;

            XYZ point = line.GetEndPoint(0);
            UV pointProjUV = GeometryUtil.ProjectPointToXYPlaneOfLCS(lcs, projDir, point);
            if (pointProjUV == null)
               return null;
            pointProjUV = UnitUtil.ScaleLength(pointProjUV);

            if ((upperRight == null) || ((pointProjUV.U > upperRight.U - MathUtil.Eps()) && (pointProjUV.V > upperRight.V - MathUtil.Eps())))
            {
               upperRight = pointProjUV;
               startVertex = idx;
               startFillet = filletPositions.Count;
            }

            if (pointProjUV.U < lowerBoundU)
               lowerBoundU = pointProjUV.U;
            if (pointProjUV.U > upperBoundU)
               upperBoundU = pointProjUV.U;

            vertices.Add(pointProjUV);

            XYZ direction3d = line.Direction;
            UV direction = new UV(direction3d.DotProduct(xDir), direction3d.DotProduct(yDir));
            lengths.Add(UnitUtil.ScaleLength(line.Length));

            bool zeroU = MathUtil.IsAlmostZero(direction.U);
            bool zeroV = MathUtil.IsAlmostZero(direction.V);
            if (zeroU && zeroV)
               return null;

            // Accept only non-rotated I-Shapes.
            if (!zeroU && !zeroV)
               return null;

            direction.Normalize();
            if (idx > 0)
            {
               if (!MathUtil.IsAlmostZero(directions[idx - 1].DotProduct(direction)))
                  return null;
               turnsCCW.Add(directions[idx - 1].CrossProduct(direction) > 0);
            }

            directions.Add(direction);
            idx++;
         }

         if (directions.Count != 12)
            return null;

         if (!MathUtil.IsAlmostZero(directions[11].DotProduct(directions[0])))
            return null;
         turnsCCW.Add(directions[11].CrossProduct(directions[0]) > 0);

         bool firstTurnIsCCW = turnsCCW[startVertex];

         // Check proper turning of lines.
         // The orientation of the turns should be such that 8 match the original orientation, and 4 go in the opposite direction.
         // The opposite ones are:
         // For I-Shape:
         // if the first turn is clockwise (i.e., in -Y direction): 1,2,7,8.
         // if the first turn is counterclockwise (i.e., in the -X direction): 2,3,8,9.
         // For H-Shape:
         // if the first turn is clockwise (i.e., in -Y direction): 2,3,8,9.
         // if the first turn is counterclockwise (i.e., in the -X direction): 1,2,7,8.

         int iShapeCCWOffset = firstTurnIsCCW ? 1 : 0;
         int hShapeCWOffset = firstTurnIsCCW ? 0 : 1;

         bool isIShape = true;
         bool isHShape = false;

         for (int ii = 0; ii < 12 && isIShape; ii++)
         {
            int currOffset = 12 + (startVertex - iShapeCCWOffset);
            int currIdx = (ii + currOffset) % 12;
            if (currIdx == 1 || currIdx == 2 || currIdx == 7 || currIdx == 8)
            {
               if (firstTurnIsCCW == turnsCCW[ii])
                  isIShape = false;
            }
            else
            {
               if (firstTurnIsCCW == !turnsCCW[ii])
                  isIShape = false;
            }
         }

         if (!isIShape)
         {
            // Check if it is orientated like an H - if neither I nor H, fail.
            isHShape = true;

            for (int ii = 0; ii < 12 && isHShape; ii++)
            {
               int currOffset = 12 + (startVertex - hShapeCWOffset);
               int currIdx = (ii + currOffset) % 12;
               if (currIdx == 1 || currIdx == 2 || currIdx == 7 || currIdx == 8)
               {
                  if (firstTurnIsCCW == turnsCCW[ii])
                     return null;
               }
               else
               {
                  if (firstTurnIsCCW == !turnsCCW[ii])
                     return null;
               }
            }
         }

         // Check that the lengths of parallel and symmetric line segments are equal.
         double overallWidth = 0.0;
         double overallDepth = 0.0;
         double flangeThickness = 0.0;
         double webThickness = 0.0;

         // I-Shape:
         // CCW pairs:(0,6), (1,5), (1,7), (1,11), (2,4), (2,8), (2,10), (3, 9)
         // CW pairs: (11,5), (0,4), (0,6), (0,10), (1,3), (1,7), (1,9), (2, 8)
         // H-Shape is reversed.
         int cwPairOffset = (firstTurnIsCCW == isIShape) ? 0 : 11;

         overallWidth = lengths[(startVertex + cwPairOffset) % 12];
         flangeThickness = lengths[(startVertex + 1 + cwPairOffset) % 12];

         if (isIShape)
         {
            if (firstTurnIsCCW)
            {
               overallDepth = vertices[startVertex].V - vertices[(startVertex + 7) % 12].V;
               webThickness = vertices[(startVertex + 9) % 12].U - vertices[(startVertex + 3) % 12].U;
            }
            else
            {
               overallDepth = vertices[startVertex].V - vertices[(startVertex + 5) % 12].V;
               webThickness = vertices[(startVertex + 2) % 12].U - vertices[(startVertex + 8) % 12].U;
            }
         }
         else
         {
            if (!firstTurnIsCCW)
            {
               overallDepth = vertices[startVertex].U - vertices[(startVertex + 7) % 12].U;
               webThickness = vertices[(startVertex + 9) % 12].V - vertices[(startVertex + 3) % 12].V;
            }
            else
            {
               overallDepth = vertices[startVertex].U - vertices[(startVertex + 5) % 12].U;
               webThickness = vertices[(startVertex + 2) % 12].V - vertices[(startVertex + 8) % 12].V;
            }
         }

         if (!MathUtil.IsAlmostEqual(overallWidth, lengths[(startVertex + 6 + cwPairOffset) % 12]))
            return null;
         if (!MathUtil.IsAlmostEqual(flangeThickness, lengths[(startVertex + 5 + cwPairOffset) % 12]) ||
             !MathUtil.IsAlmostEqual(flangeThickness, lengths[(startVertex + 7 + cwPairOffset) % 12]) ||
             !MathUtil.IsAlmostEqual(flangeThickness, lengths[(startVertex + 11 + cwPairOffset) % 12]))
            return null;
         double innerTopLeftLength = lengths[(startVertex + 2 + cwPairOffset) % 12];
         if (!MathUtil.IsAlmostEqual(innerTopLeftLength, lengths[(startVertex + 4 + cwPairOffset) % 12]) ||
             !MathUtil.IsAlmostEqual(innerTopLeftLength, lengths[(startVertex + 8 + cwPairOffset) % 12]) ||
             !MathUtil.IsAlmostEqual(innerTopLeftLength, lengths[(startVertex + 10 + cwPairOffset) % 12]))
            return null;
         double iShaftLength = lengths[(startVertex + 3 + cwPairOffset) % 12];
         if (!MathUtil.IsAlmostEqual(iShaftLength, lengths[(startVertex + 9 + cwPairOffset) % 12]))
            return null;

         // Check fillet validity.
         int numFillets = fillets.Count();
         double? filletRadius = null;

         if (numFillets != 0)
         {
            if (numFillets != 4)
               return null;

            // startFillet can have any value from 0 to 4; if it is 4, need to reset it to 0.

            // The fillet positions relative to the upper right hand corner are:
            // For I-Shape:
            // if the first turn is clockwise (i.e., in -Y direction): 2,3,8,9.
            // if the first turn is counterclockwise (i.e., in the -X direction): 3,4,9,10.
            // For H-Shape:
            // if the first turn is clockwise (i.e., in -Y direction): 3,4,9,10.
            // if the first turn is counterclockwise (i.e., in the -X direction): 2,3,8,9.
            int filletOffset = (isIShape == firstTurnIsCCW) ? 1 : 0;
            if (filletPositions[startFillet % 4] != ((2 + filletOffset + startVertex) % 12) ||
                filletPositions[(startFillet + 1) % 4] != ((3 + filletOffset + startVertex) % 12) ||
                filletPositions[(startFillet + 2) % 4] != ((8 + filletOffset + startVertex) % 12) ||
                filletPositions[(startFillet + 3) % 4] != ((9 + filletOffset + startVertex) % 12))
               return null;

            double tmpFilletRadius = fillets[0].Radius;
            for (int ii = 1; ii < 4; ii++)
            {
               if (!MathUtil.IsAlmostEqual(tmpFilletRadius, fillets[ii].Radius))
                  return null;
            }

            if (!MathUtil.IsAlmostZero(tmpFilletRadius))
               filletRadius = UnitUtil.ScaleLength(tmpFilletRadius);
         }

         XYZ planeNorm = lcs.BasisZ;
         for (int ii = 0; ii < numFillets; ii++)
         {
            bool filletIsCCW = (fillets[ii].Normal.DotProduct(planeNorm) > MathUtil.Eps());
            if (filletIsCCW == firstTurnIsCCW)
               return null;
         }

         if (MathUtil.IsAlmostZero(overallWidth) || MathUtil.IsAlmostZero(overallDepth) ||
             MathUtil.IsAlmostZero(flangeThickness) || MathUtil.IsAlmostZero(webThickness))
            return null;

         // We have an I-Shape Profile!
         IList<double> newCtr = new List<double>();
         newCtr.Add((vertices[0].U + vertices[6].U) / 2);
         newCtr.Add((vertices[0].V + vertices[6].V) / 2);

         IFCAnyHandle location = IFCInstanceExporter.CreateCartesianPoint(file, newCtr);

         IList<double> refDir = new List<double>();

         if (isIShape)
         {
            refDir.Add(1.0);
            refDir.Add(0.0);
         }
         else
         {
            refDir.Add(0.0);
            refDir.Add(1.0);
         }

         IFCAnyHandle refDirectionOpt = ExporterUtil.CreateDirection(file, refDir);

         IFCAnyHandle positionHnd = IFCInstanceExporter.CreateAxis2Placement2D(file, location, null, refDirectionOpt);

         return IFCInstanceExporter.CreateIShapeProfileDef(file, IFCProfileType.Area, profileName, positionHnd,
             overallWidth, overallDepth, webThickness, flangeThickness, filletRadius);
      }

      /// <returns>true if the curve loop is clockwise, false otherwise.</returns>
      private static bool SafeIsCurveLoopClockwise(CurveLoop curveLoop, XYZ dir)
      {
         if (curveLoop == null)
            return false;

         if (curveLoop.IsOpen())
            return false;

         if ((curveLoop.Count() == 1) && !(curveLoop.First().IsBound))
            return false;

         return !curveLoop.IsCounterclockwise(dir);
      }

      /// <summary>
      /// Set the  LCS for the curveloops using the information from the member curves
      /// </summary>
      /// <param name="curveLoops">The curveLoops</param>
      /// <param name="extrDir">Extrusion direction</param>
      /// <param name="lcs">Output parameter for the "corrected" LCS</param>
      /// <returns>True if the LCS is set</returns>
      private static bool CorrectCurveLoopOrientation(IList<CurveLoop> curveLoops, XYZ extrDir, out Transform lcs)
      {
         lcs = null;
         int loopSz = curveLoops.Count;
         bool firstCurve = true;
         foreach (CurveLoop curveLoop in curveLoops)
         {
            // ignore checks if unbounded curve.
            if (curveLoop.Count() == 0)
               return false;

            if (!(curveLoop.First().IsBound))
            {
               if (firstCurve)
               {
                  Arc arc = curveLoop.First() as Arc;
                  if (arc == null)
                     return false;

                  XYZ xVec = arc.XDirection.Normalize();
                  XYZ yVec = arc.YDirection.Normalize();
                  XYZ center = arc.Center;

                  lcs = GeometryUtil.CreateTransformFromVectorsAndOrigin(xVec, yVec, xVec.CrossProduct(yVec), center);
               }
            }
            else if (firstCurve)
            {
               if (SafeIsCurveLoopClockwise(curveLoop, extrDir))
                  curveLoop.Flip();

               try
               {
                  Plane plane = curveLoop.GetPlane();
                  lcs = GeometryUtil.CreateTransformFromPlane(plane);
               }
               catch
               {
                  return false;
               }
               if (lcs == null)
                  return false;
            }
            else
            {
               if (!SafeIsCurveLoopClockwise(curveLoop, extrDir))
                  curveLoop.Flip();
            }

            firstCurve = false;
         }

         return (lcs != null);
      }

      /// <summary>
      /// Reduce the number of segments in a curveloop to make it easier to export.  Usually used on curveloops with many line segments.
      /// </summary>
      /// <param name="origCurveLoop">The original curve loop.</param>
      /// <param name="forceAllowCoarsen">If true, we will attempt to coarsen the curve loop regardless of the number of segments.</param>
      /// <returns>The coarsened loop, if creatable; otherwise, the original loop.</returns>
      private static CurveLoop CoarsenCurveLoop(CurveLoop origCurveLoop, bool forceAllowCoarsen)
      {
         // We don't really know if the original CurveLoop is valid, so attempting to create a copy may result in exceptions.
         // Protect against this for each individual loop.
         try
         {
            if (origCurveLoop.Count() <= 24 && !forceAllowCoarsen)
               return origCurveLoop;

            bool modified = false;
            XYZ lastFirstPt = null;
            Line lastLine = null;

            IList<Curve> newCurves = new List<Curve>();
            foreach (Curve curve in origCurveLoop)
            {
               // Set lastLine to be the first line in the loop, if it exists.
               // In addition, Revit may have legacy curve segments that are too short.  Don't process them.
               if (!(curve is Line))
               {
                  // Break the polyline, if it existed.
                  if (lastLine != null)
                     newCurves.Add(lastLine);

                  lastLine = null;
                  lastFirstPt = null;
                  newCurves.Add(curve);
                  continue;
               }

               if (lastLine == null)
               {
                  lastLine = curve as Line;
                  lastFirstPt = lastLine.GetEndPoint(0);
                  continue;
               }

               // If we are here, we have two lines in a row.  See if they are almost collinear.
               XYZ currLastPt = curve.GetEndPoint(1);

               Line combinedLine = null;

               // If the combined curve is too short, don't merge.
               if (currLastPt.DistanceTo(lastFirstPt) > ExporterCacheManager.Document.Application.ShortCurveTolerance)
               {
                  combinedLine = Line.CreateBound(lastFirstPt, currLastPt);

                  XYZ currMidPt = curve.GetEndPoint(0);
                  IntersectionResult result = combinedLine.Project(currMidPt);

                  // If the absolute distance is greater than 1", or 1% of either line length, use both.
                  double dist = result.Distance;
                  if ((dist > 1.0 / 12.0) || (dist / (lastLine.Length) > 0.01) || (dist / (curve.Length) > 0.01))
                     combinedLine = null;
               }

               if (combinedLine == null)
               {
                  newCurves.Add(lastLine);

                  lastLine = curve as Line;
                  lastFirstPt = lastLine.GetEndPoint(0);

                  continue;
               }

               // The combined line is now the last line.
               lastLine = combinedLine;
               modified = true;
            }

            if (modified)
            {
               if (lastLine != null)
                  newCurves.Add(lastLine);

               CurveLoop modifiedCurveLoop = new CurveLoop();
               foreach (Curve modifiedCurve in newCurves)
                  modifiedCurveLoop.Append(modifiedCurve);

               return modifiedCurveLoop;
            }
            else
            {
               return origCurveLoop;
            }
         }
         catch
         {
            // If we run into any trouble, use the original loop.  
            // TODO: this may end up failing in the ValidateCurves check that follows, so we may just skip entirely.
            return origCurveLoop;
         }
      }

      private static IList<CurveLoop> CoarsenCurveLoops(IList<CurveLoop> origCurveLoops)
      {
         // Coarsen loop unless we are at the Highest level of detail.
         if (ExporterCacheManager.ExportOptionsCache.LevelOfDetail == ExportOptionsCache.ExportTessellationLevel.High)
            return origCurveLoops;

         IList<CurveLoop> modifiedLoops = new List<CurveLoop>();
         foreach (CurveLoop curveLoop in origCurveLoops)
         {
            modifiedLoops.Add(CoarsenCurveLoop(curveLoop, false));
         }

         return modifiedLoops;
      }

      /// <summary>
      /// Creates an extruded solid from a collection of curve loops and a thickness.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="profileName">The name of the extrusion profile.</param>
      /// <param name="origCurveLoops">The profile boundary curves.</param>
      /// <param name="lcs">The local coordinate system of the boundary curves.</param>
      /// <param name="extrDirVec">The direction of the extrusion.</param>
      /// <param name="scaledExtrusionSize">The thickness of the extrusion, perpendicular to the plane.</param>
      /// <param name="allowExportingOnlyOuterLoop">If this arugment is true, we'll allow exporting the extrusion if only the outer boundary is valid.</param>
      /// <returns>The IfcExtrudedAreaSolid handle.</returns>
      /// <remarks>If the curveLoop plane normal is not the same as the plane direction, only tesellated boundaries are supported.
      /// The allowExportingOnlyOuterLoop is generally false, as its initial scope is intended for use with rooms, areas, and spaces.
      /// It could be extended with appropriate testing.</remarks> 
      public static IFCAnyHandle CreateExtrudedSolidFromCurveLoop(ExporterIFC exporterIFC, string profileName, IList<CurveLoop> origCurveLoops,
          Transform lcs, XYZ extrDirVec, double scaledExtrusionSize, bool allowExportingOnlyOuterLoop)
      {
         IFCAnyHandle extrudedSolidHnd = null;

         if (scaledExtrusionSize < MathUtil.Eps())
            return extrudedSolidHnd;

         IFCFile file = exporterIFC.GetFile();

         // we need to figure out the plane of the curve loops and modify the extrusion direction appropriately.
         // assumption: first curve loop defines the plane.
         int origCurveLoopCount = origCurveLoops.Count;
         if (origCurveLoopCount == 0)
            return extrudedSolidHnd;

         XYZ planeXDir = lcs.BasisX;
         XYZ planeYDir = lcs.BasisY;
         XYZ planeZDir = lcs.BasisZ;
         XYZ planeOrig = lcs.Origin;

         double slantFactor = Math.Abs(planeZDir.DotProduct(extrDirVec));
         if (MathUtil.IsAlmostZero(slantFactor))
            return extrudedSolidHnd;

         // Reduce the number of line segments in the curveloops from highly tessellated polylines, if applicable.
         IList<CurveLoop> curveLoops = CoarsenCurveLoops(origCurveLoops);
         if (curveLoops == null)
            return extrudedSolidHnd;

         // Check that curve loops are valid.
         curveLoops = ExporterIFCUtils.ValidateCurveLoops(curveLoops, extrDirVec);
         if (curveLoops.Count == 0)
         {
            // We are only going to try to heal the outer loop, so we'll fail if we have more than one loop
            // and we don't allow the fallback of ignoring any holes.
            if (!allowExportingOnlyOuterLoop && origCurveLoopCount > 1)
               return extrudedSolidHnd;

            CurveLoop coarseCurveLoop = CoarsenCurveLoop(origCurveLoops[0], true);
            if (coarseCurveLoop == null)
               return extrudedSolidHnd;

            curveLoops.Clear();
            curveLoops.Add(coarseCurveLoop);
            curveLoops = ExporterIFCUtils.ValidateCurveLoops(curveLoops, extrDirVec);

            // We check again in case we succeeded above.
            if (curveLoops.Count == 0)
               return extrudedSolidHnd;
         }

         scaledExtrusionSize /= slantFactor;

         IFCAnyHandle sweptArea = CreateSweptArea(exporterIFC, profileName, curveLoops, lcs, extrDirVec);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(sweptArea))
            return extrudedSolidHnd;

         IList<double> relExtrusionDirList = new List<double>();
         relExtrusionDirList.Add(extrDirVec.DotProduct(planeXDir));
         relExtrusionDirList.Add(extrDirVec.DotProduct(planeYDir));
         relExtrusionDirList.Add(extrDirVec.DotProduct(planeZDir));

         XYZ scaledXDir = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, planeXDir);
         XYZ scaledZDir = ExporterIFCUtils.TransformAndScaleVector(exporterIFC, planeZDir);
         XYZ scaledOrig = ExporterIFCUtils.TransformAndScalePoint(exporterIFC, planeOrig);

         IFCAnyHandle solidAxis = ExporterUtil.CreateAxis(file, scaledOrig, scaledZDir, scaledXDir);
         IFCAnyHandle extrusionDirection = ExporterUtil.CreateDirection(file, relExtrusionDirList);

         extrudedSolidHnd = IFCInstanceExporter.CreateExtrudedAreaSolid(file, sweptArea, solidAxis, extrusionDirection, scaledExtrusionSize);
         return extrudedSolidHnd;
      }

      /// <summary>
      /// Creates an IfcProfileDef for a swept area.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="profileName">The profile name.</param>
      /// <param name="curveLoops">The curve loops.</param>
      /// <param name="lcs">The local coordinate system whose XY plane contains the curve loops.</param>
      /// <param name="sweptDirection">The direction.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSweptArea(ExporterIFC exporterIFC, string profileName, IList<CurveLoop> curveLoops, Transform lcs, XYZ sweptDirection)
      {
         IFCAnyHandle sweptArea = null;
         if (curveLoops.Count == 1)
         {
            if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
            {
               // Only Circle profile and IndexedPolyCurve are allowed in IFC4RV
               sweptArea = CreateCircleBasedProfileDefIfPossible(exporterIFC, profileName, curveLoops[0], lcs, sweptDirection);
            }
            else
            {
               sweptArea = CreateRectangleProfileDefIfPossible(exporterIFC, profileName, curveLoops[0], lcs, sweptDirection);
               if (sweptArea == null) sweptArea = CreateCircleBasedProfileDefIfPossible(exporterIFC, profileName, curveLoops[0], lcs, sweptDirection);
               if (sweptArea == null) sweptArea = CreateIShapeProfileDefIfPossible(exporterIFC, profileName, curveLoops[0], lcs, sweptDirection);
            }
         }
         else if (curveLoops.Count == 2)
         {
            sweptArea = CreateCircleBasedProfileDefIfPossible(exporterIFC, profileName, curveLoops, lcs, sweptDirection);
         }

         if (sweptArea == null)
         {
            IFCAnyHandle profileCurve = null;
            HashSet<IFCAnyHandle> innerCurves = new HashSet<IFCAnyHandle>();

            // reorient curves if necessary: outer CCW, inners CW.
            foreach (CurveLoop curveLoop in curveLoops)
            {
               bool isCCW = false;
               try
               {
                  isCCW = curveLoop.IsCounterclockwise(lcs.BasisZ);
               }
               catch
               {
                  if (profileCurve == null)
                     return null;
                  else
                     continue;
               }

               if (profileCurve == null)
               {
                  if (!isCCW)
                     curveLoop.Flip();
                  profileCurve = GeometryUtil.CreateIFCCurveFromCurveLoop(exporterIFC, curveLoop, lcs, sweptDirection);
                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(profileCurve))
                     return null;
               }
               else
               {
                  if (isCCW)
                     curveLoop.Flip();
                  IFCAnyHandle innerCurve = GeometryUtil.CreateIFCCurveFromCurveLoop(exporterIFC, curveLoop, lcs, sweptDirection);
                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(innerCurve))
                     innerCurves.Add(innerCurve);
               }
            }

            IFCFile file = exporterIFC.GetFile();
            if (innerCurves.Count > 0)
               sweptArea = IFCInstanceExporter.CreateArbitraryProfileDefWithVoids(file, IFCProfileType.Area, profileName, profileCurve, innerCurves);
            else
               sweptArea = IFCInstanceExporter.CreateArbitraryClosedProfileDef(file, IFCProfileType.Area, profileName, profileCurve);
         }
         return sweptArea;
      }

      /// <summary>
      /// Creates extruded solid from extrusion data.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="extrusionData">The extrusion data.</param>
      /// <returns>The IfcExtrudedAreaSolid handle.</returns>
      public static IFCAnyHandle CreateExtrudedSolidFromExtrusionData(ExporterIFC exporterIFC, Element element, IFCExtrusionData extrusionData,
          out Transform lcs, string profileName = null)
      {
         lcs = null;
         if (!extrusionData.IsValid())
            return null;

         IList<CurveLoop> extrusionLoops = extrusionData.GetLoops();
         if (extrusionLoops != null)
         {
            XYZ extrusionDir = extrusionData.ExtrusionDirection;
            double extrusionSize = extrusionData.ScaledExtrusionLength;

            if (CorrectCurveLoopOrientation(extrusionLoops, extrusionDir, out lcs))
            {
               if (element != null && string.IsNullOrEmpty(profileName))
               {
                  ElementType type = element.Document.GetElement(element.GetTypeId()) as ElementType;
                  if (type != null)
                     profileName = type.Name;
               }

               IFCAnyHandle extrudedSolid = CreateExtrudedSolidFromCurveLoop(exporterIFC, profileName, extrusionLoops,
                   lcs, extrusionDir, extrusionSize, false);
               return extrudedSolid;
            }
         }

         return null;
      }

      /// <summary>
      /// Computes the outer length of curve loops.
      /// </summary>
      /// <param name="curveLoops">List of curve loops.</param>
      /// <returns>The length.</returns>
      public static double ComputeOuterPerimeterOfCurveLoops(IList<CurveLoop> curveLoops)
      {
         int numCurveLoops = curveLoops.Count;
         if (numCurveLoops == 0)
            return 0.0;

         if (curveLoops[0].IsOpen())
            return 0.0;

         return curveLoops[0].GetExactLength();
      }

      /// <summary>
      /// Computes the inner length of curve loops.
      /// </summary>
      /// <param name="curveLoops">List of curve loops.</param>
      /// <returns>The length.</returns>
      public static double ComputeInnerPerimeterOfCurveLoops(IList<CurveLoop> curveLoops)
      {
         double innerPerimeter = 0.0;

         int numCurveLoops = curveLoops.Count;
         if (numCurveLoops == 0)
            return 0.0;

         for (int ii = 1; ii < numCurveLoops; ii++)
         {
            if (curveLoops[ii].IsOpen())
               return 0.0;
            innerPerimeter += curveLoops[ii].GetExactLength();
         }

         return innerPerimeter;
      }

      /// <summary>
      /// Adds a new opening to extrusion creation data from curve loop and extrusion data.
      /// </summary>
      /// <param name="creationData">The extrusion creation data.</param>
      /// <param name="from">The extrusion data.</param>
      /// <param name="curveLoop">The curve loop.</param>
      public static void AddOpeningData(IFCExtrusionCreationData creationData, IFCExtrusionData from, CurveLoop curveLoop)
      {
         List<CurveLoop> curveLoops = new List<CurveLoop>();
         curveLoops.Add(curveLoop);
         AddOpeningData(creationData, from, curveLoops);
      }

      /// <summary>
      /// Adds a new opening to extrusion creation data from extrusion data.
      /// </summary>
      /// <param name="creationData">The extrusion creation data.</param>
      /// <param name="from">The extrusion data.</param>
      public static void AddOpeningData(IFCExtrusionCreationData creationData, IFCExtrusionData from)
      {
         AddOpeningData(creationData, from, from.GetLoops());
      }

      /// <summary>
      /// Adds a new opening to extrusion creation data from curve loops and extrusion data.
      /// </summary>
      /// <param name="creationData">The extrusion creation data.</param>
      /// <param name="from">The extrusion data.</param>
      /// <param name="curveLoops">The curve loops.</param>
      public static void AddOpeningData(IFCExtrusionCreationData creationData, IFCExtrusionData from, ICollection<CurveLoop> curveLoops)
      {
         IFCExtrusionData newData = new IFCExtrusionData();
         foreach (CurveLoop curveLoop in curveLoops)
            newData.AddLoop(curveLoop);
         newData.ScaledExtrusionLength = from.ScaledExtrusionLength;
         newData.ExtrusionBasis = from.ExtrusionBasis;

         newData.ExtrusionDirection = from.ExtrusionDirection;
         creationData.AddOpening(newData);
      }

      /// <summary>
      /// Generates an IFCExtrusionCreationData from ExtrusionAnalyzer results
      /// </summary>
      /// <remarks>This will be used to populate certain property sets.</remarks>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="projDir">The projection direction of the extrusion.</param>
      /// <param name="analyzer">The extrusion analyzer.</param>
      /// <returns>The IFCExtrusionCreationData information.</returns>
      public static IFCExtrusionCreationData GetExtrusionCreationDataFromAnalyzer(XYZ projDir, ExtrusionAnalyzer analyzer)
      {
         IFCExtrusionCreationData exportBodyParams = new IFCExtrusionCreationData();

         XYZ extrusionDirection = analyzer.ExtrusionDirection;

         double zOff = MathUtil.IsAlmostEqual(Math.Abs(projDir[2]), 1.0) ? (1.0 - Math.Abs(extrusionDirection[2])) : Math.Abs(extrusionDirection[2]);
         double scaledAngle = UnitUtil.ScaleAngle(MathUtil.SafeAsin(zOff));

         exportBodyParams.Slope = scaledAngle;
         exportBodyParams.ScaledLength = UnitUtil.ScaleLength(analyzer.EndParameter - analyzer.StartParameter);
         exportBodyParams.ExtrusionDirection = extrusionDirection;

         // no opening data support yet.

         Face extrusionBase = analyzer.GetExtrusionBase();
         if (extrusionBase == null)
            return null;

         IList<GeometryUtil.FaceBoundaryType> boundaryTypes;
         IList<CurveLoop> boundaries = GeometryUtil.GetFaceBoundaries(extrusionBase, XYZ.Zero, out boundaryTypes);
         if (boundaries.Count == 0)
            return null;

         double height = 0.0, width = 0.0;
         if (GeometryUtil.ComputeHeightWidthOfCurveLoop(boundaries[0], out height, out width))
         {
            exportBodyParams.ScaledHeight = UnitUtil.ScaleLength(height);
            exportBodyParams.ScaledWidth = UnitUtil.ScaleLength(width);
         }

         double area = extrusionBase.Area;
         if (area > 0.0)
         {
            exportBodyParams.ScaledArea = UnitUtil.ScaleArea(area);
         }

         double innerPerimeter = ExtrusionExporter.ComputeInnerPerimeterOfCurveLoops(boundaries);
         double outerPerimeter = ExtrusionExporter.ComputeOuterPerimeterOfCurveLoops(boundaries);
         if (innerPerimeter > 0.0)
            exportBodyParams.ScaledInnerPerimeter = UnitUtil.ScaleLength(innerPerimeter);
         if (outerPerimeter > 0.0)
            exportBodyParams.ScaledOuterPerimeter = UnitUtil.ScaleLength(outerPerimeter);

         return exportBodyParams;
      }

      private class HandleAndAnalyzer
      {
         public IFCAnyHandle Handle = null;
         public ExtrusionAnalyzer Analyzer = null;
         public IList<IFCAnyHandle> BaseRepresentationItems = new List<IFCAnyHandle>();
         public ShapeRepresentationType ShapeRepresentationType = ShapeRepresentationType.Undefined;
         public FootPrintInfo m_FootprintInfo = null;
         public IFCAnyHandle ProfileDefHandle = null;
         MaterialAndProfile m_MaterialAndProfile = null;

         /// <summary>
         /// Material and Profile information for IfcMaterialProfile related information
         /// </summary>
         public MaterialAndProfile MaterialAndProfile
         {
            get
            {
               if (m_MaterialAndProfile == null)
                  m_MaterialAndProfile = new MaterialAndProfile();
               return m_MaterialAndProfile;
            }
            set { m_MaterialAndProfile = value; }
         }

         /// <summary>
         /// Footprint gemetric representation item related information
         /// </summary>
         public FootPrintInfo FootPrintInfo { get; set; } = null;
      }

      /// <summary>
      /// A class to store output information when creating clipped extrusions.
      /// </summary>
      public class ExtraClippingData
      {
         /// <summary>
         /// True if the extrusion is completely clipped (i.e., no geometry).
         /// </summary>
         public bool CompletelyClipped { get; set; } = false;

         /// <summary>
         /// True if there is a clipped extrusion (vs. a simple extrusion).
         /// </summary>
         public bool HasClippingResult { get; set; } = false;

         /// <summary>
         /// True if there is an IfcBooleanResult (vs. a simple extrusion).
         /// </summary>
         public bool HasBooleanResult { get; set; } = false;

         /// <summary>
         /// The material id of the resulting geometry.
         /// </summary>
         public IList<ElementId> MaterialIds { get; set; } = new List<ElementId>();
      }

      private static HandleAndAnalyzer CreateExtrusionWithClippingBase(
         ExporterIFC exporterIFC, Element element, bool isVoid,
         ElementId catId, IList<Solid> solids, Plane basePlane, XYZ planeOrigin, XYZ projDir, IFCRange range,
         out ExtraClippingData extraClippingData,
         GenerateAdditionalInfo addInfo = GenerateAdditionalInfo.GenerateBody, string profileName = null)
      {
         IFCFile file = exporterIFC.GetFile();
         extraClippingData = new ExtraClippingData();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            HandleAndAnalyzer retVal = new HandleAndAnalyzer();
            HashSet<IFCAnyHandle> extrusionBodyItems = new HashSet<IFCAnyHandle>();
            HashSet<IFCAnyHandle> extrusionBooleanBodyItems = new HashSet<IFCAnyHandle>();
            HashSet<IFCAnyHandle> extrusionClippingBodyItems = new HashSet<IFCAnyHandle>();
            IList<FootPrintInfo> extrusionFootprintItems = new List<FootPrintInfo>();
            MaterialAndProfile materialAndProfile = new MaterialAndProfile();
            foreach (Solid solid in solids)
            {
               ExtraClippingData currentExtraClippingData = null;
               HandleAndAnalyzer currRetVal = CreateExtrusionWithClippingAndOpening(
                  exporterIFC, element, isVoid,
                  solid, basePlane, planeOrigin, projDir, range,
                  out currentExtraClippingData,
                  addInfo: addInfo,
                  profileName: profileName);

               if ((addInfo & GenerateAdditionalInfo.GenerateFootprint) != 0 && currRetVal.FootPrintInfo != null)
               {
                  retVal.MaterialAndProfile = currRetVal.MaterialAndProfile;
                  retVal.FootPrintInfo = currRetVal.FootPrintInfo;
               }

               if (currRetVal != null && currRetVal.Handle != null)
               {
                  extraClippingData.MaterialIds.Union(currentExtraClippingData.MaterialIds);
                  IFCAnyHandle repHandle = currRetVal.Handle;
                  if (extraClippingData.HasBooleanResult) // if both have boolean and clipping result, use boolean one.
                     extrusionBooleanBodyItems.Add(repHandle);
                  else if (extraClippingData.HasClippingResult)
                  {
                     extrusionClippingBodyItems.Add(repHandle);
                     // This potentially is exported as a StandardCase element (if it is a single clipping), keep the information of the profile and material
                     if ((addInfo & GenerateAdditionalInfo.GenerateProfileDef) != 0 && currRetVal.ProfileDefHandle != null)
                     {
                        retVal.MaterialAndProfile = currRetVal.MaterialAndProfile;
                        foreach (ElementId materialId in extraClippingData.MaterialIds)
                        {
                           retVal.MaterialAndProfile.Add(materialId, currRetVal.ProfileDefHandle);
                        }
                     }
                  }
                  else
                  {
                     extrusionBodyItems.Add(repHandle);
                     // This potentially is exported as a StandardCase element, keep the information of the profile and material
                     if ((addInfo & GenerateAdditionalInfo.GenerateProfileDef) != 0 && currRetVal.ProfileDefHandle != null)
                     {
                        retVal.MaterialAndProfile = currRetVal.MaterialAndProfile;
                        foreach (ElementId materialId in extraClippingData.MaterialIds)
                        {
                           retVal.MaterialAndProfile.Add(materialId, currRetVal.ProfileDefHandle);
                        }
                     }
                  }
               }
               else
               {
                  tr.RollBack();
                  return retVal;
               }

               // currRetVal will only have one extrusion.  Use the analyzer from the "last" extrusion.  Should only really be used for one extrusion.
               retVal.Analyzer = currRetVal.Analyzer;
               if (currRetVal.BaseRepresentationItems.Count > 0)
                  retVal.BaseRepresentationItems.Add(currRetVal.BaseRepresentationItems[0]);
            }

            IFCAnyHandle contextOfItemsBody = exporterIFC.Get3DContextHandle("Body");

            if (extrusionBodyItems.Count > 0 && (extrusionClippingBodyItems.Count == 0 && extrusionBooleanBodyItems.Count == 0))
            {
               if ((addInfo & GenerateAdditionalInfo.GenerateBody) != 0)
               {
                  retVal.Handle = RepresentationUtil.CreateSweptSolidRep(exporterIFC, element, catId, contextOfItemsBody,
                     extrusionBodyItems, null);
                  retVal.ShapeRepresentationType = ShapeRepresentationType.SweptSolid;
               }
            }
            else if (extrusionClippingBodyItems.Count > 0 && (extrusionBodyItems.Count == 0 && extrusionBooleanBodyItems.Count == 0))
            {
               if ((addInfo & GenerateAdditionalInfo.GenerateBody) != 0)
               {
                  retVal.Handle = RepresentationUtil.CreateClippingRep(exporterIFC, element, catId, contextOfItemsBody,
                     extrusionClippingBodyItems);
                  retVal.ShapeRepresentationType = ShapeRepresentationType.Clipping;
               }
            }
            else if (extrusionBooleanBodyItems.Count > 0 && (extrusionBodyItems.Count == 0 && extrusionClippingBodyItems.Count == 0))
            {
               if ((addInfo & GenerateAdditionalInfo.GenerateBody) != 0)
               {
                  retVal.Handle = RepresentationUtil.CreateCSGRep(exporterIFC, element, catId, contextOfItemsBody,
                     extrusionBooleanBodyItems);
                  retVal.ShapeRepresentationType = ShapeRepresentationType.CSG;
                  retVal.MaterialAndProfile.Clear();          // Clear material and profile info as it is only for StandardCase element
               }
            }
            else
            {
               if ((addInfo & GenerateAdditionalInfo.GenerateBody) != 0)
               {
                  // If both Clipping and extrusion exist, they will become boolean body Union
                  ICollection<IFCAnyHandle> booleanBodyItems = extrusionClippingBodyItems.Union<IFCAnyHandle>(extrusionBooleanBodyItems).ToList();
                  extrusionBodyItems.UnionWith(booleanBodyItems);
                  retVal.Handle = RepresentationUtil.CreateSweptSolidRep(exporterIFC, element, catId, contextOfItemsBody, extrusionBodyItems, null);
                  retVal.ShapeRepresentationType = ShapeRepresentationType.SweptSolid;
               }
            }

            tr.Commit();
            return retVal;
         }
      }

      private static bool AllowMultipleClipPlanesForCategory(ElementId cuttingElementCategoryId)
      {
         return !(cuttingElementCategoryId == new ElementId(BuiltInCategory.OST_Doors) ||
            cuttingElementCategoryId == new ElementId(BuiltInCategory.OST_Windows));
      }

      private static HandleAndAnalyzer CreateExtrusionWithClippingAndOpening(
         ExporterIFC exporterIFC, Element element, bool isVoid,
         Solid solid, Plane basePlane, XYZ planeOrigin, XYZ projDir, IFCRange range,
         out ExtraClippingData extraClippingData,
         GenerateAdditionalInfo addInfo = GenerateAdditionalInfo.GenerateBody,
         string profileName = null)
      {
         extraClippingData = new ExtraClippingData();
         HandleAndAnalyzer nullVal = new HandleAndAnalyzer();
         HandleAndAnalyzer retVal = new HandleAndAnalyzer();

         try
         {
            Plane extrusionAnalyzerPlane = GeometryUtil.CreatePlaneByXYVectorsContainingPoint(basePlane.XVec, basePlane.YVec, planeOrigin);
            ExtrusionAnalyzer elementAnalyzer = ExtrusionAnalyzer.Create(solid, extrusionAnalyzerPlane, projDir);
            retVal.Analyzer = elementAnalyzer;

            Document document = element.Document;
            XYZ baseLoopOffset = null;

            if (!MathUtil.IsAlmostZero(elementAnalyzer.StartParameter))
               baseLoopOffset = elementAnalyzer.StartParameter * projDir;

            Face extrusionBase = elementAnalyzer.GetExtrusionBase();
            retVal.MaterialAndProfile.CrossSectionArea = extrusionBase.Area;

            IList<GeometryUtil.FaceBoundaryType> boundaryTypes;
            IList<CurveLoop> extrusionBoundaryLoops =
                GeometryUtil.GetFaceBoundaries(extrusionBase, baseLoopOffset, out boundaryTypes);

            // Return if we get any CurveLoops that are complex, as we don't want to export an approximation of the boundary here.
            foreach (GeometryUtil.FaceBoundaryType boundaryType in boundaryTypes)
            {
               if (boundaryType == GeometryUtil.FaceBoundaryType.Complex)
                  return nullVal;
            }

            // Move base plane to start parameter location.
            Plane extrusionBasePlane = null;
            try
            {
               extrusionBasePlane = extrusionBoundaryLoops[0].GetPlane();
            }
            catch
            {
               return nullVal;
            }

            Transform extrusionBaseLCS = GeometryUtil.CreateTransformFromPlane(extrusionBasePlane);

            double extrusionLength = elementAnalyzer.EndParameter - elementAnalyzer.StartParameter;
            double baseOffset = extrusionBasePlane.Origin.DotProduct(projDir);
            IFCRange extrusionRange = new IFCRange(baseOffset, extrusionLength + baseOffset);

            double startParam = planeOrigin.DotProduct(projDir);
            double endParam = planeOrigin.DotProduct(projDir) + extrusionLength;
            if ((range != null) && (startParam >= range.End || endParam <= range.Start))
            {
               extraClippingData.CompletelyClipped = true;
               return nullVal;
            }

            double scaledExtrusionDepth = UnitUtil.ScaleLength(extrusionLength);

            // We use a sub-transaction here in case we are able to generate the base body but not the clippings.
            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle finalExtrusionBodyItemHnd = null;
            using (IFCTransaction tr = new IFCTransaction(file))
            {
               // For creating the actual extrusion, we want to use the calculated extrusion plane, not the input plane.
               IFCAnyHandle extrusionBodyItemHnd = ExtrusionExporter.CreateExtrudedSolidFromCurveLoop(exporterIFC, profileName,
                   extrusionBoundaryLoops, extrusionBaseLCS, projDir, scaledExtrusionDepth, false);
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(extrusionBodyItemHnd))
               {
                  if ((addInfo & GenerateAdditionalInfo.GenerateBody) != 0)
                     retVal.BaseRepresentationItems.Add(extrusionBodyItemHnd);

                  if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
                  {
                     if ((addInfo & GenerateAdditionalInfo.GenerateFootprint) != 0)
                     {
                        // Get the extrusion footprint using the first Curveloop. No transform is needed here because the profile is already transformed for the SweptArea
                        Transform lcs = GeometryUtil.CreateTransformFromVectorsAndOrigin(new XYZ(1, 0, 0), new XYZ(0, 1, 0), new XYZ(0, 0, 1), new XYZ(0, 0, 0));
                        retVal.FootPrintInfo = new FootPrintInfo(extrusionBoundaryLoops, lcs);
                     }
                     if ((addInfo & GenerateAdditionalInfo.GenerateProfileDef) != 0)
                     {
                        // Get the handle to the extrusion Swept Area needed for creation of IfcMaterialProfile
                        IFCData extrArea = extrusionBodyItemHnd.GetAttribute("SweptArea");
                        retVal.ProfileDefHandle = extrArea.AsInstance();
                        retVal.MaterialAndProfile.ExtrusionDepth = scaledExtrusionDepth;
                        retVal.MaterialAndProfile.OuterPerimeter = extrusionBoundaryLoops[0].GetExactLength();
                        for (int lcnt = 1; lcnt < extrusionBoundaryLoops.Count; ++lcnt)
                        {
                           if (extrusionBoundaryLoops[lcnt].IsCounterclockwise(extrusionBasePlane.Normal))
                              retVal.MaterialAndProfile.OuterPerimeter += extrusionBoundaryLoops[lcnt].GetExactLength();
                           else
                           {
                              if (retVal.MaterialAndProfile.InnerPerimeter.HasValue)
                                 retVal.MaterialAndProfile.InnerPerimeter += extrusionBoundaryLoops[lcnt].GetExactLength();
                              else
                                 retVal.MaterialAndProfile.InnerPerimeter = extrusionBoundaryLoops[lcnt].GetExactLength();
                           }
                        }
                        retVal.MaterialAndProfile.LCSTransformUsed = extrusionBaseLCS;
                     }
                  }

                  finalExtrusionBodyItemHnd = extrusionBodyItemHnd;
                  IDictionary<ElementId, ICollection<ICollection<Face>>> elementCutouts =
                      GeometryUtil.GetCuttingElementFaces(element, elementAnalyzer);

                  // A litle explanation is necessary here.
                  // We would like to ensure that, on export, we have a stable ordering of the clip planes that we create.
                  // The reason for this is that the order of the Boolean operations, if there are more than 1, can affect the end result; 
                  // while the ordering below does not improve the outcome of the set of Boolean operations, it does make it
                  // consistent across exports.  This, in turn, allows an importing application to also have consistent results from the export.
                  // GetCuttingElementFaces above does not guarantee any particular ordering of the faces (that will become clip planes and
                  // openings) returned; we may in the future to try do an ordering there.  In the meantime, we'll do a sort here.

                  IComparer<PlanarFace> planarFaceComparer = new PlanarFaceClipPlaneComparer();
                  IComparer<ICollection<PlanarFace>> planarFaceCollectionComparer = new PlanarFaceCollectionComparer();

                  // We will have three groups of faces:


                  // 1. Groups of PlanarFaces that allow only for simple cutouts (as determined by AllowMultipleClipPlanesForCategory).
                  // 2. Groups of PlanarFaces that don't have the restriction above (as determined by AllowMultipleClipPlanesForCategory).
                  // These two groups are in a list to avoid duplicated code later.
                  IList<ICollection<ICollection<PlanarFace>>> sortedElementCutouts =
                     new List<ICollection<ICollection<PlanarFace>>>();
                  sortedElementCutouts.Add(new SortedSet<ICollection<PlanarFace>>(planarFaceCollectionComparer)); // simple
                  sortedElementCutouts.Add(new SortedSet<ICollection<PlanarFace>>(planarFaceCollectionComparer)); // complex

                  // 3. Groups of arbitrary faces that may be converted into void extrusions.
                  ICollection<ICollection<Face>> unhandledElementCutouts = new HashSet<ICollection<Face>>();

                  // Go through the return value from GeometryUtil.GetCuttingElementFaces and populate the groups above.
                  foreach (KeyValuePair<ElementId, ICollection<ICollection<Face>>> elementCutoutsForElement in elementCutouts)
                  {
                     // allowMultipleClipPlanes is based on category, as determined in AllowMultipleClipPlanesForCategory.  Default is true.
                     Element cuttingElement = document.GetElement(elementCutoutsForElement.Key);
                     bool allowMultipleClipPlanes = true;
                     if (cuttingElement != null && cuttingElement.Category != null)
                        AllowMultipleClipPlanesForCategory(cuttingElement.Category.Id);

                     foreach (ICollection<Face> elementCutout in elementCutoutsForElement.Value)
                     {
                        // Need to make sure that all of the faces in elementCotout are all planar; otherwise add to the unhandled list.
                        ICollection<PlanarFace> planarFacesByNormal = new SortedSet<PlanarFace>(planarFaceComparer);

                        foreach (Face elementCutoutFace in elementCutout)
                        {
                           if (!(elementCutoutFace is PlanarFace))
                           {
                              planarFacesByNormal = null;
                              break;
                           }

                           planarFacesByNormal.Add(elementCutoutFace as PlanarFace);
                        }

                        if (planarFacesByNormal != null && planarFacesByNormal.Count != elementCutout.Count)
                           planarFacesByNormal = null; // Our comparer merged faces; this isn't good.

                        if (planarFacesByNormal != null)
                           sortedElementCutouts[allowMultipleClipPlanes ? 1 : 0].Add(planarFacesByNormal);
                        else
                           unhandledElementCutouts.Add(elementCutout);
                     }
                  }

                  // process clippings first, then openings
                  for (int ii = 0; ii < 2; ii++)
                  {
                     ICollection<ICollection<PlanarFace>> currentSortedElementCutouts = sortedElementCutouts[ii];
                     foreach (ICollection<PlanarFace> currentElementCutouts in currentSortedElementCutouts)
                     {
                        ICollection<Face> skippedFaces = null;
                        bool unhandledClipping = false;
                        try
                        {
                           bool allowMultipleClipPlanes = (ii == 1);
                           // The skippedFaces may represent openings that will be dealt with below.
                           finalExtrusionBodyItemHnd = GeometryUtil.CreateClippingFromPlanarFaces(exporterIFC, allowMultipleClipPlanes,
                              extrusionBaseLCS, projDir, currentElementCutouts, extrusionRange, finalExtrusionBodyItemHnd, out skippedFaces);
                        }
                        catch
                        {
                           skippedFaces = null;
                           unhandledClipping = true;
                        }

                        if (finalExtrusionBodyItemHnd == null || unhandledClipping)
                        {
                           ICollection<Face> currentUnhandledElementCutouts = new HashSet<Face>(currentElementCutouts);
                           unhandledElementCutouts.Add(currentUnhandledElementCutouts);
                        }
                        else
                        {
                           if (finalExtrusionBodyItemHnd != extrusionBodyItemHnd)
                           {
                              // IFC4RV does not support Clipping, so it needs to rollback and return null value
                              if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
                              {
                                 tr.RollBack();
                                 return nullVal;
                              }

                              extraClippingData.HasClippingResult = true;
                           }

                           // Even if we created a clipping, we may have faces to further process as openings.  
                           if (skippedFaces != null && skippedFaces.Count != 0)
                              unhandledElementCutouts.Add(skippedFaces);
                        }
                     }
                  }

                  IFCAnyHandle finalExtrusionClippingBodyItemHnd = finalExtrusionBodyItemHnd;
                  foreach (ICollection<Face> currentElementCutouts in unhandledElementCutouts)
                  {
                     bool unhandledOpening = false;
                     try
                     {
                        finalExtrusionBodyItemHnd = GeometryUtil.CreateOpeningFromFaces(exporterIFC, extrusionBasePlane, projDir,
                            currentElementCutouts, extrusionRange, finalExtrusionBodyItemHnd);
                     }
                     catch
                     {
                        unhandledOpening = true;
                     }

                     if (finalExtrusionBodyItemHnd == null || unhandledOpening)
                     {
                        // Item is completely clipped.  We use this only when we are certain:
                        // 1. finalExtrusionBodyItemHnd is null.
                        // 2. range is not null (i.e., we expect the possibility of clipping)
                        // 3. unhandledOpening is not true (i.e., we didn't abort the operation).
                        // If completelyClipped is true, we won't export the item, so we want to make sure
                        // that we don't actually want to try a backup method instead.
                        extraClippingData.CompletelyClipped = (finalExtrusionBodyItemHnd == null) && (range != null) && (!unhandledOpening);
                        tr.RollBack();
                        return nullVal;
                     }
                     else if (finalExtrusionBodyItemHnd != finalExtrusionClippingBodyItemHnd)
                     {
                        // IFC4RV does not support BooleanResult, so it needs to rollback and return null value
                        if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
                        {
                           tr.RollBack();
                           return nullVal;
                        }
                        extraClippingData.HasBooleanResult = true;
                     }
                  }

                  ElementId materialId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(solid, element);
                  extraClippingData.MaterialIds.Add(materialId);
                  if ((addInfo & GenerateAdditionalInfo.GenerateBody) != 0)
                  {
                     BodyExporter.CreateSurfaceStyleForRepItem(exporterIFC, document, isVoid, extrusionBodyItemHnd, materialId);
                  }
                  else
                  {
                     // If the body is not needed
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(extrusionBodyItemHnd))
                        IFCAnyHandleUtil.Delete(extrusionBodyItemHnd);
                  }
               }
               tr.Commit();
            }

            if ((addInfo & GenerateAdditionalInfo.GenerateBody) != 0)
               retVal.Handle = finalExtrusionBodyItemHnd;
            else
            {
               // If the body is not needed
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(finalExtrusionBodyItemHnd))
                  IFCAnyHandleUtil.Delete(finalExtrusionBodyItemHnd);
            }
            return retVal;
         }
         catch
         {
            return nullVal;
         }
      }

      /// <summary>
      /// Creates an extrusion with potential clipping from a solid representation of an element.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC class.</param>
      /// <param name="element">The element.</param>
      /// <param name="catId">The category of the element and/or the solid geometry.</param>
      /// <param name="solid">The solid geometry.</param>
      /// <param name="basePlane">The extrusion base plane.  The origin is ignored.</param>
      /// <param name="planeOrigin">The origin if the basePlane.</param>
      /// <param name="projDir">The projection direction.</param>
      /// <param name="range">The upper and lower limits of the extrusion, in the projection direction.</param>
      /// <param name="completelyClipped">Returns true if the extrusion is completely outside the range.</param>
      /// <returns>The extrusion handle.</returns>
      public static IFCAnyHandle CreateExtrusionWithClipping(
         ExporterIFC exporterIFC, Element element, bool isVoid, ElementId catId,
          Solid solid, Plane basePlane, XYZ planeOrigin, XYZ projDir, IFCRange range, 
          out ExtraClippingData extraClippingData,
          out FootPrintInfo footPrintInfo, out MaterialAndProfile materialAndProfile,
          GenerateAdditionalInfo addInfo = GenerateAdditionalInfo.GenerateBody, string profileName = null)
      {
         footPrintInfo = null;
         materialAndProfile = null;
         IList<Solid> solids = new List<Solid>();
         solids.Add(solid);
         HandleAndAnalyzer handleAndAnalyzer = CreateExtrusionWithClippingBase(exporterIFC, element, isVoid,
            catId, solids, basePlane, planeOrigin, projDir, range, 
            out extraClippingData, addInfo: addInfo, profileName: profileName);
         if ((addInfo & GenerateAdditionalInfo.GenerateFootprint) != 0)
         {
            footPrintInfo = handleAndAnalyzer.FootPrintInfo;
         }
         if ((addInfo & GenerateAdditionalInfo.GenerateProfileDef) != 0)
         {
            materialAndProfile = handleAndAnalyzer.MaterialAndProfile;
         }

         return handleAndAnalyzer.Handle;
      }


      /// <summary>
      /// Creates an extrusion with potential clipping from a list of solids corresponding to an element.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC class.</param>
      /// <param name="element">The element.</param>
      /// <param name="catId">The category of the element and/or the solid geometry.</param>
      /// <param name="solids">The list of solid geometries.</param>
      /// <param name="basePlane">The extrusion base plane.  The origin is ignored.</param>
      /// <param name="planeOrigin">The origin if the basePlane.</param>
      /// <param name="projDir">The projection direction.</param>
      /// <param name="range">The upper and lower limits of the extrusion, in the projection direction.</param>
      /// <param name="completelyClipped">Returns true if the extrusion is completely outside the range.</param>
      /// <param name="materialIds">The material ids of the solid geometry.</param>
      /// <returns>The extrusion handle.</returns>
      public static IFCAnyHandle CreateExtrusionWithClipping(ExporterIFC exporterIFC, Element element, 
         ElementId catId, bool isVoid, IList<Solid> solids, 
         Plane basePlane, XYZ planeOrigin, XYZ projDir, IFCRange range, out ExtraClippingData extraClippingData,
         out FootPrintInfo footPrintInfo, out MaterialAndProfile materialAndProfile, out IFCExtrusionCreationData extrusionData,
         GenerateAdditionalInfo addInfo = GenerateAdditionalInfo.GenerateBody, string profileName = null)
      {
         footPrintInfo = null;
         materialAndProfile = null;
         extrusionData = null;
         HandleAndAnalyzer handleAndAnalyzer = CreateExtrusionWithClippingBase(exporterIFC, element, isVoid, catId,
             solids, basePlane, planeOrigin, projDir, range, out extraClippingData, addInfo: addInfo, profileName: profileName);

         if ((addInfo & GenerateAdditionalInfo.GenerateFootprint) != 0)
         {
            footPrintInfo = handleAndAnalyzer.FootPrintInfo;
         }
         if ((addInfo & GenerateAdditionalInfo.GenerateProfileDef) != 0)
         {
            materialAndProfile = handleAndAnalyzer.MaterialAndProfile;
         }
         if (handleAndAnalyzer.Analyzer != null)
            extrusionData = GetExtrusionCreationDataFromAnalyzer(projDir, handleAndAnalyzer.Analyzer);

         return handleAndAnalyzer.Handle;
      }

      /// <summary>
      /// Creates an extrusion with potential clipping from a solid corresponding to an element, and supplies ExtrusionCreationData for the result.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC class.</param>
      /// <param name="element">The element.</param>
      /// <param name="catId">The category of the element and/or the solid geometry.</param>
      /// <param name="solid">The solid geometry.</param>
      /// <param name="basePlane">The extrusion base plane.  The origin is ignored.</param>
      /// <param name="planeOrigin">The origin if the basePlane.</param>
      /// <param name="projDir">The projection direction.</param>
      /// <param name="range">The upper and lower limits of the extrusion, in the projection direction.</param>
      /// <param name="completelyClipped">Returns true if the extrusion is completely outside the range.</param>
      /// <returns>The extrusion handle.</returns>
      public static HandleAndData CreateExtrusionWithClippingAndProperties(ExporterIFC exporterIFC,
         Element element, bool isVoid, ElementId catId, Solid solid, Plane basePlane, XYZ planeOrig, XYZ projDir, IFCRange range, 
         out ExtraClippingData extraClippingData,
         GenerateAdditionalInfo addInfo = GenerateAdditionalInfo.GenerateBody,
         string profileName = null)
      {
         IList<Solid> solids = new List<Solid>();
         solids.Add(solid);

         HandleAndAnalyzer handleAndAnalyzer = CreateExtrusionWithClippingBase(exporterIFC, element, isVoid, catId,
             solids, basePlane, planeOrig, projDir, range, out extraClippingData, addInfo: addInfo, profileName: profileName);

         HandleAndData ret = new HandleAndData();
         ret.Handle = handleAndAnalyzer.Handle;     // Add the "Body" representation
         ret.FootprintInfo = handleAndAnalyzer.FootPrintInfo;    //Add the "FootPrint" representation
         ret.BaseRepresentationItems = handleAndAnalyzer.BaseRepresentationItems;
         ret.ShapeRepresentationType = handleAndAnalyzer.ShapeRepresentationType;
         ret.MaterialIds = extraClippingData.MaterialIds;
         if (handleAndAnalyzer.Analyzer != null)
            ret.Data = GetExtrusionCreationDataFromAnalyzer(projDir, handleAndAnalyzer.Analyzer);
         if ((addInfo & GenerateAdditionalInfo.GenerateProfileDef) != 0)
            ret.MaterialAndProfile = handleAndAnalyzer.MaterialAndProfile;
         return ret;
      }

      /// <summary>
      /// Creates an extruded surface of type IfcSurfaceOfLinearExtrusion given a base 2D curve, a direction and a length.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC class.</param>
      /// <param name="baseCurve">The curve to be extruded.</param>
      /// <param name="extrusionLCS">The coordinate system of the extrusion, where the Z direction is the direction of the extrusion.</param>
      /// <param name="scaledExtrusionSize">The length of the extrusion, in IFC unit scale.</param>
      /// <param name="unscaledBaseHeight">The Z value of the base level for the surface, in Revit unit scale.</param>
      /// <param name="sweptCurve">The handle of the created curve entity.</param>
      /// <returns>The extrusion handle.</returns>
      /// <remarks>Note that scaledExtrusionSize and unscaledBaseHeight are in potentially different scaling units.</remarks>
      public static IFCAnyHandle CreateSurfaceOfLinearExtrusionFromCurve(ExporterIFC exporterIFC, Curve baseCurve, Transform extrusionLCS,
          double scaledExtrusionSize, double unscaledBaseHeight, out IFCAnyHandle curveHandle)
      {
         curveHandle = null;

         IFCFile file = exporterIFC.GetFile();

         XYZ extrusionDir = extrusionLCS.BasisZ;
         IList<IFCAnyHandle> profileCurves = null;

         // A list of IfcCurve entities.
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
         {
            IFCAnyHandle curveHnd;
            try
            {
               curveHnd = GeometryUtil.CreatePolyCurveFromCurve(exporterIFC, baseCurve, extrusionLCS, extrusionDir);
            }
            catch
            {
               curveHnd = GeometryUtil.OutdatedCreatePolyCurveFromCurve(exporterIFC, baseCurve, extrusionLCS, extrusionDir);
            }

            profileCurves = new List<IFCAnyHandle>();
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(curveHnd))
               profileCurves.Add(curveHnd);
         }
         else
         {
            IFCGeometryInfo info = IFCGeometryInfo.CreateCurveGeometryInfo(exporterIFC, extrusionLCS, extrusionDir, true);
            ExporterIFCUtils.CollectGeometryInfo(exporterIFC, info, baseCurve, XYZ.Zero, true);

            profileCurves = info.GetCurves();
         }

         if ((profileCurves.Count != 1) || (!IFCAnyHandleUtil.IsSubTypeOf(profileCurves[0], IFCEntityType.IfcBoundedCurve)))
            return null;

         curveHandle = profileCurves[0];
         IFCAnyHandle sweptCurve = IFCInstanceExporter.CreateArbitraryOpenProfileDef(file, IFCProfileType.Curve, null, profileCurves[0]);

         XYZ oCurveOrig = baseCurve.GetEndPoint(0);
         XYZ orig = UnitUtil.ScaleLength(new XYZ(0.0, 0.0, oCurveOrig[2] - unscaledBaseHeight));

         IFCAnyHandle surfaceAxis = ExporterUtil.CreateAxis(file, orig, null, null);
         IFCAnyHandle direction = ExporterUtil.CreateDirection(file, extrusionDir);     // zDir

         return IFCInstanceExporter.CreateSurfaceOfLinearExtrusion(file, sweptCurve, surfaceAxis, direction, scaledExtrusionSize);
      }

      /// <summary>
      /// Creates an IfcConnectionSurfaceGeometry given a base 2D curve, a direction and a length.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC class.</param>
      /// <param name="baseCurve">The curve to be extruded.</param>
      /// <param name="extrusionLCS">The local coordinate system whose XY plane contains the baseCurve, and whose normal is the direction of the extrusion.</param>
      /// <param name="scaledExtrusionSize">The length of the extrusion, in IFC unit scale.</param>
      /// <param name="unscaledBaseHeight">The Z value of the base level for the surface, in Revit unit scale.</param>
      /// <returns>The extrusion handle.</returns>
      /// <remarks>Note that scaledExtrusionSize and unscaledBaseHeight are in potentially different scaling units.</remarks>
      public static IFCAnyHandle CreateConnectionSurfaceGeometry(ExporterIFC exporterIFC, Curve baseCurve, Transform extrusionLCS,
            double scaledExtrusionSize, double unscaledBaseHeight)
      {
         IFCFile file = exporterIFC.GetFile();

         IFCAnyHandle sweptCurve;
         IFCAnyHandle surfOnRelatingElement = CreateSurfaceOfLinearExtrusionFromCurve(exporterIFC, baseCurve, extrusionLCS,
             scaledExtrusionSize, unscaledBaseHeight, out sweptCurve);

         return IFCInstanceExporter.CreateConnectionSurfaceGeometry(file, surfOnRelatingElement, null);
      }
   }
}