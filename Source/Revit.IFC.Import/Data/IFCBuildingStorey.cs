﻿//
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

using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcBuildingStorey.
   /// </summary>
   public class IFCBuildingStorey : IFCSpatialStructureElement
   {
      static ElementId ViewPlanTypeId { get; set; } = ElementId.InvalidElementId;

      /// <summary>
      /// Returns true if we have tried to set ViewPlanTypeId.  ViewPlanTypeId may or may not have a valid value.
      /// </summary>
      static bool ViewPlanTypeIdInitialized { get; set; } = false;

      /// <summary>
      /// Returns the associated Plan View for the level.
      /// </summary>
      public ElementId CreatedViewId { get; protected set; } = ElementId.InvalidElementId;

      /// <summary>
      /// If the ActiveView is level-based, we can't delete it.  Instead, use it for the first level "created".
      /// </summary>
      public static ElementId ExistingLevelIdToReuse { get; set; } = ElementId.InvalidElementId;

      /// <summary>
      /// Get the default family type for creating ViewPlans.
      /// </summary>
      /// <param name="doc"></param>
      /// <returns></returns>
      public static ElementId GetViewPlanTypeId(Document doc)
      {
         if (ViewPlanTypeIdInitialized == false)
         {
            ViewFamily viewFamilyToUse = (doc.Application.Product == ProductType.Structure) ? ViewFamily.StructuralPlan : ViewFamily.FloorPlan;

            ViewPlanTypeIdInitialized = true;
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> viewFamilyTypes = collector.OfClass(typeof(ViewFamilyType)).ToElements();
            foreach (Element element in viewFamilyTypes)
            {
               ViewFamilyType viewFamilyType = element as ViewFamilyType;
               if (viewFamilyType.ViewFamily == viewFamilyToUse)
               {
                  ViewPlanTypeId = viewFamilyType.Id;
                  break;
               }
            }
         }
         return ViewPlanTypeId;
      }

      /// <summary>
      /// Constructs an IFCBuildingStorey from the IfcBuildingStorey handle.
      /// </summary>
      /// <param name="ifcIFCBuildingStorey">The IfcBuildingStorey handle.</param>
      protected IFCBuildingStorey(IFCAnyHandle ifcIFCBuildingStorey)
      {
         Process(ifcIFCBuildingStorey);
      }

      /// <summary>
      /// Creates or populates Revit element params based on the information contained in this class.
      /// </summary>
      /// <param name="doc"></param>
      /// <param name="element"></param>
      protected override void CreateParametersInternal(Document doc, Element element)
      {
         base.CreateParametersInternal(doc, element);

         if (element != null)
         {
            // Set "IfcElevation" parameter.
            Category category = IFCPropertySet.GetCategoryForParameterIfValid(element, Id);
            IFCPropertySet.AddParameterDouble(doc, element, category, this, "IfcElevation", SpecTypeId.Length, null, Elevation, Id);
         }
      }

      /// <summary>
      /// Creates or populates Revit elements based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      protected override void Create(Document doc)
      {
         // We may re-use the ActiveView Level and View, since we can't delete them.
         // We will consider that we "created" this level and view for creation metrics.
         Level level = Importer.TheCache.UseElementByGUID<Level>(doc, GlobalId);

         bool reusedLevel = false;
         bool foundLevel = false;

         if (level == null)
         {
            if (ExistingLevelIdToReuse != ElementId.InvalidElementId)
            {
               level = doc.GetElement(ExistingLevelIdToReuse) as Level;
               Importer.TheCache.UseElement(level);
               ExistingLevelIdToReuse = ElementId.InvalidElementId;
               reusedLevel = true;
            }
         }
         else
            foundLevel = true;

         if (level == null)
            level = Level.Create(doc, Elevation);
         else
            level.Elevation = Elevation;

         if (level != null)
            CreatedElementId = level.Id;

         if (CreatedElementId != ElementId.InvalidElementId)
         {
            if (!foundLevel)
            {
               if (!reusedLevel)
               {
                  ElementId viewPlanTypeId = IFCBuildingStorey.GetViewPlanTypeId(doc);
                  if (viewPlanTypeId != ElementId.InvalidElementId)
                  {
                     ViewPlan viewPlan = ViewPlan.Create(doc, viewPlanTypeId, CreatedElementId);
                     if (viewPlan != null)
                        CreatedViewId = viewPlan.Id;
                  }

                  if (CreatedViewId == ElementId.InvalidElementId)
                     Importer.TheLog.LogAssociatedCreationError(this, typeof(ViewPlan));
               }
               else
               {
                  if (doc.ActiveView != null)
                     CreatedViewId = doc.ActiveView.Id;
               }
            }
         }
         else
            Importer.TheLog.LogCreationError(this, null, false);

         TraverseSubElements(doc);
      }

      /// <summary>
      /// Get the element ids created for this entity, for summary logging.
      /// </summary>
      /// <param name="createdElementIds">The creation list.</param>
      /// <remarks>May contain InvalidElementId; the caller is expected to remove it.</remarks>
      public override void GetCreatedElementIds(ISet<ElementId> createdElementIds)
      {
         base.GetCreatedElementIds(createdElementIds);
         if (CreatedViewId != ElementId.InvalidElementId)
            createdElementIds.Add(CreatedViewId);
      }

      /// <summary>
      /// Processes IfcBuildingStorey attributes.
      /// </summary>
      /// <param name="ifcIFCBuildingStorey">The IfcBuildingStorey handle.</param>
      protected override void Process(IFCAnyHandle ifcIFCBuildingStorey)
      {
         base.Process(ifcIFCBuildingStorey);

         Elevation = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(ifcIFCBuildingStorey, "Elevation", 0.0);
      }

      /// <summary>
      /// The elevation.
      /// </summary>
      public double Elevation { get; protected set; } = 0.0;

      /// <summary>
      /// Processes an IfcBuildingStorey object.
      /// </summary>
      /// <param name="ifcBuildingStorey">The IfcBuildingStorey handle.</param>
      /// <returns>The IFCBuildingStorey object.</returns>
      public static IFCBuildingStorey ProcessIFCBuildingStorey(IFCAnyHandle ifcBuildingStorey)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcBuildingStorey))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcBuildingStorey);
            return null;
         }

         IFCEntity buildingStorey;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcBuildingStorey.StepId, out buildingStorey))
            buildingStorey = new IFCBuildingStorey(ifcBuildingStorey);
         return (buildingStorey as IFCBuildingStorey);
      }
   }
}