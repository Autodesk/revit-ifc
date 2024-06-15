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
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// A structure to contain information about the defining axis of a structural member element (beam, column, brace).
   /// </summary>
   public class StructuralMemberAxisInfo
   {
      /// <summary>
      /// The default constructor.
      /// </summary>
      public StructuralMemberAxisInfo()
      {
         Axis = null;
         LCSAsTransform = null;
         AxisDirection = null;
         AxisNormal = null;
      }

      /// <summary>
      /// The curve that represents the structural member axis.
      /// </summary>
      public Curve Axis { get; set; }

      /// <summary>
      /// The local coordinate system of the structural member used for IFC export as a transform.
      /// </summary>
      public Transform LCSAsTransform { get; set; }

      /// <summary>
      /// The tangent to the axis at the start parameter of the axis curve.
      /// </summary>
      public XYZ AxisDirection { get; set; }

      /// <summary>
      /// The normal to the axis at the start parameter of the axis curve.
      /// </summary>
      public XYZ AxisNormal { get; set; }
   }

   class StructuralMemberExporter
   {
      /// <summary>
      /// Get information about the structural member axis, if possible.
      /// Here we will do the following:
      /// - Calculate the Axis LCS by using the Axis curve itself with the StartPoint of the curve to be the origin and the tangent of the curve at origin as the direction
      /// - The curve is then transformed to its LCS
      /// </summary>
      /// <param name="element">The structural member element.</param>
      /// <returns>The StructuralMemberAxisInfo structure, or null if the structural member has no axis, or it is not a Line or Arc.</returns>
      public static StructuralMemberAxisInfo GetStructuralMemberAxisTransform(Element element)
      {
         StructuralMemberAxisInfo axisInfo = null;
         Transform orientTrf = Transform.Identity;

         XYZ structMemberDirection = null;
         XYZ projDir = null;
         Curve curve = null;

         LocationCurve locCurve = element.Location as LocationCurve;
         bool canExportAxis = (locCurve != null);

         if (canExportAxis)
         {
            // Here we are defining the Axis Curve LCS by using the start point (for line) as the origin.
            // For the Arc, the center is the origin
            // The Structural member direction is the X-Axis
            curve = locCurve.Curve;
            if (curve is Line)
            {
               Line line = curve as Line;
               XYZ planeY;

               XYZ LCSorigin = line.GetEndPoint(0);
               structMemberDirection = line.Direction.Normalize();
               if (Math.Abs(structMemberDirection.Z) < 0.707)  // approx 1.0/sqrt(2.0)
               {
                  planeY = XYZ.BasisZ.CrossProduct(structMemberDirection).Normalize();
               }
               else
               {
                  planeY = XYZ.BasisX.CrossProduct(structMemberDirection).Normalize();
               }
               projDir = structMemberDirection.CrossProduct(planeY);
               orientTrf.BasisX = structMemberDirection; orientTrf.BasisY = planeY; orientTrf.BasisZ = projDir; orientTrf.Origin = LCSorigin;
            }
            else if (curve is Arc)
            {
               XYZ yDir;
               Arc arc = curve as Arc;
               structMemberDirection = arc.XDirection.Normalize();
               yDir = arc.YDirection.Normalize();
               projDir = arc.Normal;

               XYZ center = arc.Center;

               if (!MathUtil.IsAlmostZero(structMemberDirection.DotProduct(yDir)))
               {
                  // ensure that beamDirection and yDir are orthogonal
                  yDir = projDir.CrossProduct(structMemberDirection);
                  yDir = yDir.Normalize();
               }
               orientTrf.BasisX = structMemberDirection; orientTrf.BasisY = yDir; orientTrf.BasisZ = projDir; orientTrf.Origin = center;
            }
            else
            {
               canExportAxis = false;
            }
         }

         if (canExportAxis)
         {
            axisInfo = new StructuralMemberAxisInfo();
            axisInfo.Axis = curve.CreateTransformed(orientTrf.Inverse);       // transform the curve into its LCS
            axisInfo.AxisDirection = orientTrf.BasisX;                // We define here the Axis Curve to be following the X-axis
            axisInfo.AxisNormal = orientTrf.BasisZ;
            axisInfo.LCSAsTransform = orientTrf;
         }

         return axisInfo;
      }

      static Transform FlipYTrf()
      {
         Transform flipYTrf = Transform.Identity;
         flipYTrf.BasisX = new XYZ(1, 0, 0);
         flipYTrf.BasisY = new XYZ(0, -1, 0);
         flipYTrf.BasisZ = new XYZ(0, 0, 1);
         flipYTrf.Origin = new XYZ(0, 0, 0);
         return flipYTrf;
      }

      /// <summary>
      /// Create the handle corresponding to the "Axis" IfcRepresentation for a structural member objects, if possible.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC class.</param>
      /// <param name="element">The structural member element.</param>
      /// <param name="catId">The structural member category id.</param>
      /// <param name="axisInfo">The optional structural member axis information.</param>
      /// <param name="offsetTransform">The optional offset transform applied to the "Body" representation.</param>
      /// <returns>The handle, or null if not created.</returns>
      public static IFCAnyHandle CreateStructuralMemberAxis(ExporterIFC exporterIFC, Element element, ElementId catId, StructuralMemberAxisInfo axisInfo, Transform newTransformLCS)
      {
         if (axisInfo == null)
            return null;

         // This Axis should have been transformed into its ECS position previously (in GetStructuralMemberAxisTransform())
         Curve curve = axisInfo.Axis;
         Transform offset = Transform.Identity;
         if (newTransformLCS != null)
         {
            // We need to flip the Left-handed transform to the right-hand one as IFC only support right-handed coordinate system
            if (newTransformLCS.Determinant < 0)
            {
               XYZ orig = newTransformLCS.Origin;
               newTransformLCS.Origin = XYZ.Zero;
               offset = FlipYTrf().Multiply(newTransformLCS);
               offset.Origin = orig;
            }
            else
               offset = newTransformLCS;
         }

         // Calculate the transformation matrix to tranform the original Axis Curve at its ECS into the new ECS assigned in the offset
         curve = curve.CreateTransformed(offset.Inverse.Multiply(axisInfo.LCSAsTransform));

         IDictionary<IFCFuzzyXYZ, IFCAnyHandle> cachePoints = new Dictionary<IFCFuzzyXYZ, IFCAnyHandle>();
         const GeometryUtil.TrimCurvePreference trimCurvePreference = GeometryUtil.TrimCurvePreference.TrimmedCurve;
         IFCAnyHandle ifcCurveHnd = GeometryUtil.CreateIFCCurveFromRevitCurve(exporterIFC.GetFile(), 
            exporterIFC, curve, true, cachePoints, trimCurvePreference, null);
         IList<IFCAnyHandle> axis_items = new List<IFCAnyHandle>();
         if (!(IFCAnyHandleUtil.IsNullOrHasNoValue(ifcCurveHnd)))
            axis_items.Add(ifcCurveHnd);

         if (axis_items.Count > 0)
         {
            IFCRepresentationIdentifier identifier = IFCRepresentationIdentifier.Axis;
            string identifierOpt = identifier.ToString();   // This is by IFC2x2+ convention.
            string representationTypeOpt = "Curve3D";  // This is by IFC2x2+ convention.
            IFCAnyHandle contextOfItems = ExporterCacheManager.Get3DContextHandle(identifier);
            IFCAnyHandle axisRep = RepresentationUtil.CreateShapeRepresentation(exporterIFC, 
               element, catId, contextOfItems, identifierOpt, representationTypeOpt, 
               axis_items);
            return axisRep;
         }

         return null;
      }
   }
}