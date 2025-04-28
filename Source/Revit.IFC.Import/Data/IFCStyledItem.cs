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
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public class IFCStyledItem : IFCRepresentationItem
   {
      // Currently the only created element would be a material.  May expand to a list of elements.
      private ElementId m_CreatedElementId = ElementId.InvalidElementId;

      /// <summary>
      /// The optional associated representation item.
      /// </summary>
      public IFCRepresentationItem Item { get; protected set; } = null;

      /// <summary>
      /// Get the styles associated with the IfcStyledItem.
      /// Note that the IFC specification allows a set of these, but usage restricts 
      /// this to one item.
      /// </summary>
      public List<IFCPresentationStyle> Styles { get; } = new List<IFCPresentationStyle>();

      /// <summary>
      /// The optional name of the styled item.
      /// </summary>
      public string Name { get; protected set; } = null;

      /// <summary>
      /// Returns the main element id associated with this material.
      /// </summary>
      /// <param name="scope">The containing import scope.</param>
      /// <remarks>The creator argument is ignored, as it is taken into account when creating the material.</remarks>
      public override ElementId GetMaterialElementId(IFCImportShapeEditScope scope)
      {
         return m_CreatedElementId;
      }

      /// <summary>
      /// Does a top-level check to see if this styled item may be equivalent to another styled item.
      /// </summary>
      /// <param name="otherEntity">The other styled item.</param>
      /// <returns>False if they don't have the same handles, null otherwise.</returns>
      public override bool? MaybeEquivalentTo(IFCEntity otherEntity)
      {
         bool? maybeEquivalentTo = base.MaybeEquivalentTo(otherEntity);
         if (maybeEquivalentTo.HasValue)
            return maybeEquivalentTo.Value;

         if (!(otherEntity is IFCStyledItem))
            return false;

         IFCStyledItem other = otherEntity as IFCStyledItem;

         if (!Equals(Item, other.Item))
            return false;

         IList<IFCPresentationStyle> otherStyles = other.Styles;
         if (Styles == null && otherStyles == null)
            return null;

         // If only one is null, they are not equal.
         if (Styles == null || otherStyles == null)
            return false;

         int thisCount = Styles.Count;
         int otherCount = otherStyles.Count;
         if (thisCount != otherCount)
            return false;

         // This could miss the case where the styles are the same but in different
         // orders.  At the moment, that would just lead to a spurious warning.
         for (int ii = 0; ii < thisCount; ii++)
         {
            if (!Equals(Styles[ii], otherStyles[ii]))
               return false;
         }

         return null;
      }

      /// <summary>
      /// Does a top-level check to see if this entity is equivalent to otherEntity.
      /// </summary>
      /// <param name="otherEntity">The other IFCEntity.</param>
      /// <returns>True if they are equivalent, false if they aren't.</returns>
      /// <remarks>This isn't intended to be an exhaustive check, and isn't implemented for all types.  This is intended
      /// to make a final decision, and will err on the side of deciding that entities aren't equivalent.</remarks>
      public override bool IsEquivalentTo(IFCEntity otherEntity)
      {
         bool? maybeEquivalentTo = MaybeEquivalentTo(otherEntity);
         if (maybeEquivalentTo.HasValue)
            return maybeEquivalentTo.Value;

         // If it passes all of the Maybe tests and doesn't come back false, good enough.
         return true;
      }

      protected IFCStyledItem()
      {
      }

      override protected void Process(IFCAnyHandle styledItem)
      {
         base.Process(styledItem);

         IFCAnyHandle item = IFCImportHandleUtil.GetOptionalInstanceAttribute(styledItem, "Item");
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(item))
            Item = IFCRepresentationItem.ProcessIFCRepresentationItem(item);

         Name = IFCImportHandleUtil.GetOptionalStringAttribute(styledItem, "Name", null);

         List<IFCAnyHandle> styles = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(styledItem, "Styles");
         if (styles == null || styles.Count == 0)
            Importer.TheLog.LogMissingRequiredAttributeError(styledItem, "Styles", true);
         
         foreach (IFCAnyHandle style in styles)
         {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(style))
               continue;

            if (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC4Obsolete) && IFCAnyHandleUtil.IsValidSubTypeOf(style, IFCEntityType.IfcPresentationStyle))
            {
               Styles.Add(IFCPresentationStyle.ProcessIFCPresentationStyle(style));
            }
            else
            {
               if (Styles.Count != 0)
               {
                  Importer.TheLog.LogWarning(styledItem.StepId, "Multiple presentation styles found for IfcStyledItem - using first.", false);
                  continue;
               }

               IFCPresentationStyleAssignment presentationStyleAssignment = 
                  IFCPresentationStyleAssignment.ProcessIFCPresentationStyleAssignment(style);
               if (presentationStyleAssignment != null && presentationStyleAssignment.Styles != null)
                  Styles.AddRange(presentationStyleAssignment.Styles);
            }
         }
      }

      protected IFCStyledItem(IFCAnyHandle item)
      {
         Process(item);
      }

      /// <summary>
      /// Get the IFCSurfaceStyle associated with this IFCStyledItem.
      /// </summary>
      /// <returns>The IFCSurfaceStyle, if any.</returns>
      public IFCSurfaceStyle GetSurfaceStyle()
      {
         IList<IFCPresentationStyle> presentationStyles = Styles;
         foreach (IFCPresentationStyle presentationStyle in presentationStyles)
         {
            if (presentationStyle is IFCSurfaceStyle)
               return (presentationStyle as IFCSurfaceStyle);
         }

         return null;
      }

      /// <summary>
      /// Creates a Revit material based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      public void Create(IFCImportShapeEditScope shapeEditScope)
      {
         // TODO: support cut pattern id and cut pattern color.
         if (m_CreatedElementId != ElementId.InvalidElementId || !IsValidForCreation)
            return;

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

            string suggestedName = null;
            if (Item != null)
            {
               IFCProduct creator = shapeEditScope.Creator;
               suggestedName = creator.GetTheMaterialName();
            }

            m_CreatedElementId = surfaceStyle.Create(shapeEditScope.Document, forcedName, suggestedName, Id);
         }
         catch (Exception ex)
         {
            IsValidForCreation = false;
            Importer.TheLog.LogCreationError(this, ex.Message, false);
         }
      }

      /// <summary>
      /// Processes an IfcStyledItem entity handle.
      /// </summary>
      /// <param name="ifcStyledItem">The IfcStyledItem handle.</param>
      /// <returns>The IFCStyledItem object.</returns>
      public static IFCStyledItem ProcessIFCStyledItem(IFCAnyHandle ifcStyledItem)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcStyledItem))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcStyledItem);
            return null;
         }

         IFCEntity styledItem;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcStyledItem.StepId, out styledItem))
            styledItem = new IFCStyledItem(ifcStyledItem);
         return (styledItem as IFCStyledItem);
      }
   }
}