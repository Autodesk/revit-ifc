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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RevitIFCTools.PropertySet;

namespace RevitIFCTools
{
   /// <summary>
   /// Parser for HTML Property Set Definition files from 'lexical' folders
   /// Extracts the same data as XML parser but from HTML format
   /// </summary>
   public class HtmlPsetDefinitionParser
   {
      private StreamWriter logF;
      
      // Static cache to avoid double reading of enumeration files
      private static Dictionary<string, List<PropertyEnumItem>> enumCache = new Dictionary<string, List<PropertyEnumItem>>();

      public HtmlPsetDefinitionParser(StreamWriter logger)
      {
         logF = logger;
      }

      /// <summary>
      /// Process HTML file and extract PropertySet definition
      /// </summary>
      /// <param name="schemaVersion">IFC schema version</param>
      /// <param name="htmlFile">HTML file to process</param>
      /// <param name="psetOrQtoSet">Property set type definitions</param>
      /// <returns>Parsed PsetDefinition</returns>
      public PsetDefinition ProcessHtmlFile(string schemaVersion, FileInfo htmlFile, Dictionary<ItemsInPsetQtoDefs, string> psetOrQtoSet)
      {
         try
         {
            string htmlContent = File.ReadAllText(htmlFile.FullName);
            
            PsetDefinition pset = new PsetDefinition();
            
            // Extract core data
            pset.Name = ExtractSetName(htmlContent);
            pset.IfcVersion = NormalizeIfcVersion(ExtractIfcVersionFromHeader(htmlContent));
            
            // Extract applicable classes and predefined types
            var applicableData = ExtractApplicableClassesAndTypes(htmlContent);
            pset.ApplicableClasses = applicableData.Classes;
            pset.ApplicableType = applicableData.ApplicableType;
            pset.PredefinedTypes = applicableData.PredefinedTypes;
            
            // Extract properties
            pset.properties = ExtractProperties(htmlContent, htmlFile.DirectoryName, psetOrQtoSet);

            return pset;
         }
         catch (Exception ex)
         {
            throw new Exception($"Failed to parse HTML file {htmlFile.FullName}: {ex.Message}", ex);
         }
      }

      /// <summary>
      /// Extract Set name from HTML h1 tag (Pset_ or Qto_)
      /// </summary>
      private string ExtractSetName(string htmlContent)
      {
         // Pattern: <h1>7.2.5.1 Qto_ActuatorBaseQuantities</h1> or <h1>6.1.4.23 Pset_WallCommon</h1>
         var match = Regex.Match(htmlContent, @"<h1[^>]*>.*?(Pset_\w+|Qto_\w+)</h1>", RegexOptions.Singleline);
         if (!match.Success)
         {
            throw new Exception("Property Set or QTO Set name not found in HTML h1 tag");
         }
         return match.Groups[1].Value;
      }

      /// <summary>
      /// Extract IFC Version from header section
      /// </summary>
      private string ExtractIfcVersionFromHeader(string htmlContent)
      {
         // Pattern: <header><p>IFC 4.3.2.0 (IFC4X3_ADD2)</p>
         var headerMatch = Regex.Match(htmlContent, @"<header>.*?<p>(.*?)\s*\(([^)]+)\)", RegexOptions.Singleline);
         if (headerMatch.Success)
         {
            return headerMatch.Groups[2].Value; // IFC4X3_ADD2
         }
         
         throw new Exception("IFC Version not found in HTML header section");
      }

      /// <summary>
      /// Normalize IFC version to match XML parser format
      /// </summary>
      private string NormalizeIfcVersion(string ifcVersion)
      {
         if (ifcVersion.StartsWith("2"))
         {
            if (ifcVersion.Equals("2X", StringComparison.CurrentCultureIgnoreCase)
               || ifcVersion.Equals("2X2", StringComparison.CurrentCultureIgnoreCase)
               || ifcVersion.Equals("2.X", StringComparison.CurrentCultureIgnoreCase))
               return "IFC2X2";
            else if (ifcVersion.StartsWith("IFC2X3", StringComparison.CurrentCultureIgnoreCase))
               return "IFC2X3";
            else
               return "IFC" + ifcVersion.ToUpper();
         }
         else if (ifcVersion.StartsWith("IFC4X3", StringComparison.InvariantCultureIgnoreCase))
         {
            return "IFC4X3";
         }
         else if (ifcVersion.StartsWith("IFC4", StringComparison.InvariantCultureIgnoreCase))
         {
            return ifcVersion.ToUpper();
         }
         else
         {
            throw new Exception($"Unrecognized IFC version: {ifcVersion}");
         }
      }

