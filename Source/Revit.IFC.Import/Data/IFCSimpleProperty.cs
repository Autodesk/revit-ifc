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
      IList<IFCPropertyValue> m_IFCPropertyValues = new List<IFCPropertyValue>();

      /// <summary>
      /// The unit.
      /// </summary>
      IFCUnit m_IFCUnit = null;

      /// <summary>
      /// The unit.
      /// </summary>
      public IFCUnit IFCUnit
      {
         get { return m_IFCUnit; }
      }

      /// <summary>
      /// Returns the property value as a string, for SetValueString().
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

      /// <summary>
      /// The property values.
      /// </summary>
      public IList<IFCPropertyValue> IFCPropertyValues
      {
         get { return m_IFCPropertyValues; }
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

         Name = IFCImportHandleUtil.GetRequiredStringAttribute(simpleProperty, "Name", true);

         // IfcPropertyBoundedValue has already been split off into its own class.  Need to do the same with the rest here.
         if (IFCAnyHandleUtil.IsSubTypeOf(simpleProperty, IFCEntityType.IfcPropertySingleValue))
            ProcessIFCPropertySingleValue(simpleProperty);
         else if (IFCAnyHandleUtil.IsSubTypeOf(simpleProperty, IFCEntityType.IfcPropertyEnumeratedValue))
            ProcessIFCPropertyEnumeratedValue(simpleProperty);
         else if (IFCAnyHandleUtil.IsSubTypeOf(simpleProperty, IFCEntityType.IfcPropertyReferenceValue))
            ProcessIFCPropertyReferenceValue(simpleProperty);
         else if (IFCAnyHandleUtil.IsSubTypeOf(simpleProperty, IFCEntityType.IfcPropertyListValue))
            ProcessIFCPropertyListValue(simpleProperty);
         else if (!IFCAnyHandleUtil.IsSubTypeOf(simpleProperty, IFCEntityType.IfcPropertyBoundedValue))
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
         IFCPropertyValues.Add(new IFCPropertyValue(this, propertySingleValue.GetAttribute("NominalValue")));
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
            IFCPropertyValues.Add(new IFCPropertyValue(this, value));
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
         foreach (IFCData value in enumValues)
         {
            IFCPropertyValues.Add(new IFCPropertyValue(this, value));
         }
      }

      /// <summary>
      /// Processes an IFC property reference value.
      /// </summary>
      /// <param name="propertyReferenceValue">The IfcPropertyReferenceValue object.</param>
      void ProcessIFCPropertyReferenceValue(IFCAnyHandle propertyReferenceValue)
      {
         IFCData referenceValue = propertyReferenceValue.GetAttribute("PropertyReference");
         IFCPropertyValues.Add(new IFCPropertyValue(this, referenceValue));
      }

      /// <summary>
      /// Processes an IFC unit in the property.
      /// </summary>
      /// <param name="ifcSimpleProperty">The simple property.</param>
      /// <param name="simplePropertyHandle">The simple property handle.</param>
      static protected void ProcessIFCSimplePropertyUnit(IFCSimpleProperty ifcSimpleProperty, IFCAnyHandle simplePropertyHandle)
      {
         IFCAnyHandle ifcUnitHandle = IFCImportHandleUtil.GetOptionalInstanceAttribute(simplePropertyHandle, "Unit");
         IFCUnit ifcUnit = (ifcUnitHandle != null) ? IFCUnit.ProcessIFCUnit(ifcUnitHandle) : null;
         if (ifcUnit == null)
         {
            if (ifcSimpleProperty.IFCPropertyValues.Count > 0)
            {
               IFCPropertyValue propertyValue = ifcSimpleProperty.IFCPropertyValues[0];
               if (propertyValue != null && propertyValue.HasValue() &&
                   (propertyValue.Type == IFCDataPrimitiveType.Integer) || (propertyValue.Type == IFCDataPrimitiveType.Double))
               {
                  string unitTypeName;
                  UnitType unitType = IFCDataUtil.GetUnitTypeFromData(propertyValue.Value, UnitType.UT_Undefined, out unitTypeName);
                  if (unitType != UnitType.UT_Undefined)
                     ifcUnit = IFCImportFile.TheFile.IFCUnits.GetIFCProjectUnit(unitType);
                  else
                     Importer.TheLog.LogWarning(simplePropertyHandle.StepId, "Unhandled unit type: " + unitTypeName, true);
               }
            }
         }

         ifcSimpleProperty.m_IFCUnit = ifcUnit;
      }
   }
}