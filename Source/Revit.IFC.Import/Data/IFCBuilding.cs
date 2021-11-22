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

using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcBuilding.
   /// </summary>
   public class IFCBuilding : IFCSpatialStructureElement
   {
      /// <summary>
      /// Constructs an IFCBuilding from the IfcBuilding handle.
      /// </summary>
      /// <param name="ifcBuilding">The IfcBuilding handle.</param>
      protected IFCBuilding(IFCAnyHandle ifcBuilding)
      {
         Process(ifcBuilding);
      }

      /// <summary>
      /// The base elevation of the building.
      /// </summary>
      public double ElevationOfRefHeight { get; protected set; } = 0.0;

      /// <summary>
      /// Processes IfcBuilding attributes.
      /// </summary>
      /// <param name="ifcBuilding">The IfcBuilding handle.</param>
      protected override void Process(IFCAnyHandle ifcBuilding)
      {
         // TODO: process IfcBuilding specific data.
         ElevationOfRefHeight = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(ifcBuilding, "ElevationOfRefHeight", 0.0);

         base.Process(ifcBuilding);
      }

      public override void PostProcess()
      {
         try
         {
            TryToFixFarawayOrigin();
         }
         catch
         {
            //2022.0.1 doesnt contain BoundingBoxXYZ.IsSet that used in TryToFixFarawayOrigin()
         }
         base.PostProcess();
      }


      /// <summary>
      /// Allow for override of IfcObjectDefinition shared parameter names.
      /// </summary>
      /// <param name="name">The enum corresponding of the shared parameter.</param>
      /// <param name="isType">True if the shared parameter is a type parameter.</param>
      /// <returns>The name appropriate for this IfcObjectDefinition.</returns>
      public override string GetSharedParameterName(IFCSharedParameters name, bool isType)
      {
         if (!isType)
         {
            switch (name)
            {
               case IFCSharedParameters.IfcName:
                  return "BuildingName";
               case IFCSharedParameters.IfcDescription:
                  return "BuildingDescription";
            }
         }

         return base.GetSharedParameterName(name, isType);
      }

      /// <summary>
      /// Get the element ids created for this entity, for summary logging.
      /// </summary>
      /// <param name="createdElementIds">The creation list.</param>
      /// <remarks>May contain InvalidElementId; the caller is expected to remove it.</remarks>
      public override void GetCreatedElementIds(ISet<ElementId> createdElementIds)
      {
         // If we used ProjectInformation, don't report that.
         if (CreatedElementId != ElementId.InvalidElementId && CreatedElementId != Importer.TheCache.ProjectInformationId)
         {
            createdElementIds.Add(CreatedElementId);
         }
      }

      /// <summary>
      /// Creates or populates Revit elements based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      protected override void Create(Document doc)
      {
         base.Create(doc);

         IFCLocation.WarnIfFaraway(this);

         // IfcBuilding usually won't create an element, as it contains no geometry.
         // If it doesn't, use the ProjectInfo element in the document to store its parameters.
         if (CreatedElementId == ElementId.InvalidElementId)
            CreatedElementId = Importer.TheCache.ProjectInformationId;
      }

      /// <summary>
      /// Processes an IfcBuilding object.
      /// </summary>
      /// <param name="ifcBuilding">The IfcBuilding handle.</param>
      /// <returns>The IFCBuilding object.</returns>
      public static IFCBuilding ProcessIFCBuilding(IFCAnyHandle ifcBuilding)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcBuilding))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcBuilding);
            return null;
         }

         IFCEntity building;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcBuilding.StepId, out building))
            building = new IFCBuilding(ifcBuilding);
         return (building as IFCBuilding);
      }
   }
}