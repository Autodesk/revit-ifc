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

using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Enums of shape representation type (according to the list defined in IFC4)
   /// </summary>
   public enum ShapeRepresentationType
   {
      Undefined,
      Point,
      PointCloud,
      Curve,
      Curve2D,
      Curve3D,
      Surface,
      Surface2D,
      Surface3D,
      FillArea,
      Text,
      AdvancedSurface,
      GeometricSet,
      GeometricCurveSet,
      Annotation2D,
      SurfaceModel,
      Tessellation,
      SolidModel,
      SweptSolid,
      AdvancedSweptSolid,
      Brep,
      AdvancedBrep,
      CSG,
      Clipping,
      // additional types
      BoundingBox,
      SectionedSpine,
      LightSource,
      MappedRepresentation,
      // Misc. - non standard
      Mesh,
      Facetation
   }

   /// <summary>
   /// This class contains output information from the ExportBody functions.
   /// </summary>
   public class BodyData
   {
      /// <summary>
      /// The created shape representation handle.
      /// </summary>
      public IFCAnyHandle RepresentationHnd { get; set; } = null;

      /// <summary>
      /// The representation type of the created shape representation handle.
      /// </summary>
      public ShapeRepresentationType ShapeRepresentationType { get; set; } = ShapeRepresentationType.Undefined;

      /// <summary>
      /// The new offset transform, if the local placement was shifted to closer to the exported geometry location.
      /// </summary>
      public Transform OffsetTransform { get; set; } = null;

      /// <summary>
      /// The exported material Ids
      /// </summary>
      public HashSet<ElementId> MaterialIds
      {
         get
         {
            if (MaterialIdList != null)
               return new HashSet<ElementId>(MaterialIdList);
            return null;
         }
      }

      /// <summary>
      /// Material Ids in a list to maintain its order and allows duplicate item (similar to MaterialIds)
      /// </summary>
      public IList<ElementId> MaterialIdList { get; set; } = new List<ElementId>();

      /// <summary>
      /// A handle for the Footprint representation
      /// </summary>
      public FootPrintInfo FootprintInfo { get; set; } = null;

      /// <summary>
      /// A Dictionary for Material Profile
      /// </summary>
      public MaterialAndProfile MaterialAndProfile { get; set; } = new MaterialAndProfile();

      /// <summary>
      /// Constructs a default BodyData object.
      /// </summary>
      public BodyData() { }

      /// <summary>
      /// Constructs a BodyData object.
      /// </summary>
      /// <param name="representationHnd">The representation handle.</param>
      /// <param name="offsetTransform">The offset transform.</param>
      /// <param name="materialIds">The material ids.</param>
      public BodyData(IFCAnyHandle representationHnd, Transform offsetTransform, HashSet<ElementId> materialIds)
      {
         RepresentationHnd = representationHnd;
         if (offsetTransform != null)
            OffsetTransform = offsetTransform;
         if (materialIds != null)
         {
            MaterialIdList = new List<ElementId>(materialIds);
         }
      }

      /// <summary>
      /// Copies a BodyData object.
      /// </summary>
      /// <param name="representationHnd">The representation handle.</param>
      /// <param name="offsetTransform">The offset transform.</param>
      /// <param name="materialIds">The material ids.</param>
      public BodyData(BodyData bodyData)
      {
         RepresentationHnd = bodyData.RepresentationHnd;
         ShapeRepresentationType = bodyData.ShapeRepresentationType;
         OffsetTransform = bodyData.OffsetTransform;
         MaterialIdList = bodyData.MaterialIdList;
      }

      /// <summary>
      /// Add a material id to the set of material ids.
      /// </summary>
      /// <param name="matId">The new material id.</param>
      public void AddMaterial(ElementId matId)
      {
         MaterialIdList.Add(matId);
      }

      /// <summary>
      /// Material and Profile information for IfcMaterialProfile
      /// </summary>
      public MaterialAndProfile materialAndProfile
      {
         get
         {
            if (MaterialAndProfile == null)
            {
               MaterialAndProfile = new MaterialAndProfile();
            }
            return MaterialAndProfile;
         }
         set { MaterialAndProfile = value; }
      }

      /// <summary>
      /// Static function to create a new copy of BodyData but resetting the MaterialIds
      /// </summary>
      /// <param name="bodyDataIn">the input BodyData</param>
      /// <returns>the new copy of BodyData with cleared MaterialIds</returns>
      public static BodyData Create(BodyData bodyDataIn)
      {
         BodyData retBodyData = new BodyData(bodyDataIn);   // create a new copy of bodyDataIn
         retBodyData.MaterialIdList.Clear();                // Clear the MaterialIdsList
         return retBodyData;
      }
   }
}