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

      /// <summary>
      /// A helper class that checks object placement, intended to be used within a "using" scope.
      /// </summary>
      public class ActiveSiteSetter : IDisposable
      {
         /// <summary>
         /// The constuctor.
         /// </summary>
         /// <param name="ifcSite">The current IFCSite being processed.</param>
         public ActiveSiteSetter(IFCSite ifcSite)
         {
            IFCLocation siteLocation = ifcSite?.ObjectLocation;
            Transform siteTransform = siteLocation?.TotalTransform;

            Tuple<IFCLocation, Transform> newSiteInfo = null;

            if (siteTransform != null)
            {
               // We intend to get rid of ActiveSiteSetter entirely.  This is the first step -
               // only use the site transform if it is far from the origin.
               if (!XYZ.IsWithinLengthLimits(siteTransform.Origin))
               {
                  newSiteInfo = Tuple.Create(siteLocation, siteTransform);
               }
            }

            ActiveSiteInfo.Push(newSiteInfo);
         }

         /// <summary>
         /// The active site within this scope.
         /// </summary>
         private static Stack<Tuple<IFCLocation, Transform>> ActiveSiteInfo { get; set; } =
            new Stack<Tuple<IFCLocation, Transform>>();

         /// <summary>
         /// Check if an object placement is relative to the site's placement, and fix it if necessary.
         /// </summary>
         /// <param name="productEntity">The entity being checked.</param>
         /// <param name="productStepId">The id of the entity being checked.</param>
         /// <param name="objectPlacement">The object placement handle.</param>
         public static void CheckObjectPlacementIsRelativeToSite(IFCProduct productEntity, int productStepId,
            IFCAnyHandle objectPlacement)
         {
            if (ActiveSiteInfo.Count == 0)
               return;

            Tuple<IFCLocation, Transform> currentSiteInfo = ActiveSiteInfo.Peek();
            if (currentSiteInfo == null || currentSiteInfo.Item1 == null)
               return;

            IFCLocation productEntityLocation = productEntity.ObjectLocation;
            if (productEntityLocation != null && productEntityLocation.RelativeToSite == false)
            {
               if (!(productEntity is IFCSite))
               {
                  IFCLocation activeSiteLocation = currentSiteInfo.Item1;
                  if (activeSiteLocation != null)
                  {
                     if (!IFCAnyHandleUtil.IsSubTypeOf(objectPlacement, IFCEntityType.IfcGridPlacement))
                     {
                        Importer.TheLog.LogWarning(productStepId, "The local placement (#" + objectPlacement.StepId + ") of this entity was not relative to the IfcSite's local placement, patching.", false);
                     }

                     Transform siteTransform = currentSiteInfo.Item2;
                     if (siteTransform != null)
                     {
                        Transform siteTransformInverse = siteTransform.Inverse;
                        Transform originalTotalTransform = productEntityLocation.TotalTransform;
                        if (originalTotalTransform == null)
                           productEntityLocation.RelativeTransform = siteTransformInverse;
                        else
                           productEntityLocation.RelativeTransform = originalTotalTransform.Multiply(siteTransformInverse);
                     }

                     productEntityLocation.RelativeTo = currentSiteInfo.Item1;
                  }
               }
            }
            productEntityLocation.RelativeToSite = true;
         }

         public void Dispose()
         {
            ActiveSiteInfo.Pop();
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
         // Only set the project location for the site that contains the building.
         // NOTE: The file isn't required to have an IfcBuilding, even though it generally does.
         // Furthermore, it generally only has one ifcSite.  As such, we may want to rethink
         // which the "main" IfcSite is.
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

               if (ObjectLocation != null && ObjectLocation.RelativeTransform != null)
               {
                  XYZ projectLoc = ObjectLocation.RelativeTransform.Origin;
                  if (!MathUtil.IsAlmostZero(projectLoc.Z))
                     Importer.TheLog.LogError(Id, "The Z-value of the IfcSite object placement relative transform should be 0.  This will be ignored in favor of the RefElevation value.", false);

                  // TODO: Extend this to work properly if the site relative transform
                  // isn't a simple translation.
                  if (ObjectLocation.RelativeTransform.IsTranslation)
                  {
                     XYZ offset = new XYZ(projectLoc.X, projectLoc.Y, RefElevation);
                     if (!XYZ.IsWithinLengthLimits(offset))
                     {
                        ProjectPosition projectPosition = projectLocation.GetProjectPosition(XYZ.Zero);
                        projectPosition.EastWest += projectLoc.X;
                        projectPosition.NorthSouth += projectLoc.Y;
                        projectPosition.Elevation += RefElevation;

                        projectLocation.SetProjectPosition(XYZ.Zero, projectPosition);

                        // Now that we've set the project position, remove the site relative transform, if the file is created correctly (that is, all entities contained in the site
                        // have the local placements relative to the site.
                        IFCLocation.RemoveRelativeTransformForSite(this);
                     }
                  }
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
            {
               Category category = IFCPropertySet.GetCategoryForParameterIfValid(element, Id);
               IFCPropertySet.AddParameterString(doc, element, category, parameterName, landTitleNumber, Id);
            }
         }
      }
   }
}