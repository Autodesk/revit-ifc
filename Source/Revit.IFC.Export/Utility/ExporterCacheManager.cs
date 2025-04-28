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

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;

using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;


namespace Revit.IFC.Export.Utility
{
   // Alias to make it easier to deal with ExportInfoCache.
   using ExportTypeInfo = Tuple<IFCExportInfoPair, string, ExportTypeOverrideHelper>;

   /// <summary>
   /// Manages caches necessary for IFC export.
   /// </summary>
   public class ExporterCacheManager
   {
      /// <summary>
      /// The AllocatedGeometryObjectCache object.
      /// </summary>
      public static AllocatedGeometryObjectCache AllocatedGeometryObjectCache { get; protected set; } = new();

      /// <summary>
      /// The AreaSchemeCache object.
      /// </summary>
      public static Dictionary<ElementId, HashSet<IFCAnyHandle>> AreaSchemeCache = new();

      /// <summary>
      /// The AssemblyInstanceCache object.
      /// </summary>
      public static AssemblyInstanceCache AssemblyInstanceCache { get; private set; } = new();

      public static AttributeCache AttributeCache { get; private set; } = new();

      /// <summary>
      /// The base guid to use for all entities when exporting, used when exporting linked documents.
      /// </summary>
      public static string BaseLinkedDocumentGUID { get; set; } = null;

      /// <summary>
      /// Cache for Base Quantities that require separate calculation.
      /// </summary>
      public static Dictionary<IFCAnyHandle, HashSet<IFCAnyHandle>> BaseQuantitiesCache { get; private set; } = new();

      /// <summary>
      /// A mapping of element ids to a material id determined by looking at element parameters.
      /// </summary>
      /// <summary>
      /// The BeamSystemCache object.
      /// </summary>
      public static HashSet<ElementId> BeamSystemCache { get; private set; } = new();

      /// <summary>
      /// The IfcBuilding handle.
      /// </summary>
      public static IFCAnyHandle BuildingHandle { get; set; } = null;

      /// <summary>
      /// A cache to keep track of what beams can be exported as extrusions.
      /// Strictly for performance issues.
      /// </summary>
      public static Dictionary<ElementId, bool> CanExportBeamGeometryAsExtrusionCache { get; private set; } = new();

      private static IFCCategoryTemplate m_CategoryMappingTemplate = null;

      /// <summary>
      /// Ceiling and Space relationship cache. We need it to check whether a Ceiling should be contained in a Space later on when exporting Ceiling
      /// </summary>
      public static Dictionary<ElementId, IList<ElementId>> CeilingSpaceRelCache { get; private set; } = new();

      /// <summary>
      /// The CertifiedEntitiesAndPsetsCache
      /// </summary>
      public static IFCCertifiedEntitiesAndPSets CertifiedEntitiesAndPsetsCache { get; private set; } = new();

      private static ClassificationCache m_ClassificationCache = null;

      public static ClassificationLocationCache ClassificationLocationCache { get; private set; } = new();

      /// <summary>
      /// Cache for additional Quantities or Properties to be created later with the other quantities
      /// </summary>
      public static Dictionary<IFCAnyHandle, HashSet<IFCAnyHandle>> ComplexPropertyCache { get; private set; } = new();

      public static ContainmentCache ContainmentCache { get; private set; } = new();

      /// <summary>
      /// The top level 2D context handles by identifier.
      /// </summary>
      private static Dictionary<IFCRepresentationIdentifier, IFCAnyHandle> Context2DHandles { get; set; } = new();

      /// <summary>
      /// The top level 3D context handles by identifier.
      /// </summary>
      private static Dictionary<IFCRepresentationIdentifier, IFCAnyHandle> Context3DHandles { get; set; } = new();

      /// <summary>
      /// Cache for "special" property sets to make sure we don't re-export them.
      /// </summary>
      /// <remarks>
      /// At the moment, this is only for Pset_Draughting for 2x2.  But really we
      /// should combine this with CreatedInternalPropertySets.
      /// </remarks>
      public static PropertySetCache CreatedSpecialPropertySets { get; private set; } = new();

