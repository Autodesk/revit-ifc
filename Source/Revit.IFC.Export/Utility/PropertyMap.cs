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
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using GeometryGym.Ifc;

namespace Revit.IFC.Export.Utility
{
   public class PropertyParameterDefinition
   {
      public string RevitParameterName = "";
      public Autodesk.Revit.DB.BuiltInParameter RevitBuiltInParameter = Autodesk.Revit.DB.BuiltInParameter.INVALID;
      public PropertyParameterDefinition(string revitParameterName)
      {
         RevitParameterName = revitParameterName;
      }
      public PropertyParameterDefinition(Autodesk.Revit.DB.BuiltInParameter builtInParameter)
      {
         RevitBuiltInParameter = builtInParameter;
      }
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
            if (split.Count() == 3)
               parameterMap.Add(Tuple.Create(split[0], split[1]), split[2]);
         }
      }

      /// <summary>
      /// Load user-defined Property set
      /// Format:
      ///    PropertSet: <Pset_name> I[nstance]/T[ype] <IFC entity list separated by ','> 
      ///              Property_name   Data_type   Revit_Parameter
      ///              ...
      /// Datatype supported: Text, Integer, Real, Boolean
      /// Line divider between Property Mapping and User defined property sets:
      ///     #! UserDefinedPset
      /// </summary>
      /// <returns>List of property set definitions</returns>
      public static IEnumerable<IfcPropertySetTemplate> LoadUserDefinedPset()
      {
         List<IfcPropertySetTemplate> userDefinedPsets = new List<IfcPropertySetTemplate>();

         try
         {
            string filename = ExporterCacheManager.ExportOptionsCache.PropertySetOptions.ExportUserDefinedPsetsFileName;
            if (!File.Exists(filename))
            {
               // This allows for the original behavior of looking in the directory of the export DLL to look for the default file name.
               filename = GetUserDefPsetFilename();
            }
            if (!File.Exists(filename))
               return userDefinedPsets;

            string extension = Path.GetExtension(filename);
            if (string.Compare(extension, ".ifcxml", true) == 0 || string.Compare(extension, ".ifcjson", true) == 0 || string.Compare(extension, ".ifc", true) == 0)
            {
               DatabaseIfc db = new DatabaseIfc(filename);
               IfcContext context = db.Context;
               if (context == null)
                  return userDefinedPsets;
               foreach (IfcRelDeclares relDeclares in context.Declares)
               {
                  userDefinedPsets.AddRange(relDeclares.RelatedDefinitions.OfType<IfcPropertySetTemplate>());
               }
            }
            else
            { 
               using (StreamReader sr = new StreamReader(filename))
               {
                  string line;

                  DatabaseIfc db = new DatabaseIfc(false, ReleaseVersion.IFC4);
                  IfcPropertySetTemplate userDefinedPset = null;
                  while ((line = sr.ReadLine()) != null)
                  {
                     line.TrimStart(' ', '\t');

                     if (String.IsNullOrEmpty(line)) continue;
                     if (line[0] != '#')
                     {
                        // Format: PropertSet: <Pset_name> I[nstance]/T[ype] <IFC entity list separated by ','> 
                        //              Property_name   Data_type   Revit_Parameter
                        // ** For now it only works for simple property with single value (datatype supported: Text, Integer, Real and Boolean)

                        string[] split = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (string.Compare(split[0], "PropertySet:", true) == 0)
                        {
                           userDefinedPset = new IfcPropertySetTemplate(db, split.Length > 2 ? split[1] : "Unknown");
                           if (split.Count() >= 4)         // Any entry with less than 3 par is malformed
                           {
                              switch (split[2][0])
                              {
                                 case 'T':
                                    userDefinedPset.TemplateType = IfcPropertySetTemplateTypeEnum.PSET_TYPEDRIVENONLY;
                                    break;
                                 case 'I':
                                    userDefinedPset.TemplateType = IfcPropertySetTemplateTypeEnum.PSET_OCCURRENCEDRIVEN;
                                    break;
                                 default:
                                    userDefinedPset.TemplateType = IfcPropertySetTemplateTypeEnum.PSET_OCCURRENCEDRIVEN;
                                    break;
                              }
                              userDefinedPset.ApplicableEntity = string.Join(",", split[3].Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries));
                              userDefinedPsets.Add(userDefinedPset);
                           }
                        }
                        else
                        {
                           if (split.Count() >= 2)
                           {
                              string propertyTemplateName = split[0];
                              IfcSimplePropertyTemplate propertyDefUnit = userDefinedPset[propertyTemplateName] as IfcSimplePropertyTemplate;
                              if(propertyDefUnit == null)
                                 userDefinedPset.AddPropertyTemplate(propertyDefUnit = new IfcSimplePropertyTemplate(db, split[0]));
                              if (split.Count() >= 3 && !string.IsNullOrEmpty(split[2]))
                              {
                                 new IfcRelAssociatesClassification(new IfcClassificationReference(db) { Identification = split[2] }, propertyDefUnit);
                              }
                              if(!string.IsNullOrEmpty(split[1]))
                                 propertyDefUnit.PrimaryMeasureType = "Ifc" + split[1];
                           }
                        }
                     }
                  }
               }
            }
         }
         catch (Exception e)
         {
            Console.WriteLine("The file could not be read:");
            Console.WriteLine(e.Message);
         }

         return userDefinedPsets;
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
