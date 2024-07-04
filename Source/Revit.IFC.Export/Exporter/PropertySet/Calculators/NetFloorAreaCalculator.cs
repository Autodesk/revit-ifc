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
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate gross area.
   /// </summary>
   class NetFloorAreaCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_Area = 0;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static NetFloorAreaCalculator s_Instance = new NetFloorAreaCalculator();

      /// <summary>
      /// The GrossAreaCalculator instance.
      /// </summary>
      public static NetFloorAreaCalculator Instance
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
         ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName, out m_Area, entryMap.CompatibleRevitParameterName, "IfcQtyNetFloorArea");
         m_Area = UnitUtil.ScaleArea(m_Area);
         if (m_Area > MathUtil.Eps() * MathUtil.Eps())
            return true;

         m_Area = UnitUtil.ScaleArea(CalculateSpatialElementNetFloorArea(element as SpatialElement));
         if (m_Area > MathUtil.Eps() * MathUtil.Eps())
            return true;

         ElementId categoryId = CategoryUtil.GetSafeCategoryId(element);
         IFCAnyHandle hnd = ExporterCacheManager.ElementToHandleCache.Find(element.Id);
         if (element is SpatialElement
            || categoryId == new ElementId(BuiltInCategory.OST_Rooms) || categoryId == new ElementId(BuiltInCategory.OST_MEPSpaces)
            || categoryId == new ElementId(BuiltInCategory.OST_Areas)
            || IFCAnyHandleUtil.IsSubTypeOf(hnd, IFCEntityType.IfcSpace))
         {
            SpatialElement sp = element as SpatialElement;
            if (sp != null)
               m_Area = sp.Area;
         }
         else
         {
            ParameterUtil.GetDoubleValueFromElementOrSymbol(element, BuiltInParameter.HOST_AREA_COMPUTED, out m_Area);
         }

         m_Area = UnitUtil.ScaleArea(m_Area);
         if (m_Area > MathUtil.Eps() * MathUtil.Eps())
            return true;

         return false;
      }

      private double CalculateSpatialElementNetFloorArea(SpatialElement spatialElement)
      {
         double netFloorArea = 0.0;

         if (spatialElement == null)
            return netFloorArea;

         // Get the boundary loops of the SpatialElement
         IList<IList<BoundarySegment>> boundaryLoops = spatialElement.GetBoundarySegments(new SpatialElementBoundaryOptions());

         double outerLoopArea = 0.0;
         double loopsArea = 0.0;
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

            if (outerLoopArea < loopArea)
               outerLoopArea = loopArea;

            loopsArea += loopArea;
         }

         //To define the net area, we need to subtract the area of the holes from the area of the outerLoopArea.
         //loopsArea is the sum of the areas of all the loops.
         double innerLoopsArea = loopsArea - outerLoopArea;
         netFloorArea = outerLoopArea - innerLoopsArea;

         return netFloorArea;
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
