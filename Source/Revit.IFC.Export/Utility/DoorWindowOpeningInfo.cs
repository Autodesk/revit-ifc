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
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Contains return value for CreateOpeningForDoorWindow.
   /// </summary>
   public class DoorWindowOpeningInfo
   {
      IFCAnyHandle m_OpeningHnd = null;

      double m_OpeningHeight = -1.0;

      double m_OpeningWidth = -1.0;

      private DoorWindowOpeningInfo() { }

      /// <summary>
      /// The public Create function.
      /// </summary>
      /// <param name="hnd">The opening handle.</param>
      /// <param name="placement">The opening local placement.</param>
      /// <param name="height">The opening height.</param>
      /// <param name="width">The opening width.</param>
      /// <returns>The DoorWindowOpeningInfo class.</returns>
      static public DoorWindowOpeningInfo Create(IFCAnyHandle hnd, double height, double width)
      {
         DoorWindowOpeningInfo doorWindowOpeningInfo = new DoorWindowOpeningInfo();
         doorWindowOpeningInfo.m_OpeningHnd = hnd;
         doorWindowOpeningInfo.m_OpeningHeight = height;
         doorWindowOpeningInfo.m_OpeningWidth = width;
         return doorWindowOpeningInfo;
      }

      /// <summary>
      /// Access the opening handle.
      /// </summary>
      public IFCAnyHandle OpeningHnd { get { return m_OpeningHnd; } }

      /// <summary>
      /// Access the opening height.
      /// </summary>
      public double OpeningHeight { get { return m_OpeningHeight; } }

      /// <summary>
      /// Access the opening width.
      /// </summary>
      public double OpeningWidth { get { return m_OpeningWidth; } }
   }
}