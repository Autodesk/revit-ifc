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
   /// Represents an IfcSimpleProperty
   /// </summary>
   public class IFCSimpleProperty : IFCProperty
   {
      /// <summary>
      /// The property values.
      /// </summary>
      public IList<IFCPropertyValue> IFCPropertyValues { get; private set; } = new List<IFCPropertyValue>();

      /// <summary>
      /// The unit.
      /// </summary>
      public IFCUnit IFCUnit { get; protected set; } = null;

      /// <summary>
      /// Returns the property value as a string, for Set().
      /// </summary>
      /// <returns>The property value as a string.</returns>
      public override string PropertyValueAsString()
      {
         int numValues = (IFCPropertyValues != null) ? IFCPropertyValues.Count : 0;
         if (numValues == 0)
            return string.Empty;

         string propertyValue = IFCPropertyValues[0].ValueAsString();
         for (int ii = 1; ii < numValues; ii++)
         {
            if (propertyValue != "")
               propertyValue += "; ";
            propertyValue += IFCPropertyValues[ii].ValueAsString();
         }

         return propertyValue;
      }

      protected IFCSimpleProperty()
      {
      }

      protected IFCSimpleProperty(IFCAnyHandle simpleProperty)
      {
         Process(simpleProperty);
      }

      /// <summary>
      /// Processes an IFC simple property.
      /// </summary>
      /// <param name="simpleProperty">The IfcSimpleProperty object.</param>
      /// <returns>The IFCSimpleProperty object.</returns>
      override protected void Process(IFCAnyHandle simpleProperty)
      {
         base.Process(simpleProperty);

         Name = IFCImportHandleUtil.GetRequiredStringAttribute(simpleProperty, "Name", false);
         if (string.IsNullOrWhiteSpace(Name))
         {
            Name = Properties.Resources.IFCUnknownProperty + " " + Id;
         }

         // IfcPropertyBoundedValue and IfcPropertyTableValue has already been split off into their own classes.  Need to do the same with the rest here.
         if (IFCAnyHandleUtil.IsSubTypeOf(simpleProperty, IFCEntityType.IfcPropertySingleValue))
            ProcessIFCPropertySingleValue(simpleProperty);
         else if (IFCAnyHandleUtil.IsSubTypeOf(simpleProperty, IFCEntityType.IfcPropertyEnumeratedValue))
            ProcessIFCPropertyEnumeratedValue(simpleProperty);
         else if (IFCAnyHandleUtil.IsSubTypeOf(simpleProperty, IFCEntityType.IfcPropertyReferenceValue))
            ProcessIFCPropertyReferenceValue(simpleProperty);
         else if (IFCAnyHandleUtil.IsSubTypeOf(simpleProperty, IFCEntityType.IfcPropertyListValue))
            ProcessIFCPropertyListValue(simpleProperty);
         else if (!IFCAnyHandleUtil.IsSubTypeOf(simpleProperty, IFCEntityType.IfcPropertyBoundedValue) &&
                  !IFCAnyHandleUtil.IsSubTypeOf(simpleProperty, IFCEntityType.IfcPropertyTableValue))
            Importer.TheLog.LogUnhandledSubTypeError(simpleProperty, "IfcSimpleProperty", true);
      }

      /// <summary>
      /// Processes an IFC simple property.
      /// </summary>
      /// <param name="ifcSimpleProperty">The IfcSimpleProperty handle.</param>
      /// <returns>The IFCSimpleProperty object.</returns>
      public static IFCSimpleProperty ProcessIFCSimpleProperty(IFCAnyHandle ifcSimpleProperty)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSimpleProperty))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcSimpleProperty);
            return null;
         }

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcSimpleProperty, IFCEntityType.IfcPropertyBoundedValue))
            return IFCPropertyBoundedValue.ProcessIFCPropertyBoundedValue(ifcSimpleProperty);
         else if (IFCAnyHandleUtil.IsSubTypeOf(ifcSimpleProperty, IFCEntityType.IfcPropertyTableValue))
            return IFCPropertyTableValue.ProcessIFCPropertyTableValue(ifcSimpleProperty);

         // Other subclasses are handled below for now.
         IFCEntity simpleProperty;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSimpleProperty.StepId, out simpleProperty))
            return (simpleProperty as IFCSimpleProperty);

         return new IFCSimpleProperty(ifcSimpleProperty);
      }

      /// <summary>
      /// Processes an IFC property single value.
      /// </summary>
      /// <param name="propertySingleValue">The IfcPropertySingleValue object.</param>
      void ProcessIFCPropertySingleValue(IFCAnyHandle propertySingleValue)
      {
         IFCPropertyValues.Add(new IFCPropertyValue(this, propertySingleValue.GetAttribute("NominalValue"), false));
         ProcessIFCSimplePropertyUnit(this, propertySingleValue);
      }

      /// <summary>
      /// Processes an IFC property list value.
      /// </summary>
      /// <param name="propertyListValue">The IfcPropertyListValue object.</param>
      void ProcessIFCPropertyListValue(IFCAnyHandle propertyListValue)
      {
         List<IFCData> listValues = IFCAnyHandleUtil.GetAggregateAttribute<List<IFCData>>(propertyListValue, "ListValues");
         foreach (IFCData value in listValues)
         {
            IFCPropertyValues.Add(new IFCPropertyValue(this, value, false));
         }
         ProcessIFCSimplePropertyUnit(this, propertyListValue);
      }

      /// <summary>
      /// Processes an IFC property enumerated value.
      /// </summary>
      /// <param name="propertyEnumeratedValue">The IfcPropertyEnumeratedValue object.</param>
      void ProcessIFCPropertyEnumeratedValue(IFCAnyHandle propertyEnumeratedValue)
      {
         List<IFCData> enumValues = IFCAnyHandleUtil.GetAggregateAttribute<List<IFCData>>(propertyEnumeratedValue, "EnumerationValues");
         if (enumValues != null)
         {
            foreach (IFCData value in enumValues)
            {
               IFCPropertyValues.Add(new IFCPropertyValue(this, value, false));
            }
         }
      }

      /// <summary>
      /// Processes an IFC property reference value.
      /// </summary>
      /// <param name="propertyReferenceValue">The IfcPropertyReferenceValue object.</param>
      void ProcessIFCPropertyReferenceValue(IFCAnyHandle propertyReferenceValue)
      {
         IFCData referenceValue = propertyReferenceValue.GetAttribute("PropertyReference");
         IFCPropertyValues.Add(new IFCPropertyValue(this, referenceValue, false));
      }

      /// <summary>
      /// Processes an IFC unit in the property.
      /// </summary>
      /// <param name="ifcSimpleProperty">The simple property.</param>
      /// <param name="simplePropertyHandle">The simple property handle.</param>
      static protected void ProcessIFCSimplePropertyUnit(IFCSimpleProperty ifcSimpleProperty, IFCAnyHandle simplePropertyHandle)
      {
         IFCPropertyValue firstPropertyValue = (ifcSimpleProperty.IFCPropertyValues.Count > 0) ? ifcSimpleProperty.IFCPropertyValues[0] : null;
         ifcSimpleProperty.IFCUnit = ProcessUnit(simplePropertyHandle, "Unit", firstPropertyValue);
      }

      /// <summary>
      /// Processes an IFC unit.
      /// </summary>
      /// <param name="simplePropertyHandle">The simple property.</param>
      /// <param name="unitAttributeName">The name of unit attribute.</param>
      /// <param name="propertyValue">The property value.</param>
      static protected IFCUnit ProcessUnit(IFCAnyHandle simplePropertyHandle, string unitAttributeName, IFCPropertyValue propertyValue)
      {
         IFCAnyHandle ifcUnitHandle = IFCImportHandleUtil.GetOptionalInstanceAttribute(simplePropertyHandle, unitAttributeName);
         IFCUnit ifcUnit = (ifcUnitHandle != null) ? IFCUnit.ProcessIFCUnit(ifcUnitHandle) : null;
         if (ifcUnit == null)
         {
            if (propertyValue != null)
            {
               if (propertyValue != null && propertyValue.HasValue() &&
                   (propertyValue.Type == IFCDataPrimitiveType.Integer) || (propertyValue.Type == IFCDataPrimitiveType.Double))
               {
                  string unitTypeName;
                  ForgeTypeId specTypeId = IFCDataUtil.GetUnitTypeFromData(propertyValue.Value, new ForgeTypeId(), out unitTypeName);
                  if (!specTypeId.Empty())
                     ifcUnit = IFCImportFile.TheFile.IFCUnits.GetIFCProjectUnit(specTypeId);
                  else
                     Importer.TheLog.LogWarning(simplePropertyHandle.StepId, "Unhandled unit type: " + unitTypeName, true);
               }
            }
         }

         return ifcUnit;
      }

      protected string FormatPropertyValue(IFCPropertyValue propertyValue)
      {
         if (propertyValue.IFCUnit != null)
         {
            FormatValueOptions formatValueOptions = new FormatValueOptions();
            FormatOptions specFormatOptions = IFCImportFile.TheFile.Document.GetUnits().GetFormatOptions(propertyValue.IFCUnit.Spec);
            specFormatOptions.Accuracy = 1e-8;
            if (specFormatOptions.CanSuppressTrailingZeros())
               specFormatOptions.SuppressTrailingZeros = true;
            formatValueOptions.SetFormatOptions(specFormatOptions);

            // If ScaleValues is false, value is in source file units, but 'UnitFormatUtils.Format' expects
            // it in internal units and it then converts it to display units, which should be the same as
            // the source file units.
            double value = Importer.TheProcessor.ScaleValues ?
               propertyValue.AsDouble() :
               UnitUtils.ConvertToInternalUnits(propertyValue.AsDouble(), specFormatOptions.GetUnitTypeId());

            return UnitFormatUtils.Format(IFCImportFile.TheFile.Document.GetUnits(), propertyValue.IFCUnit.Spec, value, false, formatValueOptions);
         }
         else
            return propertyValue.ValueAsString();
      }


   }
}