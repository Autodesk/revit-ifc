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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;

namespace Revit.IFC.Export.Exporter.PropertySet
{
   /// <summary>
   /// Represents a mapping from a Revit parameter to an IFC predefined property.
   /// </summary>
   public class PreDefinedPropertySetEntry : Entry<PreDefinedPropertySetEntryMap>
   {
      /// <summary>
      /// The type of the IFC predefined property set entry. Default is label.
      /// </summary>
      public PropertyType PropertyType { get; set; } = PropertyType.Label;

      /// <summary>
      /// The value type of the IFC predefined property set entry.
      /// </summary>
      public PropertyValueType PropertyValueType { get; set; } = PropertyValueType.SingleValue;

      /// <summary>
      /// The type of the Enum that will validate the value for an enumeration.
      /// </summary>
      public Type PropertyEnumerationType { get; set; } = null;
      
      /// <summary>
      /// Constructs a PreDefinedPropertySetEntry object.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      public PreDefinedPropertySetEntry(string revitParameterName, string compatibleParamName = null)
          : base(revitParameterName, compatibleParamName)
      {
      }

      public PreDefinedPropertySetEntry(PropertyType propertyType, string revitParameterName)
          : base(revitParameterName)
      {
         PropertyType = propertyType;
      }
      public PreDefinedPropertySetEntry(PropertyType propertyType, string propertyName, BuiltInParameter builtInParameter)
             : base(propertyName, new PreDefinedPropertySetEntryMap(propertyName) { RevitBuiltInParameter = builtInParameter })
      {
         PropertyType = propertyType;
      }
      public PreDefinedPropertySetEntry(PropertyType propertyType, string propertyName, PropertyCalculator propertyCalculator)
             : base(propertyName, new PreDefinedPropertySetEntryMap(propertyName) { PropertyCalculator = propertyCalculator })
      {
         PropertyType = propertyType;
      }
      public PreDefinedPropertySetEntry(PropertyType propertyType, string propertyName, BuiltInParameter builtInParameter, PropertyCalculator propertyCalculator)
             : base(propertyName, new PreDefinedPropertySetEntryMap(propertyName) { RevitBuiltInParameter = builtInParameter, PropertyCalculator = propertyCalculator })
      {
         PropertyType = propertyType;
      }
      public PreDefinedPropertySetEntry(PropertyType propertyType, string propertyName, PreDefinedPropertySetEntryMap entry)
           : base(propertyName, entry)
      {
         PropertyType = propertyType;
      }
      public PreDefinedPropertySetEntry(PropertyType propertyType, string propertyName, IEnumerable<PreDefinedPropertySetEntryMap> entries)
           : base(propertyName, entries)
      {
         PropertyType = propertyType;
      }

      /// <summary>
      /// Process to create predefined property data.
      /// </summary>
      /// <param name="element">The element for which this property is created for.</param>
      /// <returns>The created predefined property data.</returns>
      public IList<IFCData> ProcessEntry(IFCFile file, Element element)
      {
         foreach (PreDefinedPropertySetEntryMap map in Entries)
         {
            IList<IFCData> propHnd = map.ProcessEntry(file, element, PropertyType, PropertyValueType, PropertyEnumerationType);
            if (propHnd != null)
               return propHnd;
         }

         return null;
      }

   }
}