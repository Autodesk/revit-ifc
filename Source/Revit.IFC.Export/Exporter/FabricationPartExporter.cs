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

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Export.Utility;
using System;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export a Fabrication part element.
   /// </summary>
   class FabricationPartExporter
   {
      /// <summary>
      /// Exports an element as a covering of type insulation.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      /// <returns>True if exported successfully, false otherwise.</returns>
      public static bool ExportFabricationInsulationOrLining(ExporterIFC exporterIFC, FabricationPartInsulationLining element,
          GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         // Let the method IFCCategoryTemplate::getDefaultMapping take care of the fabrication part insulation/lining export.
         return GenericElementExporter.ExportElement(exporterIFC, element, geometryElement, productWrapper);
      }

      /// <summary>
      /// Exports a fabrication part element.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="fabPart">The fabrication part.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      /// <returns>True if exported successfully, false otherwise.</returns>
      public static bool ExportFabricationPart(ExporterIFC exporterIFC, FabricationPart fabPart,
          GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         ElementId fabCategory = CategoryUtil.GetSafeCategoryId(fabPart);
         if (fabCategory != new ElementId(BuiltInCategory.OST_FabricationDuctwork) && fabCategory != new ElementId(BuiltInCategory.OST_FabricationPipework))
         {
            // Let the containment and stiffener parts be handled by the generic exporter.
            return GenericElementExporter.ExportElement(exporterIFC, fabPart, geometryElement, productWrapper);
         }

         string ifcEnumType;
         // Let the GetExportTypeImpl method return the correct export type for fabrication straights and fittings.
         IFCExportInfoPair exportType = ExporterUtil.GetProductExportType(fabPart, out ifcEnumType);



         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         IFCEntityType elementClassTypeEnum;
         if (Enum.TryParse(exportType.ExportInstance.ToString(), out elementClassTypeEnum)
            || Enum.TryParse(exportType.ExportType.ToString(), out elementClassTypeEnum))
         {
            if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
               return false;
         }

         bool exported = GenericMEPExporter.Export(exporterIFC, fabPart, geometryElement, exportType, ifcEnumType, productWrapper);
         if (exported)
         {
            // Similar as the design elements, we will add a IfcRelCoversBldgElements for the fabrication parts during the end of export.
            ExporterCacheManager.MEPCache.CoveredElementsCache[fabPart.Id] = fabCategory;
         }
         return exported;
      }
   }
}