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
   /// A calculation class to calculate riser and tread parameters.
   /// </summary>
   /// <remarks>This is intended as a base class for the various stairs calculators
   /// that use its values.</remarks>
   class StairRiserTreadsCalculator : PropertyCalculator
   {
      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static StairRiserTreadsCalculator s_Instance = new StairRiserTreadsCalculator();

      /// <summary>
      /// The NumberOfTreadsCalculator instance.
      /// </summary>
      public static StairRiserTreadsCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// An int variable to keep the calculated NumberOfRisers value.
      /// </summary>
      public int NumberOfRisers { get; private set; } = 0;

      /// <summary>
      /// An int variable to keep the calculated NumberOfTreads value.
      /// </summary>
      public int NumberOfTreads { get; private set; } = 0;

      /// <summary>
      /// A double variable to keep the calculated RiserHeight value.
      /// </summary>
      public double RiserHeight { get; private set; } = 0.0;

      /// <summary>
      /// A double variable to keep the calculated TreadLength value.
      /// </summary>
      public double TreadLength { get; private set; } = 0.0;

      /// <summary>
      /// A double variable to keep the calculated TreadLengthAtOffset value.
      /// </summary>
      public double TreadLengthAtOffset { get; private set; } = 0.0;

      /// <summary>
      /// A double variable to keep the calculated TreadLength value.
      /// </summary>
      public double TreadLengthAtInnerSide { get; private set; } = 0.0;

      /// <summary>
      /// An int variable to keep the calculated NosingLength value.
      /// </summary>
      public double NosingLength { get; private set; } = 0.0;

      /// <summary>
      /// An int variable to keep the calculated WalkingLineOffset value.
      /// </summary>
      public double WalkingLineOffset { get; private set; } = 0.0;

      /// <summary>
      /// An int variable to keep the calculated WaistThickness value.
      /// </summary>
      public double WaistThickness { get; private set; } = 0.0;

      /// <summary>
      /// The current element whose values are being calculated, cached to reduce number of operations.
      /// </summary>
      private Element CurrentElement { get; set; } = null;

      /// <summary>
      /// Calculates number of risers for a stair.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="extrusionCreationData">The IFCExtrusionCreationData.</param>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="elementType">The element type.</param>
      /// <returns>True if the operation succeed, false otherwise.</returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element, ElementType elementType)
      {
         bool valid = true;
         if (CurrentElement != element)
         {
            CurrentElement = element;
            if (StairsExporter.IsLegacyStairs(element))
            {
               int numberOfRisers, numberOfTreads;
               double riserHeight, treadLength, treadLengthAtInnerSide, nosingLength, waistThickness;

               ExporterIFCUtils.GetLegacyStairsProperties(exporterIFC, element,
                   out numberOfRisers, out numberOfTreads,
                   out riserHeight, out treadLength, out treadLengthAtInnerSide,
                   out nosingLength, out waistThickness);

               NumberOfRisers = numberOfRisers;
               NumberOfTreads = numberOfTreads;
               RiserHeight = riserHeight;
               TreadLength = treadLength;
               TreadLengthAtInnerSide = treadLengthAtInnerSide;
               NosingLength = nosingLength;
               WaistThickness = waistThickness;

               TreadLengthAtOffset = TreadLength;
               WalkingLineOffset = WaistThickness / 2.0;
            }
            else if (element is Stairs)
            {
               Stairs stairs = element as Stairs;
               NumberOfRisers = stairs.ActualRisersNumber;
               NumberOfTreads = stairs.ActualTreadsNumber;
               RiserHeight = UnitUtil.ScaleLength(stairs.ActualRiserHeight);
               TreadLength = UnitUtil.ScaleLength(stairs.ActualTreadDepth);
            }
            else if (element is StairsRun)
            {
               StairsRun stairsRun = element as StairsRun;
               StairsRunType stairsRunType = stairsRun.Document.GetElement(stairsRun.GetTypeId()) as StairsRunType;
               Stairs stairs = stairsRun.GetStairs();
               StairsType stairsType = stairs.Document.GetElement(stairs.GetTypeId()) as StairsType;

               NumberOfRisers = stairs.ActualRisersNumber;
               NumberOfTreads = stairs.ActualTreadsNumber;
               RiserHeight = UnitUtil.ScaleLength(stairs.ActualRiserHeight);
               TreadLength = UnitUtil.ScaleLength(stairs.ActualTreadDepth);
               TreadLengthAtOffset = TreadLength;
               NosingLength = UnitUtil.ScaleLength(stairsRunType.NosingLength);
               WaistThickness = UnitUtil.ScaleLength(stairsRun.ActualRunWidth);
               WalkingLineOffset = WaistThickness / 2.0;

               double treadLengthAtInnerSide;
               if (ParameterUtil.GetDoubleValueFromElement(stairsType,
                   BuiltInParameter.STAIRSTYPE_MINIMUM_TREAD_WIDTH_INSIDE_BOUNDARY, out treadLengthAtInnerSide) != null)
                  TreadLengthAtInnerSide = UnitUtil.ScaleLength(treadLengthAtInnerSide);
               else
                  TreadLengthAtInnerSide = 0.0;
            }
            else
            {
               valid = false;
            }
         }
         return valid;
      }
   }

   /// <summary>
   /// A calculation class to calculate riser and tread parameters.
   /// </summary>
   /// <remarks>This allows us to share the calculated values in StairRiserTreadsCalculator without
   /// doing unnecessary API calls.</remarks>
   class StairRiserTreadsDerivedCalculator : PropertyCalculator
   {
      protected StairRiserTreadsCalculator m_StairRiserTreadsCalculator = StairRiserTreadsCalculator.Instance;

      public override bool Calculate(ExporterIFC exporterIFC, IFCExtrusionCreationData extrusionCreationData, Element element, ElementType elementType)
      {
         return m_StairRiserTreadsCalculator.Calculate(exporterIFC, extrusionCreationData, element, elementType);
      }
   }

   /// <summary>
   /// A calculation class to calculate number of risers.
   /// </summary>
   class NumberOfRiserCalculator : StairRiserTreadsDerivedCalculator
   {
      /// <summary>
      /// Gets the calculated int value.
      /// </summary>
      /// <returns>The int value.</returns>
      public override int GetIntValue()
      {
         return m_StairRiserTreadsCalculator.NumberOfRisers;
      }
   }

   /// <summary>
   /// A calculation class to calculate number of treads.
   /// </summary>
   class NumberOfTreadsCalculator : StairRiserTreadsDerivedCalculator
   {
      /// <summary>
      /// Gets the calculated int value.
      /// </summary>
      /// <returns>The int value.</returns>
      public override int GetIntValue()
      {
         return m_StairRiserTreadsCalculator.NumberOfTreads;
      }
   }

   /// <summary>
   /// A calculation class to calculate nosing length.
   /// </summary>
   class NosingLengthCalculator : StairRiserTreadsDerivedCalculator
   {
      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>The double value.</returns>
      public override double GetDoubleValue()
      {
         return m_StairRiserTreadsCalculator.NosingLength;
      }
   }

   /// <summary>
   /// A calculation class to calculate riser height.
   /// </summary>
   class RiserHeightCalculator : StairRiserTreadsDerivedCalculator
   {
      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>The double value.</returns>
      public override double GetDoubleValue()
      {
         return m_StairRiserTreadsCalculator.RiserHeight;
      }
   }

   /// <summary>
   /// A calculation class to calculate tread length.
   /// </summary>
   class TreadLengthCalculator : StairRiserTreadsDerivedCalculator
   {
      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>The double value.</returns>
      public override double GetDoubleValue()
      {
         return m_StairRiserTreadsCalculator.TreadLength;
      }
   }

   /// <summary>
   /// A calculation class to calculate tread length at inner side.
   /// </summary>
   class TreadLengthAtInnerSideCalculator : StairRiserTreadsDerivedCalculator
   {
      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>The double value.</returns>
      public override double GetDoubleValue()
      {
         return m_StairRiserTreadsCalculator.TreadLengthAtInnerSide;
      }
   }

   /// <summary>
   /// A calculation class to calculate tread length at offset.
   /// </summary>
   class TreadLengthAtOffsetCalculator : StairRiserTreadsDerivedCalculator
   {
      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>The double value.</returns>
      public override double GetDoubleValue()
      {
         return m_StairRiserTreadsCalculator.TreadLengthAtOffset;
      }
   }

   /// <summary>
   /// A calculation class to calculate walking line offset.
   /// </summary>
   class WalkingLineOffsetCalculator : StairRiserTreadsDerivedCalculator
   {
      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>The double value.</returns>
      public override double GetDoubleValue()
      {
         return m_StairRiserTreadsCalculator.WalkingLineOffset;
      }
   }

   /// <summary>
   /// A calculation class to calculate waist thickness.
   /// </summary>
   class WaistThicknessCalculator : StairRiserTreadsDerivedCalculator
   {
      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>The double value.</returns>
      public override double GetDoubleValue()
      {
         return m_StairRiserTreadsCalculator.WaistThickness;
      }
   }
}