using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Exporter;

namespace Revit.IFC.Export.Utility
{
   public class MaterialLayerSetInfo
   {
      public class MaterialInfo
      {
         public MaterialInfo(ElementId baseMatId, string layerName, double matWidth, MaterialFunctionAssignment function)
         {
            BaseMatId = baseMatId;
            LayerName = layerName;
            ShapeAspectName = layerName;
            Width = matWidth;
            Function = function;
         }

         public ElementId BaseMatId { get; private set; } = ElementId.InvalidElementId;

         public string LayerName { get; private set; } = null;

         public string ShapeAspectName { get; set; } = null;

         public double Width { get; set; } = 0.0;

         public MaterialFunctionAssignment Function { get; private set; } = MaterialFunctionAssignment.None;
      }

      private ExporterIFC ExporterIFC { get; set; } = null;

      private Element Element { get; set; } = null;

      private ProductWrapper ProductWrapper { get; set; } = null;

      private GeometryElement GeometryElement { get; set; } = null;
      
      bool NeedToGenerateIFCObjects { get; set; } = false;

      /// <summary>
      /// Initialize MaterialLayerSetInfo for element
      /// </summary>
      /// <param name="exporterIFC">the exporter IFC</param>
      /// <param name="element">the element</param>
      /// <param name="productWrapper">the product wrapper</param>
      public MaterialLayerSetInfo(ExporterIFC exporterIFC, Element element, ProductWrapper productWrapper, GeometryElement geometryElement = null)
      {
         Element = element;
         ExporterIFC = exporterIFC;
         ProductWrapper = productWrapper;
         GeometryElement = geometryElement;
         CollectMaterialLayerSet();
      }

      public void SingleMaterialOverride (ElementId materialId, double materialWidth)
      {
         GenerateIFCObjectsIfNeeded();

         Material material = Element.Document.GetElement(materialId) as Material;
         string layerName = "Layer";
         if (material != null)
         {
            layerName = NamingUtil.GetMaterialLayerName(material);
         }
         MaterialInfo matInfo = new MaterialInfo(materialId, layerName, materialWidth, MaterialFunctionAssignment.None);
         MaterialIds.Add(matInfo);

         IFCAnyHandle singleMaterialOverrideHnd = IFCInstanceExporter.CreateMaterial(ExporterIFC.GetFile(), layerName, null, null);
         ExporterCacheManager.MaterialHandleCache.Register(MaterialIds[0].BaseMatId, singleMaterialOverrideHnd);
         m_GeneratedMaterialLayerSetHandle = singleMaterialOverrideHnd;
      }

      /// <summary>
      /// Return status whether the LayerSetInfo is empty
      /// </summary>
      public bool IsEmpty
      {
         get
         {
            return (m_GeneratedMaterialLayerSetHandle == null && MaterialIds.Count == 0);
         }
      }

      /// <summary>
      /// List of Material Ids and their associated layer names of the material layer set
      /// </summary>
      public List<MaterialInfo> MaterialIds { get; private set; } = new List<MaterialInfo>();

      /// <summary>
      /// MaterialLayerSet Collected or MaterialConstituentSet (for IFC4RV)
      /// Warning: Do not call Properties inside class because this can break lazy generation of IFC objects.
      ///          Use private members instead.
      /// </summary>
      private IFCAnyHandle m_GeneratedMaterialLayerSetHandle = null;
      
      public IFCAnyHandle MaterialLayerSetHandle
      {
         get
         {
            GenerateIFCObjectsIfNeeded();
            return m_GeneratedMaterialLayerSetHandle;
         }
      }

      /// <summary>
      /// Primary Material handle
      /// Warning: Do not call Properties inside class because this can break lazy generation of IFC objects.
      ///          Use private members instead.
      /// </summary>
      private IFCAnyHandle m_GeneratedPrimaryMaterialHandle = null;
      
      public IFCAnyHandle PrimaryMaterialHandle
      {
         get
         {
            GenerateIFCObjectsIfNeeded();

            return m_GeneratedPrimaryMaterialHandle;
         }
      }

      /// <summary>
      /// Material layer quantity properties
      /// Warning: Do not call Properties inside class because this can break lazy generation of IFC objects.
      ///          Use private members instead.
      /// </summary>
      private HashSet<IFCAnyHandle> m_GeneratedLayerQuantityWidthHnd = new HashSet<IFCAnyHandle>();
      
