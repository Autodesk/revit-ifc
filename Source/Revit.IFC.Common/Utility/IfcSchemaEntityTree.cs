using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Autodesk.Revit.DB;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Common.Utility
{
   /// <summary>
   /// An enumeration of the supported IFC schema file versions.
   /// </summary>
   public enum IFCSchemaFileVersion
   {
      IFC2X2,
      IFC2X3,
      IFC4,
      IFC4RV,
      IFC4X3
   }

   /// <summary>
   /// It is a class that captures IFC entities in their respective hierarchical inheritance structure, to be captured from the IFCXML schema
   /// It uses static dictionary and set!!
   /// </summary>
   public class IfcSchemaEntityTree
   {
      /// <summary>
      /// The schema file version corresponding to this tree.
      /// </summary>
      public IFCSchemaFileVersion SchemaFileVersion { get; private set; }

      /// <summary>
      /// The IFC Entity Dictionary
      /// </summary>
      public Dictionary<string, IfcSchemaEntityNode> IfcEntityDict { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

      /// <summary>
      /// The Predefined Type enumeration Dictionary
      /// </summary>
      public Dictionary<string, IList<string>> PredefinedTypeEnumDict { get; private set; } = new();

      /// <summary>
      /// The set of the entity nodes in the tree
      /// </summary>
      public HashSet<IfcSchemaEntityNode> TheTree { get; set; } = new HashSet<IfcSchemaEntityNode>();

      public static readonly string[] SupportedSchemaFileNames = [ "IFC2X2_ADD1", "IFC2X3_TC1", "IFC4", "IFC4RV", "IFC4X3" ];

      /// <summary>
      /// Reset the static Dictionary and Set. To be done before parsing another IFC schema
      /// </summary>
      public IfcSchemaEntityTree(IFCSchemaFileVersion schemaFileVersion)
      {
         SchemaFileVersion = schemaFileVersion;
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
            entityNode.IsAbstract = isAbstract;
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

      static readonly Dictionary<IFCSchemaFileVersion, HashSet<string>> DeprecatedDict = new()
      {
         { IFCSchemaFileVersion.IFC4X3, new HashSet<string>() { "IfcProxy", "IfcOpeningStandardCase", "IfcBeamStandardCase", "IfcColumnStandardCase", "IfcDoorStandardCase",
            "IfcMemberStandardCase", "IfcPlateStandardCase", "IfcSlabElementedCase", "IfcSlabStandardCase", "IfcWallElementedCase",
            "IfcWallStandardCase", "IfcWindowStandardCase", "IfcDoorStyle", "IfcWindowStyle" } },
         { IFCSchemaFileVersion.IFC4, new HashSet<string>() { "IfcProxy", "IfcOpeningStandardCase", "IfcBeamStandardCase", "IfcColumnStandardCase", "IfcDoorStandardCase",
            "IfcMemberStandardCase", "IfcPlateStandardCase", "IfcSlabElementedCase", "IfcSlabStandardCase", "IfcWallElementedCase",
            "IfcWallStandardCase", "IfcWindowStandardCase", "IfcDoorStyle", "IfcWindowStyle" } },
         { IFCSchemaFileVersion.IFC2X3, new HashSet<string>(){ "IfcElectricalElement", "IfcEquipmentElement" } },
      };

      static readonly Dictionary<IFCSchemaFileVersion, HashSet<string>> UnsupportedDict = new()
      {
         { IFCSchemaFileVersion.IFC4X3, new HashSet<string>() { "IfcBuilding", "IfcBuildingStorey" } },
         { IFCSchemaFileVersion.IFC4, new HashSet<string>() { "IfcBuilding", "IfcBuildingStorey" } },
         { IFCSchemaFileVersion.IFC2X3, new HashSet<string>() { "IfcBuilding", "IfcBuildingStorey" } },
         { IFCSchemaFileVersion.IFC2X2, new HashSet<string>() { "IfcBuilding", "IfcBuildingStorey" } }
      };

      static readonly Dictionary<IFCSchemaFileVersion, Dictionary<string, HashSet<string>>> DeprecatedPredefinedType = new()
      {
         {
            IFCSchemaFileVersion.IFC4X3,
            new Dictionary<string, HashSet<string>>()
            {
               { "IfcBuildingElementProxy", new HashSet<string>()
                  { "COMPLEX", "ELEMENT", "PARTIAL" }
               },
               { "IfcBuildingElementProxyType", new HashSet<string>()
                  { "COMPLEX", "ELEMENT", "PARTIAL" }
               },
               { "IfcCableCarrierFitting", new HashSet<string>()
                  { "CROSS", "REDUCER", "TEE" }
               },
               { "IfcCableCarrierFittingType", new HashSet<string>()
                  { "CROSS", "REDUCER", "TEE" }
               },
               { "IfcFireSuppressionTerminal", new HashSet<string>()
                  { "SPRINKLERDEFLECTOR" }
               },
               { "IfcFireSuppressionTerminalType", new HashSet<string>()
                  { "SPRINKLERDEFLECTOR" }
               },
               { "IfcFooting", new HashSet<string>()
                  { "CAISSON_FOUNDATION" }
               },
               { "IfcFootingType", new HashSet<string>()
                  { "CAISSON_FOUNDATION" }
               },
               { "IfcGeographicElement", new HashSet<string>()
                  { "SOIL_BORING_POINT" }
               },
               { "IfcGeographicElementType", new HashSet<string>()
                  { "SOIL_BORING_POINT" }
               },
               { "IfcWall", new HashSet<string>()
                  { "POLYGONAL", "STANDARD" }
               },
               { "IfcWallType", new HashSet<string>()
                  { "POLYGONAL", "STANDARD" }
               },
            }
         }
      };

      static IfcSchemaEntityTree[] IFCSchemaDict { get; set; } = new IfcSchemaEntityTree[Enum.GetNames<IFCSchemaFileVersion>().Length];

      static public IFCSchemaFileVersion GetSchemaVersion(IFCVersion ifcFileVersion)
      {
         switch (ifcFileVersion)
         {
            case IFCVersion.IFC2x2:
               return IFCSchemaFileVersion.IFC2X2;
            case IFCVersion.IFC2x3:
            case IFCVersion.IFC2x3BFM:
            case IFCVersion.IFC2x3CV2:
            case IFCVersion.IFC2x3FM:
            case IFCVersion.IFCCOBIE:
               return IFCSchemaFileVersion.IFC2X3;
            case IFCVersion.IFC4:
            case IFCVersion.IFC4DTV:
               return IFCSchemaFileVersion.IFC4;
            case IFCVersion.IFC4RV:
            case IFCVersion.IFCSG:
               return IFCSchemaFileVersion.IFC4RV;
            case IFCVersion.IFC4x3:
            case IFCVersion.IFC4x3RV:
            case IFCVersion.IFC4x3DTV:
               return IFCSchemaFileVersion.IFC4X3;
         }

         throw new ArgumentException("Unsupported IFC version: " + ifcFileVersion.ToString());
      }

      static public bool TryGetSchemaVersion(string schemaId, out IFCSchemaFileVersion version)
      {
         int index = Array.FindIndex(SupportedSchemaFileNames, x => string.Compare(x, schemaId, true) == 0);
         if (index >= 0)
         {
            version = (IFCSchemaFileVersion)index;
            return true;
         }

         version = IFCSchemaFileVersion.IFC4;  // default to IFC4 if not found, but it should not be used
         return false;
      }

      /// <summary>
      /// Get the IFC entity Dictionary for a particular IFC version
      /// </summary>
      /// <param name="schemaFileVersion">the IFC schema file version</param>
      /// <param name="schemaLoc">the location of the schema file</param>
      /// <returns>the entity Dictionary</returns>
      static public IfcSchemaEntityTree GetEntityDictFor(IFCSchemaFileVersion schemaFileVersion, string schemaLoc)
      {
         if (IFCSchemaDict[(int)schemaFileVersion] == null)
         {
            string schemaFile = SupportedSchemaFileNames[(int)schemaFileVersion];
            IfcSchemaEntityTree entityTree = PopulateEntityDictFor(schemaFileVersion, schemaFile, schemaLoc);
            if (entityTree == null)
               return null;

            IFCSchemaDict[(int)schemaFileVersion] = entityTree;
         }

         return IFCSchemaDict[(int)schemaFileVersion];
      }

      /// <summary>
      /// Get the IFC entity Dictionary for a particular IFC version
      /// </summary>
      /// <param name="ifcFileVersion">the IFC version</param>
      /// <returns>the entity Dictionary</returns>
      static public IfcSchemaEntityTree GetEntityDictFor(IFCVersion ifcFileVersion, string schemaLoc)
      { 
         return GetEntityDictFor(GetSchemaVersion(ifcFileVersion), schemaLoc);
      }

      /// <summary>
      /// Get the IFC entity Dictionary for a particular IFC version from the schema file
      /// </summary>
      /// <param name="schemaFile">the schema file name</param>
      /// <returns>the entity Dictionary</returns>
      static IfcSchemaEntityTree PopulateEntityDictFor(IFCSchemaFileVersion schemaFileVersion, string schemaFile, string schemaLoc = null)
      {
         IfcSchemaEntityTree entityTree = null;

         // Process IFCXml schema here, then search for IfcProduct and build TreeView beginning from that node. Allow checks for the tree nodes. Grey out (and Italic) the abstract entity
         string schemaFilePath;
         FileInfo schemaFileInfo;

         if (string.IsNullOrEmpty(schemaLoc))
            schemaLoc = Path.GetDirectoryName(System.Reflection.Assembly.GetCallingAssembly().Location);
         schemaFilePath = Path.Combine(schemaLoc, schemaFile + ".xsd");
         schemaFileInfo = new FileInfo(schemaFilePath);
         if (!schemaFileInfo.Exists)
         {
            schemaFilePath = Path.Combine(DirectoryUtil.IFCSchemaLocation, schemaFile + ".xsd");
            schemaFileInfo = new FileInfo(schemaFilePath);
         }

         if (schemaFileInfo.Exists)
         {
            entityTree = new IfcSchemaEntityTree(schemaFileVersion);
            bool success = ProcessIFCXMLSchema.ProcessIFCSchema(schemaFileInfo, ref entityTree);
         }

         return entityTree;
      }

      /// <summary>
      /// Generate the IFC entiry Trie data
      /// </summary>
      /// <param name="entityTrie">the IFCEntityTrie</param>
      public static void GenerateEntityTrie(ref IFCEntityTrie entityTrie)
      {
         foreach (KeyValuePair<short, string> entEntry in entityTrie.FilteredIFCEntityDict)
         {
            entityTrie.AddEntry(entEntry.Value);
         }
      }

      static void ProcessSchemaFiles(string dirLocation)
      {
         DirectoryInfo dirInfo = new DirectoryInfo(dirLocation);
         if (dirInfo == null)
            return;

         foreach (FileInfo fileInfo in dirInfo.GetFiles("*.xsd"))
         {
            string schemaId = Path.GetFileNameWithoutExtension(fileInfo.Name).ToUpper();
            if (!TryGetSchemaVersion(schemaId, out IFCSchemaFileVersion version))
               continue;

            if (IFCSchemaDict[(int)version] != null)
               continue;

            IfcSchemaEntityTree entityTree = new IfcSchemaEntityTree(version);
            bool success = ProcessIFCXMLSchema.ProcessIFCSchema(fileInfo, ref entityTree);
            if (success)
            {
               IFCSchemaDict[(int)version] = entityTree;
            }
         }
      }

      static bool AllIFCSchemaProcessed { get; set; } = false;

      /// <summary>
      /// Get All IFC schema inside the designated folder. They will be cached.
      /// </summary>
      static public void GetAllEntityDict()
      {
         if (AllIFCSchemaProcessed)
            return;

#if IFC_OPENSOURCE
         // For the open source code, search it from the IfcExporter install folder
         string schemaLoc = Path.GetDirectoryName(System.Reflection.Assembly.GetCallingAssembly().Location);
         ProcessSchemaFiles(schemaLoc);
#endif
         {
            ProcessSchemaFiles(DirectoryUtil.IFCSchemaLocation);
         }

         foreach (IfcSchemaEntityTree node in IFCSchemaDict)
         {
            if (node == null)
            {
               AllIFCSchemaProcessed = false;
               return;
            }
         }
         
         AllIFCSchemaProcessed = true;
      }

      /// <summary>
      /// Generate the Entity type name corresponding to an instance.
      /// </summary>
      /// <param name="instanceName">The instance name.</param>
      /// <returns>The type name.</returns>
      /// <remarks>
      /// This is done in a heuristic fashion, so we will need to 
      /// make sure exceptions are dealt with.
      /// </remarks>
      public static string GetTypeNameFromInstanceName(string instanceName, bool exportAsOlderThanIFC4)
      {
         // Deal with exceptions.
         if (string.Compare(instanceName, "IfcProduct", true) == 0)
            return "IfcTypeProduct";
         else if (string.Compare(instanceName, "IfcObject", true) == 0)
            return "IfcTypeObject";
         // IFCDoorType and IFCWindowType are available since IFC4.
         else if (string.Compare(instanceName, "IfcWindow", true) == 0 && exportAsOlderThanIFC4)
            return "IfcWindowStyle";
         else if (string.Compare(instanceName, "IFCDoor", true) == 0 && exportAsOlderThanIFC4)
            return "IFCDoorStyle";
         // IfcReinforcingBarType, IfcReinforcingMeshType, IfcTendonAnchorType, and IfcTendonType
         // are type entities introduced only in IFC 4. For older schemas, we use the basic type entity instead.
         else if (exportAsOlderThanIFC4 &&
            ((string.Compare(instanceName, "IfcReinforcingBar", true) == 0) ||
            (string.Compare(instanceName, "IfcReinforcingMesh", true) == 0) ||
            (string.Compare(instanceName, "IfcTendonAnchor", true) == 0) ||
            (string.Compare(instanceName, "IfcTendon", true) == 0)))
            return "IfcReinforcingElementType";

         return instanceName + "Type";
      }

      /// <summary>
      /// Generate the Entity type name corresponding to an instance.
      /// </summary>
      /// <param name="instance">The instance type.</param>
      /// <returns>The type name.</returns>
      /// <remarks>
      /// This is done in a heuristic fashion, so we will need to 
      /// make sure exceptions are dealt with.
      /// </remarks>
      public static string GetTypeNameFromInstance(IFCEntityType instance, bool exportAsOlderThanIFC4)
      {
         // Deal with exceptions.
         switch (instance)
         {
            case IFCEntityType.IfcProduct:
               return "IfcTypeProduct";
            case IFCEntityType.IfcObject:
               return "IfcTypeObject";
            case IFCEntityType.IfcWindow:
               return exportAsOlderThanIFC4 ? "IfcWindowStyle" : "IfcWindowType";
            case IFCEntityType.IfcDoor:
               return exportAsOlderThanIFC4 ? "IfcDoorStyle" : "IfcDoorType";
            case IFCEntityType.IfcReinforcingBar:
            case IFCEntityType.IfcReinforcingMesh:
            case IFCEntityType.IfcTendonAnchor:
            case IFCEntityType.IfcTendon:
               // IfcReinforcingBarType, IfcReinforcingMeshType, IfcTendonAnchorType, and IfcTendonType
               // are type entities introduced only in IFC 4. For older schemas, we use the basic type entity instead.
               if (!exportAsOlderThanIFC4)
                  break;
               return "IfcReinforcingElementType";
         }

         return IFCAnyHandleUtil.GetIFCEntityTypeName(instance) + "Type";
      }
      
      /// <summary>
       /// Find a Non-Abstract Super Type in the current IFC Schema
       /// </summary>
       /// <param name="ifcEntitySchemaTree">The IFC schema entity tree.</param>
       /// <param name="entity">The entity type.</param>
       /// <param name="stopNode">Optional list of entity name(s) to stop the search.</param>
       /// <returns>The appropriate node or null.</returns>
      static public IfcSchemaEntityNode FindNonAbsInstanceSuperType(IfcSchemaEntityTree ifcEntitySchemaTree, IFCVersion version, string typeName)
      {
         IfcSchemaEntityNode res = null;

         // Note: Implementer's agreement #CV-2x3-166 changes IfcSpaceHeaterType from IfcEnergyConversionDevice to IfcFlowTerminal.
         if (version == IFCVersion.IFC2x3 && typeName.Equals("IfcSpaceHeaterType", StringComparison.InvariantCultureIgnoreCase))
         {
            res = ifcEntitySchemaTree.Find("IfcFlowTerminal");
            if (res.IsAbstract)
               return null;
            return res;
         }

         bool schemaOlderThanIFC4 = version is IFCVersion.IFC2x3 or IFCVersion.IFC2x2;

         string theTypeName = typeName.EndsWith("Type", StringComparison.CurrentCultureIgnoreCase) ?
            typeName : GetTypeNameFromInstanceName(typeName, schemaOlderThanIFC4);

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
               if (entNode != null && !entNode.IsAbstract)
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
      /// <param name="context">The IFC schema context</param>
      /// <param name="entityType">the entity name</param>
      /// <param name="stopNode">optional list of entity name(s) to stop the search</param>
      /// <returns>the appropriate node or null</returns>
      static public IfcSchemaEntityNode FindNonAbsSuperType(IfcSchemaEntityTree ifcEntitySchemaTree, IFCEntityType entityType, 
         params IFCEntityType[] stopNode)
      {
         IfcSchemaEntityNode res = null;

         string entityName = IFCAnyHandleUtil.GetIFCEntityTypeName(entityType);
         IfcSchemaEntityNode entNode = ifcEntitySchemaTree.Find(entityName);

         if (entNode != null)
         {
            if (stopNode.Contains(entNode.EntityType))
               return res;

            while (true)
            {
               entNode = entNode.GetParent();
               // no more parent node to get
               if (entNode == null)
                  break;

               if (!entNode.IsAbstract)
               {
                  res = entNode;
                  break;
               }

               if (stopNode.Contains(entityType))
               {
                  return res;
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
      static public IList<IfcSchemaEntityNode> FindAllSuperTypes(IfcSchemaEntityTree ifcEntitySchemaTree, string entityName, 
         params IFCEntityType[] stopNode)
      {
         List<IfcSchemaEntityNode> res = [];

         IfcSchemaEntityNode entNode = ifcEntitySchemaTree.Find(entityName);

         if (entNode != null)
         {
            // return the list when it reaches the stop node
            if (stopNode.Contains(entNode.EntityType))
               return res;

            bool continueSearch = true;
            while (continueSearch)
            {
               entNode = entNode.GetParent();
               // no more parent node to get
               if (entNode == null)
                  break;

               // Stop the search when it reaches the stop node
               if (stopNode.Contains(entNode.EntityType))
               {
                  continueSearch = false;
               }

               res.Add(entNode);
            }
         }

         return res;
      }

      /// <summary>
      /// Check whether an entity is a subtype of another entity
      /// </summary>
      /// <param name="subType">candidate of the subtype entity</param>
      /// <param name="superType">candidate of the supertype entity</param>
      /// <returns>true: if the the subType is a strict subtype of superType</returns>
      public bool IsStrictSubTypeOf(IFCEntityType subType, IFCEntityType superType)
      {
         IfcSchemaEntityNode theNode = Find(IFCAnyHandleUtil.GetIFCEntityTypeName(subType));
         return theNode?.IsSubTypeOf(superType, true) ?? false;
      }

      /// <summary>
      /// Check whether an entity is a subtype of another entity
      /// </summary>
      /// <param name="subType">candidate of the subtype entity</param>
      /// <param name="superType">candidate of the supertype entity</param>
      /// <returns>true: if the the subType is the subtype of superType</returns>
      public bool IsSubTypeOf(IFCEntityType subType, IFCEntityType superType, bool strict = true)
      {
         return (!strict && subType == superType) || IsStrictSubTypeOf(subType, superType);
      }

      /// <summary>
      /// Check whether an entity is a subtype of another entity
      /// </summary>
      /// <param name="subType">candidate of the subtype entity</param>
      /// <param name="superType">candidate of the supertype entity</param>
      /// <returns>true: if the the subType is the subtype of superType</returns>
      static public bool IsSubTypeOf(IFCVersion ifcVersion, IFCEntityType subType, IFCEntityType superType, bool strict = true)
      {
         if (!strict && subType == superType)
            return true;

         IFCSchemaFileVersion schemaFileVersion = GetSchemaVersion(ifcVersion);
         IfcSchemaEntityTree ifcEntitySchemaTree = IFCSchemaDict[(int)schemaFileVersion];
         if ((ifcEntitySchemaTree?.IfcEntityDict?.Count ?? 0) == 0)
            throw new Exception("Unable to locate IFC Schema xsd file! Make sure the relevant xsd " + ifcVersion.ToString() + " exists.");

         return ifcEntitySchemaTree.IsStrictSubTypeOf(subType, superType);
      }

      /// <summary>
      /// Get the PredefinedType list from the processed schema
      /// </summary>
      /// <param name="context"></param>
      /// <param name="ifcEntity"></param>
      /// <returns></returns>
      static public IList<string> GetPredefinedTypeList(IFCVersion context, string ifcEntity)
      {
         IFCSchemaFileVersion schemaFileVersion = GetSchemaVersion(context);
         IfcSchemaEntityTree ifcEntitySchemaTree = IFCSchemaDict[(int)schemaFileVersion];
         return GetPredefinedTypeList(ifcEntitySchemaTree, ifcEntity);
      }

      /// <summary>
      /// Get the PredefinedType list from the given Ifc Entity tree
      /// </summary>
      /// <param name="context">The IFC version</param>
      /// <param name="ifcEntity">the specific Entity to get the PredefinedType list from</param>
      /// <returns>List of PredefinedType strings</returns>
      static public IList<string> GetPredefinedTypeList(IfcSchemaEntityTree ifcEntitySchemaTree, string ifcEntity)
      {
         if ((ifcEntitySchemaTree?.IfcEntityDict?.Count ?? 0) == 0)
            throw new Exception("Unable to locate IFC Schema xsd file! Make sure the relevant xsd exists.");

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

      /// <summary>
      /// Return whether an entity is deprecated in the IFC schema.
      /// </summary>
      public static bool IsDeprecated(IFCSchemaFileVersion version, string entityName)
      {
         return DeprecatedDict.TryGetValue(version, out var set) && set.Contains(entityName);
      }

      /// <summary>
      /// Return whether an entity is valid in the schema but unsupported for Export As.
      /// </summary>
      public static bool IsUnsupported(IFCSchemaFileVersion version, string entityName)
      {
         return UnsupportedDict.TryGetValue(version, out var set) && set.Contains(entityName);
      }

      /// <summary>
      /// Return whether an entity is deprecated or unsupported.
      /// </summary>
      public static bool IsDeprecatedOrUnsupported(IFCSchemaFileVersion version, string entityName)
      {
         return IsDeprecated(version, entityName) || IsUnsupported(version, entityName);
      }

      /// <summary>
      /// Checks whether the specified predefined type of an entity is marked as deprecated 
      /// for the given schema.
      /// </summary>
      /// <param name="version">The version of the schema.</param>
      /// <param name="entityName">The name of the entity.</param>
      /// <param name="predefinedTypeName">The predefined type to check.</param>
      /// <returns>
      /// <c>true</c> if the specified predefined type is deprecated in the given schema; otherwise, <c>false</c>.
      /// </returns>
      public static bool IsDeprecatedPredefinedType(IFCSchemaFileVersion version, string entityName, string predefinedTypeName)
      {
         if (DeprecatedPredefinedType.TryGetValue(version, out var entities))
         {
            if (entities.TryGetValue(entityName, out var predefinedTypes))
            {
               return predefinedTypes.Contains(predefinedTypeName);
            }
         }

         return false;
      }

      #endregion
   }
}
