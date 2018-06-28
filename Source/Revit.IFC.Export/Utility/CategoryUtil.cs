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
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
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
      /// Get various color values from element's material
      /// </summary>
      /// <param name="element">the element</param>
      /// <param name="materialColor">element's material color</param>
      /// <param name="surfacePatternColor">element's material surface pattern color</param>
      /// <param name="cutPatternColor">element's material cut pattern color</param>
      /// <param name="opacity">material opacity</param>
      public static void GetElementColorAndTransparency(Element element, out Color materialColor, out Color surfacePatternColor, out Color cutPatternColor, out double? opacity)
      {
         Category category = element.Category;

         materialColor = null;
         surfacePatternColor = null;
         cutPatternColor = null;
         opacity = null;

         ElementId materialId = element.GetMaterialIds(false).FirstOrDefault();
         Material matElem = (materialId != null) ? element.Document.GetElement(materialId) as Material : null;

         if (matElem == null)
         {
            if (category == null)
            {
               return;
            }
            matElem = category.Material;
         }

         if (matElem != null)
         {
            materialColor = GetSafeColor(matElem.Color);
            surfacePatternColor = GetSafeColor(matElem.SurfacePatternColor);
            cutPatternColor = GetSafeColor(matElem.CutPatternColor);
            opacity = (double) (100 - matElem.Transparency)/100;
         }
         else
         {
            Color color = GetSafeColor(category.LineColor);

            // Grey is returned in place of pure black.  For systems which default to a black background color, 
            // Grey is more of a contrast.  
            if (color.Red == 0 && color.Green == 0 && color.Blue == 0)
               color = new Color(0x7f, 0x7f, 0x7f);

            materialColor = color;
            surfacePatternColor = color;
            cutPatternColor = color;
            opacity = 1.0;
         }
      }

      /// <summary>
      /// Checks if element is external.
      /// </summary>
      /// <remarks>
      /// An element is considered external if either:
      ///   <li> A special Yes/No parameter "IsExternal" is applied to it or its type and it's value is set to "yes".</li>
      ///   <li> The element itself has information about being an external element.</li>
      /// All other elements are internal.
      /// </remarks>
      /// <param name="element">The element.</param>
      /// <returns>True if the element is external, false otherwise.</returns>
      public static bool IsElementExternal(Element element)
      {
         if (element == null)
            return false;

         Document document = element.Document;

         // Look for a parameter "IsExternal", potentially localized.
         {
            ElementId elementId = element.Id;

            bool? maybeIsExternal = null;
            if (!ExporterCacheManager.IsExternalParameterValueCache.TryGetValue(elementId, out maybeIsExternal))
            {
               int intIsExternal = 0;
               string localExternalParamName = PropertySetEntryUtil.GetLocalizedIsExternal(ExporterCacheManager.LanguageType);
               if ((localExternalParamName != null) && (ParameterUtil.GetIntValueFromElementOrSymbol(element, localExternalParamName, out intIsExternal) != null))
                  maybeIsExternal = (intIsExternal != 0);

               if (!maybeIsExternal.HasValue && (ExporterCacheManager.LanguageType != LanguageType.English_USA))
               {
                  string externalParamName = PropertySetEntryUtil.GetLocalizedIsExternal(LanguageType.English_USA);
                  if (ParameterUtil.GetIntValueFromElementOrSymbol(element, externalParamName, out intIsExternal) != null)
                     maybeIsExternal = (intIsExternal != 0);
               }

               ExporterCacheManager.IsExternalParameterValueCache.Add(new KeyValuePair<ElementId, bool?>(elementId, maybeIsExternal));
            }

            if (maybeIsExternal.HasValue)
               return maybeIsExternal.Value;
         }

         // Many element types have the FUNCTION_PARAM parameter.  If this is set, use its value.
         ElementType elementType = document.GetElement(element.GetTypeId()) as ElementType;
         int elementFunction;
         if ((elementType != null) && ParameterUtil.GetIntValueFromElement(elementType, BuiltInParameter.FUNCTION_PARAM, out elementFunction) != null)
         {
            // Note that the WallFunction enum value is the same for many different kinds of objects.
            return elementFunction != ((int)WallFunction.Interior);
         }

         // Specific element types that know if they are external or not if the built-in parameter isn't set.
         // Categories are used, and not types, to also support in-place families 

         // Roofs are always external
         ElementId categoryId = element.Category.Id;
         if (categoryId == new ElementId(BuiltInCategory.OST_Roofs) ||
             categoryId == new ElementId(BuiltInCategory.OST_MassExteriorWall))
            return true;

         // Mass interior walls are always internal
         if (categoryId == new ElementId(BuiltInCategory.OST_MassInteriorWall))
            return false;

         // Family instances may be hosted on an external element
         if (element is FamilyInstance)
         {
            FamilyInstance familyInstance = element as FamilyInstance;
            Element familyInstanceHost = familyInstance.Host;
            if (familyInstanceHost == null)
            {
               Reference familyInstanceHostReference = familyInstance.HostFace;
               if (familyInstanceHostReference != null)
                  familyInstanceHost = document.GetElement(familyInstanceHostReference);
            }

            if (familyInstanceHost != null)
               return IsElementExternal(familyInstanceHost);
         }

         return false;
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
         IList<IFCAnyHandle> materials = new List<IFCAnyHandle>();
         HashSet<IFCAnyHandle> constituentSet = new HashSet<IFCAnyHandle>();
         foreach (ElementId materialId in materialList)
         {
            if (materialId != ElementId.InvalidElementId)
            {
               IFCAnyHandle matHnd = GetOrCreateMaterialHandle(exporterIFC, materialId);
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(matHnd))
                  materials.Add(matHnd);

               // in IFC4 we will create IfcConstituentSet instead of MaterialList, create the associated IfcConstituent here from IfcMaterial
               if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
               {
                  IFCAnyHandle constituentHnd = GetOrCreateMaterialConstituent(exporterIFC, materialId);
                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(constituentHnd))
                     constituentSet.Add(constituentHnd);
               }
            }
         }

         if (materials.Count == 0)
            return;

         if (materials.Count == 1)
         {
            ExporterCacheManager.MaterialRelationsCache.Add(materials[0], instanceHandle);
            // Delete IfcMaterialConstituent that has been created if it turns out that there is only one material that can be defined
            if (constituentSet.Count > 0)
            {
               foreach (IFCAnyHandle matC in constituentSet)
               {
                  matC.Delete();
               }
            }
            return;
         }

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            IFCAnyHandle materialConsituentSetHnd = GetOrCreateMaterialConstituentSet(exporterIFC, constituentSet);
            ExporterCacheManager.MaterialRelationsCache.Add(materialConsituentSetHnd, instanceHandle);
         }
         else
         {
            IFCAnyHandle materialListHnd = IFCInstanceExporter.CreateMaterialList(exporterIFC.GetFile(), materials);
            ExporterCacheManager.MaterialRelationsCache.Add(materialListHnd, instanceHandle);
         }
      }

      public static IFCAnyHandle GetOrCreateMaterialStyle(Document document, ExporterIFC exporterIFC, ElementId materialId)
      {
         IFCAnyHandle styleHnd = ExporterCacheManager.MaterialIdToStyleHandleCache.Find(materialId);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(styleHnd))
         {
            Material material = document.GetElement(materialId) as Material;
            if (material == null)
               return null;

            string matName = NamingUtil.GetNameOverride(material, material.Name);

            Color color = GetSafeColor(material.Color);
            double blueVal = color.Blue / 255.0;
            double greenVal = color.Green / 255.0;
            double redVal = color.Red / 255.0;

            IFCFile file = exporterIFC.GetFile();
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
      /// <returns>the Handle to IfcMaterialConstituent</returns>
      public static IFCAnyHandle GetOrCreateMaterialConstituent(ExporterIFC exporterIFC, ElementId materialId)
      {
         Document document = ExporterCacheManager.Document;
         //IFCAnyHandle materialConstituentHnd = ExporterCacheManager.MaterialConstituentCache.Find(materialId);
         //if (IFCAnyHandleUtil.IsNullOrHasNoValue(materialConstituentHnd))
         //{
         IFCAnyHandle materialHnd = GetOrCreateMaterialHandle(exporterIFC, materialId);

         string constituentName = "<Unnamed>";
         string category = string.Empty;
         if (materialId != ElementId.InvalidElementId)
         {
            Material material = document.GetElement(materialId) as Material;
            if (material != null)
            {
               constituentName = material.Name;
               category = material.Category.Name;
            }
         }

         IFCAnyHandle materialConstituentHnd = IFCInstanceExporter.CreateMaterialConstituent(exporterIFC.GetFile(), materialHnd, name: constituentName, category: category);
         //ExporterCacheManager.MaterialConstituentCache.Register(materialId, materialConstituentHnd);
         //}
         return materialConstituentHnd;
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
            string materialName = " <Unnamed>";
            string description = null;
            string category = null;
            if (materialId != ElementId.InvalidElementId)
            {
               Material material = document.GetElement(materialId) as Material;
               if (material != null)
                  materialName = NamingUtil.GetNameOverride(material, material.Name);

               if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
               {
                  category = NamingUtil.GetOverrideStringValue(material, "IfcCategory", 
                     NamingUtil.GetOverrideStringValue(material, "Category", material.MaterialCategory));
                  description = NamingUtil.GetOverrideStringValue(material, "IfcDescription", null);
               }
            }

            materialNameHandle = IFCInstanceExporter.CreateMaterial(exporterIFC.GetFile(), materialName, description: description, category: category);

            ExporterCacheManager.MaterialHandleCache.Register(materialId, materialNameHandle);

            // associate Material with SurfaceStyle if necessary.
            IFCFile file = exporterIFC.GetFile();
            if (materialId != ElementId.InvalidElementId && !ExporterCacheManager.ExportOptionsCache.ExportAs2x2 && materialNameHandle.HasValue)
            {
               HashSet<IFCAnyHandle> matRepHandles = IFCAnyHandleUtil.GetHasRepresentation(materialNameHandle);
               if (matRepHandles.Count == 0)
               {
                  Material matElem = document.GetElement(materialId) as Material;

                  // TODO_DOUBLE_PATTERN - deal with background pattern
                  ElementId fillPatternId = (matElem != null) ? matElem.CutPatternId : ElementId.InvalidElementId;
                  Autodesk.Revit.DB.Color color = (matElem != null) ? GetSafeColor(matElem.CutPatternColor) : new Color(0, 0, 0);

                  double planScale = 100.0;

                  HashSet<IFCAnyHandle> styles = new HashSet<IFCAnyHandle>();

                  bool hasFill = false;

                  IFCAnyHandle styledRepItem = null;
                  IFCAnyHandle matStyleHnd = CategoryUtil.GetOrCreateMaterialStyle(document, exporterIFC, materialId);
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

                     IFCAnyHandle presStyleHnd = IFCInstanceExporter.CreatePresentationStyleAssignment(file, styles);

                     HashSet<IFCAnyHandle> presStyleSet = new HashSet<IFCAnyHandle>();
                     presStyleSet.Add(presStyleHnd);

                     IFCAnyHandle styledItemHnd = IFCInstanceExporter.CreateStyledItem(file, styledRepItem, presStyleSet, null);

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
               if (materialHnd != null && ExporterCacheManager.ExportOptionsCache.ExportAs4)
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

      public static IFCAnyHandle GetOrCreateMaterialConstituentSet(ExporterIFC exporterIFC, HashSet<IFCAnyHandle> constituentSet)
      {
         IFCAnyHandle constituentSetHnd = ExporterCacheManager.MaterialConstituentSetCache.Find(constituentSet);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(constituentSetHnd))
         {
            constituentSetHnd = IFCInstanceExporter.CreateMaterialConstituentSet(exporterIFC.GetFile(), constituentSet, name: "MaterialConstituentSet");
            ExporterCacheManager.MaterialConstituentSetCache.Register(constituentSet, constituentSetHnd);
         }

         return constituentSetHnd;
      }
   }
}