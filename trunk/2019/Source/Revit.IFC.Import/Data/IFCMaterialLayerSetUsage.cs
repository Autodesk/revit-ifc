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
   /// Class that mimics IfcMaterialLayerSetUsage.
   /// </summary>
   /// <remarks>This class is fairly complex in its behavior; more information can be found at:
   /// http://www.buildingsmart-tech.org/ifc/IFC4/final/html/index.htm, section 8.10.3.8.
   /// </remarks>
   public class IFCMaterialLayerSetUsage : IFCEntity, IIFCMaterialSelect
   {
      IFCMaterialLayerSet m_MaterialLayerSet = null;

      IFCLayerSetDirection m_Direction = IFCLayerSetDirection.Axis3;

      IFCDirectionSense m_DirectionSense = IFCDirectionSense.Positive;

      double m_Offset = 0.0;

      /// <summary>
      /// Get the associated IFCMaterialLayerSet.
      /// </summary>
      public IFCMaterialLayerSet MaterialLayerSet
      {
         get { return m_MaterialLayerSet; }
         protected set { m_MaterialLayerSet = value; }
      }

      /// <summary>
      /// Get the associated IFCLayerSetDirection enum.
      /// </summary>
      public IFCLayerSetDirection Direction
      {
         get { return m_Direction; }
         protected set { m_Direction = value; }
      }

      /// <summary>
      /// Get the associated IFCDirectionSense enum.
      /// </summary>
      public IFCDirectionSense DirectionSense
      {
         get { return m_DirectionSense; }
         protected set { m_DirectionSense = value; }
      }

      /// <summary>
      /// Get the associated OffsetFromReferenceLine value.
      /// </summary>
      public double Offset
      {
         get { return m_Offset; }
         protected set { m_Offset = value; }
      }

      /// <summary>
      /// Return the material list for this IFCMaterialSelect.
      /// </summary>
      public IList<IFCMaterial> GetMaterials()
      {
         if (MaterialLayerSet == null)
            return new List<IFCMaterial>();

         return MaterialLayerSet.GetMaterials();
      }

      protected IFCMaterialLayerSetUsage()
      {
      }

      protected IFCMaterialLayerSetUsage(IFCAnyHandle ifcMaterialLayerSetUsage)
      {
         Process(ifcMaterialLayerSetUsage);
      }

      protected override void Process(IFCAnyHandle ifcMaterialLayerSetUsage)
      {
         base.Process(ifcMaterialLayerSetUsage);

         IFCAnyHandle ifcMaterialLayerSet =
             IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcMaterialLayerSetUsage, "ForLayerSet", true);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMaterialLayerSet))
            MaterialLayerSet = IFCMaterialLayerSet.ProcessIFCMaterialLayerSet(ifcMaterialLayerSet);

         Direction = IFCEnums.GetSafeEnumerationAttribute(ifcMaterialLayerSetUsage, "LayerSetDirection", IFCLayerSetDirection.Axis3);

         DirectionSense = IFCEnums.GetSafeEnumerationAttribute(ifcMaterialLayerSetUsage, "DirectionSense", IFCDirectionSense.Positive);

         bool found = false;
         Offset = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(ifcMaterialLayerSetUsage, "OffsetFromReferenceLine", out found);
         if (!found)
            Importer.TheLog.LogWarning(ifcMaterialLayerSetUsage.StepId, "No Offset defined, defaulting to 0.", false);
      }

      /// <summary>
      /// Create the contained materials within the IfcMaterialLayerSetUsage.
      /// </summary>
      /// <param name="doc">The document.</param>
      public void Create(Document doc)
      {
         if (MaterialLayerSet != null)
            MaterialLayerSet.Create(doc);
      }

      /// <summary>
      /// Processes an IfcMaterialLayerSetUsage entity.
      /// </summary>
      /// <param name="ifcMaterialLayerSetUsage">The IfcMaterialLayerSetUsage handle.</param>
      /// <returns>The IFCMaterialLayerSetUsage object.</returns>
      public static IFCMaterialLayerSetUsage ProcessIFCMaterialLayerSetUsage(IFCAnyHandle ifcMaterialLayerSetUsage)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMaterialLayerSetUsage))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcMaterialLayerSetUsage);
            return null;
         }

         IFCEntity materialLayerSetUsage;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcMaterialLayerSetUsage.StepId, out materialLayerSetUsage))
            materialLayerSetUsage = new IFCMaterialLayerSetUsage(ifcMaterialLayerSetUsage);
         return (materialLayerSetUsage as IFCMaterialLayerSetUsage);
      }
   }
}