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
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcSite.
   /// </summary>
   public class IFCSite : IFCSpatialStructureElement
   {
      // TODO: handle SiteAddress.

      public class ActiveSiteSetter : IDisposable
      {
         public ActiveSiteSetter(IFCSite ifcSite)
         {
            ActiveSite = ifcSite;
         }

         public static IFCSite ActiveSite { get; private set; }

         public static void CheckObjectPlacementIsRelativeToSite(IFCProduct productEntity, int productStepId, int objectPlacementStepId)
         {
            IFCLocation productEntityLocation = productEntity.ObjectLocation;
            if (ActiveSite != null && productEntityLocation != null && productEntityLocation.RelativeToSite == false)
            {
               if (!(productEntity is IFCSite))
               {
                  IFCLocation activeSiteLocation = ActiveSite.ObjectLocation;
                  if (activeSiteLocation != null)
                  {
                     Importer.TheLog.LogWarning(productStepId, "The local placement (#" + objectPlacementStepId + ") of this entity was not relative to the IfcSite's local placement, patching.", false);
                     Transform siteTransform = activeSiteLocation.TotalTransform;
                     if (siteTransform != null)
                     {
                        Transform siteTransformInverse = siteTransform.Inverse;
                        Transform originalTotalTransform = productEntityLocation.TotalTransform;
                        if (originalTotalTransform == null)
                           productEntityLocation.RelativeTransform = siteTransformInverse;
                        else
                           productEntityLocation.RelativeTransform = originalTotalTransform.Multiply(siteTransformInverse);
                     }

                     productEntityLocation.RelativeTo = activeSiteLocation;
                  }
               }
               productEntityLocation.RelativeToSite = true;
            }
      }

         public void Dispose()
         {
            ActiveSite = null;
         }
      }

      /// <summary>
      /// Constructs an IFCSite from the IfcSite handle.
      /// </summary>
      /// <param name="ifcIFCSite">The IfcSite handle.</param>
      protected IFCSite(IFCAnyHandle ifcIFCSite)
      {
         Process(ifcIFCSite);
      }

      private double GetLatLongScale(int index)
      {
         switch (index)
         {
            case 0:
               return 1.0;
            case 1:
               return 60.0;
            case 2:
               return 3600.0;
            case 3:
               return 3600000000.0;
         }

         return 1.0;
      }

      /// <summary>
      /// Processes IfcSite attributes.
      /// </summary>
      /// <param name="ifcIFCSite">The IfcSite handle.</param>
      protected override void Process(IFCAnyHandle ifcIFCSite)
      {
         using (ActiveSiteSetter setter = new ActiveSiteSetter(this))
         {
            base.Process(ifcIFCSite);
         }

         RefElevation = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(ifcIFCSite, "RefElevation", 0.0);

         IList<int> refLatitudeList = IFCAnyHandleUtil.GetAggregateIntAttribute<List<int>>(ifcIFCSite, "RefLatitude");
         IList<int> refLongitudeList = IFCAnyHandleUtil.GetAggregateIntAttribute<List<int>>(ifcIFCSite, "RefLongitude");

         if (refLatitudeList != null)
         {
            RefLatitude = 0.0;
            int numLats = Math.Min(refLatitudeList.Count, 4);   // Only support up to degress, minutes, seconds, and millionths of seconds.
            for (int ii = 0; ii < numLats; ii++)
            {
               RefLatitude += ((double)refLatitudeList[ii]) / GetLatLongScale(ii);
            }
         }

         if (refLongitudeList != null)
         {
            RefLongitude = 0.0;
            int numLongs = Math.Min(refLongitudeList.Count, 4);   // Only support up to degress, minutes, seconds, and millionths of seconds.
            for (int ii = 0; ii < numLongs; ii++)
            {
               RefLongitude += ((double)refLongitudeList[ii]) / GetLatLongScale(ii);
            }
         }

         LandTitleNumber = IFCAnyHandleUtil.GetStringAttribute(ifcIFCSite, "LandTitleNumber");
      }

      /// <summary>
      /// The site elevation, in Revit internal units.
      /// </summary>
      public double RefElevation { get; protected set; } = 0.0;

      /// <summary>
      /// The site latitude, in degrees.
      /// </summary>
      public double? RefLatitude { get; protected set; } = null;

      /// <summary>
      /// The site longitude, in degrees.
      /// </summary>
      public double? RefLongitude { get; protected set; } = null;

      /// <summary>
      /// The Land Title number.
      /// </summary>
      public string LandTitleNumber { get; protected set; } = null;

      /// <summary>
      /// Processes an IfcSite object.
      /// </summary>
      /// <param name="ifcSite">The IfcSite handle.</param>
      /// <returns>The IFCSite object.</returns>
      public static IFCSite ProcessIFCSite(IFCAnyHandle ifcSite)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSite))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcSite);
            return null;
         }

         IFCEntity site;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSite.StepId, out site))
            return (site as IFCSite);

         return new IFCSite(ifcSite);
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
               return "IfcSite Name";
            case IFCSharedParameters.IfcDescription:
               return "IfcSite Description";
            default:
               return base.GetSharedParameterName(name);
         }
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
         // Only set the project location for the site that contains the building.
         bool hasBuilding = false;

         foreach (IFCObjectDefinition objectDefinition in ComposedObjectDefinitions)
         {
            if (objectDefinition is IFCBuilding)
            {
               hasBuilding = true;
               break;
            }
         }

         if (hasBuilding)
         {
            ProjectLocation projectLocation = doc.ActiveProjectLocation;
            if (projectLocation != null)
            {
               SiteLocation siteLocation = projectLocation.GetSiteLocation();
               if (siteLocation != null)
               {
                  // Some Tekla files may have invalid information here that would otherwise cause the
                  // link to fail.  Recover with a warning.
                  try
                  {
                     if (RefLatitude.HasValue)
                        siteLocation.Latitude = RefLatitude.Value * Math.PI / 180.0;
                     if (RefLongitude.HasValue)
                        siteLocation.Longitude = RefLongitude.Value * Math.PI / 180.0;
                  }
                  catch (Exception ex)
                  {
                     Importer.TheLog.LogWarning(Id, "Invalid latitude or longitude value supplied for IFCSITE: " + ex.Message, false);
                  }
               }

               if (ObjectLocation != null)
               {
                  XYZ projectLoc = (ObjectLocation.RelativeTransform != null) ? ObjectLocation.RelativeTransform.Origin : XYZ.Zero;
                  if (!MathUtil.IsAlmostZero(projectLoc.Z))
                     Importer.TheLog.LogError(Id, "The Z-value of the IfcSite object placement relative transform should be 0.  This will be ignored in favor of the RefElevation value.", false);

                  // Get true north from IFCProject.
                  double trueNorth = 0.0;
                  UV trueNorthUV = IFCImportFile.TheFile.IFCProject.TrueNorthDirection;
                  if (trueNorthUV != null)
                  {
                     double geometricAngle = Math.Atan2(trueNorthUV.V, trueNorthUV.U);
                     // Convert from geometric angle to compass direction.
                     // This involves two steps: (1) subtract PI/2 from the angle, staying in (-PI, PI], then (2) reversing the result.
                     trueNorth = (geometricAngle > -Math.PI / 2.0) ? geometricAngle - Math.PI / 2.0 : geometricAngle + Math.PI * 1.5;
                     trueNorth = -trueNorth;
                  }

                  ProjectPosition projectPosition = new ProjectPosition(projectLoc.X, projectLoc.Y, RefElevation, trueNorth);

                  projectLocation.SetProjectPosition(XYZ.Zero, projectPosition);

                  // Now that we've set the project position, remove the site relative transform, if the file is created correctly (that is, all entities contained in the site
                  // have the local placements relative to the site.
                  IFCLocation.RemoveRelativeTransformForSite(this);
               }
            }
         }

         base.Create(doc);

         if (hasBuilding)
         {
            // There should only be one IfcSite in the file, but in case there are multiple, we want to make sure that the one
            // containing the IfcBuilding has its parameters stored somewhere.
            // In the case where we didn't create an element above, use the ProjectInfo element in the document to store its parameters.
            if (CreatedElementId == ElementId.InvalidElementId)
               CreatedElementId = Importer.TheCache.ProjectInformationId;
         }
      }

      /// <summary>
      /// Creates or populates Revit element params based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element.</param>
      protected override void CreateParametersInternal(Document doc, Element element)
      {
         base.CreateParametersInternal(doc, element);
         string parameterName = "LandTitleNumber";

         // TODO: move this to new shared parameter names override function.
         if (element is ProjectInfo)
         {
            parameterName = "IfcSite " + parameterName;
         }

         if (element != null)
         {
            string landTitleNumber = LandTitleNumber;
            if (!string.IsNullOrWhiteSpace(landTitleNumber))
               IFCPropertySet.AddParameterString(doc, element, parameterName, landTitleNumber, Id);
         }
      }
   }
}