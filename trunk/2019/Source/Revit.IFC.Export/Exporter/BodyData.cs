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
   /// The class contains output information from ExportBody
   /// </summary>
   public class BodyData
   {
      /// <summary>
      /// The representation handle.
      /// </summary>
      private IFCAnyHandle m_RepresentationHnd = null;

      /// <summary>
      /// The representation type.
      /// </summary>
      private ShapeRepresentationType m_ShapeRepresentationType = ShapeRepresentationType.Undefined;

      /// <summary>
      /// The offset transform.
      /// </summary>
      private Transform m_OffsetTransform = null;

      /// <summary>
      /// The exported material Ids
      /// </summary>
      private HashSet<ElementId> m_MaterialIds = new HashSet<ElementId>();

      /// <summary>
      /// A handle for the Footprint representation
      /// </summary>
      private FootPrintInfo m_FootprintInfo = null;

      /// <summary>
      /// A Dictionary for Material Profile
      /// </summary>
      private MaterialAndProfile m_MaterialAndProfile = null;

      /// <summary>
      /// Constructs a default BodyData object.
      /// </summary>
      public BodyData() { }

      /// <summary>
      /// Constructs a BodyData object.
      /// </summary>
      /// <param name="representationHnd">
      /// The representation handle.
      /// </param>
      /// <param name="offsetTransform">
      /// The offset transform.
      /// </param>
      /// <param name="materialIds">
      /// The material ids.
      /// </param>
      public BodyData(IFCAnyHandle representationHnd, Transform offsetTransform, HashSet<ElementId> materialIds)
      {
         this.m_RepresentationHnd = representationHnd;
         if (offsetTransform != null)
            this.m_OffsetTransform = offsetTransform;
         if (materialIds != null)
            this.m_MaterialIds = materialIds;
      }

      /// <summary>
      /// Copies a BodyData object.
      /// </summary>
      /// <param name="representationHnd">
      /// The representation handle.
      /// </param>
      /// <param name="offsetTransform">
      /// The offset transform.
      /// </param>
      /// <param name="materialIds">
      /// The material ids.
      /// </param>
      public BodyData(BodyData bodyData)
      {
         this.m_RepresentationHnd = bodyData.RepresentationHnd;
         this.m_ShapeRepresentationType = bodyData.m_ShapeRepresentationType;
         this.m_OffsetTransform = bodyData.OffsetTransform;
         this.m_MaterialIds = bodyData.MaterialIds;
      }

      /// <summary>
      /// The representation handle.
      /// </summary>
      public IFCAnyHandle RepresentationHnd
      {
         get { return m_RepresentationHnd; }
         set { m_RepresentationHnd = value; }
      }

      /// <summary>
      /// The representation type.
      /// </summary>
      public ShapeRepresentationType ShapeRepresentationType
      {
         get { return m_ShapeRepresentationType; }
         set { m_ShapeRepresentationType = value; }
      }

      /// <summary>
      /// The offset transform.
      /// </summary>
      public Transform OffsetTransform
      {
         get { return m_OffsetTransform; }
         set { m_OffsetTransform = value; }
      }

      /// <summary>
      /// The associated material ids.
      /// </summary>
      public HashSet<ElementId> MaterialIds
      {
         get { return m_MaterialIds; }
         set { m_MaterialIds = value; }
      }

      /// <summary>
      /// Add a material id to the set of material ids.
      /// </summary>
      /// <param name="matId">The new material</param>
      public void AddMaterial(ElementId matId)
      {
         MaterialIds.Add(matId);
      }

      /// <summary>
      /// Footprint Handle
      /// </summary>
      public FootPrintInfo FootprintInfo
      {
         get { return m_FootprintInfo; }
         set { m_FootprintInfo = value; }
      }

      public MaterialAndProfile materialAndProfile
      {
         get
         {
            if (m_MaterialAndProfile == null)
            {
               m_MaterialAndProfile = new MaterialAndProfile();
            }
            return m_MaterialAndProfile;
         }
         set { m_MaterialAndProfile = value; }
      }
   }
}