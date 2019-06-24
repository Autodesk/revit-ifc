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
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export text notes.
   /// </summary>
   class TextNoteExporter
   {
      /// <summary>
      /// Exports text note elements.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="textNote">
      /// The text note element.
      /// </param>
      /// <param name="productWrapper">
      /// The ProductWrapper.
      /// </param>
      public static void Export(ExporterIFC exporterIFC, TextNote textNote, ProductWrapper productWrapper)
      {
         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         string predefinedType = null;
         IFCExportInfoPair exportType = ExporterUtil.GetExportType(exporterIFC, textNote, out predefinedType);
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(exportType.ExportInstance))
            return;

         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction tr = new IFCTransaction(file))
         {
            string textString = textNote.Text;
            if (String.IsNullOrEmpty(textString))
               throw new Exception("TextNote does not have test string.");

            ElementId symId = textNote.GetTypeId();
            if (symId == ElementId.InvalidElementId)
               throw new Exception("TextNote does not have valid type id.");

            PresentationStyleAssignmentCache cache = ExporterCacheManager.PresentationStyleAssignmentCache;
            IFCAnyHandle presHnd = cache.Find(symId);
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(presHnd))
            {
               TextElementType textElemType = textNote.Symbol;
               CreatePresentationStyleAssignmentForTextElementType(exporterIFC, textElemType, cache);
               presHnd = cache.Find(symId);
               if (IFCAnyHandleUtil.IsNullOrHasNoValue(presHnd))
                  throw new Exception("Failed to create presentation style assignment for TextElementType.");
            }

            HashSet<IFCAnyHandle> presHndSet = new HashSet<IFCAnyHandle>();
            presHndSet.Add(presHnd);

            using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, textNote))
            {
               const double planScale = 100.0;  // currently hardwired.

               XYZ orig = UnitUtil.ScaleLength(textNote.Coord);
               XYZ yDir = textNote.UpDirection;
               XYZ xDir = textNote.BaseDirection;
               XYZ zDir = xDir.CrossProduct(yDir);

               double sizeX = UnitUtil.ScaleLength(textNote.Width * planScale);
               double sizeY = UnitUtil.ScaleLength(textNote.Height * planScale);

               // When we display text on screen, we "flip" it if the xDir is negative with relation to
               // the X-axis.  So if it is, we'll flip x and y.
               bool flipOrig = false;
               if (xDir.X < 0)
               {
                  xDir = xDir.Multiply(-1.0);
                  yDir = yDir.Multiply(-1.0);
                  flipOrig = true;
               }

               // xFactor, yFactor only used if flipOrig.
               double xFactor = 0.0, yFactor = 0.0;
               string boxAlignment = ConvertTextNoteAlignToBoxAlign(textNote, out xFactor, out yFactor);

               // modify the origin to match the alignment.  In Revit, the origin is at the top-left (unless flipped,
               // then bottom-right).
               if (flipOrig)
               {
                  orig = orig.Add(xDir.Multiply(sizeX * xFactor));
                  orig = orig.Add(yDir.Multiply(sizeY * yFactor));
               }

               IFCAnyHandle origin = ExporterUtil.CreateAxis(file, orig, zDir, xDir);

               IFCAnyHandle extent = IFCInstanceExporter.CreatePlanarExtent(file, sizeX, sizeY);
               IFCAnyHandle repItemHnd = IFCInstanceExporter.CreateTextLiteralWithExtent(file, textString, origin, Toolkit.IFCTextPath.Left, extent, boxAlignment);
               IFCAnyHandle annoTextOccHnd = IFCInstanceExporter.CreateStyledItem(file, repItemHnd, presHndSet, null);

               ElementId catId = textNote.Category != null ? textNote.Category.Id : ElementId.InvalidElementId;
               HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
               bodyItems.Add(repItemHnd);
               IFCAnyHandle bodyRepHnd = RepresentationUtil.CreateAnnotationSetRep(exporterIFC, textNote, catId, exporterIFC.Get2DContextHandle(), bodyItems);

               if (IFCAnyHandleUtil.IsNullOrHasNoValue(bodyRepHnd))
                  throw new Exception("Failed to create shape representation.");

               IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
               shapeReps.Add(bodyRepHnd);

               IFCAnyHandle prodShapeHnd = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapeReps);
               IFCAnyHandle instHnd;
               if (exportType.ExportInstance == Common.Enums.IFCEntityType.IfcAnnotation)
                  instHnd = IFCInstanceExporter.CreateAnnotation(exporterIFC, textNote, GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle,
                     setter.LocalPlacement, prodShapeHnd);
               else
                  instHnd = IFCInstanceExporter.CreateGenericIFCEntity(exportType, exporterIFC, textNote, GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle,
                     setter.LocalPlacement, prodShapeHnd);

               productWrapper.AddAnnotation(instHnd, setter.LevelInfo, true);
            }

            tr.Commit();
         }
      }

      /// <summary>
      /// Creates IfcPresentationStyleAssignment for text element type.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="textElemType">
      /// The text note element type.
      /// </param>
      /// <param name="cache">
      /// The cache of IfcPresentationStyleAssignment.
      /// </param>
      static void CreatePresentationStyleAssignmentForTextElementType(ExporterIFC exporterIFC, TextElementType textElemType, PresentationStyleAssignmentCache cache)
      {
         IFCFile file = exporterIFC.GetFile();

         string fontName;
         if (ParameterUtil.GetStringValueFromElement(textElemType, BuiltInParameter.TEXT_FONT, out fontName) == null)
            fontName = null;

         double fontSize;
         if (ParameterUtil.GetDoubleValueFromElement(textElemType, BuiltInParameter.TEXT_SIZE, out fontSize) == null)
            fontSize = -1.0;

         double viewScale = 100.0;  // currently hardwired.
         fontSize = UnitUtil.ScaleLength(fontSize * viewScale);

         string ifcPreDefinedItemName = "Text Font";

         IList<string> fontNameList = new List<string>();
         fontNameList.Add(fontName);

         IFCAnyHandle textSyleFontModelHnd = IFCInstanceExporter.CreateTextStyleFontModel(file, ifcPreDefinedItemName, fontNameList, null, null, null, IFCDataUtil.CreateAsPositiveLengthMeasure(fontSize));

         int color;
         ParameterUtil.GetIntValueFromElement(textElemType, BuiltInParameter.LINE_COLOR, out color);

         double blueVal = ((double)((color & 0xff0000) >> 16)) / 255.0;
         double greenVal = ((double)((color & 0xff00) >> 8)) / 255.0;
         double redVal = ((double)(color & 0xff)) / 255.0;

         IFCAnyHandle colorHnd = IFCInstanceExporter.CreateColourRgb(file, null, redVal, greenVal, blueVal);
         IFCAnyHandle fontColorHnd = IFCInstanceExporter.CreateTextStyleForDefinedFont(file, colorHnd, null);

         string ifcAttrName = textElemType.Name;
         IFCAnyHandle textStyleHnd = IFCInstanceExporter.CreateTextStyle(file, textElemType.Name, fontColorHnd, null, textSyleFontModelHnd);

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(textStyleHnd))
            return;

         HashSet<IFCAnyHandle> presStyleSet = new HashSet<IFCAnyHandle>();
         presStyleSet.Add(textStyleHnd);

         IFCAnyHandle presStyleHnd = IFCInstanceExporter.CreatePresentationStyleAssignment(file, presStyleSet);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(presStyleHnd))
            return;

         cache.Register(textElemType.Id, presStyleHnd);
      }

      /// <summary>
      /// Converts text note align to box align.
      /// </summary>
      /// <param name="textNote">
      /// The text note element.
      /// </param>
      /// <param name="xFactor">
      /// The X factor.
      /// </param>
      /// <param name="yFactor">
      /// The Y factor.
      /// </param>
      static string ConvertTextNoteAlignToBoxAlign(TextNote textNote, out double xFactor, out double yFactor)
      {
         xFactor = yFactor = 0.0;

         // boxAlignment consist of a combination of vertical and horizontal alignment
         // except for the middle-middle alignment which is named simply "center"
         // The xFactor and yFactor are set as follows:
         //      +1
         //   -1  0  +1
         //      -1

         // The center-middle is an odd case; we can deal with it firs

         if ((HorizontalTextAlignment.Center == textNote.HorizontalAlignment)
            && (VerticalTextAlignment.Middle == textNote.VerticalAlignment))
         {
            return "center";
         }

         string boxAlignment = null;

         switch (textNote.VerticalAlignment)
         {
            case VerticalTextAlignment.Top:
               yFactor = 1.0;
               boxAlignment = "top-";
               break;
            case VerticalTextAlignment.Middle:
               boxAlignment = "middle-";
               break;
            case VerticalTextAlignment.Bottom:
               yFactor = -1.0;
               boxAlignment = "bottom-";
               break;
         }

         switch (textNote.HorizontalAlignment)
         {
            case HorizontalTextAlignment.Left:
               xFactor = -1.0;
               boxAlignment += "left";
               break;
            case HorizontalTextAlignment.Center:
               boxAlignment += "middle";
               break;
            case HorizontalTextAlignment.Right:
               xFactor = 1.0;
               boxAlignment += "right";
               break;
         }

         return boxAlignment;
      }
   }
}