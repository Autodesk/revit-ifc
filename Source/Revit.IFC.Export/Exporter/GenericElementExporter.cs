using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Export.Exporter.PropertySet;

namespace Revit.IFC.Export.Exporter
{
   class GenericElementExporter
   {
      /// <summary>
      /// Exports an element as building element proxy.
      /// </summary>
      /// <remarks>
      /// This function is called from the Export function, but can also be called directly if you do not
      /// want CreateInternalPropertySets to be called.
      /// </remarks>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      /// <returns>The handle if created, null otherwise.</returns>
      public static IFCAnyHandle ExportSimpleGenericElement(ExporterIFC exporterIFC, Element element,
          GeometryElement geometryElement, ProductWrapper productWrapper, IFCExportInfoPair exportType)
      {
         if (element == null || geometryElement == null)
            return null;

         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         if (exportType.ExportInstance == IFCEntityType.UnKnown)
            exportType.SetValueWithPair(IFCEntityType.IfcBuildingElementProxy, exportType.ValidatedPredefinedType);
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(exportType.ExportInstance))
            return null;

         // Check for containment override
         IFCAnyHandle overrideContainerHnd = null;
         ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, element, out overrideContainerHnd);

         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle instanceHandle = null;
         using (IFCTransaction tr = new IFCTransaction(file))
         {
            using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, element, null, null, overrideContainerId, overrideContainerHnd))
            {
               using (IFCExportBodyParams ecData = new IFCExportBodyParams())
               {
                  ecData.SetLocalPlacement(placementSetter.LocalPlacement);

                  ElementId categoryId = CategoryUtil.GetSafeCategoryId(element);

                  BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                  IFCAnyHandle representation = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC, element,
                      categoryId, geometryElement, bodyExporterOptions, null, ecData, true);

                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(representation))
                  {
                     ecData.ClearOpenings();
                     return null;
                  }

                  string guid = GUIDUtil.CreateGUID(element);
                  IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
                  IFCAnyHandle localPlacement = ecData.GetLocalPlacement();
                  IFCAnyHandle styleHandle = null;

                  instanceHandle = FamilyExporterUtil.ExportGenericInstance(exportType, exporterIFC, element, productWrapper, placementSetter, ecData, guid, ownerHistory,
                     representation, null);

                  if (exportType.ExportType != IFCEntityType.UnKnown)
                  {
                     FamilySymbol familySymbol = (element as FamilyInstance)?.Symbol;
                     if (familySymbol != null)
                     {
                        HashSet<IFCAnyHandle> propertySetsOpt = new HashSet<IFCAnyHandle>();
                        IList<IFCAnyHandle> repMapListOpt = new List<IFCAnyHandle>();

                        string typeGuid = FamilyExporterUtil.GetGUIDForFamilySymbol(element as FamilyInstance, 
                           familySymbol, exportType);
                        styleHandle = FamilyExporterUtil.ExportGenericType(exporterIFC, exportType,
                           exportType.ValidatedPredefinedType, propertySetsOpt, repMapListOpt,
                           element, familySymbol, typeGuid);
                        productWrapper.RegisterHandleWithElementType(familySymbol, exportType, styleHandle, propertySetsOpt);
                     }

                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(styleHandle))
                        styleHandle = ExporterUtil.CreateGenericTypeFromElement(element, exportType, 
                           file, ownerHistory, exportType.ValidatedPredefinedType, productWrapper);
                  }

                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(instanceHandle))
                  {
                     if (exportType.ExportInstance == IFCEntityType.IfcSpace)
                        productWrapper.AddSpace(element, instanceHandle, placementSetter.LevelInfo, ecData, true, exportType);
                     else
                        productWrapper.AddElement(element, instanceHandle, placementSetter.LevelInfo, ecData, true, exportType);

                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(styleHandle))
                        ExporterCacheManager.TypeRelationsCache.Add(styleHandle, instanceHandle);
                  }
               }
               tr.Commit();
            }
         }

         return instanceHandle;
      }

      private static GeometryInstance GetTheGeometryInstance(GeometryElement geomElem)
      {
         GeometryInstance geometryInstance = null;
         foreach (GeometryObject geomObj in geomElem)
         {
            GeometryInstance geomInst = geomObj as GeometryInstance;
            if (geomInst != null)
            {
               if (geometryInstance == null)
               {
                  geometryInstance = geomInst;
                  continue;
               }
               else
               {
                  return null;
               }
            }

            Solid solid = geomObj as Solid;
            if (solid != null)
            {
               double? volume = GeometryUtil.GetSafeVolume(solid);
               if (volume.HasValue || MathUtil.IsAlmostZero(volume.Value))
                  continue;
            }

            return null;
         }

         return geometryInstance;
      }

      private static bool ExportGenericElementAsMappedItem(ExporterIFC exporterIFC,
         Element element, GeometryElement geomElem, IFCExportInfoPair exportType,
         ProductWrapper wrapper)
      {
         GeometryInstance geometryInstance = GetTheGeometryInstance(geomElem);
         if (geometryInstance == null)
            return false;

         GeometryElement exportGeometry = geometryInstance.GetSymbolGeometry();
         if (exportGeometry == null)
            return false;

         ElementId symbolId = geometryInstance.Symbol?.Id ?? ElementId.InvalidElementId;
         ElementType elementType = element.Document.GetElement(symbolId) as ElementType;
         if (elementType == null)
            return false;

         Transform originalTrf = geometryInstance.Transform;
         // Can't handle mirrored transforms yet.
         if (originalTrf.HasReflection)
            return false; 
         
         ElementId categoryId = CategoryUtil.GetSafeCategoryId(element);

         IFCFile file = exporterIFC.GetFile();

         IList<Transform> repMapTrfList = new List<Transform>();
         BodyData bodyData = null;
         FamilyTypeInfo typeInfo = new FamilyTypeInfo();
         IFCExportBodyParams extraParams = typeInfo.extraParams;

         Transform offsetTransform = Transform.Identity;

         // We will create a new mapped type if we haven't already created the type.
         // GUID_TODO: This assumes that there are no types relating to objects split by level,
         // or to doors/windows that are flipped.
         var typeKey = new TypeObjectKey(symbolId, ElementId.InvalidElementId,
            false, exportType);

         FamilyTypeInfo currentTypeInfo = 
            ExporterCacheManager.FamilySymbolToTypeInfoCache.Find(typeKey);
         bool found = currentTypeInfo.IsValid();
         if (!found)
         {
            IList<IFCAnyHandle> representations3D = new List<IFCAnyHandle>();

            IFCAnyHandle dummyPlacement = ExporterUtil.CreateLocalPlacement(file, null, null);
            extraParams.SetLocalPlacement(dummyPlacement);

            using (TransformSetter trfSetter = TransformSetter.Create())
            {
               BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(false, ExportOptionsCache.ExportTessellationLevel.ExtraLow);

               bodyData = BodyExporter.ExportBody(exporterIFC, element, categoryId,
                  ExporterUtil.GetSingleMaterial(element), exportGeometry,
                  bodyExporterOptions, extraParams);
               typeInfo.MaterialIdList = bodyData.MaterialIds;
               offsetTransform = bodyData.OffsetTransform;

               // This code does not handle openings yet.
               // The intention for this is FabricationParts and DirectShapes which do not
               // currently have opening.
               // If they can have openings in the future, we can add this.
               IFCAnyHandle bodyRepHnd = bodyData.RepresentationHnd;
               if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRepHnd) || extraParams.GetOpenings().Count > 0)
                  return false;

               representations3D.Add(bodyRepHnd);
               repMapTrfList.Add(null);
            }

            typeInfo.StyleTransform = ExporterIFCUtils.GetUnscaledTransform(exporterIFC,
               extraParams.GetLocalPlacement());

            IFCAnyHandle typeStyle = FamilyInstanceExporter.CreateTypeEntityHandle(exporterIFC,
               typeKey, ref typeInfo, null, representations3D, repMapTrfList, null,
               element, elementType, elementType, ElementId.InvalidElementId, false, false,
               exportType, out HashSet<IFCAnyHandle> propertySets);

            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(typeStyle))
            {
               wrapper.RegisterHandleWithElementType(elementType, exportType, typeStyle,
                  propertySets);

               typeInfo.Style = typeStyle;

               CategoryUtil.TryToCreateMaterialAssocation(exporterIFC, bodyData, elementType,
                  element, exportGeometry, typeStyle, typeInfo);

               // Create other generic classification from ClassificationCode(s)
               ClassificationUtil.CreateClassification(exporterIFC, file, elementType, typeStyle);
               ClassificationUtil.CreateUniformatClassification(exporterIFC, file, elementType, typeStyle);
            }
         }

         if (found && !typeInfo.IsValid())
            typeInfo = currentTypeInfo;

         // we'll pretend we succeeded, but we'll do nothing.
         if (!typeInfo.IsValid())
            return false;

         extraParams = typeInfo.extraParams;

         // We expect no openings, so always add to map.
         ExporterCacheManager.FamilySymbolToTypeInfoCache.Register(typeKey, typeInfo, false);

         XYZ scaledMapOrigin = XYZ.Zero;
         Transform scaledTrf = originalTrf.Multiply(typeInfo.StyleTransform);

         // create instance.  
         IList<IFCAnyHandle> shapeReps = FamilyInstanceExporter.CreateShapeRepresentations(exporterIFC,
            file, element, categoryId, typeInfo, scaledMapOrigin);
         if (shapeReps == null)
            return false;

         Transform boundingBoxTrf = (offsetTransform != null) ? offsetTransform.Inverse : Transform.Identity;
         boundingBoxTrf = boundingBoxTrf.Multiply(scaledTrf.Inverse);
         IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geomElem, boundingBoxTrf);

         if (boundingBoxRep != null)
            shapeReps.Add(boundingBoxRep);

         IFCAnyHandle repHnd = (shapeReps.Count > 0) ? IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapeReps) : null;

         using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element, scaledTrf, null))
         {
            IFCAnyHandle instanceHandle = null;
            IFCAnyHandle localPlacement = setter.LocalPlacement;
            bool materialAlreadyAssociated = false;

            // We won't create the instance if: 
            // (1) we are exporting to CV2.0/RV, (2) we have no 2D, 3D, or bounding box geometry, and (3) we aren't exporting parts.
            if (!(repHnd == null
                  && (ExporterCacheManager.ExportOptionsCache.ExportAsCoordinationView2
                  || ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)))
            {
               string instanceGUID = GUIDUtil.CreateGUID(element);

               bool isChildInContainer = element.AssemblyInstanceId != ElementId.InvalidElementId;

               if (IFCAnyHandleUtil.IsNullOrHasNoValue(instanceHandle))
               {
                  bool isBuildingElementProxy =
                        ((exportType.ExportInstance == IFCEntityType.IfcBuildingElementProxy) ||
                        (exportType.ExportType == IFCEntityType.IfcBuildingElementProxyType));

                  ElementId roomId = setter.UpdateRoomRelativeCoordinates(element,
                     out IFCAnyHandle localPlacementToUse);
                  bool containedInSpace = (roomId != ElementId.InvalidElementId) && (exportType.ExportInstance != IFCEntityType.IfcSystemFurnitureElement);
                  IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

                  if (!isBuildingElementProxy)
                  {
                     instanceHandle = IFCInstanceExporter.CreateGenericIFCEntity(exportType, exporterIFC, element, instanceGUID,
                        ownerHistory, localPlacementToUse, repHnd);
                  }
                  else
                  {
                     instanceHandle = IFCInstanceExporter.CreateBuildingElementProxy(exporterIFC, element, instanceGUID,
                        ownerHistory, localPlacementToUse, repHnd, exportType.ValidatedPredefinedType);
                  }

                  bool associateToLevel = !containedInSpace && !isChildInContainer;
                  wrapper.AddElement(element, instanceHandle, setter, extraParams, associateToLevel, exportType);
                  if (containedInSpace)
                     ExporterCacheManager.SpaceInfoCache.RelateToSpace(roomId, instanceHandle);
               }

               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(instanceHandle))
               {
                  if (ElementFilteringUtil.IsMEPType(exportType) || ElementFilteringUtil.ProxyForMEPType(element, exportType))
                  {
                     ExporterCacheManager.MEPCache.Register(element, instanceHandle);
                  }

                  ExporterCacheManager.HandleToElementCache.Register(instanceHandle, element.Id);

                  if (!materialAlreadyAssociated)
                  {
                     // Create material association for the instance only if the the istance geometry is different from the type
                     // or the type does not have any material association
                     IFCAnyHandle constituentSetHnd = ExporterCacheManager.MaterialConstituentSetCache.Find(symbolId);
                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(constituentSetHnd)
                        && bodyData != null && bodyData.RepresentationItemInfo != null && bodyData.RepresentationItemInfo.Count > 0)
                     {
                        CategoryUtil.CreateMaterialAssociationWithShapeAspect(exporterIFC, element, instanceHandle, bodyData.RepresentationItemInfo);
                     }
                     else
                     {
                        // Create material association in case if bodyData is null
                        CategoryUtil.CreateMaterialAssociation(exporterIFC, instanceHandle, typeInfo.MaterialIdList);
                     }
                  }

                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(typeInfo.Style))
                     ExporterCacheManager.TypeRelationsCache.Add(typeInfo.Style, instanceHandle);
               }
            }
         }
         return true;
      }


      /// <summary>
      /// Exports a fabrication part to corresponding IFC object.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element to be exported.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static bool ExportElement(ExporterIFC exporterIFC,
         Element element, GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         string ifcEnumType;
         IFCExportInfoPair exportType = ExporterUtil.GetProductExportType(exporterIFC, element, out ifcEnumType);

         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         IFCEntityType elementClassTypeEnum;
         if (Enum.TryParse(exportType.ExportInstance.ToString(), out elementClassTypeEnum)
               || Enum.TryParse(exportType.ExportType.ToString(), out elementClassTypeEnum))
            if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
               return false;

         if (ExportGenericElementAsMappedItem(exporterIFC, element, geometryElement,
            exportType, productWrapper))
            return true;

         if (FamilyInstanceExporter.ExportGenericToSpecificElement(exporterIFC,
            element, ref geometryElement, exportType, ifcEnumType, productWrapper))
            return true;

         return (ExportSimpleGenericElement(exporterIFC, element, geometryElement, productWrapper, 
            exportType) != null);
      }
   }
}
