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

using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.Electrical;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Used to keep a cache of MEP handles mapping to MEP elements in Revit.
   /// </summary>
   public class MEPCache
   {
      /// <summary>
      /// A cache of elements (Cable Trays and Conduits) that may be assigned to systems
      /// </summary>
      public HashSet<ElementId> CableElementsCache { get; set; } = new();

      /// <summary>
      /// The dictionary mapping from an exported connector to an Ifc handle. 
      /// </summary>
      public Dictionary<Connector, IFCAnyHandle> ConnectorCache { get; set; } = new();

      /// <summary>
      /// The dictionary mapping from an exported connector to its description string. 
      /// </summary>
      public Dictionary<Connector, string> ConnectorDescriptionCache = new();

      /// <summary>
      /// A cache of elements (Ducts and Pipes) that may have coverings (Linings and/or Insulations) and their categories.
      /// </summary>
      public Dictionary<ElementId, ElementId> CoveredElementsCache { get; set; } = new();

      /// <summary>
      /// A list of connectors
      /// </summary>
      public List<ConnectorSet> MEPConnectors { get; set; } = new();

      /// <summary>
      /// The dictionary mapping from a MEP element elementId to an Ifc handle. 
      /// </summary>
      /// <remarks>ElementId can't be used as a key for a SortedDictionary.</remarks>
      private SortedDictionary<long, IFCAnyHandle> MEPElementHandleDictionary { get; set; } = new();

      /// <summary>
      /// Finds the IFC handle from the dictionary.
      /// </summary>
      /// <param name="elementId">The element elementId.</param>
      /// <returns>The IFC handle.</returns>
      public IFCAnyHandle Find(ElementId elementId)
      {
         IFCAnyHandle handle;
         if (MEPElementHandleDictionary.TryGetValue(elementId.Value, out handle))
         {
            return handle;
         }
         return null;
      }

      /// <summary>
      /// Adds the IFC handle to the dictionary and connectors.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="handle">The IFC handle.</param>
      public void Register(Element element, IFCAnyHandle handle)
      {
         long idVal = element.Id.Value;
         if (MEPElementHandleDictionary.ContainsKey(idVal))
            return;

         MEPElementHandleDictionary[idVal] = handle;

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
      /// Modified from http://thebuildingcoder.typepad.com/blog/2010/06/retrieve-mep-elements-and-connectors.html.
      /// </summary>
      /// <param name="element">The element that may host connectors</param>
      /// <returns>A set of connectors</returns>
      static ConnectorSet GetConnectors(Element element)
      {
         try
         {
            if (element is FamilyInstance)
            {
               return (element as FamilyInstance)?.MEPModel?.ConnectorManager?.Connectors;
            }

            if (element is Wire)
            {
               return (element as Wire)?.ConnectorManager?.Connectors;
            }

            if (element is MEPCurve)
            {
               return (element as MEPCurve)?.ConnectorManager?.Connectors;
            }
         }
         catch
         {
         }

         return null;
      }

      public void CacheConnectorHandle(Connector connector, IFCAnyHandle handle)
      {
         if (handle != null && !ConnectorCache.ContainsKey(connector))
         {
            ConnectorCache.Add(connector, handle);
         }
      }

      public void Clear()
      {
         CableElementsCache.Clear();
         ConnectorCache.Clear();
         ConnectorDescriptionCache.Clear();
         CoveredElementsCache.Clear();
         MEPConnectors.Clear();
         MEPElementHandleDictionary.Clear();
      }
   }
}
