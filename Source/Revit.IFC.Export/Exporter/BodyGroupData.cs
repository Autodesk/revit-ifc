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
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// The class contains data information for GroupElementGeometryCache in ExportBody if UseGroupsIfPossible option is used.
   /// </summary>
   public class BodyGroupData
   {
      /// <summary>
      /// The handles of the items.
      /// </summary>
      IList<IFCAnyHandle> m_Handles = null;

      /// <summary>
      /// The element id of the original group
      /// </summary>
      public IList<IFCAnyHandle> Handles
      {
         get
         {
            if (m_Handles == null)
               m_Handles = new List<IFCAnyHandle>();
            return m_Handles;
         }
         set { m_Handles = value; }
      }
   }
}