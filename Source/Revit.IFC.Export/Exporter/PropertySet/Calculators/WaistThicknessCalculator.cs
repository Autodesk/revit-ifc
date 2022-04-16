﻿//
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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate waist thickness parameters.
   /// </summary>
   class WaistThicknessCalculator : PropertyCalculator
   {
      /// <summary>
      /// An int variable to keep the calculated WaistThickness value.
      /// </summary>
      private double m_WaistThickness = 0.0;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static WaistThicknessCalculator s_Instance = new WaistThicknessCalculator();

      /// <summary>
      /// The WaistThicknessCalculator instance.
      /// </summary>
      public static WaistThicknessCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates number of risers for a stair.
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
      public override bool Calculate(ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element, ElementType elementType, EntryMap entryMap)
      {
         bool valid = true;

         // Get override from parameter
         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName, out m_WaistThickness) != null)
            if (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.CompatibleRevitParameterName, out m_WaistThickness) != null)
               m_WaistThickness = UnitUtil.ScaleArea(m_WaistThickness);

         return valid;
      }

      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>
      /// The double value.
      /// </returns>
      public override double GetDoubleValue()
      {
         return m_WaistThickness;
      }
   }
}
