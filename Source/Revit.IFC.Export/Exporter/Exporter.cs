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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Text;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Utility;
using Revit.IFC.Common.Extensions;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Autodesk.Revit.DB.ExternalService;
using Revit.IFC.Export.Properties;
using System.Reflection;
using Autodesk.Revit.DB.Steel;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// This class implements the methods of interface IExternalDBApplication to register the IFC export client to Autodesk Revit.
   /// </summary>
   class ExporterApplication : IExternalDBApplication
   {
      #region IExternalDBApplication Members

      /// <summary>
      /// The method called when Autodesk Revit exits.
      /// </summary>
      /// <param name="application">Controlled application to be shutdown.</param>
      /// <returns>Return the status of the external application.</returns>
      public ExternalDBApplicationResult OnShutdown(Autodesk.Revit.ApplicationServices.ControlledApplication application)
      {
         return ExternalDBApplicationResult.Succeeded;
      }

      /// <summary>
      /// The method called when Autodesk Revit starts.
      /// </summary>
      /// <param name="application">Controlled application to be loaded to Autodesk Revit process.</param>
      /// <returns>Return the status of the external application.</returns>
      public ExternalDBApplicationResult OnStartup(Autodesk.Revit.ApplicationServices.ControlledApplication application)
      {
         // As an ExternalServer, the exporter cannot be registered until full application initialization. Setup an event callback to do this
         // at the appropriate time.
         application.ApplicationInitialized += OnApplicationInitialized;
         return ExternalDBApplicationResult.Succeeded;
      }

      #endregion

      /// <summary>
      /// The action taken on application initialization.
      /// </summary>
      /// <param name="sender">The sender.</param>
      /// <param name="eventArgs">The event args.</param>
      private void OnApplicationInitialized(object sender, EventArgs eventArgs)
      {
         SingleServerService service = ExternalServiceRegistry.GetService(ExternalServices.BuiltInExternalServices.IFCExporterService) as SingleServerService;
         if (service != null)
         {
            Exporter exporter = new Exporter();
            service.AddServer(exporter);
            service.SetActiveServer(exporter.GetServerId());
         }
         // TODO log this failure accordingly
      }
   }

   /// <summary>
   /// This class implements the method of interface IExporterIFC to perform an export to IFC.
   /// </summary>
   public class Exporter : IExporterIFC
   {
      RevitStatusBar statusBar = null;

      // Used for debugging tool "WriteIFCExportedElements"
      private StreamWriter m_Writer;

      private IFCFile m_IfcFile;

      // Allow a derived class to add Element exporter routines.
      public delegate void ElementExporter(ExporterIFC exporterIFC, Autodesk.Revit.DB.Document document);

      protected ElementExporter m_ElementExporter = null;

      // Allow a derived class to add property sets.
      public delegate void PropertySetsToExport(IList<IList<PropertySetDescription>> propertySets);

      protected PropertySetsToExport m_PropertySetsToExport = null;

      // Allow a derived class to add quantities.
      public delegate void QuantitiesToExport(IList<IList<QuantityDescription>> propertySets);

      protected QuantitiesToExport m_QuantitiesToExport = null;

      #region IExporterIFC Members

      /// <summary>
      /// Create the list of element export routines.  Each routine will export a subset of Revit elements,
      /// allowing for a choice of which elements are exported, and in what order.
      /// This routine is protected, so it could be overriden by an Exporter class that inherits from this base class.
      /// </summary>
      protected virtual void InitializeElementExporters()
      {
         // Allow another function to potentially add exporters before ExportSpatialElements.
         if (m_ElementExporter == null)
            m_ElementExporter = ExportSpatialElements;
         else
            m_ElementExporter += ExportSpatialElements;
         m_ElementExporter += ExportNonSpatialElements;
         m_ElementExporter += ExportContainers;
         m_ElementExporter += ExportGrids;
         m_ElementExporter += ExportConnectors;
         // export AdvanceSteel elements
         m_ElementExporter += ExportAdvanceSteelElements;
      }

      /// <summary>
      /// Implements the method that Autodesk Revit will invoke to perform an export to IFC.
      /// </summary>
      /// <param name="document">The document to export.</param>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      /// <param name="filterView">The view whose filter visibility settings govern the export.</param>
      /// <remarks>Note that filterView doesn't control the exported geometry; it only controls which elements
      /// are visible or not. That allows us to, e.g., choose a plan view but get 3D geometry.</remarks>
      public void ExportIFC(Autodesk.Revit.DB.Document document, ExporterIFC exporterIFC, Autodesk.Revit.DB.View filterView)
      {
         // Make sure our static caches are clear at the start, and end, of export.
         ExporterCacheManager.Clear();
         ExporterStateManager.Clear();

         try
         {
            BeginExport(exporterIFC, document, filterView);

            ParamExprListener.ResetParamExprInternalDicts();
            InitializeElementExporters();
            if (m_ElementExporter != null)
               m_ElementExporter(exporterIFC, document);

            EndExport(exporterIFC, document);
            WriteIFCFile(exporterIFC, document);
         }
         catch (Exception ex)
         {
            // This doesn't always work, because we don't always reach the maximum size in the same way.
            // The default message is better than no message as a backup.
            FailureDefinitionId ifcError =
               (ex.Message == "IFC: EDM Toolkit Error: Max model data size is reached.") ?
               BuiltInFailures.ExportFailures.IFCFatalToolkitExportError :
               BuiltInFailures.ExportFailures.IFCFatalExportError;
            FailureMessage fm = new FailureMessage(ifcError);
            document.PostFailure(fm);
         }
         finally
         {

            ExporterCacheManager.Clear();
            ExporterStateManager.Clear();

            DelegateClear();

            if (m_Writer != null)
               m_Writer.Close();

            if (m_IfcFile != null)
            {
               m_IfcFile.Close();
               m_IfcFile = null;
            }
         }
      }

      public virtual string GetDescription()
      {
         return "IFC open source exporter";
      }

      public virtual string GetName()
      {
         return "IFC exporter";
      }

      public virtual Guid GetServerId()
      {
         return new Guid("BBE27F6B-E887-4F68-9152-1E664DAD29C3");
      }

      public virtual string GetVendorId()
      {
         return "IFCX";
      }

      // This is not virtual, and should not be overriden.
      public Autodesk.Revit.DB.ExternalService.ExternalServiceId GetServiceId()
      {
         return Autodesk.Revit.DB.ExternalService.ExternalServices.BuiltInExternalServices.IFCExporterService;
      }

      #endregion

      /// <summary>
      /// Exports the AdvanceSteel specific elements
      /// </summary>
      /// <param name="exporterIFC">The exporterIFC class.</param>
      /// <param name="document">The Revit document.</param>
      protected void ExportAdvanceSteelElements(ExporterIFC exporterIFC, Autodesk.Revit.DB.Document document)
      {
         // verify if Steel elements should be exported
         if (ExporterCacheManager.ExportOptionsCache.IncludeSteelElements)
         {
            try
            {
#if IFC_OPENSOURCE
               string dllPath = Assembly.GetExecutingAssembly().Location;
               Assembly assembly = Assembly.LoadFrom(Path.GetDirectoryName(dllPath) + @"\Autodesk.SteelConnections.ASIFC.dll");
#else
               Assembly assembly = Assembly.LoadFrom(AppDomain.CurrentDomain.BaseDirectory + @"\Addins\SteelConnections\Autodesk.SteelConnections.ASIFC.dll");
#endif
               if (assembly != null)
               {
                  Type type = assembly.GetType("Autodesk.SteelConnections.ASIFC.ASExporter");
                  if (type != null)
                  {
                     MethodInfo method = type.GetMethod("ExportASElements");
                     if (method != null)
                        method.Invoke(null, new object[] { exporterIFC, document });
                  }
               }
            }
            catch
            { }
         }
      }

      /// <summary>
      /// Create the based export element collector used for filtering elements
      /// </summary>
      /// <param name="document">The document.</param>
      /// <param name="useFilterViewIfExists">If false, don't use the filter view
      /// even if it exists.</param>
      /// <returns>The FilteredElementCollector.</returns>
      /// <remarks>useFilterViewIfExists is intended to be false for cases
      /// where we want to potentially export some invisible elements, such
      /// as rooms in 3D views.</remarks>
      private FilteredElementCollector GetExportElementCollector(
         Autodesk.Revit.DB.Document document, bool useFilterViewIfExists)
      {
         ExportOptionsCache exportOptionsCache = ExporterCacheManager.ExportOptionsCache;
         ICollection<ElementId> idsToExport = exportOptionsCache.ElementsForExport;
         if (idsToExport.Count > 0)
         {
            return new FilteredElementCollector(document, idsToExport);
         }

         View filterView = useFilterViewIfExists ?
            exportOptionsCache.FilterViewForExport : null;
         return (filterView == null || exportOptionsCache.ExportingLink) ?
                new FilteredElementCollector(document) :
                new FilteredElementCollector(filterView.Document, filterView.Id);
      }

      /// <summary>
      /// Checks if a spatial element is contained inside a section box, if the box exists.
      /// </summary>
      /// <param name="sectionBox">The section box.</param>
      /// <param name="element">The element.</param>
      /// <returns>False if there is a section box and the element can be determined to not be inside it.</returns>
      private bool SpatialElementInSectionBox(BoundingBoxXYZ sectionBox, Element element)
      {
         if (sectionBox == null)
            return true;

         BoundingBoxXYZ elementBBox = element.get_BoundingBox(null);
         if (elementBBox == null)
         {
            // Areas don't have bounding box geometry.  For these, try their location point.
            LocationPoint locationPoint = element.Location as LocationPoint;
            if (locationPoint == null)
               return false;
          
            elementBBox = new BoundingBoxXYZ();
            elementBBox.set_Bounds(0, locationPoint.Point);
            elementBBox.set_Bounds(1, locationPoint.Point);
         }

         return GeometryUtil.BoundingBoxesOverlap(elementBBox, sectionBox);
      }

      protected void ExportSpatialElements(ExporterIFC exporterIFC, Autodesk.Revit.DB.Document document)
      {
         // Create IfcSite first here using the first visible TopographySurface if any, if not create a default one.
         // Site and Building need to be created first to ensure containment override to work
         FilteredElementCollector topoElementCollector = GetExportElementCollector(document, true);
         List<Type> topoSurfaceType = new List<Type>() { typeof(TopographySurface) };
         ElementMulticlassFilter multiclassFilter = new ElementMulticlassFilter(topoSurfaceType);
         topoElementCollector.WherePasses(multiclassFilter);
         ICollection<ElementId> filteredTopoElements = topoElementCollector.ToElementIds();
         if (filteredTopoElements != null && filteredTopoElements.Count > 0)
         {
            foreach (ElementId topoElemId in filteredTopoElements)
            {
               // Note that the TopographySurface exporter in ExportElementImpl does nothing if the
               // element has already been processed here.
               Element topoElem = document.GetElement(topoElemId);
               if (ExportElement(exporterIFC, topoElem))
                  break; // Process only the first exportable one to create the IfcSite
            }
         }

         if (ExporterCacheManager.SiteHandle == null || IFCAnyHandleUtil.IsNullOrHasNoValue(ExporterCacheManager.SiteHandle))
         {
            using (ProductWrapper productWrapper = ProductWrapper.Create(exporterIFC, true))
            {
               SiteExporter.ExportDefaultSite(exporterIFC, document, productWrapper);
               ExporterUtil.ExportRelatedProperties(exporterIFC, document.ProjectInformation, productWrapper);
            }
         }

         // Create IfcBuilding first here
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ExporterCacheManager.BuildingHandle) && IFCAnyHandleUtil.IsNullOrHasNoValue(ExporterCacheManager.SiteHandle))
         {
            IFCAnyHandle buildingPlacement = CreateBuildingPlacement(exporterIFC.GetFile());
            IFCAnyHandle buildingHnd = CreateBuildingFromProjectInfo(exporterIFC, document, buildingPlacement);
            ExporterCacheManager.BuildingHandle = buildingHnd;
         }

         ExportOptionsCache exportOptionsCache = ExporterCacheManager.ExportOptionsCache;
         View filterView = exportOptionsCache.FilterViewForExport;

         bool exportIfBoundingBoxIsWithinViewExtent = (exportOptionsCache.ExportRoomsInView && (filterView != null) && filterView is View3D);
         // We don't want to use the filter view for exporting spaces if exportOptionsCache.ExportRoomsInView
         // is true and we have a 3D view.
         bool useFilterViewInCollector = !exportIfBoundingBoxIsWithinViewExtent;

         ISet<ElementId> exportedSpaces = null;
         if (exportOptionsCache.SpaceBoundaryLevel == 2)
            exportedSpaces = SpatialElementExporter.ExportSpatialElement2ndLevel(this, exporterIFC, document);

         // Export all spatial elements for no or 1st level room boundaries; for 2nd level, export spaces that 
         // couldn't be exported above.
         // Note that FilteredElementCollector is one use only, so we need to create a new one here.
         FilteredElementCollector spatialElementCollector = GetExportElementCollector(document, useFilterViewInCollector);
         SpatialElementExporter.InitializeSpatialElementGeometryCalculator(document, exporterIFC);
         ElementFilter spatialElementFilter = ElementFilteringUtil.GetSpatialElementFilter(document, exporterIFC);
         spatialElementCollector.WherePasses(spatialElementFilter);

         // if the view is 3D and section box is active, then set the section box
         BoundingBoxXYZ sectionBox = null;
         if (exportIfBoundingBoxIsWithinViewExtent)
         {
            View3D currentView = filterView as View3D;
            sectionBox = currentView != null && currentView.IsSectionBoxActive ? currentView.GetSectionBox() : null;
         }

         int numOfSpatialElements = spatialElementCollector.Count<Element>();
         int spatialElementCount = 1;

         foreach (Element element in spatialElementCollector)
         {
            statusBar.Set(String.Format(Resources.IFCProcessingSpatialElements, spatialElementCount, numOfSpatialElements, element.Id));
            spatialElementCount++;

            if ((element == null) || (exportedSpaces != null && exportedSpaces.Contains(element.Id)))
               continue;
            if (ElementFilteringUtil.IsRoomInInvalidPhase(element))
               continue;
            // If the element's bounding box doesn't intersect the section box then ignore it.
            // If the section box isn't active, then we export the element.
            if (!SpatialElementInSectionBox(sectionBox, element))
               continue;
            ExportElement(exporterIFC, element);
         }

         SpatialElementExporter.DestroySpatialElementGeometryCalculator();
      }

      protected void ExportNonSpatialElements(ExporterIFC exporterIFC, Autodesk.Revit.DB.Document document)
      {
         FilteredElementCollector otherElementCollector = GetExportElementCollector(document, true);

         ElementFilter nonSpatialElementFilter = ElementFilteringUtil.GetNonSpatialElementFilter(document, exporterIFC);
         otherElementCollector.WherePasses(nonSpatialElementFilter);

         int numOfOtherElement = otherElementCollector.Count();
         IList<Element> otherElementCollListCopy = new List<Element>(otherElementCollector);
         int otherElementCollectorCount = 1;
         foreach (Element element in otherElementCollListCopy)
         {
            statusBar.Set(String.Format(Resources.IFCProcessingNonSpatialElements, otherElementCollectorCount, numOfOtherElement, element.Id));
            otherElementCollectorCount++;
            ExportElement(exporterIFC, element);
         }
      }

      /// <summary>
      /// Export various containers that depend on individual element export.
      /// </summary>
      /// <param name="document">The Revit document.</param>
      /// <param name="exporterIFC">The exporterIFC class.</param>
      protected void ExportContainers(ExporterIFC exporterIFC, Autodesk.Revit.DB.Document document)
      {
         using (ExporterStateManager.ForceElementExport forceElementExport = new ExporterStateManager.ForceElementExport())
         {
            ExportCachedRailings(exporterIFC, document);
            ExportCachedFabricAreas(exporterIFC, document);
            ExportTrusses(exporterIFC, document);
            ExportBeamSystems(exporterIFC, document);
            ExportAreaSchemes(exporterIFC, document);
            ExportZones(exporterIFC, document);
         }
      }

      /// <summary>
      /// Export railings cached during spatial element export.  
      /// Railings are exported last as their containment is not known until all stairs have been exported.
      /// This is a very simple sorting, and further containment issues could require a more robust solution in the future.
      /// </summary>
      /// <param name="document">The Revit document.</param>
      /// <param name="exporterIFC">The exporterIFC class.</param>
      protected void ExportCachedRailings(ExporterIFC exporterIFC, Autodesk.Revit.DB.Document document)
      {
         HashSet<ElementId> railingCollection = ExporterCacheManager.RailingCache;
         int railingIndex = 1;
         int railingCollectionCount = railingCollection.Count;
         foreach (ElementId elementId in ExporterCacheManager.RailingCache)
         {
            statusBar.Set(String.Format(Resources.IFCProcessingRailings, railingIndex, railingCollectionCount, elementId));
            railingIndex++;
            Element element = document.GetElement(elementId);
            ExportElement(exporterIFC, element);
         }
      }

      /// <summary>
      /// Export FabricAreas cached during non-spatial element export.  
      /// We export whatever FabricAreas actually have handles as IfcGroup.
      /// </summary>
      /// <param name="document">The Revit document.</param>
      /// <param name="exporterIFC">The exporterIFC class.</param>
      protected void ExportCachedFabricAreas(ExporterIFC exporterIFC, Autodesk.Revit.DB.Document document)
      {
         IDictionary<ElementId, HashSet<IFCAnyHandle>> fabricAreaCollection = ExporterCacheManager.FabricAreaHandleCache;
         int fabricAreaIndex = 1;
         int fabricAreaCollectionCount = fabricAreaCollection.Count;
         foreach (ElementId elementId in ExporterCacheManager.FabricAreaHandleCache.Keys)
         {
            statusBar.Set(String.Format(Resources.IFCProcessingFabricAreas, fabricAreaIndex, fabricAreaCollectionCount, elementId));
            fabricAreaIndex++;
            Element element = document.GetElement(elementId);
            ExportElement(exporterIFC, element);
         }
      }

      /// <summary>
      /// Export Trusses.  These could be in assemblies, so do before assembly export, but after beams and members are exported.
      /// </summary>
      /// <param name="document">The Revit document.</param>
      /// <param name="exporterIFC">The exporterIFC class.</param>
      protected void ExportTrusses(ExporterIFC exporterIFC, Autodesk.Revit.DB.Document document)
      {
         HashSet<ElementId> trussCollection = ExporterCacheManager.TrussCache;
         int trussIndex = 1;
         int trussCollectionCount = trussCollection.Count;
         foreach (ElementId elementId in ExporterCacheManager.TrussCache)
         {
            statusBar.Set(String.Format(Resources.IFCProcessingTrusses, trussIndex, trussCollectionCount, elementId));
            trussIndex++;
            Element element = document.GetElement(elementId);
            ExportElement(exporterIFC, element);
         }
      }

      /// <summary>
      /// Export BeamSystems.  These could be in assemblies, so do before assembly export, but after beams are exported.
      /// </summary>
      /// <param name="document">The Revit document.</param>
      /// <param name="exporterIFC">The exporterIFC class.</param>
      protected void ExportBeamSystems(ExporterIFC exporterIFC, Autodesk.Revit.DB.Document document)
      {
         HashSet<ElementId> beamSystemCollection = ExporterCacheManager.BeamSystemCache;
         int beamSystemIndex = 1;
         int beamSystemCollectionCount = beamSystemCollection.Count;
         foreach (ElementId elementId in ExporterCacheManager.BeamSystemCache)
         {
            statusBar.Set(String.Format(Resources.IFCProcessingBeamSystems, beamSystemIndex, beamSystemCollectionCount, elementId));
            beamSystemIndex++;
            Element element = document.GetElement(elementId);
            ExportElement(exporterIFC, element);
         }
      }

      /// <summary>
      /// Export Zones.
      /// </summary>
      /// <param name="document">The Revit document.</param>
      /// <param name="exporterIFC">The exporterIFC class.</param>
      protected void ExportZones(ExporterIFC exporterIFC, Autodesk.Revit.DB.Document document)
      {
         HashSet<ElementId> zoneCollection = ExporterCacheManager.ZoneCache;
         int zoneIndex = 1;
         int zoneCollectionCount = zoneCollection.Count;
         foreach (ElementId elementId in ExporterCacheManager.ZoneCache)
         {
            statusBar.Set(String.Format(Resources.IFCProcessingExportZones, zoneIndex, zoneCollectionCount, elementId));
            zoneIndex++;
            Element element = document.GetElement(elementId);
            ExportElement(exporterIFC, element);
         }
      }

      /// <summary>
      /// Export Area Schemes.
      /// </summary>
      /// <param name="document">The Revit document.</param>
      /// <param name="exporterIFC">The exporterIFC class.</param>
      protected void ExportAreaSchemes(ExporterIFC exporterIFC, Autodesk.Revit.DB.Document document)
      {
         foreach (ElementId elementId in ExporterCacheManager.AreaSchemeCache.Keys)
         {
            Element element = document.GetElement(elementId);
            ExportElement(exporterIFC, element);
         }
      }

      protected void ExportGrids(ExporterIFC exporterIFC, Autodesk.Revit.DB.Document document)
      {
         // Export the grids
         GridExporter.Export(exporterIFC, document);
      }

      protected void ExportConnectors(ExporterIFC exporterIFC, Autodesk.Revit.DB.Document document)
      {
         ConnectorExporter.Export(exporterIFC);
      }

      /// <summary>
      /// Determines if the selected element meets extra criteria for export.
      /// </summary>
      /// <param name="exporterIFC">The exporter class.</param>
      /// <param name="element">The current element to export.</param>
      /// <returns>True if the element should be exported.</returns>
      protected virtual bool CanExportElement(ExporterIFC exporterIFC, Autodesk.Revit.DB.Element element)
      {
         // Skip the export of AdvanceSteel elements, they will be exported by ExportASElements
         if (ExporterCacheManager.ExportOptionsCache.IncludeSteelElements)
         {
            // In case Autodesk.Revit.DB.Steel is missing, continue the export.
            try
            {
               SteelElementProperties cell = SteelElementProperties.GetSteelElementProperties(element);
               if (cell != null)
               {
                  bool hasGraphics = false;
                  PropertyInfo graphicsCell = cell.GetType().GetProperty("HasGraphics", BindingFlags.Instance | BindingFlags.NonPublic);
                  if (graphicsCell != null) // Concrete elements with cell that have HasGraphics set to true, must be handled by Revit exporter.
                     hasGraphics = (bool)graphicsCell.GetValue(cell, null);

                  if (hasGraphics)
                     return false;
               }
            }
            catch
            { }
         }

         return ElementFilteringUtil.CanExportElement(exporterIFC, element, false);
      }

      /// <summary>
      /// Performs the export of elements, including spatial and non-spatial elements.
      /// </summary>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      /// <param name="element">The element to export.</param>
      /// <returns>False if the element can't be exported at all, true otherwise.</returns>
      /// <remarks>A true return value doesn't mean something was exported, but that the
      /// routine did a quick reject on the element, or an exception occurred.</remarks>
      public virtual bool ExportElement(ExporterIFC exporterIFC, Autodesk.Revit.DB.Element element)
      {
         if (!CanExportElement(exporterIFC, element))
         {
            if (element is RevitLinkInstance && !ExporterCacheManager.ExportOptionsCache.ExportingLink)
            {
               IDictionary<String, String> options = exporterIFC.GetOptions();
               bool? bExportLinks = OptionsUtil.GetNamedBooleanOption(options, "ExportLinkedFiles");
               if (bExportLinks.HasValue && bExportLinks.Value == true)
               {
                  bool bStoreIFCGUID = ExporterCacheManager.ExportOptionsCache.GUIDOptions.StoreIFCGUID;
                  ExporterCacheManager.ExportOptionsCache.GUIDOptions.StoreIFCGUID = true;
                  GUIDUtil.CreateGUID(element);
                  ExporterCacheManager.ExportOptionsCache.GUIDOptions.StoreIFCGUID = bStoreIFCGUID;
               }
            }
            return false;
         }

         //WriteIFCExportedElements
         if (m_Writer != null)
         {
            Category category = element.Category;
            m_Writer.WriteLine(String.Format("{0},{1},{2}", element.Id, category == null ? "null" : category.Name, element.GetType().Name));
         }

         try
         {
            using (ProductWrapper productWrapper = ProductWrapper.Create(exporterIFC, true))
            {
               ExportElementImpl(exporterIFC, element, productWrapper);
               ExporterUtil.ExportRelatedProperties(exporterIFC, element, productWrapper);
            }

            // We are going to clear the parameter cache for the element (not the type) after the export.
            // We do not expect to need the parameters for this element again, so we can free up the space.
            if (!(element is ElementType) && !ExporterStateManager.ShouldPreserveElementParameterCache(element))
               ParameterUtil.RemoveElementFromCache(element);
         }
         catch (System.Exception ex)
         {
            HandleUnexpectedException(ex, exporterIFC, element);
            return false;
         }

         return true;
      }

      /// <summary>
      /// Handles the unexpected Exception.
      /// </summary>
      /// <param name="ex">The unexpected exception.</param>
      /// <param name="element ">The element got the exception.</param>
      internal void HandleUnexpectedException(Exception exception, ExporterIFC exporterIFC, Element element)
      {
         Document document = element.Document;
         string errMsg = String.Format("IFC error: Exporting element \"{0}\",{1} - {2}", element.Name, element.Id, exception.ToString());
         element.Document.Application.WriteJournalComment(errMsg, true);

         if (!ExporterUtil.IsFatalException(document, exception))
         {
            FailureMessage fm = new FailureMessage(BuiltInFailures.ExportFailures.IFCGenericExportWarning);
            fm.SetFailingElement(element.Id);
            document.PostFailure(fm);
         }
         else
         {
            // This exception should be rethrown back to the main Revit application.
            throw exception;
         }
      }

      /// <summary>
      /// Checks if the element is MEP type.
      /// </summary>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      /// <param name="element">The element to check.</param>
      /// <returns>True for MEP type of elements.</returns>
      private bool IsMEPType(ExporterIFC exporterIFC, Element element, IFCExportInfoPair exportType)
      {
         return (ElementFilteringUtil.IsMEPType(exportType) || ElementFilteringUtil.ProxyForMEPType(element, exportType));
      }

      /// <summary>
      /// Checks if exporting an element as building elment proxy.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <returns>True for exporting as proxy element.</returns>
      private bool ExportAsProxy(Element element, IFCExportInfoPair exportType)
      {
         // FaceWall should be exported as IfcWall.
         return ((element is FaceWall) || (element is ModelText) || (exportType.ExportInstance == IFCEntityType.IfcBuildingElementProxy) || (exportType.ExportType == IFCEntityType.IfcBuildingElementProxyType));
      }

      /// <summary>
      /// Checks if exporting an element of Stairs category.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <returns>True if element is of category OST_Stairs.</returns>
      private bool IsStairs(Element element)
      {
         return (CategoryUtil.GetSafeCategoryId(element) == new ElementId(BuiltInCategory.OST_Stairs));
      }

      /// <summary>
      /// Checks if the element is one of the types that contain structural rebar.
      /// </summary>
      /// <param name="element"></param>
      /// <returns></returns>
      private bool IsRebarType(Element element)
      {
         return (element is AreaReinforcement || element is PathReinforcement || element is Rebar || element is RebarContainer);
      }

      /// <summary>
      /// Implements the export of element.
      /// </summary>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      /// <param name="element">The element to export.</param>
      /// <param name="productWrapper">The ProductWrapper object.</param>
      public virtual void ExportElementImpl(ExporterIFC exporterIFC, Element element, ProductWrapper productWrapper)
      {
         Options options;
         View ownerView = null;

         ownerView = element.Document.GetElement(element.OwnerViewId) as View;

         if (ExporterCacheManager.ExportOptionsCache.UseActiveViewGeometry)
         {
            ownerView = ExporterCacheManager.ExportOptionsCache.ActiveView;
         }
         else
         {
            ownerView = element.Document.GetElement(element.OwnerViewId) as View;
         }

         if (ownerView == null)
         {
            options = GeometryUtil.GetIFCExportGeometryOptions();
         }
         else
         {
            options = new Options();
            options.View = ownerView;
         }
         GeometryElement geomElem = element.get_Geometry(options);

         // Default: we don't preserve the element parameter cache after export.
         bool shouldPreserveParameterCache = false;

         try
         {
            exporterIFC.PushExportState(element, geomElem);

            Autodesk.Revit.DB.Document doc = element.Document;
            using (SubTransaction st = new SubTransaction(doc))
            {
               st.Start();

               // A long list of supported elements.  Please keep in alphabetical order by the first item in the list..
               if (element is AreaScheme)
               {
                  AreaSchemeExporter.ExportAreaScheme(exporterIFC, element as AreaScheme, productWrapper);
               }
               else if (element is AssemblyInstance)
               {
                  AssemblyInstance assemblyInstance = element as AssemblyInstance;
                  AssemblyInstanceExporter.ExportAssemblyInstanceElement(exporterIFC, assemblyInstance, productWrapper);
               }
               else if (element is BeamSystem)
               {
                  if (ExporterCacheManager.BeamSystemCache.Contains(element.Id))
                     AssemblyInstanceExporter.ExportBeamSystem(exporterIFC, element as BeamSystem, productWrapper);
                  else
                  {
                     ExporterCacheManager.BeamSystemCache.Add(element.Id);
                     shouldPreserveParameterCache = true;
                  }
               }
               else if (element is Ceiling)
               {
                  Ceiling ceiling = element as Ceiling;
                  CeilingExporter.ExportCeilingElement(exporterIFC, ceiling, ref geomElem, productWrapper);
               }
               else if (element is CeilingAndFloor || element is Floor)
               {
                  // This covers both Floors and Building Pads.
                  CeilingAndFloor hostObject = element as CeilingAndFloor;
                  FloorExporter.ExportCeilingAndFloorElement(exporterIFC, hostObject, ref geomElem, productWrapper);
               }
               else if (element is WallFoundation)
               {
                  WallFoundation footing = element as WallFoundation;
                  FootingExporter.ExportFootingElement(exporterIFC, footing, geomElem, productWrapper);
               }
               else if (element is CurveElement)
               {
                  CurveElement curveElem = element as CurveElement;
                  CurveElementExporter.ExportCurveElement(exporterIFC, curveElem, geomElem, productWrapper);
               }
               else if (element is CurtainSystem)
               {
                  CurtainSystem curtainSystem = element as CurtainSystem;
                  CurtainSystemExporter.ExportCurtainSystem(exporterIFC, curtainSystem, productWrapper);
               }
               else if (CurtainSystemExporter.IsLegacyCurtainElement(element))
               {
                  CurtainSystemExporter.ExportLegacyCurtainElement(exporterIFC, element, productWrapper);
               }
               else if (element is DuctInsulation)
               {
                  DuctInsulation ductInsulation = element as DuctInsulation;
                  DuctInsulationExporter.ExportDuctInsulation(exporterIFC, ductInsulation, geomElem, productWrapper);
               }
               else if (element is DuctLining)
               {
                  DuctLining ductLining = element as DuctLining;
                  DuctLiningExporter.ExportDuctLining(exporterIFC, ductLining, geomElem, productWrapper);
               }
               else if (element is ElectricalSystem)
               {
                  ExporterCacheManager.SystemsCache.AddElectricalSystem(element.Id);
               }
               else if (element is FabricArea)
               {
                  // We are exporting the fabric area as a group only.
                  FabricSheetExporter.ExportFabricArea(exporterIFC, element, productWrapper);
               }
               else if (element is FabricSheet)
               {
                  FabricSheet fabricSheet = element as FabricSheet;
                  FabricSheetExporter.ExportFabricSheet(exporterIFC, fabricSheet, geomElem, productWrapper);
               }
               else if (element is FaceWall)
               {
                  WallExporter.ExportWall(exporterIFC, null, element, null, ref geomElem, productWrapper);
               }
               else if (element is FamilyInstance)
               {
                  FamilyInstance familyInstanceElem = element as FamilyInstance;
                  FamilyInstanceExporter.ExportFamilyInstanceElement(exporterIFC, familyInstanceElem, ref geomElem, productWrapper);
               }
               else if (element is FilledRegion)
               {
                  FilledRegion filledRegion = element as FilledRegion;
                  FilledRegionExporter.Export(exporterIFC, filledRegion, geomElem, productWrapper);
               }
               else if (element is Grid)
               {
                  ExporterCacheManager.GridCache.Add(element);
               }
               else if (element is Group)
               {
                  Group group = element as Group;
                  GroupExporter.ExportGroupElement(exporterIFC, group, productWrapper);
               }
               else if (element is HostedSweep)
               {
                  HostedSweep hostedSweep = element as HostedSweep;
                  HostedSweepExporter.Export(exporterIFC, hostedSweep, geomElem, productWrapper);
               }
               else if (element is Part)
               {
                  Part part = element as Part;
                  if (ExporterCacheManager.ExportOptionsCache.ExportPartsAsBuildingElements)
                     PartExporter.ExportPartAsBuildingElement(exporterIFC, part, geomElem, productWrapper);
                  else
                     PartExporter.ExportStandalonePart(exporterIFC, part, geomElem, productWrapper);
               }
               else if (element is PipeInsulation)
               {
                  PipeInsulation pipeInsulation = element as PipeInsulation;
                  PipeInsulationExporter.ExportPipeInsulation(exporterIFC, pipeInsulation, geomElem, productWrapper);
               }
               else if (element is Railing)
               {
                  if (ExporterCacheManager.RailingCache.Contains(element.Id))
                     RailingExporter.ExportRailingElement(exporterIFC, element as Railing, productWrapper);
                  else
                  {
                     ExporterCacheManager.RailingCache.Add(element.Id);
                     RailingExporter.AddSubElementsToCache(element as Railing);
                     shouldPreserveParameterCache = true;
                  }
               }
               else if (RampExporter.IsRamp(element))
               {
                  RampExporter.Export(exporterIFC, element, geomElem, productWrapper);
               }
               else if (IsRebarType(element))
               {
                  RebarExporter.Export(exporterIFC, element, productWrapper);
               }
               else if (element is RebarCoupler)
               {
                  RebarCoupler couplerElem = element as RebarCoupler;
                  RebarCouplerExporter.ExportCoupler(exporterIFC, couplerElem, productWrapper);
               }
               else if (element is RoofBase)
               {
                  RoofBase roofElement = element as RoofBase;
                  RoofExporter.Export(exporterIFC, roofElement, ref geomElem, productWrapper);
               }
               else if (element is SpatialElement)
               {
                  SpatialElement spatialElem = element as SpatialElement;
                  SpatialElementExporter.ExportSpatialElement(exporterIFC, spatialElem, productWrapper);
               }
               else if (IsStairs(element))
               {
                  StairsExporter.Export(exporterIFC, element, geomElem, productWrapper);
               }
               else if (element is TextNote)
               {
                  TextNote textNote = element as TextNote;
                  TextNoteExporter.Export(exporterIFC, textNote, productWrapper);
               }
               else if (element is TopographySurface)
               {
                  TopographySurface topSurface = element as TopographySurface;
                  SiteExporter.ExportTopographySurface(exporterIFC, topSurface, geomElem, productWrapper);
               }
               else if (element is Truss)
               {
                  if (ExporterCacheManager.TrussCache.Contains(element.Id))
                     AssemblyInstanceExporter.ExportTrussElement(exporterIFC, element as Truss, productWrapper);
                  else
                  {
                     ExporterCacheManager.TrussCache.Add(element.Id);
                     shouldPreserveParameterCache = true;
                  }
               }
               else if (element is Wall)
               {
                  Wall wallElem = element as Wall;
                  WallExporter.Export(exporterIFC, wallElem, ref geomElem, productWrapper);
               }
               else if (element is WallSweep)
               {
                  WallSweep wallSweep = element as WallSweep;
                  WallSweepExporter.Export(exporterIFC, wallSweep, geomElem, productWrapper);
               }
               else if (element is Zone)
               {
                  if (ExporterCacheManager.ZoneCache.Contains(element.Id))
                     ZoneExporter.ExportZone(exporterIFC, element as Zone, productWrapper);
                  else
                  {
                     ExporterCacheManager.ZoneCache.Add(element.Id);
                     shouldPreserveParameterCache = true;
                  }
               }
               else
               {
                  string ifcEnumType;
                  IFCExportInfoPair exportType = ExporterUtil.GetExportType(exporterIFC, element, out ifcEnumType);

                  // Check the intended IFC entity or type name is in the exclude list specified in the UI
                  Common.Enums.IFCEntityType elementClassTypeEnum;
                  if (Enum.TryParse<Common.Enums.IFCEntityType>(exportType.ExportInstance.ToString(), out elementClassTypeEnum)
                        || Enum.TryParse<Common.Enums.IFCEntityType>(exportType.ExportType.ToString(), out elementClassTypeEnum))
                     if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
                        return;

                  // The intention with the code below is to make this the "generic" element exporter, which would export any Revit element as any IFC instance.
                  // We would then in addition have specialized functions that would convert specific Revit elements to specific IFC instances where extra information
                  // could be gathered from the element.
                  bool exported = false;
                  if (IsMEPType(exporterIFC, element, exportType))
                  {
                     exported = GenericMEPExporter.Export(exporterIFC, element, geomElem, exportType, ifcEnumType, productWrapper);
                  }
                  else if (ExportAsProxy(element, exportType))
                  {
                     // Note that we currently export FaceWalls as proxies, and that FaceWalls are HostObjects, so we need
                     // to have this check before the (element is HostObject check.
                     exported = ProxyElementExporter.Export(exporterIFC, element, geomElem, productWrapper, exportType);
                  }
                  else if ((element is HostObject) || (element is DirectShape) || (element is FabricationPart))
                  {
                     // This is intended to work for any element.  However, there are some hidden elements that we likely want to ignore.
                     // As such, this is currently limited to the two types of elements that we know we want to export that aren't covered above.
                     // Note the general comment that we would like to revamp this whole routine to be cleaner and simpler.
                     exported = FamilyInstanceExporter.ExportGenericToSpecificElement(exporterIFC, element, ref geomElem, exportType, ifcEnumType, productWrapper);

                     if (!exported)
                        exported = (GenericElementExporter.ExportGenericElement(exporterIFC, element, geomElem, productWrapper, exportType) != null);
                  }

                  // For ducts and pipes, we will add a IfcRelCoversBldgElements during the end of export.
                  if (exported && (element is Duct || element is Pipe))
                     ExporterCacheManager.MEPCache.CoveredElementsCache.Add(element.Id);
               }

               if (element.AssemblyInstanceId != ElementId.InvalidElementId)
                  ExporterCacheManager.AssemblyInstanceCache.RegisterElements(element.AssemblyInstanceId, productWrapper);
               if (element.GroupId != ElementId.InvalidElementId)
                  ExporterCacheManager.GroupCache.RegisterElements(element.GroupId, productWrapper);

               st.RollBack();
            }
         }
         finally
         {
            exporterIFC.PopExportState();
            ExporterStateManager.PreserveElementParameterCache(element, shouldPreserveParameterCache);
         }
      }

      /// <summary>
      /// Sets the schema information for the current export options.  This can be overridden.
      /// </summary>
      protected virtual IFCFileModelOptions CreateIFCFileModelOptions(ExporterIFC exporterIFC)
      {
         IFCFileModelOptions modelOptions = new IFCFileModelOptions();
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
         {
            modelOptions.SchemaFile = LocateSchemaFile("IFC2X2_ADD1.exp");
            modelOptions.SchemaName = "IFC2x2_FINAL";
         }
         else if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            modelOptions.SchemaFile = LocateSchemaFile("IFC4.exp");
            modelOptions.SchemaName = "IFC4";
         }
         else
         {
            // We leave IFC2x3 as default until IFC4 is finalized and generally supported across platforms.
            modelOptions.SchemaFile = LocateSchemaFile("IFC2X3_TC1.exp");
            modelOptions.SchemaName = "IFC2x3";
         }
         return modelOptions;
      }

      private string LocateSchemaFile(string schemaFileName)
      {
         string filePath = null;
#if IFC_OPENSOURCE
         // Find the alternate schema file from the open source install folder
         filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), schemaFileName);
         if (!File.Exists(filePath))
