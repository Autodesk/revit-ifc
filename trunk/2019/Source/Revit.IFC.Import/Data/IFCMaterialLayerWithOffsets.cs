//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
// Copyright (C) 2013  Autodesk, Inc.
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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;
using Revit.IFC.Import.Properties;

namespace Revit.IFC.Import.Data
{
   public class IFCMaterialLayerWithOffsets : IFCMaterialLayer, IIFCMaterialSelect
   {
      IFCLayerSetDirection m_OffsetDirection = IFCLayerSetDirection.Axis3;
      IList<double> m_OffsetValues = null;

      public IList<double> OffsetValues
      {
         get { return m_OffsetValues; }
         protected set { m_OffsetValues = value; }
      }

      public IFCLayerSetDirection OffsetDirection
      {
         get { return m_OffsetDirection; }
         protected set { m_OffsetDirection = value; }
      }

      protected IFCMaterialLayerWithOffsets()
      {
      }

      protected IFCMaterialLayerWithOffsets(IFCAnyHandle ifcMaterialLayerWithOffsets)
      {
         Process(ifcMaterialLayerWithOffsets);
      }

      protected override void Process(IFCAnyHandle ifcMaterialLayerWithOffsets)
      {
         base.Process(ifcMaterialLayerWithOffsets);

         OffsetValues = IFCAnyHandleUtil.GetAggregateDoubleAttribute<List<double>>(ifcMaterialLayerWithOffsets, "OffsetValues");
         OffsetDirection = (IFCLayerSetDirection)Enum.Parse(typeof(IFCLayerSetDirection), IFCAnyHandleUtil.GetEnumerationAttribute(ifcMaterialLayerWithOffsets, "OffsetDirection"));
      }

      public static IFCMaterialLayerWithOffsets ProcessIFCMaterialLayerWithOffsets(IFCAnyHandle ifcMaterialLayerWithOffsets)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMaterialLayerWithOffsets))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcMaterialLayerWithOffsets);
            return null;
         }

         IFCEntity materialLayerWithOffsets;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcMaterialLayerWithOffsets.StepId, out materialLayerWithOffsets))
            materialLayerWithOffsets = new IFCMaterialLayerWithOffsets(ifcMaterialLayerWithOffsets);
         return (materialLayerWithOffsets as IFCMaterialLayerWithOffsets);
      }
   }
}