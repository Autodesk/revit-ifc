//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
// Copyright (C) 2013  Autodesk, Inc.
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Data;
using UnitSystem = Autodesk.Revit.DB.DisplayUnit;

namespace Revit.IFC.Import.Utility
{
   /// <summary>
   /// A class that contains a map from material names to the settable values of Material from IFC.
   /// </summary>
   public class IFCMaterialCache
   {
      IDictionary<string, IList<IFCMaterialInfo>> m_MaterialCache = null;

      public IDictionary<string, IList<IFCMaterialInfo>> MaterialCache
      {
         get
         {
            if (m_MaterialCache == null)
               m_MaterialCache = new SortedDictionary<string, IList<IFCMaterialInfo>>(StringComparer.InvariantCultureIgnoreCase);
            return m_MaterialCache;
         }
      }

      /// <summary>
      /// The default constructor.
      /// </summary>
      public IFCMaterialCache()
      {
      }

      /// <summary>
      /// Add a material info entry to a new or existing material name.
      /// </summary>
      /// <param name="name">The material name.</param>
      /// <param name="info">The material information.</param>
      public void Add(string name, IFCMaterialInfo info)
      {
         IList<IFCMaterialInfo> createdMaterials;
         if (!MaterialCache.TryGetValue(name, out createdMaterials))
         {
            createdMaterials = new List<IFCMaterialInfo>();
            MaterialCache[name] = createdMaterials;
         }
         createdMaterials.Add(info);
      }

      /// <summary>
      /// Finds a material with the same name and information.
      /// </summary>
      /// <param name="name">The material name.</param>
      /// <param name="id">The id of the material.  We will look for potential matches with the id included in the name.</param>
      /// <param name="info">The material information.</param>
      /// <returns></returns>
      public ElementId FindMatchingMaterial(string name, int id, IFCMaterialInfo info)
      {
         IList<IFCMaterialInfo> createdMaterials;
         if (!MaterialCache.TryGetValue(name, out createdMaterials))
            return ElementId.InvalidElementId;

         int infoTransparency = info.Transparency.HasValue ? info.Transparency.Value : 0;
         foreach (IFCMaterialInfo createdMaterial in createdMaterials)
         {
            if (info.Color != null)
            {
               if (createdMaterial.Color == null)
                  continue;
               if ((createdMaterial.Color.Red != info.Color.Red) ||
                   (createdMaterial.Color.Green != info.Color.Green) ||
                   (createdMaterial.Color.Blue != info.Color.Blue))
                  continue;
            }

            int createdMaterialTransparency = createdMaterial.Transparency.HasValue ? createdMaterial.Transparency.Value : 0;
            if (infoTransparency != createdMaterialTransparency)
               continue;

            if (info.Shininess.HasValue)
            {
               if (!createdMaterial.Shininess.HasValue)
                  continue;
               if (info.Shininess.Value != createdMaterial.Shininess.Value)
                  continue;
            }

            if (info.Smoothness.HasValue)
            {
               if (!createdMaterial.Smoothness.HasValue)
                  continue;
               if (info.Smoothness.Value != createdMaterial.Smoothness.Value)
                  continue;
            }

            return createdMaterial.ElementId;
         }

         // We found a name match, but it didn't have the right materials.  Try again with id appended to the name.
         string newMaterialName = Importer.TheCache.CreatedMaterials.GetUniqueMaterialName(name, id);
         return FindMatchingMaterial(newMaterialName, id, info);
      }

      /// <summary>
      /// Ensure that a material has a unique name.
      /// </summary>
      /// <param name="originalName">The original name.</param>
      /// <param name="id">The id of the material.</param>
      /// <returns>A unique name, either the original name or original name + id.</returns>
      public string GetUniqueMaterialName(string originalName, int id)
      {
         string newName = originalName;
         while (MaterialCache.ContainsKey(newName))
            newName = newName + " " + id;
         return newName;
      }
   }
}