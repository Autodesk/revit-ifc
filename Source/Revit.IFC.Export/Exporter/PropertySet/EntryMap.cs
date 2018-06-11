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
      string m_RevitParameterName = String.Empty;

      /// <summary>
      /// The parameter name to be used to get the parameter value in other locales.
      /// </summary>
      Dictionary<LanguageType, string> m_LocalizedRevitParameterNames = null;

      /// <summary>
      /// The built in parameter.
      /// </summary>
      BuiltInParameter m_RevitBuiltInParameter = BuiltInParameter.INVALID;

      /// <summary>
      /// The property calculator to calculate the property value.
      /// </summary>
      PropertyCalculator m_PropertyCalculator;

      /// <summary>
      /// Indicates if the property value is retrieved only from the calculator.
      /// </summary>
      bool m_UseCalculatorOnly = false;

      /// <summary>
      /// Calculated value indicates whether or not there is a valid parameter name associated with this entry.
      /// </summary>
      bool m_ParameterNameIsValid = false;

      public EntryMap() { }
      /// <summary>
      /// Constructor to create an Entry object.
      /// </summary>
      /// <param name="revitParameterName">
      /// The parameter name for this Entry.
      /// </param>
      public EntryMap(string revitParameterName)
      {
         this.m_RevitParameterName = revitParameterName;
      }
      public EntryMap(BuiltInParameter builtInParameter)
      {
         this.m_RevitBuiltInParameter = builtInParameter;
      }
      public EntryMap(PropertyCalculator calculator)
      {
         this.m_PropertyCalculator = calculator;
      }
      public EntryMap(string revitParameterName, BuiltInParameter builtInParameter)
      {
         this.m_RevitParameterName = revitParameterName;
         this.m_RevitBuiltInParameter = builtInParameter;
      }
      /// <summary>
      /// Updates caches to make use of this Entry faster after it is completed.
      /// </summary>
      public void UpdateEntry()
      {
         m_ParameterNameIsValid = (!UseCalculatorOnly && (!String.IsNullOrEmpty(RevitParameterName) || (RevitBuiltInParameter != BuiltInParameter.INVALID)));
      }

      /// <summary>
      /// Returns whether the parameter has a usable (valid) name.
      /// </summary>
      public bool ParameterNameIsValid
      {
         get { return m_ParameterNameIsValid; }
      }

      /// <summary>
      /// The standard name of the parameter in Revit (if it exists).
      /// </summary>
      public string RevitParameterName
      {
         get
         {
            return m_RevitParameterName;
         }
         set
         {
            m_RevitParameterName = value;
         }
      }

      /// <summary>
      /// The localized name of the parameter in Revit (if it exists).
      /// </summary>
      /// <param name="locale">The language.</param>
      /// <returns>The localized name, or null if it does not exist.</returns>
      public string LocalizedRevitParameterName(LanguageType locale)
      {
         string localizedName = null;
         if (m_LocalizedRevitParameterNames != null)
         {
            if (m_LocalizedRevitParameterNames.TryGetValue(locale, out localizedName))
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
         if (m_LocalizedRevitParameterNames == null)
            m_LocalizedRevitParameterNames = new Dictionary<LanguageType, string>();

         if (m_LocalizedRevitParameterNames.ContainsKey(locale))
            throw new ArgumentException("Locale value already defined.");
         m_LocalizedRevitParameterNames[locale] = localizedName;
      }

      /// <summary>
      /// The built-in parameter.
      /// </summary>
      public BuiltInParameter RevitBuiltInParameter
      {
         get
         {
            return m_RevitBuiltInParameter;
         }
         set
         {
            m_RevitBuiltInParameter = value;
         }
      }

      /// <summary>
      /// The instance of a class that can calculate the value of the property or quantity.
      /// </summary>
      public PropertyCalculator PropertyCalculator
      {
         get
         {
            return m_PropertyCalculator;
         }
         set
         {
            m_PropertyCalculator = value;
         }
      }

      /// <summary>
      /// Indicates if the property value is retrieved only from the calculator.
      /// </summary>
      public bool UseCalculatorOnly
      {
         get
         {
            return m_UseCalculatorOnly;
         }
         set
         {
            m_UseCalculatorOnly = value;
         }
      }
   }
}