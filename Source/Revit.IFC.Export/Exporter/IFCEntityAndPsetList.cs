using Newtonsoft.Json;

using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;

using System.Collections.Generic;
using System.IO;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Valid Entity and Pset list according to MVD definitions
   /// </summary>
   public class IFCEntityAndPsetList
   {
      /// <summary>
      /// The MVD version
      /// </summary>
      public string Version { get; set; }

      /// <summary>
      /// Pset list for MVD
      /// </summary>
      public HashSet<string> PropertySetList { get; set; } = [];

      /// <summary>
      /// Entity list for MVD
      /// </summary>
      public HashSet<IFCEntityType> EntityList { get; set; } = [];

      /// <summary>
      /// Check whether a Pset name is found in the list
      /// </summary>
      /// <param name="psetName">Pset name</param>
      /// <returns>true/false</returns>
      public bool PsetIsInTheList(string psetName)
      {
         // return true if there is no entry
         if (PropertySetList.Count == 0)
            return true;

         if (PropertySetList.Contains(psetName))
            return true;
         else
            return false;
      }

      /// <summary>
      /// Check whether an Entity name is found in the list
      /// </summary>
      /// <param name="entityName">the entity name</param>
      /// <returns>true/false</returns>
      public bool EntityIsInTheList(IFCEntityType entityType)
      {
         return EntityList.Count == 0 || EntityList.Contains(entityType);
      }
   }

   /// <summary>
   /// List of valid Entities and Psets in an MVD
   /// </summary>
   public class IFCCertifiedEntitiesAndPSets
   {
      /// <summary>
      /// Valid Entity and Pset list according to MVD definitions
      /// </summary>
      class IFCEntityAndPsetListRawFromJson
      {
         /// <summary>
         /// The MVD version
         /// </summary>
         public string Version { get; set; }

         /// <summary>
         /// Pset list for MVD
         /// </summary>
         public List<string> PropertySetList { get; set; } = [];

         /// <summary>
         /// Entity list for MVD
         /// </summary>
         public List<IFCEntityType> EntityList { get; set; } = [];
      }

      Dictionary<string, IFCEntityAndPsetList> CertifiedEntityAndPsetDict { get; set; } = [];
      
      /// <summary>
      /// IFCCertifiedEntitiesAndPSets Constructor
      /// </summary>
      public IFCCertifiedEntitiesAndPSets()
      {
         string fileLoc = Path.GetDirectoryName(System.Reflection.Assembly.GetCallingAssembly().Location);
         string filePath = Path.Combine(fileLoc, "IFCCertifiedEntitiesAndPSets.json");

         if (File.Exists(filePath))
         {
            IDictionary<string, IFCEntityAndPsetListRawFromJson> CertifiedEntityAndPsetList = JsonConvert.DeserializeObject<IDictionary<string, IFCEntityAndPsetListRawFromJson>>(File.ReadAllText(filePath));
            // Copy the data to the desired format using Hashset in IFCEntityAndPsetList
            foreach (KeyValuePair<string, IFCEntityAndPsetListRawFromJson> entPsetData in CertifiedEntityAndPsetList)
            {
               IFCEntityAndPsetList entPset = new();
               entPset.Version = entPsetData.Value.Version;
               entPset.PropertySetList = [ .. entPsetData.Value.PropertySetList ];
               entPset.EntityList = [ .. entPsetData.Value.EntityList ];
               CertifiedEntityAndPsetDict.Add(entPsetData.Key, entPset);
            }
         }
      }

      /// <summary>
      /// Check whether the pset name is valid for the current MVD
      /// </summary>
      /// <param name="psetName">the propertyset name</param>
      /// <returns>true/false</returns>
      public bool AllowPsetToBeCreatedInCurrentMVD(string psetName)
      {
         string mvdName = ExporterCacheManager.ExportOptionsCache.FileVersion.ToString();
         return AllowPsetToBeCreated(mvdName, psetName);
      }

      /// <summary>
      /// Check whether the pset name is valid
      /// </summary>
      /// <param name="mvdName">the MVD name</param>
      /// <param name="psetName">the propertyset name</param>
      /// <returns>true/false</returns>
      public bool AllowPsetToBeCreated(string mvdName, string psetName)
      {
         // OK to create if the list is empty (not defined)
         if (CertifiedEntityAndPsetDict.Count == 0 || !CertifiedEntityAndPsetDict.TryGetValue(mvdName, out IFCEntityAndPsetList theList))
            return true;

         return theList.PsetIsInTheList(psetName);
      }

      /// <summary>
      /// Check whether the predefined property name is valid
      /// </summary>
      /// <param name="mvdName">the MVD name</param>
      /// <param name="psetName">the predefined property name</param>
      /// <returns>true/false</returns>
      public bool AllowPredefPsetToBeCreated(string mvdName, string psetName)
      {
         // OK to create if the list is empty (not defined)
         if (CertifiedEntityAndPsetDict.Count == 0 || !CertifiedEntityAndPsetDict.TryGetValue(mvdName, out IFCEntityAndPsetList theList))
            return true;

         return theList.EntityIsInTheList(IFCAnyHandleUtil.GetIFCEntityTypeFromName(psetName));
      }

      /// <summary>
      /// Check whether an entity name is valid in the current MVD
      /// </summary>
      /// <param name="entityName">the entity name</param>
      /// <returns>true/false</returns>
      public bool IsValidEntityInCurrentMVD(IFCEntityType entityType)
      {
         string mvdName = ExporterCacheManager.ExportOptionsCache.FileVersion.ToString();
         return IsValidEntityInMVD(mvdName, entityType);
      }

      /// <summary>
      /// Check whether an entity name is valid.
      /// </summary>
      /// <param name="mvdName">The MVD name.</param>
      /// <param name="entityType">The entity type.</param>
      /// <returns>true/false</returns>
      public bool IsValidEntityInMVD(string mvdName, IFCEntityType entityType)
      {
         // OK to create if the list is empty (not defined)
         if (CertifiedEntityAndPsetDict.Count == 0 || !CertifiedEntityAndPsetDict.TryGetValue(mvdName, out IFCEntityAndPsetList theList))
            return true;

         return theList.EntityIsInTheList(entityType);
      }
   }
}
