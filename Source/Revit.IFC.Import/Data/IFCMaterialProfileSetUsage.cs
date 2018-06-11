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
   /// <summary>
   /// Class that mimics IfcMaterialProfileSetUsage
   /// </summary>
   public class IFCMaterialProfileSetUsage : IFCEntity, IIFCMaterialSelect
   {
      IFCMaterialProfileSet m_ForProfileSet = null;
      int? m_CardinalPoint = null;
      double? m_ReferenceExtent = null;

      /// <summary>
      /// Get the associated IfcMaterialProfileSet
      /// </summary>
      public IFCMaterialProfileSet ForProfileSet
      {
         get { return m_ForProfileSet; }
         protected set { m_ForProfileSet = value; }
      }

      /// <summary>
      /// Get the optional attribute CardinalPoint
      /// </summary>
      public int? CardinalPoint
      {
         get { return m_CardinalPoint; }
         protected set { m_CardinalPoint = value; }
      }

      /// <summary>
      /// Get the optional attribute ReferenceExtent
      /// </summary>
      public double? ReferenceExtent
      {
         get { return m_ReferenceExtent; }
         protected set { m_ReferenceExtent = value; }
      }

      /// <summary>
      /// Create the IFCProfileSet in the document
      /// </summary>
      /// <param name="doc"></param>
      public void Create(Document doc)
      {
         if (ForProfileSet != null)
            ForProfileSet.Create(doc);
      }

      /// <summary>
      /// Get list of materials
      /// </summary>
      /// <returns></returns>
      public IList<IFCMaterial> GetMaterials()
      {
         if (ForProfileSet == null)
            return new List<IFCMaterial>();

         return ForProfileSet.GetMaterials();
      }

      protected IFCMaterialProfileSetUsage()
      {
      }

      protected IFCMaterialProfileSetUsage(IFCAnyHandle ifcMaterialProfileSetUsage)
      {
         Process(ifcMaterialProfileSetUsage);
      }

      protected override void Process(IFCAnyHandle ifcMaterialProfileSetUsage)
      {
         base.Process(ifcMaterialProfileSetUsage);

         IFCAnyHandle ifcMaterialProfileSet = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcMaterialProfileSetUsage, "ForProfileSet", true);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMaterialProfileSet))
            ForProfileSet = IFCMaterialProfileSet.ProcessIFCMaterialProfileSet(ifcMaterialProfileSet);

         bool found = false;
         CardinalPoint = IFCImportHandleUtil.GetOptionalIntegerAttribute(ifcMaterialProfileSetUsage, "CardinalPoint", out found);
         ReferenceExtent = IFCImportHandleUtil.GetOptionalDoubleAttribute(ifcMaterialProfileSetUsage, "ReferenceExtent", 0);
      }

      /// <summary>
      /// Process an IFCMaterialProfileSetUsage
      /// </summary>
      /// <param name="ifcMaterialProfileSetUsage">the IfcMaterialProfileSetUsage handle</param>
      /// <returns>returns an IFCMaterialProfileSetUsage object</returns>
      public static IFCMaterialProfileSetUsage ProcessIFCMaterialProfileSetUsage(IFCAnyHandle ifcMaterialProfileSetUsage)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMaterialProfileSetUsage))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcMaterialProfileSetUsage);
            return null;
         }

         IFCEntity materialProfileSetUsage;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcMaterialProfileSetUsage.StepId, out materialProfileSetUsage))
            materialProfileSetUsage = new IFCMaterialProfileSetUsage(ifcMaterialProfileSetUsage);
         return (materialProfileSetUsage as IFCMaterialProfileSetUsage);
      }
   }
}