      public HashSet<IFCAnyHandle> LayerQuantityWidthHnd
      {
         get
         {
            GenerateIFCObjectsIfNeeded();

            return m_GeneratedLayerQuantityWidthHnd;
         }
      }

      /// <summary>
      /// Total thickness of the material layer set.
      /// </summary>
      public double TotalThickness { get; private set; } = 0.0;

      /// <summary>
      /// Collect information about material layer.
      ///   For IFC4RV Architectural exchange, it will generate IfcMatrialConstituentSet along with the relevant IfcShapeAspect and the width in the quantityset
      ///   For IFC4RV Structural exchange, it will generate multiple components as IfcBuildingElementPart for each layer
      ///   For others IfcMaterialLayer will be created
      /// </summary>
      private void CollectMaterialLayerSet()
      {
         ElementId typeElemId = Element.GetTypeId();
         IFCAnyHandle materialLayerSet = ExporterCacheManager.MaterialSetCache.FindLayerSet(typeElemId);
         // Roofs with no components are only allowed one material.  We will arbitrarily choose the thickest material.
         m_GeneratedPrimaryMaterialHandle = ExporterCacheManager.MaterialSetCache.FindPrimaryMaterialHnd(typeElemId);

         bool materialHandleIsNotValid = IFCAnyHandleUtil.IsNullOrHasNoValue(materialLayerSet);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(materialLayerSet) || materialHandleIsNotValid)
         {
            if (materialHandleIsNotValid)
            {
               UnregisterIFCHandles();
            }
            NeedToGenerateIFCObjects = true;

            List<double> widths = new List<double>();
            List<MaterialFunctionAssignment> functions = new List<MaterialFunctionAssignment>();

            HostObjAttributes hostObjAttr = Element.Document.GetElement(typeElemId) as HostObjAttributes;
            if (hostObjAttr == null)
            {
               // It does not have the HostObjAttribute (where we will get the compound structure for material layer set.
               // We will define a single material instead and create the material layer set of this single material if there is enough information (At least Material id and thickness) 
               FamilyInstance familyInstance = Element as FamilyInstance;
               if (familyInstance == null)
               {
                  if (GeometryElement != null)
                  {
                     ElementId matId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(GeometryElement, Element);
                     CategoryUtil.CreateMaterialAssociation(ExporterIFC, ProductWrapper.GetAnElement(), matId);
                  }
                  return;
               }

               FamilySymbol familySymbol = familyInstance.Symbol;
               ICollection<ElementId> famMatIds = familySymbol.GetMaterialIds(false);
               if (famMatIds.Count == 0)
               {
                  // For some reason Plate type may not return any Material id
                  ElementId baseMatId = CategoryUtil.GetBaseMaterialIdForElement(Element);
                  Material material = Element.Document.GetElement(baseMatId) as Material;
                  if (material == null)
                     return;

                  string layerName = NamingUtil.GetMaterialLayerName(material);
                  double matWidth = 0.0;
                  Parameter thicknessPar = familySymbol.get_Parameter(BuiltInParameter.CURTAIN_WALL_SYSPANEL_THICKNESS);
                  if (thicknessPar != null)
                     matWidth = thicknessPar.AsDouble();
                  widths.Add(matWidth);

                  if (baseMatId != ElementId.InvalidElementId)
                     MaterialIds.Add(new MaterialInfo(baseMatId, layerName, matWidth, MaterialFunctionAssignment.None));
                  // How to get the thickness? For CurtainWall Panel (PanelType), there is a builtin parameter CURTAINWALL_SYSPANEL_THICKNESS

                  functions.Add(MaterialFunctionAssignment.None);
               }
               else
               {
                  foreach (ElementId matid in famMatIds)
                  {
                     // How to get the thickness? For CurtainWall Panel (PanelType), there is a builtin parameter CURTAINWALL_SYSPANEL_THICKNESS
                     double matWidth = familySymbol.get_Parameter(BuiltInParameter.CURTAIN_WALL_SYSPANEL_THICKNESS)?.AsDouble() ?? 
                        ParameterUtil.GetSpecialThicknessParameter(familySymbol);
                     
                     if (MathUtil.IsAlmostZero(matWidth))
                        continue;

                     widths.Add(matWidth);
                     ElementId baseMatId = CategoryUtil.GetBaseMaterialIdForElement(Element);
                     if (matid != ElementId.InvalidElementId)
                     {
                        Material material = Element.Document.GetElement(matid) as Material;
                        if (material != null)
                        {
                           string layerName = NamingUtil.GetMaterialLayerName(material);
                           MaterialIds.Add(new MaterialInfo(matid, layerName, matWidth, MaterialFunctionAssignment.None));
                        }
                     }
                     else
                     {
                        MaterialIds.Add(new MaterialInfo(baseMatId, null, matWidth, MaterialFunctionAssignment.None));
                     }

                     functions.Add(MaterialFunctionAssignment.None);
                  }
               }
            }
            else
            {
               ElementId baseMatId = CategoryUtil.GetBaseMaterialIdForElement(Element);
               CompoundStructure cs = hostObjAttr.GetCompoundStructure();

               if (cs != null)
               {
                  double scaledOffset = 0.0, scaledWallWidth = 0.0, wallHeight = 0.0;
                  Wall wall = Element as Wall;
                  if (wall != null)
                  {
                     scaledWallWidth = UnitUtil.ScaleLength(wall.Width);
                     scaledOffset = -scaledWallWidth / 2.0;
                     BoundingBoxXYZ boundingBox = wall.get_BoundingBox(null);
                     if (boundingBox != null)
                        wallHeight = boundingBox.Max.Z - boundingBox.Min.Z;
                  }

                  //TODO: Vertically compound structures are not yet supported by export.
                  if (!cs.IsVerticallyHomogeneous() && !MathUtil.IsAlmostZero(wallHeight))
                     cs = cs.GetSimpleCompoundStructure(wallHeight, wallHeight / 2.0);

                  for (int ii = 0; ii < cs.LayerCount; ++ii)
                  {
                     double matWidth = cs.GetLayerWidth(ii);
                     
                     ElementId matId = cs.GetMaterialId(ii);
                     widths.Add(matWidth);
                     // save layer function into ProductWrapper, 
                     // it's used while exporting "Function" of Pset_CoveringCommon
                     functions.Add(cs.GetLayerFunction(ii));

                     if (matId != ElementId.InvalidElementId)
                     {
                        Material material = Element.Document.GetElement(matId) as Material;
                        if (material != null)
                        {
                           string layerName = NamingUtil.GetMaterialLayerName(material);
                           MaterialIds.Add(new MaterialInfo(matId, layerName, matWidth, functions.Last()));
                        }
                     }
                     else
                     {
                        MaterialIds.Add(new MaterialInfo(baseMatId, null, matWidth, functions.Last()));
                     }
                  }
               }

               if (MaterialIds.Count == 0)
               {
                  double matWidth = cs?.GetWidth() ?? 0.0;
                  widths.Add(matWidth);
                  if (baseMatId != ElementId.InvalidElementId)
                  {
                     Material material = Element.Document.GetElement(baseMatId) as Material;
                     if (material != null)
                     {
                        string layerName = NamingUtil.GetMaterialLayerName(material);
                        MaterialIds.Add(new MaterialInfo(baseMatId, layerName, matWidth, MaterialFunctionAssignment.None));
                     }
                  }
                  functions.Add(MaterialFunctionAssignment.None);
               }
            }
            TotalThickness = UnitUtil.ScaleLength(widths.Sum());
         }
         else
         {
            NeedToGenerateIFCObjects = false;

            m_GeneratedMaterialLayerSetHandle = materialLayerSet;

            MaterialLayerSetInfo mlsInfo = ExporterCacheManager.MaterialSetCache.FindMaterialLayerSetInfo(typeElemId);
            if (mlsInfo != null)
            {
               MaterialIds = mlsInfo.MaterialIds;
               m_GeneratedPrimaryMaterialHandle = mlsInfo.PrimaryMaterialHandle;
               m_GeneratedLayerQuantityWidthHnd = mlsInfo.LayerQuantityWidthHnd;
               TotalThickness = mlsInfo.TotalThickness;
            }
         }

