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
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using System.Text.RegularExpressions;


namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Provides static methods for naming and string related operations.
   /// </summary>
   public class NamingUtil
   {
      private static IDictionary<string, Tuple<ElementId, int>> m_NameIncrNumberDict = new Dictionary<string, Tuple<ElementId, int>>();

      public static void InitNameIncrNumberCache()
      {
         m_NameIncrNumberDict.Clear();
      }

      /// <summary>
      /// Removes spaces in a string.
      /// </summary>
      /// <param name="originalString">The original string.</param>
      /// <returns>The string without spaces.</returns>
      public static string RemoveSpaces(string originalString)
      {
         return originalString.Replace(" ", null);
      }

      /// <summary>
      /// Removes underscores in a string.
      /// </summary>
      /// <param name="originalString">The original string.</param>
      /// <returns>The string without underscores.</returns>
      public static string RemoveUnderscores(string originalString)
      {
         return originalString.Replace("_", null);
      }

      /// <summary>
      /// Removes spaces and underscores in a string.
      /// </summary>
      /// <param name="originalString">The original string.</param>
      /// <returns>The string without spaces or underscores.</returns>
      public static string RemoveSpacesAndUnderscores(string originalString)
      {
         return originalString.Replace(" ", null).Replace("_", null);
      }

      /// <summary>
      /// Checks if two strings are equal ignoring case and spaces.
      /// </summary>
      /// <param name="string1">The string to be compared.</param>
      /// <param name="string2">The other string to be compared.</param>
      /// <returns>True if they are equal, false otherwise.</returns>
      public static bool IsEqualIgnoringCaseAndSpaces(string string1, string string2)
      {
         if (string1 == null || string2 == null)
            return (string1 == string2);

         string nospace1 = RemoveSpaces(string1);
         string nospace2 = RemoveSpaces(string2);
         return (string.Compare(nospace1, nospace2, true) == 0);
      }

      /// <summary>
      /// Checks if two strings are equal ignoring case, spaces and underscores.
      /// </summary>
      /// <param name="string1">
      /// The string to be compared.
      /// </param>
      /// <param name="string2">
      /// The other string to be compared.
      /// </param>
      /// <returns>
      /// True if they are equal, false otherwise.
      /// </returns>
      public static bool IsEqualIgnoringCaseSpacesAndUnderscores(string string1, string string2)
      {
         string nospaceOrUndescore1 = RemoveUnderscores(RemoveSpaces(string1));
         string nospaceOrUndescore2 = RemoveUnderscores(RemoveSpaces(string2));
         return (string.Compare(nospaceOrUndescore1, nospaceOrUndescore2, true) == 0);
      }

      /// <summary>
      /// Gets override string value from element parameter.
      /// </summary>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <param name="paramName">
      /// The parameter name.
      /// </param>
      /// <param name="originalValue">
      /// The original value.
      /// </param>
      /// <returns>
      /// The string contains the string value.
      /// </returns>
      public static string GetOverrideStringValue(Element element, string paramName, string originalValue)
      {
         string propertyValue;
         string paramValue;

         if (element != null)
         {
            if (ParameterUtil.GetStringValueFromElement(element, paramName, out paramValue) != null && !string.IsNullOrEmpty(paramValue))
            {
               object strValue = null;
               ParamExprResolver.CheckForParameterExpr(paramValue, element, paramName, ParamExprResolver.ExpectedValueEnum.STRINGVALUE, out strValue);
               if (strValue != null && strValue is string)
                  propertyValue = strValue as string;
               else
                  propertyValue = paramValue;   // return the original paramValue

               //return paramValue;
               return propertyValue;
            }
         }

         return originalValue;
      }

      /// <summary>
      /// Gets override name from element.
      /// </summary>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <param name="originalValue">
      /// The original value.
      /// </param>
      /// <returns>
      /// The string contains the name string value.
      /// </returns>
      public static string GetNameOverride(Element element, string originalValue)
      {
         string nameOverride = "IfcName";
         // CQ_TODO: Understand the naming here and possible use GetCleanName - have it as UI option?

         string overrideValue = GetOverrideStringValue(element, nameOverride, originalValue);

         // For backward compatibility where the old parameter name may still be use (NameOverride), handle it also if the above returns nothing
         if (string.IsNullOrEmpty(overrideValue)
            || (!string.IsNullOrEmpty(overrideValue) && overrideValue.Equals(originalValue)))
         { 
            nameOverride = "NameOverride";
            overrideValue = GetOverrideStringValue(element, nameOverride, originalValue);
         }

         if (element is ElementType || element is FamilySymbol)
         {
            if (string.IsNullOrEmpty(overrideValue)
               || (!string.IsNullOrEmpty(overrideValue) && overrideValue.Equals(originalValue)))
         {
            nameOverride = "IfcName[Type]";
            overrideValue = GetOverrideStringValue(element, nameOverride, originalValue);
         }
      }

         // CQ_TODO: Understand the naming here and possible use GetCleanName - have it as UI option?
         //overrideValue = GetCleanName(overrideValue);
         //GetOverrideStringValue will return the override value from the parameter specified, otherwise it will return the originalValue
         return overrideValue;
      }
      public static string GetNameOverride(IFCAnyHandle handle, Element element, string originalValue)
      {
         List<Exporter.PropertySet.AttributeEntry> entries = ExporterCacheManager.AttributeCache.GetEntry(handle, Exporter.PropertySet.PropertyType.Label, "Name");
         if (entries != null)
         {
            foreach (Exporter.PropertySet.AttributeEntry entry in entries)
            {
               string result = entry.AsString(element);
               if (result != null)
                  return result;
            }
         }
         return GetNameOverride(element, originalValue);
      }

      private static System.Text.RegularExpressions.Regex g_rxMixedName = null;

      private static string GetCleanName(string currentName)
      {
         if (g_rxMixedName == null)
         {
            g_rxMixedName = new System.Text.RegularExpressions.Regex(@"([^:]+)+", System.Text.RegularExpressions.RegexOptions.Compiled);
         }

         if (string.IsNullOrEmpty(currentName)) return currentName;

         System.Text.RegularExpressions.MatchCollection mc = g_rxMixedName.Matches(currentName);
         if (mc.Count > 2)
         {
            return mc[0].Value + ":" + mc[1].Value;
         }
         return currentName;
      }

      /// <summary>
      /// Gets override long name from element.
      /// </summary>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <param name="originalValue">
      /// The original value.
      /// </param>
      /// <returns>
      /// The string contains the long name string value.
      /// </returns>
      public static string GetLongNameOverride(Element element, string originalValue)
      {
         string longNameOverride = "IfcLongName";
         string overrideValue = GetOverrideStringValue(element, longNameOverride, originalValue);

         // For backward compatibility where the old parameter name may still be use (LongNameOverride), handle it also if the above returns nothing
         if (string.IsNullOrEmpty(overrideValue)
            || (!string.IsNullOrEmpty(overrideValue) && overrideValue.Equals(originalValue)))
         {
            longNameOverride = "LongNameOverride";
            overrideValue = GetOverrideStringValue(element, longNameOverride, originalValue);
         }
         //GetOverrideStringValue will return the override value from the parameter specified, otherwise it will return the originalValue
         return overrideValue;
      }
      public static string GetLongNameOverride(IFCAnyHandle handle, Element element, string originalValue)
      {
         List<Exporter.PropertySet.AttributeEntry> entries = ExporterCacheManager.AttributeCache.GetEntry(handle, Exporter.PropertySet.PropertyType.Text, "LongName");
         if (entries != null)
         {
            foreach (Exporter.PropertySet.AttributeEntry entry in entries)
            {
               string result = entry.AsString(element);
               if (result != null)
                  return result;
            }
         }
         return GetLongNameOverride(element, originalValue);
      }

      /// <summary>
      /// Gets override description from element.
      /// </summary>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <param name="originalValue">
      /// The original value.
      /// </param>
      /// <returns>
      /// The string contains the description string value.
      /// </returns>
      public static string GetDescriptionOverride(Element element, string originalValue)
      {
         string nameOverride = "IfcDescription";
         string overrideValue = GetOverrideStringValue(element, nameOverride, originalValue);

         // For backward compatibility where the old parameter name may still be use (DescriptionOverride), handle it also if the above returns nothing
         if (string.IsNullOrEmpty(overrideValue)
            || (!string.IsNullOrEmpty(overrideValue) && overrideValue.Equals(originalValue)))
         {
            overrideValue = GetOverrideStringValue(element, "DescriptionOverride", originalValue);
         }

         if (element is ElementType || element is FamilySymbol)
         {
            if (string.IsNullOrEmpty(overrideValue)
               || (!string.IsNullOrEmpty(overrideValue) && overrideValue.Equals(originalValue)))
            {
               nameOverride = "IfcDescription[Type]";
               overrideValue = GetOverrideStringValue(element, nameOverride, originalValue);
            }
         }
         //GetOverrideStringValue will return the override value from the parameter specified, otherwise it will return the originalValue
         return overrideValue;
      }

      public static string GetDescriptionOverride(IFCAnyHandle handle, Element element, string originalValue)
      {
         List<Exporter.PropertySet.AttributeEntry> entries = ExporterCacheManager.AttributeCache.GetEntry(handle, Exporter.PropertySet.PropertyType.Text, "Description");
         if (entries != null)
         {
            foreach (Exporter.PropertySet.AttributeEntry entry in entries)
            {
               string result = entry.AsString(element);
               if (result != null)
                  return result;
            }
         }
         return GetDescriptionOverride(element, originalValue);
      }
      /// <summary>
      /// Gets override object type from element.
      /// </summary>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <param name="originalValue">
      /// The original value.
      /// </param>
      /// <returns>The string contains the object type string value.</returns>
      public static string GetObjectTypeOverride(Element element, string originalValue)
      {
         string objectTypeOverride = "IfcObjectType";
         string overrideValue = GetOverrideStringValue(element, objectTypeOverride, originalValue);

         // For backward compatibility where the old parameter name may still be use (ObjectTypeOverride), handle it also if the above returns nothing
         if (string.IsNullOrEmpty(overrideValue)
            || (!string.IsNullOrEmpty(overrideValue) && overrideValue.Equals(originalValue)))
         {
            overrideValue = GetOverrideStringValue(element, "ObjectTypeOverride", originalValue);
         }

         // The following is a special treatment for ObjectType. If IfcObjectType is not set, or carrying the original value, 
         //   check the existence of IfcObjectType[Type] parameter for the ElementType or the FamilySymbol. If it is set, the 
         //   ObjectType attribute will be overridden by that value, allowing ObjectType is set according to the Type setting 
         //   such as IfcExportType="USERDEFINED" set in the Type which necessitates ObjectType in the instance to be set to
         //   the appropriate value
         if (string.IsNullOrEmpty(overrideValue)
            || (!string.IsNullOrEmpty(overrideValue) && overrideValue.Equals(originalValue)))
         {
            Element typeOrSymbol = element.Document.GetElement(element.GetTypeId()) as ElementType; ;
            if (typeOrSymbol == null)
            {
               FamilyInstance famInst = element as FamilyInstance;
               if (famInst != null)
                  typeOrSymbol = famInst.Symbol;
            }

            if (typeOrSymbol != null)
            {
               objectTypeOverride = "IfcObjectType[Type]";
               overrideValue = GetOverrideStringValue(typeOrSymbol, objectTypeOverride, originalValue);
            }
         }
         //GetOverrideStringValue will return the override value from the parameter specified, otherwise it will return the originalValue
         return overrideValue;
      }

      /// <summary>
      /// Get ObjectType override
      /// </summary>
      /// <param name="handle">the object Handle</param>
      /// <param name="element">the element</param>
      /// <param name="originalValue">the original value</param>
      /// <returns>the override value or the original</returns>
      public static string GetObjectTypeOverride(IFCAnyHandle handle, Element element, string originalValue)
      {
         List<Exporter.PropertySet.AttributeEntry> entries = ExporterCacheManager.AttributeCache.GetEntry(handle, Exporter.PropertySet.PropertyType.Label, "ObjectType");
         if (entries != null)
         {
            foreach (Exporter.PropertySet.AttributeEntry entry in entries)
            {
               string result = entry.AsString(element);
               if (result != null)
                  return result;
            }
         }
         return GetObjectTypeOverride(element, originalValue);
      }
      /// <summary>
      /// Gets Tag override from element.
      /// </summary>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <param name="originalValue">
      /// The original value.
      /// </param>
      /// <returns>The string contains the object type string value.</returns>
      public static string GetTagOverride(Element element, string originalValue)
      {
         string nameOverride = "IfcTag";
         string overrideValue = GetOverrideStringValue(element, nameOverride, originalValue);
         if (string.IsNullOrEmpty(overrideValue)
            || (!string.IsNullOrEmpty(overrideValue) && overrideValue.Equals(originalValue)))
         {
            overrideValue = GetOverrideStringValue(element, "TagOverride", originalValue);
         }

            if (element is ElementType || element is FamilySymbol)
         {
            if (string.IsNullOrEmpty(overrideValue)
               || (!string.IsNullOrEmpty(overrideValue) && overrideValue.Equals(originalValue)))
         {
            nameOverride = "IfcTag[Type]";
            overrideValue = GetOverrideStringValue(element, nameOverride, originalValue);
         }
         }
         return overrideValue;
      }

      /// <summary>
      /// Get ElementType attribute override
      /// </summary>
      /// <param name="element">the element (should be the Type)</param>
      /// <param name="originalValue">the original value</param>
      /// <returns>the string that contains the ElementType attribute value</returns>
      public static string GetElementTypeOverride(Element element, string originalValue)
      {
         string nameOverride = "IfcElementType";
         string overrideValue = GetOverrideStringValue(element, nameOverride, originalValue);
         if (element is ElementType || element is FamilySymbol)
         {
            if (string.IsNullOrEmpty(overrideValue)
               || (!string.IsNullOrEmpty(overrideValue) && overrideValue.Equals(originalValue)))
            {
               nameOverride = "IfcElementType[Type]";
               overrideValue = GetOverrideStringValue(element, nameOverride, originalValue);
            }
         }
         return overrideValue;
      }

      /// <summary>
      /// Generates the IFC name for the current element.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <returns> The string containing the name.</returns>
      static private string GetIFCBaseName(Element element)
      {
         if (element == null)
            return "";

         bool isType = (element is ElementType);

         string elementName = element.Name;
         if (elementName == "???")
            elementName = "";

         string familyName = "";
         ElementType elementType = (isType ? element : element.Document.GetElement(element.GetTypeId())) as ElementType;
         if (elementType != null)
         {
            familyName = elementType.FamilyName;
            if (familyName == "???")
               familyName = "";
         }

         string fullName = familyName;
         if (elementName != "")
         {
            // if it is a type and the name is set and the option is selected, use the name only
            if (isType && ExporterCacheManager.ExportOptionsCache.NamingOptions.UseTypeNameOnlyForIfcType)
               fullName = elementName;
            else
            {
            if (fullName != "")
               fullName = fullName + ":" + elementName;
            else
               fullName = elementName;
            }
         }

         if (isType)
            return fullName;
         if (fullName != "")
            return fullName + ":" + CreateIFCElementId(element);
         return CreateIFCElementId(element);
      }

      /// <summary>
      /// Generates the IFC name based on the Revit display name.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <returns> The string containing the name.</returns>
      static private string GetRevitDisplayName(Element element)
      {
         if (element == null)
            return "";

         string fullName = (element.Category != null) ? element.Category.Name : "";
         string typeName = element.Name;
         string familyName = "";

         ElementType elementType = null;
         if (element is ElementType)
            elementType = element as ElementType;
         else
            elementType = element.Document.GetElement(element.GetTypeId()) as ElementType;

         if (elementType != null)
            familyName = elementType.FamilyName;

         if (familyName != "")
         {
            if (fullName != "")
               fullName = fullName + " : " + familyName;
            else
               fullName = familyName;
         }

         if (typeName != "")
         {
            if (fullName != "")
               fullName = fullName + " : " + typeName;
            else
               fullName = typeName;
         }

         return fullName;
      }

      /// <summary>
      /// Special name format as required by COBie v2.4
      /// </summary>
      /// <param name="element">the Element</param>
      /// <returns>the name</returns>
      static private string GetCOBieDesiredName(Element element)
      {
         if (element == null)
            return "";

         Parameter instanceMarkParam = element.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
         // if NULL, try DOOR_NUMBER which also has the same parameter name "Mark"
         if (instanceMarkParam == null)
            instanceMarkParam = element.get_Parameter(BuiltInParameter.DOOR_NUMBER);

         ElementType elementType = null;

         if (element is ElementType)
            elementType = element as ElementType;
         else
            elementType = element.Document.GetElement(element.GetTypeId()) as ElementType;

         Parameter typeMarkParam = null;
         if (elementType != null)
         {
            typeMarkParam = elementType.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_MARK);
            // if NULL, try WINDOW_TYPE_ID which also has the same parameter name "Mark"
            if (typeMarkParam == null)
               typeMarkParam = elementType.get_Parameter(BuiltInParameter.WINDOW_TYPE_ID);
         }

         string typeMarkValue = (typeMarkParam != null) ? typeMarkParam.AsString() : null;
         string instanceMarkValue = (instanceMarkParam != null) ? instanceMarkParam.AsString() : null;

         string fullName = null;
         if (string.IsNullOrWhiteSpace(typeMarkValue))
            fullName = string.IsNullOrWhiteSpace(instanceMarkValue) ? "" : instanceMarkValue;
         else if (string.IsNullOrWhiteSpace(instanceMarkValue))
            fullName = typeMarkValue;
         else
            fullName = typeMarkValue + "-" + instanceMarkValue;

         Tuple<ElementId, int> tupNameDupl;
         if (!m_NameIncrNumberDict.TryGetValue(fullName, out tupNameDupl))
            m_NameIncrNumberDict.Add(fullName, new Tuple<ElementId, int>(element.Id, 1));
         else
         {
            // Found the same name used before, if not the same ElementId than the previously processed, add an incremental number to name
            if (!element.Id.Equals(tupNameDupl.Item1))
            {
               Tuple<ElementId, int> tup = new Tuple<ElementId, int>(element.Id, tupNameDupl.Item2 + 1);
               m_NameIncrNumberDict[fullName] = tup;
               fullName = fullName + " (" + tup.Item2.ToString() + ")";
            }
         }
         return fullName;
      }

      /// <summary>
      /// Get the IFC name of an element.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <returns>The name.</returns>
      static public string GetIFCName(Element element)
      {
         if (element == null)
            return "";

         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3COBIE24DesignDeliverable)
            return GetCOBieDesiredName(element);

         if (ExporterCacheManager.ExportOptionsCache.NamingOptions.UseVisibleRevitNameAsEntityName)
            return GetRevitDisplayName(element);

         string baseName = GetIFCBaseName(element);
         return GetNameOverride(element, baseName);
      }

      /// <summary>
      /// Creates an IFC name for an element, with a suffix.
      /// </summary>
      /// <param name="element">The element./// </param>
      /// <param name="index">/// The index of the name. If it is larger than 0, it is appended to the name./// </param>
      /// <returns>/// The string contains the name string value./// </returns>
      public static string GetIFCNamePlusIndex(Element element, int index)
      {
         string elementName = GetIFCName(element);
         if (index >= 0)
         {
            elementName += ":";
            elementName += index.ToString();
         }

         return elementName;
      }

      public static string GetFamilyAndTypeName(Element element)
      {
         if (element == null)
            return null;

         string familyName = null;
         string typeName = null;

         ElementType elementType = element.Document.GetElement(element.GetTypeId()) as ElementType;
         if (elementType != null)
         {
            typeName = elementType.Name;
            if (typeName == "???")
               typeName = "";

            familyName = elementType.FamilyName;
            if (familyName == "???")
               familyName = "";
         }

         // set famSym name.
         if (!string.IsNullOrEmpty(familyName))
         {
            if (!string.IsNullOrEmpty(typeName))
               return familyName + ":" + typeName;

            return familyName;
         }

         return typeName;
      }

      /// <summary>
      /// Creates an IFC object name from export state.
      /// </summary>
      /// <remarks>It is combined with family name and element type id.</remarks>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <returns>The name string value, or null if there is none.</returns>
      public static string CreateIFCObjectName(ExporterIFC exporterIFC, Element element)
      {
         // This maintains the same behavior as the previous export.
         if (element is FamilyInstance)
         {
            FamilySymbol familySymbol = (element as FamilyInstance).Symbol;
            if (familySymbol != null)
               return familySymbol.Name;
         }

         ElementId typeId = element != null ? element.GetTypeId() : ElementId.InvalidElementId;

         string objectName = GetFamilyAndTypeName(element);
         if (typeId != ElementId.InvalidElementId)
         {
            if (objectName == "")
               return typeId.ToString();
            else
               return (objectName + ":" + typeId.ToString());
         }

         return null;
      }

      /// <summary>
      /// Creates an IFC element id string from element id.
      /// </summary>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <returns>
      /// The string contains the name string value.
      /// </returns>
      public static string CreateIFCElementId(Element element)
      {
         if (element == null)
            return "NULL";

         string elemIdString = element.Id.ToString();
         return elemIdString;
      }

      /// <summary>
      /// Parses the name string and gets the parts separately.
      /// </summary>
      /// <param name="name">
      /// The name.
      /// </param>
      /// <param name="lastName">
      /// The output last name.
      /// </param>
      /// <param name="firstName">
      /// The output first name.
      /// </param>
      /// <param name="middleNames">
      /// The output middle names.
      /// </param>
      /// <param name="prefixTitles">
      /// The output prefix titles.
      /// </param>
      /// <param name="suffixTitles">
      /// The output suffix titles.
      /// </param>
      public static void ParseName(string name, out string lastName, out string firstName, out List<string> middleNames, out List<string> prefixTitles, out List<string> suffixTitles)
      {
         lastName = string.Empty;
         firstName = string.Empty;
         middleNames = null;
         prefixTitles = null;
         suffixTitles = null;

         if (String.IsNullOrEmpty(name))
            return;

         string currName = name;
         List<string> names = new List<string>();
         int noEndlessLoop = 0;
         int index = 0;
         bool foundComma = false;

         do
         {
            int currIndex = index;   // index might get reset by comma.

            currName = currName.TrimStart(' ');
            if (String.IsNullOrEmpty(currName))
               break;

            int comma = foundComma ? currName.Length : currName.IndexOf(',');
            if (comma == -1) comma = currName.Length;
            int space = currName.IndexOf(' ');
            if (space == -1) space = currName.Length;

            // treat comma as space, mark found.
            if (comma < space)
            {
               foundComma = true;
               index = -1; // start inserting at the beginning again.
               space = comma;
            }

            if (space == currName.Length)
            {
               names.Add(currName);
               break;
            }
            else if (space == 0)
            {
               if (comma == 0)
                  continue;
               else
                  break;   // shouldn't happen
            }

            names.Insert(currIndex, currName.Substring(0, space));
            index++;
            currName = currName.Substring(space + 1);
            noEndlessLoop++;

         } while (noEndlessLoop < 100);


         // parse names.
         // assuming anything ending in a dot is a prefix.
         int sz = names.Count;
         for (index = 0; index < sz; index++)
         {
            if (names[index].LastIndexOf('.') == names[index].Length - 1)
            {
               if (prefixTitles == null)
                  prefixTitles = new List<string>();
               prefixTitles.Add(names[index]);
            }
            else
               break;
         }

         if (index < sz)
         {
            firstName = names[index++];
         }

         // suffixes, if any.  Note this misses "III", "IV", etc., but this is not that important!
         int lastIndex;
         for (lastIndex = sz - 1; lastIndex >= index; lastIndex--)
         {
            if (names[lastIndex].LastIndexOf('.') == names[lastIndex].Length - 1)
            {
               if (suffixTitles == null)
                  suffixTitles = new List<string>();
               suffixTitles.Insert(0, names[lastIndex]);
            }
            else
               break;
         }

         if (lastIndex >= index)
         {
            lastName = names[lastIndex--];
         }

         // rest are middle names.
         for (; index <= lastIndex; index++)
         {
            if (middleNames == null)
               middleNames = new List<string>();
            middleNames.Add(names[index]);
         }
      }

      /// <summary>
      /// Get an IFC Profile Name from the FamilySymbol (the type) name, or from the override parameter of the type "IfcProfileName[Type]"
      /// </summary>
      /// <param name="element">the element (Instance/FamilyInstance or Type/FamilySymbol)</param>
      /// <returns>the profile name</returns>
      public static string GetProfileName(Element element, string originalName = null)
      {
         FamilySymbol fSymb;
         if (element is FamilyInstance)
            fSymb = (element as FamilyInstance).Symbol;
         else
            fSymb = element as FamilySymbol;

         if (fSymb == null)
            return originalName;

         // Get a profile name. It is by default set to the type (familySymbol) name, but can be overridden by IfcProfileName[Type] shared parameter
         string profileName = fSymb.Name;
         string profile;
         ParameterUtil.GetStringValueFromElement(fSymb, "IfcProfileName[Type]", out profile);
         if (!string.IsNullOrEmpty(profile))
            profileName = profile;

         if (string.IsNullOrEmpty(profileName))
            return originalName;

         return profileName;
      }

      ///// <summary>
      ///// Get unique name by incrementing a number as suffix. It is looking for "name + (number)". It works like this:
      ///// - if the entry is "name" without number, it will return "name (2)"
      ///// - if the entry is "name (number)", it will return "name (number + 1)"
      ///// Only the last (number) will be evaluated in the case that there are multiple (number) in the string
      ///// </summary>
      ///// <param name="nameToCheck">the name to check</param>
      ///// <returns>the name with incremented number appended</returns>
      //public static string GetUniqueNameByIncrement(string nameToCheck)
      //{
      //   string uniqueName = nameToCheck;
      //   string baseName = null;
      //   string suffix = null;
      //   string prefix = null;
      //   int number = 1;

      //   // Looking for pattern "... name (number)". If the last part is number in bracket, this number will be incremented
      //   Regex rx = new Regex(@"(?<basename>\w+)\s*[(]\s*(?<number>\d+)\s*[)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
      //   MatchCollection matches = rx.Matches(uniqueName);

      //   // If found matches, increment the number and return 
      //   if (matches.Count > 0)
      //   {
      //      Match lastMatch = matches[matches.Count - 1];
      //      GroupCollection groups = lastMatch.Groups;
      //      baseName = groups["basename"].Value;
      //      number = int.Parse(groups["number"].Value);

      //      int index = lastMatch.Index;
      //      int len = lastMatch.Length;

      //      if (index > 1)
      //      {
      //         prefix = uniqueName.Substring(0, index - 1).Trim();
      //         if (!string.IsNullOrEmpty(prefix))
      //            baseName = prefix + " " + baseName;
      //      }

      //      // It ends with word without number, it will be treated the same for the name without number
      //      if (index + len < uniqueName.Length)
      //      {
      //         suffix = uniqueName.Substring(index + len).Trim();
      //         if (!string.IsNullOrEmpty(suffix))
      //            uniqueName = uniqueName + " (" + (++number).ToString() + ")";
      //         else
      //         {
      //            number = int.Parse(groups["number"].Value);
      //            uniqueName = baseName + " (" + (++number).ToString() + ")";
      //         }
      //      }
      //      else
      //      {
      //         number = int.Parse(groups["number"].Value);
      //         uniqueName = baseName + " (" + (++number).ToString() + ")";
      //      }
      //   }
      //   else
      //   {
      //      // If no match, return the name plus the incremented number (starting from 2)
      //      uniqueName = uniqueName + " (" + (++number).ToString() + ")";
      //   }

      //   return uniqueName;
      //}

      ///// <summary>
      ///// Check unique name within the given Set. If the name is unique add the name and return it,
      ///// If it is not unique, it will call GetUniqueNameByIncrement() and check the new name for its existence in the Set, until it finds the unique name
      ///// </summary>
      ///// <param name="inputName">the input name</param>
      ///// <param name="theNameSet">the Set where the name should be search</param>
      ///// <returns>the unique name that is also added into the Set</returns>
      //public static string GetUniqueNameWithinSet(string inputName, ref HashSet<string> theNameSet)
      //{
      //   string uniqueName = inputName;
      //   if (!theNameSet.Contains(uniqueName))
      //   {
      //      theNameSet.Add(uniqueName);
      //      return uniqueName;
      //   }

      //   while (true)
      //   {
      //      uniqueName = GetUniqueNameByIncrement(uniqueName);
      //      if (!theNameSet.Contains(uniqueName))
      //      {
      //         theNameSet.Add(uniqueName);
      //         break;
      //      }
      //   }

      //   return uniqueName;
      //}
   }
}