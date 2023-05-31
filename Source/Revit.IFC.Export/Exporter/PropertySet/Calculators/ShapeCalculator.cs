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
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.Structure;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate the shape for a provision for void.
   /// </summary>
   class ShapeCalculator : PropertyCalculator
   {
      /// <summary>
      /// The element being processed.
      /// </summary>
      private Element m_CurrentElement = null;

      /// <summary>
      /// The extrusion profile of the element being processed.
      /// </summary>
      private IFCAnyHandle m_CurrentProfileHandle = null;

      /// <summary>
      /// A string variable to keep the calculated value.
      /// </summary>
      private string m_Shape = null;

      /// <summary>
      /// A static instance of this class.
      /// </summary>
      static ShapeCalculator s_Instance = new ShapeCalculator();

      /// <summary>
      /// The ProvisionForVoidShapeCalculator instance.
      /// </summary>
      public static ShapeCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates the shape for a provision for void.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="extrusionCreationData">The IFCExportBodyParams.</param>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="elementType">The element type.</param>
      /// <returns>
      /// True if the operation succeed, false otherwise.
      /// </returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCExportBodyParams extrusionCreationData, Element element, ElementType elementType, EntryMap entryMap)
      {
         if (extrusionCreationData == null)
            return false;

         IFCAnyHandle provisionForVoidHnd = ExporterCacheManager.ElementToHandleCache.Find(element.Id);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(provisionForVoidHnd))
            return false;

         IFCAnyHandle prodRepHnd = IFCAnyHandleUtil.GetInstanceAttribute(provisionForVoidHnd, "Representation");
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(prodRepHnd))
            return false;

         // TODO: See if we need to extend this beyond provisions for voids and duct segments.
         bool isDuctSegmentTypeShapeParam =
            string.Compare(entryMap.RevitParameterName, "Pset_DuctSegmentTypeCommon.Shape", true) == 0;

         IList<IFCAnyHandle> repHnds = IFCAnyHandleUtil.GetRepresentations(prodRepHnd);
         foreach (IFCAnyHandle repHnd in repHnds)
         {
            string repId = IFCAnyHandleUtil.GetStringAttribute(repHnd, "RepresentationIdentifier");
            if (String.Compare(repId, "Body", true) != 0)
               continue;

            string repType = IFCAnyHandleUtil.GetStringAttribute(repHnd, "RepresentationType");
            if (String.Compare(repType, "SweptSolid", true) != 0)
               return false;

            HashSet<IFCAnyHandle> repItemSet = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(repHnd, "Items");
            int numRepItems = repItemSet.Count;
            if (numRepItems == 0)
               return false;

            if (numRepItems == 1)
            {
               IFCAnyHandle repItemHnd = repItemSet.ElementAt(0);
               if (!IFCAnyHandleUtil.IsSubTypeOf(repItemHnd, IFCEntityType.IfcExtrudedAreaSolid))
                  return false;

               IFCAnyHandle sweptAreaHnd = IFCAnyHandleUtil.GetInstanceAttribute(repItemHnd, "SweptArea");
               if (IFCAnyHandleUtil.IsNullOrHasNoValue(sweptAreaHnd))
                  return false;

               m_CurrentProfileHandle = sweptAreaHnd;

               // "Shape" isn't just for Provision For Void; it is also for 
               // Pset_DuctSegmentTypeCommon.Shape.  They have different values, so we will
               // figure out the base value and then "translate".
               // TODO: Deal with potentially different schemas.
               if (IFCAnyHandleUtil.IsTypeOf(m_CurrentProfileHandle, IFCEntityType.IfcRectangleProfileDef))
               {
                  m_Shape = isDuctSegmentTypeShapeParam ? IFC4.PEnum_DuctSegmentShape.RECTANGULAR.ToString() :
                     IFCProvisionForVoidShapeType.Rectangle.ToString();
               }
               else if (IFCAnyHandleUtil.IsTypeOf(m_CurrentProfileHandle, IFCEntityType.IfcCircleProfileDef))
               {
                  m_Shape = IFCProvisionForVoidShapeType.Round.ToString();
               }
            }

            if (m_Shape == null)
            {
               m_Shape = isDuctSegmentTypeShapeParam ? IFC4.PEnum_DuctSegmentShape.OTHER.ToString() :
                  IFCProvisionForVoidShapeType.Undefined.ToString();
               m_CurrentProfileHandle = null;
            }

            m_CurrentElement = element;
            return true;
         }

         return false;
      }

      /// <summary>
      /// Returns the Element corresponding to the shape value.
      /// </summary>
      /// <returns>The element.</returns>
      /// <remarks>Other calculators use the return value of this function to determine if they are appropriate or not.  We store
      /// the current element to make sure that the return values are still applicable.</remarks>
      public Element GetCurrentElement()
      {
         return m_CurrentElement;
      }

      /// <summary>
      /// Returns the extrusion profile handle corresponding to the shape value.
      /// </summary>
      /// <returns>The extrusion profile handle.</returns>
      public IFCAnyHandle GetCurrentProfileHandle()
      {
         return m_CurrentProfileHandle;
      }

      /// <summary>
      /// Gets the calculated string value.
      /// </summary>
      /// <returns>
      /// The string value.
      /// </returns>
      public override string GetStringValue()
      {
         return m_Shape;
      }
   }
}