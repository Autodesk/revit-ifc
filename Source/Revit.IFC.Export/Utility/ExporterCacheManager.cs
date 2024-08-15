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
using System.Runtime.Remoting.Contexts;
using System.Runtime.InteropServices.WindowsRuntime;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Manages caches necessary for IFC export.
   /// </summary>
   public class ExporterCacheManager
   {
      /// <summary>
      /// The AllocatedGeometryObjectCache object.
      /// </summary>
      static AllocatedGeometryObjectCache m_AllocatedGeometryObjectCache;

      /// <summary>
      /// The AssemblyInstanceCache object.
      /// </summary>
      static AssemblyInstanceCache m_AssemblyInstanceCache;

      /// <summary>
      /// The IfcBuilding handle.
      /// </summary>
      static public IFCAnyHandle BuildingHandle { get; set; } = null;

      /// <summary>
      /// A cache to keep track of what beams can be exported as extrusions.
      /// Strictly for performance issues.
      /// </summary>
      static IDictionary<ElementId, bool> m_CanExportBeamGeometryAsExtrusionCache;

      /// <summary>
      /// Cache the values of the IFC entity class from the IFC Export table by category.
      /// </summary>
      static Dictionary<KeyValuePair<ElementId, int>, string> m_CategoryClassNameCache;

      /// <summary>
      /// Cache the values of the IFC entity pre-defined type from the IFC Export table by category.
      /// </summary>
      static Dictionary<KeyValuePair<ElementId, int>, string> m_CategoryTypeCache;

      /// <summary>
      /// The ClassificationCache object.
      /// Keeps track of created IfcClassifications for re-use.
      /// </summary>
      static ClassificationCache m_ClassificationCache;

      /// <summary>
      /// The Classification location cache.
      /// </summary>
      static ClassificationLocationCache m_ClassificationLocationCache;

      /// <summary>
      /// The CurveAnnotationCache object.
      /// </summary>
      static CurveAnnotationCache m_CurveAnnotationCache;

      /// <summary>
      /// The db views to export.
      /// </summary>
      static IDictionary<ElementId, ElementId> m_DBViewsToExport;

      /// <summary>
      /// The Document object.
      /// </summary>
      static Document m_Document;

      /// <summary>
      /// The collection of openings needed for created doors and windows.
      /// </summary>
      static DoorWindowDelayedOpeningCreatorCache m_DoorWindowDelayedOpeningCreatorCache;

      ///<summary>
      /// The ElementToHandleCache cache.
      /// </summary>
      static ElementToHandleCache m_ElementToHandleCache;

      /// <summary>
      /// A mapping of element ids to a material id determined by looking at element parameters.
      /// </summary>
      static public IDictionary<ElementId, ElementId> ElementIdMaterialParameterCache { get; set; } =
         new Dictionary<ElementId, ElementId>();

      ///<summary>
      /// The ElementTypeToHandleCache cache
      /// </summary>
      static ElementTypeToHandleCache m_ElementTypeToHandleCache;

      static IDictionary<ElementId, FabricParams> m_FabricParamsCache;

      /// <summary>
      /// The ExporterIFC used to access internal IFC API functions.
      /// </summary>
      static public ExporterIFC ExporterIFC { get; set; } = null;

      ///<summary>
      /// The ExportOptions cache.
      /// </summary>
      static ExportOptionsCache m_ExportOptionsCache;

      static IFCAnyHandle m_Global3DOriginHandle = null;

      /// <summary>
      /// The GUIDs to store at the end of export.
      /// </summary>
      static Dictionary<KeyValuePair<ElementId, BuiltInParameter>, string> m_GUIDsToStoreCache;

      /// <summary>
      /// The HandleToElementCache cache.
      /// This maps an IFC handle to the Element that created it.
      /// This is used to identify which element should be used for properties, for elements (e.g. Stairs) that contain other elements.
      /// </summary>
      static HandleToElementCache m_HandleToElementCache;

      /// <summary>
      /// The IsExternal parameter value cache.
      /// This stores the IsExternal value from the shared parameters, if any, 
      /// for elements that may be used by hosted elements later.
      /// We use this because we clear the ParametersCache after we export an element,
      /// and do not want to create just for IsExternal.
      static Dictionary<ElementId, bool> m_IsExternalParameterValueCache;

      /// <summary>
      /// The MaterialHandleCache object.
      /// </summary>
      static ElementToHandleCache m_MaterialHandleCache;

      /// <summary>
      /// The MaterialConsituent object cache (starting IFC4)
      /// </summary>
      static MaterialConstituentCache m_MaterialConstituentCache;

      /// <summary>
      /// The MaterialConstituentSet cache (starting IFC4)
      /// </summary>
      static MaterialConstituentSetCache m_MaterialConstituentSetCache;

      /// <summary>
      /// The MaterialSetCache object.
      /// </summary>
      static MaterialSetCache m_MaterialSetCache;

      /// <summary>
      /// The MEPCache object.
      /// </summary>
      static MEPCache m_MEPCache;

      static AttributeCache m_AttributeCache;

      /// <summary>
      /// The ParameterCache object.
      /// </summary>
      public static ParameterCache ParameterCache { get; set; } = new ParameterCache();

      /// <summary>
      /// The PartExportedCache object.
      /// </summary>
      static PartExportedCache m_PartExportedCache;

      /// <summary>
      /// The PresentationLayerSetCache object.
      /// </summary>
      static PresentationLayerSetCache m_PresentationLayerSetCache;

      /// <summary>
      /// The PresentationStyleAssignmentCache object.
      /// </summary>
      static PresentationStyleAssignmentCache m_PresentationStyleCache;

      /// <summary>
      /// The top level IfcProject handle.
      /// </summary>
      public static IFCAnyHandle ProjectHandle { get; set; } = null;

      ///<summary>
      /// The RailingCache cache.
      /// This keeps track of all of the railings in the document, to export them last.
      /// </summary>
      static HashSet<ElementId> m_RailingCache;

      ///<summary>
      /// The RailingSubElementCache cache.
      /// This keeps track of all of the sub-elements of railings in the document, to not export them twice.
      /// </summary>
      static HashSet<ElementId> m_RailingSubElementCache;

      /// <summary>
      /// The top level IfcSite handle.
      /// </summary>
      public static IFCAnyHandle SiteHandle { get; set; } = null;

      /// <summary>
      /// The top level 2D context handles by identifier.
      /// </summary>
      private static IDictionary<IFCRepresentationIdentifier, IFCAnyHandle> Context2DHandles
      { get; set; } = new Dictionary<IFCRepresentationIdentifier, IFCAnyHandle>();

      /// <summary>
      /// The top level 3D context handles by identifier.
      /// </summary>
      private static IDictionary<IFCRepresentationIdentifier, IFCAnyHandle> Context3DHandles 
         { get; set; } = new Dictionary<IFCRepresentationIdentifier, IFCAnyHandle>();

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

         return null;
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
             /// <param name="exporterIFC">The exporterIFC class for access to the internal cache.</param>
             /// <param name="identifier">The identifier.</param>
             /// <param name="contextHandle">The created context handle.</param>
      public static void Set2DContextHandle(ExporterIFC exporterIFC,
         IFCRepresentationIdentifier identifier,
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
      /// The SpaceBoundaryCache object.
      /// </summary>
      static SpaceBoundaryCache m_SpaceBoundaryCache;

      /// <summary>
      /// The SpaceOccupantInfoCache object.
      /// </summary>
      static SpaceOccupantInfoCache m_SpaceOccupantInfoCache;

      /// <summary>
      /// The SystemsCache object.
      /// </summary>
      static SystemsCache m_SystemsCache;

      ///<summary>
      /// The Truss cache.
      /// This keeps track of all of the truss in the document, to export after all beams and members.
      /// </summary>
      static HashSet<ElementId> m_TrussCache;

      /// <summary>
      /// The ViewSchedule element cache.
      /// This tracks the element ids of the elements in a view schedule that is being exported.  Not used unless schedules are being exported.
      /// </summary>
      static IDictionary<ElementId, HashSet<ElementId>> m_ViewScheduleElementCache;

      ///<summary>
      /// The AreaScheme cache.
      /// This keeps track of all of the area schemes in the document, to export them after all areas.
      /// </summary>
      static Dictionary<ElementId, HashSet<IFCAnyHandle>> m_AreaSchemeCache;

      ///<summary>
      /// The BeamSystem cache.
      /// This keeps track of all of the beam systems in the document, to export after all beams.
      /// </summary>
      static HashSet<ElementId> m_BeamSystemCache;

      ///<summary>
      /// The Group cache.
      /// This keeps track of all of the groups in the document, to export them after all regular elements.
      /// </summary>
      static GroupCache m_GroupCache;

      ///<summary>
      /// The Zone cache.
      /// This keeps track of all of the zone in the document, to export them after all spaces.
      /// </summary>
      static HashSet<ElementId> m_ZoneCache;

      /// <summary>
      /// The TypeRelationsCache object.
      /// </summary>
      static TypeRelationsCache m_TypeRelationsCache;

      /// <summary>
      /// The FamilySymbolToTypeInfoCache object
      /// </summary>
      static TypeObjectsCache m_FamilySymbolToTypeInfoCache;

      /// <summary>
      /// The WallConnectionDataCache object.
      /// </summary>
      static WallConnectionDataCache m_WallConnectionDataCache;

      /// <summary>
      /// The UnitsCache object.
      /// Keeps track of created IfcUnits for re-use.
      /// </summary>
      static UnitsCache m_UnitsCache;

      /// <summary>
      /// The ZoneInfoCache object.
      /// </summary>
      static ZoneInfoCache m_ZoneInfoCache;

      /// <summary>
      /// The TypePropertyInfoCache object.
      /// </summary>
      static TypePropertyInfoCache m_TypePropertyInfoCache;

      /// <summary>
      /// The PropertyInfoCache object.
      /// </summary>
      static PropertyInfoCache m_PropertyInfoCache;

      /// <summary>
      /// The common property sets to be exported for an entity type, regardless of Object Type.
      /// </summary>
      static IDictionary<PropertySetKey, IList<PropertySetDescription>> m_PropertySetsForTypeCache;

      /// <summary>
      /// The common predefined property sets to be exported for an entity type, regardless of Object Type.
      /// </summary>
      static IDictionary<PropertySetKey, IList<PreDefinedPropertySetDescription>> m_PreDefinedPropertySetsForTypeCache;

      /// <summary>
      /// The material id to style handle cache.
      /// </summary>
      static ElementToHandleCache m_MaterialIdToStyleHandleCache;

      /// <summary>
      /// A list of elements contained in assemblies, to be removed from the level spatial structure.
      /// </summary>
      static ISet<IFCAnyHandle> m_ElementsInAssembliesCache;

      /// <summary>
      /// The default IfcCartesianTransformationOperator3D, scale 1.0 and origin =  { 0., 0., 0. };
      /// </summary>
      static IFCAnyHandle m_DefaultCartesianTransformationOperator3D;

      /// The HostPartsCache object.
      /// </summary>
      static HostPartsCache m_HostPartsCache;

      /// <summary>
      /// The DummyHostCache object.
      /// </summary>
      static DummyHostCache m_DummyHostCache;

      /// <summary>
      /// The StairRampContainerInfoCache object.
      /// </summary>
      static StairRampContainerInfoCache m_StairRampContainerInfoCache;

      /// <summary>
      /// The GridCache object.
      /// </summary>
      static List<Element> m_GridCache;

      /// <summary>
      /// This contains the mapping from Level element id to index in the IList returned by GetHostObjects.
      /// This is redundant with a native list that is being deprecated, which has inadequate API access.
      /// </summary>
      static IDictionary<ElementId, int> m_HostObjectsLevelIndex;

      ///// <summary>
      ///// The ElementToTypeCache cache that maps Revit element type id to the IFC element type handle.
      ///// </summary>
      //static ElementToHandleCache m_ElementTypeToHandleCache;

      /// <summary>
      /// Keeps relationship of Ceiling to the Space(s) where it belongs to. Used to determine Space containment for Ceiling object that is fully contained in Space (for FMHandOverView)
      /// </summary>
      static IDictionary<ElementId, IList<ElementId>> m_CeilingSpaceRelCache;

      /// <summary>
      /// The SpaceInfo cache that maps Revit SpatialElement id to the SpaceInfo.
      /// </summary>
      static SpaceInfoCache m_SpaceInfoCache;

      /// <summary>
      /// The FabricArea id to FabricSheet handle cache.
      /// </summary>
      static IDictionary<ElementId, HashSet<IFCAnyHandle>> m_FabricAreaHandleCache;

      /// <summary>
      /// The PropertyMapCache
      /// </summary>
      static IDictionary<Tuple<string, string>, string> m_PropertyMapCache;

      /// <summary>
      /// The CertifiedEntitiesAndPsetCache
      /// </summary>
      static IFCCertifiedEntitiesAndPSets m_CertifiedEntitiesAndPsetCache;

      static HashSet<IFCAnyHandle> m_HandleToDelete;

      static IDictionary<ElementId, IList<Curve>> m_Object2DCurves;

      /// <summary>
      /// Cache for additional Quantities or Properties to be created later with the other quantities
      /// </summary>
      static public IDictionary<IFCAnyHandle, HashSet<IFCAnyHandle>> ComplexPropertyCache { get; set; } = new Dictionary<IFCAnyHandle, HashSet<IFCAnyHandle>>();

      /// <summary>
      /// Cache for Base Quantities that require separate calculation.
      /// </summary>
      static public IDictionary<IFCAnyHandle, HashSet<IFCAnyHandle>> BaseQuantitiesCache { get; set; } = new Dictionary<IFCAnyHandle, HashSet<IFCAnyHandle>>();

      /// <summary>
      /// Cache for the Project Location that comes from the Selected Site on export option
      /// </summary>
      static public ProjectLocation SelectedSiteProjectLocation { get; set; } = null;

      /// Cache for information whether a QuantitySet specified in the Dict. value has been created for the elementHandle
      /// </summary>
      static public HashSet<(IFCAnyHandle, string)> QtoSetCreated { get; set; } = new HashSet<(IFCAnyHandle, string)>();

      /// <summary>
      /// The AllocatedGeometryObjectCache object.
      /// </summary>
      public static AllocatedGeometryObjectCache AllocatedGeometryObjectCache
      {
         get
         {
            if (m_AllocatedGeometryObjectCache == null)
               m_AllocatedGeometryObjectCache = new AllocatedGeometryObjectCache();
            return m_AllocatedGeometryObjectCache;
         }
      }

      /// <summary>
      /// The AssemblyInstanceCache object.
      /// </summary>
      public static AssemblyInstanceCache AssemblyInstanceCache
      {
         get
         {
            if (m_AssemblyInstanceCache == null)
               m_AssemblyInstanceCache = new AssemblyInstanceCache();
            return m_AssemblyInstanceCache;
         }
      }

      /// <summary>
      /// A cache to keep track of what beams can be exported as extrusions.
      /// Strictly for performance issues.
      /// </summary>
      public static IDictionary<ElementId, bool> CanExportBeamGeometryAsExtrusionCache
      {
         get
         {
            if (m_CanExportBeamGeometryAsExtrusionCache == null)
               m_CanExportBeamGeometryAsExtrusionCache = new Dictionary<ElementId, bool>();
            return m_CanExportBeamGeometryAsExtrusionCache;
         }
      }

      /// <summary>
      /// The CategoryClassNameCache object.
      /// </summary>
      public static IDictionary<KeyValuePair<ElementId, int>, string> CategoryClassNameCache
      {
         get
         {
            if (m_CategoryClassNameCache == null)
               m_CategoryClassNameCache = new Dictionary<KeyValuePair<ElementId, int>, string>();
            return m_CategoryClassNameCache;
         }
      }

      /// <summary>
      /// The CategoryTypeCache object.
      /// </summary>
      public static IDictionary<KeyValuePair<ElementId, int>, string> CategoryTypeCache
      {
         get
         {
            if (m_CategoryTypeCache == null)
               m_CategoryTypeCache = new Dictionary<KeyValuePair<ElementId, int>, string>();
            return m_CategoryTypeCache;
         }
      }

      /// <summary>
      /// The base guid to use for all entities when exporting, used when exporting linked documents.
      /// </summary>
      public static string BaseLinkedDocumentGUID { get; set; } = null;
      
      /// <summary>
      /// The GUIDCache object.
      /// </summary>
      public static HashSet<string> GUIDCache { get; } = new HashSet<string>();

      /// <summary>
      /// The GUIDs to store in elements at the end of export, if the option to store GUIDs has been selected.
      /// </summary>
      public static IDictionary<KeyValuePair<ElementId, BuiltInParameter>, string> GUIDsToStoreCache
      {
         get
         {
            if (m_GUIDsToStoreCache == null)
               m_GUIDsToStoreCache = new Dictionary<KeyValuePair<ElementId, BuiltInParameter>, string>();
            return m_GUIDsToStoreCache;
         }
      }

      /// <summary>
      /// The HandleToElementCache object.
      /// </summary>
      public static HandleToElementCache HandleToElementCache
      {
         get
         {
            if (m_HandleToElementCache == null)
               m_HandleToElementCache = new HandleToElementCache();
            return m_HandleToElementCache;
         }
      }

      /// <summary>
      /// The IsExternalParameterValueCache object.
      /// </summary>
      public static IDictionary<ElementId, bool> IsExternalParameterValueCache
      {
         get
         {
            if (m_IsExternalParameterValueCache == null)
               m_IsExternalParameterValueCache = new Dictionary<ElementId, bool>();
            return m_IsExternalParameterValueCache;
         }
      }

      /// <summary>
      /// The language of the current Revit document.
      /// </summary>
      public static LanguageType LanguageType { get; set; }

      public static AttributeCache AttributeCache
      {
         get
         {
            if (m_AttributeCache == null)
               m_AttributeCache = new AttributeCache();
            return m_AttributeCache;
         }
      }

      /// <summary>
      /// The PartExportedCache object.
      /// </summary>
      public static PartExportedCache PartExportedCache
      {
         get
         {
            if (m_PartExportedCache == null)
               m_PartExportedCache = new PartExportedCache();
            return m_PartExportedCache;
         }
      }
      /// <summary>
      /// The Document object passed to the Exporter class.
      /// </summary>
      public static Autodesk.Revit.DB.Document Document
      {
         get
         {
            if (m_Document == null)
            {
               throw new InvalidOperationException("doc is null");
            }
            return m_Document;
         }
         set
         {
            m_Document = value;
         }
      }

      /// <summary>
      /// The PresentationLayerSetCache object.
      /// </summary>
      public static PresentationLayerSetCache PresentationLayerSetCache
      {
         get
         {
            if (m_PresentationLayerSetCache == null)
               m_PresentationLayerSetCache = new PresentationLayerSetCache();
            return m_PresentationLayerSetCache;
         }
      }

      /// <summary>
      /// The PresentationStyleAssignmentCache object.
      /// </summary>
      public static PresentationStyleAssignmentCache PresentationStyleAssignmentCache
      {
         get
         {
            if (m_PresentationStyleCache == null)
               m_PresentationStyleCache = new PresentationStyleAssignmentCache();
            return m_PresentationStyleCache;
         }
      }

      /// <summary>
      /// The top level IfcOwnerHistory handle.
      /// </summary>
      public static IFCAnyHandle OwnerHistoryHandle { get; set; } = null;

      /// <summary>
      /// The CurveAnnotationCache object.
      /// </summary>
      public static CurveAnnotationCache CurveAnnotationCache
      {
         get
         {
            if (m_CurveAnnotationCache == null)
               m_CurveAnnotationCache = new CurveAnnotationCache();
            return m_CurveAnnotationCache;
         }
      }

      /// <summary>
      /// The CurveAnnotationCache object.
      /// </summary>
      public static IDictionary<ElementId, ElementId> DBViewsToExport
      {
         get
         {
            if (m_DBViewsToExport == null)
               m_DBViewsToExport = new Dictionary<ElementId, ElementId>();
            return m_DBViewsToExport;
         }
      }

      /// <summary>
      /// The cache containing the openings that need to be created for doors and windows.
      /// </summary>
      public static DoorWindowDelayedOpeningCreatorCache DoorWindowDelayedOpeningCreatorCache
      {
         get
         {
            if (m_DoorWindowDelayedOpeningCreatorCache == null)
               m_DoorWindowDelayedOpeningCreatorCache = new DoorWindowDelayedOpeningCreatorCache();
            return m_DoorWindowDelayedOpeningCreatorCache;
         }
      }

      /// <summary>
      /// The Material___SetCache object (includes IfcMaterialLayerSet, IfcMaterialProfileSet, IfcMaterialConstituentSet in IFC4).
      /// </summary>
      public static MaterialSetCache MaterialSetCache
      {
         get
         {
            if (m_MaterialSetCache == null)
               m_MaterialSetCache = new MaterialSetCache();
            return m_MaterialSetCache;
         }
      }

      /// <summary>
      /// The MEPCache object.
      /// </summary>
      public static MEPCache MEPCache
      {
         get
         {
            if (m_MEPCache == null)
               m_MEPCache = new MEPCache();
            return m_MEPCache;
         }
      }


      /// <summary>
      /// The SpaceBoundaryCache object.
      /// </summary>
      public static SpaceBoundaryCache SpaceBoundaryCache
      {
         get
         {
            if (m_SpaceBoundaryCache == null)
               m_SpaceBoundaryCache = new SpaceBoundaryCache();
            return m_SpaceBoundaryCache;
         }
      }

      /// <summary>
      /// The SpaceInfo cache that maps Revit SpatialElement id to the SpaceInfo.
      /// </summary>
      public static SpaceInfoCache SpaceInfoCache
      {
         get
         {
            if (m_SpaceInfoCache == null)
               m_SpaceInfoCache = new SpaceInfoCache();
            return m_SpaceInfoCache;
         }
      }

      /// <summary>
      /// The SystemsCache object.
      /// </summary>
      public static SystemsCache SystemsCache
      {
         get
         {
            if (m_SystemsCache == null)
               m_SystemsCache = new SystemsCache();
            return m_SystemsCache;
         }
      }

      /// <summary>
      /// The MaterialHandleCache object.
      /// </summary>
      public static ElementToHandleCache MaterialHandleCache
      {
         get
         {
            if (m_MaterialHandleCache == null)
               m_MaterialHandleCache = new ElementToHandleCache();
            return m_MaterialHandleCache;
         }
      }

      /// <summary>
      /// The MaterialConstituent to IfcMaterial cache
      /// </summary>
      public static MaterialConstituentCache MaterialConstituentCache
      {
         get
         {
            if (m_MaterialConstituentCache == null)
               m_MaterialConstituentCache = new MaterialConstituentCache();
            return m_MaterialConstituentCache;
         }
      }

      /// <summary>
      /// The MaterialConstituentSet cache
      /// </summary>
      public static MaterialConstituentSetCache MaterialConstituentSetCache
      {
         get
         {
            if (m_MaterialConstituentSetCache == null)
               m_MaterialConstituentSetCache = new MaterialConstituentSetCache();
            return m_MaterialConstituentSetCache;
         }
      }

      /// <summary>
      /// The MaterialRelationsCache object.
      /// </summary>
      public static MaterialRelationsCache MaterialRelationsCache { get; private set;  } = 
         new MaterialRelationsCache();
      
      /// <summary>
      /// The MaterialLayerRelationsCache object.
      /// </summary>
      public static MaterialSetUsageCache MaterialSetUsageCache { get; private set; } = 
         new MaterialSetUsageCache();

      /// <summary>
      /// The RailingCache object.
      /// </summary>
      public static HashSet<ElementId> RailingCache
      {
         get
         {
            if (m_RailingCache == null)
               m_RailingCache = new HashSet<ElementId>();
            return m_RailingCache;
         }
      }

      /// <summary>
      /// The TrussCache object.
      /// </summary>
      public static HashSet<ElementId> TrussCache
      {
         get
         {
            if (m_TrussCache == null)
               m_TrussCache = new HashSet<ElementId>();
            return m_TrussCache;
         }
      }

      /// <summary>
      /// The ViewScheduleElementCache object.
      /// </summary>
      public static IDictionary<ElementId, HashSet<ElementId>> ViewScheduleElementCache
      {
         get
         {
            if (m_ViewScheduleElementCache == null)
               m_ViewScheduleElementCache = new Dictionary<ElementId, HashSet<ElementId>>();
            return m_ViewScheduleElementCache;
         }
      }

      /// <summary>
      /// The BeamSystemCache object.
      /// </summary>
      public static HashSet<ElementId> BeamSystemCache
      {
         get
         {
            if (m_BeamSystemCache == null)
               m_BeamSystemCache = new HashSet<ElementId>();
            return m_BeamSystemCache;
         }
      }

      /// <summary>
      /// The AreaSchemeCache object.
      /// </summary>
      public static Dictionary<ElementId, HashSet<IFCAnyHandle>> AreaSchemeCache
      {
         get
         {
            if (m_AreaSchemeCache == null)
               m_AreaSchemeCache = new Dictionary<ElementId, HashSet<IFCAnyHandle>>();
            return m_AreaSchemeCache;
         }
      }

      /// <summary>
      /// The GroupCache object.
      /// </summary>
      public static GroupCache GroupCache
      {
         get
         {
            if (m_GroupCache == null)
               m_GroupCache = new GroupCache();
            return m_GroupCache;
         }
      }

      /// <summary>
      /// The ZoneCache object.
      /// </summary>
      public static HashSet<ElementId> ZoneCache
      {
         get
         {
            if (m_ZoneCache == null)
               m_ZoneCache = new HashSet<ElementId>();
            return m_ZoneCache;
         }
      }

      /// <summary>
      /// The RailingSubElementCache object.  This ensures that we don't export sub-elements of railings (e.g. supports) separately.
      /// </summary>
      public static HashSet<ElementId> RailingSubElementCache
      {
         get
         {
            if (m_RailingSubElementCache == null)
               m_RailingSubElementCache = new HashSet<ElementId>();
            return m_RailingSubElementCache;
         }
      }

      /// <summary>
      /// The TypeRelationsCache object.
      /// </summary>
      public static TypeRelationsCache TypeRelationsCache
      {
         get
         {
            if (m_TypeRelationsCache == null)
               m_TypeRelationsCache = new TypeRelationsCache();
            return m_TypeRelationsCache;
         }
      }

      /// <summary>
      /// The FamilySymbolToTypeInfoCache object.  This maps a FamilySymbol id to the related created IFC information (the TypeObjectsCache).
      /// </summary>
      public static TypeObjectsCache FamilySymbolToTypeInfoCache
      {
         get
         {
            if (m_FamilySymbolToTypeInfoCache == null)
               m_FamilySymbolToTypeInfoCache = new TypeObjectsCache();
            return m_FamilySymbolToTypeInfoCache;
         }
      }

      /// <summary>
      /// The ZoneInfoCache object.
      /// </summary>
      public static ZoneInfoCache ZoneInfoCache
      {
         get
         {
            if (m_ZoneInfoCache == null)
               m_ZoneInfoCache = new ZoneInfoCache();
            return m_ZoneInfoCache;
         }
      }

      /// <summary>
      /// The SpaceOccupantInfoCache object.
      /// </summary>
      public static SpaceOccupantInfoCache SpaceOccupantInfoCache
      {
         get
         {
            if (m_SpaceOccupantInfoCache == null)
               m_SpaceOccupantInfoCache = new SpaceOccupantInfoCache();
            return m_SpaceOccupantInfoCache;
         }
      }

      /// <summary>
      /// The WallConnectionDataCache object.
      /// </summary>
      public static WallConnectionDataCache WallConnectionDataCache
      {
         get
         {
            if (m_WallConnectionDataCache == null)
               m_WallConnectionDataCache = new WallConnectionDataCache();
            return m_WallConnectionDataCache;
         }
      }

      public static WallCrossSectionCache WallCrossSectionCache { get; set; } = new WallCrossSectionCache();

      /// <summary>
      /// The ElementToHandleCache object, used to cache Revit element ids to IFC entity handles.
      /// </summary>
      public static ElementToHandleCache ElementToHandleCache
      {
         get
         {
            if (m_ElementToHandleCache == null)
               m_ElementToHandleCache = new ElementToHandleCache();
            return m_ElementToHandleCache;
         }
      }

      /// <summary>
      /// The ElementTypeToHandleCache object, used to cache Revit element type ids to IFC entity handles.
      /// </summary>
      public static ElementTypeToHandleCache ElementTypeToHandleCache
      {
         get
         {
            if (m_ElementTypeToHandleCache == null)
               m_ElementTypeToHandleCache = new ElementTypeToHandleCache();
            return m_ElementTypeToHandleCache;
         }
      }

      /// <summary>
      /// The FabricParamsCache object, used to cache FabricSheet parameters.
      /// </summary>
      public static IDictionary<ElementId, FabricParams> FabricParamsCache
      {
         get
         {
            if (m_FabricParamsCache == null)
               m_FabricParamsCache = new Dictionary<ElementId, FabricParams>();
            return m_FabricParamsCache;
         }
      }

      /// <summary>
      /// The ExportOptionsCache object.
      /// </summary>
      public static ExportOptionsCache ExportOptionsCache
      {
         get { return m_ExportOptionsCache; }
         set { m_ExportOptionsCache = value; }
      }

      /// <summary>
      /// The ContainmentCache object.
      /// </summary>
      public static ContainmentCache ContainmentCache { get; set; } = new ContainmentCache();

      /// <summary>
      /// The ClassificationCache object.
      /// </summary>
      public static ClassificationCache ClassificationCache
      {
         get
         {
            if (m_ClassificationCache == null)
               m_ClassificationCache = new ClassificationCache(Document);
            return m_ClassificationCache;
         }
         set { m_ClassificationCache = value; }
      }

     public static ClassificationLocationCache ClassificationLocationCache
      {
         get
         {
            if (m_ClassificationLocationCache == null)
               m_ClassificationLocationCache = new ClassificationLocationCache();
            return m_ClassificationLocationCache;
         }
         set { m_ClassificationLocationCache = value; }
      }

      /// <summary>
      /// The UnitsCache object.
      /// </summary>
      public static UnitsCache UnitsCache
      {
         get
         {
            if (m_UnitsCache == null)
               m_UnitsCache = new UnitsCache();
            return m_UnitsCache;
         }
         set { m_UnitsCache = value; }
      }

      /// <summary>
      /// The HostPartsCache object.
      /// </summary>
      public static HostPartsCache HostPartsCache
      {
         get
         {
            if (m_HostPartsCache == null)
               m_HostPartsCache = new HostPartsCache();
            return m_HostPartsCache;
         }
      }

      /// <summary>
      /// The DummyHostCache object.
      /// </summary>
      public static DummyHostCache DummyHostCache
      {
         get
         {
            if (m_DummyHostCache == null)
               m_DummyHostCache = new DummyHostCache();
            return m_DummyHostCache;
         }
      }

      /// <summary>
      /// The LevelInfoCache object.  This contains extra information on top of
      /// IFCLevelInfo, and will eventually replace it.
      /// </summary>
      public static LevelInfoCache LevelInfoCache { get; set; } = new LevelInfoCache();

      /// <summary>
      /// The TypePropertyInfoCache object.
      /// </summary>
      public static TypePropertyInfoCache TypePropertyInfoCache
      {
         get
         {
            if (m_TypePropertyInfoCache == null)
               m_TypePropertyInfoCache = new TypePropertyInfoCache();
            return m_TypePropertyInfoCache;
         }
      }

      /// <summary>
      /// The PropertyInfoCache object.
      /// </summary>
      public static PropertyInfoCache PropertyInfoCache
      {
         get
         {
            if (m_PropertyInfoCache == null)
               m_PropertyInfoCache = new PropertyInfoCache();
            return m_PropertyInfoCache;
         }
      }

      /// <summary>
      /// A cache of internally created IfcRoot-derived handles.
      /// </summary>
      /// <remarks></remarks>
      public static IDictionary<IFCAnyHandle, ElementId> InternallyCreatedRootHandles
      {
         get
         {
            if (m_InternallyCreatedRootHandles == null)
               m_InternallyCreatedRootHandles = new Dictionary<IFCAnyHandle, ElementId>();
            return m_InternallyCreatedRootHandles;
         }
      }
      
      private static IDictionary<IFCAnyHandle, ElementId> m_InternallyCreatedRootHandles;

      private static PropertySetCache m_CreatedSpecialPropertySets;

      private static PropertySetCache m_CreatedInternalPropertySets;

      /// <summary>
      /// Cache for "special" property sets to make sure we don't re-export them.
      /// </summary>
      /// <remarks>
      /// At the moment, this is only for Pset_Draughting for 2x2.  But really we
      /// should combine this with CreatedInternalPropertySets.
      /// </remarks>
      public static PropertySetCache CreatedSpecialPropertySets
      {
         get
         {
            if (m_CreatedSpecialPropertySets == null)
               m_CreatedSpecialPropertySets = new PropertySetCache();
            return m_CreatedSpecialPropertySets;
         }
      }

      public static PropertySetCache CreatedInternalPropertySets
      {
         get
         {
            if (m_CreatedInternalPropertySets == null)
               m_CreatedInternalPropertySets = new PropertySetCache();
            return m_CreatedInternalPropertySets;
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
               return (other.PredefinedType == null ? 0 : -1);
            
            if (other.PredefinedType == null)
               return 1;

            return PredefinedType.CompareTo(other.PredefinedType);
         }

         static public bool operator ==(PropertySetKey first, PropertySetKey second)
         {
            Object lhsObject = first;
            Object rhsObject = second;
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

         static public bool operator !=(PropertySetKey first, PropertySetKey second)
         {
            return !(first == second);
         }

         public override bool Equals(object obj)
         {
            if (obj == null)
               return false;

            PropertySetKey second = obj as PropertySetKey;
            return (this == second);
         }

         public override int GetHashCode()
         {
            return EntityType.GetHashCode() + 
               (PredefinedType != null ? PredefinedType.GetHashCode() : 0);
         }
      }

      /// <summary>
      /// The common property sets to be exported for an entity type, regardless of Object Type.
      /// </summary>
      public static IDictionary<PropertySetKey, IList<PropertySetDescription>> PropertySetsForTypeCache
      {
         get
         {
            if (m_PropertySetsForTypeCache == null)
               m_PropertySetsForTypeCache = new Dictionary<PropertySetKey, IList<PropertySetDescription>>();
            return m_PropertySetsForTypeCache;
         }
      }

      /// <summary>
      /// The predefined property sets to be exported for an entity type, regardless of Object Type.
      /// </summary>
      public static IDictionary<PropertySetKey, IList<PreDefinedPropertySetDescription>> PreDefinedPropertySetsForTypeCache
      {
         get
         {
            if (m_PreDefinedPropertySetsForTypeCache == null)
               m_PreDefinedPropertySetsForTypeCache = new Dictionary<PropertySetKey, IList<PreDefinedPropertySetDescription>>();
            return m_PreDefinedPropertySetsForTypeCache;
         }
      }

      /// <summary>
      /// The material id to style handle cache.
      /// </summary>
      public static ElementToHandleCache MaterialIdToStyleHandleCache
      {
         get
         {
            if (m_MaterialIdToStyleHandleCache == null)
               m_MaterialIdToStyleHandleCache = new ElementToHandleCache();
            return m_MaterialIdToStyleHandleCache;
         }
      }

      /// <summary>
      /// The elements in assemblies cache.
      /// </summary>
      public static ISet<IFCAnyHandle> ElementsInAssembliesCache
      {
         get
         {
            if (m_ElementsInAssembliesCache == null)
               m_ElementsInAssembliesCache = new HashSet<IFCAnyHandle>();
            return m_ElementsInAssembliesCache;
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
      /// The StairRampContainerInfoCache object.
      /// </summary>
      public static StairRampContainerInfoCache StairRampContainerInfoCache
      {
         get
         {
            if (m_StairRampContainerInfoCache == null)
               m_StairRampContainerInfoCache = new StairRampContainerInfoCache();
            return m_StairRampContainerInfoCache;
         }
      }

      /// <summary>
      /// The GridCache object.
      /// </summary>
      public static List<Element> GridCache
      {
         get
         {
            if (m_GridCache == null)
               m_GridCache = new List<Element>();
            return m_GridCache;
         }
      }

      /// <summary>
      /// This contains the mapping from Level element id to index in the IList returned by GetHostObjects.
      /// This is redundant with a native list that is being deprecated, which has inadequate API access.
      /// </summary>
      public static IDictionary<ElementId, int> HostObjectsLevelIndex
      {
         get
         {
            if (m_HostObjectsLevelIndex == null)
               m_HostObjectsLevelIndex = new Dictionary<ElementId, int>();
            return m_HostObjectsLevelIndex;
         }
      }

      /// <summary>
      /// Ceiling and Space relationship cache. We need it to check whether a Ceiling should be contained in a Space later on when exporting Ceiling
      /// </summary>
      public static IDictionary<ElementId, IList<ElementId>> CeilingSpaceRelCache
      {
         get
         {
            if (m_CeilingSpaceRelCache == null)
               m_CeilingSpaceRelCache = new Dictionary<ElementId, IList<ElementId>>();
            return m_CeilingSpaceRelCache;
         }
      }

      /// <summary>
      /// The FabricArea id to FabricSheet handle cache.
      /// </summary>
      public static IDictionary<ElementId, HashSet<IFCAnyHandle>> FabricAreaHandleCache
      {
         get
         {
            if (m_FabricAreaHandleCache == null)
               m_FabricAreaHandleCache = new Dictionary<ElementId, HashSet<IFCAnyHandle>>();
            return m_FabricAreaHandleCache;
         }
      }

      /// <summary>
      /// The PropertyMap cache
      /// </summary>
      public static IDictionary<Tuple<string, string>, string> PropertyMapCache
      {
         get
         {
            if (m_PropertyMapCache == null)
               m_PropertyMapCache = PropertyMap.LoadParameterMap();

            return m_PropertyMapCache;
         }
      }

      /// <summary>
      /// The CertifiedEntitiesAndPsetCache
      /// </summary>
      public static IFCCertifiedEntitiesAndPSets CertifiedEntitiesAndPsetsCache
      {
         get
         {
            if (m_CertifiedEntitiesAndPsetCache == null)
               m_CertifiedEntitiesAndPsetCache = new IFCCertifiedEntitiesAndPSets();

            return m_CertifiedEntitiesAndPsetCache;
         }
      }

      /// <summary>
      /// A local copy of the internal IfcCartesianPoint for the global origin.
      public static IFCAnyHandle Global3DOriginHandle
      {
         get
         {
            if (m_Global3DOriginHandle == null)
               m_Global3DOriginHandle = ExporterIFCUtils.GetGlobal3DOriginHandle();
            return m_Global3DOriginHandle;
         }
      }

      /// <summary>
      /// A cache of offset applied to the host model (from the shared coords) to be used in the Link file
      /// </summary>
      public static Transform ScaledTransformOffsetFromSharedCoords { get; set; } = Transform.Identity;

      /// <summary>
      /// Collection of IFC Handles to delete
      /// </summary>
      public static HashSet<IFCAnyHandle> HandleToDeleteCache
      {
         get
         {
            if (m_HandleToDelete == null)
               m_HandleToDelete = new HashSet<IFCAnyHandle>();
            return m_HandleToDelete;
         }
      }

      /// <summary>
      /// The Cache for 2D curves information of a FamilySymbol
      /// </summary>
      public static IDictionary<ElementId, IList<Curve>> Object2DCurvesCache
      {
         get
         {
            if (m_Object2DCurves == null)
               m_Object2DCurves = new Dictionary<ElementId, IList<Curve>>();
            return m_Object2DCurves;
         }
      }

      /// <summary>
      /// Clear all caches contained in this manager.
      /// </summary>
      public static void Clear(bool fullClear)
      {
         if (fullClear)
         {
            m_CertifiedEntitiesAndPsetCache = null;
            ExporterIFC = null;
            m_ExportOptionsCache = null;
            m_Global3DOriginHandle = null;
            Context2DHandles.Clear();
            Context3DHandles.Clear();
            GUIDCache.Clear();
            OwnerHistoryHandle = null;
            ParameterCache.Clear();
            ProjectHandle = null;
            m_UnitsCache = null;
         }

         // Special case: if we are sharing the IfcSite, don't clear it after the host
         // document export.
         if (fullClear || ExportOptionsCache.ExportLinkedFileAs != LinkedFileExportAs.ExportSameSite)
         {
            SiteHandle = null;
         }

         if (m_AllocatedGeometryObjectCache != null)
            m_AllocatedGeometryObjectCache.DisposeCache();
         ParameterUtil.ClearParameterValueCaches();

         m_AllocatedGeometryObjectCache = null;
         m_AreaSchemeCache = null;
         m_AssemblyInstanceCache = null;
         BaseLinkedDocumentGUID = null;
         m_BeamSystemCache = null;
         BuildingHandle = null;
         m_CanExportBeamGeometryAsExtrusionCache = null;
         m_CategoryClassNameCache = null;
         m_CategoryTypeCache = null;
         m_CeilingSpaceRelCache = null;
         m_ClassificationCache = null;
         m_ClassificationLocationCache = null;
         ContainmentCache = new ContainmentCache();
         ComplexPropertyCache.Clear();
         BaseQuantitiesCache.Clear();
         m_CreatedInternalPropertySets = null;
         m_CreatedSpecialPropertySets = null;
         m_CurveAnnotationCache = null;
         m_DBViewsToExport = null;
         m_DefaultCartesianTransformationOperator3D = null;
         m_DoorWindowDelayedOpeningCreatorCache = null;
         m_DummyHostCache = null;
         m_ElementsInAssembliesCache = null;
         ElementIdMaterialParameterCache.Clear();
         m_ElementToHandleCache = null;
         m_ElementTypeToHandleCache = null;
         m_FabricAreaHandleCache = null;
         m_FabricParamsCache = null;
         m_FamilySymbolToTypeInfoCache = null;
         m_GridCache = null;
         m_GroupCache = null;
         m_GUIDsToStoreCache = null;
         m_HandleToDelete = null;
         m_HandleToElementCache = null;
         m_HostObjectsLevelIndex = null;
         m_HostPartsCache = null;
         m_InternallyCreatedRootHandles = null;
         m_IsExternalParameterValueCache = null;
         LevelInfoCache.Clear(ExporterIFC);
         m_MaterialIdToStyleHandleCache = null;
         MaterialSetUsageCache = new MaterialSetUsageCache();
         m_MaterialSetCache = null;
         m_MaterialConstituentCache = null;
         m_MaterialConstituentSetCache = null;
         m_MaterialHandleCache = null;
         MaterialRelationsCache = new MaterialRelationsCache();
         m_MEPCache = null;
         m_Object2DCurves = null;
         m_PartExportedCache = null;
         m_PresentationLayerSetCache = null;
         m_PresentationStyleCache = null;
         m_PropertyInfoCache = null;
         m_PropertyMapCache = null;
         m_PropertySetsForTypeCache = null;
         m_PreDefinedPropertySetsForTypeCache = null;
         m_RailingCache = null;
         m_RailingSubElementCache = null;
         m_SpaceBoundaryCache = null;
         m_SpaceInfoCache = null;
         m_SpaceOccupantInfoCache = null;
         m_StairRampContainerInfoCache = null;
         m_SystemsCache = null;
         m_TrussCache = null;
         m_TypePropertyInfoCache = null;
         m_TypeRelationsCache = null;
         m_ViewScheduleElementCache = null;
         m_WallConnectionDataCache = null;
         WallCrossSectionCache.Clear();
         m_ZoneCache = null;
         m_ZoneInfoCache = null;
         QtoSetCreated.Clear();
      }
   }
}
