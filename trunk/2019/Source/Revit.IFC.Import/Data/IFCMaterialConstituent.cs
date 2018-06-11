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
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Class representing IFCMaterialConstituent
   /// </summary>
   public class IFCMaterialConstituent : IFCEntity, IIFCMaterialSelect
   {
      string m_Name = null;
      string m_Description = null;
      IFCMaterial m_Material = null;
      double? m_Fraction = null;
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
      public double? Fraction
      {
         get { return m_Fraction; }
         protected set { m_Fraction = value; }
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

      protected IFCMaterialConstituent()
      {
      }

      protected IFCMaterialConstituent(IFCAnyHandle ifcMaterialConstituent)
      {
         Process(ifcMaterialConstituent);
      }

      protected override void Process(IFCAnyHandle ifcMaterialConstituent)
      {
         base.Process(ifcMaterialConstituent);

         IFCAnyHandle ifcMaterial = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcMaterialConstituent, "Material", true);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMaterial))
            Material = IFCMaterial.ProcessIFCMaterial(ifcMaterial);

         Name = IFCImportHandleUtil.GetOptionalStringAttribute(ifcMaterialConstituent, "Name", null);
         Description = IFCImportHandleUtil.GetOptionalStringAttribute(ifcMaterialConstituent, "Description", null);
         double fraction = IFCImportHandleUtil.GetOptionalRealAttribute(ifcMaterialConstituent, "Fraction", -1);
         if (fraction >= 0)
            Fraction = fraction;
         Category = IFCImportHandleUtil.GetOptionalStringAttribute(ifcMaterialConstituent, "Category", null);
         return;
      }

      /// <summary>
      /// Process an IFCMaterialConstituent entity
      /// </summary>
      /// <param name="ifcMaterialConstituent">the material constituent</param>
      /// <returns>returns IFCMaterialCOnstituent object</returns>
      public static IFCMaterialConstituent ProcessIFCMaterialConstituent(IFCAnyHandle ifcMaterialConstituent)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMaterialConstituent))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcMaterialConstituent);
            return null;
         }

         IFCEntity materialConstituent;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcMaterialConstituent.StepId, out materialConstituent))
            materialConstituent = new IFCMaterialConstituent(ifcMaterialConstituent);
         return (materialConstituent as IFCMaterialConstituent);
      }
   }
}