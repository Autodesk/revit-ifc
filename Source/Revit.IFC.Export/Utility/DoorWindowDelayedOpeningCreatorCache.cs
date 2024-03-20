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
   /// Used to keep a cache to create door window openings.
   /// </summary>
   public class DoorWindowDelayedOpeningCreatorCache
   {
      // An opening associated with an insert may cut more than one host if the host has been split by level.  Ensure that we create all of the appropriate openings.
      // The ElementId of the main dictionary key is the insert id; the ElementId of the internal dictionary key is the base level id.
      // If we ae not splitting by level, we ignore the level id.
      // TODO: we should split the openings so that they are "trimmed" by the extents of the host element.
      Dictionary<ElementId, Dictionary<ElementId, DoorWindowDelayedOpeningCreator>> m_DelayedOpeningCreators =
          new Dictionary<ElementId, Dictionary<ElementId, DoorWindowDelayedOpeningCreator>>();

      /// <summary>
      /// Adds a new DoorWindowDelayedOpeningCreator.
      /// </summary>
      /// <param name="creator">The creator.</param>
      public void Add(DoorWindowDelayedOpeningCreator creator)
      {
         if (creator == null)
            return;

         Dictionary<ElementId, DoorWindowDelayedOpeningCreator> existingOpenings = null;
         if (!m_DelayedOpeningCreators.TryGetValue(creator.InsertId, out existingOpenings))
         {
            existingOpenings = new Dictionary<ElementId, DoorWindowDelayedOpeningCreator>();
            m_DelayedOpeningCreators[creator.InsertId] = existingOpenings;
         }

         ElementId levelIdToUse = ExporterCacheManager.ExportOptionsCache.WallAndColumnSplitting ? creator.LevelId : ElementId.InvalidElementId;

         DoorWindowDelayedOpeningCreator oldCreator = null;
         if (existingOpenings.TryGetValue(levelIdToUse, out oldCreator))
         {
            // from DoorWindowInfo has higher priority
            if (oldCreator.CreatedFromDoorWindowInfo)
            {
               if (!oldCreator.HasValidGeometry && creator.HasValidGeometry)
               {
                  oldCreator.CopyGeometry(creator);
               }
            }
            else if (creator.CreatedFromDoorWindowInfo)
            {
               if (!creator.HasValidGeometry && oldCreator.HasValidGeometry)
               {
                  creator.CopyGeometry(oldCreator);
               }
               existingOpenings[levelIdToUse] = creator;
            }
         }
         else
            existingOpenings[levelIdToUse] = creator;
      }

      /// <summary>
      /// Executes all opening creators in this cache.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="doc">The document.</param>
      public void ExecuteCreators(ExporterIFC exporterIFC, Document doc)
      {
         foreach (Dictionary<ElementId, DoorWindowDelayedOpeningCreator> creators in m_DelayedOpeningCreators.Values)
         {
            foreach (DoorWindowDelayedOpeningCreator creator in creators.Values)
            {
               //Geometry can become invalid when ExtrusionData or Solids are null or count is 0
               if (creator.HasValidGeometry)
                  creator.Execute(exporterIFC, doc);
            }
         }
      }
   }
}