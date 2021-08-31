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
using System.Threading.Tasks;
using System.Windows;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Forms;
using System.IO;
using Revit.IFC.Common.Utility;
using System.Xml.Linq;
using RevitIFCTools.PropertySet;

namespace RevitIFCTools
{
   /// <summary>
   /// Interaction logic for GeneratePsetDefitions.xaml
   /// </summary>
   public partial class GeneratePsetDefWin : Window
   {
      string outputFilename = "";
      string sourceFolder = "";
#if DEBUG
      StreamWriter logF;
#endif

      public GeneratePsetDefWin()
      {
         InitializeComponent();
         textBox_PSDSourceDir.Text = sourceFolder;
         textBox_OutputFile.Text = outputFilename;
         button_Go.IsEnabled = false;
      }

      private void button_PSDSourceDir_Click(object sender, RoutedEventArgs e)
      {
         var dialog = new FolderBrowserDialog();

         dialog.ShowDialog();
         textBox_PSDSourceDir.Text = dialog.SelectedPath;
         if (string.IsNullOrEmpty(textBox_PSDSourceDir.Text))
            return;

         if (!string.IsNullOrEmpty(textBox_PSDSourceDir.Text) && !string.IsNullOrEmpty(textBox_OutputFile.Text) && !string.IsNullOrEmpty(textBox_SharedParFile.Text))
            button_Go.IsEnabled = true;
      }

      private void button_OutputFile_Click(object sender, RoutedEventArgs e)
      {
         var dialog = new OpenFileDialog();
         dialog.DefaultExt = "cs";
         dialog.Filter = "Select *.cs output file|*.cs";
         dialog.AddExtension = true;
         dialog.CheckFileExists = false;
         dialog.ShowDialog();
         textBox_OutputFile.Text = dialog.FileName;
         outputFilename = textBox_OutputFile.Text;

         if (!string.IsNullOrEmpty(textBox_PSDSourceDir.Text) && !string.IsNullOrEmpty(textBox_OutputFile.Text) && !string.IsNullOrEmpty(textBox_SharedParFile.Text))
            button_Go.IsEnabled = true;
#if DEBUG
         logF = new StreamWriter(outputFilename + ".log");
#endif
      }