#endif
         {
            filePath = Path.Combine(DirectoryUtil.RevitProgramPath, "EDM", schemaFileName);
         }
         return filePath;
      }

      /// <summary>
      /// Sets the lists of property sets to be exported.  This can be overriden.
      /// </summary>
      protected virtual void InitializePropertySets()
      {
         ExporterInitializer.InitPropertySets(m_PropertySetsToExport);
      }

      /// <summary>
      /// Sets the lists of quantities to be exported.  This can be overriden.
      /// </summary>
      protected virtual void InitializeQuantities(IFCVersion fileVersion)
      {
         ExporterInitializer.InitQuantities(m_QuantitiesToExport, ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities);
      }

      /// <summary>
      /// Initializes the common properties at the beginning of the export process.
      /// </summary>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      /// <param name="document">The document to export.</param>
      private void BeginExport(ExporterIFC exporterIFC, Document document, Autodesk.Revit.DB.View filterView)
      {
         statusBar = RevitStatusBar.Create();

         // cache options
         ExportOptionsCache exportOptionsCache = ExportOptionsCache.Create(exporterIFC, document, filterView);
         ExporterCacheManager.ExportOptionsCache = exportOptionsCache;

         // Set language.
         Application app = document.Application;
         string pathName = document.PathName;
         LanguageType langType = LanguageType.Unknown;
         if (!String.IsNullOrEmpty(pathName))
         {
            try
            {
               BasicFileInfo basicFileInfo = BasicFileInfo.Extract(pathName);
               if (basicFileInfo != null)
                  langType = basicFileInfo.LanguageWhenSaved;
            }
            catch
            {
            }
         }
         if (langType == LanguageType.Unknown)
            langType = app.Language;
         ExporterCacheManager.LanguageType = langType;

         ElementFilteringUtil.InitCategoryVisibilityCache();
         NamingUtil.InitNameIncrNumberCache();

         ExporterCacheManager.Document = document;
         String writeIFCExportedElementsVar = Environment.GetEnvironmentVariable("WriteIFCExportedElements");
         if (writeIFCExportedElementsVar != null && writeIFCExportedElementsVar.Length > 0)
         {
            m_Writer = new StreamWriter(@"c:\ifc-output-filters.txt");
         }

         IFCFileModelOptions modelOptions = CreateIFCFileModelOptions(exporterIFC);

         m_IfcFile = IFCFile.Create(modelOptions);
         exporterIFC.SetFile(m_IfcFile);

         //init common properties
         InitializePropertySets();
         InitializeQuantities(ExporterCacheManager.ExportOptionsCache.FileVersion);

         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            // create building
            IFCAnyHandle applicationHandle = CreateApplicationInformation(file, document);

            CreateGlobalCartesianOrigin(exporterIFC);
            CreateGlobalDirection(exporterIFC);
            CreateGlobalDirection2D(exporterIFC);

            IFCAnyHandle buildingPlacement = CreateBuildingPlacement(file);

            CreateProject(exporterIFC, document, applicationHandle);

            IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
            ProjectInfo projectInfo = document.ProjectInformation;

            // create levels
            // Check if there is any level assigned as a building storey, if no at all, model will be exported without Building and BuildingStorey, all containment will be to Site
            List<Level> levels = LevelUtil.FindAllLevels(document);
            bool exportBuilding = false;
            foreach (Level level in levels)
            {
               exportBuilding |= LevelUtil.IsBuildingStory(level);
            }

            // Skip Building if there is no Storey to be exported
            if (exportBuilding)
            {
               CreateBuildingFromProjectInfo(exporterIFC, document, buildingPlacement);

               IList<Element> unassignedBaseLevels = new List<Element>();

               //ExporterCacheManager.ExportOptionsCache.ExportAllLevels = exportAllLevels;
               double lengthScale = UnitUtil.ScaleLengthForRevitAPI();

               IFCAnyHandle prevBuildingStorey = null;
               IFCAnyHandle prevPlacement = null;
               double prevHeight = 0.0;
               double prevElev = 0.0;

               for (int ii = 0; ii < levels.Count; ii++)
               {
                  Level level = levels[ii];
                  if (level == null)
                     continue;

                  IFCLevelInfo levelInfo = null;

                  if (!LevelUtil.IsBuildingStory(level))
                  {
                     if (prevBuildingStorey == null)
                        unassignedBaseLevels.Add(level);
                     else
                     {
                        levelInfo = IFCLevelInfo.Create(prevBuildingStorey, prevPlacement, prevHeight, prevElev, lengthScale, true);
                        ExporterCacheManager.LevelInfoCache.AddLevelInfo(exporterIFC, level.Id, levelInfo, false);
                     }
                     continue;
                  }

                  // When exporting to IFC 2x3, we have a limited capability to export some Revit view-specific
                  // elements, specifically Filled Regions and Text.  However, we do not have the
                  // capability to choose which views to export.  As such, we will choose (up to) one DBView per
                  // exported level.
                  // TODO: Let user choose which view(s) to export.  Ensure that the user know that only one view
                  // per level is supported.
                  View view = LevelUtil.FindViewByLevel(document, ViewType.FloorPlan, level);
                  if (view != null)
                  {
                     ExporterCacheManager.DBViewsToExport[view.Id] = level.Id;
                  }

                  double elev = level.ProjectElevation;
                  double height = 0.0;
                  List<ElementId> coincidentLevels = new List<ElementId>();
                  for (int jj = ii + 1; jj < levels.Count; jj++)
                  {
                     Level nextLevel = levels[jj];
                     if (!LevelUtil.IsBuildingStory(nextLevel))
                        continue;

                     double nextElev = nextLevel.ProjectElevation;
                     if (!MathUtil.IsAlmostEqual(nextElev, elev))
                     {
                        height = nextElev - elev;
                        break;
                     }
                     else if (ExporterCacheManager.ExportOptionsCache.WallAndColumnSplitting)
                        coincidentLevels.Add(nextLevel.Id);
                  }

                  double elevation = UnitUtil.ScaleLength(elev);
                  XYZ orig = new XYZ(0.0, 0.0, elevation);

                  IFCAnyHandle placement = ExporterUtil.CreateLocalPlacement(file, buildingPlacement, orig, null, null);
                  string bsObjectType = NamingUtil.GetObjectTypeOverride(level, null);
                  IFCElementComposition ifcComposition = LevelUtil.GetElementCompositionTypeOverride(level);
                  IFCAnyHandle buildingStorey = IFCInstanceExporter.CreateBuildingStorey(exporterIFC, level, ExporterCacheManager.OwnerHistoryHandle,
                     bsObjectType, placement, ifcComposition, elevation);


                  // Create classification reference when level has classification field name assigned to it
                  ClassificationUtil.CreateClassification(exporterIFC, file, level, buildingStorey);

                  if (prevBuildingStorey == null)
                  {
                     foreach (Level baseLevel in unassignedBaseLevels)
                     {
                        levelInfo = IFCLevelInfo.Create(buildingStorey, placement, height, elev, lengthScale, true);
                        ExporterCacheManager.LevelInfoCache.AddLevelInfo(exporterIFC, baseLevel.Id, levelInfo, false);
                     }
                  }
                  prevBuildingStorey = buildingStorey;
                  prevPlacement = placement;
                  prevHeight = height;
                  prevElev = elev;

                  levelInfo = IFCLevelInfo.Create(buildingStorey, placement, height, elev, lengthScale, true);
                  ExporterCacheManager.LevelInfoCache.AddLevelInfo(exporterIFC, level.Id, levelInfo, true);

                  // if we have coincident levels, add buildingstories for them but use the old handle.
                  for (int jj = 0; jj < coincidentLevels.Count; jj++)
                  {
                     level = levels[ii + jj + 1];
                     levelInfo = IFCLevelInfo.Create(buildingStorey, placement, height, elev, lengthScale, true);
                     ExporterCacheManager.LevelInfoCache.AddLevelInfo(exporterIFC, level.Id, levelInfo, true);
                  }

                  ii += coincidentLevels.Count;

                  // We will export element properties, quantities and classifications when we decide to keep the level - we may delete it later.
               }
            }
            transaction.Commit();
         }
      }

      private void GetElementHandles(ICollection<ElementId> ids, ISet<IFCAnyHandle> handles)
      {
         if (ids != null)
         {
            foreach (ElementId id in ids)
            {
               IFCAnyHandle handle = ExporterCacheManager.ElementToHandleCache.Find(id);
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(handle))
                  handles.Add(handle);
            }
         }
      }

      private static IFCAnyHandle CreateRelServicesBuildings(IFCAnyHandle buildingHandle, IFCFile file,
         IFCAnyHandle ownerHistory, IFCAnyHandle systemHandle)
      {
         HashSet<IFCAnyHandle> relatedBuildings = new HashSet<IFCAnyHandle>();
         relatedBuildings.Add(buildingHandle);
         return IFCInstanceExporter.CreateRelServicesBuildings(file, GUIDUtil.CreateGUID(),
            ownerHistory, null, null, systemHandle, relatedBuildings);
      }

      private static void UpdateLocalPlacementForElement(IFCAnyHandle elemHnd, IFCFile file,
         IFCAnyHandle containerObjectPlacement, Transform containerInvTrf)
      {
         IFCAnyHandle elemObjectPlacementHnd = IFCAnyHandleUtil.GetObjectPlacement(elemHnd);

         // In the case that the object has no local placement at all.  In that case, create a new default one, and set the object's
         // local placement relative to the containerObjectPlacement.  Note that in this case we are ignoring containerInvTrf
         // entirely, which may not be the right thing to do, but doesn't currently seem to occur in practice.
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(elemObjectPlacementHnd) || !elemObjectPlacementHnd.IsTypeOf("IfcLocalPlacement"))
         {
            IFCAnyHandle relToContainerPlacement =
               ExporterUtil.CreateLocalPlacement(file, containerObjectPlacement, null, null, null);
            IFCAnyHandleUtil.SetAttribute(elemHnd, "ObjectPlacement", relToContainerPlacement);
            return;
         }

         // There are two cases here.
         // 1. We want to update the local placement of the object to be relative to its container without
         // adjusting its global position.  In this case containerInvTrf would be non-null, and we would
         // adjust the relative placement to keep the global position constant.
         // 2. We want to update the local placement of the object to follow any shift of the parent object.
         // In this case containerInvTrf would be null, and we don't update the relative placement.
         Transform newTrf = null;
         if (containerInvTrf != null)
         {
            IFCAnyHandle oldRelativePlacement = IFCAnyHandleUtil.GetInstanceAttribute(elemObjectPlacementHnd, "PlacementRelTo");
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(oldRelativePlacement))
            {
               newTrf = ExporterUtil.GetTransformFromLocalPlacementHnd(elemObjectPlacementHnd);
            }
            else
            {
               Transform originalTotalTrf = ExporterUtil.GetTotalTransformFromLocalPlacement(elemObjectPlacementHnd);
               newTrf = containerInvTrf.Multiply(originalTotalTrf);
            }
         }

         GeometryUtil.SetPlacementRelTo(elemObjectPlacementHnd, containerObjectPlacement);

         if (newTrf == null)
            return;

         IFCAnyHandle newRelativePlacement =
            ExporterUtil.CreateAxis2Placement3D(file, newTrf.Origin, newTrf.BasisZ, newTrf.BasisX);
         GeometryUtil.SetRelativePlacement(elemObjectPlacementHnd, newRelativePlacement);
      }

      private void CreatePresentationLayerAssignments(ExporterIFC exporterIFC, IFCFile file)
      {
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
            return;

         ISet<IFCAnyHandle> assignedRepresentations = new HashSet<IFCAnyHandle>();
         IDictionary<string, ISet<IFCAnyHandle>> combinedPresentationLayerSet =
            new Dictionary<string, ISet<IFCAnyHandle>>();

         foreach (KeyValuePair<string, ICollection<IFCAnyHandle>> presentationLayerSet in ExporterCacheManager.PresentationLayerSetCache)
         {
            ISet<IFCAnyHandle> validHandles = new HashSet<IFCAnyHandle>();
            foreach (IFCAnyHandle handle in presentationLayerSet.Value)
            {
               if (IFCAnyHandleUtil.IsValidHandle(handle))
               {
                  validHandles.Add(handle);
                  assignedRepresentations.Add(handle);
               }
            }

            if (validHandles.Count == 0)
               continue;

            combinedPresentationLayerSet[presentationLayerSet.Key] = validHandles;
         }

         // Only for 2022
         //// Now handle the internal cases.
         //IDictionary<string, IList<IFCAnyHandle>> presentationLayerAssignments = exporterIFC.GetPresentationLayerAssignments();
         //foreach (KeyValuePair<string, IList<IFCAnyHandle>> presentationLayerAssignment in presentationLayerAssignments)
         //{
         //   // Some of the items may have been deleted, remove them from set.
         //   ICollection<IFCAnyHandle> newLayeredItemSet = new HashSet<IFCAnyHandle>();
         //   IList<IFCAnyHandle> initialSet = presentationLayerAssignment.Value;
         //   foreach (IFCAnyHandle currItem in initialSet)
         //   {
         //      if (IFCAnyHandleUtil.IsValidHandle(currItem) && !assignedRepresentations.Contains(currItem))
         //         newLayeredItemSet.Add(currItem);
         //   }

         //   if (newLayeredItemSet.Count == 0)
         //      continue;

         //   string layerName = presentationLayerAssignment.Key;
         //   ISet<IFCAnyHandle> layeredItemSet;
         //   if (!combinedPresentationLayerSet.TryGetValue(layerName, out layeredItemSet))
         //   {
         //      layeredItemSet = new HashSet<IFCAnyHandle>();
         //      combinedPresentationLayerSet[layerName] = layeredItemSet;
         //   }
         //   layeredItemSet.UnionWith(newLayeredItemSet);
         //}

         foreach (KeyValuePair<string, ISet<IFCAnyHandle>> presentationLayerSet in combinedPresentationLayerSet)
         {
            IFCInstanceExporter.CreatePresentationLayerAssignment(file, presentationLayerSet.Key, null, presentationLayerSet.Value, null);
         }
      }

      /// <summary>
      /// Completes the export process by writing information stored incrementally during export to the file.
      /// </summary>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      /// <param name="document">The document to export.</param>
      private void EndExport(ExporterIFC exporterIFC, Document document)
      {
         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            // In some cases, like multi-story stairs and ramps, we may have the same Pset used for multiple levels.
            // If ifcParams is null, re-use the property set.
            ISet<string> locallyUsedGUIDs = new HashSet<string>();

            // Relate Ducts and Pipes to their coverings (insulations and linings)
            foreach (ElementId ductOrPipeId in ExporterCacheManager.MEPCache.CoveredElementsCache)
            {
               IFCAnyHandle ductOrPipeHandle = ExporterCacheManager.MEPCache.Find(ductOrPipeId);
               if (IFCAnyHandleUtil.IsNullOrHasNoValue(ductOrPipeHandle))
                  continue;

               HashSet<IFCAnyHandle> coveringHandles = new HashSet<IFCAnyHandle>();

               try
               {
                  ICollection<ElementId> liningIds = InsulationLiningBase.GetLiningIds(document, ductOrPipeId);
                  GetElementHandles(liningIds, coveringHandles);
               }
               catch
               {
               }

               try
               {
                  ICollection<ElementId> insulationIds = InsulationLiningBase.GetInsulationIds(document, ductOrPipeId);
                  GetElementHandles(insulationIds, coveringHandles);
               }
               catch
               {
               }

               if (coveringHandles.Count > 0)
                  IFCInstanceExporter.CreateRelCoversBldgElements(file, GUIDUtil.CreateGUID(), ownerHistory, null, null, ductOrPipeHandle, coveringHandles);
            }

            // Relate stair components to stairs
            foreach (KeyValuePair<ElementId, StairRampContainerInfo> stairRamp in ExporterCacheManager.StairRampContainerInfoCache)
            {
               StairRampContainerInfo stairRampInfo = stairRamp.Value;

               IList<IFCAnyHandle> hnds = stairRampInfo.StairOrRampHandles;
               for (int ii = 0; ii < hnds.Count; ii++)
               {
                  IFCAnyHandle hnd = hnds[ii];
                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(hnd))
                     continue;

                  IList<IFCAnyHandle> comps = stairRampInfo.Components[ii];
                  if (comps.Count == 0)
                     continue;

                  Element elem = document.GetElement(stairRamp.Key);
                  string guid = GUIDUtil.CreateSubElementGUID(elem, (int)IFCStairSubElements.ContainmentRelation);
                  if (locallyUsedGUIDs.Contains(guid))
                     guid = GUIDUtil.CreateGUID();
                  else
                     locallyUsedGUIDs.Add(guid);

                  ExporterUtil.RelateObjects(exporterIFC, guid, hnd, comps);
               }
            }

            // create a Default site if we have latitude and longitude information.
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ExporterCacheManager.SiteHandle))
            {
               using (ProductWrapper productWrapper = ProductWrapper.Create(exporterIFC, true))
               {
                  SiteExporter.ExportDefaultSite(exporterIFC, document, productWrapper);
                  ExporterUtil.ExportRelatedProperties(exporterIFC, document.ProjectInformation, productWrapper);
               }
            }

            ProjectInfo projectInfo = document.ProjectInformation;

            IFCAnyHandle projectHandle = ExporterCacheManager.ProjectHandle;
            IFCAnyHandle siteHandle = ExporterCacheManager.SiteHandle;
            IFCAnyHandle buildingHandle = ExporterCacheManager.BuildingHandle;

            bool projectHasSite = !IFCAnyHandleUtil.IsNullOrHasNoValue(siteHandle);
            bool projectHasBuilding = !IFCAnyHandleUtil.IsNullOrHasNoValue(buildingHandle);

            IFCAnyHandle siteOrbuildingHnd = siteHandle;

            if (!projectHasSite)
            {
               if (!projectHasBuilding)
               {
                  // if at this point the buildingHnd is null, which means that the model does not
                  // have Site nor any Level assigned to the BuildingStorey, create the IfcBuilding 
                  // as the general container for all the elements (should be backward compatible).
                  IFCAnyHandle buildingPlacement = CreateBuildingPlacement(file);
                  buildingHandle = CreateBuildingFromProjectInfo(exporterIFC, document, buildingPlacement);
                  ExporterCacheManager.BuildingHandle = buildingHandle;
                  projectHasBuilding = true;
               }
               siteOrbuildingHnd = buildingHandle;
            }

            // Last chance to create the building handle was just above.
            if (projectHasSite)
            {
               ExporterCacheManager.ContainmentCache.AddRelation(projectHandle, siteHandle);

               if (projectHasBuilding)
               {
                  // assoc. site to the building.
                  ExporterCacheManager.ContainmentCache.AddRelation(siteHandle, buildingHandle);

                  IFCAnyHandle buildingPlacement = IFCAnyHandleUtil.GetObjectPlacement(buildingHandle);
                  IFCAnyHandle relPlacement = IFCAnyHandleUtil.GetObjectPlacement(siteHandle);
                  GeometryUtil.SetPlacementRelTo(buildingPlacement, relPlacement);
               }
            }
            else
            {
               // relate building and project if no site
               if (projectHasBuilding)
                  ExporterCacheManager.ContainmentCache.AddRelation(projectHandle, buildingHandle);
            }

            // relate assembly elements to assemblies
            foreach (KeyValuePair<ElementId, AssemblyInstanceInfo> assemblyInfoEntry in ExporterCacheManager.AssemblyInstanceCache)
            {
               AssemblyInstanceInfo assemblyInfo = assemblyInfoEntry.Value;
               if (assemblyInfo == null)
                  continue;

               IFCAnyHandle assemblyInstanceHandle = assemblyInfo.AssemblyInstanceHandle;
               HashSet<IFCAnyHandle> elementHandles = assemblyInfo.ElementHandles;
               if (elementHandles != null && assemblyInstanceHandle != null && elementHandles.Contains(assemblyInstanceHandle))
                  elementHandles.Remove(assemblyInstanceHandle);

               if (assemblyInstanceHandle != null && elementHandles != null && elementHandles.Count != 0)
               {
                  Element assemblyInstance = document.GetElement(assemblyInfoEntry.Key);
                  string guid = GUIDUtil.CreateSubElementGUID(assemblyInstance, (int)IFCAssemblyInstanceSubElements.RelContainedInSpatialStructure);

                  if (IFCAnyHandleUtil.IsSubTypeOf(assemblyInstanceHandle, IFCEntityType.IfcSystem))
                  {
                     IFCInstanceExporter.CreateRelAssignsToGroup(file, guid, ownerHistory, null, null, elementHandles, null, assemblyInstanceHandle);
                  }
                  else
                  {
                     ExporterUtil.RelateObjects(exporterIFC, guid, assemblyInstanceHandle, elementHandles);
                     // Set the PlacementRelTo of assembly elements to assembly instance.
                     IFCAnyHandle assemblyPlacement = IFCAnyHandleUtil.GetObjectPlacement(assemblyInstanceHandle);
                     AssemblyInstanceExporter.SetLocalPlacementsRelativeToAssembly(exporterIFC, assemblyPlacement, elementHandles);
                  }

                  // We don't do this in RegisterAssemblyElement because we want to make sure that the IfcElementAssembly has been created.
                  ExporterCacheManager.ElementsInAssembliesCache.UnionWith(elementHandles);
               }
            }

            // relate group elements to groups
            foreach (KeyValuePair<ElementId, GroupInfo> groupEntry in ExporterCacheManager.GroupCache)
            {
               GroupInfo groupInfo = groupEntry.Value;
               if (groupInfo == null)
                  continue;

               if (groupInfo.GroupHandle != null && groupInfo.ElementHandles != null &&
                   groupInfo.ElementHandles.Count != 0 && groupInfo.GroupType.ExportInstance != IFCEntityType.UnKnown)
               {
                  Element group = document.GetElement(groupEntry.Key);
                  string guid = GUIDUtil.CreateSubElementGUID(group, (int)IFCGroupSubElements.RelAssignsToGroup);

                  IFCAnyHandle groupHandle = groupInfo.GroupHandle;
                  HashSet<IFCAnyHandle> elementHandles = groupInfo.ElementHandles;
                  if (elementHandles != null && groupHandle != null && elementHandles.Contains(groupHandle))
                     elementHandles.Remove(groupHandle);

                  if (elementHandles != null && groupHandle != null && elementHandles.Count > 0)
                  {
                     // Group may be exported as IfcFurniture which contains IfcSystemFurnitureElements, so they need a RelAggregates relationship
                     if (groupEntry.Value.GroupType.ExportInstance == IFCEntityType.IfcFurniture)
                        IFCInstanceExporter.CreateRelAggregates(file, GUIDUtil.CreateGUID(), ownerHistory, null, null, groupHandle, elementHandles);
                     else
                        IFCInstanceExporter.CreateRelAssignsToGroup(file, guid, ownerHistory, null, null, elementHandles, null, groupHandle);
                  }
               }
            }

            IFCAnyHandle defContainerObjectPlacement = IFCAnyHandleUtil.GetObjectPlacement(siteOrbuildingHnd);
            Transform defContainerTrf = ExporterUtil.GetTotalTransformFromLocalPlacement(defContainerObjectPlacement);
            Transform defContainerInvTrf = defContainerTrf.Inverse;

            // create an association between the IfcBuilding and building elements with no other containment.
            HashSet<IFCAnyHandle> buildingElements = RemoveContainedHandlesFromSet(ExporterCacheManager.LevelInfoCache.OrphanedElements);
            buildingElements.UnionWith(exporterIFC.GetRelatedElements());
            if (buildingElements.Count > 0)
            {
               HashSet<IFCAnyHandle> relatedElementSetForSite = new HashSet<IFCAnyHandle>();
               HashSet<IFCAnyHandle> relatedElementSetForBuilding = new HashSet<IFCAnyHandle>();
               // If the object is supposed to be placed directly on Site or Building, change the object placement to be relative to the Site or Building
               foreach (IFCAnyHandle elemHnd in buildingElements)
               {
                  ElementId elementId = ExporterCacheManager.HandleToElementCache.Find(elemHnd);
                  Element elem = document.GetElement(elementId);

                  // if there is override, use the override otherwise use default from site
                  IFCAnyHandle overrideContainer = null;
                  ParameterUtil.OverrideContainmentParameter(exporterIFC, elem, out overrideContainer);

                  bool containerIsSite = projectHasSite;
                  bool containerIsBuilding = projectHasBuilding;

                  IFCAnyHandle containerObjectPlacement = null;
                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(overrideContainer))
                  {
                     containerObjectPlacement = IFCAnyHandleUtil.GetObjectPlacement(overrideContainer);
                     containerIsSite = IFCAnyHandleUtil.IsTypeOf(overrideContainer, IFCEntityType.IfcSite);
                     containerIsBuilding = !containerIsSite &&
                        IFCAnyHandleUtil.IsTypeOf(overrideContainer, IFCEntityType.IfcBuilding);
                  }
                  else
                  {
                     // Default behavior (generally site).
                     containerObjectPlacement = defContainerObjectPlacement;
                  }

                  if (containerIsSite)
                     relatedElementSetForSite.Add(elemHnd);
                  else if (containerIsBuilding)
                     relatedElementSetForBuilding.Add(elemHnd);

                  UpdateLocalPlacementForElement(elemHnd, file, containerObjectPlacement, null);
               }

               if (relatedElementSetForBuilding.Count > 0 && projectHasBuilding)
               {
                  string guid = GUIDUtil.CreateSubElementGUID(projectInfo, (int)IFCProjectSubElements.RelContainedInBuildingSpatialStructure);
                  IFCInstanceExporter.CreateRelContainedInSpatialStructure(file, guid,
                     ownerHistory, null, null, relatedElementSetForBuilding, buildingHandle);
               }

               if (relatedElementSetForSite.Count > 0 && projectHasSite)
               {
                  string guid = GUIDUtil.CreateSubElementGUID(projectInfo, (int)IFCProjectSubElements.RelContainedInSiteSpatialStructure);
                  IFCInstanceExporter.CreateRelContainedInSpatialStructure(file, guid,
                     ownerHistory, null, null, relatedElementSetForSite, siteHandle);
               }
            }

            // create an association between the IfcBuilding and spacial elements with no other containment
            // The name "GetRelatedProducts()" is misleading; this only covers spaces.
            HashSet<IFCAnyHandle> buildingSpaces = RemoveContainedHandlesFromSet(ExporterCacheManager.LevelInfoCache.OrphanedSpaces);
            buildingSpaces.UnionWith(exporterIFC.GetRelatedProducts());
            if (buildingSpaces.Count > 0)
            {
               HashSet<IFCAnyHandle> relatedElementSetForBuilding = new HashSet<IFCAnyHandle>();
               HashSet<IFCAnyHandle> relatedElementSetForSite = new HashSet<IFCAnyHandle>();
               foreach (IFCAnyHandle indivSpace in buildingSpaces)
               {
                  bool containerIsSite = projectHasSite;
                  bool containerIsBuilding = projectHasBuilding;

                  // if there is override, use the override otherwise use default from site
                  IFCAnyHandle overrideContainer = null;
                  ParameterUtil.OverrideSpaceContainmentParameter(exporterIFC, document, indivSpace, out overrideContainer);
                  IFCAnyHandle containerObjectPlacement = null;
                  Transform containerInvTrf = null;

                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(overrideContainer))
                  {
                     containerObjectPlacement = IFCAnyHandleUtil.GetObjectPlacement(overrideContainer);
                     Transform containerTrf = ExporterUtil.GetTotalTransformFromLocalPlacement(containerObjectPlacement);
                     containerInvTrf = containerTrf.Inverse;
                     containerIsSite = IFCAnyHandleUtil.IsTypeOf(overrideContainer, IFCEntityType.IfcSite);
                     containerIsBuilding = !containerIsSite &&
                        IFCAnyHandleUtil.IsTypeOf(overrideContainer, IFCEntityType.IfcBuilding);
                  }
                  else
                  {
                     // Default behavior (generally site).
                     containerObjectPlacement = defContainerObjectPlacement;
                     containerInvTrf = defContainerInvTrf;
                  }

                  if (containerIsSite)
                     relatedElementSetForSite.Add(indivSpace);
                  else if (containerIsBuilding)
                     relatedElementSetForBuilding.Add(indivSpace);

                  UpdateLocalPlacementForElement(indivSpace, file, containerObjectPlacement, containerInvTrf);
               }

               if (relatedElementSetForBuilding.Count > 0)
               {
                  string guid = GUIDUtil.CreateGUID();
                  ExporterCacheManager.ContainmentCache.SetGUIDForRelation(buildingHandle, guid);
                  ExporterCacheManager.ContainmentCache.AddRelations(buildingHandle, relatedElementSetForBuilding);
               }

               if (relatedElementSetForSite.Count > 0)
               {
                  string guid = GUIDUtil.CreateGUID();
                  ExporterCacheManager.ContainmentCache.SetGUIDForRelation(siteHandle, guid);
                  ExporterCacheManager.ContainmentCache.AddRelations(siteHandle, relatedElementSetForSite);
               }
            }

            // relate levels and products.
            RelateLevels(exporterIFC, document);

            // relate objects in containment cache.
            foreach (KeyValuePair<IFCAnyHandle, ICollection<IFCAnyHandle>> container in ExporterCacheManager.ContainmentCache)
            {
               if (container.Value.Count() > 0)
               {
                  string relationGUID = ExporterCacheManager.ContainmentCache.GetGUIDForRelation(container.Key);
                  ExporterUtil.RelateObjects(exporterIFC, relationGUID, container.Key, container.Value);
               }
            }

            // These elements are created internally, but we allow custom property sets for them.  Create them here.
            using (ProductWrapper productWrapper = ProductWrapper.Create(exporterIFC, true))
            {
               if (projectHasBuilding)
                  productWrapper.AddBuilding(projectInfo, buildingHandle);
               if (projectInfo != null)
                  ExporterUtil.ExportRelatedProperties(exporterIFC, projectInfo, productWrapper);
            }

            // create material layer associations
            foreach (IFCAnyHandle materialSetLayerUsageHnd in ExporterCacheManager.MaterialLayerRelationsCache.Keys)
            {
               HashSet<IFCAnyHandle> materialLayerRelCache = null;
               if (!ExporterCacheManager.MaterialLayerRelationsCache.TryGetValue(materialSetLayerUsageHnd, out materialLayerRelCache))
                  continue;

               IFCInstanceExporter.CreateRelAssociatesMaterial(file, GUIDUtil.CreateGUID(), ownerHistory,
                   null, null, materialLayerRelCache,
                   materialSetLayerUsageHnd);
            }

            // create material associations
            foreach (IFCAnyHandle materialHnd in ExporterCacheManager.MaterialRelationsCache.Keys)
            {
               // In some specific cased the reference object might have been deleted. Clear those from the Type cache first here
               ExporterCacheManager.MaterialRelationsCache.CleanRefObjects(materialHnd);

               HashSet<IFCAnyHandle> materialRelationsHandles = null;
               if (!ExporterCacheManager.MaterialRelationsCache.TryGetValue(materialHnd, out materialRelationsHandles))
                  continue;

               IFCInstanceExporter.CreateRelAssociatesMaterial(file, GUIDUtil.CreateGUID(), ownerHistory,
                   null, null, materialRelationsHandles, materialHnd);
            }

            // create type relations
            foreach (IFCAnyHandle typeObj in ExporterCacheManager.TypeRelationsCache.Keys)
            {
               // In some specific cased the reference object might have been deleted. Clear those from the Type cache first here
               ExporterCacheManager.TypeRelationsCache.CleanRefObjects(typeObj);

               HashSet<IFCAnyHandle> typeRelCache = null;
               if (!ExporterCacheManager.TypeRelationsCache.TryGetValue(typeObj, out typeRelCache))
                  continue;

               IFCInstanceExporter.CreateRelDefinesByType(file, GUIDUtil.CreateGUID(), ownerHistory,
                   null, null, typeRelCache, typeObj);
            }

            // create type property relations
            foreach (TypePropertyInfo typePropertyInfo in ExporterCacheManager.TypePropertyInfoCache.Values)
            {
               if (typePropertyInfo.AssignedToType)
                  continue;

               ICollection<IFCAnyHandle> propertySets = typePropertyInfo.PropertySets;
               ISet<IFCAnyHandle> elements = typePropertyInfo.Elements;

               if (elements.Count == 0)
                  continue;

               foreach (IFCAnyHandle propertySet in propertySets)
               {
                  try
                  {
                     ExporterUtil.CreateRelDefinesByProperties(file, GUIDUtil.CreateGUID(),
                        ownerHistory, null, null, elements, propertySet);
                  }
                  catch
                  {
                  }
               }
            }

            // create space boundaries
            foreach (SpaceBoundary boundary in ExporterCacheManager.SpaceBoundaryCache)
            {
               SpatialElementExporter.ProcessIFCSpaceBoundary(exporterIFC, boundary, file);
            }

            // create wall/wall connectivity objects
            if (ExporterCacheManager.WallConnectionDataCache.Count > 0 && !ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
            {
               IList<IDictionary<ElementId, IFCAnyHandle>> hostObjects = exporterIFC.GetHostObjects();
               List<int> relatingPriorities = new List<int>();
               List<int> relatedPriorities = new List<int>();

               foreach (WallConnectionData wallConnectionData in ExporterCacheManager.WallConnectionDataCache)
               {
                  foreach (IDictionary<ElementId, IFCAnyHandle> mapForLevel in hostObjects)
                  {
                     IFCAnyHandle wallElementHandle, otherElementHandle;
                     if (!mapForLevel.TryGetValue(wallConnectionData.FirstId, out wallElementHandle))
                        continue;
                     if (!mapForLevel.TryGetValue(wallConnectionData.SecondId, out otherElementHandle))
                        continue;

                     // NOTE: Definition of RelConnectsPathElements has the connection information reversed
                     // with respect to the order of the paths.
                     string connectionName = ExporterUtil.GetGlobalId(wallElementHandle) + "|"
                                                 + ExporterUtil.GetGlobalId(otherElementHandle);
                     string connectionType = "Structural";   // Assigned as Description
                     IFCInstanceExporter.CreateRelConnectsPathElements(file, GUIDUtil.CreateGUID(), ownerHistory,
                         connectionName, connectionType, wallConnectionData.ConnectionGeometry, wallElementHandle, otherElementHandle, relatingPriorities,
                         relatedPriorities, wallConnectionData.SecondConnectionType, wallConnectionData.FirstConnectionType);
                  }
               }
            }

            // create Zones and groups of Zones.
            {
               // Collect zone group names as we go.  We will limit a zone to be only in one group.
               IDictionary<string, ISet<IFCAnyHandle>> zoneGroups = new Dictionary<string, ISet<IFCAnyHandle>>();

               string relAssignsToGroupName = "Spatial Zone Assignment";
               foreach (KeyValuePair<string, ZoneInfo> zone in ExporterCacheManager.ZoneInfoCache)
               {
                  ZoneInfo zoneInfo = zone.Value;
                  if (zoneInfo == null)
                     continue;

                  string zoneName = zone.Key;
                  IFCAnyHandle zoneHandle = IFCInstanceExporter.CreateZone(file, GUIDUtil.CreateGUID(), ownerHistory,
                      zoneName, zoneInfo.Description, zoneInfo.ObjectType, zoneInfo.LongName);
                  IFCInstanceExporter.CreateRelAssignsToGroup(file, GUIDUtil.CreateGUID(), ownerHistory,
                      relAssignsToGroupName, null, zoneInfo.RoomHandles, null, zoneHandle);

                  HashSet<IFCAnyHandle> zoneHnds = new HashSet<IFCAnyHandle>();
                  zoneHnds.Add(zoneHandle);

                  foreach (KeyValuePair<string, IFCAnyHandle> classificationReference in zoneInfo.ClassificationReferences)
                  {
                     IFCAnyHandle relAssociates = IFCInstanceExporter.CreateRelAssociatesClassification(file, GUIDUtil.CreateGUID(),
                         ownerHistory, classificationReference.Key, "", zoneHnds, classificationReference.Value);
                  }

                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(zoneInfo.EnergyAnalysisProperySetHandle))
                  {
                     ExporterUtil.CreateRelDefinesByProperties(file, GUIDUtil.CreateGUID(),
                                                         ownerHistory, null, null, zoneHnds, zoneInfo.EnergyAnalysisProperySetHandle);
                  }

                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(zoneInfo.ZoneCommonProperySetHandle))
                  {
                     ExporterUtil.CreateRelDefinesByProperties(file, GUIDUtil.CreateGUID(),
                         ownerHistory, null, null, zoneHnds, zoneInfo.ZoneCommonProperySetHandle);
                  }

                  string groupName = zoneInfo.GroupName;
                  if (!string.IsNullOrWhiteSpace(groupName))
                  {
                     ISet<IFCAnyHandle> currentGroup = null;
                     if (!zoneGroups.TryGetValue(groupName, out currentGroup))
                     {
                        currentGroup = new HashSet<IFCAnyHandle>();
                        zoneGroups.Add(groupName, currentGroup);
                     }
                     currentGroup.Add(zoneHandle);
                  }
               }

               // now create any zone groups.
               string relAssignsToZoneGroupName = "Zone Group Assignment";
               foreach (KeyValuePair<string, ISet<IFCAnyHandle>> zoneGroup in zoneGroups)
               {
                  IFCAnyHandle zoneGroupHandle = IFCInstanceExporter.CreateGroup(file, GUIDUtil.CreateGUID(),
                     ownerHistory, zoneGroup.Key, null, null);
                  IFCInstanceExporter.CreateRelAssignsToGroup(file, GUIDUtil.CreateGUID(), ownerHistory,
                      relAssignsToZoneGroupName, null, zoneGroup.Value, null, zoneGroupHandle);
               }
            }

            // create Space Occupants
            {
               foreach (string spaceOccupantName in ExporterCacheManager.SpaceOccupantInfoCache.Keys)
               {
                  SpaceOccupantInfo spaceOccupantInfo = ExporterCacheManager.SpaceOccupantInfoCache.Find(spaceOccupantName);
                  if (spaceOccupantInfo != null)
                  {
                     IFCAnyHandle person = IFCInstanceExporter.CreatePerson(file, null, spaceOccupantName, null, null, null, null, null, null);
                     IFCAnyHandle spaceOccupantHandle = IFCInstanceExporter.CreateOccupant(file, GUIDUtil.CreateGUID(),
                         ownerHistory, null, null, spaceOccupantName, person, IFCOccupantType.NotDefined);
                     IFCInstanceExporter.CreateRelOccupiesSpaces(file, GUIDUtil.CreateGUID(), ownerHistory,
                         null, null, spaceOccupantInfo.RoomHandles, null, spaceOccupantHandle, null);

                     HashSet<IFCAnyHandle> spaceOccupantHandles = new HashSet<IFCAnyHandle>();
                     spaceOccupantHandles.Add(spaceOccupantHandle);

                     foreach (KeyValuePair<string, IFCAnyHandle> classificationReference in spaceOccupantInfo.ClassificationReferences)
                     {
                        IFCAnyHandle relAssociates = IFCInstanceExporter.CreateRelAssociatesClassification(file, GUIDUtil.CreateGUID(),
                            ownerHistory, classificationReference.Key, "", spaceOccupantHandles, classificationReference.Value);
                     }

                     if (spaceOccupantInfo.SpaceOccupantProperySetHandle != null && spaceOccupantInfo.SpaceOccupantProperySetHandle.HasValue)
                     {
                        ExporterUtil.CreateRelDefinesByProperties(file, GUIDUtil.CreateGUID(),
                                                          ownerHistory, null, null, spaceOccupantHandles, spaceOccupantInfo.SpaceOccupantProperySetHandle);
                     }
                  }
               }
            }

            // Create systems.
            using (ProductWrapper productWrapper = ProductWrapper.Create(exporterIFC, true))
            {
               foreach (KeyValuePair<ElementId, ISet<IFCAnyHandle>> system in ExporterCacheManager.SystemsCache.BuiltInSystemsCache)
               {
                  MEPSystem systemElem = document.GetElement(system.Key) as MEPSystem;
                  if (systemElem == null)
                     continue;

                  Element baseEquipment = systemElem.BaseEquipment;
                  if (baseEquipment != null)
                  {
                     IFCAnyHandle memberHandle = ExporterCacheManager.MEPCache.Find(baseEquipment.Id);
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(memberHandle))
                        system.Value.Add(memberHandle);
                  }

                  ElementType systemElemType = document.GetElement(systemElem.GetTypeId()) as ElementType;
                  string name = NamingUtil.GetNameOverride(systemElem, systemElem.Name);
                  string desc = NamingUtil.GetDescriptionOverride(systemElem, null);
                  string objectType = NamingUtil.GetObjectTypeOverride(systemElem,
                      (systemElemType != null) ? systemElemType.Name : "");

                  string systemGUID = GUIDUtil.CreateGUID(systemElem);
                  IFCAnyHandle systemHandle = IFCInstanceExporter.CreateSystem(file, systemGUID,
                      ownerHistory, name, desc, objectType);

                  // Create classification reference when System has classification filed name assigned to it
                  ClassificationUtil.CreateClassification(exporterIFC, file, systemElem, systemHandle);

                  productWrapper.AddSystem(systemElem, systemHandle);

                  if (projectHasBuilding)
                  {
                     CreateRelServicesBuildings(buildingHandle, file, ownerHistory, systemHandle);
                  }

                  IFCObjectType? objType = null;
                  if (!ExporterCacheManager.ExportOptionsCache.ExportAsCoordinationView2)
                     objType = IFCObjectType.Product;
                  IFCAnyHandle relAssignsToGroup = IFCInstanceExporter.CreateRelAssignsToGroup(file, GUIDUtil.CreateGUID(),
                      ownerHistory, null, null, system.Value, objType, systemHandle);
               }
            }

            using (ProductWrapper productWrapper = ProductWrapper.Create(exporterIFC, true))
            {
               foreach (KeyValuePair<ElementId, ISet<IFCAnyHandle>> entries in ExporterCacheManager.SystemsCache.ElectricalSystemsCache)
               {
                  ElementId systemId = entries.Key;
                  MEPSystem systemElem = document.GetElement(systemId) as MEPSystem;
                  if (systemElem == null)
                     continue;

                  Element baseEquipment = systemElem.BaseEquipment;
                  if (baseEquipment != null)
                  {
                     IFCAnyHandle memberHandle = ExporterCacheManager.MEPCache.Find(baseEquipment.Id);
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(memberHandle))
                        entries.Value.Add(memberHandle);
                  }

                  // The Elements property below can throw an InvalidOperationException in some cases, which could
                  // crash the export.  Protect against this without having too generic a try/catch block.
                  try
                  {
                     ElementSet members = systemElem.Elements;
                     foreach (Element member in members)
                     {
                        IFCAnyHandle memberHandle = ExporterCacheManager.MEPCache.Find(member.Id);
                        if (!IFCAnyHandleUtil.IsNullOrHasNoValue(memberHandle))
                           entries.Value.Add(memberHandle);
                     }
                  }
                  catch
                  {
                  }

                  if (entries.Value.Count == 0)
                     continue;

                  ElementType systemElemType = document.GetElement(systemElem.GetTypeId()) as ElementType;
                  string name = NamingUtil.GetNameOverride(systemElem, systemElem.Name);
                  string desc = NamingUtil.GetDescriptionOverride(systemElem, null);
                  string objectType = NamingUtil.GetObjectTypeOverride(systemElem,
                      (systemElemType != null) ? systemElemType.Name : "");

                  string systemGUID = GUIDUtil.CreateGUID(systemElem);
                  IFCAnyHandle systemHandle = IFCInstanceExporter.CreateSystem(file,
                      systemGUID, ownerHistory, name, desc, objectType);

                  // Create classification reference when System has classification filed name assigned to it
                  ClassificationUtil.CreateClassification(exporterIFC, file, systemElem, systemHandle);

                  productWrapper.AddSystem(systemElem, systemHandle);

                  if (projectHasBuilding)
                     CreateRelServicesBuildings(buildingHandle, file, ownerHistory, systemHandle);

                  IFCObjectType? objType = null;
                  if (!ExporterCacheManager.ExportOptionsCache.ExportAsCoordinationView2)
                     objType = IFCObjectType.Product;
                  IFCAnyHandle relAssignsToGroup = IFCInstanceExporter.CreateRelAssignsToGroup(file, GUIDUtil.CreateGUID(),
                      ownerHistory, null, null, entries.Value, objType, systemHandle);
               }
            }

            // Add presentation layer assignments - this is in addition to those created internally.
            // Any representation in this list will override any internal assignment.
            CreatePresentationLayerAssignments(exporterIFC, file);

            // Add door/window openings.
            ExporterCacheManager.DoorWindowDelayedOpeningCreatorCache.ExecuteCreators(exporterIFC, document);

            foreach (SpaceInfo spaceInfo in ExporterCacheManager.SpaceInfoCache.SpaceInfos.Values)
            {
               if (spaceInfo.RelatedElements.Count > 0)
               {
                  IFCInstanceExporter.CreateRelContainedInSpatialStructure(file, GUIDUtil.CreateGUID(), ownerHistory,
                      null, null, spaceInfo.RelatedElements, spaceInfo.SpaceHandle);
               }
            }

            // Delete handles that are marked for removal
            foreach (IFCAnyHandle handleToDel in ExporterCacheManager.HandleToDeleteCache)
            {
               handleToDel.Delete();
            }

            // Potentially modify elements with GUID values.
            if (ExporterCacheManager.GUIDsToStoreCache.Count > 0 && !ExporterCacheManager.ExportOptionsCache.ExportingLink)
            {
               using (SubTransaction st = new SubTransaction(document))
               {
                  st.Start();
                  foreach (KeyValuePair<KeyValuePair<Element, BuiltInParameter>, string> elementAndGUID in ExporterCacheManager.GUIDsToStoreCache)
                  {
                     if (elementAndGUID.Key.Key == null || elementAndGUID.Key.Value == BuiltInParameter.INVALID || elementAndGUID.Value == null)
                        continue;

                     ParameterUtil.SetStringParameter(elementAndGUID.Key.Key, elementAndGUID.Key.Value, elementAndGUID.Value);
                  }
                  st.Commit();
               }
            }

            // Allow native code to remove some unused handles and clear internal caches.
            ExporterIFCUtils.EndExportInternal(exporterIFC);
            transaction.Commit();
         }
      }

      /// <summary>
      /// Write out the IFC file after all entities have been created.
      /// </summary>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      /// <param name="document">The document to export.</param>
      private void WriteIFCFile(ExporterIFC exporterIFC, Document document)
      {
         ProjectInfo projectInfo = document.ProjectInformation;
         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            //create header

            ExportOptionsCache exportOptionsCache = ExporterCacheManager.ExportOptionsCache;

            string coordinationView = null;
            if (exportOptionsCache.ExportAsCoordinationView2)
               coordinationView = "CoordinationView_V2.0";
            else
               coordinationView = "CoordinationView";

            List<string> descriptions = new List<string>();
            if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
            {
               descriptions.Add("IFC2X_PLATFORM");
            }
            else if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
            {
               descriptions.Add("ViewDefinition [ReferenceView_V1.2]");
            }
            else if (ExporterCacheManager.ExportOptionsCache.ExportAs4DesignTransferView)
            {
               descriptions.Add("ViewDefinition [DesignTransferView_V1.0]");   // Tentative name as the standard is not yet fuly released
            }
            else
            {
               string currentLine;
               if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3COBIE24DesignDeliverable)
               {
                  currentLine = string.Format("ViewDefinition [{0}]",
                     "COBie2.4DesignDeliverable");
               }
               else
               {
                  currentLine = string.Format("ViewDefinition [{0}{1}]",
                     coordinationView,
                     exportOptionsCache.ExportBaseQuantities ? ", QuantityTakeOffAddOnView" : "");
               }

               descriptions.Add(currentLine);

            }
            if (!string.IsNullOrEmpty(ExporterCacheManager.ExportOptionsCache.ExcludeFilter))
            {
               descriptions.Add("Options [Excluded Entities: " + ExporterCacheManager.ExportOptionsCache.ExcludeFilter + "]");
            }

            string projectNumber = (projectInfo != null) ? projectInfo.Number : null;
            string projectName = (projectInfo != null) ? projectInfo.Name : null;
            string projectStatus = (projectInfo != null) ? projectInfo.Status : null;

            if (projectNumber == null)
               projectNumber = string.Empty;
            if (projectName == null)
               projectName = exportOptionsCache.FileName;
            if (projectStatus == null)
               projectStatus = string.Empty;

            IFCAnyHandle project = ExporterCacheManager.ProjectHandle;
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(project))
               IFCAnyHandleUtil.UpdateProject(project, projectNumber, projectName, projectStatus);

            IFCInstanceExporter.CreateFileSchema(file);

            // Get stored File Header information from the UI and use it for export
            IFCFileHeaderItem fHItem = ExporterCacheManager.ExportOptionsCache.FileHeaderItem;

            // Add information in the File Description (e.g. Exchange Requirement) that is assigned in the UI
            if (!string.IsNullOrEmpty(fHItem.FileDescription))
               descriptions.Add(fHItem.FileDescription);
            IFCInstanceExporter.CreateFileDescription(file, descriptions);

            List<string> author = new List<string>();
            if (String.IsNullOrEmpty(fHItem.AuthorName) == false)
            {
               author.Add(fHItem.AuthorName);
               if (String.IsNullOrEmpty(fHItem.AuthorEmail) == false)
                  author.Add(fHItem.AuthorEmail);
            }
            else
               author.Add(String.Empty);

            List<string> organization = new List<string>();
            if (String.IsNullOrEmpty(fHItem.Organization) == false)
               organization.Add(fHItem.Organization);
            else
               organization.Add(String.Empty);

            string versionInfos = document.Application.VersionBuild + " - " + ExporterCacheManager.ExportOptionsCache.ExporterVersion + " - " + ExporterCacheManager.ExportOptionsCache.ExporterUIVersion;

            if (fHItem.Authorization == null)
               fHItem.Authorization = String.Empty;

            IFCInstanceExporter.CreateFileName(file, projectNumber, author, organization, document.Application.VersionName,
                versionInfos, fHItem.Authorization);

            transaction.Commit();

            IFCFileWriteOptions writeOptions = new IFCFileWriteOptions();
            writeOptions.FileName = exportOptionsCache.FileName;
            writeOptions.FileFormat = exportOptionsCache.IFCFileFormat;
            if (writeOptions.FileFormat == IFCFileFormat.IfcXML || writeOptions.FileFormat == IFCFileFormat.IfcXMLZIP)
            {
               writeOptions.XMLConfigFileName = Path.Combine(DirectoryUtil.RevitProgramPath, "EDM\\ifcXMLconfiguration.xml");
            }

            // Reuse almost all of the information above to write out extra copies of the IFC file.
            if (exportOptionsCache.ExportingLink)
            {
               IFCAnyHandle buildingOrSiteHnd = ExporterCacheManager.BuildingHandle;
               if (IFCAnyHandleUtil.IsNullOrHasNoValue(buildingOrSiteHnd))
               {
                  buildingOrSiteHnd = ExporterCacheManager.SiteHandle;
               }

               IFCAnyHandle buildingOrSitePlacement = IFCAnyHandleUtil.GetObjectPlacement(buildingOrSiteHnd);

               int numRevitLinkInstances = exportOptionsCache.GetNumLinkInstanceInfos();
               for (int ii = 0; ii < numRevitLinkInstances; ii++)
               {
                  Transform linkTrf = ExporterCacheManager.ExportOptionsCache.GetLinkInstanceTransform(ii);
                  IFCAnyHandle relativePlacement = ExporterUtil.CreateAxis2Placement3D(file, linkTrf.Origin, linkTrf.BasisZ, linkTrf.BasisX);

                  // Note that we overwrite this here for subsequent writes, which clobbers the
                  // original placement, so the IfcBuilding handle is suspect after this without
                  // explicit cleanup.
                  GeometryUtil.SetRelativePlacement(buildingOrSitePlacement, relativePlacement);

                  string linkInstanceFileName = exportOptionsCache.GetLinkInstanceFileName(ii);
                  if (linkInstanceFileName != null)
                     writeOptions.FileName = linkInstanceFileName;

                  file.Write(writeOptions);
               }
            }
            else
            {
               file.Write(writeOptions);
            }

            // Display the message to the user when the IFC File has been completely exported 
            statusBar.Set(Resources.IFCExportComplete);
         }
      }

      private string GetLanguageExtension(LanguageType langType)
      {
         switch (langType)
         {
            case LanguageType.English_USA:
               return " (ENU)";
            case LanguageType.German:
               return " (DEU)";
            case LanguageType.Spanish:
               return " (ESP)";
            case LanguageType.French:
               return " (FRA)";
            case LanguageType.Italian:
               return " (ITA)";
            case LanguageType.Dutch:
               return " (NLD)";
            case LanguageType.Chinese_Simplified:
               return " (CHS)";
            case LanguageType.Chinese_Traditional:
               return " (CHT)";
            case LanguageType.Japanese:
               return " (JPN)";
            case LanguageType.Korean:
               return " (KOR)";
            case LanguageType.Russian:
               return " (RUS)";
            case LanguageType.Czech:
               return " (CSY)";
            case LanguageType.Polish:
               return " (PLK)";
            case LanguageType.Hungarian:
               return " (HUN)";
            case LanguageType.Brazilian_Portuguese:
               return " (PTB)";
            case LanguageType.English_GB:
               return " (ENG)";
            default:
               return "";
         }
      }

      private long GetCreationDate(Document document)
      {
         string pathName = document.PathName;
         // If this is not a locally saved file, we can't get the creation date.
         // This will require future work to get this, but it is very minor.
         DateTime creationTimeUtc = DateTime.Now;
         try
         {
            FileInfo fileInfo = new FileInfo(pathName);
            creationTimeUtc = fileInfo.CreationTimeUtc;
         }
         catch
         {
            creationTimeUtc = DateTime.Now;
         }

         // The IfcTimeStamp is measured in seconds since 1/1/1970.  As such, we divide by 10,000,000 
         // (100-ns ticks in a second) and subtract the 1/1/1970 offset.
         return creationTimeUtc.ToFileTimeUtc() / 10000000 - 11644473600;
      }

      /// <summary>
      /// Creates the application information.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="app">The application object.</param>
      /// <returns>The handle of IFC file.</returns>
      private IFCAnyHandle CreateApplicationInformation(IFCFile file, Document document)
      {
         Application app = document.Application;
         string pathName = document.PathName;
         LanguageType langType = ExporterCacheManager.LanguageType;
         string languageExtension = GetLanguageExtension(langType);
         string productFullName = app.VersionName + languageExtension;
         string productVersion = app.VersionNumber;
         string productIdentifier = "Revit";

         IFCAnyHandle developer = IFCInstanceExporter.CreateOrganization(file, null, productFullName, null, null, null);
         IFCAnyHandle application = IFCInstanceExporter.CreateApplication(file, developer, productVersion, productFullName, productIdentifier);
         return application;
      }

      /// <summary>
      /// Creates the 3D and 2D contexts information.
      /// </summary>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      /// <param name="doc">The document provides the ProjectLocation.</param>
      /// <returns>The collection contains the 3D/2D context (not sub-context) handles of IFC file.</returns>
      private HashSet<IFCAnyHandle> CreateContextInformation(ExporterIFC exporterIFC, Document doc, out IList<double> directionRatios)
      {
         HashSet<IFCAnyHandle> repContexts = new HashSet<IFCAnyHandle>();

         // Make sure this precision value is in an acceptable range.
         double initialPrecision = doc.Application.VertexTolerance / 10.0;
         initialPrecision = Math.Min(initialPrecision, 1e-3);
         initialPrecision = Math.Max(initialPrecision, 1e-8);

         double scaledPrecision = UnitUtil.ScaleLength(initialPrecision);
         int exponent = Convert.ToInt32(Math.Log10(scaledPrecision));
         double precision = Math.Pow(10.0, exponent);

         IFCFile file = exporterIFC.GetFile();
         SiteTransformBasis transformBasis = ExporterCacheManager.ExportOptionsCache.SiteTransformation;

         double trueNorthAngleInRadians = 0;
         IFCAnyHandle wcs = null;

         switch (transformBasis)
         {
            case SiteTransformBasis.Shared:
         {
            if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
            {
               IFCAnyHandle wcsOrigin = ExporterCacheManager.Global3DOriginHandle;
               wcs = IFCInstanceExporter.CreateAxis2Placement3D(file, wcsOrigin, null, null);
            }
                  break;
         }
            case SiteTransformBasis.Project:
            case SiteTransformBasis.Site:
         {
            ExporterUtil.GetSafeProjectPositionAngle(doc, out trueNorthAngleInRadians);
            ProjectLocation projLocation = doc.ActiveProjectLocation;
                  Transform siteSharedCoordinatesTrf =
                     (projLocation == null || !ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4) ?
                        Transform.Identity : projLocation.GetTransform().Inverse;
                  XYZ unscaledOrigin = XYZ.Zero;
            if (transformBasis == SiteTransformBasis.Project)
            {
               BasePoint prjBasePoint = BasePoint.GetProjectBasePoint(doc);
               if (prjBasePoint != null)
               {
                  BoundingBoxXYZ bbox = prjBasePoint.get_BoundingBox(null);
                  unscaledOrigin = bbox.Min;
               }
            }
            unscaledOrigin = siteSharedCoordinatesTrf.OfPoint(unscaledOrigin);
            XYZ orig = UnitUtil.ScaleLength(unscaledOrigin);
            wcs = ExporterUtil.CreateAxis2Placement3D(file, orig, siteSharedCoordinatesTrf.BasisZ, siteSharedCoordinatesTrf.BasisX);
                  break;
               }
         }

         // This covers Internal case, and Shared case for IFC4+.  
         // NOTE: If new cases appear, they should be covered above.
         if (wcs == null)
         {
            wcs = ExporterUtil.CreateAxis2Placement3D(file, XYZ.Zero, null, null);
         }

         // CoordinationView2.0 requires that we always export true north, even if it is the same as project north.
         IFCAnyHandle trueNorth = null;
         {
            double trueNorthAngleConverted = -trueNorthAngleInRadians + Math.PI / 2.0;
            directionRatios = new List<Double>(2) { Math.Cos(trueNorthAngleConverted), Math.Sin(trueNorthAngleConverted) };
            trueNorth = IFCInstanceExporter.CreateDirection(file, directionRatios);
         }

         int dimCount = 3;
         IFCAnyHandle context3D = IFCInstanceExporter.CreateGeometricRepresentationContext(file, null,
             "Model", dimCount, precision, wcs, trueNorth);
         // CoordinationView2.0 requires sub-contexts of "Axis", "Body", and "Box".  We will use these for regular export also.
         {
            IFCAnyHandle context3DAxis = IFCInstanceExporter.CreateGeometricRepresentationSubContext(file,
                "Axis", "Model", context3D, null, Toolkit.IFCGeometricProjection.Graph_View, null);
            IFCAnyHandle context3DBody = IFCInstanceExporter.CreateGeometricRepresentationSubContext(file,
                "Body", "Model", context3D, null, Toolkit.IFCGeometricProjection.Model_View, null);
            IFCAnyHandle context3DBox = IFCInstanceExporter.CreateGeometricRepresentationSubContext(file,
                "Box", "Model", context3D, null, Toolkit.IFCGeometricProjection.Model_View, null);
            IFCAnyHandle context3DFootPrint = IFCInstanceExporter.CreateGeometricRepresentationSubContext(file,
                "FootPrint", "Model", context3D, null, Toolkit.IFCGeometricProjection.Model_View, null);

            exporterIFC.Set3DContextHandle(context3DAxis, "Axis");
            exporterIFC.Set3DContextHandle(context3DBody, "Body");
            exporterIFC.Set3DContextHandle(context3DBox, "Box");
            exporterIFC.Set3DContextHandle(context3DFootPrint, "FootPrint");
         }

         exporterIFC.Set3DContextHandle(context3D, "");
         repContexts.Add(context3D); // Only Contexts in list, not sub-contexts.

         // Create IFCMapConversion information for the context
         ExportIFCMapConversion(exporterIFC, doc, context3D, directionRatios);

         if (ExporterCacheManager.ExportOptionsCache.ExportAnnotations)
         {
            IFCAnyHandle context2DHandle = IFCInstanceExporter.CreateGeometricRepresentationContext(file,
                null, "Plan", dimCount, precision, wcs, trueNorth);

            IFCAnyHandle context2D = IFCInstanceExporter.CreateGeometricRepresentationSubContext(file,
                null, "Annotation", context2DHandle, 0.01, IFCGeometricProjection.Plan_View, null);

            exporterIFC.Set2DContextHandle(context2D);
            repContexts.Add(context2DHandle); // Only Contexts in list, not sub-contexts.
         }

         return repContexts;
      }

      private void GetCOBieContactInfo(IFCFile file, Document doc)
      {
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3ExtendedFMHandoverView)
         {
            try
            {
               string CObieContactXML = Path.GetDirectoryName(doc.PathName) + @"\" + Path.GetFileNameWithoutExtension(doc.PathName) + @"_COBieContact.xml";
               string category = null, company = null, department = null, organizationCode = null, contactFirstName = null, contactFamilyName = null,
                   postalBox = null, town = null, stateRegion = null, postalCode = null, country = null;

               using (XmlReader reader = XmlReader.Create(CObieContactXML))
               {
                  IList<string> eMailAddressList = new List<string>();
                  IList<string> telNoList = new List<string>();
                  IList<string> addressLines = new List<string>();

                  while (reader.Read())
                  {
                     if (reader.IsStartElement())
                     {
                        while (reader.Read())
                        {
                           switch (reader.Name)
                           {
                              case "Email":
                                 eMailAddressList.Add(reader.ReadString());
                                 break;
                              case "Classification":
                                 category = reader.ReadString();
                                 break;
                              case "Company":
                                 company = reader.ReadString();
                                 break;
                              case "Phone":
                                 telNoList.Add(reader.ReadString());
                                 break;
                              case "Department":
                                 department = reader.ReadString();
                                 break;
                              case "OrganizationCode":
                                 organizationCode = reader.ReadString();
                                 break;
                              case "FirstName":
                                 contactFirstName = reader.ReadString();
                                 break;
                              case "LastName":
                                 contactFamilyName = reader.ReadString();
                                 break;
                              case "Street":
                                 addressLines.Add(reader.ReadString());
                                 break;
                              case "POBox":
                                 postalBox = reader.ReadString();
                                 break;
                              case "Town":
                                 town = reader.ReadString();
                                 break;
                              case "State":
                                 stateRegion = reader.ReadString();
                                 break;
                              case "Zip":
                                 category = reader.ReadString();
                                 break;
                              case "Country":
                                 country = reader.ReadString();
                                 break;
                              case "Contact":
                                 if (reader.IsStartElement()) break;     // Do nothing at the start tag, process when it is the end
                                 CreateContact(file, category, company, department, organizationCode, contactFirstName,
                                     contactFamilyName, postalBox, town, stateRegion, postalCode, country,
                                     eMailAddressList, telNoList, addressLines);
                                 // reset variables
                                 {
                                    category = null;
                                    company = null;
                                    department = null;
                                    organizationCode = null;
                                    contactFirstName = null;
                                    contactFamilyName = null;
                                    postalBox = null;
                                    town = null;
                                    stateRegion = null;
                                    postalCode = null;
                                    country = null;
                                    eMailAddressList.Clear();
                                    telNoList.Clear();
                                    addressLines.Clear();
                                 }
                                 break;
                              default:
                                 break;
                           }
                        }
                     }
                  }
               }
            }
            catch
            {
               // Can't find the XML file, ignore the whole process and continue
            }
         }
      }

      /// <summary>
      /// Creates the IfcProject.
      /// </summary>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      /// <param name="doc">The document provides the owner information.</param>
      /// <param name="application">The handle of IFC file to create the owner history.</param>
      private void CreateProject(ExporterIFC exporterIFC, Document doc, IFCAnyHandle application)
      {
         string familyName;
         string givenName;
         List<string> middleNames;
         List<string> prefixTitles;
         List<string> suffixTitles;

         string author = String.Empty;
         ProjectInfo projectInfo = doc.ProjectInformation;
         if (projectInfo != null)
         {
            try
            {
               author = projectInfo.Author;
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException)
            {
               //if failed to get author from project info, try to get the username from application later.
            }
         }

         if (String.IsNullOrEmpty(author))
         {
            author = doc.Application.Username;
         }

         NamingUtil.ParseName(author, out familyName, out givenName, out middleNames, out prefixTitles, out suffixTitles);

         IFCFile file = exporterIFC.GetFile();
         int creationDate = (int)GetCreationDate(doc);
         IFCAnyHandle ownerHistory = null;
         IFCAnyHandle person = null;
         IFCAnyHandle organization = null;

         COBieCompanyInfo cobieCompInfo = ExporterCacheManager.ExportOptionsCache.COBieCompanyInfo;

         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3COBIE24DesignDeliverable && cobieCompInfo != null)
         {
            IFCAnyHandle postalAddress = IFCInstanceExporter.CreatePostalAddress(file, null, null, null, null, new List<string>() { cobieCompInfo.StreetAddress },
               null, cobieCompInfo.City, cobieCompInfo.State_Region, cobieCompInfo.PostalCode, cobieCompInfo.Country);
            IFCAnyHandle telecomAddress = IFCInstanceExporter.CreateTelecomAddress(file, null, null, null, new List<string>() { cobieCompInfo.CompanyPhone },
               null, null, new List<string>() { cobieCompInfo.CompanyEmail }, null);

            organization = IFCInstanceExporter.CreateOrganization(file, null, cobieCompInfo.CompanyName, null,
                null, new List<IFCAnyHandle>() { postalAddress, telecomAddress });
            person = IFCInstanceExporter.CreatePerson(file, null, null, null, null, null, null, null, null);
         }
         else
         {
            IFCAnyHandle telecomAddress = GetTelecomAddressFromExtStorage(file, doc);
            IList<IFCAnyHandle> telecomAddresses = null;
            if (telecomAddress != null)
            {
               telecomAddresses = new List<IFCAnyHandle>();
               telecomAddresses.Add(telecomAddress);
            }

            person = IFCInstanceExporter.CreatePerson(file, null, familyName, givenName, middleNames,
                prefixTitles, suffixTitles, null, telecomAddresses);

            string organizationName = null;
            string organizationDescription = null;
            if (projectInfo != null)
            {
               try
               {
                  organizationName = projectInfo.OrganizationName;
                  organizationDescription = projectInfo.OrganizationDescription;
               }
               catch (Autodesk.Revit.Exceptions.InvalidOperationException)
               {
               }
            }

            organization = IFCInstanceExporter.CreateOrganization(file, null, organizationName, organizationDescription, null, null);
         }

         IFCAnyHandle owningUser = IFCInstanceExporter.CreatePersonAndOrganization(file, person, organization, null);
         ownerHistory = IFCInstanceExporter.CreateOwnerHistory(file, owningUser, application, null,
            Toolkit.IFCChangeAction.NoChange, null, null, null, creationDate);

         exporterIFC.SetOwnerHistoryHandle(ownerHistory);    // For use by native code only.
         ExporterCacheManager.OwnerHistoryHandle = ownerHistory;

         // Getting contact information from Revit extensible storage that COBie extension tool creates
         GetCOBieContactInfo(file, doc);

         IFCAnyHandle units = CreateDefaultUnits(exporterIFC, doc);
         IList<double> directionRatios = null;
         HashSet<IFCAnyHandle> repContexts = CreateContextInformation(exporterIFC, doc, out directionRatios);

         string projectName = null;
         string projectLongName = null;
         string projectDescription = null;
         string projectPhase = null;

         COBieProjectInfo cobieProjInfo = ExporterCacheManager.ExportOptionsCache.COBieProjectInfo;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3COBIE24DesignDeliverable && cobieProjInfo != null)
         {
            projectName = cobieProjInfo.ProjectName;
            projectDescription = cobieProjInfo.ProjectDescription;
            projectPhase = cobieProjInfo.ProjectPhase;
         }
         else
         {
            // As per IFC implementer's agreement, we get IfcProject.Name from ProjectInfo.Number and IfcProject.Longname from ProjectInfo.Name 
            projectName = (projectInfo != null) ? projectInfo.Number : null;
            projectLongName = (projectInfo != null) ? projectInfo.Name : null;

            // Get project description if it is set in the Project info
            projectDescription = (projectInfo != null) ? NamingUtil.GetDescriptionOverride(projectInfo, null) : null;

            if (projectInfo != null)
               ParameterUtil.GetStringValueFromElement(projectInfo, "Project Phase", out projectPhase);
         }

         string projectGUID = GUIDUtil.CreateProjectLevelGUID(doc, IFCProjectLevelGUIDType.Project);
         IFCAnyHandle projectHandle = IFCInstanceExporter.CreateProject(exporterIFC, projectInfo, projectGUID, ownerHistory,
            projectName, projectDescription, projectLongName, projectPhase, repContexts, units);
         ExporterCacheManager.ProjectHandle = projectHandle;


         if (projectInfo != null)
         {
            using (ProductWrapper productWrapper = ProductWrapper.Create(exporterIFC, true))
            {
               productWrapper.AddProject(projectInfo, projectHandle);
               ExporterUtil.ExportRelatedProperties(exporterIFC, projectInfo, productWrapper);
            }
         }
         if (ExporterCacheManager.ExportOptionsCache.ExportAsCOBIE)
         {
            HashSet<IFCAnyHandle> projectHandles = new HashSet<IFCAnyHandle>();
            projectHandles.Add(projectHandle);
            string clientName = (projectInfo != null) ? projectInfo.ClientName : String.Empty;
            IFCAnyHandle clientOrg = IFCInstanceExporter.CreateOrganization(file, null, clientName, null, null, null);
            IFCAnyHandle actor = IFCInstanceExporter.CreateActor(file, GUIDUtil.CreateGUID(), ownerHistory, null, null, null, clientOrg);
            IFCInstanceExporter.CreateRelAssignsToActor(file, GUIDUtil.CreateGUID(), ownerHistory, "Project Client/Owner", null, projectHandles, null, actor, null);

            IFCAnyHandle architectActor = IFCInstanceExporter.CreateActor(file, GUIDUtil.CreateGUID(), ownerHistory, null, null, null, person);
            IFCInstanceExporter.CreateRelAssignsToActor(file, GUIDUtil.CreateGUID(), ownerHistory, "Project Architect", null, projectHandles, null, architectActor, null);
         }
      }

      private void CreateContact(IFCFile file, string category, string company, string department, string organizationCode, string contactFirstName,
          string contactFamilyName, string postalBox, string town, string stateRegion, string postalCode, string country,
          IList<string> eMailAddressList, IList<string> telNoList, IList<string> addressLines)
      {
         IFCAnyHandle contactTelecomAddress = IFCInstanceExporter.CreateTelecomAddress(file, null, null, null, telNoList, null, null, eMailAddressList, null);
         IFCAnyHandle contactPostalAddress = IFCInstanceExporter.CreatePostalAddress(file, null, null, null, department, addressLines, postalBox, town, stateRegion,
                 postalCode, country);
         IList<IFCAnyHandle> contactAddresses = new List<IFCAnyHandle>();
         contactAddresses.Add(contactTelecomAddress);
         contactAddresses.Add(contactPostalAddress);
         IFCAnyHandle contactPerson = IFCInstanceExporter.CreatePerson(file, null, contactFamilyName, contactFirstName, null,
             null, null, null, contactAddresses);
         IFCAnyHandle contactOrganization = IFCInstanceExporter.CreateOrganization(file, organizationCode, company, null,
             null, null);
         IFCAnyHandle actorRole = IFCInstanceExporter.CreateActorRole(file, "UserDefined", category, null);
         IList<IFCAnyHandle> actorRoles = new List<IFCAnyHandle>();
         actorRoles.Add(actorRole);
         IFCAnyHandle contactEntry = IFCInstanceExporter.CreatePersonAndOrganization(file, contactPerson, contactOrganization, actorRoles);
      }

      private IFCAnyHandle GetTelecomAddressFromExtStorage(IFCFile file, Document document)
      {
         IFCFileHeaderItem fHItem = ExporterCacheManager.ExportOptionsCache.FileHeaderItem;
         if (!String.IsNullOrEmpty(fHItem.AuthorEmail))
         {
            IList<string> electronicMailAddress = new List<string>();
            electronicMailAddress.Add(fHItem.AuthorEmail);
            return IFCInstanceExporter.CreateTelecomAddress(file, null, null, null, null, null, null, electronicMailAddress, null);
         }

         return null;
      }

      /// <summary>
      /// Create IFC Address from the saved data obtained by the UI and saved in the extensible storage
      /// </summary>
      /// <param name="file"></param>
      /// <param name="document"></param>
      /// <returns>The handle of IFC file.</returns>
      static public IFCAnyHandle CreateIFCAddressFromExtStorage(IFCFile file, Document document)
      {
         IFCAddress savedAddress = new IFCAddress();
         IFCAddressItem savedAddressItem;

         if (savedAddress.GetSavedAddress(document, out savedAddressItem) == true)
         {
            if (!savedAddressItem.HasData())
               return null;

            IFCAnyHandle postalAddress;

            // We have address saved in the extensible storage
            List<string> addressLines = null;
            if (!String.IsNullOrEmpty(savedAddressItem.AddressLine1))
            {
               addressLines = new List<string>();

               addressLines.Add(savedAddressItem.AddressLine1);
               if (!String.IsNullOrEmpty(savedAddressItem.AddressLine2))
                  addressLines.Add(savedAddressItem.AddressLine2);
            }

            IFCAddressType? addressPurpose = null;
            if (!String.IsNullOrEmpty(savedAddressItem.Purpose))
            {
               addressPurpose = IFCAddressType.UserDefined;     // set this as default value
               if (String.Compare(savedAddressItem.Purpose, "OFFICE", true) == 0)
                  addressPurpose = Toolkit.IFCAddressType.Office;
               else if (String.Compare(savedAddressItem.Purpose, "SITE", true) == 0)
                  addressPurpose = Toolkit.IFCAddressType.Site;
               else if (String.Compare(savedAddressItem.Purpose, "HOME", true) == 0)
                  addressPurpose = Toolkit.IFCAddressType.Home;
               else if (String.Compare(savedAddressItem.Purpose, "DISTRIBUTIONPOINT", true) == 0)
                  addressPurpose = Toolkit.IFCAddressType.DistributionPoint;
               else if (String.Compare(savedAddressItem.Purpose, "USERDEFINED", true) == 0)
                  addressPurpose = Toolkit.IFCAddressType.UserDefined;
            }

            postalAddress = IFCInstanceExporter.CreatePostalAddress(file, addressPurpose, savedAddressItem.Description, savedAddressItem.UserDefinedPurpose,
               savedAddressItem.InternalLocation, addressLines, savedAddressItem.POBox, savedAddressItem.TownOrCity, savedAddressItem.RegionOrState, savedAddressItem.PostalCode,
               savedAddressItem.Country);

            return postalAddress;
         }

         return null;
      }

      /// <summary>
      /// Check whether there is address information that is not empty and needs to be created for IfcSite
      /// </summary>
      /// <param name="document">the document</param>
      /// <returns>true if address is to be created for the site</returns>
      static public bool NeedToCreateAddressForSite(Document document)
      {
         IFCAddress savedAddress = new IFCAddress();
         IFCAddressItem savedAddressItem;
         if (savedAddress.GetSavedAddress(document, out savedAddressItem) == true)
         {
            // Return the selection checkbox regardless whether it has data. It will be checked before the creaton of the postal address later
            return savedAddressItem.AssignAddressToSite;
         }
         return false;  //default not creating site address if not set in the ui
      }

      /// <summary>
      /// Check whether there is address information that is not empty and needs to be created for IfcBuilding
      /// </summary>
      /// <param name="document">the document</param>
      /// <returns>true if address is to be created for the building</returns>
      static public bool NeedToCreateAddressForBuilding(Document document)
      {
         IFCAddress savedAddress = new IFCAddress();
         IFCAddressItem savedAddressItem;
         if (savedAddress.GetSavedAddress(document, out savedAddressItem) == true)
         {
            // Return the selection checkbox regardless whether it has data. It will be checked before the creaton of the postal address later
            return savedAddressItem.AssignAddressToBuilding;
         }
         return true;   //default when there is no address information from the export UI, so that it will try to get other information from project info or location
      }

      /// <summary>
      /// Creates the IfcPostalAddress, and assigns it to the file.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="address">The address string.</param>
      /// <param name="town">The town string.</param>
      /// <returns>The handle of IFC file.</returns>
      static public IFCAnyHandle CreateIFCAddress(IFCFile file, Document document, ProjectInfo projInfo)
      {
         IFCAnyHandle postalAddress = null;
         postalAddress = CreateIFCAddressFromExtStorage(file, document);
         if (postalAddress != null)
            return postalAddress;

         string projectAddress = projInfo != null ? projInfo.Address : String.Empty;
         SiteLocation siteLoc = document.ActiveProjectLocation.GetSiteLocation();
         string location = siteLoc != null ? siteLoc.PlaceName : String.Empty;

         if (projectAddress == null)
            projectAddress = String.Empty;
         if (location == null)
            location = String.Empty;

         List<string> parsedAddress = new List<string>();
         string city = String.Empty;
         string state = String.Empty;
         string postCode = String.Empty;
         string country = String.Empty;

         string parsedTown = location;
         int commaLoc = -1;
         do
         {
            commaLoc = parsedTown.IndexOf(',');
            if (commaLoc >= 0)
            {
               if (commaLoc > 0)
                  parsedAddress.Add(parsedTown.Substring(0, commaLoc));
               parsedTown = parsedTown.Substring(commaLoc + 1).TrimStart(' ');
            }
            else if (!String.IsNullOrEmpty(parsedTown))
               parsedAddress.Add(parsedTown);
         } while (commaLoc >= 0);

         int numLines = parsedAddress.Count;
         if (numLines > 0)
         {
            country = parsedAddress[numLines - 1];
            numLines--;
         }

         if (numLines > 0)
         {
            int spaceLoc = parsedAddress[numLines - 1].IndexOf(' ');
            if (spaceLoc > 0)
            {
               state = parsedAddress[numLines - 1].Substring(0, spaceLoc);
               postCode = parsedAddress[numLines - 1].Substring(spaceLoc + 1);
            }
            else
               state = parsedAddress[numLines - 1];
            numLines--;
         }

         if (numLines > 0)
         {
            city = parsedAddress[numLines - 1];
            numLines--;
         }

         List<string> addressLines = new List<string>();
         if (!String.IsNullOrEmpty(projectAddress))
            addressLines.Add(projectAddress);

         for (int ii = 0; ii < numLines; ii++)
         {
            addressLines.Add(parsedAddress[ii]);
         }

         postalAddress = IFCInstanceExporter.CreatePostalAddress(file, null, null, null,
            null, addressLines, null, city, state, postCode, country);

         return postalAddress;
      }

      private IFCAnyHandle CreateSIUnit(IFCFile file, ForgeTypeId specTypeId, IFCUnit ifcUnitType, IFCSIUnitName unitName, IFCSIPrefix? prefix, ForgeTypeId unitTypeId)
      {
         IFCAnyHandle siUnit = IFCInstanceExporter.CreateSIUnit(file, ifcUnitType, prefix, unitName);
         if (specTypeId != null && unitTypeId != null)
         {
            double scaleFactor = UnitUtils.ConvertFromInternalUnits(1.0, unitTypeId);
            ExporterCacheManager.UnitsCache.AddUnit(specTypeId, siUnit, scaleFactor, 0.0);
         }

         return siUnit;
      }

      /// <summary>
      /// Creates the IfcUnitAssignment.  This is a long list of units that we correctly translate from our internal units to known units.
      /// </summary>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      /// <param name="doc">The document provides ProjectUnit and DisplayUnitSystem.</param>
      /// <returns>The IFC handle.</returns>
      private IFCAnyHandle CreateDefaultUnits(ExporterIFC exporterIFC, Document doc)
      {
         HashSet<IFCAnyHandle> unitSet = new HashSet<IFCAnyHandle>();
         IFCFile file = exporterIFC.GetFile();
         bool exportToCOBIE = ExporterCacheManager.ExportOptionsCache.ExportAsCOBIE;

         IFCAnyHandle lenSIBaseUnit = null;
         {
            bool lenConversionBased = false;
            bool lenUseDefault = false;

            IFCUnit lenUnitType = IFCUnit.LengthUnit;
            IFCSIPrefix? lenPrefix = null;
            IFCSIUnitName lenUnitName = IFCSIUnitName.Metre;
            string lenConvName = null;

            FormatOptions lenFormatOptions = doc.GetUnits().GetFormatOptions(SpecTypeId.Length);
            ForgeTypeId lengthUnit = lenFormatOptions.GetUnitTypeId();
            if (lengthUnit.Equals(UnitTypeId.Meters) ||
               lengthUnit.Equals(UnitTypeId.MetersCentimeters))
            {
               // This space intentionally left blank
            }
            else if (lengthUnit.Equals(UnitTypeId.Centimeters))
            {
               lenPrefix = IFCSIPrefix.Centi;
            }
            else if (lengthUnit.Equals(UnitTypeId.Millimeters))
            {
               lenPrefix = IFCSIPrefix.Milli;
            }
            else if (lengthUnit.Equals(UnitTypeId.Feet) ||
               lengthUnit.Equals(UnitTypeId.FeetFractionalInches))
            {
               if (exportToCOBIE)
                  lenConvName = "foot";
               else
                  lenConvName = "FOOT";
               lenConversionBased = true;
            }
            else if (lengthUnit.Equals(UnitTypeId.FractionalInches) ||
               lengthUnit.Equals(UnitTypeId.Inches))
            {
               if (exportToCOBIE)
                  lenConvName = "inch";
               else
                  lenConvName = "INCH";
               lenConversionBased = true;
            }
            else
            {
               //Couldn't find display unit type conversion -- assuming foot
               if (exportToCOBIE)
                  lenConvName = "foot";
               else
                  lenConvName = "FOOT";
               lenConversionBased = true;
               lenUseDefault = true;
            }

            double lengthScaleFactor = UnitUtils.ConvertFromInternalUnits(1.0, lenUseDefault ? UnitTypeId.Feet : lenFormatOptions.GetUnitTypeId());
            IFCAnyHandle lenSIUnit = IFCInstanceExporter.CreateSIUnit(file, lenUnitType, lenPrefix, lenUnitName);
            if (lenPrefix == null)
               lenSIBaseUnit = lenSIUnit;
            else
               lenSIBaseUnit = IFCInstanceExporter.CreateSIUnit(file, lenUnitType, null, lenUnitName);

            if (lenConversionBased)
            {
               double lengthSIScaleFactor = UnitUtils.ConvertFromInternalUnits(1.0, UnitTypeId.Meters) / lengthScaleFactor;
               IFCAnyHandle lenDims = IFCInstanceExporter.CreateDimensionalExponents(file, 1, 0, 0, 0, 0, 0, 0); // length
               IFCAnyHandle lenConvFactor = IFCInstanceExporter.CreateMeasureWithUnit(file, Toolkit.IFCDataUtil.CreateAsRatioMeasure(lengthSIScaleFactor),
                   lenSIUnit);
               lenSIUnit = IFCInstanceExporter.CreateConversionBasedUnit(file, lenDims, lenUnitType, lenConvName, lenConvFactor);
            }

            unitSet.Add(lenSIUnit);      // created above, so unique.
            ExporterCacheManager.UnitsCache.AddUnit(SpecTypeId.Length, lenSIUnit, lengthScaleFactor, 0.0);
         }

         {
            bool areaConversionBased = false;
            bool areaUseDefault = false;

            IFCUnit areaUnitType = IFCUnit.AreaUnit;
            IFCSIPrefix? areaPrefix = null;
            IFCSIUnitName areaUnitName = IFCSIUnitName.Square_Metre;
            string areaConvName = null;

            FormatOptions areaFormatOptions = doc.GetUnits().GetFormatOptions(SpecTypeId.Area);
            ForgeTypeId areaUnit = areaFormatOptions.GetUnitTypeId();
            if (areaUnit.Equals(UnitTypeId.SquareMeters))
            {
               // This space intentionally left blank.
            }
            else if (areaUnit.Equals(UnitTypeId.SquareCentimeters))
            {
               areaPrefix = IFCSIPrefix.Centi;
            }
            else if (areaUnit.Equals(UnitTypeId.SquareMillimeters))
            {
               areaPrefix = IFCSIPrefix.Milli;
            }
            else if (areaUnit.Equals(UnitTypeId.SquareFeet))
            {
               if (exportToCOBIE)
                  areaConvName = "foot";
               else
                  areaConvName = "SQUARE FOOT";
               areaConversionBased = true;
            }
            else if (areaUnit.Equals(UnitTypeId.SquareInches))
            {
               if (exportToCOBIE)
                  areaConvName = "inch";
               else
                  areaConvName = "SQUARE INCH";
               areaConversionBased = true;
            }
            else
            {
               //Couldn't find display unit type conversion -- assuming foot
               if (exportToCOBIE)
                  areaConvName = "foot";
               else
                  areaConvName = "SQUARE FOOT";
               areaConversionBased = true;
               areaUseDefault = true;
            }

            double areaScaleFactor = UnitUtils.ConvertFromInternalUnits(1.0, areaUseDefault ? UnitTypeId.SquareFeet : areaFormatOptions.GetUnitTypeId());
            IFCAnyHandle areaSiUnit = IFCInstanceExporter.CreateSIUnit(file, areaUnitType, areaPrefix, areaUnitName);
            if (areaConversionBased)
            {
               double areaSIScaleFactor = areaScaleFactor * UnitUtils.ConvertFromInternalUnits(1.0, UnitTypeId.SquareMeters);
               IFCAnyHandle areaDims = IFCInstanceExporter.CreateDimensionalExponents(file, 2, 0, 0, 0, 0, 0, 0); // area
               IFCAnyHandle areaConvFactor = IFCInstanceExporter.CreateMeasureWithUnit(file, Toolkit.IFCDataUtil.CreateAsRatioMeasure(areaSIScaleFactor), areaSiUnit);
               areaSiUnit = IFCInstanceExporter.CreateConversionBasedUnit(file, areaDims, areaUnitType, areaConvName, areaConvFactor);
            }

            unitSet.Add(areaSiUnit);      // created above, so unique.
            ExporterCacheManager.UnitsCache.AddUnit(SpecTypeId.Area, areaSiUnit, areaScaleFactor, 0.0);
         }

         {
            bool volumeConversionBased = false;
            bool volumeUseDefault = false;

            IFCUnit volumeUnitType = IFCUnit.VolumeUnit;
            IFCSIPrefix? volumePrefix = null;
            IFCSIUnitName volumeUnitName = IFCSIUnitName.Cubic_Metre;
            string volumeConvName = null;

            FormatOptions volumeFormatOptions = doc.GetUnits().GetFormatOptions(SpecTypeId.Volume);
            ForgeTypeId volumeUnit = volumeFormatOptions.GetUnitTypeId();
            if (volumeUnit.Equals(UnitTypeId.CubicMeters))
            {
               // This space intentionally left blank.
            }
            else if (volumeUnit.Equals(UnitTypeId.Liters))
            {
               volumePrefix = IFCSIPrefix.Deci;
            }
            else if (volumeUnit.Equals(UnitTypeId.CubicCentimeters))
            {
               volumePrefix = IFCSIPrefix.Centi;
            }
            else if (volumeUnit.Equals(UnitTypeId.CubicMillimeters))
            {
               volumePrefix = IFCSIPrefix.Milli;
            }
            else if (volumeUnit.Equals(UnitTypeId.CubicFeet))
            {
               if (exportToCOBIE)
                  volumeConvName = "foot";
               else
                  volumeConvName = "CUBIC FOOT";
               volumeConversionBased = true;
            }
            else if (volumeUnit.Equals(UnitTypeId.CubicInches))
            {
               if (exportToCOBIE)
                  volumeConvName = "inch";
               else
                  volumeConvName = "CUBIC INCH";
               volumeConversionBased = true;
            }
            else
            {
               //Couldn't find display unit type conversion -- assuming foot
               if (exportToCOBIE)
                  volumeConvName = "foot";
               else
                  volumeConvName = "CUBIC FOOT";
               volumeConversionBased = true;
               volumeUseDefault = true;
            }

            double volumeScaleFactor =
                UnitUtils.ConvertFromInternalUnits(1.0, volumeUseDefault ? UnitTypeId.CubicFeet : volumeFormatOptions.GetUnitTypeId());
            IFCAnyHandle volumeSiUnit = IFCInstanceExporter.CreateSIUnit(file, volumeUnitType, volumePrefix, volumeUnitName);
            if (volumeConversionBased)
            {
               double volumeSIScaleFactor = volumeScaleFactor * UnitUtils.ConvertFromInternalUnits(1.0, UnitTypeId.CubicMeters);
               IFCAnyHandle volumeDims = IFCInstanceExporter.CreateDimensionalExponents(file, 3, 0, 0, 0, 0, 0, 0); // area
               IFCAnyHandle volumeConvFactor = IFCInstanceExporter.CreateMeasureWithUnit(file, Toolkit.IFCDataUtil.CreateAsRatioMeasure(volumeSIScaleFactor), volumeSiUnit);
               volumeSiUnit = IFCInstanceExporter.CreateConversionBasedUnit(file, volumeDims, volumeUnitType, volumeConvName, volumeConvFactor);
            }

            unitSet.Add(volumeSiUnit);      // created above, so unique.
            ExporterCacheManager.UnitsCache.AddUnit(SpecTypeId.Volume, volumeSiUnit, volumeScaleFactor, 0.0);
         }

         {
            IFCUnit unitType = IFCUnit.PlaneAngleUnit;
            IFCSIUnitName unitName = IFCSIUnitName.Radian;

            IFCAnyHandle planeAngleUnit = IFCInstanceExporter.CreateSIUnit(file, unitType, null, unitName);

            string convName = null;

            FormatOptions angleFormatOptions = doc.GetUnits().GetFormatOptions(SpecTypeId.Angle);
            bool angleUseDefault = false;
            ForgeTypeId angleUnit = angleFormatOptions.GetUnitTypeId();
            if (angleUnit.Equals(UnitTypeId.Degrees) ||
               angleUnit.Equals(UnitTypeId.DegreesMinutes))
            {
               convName = "DEGREE";
            }
            else if (angleUnit.Equals(UnitTypeId.Gradians))
            {
               convName = "GRAD";
            }
            else if (angleUnit.Equals(UnitTypeId.Radians))
            {
               // This space intentionally left blank.
            }
            else
            {
               angleUseDefault = true;
               convName = "DEGREE";
            }

            IFCAnyHandle dims = IFCInstanceExporter.CreateDimensionalExponents(file, 0, 0, 0, 0, 0, 0, 0);

            double angleScaleFactor = UnitUtils.Convert(1.0, angleUseDefault ? UnitTypeId.Degrees : angleFormatOptions.GetUnitTypeId(), UnitTypeId.Radians);
            if (convName != null)
            {
               IFCAnyHandle convFactor = IFCInstanceExporter.CreateMeasureWithUnit(file, Toolkit.IFCDataUtil.CreateAsRatioMeasure(angleScaleFactor), planeAngleUnit);
               planeAngleUnit = IFCInstanceExporter.CreateConversionBasedUnit(file, dims, unitType, convName, convFactor);
            }
            unitSet.Add(planeAngleUnit);      // created above, so unique.
            ExporterCacheManager.UnitsCache.AddUnit(SpecTypeId.Angle, planeAngleUnit, 1.0 / angleScaleFactor, 0.0);
         }

         // Mass
         IFCAnyHandle massSIUnit = null;
         {
            massSIUnit = CreateSIUnit(file, SpecTypeId.Mass, IFCUnit.MassUnit, IFCSIUnitName.Gram, IFCSIPrefix.Kilo, null);
            // If we are exporting to GSA standard, we will override kg with pound below.
            if (!exportToCOBIE)
               unitSet.Add(massSIUnit);      // created above, so unique.
         }

         // Mass density - support metric kg/(m^3) only.
         {
            ISet<IFCAnyHandle> elements = new HashSet<IFCAnyHandle>();
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, massSIUnit, 1));
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, lenSIBaseUnit, -3));

            IFCAnyHandle massDensityUnit = IFCInstanceExporter.CreateDerivedUnit(file, elements,
                IFCDerivedUnitEnum.MassDensityUnit, null);
            unitSet.Add(massDensityUnit);

            double massDensityFactor = UnitUtils.ConvertFromInternalUnits(1.0, UnitTypeId.KilogramsPerCubicMeter);
            ExporterCacheManager.UnitsCache.AddUnit(SpecTypeId.MassDensity, massDensityUnit, massDensityFactor, 0.0);
         }

         // Moment of inertia - support metric m^4.
         {
            ISet<IFCAnyHandle> elements = new HashSet<IFCAnyHandle>();
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, lenSIBaseUnit, 4));

            IFCAnyHandle momentOfInertiaUnit = IFCInstanceExporter.CreateDerivedUnit(file, elements,
                IFCDerivedUnitEnum.MomentOfInertiaUnit, null);
            unitSet.Add(momentOfInertiaUnit);

            double momentOfInertiaFactor = UnitUtils.ConvertFromInternalUnits(1.0, UnitTypeId.MetersToTheFourthPower);
            ExporterCacheManager.UnitsCache.AddUnit(SpecTypeId.MomentOfInertia, momentOfInertiaUnit, momentOfInertiaFactor, 0.0);
         }

         // Time -- support seconds only.
         IFCAnyHandle timeSIUnit = null;
         {
            timeSIUnit = CreateSIUnit(file, null, IFCUnit.TimeUnit, IFCSIUnitName.Second, null, null);
            unitSet.Add(timeSIUnit);      // created above, so unique.
         }

         // Frequency = support Hertz only.
         {
            IFCAnyHandle frequencySIUnit = CreateSIUnit(file, null, IFCUnit.FrequencyUnit, IFCSIUnitName.Hertz, null, null);
            unitSet.Add(frequencySIUnit);      // created above, so unique.
         }

         // Temperature
         IFCAnyHandle tempBaseSIUnit = null;
         {
            // Base SI unit for temperature.
            tempBaseSIUnit = CreateSIUnit(file, null, IFCUnit.ThermoDynamicTemperatureUnit, IFCSIUnitName.Kelvin, null, null);

            // We are going to have two entries: one for thermodynamic temperature (default), and one for color temperature.
            FormatOptions tempFormatOptions = doc.GetUnits().GetFormatOptions(SpecTypeId.HvacTemperature);
            IFCSIUnitName thermalTempUnit;
            double offset = 0.0;
            ForgeTypeId tempUnit = tempFormatOptions.GetUnitTypeId();
            if (tempUnit.Equals(UnitTypeId.Celsius) ||
               tempUnit.Equals(UnitTypeId.Fahrenheit))
            {
               thermalTempUnit = IFCSIUnitName.Degree_Celsius;
               offset = -273.15;
            }
            else
            {
               thermalTempUnit = IFCSIUnitName.Kelvin;
            }

            IFCAnyHandle temperatureSIUnit = null;
            if (thermalTempUnit != IFCSIUnitName.Kelvin)
               temperatureSIUnit = IFCInstanceExporter.CreateSIUnit(file, IFCUnit.ThermoDynamicTemperatureUnit, null, thermalTempUnit);
            else
               temperatureSIUnit = tempBaseSIUnit;
            ExporterCacheManager.UnitsCache.AddUnit(SpecTypeId.HvacTemperature, temperatureSIUnit, 1.0, offset);

            unitSet.Add(temperatureSIUnit);      // created above, so unique.

            // Color temperature.
            // We don't add the color temperature to the unit set; it will be explicitly used.
            IFCAnyHandle colorTempSIUnit = tempBaseSIUnit;
            ExporterCacheManager.UnitsCache["COLORTEMPERATURE"] = colorTempSIUnit;
         }

         // Thermal transmittance - support metric W/(m^2 * K) = kg/(K * s^3) only.
         {
            ISet<IFCAnyHandle> elements = new HashSet<IFCAnyHandle>();
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, massSIUnit, 1));
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, tempBaseSIUnit, -1));
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, timeSIUnit, -3));

            IFCAnyHandle thermalTransmittanceUnit = IFCInstanceExporter.CreateDerivedUnit(file, elements,
                IFCDerivedUnitEnum.ThermalTransmittanceUnit, null);
            unitSet.Add(thermalTransmittanceUnit);
         }

         // Volumetric Flow Rate - support metric L/s or m^3/s only.
         {
            IFCAnyHandle volumetricFlowRateLenUnit = null;

            FormatOptions flowFormatOptions = doc.GetUnits().GetFormatOptions(SpecTypeId.AirFlow);
            ForgeTypeId forgeTypeId = flowFormatOptions.GetUnitTypeId();
            if (forgeTypeId.Equals(UnitTypeId.LitersPerSecond))
            {
               volumetricFlowRateLenUnit = IFCInstanceExporter.CreateSIUnit(file, IFCUnit.LengthUnit, IFCSIPrefix.Deci, IFCSIUnitName.Metre);
            }
            else
            {
               volumetricFlowRateLenUnit = lenSIBaseUnit;   // use m^3/s by default.
               forgeTypeId = UnitTypeId.CubicMetersPerSecond;
            }
            double volumetricFlowRateFactor = UnitUtils.ConvertFromInternalUnits(1.0, forgeTypeId);

            ISet<IFCAnyHandle> elements = new HashSet<IFCAnyHandle>();
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, lenSIBaseUnit, 3));
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, timeSIUnit, -1));

            IFCAnyHandle volumetricFlowRateUnit = IFCInstanceExporter.CreateDerivedUnit(file, elements,
                IFCDerivedUnitEnum.VolumetricFlowRateUnit, null);
            unitSet.Add(volumetricFlowRateUnit);

            ExporterCacheManager.UnitsCache.AddUnit(SpecTypeId.AirFlow, volumetricFlowRateUnit, volumetricFlowRateFactor, 0.0);
         }

         // Mass flow rate - support kg/s only.
         {
            ISet<IFCAnyHandle> elements = new HashSet<IFCAnyHandle>();
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, massSIUnit, 1));
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, timeSIUnit, -1));

            IFCAnyHandle massFlowRateUnit = IFCInstanceExporter.CreateDerivedUnit(file, elements,
                IFCDerivedUnitEnum.MassFlowRateUnit, null);
            unitSet.Add(massFlowRateUnit);

            double massFlowRateFactor = UnitUtils.ConvertFromInternalUnits(1.0, UnitTypeId.KilogramsPerSecond);
            ExporterCacheManager.UnitsCache.AddUnit(SpecTypeId.PipingMassPerTime, massFlowRateUnit, massFlowRateFactor, 0.0);
         }

         // Rotational frequency - support cycles/s only.
         {
            ISet<IFCAnyHandle> elements = new HashSet<IFCAnyHandle>();
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, timeSIUnit, -1));

            IFCAnyHandle rotationalFrequencyUnit = IFCInstanceExporter.CreateDerivedUnit(file, elements,
                IFCDerivedUnitEnum.RotationalFrequencyUnit, null);
            unitSet.Add(rotationalFrequencyUnit);

            double rotationalFrequencyFactor = UnitUtils.ConvertFromInternalUnits(1.0, UnitTypeId.RevolutionsPerSecond);
            ExporterCacheManager.UnitsCache.AddUnit(SpecTypeId.AngularSpeed, rotationalFrequencyUnit, rotationalFrequencyFactor, 0.0);
         }

         // Electrical current - support metric ampere only.
         {
            IFCAnyHandle currentSIUnit = CreateSIUnit(file, SpecTypeId.Current, IFCUnit.ElectricCurrentUnit, IFCSIUnitName.Ampere,
                null, UnitTypeId.Amperes);
            unitSet.Add(currentSIUnit);      // created above, so unique.
         }

         // Electrical voltage - support metric volt only.
         {
            IFCAnyHandle voltageSIUnit = CreateSIUnit(file, SpecTypeId.ElectricalPotential, IFCUnit.ElectricVoltageUnit, IFCSIUnitName.Volt,
                null, UnitTypeId.Volts);
            unitSet.Add(voltageSIUnit);      // created above, so unique.
         }

         // Power - support metric watt only.
         {
            IFCAnyHandle voltageSIUnit = CreateSIUnit(file, SpecTypeId.HvacPower, IFCUnit.PowerUnit, IFCSIUnitName.Watt,
                null, UnitTypeId.Watts);
            unitSet.Add(voltageSIUnit);      // created above, so unique.
         }

         // Force - support newtons (N) and kN only.
         {
            IFCSIPrefix? prefix = null;
            FormatOptions forceFormatOptions = doc.GetUnits().GetFormatOptions(SpecTypeId.Force);
            ForgeTypeId forceUnit = forceFormatOptions.GetUnitTypeId();
            if (forceUnit.Equals(UnitTypeId.Newtons))
            {
               // This space intentionally left blank.
            }
            else if (forceUnit.Equals(UnitTypeId.Kilonewtons))
            {
               prefix = IFCSIPrefix.Kilo;
            }
            else
            {
               forceUnit = UnitTypeId.Newtons;
            }

            IFCAnyHandle forceSIUnit = CreateSIUnit(file, SpecTypeId.Force, IFCUnit.ForceUnit, IFCSIUnitName.Newton,
                prefix, forceUnit);
            unitSet.Add(forceSIUnit);      // created above, so unique.
         }

         // Illuminance
         {
            IFCSIPrefix? prefix = null;
            IFCAnyHandle luxSIUnit = CreateSIUnit(file, SpecTypeId.Illuminance, IFCUnit.IlluminanceUnit, IFCSIUnitName.Lux,
                prefix, UnitTypeId.Lux);
            unitSet.Add(luxSIUnit);      // created above, so unique.
            ExporterCacheManager.UnitsCache["LUX"] = luxSIUnit;
         }

         // Luminous Flux
         IFCAnyHandle lumenSIUnit = null;
         {
            IFCSIPrefix? prefix = null;
            lumenSIUnit = CreateSIUnit(file, SpecTypeId.LuminousFlux, IFCUnit.LuminousFluxUnit, IFCSIUnitName.Lumen,
                prefix, UnitTypeId.Lumens);
            unitSet.Add(lumenSIUnit);      // created above, so unique.
         }

         // Luminous Intensity
         {
            IFCSIPrefix? prefix = null;
            IFCAnyHandle candelaSIUnit = CreateSIUnit(file, SpecTypeId.LuminousIntensity, IFCUnit.LuminousIntensityUnit, IFCSIUnitName.Candela,
                prefix, UnitTypeId.Candelas);
            unitSet.Add(candelaSIUnit);      // created above, so unique.
         }

         // Luminous Efficacy - support lm/W only.
         {
            ISet<IFCAnyHandle> elements = new HashSet<IFCAnyHandle>();
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, massSIUnit, -1));
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, lenSIBaseUnit, -2));
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, timeSIUnit, 3));
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, lumenSIUnit, 1));

            IFCAnyHandle luminousEfficacyUnit = IFCInstanceExporter.CreateDerivedUnit(file, elements,
                IFCDerivedUnitEnum.UserDefined, "Luminous Efficacy");

            double electricalEfficacyFactor = UnitUtils.ConvertFromInternalUnits(1.0, UnitTypeId.LumensPerWatt);
            ExporterCacheManager.UnitsCache.AddUnit(SpecTypeId.Efficacy, luminousEfficacyUnit, electricalEfficacyFactor, 0.0);
            ExporterCacheManager.UnitsCache["LUMINOUSEFFICACY"] = luminousEfficacyUnit;

            unitSet.Add(luminousEfficacyUnit);
         }

         // Linear Velocity - support m/s only.
         {
            ISet<IFCAnyHandle> elements = new HashSet<IFCAnyHandle>();
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, lenSIBaseUnit, 1));
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, timeSIUnit, -1));

            IFCAnyHandle linearVelocityUnit = IFCInstanceExporter.CreateDerivedUnit(file, elements,
                IFCDerivedUnitEnum.LinearVelocityUnit, null);

            double linearVelocityFactor = UnitUtils.ConvertFromInternalUnits(1.0, UnitTypeId.MetersPerSecond);
            ExporterCacheManager.UnitsCache.AddUnit(SpecTypeId.HvacVelocity, linearVelocityUnit, linearVelocityFactor, 0.0);

            unitSet.Add(linearVelocityUnit);
         }

         // Currency - disallowed for IC2x3 Coordination View 2.0.  If we find a currency, export it as a real.
         if (!ExporterCacheManager.ExportOptionsCache.ExportAs2x3CoordinationView2)
         {
            FormatOptions currencyFormatOptions = doc.GetUnits().GetFormatOptions(SpecTypeId.Currency);
            ForgeTypeId currencySymbol = currencyFormatOptions.GetSymbolTypeId();

            IFCAnyHandle currencyUnit = null;

            // Some of these are guesses for IFC2x3, since multiple currencies may use the same symbol, 
            // but no detail is given on which currency is being used.  For IFC4, we just use the label.
            if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            {
               string currencyLabel = null;
               try
               {
                  currencyLabel = LabelUtils.GetLabelForSymbol(currencySymbol);
                  currencyUnit = IFCInstanceExporter.CreateMonetaryUnit4(file, currencyLabel);
               }
               catch
               {
                  currencyUnit = null;
               }
            }
            else
            {
               IFCCurrencyType? currencyType = null;

               if (currencySymbol.Equals(SymbolTypeId.UsDollar))
               {
                  currencyType = IFCCurrencyType.USD;
               }
               else if (currencySymbol.Equals(SymbolTypeId.EuroPrefix) ||
                  currencySymbol.Equals(SymbolTypeId.EuroSuffix))
               {
                  currencyType = IFCCurrencyType.EUR;
               }
               else if (currencySymbol.Equals(SymbolTypeId.UkPound))
               {
                  currencyType = IFCCurrencyType.GBP;
               }
               else if (currencySymbol.Equals(SymbolTypeId.ChineseHongKongDollar))
               {
                  currencyType = IFCCurrencyType.HKD;
               }
               else if (currencySymbol.Equals(SymbolTypeId.Krone))
               {
                  currencyType = IFCCurrencyType.NOK;
               }
               else if (currencySymbol.Equals(SymbolTypeId.Shekel))
               {
                  currencyType = IFCCurrencyType.ILS;
               }
               else if (currencySymbol.Equals(SymbolTypeId.Yen))
               {
                  currencyType = IFCCurrencyType.JPY;
               }
               else if (currencySymbol.Equals(SymbolTypeId.Won))
               {
                  currencyType = IFCCurrencyType.KRW;
               }
               else if (currencySymbol.Equals(SymbolTypeId.Baht))
               {
                  currencyType = IFCCurrencyType.THB;
               }
               else if (currencySymbol.Equals(SymbolTypeId.Dong))
               {
                  currencyType = IFCCurrencyType.VND;
               }

               if (currencyType.HasValue)
                  currencyUnit = IFCInstanceExporter.CreateMonetaryUnit2x3(file, currencyType.Value);
            }

            if (currencyUnit != null)
            {
               unitSet.Add(currencyUnit);      // created above, so unique.
               // We will cache the currency, if we create it.  If we don't, we'll export currencies as numbers.
               ExporterCacheManager.UnitsCache["CURRENCY"] = currencyUnit;
            }
         }

         // Pressure - support Pascal, kPa and MPa.
         {
            IFCSIPrefix? prefix = null;
            FormatOptions pressureFormatOptions = doc.GetUnits().GetFormatOptions(SpecTypeId.HvacPressure);
            ForgeTypeId pressureUnit = pressureFormatOptions.GetUnitTypeId();
            if (pressureUnit.Equals(UnitTypeId.Pascals))
            {
               // This space intentionally left blank.
            }
            else if (pressureUnit.Equals(UnitTypeId.Kilopascals))
            {
               prefix = IFCSIPrefix.Kilo;
            }
            else if (pressureUnit.Equals(UnitTypeId.Megapascals))
            {
               prefix = IFCSIPrefix.Mega;
            }
            else
            {
               pressureUnit = UnitTypeId.Pascals;
            }

            IFCAnyHandle pressureSIUnit = CreateSIUnit(file, SpecTypeId.HvacPressure, IFCUnit.PressureUnit, IFCSIUnitName.Pascal,
                prefix, pressureUnit);
            unitSet.Add(pressureSIUnit);      // created above, so unique.
         }

         // Friction loss - support Pa/m only.
         {
            ISet<IFCAnyHandle> elements = new HashSet<IFCAnyHandle>();
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, lenSIBaseUnit, -2));
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, massSIUnit, 1));
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, timeSIUnit, -2));

            IFCAnyHandle frictionLossUnit = IFCInstanceExporter.CreateDerivedUnit(file, elements,
                IFCDerivedUnitEnum.UserDefined, "Friction Loss");

            double frictionLossFactor = UnitUtils.ConvertFromInternalUnits(1.0, UnitTypeId.PascalsPerMeter);
            ExporterCacheManager.UnitsCache.AddUnit(SpecTypeId.HvacFriction, frictionLossUnit, frictionLossFactor, 0.0);
            ExporterCacheManager.UnitsCache["FRICTIONLOSS"] = frictionLossUnit;

            unitSet.Add(frictionLossUnit);
         }

         // Area/Planar Force - support N/m2 only, and Linear Force - support N/m only
         {
            ISet<IFCAnyHandle> elements = new HashSet<IFCAnyHandle>();
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, massSIUnit, 1));
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, lenSIBaseUnit, 1));
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, timeSIUnit, -2));
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, lenSIBaseUnit, -1));

            IFCAnyHandle linearForceUnit = IFCInstanceExporter.CreateDerivedUnit(file, elements,
                IFCDerivedUnitEnum.LinearForceUnit, null);

            double linearForceFactor = UnitUtils.ConvertFromInternalUnits(1.0, UnitTypeId.NewtonsPerMeter);
            ExporterCacheManager.UnitsCache.AddUnit(SpecTypeId.LinearForce, linearForceUnit, linearForceFactor, 0.0);
            unitSet.Add(linearForceUnit);

            elements = new HashSet<IFCAnyHandle>();
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, massSIUnit, 1));
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, lenSIBaseUnit, 1));
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, timeSIUnit, -2));
            elements.Add(IFCInstanceExporter.CreateDerivedUnitElement(file, lenSIBaseUnit, -2));

            IFCAnyHandle planarForceUnit = IFCInstanceExporter.CreateDerivedUnit(file, elements,
                IFCDerivedUnitEnum.PlanarForceUnit, null);

            double planarForceFactor = UnitUtils.ConvertFromInternalUnits(1.0, UnitTypeId.NewtonsPerSquareMeter);
            ExporterCacheManager.UnitsCache.AddUnit(SpecTypeId.AreaForce, planarForceUnit, planarForceFactor, 0.0);
            unitSet.Add(planarForceUnit);
         }

         // GSA only units.
         if (exportToCOBIE)
         {
            // Derived imperial mass unit
            {
               IFCUnit unitType = IFCUnit.MassUnit;
               IFCAnyHandle dims = IFCInstanceExporter.CreateDimensionalExponents(file, 0, 1, 0, 0, 0, 0, 0);
               double factor = 0.45359237; // --> pound to kilogram
               string convName = "pound";

               IFCAnyHandle convFactor = IFCInstanceExporter.CreateMeasureWithUnit(file, Toolkit.IFCDataUtil.CreateAsRatioMeasure(factor), massSIUnit);
               IFCAnyHandle massUnit = IFCInstanceExporter.CreateConversionBasedUnit(file, dims, unitType, convName, convFactor);
               unitSet.Add(massUnit);      // created above, so unique.
            }

            // Air Changes per Hour
            {
               IFCUnit unitType = IFCUnit.FrequencyUnit;
               IFCAnyHandle dims = IFCInstanceExporter.CreateDimensionalExponents(file, 0, 0, -1, 0, 0, 0, 0);
               double factor = 1.0 / 3600.0; // --> seconds to hours
               string convName = "ACH";

               IFCAnyHandle convFactor = IFCInstanceExporter.CreateMeasureWithUnit(file, Toolkit.IFCDataUtil.CreateAsRatioMeasure(factor), timeSIUnit);
               IFCAnyHandle achUnit = IFCInstanceExporter.CreateConversionBasedUnit(file, dims, unitType, convName, convFactor);
               unitSet.Add(achUnit);      // created above, so unique.
               ExporterCacheManager.UnitsCache["ACH"] = achUnit;
            }
         }

         return IFCInstanceExporter.CreateUnitAssignment(file, unitSet);
      }

      /// <summary>
      /// Creates the global direction and sets the cardinal directions in 3D.
      /// </summary>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      private void CreateGlobalDirection(ExporterIFC exporterIFC)
      {
         // Note that we do not use the ExporterUtil.CreateDirection functions below, as they try
         // to match the input XYZ to one of the "global" directions that we are creating below.
         IFCAnyHandle xDirPos = null;
         IFCAnyHandle xDirNeg = null;
         IFCAnyHandle yDirPos = null;
         IFCAnyHandle yDirNeg = null;
         IFCAnyHandle zDirPos = null;
         IFCAnyHandle zDirNeg = null;

         IFCFile file = exporterIFC.GetFile();
         IList<double> xxp = new List<double>();
         xxp.Add(1.0); xxp.Add(0.0); xxp.Add(0.0);
         xDirPos = IFCInstanceExporter.CreateDirection(file, xxp);

         IList<double> xxn = new List<double>();
         xxn.Add(-1.0); xxn.Add(0.0); xxn.Add(0.0);
         xDirNeg = IFCInstanceExporter.CreateDirection(file, xxn);

         IList<double> yyp = new List<double>();
         yyp.Add(0.0); yyp.Add(1.0); yyp.Add(0.0);
         yDirPos = IFCInstanceExporter.CreateDirection(file, yyp);

         IList<double> yyn = new List<double>();
         yyn.Add(0.0); yyn.Add(-1.0); yyn.Add(0.0);
         yDirNeg = IFCInstanceExporter.CreateDirection(file, yyn);

         IList<double> zzp = new List<double>();
         zzp.Add(0.0); zzp.Add(0.0); zzp.Add(1.0);
         zDirPos = IFCInstanceExporter.CreateDirection(file, zzp);

         IList<double> zzn = new List<double>();
         zzn.Add(0.0); zzn.Add(0.0); zzn.Add(-1.0);
         zDirNeg = IFCInstanceExporter.CreateDirection(file, zzn);

         ExporterIFCUtils.SetGlobal3DDirectionHandles(true, xDirPos, yDirPos, zDirPos);
         ExporterIFCUtils.SetGlobal3DDirectionHandles(false, xDirNeg, yDirNeg, zDirNeg);
      }

      /// <summary>
      /// Creates the global direction and sets the cardinal directions in 2D.
      /// </summary>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      private void CreateGlobalDirection2D(ExporterIFC exporterIFC)
      {
         IFCAnyHandle xDirPos2D = null;
         IFCAnyHandle xDirNeg2D = null;
         IFCAnyHandle yDirPos2D = null;
         IFCAnyHandle yDirNeg2D = null;
         IFCFile file = exporterIFC.GetFile();

         IList<double> xxp = new List<double>();
         xxp.Add(1.0); xxp.Add(0.0);
         xDirPos2D = IFCInstanceExporter.CreateDirection(file, xxp);

         IList<double> xxn = new List<double>();
         xxn.Add(-1.0); xxn.Add(0.0);
         xDirNeg2D = IFCInstanceExporter.CreateDirection(file, xxn);

         IList<double> yyp = new List<double>();
         yyp.Add(0.0); yyp.Add(1.0);
         yDirPos2D = IFCInstanceExporter.CreateDirection(file, yyp);

         IList<double> yyn = new List<double>();
         yyn.Add(0.0); yyn.Add(-1.0);
         yDirNeg2D = IFCInstanceExporter.CreateDirection(file, yyn);
         ExporterIFCUtils.SetGlobal2DDirectionHandles(true, xDirPos2D, yDirPos2D);
         ExporterIFCUtils.SetGlobal2DDirectionHandles(false, xDirNeg2D, yDirNeg2D);
      }

      /// <summary>
      /// Creates the global cartesian origin then sets the 3D and 2D origins.
      /// </summary>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      private void CreateGlobalCartesianOrigin(ExporterIFC exporterIFC)
      {

         IFCAnyHandle origin2d = null;
         IFCAnyHandle origin = null;

         IFCFile file = exporterIFC.GetFile();
         IList<double> measure = new List<double>();
         measure.Add(0.0); measure.Add(0.0); measure.Add(0.0);
         origin = IFCInstanceExporter.CreateCartesianPoint(file, measure);

         IList<double> measure2d = new List<double>();
         measure2d.Add(0.0); measure2d.Add(0.0);
         origin2d = IFCInstanceExporter.CreateCartesianPoint(file, measure2d);
         ExporterIFCUtils.SetGlobal3DOriginHandle(origin);
         ExporterIFCUtils.SetGlobal2DOriginHandle(origin2d);
      }

      private HashSet<IFCAnyHandle> RemoveContainedHandlesFromSet(ICollection<IFCAnyHandle> initialSet)
      {
         HashSet<IFCAnyHandle> filteredSet = new HashSet<IFCAnyHandle>();

         if (initialSet != null)
         {
            foreach (IFCAnyHandle initialHandle in initialSet)
            {
               if (ExporterCacheManager.ElementsInAssembliesCache.Contains(initialHandle))
                  continue;

               try
               {
                  if (!IFCAnyHandleUtil.HasRelDecomposes(initialHandle))
                     filteredSet.Add(initialHandle);
               }
               catch
               {
               }
            }
         }

         return filteredSet;
      }

      /// <summary>
      /// Relate levels and products.
      /// </summary>
      /// <param name="exporterIFC">The IFC exporter object.</param>
      /// <param name="document">The document to relate the levels.</param>
      private void RelateLevels(ExporterIFC exporterIFC, Document document)
      {
         HashSet<IFCAnyHandle> buildingStories = new HashSet<IFCAnyHandle>();
         IList<ElementId> levelIds = ExporterCacheManager.LevelInfoCache.LevelsByElevation;
         IFCFile file = exporterIFC.GetFile();

         for (int ii = 0; ii < levelIds.Count; ii++)
         {
            ElementId levelId = levelIds[ii];
            IFCLevelInfo levelInfo = ExporterCacheManager.LevelInfoCache.GetLevelInfo(exporterIFC, levelId);
            if (levelInfo == null)
               continue;

            // remove products that are aggregated (e.g., railings in stairs).
            Element level = document.GetElement(levelId);

            ICollection<IFCAnyHandle> relatedProductsToCheck = levelInfo.GetRelatedProducts();
            ICollection<IFCAnyHandle> relatedElementsToCheck = levelInfo.GetRelatedElements();

            // get coincident levels, if any.
            double currentElevation = levelInfo.Elevation;
            int nextLevelIdx = ii + 1;
            for (int jj = ii + 1; jj < levelIds.Count; jj++, nextLevelIdx++)
            {
               ElementId nextLevelId = levelIds[jj];
               IFCLevelInfo levelInfo2 = ExporterCacheManager.LevelInfoCache.GetLevelInfo(exporterIFC, nextLevelId);
               if (levelInfo2 == null)
                  continue;

               if (MathUtil.IsAlmostEqual(currentElevation, levelInfo2.Elevation))
               {
                  foreach (IFCAnyHandle relatedProduct in levelInfo2.GetRelatedProducts())
                     relatedProductsToCheck.Add(relatedProduct);

                  foreach (IFCAnyHandle relatedElement in levelInfo2.GetRelatedElements())
                     relatedElementsToCheck.Add(relatedElement);
               }
               else
                  break;
            }

            // We may get stale handles in here; protect against this.
            HashSet<IFCAnyHandle> relatedProducts = RemoveContainedHandlesFromSet(relatedProductsToCheck);
            HashSet<IFCAnyHandle> relatedElements = RemoveContainedHandlesFromSet(relatedElementsToCheck);

            // skip coincident levels, if any.
            for (int jj = ii + 1; jj < nextLevelIdx; jj++)
            {
               ElementId nextLevelId = levelIds[jj];
               IFCLevelInfo levelInfo2 = ExporterCacheManager.LevelInfoCache.GetLevelInfo(exporterIFC, nextLevelId);
               if (levelInfo2 == null)
                  continue;

               if (!levelInfo.GetBuildingStorey().Equals(levelInfo2.GetBuildingStorey()))
                  IFCAnyHandleUtil.Delete(levelInfo2.GetBuildingStorey());
            }
            ii = nextLevelIdx - 1;

            if (relatedProducts.Count == 0 && relatedElements.Count == 0)
               IFCAnyHandleUtil.Delete(levelInfo.GetBuildingStorey());
            else
            {
               // We have decided to keep the level - export properties, quantities and classifications.
               using (ProductWrapper productWrapper = ProductWrapper.Create(exporterIFC, false))
               {
                  IFCAnyHandle buildingStoreyHandle = levelInfo.GetBuildingStorey();
                  buildingStories.Add(buildingStoreyHandle);
                  IFCExportInfoPair exportInfo = new IFCExportInfoPair(IFCEntityType.IfcBuildingStorey);

                  // Add Property set, quantities and classification of Building Storey also to IFC
                  productWrapper.AddElement(level, buildingStoreyHandle, levelInfo, null, false, exportInfo);

                  ExporterUtil.ExportRelatedProperties(exporterIFC, level, productWrapper);
               }
            }

            if (relatedProducts.Count > 0)
            {
               HashSet<IFCAnyHandle> buildingProducts = RemoveContainedHandlesFromSet(relatedProducts);
               if (buildingProducts.Count > 0)
               {
                  IFCAnyHandle buildingStorey = levelInfo.GetBuildingStorey();
                  string guid = GUIDUtil.CreateSubElementGUID(level, (int)IFCBuildingStoreySubElements.RelAggregates);
                  ExporterCacheManager.ContainmentCache.SetGUIDForRelation(buildingStorey, guid);
                  ExporterCacheManager.ContainmentCache.AddRelations(buildingStorey, buildingProducts);
               }
            }

            if (relatedElements.Count > 0)
            {
               HashSet<IFCAnyHandle> buildingElements = RemoveContainedHandlesFromSet(relatedElements);
               if (buildingElements.Count > 0)
               {
                  string guid = GUIDUtil.CreateSubElementGUID(level, (int)IFCBuildingStoreySubElements.RelContainedInSpatialStructure);
                  IFCInstanceExporter.CreateRelContainedInSpatialStructure(file, guid, ExporterCacheManager.OwnerHistoryHandle, null, null, buildingElements, levelInfo.GetBuildingStorey());
               }
            }
         }

         if (buildingStories.Count > 0)
         {
            IFCAnyHandle buildingHnd = ExporterCacheManager.BuildingHandle;
            ProjectInfo projectInfo = document.ProjectInformation;
            string guid = GUIDUtil.CreateSubElementGUID(projectInfo, (int)IFCProjectSubElements.RelAggregatesBuildingStories);
            if (!ExporterCacheManager.ContainmentCache.ContainsKey(buildingHnd))
               ExporterCacheManager.ContainmentCache.SetGUIDForRelation(buildingHnd, guid);
            ExporterCacheManager.ContainmentCache.AddRelations(buildingHnd, buildingStories);
         }
      }

      /// <summary>
      /// Clear all delegates.
      /// </summary>
      private void DelegateClear()
      {
         m_ElementExporter = null;
         m_PropertySetsToExport = null;
         m_QuantitiesToExport = null;
      }

      private IFCAnyHandle CreateBuildingPlacement(IFCFile file)
      {
         return IFCInstanceExporter.CreateLocalPlacement(file, null, ExporterUtil.CreateAxis2Placement3D(file));
      }

      private IFCAnyHandle CreateBuildingFromProjectInfo(ExporterIFC exporterIFC, Document document, IFCAnyHandle buildingPlacement)
      {
         ProjectInfo projectInfo = document.ProjectInformation;
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

         string buildingName = String.Empty;
         string buildingDescription = null;
         string buildingLongName = null;
         string buildingObjectType = null;

         COBieProjectInfo cobieProjectInfo = ExporterCacheManager.ExportOptionsCache.COBieProjectInfo;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3COBIE24DesignDeliverable && cobieProjectInfo != null)
         {
            buildingName = cobieProjectInfo.BuildingName_Number;
            buildingDescription = cobieProjectInfo.BuildingDescription;
         }
         else if (projectInfo != null)
         {
            try
            {
               buildingName = projectInfo.BuildingName;
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException)
            {
            }
            buildingDescription = NamingUtil.GetOverrideStringValue(projectInfo, "BuildingDescription", null);
            buildingLongName = NamingUtil.GetOverrideStringValue(projectInfo, "BuildingLongName", buildingName);
            buildingObjectType = NamingUtil.GetOverrideStringValue(projectInfo, "BuildingObjectType", null);
         }

         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle address = null;
         if (Exporter.NeedToCreateAddressForBuilding(document))
            address = Exporter.CreateIFCAddress(file, document, projectInfo);

         string buildingGUID = GUIDUtil.CreateProjectLevelGUID(document, IFCProjectLevelGUIDType.Building);
         IFCAnyHandle buildingHandle = IFCInstanceExporter.CreateBuilding(exporterIFC,
             buildingGUID, ownerHistory, buildingName, buildingDescription, buildingObjectType, buildingPlacement, null, buildingLongName,
             Toolkit.IFCElementComposition.Element, null, null, address);
         ExporterCacheManager.BuildingHandle = buildingHandle;

         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x3COBIE24DesignDeliverable && cobieProjectInfo != null)
         {
            string classificationName;
            string classificationItemCode;
            string classificationItemName;
            string classificationParamValue = cobieProjectInfo.BuildingType;
            int numRefItem = ClassificationUtil.parseClassificationCode(classificationParamValue, "dummy", out classificationName, out classificationItemCode, out classificationItemName);
            if (numRefItem > 0 && !string.IsNullOrEmpty(classificationItemCode))
            {
               IFCAnyHandle classifRef = IFCInstanceExporter.CreateClassificationReference(file, null, classificationItemCode, classificationItemName, null);
               IFCAnyHandle relClassif = IFCInstanceExporter.CreateRelAssociatesClassification(file, GUIDUtil.CreateGUID(),
                                          ownerHistory, "BuildingType", null, new HashSet<IFCAnyHandle>() { buildingHandle }, classifRef);
            }
         }

         return buildingHandle;
      }

      /// <summary>
      /// Create IFCMapConversion that is from IFC4 onward capturing information for geo referencing
      /// </summary>
      /// <param name="exporterIFC">ExporterIFC</param>
      /// <param name="doc">the Document</param>
      /// <param name="geomRepContext">The GeometricRepresentationContex</param>
      /// <param name="TNDirRatio">TrueNorth direction ratios</param>
      private bool ExportIFCMapConversion(ExporterIFC exporterIFC, Document doc, IFCAnyHandle geomRepContext, IList<double> TNDirRatio)
      {
         // Get information from Project info Parameters for Project Global Position and Coordinate Reference System
         if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
            return false;

         ProjectInfo projectInfo = doc.ProjectInformation;
         string epsgCode = null;
         string defaultEPSGCode = "EPSG:3857";     // Default to EPSG:3857, which is the commonly used ProjectedCR as in GoogleMap, OpenStreetMap
         string crsMapUnitStr = ExporterCacheManager.ExportOptionsCache.GeoRefMapUnit;
         (string projectedCRSName, string projectedCRSDesc, string epsgCode, string geodeticDatum, string uom) crsInfo = (null, null, null, null, null);
         if (string.IsNullOrEmpty(ExporterCacheManager.ExportOptionsCache.GeoRefEPSGCode))
         {
            // Only CRSName is mandatory. Paramater sets in the ProjectInfo will override any value if any
            if (string.IsNullOrEmpty(epsgCode))
            {
               // Try to get the GIS Coordinate System id from SiteLocation
               crsInfo = OptionsUtil.GetEPSGCodeFromGeoCoordDef(doc.SiteLocation);
               if (string.IsNullOrEmpty(crsInfo.projectedCRSName))
               {
                  // If not set, use the default
                  epsgCode = defaultEPSGCode;
               }
               else
               {
                  epsgCode = crsInfo.epsgCode;
               }
               crsMapUnitStr = crsInfo.uom;
            }
         }
         else
         {
            epsgCode = ExporterCacheManager.ExportOptionsCache.GeoRefEPSGCode;
         }

         // IFC only "accepts" EPSG. see https://standards.buildingsmart.org/MVD/RELEASE/IFC4/ADD2_TC1/RV1_2/HTML/schema/ifcrepresentationresource/lexical/ifccoordinatereferencesystem.htm
         if (!epsgCode.StartsWith("EPSG:", StringComparison.InvariantCultureIgnoreCase))
         {
            // The value may contain only number, which means it it EPSG:<the number>
            int epsgNum = -1;
            if (int.TryParse(epsgCode, out epsgNum))
            {
               epsgCode = "EPSG:" + epsgCode;
            }
            else
            {
               epsgCode = defaultEPSGCode;
            }
         }

         double dblVal = double.MinValue;
         IFCFile file = exporterIFC.GetFile();

         // Explanation:
         // The Survey Point will carry the Northings and Eastings of the known Survey Point usually near to the project. 
         //    This is relative to the reference (0,0) of the Map Projection system used (EPSG: xxxx)
         //    This usually can be accomplished by first locating the Survey Point to the geo reference (0,0) (usually -x, -y of the known coordinate of the Survey point)
         //       It is then moved back UNCLIPPED to the original location
         //    This essentially create the shared location at the map reference (0,0)

         SiteTransformBasis wcsBasis = ExporterCacheManager.ExportOptionsCache.SiteTransformation;
         (double eastings, double northings, double orthogonalHeight) geoRefInfo = OptionsUtil.GeoReferenceInformation(doc, wcsBasis);
         double? xAxisAbscissa = null;
         double? xAxisOrdinate = null;

         if (TNDirRatio != null)
         {
            xAxisAbscissa = TNDirRatio.Count > 0 ? TNDirRatio[1] : 0;
            xAxisOrdinate = TNDirRatio.Count > 1 ? TNDirRatio[0] : 0;
         }

         string crsDescription = ExporterCacheManager.ExportOptionsCache.GeoRefCRSDesc;
         if (string.IsNullOrEmpty(crsDescription) && !string.IsNullOrEmpty(crsInfo.projectedCRSDesc))
            crsDescription = crsInfo.projectedCRSDesc;
         string crsGeodeticDatum = ExporterCacheManager.ExportOptionsCache.GeoRefGeodeticDatum;
         if (string.IsNullOrEmpty(crsGeodeticDatum) && !string.IsNullOrEmpty(crsInfo.geodeticDatum))
            crsGeodeticDatum = crsInfo.geodeticDatum;
         string crsVerticalDatum = null;
         double? scale = null;
         if (ParameterUtil.GetDoubleValueFromElement(projectInfo, null, "ProjectGlobalPositioning.Scale", out dblVal) != null)
            scale = dblVal;
         ParameterUtil.GetStringValueFromElement(projectInfo, "ProjectGlobalPositioning.CRSVerticalDatum", out crsVerticalDatum);
         string crsMapProjection = null;
         ParameterUtil.GetStringValueFromElement(projectInfo, "ProjectGlobalPositioning.CRSMapProjection", out crsMapProjection);
         string crsMapZone = null;
         ParameterUtil.GetStringValueFromElement(projectInfo, "ProjectGlobalPositioning.CRSMapZone", out crsMapZone);

         // Handle map unit
         IFCAnyHandle crsMapUnit = null;
         ForgeTypeId utId = UnitTypeId.Meters;
         if (!string.IsNullOrEmpty(crsMapUnitStr))
         {
            if (crsMapUnitStr.EndsWith("Metre", StringComparison.InvariantCultureIgnoreCase) || crsMapUnitStr.EndsWith("Meter", StringComparison.InvariantCultureIgnoreCase))
            {
               IFCSIPrefix? prefix = null;
               if (crsMapUnitStr.Length > 5)
               {
                  string prefixStr = crsMapUnitStr.Substring(0, crsMapUnitStr.Length - 5);
                  if (Enum.TryParse(prefixStr, true, out IFCSIPrefix prefixEnum))
                  {
                     prefix = prefixEnum;
                     switch (prefix)
                     {
                        // Handle SI Units from MM to M. Somehow UnitTypeId does not have larger than M units (It is unlikely to have it in the EPSG anyway)
                        case IFCSIPrefix.Deci:
                           utId = UnitTypeId.Decimeters;
                           break;
                        case IFCSIPrefix.Centi:
                           utId = UnitTypeId.Centimeters;
                           break;
                        case IFCSIPrefix.Milli:
                           utId = UnitTypeId.Millimeters;
                           break;
                        default:
                           utId = UnitTypeId.Meters;
                           break;
                     }
                  }
               }
               crsMapUnit = IFCInstanceExporter.CreateSIUnit(file, IFCUnit.LengthUnit, prefix, IFCSIUnitName.Metre);
            }
            else
            {
               double lengthScaleFactor = 1.0;
               if (crsMapUnitStr.Equals("inch", StringComparison.InvariantCultureIgnoreCase))
               {
                  lengthScaleFactor = UnitUtils.ConvertFromInternalUnits(1.0, UnitTypeId.Inches);
               }
               else if (crsMapUnitStr.Equals("foot", StringComparison.InvariantCultureIgnoreCase))
               {
                  lengthScaleFactor = UnitUtils.ConvertFromInternalUnits(1.0, UnitTypeId.Feet);
               }
               else if (crsMapUnitStr.Equals("yard", StringComparison.InvariantCultureIgnoreCase))
               {
                  lengthScaleFactor = 1/3;
               }
               else if (crsMapUnitStr.Equals("mile", StringComparison.InvariantCultureIgnoreCase))
               {
                  lengthScaleFactor = 1/5280;
               }
               
               double lengthSIScaleFactor = UnitUtils.ConvertFromInternalUnits(1.0, UnitTypeId.Meters) / lengthScaleFactor;
               IFCAnyHandle lenDims = IFCInstanceExporter.CreateDimensionalExponents(file, 1, 0, 0, 0, 0, 0, 0); // length
               IFCAnyHandle lenSIUnit = IFCInstanceExporter.CreateSIUnit(file, IFCUnit.LengthUnit, null, IFCSIUnitName.Metre);
               IFCAnyHandle lenConvFactor = IFCInstanceExporter.CreateMeasureWithUnit(file, Toolkit.IFCDataUtil.CreateAsRatioMeasure(lengthSIScaleFactor),
                   lenSIUnit);

               crsMapUnit = IFCInstanceExporter.CreateConversionBasedUnit(file, lenDims, IFCUnit.LengthUnit, crsMapUnitStr, lenConvFactor);
            }
         }

         IFCAnyHandle projectedCRS = null;
         projectedCRS = IFCInstanceExporter.CreateProjectedCRS(file, epsgCode, crsDescription, crsGeodeticDatum, crsVerticalDatum, crsMapProjection, crsMapZone, crsMapUnit);

         // Only eastings, northings, and orthogonalHeight are mandatory beside the CRSSource (GeometricRepresentationContext) and CRSTarget (ProjectedCRS)
         double eastings = UnitUtils.ConvertFromInternalUnits(geoRefInfo.eastings, utId);
         double northings = UnitUtils.ConvertFromInternalUnits(geoRefInfo.northings, utId);
         double orthogonalHeight = UnitUtils.ConvertFromInternalUnits(geoRefInfo.orthogonalHeight, utId);
         IFCAnyHandle mapConversionHnd = IFCInstanceExporter.CreateMapConversion(file, geomRepContext, projectedCRS, eastings, northings, orthogonalHeight, xAxisAbscissa, xAxisOrdinate, scale);

         return true;
      }
   }
}