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
using System.Windows.Annotations;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Microsoft.VisualBasic.Logging;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcRepresentation.
   /// </summary>
   public class IFCRepresentation : IFCEntity
   {
      /// <summary>
      /// The related IfcRepresentationContext.
      /// </summary>
      public IFCRepresentationContext Context { get; protected set; } = null;

      /// <summary>
      /// The optional representation identifier.
      /// </summary>
      public IFCRepresentationIdentifier Identifier { get; protected set; } = IFCRepresentationIdentifier.Other;

      /// <summary>
      /// The optional representation type.
      /// </summary>
      public string RepresentationType { get; protected set; } = null;

      /// <summary>
      /// The representations of the product.
      /// </summary>
      public IList<IFCRepresentationItem> RepresentationItems { get; protected set; } = new List<IFCRepresentationItem>();

      /// <summary>
      /// The bounding box, only valid for "Box" representation type.
      /// </summary>
      public BoundingBoxXYZ BoundingBox { get; protected set; } = null;

      /// <summary>
      /// The associated layer assignment of the representation item, if any.
      /// </summary>
      public IFCPresentationLayerAssignment LayerAssignment { get; protected set; } = null;

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCRepresentation()
      {

      }

      private IFCRepresentationIdentifier GetRepresentationIdentifier(string identifier, string type, IFCAnyHandle ifcRepresentation)
      {
         if (Enum.TryParse(identifier, true, out IFCRepresentationIdentifier ifcRepresentationIdentifier))
         {
            // Special case for Annotation.
            switch (ifcRepresentationIdentifier)
            {
               case IFCRepresentationIdentifier.Annotation:
                  if (string.Compare(type, "Annotation2D", true) == 0)
                  {
                     return IFCRepresentationIdentifier.FootPrint;
                  }
                  return IFCRepresentationIdentifier.Body;
            }

            return ifcRepresentationIdentifier;
         }

         // Special cases below.
         // Sorted by order of expected occurences.
         // NOTE: This list includes invalid or obsolete identifiers found in real IFC files. 
         if ((string.Compare(identifier, "Facetation", true) == 0) ||
             string.IsNullOrWhiteSpace(identifier))
         {
            return IFCRepresentationIdentifier.Body;
         }

         if (string.Compare(identifier, "BoundingBox", true) == 0)
         {
            return IFCRepresentationIdentifier.Box;
         }

         if (string.Compare(identifier, "Plan", true) == 0)
         {
            return IFCRepresentationIdentifier.FootPrint;
         }

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcRepresentation, IFCEntityType.IfcStyledRepresentation))
         {
            return IFCRepresentationIdentifier.Style;
         }

         if (string.Compare(identifier, "Body-Fallback", true) == 0)
         {
            return IFCRepresentationIdentifier.BodyFallback;
         }

         Importer.TheLog.LogWarning(ifcRepresentation.StepId, "Found unknown representation identifier: " + identifier, false);
         return IFCRepresentationIdentifier.Other;
      }

      private bool NotAllowedInRepresentation(IFCAnyHandle item)
      {
         switch (Identifier)
         {
            case IFCRepresentationIdentifier.Axis:
               bool isNotAllowed = !(IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcCurve) ||
                   IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcMappedItem));

               if (IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcGeometricSet))
               {
                  isNotAllowed = false;
                  Importer.TheLog.LogWarning(item.StepId, "Axis representation has a disallowed IfcGeometricSet, processing.", true);
               }
               return isNotAllowed;
            case IFCRepresentationIdentifier.Body:
               return false;
            case IFCRepresentationIdentifier.Box:
               return !(IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcBoundingBox));
            case IFCRepresentationIdentifier.FootPrint:
               return !(IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcCurve) ||
                   IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcGeometricSet) ||
                   IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcMappedItem));
            case IFCRepresentationIdentifier.Style:
               return !(IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcStyledItem));
         }

         return false;
      }

      /// <summary>
      /// Processes IfcRepresentation attributes.
      /// </summary>
      /// <param name="ifcRepresentation">The IfcRepresentation handle.</param>
      override protected void Process(IFCAnyHandle ifcRepresentation)
      {
         base.Process(ifcRepresentation);

         string identifier = IFCImportHandleUtil.GetOptionalStringAttribute(ifcRepresentation, "RepresentationIdentifier", null);

         string type = IFCImportHandleUtil.GetOptionalStringAttribute(ifcRepresentation, "RepresentationType", null);

         Identifier = GetRepresentationIdentifier(identifier, type, ifcRepresentation);

         LayerAssignment = IFCPresentationLayerAssignment.GetTheLayerAssignment(ifcRepresentation);

         HashSet<IFCAnyHandle> items =
            IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcRepresentation, "Items");

         if (items == null)
         {
            Importer.TheLog.LogWarning(ifcRepresentation.StepId, "Missing non-optional representation items.", true);
         }
         else
         {
            foreach (IFCAnyHandle item in items)
            {
               IFCRepresentationItem repItem = null;
               try
               {
                  if (NotAllowedInRepresentation(item))
                  {
                     IFCEntityType entityType = IFCAnyHandleUtil.GetEntityType(item);
                     Importer.TheLog.LogWarning(item.StepId, "Ignoring unhandled representation item of type " + entityType.ToString() + " in " +
                         Identifier.ToString() + " representation.", true);
                     continue;
                  }

                  if (IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcBoundingBox))
                  {
                     // If Hybrid/Non-legacy, don't process BoundingBox.
                     if (Importer.TheOptions.IsHybridImport)
                        continue;

                     // Don't read in Box representation unless options allow it.
                     if (IFCImportFile.TheFile.Options.ProcessBoundingBoxGeometry == IFCProcessBBoxOptions.Never)
                     {
                        Importer.TheLog.LogWarning(item.StepId, "BoundingBox not imported with ProcessBoundingBoxGeometry=Never", false);
                     }
                     else
                     {
                        if (BoundingBox != null)
                        {
                           Importer.TheLog.LogWarning(item.StepId, "Found second IfcBoundingBox representation item, ignoring.", false);
                           continue;
                        }
                        BoundingBox = ProcessBoundingBox(item);
                     }
                  }
                  else
                  {
                     repItem = IFCRepresentationItem.ProcessIFCRepresentationItem(item);
                  }
               }
               catch (Exception ex)
               {
                  Importer.TheLog.LogError(item.StepId, ex.Message, false);
               }

               if (repItem != null)
                  RepresentationItems.Add(repItem);
            }
         }

         IFCAnyHandle representationContext = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcRepresentation, "ContextOfItems", false);
         if (representationContext != null)
            Context = IFCRepresentationContext.ProcessIFCRepresentationContext(representationContext);

         RepresentationType = IFCImportHandleUtil.GetOptionalStringAttribute(ifcRepresentation, "RepresentationType", null);
      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCRepresentation(IFCAnyHandle representation)
      {
         Process(representation);
      }

      private void CreateBoxShape(IFCImportShapeEditScope shapeEditScope, Transform scaledLcs)
      {
         using (IFCImportShapeEditScope.IFCContainingRepresentationSetter repSetter = new IFCImportShapeEditScope.IFCContainingRepresentationSetter(shapeEditScope, this))
         {
            // Get the material and graphics style based in the "Box" sub-category of Generic Models.  
            // We will create the sub-category if this is our first time trying to use it.
            // Note that all bounding boxes are controlled by a sub-category of Generic Models.  We may revisit that decision later.
            // Note that we hard-wire the identifier to "Box" because older files may have bounding box items in an obsolete representation.
            SolidOptions solidOptions = null;
            Category bboxCategory = IFCCategoryUtil.GetSubCategoryForRepresentation(shapeEditScope.Document, Id, IFCRepresentationIdentifier.Box);
            if (bboxCategory != null)
            {
               ElementId materialId = (bboxCategory.Material == null) ? ElementId.InvalidElementId : bboxCategory.Material.Id;
               GraphicsStyle graphicsStyle = bboxCategory.GetGraphicsStyle(GraphicsStyleType.Projection);
               ElementId gstyleId = (graphicsStyle == null) ? ElementId.InvalidElementId : graphicsStyle.Id;
               solidOptions = new SolidOptions(materialId, gstyleId);
            }

            Solid bboxSolid = IFCGeometryUtil.CreateSolidFromBoundingBox(scaledLcs, BoundingBox, solidOptions);
            if (bboxSolid != null)
            {
               IFCSolidInfo bboxSolidInfo = IFCSolidInfo.Create(Id, bboxSolid);
               shapeEditScope.AddGeometry(bboxSolidInfo);
            }
         }
         return;
      }

      /// <summary>
      /// Create geometry for a particular representation.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which representation is being created.</param>
      public void CreateShape(IFCImportShapeEditScope shapeEditScope, Transform scaledLcs, string guid)
      {
         // Special handling for Box representation.  We may decide to create an IFCBoundingBox class and stop this special treatment.
         if (BoundingBox != null)
            CreateBoxShape(shapeEditScope, scaledLcs);

         if (LayerAssignment != null)
            LayerAssignment.Create(shapeEditScope);

         // There is an assumption here that Process() weeded out any items that are invalid for this representation.
         using (IFCImportShapeEditScope.IFCMaterialStack stack = new IFCImportShapeEditScope.IFCMaterialStack(shapeEditScope, null, LayerAssignment))
         {
            using (IFCImportShapeEditScope.IFCContainingRepresentationSetter repSetter = new IFCImportShapeEditScope.IFCContainingRepresentationSetter(shapeEditScope, this))
            {
               foreach (IFCRepresentationItem representationItem in RepresentationItems)
               {
                  representationItem.CreateShape(shapeEditScope, scaledLcs, guid);
               }
            }
         }
      }

      // TODO: this function should be moved to IFCBoundingBox.cs now that they are fully supported.
      static private BoundingBoxXYZ ProcessBoundingBox(IFCAnyHandle boundingBoxHnd)
      {
         IFCAnyHandle lowerLeftHnd = IFCAnyHandleUtil.GetInstanceAttribute(boundingBoxHnd, "Corner");
         XYZ minXYZ = IFCPoint.ProcessScaledLengthIFCCartesianPoint(lowerLeftHnd);

         bool found = false;
         double xDim = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(boundingBoxHnd, "XDim", out found);
         if (!found)
            return null;

         double yDim = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(boundingBoxHnd, "YDim", out found);
         if (!found)
            return null;

         double zDim = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(boundingBoxHnd, "ZDim", out found);
         if (!found)
            return null;

         XYZ maxXYZ = new XYZ(minXYZ.X + xDim, minXYZ.Y + yDim, minXYZ.Z + zDim);
         BoundingBoxXYZ boundingBox = new BoundingBoxXYZ();
         boundingBox.set_Bounds(0, minXYZ);
         boundingBox.set_Bounds(1, maxXYZ);
         return boundingBox;
      }

      /// <summary>
      /// Processes an IfcRepresentation object.
      /// </summary>
      /// <param name="ifcRepresentation">The IfcRepresentation handle.</param>
      /// <returns>The IFCRepresentation object.</returns>
      public static IFCRepresentation ProcessIFCRepresentation(IFCAnyHandle ifcRepresentation)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcRepresentation))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcRepresentation);
            return null;
         }

         IFCEntity representation;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcRepresentation.StepId, out representation))
            return (representation as IFCRepresentation);

         return new IFCRepresentation(ifcRepresentation);
      }
   }
}