         return;
      }

      private void GenerateIFCObjectsIfNeeded()
      {
         if (!NeedToGenerateIFCObjects)
            return;

         NeedToGenerateIFCObjects = false;
         IFCAnyHandle materialLayerSet = null;

         if (ProductWrapper != null && !ProductWrapper.ToNative().IsValidObject)
            ProductWrapper = null;

         ProductWrapper?.ClearFinishMaterials();

         // We can't create IfcMaterialLayers without creating an IfcMaterialLayerSet.  So we will simply collate here.
         IList<IFCAnyHandle> materialHnds = new List<IFCAnyHandle>();
         IList<int> widthIndices = new List<int>();
         double thickestLayer = 0.0;
         for (int ii = 0; ii < MaterialIds.Count; ++ii)
         {
            // Require positive width for IFC2x3 and before, and non-negative width for IFC4.
            if (MaterialIds[ii].Width < -MathUtil.Eps())
               continue;

            bool almostZeroWidth = MathUtil.IsAlmostZero(MaterialIds[ii].Width);
            if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 && almostZeroWidth)
               continue;

            if (almostZeroWidth)
            {
               MaterialIds[ii].Width = 0.0;
            }

            IFCAnyHandle materialHnd = CategoryUtil.GetOrCreateMaterialHandle(ExporterIFC, MaterialIds[ii].BaseMatId);
            if (m_GeneratedPrimaryMaterialHandle == null || (MaterialIds[ii].Width > thickestLayer))
            {
               m_GeneratedPrimaryMaterialHandle = materialHnd;
               thickestLayer = MaterialIds[ii].Width;
            }

            widthIndices.Add(ii);
            materialHnds.Add(materialHnd);

            if ((ProductWrapper != null) && (MaterialIds[ii].Function == MaterialFunctionAssignment.Finish1 || MaterialIds[ii].Function == MaterialFunctionAssignment.Finish2))
            {
               ProductWrapper.AddFinishMaterial(materialHnd);
            }
         }

