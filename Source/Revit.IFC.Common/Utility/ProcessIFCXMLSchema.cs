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

         loadedSchema = ifcxmlSchemaFile.Name;
         IfcSchemaEntityTree.Initialize(loadedSchema);
         XmlTextReader reader = new XmlTextReader(ifcxmlSchemaFile.FullName);
         XmlSchema theSchema = XmlSchema.Read(reader, ValidationCallback);
         foreach (XmlSchemaObject item in theSchema.Items)
         {
            if (!(item is XmlSchemaComplexType))
               continue;

            XmlSchemaComplexType ct = item as XmlSchemaComplexType;
            string entityName = ct.Name;

            if (string.Compare(entityName, 0, "Ifc", 0, 3, ignoreCase: true) != 0)
               continue;

            string parentName = string.Empty;

            if (ct.ContentModel == null)
               continue;

            if (ct.ContentModel.Parent == null)
               continue;

            if (ct.ContentModel.Parent is XmlSchemaComplexType)
            {
               XmlSchemaComplexType parent = ct.ContentModel.Parent as XmlSchemaComplexType;
               XmlSchemaSimpleContentExtension parentSimpleType = parent.ContentModel.Content as XmlSchemaSimpleContentExtension;
               XmlSchemaComplexContentExtension parentComplexType = parent.ContentModel.Content as XmlSchemaComplexContentExtension;
               if (parentSimpleType != null)
                  parentName = parentSimpleType.BaseTypeName.Name;
               if (parentComplexType != null)
                  parentName = parentComplexType.BaseTypeName.Name;
            }

            IfcSchemaEntityTree.Add(entityName, parentName, isAbstract: ct.IsAbstract);
         }
         return true;
      }

      static void ValidationCallback(object sender, ValidationEventArgs args)
      {
         Console.WriteLine(args.Message);
      }
   }
}
