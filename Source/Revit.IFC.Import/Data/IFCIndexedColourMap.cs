//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
// Copyright (C) 2013  Autodesk, Inc.
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

using System.Collections.Generic;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public class IFCIndexedColourMap : IFCPresentationItem
   {
      protected IFCIndexedColourMap()
      {
      }

      /// <summary>
      /// MappedTo is a reference to the IfcTessellatedFaceSet to which this applies colors.
      /// </summary>
      public IFCTessellatedFaceSet MappedTo { get; protected set; }

      /// <summary>
      /// Indication that the IfcIndexedColourMap overrides the surface colour information
      /// that might be assigned as an IfcStyledItem to the IfcTessellatedFaceSet.
      /// </summary>
      public IFCSurfaceStyleShading Overrides { get; protected set; }

      /// <summary>
      /// Indexable list of RGB Colours
      /// </summary>
      public IFCColourRgbList Colours { get; protected set; }

      /// <summary>
      /// Indices into the IfcColourRgbList for each face of the IfcTriangulatedFaceSet. Colour is applied uniformly to the face.
      /// </summary>
      public IList<int> ColourIndex { get; protected set; }

      protected IFCIndexedColourMap (IFCAnyHandle item)
      {
         Process(item);
      }

      /// <summary>
      /// Process ifcIndexedColourMap instance.
      /// </summary>
      /// <param name="ifcIndexedColourMap">The handle to process</param>
      protected override void Process(IFCAnyHandle ifcIndexedColourMap)
      {
         base.Process(ifcIndexedColourMap);

         // Handle IfcTesselatedFaceSet / IfcTriangulatedFaceSet
         //
         IFCAnyHandle mappedTo = IFCAnyHandleUtil.GetInstanceAttribute(ifcIndexedColourMap, "MappedTo");
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(mappedTo))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcTessellatedFaceSet);
            return;
         }
         MappedTo = IFCTessellatedFaceSet.ProcessIFCTessellatedFaceSet(mappedTo);

         // Handle (Optional) IfcSurfaceStyleShading
         //
         IFCAnyHandle overrides = IFCImportHandleUtil.GetOptionalInstanceAttribute(ifcIndexedColourMap, "Overrides");
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(overrides))
         {
            Overrides = IFCSurfaceStyleShading.ProcessIFCSurfaceStyleShading(overrides);
         }

         // Handle IfcColourMapList
         //
         IFCAnyHandle colours = IFCAnyHandleUtil.GetInstanceAttribute(ifcIndexedColourMap, "Colours");
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(colours))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcColourRgbList);
            return;
         }
         Colours = IFCColourRgbList.ProcessIFCColourRgbList(colours);

         // Handle Colour Index list
         //
         IList<int> colourIndex = IFCAnyHandleUtil.GetAggregateIntAttribute<List<int>>(ifcIndexedColourMap, "ColourIndex");
         if (colourIndex?.Count > 0)
         {
            ColourIndex = colourIndex;
         }
         else
         {
            Importer.TheLog.LogError(Id, "Invalid Colour Index Map for this IndexedColourMap", false);
            return;
         }
      }

      /// <summary>
      /// Start processing the IfcIndexedColourMap
      /// </summary>
      /// <param name="ifcIndexedColourMap">the handle</param>
      /// <returns></returns>
      public static IFCIndexedColourMap ProcessIFCIndexedColourMap(IFCAnyHandle ifcIndexedColourMap)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcIndexedColourMap))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcIndexedColourMap);
            return null;
         }

         IFCEntity indexedColourMap;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcIndexedColourMap.StepId, out indexedColourMap))
            indexedColourMap = new IFCIndexedColourMap(ifcIndexedColourMap);
         return (indexedColourMap as IFCIndexedColourMap);
      }
   }
}
