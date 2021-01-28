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
   public class IFCPresentationLayerWithStyle : IFCPresentationLayerAssignment
   {
      /// <summary>
      /// Get the presentation styles for this IFCPresentationLayerWithStyle.
      /// </summary>
      public IList<IFCPresentationStyle> LayerStyles { get; protected set; } = new List<IFCPresentationStyle>();

      protected ElementId CreatedMaterialElementId { get; set; } = ElementId.InvalidElementId;

      protected IFCPresentationLayerWithStyle()
      {
      }

      protected IFCPresentationLayerWithStyle(IFCAnyHandle item)
      {
         Process(item);
      }

      /// <summary>
      /// Get the IFCSurfaceStyle associated with this IFCPresentationLayerWithStyle.
      /// </summary>
      /// <returns>The IFCSurfaceStyle, if any.</returns>
      public IFCSurfaceStyle GetSurfaceStyle()
      {
         IList<IFCPresentationStyle> presentationStyles = LayerStyles;
         foreach (IFCPresentationStyle presentationStyle in presentationStyles)
         {
            if (presentationStyle is IFCSurfaceStyle)
               return (presentationStyle as IFCSurfaceStyle);
         }

         return null;
      }

      /// <summary>
      /// Returns the main element id of the material associated with this presentation layer assignment.
      /// </summary>
      /// <param name="shapeEditScope">The shape edit scope.</param>
      /// <returns>The element id, or ElementId.invalidElementId if not set.</returns>
      public override ElementId GetMaterialElementId(IFCImportShapeEditScope shapeEditScope)
      {
         return CreatedMaterialElementId;
      }

      override protected void Process(IFCAnyHandle item)
      {
         base.Process(item);

         IList<IFCAnyHandle> layerStyles = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(item, "LayerStyles");
         foreach (IFCAnyHandle layerStyle in layerStyles)
         {
            if (layerStyle == null)
            {
               Importer.TheLog.LogNullError(IFCEntityType.IfcPresentationStyle);
               continue;
            }

            IFCPresentationStyle presentationStyle = IFCPresentationStyle.ProcessIFCPresentationStyle(layerStyle);
            if (presentationStyle != null)
               LayerStyles.Add(presentationStyle);
            else
               Importer.TheLog.LogUnhandledSubTypeError(layerStyle, "IfcPresentationStyle", false);
         }
      }

      /// <summary>
      /// Does a top-level check to see if this entity is equivalent to otherEntity.
      /// </summary>
      /// <param name="otherEntity">The other IFCEntity.</param>
      /// <returns>True if they are equivalent, false if they aren't, null if not enough information.</returns>
      /// <remarks>This isn't intended to be an exhaustive check, and isn't implemented for all types.  This is intended
      /// to be used by derived classes.</remarks>
      public override bool? MaybeEquivalentTo(IFCEntity otherEntity)
      {
         bool? maybeEquivalentTo = base.MaybeEquivalentTo(otherEntity);
         if (maybeEquivalentTo.HasValue)
            return maybeEquivalentTo.Value;

         if (!(otherEntity is IFCPresentationLayerWithStyle))
            return false;

         IFCPresentationLayerWithStyle other = otherEntity as IFCPresentationLayerWithStyle;

         if (!IFCEntity.AreIFCEntityListsEquivalent(LayerStyles, other.LayerStyles))
            return false;

         return null;
      }

      /// <summary>
      /// Processes an IfcPresentationLayerWithStyle entity handle.
      /// </summary>
      /// <param name="ifcPresentationLayerWithStyle">The IfcPresentationLayerWithStyle handle.</param>
      /// <returns>The IFCPresentationLayerWithStyle object.</returns>
      public static IFCPresentationLayerWithStyle ProcessIFCPresentationLayerWithStyle(IFCAnyHandle ifcPresentationLayerWithStyle)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcPresentationLayerWithStyle))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcPresentationLayerWithStyle);
            return null;
         }

         IFCEntity presentationLayerWithStyle;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcPresentationLayerWithStyle.StepId, out presentationLayerWithStyle))
            return (presentationLayerWithStyle as IFCPresentationLayerWithStyle);

         return new IFCPresentationLayerWithStyle(ifcPresentationLayerWithStyle);
      }

      /// <summary>
      /// Create the Revit elements associated with this IfcPresentationLayerWithStyle.
      /// </summary>
      /// <param name="shapeEditScope">The shape edit scope.</param>
      override public void Create(IFCImportShapeEditScope shapeEditScope)
      {
         // TODO: support cut pattern id and cut pattern color.
         if (CreatedMaterialElementId != ElementId.InvalidElementId || !IsValidForCreation)
            return;

         base.Create(shapeEditScope);

         try
         {
            // If the styled item or the surface style has a name, use it.
            IFCSurfaceStyle surfaceStyle = GetSurfaceStyle();
            if (surfaceStyle == null)
            {
               // We only handle surface styles at the moment; log file should already reflect any other unhandled styles.
               IsValidForCreation = true;
               return;
            }

            string forcedName = surfaceStyle.Name;
            if (string.IsNullOrWhiteSpace(forcedName))
               forcedName = Name;

            CreatedMaterialElementId = surfaceStyle.Create(shapeEditScope.Document, forcedName, null, Id);
         }
         catch (Exception ex)
         {
            IsValidForCreation = false;
            Importer.TheLog.LogCreationError(this, ex.Message, false);
         }
      }
   }
}