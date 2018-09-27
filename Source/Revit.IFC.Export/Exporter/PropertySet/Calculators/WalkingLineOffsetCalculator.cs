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
   /// A calculation class to calculate walking line offset parameters.
   /// </summary>
   class WalkingLineOffsetCalculator : PropertyCalculator
   {
      /// <summary>
      /// An int variable to keep the calculated WalkingLineOffset value.
      /// </summary>
      private double m_WalkingLineOffset = 0.0;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static WalkingLineOffsetCalculator s_Instance = new WalkingLineOffsetCalculator();

      /// <summary>
      /// The WalkingLineOffsetCalculator instance.
      /// </summary>
      public static WalkingLineOffsetCalculator Instance
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
      /// The IFCExtrusionCreationData.
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
      public override bool Calculate(ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element, ElementType elementType)
      {
         bool valid = true;
         if (StairsExporter.IsLegacyStairs(element))
         {
            double riserHeight, treadLength, treadLengthAtInnerSide, nosingLength, waistThickness = 0;
            int numberOfRisers, numberOfTreads = 0;
            ExporterIFCUtils.GetLegacyStairsProperties(exporterIFC, element,
                  out numberOfRisers, out numberOfTreads,
                  out riserHeight, out treadLength, out treadLengthAtInnerSide,
                  out nosingLength, out waistThickness);
            m_WalkingLineOffset = waistThickness / 2.0;        // !! The waist thickness seems to be wrongly associated with the width of the run!
         }
         else if (element is Stairs)
         {
            Stairs stairs = element as Stairs;
            ICollection<ElementId> stairRuns = stairs.GetStairsRuns();
            if (stairRuns.Count > 0)
            {
               // Get the run width from one of the run/flight to compute the walking line offset
               StairsRun run = stairs.Document.GetElement(stairRuns.First()) as StairsRun;
               m_WalkingLineOffset = run.ActualRunWidth / 2.0;
            }
         }
         else if (element is StairsRun)
         {
            StairsRun stairsRun = element as StairsRun;
            StairsRunType stairsRunType = stairsRun.Document.GetElement(stairsRun.GetTypeId()) as StairsRunType;
            m_WalkingLineOffset = stairsRun.ActualRunWidth / 2.0;
         }
         else
         {
            valid = false;
         }

         // Get override from parameter
         double walkingLineOffsetOverride = 0.0;
         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(element, "WalkingLineOffset", out walkingLineOffsetOverride) != null)
         {
            m_WalkingLineOffset = UnitUtil.ScaleArea(walkingLineOffsetOverride);
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
         return m_WalkingLineOffset;
      }
   }
}
