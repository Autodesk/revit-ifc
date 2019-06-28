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
         ProcessPsetDefinition.processExistingParFile(SharedParFileName, false, ref stSharedPar);

         if (File.Exists(SharedParFileNameType))
         {
            string typeParFileNameOut = Path.Combine(Path.GetDirectoryName(SharedParFileNameType), Path.GetFileNameWithoutExtension(SharedParFileNameType) + "_out.txt");
            stSharedParType = File.CreateText(typeParFileNameOut);
            ProcessPsetDefinition.processExistingParFile(SharedParFileNameType, true, ref stSharedParType);
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

         string dirName = Path.GetDirectoryName(textBox_OutputFile.Text);
         string penumFileName = Path.GetFileNameWithoutExtension(textBox_OutputFile.Text);

         if (File.Exists(textBox_OutputFile.Text))
            File.Delete(textBox_OutputFile.Text);

         StreamWriter outF = new StreamWriter(textBox_OutputFile.Text);
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
         outF.WriteLine("\tpartial class ExporterInitializer");
         outF.WriteLine("\t{");

         // Collect all Pset definition for psd folders
         foreach (DirectoryInfo psd in psdFolders)
         {
            string schemaFolder = psd.FullName.Remove(0, textBox_PSDSourceDir.Text.Length + 1).Split('\\')[0];

#if DEBUG
            logF.WriteLine("\n*** Processing " + schemaFolder);
#endif
            foreach (DirectoryInfo subDir in psd.GetDirectories())
            {
               procPsetDef.ProcessSchemaPsetDef(schemaFolder, subDir);
            }
            procPsetDef.ProcessSchemaPsetDef(schemaFolder, psd);
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
                  pSetDump += "\n  ===> IfcVersion: " + vPdef.IfcVersion;
                  pSetDump += "\n" + vPdef.PropertySetDef.ToString() + "\n";
               }
               pSetDump += "\n\n";
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

         // Method to initialize all the propertysets
         outF.WriteLine("\t\tpublic static void InitCommonPropertySets(IList<IList<PropertySetDescription>> propertySets)");
         outF.WriteLine("\t\t{");
         outF.WriteLine("\t\t\tIList<PropertySetDescription> commonPropertySets = new List<PropertySetDescription>();");
         foreach (KeyValuePair<string, IList<VersionSpecificPropertyDef>> psetDefEntry in procPsetDef.allPDefDict)
         {
            outF.WriteLine("\t\t\tInit" + psetDefEntry.Key + "(commonPropertySets);");
         }
         outF.WriteLine("\n\t\t\tpropertySets.Add(commonPropertySets);");
         outF.WriteLine("\t\t}");
         outF.WriteLine("");

         // For generated codes and shared parameters
         foreach (KeyValuePair<string, IList<VersionSpecificPropertyDef>> psetDefEntry in procPsetDef.allPDefDict)
         {
            string psetName = psetDefEntry.Key;
            outF.WriteLine("\t\tprivate static void Init" + psetName + "(IList<PropertySetDescription> commonPropertySets)");
            outF.WriteLine("\t\t{");

            string varName = psetDefEntry.Key.Replace("Pset_", "propertySet");

            outF.WriteLine("\t\t\tPropertySetDescription {0} = new PropertySetDescription();", varName);

            string psetEnumStr = psetName.Replace("PSet_", "PSet");
            try
            {
               Revit.IFC.Export.Toolkit.IFCCommonPSets psetEnum = (Revit.IFC.Export.Toolkit.IFCCommonPSets)Enum.Parse(typeof(Revit.IFC.Export.Toolkit.IFCCommonPSets), psetEnumStr);
               outF.WriteLine("\t\t\t{0}.SubElementIndex = (int)IFCCommonPSets.{1};", varName, psetName.Replace("PSet_", "PSet"));
            }
            catch(ArgumentException)
            {
#if DEBUG
               logF.WriteLine("\t%Info: " + psetEnumStr + " is not defined in Revit.IFC.Export.Toolkit.IFCCommonPSets.");
#endif
            }

            outF.WriteLine("\t\t\t{0}.Name = \"{1}\";", varName, psetName);
            outF.WriteLine("\t\t\tPropertySetEntry ifcPSE = null;");
            outF.WriteLine("\t\t\tType calcType = null;");

            foreach (VersionSpecificPropertyDef vspecPDef in psetDefEntry.Value)
            {
               PsetDefinition pDef = vspecPDef.PropertySetDef;

               if (vspecPDef.IfcVersion.Equals("IFC2X2", StringComparison.CurrentCultureIgnoreCase))
               {
                  outF.WriteLine("\t\t\tif (ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), \"" + psetName + "\"))");
                  outF.WriteLine("\t\t\t{");
                  foreach (string applEnt in vspecPDef.PropertySetDef.ApplicableClasses)
                  {
                     string applEnt2 = applEnt;
                     if (string.IsNullOrEmpty(applEnt))
                        applEnt2 = "IfcBuildingElementProxy";     // Default if somehow the data is empty
                     outF.WriteLine("\t\t\t\t{0}.EntityTypes.Add(IFCEntityType.{1});", varName, applEnt2);
                  }
                  if (!string.IsNullOrEmpty(vspecPDef.PropertySetDef.ApplicableType))
                     outF.WriteLine("\t\t\t\t{0}.ObjectType = \"{1}\";", varName, vspecPDef.PropertySetDef.ApplicableType);
                  if (!string.IsNullOrEmpty(vspecPDef.PropertySetDef.PredefinedType))
                     outF.WriteLine("\t\t\t\t{0}.PredefinedType = \"{1}\";", varName, vspecPDef.PropertySetDef.PredefinedType);
               }
               else if (vspecPDef.IfcVersion.Equals("IFC2X3TC1", StringComparison.CurrentCultureIgnoreCase)
                  || vspecPDef.IfcVersion.Equals("IFC2X3_TC1", StringComparison.CurrentCultureIgnoreCase))
               {
                  outF.WriteLine("\t\t\tif (ExporterCacheManager.ExportOptionsCache.ExportAs2x3 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), \"" + psetName + "\"))");
                  outF.WriteLine("\t\t\t{");
                  foreach (string applEnt in vspecPDef.PropertySetDef.ApplicableClasses)
                  {
                     string applEnt2 = applEnt;
                     if (string.IsNullOrEmpty(applEnt))
                        applEnt2 = "IfcBuildingElementProxy";     // Default if somehow the data is empty
                     outF.WriteLine("\t\t\t\t{0}.EntityTypes.Add(IFCEntityType.{1});", varName, applEnt2);
                  }
                  if (!string.IsNullOrEmpty(vspecPDef.PropertySetDef.ApplicableType))
                     outF.WriteLine("\t\t\t\t{0}.ObjectType = \"{1}\";", varName, vspecPDef.PropertySetDef.ApplicableType);
                  if (!string.IsNullOrEmpty(vspecPDef.PropertySetDef.PredefinedType))
                     outF.WriteLine("\t\t\t\t{0}.PredefinedType = \"{1}\";", varName, vspecPDef.PropertySetDef.PredefinedType);
               }
               //else if (vspecPDef.IfcVersion.Equals("IFC4_ADD1"))
               //{
               else if (vspecPDef.SchemaFileVersion.Equals("IFC4_ADD1", StringComparison.CurrentCultureIgnoreCase))
               {
                  outF.WriteLine("\t\t\tif (ExporterCacheManager.ExportOptionsCache.ExportAs4_ADD1 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), \"" + psetName + "\"))");
                  outF.WriteLine("\t\t\t{");
                  foreach (string applEnt in vspecPDef.PropertySetDef.ApplicableClasses)
                  {
                     string applEnt2 = applEnt;
                     if (string.IsNullOrEmpty(applEnt))
                        applEnt2 = "IfcBuildingElementProxy";     // Default if somehow the data is empty
                     outF.WriteLine("\t\t\t\t{0}.EntityTypes.Add(IFCEntityType.{1});", varName, applEnt2);
                  }
                  if (!string.IsNullOrEmpty(vspecPDef.PropertySetDef.ApplicableType))
                     outF.WriteLine("\t\t\t\t{0}.ObjectType = \"{1}\";", varName, vspecPDef.PropertySetDef.ApplicableType);
                  if (!string.IsNullOrEmpty(vspecPDef.PropertySetDef.PredefinedType))
                     outF.WriteLine("\t\t\t\t{0}.PredefinedType = \"{1}\";", varName, vspecPDef.PropertySetDef.PredefinedType);
               }
               else if (vspecPDef.SchemaFileVersion.Equals("IFC4_ADD2", StringComparison.CurrentCultureIgnoreCase))
               {
                  outF.WriteLine("\t\t\tif (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), \"" + psetName + "\"))");
                  outF.WriteLine("\t\t\t{");
                  foreach (string applEnt in vspecPDef.PropertySetDef.ApplicableClasses)
                  {
                     string applEnt2 = applEnt;
                     if (string.IsNullOrEmpty(applEnt))
                        applEnt2 = "IfcBuildingElementProxy";     // Default if somehow the data is empty
                     outF.WriteLine("\t\t\t\t{0}.EntityTypes.Add(IFCEntityType.{1});", varName, applEnt2);
                  }
                  if (!string.IsNullOrEmpty(vspecPDef.PropertySetDef.ApplicableType))
                     outF.WriteLine("\t\t\t\t{0}.ObjectType = \"{1}\";", varName, vspecPDef.PropertySetDef.ApplicableType);
                  if (!string.IsNullOrEmpty(vspecPDef.PropertySetDef.PredefinedType))
                     outF.WriteLine("\t\t\t\t{0}.PredefinedType = \"{1}\";", varName, vspecPDef.PropertySetDef.PredefinedType);
               }
               else if (vspecPDef.SchemaFileVersion.Equals("IFC4", StringComparison.CurrentCultureIgnoreCase))
               {
                  outF.WriteLine("\t\t\tif (ExporterCacheManager.ExportOptionsCache.ExportAs4 && certifiedEntityAndPsetList.AllowPsetToBeCreated(ExporterCacheManager.ExportOptionsCache.FileVersion.ToString().ToUpper(), \"" + psetName + "\"))");
                  outF.WriteLine("\t\t\t{");
                  foreach (string applEnt in vspecPDef.PropertySetDef.ApplicableClasses)
                  {
                     string applEnt2 = applEnt;
                     if (string.IsNullOrEmpty(applEnt))
                        applEnt2 = "IfcBuildingElementProxy";     // Default if somehow the data is empty
                     outF.WriteLine("\t\t\t\t{0}.EntityTypes.Add(IFCEntityType.{1});", varName, applEnt2);
                  }
                  if (!string.IsNullOrEmpty(vspecPDef.PropertySetDef.ApplicableType))
                     outF.WriteLine("\t\t\t\t{0}.ObjectType = \"{1}\";", varName, vspecPDef.PropertySetDef.ApplicableType);
                  if (!string.IsNullOrEmpty(vspecPDef.PropertySetDef.PredefinedType))
                     outF.WriteLine("\t\t\t\t{0}.PredefinedType = \"{1}\";", varName, vspecPDef.PropertySetDef.PredefinedType);
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
                        string prefixName = pDef.Name + "." + prop.Name;
                        procPsetDef.processSimpleProperty(outF, propCx, prefixName, pDef.IfcVersion, vspecPDef.SchemaFileVersion, varName, vspecPDef, penumFileName);
                     }
                  }
                  else
                  {
                     procPsetDef.processSimpleProperty(outF, prop, pDef.Name, pDef.IfcVersion, vspecPDef.SchemaFileVersion, varName, vspecPDef, penumFileName);
                  }                    
               }
               outF.WriteLine("\t\t\t}");
            }

            outF.WriteLine("\t\t\tif (ifcPSE != null)");
            outF.WriteLine("\t\t\t{");
            //outF.WriteLine("\t\t\t\t{0}.Name = \"{1}\";", varName, psetName);
            outF.WriteLine("\t\t\t\tcommonPropertySets.Add({0});", varName);
            outF.WriteLine("\t\t\t}");
            outF.WriteLine("\t\t}");
            outF.WriteLine("\n");
         }

         outF.WriteLine("\t}");
         outF.WriteLine("}");
         outF.Close();
         procPsetDef.endWriteEnumFile();

         // Now write shared parameter definitions from the Dict to destination file
         stSharedPar.WriteLine("# This is a Revit shared parameter file.");
         stSharedPar.WriteLine("# Do not edit manually.");
         stSharedPar.WriteLine("*META	VERSION	MINVERSION");
         stSharedPar.WriteLine("META	2	1");
         stSharedPar.WriteLine("*GROUP	ID	NAME");
         stSharedPar.WriteLine("GROUP	2	IFC Properties");
         stSharedPar.WriteLine("*PARAM	GUID	NAME	DATATYPE	DATACATEGORY	GROUP	VISIBLE	DESCRIPTION	USERMODIFIABLE");
         stSharedPar.WriteLine("#");
         foreach (KeyValuePair<string, SharedParameterDef> parDef in ProcessPsetDefinition.SharedParamFileDict)
         {
            SharedParameterDef newPar = parDef.Value;
            string vis = newPar.Visibility ? "1" : "0";
            string usrMod = newPar.UserModifiable ? "1" : "0";

            string parEntry = newPar.Param + "\t" + newPar.ParamGuid.ToString() + "\t" + newPar.Name + "\t" + newPar.ParamType + "\t" + newPar.DataCategory + "\t" + newPar.GroupId.ToString()
                              + "\t" + vis + "\t" + newPar.Description + "\t" + usrMod;
            stSharedPar.WriteLine(parEntry);
         }

         stSharedParType.WriteLine("# This is a Revit shared parameter file.");
         stSharedParType.WriteLine("# Do not edit manually.");
         stSharedParType.WriteLine("*META	VERSION	MINVERSION");
         stSharedParType.WriteLine("META	2	1");
         stSharedParType.WriteLine("*GROUP	ID	NAME");
         stSharedParType.WriteLine("GROUP	2	IFC Properties");
         stSharedParType.WriteLine("*PARAM	GUID	NAME	DATATYPE	DATACATEGORY	GROUP	VISIBLE	DESCRIPTION	USERMODIFIABLE");
         stSharedParType.WriteLine("#");
         foreach (KeyValuePair<string, SharedParameterDef> parDef in ProcessPsetDefinition.SharedParamFileTypeDict)
         {
            SharedParameterDef newPar = parDef.Value;
            string parName4Type;
            if (newPar.Name.EndsWith("[Type]"))
               parName4Type = newPar.Name;
            else
               parName4Type = newPar.Name + "[Type]";
            string vis = newPar.Visibility ? "1" : "0";
            string usrMod = newPar.UserModifiable ? "1" : "0";

            string parEntry = newPar.Param + "\t" + newPar.ParamGuid.ToString() + "\t" + parName4Type + "\t" + newPar.ParamType + "\t" + newPar.DataCategory + "\t" + newPar.GroupId.ToString()
                              + "\t" + vis + "\t" + newPar.Description + "\t" + usrMod;
            stSharedParType.WriteLine(parEntry);
         }

         stSharedPar.Close();
         stSharedParType.Close();
#if DEBUG
         logF.Close();
#endif
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
