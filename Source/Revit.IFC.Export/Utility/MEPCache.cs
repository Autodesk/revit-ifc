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
using System.Diagnostics;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of MEP handles mapping to MEP elements in Revit.
   /// </summary>
   public class MEPCache
   {
      /// <summary>
      /// The dictionary mapping from a MEP element elementId to an Ifc handle. 
      /// </summary>
      private Dictionary<ElementId, IFCAnyHandle> m_MEPElementHandleDictionary = new Dictionary<ElementId, IFCAnyHandle>();

      /// <summary>
      /// A list of connectors
      /// </summary>
      public List<ConnectorSet> MEPConnectors = new List<ConnectorSet>();

      /// <summary>
      /// A cache of elements (Ducts and Pipes) that may have coverings (Linings and/or Insulations).
      /// </summary>
      public HashSet<ElementId> CoveredElementsCache = new HashSet<ElementId>();

      /// <summary>
      /// The dictionary mapping from an exported connector to an Ifc handle. 
      /// </summary>
      public Dictionary<Connector, IFCAnyHandle> ConnectorCache = new Dictionary<Connector, IFCAnyHandle>();

      /// <summary>
      /// The dictionary mapping from an exported connector to its description string. 
      /// </summary>
      public Dictionary<Connector, string> ConnectorDescriptionCache = new Dictionary<Connector, string>();

      /// <summary>
      /// Finds the Ifc handle from the dictionary.
      /// </summary>
      /// <param name="elementId">
      /// The element elementId.
      /// </param>
      /// <returns>
      /// The Ifc handle.
      /// </returns>
      public IFCAnyHandle Find(ElementId elementId)
      {
         IFCAnyHandle handle;
         if (m_MEPElementHandleDictionary.TryGetValue(elementId, out handle))
         {
            return handle;
         }
         return null;
      }

      /// <summary>
      /// Adds the Ifc handle to the dictionary and connectors.
      /// </summary>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <param name="handle">
      /// The Ifc handle.
      /// </param>
      public void Register(Element element, IFCAnyHandle handle)
      {
         if (m_MEPElementHandleDictionary.ContainsKey(element.Id))
            return;

         m_MEPElementHandleDictionary[element.Id] = handle;

         ConnectorSet connectorts = GetConnectors(element);
         if (connectorts != null)
            MEPConnectors.Add(connectorts);

      }

      /// <summary>
      /// Provides public access specifically for getting the connectors associated to a wire.
      /// </summary>
      /// <param name="wireElement">The Wire element.</param>
      /// <returns>A set of connectors.</returns>
      /// <remarks>Wires in Revit are annotation elements that aren't currently exported.  As such, we want to get their
      /// connection information to connect the elements at each of the wire together via a bi-directional port.</remarks>
      public static ConnectorSet GetConnectorsForWire(Wire wireElement)
      {
         return GetConnectors(wireElement);
      }

      /// <summary>
      /// Gets a set of all connectors hosted by a single element.
      /// From http://thebuildingcoder.typepad.com/blog/2010/06/retrieve-mep-elements-and-connectors.html.
      /// </summary>
      /// <param name="e">The element that may host connectors</param>
      /// <returns>A set of connectors</returns>
      static ConnectorSet GetConnectors(Element e)
      {
         ConnectorSet connectors = null;

         try
         {
            if (e is FamilyInstance)
            {
               MEPModel m = ((FamilyInstance)e).MEPModel;
               if (m != null && m.ConnectorManager != null)
               {
                  connectors = m.ConnectorManager.Connectors;
               }
            }
            else if (e is Wire)
            {
               connectors = ((Wire)e).ConnectorManager.Connectors;
            }
            else if (e is MEPCurve)
            {
               connectors = ((MEPCurve)e).ConnectorManager.Connectors;
            }
         }
         catch
         {
         }

         return connectors;
      }

      public void CacheConnectorHandle(Connector connector, IFCAnyHandle handle)
      {
         if (!ConnectorCache.ContainsKey(connector) && handle != null)
         {
            ConnectorCache.Add(connector, handle);
         }
      }
   }
}
