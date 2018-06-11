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
   /// Class to represent IfcMaterialLayer.
   /// </summary>
   public class IFCMaterialLayer : IFCEntity, IIFCMaterialSelect
   {
      IFCMaterial m_Material = null;

      double m_LayerThickness = 0.0;

      IFCLogical m_IsVentilated = IFCLogical.False;   // default value - layer is solid material.

      /// <summary>
      /// Get the associated IFCMaterial.
      /// </summary>
      public IFCMaterial Material
      {
         get { return m_Material; }
         protected set { m_Material = value; }
      }

      /// <summary>
      /// Get the associated layer thickness.
      /// </summary>
      public double LayerThickness
      {
         get { return m_LayerThickness; }
         protected set { m_LayerThickness = value; }
      }

      /// <summary>
      /// Get the associated IsVentilated value.
      /// </summary>
      public IFCLogical IsVentilated
      {
         get { return m_IsVentilated; }
         protected set { m_IsVentilated = value; }
      }

      /// <summary>
      /// Returns true if that material layer is an air gap.  This is the case if the material layer is either ventilated,
      /// or the status is unknown (from the IFC2x3 definition).
      /// </summary>
      public bool IsAirGap()
      {
         return (IsVentilated != IFCLogical.False);
      }

      /// <summary>
      /// Return the material list for this IFCMaterialSelect.
      /// </summary>
      public IList<IFCMaterial> GetMaterials()
      {
         IList<IFCMaterial> materials = new List<IFCMaterial>();
         if (Material != null)
            materials.Add(Material);
         return materials;
      }

      protected IFCMaterialLayer()
      {
      }

      protected IFCMaterialLayer(IFCAnyHandle ifcMaterialLayer)
      {
         Process(ifcMaterialLayer);
      }

      protected override void Process(IFCAnyHandle ifcMaterialLayer)
      {
         base.Process(ifcMaterialLayer);

         IFCAnyHandle ifcMaterial = IFCImportHandleUtil.GetOptionalInstanceAttribute(ifcMaterialLayer, "Material");
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMaterial))
            Material = IFCMaterial.ProcessIFCMaterial(ifcMaterial);

         bool found = false;
         LayerThickness = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(ifcMaterialLayer, "LayerThickness", out found);
         if (!found)
            return;

         // GetOptionalLogicalAttribute defaults to Unknown.  We want to default to false here.
         IsVentilated = IFCImportHandleUtil.GetOptionalLogicalAttribute(ifcMaterialLayer, "IsVentilated", out found);
         if (!found)
            IsVentilated = IFCLogical.False;
      }

      public void Create(Document doc)
      {
         if (Material != null)
            Material.Create(doc);
      }

      /// <summary>
      /// Processes an IfcMaterialLayer entity.
      /// </summary>
      /// <param name="ifcMaterialLayer">The IfcMaterialLayer handle.</param>
      /// <returns>The IFCMaterialLayer object.</returns>
      public static IFCMaterialLayer ProcessIFCMaterialLayer(IFCAnyHandle ifcMaterialLayer)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMaterialLayer))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcMaterialLayer);
            return null;
         }

         IFCEntity materialLayer;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcMaterialLayer.StepId, out materialLayer))
            materialLayer = new IFCMaterialLayer(ifcMaterialLayer);
         return (materialLayer as IFCMaterialLayer);
      }
   }
}