      /// <summary>
      /// Extract applicable classes and predefined types
      /// </summary>
      private (List<string> Classes, string ApplicableType, IList<string> PredefinedTypes) ExtractApplicableClassesAndTypes(string htmlContent)
      {
         var classes = new List<string>();
         string applicableType = null;
         
         // Find applicable entities section
         var section = ExtractSection(htmlContent, "Applicable entities");
         if (string.IsNullOrEmpty(section))
         {
            throw new Exception("Applicable entities section not found");
         }
         
         // Pattern: <li><a href="IfcWall.htm">IfcWall</a></li> or <li><a href="IfcCableSegment.htm">IfcCableSegment</a>/CORESEGMENT</li>
         var matches = Regex.Matches(section, @"<li><a href=""(\w+)\.htm"">(\w+)</a>(?:/(\w+))?</li>", RegexOptions.Singleline);
         
         var allClassNames = new List<string>();
         var predefinedTypes = new List<string>();
         
         foreach (Match match in matches)
         {
            string className = match.Groups[2].Value; // IfcWall, IfcCableSegment
            if (!allClassNames.Contains(className))
               allClassNames.Add(className);
            
            if (match.Groups[3].Success)
            {
               string predefType = match.Groups[3].Value;
               if (!predefinedTypes.Contains(predefType))
                  predefinedTypes.Add(predefType);
            }
         }
         
         classes = allClassNames;
         
         if (allClassNames.Count > 0)
         {
            applicableType = allClassNames[0];
         }
         
         return (classes, applicableType, predefinedTypes);
      }

      /// <summary>
      /// Extract properties from HTML table
      /// </summary>
      private HashSet<PsetProperty> ExtractProperties(string htmlContent, string folderPath, Dictionary<ItemsInPsetQtoDefs, string> psetOrQtoSet)
      {
         var properties = new HashSet<PsetProperty>(new PropertyComparer());
         
         // Find properties table
         var tableSection = ExtractPropertiesTable(htmlContent);
         if (string.IsNullOrEmpty(tableSection))
         {
            throw new Exception("Properties table not found in HTML");
         }
         
         // Extract table rows
         var rows = ExtractTableRows(tableSection);
         
         foreach (var row in rows)
         {
            var prop = ParsePropertyRow(row, folderPath, psetOrQtoSet);
            if (prop != null)
            {
               properties.Add(prop);
            }
         }
         
         return properties;
      }

      /// <summary>
      /// Extract properties table from HTML
      /// </summary>
      private string ExtractPropertiesTable(string htmlContent)
      {
         // Find the properties section
         var propertiesSection = ExtractSection(htmlContent, "Properties");
         if (string.IsNullOrEmpty(propertiesSection))
         {
            return null;
         }
         
         // Pattern: <table>...</table>
         var tableMatch = Regex.Match(propertiesSection, @"<table[^>]*>(.*?)</table>", RegexOptions.Singleline);
         return tableMatch.Success ? tableMatch.Groups[1].Value : null;
      }

      /// <summary>
      /// Extract table rows from table HTML
      /// </summary>
      private List<string> ExtractTableRows(string tableHtml)
      {
         var rows = new List<string>();
         
         // Pattern: <tbody>...</tbody>
         var tbodyMatch = Regex.Match(tableHtml, @"<tbody[^>]*>(.*?)</tbody>", RegexOptions.Singleline);
         if (!tbodyMatch.Success)
         {
            return rows;
         }
         
         // Pattern: <tr>...</tr>
         var rowMatches = Regex.Matches(tbodyMatch.Groups[1].Value, @"<tr[^>]*>(.*?)</tr>", RegexOptions.Singleline);
         foreach (Match match in rowMatches)
         {
            rows.Add(match.Groups[1].Value);
         }
         
         return rows;
      }

