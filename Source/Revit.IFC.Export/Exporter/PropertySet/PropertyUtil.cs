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
using Revit.IFC.Export.Toolkit;
using System.ComponentModel;
using System.Linq;
using System.Collections;

namespace Revit.IFC.Export.Exporter.PropertySet
{
   /// <summary>
   /// Provides static methods to create varies IFC properties.
   /// </summary>
   public class PropertyUtil
   {
      private static ISet<IFCEntityType> m_EntitiesWithNoRelatedType = null;

      /// <summary>
      /// Get a list of IFC entities that have no related type before IFC4
      /// </summary>
      public static ISet<IFCEntityType> EntitiesWithNoRelatedType
      {
         get
         {
            if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
               return null;

            if (m_EntitiesWithNoRelatedType == null)
            {
               m_EntitiesWithNoRelatedType = new HashSet<IFCEntityType>();
               m_EntitiesWithNoRelatedType.Add(IFCEntityType.IfcFooting);
               m_EntitiesWithNoRelatedType.Add(IFCEntityType.IfcPile);
               m_EntitiesWithNoRelatedType.Add(IFCEntityType.IfcRamp);
               m_EntitiesWithNoRelatedType.Add(IFCEntityType.IfcRoof);
               m_EntitiesWithNoRelatedType.Add(IFCEntityType.IfcStair);
            }

            return m_EntitiesWithNoRelatedType;
         }
      }

      public static IFCAnyHandle CreateCommonPropertyFromList(IFCFile file, string propertyName, IList<IFCData> valueList, PropertyValueType valueType, string unitTypeKey)
      {
         if (valueList == null || valueList.All(x => x == null))
            return null;

         IFCAnyHandle unitHnd = (!ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView && unitTypeKey != null) ? ExporterCacheManager.UnitsCache.FindUserDefinedUnit(unitTypeKey) : null;

         switch (valueType)
         {
            case PropertyValueType.EnumeratedValue:
               {
                  return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
               }
            case PropertyValueType.SingleValue:
               {
                  return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, valueList.First(), unitHnd);
               }
            case PropertyValueType.ListValue:
               {
                  return IFCInstanceExporter.CreatePropertyListValue(file, propertyName, null, valueList, unitHnd);
               }
            case PropertyValueType.BoundedValue:
               {
                  return CreateBoundedValuePropertyFromList(file, propertyName, valueList, unitTypeKey);
               }
            case PropertyValueType.TableValue:
               {
                  // for now is handled in CreatePropertyFromElementBase as Multiline Text
                  throw new InvalidOperationException("Unhandled table property!");
               }
            default:
               throw new InvalidOperationException("Missing case!");
         }
      }

      public static IFCAnyHandle CreateCommonProperty(IFCFile file, string propertyName, IFCData valueData, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateCommonPropertyFromList(file, propertyName, new List<IFCData>() { valueData }, valueType, unitTypeKey);
      }

