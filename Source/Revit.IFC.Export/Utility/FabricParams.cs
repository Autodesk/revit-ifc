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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep the properties of a FabricSheet for use in Pset_ReinforcingMeshCommon.
   /// </summary>
   public class FabricParams
   {
      /// <summary>
      /// The steel grade attribute.
      /// </summary>
      public string SteelGrade { get; private set; } = string.Empty;

      /// <summary>
      /// The mesh length attribute.
      /// </summary>
      public double MeshLength { get; private set; } = 0.0;

      /// <summary>
      /// The mesh width attribute.
      /// </summary>
      public double MeshWidth { get; private set; } = 0.0;

      /// <summary>
      /// The longitudinal bar nominal diameter attribute.
      /// </summary>
      public double LongitudinalBarNominalDiameter { get; private set; } = 0.0;

      /// <summary>
      /// The transverse bar nominal diameter attribute.
      /// </summary>
      public double TransverseBarNominalDiameter { get; private set; } = 0.0;

      /// <summary>
      /// The longitudinal bar cross section area attribute.
      /// </summary>
      public double LongitudinalBarCrossSectionArea { get; private set; } = 0.0;

      /// <summary>
      /// The transverse bar cross section area attribute.
      /// </summary>
      public double TransverseBarCrossSectionArea { get; private set; } = 0.0;

      /// <summary>
      /// The longitudinal bar spacing attribute.
      /// </summary>
      public double LongitudinalBarSpacing { get; private set; } = 0.0;

      /// <summary>
      /// The transverse bar spacing attribute.
      /// </summary>
      public double TransverseBarSpacing { get; private set; } = 0.0;

      private void GetFabricSheetParams(FabricSheet sheet)
      {
         if (sheet == null)
            return;

         MeshLength = sheet.CutOverallLength;
         MeshWidth = sheet.CutOverallWidth;

         if (sheet == null)
            return;

         Document doc = sheet.Document;
         Element fabricSheetTypeElem = doc?.GetElement(sheet.GetTypeId());
         FabricSheetType fabricSheetType = fabricSheetTypeElem as FabricSheetType;
         SteelGrade = NamingUtil.GetOverrideStringValue(sheet, "SteelGrade", null);

         Element majorFabricWireTypeElem = doc?.GetElement(fabricSheetType?.MajorDirectionWireType);
         FabricWireType majorFabricWireType = (majorFabricWireTypeElem == null) ? null : (majorFabricWireTypeElem as FabricWireType);
         if (majorFabricWireType != null)
         {
            LongitudinalBarNominalDiameter = UnitUtil.ScaleLength(majorFabricWireType.WireDiameter);
            double localRadius = LongitudinalBarNominalDiameter / 2.0;
            LongitudinalBarCrossSectionArea = localRadius * localRadius * Math.PI;
         }

         Element minorFabricWireTypeElem = doc?.GetElement(fabricSheetType?.MinorDirectionWireType);
         FabricWireType minorFabricWireType = (minorFabricWireTypeElem == null) ? null : (minorFabricWireTypeElem as FabricWireType);
         if (minorFabricWireType != null)
         {
            TransverseBarNominalDiameter = UnitUtil.ScaleLength(minorFabricWireType.WireDiameter);
            double localRadius = TransverseBarNominalDiameter / 2.0;
            TransverseBarCrossSectionArea = localRadius * localRadius * Math.PI;
         }

         LongitudinalBarSpacing = UnitUtil.ScaleLength(fabricSheetType.MajorSpacing);
         TransverseBarSpacing = UnitUtil.ScaleLength(fabricSheetType.MinorSpacing);
      }

      /// <summary>
      /// The constructor.  Populates the parameters needed for Pset_ReinforcementMeshCommon.
      /// </summary>
      /// <param name="fabricSheet">The Revit fabric sheet.</param>
      public FabricParams(FabricSheet fabricSheet)
      {
         GetFabricSheetParams(fabricSheet);
      }
   }
}