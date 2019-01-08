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
using Autodesk.Revit.DB.Structure;
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate span value for a beam.
   /// </summary>
   class SpanCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_Span = 0;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static SpanCalculator s_Instance = new SpanCalculator();

      /// <summary>
      /// The BeamSpanCalculator instance.
      /// </summary>
      public static SpanCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates span value for a beam.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="extrusionCreationData">
      /// The IFCExtrusionCreationData.
      /// </param>
      /// <param name="element">
      /// The element to calculate the value.
      /// </param>
      /// <param name="elementType">
      /// The element type.
      /// </param>
      /// <returns>
      /// True if the operation succeed, false otherwise.
      /// </returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element, ElementType elementType)
      {
         // Check the override first from "IfcSpan" parameter, if not overriden use the geometry data from extrusion
         double spanVal;
         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, "Span", out spanVal) == null)
            return false;

         spanVal = UnitUtil.ScaleLength(spanVal);
         if (spanVal > MathUtil.Eps())
         {
            m_Span = spanVal;
            return true;
         }

         if (extrusionCreationData == null || MathUtil.IsAlmostZero(extrusionCreationData.ScaledLength))
         {
            return false;
         }
         m_Span = extrusionCreationData.ScaledLength;
         AnalyticalModel elemAnalyticalModel = element.GetAnalyticalModel();
         if (elemAnalyticalModel != null)
         {
            IList<AnalyticalModelSupport> supports = elemAnalyticalModel.GetAnalyticalModelSupports();
            if (supports != null && supports.Count > 0)
            {
               if (supports.Count == 2)
               {
                  AnalyticalSupportType supportType1 = supports[0].GetSupportType();
                  AnalyticalSupportType supportType2 = supports[1].GetSupportType();
                  // If there are exactly 2 supports, calculate the distance between the supports for Span (if the type is PointSupport)
                  if (supportType1 == AnalyticalSupportType.PointSupport && supportType2 == AnalyticalSupportType.PointSupport)
                  {
                     XYZ support1 = supports[0].GetPoint();
                     XYZ support2 = supports[1].GetPoint();
                     m_Span = UnitUtil.ScaleLength(support1.DistanceTo(support2));
                  }
                  // CurveSUpport or SurfaceSupport??
                  else
                  {
                     if (supportType1 == AnalyticalSupportType.PointSupport)
                     {
                        XYZ supportP = supports[0].GetPoint();
                        if (supportType2 == AnalyticalSupportType.CurveSupport)
                        {
                           Curve supportC = supports[1].GetCurve();
                           m_Span = UnitUtil.ScaleLength(supportC.Distance(supportP));
                        }
                        else if (supportType2 == AnalyticalSupportType.SurfaceSupport)
                        {
                           Face supportF = supports[1].GetFace();
                           m_Span = UnitUtil.ScaleLength(supportF.Project(supportP).Distance);
                        }
                     }
                     else if (supportType1 == AnalyticalSupportType.CurveSupport)
                     {
                        Curve supportC = supports[0].GetCurve();
                        if (supportType2 == AnalyticalSupportType.PointSupport)
                        {
                           XYZ supportP = supports[1].GetPoint();
                           m_Span = UnitUtil.ScaleLength(supportC.Distance(supportP));
                        }
                        else if (supportType2 == AnalyticalSupportType.SurfaceSupport)
                        {
                           Face supportF = supports[1].GetFace();
                           // TODO, how to calculate a distance from a Curve to a Face?
                        }
                     }
                     else if (supportType1 == AnalyticalSupportType.SurfaceSupport)
                     {
                        Face supportF = supports[0].GetFace();
                        if (supportType2 == AnalyticalSupportType.PointSupport)
                        {
                           XYZ supportP = supports[1].GetPoint();
                           m_Span = UnitUtil.ScaleLength(supportF.Project(supportP).Distance);
                        }
                        else if (supportType2 == AnalyticalSupportType.CurveSupport)
                        {
                           Curve supportC = supports[1].GetCurve();
                           // TODO, how to calculate a distance from a Curve to a Face?
                        }
                     }
                  }
               }
               else if (supports.Count > 2)
               {
                  // If there are more than 2 supports, which Span to take??
               }
               else
               {
                  // If only one or less support
                  // Otherwise do nothing, leave it to the extrusion length
               }
            }
            else
            {
               // No support, do nothing. Leave the Span to be the length of the entire beam
            }
         }
         return true;
      }

      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>
      /// The double value.
      /// </returns>
      public override double GetDoubleValue()
      {
         return m_Span;
      }
   }
}
