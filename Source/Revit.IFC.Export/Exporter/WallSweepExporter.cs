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

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export a wall sweep.
   /// </summary>
   class WallSweepExporter
   {
      /// <summary>
      /// Exports a wall sweep.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="wallSweep">The WallSweep.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void Export(ExporterIFC exporterIFC, WallSweep wallSweep, GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         WallSweepInfo wallSweepInfo = wallSweep.GetWallSweepInfo();
         //Reveals are exported as openings with wall exporter.
         if (wallSweepInfo.WallSweepType == WallSweepType.Reveal)
            return;

         if (!GenericElementExporter.ExportElement(exporterIFC, wallSweep, geometryElement, productWrapper))
            return;

         HostObjectExporter.ExportHostObjectMaterials(exporterIFC, wallSweep, productWrapper.GetAnElement(),
            geometryElement, productWrapper,
            ElementId.InvalidElementId, Toolkit.IFCLayerSetDirection.Axis2, null, null);
      }
   }
}
