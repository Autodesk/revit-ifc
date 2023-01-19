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
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public abstract class IFCRepresentationItem : IFCEntity
   {
      /// <summary>
      /// The associated style of the representation item, if any.
      /// </summary>
      public IFCStyledItem StyledByItem { get; protected set; } = null;

      /// <summary>
      /// The associated layer assignment of the representation item, if any.
      /// </summary>
      public IFCPresentationLayerAssignment LayerAssignment { get; protected set; } = null;

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

         LayerAssignment = IFCPresentationLayerAssignment.GetTheLayerAssignment(item);

         // IFC2x has a different representation for styled items which we don't support.
         ICollection<IFCAnyHandle> styledByItems = null;
         if (Importer.TheCache.StyledByItems.TryGetValue(item, out styledByItems))
         {
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
      /// Create geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      public void CreateShape(IFCImportShapeEditScope shapeEditScope, 
         Transform scaledLcs, string guid)
      {
         if (StyledByItem != null)
            StyledByItem.Create(shapeEditScope);

         if (LayerAssignment != null)
            LayerAssignment.Create(shapeEditScope);

         using (IFCImportShapeEditScope.IFCMaterialStack stack = new IFCImportShapeEditScope.IFCMaterialStack(shapeEditScope, StyledByItem, LayerAssignment))
         {
            CreateShapeInternal(shapeEditScope, scaledLcs, guid);
         }
      }

      /// <summary>
      /// Create geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="scaledLcs">The scaled local coordinate system for the geometry.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      virtual protected void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, 
         Transform scaledLcs, string guid)
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
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcRepresentationItem))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcRepresentationItem);
            return null;
         }

         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcMappedItem))
            return IFCMappedItem.ProcessIFCMappedItem(ifcRepresentationItem);
         if (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC2x2) && IFCAnyHandleUtil.IsSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcStyledItem))
            return IFCStyledItem.ProcessIFCStyledItem(ifcRepresentationItem);
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcTopologicalRepresentationItem))
            return IFCTopologicalRepresentationItem.ProcessIFCTopologicalRepresentationItem(ifcRepresentationItem);

         // TODO: Move everything below to IFCGeometricRepresentationItem, once it is created.
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcBooleanResult))
            return IFCBooleanResult.ProcessIFCBooleanResult(ifcRepresentationItem);
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcCurve))
            return IFCCurve.ProcessIFCCurve(ifcRepresentationItem);
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcFaceBasedSurfaceModel))
            return IFCFaceBasedSurfaceModel.ProcessIFCFaceBasedSurfaceModel(ifcRepresentationItem);
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcGeometricSet))
            return IFCGeometricSet.ProcessIFCGeometricSet(ifcRepresentationItem);
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcPoint))
            return IFCPoint.ProcessIFCPoint(ifcRepresentationItem);
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcShellBasedSurfaceModel))
            return IFCShellBasedSurfaceModel.ProcessIFCShellBasedSurfaceModel(ifcRepresentationItem);
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcSolidModel))
            return IFCSolidModel.ProcessIFCSolidModel(ifcRepresentationItem);
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcCsgPrimitive3D))
            return IFCCsgPrimitive3D.ProcessIFCCsgPrimitive3D(ifcRepresentationItem);

         // TODO: Move the items below to IFCGeometricRepresentationItem->IFCTessellatedItem->IfcTessellatedFaceSet.
         if (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC4Obsolete) && IFCAnyHandleUtil.IsSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcTriangulatedFaceSet))
            return IFCTriangulatedFaceSet.ProcessIFCTriangulatedFaceSet(ifcRepresentationItem);
         // There is no way to actually determine an IFC4Add2 file vs. a "vanilla" IFC4 file, which is
         // obsolete.  The try/catch here allows us to read these obsolete files without crashing.
         try
         {
            if (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC4) && IFCAnyHandleUtil.IsSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcPolygonalFaceSet))
               return IFCPolygonalFaceSet.ProcessIFCPolygonalFaceSet(ifcRepresentationItem);
         }
         catch (Exception ex)
         {
            // Once we fail once, downgrade the schema so we don't try again.
            if (IFCImportFile.HasUndefinedAttribute(ex))
               IFCImportFile.TheFile.DowngradeIFC4SchemaTo(IFCSchemaVersion.IFC4Add1Obsolete);
            else
               throw ex;
         }

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcRepresentationItem, IFCEntityType.IfcSurface))
            return IFCSurface.ProcessIFCSurface(ifcRepresentationItem);

         Importer.TheLog.LogUnhandledSubTypeError(ifcRepresentationItem, IFCEntityType.IfcRepresentationItem, false);
         return null;
      }
   }
}