using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Runtime.Serialization;
using System.Xml;
using Newtonsoft.Json;

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
      [JsonProperty("Version")]
      public string Version { get; set; }

      /// <summary>
      /// Pset list for MVD
      /// </summary>
      [JsonProperty("PropertySet List")]
      public HashSet<string> PropertySetList { get; set; } = new HashSet<string>();

      /// <summary>
      /// Entity list for MVD
      /// </summary>
      [JsonProperty("Entity List")]
      public HashSet<string> EntityList { get; set; } = new HashSet<string>();

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
      public bool EntityIsInTheList(string entityName)
      {
         // return true if there is no entry
         if (EntityList.Count == 0)
            return true;

         if (EntityList.Contains(entityName))
            return true;
         else
            return false;
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
         [JsonProperty("Version")]
         public string Version { get; set; }

         /// <summary>
         /// Pset list for MVD
         /// </summary>
         [JsonProperty("PropertySet List")]
         public IList<string> PropertySetList { get; set; } = new List<string>();

         /// <summary>
         /// Entity list for MVD
         /// </summary>
         [JsonProperty("Entity LIst")]
         public IList<string> EntityList { get; set; } = new List<string>();
      }

      IDictionary<string, IFCEntityAndPsetList> CertifiedEntityAndPsetDict { get; set; } = new Dictionary<string, IFCEntityAndPsetList>();

      /// <summary>
      /// IFCCertifiedEntitiesAndPSets Constructor
      /// </summary>
      public IFCCertifiedEntitiesAndPSets()
      {
         string fileLoc = Path.GetDirectoryName(System.Reflection.Assembly.GetCallingAssembly().Location);
         string filePath = Path.Combine(fileLoc, "IFCCertifiedEntitiesAndPSets.json");

         if (File.Exists(filePath))
         {
            // JavaScriptSerializer does not support mapping of Json array into Hashset (it expects List) and this code below gets the data into Lists first
            //   and then transfers the data to the final form using Hashset for performance reason
            //JavaScriptSerializer jsonConvert = new JavaScriptSerializer();
            //IDictionary<string, IFCEntityAndPsetListRawFromJson> CertifiedEntityAndPsetList = jsonConvert.Deserialize<IDictionary<string, IFCEntityAndPsetListRawFromJson>>(File.ReadAllText(filePath));
            IDictionary<string, IFCEntityAndPsetListRawFromJson> CertifiedEntityAndPsetList = JsonConvert.DeserializeObject<IDictionary<string, IFCEntityAndPsetListRawFromJson>>(File.ReadAllText(filePath));
            // Copy the data to the desired format using Hashset in IFCEntityAndPsetList
            foreach (KeyValuePair<string, IFCEntityAndPsetListRawFromJson> entPsetData in CertifiedEntityAndPsetList)
            {
               IFCEntityAndPsetList entPset = new IFCEntityAndPsetList();
               entPset.Version = entPsetData.Value.Version;
               entPset.PropertySetList = new HashSet<string>(entPsetData.Value.PropertySetList);
               entPset.EntityList = new HashSet<string>(entPsetData.Value.EntityList);
               CertifiedEntityAndPsetDict.Add(entPsetData.Key, entPset);
            }
         }
      }

      /// <summary>
      /// Check whether the pset name is valid for the current MVD
      /// </summary>
      /// <param name="psetName">the propertyset name</param>
      /// <returns>true/false</returns>
      public bool AllowPsetToBeCreatedInCurrentMVD (string psetName)
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
         if (CertifiedEntityAndPsetDict.Count == 0)
            return true;
         IFCEntityAndPsetList theList;
         if (CertifiedEntityAndPsetDict.TryGetValue(mvdName, out theList))
         {
            if (theList.PsetIsInTheList(psetName))
               return true;
            else
               return false;
         }
         else
            return true;
      }

      /// <summary>
      /// Check whether an entity name is valid in the current MVD
      /// </summary>
      /// <param name="entityName">the entity name</param>
      /// <returns>true/false</returns>
      public bool IsValidEntityInCurrentMVD (string entityName)
      {
         string mvdName = ExporterCacheManager.ExportOptionsCache.FileVersion.ToString();
         return IsValidEntityInMVD(mvdName, entityName);
      }

      /// <summary>
      /// Check whether an entity name is valid
      /// </summary>
      /// <param name="mvdName">the MVD name</param>
      /// <param name="entityName">the entity name</param>
      /// <returns>true/false</returns>
      public bool IsValidEntityInMVD(string mvdName, string entityName)
      {
         // OK to create if the list is empty (not defined)
         if (CertifiedEntityAndPsetDict.Count == 0)
            return true;
         IFCEntityAndPsetList theList;
         if (CertifiedEntityAndPsetDict.TryGetValue(mvdName, out theList))
         {
            if (theList.EntityIsInTheList(entityName))
               return true;
            else
               return false;
         }
         else
            return true;
      }
   }
}
