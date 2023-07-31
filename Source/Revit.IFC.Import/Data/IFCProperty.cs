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
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Utility;
using Revit.IFC.Import.Core;
using System.Runtime.Remoting;

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
      public string Name { get; protected set; }

      protected IFCProperty()
      {
      }

      /// <summary>
      /// Returns the property value as a string, for Set().
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

      private bool AddStringTypeParameter(bool multilineTableProperty, Document doc, Element element, Category category,
         IFCObjectDefinition objDef, string parameterName, string stringValueToUse, ParametersToSet parametersToSet)
      {
         if (multilineTableProperty)
         {
            return parametersToSet.AddParameterMultilineString(doc, element, category, objDef, parameterName,
               stringValueToUse, Id);
         }

         return parametersToSet.AddStringParameter(doc, element, category, objDef, parameterName, stringValueToUse, Id);
      }

      /// <summary>
      /// Create a property for a given element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element being created.</param>
      /// <param name="category">The category of the element being created.</param>
      /// <param name="parameterMap">The parameters of the element.  Cached for performance.</param>
      /// <param name="propertyFullName">The name of the containing property set.</param>
      /// <param name="createdParameters">The names of the created parameters.</param>
      public void Create(Document doc, Element element, Category category, IFCObjectDefinition objDef, 
         IFCParameterSetByGroup parameterGroupMap, string propertyFullName, ISet<string> createdParameters,
         ParametersToSet parametersToSet)
      {
         // Try to get the single value from the property.  If we can't get a single value, get it as a string.
         IFCPropertyValue propertyValueToUse = null;
         bool multilineTableProperty = (this is IFCPropertyTableValue);

         if ((this is IFCSimpleProperty) && !multilineTableProperty)
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
         ForgeTypeId specTypeId = new ForgeTypeId();
         ForgeTypeId unitsTypeId = null;

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
                  case IFCDataPrimitiveType.Number:
                     doubleValueToUse = propertyValueToUse.AsNumber();
                     specTypeId = IFCDataUtil.GetUnitTypeFromData(propertyValueToUse.Value, SpecTypeId.Number);
                     break;
                  case IFCDataPrimitiveType.Double:
                     if (propertyValueToUse.IFCUnit != null)
                     {
                        specTypeId = propertyValueToUse.IFCUnit.Spec;
                        unitsTypeId = propertyValueToUse.IFCUnit.Unit;
                     }
                     else
                     {
                        specTypeId = IFCDataUtil.GetUnitTypeFromData(propertyValueToUse.Value, SpecTypeId.Number);
                     }

                     doubleValueToUse = Importer.TheProcessor.ScaleValues ?
                        propertyValueToUse.AsScaledDouble() :
                        propertyValueToUse.AsUnscaledDouble();
                     break;
                  default:
                     Importer.TheLog.LogError(Id, "Unknown value type for parameter: " + Name, false);
                     return;
               }
            }
         }

         if (stringValueToUse != null && stringValueToUse.Length == 0)
            return;

         Parameter existingParameter = null;


         string parameterName = propertyFullName;

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
               parameterName = propertyFullName + " " + parameterNameCount;
               parameterNameCount++;
            }
            if (parameterNameCount > 2)
               Importer.TheLog.LogWarning(Id, "Renamed parameter: " + propertyFullName + " to: " + parameterName, false);

            bool created = false;
            switch (dataType)
            {
               case IFCDataPrimitiveType.String:
               case IFCDataPrimitiveType.Enumeration:
               case IFCDataPrimitiveType.Binary:
                  created = AddStringTypeParameter(multilineTableProperty, doc, element, category,
                     objDef, parameterName, stringValueToUse, parametersToSet);
                  break;
               case IFCDataPrimitiveType.Integer:
                  created = parametersToSet.AddParameterInt(doc, element, category, objDef, parameterName, 
                     intValueToUse.Value, Id);
                  break;
               case IFCDataPrimitiveType.Boolean:
                  created = parametersToSet.AddParameterBoolean(doc, element, category, objDef, parameterName, boolValueToUse.Value, Id);
                  break;
               case IFCDataPrimitiveType.Logical:
                  if (logicalValueToUse != IFCLogical.Unknown)
                     created = parametersToSet.AddParameterBoolean(doc, element, category, objDef, parameterName, (logicalValueToUse == IFCLogical.True), Id);
                  break;
               case IFCDataPrimitiveType.Number:
               case IFCDataPrimitiveType.Double:
                  created = parametersToSet.AddParameterDouble(doc, element, category, objDef, parameterName, specTypeId, unitsTypeId, doubleValueToUse.Value, Id);
                  break;
               case IFCDataPrimitiveType.Instance:
                  created = parametersToSet.AddParameterElementId(doc, element, category, objDef, parameterName, elementIdValueToUse, Id);
                  break;
            }

            if (created)
               createdParameters.Add(propertyFullName);

            return;
         }

         switch (existingParameter.StorageType)
         {
            case StorageType.String:
               {
                  switch (dataType)
                  {
                     case IFCDataPrimitiveType.String:
                     case IFCDataPrimitiveType.Enumeration:
                     case IFCDataPrimitiveType.Binary:
                        parametersToSet.AddStringParameter(existingParameter, stringValueToUse);
                        break;
                     case IFCDataPrimitiveType.Integer:
                        parametersToSet.AddStringParameter(existingParameter, intValueToUse.Value.ToString());
                        break;
                     case IFCDataPrimitiveType.Boolean:
                        parametersToSet.AddStringParameter(existingParameter, boolValueToUse.Value ? "True" : "False");
                        break;
                     case IFCDataPrimitiveType.Logical:
                        parametersToSet.AddStringParameter(existingParameter, logicalValueToUse.ToString());
                        break;
                     case IFCDataPrimitiveType.Number:
                     case IFCDataPrimitiveType.Double:
                        parametersToSet.AddStringParameter(existingParameter, doubleValueToUse.ToString());
                        break;
                     default:
                        break;
                  }
               }
               break;
            case StorageType.Integer:
               if (dataType == IFCDataPrimitiveType.Integer)
                  parametersToSet.AddIntegerParameter(existingParameter, intValueToUse.Value);
               else if (dataType == IFCDataPrimitiveType.Boolean)
                  parametersToSet.AddIntegerParameter(existingParameter, boolValueToUse.Value ? 1 : 0);
               else if (dataType == IFCDataPrimitiveType.Logical && logicalValueToUse != IFCLogical.Unknown)
                  parametersToSet.AddIntegerParameter(existingParameter, (logicalValueToUse == IFCLogical.True) ? 1 : 0);
               break;
            case StorageType.Double:
               if (dataType == IFCDataPrimitiveType.Double)
                  parametersToSet.AddDoubleParameter(existingParameter, doubleValueToUse.Value);
               else if (dataType == IFCDataPrimitiveType.Integer)
                  parametersToSet.AddDoubleParameter(existingParameter, intValueToUse.Value);
               else if (dataType == IFCDataPrimitiveType.Boolean)
                  parametersToSet.AddDoubleParameter(existingParameter, boolValueToUse.Value ? 1 : 0);
               else if ((dataType == IFCDataPrimitiveType.Logical) && (logicalValueToUse != IFCLogical.Unknown))
                  parametersToSet.AddDoubleParameter(existingParameter, (logicalValueToUse == IFCLogical.True) ? 1 : 0);
               break;
         }
      }
   }
}