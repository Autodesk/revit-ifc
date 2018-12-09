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
   /// A calculation class to calculate area for a Door.
   /// </summary>
   class AreaCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_Area = 0;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static AreaCalculator s_Instance = new AreaCalculator();

      /// <summary>
      /// The DoorAreaCalculator instance.
      /// </summary>
      public static AreaCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates area for a Door.
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
         double height = 0.0;
         double width = 0.0;

         // Work for Door element
         if (CategoryUtil.GetSafeCategoryId(element) == new ElementId(BuiltInCategory.OST_Doors))
            if ((ParameterUtil.GetDoubleValueFromElementOrSymbol(element, BuiltInParameter.DOOR_HEIGHT, out height) != null) &&
                  (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, BuiltInParameter.DOOR_WIDTH, out width) != null))
            {
                  m_Area = UnitUtil.ScaleArea(height * width);
                  return true;
            }

         // Work for Window element
         if (CategoryUtil.GetSafeCategoryId(element) == new ElementId(BuiltInCategory.OST_Windows))
            if ((ParameterUtil.GetDoubleValueFromElementOrSymbol(element, BuiltInParameter.WINDOW_HEIGHT, out height) != null) &&
                  (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, BuiltInParameter.WINDOW_WIDTH, out width) != null))
            {
               m_Area = UnitUtil.ScaleArea(height * width);
               return true;
            }

         // If no value from the above, consider the parameter override
         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, "IfcQtyArea", out m_Area) == null)
               ParameterUtil.GetDoubleValueFromElementOrSymbol(element, "Area", out m_Area);
         m_Area = UnitUtil.ScaleArea(m_Area);
         if (m_Area > MathUtil.Eps() * MathUtil.Eps())
            return true;

         // Work for Space element or other element that has extrusion
         if (extrusionCreationData == null)
            return false;

         m_Area = extrusionCreationData.ScaledArea;
         return m_Area > MathUtil.Eps() * MathUtil.Eps();
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
