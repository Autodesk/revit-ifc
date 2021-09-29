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

using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

namespace Revit.IFC.Export.Exporter.PropertySet
{
   /// <summary>
   /// Represents a mapping from a Revit parameter or calculated value to an IFC property or quantity.
   /// </summary>
   /// <remarks>
   /// Symbol property is true if the property comes from the symbol (vs. the element itself).  Default is TRUE.
   /// Revit parameter type defaults to RPTString.
   ///
   /// One of the following:
   /// <list type="bullet">
   /// <item>Revit parameter name</item>
   /// <item>Revit built-in parameter</item>
   /// <item>Calculator</item>
   /// </list>
   /// must be set. If more than one is valid,
   /// generally, parameter name is used first, followed by parameter id,
   /// then by function.
   /// </remarks>
   abstract public class Entry<T> where T : EntryMap, new()
   {
      /// <summary>
      /// The name of the property or quantity as stored in the IFC export.
      /// </summary>
      /// <remarks>
      /// Default is empty; if empty the name of the Revit parameter will be used.
      /// </remarks>
      public string PropertyName { get; set; } = string.Empty;

      /// <summary>
      /// True if the property comes from the element's type (vs. the element itself).
      /// </summary>
      /// <remarks>
      /// The default value is true.
      /// </remarks>
      public bool IsElementTypeProperty { get; set; } = true;

      protected List<T> Entries { get; set; } = new List<T>();

      /// <summary>
      /// Constructor to create an Entry object.
      /// </summary>
      /// <param name="revitParameterName">
      /// The parameter name for this Entry.
      /// </param>
      public Entry(string revitParameterName)
      {
         Entries.Add(new T() { RevitParameterName = revitParameterName });
         PropertyName = revitParameterName;
      }

      public Entry(string propertyName, T entry)
      {
         Entries.Add(entry);
         PropertyName = propertyName;
      }

      public Entry(string propertyName, IEnumerable<T> entries)
      {
         Entries.AddRange(entries);
         PropertyName = propertyName;
      }

      public PropertyCalculator PropertyCalculator
      {
         set
         {
            if (Entries.Count > 0)
               Entries[0].PropertyCalculator = value;
         }
      }
      public void AddLocalizedParameterName(LanguageType locale, string localizedName)
      {
         if (Entries.Count > 0)
            Entries[0].AddLocalizedParameterName(locale, localizedName);
      }

      public void AddEntry(T entry)
      {
         Entries.Add(entry);
      }

      public void UpdateEntry()
      {
         foreach (T entry in Entries)
            entry.UpdateEntry();
      }
      public void SetRevitParameterName(string revitParameterName)
      {
         if (Entries.Count == 0)
            Entries.Add(new T() { RevitParameterName = revitParameterName });
         else
            Entries[0].RevitParameterName = revitParameterName;
      }

      public void SetRevitBuiltInParameter(BuiltInParameter builtInParameter)
      {
         if (Entries.Count == 0)
            Entries.Add(new T() { RevitBuiltInParameter = builtInParameter });
         else
            Entries[0].RevitBuiltInParameter = builtInParameter;
      }
   }
}