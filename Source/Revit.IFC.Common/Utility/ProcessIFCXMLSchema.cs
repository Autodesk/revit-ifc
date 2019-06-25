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
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace Revit.IFC.Common.Utility
{
   public class ProcessIFCXMLSchema
   {
      static string loadedSchema = string.Empty;

      /// <summary>
      /// Process an IFCXML schema file
      /// </summary>
      /// <param name="ifcxmlSchemaFile">the IfcXML schema file info</param>
      public static bool ProcessIFCSchema(FileInfo ifcxmlSchemaFile)
      {
         if (ifcxmlSchemaFile.Name.Equals(loadedSchema) && IfcSchemaEntityTree.EntityDict.Count > 0)
            return false;     // The schema file has been processed and loaded before

         loadedSchema = Path.GetFileNameWithoutExtension(ifcxmlSchemaFile.Name);
         IfcSchemaEntityTree.Initialize(loadedSchema);
         XmlTextReader reader = new XmlTextReader(ifcxmlSchemaFile.FullName);
         XmlSchema theSchema = XmlSchema.Read(reader, ValidationCallback);
         foreach (XmlSchemaObject item in theSchema.Items)
         {
            if (item is XmlSchemaComplexType)
            {

               XmlSchemaComplexType ct = item as XmlSchemaComplexType;
               string entityName = ct.Name;

               if (string.Compare(entityName, 0, "Ifc", 0, 3, ignoreCase: true) != 0)
                  continue;

               string parentName = string.Empty;

               if (ct.ContentModel == null)
                  continue;

               if (ct.ContentModel.Parent == null)
                  continue;

               string predefTypeEnum = null;
               if (ct.ContentModel.Parent is XmlSchemaComplexType)
               {
                  XmlSchemaComplexType parent = ct.ContentModel.Parent as XmlSchemaComplexType;
                  XmlSchemaSimpleContentExtension parentSimpleType = parent.ContentModel.Content as XmlSchemaSimpleContentExtension;
                  XmlSchemaComplexContentExtension parentComplexType = parent.ContentModel.Content as XmlSchemaComplexContentExtension;
                  if (parentSimpleType != null)
                  {
                     parentName = parentSimpleType.BaseTypeName.Name;
                     foreach (XmlSchemaAttribute attr in parentSimpleType.Attributes)
                     {
                        if (attr.Name != null && attr.Name.Equals("PredefinedType", StringComparison.InvariantCultureIgnoreCase))
                           predefTypeEnum = attr.SchemaTypeName.Name;
                     }
                  }
                  if (parentComplexType != null)
                  {
                     parentName = parentComplexType.BaseTypeName.Name;
                     foreach (XmlSchemaAttribute attr in parentComplexType.Attributes)
                     {
                        if (attr.Name != null && attr.Name.Equals("PredefinedType", StringComparison.InvariantCultureIgnoreCase))
                        {
                           predefTypeEnum = attr.SchemaTypeName.Name;
                           break;
                        }
                     }

                     if (string.IsNullOrEmpty(predefTypeEnum) && parentComplexType.Particle != null)
                     { 
                        XmlSchemaSequence seq = parentComplexType.Particle as XmlSchemaSequence;
                        if (seq != null)
                        {
                           foreach (XmlSchemaElement elem in seq.Items)
                           {
                              if (elem.Name != null && elem.Name.Equals("PredefinedType", StringComparison.InvariantCultureIgnoreCase))
                              {
                                 predefTypeEnum = elem.SchemaTypeName.Name;
                                 break;
                              }
                           }
                        }
                     }
                  }
               }

               IfcSchemaEntityTree.Add(entityName, parentName, predefTypeEnum, isAbstract: ct.IsAbstract);
            }
            else if (item is XmlSchemaSimpleType)
            {
               XmlSchemaSimpleType st = item as XmlSchemaSimpleType;
               if (st.Name.StartsWith("Ifc", StringComparison.InvariantCultureIgnoreCase)
                  && st.Name.EndsWith("Enum", StringComparison.InvariantCultureIgnoreCase))
               {
                  string enumName = st.Name;
                  XmlSchemaSimpleTypeRestriction enums = st.Content as XmlSchemaSimpleTypeRestriction;
                  if (enums != null)
                  {
                     IList<string> enumValueList = new List<string>();
                     foreach (XmlSchemaEnumerationFacet enumFacet in enums.Facets)
                     {
                        if (IfcSchemaEntityTree.PredefinedTypeEnumDict.ContainsKey(enumName))
                        {
                           IfcSchemaEntityTree.PredefinedTypeEnumDict[enumName].Add(enumFacet.Value.ToUpper());
                        }
                        else
                        {
                           enumValueList.Add(enumFacet.Value.ToUpper());
                        }
                     }
                     if (enumValueList.Count > 0)
                        IfcSchemaEntityTree.PredefinedTypeEnumDict.Add(enumName, enumValueList);
                  }
               }
            }
         }
         return true;
      }

      static void ValidationCallback(object sender, ValidationEventArgs args)
      {
         Console.WriteLine(args.Message);
      }
   }
}
