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
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// Get Builtin Parameter for Roll property
   /// </summary>
   class RollCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_Roll = 0;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static RollCalculator s_Instance = new RollCalculator();

      /// <summary>
      /// The StructuralMemberRollCalculator instance.
      /// </summary>
      public static RollCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates Roll value (Rotation angle) for a beam, column and member
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
         Parameter rollPar = element.get_Parameter(BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE);
         if (rollPar != null)
         {
            double? roll = rollPar.AsDouble();
            if (roll != null && roll.HasValue)
            {
               m_Roll = UnitUtil.ScaleAngle(roll.Value);
               return true;
            }
         }

         // For other elements with ExtrusionData. Parameter will take precedence (override)
         ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName, out m_Roll, entryMap.CompatibleRevitParameterName);
         m_Roll = UnitUtil.ScaleAngle(m_Roll);
         if (m_Roll > MathUtil.Eps())
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
         return m_Roll;
      }
   }
}
