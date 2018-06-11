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
   /// The cache which holds dummy element and its levels and ranges.
   /// You can check, find, and crate a dummy element via the ElementId of host element. 
   /// </summary>
   public class DummyHostCache
   {
      /// <summary>
      /// The dictionary mapping from a dummy host element and its levels and ranges. 
      /// </summary>
      private Dictionary<ElementId, List<KeyValuePair<ElementId, IFCRange>>> m_DummyHostDictionary = new Dictionary<ElementId, List<KeyValuePair<ElementId, IFCRange>>>();

      /// <summary>
      /// Find the levels and ranges of from a host element.
      /// </summary>
      /// <param name="hostId">The ElementId of host element.</param>
      /// <returns>The list of level and IFCRange pair.</returns>
      public List<KeyValuePair<ElementId, IFCRange>> Find(ElementId hostId)
      {
         List<KeyValuePair<ElementId, IFCRange>> levelRange;
         if (m_DummyHostDictionary.TryGetValue(hostId, out levelRange))
         {
            return levelRange;
         }
         return null;
      }

      /// <summary>
      /// Register the host element and its levels and ranges.
      /// </summary>
      /// <param name="hostId">The ElementId of host element.</param>
      /// <param name="levelRanges">The list of level and IFCRange pair.</param>
      public void Register(ElementId hostId, List<KeyValuePair<ElementId, IFCRange>> levelRanges)
      {
         if (HasRegistered(hostId))
            return;

         m_DummyHostDictionary[hostId] = levelRanges;
      }

      /// <summary>
      /// Identifies if the host element been registered.
      /// </summary>
      /// <param name="hostId">The ElementId of host element.</param>
      /// <returns>True if registered, false otherwise.</returns>
      public bool HasRegistered(ElementId hostId)
      {
         if (m_DummyHostDictionary.ContainsKey(hostId))
            return true;
         return false;
      }
   }
}