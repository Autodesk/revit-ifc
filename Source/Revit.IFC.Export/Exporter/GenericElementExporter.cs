using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

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
      public static IFCAnyHandle ExportGenericElement(ExporterIFC exporterIFC, Element element,
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
               using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
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
                     representation, exportType.ValidatedPredefinedType, null);

                  if (exportType.ExportType != IFCEntityType.UnKnown)
                  {
                     if (element is FamilyInstance)
                     {
                        FamilySymbol familySymbol = (element as FamilyInstance).Symbol;
                        if (familySymbol != null)
                        {
                           HashSet<IFCAnyHandle> propertySetsOpt = new HashSet<IFCAnyHandle>();
                           IList<IFCAnyHandle> repMapListOpt = new List<IFCAnyHandle>();

                           styleHandle = FamilyExporterUtil.ExportGenericType(exporterIFC, exportType, exportType.ValidatedPredefinedType,
                              propertySetsOpt, repMapListOpt, element, familySymbol);
                           productWrapper.RegisterHandleWithElementType(familySymbol, exportType, styleHandle, propertySetsOpt);
                        }
                     }
                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(styleHandle))
                        styleHandle = ExporterUtil.CreateGenericTypeFromElement(element, exportType, file, ownerHistory, exportType.ValidatedPredefinedType, productWrapper);
                  }

                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(instanceHandle))
                  {
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
   }
}
