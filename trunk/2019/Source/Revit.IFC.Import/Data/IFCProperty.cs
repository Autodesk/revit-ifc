﻿//
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
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Properties;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcProperty.
   /// </summary>
   public abstract class IFCProperty : IFCEntity
   {
      /// <summary>
      /// The name.
      /// </summary>
      protected string m_Name;

      /// <summary>
      /// The name.
      /// </summary>
      public string Name
      {
         get { return m_Name; }
         protected set { m_Name = value; }
      }

      protected IFCProperty()
      {
      }

      /// <summary>
      /// Returns the property value as a string, for SetValueString().
      /// </summary>
      /// <returns>The property value as a string.</returns>
      public abstract string PropertyValueAsString();

      /// <summary>
      /// Processes an IFC property.
      /// </summary>
      /// <param name="ifcProperty">The property.</param>
      /// <returns>The IFCProperty object.</returns>
      public static IFCProperty ProcessIFCProperty(IFCAnyHandle ifcProperty)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcProperty))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcProperty);
            return null;
         }

         try
         {
            IFCEntity property;
            if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcProperty.StepId, out property))
               return (property as IFCProperty);

            if (IFCAnyHandleUtil.IsSubTypeOf(ifcProperty, IFCEntityType.IfcComplexProperty))
               return IFCComplexProperty.ProcessIFCComplexProperty(ifcProperty);

            if (IFCAnyHandleUtil.IsSubTypeOf(ifcProperty, IFCEntityType.IfcSimpleProperty))
               return IFCSimpleProperty.ProcessIFCSimpleProperty(ifcProperty);
         }
         catch (Exception ex)
         {
            Importer.TheLog.LogError(ifcProperty.StepId, ex.Message, false);
            return null;
         }

         Importer.TheLog.LogUnhandledSubTypeError(ifcProperty, IFCEntityType.IfcProperty, false);
         return null;
      }

      private bool IsValidParameterType(Parameter parameter, IFCDataPrimitiveType dataType)
      {
         switch (parameter.StorageType)
         {
            case StorageType.String:
               if (dataType == IFCDataPrimitiveType.String ||
                   dataType == IFCDataPrimitiveType.Enumeration ||
                   dataType == IFCDataPrimitiveType.Binary ||
                   dataType == IFCDataPrimitiveType.Double ||
                   dataType == IFCDataPrimitiveType.Integer ||
                   dataType == IFCDataPrimitiveType.Boolean ||
                   dataType == IFCDataPrimitiveType.Logical)
                  return true;
               break;
            case StorageType.Integer:
               if (dataType == IFCDataPrimitiveType.Integer ||
                   dataType == IFCDataPrimitiveType.Boolean ||
                   dataType == IFCDataPrimitiveType.Logical)
                  return true;
               break;
            case StorageType.Double:
               if (dataType == IFCDataPrimitiveType.Double ||
                   dataType == IFCDataPrimitiveType.Integer ||
                   dataType == IFCDataPrimitiveType.Boolean ||
                   dataType == IFCDataPrimitiveType.Logical)
                  return true;
               break;
         }

         return false;
      }

      /// <summary>
      /// Create a property for a given element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element being created.</param>
      /// <param name="parameterMap">The parameters of the element.  Cached for performance.</param>
      /// <param name="propertySetName">The name of the containing property set.</param>
      /// <param name="createdParameters">The names of the created parameters.</param>
      public void Create(Document doc, Element element, IFCParameterSetByGroup parameterGroupMap, string propertySetName, ISet<string> createdParameters)
      {
         // Try to get the single value from the property.  If we can't get a single value, get it as a string.
         IFCPropertyValue propertyValueToUse = null;
         if (this is IFCSimpleProperty)
         {
            IFCSimpleProperty simpleProperty = this as IFCSimpleProperty;
            IList<IFCPropertyValue> propertyValues = simpleProperty.IFCPropertyValues;
            if (propertyValues != null && propertyValues.Count == 1)
            {
               // If the value isn't set, skip it.  We won't warn.
               if (!propertyValues[0].HasValue())
                  return;

               propertyValueToUse = propertyValues[0];
            }
         }

         IFCDataPrimitiveType dataType = IFCDataPrimitiveType.Unknown;
         UnitType unitType = UnitType.UT_Undefined;

         bool? boolValueToUse = null;
         IFCLogical? logicalValueToUse = null;
         int? intValueToUse = null;
         double? doubleValueToUse = null;
         ElementId elementIdValueToUse = null;
         string stringValueToUse = null;

         if (propertyValueToUse == null)
         {
            string propertyValueAsString = PropertyValueAsString();
            if (propertyValueAsString == null)
            {
               Importer.TheLog.LogError(Id, "Couldn't create parameter: " + Name, false);
               return;
            }

            dataType = IFCDataPrimitiveType.String;
            stringValueToUse = propertyValueAsString;
         }
         else
         {
            dataType = propertyValueToUse.Value.PrimitiveType;
            if (dataType == IFCDataPrimitiveType.Instance)
            {
               IFCAnyHandle propertyValueHandle = propertyValueToUse.Value.AsInstance();
               ElementId propertyValueAsId = IFCObjectReferenceSelect.ToElementId(propertyValueHandle);
               if (propertyValueAsId != ElementId.InvalidElementId)
               {
                  elementIdValueToUse = propertyValueAsId;
               }
               else
               {
                  stringValueToUse = IFCObjectReferenceSelect.ToString(propertyValueHandle);
                  dataType = IFCDataPrimitiveType.String;
               }
            }
            else
            {
               switch (dataType)
               {
                  case IFCDataPrimitiveType.String:
                  case IFCDataPrimitiveType.Enumeration:
                  case IFCDataPrimitiveType.Binary:
                     stringValueToUse = propertyValueToUse.AsString();
                     break;
                  case IFCDataPrimitiveType.Integer:
                     intValueToUse = propertyValueToUse.AsInteger();
                     break;
                  case IFCDataPrimitiveType.Boolean:
                     boolValueToUse = propertyValueToUse.AsBoolean();
                     break;
                  case IFCDataPrimitiveType.Logical:
                     logicalValueToUse = propertyValueToUse.AsLogical();
                     break;
                  case IFCDataPrimitiveType.Double:
                     if (propertyValueToUse.IFCUnit != null)
                        unitType = propertyValueToUse.IFCUnit.UnitType;
                     else
                        unitType = IFCDataUtil.GetUnitTypeFromData(propertyValueToUse.Value, UnitType.UT_Number);

                     doubleValueToUse = propertyValueToUse.AsScaledDouble();
                     break;
                  default:
                     Importer.TheLog.LogError(Id, "Unknown value type for parameter: " + Name, false);
                     return;
               }
            }
         }

         Parameter existingParameter = null;
         bool elementIsType = (element is ElementType);
         string typeString = elementIsType ? " " + Resources.IFCTypeSchedule : string.Empty;
         string originalParameterName = Name + "(" + propertySetName + typeString + ")";
         string parameterName = originalParameterName;

         if (parameterGroupMap.TryFindParameter(parameterName, out existingParameter))
         {
            if ((existingParameter != null) && !IsValidParameterType(existingParameter, dataType))
               existingParameter = null;
         }

         if (existingParameter == null)
         {
            int parameterNameCount = 2;
            while (createdParameters.Contains(parameterName))
            {
               parameterName = originalParameterName + " " + parameterNameCount;
               parameterNameCount++;
            }
            if (parameterNameCount > 2)
               Importer.TheLog.LogWarning(Id, "Renamed parameter: " + originalParameterName + " to: " + parameterName, false);

            bool created = false;
            switch (dataType)
            {
               case IFCDataPrimitiveType.String:
               case IFCDataPrimitiveType.Enumeration:
               case IFCDataPrimitiveType.Binary:
                  created = IFCPropertySet.AddParameterString(doc, element, parameterName, stringValueToUse, Id);
                  break;
               case IFCDataPrimitiveType.Integer:
                  created = IFCPropertySet.AddParameterInt(doc, element, parameterName, intValueToUse.Value, Id);
                  break;
               case IFCDataPrimitiveType.Boolean:
                  created = IFCPropertySet.AddParameterBoolean(doc, element, parameterName, boolValueToUse.Value, Id);
                  break;
               case IFCDataPrimitiveType.Logical:
                  if (logicalValueToUse != IFCLogical.Unknown)
                     created = IFCPropertySet.AddParameterBoolean(doc, element, parameterName, (logicalValueToUse == IFCLogical.True), Id);
                  break;
               case IFCDataPrimitiveType.Double:
                  created = IFCPropertySet.AddParameterDouble(doc, element, parameterName, unitType, doubleValueToUse.Value, Id);
                  break;
               case IFCDataPrimitiveType.Instance:
                  created = IFCPropertySet.AddParameterElementId(doc, element, parameterName, elementIdValueToUse, Id);
                  break;
            }

            if (created)
               createdParameters.Add(originalParameterName);

            return;
         }

         bool couldSetValue = false;
         switch (existingParameter.StorageType)
         {
            case StorageType.String:
               {
                  switch (dataType)
                  {
                     case IFCDataPrimitiveType.String:
                     case IFCDataPrimitiveType.Enumeration:
                     case IFCDataPrimitiveType.Binary:
                        couldSetValue = existingParameter.Set(stringValueToUse);
                        break;
                     case IFCDataPrimitiveType.Integer:
                        couldSetValue = existingParameter.Set(intValueToUse.Value.ToString());
                        break;
                     case IFCDataPrimitiveType.Boolean:
                        couldSetValue = existingParameter.Set(boolValueToUse.Value ? "True" : "False");
                        break;
                     case IFCDataPrimitiveType.Logical:
                        couldSetValue = existingParameter.Set(logicalValueToUse.ToString());
                        break;
                     case IFCDataPrimitiveType.Double:
                        couldSetValue = existingParameter.Set(doubleValueToUse.ToString());
                        break;
                     default:
                        break;
                  }
               }
               break;
            case StorageType.Integer:
               if (dataType == IFCDataPrimitiveType.Integer)
                  couldSetValue = existingParameter.Set(intValueToUse.Value);
               else if (dataType == IFCDataPrimitiveType.Boolean)
                  couldSetValue = existingParameter.Set(boolValueToUse.Value ? 1 : 0);
               else if (dataType == IFCDataPrimitiveType.Logical)
                  couldSetValue = (logicalValueToUse == IFCLogical.Unknown) ? true : existingParameter.Set((logicalValueToUse == IFCLogical.True) ? 1 : 0);
               break;
            case StorageType.Double:
               if (dataType == IFCDataPrimitiveType.Double)
                  couldSetValue = existingParameter.Set(doubleValueToUse.Value);
               else if (dataType == IFCDataPrimitiveType.Integer)
                  couldSetValue = existingParameter.Set(intValueToUse.Value);
               else if (dataType == IFCDataPrimitiveType.Boolean)
                  couldSetValue = existingParameter.Set(boolValueToUse.Value ? 1 : 0);
               else if ((dataType == IFCDataPrimitiveType.Logical) && (logicalValueToUse != IFCLogical.Unknown))
                  couldSetValue = existingParameter.Set((logicalValueToUse == IFCLogical.True) ? 1 : 0);
               break;
         }

         if (!couldSetValue)
            Importer.TheLog.LogError(Id, "Couldn't create parameter: " + Name + " of storage type: " + existingParameter.StorageType.ToString(), false);
      }
   }
}