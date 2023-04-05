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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcRepresentationMap.
   /// </summary>
   public class IFCRepresentationMap : IFCEntity
   {
      Transform m_MappingOrigin = null;

      IFCRepresentation m_MappedRepresentation = null;

      /// <summary>
      /// The transform associated with the mapping origin.
      /// </summary>
      public Transform MappingOrigin
      {
         get { return m_MappingOrigin; }
         protected set { m_MappingOrigin = value; }
      }

      /// <summary>
      /// The geometry (mapped representation).
      /// </summary>
      public IFCRepresentation MappedRepresentation
      {
         get { return m_MappedRepresentation; }
         protected set { m_MappedRepresentation = value; }
      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCRepresentationMap()
      {

      }

      /// <summary>
      /// Processes IfcRepresentationMap attributes.
      /// </summary>
      /// <param name="ifcRepresentationMap">The IfcRepresentationMap handle.</param>
      protected override void Process(IFCAnyHandle ifcRepresentationMap)
      {
         base.Process(ifcRepresentationMap);

         IFCAnyHandle mappingOrigin = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcRepresentationMap, "MappingOrigin", false);
         if (mappingOrigin != null)
            MappingOrigin = IFCLocation.ProcessIFCAxis2Placement(mappingOrigin);
         else
            MappingOrigin = Transform.Identity;

         IFCAnyHandle mappedRepresentation = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcRepresentationMap, "MappedRepresentation", false);
         if (mappedRepresentation != null)
            MappedRepresentation = IFCRepresentation.ProcessIFCRepresentation(mappedRepresentation);
      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCRepresentationMap(IFCAnyHandle representationMap)
      {
         Process(representationMap);
      }

      /// <summary>
      /// Create geometry for a particular representation map.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <remarks>For this function, if lcs is null, we will create a library item for the geometry.</remarks>
      public void CreateShape(IFCImportShapeEditScope shapeEditScope, Transform scaledLcs, string guid)
      {
         bool creatingLibraryDefinition = (scaledLcs == null);

         if (MappedRepresentation != null)
         {
            // Look for cached shape; if found, return.
            if (creatingLibraryDefinition)
            {
               if (IFCImportFile.TheFile.ShapeLibrary.FindDefinitionType(Id.ToString()) != ElementId.InvalidElementId)
                  return;
            }

            Transform scaledMappingTransform = null;
            if (scaledLcs == null)
               scaledMappingTransform = MappingOrigin;
            else
            {
               if (MappingOrigin == null)
                  scaledMappingTransform = scaledLcs;
               else
                  scaledMappingTransform = scaledLcs.Multiply(MappingOrigin);
            }

            int numExistingSolids = shapeEditScope.Creator.Solids.Count;
            int numExistingCurves = shapeEditScope.Creator.FootprintCurves.Count;

            MappedRepresentation.CreateShape(shapeEditScope, scaledMappingTransform, guid);

            if (creatingLibraryDefinition)
            {
               int numNewSolids = shapeEditScope.Creator.Solids.Count;
               int numNewCurves = shapeEditScope.Creator.FootprintCurves.Count;

               if ((numExistingSolids != numNewSolids) || (numExistingCurves != numNewCurves))
               {
                  IList<GeometryObject> mappedSolids = new List<GeometryObject>();
                  for (int ii = numExistingSolids; ii < numNewSolids; ii++)
                  {
                     mappedSolids.Add(shapeEditScope.Creator.Solids[numExistingSolids].GeometryObject);
                     shapeEditScope.Creator.Solids.RemoveAt(numExistingSolids);
                  }

                  IList<Curve> mappedCurves = new List<Curve>();
                  for (int ii = numExistingCurves; ii < numNewCurves; ii++)
                  {
                     mappedCurves.Add(shapeEditScope.Creator.FootprintCurves[numExistingCurves]);
                     shapeEditScope.Creator.FootprintCurves.RemoveAt(numExistingCurves);
                  }
                  shapeEditScope.AddPlanViewCurves(mappedCurves, Id);

                  Document doc = IFCImportFile.TheFile.Document;
                  DirectShapeType directShapeType = null;

                  IFCTypeProduct typeProduct = null;
                  int typeId = -1;
                  if (Importer.TheCache.RepMapToTypeProduct.TryGetValue(Id, out typeProduct) && typeProduct != null)
                  {
                     ElementId directShapeTypeId = ElementId.InvalidElementId;
                     if (Importer.TheCache.CreatedDirectShapeTypes.TryGetValue(typeProduct.Id, out directShapeTypeId))
                     {
                        directShapeType = doc.GetElement(directShapeTypeId) as DirectShapeType;
                        typeId = typeProduct.Id;
                     }
                  }

                  if (directShapeType == null)
                  {
                     string directShapeTypeName = Id.ToString();
                     directShapeType = IFCElementUtil.CreateElementType(doc, directShapeTypeName, shapeEditScope.CategoryId, Id, null, EntityType);
                     typeId = Id;
                  }

                  if (!Importer.TheProcessor.PostProcessRepresentationMap(typeId, mappedCurves, mappedSolids))
                  {
                     // Do the "default" here instead of the processor, since we don't want the processor
                     // to know about Revit.IFC.Import stuff.
                     directShapeType.AppendShape(mappedSolids);
                     if (mappedCurves.Count != 0)
                        shapeEditScope.SetPlanViewRep(directShapeType);
                  }

                  IFCImportFile.TheFile.ShapeLibrary.AddDefinitionType(Id.ToString(), directShapeType.Id);
               }
            }
         }
      }

      /// <summary>
      /// Processes an IfcRepresentationMap object.
      /// </summary>
      /// <param name="ifcRepresentation">The IfcRepresentationMap handle.</param>
      /// <returns>The IFCRepresentationMap object.</returns>
      public static IFCRepresentationMap ProcessIFCRepresentationMap(IFCAnyHandle ifcRepresentationMap)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcRepresentationMap))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcRepresentationMap);
            return null;
         }

         IFCEntity representationMap;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcRepresentationMap.StepId, out representationMap))
            return (representationMap as IFCRepresentationMap);

         return new IFCRepresentationMap(ifcRepresentationMap);
      }
   }
}