      public static PropertySetCache CreatedInternalPropertySets { get; private set; } = new();

      /// <summary>
      /// The CurveAnnotationCache object.
      /// </summary>
      public static CurveAnnotationCache CurveAnnotationCache { get; private set; } = new();

      /// <summary>
      /// A convenience function to check if we are exporting IFC base quantities.
      /// </summary>
      /// <returns>True if we are exporting base quantities.</returns>
      static public bool ExportIFCBaseQuantities() { return ExportOptionsCache.PropertySetOptions.ExportIFCBaseQuantities; }

      ///<summary>
      /// A map containing the level to export for a particular view.
      /// </summary>
      public static Dictionary<ElementId, ElementId> DBViewsToExport { get; private set; } = new();

      private static IFCAnyHandle m_DefaultCartesianTransformationOperator3D = null;

      /// <summary>
      /// The Document object passed to the Exporter class.
      /// </summary>
      public static Document Document { get; set; } = null;

      /// <summary>
      /// The cache containing the openings that need to be created for doors and windows.
      /// </summary>
      public static DoorWindowDelayedOpeningCreatorCache DoorWindowDelayedOpeningCreatorCache { get; private set; } = new();

      private static Units m_DocumentUnits = null;

      /// <summary>
      /// A cache of Document.GetUnits().
      /// </summary>
      public static Units DocumentUnits
      {
         get
         {
            m_DocumentUnits ??= Document.GetUnits();
            return m_DocumentUnits;
         }
      }

      /// <summary>
      /// The DummyHostCache object.
      /// </summary>
      public static DummyHostCache DummyHostCache { get; private set; } = new();

      public static Dictionary<ElementId, ElementId> ElementIdMaterialParameterCache { get; private set; } = new();

      /// <summary>
      /// The elements in assemblies cache.
      /// </summary>
      public static HashSet<IFCAnyHandle> ElementsInAssembliesCache { get; private set; } = new();

      /// <summary>
      /// The ElementToHandleCache object, used to cache Revit element ids to IFC entity handles.
      /// </summary>
      public static ElementToHandleCache ElementToHandleCache { get; private set; } = new();

      /// <summary>
      /// The ElementTypeToHandleCache object, used to cache Revit element type ids to IFC entity handles.
      /// </summary>
      public static ElementTypeToHandleCache ElementTypeToHandleCache { get; private set; } = new();

      private static bool? m_ExportCeilingGrids { get; set; } = null;

      /// <summary>
      /// The ExporterIFC used to access internal IFC API functions.
      /// </summary>
      public static ExporterIFC ExporterIFC { get; set; } = null;

      /// <summary>
      /// The ExportOptionsCache object.
      /// </summary>
      public static ExportOptionsCache ExportOptionsCache { get; set; } = new();

      /// <summary>
      /// The ContainmentCache object.
      /// </summary>
      /// <summary>
      /// On each export, each Element will always have one and always one IFCExportInfoPair class associated with it.
      /// This cache keeps track of that.
      /// </summary>
      public static Dictionary<ElementId, ExportTypeInfo> ExportTypeInfoCache { get; private set; } = new();

      /// <summary>
      /// The FabricArea id to FabricSheet handle cache.
      /// </summary>
      public static Dictionary<ElementId, HashSet<IFCAnyHandle>> FabricAreaHandleCache { get; private set; } = new();

      public static Dictionary<ElementId, FabricParams> FabricParamsCache { get; private set; } = new();

      /// <summary>
      /// Keeps track of the active IFC parameter mapping template.
      /// </summary>
      static IFCParameterTemplate m_ParameterMappingTemplate = null;

      /// <summary>
      /// The FamilySymbolToTypeInfoCache object.  This maps a FamilySymbol id to the related created IFC information (the TypeObjectsCache).
      /// </summary>
      public static TypeObjectsCache FamilySymbolToTypeInfoCache { get; private set; } = new();

