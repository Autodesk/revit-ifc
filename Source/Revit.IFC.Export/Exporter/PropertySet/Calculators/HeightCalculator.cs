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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate height for a railing.
   /// </summary>
   class HeightCalculator : PropertyCalculator
   {
      /// <summary>
      /// A double variable to keep the calculated value.
      /// </summary>
      private double m_Height = 0;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static HeightCalculator s_Instance = new HeightCalculator();

      /// <summary>
      /// The RailingHeightCalculator instance.
      /// </summary>
      public static HeightCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates height for a Revit element.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="extrusionCreationData">The IFCExportBodyParams.</param>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="elementType">The element type.</param>
      /// <returns>True if the operation succeed, false otherwise.</returns>
      public override bool Calculate(ExporterIFC exporterIFC, 
         IFCExportBodyParams extrusionCreationData, Element element, ElementType elementType, 
         EntryMap entryMap)
      {
         m_Height = 0.0;
         if (element == null)
            return false;

         // For Railing
         RailingType railingType = element.Document.GetElement(element.GetTypeId()) as RailingType;
         if (railingType != null)
         {
            m_Height = UnitUtil.ScaleLength(railingType.TopRailHeight);
            return true;
         }

         double eps = MathUtil.Eps();

         // For ProvisionForVoid
         ShapeCalculator shapeCalculator = ShapeCalculator.Instance;
         if (shapeCalculator != null && shapeCalculator.GetCurrentElement() == element)
         {
            if (String.Compare(shapeCalculator.GetStringValue(), IFCProvisionForVoidShapeType.Rectangle.ToString()) == 0)
            {
               IFCAnyHandle rectProfile = shapeCalculator.GetCurrentProfileHandle();
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(rectProfile))
               {
                  // This is already scaled.
                  double? height = IFCAnyHandleUtil.GetDoubleAttribute(rectProfile, "YDim");
                  m_Height = height.HasValue ? height.Value : 0.0;
                  if (m_Height > eps)
                     return true;
               }
            }
         }

         ElementId categoryId = CategoryUtil.GetSafeCategoryId(element);
         IFCAnyHandle hnd = ExporterCacheManager.ElementToHandleCache.Find(element.Id);

         m_Height = 0.0;
         if (IFCAnyHandleUtil.IsSubTypeOf(hnd, IFCEntityType.IfcDoor) || categoryId == new ElementId(BuiltInCategory.OST_Doors))
         {
            ParameterUtil.GetDoubleValueFromElementOrSymbol(element, BuiltInParameter.DOOR_HEIGHT, out m_Height);
         }
         else if (IFCAnyHandleUtil.IsSubTypeOf(hnd, IFCEntityType.IfcWindow) || categoryId == new ElementId(BuiltInCategory.OST_Windows))
         {
            ParameterUtil.GetDoubleValueFromElementOrSymbol(element, BuiltInParameter.WINDOW_HEIGHT, out m_Height);
         }
         else if (IFCAnyHandleUtil.IsSubTypeOf(hnd, IFCEntityType.IfcCurtainWall))
         {
            BoundingBoxXYZ boundingBox = element.get_BoundingBox(null);
            if (boundingBox != null)
               m_Height = boundingBox.Max.Z - boundingBox.Min.Z;
         }

         if (m_Height > eps)
         {
            m_Height = UnitUtil.ScaleLength(m_Height);
            return true;
         }

         ParameterUtil.GetDoubleValueFromElementOrSymbol(element, entryMap.RevitParameterName, out m_Height, entryMap.CompatibleRevitParameterName);

         if (m_Height > eps)
         {
            m_Height = UnitUtil.ScaleLength(m_Height);
            return true;
         }

         // For other elements
         m_Height = extrusionCreationData?.ScaledHeight ?? 0.0;
         return m_Height > eps;
      }

      /// <summary>
      /// Gets the calculated double value.
      /// </summary>
      /// <returns>
      /// The double value.
      /// </returns>
      public override double GetDoubleValue()
      {
         return m_Height;
      }
   }
}
