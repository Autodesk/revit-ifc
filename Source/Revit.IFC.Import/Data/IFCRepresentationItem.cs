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
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public abstract class IFCRepresentationItem : IFCEntity
   {
      private IFCStyledItem m_StyledByItem = null;

      private IFCPresentationLayerAssignment m_LayerAssignment = null;

      /// <summary>
      /// The associated style of the representation item, if any.
      /// </summary>
      public IFCStyledItem StyledByItem
      {
         get { return m_StyledByItem; }
         protected set { m_StyledByItem = value; }
      }

      /// <summary>
      /// The associated layer assignment of the representation item, if any.
      /// </summary>
      public IFCPresentationLayerAssignment LayerAssignment
      {
         get { return m_LayerAssignment; }
         protected set { m_LayerAssignment = value; }
      }

      /// <summary>
      /// Returns the associated material id, if any.
      /// </summary>
      /// <param name="scope">The containing creation scope.</param>
      /// <returns>The element id of the material, if any.</returns>
      public virtual ElementId GetMaterialElementId(IFCImportShapeEditScope scope)
      {
         ElementId materialId = scope.GetCurrentMaterialId();
         if (materialId != ElementId.InvalidElementId)
            return materialId;

         if (scope.Creator != null)
         {
            IFCMaterial creatorMaterial = scope.Creator.GetTheMaterial();
            if (creatorMaterial != null)
               return creatorMaterial.GetMaterialElementId();
         }

         return ElementId.InvalidElementId;
      }

      protected IFCRepresentationItem()
      {
      }

      override protected void Process(IFCAnyHandle item)
      {
         base.Process(item);

         LayerAssignment = IFCPresentationLayerAssignment.GetTheLayerAssignment(item, false);

         // IFC2x has a different representation for styled items which we don't support.
         if (IFCImportFile.TheFile.SchemaVersion >= IFCSchemaVersion.IFC2x2)
         {
            List<IFCAnyHandle> styledByItems = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(item, "StyledByItem");
            if (styledByItems != null && styledByItems.Count > 0)
            {
               // We can only handle one styled item, but we allow the possiblity that there are duplicates.  Do a top-level check.
               foreach (IFCAnyHandle styledByItem in styledByItems)
               {
                  if (!IFCAnyHandleUtil.IsSubTypeOf(styledByItem, IFCEntityType.IfcStyledItem))
                  {
                     Importer.TheLog.LogUnexpectedTypeError(styledByItem, IFCEntityType.IfcStyledItem, false);
                     StyledByItem = null;
                     break;
                  }
                  else
                  {
                     if (StyledByItem == null)
                        StyledByItem = IFCStyledItem.ProcessIFCStyledItem(styledByItem);
                     else
                     {
                        IFCStyledItem compStyledByItem = IFCStyledItem.ProcessIFCStyledItem(styledByItem);
                        if (!StyledByItem.IsEquivalentTo(compStyledByItem))
                        {
                           Importer.TheLog.LogWarning(Id, "Multiple inconsistent styled items found for this item; using first one.", false);
                           break;
                        }
                     }
                  }
               }
            }
         }
      }

      /// <summary>
      /// Deal with missing "LayerAssignments" in IFC2x3 EXP file.
      /// </summary>
      /// <param name="layerAssignment">The layer assignment to add to this representation.</param>
      public void PostProcessLayerAssignment(IFCPresentationLayerAssignment layerAssignment)
      {
         if (LayerAssignment == null)
            LayerAssignment = layerAssignment;
         else
            IFCImportDataUtil.CheckLayerAssignmentConsistency(LayerAssignment, layerAssignment, Id);
      }

      /// <summary>
      /// Create geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      public void CreateShape(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
      {
         if (StyledByItem != null)
            StyledByItem.Create(shapeEditScope);

         if (LayerAssignment != null)
            LayerAssignment.Create(shapeEditScope);

         using (IFCImportShapeEditScope.IFCMaterialStack stack = new IFCImportShapeEditScope.IFCMaterialStack(shapeEditScope, StyledByItem, LayerAssignment))
         {
            CreateShapeInternal(shapeEditScope, lcs, scaledLcs, guid);
         }
      }

      /// <summary>
      /// Create geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="lcs">Local coordinate system for the geometry.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      virtual protected void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
      {
      }

      protected IFCRepresentationItem(IFCAnyHandle item)
      {
      }

      /// <summary>
      /// Processes an IfcRepresentationItem entity handle.
      /// </summary>
      /// <param name="ifcRepresentationItem">The IfcRepresentationItem handle.</param>
      /// <returns>The IFCRepresentationItem object.</returns>
      public static IFCRepresentationItem ProcessIFCRepresentationItem(IFCAnyHandle ifcRepresentationItem)
      {
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcBooleanResult))
            return IFCBooleanResult.ProcessIFCBooleanResult(ifcRepresentationItem);
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcCurve))
            return IFCCurve.ProcessIFCCurve(ifcRepresentationItem);
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcFaceBasedSurfaceModel))
            return IFCFaceBasedSurfaceModel.ProcessIFCFaceBasedSurfaceModel(ifcRepresentationItem);
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcGeometricSet))
            return IFCGeometricSet.ProcessIFCGeometricSet(ifcRepresentationItem);
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcMappedItem))
            return IFCMappedItem.ProcessIFCMappedItem(ifcRepresentationItem);
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcShellBasedSurfaceModel))
            return IFCShellBasedSurfaceModel.ProcessIFCShellBasedSurfaceModel(ifcRepresentationItem);
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcSolidModel))
            return IFCSolidModel.ProcessIFCSolidModel(ifcRepresentationItem);

         if (IFCImportFile.TheFile.SchemaVersion >= IFCSchemaVersion.IFC2x2 && IFCAnyHandleUtil.IsSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcStyledItem))
            return IFCStyledItem.ProcessIFCStyledItem(ifcRepresentationItem);

         if (IFCImportFile.TheFile.SchemaVersion >= IFCSchemaVersion.IFC4 && IFCAnyHandleUtil.IsSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcTriangulatedFaceSet))
            return IFCTriangulatedFaceSet.ProcessIFCTriangulatedFaceSet(ifcRepresentationItem);

         if (IFCImportFile.TheFile.SchemaVersion >= IFCSchemaVersion.IFC4Add2 && IFCAnyHandleUtil.IsSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcPolygonalFaceSet))
            return IFCPolygonalFaceSet.ProcessIFCPolygonalFaceSet(ifcRepresentationItem);

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcTopologicalRepresentationItem))
            return IFCTopologicalRepresentationItem.ProcessIFCTopologicalRepresentationItem(ifcRepresentationItem);

         Importer.TheLog.LogUnhandledSubTypeError(ifcRepresentationItem, IFCEntityType.IfcRepresentationItem, true);
         return null;
      }
   }
}