      private static IFCAnyHandle m_Global3DOriginHandle = null;

      /// <summary>
      /// The GridCache object.
      /// </summary>
      public static List<Element> GridCache { get; private set; } = new();

      /// <summary>
      /// The GroupCache object.
      /// </summary>
      public static GroupCache GroupCache { get; private set; } = new();

      /// <summary>
      /// The GUIDCache object.
      /// </summary>
      public static HashSet<string> GUIDCache { get; } = new();

      /// <summary>
      /// The GUIDs to store in elements at the end of export, if the option to store GUIDs has been selected.
      /// </summary>
      public static Dictionary<(ElementId, BuiltInParameter), string> GUIDsToStoreCache { get; private set; } = new();

      /// <summary>
      /// Collection of IFC Handles to delete
      /// </summary>
      public static HashSet<IFCAnyHandle> HandleToDeleteCache { get; private set; } = new();

      /// <summary>
      /// The HandleToElementCache object.
      /// </summary>
      public static HandleToElementCache HandleToElementCache { get; private set; } = new();

      /// <summary>
      /// This contains the mapping from Level element id to index in the IList returned by GetHostObjects.
      /// This is redundant with a native list that is being deprecated, which has inadequate API access.
      /// </summary>
      public static Dictionary<ElementId, int> HostObjectsLevelIndex { get; private set; } = new();

      /// <summary>
      /// The HostPartsCache object.
      /// </summary>
      public static HostPartsCache HostPartsCache { get; private set; } = new();

      /// <summary>
      /// A cache of internally created IfcRoot-derived handles.
      /// </summary>
      /// <remarks></remarks>
      public static Dictionary<IFCAnyHandle, ElementId> InternallyCreatedRootHandles { get; private set; } = new();

      /// <summary>
      /// The IsExternalParameterValueCache object.
      /// </summary>
      public static Dictionary<ElementId, bool> IsExternalParameterValueCache { get; private set; } = new();

      /// <summary>
      /// The language of the current Revit document.
      /// </summary>
      public static LanguageType LanguageType { get; set; } = LanguageType.Unknown;

      /// <summary>
      /// The precision used in the IfcRepresentationContext in Revit units.
      /// </summary>
      public static double LengthPrecision { get; set; } = MathUtil.Eps();

      /// <summary>
      /// The LevelInfoCache object.  This contains extra information on top of
      /// IFCLevelInfo, and will eventually replace it.
      /// </summary>
      public static LevelInfoCache LevelInfoCache { get; private set; } = new();


      /// <summary>
      /// The MaterialConstituent to IfcMaterial cache
      /// </summary>
      public static MaterialConstituentCache MaterialConstituentCache { get; private set; } = new();

      /// <summary>
      /// The MaterialConstituentSet cache
      /// </summary>
      public static MaterialConstituentSetCache MaterialConstituentSetCache { get; private set; } = new();

      /// <summary>
      /// The MaterialHandleCache object.
      /// </summary>
      public static ElementToHandleCache MaterialHandleCache { get; private set; } = new();

      /// <summary>
      /// The material id to style handle cache.
      /// </summary>
      public static ElementToHandleCache MaterialIdToStyleHandleCache { get; private set; } = new();

      /// <summary>
      /// The MaterialRelationsCache object.
      /// </summary>
      public static MaterialRelationsCache MaterialRelationsCache { get; private set; } = new();

      /// <summary>
      /// The Material___SetCache object (includes IfcMaterialLayerSet, IfcMaterialProfileSet, IfcMaterialConstituentSet in IFC4).
      /// </summary>
      public static MaterialSetCache MaterialSetCache { get; private set; } = new();

      /// <summary>
      /// The MaterialLayerRelationsCache object.
      /// </summary>
      public static MaterialSetUsageCache MaterialSetUsageCache { get; private set; } = new();

      /// <summary>
      /// The MEPCache object.
      /// </summary>
      public static MEPCache MEPCache { get; private set; } = new();

