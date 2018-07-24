//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012-2016  Autodesk, Inc.
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
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export curtain systems.
   /// </summary>
   class CurtainSystemExporter
   {
      /// <summary>
      /// Exports curtain object as container.
      /// </summary>
      /// <param name="allSubElements">
      /// Collection of elements contained in the host curtain element.
      /// </param>
      /// <param name="wallElement">
      /// The curtain wall element.
      /// </param>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="productWrapper">
      /// The ProductWrapper.
      /// </param>
      public static void ExportCurtainObjectCommonAsContainer(ICollection<ElementId> allSubElements, Element wallElement,
         ExporterIFC exporterIFC, ProductWrapper origWrapper, PlacementSetter currSetter)
      {
         if (wallElement == null)
            return;

         string overrideCADLayer = null;
         ParameterUtil.GetStringValueFromElementOrSymbol(wallElement, "IFCCadLayer", out overrideCADLayer);

         using (ExporterStateManager.CADLayerOverrideSetter layerSetter = new ExporterStateManager.CADLayerOverrideSetter(overrideCADLayer))
         {
            HashSet<ElementId> alreadyVisited = new HashSet<ElementId>();  // just in case.
            Options geomOptions = GeometryUtil.GetIFCExportGeometryOptions();
            {
               foreach (ElementId subElemId in allSubElements)
               {
                  using (ProductWrapper productWrapper = ProductWrapper.Create(origWrapper))
                  {
                     Element subElem = wallElement.Document.GetElement(subElemId);
                     if (subElem == null)
                        continue;

                     if (alreadyVisited.Contains(subElem.Id))
                        continue;
                     alreadyVisited.Add(subElem.Id);

                     // Respect element visibility settings.
                     if (!ElementFilteringUtil.CanExportElement(exporterIFC, subElem, false) || !ElementFilteringUtil.IsElementVisible(subElem))
                        continue;

                     GeometryElement geomElem = subElem.get_Geometry(geomOptions);
                     if (geomElem == null)
                        continue;

                     try
                     {
                        if (subElem is FamilyInstance)
                        {
                           string ifcEnumType;
                           IFCExportInfoPair exportType = ExporterUtil.GetExportType(exporterIFC, subElem, out ifcEnumType);

                           if (subElem is Mullion)
                           {
                              if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
                                 ProxyElementExporter.Export(exporterIFC, subElem, geomElem, productWrapper, exportType);
                              else
                              {
                                 IFCAnyHandle currLocalPlacement = currSetter.LocalPlacement;

                                 if (exportType.ExportInstance == IFCEntityType.IfcCurtainWall)
                                 {
                                    // By default, panels and mullions are set to the same category as their parent.  In this case,
                                    // ask to get the exportType from the category id, since we don't want to inherit the parent class.
                                    exportType.SetValueWithPair(IFCEntityType.IfcMemberType);
                                    ifcEnumType = "MULLION";
                                 }

                                 FamilyInstanceExporter.ExportFamilyInstanceAsMappedItem(exporterIFC, subElem as Mullion, exportType, ifcEnumType, productWrapper,
                                     ElementId.InvalidElementId, null, currLocalPlacement);
                              }
                           }
                           else
                           {
                              FamilyInstance subFamInst = subElem as FamilyInstance;

                              if (exportType.ExportInstance == IFCEntityType.IfcCurtainWall)
                              {
                                 // By default, panels and mullions are set to the same category as their parent.  In this case,
                                 // ask to get the exportType from the category id, since we don't want to inherit the parent class.
                                 ElementId catId = CategoryUtil.GetSafeCategoryId(subElem);
                                 exportType = ElementFilteringUtil.GetExportTypeFromCategoryId(catId, out ifcEnumType);
                              }


                              if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
                              {
                                 if ((exportType.ExportInstance == IFCEntityType.UnKnown) || 
                                       (exportType.ExportInstance == IFCEntityType.IfcPlate) ||
                                       (exportType.ExportInstance == IFCEntityType.IfcMember))
                                    exportType.SetValueWithPair(IFCEntityType.IfcBuildingElementProxy);
                              }
                              else
                              {
                                 if (exportType.ExportInstance == IFCEntityType.UnKnown)
                                 {
                                    ifcEnumType = "CURTAIN_PANEL";
                                    exportType.SetValueWithPair(IFCEntityType.IfcPlateType);
                                 }
                              }

                              IFCAnyHandle currLocalPlacement = currSetter.LocalPlacement;
                              using (IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData())
                              {
                                 FamilyInstanceExporter.ExportFamilyInstanceAsMappedItem(exporterIFC, subFamInst, exportType, ifcEnumType, productWrapper,
                                     ElementId.InvalidElementId, null, currLocalPlacement);
                              }
                           }
                        }
                        else if (subElem is CurtainGridLine)
                        {
                           ProxyElementExporter.Export(exporterIFC, subElem, geomElem, productWrapper);
                        }
                        else if (subElem is Wall)
                        {
                           WallExporter.ExportWall(exporterIFC, null, subElem, null, geomElem, productWrapper);
                        }
                     }
                     catch (Exception ex)
                     {
                        if (ExporterUtil.IsFatalException(wallElement.Document, ex))
                           throw ex;
                        continue;
                     }
                  }
               }
            }
         }
      }

      /// <summary>
      /// Exports curtain object as one Brep.
      /// </summary>
      /// <param name="allSubElements">
      /// Collection of elements contained in the host curtain element.
      /// </param>
      /// <param name="wallElement">
      /// The curtain wall element.
      /// </param>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="setter">
      /// The PlacementSetter object.
      /// </param>
      /// <param name="localPlacement">
      /// The local placement handle.
      /// </param>
      /// <returns>
      /// The handle.
      /// </returns>
      public static IFCAnyHandle ExportCurtainObjectCommonAsOneBRep(ICollection<ElementId> allSubElements, Element wallElement,
         ExporterIFC exporterIFC, PlacementSetter setter, IFCAnyHandle localPlacement)
      {
         IFCAnyHandle prodDefRep = null;
         Document document = wallElement.Document;
         double eps = UnitUtil.ScaleLength(document.Application.VertexTolerance);

         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle contextOfItems = exporterIFC.Get3DContextHandle("Body");

         IFCGeometryInfo info = IFCGeometryInfo.CreateFaceGeometryInfo(eps);

         ISet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();

         // Want to make sure we don't accidentally add a mullion or curtain line more than once.
         HashSet<ElementId> alreadyVisited = new HashSet<ElementId>();
         bool useFallbackBREP = true;
         Options geomOptions = GeometryUtil.GetIFCExportGeometryOptions();

         foreach (ElementId subElemId in allSubElements)
         {
            Element subElem = wallElement.Document.GetElement(subElemId);
            GeometryElement geomElem = subElem.get_Geometry(geomOptions);
            if (geomElem == null)
               continue;

            if (alreadyVisited.Contains(subElem.Id))
               continue;
            alreadyVisited.Add(subElem.Id);


            // Export tessellated geometry when IFC4 Reference View is selected
            if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
            {
               BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(false, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
               IFCAnyHandle triFaceSet = BodyExporter.ExportBodyAsTessellatedFaceSet(exporterIFC, subElem, bodyExporterOptions, geomElem);
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(triFaceSet))
               {
                  bodyItems.Add(triFaceSet);
                  useFallbackBREP = false;    // no need to do Brep since it is successful
               }
            }
            // Export AdvancedFace before use fallback BREP
            else if (ExporterCacheManager.ExportOptionsCache.ExportAs4DesignTransferView)
            {
               BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(false, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
               IFCAnyHandle advancedBRep = BodyExporter.ExportBodyAsAdvancedBrep(exporterIFC, subElem, bodyExporterOptions, geomElem);
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(advancedBRep))
               {
                  bodyItems.Add(advancedBRep);
                  useFallbackBREP = false;    // no need to do Brep since it is successful
               }
            }

            if (useFallbackBREP)
            {
               ExporterIFCUtils.CollectGeometryInfo(exporterIFC, info, geomElem, XYZ.Zero, false);
               HashSet<IFCAnyHandle> faces = new HashSet<IFCAnyHandle>(info.GetSurfaces());
               IFCAnyHandle outer = IFCInstanceExporter.CreateClosedShell(file, faces);

               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(outer))
                  bodyItems.Add(RepresentationUtil.CreateFacetedBRep(exporterIFC, document, outer, ElementId.InvalidElementId));
            }
         }

         if (bodyItems.Count == 0)
            return prodDefRep;

         ElementId catId = CategoryUtil.GetSafeCategoryId(wallElement);
         IFCAnyHandle shapeRep;

         // Use tessellated geometry in Reference View
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView && !useFallbackBREP)
            shapeRep = RepresentationUtil.CreateTessellatedRep(exporterIFC, wallElement, catId, contextOfItems, bodyItems, null);
         else if (ExporterCacheManager.ExportOptionsCache.ExportAs4DesignTransferView && !useFallbackBREP)
            shapeRep = RepresentationUtil.CreateAdvancedBRepRep(exporterIFC, wallElement, catId, contextOfItems, bodyItems, null);
         else
            shapeRep = RepresentationUtil.CreateBRepRep(exporterIFC, wallElement, catId, contextOfItems, bodyItems);

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(shapeRep))
            return prodDefRep;

         IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
         shapeReps.Add(shapeRep);

         IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, wallElement.get_Geometry(geomOptions), Transform.Identity);
         if (boundingBoxRep != null)
            shapeReps.Add(boundingBoxRep);

         prodDefRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapeReps);
         return prodDefRep;
      }

      /// <summary>
      /// Checks if the curtain element can be exported as container.
      /// </summary>
      /// <remarks>
      /// It checks if all sub elements to be exported have geometries.
      /// </remarks>
      /// <param name="allSubElements">
      /// Collection of elements contained in the host curtain element.
      /// </param>
      /// <param name="document">
      /// The Revit document.
      /// </param>
      /// <returns>
      /// True if it can be exported as container, false otherwise.
      /// </returns>
      private static bool CanExportCurtainWallAsContainer(ICollection<ElementId> allSubElements, Document document)
      {
         Options geomOptions = GeometryUtil.GetIFCExportGeometryOptions();

         FilteredElementCollector collector = new FilteredElementCollector(document, allSubElements);

         List<Type> curtainWallSubElementTypes = new List<Type>();
         curtainWallSubElementTypes.Add(typeof(FamilyInstance));
         curtainWallSubElementTypes.Add(typeof(CurtainGridLine));
         curtainWallSubElementTypes.Add(typeof(Wall));

         ElementMulticlassFilter multiclassFilter = new ElementMulticlassFilter(curtainWallSubElementTypes, true);
         collector.WherePasses(multiclassFilter);
         ICollection<ElementId> filteredSubElemments = collector.ToElementIds();
         foreach (ElementId subElemId in filteredSubElemments)
         {
            Element subElem = document.GetElement(subElemId);
            GeometryElement geomElem = subElem.get_Geometry(geomOptions);
            if (geomElem == null)
               return false;
         }
         return true;
      }

      /// <summary>
      /// Export Curtain Walls and Roofs.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="allSubElements">Collection of elements contained in the host curtain element.</param>
      /// <param name="element">The element to be exported.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      private static void ExportBase(ExporterIFC exporterIFC, ICollection<ElementId> allSubElements, Element element, ProductWrapper wrapper)
      {
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcRoof;
         if (element is Wall || element is CurtainSystem || IsLegacyCurtainElement(element))
            elementClassTypeEnum = Common.Enums.IFCEntityType.IfcCurtainWall;
         else if (element is RoofBase)
            elementClassTypeEnum = Common.Enums.IFCEntityType.IfcRoof;

         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return;

         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

         PlacementSetter setter = null;

         using (ProductWrapper curtainWallSubWrapper = ProductWrapper.Create(wrapper, false))
         {
            try
            {
               Transform orientationTrf = Transform.Identity;
               IFCAnyHandle localPlacement = null;
               setter = PlacementSetter.Create(exporterIFC, element, null, orientationTrf);
               localPlacement = setter.LocalPlacement;

               string objectType = NamingUtil.CreateIFCObjectName(exporterIFC, element);

               IFCAnyHandle prodRepHnd = null;
               IFCAnyHandle elemHnd = null;
               string elemGUID = GUIDUtil.CreateGUID(element);
               if (element is Wall || element is CurtainSystem || IsLegacyCurtainElement(element))
               {
                  elemHnd = IFCInstanceExporter.CreateCurtainWall(exporterIFC, element, elemGUID, ownerHistory, localPlacement, prodRepHnd, null);
               }
               else if (element is RoofBase)
               {
                  //need to convert the string to enum
                  string ifcEnumType = ExporterUtil.GetIFCTypeFromExportTable(exporterIFC, element);
                  //ifcEnumType = IFCValidateEntry.GetValidIFCPredefinedType(element, ifcEnumType);
                  elemHnd = IFCInstanceExporter.CreateRoof(exporterIFC, element, elemGUID, ownerHistory, localPlacement, prodRepHnd, ifcEnumType);
               }
               else
               {
                  return;
               }

               if (IFCAnyHandleUtil.IsNullOrHasNoValue(elemHnd))
                  return;

               wrapper.AddElement(element, elemHnd, setter, null, true);

               bool canExportCurtainWallAsContainer = CanExportCurtainWallAsContainer(allSubElements, element.Document);
               IFCAnyHandle rep = null;
               if (!canExportCurtainWallAsContainer)
               {
                  rep = ExportCurtainObjectCommonAsOneBRep(allSubElements, element, exporterIFC, setter, localPlacement);
                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(rep))
                     return;
               }
               else
               {
                  ExportCurtainObjectCommonAsContainer(allSubElements, element, exporterIFC, curtainWallSubWrapper, setter);
               }

               ICollection<IFCAnyHandle> relatedElementIds = curtainWallSubWrapper.GetAllObjects();
               if (relatedElementIds.Count > 0)
               {
                  string guid = GUIDUtil.CreateSubElementGUID(element, (int)IFCCurtainWallSubElements.RelAggregates);
                  HashSet<IFCAnyHandle> relatedElementIdSet = new HashSet<IFCAnyHandle>(relatedElementIds);
                  IFCInstanceExporter.CreateRelAggregates(file, guid, ownerHistory, null, null, elemHnd, relatedElementIdSet);
               }

               ExportCurtainWallType(exporterIFC, wrapper, elemHnd, element);
               SpaceBoundingElementUtil.RegisterSpaceBoundingElementHandle(exporterIFC, elemHnd, element.Id, ElementId.InvalidElementId);
            }
            finally
            {
               if (setter != null)
                  setter.Dispose();
            }
         }
      }

      /// <summary>
      /// Returns all of the active curtain panels for a CurtainGrid.
      /// </summary>
      /// <param name="curtainGrid">The CurtainGrid element.</param>
      /// <returns>The element ids of the active curtain panels.</returns>
      /// <remarks>CurtainGrid.GetPanelIds() returns the element ids of the curtain panels that are directly contained in the CurtainGrid.
      /// Some of these panels however, are placeholders for "host" panels.  From a user point of view, the host panels are the real panels,
      /// and should replace these internal panels for export purposes.</remarks>
      public static ICollection<ElementId> GetVisiblePanelsForGrid(CurtainGrid curtainGrid)
      {
         ICollection<ElementId> panelIdsIn = curtainGrid.GetPanelIds();
         if (panelIdsIn == null)
            return null;

         HashSet<ElementId> visiblePanelIds = new HashSet<ElementId>();
         foreach (ElementId panelId in panelIdsIn)
         {
            Element element = ExporterCacheManager.Document.GetElement(panelId);
            if (element == null)
               continue;

            ElementId hostPanelId = ElementId.InvalidElementId;
            if (element is Panel)
               hostPanelId = (element as Panel).FindHostPanel();

            if (hostPanelId != ElementId.InvalidElementId)
            {
               // If the host panel is itself a curtain wall, then we have to recursively collect its element ids.
               Element hostPanel = ExporterCacheManager.Document.GetElement(hostPanelId);
               if (IsCurtainSystem(hostPanel))
               {
                  CurtainGridSet gridSet = CurtainSystemExporter.GetCurtainGridSet(hostPanel);
                  if (gridSet == null || gridSet.Size == 0)
                  {
                     visiblePanelIds.Add(hostPanelId);
                  }
                  else
                  {
                     ICollection<ElementId> allSubElements = GetSubElements(gridSet);
                     visiblePanelIds.UnionWith(allSubElements);
                  }
               }
               else
                  visiblePanelIds.Add(hostPanelId);
            }
            else
               visiblePanelIds.Add(panelId);
         }

         return visiblePanelIds;
      }

      private static ICollection<ElementId> GetSubElements(CurtainGridSet gridSet)
      {
         HashSet<ElementId> allSubElements = new HashSet<ElementId>();
         foreach (CurtainGrid grid in gridSet)
         {
            allSubElements.UnionWith(GetVisiblePanelsForGrid(grid));
            allSubElements.UnionWith(grid.GetMullionIds());
         }

         return allSubElements;
      }

      /// <summary>
      /// Export non-legacy Curtain Walls and Roofs.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="allSubElements">Collection of elements contained in the host curtain element.</param>
      /// <param name="element">The element to be exported.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      private static void ExportBaseWithGrids(ExporterIFC exporterIFC, Element hostElement, ProductWrapper productWrapper)
      {
         // Don't export the Curtain Wall itself, which has no useful geometry; instead export all of the GReps of the
         // mullions and panels.
         CurtainGridSet gridSet = CurtainSystemExporter.GetCurtainGridSet(hostElement);
         if (gridSet == null)
         {
            if (hostElement is Wall)
               ExportLegacyCurtainElement(exporterIFC, hostElement as Wall, productWrapper);
            return;
         }

         if (gridSet.Size == 0)
            return;

         ICollection<ElementId> allSubElements = GetSubElements(gridSet);
         ExportBase(exporterIFC, allSubElements, hostElement, productWrapper);
      }

      /// <summary>
      /// Exports a curtain wall to IFC curtain wall.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="hostElement">The host object element to be exported.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportWall(ExporterIFC exporterIFC, Wall hostElement, ProductWrapper productWrapper)
      {
         ExportBaseWithGrids(exporterIFC, hostElement, productWrapper);
      }

      /// <summary>
      /// Exports a curtain roof to IFC curtain wall.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="hostElement">The host object element to be exported.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportCurtainRoof(ExporterIFC exporterIFC, RoofBase hostElement, ProductWrapper productWrapper)
      {
         ExportBaseWithGrids(exporterIFC, hostElement, productWrapper);
      }

      /// <summary>
      /// Exports a curtain system to IFC curtain system.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="hostElement">The curtain system element to be exported.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportCurtainSystem(ExporterIFC exporterIFC, CurtainSystem curtainSystem, ProductWrapper productWrapper)
      {
         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcCurtainWall;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return;

         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            ExportBaseWithGrids(exporterIFC, curtainSystem, productWrapper);
            transaction.Commit();
         }
      }

      /// <summary>
      /// Exports a legacy curtain element to IFC curtain wall.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="curtainElement">The curtain element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportLegacyCurtainElement(ExporterIFC exporterIFC, Element curtainElement, ProductWrapper productWrapper)
      {
         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcCurtainWall;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return;

         ICollection<ElementId> allSubElements = ExporterIFCUtils.GetLegacyCurtainSubElements(curtainElement);

         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            ExportBase(exporterIFC, allSubElements, curtainElement, productWrapper);
            transaction.Commit();
         }
      }

      /// <summary>
      /// Checks if the element is legacy curtain element.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <returns>True if it is legacy curtain element.</returns>
      public static bool IsLegacyCurtainElement(Element element)
      {
         //for now, it is sufficient to check its category.
         return (CategoryUtil.GetSafeCategoryId(element) == new ElementId(BuiltInCategory.OST_Curtain_Systems));
      }

      /// <summary>
      /// Checks if the wall is legacy curtain wall.
      /// </summary>
      /// <param name="wall">The wall.</param>
      /// <returns>True if it is legacy curtain wall, false otherwise.</returns>
      public static bool IsLegacyCurtainWall(Wall wall)
      {
         try
         {
            CurtainGrid curtainGrid = wall.CurtainGrid;
            if (curtainGrid != null)
            {
               // The point of this code is to potentially throw an exception. If it does, we have a legacy curtain wall.
               curtainGrid.GetPanelIds();
            }
            else
               return false;
         }
         catch (Autodesk.Revit.Exceptions.InvalidOperationException ex)
         {
            if (ex.Message == "The host object is obsolete.")
               return true;
            else
               throw ex;
         }

         return false;
      }

      /// <summary>
      /// Returns if an element is a legacy or non-legacy curtain system of any base element type.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <returns>True if it is a legacy or non-legacy curtain system of any base element type, false otherwise.</returns>
      public static bool IsCurtainSystem(Element element)
      {
         if (element == null)
            return false;

         CurtainGridSet curtainGridSet = GetCurtainGridSet(element);
         if (curtainGridSet == null)
            return (element is Wall);
         return (curtainGridSet.Size > 0);
      }

      /// <summary>
      /// Provides a unified interface to get the curtain grids associated with an element.
      /// </summary>
      /// <param name="element">The host element.</param>
      /// <returns>A CurtainGridSet with 0 or more CurtainGrids, or null if invalid.</returns>
      public static CurtainGridSet GetCurtainGridSet(Element element)
      {
         CurtainGridSet curtainGridSet = null;
         if (element is Wall)
         {
            Wall wall = element as Wall;
            if (!CurtainSystemExporter.IsLegacyCurtainWall(wall))
            {
               CurtainGrid curtainGrid = wall.CurtainGrid;
               curtainGridSet = new CurtainGridSet();
               if (curtainGrid != null)
                  curtainGridSet.Insert(curtainGrid);
            }
         }
         else if (element is FootPrintRoof)
         {
            FootPrintRoof footPrintRoof = element as FootPrintRoof;
            curtainGridSet = footPrintRoof.CurtainGrids;
         }
         else if (element is ExtrusionRoof)
         {
            ExtrusionRoof extrusionRoof = element as ExtrusionRoof;
            curtainGridSet = extrusionRoof.CurtainGrids;
         }
         else if (element is CurtainSystem)
         {
            CurtainSystem curtainSystem = element as CurtainSystem;
            curtainGridSet = curtainSystem.CurtainGrids;
         }

         return curtainGridSet;
      }

      /// <summary>
      /// Exports curtain wall types to IfcCurtainWallType.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="wrapper">The ProductWrapper class.</param>
      /// <param name="elementHandle">The element handle.</param>
      /// <param name="element">The element.</param>
      public static void ExportCurtainWallType(ExporterIFC exporterIFC, ProductWrapper wrapper, IFCAnyHandle elementHandle, Element element)
      {
         if (elementHandle == null || element == null)
            return;

         Document doc = element.Document;
         ElementId typeElemId = element.GetTypeId();
         ElementType elementType = doc.GetElement(typeElemId) as ElementType;
         if (elementType == null)
            return;

         IFCExportInfoPair exportType = new IFCExportInfoPair();
         exportType.SetValueWithPair(IFCEntityType.IfcCurtainWallType);
         IFCAnyHandle wallType = ExporterCacheManager.ElementTypeToHandleCache.Find(elementType, exportType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(wallType))
         {
            ExporterCacheManager.TypeRelationsCache.Add(wallType, elementHandle);
            return;
         }

         string elemName = NamingUtil.GetNameOverride(elementType, NamingUtil.GetIFCName(elementType));
         string elemElementType = NamingUtil.GetOverrideStringValue(elementType, "IfcElementType", null);

         // Property sets will be set later.
         wallType = IFCInstanceExporter.CreateCurtainWallType(exporterIFC.GetFile(), elementType,
             null, null, elemElementType, (elemElementType != null) ? "USERDEFINED" : "NOTDEFINED");

         wrapper.RegisterHandleWithElementType(elementType, exportType, wallType, null);

         ExporterCacheManager.TypeRelationsCache.Add(wallType, elementHandle);
      }
   }
}