      private void button_Go_Click(object sender, RoutedEventArgs e)
      {
#if DEBUG
         string tempFolder = System.IO.Path.GetTempPath();
         logF = new StreamWriter(Path.Combine(tempFolder, "GeneratePsetDefWin.log"));
#endif
         textBox_OutputMsg.Clear();

         string parFileNameOut = Path.Combine(Path.GetDirectoryName(SharedParFileName), Path.GetFileNameWithoutExtension(SharedParFileName) + "_out.txt");
         stSharedPar = File.CreateText(parFileNameOut);
         IDictionary<string, SharedParameterDef> existingParDict = ProcessPsetDefinition.processExistingParFile(SharedParFileName);

         IDictionary<string, SharedParameterDef> existingTypeParDict = new Dictionary<string, SharedParameterDef>();
         if (File.Exists(SharedParFileNameType))
         {
            string typeParFileNameOut = Path.Combine(Path.GetDirectoryName(SharedParFileNameType), Path.GetFileNameWithoutExtension(SharedParFileNameType) + "_out.txt");
            stSharedParType = File.CreateText(typeParFileNameOut);
            existingTypeParDict = ProcessPsetDefinition.processExistingParFile(SharedParFileNameType);
         }
         else
         {
            stSharedParType = File.CreateText(SharedParFileNameType);
         }

#if DEBUG
         ProcessPsetDefinition procPsetDef = new ProcessPsetDefinition(logF);
#else
         ProcessPsetDefinition procPsetDef = new ProcessPsetDefinition(null);
#endif

         if (string.IsNullOrEmpty(textBox_PSDSourceDir.Text) || string.IsNullOrEmpty(textBox_OutputFile.Text))
            return;

         var psdFolders = new DirectoryInfo(textBox_PSDSourceDir.Text).GetDirectories("psd", SearchOption.AllDirectories);
         var qtoFolders = new DirectoryInfo(textBox_PSDSourceDir.Text).GetDirectories("qto", SearchOption.AllDirectories);

         string dirName = Path.GetDirectoryName(textBox_OutputFile.Text);
         string outFileName = Path.GetFileNameWithoutExtension(textBox_OutputFile.Text);
         string penumFileName = Path.Combine(dirName, outFileName);

         // Collect all Pset definition for psd folders
         Dictionary<ItemsInPsetQtoDefs, string> keywordsToProcess = PsetOrQto.PsetOrQtoDefItems[PsetOrQtoSetEnum.PROPERTYSET];
         HashSet<string> IfcSchemaProcessed = new HashSet<string>();
         foreach (DirectoryInfo psd in psdFolders)
         {
            string schemaFolder = psd.FullName.Remove(0, textBox_PSDSourceDir.Text.Length + 1).Split('\\')[0];

#if DEBUG
            logF.WriteLine("\r\n*** Processing " + schemaFolder);
#endif
            foreach (DirectoryInfo subDir in psd.GetDirectories())
            {
               procPsetDef.ProcessSchemaPsetDef(schemaFolder, subDir, keywordsToProcess);
            }
            procPsetDef.ProcessSchemaPsetDef(schemaFolder, psd, keywordsToProcess);
            IfcSchemaProcessed.Add(schemaFolder);
         }

         // Collect all QtoSet definition for qto folders
         keywordsToProcess = PsetOrQto.PsetOrQtoDefItems[PsetOrQtoSetEnum.QTOSET];
         foreach (DirectoryInfo qto in qtoFolders)
         {
            string schemaFolder = qto.FullName.Remove(0, textBox_PSDSourceDir.Text.Length + 1).Split('\\')[0];

#if DEBUG
            logF.WriteLine("\r\n*** Processing " + schemaFolder);
#endif
            foreach (DirectoryInfo subDir in qto.GetDirectories())
            {
               procPsetDef.ProcessSchemaPsetDef(schemaFolder, subDir, keywordsToProcess);
            }
            procPsetDef.ProcessSchemaPsetDef(schemaFolder, qto, keywordsToProcess);
         }

         // Process predefined properties
         foreach (string schemaName in IfcSchemaProcessed)
         {
#if DEBUG
            logF.WriteLine("\r\n*** Processing " + schemaName);
#endif
            procPsetDef.ProcessPredefinedPsets(schemaName);
         }

         // For testing purpose: Dump all the propertyset definition in a text file
         if (checkBox_Dump.IsChecked.HasValue && checkBox_Dump.IsChecked.Value)
         {
            string pSetDump = "";
            foreach (KeyValuePair<string, IList<VersionSpecificPropertyDef>> psetDefEntry in procPsetDef.allPDefDict)
            {
               pSetDump += "**** Property Set Name: " + psetDefEntry.Key;
               foreach (VersionSpecificPropertyDef vPdef in psetDefEntry.Value)
               {
                  pSetDump += "\r\n  ===> IfcVersion: " + vPdef.IfcVersion;
                  pSetDump += "\r\n" + vPdef.PropertySetDef.ToString() + "\r\n";
               }
               pSetDump += "\r\n\n";
            }
            string dumpDir = Path.GetDirectoryName(textBox_OutputFile.Text);
            string dumpFile = Path.GetFileNameWithoutExtension(textBox_OutputFile.Text) + ".txt";
            string dumpFilePath = Path.Combine(dumpDir, dumpFile);

            if (File.Exists(dumpFilePath))
               File.Delete(dumpFilePath);

            StreamWriter tx = new StreamWriter(dumpFilePath);
            tx.Write(pSetDump);
            tx.Close();
         }

         IDictionary<string, int> groupParamDict = new Dictionary<string, int>();
         string[] outFNameParts = outFileName.Split('_');

         // Do it for the predefined propserty sets
         string fNameToProcess = Path.Combine(dirName, outFNameParts[0] + "_PredefPset.cs");
         if (File.Exists(fNameToProcess))
            File.Delete(fNameToProcess);
         StreamWriter outF = new StreamWriter(fNameToProcess);
         // Group ID 1 and 2 are reserved
         int offset = 3;
         offset = WriteGeneratedCode(outF, procPsetDef, penumFileName, "Ifc", groupParamDict, offset);

         // Do it for the predefined propserty sets
         fNameToProcess = Path.Combine(dirName, outFNameParts[0] + "_PsetDef.cs");
         if (File.Exists(fNameToProcess))
            File.Delete(fNameToProcess);
         outF = new StreamWriter(fNameToProcess);
         offset = WriteGeneratedCode(outF, procPsetDef, penumFileName, "Pset", groupParamDict, offset);

         // Do it for the predefined propserty sets
         fNameToProcess = Path.Combine(dirName, outFNameParts[0] + "_QsetDef.cs");
         if (File.Exists(fNameToProcess))
            File.Delete(fNameToProcess);
          outF = new StreamWriter(fNameToProcess);
         offset = WriteGeneratedCode(outF, procPsetDef, penumFileName, "Qto", groupParamDict, offset);

         // Close the Enum files
         procPsetDef.endWriteEnumFile();
         WriteRevitSharedParam(stSharedPar, existingParDict, groupParamDict, false, out IList<string> deferredParList);
         AppendDeferredParamList(stSharedPar, deferredParList);
         stSharedPar.Close();

         WriteRevitSharedParam(stSharedParType, existingTypeParDict, groupParamDict, true, out IList<string> deferredParTypeList);
         AppendDeferredParamList(stSharedParType, deferredParTypeList);
         stSharedParType.Close();
#if DEBUG
         logF.Close();
#endif
      }

