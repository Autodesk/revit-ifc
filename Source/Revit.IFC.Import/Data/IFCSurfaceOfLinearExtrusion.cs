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
   public class IFCSurfaceOfLinearExtrusion : IFCSweptSurface
   {
      XYZ m_ExtrudedDirection = null;

      double m_Depth = 0.0;

      public XYZ ExtrudedDirection
      {
         get { return m_ExtrudedDirection; }
         protected set { m_ExtrudedDirection = value; }
      }

      public double Depth
      {
         get { return m_Depth; }
         protected set { m_Depth = value; }
      }

      /// <summary>
      /// Get the local surface transform at a given point on the surface.
      /// </summary>
      /// <param name="pointOnSurface">The point.</param>
      /// <returns>The transform.</returns>
      /// <remarks>This does not include the translation component.</remarks>
      public override Transform GetTransformAtPoint(XYZ pointOnSurface)
      {
         if (!(SweptCurve is IFCSimpleProfile))
         {
            // LOG: ERROR: warn that we only support simple profiles.
            return null;
         }

         CurveLoop outerCurveLoop = (SweptCurve as IFCSimpleProfile).OuterCurve;
         if (outerCurveLoop == null || outerCurveLoop.Count() != 1)
         {
            // LOG: ERROR
            return null;
         }

         Curve outerCurve = outerCurveLoop.First();
         if (outerCurve == null)
         {
            // LOG: ERROR
            return null;
         }

         IntersectionResult result = outerCurve.Project(pointOnSurface);
         if (result == null)
         {
            // LOG: ERROR
            return null;
         }

         double parameter = result.Parameter;

         Transform atPoint = outerCurve.ComputeDerivatives(parameter, false);
         atPoint.set_Basis(0, atPoint.BasisX.Normalize());
         atPoint.set_Basis(1, atPoint.BasisY.Normalize());
         atPoint.set_Basis(2, atPoint.BasisZ.Normalize());
         atPoint.Origin = pointOnSurface;

         return atPoint;
      }

      protected IFCSurfaceOfLinearExtrusion()
      {
      }

      override protected void Process(IFCAnyHandle ifcSurface)
      {
         base.Process(ifcSurface);

         IFCAnyHandle extrudedDirection = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcSurface, "ExtrudedDirection", true);
         m_ExtrudedDirection = IFCPoint.ProcessNormalizedIFCDirection(extrudedDirection);
         // The extruded direction is relative to the lcs of the IfcSweptSurface position
         m_ExtrudedDirection = Position.OfVector(m_ExtrudedDirection);

         bool found = false;
         Depth = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(ifcSurface, "Depth", out found);
         if (!found)
            Importer.TheLog.LogError(Id, "IfcSurfaceOfLinearExtrusion has no height, ignoring.", true);
      }

      protected IFCSurfaceOfLinearExtrusion(IFCAnyHandle surfaceOfLinearExtrusion)
      {
         Process(surfaceOfLinearExtrusion);
      }

      /// <summary>
      /// Create an IFCSurfaceOfLinearExtrusion object from a handle of type IfcSurfaceOfLinearExtrusion.
      /// </summary>
      /// <param name="ifcSurfaceOfLinearExtrusion">The IFC handle.</param>
      /// <returns>The IFCSurfaceOfLinearExtrusion object.</returns>
      public static IFCSurfaceOfLinearExtrusion ProcessIFCSurfaceOfLinearExtrusion(IFCAnyHandle ifcSurfaceOfLinearExtrusion)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSurfaceOfLinearExtrusion))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcSurfaceOfLinearExtrusion);
            return null;
         }

         IFCEntity surfaceOfLinearExtrusion;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSurfaceOfLinearExtrusion.StepId, out surfaceOfLinearExtrusion))
            surfaceOfLinearExtrusion = new IFCSurfaceOfLinearExtrusion(ifcSurfaceOfLinearExtrusion);

         return (surfaceOfLinearExtrusion as IFCSurfaceOfLinearExtrusion);
      }

      /// <summary>
      /// Returns the surface which defines the internal shape of the face
      /// </summary>
      /// <param name="lcs">The local coordinate system for the surface.  Can be null.</param>
      /// <returns>The surface which defines the internal shape of the face</returns>
      public override Surface GetSurface(Transform lcs)
      {
         Curve sweptCurve = null;
         // Get the RuledSurface which is used to create the geometry from the brepbuilder
         if (!(SweptCurve is IFCSimpleProfile))
         {
            return null;
         }
         else
         {
            // Currently there is no easy way to get the curve from the IFCProfile, so for now we assume that
            // the SweptCurve is an IFCSimpleProfile and its outer curve only contains one curve, which is the 
            // profile curve that we want
            IFCSimpleProfile simpleSweptCurve = SweptCurve as IFCSimpleProfile;
            CurveLoop outerCurve = simpleSweptCurve.OuterCurve;
            if (outerCurve == null)
            {
               return null;
            }
            CurveLoopIterator it = outerCurve.GetCurveLoopIterator();
            sweptCurve = it.Current;
         }
         // Position/transform the Curve first according to the lcs of the IfcSurfaceOfLinearExtrusion
         sweptCurve = sweptCurve.CreateTransformed(Position);

         // Create the second profile curve by translating the first one in the extrusion direction
         Curve profileCurve2 = sweptCurve.CreateTransformed(Transform.CreateTranslation(ExtrudedDirection.Multiply(Depth)));

         if (lcs == null)
            return RuledSurface.Create(sweptCurve, profileCurve2);

         Curve transformedProfileCurve1 = sweptCurve.CreateTransformed(lcs);
         Curve transformedProfileCurve2 = profileCurve2.CreateTransformed(lcs);

         return RuledSurface.Create(transformedProfileCurve1, transformedProfileCurve2);
      }
   }
}