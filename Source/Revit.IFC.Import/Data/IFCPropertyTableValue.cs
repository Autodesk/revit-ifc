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
   /// Represents an IfcPropertyTableValue
   /// </summary>
   public class IFCPropertyTableValue : IFCSimpleProperty
   {
      /// <summary>
      /// The property values.
      /// </summary>
      IList<IFCPropertyValue> m_IFCDefinedValues = new List<IFCPropertyValue>();

      /// <summary>
      /// The Defined values unit of Table property.
      /// </summary>
      public IFCUnit IFCDefinedUnit { get; private set; } = null;

      /// <summary>
      /// Returns the property value as a string, for Set().
      /// </summary>
      /// <returns>The property value as a string.</returns>
      public override string PropertyValueAsString()
      {
         string propertyValue = string.Empty;
         for (int ii = 0; ii < IFCPropertyValues.Count; ii++)
         {
            if (ii > 0)
               propertyValue += "\r\n";

            propertyValue += FormatPropertyValue(IFCPropertyValues[ii]) + ";";
            propertyValue += FormatPropertyValue(m_IFCDefinedValues[ii]);
         }

         return propertyValue;
      }

      protected IFCPropertyTableValue()
      {
      }

      protected IFCPropertyTableValue(IFCAnyHandle ifcPropertyTableValue)
      {
         Process(ifcPropertyTableValue);
      }

      /// <summary>
      /// Processes an IFC table value property.
      /// </summary>
      /// <param name="ifcPropertyTableValue">The IfcPropertyTableValue object.</param>
      /// <returns>The IFCPropertyTableValue object.</returns>
      override protected void Process(IFCAnyHandle ifcPropertyTableValue)
      {
         base.Process(ifcPropertyTableValue);
                
         List<IFCData> definingValues = IFCAnyHandleUtil.GetAggregateAttribute<List<IFCData>>(ifcPropertyTableValue, "DefiningValues");
         List<IFCData> definedValues = IFCAnyHandleUtil.GetAggregateAttribute<List<IFCData>>(ifcPropertyTableValue, "DefinedValues");

         if (definingValues.Count != definedValues.Count)
            Importer.TheLog.LogWarning(ifcPropertyTableValue.StepId, "Invalid IfcPropertyTableValue: WR21", true);

         int pairsNumber = Math.Min(definingValues.Count, definedValues.Count);

         for (int ii = 0; ii < pairsNumber; ii++)
         {
            IFCPropertyValues.Add(new IFCPropertyValue(this, definingValues[ii], false));
            m_IFCDefinedValues.Add(new IFCPropertyValue(this, definedValues[ii], true));
         }

         ProcessIFCSimplePropertyTableUnit(this, ifcPropertyTableValue);
      }

      /// <summary>
      /// Processes an IFC unit in the table property.
      /// </summary>
      /// <param name="ifcSimpleTableProperty">The simple table property.</param>
      /// <param name="simplePropertyHandle">The simple property handle.</param>
      static protected void ProcessIFCSimplePropertyTableUnit(IFCPropertyTableValue ifcSimpleTableProperty, IFCAnyHandle simplePropertyHandle)
      {
         IFCPropertyValue firstPropertyValue = (ifcSimpleTableProperty.IFCPropertyValues.Count > 0) ? ifcSimpleTableProperty.IFCPropertyValues[0] : null;
         ifcSimpleTableProperty.IFCUnit = ProcessUnit(simplePropertyHandle, "DefiningUnit", firstPropertyValue);

         firstPropertyValue = (ifcSimpleTableProperty.m_IFCDefinedValues.Count > 0) ? ifcSimpleTableProperty.m_IFCDefinedValues[0] : null;
         ifcSimpleTableProperty.IFCDefinedUnit = ProcessUnit(simplePropertyHandle, "DefinedUnit", firstPropertyValue);
      }

      /// <summary>
      /// Processes an IFC table value property.
      /// </summary>
      /// <param name="ifcPropertyTableValue">The IfcPropertyTableValue handle.</param>
      /// <returns>The IFCPropertyTableValue object.</returns>
      public static IFCPropertyTableValue ProcessIFCPropertyTableValue(IFCAnyHandle ifcPropertyTableValue)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcPropertyTableValue))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcPropertyTableValue);
            return null;
         }

         IFCEntity propertyTableValue;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcPropertyTableValue.StepId, out propertyTableValue))
            return (propertyTableValue as IFCPropertyTableValue);

         return new IFCPropertyTableValue(ifcPropertyTableValue);
      }
   }
}