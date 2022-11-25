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
         // Check the override first from "IfcSpan" parameter, if not overriden use the geometry data from extrusion
         double spanVal;
         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName, out spanVal, entryMap.CompatibleRevitParameterName) == null)
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

         // No support, do nothing. Leave the Span to be the length of the entire beam
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
