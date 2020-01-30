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
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using System.IO;
using Revit.IFC.Common.Utility;
using System.Xml.Linq;
using RevitIFCTools.PropertySet;

namespace RevitIFCTools
{
   public class SharedParameterDef : ICloneable
   {
      public string Param { get; set; } = "PARAM";
      public Guid ParamGuid { get; set; }
      public string Name { get; set; }
      public string ParamType { get; set; }
      public string DataCategory { get; set; }
      public int GroupId { get; set; } = 2;
      public bool Visibility { get; set; } = true;
      public string Description { get; set; }
      public bool UserModifiable { get; set; } = true;

      public virtual object Clone()
      {
         SharedParameterDef clonePar = new SharedParameterDef();
         clonePar.Param = this.Param;
         clonePar.ParamGuid = this.ParamGuid;
         clonePar.Name = this.Name;
         clonePar.ParamType = this.ParamType;
         clonePar.DataCategory = this.DataCategory;
         clonePar.GroupId = this.GroupId;
         clonePar.Visibility = this.Visibility;
         clonePar.Description = this.Description;
         clonePar.UserModifiable = this.UserModifiable;
         return clonePar;
      }
   }

   public class VersionSpecificPropertyDef
   {
      public string SchemaFileVersion { get; set; }
      public string IfcVersion { get; set; }
      public PsetDefinition PropertySetDef { get; set; }
   }

   public class ProcessPsetDefinition
   {
      IDictionary<string, StreamWriter> enumFileDict;
      IDictionary<string, IList<string>> enumDict;
      public SortedDictionary<string, IList<VersionSpecificPropertyDef>> allPDefDict { get; private set; } = new SortedDictionary<string, IList<VersionSpecificPropertyDef>>();
#if DEBUG
      StreamWriter logF;
#endif

      public static IDictionary<string, SharedParameterDef> SharedParamFileDict  { get; set; } = new Dictionary<string, SharedParameterDef>();
      public static IDictionary<string, SharedParameterDef> SharedParamFileTypeDict { get; set; } = new Dictionary<string, SharedParameterDef>();

      public ProcessPsetDefinition(StreamWriter logfile)
      {
#if DEBUG
         logF = logfile;
#endif
         enumFileDict = new Dictionary<string, StreamWriter>();
         enumDict = new Dictionary<string, IList<string>>();
      }

      void AddPsetDefToDict(string schemaVersionName, PsetDefinition psetD)
      {
         VersionSpecificPropertyDef psetDefEntry = new VersionSpecificPropertyDef()
         {
            SchemaFileVersion = schemaVersionName,
            IfcVersion = psetD.IfcVersion,
            PropertySetDef = psetD
         };

         if (allPDefDict.ContainsKey(psetD.Name))
         {
            allPDefDict[psetD.Name].Add(psetDefEntry);
         }
         else
         {
            IList<VersionSpecificPropertyDef> vsPropDefList = new List<VersionSpecificPropertyDef>();
            vsPropDefList.Add(psetDefEntry);
            allPDefDict.Add(psetD.Name, vsPropDefList);
         }
      }

      LanguageType checkAliasLanguage(string language)
      {
         if (language.Equals("en-us", StringComparison.CurrentCultureIgnoreCase)
               || language.Equals("en", StringComparison.CurrentCultureIgnoreCase))
            return LanguageType.English_USA;

         if (language.Equals("ja-JP", StringComparison.CurrentCultureIgnoreCase)
               || language.Equals("ja", StringComparison.CurrentCultureIgnoreCase))
            return LanguageType.Japanese;

         if (language.Equals("ko-KR", StringComparison.CurrentCultureIgnoreCase)
            || language.Equals("ko", StringComparison.CurrentCultureIgnoreCase))
            return LanguageType.Korean;

         if (language.Equals("zh-CN", StringComparison.CurrentCultureIgnoreCase)
            || language.Equals("zh-SG", StringComparison.CurrentCultureIgnoreCase)
            || language.Equals("zh-HK", StringComparison.CurrentCultureIgnoreCase))
            return LanguageType.Chinese_Simplified;

         if (language.Equals("zh-TW", StringComparison.CurrentCultureIgnoreCase))
            return LanguageType.Chinese_Traditional;

         if (language.Equals("fr-FR", StringComparison.CurrentCultureIgnoreCase)
            || language.Equals("fr", StringComparison.CurrentCultureIgnoreCase))
            return LanguageType.French;

         if (language.Equals("de-DE", StringComparison.CurrentCultureIgnoreCase)
            || language.Equals("de", StringComparison.CurrentCultureIgnoreCase))
            return LanguageType.German;

         if (language.Equals("es", StringComparison.CurrentCultureIgnoreCase))
            return LanguageType.Spanish;

         if (language.Equals("it", StringComparison.CurrentCultureIgnoreCase))
            return LanguageType.Italian;

         if (language.Equals("nl", StringComparison.CurrentCultureIgnoreCase))
            return LanguageType.Dutch;

         if (language.Equals("ru", StringComparison.CurrentCultureIgnoreCase))
            return LanguageType.Russian;

         if (language.Equals("cs", StringComparison.CurrentCultureIgnoreCase))
            return LanguageType.Czech;

         if (language.Equals("pl", StringComparison.CurrentCultureIgnoreCase))
            return LanguageType.Polish;

         if (language.Equals("hu", StringComparison.CurrentCultureIgnoreCase))
            return LanguageType.Hungarian;

         if (language.Equals("pt-BR", StringComparison.CurrentCultureIgnoreCase))
            return LanguageType.Brazilian_Portuguese;

         return LanguageType.Unknown;
      }

