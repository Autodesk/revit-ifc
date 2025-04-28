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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public class IFCMappedItem : IFCRepresentationItem
   {
      IFCCartesianTransformOperator m_MappingTarget = null;

      IFCRepresentationMap m_MappingSource = null;

      /// <summary>
      /// The transform, potentially including mirroring and non-uniform scaling.
      /// </summary>
      public IFCCartesianTransformOperator MappingTarget
      {
         get { return m_MappingTarget; }
         protected set { m_MappingTarget = value; }
      }

      /// <summary>
      /// The representation map containing the shared geometry.
      /// </summary>
      public IFCRepresentationMap MappingSource
      {
         get { return m_MappingSource; }
         protected set { m_MappingSource = value; }
      }

      protected IFCMappedItem()
      {
      }

      override protected void Process(IFCAnyHandle item)
      {
         base.Process(item);

         IFCAnyHandle mappingSource = IFCImportHandleUtil.GetRequiredInstanceAttribute(item, "MappingSource", false);
         if (mappingSource == null)
            return;

         MappingSource = IFCRepresentationMap.ProcessIFCRepresentationMap(mappingSource);

         // We will not fail if the transform is not given, but instead assume it to be the identity.
         IFCAnyHandle mappingTarget = IFCImportHandleUtil.GetRequiredInstanceAttribute(item, "MappingTarget", false);
         if (mappingTarget != null)
            MappingTarget = IFCCartesianTransformOperator.ProcessIFCCartesianTransformOperator(mappingTarget);
         else
            MappingTarget = IFCCartesianTransformOperator.ProcessIFCCartesianTransformOperator();
      }

      private int FindAlternateGeometrySource(int originalId)
      {
         // NAVIS_TODO: Explain this.
         if (!Importer.TheProcessor.FindAlternateGeometrySource)
            return originalId;

         // Copy some code from elsewhere to work out what the object was that got the
         // geometry.
         IFCTypeProduct typeProduct;
         Importer.TheCache.RepMapToTypeProduct.TryGetValue(MappingSource.Id, out typeProduct);
         return typeProduct?.Id ?? originalId;
      }

      /// <summary>
      /// Create geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, 
         Transform scaledLcs, string guid)
      {
         base.CreateShapeInternal(shapeEditScope, scaledLcs, guid);

         // Optimization for hybrid import.  MappingTarget will be null in this case.
         if (MappingTarget == null)
         {
            return;
         }

         // Check scale; if it is uniform, create an instance.  If not, create a shape directly.
         // TODO: Instead allow creation of instances based on similar scaling.
         double scaleX = MappingTarget.Scale;
         double scaleY = MappingTarget.ScaleY.HasValue ? MappingTarget.ScaleY.Value : scaleX;
         double scaleZ = MappingTarget.ScaleZ.HasValue ? MappingTarget.ScaleZ.Value : scaleX;

         bool currentIsUnitScale = MathUtil.IsAlmostEqual(scaleX, 1.0) &&
            MathUtil.IsAlmostEqual(scaleY, 1.0) &&
            MathUtil.IsAlmostEqual(scaleZ, 1.0);

         Transform mappingTransform = MappingTarget.Transform;

         Transform newScaledLcs =
            (mappingTransform == null) ? scaledLcs : (scaledLcs?.Multiply(mappingTransform) ?? mappingTransform);

         bool isFootPrint =
            (shapeEditScope.ContainingRepresentation.Identifier == IFCRepresentationIdentifier.FootPrint);
         
         bool isUnitScale = currentIsUnitScale &&
            ((newScaledLcs?.IsConformal ?? true) ?
               MathUtil.IsAlmostEqual(newScaledLcs?.Scale ?? 1.0, 1.0) : false);

         bool canCreateType = !shapeEditScope.PreventInstances && !isFootPrint && isUnitScale &&
            (shapeEditScope.ContainingRepresentation != null);

         if (canCreateType)
         {
            int mappingSourceId = MappingSource.Id;

            int geometrySourceId = FindAlternateGeometrySource(mappingSourceId);
            MappingSource.CreateShape(shapeEditScope, null, guid);

            // IFCDefaultProcessor doesn't implement PostProcessMappedItem.  Skip this call to avoid
            // GetCategoryId(), which can create an unnecessary subcategory (which also forces an
            // unnecessary regeneration).
            if ((shapeEditScope.Creator != null) && !(Importer.TheProcessor is IFCDefaultProcessor))
            {
               Importer.TheProcessor.PostProcessMappedItem(shapeEditScope.Creator.Id,
                  shapeEditScope.Creator.GlobalId,
                  shapeEditScope.Creator.EntityType.ToString(),
                  shapeEditScope.Creator.GetCategoryId(shapeEditScope.Document),
                  geometrySourceId,
                  newScaledLcs);
            }

            // NAVIS_TODO: Figure out how not to do this.
            IList<GeometryObject> instances = DirectShape.CreateGeometryInstance(
               shapeEditScope.Document, mappingSourceId.ToString(), newScaledLcs);
            foreach (GeometryObject instance in instances)
               shapeEditScope.AddGeometry(IFCSolidInfo.Create(Id, instance));
         }
         else
         {
            if (!isUnitScale)
            {
               XYZ xScale = new XYZ(scaleX, 0.0, 0.0);
               XYZ yScale = new XYZ(0.0, scaleY, 0.0);
               XYZ zScale = new XYZ(0.0, 0.0, scaleZ);
               Transform scaleTransform = Transform.Identity;
               scaleTransform.set_Basis(0, xScale);
               scaleTransform.set_Basis(1, yScale);
               scaleTransform.set_Basis(2, zScale);
               newScaledLcs = newScaledLcs.Multiply(scaleTransform);
            }

            MappingSource.CreateShape(shapeEditScope, newScaledLcs, guid);
         }
      }

      protected IFCMappedItem(IFCAnyHandle item)
      {
         Process(item);
      }

      /// <summary>
      /// Create an IFCMappedItem object from a handle of type IfcMappedItem.
      /// </summary>
      /// <param name="ifcMappedItem">The IFC handle.</param>
      /// <returns>The IFCMappedItem object.</returns>
      public static IFCMappedItem ProcessIFCMappedItem(IFCAnyHandle ifcMappedItem)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMappedItem))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcMappedItem);
            return null;
         }

         IFCEntity mappedItem;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcMappedItem.StepId, out mappedItem))
            mappedItem = new IFCMappedItem(ifcMappedItem);
         return (mappedItem as IFCMappedItem);
      }
   }
}