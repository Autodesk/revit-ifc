﻿//
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
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Exporter.PropertySet;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Provides static methods for parameter related manipulations.
   /// </summary>
   class ParameterUtil
   {
      // Cache the parameters for the current Element.
      private static IDictionary<ElementId, IDictionary<BuiltInParameterGroup, ParameterElementCache>> m_NonIFCParameters =
         new Dictionary<ElementId, IDictionary<BuiltInParameterGroup, ParameterElementCache>>();

      private static IDictionary<ElementId, ParameterElementCache> m_IFCParameters =
         new Dictionary<ElementId, ParameterElementCache>();

      private static IDictionary<ElementId, IDictionary<IFCAnyHandle, ParameterValueSubelementCache>> m_SubelementParameterValueCache =
         new Dictionary<ElementId, IDictionary<IFCAnyHandle, ParameterValueSubelementCache>>();

      public static IDictionary<BuiltInParameterGroup, ParameterElementCache> GetNonIFCParametersForElement(ElementId elemId)
      {
         if (elemId == ElementId.InvalidElementId)
            return null;

         IDictionary<BuiltInParameterGroup, ParameterElementCache> nonIFCParametersForElement = null;
         if (!m_NonIFCParameters.TryGetValue(elemId, out nonIFCParametersForElement))
         {
            CacheParametersForElement(elemId);
            m_NonIFCParameters.TryGetValue(elemId, out nonIFCParametersForElement);
         }

         return nonIFCParametersForElement;
      }

      /// <summary>
      /// Clears parameter cache.
      /// </summary>
      public static void ClearParameterCache()
      {
         m_NonIFCParameters.Clear();
         m_IFCParameters.Clear();
         m_SubelementParameterValueCache.Clear();
      }

      private static Parameter GetStringValueFromElementBase(Element element, string propertyName, bool allowUnset, out string propertyValue)
      {
         propertyValue = string.Empty;
         if (element == null)
            return null;

         if (String.IsNullOrEmpty(propertyName))
            return null;

         ElementId elementId = element.Id;

         Parameter parameter = GetParameterFromName(elementId, null, propertyName);

         if (parameter != null)
         {
            StorageType storageType = parameter.StorageType;
            if (storageType != StorageType.String && storageType != StorageType.ElementId)
               return null;

            if (parameter.HasValue)
            {
               string propValue;
               propValue = parameter.AsString();

               if (!string.IsNullOrEmpty(propValue))
               {
                  string propValuetrim = propValue.Trim();
                  // This is kind of hack to quickly check whether we need to parse the parameter or not
                  if (((propValuetrim.Length > 1 && propValuetrim[0] == '{') || (propValuetrim.Length > 2 && propValuetrim[1] == '{')) && (propValuetrim[propValuetrim.Length - 1] == '}'))
                  {
                     ParamExprResolver pResv = new ParamExprResolver(element, propertyName, propValuetrim);
                     propertyValue = pResv.GetStringValue();
                     if (string.IsNullOrEmpty(propertyValue))
                        propertyValue = propValue;    // return the original propValue (un-trimmed)
                  }
                  else
                     propertyValue = propValue;    // return the original propValue (un-trimmed)

                  return parameter;
               }
               else if (parameter.AsElementId() != null)
               {
                  propertyValue = PropertyUtil.ElementIdParameterAsString(parameter);
                  return parameter;
               }
            }

            if (allowUnset)
            {
               propertyValue = null;
               return parameter;
            }
         }

         return null;
      }

      /// <summary>
      /// Gets a non-empty string value from parameter of an element.
      /// </summary>
      /// <param name="element">The element, which can be null..</param>
      /// <param name="propertyName">The property name.</param>
      /// <param name="propertyValue">The output property value.</param>
      /// <exception cref="System.ArgumentException">Thrown when propertyName is null or empty.</exception>
      /// <returns>The parameter, or null if not found.</returns>
      public static Parameter GetStringValueFromElement(Element element, string propertyName, out string propertyValue)
      {
         return GetStringValueFromElementBase(element, propertyName, false, out propertyValue);
      }

      /// <summary>
      /// Gets integer value from parameter of an element.
      /// </summary>
      /// <param name="element">The element, which can be null.</param>
      /// <param name="propertyName">The property name.</param>
      /// <param name="propertyValue">The output property value.</param>
      /// <exception cref="System.ArgumentNullException">Thrown when element is null.</exception>
      /// <exception cref="System.ArgumentException">Thrown when propertyName is null or empty.</exception>
      /// <returns>The parameter, or null if not found.</returns>
      public static Parameter GetIntValueFromElement(Element element, string propertyName, out int propertyValue)
      {
         if (String.IsNullOrEmpty(propertyName))
            throw new ArgumentException("The name is null or empty.", "propertyName");

         propertyValue = 0;

         if (element == null)
            return null;

         Parameter parameter = GetParameterFromName(element.Id, null, propertyName);

         if (parameter != null && parameter.HasValue)
         {
            switch (parameter.StorageType)
            {
               case StorageType.Double:
                  {
                     try
                     {
                        propertyValue = (int)parameter.AsDouble();
                        return parameter;
                     }
                     catch
                     {
                        return null;
                     }
                  }
               case StorageType.Integer:
                  {
                     propertyValue = parameter.AsInteger();
                     return parameter;
                  }
               case StorageType.String:
                  {
                     string propValue;
                     propValue = parameter.AsString();

                     string propValuetrim = propValue.Trim();
                     // This is kind of hack to quickly check whether we need to parse the parameter or not
                     if (((propValuetrim.Length > 1 && propValuetrim[0] == '{') || (propValuetrim.Length > 2 && propValuetrim[1] == '{')) && (propValuetrim[propValuetrim.Length - 1] == '}'))
                     {
                        ParamExprResolver pResv = new ParamExprResolver(element, propertyName, propValuetrim);
                        int? propertyIntValue = pResv.GetIntValue();
                        if (propertyIntValue.HasValue)
                        {
                           propertyValue = propertyIntValue.Value;
                           return parameter;
                        }
                     }

                     try
                     {
                        propertyValue = Convert.ToInt32(parameter.AsString());
                        return parameter;
                     }
                     catch
                     {
                        return null;
                     }
                  }
            }
         }
         return null;
      }

      /// <summary>
      /// Gets double value from parameter of an element.
      /// </summary>
      /// <param name="element">The element, which can be null.</param>
      /// <param name="group">Optional property group to limit search to.</param>
      /// <param name="propertyName">The property name.</param>
      /// <param name="propertyValue">The output property value.</param>
      /// <exception cref="System.ArgumentNullException">Thrown when element is null.</exception>
      /// <exception cref="System.ArgumentException">Thrown when propertyName is null or empty.</exception>
      /// <returns>The parameter, or null if not found.</returns>
      public static Parameter GetDoubleValueFromElement(Element element, BuiltInParameterGroup? group, string propertyName, out double propertyValue)
      {
         if (String.IsNullOrEmpty(propertyName))
            throw new ArgumentException("It is null or empty.", "propertyName");

         propertyValue = 0.0;

         if (element == null)
            return null;

         Parameter parameter = GetParameterFromName(element.Id, group, propertyName);

         if (parameter != null && parameter.HasValue)
         {
            switch (parameter.StorageType)
            {
               case StorageType.Double:
                  propertyValue = parameter.AsDouble();
                  return parameter;
               case StorageType.Integer:
                  propertyValue = parameter.AsInteger();
                  return parameter;
               case StorageType.String:
                  {
                     string propValue;
                     propValue = parameter.AsString();

                     string propValuetrim = propValue.Trim();
                     // This is kind of hack to quickly check whether we need to parse the parameter or not
                     if (((propValuetrim.Length > 1 && propValuetrim[0] == '{') || (propValuetrim.Length > 2 && propValuetrim[1] == '{')) && (propValuetrim[propValuetrim.Length - 1] == '}'))
                     {
                        ParamExprResolver pResv = new ParamExprResolver(element, propertyName, propValuetrim);
                        double? propertyDoubleValue = pResv.GetDoubleValue();
                        if (propertyDoubleValue.HasValue)
                        {
                           propertyValue = propertyDoubleValue.Value;
                           return parameter;
                        }
                     }

                     return Double.TryParse(propValue, out propertyValue) ? parameter : null;
                  }
            }
         }

         return null;
      }

      /// <summary>
      /// Gets string value from built-in parameter of an element.
      /// </summary>
      /// <param name="element">The element, which can be null.</param>
      /// <param name="builtInParameter">The built-in parameter.</param>
      /// <param name="propertyValue">The output property value.</param>
      /// <exception cref="System.ArgumentNullException">Thrown when element is null.</exception>
      /// <exception cref="System.ArgumentException">Thrown when builtInParameter in invalid.</exception>
      /// <returns>The parameter, or null if not found.</returns>
      public static Parameter GetStringValueFromElement(Element element, BuiltInParameter builtInParameter, out string propertyValue)
      {
         if (builtInParameter == BuiltInParameter.INVALID)
            throw new ArgumentException("BuiltInParameter is INVALID", "builtInParameter");

         propertyValue = String.Empty;

         if (element == null)
            return null;

         Parameter parameter = element.get_Parameter(builtInParameter);
         if (parameter != null && parameter.HasValue)
         {
            switch (parameter.StorageType)
            {
               case StorageType.Double:
                  propertyValue = parameter.AsDouble().ToString();
                  return parameter;
               case StorageType.Integer:
                  propertyValue = parameter.AsInteger().ToString();
                  return parameter;
               case StorageType.String:
                  propertyValue = parameter.AsString();
                  return parameter;
               case StorageType.ElementId:
                  propertyValue = PropertyUtil.ElementIdParameterAsString(parameter);
                  return parameter;
            }
         }

         return null;
      }

      /// <summary>Gets string value from built-in parameter of an element or its type.</summary>
      /// <param name="element">The element, which can be null.</param>
      /// <param name="builtInParameter">The built-in parameter.</param>
      /// <param name="nullAllowed">true if we allow the property value to be empty.</param>
      /// <param name="propertyValue">The output property value.</param>
      /// <returns>The parameter, or null if not found.</returns>
      public static Parameter GetStringValueFromElementOrSymbol(Element element, BuiltInParameter builtInParameter, bool nullAllowed, out string propertyValue)
      {
         propertyValue = string.Empty;
         if (element == null)
            return null;

         Parameter parameter = GetStringValueFromElement(element, builtInParameter, out propertyValue);
         if (parameter != null)
         {
            if (!String.IsNullOrEmpty(propertyValue))
               return parameter;
         }

         parameter = null;
         Element elementType = element.Document.GetElement(element.GetTypeId());
         if (elementType != null)
         {
            parameter = GetStringValueFromElement(elementType, builtInParameter, out propertyValue);
            if ((parameter != null) && !nullAllowed && String.IsNullOrEmpty(propertyValue))
               parameter = null;
         }

         return parameter;
      }

      /// <summary>
      /// Sets string value of a built-in parameter of an element.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="builtInParameter">The built-in parameter.</param>
      /// <param name="propertyValue">The property value.</param>
      /// <exception cref="System.ArgumentNullException">Thrown when element is null.</exception>
      /// <exception cref="System.ArgumentException">Thrown when builtInParameter in invalid.</exception>
      public static void SetStringParameter(Element element, BuiltInParameter builtInParameter, string propertyValue)
      {
         if (element == null)
            throw new ArgumentNullException("element");

         if (builtInParameter == BuiltInParameter.INVALID)
            throw new ArgumentException("BuiltInParameter is INVALID", "builtInParameter");

         ElementId parameterId = new ElementId(builtInParameter);
         ExporterIFCUtils.AddValueString(element, parameterId, propertyValue);
      }

      /// <summary>
      /// Gets double value from built-in parameter of an element.
      /// </summary>
      /// <param name="element">The element, which can be null.</param>
      /// <param name="builtInParameter">The built-in parameter.</param>
      /// <param name="propertyValue">The output property value.</param>
      /// <exception cref="System.ArgumentNullException">Thrown when element is null.</exception>
      /// <exception cref="System.ArgumentException">Thrown when builtInParameter in invalid.</exception>
      /// <returns>The parameter, or null if not found.</returns>
      public static Parameter GetDoubleValueFromElement(Element element, BuiltInParameter builtInParameter, out double propertyValue)
      {
         if (builtInParameter == BuiltInParameter.INVALID)
            throw new ArgumentException("BuiltInParameter is INVALID", "builtInParameter");

         propertyValue = 0.0;

         if (element == null)
            return null;

         Parameter parameter = element.get_Parameter(builtInParameter);

         if (parameter != null && parameter.HasValue && parameter.StorageType == StorageType.Double)
         {
            propertyValue = parameter.AsDouble();
            return parameter;
         }

         return null;
      }

      /// <summary>
      /// Gets integer value from built-in parameter of an element.
      /// </summary>
      /// <param name="element">The element, which can be null.</param>
      /// <param name="builtInParameter">The built-in parameter.</param>
      /// <param name="propertyValue">The output property value.</param>
      /// <exception cref="System.ArgumentNullException">Thrown when element is null.</exception>
      /// <exception cref="System.ArgumentException">Thrown when builtInParameter in invalid.</exception>
      /// <returns>The parameter, or null if not found.</returns>
      public static Parameter GetIntValueFromElement(Element element, BuiltInParameter builtInParameter, out int propertyValue)
      {
         if (builtInParameter == BuiltInParameter.INVALID)
            throw new ArgumentException("BuiltInParameter is INVALID", "builtInParameter");

         propertyValue = 0;

         if (element == null)
            return null;

         Parameter parameter = element.get_Parameter(builtInParameter);

         if (parameter != null && parameter.HasValue && parameter.StorageType == StorageType.Integer)
         {
            propertyValue = parameter.AsInteger();
            return parameter;
         }

         return null;
      }

      /// <summary>
      /// Gets double value from built-in parameter of an element or its element type.
      /// </summary>
      /// <param name="element">The element, which can be null.</param>
      /// <param name="builtInParameter">The built-in parameter.</param>
      /// <param name="propertyValue">The output property value.</param>
      /// <returns>The parameter, or null if not found.</returns>
      public static Parameter GetDoubleValueFromElementOrSymbol(Element element, BuiltInParameter builtInParameter, out double propertyValue)
      {
         propertyValue = 0.0;
         if (element == null)
            return null;

         Parameter parameter = GetDoubleValueFromElement(element, builtInParameter, out propertyValue);
         if (parameter != null)
            return parameter;

         Document document = element.Document;
         ElementId typeId = element.GetTypeId();

         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return GetDoubleValueFromElement(elemType, builtInParameter, out propertyValue);

         return null;
      }

      /// <summary>
      /// Gets double value from parameter of an element or its element type.
      /// </summary>
      /// <param name="element">The element, which can be null.</param>
      /// <param name="propertyName">The property name.</param>
      /// <param name="propertyValue">The output property value.</param>
      /// <returns>The parameter, or null if not found.</returns>
      public static Parameter GetDoubleValueFromElementOrSymbol(Element element, string propertyName, out double propertyValue)
      {
         propertyValue = 0.0;
         if (element == null)
            return null;

         Parameter parameter = GetDoubleValueFromElement(element, null, propertyName, out propertyValue);
         if (parameter != null)
            return parameter;

         Document document = element.Document;
         ElementId typeId = element.GetTypeId();

         Element elemType = document.GetElement(typeId);
         if (elemType != null)
         {
            parameter = GetDoubleValueFromElement(elemType, null, propertyName, out propertyValue);
            if (parameter == null)
               parameter = GetDoubleValueFromElement(elemType, null, propertyName + "[Type]", out propertyValue);
         }

         return parameter;
      }

      /// <summary>
      /// Gets positive double value from parameter of an element or its element type.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="propertyName">The property name.</param>
      /// <param name="propertyValue">The output property value.</param>
      /// <returns>The parameter, or null if not found.</returns>
      public static Parameter GetPositiveDoubleValueFromElementOrSymbol(Element element, string propertyName, out double propertyValue)
      {
         Parameter parameter = GetDoubleValueFromElementOrSymbol(element, propertyName, out propertyValue);
         if ((parameter != null) && (propertyValue > 0.0))
            return parameter;
         return null;
      }

      /// <summary>
      /// Gets element id value from parameter of an element.
      /// </summary>
      /// <param name="element">The element, which can be null.</param>
      /// <param name="builtInParameter">The built in parameter.</param>
      /// <param name="propertyValue">The output property value.</param>
      /// <returns>The parameter, or null if not found.</returns>
      public static Parameter GetElementIdValueFromElement(Element element, BuiltInParameter builtInParameter, out ElementId propertyValue)
      {
         if (builtInParameter == BuiltInParameter.INVALID)
            throw new ArgumentException("BuiltInParameter is INVALID", "builtInParameter");

         propertyValue = ElementId.InvalidElementId;

         if (element == null)
            return null;

         Parameter parameter = element.get_Parameter(builtInParameter);
         if (parameter != null && parameter.HasValue && parameter.StorageType == StorageType.ElementId)
         {
            propertyValue = parameter.AsElementId();
            return parameter;
         }

         return null;
      }

      /// <summary>
      /// Gets element id value from parameter of an element or its element type.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="builtInParameter">The built in parameter.</param>
      /// <param name="propertyValue">The output property value.</param>
      /// <returns>The parameter, or null if not found.</returns>
      public static Parameter GetElementIdValueFromElementOrSymbol(Element element, BuiltInParameter builtInParameter, out ElementId propertyValue)
      {
         propertyValue = ElementId.InvalidElementId;
         if (element == null)
            return null;

         Parameter parameter = GetElementIdValueFromElement(element, builtInParameter, out propertyValue);
         if (parameter != null)
            return parameter;

         Document document = element.Document;
         ElementId typeId = element.GetTypeId();

         Element elemType = document.GetElement(typeId);
         if (elemType != null)
            return GetElementIdValueFromElement(elemType, builtInParameter, out propertyValue);

         return null;
      }

      /// <summary>
      /// Return a list of material ids from element's parameters
      /// </summary>
      /// <param name="element">the element</param>
      /// <returns>list of material ids</returns>
      public static IList<ElementId> FindMaterialParameters(Element element)
      {
         IList<ElementId> materialIds = new List<ElementId>();

         foreach (Parameter param in element.Parameters)
         {
            // Limit to the parameter(s) within builtin parameter group PG_MATERIALS
            if (param.Definition.ParameterType == ParameterType.Material && param.Definition.ParameterGroup == BuiltInParameterGroup.PG_MATERIALS)
            {
               materialIds.Add(param.AsElementId());
            }
         }

         return materialIds;
      }

      /// <summary>
      /// Gets the parameter by name from an element from the parameter cache.
      /// </summary>
      /// <param name="elementId">The element id.</param>
      /// <param name="propertyName">The property name.</param>
      /// <returns>The Parameter.</returns>
      static private Parameter getParameterByNameFromCache(ElementId elementId, string propertyName)
      {
         Parameter parameter = null;
         string cleanPropertyName = NamingUtil.RemoveSpaces(propertyName);

         if (m_IFCParameters[elementId].ParameterCache.TryGetValue(cleanPropertyName, out parameter))
            return parameter;

         foreach (ParameterElementCache otherCache in m_NonIFCParameters[elementId].Values)
         {
            if (otherCache.ParameterCache.TryGetValue(cleanPropertyName, out parameter))
               return parameter;
         }

         return parameter;
      }

      /// <summary>
      /// Gets the parameter by name from an element from the parameter cache.
      /// </summary>
      /// <param name="elementId">The element id.</param>
      /// <param name="group">The parameter group.</param>
      /// <param name="propertyName">The property name.</param>
      /// <returns>The Parameter.</returns>
      static private Parameter getParameterByNameFromCache(ElementId elementId, BuiltInParameterGroup group,
          string propertyName)
      {
         Parameter parameter = null;
         string cleanPropertyName = NamingUtil.RemoveSpaces(propertyName);

         if (group == BuiltInParameterGroup.PG_IFC)
         {
            m_IFCParameters[elementId].ParameterCache.TryGetValue(cleanPropertyName, out parameter);
            return null;
         }

         ParameterElementCache otherCache = null;
         m_NonIFCParameters[elementId].TryGetValue(group, out otherCache);
         if (otherCache != null)
            otherCache.ParameterCache.TryGetValue(cleanPropertyName, out parameter);

         return parameter;
      }

      /// <summary>
      /// Gets the parameter value by name from the subelement parameter value cache.
      /// </summary>
      /// <param name="elementId">The element id.</param>
      /// <param name="handle">The subelement ifc handle.</param>
      /// <param name="propertyName">The property name.</param>
      /// <returns>The Parameter.</returns>
      static public ParameterValue getParameterValueByNameFromSubelementCache(ElementId elementId, IFCAnyHandle subelementHandle, string propertyName)
      {
         ParameterValue parameterVal = null;
         string cleanPropertyName = NamingUtil.RemoveSpaces(propertyName);

         IDictionary<IFCAnyHandle, ParameterValueSubelementCache> anyHandleParamValMap;
         if (!m_SubelementParameterValueCache.TryGetValue(elementId, out anyHandleParamValMap))
            return parameterVal;

         ParameterValueSubelementCache paramValueCache;
         if (!anyHandleParamValMap.TryGetValue(subelementHandle, out paramValueCache))
            return parameterVal;


         paramValueCache.ParameterValueCache.TryGetValue(cleanPropertyName, out parameterVal);
         return parameterVal;
      }

      /// <summary>
      /// Returns true if the built-in parameter has the identical name and value as another parameter.
      /// Used to remove redundant output from the IFC export.
      /// </summary>
      /// <param name="parameter">The parameter</param>
      /// <returns>Returns true if the built-in parameter has the identical name and value as another parameter.</returns>
      static private bool IsDuplicateParameter(Parameter parameter)
      {
         if (parameter.Id.IntegerValue == (int)BuiltInParameter.ELEM_CATEGORY_PARAM_MT) // Same as ELEM_CATEGORY_PARAM.
            return true;
         // DPART_ORIGINAL_CATEGORY_ID is the string version of DPART_ORIGINAL_CATEGORY_ID.  Not going to duplicate the data.
         if (parameter.Id.IntegerValue == (int)BuiltInParameter.DPART_ORIGINAL_CATEGORY)
            return true;
         return false;
      }

      /// <summary>
      /// Maps built-in parameter ids to the supported ids.  In general, this is an identity mapping, except for special
      /// cases identified in the private function IsDuplicateParameter.
      /// </summary>
      /// <param name="parameterId">The original parameter id.</param>
      /// <returns>The supported parameter id.</returns>
      static public ElementId MapParameterId(ElementId parameterId)
      {
         switch (parameterId.IntegerValue)
         {
            case ((int)BuiltInParameter.ELEM_CATEGORY_PARAM_MT):
               return new ElementId(BuiltInParameter.ELEM_CATEGORY_PARAM);
            case ((int)BuiltInParameter.DPART_ORIGINAL_CATEGORY):
               return new ElementId(BuiltInParameter.DPART_ORIGINAL_CATEGORY_ID);
         }
         return parameterId;
      }

      /// <summary>
      /// Cache the parameters for an element, allowing quick access later.
      /// </summary>
      /// <param name="id">The element id.</param>
      static private void CacheParametersForElement(ElementId id)
      {
         if (id == ElementId.InvalidElementId)
            return;

         if (m_NonIFCParameters.ContainsKey(id))
            return;

         IDictionary<BuiltInParameterGroup, ParameterElementCache> nonIFCParameters = new SortedDictionary<BuiltInParameterGroup, ParameterElementCache>();
         ParameterElementCache ifcParameters = new ParameterElementCache();

         m_NonIFCParameters[id] = nonIFCParameters;
         m_IFCParameters[id] = ifcParameters;

         Element element = ExporterCacheManager.Document.GetElement(id);
         if (element == null)
            return;

         ParameterSet parameterIds = element.Parameters;
         if (parameterIds.Size == 0)
            return;

         // We will do two passes.  In the first pass, we will look at parameters in the IFC group.
         // In the second pass, we will look at all other groups.
         ParameterSetIterator parameterIt = parameterIds.ForwardIterator();

         while (parameterIt.MoveNext())
         {
            Parameter parameter = parameterIt.Current as Parameter;
            if (parameter == null)
               continue;

            if (IsDuplicateParameter(parameter))
               continue;

            Definition paramDefinition = parameter.Definition;
            if (paramDefinition == null)
               continue;

            // Don't cache parameters that aren't visible to the user.
            InternalDefinition internalDefinition = paramDefinition as InternalDefinition;
            if (internalDefinition != null && internalDefinition.Visible == false)
               continue;

            if (string.IsNullOrWhiteSpace(paramDefinition.Name))
               continue;

            string cleanPropertyName = NamingUtil.RemoveSpaces(paramDefinition.Name);

            BuiltInParameterGroup groupId = paramDefinition.ParameterGroup;
            ParameterElementCache cacheForGroup = null;

            if (groupId != BuiltInParameterGroup.PG_IFC)
            {
               if (!nonIFCParameters.TryGetValue(groupId, out cacheForGroup))
               {
                  cacheForGroup = new ParameterElementCache();
                  nonIFCParameters[groupId] = cacheForGroup;
               }
            }
            else
            {
               cacheForGroup = ifcParameters;
            }

            if (cacheForGroup != null)
            {
               // We may have situations (due to bugs) where a parameter with the same name appears multiple times.
               // In this case, we will preserve the first parameter with a value.
               // Note that this can still cause inconsistent behavior in the case where multiple parameters with the same
               // name have values, and we should warn about that when we start logging.
               if (!cacheForGroup.ParameterCache.ContainsKey(cleanPropertyName) ||
                  !cacheForGroup.ParameterCache[cleanPropertyName].HasValue)
                  cacheForGroup.ParameterCache[cleanPropertyName] = parameter;
            }
         }
      }

      /// <summary>
      /// Cache the parameters for an element's subelement (subelementHandle), allowing quick access later.
      /// </summary>
      /// <param name="elementId">The element id.</param>
      /// <param name="subelementHandle">The subelement ifc handle.</param>
      /// <param name="param">The element's parameter that we want to override.</param>
      /// <param name="paramVal">The override value.</param>
      static public void CacheParameterValuesForSubelementHandle(ElementId elementId, IFCAnyHandle subelementHandle, Parameter param, ParameterValue paramVal)
      {
         if ((elementId == ElementId.InvalidElementId) ||
             (subelementHandle == null) ||
             (param == null) ||
             (paramVal == null))
            return;

         if (IsDuplicateParameter(param))
            return;

         Definition paramDefinition = param.Definition;
         if (paramDefinition == null)
            return;

         // Don't cache parameters that aren't visible to the user.
         InternalDefinition internalDefinition = paramDefinition as InternalDefinition;
         if (internalDefinition != null && internalDefinition.Visible == false)
            return;

         if (string.IsNullOrWhiteSpace(paramDefinition.Name))
            return;

         string cleanPropertyName = NamingUtil.RemoveSpaces(paramDefinition.Name);

         IDictionary<IFCAnyHandle, ParameterValueSubelementCache> anyHandleParamValMap;
         if (!m_SubelementParameterValueCache.TryGetValue(elementId, out anyHandleParamValMap))
         {
            anyHandleParamValMap = new Dictionary<IFCAnyHandle, ParameterValueSubelementCache>();
            m_SubelementParameterValueCache[elementId] = anyHandleParamValMap;
         }

         ParameterValueSubelementCache paramCache;
         if (!anyHandleParamValMap.TryGetValue(subelementHandle, out paramCache))
         {
            paramCache = new ParameterValueSubelementCache();
            anyHandleParamValMap[subelementHandle] = paramCache;
         }

         ParameterValue cachedParamVal;
         if (paramCache.ParameterValueCache.TryGetValue(cleanPropertyName, out cachedParamVal))
            return;

         paramCache.ParameterValueCache[cleanPropertyName] = paramVal;
      }

      /// <summary>
      /// Remove an element from the parameter cache, to save space.
      /// </summary>
      /// <param name="element">The element to be used.</param>
      /// <remarks>Generally speaking, we expect to need to access an element's parameters in one pass (this is not true
      /// for types, which could get accessed repeatedly).  As such, we are wasting space keeping an element's parameters cached
      /// after it has already been exported.</remarks>
      static public void RemoveElementFromCache(Element element)
      {
         if (element == null)
            return;

         ElementId id = element.Id;
         m_NonIFCParameters.Remove(id);
         m_IFCParameters.Remove(id);
         m_SubelementParameterValueCache.Remove(id);
      }

      /// <summary>
      /// Gets the parameter by name from an element for a specific parameter group.
      /// </summary>
      /// <param name="elemId">The element id.</param>
      /// <param name="group">The optional parameter group.</param>
      /// <param name="propertyName">The property name.</param>
      /// <returns>The Parameter.</returns>
      internal static Parameter GetParameterFromName(ElementId elemId, BuiltInParameterGroup? group, string propertyName)
      {
         if (!m_IFCParameters.ContainsKey(elemId))
            CacheParametersForElement(elemId);

         return group.HasValue ?
             getParameterByNameFromCache(elemId, group.Value, propertyName) :
             getParameterByNameFromCache(elemId, propertyName);
      }

      private static Parameter GetStringValueFromElementOrSymbolBase(Element element, string propertyName, bool allowUnset, out string propertyValue)
      {
         Parameter parameter = GetStringValueFromElementBase(element, propertyName, allowUnset, out propertyValue);
         if (parameter != null)
         {
            if (!string.IsNullOrEmpty(propertyValue))
               return parameter;
         }

         Element elementType = element.Document.GetElement(element.GetTypeId());
         if (elementType != null)
         {
            parameter = GetStringValueFromElementBase(elementType, propertyName, allowUnset, out propertyValue);
            if (parameter == null)
            {
               parameter = GetStringValueFromElementBase(elementType, propertyName + "[Type]", allowUnset, out propertyValue);
            }
         }
         return parameter;
      }

      /// <summary>
      /// Gets string value from parameter of an element or its element type.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="propertyName">The property name.</param>
      /// <param name="propertyValue">The output property value.</param>
      /// <returns>The parameter, or null if not found.</returns>
      public static Parameter GetStringValueFromElementOrSymbol(Element element, string propertyName, out string propertyValue)
      {
         return GetStringValueFromElementOrSymbolBase(element, propertyName, false, out propertyValue);
      }

      /// <summary>
      /// Gets string value from parameter of an element or its element type, which is allowed to be optional.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="propertyName">The property name.</param>
      /// <param name="propertyValue">The output property value.</param>
      /// <returns>The parameter, or null if not found.</returns>
      public static Parameter GetOptionalStringValueFromElementOrSymbol(Element element, string propertyName, out string propertyValue)
      {
         return GetStringValueFromElementOrSymbolBase(element, propertyName, true, out propertyValue);
      }

      /// <summary>
      /// Gets integer value from parameter of an element or its element type.
      /// </summary>
      /// <param name="element">The element, which can be null.</param>
      /// <param name="propertyName">The property name.</param>
      /// <param name="propertyValue">The output property value.</param>
      /// <returns>The parameter, or null if not found.</returns>
      public static Parameter GetIntValueFromElementOrSymbol(Element element, string propertyName, out int propertyValue)
      {
         propertyValue = 0;
         if (element == null)
            return null;

         Parameter parameter = GetIntValueFromElement(element, propertyName, out propertyValue);
         if (parameter != null)
            return parameter;

         Document document = element.Document;
         ElementId typeId = element.GetTypeId();

         Element elemType = document.GetElement(typeId);
         if (elemType != null)
         {
            parameter = GetIntValueFromElement(elemType, propertyName, out propertyValue);
            if (parameter == null)
               parameter = GetIntValueFromElement(elemType, propertyName + "[Type]", out propertyValue);
         }

         return parameter;
      }

      /// <summary>
      /// This method returns a special parameter for Offset found in the FamilySymbol that influence the CurtainWall Panel position.
      /// </summary>
      /// <param name="the familySymbol"></param>
      /// <returns>maximum Offset value if there are more than one parameters of the same name</returns>
      public static double GetSpecialOffsetParameter(FamilySymbol familySymbol)
      {
         // This method is isolated here so that it can adopt localized parameter name as necessary

         string offsetParameterName = "Offset";
         double maxOffset = 0.0;

         // In case there are more than one parameter of the same name, we will get one value that is the largest
         IList<Parameter> offsetParams = familySymbol.GetParameters(offsetParameterName);
         foreach (Parameter offsetP in offsetParams)
         {
            double offset = offsetP.AsDouble();
            if (offset > maxOffset)
               maxOffset = offset;
         }
         return maxOffset;
      }

      /// <summary>
      /// This method returns a special parameter for Material Thickness found in the FamilySymbol that influence the CurtainWall Panel thickness.
      /// </summary>
      /// <param name="familySymbol">the familySymbol</param>
      /// <returns>thickness</returns>
      public static double GetSpecialThicknessParameter(FamilySymbol familySymbol)
      {
         // This method is isolated here so that it can adopt localized parameter name as necessary

         string thicknessParameterName = "Thickness";
         double thickestValue = 0.0;

         IList<Parameter> thicknessParams = familySymbol.GetParameters(thicknessParameterName);

         foreach (Parameter thicknessP in thicknessParams)
         {
            // If happens there are more than 1 param with the same name, we will arbitrary choose the thickest value
            double thickness = thicknessP.AsDouble();
            if (thickness > thickestValue)
               thickestValue = thickness;
         }
         return thickestValue;
      }

      /// <summary>
      /// Get override containment value through a parameter "IfcSpatialContainer" or "OverrideElementContainer". Value can be "IFCSITE", "IFCBUILDING", or the appropriate Level name
      /// </summary>
      /// <param name="element">the element</param>
      /// <param name="overrideContainerHnd">the override container Handle</param>
      /// <returns>true if there is override</returns>
      public static ElementId OverrideContainmentParameter(ExporterIFC exporterIFC, Element element, out IFCAnyHandle overrideContainerHnd)
      {
         ElementId containerElemId = ElementId.InvalidElementId;
         // Special case whether an object should be assigned to the Site or Building container
         overrideContainerHnd = null;
         string containerOverrideName = null;
         if (ParameterUtil.GetStringValueFromElement(element, "OverrideElementContainer", out containerOverrideName) == null)
            ParameterUtil.GetStringValueFromElement(element, "IfcSpatialContainer", out containerOverrideName);
         if (!string.IsNullOrEmpty(containerOverrideName))
         {
            if (containerOverrideName.Equals("IFCSITE", StringComparison.CurrentCultureIgnoreCase))
            {
               overrideContainerHnd = ExporterCacheManager.SiteHandle;
               return containerElemId;
            }
            else if (containerOverrideName.Equals("IFCBUILDING", StringComparison.CurrentCultureIgnoreCase))
            {
               overrideContainerHnd = ExporterCacheManager.BuildingHandle;
               return containerElemId;
            }

            // Find Level that is designated as the override by iterating through all the Levels for the name match
            FilteredElementCollector collector = new FilteredElementCollector(element.Document);
            ICollection<Element> collection = collector.OfClass(typeof(Level)).ToElements();
            foreach (Element level in collection)
            {
               if (level.Name.Equals(containerOverrideName, StringComparison.CurrentCultureIgnoreCase))
               {
                  containerElemId = level.Id;
                  break;
               }
            }
            if (containerElemId != ElementId.InvalidElementId)
            {
               IFCLevelInfo levelInfo = ExporterCacheManager.LevelInfoCache.GetLevelInfo(exporterIFC, containerElemId);
               if (levelInfo != null)
                  overrideContainerHnd = levelInfo.GetBuildingStorey();
               if (overrideContainerHnd != null)
                  return containerElemId;
            }
         }

         return containerElemId;
      }
   }
}