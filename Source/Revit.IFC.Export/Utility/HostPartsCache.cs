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
   /// The cache which holds host element and associated parts.
   /// </summary>
   public class HostPartsCache
   {
      /// <summary>
      /// The dictionary mapping from a host element to its parts. 
      /// </summary>
      private Dictionary<ElementId, Dictionary<ElementId, List<KeyValuePair<Part, IFCRange>>>> m_HostToPartsDictionary = new Dictionary<ElementId, Dictionary<ElementId, List<KeyValuePair<Part, IFCRange>>>>();

      /// <summary>
      /// Finds the parts from the dictionary.
      /// </summary>
      /// <param name="hostId">
      /// The id of host element.
      /// </param>
      /// <param name="LevelId">
      /// The id of level to finding the parts.
      /// </param>
      /// <returns>
      /// The list of parts.
      /// </returns>
      public List<KeyValuePair<Part, IFCRange>> Find(ElementId hostId, ElementId LevelId)
      {
         Dictionary<ElementId, List<KeyValuePair<Part, IFCRange>>> levelParts;
         List<KeyValuePair<Part, IFCRange>> partsList;
         if (m_HostToPartsDictionary.TryGetValue(hostId, out levelParts))
         {
            if (levelParts.TryGetValue(LevelId, out partsList))
               return partsList;
         }
         return null;
      }

      /// <summary>
      /// Adds the list of parts to the dictionary.
      /// </summary>
      /// <param name="hostId">
      /// The host element elementId.
      /// </param>
      /// <param name="partsList">
      /// The list of parts.
      /// </param>
      public void Register(ElementId hostId, Dictionary<ElementId, List<KeyValuePair<Part, IFCRange>>> levelParts)
      {
         if (HasRegistered(hostId))
            return;

         m_HostToPartsDictionary[hostId] = levelParts;
      }

      /// <summary>
      /// Identifies if the host element beem registered.
      /// </summary>
      /// <param name="hostId">The id of host element.</param>
      /// <returns>True if registered, false otherwise.</returns>
      public bool HasRegistered(ElementId hostId)
      {
         if (m_HostToPartsDictionary.ContainsKey(hostId))
            return true;
         return false;
      }
   }
}