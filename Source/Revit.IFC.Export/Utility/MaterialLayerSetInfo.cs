using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
   public class MaterialLayerSetInfo
   {
      public  class MaterialInfo
      {
         public MaterialInfo(ElementId baseMatId, string layerName, double matWidth, MaterialFunctionAssignment function)
         {
            m_baseMatId = baseMatId;
            m_layerName = layerName;
            m_matWidth = matWidth;
            m_function = function;
         }
         public ElementId m_baseMatId;
         public string m_layerName;
         public double m_matWidth;
         public MaterialFunctionAssignment m_function;
      }
      ExporterIFC m_ExporterIFC;
      Element m_Element;
      ProductWrapper m_ProductWrapper;
      bool m_needToGenerateIFCObjects = false;

      /// <summary>
      /// Initialize MaterialLayerSetInfo for element
      /// </summary>
      /// <param name="exporterIFC">the exporter IFC</param>
      /// <param name="element">the element</param>
      /// <param name="productWrapper">the product wrapper</param>
      public MaterialLayerSetInfo(ExporterIFC exporterIFC, Element element, ProductWrapper productWrapper)
      {
         m_Element = element;
         m_ExporterIFC = exporterIFC;
         m_ProductWrapper = productWrapper;
         CollectMaterialLayerSet();
      }

      public void SingleMaterialOverride (ElementId materialId, double materialWidth)
      {
         GenerateIFCObjectsIfNeeded();

         Material material = m_Element.Document.GetElement(materialId) as Material;
         string layerName = "Layer";
         if (material != null)
         {
            layerName = NamingUtil.GetMaterialLayerName(material);
         }
         MaterialInfo matInfo = new MaterialInfo(materialId, layerName, materialWidth, MaterialFunctionAssignment.None);
         MaterialIds.Add(matInfo);

         IFCAnyHandle singleMaterialOverrideHnd = IFCInstanceExporter.CreateMaterial(m_ExporterIFC.GetFile(), layerName, null, null);
         ExporterCacheManager.MaterialHandleCache.Register(MaterialIds[0].m_baseMatId, singleMaterialOverrideHnd);
         m_MaterialLayerSetHandle = singleMaterialOverrideHnd;
      }

      /// <summary>
      /// Return status whether the LayerSetInfo is empty
      /// </summary>
      public bool IsEmpty
      {
         get
         {
            return (m_MaterialLayerSetHandle == null && MaterialIds.Count == 0);
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
      private IFCAnyHandle m_MaterialLayerSetHandle = null;
      
      public IFCAnyHandle MaterialLayerSetHandle
      {
         get
         {
            GenerateIFCObjectsIfNeeded();
            return m_MaterialLayerSetHandle;
         }
      }

      /// <summary>
      /// Primary Material handle
      /// Warning: Do not call Properties inside class because this can break lazy generation of IFC objects.
      ///          Use private members instead.
      /// </summary>
      private IFCAnyHandle m_PrimaryMaterialHandle = null;
      public IFCAnyHandle PrimaryMaterialHandle
      {
         get
         {
            GenerateIFCObjectsIfNeeded();

            return m_PrimaryMaterialHandle;
         }
      }

      /// <summary>
      /// Material layer quantity properties
      /// Warning: Do not call Properties inside class because this can break lazy generation of IFC objects.
      ///          Use private members instead.
      /// </summary>
      private HashSet<IFCAnyHandle> m_LayerQuantityWidthHnd = new HashSet<IFCAnyHandle>();
      public HashSet<IFCAnyHandle> LayerQuantityWidthHnd
      {
         get
         {
            GenerateIFCObjectsIfNeeded();

            return m_LayerQuantityWidthHnd;
         }
      }

      /// <summary>
      /// Collect information about material layer.
      ///   For IFC4RV Architectural exchange, it will generate IfcMatrialConstituentSet along with the relevant IfcShapeAspect and the width in the quantityset
      ///   For IFC4RV Structural exchange, it will generate multiple components as IfcBuildingElementPart for each layer
      ///   For others IfcMaterialLayer will be created
      /// </summary>
      private void CollectMaterialLayerSet()
      {
         ElementId typeElemId = m_Element.GetTypeId();
         IFCAnyHandle materialLayerSet = ExporterCacheManager.MaterialSetCache.FindLayerSet(typeElemId);
         // Roofs with no components are only allowed one material.  We will arbitrarily choose the thickest material.
         m_PrimaryMaterialHandle = ExporterCacheManager.MaterialSetCache.FindPrimaryMaterialHnd(typeElemId);

         bool materialHandleIsNotValid = IFCAnyHandleUtil.IsNullOrHasNoValue(materialLayerSet);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(materialLayerSet) || materialHandleIsNotValid)
         {
            if (materialHandleIsNotValid)
            {
               UnregisterIFCHandles();
            }
            m_needToGenerateIFCObjects = true;

            List<double> widths = new List<double>();
            List<MaterialFunctionAssignment> functions = new List<MaterialFunctionAssignment>();

            HostObjAttributes hostObjAttr = m_Element.Document.GetElement(typeElemId) as HostObjAttributes;
            if (hostObjAttr == null)
            {
               // It does not have the HostObjAttribute (where we will get the compound structure for material layer set.
               // We will define a single material instead and create the material layer set of this single material if there is enough information (At least Material id and thickness) 
               FamilyInstance familyInstance = m_Element as FamilyInstance;
               if (familyInstance == null)
                  return;

               FamilySymbol familySymbol = familyInstance.Symbol;
               ICollection<ElementId> famMatIds = familySymbol.GetMaterialIds(false);
               if (famMatIds.Count == 0)
               {
                  // For some reason Plate type may not return any Material id
                  ElementId baseMatId = CategoryUtil.GetBaseMaterialIdForElement(m_Element);
                  Material material = m_Element.Document.GetElement(baseMatId) as Material;
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
                     ElementId baseMatId = CategoryUtil.GetBaseMaterialIdForElement(m_Element);
                     if (matid != ElementId.InvalidElementId)
                     {
                        Material material = m_Element.Document.GetElement(matid) as Material;
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
               ElementId baseMatId = CategoryUtil.GetBaseMaterialIdForElement(m_Element);
               CompoundStructure cs = hostObjAttr.GetCompoundStructure();

               if (cs != null)
               {
                  double scaledOffset = 0.0, scaledWallWidth = 0.0, wallHeight = 0.0;
                  Wall wall = m_Element as Wall;
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
                        Material material = m_Element.Document.GetElement(matId) as Material;
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
                     Material material = m_Element.Document.GetElement(baseMatId) as Material;
                     if (material != null)
                     {
                        string layerName = NamingUtil.GetMaterialLayerName(material);
                        MaterialIds.Add(new MaterialInfo(baseMatId, layerName, matWidth, MaterialFunctionAssignment.None));
                     }
                  }
                  functions.Add(MaterialFunctionAssignment.None);
               }
            }
         }
         else
         {
            m_needToGenerateIFCObjects = false;

            m_MaterialLayerSetHandle = materialLayerSet;

            MaterialLayerSetInfo mlsInfo = ExporterCacheManager.MaterialSetCache.FindMaterialLayerSetInfo(typeElemId);
            if (mlsInfo != null)
            {
               MaterialIds = mlsInfo.MaterialIds;
               m_PrimaryMaterialHandle = mlsInfo.PrimaryMaterialHandle;
               m_LayerQuantityWidthHnd = mlsInfo.LayerQuantityWidthHnd;
            }
         }

         return;
      }

      private void GenerateIFCObjectsIfNeeded()
      {
         if (m_needToGenerateIFCObjects)
            m_needToGenerateIFCObjects = false;
         else
            return;

         IFCAnyHandle materialLayerSet = null;

         if (m_ProductWrapper != null && !m_ProductWrapper.ToNative().IsValidObject)
            m_ProductWrapper = null;

         m_ProductWrapper?.ClearFinishMaterials();

         // We can't create IfcMaterialLayers without creating an IfcMaterialLayerSet.  So we will simply collate here.
         IList<IFCAnyHandle> materialHnds = new List<IFCAnyHandle>();
         IList<int> widthIndices = new List<int>();
         double thickestLayer = 0.0;
         for (int ii = 0; ii < MaterialIds.Count; ++ii)
         {
            // Require positive width for IFC2x3 and before, and non-negative width for IFC4.
            if (MaterialIds[ii].m_matWidth < -MathUtil.Eps())
               continue;

            bool almostZeroWidth = MathUtil.IsAlmostZero(MaterialIds[ii].m_matWidth);
            if (!ExporterCacheManager.ExportOptionsCache.ExportAs4 && almostZeroWidth)
               continue;

            if (almostZeroWidth)
               MaterialIds[ii].m_matWidth = 0.0;

            IFCAnyHandle materialHnd = CategoryUtil.GetOrCreateMaterialHandle(m_ExporterIFC, MaterialIds[ii].m_baseMatId);
            if (m_PrimaryMaterialHandle == null || (MaterialIds[ii].m_matWidth > thickestLayer))
            {
               m_PrimaryMaterialHandle = materialHnd;
               thickestLayer = MaterialIds[ii].m_matWidth;
            }

            widthIndices.Add(ii);
            materialHnds.Add(materialHnd);

            if ((m_ProductWrapper != null) && (MaterialIds[ii].m_function == MaterialFunctionAssignment.Finish1 || MaterialIds[ii].m_function == MaterialFunctionAssignment.Finish2))
            {
               m_ProductWrapper.AddFinishMaterial(materialHnd);
            }
         }

         int numLayersToCreate = widthIndices.Count;
         if (numLayersToCreate == 0)
         {
            m_MaterialLayerSetHandle = materialLayerSet;
            return;
         }

         // If it is a single material, check single material override (only IfcMaterial without IfcMaterialLayerSet with only 1 member)
         if (numLayersToCreate == 1 && ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
         {
            m_MaterialLayerSetHandle = ExporterUtil.GetSingleMaterial(m_ExporterIFC, m_Element,
               MaterialIds[0].m_baseMatId) ?? materialHnds[0];
            return;
         }

         IFCFile file = m_ExporterIFC.GetFile();
         Document document = ExporterCacheManager.Document;

         IList<IFCAnyHandle> layers = new List<IFCAnyHandle>(numLayersToCreate);
         IList<Tuple<string, IFCAnyHandle>> layerWidthQuantities = new List<Tuple<string, IFCAnyHandle>>();
         HashSet<string> layerNameUsed = new HashSet<string>();
         double totalWidth = 0.0;

         for (int ii = 0; ii < numLayersToCreate; ii++)
         {
            int widthIndex = widthIndices[ii];
            double scaledWidth = UnitUtil.ScaleLength(MaterialIds[widthIndex].m_matWidth);

            string layerName = "Layer";
            string description = null;
            string category = null;
            int? priority = null;

            IFCLogical? isVentilated = null;
            int isVentilatedValue;

            Material material = document.GetElement(MaterialIds[ii].m_baseMatId) as Material;
            if (material != null)
            {
               if (ParameterUtil.GetIntValueFromElement(material, "IfcMaterialLayer.IsVentilated", out isVentilatedValue) != null)
               {
                  if (isVentilatedValue == 0)
                     isVentilated = IFCLogical.False;
                  else if (isVentilatedValue == 1)
                     isVentilated = IFCLogical.True;
               }

               if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
               {
                  layerName = MaterialIds[ii].m_layerName;
                  if (string.IsNullOrEmpty(layerName))
                     layerName = "Layer";

                  // Ensure layer name is unique
                  layerName = NamingUtil.GetUniqueNameWithinSet(layerName, ref layerNameUsed);

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

         ElementId typeElemId = m_Element.GetTypeId();
         if (layers.Count > 0)
         {
            Element type = document.GetElement(typeElemId);
            string layerSetName = NamingUtil.GetOverrideStringValue(type, "IfcMaterialLayerSet.Name", m_ExporterIFC.GetFamilyName());
            string layerSetDesc = NamingUtil.GetOverrideStringValue(type, "IfcMaterialLayerSet.Description", null);

            if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
            {
               HashSet<IFCAnyHandle> constituents = new HashSet<IFCAnyHandle>(layers);
               m_MaterialLayerSetHandle = IFCInstanceExporter.CreateMaterialConstituentSet(file, constituents, name: layerSetName, description: layerSetDesc);
               foreach (Tuple<string, IFCAnyHandle> layerWidthQty in layerWidthQuantities)
               {
                  m_LayerQuantityWidthHnd.Add(IFCInstanceExporter.CreatePhysicalComplexQuantity(file, layerWidthQty.Item1, null,
                     new HashSet<IFCAnyHandle>() { layerWidthQty.Item2 }, "Layer", null, null));
               }
               // Finally create the total width as a quantity
               m_LayerQuantityWidthHnd.Add(IFCInstanceExporter.CreateQuantityLength(file, "Width", null, null, totalWidth));
            }
            else
            {
               m_MaterialLayerSetHandle = IFCInstanceExporter.CreateMaterialLayerSet(file, layers, layerSetName, layerSetDesc);
            }

            ExporterCacheManager.MaterialSetCache.RegisterLayerSet(typeElemId, m_MaterialLayerSetHandle, this);
         }

         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(m_PrimaryMaterialHandle))
            ExporterCacheManager.MaterialSetCache.RegisterPrimaryMaterialHnd(typeElemId, m_PrimaryMaterialHandle);
      }

      private void UnregisterIFCHandles()
      {
         ElementId typeElemId = m_Element.GetTypeId();

         ExporterCacheManager.MaterialSetCache.UnregisterLayerSet(typeElemId);
         ExporterCacheManager.MaterialSetCache.UnregisterPrimaryMaterialHnd(typeElemId);
         if (m_ProductWrapper != null)
            m_ProductWrapper.ClearFinishMaterials();

         m_MaterialLayerSetHandle = null;
         m_PrimaryMaterialHandle = null;
         m_LayerQuantityWidthHnd.Clear();
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
