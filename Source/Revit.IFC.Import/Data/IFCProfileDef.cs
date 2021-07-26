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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public class IFCProfileDef : IFCEntity
   {
      protected interface IIFCProfileSegment
      {
         Curve Create();
      }

      protected class IFCProfileLineSegment : IIFCProfileSegment
      {
         private XYZ StartPoint { get; set; } = null;

         private XYZ EndPoint { get; set; } = null;

         public IFCProfileLineSegment(XYZ startPoint, XYZ endPoint)
         {
            StartPoint = startPoint;
            EndPoint = endPoint;
         }

         public Curve Create()
         {
            return Line.CreateBound(StartPoint, EndPoint);
         }
      }

      protected IFCProfileDef()
      {
      }

      /// <summary>
      /// Get the type of the profile.
      /// </summary>
      public IFCProfileType ProfileType { get; protected set; }

      /// <summary>
      /// Get the name of the profile.
      /// </summary>
      public string ProfileName { get; protected set; } = null;

      /// <summary>
      /// Attempt to add a group of curve segments to a curve loop.
      /// </summary>
      /// <param name="curveLoop">The curve loop.</param>
      /// <param name="segments">The description of the curve segments.</param>
      /// <returns>True if the operation succeeded, false otherwise.</returns>
      /// <remarks>
      /// It is up to the caller to decide what to do if this returns false.
      /// Regardless of whether the function succeeds or fails, <paramref name="segments"/>
      /// will be cleared afterwards, to either allow for the next set of curves to be added,
      /// or for an alternate representation to be found.
      /// </remarks>
      protected bool AppendSegmentsToCurveLoop(CurveLoop curveLoop, IList<IIFCProfileSegment> segments)
      {
         // We are explicitly trying to catch situations where the curves in the curve loop
         // are too small to be created, or otherwise illegal.  We are not trying to catch
         // the situation where the CurveLoop itself doesn't like the curves, since we are 
         // constructing the line segments ourselves, and expect the calculations to be correct.
         IList<Curve> curvesToAppend = new List<Curve>();
         try
         {
            foreach (IIFCProfileSegment segment in segments)
            {
               curvesToAppend.Add(segment.Create());
            }
         }
         catch (Exception ex)
         {
            if (ex == null || ex.Message == null || !ex.Message.Contains("Curve length is too small"))
               throw ex;

            // Returning false allows the calling code to try a backup for the curve loop,
            // in cases where the issue arises from filleting, for example.
            segments.Clear();
            return false;
         }

         foreach (Curve curveToAppend in curvesToAppend)
         {
            curveLoop.Append(curveToAppend);
         }

         segments.Clear();
         return true;
      }

      override protected void Process(IFCAnyHandle profileDef)
      {
         base.Process(profileDef);

         ProfileType = IFCEnums.GetSafeEnumerationAttribute<IFCProfileType>(profileDef, "ProfileType", IFCProfileType.Area);

         ProfileName = IFCAnyHandleUtil.GetStringAttribute(profileDef, "ProfileName");
      }

      /// <summary>
      /// Create an IFCProfileDef object from a handle of type IfcProfileDef.
      /// </summary>
      /// <param name="ifcProfileDef">The IFC handle.</param>
      /// <returns>The IFCProfileDef object.</returns>
      public static IFCProfileDef ProcessIFCProfileDef(IFCAnyHandle ifcProfileDef)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcProfileDef))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcProfileDef);
            return null;
         }

         IFCEntity profileDef;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcProfileDef.StepId, out profileDef))
            return (profileDef as IFCProfileDef);

         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcProfileDef, IFCEntityType.IfcCompositeProfileDef))
            return IFCCompositeProfile.ProcessIFCCompositeProfile(ifcProfileDef);

         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcProfileDef, IFCEntityType.IfcDerivedProfileDef))
            return IFCDerivedProfileDef.ProcessIFCDerivedProfileDef(ifcProfileDef);

         //if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcProfileDef, IFCEntityType.IfcArbitraryOpenProfileDef))
         //if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcProfileDef, IFCEntityType.IfcArbitraryClosedProfileDef))
         // IFC2x files don't have IfcParameterizedProfileDef, so we won't check the type. 
         // If profileDef is the wrong entity type, it will fail in ProcessIFCParameterizedProfileDef.
         return IFCSimpleProfile.ProcessIFCSimpleProfile(ifcProfileDef);
      }
   }

   /// <summary>
   /// Provides methods to process IfcProfileDef and its subclasses.
   /// </summary>
   public class IFCCompositeProfile : IFCProfileDef
   {
      private IList<IFCProfileDef> m_Profiles = null;

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCCompositeProfile()
      {

      }

      protected override void Process(IFCAnyHandle profileDef)
      {
         base.Process(profileDef);

         CompositeProfileDefLabel = IFCAnyHandleUtil.GetStringAttribute(profileDef, "Label");

         IList<IFCAnyHandle> profileHnds = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(profileDef, "Profiles");
         foreach (IFCAnyHandle profileHnd in profileHnds)
         {
            IFCProfileDef subProfile = IFCProfileDef.ProcessIFCProfileDef(profileHnd);
            if (subProfile != null)
               Profiles.Add(subProfile);
         }
      }

      protected IFCCompositeProfile(IFCAnyHandle profileDef)
      {
         Process(profileDef);
      }

      /// <summary>
      /// Create an IFCCompositeProfile object from a handle of type IfcCompositeProfileDef.
      /// </summary>
      /// <param name="ifcProfileDef">The IFC handle.</param>
      /// <returns>The IFCCompositeProfile object.</returns>
      public static IFCCompositeProfile ProcessIFCCompositeProfile(IFCAnyHandle ifcProfileDef)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcProfileDef))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcCompositeProfileDef);
            return null;
         }

         IFCEntity profileDef;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcProfileDef.StepId, out profileDef))
            return (profileDef as IFCCompositeProfile);

         return new IFCCompositeProfile(ifcProfileDef);
      }

      /// <summary>
      /// Get the label for an IfcCompositeProfileDef
      /// </summary>
      public string CompositeProfileDefLabel { get; protected set; } = null;

      /// <summary>
      /// Get the list of contained profiles.
      /// </summary>
      public IList<IFCProfileDef> Profiles
      {
         get
         {
            if (m_Profiles == null)
               m_Profiles = new List<IFCProfileDef>();
            return m_Profiles;
         }
      }
   }

   // We may create more subclasses if we want to preserve the original parametric data.
   public class IFCParameterizedProfile : IFCSimpleProfile
   {
      protected class IFCProfileXYArcSegment : IIFCProfileSegment
      {
         protected XYZ Center { get; private set; } = null;

         protected double Radius { get; private set; } = 0.0;

         protected double StartAngle { get; private set; } = 0.0;

         protected double EndAngle { get; private set; } = 0.0;

         protected bool Reverse { get; private set; } = false;

         public IFCProfileXYArcSegment(XYZ center, double radius, double startAngle, double endAngle, bool reverse)
         {
            Center = center;
            Radius = radius;
            StartAngle = startAngle;
            EndAngle = endAngle;
            Reverse = reverse;
         }

         public virtual Curve Create()
         {
            Arc arc = Arc.Create(Center, Radius, StartAngle, EndAngle, XYZ.BasisX, XYZ.BasisY);
            if (!Reverse)
               return arc;
            return arc.CreateReversed() as Arc;
         }
      }

      protected class IFCProfileXYEllipseSegment : IFCProfileXYArcSegment
      {
         private double RadiusY { get; set; } = 0.0;
         
         public IFCProfileXYEllipseSegment(XYZ center, double radiusX, double radiusY, double startAngle, double endAngle):
            base(center, radiusX, startAngle, endAngle, false)
         {
            RadiusY = radiusY;
         }

         public override Curve Create()
         {
            return Ellipse.CreateCurve(Center, Radius, RadiusY, XYZ.BasisX, XYZ.BasisY, StartAngle, EndAngle);
         }
      }

      private CurveLoop CreateProfilePolyCurveLoop(XYZ[] corners)
      {
         int sz = corners.Count();
         if (sz == 0)
            return null;

         CurveLoop curveLoop = new CurveLoop();
         IList<IIFCProfileSegment> segments = new List<IIFCProfileSegment>();
         for (int ii = 0; ii < sz; ii++)
         {
            segments.Add(new IFCProfileLineSegment(corners[ii], corners[(ii + 1) % sz]));
         }
         if (!AppendSegmentsToCurveLoop(curveLoop, segments))
            return null;

         return curveLoop;
      }

      private CurveLoop CreateFilletedRectangleCurveLoop(XYZ[] corners, double filletRadius)
      {
         int sz = corners.Count();
         if (sz != 4)
            return null;

         XYZ[] radii = new XYZ[4] {
                new XYZ( corners[0].X + filletRadius, corners[0].Y + filletRadius, 0.0 ),
                new XYZ( corners[1].X - filletRadius, corners[1].Y + filletRadius, 0.0 ),
                new XYZ( corners[2].X - filletRadius, corners[2].Y - filletRadius, 0.0 ),
                new XYZ( corners[3].X + filletRadius, corners[3].Y - filletRadius, 0.0 ),
            };

         XYZ[] fillets = new XYZ[8] {
                new XYZ( corners[0].X, corners[0].Y + filletRadius, 0.0 ),
                new XYZ( corners[0].X + filletRadius, corners[0].Y, 0.0 ),
                new XYZ( corners[1].X - filletRadius, corners[1].Y, 0.0 ),
                new XYZ( corners[1].X, corners[1].Y + filletRadius, 0.0 ),
                new XYZ( corners[2].X, corners[2].Y - filletRadius, 0.0 ),
                new XYZ( corners[2].X - filletRadius, corners[2].Y, 0.0 ),
                new XYZ( corners[3].X + filletRadius, corners[3].Y, 0.0 ),
                new XYZ( corners[3].X, corners[3].Y - filletRadius, 0.0 )
            };

         CurveLoop curveLoop = new CurveLoop();
         IList<IIFCProfileSegment> segments = new List<IIFCProfileSegment>();
         for (int ii = 0; ii < 4; ii++)
         {
            segments.Add(new IFCProfileLineSegment(fillets[ii * 2 + 1], fillets[(ii * 2 + 2) % 8]));
            
            double startAngle = Math.PI * ((ii + 3) % 4) / 2;
            segments.Add(new IFCProfileXYArcSegment(radii[(ii + 1) % 4], filletRadius, startAngle, startAngle + Math.PI / 2, false));
         }

         return AppendSegmentsToCurveLoop(curveLoop, segments) ? curveLoop : null;
      }

      private void ProcessIFCRoundedRectangleProfileDef(IFCAnyHandle profileDef,
          double xDimVal, double yDimVal, double roundedRadiusVal)
      {
         XYZ[] corners = new XYZ[4] {
                new XYZ( -xDimVal/2.0, -yDimVal/2.0, 0.0 ),
                new XYZ( xDimVal/2.0, -yDimVal/2.0, 0.0 ),
                new XYZ( xDimVal/2.0, yDimVal/2.0, 0.0 ),
                new XYZ( -xDimVal/2.0, yDimVal/2.0, 0.0 )
            };

         OuterCurve = CreateFilletedRectangleCurveLoop(corners, roundedRadiusVal);
      }

      private void ProcessIFCRectangleHollowProfileDef(IFCAnyHandle profileDef,
          double xDimVal, double yDimVal)
      {
         XYZ[] corners = new XYZ[4] {
                new XYZ( -xDimVal/2.0, -yDimVal/2.0, 0.0 ),
                new XYZ( xDimVal/2.0, -yDimVal/2.0, 0.0 ),
                new XYZ( xDimVal/2.0, yDimVal/2.0, 0.0 ),
                new XYZ( -xDimVal/2.0, yDimVal/2.0, 0.0 )
            };

         double outerFilletRadius = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "OuterFilletRadius", 0.0);
         bool hasFillet = (outerFilletRadius > MathUtil.Eps()) && (outerFilletRadius < ((Math.Min(xDimVal, yDimVal) / 2.0) - MathUtil.Eps()));

         if (hasFillet)
         {
            OuterCurve = CreateFilletedRectangleCurveLoop(corners, outerFilletRadius);
            if (OuterCurve == null)
            {
               Importer.TheLog.LogError(Id, "Couldn't process fillets for outer loop for IfcRectangleHollowProfileDef, ignoring.", false);
               hasFillet = false;
            }
         }

         if (!hasFillet)
            OuterCurve = CreateProfilePolyCurveLoop(corners);

         if (OuterCurve == null)
            Importer.TheLog.LogError(Id, "Couldn't create IfcRectangleHollowProfileDef, ignoring.", true);

         double wallThickness = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "WallThickness", 0.0);
         if ((wallThickness > MathUtil.Eps()) && (wallThickness < ((Math.Min(xDimVal, yDimVal) / 2.0) - MathUtil.Eps())))
         {
            double innerXDimVal = xDimVal - wallThickness * 2.0;
            double innerYDimVal = yDimVal - wallThickness * 2.0;
            XYZ[] innerCorners = new XYZ[4] {
                    new XYZ( -innerXDimVal/2.0, -innerYDimVal/2.0, 0.0 ),
                    new XYZ( innerXDimVal/2.0, -innerYDimVal/2.0, 0.0 ),
                    new XYZ( innerXDimVal/2.0, innerYDimVal/2.0, 0.0 ),
                    new XYZ( -innerXDimVal/2.0, innerYDimVal/2.0, 0.0 )
                };

            double innerFilletRadius = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "InnerFilletRadius", 0.0);
            if ((innerFilletRadius > MathUtil.Eps()) && (innerFilletRadius < ((Math.Min(innerXDimVal, innerYDimVal) / 2.0) - MathUtil.Eps())))
            {
               CurveLoop innerLoop = CreateFilletedRectangleCurveLoop(innerCorners, innerFilletRadius);
               if (innerLoop != null)
                  InnerCurves.Add(innerLoop);
               else
                  Importer.TheLog.LogError(Id, "Couldn't process fillets for inner loop for IfcRectangleHollowProfileDef, ignoring.", false);
            }

            if (InnerCurves.Count == 0)
            {
               CurveLoop innerLoop = CreateProfilePolyCurveLoop(innerCorners);
               if (innerLoop != null)
                  InnerCurves.Add(innerLoop);
               else
                  Importer.TheLog.LogError(Id, "Couldn't process inner loop for IfcRectangleHollowProfileDef, ignoring.", false);
            }
         }
      }

      private void ProcessIFCRectangleProfileDef(IFCAnyHandle profileDef)
      {
         bool found = false;
         double xDim = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "XDim", out found);
         if (!found)
            return;

         double yDim = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "YDim", out found);
         if (!found)
            return;

         if (xDim < MathUtil.Eps())
            Importer.TheLog.LogError(Id, "IfcRectangleProfileDef has invalid XDim: " + xDim + ", ignoring.", true);

         if (yDim < MathUtil.Eps())
            Importer.TheLog.LogError(Id, "IfcRectangleProfileDef has invalid YDim: " + yDim + ", ignoring.", true);

         if (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC2x2) && IFCAnyHandleUtil.IsSubTypeOf(profileDef, IFCEntityType.IfcRectangleHollowProfileDef))
         {
            ProcessIFCRectangleHollowProfileDef(profileDef, xDim, yDim);
            return;
         }

         if (IFCAnyHandleUtil.IsSubTypeOf(profileDef, IFCEntityType.IfcRoundedRectangleProfileDef))
         {
            double roundedRadius = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "RoundedRadius", 0.0);
            if ((roundedRadius > MathUtil.Eps()) && (roundedRadius < ((Math.Min(xDim, yDim) / 2.0) - MathUtil.Eps())))
            {
               ProcessIFCRoundedRectangleProfileDef(profileDef, xDim, yDim, roundedRadius);
               return;
            }
         }

         XYZ[] corners = new XYZ[4] {
                new XYZ( -xDim/2.0, -yDim/2.0, 0.0 ),
                new XYZ( xDim/2.0, -yDim/2.0, 0.0 ),
                new XYZ( xDim/2.0, yDim/2.0, 0.0 ),
                new XYZ( -xDim/2.0, yDim/2.0, 0.0 )
            };

         OuterCurve = CreateProfilePolyCurveLoop(corners);
         if (OuterCurve == null)
            Importer.TheLog.LogError(Id, "Couldn't process IfcRectangleProfileDef, ignoring.", true);
      }

      private void ProcessIFCCircleProfileDef(IFCAnyHandle profileDef)
      {
         bool found = false;
         double radius = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "Radius", out found);
         if (!found)
            return;

         if (radius < MathUtil.Eps())
            Importer.TheLog.LogError(Id, "IfcCircleProfileDef has invalid radius: " + radius + ", ignoring.", true);

         // Some internal routines want CurveLoops with bounded components.  Split to avoid problems.
         OuterCurve = new CurveLoop();
         IList<IIFCProfileSegment> segments = new List<IIFCProfileSegment>();
         segments.Add(new IFCProfileXYArcSegment(XYZ.Zero, radius, 0, Math.PI, false));
         segments.Add(new IFCProfileXYArcSegment(XYZ.Zero, radius, Math.PI, 2 * Math.PI, false));
         if (!AppendSegmentsToCurveLoop(OuterCurve, segments))
            Importer.TheLog.LogError(Id, "Couldn't create IfcCircleHollowProfileDef, ignoring.", true);

         if (IFCAnyHandleUtil.IsSubTypeOf(profileDef, IFCEntityType.IfcCircleHollowProfileDef))
         {
            double wallThickness = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "WallThickness", 0.0);
            if (wallThickness > MathUtil.Eps() && wallThickness < radius)
            {
               double innerRadius = radius - wallThickness;

               CurveLoop innerCurve = new CurveLoop();

               IList<IIFCProfileSegment> innerSegments = new List<IIFCProfileSegment>();
               innerSegments.Add(new IFCProfileXYArcSegment(XYZ.Zero, innerRadius, 0, Math.PI, false));
               innerSegments.Add(new IFCProfileXYArcSegment(XYZ.Zero, innerRadius, Math.PI, 2 * Math.PI, false));
               if (AppendSegmentsToCurveLoop(innerCurve, innerSegments))
                  InnerCurves.Add(innerCurve);
               else
                  Importer.TheLog.LogError(Id, "Couldn't create inner loop for IfcCircleHollowProfileDef, ignoring.", false);
            }
         }
      }

      private void ProcessIFCEllipseProfileDef(IFCAnyHandle profileDef)
      {
         bool found = false;
         double radiusX = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "SemiAxis1", out found);
         if (!found)
            return;

         double radiusY = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "SemiAxis2", out found);
         if (!found)
            return;

         // Some internal routines want CurveLoops with bounded components.  Split to avoid problems.
         OuterCurve = new CurveLoop();
         IList<IIFCProfileSegment> segments = new List<IIFCProfileSegment>();
         segments.Add(new IFCProfileXYEllipseSegment(XYZ.Zero, radiusX, radiusY, 0, Math.PI));
         segments.Add(new IFCProfileXYEllipseSegment(XYZ.Zero, radiusX, radiusY, Math.PI, 2 * Math.PI));
         if (!AppendSegmentsToCurveLoop(OuterCurve, segments))
            Importer.TheLog.LogError(Id, "Couldn't create IfcEllipseProfileDef, ignoring.", true);
      }

      private void ProcessIFCCShapeProfileDef(IFCAnyHandle profileDef)
      {
         bool found = false;
         double depth = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "Depth", out found);
         if (!found)
            return;

         double width = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "Width", out found);
         if (!found)
            return;

         double wallThickness = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "WallThickness", out found);
         if (!found)
            return;

         double girth = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "Girth", out found);
         if (!found)
            return;

         // Optional parameters
         double centerOptX = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "CentreOfGravityInX", 0.0);
         double innerRadius = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "InternalFilletRadius", 0.0);

         bool hasFillet = !MathUtil.IsAlmostZero(innerRadius);
         double outerRadius = hasFillet ? innerRadius + wallThickness : 0.0;

         XYZ[] cShapePoints = new XYZ[12] {
                new XYZ(width/2.0 + centerOptX, -depth/2.0+girth, 0.0),
                new XYZ(width/2.0 + centerOptX, -depth/2.0, 0.0),
                new XYZ(-width/2.0 + centerOptX, -depth/2.0, 0.0),
                new XYZ(-width/2.0 + centerOptX, depth/2.0, 0.0),
                new XYZ(width/2.0 + centerOptX, depth/2.0, 0.0),
                new XYZ(width/2.0 + centerOptX, -(-depth/2.0+girth), 0.0),
                new XYZ(width/2.0 - wallThickness, -(-depth/2.0+girth), 0.0),
                new XYZ(width/2.0 - wallThickness, depth/2.0 - wallThickness, 0.0),
                new XYZ(-width/2.0 + wallThickness, depth/2.0 - wallThickness, 0.0),
                new XYZ(-width/2.0 + wallThickness, -depth/2.0 + wallThickness, 0.0),
                new XYZ(width/2.0 - wallThickness, -depth/2.0 + wallThickness, 0.0),
                new XYZ(width/2.0 + centerOptX - wallThickness, -depth/2.0+girth, 0.0)
            };

         if (hasFillet)
         {
            XYZ[] cFilletPoints = new XYZ[16] {
                    new XYZ(cShapePoints[1][0], cShapePoints[1][1] + outerRadius, 0.0),
                    new XYZ(cShapePoints[1][0] - outerRadius, cShapePoints[1][1], 0.0),
                    new XYZ(cShapePoints[2][0] + outerRadius, cShapePoints[2][1], 0.0),
                    new XYZ(cShapePoints[2][0], cShapePoints[2][1] + outerRadius, 0.0),
                    new XYZ(cShapePoints[3][0], cShapePoints[3][1] - outerRadius, 0.0),
                    new XYZ(cShapePoints[3][0] + outerRadius, cShapePoints[3][1], 0.0),
                    new XYZ(cShapePoints[4][0] - outerRadius, cShapePoints[4][1], 0.0),
                    new XYZ(cShapePoints[4][0], cShapePoints[4][1] - outerRadius, 0.0),
                    new XYZ(cShapePoints[7][0], cShapePoints[7][1] - innerRadius, 0.0),
                    new XYZ(cShapePoints[7][0] - innerRadius, cShapePoints[7][1], 0.0),
                    new XYZ(cShapePoints[8][0] + innerRadius, cShapePoints[8][1], 0.0),
                    new XYZ(cShapePoints[8][0], cShapePoints[8][1] - innerRadius, 0.0),
                    new XYZ(cShapePoints[9][0], cShapePoints[9][1] + innerRadius, 0.0),
                    new XYZ(cShapePoints[9][0] + innerRadius, cShapePoints[9][1], 0.0),
                    new XYZ(cShapePoints[10][0] - innerRadius, cShapePoints[10][1], 0.0),
                    new XYZ(cShapePoints[10][0], cShapePoints[10][1] + innerRadius, 0.0)
                };

            // shared for inner and outer.
            XYZ[] cFilletCenters = new XYZ[4] {
                    new XYZ(cShapePoints[1][0] - outerRadius, cShapePoints[1][1] + outerRadius, 0.0),
                    new XYZ(cShapePoints[2][0] + outerRadius, cShapePoints[2][1] + outerRadius, 0.0),
                    new XYZ(cShapePoints[3][0] + outerRadius, cShapePoints[3][1] - outerRadius, 0.0),
                    new XYZ(cShapePoints[4][0] - outerRadius, cShapePoints[4][1] - outerRadius, 0.0)
                };

            // flip outers not inners.
            double[][] cRange = new double[4][] {
                    new double[2] { 3*Math.PI/2.0, 2.0*Math.PI },
                    new double[2] { Math.PI, 3*Math.PI/2.0 },
                    new double[2] { Math.PI/2.0, Math.PI },
                    new double[2] { 0.0, Math.PI/2.0 }
                };

            OuterCurve = new CurveLoop();
            IList<IIFCProfileSegment> segments = new List<IIFCProfileSegment>();

            segments.Add(new IFCProfileLineSegment(cShapePoints[0], cFilletPoints[0]));
            for (int ii = 0; ii < 3; ii++)
            {
               segments.Add(new IFCProfileXYArcSegment(cFilletCenters[ii], outerRadius, cRange[ii][0], cRange[ii][1], true));
               segments.Add(new IFCProfileLineSegment(cFilletPoints[2 * ii + 1], cFilletPoints[2 * ii + 2]));
               segments.Add(new IFCProfileXYArcSegment(cFilletCenters[3], outerRadius, cRange[3][0], cRange[3][1], true));
               segments.Add(new IFCProfileLineSegment(cFilletPoints[7], cShapePoints[5]));
               segments.Add(new IFCProfileLineSegment(cShapePoints[5], cShapePoints[6]));
               segments.Add(new IFCProfileLineSegment(cShapePoints[6], cFilletPoints[8]));
               
               for (int jj = 0; jj < 3; jj++)
               {
                  segments.Add(new IFCProfileXYArcSegment(cFilletCenters[3 - jj], innerRadius, cRange[3 - jj][0], cRange[3 - jj][1], false));
                  segments.Add(new IFCProfileLineSegment(cFilletPoints[2 * jj + 9], cFilletPoints[2 * jj + 10]));
               }

               segments.Add(new IFCProfileXYArcSegment(cFilletCenters[0], innerRadius, cRange[0][0], cRange[0][1], false));
               segments.Add(new IFCProfileLineSegment(cFilletPoints[15], cShapePoints[11]));
               segments.Add(new IFCProfileLineSegment(cShapePoints[11], cShapePoints[0]));
            }

            if (!AppendSegmentsToCurveLoop(OuterCurve, segments))
            {
               Importer.TheLog.LogError(Id, "Couldn't create filleted IfcCShapeProfileDef, removing fillets.", false);
               hasFillet = false;
            }
         }
         
         // If trying to create a filleted profile above failed, we will try this as 
         // a backup, even if hasFillet was originally set to true.
         if (!hasFillet)
         {
            OuterCurve = CreateProfilePolyCurveLoop(cShapePoints);
         }

         if (OuterCurve == null)
            Importer.TheLog.LogError(Id, "Couldn't process IfcCShapeProfileDef, ignoring.", true);
      }

      private void ProcessIFCLShapeProfileDef(IFCAnyHandle profileDef)
      {
         bool found = false;
         double depth = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "Depth", out found);
         if (!found)
            return;

         double thickness = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "Thickness", out found);
         if (!found)
            return;

         double width = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "Width", depth);

         double filletRadius = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "FilletRadius", 0.0);
         bool filletedCorner = !MathUtil.IsAlmostZero(filletRadius);

         double edgeRadius = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "EdgeRadius", 0.0);
         bool filletedEdge = !MathUtil.IsAlmostZero(edgeRadius);
         if (filletedEdge && (thickness < edgeRadius - MathUtil.Eps()))
         {
            Importer.TheLog.LogWarning(profileDef.Id, "Fillet edge radius is at least as large as the thicknes of the profile, ignoring the fillet.", false);
            filletedEdge = false;
         }

         bool fullFilletedEdge = (filletedEdge && MathUtil.IsAlmostEqual(thickness, edgeRadius));

         double centerOptX = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "CentreOfGravityInX", 0.0);

         double centerOptY = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "CentreOfGravityInY", centerOptX);

         // TODO: use leg slope
         double legSlope = IFCImportHandleUtil.GetOptionalScaledAngleAttribute(profileDef, "LegSlope", 0.0);

         XYZ lOrig = new XYZ(-width / 2.0 + centerOptX, -depth / 2.0 + centerOptY, 0.0);
         XYZ lLR = new XYZ(lOrig[0] + width, lOrig[1], 0.0);
         XYZ lLRPlusThickness = new XYZ(lLR[0], lLR[1] + thickness, 0.0);
         XYZ lCorner = new XYZ(lOrig[0] + thickness, lOrig[1] + thickness, 0.0);
         XYZ lULPlusThickness = new XYZ(lOrig[0] + thickness, lOrig[1] + depth, 0.0);
         XYZ lUL = new XYZ(lULPlusThickness[0] - thickness, lULPlusThickness[1], 0.0);

         // fillet modifications.
         double[] edgeRanges = new double[2];
         XYZ lLREdgeCtr = null, lULEdgeCtr = null;
         XYZ lLRStartFillet = null, lLREndFillet = null;
         XYZ lULStartFillet = null, lULEndFillet = null;

         if (filletedEdge)
         {
            lLREdgeCtr = new XYZ(lLRPlusThickness[0] - edgeRadius, lLRPlusThickness[1] - edgeRadius, 0.0);
            lULEdgeCtr = new XYZ(lULPlusThickness[0] - edgeRadius, lULPlusThickness[1] - edgeRadius, 0.0);

            lLRStartFillet = new XYZ(lLRPlusThickness[0], lLRPlusThickness[1] - edgeRadius, 0.0);
            lLREndFillet = new XYZ(lLRPlusThickness[0] - edgeRadius, lLRPlusThickness[1], 0.0);

            lULStartFillet = new XYZ(lULPlusThickness[0], lULPlusThickness[1] - edgeRadius, 0.0);
            lULEndFillet = new XYZ(lULPlusThickness[0] - edgeRadius, lULPlusThickness[1], 0.0);

            edgeRanges[0] = 0.0; edgeRanges[1] = Math.PI / 2.0;
         }

         XYZ lLRCorner = null, lULCorner = null, lFilletCtr = null;
         double[] filletRange = new double[2];
         if (filletedCorner)
         {
            lLRCorner = new XYZ(lCorner[0] + filletRadius, lCorner[1], lCorner[2]);
            lULCorner = new XYZ(lCorner[0], lCorner[1] + filletRadius, lCorner[2]);
            lFilletCtr = new XYZ(lCorner[0] + filletRadius, lCorner[1] + filletRadius, lCorner[2]);

            filletRange[0] = Math.PI; filletRange[1] = 3.0 * Math.PI / 2;
         }

         OuterCurve = new CurveLoop();
         IList<IIFCProfileSegment> segments = new List<IIFCProfileSegment>();

         // We will process the L shape profile is subsections, to try to fall back
         // on simpler representations as possible.
         segments.Add(new IFCProfileLineSegment(lOrig, lLR));
         if (!AppendSegmentsToCurveLoop(OuterCurve, segments))
         {
            Importer.TheLog.LogError(Id, "Couldn't process IfcLShapeProfileDef, ignoring.", true);
         }

         XYZ startCornerPoint = null, endCornerPoint = null;
         if (filletedEdge)
         {
            startCornerPoint = lLREndFillet;
            endCornerPoint = lULStartFillet;

            if (!fullFilletedEdge)
            {
               segments.Add(new IFCProfileLineSegment(lLR, lLRStartFillet));
            }

            segments.Add(new IFCProfileXYArcSegment(lLREdgeCtr, edgeRadius, edgeRanges[0], edgeRanges[1], false));
            if (!AppendSegmentsToCurveLoop(OuterCurve, segments))
            {
               Importer.TheLog.LogError(Id, "Couldn't process edge fillet for IfcLShapeProfileDef, removing fillet.", false);
               filletedEdge = false;
               fullFilletedEdge = false;
            }
         }

         // filletedEdge may have become false if above operation failed.
         if (!filletedEdge)
         {
            startCornerPoint = lLRPlusThickness;
            endCornerPoint = lULPlusThickness;

            segments.Add(new IFCProfileLineSegment(lLR, startCornerPoint));
            if (!AppendSegmentsToCurveLoop(OuterCurve, segments))
            {
               Importer.TheLog.LogError(Id, "Couldn't process IfcLShapeProfileDef, ignoring.", true);
            }
         }

         if (filletedCorner)
         {
            segments.Add(new IFCProfileLineSegment(startCornerPoint, lLRCorner));
            segments.Add(new IFCProfileXYArcSegment(lFilletCtr, filletRadius, filletRange[0], filletRange[1], true));
            segments.Add(new IFCProfileLineSegment(lULCorner, endCornerPoint));
            if (!AppendSegmentsToCurveLoop(OuterCurve, segments))
            {
               Importer.TheLog.LogError(Id, "Couldn't process corner fillet for IfcLShapeProfileDef, removing fillet.", false);
               filletedCorner = false;
            }
         }

         // filletedCorner may have become false if above operation failed.
         if (!filletedCorner)
         {
            segments.Add(new IFCProfileLineSegment(startCornerPoint, lCorner));
            segments.Add(new IFCProfileLineSegment(lCorner, endCornerPoint));
            if (!AppendSegmentsToCurveLoop(OuterCurve, segments))
            {
               Importer.TheLog.LogError(Id, "Couldn't process IfcLShapeProfileDef, ignoring.", true);
            }
         }

         if (filletedEdge)
         {
            segments.Add(new IFCProfileXYArcSegment(lULEdgeCtr, edgeRadius, edgeRanges[0], edgeRanges[1], false));
            if (!fullFilletedEdge)
            {
               segments.Add(new IFCProfileLineSegment(lULEndFillet, lUL));
            }

            if (!AppendSegmentsToCurveLoop(OuterCurve, segments))
            {
               Importer.TheLog.LogError(Id, "Couldn't process edge fillet for IfcLShapeProfileDef, removing fillet.", false);
               filletedEdge = false;
               // No need to set fullFilletedEdge to false, as it is not used after this point.
            }
         }

         if (!filletedEdge)
         {
            segments.Add(new IFCProfileLineSegment(endCornerPoint, lUL));
            // No need to AppendSegmentsToCurveLoop here; will be done below.
         }

         segments.Add(new IFCProfileLineSegment(lUL, lOrig));
         if (!AppendSegmentsToCurveLoop(OuterCurve, segments))
         {
            Importer.TheLog.LogError(Id, "Couldn't process IfcLShapeProfileDef, ignoring.", true);
         }
      }

      private void ProcessIFCIShapeProfileDef(IFCAnyHandle profileDef)
      {
         bool found = false;
         double width = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "OverallWidth", out found);
         if (!found)
            return;

         double depth = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "OverallDepth", out found);
         if (!found)
            return;

         double webThickness = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "WebThickness", out found);
         if (!found)
            return;

         double flangeThickness = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "FlangeThickness", out found);
         if (!found)
            return;

         double filletRadius = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "FilletRadius", 0.0);
         bool hasFillet = !MathUtil.IsAlmostZero(filletRadius);

         // take advantage of X/Y symmetries below.
         XYZ[] iShapePoints = new XYZ[12] {
                new XYZ(-width/2.0, -depth/2.0, 0.0),
                new XYZ(width/2.0, -depth/2.0, 0.0),
                new XYZ(width/2.0, -depth/2.0 + flangeThickness, 0.0),
                new XYZ(webThickness/2.0, -depth/2.0 + flangeThickness, 0.0),

                new XYZ(webThickness/2.0, -(-depth/2.0 + flangeThickness), 0.0),
                new XYZ(width/2.0, -(-depth/2.0 + flangeThickness), 0.0),
                new XYZ(width/2.0, depth/2.0, 0.0),
                new XYZ(-width/2.0, depth/2.0, 0.0),

                new XYZ(-width/2.0,  -(-depth/2.0 + flangeThickness), 0.0),
                new XYZ(-webThickness/2.0,  -(-depth/2.0 + flangeThickness), 0.0),
                new XYZ(-webThickness/2.0,  -depth/2.0 + flangeThickness, 0.0),
                new XYZ(-width/2.0,  -depth/2.0 + flangeThickness, 0.0)
            };

         if (hasFillet)
         {
            OuterCurve = new CurveLoop();
            XYZ[] iFilletPoints = new XYZ[8] {
                    new XYZ(iShapePoints[3][0] + filletRadius, iShapePoints[3][1], 0.0),
                    new XYZ(iShapePoints[3][0], iShapePoints[3][1] + filletRadius, 0.0),
                    new XYZ(iShapePoints[4][0], iShapePoints[4][1] - filletRadius, 0.0),
                    new XYZ(iShapePoints[4][0] + filletRadius, iShapePoints[4][1], 0.0),
                    new XYZ(iShapePoints[9][0] - filletRadius, iShapePoints[9][1], 0.0),
                    new XYZ(iShapePoints[9][0], iShapePoints[9][1] - filletRadius, 0.0),
                    new XYZ(iShapePoints[10][0], iShapePoints[10][1] + filletRadius, 0.0),
                    new XYZ(iShapePoints[10][0] - filletRadius, iShapePoints[10][1], 0.0)
                };

            XYZ[] iFilletCtr = new XYZ[4] {
                    new XYZ(iShapePoints[3][0] + filletRadius, iShapePoints[3][1] + filletRadius, 0.0),
                    new XYZ(iShapePoints[4][0] + filletRadius, iShapePoints[4][1] - filletRadius, 0.0),
                    new XYZ(iShapePoints[9][0] - filletRadius, iShapePoints[9][1] - filletRadius, 0.0),
                    new XYZ(iShapePoints[10][0] - filletRadius, iShapePoints[10][1] + filletRadius, 0.0)
                };

            // need to flip all fillets.
            double[][] filletRanges = new double[4][] 
            {
               new double[2] { Math.PI, 3.0*Math.PI/2 },
               new double[2] { Math.PI/2.0, Math.PI },
               new double[2] { 0, Math.PI/2.0 },
               new double[2] { 3.0*Math.PI/2, 2.0*Math.PI }
            };

            IList<IIFCProfileSegment> segments = new List<IIFCProfileSegment>();
            segments.Add(new IFCProfileLineSegment(iShapePoints[0], iShapePoints[1]));
            segments.Add(new IFCProfileLineSegment(iShapePoints[1], iShapePoints[2]));
            segments.Add(new IFCProfileLineSegment(iShapePoints[2], iFilletPoints[0]));
            segments.Add(new IFCProfileXYArcSegment(iFilletCtr[0], filletRadius, filletRanges[0][0], filletRanges[0][1], true));
            segments.Add(new IFCProfileLineSegment(iFilletPoints[1], iFilletPoints[2]));
            segments.Add(new IFCProfileXYArcSegment(iFilletCtr[1], filletRadius, filletRanges[1][0], filletRanges[1][1], true));
            segments.Add(new IFCProfileLineSegment(iFilletPoints[3], iShapePoints[5]));
            segments.Add(new IFCProfileLineSegment(iShapePoints[5], iShapePoints[6]));
            segments.Add(new IFCProfileLineSegment(iShapePoints[6], iShapePoints[7]));
            segments.Add(new IFCProfileLineSegment(iShapePoints[7], iShapePoints[8]));
            segments.Add(new IFCProfileLineSegment(iShapePoints[8], iFilletPoints[4]));
            segments.Add(new IFCProfileXYArcSegment(iFilletCtr[2], filletRadius, filletRanges[2][0], filletRanges[2][1], true));
            segments.Add(new IFCProfileLineSegment(iFilletPoints[5], iFilletPoints[6]));
            segments.Add(new IFCProfileXYArcSegment(iFilletCtr[3], filletRadius, filletRanges[3][0], filletRanges[3][1], true));
            segments.Add(new IFCProfileLineSegment(iFilletPoints[7], iShapePoints[11]));
            segments.Add(new IFCProfileLineSegment(iShapePoints[11], iShapePoints[0]));

            if (!AppendSegmentsToCurveLoop(OuterCurve, segments))
            {
               Importer.TheLog.LogError(Id, "Couldn't process filleted IfcIShapeProfileDef, removing fillet.", false);
               hasFillet = false;
            }
         }

         if (!hasFillet)
         {
            OuterCurve = CreateProfilePolyCurveLoop(iShapePoints);
         }

         if (OuterCurve == null)
            Importer.TheLog.LogError(Id, "Couldn't process IfcIShapeProfileDef, ignoring.", true);
      }

      private void ProcessIFCTShapeProfileDef(IFCAnyHandle profileDef)
      {
         bool found = false;
         double flangeWidth = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "FlangeWidth", out found);
         if (!found)
            return;

         double depth = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "Depth", out found);
         if (!found)
            return;

         double webThickness = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "WebThickness", out found);
         if (!found)
            return;

         double flangeThickness = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "FlangeThickness", out found);
         if (!found)
            return;

         double centerOptY = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "CentreOfGravityInY", 0.0);

         double filletRadius = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "FilletRadius", 0.0);
         bool hasFillet = !MathUtil.IsAlmostZero(filletRadius);

         double flangeEdgeRadius = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "FlangeEdgeRadius", 0.0);
         bool hasFlangeEdge = !MathUtil.IsAlmostZero(flangeEdgeRadius);

         double webEdgeRadius = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "WebEdgeRadius", 0.0);
         bool hasWebEdge = !MathUtil.IsAlmostZero(webEdgeRadius);

         double webSlope = IFCImportHandleUtil.GetOptionalScaledAngleAttribute(profileDef, "WebSlope", 0.0);
         double webDeltaX = (depth / 2.0) * Math.Sin(webSlope);
         XYZ webDir = new XYZ(-Math.Sin(webSlope), Math.Cos(webSlope), 0.0);

         double flangeSlope = IFCImportHandleUtil.GetOptionalScaledAngleAttribute(profileDef, "FlangeSlope", 0.0);
         double flangeDeltaY = (flangeWidth / 4.0) * Math.Sin(flangeSlope);
         XYZ flangeDir = new XYZ(Math.Cos(flangeSlope), -Math.Sin(flangeSlope), 0.0);

         XYZ[] tShapePoints = new XYZ[8] {
                new XYZ(-flangeWidth/2.0, depth /2.0 + centerOptY, 0.0),
                new XYZ(-flangeWidth/2.0, depth/2.0 + centerOptY - (flangeThickness-flangeDeltaY), 0.0),
                new XYZ(0.0, 0.0, 0.0),   // calc below
                new XYZ(-webThickness/2.0 + webDeltaX, -depth/2.0 + centerOptY, 0.0),
                new XYZ(-(-webThickness/2.0 + webDeltaX), -depth/2.0 + centerOptY, 0.0),
                new XYZ(0.0, 0.0, 0.0),   // calc below
                new XYZ(flangeWidth/2.0, depth/2.0 + centerOptY - (flangeThickness-flangeDeltaY), 0.0),
                new XYZ(flangeWidth/2.0, depth/2.0 + centerOptY, 0.0)
            };

         Line line1 = Line.CreateUnbound(tShapePoints[1], flangeDir);
         Line line2 = Line.CreateUnbound(tShapePoints[3], webDir);

         IntersectionResultArray intersectResultArray;
         SetComparisonResult intersectResultComp = line1.Intersect(line2, out intersectResultArray);
         if ((intersectResultComp != SetComparisonResult.Overlap) || (intersectResultArray.Size != 1))
         {
            Importer.TheLog.LogError(Id, "Couldn't calculate profile point in IfcTShapeProfileDef.", true);
         }
       
         tShapePoints[2] = intersectResultArray.get_Item(0).XYZPoint;
         tShapePoints[5] = new XYZ(-tShapePoints[2][0], tShapePoints[2][1], tShapePoints[2][2]);

         // TODO: support fillets!
         if (hasFillet)
         {
            Importer.TheLog.LogWarning(Id, "Fillets not yet supported for IfcTShapeProfileDef, ignoring.", false);
         }
         OuterCurve = CreateProfilePolyCurveLoop(tShapePoints);

         if (OuterCurve == null)
            Importer.TheLog.LogError(Id, "Couldn't process IfcTShapeProfileDef, ignoring.", true);
      }

      private void ProcessIFCUShapeProfileDef(IFCAnyHandle profileDef)
      {
         bool found = false;
         double flangeWidth = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "FlangeWidth", out found);
         if (!found)
            return;

         double depth = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "Depth", out found);
         if (!found)
            return;

         double webThickness = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "WebThickness", out found);
         if (!found)
            return;

         double flangeThickness = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "FlangeThickness", out found);
         if (!found)
            return;

         double centerOptX = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "CentreOfGravityInX", 0.0);

         double filletRadius = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "FilletRadius", 0.0);
         bool hasFillet = !MathUtil.IsAlmostZero(filletRadius);

         double edgeRadius = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "EdgeRadius", 0.0);
         bool hasEdgeRadius = !MathUtil.IsAlmostZero(edgeRadius);

         double flangeSlope = IFCImportHandleUtil.GetOptionalScaledAngleAttribute(profileDef, "FlangeSlope", 0.0);
         double flangeDirY = Math.Sin(flangeSlope);

         // start lower left, CCW.
         XYZ[] uShapePoints = new XYZ[8] {
                new XYZ(-flangeWidth/2.0+centerOptX, -depth/2.0, 0.0),
                new XYZ(flangeWidth/2.0+centerOptX, -depth/2.0, 0.0),
                new XYZ(flangeWidth/2.0+centerOptX, -depth/2.0 + (flangeThickness-flangeDirY*(flangeWidth/2.0)), 0.0),
                new XYZ(-flangeWidth/2.0+centerOptX+webThickness, -depth/2.0 + (flangeThickness+flangeDirY*(flangeWidth/2.0-webThickness)), 0.0),
                new XYZ(-flangeWidth/2.0+centerOptX+webThickness, -(-depth/2.0 + (flangeThickness+flangeDirY*(flangeWidth/2.0-webThickness))), 0.0),
                new XYZ(flangeWidth/2.0+centerOptX, -(-depth/2.0 + (flangeThickness-flangeDirY*(flangeWidth/2.0))), 0.0),
                new XYZ(flangeWidth/2.0+centerOptX, depth/2.0, 0.0),
                new XYZ(-flangeWidth/2.0+centerOptX, depth/2.0, 0.0),
            };

         // TODO: support fillets!
         if (hasFillet)
         {
            Importer.TheLog.LogWarning(Id, "Fillets not yet supported for IfcUShapeProfileDef, ignoring.", false);
         }
         OuterCurve = CreateProfilePolyCurveLoop(uShapePoints);

         if (OuterCurve == null)
            Importer.TheLog.LogError(Id, "Couldn't process IfcUShapeProfileDef, ignoring.", true);
      }

      private void ProcessIFCZShapeProfileDef(IFCAnyHandle profileDef)
      {
         bool found = false;
         double flangeWidth = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "FlangeWidth", out found);
         if (!found)
            return;

         double depth = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "Depth", out found);
         if (!found)
            return;

         double webThickness = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "WebThickness", out found);
         if (!found)
            return;

         double flangeThickness = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(profileDef, "FlangeThickness", out found);
         if (!found)
            return;

         double filletRadius = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "FilletRadius", 0.0);
         bool hasFillet = !MathUtil.IsAlmostZero(filletRadius);

         double edgeRadius = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(profileDef, "EdgeRadius", 0.0);
         bool hasEdgeRadius = !MathUtil.IsAlmostZero(edgeRadius);

         XYZ[] zShapePoints = new XYZ[8] {
                new XYZ(-webThickness/2.0, -depth/2.0, 0.0),
                new XYZ(flangeWidth - webThickness/2.0, -depth/2.0, 0.0),
                new XYZ(flangeWidth - webThickness/2.0, flangeThickness - depth/2.0, 0.0),
                new XYZ(webThickness/2.0, flangeThickness - depth/2.0, 0.0),
                new XYZ(webThickness/2.0, depth/2.0, 0.0),
                new XYZ(webThickness/2.0 - flangeWidth, depth/2.0, 0.0),
                new XYZ(webThickness/2.0 - flangeWidth, depth/2.0 - flangeThickness, 0.0),
                new XYZ(-webThickness/2.0, depth/2.0 - flangeThickness, 0.0)
            };

         // need to flip fillet arcs.
         XYZ[] zFilletPoints = new XYZ[4] {
                new XYZ(zShapePoints[3][0] + filletRadius, zShapePoints[3][1], 0.0),
                new XYZ(zShapePoints[3][0], zShapePoints[3][1] + filletRadius, 0.0),
                new XYZ(zShapePoints[7][0] - filletRadius, zShapePoints[7][1], 0.0),
                new XYZ(zShapePoints[7][0], zShapePoints[7][1] - filletRadius, 0.0)
            };

         XYZ[] zFilletCenters = new XYZ[2] {
                new XYZ(zShapePoints[3][0] + filletRadius, zShapePoints[3][1] + filletRadius, 0.0),
                new XYZ(zShapePoints[7][0] - filletRadius, zShapePoints[7][1] - filletRadius, 0.0),
            };

         double[][] filletRange = new double[2][] {
                new double[2] { Math.PI, 3*Math.PI/2.0 },
                new double[2] { 0.0, Math.PI/2.0 }
            };

         // do not flip edge arcs.
         XYZ[] zEdgePoints = new XYZ[4] {
                new XYZ(zShapePoints[2][0], zShapePoints[2][1] - edgeRadius, 0.0),
                new XYZ(zShapePoints[2][0] - edgeRadius, zShapePoints[2][1], 0.0),
                new XYZ(zShapePoints[6][0], zShapePoints[6][1] + edgeRadius, 0.0),
                new XYZ(zShapePoints[6][0] + edgeRadius, zShapePoints[6][1], 0.0)
            };

         XYZ[] zEdgeCenters = new XYZ[2] {
                new XYZ(zShapePoints[2][0] - edgeRadius, zShapePoints[2][1] - edgeRadius, 0.0),
                new XYZ(zShapePoints[6][0] + edgeRadius, zShapePoints[6][1] + edgeRadius, 0.0)
            };

         double[][] edgeRange = new double[2][] {
                new double[2] { 0.0, Math.PI/2.0 },
                new double[2] { Math.PI, 3*Math.PI/2.0 }
            };

         OuterCurve = new CurveLoop();
         IList<IIFCProfileSegment> segments = new List<IIFCProfileSegment>();
         // We will process the Z shape profile is subsections, to try to fall back
         // on simpler representations as possible.

         segments.Add(new IFCProfileLineSegment(zShapePoints[0], zShapePoints[1]));

         XYZ zNextStart = null;
         if (hasEdgeRadius)
         {
            segments.Add(new IFCProfileLineSegment(zShapePoints[1], zEdgePoints[0]));
            segments.Add(new IFCProfileXYArcSegment(zEdgeCenters[0], edgeRadius, edgeRange[0][0], edgeRange[0][1], false));
            if (!AppendSegmentsToCurveLoop(OuterCurve, segments))
            {
               Importer.TheLog.LogError(Id, "Couldn't process edge radius for IfcZShapeProfileDef, removing fillet.", false);
               hasEdgeRadius = false;
            }
            else
            {
               zNextStart = zEdgePoints[1];
            }
         }


         if (!hasEdgeRadius)
         {
            segments.Add(new IFCProfileLineSegment(zShapePoints[1], zShapePoints[2]));
            zNextStart = zShapePoints[2];
            if (!AppendSegmentsToCurveLoop(OuterCurve, segments))
            {
               Importer.TheLog.LogError(Id, "Couldn't process IfcZShapeProfileDef, ignoring.", true);
            }
         }

         if (hasFillet)
         {
            segments.Add(new IFCProfileLineSegment(zNextStart, zFilletPoints[0]));
            segments.Add(new IFCProfileXYArcSegment(zFilletCenters[0], filletRadius, filletRange[0][0], filletRange[0][1], true));
            if (!AppendSegmentsToCurveLoop(OuterCurve, segments))
            {
               Importer.TheLog.LogError(Id, "Couldn't process fillet for IfcZShapeProfileDef, removing fillet.", false);
               hasFillet = false;
            }
            else
            {
               zNextStart = zFilletPoints[1];
            }
         }
         
         if (!hasFillet)
         {
            segments.Add(new IFCProfileLineSegment(zNextStart, zShapePoints[3]));
            zNextStart = zShapePoints[3];
            // No need to call AppendSegmentsToCurveLoop, will be handled below.
         }

         segments.Add(new IFCProfileLineSegment(zNextStart, zShapePoints[4]));
         segments.Add(new IFCProfileLineSegment(zShapePoints[4], zShapePoints[5]));
         if (!AppendSegmentsToCurveLoop(OuterCurve, segments))
         {
            Importer.TheLog.LogError(Id, "Couldn't process IfcZShapeProfileDef, ignoring.", true);
         }

         if (hasEdgeRadius)
         {
            segments.Add(new IFCProfileLineSegment(zShapePoints[5], zEdgePoints[2])); 
            segments.Add(new IFCProfileXYArcSegment(zEdgeCenters[1], edgeRadius, edgeRange[1][0], edgeRange[1][1], false));
            if (!AppendSegmentsToCurveLoop(OuterCurve, segments))
            {
               Importer.TheLog.LogError(Id, "Couldn't process edge radius for IfcZShapeProfileDef, removing fillet.", false);
               hasEdgeRadius = false;
            }
            else
            {
               zNextStart = zEdgePoints[3];
            }
         }

         if (!hasEdgeRadius)
         {
            segments.Add(new IFCProfileLineSegment(zShapePoints[5], zShapePoints[6]));
            zNextStart = zShapePoints[6];
            if (!AppendSegmentsToCurveLoop(OuterCurve, segments))
            {
               Importer.TheLog.LogError(Id, "Couldn't process IfcZShapeProfileDef, ignoring.", true);
            }
         }

         if (hasFillet)
         {
            segments.Add(new IFCProfileLineSegment(zNextStart, zFilletPoints[2]));
            segments.Add(new IFCProfileXYArcSegment(zFilletCenters[1], filletRadius, filletRange[1][0], filletRange[1][1], true));
            if (!AppendSegmentsToCurveLoop(OuterCurve, segments))
            {
               Importer.TheLog.LogError(Id, "Couldn't process fillet for IfcZShapeProfileDef, removing fillet.", false);
               hasFillet = false;
            }
            else
            {
               zNextStart = zFilletPoints[3];
            }
         }
         
         if (!hasFillet)
         {
            segments.Add(new IFCProfileLineSegment(zNextStart, zShapePoints[7]));
            zNextStart = zShapePoints[7];
            // No need to call AppendSegmentsToCurveLoop, will be handled below.
         }

         segments.Add(new IFCProfileLineSegment(zNextStart, zShapePoints[0]));
         if (!AppendSegmentsToCurveLoop(OuterCurve, segments))
         {
            Importer.TheLog.LogError(Id, "Couldn't process IfcZShapeProfileDef, ignoring.", true);
         }
      }

      protected override void Process(IFCAnyHandle profileDef)
      {
         base.Process(profileDef);

         IFCAnyHandle positionHnd = IFCImportHandleUtil.GetRequiredInstanceAttribute(profileDef, "Position", false);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(positionHnd))
            Position = IFCLocation.ProcessIFCAxis2Placement(positionHnd);
         else
         {
            Importer.TheLog.LogWarning(profileDef.StepId, "\"Position\" attribute not specified in IfcParameterizedProfileDef, using origin.", false);
            Position = Transform.Identity;
         }

         if (IFCAnyHandleUtil.IsValidSubTypeOf(profileDef, IFCEntityType.IfcRectangleProfileDef))
            ProcessIFCRectangleProfileDef(profileDef);
         else if (IFCAnyHandleUtil.IsValidSubTypeOf(profileDef, IFCEntityType.IfcCircleProfileDef))
            ProcessIFCCircleProfileDef(profileDef);
         else if (IFCAnyHandleUtil.IsValidSubTypeOf(profileDef, IFCEntityType.IfcEllipseProfileDef))
            ProcessIFCEllipseProfileDef(profileDef);
         else if (IFCAnyHandleUtil.IsValidSubTypeOf(profileDef, IFCEntityType.IfcCShapeProfileDef))
            ProcessIFCCShapeProfileDef(profileDef);
         else if (IFCAnyHandleUtil.IsValidSubTypeOf(profileDef, IFCEntityType.IfcLShapeProfileDef))
            ProcessIFCLShapeProfileDef(profileDef);
         else if (IFCAnyHandleUtil.IsValidSubTypeOf(profileDef, IFCEntityType.IfcIShapeProfileDef))
            ProcessIFCIShapeProfileDef(profileDef);
         else if (IFCAnyHandleUtil.IsValidSubTypeOf(profileDef, IFCEntityType.IfcTShapeProfileDef))
            ProcessIFCTShapeProfileDef(profileDef);
         else if (IFCAnyHandleUtil.IsValidSubTypeOf(profileDef, IFCEntityType.IfcUShapeProfileDef))
            ProcessIFCUShapeProfileDef(profileDef);
         else if (IFCAnyHandleUtil.IsValidSubTypeOf(profileDef, IFCEntityType.IfcZShapeProfileDef))
            ProcessIFCZShapeProfileDef(profileDef);
         else
         {
            //LOG: ERROR: IfcParameterizedProfileDef of subtype {subtype} not supported.
         }
      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCParameterizedProfile()
      {

      }

      protected IFCParameterizedProfile(IFCAnyHandle profileDef)
      {
         Process(profileDef);
      }

      public static IFCParameterizedProfile ProcessIFCParameterizedProfile(IFCAnyHandle ifcProfileDef)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcProfileDef))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcProfileDef);
            return null;
         }

         IFCEntity profileDef;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcProfileDef.StepId, out profileDef))
            return (profileDef as IFCParameterizedProfile);

         return new IFCParameterizedProfile(ifcProfileDef);
      }

   }

   /// <summary>
   /// Provides methods to process IfcProfileDef and its subclasses.
   /// </summary>
   public class IFCSimpleProfile : IFCProfileDef
   {
      private CurveLoop m_OuterCurve = null;

      private IList<CurveLoop> m_InnerCurves = null;

      // This is only valid for IFCParameterizedProfile.  We place it here to be at the same level as the CurveLoops,
      // so that they can be transformed in a consisent matter.
      private Transform m_Position = null;

      /// <summary>
      /// The location (origin and rotation) of the parametric profile.
      /// </summary>
      public Transform Position
      {
         get { return m_Position; }
         protected set { m_Position = value; }
      }

      private void ProcessIFCArbitraryOpenProfileDef(IFCAnyHandle profileDef)
      {
         IFCAnyHandle curveHnd = IFCAnyHandleUtil.GetInstanceAttribute(profileDef, "Curve");
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(curveHnd))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcArbitraryOpenProfileDef);
            return;
         }

         IFCCurve profileIFCCurve = IFCCurve.ProcessIFCCurve(curveHnd);
         CurveLoop profileCurveLoop = profileIFCCurve.GetTheCurveLoop();
         if (profileCurveLoop == null)
         {
            Curve profileCurve = profileIFCCurve.Curve;
            if (profileCurve != null)
            {
               profileCurveLoop = new CurveLoop();
               profileCurveLoop.Append(profileCurve);
            }
         }


         if ((profileCurveLoop != null) && IFCAnyHandleUtil.IsValidSubTypeOf(profileDef, IFCEntityType.IfcCenterLineProfileDef))
         {
            double? thickness = IFCAnyHandleUtil.GetDoubleAttribute(profileDef, "Thickness");
            if (!thickness.HasValue)
            {
               //LOG: ERROR: IfcCenterLineProfileDef has no thickness defined.
               return;
            }

            Plane plane = null;
            try
            {
               plane = profileCurveLoop.GetPlane();
            }
            catch
            {
               //LOG: ERROR: Curve for IfcCenterLineProfileDef is non-planar.
               return;
            }

            double thicknessVal = IFCUnitUtil.ScaleLength(thickness.Value);
            profileCurveLoop = null;
            try
            {
               profileCurveLoop = CurveLoop.CreateViaThicken(profileCurveLoop, thicknessVal, plane.Normal);
            }
            catch
            {
            }
         }

         if (profileCurveLoop != null)
            OuterCurve = profileCurveLoop;
         else
         {
            //LOG: ERROR: Invalid outer curve in IfcArbitraryOpenProfileDef.
            return;
         }
      }

      // In certain cases, Revit can't handle unbounded circles and ellipses.  Create a CurveLoop with the curve split into two segments.
      private CurveLoop CreateCurveLoopFromUnboundedCyclicCurve(Curve innerCurve)
      {
         if (innerCurve == null)
            return null;

         if (!innerCurve.IsCyclic)
            return null;

         // Note that we don't disallow bound curves, as they could be bound but still closed.

         // We don't know how to handle anything other than circles or ellipses with a period of 2PI.
         double period = innerCurve.Period;
         if (!MathUtil.IsAlmostEqual(period, Math.PI * 2.0))
            return null;

         double startParam = innerCurve.IsBound ? innerCurve.GetEndParameter(0) : 0.0;
         double endParam = innerCurve.IsBound ? innerCurve.GetEndParameter(1) : period;

         // Not a closed curve.
         if (!MathUtil.IsAlmostEqual(endParam - startParam, period))
            return null;

         Curve firstCurve = innerCurve.Clone();
         if (firstCurve == null)
            return null;

         Curve secondCurve = innerCurve.Clone();
         if (secondCurve == null)
            return null;

         firstCurve.MakeBound(0, period / 2.0);
         secondCurve.MakeBound(period / 2.0, period);

         CurveLoop innerCurveLoop = new CurveLoop();
         innerCurveLoop.Append(firstCurve);
         innerCurveLoop.Append(secondCurve);
         return innerCurveLoop;
      }

      private void ProcessIFCArbitraryClosedProfileDef(IFCAnyHandle profileDef)
      {
         IFCAnyHandle curveHnd = IFCImportHandleUtil.GetRequiredInstanceAttribute(profileDef, "OuterCurve", false);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(curveHnd))
            return;

         IFCCurve outerIFCCurve = IFCCurve.ProcessIFCCurve(curveHnd);
         CurveLoop outerCurveLoop = outerIFCCurve.GetTheCurveLoop();

         // We need to convert outerIFCCurve into a CurveLoop with bound curves.  This is handled below (with possible errors logged).
         if (outerCurveLoop != null)
            OuterCurve = outerCurveLoop;
         else
         {
            Curve outerCurve = outerIFCCurve.Curve;
            if (outerCurve == null)
               Importer.TheLog.LogError(profileDef.StepId, "Couldn't convert outer curve #" + curveHnd.StepId + " in IfcArbitraryClosedProfileDef.", true);
            else
            {
               OuterCurve = CreateCurveLoopFromUnboundedCyclicCurve(outerCurve);
               if (OuterCurve == null)
               {
                  if (outerCurve.IsBound)
                     Importer.TheLog.LogError(profileDef.StepId, "Outer curve #" + curveHnd.StepId + " in IfcArbitraryClosedProfileDef isn't closed and can't be used.", true);
                  else
                     Importer.TheLog.LogError(profileDef.StepId, "Couldn't split unbound outer curve #" + curveHnd.StepId + " in IfcArbitraryClosedProfileDef.", true);
               }
            }
         }

         if (IFCAnyHandleUtil.IsSubTypeOf(profileDef, IFCEntityType.IfcArbitraryProfileDefWithVoids))
         {
            IList<IFCAnyHandle> innerCurveHnds = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(profileDef, "InnerCurves");
            if (innerCurveHnds == null || innerCurveHnds.Count == 0)
            {
               Importer.TheLog.LogWarning(profileDef.StepId, "IfcArbitraryProfileDefWithVoids has no voids.", false);
               return;
            }

            ISet<IFCAnyHandle> usedHandles = new HashSet<IFCAnyHandle>();
            foreach (IFCAnyHandle innerCurveHnd in innerCurveHnds)
            {
               if (IFCAnyHandleUtil.IsNullOrHasNoValue(innerCurveHnd))
               {
                  Importer.TheLog.LogWarning(profileDef.StepId, "Null or invalid inner curve handle in IfcArbitraryProfileDefWithVoids.", false);
                  continue;
               }

               if (usedHandles.Contains(innerCurveHnd))
               {
                  Importer.TheLog.LogWarning(profileDef.StepId, "Duplicate void #" + innerCurveHnd.StepId + " in IfcArbitraryProfileDefWithVoids, ignoring.", false);
                  continue;
               }

               // If any inner is the same as the outer, throw an exception.
               if (curveHnd.Equals(innerCurveHnd))
               {
                  Importer.TheLog.LogError(profileDef.StepId, "Inner curve loop #" + innerCurveHnd.StepId + " same as outer curve loop in IfcArbitraryProfileDefWithVoids.", true);
                  continue;
               }

               usedHandles.Add(innerCurveHnd);

               IFCCurve innerIFCCurve = IFCCurve.ProcessIFCCurve(innerCurveHnd);
               CurveLoop innerCurveLoop = innerIFCCurve.GetTheCurveLoop();

               // See if we have a closed curve instead.
               if (innerCurveLoop == null)
                  innerCurveLoop = CreateCurveLoopFromUnboundedCyclicCurve(innerIFCCurve.Curve);

               if (innerCurveLoop == null)
               {
                  //LOG: WARNING: Null or invalid inner curve in IfcArbitraryProfileDefWithVoids.
                  Importer.TheLog.LogWarning(profileDef.StepId, "Invalid inner curve #" + innerCurveHnd.StepId + " in IfcArbitraryProfileDefWithVoids.", false);
                  continue;
               }

               InnerCurves.Add(innerCurveLoop);
            }
         }
      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCSimpleProfile()
      {

      }

      protected override void Process(IFCAnyHandle profileDef)
      {
         base.Process(profileDef);

         if (IFCAnyHandleUtil.IsSubTypeOf(profileDef, IFCEntityType.IfcArbitraryOpenProfileDef))
            ProcessIFCArbitraryOpenProfileDef(profileDef);
         else if (IFCAnyHandleUtil.IsSubTypeOf(profileDef, IFCEntityType.IfcArbitraryClosedProfileDef))
            ProcessIFCArbitraryClosedProfileDef(profileDef);
      }

      protected IFCSimpleProfile(IFCAnyHandle profileDef)
      {
         Process(profileDef);
      }

      /// <summary>
      /// Process an IFCAnyHandle corresponding to a simple profile.
      /// </summary>
      /// <param name="ifcProfileDef"></param>
      /// <returns>IFCSimpleProfile object.</returns>
      public static IFCSimpleProfile ProcessIFCSimpleProfile(IFCAnyHandle ifcProfileDef)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcProfileDef))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcProfileDef);
            return null;
         }

         IFCEntity profileDef;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcProfileDef.StepId, out profileDef))
            return (profileDef as IFCSimpleProfile);

         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcProfileDef, IFCEntityType.IfcArbitraryOpenProfileDef) ||
             (IFCAnyHandleUtil.IsValidSubTypeOf(ifcProfileDef, IFCEntityType.IfcArbitraryClosedProfileDef)))
            return new IFCSimpleProfile(ifcProfileDef);

         // IFC2x files don't have IfcParameterizedProfileDef, so we won't check the type.  If profileDef is the wrong entity type, it will fail in
         // ProcessIFCParameterizedProfileDef.
         return IFCParameterizedProfile.ProcessIFCParameterizedProfile(ifcProfileDef);
      }

      /// <summary>
      /// Get the outer curve loop.
      /// </summary>
      public CurveLoop OuterCurve
      {
         get { return m_OuterCurve; }
         protected set { m_OuterCurve = value; }
      }

      /// <summary>
      /// Get the list of inner curve loops.
      /// </summary>
      public IList<CurveLoop> InnerCurves
      {
         get
         {
            if (m_InnerCurves == null)
               m_InnerCurves = new List<CurveLoop>();
            return m_InnerCurves;
         }
      }
   }
}