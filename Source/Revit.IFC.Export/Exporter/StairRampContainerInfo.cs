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
using Autodesk.Revit.DB.IFC;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// The class contains the components information for stairs or ramps.
   /// </summary>
   public class StairRampContainerInfo
   {
      /// <summary>
      /// Stair or ramp handles.
      /// </summary>
      List<IFCAnyHandle> m_StairOrRampHandles;

      /// <summary>
      /// Sub components handles of stairs or ramps.
      /// </summary>
      List<List<IFCAnyHandle>> m_Components;

      /// <summary>
      /// Local placements.
      /// </summary>
      List<IFCAnyHandle> m_LocalPlacements;

      /// <summary>
      /// Constructs an StairRampContainerInfo.
      /// </summary>
      /// <param name="stairOrRampHandles">The stair or ramp handles.</param>
      /// <param name="components">The sub components.</param>
      /// <param name="localPlacements">The local placements.</param>
      private void Construct(List<IFCAnyHandle> stairOrRampHandles, List<List<IFCAnyHandle>> components, List<IFCAnyHandle> localPlacements)
      {
         m_StairOrRampHandles = stairOrRampHandles;
         m_Components = components;
         m_LocalPlacements = localPlacements;
      }

      /// <summary>
      /// Constructs an StairRampContainerInfo.
      /// </summary>
      /// <param name="stairOrRampHandles">The stair or ramp handles.</param>
      /// <param name="components">The sub components.</param>
      /// <param name="localPlacements">The local placements.</param>
      public StairRampContainerInfo(List<IFCAnyHandle> stairOrRampHandles, List<List<IFCAnyHandle>> components, List<IFCAnyHandle> localPlacements)
      {
         Construct(stairOrRampHandles, components, localPlacements);
      }

      /// <summary>
      /// Constructs an StairRampContainerInfo.
      /// </summary>
      /// <param name="stairOrRampHandle">The stair or ramp handle.</param>
      /// <param name="components">The sub components.</param>
      /// <param name="localPlacement">The local placement.</param>
      public StairRampContainerInfo(IFCAnyHandle stairOrRampHandle, List<IFCAnyHandle> components, IFCAnyHandle localPlacement)
      {
         List<IFCAnyHandle> stairOrRampHandles = new List<IFCAnyHandle>();
         stairOrRampHandles.Add(stairOrRampHandle);

         List<List<IFCAnyHandle>> componentsList = new List<List<IFCAnyHandle>>();
         componentsList.Add(components);

         List<IFCAnyHandle> localPlacemtns = new List<IFCAnyHandle>();
         localPlacemtns.Add(localPlacement);

         Construct(stairOrRampHandles, componentsList, localPlacemtns);
      }

      /// <summary>
      /// Stair or ramp handles.
      /// </summary>
      public List<IFCAnyHandle> StairOrRampHandles
      {
         get { return m_StairOrRampHandles; }
      }

      /// <summary>
      /// Sub components handles of stairs or ramps.
      /// </summary>
      public List<List<IFCAnyHandle>> Components
      {
         get { return m_Components; }
      }

      /// <summary>
      /// Local placements.
      /// </summary>
      public List<IFCAnyHandle> LocalPlacements
      {
         get { return m_LocalPlacements; }
      }

      /// <summary>
      /// Adds a sub component to the container.
      /// </summary>
      /// <param name="index">The index of the container.</param>
      /// <param name="component">The component.</param>
      public void AddComponent(int index, IFCAnyHandle component)
      {
         if (index < 0 || index >= Components.Count)
            throw new ArgumentOutOfRangeException("index");

         Components[index].Add(component);
      }
   }
}