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

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using static Revit.IFC.Export.Utility.ParameterUtil;
using PropertyDescription = Revit.IFC.Export.Utility.ParameterUtil.PropertyDescription;

namespace Revit.IFC.Export.Exporter.PropertySet
{
   using ParameterMappingKey = Tuple<PropertySetupType, string, ElementId, string>;

   /// <summary>
   /// Provides static methods to create varies IFC properties.
   /// </summary>
   public class PropertyUtil
   {
      private static readonly ISet<IFCEntityType> PreIFC4EntitiesWithNoRelatedType = new HashSet<IFCEntityType>()
      {
         IFCEntityType.IfcFooting,
         IFCEntityType.IfcPile,
         IFCEntityType.IfcRamp,
         IFCEntityType.IfcRoof,
         IFCEntityType.IfcStair
      };

      /// <summary>
      /// Maps IFC4 quantity names to their corresponding names in pre-IFC4 versions (IFC2x2 and IFC2x3)
      /// </summary>
      private static Dictionary<IFCVersion, Dictionary<(string, string), string>> PreIFC4QuantityNamesMap = new()
      {
         { IFCVersion.IFC2x2, 
            new() { 
               { ("Qto_BeamBaseQuantities", "Length"), "NominalLength" },
               { ("Qto_BeamBaseQuantities", "GrossSurfaceArea"), "TotalSurfaceArea" },
               { ("Qto_BeamBaseQuantities", "CrossSectionArea"), "CrossSectionArea" },
               { ("Qto_BeamBaseQuantities", "GrossVolume"), "GrossVolume" },
               { ("Qto_BeamBaseQuantities", "NetVolume"), "NetVolume" },
               { ("Qto_BeamBaseQuantities", "OuterSurfaceArea"), "OuterSurfaceArea" },
               { ("Qto_ColumnBaseQuantities", "Length"), "NominalLength" },
               { ("Qto_ColumnBaseQuantities", "GrossSurfaceArea"), "TotalSurfaceArea" },
               { ("Qto_ColumnBaseQuantities", "CrossSectionArea"), "CrossSectionArea" },
               { ("Qto_ColumnBaseQuantities", "GrossVolume"), "GrossVolume" },
               { ("Qto_ColumnBaseQuantities", "NetVolume"), "NetVolume" },
               { ("Qto_ColumnBaseQuantities", "OuterSurfaceArea"), "OuterSurfaceArea" },
               { ("Qto_MemberBaseQuantities", "Length"), "NominalLength" },
               { ("Qto_MemberBaseQuantities", "GrossSurfaceArea"), "TotalSurfaceArea" },
               { ("Qto_MemberBaseQuantities", "CrossSectionArea"), "CrossSectionArea" },
               { ("Qto_MemberBaseQuantities", "GrossVolume"), "GrossVolume" },
               { ("Qto_MemberBaseQuantities", "NetVolume"), "NetVolume" },
               { ("Qto_MemberBaseQuantities", "OuterSurfaceArea"), "OuterSurfaceArea" },
               { ("Qto_OpeningElementBaseQuantities", "Area"), "OpeningArea" },
               { ("Qto_SlabBaseQuantities", "GrossArea"), "SurfaceArea" },
               { ("Qto_SlabBaseQuantities", "GrossVolume"), "GrossVolume" },
               { ("Qto_SlabBaseQuantities", "NetVolume"), "NetVolume" },
               { ("Qto_WallBaseQuantities", "Length"), "NominalLength" },
               { ("Qto_WallBaseQuantities", "GrossVolume"), "GrossVolume" },
               { ("Qto_WallBaseQuantities", "NetVolume"), "NetVolume" },
               { ("Qto_WallBaseQuantities", "GrossFootprintArea"), "GrossFootprintArea" }
            }
         },
         { IFCVersion.IFC2x3,
            new() {
               { ("Qto_BeamBaseQuantities", "Length"), "NominalLength" },
               { ("Qto_BeamBaseQuantities", "GrossSurfaceArea"), "TotalSurfaceArea" },
               { ("Qto_BeamBaseQuantities", "CrossSectionArea"), "CrossSectionArea" },
               { ("Qto_BeamBaseQuantities", "GrossVolume"), "GrossVolume" },
               { ("Qto_BeamBaseQuantities", "NetVolume"), "NetVolume" },
               { ("Qto_BeamBaseQuantities", "OuterSurfaceArea"), "OuterSurfaceArea" },
               { ("Qto_ColumnBaseQuantities", "Length"), "NominalLength" },
               { ("Qto_ColumnBaseQuantities", "GrossSurfaceArea"), "TotalSurfaceArea" },
               { ("Qto_ColumnBaseQuantities", "CrossSectionArea"), "CrossSectionArea" },
               { ("Qto_ColumnBaseQuantities", "GrossVolume"), "GrossVolume" },
               { ("Qto_ColumnBaseQuantities", "NetVolume"), "NetVolume" },
               { ("Qto_ColumnBaseQuantities", "OuterSurfaceArea"), "OuterSurfaceArea" },
               { ("Qto_MemberBaseQuantities", "Length"), "NominalLength" },
               { ("Qto_MemberBaseQuantities", "GrossSurfaceArea"), "TotalSurfaceArea" },
               { ("Qto_MemberBaseQuantities", "CrossSectionArea"), "CrossSectionArea" },
               { ("Qto_MemberBaseQuantities", "GrossVolume"), "GrossVolume" },
               { ("Qto_MemberBaseQuantities", "NetVolume"), "NetVolume" },
               { ("Qto_MemberBaseQuantities", "OuterSurfaceArea"), "OuterSurfaceArea" },
               { ("Qto_OpeningElementBaseQuantities", "Area"), "NominalArea" },
               { ("Qto_OpeningElementBaseQuantities", "Volume"), "NominalVolume" },
               { ("Qto_SlabBaseQuantities", "Width"), "NominalWidth" },
               { ("Qto_SlabBaseQuantities", "GrossArea"), "GrossArea" },
               { ("Qto_SlabBaseQuantities", "NetArea"), "NetArea" },
               { ("Qto_SlabBaseQuantities", "GrossVolume"), "GrossVolume" },
               { ("Qto_SlabBaseQuantities", "NetVolume"), "NetVolume" },
               { ("Qto_WallBaseQuantities", "Length"), "NominalLength" },
               { ("Qto_WallBaseQuantities", "Width"), "NominalWidth" },
               { ("Qto_WallBaseQuantities", "Height"), "NominalHeight" },
               { ("Qto_WallBaseQuantities", "GrossSideArea"), "GrossSideArea" },
               { ("Qto_WallBaseQuantities", "NetSideArea"), "NetSideArea" },
               { ("Qto_WallBaseQuantities", "GrossVolume"), "GrossVolume" },
               { ("Qto_WallBaseQuantities", "NetVolume"), "NetVolume" },
               { ("Qto_WallBaseQuantities", "GrossFootprintArea"), "GrossFootprintArea" }
            }
         }
      };

      /// <summary>
      /// Contains a list of built-in parameters that have a value represented by button text in Revit.
      /// </summary>
      /// <remarks>Regardless of the underlying data type, these parameters will be exported as Text parameters
      /// that have the displayed value in Revit.  If a user wants the data that created this text, they should
      /// export that separately.</remarks>
      public class ProxyParameter
      {
         public ProxyParameter()
         {
         }

         public static bool IsProxyParameter(BuiltInParameter param)
         {
            return param switch
            {
               BuiltInParameter.FBX_LIGHT_INITIAL_COLOR_CTRL or
               BuiltInParameter.FBX_LIGHT_INITIAL_INTENSITY or
               BuiltInParameter.FBX_LIGHT_LOSS_FACTOR_CTRL => true,
               _ => false
            };
         }

         private static string GetDoubleProxyValueAsString(Element element, BuiltInParameter proxyParam)
         {
            Parameter tempParameter = element.get_Parameter(proxyParam);
            if (tempParameter == null || !tempParameter.HasValue)
               return null;
            return tempParameter.AsValueString();
         }

         private static string GetLightIntensityAsString(Element element)
         {
            Parameter inputMethodParameter = element.get_Parameter(BuiltInParameter.FBX_LIGHT_INITIAL_INTENSITY_INPUT_METHOD);
            if (inputMethodParameter == null || !inputMethodParameter.HasValue)
               return null;

            int inputMethod = inputMethodParameter.AsInteger();
            switch (inputMethod)
            {
               case 0:
                  Parameter lightWattageParameter = element.get_Parameter(BuiltInParameter.FBX_LIGHT_WATTAGE);
                  Parameter lightEfficacyParameter = element.get_Parameter(BuiltInParameter.FBX_LIGHT_EFFICACY);
                  string wattage = (lightWattageParameter?.HasValue ?? false) ? lightWattageParameter.AsValueString() : null;
                  string efficacy = (lightEfficacyParameter?.HasValue ?? false) ? lightEfficacyParameter.AsValueString() : null;
                  return (wattage != null && efficacy != null) ? $"{wattage} @ {efficacy}" : null;
               case 1:
                  Parameter luminousIntensityParameter = element.get_Parameter(BuiltInParameter.FBX_LIGHT_LIMUNOUS_INTENSITY);
                  return (luminousIntensityParameter?.HasValue ?? false) ? luminousIntensityParameter.AsValueString() : null;
               case 2:
                  Parameter luminousFlux = element.get_Parameter(BuiltInParameter.FBX_LIGHT_LIMUNOUS_FLUX);
                  return (luminousFlux?.HasValue ?? false) ? luminousFlux.AsValueString() : null;
               case 3:
                  Parameter illuminanceParameter = element.get_Parameter(BuiltInParameter.FBX_LIGHT_ILLUMINANCE);
                  Parameter atDistanceParameter = element.get_Parameter(BuiltInParameter.FBX_LIGHT_AT_A_DISTANCE);
                  string illuminance = (illuminanceParameter?.HasValue ?? false) ? illuminanceParameter.AsValueString() : null;
                  string atDistance = (atDistanceParameter?.HasValue ?? false) ? atDistanceParameter.AsValueString() : null;
                  return (illuminance != null && atDistance != null) ? $"{illuminance} @ {atDistance}" : null;
            }

            return null;
         }

         public static string GetProxyValue(Element element, EvaluatedParameter parameter)
         {
            if (parameter == null || element == null)
               return null;
            switch (parameter.Definition.Id.Value)
            {
               case (long)BuiltInParameter.FBX_LIGHT_INITIAL_COLOR_CTRL:
                  return GetDoubleProxyValueAsString(element, BuiltInParameter.FBX_LIGHT_INITIAL_COLOR_TEMPERATURE);
               case (long)BuiltInParameter.FBX_LIGHT_INITIAL_INTENSITY:
                  return GetLightIntensityAsString(element);
               case (long)BuiltInParameter.FBX_LIGHT_LOSS_FACTOR_CTRL:
                  return GetDoubleProxyValueAsString(element, BuiltInParameter.FBX_LIGHT_TOTAL_LIGHT_LOSS);
               default:
                  return null;
            }
         }
      }

      /// <summary>
      /// Get a list of IFC entities that have no related type before IFC4
      /// </summary>
      public static ISet<IFCEntityType> EntitiesWithNoRelatedType
      {
         get
         {
            return ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 ? PreIFC4EntitiesWithNoRelatedType : null;
         }
      }

