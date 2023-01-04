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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export footing elements.
   /// </summary>
   class FootingExporter
   {
      /// <summary>
      /// Exports a footing to IFC footing.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="footing">
      /// The footing element.
      /// </param>
      /// <param name="geometryElement">
      /// The geometry element.
      /// </param>
      /// <param name="productWrapper">
      /// The ProductWrapper.
      /// </param>
      public static void ExportFootingElement(ExporterIFC exporterIFC,
         WallFoundation footing, GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         String ifcEnumType = "STRIP_FOOTING";
         ExportFooting(exporterIFC, footing, geometryElement, ifcEnumType, productWrapper);
      }

      /// <summary>
      /// Exports an element to IFC footing.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <param name="geometryElement">
      /// The geometry element.
      /// </param>
      /// <param name="ifcEnumType">
      /// The string value represents the IFC type.
      /// </param>
      /// <param name="productWrapper">
      /// The ProductWrapper.
      /// </param>
      public static void ExportFooting(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement,
         string ifcEnumType, ProductWrapper productWrapper)
      {
         // export parts or not
         bool exportParts = PartExporter.CanExportParts(element);
         if (exportParts && !PartExporter.CanExportElementInPartExport(element, element.LevelId, false))
            return;

         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcFooting;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return;

         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            // Check for containment override
            IFCAnyHandle overrideContainerHnd = null;
            ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, element, out overrideContainerHnd);

            using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element, null, null, overrideContainerId, overrideContainerHnd))
            {
               using (IFCExportBodyParams ecData = new IFCExportBodyParams())
               {
                  ecData.SetLocalPlacement(setter.LocalPlacement);

                  IFCAnyHandle prodRep = null;
                  ElementId matId = ElementId.InvalidElementId;
                  if (!exportParts)
                  {
                     ElementId catId = CategoryUtil.GetSafeCategoryId(element);


                     matId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(geometryElement, element);
                     BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                     prodRep = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC,
                        element, catId, geometryElement, bodyExporterOptions, null, ecData, true);
                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(prodRep))
                     {
                        ecData.ClearOpenings();
                        return;
                     }
                  }

                  string instanceGUID = GUIDUtil.CreateGUID(element);

                  string footingType = GetIFCFootingType(ifcEnumType);    // need to keep it for legacy support when original data follows slightly diff naming
                  IFCExportInfoPair exportInfo = new IFCExportInfoPair(elementClassTypeEnum, footingType);

                  IFCAnyHandle footing = IFCInstanceExporter.CreateFooting(exporterIFC, element, instanceGUID, ExporterCacheManager.OwnerHistoryHandle,
                      ecData.GetLocalPlacement(), prodRep, footingType);

                  // TODO: to allow shared geometry for Footings. For now, Footing export will not use shared geometry
                  if (exportInfo.ExportType != Common.Enums.IFCEntityType.UnKnown)
                  {
                     IFCAnyHandle type = ExporterUtil.CreateGenericTypeFromElement(element, exportInfo, file, ExporterCacheManager.OwnerHistoryHandle, exportInfo.ValidatedPredefinedType, productWrapper);
                     ExporterCacheManager.TypeRelationsCache.Add(type, footing);
                  }

                  if (exportParts)
                  {
                     PartExporter.ExportHostPart(exporterIFC, element, footing, productWrapper, setter, setter.LocalPlacement, null);
                  }
                  else
                  {
                     if (matId != ElementId.InvalidElementId)
                     {
                        CategoryUtil.CreateMaterialAssociation(exporterIFC, footing, matId);
                     }
                  }

                  productWrapper.AddElement(element, footing, setter, ecData, true, exportInfo);

                  OpeningUtil.CreateOpeningsIfNecessary(footing, element, ecData, null,
                      exporterIFC, ecData.GetLocalPlacement(), setter, productWrapper);
               }
            }

            tr.Commit();
         }
      }

      /// <summary>
      /// Gets IFC footing type from a string.
      /// </summary>
      /// <param name="value">The type name.</param>
      /// <returns>The IFCFootingType.</returns>
      public static string GetIFCFootingType(string value)
      {
         if (String.IsNullOrEmpty(value))
            return "NOTDEFINED";

         string newValue = NamingUtil.RemoveSpacesAndUnderscores(value);

         if (String.Compare(newValue, "USERDEFINED", true) == 0)
            return "USERDEFINED";
         if (String.Compare(newValue, "FOOTINGBEAM", true) == 0)
            return "FOOTING_BEAM";
         if (String.Compare(newValue, "PADFOOTING", true) == 0)
            return "PAD_FOOTING";
         if (String.Compare(newValue, "PILECAP", true) == 0)
            return "PILE_CAP";
         if (String.Compare(newValue, "STRIPFOOTING", true) == 0)
            return "STRIP_FOOTING";

         if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
         {
            if (String.Compare(newValue, "CAISSONFOUNDATION", true) == 0)
               return "CAISSON_FOUNDATION";
         }

         return "NOTDEFINED";
      }

   }
}