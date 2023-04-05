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
   /// This enumerated type represents the types of quantities that can be exported.
   /// </summary>
   public enum QuantityType
   {
      /// <summary>
      /// A real number quantity.
      /// </summary>
      Real,
      /// <summary>
      /// A length quantity.
      /// </summary>
      Length,
      PositiveLength,
      /// <summary>
      /// An area quantity.
      /// </summary>
      Area,
      /// <summary>
      /// A volume quantity.
      /// </summary>
      Volume,
      /// <summary>
      /// A Weight quantity
      /// </summary>
      Weight,
      /// <summary>
      /// A Count quantity
      /// </summary>
      Count,
      /// <summary>
      /// A time duration quantity
      /// </summary>
      Time,
      Mass
   }

   /// <summary>
   /// Represents a mapping from a Revit parameter or calculated quantity to an IFC quantity.
   /// </summary>
   public class QuantityEntry : Entry<QuantityEntryMap>
   {
      /// <summary>
      /// Constructs a QuantityEntry object.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      /// 
      public QuantityEntry(string revitParameterName)
          : base(revitParameterName)
      {

      }
      public QuantityEntry(string revitParameterName, string propertyName)
          : base(propertyName, new QuantityEntryMap(revitParameterName, propertyName))
      {

      }
      public QuantityEntry(string propertyName, BuiltInParameter builtInParameter)
          : base(propertyName, new QuantityEntryMap(propertyName, null) { RevitBuiltInParameter = builtInParameter })
      {

      }
      public QuantityEntry(string propertyName, PropertyCalculator calculator)
          : base(propertyName, new QuantityEntryMap(propertyName, null) { PropertyCalculator = calculator })
      {

      }

      public QuantityEntry(string propertyName, IEnumerable<QuantityEntryMap> entries)
           : base(propertyName, entries)
      {

      }

      /// <summary>
      /// The type of the quantity.
      /// </summary>
      public QuantityType QuantityType { get; set; } = QuantityType.Real;

      /// <summary>
      /// Defines the building code used to calculate the element quantity.
      /// </summary>
      public string MethodOfMeasurement { get; set; } = String.Empty;

      /// <summary>
      /// If true, don't use the internal Revit parameter of the same name.
      /// </summary>
      /// <remarks>
      /// This is intended to be used when the internal Revit parameter of the same name 
      /// (depending on localization) has a different calculation than the IFC parameter.
      /// </remarks>
      public bool IgnoreInternalValue { get; set; } = false;

      /// <summary>
      /// Process to create element quantity.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="extrusionCreationData">The IFCExportBodyParams.</param>
      /// <param name="element">The element of which this property is created for.</param>
      /// <param name="elementType">The element type of which this quantity is created for.</param>
      /// <returns>The created quantity handle.</returns>
      public IFCAnyHandle ProcessEntry(IFCFile file, ExporterIFC exporterIFC,
         IFCExportBodyParams extrusionCreationData, Element element, ElementType elementType)
      {
         foreach (QuantityEntryMap entry in Entries)
         {
            IFCAnyHandle result = entry.ProcessEntry(file, exporterIFC, extrusionCreationData, element, 
               elementType, this);
            if (result != null)
               return result;
         }
         return null;
      }
   }
}