      void writeEnumFile(string IfcVersion, string schemaVersion, string penumName, IList<string> enumValues, string outputFile)
      {
         if (string.IsNullOrEmpty(IfcVersion) || string.IsNullOrEmpty(penumName) || enumValues == null || enumValues.Count == 0)
            return;

         string version = IfcVersion;
         if (IfcVersion.Equals("IFC4", StringComparison.CurrentCultureIgnoreCase))
            version = schemaVersion.ToUpper();

         StreamWriter fileToWrite;
         if (!enumFileDict.TryGetValue(version, out fileToWrite))
         {
            string fileName = Path.Combine(Path.GetDirectoryName(outputFile),
                                 Path.GetFileNameWithoutExtension(outputFile) + version + "Enum.cs");
            if (File.Exists(fileName))
               File.Delete(fileName);
            fileToWrite = new StreamWriter(fileName);
            enumFileDict.Add(version, fileToWrite);

            fileToWrite.WriteLine("using System;");
            fileToWrite.WriteLine("using System.Collections.Generic;");
            fileToWrite.WriteLine("using System.Linq;");
            fileToWrite.WriteLine("using System.Text;");
            fileToWrite.WriteLine("using System.Threading.Tasks;");
            fileToWrite.WriteLine("using Autodesk.Revit;");
            fileToWrite.WriteLine("using Autodesk.Revit.DB;");
            fileToWrite.WriteLine("using Autodesk.Revit.DB.IFC;");
            fileToWrite.WriteLine("using Revit.IFC.Export.Exporter.PropertySet;");
            fileToWrite.WriteLine("using Revit.IFC.Export.Exporter.PropertySet.Calculators;");
            fileToWrite.WriteLine("using Revit.IFC.Export.Utility;");
            fileToWrite.WriteLine("using Revit.IFC.Export.Toolkit;");
            fileToWrite.WriteLine("using Revit.IFC.Common.Enums;");
            fileToWrite.WriteLine("");
            fileToWrite.WriteLine("namespace Revit.IFC.Export.Exporter.PropertySet." + version);
            fileToWrite.WriteLine("{");
            fileToWrite.WriteLine("");
         }

         // Check for duplicate Penum
         string key = version + "." + penumName;
         if (!enumDict.ContainsKey(key))
         {
            enumDict.Add(key, enumValues);
         }
         else
         {
            return;  // the enum already in the Dict, i.e. alreadey written, do not write it again
         }

         fileToWrite.WriteLine("");
         fileToWrite.WriteLine("\tpublic enum " + penumName + " {");
         foreach (string enumV in enumValues)
         {
            string endWith = ",";
            if (enumV == enumValues.Last())
               endWith = "}";

            fileToWrite.WriteLine("\t\t" + enumV + endWith);
         }
      }

      public void endWriteEnumFile()
      {
         foreach (KeyValuePair<string, StreamWriter> enumFile in enumFileDict)
         {
            enumFile.Value.WriteLine("}");
            enumFile.Value.Close();
         }
      }

      string HandleInvalidCharacter(string name)
      {
         // 1. Check for all number or start with number enum name. Rename with _, and assign number as value for all number
         string appendValue = "";
         // If the name consists number only, assig the number value to the enum
         if (Regex.IsMatch(name, @"^\d+$"))
            appendValue = " = " + name;

         // if the enum starts with a number, add prefix to the enum with an underscore
         if (Regex.IsMatch(name, @"^\d"))
            name = "_" + name + appendValue;

         // check for any illegal character and remove them
         name = name.Replace(".", "").Replace("-", "");
         return name;
      }

      public static string removeInvalidNName(string name)
      {
         string[] subNames = name.Split('/', '\\', ' ');
         if (subNames[0].Trim().StartsWith("Ifc", StringComparison.InvariantCultureIgnoreCase))
            return subNames[0].Trim();  // Only returns the name before '/' if any
         else
         {
            foreach (string entName in subNames)
               if (entName.Trim().StartsWith("Ifc", StringComparison.InvariantCultureIgnoreCase))
                  return entName.Trim();
         }
         return null;
      }

