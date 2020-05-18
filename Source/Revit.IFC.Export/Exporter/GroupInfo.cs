//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to import and export IFC files containing model geometry.
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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   public class GroupInfo
   {
      /// <summary>
      /// The Group handle.
      /// </summary>
      public IFCAnyHandle GroupHandle { get; set; } = null;

      /// <summary>
      /// The associated element handles.
      /// </summary>
      public HashSet<IFCAnyHandle> ElementHandles { get; set; } = new HashSet<IFCAnyHandle>();

      /// <summary>
      /// Group's export type.
      /// </summary>
      public IFCExportInfoPair GroupType { get; set; } = new IFCExportInfoPair(IFCEntityType.UnKnown);
   }
}