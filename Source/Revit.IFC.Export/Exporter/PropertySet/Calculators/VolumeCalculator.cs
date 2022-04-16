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
   /// A calculation class to calculate volume for a space.
   /// </summary>
   class VolumeCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_Volume = 0;

      /// <summary>
      /// The SpaceVolumeCalculator instance.
      /// </summary>
      public static VolumeCalculator Instance { get; } = new VolumeCalculator();

      /// <summary>
      /// Calculates volume for a space.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="calcValues">The IFCExtrusionCreationData.</param>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="elementType">The element type.</param>
      /// <returns>True if the operation succeed, false otherwise.</returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element, ElementType elementType, EntryMap entryMap)
      {
         double volumeEps = MathUtil.Eps() * MathUtil.Eps() * MathUtil.Eps();
         if ((ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName, out m_Volume) != null)
               || (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.CompatibleRevitParameterName, out m_Volume) != null)
               || (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, "IfcQtyVolume", out m_Volume) != null))
         {
            m_Volume = UnitUtil.ScaleArea(m_Volume);
            if (m_Volume > volumeEps)
               return true;
         }

         if (extrusionCreationData == null)
            return false;

         double area = UnitUtil.UnscaleArea(extrusionCreationData.ScaledArea);
         double height = UnitUtil.UnscaleLength(extrusionCreationData.ScaledHeight);
         m_Volume = UnitUtil.ScaleVolume(area * height);
         return (m_Volume > volumeEps);
      }

      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>The double value.</returns>
      public override double GetDoubleValue()
      {
         return m_Volume;
      }
   }
}
