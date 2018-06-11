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
using System.Linq;
using System.Text;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcTypeProduct.
   /// </summary>
   public class IFCTypeProduct : IFCTypeObject
   {
      private string m_Tag = null;

      private IList<IFCRepresentationMap> m_RepresentationMaps = new List<IFCRepresentationMap>();

      /// <summary>
      /// The tag.
      /// </summary>
      public string Tag
      {
         get { return m_Tag; }
         protected set { m_Tag = value; }
      }

      /// <summary>
      /// The optional list of RepresentationMaps associated with this IfcTypeProduct.
      /// If an IFCRepresentationMap is contained by only one IFCTypeProduct, we'll do special processing of the IfcRepresentationMap.
      /// </summary>
      public IList<IFCRepresentationMap> RepresentationMaps
      {
         get { return m_RepresentationMaps; }
         protected set { m_RepresentationMaps = value; }
      }

      protected IFCTypeProduct()
      {
      }

      /// <summary>
      /// Constructs an IFCTypeProduct from the IfcTypeProduct handle.
      /// </summary>
      /// <param name="ifcTypeProduct">The IfcTypeProduct handle.</param>
      protected IFCTypeProduct(IFCAnyHandle ifcTypeProduct)
      {
         Process(ifcTypeProduct);
      }


      private void RegisterRepresentationMapWithTypeProject(IFCRepresentationMap representationMap, IFCTypeProduct typeProduct)
      {
         if (representationMap == null || representationMap.MappedRepresentation == null || typeProduct == null)
            return;

         // Note that if the representation map is already in the RepMapToTypeProduct map, or we have already found
         // a representation of the same type, then we null out the entry, but keep it.
         // That prevents future attempts to register the representation map.
         if (Importer.TheCache.RepMapToTypeProduct.ContainsKey(representationMap.Id))
         {
            Importer.TheCache.RepMapToTypeProduct[representationMap.Id] = null;
            return;
         }

         string repType = representationMap.MappedRepresentation.Type;
         if (repType == null)
            repType = string.Empty;

         ISet<string> typeProductLabels = null;
         if (Importer.TheCache.TypeProductToRepLabel.TryGetValue(typeProduct.Id, out typeProductLabels) &&
            typeProductLabels != null && typeProductLabels.Contains(repType))
         {
            // We expect a TypeProduct to only have one Representation of each type.  In the case
            // that we find a 2nd representation of the same type, we will refuse to add it.
            Importer.TheCache.RepMapToTypeProduct[representationMap.Id] = null;
            return;
         }

         if (typeProductLabels == null)
            Importer.TheCache.TypeProductToRepLabel[typeProduct.Id] = new HashSet<string>();

         Importer.TheCache.RepMapToTypeProduct[representationMap.Id] = typeProduct;
         Importer.TheCache.TypeProductToRepLabel[typeProduct.Id].Add(repType);
      }

      /// <summary>
      /// Processes IfcTypeObject attributes.
      /// </summary>
      /// <param name="ifcTypeProduct">The IfcTypeProduct handle.</param>
      protected override void Process(IFCAnyHandle ifcTypeProduct)
      {
         base.Process(ifcTypeProduct);

         Tag = IFCAnyHandleUtil.GetStringAttribute(ifcTypeProduct, "Tag");

         IList<IFCAnyHandle> representationMapsHandle = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcTypeProduct, "RepresentationMaps");
         if (representationMapsHandle != null && representationMapsHandle.Count > 0)
         {
            foreach (IFCAnyHandle representationMapHandle in representationMapsHandle)
            {
               IFCRepresentationMap representationMap = IFCRepresentationMap.ProcessIFCRepresentationMap(representationMapHandle);
               if (representationMap != null)
               {
                  RepresentationMaps.Add(representationMap);

                  // Traditionally we would create a "dummy" DirectShapeType for each IfcRepresentationMap.  In the case where the IfcRepresentationMap is not used by another other IfcTypeProduct, 
                  // we would like to stop creating the "dummy" DirectShapeType and store the geometry in the DirectShapeType associated with the IfcTypeProduct.  However, IfcRepresentationMap 
                  // does not have an INVERSE relationship to its IfcTypeProduct(s), at least in IFC2x3.
                  // As such, we keep track of the IfcRepresentationMaps that have the relationship described above for future correspondence.
                  RegisterRepresentationMapWithTypeProject(representationMap, this);
               }
            }
         }
      }

      /// <summary>
      /// Processes an IfcTypeProduct.
      /// </summary>
      /// <param name="ifcTypeProduct">The IfcTypeProduct handle.</param>
      /// <returns>The IFCTypeProduct object.</returns>
      public static IFCTypeProduct ProcessIFCTypeProduct(IFCAnyHandle ifcTypeProduct)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcTypeProduct))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcTypeProduct);
            return null;
         }

         IFCEntity typeProduct;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcTypeProduct.StepId, out typeProduct))
            return (typeProduct as IFCTypeProduct);

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcTypeProduct, IFCEntityType.IfcDoorStyle))
            return IFCDoorStyle.ProcessIFCDoorStyle(ifcTypeProduct);

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcTypeProduct, IFCEntityType.IfcElementType))
            return IFCElementType.ProcessIFCElementType(ifcTypeProduct);

         return new IFCTypeProduct(ifcTypeProduct);
      }
   }
}