      public static IFCAnyHandle CreateCommonPropertyFromList(IFCFile file, PropertyDescription propertyDescription, IList<IFCData> valueList, PropertyValueType valueType, string unitTypeKey)
      {
         if (valueList == null || valueList.All(x => x == null))
            return null;

         IFCAnyHandle unitHnd = (!ExporterCacheManager.ExportOptionsCache.ExportAsReferenceView && unitTypeKey != null) ? ExporterCacheManager.UnitsCache.FindUserDefinedUnit(unitTypeKey) : null;

         switch (valueType)
         {
            case PropertyValueType.EnumeratedValue:
               {
                  return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyDescription, valueList, null);
               }
            case PropertyValueType.SingleValue:
               {
                  return IFCInstanceExporter.CreatePropertySingleValue(file, propertyDescription, valueList.First(), unitHnd);
               }
            case PropertyValueType.ListValue:
               {
                  return IFCInstanceExporter.CreatePropertyListValue(file, propertyDescription, valueList, unitHnd);
               }
            case PropertyValueType.BoundedValue:
               {
                  return CreateBoundedValuePropertyFromList(file, propertyDescription, valueList, unitTypeKey);
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

      public static IFCAnyHandle CreateCommonProperty(IFCFile file, PropertyDescription propertyDescription, IFCData valueData, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateCommonPropertyFromList(file, propertyDescription, new List<IFCData>() { valueData }, valueType, unitTypeKey);
      }

      /// <summary>
      /// Creates an IfcPropertyBoundedValue.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="propertyName">The name.</param>
      /// <param name="valueDataList">The list of values.</param>
      /// <param name="unitTypeKey">The unit name.</param>
      protected static IFCAnyHandle CreateBoundedValuePropertyFromList(IFCFile file, PropertyDescription propertyDescription, IList<IFCData> valueDataList, string unitTypeKey)
      {
         if (valueDataList.Count < 1)
            throw new InvalidOperationException("Invalid bounded property!");
         IFCAnyHandle unitHnd = (!ExporterCacheManager.ExportOptionsCache.ExportAsReferenceView && unitTypeKey != null) ? ExporterCacheManager.UnitsCache.FindUserDefinedUnit(unitTypeKey) : null;

         IFCData setPointValue = valueDataList[0];
         IFCData upperBoundValue = valueDataList.Count > 1 ? valueDataList[1] : null;
         IFCData lowerBoundValue = valueDataList.Count > 2 ? valueDataList[2] : null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 && upperBoundValue == null && lowerBoundValue == null)
         {
            // In IFC2x3, IfcPropertyBoundedValue has no SetPointValue attribute and upper/lower values should satisfy the rule WR22 : EXISTS(UpperBoundValue) OR EXISTS(LowerBoundValue);
            return IFCInstanceExporter.CreatePropertySingleValue(file, propertyDescription, setPointValue, null);
         }
         else
         {
            return IFCInstanceExporter.CreatePropertyBoundedValue(file, propertyDescription, lowerBoundValue, upperBoundValue, setPointValue, unitHnd);
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
      public static IFCAnyHandle CreateTableProperty(IFCFile file, PropertyDescription propertyDescription, IList<IFCData> definingValues, IList<IFCData> definedValues, string definingUnitTypeKey, string definedUnitTypeKey)
      {
         IFCAnyHandle definingUnitHnd = (!ExporterCacheManager.ExportOptionsCache.ExportAsReferenceView && definingUnitTypeKey != null) ? ExporterCacheManager.UnitsCache.FindUserDefinedUnit(definingUnitTypeKey) : null;
         IFCAnyHandle definedUnitHnd = (!ExporterCacheManager.ExportOptionsCache.ExportAsReferenceView && definedUnitTypeKey != null) ? ExporterCacheManager.UnitsCache.FindUserDefinedUnit(definedUnitTypeKey) : null;

         return IFCInstanceExporter.CreatePropertyTableValue(file, propertyDescription, definingValues, definedValues, definingUnitHnd, definedUnitHnd);
      }

      /// <summary>
      /// Create a label property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="propertyEnumerationType">The type of enumeration, if appropriate.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLabelProperty(IFCFile file, PropertyDescription propertyDescription, string value,
         PropertyValueType valueType, Type propertyEnumerationType)
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

                  return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyDescription, valueList, null);
               }
            case PropertyValueType.SingleValue:
               {
                  return IFCInstanceExporter.CreatePropertySingleValue(file, propertyDescription,
                     IFCDataUtil.CreateAsLabel(value), null);
               }
            case PropertyValueType.ListValue:
               {
                  IList<IFCData> valueList = new List<IFCData>() { IFCDataUtil.CreateAsLabel(value) };
                  return IFCInstanceExporter.CreatePropertyListValue(file, propertyDescription, valueList, null);
               }
            default:
               throw new InvalidOperationException("Missing case!");
         }
      }

      /// <summary>
      /// Create a text property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTextProperty(IFCFile file, PropertyDescription propertyDescription, string value, PropertyValueType valueType)
      {
         IFCData textData = IFCDataUtil.CreateAsText(value);
         return CreateCommonProperty(file, propertyDescription, textData, valueType, null);
      }

      /// <summary>
      /// Create a text property, using the cached value if possible.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTextPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, string value, PropertyValueType valueType)
      {
         bool canCache = (value == string.Empty);
         StringPropertyInfoCache stringInfoCache = null;
         IFCAnyHandle textHandle = null;

         if (canCache)
         {
            stringInfoCache = ExporterCacheManager.PropertyInfoCache.TextCache;
            textHandle = stringInfoCache.Find(null, propertyDescription.Name, value);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(textHandle))
               return textHandle;
         }

         textHandle = CreateTextProperty(file, propertyDescription, value, valueType);

         if (canCache)
            stringInfoCache.Add(null, propertyDescription.Name, value, textHandle);

         return textHandle;
      }

      /// <summary>
      /// Create a text property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTextPropertyFromElement(IFCFile file, Element elem, string revitParameterName, PropertyDescription propertyDescription, PropertyValueType valueType, Type propertyEnumerationType)
      {
         if (elem == null)
            return null;

         (_, string propertyValue) = ParameterUtil.GetStringValueFromElement(elem, false, revitParameterName);
         return propertyValue != null ? CreateTextPropertyFromCache(file, propertyDescription, propertyValue, valueType) : null;
      }

      /// <summary>
      /// Create a text property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTextPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
         BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType, Type propertyEnumerationType)
      {
         // For Instance
         IFCAnyHandle propHnd = CreateTextPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType,
             propertyEnumerationType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateTextPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType, propertyEnumerationType);
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
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="cacheAllStrings">Whether to cache all strings (true), or only the empty string (false).</param>
      /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLabelPropertyFromCache(IFCFile file, ElementId parameterId,
         PropertyDescription propertyDescription, string value, PropertyValueType valueType, bool cacheAllStrings,
         Type propertyEnumerationType)
      {
         bool canCache = (value == string.Empty) || cacheAllStrings;
         StringPropertyInfoCache stringInfoCache = null;
         IFCAnyHandle labelHandle = null;

         string propertyName = propertyDescription.Name;
         if (canCache)
         {
            stringInfoCache = ExporterCacheManager.PropertyInfoCache.LabelCache;
            labelHandle = stringInfoCache.Find(parameterId, propertyName, value);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(labelHandle))
               return labelHandle;
         }

         labelHandle = CreateLabelProperty(file, propertyDescription, value, valueType, propertyEnumerationType);

         if (canCache)
            stringInfoCache.Add(parameterId, propertyName, value, labelHandle);

         return labelHandle;
      }

      /// <summary>
      /// Create a label property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLabelProperty(IFCFile file, PropertyDescription propertyDescription, IList<string> values, PropertyValueType valueType,
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
                  return IFCInstanceExporter.CreatePropertyEnumeratedValue(file, propertyDescription, valueList, null);
               }
            case PropertyValueType.ListValue:
               {
                  IList<IFCData> valueList = new List<IFCData>();
                  foreach (string value in values)
                  {
                     valueList.Add(IFCDataUtil.CreateAsLabel(value));
                  }
                  return IFCInstanceExporter.CreatePropertyListValue(file, propertyDescription, valueList, null);
               }
            default:
               throw new InvalidOperationException("Missing case!");
         }
      }

      /// <summary>
      /// Create an identifier property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIdentifierProperty(IFCFile file, PropertyDescription propertyDescription, string value, PropertyValueType valueType)
      {
         IFCData idData = IFCDataUtil.CreateAsIdentifier(value);
         return CreateCommonProperty(file, propertyDescription, idData, valueType, null);
      }

      /// <summary>
      /// Create an identifier property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIdentifierPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, string value, PropertyValueType valueType)
      {
         StringPropertyInfoCache stringInfoCache = ExporterCacheManager.PropertyInfoCache.IdentifierCache;
         string propertyName = propertyDescription.Name;
         IFCAnyHandle stringHandle = stringInfoCache.Find(null, propertyName, value);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(stringHandle))
            return stringHandle;

         stringHandle = CreateIdentifierProperty(file, propertyDescription, value, valueType);

         stringInfoCache.Add(null, propertyName, value, stringHandle);
         return stringHandle;
      }

      /// <summary>
      /// Create a boolean property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateBooleanProperty(IFCFile file, PropertyDescription propertyDescription, bool value, PropertyValueType valueType)
      {
         IFCData boolData = IFCDataUtil.CreateAsBoolean(value);
         return CreateCommonProperty(file, propertyDescription, boolData, valueType, null);
      }

      /// <summary>
      /// Create a logical property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLogicalProperty(IFCFile file, PropertyDescription propertyDescription, IFCLogical value, PropertyValueType valueType)
      {
         IFCData logicalData = IFCDataUtil.CreateAsLogical(value);
         return CreateCommonProperty(file, propertyDescription, logicalData, valueType, null);
      }

      /// <summary>
      /// Create a boolean property or gets one from cache.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="propertyName">The property name.</param>
      /// <param name="value">The value.</param>
      /// <param name="valueType">The value type.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateBooleanPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, bool value, PropertyValueType valueType)
      {
         BooleanPropertyInfoCache boolInfoCache = ExporterCacheManager.PropertyInfoCache.BooleanCache;
         string propertyName = propertyDescription.Name;
         IFCAnyHandle boolHandle = boolInfoCache.Find(propertyName, value);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(boolHandle))
            return boolHandle;

         boolHandle = CreateBooleanProperty(file, propertyDescription, value, valueType);
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
      public static IFCAnyHandle CreateLogicalPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IFCLogical value, PropertyValueType valueType)
      {
         LogicalPropertyInfoCache logicalInfoCache = ExporterCacheManager.PropertyInfoCache.LogicalCache;
         string propertyName = propertyDescription.Name;
         IFCAnyHandle logicalHandle = logicalInfoCache.Find(propertyName, value);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(logicalHandle))
            return logicalHandle;

         logicalHandle = CreateLogicalProperty(file, propertyDescription, value, valueType);
         logicalInfoCache.Add(propertyName, value, logicalHandle);
         return logicalHandle;
      }

      /// <summary>
      /// Create an integer property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIntegerProperty(IFCFile file, PropertyDescription propertyDescription, int value, PropertyValueType valueType)
      {
         IFCData intData = IFCDataUtil.CreateAsInteger(value);
         return CreateCommonProperty(file, propertyDescription, intData, valueType, null);
      }

      /// <summary>
      /// Create an integer property or gets one from cache.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="propertyName">The property name.</param>
      /// <param name="value">The value.</param>
      /// <param name="valueType">The value type.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIntegerPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, int value, PropertyValueType valueType)
      {
         bool canCache = (value >= -10 && value <= 10);
         IFCAnyHandle intHandle = null;
         string propertyName = propertyDescription.Name;
         IntegerPropertyInfoCache intInfoCache = null;
         if (canCache)
         {
            intInfoCache = ExporterCacheManager.PropertyInfoCache.IntegerCache;
            intHandle = intInfoCache.Find(propertyName, value);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(intHandle))
               return intHandle;
         }

         intHandle = CreateIntegerProperty(file, propertyDescription, value, valueType);
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
         if (Math.Abs(value) <= 10.0 + MathUtil.Eps)
            multiplier = 0.05;
         else if (Math.Abs(value) <= 300.0 + MathUtil.Eps)
            multiplier = 0.5;

         double valueCorrected = Math.Floor(value / multiplier + MathUtil.Eps);
         if (MathUtil.IsAlmostZero(value / multiplier - valueCorrected))
            return valueCorrected * multiplier;

         return null;
      }

      /// <summary>Create a count measure property.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateCountMeasureProperty(IFCFile file, PropertyDescription propertyDescription, double value, PropertyValueType valueType)
      {
         IFCData countData = IFCDataUtil.CreateAsCountMeasure(value);
         return CreateCommonProperty(file, propertyDescription, countData, valueType, null);
      }

      /// <summary>Create a count measure property. From IFC4x3 onward the value has been changed to Integer</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateCountMeasureProperty(IFCFile file, PropertyDescription propertyDescription, int value, PropertyValueType valueType)
      {
         IFCData countData = IFCDataUtil.CreateAsCountMeasure(value);
         return CreateCommonProperty(file, propertyDescription, countData, valueType, null);
      }

      /// <summary>Create a ClassificationReference property.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateClassificationReferenceProperty(IFCFile file, PropertyDescription propertyDescription, string value)
      {
         IFCAnyHandle classificationReferenceHandle =
            IFCInstanceExporter.CreateClassificationReference(file, null, value, null, null, null);
         return IFCInstanceExporter.CreatePropertyReferenceValue(file, propertyDescription, null, classificationReferenceHandle);
      }

      /// <summary>
      /// Create a Time measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTimePropertyFromElement(IFCFile file, Element elem,
          string revitParameterName, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         return CreateDoublePropertyFromElement(file, elem, revitParameterName, propertyDescription,
            "IfcTimeMeasure", SpecTypeId.Time, valueType);
      }

      public static IFCAnyHandle CreateUserDefinedRealPropertyFromElement(IFCFile file, Element elem,
         string revitParameterName, PropertyDescription propertyDescription, PropertyValueType valueType, ForgeTypeId specType, string unitTypeKey)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, specType, valueType);
         return CreateRealProperty(file, propertyDescription, doubleValues, valueType, unitTypeKey);
      }

      /// <summary>
      /// Create a currency measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property.  Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateCurrencyPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         string measureName = ExporterCacheManager.UnitsCache.HasCurrencyUnit() ? "IfcMonetaryMeasure" : "IfcReal";

         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Number, valueType);
         return CreateRealProperty(file, propertyDescription, doubleValues, valueType, measureName);
      }

      public static IFCAnyHandle CreateUserDefinedRealPropertyFromElement(IFCFile file, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType, ForgeTypeId specType, string unitTypeKey)
      {
         IFCAnyHandle propHnd = CreateUserDefinedRealPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType, specType, unitTypeKey);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateUserDefinedRealPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType, specType, unitTypeKey);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateCurrencyPropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateCurrencyPropertyFromElement(file, exporterIFC, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateCurrencyPropertyFromElement(file, exporterIFC, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTimePropertyFromElement(IFCFile file, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateTimePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateTimePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateClassificationReferencePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, PropertyDescription propertyDescription)
      {
         if (elem == null)
            return null;

         (_, string propertyValue) = ParameterUtil.GetStringValueFromElement(elem, false, revitParameterName);
         return (propertyValue != null) ? CreateClassificationReferenceProperty(file, propertyDescription, propertyValue) : null;
      }

      /// <summary>
      /// Create a generic measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="measureType">The IfcMeasure type of the property.</param>
      /// <param name="specTypeId">Identifier of the property spec.</param>
      /// <param name="valueType">The property value type of the property.</param>
      /// <returns>The created property handle.</returns>
      private static IFCAnyHandle CreateDoublePropertyFromElement(IFCFile file, Element elem,
          string revitParameterName, PropertyDescription propertyDescription, string measureType, ForgeTypeId specTypeId, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, specTypeId, valueType);

         IList<IFCData> doubleData = new List<IFCData>();
         foreach (var val in doubleValues)
            doubleData.Add(val.HasValue ? IFCData.CreateDoubleOfType(val.Value, measureType) : null);

         return CreateCommonPropertyFromList(file, propertyDescription, doubleData, valueType, null);
      }

      /// <summary>
      /// Create an IfcClassificationReference property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateClassificationReferencePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription)
      {
         IFCAnyHandle propHnd = CreateClassificationReferencePropertyFromElement(file, exporterIFC, elem, revitParameterName, propertyDescription);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateClassificationReferencePropertyFromElement(file, exporterIFC, elem, builtInParamName, propertyDescription);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLabelPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
         PropertyDescription propertyDescription, PropertyValueType valueType, Type propertyEnumerationType)
      {
         if (elem == null)
         {
            return null;
         }

         (EvaluatedParameter parameter, string propertyValue) = GetStringValueFromElement(elem, false, revitParameterName);
         return parameter != null ? 
            CreateLabelPropertyFromCache(file, parameter.Definition.Id, propertyDescription, propertyValue, valueType, false, propertyEnumerationType) :
            null;
      }

      /// <summary>
      /// Create a label property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="propertyEnumerationType">The type of the enum, null if valueType isn't EnumeratedValue.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLabelPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
         BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType, Type propertyEnumerationType)
      {
         // For Instance
         IFCAnyHandle propHnd = CreateLabelPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType,
             propertyEnumerationType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateLabelPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType, propertyEnumerationType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIdentifierPropertyFromElement(IFCFile file, Element elem, string revitParameterName, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         if (elem == null)
            return null;

         (_, string propertyValue) = ParameterUtil.GetStringValueFromElement(elem, false, revitParameterName);
         return propertyValue != null ? CreateIdentifierPropertyFromCache(file, propertyDescription, propertyValue, valueType) : null;
      }

      /// <summary>
      /// Create an identifier property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIdentifierPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
         BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         // For Instance
         IFCAnyHandle propHnd = CreateIdentifierPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateIdentifierPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a date property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateDateProperty(IFCFile file, PropertyDescription propertyDescription, string value, PropertyValueType valueType)
      {
         IFCData dateData = IFCDataUtil.CreateAsDate(value);
         return CreateCommonProperty(file, propertyDescription, dateData, valueType, null);
      }

      /// <summary>
      /// Create a date property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateDatePropertyFromElement(IFCFile file, Element elem, string revitParameterName, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         if (elem == null)
            return null;

         (_, string propertyValue) = ParameterUtil.GetStringValueFromElement(elem, false, revitParameterName);
         return (propertyValue != null) ? CreateDateProperty(file, propertyDescription, propertyValue, valueType) : null;
      }

      /// <summary>
      /// Create a date property from the element's or type's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateDatePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
         BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateDatePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateDatePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a date-time property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateDateTimeProperty(IFCFile file, PropertyDescription propertyDescription, string value, PropertyValueType valueType)
      {
         IFCData dateTimeData = IFCDataUtil.CreateAsDateTime(value);
         return CreateCommonProperty(file, propertyDescription, dateTimeData, valueType, null);
      }

      /// <summary>
      /// Create a date-time property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateDateTimePropertyFromElement(IFCFile file, Element elem, string revitParameterName, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         if (elem == null)
            return null;

         (_, string propertyValue) = ParameterUtil.GetStringValueFromElement(elem, false, revitParameterName);
         return (propertyValue != null) ? CreateDateTimeProperty(file, propertyDescription, propertyValue, valueType) : null;
      }

      /// <summary>
      /// Create a date-time property from the element's or type's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateDateTimePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
         BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateDateTimePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateDateTimePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a URI reference property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateURIReferenceProperty(IFCFile file, PropertyDescription propertyDescription, string value, PropertyValueType valueType)
      {
         IFCData uriReferenceData = IFCDataUtil.CreateAsURIReference(value);
         return CreateCommonProperty(file, propertyDescription, uriReferenceData, valueType, null);
      }

      /// <summary>
      /// Create a URI reference property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateURIReferencePropertyFromElement(IFCFile file, Element elem, string revitParameterName, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         if (elem == null)
            return null;

         (_, string propertyValue) = ParameterUtil.GetStringValueFromElement(elem, false, revitParameterName);
         return propertyValue != null ? CreateURIReferenceProperty(file, propertyDescription, propertyValue, valueType) : null;
      }

      /// <summary>
      /// Create a URI reference property from the element's or type's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateURIReferencePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
         BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateURIReferencePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateURIReferencePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create a duration property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="value">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateDurationProperty(IFCFile file, PropertyDescription propertyDescription, string value, PropertyValueType valueType)
      {
         IFCData durationData = IFCDataUtil.CreateAsDuration(value);
         return CreateCommonProperty(file, propertyDescription, durationData, valueType, null);
      }

      /// <summary>
      /// Create a duration property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateDurationPropertyFromElement(IFCFile file, Element elem, string revitParameterName, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         if (elem == null)
            return null;

         (_, string propertyValue) = ParameterUtil.GetStringValueFromElement(elem, false, revitParameterName);
         return propertyValue != null ? CreateDurationProperty(file, propertyDescription, propertyValue, valueType) : null;
      }

      /// <summary>
      /// Create a duration property from the element's or type's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateDurationPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
         BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateDurationPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateDurationPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property.  Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateBooleanPropertyFromElement(IFCFile file, Element elem,
         string revitParameterName, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         (EvaluatedParameter parameter, int propertyValue) = ParameterUtil.GetIntValueFromElement(elem, revitParameterName);
         if (parameter != null)
            return CreateBooleanPropertyFromCache(file, propertyDescription, propertyValue != 0, valueType);
         (parameter, propertyValue) = ParameterUtil.GetIntValueFromElement(elem, propertyDescription.Name);
         if (parameter != null)
            return CreateBooleanPropertyFromCache(file, propertyDescription, propertyValue != 0, valueType);

         return null;
      }

      /// <summary>
      /// Create a logical property from the element's or type's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLogicalPropertyFromElement(IFCFile file, Element elem,
         string revitParameterName, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCLogical ifcLogical = IFCLogical.Unknown;
         (EvaluatedParameter parameter, int propertyValue) = ParameterUtil.GetIntValueFromElement(elem, revitParameterName);
         if (parameter != null)
         {
            ifcLogical = propertyValue != 0 ? IFCLogical.True : IFCLogical.False;
         }

         return CreateLogicalPropertyFromCache(file, propertyDescription, ifcLogical, valueType);
      }

      /// <summary>
      /// Create an integer property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIntegerPropertyFromElement(IFCFile file, Element elem,
         string revitParameterName, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         (EvaluatedParameter parameter, int propertyValue) = ParameterUtil.GetIntValueFromElement(elem, revitParameterName);
         if (parameter != null)
            return CreateIntegerPropertyFromCache(file, propertyDescription, propertyValue, valueType);

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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateCountMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         int propertyValue;
         if (ParameterUtil.TryGetDoubleValueFromElement(elem, revitParameterName) is double propertyValueReal)
         {
            if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4x3)
            {
               return CreateCountMeasureProperty(file, propertyDescription, propertyValueReal, valueType);
            }
            else if (MathUtil.IsAlmostInteger(propertyValueReal))
            {
               propertyValue = (int)Math.Floor(propertyValueReal);
               return CreateCountMeasureProperty(file, propertyDescription, propertyValue, valueType);
            }
         }

         (EvaluatedParameter parameter, propertyValue) = ParameterUtil.GetIntValueFromElement(elem, revitParameterName);
         if (parameter != null)
            return CreateCountMeasureProperty(file, propertyDescription, propertyValue, valueType);

         return null;
      }

      /// <summary>
      /// Create a count measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use, if revitParameterName isn't found.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateCountMeasurePropertyFromElement(IFCFile file, ExporterIFC exporterIFC, Element elem,
          string revitParameterName, BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateCountMeasurePropertyFromElement(file, exporterIFC, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateCountMeasurePropertyFromElement(file, exporterIFC, elem, builtInParamName, propertyDescription, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Gets the appropriate quantity name for pre-IFC4 versions based on the export version and quantity set.
      /// </summary>
      /// <param name="ifcVersion">The IFC version being exported to.</param>
      /// <param name="quantitySetName">The IFC quantity set name.</param>
      /// <param name="ifc4QuantityName">The IFC4 quantity name to map.</param>
      /// <returns>The mapped quantity name for the specified IFC version, or the original name if exporting to IFC4+.</returns>
      public static string GetPreIfc4QuantityNameIfNeeded(string quantitySetName, string ifc4QuantityName)
      {
         if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
            return ifc4QuantityName;

         IFCVersion mapVersion = IFCVersion.Default;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
            mapVersion = IFCVersion.IFC2x2;
         else if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3)
            mapVersion = IFCVersion.IFC2x3;
         else
            return null;

         if (!PreIFC4QuantityNamesMap.TryGetValue(mapVersion, out var versionMap))
            return null;

         var key = (quantitySetName, ifc4QuantityName);
         if (versionMap.TryGetValue(key, out var mappedName))
            return mappedName;

         return null;
      }

      /// <summary>
      /// Retrieves a double value from a Revit element's parameter for IFC quantity export.
      /// Uses custom parameter mappings if provided, otherwise defaults to "{quantitySetName}.{quantityName}" format.
      /// </summary>
      /// <param name="element">The Revit element to extract the parameter value from.</param>
      /// <param name="quantitySetName">The IFC quantity set name.</param>
      /// <param name="quantityName">The specific quantity name.</param>
      /// <param name="mappingInfo">Optional custom parameter mapping information.</param>
      /// <param name="quantityType">The quantity type for proper unit scaling.</param>
      /// <param name="value">The extracted and scaled parameter value.</param>
      /// <returns>True if a valid parameter value was found; false otherwise.</returns>
      public static bool GetQuantityDoubleValueFromMappedOrDefaultParameter(Element element, string quantitySetName,
         string quantityName, IFCPropertyMappingInfo mappingInfo, QuantityType quantityType, out double value)
      {
         value = 0.0;
         if (element == null)
            return false;

         string parameterName = (mappingInfo != null) ?
            mappingInfo.RevitPropertyName :
            string.Format("{0}.{1}", quantitySetName, quantityName);

         BuiltInParameter parameterId = (mappingInfo != null) ?
            (BuiltInParameter)mappingInfo.RevitPropertyId.Value :
            BuiltInParameter.INVALID;

         return GetQuantityDoubleValueFromParameter(element, parameterName, parameterId, quantityType, out value);
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
         if (!ExporterCacheManager.ExportIFCBaseQuantities() || (ExporterCacheManager.ExportOptionsCache.ExportAsCOBIE))
            return;

         string quantitySetName = string.Empty;
         if (IFCAnyHandleUtil.IsSubTypeOf(elemHandle, IFCEntityType.IfcColumn))
            quantitySetName = "Qto_ColumnBaseQuantities";
         else if (IFCAnyHandleUtil.IsSubTypeOf(elemHandle, IFCEntityType.IfcBeam))
            quantitySetName = "Qto_BeamBaseQuantities";
         else if (IFCAnyHandleUtil.IsSubTypeOf(elemHandle, IFCEntityType.IfcMember))
            quantitySetName = "Qto_MemberBaseQuantities";

         string revitPropertyName = string.Empty;

         PropertySetupType propertySetup = PropertySetupType.IfcBaseQuantities;
         if (IsPropertySetExcluded(propertySetup, quantitySetName))
            return;

         IFCFile file = exporterIFC.GetFile();
         HashSet<IFCAnyHandle> quantityHnds = new();
         double scaledLength = typeInfo.extraParams.ScaledLength;
         //According to investigation of current code the passed in typeInfo contains grossArea
         double scaledGrossArea = typeInfo.extraParams.ScaledArea;
         double crossSectionArea = scaledGrossArea;
         double scaledOuterPerimeter = typeInfo.extraParams.ScaledOuterPerimeter;
         double scaledInnerPerimeter = typeInfo.extraParams.ScaledInnerPerimeter;
         double outSurfaceArea = 0.0;
         double dblVal = 0.0;

         // Length     
         string quantityName = GetPreIfc4QuantityNameIfNeeded(quantitySetName, "Length");
         IFCPropertyMappingInfo info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
         if (!string.IsNullOrEmpty(quantityName) && (info?.ExportFlag ?? true))
         {
            bool valueFound = GetQuantityDoubleValueFromMappedOrDefaultParameter(element, quantitySetName, quantityName,
               info, QuantityType.Length, out dblVal);

            if (!valueFound)
            {
               if (scaledLength > MathUtil.Eps)
               {
                  dblVal = scaledLength;
                  valueFound = true;
               }
            }

            if (valueFound)
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, quantityName, null, null, dblVal);
               quantityHnds.Add(quantityHnd);
            }
         }

         // CrossSectionArea         
         quantityName = GetPreIfc4QuantityNameIfNeeded(quantitySetName, "CrossSectionArea");
         info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
         if (!string.IsNullOrEmpty(quantityName) && (info?.ExportFlag ?? true))
         {
            bool valueFound = GetQuantityDoubleValueFromMappedOrDefaultParameter(element, quantitySetName, quantityName,
               info, QuantityType.Area, out dblVal);

            if (!valueFound)
            {
               if (MathUtil.AreaIsAlmostZero(crossSectionArea) && element != null)
               {
                  (_, crossSectionArea) = ParameterUtil.GetDoubleValueFromElement(element.Id, BuiltInParameter.HOST_AREA_COMPUTED);
                  crossSectionArea = UnitUtil.ScaleArea(crossSectionArea);
               }

               if (!MathUtil.AreaIsAlmostZero(crossSectionArea))
               {
                  dblVal = crossSectionArea;
                  valueFound = true;
               }
            }

            if (valueFound)
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, quantityName, null, null, dblVal);
               quantityHnds.Add(quantityHnd);
            }
         }

         // OuterSurfaceArea
         quantityName = GetPreIfc4QuantityNameIfNeeded(quantitySetName, "OuterSurfaceArea");
         info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
         if (!string.IsNullOrEmpty(quantityName) && (info?.ExportFlag ?? true))
         {
            bool valueFound = GetQuantityDoubleValueFromMappedOrDefaultParameter(element, quantitySetName, quantityName,
               info, QuantityType.Area, out dblVal);

            if (!valueFound)
            {
               if (!MathUtil.AreaIsAlmostZero(scaledGrossArea) && !MathUtil.IsAlmostZero(scaledLength) && !MathUtil.IsAlmostZero(scaledOuterPerimeter))
               {
                  double scaledPerimeter = scaledOuterPerimeter + scaledInnerPerimeter;
                  //According to the IFC documentation, OuterSurfaceArea does not include the end caps area, only Length * Perimeter
                  dblVal = UnitUtil.ScaleArea(UnitUtil.UnscaleLength(scaledLength) * UnitUtil.UnscaleLength(scaledPerimeter));
                  valueFound = true;
               }
            }

            if (valueFound)
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, quantityName, null, null, dblVal);
               quantityHnds.Add(quantityHnd);
            }
         }

         // GrossSurfaceArea
         quantityName = GetPreIfc4QuantityNameIfNeeded(quantitySetName, "GrossSurfaceArea");
         info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
         if (!string.IsNullOrEmpty(quantityName) && (info?.ExportFlag ?? true))
         {
            bool valueFound = GetQuantityDoubleValueFromMappedOrDefaultParameter(element, quantitySetName, quantityName,
               info, QuantityType.Area, out dblVal);

            if (!valueFound)
            {
               if (MathUtil.AreaIsAlmostZero(crossSectionArea) && MathUtil.AreaIsAlmostZero(outSurfaceArea))
               {
                  double scaledPerimeter = scaledOuterPerimeter + scaledInnerPerimeter;
                  dblVal = scaledGrossArea * 2 + UnitUtil.ScaleArea(UnitUtil.UnscaleLength(scaledLength) * UnitUtil.UnscaleLength(scaledPerimeter));
                  valueFound = true;
               }
            }

            if (valueFound)
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, quantityName, null, null, dblVal);
               quantityHnds.Add(quantityHnd);
            }
         }

         // GrossVolume
         quantityName = "GrossVolume";
         info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
         if (info?.ExportFlag ?? true)
         {
            bool valueFound = GetQuantityDoubleValueFromMappedOrDefaultParameter(element, quantitySetName, quantityName,
               info, QuantityType.Volume, out dblVal);

            if (!valueFound)
            {
               double grossVolume = UnitUtil.ScaleVolume(UnitUtil.UnscaleLength(scaledLength) * UnitUtil.UnscaleArea(scaledGrossArea));
               if (!MathUtil.VolumeIsAlmostZero(grossVolume))
               {
                  dblVal = grossVolume;
                  valueFound = true;
               }
            }

            if (valueFound)
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityVolume(file, quantityName, null, null, dblVal);
               quantityHnds.Add(quantityHnd);
            }
         }

         // NetVolume
         quantityName = GetPreIfc4QuantityNameIfNeeded(quantitySetName, "NetVolume");
         info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
         if (!string.IsNullOrEmpty(quantityName) && (info?.ExportFlag ?? true))
         {
            bool valueFound = GetQuantityDoubleValueFromMappedOrDefaultParameter(element, quantitySetName, quantityName,
               info, QuantityType.Volume, out dblVal);

            if (!valueFound)
            {
               double netVolume = 0.0;
               if (element != null)
               {
                  // If we are splitting columns, we will look at the actual geometry used when exporting this segment
                  // of the column, but only if we have the geomObjects passed in.
                  if (geomObjects != null && (ExporterCacheManager.ExportOptionsCache.WallAndColumnSplitting ||
                                               GeometryUtil.HasSteelGeometry(element))) //allways compute volume from geometry Object
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
                  {
                     (_, netVolume) = ParameterUtil.GetDoubleValueFromElement(element.Id, BuiltInParameter.HOST_VOLUME_COMPUTED);
                  }
                  netVolume = UnitUtil.ScaleVolume(netVolume);

                  if (!MathUtil.VolumeIsAlmostZero(netVolume))
                  {
                     dblVal = netVolume;
                     valueFound = true;
                  }
               }
            }

            if (valueFound)
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityVolume(file, quantityName, null, null, dblVal);
               quantityHnds.Add(quantityHnd);
            }
         }

         string quantitySetNameToUse = ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 ? null : quantitySetName;
         CreateAndRelateBaseQuantities(file, exporterIFC, elemHandle, quantityHnds, quantitySetNameToUse);
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
         CreateOpeningQuantities(exporterIFC, openingElement,
            extraParams.ScaledLength, extraParams.ScaledHeight, extraParams.ScaledWidth, extraParams.ScaledArea);
      }

      /// <summary>
      /// Creates the opening quantities and adds them to the export.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="openingElement">The opening element handle.</param>
      public static void CreateOpeningQuantities(ExporterIFC exporterIFC, IFCAnyHandle openingElement,
         double depth, double height, double width, double area)
      {
         string quantitySetName = "Qto_OpeningElementBaseQuantities";
         PropertySetupType propertySetup = PropertySetupType.IfcBaseQuantities;
         if (IsPropertySetExcluded(propertySetup, quantitySetName))
            return;

         IFCFile file = exporterIFC.GetFile();
         HashSet<IFCAnyHandle> quantityHnds = new();

         // Depth
         string quantityName = GetPreIfc4QuantityNameIfNeeded(quantitySetName, "Depth");
         IFCPropertyMappingInfo info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
         if (!string.IsNullOrEmpty(quantityName) && (info?.ExportFlag ?? true))
         {
            if (depth > MathUtil.Eps)
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, quantityName, null, null, depth);
               quantityHnds.Add(quantityHnd);
            }
         }

         if (height > MathUtil.Eps)
         {
            // Height
            quantityName = GetPreIfc4QuantityNameIfNeeded(quantitySetName, "Height");
            info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
            if (!string.IsNullOrEmpty(quantityName) && (info?.ExportFlag ?? true))
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, quantityName, null, null, height);
               quantityHnds.Add(quantityHnd);
            }

            // Width
            quantityName = GetPreIfc4QuantityNameIfNeeded(quantitySetName, "Width");
            info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
            if (!string.IsNullOrEmpty(quantityName) && (info?.ExportFlag ?? true))
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, quantityName, null, null, width);
               quantityHnds.Add(quantityHnd);
            }
         }

         // Area
         bool exportArea = true;
         if (area < MathUtil.Eps)
         {
            if (height > MathUtil.Eps && width > MathUtil.Eps)
               area = height * width;
            else
               exportArea = false;
         }

         if (exportArea)
         {
            quantityName = GetPreIfc4QuantityNameIfNeeded(quantitySetName, "Area");
            info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
            if (!string.IsNullOrEmpty(quantityName) && (info?.ExportFlag ?? true))
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, quantityName, null, null, area);
               quantityHnds.Add(quantityHnd);
            }
         }

         // Volume
         double volume = area * depth;
         if (volume > MathUtil.Eps)
         {
            quantityName = GetPreIfc4QuantityNameIfNeeded(quantitySetName, "Volume");
            info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
            if (!string.IsNullOrEmpty(quantityName) && (info?.ExportFlag ?? true))
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityVolume(file, quantityName, null, null, volume);
               quantityHnds.Add(quantityHnd);
            }
         }

         string quantitySetNameToUse = ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 ? null : quantitySetName;
         CreateAndRelateBaseQuantities(file, exporterIFC, openingElement, quantityHnds, quantitySetNameToUse);
      }

      /// <summary>
      /// Creates and exports the IFC base quantities for a slab handle.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="slabHnd">The slab handle.</param>
      /// <param name="element">The Revit element backing the slab handle (the landing for stair
      /// landings, the parent host for roof slab subcomponents). Used to source values from
      /// mapped Revit parameters before falling back to geometric computation. May be null when
      /// no element context is available.</param>
      /// <param name="extrusionData">The IFCExportBodyParams containing the slab extrusion creation data.</param>
      /// <param name="outerCurveLoop">The slab outer loop, used to compute gross/perimeter quantities. May be null.</param>
      public static void CreateSlabBaseQuantities(ExporterIFC exporterIFC, IFCAnyHandle slabHnd,
         Element element, IFCExportBodyParams extrusionData, CurveLoop outerCurveLoop)
      {
         if (extrusionData == null)
            return;

         const string quantitySetName = "Qto_SlabBaseQuantities";
         PropertySetupType propertySetup = PropertySetupType.IfcBaseQuantities;
         if (IsPropertySetExcluded(propertySetup, quantitySetName))
            return;

         IFCFile file = exporterIFC.GetFile();
         HashSet<IFCAnyHandle> quantityHnds = new();

         // Compute these once. Length/area/volume can have different base length units, so we
         // round-trip through unscaled values when combining them into derived volumes.
         double scaledWidth = extrusionData.ScaledLength;
         double scaledNetArea = extrusionData.ScaledArea;
         double unscaledWidth = UnitUtil.UnscaleLength(scaledWidth);
         double unscaledNetArea = UnitUtil.UnscaleArea(scaledNetArea);

         // ScaledWidth is the wider dimension of rectangular extrusion profiles, computed by
         // ComputeHeightWidthOfCurveLoop. It will be non-zero only for rectangular slabs.
         double scaledLength = extrusionData.ScaledWidth;

         double unscaledGrossArea = 0.0;
         double scaledGrossArea = 0.0;
         if (outerCurveLoop != null)
         {
            unscaledGrossArea = ExporterIFCUtils.ComputeAreaOfCurveLoops([outerCurveLoop]);
            scaledGrossArea = UnitUtil.ScaleArea(unscaledGrossArea);
         }

         double scaledPerimeter = (outerCurveLoop != null)
            ? UnitUtil.ScaleLength(outerCurveLoop.GetExactLength())
            : extrusionData.ScaledOuterPerimeter;

         // Width — slab thickness (extrusion length for horizontal slabs).
         AddSlabLengthQuantity(file, element, propertySetup, quantitySetName, "Width",
            "IfcQtyWidth",
            scaledWidth > MathUtil.Eps ? scaledWidth : (double?)null,
            allowFallbackName: true, quantityHnds);

         // Length — for rectangular slabs, use the extrusion's ScaledWidth (the major edge of the
         // rectangular profile, matching the original LengthCalculator behavior where width/length
         // are swapped for slabs). For non-rectangular slabs, ScaledWidth is zero so only the mapped
         // Revit parameter is used. Skipped for IFC versions that don't define it (IFC2x2 and IFC2x3
         // Qto_SlabBaseQuantities don't include Length).
         AddSlabLengthQuantity(file, element, propertySetup, quantitySetName, "Length",
            "IfcQtyLength",
            scaledLength > MathUtil.Eps ? scaledLength : (double?)null,
            allowFallbackName: false, quantityHnds);

         // Depth — for slabs the calculator returns the extrusion length (slab thickness), so we
         // mirror that as the geometric fallback. Skipped for IFC versions that don't define it.
         AddSlabLengthQuantity(file, element, propertySetup, quantitySetName, "Depth",
            "IfcQtyDepth",
            scaledWidth > MathUtil.Eps ? scaledWidth : (double?)null,
            allowFallbackName: false, quantityHnds);

         // Perimeter — outer curve loop length or extrusion's outer perimeter.
         AddSlabLengthQuantity(file, element, propertySetup, quantitySetName, "Perimeter",
            "IfcQtyPerimeter",
            scaledPerimeter > MathUtil.Eps ? scaledPerimeter : (double?)null,
            allowFallbackName: true, quantityHnds);

         // NetArea — extrusion cross-section area (footprint area for horizontal slabs).
         AddSlabAreaQuantity(file, element, propertySetup, quantitySetName, "NetArea",
            "IfcQtyNetArea",
            scaledNetArea > MathUtil.Eps ? scaledNetArea : (double?)null,
            allowFallbackName: true, quantityHnds);

         // GrossArea — outer curve loop area when supplied; otherwise fall back to the extrusion
         // cross-section area. This matches GrossAreaCalculator for stair-landing slabs that have
         // no curve loop available.
         double? grossAreaFallback = null;
         if (outerCurveLoop != null && scaledGrossArea > MathUtil.Eps)
            grossAreaFallback = scaledGrossArea;
         else if (scaledNetArea > MathUtil.Eps)
            grossAreaFallback = scaledNetArea;
         AddSlabAreaQuantity(file, element, propertySetup, quantitySetName, "GrossArea",
            "IfcQtyGrossArea",
            grossAreaFallback, allowFallbackName: true, quantityHnds);

         // NetVolume — net area * thickness, computed in unscaled units to keep mixed-unit projects consistent.
         double? netVolumeFallback = null;
         {
            double scaledNetVolume = UnitUtil.ScaleVolume(unscaledNetArea * unscaledWidth);
            if (scaledNetVolume > MathUtil.Eps)
               netVolumeFallback = scaledNetVolume;
         }
         AddSlabVolumeQuantity(file, element, propertySetup, quantitySetName, "NetVolume",
            "IfcQtyNetVolume",
            netVolumeFallback, allowFallbackName: true, quantityHnds);

         // GrossVolume — gross (boundary) area * thickness. When no outer curve loop is available
         // or the computed volume is degenerate (near-zero), fall back to NetVolume.
         // This mirrors how GrossArea falls back to NetArea.
         double? grossVolumeFallback = null;
         if (outerCurveLoop != null)
         {
            double scaledGrossVolume = UnitUtil.ScaleVolume(unscaledGrossArea * unscaledWidth);
            if (scaledGrossVolume > MathUtil.Eps)
               grossVolumeFallback = scaledGrossVolume;
            else if (netVolumeFallback.HasValue)
               grossVolumeFallback = netVolumeFallback;
         }
         else if (netVolumeFallback.HasValue)
         {
            grossVolumeFallback = netVolumeFallback;
         }
         AddSlabVolumeQuantity(file, element, propertySetup, quantitySetName, "GrossVolume",
            "IfcQtyGrossVolume",
            grossVolumeFallback, allowFallbackName: true, quantityHnds);

         // GrossWeight, NetWeight — mass quantities. No geometric source (Revit cannot generically
         // derive mass for a slab), but IfcQtyGrossWeight / IfcQtyNetWeight may be stamped on imported
         // elements during round-trip, so we honour those alongside the standard mapped parameter.
         AddSlabWeightQuantity(file, element, propertySetup, quantitySetName, "GrossWeight",
            "IfcQtyGrossWeight", quantityHnds);
         AddSlabWeightQuantity(file, element, propertySetup, quantitySetName, "NetWeight",
            "IfcQtyNetWeight", quantityHnds);

         // Pre-IFC4: pass null so CreateAndRelateBaseQuantities uses the generic "BaseQuantities"
         // name. Older IFC versions do not support named quantity sets on IfcElementQuantity.
         string quantitySetNameToUse = ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 ? null : quantitySetName;
         CreateAndRelateBaseQuantities(file, exporterIFC, slabHnd, quantityHnds, quantitySetNameToUse);
      }

      /// <summary>
      /// Resolves the IFC name to use for a slab quantity in the active IFC version.
      /// </summary>
      /// <param name="quantitySetName">The IFC4-style quantity set name (e.g. "Qto_SlabBaseQuantities").</param>
      /// <param name="ifc4QuantityName">The IFC4-style quantity name (e.g. "Width").</param>
      /// <param name="allowFallbackName">When true, if no version mapping exists the IFC4 name is used
      /// as-is (the behaviour introduced for entries that are emitted in older IFC
      /// versions even without a mapping). When false, returns null so the caller skips the entry —
      /// used for quantities that don't exist in older IFC standards (Length, Depth).</param>
      /// <returns>The quantity name to use, or null when the entry should be skipped.</returns>
      private static string ResolveSlabQuantityName(string quantitySetName, string ifc4QuantityName, bool allowFallbackName)
      {
         string mapped = GetPreIfc4QuantityNameIfNeeded(quantitySetName, ifc4QuantityName);
         if (!string.IsNullOrEmpty(mapped))
            return mapped;
         return allowFallbackName ? ifc4QuantityName : null;
      }

      /// <summary>
      /// Reads a slab quantity value, in IFC-scaled units, from the mapped Revit parameter and
      /// then from the conventional <c>IfcQty*</c> ad-hoc parameter written on imported
      /// elements. Mirrors the parameter-resolution chain used by the standalone calculators
      /// (e.g. <c>DepthCalculator</c>, <c>GrossWeightCalculator</c>).
      /// </summary>
      /// <returns>True when a value was retrieved.</returns>
      private static bool TryGetSlabQuantityValue(Element element, IFCPropertyMappingInfo info,
         string quantitySetName, string quantityName, QuantityType quantityType,
         string ifcQtyAltName, out double scaledValue)
      {
         scaledValue = 0.0;
         if (element == null)
            return false;

         string primaryName = !string.IsNullOrEmpty(info?.RevitPropertyName)
            ? info.RevitPropertyName
            : string.Format("{0}.{1}", quantitySetName, quantityName);

         BuiltInParameter primaryBuiltIn = !MathUtil.IsInvalidElementId(info?.RevitPropertyId)
            ? (BuiltInParameter)info.RevitPropertyId.Value
            : BuiltInParameter.INVALID;

         (EvaluatedParameter parameter, double rawValue) = ParameterUtil.GetDoubleValueFromElementOrSymbol(element, primaryName, ifcQtyAltName);
         
         if (parameter == null && primaryBuiltIn != BuiltInParameter.INVALID)
            (parameter, rawValue) = ParameterUtil.GetDoubleValueFromElementOrSymbol(element, primaryBuiltIn);

         if (parameter == null)
            return false;

         switch (quantityType)
         {
            case QuantityType.Length:
            case QuantityType.PositiveLength:
               rawValue = UnitUtil.ScaleLength(rawValue);
               break;
            case QuantityType.Area:
               rawValue = UnitUtil.ScaleArea(rawValue);
               break;
            case QuantityType.Volume:
               rawValue = UnitUtil.ScaleVolume(rawValue);
               break;
            case QuantityType.Weight:
            case QuantityType.Mass:
               rawValue = UnitUtil.ScaleMass(rawValue);
               break;
         }

         scaledValue = rawValue;
         return true;
      }

      private static void AddSlabLengthQuantity(IFCFile file, Element element, PropertySetupType propertySetup,
         string quantitySetName, string ifc4QuantityName, string ifcQtyAltName, double? computedFallback,
         bool allowFallbackName, ICollection<IFCAnyHandle> quantityHnds)
      {
         string quantityName = ResolveSlabQuantityName(quantitySetName, ifc4QuantityName, allowFallbackName);
         if (string.IsNullOrEmpty(quantityName))
            return;

         IFCPropertyMappingInfo info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
         if (!(info?.ExportFlag ?? true))
            return;

         bool valueFound = TryGetSlabQuantityValue(element, info, quantitySetName, quantityName,
            QuantityType.Length, ifcQtyAltName, out double dblVal);

         if (!valueFound && computedFallback.HasValue)
         {
            dblVal = computedFallback.Value;
            valueFound = true;
         }

         if (valueFound)
            quantityHnds.Add(IFCInstanceExporter.CreateQuantityLength(file, quantityName, null, null, dblVal));
      }

      private static void AddSlabAreaQuantity(IFCFile file, Element element, PropertySetupType propertySetup,
         string quantitySetName, string ifc4QuantityName, string ifcQtyAltName, double? computedFallback,
         bool allowFallbackName, ICollection<IFCAnyHandle> quantityHnds)
      {
         string quantityName = ResolveSlabQuantityName(quantitySetName, ifc4QuantityName, allowFallbackName);
         if (string.IsNullOrEmpty(quantityName))
            return;

         IFCPropertyMappingInfo info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
         if (!(info?.ExportFlag ?? true))
            return;

         bool valueFound = TryGetSlabQuantityValue(element, info, quantitySetName, quantityName,
            QuantityType.Area, ifcQtyAltName, out double dblVal);

         if (!valueFound && computedFallback.HasValue)
         {
            dblVal = computedFallback.Value;
            valueFound = true;
         }

         if (valueFound)
            quantityHnds.Add(IFCInstanceExporter.CreateQuantityArea(file, quantityName, null, null, dblVal));
      }

      private static void AddSlabVolumeQuantity(IFCFile file, Element element, PropertySetupType propertySetup,
         string quantitySetName, string ifc4QuantityName, string ifcQtyAltName, double? computedFallback,
         bool allowFallbackName, ICollection<IFCAnyHandle> quantityHnds)
      {
         string quantityName = ResolveSlabQuantityName(quantitySetName, ifc4QuantityName, allowFallbackName);
         if (string.IsNullOrEmpty(quantityName))
            return;

         IFCPropertyMappingInfo info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
         if (!(info?.ExportFlag ?? true))
            return;

         bool valueFound = TryGetSlabQuantityValue(element, info, quantitySetName, quantityName,
            QuantityType.Volume, ifcQtyAltName, out double dblVal);

         if (!valueFound && computedFallback.HasValue)
         {
            dblVal = computedFallback.Value;
            valueFound = true;
         }

         if (valueFound)
            quantityHnds.Add(IFCInstanceExporter.CreateQuantityVolume(file, quantityName, null, null, dblVal));
      }

      private static void AddSlabWeightQuantity(IFCFile file, Element element, PropertySetupType propertySetup,
         string quantitySetName, string ifc4QuantityName, string ifcQtyAltName, ICollection<IFCAnyHandle> quantityHnds)
      {
         string quantityName = ResolveSlabQuantityName(quantitySetName, ifc4QuantityName, allowFallbackName: true);
         if (string.IsNullOrEmpty(quantityName))
            return;

         IFCPropertyMappingInfo info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
         if (!(info?.ExportFlag ?? true))
            return;

         if (TryGetSlabQuantityValue(element, info, quantitySetName, quantityName,
                QuantityType.Mass, ifcQtyAltName, out double dblVal))
         {
            quantityHnds.Add(IFCInstanceExporter.CreateQuantityWeight(file, quantityName, null, null, dblVal));
         }
      }

      /// <summary>
      /// Determines whether the given parameter requires scaling.
      /// </summary>
      /// <param name="param">The parameter to check.</param>
      /// <returns>True if the parameter requires scaling; otherwise, false.</returns>
      public static bool IsParameterScalingRequired(EvaluatedParameter param)
      {
         // Exclude parameters that are of type SpecTypeId.Number.
         if (ParameterUtil.ParameterDataTypeIsEqualTo(param, SpecTypeId.Number))
            return false;

         // The built-in "TotalWattage" parameter is stored as a string in Revit, likely in the current units, and does not require additional scaling.
         if (param.Definition.Id.Value == (long)BuiltInParameter.LIGHTING_FIXTURE_WATTAGE)
            return false;

         return true;
      }

      /// <summary>
      /// Checks whether the property set is excluded from export.
      /// </summary>
      /// <param name="propertySetupType">The property setup type.</param>
      /// <param name="psetName">The property set name.</param>
      /// <returns>True is the property set is exists in mapping template and unchecked.</returns>
      public static bool IsPropertySetExcluded(PropertySetupType propertySetupType, string psetName)
      {
         if (string.IsNullOrEmpty(psetName))
            return false;

         IFCParameterTemplate parameterTemplate = ExporterCacheManager.ParameterMappingTemplate;
         if (parameterTemplate == null)
            return false;

         return parameterTemplate.IsPropertySetAMemberOfTemplate(propertySetupType, psetName) &&
            !parameterTemplate.IsExportingPropertySet(propertySetupType, psetName);
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
         string quantitySetName = "Qto_WallBaseQuantities";
         PropertySetupType propertySetup = PropertySetupType.IfcBaseQuantities;
         if (IsPropertySetExcluded(propertySetup, quantitySetName))
            return;

         double dblVal = 0.0;
         IFCFile file = exporterIFC.GetFile();
         HashSet<IFCAnyHandle> quantityHnds = new();

         // Height
         string quantityName = GetPreIfc4QuantityNameIfNeeded(quantitySetName, "Height");
         IFCPropertyMappingInfo info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
         if (!string.IsNullOrEmpty(quantityName) && (info?.ExportFlag ?? true))
         {
            bool valueFound = GetQuantityDoubleValueFromMappedOrDefaultParameter(wallElement, quantitySetName, quantityName,
               info, QuantityType.Length, out dblVal);

            if (!valueFound)
            {
               if (scaledDepth > MathUtil.Eps)
               {
                  dblVal = scaledDepth;
                  valueFound = true;
               }
            }

            if (valueFound)
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, quantityName, null, null, dblVal);
               quantityHnds.Add(quantityHnd);
            }
         }

         // Length
         quantityName = GetPreIfc4QuantityNameIfNeeded(quantitySetName, "Length");
         info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
         if (!string.IsNullOrEmpty(quantityName) && (info?.ExportFlag ?? true))
         {
            bool valueFound = GetQuantityDoubleValueFromMappedOrDefaultParameter(wallElement, quantitySetName, quantityName,
               info, QuantityType.Length, out dblVal);

            if (!valueFound)
            {
               if (!MathUtil.IsAlmostZero(scaledLength))
               {
                  dblVal = scaledLength;
                  valueFound = true;
               }
               else if (wallElement.Location != null)
               {
                  Curve wallAxis = (wallElement.Location as LocationCurve).Curve;
                  if (wallAxis != null)
                  {
                     dblVal = UnitUtil.ScaleLength(wallAxis.Length);
                     valueFound = true;
                  }
               }
            }

            if (valueFound)
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, quantityName, null, null, dblVal);
               quantityHnds.Add(quantityHnd);
            }
         }

         // Width
         quantityName = GetPreIfc4QuantityNameIfNeeded(quantitySetName, "Width");
         info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
         if (!string.IsNullOrEmpty(quantityName) && (info?.ExportFlag ?? true))
         {
            bool valueFound = GetQuantityDoubleValueFromMappedOrDefaultParameter(wallElement, quantitySetName, quantityName,
               info, QuantityType.Length, out dblVal);

            if (!valueFound)
            {
               if (wallElement != null)
               {
                  double width = UnitUtil.ScaleLength(wallElement.Width);
                  if (!MathUtil.IsAlmostZero(width))
                  {
                     if ((widthAsComplexQty?.Count ?? 0) == 0)
                     {
                        dblVal = width;
                        valueFound = true;
                     }
                     else
                     {
                        quantityHnds.UnionWith(widthAsComplexQty);
                     }
                  }
               }
            }

            if (valueFound)
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityLength(file, quantityName, null, null, dblVal);
               quantityHnds.Add(quantityHnd);
            }
         }

         // GrossFootprintArea
         quantityName = GetPreIfc4QuantityNameIfNeeded(quantitySetName, "GrossFootprintArea");
         info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
         if (!string.IsNullOrEmpty(quantityName) && (info?.ExportFlag ?? true))
         {
            bool valueFound = GetQuantityDoubleValueFromMappedOrDefaultParameter(wallElement, quantitySetName, quantityName,
               info, QuantityType.Area, out dblVal);

            if (!valueFound)
            {
               if (!MathUtil.IsAlmostZero(scaledFootPrintArea))
               {
                  dblVal = scaledFootPrintArea;
                  valueFound = true;
               }
            }

            if (valueFound)
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, quantityName, null, null, dblVal);
               quantityHnds.Add(quantityHnd);
            }
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
                              wallSides[wallSide.Key] = (sideFaces, sumArea);
                              faceAdded = true;
                              break;
                           }
                        }
                        if (!faceAdded)
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

            // Compute gross area from wall length and height
            // (scaledLength and scaledDepth parameters, both already in IFC units).
            // When those are unavailable, fall back to summing the outer boundary
            // loop area of each face on the selected side, which is still correct
            // for walls whose openings don't reach the wall edges.
            double unscaledLength = UnitUtil.UnscaleLength(scaledLength);
            double unscaledDepth = UnitUtil.UnscaleLength(scaledDepth);
            if (!MathUtil.IsAlmostZero(unscaledLength) && !MathUtil.IsAlmostZero(unscaledDepth))
            {
               grossArea = unscaledLength * unscaledDepth;
            }
            else
            {
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
         }

         netArea = UnitUtil.ScaleArea(netArea);
         grossArea = UnitUtil.ScaleArea(grossArea);
         volume = UnitUtil.ScaleVolume(volume);

         double scaledWidth = (wallElement != null) ? UnitUtil.ScaleLength(wallElement.Width) : 0.0;

         // GrossVolume
         quantityName = GetPreIfc4QuantityNameIfNeeded(quantitySetName, "GrossVolume");
         info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
         if (!string.IsNullOrEmpty(quantityName) && (info?.ExportFlag ?? true))
         {
            bool valueFound = GetQuantityDoubleValueFromMappedOrDefaultParameter(wallElement, quantitySetName, quantityName,
               info, QuantityType.Volume, out dblVal);

            if (!valueFound)
            {
               if (scaledDepth > MathUtil.Eps && !MathUtil.IsAlmostZero(scaledWidth) && !MathUtil.IsAlmostZero(grossArea))
               {
                  dblVal = UnitUtil.ScaleVolume(UnitUtil.UnscaleLength(scaledWidth) * UnitUtil.UnscaleArea(grossArea));
                  valueFound = true;
               }
            }

            if (valueFound)
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityVolume(file, quantityName, null, null, dblVal);
               quantityHnds.Add(quantityHnd);
            }
         }

         // GrossSideArea
         quantityName = GetPreIfc4QuantityNameIfNeeded(quantitySetName, "GrossSideArea");
         info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
         if (!string.IsNullOrEmpty(quantityName) && (info?.ExportFlag ?? true))
         {
            bool valueFound = GetQuantityDoubleValueFromMappedOrDefaultParameter(wallElement, quantitySetName, quantityName,
               info, QuantityType.Area, out dblVal);

            if (!valueFound)
            {
               if (!MathUtil.IsAlmostZero(grossArea))
               {
                  dblVal = grossArea;
                  valueFound = true;
               }
            }

            if (valueFound)
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, quantityName, null, null, dblVal);
               quantityHnds.Add(quantityHnd);
            }
         }

         // NetSideArea
         quantityName = GetPreIfc4QuantityNameIfNeeded(quantitySetName, "NetSideArea");
         info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
         if (!string.IsNullOrEmpty(quantityName) && (info?.ExportFlag ?? true))
         {
            bool valueFound = GetQuantityDoubleValueFromMappedOrDefaultParameter(wallElement, quantitySetName, quantityName,
               info, QuantityType.Area, out dblVal);

            if (!valueFound)
            {
               if (!MathUtil.IsAlmostZero(netArea))
               {
                  dblVal = netArea;
                  valueFound = true;
               }
            }

            if (valueFound)
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityArea(file, quantityName, null, null, dblVal);
               quantityHnds.Add(quantityHnd);
            }
         }

         // NetVolume
         quantityName = GetPreIfc4QuantityNameIfNeeded(quantitySetName, "NetVolume");
         info = GetParameterMappingInfoFromCache(propertySetup, quantitySetName, ElementId.InvalidElementId, quantityName);
         if (!string.IsNullOrEmpty(quantityName) && (info?.ExportFlag ?? true))
         {
            bool valueFound = GetQuantityDoubleValueFromMappedOrDefaultParameter(wallElement, quantitySetName, quantityName,
               info, QuantityType.Volume, out dblVal);

            if (!valueFound)
            {
               if (!MathUtil.IsAlmostZero(volume))
               {
                  dblVal = volume;
                  valueFound = true;
               }
            }

            if (valueFound)
            {
               IFCAnyHandle quantityHnd = IFCInstanceExporter.CreateQuantityVolume(file, quantityName, null, null, dblVal);
               quantityHnds.Add(quantityHnd);
            }
         }

         string quantitySetNameToUse = ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 ? null : quantitySetName;
         CreateAndRelateBaseQuantities(file, exporterIFC, wallHnd, quantityHnds, quantitySetNameToUse);
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
         return (IFCAnyHandleUtil.IsSubTypeOf(elemHandle, IFCEntityType.IfcSlab) ||
            IFCAnyHandleUtil.IsSubTypeOf(elemHandle, IFCEntityType.IfcCovering) ||
            IFCAnyHandleUtil.IsSubTypeOf(elemHandle, IFCEntityType.IfcFooting));
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

         SortedDictionary<string, (string, HashSet<IFCAnyHandle>)>[] propertySets =
         {
            [],
            []
         };

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

            bool createType = (which == 1);
            if (createType)
            {
               if (ExporterCacheManager.TypePropertyInfoCache.HasTypeProperties(typeId))
                  continue;
            }

            propertySets[which] = CreateGroupedInternalProperties(file, whichElement);
         }

         for (int which = whichStart; which < 2; which++)
         {
            Element whichElement = (which == 0) ? element : elementType;
            if (whichElement == null)
               continue;

            if (propertySets[which].Count == 0)
            {
               ExporterCacheManager.TypePropertyInfoCache.AddNewElementHandles(typeId, elementSets);
               continue;
            }

            bool materialProperties = element is Material;

            if (which == 1)
            {
               // Type path: cache ingredients (pset name + individual property handles)
               // rather than finished IfcPropertySet handles. Each consuming IfcTypeObject
               // will create its own IfcPropertySet wrappers from these ingredients.
               if (!materialProperties)
               {
                  var propertyInputs = new List<(string, HashSet<IFCAnyHandle>)>();
                  foreach (KeyValuePair<string, (string, HashSet<IFCAnyHandle>)> currPropertySet in propertySets[which])
                  {
                     if (currPropertySet.Value.Item2.Count == 0)
                        continue;
                     propertyInputs.Add((currPropertySet.Value.Item1, new HashSet<IFCAnyHandle>(currPropertySet.Value.Item2)));
                  }

                  if (propertyInputs.Count > 0)
                     ExporterCacheManager.TypePropertyInfoCache.AddNewTypeProperties(typeId, propertyInputs, elementSets);
                  else
                     ExporterCacheManager.TypePropertyInfoCache.AddNewElementHandles(typeId, elementSets);
               }
               continue;
            }

            // Instance path (which == 0): create IfcPropertySet handles as before.
            HashSet<IFCAnyHandle> createdPropertySets = new HashSet<IFCAnyHandle>();

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

            if (!materialProperties)
               ExporterCacheManager.CreatedInternalPropertySets.Add(whichElement.Id, createdPropertySets, elementSets);
         }
      }

      public static HashSet<IFCAnyHandle> CreateInternalRevitPropertySetsForTemporaryParts(ExporterIFC exporterIFC, Element part)
      {
         HashSet<IFCAnyHandle> createdPropertySets = new();

         if (exporterIFC == null || part == null ||
             (!ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportInternalRevit))
            return createdPropertySets;

         IFCFile file = exporterIFC.GetFile();

         SortedDictionary<string, (string, HashSet<IFCAnyHandle>)> createdProperties = CreateGroupedInternalProperties(file, part);

         if (createdProperties.Count == 0)
            return createdPropertySets;

         foreach (KeyValuePair<string, (string, HashSet<IFCAnyHandle>)> currPropertySet in createdProperties)
         {
            if (currPropertySet.Value.Item2.Count == 0)
               continue;

            string psetGUID = GUIDUtil.GenerateIFCGuidFrom(
               GUIDUtil.CreateGUIDString(part, "IfcPropertySet: " + currPropertySet.Key.ToString()));

            IFCAnyHandle propertySet = IFCInstanceExporter.CreatePropertySet(file, psetGUID,
               ExporterCacheManager.OwnerHistoryHandle, currPropertySet.Value.Item1, null,
               currPropertySet.Value.Item2);
            createdPropertySets.Add(propertySet);
         }
         return createdPropertySets;
      }

      private static SortedDictionary<string, (string, HashSet<IFCAnyHandle>)> CreateGroupedInternalProperties(IFCFile file, Element element)
      {
         ElementId elemId = element.Id;
         ParameterElementCache parameterElementCache = GetCachedParametersForElement(elemId, element is ElementType);
         
         IFCParameterTemplate parameterTemplate = ExporterCacheManager.ParameterMappingTemplate;

         SortedDictionary<string, ParameterElementCache> parametersByGroup = [];
         foreach (IList<ParameterElementInfo> parameters in parameterElementCache.ParameterIdCache.Values)
         {
            foreach (ParameterElementInfo info in parameters)
            {
               string groupTypeId = info.Details.GroupTypeIdAsString;
               if (!parametersByGroup.TryGetValue(groupTypeId, out ParameterElementCache currentCache))
               {
                  currentCache = new(elemId);
                  parametersByGroup.Add(groupTypeId, currentCache);
               }
               currentCache.AddParameter(info.Name, info.ElementId, info.Details, info.HostExtendedPropertyLinkId);
            }
         }

         SortedDictionary<string, (string, HashSet<IFCAnyHandle>)> propertySets = [];
         foreach (KeyValuePair<string, ParameterElementCache> parameterElementGroup in parametersByGroup)
         {
            ForgeTypeId parameterGroup = new ForgeTypeId(parameterElementGroup.Key);
            string groupName = LabelUtils.GetLabelForGroup(parameterGroup);

            // Skip property groups excluded from export
            if (parameterTemplate != null &&
                parameterTemplate.IsPropertySetAMemberOfTemplate(PropertySetupType.RevitElementParameters, groupName) &&
               !parameterTemplate.IsExportingPropertySet(PropertySetupType.RevitElementParameters, groupName))
            {
               continue;
            }

            // We are only going to append the "(Type)" suffix if we aren't also exporting the corresponding entity type.
            // In general, we'd like to always export them entity type, regardless of whether it holds any geometry or not - it can hold
            // at least the parameteric information.  When this is achieved, when can get rid of this entirely.
            // Unfortunately, IFC2x3 doesn't have types for all entities, so for IFC2x3 at least this will continue to exist
            // in some fashion.
            // There was a suggestion in SourceForge that we could "merge" the instance/type property sets in the cases where we aren't
            // creating an entity type, and in the cases where two properties had the same name, use the instance over type.
            // However, given our intention to generally export all types, this seems like a lot of work for diminishing returns.
            string groupNameToExport = groupName;
            if (element is ElementType elementType &&
              !ExporterCacheManager.ElementTypeToHandleCache.IsRegistered(elementType))
               groupNameToExport += Properties.Resources.PropertySetTypeSuffix;

            HashSet<IFCAnyHandle> currPropertiesForGroup = new();
            propertySets[parameterElementGroup.Key] = (groupNameToExport, currPropertiesForGroup);

            // TODO: This looks like it has some redundant code to clean up.
            foreach (EvaluatedParameter parameter in parameterElementGroup.Value.CalculateAllValues())
            {
               string proxyValue = null;
               if (!parameter.HasValue)
               {
                  proxyValue = ProxyParameter.GetProxyValue(element, parameter);
                  if (string.IsNullOrEmpty(proxyValue))
                     continue;
               }

               Definition parameterDefinition = parameter.Definition;
               if (parameterDefinition == null)
                  continue;

               string parameterName = parameterDefinition.Name;

               IFCPropertyMappingInfo mappingInfo = GetParameterMappingInfoFromCache(PropertySetupType.RevitElementParameters,
                  groupName, parameter.Definition.Id, parameterName);
               if ((mappingInfo?.ExportFlag ?? true) == false)
                  continue;

               string parameterNameToUse = !string.IsNullOrEmpty(mappingInfo?.IFCPropertyName) ? mappingInfo.IFCPropertyName : parameterName;

               IFCAnyHandle propertyHnd = CreatePropertyByParameterStorageType(file, parameter, proxyValue,
                  parameterNameToUse);
               if (propertyHnd != null)
                  currPropertiesForGroup.Add(propertyHnd);
            }
         }

         return propertySets;
      }


      /// <summary>
      /// Creates a property handle for a parameter based on its storage type.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="parameter">The parameter.</param>
      /// <param name="proxyValue">An alternative value for the parameter, can be unset.</param>
      /// <param name="propertyName">The property name.</param>
      /// <returns>The property handle.</returns>
      public static IFCAnyHandle CreatePropertyByParameterStorageType(IFCFile file, EvaluatedParameter parameter,
         string proxyValue, string propertyName)
      {
         // NOTE: Built-in parameters don't have descriptions, and proxy values will only exist for built-in parameters.
         if (!string.IsNullOrEmpty(proxyValue))
         {
            return CreateTextPropertyFromCache(file, new(propertyName, null), proxyValue, PropertyValueType.SingleValue);
         }

         StorageType storageType = parameter?.StorageType ?? StorageType.None;
         if (storageType == StorageType.None)
            return null;

         Definition parameterDefinition = parameter.Definition;
         if (parameterDefinition == null)
            return null;

         PropertyDescription propertyDescription = new(propertyName, null);

         switch (storageType)
         {
            case StorageType.Integer:
               {
                  bool hasValue = parameter.HasValue;
                  int value = (parameter.Value as IntegerParameterValue)?.Value ?? 0;
                  
                  // YesNo or actual integer?
                  if (parameterDefinition.GetDataType() == SpecTypeId.Boolean.YesNo)
                  {
                     return hasValue ? CreateBooleanPropertyFromCache(file, propertyDescription, value != 0, PropertyValueType.SingleValue) : null;
                  }

                  if (!parameterDefinition.GetDataType().Empty())
                     return hasValue ? CreateIntegerPropertyFromCache(file, propertyDescription, value, PropertyValueType.SingleValue) : null;

                  string valueAsString = parameter.AsValueString(ExporterCacheManager.Document);

                  // This is probably an internal enumerated type that should be exported as a string.
                  // NOTE: We check this even if the parameter doesn't have a value!  In this case, the UI shows <something>.
                  if (!string.IsNullOrEmpty(valueAsString))
                  {
                     return CreateIdentifierPropertyFromCache(file, propertyDescription, valueAsString, PropertyValueType.SingleValue);
                  }

                  // For internal enum parameters where AsValueString() returns nothing,
                  // the raw integer IS the value (e.g. StructuralAssetClass = 0 = Undefined).
                  // EvaluatedParameter.HasValue may be false for default enum value 0, but
                  // the parameter exists and its integer value must still be exported.
                  return CreateIntegerPropertyFromCache(file, propertyDescription, value, PropertyValueType.SingleValue);
               }
            case StorageType.Double:
               {
                  if (!parameter.HasValue)
                     return null;

                  double value = (parameter.Value as DoubleParameterValue)?.Value ?? 0.0;
                  return CreateRealPropertyBasedOnParameterType(file, parameter, propertyDescription, value, PropertyValueType.SingleValue);
               }
            case StorageType.String:
               {
                  string value = (parameter.Value as StringParameterValue)?.Value;
                  if (string.IsNullOrEmpty(value))
                     return null;
                  return CreateTextPropertyFromCache(file, propertyDescription, value, PropertyValueType.SingleValue);
               }
            case StorageType.ElementId:
               {
                  if (!parameter.HasValue)
                     return null;

                  if (MathUtil.IsInvalidElementId((parameter.Value as ElementIdParameterValue)?.Value ?? ElementId.InvalidElementId))
                     return null;
                  
                  string valueString = parameter.AsValueString(ExporterCacheManager.Document);
                  return CreateLabelPropertyFromCache(file, parameter.Definition.Id, propertyDescription, valueString, PropertyValueType.SingleValue, true, null);
               }
         }

         return null;
      }

      /// <summary>
      /// Gets parameter mapping information from cache.
      /// </summary>
      /// <param name="propertySetup">The property setup.</param>
      /// <param name="groupName">The parameter group name.</param>
      /// <param name="parameterId">The parameter id.</param>
      /// <param name="parameterName">parameter name.</param>
      /// <returns>The parameter mapping info.</returns>
      public static IFCPropertyMappingInfo GetParameterMappingInfoFromCache(PropertySetupType propertySetup, string groupName, ElementId parameterId, string parameterName)
      {
         IFCParameterTemplate parameterTemplate = ExporterCacheManager.ParameterMappingTemplate;
         if (parameterTemplate == null)
            return null;

         ParameterMappingKey mappingKey = new(propertySetup, groupName, parameterId, parameterName);
         if (ExporterCacheManager.PropertyMappingCache.TryGetValue(mappingKey, out IFCPropertyMappingInfo mappingInfo))
            return mappingInfo;

         mappingInfo = (parameterId.Value < -1 || string.IsNullOrEmpty(parameterName)) ?
            parameterTemplate.FindPropertyMappingInfo(propertySetup, groupName, parameterId)
            : parameterTemplate.FindPropertyMappingInfo(propertySetup, groupName, parameterName);

         ExporterCacheManager.PropertyMappingCache[mappingKey] = mappingInfo;
         return mappingInfo;
      }

      /// <summary>
      /// Get a unit type of parameter.
      /// IFCUnit for each one.
      /// </summary>
      /// <param name="parameter">The parameter.</param>
      /// <returns>The parameter unit type.</returns>
      public static ForgeTypeId GetParameterUnitType(EvaluatedParameter parameter)
      {
         try
         {
            ForgeTypeId specTypeId = parameter?.Definition?.GetDataType();
            if (specTypeId == null || specTypeId.Empty())
               return null;

            return ExporterCacheManager.DocumentUnits.GetFormatOptions(specTypeId).GetUnitTypeId();
         }
         catch (Exception ex) when
         (ex is Autodesk.Revit.Exceptions.InvalidOperationException ||
          ex is Autodesk.Revit.Exceptions.ArgumentNullException ||
          ex is Autodesk.Revit.Exceptions.ArgumentException)
         {
         }

         return null;
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
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="propertyValue">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRealPropertyBasedOnParameterType(IFCFile file, EvaluatedParameter parameter,
         PropertyDescription propertyDescription, double propertyValue, PropertyValueType valueType)
      {
         if (parameter == null)
            return null;

         ForgeTypeId type = parameter.Definition?.GetDataType();
         ForgeTypeId fallbackUnitType = GetParameterUnitType(parameter);

         return CreateRealPropertyByType(file, type, propertyDescription, propertyValue, valueType, fallbackUnitType);
      }

      /// <summary>
      /// Creates property from real parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="parameterType">The type of the parameter.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="propertyValue">The value of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="fallbackUnitType">The optional unit type. Can be used for scaling in final case</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRealPropertyByType(IFCFile file, ForgeTypeId parameterType,
         PropertyDescription propertyDescription, double propertyValue, PropertyValueType valueType,
         ForgeTypeId fallbackUnitType = null)
      {
         IFCAnyHandle propertyHandle = null;

         // NOTE: For cases where multiple Revit parameterTypes map to one IFC unit system, we take the 
         // display units of only one of the types, regardless of the fact that they could all have different 
         // display units.  To allow for each Revit parameter type to have its separate display units, we
         // would have to keep track of "secondary" units so that (1) they weren't part of IfcUnitAssignment and
         // (2) we would assign the unit to the IfcProperty.  We would also have to make this not work for
         // at least Reference View, since specifying the unit of an IfcProperty is disallowed.
         if (parameterType == SpecTypeId.Acceleration)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.Acceleration, propertyValue);
            propertyHandle = CreateAccelerationPropertyFromCache(file, propertyDescription,
                new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Energy ||
            parameterType == SpecTypeId.HvacEnergy)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.Energy, propertyValue);
            propertyHandle = CreateEnergyPropertyFromCache(file, propertyDescription,
                new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.LinearMoment)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.LinearMoment, propertyValue);
            propertyHandle = CreateLinearMomentPropertyFromCache(file, propertyDescription,
                new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.MassPerUnitLength ||
            parameterType == SpecTypeId.PipeMassPerUnitLength)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.MassPerUnitLength, propertyValue);
            propertyHandle = CreateMassPerLengthPropertyFromCache(file, propertyDescription,
                new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Moment)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.Moment, propertyValue);
            propertyHandle = CreateTorquePropertyFromCache(file, propertyDescription,
                new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.PointSpringCoefficient)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.PointSpringCoefficient, propertyValue);
            propertyHandle = CreateLinearStiffnessPropertyFromCache(file, propertyDescription,
                new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Pulsation)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.Pulsation, propertyValue);
            propertyHandle = CreateAngularVelocityPropertyFromCache(file, propertyDescription,
                new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.ThermalResistance)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.ThermalResistance, propertyValue);
            propertyHandle = CreateThermalResistancePropertyFromCache(file, propertyDescription,
                new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.WarpingConstant)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.WarpingConstant, propertyValue);
            propertyHandle = CreateWarpingConstantPropertyFromCache(file, propertyDescription,
                new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Angle ||
            parameterType == SpecTypeId.Rotation ||
            parameterType == SpecTypeId.RotationAngle)
         {
            double scaledValue = UnitUtil.ScaleAngle(propertyValue);
            propertyHandle = CreatePlaneAnglePropertyFromCache(file, propertyDescription,
               new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Slope ||
            parameterType == SpecTypeId.HvacSlope ||
            parameterType == SpecTypeId.PipingSlope ||
            parameterType == SpecTypeId.DemandFactor ||
            parameterType == SpecTypeId.Factor)
         {
            propertyHandle = CreateRatioPropertyFromCache(file, propertyDescription,
               new List<double?>() { propertyValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Area ||
            parameterType == SpecTypeId.CrossSection ||
            parameterType == SpecTypeId.ReinforcementArea ||
            parameterType == SpecTypeId.SectionArea)
         {
            double scaledValue = UnitUtil.ScaleArea(propertyValue);
            propertyHandle = CreateAreaPropertyFromCache(file, propertyDescription,
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
            propertyHandle = CreateLengthPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Currency)
         {
            IFCData currencyData = ExporterCacheManager.UnitsCache.HasCurrencyUnit() ?
                  IFCDataUtil.CreateAsMonetaryMeasure(propertyValue) :
                  IFCDataUtil.CreateAsReal(propertyValue);
            propertyHandle = CreateCommonProperty(file, propertyDescription, currencyData,
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
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.HvacPower, propertyValue);
            propertyHandle = CreatePowerPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Current)
         {
            double scaledValue = UnitUtil.ScaleElectricCurrent(propertyValue);
            propertyHandle = CreateElectricCurrentPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Diffusivity)
         {
            double scaledValue = UnitUtil.ScaleMoistureDiffusivity(propertyValue);
            propertyHandle = CreateMoistureDiffusivityPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.ElectricalFrequency ||
            parameterType == SpecTypeId.StructuralFrequency)
         {
            propertyHandle = CreateFrequencyPropertyFromCache(file, propertyDescription,
                  new List<double?>() { propertyValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Illuminance)
         {
            double scaledValue = UnitUtil.ScaleIlluminance(propertyValue);
            propertyHandle = CreateIlluminancePropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.LuminousFlux)
         {
            double scaledValue = UnitUtil.ScaleLuminousFlux(propertyValue);
            propertyHandle = CreateLuminousFluxPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.LuminousIntensity)
         {
            double scaledValue = UnitUtil.ScaleLuminousIntensity(propertyValue);
            propertyHandle = CreateLuminousIntensityPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.ElectricalPotential)
         {
            double scaledValue = UnitUtil.ScaleElectricVoltage(propertyValue);
            propertyHandle = CreateElectricVoltagePropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.HvacTemperature ||
            parameterType == SpecTypeId.ElectricalTemperature ||
            parameterType == SpecTypeId.PipingTemperature)
         {
            double scaledValue = UnitUtil.ScaleThermodynamicTemperature(propertyValue);
            propertyHandle = CreateThermodynamicTemperaturePropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.HeatTransferCoefficient)
         {
            double scaledValue = UnitUtil.ScaleThermalTransmittance(propertyValue);
            propertyHandle = CreateThermalTransmittancePropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Force ||
            parameterType == SpecTypeId.Weight)
         {
            double scaledValue = UnitUtil.ScaleForce(propertyValue);
            propertyHandle = CreateForcePropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.AreaForce)
         {
            double scaledValue = UnitUtil.ScalePlanarForce(propertyValue);
            propertyHandle = CreatePlanarForcePropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.LinearForce ||
            parameterType == SpecTypeId.WeightPerUnitLength)
         {
            double scaledValue = UnitUtil.ScaleLinearForce(propertyValue);
            propertyHandle = CreateLinearForcePropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.AirFlow ||
            parameterType == SpecTypeId.Flow)
         {
            double scaledValue = UnitUtil.ScaleVolumetricFlowRate(propertyValue);
            propertyHandle = CreateVolumetricFlowRatePropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.HvacPressure ||
            parameterType == SpecTypeId.PipingPressure ||
            parameterType == SpecTypeId.Stress)
         {
            double scaledValue = UnitUtil.ScalePressure(propertyValue);
            propertyHandle = CreatePressurePropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.HvacVelocity ||
            parameterType == SpecTypeId.PipingVelocity ||
            parameterType == SpecTypeId.StructuralVelocity ||
            parameterType == SpecTypeId.Speed)
         {
            double scaledValue = UnitUtil.ScaleLinearVelocity(propertyValue);
            propertyHandle = CreateLinearVelocityPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Mass ||
            parameterType == SpecTypeId.PipingMass)
         {
            double scaledValue = UnitUtil.ScaleMass(propertyValue);
            propertyHandle = CreateMassPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.MassDensity ||
            parameterType == SpecTypeId.HvacDensity)
         {
            double scaledValue = UnitUtil.ScaleMassDensity(propertyValue);
            propertyHandle = CreateMassDensityPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.PipingDensity)
         {
            double scaledValue = UnitUtil.ScaleIonConcentration(propertyValue);
            propertyHandle = CreateIonConcentrationPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.MomentOfInertia)
         {
            double scaledValue = UnitUtil.ScaleMomentOfInertia(propertyValue);
            propertyHandle = CreateMomentOfInertiaPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Number)
         {
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription, new List<double?>() { propertyValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.PipingVolume ||
            parameterType == SpecTypeId.ReinforcementVolume ||
            parameterType == SpecTypeId.Volume)
         {
            double scaledValue = UnitUtil.ScaleVolume(propertyValue);
            propertyHandle = CreateVolumePropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.SectionModulus)
         {
            double scaledValue = UnitUtil.ScaleSectionModulus(propertyValue);
            propertyHandle = CreateSectionModulusPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.PipingMassPerTime ||
            parameterType == SpecTypeId.HvacMassPerTime)
         {
            double scaledValue = UnitUtil.ScaleMassFlowRate(propertyValue);
            propertyHandle = CreateMassFlowRatePropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.AngularSpeed)
         {
            double scaledValue = UnitUtil.ScaleRotationalFrequency(propertyValue);
            propertyHandle = CreateRotationalFrequencyPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.ThermalConductivity)
         {
            double scaledValue = UnitUtil.ScaleThermalConductivity(propertyValue);
            propertyHandle = CreateThermalConductivityPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.SpecificHeat)
         {
            double scaledValue = UnitUtil.ScaleSpecificHeatCapacity(propertyValue);
            propertyHandle = CreateSpecificHeatCapacityPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Permeability)
         {
            double scaledValue = UnitUtil.ScaleVaporPermeability(propertyValue);
            propertyHandle = CreateVaporPermeabilityPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.HvacViscosity ||
            parameterType == SpecTypeId.PipingViscosity)
         {
            double scaledValue = UnitUtil.ScaleDynamicViscosity(propertyValue);
            propertyHandle = CreateDynamicViscosityPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.ThermalExpansionCoefficient)
         {
            double scaledValue = UnitUtil.ScaleThermalExpansionCoefficient(propertyValue);
            propertyHandle = CreateThermalExpansionCoefficientPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.SpecificHeatOfVaporization)
         {
            double scaledValue = UnitUtil.ScaleHeatingValue(propertyValue);
            propertyHandle = CreateHeatingValuePropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.IsothermalMoistureCapacity)
         {
            double scaledValue = UnitUtil.ScaleIsothermalMoistureCapacity(propertyValue);
            propertyHandle = CreateIsothermalMoistureCapacityPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.HvacPowerDensity)
         {
            double scaledValue = UnitUtil.ScaleHeatFluxDensity(propertyValue);
            propertyHandle = CreateHeatFluxDensityPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.MassPerUnitArea && !ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
         {
            double scaledValue = UnitUtil.ScaleAreaDensity(propertyValue);
            propertyHandle = CreateAreaDensityPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, null);
         }
         else if (parameterType == SpecTypeId.Time ||
            parameterType == SpecTypeId.Period)
         {
            double scaledValue = UnitUtil.ScaleTime(propertyValue);
            IFCData timeData = IFCDataUtil.CreateAsTimeMeasure(scaledValue);
            propertyHandle = CreateCommonProperty(file, propertyDescription, timeData,
                  valueType, null);
         }
         else if (parameterType == SpecTypeId.ColorTemperature)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.ColorTemperature, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "COLORTEMPERATURE");
         }
         else if (parameterType == SpecTypeId.CostPerArea)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.CostPerArea, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "COSTPERAREA");
         }
         else if (parameterType == SpecTypeId.ApparentPowerDensity)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.ApparentPowerDensity, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "APPARENTPOWERDENSITY");
         }
         else if (parameterType == SpecTypeId.CostRateEnergy)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.CostRateEnergy, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "COSTRATEENERGY");
         }
         else if (parameterType == SpecTypeId.CostRatePower)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.CostRatePower, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "COSTRATEPOWER");
         }
         else if (parameterType == SpecTypeId.Efficacy)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.Efficacy, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "LUMINOUSEFFICACY");
         }
         else if (parameterType == SpecTypeId.Luminance)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.Luminance, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "LUMINANCE");
         }
         else if (parameterType == SpecTypeId.ElectricalPowerDensity)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.ElectricalPowerDensity, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "ELECTRICALPOWERDENSITY");
         }
         else if (parameterType == SpecTypeId.PowerPerLength)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.PowerPerLength, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "POWERPERLENGTH");
         }
         else if (parameterType == SpecTypeId.ElectricalResistivity)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.ElectricalResistivity, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "ELECTRICALRESISTIVITY");
         }
         else if (parameterType == SpecTypeId.HeatCapacityPerArea)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.HeatCapacityPerArea, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "HEATCAPACITYPERAREA");
         }
         else if (parameterType == SpecTypeId.ThermalGradientCoefficientForMoistureCapacity)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.ThermalGradientCoefficientForMoistureCapacity, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "THERMALGRADIENTCOEFFICIENTFORMOISTURECAPACITY");
         }
         else if (parameterType == SpecTypeId.ThermalMass)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.ThermalMass, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "THERMALMASS");
         }
         else if (parameterType == SpecTypeId.AirFlowDensity)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.AirFlowDensity, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "AIRFLOWDENSITY");
         }
         else if (parameterType == SpecTypeId.AirFlowDividedByCoolingLoad)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.AirFlowDividedByCoolingLoad, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "AIRFLOWDIVIDEDBYCOOLINGLOAD");
         }
         else if (parameterType == SpecTypeId.AirFlowDividedByVolume)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.AirFlowDividedByVolume, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "AIRFLOWDIVIDEDBYVOLUME");
         }
         else if (parameterType == SpecTypeId.AreaDividedByCoolingLoad)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.AreaDividedByCoolingLoad, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "AREADIVIDEDBYCOOLINGLOAD");
         }
         else if (parameterType == SpecTypeId.AreaDividedByHeatingLoad)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.AreaDividedByHeatingLoad, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "AREADIVIDEDBYHEATINGLOAD");
         }
         else if (parameterType == SpecTypeId.CoolingLoadDividedByArea)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.CoolingLoadDividedByArea, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "COOLINGLOADDIVIDEDBYAREA");
         }
         else if (parameterType == SpecTypeId.CoolingLoadDividedByVolume)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.CoolingLoadDividedByVolume, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "COOLINGLOADDIVIDEDBYVOLUME");
         }
         else if (parameterType == SpecTypeId.FlowPerPower)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.FlowPerPower, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "FLOWPERPOWER");
         }
         else if (parameterType == SpecTypeId.HvacFriction)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.HvacFriction, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
               new List<double?>() { scaledValue }, valueType, "FRICTIONLOSS");
         }
         else if (parameterType == SpecTypeId.HeatingLoadDividedByArea)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.HeatingLoadDividedByArea, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "HEATINGLOADDIVIDEDBYAREA");
         }
         else if (parameterType == SpecTypeId.HeatingLoadDividedByVolume)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.HeatingLoadDividedByVolume, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "HEATINGLOADDIVIDEDBYVOLUME");
         }
         else if (parameterType == SpecTypeId.PowerPerFlow)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.PowerPerFlow, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "POWERPERFLOW");
         }
         else if (parameterType == SpecTypeId.PipingFriction)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.PipingFriction, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "PIPINGFRICTION");
         }
         else if (parameterType == SpecTypeId.AreaSpringCoefficient)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.AreaSpringCoefficient, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "AREASPRINGCOEFFICIENT");
         }
         else if (parameterType == SpecTypeId.LineSpringCoefficient)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.LineSpringCoefficient, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "LINESPRINGCOEFFICIENT");
         }
         else if (parameterType == SpecTypeId.MassPerUnitArea)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.MassPerUnitArea, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "MASSPERUNITAREA");
         }
         else if (parameterType == SpecTypeId.ReinforcementAreaPerUnitLength)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.ReinforcementAreaPerUnitLength, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "REINFORCEMENTAREAPERUNITLENGTH");
         }
         else if (parameterType == SpecTypeId.RotationalLineSpringCoefficient)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.RotationalLineSpringCoefficient, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "ROTATIONALLINESPRINGCOEFFICIENT");
         }
         else if (parameterType == SpecTypeId.RotationalPointSpringCoefficient)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.RotationalPointSpringCoefficient, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "ROTATIONALPOINTSPRINGCOEFFICIENT");
         }
         else if (parameterType == SpecTypeId.UnitWeight)
         {
            double scaledValue = UnitUtil.ScaleDouble(SpecTypeId.UnitWeight, propertyValue);
            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription,
                  new List<double?>() { scaledValue }, valueType, "UNITWEIGHT");
         }
         else
         {
            double scaledValue = propertyValue;
            if (fallbackUnitType != null)
               scaledValue = UnitUtils.ConvertFromInternalUnits(propertyValue, fallbackUnitType);

            propertyHandle = CreateRealPropertyFromCache(file, propertyDescription, new List<double?>() { scaledValue }, valueType, null);
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
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(prodTypeHnd))
            return;

         HashSet<IFCAnyHandle> propertySets = new HashSet<IFCAnyHandle>();

         // Pass in an empty set of handles - we don't want IfcRelDefinesByProperties for type properties.
         ISet<IFCAnyHandle> associatedObjectIds = new HashSet<IFCAnyHandle>();
         CreateInternalRevitPropertySets(exporterIFC, elementType, associatedObjectIds, false);

         TypePropertyInfo additionalPropertySets = null;
         ElementId typeId = elementType.Id;
         ExporterCacheManager.TypePropertyInfoCache.TryGetValue(typeId, out additionalPropertySets);

         if (existingPropertySets != null && existingPropertySets.Count > 0)
            propertySets.UnionWith(existingPropertySets);

         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            Document doc = elementType.Document;

            IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

            if (additionalPropertySets != null)
            {
               foreach (var (psetName, properties) in additionalPropertySets.PropertyInputs)
               {
                  string guid = GUIDUtil.GenerateIFCGuidFrom(
                     GUIDUtil.CreateGUIDString(IFCEntityType.IfcPropertySet, psetName, prodTypeHnd));
                  IFCAnyHandle pset = IFCInstanceExporter.CreatePropertySet(
                     file, guid, ownerHistory, psetName, null, properties);
                  propertySets.Add(pset);
               }
            }

            IList<PropertySetDescription> currPsetsToCreate =
               ExporterUtil.GetCurrPSetsToCreate(prodTypeHnd, PSetsToProcess.Type);
            foreach (PropertySetDescription currDesc in currPsetsToCreate)
            {
               // Last conditional check: if the property set comes from a ViewSchedule, check if the element is in the schedule.
               if (!MathUtil.IsInvalidElementId(currDesc.ViewScheduleId) && ExporterUtil.ExportingHostModel())
               {
                  if (!ExporterCacheManager.ViewScheduleElementCache[currDesc.ViewScheduleId].Contains(typeId))
                     continue;
               }

               ElementOrConnector elementOrConnector = new ElementOrConnector(elementType);
               ISet<IFCAnyHandle> props = currDesc.ProcessEntries(file, exporterIFC, null, elementOrConnector, elementType, prodTypeHnd, null);

               // Merge with pre-created properties from specific exporters (e.g., door/window panels).
               // Pre-created properties take precedence; centralized properties fill in the rest.
               if (ExporterCacheManager.PreCreatedPsetProperties.TryGetValue((currDesc.Name, typeId), out var preCreatedProps))
               {
                  Dictionary<string, IFCAnyHandle> merged = new(preCreatedProps);
                  foreach (IFCAnyHandle prop in props)
                  {
                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(prop)) 
                        continue;
                     string propName = IFCAnyHandleUtil.GetStringAttribute(prop, "Name");
                     if (!string.IsNullOrWhiteSpace(propName))
                        merged.TryAdd(propName, prop);
                  }
                  props = new HashSet<IFCAnyHandle>(merged.Values);
               }

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

      public static bool GetQuantityDoubleValueFromParameter(Element element, string revitParameterName,
         BuiltInParameter revitBuiltInParameter, QuantityType quantityType, out double value)
      {
         value = 0.0;
         if (element == null)
            return false;

         (EvaluatedParameter parameter, value) = GetDoubleValueFromElementOrSymbol(element, revitParameterName);
         if (parameter == null)
         { 
            (parameter, value) = GetDoubleValueFromElementOrSymbol(element, revitBuiltInParameter);
            if (parameter == null)
               return false;
         }

         switch (quantityType)
         {
            case QuantityType.PositiveLength:
            case QuantityType.Length:
               value = UnitUtil.ScaleLength(value);
               break;
            case QuantityType.Area:
               value = UnitUtil.ScaleArea(value);
               break;
            case QuantityType.Volume:
               value = UnitUtil.ScaleVolume(value);
               break;
            case QuantityType.Weight:
            case QuantityType.Mass:
               value = UnitUtil.ScaleMass(value);
               break;
            case QuantityType.Count:
               break;
            case QuantityType.Time:
               break;
            default:
               break;
         }

         return true;
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
         (EvaluatedParameter param, double propertyValue) = GetDoubleValueFromElement(elem, revitParameterName);
         if (param == null)
            return null;

         if (IsParameterScalingRequired(param))
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

      #region Create___PropertyFromElement_1
      /// <summary>
      /// Create Area measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAreaPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateAreaPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateAreaPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAccelerationPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateAccelerationPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateAccelerationPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAngularVelocityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateAngularVelocityPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateAngularVelocityPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAreaDensityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateAreaDensityPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateAreaDensityPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateDynamicViscosityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateDynamicViscosityPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateDynamicViscosityPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateElectricCurrentPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateElectricCurrentPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateElectricCurrentPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateElectricVoltagePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateElectricVoltagePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateElectricVoltagePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateEnergyPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateEnergyPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateEnergyPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateForcePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateForcePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateForcePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateFrequencyPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateFrequencyPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateFrequencyPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateHeatingValuePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateHeatingValuePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateHeatingValuePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIlluminancePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateIlluminancePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateIlluminancePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIonConcentrationPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateIonConcentrationPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateIonConcentrationPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIsothermalMoistureCapacityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateIsothermalMoistureCapacityPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateIsothermalMoistureCapacityPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateHeatFluxDensityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateHeatFluxDensityPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateHeatFluxDensityPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLengthPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateLengthPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateLengthPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearForcePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateLinearForcePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateLinearForcePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearMomentPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateLinearMomentPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateLinearMomentPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearStiffnessPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateLinearStiffnessPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateLinearStiffnessPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearVelocityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateLinearVelocityPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateLinearVelocityPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLuminousFluxPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateLuminousFluxPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateLuminousFluxPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLuminousIntensityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateLuminousIntensityPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateLuminousIntensityPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateMassPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateMassPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassDensityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateMassDensityPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateMassDensityPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassFlowRatePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateMassFlowRatePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateMassFlowRatePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassPerLengthPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateMassPerLengthPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateMassPerLengthPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateModulusOfElasticityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateModulusOfElasticityPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateModulusOfElasticityPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMoistureDiffusivityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateMoistureDiffusivityPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateMoistureDiffusivityPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMomentOfInertiaPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateMomentOfInertiaPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateMomentOfInertiaPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create SectionModulus measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSectionModulusPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateSectionModulusPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateSectionModulusPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateNormalisedRatioPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateNormalisedRatioPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateNormalisedRatioPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateNumericPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateNumericPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateNumericPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePlaneAnglePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreatePlaneAnglePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreatePlaneAnglePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePlanarForcePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreatePlanarForcePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreatePlanarForcePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create NonNegativeLength measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateNonNegativeLengthPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateNonNegativeLengthPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateNonNegativeLengthPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositiveLengthPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreatePositiveLengthPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreatePositiveLengthPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositiveRatioPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreatePositiveRatioPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreatePositiveRatioPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositivePlaneAnglePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreatePositivePlaneAnglePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreatePositivePlaneAnglePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePowerPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreatePowerPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreatePowerPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePressurePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreatePressurePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreatePressurePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRatioPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateRatioPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateRatioPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRealPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateRealPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateRealPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRotationalFrequencyPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateRotationalFrequencyPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateRotationalFrequencyPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPowerPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateSoundPowerPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateSoundPowerPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
               return propHnd;
         }

         return null;
      }

      /// <summary>
      /// Create SoundPowerLevel measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPowerLevelPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateSoundPowerLevelPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateSoundPowerLevelPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPressurePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateSoundPressurePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateSoundPressurePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSpecificHeatCapacityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateSpecificHeatCapacityPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateSpecificHeatCapacityPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalConductivityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateThermalConductivityPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateThermalConductivityPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalExpansionCoefficientPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateThermalExpansionCoefficientPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateThermalExpansionCoefficientPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalResistancePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateThermalResistancePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateThermalResistancePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalTransmittancePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateThermalTransmittancePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateThermalTransmittancePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermodynamicTemperaturePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateThermodynamicTemperaturePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateThermodynamicTemperaturePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVaporPermeabilityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateVaporPermeabilityPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateVaporPermeabilityPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVolumePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateVolumePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateVolumePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVolumetricFlowRatePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateVolumetricFlowRatePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateVolumetricFlowRatePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTorquePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateTorquePropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateTorquePropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="revitBuiltInParam">The built in parameter to use.</param>
      /// <param name="propertyDescription">The name of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateWarpingConstantPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          BuiltInParameter revitBuiltInParam, PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IFCAnyHandle propHnd = CreateWarpingConstantPropertyFromElement(file, elem, revitParameterName, propertyDescription, valueType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return propHnd;

         if (revitBuiltInParam != BuiltInParameter.INVALID)
         {
            string builtInParamName = NamingUtil.GetSafeLabel(revitBuiltInParam);
            propHnd = CreateWarpingConstantPropertyFromElement(file, elem, builtInParamName, propertyDescription, valueType);
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
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAreaPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Area, valueType);
         IFCAnyHandle property = CreateAreaPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Area, valueType);
            property = CreateAreaPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Acceleration measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAccelerationPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Acceleration, valueType);
         IFCAnyHandle property = CreateAccelerationPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Acceleration, valueType);
            property = CreateAccelerationPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a AngularVelocity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAngularVelocityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Pulsation, valueType);
         IFCAnyHandle property = CreateAngularVelocityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Pulsation, valueType);
            property = CreateAngularVelocityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a AreaDensity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAreaDensityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.MassPerUnitArea, valueType);
         IFCAnyHandle property = CreateAreaDensityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.MassPerUnitArea, valueType);
            property = CreateAreaDensityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a DynamicViscosity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateDynamicViscosityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.HvacViscosity, valueType);
         IFCAnyHandle property = CreateDynamicViscosityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.HvacViscosity, valueType);
            property = CreateDynamicViscosityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a ElectricCurrent measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateElectricCurrentPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Current, valueType);
         IFCAnyHandle property = CreateElectricCurrentPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Current, valueType);
            property = CreateElectricCurrentPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a ElectricVoltage measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateElectricVoltagePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.ElectricalPotential, valueType);
         IFCAnyHandle property = CreateElectricVoltagePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.ElectricalPotential, valueType);
            property = CreateElectricVoltagePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Energy measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateEnergyPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Energy, valueType);
         IFCAnyHandle property = CreateEnergyPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Energy, valueType);
            property = CreateEnergyPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Force measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateForcePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Force, valueType);
         IFCAnyHandle property = CreateForcePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Force, valueType);
            property = CreateForcePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Frequency measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateFrequencyPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Number, valueType);
         IFCAnyHandle property = CreateFrequencyPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Number, valueType);
            property = CreateFrequencyPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a HeatingValue measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateHeatingValuePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.SpecificHeatOfVaporization, valueType);
         IFCAnyHandle property = CreateHeatingValuePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.SpecificHeatOfVaporization, valueType);
            property = CreateHeatingValuePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Illuminance measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIlluminancePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Illuminance, valueType);
         IFCAnyHandle property = CreateIlluminancePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Illuminance, valueType);
            property = CreateIlluminancePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a IonConcentration measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIonConcentrationPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.PipingDensity, valueType);
         IFCAnyHandle property = CreateIonConcentrationPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.PipingDensity, valueType);
            property = CreateIonConcentrationPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a IsothermalMoistureCapacity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIsothermalMoistureCapacityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.IsothermalMoistureCapacity, valueType);
         IFCAnyHandle property = CreateIsothermalMoistureCapacityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.IsothermalMoistureCapacity, valueType);
            property = CreateIsothermalMoistureCapacityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a HeatFluxDensity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateHeatFluxDensityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.HvacPowerDensity, valueType);
         IFCAnyHandle property = CreateHeatFluxDensityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.HvacPowerDensity, valueType);
            property = CreateHeatFluxDensityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Length measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLengthPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Length, valueType);
         IFCAnyHandle property = CreateLengthPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Length, valueType);
            property = CreateLengthPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a LinearForce measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearForcePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.LinearForce, valueType);
         IFCAnyHandle property = CreateLinearForcePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.LinearForce, valueType);
            property = CreateLinearForcePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a LinearMoment measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearMomentPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.LinearMoment, valueType);
         IFCAnyHandle property = CreateLinearMomentPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.LinearMoment, valueType);
            property = CreateLinearMomentPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a LinearStiffness measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearStiffnessPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.PointSpringCoefficient, valueType);
         IFCAnyHandle property = CreateLinearStiffnessPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.PointSpringCoefficient, valueType);
            property = CreateLinearStiffnessPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a LinearVelocity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearVelocityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.HvacVelocity, valueType);
         IFCAnyHandle property = CreateLinearVelocityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.HvacVelocity, valueType);
            property = CreateLinearVelocityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a LuminousFlux measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLuminousFluxPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.LuminousFlux, valueType);
         IFCAnyHandle property = CreateLuminousFluxPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.LuminousFlux, valueType);
            property = CreateLuminousFluxPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a LuminousIntensity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLuminousIntensityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.LuminousIntensity, valueType);
         IFCAnyHandle property = CreateLuminousIntensityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.LuminousIntensity, valueType);
            property = CreateLuminousIntensityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Mass measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Mass, valueType);
         IFCAnyHandle property = CreateMassPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Mass, valueType);
            property = CreateMassPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a MassDensity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassDensityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.MassDensity, valueType);
         IFCAnyHandle property = CreateMassDensityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.MassDensity, valueType);
            property = CreateMassDensityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a MassFlowRate measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassFlowRatePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.PipingMassPerTime, valueType);
         IFCAnyHandle property = CreateMassFlowRatePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.PipingMassPerTime, valueType);
            property = CreateMassFlowRatePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a MassPerLength measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassPerLengthPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.MassPerUnitLength, valueType);
         IFCAnyHandle property = CreateMassPerLengthPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.MassPerUnitLength, valueType);
            property = CreateMassPerLengthPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a ModulusOfElasticity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateModulusOfElasticityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Stress, valueType);
         IFCAnyHandle property = CreateModulusOfElasticityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Stress, valueType);
            property = CreateModulusOfElasticityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a MoistureDiffusivity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMoistureDiffusivityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Diffusivity, valueType);
         IFCAnyHandle property = CreateMoistureDiffusivityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Diffusivity, valueType);
            property = CreateMoistureDiffusivityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a MomentOfInertia measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMomentOfInertiaPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.MomentOfInertia, valueType);
         IFCAnyHandle property = CreateMomentOfInertiaPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.MomentOfInertia, valueType);
            property = CreateMomentOfInertiaPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a SectionModulus measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSectionModulusPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.SectionModulus, valueType);
         IFCAnyHandle property = CreateSectionModulusPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.SectionModulus, valueType);
            property = CreateSectionModulusPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a NormalisedRatio measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateNormalisedRatioPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Number, valueType);
         IFCAnyHandle property = CreateNormalisedRatioPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Number, valueType);
            property = CreateNormalisedRatioPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Numeric measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateNumericPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Number, valueType);
         IFCAnyHandle property = CreateNumericPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Number, valueType);
            property = CreateNumericPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a PlaneAngle measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePlaneAnglePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Angle, valueType);
         IFCAnyHandle property = CreatePlaneAnglePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Angle, valueType);
            property = CreatePlaneAnglePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a PlanarForce measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePlanarForcePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.AreaForce, valueType);
         IFCAnyHandle property = CreatePlanarForcePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.AreaForce, valueType);
            property = CreatePlanarForcePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a NonNegativeLength measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateNonNegativeLengthPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Length, valueType);
         IFCAnyHandle property = CreateNonNegativeLengthPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Length, valueType);
            property = CreateNonNegativeLengthPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a PositiveLength measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositiveLengthPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Length, valueType);
         IFCAnyHandle property = CreatePositiveLengthPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Length, valueType);
            property = CreatePositiveLengthPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a PositiveRatio measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositiveRatioPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Number, valueType);
         IFCAnyHandle property = CreatePositiveRatioPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Number, valueType);
            property = CreatePositiveRatioPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a PositivePlaneAngle measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositivePlaneAnglePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Angle, valueType);
         IFCAnyHandle property = CreatePositivePlaneAnglePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Angle, valueType);
            property = CreatePositivePlaneAnglePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Power measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePowerPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.HvacPower, valueType);
         IFCAnyHandle property = CreatePowerPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.HvacPower, valueType);
            property = CreatePowerPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Pressure measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePressurePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.HvacPressure, valueType);
         IFCAnyHandle property = CreatePressurePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.HvacPressure, valueType);
            property = CreatePressurePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Ratio measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRatioPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Number, valueType);
         IFCAnyHandle property = CreateRatioPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Number, valueType);
            property = CreateRatioPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Real measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRealPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Number, valueType);
         IFCAnyHandle property = CreateRealPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Number, valueType);
            property = CreateRealPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a RotationalFrequency measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRotationalFrequencyPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.AngularSpeed, valueType);
         IFCAnyHandle property = CreateRotationalFrequencyPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.AngularSpeed, valueType);
            property = CreateRotationalFrequencyPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a SoundPower measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPowerPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Wattage, valueType);
         IFCAnyHandle property = CreateSoundPowerPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Wattage, valueType);
            property = CreateSoundPowerPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a SoundPowerLevel measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPowerLevelPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Number, valueType);
         IFCAnyHandle property = CreateSoundPowerLevelPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Number, valueType);
            property = CreateSoundPowerLevelPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a SoundPressure measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPressurePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.HvacPressure, valueType);
         IFCAnyHandle property = CreateSoundPressurePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.HvacPressure, valueType);
            property = CreateSoundPressurePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a SpecificHeatCapacity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSpecificHeatCapacityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.SpecificHeat, valueType);
         IFCAnyHandle property = CreateSpecificHeatCapacityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.SpecificHeat, valueType);
            property = CreateSpecificHeatCapacityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a ThermalConductivity measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalConductivityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.ThermalConductivity, valueType);
         IFCAnyHandle property = CreateThermalConductivityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.ThermalConductivity, valueType);
            property = CreateThermalConductivityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a ThermalExpansionCoefficient measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalExpansionCoefficientPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.ThermalExpansionCoefficient, valueType);
         IFCAnyHandle property = CreateThermalExpansionCoefficientPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.ThermalExpansionCoefficient, valueType);
            property = CreateThermalExpansionCoefficientPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a ThermalResistance measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalResistancePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.ThermalResistance, valueType);
         IFCAnyHandle property = CreateThermalResistancePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.ThermalResistance, valueType);
            property = CreateThermalResistancePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a ThermalTransmittance measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalTransmittancePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.HeatTransferCoefficient, valueType);
         IFCAnyHandle property = CreateThermalTransmittancePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.HeatTransferCoefficient, valueType);
            property = CreateThermalTransmittancePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a ThermodynamicTemperature measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermodynamicTemperaturePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.HvacTemperature, valueType);
         IFCAnyHandle property = CreateThermodynamicTemperaturePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.HvacTemperature, valueType);
            property = CreateThermodynamicTemperaturePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a VaporPermeability measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVaporPermeabilityPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Permeability, valueType);
         IFCAnyHandle property = CreateVaporPermeabilityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Permeability, valueType);
            property = CreateVaporPermeabilityPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Volume measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVolumePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Volume, valueType);
         IFCAnyHandle property = CreateVolumePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Volume, valueType);
            property = CreateVolumePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a VolumetricFlowRate measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVolumetricFlowRatePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.AirFlow, valueType);
         IFCAnyHandle property = CreateVolumetricFlowRatePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.AirFlow, valueType);
            property = CreateVolumetricFlowRatePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a Torque measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTorquePropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.Moment, valueType);
         IFCAnyHandle property = CreateTorquePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.Moment, valueType);
            property = CreateTorquePropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }

      /// <summary>
      /// Create a WarpingConstant measure property from the element's parameter.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="elem">The Element.</param>
      /// <param name="revitParameterName">The name and description of the parameter.</param>
      /// <param name="propertyDescription">The name of the property. Also, the backup name of the parameter.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateWarpingConstantPropertyFromElement(IFCFile file, Element elem, string revitParameterName,
          PropertyDescription propertyDescription, PropertyValueType valueType)
      {
         IList<double?> doubleValues = GetDoubleValuesFromParameterByType(elem, revitParameterName, SpecTypeId.WarpingConstant, valueType);
         IFCAnyHandle property = CreateWarpingConstantPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);

         if (property == null)
         {
            doubleValues = GetDoubleValuesFromParameterByType(elem, propertyDescription.Name, SpecTypeId.WarpingConstant, valueType);
            property = CreateWarpingConstantPropertyFromCache(file, propertyDescription, doubleValues, valueType, null);
         }

         return property;
      }
      #endregion

      #region Create___PropertyFromCache

      /// <summary>Create property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="createProperty">The function to craete property.</param>
      /// <param name="propertyType">The property type.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateGenericPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey,
          Func<IFCFile, PropertyDescription, IList<double?>, PropertyValueType, string, IFCAnyHandle> createProperty, PropertyType propertyType)
      {
         if ((values?.Count ?? 0) == 0)
            return null;

         bool canCache = false;
         double value = 0.0;
         if (values.ElementAt(0) != null && valueType == PropertyValueType.SingleValue && string.IsNullOrEmpty(unitTypeKey))
         {
            bool isLengthProperty = (propertyType == PropertyType.Length);
            value = values.ElementAt(0).Value;

            double? adjustedValue = isLengthProperty ? CanCacheDouble(UnitUtil.UnscaleLength(value)) : CanCacheDouble(value);
            canCache = adjustedValue.HasValue;
            if (canCache)
            {
               value = isLengthProperty ? UnitUtil.ScaleLength(adjustedValue.GetValueOrDefault()) : adjustedValue.GetValueOrDefault();
               values[0] = value;
            }
         }

         IFCAnyHandle propertyHandle;
         string propertyName = propertyDescription.Name;
         if (canCache)
         {
            propertyHandle = ExporterCacheManager.PropertyInfoCache.GetDoubleCache(propertyType).Find(propertyName, value);
            if (propertyHandle != null)
               return propertyHandle;
         }

         propertyHandle = createProperty(file, propertyDescription, values, valueType, unitTypeKey);

         if (canCache && !IFCAnyHandleUtil.IsNullOrHasNoValue(propertyHandle))
         {
            ExporterCacheManager.PropertyInfoCache.GetDoubleCache(propertyType).Add(propertyName, value, propertyHandle);
         }

         return propertyHandle;
      }


      /// <summary>Create Area property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateAreaPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateAreaProperty, PropertyType.Area);
      }

      /// <summary>Create Acceleration property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateAccelerationPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateAccelerationProperty, PropertyType.Acceleration);
      }

      /// <summary>Create AngularVelocity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateAngularVelocityPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateAngularVelocityProperty, PropertyType.AngularVelocity);
      }

      /// <summary>Create AreaDensity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateAreaDensityPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateAreaDensityProperty, PropertyType.AreaDensity);
      }

      /// <summary>Create DynamicViscosity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateDynamicViscosityPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateDynamicViscosityProperty, PropertyType.DynamicViscosity);
      }

      /// <summary>Create ElectricCurrent property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateElectricCurrentPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateElectricCurrentProperty, PropertyType.ElectricCurrent);
      }

      /// <summary>Create ElectricVoltage property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateElectricVoltagePropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateElectricVoltageProperty, PropertyType.ElectricVoltage);
      }

      /// <summary>Create Energy property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateEnergyPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateEnergyProperty, PropertyType.Energy);
      }

      /// <summary>Create Force property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateForcePropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateForceProperty, PropertyType.Force);
      }

      /// <summary>Create Frequency property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateFrequencyPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateFrequencyProperty, PropertyType.Frequency);
      }

      /// <summary>Create HeatingValue property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateHeatingValuePropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateHeatingValueProperty, PropertyType.HeatingValue);
      }

      /// <summary>Create Illuminance property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateIlluminancePropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateIlluminanceProperty, PropertyType.Illuminance);
      }

      /// <summary>Create IonConcentration property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateIonConcentrationPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateIonConcentrationProperty, PropertyType.IonConcentration);
      }

      /// <summary>Create IsothermalMoistureCapacity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateIsothermalMoistureCapacityPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateIsothermalMoistureCapacityProperty, PropertyType.IsothermalMoistureCapacity);
      }

      /// <summary>Create HeatFluxDensity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateHeatFluxDensityPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateHeatFluxDensityProperty, PropertyType.HeatFluxDensity);
      }

      /// <summary>Create Length property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateLengthPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateLengthProperty, PropertyType.Length);
      }

      /// <summary>Create LinearForce property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateLinearForcePropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateLinearForceProperty, PropertyType.LinearForce);
      }

      /// <summary>Create LinearMoment property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateLinearMomentPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateLinearMomentProperty, PropertyType.LinearMoment);
      }

      /// <summary>Create LinearStiffness property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateLinearStiffnessPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateLinearStiffnessProperty, PropertyType.LinearStiffness);
      }

      /// <summary>Create LinearVelocity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateLinearVelocityPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateLinearVelocityProperty, PropertyType.LinearVelocity);
      }

      /// <summary>Create LuminousFlux property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateLuminousFluxPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateLuminousFluxProperty, PropertyType.LuminousFlux);
      }

      /// <summary>Create LuminousIntensity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateLuminousIntensityPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateLuminousIntensityProperty, PropertyType.LuminousIntensity);
      }

      /// <summary>Create Mass property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateMassPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateMassProperty, PropertyType.Mass);
      }

      /// <summary>Create MassDensity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateMassDensityPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateMassDensityProperty, PropertyType.MassDensity);
      }

      /// <summary>Create MassFlowRate property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateMassFlowRatePropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateMassFlowRateProperty, PropertyType.MassFlowRate);
      }

      /// <summary>Create MassPerLength property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateMassPerLengthPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateMassPerLengthProperty, PropertyType.MassPerLength);
      }

      /// <summary>Create ModulusOfElasticity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateModulusOfElasticityPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateModulusOfElasticityProperty, PropertyType.ModulusOfElasticity);
      }

      /// <summary>Create MoistureDiffusivity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateMoistureDiffusivityPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateMoistureDiffusivityProperty, PropertyType.MoistureDiffusivity);
      }

      /// <summary>Create MomentOfInertia property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateMomentOfInertiaPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateMomentOfInertiaProperty, PropertyType.MomentOfInertia);
      }

      /// <summary>Create SectionModulus property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateSectionModulusPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateSectionModulusProperty, PropertyType.SectionModulus);
      }

      /// <summary>Create NormalisedRatio property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateNormalisedRatioPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateNormalisedRatioProperty, PropertyType.NormalisedRatio);
      }

      /// <summary>Create Numeric property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateNumericPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateNumericProperty, PropertyType.Numeric);
      }

      /// <summary>Create PlaneAngle property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreatePlaneAnglePropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreatePlaneAngleProperty, PropertyType.PlaneAngle);
      }

      /// <summary>Create PlanarForce property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreatePlanarForcePropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreatePlanarForceProperty, PropertyType.PlanarForce);
      }

      /// <summary>Create NonNegativeLength property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateNonNegativeLengthPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateNonNegativeLengthProperty, PropertyType.NonNegativeLength);
      }

      /// <summary>Create PositiveLength property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreatePositiveLengthPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreatePositiveLengthProperty, PropertyType.PositiveLength);
      }

      /// <summary>Create PositiveRatio property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreatePositiveRatioPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreatePositiveRatioProperty, PropertyType.PositiveRatio);
      }

      /// <summary>Create PositivePlaneAngle property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreatePositivePlaneAnglePropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreatePositivePlaneAngleProperty, PropertyType.PositivePlaneAngle);
      }

      /// <summary>Create Power property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreatePowerPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreatePowerProperty, PropertyType.Power);
      }

      /// <summary>Create Pressure property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreatePressurePropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreatePressureProperty, PropertyType.Pressure);
      }

      /// <summary>Create Ratio property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateRatioPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateRatioProperty, PropertyType.Ratio);
      }

      /// <summary>Create Real property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateRealPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateRealProperty, PropertyType.Real);
      }

      /// <summary>Create RotationalFrequency property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateRotationalFrequencyPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateRotationalFrequencyProperty, PropertyType.RotationalFrequency);
      }

      /// <summary>Create SoundPower property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateSoundPowerPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateSoundPowerProperty, PropertyType.SoundPower);
      }

      /// <summary>Create SoundPowerLevel property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateSoundPowerLevelPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateSoundPowerLevelProperty, PropertyType.SoundPowerLevel);
      }

      /// <summary>Create SoundPressure property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateSoundPressurePropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateSoundPressureProperty, PropertyType.SoundPressure);
      }

      /// <summary>Create SpecificHeatCapacity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateSpecificHeatCapacityPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateSpecificHeatCapacityProperty, PropertyType.SpecificHeatCapacity);
      }

      /// <summary>Create ThermalConductivity property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateThermalConductivityPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateThermalConductivityProperty, PropertyType.ThermalConductivity);
      }

      /// <summary>Create ThermalExpansionCoefficient property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateThermalExpansionCoefficientPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateThermalExpansionCoefficientProperty, PropertyType.ThermalExpansionCoefficient);
      }

      /// <summary>Create ThermalResistance property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateThermalResistancePropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateThermalResistanceProperty, PropertyType.ThermalResistance);
      }

      /// <summary>Create ThermalTransmittance property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateThermalTransmittancePropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateThermalTransmittanceProperty, PropertyType.ThermalTransmittance);
      }

      /// <summary>Create ThermodynamicTemperature property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateThermodynamicTemperaturePropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateThermodynamicTemperatureProperty, PropertyType.ThermodynamicTemperature);
      }

      /// <summary>Create VaporPermeability property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateVaporPermeabilityPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateVaporPermeabilityProperty, PropertyType.VaporPermeability);
      }

      /// <summary>Create Volume property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateVolumePropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateVolumeProperty, PropertyType.Volume);
      }

      /// <summary>Create VolumetricFlowRate property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateVolumetricFlowRatePropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateVolumetricFlowRateProperty, PropertyType.VolumetricFlowRate);
      }

      /// <summary>Create Torque property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateTorquePropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateTorqueProperty, PropertyType.Torque);
      }

      /// <summary>Create WarpingConstant property, using a cached value if possible.</summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created or cached property handle.</returns>
      public static IFCAnyHandle CreateWarpingConstantPropertyFromCache(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericPropertyFromCache(file, propertyDescription, values, valueType, unitTypeKey, CreateWarpingConstantProperty, PropertyType.WarpingConstant);
      }


      #endregion

      #region Create___Property

      /// <summary>
      /// Create property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <param name="createMeasure">The craete measure function.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateGenericProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType,
         string unitTypeKey, Func<double, IFCData> createMeasure)
      {
         if (values == null)
            return null;

         List<IFCData> dataList = new List<IFCData>();
         foreach (var val in values)
            dataList.Add(val.HasValue ? createMeasure(val.Value) : null);
         return CreateCommonPropertyFromList(file, propertyDescription, dataList, valueType, unitTypeKey);
      }

      /// <summary>
      /// Create Area property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAreaProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsAreaMeasure);
      }

      /// <summary>
      /// Create Acceleration property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAccelerationProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsAccelerationMeasure);
      }

      /// <summary>
      /// Create AngularVelocity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAngularVelocityProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsAngularVelocityMeasure);
      }

      /// <summary>
      /// Create AreaDensity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateAreaDensityProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsAreaDensityMeasure);
      }

      /// <summary>
      /// Create DynamicViscosity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateDynamicViscosityProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsDynamicViscosityMeasure);
      }

      /// <summary>
      /// Create ElectricCurrent property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateElectricCurrentProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsElectricCurrentMeasure);
      }

      /// <summary>
      /// Create ElectricVoltage property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateElectricVoltageProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsElectricVoltageMeasure);
      }

      /// <summary>
      /// Create Energy property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateEnergyProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsEnergyMeasure);
      }

      /// <summary>
      /// Create Force property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateForceProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsForceMeasure);
      }

      /// <summary>
      /// Create Frequency property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateFrequencyProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsFrequencyMeasure);
      }

      /// <summary>
      /// Create HeatingValue property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateHeatingValueProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsHeatingValueMeasure);
      }

      /// <summary>
      /// Create Illuminance property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIlluminanceProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsIlluminanceMeasure);
      }

      /// <summary>
      /// Create IonConcentration property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIonConcentrationProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsIonConcentrationMeasure);
      }

      /// <summary>
      /// Create IsothermalMoistureCapacity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateIsothermalMoistureCapacityProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsIsothermalMoistureCapacityMeasure);
      }

      /// <summary>
      /// Create HeatFluxDensity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateHeatFluxDensityProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsHeatFluxDensityMeasure);
      }

      /// <summary>
      /// Create Length property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLengthProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsLengthMeasure);
      }

      /// <summary>
      /// Create LinearForce property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearForceProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsLinearForceMeasure);
      }

      /// <summary>
      /// Create LinearMoment property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearMomentProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsLinearMomentMeasure);
      }

      /// <summary>
      /// Create LinearStiffness property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearStiffnessProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsLinearStiffnessMeasure);
      }

      /// <summary>
      /// Create LinearVelocity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLinearVelocityProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsLinearVelocityMeasure);
      }

      /// <summary>
      /// Create LuminousFlux property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLuminousFluxProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsLuminousFluxMeasure);
      }

      /// <summary>
      /// Create LuminousIntensity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateLuminousIntensityProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsLuminousIntensityMeasure);
      }

      /// <summary>
      /// Create Mass property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsMassMeasure);
      }

      /// <summary>
      /// Create MassDensity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassDensityProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsMassDensityMeasure);
      }

      /// <summary>
      /// Create MassFlowRate property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassFlowRateProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsMassFlowRateMeasure);
      }

      /// <summary>
      /// Create MassPerLength property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMassPerLengthProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsMassPerLengthMeasure);
      }

      /// <summary>
      /// Create ModulusOfElasticity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateModulusOfElasticityProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsModulusOfElasticityMeasure);
      }

      /// <summary>
      /// Create MoistureDiffusivity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMoistureDiffusivityProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsMoistureDiffusivityMeasure);
      }

      /// <summary>
      /// Create MomentOfInertia property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateMomentOfInertiaProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsMomentOfInertiaMeasure);
      }

      /// <summary>
      /// Create SectionModulus property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSectionModulusProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsSectionModulusMeasure);
      }

      /// <summary>
      /// Create NormalisedRatio property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateNormalisedRatioProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsNormalisedRatioMeasure);
      }

      /// <summary>
      /// Create Numeric property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateNumericProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsNumeric);
      }

      /// <summary>
      /// Create PlaneAngle property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePlaneAngleProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsPlaneAngleMeasure);
      }

      /// <summary>
      /// Create PlanarForce property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePlanarForceProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsPlanarForceMeasure);
      }

      /// <summary>
      /// Create NonNegativeLength property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateNonNegativeLengthProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsNonNegativeLengthMeasure);
      }

      /// <summary>
      /// Create PositiveLength property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositiveLengthProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsPositiveLengthMeasure);
      }

      /// <summary>
      /// Create PositiveRatio property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositiveRatioProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         foreach (var val in values)
         {
            if (val.HasValue && val < MathUtil.Eps)
               return null;
         }

         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsPositiveRatioMeasure);
      }

      /// <summary>
      /// Create PositivePlaneAngle property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePositivePlaneAngleProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsPositivePlaneAngleMeasure);
      }

      /// <summary>
      /// Create Power property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePowerProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsPowerMeasure);
      }

      /// <summary>
      /// Create Pressure property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreatePressureProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsPressureMeasure);
      }

      /// <summary>
      /// Create Ratio property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRatioProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsRatioMeasure);
      }

      /// <summary>
      /// Create Real property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRealProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsReal);
      }

      /// <summary>
      /// Create RotationalFrequency property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateRotationalFrequencyProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsRotationalFrequencyMeasure);
      }

      /// <summary>
      /// Create SoundPower property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPowerProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsSoundPowerMeasure);
      }

      /// <summary>
      /// Create SoundPowerLevel property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPowerLevelProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsSoundPowerLevelMeasure);
      }

      /// <summary>
      /// Create SoundPressure property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSoundPressureProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsSoundPressureMeasure);
      }

      /// <summary>
      /// Create SpecificHeatCapacity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateSpecificHeatCapacityProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsSpecificHeatCapacityMeasure);
      }

      /// <summary>
      /// Create ThermalConductivity property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalConductivityProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsThermalConductivityMeasure);
      }

      /// <summary>
      /// Create ThermalExpansionCoefficient property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalExpansionCoefficientProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsThermalExpansionCoefficientMeasure);
      }

      /// <summary>
      /// Create ThermalResistance property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalResistanceProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsThermalResistanceMeasure);
      }

      /// <summary>
      /// Create ThermalTransmittance property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermalTransmittanceProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsThermalTransmittanceMeasure);
      }

      /// <summary>
      /// Create ThermodynamicTemperature property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateThermodynamicTemperatureProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsThermodynamicTemperatureMeasure);
      }

      /// <summary>
      /// Create VaporPermeability property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVaporPermeabilityProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsVaporPermeabilityMeasure);
      }

      /// <summary>
      /// Create Volume property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVolumeProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsVolumeMeasure);
      }

      /// <summary>
      /// Create VolumetricFlowRate property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateVolumetricFlowRateProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsVolumetricFlowRateMeasure);
      }

      /// <summary>
      /// Create Torque property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateTorqueProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsTorqueMeasure);
      }

      /// <summary>
      /// Create WarpingConstant property.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="propertyDescription">The name and description of the property.</param>
      /// <param name="values">The values of the property.</param>
      /// <param name="valueType">The value type of the property.</param>
      /// <returns>The created property handle.</returns>
      public static IFCAnyHandle CreateWarpingConstantProperty(IFCFile file, PropertyDescription propertyDescription, IList<double?> values, PropertyValueType valueType, string unitTypeKey)
      {
         return CreateGenericProperty(file, propertyDescription, values, valueType, unitTypeKey, IFCDataUtil.CreateAsWarpingConstantMeasure);
      }
      #endregion

   }
}