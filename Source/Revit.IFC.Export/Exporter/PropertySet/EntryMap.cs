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
using System.Collections.ObjectModel;
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;

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
   abstract public class EntryMap
   {
      /// <summary>
      /// The parameter name to be used to get the parameter value.  This is generally in English (ENU).
      /// </summary>
      public string RevitParameterName { get; set; } = string.Empty;

      /// <summary>
      /// The name kept for backward compatibility when the revit parameter name uses the old format, which is equal to the property name 
      /// </summary>
      public string CompatibleRevitParameterName { get; set; } = string.Empty;

      /// <summary>
      /// The parameter name to be used to get the parameter value in other locales.
      /// </summary>
      public Dictionary<LanguageType, string> LocalizedRevitParameterNames { get; set; } = null;

      /// <summary>
      /// The built in parameter.
      /// </summary>
      public BuiltInParameter RevitBuiltInParameter { get; set; } = BuiltInParameter.INVALID;

      /// <summary>
      /// The property calculator to calculate the property value.
      /// </summary>
      public PropertyCalculator PropertyCalculator { get; set; }

      /// <summary>
      /// Indicates if the property value is retrieved only from the calculator.
      /// </summary>
      public bool UseCalculatorOnly { get; set; }  = false;

      /// <summary>
      /// Calculated value indicates whether or not there is a valid parameter name associated with this entry.
      /// </summary>
      public bool ParameterNameIsValid { get; private set; } = false;

      public EntryMap() { }

      /// <summary>
      /// Constructor to create an Entry object.
      /// </summary>
      /// <param name="revitParameterName">
      /// The parameter name for this Entry.
      /// </param>
      public EntryMap(string revitParameterName, string compatibleParamName)
      {
         RevitParameterName = revitParameterName;
         CompatibleRevitParameterName = compatibleParamName;
      }

      public EntryMap(BuiltInParameter builtInParameter)
      {
         RevitBuiltInParameter = builtInParameter;
      }

      public EntryMap(PropertyCalculator calculator)
      {
         PropertyCalculator = calculator;
      }

      public EntryMap(string revitParameterName, BuiltInParameter builtInParameter)
      {
         RevitParameterName = revitParameterName;
         RevitBuiltInParameter = builtInParameter;
      }

      /// <summary>
      /// Updates caches to make use of this Entry faster after it is completed.
      /// </summary>
      public void UpdateEntry()
      {
         ParameterNameIsValid = (!UseCalculatorOnly && (!string.IsNullOrEmpty(RevitParameterName) || (RevitBuiltInParameter != BuiltInParameter.INVALID)));
      }

      /// <summary>
      /// The localized name of the parameter in Revit (if it exists).
      /// </summary>
      /// <param name="locale">The language.</param>
      /// <returns>The localized name, or null if it does not exist.</returns>
      public string LocalizedRevitParameterName(LanguageType locale)
      {
         if (LocalizedRevitParameterNames != null)
         {
            if (LocalizedRevitParameterNames.TryGetValue(locale, out string localizedName))
               return localizedName;
         }
         return null;
      }

      /// <summary>
      /// Adds a localized name for the entry.
      /// </summary>
      /// <param name="locale">The language.</param>
      /// <param name="localizedName">The name for that language.</param>
      public void AddLocalizedParameterName(LanguageType locale, string localizedName)
      {
         if (LocalizedRevitParameterNames == null)
            LocalizedRevitParameterNames = new Dictionary<LanguageType, string>();

         if (LocalizedRevitParameterNames.ContainsKey(locale))
            throw new ArgumentException("Locale value already defined.");
         LocalizedRevitParameterNames[locale] = localizedName;
      }
   }
}