      /// <summary>
      /// Parse individual property row
      /// </summary>
      private PsetProperty ParsePropertyRow(string rowHtml, string folderPath, Dictionary<ItemsInPsetQtoDefs, string> psetOrQtoSet)
      {
         var cells = ExtractTableCells(rowHtml);
         bool isQtoSet = psetOrQtoSet[ItemsInPsetQtoDefs.PropertySetOrQtoSetDef].Equals("QtoSetDef");
         
         // QTO format: Name | Data Type | Description (3 cells)
         // Pset format: Name | Property Type | Data Type | Description (4 cells)
         int expectedCells = isQtoSet ? 3 : 4;
         
         if (cells.Count < expectedCells)
         {
            throw new Exception($"Property row has insufficient cells (expected {expectedCells}, found {cells.Count})");
         }
         
         try
         {
            var prop = new PsetProperty();
            
            // Extract basic property data
            prop.Name = StripHtmlTags(cells[0]).Trim();
            
            if (isQtoSet)
            {
               // QTO format: Name | Data Type | Description
               string quantityType = ExtractDataTypeLink(cells[1]); // IfcQuantityLength, etc.
               prop.PropertyType = MapQuantityType(quantityType);
            }
            else
            {
               // Pset format: Name | Property Type | Data Type | Description
               string propertyType = ExtractPropertyTypeLink(cells[1]);
               string dataType = ExtractDataTypeLink(cells[2]);
               prop.PropertyType = MapPropertyType(propertyType, dataType, folderPath, psetOrQtoSet);
            }
            
            return prop;
         }
         catch (Exception ex)
         {
            throw new Exception($"Failed to parse property row: {ex.Message}", ex);
         }
      }

      /// <summary>
      /// Extract table cells from row HTML
      /// </summary>
      private List<string> ExtractTableCells(string rowHtml)
      {
         var cells = new List<string>();
         // Pattern: <td>...</td>
         var cellMatches = Regex.Matches(rowHtml, @"<td[^>]*>(.*?)</td>", RegexOptions.Singleline);
         
         foreach (Match match in cellMatches)
         {
            cells.Add(match.Groups[1].Value);
         }
         
         return cells;
      }

      /// <summary>
      /// Extract property type from link
      /// </summary>
      private string ExtractPropertyTypeLink(string cellHtml)
      {
         // Pattern: <a href="IfcPropertySingleValue.htm">IfcPropertySingleValue</a>
         var match = Regex.Match(cellHtml, @"<a href=""(\w+)\.htm"">(\w+)</a>");
         return match.Success ? match.Groups[2].Value : null;
      }

      /// <summary>
      /// Extract data type from link, handling compound types
      /// </summary>
      private string ExtractDataTypeLink(string cellHtml)
      {
         // Pattern: <a href="IfcPowerMeasure.htm">IfcPowerMeasure</a> or <a href="Type1.htm">Type1</a>/<a href="Type2.htm">Type2</a>
         var matches = Regex.Matches(cellHtml, @"<a href=""(\w+)\.htm"">(\w+)</a>");
         
         if (matches.Count == 1)
         {
            return matches[0].Groups[2].Value;
         }
         else if (matches.Count > 1)
         {
            // Compound type - join with "/"
            var types = matches.Cast<Match>().Select(m => m.Groups[2].Value).ToArray();
            return string.Join("/", types);
         }
         
         throw new Exception("Data type not found in HTML table cell");
      }

      /// <summary>
      /// Map HTML quantity type to internal PropertyDataType (for QTO files)
      /// </summary>
      private PropertyDataType MapQuantityType(string htmlQuantityType)
      {
         if (string.IsNullOrEmpty(htmlQuantityType))
         {
            throw new Exception("Quantity type not found in HTML table cell");
         }

         switch (htmlQuantityType.ToLower())
         {
            case "ifcquantitylength":
               return new PropertySingleValue { DataType = "IfcLengthMeasure" };
               
            case "ifcquantityarea":
               return new PropertySingleValue { DataType = "IfcAreaMeasure" };
               
            case "ifcquantityvolume":
               return new PropertySingleValue { DataType = "IfcVolumeMeasure" };
               
            case "ifcquantityweight":
               return new PropertySingleValue { DataType = "IfcMassMeasure" };
               
            case "ifcquantitycount":
               return new PropertySingleValue { DataType = "IfcCountMeasure" };
               
            case "ifcquantitytime":
               return new PropertySingleValue { DataType = "IfcTimeMeasure" };
               
            default:
               // Default fallback
               return new PropertySingleValue { DataType = "IfcLabel" };
         }
      }

