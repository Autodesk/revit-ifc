//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
// Copyright (C) 2013  Autodesk, Inc.
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
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcPropertyBoundedValue
   /// </summary>
   public class IFCPropertyBoundedValue : IFCSimpleProperty
   {
      private int m_LowerBoundPropertyIndex = -1;

      private int m_UpperBoundPropertyIndex = -1;

      private int m_SetPointValueIndex = -1;

      private string FormatBoundedValue(IFCPropertyValue propertyValue)
      {
         if (IFCUnit != null)
         {
            FormatValueOptions formatValueOptions = new FormatValueOptions();
            FormatOptions specFormatOptions = IFCImportFile.TheFile.Document.GetUnits().GetFormatOptions(IFCUnit.UnitType);
            specFormatOptions.Accuracy = 1e-8;
            if (specFormatOptions.CanSuppressTrailingZeros())
               specFormatOptions.SuppressTrailingZeros = true;
            formatValueOptions.SetFormatOptions(specFormatOptions);
            return UnitFormatUtils.Format(IFCImportFile.TheFile.Document.GetUnits(), IFCUnit.UnitType, propertyValue.AsDouble(), true, false, formatValueOptions);
         }
         else
            return propertyValue.ValueAsString();
      }

      /// <summary>
      /// Returns the property value as a string, for Set().
      /// </summary>
      /// <returns>The property value as a string.</returns>
      public override string PropertyValueAsString()
      {
         // Format as one of the following:
         // None: empty string
         // Lower only: >= LowValue
         // Upper only: <= UpperValue
         // Lower and Upper: [ LowValue - UpperValue ]
         // SetPointValue: (SetPointValue)
         // Lower, SetPointValue: >= LowValue (SetPointValue)
         // Upper, SetPointValue: >= UpperValue (SetPointValue)
         // Lower, Upper, SetPointValue: [ LowValue - UpperValue ] (SetPointValue)
         string propertyValueAsString = string.Empty;

         bool hasLowerBoundPropertyIndex = (m_LowerBoundPropertyIndex >= 0);
         bool hasUpperBoundPropertyIndex = (m_UpperBoundPropertyIndex >= 0);

         if (hasLowerBoundPropertyIndex)
         {
            if (!hasUpperBoundPropertyIndex)
               propertyValueAsString += ">= ";
            else
               propertyValueAsString += "[ ";

            propertyValueAsString += FormatBoundedValue(IFCPropertyValues[m_LowerBoundPropertyIndex]);
         }

         if (hasUpperBoundPropertyIndex)
         {
            if (!hasLowerBoundPropertyIndex)
               propertyValueAsString += "<= ";
            else
               propertyValueAsString += " - ";
            propertyValueAsString += FormatBoundedValue(IFCPropertyValues[m_UpperBoundPropertyIndex]);
            if (hasLowerBoundPropertyIndex)
               propertyValueAsString += " ]";
         }

         if (m_SetPointValueIndex >= 0)
         {
            if (hasUpperBoundPropertyIndex || hasLowerBoundPropertyIndex)
               propertyValueAsString += " ";
            propertyValueAsString += "(" + FormatBoundedValue(IFCPropertyValues[m_SetPointValueIndex]) + ")";
         }

         return propertyValueAsString;
      }

      protected IFCPropertyBoundedValue()
      {
      }

      protected IFCPropertyBoundedValue(IFCAnyHandle ifcPropertyBoundedValue)
      {
         Process(ifcPropertyBoundedValue);
      }

      /// <summary>
      /// Processes an IFC bounded value property.
      /// </summary>
      /// <param name="ifcPropertyBoundedValue">The IfcPropertyBoundedValue object.</param>
      /// <returns>The IFCPropertyBoundedValue object.</returns>
      override protected void Process(IFCAnyHandle ifcPropertyBoundedValue)
      {
         base.Process(ifcPropertyBoundedValue);

         IFCData lowerBoundValue = ifcPropertyBoundedValue.GetAttribute("LowerBoundValue");
         IFCData upperBoundValue = ifcPropertyBoundedValue.GetAttribute("UpperBoundValue");
         IFCData setPointValue = (IFCImportFile.TheFile.SchemaVersion > IFCSchemaVersion.IFC2x3) ? ifcPropertyBoundedValue.GetAttribute("SetPointValue") : null;

         if (lowerBoundValue != null)
         {
            m_LowerBoundPropertyIndex = IFCPropertyValues.Count;
            IFCPropertyValues.Add(new IFCPropertyValue(this, lowerBoundValue));
         }

         if (upperBoundValue != null)
         {
            m_UpperBoundPropertyIndex = IFCPropertyValues.Count;
            IFCPropertyValues.Add(new IFCPropertyValue(this, upperBoundValue));
         }

         if (setPointValue != null)
         {
            m_SetPointValueIndex = IFCPropertyValues.Count;
            IFCPropertyValues.Add(new IFCPropertyValue(this, setPointValue));
         }

         ProcessIFCSimplePropertyUnit(this, ifcPropertyBoundedValue);
      }

      /// <summary>
      /// Processes an IFC bounded value property.
      /// </summary>
      /// <param name="ifcPropertyBoundedValue">The IfcPropertyBoundedValue handle.</param>
      /// <returns>The IFCPropertyBoundedValue object.</returns>
      public static IFCPropertyBoundedValue ProcessIFCPropertyBoundedValue(IFCAnyHandle ifcPropertyBoundedValue)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcPropertyBoundedValue))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcPropertyBoundedValue);
            return null;
         }

         IFCEntity propertyBoundedValue;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcPropertyBoundedValue.StepId, out propertyBoundedValue))
            return (propertyBoundedValue as IFCPropertyBoundedValue);

         return new IFCPropertyBoundedValue(ifcPropertyBoundedValue);
      }
   }
}