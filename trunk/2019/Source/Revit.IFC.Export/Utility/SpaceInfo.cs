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
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.Mechanical;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// The class contains relation of IFC space and elements.
   /// </summary>
   public class SpaceInfo
   {
      IFCAnyHandle m_SpaceHandle;
      HashSet<IFCAnyHandle> m_RelatedElements = new HashSet<IFCAnyHandle>();

      /// <summary>
      /// Constructs default object.
      /// </summary>
      public SpaceInfo()
      {

      }

      /// <summary>
      /// Construct a SpaceInfo object.
      /// </summary>
      /// <param name="spaceHandle">The space handle.</param>
      public SpaceInfo(IFCAnyHandle spaceHandle)
      {
         m_SpaceHandle = spaceHandle;
      }

      /// <summary>
      /// The space handle.
      /// </summary>
      public IFCAnyHandle SpaceHandle
      {
         get { return m_SpaceHandle; }
         set { m_SpaceHandle = value; }
      }

      /// <summary>
      /// The related elements.
      /// </summary>
      public HashSet<IFCAnyHandle> RelatedElements
      {
         get { return m_RelatedElements; }
      }
   }

   /// <summary>
   /// Used to keep a cache of the SpaceInfo objects mapping to a SpatialElement.
   /// </summary>
   public class SpaceInfoCache
   {
      Dictionary<ElementId, SpaceInfo> m_SpaceInfos = new Dictionary<ElementId, SpaceInfo>();

      /// <summary>
      /// Returns true if any architectural rooms are cached.
      /// </summary>
      public bool ContainsRooms
      {
         get;
         set;
      }

      /// <summary>
      /// Returns true if any MEP spaces are cached.
      /// </summary>
      public bool ContainsSpaces
      {
         get;
         set;
      }

      /// <summary>
      /// The direction of the SpaceInfos mapping to SpatialElement ids.
      /// </summary>
      public Dictionary<ElementId, SpaceInfo> SpaceInfos
      {
         get { return m_SpaceInfos; }
      }

      /// <summary>
      /// Finds the SpaceInfo.
      /// </summary>
      /// <param name="spatialElementId">The SpatialElement id.</param>
      /// <returns></returns>
      public SpaceInfo FindSpaceInfo(ElementId spatialElementId)
      {
         SpaceInfo spaceInfo;
         m_SpaceInfos.TryGetValue(spatialElementId, out spaceInfo);
         return spaceInfo;
      }

      /// <summary>
      /// Finds the space handle from a spatial element id.
      /// </summary>
      /// <param name="spatialElementId">The spatial element id.</param>
      /// <returns>The handle.</returns>
      public IFCAnyHandle FindSpaceHandle(ElementId spatialElementId)
      {
         SpaceInfo spaceInfo;
         if (m_SpaceInfos.TryGetValue(spatialElementId, out spaceInfo))
            return spaceInfo.SpaceHandle;
         return null;
      }

      /// <summary>
      /// Sets the space handle to corresponding spatial element.
      /// </summary>
      /// <param name="spatialElement">The spatial element.</param>
      /// <param name="spaceHandle">The space handle.</param>
      public void SetSpaceHandle(Element spatialElement, IFCAnyHandle spaceHandle)
      {
         SpaceInfo spaceInfo;
         if (m_SpaceInfos.TryGetValue(spatialElement.Id, out spaceInfo))
         {
            spaceInfo.SpaceHandle = spaceHandle;
         }
         else
         {
            m_SpaceInfos[spatialElement.Id] = new SpaceInfo(spaceHandle);
         }
         if (!ContainsRooms)
            ContainsRooms = spatialElement is Room;
         if (!ContainsSpaces)
            ContainsSpaces = spatialElement is Space;
      }

      /// <summary>
      /// Adds relation from a element handle to a spatial element.
      /// </summary>
      /// <param name="spatialElementId">The spatial element id.</param>
      /// <param name="elemHandle">The element handle.</param>
      public void RelateToSpace(ElementId spatialElementId, IFCAnyHandle elemHandle)
      {
         SpaceInfo spaceInfo = FindSpaceInfo(spatialElementId);
         if (spaceInfo == null)
         {
            spaceInfo = new SpaceInfo();
            m_SpaceInfos[spatialElementId] = spaceInfo;
         }
         spaceInfo.RelatedElements.Add(elemHandle);
      }

      public SpaceInfoCache()
      {
         ContainsRooms = false;
         ContainsSpaces = false;
      }
   }
}