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
using Revit.IFC.Export.Utility;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate gross area.
   /// </summary>
   class GrossFloorAreaCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_Area = 0;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static GrossFloorAreaCalculator s_Instance = new GrossFloorAreaCalculator();

      /// <summary>
      /// The GrossAreaCalculator instance.
      /// </summary>
      public static GrossFloorAreaCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates cross area.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="extrusionCreationData">
      /// The IFCExportBodyParams.
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
      public override bool Calculate(ExporterIFC exporterIFC, IFCExportBodyParams extrusionCreationData, Element element, ElementType elementType, EntryMap entryMap)
      {
         ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName, out m_Area, entryMap.CompatibleRevitParameterName, "IfcQtyGrossFloorArea");
         m_Area = UnitUtil.ScaleArea(m_Area);
         if (m_Area > MathUtil.Eps() * MathUtil.Eps())
            return true;

         m_Area = UnitUtil.ScaleArea(CalculateSpatialElementGrossFloorArea(element as SpatialElement));
         if (m_Area > MathUtil.Eps() * MathUtil.Eps())
            return true;

         if (extrusionCreationData == null)
            return false;

         m_Area = extrusionCreationData.ScaledArea;

         if (m_Area > MathUtil.Eps() * MathUtil.Eps())
            return true;

         return false;
      }

      private double CalculateSpatialElementGrossFloorArea(SpatialElement spatialElement)
      {
         double area = 0.0;

         if (spatialElement == null)
            return area;

         // Get the outer boundary loops of the SpatialElement.
         IList<IList<BoundarySegment>> boundaryLoops = spatialElement.GetBoundarySegments(new SpatialElementBoundaryOptions());

         //Search for a outer loop with the largest area.
         foreach (IList<BoundarySegment> boundaryLoop in boundaryLoops)
         {
            CurveLoop curveLoop = new CurveLoop();
            foreach (BoundarySegment boundarySegment in boundaryLoop)
            {
               try
               {
                  Curve curve = boundarySegment.GetCurve();
                  curveLoop.Append(curve);
               }
               catch (Autodesk.Revit.Exceptions.ArgumentException)
               {
                  //For some special cases, BoundarySegments of the element are not valid for CurveLoop creation
                  //(curveLoop.Append(curve) throws exception because "This curve will make the loop discontinuous.") 

                  return 0.0;
               }
            }

            double loopArea = ExporterIFCUtils.ComputeAreaOfCurveLoops(new List<CurveLoop>() { curveLoop });

            if (area < loopArea)
               area = loopArea;
         }

         return area;
      }

      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>
      /// The double value.
      /// </returns>
      public override double GetDoubleValue()
      {
         return m_Area;
      }
   }
}