      public void processSimpleProperty(StreamWriter outF, PsetProperty prop, string propNamePrefix, string IfcVersion, string schemaVersion, 
         string varName, VersionSpecificPropertyDef vSpecPDef, string outputFile)
      {
         outF.WriteLine("\t\t\t\tifcPSE = new PropertySetEntry(\"{0}\");", prop.Name);
         outF.WriteLine("\t\t\t\tifcPSE.PropertyName = \"{0}\";", prop.Name);
         if (prop.PropertyType != null)
         {
            if (prop.PropertyType is PropertyEnumeratedValue)
            {
               PropertyEnumeratedValue propEnum = prop.PropertyType as PropertyEnumeratedValue;
               outF.WriteLine("\t\t\t\tifcPSE.PropertyType = PropertyType.Label;");
               outF.WriteLine("\t\t\t\tifcPSE.PropertyValueType = PropertyValueType.EnumeratedValue;");
               outF.WriteLine("\t\t\t\tifcPSE.PropertyEnumerationType = typeof(Revit.IFC.Export.Exporter.PropertySet." + IfcVersion + "." + propEnum.Name + ");");
               IList<string> enumItems = new List<string>();
               foreach (PropertyEnumItem enumItem in propEnum.EnumDef)
               {
                  string item = HandleInvalidCharacter(enumItem.EnumItem);
                  enumItems.Add(item);
               }
               writeEnumFile(IfcVersion, schemaVersion, propEnum.Name, enumItems, outputFile);
            }
            else if (prop.PropertyType is PropertyReferenceValue)
            {
               PropertyReferenceValue propRef = prop.PropertyType as PropertyReferenceValue;
               outF.WriteLine("\t\t\t\tifcPSE.PropertyType = PropertyType.{0};", propRef.RefEntity.Trim());
               outF.WriteLine("\t\t\t\tifcPSE.PropertyValueType = PropertyValueType.ReferenceValue;");
            }
            else if (prop.PropertyType is PropertyListValue)
            {
               PropertyListValue propList = prop.PropertyType as PropertyListValue;
               if (propList.DataType != null && !propList.DataType.Equals("IfcValue", StringComparison.InvariantCultureIgnoreCase))
                  outF.WriteLine("\t\t\t\tifcPSE.PropertyType = PropertyType.{0};", propList.DataType.ToString().Replace("Ifc", "").Replace("Measure", "").Trim());
               else
                  outF.WriteLine("\t\t\t\tifcPSE.PropertyType = PropertyType.Label;");    // default to Label if not defined

               outF.WriteLine("\t\t\t\tifcPSE.PropertyValueType = PropertyValueType.ListValue;");
            }
            else if (prop.PropertyType is PropertyTableValue)
            {
               PropertyTableValue propTab = prop.PropertyType as PropertyTableValue;
               // TableValue has 2 types: DefiningValue and DefinedValue. This is not fully implemented yet
               if (propTab.DefinedValueType != null)
                  outF.WriteLine("\t\t\t\tifcPSE.PropertyType = PropertyType.{0};", propTab.DefinedValueType.ToString().Replace("Ifc", "").Replace("Measure", "").Trim());
               else
                  outF.WriteLine("\t\t\t\tifcPSE.PropertyType = PropertyType.Label;");    // default to Label if missing

               outF.WriteLine("\t\t\t\tifcPSE.PropertyValueType = PropertyValueType.TableValue;");
            }
            else
               outF.WriteLine("\t\t\t\tifcPSE.PropertyType = PropertyType.{0};", prop.PropertyType.ToString().Replace("Ifc", "").Replace("Measure", "").Trim());
         }
         else
         {
            prop.PropertyType = new PropertySingleValue();
            // Handle bad cases where datatype is somehow missing in the PSD
            if (prop.Name.ToLowerInvariant().Contains("ratio")
               || prop.Name.ToLowerInvariant().Contains("length")
               || prop.Name.ToLowerInvariant().Contains("width")
               || prop.Name.ToLowerInvariant().Contains("thickness")
               || prop.Name.ToLowerInvariant().Contains("angle")
               || prop.Name.ToLowerInvariant().Contains("transmittance")
               || prop.Name.ToLowerInvariant().Contains("fraction")
               || prop.Name.ToLowerInvariant().Contains("rate")
               || prop.Name.ToLowerInvariant().Contains("velocity")
               || prop.Name.ToLowerInvariant().Contains("speed")
               || prop.Name.ToLowerInvariant().Contains("capacity")
               || prop.Name.ToLowerInvariant().Contains("pressure")
               || prop.Name.ToLowerInvariant().Contains("temperature")
               || prop.Name.ToLowerInvariant().Contains("power")
               || prop.Name.ToLowerInvariant().Contains("heatgain")
               || prop.Name.ToLowerInvariant().Contains("efficiency")
               || prop.Name.ToLowerInvariant().Contains("resistance")
               || prop.Name.ToLowerInvariant().Contains("coefficient")
               || prop.Name.ToLowerInvariant().Contains("measure"))
               (prop.PropertyType as PropertySingleValue).DataType = "IfcReal";
            else if (prop.Name.ToLowerInvariant().Contains("loadbearing"))
               (prop.PropertyType as PropertySingleValue).DataType = "IfcBoolean";
            else if (prop.Name.ToLowerInvariant().Contains("reference"))
               (prop.PropertyType as PropertySingleValue).DataType = "IfcIdentifier";
            else
               (prop.PropertyType as PropertySingleValue).DataType = "IfcLabel";
#if DEBUG
            logF.WriteLine("%Warning: " + prop.Name + " from " + vSpecPDef.PropertySetDef.Name + "(" + vSpecPDef.SchemaFileVersion + ") is missing PropertyType/datatype. Set to default "
                  + (prop.PropertyType as PropertySingleValue).DataType);
#endif
         }

         // Append new definition to the Shared parameter file
         SharedParameterDef newPar = new SharedParameterDef();
         newPar.Name = prop.Name;

         // Use IfdGuid for the GUID if defined
         Guid pGuid = Guid.Empty;
         bool hasIfdGuid = false;
         if (!string.IsNullOrEmpty(prop.IfdGuid))
         {
            if (Guid.TryParse(prop.IfdGuid, out pGuid))
               hasIfdGuid = true;
         }
         if (pGuid == Guid.Empty)
            pGuid = Guid.NewGuid();

         newPar.ParamGuid = pGuid;

         if (prop.PropertyType != null)
            newPar.Description = prop.PropertyType.ToString().Split(' ', '\t')[0].Trim();     // Put the original IFC datatype in the description
         else
         {
#if DEBUG
            logF.WriteLine("%Warning: " + prop.Name + " from " + vSpecPDef.PropertySetDef.Name + "(" + vSpecPDef.SchemaFileVersion + ") is missing PropertyType/datatype.");
#endif
         }

         if (prop.PropertyType is PropertyEnumeratedValue)
         {
            newPar.ParamType = "TEXT";    // Support only a single enum value (which is most if not all cases known)
         }
         else if (prop.PropertyType is PropertyReferenceValue
            || prop.PropertyType is PropertyBoundedValue
            || prop.PropertyType is PropertyListValue
            || prop.PropertyType is PropertyTableValue)
         {
            // For all the non-simple value, a TEXT parameter will be created that will contain formatted string
            newPar.ParamType = "MULTILINETEXT";
            if (prop.PropertyType is PropertyBoundedValue)
               newPar.Description = "PropertyBoundedValue";   // override the default to the type of property datatype
            else if (prop.PropertyType is PropertyListValue)
               newPar.Description = "PropertyListValue";   // override the default to the type of property datatype
            else if (prop.PropertyType is PropertyTableValue)
               newPar.Description = "PropertyTableValue";   // override the default to the type of property datatype
         }
         else if (prop.PropertyType is PropertySingleValue)
         {
            PropertySingleValue propSingle = prop.PropertyType as PropertySingleValue;
            newPar.Description = propSingle.DataType; // Put the original IFC datatype in the description

            if (propSingle.DataType.Equals("IfcPositivePlaneAngleMeasure", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcSolidAngleMeasure", StringComparison.InvariantCultureIgnoreCase))
               newPar.ParamType = "ANGLE";
            else if (propSingle.DataType.Equals("IfcAreaMeasure", StringComparison.InvariantCultureIgnoreCase))
               newPar.ParamType = "AREA";
            else if (propSingle.DataType.Equals("IfcMonetaryMeasure", StringComparison.InvariantCultureIgnoreCase))
               newPar.ParamType = "CURRENCY";
            else if (propSingle.DataType.Equals("IfcPositivePlaneAngleMeasure", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcCardinalPointReference", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcCountMeasure", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcDayInMonthNumber", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcDayInWeekNumber", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcDimensionCount", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcInteger", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcIntegerCountRateMeasure", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcMonthInYearNumber", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcTimeStamp", StringComparison.InvariantCultureIgnoreCase))
               newPar.ParamType = "INTEGER";
            else if (propSingle.DataType.Equals("IfcLengthMeasure", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcNonNegativeLengthMeasure", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcPositiveLengthMeasure", StringComparison.InvariantCultureIgnoreCase))
               newPar.ParamType = "LENGTH";
            else if (propSingle.DataType.Equals("IfcMassDensityMeasure", StringComparison.InvariantCultureIgnoreCase))
               newPar.ParamType = "MASS_DENSITY";
            else if (propSingle.DataType.Equals("IfcArcIndex", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcComplexNumber", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcCompoundPlaneAngleMeasure", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcLineIndex", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcPropertySetDefinitionSet", StringComparison.InvariantCultureIgnoreCase))
               newPar.ParamType = "MULTILINETEXT";
            else if (propSingle.DataType.Equals("IfcBinary", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcBoxAlignment", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcDate", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcDateTime", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcDescriptiveMeasure", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcDuration", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcFontStyle", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcFontVariant", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcFontWeight", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcGloballyUniqueId", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcIdentifier", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcLabel", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcLanguageId", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcPresentableText", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcText", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcTextAlignment", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcTextDecoration", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcTextFontName", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcTextTransformation", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcTime", StringComparison.InvariantCultureIgnoreCase))
               newPar.ParamType = "TEXT";
            else if (propSingle.DataType.Equals("IfcURIReference", StringComparison.InvariantCultureIgnoreCase))
               newPar.ParamType = "URL";
            else if (propSingle.DataType.Equals("IfcVolumeMeasure", StringComparison.InvariantCultureIgnoreCase))
               newPar.ParamType = "VOLUME";
            else if (propSingle.DataType.Equals("IfcBoolean", StringComparison.InvariantCultureIgnoreCase)
               || propSingle.DataType.Equals("IfcLogical", StringComparison.InvariantCultureIgnoreCase))
               newPar.ParamType = "YESNO";
            else
               newPar.ParamType = "NUMBER";
         }

         if (!SharedParamFileDict.ContainsKey(newPar.Name))
         {
            SharedParamFileDict.Add(newPar.Name, newPar);
         }
         else    // Keep the GUID, but override the details
         {
            // If this Property has IfcGuid, use the IfdGuid set in the newPar, otherwise keep the original one
            if (!hasIfdGuid)
               newPar.ParamGuid = SharedParamFileDict[newPar.Name].ParamGuid;
            SharedParamFileDict[newPar.Name] = newPar;     // override the Dict
         }

         SharedParameterDef newParType = (SharedParameterDef)newPar.Clone();
         newParType.Name = newParType.Name + "[Type]";
         if (!SharedParamFileTypeDict.ContainsKey(newParType.Name))
         {
            SharedParamFileTypeDict.Add(newParType.Name, newParType);
         }
         else    // Keep the GUID, but override the details
         {
            newParType.ParamGuid = SharedParamFileTypeDict[newParType.Name].ParamGuid;
            SharedParamFileTypeDict[newParType.Name] = newParType;     // override the Dict
         }

         if (prop.NameAliases != null)
         {
            foreach (NameAlias alias in prop.NameAliases)
            {
               LanguageType lang = checkAliasLanguage(alias.lang);
               outF.WriteLine("\t\t\t\tifcPSE.AddLocalizedParameterName(LanguageType.{0}, \"{1}\");", lang, alias.Alias);
            }
         }

         string calcName = "Revit.IFC.Export.Exporter.PropertySet.Calculators." + prop.Name + "Calculator";
         outF.WriteLine("\t\t\t\tcalcType = System.Reflection.Assembly.GetExecutingAssembly().GetType(\"" + calcName + "\");");
         outF.WriteLine("\t\t\t\tif (calcType != null)");
         outF.WriteLine("\t\t\t\t\tifcPSE.PropertyCalculator = (PropertyCalculator) calcType.GetConstructor(Type.EmptyTypes).Invoke(new object[]{});");
         outF.WriteLine("\t\t\t\t{0}.AddEntry(ifcPSE);", varName);
         outF.WriteLine("");
      }

      public static string processExistingParFile(string parFileName, bool isType, ref StreamWriter destFile)
      {
         IDictionary<string, SharedParameterDef> dictToFill;
         if (isType)
            dictToFill = SharedParamFileTypeDict;
         else
            dictToFill = SharedParamFileDict;

         string messageText = null;
         // Keep original data (for maintaining the GUID) in a dictionary
         using (StreamReader stSharedParam = File.OpenText(parFileName))
         {
            string line;
            while ((line = stSharedParam.ReadLine()) != null && !string.IsNullOrEmpty(line))
            {
               // Copy content to the destination file
               //destFile.WriteLine(line);

               string[] token = line.Split('\t');
               if (token == null || token.Count() == 0)
                  continue;

               if (!token[0].Equals("PARAM"))
                  continue;

               SharedParameterDef parDef = new SharedParameterDef();
               parDef.Param = token[0];
               try
               {
                  parDef.ParamGuid = Guid.Parse(token[1]);
               }
               catch
               {
                  // Shouldn't be here
                  continue;
               }

               if (string.IsNullOrEmpty(token[2]))
               {
                  // Shouldn't be here
                  continue;
               }
               parDef.Name = token[2];

               if (token[3] == null)
                  continue;

               parDef.ParamType = token[3];

               parDef.DataCategory = token[4];

               int grp;
               if (int.TryParse(token[5], out grp))
                  parDef.GroupId = grp;
               else
                  continue;

               parDef.Visibility = false;
               if (!string.IsNullOrEmpty(token[6]))
               {
                  int vis;
                  if (int.TryParse(token[6], out vis))
                     if (vis == 1)
                        parDef.Visibility = true;
               }

               if (!string.IsNullOrEmpty(token[7]))
               {
                  parDef.Description = token[7];
               }

               parDef.UserModifiable = false;
               if (!string.IsNullOrEmpty(token[8]))
               {
                  int mod;
                  if (int.TryParse(token[8], out mod))
                     if (mod == 1)
                        parDef.UserModifiable = true;
               }

               try
               {
                  dictToFill.Add(parDef.Name, parDef);
               }
               catch (ArgumentException exp)
               {
                  messageText += "\n" + parDef.Name + ": " + exp.Message;
               }
            }
         }
         return messageText;
      }

      public PsetProperty getPropertyDef(XNamespace ns, XElement pDef)
      {
         try
         {
            PsetProperty prop = new PsetProperty();
            if (pDef.Attribute("ifdguid") != null)
               prop.IfdGuid = pDef.Attribute("ifdguid").Value;
            prop.Name = pDef.Element(ns + "Name").Value;
            IList<NameAlias> aliases = new List<NameAlias>();
            XElement nAliasesElem = pDef.Elements(ns + "NameAliases").FirstOrDefault();
            if (nAliasesElem != null)
            {
               var nAliases = from el in nAliasesElem.Elements(ns + "NameAlias") select el;
               foreach (XElement alias in nAliases)
               {
                  NameAlias nameAlias = new NameAlias();
                  nameAlias.Alias = alias.Value;
                  nameAlias.lang = alias.Attribute("lang").Value;
                  aliases.Add(nameAlias);
               }
            }
            if (aliases.Count > 0)
               prop.NameAliases = aliases;

            PropertyDataType dataTyp = null;
            var propType = pDef.Elements(ns + "PropertyType").FirstOrDefault();
            XElement propDetType = propType.Elements().FirstOrDefault();
            if (propDetType == null)
            {
#if DEBUG
               logF.WriteLine("%Warning: Missing PropertyType for {0}.{1}", pDef.Parent.Parent.Element(ns + "Name").Value, prop.Name);
#endif
               return prop;
            }

            if (propDetType.Name.LocalName.Equals("TypePropertySingleValue"))
            {
               XElement dataType = propDetType.Element(ns + "DataType");
               PropertySingleValue sv = new PropertySingleValue();
               if (dataType.Attribute("type") != null)
               {
                  sv.DataType = dataType.Attribute("type").Value;
               }
               else
               {
#if DEBUG
                  logF.WriteLine("%Warning: Missing TypePropertySingleValue for {0}.{1}", pDef.Parent.Parent.Element(ns + "Name").Value, prop.Name);
#endif
                  // Hndle a known issue of missing data type for a specific property
                  if (prop.Name.Equals("Reference", StringComparison.InvariantCultureIgnoreCase))
                     sv.DataType = "IfcIdentifier";
                  else
                     sv.DataType = "IfcLabel";     // Set this to default if missing
               }
               dataTyp = sv;
            }
            else if (propDetType.Name.LocalName.Equals("TypePropertyReferenceValue"))
            {
               PropertyReferenceValue rv = new PropertyReferenceValue();
               // Older versions uses Element DataType!
               XElement dt = propDetType.Element(ns + "DataType");
               if (dt == null)
               {
                  rv.RefEntity = propDetType.Attribute("reftype").Value;
               }
               else
               {
                  rv.RefEntity = dt.Attribute("type").Value;
               }
               dataTyp = rv;
            }
            else if (propDetType.Name.LocalName.Equals("TypePropertyEnumeratedValue"))
            {
               PropertyEnumeratedValue pev = new PropertyEnumeratedValue();
               var enumItems = propDetType.Descendants(ns + "EnumItem");
               if (enumItems.Count() > 0)
               {
                  pev.Name = propDetType.Element(ns + "EnumList").Attribute("name").Value;
                  pev.EnumDef = new List<PropertyEnumItem>();
                  foreach (var en in enumItems)
                  {
                     string enumItemName = en.Value.ToString();
                     IEnumerable<XElement> consDef = null;
                     if (propDetType.Element(ns + "ConstantList") != null)
                     {
                        consDef = from el in propDetType.Element(ns + "ConstantList").Elements(ns + "ConstantDef")
                                  where (el.Element(ns + "Name").Value.Equals(enumItemName, StringComparison.CurrentCultureIgnoreCase))
                                  select el;
                     }

                     if (propDetType.Element(ns + "ConstantList") != null)
                     {
                        var consList = propDetType.Element(ns + "ConstantList").Elements(ns + "ConstantDef");
                        if (consList != null && consList.Count() != enumItems.Count())
                        {
#if DEBUG
                           logF.WriteLine("%Warning: EnumList (" + enumItems.Count().ToString() + ") is not consistent with the ConstantList ("
                              + consList.Count().ToString() + ") for: {0}.{1}",
                              pDef.Parent.Parent.Element(ns + "Name").Value, prop.Name);
#endif
                        }
                     }

                     if (consDef != null && consDef.Count() > 0)
                     {
                        foreach (var cD in consDef)
                        {
                           PropertyEnumItem enumItem = new PropertyEnumItem();
                           enumItem.EnumItem = cD.Elements(ns + "Name").FirstOrDefault().Value;
                           enumItem.Aliases = new List<NameAlias>();
                           var eAliases = from el in cD.Elements(ns + "NameAliases").FirstOrDefault().Elements(ns + "NameAlias") select el;
                           if (eAliases.Count() > 0)
                           {
                              foreach (var aliasItem in eAliases)
                              {
                                 NameAlias nal = new NameAlias();
                                 nal.Alias = aliasItem.Value;
                                 nal.lang = aliasItem.Attribute("lang").Value;
                                 enumItem.Aliases.Add(nal);
                              }
                           }
                           pev.EnumDef.Add(enumItem);
                        }
                     }
                     else
                     {
                        PropertyEnumItem enumItem = new PropertyEnumItem();
                        enumItem.EnumItem = enumItemName;
                        enumItem.Aliases = new List<NameAlias>();
                        pev.EnumDef.Add(enumItem);
                     }
                  }
               }
               else
               {
                  {
#if DEBUG
                     logF.WriteLine("%Warning: EnumList {0}.{1} is empty!", pDef.Parent.Parent.Element(ns + "Name").Value, prop.Name);
#endif
                  }
                  // If EnumList is empty, try to see whether ConstantDef has values. The Enum item name will be taken from the ConstantDef.Name
                  pev.Name = "PEnum_" + prop.Name;
                  pev.EnumDef = new List<PropertyEnumItem>();
                  var consDef = from el in propDetType.Element(ns + "ConstantList").Elements(ns + "ConstantDef")
                                select el;
                  if (consDef != null && consDef.Count() > 0)
                  {
                     foreach (var cD in consDef)
                     {
                        PropertyEnumItem enumItem = new PropertyEnumItem();
                        enumItem.EnumItem = cD.Elements(ns + "Name").FirstOrDefault().Value;
                        enumItem.Aliases = new List<NameAlias>();
                        var eAliases = from el in cD.Elements(ns + "NameAliases").FirstOrDefault().Elements(ns + "NameAlias") select el;
                        if (eAliases.Count() > 0)
                           foreach (var aliasItem in eAliases)
                           {
                              NameAlias nal = new NameAlias();
                              nal.Alias = aliasItem.Value;
                              nal.lang = aliasItem.Attribute("lang").Value;
                              enumItem.Aliases.Add(nal);
                           }
                        pev.EnumDef.Add(enumItem);
                     }
                  }
               }
               dataTyp = pev;
            }
            else if (propDetType.Name.LocalName.Equals("TypePropertyBoundedValue"))
            {
               XElement dataType = propDetType.Element(ns + "DataType");
               PropertyBoundedValue bv = new PropertyBoundedValue();
               bv.DataType = dataType.Attribute("type").Value;
               dataTyp = bv;
            }
            else if (propDetType.Name.LocalName.Equals("TypePropertyListValue"))
            {
               XElement dataType = propDetType.Descendants(ns + "DataType").FirstOrDefault();
               PropertyListValue lv = new PropertyListValue();
               lv.DataType = dataType.Attribute("type").Value;
               dataTyp = lv;
            }
            else if (propDetType.Name.LocalName.Equals("TypePropertyTableValue"))
            {
               PropertyTableValue tv = new PropertyTableValue();
               var tve = propDetType.Element(ns + "Expression");
               if (tve != null)
                  tv.Expression = tve.Value;
               XElement el = propDetType.Element(ns + "DefiningValue");
               if (el != null)
               {
                  XElement el2 = propDetType.Element(ns + "DefiningValue").Element(ns + "DataType");
                  if (el2 != null)
                     tv.DefiningValueType = el2.Attribute("type").Value;
               }
               el = propDetType.Element(ns + "DefinedValue");
               if (el != null)
               {
                  XElement el2 = propDetType.Element(ns + "DefinedValue").Element(ns + "DataType");
                  if (el2 != null)
                     tv.DefinedValueType = el2.Attribute("type").Value;
               }
               dataTyp = tv;
            }
            else if (propDetType.Name.LocalName.Equals("TypeComplexProperty"))
            {
               ComplexProperty compProp = new ComplexProperty();
               compProp.Name = propDetType.Attribute("name").Value;
               compProp.Properties = new List<PsetProperty>();
               foreach (XElement cpPropDef in propDetType.Elements(ns + "PropertyDef"))
               {
                  PsetProperty pr = getPropertyDef(ns, cpPropDef);
                  if (pr == null)
                  {
#if DEBUG
                     logF.WriteLine("%Error: Mising PropertyType data in complex property {0}.{1}.{2}", propDetType.Parent.Parent.Element(ns + "Name").Value,
                        prop.Name, cpPropDef.Element(ns + "Name").Value);
#endif
                  }
                  else
                     compProp.Properties.Add(pr);
               }
               dataTyp = compProp;
            }
            prop.PropertyType = dataTyp;

            return prop;
         }
         catch
         {
            return null;
         }
      }

      PsetDefinition Process(string schemaVersion, FileInfo PSDfileName)
      {
         PsetDefinition pset = new PsetDefinition();
         XDocument doc = XDocument.Load(PSDfileName.FullName);

         // Older versions of psd uses namespace!
         var nsInfo = doc.Root.Attributes("xmlns").FirstOrDefault();
         XNamespace ns = "";
         if (nsInfo != null)
            ns = nsInfo.Value;

         pset.Name = doc.Elements(ns + "PropertySetDef").Elements(ns + "Name").FirstOrDefault().Value;
         pset.IfcVersion = doc.Elements(ns + "PropertySetDef").Elements(ns + "IfcVersion").FirstOrDefault().Attribute("version").Value.Replace(" ", "");
         if (pset.IfcVersion.StartsWith("2"))
         {
            if (pset.IfcVersion.Equals("2X", StringComparison.CurrentCultureIgnoreCase)
               || pset.IfcVersion.Equals("2X2", StringComparison.CurrentCultureIgnoreCase)
               || pset.IfcVersion.Equals("2.X", StringComparison.CurrentCultureIgnoreCase))
               pset.IfcVersion = "IFC2X2";  // BUG in the documentation. It ony contains "2x" instead of "2x2"
            else
               pset.IfcVersion = "IFC" + pset.IfcVersion.ToUpper();   // Namespace cannot start with a number. e.g. make sure 2x3 -> IFC2x3
         }
         else if (pset.IfcVersion.StartsWith("IFC4"))
            pset.IfcVersion = schemaVersion.ToUpper();

         if (doc.Element(ns + "PropertySetDef").Attribute("ifdguid") != null)
            pset.IfdGuid = doc.Element(ns + "PropertySetDef").Attribute("ifdguid").Value;
         // Get applicable classes
         IEnumerable<XElement> applicableClasses = from el in doc.Descendants(ns + "ClassName") select el;
         IList<string> applClassesList = new List<string>();
         foreach (XElement applClass in applicableClasses)
         {
            string className = ProcessPsetDefinition.removeInvalidNName(applClass.Value);
            if (!string.IsNullOrEmpty(className))
               applClassesList.Add(className);
         }

         pset.ApplicableClasses = applClassesList;

         XElement applType = doc.Elements(ns + "PropertySetDef").Elements(ns + "ApplicableTypeValue").FirstOrDefault();
         if (applType != null)
         {
            string applicableType = applType.Value;
            if (!string.IsNullOrEmpty(applicableType) && !applicableType.Equals("N/A", StringComparison.InvariantCultureIgnoreCase))
            {
               // Remove "SELF\" in the applicable type
               if (applicableType.Contains("SELF\\"))
                  applicableType = applicableType.Replace("SELF\\", "");

               string[] applTypeStr = applicableType.Split('/', '.', '=');
               pset.ApplicableType = applTypeStr[0];
               if (applTypeStr.Count() > 1)
                  pset.PredefinedType = applTypeStr[applTypeStr.Count() - 1].Replace("\"", "");

               // If the applicable type contains more than 1 entry, add them into the applicable classes
               string[] addClasses = pset.ApplicableType.Split(',');
               if (addClasses.Count() > 1)
               {
                  foreach (string addClass in addClasses)
                  {
                     string addClassTr = addClass.TrimStart().TrimEnd();
                     if (!pset.ApplicableClasses.Contains(addClassTr))
                        pset.ApplicableClasses.Add(addClassTr);
                  }
               }
            }
         }

         // Shouldn't be here, but some time the IFC documentation is not good
         if (pset.ApplicableClasses.Count == 0)
         {
#if DEBUG
            logF.WriteLine("%Error - This pset is missing applicable class information: " + pset.IfcVersion + " " + pset.Name);
#endif
            // Special handling of problematic issue
            #region IFCSpecialHandling
            if (pset.IfcVersion.Equals("IFC4", StringComparison.InvariantCultureIgnoreCase))
            {
               if (pset.Name.Equals("Pset_CivilElementCommon"))
                  pset.ApplicableClasses.Add("IfcCivilElement");
               else if (pset.Name.Equals("Pset_ElectricFlowStorageDevicePHistory", StringComparison.InvariantCultureIgnoreCase))
                  pset.ApplicableClasses.Add("IfcElectricFlowStorageDevice");
               else if (pset.Name.Equals("Pset_ElementAssemblyCommon", StringComparison.InvariantCultureIgnoreCase))
                  pset.ApplicableClasses.Add("IfcElementAssembly");
               else if (pset.Name.Equals("Pset_SpatialZoneCommon", StringComparison.InvariantCultureIgnoreCase))
                  pset.ApplicableClasses.Add("IfcSpatialZone");
            }
            else if (pset.IfcVersion.Equals("IFC2X2", StringComparison.InvariantCultureIgnoreCase))
            {
               if (pset.Name.Equals("Pset_AnalogInput"))
               {
                  pset.ApplicableClasses.Add("IfcDistributionControlElement");
               }
               else if (pset.Name.Equals("Pset_AnalogOutput", StringComparison.InvariantCultureIgnoreCase))
               {
                  pset.ApplicableClasses.Add("IfcDistributionControlElement");
               }
               else if (pset.Name.Equals("Pset_BinaryInput", StringComparison.InvariantCultureIgnoreCase))
               {
                  pset.ApplicableClasses.Add("IfcDistributionControlElement");
               }
               else if (pset.Name.Equals("Pset_BinaryOutput", StringComparison.InvariantCultureIgnoreCase))
               {
                  pset.ApplicableClasses.Add("IfcDistributionControlElement");
               }
               else if (pset.Name.Equals("Pset_MultiStateInput", StringComparison.InvariantCultureIgnoreCase))
               {
                  pset.ApplicableClasses.Add("IfcDistributionControlElement");
               }
               else if (pset.Name.Equals("Pset_MultiStateOutput", StringComparison.InvariantCultureIgnoreCase))
               {
                  pset.ApplicableClasses.Add("IfcDistributionControlElement");
               }
               else if (pset.Name.Equals("Pset_ElectricalDeviceCommon", StringComparison.InvariantCultureIgnoreCase))
               {
                  pset.ApplicableClasses.Add("IfcDistributionElement");
               }
               else if (pset.Name.Equals("Pset_DuctConnection", StringComparison.InvariantCultureIgnoreCase))
               {
                  pset.ApplicableClasses.Add("IfcDuctSegmentType");
                  pset.ApplicableClasses.Add("IfcDuctFittingType");
               }
               else if (pset.Name.Equals("Pset_DuctDesignCriteria", StringComparison.InvariantCultureIgnoreCase))
               {
                  pset.ApplicableClasses.Add("IfcSystem");
                  pset.ApplicableClasses.Add("IfcDuctSegmentType");
                  pset.ApplicableClasses.Add("IfcDuctFittingType");
               }
               else if (pset.Name.Equals("Pset_PipeConnection", StringComparison.InvariantCultureIgnoreCase))
               {
                  pset.ApplicableClasses.Add("IfcPipeSegmentType");
                  pset.ApplicableClasses.Add("IfcPipeFittingType");
               }
               else if (pset.Name.Equals("Pset_PipeConnectionFlanged", StringComparison.InvariantCultureIgnoreCase))
               {
                  pset.ApplicableClasses.Add("IfcPipeSegmentType");
                  pset.ApplicableClasses.Add("IfcPipeFittingType");
               }
               else if (pset.Name.Equals("Pset_AirSideSystemInformation", StringComparison.InvariantCultureIgnoreCase))
               {
                  pset.ApplicableClasses.Add("IfcSystem");
               }
               else if (pset.Name.Equals("Pset_FireRatingProperties", StringComparison.InvariantCultureIgnoreCase))
               {
                  pset.ApplicableClasses.Add("IfcElement");
                  pset.ApplicableClasses.Add("IfcSpatialStructureElement");
               }
               else if (pset.Name.Equals("Pset_ThermalLoadAggregate", StringComparison.InvariantCultureIgnoreCase))
               {
                  pset.ApplicableClasses.Add("IfcElement");
                  pset.ApplicableClasses.Add("IfcSpatialStructureElement");
                  pset.ApplicableClasses.Add("IfcZone");
               }
               else if (pset.Name.Equals("Pset_ThermalLoadDesignCriteria", StringComparison.InvariantCultureIgnoreCase))
               {
                  pset.ApplicableClasses.Add("IfcElement");
                  pset.ApplicableClasses.Add("IfcSpatialStructureElement");
                  pset.ApplicableClasses.Add("IfcZone");
                  pset.ApplicableClasses.Add("IfcBuilding");
               }
               else if (pset.Name.Equals("Pset_ConcreteElementGeneral", StringComparison.InvariantCultureIgnoreCase))
               {
                  pset.ApplicableClasses.Add("IfcBuildingElement");
               }
               else if (pset.Name.Equals("Pset_ConcreteElementQuantityGeneral", StringComparison.InvariantCultureIgnoreCase))
               {
                  pset.ApplicableClasses.Add("IfcBuildingElement");
               }
               else if (pset.Name.Equals("Pset_ConcreteElementSurfaceFinishQuantityGeneral", StringComparison.InvariantCultureIgnoreCase))
               {
                  pset.ApplicableClasses.Add("IfcBuildingElement");
               }
               else if (pset.Name.Equals("Pset_PrecastConcreteElementGeneral", StringComparison.InvariantCultureIgnoreCase))
               {
                  pset.ApplicableClasses.Add("IfcBuildingElement");
               }
            }
            #endregion
         }

         IList<PsetProperty> propList = new List<PsetProperty>();
         var pDefs = from p in doc.Descendants(ns + "PropertyDef") select p;
         foreach (XElement pDef in pDefs)
         {
            PsetProperty prop = getPropertyDef(ns, pDef);
            SharedParameterDef shPar = new SharedParameterDef();
            if (prop == null)
            {
#if DEBUG
               logF.WriteLine("%Error: Mising PropertyType data for {0}.{1}", pset.Name, pDef.Element(ns + "Name").Value);
#endif
            }
            else
            {
               propList.Add(prop);
            }
         }
         pset.properties = propList;

         return pset;
      }

      public void ProcessSchemaPsetDef(string schemaName, DirectoryInfo psdFolder)
      {
         foreach (FileInfo file in psdFolder.GetFiles("Pset_*.xml"))
         {
            PropertySet.PsetDefinition psetD = Process(schemaName, file);
            AddPsetDefToDict(schemaName, psetD);
         }
      }
   }
}
