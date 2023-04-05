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

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// This class is used to pass information related to extrusion creation.
   /// </summary>
   public class IFCExportBodyParams : IDisposable
   {
      /// <summary>
      /// The extra parameters for the extrusion.
      /// </summary>
      public IFCExtrusionCreationData Data { get; set; } = new IFCExtrusionCreationData();

      /// <summary>
      /// The preferred direction to calculate width
      /// </summary>
      public XYZ PreferredWidthDirection { get; set; } = null;

      /// <summary>
      /// True if inner regions of the extrusion should become openings, false otherwise.
      /// </summary>
      public bool AreInnerRegionsOpenings
      {
         get { return Data.AreInnerRegionsOpenings; }
         set { Data.AreInnerRegionsOpenings = value; }
      }

      /// <summary>
      /// The extrusion direction to generate an extrusion.
      /// </summary>
      public XYZ ExtrusionDirection
      {
         get { return Data.ExtrusionDirection; }
         set { Data.ExtrusionDirection = value; }
      }

      /// <summary>
      /// The custom extrusion axis to try when generating an extrusion.
      /// </summary>
      public XYZ CustomAxis
      {
         get { return Data.CustomAxis; }
         set { Data.CustomAxis = value; }
      }

      /// <summary>
      /// Identifies if the data contains a extrusion direction.
      /// </summary>
      public bool HasExtrusionDirection
      {
         get { return Data.HasExtrusionDirection; }
      }

      /// <summary>
      /// Identifies if the data contains a custom extrusion axis.
      /// </summary>
      public bool HasCustomAxis
      {
         get { return Data.HasCustomAxis; }
      }

      /// <summary>
      /// The axes to try when generating the properties of the extrusion.
      /// </summary>
      public IFCExtrusionAxes PossibleExtrusionAxes
      {
         get { return Data.PossibleExtrusionAxes; }
         set { Data.PossibleExtrusionAxes = value; }
      }

      /// <summary>
      /// Allows re-use of local placement when creating a new local placement due to shifting 
      /// of breps when moving towards the origin.
      /// </summary>
      public bool ReuseLocalPlacement
      {
         get { return Data.ReuseLocalPlacement; }
         set { Data.ReuseLocalPlacement = value; }
      }

      /// <summary>
      /// Allows vertical shifting of breps when moving towards the origin.
      /// </summary>
      public bool AllowVerticalOffsetOfBReps
      {
         get { return Data.AllowVerticalOffsetOfBReps; }
         set { Data.AllowVerticalOffsetOfBReps = value; }
      }

      /// <summary>
      /// The outer perimeter of the extrusion, scaled to the units of export.
      /// This value represents the perimeter of the outermost curve loop bounding the 
      /// area of the extrusion. Zero if the perimeter has never been set on this object.
      /// </summary>
      public double ScaledOuterPerimeter
      {
         get { return Data.ScaledOuterPerimeter; }
         set { Data.ScaledOuterPerimeter = value; }
      }

      /// <summary>
      /// The inner perimeter of the extrusion, scaled to the units of export.
      /// This value represents the perimeter of all of the inner curve loops within the
      /// area of the extrusion. Zero if the perimeter has never been set on this object.
      /// </summary>
      public double ScaledInnerPerimeter
      {
         get { return Data.ScaledInnerPerimeter; }
         set { Data.ScaledInnerPerimeter = value; }
      }

      /// <summary>
      /// The width of the extrusion, scaled to the units of export.
      /// Zero if the width has never been set on this object.
      /// </summary>
      public double ScaledWidth
      {
         get { return Data.ScaledWidth; }
         set { Data.ScaledWidth = value; }
      }

      /// <summary>
      /// The slope of the extrusion, in degrees.
      /// Zero if the slope has never been set on this object.
      /// </summary>
      public double Slope
      {
         get { return Data.Slope; }
         set { Data.Slope = value; }
      }

      /// <summary>
      /// The length of the extrusion, scaled to the units of export.
      /// Zero if the length has never been set on this object.
      /// </summary>
      public double ScaledLength
      {
         get { return Data.ScaledLength; }
         set { Data.ScaledLength = value; }
      }

      /// <summary>
      /// The height of the extrusion, scaled to the units of export.
      /// Zero if the height has never been set on this object.
      /// </summary>
      public double ScaledHeight
      {
         get { return Data.ScaledHeight; }
         set { Data.ScaledHeight = value; }
      }

      /// <summary>
      /// The area of the extrusion, scaled to the units of export.
      /// The value will be 0.0 if the area has never been set on this object.
      /// </summary>
      public double ScaledArea
      {
         get { return Data.ScaledArea; }
         set { Data.ScaledArea = value; }
      }

      /// <summary>
      /// True to create new local placement with identity transform.
      /// </summary>
      public bool ForceOffset
      {
         get { return Data.ForceOffset; }
         set { Data.ForceOffset = value; }
      }

      /// <summary>
      /// Constructs a default IFCExportBodyParams object.
      /// </summary>
      public IFCExportBodyParams() { }

      /// <summary>
      /// Constructs a IFCExportBodyParams object.
      /// </summary>
      /// <param name="extrusionCreationData">The extrusion creation data.</param>
      public IFCExportBodyParams(IFCExtrusionCreationData extrusionCreationData)
      {
         Data = extrusionCreationData ?? new IFCExtrusionCreationData();
      }

      /// <summary>
      /// The Dispose function
      /// </summary>
      public void Dispose()
      {
         Data.Dispose();
      }


      /// <summary>
      /// Sets the data to reference an IfcLocalPlacement handle when creating the extrusion.
      /// Side effect: will set ReuseLocalPlacement to true.
      /// </summary>
      public void SetLocalPlacement(IFCAnyHandle localPlacement)
      {
         Data.SetLocalPlacement(localPlacement);
      }

      /// <summary>
      /// Gets the reference to the IfcLocalPlacement handle used when creating the extrusion.
      /// </summary>
      public IFCAnyHandle GetLocalPlacement()
      {
         return Data.GetLocalPlacement();
      }

      /// <summary>
      /// Gets a collection of all of the openings stored in this data.
      /// </summary>
      public IList<IFCExtrusionData> GetOpenings()
      {
         return Data.GetOpenings();
      }

      /// <summary>
      /// Adds an opening to the data.
      /// </summary>
      public void AddOpening(IFCExtrusionData data)
      {
         Data.AddOpening(data);
      }

      /// <summary>
      /// Removes all cached openings from the data.
      /// </summary>
      public void ClearOpenings()
      {
         Data.ClearOpenings();
      }
   }
}