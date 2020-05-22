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
      ExporterIFC m_ExporterIFC;
      Element m_Element;
      ProductWrapper m_ProductWrapper;

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
         Material material = m_Element.Document.GetElement(materialId) as Material;
         string layerName = "Layer";
         if (material != null)
         {
            layerName = NamingUtil.GetOverrideStringValue(material, "IfcMaterialLayer.Name", material.Name);
         }
         Tuple<ElementId, string, double> matInfo = new Tuple<ElementId, string, double>(materialId, layerName, materialWidth);
         MaterialIds.Add(matInfo);

         IFCAnyHandle singleMaterialOverrideHnd = IFCInstanceExporter.CreateMaterial(m_ExporterIFC.GetFile(), layerName, null, null);
         ExporterCacheManager.MaterialHandleCache.Register(MaterialIds[0].Item1, singleMaterialOverrideHnd);
         MaterialLayerSetHandle = singleMaterialOverrideHnd;
      }

      /// <summary>
      /// Return status whether the LayerSetInfo is empty
      /// </summary>
      public bool IsEmpty
      {
         get
         {
            return (MaterialLayerSetHandle == null && MaterialIds.Count == 0);
         }
      }

      /// <summary>
      /// MaterialLayerSet Collected or MaterialConstituentSet (for IFC4RV)
      /// </summary>
      public IFCAnyHandle MaterialLayerSetHandle { get; private set; } = null;

      /// <summary>
      /// List of Material Ids and their associated layer names of the material layer set
      /// </summary>
      public List<Tuple<ElementId, string, double>> MaterialIds { get; private set; } = new List<Tuple<ElementId, string, double>>();

      /// <summary>
      /// Primary Material handle
      /// </summary>
      public IFCAnyHandle PrimaryMaterialHandle { get; private set; } = null;

      /// <summary>
      /// Material layer quantity properties
      /// </summary>
      public HashSet<IFCAnyHandle> LayerQuantityWidthHnd { get; private set; } = new HashSet<IFCAnyHandle>();

      /// <summary>
      /// Collect information about material layer.
      ///   For IFC4RV Architectural exchange, it will generate IfcMatrialConstituentSet along with the relevant IfcShapeAspect and the width in the quantityset
      ///   For IFC4RV Structural exchange, it will generate multiple components as IfcBuildingElementPart for each layer
      ///   For others IfcMaterialLayer will be created
      /// </summary>
      private void CollectMaterialLayerSet()
      {
         ElementId typeElemId = m_Element.GetTypeId();
         MaterialIds = new List<Tuple<ElementId, string, double>>();
         IFCAnyHandle materialLayerSet = ExporterCacheManager.MaterialSetCache.FindLayerSet(typeElemId);
         // Roofs with no components are only allowed one material.  We will arbitrarily choose the thickest material.
         PrimaryMaterialHandle = ExporterCacheManager.MaterialSetCache.FindPrimaryMaterialHnd(typeElemId);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(materialLayerSet))
         {
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

                  string layerName = NamingUtil.GetOverrideStringValue(material, "IfcMaterialLayer.Name", material.Name);
                  double matWidth = 0.0;
                  Parameter thicknessPar = familySymbol.get_Parameter(BuiltInParameter.CURTAIN_WALL_SYSPANEL_THICKNESS);
                  if (thicknessPar != null)
                     matWidth = thicknessPar.AsDouble();
                  widths.Add(matWidth);

                  if (baseMatId != ElementId.InvalidElementId)
                     MaterialIds.Add(new Tuple<ElementId,string,double>(baseMatId, layerName, matWidth));
                  // How to get the thickness? For CurtainWall Panel (PanelType), there is a builtin parameter CURTAINWALL_SYSPANEL_THICKNESS

                  functions.Add(MaterialFunctionAssignment.None);
               }
               else
               {
                  foreach (ElementId matid in famMatIds)
                  {
                     // How to get the thickness? For CurtainWall Panel (PanelType), there is a builtin parameter CURTAINWALL_SYSPANEL_THICKNESS
                     Parameter thicknessPar = familySymbol.get_Parameter(BuiltInParameter.CURTAIN_WALL_SYSPANEL_THICKNESS);
                     double matWidth = 0.0;
                     if (thicknessPar == null)
                     {
                        matWidth = ParameterUtil.GetSpecialThicknessParameter(familySymbol);
                     }
                     else
                        matWidth = thicknessPar.AsDouble();

                     if (MathUtil.IsAlmostZero(matWidth))
                        continue;

                     widths.Add(matWidth);
                     ElementId baseMatId = CategoryUtil.GetBaseMaterialIdForElement(m_Element);
                     if (matid != ElementId.InvalidElementId)
                     {
                        Material material = m_Element.Document.GetElement(matid) as Material;
                        if (material != null)
                        {
                           string layerName = NamingUtil.GetOverrideStringValue(material, "IfcMaterialLayer.Name", material.Name);
                           MaterialIds.Add(new Tuple<ElementId, string, double>(matid, layerName, matWidth));
                        }
                     }
                     else
                     {
                        MaterialIds.Add(new Tuple<ElementId, string, double>(baseMatId, null, matWidth));
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
                     //if (MathUtil.IsAlmostZero(matWidth))
                     //   continue;

                     ElementId matId = cs.GetMaterialId(ii);
                     if (matId != ElementId.InvalidElementId)
                     {
                        Material material = m_Element.Document.GetElement(matId) as Material;
                        if (material != null)
                        {
                           string layerName = NamingUtil.GetOverrideStringValue(material, "IfcMaterialLayer.Name", material.Name);
                           MaterialIds.Add(new Tuple<ElementId, string, double>(matId, layerName, matWidth));
                        }
                     }
                     else
                     {
                        MaterialIds.Add(new Tuple<ElementId, string,double>(baseMatId, null, matWidth));
                     }
                     widths.Add(matWidth);
                     // save layer function into ProductWrapper, 
                     // it's used while exporting "Function" of Pset_CoveringCommon
                     functions.Add(cs.GetLayerFunction(ii));
                  }
               }

               if (MaterialIds.Count == 0)
               {
                  double matWidth = cs != null ? cs.GetWidth() : 0.0;
                  widths.Add(matWidth);
                  if (baseMatId != ElementId.InvalidElementId)
                  {
                     Material material = m_Element.Document.GetElement(baseMatId) as Material;
                     if (material != null)
                     {
                        string layerName = NamingUtil.GetOverrideStringValue(material, "IfcMaterialLayer.Name", material.Name);
                        MaterialIds.Add(new Tuple<ElementId, string, double>(baseMatId, layerName, matWidth));
                     }
                  }
                  functions.Add(MaterialFunctionAssignment.None);
               }
            }

            if (m_ProductWrapper != null)
               m_ProductWrapper.ClearFinishMaterials();

            // We can't create IfcMaterialLayers without creating an IfcMaterialLayerSet.  So we will simply collate here.
            IList<IFCAnyHandle> materialHnds = new List<IFCAnyHandle>();
            IList<int> widthIndices = new List<int>();
            double thickestLayer = 0.0;
            for (int ii = 0; ii < MaterialIds.Count; ++ii)
            {
               // Require positive width for IFC2x3 and before, and non-negative width for IFC4.
               if (widths[ii] < -MathUtil.Eps())
                  continue;

               bool almostZeroWidth = MathUtil.IsAlmostZero(widths[ii]);
               if (!ExporterCacheManager.ExportOptionsCache.ExportAs4 && almostZeroWidth)
                  continue;

               if (almostZeroWidth)
                  widths[ii] = 0.0;

               IFCAnyHandle materialHnd = CategoryUtil.GetOrCreateMaterialHandle(m_ExporterIFC, MaterialIds[ii].Item1);
               if (PrimaryMaterialHandle == null || (widths[ii] > thickestLayer))
               {
                  PrimaryMaterialHandle = materialHnd;
                  thickestLayer = widths[ii];
               }

               widthIndices.Add(ii);
               materialHnds.Add(materialHnd);

               if ((m_ProductWrapper != null) && (functions[ii] == MaterialFunctionAssignment.Finish1 || functions[ii] == MaterialFunctionAssignment.Finish2))
               {
                  m_ProductWrapper.AddFinishMaterial(materialHnd);
               }
            }

            int numLayersToCreate = widthIndices.Count;
            if (numLayersToCreate == 0)
            {
               MaterialLayerSetHandle = materialLayerSet;
               return;
            }

            // If it is a single material, check single material override (only IfcMaterial without IfcMaterialLayerSet with only 1 member)
            if (numLayersToCreate == 1 && ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
            {
               string paramValue;
               ParameterUtil.GetStringValueFromElementOrSymbol(m_Element, "IfcSingleMaterialOverride", out paramValue);
               if (!string.IsNullOrEmpty(paramValue))
               {
                  IFCAnyHandle singleMaterialOverrideHnd = IFCInstanceExporter.CreateMaterial(m_ExporterIFC.GetFile(), paramValue, null, null);
                  ExporterCacheManager.MaterialHandleCache.Register(MaterialIds[0].Item1, singleMaterialOverrideHnd);
                  MaterialLayerSetHandle = singleMaterialOverrideHnd;
                  return;
               }
            }

            IFCFile file = m_ExporterIFC.GetFile();
            Document document = ExporterCacheManager.Document;

            IList<IFCAnyHandle> layers = new List<IFCAnyHandle>(numLayersToCreate);
            IList<Tuple<string,IFCAnyHandle>> layerWidthQuantities = new List<Tuple<string,IFCAnyHandle>>();
            HashSet<string> layerNameUsed = new HashSet<string>();
            double totalWidth = 0.0;

            for (int ii = 0; ii < numLayersToCreate; ii++)
            {
               int widthIndex = widthIndices[ii];
               double scaledWidth = UnitUtil.ScaleLength(widths[widthIndex]);

               string layerName = "Layer";
               string description = null;
               string category = null;
               int? priority = null;

               IFCLogical? isVentilated = null;
               int isVentilatedValue;

               Material material = document.GetElement(MaterialIds[ii].Item1) as Material;
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
                     layerName = MaterialIds[ii].Item2;
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
                  layerWidthQuantities.Add(new Tuple<string,IFCAnyHandle>(layerName,layerQtyHnd));
               }
               else
               {
                  IFCAnyHandle materialLayer = IFCInstanceExporter.CreateMaterialLayer(file, materialHnds[ii], scaledWidth, isVentilated,
                     name: layerName, description: description, category: category, priority: priority);
                  layers.Add(materialLayer);
               }
            }

            if (layers.Count > 0)
            {
               Element type = document.GetElement(typeElemId);
               string layerSetName = NamingUtil.GetOverrideStringValue(type, "IfcMaterialLayerSet.Name", m_ExporterIFC.GetFamilyName());
               string layerSetDesc = NamingUtil.GetOverrideStringValue(type, "IfcMaterialLayerSet.Description", null);

               if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
               {
                  HashSet<IFCAnyHandle> constituents = new HashSet<IFCAnyHandle>(layers);
                  MaterialLayerSetHandle = IFCInstanceExporter.CreateMaterialConstituentSet(file, constituents, name: layerSetName, description: layerSetDesc);
                  foreach (Tuple<string, IFCAnyHandle> layerWidthQty in layerWidthQuantities)
                  {
                     LayerQuantityWidthHnd.Add(IFCInstanceExporter.CreatePhysicalComplexQuantity(file, layerWidthQty.Item1, null, 
                        new HashSet<IFCAnyHandle>() { layerWidthQty.Item2 }, "Layer", null, null));
                  }
                  // Finally create the total width as a quantity
                  LayerQuantityWidthHnd.Add(IFCInstanceExporter.CreateQuantityLength(file, "Width", null, null, totalWidth));
               }
               else
               {
                  MaterialLayerSetHandle = IFCInstanceExporter.CreateMaterialLayerSet(file, layers, layerSetName, layerSetDesc);
               }

               ExporterCacheManager.MaterialSetCache.RegisterLayerSet(typeElemId, MaterialLayerSetHandle, this);
            }

            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(PrimaryMaterialHandle))
               ExporterCacheManager.MaterialSetCache.RegisterPrimaryMaterialHnd(typeElemId, PrimaryMaterialHandle);
         }
         else
         {
            MaterialLayerSetHandle = materialLayerSet;

            MaterialLayerSetInfo mlsInfo = ExporterCacheManager.MaterialSetCache.FindMaterialLayerSetInfo(typeElemId);
            if (mlsInfo != null)
            {
               MaterialIds = mlsInfo.MaterialIds;
               PrimaryMaterialHandle = mlsInfo.PrimaryMaterialHandle;
               LayerQuantityWidthHnd = mlsInfo.LayerQuantityWidthHnd;
            }
         }

         return;
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
