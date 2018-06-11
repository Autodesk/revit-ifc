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
      string m_Name = null;

      string m_Description = null;

      IList<IFCEntity> m_AssignedItems = new List<IFCEntity>();

      string m_Identifier = null;

      /// <summary>
      /// Get the name of the IFCPresentationLayerAssignment.
      /// </summary>
      public string Name
      {
         get { return m_Name; }
         protected set { m_Name = value; }
      }

      /// <summary>
      /// Get the optional description of the IFCPresentationLayerAssignment.
      /// </summary>
      public string Description
      {
         get { return m_Description; }
         protected set { m_Description = value; }
      }

      /// <summary>
      /// Get the optional identifier of the IFCPresentationLayerAssignment.
      /// </summary>
      public string Identifier
      {
         get { return m_Identifier; }
         protected set { m_Identifier = value; }
      }

      public IList<IFCEntity> AssignedItems
      {
         get { return m_AssignedItems; }
         protected set { m_AssignedItems = value; }
      }

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

         IList<IFCAnyHandle> assignedItems = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(item, "AssignedItems");
         foreach (IFCAnyHandle assignedItem in assignedItems)
         {
            // We do NOT process items here.  We only use already created representations and representation items.
            IFCEntity entity = null;
            if (!IFCImportFile.TheFile.EntityMap.TryGetValue(assignedItem.StepId, out entity))
               continue;

            if (IFCAnyHandleUtil.IsSubTypeOf(assignedItem, IFCEntityType.IfcRepresentation))
               (entity as IFCRepresentation).PostProcessLayerAssignment(this);
            else if (IFCAnyHandleUtil.IsSubTypeOf(assignedItem, IFCEntityType.IfcRepresentationItem))
               (entity as IFCRepresentationItem).PostProcessLayerAssignment(this);

            if (entity != null)
               AssignedItems.Add(entity);
            else
               Importer.TheLog.LogUnhandledSubTypeError(assignedItem, "IfcLayeredItem", false);
         }
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

         if (!IFCEntity.AreIFCEntityListsEquivalent(AssignedItems, other.AssignedItems))
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
      /// <param name="isIFCRepresentation">True if the handle is an IfcRepresentation.  This determines the name of the inverse attribute.</param>
      /// <returns>The associated IfcLayerAssignment.</returns>
      /// <remarks>This deals with the issues that:
      /// 1. the default IFC2x3 EXP file doesn't have this inverse attribute set.
      /// 2. The name changed in IFC4.
      /// 3. The attribute didn't exist before IFC2x3.</remarks>
      static public IFCPresentationLayerAssignment GetTheLayerAssignment(IFCAnyHandle ifcLayeredItem, bool isIFCRepresentation)
      {
         IFCPresentationLayerAssignment theLayerAssignment = null;
         IList<IFCAnyHandle> layerAssignments = null;

         if (IFCImportFile.TheFile.Options.AllowUseLayerAssignments)
         {
            // Inverse attribute changed names in IFC4 for IfcRepresentationItem only.
            string layerAssignmentsAttributeName = (isIFCRepresentation || IFCImportFile.TheFile.SchemaVersion < IFCSchemaVersion.IFC4) ? "LayerAssignments" : "LayerAssignment";
            try
            {
               layerAssignments = IFCAnyHandleUtil.GetAggregateInstanceAttribute
                   <List<IFCAnyHandle>>(ifcLayeredItem, layerAssignmentsAttributeName);
            }
            catch
            {
               IFCImportFile.TheFile.Options.AllowUseLayerAssignments = false;
               layerAssignments = null;
            }
         }

         if (layerAssignments != null && layerAssignments.Count > 0)
         {
            // We can only handle one layer assignment, but we allow the possiblity that there are duplicates.  Do a top-level check.
            foreach (IFCAnyHandle layerAssignment in layerAssignments)
            {
               if (!IFCAnyHandleUtil.IsSubTypeOf(layerAssignment, IFCEntityType.IfcPresentationLayerAssignment))
               {
                  Importer.TheLog.LogUnexpectedTypeError(layerAssignment, IFCEntityType.IfcStyledItem, false);
                  theLayerAssignment = null;
                  break;
               }
               else
               {
                  IFCPresentationLayerAssignment compLayerAssignment = IFCPresentationLayerAssignment.ProcessIFCPresentationLayerAssignment(layerAssignment);
                  if (theLayerAssignment == null)
                  {
                     theLayerAssignment = compLayerAssignment;
                     continue;
                  }

                  if (!IFCImportDataUtil.CheckLayerAssignmentConsistency(theLayerAssignment, compLayerAssignment, ifcLayeredItem.StepId))
                     break;
               }
            }
         }

         return theLayerAssignment;
      }

      static public void ProcessAllLayerAssignments()
      {
         IList<IFCAnyHandle> layerAssignments = IFCImportFile.TheFile.GetInstances(IFCEntityType.IfcPresentationLayerAssignment, true);

         if (layerAssignments != null)
         {
            foreach (IFCAnyHandle layerAssignment in layerAssignments)
            {
               IFCPresentationLayerAssignment.ProcessIFCPresentationLayerAssignment(layerAssignment);
            }
         }
      }
   }
}