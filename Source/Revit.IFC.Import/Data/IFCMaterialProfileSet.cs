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
   /// Class to represent IfcMaterialProfileSet
   /// </summary>
   public class IFCMaterialProfileSet : IFCEntity, IIFCMaterialSelect
   {
      string m_Name = null;
      string m_Description = null;
      IList<IFCMaterialProfile> m_MaterialProfileSet = null;
      IFCCompositeProfile m_CompositeProfile = null;

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
      /// Get the associated list of MaterialProfiles
      /// </summary>
      public IList<IFCMaterialProfile> MaterialProfileSet
      {
         get
         {
            if (m_MaterialProfileSet == null)
               m_MaterialProfileSet = new List<IFCMaterialProfile>();
            return m_MaterialProfileSet;
         }
      }

      /// <summary>
      /// Get the associated optional IfcCompositeCurve
      /// </summary>
      public IFCCompositeProfile CompositeProfile
      {
         get { return m_CompositeProfile; }
         protected set { m_CompositeProfile = value; }
      }

      public void Create(Document doc)
      {
         foreach (IFCMaterialProfile materialprofile in MaterialProfileSet)
            materialprofile.Create(doc);
      }

      /// <summary>
      /// Get the list of associated Materials
      /// </summary>
      /// <returns></returns>
      public IList<IFCMaterial> GetMaterials()
      {
         HashSet<IFCMaterial> materials = new HashSet<IFCMaterial>();
         foreach (IFCMaterialProfile materialProfile in MaterialProfileSet)
         {
            IList<IFCMaterial> profileMaterials = materialProfile.GetMaterials();
            foreach (IFCMaterial material in profileMaterials)
               materials.Add(material);
         }
         return materials.ToList();
      }

      protected IFCMaterialProfileSet()
      {
      }

      protected IFCMaterialProfileSet(IFCAnyHandle ifcMaterialProfileSet)
      {
         Process(ifcMaterialProfileSet);
      }

      protected override void Process(IFCAnyHandle ifcMaterialProfileSet)
      {
         base.Process(ifcMaterialProfileSet);

         IList<IFCAnyHandle> ifcMaterialProfiles =
             IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcMaterialProfileSet, "MaterialProfiles");
         if (ifcMaterialProfiles == null)
         {
            Importer.TheLog.LogError(ifcMaterialProfileSet.Id, "Expected at least 1 MaterialProfile, found none.", false);
            return;
         }

         foreach (IFCAnyHandle ifcMaterialProfile in ifcMaterialProfiles)
         {
            IFCMaterialProfile materialProfile = null;
            if (IFCAnyHandleUtil.IsTypeOf(ifcMaterialProfile, IFCEntityType.IfcMaterialProfileWithOffsets))
               materialProfile = IFCMaterialProfileWithOffsets.ProcessIFCMaterialProfileWithOffsets(ifcMaterialProfile);
            else
               materialProfile = IFCMaterialProfile.ProcessIFCMaterialProfile(ifcMaterialProfile);

            if (materialProfile != null)
               MaterialProfileSet.Add(materialProfile);
         }

         Name = IFCImportHandleUtil.GetOptionalStringAttribute(ifcMaterialProfileSet, "Name", null);
         Description = IFCImportHandleUtil.GetOptionalStringAttribute(ifcMaterialProfileSet, "Description", null);
         IFCAnyHandle compositeProfileHnd = IFCImportHandleUtil.GetOptionalInstanceAttribute(ifcMaterialProfileSet, "CompositeProfile");
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(compositeProfileHnd))
            CompositeProfile = IFCCompositeProfile.ProcessIFCCompositeProfile(compositeProfileHnd);
      }

      /// <summary>
      /// Processes an IFCMaterialProfileSet entity.
      /// </summary>
      /// <param name="IFCMaterialProfileSet">The IFCMaterialProfileSet handle.</param>
      /// <returns>The IFCMaterialProfileSet object.</returns>
      public static IFCMaterialProfileSet ProcessIFCMaterialProfileSet(IFCAnyHandle ifcMaterialProfileSet)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMaterialProfileSet))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcMaterialProfileSet);
            return null;
         }

         IFCEntity materialProfileSet;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcMaterialProfileSet.StepId, out materialProfileSet))
            materialProfileSet = new IFCMaterialProfileSet(ifcMaterialProfileSet);
         return (materialProfileSet as IFCMaterialProfileSet);
      }
   }
}