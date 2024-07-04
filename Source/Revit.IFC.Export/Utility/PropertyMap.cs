//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
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
using System.IO;
using Autodesk.Revit.DB;
using Revit.IFC.Export.Exporter.PropertySet;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Represents the Revit parameter to get from element.
   /// </summary>
   class UserDefinedPropertyRevitParameter
   {
      /// <summary>
      /// Gets Revit <see cref="BuiltInParameter"/>.
      /// </summary>
      public BuiltInParameter BuiltInParameter { get; private set; } = BuiltInParameter.INVALID;

      /// <summary>
      /// Gets custom Revit paramater name.
      /// </summary>
      public string RevitParameter { get; private set; } = null;

      /// <summary>
      /// Gets a value indicating whether the Revit built-in parameter specified.
      /// </summary>
      public bool IsBuiltInParameterDefined { get; private set; } = false;

      /// <summary>
      /// Creates a new revit parameter representation instance for the given raw definition from the config.
      /// </summary>
      /// <param name="rawParameter">Data to parse.</param>
      /// <returns>A new instance of <see cref="UserDefinedPropertyRevitParameter"/>.</returns>
      public static UserDefinedPropertyRevitParameter Create(string rawParameter)
      {
         if (string.IsNullOrWhiteSpace(rawParameter))
            return null;

         UserDefinedPropertyRevitParameter parameter = new UserDefinedPropertyRevitParameter();
         const string prefix = "BuiltInParameter.";
         if (rawParameter.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
         {
            parameter.IsBuiltInParameterDefined = true;
            string builtinParameterName = rawParameter.Substring(prefix.Length);
            if (Enum.TryParse(builtinParameterName, out BuiltInParameter builtInParameter) && builtInParameter != BuiltInParameter.INVALID)
            {
               parameter.BuiltInParameter = builtInParameter;
            }
         }
         else
         {
            parameter.RevitParameter = rawParameter;
         }

         return parameter;
      }
   }

   /// <summary>
   /// Represents a single property definiton from the config.
   /// </summary>
   class UserDefinedProperty
   {
      /// <summary>
      /// Gets or sets a property name.
      /// </summary>
      public string Name { get; set; }

      /// <summary>
      /// Gets a list of defined Revit parameters.
      /// </summary>
      public List<UserDefinedPropertyRevitParameter> RevitParameters { get; private set; } = new List<UserDefinedPropertyRevitParameter>();

      /// <summary>
      /// Gets a property value type. By default <see cref="PropertyValueType.SingleValue"/>.
      /// </summary>
      public PropertyValueType IfcPropertyValueType { get; private set; } = PropertyValueType.SingleValue;

      /// <summary>
      /// Gets a list of property types.
      /// </summary>
      public List<string> IfcPropertyTypes { get; private set; } = new List<string>();

      /// <summary>
      /// Parses and sets <see cref="IfcPropertyValueType"/> and <see cref="IfcPropertyTypes"/> by given <paramref name="rawIfcPropertyTypes"/>.
      /// </summary>
      /// <param name="rawIfcPropertyTypes">Property type data to parse.</param>
      public void ParseIfcPropertyTypes(string rawIfcPropertyTypes)
      {
         if (string.IsNullOrWhiteSpace(rawIfcPropertyTypes))
            return;

         // format: <PropertyVaueType>.<ValueType>/<ValueType>/...
         IfcPropertyTypes.Clear();
         string dataTypePartToParse = string.Empty;
         string[] split = rawIfcPropertyTypes.Split('.') ?? new string[] {};
         if (split.Length == 1)
         {
            IfcPropertyValueType = PropertyValueType.SingleValue;
            dataTypePartToParse = split[0];
         } 
         else if (split.Length >= 2)
         {
            const string prefix = "Property";
            bool withPrefix = split[0].StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase);
            string rawValueType = withPrefix ? split[0].Substring(prefix.Length) : split[0];
            if (!Enum.TryParse(rawValueType, true, out PropertyValueType propertyValueType))
            {
               propertyValueType = PropertyValueType.SingleValue;
            }

            IfcPropertyValueType = propertyValueType;
            dataTypePartToParse = split[1];
         }

         string[] rawPropertyTypes = dataTypePartToParse.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
         foreach(string rawPropertyType in rawPropertyTypes)
         {
            const string prefix = "Ifc";
            bool withPrefix = rawPropertyType.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase);
            IfcPropertyTypes.Add(withPrefix ? rawPropertyType.Substring(prefix.Length) : rawPropertyType);
         }
      }

      /// <summary>
      /// Parses a list of revit parameters by given data from config.
      /// </summary>
      /// <param name="rawParameters">Data to parse.</param>
      public void ParseRevitParameters(string rawParameters)
      {
         if (string.IsNullOrWhiteSpace(rawParameters))
            return;

         RevitParameters.Clear();
         UserDefinedPropertyRevitParameter parameter = UserDefinedPropertyRevitParameter.Create(rawParameters);
         if (parameter != null)
            RevitParameters.Add(parameter);
      }

      /// <summary>
      /// Gets Revit parameters mapped into list of T type.
      /// </summary>
      /// <typeparam name="T">The type of list elements.</typeparam>
      /// <param name="mapper">Function to initialize T.</param>
      /// <returns>A list of Revit parameters mapped into list of T type.</returns>
      public List<T> GetEntryMap<T>(Func<string, BuiltInParameter, T> mapper) where T : EntryMap, new()
      {
         List<T> entryMap = new List<T>();
         foreach (UserDefinedPropertyRevitParameter parameter in RevitParameters)
         {
            if (parameter.BuiltInParameter != BuiltInParameter.INVALID)
            {
               entryMap.Add(mapper(Name, parameter.BuiltInParameter));
            }
            else if (!parameter.IsBuiltInParameterDefined)
            {
               entryMap.Add(mapper(parameter.RevitParameter, BuiltInParameter.INVALID));
            }
            else
            {
               // report as error in log when we create log file.
            }
         }

         return entryMap;
      }

      /// <summary>
      /// Returns the first element of <see cref="IfcPropertyTypes"/> converted to <typeparamref name="TEnum"/>,
      /// or a <paramref name="defaultValue"/> if <see cref="IfcPropertyTypes"/> contains no elements.
      /// </summary>
      /// <typeparam name="TEnum">The enumeration type to which to convert value.</typeparam>
      /// <param name="defaultValue">
      /// Value to return if the index is out of range, if <see cref="IfcPropertyTypes"/> is empty or
      /// first element value is not represented in the <typeparamref name="TEnum"/>.
      /// </param>
      /// <returns>
      /// <paramref name="defaultValue"/> if source is empty; otherwise, the first element in <see cref="IfcPropertyTypes"/>
      /// converted to <typeparamref name="TEnum"/>
      /// </returns>
      public TEnum FirstIfcPropertyTypeOrDefault<TEnum>(TEnum defaultValue) where TEnum : struct
      {
         if ((IfcPropertyTypes?.Count ?? 0) == 0)
            return defaultValue;

         if(Enum.TryParse(IfcPropertyTypes[0], true, out TEnum t))
            return t;

         return defaultValue;
      }

      /// <summary>
      /// Returns the type at a specified index in <see cref=" IfcPropertyTypes"/> converted to <typeparamref name="TEnum"/>
      /// or a default value if the index is out of range, if <see cref="IfcPropertyTypes"/> is empty, if value at a specified
      /// index is not represented in the <typeparamref name="TEnum"/>.
      /// </summary>
      /// <typeparam name="TEnum">The enumeration type to which to convert value.</typeparam>
      /// <param name="index">The zero-based index of the element to retrieve.</param>
      /// <param name="defaultValue">
      /// Value to return if the index is out of range, if <see cref="IfcPropertyTypes"/> is empty, if value at a specified
      /// index is not represented in the <typeparamref name="TEnum"/>.
      /// </param>
      /// <returns>
      /// <paramref name="defaultValue"/> if the index is outside the bounds of the source sequence or <see cref="IfcPropertyTypes"/> is empty; 
      /// otherwise,the element at the specified position in the source sequence.
      /// </returns>
      public TEnum GetIfcPropertyAtOrDefault<TEnum>(int index, TEnum defaultValue) where TEnum : struct
      {
         if ((IfcPropertyTypes?.Count ?? 0) == 0 || IfcPropertyTypes.Count >= index || index < 0)
            return defaultValue;

         if (Enum.TryParse(IfcPropertyTypes[index], true, out TEnum t))
            return t;

         return defaultValue;
      }
   }

   /// <summary>
   /// Represents a property set from the config.
   /// </summary>
   class UserDefinedPropertySet
   {
      /// <summary>
      /// Gets a property set name.
      /// </summary>
      public string Name { get; set; }

      /// <summary>
      /// Gets a type of elements group for which the property set is defined.
      /// </summary>
      public string Type { get; set; }

      /// <summary>
      /// Gets a list of IFC entites for which property set is defined.
      /// </summary>
      public string[] IfcEntities { get; set; }

      /// <summary>
      /// Gets a list of properties in the property set.
      /// </summary>
      public IList<UserDefinedProperty> Properties { get; set; } = new List<UserDefinedProperty>();
   }

   class PropertyMap
   {
      /// <summary>
      /// Load parameter mapping. It parses lines until it finds UserdefinedPset
      /// </summary>
      /// <returns>dictionary of parameter mapping</returns>
      public static Dictionary<Tuple<string, string>, string> LoadParameterMap()
      {
         Dictionary<Tuple<string, string>, string> parameterMap = new Dictionary<Tuple<string, string>, string>();
         try
         {
            string filename = GetFilename();
            if (File.Exists(filename))
            {
               using (StreamReader sr = new StreamReader(filename))
               {
                  string line;
                  while ((line = sr.ReadLine()) != null)
                  {
                     line.TrimStart(' ', '\t');
                     if (String.IsNullOrEmpty(line)) continue;
                     ParseLine(parameterMap, line);
                  }
               }
            }
         }
         catch (Exception e)
         {
            Console.WriteLine("The file could not be read:");
            Console.WriteLine(e.Message);
         }

         return parameterMap;
      }

      /// <summary>
      /// Parsing lines for Property Mapping
      /// </summary>
      /// <param name="parameterMap">parameter map</param>
      /// <param name="line">line from file</param>
      private static void ParseLine(Dictionary<Tuple<string, string>, string> parameterMap, string line)
      {
         // if not a comment
         if (line[0] != '#')
         {
            // add the line
            string[] split = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length == 3)
               parameterMap.Add(Tuple.Create(split[0], split[1]), split[2]);
         }
      }

      /// <summary>
      /// Load user-defined Property set
      /// Format:
      ///    PropertySet: <Pset_name> I[nstance]/T[ype] <IFC entity list separated by ','> 
      ///              Property_name   Data_type   Revit_Parameter
      ///              ...
      /// Datatype supported: Text, Integer, Real, Boolean
      /// Line divider between Property Mapping and User defined property sets:
      ///     #! UserDefinedPset
      /// </summary>
      /// <returns>List of property set definitions</returns>
      public static IEnumerable<UserDefinedPropertySet> LoadUserDefinedPset()
      {
         List<UserDefinedPropertySet> userDefinedPropertySets = new List<UserDefinedPropertySet>();

         try
         {
            string filename = ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportUserDefinedPsetsFileName;
            if (!File.Exists(filename))
            {
               // This allows for the original behavior of looking in the directory of the export DLL to look for the default file name.
               filename = GetUserDefPsetFilename();
            }
            if (!File.Exists(filename))
               return userDefinedPropertySets;

            // Format: PropertSet: <Pset_name> I[nstance]/T[ype] <IFC entity list separated by ','> 
            //              Property_name   Data_type   Revit_Parameter
            // ** For now it only works for simple property with single value (datatype supported: Text, Integer, Real and Boolean)
            using (StreamReader sr = new StreamReader(filename))
            {
               string line;

               // current property set
               UserDefinedPropertySet userDefinedPropertySet = null;
               while ((line = sr.ReadLine()) != null)
               {
                  line = line.TrimStart(' ', '\t');

                  if (string.IsNullOrEmpty(line) || line[0] == '#')
                     continue;

                  string[] split = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                  if (split.Length >=4 && string.Compare(split[0], "PropertySet:", true) == 0) // Any entry with less than 3 parameters is malformed.
                  {
                     userDefinedPropertySet = new UserDefinedPropertySet()
                     {
                        Name = split[1],
                        Type = split[2],
                        IfcEntities = split[3].Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                     };

                     userDefinedPropertySets.Add(userDefinedPropertySet);
                  }
                  else if (split.Length >= 2 && userDefinedPropertySet != null) // Skip property definitions outside of property set block.
                  {
                     UserDefinedProperty userDefinedProperty = new UserDefinedProperty();
                     userDefinedProperty.Name = split[0];
                     userDefinedProperty.ParseIfcPropertyTypes(split[1]);

                     if (split.Length >= 3)
                     {
                        userDefinedProperty.ParseRevitParameters(split[2]);
                     }

                     userDefinedPropertySet.Properties.Add(userDefinedProperty);
                  }
               }
            }
         }
         catch (Exception e)
         {
            Console.WriteLine("The file could not be read:");
            Console.WriteLine(e.Message);
         }

         return userDefinedPropertySets;
      }

      /// <summary>
      /// Get file name
      /// </summary>
      /// <returns>file name</returns>
      private static string GetFilename()
      {
         return ExporterCacheManager.ExportOptionsCache.SelectedParametermappingTableName;

      }

      /// <summary>
      /// Get file that contains User defined PSet (using the export configuration name as the file name)
      /// </summary>
      /// <returns></returns>
      private static string GetUserDefPsetFilename()
      {
         string directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
         return directory + @"\" + ExporterCacheManager.ExportOptionsCache.SelectedConfigName + @".txt";
      }
   }
}
