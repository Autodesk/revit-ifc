using Revit.IFC.Common.Enums;
using System;
using System.Collections.Generic;

namespace Revit.IFC.Common.Utility
{
   /// <summary>
   /// A Class that represent a node in the Ifc schema entity tree
   /// </summary>
   public class IfcSchemaEntityNode
   {
      IfcSchemaEntityNode superType = null;
      IList<IfcSchemaEntityNode> subType = null;

      /// <summary>
      /// Entity node name
      /// </summary>
      public IFCEntityType EntityType { get; private set; } = IFCEntityType.UnKnown;

      /// <summary>
      /// Entity node name
      /// </summary>
      public string Name { get; private set; } = null;

      private void SetNameAndType(string name)
      {
         Name = name;
         // There is some nodes that have abritrary names - ignore those.
         EntityType = Enum.TryParse(name, true, out IFCEntityType entityType) ? entityType : IFCEntityType.UnKnown;
      }

      /// <summary>
      /// The status whether the entity is Abstract type
      /// </summary>
      public bool IsAbstract { get; set; }

      /// <summary>
      /// The predefined type
      /// </summary>
      public string PredefinedType { get; set; }

      /// <summary>
      /// Create the class with only the entityname
      /// </summary>
      /// <param name="nodeName">the entity name</param>
      /// <param name="abstractEntity">optional: whether the entity is an abstract type (default is false)</param>
      public IfcSchemaEntityNode(string nodeName, bool abstractEntity = false)
      {
         SetNameAndType(nodeName);
         IsAbstract = abstractEntity;
      }

      /// <summary>
      /// Create the class with the information about the parent (supertype)
      /// </summary>
      /// <param name="nodeName">the entity name</param>
      /// <param name="parentNode">the supertype entity name</param>
      /// <param name="abstractEntity">optional: whether the entity is an abstract type (default is false)</param>
      public IfcSchemaEntityNode(string nodeName, IfcSchemaEntityNode parentNode, string predefTypeEnum, bool abstractEntity = false)
      {
         SetNameAndType(nodeName);
         IsAbstract = abstractEntity;
         if (parentNode != null)
            superType = parentNode;
         if (predefTypeEnum != null)
            PredefinedType = predefTypeEnum;
      }

      /// <summary>
      /// Add the subtype node into this node
      /// </summary>
      /// <param name="childNode">the subtype entity node</param>
      public void AddChildNode(IfcSchemaEntityNode childNode)
      {
         if (childNode != null)
         {
            if (subType == null)
               subType = new List<IfcSchemaEntityNode>();
            subType.Add(childNode);
         }
      }

      /// <summary>
      /// Set the supertype node into this node
      /// </summary>
      /// <param name="parentNode">the supertype entity node</param>
      public void SetParentNode(IfcSchemaEntityNode parentNode)
      {
         if (superType != null)
            throw new InvalidOperationException("parentNode cannot be null!");

         if (superType == null)
            if (parentNode != null)
               superType = parentNode;
      }

      /// <summary>
      /// get the supertype node of the this node
      /// </summary>
      /// <returns>the supertype entity node</returns>
      public IfcSchemaEntityNode GetParent()
      {
         return superType;
      }

      /// <summary>
      /// get the list of the subtypes entity nodes
      /// </summary>
      /// <returns>the list of subtype nodes</returns>
      public IList<IfcSchemaEntityNode> GetChildren()
      {
         return subType ?? [];
      }

      /// <summary>
      /// Get all the subtype branch of this entity
      /// </summary>
      /// <returns>the list of all the subtype nodes</returns>
      public IList<IfcSchemaEntityNode> GetAllDescendants()
      {
         List<IfcSchemaEntityNode> res = [];
         foreach (IfcSchemaEntityNode child in subType)
         {
            res.AddRange(child.GetAllDescendants());
         }

         return res;
      }

      /// <summary>
      /// Get all the supertype line of this entity
      /// </summary>
      /// <returns>the list of supertype following the level order</returns>
      public IList<IfcSchemaEntityNode> GetAllAncestors()
      {
         List<IfcSchemaEntityNode> res = [];

         IfcSchemaEntityNode node = this;
         while (node.superType != null)
         {
            res.Add(superType);
            node = superType;
         }

         return res;
      }

      /// <summary>
      /// Test whether the superTypeName is the valid supertype of this entity
      /// </summary>
      /// <param name="superTypeName">the name of the potential supertype</param>
      /// <returns>true: is the valid supertype</returns>
      public bool IsSubTypeOf(IFCEntityType superType, bool strict)
      {
         bool res = false;

         IfcSchemaEntityNode node = this;
         while (node.superType != null)
         {
            if (superType == node.superType.EntityType || (!strict && superType == node.EntityType))
            {
               return true;
            }

            node = node.superType;
         }

         return res;
      }

      /// <summary>
      /// Test whether the subtype name is the valid subtype of this entity
      /// </summary>
      /// <param name="subTypeName">the name of the potential subtype</param>
      /// <returns>true: is the valid subtype</returns>
      public bool IsSuperTypeOf(string subTypeName)
      {
         return CheckChildNode(subTypeName);
      }

      /// <summary>
      /// Print the branch starting from this entity node. The print is formatted using tab indentation to represent the hierarchical level
      /// </summary>
      /// <param name="level">the level number</param>
      /// <returns>the tree structure of the banch in a string</returns>
      public string PrintBranch(int level = 0)
      {
         string res = string.Empty;

         // Print itself first and then followed by each subtypes
         IfcSchemaEntityNode node = this;
         string abs = string.Empty;
         if (node.IsAbstract)
            abs = " (ABS)";

         res += "\n";
         for (int i = 0; i < level; ++i)
            res += "\t";
         res += node.Name + abs;

         if (node.subType == null)
            return res;

         foreach (IfcSchemaEntityNode sub in node.subType)
         {
            for (int i = 0; i < level; ++i)
               res += "\t";
            string br = sub.PrintBranch(level + 1);
            if (!string.IsNullOrWhiteSpace(br))
               res += br;
         }
         return res;
      }

      /// <summary>
      /// Get the entities in the branch starting of this entity node in a set
      /// </summary>
      /// <returns>a set that contains all the subtype entity names</returns>
      public HashSet<string> GetBranch()
      {
         HashSet<string> resSet = new HashSet<string>();

         IfcSchemaEntityNode node = this;
         resSet.Add(node.Name);

         if (node.subType == null)
            return resSet;

         foreach (IfcSchemaEntityNode sub in node.subType)
         {
            HashSet<string> br = sub.GetBranch();
            if (br.Count > 0)
               resSet.UnionWith(br);
         }
         return resSet;
      }

      /// <summary>
      /// Check whether an entityName is found in this entity and its subtypes
      /// </summary>
      /// <param name="entityName">the entity name to check</param>
      /// <returns>true: the entityName is found in this entity or tits subtype</returns>
      public bool CheckChildNode(string entityName)
      {
         IfcSchemaEntityNode node = this;
         if (string.Compare(node.Name, entityName, ignoreCase: true) == 0)
            return true;

         if (node.subType == null)
            return false;

         foreach (IfcSchemaEntityNode sub in node.subType)
         {
            if (sub.CheckChildNode(entityName))
               return true;
         }
         return false;
      }
   }
}