      /// <summary>
      /// Non-spatial Elements (e.g., Floor) for export.
      /// </summary>
      public static HashSet<ElementId> NonSpatialElements { get; private set; } = new();

      /// <summary>
      /// The Cache for 2D curves information of a FamilySymbol
      /// </summary>
      public static Dictionary<ElementId, IList<Curve>> Object2DCurvesCache { get; private set; } = new();

      /// <summary>
      /// The top level IfcOwnerHistory handle.
      /// </summary>
      public static IFCAnyHandle OwnerHistoryHandle { get; set; } = null;

      /// <summary>
      /// The ParameterCache object.
      /// </summary>
      public static ParameterCache ParameterCache { get; private set; } = new();

      /// <summary>
      /// The top level IfcProject handle.
      /// </summary>
      public static IFCAnyHandle ProjectHandle { get; set; } = null;

      /// <summary>
      /// The PartExportedCache object.
      /// </summary>
      public static PartExportedCache PartExportedCache { get; private set; } = new();

      /// <summary>
      /// The PresentationLayerSetCache object.
      /// </summary>
      public static PresentationLayerSetCache PresentationLayerSetCache { get; private set; } = new();

      /// <summary>
      /// The PresentationStyleAssignmentCache object.
      /// </summary>
      public static PresentationStyleAssignmentCache PresentationStyleAssignmentCache { get; private set; } = new();

      private static IDictionary<Tuple<string, string>, string> m_PropertyMapCache = null;

      /// Cache for information whether a QuantitySet specified in the Dict. value has been created for the elementHandle
      /// </summary>
      public static HashSet<(IFCAnyHandle, string)> QtoSetCreated { get; private set; } = new();

      /// <summary>
      /// The predefined property sets to be exported for an entity type, regardless of Object Type.
      /// </summary>
      public static Dictionary<PropertySetKey, IList<PreDefinedPropertySetDescription>> PreDefinedPropertySetsForTypeCache { get; private set; } = new();

      /// <summary>
      /// The PropertyInfoCache object.
      /// </summary>
      public static PropertyInfoCache PropertyInfoCache { get; private set; } = new();

      /// <summary>
      /// The common property sets to be exported for an entity type, regardless of Object Type.
      /// </summary>
      public static Dictionary<PropertySetKey, IList<PropertySetDescription>> PropertySetsForTypeCache { get; private set; } = new();

      /// <summary>
      /// The RailingCache object.
      /// </summary>
      public static HashSet<ElementId> RailingCache { get; private set; } = new();

      /// <summary>
      /// The RailingSubElementCache object.  This ensures that we don't export sub-elements of railings (e.g. supports) separately.
      /// </summary>
      public static HashSet<ElementId> RailingSubElementCache { get; private set; } = new();

      /// <summary>
      /// Cache for the Project Location that comes from the Selected Site on export option
      /// </summary>
      public static ProjectLocation SelectedSiteProjectLocation { get; set; } = null;

      /// <summary>
      /// This keeps track of IfcSite-related information.
      /// </summary>
      public static SiteExportInfo SiteExportInfo { get; private set; } = new();

      /// <summary>
      /// The SpaceBoundaryCache object.
      /// </summary>
      public static SpaceBoundaryCache SpaceBoundaryCache { get; private set; } = new();

      /// <summary>
      /// The SpaceInfo cache that maps Revit SpatialElement id to the SpaceInfo.
      /// </summary>
      public static SpaceInfoCache SpaceInfoCache { get; private set; } = new();

      /// <summary>
      /// The SpaceOccupantInfoCache object.
      /// </summary>
      public static SpaceOccupantInfoCache SpaceOccupantInfoCache { get; private set; } = new();

      /// <summary>
      /// The StairRampContainerInfoCache object.
      /// </summary>
      public static StairRampContainerInfoCache StairRampContainerInfoCache { get; private set; } = new();

      /// <summary>
      /// The SystemsCache object.
      /// </summary>
      public static SystemsCache SystemsCache { get; private set; } = new();

