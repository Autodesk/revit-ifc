using Autodesk.Revit.DB;
using Revit.IFC.Common.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitIFCTools
{
   public class ObjectHierarchy
   {
      private string IfcSchemaver;
      private string ElementType;
      private string ElementSubType;
      private bool IsAbstract;
      private int LevelsRemoved;

      /// <summary>
      /// Constructor of Object Hierarchy 
      /// </summary>
      /// <param name="ifcSchemaver"></param>
      /// <param name="elementType"></param>
      /// <param name="elementSubType"></param>
      /// <param name="isAbstract"></param>
      /// <param name="levelsRemoved"></param>
      public ObjectHierarchy(string ifcSchemaver, string elementType,
         string elementSubType, bool isAbstract, int levelsRemoved)
      {
         IfcSchemaver = ifcSchemaver;
         ElementType = elementType;
         ElementSubType = elementSubType;
         IsAbstract = isAbstract;
         LevelsRemoved = levelsRemoved;
      }

      public override string ToString()
      {
         return $"insert into bimrl_objecthierarchy values ('{IfcSchemaver.ToUpper()}', '{ElementType.ToUpper()}', '{ElementSubType.ToUpper()}', {IsAbstract}, '{LevelsRemoved}');";
      }
   }

   public class ObjectHierarchyTool
   {
      /// <summary>
      /// This method generates object hierarchy based on the ifcshema
      /// </summary>
      /// <param name="ifcSchemaFile"></param>
      /// <returns>List of object hierarchies</returns>
      public static List<ObjectHierarchy> GenerateObjectHierarchy(string ifcSchemaFile)
      {
         if (string.IsNullOrEmpty(ifcSchemaFile))
         {
            return new List<ObjectHierarchy>();
         }

         FileInfo schemaFileInfo = new FileInfo(ifcSchemaFile);
         IfcSchemaEntityTree entityTree = new IfcSchemaEntityTree();
         bool newLoad = ProcessIFCXMLSchema.ProcessIFCSchema(schemaFileInfo, ref entityTree);
         if (newLoad)
         {
            string ifcVersion = Path.GetFileNameWithoutExtension(schemaFileInfo.Name);
            List<ObjectHierarchy> objectHierarchies = new List<ObjectHierarchy>();
            foreach (var entityDict in entityTree.IfcEntityDict)
            {
               IfcSchemaEntityNode parentEntityNode = entityDict.Value;

               if (parentEntityNode.Name.Contains("IfcProduct") || parentEntityNode.Name.Contains("IfcTypeProduct") || parentEntityNode.Name.Contains("IfcGroup"))
               {
                  //check if current has a child
                  IList<IfcSchemaEntityNode> childNodes = parentEntityNode.GetChildren();
                  //if (childNodes.Count == 0)
                  {
                     objectHierarchies.Add(new ObjectHierarchy(ifcVersion, parentEntityNode.Name, parentEntityNode.Name,
                        parentEntityNode.isAbstract, 0));
                     //continue;
                  }

                  foreach (IfcSchemaEntityNode childNode in childNodes)
                  {
                     List<ObjectHierarchy> hierarchies = CollectObjectHierarchies(childNode, ifcVersion, parentEntityNode.Name);
                     objectHierarchies.AddRange(hierarchies);
                  }
               }
            }
            return objectHierarchies;
         }
         return new List<ObjectHierarchy>();
      }

      /// <summary>
      /// This method is a recursive method that checks the current node properties
      /// and collects 
      /// </summary>
      /// <param name="childNode"></param>
      /// <param name="ifcSchemaVersion"></param>
      /// <param name="currentNodeName"></param>
      /// <returns></returns>
      private static List<ObjectHierarchy> CollectObjectHierarchies(IfcSchemaEntityNode childNode, string ifcSchemaVersion, string currentNodeName)
      {
         List<ObjectHierarchy> objectHierarchies = new List<ObjectHierarchy>();

         //check if current child node has a child
         IList<IfcSchemaEntityNode> descendantNodes = childNode.GetChildren();
         //if (descendantNodes.Count == 0 || !childNode.isAbstract)
         {
            int level = 0;
            //add the current child node into the list
            objectHierarchies.Add(new ObjectHierarchy(ifcSchemaVersion, childNode.Name, childNode.Name,
                      childNode.isAbstract, level));

            IfcSchemaEntityNode parentNode = childNode.GetParent();

            //add every current child node parent's parent node into the list
            while (parentNode != null)
            {
               level++;
               //stops the loop when the current node and child node's parent are the same
               if (currentNodeName != parentNode.Name)
               {
                  objectHierarchies.Add(new ObjectHierarchy(ifcSchemaVersion, parentNode.Name, childNode.Name,
                     parentNode.isAbstract, level));
               }
               else
               {
                  objectHierarchies.Add(new ObjectHierarchy(ifcSchemaVersion, parentNode.Name, childNode.Name,
                     parentNode.isAbstract, level));
                  break;
               }
               parentNode = parentNode.GetParent();
            }
         }

         foreach (var descendantNode in descendantNodes)
         {
            objectHierarchies.AddRange(CollectObjectHierarchies(descendantNode, ifcSchemaVersion, currentNodeName));
         }
         return objectHierarchies;
      }

      List<ObjectHierarchy> GenerateHierarchy(string ifcSchemaVersion, IfcSchemaEntityNode currNode, string stopNodeName)
      {
         List<ObjectHierarchy> objectHierarchies = new List<ObjectHierarchy>();
         int level = 0;
         //add the current child node into the list
         objectHierarchies.Add(new ObjectHierarchy(ifcSchemaVersion, currNode.Name, currNode.Name,
                currNode.isAbstract, level));

         IfcSchemaEntityNode parentNode = currNode.GetParent();

         //add every current child node parent's parent node into the list
         while (parentNode != null)
         {
            level++;
            //stops the loop when the current node and child node's parent are the same
            if (stopNodeName != parentNode.Name)
            {
               objectHierarchies.Add(new ObjectHierarchy(ifcSchemaVersion, parentNode.Name, currNode.Name,
                  parentNode.isAbstract, level));
            }
            else
            {
               objectHierarchies.Add(new ObjectHierarchy(ifcSchemaVersion, parentNode.Name, currNode.Name,
                  parentNode.isAbstract, level));
               break;
            }
            parentNode = parentNode.GetParent();
         }
         return objectHierarchies;
      }
   }
}
