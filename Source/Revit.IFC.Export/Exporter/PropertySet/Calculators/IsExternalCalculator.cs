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

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate external value for an element.
   /// </summary>
   class IsExternalCalculator : PropertyCalculator
   {
      /// <summary>
      /// A boolean variable to keep the calculated value.
      /// </summary>
      private bool IsExternal { get; set; } = false;

      /// <summary>
      /// The IsExternalCalculator instance.
      /// </summary>
      public static IsExternalCalculator Instance { get; } = new IsExternalCalculator();

      /// <summary>
      /// Calculates external value for an element.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="extrusionCreationData">The IFCExportBodyParams.</param>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="elementType">The element type.</param>
      /// <returns>True if the operation succeed, false otherwise.</returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCExportBodyParams extrusionCreationData, Element element, ElementType elementType, EntryMap entryMap)
      {
         IsExternal = CategoryUtil.IsElementExternal(element);
         return true;
      }

      /// <summary>
      /// Gets the calculated boolean value.
      /// </summary>
      /// <returns>The boolean value.</returns>
      public override bool GetBooleanValue()
      {
         return IsExternal;
      }
   }
}