      /// <summary>
      /// The TrussCache object.
      /// </summary>
      public static HashSet<ElementId> TrussCache { get; private set; } = new();

      /// <summary>
      /// A container of geometry associated with temporary parts.
      /// </summary>
      public static TemporaryPartsCache TemporaryPartsCache { get; private set; } = new();

      /// <summary>
      /// The TypePropertyInfoCache object.
      /// </summary>
      public static TypePropertyInfoCache TypePropertyInfoCache { get; private set; } = new();

      /// <summary>
      /// The TypeRelationsCache object.
      /// </summary>
      public static TypeRelationsCache TypeRelationsCache { get; private set; } = new();

      /// <summary>
      /// The UnitsCache object.
      /// </summary>
      public static UnitsCache UnitsCache { get; private set; } = new();

      /// <summary>
      /// The ViewScheduleElementCache object.
      /// </summary>
      public static Dictionary<ElementId, HashSet<ElementId>> ViewScheduleElementCache { get; private set; } = new();

      /// <summary>
      /// The WallConnectionDataCache object.
      /// </summary>
      public static WallConnectionDataCache WallConnectionDataCache { get; private set; } = new();

      public static WallCrossSectionCache WallCrossSectionCache { get; private set; } = new();

      /// <summary>
      /// The ZoneCache object.
      /// </summary>
      public static HashSet<ElementId> ZoneCache { get; private set; } = new();

      /// <summary>
      /// The ZoneInfoCache object.
      /// </summary>
      public static ZoneInfoCache ZoneInfoCache { get; private set; } = new();

      /// <summary>
      /// Caches the context handle for a particular IfcGeometricRepresentationContext in this
      /// cache and in the internal cache if necessary.
      /// </summary>
      /// <param name="exporterIFC">The exporterIFC class for access to the internal cache.</param>
      /// <param name="identifier">The identifier.</param>
      /// <param name="contextHandle">The created context handle.</param>
      public static void Set3DContextHandle(ExporterIFC exporterIFC, 
         IFCRepresentationIdentifier identifier, 
         IFCAnyHandle contextHandle)
      {
         string identifierAsString = identifier == IFCRepresentationIdentifier.None ? 
            string.Empty : identifier.ToString();
         exporterIFC.Set3DContextHandle(contextHandle, identifierAsString);
         Context3DHandles[identifier] = contextHandle;
      }

      /// <summary>
      /// Get the handle associated to a particular IfcGeometricRepresentationContext.
      /// </summary>
      /// <param name="identifier">The identifier.</param>
      /// <returns>The corresponding IfcGeometricRepresentationContext handle.</returns>
      public static IFCAnyHandle Get3DContextHandle(IFCRepresentationIdentifier identifier)
      {
         if (Context3DHandles.TryGetValue(identifier, out IFCAnyHandle handle))
            return handle;

         if (!Context3DHandles.TryGetValue(IFCRepresentationIdentifier.None, out IFCAnyHandle context3D))
            return handle;

         IFCGeometricProjection projection = (identifier == IFCRepresentationIdentifier.Axis) ?
            IFCGeometricProjection.Graph_View : IFCGeometricProjection.Model_View;

         IFCFile file = ExporterIFC.GetFile();
         IFCAnyHandle context3DHandle = IFCInstanceExporter.CreateGeometricRepresentationSubContext(file,
                identifier.ToString(), "Model", context3D, null, projection, null);
         Set3DContextHandle(ExporterIFC, identifier, context3DHandle);
         return context3DHandle;
      }

      /// <summary>
      /// Determines if we should export ceiling grids.
      /// </summary>
      /// <returns>True if the user has chosen to export ceiling grids and ceiling surface patterns are exported.</returns>
      public static bool ExportCeilingGrids()
      {
         if (!ExportOptionsCache.ExportCeilingGrids)
         {
            return false;
         }

         if (!m_ExportCeilingGrids.HasValue)
         {
            m_ExportCeilingGrids = CategoryMappingTemplate?.GetMappingInfoById(Document,
               new ElementId(BuiltInCategory.OST_CeilingsSurfacePattern), CustomSubCategoryId.None)?.IFCExportFlag ?? false;
         }

         return m_ExportCeilingGrids.Value;         
      }

