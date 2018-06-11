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
   /// Class to represent IFCMaterialProfile
   /// </summary>
   public class IFCMaterialProfile : IFCEntity, IIFCMaterialSelect
   {
      string m_Name = null;
      string m_Description = null;
      IFCProfileDef m_Profile = null;
      IFCMaterial m_Material = null;
      double? m_Priority = null;
      string m_Category = null;

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
      /// Get the associated IFCProfileDef
      /// </summary>
      public IFCProfileDef Profile
      {
         get { return m_Profile; }
         protected set { m_Profile = value; }
      }

      /// <summary>
      /// Get the associated IFCMaterial
      /// </summary>
      public IFCMaterial Material
      {
         get { return m_Material; }
         protected set { m_Material = value; }
      }

      /// <summary>
      /// Get the Priority attribute
      /// </summary>
      public double? Priority
      {
         get { return m_Priority; }
         protected set { m_Priority = value; }
      }

      /// <summary>
      ///  Get the Category attribute
      /// </summary>
      public string Category
      {
         get { return m_Category; }
         set { m_Category = value; }
      }

      public void Create(Document doc)
      {
         if (Material != null)
            Material.Create(doc);
      }

      /// <summary>
      /// Return the material (in list) for this IFCMaterialSelect
      /// </summary>
      /// <returns></returns>
      public IList<IFCMaterial> GetMaterials()
      {
         IList<IFCMaterial> materials = new List<IFCMaterial>();
         if (Material != null)
            materials.Add(Material);
         return materials;
      }

      protected IFCMaterialProfile()
      {
      }

      protected IFCMaterialProfile(IFCAnyHandle ifcMaterialProfile)
      {
         Process(ifcMaterialProfile);
      }

      protected override void Process(IFCAnyHandle ifcMaterialProfile)
      {
         base.Process(ifcMaterialProfile);

         IFCAnyHandle ifcMaterial = IFCImportHandleUtil.GetOptionalInstanceAttribute(ifcMaterialProfile, "Material");
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMaterial))
            Material = IFCMaterial.ProcessIFCMaterial(ifcMaterial);

         IFCAnyHandle profileHnd = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcMaterialProfile, "Profile", true);
         if (profileHnd != null)
            Profile = IFCProfileDef.ProcessIFCProfileDef(profileHnd);

         Name = IFCImportHandleUtil.GetOptionalStringAttribute(ifcMaterialProfile, "Name", null);
         Description = IFCImportHandleUtil.GetOptionalStringAttribute(ifcMaterialProfile, "Description", null);
         double prio = IFCImportHandleUtil.GetOptionalRealAttribute(ifcMaterialProfile, "Priority", -1);
         if (prio >= 0)
            Priority = prio;
         Category = IFCImportHandleUtil.GetOptionalStringAttribute(ifcMaterialProfile, "Category", null);

         return;
      }

      /// <summary>
      /// Process an IFCMaterialProfile entity
      /// </summary>
      /// <param name="ifcMaterialProfile">the matrial profile</param>
      /// <returns>returns a IFCMaterialProfile object</returns>
      public static IFCMaterialProfile ProcessIFCMaterialProfile(IFCAnyHandle ifcMaterialProfile)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMaterialProfile))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcMaterialProfile);
            return null;
         }

         IFCEntity materialProfile;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcMaterialProfile.StepId, out materialProfile))
            materialProfile = new IFCMaterialProfile(ifcMaterialProfile);
         return (materialProfile as IFCMaterialProfile);
      }
   }
}