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
   /// Class to represent IfcMaterialLayerSet.
   /// </summary>
   public class IFCMaterialLayerSet : IFCEntity, IIFCMaterialSelect
   {
      IList<IFCMaterialLayer> m_MaterialLayers = null;

      string m_LayerSetName = null;

      /// <summary>
      /// Get the associated list of IFCMaterialLayers.
      /// </summary>
      public IList<IFCMaterialLayer> MaterialLayers
      {
         get
         {
            if (m_MaterialLayers == null)
               m_MaterialLayers = new List<IFCMaterialLayer>();
            return m_MaterialLayers;
         }

      }

      /// <summary>
      /// Get the associated optional LayerSetName, if any.
      /// </summary>
      public string LayerSetName
      {
         get { return m_LayerSetName; }
         protected set { m_LayerSetName = value; }
      }

      /// <summary>
      /// Return the material list for this IFCMaterialSelect.
      /// </summary>
      public IList<IFCMaterial> GetMaterials()
      {
         HashSet<IFCMaterial> materials = new HashSet<IFCMaterial>();
         foreach (IFCMaterialLayer materialLayer in MaterialLayers)
         {
            IList<IFCMaterial> layerMaterials = materialLayer.GetMaterials();
            foreach (IFCMaterial material in layerMaterials)
               materials.Add(material);
         }
         return materials.ToList();
      }

      protected IFCMaterialLayerSet()
      {
      }

      protected IFCMaterialLayerSet(IFCAnyHandle ifcMaterialLayerSet)
      {
         Process(ifcMaterialLayerSet);
      }

      protected override void Process(IFCAnyHandle ifcMaterialLayerSet)
      {
         base.Process(ifcMaterialLayerSet);

         IList<IFCAnyHandle> ifcMaterialLayers =
             IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcMaterialLayerSet, "MaterialLayers");
         if (ifcMaterialLayers == null)
         {
            Importer.TheLog.LogError(ifcMaterialLayerSet.Id, "Expected at least 1 IfcMaterialLayer, found none.", false);
            return;
         }

         foreach (IFCAnyHandle ifcMaterialLayer in ifcMaterialLayers)
         {
            IFCMaterialLayer materialLayer = null;
            if (materialLayer is IFCMaterialLayerWithOffsets)
               materialLayer = IFCMaterialLayerWithOffsets.ProcessIFCMaterialLayerWithOffsets(ifcMaterialLayer);
            else
               materialLayer = IFCMaterialLayer.ProcessIFCMaterialLayer(ifcMaterialLayer);

            if (materialLayer != null)
               MaterialLayers.Add(materialLayer);
         }

         LayerSetName = IFCImportHandleUtil.GetOptionalStringAttribute(ifcMaterialLayerSet, "LayerSetName", null);
      }

      /// <summary>
      /// Create the contained materials within the IfcMaterialLayerSet.
      /// </summary>
      /// <param name="doc">The document.</param>
      public void Create(Document doc)
      {
         foreach (IFCMaterialLayer materialLayer in MaterialLayers)
            materialLayer.Create(doc);
      }

      /// <summary>
      /// Processes an IfcMaterialLayerSet entity.
      /// </summary>
      /// <param name="ifcMaterialLayerSet">The IfcMaterialLayerSet handle.</param>
      /// <returns>The IFCMaterialLayerSet object.</returns>
      public static IFCMaterialLayerSet ProcessIFCMaterialLayerSet(IFCAnyHandle ifcMaterialLayerSet)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMaterialLayerSet))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcMaterialLayerSet);
            return null;
         }

         IFCEntity materialLayerSet;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcMaterialLayerSet.StepId, out materialLayerSet))
            materialLayerSet = new IFCMaterialLayerSet(ifcMaterialLayerSet);
         return (materialLayerSet as IFCMaterialLayerSet);
      }
   }
}