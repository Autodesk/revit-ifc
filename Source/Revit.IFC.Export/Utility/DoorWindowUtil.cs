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
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
   using Revit.IFC.Common.Utility;
   using Revit.IFC.Export.Exporter.PropertySet;

   /// <summary>
   /// Provides static methods for door and window related manipulations.
   /// </summary>
   class DoorWindowUtil
   {
      /// <summary>
      /// Gets the panel operation from door style operation.
      /// </summary>
      /// <param name="ifcDoorStyleOperationType">
      /// The IFCDoorStyleOperation.
      /// </param>
      /// <returns>
      /// The string represents the door panel operation.
      /// </returns>
      public static string GetPanelOperationFromDoorStyleOperation(string ifcDoorStyleOperationType)
      {
         if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(ifcDoorStyleOperationType, "SingleSwingLeft") ||
             NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(ifcDoorStyleOperationType, "SingleSwingRight") ||
             NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(ifcDoorStyleOperationType, "DoubleDoorSingleSwing") ||
             NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(ifcDoorStyleOperationType, "DoubleDoorSingleSwingOppositeLeft") ||
             NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(ifcDoorStyleOperationType, "DoubleDoorSingleSwingOppositeRight"))
            return "SWINGING";

         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(ifcDoorStyleOperationType, "DoubleSwingLeft") ||
             NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(ifcDoorStyleOperationType, "DoubleSwingRight") ||
             NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(ifcDoorStyleOperationType, "DoubleDoorDoubleSwing"))
            return "DOUBLE_ACTING";

         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(ifcDoorStyleOperationType, "SlidingToLeft") ||
             NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(ifcDoorStyleOperationType, "SlidingToRight") ||
             NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(ifcDoorStyleOperationType, "DoubleDoorSliding"))
            return "SLIDING";

         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(ifcDoorStyleOperationType, "FoldingToLeft") ||
             NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(ifcDoorStyleOperationType, "FoldingToRight") ||
             NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(ifcDoorStyleOperationType, "DoubleDoorFolding"))
            return "FOLDING";

         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(ifcDoorStyleOperationType, "Revolving"))
            return "REVOLVING";

         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(ifcDoorStyleOperationType, "RollingUp"))
            return "ROLLINGUP";

         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(ifcDoorStyleOperationType, "UserDefined"))
            return "USERDEFINED";
         else
            return "NOTDEFINED";
      }

      /// <summary>
      /// Creates door panel properties.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="doorWindowInfo">The DoorWindowInfo object.</param>
      /// <param name="familyInstance">The family instance of a door.</param>
      /// <returns>The list of handles created.</returns>
      public static IList<IFCAnyHandle> CreateDoorPanelProperties(ExporterIFC exporterIFC,
         DoorWindowInfo doorWindowInfo, Element familyInstance)
      {
         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

         IList<IFCAnyHandle> doorPanels = new List<IFCAnyHandle>();

         IList<double?> panelDepthList = new List<double?>();
         IList<double?> panelWidthList = new List<double?>();

         IList<string> panelOperationList = new List<string>();
         IList<string> panelPositionList = new List<string>();

         int panelNumber = 1;
         const int maxPanels = 64;  // arbitrary large number to prevent infinite loops.
         for (; panelNumber < maxPanels; panelNumber++)
         {
            string panelDepthCurrString = "PanelDepth" + panelNumber.ToString();
            string panelWidthCurrString = "PanelWidth" + panelNumber.ToString();

            // We will always have at least one panel definition as long as the panelOperation is not
            // NotDefined.

            panelOperationList.Add(GetPanelOperationFromDoorStyleOperation(doorWindowInfo.DoorOperationTypeString));

            // If the panel operation is defined we'll allow no panel position for the 1st panel.
            string panelPosition = GetIFCDoorPanelPosition("", familyInstance, panelNumber);
            if (panelPosition == null)
            {
               if (panelNumber == 1)
                  panelPosition = GetIFCDoorPanelPosition("", familyInstance, -1);
               if ((panelPosition == null) && (panelNumber > 1))
               {
                  panelPositionList.Add("NOTDEFINED");
                  break;
               }
            }

            if (doorWindowInfo.FlippedX ^ doorWindowInfo.FlippedY)
               panelPosition = ReverseDoorPanelPosition(panelPosition);

            panelPositionList.Add(panelPosition != null ? panelPosition : "NOTDEFINED");

            double value1 = 0.0, value2 = 0.0;
            bool foundDepth = (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, panelDepthCurrString, out value1) != null);
            if (!foundDepth && (panelNumber == 1))
               foundDepth = (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "PanelDepth", out value1) != null);

            bool foundWidth = (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, panelWidthCurrString, out value2) != null);
            if (!foundWidth && (panelNumber == 1))
               foundWidth = (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "PanelWidth", out value2) != null);

            if (foundDepth && foundWidth)
            {
               panelDepthList.Add(UnitUtil.ScaleLength(value1));
               // Make sure value is in [0,1] range.
               if (value2 < 0.0) value2 = 0.0; else if (value2 > 1.0) value2 = 1.0;
               panelWidthList.Add(value2);
            }
            else
            {
               panelDepthList.Add(null);
               panelWidthList.Add(null);
            }
         }

         string baseDoorPanelName = NamingUtil.GetIFCName(familyInstance);
         for (int panelIndex = 0; (panelIndex < panelNumber - 1); panelIndex++)
         {
            double? currentPanelWidth = null;
            if (panelWidthList[panelIndex].HasValue)
               currentPanelWidth = (double)panelWidthList[panelIndex];

            string doorPanelName = baseDoorPanelName;
            //string doorPanelGUID = GUIDUtil.CreateGUID();
            string doorPanelGUID = GUIDUtil.CreateSubElementGUID(familyInstance, (int)IFCDoorSubElements.DoorPanelStart + panelIndex);
            IFCAnyHandle doorPanel = IFCInstanceExporter.CreateDoorPanelProperties(file, doorPanelGUID, ownerHistory,
               doorPanelName, null, panelDepthList[panelIndex], panelOperationList[panelIndex],
               currentPanelWidth, panelPositionList[panelIndex], null);
            doorPanels.Add(doorPanel);
         }

         return doorPanels;
      }

      /// <summary>
      /// Creates door lining properties.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="familyInstance">
      /// The family instance of a door.
      /// </param>
      /// <returns>
      /// The handle created.
      /// </returns>
      public static IFCAnyHandle CreateDoorLiningProperties(ExporterIFC exporterIFC, Element familyInstance)
      {
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

         double value1, value2;

         // both of these must be defined, or not defined - if only one is defined, we ignore the values.
         if ((ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "LiningDepth", out value1) != null) &&
             (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "LiningThickness", out value2) != null))
         {
            liningDepthOpt = UnitUtil.ScaleLength(value1);
            liningThicknessOpt = UnitUtil.ScaleLength(value2);
         }

         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "LiningOffset", out value1) != null)
            liningOffsetOpt = UnitUtil.ScaleLength(value1);

         // both of these must be defined, or not defined - if only one is defined, we ignore the values.
         if ((ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "ThresholdDepth", out value1) != null) &&
             (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "ThresholdThickness", out value2) != null))
         {
            thresholdDepthOpt = UnitUtil.ScaleLength(value1);
            thresholdThicknessOpt = UnitUtil.ScaleLength(value2);
         }

         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "ThreshholdOffset", out value1) != null)
            liningOffsetOpt = UnitUtil.ScaleLength(value1);

         // both of these must be defined, or not defined - if only one is defined, we ignore the values.
         if ((ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "TransomOffset", out value1) != null) &&
             (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "TransomThickness", out value2) != null))
         {
            transomOffsetOpt = UnitUtil.ScaleLength(value1);
            transomThicknessOpt = UnitUtil.ScaleLength(value2);
         }

         // both of these must be defined, or not defined - if only one is defined, we ignore the values.
         if ((ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "CasingDepth", out value1) != null) &&
             (ParameterUtil.GetPositiveDoubleValueFromElementOrSymbol(familyInstance, "CasingThickness", out value2) != null))
         {
            casingDepthOpt = UnitUtil.ScaleLength(value1);
            casingThicknessOpt = UnitUtil.ScaleLength(value2);
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
      /// <param name="typeName">
      /// The type name of the door.
      /// </param>
      /// <param name="element">
      /// The door element.
      /// </param>
      /// <param name="number">
      /// The number of panel position.
      /// </param>
      /// <returns>
      /// The string represents the door panel position.
      /// </returns>
      public static string GetIFCDoorPanelPosition(string typeName, Element element, int number)
      {
         string currPanelName;
         if (number == -1)
            currPanelName = "PanelPosition";
         else
            currPanelName = "PanelPosition" + number.ToString();

         string value = "";
         if (ParameterUtil.GetStringValueFromElementOrSymbol(element, currPanelName, out value) == null)
            value = typeName;

         if (value == "")
            return null;
         else if (String.Compare(value, "left", true) == 0)
            return "LEFT";
         else if (String.Compare(value, "middle", true) == 0)
            return "MIDDLE";
         else if (String.Compare(value, "right", true) == 0)
            return "RIGHT";
         else
            return "NOTDEFINED";
      }

      /// <summary>
      /// Reverses door panel position.
      /// </summary>
      /// <param name="originalPosition">
      /// The original position.
      /// </param>
      /// <returns>
      /// The string represents the reversed door panel position.
      /// </returns>
      public static string ReverseDoorPanelPosition(string originalPosition)
      {
         if (originalPosition == null)
            return "NOTDEFINED";
         else if (String.Compare(originalPosition, "Left", true) == 0)
            return "RIGHT";
         else if (String.Compare(originalPosition, "Right", true) == 0)
            return "LEFT";
         return originalPosition;
      }

      /// <summary>
      /// Gets window style operation.
      /// </summary>
      /// <param name="familySymbol">
      /// The element type of window.
      /// </param>
      /// <returns>
      /// The IFCWindowStyleOperation.
      /// </returns>
      public static Toolkit.IFCWindowStyleOperation GetIFCWindowStyleOperation(ElementType familySymbol)
      {
         string value;
         ParameterUtil.GetStringValueFromElement(familySymbol, BuiltInParameter.WINDOW_OPERATION_TYPE, out value);

         if (String.IsNullOrEmpty(value))
            return Toolkit.IFCWindowStyleOperation.NotDefined;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "UserDefined"))
            return Toolkit.IFCWindowStyleOperation.UserDefined;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "SinglePanel"))
            return Toolkit.IFCWindowStyleOperation.Single_Panel;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "DoublePanelVertical"))
            return Toolkit.IFCWindowStyleOperation.Double_Panel_Vertical;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "DoublePanelHorizontal"))
            return Toolkit.IFCWindowStyleOperation.Double_Panel_Horizontal;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelVertical"))
            return Toolkit.IFCWindowStyleOperation.Triple_Panel_Vertical;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelBottom"))
            return Toolkit.IFCWindowStyleOperation.Triple_Panel_Bottom;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelTop"))
            return Toolkit.IFCWindowStyleOperation.Triple_Panel_Top;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelLeft"))
            return Toolkit.IFCWindowStyleOperation.Triple_Panel_Left;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelRight"))
            return Toolkit.IFCWindowStyleOperation.Triple_Panel_Right;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelHorizontal"))
            return Toolkit.IFCWindowStyleOperation.Triple_Panel_Horizontal;

         return Toolkit.IFCWindowStyleOperation.NotDefined;
      }

      /// <summary>
      /// New in IFC4: to get Partitioning type information from Window. In IFC2x3 is called Window Operation Type
      /// </summary>
      /// <param name="familySymbol"></param>
      /// <returns>The partitioning type information.</returns>
      public static string GetIFCWindowPartitioningType(ElementType familySymbol)
      {
         string value;
         ParameterUtil.GetStringValueFromElement(familySymbol, "WINDOW_PARTITIONING_TYPE", out value);

         if (String.IsNullOrEmpty(value))
            return "NOTDEFINED";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "UserDefined"))
            return "USERDEFINED";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "SinglePanel"))
            return "SINGLE_PANEL";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "DoublePanelVertical"))
            return "DOUBLE_PANEL_VERTICAL";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "DoublePanelHorizontal"))
            return "DOUBLE_PANEL_HORIZONTAL";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelVertical"))
            return "TRIPLE_PANEL_VERTICAL";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelHorizontal"))
            return "TRIPLE_PANEL_HORIZONTAL";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelBottom"))
            return "TRIPLE_PANEL_BOTTOM";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelTop"))
            return "TRIPLE_PANEL_TOP";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelLeft"))
            return "TRIPLE_PANEL_LEFT";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TriplePanelRight"))
            return "TRIPLE_PANEL_RIGHT";

         return "NOTDEFINED";
      }

      public static string GetIFCWindowType(ElementType familySymbol)
      {
         string value;
         ParameterUtil.GetStringValueFromElement(familySymbol, "WINDOW_PREDEFINED_TYPE", out value);

         if (String.IsNullOrEmpty(value))
            return "NOTDEFINED";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Movable"))
            return "MOVABLE";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Parapet"))
            return "PARAPET";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Partitioning"))
            return "PARTITIONING";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "PlumbingWall"))
            return "PLUMBINGWALL";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Shear"))
            return "SHEAR";
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "SolidWall"))
            return "SOLIDWALL";

         return "NOTDEFINED";

      }

      /// <summary>
      /// Gets IFCDoorStyleConstruction from construction type name.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <returns>The IFCDoorStyleConstruction.</returns>
      public static IFCDoorStyleConstruction GetDoorStyleConstruction(Element element)
      {
         string value = null;
         if (ParameterUtil.GetStringValueFromElementOrSymbol(element, "Construction", out value) == null)
            ParameterUtil.GetStringValueFromElementOrSymbol(element, BuiltInParameter.DOOR_CONSTRUCTION_TYPE, false, out value);

         if (String.IsNullOrEmpty(value))
            return IFCDoorStyleConstruction.NotDefined;

         string newValue = NamingUtil.RemoveSpacesAndUnderscores(value);

         if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(newValue, "USERDEFINED"))
            return IFCDoorStyleConstruction.UserDefined;
         if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(newValue, "ALUMINIUM"))
            return IFCDoorStyleConstruction.Aluminium;
         if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(newValue, "HIGHGRADESTEEL"))
            return IFCDoorStyleConstruction.High_Grade_Steel;
         if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(newValue, "STEEL"))
            return IFCDoorStyleConstruction.Steel;
         if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(newValue, "WOOD"))
            return IFCDoorStyleConstruction.Wood;
         if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(newValue, "ALUMINIUMWOOD"))
            return IFCDoorStyleConstruction.Aluminium_Wood;
         if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(newValue, "ALUMINIUMPLASTIC"))
            return IFCDoorStyleConstruction.Aluminium_Plastic;
         if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(newValue, "PLASTIC"))
            return IFCDoorStyleConstruction.Plastic;

         return IFCDoorStyleConstruction.UserDefined;
      }

      /// <summary>
      /// Gets window style construction.
      /// </summary>
      /// <param name="element">The window element.</param>
      /// <returns>The string represents the window style construction.</returns>
      public static IFCWindowStyleConstruction GetIFCWindowStyleConstruction(Element element)
      {
         string value;
         if (ParameterUtil.GetStringValueFromElementOrSymbol(element, "Construction", out value) == null)
            ParameterUtil.GetStringValueFromElementOrSymbol(element, BuiltInParameter.WINDOW_CONSTRUCTION_TYPE, false, out value);

         if (String.IsNullOrWhiteSpace(value))
            return IFCWindowStyleConstruction.NotDefined;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Aluminum"))
            return IFCWindowStyleConstruction.Aluminium;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "HighGradeSteel"))
            return IFCWindowStyleConstruction.High_Grade_Steel;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Steel"))
            return IFCWindowStyleConstruction.Steel;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Wood"))
            return IFCWindowStyleConstruction.Wood;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "AluminumWood"))
            return IFCWindowStyleConstruction.Aluminium_Wood;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Plastic"))
            return IFCWindowStyleConstruction.Plastic;

         //else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "OtherConstruction"))
         return IFCWindowStyleConstruction.Other_Construction;
      }

      /// <summary>
      /// Gets window panel operation.
      /// </summary>
      /// <param name="initialValue">
      /// The initial value.
      /// </param>
      /// <param name="element">
      /// The window element.
      /// </param>
      /// <param name="number">
      /// The number of panel operation.
      /// </param>
      /// <returns>
      /// The string represents the window panel operation.
      /// </returns>
      public static IFCWindowPanelOperation GetIFCWindowPanelOperation(string initialValue, Element element, int number)
      {
         string currPanelName = "PanelOperation" + number.ToString();

         string value;
         if (ParameterUtil.GetStringValueFromElementOrSymbol(element, currPanelName, out value) == null)
            value = initialValue;

         if (value == "")
            return IFCWindowPanelOperation.NotDefined;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "SideHungRightHand"))
            return IFCWindowPanelOperation.SideHungRightHand;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "SideHungLeftHand"))
            return IFCWindowPanelOperation.SideHungLeftHand;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TiltAndTurnRightHand"))
            return IFCWindowPanelOperation.TiltAndTurnRightHand;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TiltAndTurnLeftHand"))
            return IFCWindowPanelOperation.TiltAndTurnLeftHand;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "TopHung"))
            return IFCWindowPanelOperation.TopHung;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "BottomHung"))
            return IFCWindowPanelOperation.BottomHung;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "PivotHorizontal"))
            return IFCWindowPanelOperation.PivotHorizontal;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "PivotVertical"))
            return IFCWindowPanelOperation.PivotVertical;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "SlidingHorizontal"))
            return IFCWindowPanelOperation.SlidingHorizontal;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "SlidingVertical"))
            return IFCWindowPanelOperation.SlidingVertical;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "RemovableCasement"))
            return IFCWindowPanelOperation.RemovableCasement;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "FixedCasement"))
            return IFCWindowPanelOperation.FixedCasement;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "OtherOperation"))
            return IFCWindowPanelOperation.OtherOperation;

         return IFCWindowPanelOperation.NotDefined;
      }

      /// <summary>
      /// Gets window panel position.
      /// </summary>
      /// <param name="initialValue">
      /// The initial value.
      /// </param>
      /// <param name="element">
      /// The window element.
      /// </param>
      /// <param name="number">
      /// The number of panel position.
      /// </param>
      /// <returns>
      /// The string represents the window panel position.
      /// </returns>
      public static IFCWindowPanelPosition GetIFCWindowPanelPosition(string initialValue, Element element, int number)
      {
         string currPanelName = "PanelPosition" + number.ToString();

         string value;
         if (ParameterUtil.GetStringValueFromElementOrSymbol(element, currPanelName, out value) == null)
            value = initialValue;

         if (value == "")
            return IFCWindowPanelPosition.NotDefined;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Left"))
            return IFCWindowPanelPosition.Left;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Middle"))
            return IFCWindowPanelPosition.Middle;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Right"))
            return IFCWindowPanelPosition.Right;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Bottom"))
            return IFCWindowPanelPosition.Bottom;
         else if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(value, "Top"))
            return IFCWindowPanelPosition.Top;

         return IFCWindowPanelPosition.NotDefined;
      }

      /// <summary>
      /// Creates window panel position.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="familyInstance">
      /// The family instance of a window.
      /// </param>
      /// <param name="description">
      /// The description.
      /// </param>
      /// <returns>
      /// The handle created.
      /// </returns>
      public static IFCAnyHandle CreateWindowLiningProperties(ExporterIFC exporterIFC,
         Element familyInstance, string description)
      {
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

         double value1 = 0.0;
         double value2 = 0.0;

         // both of these must be defined (or not defined)
         if ((ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "LiningDepth", out value1) != null) &&
             (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "LiningThickness", out value2) != null))
         {
            liningDepthOpt = UnitUtil.ScaleLength(value1);
            liningThicknessOpt = UnitUtil.ScaleLength(value2);
         }

         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "TransomThickness", out value1) != null)
            transomThicknessOpt = UnitUtil.ScaleLength(value1);

         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "FirstTransomOffset", out value1) != null)
            firstTransomOffsetOpt = UnitUtil.ScaleLength(value1);

         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "SecondTransomOffset", out value1) != null)
            secondTransomOffsetOpt = UnitUtil.ScaleLength(value1);

         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "MullionThickness", out value1) != null)
            mullionThicknessOpt = UnitUtil.ScaleLength(value1);

         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "FirstMullionOffset", out value1) != null)
            firstMullionOffsetOpt = UnitUtil.ScaleLength(value1);

         if (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "SecondMullionOffset", out value1) != null)
            secondMullionOffsetOpt = UnitUtil.ScaleLength(value1);

         //string windowLiningGUID = GUIDUtil.CreateGUID();
         string windowLiningGUID = GUIDUtil.CreateSubElementGUID(familyInstance, (int)IFCWindowSubElements.WindowLining);
         string windowLiningName = NamingUtil.GetIFCName(familyInstance);
         return IFCInstanceExporter.CreateWindowLiningProperties(file, windowLiningGUID, ownerHistory,
            windowLiningName, description, liningDepthOpt, liningThicknessOpt, transomThicknessOpt, mullionThicknessOpt,
            firstTransomOffsetOpt, secondTransomOffsetOpt, firstMullionOffsetOpt, secondMullionOffsetOpt, null);
      }

      /// <summary>
      /// Creates window panel properties.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="doorWindowInfo">
      /// The IFCDoorWindowInfo object.
      /// </param>
      /// <param name="familyInstance">
      /// The family instance of a window.
      /// </param>
      /// <param name="description">
      /// The description.
      /// </param>
      /// <returns>
      /// The list of handles created.
      /// </returns>
      public static IList<IFCAnyHandle> CreateWindowPanelProperties(ExporterIFC exporterIFC,
         Element familyInstance, string description)
      {
         IList<IFCAnyHandle> panels = new List<IFCAnyHandle>();
         IFCFile file = exporterIFC.GetFile();
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

         const int maxPanels = 1000;  // arbitrary large number to prevent infinite loops.
         for (int panelNumber = 1; panelNumber < maxPanels; panelNumber++)
         {
            string frameDepthCurrString = "FrameDepth" + panelNumber.ToString();
            string frameThicknessCurrString = "FrameThickness" + panelNumber.ToString();

            IFCWindowPanelOperation panelOperation = GetIFCWindowPanelOperation("", familyInstance, panelNumber);
            IFCWindowPanelPosition panelPosition = GetIFCWindowPanelPosition("", familyInstance, panelNumber);
            if (panelOperation == IFCWindowPanelOperation.NotDefined && panelPosition == IFCWindowPanelPosition.NotDefined)
               break;

            double? frameDepth = null;
            double? frameThickness = null;

            double value1, value2;
            if (((ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, frameDepthCurrString, out value1) != null) ||
                ((panelNumber == 1) && (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "FrameDepth", out value1) != null))) &&
               ((ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, frameThicknessCurrString, out value2) != null) ||
                ((panelNumber == 1) && (ParameterUtil.GetDoubleValueFromElementOrSymbol(familyInstance, "FrameThickness", out value2) != null))))
            {
               frameDepth = UnitUtil.ScaleLength(value1);
               frameThickness = UnitUtil.ScaleLength(value2);
            }

            //string panelGUID = GUIDUtil.CreateGUID();
            string panelGUID = GUIDUtil.CreateSubElementGUID(familyInstance, (int)IFCWindowSubElements.WindowPanelStart + panelNumber);
            string panelName = NamingUtil.GetIFCNamePlusIndex(familyInstance, panelNumber);
            panels.Add(IFCInstanceExporter.CreateWindowPanelProperties(file, panelGUID, ownerHistory,
               panelName, description, panelOperation, panelPosition, frameDepth, frameThickness, null));
         }
         return panels;
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
         if (hostId == ElementId.InvalidElementId)
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

         double eps = MathUtil.Eps();
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
         // calculate some values.
         double openingHeight = -1.0;
         double openingWidth = -1.0;

         Element wallElement = doc.GetElement(hostId);
         Wall wall = (wallElement != null) ? wallElement as Wall : null;
         Curve curve = WallExporter.GetWallAxis(wall);
         if (curve == null)
            return null;

         // Don't export opening if we are exporting parts on a wall, as the parts will already have the openings cut out.
         if (PartExporter.CanExportParts(wall))
            return null;

         ElementId catId = CategoryUtil.GetSafeCategoryId(wall);

         double unScaledDepth = origUnscaledDepth;

         IFCAnyHandle hostObjPlacementHnd = IFCAnyHandleUtil.GetObjectPlacement(hostObjHnd);
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

         XYZ relOrig = XYZ.Zero;
         XYZ relZ = XYZ.BasisZ;
         XYZ relX = XYZ.BasisX;

         double openingZNonScaled = -1.0;

         // get height, width before transform
         BoundingBoxXYZ cutLoopBBox = ComputeApproximateCurveLoopBBoxForOpening(cutLoop, null);
         if (cutLoopBBox != null)
         {
            XYZ dist = cutLoopBBox.Max - cutLoopBBox.Min;
            openingZNonScaled = cutLoopBBox.Min.Z;
            openingHeight = Math.Abs(dist.Z);
            openingWidth = Math.Sqrt(dist.X * dist.X + dist.Y * dist.Y);
         }

         Transform openingTrf = ExporterIFCUtils.GetUnscaledTransform(exporterIFC, hostObjPlacementHnd);
         XYZ hostObjYDir = openingTrf.BasisY;
         XYZ hostObjOrig = openingTrf.Origin;
         openingTrf = openingTrf.Inverse;

         // move to wall axis
         CurveLoop tmpCutLoop = GeometryUtil.TransformCurveLoop(cutLoop, openingTrf);

         cutDir = openingTrf.OfVector(cutDir);
         if (curve is Line)
         {
            Plane cutLoopPlane = null;
            try
            {
               cutLoopPlane = tmpCutLoop.GetPlane();
            }
            catch
            {
               return null;
            }

            XYZ clOrig = cutLoopPlane.Origin;

            double wantOriginAtY = posHingeSide ? (-unScaledDepth / 2.0) : (unScaledDepth / 2.0);

            if (!MathUtil.IsAlmostEqual(wantOriginAtY, clOrig[1]))
            {
               XYZ moveVec = new XYZ(0, wantOriginAtY - clOrig[1], 0);
               tmpCutLoop = GeometryUtil.MoveCurveLoop(tmpCutLoop, moveVec);
            }

            bool cutDirRelToHostObjY = (cutDir[1] > 0.0); // true = same sense, false = opp. sense

            if (posHingeSide != cutDirRelToHostObjY)
            {
               cutDir = cutDir * -1.0;
               cutDirRelToHostObjY = !cutDirRelToHostObjY;  // not used beyond this point.
            }
         }
         else if ((cutLoopBBox != null) && (curve is Arc))
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
            double eps = MathUtil.Eps();
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

         XYZ cutXDir = XYZ.BasisZ;
         XYZ cutYDir = cutDir.CrossProduct(cutXDir);
         cutYDir = cutYDir.Normalize();
         Transform lcs = GeometryUtil.CreateTransformFromVectorsAndOrigin(cutXDir, cutYDir, cutDir, XYZ.Zero);

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

         Element doorWindowElement = doc.GetElement(insertId);

         IFCAnyHandle openingRepHnd = RepresentationUtil.CreateExtrudedProductDefShape(exporterIFC, doorWindowElement, catId,
             oCutLoopList, lcs, cutDir, depth);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(openingRepHnd))
            return null;

         // care only about first loop.
         IFCFile file = exporterIFC.GetFile();
         XYZ scaledOrig = UnitUtil.ScaleLength(relOrig);
         IFCAnyHandle openingPlacement = ExporterUtil.CreateLocalPlacement(file, hostObjPlacementHnd, scaledOrig, relZ, relX);

         string openingObjectType = isRecess ? "Recess" : "Opening";
         string origOpeningName = NamingUtil.GetIFCNamePlusIndex(doorWindowElement, 1);
         string openingName = NamingUtil.GetNameOverride(doorWindowElement, origOpeningName);

         IFCAnyHandle openingHnd = IFCInstanceExporter.CreateOpeningElement(exporterIFC, doorWindowElement, openingGUID, ownerHistory,
             openingPlacement, openingRepHnd);
         IFCAnyHandleUtil.OverrideNameAttribute(openingHnd, openingName);
         IFCAnyHandleUtil.SetAttribute(openingHnd, "ObjectType", openingObjectType);

         string openingVoidsGUID = GUIDUtil.CreateSubElementGUID(doorWindowElement, (int)IFCDoorSubElements.DoorOpeningRelVoid);
         IFCInstanceExporter.CreateRelVoidsElement(file, openingVoidsGUID, ownerHistory, null, null, hostObjHnd, openingHnd);

         if (ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities)
         {
            using (IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData())
            {
               double height = 0.0, width = 0.0;
               if (GeometryUtil.ComputeHeightWidthOfCurveLoop(tmpCutLoop, lcs, out height, out width))
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
            using (IFCExtrusionCreationData extrusionCreationData = new IFCExtrusionCreationData())
            {
               extrusionCreationData.SetLocalPlacement(ExporterUtil.CreateLocalPlacement(file, setter.LocalPlacement, null));
               extrusionCreationData.ReuseLocalPlacement = true;

               IFCAnyHandle openingHnd = OpeningUtil.CreateOpening(exporterIFC, hostObjHnd, hostElement, insertElement, openingGUID, solid, scaledHostWidth,
                   isRecess, extrusionCreationData, null, null);

               double unscaledHeight = UnitUtil.UnscaleLength(extrusionCreationData.ScaledHeight);
               double unscaledWidth = UnitUtil.UnscaleLength(extrusionCreationData.ScaledWidth);
               return DoorWindowOpeningInfo.Create(openingHnd, unscaledHeight, unscaledWidth);
            }
         }
      }
   }
}