         int numLayersToCreate = widthIndices.Count;
         if (numLayersToCreate == 0)
         {
            m_GeneratedMaterialLayerSetHandle = materialLayerSet;
            return;
         }

         // If it is a single material, check single material override (only IfcMaterial without IfcMaterialLayerSet with only 1 member)
         if (numLayersToCreate == 1 && ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
         {
            m_GeneratedMaterialLayerSetHandle = ExporterUtil.GetSingleMaterial(ExporterIFC, Element,
               MaterialIds[0].BaseMatId) ?? materialHnds[0];
            return;
         }

         IFCFile file = ExporterIFC.GetFile();
         Document document = ExporterCacheManager.Document;

         IList<IFCAnyHandle> layers = new List<IFCAnyHandle>(numLayersToCreate);
         IList<Tuple<string, IFCAnyHandle>> layerWidthQuantities = new List<Tuple<string, IFCAnyHandle>>();
         HashSet<string> layerNameUsed = new HashSet<string>();
         double totalWidth = 0.0;

         for (int ii = 0; ii < numLayersToCreate; ii++)
         {
            int widthIndex = widthIndices[ii];
            double scaledWidth = UnitUtil.ScaleLength(MaterialIds[widthIndex].Width);

            string layerName = "Layer";
            string description = null;
            string category = null;
            int? priority = null;

            IFCLogical? isVentilated = null;
            int isVentilatedValue;

            Material material = document.GetElement(MaterialIds[ii].BaseMatId) as Material;
            if (material != null)
            {
               if (ParameterUtil.GetIntValueFromElement(material, "IfcMaterialLayer.IsVentilated", out isVentilatedValue) != null)
               {
                  if (isVentilatedValue == 0)
                     isVentilated = IFCLogical.False;
                  else if (isVentilatedValue == 1)
                     isVentilated = IFCLogical.True;
               }

               if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
               {
                  layerName = MaterialIds[ii].ShapeAspectName;
                  if (string.IsNullOrEmpty(layerName))
                     layerName = "Layer";

                  description = NamingUtil.GetOverrideStringValue(material, "IfcMaterialLayer.Description",
                     IFCAnyHandleUtil.GetStringAttribute(materialHnds[ii], "Description"));
                  category = NamingUtil.GetOverrideStringValue(material, "IfcMaterialLayer.Category",
                     IFCAnyHandleUtil.GetStringAttribute(materialHnds[ii], "Category"));
                  int priorityValue;
                  if (ParameterUtil.GetIntValueFromElement(material, "IfcMaterialLayer.Priority", out priorityValue) != null)
                     priority = priorityValue;
               }
            }

            if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
            {
               // Skip component that has zero width (the will be no geometry associated to it
               if (MathUtil.IsAlmostZero(scaledWidth))
                  continue;

               IFCAnyHandle materialConstituent = IFCInstanceExporter.CreateMaterialConstituent(file, materialHnds[ii],
                  name: layerName, description: description, category: category);
               layers.Add(materialConstituent);

               IFCAnyHandle layerQtyHnd = IFCInstanceExporter.CreateQuantityLength(file, "Width", description, null, scaledWidth);
               totalWidth += scaledWidth;
               layerWidthQuantities.Add(new Tuple<string, IFCAnyHandle>(layerName, layerQtyHnd));
            }
            else
            {
               IFCAnyHandle materialLayer = IFCInstanceExporter.CreateMaterialLayer(file, materialHnds[ii], scaledWidth, isVentilated,
                  name: layerName, description: description, category: category, priority: priority);
               layers.Add(materialLayer);
            }
         }

         ElementId typeElemId = Element.GetTypeId();
         if (layers.Count > 0)
         {
            ElementType type = document.GetElement(typeElemId) as ElementType;
            string layerSetBaseName = type.FamilyName + ":" + type.Name;
            string layerSetName = NamingUtil.GetOverrideStringValue(type, "IfcMaterialLayerSet.Name", layerSetBaseName);
            string layerSetDesc = NamingUtil.GetOverrideStringValue(type, "IfcMaterialLayerSet.Description", null);

            if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
            {
               HashSet<IFCAnyHandle> constituents = new HashSet<IFCAnyHandle>(layers);
               m_GeneratedMaterialLayerSetHandle = CategoryUtil.GetOrCreateMaterialConstituentSet(file,
                  typeElemId, null, constituents, layerSetName, layerSetDesc);
               
               foreach (Tuple<string, IFCAnyHandle> layerWidthQty in layerWidthQuantities)
               {
                  m_GeneratedLayerQuantityWidthHnd.Add(IFCInstanceExporter.CreatePhysicalComplexQuantity(file, layerWidthQty.Item1, null,
                     new HashSet<IFCAnyHandle>() { layerWidthQty.Item2 }, "Layer", null, null));
               }
               // Finally create the total width as a quantity
               m_GeneratedLayerQuantityWidthHnd.Add(IFCInstanceExporter.CreateQuantityLength(file, "Width", null, null, totalWidth));
            }
            else
            {
               m_GeneratedMaterialLayerSetHandle = IFCInstanceExporter.CreateMaterialLayerSet(file, layers, layerSetName, layerSetDesc);
            }

            ExporterCacheManager.MaterialSetCache.RegisterLayerSet(typeElemId, m_GeneratedMaterialLayerSetHandle, this);
         }

         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(m_GeneratedPrimaryMaterialHandle))
            ExporterCacheManager.MaterialSetCache.RegisterPrimaryMaterialHnd(typeElemId, m_GeneratedPrimaryMaterialHandle);
      }

      private void UnregisterIFCHandles()
      {
         ElementId typeElemId = Element.GetTypeId();

         ExporterCacheManager.MaterialSetCache.UnregisterLayerSet(typeElemId);
         ExporterCacheManager.MaterialSetCache.UnregisterPrimaryMaterialHnd(typeElemId);
         if (ProductWrapper != null)
            ProductWrapper.ClearFinishMaterials();

         m_GeneratedMaterialLayerSetHandle = null;
         m_GeneratedPrimaryMaterialHandle = null;
         m_GeneratedLayerQuantityWidthHnd.Clear();
      }

      public enum CompareTwoLists
      {
         ListsSequentialEqual,
         ListsReversedEqual,
         ListsUnequal
      }

      public static CompareTwoLists CompareMaterialInfoList(IList<ElementId> materialListBody, IList<ElementId> materialIdsFromMaterialInfo)
      {
         if (materialListBody.Count != materialIdsFromMaterialInfo.Count)
            return CompareTwoLists.ListsUnequal;

         // Forward compare
         if (Enumerable.SequenceEqual(materialIdsFromMaterialInfo, materialListBody))
            return CompareTwoLists.ListsSequentialEqual;

         //Backward compare
         materialIdsFromMaterialInfo = materialIdsFromMaterialInfo.Reverse().ToList();
         if (Enumerable.SequenceEqual(materialIdsFromMaterialInfo, materialListBody))
            return CompareTwoLists.ListsReversedEqual;

         return CompareTwoLists.ListsUnequal;
      }
   }
}
