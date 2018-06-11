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

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   ///    The description calculator for the COBIE BaseQuantities from a spatial element
   ///    to its associated level.
   /// </summary>
   class SpaceLevelDescriptionCalculator : DescriptionCalculator
   {
      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static SpaceLevelDescriptionCalculator s_Instance = new SpaceLevelDescriptionCalculator();

      /// <summary>
      /// The SpaceLevelDescriptionCalculator instance.
      /// </summary>
      public static SpaceLevelDescriptionCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Finds the spatial element to its associated level.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <returns>
      /// The handle.
      /// </returns>
      public override IFCAnyHandle RedirectDescription(ExporterIFC exporterIFC, Element element)
      {
         SpatialElement spatialElem = element as SpatialElement;
         if (spatialElem != null)
         {
               ElementId levelId = spatialElem.LevelId;
               IFCLevelInfo levelInfo = exporterIFC.GetLevelInfo(levelId);
               if (levelInfo != null)
                  return levelInfo.GetBuildingStorey();
         }

         return null;
      }
   }
}
