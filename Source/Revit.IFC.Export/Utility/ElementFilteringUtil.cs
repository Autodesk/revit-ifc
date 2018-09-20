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
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.Structure;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// A class that hold information for exporting what IfcEntity and its type pair
   /// </summary>
   public class IFCExportInfoPair
   {
      /// <summary>
      /// The IfcEntity for export
      /// </summary>
      public IFCEntityType ExportInstance { get; set; } = IFCEntityType.UnKnown;
      /// <summary>
      /// The type for export
      /// </summary>
      public IFCEntityType ExportType { get; set; } = IFCEntityType.UnKnown;
      /// <summary>
      /// Validated PredefinedType from IfcExportType (or IfcType for the old param), or from IfcExportAs
      /// </summary>
      public string ValidatedPredefinedType { get; set; } = null;

      /// <summary>
      /// Initialization of the class
      /// </summary>
      public IFCExportInfoPair()
      {
         // Set default value if not defined
         ValidatedPredefinedType = "NOTDEFINED";
      }

      /// <summary>
      /// Initialize the class with the entity and the type
      /// </summary>
      /// <param name="instance">the entity</param>
      /// <param name="type">the type</param>
      public IFCExportInfoPair(IFCEntityType instance, IFCEntityType type, string predefinedType)
      {
         instance = ElementFilteringUtil.GetValidIFCEntityType(instance);
         ExportInstance = instance;

         type = ElementFilteringUtil.GetValidIFCEntityType(type);
         ExportType = type;

         if (!string.IsNullOrEmpty(predefinedType))
         {
            string newValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedTypeType(predefinedType, ValidatedPredefinedType, ExportInstance.ToString());
            if (ExporterUtil.IsNotDefined(newValidatedPredefinedType))
               newValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedTypeType(predefinedType, ValidatedPredefinedType, ExportType.ToString());
            ValidatedPredefinedType = newValidatedPredefinedType;
         }
         else
            ValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedTypeType("NOTDEFINED", ValidatedPredefinedType, ExportType.ToString());
      }

      /// <summary>
      /// Check whether the export information is unknown type
      /// </summary>
      public bool IsUnKnown
      {
         get { return (ExportInstance == IFCEntityType.UnKnown); }
      }

      /// <summary>
      /// set an static class to this object with default value unknown
      /// </summary>
      public static IFCExportInfoPair UnKnown
      {
         get { return new IFCExportInfoPair(); }
      }

      /// <summary>
      /// Assign the entity and the type pair
      /// </summary>
      /// <param name="instance">the entity</param>
      /// <param name="type">the type</param>
      public void SetValue(IFCEntityType instance, IFCEntityType type, string predefinedType)
      {
         instance = ElementFilteringUtil.GetValidIFCEntityType(instance);
         ExportInstance = instance;

         type = ElementFilteringUtil.GetValidIFCEntityType(type);
         ExportType = type;

         if (!string.IsNullOrEmpty(predefinedType))
         {
            string newValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedTypeType(predefinedType, ValidatedPredefinedType, ExportInstance.ToString());
            if (ExporterUtil.IsNotDefined(newValidatedPredefinedType))
               newValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedTypeType(predefinedType, ValidatedPredefinedType, ExportType.ToString());
            ValidatedPredefinedType = newValidatedPredefinedType;
         }
         else
            ValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedTypeType("NOTDEFINED", ValidatedPredefinedType, ExportType.ToString());
      }

      /// <summary>
      /// Set the pair information using only either the entity or the type
      /// </summary>
      /// <param name="entityType">the entity or type</param>
      public void SetValueWithPair(IFCEntityType entityType)
      {
         string entityTypeStr = entityType.ToString();
         bool isType = entityTypeStr.Substring(entityTypeStr.Length - 4, 4).Equals("Type", StringComparison.CurrentCultureIgnoreCase);
         
         if (isType)
         {
            // Get the instance
            string instName = entityTypeStr.Substring(0, entityTypeStr.Length - 4);
            IfcSchemaEntityNode node = IfcSchemaEntityTree.Find(instName);
            if (node != null && !node.isAbstract)
            {
               IFCEntityType instType = IFCEntityType.UnKnown;
               if (IFCEntityType.TryParse(instName, out instType))
                  ExportInstance = instType;
            }
            // If not found, try non-abstract supertype derived from the type
            node = IfcSchemaEntityTree.FindNonAbsInstanceSuperType(instName);
            if (node != null)
            {
               IFCEntityType instType = IFCEntityType.UnKnown;
               if (IFCEntityType.TryParse(node.Name, out instType))
                  ExportInstance = instType;
            }

            // set the type
            entityType = ElementFilteringUtil.GetValidIFCEntityType(entityType);
            if (entityType != IFCEntityType.UnKnown)
               ExportType = entityType;
            else
            {
               node = IfcSchemaEntityTree.FindNonAbsInstanceSuperType(entityTypeStr);
               if (node != null)
               {
                  IFCEntityType instType = IFCEntityType.UnKnown;
                  if (IFCEntityType.TryParse(node.Name, out instType))
                     ExportType = instType;
               }
            }
         }
         else
         {
            // set the instance
            entityType = ElementFilteringUtil.GetValidIFCEntityType(entityType);
            if (entityType != IFCEntityType.UnKnown)
               ExportInstance = entityType;
            else
            {
               // If not found, try non-abstract supertype derived from the type
               IfcSchemaEntityNode node = IfcSchemaEntityTree.FindNonAbsInstanceSuperType(entityTypeStr);
               if (node != null)
               {
                  IFCEntityType instType = IFCEntityType.UnKnown;
                  if (IFCEntityType.TryParse(node.Name, out instType))
                     ExportInstance = instType;
               }
            }

            // set the type pair
            string typeName = entityType.ToString() + "Type";
            entityType = ElementFilteringUtil.GetValidIFCEntityType(typeName);
            if (entityType != IFCEntityType.UnKnown)
               ExportType = entityType;
            else
            {
               IfcSchemaEntityNode node = IfcSchemaEntityTree.FindNonAbsInstanceSuperType(typeName);
               if (node != null)
               {
                  IFCEntityType instType = IFCEntityType.UnKnown;
                  if (IFCEntityType.TryParse(node.Name, out instType))
                     ExportType = instType;
               }
            }
         }

         ValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedTypeType("NOTDEFINED", ValidatedPredefinedType, ExportType.ToString());
      }
   }

   /// <summary>
   /// Provides static methods for filtering elements.
   /// </summary>
   class ElementFilteringUtil
   {
      /// <summary>
      /// Gets spatial element filter.
      /// </summary>
      /// <param name="document">The Revit document.</param>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <returns>The element filter.</returns>
      public static ElementFilter GetSpatialElementFilter(Document document, ExporterIFC exporterIFC)
      {
         return GetExportFilter(document, exporterIFC, true);
      }

      /// <summary>
      /// Gets filter for non spatial elements.
      /// </summary>
      /// <param name="document">The Revit document.</param>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <returns>The Element filter.</returns>
      public static ElementFilter GetNonSpatialElementFilter(Document document, ExporterIFC exporterIFC)
      {
         return GetExportFilter(document, exporterIFC, false);
      }

      /// <summary>
      /// Gets element filter for export.
      /// </summary>
      /// <param name="document">The Revit document.</param>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="forSpatialElements">True to get spatial element filter, false for non spatial elements filter.</param>
      /// <returns>The element filter.</returns>
      private static ElementFilter GetExportFilter(Document document, ExporterIFC exporterIFC, bool forSpatialElements)
      {
         List<ElementFilter> filters = new List<ElementFilter>();

         // Class types & categories
         ElementFilter classFilter = GetClassFilter(forSpatialElements);

         // Special handling for family instances and view specific elements
         if (!forSpatialElements)
         {
            ElementFilter familyInstanceFilter = GetFamilyInstanceFilter(exporterIFC);

            List<ElementFilter> classFilters = new List<ElementFilter>();
            classFilters.Add(classFilter);
            classFilters.Add(familyInstanceFilter);

            if (ExporterCacheManager.ExportOptionsCache.ExportAnnotations)
            {
               ElementFilter ownerViewFilter = GetViewSpecificTypesFilter(exporterIFC);
               classFilters.Add(ownerViewFilter);
            }

            classFilter = new LogicalOrFilter(classFilters);
         }

         filters.Add(classFilter);

         // Design options
         filters.Add(GetDesignOptionFilter());

         // Phases: only for non-spatial elements.  For spatial elements, we will do a check afterwards.
         if (!forSpatialElements && !ExporterCacheManager.ExportOptionsCache.ExportingLink)
            filters.Add(GetPhaseStatusFilter(document));

         return new LogicalAndFilter(filters);
      }

      /// <summary>
      /// Gets element filter for family instance.
      /// </summary>
      /// <param name="exporter">The ExporterIFC object.</param>
      /// <returns>The element filter.</returns>
      private static ElementFilter GetFamilyInstanceFilter(ExporterIFC exporter)
      {
         List<ElementFilter> filters = new List<ElementFilter>();
         filters.Add(new ElementOwnerViewFilter(ElementId.InvalidElementId));
         filters.Add(new ElementClassFilter(typeof(FamilyInstance)));
         LogicalAndFilter andFilter = new LogicalAndFilter(filters);

         return andFilter;
      }

      /// <summary>
      /// Gets element filter meeting design option requirements.
      /// </summary>
      /// <returns>The element filter.</returns>
      private static ElementFilter GetDesignOptionFilter()
      {
         // We will respect the active design option if we are exporting a specific view.
         ElementFilter noDesignOptionFilter = new ElementDesignOptionFilter(ElementId.InvalidElementId);
         ElementFilter primaryOptionsFilter = new PrimaryDesignOptionMemberFilter();
         ElementFilter designOptionFilter = new LogicalOrFilter(noDesignOptionFilter, primaryOptionsFilter);

         View filterView = ExporterCacheManager.ExportOptionsCache.FilterViewForExport;
         if (filterView != null)
         {
            ElementId designOptionId = DesignOption.GetActiveDesignOptionId(ExporterCacheManager.Document);
            if (designOptionId != ElementId.InvalidElementId)
            {
               ElementFilter activeDesignOptionFilter = new ElementDesignOptionFilter(designOptionId);
               return new LogicalOrFilter(designOptionFilter, activeDesignOptionFilter);
            }
         }

         return designOptionFilter;
      }

      // Cannot be implemented until ExportLayerTable can be read.  Replacement is ShouldCategoryBeExported()
      /*private static ElementFilter GetCategoryFilter()
      {

      }*/

      /// <summary>
      /// Checks if element in certain category should be exported.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="allowSeparateOpeningExport">True if IfcOpeningElement is allowed to be exported.</param>
      /// <returns>True if the element should be exported, false otherwise.</returns>
      private static bool ShouldCategoryBeExported(ExporterIFC exporterIFC, Element element, bool allowSeparateOpeningExport)
      {
         IFCExportInfoPair exportType = new IFCExportInfoPair();
         ElementId categoryId;
         string ifcClassName = ExporterUtil.GetIFCClassNameFromExportTable(exporterIFC, element, out categoryId);
         if (string.IsNullOrEmpty(ifcClassName))
         {
            // Special case: these elements aren't contained in the default export layers mapping table.
            // This allows these elements to be exported by default.
            if (element is AreaScheme || element is Group)
               ifcClassName = "IfcGroup";
            else if (element is ElectricalSystem)
               ifcClassName = "IfcSystem";
            else
               return false;
         }

         bool foundName = string.Compare(ifcClassName, "Default", true) != 0;
         if (foundName)
            exportType = GetExportTypeFromClassName(ifcClassName);
         if (!foundName)
            return true;

         if (exportType.ExportInstance == IFCEntityType.UnKnown)
            return false;

         // We don't export openings directly, only via the element they are opening, unless flag is set.
         if (exportType.ExportInstance == IFCEntityType.IfcOpeningElement && !allowSeparateOpeningExport)
            return false;

         // Check whether the intended Entity type is inside the export exclusion set
         Common.Enums.IFCEntityType elementClassTypeEnum;
         if (Enum.TryParse<Common.Enums.IFCEntityType>(ifcClassName, out elementClassTypeEnum))
            if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
               return false;

         return true;
      }

      /// <summary>
      /// Checks if an element has the IfcExportAs variable set to "DontExport".
      /// </summary>
      /// <param name="element">The element.</param>
      /// <returns>True if the element has the IfcExportAs variable set to "DontExport".</returns>
      public static bool IsIFCExportAsSetToDontExport(Element element)
      {
         string exportAsEntity = "IFCExportAs";
         string elementClassName;
         if (ParameterUtil.GetStringValueFromElementOrSymbol(element, exportAsEntity, out elementClassName) != null)
         {
            if (CompareAlphaOnly(elementClassName, "DONTEXPORT"))
               return true;
         }
         return false;
      }

      /// <summary>
      /// Checks if element should be exported using a variety of different checks.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="allowSeparateOpeningExport">True if IfcOpeningElement is allowed to be exported.</param>
      /// <returns>True if the element should be exported, false otherwise.</returns>
      /// <remarks>There are some inefficiencies here, as we later check IfcExportAs in other contexts.  We should attempt to get the value only once.</remarks>
      public static bool ShouldElementBeExported(ExporterIFC exporterIFC, Element element, bool allowSeparateOpeningExport)
      {
         // Allow the ExporterStateManager to say that an element should be exported regardless of settings.
         if (ExporterStateManager.CanExportElementOverride())
            return true;

         // Check to see if the category should be exported.  This overrides the IfcExportAs parameter.
         if (!ShouldCategoryBeExported(exporterIFC, element, allowSeparateOpeningExport))
            return false;

         string exportAsEntity = "IFCExportAs";
         string elementClassName;
         if (ParameterUtil.GetStringValueFromElementOrSymbol(element, exportAsEntity, out elementClassName) != null)
         {
            string enumTypeValue = string.Empty;
            ExporterUtil.ExportEntityAndPredefinedType(elementClassName, out elementClassName, out enumTypeValue);

            if (CompareAlphaOnly(elementClassName, "DONTEXPORT"))
               return false;

            // Check whether the intended Entity type is inside the export exclusion set
            Common.Enums.IFCEntityType elementClassTypeEnum;
            if (Enum.TryParse<Common.Enums.IFCEntityType>(elementClassName, out elementClassTypeEnum))
               if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
                  return false;
         }
         return true;
      }

      /// <summary>
      /// Determines if the selected element meets extra criteria for export.
      /// </summary>
      /// <param name="exporterIFC">The exporter class.</param>
      /// <param name="element">The current element to export.</param>
      /// <param name="allowSeparateOpeningExport">True if IfcOpeningElement is allowed to be exported.</param>
      /// <returns>True if the element should be exported, false otherwise.</returns>
      public static bool CanExportElement(ExporterIFC exporterIFC, Autodesk.Revit.DB.Element element, bool allowSeparateOpeningExport)
      {
         if (!ElementFilteringUtil.ShouldElementBeExported(exporterIFC, element, allowSeparateOpeningExport))
            return false;

         // if we allow exporting parts as independent building elements, then prevent also exporting the host elements containing the parts.
         bool checkIfExportingPart = ExporterCacheManager.ExportOptionsCache.ExportPartsAsBuildingElements || element is Part;
         if (checkIfExportingPart && PartExporter.CanExportParts(element))
            return false;

         return true;
      }

      /// <summary>
      /// Checks if name is equal to base or its type name.
      /// </summary>
      /// <param name="name">The object type name.</param>
      /// <param name="baseName">The IFC base name.</param>
      /// <returns>True if equal, false otherwise.</returns>
      private static bool IsEqualToTypeName(String name, String baseName)
      {
         if (String.Compare(name, baseName, true) == 0)
            return true;

         String typeName = baseName + "Type";
         return (String.Compare(name, typeName, true) == 0);
      }

      /// <summary>
      /// Compares two strings, ignoring spaces, punctuation and case.
      /// </summary>
      /// <param name="name">The string to compare.</param>
      /// <param name="baseNameAllCapsNoSpaces">String to compare to, all caps, no punctuation or cases.</param>
      /// <returns></returns>
      private static bool CompareAlphaOnly(String name, String baseNameAllCapsNoSpaces)
      {
         if (string.IsNullOrEmpty(name))
            return string.IsNullOrEmpty(baseNameAllCapsNoSpaces);
         string nameToUpper = name.ToUpper();
         int loc = 0;
         int maxLen = baseNameAllCapsNoSpaces.Length;
         foreach (char c in nameToUpper)
         {
            if (c >= 'A' && c <= 'Z')
            {
               if (baseNameAllCapsNoSpaces[loc] != c)
                  return false;
               loc++;
               if (loc == maxLen)
                  return true;
            }
         }
         return false;
      }

      /// <summary>
      /// Gets export type from IFC class name.
      /// </summary>
      /// <param name="ifcClassName">The IFC class name.</param>
      /// <returns>The export type.</returns>
      public static IFCExportInfoPair GetExportTypeFromClassName(String ifcClassName)
      {
         IFCExportInfoPair exportInfoPair = new IFCExportInfoPair();

         if (ifcClassName.StartsWith("Ifc", true, null))
         {
            // Here we try to catch any possible types that are missing above by checking both the class name or the type name
            // Unless there is any special treatment needed most of the above check can be done here
            string clName = ifcClassName.Substring(ifcClassName.Length - 4, 4).Equals("Type", StringComparison.CurrentCultureIgnoreCase) ? ifcClassName.Substring(0, ifcClassName.Length - 4) : ifcClassName;
            string tyName = null;
            if ( ((ExporterCacheManager.ExportOptionsCache.ExportAs2x2 || ExporterCacheManager.ExportOptionsCache.ExportAs2x3))
                  && (clName.Equals("IfcDoor", StringComparison.InvariantCultureIgnoreCase) || clName.Equals("ifcWindow", StringComparison.InvariantCultureIgnoreCase)) )
            {
               // Prior to IFC4 Door and Window types are not "Ifc..Type", but "Ifc.. Style"
               tyName = clName + "Style";
            }
            else
               tyName = clName + "Type";

            IFCEntityType theGenExportClass;
            IFCEntityType theGenExportType;
            var ifcEntitySchemaTree = IfcSchemaEntityTree.GetEntityDictFor(ExporterCacheManager.ExportOptionsCache.FileVersion);
            if (ifcEntitySchemaTree == null || ifcEntitySchemaTree.Count == 0)
               throw new Exception("Unable to locate IFC Schema xsd file! Make sure the relevant xsd " + ExporterCacheManager.ExportOptionsCache.FileVersion + " exists.");

            bool clNameValid = false;
            bool tyNameValid = false;

            // Deal with small number of IFC2x3/IFC4 types that have changed in a hardwired way.
            if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
            {
               if (string.Compare(clName, "IfcBurner", true) == 0)
               {
                  clName = "IfcFlowTerminal";
                  tyName = "IfcGasTerminalType";
               }
               else if (string.Compare(clName, "IfcElectricDistributionBoard", true) == 0)
               {
                  clName = "IfcElectricDistributionPoint";
                  tyName = "";
               }
            }
            else
            {
               if (string.Compare(clName, "IfcGasTerminal", true) == 0)
               {
                  clName = "IfcBurner";
                  tyName = "IfcBurnerType";
               }
               else if (string.Compare(clName, "IfcElectricDistributionPoint", true) == 0)
               {
                  clName = "IfcElectricDistributionBoard";
                  tyName = "IfcElectricDistributionBoardType";
               }
               else if (string.Compare(clName, "IfcElectricHeater", true) == 0)
               {
                  clName = "IfcSpaceHeater";
                  tyName = "IfcSpaceHeaterType";
               }
            }

            IfcSchemaEntityNode clNode = IfcSchemaEntityTree.Find(clName);
            if (clNode != null)
               clNameValid = IfcSchemaEntityTree.IsSubTypeOf(clName, "IfcObject") && !clNode.isAbstract;

            IfcSchemaEntityNode tyNode = IfcSchemaEntityTree.Find(tyName);
            if (tyNode != null)
               tyNameValid = IfcSchemaEntityTree.IsSubTypeOf(tyName, "IfcTypeObject") && !tyNode.isAbstract;

            if (tyNameValid)
            {
               if (IFCEntityType.TryParse(tyNode.Name, out theGenExportType))
                  exportInfoPair.ExportType = theGenExportType;
            }

            if (clNameValid)
            {
               if (IFCEntityType.TryParse(clNode.Name, out theGenExportClass))
                  exportInfoPair.ExportInstance = theGenExportClass;
            }
            // If the instance is not valid, but the type is valid, try find the paired instance supertype that is not Abstract type
            else if (tyNameValid)
            {
               IfcSchemaEntityNode compatibleInstance = IfcSchemaEntityTree.FindNonAbsInstanceSuperType(tyName);
               if (compatibleInstance != null)
               {
                  if (IFCEntityType.TryParse(compatibleInstance.Name, out theGenExportClass))
                     exportInfoPair.ExportInstance = theGenExportClass;
               }
            }

            // This used to throw an exception, but this could abort export if the user enters a bad IFC class name
            // in the ExportLayerOptions table.  In the future, we should log this.
            //throw new Exception("IFC: Unknown IFC type in getExportTypeFromClassName: " + ifcClassName);
            //return IFCExportType.IfcBuildingElementProxyType;

            if (exportInfoPair.ExportInstance == IFCEntityType.UnKnown)
               exportInfoPair.ExportInstance = IFCEntityType.IfcBuildingElementProxy;
         }

         //return IFCExportType.DontExport;
         exportInfoPair.ValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedType("NOTDEFINED", exportInfoPair.ExportType.ToString());

         return exportInfoPair;
      }

      // TODO: implement  out bool exportSeparately
      /// <summary>
      /// Gets export type from category id.
      /// </summary>
      /// <param name="categoryId">The category id.</param>
      /// <param name="ifcEnumType">The string value represents the IFC type.</param>
      /// <returns>The export type.</returns>
      public static IFCExportInfoPair GetExportTypeFromCategoryId(ElementId categoryId, out string ifcEnumType /*, out bool exportSeparately*/)
      {
         IFCExportInfoPair exportInfoPair = new IFCExportInfoPair();
         ifcEnumType = "NOTDEFINED";
         //exportSeparately = true;

         if (categoryId == new ElementId(BuiltInCategory.OST_Cornices))
            exportInfoPair.ExportInstance = IFCEntityType.IfcBeam;
         else if (categoryId == new ElementId(BuiltInCategory.OST_Ceilings))
            exportInfoPair.ExportInstance = IFCEntityType.IfcCovering;
         else if (categoryId == new ElementId(BuiltInCategory.OST_CurtainWallPanels))
         {
            ifcEnumType = "CURTAIN_PANEL";
            //exportSeparately = false;
            exportInfoPair.ExportInstance = IFCEntityType.IfcPlate;
         }
         else if (categoryId == new ElementId(BuiltInCategory.OST_Doors))
            exportInfoPair.ExportInstance = IFCEntityType.IfcDoor;
         else if (categoryId == new ElementId(BuiltInCategory.OST_Furniture))
            exportInfoPair.ExportInstance = IFCEntityType.IfcFurniture;
         else if (categoryId == new ElementId(BuiltInCategory.OST_Floors))
         {
            ifcEnumType = "FLOOR";
            exportInfoPair.ExportInstance = IFCEntityType.IfcSlab;
         }
         else if (categoryId == new ElementId(BuiltInCategory.OST_IOSModelGroups))
            exportInfoPair.ExportInstance = IFCEntityType.IfcGroup;
         else if (categoryId == new ElementId(BuiltInCategory.OST_Mass))
            exportInfoPair.ExportInstance = IFCEntityType.IfcBuildingElementProxy;
         else if (categoryId == new ElementId(BuiltInCategory.OST_CurtainWallMullions))
         {
            ifcEnumType = "MULLION";
            //exportSeparately = false;
            exportInfoPair.ExportInstance = IFCEntityType.IfcMember;
         }
         else if (categoryId == new ElementId(BuiltInCategory.OST_Railings))
            exportInfoPair.ExportInstance = IFCEntityType.IfcRailing;
         else if (categoryId == new ElementId(BuiltInCategory.OST_Ramps))
            exportInfoPair.ExportInstance = IFCEntityType.IfcRamp;
         else if (categoryId == new ElementId(BuiltInCategory.OST_Roofs))
            exportInfoPair.ExportInstance = IFCEntityType.IfcRoof;
         else if (categoryId == new ElementId(BuiltInCategory.OST_Site))
            exportInfoPair.ExportInstance = IFCEntityType.IfcSite;
         else if (categoryId == new ElementId(BuiltInCategory.OST_Stairs))
            exportInfoPair.ExportInstance = IFCEntityType.IfcStair;
         else if (categoryId == new ElementId(BuiltInCategory.OST_Walls))
            exportInfoPair.ExportInstance = IFCEntityType.IfcWall;
         else if (categoryId == new ElementId(BuiltInCategory.OST_Windows))
            exportInfoPair.ExportInstance = IFCEntityType.IfcWindow;

         // Get the associated Type pair if it is a valid entity
         if (exportInfoPair.ExportInstance != IFCEntityType.UnKnown)
         {
            string typeName = exportInfoPair.ExportInstance.ToString() + "Type";
            exportInfoPair.ExportType = GetValidIFCEntityType(typeName);
            exportInfoPair.ValidatedPredefinedType = IFCValidateEntry.GetValidIFCPredefinedType(ifcEnumType, exportInfoPair.ExportType.ToString());
         }

         return exportInfoPair;
      }

      /// <summary>
      /// Gets element filter for specific views.
      /// </summary>
      /// <param name="exporter">The ExporterIFC object.</param>
      /// <returns>The element filter.</returns>
      private static ElementFilter GetViewSpecificTypesFilter(ExporterIFC exporter)
      {
         ElementFilter ownerViewFilter = GetOwnerViewFilter(exporter);

         List<Type> viewSpecificTypes = new List<Type>();
         viewSpecificTypes.Add(typeof(TextNote));
         viewSpecificTypes.Add(typeof(FilledRegion));
         ElementMulticlassFilter classFilter = new ElementMulticlassFilter(viewSpecificTypes);


         LogicalAndFilter viewSpecificTypesFilter = new LogicalAndFilter(ownerViewFilter, classFilter);
         return viewSpecificTypesFilter;
      }

      /// <summary>
      /// Gets element filter to match elements which are owned by a particular view.
      /// </summary>
      /// <param name="exporter">The exporter.</param>
      /// <returns>The element filter.</returns>
      private static ElementFilter GetOwnerViewFilter(ExporterIFC exporter)
      {
         List<ElementFilter> filters = new List<ElementFilter>();
         ICollection<ElementId> viewIds = ExporterCacheManager.DBViewsToExport.Keys;
         foreach (ElementId id in viewIds)
         {
            filters.Add(new ElementOwnerViewFilter(id));
         }
         filters.Add(new ElementOwnerViewFilter(ElementId.InvalidElementId));
         LogicalOrFilter viewFilters = new LogicalOrFilter(filters);

         return viewFilters;
      }

      /// <summary>
      /// Gets element filter that match certain types.
      /// </summary>
      /// <param name="forSpatialElements">True if to get filter for spatial element, false for other elements.</param>
      /// <returns>The element filter.</returns>
      private static ElementFilter GetClassFilter(bool forSpatialElements)
      {
         if (forSpatialElements)
         {
            return new ElementClassFilter(typeof(SpatialElement));
         }
         else
         {
            List<Type> excludedTypes = new List<Type>();

            // FamilyInstances are handled in separate filter.
            excludedTypes.Add(typeof(FamilyInstance));

            // Spatial element are exported in a separate pass.
            excludedTypes.Add(typeof(SpatialElement));

            // AreaScheme elements are exported as groups after all Areas have been exported.
            excludedTypes.Add(typeof(AreaScheme));
            // FabricArea elements are exported as groups after all FabricSheets have been exported.
            excludedTypes.Add(typeof(FabricArea));

            if (!ExporterCacheManager.ExportOptionsCache.ExportAnnotations)
               excludedTypes.Add(typeof(CurveElement));

            excludedTypes.Add(typeof(ElementType));

            excludedTypes.Add(typeof(BaseArray));

            excludedTypes.Add(typeof(FillPatternElement));
            excludedTypes.Add(typeof(LinePatternElement));
            excludedTypes.Add(typeof(Material));
            excludedTypes.Add(typeof(GraphicsStyle));
            excludedTypes.Add(typeof(Family));
            excludedTypes.Add(typeof(SketchPlane));
            excludedTypes.Add(typeof(View));
            excludedTypes.Add(typeof(Autodesk.Revit.DB.Structure.LoadBase));

            // curtain wall sub-types we are ignoring.
            excludedTypes.Add(typeof(CurtainGridLine));
            // excludedTypes.Add(typeof(Mullion));

            // this will be gotten from the element(s) it cuts.
            excludedTypes.Add(typeof(Opening));

            // 2D types we are ignoring
            excludedTypes.Add(typeof(SketchBase));
            excludedTypes.Add(typeof(FaceSplitter));

            // 2D types covered by the element owner view filter
            excludedTypes.Add(typeof(TextNote));
            excludedTypes.Add(typeof(FilledRegion));

            // exclude levels that are covered in BeginExport
            excludedTypes.Add(typeof(Level));

            // exclude analytical models
            excludedTypes.Add(typeof(Autodesk.Revit.DB.Structure.AnalyticalModel));

            ElementFilter excludedClassFilter = new ElementMulticlassFilter(excludedTypes, true);

            List<BuiltInCategory> excludedCategories = new List<BuiltInCategory>();

            // Native Revit types without match in API
            excludedCategories.Add(BuiltInCategory.OST_ConduitCenterLine);
            excludedCategories.Add(BuiltInCategory.OST_ConduitFittingCenterLine);
            excludedCategories.Add(BuiltInCategory.OST_DecalElement);
            //excludedCategories.Add(BuiltInCategory.OST_Parts);
            //excludedCategories.Add(BuiltInCategory.OST_RvtLinks);
            excludedCategories.Add(BuiltInCategory.OST_DuctCurvesCenterLine);
            excludedCategories.Add(BuiltInCategory.OST_DuctFittingCenterLine);
            excludedCategories.Add(BuiltInCategory.OST_FlexDuctCurvesCenterLine);
            excludedCategories.Add(BuiltInCategory.OST_FlexPipeCurvesCenterLine);
            excludedCategories.Add(BuiltInCategory.OST_IOS_GeoLocations);
            excludedCategories.Add(BuiltInCategory.OST_PipeCurvesCenterLine);
            excludedCategories.Add(BuiltInCategory.OST_PipeFittingCenterLine);
            excludedCategories.Add(BuiltInCategory.OST_Property);
            excludedCategories.Add(BuiltInCategory.OST_SiteProperty);
            excludedCategories.Add(BuiltInCategory.OST_SitePropertyLineSegment);
            excludedCategories.Add(BuiltInCategory.OST_TopographyContours);
            excludedCategories.Add(BuiltInCategory.OST_Viewports);
            excludedCategories.Add(BuiltInCategory.OST_Views);

            // Exclude elements with no category. 
            excludedCategories.Add(BuiltInCategory.INVALID);

            ElementMulticategoryFilter excludedCategoryFilter = new ElementMulticategoryFilter(excludedCategories, true);

            LogicalAndFilter exclusionFilter = new LogicalAndFilter(excludedClassFilter, excludedCategoryFilter);

            ElementOwnerViewFilter ownerViewFilter = new ElementOwnerViewFilter(ElementId.InvalidElementId);

            LogicalAndFilter returnedFilter = new LogicalAndFilter(exclusionFilter, ownerViewFilter);

            return returnedFilter;
         }
      }

      /// <summary>
      /// Checks if the room is in an invalid phase.
      /// </summary>
      /// <param name="element">The element, which may or may not be a room element.</param>
      /// <returns>True if the element is in the room, has a phase set, which is different from the active phase.</returns>
      public static bool IsRoomInInvalidPhase(Element element)
      {
         if (element is Room)
         {
            Parameter phaseParameter = element.get_Parameter(BuiltInParameter.ROOM_PHASE);
            if (phaseParameter != null)
            {
               ElementId phaseId = phaseParameter.AsElementId();
               if (phaseId != ElementId.InvalidElementId && phaseId != ExporterCacheManager.ExportOptionsCache.ActivePhaseId)
                  return true;
            }
         }

         return false;
      }

      /// <summary>
      /// Gets element filter that match certain phases. 
      /// </summary>
      /// <param name="document">The Revit document.</param>
      /// <returns>The element filter.</returns>
      private static ElementFilter GetPhaseStatusFilter(Document document)
      {
         ElementId phaseId = ExporterCacheManager.ExportOptionsCache.ActivePhaseId;

         List<ElementOnPhaseStatus> phaseStatuses = new List<ElementOnPhaseStatus>();
         phaseStatuses.Add(ElementOnPhaseStatus.None);  //include "none" because we might want to export phaseless elements.
         phaseStatuses.Add(ElementOnPhaseStatus.Existing);
         phaseStatuses.Add(ElementOnPhaseStatus.New);

         return new ElementPhaseStatusFilter(phaseId, phaseStatuses);
      }

      private static IDictionary<ElementId, bool> m_CategoryVisibilityCache = new Dictionary<ElementId, bool>();

      public static void InitCategoryVisibilityCache()
      {
         m_CategoryVisibilityCache.Clear();
      }

      /// <summary>
      /// Checks if a category is visible for certain view.
      /// </summary>
      /// <param name="category">The category.</param>
      /// <param name="filterView">The view.</param>
      /// <returns>True if the category is visible, false otherwise.</returns>
      public static bool IsCategoryVisible(Category category, View filterView)
      {
         // This routine is generally used to decide whether or not to export geometry assigned to a praticular category.
         // Default behavior is to return true, even for a null category.  In general, we want to err on the side of showing geometry over hiding it.
         if (category == null || filterView == null)
            return true;

         bool isVisible = false;
         if (m_CategoryVisibilityCache.TryGetValue(category.Id, out isVisible))
            return isVisible;

         // The category will be visible if either we don't allow visibility controls (default: true), or
         // we do allow visibility controls and the category is visible in the view.
         isVisible = (!category.get_AllowsVisibilityControl(filterView) || category.get_Visible(filterView));
         m_CategoryVisibilityCache[category.Id] = isVisible;
         return isVisible;
      }

      /// <summary>
      /// Checks if element is visible for certain view.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <returns>True if the element is visible, false otherwise.</returns>
      public static bool IsElementVisible(Element element)
      {
         View filterView = ExporterCacheManager.ExportOptionsCache.FilterViewForExport;
         if (filterView == null)
            return true;

         bool hidden = element.IsHidden(filterView);
         if (hidden)
            return false;

         Category category = element.Category;
         hidden = !IsCategoryVisible(category, filterView);
         if (hidden)
            return false;

         bool temporaryVisible = filterView.IsElementVisibleInTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate, element.Id);

         return temporaryVisible;
      }

      /// <summary>
      /// Checks if the IFC type is MEP type.
      /// </summary>
      /// <param name="exportType">IFC Export Type to check</param>
      /// <returns>True for MEP type of elements.</returns>
      public static bool IsMEPType(IFCExportInfoPair exportType)
      {
         bool instanceIsMEPInst = IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportInstance.ToString(), IFCEntityType.IfcDistributionElement.ToString(), strict:false);

         // The Type probably is not needed for check?
         bool typeIsMEPType = IfcSchemaEntityTree.IsSubTypeOf(ExporterCacheManager.ExportOptionsCache.FileVersion, exportType.ExportType.ToString(), IFCEntityType.IfcDistributionElementType.ToString(), strict:false);

         return (instanceIsMEPInst);
      }

      /// <summary>
      /// Check if an element assigned to IfcBuildingElementProxy is of MEP Type (by checking its connectors) to enable IfcBuildingElementProxy to take part
      /// in the System component and connectivity
      /// </summary>
      /// <param name="element">The element</param>
      /// <param name="exportType">IFC Export Type to check: only for IfcBuildingElementProxy or IfcBuildingElementProxyType</param>
      /// <returns></returns>
      public static bool ProxyForMEPType(Element element, IFCExportInfoPair exportType)
      {
         if ((exportType.ExportInstance == IFCEntityType.IfcBuildingElementProxy) || (exportType.ExportType == IFCEntityType.IfcBuildingElementProxyType))
         {
            try
            {
               if (element is FamilyInstance)
               {
                  MEPModel m = ((FamilyInstance)element).MEPModel;
                  if (m != null && m.ConnectorManager != null)
                  {
                     return true;
                  }
               }
               else
                  return false;
            }
            catch
            {
            }
         }

         return false;
      }

      public static IFCEntityType GetValidIFCEntityType (string entityType)
      {
         IFCEntityType ret = IFCEntityType.UnKnown;

         var ifcEntitySchemaTree = IfcSchemaEntityTree.GetEntityDictFor(ExporterCacheManager.ExportOptionsCache.FileVersion);
         if (ifcEntitySchemaTree == null || ifcEntitySchemaTree.Count == 0)
            throw new Exception("Unable to locate IFC Schema xsd file! Make sure the relevant xsd " + ExporterCacheManager.ExportOptionsCache.FileVersion + " exists.");

         IfcSchemaEntityNode node = IfcSchemaEntityTree.Find(entityType);
         if (node != null && !node.isAbstract)
         {
            IFCEntityType ifcType = IFCEntityType.UnKnown;
            // Only IfcProduct or IfcTypeProduct can be assigned for export type
            if (!node.IsSubTypeOf("IfcProduct") && !node.IsSubTypeOf("IfcTypeProduct"))
               ret = ifcType;
            else
               if (IFCEntityType.TryParse(entityType, out ifcType))
                  ret = ifcType;
         }

         return ret;
      }

      public static IFCEntityType GetValidIFCEntityType (IFCEntityType entityType)
      {
         return GetValidIFCEntityType(entityType.ToString());
      }
   }
}