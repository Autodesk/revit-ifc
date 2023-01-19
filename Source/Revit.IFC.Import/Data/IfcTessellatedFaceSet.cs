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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Properties;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public class IFCTessellatedFaceSet : IFCRepresentationItem
   {
      IFCCartesianPointList m_Coordinates = null;

      /// <summary>
      /// Coordinates attribute. This is an IFCCartesianPointList.
      /// </summary>
      public IFCCartesianPointList Coordinates
      {
         get { return m_Coordinates; }
         protected set { m_Coordinates = value; }
      }

      /// <summary>
      /// Indexed Colour Map for TesselatedFaceSet (one colour per face).
      /// </summary>
      public IFCIndexedColourMap HasColours { get; protected set; } = null;

      /// <summary>
      /// Color to use in Material.  Created in Process() and used in CreateShapeInternal()
      /// </summary>
      protected Color MaterialColor { get; set; } = null;

      /// <summary>
      /// Material Created during CreateShapeInternal for applying the MaterialColor to this Face Set if needed.
      /// If the shapeEditScope already contains a MeterialId then that should be used.
      /// </summary>
      protected ElementId MaterialId { get; set; } = ElementId.InvalidElementId;

      protected IFCTessellatedFaceSet()
      {
      }

      protected IFCTessellatedFaceSet(IFCAnyHandle item)
      {
         Process(item);
      }

      /// <summary>
      /// Process IfcTriangulatedFaceSet instance
      /// </summary>
      /// <param name="ifcTessellatedFaceSet">the handle</param>
      protected override void Process(IFCAnyHandle ifcTessellatedFaceSet)
      {
         base.Process(ifcTessellatedFaceSet);

         // Process the IFCCartesianPointList
         IFCAnyHandle coordinates = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcTessellatedFaceSet, "Coordinates", true);
         if (IFCAnyHandleUtil.IsSubTypeOf(coordinates, IFCEntityType.IfcCartesianPointList))
         {
            IFCCartesianPointList coordList = IFCCartesianPointList.ProcessIFCCartesianPointList(coordinates);
            if (coordList != null)
               Coordinates = coordList;
         }

         // Process IfcIndexedColourMap.  There is a maximum of one IfcIndexedColourMap per IfcTesselatedFaceSet (even though it is stored as a SET).
         //
         HashSet<IFCAnyHandle> hasColoursSet = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcTessellatedFaceSet, "HasColours");
         if (hasColoursSet?.Count == 1)
         {
            IFCAnyHandle ifcIndexedColourMap = hasColoursSet.First();
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcIndexedColourMap))
            {
               Importer.TheLog.LogNullError(IFCEntityType.IfcIndexedColourMap);
               return;
            }

            HasColours = IFCIndexedColourMap.ProcessIFCIndexedColourMap(ifcIndexedColourMap);

            if ((StyledByItem == null) && (LayerAssignment == null) && (HasColours != null))
            {
               // Currently we only support one color per Mesh, so just use the first color.
               //
               IFCColourRgbList ifcColourRgbList = HasColours.Colours;
               IList<int> colourIndices = HasColours?.ColourIndex;
               if ((ifcColourRgbList != null) && (colourIndices?.Count > 1))
               {
                  MaterialColor = ifcColourRgbList.GetColor(colourIndices[0]);

                  // Check for more than one unique colour and warn if found.
                  // Simple case:  there is only one colour in IFCColourRgbList
                  //
                  if (ifcColourRgbList.ColourList?.Count > 1)
                  {
                     List<int> uniqueColorValues = colourIndices.Distinct().ToList();
                     if (uniqueColorValues.Count > 1)
                     {
                        Importer.TheLog.LogWarning(Id, "Multiple Colour Indices unsupported in IFCTesselatedFaceSet at this time.", false);
                     }
                  }
               }
            }
         }
      }

      /// <summary>
      /// Returns a Material ElementId for the primary Color in the IFCColourRgbList.
      /// This retrieves the Material ElementId from the Material Cache first.
      /// Matching is done primarily on the RGB Values in the Material Color using a Default Material name ("IFCTesselatedFaceSet").
      /// If that doesn't work, we create a new Material.
      /// </summary>
      /// <param name="shapeEditScope">Edit Scope used in creating the IFCTesselatedFaceSet.</param>
      /// <returns></returns>
      public override ElementId GetMaterialElementId (IFCImportShapeEditScope shapeEditScope)
      {
         // If we've already called this and set the Material Id, then just use that.
         //
         if (MaterialId != ElementId.InvalidElementId)
            return MaterialId;

         // If base class already has a Material, use that.  Also set the MaterialId of this class to avoid unneeded processing.
         //
         ElementId materialId = base.GetMaterialElementId(shapeEditScope);
         if (materialId != ElementId.InvalidElementId)
         {
            MaterialId = materialId;
            return MaterialId;
         }

         // Otherwise, create a new Material representing the MaterialColor for this object (used cached one if it exists).
         //
         if (MaterialId == ElementId.InvalidElementId)
         {
            IFCMaterialInfo newMaterialInfo = IFCMaterialInfo.Create(MaterialColor, null, null, null, null);
            MaterialId = IFCMaterial.CreateMaterialElem(shapeEditScope.Document, Id, String.Format(Resources.IFCDefaultMaterialName, Id), newMaterialInfo);
         }
         return MaterialId;
      }

      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, 
         Transform scaledLcs, string guid)
      {
      }

      /// <summary>
      /// Start processing the IfcTriangulatedFaceSet
      /// </summary>
      /// <param name="ifcTriangulatedFaceSet">the handle</param>
      /// <returns></returns>
      public static IFCTessellatedFaceSet ProcessIFCTessellatedFaceSet(IFCAnyHandle ifcTessellatedFaceSet)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcTessellatedFaceSet))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcTessellatedFaceSet);
            return null;
         }

         IFCEntity tessellatedFaceSet;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcTessellatedFaceSet.StepId, out tessellatedFaceSet))
            tessellatedFaceSet = new IFCTessellatedFaceSet(ifcTessellatedFaceSet);
         return (tessellatedFaceSet as IFCTessellatedFaceSet);
      }
   }
}