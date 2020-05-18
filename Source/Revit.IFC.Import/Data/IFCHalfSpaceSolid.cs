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
   public class IFCHalfSpaceSolid : IFCRepresentationItem, IIFCBooleanOperand
   {
      IFCSurface m_BaseSurface = null;

      // for IfcPolygonalHalfSpaceSolid only.
      IFCCurve m_BaseBoundingCurve = null;
      Transform m_BaseBoundingCurveTransform = null;

      bool m_AgreementFlag = true;

      public IFCSurface BaseSurface
      {
         get { return m_BaseSurface; }
         protected set { m_BaseSurface = value; }
      }

      public IFCCurve BaseBoundingCurve
      {
         get { return m_BaseBoundingCurve; }
         protected set { m_BaseBoundingCurve = value; }
      }

      public Transform BaseBoundingCurveTransform
      {
         get { return m_BaseBoundingCurveTransform; }
         protected set { m_BaseBoundingCurveTransform = value; }
      }

      public bool AgreementFlag
      {
         get { return m_AgreementFlag; }
         protected set { m_AgreementFlag = value; }
      }

      protected IFCHalfSpaceSolid()
      {
      }

      override protected void Process(IFCAnyHandle solid)
      {
         base.Process(solid);

         bool found = false;
         bool agreementFlag = IFCImportHandleUtil.GetRequiredBooleanAttribute(solid, "AgreementFlag", out found);
         if (found)
            AgreementFlag = agreementFlag;

         IFCAnyHandle baseSurface = IFCImportHandleUtil.GetRequiredInstanceAttribute(solid, "BaseSurface", true);
         BaseSurface = IFCSurface.ProcessIFCSurface(baseSurface);
         if (!(BaseSurface is IFCPlane))
            Importer.TheLog.LogUnhandledSubTypeError(baseSurface, IFCEntityType.IfcSurface, true);

         if (IFCAnyHandleUtil.IsValidSubTypeOf(solid, IFCEntityType.IfcPolygonalBoundedHalfSpace))
         {
            IFCAnyHandle position = IFCImportHandleUtil.GetRequiredInstanceAttribute(solid, "Position", false);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(position))
               BaseBoundingCurveTransform = IFCLocation.ProcessIFCAxis2Placement(position);
            else
               BaseBoundingCurveTransform = Transform.Identity;

            IFCAnyHandle boundaryCurveHandle = IFCImportHandleUtil.GetRequiredInstanceAttribute(solid, "PolygonalBoundary", true);
            BaseBoundingCurve = IFCCurve.ProcessIFCCurve(boundaryCurveHandle);
            if (BaseBoundingCurve == null || BaseBoundingCurve.GetTheCurveLoop() == null)
               Importer.TheLog.LogError(Id, "IfcPolygonalBoundedHalfSpace has an invalid boundary, ignoring.", true);
         }
      }

      /// <summary>
      /// Create geometry for an IfcHalfSpaceSolid.
      /// </summary>
      /// <param name="shapeEditScope">The shape edit scope.</param>
      /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      /// <returns>A list containing one geometry for the IfcHalfSpaceSolid.</returns>
      protected virtual IList<GeometryObject> CreateGeometryInternal(
            IFCImportShapeEditScope shapeEditScope, Transform unscaledLcs, Transform scaledLcs, string guid)
      {
         IFCPlane ifcPlane = BaseSurface as IFCPlane;
         Plane plane = ifcPlane.Plane;
         XYZ origin = plane.Origin;
         XYZ xVec = plane.XVec;
         XYZ yVec = plane.YVec;

         // Set some huge boundaries for now.
         const double largeCoordinateValue = 100000;
         XYZ[] corners = new XYZ[4] {
                unscaledLcs.OfPoint((xVec * -largeCoordinateValue) + (yVec * -largeCoordinateValue) + origin),
                unscaledLcs.OfPoint((xVec * largeCoordinateValue) + (yVec * -largeCoordinateValue) + origin),
                unscaledLcs.OfPoint((xVec * largeCoordinateValue) + (yVec * largeCoordinateValue) + origin),
                unscaledLcs.OfPoint((xVec * -largeCoordinateValue) + (yVec * largeCoordinateValue) + origin)
            };

         IList<CurveLoop> loops = new List<CurveLoop>();
         CurveLoop loop = new CurveLoop();
         for (int ii = 0; ii < 4; ii++)
         {
            if (AgreementFlag)
               loop.Append(Line.CreateBound(corners[(5 - ii) % 4], corners[(4 - ii) % 4]));
            else
               loop.Append(Line.CreateBound(corners[ii], corners[(ii + 1) % 4]));
         }
         loops.Add(loop);

         XYZ normal = unscaledLcs.OfVector(AgreementFlag ? -plane.Normal : plane.Normal);
         SolidOptions solidOptions = new SolidOptions(GetMaterialElementId(shapeEditScope), shapeEditScope.GraphicsStyleId);
         Solid baseSolid = GeometryCreationUtilities.CreateExtrusionGeometry(loops, normal, largeCoordinateValue, solidOptions);

         if (BaseBoundingCurve != null)
         {
            CurveLoop polygonalBoundary = BaseBoundingCurve.GetTheCurveLoop();

            Transform unscaledTotalTransform = unscaledLcs.Multiply(BaseBoundingCurveTransform);
            Transform scaledTotalTransform = scaledLcs.Multiply(BaseBoundingCurveTransform);

            // Make sure this bounding polygon extends below base of half-space soild.
            Transform moveBaseTransform = Transform.Identity;
            moveBaseTransform.Origin = new XYZ(0, 0, -largeCoordinateValue);

            unscaledTotalTransform = unscaledTotalTransform.Multiply(moveBaseTransform);
            scaledTotalTransform = scaledTotalTransform.Multiply(moveBaseTransform);

            CurveLoop transformedPolygonalBoundary = IFCGeometryUtil.CreateTransformed(polygonalBoundary, Id, unscaledTotalTransform, scaledTotalTransform);
            IList<CurveLoop> boundingLoops = new List<CurveLoop>();
            boundingLoops.Add(transformedPolygonalBoundary);

            Solid boundingSolid = GeometryCreationUtilities.CreateExtrusionGeometry(boundingLoops, unscaledTotalTransform.BasisZ, 2.0 * largeCoordinateValue,
                solidOptions);
            baseSolid = IFCGeometryUtil.ExecuteSafeBooleanOperation(Id, BaseBoundingCurve.Id, baseSolid, boundingSolid, BooleanOperationsType.Intersect, null);
         }

         IList<GeometryObject> returnList = new List<GeometryObject>();
         returnList.Add(baseSolid);
         return returnList;
      }

      /// <summary>
      /// Return geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The shape edit scope.</param>
      /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      /// <returns>The created geometries.</returns>
      public IList<GeometryObject> CreateGeometry(
            IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
      {
         return CreateGeometryInternal(shapeEditScope, lcs, scaledLcs, guid);
      }

      /// <summary>
      /// In case of a Boolean operation failure, provide a recommended direction to shift the geometry in for a second attempt.
      /// </summary>
      /// <param name="lcs">The local transform for this entity.</param>
      /// <returns>An XYZ representing a unit direction vector, or null if no direction is suggested.</returns>
      /// <remarks>If the 2nd attempt fails, a third attempt will be done with a shift in the opposite direction.</remarks>
      public XYZ GetSuggestedShiftDirection(Transform lcs)
      {
         IFCPlane ifcPlane = BaseSurface as IFCPlane;
         Plane plane = (ifcPlane != null) ? ifcPlane.Plane : null;
         XYZ untransformedNorm = (plane != null) ? plane.Normal : null;
         return (lcs == null) ? untransformedNorm : lcs.OfVector(untransformedNorm);
      }

      protected IFCHalfSpaceSolid(IFCAnyHandle solid)
      {
         Process(solid);
      }

      /// <summary>
      /// Create an IFCHalfSpaceSolid object from a handle of type IfcHalfSpaceSolid.
      /// </summary>
      /// <param name="ifcHalfSpaceSolid">The IFC handle.</param>
      /// <returns>The IFCHalfSpaceSolid object.</returns>
      public static IFCHalfSpaceSolid ProcessIFCHalfSpaceSolid(IFCAnyHandle ifcHalfSpaceSolid)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcHalfSpaceSolid))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcHalfSpaceSolid);
            return null;
         }

         IFCEntity halfSpaceSolid;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcHalfSpaceSolid.StepId, out halfSpaceSolid))
            halfSpaceSolid = new IFCHalfSpaceSolid(ifcHalfSpaceSolid);

         return (halfSpaceSolid as IFCHalfSpaceSolid);
      }
   }
}