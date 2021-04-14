using System;
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
      static private IDictionary<string, IfcSchemaEntityNode> m_IfcEntityDict = null;
      static private IDictionary<string, IList<string>> m_PredefTypeEnum = null;

      static HashSet<IfcSchemaEntityNode> rootNodes = new HashSet<IfcSchemaEntityNode>();

      static string loadedIfcSchemaVersion = "";

      /// <summary>
      /// The Dictionary to the IFC schema entities
      /// </summary>
      static public IDictionary<string, IfcSchemaEntityNode> IfcEntityDict
      {
         get
         {
            if (m_IfcEntityDict == null)
            {
               var comparer = StringComparer.OrdinalIgnoreCase;
               m_IfcEntityDict = new SortedDictionary<string, IfcSchemaEntityNode>(comparer);
            }
            return m_IfcEntityDict;
         }
      }

      static public IDictionary<string, IList<string>> PredefinedTypeEnumDict
      {
         get
         {
            if (m_PredefTypeEnum == null)
               m_PredefTypeEnum = new Dictionary<string, IList<string>>();
            return m_PredefTypeEnum;
         }
      }

      static public void AddPredefinedTypeEnum(string enumType, IList<string> enumList)
      {
         if (enumType == null || enumList == null || enumList.Count == 0)
            return;

         if (m_PredefTypeEnum.ContainsKey(enumType))
         {
            m_PredefTypeEnum[enumType] = enumList;
         }
         else
         {
            m_PredefTypeEnum.Add(enumType, enumList);
         }
      }

      /// <summary>
      /// Reset the static Dictionary and Set. To be done before parsing another IFC schema
      /// </summary>
      public static void Initialize(string schemaFile)
      {
         // If the same schema is already loaded and has content, do nothing
         if (loadedIfcSchemaVersion.Equals(schemaFile, StringComparison.InvariantCultureIgnoreCase) && EntityDict.Count > 0)
            return;

         // It is a new schema or the first time
         IfcEntityDict.Clear();
         PredefinedTypeEnumDict.Clear();
         rootNodes.Clear();
         loadedIfcSchemaVersion = "";
      }

      /// <summary>
      /// Property: the Entity Dictionary, which lists all the entities 
      /// </summary>
      public static IDictionary<string, IfcSchemaEntityNode> EntityDict
      {
         get { return IfcEntityDict; }
      }

      /// <summary>
      /// Property: the set containing the root nodes of the IFC entity tree
      /// </summary>
      public static HashSet<IfcSchemaEntityNode> TheTree
      {
         get { return rootNodes; }
      }

      private static string SchemaFileName(IFCVersion ifcFileVersion)
      {
         string schemaFile = string.Empty;
         switch (ifcFileVersion)
         {
            case IFCVersion.IFC2x2:
               schemaFile = "IFC2X2_ADD1.xsd";
               break;
            case IFCVersion.IFC2x3:
            case IFCVersion.IFC2x3BFM:
            case IFCVersion.IFC2x3CV2:
            case IFCVersion.IFC2x3FM:
            case IFCVersion.IFCCOBIE:
               schemaFile = "IFC2X3_TC1.xsd";
               break;
            case IFCVersion.IFC4:
            case IFCVersion.IFC4DTV:
            case IFCVersion.IFC4RV:
               schemaFile = "IFC4.xsd";
               break;
            default:
               schemaFile = "IFC4.xsd";
               break;
         }
         return schemaFile;
      }

      /// <summary>
      /// Get the IFC entity Dictionary for a particular IFC version
      /// </summary>
      /// <param name="ifcFileVersion">the IFC version</param>
      /// <returns>the entity Dictionary</returns>
      public static IDictionary<string, IfcSchemaEntityNode> GetEntityDictFor(IFCVersion ifcFileVersion)
      {
         string schemaFile = SchemaFileName(ifcFileVersion);
         return GetEntityDictFor(schemaFile);
      }

      /// <summary>
      /// Get the IFC entity Dictionary for a particular IFC version from the schema file
      /// </summary>
      /// <param name="schemaFile">the schema file name</param>
      /// <returns>the entity Dictionary</returns>
      public static IDictionary<string, IfcSchemaEntityNode> GetEntityDictFor(string schemaFile)
      {
         if (string.IsNullOrEmpty(loadedIfcSchemaVersion) || !loadedIfcSchemaVersion.Equals(schemaFile, StringComparison.InvariantCultureIgnoreCase))
         {
            // Process IFCXml schema here, then search for IfcProduct and build TreeView beginning from that node. Allow checks for the tree nodes. Grey out (and Italic) the abstract entity
            string schemaFilePath;
            FileInfo schemaFileInfo;

#if IFC_OPENSOURCE
            // For the open source code, search it from the IfcExporter install folder
            string schemaLoc = Path.GetDirectoryName(System.Reflection.Assembly.GetCallingAssembly().Location);
            schemaFilePath = Path.Combine(schemaLoc, schemaFile);
            schemaFileInfo = new FileInfo(schemaFilePath);
            if (!schemaFileInfo.Exists)
#endif
            {
               schemaFilePath = Path.Combine(DirectoryUtil.IFCSchemaLocation, schemaFile);
               schemaFileInfo = new FileInfo(schemaFilePath);
            }

            if (schemaFileInfo.Exists)
            {
               bool newLoad = ProcessIFCXMLSchema.ProcessIFCSchema(schemaFileInfo);
               if (newLoad)
                  loadedIfcSchemaVersion = Path.GetFileName(schemaFile);
            }
         }

         return EntityDict;
      }

      /// <summary>
      /// Add a new node into the tree
      /// </summary>
      /// <param name="entityName">the entity name</param>
      /// <param name="parentNodeName">the name of the supertype entity</param>
      static public void Add(string entityName, string parentNodeName, string predefTypeEnum, bool isAbstract = false)
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
                  rootNodes.Add(parentNode);    // Add first into the rootNodes because the parent is null at this stage, we will remove it later is not the case
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
               rootNodes.Add(entityNode);
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
               if (rootNodes.Contains(entityNode))
                  rootNodes.Remove(entityNode);
               parentNode.AddChildNode(entityNode);
            }
         }
      }

      /// <summary>
      /// Find whether an entity is already created before
      /// </summary>
      /// <param name="entityName">the entity in concern</param>
      /// <returns>the entity node in the tree</returns>
      static public IfcSchemaEntityNode Find(string entityName)
      {
         IfcSchemaEntityNode res = null;
         IfcEntityDict.TryGetValue(entityName, out res);
         return res;
      }

      /// <summary>
      /// Find a Non ABS supertype entity from the input type name
      /// </summary>
      /// <param name="typeName">the type name</param>
      /// <returns>the non-abs supertype instance node</returns>
      static public IfcSchemaEntityNode FindNonAbsInstanceSuperType(string typeName)
      {
         IfcSchemaEntityNode res = null;

         // Note: Implementer's agreement #CV-2x3-166 changes IfcSpaceHeaterType from IfcEnergyConversionDevice to IfcFlowTerminal.
         if (loadedIfcSchemaVersion.Equals("IFC2X3_TC1.xsd", StringComparison.InvariantCultureIgnoreCase) && typeName.Equals("IfcSpaceHeaterType", StringComparison.InvariantCultureIgnoreCase))
         {
            res = Find("IfcFlowTerminal");
            if (res.isAbstract)
               return null;
            return res;
         }

         string theTypeName = typeName.Substring(typeName.Length - 4, 4).Equals("Type", StringComparison.CurrentCultureIgnoreCase) ? typeName : typeName + "Type";
         IfcSchemaEntityNode entNode = Find(theTypeName);
         if (entNode != null)
         {
            while (true)
            {
               res = entNode.GetParent();
               // no more parent node to get
               if (res == null)
                  break;

               entNode = Find(res.Name.Substring(0, res.Name.Length - 4));
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
      static public IfcSchemaEntityNode FindNonAbsSuperType(string entityName, params string[] stopNode)
      {
         IfcSchemaEntityNode res = null;
         
         IfcSchemaEntityNode entNode = Find(entityName);

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
      static public IList<IfcSchemaEntityNode> FindAllSuperTypes(string entityName, params string[] stopNode)
      {
         IList<IfcSchemaEntityNode> res = new List<IfcSchemaEntityNode>();

         IfcSchemaEntityNode entNode = Find(entityName);

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
      static public bool IsSubTypeOf(IFCEntityType subType, IFCEntityType superType, bool strict = true)
      {
         return IsSubTypeOf(subType.ToString(), superType.ToString(), strict);
      }
      
      /// <summary>
      /// Check whether an entity is a subtype of another entity
      /// </summary>
      /// <param name="subTypeName">candidate of the subtype entity name</param>
      /// <param name="superTypeName">candidate of the supertype entity name</param>
      /// <returns>true: if the the subTypeName is the subtype of supertTypeName</returns>
      static public bool IsSubTypeOf(string subTypeName, string superTypeName, bool strict = true)
      {
         IfcSchemaEntityNode theNode = Find(subTypeName);
         if (theNode != null)
         {
            if (strict)
               return (theNode.IsSubTypeOf(superTypeName));
            else
               return (theNode.Name.Equals(superTypeName, StringComparison.InvariantCultureIgnoreCase) || theNode.IsSubTypeOf(superTypeName));
         }

         return false;
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
         var ifcEntitySchemaTree = IfcSchemaEntityTree.GetEntityDictFor(context);
         if (ifcEntitySchemaTree == null || ifcEntitySchemaTree.Count == 0)
            throw new Exception("Unable to locate IFC Schema xsd file! Make sure the relevant xsd " + context + " exists.");

         IfcSchemaEntityNode theNode = Find(subTypeName);
         if (theNode != null)
         {
            if (strict)
               return (theNode.IsSubTypeOf(superTypeName));
            else
               return (theNode.Name.Equals(superTypeName, StringComparison.InvariantCultureIgnoreCase) || theNode.IsSubTypeOf(superTypeName));
         }

         return false;
      }
      
      /// <summary>
      /// Check whether an entity is a supertype of another entity
      /// </summary>
      /// <param name="superTypeName">candidate of the supertype entity name</param>
      /// <param name="subTypeName">candidate of the subtype entity name</param>
      /// <returns>true: if the the superTypeName is the supertype of subtTypeName</returns>
      static public bool IsSuperTypeOf(string superTypeName, string subTypeName, bool strict = true)
      {
         IfcSchemaEntityNode theNode = Find(superTypeName);
         if (theNode != null)
         {
            if (strict)
               return (theNode.IsSuperTypeOf(subTypeName));
            else
               return (theNode.Name.Equals(subTypeName, StringComparison.InvariantCultureIgnoreCase) || theNode.IsSuperTypeOf(subTypeName));
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
         var ifcEntitySchemaTree = IfcSchemaEntityTree.GetEntityDictFor(context);
         if (ifcEntitySchemaTree == null || ifcEntitySchemaTree.Count == 0)
            throw new Exception("Unable to locate IFC Schema xsd file! Make sure the relevant xsd " + context + " exists.");

         IfcSchemaEntityNode theNode = Find(superTypeName);
         if (theNode != null)
         {
            if (strict)
               return (theNode.IsSuperTypeOf(subTypeName));
            else
               return (theNode.Name.Equals(subTypeName, StringComparison.InvariantCultureIgnoreCase) || theNode.IsSuperTypeOf(subTypeName));
         }

         return false;
      }

      /// <summary>
      /// Dump the IFC entity names in a list
      /// </summary>
      /// <param name="listName">a name of the list</param>
      /// <returns>the list dump in a string</returns>
      static public string DumpEntityDict(string listName)
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
      static public string DumpTree()
      {
         string tree = string.Empty;
         foreach (IfcSchemaEntityNode rootNode in rootNodes)
         {
            tree += rootNode.PrintBranch();
         }

         return tree;
      }
   }
}
