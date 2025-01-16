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
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet
{
   public class RevitBuiltInParameterMapper
   {
      /// <summary>
      /// This is currently a hardwired list of properties that can be retrieved from Revit
      /// built-in parameters.
      /// </summary>
      static private IDictionary<KeyValuePair<string, string>, BuiltInParameter> m_BuiltInSpecificParameterMapping = null;

      static private IDictionary<string, BuiltInParameter> m_BuiltInGeneralParameterMapping = null;

      static private void AddEntry(string propertyName, BuiltInParameter revitParameter)
      {
         m_BuiltInGeneralParameterMapping.Add(propertyName, revitParameter);
      }

      static private void AddEntry(string psetName, string propertyName, BuiltInParameter revitParameter)
      {
         m_BuiltInSpecificParameterMapping.Add(new KeyValuePair<string, string>(psetName, propertyName), revitParameter);
      }

      static private void Initialize()
      {
         m_BuiltInSpecificParameterMapping = new Dictionary<KeyValuePair<string, string>, BuiltInParameter>();
         AddEntry("Pset_ManufacturerTypeInformation", "Manufacturer", BuiltInParameter.ALL_MODEL_MANUFACTURER);
         AddEntry("Pset_CoveringCommon", "TotalThickness", BuiltInParameter.CEILING_THICKNESS);
         AddEntry("Pset_LightFixtureTypeCommon", "TotalWattage", BuiltInParameter.LIGHTING_FIXTURE_WATTAGE);
         AddEntry("Pset_RoofCommon", "TotalArea", BuiltInParameter.HOST_AREA_COMPUTED);
         
         m_BuiltInGeneralParameterMapping = new Dictionary<string, BuiltInParameter>();
         AddEntry("Span", BuiltInParameter.INSTANCE_LENGTH_PARAM);
         AddEntry("CeilingCovering", BuiltInParameter.ROOM_FINISH_CEILING);
         AddEntry("WallCovering", BuiltInParameter.ROOM_FINISH_WALL);
         AddEntry("FloorCovering", BuiltInParameter.ROOM_FINISH_FLOOR);
         AddEntry("FireRating", BuiltInParameter.FIRE_RATING);
         AddEntry("ThermalTransmittance", BuiltInParameter.ANALYTICAL_HEAT_TRANSFER_COEFFICIENT);
      }

      static private IDictionary<KeyValuePair<string, string>, BuiltInParameter> BuiltInSpecificParameterMapping
      {
         get
         {
            if (m_BuiltInSpecificParameterMapping == null)
               Initialize();
            return m_BuiltInSpecificParameterMapping;
         }
      }

      static private IDictionary<string, BuiltInParameter> BuiltInGeneralParameterMapping
      {
         get
         {
            if (m_BuiltInGeneralParameterMapping == null)
               Initialize();
            return m_BuiltInGeneralParameterMapping;
         }
      }

      static public BuiltInParameter GetRevitBuiltInParameter(string psetName, string propertyName)
      {
         BuiltInParameter builtInParameter = BuiltInParameter.INVALID;
         if (BuiltInGeneralParameterMapping.TryGetValue(propertyName, out builtInParameter))
            return builtInParameter;
         if (BuiltInSpecificParameterMapping.TryGetValue(new KeyValuePair<string, string>(psetName, propertyName),
               out builtInParameter))
            return builtInParameter;
         return BuiltInParameter.INVALID;
      }
   }

   /// <summary>
   /// A description mapping of a group of Revit parameters and/or calculated values to an IfcPropertySet.
   /// </summary>
   /// <remarks>
   /// The mapping includes: the name of the IFC property set, the entity type this property to which this set applies,
   /// and an array of property set entries.  A property set description is valid for only one entity type.
   /// </remarks>
   public class PropertySetDescription : Description
   {
      /// <summary>
      /// The entries stored in this property set description.
      /// </summary>
      IList<PropertySetEntry> m_Entries = new List<PropertySetEntry>();

   
      /// <summary>
      /// Determines whether properties from the element's type should be added to the instance.
      /// </summary>
      public bool AddTypePropertiesToInstance { get; set; }

      /// <summary>
      /// The entries stored in this property set description.
      /// </summary>
      public void AddEntry(PropertySetEntry entry)
      {
         //if the PropertySetDescription name and PropertySetEntry name are in the dictionary, 
         Tuple<string, string> key = new Tuple<string, string>(this.Name, entry.PropertyName);
         if (ExporterCacheManager.PropertyMapCache.ContainsKey(key))
         {
            //replace the PropertySetEntry.RevitParameterName by the value in the cache.
            entry.SetRevitParameterName(ExporterCacheManager.PropertyMapCache[key]);
         }

         entry.SetRevitBuiltInParameter(RevitBuiltInParameterMapper.GetRevitBuiltInParameter(key.Item1, key.Item2));
         entry.UpdateEntry();
         m_Entries.Add(entry);
      }

      private string UsablePropertyName(IFCAnyHandle propHnd, IDictionary<string, IFCAnyHandle> propertiesByName)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
            return null;

         string currPropertyName = IFCAnyHandleUtil.GetStringAttribute(propHnd, "Name");
         if (string.IsNullOrWhiteSpace(currPropertyName))
            return null;   // This shouldn't be posssible.

         // Don't override if the new value is empty.
         if (propertiesByName.ContainsKey(currPropertyName))
         {
            try
            {
               // Only IfcSimplePropertyValue has the NominalValue attribute; any other type of property will throw.
               IFCData currPropertyValue = propHnd.GetAttribute("NominalValue");
               if (currPropertyValue.PrimitiveType == IFCDataPrimitiveType.String && string.IsNullOrWhiteSpace(currPropertyValue.AsString()))
                  return null;
            }
            catch
            {
               // Not an IfcSimplePropertyValue - no need to verify.
            }
         }

         return currPropertyName;
      }

      /// <summary>
      /// Creates handles for the properties.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="exporterIFC">The ExporterIFC class.</param>
      /// <param name="ifcParams">The extrusion creation data, used to get extra parameter information.</param>
      /// <param name="elementOrConnectorToUse">The base element or connector.</param>
      /// <param name="elemTypeToUse">The base element type.</param>
      /// <param name="handle">The handle for which we process the entries.</param>
      /// <returns>A set of property handles.</returns>
      public ISet<IFCAnyHandle> ProcessEntries(IFCFile file, ExporterIFC exporterIFC, IFCExportBodyParams ifcParams, 
         ElementOrConnector elementOrConnectorToUse, ElementType elemTypeToUse, IFCAnyHandle handle)
      {
         // We need to ensure that we don't have the same property name twice in the same property set.
         // By convention, we will keep the last property with the same name.  This allows for a user-defined
         // property set to look at both the type and the instance for a property value, if the type and instance properties
         // have different names.
         IDictionary<string, IFCAnyHandle> propertiesByName = new SortedDictionary<string, IFCAnyHandle>();

         // Get the property from Type for this element if the pset is for schedule or 
         // if element doesn't have an associated type (e.g. IfcRoof)
         bool lookInType = ExporterCacheManager.ViewScheduleElementCache.ContainsKey(this.ViewScheduleId)
                           || IFCAnyHandleUtil.IsTypeOneOf(handle, PropertyUtil.EntitiesWithNoRelatedType);

         foreach (PropertySetEntry entry in m_Entries)
         {
            try
            {
               IFCAnyHandle propHnd = entry.ProcessEntry(file, exporterIFC, Name, ifcParams, elementOrConnectorToUse, elemTypeToUse, handle, lookInType, AddTypePropertiesToInstance);

               if (IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd) && ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportMaterialPsets)
                  propHnd = MaterialBuildInParameterUtil.CreateMaterialPropertyIfBuildIn(Name, entry.PropertyName, entry.PropertyType, elementOrConnectorToUse?.Element, file);

               if (IFCAnyHandleUtil.IsNullOrHasNoValue(propHnd))
                  continue;

               string currPropertyName = UsablePropertyName(propHnd, propertiesByName);
               if (currPropertyName != null)
                  propertiesByName[currPropertyName] = propHnd;
            }
            catch(Exception) { }
         }

         ISet<IFCAnyHandle> props = new HashSet<IFCAnyHandle>(propertiesByName.Values);
         return props;
      }
   }
}
