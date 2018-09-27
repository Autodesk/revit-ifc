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

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate nosing length parameters.
   /// </summary>
   class NosingLengthCalculator : PropertyCalculator
   {
      /// <summary>
      /// An int variable to keep the calculated NosingLength value.
      /// </summary>
      private double m_NosingLength = 0.0;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static NosingLengthCalculator s_Instance = new NosingLengthCalculator();

      /// <summary>
      /// The StairNumberOfRisersCalculator instance.
      /// </summary>
      public static NosingLengthCalculator Instance
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
         double riserHeight, treadLength, treadLengthAtInnerSide, waistThickness = 0;
         int numberOfRisers, numberOfTreads = 0;
         ExporterIFCUtils.GetLegacyStairsProperties(exporterIFC, element,
               out numberOfRisers, out numberOfTreads,
               out riserHeight, out treadLength, out treadLengthAtInnerSide,
               out m_NosingLength, out waistThickness);
         }
         else if (element is Stairs)
         {
            Stairs stairs = element as Stairs;
            ICollection<ElementId> stairRuns = stairs.GetStairsRuns();
            if (stairRuns.Count > 0)
            {
               // Get the run width from one of the run/flight to compute the walking line offset
               StairsRun run = stairs.Document.GetElement(stairRuns.First()) as StairsRun;
               StairsRunType stairsRunType = run.Document.GetElement(run.GetTypeId()) as StairsRunType;
               m_NosingLength = UnitUtil.ScaleLength(stairsRunType.NosingLength);
            }
         }
         else if (element is StairsRun)
         {
            StairsRun stairsRun = element as StairsRun;
            StairsRunType stairsRunType = stairsRun.Document.GetElement(stairsRun.GetTypeId()) as StairsRunType;
            m_NosingLength = UnitUtil.ScaleLength(stairsRunType.NosingLength);
         }
         else
         {
            valid = false;
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
         return m_NosingLength;

      }
   }
}
