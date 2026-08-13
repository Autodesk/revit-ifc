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

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Newtonsoft.Json.Linq;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using static Revit.IFC.Export.Utility.ParameterUtil;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Export.Toolkit;
using System;
using System.Collections.Generic;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Provides static methods for door and window related manipulations.
   /// </summary>
   class DoorWindowUtil
   {
      /// <summary>
      /// Gets the panel operation from door style operation.
      /// </summary>
      /// <param name="ifcDoorStyleOperationType">The IFCDoorStyleOperation.</param>
      /// <returns>The string represents the door panel operation.</returns>
      private static string GetPanelOperationFromDoorStyleOperation(string ifcDoorStyleOperationType)
      {
         const string baseValue = "NOTDEFINED";
         if (string.IsNullOrWhiteSpace(ifcDoorStyleOperationType))
            return baseValue;

         NamingUtil.IFCStringKey allCapsDoorStyleOperationType = new(ifcDoorStyleOperationType);
         if (allCapsDoorStyleOperationType.Contains("SINGLESWING"))
            return "SWINGING";

         if (allCapsDoorStyleOperationType.Contains("DOUBLESWING"))
            return "DOUBLE_ACTING";

         if (allCapsDoorStyleOperationType.Contains("SLIDING"))
            return "SLIDING";

         if (allCapsDoorStyleOperationType.Contains("FOLDING"))
            return "FOLDING";

         if (allCapsDoorStyleOperationType.Contains("REVOLVING"))
            return "REVOLVING";

         if (allCapsDoorStyleOperationType.Contains("ROLLINGUP"))
            return "ROLLINGUP";

         if (allCapsDoorStyleOperationType.Contains("USERDEFINED"))
            return "USERDEFINED";

         if (allCapsDoorStyleOperationType.Contains("FIXED"))
            return "FIXEDPANEL";

         return baseValue;
      }

      private static double? GetValueFromIndexedParameter(Element element, string baseParameterName, int index)
      {
         string parameterName = baseParameterName + index.ToString();
         double? value = GetPositiveDoubleValueFromElementOrSymbol(element, parameterName);
         if (value.HasValue)
            return value;

         // If the index is 1, we will try again with baseParameterName.
         if (index != 1)
            return null;

         return GetPositiveDoubleValueFromElementOrSymbol(element, baseParameterName);
      }

      /// <summary>
      /// Calculates PanelWidth as a normalised ratio (PanelWidth / Width) from the door type element.
      /// Returns null if either parameter is missing or Width is zero.
      /// </summary>
      private static double? CalculatePanelWidthFromDoorType(Element elem)
      {
         if (elem == null)
            return null;

         IList<double?> panelWidthValues = PropertyUtil.GetDoubleValuesFromParameterByType(elem, "PanelWidth", SpecTypeId.Length, PropertyValueType.SingleValue);
         IList<double?> widthValues = PropertyUtil.GetDoubleValuesFromParameterByType(elem, "Width", SpecTypeId.Length, PropertyValueType.SingleValue);

         double? panelWidth = panelWidthValues?.Count > 0 ? panelWidthValues[0] : null;
         double? width = widthValues?.Count > 0 ? widthValues[0] : null;

         if (!panelWidth.HasValue || !width.HasValue || MathUtil.IsAlmostZero(width.Value))
            return null;

         return panelWidth.Value / width.Value;
      }

      private class DoorPanelInformation
      {
         public double? Depth { get; private set; } = null;
         public double? Width { get; private set; } = null;
         public string Operation { get; private set; } = null;
         public string Position { get; private set; } = null;

         public DoorPanelInformation(double? depth, double? width, string operation, string position) 
         {
            Depth = depth;
            Width = width;
            Operation = operation;
            Position = position;
         }
      }

      /// <summary>
      /// Collects door panel information from the family instance parameters.
      /// </summary>
      private static IList<DoorPanelInformation> CollectDoorPanelInfo(
         DoorWindowInfo doorWindowInfo, Element familyInstance, Element familySymbol)
      {
         IList<DoorPanelInformation> doorPanelInfoList = new List<DoorPanelInformation>();

         const int maxPanels = 64;
         for (int panelNumber = 1; panelNumber < maxPanels; panelNumber++)
         {
            double? panelDepth = GetValueFromIndexedParameter(familyInstance, "PanelDepth", panelNumber);
            if (panelDepth == null && panelNumber > 1)
               break;

            double? panelWidth = (panelDepth != null) ?
               GetValueFromIndexedParameter(familyInstance, "PanelWidth", panelNumber) : null;
            if (panelWidth == null)
            {
               if (panelNumber > 1)
                  break;
               panelDepth = null;
            }

            bool breakAfterCreation = (panelDepth == null || panelWidth == null);
            if (!breakAfterCreation)
            {
               panelDepth = UnitUtil.ScaleLength(panelDepth.Value);
               panelWidth = (panelWidth.Value < 0.0) ? 0.0 : ((panelWidth.Value > 1.0) ? 1.0 : panelWidth);
            }

            string panelOperaton = GetPanelOperationFromDoorStyleOperation(doorWindowInfo?.DoorOperationTypeString);

            bool flippedX = doorWindowInfo?.FlippedX ?? false;
            bool flippedY = doorWindowInfo?.FlippedY ?? false;

            bool flip = flippedX ^ flippedY;
            string panelPosition = GetIFCDoorPanelPosition(familyInstance, panelNumber, flip);

            if (panelWidth == null && panelNumber == 1)
            {
               panelWidth = CalculatePanelWidthFromDoorType(familySymbol);
            }

            doorPanelInfoList.Add(new DoorPanelInformation(panelDepth, panelWidth, panelOperaton, panelPosition));

            if (breakAfterCreation)
               break;
         }

         return doorPanelInfoList;
      }

      /// <summary>
      /// Creates door panel properties to be attached to the door type.
      /// For IFC4.3+ multi-panel doors, returns empty — panels are decomposed at instance level
      /// via <see cref="CreateDoorPanelDecomposition"/>.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="doorWindowInfo">The DoorWindowInfo object.</param>
      /// <param name="familyInstance">The family instance of a door.</param>
      /// <param name="familySymbol">The type element.</param>
      /// <returns>The list of handles created.</returns>
      public static IList<IFCAnyHandle> CreateDoorPanelProperties(ExporterIFC exporterIFC,
         DoorWindowInfo doorWindowInfo, Element familyInstance, Element familySymbol)
      {
         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

         IList<IFCAnyHandle> doorPanels = new List<IFCAnyHandle>();

         IList<DoorPanelInformation> doorPanelInfoList = CollectDoorPanelInfo(doorWindowInfo, familyInstance, familySymbol);

         if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4x3)
         {
            // IFC4.3+ multi-panel: IfcPlate decomposition at instance level.
            // IFC4.3+ single-panel: cache properties for the centralized pset pass
            // so it can merge them into the IfcPropertySet it creates.
            if (doorPanelInfoList.Count == 1 && familySymbol != null)
            {
               DoorPanelInformation panelInfo = doorPanelInfoList[0];
               ExporterCacheManager.PreCreatedPsetProperties[("Pset_DoorPanelProperties", familySymbol.Id)] =
                  CreateDoorPanelPropertyHandles4x3(file, panelInfo.Depth, panelInfo.Operation, panelInfo.Width, panelInfo.Position);
            }
            return doorPanels;
         }

         string baseDoorPanelName = NamingUtil.GetIFCName(familyInstance);
         int panelNumber = 1;
         foreach (DoorPanelInformation doorPanelInfo in doorPanelInfoList)
         {
            string doorPanelName = baseDoorPanelName + ":" + panelNumber.ToString();
            string doorPanelGUID = GUIDUtil.CreateSubElementGUID(familyInstance, (int)IFCDoorSubElements.DoorPanelStart + panelNumber - 1);
            IFCAnyHandle doorPanel = IFCInstanceExporter.CreateDoorPanelProperties(file, doorPanelGUID, ownerHistory, doorPanelName, null,
               doorPanelInfo.Depth, doorPanelInfo.Operation, doorPanelInfo.Width, doorPanelInfo.Position, null);
            doorPanels.Add(doorPanel);
            panelNumber++;
         }

         return doorPanels;
      }

      /// <summary>
      /// Creates door lining properties.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="familyInstance">The family instance of a door.</param>
      /// <returns>The handle created.</returns>
      /// <remarks>This is deprecated in IFC4.3.</remarks>
      public static IFCAnyHandle CreateDoorLiningProperties(ExporterIFC exporterIFC, Element familyInstance)
      {
         if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4x3)
            return null;

         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

         double? liningDepthOpt = null;
         double? liningThicknessOpt = null;
         double? thresholdDepthOpt = null;
         double? thresholdThicknessOpt = null;
         double? transomThicknessOpt = null;
         double? transomOffsetOpt = null;
         double? liningOffsetOpt = null;
         double? thresholdOffsetOpt = null;
         double? casingThicknessOpt = null;
         double? casingDepthOpt = null;

         if ((GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "IfcDoorLiningProperties.LiningDepth", "LiningDepth") is double value1) && 
            (GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "IfcDoorLiningProperties.LiningThickness", "LiningThickness") is double value2))
         {
            // both of these must be defined, or not defined - if only one is defined, we ignore the values.
            liningDepthOpt = UnitUtil.ScaleLength(value1);
            liningThicknessOpt = UnitUtil.ScaleLength(value2);
         }

         (EvaluatedParameter parameter, value1) = GetDoubleValueFromElementOrSymbol(familyInstance, "IfcDoorLiningProperties.LiningOffset", 
            "LiningOffset");
         if (parameter != null)   
            liningOffsetOpt = UnitUtil.ScaleLength(value1);

         // both of these must be defined, or not defined - if only one is defined, we ignore the values.
         if ((GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "IfcDoorLiningProperties.ThresholdDepth", "ThresholdDepth") is double value3) &&
             (GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "IfcDoorLiningProperties.ThresholdThickness", "ThresholdThickness") is double value4))
         {
            thresholdDepthOpt = UnitUtil.ScaleLength(value3);
            thresholdThicknessOpt = UnitUtil.ScaleLength(value4);
         }

         (parameter, value1) = ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "IfcDoorLiningProperties.ThresholdOffset", "ThresholdOffset");
         if (parameter != null)
            thresholdOffsetOpt = UnitUtil.ScaleLength(value1);

         // both of these must be defined, or not defined - if only one is defined, we ignore the values.
         if ((GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "IfcDoorLiningProperties.TransomOffset", "TransomOffset") is double value5) &&
         (GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "IfcDoorLiningProperties.TransomThickness", "TransomThickness") is double value6))
         {
            transomOffsetOpt = UnitUtil.ScaleLength(value5);
            transomThicknessOpt = UnitUtil.ScaleLength(value6);
         }

         // both of these must be defined, or not defined - if only one is defined, we ignore the values.
         if ((GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "IfcDoorLiningProperties.CasingDepth", "CasingDepth") is double value7) &&
            (GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "IfcDoorLiningProperties.CasingThickness", "CasingThickness") is double value8))
         {
            casingDepthOpt = UnitUtil.ScaleLength(value7);
            casingThicknessOpt = UnitUtil.ScaleLength(value8);
         }

         string doorLiningGUID = GUIDUtil.CreateSubElementGUID(familyInstance, (int)IFCDoorSubElements.DoorLining);
         string doorLiningName = NamingUtil.GetIFCName(familyInstance);
         return IFCInstanceExporter.CreateDoorLiningProperties(file, doorLiningGUID, ownerHistory,
            doorLiningName, null, liningDepthOpt, liningThicknessOpt, thresholdDepthOpt, thresholdThicknessOpt,
            transomThicknessOpt, transomOffsetOpt, liningOffsetOpt, thresholdOffsetOpt, casingThicknessOpt,
            casingDepthOpt, null);
      }

      /// <summary>
      /// Gets door panel position.
      /// </summary>
      /// <param name="element">The door element.</param>
      /// <param name="number">The number of panel position.</param>
      /// <param name="flip">True if the position value should be reversed.</param>
      /// <returns>The string represents the door panel position.</returns>
      private static string GetIFCDoorPanelPosition(Element element, int number, bool flip)
      {
         const string basePanelName = "PanelPosition";
         string currPanelName = "PanelPosition" + number.ToString();

         string value = null;
         (_, value) = ParameterUtil.GetStringValueFromElementOrSymbol(element, null, false, "IfcDoorPanelProperties." + currPanelName, 
            currPanelName, "IfcDoorPanelProperties." + basePanelName, basePanelName);
         if (string.IsNullOrEmpty(value))
            return null;
         
         string cleanedValue = NamingUtil.RemoveSpacesAndUnderscores(value);
         string validatedValue = ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4x3 ?
            IFCValidateEntry.ValidateStrEnum<IFCDoorPanelPosition>(cleanedValue) :
            IFCValidateEntry.ValidateStrEnum<Exporter.PropertySet.IFC4X3.PEnum_DoorPanelPositionEnum>(cleanedValue);

         if (flip)
         {
            if (validatedValue == "LEFT")
               validatedValue = "RIGHT";
            else if (validatedValue == "RIGHT")
               validatedValue = "LEFT";
         }

         return validatedValue;
      }

      static readonly Dictionary<NamingUtil.IFCStringKey, IFCWindowStyleOperation> WindowStyles = new()
      {
         { new NamingUtil.IFCStringKey("DOUBLEPANELHORIZONTAL"), IFCWindowStyleOperation.Double_Panel_Horizontal },
         { new NamingUtil.IFCStringKey("DOUBLEPANELVERTICAL"), IFCWindowStyleOperation.Double_Panel_Vertical },
         { new NamingUtil.IFCStringKey("USERDEFINED"), IFCWindowStyleOperation.UserDefined },
         { new NamingUtil.IFCStringKey("SINGLEPANEL"), IFCWindowStyleOperation.Single_Panel },
         { new NamingUtil.IFCStringKey("TRIPLEPANELBOTTOM"), IFCWindowStyleOperation.Triple_Panel_Bottom },
         { new NamingUtil.IFCStringKey("TRIPLEPANELHORIZONTAL"), IFCWindowStyleOperation.Triple_Panel_Horizontal },
         { new NamingUtil.IFCStringKey("TRIPLEPANELLEFT"), IFCWindowStyleOperation.Triple_Panel_Left },
         { new NamingUtil.IFCStringKey("TRIPLEPANELRIGHT"), IFCWindowStyleOperation.Triple_Panel_Right },
         { new NamingUtil.IFCStringKey("TRIPLEPANELTOP"), IFCWindowStyleOperation.Triple_Panel_Top },
         { new NamingUtil.IFCStringKey("TRIPLEPANELVERTICAL"), IFCWindowStyleOperation.Triple_Panel_Vertical }
      };

      /// <summary>
      /// Gets window style operation.
      /// </summary>
      /// <param name="familySymbol">The element type of window.</param>
      /// <returns>The IFCWindowStyleOperation.</returns>
      public static IFCWindowStyleOperation GetIFCWindowStyleOperation(ElementType familySymbol)
      {
         (_, string value) = GetStringValueFromElement(familySymbol, BuiltInParameter.WINDOW_OPERATION_TYPE);

         if (string.IsNullOrEmpty(value))
            return IFCWindowStyleOperation.NotDefined;

         NamingUtil.IFCStringKey compValue = new(value);
         if (WindowStyles.TryGetValue(compValue, out IFCWindowStyleOperation operation))
            return operation;

         return IFCWindowStyleOperation.UserDefined;
      }

      public static IFCWindowStyleOperation ReverseWindowStyleOperation(Toolkit.IFCWindowStyleOperation operationType)
      {
         switch (operationType)
         {
            case IFCWindowStyleOperation.Triple_Panel_Left:
               return IFCWindowStyleOperation.Triple_Panel_Right;
            case IFCWindowStyleOperation.Triple_Panel_Right:
               return IFCWindowStyleOperation.Triple_Panel_Left;
            default:
               return operationType;
         }
      }

      public static string ReverseWindowPartitioningType(string partitioningType)
      {
         string compName = NamingUtil.RemoveSpacesAndUnderscores(partitioningType);
         if (string.Equals(compName, "TRIPLEPANELLEFT", StringComparison.InvariantCultureIgnoreCase))
            return "TRIPLE_PANEL_RIGHT";
         if (string.Equals(compName, "TRIPLEPANELRIGHT", StringComparison.InvariantCultureIgnoreCase))
            return "TRIPLE_PANEL_LEFT";
         return partitioningType;
      }

      static readonly Dictionary<NamingUtil.IFCStringKey, string> WindowPartitioningTypes = new()
      {
         { new NamingUtil.IFCStringKey("DOUBLEPANELHORIZONTAL"), "DOUBLE_PANEL_HORIZONTAL" },
         { new NamingUtil.IFCStringKey("DOUBLEPANELVERTICAL"), "DOUBLE_PANEL_VERTICAL" },
         { new NamingUtil.IFCStringKey("USERDEFINED"), "USERDEFINED" },
         { new NamingUtil.IFCStringKey("SINGLEPANEL"), "SINGLE_PANEL"},
         { new NamingUtil.IFCStringKey("TRIPLEPANELBOTTOM"), "TRIPLE_PANEL_BOTTOM" },
         { new NamingUtil.IFCStringKey("TRIPLEPANELHORIZONTAL"), "TRIPLE_PANEL_HORIZONTAL" },
         { new NamingUtil.IFCStringKey("TRIPLEPANELLEFT"), "TRIPLE_PANEL_LEFT" },
         { new NamingUtil.IFCStringKey("TRIPLEPANELRIGHT"), "TRIPLE_PANEL_RIGHT" },
         { new NamingUtil.IFCStringKey("TRIPLEPANELTOP"), "TRIPLE_PANEL_TOP" },
         { new NamingUtil.IFCStringKey("TRIPLEPANELVERTICAL"), "TRIPLE_PANEL_VERTICAL" }
      };

      /// <summary>
      /// New in IFC4: to get Partitioning type information from Window. In IFC2x3 is called Window Operation Type
      /// </summary>
      /// <param name="familySymbol"></param>
      /// <returns>The partitioning type information.</returns>
      public static string GetIFCWindowPartitioningType(ElementType familySymbol)
      {
         (_, string value) = GetStringValueFromElement(familySymbol, false, "WINDOW_PARTITIONING_TYPE");

         if (string.IsNullOrEmpty(value))
            return "NOTDEFINED";

         NamingUtil.IFCStringKey compValue = new(value);
         if (WindowPartitioningTypes.TryGetValue(compValue, out string type))
            return type;
         
         return "USERDEFINED";
      }

      static readonly Dictionary<NamingUtil.IFCStringKey, IFCDoorStyleConstruction> DoorStyleConstructions = new()
      {
         { new NamingUtil.IFCStringKey("ALUMINIUM"), IFCDoorStyleConstruction.Aluminium },
         { new NamingUtil.IFCStringKey("ALUMINIUMPLASTIC"), IFCDoorStyleConstruction.Aluminium_Plastic },
         { new NamingUtil.IFCStringKey("ALUMINIUMWOOD"), IFCDoorStyleConstruction.Aluminium_Wood },
         { new NamingUtil.IFCStringKey("USERDEFINED"), IFCDoorStyleConstruction.UserDefined },
         { new NamingUtil.IFCStringKey("HIGHGRADESTEEL"), IFCDoorStyleConstruction.High_Grade_Steel },
         { new NamingUtil.IFCStringKey("PLASTIC"), IFCDoorStyleConstruction.Plastic },
         { new NamingUtil.IFCStringKey("STEEL"), IFCDoorStyleConstruction.Steel },
         { new NamingUtil.IFCStringKey("WOOD"), IFCDoorStyleConstruction.Wood }
      };

      /// <summary>
      /// Gets IFCDoorStyleConstruction from construction type name.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <returns>The IFCDoorStyleConstruction.</returns>
      public static IFCDoorStyleConstruction GetDoorStyleConstruction(Element element)
      {
         (_, string value) = GetStringValueFromElementOrSymbol(element, null, false, "IfcDoorStyle.ConstructionType", "ConstructionType", "Construction");
         if (string.IsNullOrEmpty(value))
         {
            value = GetStringValueFromElementOrSymbol(element, null, false, BuiltInParameter.DOOR_CONSTRUCTION_TYPE);
            if (string.IsNullOrEmpty(value))
               return IFCDoorStyleConstruction.NotDefined;
         }

         NamingUtil.IFCStringKey compValue = new(value);
         if (DoorStyleConstructions.TryGetValue(compValue, out var result))
            return result;

         return IFCDoorStyleConstruction.UserDefined;
      }

      static readonly Dictionary<NamingUtil.IFCStringKey, IFCWindowStyleConstruction> WindowStyleConstructions = new()
      {
         { new NamingUtil.IFCStringKey("ALUMINIUM"), IFCWindowStyleConstruction.Aluminium },
         { new NamingUtil.IFCStringKey("ALUMINIUMWOOD"), IFCWindowStyleConstruction.Aluminium_Wood },
         { new NamingUtil.IFCStringKey("HIGHGRADESTEEL"), IFCWindowStyleConstruction.High_Grade_Steel },
         { new NamingUtil.IFCStringKey("PLASTIC"), IFCWindowStyleConstruction.Plastic },
         { new NamingUtil.IFCStringKey("STEEL"), IFCWindowStyleConstruction.Steel },
         { new NamingUtil.IFCStringKey("WOOD"), IFCWindowStyleConstruction.Wood }
      };

      /// <summary>
      /// Gets window style construction.
      /// </summary>
      /// <param name="element">The window element.</param>
      /// <returns>The string represents the window style construction.</returns>
      public static IFCWindowStyleConstruction GetIFCWindowStyleConstruction(Element element)
      {
         (_, string value) = GetStringValueFromElementOrSymbol(element, null, false, "IfcWindowStyle.ConstructionType", "ConstructionType", "Construction");
         if (string.IsNullOrEmpty(value))
         {
            value = GetStringValueFromElementOrSymbol(element, null, false, BuiltInParameter.WINDOW_CONSTRUCTION_TYPE);
            if (string.IsNullOrWhiteSpace(value))
               return IFCWindowStyleConstruction.NotDefined;
         }

         NamingUtil.IFCStringKey compValue = new(value);
         if (WindowStyleConstructions.TryGetValue(compValue, out IFCWindowStyleConstruction windowStyleConstruction))
            return windowStyleConstruction;

         return IFCWindowStyleConstruction.Other_Construction;
      }

      /// <summary>
      /// Gets window panel operation.
      /// </summary>
      /// <param name="initialValue">The initial value.</param>
      /// <param name="element">The window element.</param>
      /// <param name="number">The number of panel operation.</param>
      /// <returns>The string represents the window panel operation.</returns>
      public static string GetIFCWindowPanelOperation(string initialValue, Element element, int number)
      {
         string currPanelName = "PanelOperation" + number.ToString();

         (_, string value) = ParameterUtil.GetStringValueFromElementOrSymbol(element, null, false, "IfcWindowPanelProperties." + currPanelName, currPanelName);
         value ??= initialValue;

         string cleanedValue = NamingUtil.RemoveSpacesAndUnderscores(value);
         return ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4x3 ?
            IFCValidateEntry.ValidateStrEnum<IFCWindowPanelOperation>(cleanedValue) :
            IFCValidateEntry.ValidateStrEnum<Exporter.PropertySet.IFC4X3.PEnum_WindowPanelOperationEnum>(cleanedValue);
      }

      /// <summary>
      /// Gets window panel position.
      /// </summary>
      /// <param name="initialValue">The initial value.</param>
      /// <param name="element">The window element.</param>
      /// <param name="number">The number of panel position.</param>
      /// <returns>The string represents the window panel position, or null if unset.</returns>
      public static string GetIFCWindowPanelPosition(string initialValue, Element element, int number)
      {
         string currPanelName = "PanelPosition" + number.ToString();

         (_, string value) = ParameterUtil.GetStringValueFromElementOrSymbol(element, null, false, "IfcWindowPanelProperties." + currPanelName, currPanelName);
         value ??= initialValue;

         return ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4x3 ?
            IFCValidateEntry.ValidateStrEnum<IFCWindowPanelPosition>(value) :
            IFCValidateEntry.ValidateStrEnum<Exporter.PropertySet.IFC4X3.PEnum_WindowPanelPositionEnum>(value);
      }

      /// <summary>
      /// Creates window panel position.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="familyInstance">The family instance of a window.</param>
      /// <param name="description">The description.</param>
      /// <returns>The handle created.</returns>
      /// <remarks>This is deprecated in IFC4.3</remarks>
      public static IFCAnyHandle CreateWindowLiningProperties(ExporterIFC exporterIFC,
         Element familyInstance, string description)
      {
         if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4x3)
            return null;

         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

         double? liningDepthOpt = null;
         double? liningThicknessOpt = null;
         double? transomThicknessOpt = null;
         double? mullionThicknessOpt = null;
         double? firstTransomOffsetOpt = null;
         double? secondTransomOffsetOpt = null;
         double? firstMullionOffsetOpt = null;
         double? secondMullionOffsetOpt = null;

         // both of these must be defined (or not defined)
         if ((TryGetDoubleValueFromElementOrSymbol(familyInstance, "IfcWindowLiningProperties.LiningDepth", "LiningDepth") is double value1) &&
             (TryGetDoubleValueFromElementOrSymbol(familyInstance, "IfcWindowLiningProperties.LiningThickness", "LiningThickness") is double value2))
         {
            liningDepthOpt = UnitUtil.ScaleLength(value1);
            liningThicknessOpt = UnitUtil.ScaleLength(value2);
         }

         if (TryGetDoubleValueFromElementOrSymbol(familyInstance, "IfcWindowLiningProperties.TransomThickness", "TransomThickness") is double value3)
            transomThicknessOpt = UnitUtil.ScaleLength(value3);

         if (TryGetDoubleValueFromElementOrSymbol(familyInstance, "IfcWindowLiningProperties.FirstTransomOffset", "FirstTransomOffset") is double value4)
            firstTransomOffsetOpt = value4;

         if (TryGetDoubleValueFromElementOrSymbol(familyInstance, "IfcWindowLiningProperties.SecondTransomOffset", "SecondTransomOffset") is double value5)
            secondTransomOffsetOpt = value5;

         if (TryGetDoubleValueFromElementOrSymbol(familyInstance, "IfcWindowLiningProperties.MullionThickness", "MullionThickness") is double value6)
            mullionThicknessOpt = UnitUtil.ScaleLength(value6);

         if (TryGetDoubleValueFromElementOrSymbol(familyInstance, "IfcWindowLiningProperties.FirstMullionOffset", "FirstMullionOffset") is double value7)
            firstMullionOffsetOpt = value7;

         if (TryGetDoubleValueFromElementOrSymbol(familyInstance, "IfcWindowLiningProperties.SecondMullionOffset", "SecondMullionOffset") is double value8)
            secondMullionOffsetOpt = value8;

         string windowLiningGUID = GUIDUtil.CreateSubElementGUID(familyInstance, (int)IFCWindowSubElements.WindowLining);
         string windowLiningName = NamingUtil.GetIFCName(familyInstance);
         return IFCInstanceExporter.CreateWindowLiningProperties(file, windowLiningGUID, ownerHistory,
            windowLiningName, description, liningDepthOpt, liningThicknessOpt, transomThicknessOpt, mullionThicknessOpt,
            firstTransomOffsetOpt, secondTransomOffsetOpt, firstMullionOffsetOpt, secondMullionOffsetOpt, null);
      }

      private static Dictionary<string, IFCAnyHandle> CreateWindowPanelPropertyHandles4x3(IFCFile file,
         string panelOperation, string panelPosition, double? frameDepth, double? frameThickness)
      {
         Dictionary<string, IFCAnyHandle> props = [];
         props["OperationType"] = IFCInstanceExporter.CreatePropertyEnumeratedValue(file,
            new("OperationType"), [IFCData.CreateEnumeration(panelOperation ?? "UNSET")], null);

         props["PanelPosition"] = IFCInstanceExporter.CreatePropertyEnumeratedValue(file,
            new("PanelPosition"), [IFCData.CreateEnumeration(panelPosition ?? "UNSET")], null);

         if (frameDepth.HasValue)
         {
            IFCData frameDepthData = IFCDataUtil.CreateAsPositiveLengthMeasure(frameDepth.Value);
            if (frameDepthData != null)
               props["FrameDepth"] = IFCInstanceExporter.CreatePropertySingleValue(file, new("FrameDepth"), frameDepthData, null);
         }

         if (frameThickness.HasValue)
         {
            IFCData frameThicknessData = IFCDataUtil.CreateAsPositiveLengthMeasure(frameThickness.Value);
            if (frameThicknessData != null)
               props["FrameThickness"] = IFCInstanceExporter.CreatePropertySingleValue(file, new("FrameThickness"), frameThicknessData, null);
         }

         return props;
      }

      private static IFCAnyHandle CreatePsetWindowPanelProperties4x3(IFCFile file, string panelGUID, IFCAnyHandle ownerHistory, 
         string description, string panelOperation, string panelPosition, double? frameDepth, 
         double? frameThickness)
      {
         Dictionary<string, IFCAnyHandle> props = CreateWindowPanelPropertyHandles4x3(file,
            panelOperation, panelPosition, frameDepth, frameThickness);
         return IFCInstanceExporter.CreatePropertySet(file, panelGUID, ownerHistory, "Pset_WindowPanelProperties", description,
            new HashSet<IFCAnyHandle>(props.Values));
      }

      private static Dictionary<string, IFCAnyHandle> CreateDoorPanelPropertyHandles4x3(IFCFile file,
         double? panelDepth, string panelOperation, double? panelWidth, string panelPosition)
      {
         Dictionary<string, IFCAnyHandle> props = [];
         if (panelDepth.HasValue)
         {
            IFCData panelDepthData = IFCDataUtil.CreateAsPositiveLengthMeasure(panelDepth.Value);
            if (panelDepthData != null)
               props["PanelDepth"] = IFCInstanceExporter.CreatePropertySingleValue(file, new("PanelDepth"), panelDepthData, null);
         }

         props["PanelOperation"] = IFCInstanceExporter.CreatePropertyEnumeratedValue(file,
            new("PanelOperation"), [IFCDataUtil.CreateAsLabel(panelOperation ?? "UNSET")], null);

         if (panelWidth.HasValue)
         {
            IFCData panelWidthData = IFCDataUtil.CreateAsNormalisedRatioMeasure(panelWidth.Value);
            if (panelWidthData != null)
               props["PanelWidth"] = IFCInstanceExporter.CreatePropertySingleValue(file, new("PanelWidth"), panelWidthData, null);
         }

         props["PanelPosition"] = IFCInstanceExporter.CreatePropertyEnumeratedValue(file,
            new("PanelPosition"), [IFCDataUtil.CreateAsLabel(panelPosition ?? "UNSET")], null);

         return props;
      }

      private static IFCAnyHandle CreatePsetDoorPanelProperties4x3(IFCFile file, string panelGUID, IFCAnyHandle ownerHistory,
         string description, double? panelDepth, string panelOperation, double? panelWidth, string panelPosition)
      {
         Dictionary<string, IFCAnyHandle> props = CreateDoorPanelPropertyHandles4x3(file,
            panelDepth, panelOperation, panelWidth, panelPosition);
         return IFCInstanceExporter.CreatePropertySet(file, panelGUID, ownerHistory, "Pset_DoorPanelProperties", description,
            new HashSet<IFCAnyHandle>(props.Values));
      }

      /// <summary>
      /// For IFC4.3+ multi-panel doors, creates property-only IfcPlate children aggregated under
      /// the door instance via IfcRelAggregates. Each plate carries its own Pset_DoorPanelProperties,
      /// avoiding the IfcTypeObject.UniquePropertySetNames violation that would occur if multiple
      /// same-named psets were placed on the door type.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="doorWindowInfo">The DoorWindowInfo object.</param>
      /// <param name="familyInstance">The family instance of a door.</param>
      /// <param name="familySymbol">The type element.</param>
      /// <param name="doorInstanceHandle">The IfcDoor instance handle to aggregate plates under.</param>
      public static void CreateDoorPanelDecomposition(ExporterIFC exporterIFC,
         DoorWindowInfo doorWindowInfo, Element familyInstance, Element familySymbol,
         IFCAnyHandle doorInstanceHandle)
      {
         if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4x3)
            return;

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(doorInstanceHandle))
            return;

         IList<DoorPanelInformation> doorPanelInfoList = CollectDoorPanelInfo(doorWindowInfo, familyInstance, familySymbol);
         if (doorPanelInfoList.Count <= 1)
            return;

         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
         HashSet<IFCAnyHandle> plateHandles = [];

         string baseName = NamingUtil.GetIFCName(familyInstance);
         int panelNumber = 1;
         foreach (DoorPanelInformation panelInfo in doorPanelInfoList)
         {
            string plateGUID = GUIDUtil.CreateSubElementGUID(familyInstance,
               (int)IFCDoorSubElements.DoorPanelStart + panelNumber - 1);

            IFCAnyHandle plateHandle = IFCInstanceExporter.CreatePlate(file, null, null,
               plateGUID, ownerHistory, null, null, "USERDEFINED");
            IFCAnyHandleUtil.OverrideNameAttribute(plateHandle, baseName + ":Panel:" + panelNumber);
            IFCAnyHandleUtil.SetAttribute(plateHandle, "ObjectType", "DOOR_PANEL");

            string psetGUID = GUIDUtil.GenerateIFCGuidFrom(
               GUIDUtil.CreateGUIDString(IFCEntityType.IfcPropertySet, "Pset_DoorPanelProperties", plateHandle));
            IFCAnyHandle psetHandle = CreatePsetDoorPanelProperties4x3(file, psetGUID, ownerHistory,
               null, panelInfo.Depth, panelInfo.Operation, panelInfo.Width, panelInfo.Position);

            ExporterUtil.CreateRelDefinesByProperties(file, ownerHistory, null, null,
               new HashSet<IFCAnyHandle> { plateHandle }, psetHandle);

            plateHandles.Add(plateHandle);
            panelNumber++;
         }

         if (plateHandles.Count > 0)
         {
            string relGuid = GUIDUtil.GenerateIFCGuidFrom(
               GUIDUtil.CreateGUIDString(IFCEntityType.IfcRelAggregates, doorInstanceHandle));
            IFCInstanceExporter.CreateRelAggregates(file, relGuid, ownerHistory, null, null,
               doorInstanceHandle, plateHandles);
         }
      }

      private class WindowPanelInformation
      {
         public string Operation { get; private set; }
         public string Position { get; private set; }
         public double? FrameDepth { get; private set; }
         public double? FrameThickness { get; private set; }

         public WindowPanelInformation(string operation, string position, double? frameDepth, double? frameThickness)
         {
            Operation = operation;
            Position = position;
            FrameDepth = frameDepth;
            FrameThickness = frameThickness;
         }
      }

      /// <summary>
      /// Collects window panel information from the family instance parameters.
      /// </summary>
      private static IList<WindowPanelInformation> CollectWindowPanelInfo(Element familyInstance)
      {
         IList<WindowPanelInformation> windowPanelInfoList = new List<WindowPanelInformation>();

         const int maxPanels = 1000;
         for (int panelNumber = 1; panelNumber < maxPanels; panelNumber++)
         {
            string panelOperation = GetIFCWindowPanelOperation("", familyInstance, panelNumber);
            string panelPosition = GetIFCWindowPanelPosition("", familyInstance, panelNumber);
            if (panelOperation == null && panelPosition == null)
               break;

            double? frameDepth = null;
            double? frameThickness = null;

            string frameDepthCurrString = "FrameDepth" + panelNumber.ToString();
            string frameThicknessCurrString = "FrameThickness" + panelNumber.ToString();

            if (panelNumber == 1)
            {
               if ((TryGetDoubleValueFromElementOrSymbol(familyInstance, "IfcWindowPanelProperties." + frameDepthCurrString,
                  frameDepthCurrString, "IfcWindowPanelProperties.FrameDepth", "FrameDepth") is double value1) &&
                  (TryGetDoubleValueFromElementOrSymbol(familyInstance, "IfcWindowPanelProperties." + frameThicknessCurrString,
                  frameThicknessCurrString, "IfcWindowPanelProperties.FrameThickness", "FrameThickness") is double value2))
               {
                  frameDepth = UnitUtil.ScaleLength(value1);
                  frameThickness = UnitUtil.ScaleLength(value2);
               }
            }
            else
            {
               if ((TryGetDoubleValueFromElementOrSymbol(familyInstance, "IfcWindowPanelProperties." + frameDepthCurrString, 
                  frameDepthCurrString) is double value1) &&
                  (TryGetDoubleValueFromElementOrSymbol(familyInstance, "IfcWindowPanelProperties." + frameThicknessCurrString,
                  frameThicknessCurrString) is double value2))
               {
                  frameDepth = UnitUtil.ScaleLength(value1);
                  frameThickness = UnitUtil.ScaleLength(value2);
               }
            }
      
            windowPanelInfoList.Add(new WindowPanelInformation(panelOperation, panelPosition, frameDepth, frameThickness));
         }

         return windowPanelInfoList;
      }

      /// <summary>
      /// Creates window panel properties to be attached to the window type.
      /// For IFC4.3+ multi-panel windows, returns empty — panels are decomposed at instance level
      /// via <see cref="CreateWindowPanelDecomposition"/>.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="familyInstance">The family instance of a window.</param>
      /// <param name="description">The description.</param>
      /// <param name="familySymbol">The type element.</param>
      /// <returns>The list of handles created.</returns>
      public static IList<IFCAnyHandle> CreateWindowPanelProperties(ExporterIFC exporterIFC,
         Element familyInstance, string description, Element familySymbol)
      {
         IList<IFCAnyHandle> panels = new List<IFCAnyHandle>();
         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

         IList<WindowPanelInformation> windowPanelInfoList = CollectWindowPanelInfo(familyInstance);

         if (!ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4x3)
         {
            // IFC4.3+ multi-panel: IfcPlate decomposition at instance level.
            // IFC4.3+ single-panel: cache properties for the centralized pset pass.
            if (windowPanelInfoList.Count == 1 && familySymbol != null)
            {
               WindowPanelInformation panelInfo = windowPanelInfoList[0];
               ExporterCacheManager.PreCreatedPsetProperties[("Pset_WindowPanelProperties", familySymbol.Id)] =
                  CreateWindowPanelPropertyHandles4x3(file, panelInfo.Operation, panelInfo.Position,
                     panelInfo.FrameDepth, panelInfo.FrameThickness);
            }
            return panels;
         }

         int panelNumber = 1;
         foreach (WindowPanelInformation panelInfo in windowPanelInfoList)
         {
            string panelGUID = GUIDUtil.CreateSubElementGUID(familyInstance, (int)IFCWindowSubElements.WindowPanelStart + panelNumber);
            string panelName = NamingUtil.GetIFCNamePlusIndex(familyInstance, panelNumber);
            IFCAnyHandle psetHandle = IFCInstanceExporter.CreateWindowPanelProperties(file, panelGUID, ownerHistory,
               panelName, description, panelInfo.Operation, panelInfo.Position, panelInfo.FrameDepth, panelInfo.FrameThickness, null);
            panels.Add(psetHandle);
            panelNumber++;
         }
         return panels;
      }

      /// <summary>
      /// For IFC4.3+ multi-panel windows, creates property-only IfcPlate children aggregated under
      /// the window instance via IfcRelAggregates. Each plate carries its own Pset_WindowPanelProperties.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="familyInstance">The family instance of a window.</param>
      /// <param name="windowInstanceHandle">The IfcWindow instance handle to aggregate plates under.</param>
      public static void CreateWindowPanelDecomposition(ExporterIFC exporterIFC,
         Element familyInstance, IFCAnyHandle windowInstanceHandle)
      {
         if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4x3)
            return;

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(windowInstanceHandle))
            return;

         IList<WindowPanelInformation> windowPanelInfoList = CollectWindowPanelInfo(familyInstance);
         if (windowPanelInfoList.Count <= 1)
            return;

         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
         HashSet<IFCAnyHandle> plateHandles = [];

         string baseName = NamingUtil.GetIFCName(familyInstance);
         int panelNumber = 1;
         foreach (WindowPanelInformation panelInfo in windowPanelInfoList)
         {
            string plateGUID = GUIDUtil.CreateSubElementGUID(familyInstance,
               (int)IFCWindowSubElements.WindowPanelStart + panelNumber);

            IFCAnyHandle plateHandle = IFCInstanceExporter.CreatePlate(file, null, null,
               plateGUID, ownerHistory, null, null, "USERDEFINED");
            IFCAnyHandleUtil.OverrideNameAttribute(plateHandle, baseName + ":Panel:" + panelNumber);
            IFCAnyHandleUtil.SetAttribute(plateHandle, "ObjectType", "WINDOW_PANEL");

            string psetGUID = GUIDUtil.GenerateIFCGuidFrom(
               GUIDUtil.CreateGUIDString(IFCEntityType.IfcPropertySet, "Pset_WindowPanelProperties", plateHandle));
            IFCAnyHandle psetHandle = CreatePsetWindowPanelProperties4x3(file, psetGUID, ownerHistory,
               null, panelInfo.Operation, panelInfo.Position, panelInfo.FrameDepth, panelInfo.FrameThickness);

            ExporterUtil.CreateRelDefinesByProperties(file, ownerHistory, null, null,
               new HashSet<IFCAnyHandle> { plateHandle }, psetHandle);

            plateHandles.Add(plateHandle);
            panelNumber++;
         }

         if (plateHandles.Count > 0)
         {
            string relGuid = GUIDUtil.GenerateIFCGuidFrom(
               GUIDUtil.CreateGUIDString(IFCEntityType.IfcRelAggregates, windowInstanceHandle));
            IFCInstanceExporter.CreateRelAggregates(file, relGuid, ownerHistory, null, null,
               windowInstanceHandle, plateHandles);
         }
      }

      /// <summary>
      /// Access the HostObjects map to get the handle associated with a wall at a particular level.  This does something special only 
      /// for walls split by level.
      /// </summary>
      /// <param name="exporterIFC">The exporterIFC class.</param>
      /// <param name="hostId">The (wall) host id.</param>
      /// <param name="levelId">The level id.</param>
      /// <returns>The IFC handle associated with the host at that level.</returns>
      static public IFCAnyHandle GetHndForHostAndLevel(ExporterIFC exporterIFC, ElementId hostId, ElementId levelId)
      {
         if (MathUtil.IsInvalidElementId(hostId))
            return null;

         IFCAnyHandle hostObjectHnd = null;

         IList<IDictionary<ElementId, IFCAnyHandle>> hostObjects = exporterIFC.GetHostObjects();
         int idx = -1;
         if (ExporterCacheManager.HostObjectsLevelIndex.TryGetValue(levelId, out idx))
         {
            IDictionary<ElementId, IFCAnyHandle> mapForLevel = hostObjects[idx];
            mapForLevel.TryGetValue(hostId, out hostObjectHnd);
         }

         // If we can't find a specific handle for the host on that level, look for a generic handle for the host.
         // These are stored in the "invalidElementId" level id map.
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(hostObjectHnd))
         {
            if (ExporterCacheManager.HostObjectsLevelIndex.TryGetValue(ElementId.InvalidElementId, out idx))
            {
               IDictionary<ElementId, IFCAnyHandle> mapForLevel = hostObjects[idx];
               mapForLevel.TryGetValue(hostId, out hostObjectHnd);
            }
         }

         return hostObjectHnd;
      }

      private static void ComputeArcBoundingBox(Arc arc, IList<XYZ> pts, double startParam, double endParam)
      {
         XYZ point = arc.Evaluate(startParam, false);
         XYZ otherPoint = arc.Evaluate(endParam, false);

         double eps = MathUtil.Eps;
         XYZ maximum = new XYZ(Math.Max(point[0], otherPoint[0]),
             Math.Max(point[1], otherPoint[1]),
             Math.Max(point[2], otherPoint[2]));
         XYZ minimum = new XYZ(Math.Min(point[0], otherPoint[0]),
             Math.Min(point[1], otherPoint[1]),
             Math.Min(point[2], otherPoint[2]));

         if (endParam < startParam + eps)
            return;

         // find mins and maxs along each axis
         for (int aa = 0; aa < 3; aa++)    // aa is the axis index
         {
            XYZ axis = new XYZ((aa == 0) ? 1 : 0, (aa == 1) ? 1 : 0, (aa == 2) ? 1 : 0);
            double xProj = arc.XDirection.DotProduct(axis);
            double yProj = arc.YDirection.DotProduct(axis);
            if (Math.Abs(xProj) < eps && Math.Abs(yProj) < eps)
               continue;

            double angle = Math.Atan2(yProj, xProj);

            if (angle > startParam)
               angle -= Math.PI * ((int)((angle - startParam) / Math.PI));
            else
               angle += Math.PI * (1 + ((int)((startParam - angle) / Math.PI)));

            for (; angle < endParam; angle += Math.PI)
            {
               point = arc.Evaluate(angle, false);
               maximum = new XYZ(Math.Max(point[0], maximum[0]),
                   Math.Max(point[1], maximum[1]),
                   Math.Max(point[2], maximum[2]));
               minimum = new XYZ(Math.Min(point[0], minimum[0]),
                   Math.Min(point[1], minimum[1]),
                   Math.Min(point[2], minimum[2]));
            }
         }

         pts.Add(minimum);
         pts.Add(maximum);
      }

      private static void ComputeArcBoundingBox(Arc arc, IList<XYZ> pts)
      {
         if (arc == null)
            return;

         if (arc.IsBound)
         {
            ComputeArcBoundingBox(arc, pts, arc.GetEndParameter(0), arc.GetEndParameter(1));
         }
         else
         {
            ComputeArcBoundingBox(arc, pts, 0.0, Math.PI);
            ComputeArcBoundingBox(arc, pts, Math.PI, 2.0 * Math.PI);
         }
      }

      private static BoundingBoxXYZ ComputeApproximateCurveLoopBBoxForOpening(CurveLoop curveLoop, Transform trf)
      {
         Transform trfInv = (trf != null) ? trf.Inverse : null;

         XYZ ll = null;
         XYZ ur = null;

         bool init = false;
         foreach (Curve curve in curveLoop)
         {
            IList<XYZ> pts = new List<XYZ>();
            if (curve is Line)
            {
               pts.Add(curve.GetEndPoint(0));
               pts.Add(curve.GetEndPoint(1));
            }
            else if (curve is Arc)
            {
               ComputeArcBoundingBox(curve as Arc, pts);
            }
            else
               pts = curve.Tessellate();

            foreach (XYZ pt in pts)
            {
               XYZ ptToUse = (trf != null) ? trfInv.OfPoint(pt) : pt;
               if (!init)
               {
                  ll = ptToUse;
                  ur = ptToUse;
                  init = true;
               }
               else
               {
                  ll = new XYZ(Math.Min(ll.X, ptToUse.X), Math.Min(ll.Y, ptToUse.Y), Math.Min(ll.Z, ptToUse.Z));
                  ur = new XYZ(Math.Max(ur.X, ptToUse.X), Math.Max(ur.Y, ptToUse.Y), Math.Max(ur.Z, ptToUse.Z));
               }
            }
         }

         if (!init)
            return null;

         if (trf != null)
         {
            ll = trf.OfPoint(ll);
            ur = trf.OfPoint(ur);
         }

         BoundingBoxXYZ curveLoopBounds = new BoundingBoxXYZ();
         curveLoopBounds.set_Bounds(0, ll);
         curveLoopBounds.set_Bounds(1, ur);
         return curveLoopBounds;
      }

      /// <summary>
      /// Create the opening associated to an already created door or window.
      /// </summary>
      /// <param name="exporterIFC">The exporter class.</param>
      /// <param name="doc">The document.</param>
      /// <param name="hostObjHnd">The host object IFC handle.</param>
      /// <param name="hostId">The host object element id.</param>
      /// <param name="insertId">The insert element id.</param>
      /// <param name="openingGUID">The GUID for the IfcOpeningElement.</param>
      /// <param name="cutLoop">The 2D outline representing the opening geometry.</param>
      /// <param name="cutDir">The direction of the extrusion representing the opening geometry.</param>
      /// <param name="origUnscaledDepth">The width of the host object that the opening is cutting.</param>
      /// <param name="posHingeSide">True if the 2D outline is on the plane containing the hinge.</param>
      /// <param name="isRecess">True if the IfcOpeningElement should represent a recess.</param>
      /// <returns>The class containing information about the opening.</returns>
      static public DoorWindowOpeningInfo CreateOpeningForDoorWindow(ExporterIFC exporterIFC, Document doc,
          IFCAnyHandle hostObjHnd, ElementId hostId, ElementId insertId, string openingGUID, CurveLoop cutLoop, XYZ cutDir,
          double origUnscaledDepth, bool posHingeSide, bool isRecess)
      {
         double openingHeight = 0.0, openingWidth = 0.0;
         Transform loopLcs = cutLoop.HasPlane() ? GeometryUtil.CreateTransformFromPlane(cutLoop.GetPlane()) : null;
         BoundingBoxXYZ cutLoopBBox = ComputeApproximateCurveLoopBBoxForOpening(cutLoop, loopLcs);
         if (cutLoopBBox != null)
         {
            XYZ dist = cutLoopBBox.Max - cutLoopBBox.Min;
            openingHeight = Math.Abs(dist.Z);
            openingWidth = Math.Sqrt(dist.X * dist.X + dist.Y * dist.Y);
         }

         Element wallElement = doc.GetElement(hostId);
         Wall wall = (wallElement != null) ? wallElement as Wall : null;
         Curve curve = WallExporter.GetWallAxis(wall);
         if (curve == null)
            return null;

         // Don't export opening if we are exporting parts on a wall, as the parts will already have the openings cut out.
         if (PartExporter.CanExportParts(wall))
            return null;

         Element doorWindowElement = doc.GetElement(insertId);

         double? optWallSlantAngle = ExporterCacheManager.WallCrossSectionCache.GetUniformSlantAngle(wall);
         bool wallIsVertical = (optWallSlantAngle != null) && MathUtil.IsAlmostZero(optWallSlantAngle.Value);
         Parameter insertOrientation = doorWindowElement.get_Parameter(BuiltInParameter.INSERT_ORIENTATION);
         bool insertIsVertical = (insertOrientation != null && insertOrientation.HasValue && insertOrientation.StorageType == StorageType.Integer && insertOrientation.AsInteger() == 0 /*vertical orientation*/);

         ElementId catId = CategoryUtil.GetSafeCategoryId(wall);

         double unScaledDepth = origUnscaledDepth;

         IFCAnyHandle hostObjPlacementHnd = IFCAnyHandleUtil.GetObjectPlacement(hostObjHnd);
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

         XYZ relOrig = XYZ.Zero;
         XYZ relZ = XYZ.BasisZ;
         XYZ relX = XYZ.BasisX;

         Transform openingTrf = ExporterIFCUtils.GetUnscaledTransform(exporterIFC, hostObjPlacementHnd);
         openingTrf = openingTrf.Inverse;

         if (RepresentationUtil.DocumentMirrorState.IsExportingMirroredLink())
         {
            Transform mirrorTrf = FederatedLinkManager.MirrorTransform;
            if (mirrorTrf != null)
               openingTrf = openingTrf.Multiply(mirrorTrf);
         }

         // Create a copy of the opening loop that will be expressed in the local coordinate system relative to the wall
         CurveLoop tmpCutLoop = GeometryUtil.TransformCurveLoop(cutLoop, openingTrf);
         loopLcs = openingTrf.Multiply(loopLcs);
         cutDir = openingTrf.OfVector(cutDir);

         if (curve is Line)
         {
            // TODO: Check this code for inserts in tapered walls.
            if (wallIsVertical == insertIsVertical) // For vertical inserts in vertical walls and slanted inserts in slanted walls
            {
               XYZ wallExtrusionDir = WallExporter.GetWallExtrusionDirection(wall) ?? XYZ.BasisZ;

               // Create a plane that goes through the center of the wall along its length
               XYZ localExtrusionDir = openingTrf.OfVector(wallExtrusionDir);
               Transform curveData = curve.ComputeDerivatives(curve.GetEndParameter(0), false);
               if (curveData.BasisX.IsZeroLength())
                  return null;

               curveData = openingTrf.Multiply(curveData);
               Plane wallCenterPlane = Plane.CreateByOriginAndBasis(curveData.Origin, curveData.BasisX.Normalize(), localExtrusionDir.Normalize());
               // Calculate a center wall point relative to the origin of the opening loop
               wallCenterPlane.Project(loopLcs.Origin, out UV uv, out _);

               // Revit API doesn't seem to provide a plane evaluation method, so calculating the point by hand here
               XYZ wallCenterPoint = wallCenterPlane.Origin + wallCenterPlane.XVec * uv.U + wallCenterPlane.YVec * uv.V;

               // Place the opening loop on the proper side related to insert's hinge
               // This is not applicable to vertical inserts in slanted walls, since they won't cut 
               // the wall if cutout loop is placed on the side of the insert closer to the wall.
               double desiredLoopOffset = posHingeSide ? (-unScaledDepth / 2.0) : (unScaledDepth / 2.0);
               XYZ localY = localExtrusionDir.CrossProduct(curveData.BasisX).Normalize();
               XYZ desiredPosition = wallCenterPoint + localY * desiredLoopOffset;
               if (!loopLcs.Origin.IsAlmostEqualTo(desiredPosition))
               {
                  XYZ moveVec = desiredPosition - loopLcs.Origin;
                  tmpCutLoop = GeometryUtil.MoveCurveLoop(tmpCutLoop, moveVec);
               }

               bool cutDirRelToHostObjY = (cutDir[1] > 0.0); // true = same sense, false = opp. sense
               if (posHingeSide != cutDirRelToHostObjY)
               {
                  cutDir = cutDir.Negate();
               }

               loopLcs.BasisX = localExtrusionDir;
            }
            else // For vertical inserts in slanted walls
            {
               if (wallIsVertical && insertIsVertical)
                  return null; // This shouldn't be possible

               // TODO: Is this right for tapered walls?
               double slantAngle = optWallSlantAngle.GetValueOrDefault(0.0);

               // Handle cases where cut direction is looking away from the wall
               // Positive Y coordinate in cutDir means it's looking away from the positive slant direction
               if ((cutDir[1] > 0.0) != (slantAngle < 0.0))
               {
                  // Move the cut loop forward to make sure that the width of the opening will also be cut out
                  XYZ moveVec = cutDir * unScaledDepth;
                  tmpCutLoop = GeometryUtil.MoveCurveLoop(tmpCutLoop, moveVec);
                  // Flip the cut direction so that the cut would intersect the wall
                  cutDir = cutDir.Negate();
               }

               // Calculate the distance from the top of the insert to the wall
               double distToWall = openingHeight * Math.Tan(Math.Abs(slantAngle));
               // Add wall's width to make sure the cut reaches its opposite side
               unScaledDepth = distToWall + wall.Width;

               loopLcs.BasisX = XYZ.BasisZ;
            }

            // In IFC the local X direction should point upwards along the wall, 
            // and local Y direction should point horizontally along the wall.
            loopLcs.BasisY = cutDir.CrossProduct(loopLcs.BasisX).Normalize();
            loopLcs.BasisZ = cutDir;
         }
         else if (curve is Arc)
         {
            Arc arc = curve as Arc;
            double radius = arc.Radius;
         
            XYZ curveCtr = arc.Center;

            // check orientation to cutDir, make sure it points to center of arc.
            XYZ origLL = new XYZ(cutLoopBBox.Min.X, cutLoopBBox.Min.Y, curveCtr.Z);
            XYZ origUR = new XYZ(cutLoopBBox.Max.X, cutLoopBBox.Max.Y, curveCtr.Z);
            XYZ origCtr = (origLL + origUR) / 2.0;
         
            double centerDist = origCtr.DistanceTo(curveCtr);
            XYZ approxMoveDir = (origCtr - curveCtr).Normalize();
         
            bool cutDirPointingIn = (cutDir.DotProduct(approxMoveDir) < 0.0);
            bool centerInsideArc = (centerDist < radius);
            if (centerInsideArc == cutDirPointingIn)
            {
               XYZ moveVec = cutDir * -unScaledDepth;
               origCtr += moveVec;
               tmpCutLoop = GeometryUtil.MoveCurveLoop(tmpCutLoop, moveVec);
            }
         
            // not for windows that are too big ... forget about it.  Very rare case.
            double depthFactor = openingWidth / (2.0 * radius);
            double eps = MathUtil.Eps;
            if (depthFactor < 1.0 - eps)
            {
               double depthFactorSq = depthFactor * depthFactor * 4;
               double extraDepth = radius * (1.0 - Math.Sqrt(1.0 - depthFactorSq));
               if (extraDepth > eps)
               {
                  XYZ moveVec = cutDir * -extraDepth;
                  tmpCutLoop = GeometryUtil.MoveCurveLoop(tmpCutLoop, moveVec);
                  unScaledDepth += extraDepth;
               }
            }
         
            // extra fudge on the other side of the window opening.
            depthFactor = origUnscaledDepth / (2.0 * radius);
            if (depthFactor < 1.0 - eps)
            {
               double extraDepth = radius * (1.0 - Math.Sqrt(1.0 - depthFactor));
               if (extraDepth > eps)
                  unScaledDepth += extraDepth;
            }
         }

         Transform lcs = new Transform(loopLcs)
         {
            Origin = XYZ.Zero
         };

         // now move to origin in this coordinate system.
         // todo: update openingtrf if we are to use it again!
         BoundingBoxXYZ tmpBBox = ComputeApproximateCurveLoopBBoxForOpening(tmpCutLoop, lcs);
         if (tmpBBox != null)
         {
            relOrig = tmpBBox.Min;
            XYZ moveVec = relOrig * -1.0;
            tmpCutLoop = GeometryUtil.MoveCurveLoop(tmpCutLoop, moveVec);
         }

         IList<CurveLoop> oCutLoopList = new List<CurveLoop>();
         oCutLoopList.Add(tmpCutLoop);

         double depth = UnitUtil.ScaleLength(unScaledDepth);

         IFCAnyHandle openingRepHnd = RepresentationUtil.CreateExtrudedProductDefShape(exporterIFC, doorWindowElement, catId,
             oCutLoopList, lcs, cutDir, depth);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(openingRepHnd))
            return null;

         // care only about first loop.
         IFCFile file = exporterIFC.GetFile();
         XYZ scaledOrig = UnitUtil.ScaleLength(relOrig);

         if (RepresentationUtil.DocumentMirrorState.IsExportingMirroredLink())
         {
            Transform mirrorTrf = FederatedLinkManager.MirrorTransform;
            if (mirrorTrf != null)
               scaledOrig = mirrorTrf.OfPoint(scaledOrig);
         }

         IFCAnyHandle openingPlacement = ExporterUtil.CreateLocalPlacement(file, hostObjPlacementHnd, scaledOrig, relZ, relX);

         string openingObjectType = isRecess ? "Recess" : "Opening";
         string origOpeningName = NamingUtil.GetIFCNamePlusIndex(doorWindowElement, 1);
         string openingDescription = NamingUtil.GetDescriptionOverride(doorWindowElement, null);
         string openingName = NamingUtil.GetNameOverride(doorWindowElement, origOpeningName);
         string openingTag = NamingUtil.GetTagOverride(doorWindowElement);
         IFCAnyHandle openingHnd = IFCInstanceExporter.CreateOpeningElement(exporterIFC, 
            openingGUID, ownerHistory, 
            openingName, openingDescription, openingObjectType,
            openingPlacement, openingRepHnd, openingTag);
         
         string openingVoidsGUID = GUIDUtil.CreateSubElementGUID(doorWindowElement, (int)IFCDoorSubElements.DoorOpeningRelVoid);
         IFCInstanceExporter.CreateRelVoidsElement(file, openingVoidsGUID, ownerHistory, null, null, hostObjHnd, openingHnd);

         if (ExporterCacheManager.ExportIFCBaseQuantities())
         {
            using (IFCExportBodyParams extraParams = new IFCExportBodyParams())
            {
               double height = 0.0, width = 0.0;
               OpeningUtil.GetOpeningDirections(wallElement, out _, out XYZ wallAxis);
               if (GeometryUtil.ComputeHeightWidthOfCurveLoop(tmpCutLoop, wallAxis, out height, out width))
               {
                  extraParams.ScaledHeight = UnitUtil.ScaleLength(height);
                  extraParams.ScaledWidth = UnitUtil.ScaleLength(width);
               }

               IList<CurveLoop> curveLoops = new List<CurveLoop>();
               curveLoops.Add(tmpCutLoop);
               double area = ExporterIFCUtils.ComputeAreaOfCurveLoops(curveLoops);
               if (area > 0.0)
                  extraParams.ScaledArea = UnitUtil.ScaleArea(area);

               extraParams.ScaledLength = depth;
               PropertyUtil.CreateOpeningQuantities(exporterIFC, openingHnd, extraParams);
            }
         }

         return DoorWindowOpeningInfo.Create(openingHnd, openingHeight, openingWidth);
      }

      /// <summary>
      /// Create the opening associated to an already created door or window.
      /// </summary>
      /// <param name="exporterIFC">The exporter class.</param>
      /// <param name="doc">The document.</param>
      /// <param name="hostObjHnd">The host object IFC handle.</param>
      /// <param name="hostId">The host object element id.</param>
      /// <param name="insertId">The insert element id.</param>
      /// <param name="openingGUID">The GUID for the IfcOpeningElement.</param>
      /// <param name="solid">The solid representing the opening geometry.</param>
      /// <param name="scaledHostWidth">The width of the host object that the opening is cutting.</param>
      /// <param name="isRecess">True if the IfcOpeningElement should represent a recess.</param>
      /// <returns>The class containing information about the opening.</returns>
      static public DoorWindowOpeningInfo CreateOpeningForDoorWindow(ExporterIFC exporterIFC, Document doc,
          IFCAnyHandle hostObjHnd, ElementId hostId, ElementId insertId, string openingGUID, Solid solid, double scaledHostWidth, bool isRecess)
      {
         IFCFile file = exporterIFC.GetFile();
         Element hostElement = doc.GetElement(hostId);
         Element insertElement = doc.GetElement(insertId);

         ElementId catId = CategoryUtil.GetSafeCategoryId(hostElement);

         using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, insertElement))
         {
            using (IFCExportBodyParams extrusionCreationData = new IFCExportBodyParams())
            {
               extrusionCreationData.SetLocalPlacement(ExporterUtil.CreateLocalPlacement(file, setter.LocalPlacement, null));
               extrusionCreationData.ReuseLocalPlacement = true;

               IFCAnyHandle openingHnd = OpeningUtil.CreateOpening(exporterIFC, hostObjHnd, hostElement, insertElement, openingGUID, solid, scaledHostWidth,
                   isRecess, extrusionCreationData, null, null, null, -1, -1);

               double unscaledHeight = UnitUtil.UnscaleLength(extrusionCreationData.ScaledHeight);
               double unscaledWidth = UnitUtil.UnscaleLength(extrusionCreationData.ScaledWidth);
               return DoorWindowOpeningInfo.Create(openingHnd, unscaledHeight, unscaledWidth);
            }
         }
      }
   }
}