      /// <summary>
      /// Map HTML property type to internal PropertyDataType
      /// </summary>
      private PropertyDataType MapPropertyType(string htmlPropertyType, string dataType, string folderPath, Dictionary<ItemsInPsetQtoDefs, string> psetOrQtoSet)
      {
         if (string.IsNullOrEmpty(htmlPropertyType))
         {
            throw new Exception("Property type not found in HTML table cell");
         }
         
         switch (htmlPropertyType.ToLower())
         {
            case "ifcpropertysinglevalue":
               return new PropertySingleValue { DataType = dataType };
               
            case "ifcpropertyenumeratedvalue":
               return ParseEnumeratedValue(dataType, folderPath);
               
            case "ifcpropertytablevalue":
               return ParseTableValue(dataType);
               
            case "ifcpropertyboundedvalue":
               return new PropertyBoundedValue { DataType = dataType };
               
            case "ifcpropertylistvalue":
               return new PropertyListValue { DataType = dataType };
               
            case "ifcpropertyreferencevalue":
               return new PropertyReferenceValue { RefEntity = dataType };
               
            default:
               throw new Exception($"Unknown property type: {htmlPropertyType}");
         }
      }

      /// <summary>
      /// Parse enumerated value with caching
      /// </summary>
      private PropertyEnumeratedValue ParseEnumeratedValue(string enumName, string folderPath)
      {
         var pev = new PropertyEnumeratedValue();
         pev.Name = enumName; // e.g., "PEnum_CoreColoursEnum"
         
         // Check cache first
         if (enumCache.ContainsKey(enumName))
         {
            pev.EnumDef = enumCache[enumName];
            return pev;
         }
         
         // Load from HTML file
         string enumFilePath = Path.Combine(folderPath, enumName + ".htm");
         if (File.Exists(enumFilePath))
         {
            var enumItems = ParseEnumFile(enumFilePath);
            enumCache[enumName] = enumItems; // Cache for next use
            pev.EnumDef = enumItems;
         }
         else
         {
            throw new Exception($"Enumeration file not found: {enumFilePath}");
         }
         
         return pev;
      }

      /// <summary>
      /// Parse enumeration file
      /// </summary>
      private List<PropertyEnumItem> ParseEnumFile(string enumFilePath)
      {
         try
         {
            string htmlContent = File.ReadAllText(enumFilePath);
            var enumItems = new List<PropertyEnumItem>();
            
            // Pattern: <td><code>BLACK</code></td>
            var matches = Regex.Matches(htmlContent, @"<td[^>]*><code>([^<]+)</code>");
            
            foreach (Match match in matches)
            {
               var enumItem = new PropertyEnumItem();
               enumItem.EnumItem = match.Groups[1].Value; // BLACK, BLUE, etc.
               enumItem.Aliases = new List<NameAlias>();
               enumItems.Add(enumItem);
            }
            
            if (enumItems.Count == 0)
            {
               throw new Exception("No enumeration values found in file");
            }
            
            return enumItems;
         }
         catch (Exception ex)
         {
            throw new Exception($"Failed to parse enumeration file {enumFilePath}: {ex.Message}", ex);
         }
      }

      /// <summary>
      /// Parse table value with compound data types
      /// </summary>
      private PropertyTableValue ParseTableValue(string compoundDataType)
      {
         var tableValue = new PropertyTableValue();
         
         // Parse compound types like "IfcPowerMeasure/IfcThermodynamicTemperatureMeasure"
         if (compoundDataType.Contains("/"))
         {
            var types = compoundDataType.Split('/');
            tableValue.DefinedValueType = types[0];    // IfcPowerMeasure
            tableValue.DefiningValueType = types[1];   // IfcThermodynamicTemperatureMeasure
         }
         else
         {
            tableValue.DefinedValueType = compoundDataType;
            tableValue.DefiningValueType = compoundDataType;
         }
         
         return tableValue;
      }

      /// <summary>
      /// Extract section content by title
      /// </summary>
      private string ExtractSection(string htmlContent, string sectionTitle)
      {
         // Pattern: <h2><a class="anchor" id="7.2.5.1.3-Properties"></a> 7.2.5.1.3 Properties <a class="link">...</a></h2>
         var pattern = $@"<h2><a class=""anchor""[^>]*></a>\s*[\d\.]+\s+{Regex.Escape(sectionTitle)}\s*<a class=""link""[^>]*>.*?</h2>(.*?)(?=<h2|</div>|$)";
         var match = Regex.Match(htmlContent, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
         
         return match.Success ? match.Groups[1].Value : "";
      }

      /// <summary>
      /// Strip HTML tags from content
      /// </summary>
      private string StripHtmlTags(string html)
      {
         // Pattern: <tag> or <tag attribute="value">
         return Regex.Replace(html, @"<[^>]+>", "").Trim();
      }
   }
}
