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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Runtime.Serialization.Json;
using System.IO;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Export.Utility;
using RevitIFCTools.PropertySet;

namespace RevitIFCTools
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class IFCEntityListWin : Window
   {
      SortedSet<string> aggregateEntities;
      string outputFolder = @"c:\temp";
      string userDefPsetLoc = "";
      StreamWriter logF;

      public IFCEntityListWin()
      {
         InitializeComponent();
         textBox_outputFolder.Text = outputFolder; // set default
         button_subtypeTest.IsEnabled = false;
         button_supertypeTest.IsEnabled = false;
         button_ExportInfoPair.IsEnabled = false;
         button_Go.IsEnabled = false;
      }

      private void button_browse_Click(object sender, RoutedEventArgs e)
      {
         var dialog = new FolderBrowserDialog();
         dialog.RootFolder = Environment.SpecialFolder.MyComputer;
         dialog.ShowDialog();
         textBox_folderLocation.Text = dialog.SelectedPath;
         if (string.IsNullOrEmpty(textBox_folderLocation.Text))
            return;

         DirectoryInfo dInfo = new DirectoryInfo(dialog.SelectedPath);
         foreach (FileInfo f in dInfo.GetFiles("IFC*.xsd"))
         {
            listBox_schemaList.Items.Add(f.Name);
         }
      }

      /// <summary>
      /// Procees an IFC schema from the IFCXML schema
      /// </summary>
      /// <param name="f">IFCXML schema file</param>
      private void processSchema(FileInfo f)
      {
         IfcSchemaEntityTree entityTree = new IfcSchemaEntityTree();
         ProcessIFCXMLSchema.ProcessIFCSchema(f, ref entityTree);

         string schemaName = f.Name.Replace(".xsd", "");

         if (checkBox_outputSchemaTree.IsChecked == true)
         {
            string treeDump = entityTree.DumpTree();
            File.WriteAllText(outputFolder + @"\entityTree" + schemaName + ".txt", treeDump);
         }

         if (checkBox_outputSchemaEnum.IsChecked == true)
         {
            string dictDump = entityTree.DumpEntityDict(schemaName);
            File.WriteAllText(outputFolder + @"\entityEnum" + schemaName + ".cs", dictDump);
         }

         // Add aggregate of the entity list into a set
         foreach (KeyValuePair<string, IfcSchemaEntityNode> entry in entityTree.IfcEntityDict)
         {
            aggregateEntities.Add(entry.Key);
         }
      }

      private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         if (listBox_schemaList.SelectedItems.Count > 0)
            button_Go.IsEnabled = true;
         else
            button_Go.IsEnabled = false;
      }

      private void button_Go_Click(object sender, RoutedEventArgs e)
      {
         if (listBox_schemaList.SelectedItems.Count == 0)
            return;

         DirectoryInfo dInfo = new DirectoryInfo(textBox_folderLocation.Text);
         if (dInfo == null)
            return;

         if (aggregateEntities == null)
            aggregateEntities = new SortedSet<string>();
         aggregateEntities.Clear();

         if (!Directory.Exists(textBox_outputFolder.Text))
         {
            textBox_outputFolder.Text = "";
            return;
         }

         logF = new StreamWriter(Path.Combine(outputFolder, "entityList.log"));

#if FORNAX_EXTENSION
         StreamWriter objHStream = new StreamWriter(Path.Combine(outputFolder, "ObjectHierarchy.txt"));
#endif

         IList<IFCEntityAndPsetList> fxEntityNPsetList = new List<IFCEntityAndPsetList>();

         string jsonFile = outputFolder + @"\IFCEntityAndPsetDefs.json";
         if (File.Exists(jsonFile))
            File.Delete(jsonFile);
         FileStream fs = File.Create(jsonFile);
         DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(List<IFCEntityAndPsetList>));

         foreach (string fileName in listBox_schemaList.SelectedItems)
         {
            FileInfo f = dInfo.GetFiles(fileName).First();
            //processSchema(f);

            ProcessPsetDefinition procPdef = new ProcessPsetDefinition(logF);

            string schemaName = f.Name.Replace(".xsd", "");
            IfcSchemaEntityTree entityTree = IfcSchemaEntityTree.GetEntityDictFor(schemaName, dInfo.FullName);
            IDictionary<string, IfcSchemaEntityNode> entDict = entityTree.IfcEntityDict;
            IFCEntityAndPsetList schemaEntities = new IFCEntityAndPsetList();
            schemaEntities.Version = schemaName;
            schemaEntities.EntityList = new HashSet<IFCEntityInfo>();
            schemaEntities.PsetDefList = new HashSet<IFCPropertySetDef>();

            Dictionary<ItemsInPsetQtoDefs, string> keywordsToProcess = PsetOrQto.PsetOrQtoDefItems[PsetOrQtoSetEnum.PROPERTYSET];
            DirectoryInfo[] psdFolders = new DirectoryInfo(Path.Combine(textBox_folderLocation.Text, schemaName)).GetDirectories("psd", SearchOption.AllDirectories);
            DirectoryInfo[] underpsdFolders = psdFolders[0].GetDirectories();
            if (underpsdFolders.Count() > 0)
            {
               foreach (DirectoryInfo subDir in psdFolders[0].GetDirectories())
               {
                  procPdef.ProcessSchemaPsetDef(schemaName, subDir, keywordsToProcess);
               }
            }
            else
            {
               procPdef.ProcessSchemaPsetDef(schemaName, psdFolders[0], keywordsToProcess);
            }

            keywordsToProcess = PsetOrQto.PsetOrQtoDefItems[PsetOrQtoSetEnum.QTOSET];
            DirectoryInfo[] qtoFolders = new DirectoryInfo(Path.Combine(textBox_folderLocation.Text, schemaName)).GetDirectories("qto", SearchOption.AllDirectories);
            if (qtoFolders.Count() == 0)
               qtoFolders = new DirectoryInfo(Path.Combine(textBox_folderLocation.Text, schemaName)).GetDirectories("psd", SearchOption.AllDirectories);

            if (qtoFolders.Count() > 0)
            {
               DirectoryInfo[] underqtoFolders = qtoFolders[0].GetDirectories();
               if (underqtoFolders.Count() > 0)
               {
                  foreach (DirectoryInfo subDir in qtoFolders[0].GetDirectories())
                  {
                     procPdef.ProcessSchemaPsetDef(schemaName, subDir, keywordsToProcess);
                  }
               }
               else
               {
                  procPdef.ProcessSchemaPsetDef(schemaName, qtoFolders[0], keywordsToProcess);
               }
            }

            // Process IFC2x2/IFC2x3 QTO properties
            procPdef.ProcessPreIfc4QtoSets(schemaName);

            procPdef.ProcessPredefinedPsets(schemaName);

            //Collect information on applicable Psets for Entity
            IDictionary<string, HashSet<string>> entPsetDict = new Dictionary<string, HashSet<string>>();
            foreach (KeyValuePair<string, IList<VersionSpecificPropertyDef>> pdefEntry in procPdef.allPDefDict)
            {
               foreach (VersionSpecificPropertyDef vPdef in pdefEntry.Value)
               {
                  //if (vPdef.IfcVersion.Equals(schemaName, StringComparison.InvariantCultureIgnoreCase))
                  {
                     IFCPropertySetDef psetDef = new IFCPropertySetDef();
                     psetDef.PsetName = vPdef.PropertySetDef.Name;
                     IList<string> props = new List<string>();
                     foreach (PropertySet.PsetProperty property in vPdef.PropertySetDef.properties)
                     {
                        // New style of property name (prefix with Pset name)
                        props.Add(psetDef.PsetName + "." + property.Name);
                     }
                     psetDef.Properties = props;
                     schemaEntities.PsetDefList.Add(psetDef);

                     // TODO: to check the appl classes either a type or not and check whether the pair (type or without) exists in entDict, if there is add 
                     foreach (string applEntity in vPdef.PropertySetDef.ApplicableClasses)
                     {
                        if (entPsetDict.ContainsKey(applEntity))
                        {
                           entPsetDict[applEntity].Add(vPdef.PropertySetDef.Name);
                        }
                        else
                        {
                           entPsetDict.Add(applEntity, new HashSet<string>() { vPdef.PropertySetDef.Name });
                        }

                        // The Pset will be valid for both the Instance and the Type. Check for that here and add if found
                        string entOrTypePair;
                        if (applEntity.Length > 4 && applEntity.EndsWith("Type"))
                           entOrTypePair = applEntity.Substring(0, applEntity.Length - 4);
                        else
                           entOrTypePair = applEntity + "Type";

                        if (aggregateEntities.Contains(entOrTypePair))
                        {
                           if (entPsetDict.ContainsKey(entOrTypePair))
                           {
                              entPsetDict[entOrTypePair].Add(vPdef.PropertySetDef.Name);
                           }
                           else
                           {
                              entPsetDict.Add(entOrTypePair, new HashSet<string>() { vPdef.PropertySetDef.Name });
                           }
                        }
                     }
                  }
               }
            }

#if FORNAX_EXTENSION
            LoadUserDefPsetFromLoc(schemaEntities, entPsetDict);

            // Generate the object hierarchy for the schema and write it into a file
            List<ObjectHierarchy> objHierarchies = ObjectHierarchyTool.GenerateObjectHierarchy(f.FullName);
            foreach (ObjectHierarchy objH in objHierarchies)
            {
               objHStream.WriteLine(objH.ToString());
            }
#endif

            // For every entity of the schema, collect the list of PredefinedType (obtained from the xsd), and collect all applicable
            //  Pset Definitions collected above
            foreach (KeyValuePair<string, IfcSchemaEntityNode> ent in entDict)
            {
               IFCEntityInfo entInfo = new IFCEntityInfo();

               // The abstract entity type is not going to be listed here as they can never be created
               if (ent.Value.isAbstract)
                  continue;

               // Collect only the IfcProducts or IfcGroup
               if (!ent.Value.IsSubTypeOf("IfcProduct") && !ent.Value.IsSubTypeOf("IfcGroup", strict: false) && !ent.Value.IsSubTypeOf("IfcTypeProduct"))
                  continue;

               entInfo.Entity = ent.Key;
               if (!string.IsNullOrEmpty(ent.Value.PredefinedType))
               {
                  if (entityTree.PredefinedTypeEnumDict.ContainsKey(ent.Value.PredefinedType))
                  {
                     entInfo.PredefinedType = entityTree.PredefinedTypeEnumDict[ent.Value.PredefinedType];
                  }
               }

               // Get Pset list that is applicable to this entity type
               if (entPsetDict.ContainsKey(entInfo.Entity))
               {
                  entInfo.PropertySets = entPsetDict[entInfo.Entity].ToList();
               }

#if FORNAX_EXTENSION
               // Add FORNAX special property sets IFCATTRIBUTES
               if (entInfo.PropertySets == null)
                  entInfo.PropertySets = new List<string>() { "IFCATTRIBUTES" };
               else
                  entInfo.PropertySets.Add("IFCATTRIBUTES");
#endif
               
               // Collect Pset that is applicable to the supertype of this entity
               IList<IfcSchemaEntityNode> supertypeList = IfcSchemaEntityTree.FindAllSuperTypes(schemaName, entInfo.Entity,
                  "IfcProduct", "IfcTypeProduct", "IfcGroup");
               if (supertypeList != null && supertypeList.Count > 0)
               {
                  foreach (IfcSchemaEntityNode superType in supertypeList)
                  {
                     if (entPsetDict.ContainsKey(superType.Name))
                     {
                        if (entInfo.PropertySets == null)
#if FORNAX_EXTENSION
                           entInfo.PropertySets = new List<string>() { "IFCATTRIBUTES" };
#else
                           entInfo.PropertySets = new List<string>();
#endif

                        foreach (string pset in entPsetDict[superType.Name])
                           entInfo.PropertySets.Add(pset);
                     }
                  }
               }

               schemaEntities.EntityList.Add(entInfo);
            }
            fxEntityNPsetList.Add(schemaEntities);
         }
         ser.WriteObject(fs, fxEntityNPsetList);
         fs.Close();

         if (aggregateEntities.Count > 0)
         {
            string entityList;
            entityList = "using System;"
                        + "\r\nusing System.Collections.Generic;"
                        + "\r\nusing System.Linq;"
                        + "\r\nusing System.Text;"
                        + "\r\n"
                        + "\r\nnamespace Revit.IFC.Common.Enums"
                        + "\r\n{"
                        + "\r\n   /// <summary>"
                        + "\r\n   /// IFC entity types. Combining IFC2x3 and IFC4 (Add2) entities."
                        + "\r\n   /// List of Entities for IFC2x is found in IFC2xEntityType.cs"
                        + "\r\n   /// List of Entities for IFC4 is found in IFC4EntityType.cs"
                        + "\r\n   /// </summary>"
                        + "\r\n   public enum IFCEntityType"
                        + "\r\n   {";

            foreach (string ent in aggregateEntities)
            {
               entityList += "\r\n      /// <summary>"
                           + "\r\n      /// IFC Entity " + ent + " enumeration"
                           + "\r\n      /// </summary>"
                           + "\r\n      " + ent + ",\n";
            }
            entityList += "\r\n      Unknown,"
                        + "\r\n      DontExport"
                        + "\r\n   }"
                        + "\r\n}";
            File.WriteAllText(outputFolder + @"\IFCEntityType.cs", entityList);
         }

         foreach (IFCEntityAndPsetList fxEntityNPset in fxEntityNPsetList)
         {
            string entityList;
            entityList = "using System;"
                        + "\r\nusing System.Collections.Generic;"
                        + "\r\nusing System.Linq;"
                        + "\r\nusing System.Text;"
                        + "\r\n"
                        + "\r\nnamespace Revit.IFC.Common.Enums." + fxEntityNPset.Version
                        + "\r\n{"
                        + "\r\n   /// <summary>"
                        + "\r\n   /// List of Entities for " + fxEntityNPset.Version
                        + "\r\n   /// </summary>"
                        + "\r\n   public enum EntityType"
                        + "\r\n   {";

            foreach (IFCEntityInfo entInfo in fxEntityNPset.EntityList)
            {
               entityList += "\r\n      /// <summary>"
                           + "\r\n      /// IFC Entity " + entInfo.Entity + " enumeration"
                           + "\r\n      /// </summary>"
                           + "\r\n      " + entInfo.Entity + ",\r\n";
            }
            entityList += "\r\n      Unknown,"
                        + "\r\n      DontExport"
                        + "\r\n   }"
                        + "\r\n}";
            File.WriteAllText(outputFolder + @"\" + fxEntityNPset.Version + "EntityType.cs", entityList);
         }

         // Only allows test when only one schema is selected
         if (listBox_schemaList.SelectedItems.Count == 1)
         {
            button_subtypeTest.IsEnabled = true;
            button_supertypeTest.IsEnabled = true;
            button_ExportInfoPair.IsEnabled = true;
         }
         else
         {
            button_subtypeTest.IsEnabled = false;
            button_supertypeTest.IsEnabled = false;
            button_ExportInfoPair.IsEnabled = false;
         }

         if (logF != null)
            logF.Close();

#if FORNAX_EXTENSION
         if (objHStream != null)
            objHStream.Close();
#endif
      }

      private void button_Cancel_Click(object sender, RoutedEventArgs e)
      {
         if (logF != null)
            logF.Close();
         Close();
      }

      private void button_browseOutputFolder_Click(object sender, RoutedEventArgs e)
      {
         var dialog = new FolderBrowserDialog();
         dialog.RootFolder = Environment.SpecialFolder.MyComputer;
         dialog.ShowDialog();
         textBox_outputFolder.Text = dialog.SelectedPath;
         outputFolder = dialog.SelectedPath;
      }

      private void button_subtypeTest_Click(object sender, RoutedEventArgs e)
      {
         if (listBox_schemaList.SelectedItems.Count == 0 || listBox_schemaList.SelectedItems.Count > 1)
            return;

         DirectoryInfo dInfo = new DirectoryInfo(textBox_folderLocation.Text);
         FileInfo f = dInfo.GetFiles(listBox_schemaList.SelectedItem.ToString()).First();
         string schemaName = f.Name.Replace(".xsd", "");

         if (string.IsNullOrEmpty(textBox_type1.Text) || string.IsNullOrEmpty(textBox_type2.Text))
            return;

         bool res = IfcSchemaEntityTree.IsSubTypeOf(schemaName, textBox_type1.Text, textBox_type2.Text);
         if (res)
            checkBox_testResult.IsChecked = true;
         else
            checkBox_testResult.IsChecked = false;
      }

      private void button_supertypeTest_Click(object sender, RoutedEventArgs e)
      {
         if (listBox_schemaList.SelectedItems.Count == 0 || listBox_schemaList.SelectedItems.Count > 1)
            return;

         DirectoryInfo dInfo = new DirectoryInfo(textBox_folderLocation.Text);
         FileInfo f = dInfo.GetFiles(listBox_schemaList.SelectedItem.ToString()).First();
         string schemaName = f.Name.Replace(".xsd", "");

         if (string.IsNullOrEmpty(textBox_type1.Text) || string.IsNullOrEmpty(textBox_type2.Text))
            return;

         bool res = IfcSchemaEntityTree.IsSuperTypeOf(schemaName, textBox_type1.Text, textBox_type2.Text);
         if (res)
            checkBox_testResult.IsChecked = true;
         else
            checkBox_testResult.IsChecked = false;
      }

      private void textBox_type1_TextChanged(object sender, TextChangedEventArgs e)
      {
         checkBox_testResult.IsChecked = false;
      }

      private void textBox_type2_TextChanged(object sender, TextChangedEventArgs e)
      {
         checkBox_testResult.IsChecked = false;
      }

      private void textBox_outputFolder_TextChanged(object sender, TextChangedEventArgs e)
      {
         outputFolder = textBox_outputFolder.Text;
      }

      private void Button_ExportInfoPair_Click(object sender, RoutedEventArgs e)
      {
         if (string.IsNullOrEmpty(textBox_type1.Text))
            return;

         string[] info = textBox_type1.Text.Split('.');
         string entity = info[0];
         string predefType = null;
         if (info.Count() > 1)
            predefType = info[1];

         IFCExportInfoPair exportInfo = new IFCExportInfoPair();
         IFCEntityType entType = IFCEntityType.UnKnown;
         if (!Enum.TryParse<IFCEntityType>(entity, out entType))
            return;

         exportInfo.SetByTypeAndPredefinedType(entType, predefType);
         textBox_type2.Text = "Instance: " + exportInfo.ExportInstance + "\nType: " + exportInfo.ExportType
               + "\r\nPredefinedTpe: " + exportInfo.PredefinedType;
      }

      private void button_browseUserDefPsetLoc_Click(object sender, RoutedEventArgs e)
      {
         var dialog = new FolderBrowserDialog();
         dialog.RootFolder = Environment.SpecialFolder.MyComputer;
         dialog.ShowDialog();
         textBox_UserDefPsetLoc.Text = dialog.SelectedPath;
         userDefPsetLoc = dialog.SelectedPath;
      }

      private void textBox_UserDefPsetLoc_TextChanged(object sender, TextChangedEventArgs e)
      {
         userDefPsetLoc = textBox_UserDefPsetLoc.Text;
      }

