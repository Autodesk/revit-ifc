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
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcGrid, which corresponds to a group of Revit Grid elements.
   /// The fields of the IFCGrid class correspond to the IfcGrid entity defined in the IFC schema.
   /// </summary>
   public class IFCGrid : IFCProduct
   {
      private IList<IFCGridAxis> m_UAxes = null;

      private IList<IFCGridAxis> m_VAxes = null;

      private IList<IFCGridAxis> m_WAxes = null;

      private IDictionary<ElementId, IFCPresentationLayerAssignment> m_PresentationLayerAssignmentsForAxes = null;

      /// <summary>
      /// The required list of U Axes.
      /// </summary>
      public IList<IFCGridAxis> UAxes
      {
         get
         {
            if (m_UAxes == null)
               m_UAxes = new List<IFCGridAxis>();

            return m_UAxes;
         }
         private set { m_UAxes = value; }
      }

      /// <summary>
      /// The required list of V Axes.
      /// </summary>
      public IList<IFCGridAxis> VAxes
      {
         get
         {
            if (m_VAxes == null)
               m_VAxes = new List<IFCGridAxis>();

            return m_VAxes;
         }
         private set { m_VAxes = value; }
      }

      /// <summary>
      /// The optional list of W Axes.
      /// </summary>
      public IList<IFCGridAxis> WAxes
      {
         get
         {
            if (m_WAxes == null)
               m_WAxes = new List<IFCGridAxis>();

            return m_WAxes;
         }
         private set { m_WAxes = value; }
      }

      /// <summary>
      /// A map of grid axes to their presentation layer assignments.  Overrides the IfcGrid PresentationLayerNames if present.
      /// </summary>
      protected IDictionary<ElementId, IFCPresentationLayerAssignment> PresentationLayerAssignmentsForAxes
      {
         get
         {
            if (m_PresentationLayerAssignmentsForAxes == null)
               m_PresentationLayerAssignmentsForAxes = new Dictionary<ElementId, IFCPresentationLayerAssignment>();
            return m_PresentationLayerAssignmentsForAxes;
         }
      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCGrid()
      {

      }

      /// <summary>
      /// Constructs an IFCGrid from the IfcGrid handle.
      /// </summary>
      /// <param name="ifcGrid">The IfcGrid handle.</param>
      protected IFCGrid(IFCAnyHandle ifcGrid)
      {
         Process(ifcGrid);
      }

      private IList<IFCGridAxis> ProcessOneAxis(IFCAnyHandle ifcGrid, string axisName)
      {
         IList<IFCGridAxis> gridAxes = new List<IFCGridAxis>();

         List<IFCAnyHandle> ifcAxes = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcGrid, axisName);
         if (ifcAxes != null)
         {
            foreach (IFCAnyHandle axis in ifcAxes)
            {
               IFCGridAxis gridAxis = IFCGridAxis.ProcessIFCGridAxis(axis);
               if (gridAxis != null)
               {
                  if (gridAxis.DuplicateAxisId == -1)
                     gridAxes.Add(gridAxis);
                  else
                  {
                     IFCEntity originalEntity;
                     if (IFCImportFile.TheFile.EntityMap.TryGetValue(gridAxis.DuplicateAxisId, out originalEntity))
                     {
                        IFCGridAxis originalGridAxis = originalEntity as IFCGridAxis;
                        if (originalGridAxis != null)
                           gridAxes.Add(originalGridAxis);
                     }
                  }
               }
            }
         }

         return gridAxes;
      }

      /// <summary>
      /// Add to a set of created element ids, based on elements created from the contained grid axes.
      /// </summary>
      /// <param name="createdElementIds">The set of created element ids.</param>
      public override void GetCreatedElementIds(ISet<ElementId> createdElementIds)
      {
         foreach (IFCGridAxis uaxis in UAxes)
         {
            ElementId gridId = uaxis.CreatedElementId;
            if (gridId != ElementId.InvalidElementId)
               createdElementIds.Add(gridId);
         }

         foreach (IFCGridAxis vaxis in VAxes)
         {
            ElementId gridId = vaxis.CreatedElementId;
            if (gridId != ElementId.InvalidElementId)
               createdElementIds.Add(gridId);
         }

         foreach (IFCGridAxis waxis in WAxes)
         {
            ElementId gridId = waxis.CreatedElementId;
            if (gridId != ElementId.InvalidElementId)
               createdElementIds.Add(gridId);
         }
      }

      /// <summary>
      /// Processes IfcGrid attributes.
      /// </summary>
      /// <param name="ifcGrid">The IfcGrid handle.</param>
      protected override void Process(IFCAnyHandle ifcGrid)
      {
         base.Process(ifcGrid);

         // We will be lenient and allow for missing U and V axes.
         UAxes = ProcessOneAxis(ifcGrid, "UAxes");
         VAxes = ProcessOneAxis(ifcGrid, "VAxes");
         WAxes = ProcessOneAxis(ifcGrid, "WAxes");
      }

      /// <summary>
      /// Create either the U, V, or W grid lines.
      /// </summary>
      /// <param name="axes">The list of axes in a particular direction.</param>
      /// <param name="doc">The document.</param>
      /// <param name="lcs">The local transform.</param>
      private void CreateOneDirection(IList<IFCGridAxis> axes, Document doc, Transform lcs)
      {
         foreach (IFCGridAxis axis in axes)
         {
            if (axis == null)
               continue;

            try
            {
               axis.Create(doc, lcs);
               ElementId createdElementId = axis.CreatedElementId;
               if (createdElementId != ElementId.InvalidElementId && axis.AxisCurve != null && axis.AxisCurve.LayerAssignment != null)
                  PresentationLayerAssignmentsForAxes[createdElementId] = axis.AxisCurve.LayerAssignment;
            }
            catch
            {
            }
         }
      }

      /// <summary>
      /// As IfcGrid should have at most one associated IFCPresentationLayerAssignment.  Return it if it exists.
      /// </summary>
      /// <returns>The associated IFCPresentationLayerAssignment, or null.</returns>
      protected IFCPresentationLayerAssignment GetTheFirstPresentationLayerAssignment()
      {
         if (ProductRepresentation == null)
            return null;

         IList<IFCRepresentation> representations = ProductRepresentation.Representations;
         if (representations == null)
            return null;

         foreach (IFCRepresentation representation in representations)
         {
            IList<IFCRepresentationItem> representationItems = representation.RepresentationItems;
            if (representationItems == null)
               continue;

            foreach (IFCRepresentationItem representationItem in representationItems)
            {
               // We will favor the layer assignment of the items over the representation itself.
               if (representationItem.LayerAssignment != null)
                  return representationItem.LayerAssignment;
            }

            if (representation.LayerAssignment != null)
               return representation.LayerAssignment;
         }

         return null;
      }

      /// <summary>
      /// Override PresentationLayerNames for the current axis.
      /// </summary>
      /// <param name="defaultLayerAssignmentName">The grid's layer assignment name</param>
      /// <param name="hasDefaultLayerAssignmentName">True if defaultLayerAssignmentName isn't empty.</param>
      private void SetCurrentPresentationLayerNames(string defaultLayerAssignmentName, bool hasDefaultLayerAssignmentName)
      {
         PresentationLayerNames.Clear();

         // We will get the presentation layer names from either the grid lines or the grid, with
         // grid lines getting the higher priority.
         IFCPresentationLayerAssignment currentLayerAssigment;
         if (PresentationLayerAssignmentsForAxes.TryGetValue(CreatedElementId, out currentLayerAssigment) &&
             currentLayerAssigment != null &&
             !string.IsNullOrWhiteSpace(currentLayerAssigment.Name))
            PresentationLayerNames.Add(currentLayerAssigment.Name);
         else if (hasDefaultLayerAssignmentName)
            PresentationLayerNames.Add(defaultLayerAssignmentName);
      }

      /// <summary>
      /// Allow for override of IfcObjectDefinition shared parameter names.
      /// </summary>
      /// <param name="name">The enum corresponding of the shared parameter.</param>
      /// <returns>The name appropriate for this IfcObjectDefinition.</returns>
      public override string GetSharedParameterName(IFCSharedParameters name)
      {
         switch (name)
         {
            case IFCSharedParameters.IfcName:
               return "IfcGrid Name";
            case IFCSharedParameters.IfcDescription:
               return "IfcGrid Description";
            default:
               return base.GetSharedParameterName(name);
         }
      }

      /// <summary>
      /// Creates or populates Revit elements based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      protected override void Create(Document doc)
      {
         Transform lcs = (ObjectLocation != null) ? ObjectLocation.TotalTransform : Transform.Identity;

         CreateOneDirection(UAxes, doc, lcs);
         CreateOneDirection(VAxes, doc, lcs);
         CreateOneDirection(WAxes, doc, lcs);

         ISet<ElementId> createdElementIds = new HashSet<ElementId>();
         GetCreatedElementIds(createdElementIds);

         // We want to get the presentation layer from the Grid representation, if any.
         IFCPresentationLayerAssignment defaultLayerAssignment = GetTheFirstPresentationLayerAssignment();
         string defaultLayerAssignmentName = (defaultLayerAssignment != null) ? defaultLayerAssignment.Name : null;
         bool hasDefaultLayerAssignmentName = !string.IsNullOrWhiteSpace(defaultLayerAssignmentName);

         foreach (ElementId createdElementId in createdElementIds)
         {
            CreatedElementId = createdElementId;

            SetCurrentPresentationLayerNames(defaultLayerAssignmentName, hasDefaultLayerAssignmentName);
            CreateParameters(doc);
         }

         CreatedElementId = ElementId.InvalidElementId;
      }

      /// <summary>
      /// Processes an IfcGrid object.
      /// </summary>
      /// <param name="ifcGrid">The IfcGrid handle.</param>
      /// <returns>The IFCGrid object.</returns>
      public static IFCGrid ProcessIFCGrid(IFCAnyHandle ifcGrid)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcGrid))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcGrid);
            return null;
         }

         IFCEntity grid;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcGrid.StepId, out grid))
            grid = new IFCGrid(ifcGrid);
         return (grid as IFCGrid);
      }
   }
}