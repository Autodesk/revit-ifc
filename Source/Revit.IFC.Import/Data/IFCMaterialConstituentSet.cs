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
   /// Class representing IFCMaterialConstituentSet
   /// </summary>
   public class IFCMaterialConstituentSet : IFCEntity, IIFCMaterialSelect
   {
      string m_Name = null;
      string m_Description = null;
      IList<IFCMaterialConstituent> m_MaterialConstituents = null;

      /// <summary>
      /// Get the Name attribute
      /// </summary>
      public string Name
      {
         get { return m_Name; }
         protected set { m_Name = value; }
      }

      // Get the Description attribute
      public string Description
      {
         get { return m_Description; }
         protected set { m_Description = value; }
      }

      /// <summary>
      /// Get the associated list of MaterialConstituents
      /// </summary>
      public IList<IFCMaterialConstituent> MaterialConstituents
      {
         get
         {
            if (m_MaterialConstituents == null)
               m_MaterialConstituents = new List<IFCMaterialConstituent>();
            return m_MaterialConstituents;
         }
      }

      public void Create(Document doc)
      {
         foreach (IFCMaterialConstituent materialConstituent in MaterialConstituents)
            materialConstituent.Create(doc);
      }

      /// <summary>
      /// Get the list of associated Materials
      /// </summary>
      /// <returns></returns>
      public IList<IFCMaterial> GetMaterials()
      {
         HashSet<IFCMaterial> materials = new HashSet<IFCMaterial>();
         foreach (IFCMaterialConstituent materialConstituent in MaterialConstituents)
         {
            IList<IFCMaterial> constituentMaterials = materialConstituent.GetMaterials();
            foreach (IFCMaterial material in constituentMaterials)
               materials.Add(material);
         }
         return materials.ToList();
      }

      protected IFCMaterialConstituentSet()
      {
      }

      protected IFCMaterialConstituentSet(IFCAnyHandle ifcMaterialConstituentSet)
      {
         Process(ifcMaterialConstituentSet);
      }

      protected override void Process(IFCAnyHandle ifcMaterialConstituentSet)
      {
         base.Process(ifcMaterialConstituentSet);

         IList<IFCAnyHandle> ifcMaterialConsitutuents =
             IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcMaterialConstituentSet, "MaterialConstituents");
         if (ifcMaterialConsitutuents == null)
         {
            Importer.TheLog.LogError(ifcMaterialConstituentSet.Id, "Expected at least 1 MaterialConsituent, found none.", false);
            return;
         }

         foreach (IFCAnyHandle ifcMaterialConstituent in ifcMaterialConsitutuents)
         {
            IFCMaterialConstituent materialConstituent = IFCMaterialConstituent.ProcessIFCMaterialConstituent(ifcMaterialConstituent);
            if (materialConstituent != null)
               MaterialConstituents.Add(materialConstituent);
         }

         Name = IFCImportHandleUtil.GetOptionalStringAttribute(ifcMaterialConstituentSet, "Name", null);
         Description = IFCImportHandleUtil.GetOptionalStringAttribute(ifcMaterialConstituentSet, "Description", null);
      }

      /// <summary>
      /// Processes an IFCMaterialConstituentSet entity.
      /// </summary>
      /// <param name="IFCMaterialConstituentSet">The IFCMaterialConstituentSet handle.</param>
      /// <returns>The IFCMaterialConstituentSet object.</returns>
      public static IFCMaterialConstituentSet ProcessIFCMaterialConstituentSet(IFCAnyHandle ifcMaterialConstituentSet)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMaterialConstituentSet))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcMaterialConstituentSet);
            return null;
         }

         IFCEntity materialConstituentSet;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcMaterialConstituentSet.StepId, out materialConstituentSet))
            materialConstituentSet = new IFCMaterialConstituentSet(ifcMaterialConstituentSet);
         return (materialConstituentSet as IFCMaterialConstituentSet);
      }
   }
}