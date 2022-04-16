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
   class SpaceTemperatureSummerCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_SpaceTemperatureSummer = 0.0;
      
      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static SpaceTemperatureSummerCalculator s_Instance = new SpaceTemperatureSummerCalculator();

      /// <summary>
      /// The SpaceTemperatureSummerCalculator instance.
      /// </summary>
      public static SpaceTemperatureSummerCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates temperature value for a space.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="extrusionCreationData">The IFCExtrusionCreationData.</param>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="elementType">The element type.</param>
      /// <returns>True if the operation succeed, false otherwise.</returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element, ElementType elementType, EntryMap entryMap)
      {
         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName, out m_SpaceTemperatureSummer) != null
            || ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.CompatibleRevitParameterName, out m_SpaceTemperatureSummer) != null)
            return true;

         double maxValue = 0, minValue = 0;
         if ((ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName + "Max", out maxValue) != null) &&
            (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName + "Min", out minValue) != null)
            || (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.CompatibleRevitParameterName + "Max", out maxValue) != null) &&
            (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.CompatibleRevitParameterName + "Min", out minValue) != null))
         {
            m_SpaceTemperatureSummer = (maxValue + minValue) / 2.0;
            return true;
         }

         return false;
      }

      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>The double value.</returns>
      public override double GetDoubleValue()
      {
         return m_SpaceTemperatureSummer;
      }
   }

   class SpaceTemperatureWinterCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_SpaceTemperatureWinter = 0.0;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static SpaceTemperatureWinterCalculator s_Instance = new SpaceTemperatureWinterCalculator();

      /// <summary>
      /// The SpaceTemperatureWinterCalculator instance.
      /// </summary>
      public static SpaceTemperatureWinterCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates temperature value for a space.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="extrusionCreationData">The IFCExtrusionCreationData.</param>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="elementType">The element type.</param>
      /// <returns>True if the operation succeed, false otherwise.</returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element, ElementType elementType, EntryMap entryMap)
      {
         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName, out m_SpaceTemperatureWinter) != null
            || ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.CompatibleRevitParameterName, out m_SpaceTemperatureWinter) != null)
            return true;

         double maxValue = 0, minValue = 0;
         if (((ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName + "Max", out maxValue) != null) &&
            (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName + "Min", out minValue) != null))
            || ((ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.CompatibleRevitParameterName + "Max", out maxValue) != null) &&
            (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.CompatibleRevitParameterName + "Min", out minValue) != null)))
         {
            m_SpaceTemperatureWinter = (maxValue + minValue) / 2.0;
            return true;
         }

         return false;
      }

      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>The double value.</returns>
      public override double GetDoubleValue()
      {
         return m_SpaceTemperatureWinter;
      }
   }
}