#if FORNAX_EXTENSION
      void LoadUserDefPsetFromLoc(IFCEntityAndPsetList schemaEntities, IDictionary<string, HashSet<string>> entPsetDict)
      {
         if (!Directory.Exists(userDefPsetLoc))
            return;

         // Add IFCATTRIBUTES Pset for Fornax extension
         schemaEntities.PsetDefList.Add(DefineFXProperties());

         DirectoryInfo dInfo = new DirectoryInfo(userDefPsetLoc);
         foreach (FileInfo psetDefFile in dInfo.GetFiles("*.txt"))
         {
            foreach (PsetDefinition pset in LoadUserDefinedPset(psetDefFile.FullName))
            {
               IFCPropertySetDef psetDef = new IFCPropertySetDef();
               psetDef.PsetName = pset.Name;
               foreach (PsetProperty property in pset.properties)
               {
                  if (psetDef.Properties == null)
                     psetDef.Properties = new List<string>();
                  psetDef.Properties.Add(property.Name);
               }
               schemaEntities.PsetDefList.Add(psetDef);

               foreach (string applEntity in pset.ApplicableClasses)
               {
                  // The Pset will be valid for both the Instance and the Type. Check for that here and add if found
                  string entOrTypePair;
                  if (applEntity.Length > 4 && applEntity.EndsWith("Type"))
                     entOrTypePair = applEntity.Substring(0, applEntity.Length - 4);
                  else
                     entOrTypePair = applEntity + "Type";

                  if (entPsetDict.ContainsKey(applEntity))
                  {
                     entPsetDict[applEntity].Add(pset.Name);
                  }
                  else
                  {
                     entPsetDict.Add(applEntity, new HashSet<string>() { pset.Name });
                  }

                  // For the other pair (Type or Instance)
                  if (entPsetDict.ContainsKey(entOrTypePair))
                  {
                     entPsetDict[entOrTypePair].Add(pset.Name);
                  }
                  else
                  {
                     entPsetDict.Add(entOrTypePair, new HashSet<string>() { pset.Name });
                  }
               }
            }
         }
      }
      
      IFCPropertySetDef DefineFXProperties ()
      {  
         // For IFCATTRIBUTES
         IFCPropertySetDef pset = new IFCPropertySetDef();
         pset.PsetName = "IFCATTRIBUTES";
         IList<string> props = new List<string>();
         props.Add("AreaClassification");
         props.Add("Building Name");
         props.Add("Description");
         props.Add("DrainageBoundary");
         props.Add("Checkbox");
         props.Add("Level");
         props.Add("LongName");
         props.Add("Material");
         props.Add("Name");
         props.Add("ObjectType");
         props.Add("OccupancyType");
         props.Add("PredefinedType");
         props.Add("ProjectDevelopmentType");
         props.Add("Project Location");
         props.Add("System");
         pset.Properties = props;                       

         return pset;
      }

      /// <summary>
      /// Load user-defined Property set (duplicate of the one from Revit.IFC.Exporter PropertyMap.cs
      /// Format:
      ///    PropertySet: <Pset_name> I[nstance]/T[ype] <IFC entity list separated by ','> 
      ///              Property_name   Data_type   Revit_Parameter
      ///              ...
      /// Datatype supported: Text, Integer, Real, Boolean
      /// Line divider between Property Mapping and User defined property sets:
      ///     #! UserDefinedPset
      /// </summary>
      /// <returns>List of property set definitions</returns>
      public static IEnumerable<PsetDefinition> LoadUserDefinedPset(string filename)
      {
         List<PsetDefinition> userDefinedPropertySets = new List<PsetDefinition>();

         try
         {
            if (!File.Exists(filename))
               return userDefinedPropertySets;

            // Format: PropertSet: <Pset_name> I[nstance]/T[ype] <IFC entity list separated by ','> 
            //              Property_name   Data_type   Revit_Parameter
            // ** For now it only works for simple property with single value (datatype supported: Text, Integer, Real and Boolean)
            using (StreamReader sr = new StreamReader(filename))
            {
               string line;

               // current property set
               PsetDefinition userDefinedPropertySet = null;
               while ((line = sr.ReadLine()) != null)
               {
                  line = line.TrimStart(' ', '\t');

                  if (string.IsNullOrEmpty(line) || line[0] == '#')
                     continue;

                  string[] split = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                  if (split.Length >= 4 && string.Compare(split[0], "PropertySet:", true) == 0) // Any entry with less than 3 parameters is malformed.
                  {
                     userDefinedPropertySet = new PsetDefinition()
                     {
                        Name = split[1],
                        //Type = split[2],
                        ApplicableClasses = split[3].Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                     };

                     userDefinedPropertySets.Add(userDefinedPropertySet);
                  }
                  else if (split.Length >= 2 && userDefinedPropertySet != null) // Skip property definitions outside of property set block.
                  {
                     PsetProperty userDefinedProperty = new PsetProperty
                     {
                        Name = split[0],
                        //userDefinedProperty.ParsePropertyType(split[1]);
                     };
 
                     if (userDefinedPropertySet.properties == null)
                        userDefinedPropertySet.properties = new HashSet<PsetProperty>();
                     userDefinedPropertySet.properties.Add(userDefinedProperty);
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
#endif

   }
}