      void WriteRevitSharedParam(StreamWriter stSharedPar, IDictionary<string, SharedParameterDef> existingParDict, 
         IDictionary<string, int> groupParamDict, bool isType, out IList<string> deferredParList)
      {
         // Now write shared parameter definitions from the Dict to destination file
         stSharedPar.WriteLine("# This is a Revit shared parameter file.");
         stSharedPar.WriteLine("# Do not edit manually.");
         stSharedPar.WriteLine("*META	VERSION	MINVERSION");
         stSharedPar.WriteLine("META	2	1");
         stSharedPar.WriteLine("*GROUP	ID	NAME");
         int groupID = groupParamDict["Revit IFCExporter Parameters"];

         // Keep the list of Parameters that do not belong to any Pset to be written all together in one group at the end
         deferredParList = new List<string>();
         deferredParList.Add(string.Format("#"));
         deferredParList.Add(string.Format("GROUP	{0}	Revit IFCExporter Parameters", groupID));
         deferredParList.Add(string.Format("*PARAM	GUID	NAME	DATATYPE	DATACATEGORY	GROUP	VISIBLE	DESCRIPTION	USERMODIFIABLE"));
         deferredParList.Add(string.Format("#"));

         string prevPsetName = "Revit IFCExporter Parameters";
         int defaultGroupID = groupParamDict[prevPsetName];
         SortedDictionary<string, SharedParameterDef> SharedParDict = null;
         if (!isType)
            SharedParDict = ProcessPsetDefinition.SharedParamFileDict;
         else
            SharedParDict = ProcessPsetDefinition.SharedParamFileTypeDict;

         foreach (KeyValuePair<string, SharedParameterDef> parDef in ProcessPsetDefinition.SharedParamFileDict)
         {
            SharedParameterDef newPar = parDef.Value;
            bool toBeDeferred = false;
            if (!prevPsetName.Equals(newPar.OwningPset, StringComparison.InvariantCultureIgnoreCase))
            {
               if (!string.IsNullOrEmpty(newPar.OwningPset))
               {
                  prevPsetName = newPar.OwningPset;
                  groupID = groupParamDict[newPar.OwningPset];
                  stSharedPar.WriteLine("#");
                  stSharedPar.WriteLine("GROUP	{0}	{1}", groupID, newPar.OwningPset);
                  stSharedPar.WriteLine("*PARAM	GUID	NAME	DATATYPE	DATACATEGORY	GROUP	VISIBLE	DESCRIPTION	USERMODIFIABLE");
                  stSharedPar.WriteLine("#");
               }
               else
                  toBeDeferred = true;
            }

            string parName = newPar.Name;
            if (isType)
            {
               if (newPar.Name.EndsWith("[Type]"))
                  parName = newPar.Name;
               else
                  parName = newPar.Name + "[Type]";
            }

            string vis = newPar.Visibility ? "1" : "0";
            string usrMod = newPar.UserModifiable ? "1" : "0";

            // Retain the same GUID if the existing file contains the same parameter name already. This is to keep consistent GUID, even for non-IfdGUID
            if (existingParDict.ContainsKey(parName))
            {
               var existingPar = existingParDict[parName];
               newPar.ParamGuid = existingPar.ParamGuid;
            }
            else if (isType)
               newPar.ParamGuid = Guid.NewGuid();  // assign new GUID for [Type] parameter if not existing

            if (toBeDeferred)
            {
               string parEntry = newPar.Param + "\t" + newPar.ParamGuid.ToString() + "\t" + parName + "\t" + newPar.ParamType + "\t" + newPar.DataCategory + "\t" + defaultGroupID.ToString()
                  + "\t" + vis + "\t" + newPar.Description + "\t" + usrMod;
               deferredParList.Add(parEntry);
            }
            else
            {
               string parEntry = newPar.Param + "\t" + newPar.ParamGuid.ToString() + "\t" + parName + "\t" + newPar.ParamType + "\t" + newPar.DataCategory + "\t" + groupID.ToString()
                  + "\t" + vis + "\t" + newPar.Description + "\t" + usrMod;
               stSharedPar.WriteLine(parEntry);
            }
         }

         // Add items in the existing parameter dict that are not found in SharedParamFileDict, into the deferred list
         var disjunctPars = existingParDict.Where(x => !SharedParDict.ContainsKey(x.Key));
         foreach (KeyValuePair<string, SharedParameterDef> parDef in disjunctPars)
         {
            SharedParameterDef newPar = parDef.Value;
            string parName = newPar.Name;
            if (isType)
            {
               if (newPar.Name.EndsWith("[Type]"))
                  parName = newPar.Name;
               else
                  parName = newPar.Name + "[Type]";
            }
            string vis = newPar.Visibility ? "1" : "0";
            string usrMod = newPar.UserModifiable ? "1" : "0";
            string parEntry = newPar.Param + "\t" + newPar.ParamGuid.ToString() + "\t" + parName + "\t" + newPar.ParamType + "\t" + newPar.DataCategory + "\t" + defaultGroupID.ToString()
               + "\t" + vis + "\t" + newPar.Description + "\t" + usrMod;
            deferredParList.Add(parEntry);
         }
      }

