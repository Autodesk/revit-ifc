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



      public static IFCAnyHandle CreateCommonProperty(IFCFile file, string propertyName, IFCData valueData, PropertyValueType valueType, string unitTypeKey)
      {
         IFCAnyHandle unitHnd = (!ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView && unitTypeKey != null) ? ExporterCacheManager.UnitsCache[unitTypeKey] : null;

         switch (valueType)
         {
            case PropertyValueType.EnumeratedValue:
               {
                  IList<IFCData> valueList = new List<IFCData>();
                  valueList.Add(valueData);
                  return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
               }
            case PropertyValueType.SingleValue:
               {
                  return IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, valueData, unitHnd);
               }
            case PropertyValueType.ListValue:
               {
                  IList<IFCData> valueList = new List<IFCData>();
                  valueList.Add(valueData);
                  return IFCInstanceExporter.CreatePropertyListValue(file, propertyName, null, valueList, unitHnd);
               }
            case PropertyValueType.BoundedValue:
               {
                  IList<IFCData> valueList = new List<IFCData>();
                  valueList.Add(valueData);
                  return CreateBoundedValuePropertyFromList(file, propertyName, valueList, unitTypeKey);
               }
            case PropertyValueType.TableValue:
               {
                  // must be handled in CreatePropertyFromElementBase
                  throw new InvalidOperationException("Unhandled table property!");
               }
            default:
               throw new InvalidOperationException("Missing case!");
         }
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
         IFCAnyHandle unitHnd = (!ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView && unitTypeKey != null) ? ExporterCacheManager.UnitsCache[unitTypeKey] : null;

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
         IFCAnyHandle definingUnitHnd = (!ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView && definingUnitTypeKey != null) ? ExporterCacheManager.UnitsCache[definingUnitTypeKey] : null;
         IFCAnyHandle definedUnitHnd = (!ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView && definedUnitTypeKey != null) ? ExporterCacheManager.UnitsCache[definedUnitTypeKey] : null;

         return IFCInstanceExporter.CreatePropertyTableValue(file, propertyName, null, definingValues, definedValues, definingUnitHnd, definedUnitHnd);
      }

      /// <summary>
      /// Create a label property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
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
                  string validatedString = IFCDataUtil.ValidateEnumeratedValue(value, propertyEnumerationType);
                  if (validatedString == null)
                     return null;
                  valueList.Add(IFCDataUtil.CreateAsLabel(validatedString));
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
      /// <param name="value">The value of the property.</param>
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
      /// <param name="value">The value of the property.</param>
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
      /// <param name="value">The value of the property.</param>
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
      /// <param name="value">The value of the property.</param>
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
      /// <param name="value">The value of the property.</param>
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
      /// <param name="value">The value of the property.</param>
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
      /// <param name="value">The value of the property.</param>
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
      /// <param name="value">The value of the property.</param>
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
         // We have a partial cache here - cache multiples of 0.5 up to 10.
         if (MathUtil.IsAlmostZero(value))
            return 0.0;

         double valueTimes2 = Math.Floor(value * 2 + MathUtil.Eps());
         if (valueTimes2 > 0 && valueTimes2 <= 20 && MathUtil.IsAlmostZero(value * 2 - valueTimes2))
            return valueTimes2 / 2;

         return null;
      }

      internal static double? CanCacheLength(double unscaledValue, double value)
      {
         // We have a partial cache here, based on the unscaledValue.
         // Cache multiples of +/- 0.05 up to 10.
         // Cache multiples of +/- 50 up to 10000.

         if (MathUtil.IsAlmostZero(value))
            return 0.0;

         // approximate tests for most common scales are good enough here.
         bool isNegative = (unscaledValue < 0);
         double unscaledPositiveValue = isNegative ? -unscaledValue : unscaledValue;
         double eps = MathUtil.Eps();

         if (unscaledPositiveValue <= 10.0 + eps)
         {
            double unscaledPositiveValueTimes2 = Math.Floor(unscaledPositiveValue * 2 + eps);
            if (MathUtil.IsAlmostZero(unscaledPositiveValue * 2 - unscaledPositiveValueTimes2))
            {
               double scaledPositiveValue = UnitUtil.ScaleLength(unscaledPositiveValueTimes2 / 2);
               return isNegative ? -scaledPositiveValue : scaledPositiveValue;
            }
            return null;
         }

         if (unscaledPositiveValue <= 10000.0 + eps)
         {
            double unscaledPositiveValueDiv50 = Math.Floor(unscaledPositiveValue / 50.0 + eps);
            if (MathUtil.IsAlmostEqual(unscaledPositiveValue / 50.0, unscaledPositiveValueDiv50))
            {
               double scaledPositiveValue = UnitUtil.ScaleLength(unscaledPositiveValueDiv50 * 50.0);
               return isNegative ? -scaledPositiveValue : scaledPositiveValue;
            }
         }

         return null;
      }

      internal static double? CanCachePower(double value)
      {
         // Allow caching of values between 0 and 300, in multiples of 5
         double eps = MathUtil.Eps();
         if (value < -eps || value > 300.0 + eps)
            return null;
         if (MathUtil.IsAlmostZero(value % 5.0))
            return Math.Truncate(value + 0.5);
         return null;
      }

      internal static double? CanCacheTemperature(double value)
      {
         // Allow caching of integral temperatures and half-degrees.
         if (MathUtil.IsAlmostEqual(value * 2.0, Math.Truncate(value * 2.0)))
            return Math.Truncate(value * 2.0) / 2.0;
         return null;
      }

      internal static double? CanCacheThermalTransmittance(double value)
      {
         // Allow caching of values between 0 and 6.0, in multiples of 0.05
         double eps = MathUtil.Eps();
         if (value < -eps || value > 6.0 + eps)
            return null;
         if (MathUtil.IsAlmostEqual(value * 20.0, Math.Truncate(value * 20.0)))
            return Math.Truncate(value * 20.0) / 20.0;
         return null;
      }

      /// <summary>
      /// Create a real property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRealProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData realData = IFCDataUtil.CreateAsReal(value);
         return CreateCommonProperty(file, propertyName, realData, valueType, null);
      }

      /// <summary>
      /// Create a numeric property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateNumericProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData NumericData = IFCDataUtil.CreateAsNumeric(value);
         return CreateCommonProperty(file, propertyName, NumericData, valueType, null);
      }

      /// <summary>Create a real property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateRealPropertyFromCache(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         double? adjustedValue = CanCacheDouble(value);
         bool canCache = adjustedValue.HasValue;
         if (canCache)
         {
            value = adjustedValue.GetValueOrDefault();
         }

         IFCAnyHandle propertyHandle;
         if (canCache)
         {
            propertyHandle = ExporterCacheManager.PropertyInfoCache.RealCache.Find(propertyName, value);
            if (propertyHandle != null)
               return propertyHandle;
         }

         propertyHandle = CreateRealProperty(file, propertyName, value, valueType);

         if (canCache && !IFCAnyHandleUtil.IsNullOrHasNoValue(propertyHandle))
         {
            ExporterCacheManager.PropertyInfoCache.RealCache.Add(propertyName, value, propertyHandle);
         }

         return propertyHandle;
      }

      /// <summary>Create a numeric property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateNumericPropertyFromCache(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         double? adjustedValue = CanCacheDouble(value);
         bool canCache = adjustedValue.HasValue;
         if (canCache)
         {
            value = adjustedValue.GetValueOrDefault();
         }

         IFCAnyHandle propertyHandle;
         if (canCache)
         {
            propertyHandle = ExporterCacheManager.PropertyInfoCache.NumericCache.Find(propertyName, value);
            if (propertyHandle != null)
               return propertyHandle;
         }

         propertyHandle = CreateNumericProperty(file, propertyName, value, valueType);

         if (canCache && !IFCAnyHandleUtil.IsNullOrHasNoValue(propertyHandle))
         {
            ExporterCacheManager.PropertyInfoCache.NumericCache.Add(propertyName, value, propertyHandle);
         }

         return propertyHandle;
      }

      /// <summary>Create a Thermodyanamic Temperature property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateThermodynamicTemperaturePropertyFromCache(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         double? adjustedValue = CanCacheTemperature(value);
         bool canCache = adjustedValue.HasValue;
         if (canCache)
            value = adjustedValue.GetValueOrDefault();

         IFCAnyHandle propertyHandle;
         if (canCache)
         {
            propertyHandle = ExporterCacheManager.PropertyInfoCache.ThermodynamicTemperatureCache.Find(propertyName, value);
            if (propertyHandle != null)
               return propertyHandle;
         }

         propertyHandle = CreateThermodynamicTemperatureProperty(file, propertyName, value, valueType);

         if (canCache && !IFCAnyHandleUtil.IsNullOrHasNoValue(propertyHandle))
            ExporterCacheManager.PropertyInfoCache.ThermodynamicTemperatureCache.Add(propertyName, value, propertyHandle);

         return propertyHandle;
      }

      /// <summary>Create a Power measure property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreatePowerPropertyFromCache(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         double? adjustedValue = CanCachePower(value);
         bool canCache = adjustedValue.HasValue;
         if (canCache)
            value = adjustedValue.GetValueOrDefault();

         IFCAnyHandle propertyHandle;
         if (canCache)
         {
            propertyHandle = ExporterCacheManager.PropertyInfoCache.PowerCache.Find(propertyName, value);
            if (propertyHandle != null)
               return propertyHandle;
         }

         propertyHandle = CreatePowerProperty(file, propertyName, value, valueType);

         if (canCache && !IFCAnyHandleUtil.IsNullOrHasNoValue(propertyHandle))
            ExporterCacheManager.PropertyInfoCache.PowerCache.Add(propertyName, value, propertyHandle);

         return propertyHandle;
      }

      /// <summary>Create a Thermal Transmittance property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateThermalTransmittancePropertyFromCache(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         double? adjustedValue = CanCacheThermalTransmittance(value);
         bool canCache = adjustedValue.HasValue;
         if (canCache)
            value = adjustedValue.GetValueOrDefault();

         IFCAnyHandle propertyHandle;
         if (canCache)
         {
            propertyHandle = ExporterCacheManager.PropertyInfoCache.ThermalTransmittanceCache.Find(propertyName, value);
            if (propertyHandle != null)
               return propertyHandle;
         }

         propertyHandle = CreateThermalTransmittanceProperty(file, propertyName, value, valueType);

         if (canCache && !IFCAnyHandleUtil.IsNullOrHasNoValue(propertyHandle))
            ExporterCacheManager.PropertyInfoCache.ThermalTransmittanceCache.Add(propertyName, value, propertyHandle);

         return propertyHandle;
      }

      /// <summary>
      /// Creates a length measure property or gets one from cache.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The unscaled value of the property, used for cache purposes.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLengthMeasurePropertyFromCache(IFCFile file, string propertyName, double value,
          PropertyValueType valueType)
      {
         double unscaledValue = UnitUtil.UnscaleLength(value);

         double? adjustedValue = CanCacheLength(unscaledValue, value);
         bool canCache = adjustedValue.HasValue;
         if (canCache)
         {
            value = adjustedValue.GetValueOrDefault();
         }

         IFCAnyHandle propertyHandle;
         if (canCache)
         {
            propertyHandle = ExporterCacheManager.PropertyInfoCache.LengthMeasureCache.Find(propertyName, value);
            if (propertyHandle != null)
               return propertyHandle;
         }

         switch (valueType)
         {
            case PropertyValueType.EnumeratedValue:
               {
                  IList<IFCData> valueList = new List<IFCData>();
                  valueList.Add(IFCDataUtil.CreateAsLengthMeasure(value));
                  propertyHandle = IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyName, null, valueList, null);
                  break;
               }
            case PropertyValueType.SingleValue:
               propertyHandle = IFCInstanceExporter.CreatePropertySingleValue(file, propertyName, null, IFCDataUtil.CreateAsLengthMeasure(value), null);
               break;
            default:
               throw new InvalidOperationException("Missing case!");
         }

         if (canCache && !IFCAnyHandleUtil.IsNullOrHasNoValue(propertyHandle))
         {
            ExporterCacheManager.PropertyInfoCache.LengthMeasureCache.Add(propertyName, value, propertyHandle);
         }

         return propertyHandle;
      }

      /// <summary>
      /// Creates a volume measure property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVolumeMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData volumeData = IFCDataUtil.CreateAsVolumeMeasure(value);
         return CreateCommonProperty(file, propertyName, volumeData, valueType, null);
      }

      /// <summary>
      /// Creates a sound power measure property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPowerMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData soundPowerData = IFCDataUtil.CreateAsSoundPowerMeasure(value);
         return CreateCommonProperty(file, propertyName, soundPowerData, valueType, null);
      }

      /// <summary>
      /// Creates a sound pressure measure property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPressureMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData soundPressureData = IFCDataUtil.CreateAsSoundPressureMeasure(value);
         return CreateCommonProperty(file, propertyName, soundPressureData, valueType, null);
      }

      /// <summary>
      /// Creates Specific Heat Capacity measure property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSpecificHeatCapacityMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData specificHeatCapacityData = IFCDataUtil.CreateAsSpecificHeatCapacityMeasure(value);
         return CreateCommonProperty(file, propertyName, specificHeatCapacityData, valueType, null);
      }

      /// <summary>
      /// Creates DynamicViscosity measure property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateDynamicViscosityMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData dynamicViscosityData = IFCDataUtil.CreateAsDynamicViscosityMeasure(value);
         return CreateCommonProperty(file, propertyName, dynamicViscosityData, valueType, null);
      }

      /// <summary>
      /// Creates ThermalConductivity measure property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalConductivityMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData thermalConductivityData = IFCDataUtil.CreateAsThermalConductivityMeasure(value);
         return CreateCommonProperty(file, propertyName, thermalConductivityData, valueType, null);
      }

      /// <summary>
      /// Creates ThermalExpansionCoefficient measure property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalExpansionCoefficientMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData thermalExpansionCoefficientData = IFCDataUtil.CreateAsThermalExpansionCoefficientMeasure(value);
         return CreateCommonProperty(file, propertyName, thermalExpansionCoefficientData, valueType, null);
      }

      /// <summary>
      /// Create a positive length measure property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositiveLengthMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         if (value > MathUtil.Eps())
         {
            IFCData posLengthData = IFCDataUtil.CreateAsPositiveLengthMeasure(value);
            return CreateCommonProperty(file, propertyName, posLengthData, valueType, null);
         }
         return null;
      }

      /// <summary>
      /// Create a linear velocity measure property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearVelocityMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData linearVelocityData = IFCDataUtil.CreateAsLinearVelocityMeasure(value);
         return CreateCommonProperty(file, propertyName, linearVelocityData, valueType, null);
      }

     

      /// <summary>
      /// Create a ratio measure property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRatioMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData data = IFCDataUtil.CreateRatioMeasureData(value);
         return CreateCommonProperty(file, propertyName, data, valueType, null);
      }

      /// <summary>
      /// Create a normalised ratio measure property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateNormalisedRatioMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData data = IFCDataUtil.CreateNormalisedRatioMeasureData(value);
         return CreateCommonProperty(file, propertyName, data, valueType, null);
      }

      /// <summary>
      /// Create a positive ratio measure property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositiveRatioMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData data = IFCDataUtil.CreatePositiveRatioMeasureData(value);
         return CreateCommonProperty(file, propertyName, data, valueType, null);
      }

      /// <summary>
      /// Create a label property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePlaneAngleMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData planeAngleData = IFCDataUtil.CreateAsPlaneAngleMeasure(value);
         return CreateCommonProperty(file, propertyName, planeAngleData, valueType, null);
      }

      /// <summary>
      /// Create a label property, or retrieve from cache.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreatePlaneAngleMeasurePropertyFromCache(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         // We have a partial cache here - we will only cache multiples of 15 degrees.
         bool canCache = false;
         double degreesDiv15 = Math.Floor(value / 15.0 + 0.5);
         double integerDegrees = degreesDiv15 * 15.0;
         if (MathUtil.IsAlmostEqual(value, integerDegrees))
         {
            canCache = true;
            value = integerDegrees;
         }

         IFCAnyHandle propertyHandle;
         if (canCache)
         {
            propertyHandle = ExporterCacheManager.PropertyInfoCache.PlaneAngleCache.Find(propertyName, value);
            if (propertyHandle != null)
               return propertyHandle;
         }

         propertyHandle = CreatePlaneAngleMeasureProperty(file, propertyName, value, valueType);

         if (canCache && !IFCAnyHandleUtil.IsNullOrHasNoValue(propertyHandle))
         {
            ExporterCacheManager.PropertyInfoCache.PlaneAngleCache.Add(propertyName, value, propertyHandle);
         }

         return propertyHandle;
      }

      /// <summary>
      /// Create a area measure property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAreaMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData areaData = IFCDataUtil.CreateAsAreaMeasure(value);
         return CreateCommonProperty(file, propertyName, areaData, valueType, null);
      }

      /// <summary>Create a count measure property.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
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
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateCountMeasureProperty(IFCFile file, string propertyName, int value, PropertyValueType valueType)
      {
         IFCData countData = IFCDataUtil.CreateAsCountMeasure(value);
         return CreateCommonProperty(file, propertyName, countData, valueType, null);
      }

      /// <summary>Create a ThermodynamicTemperature property.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermodynamicTemperatureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData thermodynamicTemperatureMeasureData = IFCDataUtil.CreateAsThermodynamicTemperatureMeasure(value);
         return CreateCommonProperty(file, propertyName, thermodynamicTemperatureMeasureData, valueType, null);
      }

      /// <summary>Create a ClassificationReference property.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateClassificationReferenceProperty(IFCFile file, string propertyName, string value)
      {
         IFCAnyHandle classificationReferenceHandle = 
            IFCInstanceExporter.CreateClassificationReference(file, null, value, null, null, null);
         return IFCInstanceExporter.CreatePropertyReferenceValue(file, propertyName, null, null, classificationReferenceHandle);
      }

      /// <summary>Create an IlluminanceMeasure property.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIlluminanceProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData illuminanceData = IFCDataUtil.CreateAsIlluminanceMeasure(value);
         return CreateCommonProperty(file, propertyName, illuminanceData, valueType, null);
      }

      /// <summary>Create a LuminousFluxMeasure property.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLuminousFluxMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData luminousFluxData = IFCDataUtil.CreateAsLuminousFluxMeasure(value);
         return CreateCommonProperty(file, propertyName, luminousFluxData, valueType, null);
      }

      /// <summary>Create a LuminousIntensityMeasure property.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLuminousIntensityProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData luminousIntensityData = IFCDataUtil.CreateAsLuminousIntensityMeasure(value);
         return CreateCommonProperty(file, propertyName, luminousIntensityData, valueType, null);
      }

      /// <summary>Create a ForceMeasure property.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateForceProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData forceData = IFCDataUtil.CreateAsForceMeasure(value);
         return CreateCommonProperty(file, propertyName, forceData, valueType, null);
      }

      /// <summary>Create a LinearForceMeasure property.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearForceProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData linearForceData = IFCDataUtil.CreateAsLinearForceMeasure(value);
         return CreateCommonProperty(file, propertyName, linearForceData, valueType, null);
      }

      public static IFCAnyHandle CreateLinearForcePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
         string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {

         IFCAnyHandle propHnd = CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
            "IfcLinearForceMeasure", SpecTypeId.LinearForce, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateDoublePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName,
               "IfcLinearForceMeasure", SpecTypeId.LinearForce, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      public static IFCAnyHandle CreatePlanarForcePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
         string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {

         IFCAnyHandle propHnd = CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
            "IfcPlanarForceMeasure", SpecTypeId.AreaForce, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateDoublePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName,
               "IfcPlanarForceMeasure", SpecTypeId.AreaForce, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>Create a PlanarForceMeasure property.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePlanarForceProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData planarForceData = IFCDataUtil.CreateAsPlanarForceMeasure(value);
         return CreateCommonProperty(file, propertyName, planarForceData, valueType, null);
      }

      /// <summary>Create a PowerMeasure property.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePowerProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData powerData = IFCDataUtil.CreateAsPowerMeasure(value);
         return CreateCommonProperty(file, propertyName, powerData, valueType, null);
      }

      /// <summary>Create a ThermalTransmittance property.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalTransmittanceProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData thermalTransmittanceData = IFCDataUtil.CreateAsThermalTransmittanceMeasure(value);
         return CreateCommonProperty(file, propertyName, thermalTransmittanceData, valueType, null);
      }

      /// <summary>Create a VolumetricFlowRate property.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVolumetricFlowRateMeasureProperty(IFCFile file, string propertyName, double value, PropertyValueType valueType)
      {
         IFCData volumetricFlowRateData = IFCDataUtil.CreateAsVolumetricFlowRateMeasure(value);
         return CreateCommonProperty(file, propertyName, volumetricFlowRateData, valueType, null);
      }

      /// <summary>
      /// Create a VolumetricFlowRate measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVolumetricFlowRatePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
             "IfcVolumetricFlowRateMeasure", SpecTypeId.AirFlow, valueType);
      }

      /// <summary>
      /// Create a Time measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTimePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
         "IfcTimeMeasure", SpecTypeId.Time, valueType);
      }

      /// <summary>
      /// Create a Sound power measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPowerPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleSoundPower(propertyValue);

            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, revitParameterName, propertyValue, SpecTypeId.Wattage, "IfcSoundPowerMeasure");
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, null);
            }
            else
            {
               return CreateSoundPowerMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            }
         }
         return null;
      }

      /// <summary>
      /// Create a Sound pressure measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPressurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleSoundPressure(propertyValue);

            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, revitParameterName, propertyValue, SpecTypeId.HvacPressure, "IfcSoundPressureMeasure");
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, null);
            }
            else
            {
               return CreateSoundPressureMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            }
         }
         return null;
      }


      /// <summary>
      /// Create a SpecificHeat Capacity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSpecificHeatCapacityPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleSpecificHeatCapacity(propertyValue);

            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, revitParameterName, propertyValue, SpecTypeId.SpecificHeat, "IfcSpecificHeatCapacityMeasure");
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, null);
            }
            else
            {
               return CreateSpecificHeatCapacityMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            }
         }
         return null;
      }

      /// <summary>
      /// Create a Dynamic Viscosity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateDynamicViscosityPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleDynamicViscosity(propertyValue);

            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, revitParameterName, propertyValue, SpecTypeId.HvacViscosity, "IfcDynamicViscosityMeasure");
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, null);
            }
            else
            {
               return CreateDynamicViscosityMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            }
         }
         return null;
      }

      /// <summary>
      /// Create a Color Temperature measure property from the element's parameter.  This will be an IfcReal with a custom unit.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateColorTemperaturePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName)
      {
         double propertyValue;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleDouble(SpecTypeId.ColorTemperature, propertyValue);
            return CreateColorTemperaturePropertyFromValue(file, ifcPropertyName, propertyValue);
         }
         return null;
      }

      public static IFCAnyHandle CreateColorTemperaturePropertyFromValue(IFCFile file, string ifcPropertyName, double propertyValue)
      {
         IFCData colorTemperatureData = IFCDataUtil.CreateAsMeasure(propertyValue, "IfcReal");
         return CreateCommonProperty(file, ifcPropertyName, colorTemperatureData,
               PropertyValueType.SingleValue, "COLORTEMPERATURE");
      }

      /// <summary>
      /// Create an electrical efficacy custom measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateElectricalEfficacyPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleDouble(SpecTypeId.Efficacy, propertyValue);

            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, revitParameterName, propertyValue, SpecTypeId.Efficacy, "IfcReal");
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, "LUMINOUSEFFICACY");
            }
            else
            {
               return CreateElectricalEfficacyPropertyFromValue(file, ifcPropertyName, propertyValue);
            }
         }
         return null;
      }

      public static IFCAnyHandle CreateElectricalEfficacyPropertyFromValue(IFCFile file, string ifcPropertyName, double propertyValue)
      {
         IFCData electricalEfficacyData = IFCDataUtil.CreateAsMeasure(propertyValue, "IfcReal");
         return CreateCommonProperty(file, ifcPropertyName, electricalEfficacyData,
               PropertyValueType.SingleValue, "LUMINOUSEFFICACY");
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
         double propertyValue;
         if (ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue) != null)
         {
            string measureName = ExporterCacheManager.UnitsCache.ContainsKey("CURRENCY") ? "IfcMonetaryMeasure" : "IfcReal";
            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, revitParameterName, propertyValue, SpecTypeId.Number, measureName);
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, null);
            }
            else
            {
               IFCData currencyData = IFCDataUtil.CreateAsMeasure(propertyValue, measureName);
               return CreateCommonProperty(file, ifcPropertyName, currencyData, PropertyValueType.SingleValue, null);
            }
         }
         return null;
      }

      /// <summary>
      /// Create a ThermodynamicTemperature measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermodynamicTemperaturePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleThermodynamicTemperature(propertyValue);

            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, revitParameterName, propertyValue, SpecTypeId.HvacTemperature, "IfcThermodynamicTemperatureMeasure");
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, null);
            }
            else
            {
               return CreateThermodynamicTemperaturePropertyFromCache(file, ifcPropertyName, propertyValue, valueType);
            }
         }

         param = ParameterUtil.GetDoubleValueFromElement(elem, ifcPropertyName, out propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleThermodynamicTemperature(propertyValue);

            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, ifcPropertyName, propertyValue, SpecTypeId.HvacTemperature, "IfcThermodynamicTemperatureMeasure");
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, null);
            }
            else
            {
               return CreateThermodynamicTemperaturePropertyFromCache(file, ifcPropertyName, propertyValue, valueType);
            }
         }
         return null;
      }


      /// <summary>
      /// Create a color temperature property from the element's parameter.  This will be an IfcReal with a special temperature unit.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateColorTemperaturePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateColorTemperaturePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateColorTemperaturePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create an electrical efficacy custom property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateElectricalEfficacyPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateElectricalEfficacyPropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateElectricalEfficacyPropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
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
      /// Create a ThermodynamicTemperature measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermodynamicTemperaturePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateThermodynamicTemperaturePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateThermodynamicTemperaturePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a VolumetricFlowRate measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVolumetricFlowRatePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateVolumetricFlowRatePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateVolumetricFlowRatePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a Time property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTimePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateTimePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateTimePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a Sound power property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPowerPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateSoundPowerPropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateSoundPowerPropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a Sound pressure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPressurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateSoundPressurePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateSoundPressurePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a Specific Heat Capacity property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSpecificHeatCapacityPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateSpecificHeatCapacityPropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateSpecificHeatCapacityPropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a Dynamic Viscosity property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateDynamicViscosityPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateDynamicViscosityPropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateDynamicViscosityPropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
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
      private static IFCAnyHandle CreateDoublePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, string measureType, ForgeTypeId specTypeId, PropertyValueType valueType)
      {
         double propertyValue;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleDouble(specTypeId, propertyValue);

            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, revitParameterName, propertyValue, specTypeId, measureType);
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, null);
            }
            else
            {
               IFCData doubleData = IFCDataUtil.CreateAsMeasure(propertyValue, measureType);
               return CreateCommonProperty(file, ifcPropertyName, doubleData, valueType, null);
            }
         }
         return null;
      }

      /// <summary>
      /// Create a list of bounded data.
      /// </summary>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="propertyValue">The SetPoint value.</param>
      /// <param name="specTypeId">Identifier of the property spec.</param>
      /// <param name="measureType">The IfcMeasure type of the property.</param>
      /// <returns>List of bounded data. Null if unset.</returns>
      public static IList<IFCData> GetBoundedDataFromElement(Element elem, string revitParameterName, double propertyValue, ForgeTypeId specTypeId, string measureType)
      {
         IList<IFCData> boundedData = new List<IFCData>();

         IList<double?> boundedValues = GetBoundedValuesFromElement(elem, revitParameterName, specTypeId);
         boundedValues.Insert(0, propertyValue);
         foreach (double? val in boundedValues)
         {
            if (!val.HasValue)
               boundedData.Add(null);
            else
               boundedData.Add(IFCDataUtil.CreateAsMeasure(val.Value, measureType));
         }
         return boundedData;
      }

      /// <summary>
      /// Reads bounded values from element
      /// </summary>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="specTypeId">Identifier of the property spec.</param>
      /// <returns>List of bounded values. Null if unset.</returns>
      public static IList<double?> GetBoundedValuesFromElement(Element elem, string revitParameterName, ForgeTypeId specTypeId)
      {
         IList<double?> boundedValues = new List<double?>();

         double upperBound;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName + ".UpperBoundValue", out upperBound);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               boundedValues.Add(UnitUtil.ScaleDouble(specTypeId, upperBound));
         }
         else
         {
            boundedValues.Add(null);
         }

         double lowerBound;
         param = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName + ".LowerBoundValue", out lowerBound);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               boundedValues.Add(UnitUtil.ScaleDouble(specTypeId, lowerBound));
         }
         else
         {
            boundedValues.Add(null);
         }

         return boundedValues;
      }

      /// <summary>
      /// Create a Force measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateForcePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
             "IfcForceMeasure", SpecTypeId.Force, valueType);
      }

      /// <summary>
      /// Create a Power measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePowerPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         Parameter powerParam = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue);
         if (powerParam != null)
         {
            // We are going to do a little hack here which we will need to extend in a nice way. The built-in parameter corresponding
            // to "TotalWattage" is a string value in Revit that is likely going to be in the current units, and doesn't need to be scaled twice.
            bool needToScale = !(ifcPropertyName == "TotalWattage" && powerParam.StorageType == StorageType.String)
                                 && ParameterUtil.ParameterDataTypeIsEqualTo(powerParam, SpecTypeId.HvacPower);

            double scaledpropertyValue = needToScale ? UnitUtil.ScalePower(propertyValue) : propertyValue;

            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, revitParameterName, propertyValue, 
                     needToScale ? SpecTypeId.HvacPower : SpecTypeId.Number, "IfcPowerMeasure");
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, null);
            }
            else
            {
               return CreatePowerPropertyFromCache(file, ifcPropertyName, scaledpropertyValue, valueType);
            }
         }
         return null;
      }

      /// <summary>
      /// Create a Luminous flux measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLuminousFluxMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
             "IfcLuminousFluxMeasure", SpecTypeId.LuminousFlux, valueType);
      }

      /// <summary>
      /// Create a Luminous intensity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLuminousIntensityMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
             "IfcLuminousIntensityMeasure", SpecTypeId.LuminousIntensity, valueType);
      }

      /// <summary>
      /// Create a illuminance measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIlluminanceMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
             "IfcIlluminanceMeasure", SpecTypeId.Illuminance, valueType);
      }

      /// <summary>
      /// Create a heat flux density measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateHeatFluxDensityMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
             "IfcHeatFluxDensityMeasure", SpecTypeId.HvacPowerDensity, valueType);
      }

      /// <summary>
      /// Create a Mass measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
             "IfcMassMeasure", SpecTypeId.Mass, valueType);
      }

      /// <summary>
      /// Create a pressure measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePressurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
             "IfcPressureMeasure", SpecTypeId.HvacPressure, valueType);
      }

      /// <summary>
      /// Create a ThermalConductivity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalConductivityPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleThermalConductivity(propertyValue);

            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, revitParameterName, propertyValue, SpecTypeId.ThermalConductivity, "IfcThermalConductivityMeasure");
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, null);
            }
            else
            {
               return CreateThermalConductivityMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            }
         }
         return null;
      }

      /// <summary>
      /// Create a ThermalExpansionCoefficient measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalExpansionCoefficientPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleThermalExpansionCoefficient(propertyValue);

            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, revitParameterName, propertyValue, SpecTypeId.ThermalExpansionCoefficient, "IfcThermalExpansionCoefficientMeasure");
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, null);
            }
            else
            {
               return CreateThermalExpansionCoefficientMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            }
         }
         return null;
      }

      /// <summary>
      /// Create a ThermalTransmittance measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalTransmittancePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleThermalTransmittance(propertyValue);

            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, revitParameterName, propertyValue, SpecTypeId.HeatTransferCoefficient, "IfcThermalTransmittanceMeasure");
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, null);
            }
            else
            {
               return CreateThermalTransmittancePropertyFromCache(file, ifcPropertyName, propertyValue, valueType);
            }
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
      /// Create a Force measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateForcePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateForcePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateForcePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a Power measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePowerPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreatePowerPropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreatePowerPropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a Mass measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateMassMeasurePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateMassMeasurePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a Mass density measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassDensityPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateMassDensityPropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateMassDensityPropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a Mass density measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassDensityPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
             "IfcMassDensityMeasure", SpecTypeId.MassDensity, valueType);
      }


      /// <summary>
      /// Create a Modulus Of Elasticity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateModulusOfElasticityPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateModulusOfElasticityPropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateModulusOfElasticityPropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a Modulus Of Elasticity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateModulusOfElasticityPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
             "IfcModulusOfElasticityMeasure", SpecTypeId.Stress, valueType);
      }

      /// <summary>
      /// Create a Heating Value measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateHeatingValuePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateHeatingValuePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateHeatingValuePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a Heating Value measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateHeatingValuePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
             "IfcHeatingValueMeasure", SpecTypeId.SpecificHeatOfVaporization, valueType);
      }

      /// <summary>
      /// Create a Moisture Diffusivity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMoistureDiffusivityPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateMoistureDiffusivityPropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateMoistureDiffusivityPropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a Diffusivity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMoistureDiffusivityPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
             "IfcMoistureDiffusivityMeasure", SpecTypeId.Diffusivity, valueType);
      }

      /// <summary>
      /// Create a Moment Of Inertia measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMomentOfInertiaPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateMomentOfInertiaPropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateMomentOfInertiaPropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a Moment Of Inertia measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMomentOfInertiaPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
             "IfcMomentOfInertiaMeasure", SpecTypeId.MomentOfInertia, valueType);
      }

      /// <summary>
      /// Create a Isothermal Moisture Capacity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIsothermalMoistureCapacityPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateIsothermalMoistureCapacityPropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateIsothermalMoistureCapacityPropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a Isothermal Moisture Capacity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIsothermalMoistureCapacityPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
             "IfcIsothermalMoistureCapacityMeasure", SpecTypeId.IsothermalMoistureCapacity, valueType);
      }

      /// <summary>
      /// Create a Isothermal Moisture Capacity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIonConcentrationPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateIonConcentrationPropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateIonConcentrationPropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a Ion Concentration measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIonConcentrationPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
             "IfcIonConcentrationMeasure", SpecTypeId.PipingDensity, valueType);
      }

      /// <summary>
      /// Create a Mass flow rate measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassFlowRatePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateMassFlowRatePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateMassFlowRatePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a Mass flow rate measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassFlowRatePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
             "IfcMassFlowRateMeasure", SpecTypeId.PipingMassPerTime, valueType);
      }

      /// <summary>
      /// Create a Rotational frequency measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRotationalFrequencyPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateRotationalFrequencyPropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateRotationalFrequencyPropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a Rotational frequency measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRotationalFrequencyPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
             "IfcRotationalFrequencyMeasure", SpecTypeId.AngularSpeed, valueType);
      }
      /// <summary>
      /// Create an Area density measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAreaDensityPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateAreaDensityPropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateAreaDensityPropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create an Area density measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAreaDensityPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
             "IfcAreaDensityMeasure", SpecTypeId.MassPerUnitArea, valueType);
      }


      /// <summary>
      /// Create a Luminous flux measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLuminousFluxMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateLuminousFluxMeasurePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateLuminousFluxMeasurePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a Luminous intensity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLuminousIntensityPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateLuminousIntensityMeasurePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateLuminousIntensityMeasurePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a heat flux density measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateHeatFluxDensityPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateHeatFluxDensityMeasurePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateHeatFluxDensityMeasurePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create an illuminance measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIlluminancePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateIlluminanceMeasurePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateIlluminanceMeasurePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a pressure measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePressurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreatePressurePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreatePressurePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a ThermalConductivity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalConductivityPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateThermalConductivityPropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateThermalConductivityPropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a ThermalExpansionCoefficient measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalExpansionCoefficientPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateThermalExpansionCoefficientPropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateThermalExpansionCoefficientPropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a ThermalTransmittance measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalTransmittancePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateThermalTransmittancePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateThermalTransmittancePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
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

      /// <summary>Create a real property from the element's parameter.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRealPropertyFromElement(IFCFile file, Element elem, string revitParameterName, string ifcPropertyName,
          PropertyValueType valueType)
      {
         double propertyValue;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue);
         if (param == null)
            param = ParameterUtil.GetDoubleValueFromElement(elem, ifcPropertyName, out propertyValue);

         if (param != null)
         {
            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, revitParameterName, propertyValue, SpecTypeId.Number, "IfcReal");
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, null);
            }
            else
            {
               return CreateRealPropertyBasedOnParameterType(file, param, ifcPropertyName, propertyValue, valueType);
            }
         }

         return null;
      }

      /// <summary>Create a numeric property from the element's parameter.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateNumericPropertyFromElement(IFCFile file, Element elem, string revitParameterName, string ifcPropertyName,
          PropertyValueType valueType)
      {
         double propertyValue;
         if (ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue) != null)
         {
            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, revitParameterName, propertyValue, SpecTypeId.Number, "IfcNumericMeasure");
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, null);
            }
            else
            {
               return CreateNumericPropertyFromCache(file, ifcPropertyName, propertyValue, valueType);
            }
         }

         if (ParameterUtil.GetDoubleValueFromElement(elem, ifcPropertyName, out propertyValue) != null)
         {
            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, ifcPropertyName, propertyValue, SpecTypeId.Number, "IfcNumericMeasure");
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, null);
            }
            else
            {
               return CreateNumericPropertyFromCache(file, ifcPropertyName, propertyValue, valueType);
            }
         }

         return null;
      }

      /// <summary>
      /// Create a length property from the element's or type's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="builtInParameterName">The name of the built-in parameter, can be null.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLengthMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
         string revitParameterName, string builtInParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;

         Parameter param = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleLength(propertyValue);

            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, revitParameterName, propertyValue, SpecTypeId.Length, "IfcLengthMeasure");
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, null);
            }
            else
            {
               return CreateLengthMeasurePropertyFromCache(file, ifcPropertyName, propertyValue, valueType);
            }
         }


         if (builtInParameterName != null)
         {
            param = ParameterUtil.GetDoubleValueFromElement(elem, builtInParameterName, out propertyValue);
            if (param != null)
            {
               if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
                  propertyValue = UnitUtil.ScaleLength(propertyValue);
               return CreateLengthMeasurePropertyFromCache(file, ifcPropertyName, propertyValue, valueType);
            }
         }

         return null;
      }

      /// <summary>
      /// Create a positive length property from the element's or type's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="builtInParameterName">The name of the built-in parameter, can be null.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositiveLengthMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
         string revitParameterName, string builtInParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleLength(propertyValue);

            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, revitParameterName, propertyValue, SpecTypeId.Length, "IfcPositiveLengthMeasure");
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, null);
            }
            else
            {
               return CreatePositiveLengthMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            }
         }

         if (builtInParameterName != null)
         {
            param = ParameterUtil.GetDoubleValueFromElement(elem, builtInParameterName, out propertyValue);
            if (param != null)
            {
               if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
                  propertyValue = UnitUtil.ScaleLength(propertyValue);
               return CreatePositiveLengthMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            }
         }

         return null;
      }

      /// <summary>
      /// Create a length property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The optional built-in parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLengthMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
         string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         string builtInParamName = null;
         if (revitBuiltInParam != BuiltInParameter.INVALID)
            builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);

         IFCAnyHandle propHnd = CreateLengthMeasurePropertyFromElement(file, exporterIFC, elem, revitParameterName, builtInParamName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         return null;
      }

      /// <summary>
      /// Create a positive length property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The optional built-in parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositiveLengthMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
         string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         string builtInParamName = null;
         if (revitBuiltInParam != BuiltInParameter.INVALID)
            builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);

         IFCAnyHandle propHnd = CreatePositiveLengthMeasurePropertyFromElement(file, exporterIFC, elem, revitParameterName, builtInParamName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         return null;
      }

      /// <summary>
      /// Create a ratio property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRatioPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
         string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         if (ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue) != null)
            return CreateRatioMeasureProperty(file, ifcPropertyName, propertyValue, valueType);

         return null;
      }

      /// <summary>
      /// Create a ratio measure data from string value.
      /// </summary>
      /// <param name="value">The value of the property.</param>
      /// <returns>The created property data.</returns>
      public static IFCData CreateRatioMeasureDataFromString(string value)
      {
         double propertyValue;
         if (Double.TryParse(value, out propertyValue))
            return IFCDataUtil.CreateRatioMeasureData(propertyValue);

         return null;
      }

      /// <summary>
      /// Create a normalised ratio property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateNormalisedRatioPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
         string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         if (ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue) != null)
            return CreateNormalisedRatioMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
         if (ParameterUtil.GetDoubleValueFromElement(elem, ifcPropertyName, out propertyValue) != null)
            return CreateNormalisedRatioMeasureProperty(file, ifcPropertyName, propertyValue, valueType);

         return null;
      }

      /// <summary>
      /// Create a normalised ratio measure data from string value.
      /// </summary>
      /// <param name="value">The value of the property.</param>
      /// <returns>The created property data.</returns>
      public static IFCData CreateNormalisedRatioMeasureDataFromString(string value)
      {
         double propertyValue;
         if (Double.TryParse(value, out propertyValue))
            return IFCDataUtil.CreateNormalisedRatioMeasureData(propertyValue);

         return null;
      }

      /// <summary>
      /// Create a linear velocity property from the element's or type's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearVelocityPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
         string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {

         IFCAnyHandle linearVelocityHandle = CreateDoublePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName,
            "IfcLinearVelocityMeasure", SpecTypeId.HvacVelocity, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(linearVelocityHandle))
            return linearVelocityHandle;

         return null;
      }

      /// <summary>
      /// Create a positive ratio property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.  Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositiveRatioPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
         string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         if (ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue) != null)
            return CreatePositiveRatioMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
         if (ParameterUtil.GetDoubleValueFromElement(elem, ifcPropertyName, out propertyValue) != null)
            return CreatePositiveRatioMeasureProperty(file, ifcPropertyName, propertyValue, valueType);

         return null;
      }

      /// <summary>
      /// Create a positive ratio measure data from string value.
      /// </summary>
      /// <param name="value">The value of the property.</param>
      /// <returns>The created property data.</returns>
      public static IFCData CreatePositiveRatioMeasureDataFromString(string value)
      {
         double propertyValue;
         if (Double.TryParse(value, out propertyValue))
            return IFCDataUtil.CreatePositiveRatioMeasureData(propertyValue);

         return null;
      }

      /// <summary>
      /// Create a plane angle measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePlaneAngleMeasurePropertyFromElement(IFCFile file, Element elem, string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleAngle(propertyValue);

            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, revitParameterName, propertyValue, SpecTypeId.Angle, "IfcPlaneAngleMeasure");
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, null);
            }
            else
            {
               return CreatePlaneAngleMeasurePropertyFromCache(file, ifcPropertyName, propertyValue, valueType);
            }
         }

         return null;
      }

      /// <summary>
      /// Create an area measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAreaMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleArea(propertyValue);

            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, revitParameterName, propertyValue, SpecTypeId.Area, "IfcAreaMeasure");
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, null);
            }
            else
            {
               return CreateAreaMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            }
         }
         return null;
      }

      /// <summary>
      /// Create an volume measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVolumeMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, string ifcPropertyName, PropertyValueType valueType)
      {
         double propertyValue;
         Parameter param = ParameterUtil.GetDoubleValueFromElement(elem, revitParameterName, out propertyValue);
         if (param != null)
         {
            if (!ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
               propertyValue = UnitUtil.ScaleVolume(propertyValue);

            if (valueType == PropertyValueType.BoundedValue)
            {
               IList<IFCData> boundedData = GetBoundedDataFromElement(elem, revitParameterName, propertyValue, SpecTypeId.Volume, "IfcVolumeMeasure");
               return CreateBoundedValuePropertyFromList(file, ifcPropertyName, boundedData, null);
            }
            else
            {
               return CreateVolumeMeasureProperty(file, ifcPropertyName, propertyValue, valueType);
            }
         }
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
      /// Create an area measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAreaMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateAreaMeasurePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateAreaMeasurePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create an volume measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="ifcPropertyName">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVolumeMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, string ifcPropertyName, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateVolumeMeasurePropertyFromElement(file, exporterIFC, elem, revitParameterName, ifcPropertyName, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = LabelUtils.GetLabelFor(revitBuiltInParam);
            propHnd = CreateVolumeMeasurePropertyFromElement(file, exporterIFC, elem, builtInParamName, ifcPropertyName, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

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
          IFCExportBodyParams extrustionData, HashSet<IFCAnyHandle> widthAsComplexQty = null)
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
               if (widthAsComplexQty == null)
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
            foreach (Solid solid in solids)
            {
               double largestFaceNetArea = 0.0;
               double largestFaceGrossArea = 0.0;
               foreach (Face face in solid.Faces)
               {
                  XYZ fNormal = face.ComputeNormal(new UV(0, 0));
                  if (MathUtil.IsAlmostZero(fNormal.Z))
                  {
                     if (face.Area > largestFaceNetArea)
                        largestFaceNetArea = face.Area;      // collecting largest face on the XY plane. It will be used for NetArea

                     IList<CurveLoop> fCurveLoops = face.GetEdgesAsCurveLoops();
                     double grArea = ExporterIFCUtils.ComputeAreaOfCurveLoops(new List<CurveLoop>() { fCurveLoops[0] });
                     if (grArea > largestFaceGrossArea)
                        largestFaceGrossArea = grArea;
                  }
               }
               netArea += largestFaceNetArea;
               grossArea += largestFaceGrossArea;
               volume += solid.Volume;
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
      /// Creates property sets for Revit groups and parameters, if export options is set.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="element">The Element.</param>
      /// <param name="elementSets">The collection of IFCAnyHandles to relate properties to.</param>
      public static void CreateInternalRevitPropertySets(ExporterIFC exporterIFC, Element element, 
         ISet<IFCAnyHandle> elementSets)
      {
         if (exporterIFC == null || element == null ||
             !ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportInternalRevit)
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
               ForgeTypeId parameterGroup = new ForgeTypeId (parameterElementGroup.Key);
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

            foreach (KeyValuePair<string, (string, HashSet<IFCAnyHandle>)> currPropertySet in propertySets[which])
            {
               if (currPropertySet.Value.Item2.Count == 0)
                  continue;

               string psetGUID = GUIDUtil.GenerateIFCGuidFrom(
                  GUIDUtil.CreateGUIDString(whichElement, "IfcPropertySet: " + currPropertySet.Key.ToString()));
               
               IFCAnyHandle propertySet = IFCInstanceExporter.CreatePropertySet(file, psetGUID, 
                  ExporterCacheManager.OwnerHistoryHandle, currPropertySet.Value.Item1, null, 
                  currPropertySet.Value.Item2);

               createdPropertySets.Add(propertySet);
            }

            if (which == 0)
               ExporterCacheManager.CreatedInternalPropertySets.Add(whichElement.Id, createdPropertySets, elementSets);
            else
               ExporterCacheManager.TypePropertyInfoCache.AddNewTypeProperties(typeId, createdPropertySets, elementSets);
         }
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
         ForgeTypeId fallbackType = null;
         try
         {
            fallbackType = parameter.GetUnitTypeId();
         }
         catch
         {
            // GetUnitTypeId() can fail for reasons that don't seem to be knowable in
            // advance, so we won't scale value in these cases.
         }

         return CreateRealPropertyByType(file, type, propertyName, propertyValue, valueType, fallbackType);
      }

      /// <summary>
      /// Creates property from real parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="parameterType">The type of the parameter.</param>
      /// <param name="propertyName">The name of the property.</param>
      /// <param name="propertyValue">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="fallbackType">The optional unit type. Can be used for scaling in final case</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRealPropertyByType(IFCFile file, ForgeTypeId parameterType, string propertyName, double propertyValue, PropertyValueType valueType, ForgeTypeId fallbackType = null)
      {
         IFCAnyHandle propertyHandle = null;
        
         if (parameterType == SpecTypeId.Angle)
         {
            propertyHandle = CreatePlaneAngleMeasurePropertyFromCache(file, propertyName,
               UnitUtil.ScaleAngle(propertyValue), valueType);
         }
         else if (parameterType == SpecTypeId.Area ||
            parameterType == SpecTypeId.CrossSection ||
            parameterType == SpecTypeId.ReinforcementArea ||
            parameterType == SpecTypeId.SectionArea)
         {
            double scaledValue = UnitUtil.ScaleArea(propertyValue);
            propertyHandle = CreateAreaMeasureProperty(file, propertyName,
                scaledValue, valueType);
         }
         else if (parameterType == SpecTypeId.BarDiameter ||
            parameterType == SpecTypeId.CrackWidth ||
            parameterType == SpecTypeId.Displacement ||
            parameterType == SpecTypeId.CableTraySize ||
            parameterType == SpecTypeId.ConduitSize ||
            parameterType == SpecTypeId.Length ||
            parameterType == SpecTypeId.DuctInsulationThickness ||
            parameterType == SpecTypeId.DuctLiningThickness ||
            parameterType == SpecTypeId.DuctSize ||
            parameterType == SpecTypeId.HvacRoughness ||
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
            propertyHandle = CreateLengthMeasurePropertyFromCache(file, propertyName,
                  UnitUtil.ScaleLength(propertyValue), valueType);
         }
         else if (parameterType == SpecTypeId.ColorTemperature)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.ColorTemperature, propertyValue);
            propertyHandle = CreateColorTemperaturePropertyFromValue(file, propertyName, scaledValue);
         }
         else if (parameterType == SpecTypeId.Currency)
         {
            IFCData currencyData = ExporterCacheManager.UnitsCache.ContainsKey("CURRENCY") ?
                  IFCDataUtil.CreateAsMeasure(propertyValue, "IfcMonetaryMeasure") :
                  IFCDataUtil.CreateAsMeasure(propertyValue, "IfcReal");
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
            propertyHandle = CreatePowerProperty(file, propertyName,
                  scaledValue, valueType);
         }
         else if (parameterType == SpecTypeId.Current)
         {
            double scaledValue = UnitUtil.ScaleElectricCurrent(propertyValue);
            propertyHandle = ElectricalCurrentPropertyUtil.CreateElectricalCurrentMeasureProperty(file, propertyName,
                  scaledValue, valueType);
         }
         else if (parameterType == SpecTypeId.Diffusivity)
         {
            double scaledValue = UnitUtil.ScaleMoistureDiffusivity(propertyValue);
            IFCData moistureDiffusivityData = IFCDataUtil.CreateAsMoistureDiffusivityMeasure(scaledValue);
            propertyHandle = CreateCommonProperty(file, propertyName, moistureDiffusivityData,
                  valueType, null);
         }
         else if (parameterType == SpecTypeId.Efficacy)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.Efficacy, propertyValue);
            propertyHandle = CreateElectricalEfficacyPropertyFromValue(file, propertyName, scaledValue);
         }
         else if (parameterType == SpecTypeId.ElectricalFrequency)
         {
            propertyHandle = FrequencyPropertyUtil.CreateFrequencyProperty(file, propertyName,
                  propertyValue, valueType);
         }
         else if (parameterType == SpecTypeId.Illuminance)
         {
            double scaledValue = UnitUtil.ScaleIlluminance(propertyValue);
            propertyHandle = CreateIlluminanceProperty(file, propertyName,
                  scaledValue, valueType);
         }
         else if (parameterType == SpecTypeId.LuminousFlux)
         {
            double scaledValue = UnitUtil.ScaleLuminousFlux(propertyValue);
            propertyHandle = CreateLuminousFluxMeasureProperty(file, propertyName,
                  scaledValue, valueType);
         }
         else if (parameterType == SpecTypeId.LuminousIntensity)
         {
            double scaledValue = UnitUtil.ScaleLuminousIntensity(propertyValue);
            propertyHandle = CreateLuminousIntensityProperty(file, propertyName,
                  scaledValue, valueType);
         }
         else if (parameterType == SpecTypeId.ElectricalPotential)
         {
            double scaledValue = UnitUtil.ScaleElectricVoltage(propertyValue);
            propertyHandle = ElectricVoltagePropertyUtil.CreateElectricVoltageMeasureProperty(file, propertyName,
                  scaledValue, valueType);
         }
         else if (parameterType == SpecTypeId.ElectricalTemperature ||
            parameterType == SpecTypeId.HvacTemperature ||
            parameterType == SpecTypeId.PipingTemperature)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.HvacTemperature, propertyValue);
            IFCData temperatureData = IFCDataUtil.CreateAsMeasure(scaledValue, "IfcThermodynamicTemperatureMeasure");
            propertyHandle = CreateCommonProperty(file, propertyName, temperatureData,
                  valueType, null);
         }
         else if (parameterType == SpecTypeId.HeatTransferCoefficient)
         {
            double scaledValue = UnitUtil.ScaleThermalTransmittance(propertyValue);
            IFCData temperatureData = IFCDataUtil.CreateAsMeasure(scaledValue, "IfcThermalTransmittanceMeasure");
            propertyHandle = CreateCommonProperty(file, propertyName, temperatureData,
                  valueType, null);
         }
         else if (parameterType == SpecTypeId.Force)
         {
            double scaledValue = UnitUtil.ScaleForce(propertyValue);
            propertyHandle = CreateForceProperty(file, propertyName,
                  scaledValue, valueType);
         }
         else if (parameterType == SpecTypeId.AreaForce)
         {
            double scaledValue = UnitUtil.ScalePlanarForce(propertyValue);
            propertyHandle = CreatePlanarForceProperty(file, propertyName,
                  scaledValue, valueType);
         }
         else if (parameterType == SpecTypeId.LinearForce)
         {
            double scaledValue = UnitUtil.ScaleLinearForce(propertyValue);
            propertyHandle = CreateLinearForceProperty(file, propertyName,
                  scaledValue, valueType);
         }
         else if (parameterType == SpecTypeId.AirFlow ||
            parameterType == SpecTypeId.Flow)
         {
            double scaledValue = UnitUtil.ScaleVolumetricFlowRate(propertyValue);
            propertyHandle = CreateVolumetricFlowRateMeasureProperty(file, propertyName,
                  scaledValue, valueType);
         }
         else if (parameterType == SpecTypeId.HvacFriction)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.HvacFriction, propertyValue);
            IFCData frictionData = IFCDataUtil.CreateAsMeasure(scaledValue, "IfcReal");
            propertyHandle = CreateCommonProperty(file, propertyName, frictionData,
                  valueType, "FRICTIONLOSS");
         }
         else if (parameterType == SpecTypeId.HvacPressure ||
            parameterType == SpecTypeId.PipingPressure ||
            parameterType == SpecTypeId.Stress)
         {
            double scaledValue = UnitUtil.ScalePressure(propertyValue);
            IFCData pressureData = IFCDataUtil.CreateAsMeasure(scaledValue, "IfcPressureMeasure");
            propertyHandle = CreateCommonProperty(file, propertyName, pressureData,
                  valueType, null);
         }
         else if (parameterType == SpecTypeId.HvacVelocity ||
            parameterType == SpecTypeId.PipingVelocity)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.HvacVelocity, propertyValue);
            IFCData linearVelocityData = IFCDataUtil.CreateAsMeasure(scaledValue, "IfcLinearVelocityMeasure");
            propertyHandle = CreateCommonProperty(file, propertyName, linearVelocityData,
                  valueType, null);
         }
         else if (parameterType == SpecTypeId.Mass)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.Mass, propertyValue);
            IFCData massData = IFCDataUtil.CreateAsMeasure(scaledValue, "IfcMassMeasure");
            propertyHandle = CreateCommonProperty(file, propertyName, massData,
                  valueType, null);
         }
         else if (parameterType == SpecTypeId.MassDensity)
         {
            double scaledValue = UnitUtil.ScaleMassDensity(propertyValue);
            IFCData massDensityData = IFCDataUtil.CreateAsMeasure(scaledValue, "IfcMassDensityMeasure");
            propertyHandle = CreateCommonProperty(file, propertyName, massDensityData,
                  valueType, null);
         }
         else if (parameterType == SpecTypeId.PipingDensity)
         {
            double scaledValue = UnitUtil.ScaleIonConcentration(propertyValue);
            IFCData ionConcentrationData = IFCDataUtil.CreateAsIonConcentrationMeasure(scaledValue);
            propertyHandle = CreateCommonProperty(file, propertyName, ionConcentrationData,
                  valueType, null);
         }
         else if (parameterType == SpecTypeId.MomentOfInertia)
         {
            double scaledValue = UnitUtil.ScaleMomentOfInertia(propertyValue);
            IFCData momentOfInertiaData = IFCDataUtil.CreateAsMomentOfInertiaMeasure(scaledValue);
            propertyHandle = CreateCommonProperty(file, propertyName, momentOfInertiaData,
                  valueType, null);
         }
         else if (parameterType == SpecTypeId.Number)
         {
            propertyHandle = CreateRealPropertyFromCache(file, propertyName, propertyValue, valueType);
         }
         else if (parameterType == SpecTypeId.PipingVolume ||
            parameterType == SpecTypeId.ReinforcementVolume ||
            parameterType == SpecTypeId.SectionModulus ||
            parameterType == SpecTypeId.Volume)
         {
            double scaledValue = UnitUtil.ScaleVolume(propertyValue);
            propertyHandle = CreateVolumeMeasureProperty(file, propertyName,
                  scaledValue, valueType);
         }
         else if (parameterType == SpecTypeId.PipingMassPerTime)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.PipingMassPerTime, propertyValue);
            IFCData massFlowRateData = IFCDataUtil.CreateAsMeasure(scaledValue, "IfcMassFlowRateMeasure");
            propertyHandle = CreateCommonProperty(file, propertyName, massFlowRateData,
                  valueType, null);
         }
         else if (parameterType == SpecTypeId.AngularSpeed)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.AngularSpeed, propertyValue);
            IFCData rotationalFrequencyData = IFCDataUtil.CreateAsMeasure(scaledValue, "IfcRotationalFrequencyMeasure");
            propertyHandle = CreateCommonProperty(file, propertyName, rotationalFrequencyData,
                  valueType, null);
         }
         else if (parameterType == SpecTypeId.ThermalConductivity)
         {
            double scaledValue = UnitUtil.ScaleThermalConductivity(propertyValue);
            IFCData thermalConductivityData = IFCDataUtil.CreateAsThermalConductivityMeasure(scaledValue);
            propertyHandle = CreateCommonProperty(file, propertyName, thermalConductivityData,
                  valueType, null);
         }
         else if (parameterType == SpecTypeId.SpecificHeat)
         {
            double scaledValue = UnitUtil.ScaleSpecificHeatCapacity(propertyValue);
            IFCData specificHeatData = IFCDataUtil.CreateAsSpecificHeatCapacityMeasure(scaledValue);
            propertyHandle = CreateCommonProperty(file, propertyName, specificHeatData,
                  valueType, null);
         }
         else if (parameterType == SpecTypeId.Permeability)
         {
            double scaledValue = UnitUtil.ScaleVaporPermeability(propertyValue);
            IFCData permeabilityData = IFCDataUtil.CreateAsVaporPermeabilityMeasure(scaledValue);
            propertyHandle = CreateCommonProperty(file, propertyName, permeabilityData,
                  valueType, null);
         }
         else if (parameterType == SpecTypeId.HvacViscosity)
         {
            double scaledValue = UnitUtil.ScaleDynamicViscosity(propertyValue);
            IFCData hvacViscosityData = IFCDataUtil.CreateAsDynamicViscosityMeasure(scaledValue);
            propertyHandle = CreateCommonProperty(file, propertyName, hvacViscosityData,
                  valueType, null);
         }
         else if (parameterType == SpecTypeId.ThermalExpansionCoefficient)
         {
            double scaledValue = UnitUtil.ScaleThermalExpansionCoefficient(propertyValue);
            IFCData thermalExpansionCoefficientData = IFCDataUtil.CreateAsThermalExpansionCoefficientMeasure(scaledValue);
            propertyHandle = CreateCommonProperty(file, propertyName, thermalExpansionCoefficientData,
                  valueType, null);
         }
         else if (parameterType == SpecTypeId.ElectricalResistivity)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.ElectricalResistivity, propertyValue);
            IFCData electricalResistivityData = IFCDataUtil.CreateAsMeasure(scaledValue, "IfcReal");
            propertyHandle = CreateCommonProperty(file, propertyName, electricalResistivityData,
                  valueType, "ELECTRICALRESISTIVITY");
         }
         else if (parameterType == SpecTypeId.SpecificHeatOfVaporization)
         {
            double scaledValue = UnitUtil.ScaleHeatingValue(propertyValue);
            IFCData heatingValueData = IFCDataUtil.CreateAsHeatingValueMeasure(scaledValue);
            propertyHandle = CreateCommonProperty(file, propertyName, heatingValueData,
                  valueType, null);
         }
         else if (parameterType == SpecTypeId.IsothermalMoistureCapacity)
         {
            double scaledValue = UnitUtil.ScaleIsothermalMoistureCapacity(propertyValue);
            IFCData isothermalMoistureCapacityData = IFCDataUtil.CreateAsIsothermalMoistureCapacityMeasure(scaledValue);
            propertyHandle = CreateCommonProperty(file, propertyName, isothermalMoistureCapacityData,
                  valueType, null);
         }
         else
         {
            double scaledValue = propertyValue;
            if (fallbackType != null)
               scaledValue = UnitUtils.ConvertFromInternalUnits(propertyValue, fallbackType);

            propertyHandle = CreateRealPropertyFromCache(file, propertyName, scaledValue, valueType);
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
         CreateInternalRevitPropertySets(exporterIFC, elementType, associatedObjectIds);

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

            IList<IList<PropertySetDescription>> psetsToCreate = ExporterCacheManager.ParameterCache.PropertySets;

            IList<PropertySetDescription> currPsetsToCreate = ExporterUtil.GetCurrPSetsToCreate(prodTypeHnd, psetsToCreate);
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
   }
}