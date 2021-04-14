﻿//
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
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Provides static methods for category related manipulations.
   /// </summary>
   public class CategoryUtil
   {
      /// <summary>
      /// Gets category id of an element.
      /// </summary>
      /// <remarks>
      /// Returns InvalidElementId when argument is null.
      /// </remarks>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <returns>
      /// The category id.
      /// </returns>
      public static ElementId GetSafeCategoryId(Element element)
      {
         if (element == null)
            return ElementId.InvalidElementId;
         return element.Category.Id;
      }

      /// <summary>
      /// Gets category name of an element.
      /// </summary>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <returns>
      /// The category name.
      /// </returns>
      public static String GetCategoryName(Element element)
      {
         Category category = element.Category;

         if (category == null)
         {
            throw new Exception("Unable to obtain category for element id " + element.Id.IntegerValue);
         }
         return category.Name;
      }

      /// <summary>
      /// Gets material id of the category of an element.
      /// </summary>
      /// <remarks>
      /// Returns the material id of the parent category when the category of the element has no material.
      /// </remarks>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <returns>
      /// The material id.
      /// </returns>
      public static ElementId GetBaseMaterialIdForElement(Element element)
      {
         ElementId baseMaterialId = ElementId.InvalidElementId;
         Category category = element.Category;
         if (category != null)
         {
            Material baseMaterial = category.Material;
            if (baseMaterial != null)
               baseMaterialId = baseMaterial.Id;
            else
            {
               category = category.Parent;
               if (category != null)
               {
                  baseMaterial = category.Material;
                  if (baseMaterial != null)
                     baseMaterialId = baseMaterial.Id;
               }
            }
         }
         return baseMaterialId;
      }

      /// <summary>
      /// Returns the original color if it is valid, or a default color (grey) if it isn't.
      /// </summary>
      /// <param name="originalColor">The original color.</param>
      /// <returns>The original color if it is valid, or a default color (grey) if it isn't.</returns>
      public static Color GetSafeColor(Color originalColor)
      {
         if (originalColor.IsValid)
            return originalColor;

         // Default color is grey.
         return new Color(0x7f, 0x7f, 0x7f);
      }

      /// <summary>
      /// Gets the color of the material of the category of an element.
      /// </summary>
      /// <remarks>
      /// Returns the line color of the category when the category of the element has no material.
      /// </remarks>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <returns>
      /// The color of the element.
      /// </returns>
      public static Autodesk.Revit.DB.Color GetElementColor(Element element)
      {
         Category category = element.Category;

         if (category == null)
         {
            throw new Exception("Unable to obtain category for element id " + element.Id.IntegerValue);
         }

         Material material = category.Material;

         if (material != null)
         {
            return GetSafeColor(material.Color);
         }
         else
         {
            Color color = GetSafeColor(category.LineColor);

            // Grey is returned in place of pure black.  For systems which default to a black background color, 
            // Grey is more of a contrast.  
            if (color.Red == 0 && color.Green == 0 && color.Blue == 0)
               color = new Color(0x7f, 0x7f, 0x7f);

            return color;
         }
      }

      /// <summary>
      /// Get the element color and opacity from the material associated to an element's category, if any.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="opacity">The return opacity value.</param>
      /// <returns>The material color.</returns>
      public static Color GetElementColorAndOpacityFromCategory(Element element, out double opacity)
      {
         opacity = 1.0;
         Category category = element.Category;
         if (category != null)
         {
            Material matElem = element.Category.Material;
            if (matElem != null)
            {
               opacity = (double)(100 - matElem.Transparency) / 100;
            }
         }
         return GetElementColor(element);
      }

      private static bool CacheIsElementExternal(ElementId elementId, bool isExternal)
      {
         ExporterCacheManager.IsExternalParameterValueCache[elementId] = isExternal;
         return isExternal;
      }

      private static bool? IsElementExternalViaParameter(Element element)
      {
         int? intIsExternal = null;
         string localExternalParamName = PropertySetEntryUtil.GetLocalizedIsExternal(ExporterCacheManager.LanguageType);
         if (localExternalParamName != null)
         {
            intIsExternal = ParameterUtil.GetIntValueFromElementOrSymbol(element, localExternalParamName);
            if (intIsExternal.HasValue)
            {
               return (intIsExternal.Value != 0);
            }
         }

         if (ExporterCacheManager.LanguageType != LanguageType.English_USA)
         {
            string externalParamName = PropertySetEntryUtil.GetLocalizedIsExternal(LanguageType.English_USA);
            intIsExternal = ParameterUtil.GetIntValueFromElementOrSymbol(element, externalParamName);
            if (intIsExternal.HasValue)
            {
               return (intIsExternal.Value != 0);
            }
         }

         return null;
      }

      /// <summary>
      /// Checks if element is external.
      /// </summary>
      /// <remarks>
      /// An element is considered external if either:
      ///   <li> A special Yes/No parameter "IsExternal" is applied to it or its type and its value is set to "yes".</li>
      ///   <li> The element itself has information about being an external element.</li>
      /// All other elements are internal.
      /// </remarks>
      /// <param name="element">The element.</param>
      /// <returns>True if the element is external, false otherwise.</returns>
      public static bool IsElementExternal(Element element)
      {
         if (element == null)
            return false;

         // Look for a parameter "IsExternal", potentially localized.
         ElementId elementId = element.Id;

         bool isExternal;
         if (ExporterCacheManager.IsExternalParameterValueCache.TryGetValue(elementId, out isExternal))
         {
            return isExternal;
         }

         bool? maybeIsExternal = IsElementExternalViaParameter(element);
         if (maybeIsExternal.HasValue)
         {
            return CacheIsElementExternal(elementId, maybeIsExternal.Value);
         }
         
         ElementId elementTypeId = element.GetTypeId();
         Element elementType = null;
         if (elementTypeId != ElementId.InvalidElementId)
         {
            if (ExporterCacheManager.IsExternalParameterValueCache.TryGetValue(elementTypeId, out isExternal))
            {
               return isExternal;
            }

            elementType = element.Document.GetElement(element.GetTypeId());
         }
         else if (element is ElementType)
         {
            elementType = element;
         }

         // Many element types have the FUNCTION_PARAM parameter.  If this is set, use its value.
         int elementFunction;
         if ((elementType != null) && ParameterUtil.GetIntValueFromElement(elementType, BuiltInParameter.FUNCTION_PARAM, out elementFunction) != null)
         {
            // Note that the WallFunction enum value is the same for many different kinds of objects.
            // Note: it is unclear whether Soffit walls should be considered exterior, but won't change
            // existing functionality for now.
            isExternal = (elementFunction != ((int)WallFunction.Interior));
            if (elementId != elementTypeId && elementTypeId != ElementId.InvalidElementId)
               ExporterCacheManager.IsExternalParameterValueCache[elementTypeId] = isExternal;
            return CacheIsElementExternal(elementId, isExternal);
         }

         // Specific element types that know if they are external or not if the built-in parameter isn't set.
         // Categories are used, and not types, to also support in-place families 

         // Roofs are always external
         ElementId categoryId = element.Category.Id;
         if (categoryId == new ElementId(BuiltInCategory.OST_Roofs) ||
             categoryId == new ElementId(BuiltInCategory.OST_MassExteriorWall))
         {
            return CacheIsElementExternal(elementId, true);
         }

         // Mass interior walls are always internal
         if (categoryId == new ElementId(BuiltInCategory.OST_MassInteriorWall))
         {
            return CacheIsElementExternal(elementId, false);
         }

         // Family instances may be hosted on an external element
         if (element is FamilyInstance)
         {
            FamilyInstance familyInstance = element as FamilyInstance;
            Element familyInstanceHost = familyInstance.Host;
            if (familyInstanceHost == null)
            {
               Reference familyInstanceHostReference = familyInstance.HostFace;
               if (familyInstanceHostReference != null)
                  familyInstanceHost = element.Document.GetElement(familyInstanceHostReference);
            }

            if (familyInstanceHost != null)
               return IsElementExternal(familyInstanceHost);
         }

         return CacheIsElementExternal(elementId, false);
      }

      /// <summary>
      /// Create material association with inputs of 2 IFCAnyHandle. It is used for example to create material relation between an instance and its material set usage
      /// </summary>
      /// <param name="exporterIFC">the exporter IFC</param>
      /// <param name="instanceHnd">the instance handle</param>
      /// <param name="materialSetHnd">the material usage handle, e.g. material set usage</param>
      public static void CreateMaterialAssociation(ExporterIFC exporterIFC, IFCAnyHandle instanceHnd, IFCAnyHandle materialSetHnd)
      {
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(materialSetHnd) && !IFCAnyHandleUtil.IsNullOrHasNoValue(instanceHnd))
         {
            ExporterCacheManager.MaterialRelationsCache.Add(materialSetHnd, instanceHnd);
         }
      }

      /// <summary>
      /// Create or add into exsiting relation for a material set from a family symbol.
      /// </summary>
      /// <param name="exporterIFC">the exporter IFC</param>
      /// <param name="familySymbol">the element family symbol</param>
      /// <param name="typeStyle">the IFC type object created for the above family symbol</param>
      /// <param name="materialAndProfile">a dictionary that keeps the association of the material elementid and the associated profile def</param>
      public static void CreateMaterialAssociation(ExporterIFC exporterIFC, FamilySymbol familySymbol, IFCAnyHandle typeStyle, MaterialAndProfile materialAndProfile)
      {
         ElementId typeId = familySymbol.Id;
         IFCAnyHandle materialSet = GetOrCreateMaterialSet(exporterIFC, familySymbol, materialAndProfile);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(materialSet))
            ExporterCacheManager.MaterialRelationsCache.Add(materialSet, typeStyle);
      }

      /// <summary>
      /// Creates an association between a material handle and an instance handle.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="instanceHandle">The IFC instance handle.</param>
      /// <param name="materialId">The material id.</param>
      public static void CreateMaterialAssociation(ExporterIFC exporterIFC, IFCAnyHandle instanceHandle, ElementId materialId)
      {
         // Create material association if any.
         if (materialId != ElementId.InvalidElementId)
         {
            IFCAnyHandle materialNameHandle = GetOrCreateMaterialHandle(exporterIFC, materialId);

            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(materialNameHandle))
               ExporterCacheManager.MaterialRelationsCache.Add(materialNameHandle, instanceHandle);
         }
      }

      /// <summary>
      /// Creates an association between a list of material handles and an instance handle.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="instanceHandle">The IFC instance handle.</param>
      /// <param name="materialId">The list of material ids.</param>
      public static void CreateMaterialAssociation(ExporterIFC exporterIFC, IFCAnyHandle instanceHandle, ICollection<ElementId> materialList)
      {
         Document document = ExporterCacheManager.Document;

         // Create material association if any.
         bool createConstituentSet = (ExporterCacheManager.ExportOptionsCache.ExportAs4);
         HashSet<IFCAnyHandle> materials = createConstituentSet ? null : new HashSet<IFCAnyHandle>();
         ISet<ElementId> alreadySeenIds = createConstituentSet ? new HashSet<ElementId>() : null;

         IFCAnyHandle matHnd = null;
         foreach (ElementId materialId in materialList)
         {
            matHnd = GetOrCreateMaterialHandle(exporterIFC, materialId);
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(matHnd))
               continue;

            // Strictly speaking, we only need at most one material if createConstituentSet is true.
            if (createConstituentSet)
               alreadySeenIds.Add(materialId);
            else
               materials.Add(matHnd);
         }

         int numMaterials = createConstituentSet ? alreadySeenIds.Count : materials.Count;
         if (numMaterials == 0)
            return;

         // If there is only one material, we will associate the one material directly.
         // matHnd above is guaranteed to have a valid value if numMaterials > 0.
         if (numMaterials == 1)
         {
            ExporterCacheManager.MaterialRelationsCache.Add(matHnd, instanceHandle);
            return;
         }

         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle materialContainerHnd = null;
         
         if (createConstituentSet)
         {
            ExporterCacheManager.MaterialConstituentCache.Reset();
            HashSet<IFCAnyHandle> constituentSet = new HashSet<IFCAnyHandle>();
            // in IFC4 we will create IfcConstituentSet instead of MaterialList, create the associated IfcConstituent here from IfcMaterial
            foreach (ElementId materialId in alreadySeenIds)
            {
               IFCAnyHandle constituentHnd = GetOrCreateMaterialConstituent(exporterIFC, materialId);
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(constituentHnd))
                  constituentSet.Add(constituentHnd);
            }

            materialContainerHnd = GetOrCreateMaterialConstituentSet(file, instanceHandle, constituentSet);
         }
         else
         {
            materialContainerHnd = IFCInstanceExporter.CreateMaterialList(file, materials.ToList());
         }

         ExporterCacheManager.MaterialRelationsCache.Add(materialContainerHnd, instanceHandle);
      }

      /// <summary>
      /// Creates an association between a list of material handles and an instance handle, and create the relevant IfcShapeAspect
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element</param>
      /// <param name="instanceHandle">The IFC instance handle.</param>
      /// <param name="representationItemInfoSet">RepresentationItem info set</param>
      public static void CreateMaterialAssociationWithShapeAspect(ExporterIFC exporterIFC, Element element, IFCAnyHandle instanceHandle, HashSet<Tuple<MaterialConstituentInfo, IFCAnyHandle>> representationItemInfoSet)
      {
         Document document = ExporterCacheManager.Document;

         // Create material association if any.
         bool createConstituentSet = (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4);
         IList<IFCAnyHandle> materials = createConstituentSet ? null : new List<IFCAnyHandle>();
         ISet<ElementId> alreadySeenMaterialIds = createConstituentSet ? new HashSet<ElementId>() : null;
         IFCAnyHandle matHnd = null;
         foreach (Tuple<MaterialConstituentInfo, IFCAnyHandle> repItemInfo in representationItemInfoSet)
         {
            matHnd = GetOrCreateMaterialHandle(exporterIFC, repItemInfo.Item1.MaterialId);
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(matHnd))
               continue;

            // Strictly speaking, we only need at most one material if createConstituentSet is true.
            if (createConstituentSet)
               alreadySeenMaterialIds.Add(repItemInfo.Item1.MaterialId);
            else
               materials.Add(matHnd);
         }

         int numMaterials = createConstituentSet ? alreadySeenMaterialIds.Count : materials.Count;
         if (numMaterials == 0)
            return;

         // If there is only one material, we will associate the one material directly.
         // matHnd above is guaranteed to have a valid value if numMaterials > 0.
         if (numMaterials == 1)
         {
            ExporterCacheManager.MaterialRelationsCache.Add(matHnd, instanceHandle);
            return;
         }

         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle materialContainerHnd = null;

         if (createConstituentSet)
         {
            ExporterCacheManager.MaterialConstituentCache.Reset();
            IDictionary<IFCAnyHandle, IFCAnyHandle> mapRepItemToItemDict = new Dictionary<IFCAnyHandle, IFCAnyHandle>();
            string repType = null;
            IFCAnyHandle prodRep = null;
            if (IFCAnyHandleUtil.IsSubTypeOf(instanceHandle, IFCEntityType.IfcProduct))
            {
               prodRep = IFCAnyHandleUtil.GetRepresentation(instanceHandle);
               IList<IFCAnyHandle> reps = IFCAnyHandleUtil.GetRepresentations(prodRep);
               // Get RepresentationType for shapeAspect in "Body" representation
               foreach (IFCAnyHandle rep in reps)
               {
                  if (IFCAnyHandleUtil.GetRepresentationIdentifier(rep).Equals("Body"))
                  {
                     repType = IFCAnyHandleUtil.GetRepresentationType(rep);
                     if (repType.Equals("MappedRepresentation", StringComparison.InvariantCultureIgnoreCase))
                     {
                        HashSet<IFCAnyHandle> items = IFCAnyHandleUtil.GetItems(rep);
                        foreach (IFCAnyHandle item in items)
                        {
                           IFCAnyHandle mappingSource = IFCAnyHandleUtil.GetInstanceAttribute(item, "MappingSource");
                           IFCAnyHandle mappingSourceRep = IFCAnyHandleUtil.GetInstanceAttribute(mappingSource, "MappedRepresentation");
                           repType = IFCAnyHandleUtil.GetRepresentationType(mappingSourceRep);
                        }
                     }
                     break;
                  }
               }
            }
            else if (IFCAnyHandleUtil.IsSubTypeOf(instanceHandle, IFCEntityType.IfcTypeProduct))
            {
               IList<IFCAnyHandle> repMaps = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(instanceHandle, "RepresentationMaps");
               if (repMaps != null && repMaps.Count > 0)
               {
                  // Will use representation maps for shapeAspect if there is "Body"
                  foreach (IFCAnyHandle repMap in repMaps)
                  {
                     IFCAnyHandle rep = IFCAnyHandleUtil.GetInstanceAttribute(repMap, "MappedRepresentation");
                     if (IFCAnyHandleUtil.GetRepresentationIdentifier(rep).Equals("Body"))
                     {
                        prodRep = repMap;
                        repType = IFCAnyHandleUtil.GetRepresentationType(rep);
                        break;
                     }
                  }
               }
            }

            // Collect ALL representationItems that have the same Category and MaterialId into one Set
            MaterialConsituentInfoComparer comparer = new MaterialConsituentInfoComparer();
            IDictionary<MaterialConstituentInfo, HashSet<IFCAnyHandle>> repItemInfoGroup = new Dictionary<MaterialConstituentInfo, HashSet<IFCAnyHandle>>(comparer);
            foreach (Tuple<MaterialConstituentInfo, IFCAnyHandle> repItemInfo in representationItemInfoSet)
            {
               if (!repItemInfoGroup.ContainsKey(repItemInfo.Item1))
               {
                  HashSet<IFCAnyHandle> repItemSet = new HashSet<IFCAnyHandle>() { repItemInfo.Item2 };
                  repItemInfoGroup.Add(repItemInfo.Item1, repItemSet);
               }
               else
               {
                  repItemInfoGroup[repItemInfo.Item1].Add(repItemInfo.Item2);
               }
            }

            HashSet<IFCAnyHandle> constituentSet = new HashSet<IFCAnyHandle>();
            // in IFC4 we will create IfcConstituentSet instead of MaterialList, create the associated IfcConstituent here from IfcMaterial
            foreach (KeyValuePair<MaterialConstituentInfo, HashSet<IFCAnyHandle>> repItemInfoSet in repItemInfoGroup)
            {
               IFCAnyHandle constituentHnd = GetOrCreateMaterialConstituent(exporterIFC, repItemInfoSet.Key);
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(constituentHnd))
                  constituentSet.Add(constituentHnd);

               RepresentationUtil.CreateRepForShapeAspect(exporterIFC, element, prodRep, repType, repItemInfoSet.Key.ComponentCat, repItemInfoSet.Value);
            }

            if (constituentSet.Count > 0)
            {
               materialContainerHnd = GetOrCreateMaterialConstituentSet(file, instanceHandle, constituentSet);
               ExporterCacheManager.MaterialRelationsCache.Add(materialContainerHnd, instanceHandle);
            }
         }
         else
         {
            materialContainerHnd = IFCInstanceExporter.CreateMaterialList(file, materials);
            ExporterCacheManager.MaterialRelationsCache.Add(materialContainerHnd, instanceHandle);
         }
      }

      public static IFCAnyHandle GetOrCreateMaterialStyle(Document document, IFCFile file, ElementId materialId)
      {
         IFCAnyHandle styleHnd = ExporterCacheManager.MaterialIdToStyleHandleCache.Find(materialId);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(styleHnd))
         {
            Material material = document.GetElement(materialId) as Material;
            if (material == null)
               return null;

            string matName = NamingUtil.GetMaterialName(material);

            Color color = GetSafeColor(material.Color);
            double blueVal = color.Blue / 255.0;
            double greenVal = color.Green / 255.0;
            double redVal = color.Red / 255.0;

            IFCAnyHandle colorHnd = IFCInstanceExporter.CreateColourRgb(file, null, redVal, greenVal, blueVal);

            double transparency = ((double)material.Transparency) / 100.0;
            IFCData smoothness = IFCDataUtil.CreateAsNormalisedRatioMeasure(((double)material.Smoothness) / 100.0);

            IFCData specularExp = IFCDataUtil.CreateAsSpecularExponent(material.Shininess);

            IFCReflectanceMethod method = IFCReflectanceMethod.NotDefined;

            IFCAnyHandle renderingHnd = IFCInstanceExporter.CreateSurfaceStyleRendering(file, colorHnd, transparency,
                null, null, null, null, smoothness, specularExp, method);

            ISet<IFCAnyHandle> surfStyles = new HashSet<IFCAnyHandle>();
            surfStyles.Add(renderingHnd);

            IFCSurfaceSide surfSide = IFCSurfaceSide.Both;
            styleHnd = IFCInstanceExporter.CreateSurfaceStyle(file, matName, surfSide, surfStyles);
            ExporterCacheManager.MaterialIdToStyleHandleCache.Register(materialId, styleHnd);
         }

         return styleHnd;
      }

      /// <summary>
      /// Get or Create (if not yet exists) a handle for IfcMaterialConstituent. It points to an IfcMaterial
      /// </summary>
      /// <param name="exporterIFC"></param>
      /// <param name="materialId"></param>
      /// <returns>The Handle to IfcMaterialConstituent</returns>
      public static IFCAnyHandle GetOrCreateMaterialConstituent(ExporterIFC exporterIFC, ElementId materialId)
      {
         IFCAnyHandle materialConstituentHandle = ExporterCacheManager.MaterialConstituentCache.Find(materialId);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(materialConstituentHandle))
         {
            Document document = ExporterCacheManager.Document;
            IFCAnyHandle materialHnd = GetOrCreateMaterialHandle(exporterIFC, materialId);

            //Material constituent name will be defaulted to the same as the material name
            Material material = document.GetElement(materialId) as Material;
            string constituentName = (material != null) ? NamingUtil.GetMaterialName(material) : "<Unnamed>";
            MaterialConstituentInfo constInfo = new MaterialConstituentInfo(constituentName, materialId);

            materialConstituentHandle = GetOrCreateMaterialConstituent(exporterIFC, constInfo);
         }

         return materialConstituentHandle;
      }

      /// <summary>
      /// Get or Create (if not yet exists) a handle for IfcMaterialConstituent. It points to an IfcMaterial
      /// </summary>
      /// <param name="exporterIFC"></param>
      /// <param name="materialId"></param>
      /// <returns>The Handle to IfcMaterialConstituent</returns>
      public static IFCAnyHandle GetOrCreateMaterialConstituent(ExporterIFC exporterIFC, MaterialConstituentInfo constInfo)
      {
         IFCAnyHandle materialConstituentHandle = ExporterCacheManager.MaterialConstituentCache.Find(constInfo);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(materialConstituentHandle))
         {
            Document document = ExporterCacheManager.Document;
            IFCAnyHandle materialHnd = GetOrCreateMaterialHandle(exporterIFC, constInfo.MaterialId);

            Material material = document.GetElement(constInfo.MaterialId) as Material;
            string category = (material != null) ? NamingUtil.GetMaterialCategoryName(material) : string.Empty;

            materialConstituentHandle = IFCInstanceExporter.CreateMaterialConstituent(exporterIFC.GetFile(), materialHnd, name: constInfo.ComponentCat, category: category);
            ExporterCacheManager.MaterialConstituentCache.Register(constInfo, materialConstituentHandle);
         }

         return materialConstituentHandle;
      }

      /// <summary>
      /// Gets material handle from material id or creates one if there is none.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="materialId">The material id.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle GetOrCreateMaterialHandle(ExporterIFC exporterIFC, ElementId materialId)
      {
         Document document = ExporterCacheManager.Document;
         IFCAnyHandle materialNameHandle = ExporterCacheManager.MaterialHandleCache.Find(materialId);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(materialNameHandle))
         {
            string materialName = "<Unnamed>";
            string description = null;
            string category = null;
            if (materialId != ElementId.InvalidElementId)
            {
               Material material = document.GetElement(materialId) as Material;
               if (material != null)
               {
                  materialName = NamingUtil.GetMaterialName(material);

                  if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
                  {
                     category = NamingUtil.GetMaterialCategoryName(material);
                     description = NamingUtil.GetOverrideStringValue(material, "IfcDescription", null);
                  }
               }
            }

            IFCFile file = exporterIFC.GetFile();
            materialNameHandle = IFCInstanceExporter.CreateMaterial(file, materialName, description: description, category: category);

            ExporterCacheManager.MaterialHandleCache.Register(materialId, materialNameHandle);

            // associate Material with SurfaceStyle if necessary.
            if (materialId != ElementId.InvalidElementId && !ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && materialNameHandle.HasValue)
            {
               HashSet<IFCAnyHandle> matRepHandles = IFCAnyHandleUtil.GetHasRepresentation(materialNameHandle);
               if (matRepHandles.Count == 0)
               {
                  Material matElem = document.GetElement(materialId) as Material;

                  // TODO_DOUBLE_PATTERN - deal with background pattern
                  ElementId fillPatternId = (matElem != null) ? matElem.CutForegroundPatternId : ElementId.InvalidElementId;
                  Color color = (matElem != null) ? GetSafeColor(matElem.CutForegroundPatternColor) : new Color(0, 0, 0);

                  double planScale = 100.0;

                  HashSet<IFCAnyHandle> styles = new HashSet<IFCAnyHandle>();

                  bool hasFill = false;

                  IFCAnyHandle styledRepItem = null;
                  IFCAnyHandle matStyleHnd = GetOrCreateMaterialStyle(document, file, materialId);
                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(matStyleHnd) && !ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
                  {
                     styles.Add(matStyleHnd);

                     bool supportCutStyles = !ExporterCacheManager.ExportOptionsCache.ExportAsCoordinationView2;
                     if (fillPatternId != ElementId.InvalidElementId && supportCutStyles)
                     {
                        IFCAnyHandle cutStyleHnd = exporterIFC.GetOrCreateFillPattern(fillPatternId, color, planScale);
                        if (cutStyleHnd.HasValue)
                        {
                           styles.Add(cutStyleHnd);
                           hasFill = true;
                        }
                     }

                     IFCAnyHandle styledItemHnd;
                     if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
                     {
                        IFCAnyHandle presStyleHnd = IFCInstanceExporter.CreatePresentationStyleAssignment(file, styles);

                        HashSet<IFCAnyHandle> presStyleSet = new HashSet<IFCAnyHandle>();
                        presStyleSet.Add(presStyleHnd);

                        styledItemHnd = IFCInstanceExporter.CreateStyledItem(file, styledRepItem, presStyleSet, null);
                     }
                     else
                     {
                        styledItemHnd = IFCInstanceExporter.CreateStyledItem(file, styledRepItem, styles, null);
                     }

                     IFCAnyHandle contextOfItems = exporterIFC.Get3DContextHandle("");

                     string repId = "Style";
                     string repType = (hasFill) ? "Material and Cut Pattern" : "Material";
                     HashSet<IFCAnyHandle> repItems = new HashSet<IFCAnyHandle>();

                     repItems.Add(styledItemHnd);

                     IFCAnyHandle styleRepHnd = IFCInstanceExporter.CreateStyledRepresentation(file, contextOfItems, repId, repType, repItems);

                     List<IFCAnyHandle> repList = new List<IFCAnyHandle>();
                     repList.Add(styleRepHnd);

                     IFCAnyHandle matDefRepHnd = IFCInstanceExporter.CreateMaterialDefinitionRepresentation(file, null, null, repList, materialNameHandle);
                  }
               }
            }
         }
         return materialNameHandle;
      }

      public static IFCAnyHandle GetOrCreateMaterialSet(ExporterIFC exporterIFC, FamilySymbol familySymbol, MaterialAndProfile materialAndProfile)
      {
         IFCFile file = exporterIFC.GetFile();
         ElementId typeId = familySymbol.Id;
         IFCAnyHandle materialSet = ExporterCacheManager.MaterialSetCache.FindProfileSet(typeId);
         if (materialSet == null && materialAndProfile != null)
         {
            IList<IFCAnyHandle> matProf = new List<IFCAnyHandle>();
            foreach (KeyValuePair<ElementId, IFCAnyHandle> MnP in materialAndProfile.GetKeyValuePairs())
            {
               IFCAnyHandle materialHnd = CategoryUtil.GetOrCreateMaterialHandle(exporterIFC, MnP.Key);
               if (materialHnd != null && !ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 && !ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
                  matProf.Add(IFCInstanceExporter.CreateMaterialProfile(file, MnP.Value, Material: materialHnd, name: familySymbol.Name));
            }

            if (matProf.Count > 0)
            {
               materialSet = IFCInstanceExporter.CreateMaterialProfileSet(file, matProf, name: familySymbol.Name);
               ExporterCacheManager.MaterialSetCache.RegisterProfileSet(typeId, materialSet);
            }
         }

         return materialSet;
      }

      public static IFCAnyHandle GetOrCreateMaterialConstituentSet(IFCFile file, IFCAnyHandle instanceHandle, HashSet<IFCAnyHandle> constituentSet)
      {
         IFCAnyHandle constituentSetHnd = ExporterCacheManager.MaterialConstituentSetCache.Find(constituentSet);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(constituentSetHnd))
         {
            constituentSetHnd = IFCInstanceExporter.CreateMaterialConstituentSet(file, constituentSet, name: "MaterialConstituentSet");
            ExporterCacheManager.MaterialConstituentSetCache.Register(constituentSet, constituentSetHnd);

            ElementId elementId = ElementId.InvalidElementId;
            if (IFCAnyHandleUtil.IsSubTypeOf(instanceHandle, IFCEntityType.IfcProduct))
               elementId = ExporterCacheManager.HandleToElementCache.Find(instanceHandle);
            else if (IFCAnyHandleUtil.IsSubTypeOf(instanceHandle, IFCEntityType.IfcTypeProduct))
            {
               ElementTypeKey eTypeKey = ExporterCacheManager.ElementTypeToHandleCache.Find(instanceHandle);
               if (eTypeKey != null)
                  elementId = eTypeKey.Item1.Id;
            }

            if (elementId != ElementId.InvalidElementId)
               ExporterCacheManager.MaterialSetCache.RegisterConstituentSetHnd(elementId, constituentSetHnd);
         }

         return constituentSetHnd;
      }
   }
}