      void AppendDeferredParamList(StreamWriter stSharedPar, IList<string> deferredParList)
      {
         foreach (string parToWrite in deferredParList)
         {
            stSharedPar.WriteLine(parToWrite);
         }
      }

      int WriteGeneratedCode(StreamWriter outF, ProcessPsetDefinition procPsetDef, string penumFileName, string whichCat, 
         IDictionary<string, int> paramGroupDict, int offset)
      {
         // Header section of the generated code
         outF.WriteLine("/********************************************************************************************************************************");
         outF.WriteLine("** NOTE: This code is generated from IFC psd files automatically by RevitIFCTools.                                            **");
         outF.WriteLine("**       DO NOT change it manually as it will be overwritten the next time this file is re-generated!!                        **");
         outF.WriteLine("********************************************************************************************************************************/");
         outF.WriteLine();
         outF.WriteLine("using System;");
         outF.WriteLine("using System.Collections.Generic;");
         outF.WriteLine("using System.Linq;");
         outF.WriteLine("using System.Text;");
         outF.WriteLine("using System.Threading.Tasks;");
         outF.WriteLine("using Autodesk.Revit;");
         outF.WriteLine("using Autodesk.Revit.DB;");
         outF.WriteLine("using Autodesk.Revit.DB.IFC;");
         outF.WriteLine("using Autodesk.Revit.ApplicationServices;");
         outF.WriteLine("using Revit.IFC.Export.Exporter.PropertySet;");
         outF.WriteLine("using Revit.IFC.Export.Exporter.PropertySet.Calculators;");
         outF.WriteLine("using Revit.IFC.Export.Utility;");
         outF.WriteLine("using Revit.IFC.Export.Toolkit;");
         outF.WriteLine("using Revit.IFC.Common.Enums;");
         outF.WriteLine("");
         outF.WriteLine("namespace Revit.IFC.Export.Exporter");
         outF.WriteLine("{");
         outF.WriteLine("   partial class ExporterInitializer");
         outF.WriteLine("   {");

         // Initialization section

         string allPsetOrQtoSetsName = "allPsetOrQtoSets";
         string theSetName = "theSets";
         string initPsetOrQsets = null;
         string setDescription = null;
         switch (whichCat)
         {
            case "Pset":
               initPsetOrQsets = "InitCommonPropertySets";
               setDescription = "PropertySetDescription";
               break;
            case "Ifc":
               initPsetOrQsets = "InitPredefinedPropertySets";
               setDescription = "PropertySetDescription";
               break;
            case "Qto":
               initPsetOrQsets = "InitQtoSets";
               setDescription = "QuantityDescription";
               break;
            default:
               logF.WriteLine("Category not supported {0}! Use only \"Pset\", \"Qto\", or \"Ifc\"", whichCat);
               break;
         }

         outF.WriteLine("      public static void {0}(IList<IList<{1}>> {2})", initPsetOrQsets, setDescription, allPsetOrQtoSetsName);
         outF.WriteLine("      {");
         outF.WriteLine("         IList<{0}> {1} = new List<{0}>();", setDescription, theSetName);

         int groupId = offset;
         int defaultGroupId = 2;
         if (!paramGroupDict.ContainsKey("Revit IFCExporter Parameters"))
            paramGroupDict.Add("Revit IFCExporter Parameters", defaultGroupId);

         foreach (KeyValuePair<string, IList<VersionSpecificPropertyDef>> psetDefEntry in procPsetDef.allPDefDict)
         {
            // Skip key (name) that does not start with the requested type
            if (!psetDefEntry.Key.StartsWith(whichCat.ToString(), StringComparison.InvariantCultureIgnoreCase))
               continue;

            outF.WriteLine("         Init" + psetDefEntry.Key + "({0});", theSetName);
            if (!paramGroupDict.ContainsKey(psetDefEntry.Key))
               paramGroupDict.Add(psetDefEntry.Key, groupId++);
         }
         outF.WriteLine("\r\n         allPsetOrQtoSets.Add({0});", theSetName);
         outF.WriteLine("      }");
         outF.WriteLine("");

         // For Pset or QtoSet definitions
         foreach (KeyValuePair<string, IList<VersionSpecificPropertyDef>> psetDefEntry in procPsetDef.allPDefDict)
         {
            // Skip key (name) that does not start with the requested type
            if (!psetDefEntry.Key.StartsWith(whichCat.ToString(), StringComparison.InvariantCultureIgnoreCase))
               continue;

            string varName = null;
            string setsName = null;
            string psetName = psetDefEntry.Key;
            switch (whichCat)
            {
               case "Pset":
                  setsName = "commonPropertySets";
                  outF.WriteLine("      private static void Init" + psetName + "(IList<{0}> {1})", setDescription, setsName);
                  varName = psetDefEntry.Key.Replace("Pset_", "propertySet");
                  outF.WriteLine("      {");
                  outF.WriteLine("         {0} {1} = new {0}();", setDescription, varName);
                  outF.WriteLine("         {0}.Name = \"{1}\";", varName, psetName);
                  outF.WriteLine("         PropertySetEntry ifcPSE = null;");
                  break;
               case "Ifc":
                  setsName = "commonPropertySets";
                  outF.WriteLine("      private static void Init" + psetName + "(IList<{0}> {1})", setDescription, setsName);
                  varName = psetDefEntry.Key.Replace("Pset_", "propertySet");
                  outF.WriteLine("      {");
                  outF.WriteLine("         {0} {1} = new {0}();", setDescription, varName);
                  outF.WriteLine("         {0}.Name = \"{1}\";", varName, psetName);
                  outF.WriteLine("         PropertySetEntry ifcPSE = null;");
                  break;
               case "Qto":
                  setsName = "quantitySets";
                  outF.WriteLine("      private static void Init" + psetName + "(IList<{0}> {1})", setDescription, setsName);
                  varName = psetDefEntry.Key.Replace("Qto_", "qtoSet");
                  outF.WriteLine("      {");
                  outF.WriteLine("         {0} {1} = new {0}();", setDescription, varName);
                  outF.WriteLine("         {0}.Name = \"{1}\";", varName, psetName);
                  outF.WriteLine("         QuantityEntry ifcPSE = null;");
                  break;
               default:
                  logF.WriteLine("Category not supported {0}! Use only \"Pset\", \"Qto\", or \"Ifc\"", whichCat);
                  break;
            }

            outF.WriteLine("         Type calcType = null;");

            foreach (VersionSpecificPropertyDef vspecPDef in psetDefEntry.Value)
            {
               PsetDefinition pDef = vspecPDef.PropertySetDef;

               if (vspecPDef.IfcVersion.StartsWith("IFC2X2", StringComparison.CurrentCultureIgnoreCase))
               {
                  outF.WriteLine("         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), \"" + psetName + "\"))");
                  outF.WriteLine("         {");
                  foreach (string applEnt in vspecPDef.PropertySetDef.ApplicableClasses)
                  {
                     string applEnt2 = applEnt;
                     if (string.IsNullOrEmpty(applEnt))
                        applEnt2 = "IfcBuildingElementProxy";     // Default if somehow the data is empty
                     outF.WriteLine("            {0}.EntityTypes.Add(IFCEntityType.{1});", varName, applEnt2);
                  }
                  if (!string.IsNullOrEmpty(vspecPDef.PropertySetDef.ApplicableType))
                     outF.WriteLine("            {0}.ObjectType = \"{1}\";", varName, vspecPDef.PropertySetDef.ApplicableType);
                  if (!string.IsNullOrEmpty(vspecPDef.PropertySetDef.PredefinedType))
                     outF.WriteLine("            {0}.PredefinedType = \"{1}\";", varName, vspecPDef.PropertySetDef.PredefinedType);
               }
               else if (vspecPDef.IfcVersion.StartsWith("IFC2X3", StringComparison.CurrentCultureIgnoreCase)
                  || vspecPDef.IfcVersion.Equals("IFC2X3_TC1", StringComparison.CurrentCultureIgnoreCase))
               {
                  outF.WriteLine("         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), \"" + psetName + "\"))");
                  outF.WriteLine("         {");
                  foreach (string applEnt in vspecPDef.PropertySetDef.ApplicableClasses)
                  {
                     string applEnt2 = applEnt;
                     if (string.IsNullOrEmpty(applEnt))
                        applEnt2 = "IfcBuildingElementProxy";     // Default if somehow the data is empty
                     outF.WriteLine("            {0}.EntityTypes.Add(IFCEntityType.{1});", varName, applEnt2);
                  }
                  if (!string.IsNullOrEmpty(vspecPDef.PropertySetDef.ApplicableType))
                     outF.WriteLine("            {0}.ObjectType = \"{1}\";", varName, vspecPDef.PropertySetDef.ApplicableType);
                  if (!string.IsNullOrEmpty(vspecPDef.PropertySetDef.PredefinedType))
                     outF.WriteLine("            {0}.PredefinedType = \"{1}\";", varName, vspecPDef.PropertySetDef.PredefinedType);
               }
               else if (vspecPDef.SchemaFileVersion.Equals("IFC4_ADD1", StringComparison.CurrentCultureIgnoreCase))
               {
                  outF.WriteLine("         if (ExporterCacheManager.ExportOptionsCache.ExportAs4_ADD1 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), \"" + psetName + "\"))");
                  outF.WriteLine("         {");
                  foreach (string applEnt in vspecPDef.PropertySetDef.ApplicableClasses)
                  {
                     string applEnt2 = applEnt;
                     if (string.IsNullOrEmpty(applEnt))
                        applEnt2 = "IfcBuildingElementProxy";     // Default if somehow the data is empty
                     outF.WriteLine("            {0}.EntityTypes.Add(IFCEntityType.{1});", varName, applEnt2);
                  }
                  if (!string.IsNullOrEmpty(vspecPDef.PropertySetDef.ApplicableType))
                     outF.WriteLine("            {0}.ObjectType = \"{1}\";", varName, vspecPDef.PropertySetDef.ApplicableType);
                  if (!string.IsNullOrEmpty(vspecPDef.PropertySetDef.PredefinedType))
                     outF.WriteLine("            {0}.PredefinedType = \"{1}\";", varName, vspecPDef.PropertySetDef.PredefinedType);
               }
               else if (vspecPDef.SchemaFileVersion.Equals("IFC4_ADD2", StringComparison.CurrentCultureIgnoreCase))
               {
                  outF.WriteLine("         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), \"" + psetName + "\"))");
                  outF.WriteLine("         {");
                  foreach (string applEnt in vspecPDef.PropertySetDef.ApplicableClasses)
                  {
                     string applEnt2 = applEnt;
                     if (string.IsNullOrEmpty(applEnt))
                        applEnt2 = "IfcBuildingElementProxy";     // Default if somehow the data is empty
                     outF.WriteLine("            {0}.EntityTypes.Add(IFCEntityType.{1});", varName, applEnt2);
                  }
                  if (!string.IsNullOrEmpty(vspecPDef.PropertySetDef.ApplicableType))
                     outF.WriteLine("            {0}.ObjectType = \"{1}\";", varName, vspecPDef.PropertySetDef.ApplicableType);
                  if (!string.IsNullOrEmpty(vspecPDef.PropertySetDef.PredefinedType))
                     outF.WriteLine("            {0}.PredefinedType = \"{1}\";", varName, vspecPDef.PropertySetDef.PredefinedType);
               }
               else if (vspecPDef.SchemaFileVersion.Equals("IFC4", StringComparison.CurrentCultureIgnoreCase))
               {
                  outF.WriteLine("         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), \"" + psetName + "\"))");
                  outF.WriteLine("         {");
                  foreach (string applEnt in vspecPDef.PropertySetDef.ApplicableClasses)
                  {
                     string applEnt2 = applEnt;
                     if (string.IsNullOrEmpty(applEnt))
                        applEnt2 = "IfcBuildingElementProxy";     // Default if somehow the data is empty
                     outF.WriteLine("            {0}.EntityTypes.Add(IFCEntityType.{1});", varName, applEnt2);
                  }
                  if (!string.IsNullOrEmpty(vspecPDef.PropertySetDef.ApplicableType))
                     outF.WriteLine("            {0}.ObjectType = \"{1}\";", varName, vspecPDef.PropertySetDef.ApplicableType);
                  if (!string.IsNullOrEmpty(vspecPDef.PropertySetDef.PredefinedType))
                     outF.WriteLine("            {0}.PredefinedType = \"{1}\";", varName, vspecPDef.PropertySetDef.PredefinedType);
               }
               else
               {
#if DEBUG
                  logF.WriteLine("%Error - Unrecognized schema version : " + vspecPDef.SchemaFileVersion);
#endif
               }
               //}

               // Process each property
               foreach (PsetProperty prop in pDef.properties)
               {
                  // Handle only one level deep of complex property !!!!
                  if (prop.PropertyType is ComplexProperty)
                  {
                     ComplexProperty complexProp = prop.PropertyType as ComplexProperty;
                     // For complex property the properties will be flattened by using <Pset>.<Property>.<SubProperty>
                     foreach (PsetProperty propCx in complexProp.Properties)
                     {
                        string prefixName = prop.Name;
                        procPsetDef.processSimpleProperty(outF, psetName, propCx, prefixName, pDef.IfcVersion, vspecPDef.SchemaFileVersion, varName, vspecPDef, penumFileName);
                     }
                  }
                  else
                  {
                     procPsetDef.processSimpleProperty(outF, psetName, prop, null, pDef.IfcVersion, vspecPDef.SchemaFileVersion, varName, vspecPDef, penumFileName);
                  }
               }
               outF.WriteLine("         }");
            }

            outF.WriteLine("         if (ifcPSE != null)");
            outF.WriteLine("         {");
            outF.WriteLine("            {0}.Add({1});", setsName, varName);
            outF.WriteLine("         }");
            outF.WriteLine("      }");
            outF.WriteLine("");
            outF.WriteLine("");
         }

         outF.WriteLine("   }");
         outF.WriteLine("}");
         outF.Close();
         return groupId;
      }
      

