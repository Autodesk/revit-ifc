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
   public class IFCPresentationLayerAssignment : IFCEntity
   {
      /// <summary>
      /// Get the name of the IFCPresentationLayerAssignment.
      /// </summary>
      public string Name { get; protected set; } = null;
      /// <summary>
      /// Get the optional description of the IFCPresentationLayerAssignment.
      /// </summary>

      public string Description { get; protected set; } = null;

      /// <summary>
      /// Get the optional identifier of the IFCPresentationLayerAssignment.
      /// </summary>
      public string Identifier { get; protected set; } = null;

      protected IFCPresentationLayerAssignment()
      {
      }

      protected IFCPresentationLayerAssignment(IFCAnyHandle item)
      {
         Process(item);
      }

      /// <summary>
      /// Returns the main element id of the material associated with this presentation layer assignment.
      /// </summary>
      /// <param name="shapeEditScope">The shape edit scope.</param>
      /// <returns>The element id, or ElementId.invalidElementId if not set.</returns>
      public virtual ElementId GetMaterialElementId(IFCImportShapeEditScope shapeEditScope)
      {
         return ElementId.InvalidElementId;
      }

      override protected void Process(IFCAnyHandle item)
      {
         base.Process(item);

         Name = IFCImportHandleUtil.GetOptionalStringAttribute(item, "Name", null);

         Description = IFCImportHandleUtil.GetOptionalStringAttribute(item, "Description", null);

         Identifier = IFCImportHandleUtil.GetOptionalStringAttribute(item, "Identifier", null);

         // We do NOT process AssignedItems here.  That is pre-processed to avoid INVERSE attribute calls.
      }

      /// <summary>
      /// Processes an IfcPresentationLayerAssignment entity handle.
      /// </summary>
      /// <param name="ifcPresentationLayerAssignment">The IfcPresentationLayerAssignment handle.</param>
      /// <returns>The IFCPresentationLayerAssignment object.</returns>
      public static IFCPresentationLayerAssignment ProcessIFCPresentationLayerAssignment(IFCAnyHandle ifcPresentationLayerAssignment)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcPresentationLayerAssignment))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcPresentationLayerAssignment);
            return null;
         }

         IFCEntity presentationLayerAssignment;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcPresentationLayerAssignment.StepId, out presentationLayerAssignment))
            return (presentationLayerAssignment as IFCPresentationLayerAssignment);

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcPresentationLayerAssignment, IFCEntityType.IfcPresentationLayerWithStyle))
            return IFCPresentationLayerWithStyle.ProcessIFCPresentationLayerWithStyle(ifcPresentationLayerAssignment);

         return new IFCPresentationLayerAssignment(ifcPresentationLayerAssignment);
      }

      /// <summary>
      /// Does a top-level check to see if this entity may be equivalent to otherEntity.
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

         if (!(otherEntity is IFCPresentationLayerAssignment))
            return false;

         IFCPresentationLayerAssignment other = otherEntity as IFCPresentationLayerAssignment;

         if (!IFCNamingUtil.SafeStringsAreEqual(Name, other.Name))
            return false;

         if (!IFCNamingUtil.SafeStringsAreEqual(Description, other.Description))
            return false;

         if (!IFCNamingUtil.SafeStringsAreEqual(Identifier, other.Identifier))
            return false;

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

      /// <summary>
      /// Create the Revit elements associated with this IfcPresentationLayerAssignment.
      /// </summary>
      /// <param name="shapeEditScope">The shape edit scope.</param>
      virtual public void Create(IFCImportShapeEditScope shapeEditScope)
      {
         if (!string.IsNullOrWhiteSpace(Name))
            shapeEditScope.PresentationLayerNames.Add(Name);
      }

      /// <summary>
      /// Get the one layer assignment associated to this handle, if it is defined.
      /// </summary>
      /// <param name="ifcLayeredItem">The handle assumed to be an IfcRepresentation or IfcRepresentationItem.</param>
      /// <returns>The associated IfcLayerAssignment.</returns>
      static public IFCPresentationLayerAssignment GetTheLayerAssignment(IFCAnyHandle ifcLayeredItem)
      {
         IFCAnyHandle layerAssignmentHnd;
         if (!Importer.TheCache.LayerAssignment.TryGetValue(ifcLayeredItem, out layerAssignmentHnd))
            return null;

         IFCPresentationLayerAssignment layerAssignment = 
            ProcessIFCPresentationLayerAssignment(layerAssignmentHnd);
         return layerAssignment;
      }
   }
}
