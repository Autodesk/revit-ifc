using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// MaterialConstituentInfo class for Material constituent defined by Component Category name and he Material Id
   /// </summary>
   public class MaterialConstituentInfo
   {
      /// <summary>
      /// Geometry component category/sub-category name
      /// </summary>
      public string ComponentCat { get; private set; }

      /// <summary>
      /// Material Id
      /// </summary>
      public ElementId MaterialId { get; private set; }
      
      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="componentCat">The geometry component category name</param>
      /// <param name="materialId">The material elementid</param>
      public MaterialConstituentInfo(string componentCat, ElementId materialId)
      {

         ComponentCat = componentCat;
         MaterialId = materialId;
      }
   }

   /// <summary>
   /// Comparer class for the MaterialConstituentInfo
   /// </summary>
   public class MaterialConsituentInfoComparer : IEqualityComparer<MaterialConstituentInfo>
   {
      public bool Equals(MaterialConstituentInfo obj1, MaterialConstituentInfo obj2)
      {
         return (obj1.ComponentCat.Equals(obj2.ComponentCat, StringComparison.CurrentCultureIgnoreCase)
            && obj1.MaterialId.Equals(obj2.MaterialId));
      }

      public int GetHashCode(MaterialConstituentInfo obj)
      {
         int hash = 23;
         hash = hash * 31 + obj.ComponentCat.GetHashCode();
         hash = hash * 31 + obj.MaterialId.GetHashCode();
         return hash;
      }
   }

   public class MaterialConstituentCache
   {
      /// <summary>
      /// The dictionary mapping from a Material Constituent to a handle. 
      /// </summary>
      private Dictionary<MaterialConstituentInfo, IFCAnyHandle> m_MaterialConstDictionary = null;

      public MaterialConstituentCache()
      {
         MaterialConsituentInfoComparer comparer = new MaterialConsituentInfoComparer();
         if (m_MaterialConstDictionary == null)
            m_MaterialConstDictionary = new Dictionary<MaterialConstituentInfo, IFCAnyHandle>(comparer);
      }

      /// <summary>
      /// Find the handle from the cache using only the Material Id.
      /// </summary>
      /// <param name="materialId"></param>
      /// <returns></returns>
      public IFCAnyHandle Find(ElementId materialId)
      {
         // If only a Material ElementId is provided, default the constituent name to be the same as the material name
         Material material = ExporterCacheManager.Document.GetElement(materialId) as Material;
         string catName = (material != null) ? NamingUtil.GetMaterialName(material) : "<Unnamed>";    // Default name to the Material name if not null or <Unnamed>
         MaterialConstituentInfo constInfo = new MaterialConstituentInfo(catName, materialId);
         return Find(constInfo);
      }

      /// <summary>
      /// Finds the handle from the dictionary.
      /// </summary>
      /// <param name="constInfo">The Material Constituent Info</param>
      /// <returns>The handle</returns>
      public IFCAnyHandle Find(MaterialConstituentInfo constInfo)
      {
         IFCAnyHandle handle = null;
         if (m_MaterialConstDictionary.TryGetValue(constInfo, out handle))
         {
            // We need to make sure the handle isn't stale.  If it is, remove it. 
            try
            {
               if (!IFCAnyHandleUtil.IsValidHandle(handle))
               {
                  m_MaterialConstDictionary.Remove(constInfo);
                  handle = null;
               }
            }
            catch
            {
               m_MaterialConstDictionary.Remove(constInfo);
               handle = null;
            }
         }
         return handle;
      }

      /// <summary>
      /// Adds the handle to the dictionary.
      /// </summary>
      /// <param name="constInfo">The Material Constituent Info</param>
      /// <param name="handle">The handle</param>
      public void Register(MaterialConstituentInfo constInfo, IFCAnyHandle handle)
      {
         if (m_MaterialConstDictionary.ContainsKey(constInfo))
            return;

         m_MaterialConstDictionary[constInfo] = handle;
      }

      /// <summary>
      /// Register Material Constituent Handle with only Material Id. This is the original behavior
      /// </summary>
      /// <param name="materialId">the material elementId</param>
      /// <param name="handle">the handle</param>
      public void Register(ElementId materialId, IFCAnyHandle handle)
      {
         // If only a Material ElementId is provided, default the constituent name to be the same as the material name
         Material material = ExporterCacheManager.Document.GetElement(materialId) as Material;
         string catName = (material != null) ? NamingUtil.GetMaterialName(material) : "<Unnamed>";    // Default name to the Material name if not null or <Unnamed>
         MaterialConstituentInfo constInfo = new MaterialConstituentInfo(catName, materialId);
         if (m_MaterialConstDictionary.ContainsKey(constInfo))
            return;

         m_MaterialConstDictionary[constInfo] = handle;
      }

      /// <summary>
      /// Delete an element from the cache
      /// </summary>
      /// <param name="consInfo">the Material Constituent Info</param>
      public void Delete(MaterialConstituentInfo constInfo)
      {
         if (m_MaterialConstDictionary.ContainsKey(constInfo))
         {
            IFCAnyHandle handle = m_MaterialConstDictionary[constInfo];
            m_MaterialConstDictionary.Remove(constInfo);
            ExporterCacheManager.HandleToElementCache.Delete(handle);
         }
      }

      /// <summary>
      /// Clear the dictionary. Constituent should not be cached beyond the Set
      /// </summary>
      public void Reset()
      {
         if (m_MaterialConstDictionary != null)
            m_MaterialConstDictionary.Clear();
         else
            m_MaterialConstDictionary = new Dictionary<MaterialConstituentInfo, IFCAnyHandle>();
      }

   }
}