      private void button_Cancel_Click(object sender, RoutedEventArgs e)
      {
         Close();
      }

      string SharedParFileName;
      string SharedParFileNameType;
      StreamWriter stSharedPar;
      StreamWriter stSharedParType;

      private void Button_BrowseSharedParFile_Click(object sender, RoutedEventArgs e)
      {
         var dialog = new OpenFileDialog();
         dialog.DefaultExt = "txt";
         dialog.Filter = "Select *.txt shared parameter file|*.txt";
         dialog.AddExtension = true;
         dialog.CheckFileExists = true;
         dialog.ShowDialog();
         textBox_SharedParFile.Text = dialog.FileName;

         SharedParFileName = textBox_SharedParFile.Text;

         if (!string.IsNullOrEmpty(textBox_PSDSourceDir.Text) && !string.IsNullOrEmpty(textBox_OutputFile.Text) && !string.IsNullOrEmpty(textBox_SharedParFile.Text))
            button_Go.IsEnabled = true;
      }

      private void Button_BrowseSharedParFileType_Click(object sender, RoutedEventArgs e)
      {
         var dialog = new OpenFileDialog();
         dialog.DefaultExt = "txt";
         dialog.Filter = "Select *.txt shared parameter file|*.txt";
         dialog.AddExtension = true;
         dialog.CheckFileExists = false;
         dialog.ShowDialog();
         textBox_ShParFileType.Text = dialog.FileName;

         SharedParFileNameType = textBox_ShParFileType.Text;

         if (!string.IsNullOrEmpty(textBox_PSDSourceDir.Text) && !string.IsNullOrEmpty(textBox_OutputFile.Text)
            && !string.IsNullOrEmpty(textBox_SharedParFile.Text) && !string.IsNullOrEmpty(textBox_ShParFileType.Text))
            button_Go.IsEnabled = true;
      }
   }
}
