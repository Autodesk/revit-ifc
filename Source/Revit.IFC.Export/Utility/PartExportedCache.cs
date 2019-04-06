//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012  Autodesk, Inc.
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

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of the exported Parts.
   /// </summary>
   public class PartExportedCache
   {
      /// <summary>
      /// The dictionary mapping from a exported part and it's level and host element. 
      /// </summary>
      private Dictionary<ElementId, Dictionary<ElementId, ElementId>> m_PartExportedDictionary = new Dictionary<ElementId, Dictionary<ElementId, ElementId>>();

      /// <summary>
      /// Find the host element from a part and a level.
      /// </summary>
      /// <param name="partId">The part exported.</param>
      /// <param name="LevelId">The level to which the part has exported.</param>
      /// <returns>The host element.</returns>
      public ElementId Find(ElementId partId, ElementId LevelId)
      {
         Dictionary<ElementId, ElementId> hostOverrideLevels;
         ElementId hostId;
         if (m_PartExportedDictionary.TryGetValue(partId, out hostOverrideLevels))
         {
            if (hostOverrideLevels.TryGetValue(LevelId, out hostId))
               return hostId;
         }
         return null;
      }

      /// <summary>
      /// Identifies if the part in the level has exported or not.
      /// </summary>
      /// <param name="partId">The part.</param>
      /// <param name="LevelId">The level to export.</param>
      /// <returns>True if the part in the level has exported, false otherwise.</returns>
      public bool HasExported(ElementId partId, ElementId LevelId)
      {
         if (Find(partId, LevelId) != null)
            return true;
         return false;
      }

      /// <summary>
      /// Register the exported part and its host and level.
      /// </summary>
      /// <param name="partId">The exported part.</param>
      /// <param name="hostOverrideLevels">The dictionary of host and level the part has exported.</param>
      public void Register(ElementId partId, Dictionary<ElementId, ElementId> hostOverrideLevels)
      {
         if (HasRegistered(partId))
            return;

         m_PartExportedDictionary[partId] = hostOverrideLevels;
      }

      /// <summary>
      /// Identifies if the part element been registered.
      /// </summary>
      /// <param name="hostId">The id of part element.</param>
      /// <returns>True if registered, false otherwise.</returns>
      public bool HasRegistered(ElementId partId)
      {
         if (m_PartExportedDictionary.ContainsKey(partId))
            return true;
         return false;
      }

      /// <summary>
      /// Add the exported part to the cache.
      /// </summary>
      /// <param name="partId">The exported part.</param>
      /// <param name="levelId">The level to which the part has exported.</param>
      /// <param name="hostId">The host element the part has exported.</param>
      public void Add(ElementId partId, ElementId levelId, ElementId hostId)
      {
         m_PartExportedDictionary[partId].Add(levelId, hostId);
      }
   }
}