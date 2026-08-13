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
using Revit.IFC.Common.Utility;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Provides static methods for parameter related manipulations.
   /// </summary>
   public class ParameterUtil
   {
      /// <summary>
      /// The information needed to create an IFC property.
      /// </summary>
      public class PropertyDescription
      {
         /// <summary>
         /// Create a property description with only a name.
         /// </summary>
         /// <param name="name">The name of the property.</param>
         public PropertyDescription(string name)
         {
            Name = name;
         }

         /// <summary>
         /// Create a property description with a name and description.
         /// </summary>
         /// <param name="name">The name of the property.</param>
         /// <param name="description">The description of the property.</param>
         public PropertyDescription(string name, string description)
         {
            Name = name;
            Description = description;
         }

         /// <summary>
         ///  The required name.
         /// </summary>
         public string Name { get; set; } = null;

         /// <summary>
         /// The optional description.
         /// </summary>
         public string Description { get; set; } = null;
      };

      // Cache the parameters for the current Element.
      private static Dictionary<ElementId, ParameterElementCache> Parameters = [];

      private static Dictionary<ElementId, IDictionary<IFCAnyHandle, ParameterValueSubelementCache>> SubelementParameterValueCache = [];

      /// <summary>
      /// Clears the parameter value caches.
      /// </summary>
      public static void ClearParameterValueCaches()
      {
         Parameters.Clear();
         SubelementParameterValueCache.Clear();
      }

      /// <summary>
      /// Gets a non-empty string value from parameter of an element.
      /// </summary>
      /// <param name="element">The element, which can be null..</param>
      /// <param name="allowUnset">If set to true, allows unset values.</param>
      /// <param name="propertyNames">The possible property names.</param>
      /// <exception cref="System.ArgumentException">Thrown when propertyName is null or empty.</exception>
      /// <returns>The parameter and value, or null if not found.</returns>
      public static (EvaluatedParameter, string) GetStringValueFromElement(Element element, bool allowUnset, params string[] propertyNames)
      {
         if (element == null)
            return (null, null);

         ElementId elementId = element.Id;
         bool isType = element is ElementType;
         
         EvaluatedParameter parameter = null;
         string usedPropertyName = null;
         
         foreach (string propertyName in propertyNames)
         {
            if (string.IsNullOrEmpty(propertyName))
               continue;

            EvaluatedParameter possibleParameter = GetParameterFromName(elementId, propertyName, isType);
            if (possibleParameter == null)
               continue;

            parameter ??= possibleParameter;
            if (parameter.HasValue)
            {
               usedPropertyName = propertyName;
               break;
            }
         }

         if (parameter == null || usedPropertyName == null)
            return (null, null);

         string propertyValue = null;
         if (parameter.HasValue)
         {
            StorageType storageType = parameter.StorageType;
            switch (storageType)
            {
               case StorageType.String:
                  propertyValue = ParamExprResolver.EvaluateStringParameterExpr(element, (parameter.Value as StringParameterValue).Value, 
                     usedPropertyName);
                  break;
               case StorageType.ElementId:
                  if (!MathUtil.IsInvalidElementId((parameter.Value as ElementIdParameterValue).Value))
                     propertyValue = parameter.AsValueString(ExporterCacheManager.Document);
                  break;
               default:
                  propertyValue = parameter.AsValueString(ExporterCacheManager.Document);
                  break;
            }
         }

         if (!string.IsNullOrEmpty(propertyValue))
            return (parameter, propertyValue);

         if (!allowUnset)
            return (null, null);

         return (parameter, null);
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
      public static (EvaluatedParameter, int) GetIntValueFromElement(Element element, params string[] propertyNames)
      {
         if (element == null)
            return (null, 0);

         ElementId elementId = element.Id;
         bool isType = element is ElementType;
         foreach (string propertyName in propertyNames)
         {
            if (string.IsNullOrEmpty(propertyName))
               continue;

            EvaluatedParameter parameter = GetParameterFromName(elementId, propertyName, isType);
            if (!(parameter?.HasValue ?? false))
               continue;

            switch (parameter.StorageType)
            {
               case StorageType.Double:
                  {
                     try
                     {
                        return (parameter, (int)(parameter.Value as DoubleParameterValue).Value);
                     }
                     catch
                     {
                        continue;
                     }
                  }
               case StorageType.Integer:
                  {
                     return (parameter, (parameter.Value as IntegerParameterValue).Value);
                  }
               case StorageType.String:
                  {
                     string propValue = (parameter.Value as StringParameterValue).Value;
                     int? evalPropertyValue = ParamExprResolver.EvaluateIntegerParameterExpr(element, propValue, propertyName);
                     if (evalPropertyValue.HasValue)
                     {
                        return (parameter, evalPropertyValue.Value);
                     }
                     break;
                  }
            }
         }

         return (null, 0);
      }

      /// <summary>
      /// Get an EvaluatedParameter and its double value from an element.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="propertyNames">The list of property names to check.</param>
      /// <returns>The evaluated parameter and the double value.</returns>
      public static (EvaluatedParameter, double) GetDoubleValueFromElement(Element element, params string[] propertyNames)
      {
         if (element == null)
            return (null, 0.0);

         ElementId elementId = element.Id;
         bool isType = element is ElementType;

         foreach (string propertyName in propertyNames)
         {
            if (string.IsNullOrEmpty(propertyName))
               continue;

            EvaluatedParameter parameter = GetParameterFromName(elementId, propertyName, isType);
            if (parameter == null)
               continue;
            
            parameter = GetParameterValue(element, parameter, propertyName, out double propertyValue);
            if (parameter == null)
               continue;

            return (parameter, propertyValue);
         }

         return (null, 0.0);
      }

      public static double? TryGetDoubleValueFromElement(Element element, params string[] propertyNames)
      {
         (EvaluatedParameter parameter, double propertyValue) = GetDoubleValueFromElement(element, propertyNames);
         return parameter != null ? (double?)propertyValue : null;
      }

      public static double? GetDoubleValueFromElement(Element element, ForgeTypeId group, string propertyName)
      {
         if (string.IsNullOrEmpty(propertyName) || element == null)
            return null;

         EvaluatedParameter parameter = GetParameterFromNameAndGroup(element.Id, group, propertyName, element is ElementType);
         if (parameter == null)
            return null;

         GetParameterValue(element, parameter, propertyName, out double propertyValue);
         return propertyValue;
      }

      private static EvaluatedParameter GetParameterValue(Element element, EvaluatedParameter parameter, string propertyName, out double propertyValue)
      {
         propertyValue = 0.0;
         if (!(parameter?.HasValue ?? false))
            return null;

         switch (parameter.StorageType)
         {
            case StorageType.Double:
               propertyValue = (parameter.Value as DoubleParameterValue).Value;
               return parameter;
            case StorageType.Integer:
               propertyValue = (parameter.Value as IntegerParameterValue).Value;
               return parameter;
            case StorageType.String:
               string propValue = (parameter.Value as StringParameterValue).Value;
               double? resVal = ParamExprResolver.EvaluateDoubleParameterExpr(element,propValue, propertyName);
               if (!resVal.HasValue)
                  return null;
               
               propertyValue = resVal.Value;
               return parameter;
         }

         return null;
      }

      /// <summary>
      /// Gets string value from built-in parameter of an element.
      /// </summary>
      /// <param name="element">The element, which can be null.</param>
      /// <param name="builtInParameter">The built-in parameter.</param>
      /// <exception cref="System.ArgumentNullException">Thrown when element is null.</exception>
      /// <exception cref="System.ArgumentException">Thrown when builtInParameter in invalid.</exception>
      /// <returns>The parameter and value, or null if not found.</returns>
      public static (Parameter, string) GetStringValueFromElement(Element element, BuiltInParameter builtInParameter)
      {
         if (builtInParameter == BuiltInParameter.INVALID)
            return (null, null);

         Parameter parameter = element?.get_Parameter(builtInParameter);
         if (!(parameter?.HasValue ?? false))
            return (null, null);
         
         return (parameter, parameter.AsValueString());
      }

      /// <summary>Gets string value from built-in parameter of an element or its type.</summary>
      /// <param name="element">The element, which can be null.</param>
      /// <param name="elementType">The element, which can be null.  It will be calculated from the element if it is.</param>
      /// <param name="builtInParameter">The built-in parameter.</param>
      /// <param name="nullAllowed">true if we allow the property value to be empty.</param>
      /// <returns>The value, or null if not found.</returns>
      public static string GetStringValueFromElementOrSymbol(Element element, Element elementType, bool nullAllowed, BuiltInParameter builtInParameter)
      {
         if (element == null)
            return null;

         (_, string propertyValue) = GetStringValueFromElement(element, builtInParameter);
         if (!string.IsNullOrEmpty(propertyValue))
            return propertyValue;

         if (elementType == null && !(element is ElementType))
            elementType = element.Document.GetElement(element.GetTypeId());

         if (elementType == null)
            return null;

         (_, propertyValue) = GetStringValueFromElement(elementType, builtInParameter);
         if (!nullAllowed && string.IsNullOrEmpty(propertyValue))
            return null;

         return propertyValue;
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

         Parameter parameter = element.get_Parameter(builtInParameter);
         if (parameter != null &&
            parameter.HasValue &&
            parameter.StorageType == StorageType.String)
         {
            if (!parameter.IsReadOnly)
               parameter.Set(propertyValue);
            return;
         }

         ElementId parameterId = new ElementId(builtInParameter);
         ExporterIFCUtils.AddValueString(element, parameterId, propertyValue);
      }

      /// <summary>
      /// Gets double value from built-in parameter of an element.
      /// </summary>
      /// <param name="elementId">The element, which can be invalidElementId.</param>
      /// <param name="builtInParameter">The built-in parameter.</param>
      /// <exception cref="System.ArgumentNullException">Thrown when element is null.</exception>
      /// <exception cref="System.ArgumentException">Thrown when builtInParameter in invalid.</exception>
      /// <returns>The parameter and its value, or (null, 0.0) if not found.</returns>
      public static (EvaluatedParameter, double) GetDoubleValueFromElement(ElementId elementId, BuiltInParameter builtInParameter)
      {
         if (builtInParameter == BuiltInParameter.INVALID)
            return (null, 0.0);

         if (MathUtil.IsInvalidElementId(elementId))
            return (null, 0.0);

         EvaluatedParameter parameter = ExporterCacheManager.ParameterAccess?.GetParameter(elementId, new ElementId(builtInParameter));
         if (!(parameter?.HasValue ?? false) || parameter.StorageType != StorageType.Double)
            return (null, 0.0);
         
         double propertyValue = (parameter.Value as DoubleParameterValue).Value;
         return (parameter, propertyValue);
      }

      public static double? TryGetDoubleValueFromElement(ElementId elementId, BuiltInParameter builtInParameter)
      {
         (EvaluatedParameter parameter, double value) = GetDoubleValueFromElement(elementId, builtInParameter);
         return (parameter != null) ? value : null;
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
      public static (Parameter, int) GetIntValueFromElement(Element element, BuiltInParameter builtInParameter)
      {
         if (builtInParameter == BuiltInParameter.INVALID)
            return (null, 0);

         Parameter parameter = element?.get_Parameter(builtInParameter);
         if (!(parameter?.HasValue ?? false) || parameter.StorageType != StorageType.Integer)
            return (null, 0);

         return (parameter, parameter.AsInteger());
      }

      /// <summary>
      /// Gets double value from parameter of an element or its element type.
      /// </summary>
      /// <param name="element">The element, which can be null.</param>
      /// <param name="builtInParameter">The built-in parameter.</param>
      /// <param name="propertyValue">The output property value.</param>
      /// <returns>The parameter, or null if not found.</returns>
      public static (EvaluatedParameter, double) GetDoubleValueFromElementOrSymbol(Element element, BuiltInParameter builtInParameter)
      {
         if (element == null)
            return (null, 0.0);

         ElementId elementId = element.Id;
         (EvaluatedParameter parameter, double propertyValue) = GetDoubleValueFromElement(elementId, builtInParameter);
         if (parameter != null)
            return (parameter, propertyValue);

         ElementId elemTypeId = element.GetTypeId();
         if (!MathUtil.IsInvalidElementId(elemTypeId))
            return GetDoubleValueFromElement(elemTypeId, builtInParameter);

         return (null, 0.0);
      }

      /// <summary>
      /// Gets double value from parameter of an element or its element type.
      /// </summary>
      /// <param name="element">The element, which can be null.</param>
      /// <param name="propertyName">The property name.</param>
      /// <param name="propertyValue">The output property value.</param>
      /// <param name="alternateNames">the variable array of alternate names mainly to support backward compatibility</param>
      /// <returns>The parameter, or null if not found.</returns>
      public static (EvaluatedParameter, double) GetDoubleValueFromElementOrSymbol(Element element, params string[] propertyNames)
      {
         if (element == null)
            return (null, 0.0);

         (EvaluatedParameter parameter, double propertyValue) = GetDoubleValueFromElement(element, propertyNames);
         if (parameter != null)
            return (parameter, propertyValue);

         bool isType = element is ElementType;
         Element elementType = isType ? null : ExporterCacheManager.Document.GetElement(element.GetTypeId());
         if (elementType == null)
            return (null, 0.0);

         (parameter, propertyValue) = GetDoubleValueFromElement(elementType, propertyNames);
         if (parameter != null)
            return (parameter, propertyValue);
         
         foreach (string propertyName in propertyNames)
         {
            if (string.IsNullOrEmpty(propertyName))
               continue;

            (parameter, propertyValue) = GetDoubleValueFromElement(elementType, propertyName + "[Type]");
            if (parameter != null)
               return (parameter, propertyValue);
         }

         return (null, 0.0);
      }

      public static double? TryGetDoubleValueFromElementOrSymbol(Element element, params string[] propertyNames)
      {
         (EvaluatedParameter parameter, double value) = GetDoubleValueFromElementOrSymbol(element, propertyNames);
         return parameter != null ? value : null;
      }

      /// <summary>
      /// Gets positive double value from parameter of an element or its element type.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="propertyNames">The variable array of property names mainly to support backward compatibility</param>
      /// <returns>The parameter and its value, or (null, 0.0) if not found.</returns>
      public static double? GetPositiveDoubleValueFromElementOrSymbol(Element element, params string[] propertyNames)
      {
         (EvaluatedParameter parameter, double value) = GetDoubleValueFromElementOrSymbol(element, propertyNames);
         return parameter != null && value > 0.0 ? value : null;
      }

      /// <summary>
      /// Gets element id value from parameter of an element.
      /// </summary>
      /// <param name="element">The element, which can be null.</param>
      /// <param name="builtInParameter">The built in parameter.</param>
      /// <param name="propertyValue">The output property value.</param>
      /// <returns>The element id, or null if not found.</returns>
      public static ElementId GetElementIdValueFromElement(Element element, BuiltInParameter builtInParameter)
      {
         if (builtInParameter == BuiltInParameter.INVALID)
            return ElementId.InvalidElementId;

         Parameter parameter = element?.get_Parameter(builtInParameter);
         if (!(parameter?.HasValue ?? false) || parameter.StorageType != StorageType.ElementId)
            return ElementId.InvalidElementId;

         return parameter.AsElementId() ?? ElementId.InvalidElementId;
      }

      /// <summary>
      /// Gets element id value from parameter of an element or its element type.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="elementType">The optional element type.</param>
      /// <param name="builtInParameter">The built in parameter.</param>
      /// <param name="propertyValue">The output property value.</param>
      /// <returns>The element id, or null if not found.</returns>
      public static ElementId GetElementIdValueFromElementOrSymbol(Element element, Element elementType, BuiltInParameter builtInParameter)
      {
         if (element == null)
            return ElementId.InvalidElementId;

         ElementId propertyValue = GetElementIdValueFromElement(element, builtInParameter);
         if (!MathUtil.IsInvalidElementId(propertyValue))
            return propertyValue;

         elementType ??= element.Document.GetElement(element.GetTypeId());
         return (elementType != null) ? GetElementIdValueFromElement(elementType, builtInParameter) : ElementId.InvalidElementId;
      }

      /// <summary>
      /// Return a list of material ids from element's parameters
      /// </summary>
      /// <param name="element">the element</param>
      /// <returns>list of material ids</returns>
      public static IList<ElementId> FindMaterialParameters(ElementId elementId, bool isType)
      {
         if (MathUtil.IsInvalidElementId(elementId))
            return [];

         List<ElementId> materialIds = [];

         ParameterElementCache cache = GetCachedParametersForElement(elementId, isType);
         foreach (IList<ParameterElementInfo> parameterIds in cache.ParameterIdCache.Values)
         {
            foreach (ParameterElementInfo info in parameterIds)
            {
               if (info.Details.GroupTypeId != GroupTypeId.Materials)
                  continue;

               if (info.Details.DataType != SpecTypeId.Reference.Material)
                  continue;

               EvaluatedParameter parameter = ExporterCacheManager.ParameterAccess.GetParameter(elementId, info.ElementId);
               ElementId matId = (parameter?.Value as ElementIdParameterValue)?.Value;
               if (MathUtil.IsInvalidElementId(matId))
                  continue;
                  
               materialIds.Add(matId);
            }
         }

         return materialIds;
      }

      /// <summary>
      /// Gets the parameter value by name from the subelement parameter value cache.
      /// </summary>
      /// <param name="elementId">The element id.</param>
      /// <param name="handle">The subelement ifc handle.</param>
      /// <param name="propertyName">The property name.</param>
      /// <returns>The parameter.</returns>
      static public ParameterValue getParameterValueByNameFromSubelementCache(ElementId elementId, IFCAnyHandle subelementHandle, string propertyName)
      {
         ParameterValue parameterVal = null;

         IDictionary<IFCAnyHandle, ParameterValueSubelementCache> anyHandleParamValMap;
         if (!SubelementParameterValueCache.TryGetValue(elementId, out anyHandleParamValMap))
            return parameterVal;

         ParameterValueSubelementCache paramValueCache;
         if (!anyHandleParamValMap.TryGetValue(subelementHandle, out paramValueCache))
            return parameterVal;

         paramValueCache.TryGetValue(propertyName, out parameterVal);
         return parameterVal;
      }

      /// <summary>
      /// Returns true if the built-in parameter has the identical name and value as another parameter.
      /// Used to remove redundant output from the IFC export.
      /// </summary>
      /// <param name="parameter">The parameter</param>
      /// <returns>Returns true if the built-in parameter has the identical name and value as another parameter.</returns>
      static private bool IsDuplicateParameter(ElementId parameterId)
      {
         switch (parameterId.Value)
         {
            // Same as ELEM_CATEGORY_PARAM.
            case (long)BuiltInParameter.ELEM_CATEGORY_PARAM_MT:
            // DPART_ORIGINAL_CATEGORY_ID is the string version of DPART_ORIGINAL_CATEGORY_ID.  Not going to duplicate the data.
            case (long)BuiltInParameter.DPART_ORIGINAL_CATEGORY:
               return true;
         }
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
         switch (parameterId.Value)
         {
            case ((long)BuiltInParameter.ELEM_CATEGORY_PARAM_MT):
               return new ElementId(BuiltInParameter.ELEM_CATEGORY_PARAM);
            case ((long)BuiltInParameter.DPART_ORIGINAL_CATEGORY):
               return new ElementId(BuiltInParameter.DPART_ORIGINAL_CATEGORY_ID);
         }
         return parameterId;
      }

      static private ParameterElementCache PopulateCache(ElementId id)
      {
         ParameterElementCache parameterCache = new(id);

         IList<ElementId> parameterIds = ExporterCacheManager.ParameterAccess?.ListParameters(id);
         if (parameterIds != null)
         {
            foreach (ElementId parameterId in parameterIds)
            {
               if (IsDuplicateParameter(parameterId))
                  continue;

               ParameterInformation parameterInfo = ExporterCacheManager.ParameterInformationCache.GetDocumentParameterInformation(parameterId);
               if (string.IsNullOrWhiteSpace(parameterInfo.Name))
                  continue;

               parameterCache.AddParameter(parameterInfo.Name, parameterId, parameterInfo.Details);
            }
         }

         // In a federated export, also include the extended properties that the host document
         // defines for this linked element.  HostDocument is only non-null while exporting a link
         // in a federated model, so skip the call entirely in the common case.
         if (ExporterCacheManager.ExportOptionsCache.HostDocument != null)
            AddHostExtendedPropertiesForLinkedElement(id, parameterCache);

         return parameterCache;
      }

      /// <summary>
      /// Cache the parameters for an element, allowing quick access later.
      /// </summary>
      /// <param name="id">The element id.</param>
      static public ParameterElementCache GetCachedParametersForElement(ElementId id, bool isType)
      {
         if (isType)
         {
            if (ParameterElementCache.CurrentTypeCache.Item1 == id)
               return ParameterElementCache.CurrentTypeCache.Item2;
         }
         else
         {
            if (ParameterElementCache.CurrentInstanceCache.Item1 == id)
               return ParameterElementCache.CurrentInstanceCache.Item2;
         }

         if (isType || ExporterCacheManager.PreservedParameterCacheElementIds.Contains(id))
         {
            ref ParameterElementCache storedParameterCache = ref CollectionsMarshal.GetValueRefOrAddDefault(Parameters, id, out bool exists);
            if (!exists)
            {
               storedParameterCache = PopulateCache(id);
            }

            // If this is a stored instance id, we won't update so that it doesn't overwrite a non-stored cache.
            if (isType)
               ParameterElementCache.CurrentTypeCache = (id, storedParameterCache);
            
            return storedParameterCache;
         }

         ParameterElementCache parameterCache = PopulateCache(id);
         ParameterElementCache.CurrentInstanceCache = (id, parameterCache);
         return parameterCache;
      }

      /// <summary>
      /// In a federated export, add the extended properties that the host document defines for a
      /// linked element to its parameter cache.
      /// </summary>
      /// <param name="id">The id of the element in the linked document.</param>
      /// <param name="combinedList">The parameter cache to populate.</param>
      /// <remarks>These properties are stored in the host document and are not returned when listing
      /// the parameters of the linked element from the linked document.  Their values are evaluated
      /// on demand through the host document's ParameterAccess.  The caller must ensure HostDocument
      /// is non-null (i.e. that we are exporting a link in a federated model) before calling this.</remarks>
      static private void AddHostExtendedPropertiesForLinkedElement(ElementId id, ParameterElementCache combinedList)
      {
         Document hostDocument = ExporterCacheManager.ExportOptionsCache.HostDocument;

         LinkElementId linkElementId = ExporterStateManager.FederatedLinkManager.GetLinkElementId(id);
         if (linkElementId == null)
            return;

         ParameterAccess hostParameterAccess = ExporterCacheManager.HostParameterAccess;
         if (hostParameterAccess == null)
            return;

         IDictionary<Document, IList<ElementId>> parametersByDocument = hostParameterAccess.ListParameters(linkElementId);

         // We only want the extended properties whose definitions are stored in the host document.
         // The linked element's own parameters are already added when listing parameters from the
         // linked document.
         if (!(parametersByDocument?.TryGetValue(hostDocument, out IList<ElementId> parameterIds) ?? false) ||
            parameterIds == null)
            return;

         foreach (ElementId parameterId in parameterIds)
         {
            if (IsDuplicateParameter(parameterId))
               continue;

            ParameterInformation parameterInfo = ExporterCacheManager.ParameterInformationCache.GetHostDocumentParameterInformation(parameterId);
            if (string.IsNullOrWhiteSpace(parameterInfo.Name))
               continue;

            combinedList.AddHostExtendedProperty(parameterInfo.Name, parameterId, parameterInfo.Details, linkElementId);
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
         if (MathUtil.IsInvalidElementId(elementId) ||
             (subelementHandle == null) ||
             (param == null) ||
             (paramVal == null))
            return;

         if (IsDuplicateParameter(param.Id))
            return;

         Definition paramDefinition = param.Definition;
         if (paramDefinition == null)
            return;

         // Don't cache parameters that aren't visible to the user.
         InternalDefinition internalDefinition = paramDefinition as InternalDefinition;
         if (internalDefinition != null && internalDefinition.Visible == false)
            return;

         string propertyName = paramDefinition.Name;
         if (string.IsNullOrWhiteSpace(propertyName))
            return;

         IDictionary<IFCAnyHandle, ParameterValueSubelementCache> anyHandleParamValMap;
         if (!SubelementParameterValueCache.TryGetValue(elementId, out anyHandleParamValMap))
         {
            anyHandleParamValMap = new Dictionary<IFCAnyHandle, ParameterValueSubelementCache>();
            SubelementParameterValueCache[elementId] = anyHandleParamValMap;
         }

         ParameterValueSubelementCache paramCache;
         if (!anyHandleParamValMap.TryGetValue(subelementHandle, out paramCache))
         {
            paramCache = new ParameterValueSubelementCache();
            anyHandleParamValMap[subelementHandle] = paramCache;
         }

         ParameterValue cachedParamVal;
         if (paramCache.TryGetValue(propertyName, out cachedParamVal))
            return;

         paramCache.Add(propertyName, paramVal);
      }

      /// <summary>
      /// Gets the parameter by name from an element.
      /// </summary>
      /// <param name="elementId">The element id.</param>
      /// <param name="propertyName">The property name.</param>
      /// <returns>The Parameter.</returns>
      internal static EvaluatedParameter GetParameterFromName(ElementId elementId, string propertyName, bool isType)
      {
         return (GetCachedParametersForElement(elementId, isType).TryGetValue(propertyName, null, out var info) &&
            CanExportParameter(info.Item2)) ? info.Item1: null;
      }

      internal static bool CanExportParameter(ElementId parameterId)
      {
         switch (parameterId.Value)
         {
            case ((long) BuiltInParameter.ANALYTICAL_ROUGHNESS):
            case ((long)BuiltInParameter.DUCT_ROUGHNESS):
            case ((long)BuiltInParameter.PIPE_ROUGHNESS):
            case ((long)BuiltInParameter.ELEM_CATEGORY_PARAM):
               return false;
            default:
               return true;
         }
      }

      /// <summary>
      /// Gets the parameter by name from an element for a specific parameter group.
      /// </summary>
      /// <param name="elementId">The element id.</param>
      /// <param name="groupTypeId">The parameter group.</param>
      /// <param name="propertyName">The property name.</param>
      /// <returns>The Parameter.</returns>
      internal static EvaluatedParameter GetParameterFromNameAndGroup(ElementId elementId, ForgeTypeId groupTypeId, 
         string propertyName, bool isType)
      {
         // Should we use CanExportParameter here?
         ParameterElementCache combinedCache = GetCachedParametersForElement(elementId, isType);
         combinedCache.TryGetValue(propertyName, groupTypeId.TypeId, out (EvaluatedParameter, ElementId) info);
         return info.Item1;
      }

      public static (EvaluatedParameter, string) GetStringValueFromElementOrSymbol(Element element, Element elementType, bool allowUnset, 
         params string[] propertyNames)
      {
         (EvaluatedParameter parameter, string propertyValue) = GetStringValueFromElement(element, allowUnset, propertyNames);
         if (parameter != null)
            return (parameter, propertyValue);
         
         if (elementType == null && !(element is ElementType))
            elementType = element != null ? element.Document.GetElement(element.GetTypeId()) : null;
         
         if (elementType == null)
            return (null, null);

         (parameter, propertyValue) = GetStringValueFromElement(elementType, allowUnset, propertyNames);
         if (parameter != null)
            return (parameter, propertyValue);

         foreach (string propertyName in propertyNames)
         {
            (parameter, propertyValue) = GetStringValueFromElement(elementType, allowUnset, propertyName + "[Type]");
            if (parameter == null)
               continue;
            return (parameter, propertyValue);
         }
         
         return (null, null);
      }

      /// <summary>
      /// Gets integer value from parameter of an element or its element type.
      /// </summary>
      /// <param name="element">The element, which can be null.</param>
      /// <param name="propertyNames">The variable array of alternate names mainly to support backward compatibility</param>
      /// <returns>The property value, or null if not found.</returns>
      public static int? GetIntValueFromElementOrSymbol(Element element, params string[] propertyNames)
      {
         if (element == null)
            return null;

         (EvaluatedParameter parameter, int propertyValue) = GetIntValueFromElement(element, propertyNames);
         if (parameter != null)
            return propertyValue;

         Element elemType = element is ElementType ? element : element.Document.GetElement(element.GetTypeId());
         if (elemType == null)
            return null;

         (parameter, propertyValue) = GetIntValueFromElement(elemType, propertyNames);
         if (parameter != null)
            return propertyValue;

         foreach (string propertyName in propertyNames)
         {
            if (string.IsNullOrEmpty(propertyName))
               continue;

            (parameter, propertyValue) = GetIntValueFromElement(elemType, propertyName + "[Type]");
            if (parameter != null)
               return propertyValue;
         }

         return null;
      }

      /// <summary>
      /// This method returns a special parameter for Offset found in the FamilySymbol that influence the CurtainWall Panel position.
      /// </summary>
      /// <param name="the familySymbol"></param>
      /// <returns>maximum Offset value if there are more than one parameters of the same name</returns>
      public static double GetSpecialOffsetParameter(FamilySymbol familySymbol)
      {
         // This method is isolated here so that it can adopt localized parameter name as necessary
         double maxOffset = 0.0;

         Parameter paramOffset = familySymbol.GetParameter(ParameterTypeId.FamilyTopLevelOffsetParam);
         if (paramOffset != null)
         {
            maxOffset = paramOffset.AsDouble();
         }
         else
         {
            string offsetParameterName = "Offset";

            // In case there are more than one parameter of the same name, we will get one value that is the largest
            IList<Parameter> offsetParams = familySymbol.GetParameters(offsetParameterName);
            foreach (Parameter offsetP in offsetParams)
            {
               double offset = offsetP.AsDouble();
               if (offset > maxOffset)
                  maxOffset = offset;
            }
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

         double thicknessValue = 0.0;

         Parameter paramThickness = familySymbol.GetParameter(ParameterTypeId.FamilyThicknessParam);
         if (paramThickness != null)
         {
            thicknessValue = paramThickness.AsDouble();
         }
         else
         {
            string thicknessParameterName = "Thickness";
            IList<Parameter> thicknessParams = familySymbol.GetParameters(thicknessParameterName);

            foreach (Parameter thicknessP in thicknessParams)
            {
               // If happens there are more than 1 param with the same name, we will arbitrary choose the thickest value
               double thickness = thicknessP.AsDouble();
               if (thickness > thicknessValue)
                  thicknessValue = thickness;
            }
         }

         return thicknessValue;
      }

      /// <summary>
      /// Get override containment value through a parameter "IfcSpatialContainer" or "OverrideElementContainer". 
      /// Value can be "IFCSITE", "IFCBUILDING", or the appropriate Level name.
      /// </summary>
      /// <param name="element">The input element.</param>
      /// <param name="overrideContainerHnd">The entity handle of the container.</param>
      /// <returns>The element id of the container.</returns>
      public static ElementId OverrideContainmentParameter(Element element, out IFCAnyHandle overrideContainerHnd)
      {
         overrideContainerHnd = null;

         if (element == null)
         {
            return ElementId.InvalidElementId;
         }

         // Special case whether an object should be assigned to the Site or Building container
         (_, string containerOverrideName) = GetStringValueFromElement(element, false, "OverrideElementContainer", "IfcSpatialContainer");

         (ElementId containerElemId, overrideContainerHnd) = LevelUtil.FindContainer(containerOverrideName);
         return containerElemId;
      }

      /// <summary>
      /// Get override containment value through a parameter "IfcSpatialContainer" or "OverrideElementContainer". 
      /// Value can be "IFCSITE", "IFCBUILDING", or the appropriate Level name, given an IfcSpace entity handle.
      /// </summary>
      /// <param name="document">The document containing the element corresponding the the IfcSpace handle.</param>
      /// <param name="spaceHnd">The entity handle of the IfcSpace.</param>
      /// <param name="overrideContainerHnd">The entity handle of the container.</param>
      /// <returns>The element id of the container.</returns>
      public static ElementId OverrideSpaceContainmentParameter(Document document,
         IFCAnyHandle spaceHnd, out IFCAnyHandle overrideContainerHnd)
      {
         ElementId spaceId = ExporterCacheManager.HandleToElementCache.Find(spaceHnd);
         Element elem = document.GetElement(spaceId);
         return ParameterUtil.OverrideContainmentParameter(elem, out overrideContainerHnd);
      }

      /// <summary>
      /// Checks if the if parameter data type is equal to forgeTypeId
      /// </summary>
      /// <param name="parameter">The parameter.</param>
      /// <param name="forgeTypeId">The ForgeTypeId.</param>
      /// <returns>True if parameter data type is equal to forgeTypeId.</returns>
      public static bool ParameterDataTypeIsEqualTo(EvaluatedParameter parameter, ForgeTypeId forgeTypeId)
      {
         return parameter?.Definition?.GetDataType() == forgeTypeId;
      }

   }
}