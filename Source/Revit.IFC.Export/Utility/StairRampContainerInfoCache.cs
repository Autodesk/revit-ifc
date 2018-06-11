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
using Revit.IFC.Export.Exporter;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of the element ids mapping to a StairRampContainerInfo.
   /// </summary>
   public class StairRampContainerInfoCache : Dictionary<ElementId, StairRampContainerInfo>
   {
      /// <summary>
      /// Adds a StairRampContainerInfo for an element.
      /// </summary>
      /// <param name="elementId">The element id.</param>
      /// <param name="stairRampContainerInfo">The StairRampContainerInfo.</param>
      public void AddStairRampContainerInfo(ElementId elementId, StairRampContainerInfo stairRampContainerInfo)
      {
         this[elementId] = stairRampContainerInfo;
      }

      /// <summary>
      /// Appends information of a StairRampContainerInfo to an existing one in the cache or add it if there is no existing one.
      /// </summary>
      /// <param name="elementId">The element id.</param>
      /// <param name="stairRampContainerInfo">The StairRampContainerInfo.</param>
      public void AppendStairRampContainerInfo(ElementId elementId, StairRampContainerInfo stairRampContainerInfo)
      {
         StairRampContainerInfo existStairRampContainerInfo = null;

         if (!TryGetValue(elementId, out existStairRampContainerInfo))
            AddStairRampContainerInfo(elementId, stairRampContainerInfo);
         else
         {
            existStairRampContainerInfo.StairOrRampHandles.AddRange(stairRampContainerInfo.StairOrRampHandles);
            existStairRampContainerInfo.Components.AddRange(stairRampContainerInfo.Components);
            existStairRampContainerInfo.LocalPlacements.AddRange(stairRampContainerInfo.LocalPlacements);
         }
      }

      /// <summary>
      /// Checks if it contains a StairRampContainerInfo of an element.
      /// </summary>
      /// <param name="elementId">The element id.</param>
      /// <returns>True if there is, false if there is none.</returns>
      public bool ContainsStairRampContainerInfo(ElementId elementId)
      {
         return this.ContainsKey(elementId);
      }

      /// <summary>
      /// Gets the StairRampContainerInfo of an element.
      /// </summary>
      /// <param name="elementId">The element id.</param>
      /// <returns>The StairRampContainerInfo.</returns>
      public StairRampContainerInfo GetStairRampContainerInfo(ElementId elementId)
      {
         StairRampContainerInfo existStairRampContainerInfo = null;
         TryGetValue(elementId, out existStairRampContainerInfo);
         return existStairRampContainerInfo;
      }
   }
}