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
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export a Revit element as IfcCovering of type WRAPPING.
   /// </summary>
   class DuctLiningExporter
   {
      /// <summary>
      /// Exports an element as a covering of type insulation.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      /// <returns>True if exported successfully, false otherwise.</returns>
      public static bool ExportDuctLining(ExporterIFC exporterIFC, Element element,
          GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         if (element == null || geometryElement == null)
            return false;

         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcCovering;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return false;

         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            // Check for containment override
            IFCAnyHandle overrideContainer = null;
            ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, element, out overrideContainer);

            using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, element, null, null, overrideContainerId, overrideContainer))
            {
               using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
               {
                  ecData.SetLocalPlacement(placementSetter.LocalPlacement);

                  ElementId categoryId = CategoryUtil.GetSafeCategoryId(element);

                  BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                  IFCAnyHandle representation = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC, element,
                      categoryId, geometryElement, bodyExporterOptions, null, ecData, true);

                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(representation))
                  {
                     ecData.ClearOpenings();
                     return false;
                  }

                  string guid = GUIDUtil.CreateGUID(element);
                  IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
                  IFCAnyHandle localPlacement = ecData.GetLocalPlacement();

                  string ifcType = "Wrapping";
                  IFCAnyHandle ductLining = IFCInstanceExporter.CreateCovering(exporterIFC, element, guid,
                      ownerHistory, localPlacement, representation, ifcType);
                  ExporterCacheManager.ElementToHandleCache.Register(element.Id, ductLining);
                  IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcCovering, ifcType);

                  productWrapper.AddElement(element, ductLining, placementSetter.LevelInfo, ecData, true, exportInfo);

                  ElementId matId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(geometryElement, element);
                  CategoryUtil.CreateMaterialAssociation(exporterIFC, ductLining, matId);
               }
            }
            tr.Commit();
            return true;
         }
      }
   }
}