      /// <summary>
      /// Get the current parameter mapping template.
      /// </summary>
      public static IFCParameterTemplate ParameterMappingTemplate
      {
         get
         {
            // TODO: this isn't really correct if we are exporting multiple documents.
            if (m_ParameterMappingTemplate == null)
            {
               try
               {
                  string name = ExportOptionsCache.ParameterMappingTemplateName;
                  if (name != null)
                  {
                     Document document = ExportOptionsCache.HostDocument ?? Document;
                     if (document != null)
                     {
                        m_ParameterMappingTemplate = IFCParameterTemplate.FindByName(document, name);
                     }
                  }
               }
               catch
               {
                  m_ParameterMappingTemplate = null;
               }

               m_ParameterMappingTemplate ??= IFCParameterTemplate.GetOrCreateInSessionTemplate(Document);
            }

            return m_ParameterMappingTemplate;
         }
      }

      public static IFCCategoryTemplate CategoryMappingTemplate
      {
         get
         {
            // TODO: this isn't really correct if we are exporting multiple documents.
            if (m_CategoryMappingTemplate == null)
            {
               try
               {
                  string name = ExportOptionsCache.CategoryMappingTemplateName;
                  if (name != null)
                  {
                     Document document = ExportOptionsCache.HostDocument ?? Document;
                     if (document != null)
                     {
                        m_CategoryMappingTemplate = IFCCategoryTemplate.FindByName(document, name);
                     }
                  }
               }
               catch
               {
                  m_CategoryMappingTemplate = null;
               }

               m_CategoryMappingTemplate ??= IFCCategoryTemplate.GetOrCreateInSessionTemplate(Document);
               m_CategoryMappingTemplate?.UpdateCategoryList(Document);
            }

            return m_CategoryMappingTemplate;
         }
      }

      /// <summary>
      /// Get the handle associated to a particular IfcGeometricRepresentationContext, or create it
      /// if it doesn't exist.
      /// </summary>
      /// <param name="file">The IFCFile class.</param>
      /// <param name="identifier">The identifier.</param>
      /// <returns>The corresponding IfcGeometricRepresentationContext handle.</returns>
      public static IFCAnyHandle GetOrCreate3DContextHandle(ExporterIFC exporterIFC, 
         IFCRepresentationIdentifier identifier)
      {
         IFCAnyHandle context3d = Get3DContextHandle(identifier);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(context3d))
            return context3d;

         // This is primarily intended for model curves; we don't
         // want to add the IfcGeometricRepresentationContext unless it is actually used.
         if (!Context3DHandles.TryGetValue(IFCRepresentationIdentifier.None, out IFCAnyHandle parent))
            return null;

         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle newContext3D = IFCInstanceExporter.CreateGeometricRepresentationSubContext(
            file, identifier.ToString(), "Model", parent, null, IFCGeometricProjection.Model_View, null);
         Set3DContextHandle(exporterIFC, identifier, newContext3D);
         return newContext3D;
      }
      
      /// <summary>
      /// Caches the context handle for a particular IfcGeometricRepresentationContext in this
      /// cache and in the internal cache if necessary.
      /// </summary>
      /// <param name="identifier">The identifier.</param>
      /// <param name="contextHandle">The created context handle.</param>
      public static void Set2DContextHandle(IFCRepresentationIdentifier identifier,
         IFCAnyHandle contextHandle)
      {
         Context2DHandles[identifier] = contextHandle;
      }

      /// <summary>
      /// Get the handle associated to a particular IfcGeometricRepresentationContext.
      /// </summary>
      /// <param name="identifier">The identifier.</param>
      /// <returns>The corresponding IfcGeometricRepresentationContext handle.</returns>
      public static IFCAnyHandle Get2DContextHandle(IFCRepresentationIdentifier identifier)
      {
         if (Context2DHandles.TryGetValue(identifier, out IFCAnyHandle handle))
            return handle;

         return null;
      }

