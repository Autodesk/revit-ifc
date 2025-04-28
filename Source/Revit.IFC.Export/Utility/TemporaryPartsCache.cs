using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Exporter.PropertySet;
using System.Collections.Generic;

namespace Revit.IFC.Export.Utility
{

   /// <summary>
   /// The class contains additional information for temporary parts.
   /// </summary>
   public class TemporaryPartInfo
   {
      public string IfcName { get; set; } = null;

      public string IfcTag { get; set; } = null;

      public int LayerIndex { get; set; } = -1;

      public Dictionary<ElementId, double> MaterialToVolumeMap = null;

      public HashSet<IFCAnyHandle> InternalPropertySets = null;
   };

   /// <summary>
   /// Used to keep a cache of temporary parts geomety
   /// and all the additional information that is not accessible during export.
   /// </summary>
   public class TemporaryPartsCache
   {
      /// <summary>
      /// The dictionary mapping from Host element id to list of associated parts geometry elements.
      /// </summary>
      private Dictionary<ElementId, List<GeometryElement>> ElementToPartGeometries { get; set; } = new();

      /// <summary>
      /// The dictionary mapping from part geometry element to additional information about temporary part.
      /// </summary>
      private Dictionary<GeometryElement, TemporaryPartInfo> GeometryToPartInfo { get; set; } = new();

      /// <summary>
      /// The dictionary mapping from Host element id to ExportPartAs type.
      /// </summary>
      private Dictionary<ElementId, ExporterUtil.ExportPartAs> PartExportTypes { get; set; } = new();

      /// <summary>
      /// The temporary part presentation layer.
      /// </summary>
      public string TemporaryPartPresentationLayer { get; set; } = null;

      /// <summary>
      /// Register the part geometry elements for the host element.
      /// </summary>
      public void Register(ElementId elementId, List<GeometryElement> partGeometries)
      {
         if (elementId == ElementId.InvalidElementId)
            return;

         ElementToPartGeometries.TryAdd(elementId, partGeometries);
         PartExportTypes.TryAdd(elementId, ExporterUtil.ExportPartAs.None);
      }

      /// <summary>
      /// Find the registered part geometry elements for the host element.
      /// </summary>
      public bool Find(ElementId elementId, out List<GeometryElement> partGeometries)
      {  
         return ElementToPartGeometries.TryGetValue(elementId, out partGeometries);
      }

      /// <summary>
      /// Collect additional information about temporary part.
      /// </summary>
      public void CollectTemporaryPartInfo(ExporterIFC exporterIFC, GeometryElement geometryElement, Element part)
      {
         if (geometryElement == null)
            return;

         TemporaryPartInfo temporaryPartInfo = new();
         temporaryPartInfo.IfcName = NamingUtil.GetIFCName(part);
         temporaryPartInfo.IfcTag = NamingUtil.GetTagOverride(part);

         // Get layer index
         if (PartExporter.GetLayerIndex(part, out int layerIndex))
            temporaryPartInfo.LayerIndex = layerIndex;

         // Collect material volume dictionary
         ICollection<ElementId> materialIds = part.GetMaterialIds(false);
         if (materialIds.Count > 0)
         {
            temporaryPartInfo.MaterialToVolumeMap = new();
            foreach (ElementId materialId in materialIds)
            {
               double materialVolume = part.GetMaterialVolume(materialId);
               temporaryPartInfo.MaterialToVolumeMap.TryAdd(materialId, materialVolume);
            }
         }

         // Create internal property sets
         temporaryPartInfo.InternalPropertySets = PropertyUtil.CreateInternalRevitPropertySetsForTemporaryParts(exporterIFC, part);

         GeometryToPartInfo.Add(geometryElement, temporaryPartInfo);
      }

      /// <summary>
      /// Determines whether the cache contains the specified temporary part.
      /// </summary>
      public bool HasTemporaryParts(ElementId elementId)
      {
         return ElementToPartGeometries.ContainsKey(elementId);
      }

      /// <summary>
      /// Get the number of part geometries for the host element.
      /// </summary>
      public int GeometriesCount(ElementId elementId)
      {
         if (ElementToPartGeometries.TryGetValue(elementId, out List<GeometryElement> geometries))
            return geometries.Count;
         return 0;
      }

      /// <summary>
      /// Find the additional information about temporary part.
      /// </summary>
      public bool FindInfo(GeometryElement geometry, out TemporaryPartInfo partInfo)
      {
         return GeometryToPartInfo.TryGetValue(geometry, out partInfo);
      }

      /// <summary>
      /// Set the export type for the part.
      /// </summary>
      public bool SetPartExportType(ElementId elementId, ExporterUtil.ExportPartAs newExportType)
      {
         if (PartExportTypes.TryGetValue(elementId, out ExporterUtil.ExportPartAs exportPartAs))
         {
            exportPartAs = newExportType;
            return true;
         }
         return false;
      }

      /// <summary>
      /// Get the export type for the part.
      /// </summary>
      public ExporterUtil.ExportPartAs GetPartExportType(ElementId elementId)
      {
         if (PartExportTypes.TryGetValue(elementId, out ExporterUtil.ExportPartAs exportPartAs))
         {
            return exportPartAs;
         }
         return ExporterUtil.ExportPartAs.None;
      }

      /// <summary>
      /// Clear the cache.
      /// </summary>
      public void Clear()
      {
         ElementToPartGeometries.Clear();
         GeometryToPartInfo.Clear();
      }
   }
}
