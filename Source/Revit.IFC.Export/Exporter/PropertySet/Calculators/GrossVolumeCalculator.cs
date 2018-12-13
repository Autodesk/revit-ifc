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
      /// A static instance of this class.
      /// </summary>
      static GrossVolumeCalculator s_Instance = new GrossVolumeCalculator();

      /// <summary>
      /// The SlabGrossVolumeCalculator instance.
      /// </summary>
      public static GrossVolumeCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates gross volume.
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
         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, "IfcQtyGrossVolume", out m_Volume) == null)
               ParameterUtil.GetDoubleValueFromElementOrSymbol(element, "GrossVolume", out m_Volume);
         m_Volume = UnitUtil.ScaleVolume(m_Volume);
         if (m_Volume > MathUtil.Eps() * MathUtil.Eps() * MathUtil.Eps())
            return true;

         if (extrusionCreationData == null)
            return false;

         double area = extrusionCreationData.ScaledArea;
         double length = extrusionCreationData.ScaledLength;
         m_Volume = area * length;
         if (m_Volume > MathUtil.Eps() * MathUtil.Eps() * MathUtil.Eps())
            return true;

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
         return m_Volume;
      }
   }
}