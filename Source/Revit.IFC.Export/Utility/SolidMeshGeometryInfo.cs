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

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// A solid with extra pertinant information.
   /// </summary>
   public class SolidInfo
   {
      /// <summary>
      /// The constructor.
      /// </summary>
      /// <param name="solid">The solid.</param>
      /// <param name="ownerElement">The optional owner element for this solid.</param>
      public SolidInfo(Solid solid, Element ownerElement)
      {
         Solid = solid;
         OwnerElement = ownerElement;
      }

      /// <summary>
      /// The contained solid.
      /// </summary>
      public Solid Solid { get; protected set; }

      /// <summary>
      /// The element that contains the solid in its GeometryElement.
      /// This is optional, and can be unset (null).
      /// </summary>
      public Element OwnerElement { get; protected set; }
   }

   /// <summary>
   /// A container class for lists of solids and meshes.
   /// </summary>
   /// <remarks>
   /// Added in 2013, this is a migration from the IFCSolidMeshGeometryInfo class 
   /// that is used within the BeamExporter, BodyExporter, WallExporter, 
   /// FamilyInstanceExporter and RepresentationUtil classes.
   /// </remarks>
   public class SolidMeshGeometryInfo
   {
      // A list of collected solids, and the external element (if any) that generated them.
      // In general, this will be this will be non-null if the geometry come from an
      // Instance/Symbol pair, and will be the element that contains the geometry
      // that the Instance is pointing to.
      private IList<SolidInfo> m_SolidInfoList;

      // A list of collected meshes.
      private List<Mesh> m_MeshesList;

      /// <summary>
      /// Creates a default SolidMeshGeometryInfo with empty solidsList and meshesList. 
      /// </summary>
      public SolidMeshGeometryInfo()
      {
         m_SolidInfoList = new List<SolidInfo>();
         m_MeshesList = new List<Mesh>();
      }

      /// <summary>
      /// Appends a given Solid to the solidsList.
      /// </summary>
      /// <param name="geomElem">
      /// The Solid we are appending to the solidsList.
      /// </param>
      public void AddSolid(Solid solidToAdd, Element externalElement)
      {
         m_SolidInfoList.Add(new SolidInfo(solidToAdd, externalElement));
      }

      /// <summary>
      /// Appends a given Mesh to the meshesList. 
      /// </summary>
      /// <param name="meshToAdd">
      /// The Mesh we are appending to the meshesList.
      /// </param>
      public void AddMesh(Mesh meshToAdd)
      {
         m_MeshesList.Add(meshToAdd);
      }

      /// <summary>
      /// Returns the list of Solids and their generating external elements. 
      /// </summary>
      /// <remarks>We return a List instead of an IList for the AddRange functionality.</remarks>
      public List<Solid> GetSolids()
      {
         List<Solid> solids = new List<Solid>();
         foreach (SolidInfo solidInfo in m_SolidInfoList)
         {
            solids.Add(solidInfo.Solid);
         }
         return solids;
      }

      /// <summary>
      /// Returns the list of Solids and their generating external elements. 
      /// </summary>
      public IList<SolidInfo> GetSolidInfos()
      {
         return m_SolidInfoList;
      }
   
      /// <summary>
      /// Returns the list of Meshes. 
      /// </summary>
      public List<Mesh> GetMeshes()
      {
         return m_MeshesList;
      }

      /// <summary>
      /// Returns the number of Solids in solidsList.
      /// </summary>
      public int SolidsCount()
      {
         return m_SolidInfoList.Count;
      }

      /// <summary>
      /// Returns the number of Meshes in meshesList.
      /// </summary>
      public int MeshesCount()
      {
         return m_MeshesList.Count;
      }

      /// <summary>
      /// This method takes the solidsList and clips all of its solids between the given range.
      /// </summary>
      /// <param name="elem">The Element from which we obtain our BoundingBoxXYZ.</param>
      /// <param name="geomElem">The top-level GeometryElement from which to gather X and Y 
      /// coordinates for the intersecting solid.</param>
      /// <param name="range">The IFCRange whose Z values we use to create an intersecting 
      /// solid to clip the solids in this class's internal solidsList.
      /// If range boundaries are equal, method returns, performing no clippings.</param>
      public void ClipSolidsList(GeometryElement geomElem, IFCRange range)
      {
         if (geomElem == null)
         {
            throw new ArgumentNullException("geomElemToUse");
         }

         if (MathUtil.IsAlmostEqual(range.Start, range.End) || SolidsCount() == 0)
         {
            return;
         }

         double bottomZ;
         double boundDifference;
         if (range.Start < range.End)
         {
            bottomZ = range.Start;
            boundDifference = range.End - range.Start;
         }
         else
         {
            bottomZ = range.End;
            boundDifference = range.Start - range.End;
         }

         // create a new solid using the X and Y of the bounding box on the top level GeometryElement and the Z of the IFCRange
         BoundingBoxXYZ elemBoundingBox = geomElem.GetBoundingBox();
         XYZ pointA = new XYZ(elemBoundingBox.Min.X, elemBoundingBox.Min.Y, bottomZ);
         XYZ pointB = new XYZ(elemBoundingBox.Max.X, elemBoundingBox.Min.Y, bottomZ);
         XYZ pointC = new XYZ(elemBoundingBox.Max.X, elemBoundingBox.Max.Y, bottomZ);
         XYZ pointD = new XYZ(elemBoundingBox.Min.X, elemBoundingBox.Max.Y, bottomZ);

         List<Curve> perimeter = new List<Curve>();

         try
         {
            perimeter.Add(Line.CreateBound(pointA, pointB));
            perimeter.Add(Line.CreateBound(pointB, pointC));
            perimeter.Add(Line.CreateBound(pointC, pointD));
            perimeter.Add(Line.CreateBound(pointD, pointA));
         }
         catch
         {
            // One of the boundary lines was invalid.  Do nothing.
            return;
         }

         List<CurveLoop> boxPerimeterList = new List<CurveLoop>();
         boxPerimeterList.Add(CurveLoop.Create(perimeter));
         Solid intersectionSolid = GeometryCreationUtilities.CreateExtrusionGeometry(boxPerimeterList, XYZ.BasisZ, boundDifference);

         // cycle through the elements in solidsList and intersect them against intersectionSolid to create a new list
         List<SolidInfo> clippedSolidsList = new List<SolidInfo>();
         Solid currSolid;

         foreach (SolidInfo solidAndElement in m_SolidInfoList)
         {
            Solid solid = solidAndElement.Solid;

            try
            {
               // ExecuteBooleanOperation can throw if it fails.  In this case, just ignore the clipping.
               currSolid = BooleanOperationsUtils.ExecuteBooleanOperation(solid, intersectionSolid, BooleanOperationsType.Intersect);
               if (currSolid != null && currSolid.Volume != 0)
               {
                  clippedSolidsList.Add(new SolidInfo(currSolid, solidAndElement.OwnerElement));
               }
            }
            catch
            {
               // unable to perform intersection, add original solid instead
               clippedSolidsList.Add(solidAndElement);
            }
         }

         m_SolidInfoList = clippedSolidsList;
      }

      /// <summary>
      /// Splits any solid volumes which consist of multiple closed bodies into individual solids (and updates the storage accordingly).
      /// </summary>
      public void SplitSolidsList()
      {
         IList<SolidInfo> splitSolidsList = new List<SolidInfo>();

         foreach (SolidInfo solidInfo in m_SolidInfoList)
         {
            Element element = solidInfo.OwnerElement;
            IList<Solid> splitSolids = GeometryUtil.SplitVolumes(solidInfo.Solid);
            foreach (Solid splitSolid in splitSolids)
            {
               splitSolidsList.Add(new SolidInfo(splitSolid, element));
            }
         }

         m_SolidInfoList = splitSolidsList;
      }
   }
}