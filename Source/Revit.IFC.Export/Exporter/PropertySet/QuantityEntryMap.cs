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

      public QuantityEntryMap(string revitParameterName, BuiltInParameter builtInParameter)
          : base(revitParameterName, builtInParameter)
      {

      }
      /// <summary>
      /// Process to create element quantity.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="extrusionCreationData">The IFCExtrusionCreationData.</param>
      /// <param name="element">The element of which this property is created for.</param>
      /// <param name="elementType">The element type of which this quantity is created for.</param>
      /// <returns>The created quantity handle.</returns>
      public IFCAnyHandle ProcessEntry(IFCFile file, ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData,
             Element element, ElementType elementType, QuantityEntry parentEntry)
      {
         bool useProperty = (!String.IsNullOrEmpty(RevitParameterName)) || (RevitBuiltInParameter != BuiltInParameter.INVALID);
         
         bool success = false;
         object val = 0;
         if (useProperty)
         {
            double dblVal = 0.0;

            if (parentEntry.QuantityType is QuantityType.Count)
            {
               int? intValPar = null;
               intValPar = (ParameterUtil.GetIntValueFromElementOrSymbol(element, RevitParameterName));
               if (intValPar.HasValue)
               {
                  success = true;
                  val = intValPar.Value;
               }
            }
            else
            {
               success = (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, RevitParameterName, parentEntry.IgnoreInternalValue, out dblVal) != null);
               if (!success && RevitBuiltInParameter != BuiltInParameter.INVALID)
                  success = (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, RevitBuiltInParameter, out dblVal) != null);
               if (success)
                  val = dblVal;
            }

            if (success) // factor in the scale factor for all the parameters depending of the data type to get the correct value
            {
               switch (parentEntry.QuantityType)
               {
                  case QuantityType.PositiveLength:
                  case QuantityType.Length:
                     val = UnitUtil.ScaleLength((double)val);
                     break;
                  case QuantityType.Area:
                     val = UnitUtil.ScaleArea((double)val);
                     break;
                  case QuantityType.Volume:
                     val = UnitUtil.ScaleVolume((double)val);
                     break;
                  case QuantityType.Count:
                     break;
                  case QuantityType.Time:
                     break;
                  default:
                     break;
               }
            }
         }

         if (PropertyCalculator != null && !success)
         {
            success = PropertyCalculator.Calculate(exporterIFC, extrusionCreationData, element, elementType);
            if (success && parentEntry.QuantityType == QuantityType.Count)
               val = PropertyCalculator.GetIntValue();
            else
               val = PropertyCalculator.GetDoubleValue();
         }

         IFCAnyHandle quantityHnd = null;
         if (success)
         {
            switch (parentEntry.QuantityType)
            {
               case QuantityType.PositiveLength:
               case QuantityType.Length:
                  quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, parentEntry.PropertyName, parentEntry.MethodOfMeasurement, null, (double)val);
                  break;
               case QuantityType.Area:
                  quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, parentEntry.PropertyName, parentEntry.MethodOfMeasurement, null, (double)val);
                  break;
               case QuantityType.Volume:
                  quantityHnd = IFCInstanceExporter.CreateQuantityVolume(file, parentEntry.PropertyName, parentEntry.MethodOfMeasurement, null, (double)val);
                  break;
               case QuantityType.Weight:
                  quantityHnd = IFCInstanceExporter.CreateQuantityWeight(file, parentEntry.PropertyName, parentEntry.MethodOfMeasurement, null, (double)val);
                  break;
               case QuantityType.Count:
                  quantityHnd = IFCInstanceExporter.CreateQuantityCount(file, parentEntry.PropertyName, parentEntry.MethodOfMeasurement, null, (int)val);
                  break;
               case QuantityType.Time:
                  quantityHnd = IFCInstanceExporter.CreateQuantityTime(file, parentEntry.PropertyName, parentEntry.MethodOfMeasurement, null, (double)val);
                  break;
               default:
                  throw new InvalidOperationException("Missing case!");
            }
         }

         return quantityHnd;
      }
   }
}
