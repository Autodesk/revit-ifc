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

//using GeometryGym.Ifc;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Type or Instance enum
   /// </summary>
   public enum TypeOrInstance
   {
      Type,
      Instance
   }

   /// <summary>
   /// PropertSet definition struct
   /// </summary>
   public struct PropertySetDef
   {
      public string propertySetName;
      public string propertySetDescription;
      public IList<PropertyDef> propertyDefs;
      public TypeOrInstance applicableTo;
      public IList<string> applicableElements;
   }

   /// <summary>
   /// Property 
   /// </summary>
   public class PropertyDef
   {
      public string PropertyName;
      public string PropertyDataType;
      public List<PropertyParameterDefinition> ParameterDefinitions = new List<PropertyParameterDefinition>();
      //public GeometryGym.Ifc.IfcValue DefaultValue = null;
      public PropertyDef(string propertyName)
      {
         PropertyName = propertyName;
         ParameterDefinitions.Add(new PropertyParameterDefinition(propertyName));
      }
      public PropertyDef(string propertyName, PropertyParameterDefinition definition)
      {
         PropertyName = propertyName;
         ParameterDefinitions.Add(definition);
      }
      public PropertyDef(string propertyName, IEnumerable<PropertyParameterDefinition> definitions)
      {
         PropertyName = propertyName;
         ParameterDefinitions.AddRange(definitions);
      }
   }
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
      public static IList<PropertySetDef> LoadUserDefinedPset()
      {
         IList<PropertySetDef> userDefinedPsets = new List<PropertySetDef>();
         PropertySetDef userDefinedPset;

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
            //if (string.Compare(extension, ".ifcxml", true) == 0 || string.Compare(extension, ".ifcjson", true) == 0 || string.Compare(extension, ".ifc", true) == 0)
            //{
            //DatabaseIfc db = new DatabaseIfc(filename);
            //IfcContext context = db.Context;
            //if (context == null)
            //return userDefinedPsets;
            //foreach (IfcRelDeclares relDeclares in context.Declares)
            //{
            //foreach (IfcPropertySetTemplate template in relDeclares.RelatedDefinitions.OfType<IfcPropertySetTemplate>())
            //{
            //userDefinedPset = new PropertySetDef();
            //userDefinedPset.applicableElements = new List<string>(template.ApplicableEntity.Split(",".ToCharArray()));
            //userDefinedPset.propertyDefs = new List<PropertyDef>();

            //userDefinedPset.propertySetName = template.Name;
            //userDefinedPset.propertySetDescription = template.Description;
            //userDefinedPset.applicableTo = (template.TemplateType == IfcPropertySetTemplateTypeEnum.PSET_TYPEDRIVENONLY || template.TemplateType == IfcPropertySetTemplateTypeEnum.QTO_TYPEDRIVENONLY ? TypeOrInstance.Type : TypeOrInstance.Instance);
            //userDefinedPsets.Add(userDefinedPset);
            //foreach (IfcPropertyTemplate propTemplate in template.HasPropertyTemplates)
            //{
            //List<PropertyParameterDefinition> definitions = new List<PropertyParameterDefinition>();
            //IfcValue value = null;
            //foreach (IfcRelAssociates associates in propTemplate.HasAssociations)
            //{
            //IfcRelAssociatesClassification associatesClassification = associates as IfcRelAssociatesClassification;
            //if (associatesClassification != null)
            //{
            //IfcClassificationReference classificationReference = associatesClassification.RelatingClassification as IfcClassificationReference;
            //if (classificationReference != null)
            //{
            //string id = classificationReference.Identification;
            //if (id.ToLower().StartsWith("builtinparameter."))
            //{
            //Autodesk.Revit.DB.BuiltInParameter builtInParameter = Autodesk.Revit.DB.BuiltInParameter.INVALID;
            //id = id.Substring(17);
            //if (Enum.TryParse<Autodesk.Revit.DB.BuiltInParameter>(id, out builtInParameter) && builtInParameter != Autodesk.Revit.DB.BuiltInParameter.INVALID)
            //definitions.Add(new PropertyParameterDefinition(builtInParameter));
            //else
            //{
            //}
            //}
            //else
            //definitions.Add(new PropertyParameterDefinition(id)); 
            //}
            //}
            //else
            //{
            //IfcRelAssociatesConstraint associatesConstraint = associates as IfcRelAssociatesConstraint;
            //if (associatesConstraint != null)
            //{
            //IfcMetric metric = associatesConstraint.RelatingConstraint as IfcMetric;
            //if (metric != null)
            //{
            //value = metric.DataValue as IfcValue;
            //}
            //}
            //}
            //}
            //if (definitions.Count > 0 || value != null)
            //{
            //PropertyDef propertyDefUnit = new PropertyDef(propTemplate.Name, definitions);
            //IfcSimplePropertyTemplate simple = propTemplate as IfcSimplePropertyTemplate;
            //if (simple != null)
            //{
            //propertyDefUnit.PropertyDataType = simple.PrimaryMeasureType.ToLower().Replace("ifc", "");
            //}
            //propertyDefUnit.DefaultValue = value;
            //userDefinedPset.propertyDefs.Add(propertyDefUnit);
            //}
            //}
            //}
            //}
            //}
            //else
            {
               using (StreamReader sr = new StreamReader(filename))
               {
                  string line;

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
                           userDefinedPset = new PropertySetDef();
                           userDefinedPset.applicableElements = new List<string>();
                           userDefinedPset.propertyDefs = new List<PropertyDef>();

                           if (split.Count() >= 4)         // Any entry with less than 3 par is malformed
                           {
                              userDefinedPset.propertySetName = split[1];
                              switch (split[2][0])
                              {
                                 case 'T':
                                    userDefinedPset.applicableTo = TypeOrInstance.Type;
                                    break;
                                 case 'I':
                                    userDefinedPset.applicableTo = TypeOrInstance.Instance;
                                    break;
                                 default:
                                    userDefinedPset.applicableTo = TypeOrInstance.Instance;
                                    break;
                              }
                              string[] elemlist = split[3].Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                              foreach (string elem in elemlist)
                                 userDefinedPset.applicableElements.Add(elem);

                              userDefinedPsets.Add(userDefinedPset);
                           }
                        }
                        else
                        {
                           PropertyDef propertyDefUnit = null;
                           if (split.Count() >= 2)
                           {
                              if (split.Count() >= 3)
                              {
                                 propertyDefUnit = new PropertyDef(split[0], new PropertyParameterDefinition(split[2]));
                              }
                              else
                              {
                                 propertyDefUnit = new PropertyDef(split[0]);
                              }
                              propertyDefUnit.PropertyDataType = split[1];
                              userDefinedPsets.Last().propertyDefs.Add(propertyDefUnit);
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
