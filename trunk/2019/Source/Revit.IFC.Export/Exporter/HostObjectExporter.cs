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
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export host objects.
   /// </summary>
   class HostObjectExporter
   {
      /// <summary>
      /// Exports materials for host object.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="hostObject">The host object.</param>
      /// <param name="elemHnds">The host IFC handles.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      /// <param name="levelId">The level id.</param>
      /// <param name="direction">The IFCLayerSetDirection.</param>
      /// <param name="containsBRepGeometry">True if the geometry contains BRep geoemtry.  If so, we will export an IfcMaterialList</param>
      /// <returns>True if exported successfully, false otherwise.</returns>
      public static bool ExportHostObjectMaterials(ExporterIFC exporterIFC, HostObject hostObject,
          IList<IFCAnyHandle> elemHnds, GeometryElement geometryElement, ProductWrapper productWrapper,
          ElementId levelId, Toolkit.IFCLayerSetDirection direction, bool containsBRepGeometry, IFCAnyHandle typeHnd = null)
      {
         if (hostObject == null)
            return true; //nothing to do

         if (elemHnds == null || (elemHnds.Count == 0))
            return true; //nothing to do

         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction tr = new IFCTransaction(file))
         {
            //if (productWrapper != null)
            //    productWrapper.ClearFinishMaterials();

            double scaledOffset = 0.0, scaledWallWidth = 0.0, wallHeight = 0.0;
            Wall wall = hostObject as Wall;

            if (wall != null)
            {
               scaledWallWidth = UnitUtil.ScaleLength(wall.Width);
               scaledOffset = -scaledWallWidth / 2.0;
               BoundingBoxXYZ boundingBox = wall.get_BoundingBox(null);
               if (boundingBox != null)
                  wallHeight = boundingBox.Max.Z - boundingBox.Min.Z;
            }

            List<ElementId> matIds;
            IFCAnyHandle primaryMaterialHnd;
            IFCAnyHandle materialLayerSet = ExporterUtil.CollectMaterialLayerSet(exporterIFC, hostObject, productWrapper, out matIds, out primaryMaterialHnd);

            // For IFC4 RV, material layer may still be created even if the geometry is Brep/Tessellation
            if ((containsBRepGeometry && matIds.Count > 0)
                  && !(ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView && !IFCAnyHandleUtil.IsNullOrHasNoValue(materialLayerSet)))
            {
               foreach (IFCAnyHandle elemHnd in elemHnds)
               {
                  CategoryUtil.CreateMaterialAssociation(exporterIFC, elemHnd, matIds);
               }
            }

            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(materialLayerSet))
            {
               // IfcMaterialLayerSetUsage is not supported for IfcWall, only IfcWallStandardCase.
               IFCAnyHandle layerSetUsage = null;
               for (int ii = 0; ii < elemHnds.Count; ii++)
               {
                  IFCAnyHandle elemHnd = elemHnds[ii];
                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(elemHnd))
                     continue;

                  SpaceBoundingElementUtil.RegisterSpaceBoundingElementHandle(exporterIFC, elemHnd, hostObject.Id, levelId);

                  // Even if it is Tessellated geometry in IFC4RV, the material layer will still be assigned
                  if (containsBRepGeometry && !ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
                     continue;

                  HashSet<IFCAnyHandle> relDecomposesSet = IFCAnyHandleUtil.GetRelDecomposes(elemHnd);

                  IList<IFCAnyHandle> subElemHnds = null;
                  if (relDecomposesSet != null && relDecomposesSet.Count == 1)
                  {
                     IFCAnyHandle relAggregates = relDecomposesSet.First();
                     if (IFCAnyHandleUtil.IsTypeOf(relAggregates, IFCEntityType.IfcRelAggregates))
                        subElemHnds = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(relAggregates, "RelatedObjects");
                  }

                  bool hasSubElems = (subElemHnds != null && subElemHnds.Count != 0);
                  bool isRoof = IFCAnyHandleUtil.IsTypeOf(elemHnd, IFCEntityType.IfcRoof);
                  bool isDisallowedWallType = (IFCAnyHandleUtil.IsTypeOf(elemHnd, IFCEntityType.IfcWall) && !ExporterCacheManager.ExportOptionsCache.ExportAs4);

                  // Create IfcMaterialLayerSetUsage unless we have sub-elements, are exporting a Roof, or are exporting a pre-IFC4 IfcWall.
                  if (!hasSubElems && !isRoof && !isDisallowedWallType)
                  {
                     bool materialAlreadyAssoc = false;
                     if (typeHnd != null)
                     {
                        CategoryUtil.CreateMaterialAssociation(exporterIFC, typeHnd, materialLayerSet);
                        materialAlreadyAssoc = true;
                     }

                     if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
                     {
                        if (!materialAlreadyAssoc)
                        {
                           CategoryUtil.CreateMaterialAssociation(exporterIFC, elemHnd, materialLayerSet);
                        }
                     }
                     else
                     {
                        if (layerSetUsage == null)
                        {
                           bool flipDirSense = true;
                           if (wall != null)
                           {
                              // if we have flipped the center curve on export, we need to take that into account here.
                              // We flip the center curve on export if it is an arc and it has a negative Z direction.
                              LocationCurve locCurve = wall.Location as LocationCurve;
                              if (locCurve != null)
                              {
                                 Curve curve = locCurve.Curve;
                                 Transform lcs = Transform.Identity;
                                 bool curveFlipped = GeometryUtil.MustFlipCurve(lcs, curve);
                                 flipDirSense = !(wall.Flipped ^ curveFlipped);
                              }
                           }
                           else if (hostObject is CeilingAndFloor)
                           {
                              flipDirSense = false;
                           }

                           double offsetFromReferenceLine = flipDirSense ? -scaledOffset : scaledOffset;
                           IFCDirectionSense sense = flipDirSense ? IFCDirectionSense.Negative : IFCDirectionSense.Positive;

                           layerSetUsage = IFCInstanceExporter.CreateMaterialLayerSetUsage(file, materialLayerSet, direction, sense, offsetFromReferenceLine);
                        }
                        ExporterCacheManager.MaterialLayerRelationsCache.Add(layerSetUsage, elemHnd);
                     }
                  }
                  else
                  {
                     if (hasSubElems)
                     {
                        foreach (IFCAnyHandle subElemHnd in subElemHnds)
                        {
                           // TODO: still need to figure out the best way to create type for the sub elements because at this time a lot of information is not available, e.g.
                           //    the Revit Element to get the type, other information for name, GUID, etc.
                           if (!IFCAnyHandleUtil.IsNullOrHasNoValue(subElemHnd))
                           {
                              CategoryUtil.CreateMaterialAssociation(exporterIFC, subElemHnd, materialLayerSet);
                           }
                        }
                     }
                     else if (!isRoof)
                     {
                        if (typeHnd != null)
                        {
                           CategoryUtil.CreateMaterialAssociation(exporterIFC, typeHnd, materialLayerSet);
                        }
                        else
                        {
                           CategoryUtil.CreateMaterialAssociation(exporterIFC, elemHnd, materialLayerSet);
                        }
                     }
                     else if (primaryMaterialHnd != null)
                     {
                        if (typeHnd != null)
                        {
                           CategoryUtil.CreateMaterialAssociation(exporterIFC, typeHnd, materialLayerSet);
                        }
                        else
                        {
                           CategoryUtil.CreateMaterialAssociation(exporterIFC, elemHnd, materialLayerSet);
                        }
                     }
                  }
               }
            }
            tr.Commit();
            return true;
         }
      }


      /// <summary>
      /// Exports materials for host object.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="hostObject">The host object.</param>
      /// <param name="elemHnd">The host IFC handle.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      /// <param name="levelId">The level id.</param>
      /// <param name="direction">The IFCLayerSetDirection.</param>
      /// <param name="containsBRepGeometry">True if the geometry contains BRep geoemtry.  If so, we will export an IfcMaterialList.  If null, we will calculate.</param>
      /// <returns>True if exported successfully, false otherwise.</returns>
      public static bool ExportHostObjectMaterials(ExporterIFC exporterIFC, HostObject hostObject,
          IFCAnyHandle elemHnd, GeometryElement geometryElement, ProductWrapper productWrapper,
          ElementId levelId, Toolkit.IFCLayerSetDirection direction, bool? containsBRepGeometry, IFCAnyHandle typeHnd)
      {
         IList<IFCAnyHandle> elemHnds = new List<IFCAnyHandle>();
         elemHnds.Add(elemHnd);

         // Setting doesContainBRepGeometry to false below preserves the original behavior that we created IfcMaterialLists for all geometries.
         // TODO: calculate, or pass in, a valid bool value for Ceilings, Roofs, and Wall Sweeps.
         bool doesContainBRepGeometry = containsBRepGeometry.HasValue ? containsBRepGeometry.Value : false;
         return ExportHostObjectMaterials(exporterIFC, hostObject, elemHnds, geometryElement, productWrapper, levelId, direction, doesContainBRepGeometry, typeHnd);
      }

      /// <summary>
      /// Gets the material Id of the first layer of the host object.
      /// </summary>
      /// <param name="hostObject">The host object.</param>
      /// <returns>The material id.</returns>
      public static ElementId GetFirstLayerMaterialId(HostObject hostObject)
      {
         ElementId typeElemId = hostObject.GetTypeId();
         HostObjAttributes hostObjAttr = hostObject.Document.GetElement(typeElemId) as HostObjAttributes;
         if (hostObjAttr == null)
            return ElementId.InvalidElementId;

         CompoundStructure cs = hostObjAttr.GetCompoundStructure();
         if (cs != null)
         {
            ElementId matId = cs.LayerCount > 0 ? cs.GetMaterialId(0) : ElementId.InvalidElementId;
            if (matId != ElementId.InvalidElementId)
               return matId;
            else
               return CategoryUtil.GetBaseMaterialIdForElement(hostObject); ;
         }

         return ElementId.InvalidElementId;
      }

      /// <summary>
      /// Gets the material ids of finish function of the host object.
      /// </summary>
      /// <param name="hostObject">The host object.</param>
      /// <returns>The material ids.</returns>
      public static ISet<ElementId> GetFinishMaterialIds(HostObject hostObject)
      {
         HashSet<ElementId> matIds = new HashSet<ElementId>();

         ElementId typeElemId = hostObject.GetTypeId();
         HostObjAttributes hostObjAttr = hostObject.Document.GetElement(typeElemId) as HostObjAttributes;
         if (hostObjAttr == null)
            return matIds;

         ElementId baseMatId = CategoryUtil.GetBaseMaterialIdForElement(hostObject);
         CompoundStructure cs = hostObjAttr.GetCompoundStructure();
         if (cs != null)
         {
            for (int ii = 0; ii < cs.LayerCount; ++ii)
            {
               MaterialFunctionAssignment function = cs.GetLayerFunction(ii);
               if (function == MaterialFunctionAssignment.Finish1 || function == MaterialFunctionAssignment.Finish2)
               {
                  ElementId matId = cs.GetMaterialId(ii);
                  if (matId != ElementId.InvalidElementId)
                  {
                     matIds.Add(matId);
                  }
                  else if (baseMatId != ElementId.InvalidElementId)
                  {
                     matIds.Add(baseMatId);
                  }
               }
            }
         }

         return matIds;
      }
   }
}