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

using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate gross volume.
   /// </summary>
   class GrossVolumeCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_Volume = 0;

      /// <summary>
      /// The SlabGrossVolumeCalculator instance.
      /// </summary>
      public static GrossVolumeCalculator Instance { get; } = new GrossVolumeCalculator();

      /// <summary>
      /// Calculates gross volume.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="extrusionCreationData">The IFCExportBodyParams.</param>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="elementType">The element type.</param>
      /// <returns>True if the operation succeeded, false otherwise.</returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCExportBodyParams extrusionCreationData, Element element, ElementType elementType, EntryMap entryMap)
      {
         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName, out m_Volume, entryMap.CompatibleRevitParameterName, "IfcQtyGrossVolume") != null)
         {
            m_Volume = UnitUtil.ScaleVolume(m_Volume);
            if (m_Volume > MathUtil.Eps() * MathUtil.Eps() * MathUtil.Eps())
               return true;
         }

         m_Volume = UnitUtil.ScaleVolume(CalculateSpatialElementGrossVolume(element as SpatialElement, extrusionCreationData));
         if (m_Volume > MathUtil.Eps() * MathUtil.Eps() * MathUtil.Eps())
            return true;

         if (extrusionCreationData == null)
            return false;

         // While it would be unlikely that area and volume have different base length units,
         // it is still safer to unscale and rescale the results.  For length, it is somewhat
         // common to have mm as the length unit and m^3 as the volume unit.
         double area = UnitUtil.UnscaleArea(extrusionCreationData.ScaledArea);
         double length = UnitUtil.UnscaleLength(extrusionCreationData.ScaledLength);
         m_Volume = UnitUtil.ScaleVolume(area * length);
         return (m_Volume > MathUtil.Eps() * MathUtil.Eps() * MathUtil.Eps());
      }

      private double CalculateSpatialElementGrossVolume(SpatialElement spatialElement, IFCExportBodyParams extrusionCreationData)
      {
         double area = 0.0;

         if (spatialElement == null || extrusionCreationData == null)
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

         double length = UnitUtil.UnscaleLength(extrusionCreationData.ScaledLength);

         return area * length;
      }

      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>The calculated volume.</returns>
      public override double GetDoubleValue()
      {
         return m_Volume;
      }
   }
}