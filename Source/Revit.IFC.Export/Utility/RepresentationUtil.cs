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
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Provides static methods to create varies IFC representations.
   /// </summary>
   public class RepresentationUtil
   {
      /// <summary>
      /// Creates a shape representation and register it to shape representation layer.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
      /// <param name="identifier">The identifier for the representation.</param>
      /// <param name="representationType">The type handle for the representation.</param>
      /// <param name="items">Collection of geometric representation items that are defined for this representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateBaseShapeRepresentation(ExporterIFC exporterIFC, IFCAnyHandle contextOfItems,
         string identifier, string representationType, ISet<IFCAnyHandle> items)
      {
         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle newShapeRepresentation = IFCInstanceExporter.CreateShapeRepresentation(file, contextOfItems, identifier, representationType, items);
         return newShapeRepresentation;
      }

      /// <summary>
      /// Creates a shape representation or appends existing ones to original representation.
      /// </summary>
      /// <remarks>
      /// This function has two modes. 
      /// If originalShapeRepresentation has no value, then this function will create a new ShapeRepresentation handle. 
      /// If originalShapeRepresentation has a value, then it is expected to be an aggregation of representations, and the new representation
      /// will be appended to the end of the list.
      /// </remarks>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="categoryId">The category id.</param>
      /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
      /// <param name="identifierOpt">The identifier for the representation.</param>
      /// <param name="representationTypeOpt">The type handle for the representation.</param>
      /// <param name="items">Collection of geometric representation items that are defined for this representation.</param>
      /// <param name="originalShapeRepresentation">The original shape representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateOrAppendShapeRepresentation(ExporterIFC exporterIFC, Element element, ElementId categoryId, IFCAnyHandle contextOfItems,
         string identifierOpt, string representationTypeOpt, ISet<IFCAnyHandle> items, IFCAnyHandle originalShapeRepresentation)
      {
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(originalShapeRepresentation))
         {
            GeometryUtil.AddItemsToShape(originalShapeRepresentation, items);
            return originalShapeRepresentation;
         }

         return CreateShapeRepresentation(exporterIFC, element, categoryId, contextOfItems, identifierOpt, representationTypeOpt, items);
      }

      /// <summary>
      /// Determine if there is a per-element presentation layer override for a representation handle.
      /// </summary>
      /// <param name="element">The element associated with the representation handle.</param>
      /// <returns>The name of the layer, or null if there is no override.</returns>
      public static string GetPresentationLayerOverride(Element element)
      {
         // Search for old "IFCCadLayer" or new "IfcPresentationLayer".
         string ifcCADLayer = null;
         if ((ParameterUtil.GetStringValueFromElementOrSymbol(element, "IFCCadLayer", out ifcCADLayer) == null) ||
             string.IsNullOrWhiteSpace(ifcCADLayer))
         {
            if ((ParameterUtil.GetStringValueFromElementOrSymbol(element, "IfcPresentationLayer", out ifcCADLayer) == null) ||
                string.IsNullOrWhiteSpace(ifcCADLayer))
            {
               ifcCADLayer = ExporterStateManager.GetCurrentCADLayerOverride();
            }
         }
         return ifcCADLayer;
      }

      /// <summary>
      /// Creates a shape representation and register it to shape representation layer.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="categoryId">The category id.</param>
      /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
      /// <param name="identifier">The identifier for the representation.</param>
      /// <param name="representationType">The type handle for the representation.</param>
      /// <param name="items">Collection of geometric representation items that are defined for this representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateShapeRepresentation(ExporterIFC exporterIFC, Element element, ElementId categoryId, IFCAnyHandle contextOfItems,
         string identifier, string representationType, ISet<IFCAnyHandle> items)
      {
         IFCAnyHandle newShapeRepresentation = CreateBaseShapeRepresentation(exporterIFC, contextOfItems, identifier, representationType, items);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(newShapeRepresentation) &&
            !ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
         {
            string ifcCADLayer = GetPresentationLayerOverride(element);

            // We are using the DWG export layer table to correctly map category to DWG layer for the 
            // IfcPresentationLayerAsssignment, if it is not overridden.
            if (string.IsNullOrWhiteSpace(ifcCADLayer))
            {
               ifcCADLayer = GetPresentationLayerOverride(element);
            }

            if (!string.IsNullOrWhiteSpace(ifcCADLayer))
            {
               ExporterCacheManager.PresentationLayerSetCache.AddRepresentationToLayer(ifcCADLayer, newShapeRepresentation);
            }
            else
               exporterIFC.RegisterShapeForPresentationLayer(element, categoryId, newShapeRepresentation);
         }

         return newShapeRepresentation;
      }

      /// <summary>
      /// Creates a shape representation and register it to shape representation layer.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
      /// <param name="identifier">The identifier for the representation.</param>
      /// <param name="representationType">The type handle for the representation.</param>
      /// <param name="items">Collection of geometric representation items that are defined for this representation.</param>
      /// <param name="ifcCADLayer">The IFC CAD layer name.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateShapeRepresentation(ExporterIFC exporterIFC, IFCAnyHandle contextOfItems,
         string identifier, string representationType, ISet<IFCAnyHandle> items, string ifcCADLayer)
      {
         IFCAnyHandle newShapeRepresentation = CreateBaseShapeRepresentation(exporterIFC, contextOfItems, identifier, representationType, items);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(newShapeRepresentation))
         {
            if (!string.IsNullOrWhiteSpace(ifcCADLayer))
            {
               ExporterCacheManager.PresentationLayerSetCache.AddRepresentationToLayer(ifcCADLayer, newShapeRepresentation);
            }
         }

         return newShapeRepresentation;
      }

      /// <summary>
      /// Delete a Shape Representation. We will also delete it from a PresentationLayerSetCache if it is registered in there during the creation to remove an invalid item
      /// </summary>
      /// <param name="handleToDelete">handle to delete</param>
      public static void DeleteShapeRepresentation(IFCAnyHandle handleToDelete)
      {
         // As the shape representation might be already asigned to Presentation Layer (and in the cache), we need to remove it from there
         foreach (KeyValuePair<string, ICollection<IFCAnyHandle>> presentationLayerSet in ExporterCacheManager.PresentationLayerSetCache)
         {
            if (presentationLayerSet.Value.Contains(handleToDelete))
               presentationLayerSet.Value.Remove(handleToDelete);
         }
         handleToDelete.Delete();
      }

      /// <summary>
      /// Creates a shape representation and register it to shape representation layer.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="categoryId">The category id.</param>
      /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
      /// <param name="identifierOpt">The identifier for the representation.</param>
      /// <param name="representationTypeOpt">The type handle for the representation.</param>
      /// <param name="items">List of geometric representation items that are defined for this representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateShapeRepresentation(ExporterIFC exporterIFC, Element element, ElementId categoryId, IFCAnyHandle contextOfItems,
         string identifierOpt, string representationTypeOpt, IList<IFCAnyHandle> items)
      {
         HashSet<IFCAnyHandle> itemSet = new HashSet<IFCAnyHandle>();
         foreach (IFCAnyHandle axisItem in items)
            itemSet.Add(axisItem);
         return CreateShapeRepresentation(exporterIFC, element, categoryId, contextOfItems, identifierOpt, representationTypeOpt, itemSet);
      }

      /// <summary>
      /// Creates an IfcFacetedBrep handle.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="shell">The closed shell handle.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateFacetedBRep(ExporterIFC exporterIFC, Document document, IFCAnyHandle shell, ElementId overrideMaterialId)
      {
         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle brep = IFCInstanceExporter.CreateFacetedBrep(file, shell);
         BodyExporter.CreateSurfaceStyleForRepItem(exporterIFC, document, brep, overrideMaterialId);
         return brep;
      }

      /// <summary>
      /// Create tessellated body representation 
      /// </summary>
      /// <param name="exporterIFC">the ExporterIFC object</param>
      /// <param name="element">the Element</param>
      /// <param name="categoryId">The Category Id</param>
      /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
      /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
      /// <param name="originalRepresentation">The original shape representation.</param>
      /// <returns></returns>
      public static IFCAnyHandle CreateTessellatedRep(ExporterIFC exporterIFC, Element element, ElementId categoryId, IFCAnyHandle contextOfItems,
          ISet<IFCAnyHandle> bodyItems, IFCAnyHandle originalRepresentation)
      {
         // Currently set to "Body" as shown also in the IFC documentation example. But there is also "Body-Fallback" for tessellated geometry.
         string identifierOpt = "Body";
         string repTypeOpt = ShapeRepresentationType.Tessellation.ToString();
         IFCAnyHandle bodyRepresentation =
             CreateOrAppendShapeRepresentation(exporterIFC, element, categoryId, contextOfItems, identifierOpt, repTypeOpt,
                 bodyItems, originalRepresentation);
         return bodyRepresentation;
      }

      /// <summary>
      /// Create advanced Brep body representation 
      /// </summary>
      /// <param name="exporterIFC">the ExporterIFC object</param>
      /// <param name="element">the Element</param>
      /// <param name="categoryId">The Category Id</param>
      /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
      /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
      /// <param name="originalRepresentation">The original shape representation.</param>
      /// <returns></returns>
      public static IFCAnyHandle CreateAdvancedBRepRep(ExporterIFC exporterIFC, Element element, ElementId categoryId, IFCAnyHandle contextOfItems,
          ISet<IFCAnyHandle> bodyItems, IFCAnyHandle originalRepresentation)
      {
         string identifierOpt = "Body";
         string repTypeOpt = ShapeRepresentationType.AdvancedBrep.ToString();
         IFCAnyHandle bodyRepresentation =
             CreateOrAppendShapeRepresentation(exporterIFC, element, categoryId, contextOfItems, identifierOpt, repTypeOpt,
                 bodyItems, originalRepresentation);
         return bodyRepresentation;
      }

      /// <summary>
      /// Creates a sweep solid representation.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="categoryId">The category id.</param>
      /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
      /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
      /// <param name="originalShapeRepresentation">The original shape representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSweptSolidRep(ExporterIFC exporterIFC, Element element, ElementId categoryId, IFCAnyHandle contextOfItems,
          ISet<IFCAnyHandle> bodyItems, IFCAnyHandle originalRepresentation)
      {
         string identifierOpt = "Body";   // this is by IFC2x2 convention, not temporary
         string repTypeOpt = ShapeRepresentationType.SweptSolid.ToString();  // this is by IFC2x2 convention, not temporary
         IFCAnyHandle bodyRepresentation =
            CreateOrAppendShapeRepresentation(exporterIFC, element, categoryId, contextOfItems, identifierOpt, repTypeOpt,
               bodyItems, originalRepresentation);
         return bodyRepresentation;
      }

      /// <summary>
      /// Creates an advanced sweep solid representation.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="categoryId">The category id.</param>
      /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
      /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
      /// <param name="originalShapeRepresentation">The original shape representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateAdvancedSweptSolidRep(ExporterIFC exporterIFC, Element element, ElementId categoryId, IFCAnyHandle contextOfItems,
          ISet<IFCAnyHandle> bodyItems, IFCAnyHandle originalRepresentation)
      {
         string identifierOpt = "Body";   // this is by IFC2x2 convention, not temporary
         string repTypeOpt = ShapeRepresentationType.AdvancedSweptSolid.ToString();  // this is by IFC2x2 convention, not temporary
         IFCAnyHandle bodyRepresentation =
            CreateOrAppendShapeRepresentation(exporterIFC, element, categoryId, contextOfItems, identifierOpt, repTypeOpt,
               bodyItems, originalRepresentation);
         return bodyRepresentation;
      }

      /// <summary>
      /// Creates a clipping representation.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="categoryId">The category id.</param>
      /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
      /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateClippingRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
         IFCAnyHandle contextOfItems, HashSet<IFCAnyHandle> bodyItems)
      {
         string identifierOpt = "Body";   // this is by IFC2x2 convention, not temporary
         string repTypeOpt = ShapeRepresentationType.Clipping.ToString();  // this is by IFC2x2 convention, not temporary
         IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, element, categoryId,
            contextOfItems, identifierOpt, repTypeOpt, bodyItems);
         return bodyRepresentation;
      }

      /// <summary>
      /// Creates a CSG representation which contains the result of boolean operations between solid models, half spaces, and other boolean operations.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="categoryId">The category id.</param>
      /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
      /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateCSGRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
         IFCAnyHandle contextOfItems, ISet<IFCAnyHandle> bodyItems)
      {
         string identifierOpt = "Body";   // this is by IFC2x2 convention, not temporary
         string repTypeOpt = ShapeRepresentationType.CSG.ToString();  // this is by IFC2x2 convention, not temporary
         IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, element, categoryId,
            contextOfItems, identifierOpt, repTypeOpt, bodyItems);
         return bodyRepresentation;
      }

      /// <summary>
      /// Creates a Brep representation.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="categoryId">The category id.</param>
      /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
      /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateBRepRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
         IFCAnyHandle contextOfItems, ISet<IFCAnyHandle> bodyItems)
      {
         string identifierOpt = "Body";   // this is by IFC2x2 convention, not temporary
         string repTypeOpt = ShapeRepresentationType.Brep.ToString();   // this is by IFC2x2 convention, not temporary
         IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, element, categoryId,
            contextOfItems, identifierOpt, repTypeOpt, bodyItems);
         return bodyRepresentation;
      }

      /// <summary>
      /// Creates a Solid model representation.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="categoryId">The category id.</param>
      /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
      /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSolidModelRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
         IFCAnyHandle contextOfItems, ISet<IFCAnyHandle> bodyItems)
      {
         string identifierOpt = "Body";
         string repTypeOpt = ShapeRepresentationType.SolidModel.ToString();
         IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, element, categoryId,
            contextOfItems, identifierOpt, repTypeOpt, bodyItems);
         return bodyRepresentation;
      }

      /// <summary>
      /// Creates a Brep representation.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="categoryId">The category id.</param>
      /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
      /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
      /// <param name="exportAsFacetationOrMesh">
      /// If this is true, the identifier for the representation is "Facetation" as required by IfcSite for IFC2x2, IFC2x3, or "Mesh" for GSA.
      /// If this is false, the identifier for the representation is "Body" as required by IfcBuildingElement, or IfcSite for IFC2x3 v2.
      /// </param>
      /// <param name="originalShapeRepresentation">The original shape representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSurfaceRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
          IFCAnyHandle contextOfItems, ISet<IFCAnyHandle> bodyItems, bool exportAsFacetationOrMesh, IFCAnyHandle originalRepresentation)
      {
         string identifierOpt = null;
         if (exportAsFacetationOrMesh && ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
         {
            if (ExporterCacheManager.ExportOptionsCache.ExportAsCOBIE)
               identifierOpt = ShapeRepresentationType.Mesh.ToString(); // IFC GSA convention
            else
               identifierOpt = ShapeRepresentationType.Facetation.ToString(); // IFC2x2+ convention
         }
         else
            identifierOpt = "Body"; // Default

         string repTypeOpt = ShapeRepresentationType.SurfaceModel.ToString();  // IFC2x2+ convention
         IFCAnyHandle bodyRepresentation = CreateOrAppendShapeRepresentation(exporterIFC, element, categoryId,
            contextOfItems, identifierOpt, repTypeOpt, bodyItems, originalRepresentation);
         return bodyRepresentation;
      }

      /// <summary>
      /// Creates a boundary representation.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="categoryId">The category id.</param>
      /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
      /// <param name="bodyItems">Collection of geometric representation items that are defined for this representation.</param>
      /// <param name="originalShapeRepresentation">The original shape representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateBoundaryRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
          IFCAnyHandle contextOfItems, ISet<IFCAnyHandle> bodyItems, IFCAnyHandle originalRepresentation)
      {
         string identifierOpt = "FootPrint"; // this is by IFC2x3 convention, not temporary

         string repTypeOpt = ShapeRepresentationType.Curve2D.ToString();  // this is by IFC2x2 convention, not temporary
         IFCAnyHandle bodyRepresentation = CreateOrAppendShapeRepresentation(exporterIFC, element, categoryId,
            contextOfItems, identifierOpt, repTypeOpt, bodyItems, originalRepresentation);
         return bodyRepresentation;
      }

      /// <summary>
      /// Creates a geometric set representation.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="categoryId">The category id.</param>
      /// <param name="type">The representation type.</param>
      /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
      /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateGeometricSetRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
         string type, IFCAnyHandle contextOfItems, HashSet<IFCAnyHandle> bodyItems)
      {
         string identifierOpt = type;
         string repTypeOpt = ShapeRepresentationType.GeometricSet.ToString(); // this is by IFC2x2 convention, not temporary
         IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, element, categoryId,
            contextOfItems, identifierOpt, repTypeOpt, bodyItems);
         return bodyRepresentation;
      }

      /// <summary>
      /// Creates a body bounding box representation.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
      /// <param name="boundingBoxItem">Set of geometric representation items that are defined for this representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateBoundingBoxRep(ExporterIFC exporterIFC, IFCAnyHandle contextOfItems, IFCAnyHandle boundingBoxItem)
      {
         string identifierOpt = "Box"; // this is by IFC2x2+ convention
         string repTypeOpt = ShapeRepresentationType.BoundingBox.ToString();  // this is by IFC2x2+ convention
         ISet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
         bodyItems.Add(boundingBoxItem);
         IFCAnyHandle bodyRepresentation = CreateBaseShapeRepresentation(exporterIFC, contextOfItems, identifierOpt, repTypeOpt, bodyItems);
         return bodyRepresentation;
      }

      /// <summary>
      /// Creates a body mapped item representation.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="categoryId">The category id.</param>
      /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
      /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateBodyMappedItemRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
         IFCAnyHandle contextOfItems, ISet<IFCAnyHandle> bodyItems)
      {
         string identifierOpt = IFCAnyHandleUtil.GetStringAttribute(contextOfItems, "ContextIdentifier");
         string repTypeOpt = ShapeRepresentationType.MappedRepresentation.ToString();  // this is by IFC2x2+ convention
         IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, element, categoryId,
            contextOfItems, identifierOpt, repTypeOpt, bodyItems);
         return bodyRepresentation;
      }

      /// <summary>
      /// Creates a plan mapped item representation.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="categoryId">The category id.</param>
      /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
      /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreatePlanMappedItemRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
          IFCAnyHandle contextOfItems, HashSet<IFCAnyHandle> bodyItems)
      {
         string identifierOpt = "FootPrint"; // this is by IFC2x2+ convention
         string repTypeOpt = ShapeRepresentationType.MappedRepresentation.ToString();  // this is by IFC2x2+ convention
         IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, element, categoryId,
             contextOfItems, identifierOpt, repTypeOpt, bodyItems);
         return bodyRepresentation;
      }

      /// <summary>
      /// Create a graph mapped item representation for Axis data
      /// </summary>
      /// <param name="exporterIFC">tje ExporterIFC object</param>
      /// <param name="element">the category Id</param>
      /// <param name="categoryId"></param>
      /// <param name="contextOfItems">he context for which the different subtypes of representation are valid.</param>
      /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
      /// <returns></returns>
      public static IFCAnyHandle CreateGraphMappedItemRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
         IFCAnyHandle contextOfItems, HashSet<IFCAnyHandle> bodyItems)
      {
         string identifierOpt = "Axis"; // this is by IFC2x2+ convention
         string repTypeOpt = ShapeRepresentationType.MappedRepresentation.ToString();  // this is by IFC2x2+ convention
         IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, element, categoryId,
               contextOfItems, identifierOpt, repTypeOpt, bodyItems);
         return bodyRepresentation;
      }

      /// <summary>
      /// Creates an annotation representation.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="element">The element.</param>
      /// <param name="categoryId">The category id.</param>
      /// <param name="contextOfItems">The context for which the different subtypes of representation are valid.</param>
      /// <param name="bodyItems">Set of geometric representation items that are defined for this representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateAnnotationSetRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
            IFCAnyHandle contextOfItems, HashSet<IFCAnyHandle> bodyItems)
      {
         string identifierOpt = "Annotation";
         string repTypeOpt = ShapeRepresentationType.Annotation2D.ToString(); // this is by IFC2x3 convention

         IFCAnyHandle bodyRepresentation = CreateShapeRepresentation(exporterIFC, element, categoryId,
             contextOfItems, identifierOpt, repTypeOpt, bodyItems);
         return bodyRepresentation;
      }

      /// <summary>
      /// Creates a SweptSolid, Brep, or Surface product definition shape representation, depending on the geoemtry and export version.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="categoryId">The category id.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="bodyExporterOptions">The body exporter options.</param>
      /// <param name="extraReps">Extra representations (e.g. Axis, Boundary).  May be null.</param>
      /// <param name="extrusionCreationData">The extrusion creation data.</param>
      /// <param name="allowOffsetTransform">Allows local coordinate system to be placed close to geometry.</param>
      /// <returns>The handle.</returns>
      /// <remarks>allowOffsetTransform should only be set to true if no other associated geometry is going to be exported.  Otherwise,
      /// there could be an offset between this geometry and the other, non-transformed, geometry.</remarks>
      public static IFCAnyHandle CreateAppropriateProductDefinitionShape(ExporterIFC exporterIFC, Element element, ElementId categoryId,
          GeometryElement geometryElement, BodyExporterOptions bodyExporterOptions, IList<IFCAnyHandle> extraReps,
          IFCExtrusionCreationData extrusionCreationData, bool allowOffsetTransform)
      {
         BodyData bodyData;
         BodyExporterOptions newBodyExporterOptions = new BodyExporterOptions(bodyExporterOptions);
         newBodyExporterOptions.AllowOffsetTransform = allowOffsetTransform;

         return CreateAppropriateProductDefinitionShape(exporterIFC, element, categoryId,
             geometryElement, newBodyExporterOptions, extraReps, extrusionCreationData, out bodyData);
      }

      /// <summary>
      /// Creates a SweptSolid, Brep, SolidModel or SurfaceModel product definition shape representation, based on the geometry and IFC version.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="categoryId">The category id.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="bodyExporterOptions">The body exporter options.</param>
      /// <param name="extraReps">Extra representations (e.g. Axis, Boundary).  May be null.</param>
      /// <param name="extrusionCreationData">The extrusion creation data.</param>
      /// <param name="bodyData">The body data.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateAppropriateProductDefinitionShape(ExporterIFC exporterIFC, Element element, ElementId categoryId,
          GeometryElement geometryElement, BodyExporterOptions bodyExporterOptions, IList<IFCAnyHandle> extraReps,
          IFCExtrusionCreationData extrusionCreationData, out BodyData bodyData, bool skipBody = false)
      {
         bodyData = null;
         SolidMeshGeometryInfo info = null;
         IList<GeometryObject> geometryList = new List<GeometryObject>();

         if (!ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
         {
            info = GeometryUtil.GetSplitSolidMeshGeometry(geometryElement, Transform.Identity);
            IList<Mesh> meshes = info.GetMeshes();
            if (meshes.Count == 0)
            {
               IList<Solid> solidList = info.GetSolids();
               //foreach (Solid solid in solidList)
               //{
               //   geometryList.Add(solid);
               //}
               geometryList = FamilyExporterUtil.RemoveInvisibleSolidsAndMeshes(element.Document, exporterIFC, ref solidList, ref meshes);
               if (geometryList.Count == 0 && !skipBody)
                  return null;
            }
         }

         if (geometryList.Count == 0)
            geometryList.Add(geometryElement);
         else
         {
            bodyExporterOptions.TryToExportAsExtrusion = true;
         }

         List<IFCAnyHandle> bodyReps = new List<IFCAnyHandle>();
         if (!skipBody)
         {
            ElementId matId = ExporterUtil.GetSingleMaterial(element);

            bodyData = BodyExporter.ExportBody(exporterIFC, element, categoryId, matId, geometryList,
                bodyExporterOptions, extrusionCreationData);
            IFCAnyHandle bodyRep = bodyData.RepresentationHnd;
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRep))
            {
               if (extrusionCreationData != null)
                  extrusionCreationData.ClearOpenings();
            }
            else
               bodyReps.Add(bodyRep);
         }

         if (extraReps != null)
         {
            foreach (IFCAnyHandle hnd in extraReps)
               bodyReps.Add(hnd);
         }

         Transform boundingBoxTrf = Transform.Identity;
         if (bodyData != null && bodyData.OffsetTransform != null)
            boundingBoxTrf = bodyData.OffsetTransform.Inverse;
         IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geometryElement, boundingBoxTrf);
         if (boundingBoxRep != null)
            bodyReps.Add(boundingBoxRep);

         return IFCInstanceExporter.CreateProductDefinitionShape(exporterIFC.GetFile(), null, null, bodyReps);
      }

      /// <summary>
      /// Creates a product definition shape representation wihotu Body rep, but incl. bounding box based on the geometry and IFC version.
      /// </summary>
      /// <param name="exporterIFC">exporteriFC</param>
      /// <param name="element">the Element</param>
      /// <param name="categoryId">the cateogory id</param>
      /// <param name="geometryElement">the geometry element</param>
      /// <param name="bodyExporterOptions">exporter option</param>
      /// <param name="extraReps">extra representation to be added to the product definition shape</param>
      /// <returns>product definition shape handle</returns>
      public static IFCAnyHandle CreateProductDefinitionShapeWithoutBodyRep(ExporterIFC exporterIFC, Element element, ElementId categoryId,
          GeometryElement geometryElement, IList<IFCAnyHandle> extraReps)
      {
         BodyData bodyData;
         BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(false, ExportOptionsCache.ExportTessellationLevel.Medium);

         return CreateAppropriateProductDefinitionShape(exporterIFC, element, categoryId,
             geometryElement, bodyExporterOptions, extraReps, null, out bodyData, skipBody:true);
      }
      
      /// <summary>
         /// Creates a surface product definition shape representation.
         /// </summary>
         /// <param name="exporterIFC">The ExporterIFC object.</param>
         /// <param name="element">The element.</param>
         /// <param name="geometryElement">The geometry element.</param>
         /// <param name="exportBoundaryRep">If this is true, it will export boundary representations.</param>
         /// <param name="exportAsFacetation">If this is true, it will export the geometry as facetation.</param>
         /// <returns>The handle.</returns>
         public static IFCAnyHandle CreateSurfaceProductDefinitionShape(ExporterIFC exporterIFC, Element element,
         GeometryElement geometryElement, bool exportBoundaryRep, bool exportAsFacetation)
      {
         IFCAnyHandle bodyRep = null;
         IFCAnyHandle boundaryRep = null;
         return CreateSurfaceProductDefinitionShape(exporterIFC, element, geometryElement, exportBoundaryRep, exportAsFacetation, ref bodyRep, ref boundaryRep);
      }

      /// <summary>
      /// Creates a surface product definition shape representation.
      /// </summary>
      /// <remarks>
      /// If a body representation is supplied, then we expect that this is already contained in a representation list, inside
      /// a product representation. As such, just modify the list and return.
      /// </remarks>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="exportBoundaryRep">If this is true, it will export boundary representations.</param>
      /// <param name="exportAsFacetation">If this is true, it will export the geometry as facetation.</param>
      /// <param name="bodyRep">Body representation.</param>
      /// <param name="boundaryRep">Boundary representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSurfaceProductDefinitionShape(ExporterIFC exporterIFC, Element element,
         GeometryElement geometryElement, bool exportBoundaryRep, bool exportAsFacetation, ref IFCAnyHandle bodyRep, ref IFCAnyHandle boundaryRep)
      {
         bool hasOriginalBodyRepresentation = bodyRep != null;
         bool success = SurfaceExporter.ExportSurface(exporterIFC, element, geometryElement, exportBoundaryRep, exportAsFacetation, ref bodyRep, ref boundaryRep);

         if (!success)
            return null;

         // If we supplied a body representation, then we expect that this is already contained in a representation list, inside
         // a product representation.  As such, just modify the list and return.
         if (hasOriginalBodyRepresentation)
            return null;

         List<IFCAnyHandle> representations = new List<IFCAnyHandle>();
         representations.Add(bodyRep);
         if (exportBoundaryRep && !IFCAnyHandleUtil.IsNullOrHasNoValue(boundaryRep))
            representations.Add(boundaryRep);

         IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geometryElement, Transform.Identity);
         if (boundingBoxRep != null)
            representations.Add(boundingBoxRep);

         return IFCInstanceExporter.CreateProductDefinitionShape(exporterIFC.GetFile(), null, null, representations);
      }

      /// <summary>
      /// Creates a extruded product definition shape representation.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The base element.</param>
      /// <param name="categoryId">The category of the element.</param>
      /// <param name="curveLoops">The curve loops defining the extruded surface.</param>
      /// <param name="lcs">The local coordinate system of the bse curve loops.</param>
      /// <param name="extrDirVec">The extrusion direction.</param>
      /// <param name="extrusionSize">The scaled extrusion length.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateExtrudedProductDefShape(ExporterIFC exporterIFC, Element element, ElementId categoryId,
          IList<CurveLoop> curveLoops, Transform lcs, XYZ extrDirVec, double extrusionSize)
      {
         IFCFile file = exporterIFC.GetFile();

         IFCAnyHandle extrusionHnd = ExtrusionExporter.CreateExtrudedSolidFromCurveLoop(exporterIFC, null, curveLoops, lcs,
             extrDirVec, extrusionSize, false);

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(extrusionHnd))
            return null;

         ISet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
         bodyItems.Add(extrusionHnd);

         IFCAnyHandle contextOfItems = exporterIFC.Get3DContextHandle("Body");
         IFCAnyHandle shapeRepHnd = CreateSweptSolidRep(exporterIFC, element, categoryId, contextOfItems, bodyItems, null);

         IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
         shapeReps.Add(shapeRepHnd);
         return IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapeReps);
      }

      /// <summary>
      /// Checking the the geometry representation of a product contains representations that fulfill the requirements for "StandardCase"
      /// </summary>
      /// <param name="exportType">the export type of the element</param>
      /// <param name="productHnd">IfcProduct handle</param>
      /// <returns>true if it fulfills the StandardCase requirements</returns>
      public static bool RepresentationForStandardCaseFromProduct(IFCEntityType exportType, IFCAnyHandle productHnd)
      {
         List<IFCAnyHandle> representationHnds = IFCAnyHandleUtil.GetRepresentations(IFCAnyHandleUtil.GetRepresentation(productHnd));
         return RepresentationForStandardCases(exportType, representationHnds);
      }

      /// <summary>
      /// Check the content of IfcRepresentation for geometries that fulfill the requirements for StandardCase
      /// </summary>
      /// <param name="exportType">element export type</param>
      /// <param name="representationHnds">List of IfcRepresentations</param>
      /// <returns>true if the IfcRepresentation (Body) contains the suitable geometry for various types</returns>
      public static bool RepresentationForStandardCases(IFCEntityType exportType, List<IFCAnyHandle> representationHnds)
      {
         if (representationHnds.Count == 0)
            return false;

         foreach (IFCAnyHandle repHnd in representationHnds)
         {
            IFCAnyHandleUtil.ValidateSubTypeOf(repHnd, false, Common.Enums.IFCEntityType.IfcRepresentation);
            string repIdent = IFCAnyHandleUtil.GetRepresentationIdentifier(repHnd);
            if (repIdent.Equals("Body"))
            {
               HashSet<IFCAnyHandle> repItems = null;
               string repType = IFCAnyHandleUtil.GetRepresentationType(repHnd);
               if (repType.Equals("SweptSolid") || repType.Equals("AdvancedSweptSolid") || repType.Equals("Clipping")
                  || (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView && repType.Equals("Tessellation")))
                  repItems = IFCAnyHandleUtil.GetItems(repHnd);
               else if (repType.Equals("MappedRepresentation"))
               {
                  // It is a MappedRepresentation, get IfcRepresentation from the source
                  HashSet<IFCAnyHandle> mappedRepItems = IFCAnyHandleUtil.GetItems(repHnd);
                  foreach (IFCAnyHandle mappedRepItem in mappedRepItems)
                  {
                     IFCAnyHandle mappingSource = IFCAnyHandleUtil.GetInstanceAttribute(mappedRepItem, "MappingSource");
                     IFCAnyHandle mappedRep = IFCAnyHandleUtil.GetInstanceAttribute(mappingSource, "MappedRepresentation");
                     repItems = IFCAnyHandleUtil.GetItems(mappedRep);
                  }
               }
               else
                  return false;

               int validGeomCount = 0;
               foreach (IFCAnyHandle repItem in repItems)
               {
                  if (IFCAnyHandleUtil.IsTypeOf(repItem, Common.Enums.IFCEntityType.IfcExtrudedAreaSolid))
                  {
                     validGeomCount++;
                     continue;
                  }

                  // IfcOpeningStandardCase only allows IfcExtrudedAreaSolid
                  if (exportType == IFCEntityType.IfcOpeningElement)
                     continue;

                  // For IfcBooleanClippingResult, we must ensure the that at least one of the leaf is an ExtrudedAreaSolid (should be the base, but we have no way to know)
                  if (IFCAnyHandleUtil.IsTypeOf(repItem, Common.Enums.IFCEntityType.IfcBooleanClippingResult))
                  {
                     IFCAnyHandle firstOperand = IFCAnyHandleUtil.GetInstanceAttribute(repItem, "FirstOperand");
                     IFCAnyHandle secondOperand = IFCAnyHandleUtil.GetInstanceAttribute(repItem, "SecondOperand");
                     if (BooleanResultLeafIsTypeOf(firstOperand, Common.Enums.IFCEntityType.IfcExtrudedAreaSolid)
                         || BooleanResultLeafIsTypeOf(secondOperand, Common.Enums.IFCEntityType.IfcExtrudedAreaSolid))
                     {
                        validGeomCount++;
                        continue;
                     }
                  }

                  if ((exportType == IFCEntityType.IfcBeam || exportType == IFCEntityType.IfcBeamType
                        || exportType == IFCEntityType.IfcColumn || exportType == IFCEntityType.IfcColumnType
                        || exportType == IFCEntityType.IfcMember || exportType == IFCEntityType.IfcMemberType)
                      && (IFCAnyHandleUtil.IsTypeOf(repItem, Common.Enums.IFCEntityType.IfcSurfaceCurveSweptAreaSolid)
                        || IFCAnyHandleUtil.IsTypeOf(repItem, Common.Enums.IFCEntityType.IfcFixedReferenceSweptAreaSolid)
                        || IFCAnyHandleUtil.IsTypeOf(repItem, Common.Enums.IFCEntityType.IfcExtrudedAreaSolidTapered)
                        || IFCAnyHandleUtil.IsTypeOf(repItem, Common.Enums.IFCEntityType.IfcRevolvedAreaSolid))
                        || (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView && IFCAnyHandleUtil.IsSubTypeOf(repItem, Common.Enums.IFCEntityType.IfcTessellatedFaceSet)))
                  {
                     validGeomCount++;
                     continue;
                  }
               }

               // If the valid geometry count is equel to the the number of representation items, it is a valid geometry for Ifc*StandardCase
               if ((validGeomCount == repItems.Count) && (validGeomCount > 0))
                  return true;
            }
         }
         return false;
      }

      private static bool BooleanResultLeafIsTypeOf(IFCAnyHandle booleanResultOperand, Common.Enums.IFCEntityType entityType)
      {
         bool firstOpLeafOfType = false;
         bool secondOpLeafOfType = false;

         // It is already a leaf and it is of the right type
         if (IFCAnyHandleUtil.IsTypeOf(booleanResultOperand, entityType))
            return true;

         if (IFCAnyHandleUtil.IsTypeOf(booleanResultOperand, Common.Enums.IFCEntityType.IfcBooleanClippingResult))
         {
            IFCAnyHandle firstOperand = IFCAnyHandleUtil.GetInstanceAttribute(booleanResultOperand, "FirstOperand");
            if (IFCAnyHandleUtil.IsTypeOf(firstOperand, Common.Enums.IFCEntityType.IfcBooleanClippingResult))
            {
               firstOpLeafOfType = BooleanResultLeafIsTypeOf(firstOperand, entityType);
            }
            else
            {
               if (IFCAnyHandleUtil.IsTypeOf(firstOperand, entityType))
                  firstOpLeafOfType = true;
            }

            IFCAnyHandle secondOperand = IFCAnyHandleUtil.GetInstanceAttribute(booleanResultOperand, "SecondOperand");
            if (IFCAnyHandleUtil.IsTypeOf(secondOperand, Common.Enums.IFCEntityType.IfcBooleanClippingResult))
            {
               secondOpLeafOfType = BooleanResultLeafIsTypeOf(secondOperand, entityType);
            }
            else
            {
               if (IFCAnyHandleUtil.IsTypeOf(secondOperand, entityType))
                  secondOpLeafOfType = true;
            }

         }
         return (firstOpLeafOfType || secondOpLeafOfType);
      }

      public static int DimOfRepresentationContext(IFCAnyHandle rep)
      {
         if (rep == null)
            throw new ArgumentNullException("representation");

         if (!rep.HasValue)
            throw new ArgumentException("Invalid handle.");

         IFCAnyHandle theRep;
         if (IFCAnyHandleUtil.IsSubTypeOf(rep, Common.Enums.IFCEntityType.IfcRepresentationMap))
         {
            theRep = IFCAnyHandleUtil.GetInstanceAttribute(rep, "MappedRepresentation");
            if (theRep == null)
               return 0;
         }
         else if (IFCAnyHandleUtil.IsSubTypeOf(rep, Common.Enums.IFCEntityType.IfcRepresentation))
         {
            theRep = rep;
         }
         else
            throw new ArgumentException("The operation is not valid for this handle.");

         IFCAnyHandle context = IFCAnyHandleUtil.GetContextOfItems(theRep);
         if (IFCAnyHandleUtil.IsTypeOf(context, Common.Enums.IFCEntityType.IfcGeometricRepresentationSubContext))
         {
            string targetViewEnum = IFCAnyHandleUtil.GetEnumerationAttribute(context, "TargetView");
            if (targetViewEnum.Equals("MODEL_VIEW"))
               return 3;
            if (targetViewEnum.Equals("PLAN_VIEW") || targetViewEnum.Equals("SKETCH_VIEW") || targetViewEnum.Equals("REFLECTED_PLAN_VIEW")
                  || targetViewEnum.Equals("SECTION_VIEW") || targetViewEnum.Equals("ELEVATION_VIEW"))
               return 2;
            if (targetViewEnum.Equals("GRAPH_VIEW"))
               return 1;
         }

         return 0;
      }

      /// <summary>
      /// Create Shape representation of a geometry item together with its associated IfcShapeAspect. This works only for "Body"
      /// </summary>
      /// <param name="exporterIFC">exporter IFC</param>
      /// <param name="hostElement">the host element owning the geometries</param>
      /// <param name="hostProdDefShape">product definition shape of the host</param>
      /// <param name="repType">Representation type</param>
      /// <param name="aspectName">aspect name: expected to be component category, if not by default should be the same as the material name</param>
      /// <param name="itemRep">the geometry representation item</param>
      public static void CreateRepForShapeAspect(ExporterIFC exporterIFC, Element hostElement, IFCAnyHandle hostProdDefShape, string repType, string aspectName, IFCAnyHandle itemRep)
      {
         CreateRepForShapeAspect(exporterIFC, hostElement, hostProdDefShape, repType, aspectName, new HashSet<IFCAnyHandle>() { itemRep });
      }

      /// <summary>
      /// Create Shape representation of a geometry item together with its associated IfcShapeAspect. This works only for "Body"
      /// </summary>
      /// <param name="exporterIFC">exporter IFC</param>
      /// <param name="hostElement">the host element owning the geometries</param>
      /// <param name="hostProdDefShape">product definition shape of the host</param>
      /// <param name="repType">Representation type</param>
      /// <param name="aspectName">aspect name: expected to be component category, if not by default should be the same as the material name</param>
      /// <param name="itemRepSet">Set of IfcRepresentationItems</param>
      public static void CreateRepForShapeAspect(ExporterIFC exporterIFC, Element hostElement, IFCAnyHandle hostProdDefShape, string repType, string aspectName, HashSet<IFCAnyHandle> itemRepSet)
      {
         string shapeIdent = "Body";
         IFCAnyHandle contextOfItems = exporterIFC.Get3DContextHandle(shapeIdent);
         ElementId catId = CategoryUtil.GetSafeCategoryId(hostElement);
         if (IFCAnyHandleUtil.IsSubTypeOf(hostProdDefShape, IFCEntityType.IfcProductRepresentation))
         {
            IFCAnyHandle representationOfItem = RepresentationUtil.CreateShapeRepresentation(exporterIFC, hostElement, catId, contextOfItems, shapeIdent, repType, itemRepSet);
            IFCAnyHandle shapeAspect = IFCInstanceExporter.CreateShapeAspect(exporterIFC.GetFile(), new List<IFCAnyHandle>() { representationOfItem }, aspectName, null, null, hostProdDefShape);
         }
         else if (IFCAnyHandleUtil.IsSubTypeOf(hostProdDefShape, IFCEntityType.IfcRepresentationMap))
         {
            IFCAnyHandle representation = IFCAnyHandleUtil.GetInstanceAttribute(hostProdDefShape, "MappedRepresentation");
            string representationType = IFCAnyHandleUtil.GetRepresentationType(representation);
            IFCAnyHandle representationOfItem = RepresentationUtil.CreateShapeRepresentation(exporterIFC, hostElement, catId, contextOfItems, shapeIdent,
               representationType, itemRepSet);
            IFCAnyHandle shapeAspect = IFCInstanceExporter.CreateShapeAspect(exporterIFC.GetFile(), new List<IFCAnyHandle>() { representationOfItem }, aspectName, null, null, hostProdDefShape);
         }
      }

      /// <summary>
      /// Create IfcStyledItem if not yet exists for material and assign it to the bodyItem
      /// </summary>
      /// <param name="file">The IFC file</param>
      /// <param name="document">The document</param>
      /// <param name="materialId">The material id</param>
      /// <param name="bodyItem">The body Item to assign to the StyleItem to</param>
      public static void CreateStyledItemAndAssign(IFCFile file, Document document, ElementId materialId, IFCAnyHandle bodyItem)
      {
         IFCAnyHandle surfStyleHnd = CategoryUtil.GetOrCreateMaterialStyle(document, file, materialId);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(surfStyleHnd))
         {
            ISet<IFCAnyHandle> styles = new HashSet<IFCAnyHandle>();
            styles.Add(surfStyleHnd);

            if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
            {
               IFCInstanceExporter.CreatePresentationStyleAssignment(file, styles);
            }
            else
            {
               IFCInstanceExporter.CreateStyledItem(file, bodyItem, styles as HashSet<IFCAnyHandle>, null);
            }
         }
      }
   }
}