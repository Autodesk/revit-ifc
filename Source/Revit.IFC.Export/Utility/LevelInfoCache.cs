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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of the heights of levels.
   /// </summary>
   public class LevelInfoCache
   {
      class DuplicateKeyComparer : IComparer<double>
      {
         public int Compare(double key1, double key2)
         {
            if (MathComparisonUtils.IsAlmostEqual(key1, key2))
            {
               if (key1 < key2)
                  return -1;
               else
                  return 1;
            }
            else
               return key1.CompareTo(key2);
         }
      }  
      
      /// <summary>
      /// The dictionary mapping from an ElementId to a level height.
      /// </summary>
      private IDictionary<long, KeyValuePair<ElementId, double>> m_ElementIdToLevelHeight = 
         new SortedDictionary<long, KeyValuePair<ElementId, double>>();

      /// <summary>
      /// A list of building storeys (that is, levels that are being exported), sorted by elevation.  
      /// The user is expected to create the list in the proper order; this is done in Exporter.cs.
      /// </summary>
      private SortedList<double, ElementId> m_BuildingStoriesByElevation = new SortedList<double, ElementId>(new DuplicateKeyComparer());

      /// <summary>
      /// A list of levels, sorted by elevation.  
      /// The user is expected to create the list in the proper order; this is done in Exporter.cs.
      /// </summary>
      private SortedList<double, ElementId> m_LevelsByElevation = new SortedList<double, ElementId>(new DuplicateKeyComparer());

      /// <summary>
      /// A set of IFC entities that should be associated to a level, but there is no level to associate them to.  These are buliding element related.
      /// </summary>
      private HashSet<IFCAnyHandle> m_OrphanedElements;

      /// <summary>
      /// A set of IFC entities that should be associated to a level, but there is no level to associate them to.  These are for spatial elements.
      /// </summary>
      private HashSet<IFCAnyHandle> m_OrphanedSpaces;

      /// <summary>
      /// The dictionary mapping from a SlabEdge.Id to a Floor.LevelId.
      /// </summary>
      private IDictionary<ElementId, ElementId> FloorSlabEdgeLevels = null;

      /// <summary>
      /// Finds the height of the level from the dictionary.
      /// </summary>
      /// <param name="elementId">The level element elementId.</param>
      /// <returns>The height.  Returns -1.0 if there is no entry in the cache, since valid entries must always be non-negative.</returns>
      public double FindHeight(ElementId elementId)
      {
         KeyValuePair<ElementId, double> info;
         if (m_ElementIdToLevelHeight.TryGetValue(elementId.Value, out info))
         {
            return info.Value;
         }
         return -1.0;
      }

      /// <summary>
      /// Finds the next level id, if any, of the level from the dictionary.
      /// </summary>
      /// <param name="elementId">The level element elementId.</param>
      /// <returns>The next level Id.  Returns InvalidElementId if there is no entry in the cache.</returns>
      public ElementId FindNextLevel(ElementId elementId)
      {
         KeyValuePair<ElementId, double> info;
         if (m_ElementIdToLevelHeight.TryGetValue(elementId.Value, out info))
         {
            return info.Key;
         }
         return ElementId.InvalidElementId;
      }

      /// <summary>
      /// Adds the height and next level id (if valid) to the dictionary.
      /// </summary>
      /// <param name="elementId">The level element elementId.</param>
      /// <param name="nextLevelId">The next level ElementId.</param>
      /// <param name="height">The height.</param>
      public void Register(ElementId elementId, ElementId nextLevelId, double height)
      {
         if (m_ElementIdToLevelHeight.ContainsKey(elementId.Value))
            return;

         m_ElementIdToLevelHeight[elementId.Value] = new KeyValuePair<ElementId, double>(nextLevelId, height);
      }

      /// <summary>
      /// A list of building storeys (that is, levels that are being exported), sorted by elevation.  
      /// The user is expected to create the list in the proper order; this is done in Exporter.cs.
      /// </summary>
      public IList<ElementId> BuildingStoriesByElevation
      {
         get
         {
            return m_BuildingStoriesByElevation.Where(k => k.Value != ElementId.InvalidElementId).Select(x => x.Value).ToList();
         }
      }

      /// <summary>
      /// A list of levels, sorted by elevation.  
      /// The user is expected to create the list in the proper order; this is done in Exporter.cs.
      /// </summary>
      public IList<ElementId> LevelsByElevation
      {
         get
         {
            return m_LevelsByElevation.Where(k => k.Value != ElementId.InvalidElementId).Select(x => x.Value).ToList();
         }
      }

      /// <summary>
      /// A set of IFC entities that should be associated to a level, but there is no level to associate them to.  These are buliding element related.
      /// </summary>
      public HashSet<IFCAnyHandle> OrphanedElements
      {
         get
         {
            if (m_OrphanedElements == null)
               m_OrphanedElements = new HashSet<IFCAnyHandle>();
            return m_OrphanedElements;
         }
      }

      /// <summary>
      /// A set of IFC entities that should be associated to a level, but there is no level to associate them to.  These are for spatial elements.
      /// </summary>
      public HashSet<IFCAnyHandle> OrphanedSpaces
      {
         get
         {
            if (m_OrphanedSpaces == null)
               m_OrphanedSpaces = new HashSet<IFCAnyHandle>();
            return m_OrphanedSpaces;
         }
      }

      /// <summary>
      /// Adds an IFCLevelInfo to the LevelsByElevation list, also updating the native cache item.
      /// </summary>
      /// <param name="exporterIFC">The exporter data object.</param>
      /// <param name="levelId">The level ElementId.</param>
      /// <param name="info">The IFCLevelInfo.</param>
      /// <param name="isBaseBuildingStorey">True if it is the levelId associated with the building storey.</param>
      public void AddLevelInfo(ExporterIFC exporterIFC, ElementId levelId, IFCLevelInfo info, bool isBaseBuildingStorey)
      {
         if (m_LevelsByElevation.Count == 0)
         {
            m_LevelsByElevation.Add(double.MinValue, ElementId.InvalidElementId);
            m_LevelsByElevation.Add(double.MaxValue, ElementId.InvalidElementId);
         }
         Level level = ExporterCacheManager.Document.GetElement(levelId) as Level;
         if (level != null)
            m_LevelsByElevation.Add(level.Elevation, levelId);
         else
            m_LevelsByElevation.Add(info.Elevation, levelId);

         if (isBaseBuildingStorey)
         {
            if (m_BuildingStoriesByElevation.Count == 0)
            {
               m_BuildingStoriesByElevation.Add(double.MinValue, ElementId.InvalidElementId);
               m_BuildingStoriesByElevation.Add(double.MaxValue, ElementId.InvalidElementId);
            }
            m_BuildingStoriesByElevation.Add(info.Elevation, levelId);
         }
         exporterIFC.AddBuildingStorey(levelId, info);
      }

      /// <summary>
      /// Clears all caches.
      /// </summary>
      /// <param name="exporterIFC">The Exporter object.</param>
      public void Clear(ExporterIFC exporterIFC)
      {
         // Clear data stored externally to LevelInfoCache.
         if (exporterIFC != null)
         {
            ISet<ElementId> uniqueLevels = LevelsByElevation?.ToHashSet();
            if ((uniqueLevels?.Count ?? 0) > 0)
            {
               foreach (ElementId levelId in uniqueLevels)
               {
                  exporterIFC.RemoveBuildingStorey(levelId);
               }
            }
         }

         // Revert all caches back to original state.
         m_ElementIdToLevelHeight?.Clear();
         m_BuildingStoriesByElevation?.Clear();
         m_LevelsByElevation?.Clear();
         m_OrphanedElements?.Clear();
         m_OrphanedSpaces?.Clear();
         FloorSlabEdgeLevels = null;
      }

      /// <summary>
      /// Get the IFCLevelInfo corresponding to a level.
      /// </summary>
      /// <param name="exporterIFC">The exporter data object.</param>
      /// <param name="levelId">The level ElementId.</param>
      /// <returns>The IFCLevelInfo.</returns>
      public IFCLevelInfo GetLevelInfo(ExporterIFC exporterIFC, ElementId levelId)
      {
         IFCLevelInfo levelInfo = null;
         if (!exporterIFC.GetLevelInfos().TryGetValue(levelId, out levelInfo))
            return null;
         return levelInfo;
      }

      /// <summary>
      /// Get the appropriate levelid for an object. This function will try to get the dependent objects to form
      ///   the overall bounding box to find the right level
      /// </summary>
      /// <param name="element">the element</param>
      /// <returns>return the level id</returns>
      public ElementId GetLevelIdOfObject(Element element)
      {
         ElementId levelId = ElementId.InvalidElementId;
         double lowestPosition = double.MaxValue;

         if (element.LevelId != ElementId.InvalidElementId)
            return element.LevelId;

         BoundingBoxXYZ bbox = element.get_BoundingBox(null);
         if (bbox == null)
         {
            Document doc = ExporterCacheManager.Document;
            IList<ElementId> dependentElements = element.GetDependentElements(null);
            if (dependentElements != null && dependentElements.Count > 0)
            {
               foreach (ElementId elemid in dependentElements)
               {
                  Element depElem = doc.GetElement(elemid);
                  BoundingBoxXYZ bboxElem = depElem.get_BoundingBox(null);
                  if (bboxElem != null && bboxElem.Min.Z < lowestPosition)
                     lowestPosition = bboxElem.Min.Z;
               }
            }
         }
         else
         {
            lowestPosition = bbox.Min.Z;
         }

         if (lowestPosition > double.MinValue)
         {
            for (int ii = 0; ii < m_BuildingStoriesByElevation.Count - 2; ++ii)
            {
               if (lowestPosition >= (m_BuildingStoriesByElevation.Keys[ii] - LevelUtil.LevelExtension) && lowestPosition < m_BuildingStoriesByElevation.Keys[ii + 1])
               {
                  if (ii == 0)
                     levelId = m_BuildingStoriesByElevation.Values[ii + 1];
                  else
                     levelId = m_BuildingStoriesByElevation.Values[ii];
                  break;
               }
            }
         }
         return levelId;
      }

      /// <summary>
      /// Get the appropriate level id for a slab edge.
      /// </summary>
      /// <param name="slabEdgeId">The slab edge element id.</param>
      /// <returns>The level id.</returns>
      public ElementId GetSlabEdgeLevelId(ElementId slabEdgeId)
      {
         if (FloorSlabEdgeLevels == null)
         {
            InitFloorSlabEdgeLevels(ExporterCacheManager.Document);
         }

         if (FloorSlabEdgeLevels.TryGetValue(slabEdgeId, out ElementId levelId))
         {
            return levelId;
         }

         return ElementId.InvalidElementId;
      }

      private void InitFloorSlabEdgeLevels(Document document)
      {
         FloorSlabEdgeLevels = new Dictionary<ElementId, ElementId>();

         ElementFilter floorFilter = new ElementClassFilter(typeof(Floor));
         FilteredElementCollector floorCollector = new FilteredElementCollector(document);
         floorCollector.WherePasses(floorFilter);
         IList<Element> floorElements = floorCollector.ToElements();
         if (floorElements?.Count > 0)
         {
            foreach (Element floor in floorElements)
            {
               IList<ElementId> dependentElementIds = floor.GetDependentElements(null);
               if (dependentElementIds?.Count > 0)
               {
                  var levelId = floor.LevelId;
                  foreach(ElementId dependentElementId in dependentElementIds)
                  {
                     if (dependentElementId == ElementId.InvalidElementId)
                        continue;
                     var dependentElement = document.GetElement(dependentElementId);
                     if (dependentElement is SlabEdge)
                     {
                        FloorSlabEdgeLevels[dependentElementId] = levelId;
                     }
                  }
               }
            }
         }
      }
   }

}