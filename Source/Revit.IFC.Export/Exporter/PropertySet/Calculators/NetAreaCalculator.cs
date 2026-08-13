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
   /// A calculation class to calculate gross area.
   /// </summary>
   class NetAreaCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_Area = 0;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static NetAreaCalculator s_Instance = new NetAreaCalculator();

      /// <summary>
      /// The GrossAreaCalculator instance.
      /// </summary>
      public static NetAreaCalculator Instance
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
      public override bool Calculate(ExporterIFC exporterIFC, IFCAnyHandle handle, IFCExportBodyParams extrusionCreationData, Element element, ElementType elementType, EntryMap entryMap)
      {
         (_, m_Area) = ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName, entryMap.CompatibleRevitParameterName,
            "IfcQtyNetArea");
         if (m_Area > MathUtil.Eps * MathUtil.Eps)
         {
            m_Area = UnitUtil.ScaleArea(m_Area);
            return true;
         }

         // extrusionCreationData.ScaledArea is computed from actual geometry, so it is
         // reliable for all element types including non-in-place FamilyInstances.
         // This intentionally runs before the FamilyInstance guard below, which only
         // applies to the HOST_AREA_COMPUTED built-in parameter fallback.
         if (extrusionCreationData != null)
         {
            m_Area = extrusionCreationData.ScaledArea;
            if (m_Area > MathUtil.Eps * MathUtil.Eps)
               return true;
         }

         // HOST_AREA_COMPUTED is incorrect in case of not in-place families.
         // Do not export 'Net Area' quantity.
         if (element is FamilyInstance familyInstance && !IsInPlace(familyInstance))
            return false;

         (_, m_Area) = ParameterUtil.GetDoubleValueFromElementOrSymbol(element, BuiltInParameter.HOST_AREA_COMPUTED);
         if (m_Area > MathUtil.Eps * MathUtil.Eps)
         {
            m_Area = UnitUtil.ScaleArea(m_Area);
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
         return m_Area;
      }

      private bool IsInPlace(FamilyInstance familyInstance)
      {
         return familyInstance?.Symbol?.Family?.IsInPlace ?? false;
      }
   }
}
