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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// A multi-purpose wrapper used within the export of an element to:
   /// 1. Associate IFC handles to their container (e.g., levels, buildings)
   /// 2. Create properties and quantities for top-level handles.
   /// </summary>
   /// <remarks>The intention of this class is to phase out the use of IFCProductWrapper.  As long as handles are created in native code,
   /// IFCProductWrapper will be necessary, but it should be used for as little as possible.  Note that items added directly to the
   /// ProductWrapper and not the internal IFCProductWrapper will not show up on entity counters in journal files.</remarks>
   public class ProductWrapper :IDisposable
   {
      HashSet<IFCAnyHandle> CreatedHandles { get; set; } = new HashSet<IFCAnyHandle>();

      IDictionary<Element, HashSet<IFCAnyHandle>> PropertySetsToCreate { get; set; } = new Dictionary<Element, HashSet<IFCAnyHandle>>();

      Dictionary<Tuple<ElementType, IFCExportInfoPair>, KeyValuePair<IFCAnyHandle, HashSet<IFCAnyHandle>>>
         ElementTypeHandles { get; set; } = new Dictionary<Tuple<ElementType, IFCExportInfoPair>, KeyValuePair<IFCAnyHandle, HashSet<IFCAnyHandle>>>();


      IFCProductWrapper InternalWrapper { get; set; } = null;

      ProductWrapper ParentWrapper { get; set; } = null;

      ExporterIFC ExporterIFC { get; set; } = null;

      protected ProductWrapper(ExporterIFC exporterIFC)
      {
         ExporterIFC = exporterIFC;
      }

      private void RegisterHandleWithElement(Element element, IFCAnyHandle handle, 
         IFCExportInfoPair exportType = null)
      {
         if (element == null || IFCAnyHandleUtil.IsNullOrHasNoValue(handle))
            return;
         
         if (!PropertySetsToCreate.TryGetValue(element, out HashSet<IFCAnyHandle> propertySetToCreate))
         {
            propertySetToCreate = new HashSet<IFCAnyHandle>();
            PropertySetsToCreate[element] = propertySetToCreate;
         }
         propertySetToCreate.Add(handle);

         ExporterCacheManager.ElementToHandleCache.Register(element.Id, handle, exportType);
         ExporterCacheManager.HandleToElementCache.Register(handle, element.Id);
      }

      /// <summary>
      /// Register an ElementType with the ProductWrapper, to create its property sets on Dispose.
      /// </summary>
      /// <param name="elementType">The element type.</param>
      /// <param name="prodTypeHnd">The handle.</param>
      /// <param name="existingPropertySets">Any existing propertysets.</param>
      public void RegisterHandleWithElementType(ElementType elementType, IFCExportInfoPair exportType, 
         IFCAnyHandle prodTypeHnd, HashSet<IFCAnyHandle> existingPropertySets)
      {
         Tuple<ElementType, IFCExportInfoPair> elTypeKey = new Tuple<ElementType, IFCExportInfoPair>(elementType, exportType);
         if (elTypeKey.Item1 == null || IFCAnyHandleUtil.IsNullOrHasNoValue(prodTypeHnd))
            return;

         KeyValuePair<IFCAnyHandle, HashSet<IFCAnyHandle>> elementTypeHandle;
         if (ElementTypeHandles.TryGetValue(elTypeKey, out elementTypeHandle))
            return;
      
         ElementTypeHandles[elTypeKey] = new KeyValuePair<IFCAnyHandle, HashSet<IFCAnyHandle>>(prodTypeHnd, existingPropertySets);

         // In addition, add it to the ElementTypeToHandleCache.
         ExporterCacheManager.ElementTypeToHandleCache.Register(elementType, exportType, prodTypeHnd);
      }

      /// <summary>
      /// Standard static Create function. 
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="allowRelateToLevel">Whether or not handles are allowed to be related to levels.</param>
      /// <returns>A new ProductWrapper.</returns>
      public static ProductWrapper Create(ExporterIFC exporterIFC, bool allowRelateToLevel)
      {
         ProductWrapper productWrapper = new ProductWrapper(exporterIFC);
         productWrapper.InternalWrapper = IFCProductWrapper.Create(exporterIFC, allowRelateToLevel);
         return productWrapper;
      }

      /// <summary>
      /// Static Create function for a child wrapper.
      /// </summary>
      /// <param name="parentWrapper">The parent wrapper.</param>
      /// <returns>A new ProductWrapper.</returns>
      public static ProductWrapper Create(ProductWrapper parentWrapper)
      {
         ProductWrapper productWrapper = new ProductWrapper(parentWrapper.ExporterIFC);
         productWrapper.InternalWrapper = IFCProductWrapper.Create(parentWrapper.InternalWrapper);
         productWrapper.ParentWrapper = parentWrapper;
         return productWrapper;
      }

      /// <summary>
      /// Static Create function for a child wrapper, with a different allowRelateToLevel value.
      /// </summary>
      /// <param name="parentWrapper">The parent wrapper.</param>
      /// <param name="allowRelateToLevel">Whether or not handles are allowed to be related to levels.</param>
      /// <returns>A new ProductWrapper.</returns>
      public static ProductWrapper Create(ProductWrapper parentWrapper, bool allowRelateToLevel)
      {
         ProductWrapper productWrapper = new ProductWrapper(parentWrapper.ExporterIFC);
         productWrapper.InternalWrapper = IFCProductWrapper.Create(parentWrapper.InternalWrapper, allowRelateToLevel);
         productWrapper.ParentWrapper = parentWrapper;
         return productWrapper;
      }

      /// <summary>
      /// Returns the internal IFCProductWrapper, for use as arguments to native functions.
      /// </summary>
      /// <returns>The internal IFCProductWrapper.</returns>
      public IFCProductWrapper ToNative()
      {
         return InternalWrapper;
      }

      /// <summary>
      /// Gets an arbitrary handle from the wrapper, if one exists.
      /// </summary>
      /// <returns>The handle, or null if no handle exists.</returns>
      /// <remarks>Generally intended for when there is only one handle in the wrapper.</remarks>
      public IFCAnyHandle GetAnElement()
      {
         if (CreatedHandles.Count > 0)
         {
            foreach (IFCAnyHandle firstHandle in CreatedHandles)
               return firstHandle;
         }

         return InternalWrapper.GetAnElement();
      }

      /// <summary>
      /// Determines whether there are any handles associated with the wrapper.
      /// </summary>
      /// <returns>True if it is empty, false otherwise.</returns>
      public bool IsEmpty()
      {
         return ((CreatedHandles.Count == 0) && (InternalWrapper.Count == 0));
      }

      /// <summary>
      /// Gets the first handle of a particular type, or null if none exists.
      /// </summary>
      /// <param name="type">The entity type.</param>
      /// <returns>The handle, or null.</returns>
      public IFCAnyHandle GetElementOfType(IFCEntityType type)
      {
         foreach (IFCAnyHandle handle in CreatedHandles)
         {
            if (IFCAnyHandleUtil.IsSubTypeOf(handle, type))
               return handle;
         }

         ICollection<IFCAnyHandle> internalObjects = InternalWrapper.GetAllObjects();
         foreach (IFCAnyHandle handle in internalObjects)
         {
            if (IFCAnyHandleUtil.IsSubTypeOf(handle, type))
               return handle;
         }

         return null;
      }

      /// <summary>
      /// Get all handles in the wrapper.
      /// </summary>
      /// <returns>The collection of handles.</returns>
      public ISet<IFCAnyHandle> GetAllObjects()
      {
         ICollection<IFCAnyHandle> internalObjects = InternalWrapper.GetAllObjects();
         if (internalObjects.Count == 0)
            return CreatedHandles;

         HashSet<IFCAnyHandle> allObjects = new HashSet<IFCAnyHandle>();

         // We aren't going to trust that the handles aren't stale.  This needs a rewrite
         // of disposal of entities, and in general a move to .NET only created entities.
         foreach (IFCAnyHandle internalObject in internalObjects)
         {
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(internalObject))
               allObjects.Add(internalObject);
         }

         allObjects.UnionWith(CreatedHandles);
         return allObjects;
      }

      /// <summary>
      /// Add a generic element to the wrapper, and create associated internal property sets if option is set.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="handle">The handle.</param>
      public void AddElement(Element element, IFCAnyHandle handle, IFCExportInfoPair exportType, bool register = true)
      {
         CreatedHandles.Add(handle);
         if (register)
            RegisterHandleWithElement(element, handle, exportType);
      }

      /// <summary>
      /// Add a generic element to the wrapper, with associated setter and extrusion data information, and create associated internal property sets if option is set.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="handle">The element handle.</param>
      /// <param name="setter">The placement setter.</param>
      /// <param name="data">The extrusion creation data (can be null.)</param>
      /// <param name="relateToLevel">Relate to the level in the setter, or not.</param>
      public void AddElement(Element element, IFCAnyHandle handle, PlacementSetter setter, IFCExportBodyParams data, bool relateToLevel, 
         IFCExportInfoPair exportType, bool register = true)
      {
         // There is a bug in the internal AddElement that requires us to do a levelInfo null check here.
         IFCLevelInfo levelInfo = setter.LevelInfo;
         bool actuallyRelateToLevel = relateToLevel && (levelInfo != null);
         InternalWrapper.AddElement(handle, levelInfo, data?.Data, actuallyRelateToLevel);
         if (levelInfo == null && relateToLevel)
            ExporterCacheManager.LevelInfoCache.OrphanedElements.Add(handle);
         if (register)
            RegisterHandleWithElement(element, handle, exportType);
      }

      /// <summary>
      /// Add a generic element to the wrapper, with associated level and extrusion data information, and create associated internal property sets if option is set.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="handle">The element handle.</param>
      /// <param name="levelInfo">The level information.</param>
      /// <param name="data">The extrusion creation data (can be null.)</param>
      /// <param name="relateToLevel">Relate to the level in the setter, or not.</param>
      public void AddElement(Element element, IFCAnyHandle handle, IFCLevelInfo levelInfo, IFCExportBodyParams data, bool relateToLevel, 
         IFCExportInfoPair exportType, bool register = true)
      {
         // There is a bug in the internal AddElement that requires us to do a levelInfo null check here.
         bool actuallyRelateToLevel = relateToLevel && (levelInfo != null);
         InternalWrapper.AddElement(handle, levelInfo, data?.Data, actuallyRelateToLevel);
         if (levelInfo == null && relateToLevel)
            ExporterCacheManager.LevelInfoCache.OrphanedElements.Add(handle);
         if (register)
            RegisterHandleWithElement(element, handle, exportType);
      }

      /// <summary>
      /// Add a space to the wrapper, with associated level and extrusion data information.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="handle">The element handle.</param>
      /// <param name="levelInfo">The level information.</param>
      /// <param name="data">The extrusion creation data (can be null.)</param>
      /// <param name="relateToLevel">Relate to the level in the setter, or not.</param>
      public void AddSpace(Element element, IFCAnyHandle handle, IFCLevelInfo levelInfo, IFCExportBodyParams data, bool relateToLevel, IFCExportInfoPair exportType)
      {
         bool actuallyRelateToLevel = relateToLevel && (levelInfo != null);
         InternalWrapper.AddSpace(handle, levelInfo, data?.Data, actuallyRelateToLevel);
         if (levelInfo == null && relateToLevel)
            ExporterCacheManager.LevelInfoCache.OrphanedSpaces.Add(handle);
         RegisterHandleWithElement(element, handle, exportType);
      }

      /// <summary>
      /// Adds an annotation handle to associate with the IfcProduct in this wrapper.
      /// </summary>
      /// <param name="handle">The annotation handle.</param>
      /// <param name="levelInfo">The level information, can be null if relateToLevel is false.</param>
      /// <param name="relateToLevel">Whether the annotation is contained in a level.</param>
      public void AddAnnotation(IFCAnyHandle handle, IFCLevelInfo levelInfo, bool relateToLevel)
      {
         // The internal AddAnnotation takes an optional levelInfo, so we don't need to do a levelInfo null check here.
         InternalWrapper.AddAnnotation(handle, levelInfo, relateToLevel);
      }

      /// <summary>
      /// Adds a building handle to this wrapper.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="handle">The building handle.</param>
      public void AddBuilding(Element element, IFCAnyHandle handle)
      {
         RegisterHandleWithElement(element, handle);
         InternalWrapper.AddBuilding(handle);
      }

      /// <summary>
      /// Adds a system handle to this wrapper.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="handle">The building handle.</param>
      public void AddSystem(Element element, IFCAnyHandle handle)
      {
         RegisterHandleWithElement(element, handle);
         CreatedHandles.Add(handle);
      }

      /// <summary>
      /// Adds a site (IfcObject) handle to associate with the IfcProduct in this wrapper.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="handle">The site handle.</param>
      public void AddSite(Element element, IFCAnyHandle handle)
      {
         RegisterHandleWithElement(element, handle);
         InternalWrapper.AddSite(handle);
      }

      public void AddProject(Element element, IFCAnyHandle handle)
      {
         CreatedHandles.Add(handle);
      }
      /// <summary>
      /// Adds a material handle to associate with the IfcProduct in this wrapper.
      /// </summary>
      /// <param name="materialHnd"></param>
      public void AddFinishMaterial(IFCAnyHandle materialHnd)
      {
         InternalWrapper.AddFinishMaterial(materialHnd);
      }

      /// <summary>
      /// Clear finish materials in this wrapper.
      /// </summary>
      public void ClearFinishMaterials()
      {
         InternalWrapper.ClearFinishMaterials();
      }

      /// <summary>
      /// Gets the extrusion creation data associated with a handle.
      /// </summary>
      /// <param name="handle">The handle.</param>
      /// <returns>The extrusion creation data, or null.</returns>
      public IFCExportBodyParams FindExtrusionCreationParameters(IFCAnyHandle handle)
      {
         return new IFCExportBodyParams(InternalWrapper.FindExtrusionCreationParameters(handle));
      }

      /// <summary>
      /// The Dispose function, to do bookkeeping at end of "using" block.
      /// </summary>
      public void Dispose()
      {
         // If we have a parent wrapper, postpone creating property set so that we
         // don't create the same property sets multiple times.
         if (ParentWrapper != null)
         {
            foreach (var propertySetToCreate in PropertySetsToCreate)
            {
               if (!ParentWrapper.PropertySetsToCreate.TryGetValue(propertySetToCreate.Key, 
                  out HashSet<IFCAnyHandle> handles))
               {
                  handles = new HashSet<IFCAnyHandle>();
                  ParentWrapper.PropertySetsToCreate.Add(propertySetToCreate.Key, handles);
               }
               handles.UnionWith(propertySetToCreate.Value);
            }

            ParentWrapper.CreatedHandles.UnionWith(CreatedHandles);

            foreach (var elementTypeHandle in ElementTypeHandles)
            {
               if (ParentWrapper.ElementTypeHandles.ContainsKey(elementTypeHandle.Key))
                  continue;
               
               ParentWrapper.ElementTypeHandles[elementTypeHandle.Key] = elementTypeHandle.Value;
            }
         }
         else
         {
            foreach (var propertySetToCreate in PropertySetsToCreate)
            {
               PropertyUtil.CreateInternalRevitPropertySets(ExporterIFC, propertySetToCreate.Key, propertySetToCreate.Value);
            }

            foreach (var elementTypeHandle in ElementTypeHandles)
            {
               PropertyUtil.CreateElementTypeProperties(ExporterIFC, elementTypeHandle.Key.Item1, elementTypeHandle.Value.Value, elementTypeHandle.Value.Key);
            }
         }

         InternalWrapper.Dispose();
      }

      private ProductWrapper()
      {
      }

      /// <summary>
      /// Clear propertyset assignments to the internal Handle stored in the wrapper if it is incomplete
      /// </summary>
      /// <param name="element">the element</param>
      public void ClearInternalHandleWrapperData(Element element)
      {
         if (InternalWrapper.Count > 0)
         {
            HashSet<IFCAnyHandle> propertySetToCreate = null;
            if (PropertySetsToCreate.TryGetValue(element, out propertySetToCreate))
            {
               ICollection<IFCAnyHandle> internalObjects = InternalWrapper.GetAllObjects();
               foreach (IFCAnyHandle internalObj in internalObjects)
               {
                  propertySetToCreate.Remove(internalObj);
               }
            }
         }
      }
   }
}