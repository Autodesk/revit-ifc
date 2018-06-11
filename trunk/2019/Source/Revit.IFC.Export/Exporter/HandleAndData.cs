﻿//
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
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// A simple class to store both handle and ExtrusionCreationData.
   /// </summary>
   public class HandleAndData
   {
      /// <summary>
      /// The handle of the created representation.
      /// </summary>
      public IFCAnyHandle Handle = null;

      /// <summary>
      /// The type of shape representation created.
      /// </summary>
      public ShapeRepresentationType ShapeRepresentationType = ShapeRepresentationType.Undefined;

      /// <summary>
      /// The extra parameters for the extrusion.
      /// </summary>
      public IFCExtrusionCreationData Data = null;

      /// <summary>
      /// The material ids for the extrusion.
      /// </summary>
      public HashSet<ElementId> MaterialIds = null;

      /// <summary>
      /// The handles that represent the base representation items inside the final shape representation, without any openings or clippings.
      /// In general, these are extrusions, but could be triangulated face sets for the Reference View.
      /// </summary>
      public IList<IFCAnyHandle> BaseRepresentationItems = null;

      /// <summary>
      /// A handle for the Footprint representation
      /// </summary>
      public FootPrintInfo FootprintInfo = null;

      /// <summary>
      /// A Dictionary for Material Profile
      /// </summary>
      public MaterialAndProfile MaterialAndProfile = null;
   }
}