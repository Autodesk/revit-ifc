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
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public abstract class IFCCurve : IFCRepresentationItem
   {
      // Some IfcCurves may use a non-unit length IfcVector to influence
      // the parametrization of the underlying curve.
      public double ParametericScaling { get; protected set; } = 1.0;

      /// <summary>
      /// If the curve can't get represented by Revit Curve(s) or CurveLoops(s), 
      /// the start point of the curve.
      /// </summary>
      /// <remarks>
      /// This will only be set if we fail at creating Revit geometry for the curve.
      /// This is intended as a backup where if we have multiple invalid curve 
      /// segments, we can potentially combine them into one valid curve segment.
      /// In practice, this is intended to be set when calling routines try to 
      /// bound the curve.
      /// </remarks>
      public XYZ BackupCurveStartLocation { get; set; } = null;

      /// <summary>
      /// If the curve can't get represented by Revit Curve(s) or CurveLoops(s), 
      /// the end point of the curve.
      /// </summary>
      /// <remarks>
      /// This will only be set if we fail at creating Revit geometry for the curve.
      /// This is intended as a backup where if we have multiple invalid curve 
      /// segments, we can potentially combine them into one valid curve segment.
      /// In practice, this is intended to be set when calling routines try to 
      /// bound the curve.
      /// </remarks>
      public XYZ BackupCurveEndLocation { get; set; } = null;

      /// <summary>
      /// Get the representation of IFCCurve as a single Revit curve, if it can be 
      /// representated as such.  It could be null.
      /// </summary>
      public Curve Curve { get; private set; } = null;

      /// <summary>
      /// Get the representation of IFCCurve as a list of Revit CurveLoops, 
      /// if it can be representated as such.  It could be null.
      /// </summary>
      public IList<CurveLoop> CurveLoops { get; private set; } = null;
      
      /// <summary>
      /// Get the representation of IFCCurve, as a list of 0 or more Revit curves.
      /// </summary>
      public IList<Curve> GetCurves()
      {
         IList<Curve> curves = new List<Curve>();

         if (Curve != null)
            curves.Add(Curve);
         else if (CurveLoops != null)
         {
            foreach (CurveLoop curveLoop in CurveLoops)
            {
               foreach (Curve curve in curveLoop)
                  curves.Add(curve);
            }
         }

         return curves;
      }

      /// <summary>
      /// Return true if this IFCCurve has no Revit geometric data associated with it.
      /// </summary>
      /// <returns></returns>
      public bool IsEmpty()
      {
         if (BackupCurveStartLocation != null && BackupCurveEndLocation != null)
            return false;

         if (Curve != null)
            return false;

         if (CurveLoops == null || CurveLoops.Count == 0)
            return true;

         foreach (CurveLoop curveLoop in CurveLoops)
         {
            if (curveLoop.NumberOfCurves() != 0)
               return false;
         }

         return true;
      }

      /// <summary>
      /// Get the one and only CurveLoop if it exists and is non-empty, otherwise null.
      /// </summary>
      /// <returns>The one and only CurveLoop if it exists and is non-empty.</returns>
      public CurveLoop GetTheCurveLoop()
      {
         if ((CurveLoops?.Count ?? 0) != 1)
            return null;

         CurveLoop theCurveLoop = CurveLoops[0];
         return ((theCurveLoop?.NumberOfCurves() ?? 0) > 0) ? theCurveLoop : null;
      }

      /// <summary>
      /// Set the representation of the curve based on one Curve.
      /// </summary>
      /// <param name="curve">The one curve.</param>
      /// <remarks>This will set CurveLoop to null.</remarks>
      public void SetCurve(Curve curve)
      {
         Curve = curve;
         CurveLoops = null;
      }
      
      /// <summary>
      /// Set the representation of the curve based on one CurveLoop.
      /// </summary>
      /// <param name="curveLoop">The one CurveLoop.</param>
      public void SetCurveLoop(CurveLoop curveLoop)
      {
         Curve = null;
         CurveLoops = null;

         if (curveLoop == null)
            return;

         if (curveLoop.NumberOfCurves() == 1)
         {
            Curve = curveLoop.GetCurveLoopIterator().Current;
         }

         CurveLoops = new List<CurveLoop>();
         CurveLoops.Add(curveLoop);
      }

      /// <summary>
      /// Set the representation of the curve based on one CurveLoop.
      /// </summary>
      /// <param name="curveLoop">The one CurveLoop.</param>
      /// <param name="pointXYZs">The point list that created this CurveLoop.</param>
      /// <remarks>The point list is used to potentially collapse a series of
      /// line segments into one.</remarks>
      public void SetCurveLoop(CurveLoop curveLoop, IList<XYZ> pointXYZs)
      {
         SetCurveLoop(curveLoop);
         if (Curve == null)
            Curve = IFCGeometryUtil.CreateCurveFromPolyCurveLoop(GetTheCurveLoop(), pointXYZs);
      }
      
      /// <summary>
      /// Set the representation of the curve based on a list of curves.
      /// </summary>
      /// <param name="curves">The initial list of curves.</param>
      public void SetCurveLoops(IList<Curve> curves)
      {
         Curve = null;
         CurveLoops = null;

         if (curves == null || curves.Count == 0)
            return;

         CurveLoops = new List<CurveLoop>();
         CurveLoop currCurveLoop = new CurveLoop();
         CurveLoops.Add(currCurveLoop);
         foreach (Curve curve in curves)
         {
            if (curve == null)
               continue;

            try
            {
               currCurveLoop.Append(curve);
               continue;
            }
            catch
            {
            }

            currCurveLoop = new CurveLoop();
            CurveLoops.Add(currCurveLoop);
            currCurveLoop.Append(curve);
         }

         if (curves.Count == 1)
            Curve = curves[0];
         else
            Curve = ConvertCurveLoopIntoSingleCurve();
      }

      /// <summary>
      /// Create a curve representation of this IFCCompositeCurve from a curveloop
      /// </summary>
      /// <returns>A Revit curve that is made by appending every curve in the given curveloop, if possible</returns>
      public Curve ConvertCurveLoopIntoSingleCurve()
      {
         CurveLoop curveLoop = GetTheCurveLoop();
         if (curveLoop == null)
         {
            return null;
         }

         CurveLoopIterator curveIterator = curveLoop.GetCurveLoopIterator();
         Curve firstCurve = curveIterator.Current;
         Curve returnCurve = null;

         double vertexEps = IFCImportFile.TheFile.VertexTolerance;

         // We only connect the curves if they are Line, Arc or Ellipse
         if (!((firstCurve is Line) || (firstCurve is Arc) || (firstCurve is Ellipse)))
         {
            return null;
         }

         XYZ firstStartPoint = firstCurve.GetEndPoint(0);

         Curve currentCurve = null;
         if (firstCurve is Line)
         {
            Line firstLine = firstCurve as Line;
            while (curveIterator.MoveNext())
            {
               currentCurve = curveIterator.Current;
               if (!(currentCurve is Line))
               {
                  return null;
               }
               Line currentLine = currentCurve as Line;

               if (!(firstLine.Direction.IsAlmostEqualTo(currentLine.Direction)))
               {
                  return null;
               }
            }
            returnCurve = Line.CreateBound(firstStartPoint, currentCurve.GetEndPoint(1));
         }
         else if (firstCurve is Arc)
         {
            Arc firstArc = firstCurve as Arc;
            XYZ firstCurveNormal = firstArc.Normal;

            while (curveIterator.MoveNext())
            {
               currentCurve = curveIterator.Current;
               if (!(currentCurve is Arc))
               {
                  return null;
               }

               XYZ currentStartPoint = currentCurve.GetEndPoint(0);
               XYZ currentEndPoint = currentCurve.GetEndPoint(1);

               Arc currentArc = currentCurve as Arc;
               XYZ currentCenter = currentArc.Center;
               double currentRadius = currentArc.Radius;
               XYZ currentNormal = currentArc.Normal;

               // We check if this circle is similar to the first circle by checking that they have the same center, same radius,
               // and lie on the same plane
               if (!(currentCenter.IsAlmostEqualTo(firstArc.Center, vertexEps) && MathUtil.IsAlmostEqual(currentRadius, firstArc.Radius)))
               {
                  return null;
               }
               if (!MathUtil.IsAlmostEqual(Math.Abs(currentNormal.DotProduct(firstCurveNormal)), 1))
               {
                  return null;
               }
            }
            // If all of the curve segments are part of the same circle, then the returning curve will be a circle bounded
            // by the start point of the first curve and the end point of the last curve.
            XYZ lastPoint = currentCurve.GetEndPoint(1);
            if (lastPoint.IsAlmostEqualTo(firstStartPoint, vertexEps))
            {
               firstCurve.MakeUnbound();
            }
            else
            {
               double startParameter = firstArc.GetEndParameter(0);
               double endParameter = firstArc.Project(lastPoint).Parameter;

               if (endParameter < startParameter)
                  endParameter += Math.PI * 2;

               firstCurve.MakeBound(startParameter, endParameter);
            }
            returnCurve = firstCurve;
         }
         else if (firstCurve is Ellipse)
         {
            Ellipse firstEllipse = firstCurve as Ellipse;
            double radiusX = firstEllipse.RadiusX;
            double radiusY = firstEllipse.RadiusY;
            XYZ xDirection = firstEllipse.XDirection;
            XYZ yDirection = firstEllipse.YDirection;
            XYZ firstCurveNormal = firstEllipse.Normal;

            while (curveIterator.MoveNext())
            {
               currentCurve = curveIterator.Current;
               if (!(currentCurve is Ellipse))
                  return null;

               XYZ currentStartPoint = currentCurve.GetEndPoint(0);
               XYZ currentEndPoint = currentCurve.GetEndPoint(1);

               Ellipse currentEllipse = currentCurve as Ellipse;
               XYZ currentCenter = currentEllipse.Center;

               double currentRadiusX = currentEllipse.RadiusX;
               double currentRadiusY = currentEllipse.RadiusY;
               XYZ currentXDirection = currentEllipse.XDirection;
               XYZ currentYDirection = currentEllipse.YDirection;

               XYZ currentNormal = currentEllipse.Normal;

               if (!MathUtil.IsAlmostEqual(Math.Abs(currentNormal.DotProduct(firstCurveNormal)), 1))
               {
                  return null;
               }

               // We determine whether this ellipse is the same as the initial ellipse by checking if their centers and corresponding
               // radiuses as well as radius directions are the same or permutations of each other.
               if (!currentCenter.IsAlmostEqualTo(firstEllipse.Center))
               {
                  return null;
               }

               // Checks if the corresponding radius and radius direction are the same
               if (MathUtil.IsAlmostEqual(radiusX, currentRadiusX))
               {
                  if (!(MathUtil.IsAlmostEqual(radiusY, currentRadiusY) && currentXDirection.IsAlmostEqualTo(xDirection) && currentYDirection.IsAlmostEqualTo(yDirection)))
                  {
                     return null;
                  }
               }
               // Checks if the corresponding radiuses and radius directions are permutations of each other
               else if (MathUtil.IsAlmostEqual(radiusX, currentRadiusY))
               {
                  if (!(MathUtil.IsAlmostEqual(radiusY, currentRadiusX) && currentXDirection.IsAlmostEqualTo(yDirection) && currentYDirection.IsAlmostEqualTo(xDirection)))
                  {
                     return null;
                  }
               }
               else
               {
                  return null;
               }
            }

            // If all of the curve segments are part of the same ellipse then the returning curve will be the ellipse whose start point is the start 
            // point of the first curve and the end point is the end point of the last curve
            XYZ lastPoint = currentCurve.GetEndPoint(1);
            if (lastPoint.IsAlmostEqualTo(firstStartPoint))
            {
               firstCurve.MakeUnbound();
            }
            else
            {
               double startParameter = firstEllipse.GetEndParameter(0);
               double endParameter = firstEllipse.Project(lastPoint).Parameter;

               if (endParameter < startParameter)
               {
                  endParameter += Math.PI * 2;
               }
               firstCurve.MakeBound(startParameter, endParameter);
            }
            returnCurve = firstCurve;
         }

         return returnCurve;
      }
   
   /// <summary>
   /// Calculates the normal of the plane of the curve or curve loop.
   /// </summary>
   /// <returns>The normal, or null if there is no curve or curve loop.</returns>
   public XYZ GetNormal()
      {
         if (Curve != null)
         {
            Transform transform = Curve.ComputeDerivatives(0, false);
            if (transform != null)
               return transform.BasisZ;
         }

         if (CurveLoops == null)
            return null;

         XYZ normal = null;
         foreach (CurveLoop curveLoop in CurveLoops)
         {
            try
            {
               Plane plane = curveLoop.GetPlane();
               if (plane != null)
               {
                  if (normal == null)
                  {
                     normal = plane.Normal;
                  }
                  else
                  {
                     if (!normal.IsAlmostEqualTo(plane.Normal))
                        return null;
                  }
               }
            }
            catch
            {
               return null;
            }
         }
         
         return normal;
      }

      protected IFCCurve()
      {
      }

      override protected void Process(IFCAnyHandle ifcCurve)
      {
         base.Process(ifcCurve);
      }

      protected IFCCurve(IFCAnyHandle profileDef)
      {
         Process(profileDef);
      }

      /// <summary>
      /// Create an IFCCurve object from a handle of type IfcCurve.
      /// </summary>
      /// <param name="ifcCurve">The IFC handle.</param>
      /// <returns>The IFCCurve object.</returns>
      public static IFCCurve ProcessIFCCurve(IFCAnyHandle ifcCurve)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcCurve))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcCurve);
            return null;
         }

         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcCurve, IFCEntityType.IfcBoundedCurve))
            return IFCBoundedCurve.ProcessIFCBoundedCurve(ifcCurve);
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcCurve, IFCEntityType.IfcConic))
            return IFCConic.ProcessIFCConic(ifcCurve);
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcCurve, IFCEntityType.IfcLine))
            return IFCLine.ProcessIFCLine(ifcCurve);
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcCurve, IFCEntityType.IfcOffsetCurve2D))
            return IFCOffsetCurve2D.ProcessIFCOffsetCurve2D(ifcCurve);
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcCurve, IFCEntityType.IfcOffsetCurve3D))
            return IFCOffsetCurve3D.ProcessIFCOffsetCurve3D(ifcCurve);

         Importer.TheLog.LogUnhandledSubTypeError(ifcCurve, IFCEntityType.IfcCurve, true);
         return null;
      }

      private Curve CreateTransformedCurve(Curve baseCurve, IFCRepresentation parentRep, Transform lcs)
      {
         Curve transformedCurve = baseCurve?.CreateTransformed(lcs);
         if (transformedCurve == null)
         {
            Importer.TheLog.LogWarning(Id, "couldn't create curve for " +
                ((parentRep == null) ? "" : parentRep.Identifier.ToString()) +
                " representation.", false);
         }

         return transformedCurve;
      }

      /// <summary>
      /// Create geometry for a particular representation item, and add to scope.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, 
         Transform scaledLcs, string guid)
      {
         base.CreateShapeInternal(shapeEditScope, scaledLcs, guid);

         IFCRepresentation parentRep = shapeEditScope.ContainingRepresentation;

         IList<Curve> transformedCurves = new List<Curve>();
         if (Curve != null)
         {
            Curve transformedCurve = CreateTransformedCurve(Curve, parentRep, scaledLcs);
            if (transformedCurve != null)
               transformedCurves.Add(transformedCurve);
         }
         else if (CurveLoops != null)
         {
            foreach (CurveLoop curveLoop in CurveLoops)
            {
               foreach (Curve curve in curveLoop)
               {
                  Curve transformedCurve = CreateTransformedCurve(curve, parentRep, scaledLcs);
                  if (transformedCurve != null)
                     transformedCurves.Add(transformedCurve);
               }
            }
         }

         // TODO: set graphics style for footprint curves.
         IFCRepresentationIdentifier repId = (parentRep == null) ? IFCRepresentationIdentifier.Unhandled : parentRep.Identifier;
         bool createModelGeometry = (repId == IFCRepresentationIdentifier.Body) || 
            (repId == IFCRepresentationIdentifier.Axis) || 
            (repId == IFCRepresentationIdentifier.Unhandled);
         bool createFootprintGeometry = (repId == IFCRepresentationIdentifier.FootPrint);

         ElementId gstyleId = ElementId.InvalidElementId;
         if (createModelGeometry || createFootprintGeometry)
         {
            Category curveCategory = IFCCategoryUtil.GetSubCategoryForRepresentation(shapeEditScope.Document, Id, parentRep.Identifier);
            if (curveCategory != null)
            {
               GraphicsStyle graphicsStyle = curveCategory.GetGraphicsStyle(GraphicsStyleType.Projection);
               if (graphicsStyle != null)
                  gstyleId = graphicsStyle.Id;
            }
         }

         foreach (Curve curve in transformedCurves)
         {
            if (gstyleId != ElementId.InvalidElementId)
               curve.SetGraphicsStyleId(gstyleId);
  
            // If it is not model geometry, assume it is a plan view curve.
            if (createModelGeometry)
               shapeEditScope.AddGeometry(IFCSolidInfo.Create(Id, curve));
            else
               shapeEditScope.AddFootprintCurve(curve);
         }
      }
   }
}