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
   /// A calculation class to calculate Fire Rating value for an element.
   /// </summary>
   class FireRatingCalculator : PropertyCalculator
   {
      /// <summary>
      /// A string variable to keep the calculated value.
      /// </summary>
      string m_FireRating = String.Empty;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static FireRatingCalculator s_Instance = new FireRatingCalculator();

      /// <summary>
      /// The FireRatingCalculator instance.
      /// </summary>
      public static FireRatingCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates the reference.
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
         if (ParameterUtil.GetStringValueFromElementOrSymbol(element, entryMap.RevitParameterName, out m_FireRating, entryMap.CompatibleRevitParameterName, "Fire Rating") != null
            && !string.IsNullOrEmpty(m_FireRating))
            return true;

         return false;
      }

      /// <summary>
      /// Gets the calculated string value.
      /// </summary>
      /// <returns>
      /// The string value.
      /// </returns>
      public override string GetStringValue()
      {
         return m_FireRating;
      }
   }
}

