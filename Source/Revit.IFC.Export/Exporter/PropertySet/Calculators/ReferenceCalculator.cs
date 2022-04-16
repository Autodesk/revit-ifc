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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to find the reference.
   /// </summary>
   class ReferenceCalculator : PropertyCalculator
   {
      /// <summary>
      /// A string variable to keep the calculated value.
      /// </summary>
      string m_ReferenceName = string.Empty;

      /// <summary>
      /// The ReferenceCalculator instance.
      /// </summary>
      public static ReferenceCalculator Instance { get; } = new ReferenceCalculator();

      /// <summary>
      /// Calculates the reference.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="extrusionCreationData">The IFCExtrusionCreationData.</param>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="elementType">The element type.</param>
      /// <returns>True if the operation succeed, false otherwise.</returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element, ElementType elementType, EntryMap entryMap)
      {
         if (ParameterUtil.GetStringValueFromElementOrSymbol(element, entryMap.RevitParameterName, out m_ReferenceName) == null)
            ParameterUtil.GetStringValueFromElementOrSymbol(element, entryMap.CompatibleRevitParameterName, out m_ReferenceName);
         if (!string.IsNullOrEmpty(m_ReferenceName))
            return true;

         // If the element is an element type, "Reference" doesn't mean much, since IFC doesn't
         // care about the type of a type.  We'll keep the override in place in case someone wants
         // to force it, but otherwise will not export the value.
         if (element is ElementType)
            return false;

         if (elementType == null)
            m_ReferenceName = element.Name;
         else
         {
            string elementTypeName = elementType.Name;
            if (ExporterCacheManager.ExportOptionsCache.NamingOptions.UseFamilyAndTypeNameForReference ||
               string.IsNullOrEmpty(elementTypeName))
            {
               if (!String.IsNullOrEmpty(elementTypeName))
                  m_ReferenceName = String.Format("{0}:{1}", elementType.FamilyName, elementTypeName);
               else
                  m_ReferenceName = elementType.FamilyName;
            }
            else
               m_ReferenceName = elementTypeName;
         }
         return true;
      }

      /// <summary>
      /// Gets the calculated string value.
      /// </summary>
      /// <returns>
      /// The string value.
      /// </returns>
      public override string GetStringValue()
      {
         return m_ReferenceName;
      }
   }

}
