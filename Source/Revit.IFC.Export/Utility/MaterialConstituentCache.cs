using System;
using System.Collections;
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
      /// Fraction of the total volume of the material that this constituent represents.
      /// </summary>
      public double Fraction { get; set; } = 0.0;

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

      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="componentCat">The geometry component category name</param>
      /// <param name="materialId">The material elementid</param>
      /// <param name="fraction">The material fraction</param>
      public MaterialConstituentInfo(string componentCat, ElementId materialId, double fraction)
      {
         ComponentCat = componentCat;
         MaterialId = materialId;
         Fraction = fraction;
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
            && obj1.MaterialId.Equals(obj2.MaterialId)
            && MathUtil.IsAlmostEqual(obj1.Fraction, obj2.Fraction));
      }

      public int GetHashCode(MaterialConstituentInfo obj)
      {
         int hash = 23;
         hash = hash * 31 + obj.ComponentCat.GetHashCode();
         hash = hash * 31 + obj.MaterialId.GetHashCode();
         hash = hash * 31 + Math.Floor(obj.Fraction).GetHashCode();
         return hash;
      }
   }

   public class MaterialConstituentCache
   {
      /// <summary>
      /// The dictionary mapping from a Material Constituent to a handle. 
      /// </summary>
      private Dictionary<MaterialConstituentInfo, IFCAnyHandle> MaterialConstDictionary { get; set; } =
         new(new MaterialConsituentInfoComparer());

      /// <summary>
      /// Find the handle from the cache using only the Material Id.
      /// </summary>
      /// <param name="materialId"></param>
      /// <returns></returns>
      public IFCAnyHandle Find(ElementId materialId, double fraction)
      {
         // If only a Material ElementId is provided, default the constituent name to be the same as the material name
         Material material = ExporterCacheManager.Document.GetElement(materialId) as Material;
         string catName = (material != null) ? NamingUtil.GetMaterialName(material) : "<Unnamed>";    // Default name to the Material name if not null or <Unnamed>
         MaterialConstituentInfo constInfo = new MaterialConstituentInfo(catName, materialId, fraction);
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
         if (MaterialConstDictionary.TryGetValue(constInfo, out handle))
         {
            // We need to make sure the handle isn't stale.  If it is, remove it. 
            try
            {
               if (IFCAnyHandleUtil.IsNullOrHasNoValue(handle))
               {
                  MaterialConstDictionary.Remove(constInfo);
                  handle = null;
               }
            }
            catch
            {
               MaterialConstDictionary.Remove(constInfo);
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
         if (MaterialConstDictionary.ContainsKey(constInfo))
            return;

         MaterialConstDictionary[constInfo] = handle;
      }

      /// <summary>
      /// Register Material Constituent Handle with only Material Id. This is the original behavior
      /// </summary>
      /// <param name="materialId">the material elementId</param>
      /// <param name="handle">the handle</param>
      public void Register(ElementId materialId, double fraction, IFCAnyHandle handle)
      {
         // If only a Material ElementId is provided, default the constituent name to be the same as the material name
         Material material = ExporterCacheManager.Document.GetElement(materialId) as Material;
         string catName = (material != null) ? NamingUtil.GetMaterialName(material) : "<Unnamed>";    // Default name to the Material name if not null or <Unnamed>
         MaterialConstituentInfo constInfo = new MaterialConstituentInfo(catName, materialId, fraction);
         if (MaterialConstDictionary.ContainsKey(constInfo))
            return;

         MaterialConstDictionary[constInfo] = handle;
      }

      /// <summary>
      /// Delete an element from the cache
      /// </summary>
      /// <param name="consInfo">the Material Constituent Info</param>
      public void Delete(MaterialConstituentInfo constInfo)
      {
         if (MaterialConstDictionary.ContainsKey(constInfo))
         {
            IFCAnyHandle handle = MaterialConstDictionary[constInfo];
            MaterialConstDictionary.Remove(constInfo);
            ExporterCacheManager.HandleToElementCache.Delete(handle);
         }
      }

      /// <summary>
      /// Clear the dictionary. Constituent should not be cached beyond the Set
      /// </summary>
      public void Clear()
      {
         MaterialConstDictionary.Clear();
      }

   }
}
