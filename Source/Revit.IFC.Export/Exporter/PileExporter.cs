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

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export Pile elements.
   /// </summary>
   class PileExporter
   {
      /// <summary>
      /// Exports an element to IfcPile.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="ifcEnumType">The string value represents the IFC type.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportPile(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement,
         string ifcEnumType, ProductWrapper productWrapper)
      {
         // export parts or not
         bool exportParts = PartExporter.CanExportParts(element);
         if (exportParts && !PartExporter.CanExportElementInPartExport(element, element.LevelId, false))
            return;

         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            // Check for containment override
            IFCAnyHandle overrideContainerHnd = null;
            ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, element, out overrideContainerHnd);

            using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element, null, null, overrideContainerId, overrideContainerHnd))
            {
               using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
               {
                  ecData.SetLocalPlacement(setter.LocalPlacement);

                  IFCAnyHandle prodRep = null;
                  ElementId matId = ElementId.InvalidElementId;
                  if (!exportParts)
                  {
                     ElementId catId = CategoryUtil.GetSafeCategoryId(element);


                     matId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(geometryElement, exporterIFC, element);
                     BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);

                     StructuralMemberAxisInfo axisInfo = StructuralMemberExporter.GetStructuralMemberAxisTransform(element);
                     if (axisInfo != null)
                     {
                        ecData.CustomAxis = axisInfo.AxisDirection;
                        ecData.PossibleExtrusionAxes = IFCExtrusionAxes.TryCustom;
                     }
                     else
                        ecData.PossibleExtrusionAxes = IFCExtrusionAxes.TryZ;

                     prodRep = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC,
                        element, catId, geometryElement, bodyExporterOptions, null, ecData, true);
                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(prodRep))
                     {
                        ecData.ClearOpenings();
                        return;
                     }
                  }

                  string instanceGUID = GUIDUtil.CreateGUID(element);
                  //string pileType = IFCValidateEntry.GetValidIFCPredefinedType(element, ifcEnumType);
                  IFCExportInfoPair exportInfo = new IFCExportInfoPair();
                  exportInfo.ValidatedPredefinedType = ifcEnumType;
                  exportInfo.SetValueWithPair(Common.Enums.IFCEntityType.IfcPile, ifcEnumType);

                  IFCAnyHandle pile = IFCInstanceExporter.CreatePile(exporterIFC, element, instanceGUID, ExporterCacheManager.OwnerHistoryHandle,
                      ecData.GetLocalPlacement(), prodRep, ifcEnumType, null);

                  // TODO: to allow shared geometry for Piles. For now, Pile export will not use shared geometry
                  if (exportInfo.ExportType != Common.Enums.IFCEntityType.UnKnown)
                  {
                     IFCAnyHandle type = ExporterUtil.CreateGenericTypeFromElement(element, exportInfo, file, ExporterCacheManager.OwnerHistoryHandle, exportInfo.ValidatedPredefinedType, productWrapper);
                     ExporterCacheManager.TypeRelationsCache.Add(type, pile);
                  }

                  if (exportParts)
                  {
                     PartExporter.ExportHostPart(exporterIFC, element, pile, productWrapper, setter, setter.LocalPlacement, null);
                  }
                  else
                  {
                     if (matId != ElementId.InvalidElementId)
                     {
                        CategoryUtil.CreateMaterialAssociation(exporterIFC, pile, matId);
                     }
                  }

                  productWrapper.AddElement(element, pile, setter, ecData, true);

                  OpeningUtil.CreateOpeningsIfNecessary(pile, element, ecData, null,
                      exporterIFC, ecData.GetLocalPlacement(), setter, productWrapper);
               }
            }

            tr.Commit();
         }
      }
   }
}