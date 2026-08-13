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
using System.Collections.Generic;

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
            using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element, null))
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
                  // TODO: to allow shared geometry for Footings. For now, Footing export will not use shared geometry
                  IFCAnyHandle typeHandle = (exportInfo.ExportType != Common.Enums.IFCEntityType.UnKnown) ?
                     ExporterUtil.CreateGenericTypeFromElement(element, exportInfo, file, productWrapper) : null;

                  IFCAnyHandle footing = IFCInstanceExporter.CreateGenericIFCEntity(exportInfo, file, element, typeHandle, instanceGUID,
                     ExporterCacheManager.OwnerHistoryHandle, ecData.GetLocalPlacement(), prodRep);
                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(footing))
                     return;

                  ExporterCacheManager.TypeRelationsCache.Add(typeHandle, footing);

                  if (exportParts)
                  {
                     PartExporter.ExportHostPart(exporterIFC, element, footing, setter, setter.LocalPlacement, null);
                  }
                  else
                  {
                     if (!MathUtil.IsInvalidElementId(matId))
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

      static readonly Dictionary<NamingUtil.IFCStringKey, string> FootingTypesPre4 = new()
      {
         { new NamingUtil.IFCStringKey("FOOTINGBEAM"), "FOOTING_BEAM" },
         { new NamingUtil.IFCStringKey("PADFOOTING"), "PAD_FOOTING" },
         { new NamingUtil.IFCStringKey("PILECAP"), "PILE_CAP" },
         { new NamingUtil.IFCStringKey("STRIPFOOTING"), "STRIP_FOOTING" },
         { new NamingUtil.IFCStringKey("USERDEFINED"), "USERDEFINED" }
      };

      /// <summary>
      /// Gets IFC footing type from a string.
      /// </summary>
      /// <param name="value">The type name.</param>
      /// <returns>The IFCFootingType.</returns>
      public static string GetIFCFootingType(string value)
      {
         if (string.IsNullOrEmpty(value))
            return "NOTDEFINED";

         NamingUtil.IFCStringKey compValue = new(value);
         if (FootingTypesPre4.TryGetValue(compValue, out string footingType))
            return footingType;

         if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
         {
            if (compValue.IsEqualTo("CAISSONFOUNDATION"))
               return "CAISSON_FOUNDATION";
         }

         return "NOTDEFINED";
      }

   }
}