      /// <summary>
      /// The ClassificationCache object.
      /// </summary>
      public static ClassificationCache ClassificationCache
      {
         get
         {
            m_ClassificationCache ??= new ClassificationCache(Document);
            return m_ClassificationCache;
         }
         set 
         { 
            m_ClassificationCache = value; 
         }
      }

      /// <summary>
      /// This class is used to identify property set in cache.
      /// Current logic uses a combination of instance type and predefined type
      /// to uniquely identify relation of ifc object and property set.
      /// </summary>
      public class PropertySetKey : IComparable<PropertySetKey>
      {
         public PropertySetKey(IFCEntityType entityType, string predefinedType)
         {
            EntityType = entityType;
            PredefinedType = predefinedType;
         }

         public IFCEntityType EntityType { get; protected set; } = IFCEntityType.UnKnown;

         public string PredefinedType { get; protected set; } = null;

         public int CompareTo(PropertySetKey other)
         {
            if (other == null) 
               return 1;

            if (EntityType < other.EntityType)
               return -1;

            if (EntityType > other.EntityType)
               return 1;

            if (PredefinedType == null)
               return other.PredefinedType == null ? 0 : -1;
            
            if (other.PredefinedType == null)
               return 1;

            return PredefinedType.CompareTo(other.PredefinedType);
         }

         public static bool operator ==(PropertySetKey first, PropertySetKey second)
         {
            object lhsObject = first;
            object rhsObject = second;
            if (null == lhsObject)
            {
               if (null == rhsObject)
                  return true;
               return false;
            }
            if (null == rhsObject)
               return false;

            if (first.EntityType != second.EntityType)
               return false;

            if (first.PredefinedType != second.PredefinedType)
               return false;

            return true;
         }

         public static bool operator !=(PropertySetKey first, PropertySetKey second)
         {
            return !(first == second);
         }

         public override bool Equals(object obj)
         {
            if (obj == null)
               return false;

            PropertySetKey second = obj as PropertySetKey;
            return this == second;
         }

         public override int GetHashCode()
         {
            return EntityType.GetHashCode() + 
               (PredefinedType != null ? PredefinedType.GetHashCode() : 0);
         }
      }

      public static IFCAnyHandle GetDefaultCartesianTransformationOperator3D(IFCFile file)
      {
         if (m_DefaultCartesianTransformationOperator3D == null)
         {
            XYZ orig = new XYZ();
            IFCAnyHandle origHnd = ExporterUtil.CreateCartesianPoint(file, orig);
            m_DefaultCartesianTransformationOperator3D = IFCInstanceExporter.CreateCartesianTransformationOperator3D(file, null, null, origHnd, 1.0, null);
         }
         return m_DefaultCartesianTransformationOperator3D;
      }

      /// <summary>
      /// The PropertyMap cache
      /// </summary>
      public static IDictionary<Tuple<string, string>, string> PropertyMapCache
      {
         get
         {
            m_PropertyMapCache ??= PropertyMap.LoadParameterMap();
            return m_PropertyMapCache;
         }
      }

      /// <summary>
      /// A local copy of the internal IfcCartesianPoint for the global origin.
      public static IFCAnyHandle Global3DOriginHandle
      {
         get
         {
            m_Global3DOriginHandle ??= ExporterIFCUtils.GetGlobal3DOriginHandle();
            return m_Global3DOriginHandle;
         }
      }

