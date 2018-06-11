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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public class IFCMaterialProfileSetUsageTapering : IFCMaterialProfileSetUsage, IIFCMaterialSelect
   {
      IFCMaterialProfileSet m_ForProfileEndSet = null;
      int? m_CardinalEndPoint = null;

      /// <summary>
      /// Get the associated IfcMaterialProfileSet
      /// </summary>
      public IFCMaterialProfileSet ForProfileEndSet
      {
         get { return m_ForProfileEndSet; }
         protected set { m_ForProfileEndSet = value; }
      }

      /// <summary>
      /// Get the optional attribute CardinalPoint
      /// </summary>
      public int? CardinalEndPoint
      {
         get { return m_CardinalEndPoint; }
         protected set { m_CardinalEndPoint = value; }
      }

      protected IFCMaterialProfileSetUsageTapering()
      {
      }

      protected IFCMaterialProfileSetUsageTapering(IFCAnyHandle ifcMaterialProfileSetUsageTapering)
      {
         Process(ifcMaterialProfileSetUsageTapering);
      }

      protected override void Process(IFCAnyHandle ifcMaterialProfileSetUsageTapering)
      {
         base.Process(ifcMaterialProfileSetUsageTapering);

         IFCAnyHandle ifcMaterialProfileSet = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcMaterialProfileSetUsageTapering, "ForProfileEndSet", true);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMaterialProfileSet))
            ForProfileSet = IFCMaterialProfileSet.ProcessIFCMaterialProfileSet(ifcMaterialProfileSet);

         bool found = false;
         CardinalPoint = IFCImportHandleUtil.GetOptionalIntegerAttribute(ifcMaterialProfileSetUsageTapering, "CardinalEndPoint", out found);
      }

      public static IFCMaterialProfileSetUsage ProcessIFCMaterialProfileSetUsageTapering(IFCAnyHandle ifcMaterialProfileSetUsageTapering)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMaterialProfileSetUsageTapering))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcMaterialProfileSetUsageTapering);
            return null;
         }

         IFCEntity materialProfileSetUsageTapering;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcMaterialProfileSetUsageTapering.StepId, out materialProfileSetUsageTapering))
            materialProfileSetUsageTapering = new IFCMaterialProfileSetUsageTapering(ifcMaterialProfileSetUsageTapering);
         return (materialProfileSetUsageTapering as IFCMaterialProfileSetUsageTapering);
      }
   }
}