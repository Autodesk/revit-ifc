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
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB;
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   class SpaceTemperatureCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_Temperature = 0.0;

      private string m_Name;

      /// <summary>
      /// The SpaceTemperatureCalculator instance.
      /// </summary>
      public SpaceTemperatureCalculator(string name)
      {
         m_Name = name;
      }

      /// <summary>
      /// Calculates temperature value for a space.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="extrusionCreationData">The IFCExtrusionCreationData.</param>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="elementType">The element type.</param>
      /// <returns>True if the operation succeed, false otherwise.</returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element, ElementType elementType)
      {
         if (!string.IsNullOrEmpty(m_Name))
         {
            string temperatureName, temperatureNameMax, temperatureNameMin;
            if (string.Compare(m_Name, "SpaceTemperatureSummer") == 0)
            {
               temperatureName = "SpaceTemperatureSummer";
               temperatureNameMax = "SpaceTemperatureSummerMax";
               temperatureNameMin = "SpaceTemperatureSummerMin";
            }
            else if (string.Compare(m_Name, "SpaceTemperatureWinter") == 0)
            {
               temperatureName = "SpaceTemperatureWinter";
               temperatureNameMax = "SpaceTemperatureWinterMax";
               temperatureNameMin = "SpaceTemperatureWinterMin";
            }
            else
               return false;

            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, temperatureName, out m_Temperature) != null)
               return true;

            double maxValue = 0, minValue = 0;
            if ((ParameterUtil.GetDoubleValueFromElementOrSymbol(element, temperatureNameMax, out maxValue) != null) &&
                (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, temperatureNameMin, out minValue) != null))
            {
               m_Temperature = (maxValue + minValue) / 2.0;
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
         return m_Temperature;
      }
   }
}