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
   /// <summary>
   /// A description mapping of a group of Revit parameters to an predefined properties.
   /// </summary>
   /// <remarks>
   /// The mapping includes: the name of the IFC property set, the entity type this property to which this set applies,
   /// and an array of property set entries.  A property set description is valid for only one entity type.
   /// </remarks>
   public class PreDefinedPropertySetDescription : Description
   {
      /// <summary>
      /// The entries stored in this predefined property set description.
      /// </summary>
      IList<PreDefinedPropertySetEntry> m_Entries = new List<PreDefinedPropertySetEntry>();


      /// <summary>
      /// Add entry to this predefined property set description.
      /// </summary>
      public void AddEntry(PreDefinedPropertySetEntry entry)
      {
         //if the PreDefinedPropertySetDescription name and PreDefinedPropertySetEntry name are in the dictionary, 
         Tuple<string, string> key = new Tuple<string, string>(this.Name, entry.PropertyName);
         if (ExporterCacheManager.PropertyMapCache.ContainsKey(key))
         {
            //replace the PreDefinedPropertySetEntry.RevitParameterName by the value in the cache.
            entry.SetRevitParameterName(ExporterCacheManager.PropertyMapCache[key]);
         }

         entry.SetRevitBuiltInParameter(RevitBuiltInParameterMapper.GetRevitBuiltInParameter(key.Item1, key.Item2));
         entry.UpdateEntry();
         m_Entries.Add(entry);
      }

      /// <summary>
      /// Creates attributes for predefined property.
      /// </summary>
      /// <param name="element">The base element.</param>
      /// <returns>A set attributes for predefined property.</returns>
      public IList<(string name, PropertyValueType type, IList<IFCData> data)> ProcessEntries(IFCFile file, Element element)
      {
         IList<(string name, PropertyValueType type, IList<IFCData> data)> createdAttributes = null;

         foreach (PreDefinedPropertySetEntry entry in m_Entries)
         {
            IList<IFCData> data = entry.ProcessEntry(file, element);

            if (data == null && ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportMaterialPsets)
               data = MaterialBuildInParameterUtil.CreatePredefinedDataIfBuildIn(Name, entry.PropertyName, entry.PropertyType, element);

            if (data == null)
               continue;

            if (createdAttributes == null)
               createdAttributes = new List<(string, PropertyValueType, IList<IFCData>)>();

            createdAttributes.Add((entry.PropertyName, entry.PropertyValueType, data));
         }

         return createdAttributes;
      }
   }
}