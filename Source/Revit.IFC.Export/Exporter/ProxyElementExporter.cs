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
   /// Provides methods to export a Revit element as IfcBuildingElementProxy.
   /// </summary>
   class ProxyElementExporter
   {
      /// <summary>
      /// Exports an element as building element proxy.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      /// <returns>True if exported successfully, false otherwise.</returns>
      public static bool Export(ExporterIFC exporterIFC, Element element,
          GeometryElement geometryElement, ProductWrapper productWrapper, IFCExportInfoPair exportType=null)
      {
         bool exported = false;
         if (element == null || geometryElement == null)
            return exported;

         if (exportType == null)
         {
            exportType = new IFCExportInfoPair(IFCEntityType.IfcBuildingElementProxy, IFCEntityType.IfcBuildingElementProxyType, "NOTDEFINED");
         }

         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            exported = (GenericElementExporter.ExportSimpleGenericElement(exporterIFC, element, geometryElement, productWrapper, exportType) != null);
            if (exported)
               tr.Commit();
         }

         return exported;
      }
   }
}