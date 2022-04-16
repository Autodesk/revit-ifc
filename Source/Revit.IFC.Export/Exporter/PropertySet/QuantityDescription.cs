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
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet
{
   /// <summary>
   /// A description mapping of a group of Revit parameters and/or calculated values to an IfcElementQuantity.
   /// </summary>
   /// <remarks>
   /// The mapping includes: the name of the IFC quantity, the entity type to which this mapping of quantities apply,
   /// and an array of quantities.  A quantity description is valid for only one entity type.
   /// </remarks>
   public class QuantityDescription : Description
   {
      /// <summary>
      /// The quantities stored in this quantity description.
      /// </summary>
      IList<QuantityEntry> Entries { get; set; } = new List<QuantityEntry>();

      /// <summary>
      /// Defines the building code used to calculate the element quantity.
      /// </summary>
      public string MethodOfMeasurement { get; set; } = String.Empty;

      public QuantityDescription() { }

      public QuantityDescription(string baseName, IFCEntityType entityType)
      {
         string quantitySetName = (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4) ?
             "BaseQuantities" : "Qto_" + baseName + "BaseQuantities";
         Name = quantitySetName;
         EntityTypes.Add(entityType);
      }

      /// <summary>
      /// Add an entry to the quantity map.
      /// </summary>
      /// <param name="entry">The entry to add.</param>
      public void AddEntry(QuantityEntry entry)
      {
         entry.UpdateEntry();
         Entries.Add(entry);
      }

      /// <summary>
      /// Add an entry to the quantity map.
      /// </summary>
      /// <param name="name">The name of the quantity.</param>
      /// <param name="quantityType">The type of the quantity (e.g., area).</param>
      /// <param name="calculator">The PropertyCalculator that can generate the quantity value.</param>
      /// <param name="ignoreInternalValue">If true, don't use the internal Revit parameter of the same name.</param>
      /// <remarks>
      /// <paramref name="ignoreInternalValue"/> is intended to be used when the internal Revit 
      /// parameter of the same name (depending on localization) has a different calculation than
      /// the IFC parameter.
      /// </remarks>
      public QuantityEntry AddEntry(string name, QuantityType quantityType, 
         PropertyCalculator calculator, bool ignoreInternalValue = false)
      {
         QuantityEntry ifcQE = new QuantityEntry(name);
         ifcQE.QuantityType = quantityType;
         ifcQE.PropertyCalculator = calculator;
         ifcQE.IgnoreInternalValue = ignoreInternalValue;
         AddEntry(ifcQE);
         return ifcQE;
      }

      /// <summary>
      /// Add an entry to the quantity map.
      /// </summary>
      /// <param name="entry">The entry to add.</param>
      public QuantityEntry AddEntry(string name, string revitName, QuantityType quantityType, PropertyCalculator calculator)
      {
         QuantityEntry ifcQE = new QuantityEntry(revitName, name);
         ifcQE.QuantityType = quantityType;
         ifcQE.PropertyCalculator = calculator;
         AddEntry(ifcQE);
         return ifcQE;
      }

      /// <summary>
      /// Add an entry to the quantity map.
      /// </summary>
      /// <param name="entry">The entry to add.</param>
      public QuantityEntry AddEntry(string name, BuiltInParameter parameterName, QuantityType quantityType, PropertyCalculator calculator)
      {
         QuantityEntry ifcQE = new QuantityEntry(name, parameterName);
         ifcQE.QuantityType = quantityType;
         ifcQE.PropertyCalculator = calculator;
         AddEntry(ifcQE);
         return ifcQE;
      }

      /// <summary>
      /// Creates handles for the quantities.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC class.</param>
      /// <param name="ifcParams">The extrusion creation data, used to get extra parameter information.</param>
      /// <param name="elementToUse">The base element.</param>
      /// <param name="elemTypeToUse">The base element type.</param>
      /// <returns>A set of quantities handles.</returns>
      public HashSet<IFCAnyHandle> ProcessEntries(IFCFile file, ExporterIFC exporterIFC, IFCExtrusionCreationData ifcParams, Element elementToUse, ElementType elemTypeToUse)
      {
         HashSet<IFCAnyHandle> props = new HashSet<IFCAnyHandle>();
         foreach (QuantityEntry entry in Entries)
         {
            IFCAnyHandle propHnd = entry.ProcessEntry(file, exporterIFC, ifcParams, elementToUse, elemTypeToUse);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               props.Add(propHnd);
         }
         return props;
      }
   }
}