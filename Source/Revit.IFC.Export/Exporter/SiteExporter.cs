//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012  Autodesk, Inc.
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
using Autodesk.Revit.DB.Architecture;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export sites.
   /// </summary>
   class SiteExporter
   {
      /// <summary>
      /// Exports topography surface as IFC site object.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="topoSurface">The TopographySurface object.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportTopographySurface(ExporterIFC exporterIFC, TopographySurface topoSurface, GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         // Skip if the element is already processed and the Site has been created before
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(ExporterCacheManager.SiteHandle) && !IFCAnyHandleUtil.IsNullOrHasNoValue(ExporterCacheManager.ElementToHandleCache.Find(topoSurface.Id)))
            return;

         string ifcEnumType;
         IFCExportInfoPair exportType = ExporterUtil.GetProductExportType(exporterIFC, topoSurface, out ifcEnumType);

         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum;

         if (Enum.TryParse<Common.Enums.IFCEntityType>(exportType.ExportInstance.ToString(), out elementClassTypeEnum)
               || Enum.TryParse<Common.Enums.IFCEntityType>(exportType.ExportType.ToString(), out elementClassTypeEnum))
         {
            if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
               return;

            if (elementClassTypeEnum == Common.Enums.IFCEntityType.IfcSite)
               ExportSiteBase(exporterIFC, topoSurface.Document, topoSurface, geometryElement, productWrapper);
            else
            {
               // Export Default Site first before exporting the TopographySurface as a generic element
               ExportDefaultSite(exporterIFC, topoSurface.Document, productWrapper);
               using (ProductWrapper genElemProductWrapper = ProductWrapper.Create(exporterIFC, true))
               {
                  GenericElementExporter.ExportGenericElement(exporterIFC, topoSurface, geometryElement, genElemProductWrapper, exportType);
                  ExporterUtil.ExportRelatedProperties(exporterIFC, topoSurface, genElemProductWrapper);
               }
               productWrapper.ClearInternalHandleWrapperData(topoSurface.Document.ProjectInformation);
            }
         }
         else
            ExportSiteBase(exporterIFC, null, topoSurface, geometryElement, productWrapper);
      }

      /// <summary>
      /// Exports IFC site object if having latitude and longitude.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="document">
      /// The Revit document.
      /// </param>
      /// <param name="productWrapper">
      /// The ProductWrapper.
      /// </param>
      public static void ExportDefaultSite(ExporterIFC exporterIFC, Document document, ProductWrapper productWrapper)
      {
         ExportSiteBase(exporterIFC, document, null, null, productWrapper);
      }

      /// <summary>
      /// Base implementation to export IFC site object.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="document">The Revit document.  It may be null if element isn't.</param>
      /// <param name="element">The element.  It may be null if document isn't.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      private static void ExportSiteBase(ExporterIFC exporterIFC, Document document, Element element, GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         IFCAnyHandle siteHandle = ExporterCacheManager.SiteHandle;

         // Nothing to do if we've already created an IfcSite, and have no site element to try to
         // export or append to the existing site.
         if (element == null && !IFCAnyHandleUtil.IsNullOrHasNoValue(siteHandle))
            return;

         Document doc = document;
         if (doc == null)
         {
            if (element != null)
               doc = element.Document;
            else
               throw new ArgumentException("Both document and element are null.");
         }

         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcSite;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return;

         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction tr = new IFCTransaction(file))
         {
            IFCAnyHandle siteRepresentation = null;
            if (element != null)
            {
               // It would be possible that they actually represent several different sites with different buildings, 
               // but until we have a concept of a building in Revit, we have to assume 0-1 sites, 1 building.
               bool appendedToSite = false;
               bool exportAsFacetation = !ExporterCacheManager.ExportOptionsCache.ExportAsCoordinationView2;
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(siteHandle))
               {
                  IList<IFCAnyHandle> representations = IFCAnyHandleUtil.GetProductRepresentations(siteHandle);
                  if (representations.Count > 0)
                  {
                     IFCAnyHandle bodyRep = representations[0];
                     IFCAnyHandle boundaryRep = null;
                     if (representations.Count > 1)
                        boundaryRep = representations[1];

                     siteRepresentation = RepresentationUtil.CreateSurfaceProductDefinitionShape(exporterIFC, element, geometryElement, true, exportAsFacetation, ref bodyRep, ref boundaryRep);
                     if (representations.Count == 1 && !IFCAnyHandleUtil.IsNullOrHasNoValue(boundaryRep))
                     {
                        // If the first site has no boundaryRep,
                        // we will add the boundaryRep from second site to it.
                        representations.Clear();
                        representations.Add(boundaryRep);
                        IFCAnyHandleUtil.AddProductRepresentations(siteHandle, representations);
                     }
                     appendedToSite = true;
                  }
               }

               if (!appendedToSite)
               {
                  siteRepresentation = RepresentationUtil.CreateSurfaceProductDefinitionShape(exporterIFC, element, geometryElement, true, exportAsFacetation);
               }
            }

            List<int> latitude = new List<int>();
            List<int> longitude = new List<int>();
            ProjectLocation projLocation = doc.ActiveProjectLocation;

            double unscaledElevation = 0.0;
            if (projLocation != null)
            {
               const double scaleToDegrees = 180 / Math.PI;
               double latitudeInDeg = projLocation.GetSiteLocation().Latitude * scaleToDegrees;
               double longitudeInDeg = projLocation.GetSiteLocation().Longitude * scaleToDegrees;

               //ExporterUtil.GetSafeProjectPositionElevation(doc, out unscaledElevation);
               SiteTransformBasis wcsBasis = ExporterCacheManager.ExportOptionsCache.SiteTransformation;
               (double eastings, double northings, double orthogonalHeight) geoRefInfo = OptionsUtil.GeoReferenceInformation(doc, wcsBasis);
               unscaledElevation = geoRefInfo.orthogonalHeight;

               int latDeg = ((int)latitudeInDeg); latitudeInDeg -= latDeg; latitudeInDeg *= 60;
               int latMin = ((int)latitudeInDeg); latitudeInDeg -= latMin; latitudeInDeg *= 60;
               int latSec = ((int)latitudeInDeg); latitudeInDeg -= latSec; latitudeInDeg *= 1000000;
               int latFracSec = ((int)latitudeInDeg);
               latitude.Add(latDeg);
               latitude.Add(latMin);
               latitude.Add(latSec);
               if (!ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
                  latitude.Add(latFracSec);

               int longDeg = ((int)longitudeInDeg); longitudeInDeg -= longDeg; longitudeInDeg *= 60;
               int longMin = ((int)longitudeInDeg); longitudeInDeg -= longMin; longitudeInDeg *= 60;
               int longSec = ((int)longitudeInDeg); longitudeInDeg -= longSec; longitudeInDeg *= 1000000;
               int longFracSec = ((int)longitudeInDeg);
               longitude.Add(longDeg);
               longitude.Add(longMin);
               longitude.Add(longSec);
               if (!ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
                  longitude.Add(longFracSec);
            }

            // Get elevation for site.
            IFCAnyHandle relativePlacement = null;
            IFCAnyHandle localPlacement = null;
            if (ExporterCacheManager.ExportOptionsCache.ExportingLink)
            {
               relativePlacement = ExporterUtil.CreateAxis2Placement3D(file, UnitUtil.ScaleLength(ExporterCacheManager.HostRvtFileWCS.Origin), ExporterCacheManager.HostRvtFileWCS.BasisZ, ExporterCacheManager.HostRvtFileWCS.BasisX);
               localPlacement = IFCInstanceExporter.CreateLocalPlacement(file, null, relativePlacement);
            }
            else
            {
               if (ExporterCacheManager.ExportOptionsCache.IncludeSiteElevation)
                  unscaledElevation = 0.0;
               Transform wcs = GeometryUtil.GetWCS(doc, unscaledElevation);
               if (wcs != null && !wcs.IsIdentity)
               {
                  relativePlacement = ExporterUtil.CreateAxis2Placement3D(file, wcs.Origin, wcs.BasisZ, wcs.BasisX);
                  localPlacement = IFCInstanceExporter.CreateLocalPlacement(file, null, relativePlacement);
                  ExporterCacheManager.HostRvtFileWCS = wcs;
                  ExporterCacheManager.HostRvtFileWCS.Origin = UnitUtil.UnscaleLength(ExporterCacheManager.HostRvtFileWCS.Origin);
               }
               else
                  ExporterCacheManager.HostRvtFileWCS = Transform.Identity;
            }

            if (IFCAnyHandleUtil.IsNullOrHasNoValue(relativePlacement))
               relativePlacement = ExporterUtil.CreateAxis2Placement3D(file);

            if (IFCAnyHandleUtil.IsNullOrHasNoValue(localPlacement))
               localPlacement = IFCInstanceExporter.CreateLocalPlacement(file, null, relativePlacement);

            IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
            string siteObjectType = null;

            ProjectInfo projectInfo = doc.ProjectInformation;
            Element mainSiteElement = (element != null) ? element : projectInfo;

            string siteGUID = null;
            string siteName = null;
            string siteLongName = null;
            string siteLandTitleNumber = null;
            string siteDescription = null;
            bool exportSite = false;

            if ((element != null && IFCAnyHandleUtil.IsNullOrHasNoValue(siteHandle)) || (element == null))
            {
               exportSite = true;

               // We will use the Project Information site name as the primary name, if it exists.
               siteGUID = (element != null) ? GUIDUtil.CreateSiteGUID(doc, element) : GUIDUtil.CreateProjectLevelGUID(doc, GUIDUtil.ProjectLevelGUIDType.Site); ;

               if (element != null)
               {
                  siteName = NamingUtil.GetNameOverride(element, NamingUtil.GetIFCName(element));
                  siteDescription = NamingUtil.GetDescriptionOverride(element, null);
                  siteObjectType = NamingUtil.GetObjectTypeOverride(element, null);
                  siteLongName = NamingUtil.GetLongNameOverride(element, null);
                  siteLandTitleNumber = NamingUtil.GetOverrideStringValue(element, "IfcLandTitleNumber", null);
               }
               else
               {
                  siteName = "Default";
               }

               siteName = NamingUtil.GetOverrideStringValue(projectInfo, "SiteName", siteName);
               siteDescription = NamingUtil.GetOverrideStringValue(projectInfo, "SiteDescription", siteDescription);
               siteObjectType = NamingUtil.GetOverrideStringValue(projectInfo, "SiteObjectType", siteObjectType);
               siteLongName = NamingUtil.GetOverrideStringValue(projectInfo, "SiteLongName", siteLongName);
               siteLandTitleNumber = NamingUtil.GetOverrideStringValue(projectInfo, "SiteLandTitleNumber", siteLandTitleNumber);

               if (element == null)
               {
                  // don't bother exporting if we have nothing in the site whatsoever, and it is virtual.
                  if ((latitude.Count == 0 || longitude.Count == 0) &&
                     IFCAnyHandleUtil.IsNullOrHasNoValue(relativePlacement) &&
                     string.IsNullOrWhiteSpace(siteLongName) &&
                     string.IsNullOrWhiteSpace(siteLandTitleNumber))
                  {
                     return;
                  }
               }
            }

            COBieProjectInfo cobieProjectInfo = ExporterCacheManager.ExportOptionsCache.COBieProjectInfo;
            // Override Site information when it is a special COBie export
            if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3COBIE24DesignDeliverable && cobieProjectInfo != null)
            {
               siteName = cobieProjectInfo.SiteLocation;
               siteDescription = cobieProjectInfo.SiteDescription;
            }

            if (exportSite)
            {
               IFCAnyHandle address = null;
               if (Exporter.NeedToCreateAddressForSite(doc))
                  address = Exporter.CreateIFCAddress(file, doc, projectInfo);

               double elevation = UnitUtil.ScaleLength(unscaledElevation);

               siteHandle = IFCInstanceExporter.CreateSite(exporterIFC, element, siteGUID, ownerHistory, siteName, siteDescription, siteObjectType, localPlacement,
                  siteRepresentation, siteLongName, IFCElementComposition.Element, latitude, longitude, elevation, siteLandTitleNumber, address);
               productWrapper.AddSite(mainSiteElement, siteHandle);
               ExporterCacheManager.SiteHandle = siteHandle;
            }


            tr.Commit();
         }
      }
   }
}