      /// <summary>
      /// Creates an IfcPropertyBoundedValue.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="propertyName">The name.</param>
      /// <param name="valueDataList">The list of values.</param>
      /// <param name="unitTypeKey">The unit name.</param>
      protected static IFCAnyHandle CreateBoundedValuePropertyFromList(IFCFile file, string propertyName, IList<IFCData> valueDataList, string unitTypeKey)
      {
         if (valueDataList.Count < 1)
            throw new InvalidOperationException("Invalid bounded property!");
         IFCAnyHandle unitHnd = (!ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView && unitTypeKey != null) ? ExporterCacheManager.UnitsCache.FindUserDefinedUnit(unitTypeKey) : null;

         IFCData setPointValue = valueDataList[0];
         IFCData upperBoundValue = valueDataList.Count > 1 ? valueDataList[1] : null;
         IFCData lowerBoundValue = valueDataList.Count > 2 ? valueDataList[2] : null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 && upperBoundValue == null && lowerBoundValue == null)
         {
            // In IFC2x3, IfcPropertyBoundedValue has no SetPointValue attribute and upper/lower values should satisfy the rule WR22 : EXISTS(UpperBoundValue) OR EXISTS(LowerBoundValue);
            return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, setPointValue, null);
         }
         else
         {
            return IFCInstanceExporter.CreatePropertyBoundedValue(file, propertyName, null, lowerBoundValue, upperBoundValue, setPointValue, unitHnd);
         }
      }

      /// <summary>
      /// Creates an IfcPropertyTableValue.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="propertyName">The name.</param>
      /// <param name="definingValues">The defining values of the property.</param>
      /// <param name="definedValues">The defined values of the property.</param>
      /// <param name="definingUnitTypeKey">Unit for the defining values.</param>
      /// <param name="definedUnitTypeKey">Unit for the defined values.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTableProperty(IFCFile file, string propertyName, IList<IFCData> definingValues, IList<IFCData> definedValues, string definingUnitTypeKey, string definedUnitTypeKey)
      {
         IFCAnyHandle definingUnitHnd = (!ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView && definingUnitTypeKey != null) ? ExporterCacheManager.UnitsCache.FindUserDefinedUnit(definingUnitTypeKey) : null;
         IFCAnyHandle definedUnitHnd = (!ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView && definedUnitTypeKey != null) ? ExporterCacheManager.UnitsCache.FindUserDefinedUnit(definedUnitTypeKey) : null;

         return IFCInstanceExporter.CreatePropertyTableValue(file, propertyName, null, definingValues, definedValues, definingUnitHnd, definedUnitHnd);
      }

      /// <summary>
      /// Create a label property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLabelProperty(IFCFile file, string propertyName, string value, PropertyValueType valueType,
          Type propertyEnumerationType)
      {
         switch (valueType)
         {
            case PropertyValueType.EnumeratedValue:
               {
                  IList<IFCData> valueList = new List<IFCData>();

                  if (string.IsNullOrEmpty(value))
                     return null;

                  bool hasOther = false;

                  string[] subValues = value.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                  foreach (string item in subValues)
                  {
                     string validatedString = IFCDataUtil.ValidateEnumeratedValue(item, propertyEnumerationType);
                     if (validatedString == null && !hasOther)
                     {
                        // Use other if it exists and we haven't already used it.
                        validatedString = IFCDataUtil.ValidateEnumeratedValue("Other", propertyEnumerationType);
                        if (validatedString == null)
                           continue;
                        else
                           hasOther = true;
                     }

                     valueList.Add(IFCDataUtil.CreateAsLabel(validatedString));
                  }

                  if (valueList.Count == 0)
                     return null;

                  return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
               }
            case PropertyValueType.SingleValue:
               return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, IFCDataUtil.CreateAsLabel(value), null);
            case PropertyValueType.ListValue:
               {
                  IList<IFCData> valueList = new List<IFCData>();
                  valueList.Add(IFCDataUtil.CreateAsLabel(value));
                  return IFCInstanceExporter.CreatePropertyListValue(file, propertyName, null, valueList, null);
               }
            default:
               throw new InvalidOperationException("Missing case!");
         }
      }

      /// <summary>
      /// Create a text property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTextProperty(IFCFile file, string propertyName, string value, PropertyValueType valueType)
      {
         IFCData textData = IFCDataUtil.CreateAsText(value);
         return CreateCommonProperty(file, propertyName, textData, valueType, null);
      }

      /// <summary>
      /// Create a text property, using the cached value if possible.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTextPropertyFromCache(IFCFile file, string propertyName, string value, PropertyValueType valueType)
      {
         bool canCache = (value == string.Empty);
         StringPropertyInfoCache stringInfoCache = null;
         IFCAnyHandle textHandle = null;

         if (canCache)
         {
            stringInfoCache = ExporterCacheManager.PropertyInfoCache.TextCache;
            textHandle = stringInfoCache.Find(null, propertyName, value);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(textHandle))
               return textHandle;
         }

         textHandle = CreateTextProperty(file, propertyName, value, valueType);

         if (canCache)
            stringInfoCache.Add(null, propertyName, value, textHandle);

         return textHandle;
      }

      /// <summary>
      /// Create a text property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTextPropertyFromElement(IFCFile file, Element elem, string revitParameterName, string ifcPropertyName, PropertyValueType valueType, Type propertyEnumerationType)
      {
         if (elem == null)
            return null;

         string propertyValue;
         if (ParameterUtil.GetStringValueFromElement(elem, revitParameterName, out propertyValue) != null)
            return CreateTextPropertyFromCache(file, ifcPropertyName, propertyValue, valueType);

         return null;
      }

      /// <summary>
      /// Create a text property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTextPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
         BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType, Type propertyEnumerationType)
      {
         // For Instance
         IFCAnyHandle propHnd = CreateTextPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType,
             propertyEnumerationType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateTextPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType, propertyEnumerationType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a label property, using the cached value if possible.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="parameterId">The id of the parameter that generated the value.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="cacheAllStrings">Whether to cache all strings (true), or only the empty string (false).</param>
      /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLabelPropertyFromCache(IFCFile file, ElementId parameterId, string propertyName, string value, PropertyValueType valueType,
          bool cacheAllStrings, Type propertyEnumerationType)
      {
         bool canCache = (value == String.Empty) || cacheAllStrings;
         StringPropertyInfoCache stringInfoCache = null;
         IFCAnyHandle labelHandle = null;

         if (canCache)
         {
            stringInfoCache = ExporterCacheManager.PropertyInfoCache.LabelCache;
            labelHandle = stringInfoCache.Find(parameterId, propertyName, value);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(labelHandle))
               return labelHandle;
         }

         labelHandle = CreateLabelProperty(file, propertyName, value, valueType, propertyEnumerationType);

         if (canCache)
            stringInfoCache.Add(parameterId, propertyName, value, labelHandle);

         return labelHandle;
      }

      /// <summary>
      /// Create a label property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLabelProperty(IFCFile file, string propertyName, IList<string> values, PropertyValueType valueType,
          Type propertyEnumerationType)
      {
         switch (valueType)
         {
            case PropertyValueType.EnumeratedValue:
               {
                  IList<IFCData> valueList = new List<IFCData>();
                  foreach (string value in values)
                  {
                     valueList.Add(IFCDataUtil.CreateAsLabel(value));
                  }
                  return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
               }
            case PropertyValueType.ListValue:
               {
                  IList<IFCData> valueList = new List<IFCData>();
                  foreach (string value in values)
                  {
                     valueList.Add(IFCDataUtil.CreateAsLabel(value));
                  }
                  return IFCInstanceExporter.CreatePropertyListValue(file, propertyName, null, valueList, null);
               }
            default:
               throw new InvalidOperationException("Missing case!");
         }
      }

      /// <summary>
      /// Create an identifier property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIdentifierProperty(IFCFile file, string propertyName, string value, PropertyValueType valueType)
      {
         IFCData idData = IFCDataUtil.CreateAsIdentifier(value);
         return CreateCommonProperty(file, propertyName, idData, valueType, null);
      }

      /// <summary>
      /// Create an identifier property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIdentifierPropertyFromCache(IFCFile file, string propertyName, string value, PropertyValueType valueType)
      {
         StringPropertyInfoCache stringInfoCache = ExporterCacheManager.PropertyInfoCache.IdentifierCache;
         IFCAnyHandle stringHandle = stringInfoCache.Find(null, propertyName, value);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(stringHandle))
            return stringHandle;

         stringHandle = CreateIdentifierProperty(file, propertyName, value, valueType);

         stringInfoCache.Add(null, propertyName, value, stringHandle);
         return stringHandle;
      }

      /// <summary>
      /// Create a boolean property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateBooleanProperty(IFCFile file, string propertyName, bool value, PropertyValueType valueType)
      {
         IFCData boolData = IFCDataUtil.CreateAsBoolean(value);
         return CreateCommonProperty(file, propertyName, boolData, valueType, null);
      }

      /// <summary>
      /// Create a logical property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLogicalProperty(IFCFile file, string propertyName, IFCLogical value, PropertyValueType valueType)
      {
         IFCData logicalData = IFCDataUtil.CreateAsLogical(value);
         return CreateCommonProperty(file, propertyName, logicalData, valueType, null);
      }

      /// <summary>
      /// Create a boolean property or gets one from cache.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="propertyName">The property name.</param>
      /// <param name="value">The value.</param>
      /// <param name="valueType">The value type.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateBooleanPropertyFromCache(IFCFile file, string propertyName, bool value, PropertyValueType valueType)
      {
         BooleanPropertyInfoCache boolInfoCache = ExporterCacheManager.PropertyInfoCache.BooleanCache;
         IFCAnyHandle boolHandle = boolInfoCache.Find(propertyName, value);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(boolHandle))
            return boolHandle;

         boolHandle = CreateBooleanProperty(file, propertyName, value, valueType);
         boolInfoCache.Add(propertyName, value, boolHandle);
         return boolHandle;
      }

      /// <summary>
      /// Create a logical property or gets one from cache.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="propertyName">The property name.</param>
      /// <param name="value">The value.</param>
      /// <param name="valueType">The value type.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLogicalPropertyFromCache(IFCFile file, string propertyName, IFCLogical value, PropertyValueType valueType)
      {
         LogicalPropertyInfoCache logicalInfoCache = ExporterCacheManager.PropertyInfoCache.LogicalCache;
         IFCAnyHandle logicalHandle = logicalInfoCache.Find(propertyName, value);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(logicalHandle))
            return logicalHandle;

         logicalHandle = CreateLogicalProperty(file, propertyName, value, valueType);
         logicalInfoCache.Add(propertyName, value, logicalHandle);
         return logicalHandle;
      }

      /// <summary>
      /// Create an integer property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIntegerProperty(IFCFile file, string propertyName, int value, PropertyValueType valueType)
      {
         IFCData intData = IFCDataUtil.CreateAsInteger(value);
         return CreateCommonProperty(file, propertyName, intData, valueType, null);
      }

      /// <summary>
      /// Create an integer property or gets one from cache.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="propertyName">The property name.</param>
      /// <param name="value">The value.</param>
      /// <param name="valueType">The value type.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIntegerPropertyFromCache(IFCFile file, string propertyName, int value, PropertyValueType valueType)
      {
         bool canCache = (value >= -10 && value <= 10);
         IFCAnyHandle intHandle = null;
         IntegerPropertyInfoCache intInfoCache = null;
         if (canCache)
         {
            intInfoCache = ExporterCacheManager.PropertyInfoCache.IntegerCache;
            intHandle = intInfoCache.Find(propertyName, value);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(intHandle))
               return intHandle;
         }

         intHandle = CreateIntegerProperty(file, propertyName, value, valueType);
         if (canCache)
         {
            intInfoCache.Add(propertyName, value, intHandle);
         }
         return intHandle;
      }

      internal static double? CanCacheDouble(double value)
      {
         // We have a partial cache here
         // Cache multiples of +/- 0.05 up to 10.
         // Cache multiples of +/- 0.5 up to 300.
         // Cache multiples of +/- 5 reset.

         if (MathUtil.IsAlmostZero(value))
            return 0.0;

         double multiplier = 5.0;
         if (Math.Abs(value) <= 10.0 + MathUtil.Eps())
            multiplier = 0.05;
         else if (Math.Abs(value) <= 300.0 + MathUtil.Eps())
            multiplier = 0.5;

         double valueCorrected = Math.Floor(value / multiplier + MathUtil.Eps());
         if (MathUtil.IsAlmostZero(value / multiplier - valueCorrected))
            return valueCorrected * multiplier;

         return null;
      }

      /// <summary>Create a count measure property.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateCountMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData countData = IFCDataUtil.CreateAsCountMeasure(value);
         return CreateCommonProperty(file, propertyName, countData, valueType, null);
      }

      /// <summary>Create a count measure property. From IFC4x3 onward the value has been changed to Integer</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateCountMeasureProperty(IFCFile file, string propertyName, int value, PropertyValueType valueType)
      {
         IFCData countData = IFCDataUtil.CreateAsCountMeasure(value);
         return CreateCommonProperty(file, propertyName, countData, valueType, null);
      }

      /// <summary>Create a ClassificationReference property.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateClassificationReferenceProperty(IFCFile file, string propertyName, string value)
      {
         IFCAnyHandle classificationReferenceHandle =
            IFCInstanceExporter.CreateClassificationReference(file, null, value, null, null, null);
         return IFCInstanceExporter.CreatePropertyReferenceValue(file, propertyName, null, null, classificationReferenceHandle);
      }

      /// <summary>
      /// Create a Time measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTimePropertyFromElement(IFCFile file, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, elem, revitParameterName, ifcPropertyName,
            "IfcTimeMeasure", SpecTypeId.Time, valueType);
      }

      public static IFCAnyHandle CreateUserDefinedRealPropertyFromElement(IFCFile file, Element elem,
         string revitParameterName, string ifcPropertyName, PropertyValueType valueType, ForgeTypeId specType, string unitTypeKey)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, specType, valueType);
         return CreateRealProperty(file, ifcPropertyName, doubleValues, valueType, unitTypeKey);
      }

      /// <summary>
      /// Create a currency measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateCurrencyPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         string measureName = ExporterCacheManager.UnitsCache.ContainsKey("CURRENCY") ? "IfcMonetaryMeasure" : "IfcReal";

         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Number, valueType);
         return CreateRealProperty(file, ifcPropertyName, doubleValues, valueType, measureName);
      }

      public static IFCAnyHandle CreateUserDefinedRealPropertyFromElement(IFCFile file, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType, ForgeTypeId specType, string unitTypeKey)
      {
         IFCAnyHandle propHnd = CreateUserDefinedRealPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType, specType, unitTypeKey);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateUserDefinedRealPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType, specType, unitTypeKey);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a currency property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateCurrencyPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateCurrencyPropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateCurrencyPropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a Time property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTimePropertyFromElement(IFCFile file, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateTimePropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateTimePropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create an IfcClassificationReference property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateClassificationReferencePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName)
      {
         if (elem == null)
            return null;

         string propertyValue;
         if (ParameterUtil.GetStringValueFromElement(elem, revitParameterName, out propertyValue) != null)
            return CreateClassificationReferenceProperty(file, ifcPropertyName, propertyValue);

         return null;
      }

      /// <summary>
      /// Create a generic measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="measureType">The IfcMeasure type of the property.</param>
      /// <param name="specTypeId">Identifier of the property spec.</param>
      /// <param name="valueType">The property value type of the property.</param>
      /// <returns>The created property handle.</returns>
      private static IFCAnyHandle CreateDoublePropertyFromElement(IFCFile file, Element elem,
          string revitParameterName, string ifcPropertyName, string measureType, ForgeTypeId specTypeId, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, specTypeId, valueType);

         IList<IFCData> doubleData = new List<IFCData>();
         foreach (var val in doubleValues)
            doubleData.Add(val.HasValue ? IFCData.CreateDoubleOfType(val.Value, measureType) : null);

         return CreateCommonPropertyFromList(file, ifcPropertyName, doubleData, valueType, null);
      }

      /// <summary>
      /// Create an IfcClassificationReference property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateClassificationReferencePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName)
      {
         IFCAnyHandle propHnd = CreateClassificationReferencePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateClassificationReferencePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a label property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLabelPropertyFromElement(IFCFile file, Element elem, string revitParameterName, string ifcPropertyName,
          PropertyValueType valueType, Type propertyEnumerationType)
      {
         if (elem == null)
            return null;

         string propertyValue;
         Parameter parameter = ParameterUtil.GetStringValueFromElement(elem, revitParameterName, out propertyValue);
         if (parameter != null)
            return CreateLabelPropertyFromCache(file, parameter.Id, ifcPropertyName, propertyValue, valueType, false, propertyEnumerationType);

         return null;
      }

      /// <summary>
      /// Create a label property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLabelPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
         BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType, Type propertyEnumerationType)
      {
         // For Instance
         IFCAnyHandle propHnd = CreateLabelPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType,
             propertyEnumerationType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateLabelPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType, propertyEnumerationType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create an identifier property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIdentifierPropertyFromElement(IFCFile file, Element elem, string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         if (elem == null)
            return null;

         string propertyValue;
         if (ParameterUtil.GetStringValueFromElement(elem, revitParameterName, out propertyValue) != null)
            return CreateIdentifierPropertyFromCache(file, ifcPropertyName, propertyValue, valueType);

         return null;
      }

      /// <summary>
      /// Create an identifier property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIdentifierPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
         BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         // For Instance
         IFCAnyHandle propHnd = CreateIdentifierPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateIdentifierPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a boolean property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateBooleanPropertyFromElement(IFCFile file, Element elem,
         string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         int propertyValue;
         if (ParameterUtil.GetIntValueFromElement(elem, revitParameterName, out propertyValue) != null)
            return CreateBooleanPropertyFromCache(file, ifcPropertyName, propertyValue != 0, valueType);
         if (ParameterUtil.GetIntValueFromElement(elem, ifcPropertyName, out propertyValue) != null)
            return CreateBooleanPropertyFromCache(file, ifcPropertyName, propertyValue != 0, valueType);

         return null;
      }

      /// <summary>
      /// Create a logical property from the element's or type's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLogicalPropertyFromElement(IFCFile file, Element elem,
         string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCLogical ifcLogical = IFCLogical.Unknown;
         int propertyValue;
         if (ParameterUtil.GetIntValueFromElement(elem, revitParameterName, out propertyValue) != null)
         {
            ifcLogical = propertyValue != 0 ? IFCLogical.True : IFCLogical.False;
         }

         return CreateLogicalPropertyFromCache(file, ifcPropertyName, ifcLogical, valueType);
      }

      /// <summary>
      /// Create an integer property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIntegerPropertyFromElement(IFCFile file, Element elem,
         string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         int propertyValue;
         if (ParameterUtil.GetIntValueFromElement(elem, revitParameterName, out propertyValue) != null)
            return CreateIntegerPropertyFromCache(file, ifcPropertyName, propertyValue, valueType);

         return null;
      }

      /// <summary>
      /// Create a ratio measure data from string value.
      /// </summary>
      /// <param name="values">The values of the property.</param>
      /// <returns>The created property data.</returns>
      public static IFCData CreateRatioMeasureDataFromString(string value)
      {
         double propertyValue;
         if (Double.TryParse(value, out propertyValue))
            return IFCDataUtil.CreateRatioMeasureData(propertyValue);

         return null;
      }



      /// <summary>
      /// Create a normalised ratio measure data from string value.
      /// </summary>
      /// <param name="values">The values of the property.</param>
      /// <returns>The created property data.</returns>
      public static IFCData CreateNormalisedRatioMeasureDataFromString(string value)
      {
         double propertyValue;
         if (Double.TryParse(value, out propertyValue))
            return IFCDataUtil.CreateNormalisedRatioMeasureData(propertyValue);

         return null;
      }


      /// <summary>
      /// Create a positive ratio measure data from string value.
      /// </summary>
      /// <param name="values">The values of the property.</param>
      /// <returns>The created property data.</returns>
      public static IFCData CreatePositiveRatioMeasureDataFromString(string value)
      {
         double propertyValue;
         if (Double.TryParse(value, out propertyValue))
            return IFCDataUtil.CreatePositiveRatioMeasureData(propertyValue);

         return null;
      }

      /// <summary>
      /// Create a count measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateCountMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         int propertyValue;
         double propertyValueReal;
         if (ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValueReal) != null)
         {
            if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4x3)
            {
               return CreateCountMeasureProperty(file, ifcPropertyName, propertyValueReal, valueType);
            }
            else if (MathUtil.IsAlmostInteger(propertyValueReal))
            {
               propertyValue = (int)Math.Floor(propertyValueReal);
               return CreateCountMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            }
         }

         if (ParameterUtil.GetIntValueFromElement(elem, revitParameterName, out propertyValue) != null)
            return CreateCountMeasureProperty(file, ifcPropertyName, propertyValue, valueType);

         return null;
      }

      /// <summary>
      /// Create a count measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateCountMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateCountMeasurePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateCountMeasurePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Creates the shared beam and column QTO values.  
      /// </summary>
      /// <remarks>
      /// This code uses the native implementation for creating these quantities, and the native class for storing the information.
      /// This will be obsoleted.
      /// </remarks>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="elemHandle">The element handle.</param>
      /// <param name="element">The beam or column element.</param>
      /// <param name="typeInfo">The FamilyTypeInfo containing the appropriate data.</param>
      /// <param name="geomObjects">The list of geometries for the exported column only, used if split walls and columns is set.</param>
      /// <remarks>The geomObjects is used if we have the split by level option.  It is intended only for columns, as beams and members are not split by level.  
      /// In this case, we use the solids in the list to determine the real volume of the exported objects. If the list contains meshes, we won't export the volume at all.</remarks>
      public static void CreateBeamColumnBaseQuantities(ExporterIFC exporterIFC, IFCAnyHandle elemHandle, Element element, FamilyTypeInfo typeInfo, IList<GeometryObject> geomObjects)
      {
         // Make sure QTO export is enabled.
         if (!ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities || (ExporterCacheManager.ExportOptionsCache.ExportAsCOBIE))
            return;

         IFCFile file = exporterIFC.GetFile();
         HashSet<IFCAnyHandle> quantityHnds = new HashSet<IFCAnyHandle>();
         double scaledLength = typeInfo.extraParams.ScaledLength;
         //According to investigation of current code the passed in typeInfo contains grossArea
         double scaledGrossArea = typeInfo.extraParams.ScaledArea;
         double crossSectionArea = scaledGrossArea;
         double scaledOuterPerimeter = typeInfo.extraParams.ScaledOuterPerimeter;
         double scaledInnerPerimeter = typeInfo.extraParams.ScaledInnerPerimeter;
         double outSurfaceArea = 0.0;

         if (scaledLength > MathUtil.Eps())
         {
            IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, "Length", null, null, scaledLength);
            quantityHnds.Add(quantityHnd);
         }

         if (MathUtil.AreaIsAlmostZero(crossSectionArea))
         {
            if (element != null)
            {
               ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.HOST_AREA_COMPUTED, out crossSectionArea);
               crossSectionArea = UnitUtil.ScaleArea(crossSectionArea);
            }
         }

         if (!MathUtil.AreaIsAlmostZero(crossSectionArea))
         {
            IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, "CrossSectionArea", null, null, crossSectionArea);
            quantityHnds.Add(quantityHnd);
         }

         if (!MathUtil.AreaIsAlmostZero(scaledGrossArea) && !MathUtil.IsAlmostZero(scaledLength) && !MathUtil.IsAlmostZero(scaledOuterPerimeter))
         {
            double scaledPerimeter = scaledOuterPerimeter + scaledInnerPerimeter;
            //According to the IFC documentation, OuterSurfaceArea does not include the end caps area, only Length * Perimeter
            outSurfaceArea = UnitUtil.ScaleArea(UnitUtil.UnscaleLength(scaledLength) * UnitUtil.UnscaleLength(scaledPerimeter));
            IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, "OuterSurfaceArea", null, null, outSurfaceArea);
            quantityHnds.Add(quantityHnd);
         }

         // Compute GrossSurfaceArea if both CrossSectionAre and OuterSurfaceArea cannot be determined separately
         if (MathUtil.AreaIsAlmostZero(crossSectionArea) && MathUtil.AreaIsAlmostZero(outSurfaceArea))
         {
            double scaledPerimeter = scaledOuterPerimeter + scaledInnerPerimeter;
            double grossSurfaceArea = scaledGrossArea * 2 + UnitUtil.ScaleArea(UnitUtil.UnscaleLength(scaledLength) * UnitUtil.UnscaleLength(scaledPerimeter));
            IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, "GrossSurfaceArea", null, null, grossSurfaceArea);
            quantityHnds.Add(quantityHnd);
         }

         double grossVolume = UnitUtil.ScaleVolume(UnitUtil.UnscaleLength(scaledLength) * UnitUtil.UnscaleArea(scaledGrossArea));
         double netVolume = 0.0;
         if (element != null)
         {
            // If we are splitting columns, we will look at the actual geometry used when exporting this segment
            // of the column, but only if we have the geomObjects passed in.
            if (geomObjects != null && ExporterCacheManager.ExportOptionsCache.WallAndColumnSplitting)
            {
               foreach (GeometryObject geomObj in geomObjects)
               {
                  // We don't suport calculating the volume of Meshes at this time.
                  if (geomObj is Mesh)
                  {
                     netVolume = 0.0;
                     break;
                  }

                  if (geomObj is Solid)
                     netVolume += (geomObj as Solid).Volume;
               }
            }
            else
               ParameterUtil.GetDoubleValueFromElement(element, BuiltInParameter.HOST_VOLUME_COMPUTED, out netVolume);
            netVolume = UnitUtil.ScaleVolume(netVolume);
         }

         if (!MathUtil.VolumeIsAlmostZero(grossVolume))
         {
            IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityVolume(file, "GrossVolume", null, null, grossVolume);
            quantityHnds.Add(quantityHnd);
         }

         if (!MathUtil.VolumeIsAlmostZero(netVolume))
         {
            IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityVolume(file, "NetVolume", null, null, netVolume);
            quantityHnds.Add(quantityHnd);
         }

         string quantitySetName = string.Empty;
         if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
         {
            if (IFCAnyHandleUtil.IsSubTypeOf(elemHandle, Common.Enums.IFCEntityType.IfcColumn))
               quantitySetName = "Qto_ColumnBaseQuantities";
            if (IFCAnyHandleUtil.IsSubTypeOf(elemHandle, Common.Enums.IFCEntityType.IfcBeam))
               quantitySetName = "Qto_BeamBaseQuantities";
            if (IFCAnyHandleUtil.IsSubTypeOf(elemHandle, Common.Enums.IFCEntityType.IfcMember))
               quantitySetName = "Qto_MemberBaseQuantities";
         }
         CreateAndRelateBaseQuantities(file, exporterIFC, elemHandle, quantityHnds, quantitySetName);
      }

      /// <summary>
      /// Creates the spatial element quantities required by GSA before COBIE and adds them to the export.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="elemHnd">The element handle.</param>
      /// <param name="quantityName">The quantity name.</param>
      /// <param name="areaName">The area name.</param>
      /// <param name="area">The area.</param>
      public static void CreatePreCOBIEGSAQuantities(ExporterIFC exporterIFC, IFCAnyHandle elemHnd, string quantityName, string areaName, double area)
      {
         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
         IFCAnyHandle areaQuantityHnd = IFCInstanceExporter.CreateQuantityArea(file, quantityName, null, null, area);
         HashSet<IFCAnyHandle> areaQuantityHnds = new HashSet<IFCAnyHandle>();
         areaQuantityHnds.Add(areaQuantityHnd);

         PropertyUtil.CreateAndRelateBaseQuantities(file, exporterIFC, elemHnd, areaQuantityHnds, quantityName, null, areaName);
      }

      /// <summary>
      /// Creates the opening quantities and adds them to the export.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="openingElement">The opening element handle.</param>
      /// <param name="extraParams">The extrusion creation data.</param>
      public static void CreateOpeningQuantities(ExporterIFC exporterIFC, IFCAnyHandle openingElement, IFCExportBodyParams extraParams)
      {
         IFCFile file = exporterIFC.GetFile();
         HashSet<IFCAnyHandle> quantityHnds = new HashSet<IFCAnyHandle>();
         if (extraParams.ScaledLength > MathUtil.Eps())
         {
            IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, "Depth", null, null, extraParams.ScaledLength);
            quantityHnds.Add(quantityHnd);
         }
         if (extraParams.ScaledHeight > MathUtil.Eps())
         {
            IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, "Height", null, null, extraParams.ScaledHeight);
            quantityHnds.Add(quantityHnd);
            quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, "Width", null, null, extraParams.ScaledWidth);
            quantityHnds.Add(quantityHnd);
         }
         else if (extraParams.ScaledArea > MathUtil.Eps())
         {
            IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, "Area", null, null, extraParams.ScaledArea);
            quantityHnds.Add(quantityHnd);
         }

         string quantitySetName = string.Empty;
         if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
         {
            quantitySetName = "Qto_OpeningElementBaseQuantities";
         }
         CreateAndRelateBaseQuantities(file, exporterIFC, openingElement, quantityHnds, quantitySetName);
      }

      /// <summary>
      /// Creates and caches area and volume base quantities for slabs.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="slabHnd">The slab handle.</param>
      /// <param name="extrusionData">The IFCExportBodyParams containing the slab extrusion creation data.</param>
      /// <param name="outerCurveLoop">The slab outer loop.</param>
      public static void CreateSlabBaseQuantities(ExporterIFC exporterIFC, IFCAnyHandle slabHnd, IFCExportBodyParams extrusionData, CurveLoop outerCurveLoop)
      {
         if (extrusionData != null)
         {
            IFCFile file = exporterIFC.GetFile();
            HashSet<IFCAnyHandle> quantityHnds = new HashSet<IFCAnyHandle>();

            double netArea = extrusionData.ScaledArea;
            if (!MathUtil.IsAlmostZero(netArea))
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, "NetArea", null, null, netArea);
               quantityHnds.Add(quantityHnd);
            }

            //The length, area and volume may have different base length units, it safer to unscale and rescale the results.
            double unscaledArea = UnitUtil.UnscaleArea(netArea);
            double unscaledLength = UnitUtil.UnscaleLength(extrusionData.ScaledLength);
            double netVolume = UnitUtil.ScaleVolume(unscaledArea * unscaledLength);
            if (!MathUtil.IsAlmostZero(netVolume))
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, "NetVolume", null, null, netVolume);
               quantityHnds.Add(quantityHnd);
            }

            if (outerCurveLoop != null)
            {
               double unscaledSlabGrossArea = ExporterIFCUtils.ComputeAreaOfCurveLoops(new List<CurveLoop>() { outerCurveLoop });
               double scaledSlabGrossArea = UnitUtil.ScaleArea(unscaledSlabGrossArea);
               if (!MathUtil.IsAlmostZero(scaledSlabGrossArea))
               {
                  IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, "GrossArea", null, null, scaledSlabGrossArea);
                  quantityHnds.Add(quantityHnd);
               }

               double grossVolume = UnitUtil.ScaleVolume(unscaledArea * unscaledLength);
               if (!MathUtil.IsAlmostZero(grossVolume))
               {
                  IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, "GrossVolume", null, null, grossVolume);
                  quantityHnds.Add(quantityHnd);
               }
            }

            ExporterCacheManager.BaseQuantitiesCache.Add(slabHnd, quantityHnds);
         }
      }
      /// <summary>
      /// Creates the wall base quantities and adds them to the export.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="wallElement">The wall element.</param>
      /// <param name="wallHnd">The wall handle.</param>
      /// <param name="solids">The list of solids for the entity created for the wall element.</param>
      /// <param name="meshes">The list of meshes for the entity created for the wall element.</param>
      /// <param name="scaledLength">The scaled length.</param>
      /// <param name="scaledDepth">The scaled depth.</param>
      /// <param name="scaledFootPrintArea">The scaled foot print area.</param>
      /// <remarks>If we are splitting walls by level, the list of solids and meshes represent the currently
      /// exported section of wall, not the entire wall.</remarks>
      public static void CreateWallBaseQuantities(ExporterIFC exporterIFC, Wall wallElement,
          IList<Solid> solids, IList<Mesh> meshes,
          IFCAnyHandle wallHnd,
          double scaledLength, double scaledDepth, double scaledFootPrintArea,
          IFCExportBodyParams extrusionData, HashSet<IFCAnyHandle> widthAsComplexQty = null)
      {
         IFCFile file = exporterIFC.GetFile();
         HashSet<IFCAnyHandle> quantityHnds = new HashSet<IFCAnyHandle>();
         if (scaledDepth > MathUtil.Eps())
         {
            IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, "Height", null, null, scaledDepth);
            quantityHnds.Add(quantityHnd);
         }

         if (!MathUtil.IsAlmostZero(scaledLength))
         {
            IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, "Length", null, null, scaledLength);
            quantityHnds.Add(quantityHnd);
         }
         else if (wallElement.Location != null)
         {
            Curve wallAxis = (wallElement.Location as LocationCurve).Curve;
            if (wallAxis != null)
            {
               double axisLength = UnitUtil.ScaleLength(wallAxis.Length);
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, "Length", null, null, axisLength);
               quantityHnds.Add(quantityHnd);
            }
         }


         double scaledWidth = 0.0;
         if (wallElement != null)
         {
            scaledWidth = UnitUtil.ScaleLength(wallElement.Width);
            if (!MathUtil.IsAlmostZero(scaledWidth))
            {
               if ((widthAsComplexQty?.Count ?? 0) == 0)
               {
                  IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, "Width", null, null, scaledWidth);
                  quantityHnds.Add(quantityHnd);
               }
               else
               {
                  quantityHnds.UnionWith(widthAsComplexQty);
               }
            }
         }

         if (!MathUtil.IsAlmostZero(scaledFootPrintArea))
         {
            IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, "GrossFootprintArea", null, null, scaledFootPrintArea);
            quantityHnds.Add(quantityHnd);
         }

         double netArea = 0;
         double grossArea = 0;
         double volume = 0;

         // We will only assign the area if we have all solids that we are exporting; we won't bother calcuting values for Meshes.
         if (solids != null && (meshes == null || meshes.Count == 0))
         {
            //To determine the side of the wall that is suitable for calculating BaseQuantities, 
            //we group the faces by normal and calculate the total area of each side.
            Dictionary<XYZ, (List<Face>, double)> wallSides = new Dictionary<XYZ, (List<Face>, double)>();
            foreach (Solid solid in solids)
            {
               foreach (Face face in solid.Faces)
               {
                  XYZ faceNormal = face.ComputeNormal(new UV(0, 0));
                  if (MathUtil.IsAlmostZero(faceNormal.Z))
                  {
                     double faceArea = face.Area;
                     if (wallSides.Any())
                     {
                        bool faceAdded = false;
                        foreach (var wallSide in wallSides)
                        {
                           if (faceNormal.IsAlmostEqualTo(wallSide.Key))
                           {
                              List<Face> sideFaces = wallSide.Value.Item1;
                              sideFaces.Add(face);
                              double sumArea = wallSide.Value.Item2 + faceArea;
                              wallSides[wallSide.Key] = ( sideFaces, sumArea);
                              faceAdded = true;
                              break;
                           }
                        }
                        if(!faceAdded)
                        {
                           wallSides.Add(faceNormal, (new List<Face> { face }, face.Area));
                        }
                     }
                     else
                     {
                        wallSides.Add(faceNormal, (new List<Face> { face }, face.Area));
                     }
                  }
               }
               volume += solid.Volume;
            }

            KeyValuePair<XYZ, (List<Face>, double)> largestSide = new KeyValuePair<XYZ, (List<Face>, double)>();
            foreach (var wallSide in wallSides)
            {
               if (wallSide.Value.Item2 > largestSide.Value.Item2)
                  largestSide = wallSide;
            }

            List<Face> facesOfLargestWallSide = largestSide.Value.Item1;
            netArea = largestSide.Value.Item2;

            foreach (Face face in facesOfLargestWallSide)
            {
               double largestFaceGrossArea = 0.0;
               IList<CurveLoop> fCurveLoops = face.GetEdgesAsCurveLoops();
               for (int ii = 0; ii < fCurveLoops.Count; ii++)
               {
                  double grArea = ExporterIFCUtils.ComputeAreaOfCurveLoops(new List<CurveLoop>() { fCurveLoops[ii] });
                  if (grArea > largestFaceGrossArea)
                     largestFaceGrossArea = grArea;
               }
               grossArea += largestFaceGrossArea;
            }
         }

         netArea = UnitUtil.ScaleArea(netArea);
         grossArea = UnitUtil.ScaleArea(grossArea);
         volume = UnitUtil.ScaleVolume(volume);

         if (scaledDepth > MathUtil.Eps() && !MathUtil.IsAlmostZero(scaledWidth) && !MathUtil.IsAlmostZero(grossArea))
         {
            double grossVolume = UnitUtil.ScaleVolume(UnitUtil.UnscaleLength(scaledWidth) * UnitUtil.UnscaleArea(grossArea));
            IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityVolume(file, "GrossVolume", null, null, grossVolume);
            quantityHnds.Add(quantityHnd);
         }

         if (!MathUtil.IsAlmostZero(grossArea))
         {
            IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, "GrossSideArea", null, null, grossArea);
            quantityHnds.Add(quantityHnd);
         }

         if (!MathUtil.IsAlmostZero(netArea))
         {
            IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, "NetSideArea", null, null, netArea);
            quantityHnds.Add(quantityHnd);
         }

         if (!MathUtil.IsAlmostZero(volume))
         {
            IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityVolume(file, "NetVolume", null, null, volume);
            quantityHnds.Add(quantityHnd);
         }

         string quantitySetName = string.Empty;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            quantitySetName = "Qto_WallBaseQuantities";
         }

         CreateAndRelateBaseQuantities(file, exporterIFC, wallHnd, quantityHnds, quantitySetName);
      }

      /// <summary>
      /// Creates and relate base quantities to quantity handle.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="elemHnd">The element handle.</param>
      /// <param name="quantityHnds">The quantity handles.</param>
      static public void CreateAndRelateBaseQuantities(IFCFile file, ExporterIFC exporterIFC, IFCAnyHandle elemHnd, HashSet<IFCAnyHandle> quantityHnds,
         string quantitySetName = null, string description = null, string methodOfMeasurement = null)
      {
         if (quantityHnds.Count > 0)
         {
            if (string.IsNullOrEmpty(quantitySetName))
               quantitySetName = "BaseQuantities";
            IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

            // Skip if the elementHandle has the associated QuantitySet has been created before 
            if (!ExporterCacheManager.QtoSetCreated.Contains((elemHnd, quantitySetName)))
            {
               string quantityGuid = GUIDUtil.GenerateIFCGuidFrom(
                  GUIDUtil.CreateGUIDString(IFCEntityType.IfcElementQuantity, quantitySetName, elemHnd));
               IFCAnyHandle quantity = IFCInstanceExporter.CreateElementQuantity(file, elemHnd,
                  quantityGuid, ownerHistory, quantitySetName, description,
                  methodOfMeasurement, quantityHnds);
               HashSet<IFCAnyHandle> relatedObjects = new HashSet<IFCAnyHandle>();
               relatedObjects.Add(elemHnd);

               string quantityRelGuid = GUIDUtil.GenerateIFCGuidFrom(
                  GUIDUtil.CreateGUIDString(IFCEntityType.IfcRelDefinesByProperties, quantitySetName, elemHnd));
               ExporterUtil.CreateRelDefinesByProperties(file, quantityRelGuid, ownerHistory, null, null,
                  relatedObjects, quantity);
            }
         }
      }

      /// <summary>
      ///  Creates the shared beam, column and member QTO values.  
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="elemHandle">The element handle.</param>
      /// <param name="element">The element.</param>
      /// <param name="ecData">The IFCExportBodyParams containing the appropriate data.</param>
      public static void CreateBeamColumnMemberBaseQuantities(ExporterIFC exporterIFC, IFCAnyHandle elemHandle, Element element, IFCExportBodyParams ecData)
      {
         FamilyTypeInfo ifcTypeInfo = new FamilyTypeInfo() { extraParams = ecData };
         CreateBeamColumnBaseQuantities(exporterIFC, elemHandle, element, ifcTypeInfo, null);
      }

      /// <summary>
      /// True if QTO width and length values should be reversed.  
      /// </summary>
      /// <param name="elemHandle">The element handle.</param>
      public static bool IsWidthLengthReversed(IFCAnyHandle elemHandle)
      {
         return (IFCAnyHandleUtil.IsSubTypeOf(elemHandle, IFCEntityType.IfcSlab) || IFCAnyHandleUtil.IsSubTypeOf(elemHandle, IFCEntityType.IfcCovering));
      }

      /// <summary>
      /// Creates property sets for Revit groups and parameters, if export options is set.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="element">The Element.</param>
      /// <param name="elementSets">The collection of IFCAnyHandles to relate properties to.</param>
      /// <param name="forceCreate">Forces properties creation even if 'Export internal properties' is unchecked.</param>
      public static void CreateInternalRevitPropertySets(ExporterIFC exporterIFC, Element element,
         ISet<IFCAnyHandle> elementSets, bool forceCreate)
      {
         if (exporterIFC == null || element == null ||
             (!ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportInternalRevit && !forceCreate))
            return;

         // We will allow creating internal Revit property sets for element types with no associated element handles.
         if (((elementSets?.Count ?? 0) == 0) && !(element is ElementType))
            return;

         IFCFile file = exporterIFC.GetFile();

         ElementId typeId = element.GetTypeId();
         Element elementType = element.Document.GetElement(typeId);
         int whichStart = elementType != null ? 0 : (element is ElementType ? 1 : 0);
         if (whichStart == 1)
         {
            typeId = element.Id;
            elementType = element as ElementType;
         }

         SortedDictionary<string, (string, HashSet<IFCAnyHandle>)>[] propertySets;
         propertySets = new SortedDictionary<string, (string, HashSet<IFCAnyHandle>)>[2];
         propertySets[0] = new SortedDictionary<string, (string, HashSet<IFCAnyHandle>)>();
         propertySets[1] = new SortedDictionary<string, (string, HashSet<IFCAnyHandle>)>();

         // pass through: element and element type.  If the element is a ElementType, there will only be one pass.
         for (int which = whichStart; which < 2; which++)
         {
            Element whichElement = (which == 0) ? element : elementType;
            if (whichElement == null)
               continue;

            // If we have already processed this element, just add the new
            // IFC entities.
            if (ExporterCacheManager.CreatedInternalPropertySets.TryAppend(whichElement.Id, elementSets))
               continue;

            ElementId whichElementId = whichElement.Id;

            bool createType = (which == 1);
            if (createType)
            {
               if (ExporterCacheManager.TypePropertyInfoCache.HasTypeProperties(typeId))
                  continue;
            }

            IDictionary<string, ParameterElementCache> parameterElementCache =
                ParameterUtil.GetNonIFCParametersForElement(whichElementId);
            if (parameterElementCache == null)
               continue;

            foreach (KeyValuePair<string, ParameterElementCache> parameterElementGroup in parameterElementCache)
            {
               ForgeTypeId parameterGroup = new ForgeTypeId(parameterElementGroup.Key);
               string groupName = LabelUtils.GetLabelForGroup(parameterGroup);

               // We are only going to append the "(Type)" suffix if we aren't also exporting the corresponding entity type.
               // In general, we'd like to always export them entity type, regardles of whether it holds any geometry or not - it can hold
               // at least the parameteric information.  When this is acheived, when can get rid of this entirely.
               // Unfortunately, IFC2x3 doesn't have types for all entities, so for IFC2x3 at least this will continue to exist
               // in some fashion.
               // There was a suggestion in SourceForge that we could "merge" the instance/type property sets in the cases where we aren't
               // creating an entity type, and in the cases where two properties had the same name, use the instance over type.
               // However, given our intention to generally export all types, this seems like a lot of work for diminishing returns.
               if (whichElement is ElementType)
                  if (which == 1 && !ExporterCacheManager.ElementTypeToHandleCache.IsRegistered(whichElement as ElementType))
                     groupName += Properties.Resources.PropertySetTypeSuffix;

               HashSet<IFCAnyHandle> currPropertiesForGroup = new HashSet<IFCAnyHandle>();
               propertySets[which][parameterElementGroup.Key] = (groupName, currPropertiesForGroup);

               foreach (Parameter parameter in parameterElementGroup.Value.ParameterCache.Values)
               {
                  if (!parameter.HasValue)
                     continue;

                  Definition parameterDefinition = parameter.Definition;
                  if (parameterDefinition == null)
                     continue;

                  string parameterCaption = parameterDefinition.Name;

                  switch (parameter.StorageType)
                  {
                     case StorageType.None:
                        break;
                     case StorageType.Integer:
                        {
                           int value = parameter.AsInteger();
                           string valueAsString = parameter.AsValueString();

                           // YesNo or actual integer?
                           if (parameterDefinition.GetDataType() == SpecTypeId.Boolean.YesNo)
                           {
                              currPropertiesForGroup.Add(CreateBooleanPropertyFromCache(file, parameterCaption, value != 0, PropertyValueType.SingleValue));
                           }
                           else if (parameterDefinition.GetDataType().Empty() && (valueAsString != null))
                           {
                              // This is probably an internal enumerated type that should be exported as a string.
                              currPropertiesForGroup.Add(CreateIdentifierPropertyFromCache(file, parameterCaption, valueAsString, PropertyValueType.SingleValue));
                           }
                           else
                           {
                              currPropertiesForGroup.Add(CreateIntegerPropertyFromCache(file, parameterCaption, value, PropertyValueType.SingleValue));
                           }
                           break;
                        }
                     case StorageType.Double:
                        {
                           double value = parameter.AsDouble();
                           IFCAnyHandle propertyHandle = CreateRealPropertyBasedOnParameterType(file, parameter, parameterCaption, value, PropertyValueType.SingleValue);

                           if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propertyHandle))
                              currPropertiesForGroup.Add(propertyHandle);
                           break;
                        }
                     case StorageType.String:
                        {
                           string value = parameter.AsString();
                           if (!string.IsNullOrEmpty(value))
                              currPropertiesForGroup.Add(CreateTextPropertyFromCache(file, parameterCaption, value, PropertyValueType.SingleValue));
                           break;
                        }
                     case StorageType.ElementId:
                        {
                           if (parameter.AsElementId() != ElementId.InvalidElementId)
                           {
                              string valueString = parameter.AsValueString();
                              currPropertiesForGroup.Add(CreateLabelPropertyFromCache(file, parameter.Id, parameterCaption, valueString, PropertyValueType.SingleValue, true, null));
                           }
                           break;
                        }
                  }
               }
            }
         }

         for (int which = whichStart; which < 2; which++)
         {
            Element whichElement = (which == 0) ? element : elementType;
            if (whichElement == null)
               continue;

            HashSet<IFCAnyHandle> createdPropertySets = new HashSet<IFCAnyHandle>();

            int size = propertySets[which].Count;
            if (size == 0)
            {
               ExporterCacheManager.TypePropertyInfoCache.AddNewElementHandles(typeId, elementSets);
               continue;
            }

            bool materialProperties = element is Material;
            foreach (KeyValuePair<string, (string, HashSet<IFCAnyHandle>)> currPropertySet in propertySets[which])
            {
               if (currPropertySet.Value.Item2.Count == 0)
                  continue;

               if (materialProperties)
               {
                  MaterialPropertiesUtil.ExportGenericMaterialPropertySet(file, elementSets?.ToList().First(), currPropertySet.Value.Item2, null, currPropertySet.Value.Item1);
               }
               else
               {
                  string psetGUID = GUIDUtil.GenerateIFCGuidFrom(
                     GUIDUtil.CreateGUIDString(whichElement, "IfcPropertySet: " + currPropertySet.Key.ToString()));

                  IFCAnyHandle propertySet = IFCInstanceExporter.CreatePropertySet(file, psetGUID,
                     ExporterCacheManager.OwnerHistoryHandle, currPropertySet.Value.Item1, null,
                     currPropertySet.Value.Item2);
                  createdPropertySets.Add(propertySet);
               }
            }

            // Don't need to create relations for material properties
            if (!materialProperties)
            {
               if (which == 0)
                  ExporterCacheManager.CreatedInternalPropertySets.Add(whichElement.Id, createdPropertySets, elementSets);
               else
                  ExporterCacheManager.TypePropertyInfoCache.AddNewTypeProperties(typeId, createdPropertySets, elementSets);
            }
         }
      }

      /// <summary>
      /// Get a unit type of parameter.
      /// IFCUnit for each one.
      /// </summary>
      /// <param name="parameter">The parameter.</param>
      /// <returns>The parameter unit type.</returns>
      public static ForgeTypeId GetParameterUnitType(Parameter parameter)
      {
         ForgeTypeId parameterUnitType = null;

         try
         {
            parameterUnitType = parameter?.GetUnitTypeId();
         }
         catch
         {
            // GetUnitTypeId() can fail for reasons that don't seem to be knowable in
            // advance, so we won't scale value in these cases.
         }

         return parameterUnitType;
      }

      /// <summary>
      /// Creates property from real parameter.
      /// There are many different ParameterTypes in Revit that share the same unit dimensions, but that
      /// have potentially different display units (e.g. Bar Diameter could be in millimeters while the project 
      /// default length parameter is in meters.)  For now, we will only support one unit type.  At a later
      /// point, we could decide to have different caches for each parameter type, and export a different
      /// IFCUnit for each one.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="parameter">The parameter.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="propertyValue">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRealPropertyBasedOnParameterType(IFCFile file, Parameter parameter, string propertyName, double propertyValue, PropertyValueType valueType)
      {
         if (parameter == null)
            return null;

         ForgeTypeId type = parameter.Definition?.GetDataType();
         ForgeTypeId fallbackUnitType = GetParameterUnitType(parameter);

         return CreateRealPropertyByType(file, type, propertyName, propertyValue, valueType, fallbackUnitType);
      }

      /// <summary>
      /// Creates property from real parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="parameterType">The type of the parameter.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="propertyValue">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="fallbackUnitType">The optional unit type. Can be used for scaling in final case</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRealPropertyByType(IFCFile file, ForgeTypeId parameterType, string propertyName, double propertyValue, PropertyValueType valueType, ForgeTypeId fallbackUnitType = null)
      {
         IFCAnyHandle propertyHandle = null;

         if (parameterType == SpecTypeId.Acceleration)
         {
            double scaledValue = UnitUtil.ScaleAcceleration(propertyValue);
            propertyHandle = CreateAccelerationPropertyFromCache(file, propertyName,
                new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Energy ||
            parameterType == SpecTypeId.HvacEnergy)
         {
            double scaledValue = UnitUtil.ScaleEnergy(propertyValue);
            propertyHandle = CreateEnergyPropertyFromCache(file, propertyName,
                new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.LinearMoment)
         {
            double scaledValue = UnitUtil.ScaleLinearMoment(propertyValue);
            propertyHandle = CreateLinearMomentPropertyFromCache(file, propertyName,
                new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.MassPerUnitLength ||
            parameterType == SpecTypeId.PipeMassPerUnitLength)
         {
            double scaledValue = UnitUtil.ScaleMassPerLength(propertyValue);
            propertyHandle = CreateMassPerLengthPropertyFromCache(file, propertyName,
                new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Moment)
         {
            double scaledValue = UnitUtil.ScaleTorque(propertyValue);
            propertyHandle = CreateTorquePropertyFromCache(file, propertyName,
                new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.PointSpringCoefficient)
         {
            double scaledValue = UnitUtil.ScaleLinearStiffness(propertyValue);
            propertyHandle = CreateLinearStiffnessPropertyFromCache(file, propertyName,
                new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Pulsation)
         {
            double scaledValue = UnitUtil.ScaleAngularVelocity(propertyValue);
            propertyHandle = CreateAngularVelocityPropertyFromCache(file, propertyName,
                new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.ThermalResistance)
         {
            double scaledValue = UnitUtil.ScaleThermalResistance(propertyValue);
            propertyHandle = CreateThermalResistancePropertyFromCache(file, propertyName,
                new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.WarpingConstant)
         {
            double scaledValue = UnitUtil.ScaleWarpingConstant(propertyValue);
            propertyHandle = CreateWarpingConstantPropertyFromCache(file, propertyName,
                new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Angle ||
            parameterType == SpecTypeId.Rotation ||
            parameterType == SpecTypeId.RotationAngle)
         {
            double scaledValue = UnitUtil.ScaleAngle(propertyValue);
            propertyHandle = CreatePlaneAnglePropertyFromCache(file, propertyName,
               new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Slope ||
            parameterType == SpecTypeId.HvacSlope ||
            parameterType == SpecTypeId.PipingSlope ||
            parameterType == SpecTypeId.DemandFactor ||
            parameterType == SpecTypeId.Factor)
         {
            propertyHandle = CreatePositiveRatioPropertyFromCache(file, propertyName,
               new List<double?>() { propertyValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Area ||
            parameterType == SpecTypeId.CrossSection ||
            parameterType == SpecTypeId.ReinforcementArea ||
            parameterType == SpecTypeId.SectionArea)
         {
            double scaledValue = UnitUtil.ScaleArea(propertyValue);
            propertyHandle = CreateAreaPropertyFromCache(file, propertyName,
                new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.BarDiameter ||
            parameterType == SpecTypeId.CrackWidth ||
            parameterType == SpecTypeId.Displacement ||
            parameterType == SpecTypeId.Distance ||
            parameterType == SpecTypeId.CableTraySize ||
            parameterType == SpecTypeId.ConduitSize ||
            parameterType == SpecTypeId.Length ||
            parameterType == SpecTypeId.DuctInsulationThickness ||
            parameterType == SpecTypeId.DuctLiningThickness ||
            parameterType == SpecTypeId.DuctSize ||
            parameterType == SpecTypeId.HvacRoughness ||
            parameterType == SpecTypeId.PipeDimension ||
            parameterType == SpecTypeId.PipeInsulationThickness ||
            parameterType == SpecTypeId.PipeSize ||
            parameterType == SpecTypeId.PipingRoughness ||
            parameterType == SpecTypeId.ReinforcementCover ||
            parameterType == SpecTypeId.ReinforcementLength ||
            parameterType == SpecTypeId.ReinforcementSpacing ||
            parameterType == SpecTypeId.SectionDimension ||
            parameterType == SpecTypeId.SectionProperty ||
            parameterType == SpecTypeId.WireDiameter ||
            parameterType == SpecTypeId.SurfaceAreaPerUnitLength)
         {
            double scaledValue = UnitUtil.ScaleLength(propertyValue);
            propertyHandle = CreateLengthPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Currency)
         {
            IFCData currencyData = ExporterCacheManager.UnitsCache.ContainsKey("CURRENCY") ?
                  IFCDataUtil.CreateAsMonetaryMeasure(propertyValue) :
                  IFCDataUtil.CreateAsReal(propertyValue);
            propertyHandle = CreateCommonProperty(file, propertyName, currencyData,
                  valueType, null);
         }
         else if (parameterType == SpecTypeId.ApparentPower ||
            parameterType == SpecTypeId.ElectricalPower ||
            parameterType == SpecTypeId.Wattage ||
            parameterType == SpecTypeId.CoolingLoad ||
            parameterType == SpecTypeId.HeatGain ||
            parameterType == SpecTypeId.HeatingLoad ||
            parameterType == SpecTypeId.HvacPower)
         {
            double scaledValue = UnitUtil.ScalePower(propertyValue);
            propertyHandle = CreatePowerPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Current)
         {
            double scaledValue = UnitUtil.ScaleElectricCurrent(propertyValue);
            propertyHandle = CreateElectricCurrentPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Diffusivity)
         {
            double scaledValue = UnitUtil.ScaleMoistureDiffusivity(propertyValue);
            propertyHandle = CreateMoistureDiffusivityPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.ElectricalFrequency ||
            parameterType == SpecTypeId.StructuralFrequency)
         {
            propertyHandle = CreateFrequencyPropertyFromCache(file, propertyName,
                  new List<double?>() { propertyValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Illuminance)
         {
            double scaledValue = UnitUtil.ScaleIlluminance(propertyValue);
            propertyHandle = CreateIlluminancePropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.LuminousFlux)
         {
            double scaledValue = UnitUtil.ScaleLuminousFlux(propertyValue);
            propertyHandle = CreateLuminousFluxPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.LuminousIntensity)
         {
            double scaledValue = UnitUtil.ScaleLuminousIntensity(propertyValue);
            propertyHandle = CreateLuminousIntensityPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.ElectricalPotential)
         {
            double scaledValue = UnitUtil.ScaleElectricVoltage(propertyValue);
            propertyHandle = CreateElectricVoltagePropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.HvacTemperature ||
            parameterType == SpecTypeId.ElectricalTemperature ||
            parameterType == SpecTypeId.PipingTemperature)
         {
            double scaledValue = UnitUtil.ScaleThermodynamicTemperature(propertyValue);
            propertyHandle = CreateThermodynamicTemperaturePropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.HeatTransferCoefficient)
         {
            double scaledValue = UnitUtil.ScaleThermalTransmittance(propertyValue);
            propertyHandle = CreateThermalTransmittancePropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Force ||
            parameterType == SpecTypeId.Weight)
         {
            double scaledValue = UnitUtil.ScaleForce(propertyValue);
            propertyHandle = CreateForcePropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.AreaForce)
         {
            double scaledValue = UnitUtil.ScalePlanarForce(propertyValue);
            propertyHandle = CreatePlanarForcePropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.LinearForce ||
            parameterType == SpecTypeId.WeightPerUnitLength)
         {
            double scaledValue = UnitUtil.ScaleLinearForce(propertyValue);
            propertyHandle = CreateLinearForcePropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.AirFlow ||
            parameterType == SpecTypeId.Flow)
         {
            double scaledValue = UnitUtil.ScaleVolumetricFlowRate(propertyValue);
            propertyHandle = CreateVolumetricFlowRatePropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.HvacPressure ||
            parameterType == SpecTypeId.PipingPressure ||
            parameterType == SpecTypeId.Stress)
         {
            double scaledValue = UnitUtil.ScalePressure(propertyValue);
            propertyHandle = CreatePressurePropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.HvacVelocity ||
            parameterType == SpecTypeId.PipingVelocity ||
            parameterType == SpecTypeId.StructuralVelocity ||
            parameterType == SpecTypeId.Speed)
         {
            double scaledValue = UnitUtil.ScaleLinearVelocity(propertyValue);
            propertyHandle = CreateLinearVelocityPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Mass ||
            parameterType == SpecTypeId.PipingMass)
         {
            double scaledValue = UnitUtil.ScaleMass(propertyValue);
            propertyHandle = CreateMassPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.MassDensity ||
            parameterType == SpecTypeId.HvacDensity)
         {
            double scaledValue = UnitUtil.ScaleMassDensity(propertyValue);
            propertyHandle = CreateMassDensityPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.PipingDensity)
         {
            double scaledValue = UnitUtil.ScaleIonConcentration(propertyValue);
            propertyHandle = CreateIonConcentrationPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.MomentOfInertia)
         {
            double scaledValue = UnitUtil.ScaleMomentOfInertia(propertyValue);
            propertyHandle = CreateMomentOfInertiaPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Number)
         {
            propertyHandle = CreateRealPropertyFromCache(file, propertyName, new List<double?>() { propertyValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.PipingVolume ||
            parameterType == SpecTypeId.ReinforcementVolume ||
            parameterType == SpecTypeId.SectionModulus ||
            parameterType == SpecTypeId.Volume)
         {
            double scaledValue = UnitUtil.ScaleVolume(propertyValue);
            propertyHandle = CreateVolumePropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.PipingMassPerTime ||
            parameterType == SpecTypeId.HvacMassPerTime)
         {
            double scaledValue = UnitUtil.ScaleMassFlowRate(propertyValue);
            propertyHandle = CreateMassFlowRatePropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.AngularSpeed)
         {
            double scaledValue = UnitUtil.ScaleRotationalFrequency(propertyValue);
            propertyHandle = CreateRotationalFrequencyPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.ThermalConductivity)
         {
            double scaledValue = UnitUtil.ScaleThermalConductivity(propertyValue);
            propertyHandle = CreateThermalConductivityPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.SpecificHeat)
         {
            double scaledValue = UnitUtil.ScaleSpecificHeatCapacity(propertyValue);
            propertyHandle = CreateSpecificHeatCapacityPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Permeability)
         {
            double scaledValue = UnitUtil.ScaleVaporPermeability(propertyValue);
            propertyHandle = CreateVaporPermeabilityPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.HvacViscosity ||
            parameterType == SpecTypeId.PipingViscosity)
         {
            double scaledValue = UnitUtil.ScaleDynamicViscosity(propertyValue);
            propertyHandle = CreateDynamicViscosityPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.ThermalExpansionCoefficient)
         {
            double scaledValue = UnitUtil.ScaleThermalExpansionCoefficient(propertyValue);
            propertyHandle = CreateThermalExpansionCoefficientPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.SpecificHeatOfVaporization)
         {
            double scaledValue = UnitUtil.ScaleHeatingValue(propertyValue);
            propertyHandle = CreateHeatingValuePropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.IsothermalMoistureCapacity)
         {
            double scaledValue = UnitUtil.ScaleIsothermalMoistureCapacity(propertyValue);
            propertyHandle = CreateIsothermalMoistureCapacityPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.HvacPowerDensity)
         {
            double scaledValue = UnitUtil.ScaleHeatFluxDensity(propertyValue);
            propertyHandle = CreateHeatFluxDensityPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.MassPerUnitArea && !ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
         {
            double scaledValue = UnitUtil.ScaleAreaDensity(propertyValue);
            propertyHandle = CreateAreaDensityPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Time ||
            parameterType == SpecTypeId.Period)
         {
            double scaledValue = UnitUtil.ScaleTime(propertyValue);
            IFCData timeData = IFCDataUtil.CreateAsTimeMeasure(scaledValue);
            propertyHandle = CreateCommonProperty(file, propertyName, timeData,
                  valueType, null);
         }
         else if (parameterType == SpecTypeId.ColorTemperature)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.ColorTemperature, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "COLORTEMPERATURE");
         }
         else if (parameterType == SpecTypeId.CostPerArea)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.CostPerArea, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "COSTPERAREA");
         }
         else if (parameterType == SpecTypeId.ApparentPowerDensity)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.ApparentPowerDensity, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "APPARENTPOWERDENSITY");
         }
         else if (parameterType == SpecTypeId.CostRateEnergy)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.CostRateEnergy, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "COSTRATEENERGY");
         }
         else if (parameterType == SpecTypeId.CostRatePower)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.CostRatePower, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "COSTRATEPOWER");
         }
         else if (parameterType == SpecTypeId.Efficacy)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.Efficacy, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "LUMINOUSEFFICACY");
         }
         else if (parameterType == SpecTypeId.Luminance)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.Luminance, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "LUMINANCE");
         }
         else if (parameterType == SpecTypeId.ElectricalPowerDensity)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.ElectricalPowerDensity, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "ELECTRICALPOWERDENSITY");
         }
         else if (parameterType == SpecTypeId.PowerPerLength)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.PowerPerLength, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "POWERPERLENGTH");
         }
         else if (parameterType == SpecTypeId.ElectricalResistivity)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.ElectricalResistivity, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "ELECTRICALRESISTIVITY");
         }
         else if (parameterType == SpecTypeId.HeatCapacityPerArea)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.HeatCapacityPerArea, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "HEATCAPACITYPERAREA");
         }
         else if (parameterType == SpecTypeId.ThermalGradientCoefficientForMoistureCapacity)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.ThermalGradientCoefficientForMoistureCapacity, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "THERMALGRADIENTCOEFFICIENTFORMOISTURECAPACITY");
         }
         else if (parameterType == SpecTypeId.ThermalMass)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.ThermalMass, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "THERMALMASS");
         }
         else if (parameterType == SpecTypeId.AirFlowDensity)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.AirFlowDensity, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "AIRFLOWDENSITY");
         }
         else if (parameterType == SpecTypeId.AirFlowDividedByCoolingLoad)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.AirFlowDividedByCoolingLoad, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "AIRFLOWDIVIDEDBYCOOLINGLOAD");
         }
         else if (parameterType == SpecTypeId.AirFlowDividedByVolume)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.AirFlowDividedByVolume, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "AIRFLOWDIVIDEDBYVOLUME");
         }
         else if (parameterType == SpecTypeId.AreaDividedByCoolingLoad)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.AreaDividedByCoolingLoad, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "AREADIVIDEDBYCOOLINGLOAD");
         }
         else if (parameterType == SpecTypeId.AreaDividedByHeatingLoad)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.AreaDividedByHeatingLoad, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "AREADIVIDEDBYHEATINGLOAD");
         }
         else if (parameterType == SpecTypeId.CoolingLoadDividedByArea)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.CoolingLoadDividedByArea, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "COOLINGLOADDIVIDEDBYAREA");
         }
         else if (parameterType == SpecTypeId.CoolingLoadDividedByVolume)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.CoolingLoadDividedByVolume, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "COOLINGLOADDIVIDEDBYVOLUME");
         }
         else if (parameterType == SpecTypeId.FlowPerPower)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.FlowPerPower, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "FLOWPERPOWER");
         }
         else if (parameterType == SpecTypeId.HvacFriction)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.HvacFriction, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
               new List<double?>() { scaledValue }, valueType, "FRICTIONLOSS");
         }
         else if (parameterType == SpecTypeId.HeatingLoadDividedByArea)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.HeatingLoadDividedByArea, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "HEATINGLOADDIVIDEDBYAREA");
         }
         else if (parameterType == SpecTypeId.HeatingLoadDividedByVolume)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.HeatingLoadDividedByVolume, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "HEATINGLOADDIVIDEDBYVOLUME");
         }
         else if (parameterType == SpecTypeId.PowerPerFlow)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.PowerPerFlow, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "POWERPERFLOW");
         }
         else if (parameterType == SpecTypeId.PipingFriction)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.PipingFriction, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "PIPINGFRICTION");
         }
         else if (parameterType == SpecTypeId.AreaSpringCoefficient)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.AreaSpringCoefficient, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "AREASPRINGCOEFFICIENT");
         }
         else if (parameterType == SpecTypeId.LineSpringCoefficient)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.LineSpringCoefficient, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "LINESPRINGCOEFFICIENT");
         }
         else if (parameterType == SpecTypeId.MassPerUnitArea)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.MassPerUnitArea, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "MASSPERUNITAREA");
         }
         else if (parameterType == SpecTypeId.ReinforcementAreaPerUnitLength)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.ReinforcementAreaPerUnitLength, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "REINFORCEMENTAREAPERUNITLENGTH");
         }
         else if (parameterType == SpecTypeId.RotationalLineSpringCoefficient)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.RotationalLineSpringCoefficient, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "ROTATIONALLINESPRINGCOEFFICIENT");
         }
         else if (parameterType == SpecTypeId.RotationalPointSpringCoefficient)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.RotationalPointSpringCoefficient, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "ROTATIONALPOINTSPRINGCOEFFICIENT");
         }
         else if (parameterType == SpecTypeId.UnitWeight)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.UnitWeight, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyName,
                  new List<double?>() { scaledValue }, valueType, "UNITWEIGHT");
         }
         else
         {
            double scaledValue = propertyValue;
            if (fallbackUnitType != null)
               scaledValue = UnitUtils.ConvertFromInternalUnits(propertyValue, fallbackUnitType);

            propertyHandle = CreateRealPropertyFromCache(file, propertyName, new List<double?>() { scaledValue }, valueType, null);
         }

         return propertyHandle;
      }

      /// <summary>
      /// Creates and associates the common property sets associated with ElementTypes.  These are handled differently than for elements.
      /// </summary>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      /// <param name="elementType">The element type whose properties are exported.</param>
      /// <param name="existingPropertySets">The handles of property sets already associated with the type.</param>
      /// <param name="prodTypeHnd">The handle of the entity associated with the element type object.</param>
      public static void CreateElementTypeProperties(ExporterIFC exporterIFC, ElementType elementType,
          HashSet<IFCAnyHandle> existingPropertySets, IFCAnyHandle prodTypeHnd)
      {
         HashSet<IFCAnyHandle> propertySets = new HashSet<IFCAnyHandle>();

         // Pass in an empty set of handles - we don't want IfcRelDefinesByProperties for type properties.
         ISet<IFCAnyHandle> associatedObjectIds = new HashSet<IFCAnyHandle>();
         CreateInternalRevitPropertySets(exporterIFC, elementType, associatedObjectIds, false);

         TypePropertyInfo additionalPropertySets = null;
         ElementId typeId = elementType.Id;
         if (ExporterCacheManager.TypePropertyInfoCache.TryGetValue(typeId, out additionalPropertySets))
            propertySets.UnionWith(additionalPropertySets.PropertySets);

         if (existingPropertySets != null && existingPropertySets.Count > 0)
            propertySets.UnionWith(existingPropertySets);

         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            Document doc = elementType.Document;

            IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

            IList<PropertySetDescription> currPsetsToCreate =
               ExporterUtil.GetCurrPSetsToCreate(prodTypeHnd, PSetsToProcess.Type);
            foreach (PropertySetDescription currDesc in currPsetsToCreate)
            {
               // Last conditional check: if the property set comes from a ViewSchedule, check if the element is in the schedule.
               if (currDesc.ViewScheduleId != ElementId.InvalidElementId)
                  if (!ExporterCacheManager.ViewScheduleElementCache[currDesc.ViewScheduleId].Contains(typeId))
                     continue;

               ElementOrConnector elementOrConnector = new ElementOrConnector(elementType);
               ISet<IFCAnyHandle> props = currDesc.ProcessEntries(file, exporterIFC, null, elementOrConnector, elementType, prodTypeHnd);
               if (props.Count > 0)
               {
                  string paramSetName = currDesc.Name;
                  string guid = GUIDUtil.GenerateIFCGuidFrom(
                     GUIDUtil.CreateGUIDString(IFCEntityType.IfcPropertySet, paramSetName, prodTypeHnd));

                  IFCAnyHandle propertySet = IFCInstanceExporter.CreatePropertySet(file, guid, ownerHistory, paramSetName, null, props);
                  propertySets.Add(propertySet);
               }
            }

            if (propertySets.Count != 0)
            {
               prodTypeHnd.SetAttribute("HasPropertySets", propertySets);
               // Don't assign the property sets to the instances if we have just assigned them to the type.
               if (additionalPropertySets != null)
                  additionalPropertySets.AssignedToType = true;
            }

            transaction.Commit();
         }
      }

      public static IList<double?> GetDoubleValuesFromParameterByType(Element elem, string revitParameterName, ForgeTypeId specTypeId, PropertyValueType valueType)
      {
         List<double?> values = new List<double?>();

         switch (valueType)
         {
            case PropertyValueType.SingleValue:
            case PropertyValueType.ListValue:   // TODO: REVIT-193510
            case PropertyValueType.TableValue:
               {
                  double? propertyValue = GetScaledDoubleValueFromParameter(elem, revitParameterName, specTypeId);
                  if (propertyValue.HasValue)
                     values.Add(propertyValue.Value);
               }
               break;
            case PropertyValueType.BoundedValue:
               {                  
                  double? valueSetPoint = GetScaledDoubleValueFromParameter(elem, revitParameterName + ".SetPointValue", specTypeId);
                  double? valueUpper = GetScaledDoubleValueFromParameter(elem, revitParameterName + ".UpperBoundValue", specTypeId);
                  double? valueLower = GetScaledDoubleValueFromParameter(elem, revitParameterName + ".LowerBoundValue", specTypeId);

                  if (valueUpper == null && valueLower == null && valueSetPoint == null)
                     valueUpper = GetScaledDoubleValueFromParameter(elem, revitParameterName, specTypeId);

                  if (valueUpper != null || valueLower != null || valueSetPoint != null)
                  {
                     values.Add(valueSetPoint);
                     values.Add(valueUpper);
                     values.Add(valueLower);
                  }
               }
               break;
            default:
               throw new InvalidOperationException("Missing case!");

         }
         return values;
      }

      public static double? GetScaledDoubleValueFromParameter(Element elem, string revitParameterName, ForgeTypeId specTypeId)
      {
         double propertyValue = 0.0;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number) &&
               param.StorageType != StorageType.String) //The built-in parameter corresponding to "TotalWattage" is a string value in Revit that is likely going to be in the current units, and doesn't need to be scaled twice.
            {
               propertyValue = UnitUtil.ScaleDouble(specTypeId, propertyValue);
            }

            // Convert value from internal to displayed units if we want to export it as Real
            if (specTypeId == SpecTypeId.Number)
            {
               ForgeTypeId paramUnitType = GetParameterUnitType(param);
               if (paramUnitType != null)
                  propertyValue = UnitUtils.ConvertFromInternalUnits(propertyValue, paramUnitType);
            }

            return propertyValue;
         }

         return null;
      }

      #region Create___PropertyFromElement_1
      /// <summary>
      /// Create Area measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAreaPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateAreaPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateAreaPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create Acceleration measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAccelerationPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateAccelerationPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateAccelerationPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create AngularVelocity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAngularVelocityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateAngularVelocityPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateAngularVelocityPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create AreaDensity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAreaDensityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateAreaDensityPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateAreaDensityPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create DynamicViscosity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateDynamicViscosityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateDynamicViscosityPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateDynamicViscosityPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create ElectricCurrent measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateElectricCurrentPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateElectricCurrentPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateElectricCurrentPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create ElectricVoltage measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateElectricVoltagePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateElectricVoltagePropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateElectricVoltagePropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create Energy measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateEnergyPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateEnergyPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateEnergyPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create Force measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateForcePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateForcePropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateForcePropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create Frequency measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateFrequencyPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateFrequencyPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateFrequencyPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create HeatingValue measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateHeatingValuePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateHeatingValuePropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateHeatingValuePropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create Illuminance measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIlluminancePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateIlluminancePropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateIlluminancePropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create IonConcentration measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIonConcentrationPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateIonConcentrationPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateIonConcentrationPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create IsothermalMoistureCapacity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIsothermalMoistureCapacityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateIsothermalMoistureCapacityPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateIsothermalMoistureCapacityPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create HeatFluxDensity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateHeatFluxDensityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateHeatFluxDensityPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateHeatFluxDensityPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create Length measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLengthPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateLengthPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateLengthPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create LinearForce measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearForcePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateLinearForcePropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateLinearForcePropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create LinearMoment measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearMomentPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateLinearMomentPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateLinearMomentPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create LinearStiffness measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearStiffnessPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateLinearStiffnessPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateLinearStiffnessPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create LinearVelocity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearVelocityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateLinearVelocityPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateLinearVelocityPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create LuminousFlux measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLuminousFluxPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateLuminousFluxPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateLuminousFluxPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create LuminousIntensity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLuminousIntensityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateLuminousIntensityPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateLuminousIntensityPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create Mass measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateMassPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateMassPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create MassDensity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassDensityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateMassDensityPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateMassDensityPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create MassFlowRate measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassFlowRatePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateMassFlowRatePropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateMassFlowRatePropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create MassPerLength measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassPerLengthPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateMassPerLengthPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateMassPerLengthPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create ModulusOfElasticity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateModulusOfElasticityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateModulusOfElasticityPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateModulusOfElasticityPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create MoistureDiffusivity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMoistureDiffusivityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateMoistureDiffusivityPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateMoistureDiffusivityPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create MomentOfInertia measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMomentOfInertiaPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateMomentOfInertiaPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateMomentOfInertiaPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create NormalisedRatio measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateNormalisedRatioPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateNormalisedRatioPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateNormalisedRatioPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create Numeric measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateNumericPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateNumericPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateNumericPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create PlaneAngle measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePlaneAnglePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreatePlaneAnglePropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreatePlaneAnglePropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create PlanarForce measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePlanarForcePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreatePlanarForcePropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreatePlanarForcePropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create PositiveLength measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositiveLengthPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreatePositiveLengthPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreatePositiveLengthPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create PositiveRatio measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositiveRatioPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreatePositiveRatioPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreatePositiveRatioPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create PositivePlaneAngle measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositivePlaneAnglePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreatePositivePlaneAnglePropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreatePositivePlaneAnglePropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create Power measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePowerPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreatePowerPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreatePowerPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create Pressure measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePressurePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreatePressurePropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreatePressurePropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create Ratio measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRatioPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateRatioPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateRatioPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create Real measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRealPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateRealPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateRealPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create RotationalFrequency measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRotationalFrequencyPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateRotationalFrequencyPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateRotationalFrequencyPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create SoundPower measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPowerPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateSoundPowerPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateSoundPowerPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create SoundPressure measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPressurePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateSoundPressurePropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateSoundPressurePropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create SpecificHeatCapacity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSpecificHeatCapacityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateSpecificHeatCapacityPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateSpecificHeatCapacityPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create ThermalConductivity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalConductivityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateThermalConductivityPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateThermalConductivityPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create ThermalExpansionCoefficient measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalExpansionCoefficientPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateThermalExpansionCoefficientPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateThermalExpansionCoefficientPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create ThermalResistance measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalResistancePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateThermalResistancePropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateThermalResistancePropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create ThermalTransmittance measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalTransmittancePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateThermalTransmittancePropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateThermalTransmittancePropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create ThermodynamicTemperature measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermodynamicTemperaturePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateThermodynamicTemperaturePropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateThermodynamicTemperaturePropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create VaporPermeability measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVaporPermeabilityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateVaporPermeabilityPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateVaporPermeabilityPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create Volume measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVolumePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateVolumePropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateVolumePropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create VolumetricFlowRate measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVolumetricFlowRatePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateVolumetricFlowRatePropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateVolumetricFlowRatePropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create Torque measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTorquePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateTorquePropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateTorquePropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create WarpingConstant measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateWarpingConstantPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateWarpingConstantPropertyFromElement(file, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateWarpingConstantPropertyFromElement(file, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }
      #endregion

      #region Create___PropertyFromElement_2

      /// <summary>
      /// Create a Area measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAreaPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Area, valueType);
         IFCAnyHandle property = CreateAreaPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Area, valueType);
            property = CreateAreaPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Acceleration measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAccelerationPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Acceleration, valueType);
         IFCAnyHandle property = CreateAccelerationPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Acceleration, valueType);
            property = CreateAccelerationPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a AngularVelocity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAngularVelocityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Pulsation, valueType);
         IFCAnyHandle property = CreateAngularVelocityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Pulsation, valueType);
            property = CreateAngularVelocityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a AreaDensity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAreaDensityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.MassPerUnitArea, valueType);
         IFCAnyHandle property = CreateAreaDensityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.MassPerUnitArea, valueType);
            property = CreateAreaDensityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a DynamicViscosity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateDynamicViscosityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.HvacViscosity, valueType);
         IFCAnyHandle property = CreateDynamicViscosityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.HvacViscosity, valueType);
            property = CreateDynamicViscosityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a ElectricCurrent measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateElectricCurrentPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Current, valueType);
         IFCAnyHandle property = CreateElectricCurrentPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Current, valueType);
            property = CreateElectricCurrentPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a ElectricVoltage measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateElectricVoltagePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.ElectricalPotential, valueType);
         IFCAnyHandle property = CreateElectricVoltagePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.ElectricalPotential, valueType);
            property = CreateElectricVoltagePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Energy measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateEnergyPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Energy, valueType);
         IFCAnyHandle property = CreateEnergyPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Energy, valueType);
            property = CreateEnergyPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Force measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateForcePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Force, valueType);
         IFCAnyHandle property = CreateForcePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Force, valueType);
            property = CreateForcePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Frequency measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateFrequencyPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Number, valueType);
         IFCAnyHandle property = CreateFrequencyPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Number, valueType);
            property = CreateFrequencyPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a HeatingValue measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateHeatingValuePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.SpecificHeatOfVaporization, valueType);
         IFCAnyHandle property = CreateHeatingValuePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.SpecificHeatOfVaporization, valueType);
            property = CreateHeatingValuePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Illuminance measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIlluminancePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Illuminance, valueType);
         IFCAnyHandle property = CreateIlluminancePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Illuminance, valueType);
            property = CreateIlluminancePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a IonConcentration measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIonConcentrationPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.PipingDensity, valueType);
         IFCAnyHandle property = CreateIonConcentrationPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.PipingDensity, valueType);
            property = CreateIonConcentrationPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a IsothermalMoistureCapacity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIsothermalMoistureCapacityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.IsothermalMoistureCapacity, valueType);
         IFCAnyHandle property = CreateIsothermalMoistureCapacityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.IsothermalMoistureCapacity, valueType);
            property = CreateIsothermalMoistureCapacityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a HeatFluxDensity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateHeatFluxDensityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.HvacPowerDensity, valueType);
         IFCAnyHandle property = CreateHeatFluxDensityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.HvacPowerDensity, valueType);
            property = CreateHeatFluxDensityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Length measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLengthPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Length, valueType);
         IFCAnyHandle property = CreateLengthPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Length, valueType);
            property = CreateLengthPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a LinearForce measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearForcePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.LinearForce, valueType);
         IFCAnyHandle property = CreateLinearForcePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.LinearForce, valueType);
            property = CreateLinearForcePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a LinearMoment measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearMomentPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.LinearMoment, valueType);
         IFCAnyHandle property = CreateLinearMomentPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.LinearMoment, valueType);
            property = CreateLinearMomentPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a LinearStiffness measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearStiffnessPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.PointSpringCoefficient, valueType);
         IFCAnyHandle property = CreateLinearStiffnessPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.PointSpringCoefficient, valueType);
            property = CreateLinearStiffnessPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a LinearVelocity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearVelocityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.HvacVelocity, valueType);
         IFCAnyHandle property = CreateLinearVelocityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.HvacVelocity, valueType);
            property = CreateLinearVelocityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a LuminousFlux measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLuminousFluxPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.LuminousFlux, valueType);
         IFCAnyHandle property = CreateLuminousFluxPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.LuminousFlux, valueType);
            property = CreateLuminousFluxPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a LuminousIntensity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLuminousIntensityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.LuminousIntensity, valueType);
         IFCAnyHandle property = CreateLuminousIntensityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.LuminousIntensity, valueType);
            property = CreateLuminousIntensityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Mass measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Mass, valueType);
         IFCAnyHandle property = CreateMassPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Mass, valueType);
            property = CreateMassPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a MassDensity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassDensityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.MassDensity, valueType);
         IFCAnyHandle property = CreateMassDensityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.MassDensity, valueType);
            property = CreateMassDensityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a MassFlowRate measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassFlowRatePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.PipingMassPerTime, valueType);
         IFCAnyHandle property = CreateMassFlowRatePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.PipingMassPerTime, valueType);
            property = CreateMassFlowRatePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a MassPerLength measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassPerLengthPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.MassPerUnitLength, valueType);
         IFCAnyHandle property = CreateMassPerLengthPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.MassPerUnitLength, valueType);
            property = CreateMassPerLengthPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a ModulusOfElasticity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateModulusOfElasticityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Stress, valueType);
         IFCAnyHandle property = CreateModulusOfElasticityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Stress, valueType);
            property = CreateModulusOfElasticityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a MoistureDiffusivity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMoistureDiffusivityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Diffusivity, valueType);
         IFCAnyHandle property = CreateMoistureDiffusivityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Diffusivity, valueType);
            property = CreateMoistureDiffusivityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a MomentOfInertia measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMomentOfInertiaPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.MomentOfInertia, valueType);
         IFCAnyHandle property = CreateMomentOfInertiaPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.MomentOfInertia, valueType);
            property = CreateMomentOfInertiaPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a NormalisedRatio measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateNormalisedRatioPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Number, valueType);
         IFCAnyHandle property = CreateNormalisedRatioPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Number, valueType);
            property = CreateNormalisedRatioPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Numeric measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateNumericPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Number, valueType);
         IFCAnyHandle property = CreateNumericPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Number, valueType);
            property = CreateNumericPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a PlaneAngle measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePlaneAnglePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Angle, valueType);
         IFCAnyHandle property = CreatePlaneAnglePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Angle, valueType);
            property = CreatePlaneAnglePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a PlanarForce measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePlanarForcePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.AreaForce, valueType);
         IFCAnyHandle property = CreatePlanarForcePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.AreaForce, valueType);
            property = CreatePlanarForcePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a PositiveLength measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositiveLengthPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Length, valueType);
         IFCAnyHandle property = CreatePositiveLengthPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Length, valueType);
            property = CreatePositiveLengthPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a PositiveRatio measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositiveRatioPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Number, valueType);
         IFCAnyHandle property = CreatePositiveRatioPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Number, valueType);
            property = CreatePositiveRatioPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a PositivePlaneAngle measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositivePlaneAnglePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Angle, valueType);
         IFCAnyHandle property = CreatePositivePlaneAnglePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Angle, valueType);
            property = CreatePositivePlaneAnglePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Power measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePowerPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.HvacPower, valueType);
         IFCAnyHandle property = CreatePowerPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.HvacPower, valueType);
            property = CreatePowerPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Pressure measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePressurePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.HvacPressure, valueType);
         IFCAnyHandle property = CreatePressurePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.HvacPressure, valueType);
            property = CreatePressurePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Ratio measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRatioPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Number, valueType);
         IFCAnyHandle property = CreateRatioPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Number, valueType);
            property = CreateRatioPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Real measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRealPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Number, valueType);
         IFCAnyHandle property = CreateRealPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Number, valueType);
            property = CreateRealPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a RotationalFrequency measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRotationalFrequencyPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.AngularSpeed, valueType);
         IFCAnyHandle property = CreateRotationalFrequencyPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.AngularSpeed, valueType);
            property = CreateRotationalFrequencyPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a SoundPower measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPowerPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Wattage, valueType);
         IFCAnyHandle property = CreateSoundPowerPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Wattage, valueType);
            property = CreateSoundPowerPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a SoundPressure measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPressurePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.HvacPressure, valueType);
         IFCAnyHandle property = CreateSoundPressurePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.HvacPressure, valueType);
            property = CreateSoundPressurePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a SpecificHeatCapacity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSpecificHeatCapacityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.SpecificHeat, valueType);
         IFCAnyHandle property = CreateSpecificHeatCapacityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.SpecificHeat, valueType);
            property = CreateSpecificHeatCapacityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a ThermalConductivity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalConductivityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.ThermalConductivity, valueType);
         IFCAnyHandle property = CreateThermalConductivityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.ThermalConductivity, valueType);
            property = CreateThermalConductivityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a ThermalExpansionCoefficient measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalExpansionCoefficientPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.ThermalExpansionCoefficient, valueType);
         IFCAnyHandle property = CreateThermalExpansionCoefficientPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.ThermalExpansionCoefficient, valueType);
            property = CreateThermalExpansionCoefficientPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a ThermalResistance measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalResistancePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.ThermalResistance, valueType);
         IFCAnyHandle property = CreateThermalResistancePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.ThermalResistance, valueType);
            property = CreateThermalResistancePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a ThermalTransmittance measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalTransmittancePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.HeatTransferCoefficient, valueType);
         IFCAnyHandle property = CreateThermalTransmittancePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.HeatTransferCoefficient, valueType);
            property = CreateThermalTransmittancePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a ThermodynamicTemperature measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermodynamicTemperaturePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.HvacTemperature, valueType);
         IFCAnyHandle property = CreateThermodynamicTemperaturePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.HvacTemperature, valueType);
            property = CreateThermodynamicTemperaturePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a VaporPermeability measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVaporPermeabilityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Permeability, valueType);
         IFCAnyHandle property = CreateVaporPermeabilityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Permeability, valueType);
            property = CreateVaporPermeabilityPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Volume measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVolumePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Volume, valueType);
         IFCAnyHandle property = CreateVolumePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Volume, valueType);
            property = CreateVolumePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a VolumetricFlowRate measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVolumetricFlowRatePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.AirFlow, valueType);
         IFCAnyHandle property = CreateVolumetricFlowRatePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.AirFlow, valueType);
            property = CreateVolumetricFlowRatePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Torque measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTorquePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Moment, valueType);
         IFCAnyHandle property = CreateTorquePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.Moment, valueType);
            property = CreateTorquePropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a WarpingConstant measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateWarpingConstantPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          string ifcPropertyName, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.WarpingConstant, valueType);
         IFCAnyHandle property = CreateWarpingConstantPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, ifcPropertyName, SpecTypeId.WarpingConstant, valueType);
            property = CreateWarpingConstantPropertyFromCache(file, ifcPropertyName, doubleValues, valueType, null);
         }

         return property;
      }
      #endregion

      #region Create___PropertyFromCache

      /// <summary>Create property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="createProperty">The function to craete property.</param>
      /// <param name="propertyType">The property type.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateGenericPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey,
          Func<IFCFile, string, IList<double?>, PropertyValueType, string, IFCAnyHandle> createProperty, PropertyType propertyType)
      {
         if ((values?.Count ?? 0) == 0)
            return null;

         bool canCache = false;
         double value = 0.0;
         if (values.ElementAt(0) != null && valueType == PropertyValueType.SingleValue && string.IsNullOrEmpty(unitTypeKey))
         {
            bool isLengthProeprty = (propertyType == PropertyType.Length);
            value = values.ElementAt(0).Value;

            double? adjustedValue = (isLengthProeprty) ? CanCacheDouble(UnitUtil.UnscaleLength(value)) : CanCacheDouble(value);
            canCache = adjustedValue.HasValue;
            if (canCache)
            {
               value = (isLengthProeprty) ? UnitUtil.UnscaleLength(adjustedValue.GetValueOrDefault()) : adjustedValue.GetValueOrDefault();
               values[0] = value;
            }
         }

         IFCAnyHandle propertyHandle;
         if (canCache)
         {
            propertyHandle = ExporterCacheManager.PropertyInfoCache.GetDoubleChache(propertyType).Find(propertyName, value);
            if (propertyHandle != null)
               return propertyHandle;
         }

         propertyHandle = createProperty(file, propertyName, values, valueType, unitTypeKey);

         if (canCache && !IFCAnyHandleUtil.IsNullOrHasNoValue(propertyHandle))
         {
            ExporterCacheManager.PropertyInfoCache.GetDoubleChache(propertyType).Add(propertyName, value, propertyHandle);
         }

         return propertyHandle;
      }


      /// <summary>Create Area property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateAreaPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateAreaProperty, PropertyType.Area);
      }

      /// <summary>Create Acceleration property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateAccelerationPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateAccelerationProperty, PropertyType.Acceleration);
      }

      /// <summary>Create AngularVelocity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateAngularVelocityPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateAngularVelocityProperty, PropertyType.AngularVelocity);
      }

      /// <summary>Create AreaDensity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateAreaDensityPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateAreaDensityProperty, PropertyType.AreaDensity);
      }

      /// <summary>Create DynamicViscosity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateDynamicViscosityPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateDynamicViscosityProperty, PropertyType.DynamicViscosity);
      }

      /// <summary>Create ElectricCurrent property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateElectricCurrentPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateElectricCurrentProperty, PropertyType.ElectricCurrent);
      }

      /// <summary>Create ElectricVoltage property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateElectricVoltagePropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateElectricVoltageProperty, PropertyType.ElectricVoltage);
      }

      /// <summary>Create Energy property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateEnergyPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateEnergyProperty, PropertyType.Energy);
      }

      /// <summary>Create Force property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateForcePropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateForceProperty, PropertyType.Force);
      }

      /// <summary>Create Frequency property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateFrequencyPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateFrequencyProperty, PropertyType.Frequency);
      }

      /// <summary>Create HeatingValue property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateHeatingValuePropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateHeatingValueProperty, PropertyType.HeatingValue);
      }

      /// <summary>Create Illuminance property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateIlluminancePropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateIlluminanceProperty, PropertyType.Illuminance);
      }

      /// <summary>Create IonConcentration property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateIonConcentrationPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateIonConcentrationProperty, PropertyType.IonConcentration);
      }

      /// <summary>Create IsothermalMoistureCapacity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateIsothermalMoistureCapacityPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateIsothermalMoistureCapacityProperty, PropertyType.IsothermalMoistureCapacity);
      }

      /// <summary>Create HeatFluxDensity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateHeatFluxDensityPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateHeatFluxDensityProperty, PropertyType.HeatFluxDensity);
      }

      /// <summary>Create Length property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateLengthPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateLengthProperty, PropertyType.Length);
      }

      /// <summary>Create LinearForce property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateLinearForcePropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateLinearForceProperty, PropertyType.LinearForce);
      }

      /// <summary>Create LinearMoment property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateLinearMomentPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateLinearMomentProperty, PropertyType.LinearMoment);
      }

      /// <summary>Create LinearStiffness property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateLinearStiffnessPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateLinearStiffnessProperty, PropertyType.LinearStiffness);
      }

      /// <summary>Create LinearVelocity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateLinearVelocityPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateLinearVelocityProperty, PropertyType.LinearVelocity);
      }

      /// <summary>Create LuminousFlux property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateLuminousFluxPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateLuminousFluxProperty, PropertyType.LuminousFlux);
      }

      /// <summary>Create LuminousIntensity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateLuminousIntensityPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateLuminousIntensityProperty, PropertyType.LuminousIntensity);
      }

      /// <summary>Create Mass property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateMassPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateMassProperty, PropertyType.Mass);
      }

      /// <summary>Create MassDensity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateMassDensityPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateMassDensityProperty, PropertyType.MassDensity);
      }

      /// <summary>Create MassFlowRate property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateMassFlowRatePropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateMassFlowRateProperty, PropertyType.MassFlowRate);
      }

      /// <summary>Create MassPerLength property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateMassPerLengthPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateMassPerLengthProperty, PropertyType.MassPerLength);
      }

      /// <summary>Create ModulusOfElasticity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateModulusOfElasticityPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateModulusOfElasticityProperty, PropertyType.ModulusOfElasticity);
      }

      /// <summary>Create MoistureDiffusivity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateMoistureDiffusivityPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateMoistureDiffusivityProperty, PropertyType.MoistureDiffusivity);
      }

      /// <summary>Create MomentOfInertia property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateMomentOfInertiaPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateMomentOfInertiaProperty, PropertyType.MomentOfInertia);
      }

      /// <summary>Create NormalisedRatio property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateNormalisedRatioPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateNormalisedRatioProperty, PropertyType.NormalisedRatio);
      }

      /// <summary>Create Numeric property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateNumericPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateNumericProperty, PropertyType.Numeric);
      }

      /// <summary>Create PlaneAngle property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreatePlaneAnglePropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreatePlaneAngleProperty, PropertyType.PlaneAngle);
      }

      /// <summary>Create PlanarForce property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreatePlanarForcePropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreatePlanarForceProperty, PropertyType.PlanarForce);
      }

      /// <summary>Create PositiveLength property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreatePositiveLengthPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreatePositiveLengthProperty, PropertyType.PositiveLength);
      }

      /// <summary>Create PositiveRatio property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreatePositiveRatioPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreatePositiveRatioProperty, PropertyType.PositiveRatio);
      }

      /// <summary>Create PositivePlaneAngle property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreatePositivePlaneAnglePropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreatePositivePlaneAngleProperty, PropertyType.PositivePlaneAngle);
      }

      /// <summary>Create Power property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreatePowerPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreatePowerProperty, PropertyType.Power);
      }

      /// <summary>Create Pressure property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreatePressurePropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreatePressureProperty, PropertyType.Pressure);
      }

      /// <summary>Create Ratio property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateRatioPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateRatioProperty, PropertyType.Ratio);
      }

      /// <summary>Create Real property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateRealPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateRealProperty, PropertyType.Real);
      }

      /// <summary>Create RotationalFrequency property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateRotationalFrequencyPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateRotationalFrequencyProperty, PropertyType.RotationalFrequency);
      }

      /// <summary>Create SoundPower property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateSoundPowerPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateSoundPowerProperty, PropertyType.SoundPower);
      }

      /// <summary>Create SoundPressure property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateSoundPressurePropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateSoundPressureProperty, PropertyType.SoundPressure);
      }

      /// <summary>Create SpecificHeatCapacity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateSpecificHeatCapacityPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateSpecificHeatCapacityProperty, PropertyType.SpecificHeatCapacity);
      }

      /// <summary>Create ThermalConductivity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateThermalConductivityPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateThermalConductivityProperty, PropertyType.ThermalConductivity);
      }

      /// <summary>Create ThermalExpansionCoefficient property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateThermalExpansionCoefficientPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateThermalExpansionCoefficientProperty, PropertyType.ThermalExpansionCoefficient);
      }

      /// <summary>Create ThermalResistance property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateThermalResistancePropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateThermalResistanceProperty, PropertyType.ThermalResistance);
      }

      /// <summary>Create ThermalTransmittance property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateThermalTransmittancePropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateThermalTransmittanceProperty, PropertyType.ThermalTransmittance);
      }

      /// <summary>Create ThermodynamicTemperature property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateThermodynamicTemperaturePropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateThermodynamicTemperatureProperty, PropertyType.ThermodynamicTemperature);
      }

      /// <summary>Create VaporPermeability property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateVaporPermeabilityPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateVaporPermeabilityProperty, PropertyType.VaporPermeability);
      }

      /// <summary>Create Volume property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateVolumePropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateVolumeProperty, PropertyType.Volume);
      }

      /// <summary>Create VolumetricFlowRate property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateVolumetricFlowRatePropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateVolumetricFlowRateProperty, PropertyType.VolumetricFlowRate);
      }

      /// <summary>Create Torque property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateTorquePropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateTorqueProperty, PropertyType.Torque);
      }

      /// <summary>Create WarpingConstant property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateWarpingConstantPropertyFromCache(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyName, values, valueType, unitTypeKey, CreateWarpingConstantProperty, PropertyType.WarpingConstant);
      }


      #endregion

      #region Create___Property

      /// <summary>
      /// Create property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="createMeasure">The craete measure function.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateGenericProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, 
         string unitTypeKey, Func<double, IFCData> createMeasure)
      {
         if (values == null)
            return null;

         List<IFCData> dataList = new List<IFCData>();
         foreach (var val in values)
            dataList.Add(val.HasValue ? createMeasure(val.Value) : null);
         return CreateCommonPropertyFromList(file, propertyName, dataList, valueType, unitTypeKey);
      }

      /// <summary>
      /// Create Area property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAreaProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsAreaMeasure);
      }

      /// <summary>
      /// Create Acceleration property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAccelerationProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsAccelerationMeasure);
      }

      /// <summary>
      /// Create AngularVelocity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAngularVelocityProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsAngularVelocityMeasure);
      }

      /// <summary>
      /// Create AreaDensity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAreaDensityProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsAreaDensityMeasure);
      }

      /// <summary>
      /// Create DynamicViscosity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateDynamicViscosityProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsDynamicViscosityMeasure);
      }

      /// <summary>
      /// Create ElectricCurrent property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateElectricCurrentProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsElectricCurrentMeasure);
      }

      /// <summary>
      /// Create ElectricVoltage property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateElectricVoltageProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsElectricVoltageMeasure);
      }

      /// <summary>
      /// Create Energy property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateEnergyProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsEnergyMeasure);
      }

      /// <summary>
      /// Create Force property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateForceProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsForceMeasure);
      }

      /// <summary>
      /// Create Frequency property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateFrequencyProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsFrequencyMeasure);
      }

      /// <summary>
      /// Create HeatingValue property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateHeatingValueProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsHeatingValueMeasure);
      }

      /// <summary>
      /// Create Illuminance property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIlluminanceProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsIlluminanceMeasure);
      }

      /// <summary>
      /// Create IonConcentration property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIonConcentrationProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsIonConcentrationMeasure);
      }

      /// <summary>
      /// Create IsothermalMoistureCapacity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIsothermalMoistureCapacityProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsIsothermalMoistureCapacityMeasure);
      }

      /// <summary>
      /// Create HeatFluxDensity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateHeatFluxDensityProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsHeatFluxDensityMeasure);
      }

      /// <summary>
      /// Create Length property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLengthProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsLengthMeasure);
      }

      /// <summary>
      /// Create LinearForce property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearForceProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsLinearForceMeasure);
      }

      /// <summary>
      /// Create LinearMoment property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearMomentProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsLinearMomentMeasure);
      }

      /// <summary>
      /// Create LinearStiffness property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearStiffnessProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsLinearStiffnessMeasure);
      }

      /// <summary>
      /// Create LinearVelocity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearVelocityProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsLinearVelocityMeasure);
      }

      /// <summary>
      /// Create LuminousFlux property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLuminousFluxProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsLuminousFluxMeasure);
      }

      /// <summary>
      /// Create LuminousIntensity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLuminousIntensityProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsLuminousIntensityMeasure);
      }

      /// <summary>
      /// Create Mass property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsMassMeasure);
      }

      /// <summary>
      /// Create MassDensity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassDensityProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsMassDensityMeasure);
      }

      /// <summary>
      /// Create MassFlowRate property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassFlowRateProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsMassFlowRateMeasure);
      }

      /// <summary>
      /// Create MassPerLength property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassPerLengthProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsMassPerLengthMeasure);
      }

      /// <summary>
      /// Create ModulusOfElasticity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateModulusOfElasticityProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsModulusOfElasticityMeasure);
      }

      /// <summary>
      /// Create MoistureDiffusivity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMoistureDiffusivityProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsMoistureDiffusivityMeasure);
      }

      /// <summary>
      /// Create MomentOfInertia property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMomentOfInertiaProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsMomentOfInertiaMeasure);
      }

      /// <summary>
      /// Create NormalisedRatio property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateNormalisedRatioProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsNormalisedRatioMeasure);
      }

      /// <summary>
      /// Create Numeric property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateNumericProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsNumeric);
      }

      /// <summary>
      /// Create PlaneAngle property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePlaneAngleProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsPlaneAngleMeasure);
      }

      /// <summary>
      /// Create PlanarForce property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePlanarForceProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsPlanarForceMeasure);
      }

      /// <summary>
      /// Create PositiveLength property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositiveLengthProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsPositiveLengthMeasure);
      }

      /// <summary>
      /// Create PositiveRatio property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositiveRatioProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsPositiveRatioMeasure);
      }

      /// <summary>
      /// Create PositivePlaneAngle property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositivePlaneAngleProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsPositivePlaneAngleMeasure);
      }

      /// <summary>
      /// Create Power property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePowerProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsPowerMeasure);
      }

      /// <summary>
      /// Create Pressure property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePressureProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsPressureMeasure);
      }

      /// <summary>
      /// Create Ratio property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRatioProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsRatioMeasure);
      }

      /// <summary>
      /// Create Real property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRealProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsReal);
      }

      /// <summary>
      /// Create RotationalFrequency property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRotationalFrequencyProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsRotationalFrequencyMeasure);
      }

      /// <summary>
      /// Create SoundPower property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPowerProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsSoundPowerMeasure);
      }

      /// <summary>
      /// Create SoundPressure property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPressureProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsSoundPressureMeasure);
      }

      /// <summary>
      /// Create SpecificHeatCapacity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSpecificHeatCapacityProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsSpecificHeatCapacityMeasure);
      }

      /// <summary>
      /// Create ThermalConductivity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalConductivityProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsThermalConductivityMeasure);
      }

      /// <summary>
      /// Create ThermalExpansionCoefficient property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalExpansionCoefficientProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsThermalExpansionCoefficientMeasure);
      }

      /// <summary>
      /// Create ThermalResistance property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalResistanceProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsThermalResistanceMeasure);
      }

      /// <summary>
      /// Create ThermalTransmittance property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalTransmittanceProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsThermalTransmittanceMeasure);
      }

      /// <summary>
      /// Create ThermodynamicTemperature property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermodynamicTemperatureProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsThermodynamicTemperatureMeasure);
      }

      /// <summary>
      /// Create VaporPermeability property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVaporPermeabilityProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsVaporPermeabilityMeasure);
      }

      /// <summary>
      /// Create Volume property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVolumeProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsVolumeMeasure);
      }

      /// <summary>
      /// Create VolumetricFlowRate property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVolumetricFlowRateProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsVolumetricFlowRateMeasure);
      }

      /// <summary>
      /// Create Torque property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTorqueProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsTorqueMeasure);
      }

      /// <summary>
      /// Create WarpingConstant property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateWarpingConstantProperty(IFCFile file, string propertyName, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyName, values, valueType, unitTypeKey, IFCDataUtil.CreateAsWarpingConstantMeasure);
      }
      #endregion

   }
}