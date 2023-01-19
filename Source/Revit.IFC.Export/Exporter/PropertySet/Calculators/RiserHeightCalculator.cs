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
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate riser and tread parameters.
   /// </summary>
   class RiserHeightCalculator : PropertyCalculator
   {
      /// <summary>
      /// An int variable to keep the calculated RiserHeight value.
      /// </summary>
      private double m_RiserHeight = 0.0;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static RiserHeightCalculator s_Instance = new RiserHeightCalculator();

      /// <summary>
      /// The RiserHeightCalculator instance.
      /// </summary>
      public static RiserHeightCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates number of risers for a stair.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="extrusionCreationData">
      /// The IFCExportBodyParams.
      /// </param>
      /// <param name="element">
      /// The element to calculate the value.
      /// </param>
      /// <param name="elementType">
      /// The element type.
      /// </param>
      /// <returns>
      /// True if the operation succeed, false otherwise.
      /// </returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCExportBodyParams extrusionCreationData, Element element, ElementType elementType, EntryMap entryMap)
      {
         bool valid = true;
         if (StairsExporter.IsLegacyStairs(element))
         {
            double treadLength, treadLengthAtInnerSide, nosingLength, waistThickness = 0;
            int numberOfRisers, numberOfTreads = 0;
            ExporterIFCUtils.GetLegacyStairsProperties(exporterIFC, element,
                  out numberOfRisers, out numberOfTreads,
                  out m_RiserHeight, out treadLength, out treadLengthAtInnerSide,
                  out nosingLength, out waistThickness);
         }
         else if (element is Stairs)
         {
            Stairs stairs = element as Stairs;
            m_RiserHeight = UnitUtil.ScaleLength(stairs.ActualRiserHeight);
         }
         else if (element is StairsRun)
         {
            StairsRun stairsRun = element as StairsRun;
            StairsRunType stairsRunType = stairsRun.Document.GetElement(stairsRun.GetTypeId()) as StairsRunType;
            m_RiserHeight = UnitUtil.ScaleLength(stairsRun.Height / stairsRun.ActualRisersNumber);
         }
         else
         {
            valid = false;
         }

         // Get override from parameter
         double riserHeightOverride = 0.0;
         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName, out riserHeightOverride, entryMap.CompatibleRevitParameterName) != null)
         {
            m_RiserHeight = UnitUtil.ScaleArea(riserHeightOverride);
         }

         return valid;
      }

      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>
      /// The double value.
      /// </returns>
      public override double GetDoubleValue()
      {
         return m_RiserHeight;
      }
   }
}
