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

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using static Revit.IFC.Export.Utility.ParameterUtil;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate slope value.
   /// </summary>
   class CounterSlopeCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double? m_CounterSlope = null;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static CounterSlopeCalculator s_Instance = new CounterSlopeCalculator();

      /// <summary>
      /// The CounterSlopeCalculator instance.
      /// </summary>
      public static CounterSlopeCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates slope value.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="extrusionCreationData">The IFCExportBodyParams.</param>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="elementType">The element type.</param>
      /// <returns>True if the operation succeed, false otherwise.</returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCExportBodyParams extrusionCreationData, Element element, ElementType elementType, EntryMap entryMap)
      {
         if(extrusionCreationData == null)
            return false;

         double slope = extrusionCreationData.Slope;

         if (!MathUtil.IsAlmostZero(slope))
         { 
            m_CounterSlope = UnitUtil.ScaleAngle(Math.PI / 2.0) - slope;
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
         if (m_CounterSlope.HasValue)
            return m_CounterSlope.Value;
         else
            return 0.0;
      }
   }
}
