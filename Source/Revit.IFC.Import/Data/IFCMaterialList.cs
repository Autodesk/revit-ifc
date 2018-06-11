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
using System.Linq;
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
   /// Class to represent IfcMaterialList.
   /// </summary>
   public class IFCMaterialList : IFCEntity, IIFCMaterialSelect
   {
      IList<IFCMaterial> m_Materials = null;

      /// <summary>
      /// Get the associated list of IFCMaterialLayers.
      /// </summary>
      public IList<IFCMaterial> Materials
      {
         get
         {
            if (m_Materials == null)
               m_Materials = new List<IFCMaterial>();
            return m_Materials;
         }

      }

      /// <summary>
      /// Return the material list for this IFCMaterialSelect.
      /// </summary>
      public IList<IFCMaterial> GetMaterials()
      {
         return Materials;
      }

      protected IFCMaterialList()
      {
      }

      protected IFCMaterialList(IFCAnyHandle ifcMaterialList)
      {
         Process(ifcMaterialList);
      }

      protected override void Process(IFCAnyHandle ifcMaterialList)
      {
         base.Process(ifcMaterialList);

         IList<IFCAnyHandle> ifcMaterials =
             IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcMaterialList, "Materials");
         if (ifcMaterials == null)
         {
            Importer.TheLog.LogError(ifcMaterialList.Id, "Expected at least 1 IfcMaterial, found none.", false);
            return;
         }

         foreach (IFCAnyHandle ifcMaterial in ifcMaterials)
         {
            IFCMaterial material = IFCMaterial.ProcessIFCMaterial(ifcMaterial);
            if (material != null)
               Materials.Add(material);
         }
      }

      /// <summary>
      /// Create the contained materials within the IfcMaterialList.
      /// </summary>
      /// <param name="doc">The document.</param>
      public void Create(Document doc)
      {
         foreach (IFCMaterial material in Materials)
            material.Create(doc);
      }

      /// <summary>
      /// Processes an IfcMaterialList entity.
      /// </summary>
      /// <param name="ifcMaterialList">The IfcMaterialList handle.</param>
      /// <returns>The IFCMaterialList object.</returns>
      public static IFCMaterialList ProcessIFCMaterialList(IFCAnyHandle ifcMaterialList)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMaterialList))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcMaterialList);
            return null;
         }

         IFCEntity materialList;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcMaterialList.StepId, out materialList))
            materialList = new IFCMaterialList(ifcMaterialList);
         return (materialList as IFCMaterialList);
      }
   }
}