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
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate the lining thickness of a window or door.
   /// </summary>
   class LiningThicknessCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_LiningThickness = 0.0;

      /// <summary>
      /// The LiningThicknessCalculator instance.
      /// </summary>
      public static LiningThicknessCalculator Instance { get; } = new LiningThicknessCalculator();

      /// <summary>
      /// Stores the last calculated lining thickness.
      /// </summary>
      /// <remarks>Many other properties depend on lining thickness, so we store it here to avoid redundant calculations.</remarks>
      private (int, double) LastCalculatedLiningThickness { get; set; } = (0, 0.0);

      /// <summary>
      /// Calculates the lining thickness.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="handle">The created IFC handle.</param>
      /// <param name="extrusionCreationData">Extra quantity information calculated when creating the instance.</param>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="elementType">The element type.</param>
      /// <param name="psetOrQtoEntryMap">The corresponding property set or quantity set entry map.</param>
      /// <returns>True if the operation succeed, false otherwise.</returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCAnyHandle handle, IFCExportBodyParams extrusionCreationData, Element element, ElementType elementType, EntryMap entryMap)
      {
         // We have already calculated the value; don't try again.
         int hndId = handle.Id;
         if (LastCalculatedLiningThickness.Item1 == hndId)
            return m_LiningThickness > MathUtil.Eps;

         // We maintain compatibility with old parameter names.  Newer names take precedence.
         string typeName = handle.TypeName;
         string baseTypeName = typeName.Substring(3);
         
         const string parameterNameV1 = "LiningThickness";
         string parameterNameV2 = typeName + "LiningProperties.LiningThickness";
         string parameterNameV3 = "Pset_" + baseTypeName + "LiningProperties.LiningThickness";

         double? value = ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(element, parameterNameV3, parameterNameV2, parameterNameV1);
         m_LiningThickness = value.HasValue ? UnitUtil.ScaleLength(value.Value) : 0.0;
         LastCalculatedLiningThickness = (hndId, m_LiningThickness);

         return value.HasValue;
      }

      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>
      /// The double value.
      /// </returns>
      public override double GetDoubleValue()
      {
         return m_LiningThickness;
      }
   }
}
