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
   /// A calculation class to calculate tread length parameters.
   /// </summary>
   class TreadLengthCalculator : PropertyCalculator
   {
      /// <summary>
      /// An int variable to keep the calculated TreadLength value.
      /// </summary>
      private double m_TreadLength = 0.0;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static TreadLengthCalculator s_Instance = new TreadLengthCalculator();

      /// <summary>
      /// The TreadLengthCalculator instance.
      /// </summary>
      public static TreadLengthCalculator Instance
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
      public override bool Calculate(ExporterIFC exporterIFC, IFCAnyHandle handle, IFCExportBodyParams extrusionCreationData, Element element, ElementType elementType, EntryMap entryMap)
      {
         // Get override from parameter
         if (ParameterUtil.TryGetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName, entryMap.CompatibleRevitParameterName) is double treadLengthOverride)
         {
            m_TreadLength = UnitUtil.ScaleArea(treadLengthOverride);
            return true;
         }

         if (StairsExporter.IsLegacyStairs(element))
         {
            double riserHeight, treadLengthAtInnerSide, nosingLength, waistThickness = 0;
            int numberOfRisers, numberOfTreads = 0;
            ExporterIFCUtils.GetLegacyStairsProperties(exporterIFC, element,
                  out numberOfRisers, out numberOfTreads,
                  out riserHeight, out m_TreadLength, out treadLengthAtInnerSide,
                  out nosingLength, out waistThickness);
         }
         else if (element is Stairs)
         {
            Stairs stairs = element as Stairs;
            m_TreadLength = UnitUtil.ScaleLength(stairs.ActualTreadDepth);
         }
         else if (element is StairsRun)
         {
            StairsRun stairsRun = element as StairsRun;
            StairsRunType stairsRunType = stairsRun.Document.GetElement(stairsRun.GetTypeId()) as StairsRunType;
            Stairs stairs = stairsRun.GetStairs();

            m_TreadLength = UnitUtil.ScaleLength(stairs.ActualTreadDepth);
         }
         else
         {
            return false;
         }

         return true;
      }

      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>
      /// The double value.
      /// </returns>
      public override double GetDoubleValue()
      {
         return m_TreadLength;
      }
   }
}
