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
      private double m_Slope = 0;

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
                  double tempSlope = UnitUtil.ScaleAngle(MathUtil.SafeAsin(factor));
                  if (!Double.IsNaN(tempSlope))
                  {
                     m_Slope = tempSlope;
                     return true;
                  }
               }
            }
         }

         // This works for Ramp/RampFlight
         double slope = 0.0;
         if (ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.RAMP_ATTR_MIN_INV_SLOPE, out slope) != null)
         {
            m_Slope = slope;

            if (!MathUtil.IsAlmostZero(m_Slope))
            {
               m_Slope = UnitUtil.ScaleAngle(Math.Atan(m_Slope));
               return true;
            }
         }

         // For other elements with ExtrusionData. Parameter will take precedence (override)
         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, "IfcSlope", out m_Slope) == null)
            ParameterUtil.GetDoubleValueFromElementOrSymbol(element, "Slope", out m_Slope);
         m_Slope = UnitUtil.ScaleAngle(m_Slope);
         if (m_Slope > MathUtil.Eps())
            return true;

         if (extrusionCreationData != null)
         {
            m_Slope = extrusionCreationData.Slope;
            return true;
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
            m_Slope = GeometryUtil.GetAngleOfFace(largestTopFace, faceNormalProjXYPlane);
            return true;
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
         return m_Slope;
      }
   }

   /// <summary>
   /// The PitchAngleCalculator calculator, which is the same as the SlopeCalculator.
   /// </summary>
   class PitchAngleCalculator : SlopeCalculator
   {

   }
}
