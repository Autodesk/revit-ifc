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
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of systems and the elements contained in them.
   /// </summary>
   public class SystemsCache
   {
      private IDictionary<ElementId, ISet<IFCAnyHandle>> m_BuiltInSystemsCache;
      private IDictionary<string, ICollection<IFCAnyHandle>> m_CustomSystemsCache;
      private IDictionary<ElementId, ISet<IFCAnyHandle>> m_ElectricalSystemsCache;

      /// <summary>
      /// Creates a new SystemsCache.
      /// </summary>
      public SystemsCache()
      {
         m_BuiltInSystemsCache = new Dictionary<ElementId, ISet<IFCAnyHandle>>();
         m_CustomSystemsCache = new Dictionary<string, ICollection<IFCAnyHandle>>();
         m_ElectricalSystemsCache = new Dictionary<ElementId, ISet<IFCAnyHandle>>();
      }

      /// <summary>
      /// Get the list of systems.
      /// </summary>
      public IDictionary<ElementId, ISet<IFCAnyHandle>> BuiltInSystemsCache
      {
         get { return m_BuiltInSystemsCache; }
      }

      /// <summary>
      /// Get the list of Electrical systems.  The members will be determined at the end of export.
      /// </summary>
      public IDictionary<ElementId, ISet<IFCAnyHandle>> ElectricalSystemsCache
      {
         get { return m_ElectricalSystemsCache; }
      }

      /// <summary>
      /// Get the list of custom systems.
      /// </summary>
      public IDictionary<string, ICollection<IFCAnyHandle>> CustomSystemsCache
      {
         get { return m_CustomSystemsCache; }
      }

      /// <summary>
      /// Gets a custom system to the custom systems list.
      /// </summary>
      /// <param name="systemName">The system name.</param>
      /// <returns>The value of the system.</returns>
      private ICollection<IFCAnyHandle> GetCustomSystem(string systemName)
      {
         ICollection<IFCAnyHandle> systemValue;
         if (!CustomSystemsCache.TryGetValue(systemName, out systemValue))
         {
            systemValue = new HashSet<IFCAnyHandle>();
            CustomSystemsCache.Add(new KeyValuePair<string, ICollection<IFCAnyHandle>>(systemName, systemValue));
         }
         return systemValue;
      }

      /// <summary>
      /// Gets a system from the systems list.
      /// </summary>
      /// <param name="systemElement">The Revit System element.</param>
      /// <returns>The new system container.</returns>
      private ICollection<IFCAnyHandle> GetSystem(Element systemElement)
      {
         if (systemElement == null)
            throw new ArgumentNullException("systemElement");

         ISet<IFCAnyHandle> system;
         if (!BuiltInSystemsCache.TryGetValue(systemElement.Id, out system))
         {
            system = new HashSet<IFCAnyHandle>();
            BuiltInSystemsCache.Add(new KeyValuePair<ElementId, ISet<IFCAnyHandle>>(systemElement.Id, system));
         }

         return system;
      }

      /// <summary>
      /// Adds a handle to a built-in system.
      /// </summary>
      /// <param name="systemElement">The Revit system element.</param>
      /// <param name="handle">The handle.</param>
      public void AddHandleToBuiltInSystem(Element systemElement, IFCAnyHandle handle)
      {
         if (systemElement == null)
            throw new ArgumentNullException("systemElement");

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(handle))
            return;

         ICollection<IFCAnyHandle> subSystem = GetSystem(systemElement);
         if (subSystem == null)
            throw new InvalidOperationException("Error getting system.");
         subSystem.Add(handle);
      }

      /// <summary>
      /// Adds a handle to a custom system.
      /// </summary>
      /// <param name="systemName">The new system.</param>
      /// <param name="newSystem">The Revit System element.</param>
      public void AddHandleToCustomSystem(string customSystemName, IFCAnyHandle handle)
      {
         ICollection<IFCAnyHandle> system = GetCustomSystem(customSystemName);
         if (system == null)
            throw new InvalidOperationException("Error getting system.");
         system.Add(handle);
      }

      /// <summary>
      /// Adds an electrical system by Element id, if it doesn't already exist.
      /// </summary>
      /// <param name="systemId">The system element Id.</param>
      public void AddElectricalSystem(ElementId systemId)
      {
         if (!ElectricalSystemsCache.ContainsKey(systemId))
         {
            KeyValuePair<ElementId, ISet<IFCAnyHandle>> entry = new KeyValuePair<ElementId, ISet<IFCAnyHandle>>(systemId, new HashSet<IFCAnyHandle>());
            ElectricalSystemsCache.Add(entry);
         }
      }

      /// <summary>
      /// Adds a handle to an existing electrical system.
      /// </summary>
      /// <param name="systemId">The system element Id.</param>
      /// <param name="handle">The entity handle.</param>
      public void AddHandleToElectricalSystem(ElementId systemId, IFCAnyHandle handle)
      {
         if (!ElectricalSystemsCache.ContainsKey(systemId))
            throw new InvalidOperationException("Error getting system.");

         ElectricalSystemsCache[systemId].Add(handle);
      }
   }
}