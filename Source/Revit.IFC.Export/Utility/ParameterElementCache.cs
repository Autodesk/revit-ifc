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
using Revit.IFC.Common.Utility;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// The values associated with an parameter element.
   /// </summary>
   public class ParameterElementInfo
   {
      public ParameterElementInfo() { }

      public ParameterElementInfo(ElementId elementId, ParameterDetails details, string name) 
      {
         ElementId = elementId;
         Details = details;
         Name = name;
      }

      /// <summary>
      /// The element id of the parameter.
      /// </summary>
      public ElementId ElementId { get; set; } = null;

      /// <summary>
      /// Extra information for the parameter that is calculated as needed.
      /// </summary>
      public ParameterDetails Details { get; set; } = null;

      /// <summary>
      /// The name of the parameter.
      /// </summary>
      /// <remarks>This is generally stored in a dictionary where the key is the name, but the key is all uppercase with no spaces or
      /// underscores.  This is the original name.</remarks>
      public string Name { get; set; } = null;

      /// <summary>
      /// For a host-defined extended property on a linked element in a federated export, the linked
      /// element as seen from the host document, used to evaluate the value through the host
      /// document's ParameterAccess.  Null for ordinary parameters, which are evaluated through the
      /// owning document's ParameterAccess.
      /// </summary>
      /// <remarks>This routing is stored per parameter (rather than keyed by parameter id) so that a
      /// linked element's own parameter and a host extended property that happen to share the same
      /// numeric ParameterElement id are never confused with each other.</remarks>
      public LinkElementId HostExtendedPropertyLinkId { get; set; } = null;
   }
   /// <summary>
   /// Contains a cache from cleaned parameter name to the parameters with that name.  
   /// Intended to be grouped by parameter group.
   /// </summary>
   /// <remarks>
   /// Note that Revit may have multiple parameters with the same name, that IFC doesn't support.
   /// For now, we support only exporting the first parameter with a value with the same name, as determined
   /// by the parameter with the lowest id.  We'd like to extend this by exporting all parameters
   /// with the same name, uniqified for IFC, but this requires significant changes in routines
   /// that expect one parameter per name.
   /// </remarks>
   public class ParameterElementCache
   {
      public Dictionary<NamingUtil.IFCStringKey, IList<ParameterElementInfo>> ParameterIdCache { get; private set; } = new();

      public static (ElementId, ParameterElementCache) CurrentInstanceCache = (null, null);

      public static (ElementId, ParameterElementCache) CurrentTypeCache = (null, null);

      private ElementId ElementId { get; set; }

      /// <summary>
      /// The number of distinct parameter names in the cache.
      /// </summary>
      public int Count => ParameterIdCache.Count;

      /// <summary>
      /// The default constructor.
      /// </summary>
      public ParameterElementCache(ElementId id)
      {
         if (MathUtil.IsInvalidElementId(id))
            throw new ArgumentException("id must be a valid ElementId", nameof(id));
         ElementId = id;
      }

      /// <summary>
      /// Force the calculation of all parameter values, and return the collection of parameters.
      /// </summary>
      /// <param name="elementId">The Revit element id.</param>
      /// <returns>The collection of evaluated parameters.</returns>
      public ICollection<EvaluatedParameter> CalculateAllValues()
      {
         Dictionary<ElementId, EvaluatedParameter> parameterValues = [];

         foreach (IList<ParameterElementInfo> parameterIds in ParameterIdCache.Values)
         {
            foreach (ParameterElementInfo info in parameterIds)
            {
               // We will only take the first parameter with a value.  If no parameter has a value, we will ignore it.
               // TODO: In the future, if we want to export parameters without values, we will need to put a placeholder here.
               EvaluatedParameter value = GetEvaluatedParameter(info);
               if (value?.HasValue == true)
               {
                  parameterValues[info.ElementId] = value;
                  break;
               }
            }
         }

         return parameterValues.Values;
      }

      /// <summary>
      /// Try to get the parameter for this element by name.
      /// </summary>
      /// <param name="key">The name of the parameter.</param>
      /// <param name="groupId">The optional group ID of the parameter.</param>
      /// <param name="value">The evaluated parameter.</param>
      /// <returns>True if the parameter was found and has a value; otherwise, false.</returns>
      public bool TryGetValue(string parameterName, string groupId, out (EvaluatedParameter, ElementId) value)
      {
         value = (null, null);

         NamingUtil.IFCStringKey parameterKey = new(parameterName);
         if (!ParameterIdCache.TryGetValue(parameterKey, out IList<ParameterElementInfo> parameterIds))
            return false;

         // We will populate the value parameter with the first EvaluatedParameter in the list that has a
         // value, or null.
         foreach (ParameterElementInfo info in parameterIds)
         {
            if (groupId != null && groupId.CompareTo(info.Details.GroupTypeIdAsString) != 0)
               continue;

            value = (GetEvaluatedParameter(info), info.ElementId);
            if (value.Item1?.HasValue == true)
               return true;
         }

         return false;
      }

      /// <summary>
      /// Associate a parameter name with a parameter id for this element.
      /// </summary>
      /// <param name="parameterName">The parameter name.</param>
      /// <param name="parameterId">The parameter id.</param>
      /// <param name="details">The parameter details.</param>
      /// <param name="hostExtendedPropertyLinkId">For a host-defined extended property on a linked
      /// element, the linked element as seen from the host document; null for ordinary parameters.</param>
      public void AddParameter(string parameterName, ElementId parameterId, ParameterDetails details,
         LinkElementId hostExtendedPropertyLinkId = null)
      {
         ParameterElementInfo info = new(parameterId, details, parameterName)
         {
            HostExtendedPropertyLinkId = hostExtendedPropertyLinkId
         };
         
         NamingUtil.IFCStringKey parameterKey = new(parameterName);
         ref IList<ParameterElementInfo> parameterInfos = ref CollectionsMarshal.GetValueRefOrAddDefault(ParameterIdCache, parameterKey, out bool exists);
         if (!exists)
         {
            parameterInfos = [info];
            return;
         }

         parameterInfos.Add(info);
      }

      /// <summary>
      /// Associate a parameter name with a host-defined extended property for a linked element.
      /// </summary>
      /// <param name="parameterName">The parameter name.</param>
      /// <param name="parameterId">The parameter id in the host document.</param>
      /// <param name="details">The parameter details.</param>
      /// <param name="linkElementId">The linked element as seen from the host document.</param>
      /// <remarks>The value is evaluated on demand through the host document's ParameterAccess, since
      /// it can't be obtained through the linked document's ParameterAccess.  The routing is stored on
      /// the parameter itself so that it is never confused with the linked element's own parameters,
      /// even when a host extended property shares the same numeric ParameterElement id.</remarks>
      public void AddHostExtendedProperty(string parameterName, ElementId parameterId, ParameterDetails details, LinkElementId linkElementId)
      {
         AddParameter(parameterName, parameterId, details, linkElementId);
      }

      /// <summary>
      /// Get the evaluated value for one of this element's parameters, evaluating host-defined
      /// extended properties through the host document's ParameterAccess and all other parameters
      /// through the main document's ParameterAccess.
      /// </summary>
      /// <param name="info">The parameter to evaluate.</param>
      /// <returns>The evaluated parameter, or null if a host-defined extended property cannot be
      /// evaluated because the host document's ParameterAccess is unavailable.</returns>
      private EvaluatedParameter GetEvaluatedParameter(ParameterElementInfo info)
      {
         // HostParameterAccess is null unless we are exporting a link in a federated model, which is
         // the rare case.  The link id is stored on the parameter itself, so there is no chance of
         // confusing a host extended property with a linked element's own parameter that happens to
         // share the same id.
         LinkElementId linkElementId = info.HostExtendedPropertyLinkId;
         if (linkElementId != null)
         {
            // A host-defined extended property can only be evaluated through the host document's
            // ParameterAccess.  If it is unavailable (e.g. a stale routing entry evaluated outside of
            // a federated link export), we must not fall back to the linked document's ParameterAccess:
            // that would evaluate a different document and could silently return the linked element's
            // own parameter that happens to share the same numeric id.  Return null so callers observe
            // "no value" rather than a wrong value.
            ParameterAccess hostParameterAccess = ExporterCacheManager.HostParameterAccess;
            if (hostParameterAccess == null)
               return null;

            return hostParameterAccess.GetParameter(linkElementId,
               ExporterCacheManager.ExportOptionsCache.HostDocument, info.ElementId);
         }

         return ExporterCacheManager.ParameterAccess.GetParameter(ElementId, info.ElementId);
      }
   }

   /// <summary>
   /// Contains a cache from parameter name to parameter value.
   /// </summary>
   public class ParameterValueSubelementCache
   {
      /// <summary>
      /// The cache from parameter name to parameter value.
      /// </summary>
      private Dictionary<NamingUtil.IFCStringKey, ParameterValue> ParameterValueCache { get; set; } = new();

      public bool TryGetValue(string propertyName, out ParameterValue value)
      {
         NamingUtil.IFCStringKey key = new(propertyName);
         return ParameterValueCache.TryGetValue(key, out value);
      }

      public void Add(string propertyName, ParameterValue value)
      {
         NamingUtil.IFCStringKey key = new(propertyName);
         ParameterValueCache[key] = value;
      }

      /// <summary>
      /// The default constructor.
      /// </summary>
      public ParameterValueSubelementCache()
      {
      }
   }
}