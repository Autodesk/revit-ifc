﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using Autodesk.Revit.DB;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Common.Utility
{
   /// <summary>
   /// It is a class that captures IFC entities in their respective hierarchical inheritance structure, to be captured from the IFCXML schema
   /// It uses static dictionary and set!!
   /// </summary>
   public class IfcSchemaEntityTree
   {
      /// <summary>
      /// The IFC Entity Dictionary
      /// </summary>
      public IDictionary<string, IfcSchemaEntityNode> IfcEntityDict { get; private set; } = new Dictionary<string, IfcSchemaEntityNode>(StringComparer.OrdinalIgnoreCase);
      
      /// <summary>
      /// The Predefined Type enumeration Dictionary
      /// </summary>
      public IDictionary<string, IList<string>> PredefinedTypeEnumDict { get; private set; } = new Dictionary<string, IList<string>>();

      /// <summary>
      /// The set of the entity nodes in the tree
      /// </summary>
      public HashSet<IfcSchemaEntityNode> TheTree { get; set; } = new HashSet<IfcSchemaEntityNode>();

      static string Ifc2x2Schema = "IFC2X2_ADD1";
      static string Ifc2x3Schema = "IFC2X3_TC1";
      static string Ifc4Schema = "IFC4";

      /// <summary>
      /// Reset the static Dictionary and Set. To be done before parsing another IFC schema
      /// </summary>
      public IfcSchemaEntityTree()
      {
      }

      /// <summary>
      /// Add Predefined Type and the list of enumeration values
      /// </summary>
      /// <param name="enumType">Predefined Type</param>
      /// <param name="enumList">The list of Predefined Type enumeration values</param>
      public void AddPredefinedTypeEnum(string enumType, IList<string> enumList)
      {
         if (enumType == null || enumList == null || enumList.Count == 0)
            return;

         if (PredefinedTypeEnumDict.ContainsKey(enumType))
         {
            PredefinedTypeEnumDict[enumType] = enumList;
         }
         else
         {
            PredefinedTypeEnumDict.Add(enumType, enumList);
         }
      }

      /// <summary>
      /// Add a new node into the tree
      /// </summary>
      /// <param name="entityName">the entity name</param>
      /// <param name="parentNodeName">the name of the supertype entity</param>
      public void Add(string entityName, string parentNodeName, string predefTypeEnum, bool isAbstract = false)
      {
         if (string.IsNullOrEmpty(entityName))
            return;

         // We will skip the entityname or its parent name that does not start with Ifc (except Entity)
         if (string.Compare(entityName, 0, "Ifc", 0, 3, ignoreCase: true) != 0
            || (string.Compare(parentNodeName, 0, "Ifc", 0, 3, ignoreCase: true) != 0 && string.Compare(parentNodeName, "Entity", ignoreCase: true) != 0))
            return;

         IfcSchemaEntityNode parentNode = null;
         if (!string.IsNullOrEmpty(parentNodeName))
         {
            // skip if the parent name does not start with Ifc
            if (string.Compare(parentNodeName, 0, "Ifc", 0, 3, ignoreCase: true) == 0)
            {
               if (!IfcEntityDict.TryGetValue(parentNodeName, out parentNode))
               {
                  // Parent node does not exist yet, create
                  parentNode = new IfcSchemaEntityNode(parentNodeName);

                  IfcEntityDict.Add(parentNodeName, parentNode);
                  TheTree.Add(parentNode);    // Add first into the rootNodes because the parent is null at this stage, we will remove it later is not the case
               }
            }
         }

         IfcSchemaEntityNode entityNode;
         if (!IfcEntityDict.TryGetValue(entityName, out entityNode))
         {
            if (parentNode != null)
            {
               entityNode = new IfcSchemaEntityNode(entityName, parentNode, predefTypeEnum, abstractEntity: isAbstract);
               parentNode.AddChildNode(entityNode);
            }
            else
            {
               entityNode = new IfcSchemaEntityNode(entityName, abstractEntity: isAbstract);
               // Add into the set of root nodes when parent is null/no parent
               TheTree.Add(entityNode);
            }

            IfcEntityDict.Add(entityName, entityNode);
         }
         else
         {
            // Update the node's isAbstract property and the parent node (if any)
            entityNode.isAbstract = isAbstract;
            if (parentNode != null)
            {
               entityNode.SetParentNode(parentNode);
               if (TheTree.Contains(entityNode))
                  TheTree.Remove(entityNode);
               parentNode.AddChildNode(entityNode);
            }
         }
      }

      /// <summary>
      /// Find whether an entity is already created before
      /// </summary>
      /// <param name="entityName">the entity in concern</param>
      /// <returns>the entity node in the tree</returns>
      public IfcSchemaEntityNode Find(string entityName)
      {
         IfcSchemaEntityNode res = null;
         IfcEntityDict.TryGetValue(entityName, out res);
         return res;
      }

      /// <summary>
      /// Dump the IFC entity names in a list
      /// </summary>
      /// <param name="listName">a name of the list</param>
      /// <returns>the list dump in a string</returns>
      public string DumpEntityDict(string listName)
      {
         string entityList;
         entityList = "namespace Revit.IFC.Common.Enums." + listName
                     + "\n{"
                        + "\n\t/// <summary>"
                        + "\n\t/// List of Entities for " + listName
                        + "\n\t/// </summary>"
                        + "\n\tpublic enum IFCEntityType"
                     + "\n\t{";

         foreach (KeyValuePair<string, IfcSchemaEntityNode> ent in IfcEntityDict)
         {
            entityList += "\n\t\t/// <summary>"
                           + "\n\t\t/// " + ent.Key + " enumeration"
                           + "\n\t\t/// </summary>"
                           + "\n\t\t" + ent.Key + ",\n";
         }
         entityList += "\n\t\tUnknown"
                     + "\n\t}"
                     + "\n}";

         return entityList;
      }

      /// <summary>
      /// Dump the IFC entity hierarchical tree
      /// </summary>
      /// <returns>the IFC entity tree in a string</returns>
      public string DumpTree()
      {
         string tree = string.Empty;
         foreach (IfcSchemaEntityNode rootNode in TheTree)
         {
            tree += rootNode.PrintBranch();
         }

         return tree;
      }

      #region static_functions
      static IDictionary<string, IfcSchemaEntityTree> m_IFCSchemaDict = new Dictionary<string, IfcSchemaEntityTree>();
      static IDictionary<string, IDictionary<string, IList<string>>> m_IFCEntityPredefTypeDict = new Dictionary<string, IDictionary<string, IList<string>>>();

      static public string SchemaName(IFCVersion ifcFileVersion)
      {
         string schemaFile = string.Empty;
         switch (ifcFileVersion)
         {
            case IFCVersion.IFC2x2:
               schemaFile = Ifc2x2Schema;
               break;
            case IFCVersion.IFC2x3:
            case IFCVersion.IFC2x3BFM:
            case IFCVersion.IFC2x3CV2:
            case IFCVersion.IFC2x3FM:
            case IFCVersion.IFCCOBIE:
               schemaFile = Ifc2x3Schema;
               break;
            case IFCVersion.IFC4:
            case IFCVersion.IFC4DTV:
            case IFCVersion.IFC4RV:
               schemaFile = Ifc4Schema;
               break;
            default:
               schemaFile = Ifc4Schema;
               break;
         }
         return schemaFile;
      }

      /// <summary>
      /// Get the IFC entity Dictionary for a particular IFC version
      /// </summary>
      /// <param name="ifcFileVersion">the IFC version</param>
      /// <returns>the entity Dictionary</returns>
      static public IfcSchemaEntityTree GetEntityDictFor(IFCVersion ifcFileVersion)
      {
         string schemaFile = SchemaName(ifcFileVersion);
         if (m_IFCSchemaDict.ContainsKey(schemaFile))
            return m_IFCSchemaDict[schemaFile];

         // if not found, process the file and add into the static dictionary
         IfcSchemaEntityTree entityTree = GetEntityDictFor(schemaFile);
         if (entityTree == null)
            return null;

         m_IFCSchemaDict.Add(schemaFile, entityTree);
         m_IFCEntityPredefTypeDict.Add(schemaFile, entityTree.PredefinedTypeEnumDict);
         return entityTree;
      }

      /// <summary>
      /// Get the IFC entity Dictionary for a particular IFC version from the schema file
      /// </summary>
      /// <param name="schemaFile">the schema file name</param>
      /// <returns>the entity Dictionary</returns>
      static public IfcSchemaEntityTree GetEntityDictFor(string schemaFile)
      {
         IfcSchemaEntityTree entityTree = null;

         //if (string.IsNullOrEmpty(loadedIfcSchemaVersion) || !loadedIfcSchemaVersion.Equals(schemaFile, StringComparison.InvariantCultureIgnoreCase))
         {
            // Process IFCXml schema here, then search for IfcProduct and build TreeView beginning from that node. Allow checks for the tree nodes. Grey out (and Italic) the abstract entity
            string schemaFilePath;
            FileInfo schemaFileInfo;

            string schemaLoc = Path.GetDirectoryName(System.Reflection.Assembly.GetCallingAssembly().Location);
            schemaFilePath = Path.Combine(schemaLoc, schemaFile + ".xsd");
            schemaFileInfo = new FileInfo(schemaFilePath);
            if (!schemaFileInfo.Exists)
            {
               schemaFilePath = Path.Combine(DirectoryUtil.IFCSchemaLocation, schemaFile + ".xsd");
               schemaFileInfo = new FileInfo(schemaFilePath);
            }

            if (schemaFileInfo.Exists)
            {
               entityTree = new IfcSchemaEntityTree();
               bool success = ProcessIFCXMLSchema.ProcessIFCSchema(schemaFileInfo, ref entityTree);
            }
         }

         return entityTree;
      }

      static void ProcessSchemaFile(string dirLocation, ref HashSet<string> schemaProcessed)
      {
         DirectoryInfo dirInfo = new DirectoryInfo(dirLocation);
         if (dirInfo == null)
            return;

         foreach (FileInfo fileInfo in dirInfo.GetFiles("*.xsd"))
         {
            string schemaId = Path.GetFileNameWithoutExtension(fileInfo.Name);
            if (!schemaProcessed.Contains(fileInfo.Name) && !m_IFCSchemaDict.ContainsKey(schemaId))
            {
               IfcSchemaEntityTree entityTree = new IfcSchemaEntityTree();
               bool success = ProcessIFCXMLSchema.ProcessIFCSchema(fileInfo, ref entityTree);
               if (success)
               {
                  schemaProcessed.Add(fileInfo.Name);
                  m_IFCSchemaDict.Add(schemaId, entityTree);
                  m_IFCEntityPredefTypeDict.Add(schemaId, entityTree.PredefinedTypeEnumDict);
               }
            }
         }
      }

      static bool m_AllIFCSchemaProcessed = false;

      /// <summary>
      /// Get All IFC schema inside the designated folder. They will be cached
      /// </summary>
      static public void GetAllEntityDict()
      {
         if (m_AllIFCSchemaProcessed)
            return;

         HashSet<string> schemaProcessed = new HashSet<string>();

#if IFC_OPENSOURCE
         // For the open source code, search it from the IfcExporter install folder
         string schemaLoc = Path.GetDirectoryName(System.Reflection.Assembly.GetCallingAssembly().Location);
         ProcessSchemaFile(schemaLoc, ref schemaProcessed);
#endif
         {
            ProcessSchemaFile(DirectoryUtil.IFCSchemaLocation, ref schemaProcessed);
         }

         if (schemaProcessed.Count == m_IFCSchemaDict.Count)
            m_AllIFCSchemaProcessed = true;
      }

      static public IList<string> GetAllCachedSchemaTrees()
      {
         return m_IFCSchemaDict.Select(x => x.Key).ToList();
      }

      /// <summary>
      /// Find a Non ABS supertype entity from the input type name
      /// </summary>
      /// <param name="typeName">the type name</param>
      /// <returns>the non-abs supertype instance node</returns>
      static public IfcSchemaEntityNode FindNonAbsInstanceSuperType(IFCVersion context, string typeName)
      {
         IfcSchemaEntityTree ifcEntitySchemaTree = GetEntityDictFor(context);
         IfcSchemaEntityNode res = null;

         // Note: Implementer's agreement #CV-2x3-166 changes IfcSpaceHeaterType from IfcEnergyConversionDevice to IfcFlowTerminal.
         if ((context == IFCVersion.IFC2x3 || context == IFCVersion.IFC2x3BFM
               || context == IFCVersion.IFC2x3CV2 || context == IFCVersion.IFC2x3FM || context == IFCVersion.IFCCOBIE)
             && typeName.Equals("IfcSpaceHeaterType", StringComparison.InvariantCultureIgnoreCase))
         {
            res = ifcEntitySchemaTree.Find("IfcFlowTerminal");
            if (res.isAbstract)
               return null;
            return res;
         }

         string theTypeName = typeName.Substring(typeName.Length - 4, 4).Equals("Type", StringComparison.CurrentCultureIgnoreCase) ? typeName : typeName + "Type";
         IfcSchemaEntityNode entNode = ifcEntitySchemaTree.Find(theTypeName);
         if (entNode != null)
         {
            while (true)
            {
               res = entNode.GetParent();
               // no more parent node to get
               if (res == null)
                  break;

               entNode = ifcEntitySchemaTree.Find(res.Name.Substring(0, res.Name.Length - 4));
               if (entNode != null && !entNode.isAbstract)
               {
                  res = entNode;
                  break;
               }
               else
                  entNode = res;    // put back the Type Node
            }
         }

         return res;
      }

      /// <summary>
      /// Find a Non-Abstract Super Type in the current IFC Schema
      /// </summary>
      /// <param name="typeName">the entity name</param>
      /// <param name="stopNode">optional list of entity name(s) to stop the search</param>
      /// <returns>the appropriate node or null</returns>
      static public IfcSchemaEntityNode FindNonAbsSuperType(IFCVersion context, string entityName, params string[] stopNode)
      {
         IfcSchemaEntityTree ifcEntitySchemaTree = GetEntityDictFor(context);
         IfcSchemaEntityNode res = null;

         IfcSchemaEntityNode entNode = ifcEntitySchemaTree.Find(entityName);

         if (entNode != null)
         {
            foreach (string stopCond in stopNode)
               if (entNode.Name.Equals(stopCond, StringComparison.InvariantCultureIgnoreCase))
                  return res;

            while (true)
            {
               entNode = entNode.GetParent();
               // no more parent node to get
               if (entNode == null)
                  break;

               foreach (string stopCond in stopNode)
                  if (entNode.Name.Equals(stopCond, StringComparison.InvariantCultureIgnoreCase))
                     break;

               if (entNode != null && !entNode.isAbstract)
               {
                  res = entNode;
                  break;
               }
            }
         }
         return res;
      }

      /// <summary>
      /// Collect all the supertype of an entity node
      /// </summary>
      /// <param name="entityName">the entity</param>
      /// <param name="stopNode">array of the stop node(s)</param>
      /// <returns>List of the supertypes</returns>
      static public IList<IfcSchemaEntityNode> FindAllSuperTypes(IFCVersion context, string entityName, params string[] stopNode)
      {
         IfcSchemaEntityTree ifcEntitySchemaTree = GetEntityDictFor(context);
         IList<IfcSchemaEntityNode> res = new List<IfcSchemaEntityNode>();

         IfcSchemaEntityNode entNode = ifcEntitySchemaTree.Find(entityName);

         if (entNode != null)
         {
            // return the list when it reaches the stop node
            foreach (string stopCond in stopNode)
               if (entNode.Name.Equals(stopCond, StringComparison.InvariantCultureIgnoreCase))
                  return res;

            bool continueSearch = true;
            while (continueSearch)
            {
               entNode = entNode.GetParent();
               // no more parent node to get
               if (entNode == null)
                  break;

               // Stop the search when it reaches the stop node
               foreach (string stopCond in stopNode)
               {
                  if (entNode.Name.Equals(stopCond, StringComparison.InvariantCultureIgnoreCase))
                  {
                     continueSearch = false;
                     break;
                  }
               }
               if (entNode != null)
               {
                  res.Add(entNode);
               }
            }
         }
         return res;
      }

      /// <summary>
      /// Check whether an entity is a subtype of another entity
      /// </summary>
      /// <param name="subTypeName">candidate of the subtype entity</param>
      /// <param name="superTypeName">candidate of the supertype entity</param>
      /// <returns>true: if the the subTypeName is the subtype of supertTypeName</returns>
      static public bool IsSubTypeOf(IFCVersion ifcVersion, IFCEntityType subType, IFCEntityType superType, bool strict = true)
      {
         return IsSubTypeOf(ifcVersion, subType.ToString(), superType.ToString(), strict);
      }

      /// <summary>
      /// Check whether an entity (string) is a subtype of another entity
      /// </summary>
      /// <param name="context">the IFC version in context for the check</param>
      /// <param name="subTypeName">the subtype name</param>
      /// <param name="superTypeName">the supertype name</param>
      /// <param name="strict">whether the subtype is strictly subtype. Set to false if it "supertype == subtype" is acceptable</param>
      /// <returns>true if it is subtype</returns>
      static public bool IsSubTypeOf(IFCVersion context, string subTypeName, string superTypeName, bool strict = true)
      {
         IfcSchemaEntityTree ifcEntitySchemaTree = GetEntityDictFor(context);
         //var ifcEntitySchemaTree = IfcSchemaEntityTree.GetEntityDictFor(context);
         if (ifcEntitySchemaTree == null || ifcEntitySchemaTree.IfcEntityDict == null || ifcEntitySchemaTree.IfcEntityDict.Count == 0)
            throw new Exception("Unable to locate IFC Schema xsd file! Make sure the relevant xsd " + context + " exists.");

         IfcSchemaEntityNode theNode = ifcEntitySchemaTree.Find(subTypeName);
         if (theNode != null)
         {
            if (strict)
               return theNode.IsSubTypeOf(superTypeName);
            else
               return theNode.Name.Equals(superTypeName, StringComparison.InvariantCultureIgnoreCase) || theNode.IsSubTypeOf(superTypeName);
         }

         return false;
      }

      /// <summary>
      /// Check whether an entity (string) is a supertype of another entity
      /// </summary>
      /// <param name="context">the IFC version in context for the check</param>
      /// <param name="superTypeName">the supertype name</param>
      /// <param name="subTypeName">the subtype name</param>
      /// <param name="strict">whether the supertype is strictly supertype. Set to false if it "supertype == subtype" is acceptable</param>
      /// <returns>true if it is supertype</returns>
      static public bool IsSuperTypeOf(IFCVersion context, string superTypeName, string subTypeName, bool strict = true)
      {
         IfcSchemaEntityTree ifcEntitySchemaTree = GetEntityDictFor(context);
         //var ifcEntitySchemaTree = IfcSchemaEntityTree.GetEntityDictFor(context);
         if (ifcEntitySchemaTree == null || ifcEntitySchemaTree.IfcEntityDict == null || ifcEntitySchemaTree.IfcEntityDict.Count == 0)
            throw new Exception("Unable to locate IFC Schema xsd file! Make sure the relevant xsd " + context + " exists.");

         IfcSchemaEntityNode theNode = ifcEntitySchemaTree.Find(superTypeName);
         if (theNode != null)
         {
            if (strict)
               return theNode.IsSuperTypeOf(subTypeName);
            else
               return theNode.Name.Equals(subTypeName, StringComparison.InvariantCultureIgnoreCase) || theNode.IsSuperTypeOf(subTypeName);
         }

         return false;
      }

      /// <summary>
      /// Get the PredefinedType list from the processed schema
      /// </summary>
      /// <param name="context">The IFC version</param>
      /// <param name="ifcEntity">the specific Entity to get the PredefinedType list from</param>
      /// <returns>List of PredefinedType strings</returns>
      static public IList<string> GetPredefinedTypeList(IFCVersion context, string ifcEntity)
      {
         IList<string> predefinedtypeList = new List<string>();
         IfcSchemaEntityTree ifcEntitySchemaTree = GetEntityDictFor(context);
         if (ifcEntitySchemaTree == null || ifcEntitySchemaTree.IfcEntityDict == null || ifcEntitySchemaTree.IfcEntityDict.Count == 0)
            throw new Exception("Unable to locate IFC Schema xsd file! Make sure the relevant xsd " + context + " exists.");

         if (string.IsNullOrEmpty(ifcEntity))
            return null;

         // Check for both <name>Enum, and <name>TypeEnum
         string entEnum;
         string entTypeEnum;
         if (ifcEntity.EndsWith("Type", StringComparison.InvariantCultureIgnoreCase))
         {
            entTypeEnum = ifcEntity + "Enum";
            entEnum = ifcEntity.Remove(ifcEntity.Length - 4) + "Enum";
         }
         else
         {
            entEnum = ifcEntity + "Enum";
            entTypeEnum = ifcEntity + "TypeEnum";
         }
         if (ifcEntitySchemaTree.PredefinedTypeEnumDict.ContainsKey(entEnum))
            return ifcEntitySchemaTree.PredefinedTypeEnumDict[entEnum];
         if (ifcEntitySchemaTree.PredefinedTypeEnumDict.ContainsKey(entTypeEnum))
            return ifcEntitySchemaTree.PredefinedTypeEnumDict[entTypeEnum];

         return null;
      }
      #endregion
   }
}
