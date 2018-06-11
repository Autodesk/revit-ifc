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
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public abstract class IFCCurve : IFCRepresentationItem
   {
      // One of m_Curve or m_CurveLoop will be non-null.
      Curve m_Curve = null;

      CurveLoop m_CurveLoop = null;

      // In theory, some IfcCurves may use a non-unit length IfcVector to influence the parametrization of the underlying curve.
      // While in practice this is unlikely, we keep this value to be complete.
      double m_ParametericScaling = 1.0;

      protected double ParametericScaling
      {
         get { return m_ParametericScaling; }
         set { m_ParametericScaling = value; }
      }

      /// <summary>
      /// Get the Curve representation of IFCCurve.  It could be null.
      /// </summary>
      public Curve Curve
      {
         get { return m_Curve; }
         protected set { m_Curve = value; }
      }

      /// <summary>
      /// Get the CurveLoop representation of IFCCurve.  It could be null.
      /// </summary>
      public CurveLoop CurveLoop
      {
         get { return m_CurveLoop; }
         protected set { m_CurveLoop = value; }
      }

      /// <summary>
      /// Get the curve or CurveLoop representation of IFCCurve, as a list of 0 or more curves.
      /// </summary>
      public IList<Curve> GetCurves()
      {
         IList<Curve> curves = new List<Curve>();

         if (Curve != null)
            curves.Add(Curve);
         else if (CurveLoop != null)
         {
            foreach (Curve curve in CurveLoop)
               curves.Add(curve);
         }

         return curves;
      }

      /// <summary>
      /// Get the curve or CurveLoop representation of IFCCurve, as a CurveLoop.  This will have a value, as long as Curve or CurveLoop do.
      /// </summary>
      public CurveLoop GetCurveLoop()
      {
         if (CurveLoop != null)
            return CurveLoop;
         if (Curve == null)
            return null;

         CurveLoop curveAsCurveLoop = new CurveLoop();
         curveAsCurveLoop.Append(Curve);
         return curveAsCurveLoop;
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
         else if (CurveLoop != null)
         {
            try
            {
               Plane plane = CurveLoop.GetPlane();
               if (plane != null)
                  return plane.Normal;
            }
            catch
            {
            }
         }

         return null;
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

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcCurve, IFCEntityType.IfcBoundedCurve))
            return IFCBoundedCurve.ProcessIFCBoundedCurve(ifcCurve);
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcCurve, IFCEntityType.IfcConic))
            return IFCConic.ProcessIFCConic(ifcCurve);
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcCurve, IFCEntityType.IfcLine))
            return IFCLine.ProcessIFCLine(ifcCurve);
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcCurve, IFCEntityType.IfcOffsetCurve2D))
            return IFCOffsetCurve2D.ProcessIFCOffsetCurve2D(ifcCurve);
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcCurve, IFCEntityType.IfcOffsetCurve3D))
            return IFCOffsetCurve3D.ProcessIFCOffsetCurve3D(ifcCurve);

         Importer.TheLog.LogUnhandledSubTypeError(ifcCurve, IFCEntityType.IfcCurve, true);
         return null;
      }

      private Curve CreateTransformedCurve(Curve baseCurve, IFCRepresentation parentRep, Transform lcs)
      {
         Curve transformedCurve = (baseCurve != null) ? baseCurve.CreateTransformed(lcs) : null;
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
      /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
      {
         base.CreateShapeInternal(shapeEditScope, lcs, scaledLcs, guid);

         IFCRepresentation parentRep = shapeEditScope.ContainingRepresentation;

         IList<Curve> transformedCurves = new List<Curve>();
         if (Curve != null)
         {
            Curve transformedCurve = CreateTransformedCurve(Curve, parentRep, lcs);
            if (transformedCurve != null)
               transformedCurves.Add(transformedCurve);
         }
         else if (CurveLoop != null)
         {
            foreach (Curve curve in CurveLoop)
            {
               Curve transformedCurve = CreateTransformedCurve(curve, parentRep, lcs);
               if (transformedCurve != null)
                  transformedCurves.Add(transformedCurve);
            }
         }

         // TODO: set graphics style for footprint curves.
         IFCRepresentationIdentifier repId = (parentRep == null) ? IFCRepresentationIdentifier.Unhandled : parentRep.Identifier;
         bool createModelGeometry = (repId == IFCRepresentationIdentifier.Body) || (repId == IFCRepresentationIdentifier.Axis) || (repId == IFCRepresentationIdentifier.Unhandled);

         ElementId gstyleId = ElementId.InvalidElementId;
         if (createModelGeometry)
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
            if (createModelGeometry)
            {
               curve.SetGraphicsStyleId(gstyleId);
               shapeEditScope.AddGeometry(IFCSolidInfo.Create(Id, curve));
            }
            else
            {
               // Default: assume a plan view curve.
               shapeEditScope.AddFootprintCurve(curve);
            }
         }
      }
   }
}