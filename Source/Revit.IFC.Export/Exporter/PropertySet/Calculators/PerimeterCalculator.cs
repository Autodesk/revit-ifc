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
using Revit.IFC.Export.Exporter.PropertySet.IFC2X2;
using Revit.IFC.Export.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate perimeter for a host object.
   /// </summary>
   class PerimeterCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_Perimeter = 0;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static PerimeterCalculator s_Instance = new PerimeterCalculator();

      /// <summary>
      /// The PerimeterCalculator instance.
      /// </summary>
      public static PerimeterCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates perimeter for a space or floor.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="extrusionCreationData">The IFCExportBodyParams.</param>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="elementType">The element type.</param>
      /// <returns>True if the operation succeed, false otherwise.</returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCAnyHandle handle, IFCExportBodyParams extrusionCreationData, 
         Element element, ElementType elementType, EntryMap entryMap)
      {
         (_, m_Perimeter) = ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName, 
            entryMap.CompatibleRevitParameterName, "IfcQtyPerimeter");
         if (m_Perimeter > MathUtil.Eps)
         {
            m_Perimeter = UnitUtil.ScaleLength(m_Perimeter);
            return true;
         }

         (_, m_Perimeter) = ParameterUtil.GetDoubleValueFromElement(element.Id, BuiltInParameter.HOST_PERIMETER_COMPUTED);
         if (m_Perimeter > MathUtil.Eps)
         {
            m_Perimeter = UnitUtil.ScaleLength(m_Perimeter);
            return true;
         }

         m_Perimeter = extrusionCreationData?.ScaledOuterPerimeter ?? 0.0;
         return m_Perimeter > MathUtil.Eps;
      }

      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>
      /// The double value.
      /// </returns>
      public override double GetDoubleValue()
      {
         return m_Perimeter;
      }
   }

   /// <summary>
   /// A calculation class to calculate perimeter for a space.
   /// </summary>
   class GrossPerimeterCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_GrossPerimeter = 0;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static GrossPerimeterCalculator s_Instance = new();

      /// <summary>
      /// The GrossPerimeterCalculator instance.
      /// </summary>
      public static GrossPerimeterCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates perimeter for a space or floor.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="extrusionCreationData">The IFCExportBodyParams.</param>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="elementType">The element type.</param>
      /// <returns>True if the operation succeed, false otherwise.</returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCAnyHandle handle, IFCExportBodyParams extrusionCreationData, Element element, ElementType elementType, EntryMap entryMap)
      {
         (_, m_GrossPerimeter) = ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName,
            entryMap.CompatibleRevitParameterName, "IfcQtyGrossPerimeter");

         if (m_GrossPerimeter > MathUtil.Eps)
         {
            m_GrossPerimeter = UnitUtil.ScaleLength(m_GrossPerimeter);
            return true;
         }

         m_GrossPerimeter = extrusionCreationData?.ScaledOuterPerimeter ?? 0.0;
         return m_GrossPerimeter > MathUtil.Eps;
      }

      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>
      /// The double value.
      /// </returns>
      public override double GetDoubleValue()
      {
         return m_GrossPerimeter;
      }
   }
}
