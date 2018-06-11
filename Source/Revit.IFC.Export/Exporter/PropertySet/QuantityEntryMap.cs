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
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Exporter.PropertySet
{


   /// <summary>
   /// Represents a mapping from a Revit parameter or calculated quantity to an IFC quantity.
   /// </summary>
   public class QuantityEntryMap : EntryMap
   {
      public QuantityEntryMap() { }
      /// <summary>
      /// Constructs a QuantityEntry object.
      /// </summary>
      /// <param name="revitParameterName">
      /// Revit parameter name.
      /// </param>
      public QuantityEntryMap(string revitParameterName)
          : base(revitParameterName)
      {

      }

      public QuantityEntryMap(BuiltInParameter builtInParameter)
           : base(builtInParameter)
      {

      }

      public QuantityEntryMap(PropertyCalculator calculator)
           : base(calculator)
      {

      }
      /// <summary>
      /// Process to create element quantity.
      /// </summary>
      /// <param name="file">
      /// The IFC file.
      /// </param>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="extrusionCreationData">
      /// The IFCExtrusionCreationData.
      /// </param>
      /// <param name="element">
      /// The element of which this property is created for.
      /// </param>
      /// <param name="elementType">
      /// The element type of which this quantity is created for.
      /// </param>
      /// <returns>
      /// Then created quantity handle.
      /// </returns>
      public IFCAnyHandle ProcessEntry(IFCFile file, ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData,
             Element element, ElementType elementType, QuantityType quantityType, string methodOfMeasurement, string quantityName)
      {
         bool useProperty = (!String.IsNullOrEmpty(RevitParameterName)) || (RevitBuiltInParameter != BuiltInParameter.INVALID);

         bool success = false;
         double val = 0;
         if (useProperty)
         {
            success = (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, RevitParameterName, out val) != null);
            if (!success && RevitBuiltInParameter != BuiltInParameter.INVALID)
               success = (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, RevitBuiltInParameter, out val) != null);

            if (success) // factor in the scale factor for all the parameters depending of the data type to get the correct value
            {
               switch (quantityType)
               {
                  case QuantityType.PositiveLength:
                     val = UnitUtil.ScaleLength(val);
                     break;
                  case QuantityType.Area:
                     val = UnitUtil.ScaleArea(val);
                     break;
                  case QuantityType.Volume:
                     val = UnitUtil.ScaleVolume(val);
                     break;
                  default:
                     break;
               }
            }
         }

         if (PropertyCalculator != null && !success)
         {
            success = PropertyCalculator.Calculate(exporterIFC, extrusionCreationData, element, elementType);
            if (success)
               val = PropertyCalculator.GetDoubleValue();
         }

         IFCAnyHandle quantityHnd = null;
         if (success)
         {
            switch (quantityType)
            {
               case QuantityType.PositiveLength:
                  quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, quantityName, methodOfMeasurement, null, val);
                  break;
               case QuantityType.Area:
                  quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, quantityName, methodOfMeasurement, null, val);
                  break;
               case QuantityType.Volume:
                  quantityHnd = IFCInstanceExporter.CreateQuantityVolume(file, quantityName, methodOfMeasurement, null, val);
                  break;
               case QuantityType.Weight:
                  quantityHnd = IFCInstanceExporter.CreateQuantityWeight(file, quantityName, methodOfMeasurement, null, val);
                  break;
               default:
                  throw new InvalidOperationException("Missing case!");
            }
         }

         return quantityHnd;
      }
   }
}
