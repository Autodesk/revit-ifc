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
   /// A calculation class to calculate the casing depth of a door.
   /// </summary>
   class CasingDepthCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_CasingDepth = 0.0;

      /// <summary>
      /// The CasingDepthCalculator instance.
      /// </summary>
      public static CasingDepthCalculator Instance { get; } = new CasingDepthCalculator();

      /// <summary>
      /// Calculates the casing depth.
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
         const string parameterNameV1 = "CasingDepth";
         const string parameterNameV2 = "IfcDoorLiningProperties.CasingDepth";
         const string parameterNameV3 = "Pset_DoorLiningProperties.CasingDepth";

         // The value has to exist and be positive.
         if (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(element, parameterNameV3, parameterNameV2, parameterNameV1) is double value)
         {
            m_CasingDepth = UnitUtil.ScaleLength(value);
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
         return m_CasingDepth;
      }
   }
}
