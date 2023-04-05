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
      /// <summary>
      /// Check if an object placement is relative to the site's placement, and fix it if necessary.
      /// </summary>
      /// <param name="productEntity">The entity being checked.</param>
      /// <param name="productStepId">The id of the entity being checked.</param>
      /// <param name="objectPlacement">The object placement handle.</param>
      public static void CheckObjectPlacementIsRelativeToSite(IFCProduct productEntity, int productStepId,
         IFCAnyHandle objectPlacement)
      {
         if (BaseSiteOffset == null)
            return;

         IFCLocation productEntityLocation = productEntity.ObjectLocation;
         if (productEntityLocation != null && productEntityLocation.RelativeToSite == false)
         {
            if (!(productEntity is IFCSite))
            {
               if (!IFCAnyHandleUtil.IsSubTypeOf(objectPlacement, IFCEntityType.IfcGridPlacement))
               {
                  Importer.TheLog.LogWarning(productStepId, "The local placement (#" + objectPlacement.StepId + ") of this entity was not relative to the IfcSite's local placement, patching.", false);
               }

               if (productEntityLocation.RelativeTransform == null)
               {
                  productEntityLocation.RelativeTransform = Transform.CreateTranslation(-BaseSiteOffset);
               }
               else
               {
                  productEntityLocation.RelativeTransform.Origin -= BaseSiteOffset;
               }
            }
            productEntityLocation.RelativeToSite = true;
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
         base.Process(ifcIFCSite);

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

         IFCAnyHandle ifcPostalAddress = IFCImportHandleUtil.GetOptionalInstanceAttribute(ifcIFCSite, "SiteAddress");
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(ifcPostalAddress))
            SiteAddress = IFCPostalAddress.ProcessIFCPostalAddress(ifcPostalAddress);
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
      /// The optional address given to the site for postal purposes.
      /// </summary>
      public IFCPostalAddress SiteAddress { get; protected set; } = null;

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
      /// <param name="isType">True if the shared parameter is a type parameter.</param>
      /// <returns>The name appropriate for this IfcObjectDefinition.</returns>
      public override string GetSharedParameterName(IFCSharedParameters name, bool isType)
      {
         if (!isType)
         {
            switch (name)
            {
               case IFCSharedParameters.IfcName:
                  return "SiteName";
               case IFCSharedParameters.IfcDescription:
                  return "SiteDescription";
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

         if ((Id == Importer.TheCache.DefaultSiteId) && (CreatedElementId == ElementId.InvalidElementId))
            CreatedElementId = Importer.TheCache.ProjectInformationId;
      }

      /// <summary>
      /// The base site offset for this file.
      /// </summary>
      /// <remarks>This corresponds to the ProjectPosition, and should be
      /// used to offset objects placed not relative to a site.</remarks>
      public static XYZ BaseSiteOffset { get; set; } = null;

      /// <summary>
      /// Iterates through all of the IFCSites belonging to a an IFCProject, and finds the Default one.
      /// The default IFCSite is one that is composed of an IFCBuilding (or the first IFCSite if no IFCBuildings are found).
      /// </summary>
      /// <param name="sites">List of IFCSites in file.</param>
      public static void FindDefaultSite(IList<IFCSite> sites)
      {
         // If no sites, then stop the processing.
         //
         if ((sites?.Count ?? 0) == 0)
            return;

         IFCSite firstSite = sites[0];
         if (firstSite == null)
            return;

         Importer.TheCache.DefaultSiteId = firstSite.Id;

         foreach (IFCSite site in sites)
         {
            foreach (IFCObjectDefinition objectDefinition in site.ComposedObjectDefinitions)
            {
               if (objectDefinition is IFCBuilding)
               {
                  Importer.TheCache.DefaultSiteId = site.Id;
                  return;
               }
            }
         }
      }

      public static void ProcessSiteLocations(Document doc, IList<IFCSite> sites)
      {
         BaseSiteOffset = null;

         // Ideally, in most cases, this routine will do nothing.  In particular, that is 
         // true if the project has an arbitrary number of sites that are "close" to the
         // origin.

         if (sites == null || sites.Count == 0)
            return;

         ProjectLocation projectLocation = doc.ActiveProjectLocation;
         if (projectLocation == null)
            return;

         // If there is one site, and it is far from the origin, then we will move the site
         // close to the origin, give a warning, and set the shared coordinates in the file.

         // If there is more than one site, and at least one site is far from the origin:
         // 1. If all of the sites have an origin close to one another, then we will move the 
         // site close to the origin based on the first site encountered, give a warning, 
         // and set the shared coordinates in the file and the rest of the sites relative to
         // the first site.
         // 2. If the sites do not have origins close to one another, then we will do nothing
         // and give an error that the site is far from the origin and may have poor
         // performance and appearance.
         int numSites = sites.Count;
         bool hasSiteLocation = false;

         // First pass: deal with latitude and longitude.
         for (int ii = 0; ii < numSites; ii++)
         {
            IFCSite currSite = sites[ii];

            // Set the project latitude and longitude if the information is available, and
            // it hasn't already been set.
            SiteLocation siteLocation = projectLocation.GetSiteLocation();
            if (siteLocation != null)
            {
               // Some Tekla files may have invalid information here that would otherwise cause the
               // link to fail.  Recover with a warning.
               try
               {
                  bool foundSiteLocation = (currSite.RefLatitude.HasValue && currSite.RefLongitude.HasValue);
                  if (foundSiteLocation)
                  {
                     if (hasSiteLocation)
                     {
                        Importer.TheLog.LogWarning(currSite.Id, "Duplicate latitude or longitude value supplied for IFCSITE, ignoring.", false);
                     }
                     else
                     {
                        hasSiteLocation = true;
                        siteLocation.Latitude = currSite.RefLatitude.Value * Math.PI / 180.0;
                        siteLocation.Longitude = currSite.RefLongitude.Value * Math.PI / 180.0;
                     }
                  }
               }
               catch (Exception ex)
               {
                  Importer.TheLog.LogWarning(currSite.Id, "Invalid latitude or longitude value supplied for IFCSITE: " + ex.Message, false);
               }
            }
         }

         int? distantOriginFirstSiteId = null;

         for (int ii = 0; ii < numSites; ii++)
         {
            IFCSite currSite = sites[ii];

            // This is effectively no offset.  This is good, as long as we don't have
            // a distance origin.  In that case, we will warn and not do any special offsets.
            if (currSite.ObjectLocation?.RelativeTransform == null)
            {
               if (distantOriginFirstSiteId.HasValue)
               {
                  BaseSiteOffset = null;
                  break;
               }
               continue;
            }

            XYZ projectLoc = currSite.ObjectLocation.RelativeTransform.Origin;
            XYZ offset = new XYZ(projectLoc.X, projectLoc.Y, projectLoc.Z);
            if (XYZ.IsWithinLengthLimits(offset))
            {
               if (distantOriginFirstSiteId.HasValue)
               {
                  BaseSiteOffset = null;
                  break;
               }
               continue;
            }

            if (BaseSiteOffset == null)
            {
               distantOriginFirstSiteId = currSite.Id;

               // If the index is greater than 0, then we have found some sites close to the
               // origin.  That means we have incompatible origins which is an issue.
               if (ii == 0)
                  BaseSiteOffset = offset;
               else
                  break;
            }
         }

         if (BaseSiteOffset != null)
         {
            // Modify the RelativeTransforms for each of these sites.
            // Note that the RelativeTransform must be defined to have gotten here.
            for (int ii = 0; ii < numSites; ii++)
            {
               XYZ currentOffset =
                  new XYZ(-BaseSiteOffset.X, -BaseSiteOffset.Y, -BaseSiteOffset.Z /*+ sites[ii].RefElevation*/);
               Transform newSiteTransform = sites[ii].ObjectLocation.TotalTransform;
               newSiteTransform.Origin += currentOffset;
               sites[ii].ObjectLocation = IFCLocation.CreateDummyLocation(newSiteTransform);
            }

            // Register the offset by moving the Shared Coordinates away
            ProjectPosition pPos = projectLocation.GetProjectPosition(XYZ.Zero);
            pPos.EastWest += BaseSiteOffset.X;
            pPos.NorthSouth += BaseSiteOffset.Y;
            pPos.Elevation += BaseSiteOffset.Z;
            projectLocation.SetProjectPosition(XYZ.Zero, pPos);
         }
         else
         {
            // In this case, we just have to make sure that the RefElevation is included in
            // the site transform.
            for (int ii = 0; ii < numSites; ii++)
            {
               if (MathUtil.IsAlmostZero(sites[ii].RefElevation))
                  continue;

               if (sites[ii].ObjectLocation == null || sites[ii].ObjectLocation.RelativeTransform == null)
               {
                  XYZ currentOffset = XYZ.Zero;
                  sites[ii].ObjectLocation = IFCLocation.CreateDummyLocation(Transform.CreateTranslation(currentOffset));
               }
               else
               {
                  double currRefElevation = sites[ii].RefElevation;
                  double currZOffset = sites[ii].ObjectLocation.RelativeTransform.Origin.Z;
                  if (!MathUtil.IsAlmostEqual(currZOffset, currRefElevation))
                  {
                     Transform newSiteTransform = sites[ii].ObjectLocation.TotalTransform;
                     sites[ii].ObjectLocation = IFCLocation.CreateDummyLocation(newSiteTransform);
                  }
               }
            }
         }

         if (BaseSiteOffset == null && distantOriginFirstSiteId.HasValue)
         {
            Importer.TheLog.LogError(distantOriginFirstSiteId.Value, "There are multiple sites in the file that are located far away from each other.  This may result in poor visualization of the data.", false);
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
            {
               Category category = IFCPropertySet.GetCategoryForParameterIfValid(element, Id);
               IFCPropertySet.AddParameterString(doc, element, category, this, parameterName, landTitleNumber, Id);
            }
         }

         CreatePostalParameters(doc, element, SiteAddress);

         ForgeTypeId lengthUnits = null;
         if (!Importer.TheProcessor.ScaleValues)
         {
            lengthUnits = IFCImportFile.TheFile.IFCUnits.GetIFCProjectUnit(SpecTypeId.Length)?.Unit;
         }
         
         Importer.TheProcessor.PostProcessSite(Id, RefLatitude,
            RefLongitude, RefElevation, LandTitleNumber, lengthUnits,
            ObjectLocation?.TotalTransform);
      }
   }
}