      /// <summary>
      /// Clear all caches contained in this manager.
      /// </summary>
      public static void Clear(bool fullClear)
      {
         if (fullClear)
         {
            m_CategoryMappingTemplate = null;
            CertifiedEntitiesAndPsetsCache = new IFCCertifiedEntitiesAndPSets(); // No Clear() for this, just remake.
            ExporterIFC = null;
            ExportOptionsCache = new();    // This will need to be re-initialized before use.
            m_Global3DOriginHandle = null;
            Context2DHandles.Clear();
            Context3DHandles.Clear();
            GUIDCache.Clear();
            OwnerHistoryHandle = null;
            ParameterCache.Clear();
            m_ParameterMappingTemplate = null;
            ProjectHandle = null;
            UnitsCache.Clear();
         }

         // Special case: if we are sharing the IfcSite, don't clear it after the host
         // document export.
         if (fullClear || ExportOptionsCache.ExportLinkedFileAs != LinkedFileExportAs.ExportSameSite)
         {
            SiteExportInfo.Clear();
         }

         AllocatedGeometryObjectCache.DisposeCache();
         ParameterUtil.ClearParameterValueCaches();

         AreaSchemeCache.Clear();
         AssemblyInstanceCache.Clear();
         BaseLinkedDocumentGUID = null;
         BeamSystemCache.Clear();
         BuildingHandle = null;
         CanExportBeamGeometryAsExtrusionCache.Clear();
         CeilingSpaceRelCache.Clear();
         m_ClassificationCache = null;
         ClassificationLocationCache.Clear();
         ContainmentCache.Clear();
         ComplexPropertyCache.Clear();
         BaseQuantitiesCache.Clear();
         CreatedInternalPropertySets.Clear();
         CreatedSpecialPropertySets.Clear();
         CurveAnnotationCache.Clear();
         DBViewsToExport.Clear();
         m_DefaultCartesianTransformationOperator3D = null;
         DoorWindowDelayedOpeningCreatorCache.Clear();
         m_DocumentUnits = null;
         DummyHostCache.Clear();
         ElementsInAssembliesCache.Clear();
         ElementIdMaterialParameterCache.Clear();
         ElementToHandleCache.Clear();
         ElementTypeToHandleCache.Clear();
         m_ExportCeilingGrids = null;
         ExportTypeInfoCache.Clear();
         FabricAreaHandleCache.Clear();
         FabricParamsCache.Clear();
         FamilySymbolToTypeInfoCache.Clear();
         GridCache.Clear();
         GroupCache.Clear();
         GUIDsToStoreCache.Clear();
         HandleToDeleteCache.Clear();
         HandleToElementCache.Clear();
         HostObjectsLevelIndex.Clear();
         HostPartsCache.Clear();
         InternallyCreatedRootHandles.Clear();
         IsExternalParameterValueCache.Clear();
         LengthPrecision = MathUtil.Eps();
         LevelInfoCache.Clear();
         MaterialIdToStyleHandleCache.Clear();
         MaterialSetUsageCache.Clear();
         MaterialSetCache.Clear();
         MaterialConstituentCache.Clear();
         MaterialConstituentSetCache.Clear();
         MaterialHandleCache.Clear();
         MaterialRelationsCache.Clear();
         MEPCache.Clear();
         NonSpatialElements.Clear();
         Object2DCurvesCache.Clear();
         PartExportedCache.Clear();
         PresentationLayerSetCache.Clear();
         PresentationStyleAssignmentCache.Clear();
         PropertyInfoCache.Clear();
         m_PropertyMapCache = null;
         PropertySetsForTypeCache.Clear();
         PreDefinedPropertySetsForTypeCache.Clear();
         RailingCache.Clear();
         RailingSubElementCache.Clear();
         // SelectedSiteProjectLocation is dealt with in ExportOptionsCache.UpdateForDocument().
         SpaceBoundaryCache.Clear();
         SpaceInfoCache.Clear();
         SpaceOccupantInfoCache.Clear();
         StairRampContainerInfoCache.Clear();
         SystemsCache.Clear();
         TemporaryPartsCache.Clear();
         TrussCache.Clear();
         TypePropertyInfoCache.Clear();
         TypeRelationsCache.Clear();
         ViewScheduleElementCache.Clear();
         WallConnectionDataCache.Clear();
         WallCrossSectionCache.Clear();
         ZoneCache.Clear();
         ZoneInfoCache.Clear();
         QtoSetCreated.Clear();
      }
   }
}
