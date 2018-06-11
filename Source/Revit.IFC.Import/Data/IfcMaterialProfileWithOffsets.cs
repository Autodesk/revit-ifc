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
   public class IFCMaterialProfileWithOffsets : IFCMaterialProfile, IIFCMaterialSelect
   {
      IList<double> m_OffsetValues = null;

      public IList<double> OffsetValues
      {
         get { return m_OffsetValues; }
         protected set { m_OffsetValues = value; }
      }

      protected IFCMaterialProfileWithOffsets()
      {
      }

      protected IFCMaterialProfileWithOffsets(IFCAnyHandle ifcMaterialProfileWithOffsets)
      {
         Process(ifcMaterialProfileWithOffsets);
      }

      protected override void Process(IFCAnyHandle ifcMaterialProfileWithOffsets)
      {
         base.Process(ifcMaterialProfileWithOffsets);

         OffsetValues = IFCAnyHandleUtil.GetAggregateDoubleAttribute<List<double>>(ifcMaterialProfileWithOffsets, "OffsetValues");
      }

      public static IFCMaterialProfileWithOffsets ProcessIFCMaterialProfileWithOffsets(IFCAnyHandle ifcMaterialProfileWithOffsets)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMaterialProfileWithOffsets))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcMaterialProfileWithOffsets);
            return null;
         }

         IFCEntity materialProfileWithOffsets;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcMaterialProfileWithOffsets.StepId, out materialProfileWithOffsets))
            materialProfileWithOffsets = new IFCMaterialProfileWithOffsets(ifcMaterialProfileWithOffsets);
         return (materialProfileWithOffsets as IFCMaterialProfileWithOffsets);
      }
   }
}