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
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate slope value.
   /// </summary>
   class SlopeCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double? m_Slope = null;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static SlopeCalculator s_Instance = new SlopeCalculator();

      /// <summary>
      /// The SlopeCalculator instance.
      /// </summary>
      public static SlopeCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates slope value.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="extrusionCreationData">The IFCExtrusionCreationData.</param>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="elementType">The element type.</param>
      /// <returns>True if the operation succeed, false otherwise.</returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element, ElementType elementType)
      {
         double slope = double.NaN;
         // We may have an extrusionCreationData that doesn't have anything set.  We will check this by seeing if there is a valid length set.
         // This works for Beam 
         if (extrusionCreationData == null || MathUtil.IsAlmostZero(extrusionCreationData.ScaledLength))
         {
            // Try looking for parameters that we can calculate slope from.
            double startParamHeight = 0.0;
            double endParamHeight = 0.0;
            double length = 0.0;

            if ((ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.STRUCTURAL_BEAM_END0_ELEVATION, out startParamHeight) != null) &&
               (ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.STRUCTURAL_BEAM_END1_ELEVATION, out endParamHeight) != null) &&
               (ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.INSTANCE_LENGTH_PARAM, out length) != null))
            {
               if (!MathUtil.IsAlmostZero(length))
               {
                  double factor = Math.Abs(endParamHeight - startParamHeight) / length;
                  slope = UnitUtil.ScaleAngle(MathUtil.SafeAsin(factor));
                  if (!double.IsNaN(slope))
                  {
                     m_Slope = slope;
                     return true;
                  }
               }
            }
         }

         // This works for Ramp/RampFlight
         if (ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.RAMP_ATTR_MIN_INV_SLOPE, out slope) != null)
         {
            m_Slope = UnitUtil.ScaleAngle(Math.Atan(slope));
            return true;
         }

         // For other elements with ExtrusionData. Parameter will take precedence (override)
         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, "Slope", out slope) != null)
         {
            m_Slope = UnitUtil.ScaleAngle(slope);
            return true;
         }

         if (extrusionCreationData != null)
         {
            if (extrusionCreationData.Slope > MathUtil.Eps())
            {
               m_Slope = extrusionCreationData.Slope;
               return true;
            }
            else
            {
               // For any element that has axis, the slope will be computed based on the angle of the line vector
               if (element.Location != null && element.Location is LocationCurve)
               {
                  LocationCurve axis = element.Location as LocationCurve;
                  if (axis.Curve is Line)
                  {
                     Line axisCurve = axis.Curve as Line;
                     XYZ vectorProjOnXY = new XYZ(axisCurve.Direction.X, axisCurve.Direction.Y, 0.0).Normalize(); //Project the vector to XY plane
                     if (axisCurve.Direction.GetLength() > 0.0 && vectorProjOnXY.GetLength() > 0.0)
                        slope = UnitUtil.ScaleAngle(MathUtil.SafeAcos(axisCurve.Direction.DotProduct(vectorProjOnXY) / (axisCurve.Direction.GetLength() * vectorProjOnXY.GetLength())));

                     if (!double.IsNaN(slope))
                     {
                        m_Slope = slope;
                        return true;
                     }
                  }
               }
            }
         }

         // The last attempt to compute the slope angle is to get the slope of the largest top facing face of the geometry
         GeometryElement geomElem = element.get_Geometry(GeometryUtil.GetIFCExportGeometryOptions());
         Face largestTopFace = null;

         if (geomElem == null)
            return false;

         foreach (GeometryObject geomObj in geomElem)
         {
            largestTopFace = GeometryUtil.GetLargestFaceInSolid(geomObj, new XYZ(0,0,1));
         }

         if (largestTopFace != null)
         {
            XYZ faceNormal = largestTopFace.ComputeNormal(new UV());
            XYZ faceNormalProjXYPlane = new XYZ(faceNormal.X, faceNormal.Y, 0.0).Normalize();
            slope = GeometryUtil.GetAngleOfFace(largestTopFace, faceNormalProjXYPlane);
            if (!double.IsNaN(slope))
            {
               m_Slope = slope;
               return true;
            }
         }

         return false;
      }

      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>
      /// The double value.
      /// </returns>
      public override double GetDoubleValue()
      {
         if (m_Slope.HasValue)
            return m_Slope.Value;
         else
            return 0.0;
      }
   }

   /// <summary>
   /// The PitchAngleCalculator calculator, which is the same as the SlopeCalculator.
   /// </summary>
   class PitchAngleCalculator : SlopeCalculator
   {

   }
}
