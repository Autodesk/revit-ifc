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
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.Structure;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Provides static methods for filtering elements.
   /// </summary>
   class ElementFilteringUtil
   {
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
      public static FilteredElementCollector GetExportElementCollector(
         Document document, bool useFilterViewIfExists)
      {
         ExportOptionsCache exportOptionsCache = ExporterCacheManager.ExportOptionsCache;
         ICollection<ElementId> idsToExport = exportOptionsCache.ElementsForExport;
         if (idsToExport.Count > 0)
         {
            return new FilteredElementCollector(document, idsToExport);
         }

         View filterView = useFilterViewIfExists ?
            exportOptionsCache.FilterViewForExport : null;

         if (filterView == null)
         {
            return new FilteredElementCollector(document);
         }

         if (ExporterStateManager.CurrentLinkId != ElementId.InvalidElementId)
         {
            return new FilteredElementCollector(filterView.Document, filterView.Id,
               ExporterStateManager.CurrentLinkId);
         }

         return new FilteredElementCollector(filterView.Document, filterView.Id);
      }

      /// <summary>
      /// Gets spatial element filter.
      /// </summary>
      /// <param name="document">The Revit document.</param>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <returns>The element filter.</returns>
      public static ElementFilter GetSpatialElementFilter(Document document, ExporterIFC exporterIFC)
      {
         return GetExportFilter(document, exporterIFC, true);
      }

      /// <summary>
      /// Gets the filtered non-spatial elements.
      /// </summary>
      /// <param name="document">The Revit document.</param>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <returns>The Element filter.</returns>
      public static ISet<ElementId> GetNonSpatialElements(Document document, ExporterIFC exporterIFC)
      {
         ICollection<ElementId> nonSpatialElements;
         if (ExporterCacheManager.ExportOptionsCache.ElementsForExport.Count > 0)
         {
            nonSpatialElements = ExporterCacheManager.ExportOptionsCache.ElementsForExport;
         }
         else
         {
            FilteredElementCollector otherElementCollector = GetExportElementCollector(document, true);
            ElementFilter nonSpatialElementFilter = GetExportFilter(document, exporterIFC, false);
            otherElementCollector.WherePasses(nonSpatialElementFilter);
            nonSpatialElements = otherElementCollector.ToElementIds();
         }

         ExporterCacheManager.NonSpatialElements.UnionWith(nonSpatialElements);

         return ExporterCacheManager.NonSpatialElements;
      }

      /// <summary>
      /// Gets element filter for export.
      /// </summary>
      /// <param name="document">The Revit document.</param>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="forSpatialElements">True to get spatial element filter, false for non spatial elements filter.</param>
      /// <returns>The element filter.</returns>
      private static ElementFilter GetExportFilter(Document document, ExporterIFC exporterIFC, bool forSpatialElements)
      {
         List<ElementFilter> filters = new List<ElementFilter>();

         // Class types & categories
         ElementFilter classFilter = GetClassFilter(forSpatialElements);

         // Special handling for family instances and view specific elements
         if (!forSpatialElements)
         {
            ElementFilter familyInstanceFilter = GetFamilyInstanceFilter(exporterIFC);

            List<ElementFilter> classFilters = new List<ElementFilter>();
            classFilters.Add(classFilter);
            classFilters.Add(familyInstanceFilter);

            if (ExporterCacheManager.ExportOptionsCache.ExportAnnotations)
            {
               ElementFilter ownerViewFilter = GetViewSpecificTypesFilter(exporterIFC);
               classFilters.Add(ownerViewFilter);
            }

            classFilter = new LogicalOrFilter(classFilters);
         }

         filters.Add(classFilter);

         // Design options
         filters.Add(GetDesignOptionFilter());

         // Phases: only for non-spatial elements.  For spatial elements, we will do a check afterwards.
         if (!forSpatialElements && ExporterUtil.ExportingHostModel())
            filters.Add(GetPhaseStatusFilter(document));

         return new LogicalAndFilter(filters);
      }

      /// <summary>
      /// Gets element filter for family instance.
      /// </summary>
      /// <param name="exporter">The ExporterIFC object.</param>
      /// <returns>The element filter.</returns>
      private static ElementFilter GetFamilyInstanceFilter(ExporterIFC exporter)
      {
         List<ElementFilter> filters = new List<ElementFilter>();
         filters.Add(new ElementOwnerViewFilter(ElementId.InvalidElementId));
         filters.Add(new ElementClassFilter(typeof(FamilyInstance)));
         LogicalAndFilter andFilter = new LogicalAndFilter(filters);

         return andFilter;
      }

      /// <summary>
      /// Gets element filter meeting design option requirements.
      /// </summary>
      /// <returns>The element filter.</returns>
      private static ElementFilter GetDesignOptionFilter()
      {
         // We will respect the active design option if we are exporting a specific view.
         ElementFilter noDesignOptionFilter = new ElementDesignOptionFilter(ElementId.InvalidElementId);
         ElementFilter primaryOptionsFilter = new PrimaryDesignOptionMemberFilter();
         ElementFilter designOptionFilter = new LogicalOrFilter(noDesignOptionFilter, primaryOptionsFilter);

         View filterView = ExporterCacheManager.ExportOptionsCache.FilterViewForExport;
         if (filterView != null)
         {
            ElementId designOptionId = DesignOption.GetActiveDesignOptionId(ExporterCacheManager.Document);
            if (designOptionId != ElementId.InvalidElementId)
            {
               ElementFilter activeDesignOptionFilter = new ElementDesignOptionFilter(designOptionId);
               return new LogicalOrFilter(designOptionFilter, activeDesignOptionFilter);
            }
         }

         return designOptionFilter;
      }

      /// <summary>
      /// Checks if an element with a given ExportIFCCategoryInfo should be exported.
      /// </summary>
      /// <param name="info">The exporting mapping information for a category.</param>
      /// <param name="element">The optional element.</param>
      /// <param name="allowSeparateOpeningExport">True if IfcOpeningElement is allowed to be exported.</param>
      /// <returns>True if the element should be exported, false otherwise.</returns>
      public static bool ShouldExportMappingInfo(ExportIFCCategoryInfo info, Element element, bool allowSeparateOpeningExport)
      {
         string entityName = info?.IFCEntityName;
         bool? exportFlag = info?.IFCExportFlag;

         // If the element is null, we won't do this check.  If the entityName is empty, it is
         // likely a sub-category that should defer to its parent to make a final decision.
         if (info == null && element != null)
         {
            if (element is AreaScheme || element is Group)
               entityName = "IfcGroup";
            else if (element is ElectricalSystem)
               entityName = "IfcSystem";
            else
               return false;

            exportFlag = true;
         }

         if (!(exportFlag ?? false))
            return false;

         if (!allowSeparateOpeningExport && (string.Compare(entityName, "IfcOpeningElement", true) == 0))
            return false;

         if (ExporterCacheManager.ExportOptionsCache.HasExcludeList())
         {
            if (ExporterCacheManager.ExportOptionsCache.IsEntityInExcludeList(entityName))
               return false;

            IFCExportInfoPair pair = new IFCExportInfoPair();
            pair.SetByTypeName(entityName);
            if (ExporterCacheManager.ExportOptionsCache.IsEntityInExcludeList(pair.ExportType.ToString()))
               return false;

         }

         return true;
      }

      /// <summary>
      /// Checks if element in certain category should be exported.
      /// </summary>
      /// <param name="allowSeparateOpeningExport">True if IfcOpeningElement is allowed to be exported.</param>
      /// <returns>True if the element should be exported, false otherwise.</returns>
      public static bool ShouldCategoryBeExported(Category category, bool allowSeparateOpeningExport)
      {
         ElementId categoryId = category?.Id ?? ElementId.InvalidElementId;
         if (ExporterUtil.GetCategoryInfoById(categoryId, null, out ExportIFCCategoryInfo info))
            return ShouldExportMappingInfo(info, null, allowSeparateOpeningExport);

         ElementId parentCategoryId = category?.Parent?.Id ?? ElementId.InvalidElementId;
         if (parentCategoryId != ElementId.InvalidElementId)
         {
            if (ExporterUtil.GetCategoryInfoById(parentCategoryId, null, out info))
               return ShouldExportMappingInfo(info, null, allowSeparateOpeningExport);
         }

         // the category is not in the mapping template
         return IsCategoryVisible(category, ExporterCacheManager.ExportOptionsCache.FilterViewForExport);
      }

      /// <summary>
      /// Checks if element in certain category should be exported.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="allowSeparateOpeningExport">True if IfcOpeningElement is allowed to be exported.</param>
      /// <returns>True if the element should be exported, false otherwise.</returns>
      private static bool ShouldCategoryBeExported(Element element, bool allowSeparateOpeningExport)
      {
         ExportIFCCategoryInfo info = ExporterUtil.GetIFCCategoryExportMappingInfo(element);
         return ShouldExportMappingInfo(info, element, allowSeparateOpeningExport);
      }

      /// <summary>
      /// Checks if an element should be exported based on parameter settings.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="elementType">The element type, if any.</param>
      /// <returns>An IFCExportElement value, calculated from several parameters, or null if inconclusive.</returns>
      /// <remarks>This routine will never return IFCExportElement.ByType: it will return Yes, No, or null.</remarks>
      public static IFCExportElement? GetExportElementState(Element element, Element elementType)
      {
         Parameter exportElement = element.get_Parameter(BuiltInParameter.IFC_EXPORT_ELEMENT);
         IFCExportElement value = (exportElement != null) ? (IFCExportElement)exportElement.AsInteger() : IFCExportElement.ByType;
         if (value != IFCExportElement.ByType)
            return value;

         // Element is ByType - look at the ElementType, if it exists.
         Parameter exportElementType = elementType?.get_Parameter(BuiltInParameter.IFC_EXPORT_ELEMENT_TYPE);
         IFCExportElementType typeValue = (exportElementType != null) ? (IFCExportElementType)exportElementType.AsInteger() : IFCExportElementType.Default;
         switch (typeValue)
         {
            case IFCExportElementType.No:
               return IFCExportElement.No;
            case IFCExportElementType.Yes:
               return IFCExportElement.Yes;
            case IFCExportElementType.Default:
               return null;
         }

         return null;
      }

      /// <summary>
      /// Checks if element should be exported using a variety of different checks.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="allowSeparateOpeningExport">True if IfcOpeningElement is allowed to be exported.</param>
      /// <returns>True if the element should be exported, false otherwise.</returns>
      /// <remarks>There are some inefficiencies here, as we call GetExportInfoFromParameters
      /// in other contexts.  We should attempt to get the value only once.</remarks>
      public static bool ShouldElementBeExported(Element element, bool allowSeparateOpeningExport)
      {
         // Allow the ExporterStateManager to say that an element should be exported regardless of settings.
         if (ExporterStateManager.CanExportElementOverride)
            return true;

         // First, check if the element is set explicitly to be exported or not exported.  This
         // overrides category settings.
         Element elementType = element.Document.GetElement(element.GetTypeId());
         IFCExportElement? exportElementState = GetExportElementState(element, elementType);
         if (exportElementState.HasValue)
            return exportElementState.Value == IFCExportElement.Yes;

         // Check to see if the category should be exported if parameters aren't set.
         // Note that in previous versions, the category override the parameter settings.  This is
         // no longer true.
         if (!ShouldCategoryBeExported(element, allowSeparateOpeningExport))
            return false;

         // Check whether the intended Entity type is inside the export exclusion set
         IFCExportInfoPair exportInfo = ExporterUtil.GetIFCExportElementParameterInfo(element, IFCEntityType.IfcRoot);
         return !ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(exportInfo.ExportInstance);
      }

      /// <summary>
      /// Determines if the selected element meets extra criteria for export.
      /// </summary>
      /// <param name="element">The current element to export.</param>
      /// <param name="allowSeparateOpeningExport">True if IfcOpeningElement is allowed to be exported.</param>
      /// <returns>True if the element should be exported, false otherwise.</returns>
      public static bool CanExportElement(Element element, bool allowSeparateOpeningExport)
      {
         if (!ShouldElementBeExported(element, allowSeparateOpeningExport))
            return false;

         // if we allow exporting parts as independent building elements, then prevent also exporting the host elements containing the parts.
         bool checkIfExportingPart = ExporterCacheManager.ExportOptionsCache.ExportPartsAsBuildingElements || element is Part;
         if (checkIfExportingPart && PartExporter.CanExportParts(element))
            return false;

         return true;
      }

      static readonly Dictionary<string, IFCEntityType> PreIFC4Remap = new Dictionary<string, IFCEntityType>()
      {
         { "IFCAUDIOVISUALAPPLIANCE", IFCEntityType.IfcElectricApplianceType },
         { "IFCBURNER", IFCEntityType.IfcGasTerminalType },
         { "IFCELECTRICDISTRIBUTIONBOARD", IFCEntityType.IfcElectricDistributionPoint }
      };

      static readonly Dictionary<string, IFCEntityType> IFC4Remap = new Dictionary<string, IFCEntityType>()
      {
         { "IFCGASTERMINAL", IFCEntityType.IfcBurnerType },
         { "IFCELECTRICDISTRIBUTIONPOINT", IFCEntityType.IfcElectricDistributionBoardType },
         { "IFCELECTRICHEATER", IFCEntityType.IfcSpaceHeaterType }
      };

      /// <summary>
      /// Gets export type from IFC class name.
      /// </summary>
      /// <param name="originalIFCClassName">The IFC class name.</param>
      /// <returns>The export type.</returns>
      public static IFCExportInfoPair GetExportTypeFromClassName(string originalIFCClassName)
      {
         IFCExportInfoPair exportInfoPair = new IFCExportInfoPair();

         string cleanIFCClassName = originalIFCClassName.Trim().ToUpper();
         if (cleanIFCClassName.StartsWith("IFC"))
         {
            // Here we try to catch any possible types that are missing above by checking both the class name or the type name
            // Unless there is any special treatment needed most of the above check can be done here
            string clName = cleanIFCClassName.EndsWith("TYPE") ?
               cleanIFCClassName.Substring(0, cleanIFCClassName.Length - 4) : cleanIFCClassName;
            
            // Deal with small number of IFC2x3/IFC4 types that have changed in a hardwired way.
            if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
            {
               if (PreIFC4Remap.TryGetValue(clName, out IFCEntityType ifcEntityType))
                  exportInfoPair.SetByType(ifcEntityType);
               else
                  exportInfoPair.SetByTypeName(clName);
            }
            else
            {
               if (IFC4Remap.TryGetValue(clName, out IFCEntityType ifcEntityType))
                  exportInfoPair.SetByType(ifcEntityType);
               else
                  exportInfoPair.SetByTypeName(clName);
            }

            if (exportInfoPair.ExportInstance == IFCEntityType.UnKnown)
               exportInfoPair.SetByType(IFCEntityType.IfcBuildingElementProxy);
         }

         exportInfoPair.PredefinedType = null;

         return exportInfoPair;
      }

      static readonly Dictionary<BuiltInCategory, (IFCEntityType, string)> CategoryToExportType = new Dictionary<BuiltInCategory, (IFCEntityType, string)>() {
         { BuiltInCategory.OST_Cornices, (IFCEntityType.IfcBeam, "NOTDEFINED") },
         { BuiltInCategory.OST_Ceilings, (IFCEntityType.IfcCovering, "NOTDEFINED") },
         { BuiltInCategory.OST_CurtainWallPanels, (IFCEntityType.IfcPlate, "CURTAIN_PANEL") },
         { BuiltInCategory.OST_Furniture, (IFCEntityType.IfcFurniture, "NOTDEFINED") },
         { BuiltInCategory.OST_Floors, (IFCEntityType.IfcSlab, "FLOOR") },
         { BuiltInCategory.OST_IOSModelGroups, (IFCEntityType.IfcGroup, "NOTDEFINED") },
         { BuiltInCategory.OST_Mass, (IFCEntityType.IfcBuildingElementProxy, "NOTDEFINED") },
         { BuiltInCategory.OST_CurtainWallMullions, (IFCEntityType.IfcMember, "MULLION") },
         { BuiltInCategory.OST_Railings, (IFCEntityType.IfcRailing, "NOTDEFINED") },
         { BuiltInCategory.OST_Ramps, (IFCEntityType.IfcRamp, "NOTDEFINED") },
         { BuiltInCategory.OST_Roofs, (IFCEntityType.IfcRoof, "NOTDEFINED") },
         { BuiltInCategory.OST_Site, (IFCEntityType.IfcSite, "NOTDEFINED") },
         { BuiltInCategory.OST_Stairs, (IFCEntityType.IfcStair, "NOTDEFINED") },
         { BuiltInCategory.OST_Walls, (IFCEntityType.IfcWall, "NOTDEFINED") },
         { BuiltInCategory.OST_Windows, (IFCEntityType.IfcWindow, "NOTDEFINED") }
      };

      /// <summary>
      /// Gets export type from category id.
      /// </summary>
      /// <param name="categoryId">The category id.</param>
      /// <returns>The export type.</returns>
      public static IFCExportInfoPair GetExportTypeFromCategoryId(ElementId categoryId)
      {
         (IFCEntityType, string) exportInfoPair;
         BuiltInCategory builtInCategory = (BuiltInCategory)categoryId.Value;
         if (CategoryToExportType.TryGetValue(builtInCategory, out exportInfoPair))
            return new IFCExportInfoPair(exportInfoPair.Item1, exportInfoPair.Item2);
         return new IFCExportInfoPair();
      }

      /// <summary>
      /// Gets element filter for specific views.
      /// </summary>
      /// <param name="exporter">The ExporterIFC object.</param>
      /// <returns>The element filter.</returns>
      private static ElementFilter GetViewSpecificTypesFilter(ExporterIFC exporter)
      {
         ElementFilter ownerViewFilter = GetOwnerViewFilter(exporter);

         List<Type> viewSpecificTypes = new List<Type>()
         {
            typeof(TextNote),
            typeof(FilledRegion)
         };

         ElementMulticlassFilter classFilter = new ElementMulticlassFilter(viewSpecificTypes);

         LogicalAndFilter viewSpecificTypesFilter = new LogicalAndFilter(ownerViewFilter, classFilter);
         return viewSpecificTypesFilter;
      }

      /// <summary>
      /// Gets element filter to match elements which are owned by a particular view.
      /// </summary>
      /// <param name="exporter">The exporter.</param>
      /// <returns>The element filter.</returns>
      private static ElementFilter GetOwnerViewFilter(ExporterIFC exporter)
      {
         List<ElementFilter> filters = new List<ElementFilter>();
         ICollection<ElementId> viewIds = ExporterCacheManager.DBViewsToExport.Keys;
         foreach (ElementId id in viewIds)
         {
            filters.Add(new ElementOwnerViewFilter(id));
         }
         filters.Add(new ElementOwnerViewFilter(ElementId.InvalidElementId));
         LogicalOrFilter viewFilters = new LogicalOrFilter(filters);

         return viewFilters;
      }

      /// <summary>
      /// Gets element filter that match certain types.
      /// </summary>
      /// <param name="forSpatialElements">True if to get filter for spatial element, false for other elements.</param>
      /// <returns>The element filter.</returns>
      private static ElementFilter GetClassFilter(bool forSpatialElements)
      {
         if (forSpatialElements)
         {
            return new ElementClassFilter(typeof(SpatialElement));
         }
         else
         {
            List<Type> excludedTypes = new List<Type>
            {
               typeof(AnalyticalElement),

               // AreaScheme elements are exported as groups after all Areas have been
               // exported.
               typeof(AreaScheme),

               // curtain wall sub-types we are ignoring.
               typeof(CurtainGridLine),
               // typeof(Mullion),
      
               typeof(ElevationMarker),

               // FabricArea elements are exported as groups after all FabricSheets have
               // been exported.
               typeof(FabricArea),

               // exclude levels that are covered in BeginExport
               typeof(Level),

               // this will be gotten from the element(s) it cuts.
               typeof(Opening),

               // Spatial element are exported in a separate pass.
               typeof(SpatialElement),
   
               // 2D types we are ignoring
               typeof(FaceSplitter),
               typeof(Revision),
               typeof(SketchBase),

                // 2D types covered by the element owner view filter
               typeof(FilledRegion),
               typeof(TextNote),

               typeof(BaseArray),
               typeof(ColorFillScheme),
               typeof(ElementType),
               typeof(GraphicsStyle),
               typeof(Family),
               typeof(FamilyInstance),
               typeof(FillPatternElement),
               typeof(InternalOrigin),
               typeof(LinePatternElement),
               typeof(LoadBase),
               typeof(LoadCase),
               typeof(Material),
               typeof(Phase),
               typeof(SketchPlane),
               typeof(SunAndShadowSettings),
               typeof(View)
            };

            ElementFilter excludedClassFilter = new ElementMulticlassFilter(excludedTypes,
               true);

            List<BuiltInCategory> excludedCategories = new List<BuiltInCategory>()
            { 
               // Native Revit types without match in API
               BuiltInCategory.OST_ConduitCenterLine,
               BuiltInCategory.OST_ConduitFittingCenterLine,
               BuiltInCategory.OST_DecalElement,
               //BuiltInCategory.OST_Parts,
               //BuiltInCategory.OST_RvtLinks,
               BuiltInCategory.OST_DuctCurvesCenterLine,
               BuiltInCategory.OST_DuctFittingCenterLine,
               BuiltInCategory.OST_FlexDuctCurvesCenterLine,
               BuiltInCategory.OST_FlexPipeCurvesCenterLine,
               BuiltInCategory.OST_HVAC_Load_Schedules,
               BuiltInCategory.OST_IOS_GeoLocations,
               BuiltInCategory.OST_IOSSketchGrid,
               BuiltInCategory.OST_PipeCurvesCenterLine,
               BuiltInCategory.OST_PipeFittingCenterLine,
               BuiltInCategory.OST_Property,
               BuiltInCategory.OST_SitePropertyLineSegment,
               BuiltInCategory.OST_TopographyContours,
               BuiltInCategory.OST_Viewers,
               BuiltInCategory.OST_Viewports,
               BuiltInCategory.OST_Views,

               // Exclude elements with no category. 
               BuiltInCategory.INVALID
            };

            ElementMulticategoryFilter excludedCategoryFilter = 
               new ElementMulticategoryFilter(excludedCategories, true);

            LogicalAndFilter exclusionFilter = new LogicalAndFilter(excludedClassFilter, 
               excludedCategoryFilter);

            ElementOwnerViewFilter ownerViewFilter = 
               new ElementOwnerViewFilter(ElementId.InvalidElementId);

            LogicalAndFilter returnedFilter = new LogicalAndFilter(exclusionFilter, 
               ownerViewFilter);

            return returnedFilter;
         }
      }

      /// <summary>
      /// Checks if the room is in an invalid phase.
      /// </summary>
      /// <param name="element">The element, which may or may not be a room element.</param>
      /// <returns>True if the element is in the room, has a phase set, which is different from the active phase.</returns>
      public static bool IsRoomInInvalidPhase(Element element)
      {
         if (element is Room)
         {
            Parameter phaseParameter = element.get_Parameter(BuiltInParameter.ROOM_PHASE);
            if (phaseParameter != null)
            {
               ElementId phaseId = phaseParameter.AsElementId();
               if (phaseId != ElementId.InvalidElementId && phaseId != ExporterCacheManager.ExportOptionsCache.ActivePhaseId)
                  return true;
            }
         }

         return false;
      }

      /// <summary>
      /// Gets element filter that match certain phases. 
      /// </summary>
      /// <param name="document">The Revit document.</param>
      /// <returns>The element filter.</returns>
      private static ElementFilter GetPhaseStatusFilter(Document document)
      {
         ElementId phaseId = ExporterCacheManager.ExportOptionsCache.ActivePhaseId;

         List<ElementOnPhaseStatus> phaseStatuses = new List<ElementOnPhaseStatus>();
         phaseStatuses.Add(ElementOnPhaseStatus.None);  //include "none" because we might want to export phaseless elements.
         phaseStatuses.Add(ElementOnPhaseStatus.Existing);
         phaseStatuses.Add(ElementOnPhaseStatus.New);

         return new ElementPhaseStatusFilter(phaseId, phaseStatuses);
      }

      private static IDictionary<ElementId, bool> m_CategoryVisibilityCache = new Dictionary<ElementId, bool>();

      /// <summary>
      /// Initialize the category visibility cache
      /// </summary>
      public static void InitCategoryVisibilityCache()
      {
         m_CategoryVisibilityCache.Clear();
      }

      private static bool ProcessingLink()
      {
         return ExporterCacheManager.ExportOptionsCache.HostViewId != ElementId.InvalidElementId ||
            ExporterStateManager.CurrentLinkId != ElementId.InvalidElementId;
      }

      /// <summary>
      /// Checks if a category is visible for certain view.
      /// </summary>
      /// <param name="category">The category.</param>
      /// <param name="filterView">The view.</param>
      /// <returns>True if the category is visible, false otherwise.</returns>
      public static bool IsCategoryVisible(Category category, View filterView)
      {
         // This routine is generally used to decide whether or not to export geometry assigned to a praticular category.
         // Default behavior is to return true, even for a null category.  In general, we want to err on the side of showing geometry over hiding it.
         if (category == null || filterView == null)
            return true;

         bool isVisible;
         if (m_CategoryVisibilityCache.TryGetValue(category.Id, out isVisible))
            return isVisible;

         if (category.Id.Value > 0 && ProcessingLink())
         {
            // We don't support checking the visibility of link document custom categories
            // in the host view here.  We will use a different filter for this.
            isVisible = true;
         }
         else
         {
            // The category will be visible if either we don't allow visibility controls (default: true), or
            // we do allow visibility controls and the category is visible in the view.
            isVisible = (!category.get_AllowsVisibilityControl(filterView) || category.get_Visible(filterView));
         }

         m_CategoryVisibilityCache[category.Id] = isVisible;
         return isVisible;
      }

      /// <summary>
      /// Checks if element is visible for certain view.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <returns>True if the element is visible, false otherwise.</returns>
      public static bool IsElementVisible(Element element)
      {
         View filterView = ExporterCacheManager.ExportOptionsCache.FilterViewForExport;
         if (filterView == null)
            return true;

         bool hidden = element.IsHidden(filterView);
         if (hidden)
            return false;

         Category category = CategoryUtil.GetSafeCategory(element);
         hidden = !IsCategoryVisible(category, filterView);
         if (hidden)
            return false;

         if (ProcessingLink())
            return true;

         return filterView.IsElementVisibleInTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate, element.Id);
      }

      /// <summary>
      /// Checks if the IFC type is MEP type.
      /// </summary>
      /// <param name="exportType">IFC Export Type to check</param>
      /// <returns>True for MEP type of elements.</returns>
      public static bool IsMEPType(IFCExportInfoPair exportType)
      {
         bool instanceIsMEPInst = IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportInstance.ToString(), IFCEntityType.IfcDistributionElement.ToString(), strict:false);

         // The Type probably is not needed for check?
         bool typeIsMEPType = IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportType.ToString(), IFCEntityType.IfcDistributionElementType.ToString(), strict:false);

         return (instanceIsMEPInst);
      }

      /// <summary>
      /// Check if an element assigned to IfcBuildingElementProxy is of MEP Type (by checking its connectors) to enable IfcBuildingElementProxy to take part
      /// in the System component and connectivity
      /// </summary>
      /// <param name="element">The element</param>
      /// <param name="exportType">IFC Export Type to check: only for IfcBuildingElementProxy or IfcBuildingElementProxyType</param>
      /// <returns></returns>
      public static bool ProxyForMEPType(Element element, IFCExportInfoPair exportType)
      {
         if ((exportType.ExportInstance == IFCEntityType.IfcBuildingElementProxy) || (exportType.ExportType == IFCEntityType.IfcBuildingElementProxyType))
         {
            try
            {
               if (element is FamilyInstance)
               {
                  MEPModel m = ((FamilyInstance)element).MEPModel;
                  if (m != null && m.ConnectorManager != null)
                  {
                     return true;
                  }
               }
               else
                  return false;
            }
            catch
            {
            }
         }

         return false;
      }

      /// <summary>
      /// Get valid IFC entity type by using the official IFC schema (using the XML schema). It checks the non-abstract valid entity. 
      /// If it is found to be abstract, it will try to find its supertype until it finds a non-abstract type.  
      /// </summary>
      /// <param name="entityType">the IFC entity type (string) to check</param>
      /// <returns>return the appropriate IFCEntityType enumeration or Unknown</returns>
      public static IFCEntityType GetValidIFCEntityType (string entityType)
      {
         IFCVersion ifcVersion = ExporterCacheManager.ExportOptionsCache.FileVersion;
         IFCEntityType ret = IFCEntityType.UnKnown;

         var ifcEntitySchemaTree = IfcSchemaEntityTree.GetEntityDictFor(ExporterCacheManager.ExportOptionsCache.FileVersion);
         if (ifcEntitySchemaTree == null || ifcEntitySchemaTree.IfcEntityDict == null || ifcEntitySchemaTree.IfcEntityDict.Count == 0)
            throw new Exception("Unable to locate IFC Schema xsd file! Make sure the relevant xsd " + ExporterCacheManager.ExportOptionsCache.FileVersion + " exists.");

         IfcSchemaEntityNode node = ifcEntitySchemaTree.Find(entityType);
         IFCEntityType ifcType = IFCEntityType.UnKnown;
         if (node != null && !node.isAbstract)
         {
            // Only IfcProduct or IfcTypeProduct can be assigned for export type
            //if (!node.IsSubTypeOf("IfcProduct") && !node.IsSubTypeOf("IfcTypeProduct") && !node.Name.Equals("IfcGroup", StringComparison.InvariantCultureIgnoreCase))
            if ((node.IsSubTypeOf("IfcObject") && 
                     (node.IsSubTypeOf("IfcProduct") || node.IsSubTypeOf("IfcGroup") || node.Name.Equals("IfcGroup", StringComparison.InvariantCultureIgnoreCase)))
                  || node.IsSubTypeOf("IfcProject") || node.Name.Equals("IfcProject", StringComparison.InvariantCultureIgnoreCase)
                  || node.IsSubTypeOf("IfcTypeObject") || node.Name.Equals("IfcMaterial", StringComparison.InvariantCultureIgnoreCase))
            {
               if (IFCEntityType.TryParse(entityType, true, out ifcType))
                  ret = ifcType;
            }
            else
               ret = ifcType;
         }
         else if (node != null && node.isAbstract)
         {
            node = IfcSchemaEntityTree.FindNonAbsSuperType(ifcVersion, entityType, "IfcProduct", "IfcProductType", "IfcGroup", "IfcProject");
            if (node != null)
            {
               if (Enum.TryParse<IFCEntityType>(node.Name, true, out ifcType))
                  ret = ifcType;
            }
         }

         return ret;
      }

      /// <summary>
      /// Get valid IFC entity type by using the official IFC schema (using the XML schema). It checks the non-abstract valid entity. 
      /// If it is found to be abstract, it will try to find its supertype until it finds a non-abstract type. 
      /// </summary>
      /// <param name="entityType">the IFC Entity type enum</param>
      /// <returns>return the appropriate entity type or Unknown</returns>
      public static IFCEntityType GetValidIFCEntityType (IFCEntityType entityType)
      {
         return GetValidIFCEntityType(entityType.ToString());
      }
   }
}