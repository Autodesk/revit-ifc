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

         int numSiteElements = (!IFCAnyHandleUtil.IsNullOrHasNoValue(siteHandle) ? 1 : 0);
         if (element == null && (numSiteElements != 0))
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

            IFCAnyHandle relativePlacement = null;
            double unscaledElevation = 0.0;
            if (projLocation != null)
            {
               const double scaleToDegrees = 180 / Math.PI;
               double latitudeInDeg = projLocation.GetSiteLocation().Latitude * scaleToDegrees;
               double longitudeInDeg = projLocation.GetSiteLocation().Longitude * scaleToDegrees;

               ExporterUtil.GetSafeProjectPositionElevation(doc, out unscaledElevation);

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

               ExportOptionsCache.SiteTransformBasis transformBasis = ExporterCacheManager.ExportOptionsCache.SiteTransformation;

               Transform siteSharedCoordinatesTrf = Transform.Identity;

               if (transformBasis != ExportOptionsCache.SiteTransformBasis.Internal)
               {
                  BasePoint basePoint = null;
                  if (transformBasis == ExportOptionsCache.SiteTransformBasis.Project)
                     basePoint = new FilteredElementCollector(doc).WherePasses(new ElementCategoryFilter(BuiltInCategory.OST_ProjectBasePoint)).First() as BasePoint;
                  else if (transformBasis == ExportOptionsCache.SiteTransformBasis.Site)
                     basePoint = new FilteredElementCollector(doc).WherePasses(new ElementCategoryFilter(BuiltInCategory.OST_SharedBasePoint)).First() as BasePoint;

                  if (basePoint != null)
                  {
                     BoundingBoxXYZ bbox = basePoint.get_BoundingBox(null);
                     XYZ xyz = bbox.Min;
                     siteSharedCoordinatesTrf = Transform.CreateTranslation(new XYZ(-xyz.X, -xyz.Y, unscaledElevation - xyz.Z));
                  }
                  else
                     siteSharedCoordinatesTrf = projLocation.GetTransform().Inverse;
               }

               if (!siteSharedCoordinatesTrf.IsIdentity)
               {
                  double unscaledSiteElevation = ExporterCacheManager.ExportOptionsCache.IncludeSiteElevation ? 0.0 : unscaledElevation;
                  XYZ orig = UnitUtil.ScaleLength(siteSharedCoordinatesTrf.Origin - new XYZ(0, 0, unscaledSiteElevation));
                  relativePlacement = ExporterUtil.CreateAxis2Placement3D(file, orig, siteSharedCoordinatesTrf.BasisZ, siteSharedCoordinatesTrf.BasisX);
               }
            }

            // Get elevation for site.
            double elevation = UnitUtil.ScaleLength(unscaledElevation);

            if (IFCAnyHandleUtil.IsNullOrHasNoValue(relativePlacement))
               relativePlacement = ExporterUtil.CreateAxis2Placement3D(file);

            IFCAnyHandle localPlacement = IFCInstanceExporter.CreateLocalPlacement(file, null, relativePlacement);
            IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
            string siteObjectType = NamingUtil.CreateIFCObjectName(exporterIFC, element);

            ProjectInfo projectInfo = doc.ProjectInformation;
            Element mainSiteElement = (element != null) ? element : projectInfo;

            bool exportSite = false;
            string siteGUID = null;
            string siteName = null;
            string siteLongName = null;
            string siteLandTitleNumber = null;
            string siteDescription = null;

            if (element != null)
            {
               if (IFCAnyHandleUtil.IsNullOrHasNoValue(siteHandle))
               {
                  exportSite = true;

                  // We will use the Project Information site name as the primary name, if it exists.
                  siteGUID = GUIDUtil.CreateSiteGUID(doc, element);

                  siteName = NamingUtil.GetOverrideStringValue(projectInfo, "SiteName", NamingUtil.GetNameOverride(element, NamingUtil.GetIFCName(element)));
                  siteDescription = NamingUtil.GetDescriptionOverride(element, null);

                  // Look in site element for "IfcLongName" or project information for either "IfcLongName" or "SiteLongName".
                  siteLongName = NamingUtil.GetLongNameOverride(projectInfo, NamingUtil.GetLongNameOverride(element, null));
                  if (string.IsNullOrWhiteSpace(siteLongName))
                     siteLongName = NamingUtil.GetOverrideStringValue(projectInfo, "SiteLongName", null);

                  // Look in site element for "IfcLandTitleNumber" or project information for "SiteLandTitleNumber".
                  siteLandTitleNumber = NamingUtil.GetOverrideStringValue(element, "IfcLandTitleNumber", null);
                  if (string.IsNullOrWhiteSpace(siteLandTitleNumber))
                     siteLandTitleNumber = NamingUtil.GetOverrideStringValue(projectInfo, "SiteLandTitleNumber", null);
               }
            }
            else
            {
               exportSite = true;

               siteGUID = GUIDUtil.CreateProjectLevelGUID(doc, IFCProjectLevelGUIDType.Site);
               siteName = NamingUtil.GetOverrideStringValue(projectInfo, "SiteName", "Default");
               siteLongName = NamingUtil.GetLongNameOverride(projectInfo, NamingUtil.GetOverrideStringValue(projectInfo, "SiteLongName", null));
               siteLandTitleNumber = NamingUtil.GetOverrideStringValue(projectInfo, "SiteLandTitleNumber", null);

               // don't bother if we have nothing in the site whatsoever.
               if ((latitude.Count == 0 || longitude.Count == 0) && IFCAnyHandleUtil.IsNullOrHasNoValue(relativePlacement) &&
                   string.IsNullOrWhiteSpace(siteLongName) && string.IsNullOrWhiteSpace(siteLandTitleNumber))
                  return;
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
               bool assignToBldg = false;
               bool assignToSite = false;
               IFCAnyHandle address = Exporter.CreateIFCAddress(file, doc, projectInfo, out assignToBldg, out assignToSite);
               if (!assignToSite)
                  address = null;

               siteHandle = IFCInstanceExporter.CreateSite(exporterIFC, element, siteGUID, ownerHistory, siteName, siteDescription, localPlacement,
                  siteRepresentation, siteLongName, IFCElementComposition.Element, latitude, longitude, elevation, siteLandTitleNumber, address);
               productWrapper.AddSite(mainSiteElement, siteHandle);
               ExporterCacheManager.SiteHandle = siteHandle;
            }


            tr.Commit();
         }
      }
   }
}