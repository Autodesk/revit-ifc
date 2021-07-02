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
using System.Diagnostics;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit;

using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export MEP Connectors.
   /// </summary>
   class ConnectorExporter
   {
      private static IDictionary<IFCAnyHandle, IList<IFCAnyHandle>> m_NestedMembershipDict = new Dictionary<IFCAnyHandle, IList<IFCAnyHandle>>();

      private static IDictionary<string, IList<Toolkit.IFC4.IFCDistributionSystem>> m_SystemClassificationToIFC;

      /// <summary>
      /// Exports a connector instance. Almost verbatim exmaple from Revit 2012 API for Connector Class
      /// Works only for HVAC and Piping for now
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      public static void Export(ExporterIFC exporterIFC)
      {
         foreach (ConnectorSet connectorSet in ExporterCacheManager.MEPCache.MEPConnectors)
         {
            Export(exporterIFC, connectorSet);
         }

         foreach (KeyValuePair<Connector, IFCAnyHandle> connector in ExporterCacheManager.MEPCache.ConnectorCache)
         {
            ExportConnectorProperties(exporterIFC, connector.Key, connector.Value);
         }


         // Create all the IfcRelNests relationships from the Dictionary for Port connection in IFC4
         CreateRelNestsFromCache(exporterIFC.GetFile());

         // clear local cache 
         ConnectorExporter.ClearConnections();
      }

      // If originalConnector != null, use that connector for AddConnection routine, instead of connector.
      private static void ProcessConnections(ExporterIFC exporterIFC, Connector connector, Connector originalConnector)
      {
         // Port connection is not allowed for IFC4RV MVD
         bool isIFC4AndAbove = !ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4;

         Domain domain = connector.Domain;
         bool isElectricalDomain = (domain == Domain.DomainElectrical);
         bool supportsDirection = (domain == Domain.DomainHvac || domain == Domain.DomainPiping);

         ConnectorType connectorType = connector.ConnectorType;
         if (connectorType == ConnectorType.End ||
            connectorType == ConnectorType.Curve ||
            connectorType == ConnectorType.Physical)
         {

            Connector originalConnectorToUse = (originalConnector != null) ? originalConnector : connector;
            FlowDirectionType flowDirection = supportsDirection ? connector.Direction : FlowDirectionType.Bidirectional;
            bool isBiDirectional = (flowDirection == FlowDirectionType.Bidirectional);
            if (connector.IsConnected)
            {
               ConnectorSet connectorSet = connector.AllRefs;
               ConnectorSetIterator csi = connectorSet.ForwardIterator();

               while (csi.MoveNext())
               {
                  Connector connected = csi.Current as Connector;
                  if (connected != null && connected.Owner != null && connector.Owner != null)
                  {
                     if (connected.Owner.Id != connector.Owner.Id)
                     {
                        // look for physical connections
                        ConnectorType connectedType = connected.ConnectorType;
                        if (connectedType == ConnectorType.End ||
                           connectedType == ConnectorType.Curve ||
                           connectedType == ConnectorType.Physical)
                        {
                           if (flowDirection == FlowDirectionType.Out)
                           {
                              AddConnection(exporterIFC, connected, originalConnectorToUse, false, isElectricalDomain);
                           }
                           else
                           {
                              AddConnection(exporterIFC, originalConnectorToUse, connected, isBiDirectional, isElectricalDomain);
                           }
                        }
                     }
                  }
               }
            }
            else
            {
               string guid = GUIDUtil.CreateGUID();
               IFCFlowDirection flowDir = (isBiDirectional) ? IFCFlowDirection.SourceAndSink : (flowDirection == FlowDirectionType.Out ? IFCFlowDirection.Source : IFCFlowDirection.Sink);
               Element hostElement = connector.Owner;
               IFCAnyHandle hostElementIFCHandle = ExporterCacheManager.MEPCache.Find(hostElement.Id);

               //if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && !(IFCAnyHandleUtil.IsSubTypeOf(hostElementIFCHandle, IFCEntityType.IfcDistributionElement)))
               //   return;

               IFCAnyHandle localPlacement = CreateLocalPlacementForConnector(exporterIFC, connector, hostElementIFCHandle, flowDir);
               IFCFile ifcFile = exporterIFC.GetFile();
               IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
               IFCAnyHandle port = IFCInstanceExporter.CreateDistributionPort(exporterIFC, null, guid, ownerHistory, localPlacement, null, flowDir);
               string portName = "Port_" + hostElement.Id;
               string portType = "Flow";   // Assigned as Port.Description
               ExporterCacheManager.MEPCache.CacheConnectorHandle(connector, port);
               SetDistributionPortAttributes(port, connector, portName, portType);

               // Attach the port to the element
               guid = GUIDUtil.CreateGUID();
               string connectionName = hostElement.Id + "|" + guid;
               IFCAnyHandle connectorHandle = null;

               // Port connection is changed in IFC4 to use IfcRelNests for static connection. IfcRelConnectsPortToElement is used for a dynamic connection and it is restricted to IfcDistributionElement
               // The following code collects the ports that are nested to the object to be assigned later
               if (isIFC4AndAbove)
                  AddNestedMembership(hostElementIFCHandle, port);
               else
                  connectorHandle = IFCInstanceExporter.CreateRelConnectsPortToElement(ifcFile, guid, ownerHistory, connectionName, portType, port, hostElementIFCHandle);

               HashSet<MEPSystem> systemList = new HashSet<MEPSystem>();
               try
               {
                  MEPSystem system = connector.MEPSystem;
                  if (system != null)
                     systemList.Add(system);
               }
               catch
               {
               }

               if (isElectricalDomain)
               {
                  foreach (MEPSystem system in systemList)
                  {
                     ExporterCacheManager.SystemsCache.AddElectricalSystem(system.Id);
                     ExporterCacheManager.SystemsCache.AddHandleToElectricalSystem(system.Id, hostElementIFCHandle);
                     ExporterCacheManager.SystemsCache.AddHandleToElectricalSystem(system.Id, port);
                  }
               }
               else
               {
                  foreach (MEPSystem system in systemList)
                  {
                     ExporterCacheManager.SystemsCache.AddHandleToBuiltInSystem(system, hostElementIFCHandle);
                     ExporterCacheManager.SystemsCache.AddHandleToBuiltInSystem(system, port);
                  }
               }
            }
         }
      }

      /// <summary>
      /// Exports a connector instance. Almost verbatim exmaple from Revit 2012 API for Connector Class
      /// Works only for HVAC and Piping for now
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="connectors">The ConnectorSet object.</param>
      private static void Export(ExporterIFC exporterIFC, ConnectorSet connectors)
      {
         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction tr = new IFCTransaction(file))
         {
            foreach (Connector connector in connectors)
            {
               try
               {
                  if (connector != null)
                     ProcessConnections(exporterIFC, connector, null);
               }
               catch (System.Exception)
               {
                  // Log an error here
               }
            }
            tr.Commit();
         }
      }

      static IFCAnyHandle CreateLocalPlacementForConnector(ExporterIFC exporterIFC, Connector connector, IFCAnyHandle elementHandle,
         IFCFlowDirection flowDir)
      {
         try
         {
            IFCFile file = exporterIFC.GetFile();

            IFCAnyHandle elementPlacement = IFCAnyHandleUtil.GetObjectPlacement(elementHandle);
            Transform origTrf = ExporterIFCUtils.GetUnscaledTransform(exporterIFC, elementPlacement);

            Transform connectorCoordinateSystem = connector.CoordinateSystem;
            if (flowDir == IFCFlowDirection.Sink)
            {
               // Reverse the direction of the connector.
               connectorCoordinateSystem.BasisX = -connectorCoordinateSystem.BasisX;
               connectorCoordinateSystem.BasisZ = -connectorCoordinateSystem.BasisZ;
            }

            Transform relTransform = origTrf.Inverse.Multiply(connectorCoordinateSystem);
            XYZ scaledOrigin = UnitUtil.ScaleLength(relTransform.Origin);

            IFCAnyHandle relLocalPlacement = ExporterUtil.CreateAxis2Placement3D(file,
               scaledOrigin, relTransform.BasisZ, relTransform.BasisX);

            return IFCInstanceExporter.CreateLocalPlacement(file, elementPlacement, relLocalPlacement);
         }
         catch
         {

         }
         return null;
      }

      static void AddConnection(ExporterIFC exporterIFC, Connector connector, Connector connected, bool isBiDirectional, bool isElectricalDomain)
      {
         // Port connection is changed in IFC4 to use IfcRelNests for static connection. IfcRelConnectsPortToElement is used for a dynamic connection and it is restricted to IfcDistributionElement
         bool isIFC4AndAbove = !ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4;

         Element inElement = connector.Owner;
         Element outElement = connected.Owner;

         if (isElectricalDomain)
         {
            // We may get a connection back to the original element.  Ignore it.
            if (inElement.Id == outElement.Id)
               return;

            // Check the outElement to see if it is a Wire; if so, get its connections and "skip" the wire.
            if (outElement is Wire)
            {
               if (m_ProcessedWires.Contains(outElement.Id))
                  return;
               m_ProcessedWires.Add(outElement.Id);

               try
               {
                  ConnectorSet wireConnectorSet = MEPCache.GetConnectorsForWire(outElement as Wire);
                  if (wireConnectorSet != null)
                  {
                     foreach (Connector connectedToWire in wireConnectorSet)
                        ProcessConnections(exporterIFC, connectedToWire, connector);
                  }
               }
               catch
               {
               }
               return;
            }
         }

         // Check if the connection already exist
         if (ConnectionExists(inElement.Id, outElement.Id))
            return;

         if (isBiDirectional)
         {
            if (ConnectionExists(outElement.Id, inElement.Id))
               return;
         }

         IFCAnyHandle inElementIFCHandle = ExporterCacheManager.MEPCache.Find(inElement.Id);
         IFCAnyHandle outElementIFCHandle = ExporterCacheManager.MEPCache.Find(outElement.Id);

         // Note: In IFC4 the IfcRelConnectsPortToElement should be used for a dynamic connection. The static connection should use IfcRelNests
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            if (inElementIFCHandle == null || outElementIFCHandle == null ||
               !IFCAnyHandleUtil.IsSubTypeOf(inElementIFCHandle, IFCEntityType.IfcObjectDefinition)
               || !IFCAnyHandleUtil.IsSubTypeOf(outElementIFCHandle, IFCEntityType.IfcObjectDefinition))
               return;
         }
         else
         {
            if (inElementIFCHandle == null || outElementIFCHandle == null ||
               !IFCAnyHandleUtil.IsSubTypeOf(inElementIFCHandle, IFCEntityType.IfcElement)
               || !IFCAnyHandleUtil.IsSubTypeOf(outElementIFCHandle, IFCEntityType.IfcElement))
               return;
         }

         IFCFile ifcFile = exporterIFC.GetFile();
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
         IFCAnyHandle portOut = null;
         IFCAnyHandle portIn = null;
         // ----------------------- In Port ----------------------
         {
            string guid = GUIDUtil.CreateGUID();
            IFCFlowDirection flowDir = (isBiDirectional) ? IFCFlowDirection.SourceAndSink : IFCFlowDirection.Sink;

            IFCAnyHandle localPlacement = CreateLocalPlacementForConnector(exporterIFC, connector, inElementIFCHandle, flowDir);            

            portIn = IFCInstanceExporter.CreateDistributionPort(exporterIFC, null, guid, ownerHistory, localPlacement, null, flowDir);
            string portName = "InPort_" + inElement.Id;
            string portType = "Flow";   // Assigned as Port.Description
            ExporterCacheManager.MEPCache.CacheConnectorHandle(connector, portIn);            
            SetDistributionPortAttributes(portIn, connector, portName, portType);

            // Attach the port to the element
            guid = GUIDUtil.CreateGUID();
            string connectionName = inElement.Id + "|" + guid;

            // Port connection is changed in IFC4 to use IfcRelNests for static connection. IfcRelConnectsPortToElement is used for a dynamic connection and it is restricted to IfcDistributionElement
            // The following code collects the ports that are nested to the object to be assigned later
            if (isIFC4AndAbove)
               AddNestedMembership(inElementIFCHandle, portIn);
            else
               IFCInstanceExporter.CreateRelConnectsPortToElement(ifcFile, guid, ownerHistory, connectionName, portType, portIn, inElementIFCHandle);
         }

         // ----------------------- Out Port----------------------
         {
            string guid = GUIDUtil.CreateGUID();
            IFCFlowDirection flowDir = (isBiDirectional) ? IFCFlowDirection.SourceAndSink : IFCFlowDirection.Source;

            IFCAnyHandle localPlacement = CreateLocalPlacementForConnector(exporterIFC, connected, outElementIFCHandle, flowDir);

            portOut = IFCInstanceExporter.CreateDistributionPort(exporterIFC, null, guid, ownerHistory, localPlacement, null, flowDir);
            string portName = "OutPort_" + outElement.Id;
            string portType = "Flow";   // Assigned as Port.Description
            ExporterCacheManager.MEPCache.CacheConnectorHandle(connected, portOut);
            SetDistributionPortAttributes(portOut, connected, portName, portType);

            // Attach the port to the element
            guid = GUIDUtil.CreateGUID();
            string connectionName = outElement.Id + "|" + guid;

            // Port connection is changed in IFC4 to use IfcRelNests for static connection. IfcRelConnectsPortToElement is used for a dynamic connection and it is restricted to IfcDistributionElement
            // The following code collects the ports that are nested to the object to be assigned later
            if (isIFC4AndAbove)
               AddNestedMembership(outElementIFCHandle, portOut);
            else
               IFCInstanceExporter.CreateRelConnectsPortToElement(ifcFile, guid, ownerHistory, connectionName, portType, portOut, outElementIFCHandle);
         }

         //  ----------------------- Out Port -> In Port ----------------------
         if (portOut != null && portIn != null)
         {
            string guid = GUIDUtil.CreateGUID();
            IFCAnyHandle realizingElement = null;
            string connectionName = ExporterUtil.GetGlobalId(portIn) + "|" + ExporterUtil.GetGlobalId(portOut);
            string connectionType = "Flow";   // Assigned as Description
            IFCInstanceExporter.CreateRelConnectsPorts(ifcFile, guid, ownerHistory, connectionName, connectionType, portIn, portOut, realizingElement);
            AddConnectionInternal(inElement.Id, outElement.Id);
         }

         // Add the handles to the connector system.
         HashSet<MEPSystem> systemList = new HashSet<MEPSystem>();
         try
         {
            MEPSystem system = connector.MEPSystem;
            if (system != null)
               systemList.Add(system);
         }
         catch
         {
         }

         if (isElectricalDomain)
         {
            foreach (MEPSystem system in systemList)
            {
               ExporterCacheManager.SystemsCache.AddElectricalSystem(system.Id);
               ExporterCacheManager.SystemsCache.AddHandleToElectricalSystem(system.Id, inElementIFCHandle);
               ExporterCacheManager.SystemsCache.AddHandleToElectricalSystem(system.Id, outElementIFCHandle);
               ExporterCacheManager.SystemsCache.AddHandleToElectricalSystem(system.Id, portIn);
               ExporterCacheManager.SystemsCache.AddHandleToElectricalSystem(system.Id, portOut);
            }
         }
         else
         {
            foreach (MEPSystem system in systemList)
            {
               ExporterCacheManager.SystemsCache.AddHandleToBuiltInSystem(system, inElementIFCHandle);
               ExporterCacheManager.SystemsCache.AddHandleToBuiltInSystem(system, outElementIFCHandle);
               ExporterCacheManager.SystemsCache.AddHandleToBuiltInSystem(system, portIn);
               ExporterCacheManager.SystemsCache.AddHandleToBuiltInSystem(system, portOut);
            }
         }
      }

      /// <summary>
      /// Keeps track of created connection to prevent duplicate connections, 
      /// might not be necessary
      /// </summary>
      private static HashSet<string> m_ConnectionExists = new HashSet<string>();

      /// <summary>
      /// Keeps track of created connection to prevent duplicate connections, 
      /// might not be necessary
      /// </summary>
      private static HashSet<ElementId> m_ProcessedWires = new HashSet<ElementId>();

      /// <summary>
      /// Checks existance of the connects
      /// </summary>
      /// <param name="inID">ElementId of the incoming Element</param>
      /// <param name="outID">ElementId of the outgoing Element</param>
      /// <returns>True if the connection exists already</returns>
      private static bool ConnectionExists(ElementId inID, ElementId outID)
      {
         string elementIdKey = inID.ToString() + "_" + outID.ToString();
         return m_ConnectionExists.Contains(elementIdKey);
      }

      /// <summary>
      /// Add new Connection
      /// </summary>
      /// <param name="inID"></param>
      /// <param name="outID"></param>
      private static void AddConnectionInternal(ElementId inID, ElementId outID)
      {
         string elementIdKey = inID.ToString() + "_" + outID.ToString();
         m_ConnectionExists.Add(elementIdKey);
      }

      /// <summary>
      /// Clear the connection cache
      /// </summary>
      public static void ClearConnections()
      {
         m_ConnectionExists.Clear();
         m_ProcessedWires.Clear();
         m_NestedMembershipDict.Clear();
      }

      private static void AddNestedMembership(IFCAnyHandle hostElement, IFCAnyHandle nestedElement)
      {
         if (m_NestedMembershipDict.ContainsKey(hostElement))
         {
            IList<IFCAnyHandle> nestedElements = m_NestedMembershipDict[hostElement];
            nestedElements.Add(nestedElement);
         }
         else
         {
            IList<IFCAnyHandle> nestedElements = new List<IFCAnyHandle>();
            nestedElements.Add(nestedElement);
            m_NestedMembershipDict.Add(hostElement, nestedElements);
         }

      }

      private static void CreateRelNestsFromCache(IFCFile file)
      {
         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
         string name = "NestedPorts";
         string description = "Flow";

         foreach (KeyValuePair<IFCAnyHandle, IList<IFCAnyHandle>> relNests in m_NestedMembershipDict)
         {
            string guid = GUIDUtil.CreateGUID();
            IFCAnyHandle ifcRelNests = IFCInstanceExporter.CreateRelNests(file, guid, ownerHistory, name, description, relNests.Key, relNests.Value);
         }
      }

      /// <summary>
      /// Reads the parameter by parsing connector's description string
      /// </summary>
      /// <param name="connector">The Connector object.</param>
      /// <param name="parameterName">The name of parameter to search.</param>
      /// <returns>String assigned to parameterName.</returns>
      public static string GetConnectorParameterFromDescription(Connector connector, string parameterName)
      {
         string parameterValue = String.Empty;
         if (String.IsNullOrEmpty(parameterName) || connector == null)
            return parameterValue;

         string parsedValue = String.Empty;

         // For the connectors of pipes or fittings we extract the parameters from the connector's owner
         // for others - from the connector itself
         Element owner = connector.Owner;
         bool isPipeConnector = owner is Pipe;
         bool isFittingConnector = owner is FamilyInstance && (owner as FamilyInstance).MEPModel is MechanicalFitting;
         if (isPipeConnector || isFittingConnector)
         {
            // Read description from the parameter with the name based on connector ID
            int connectorId = connector.Id;
            // ID's if pipe connectors are zero-based
            if (isPipeConnector)
               connectorId++;

            string descriptionParameter = "PortDescription " + connectorId.ToString();
            ParameterUtil.GetStringValueFromElementOrSymbol(owner, descriptionParameter, out parsedValue);
         }
         else
         {
            parsedValue = connector.Description;
         }
        
         if (String.IsNullOrEmpty(parsedValue))
            return parameterValue;

         int ind = parsedValue.IndexOf(parameterName + '=');
         if (ind > -1)
         {
            parsedValue = parsedValue.Substring(ind + parameterName.Length + 1);
            if (!String.IsNullOrEmpty(parsedValue))
            {
               int delimiterInd = parsedValue.IndexOf(',');
               if (delimiterInd > -1)
                  parameterValue = parsedValue.Substring(0, delimiterInd);
               else
                  parameterValue = parsedValue;
            }
         }

         return parameterValue;
      }


      /// <summary>
      /// Export porterty sets for the connector
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="connector">The connector to export properties for.</param>
      /// <param name="handle">The ifc handle of exported connector.</param>
      private static void ExportConnectorProperties(ExporterIFC exporterIFC, Connector connector, IFCAnyHandle handle)
      {
         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
            IList<IList<PropertySetDescription>> psetsToCreate = ExporterCacheManager.ParameterCache.PropertySets;
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(handle))
               return;

            IList<PropertySetDescription> currPsetsToCreate = ExporterUtil.GetCurrPSetsToCreate(handle, psetsToCreate);
            if (currPsetsToCreate.Count == 0)
               return;

            foreach (PropertySetDescription currDesc in currPsetsToCreate)
            {
               ElementOrConnector elementOrConnector = new ElementOrConnector(connector);
               ISet<IFCAnyHandle> props = currDesc.ProcessEntries(file, exporterIFC, null, elementOrConnector, null, handle);
               if (props.Count < 1)
                  continue;

               IFCAnyHandle propertySet = IFCInstanceExporter.CreatePropertySet(file, GUIDUtil.CreateGUID(), ownerHistory, currDesc.Name, currDesc.DescriptionOfSet, props);
               if (propertySet == null)
                  continue;

               HashSet<IFCAnyHandle> relatedObjects = new HashSet<IFCAnyHandle>() { handle };
               ExporterUtil.CreateRelDefinesByProperties(file, GUIDUtil.CreateGUID(), ownerHistory, null, null, relatedObjects, propertySet);
            }
            transaction.Commit();
         }
      }

      /// <summary>
      /// Set few attributes for already created distribution port
      /// </summary>
      /// <param name="port">The handle of exported connector.</param>
      /// <param name="connector">The Connector object.</param>
      /// <param name="portAutoName">The auto gerated name with id.</param>
      /// <param name="portDescription">The description string to set.</param>
      private static void SetDistributionPortAttributes(IFCAnyHandle port, Connector connector, string portAutoName, string portDescription)
      {
         // "Description"
         IFCAnyHandleUtil.SetAttribute(port, "Description", portDescription);

         // "Name" 
         string portName = ConnectorExporter.GetConnectorParameterFromDescription(connector, "PortName");
         if (String.IsNullOrEmpty(portName))
            portName = portAutoName;
         IFCAnyHandleUtil.OverrideNameAttribute(port, portName);

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            // "PredefinedType"
            Toolkit.IFC4.IFCDistributionPortType portType = GetMappedIFCDistributionPortType(connector.Domain);
            string validatedPredefinedType = IFCValidateEntry.ValidateStrEnum<Toolkit.IFC4.IFCDistributionPortType>(portType.ToString());
            IFCAnyHandleUtil.SetAttribute(port, "PredefinedType", validatedPredefinedType, true);

            // "SystemType" from description
            string systemTypeFromDescription = ConnectorExporter.GetConnectorParameterFromDescription(connector, "SystemType");
            string validatedSystemType = IFCValidateEntry.ValidateStrEnum<Toolkit.IFC4.IFCDistributionSystem>(systemTypeFromDescription);
            if (String.IsNullOrEmpty(validatedSystemType))
            {
               // "SystemType" from revit system classification
               Toolkit.IFC4.IFCDistributionSystem systemType = GetMappedIFCDistributionSystem(connector);
               validatedSystemType = IFCValidateEntry.ValidateStrEnum<Toolkit.IFC4.IFCDistributionSystem>(systemType.ToString());
            }
            if (!String.IsNullOrEmpty(validatedSystemType))
               IFCAnyHandleUtil.SetAttribute(port, "SystemType", validatedSystemType, true);
         }
      }

      /// <summary>
      /// Get ifc distribution port type from connector type domain
      /// </summary>
      /// <param name="connectorDomain">The type of connector domain.</param>
      /// <returns>ifc distribution port type.</returns>
      private static Toolkit.IFC4.IFCDistributionPortType GetMappedIFCDistributionPortType(Domain connectorDomain)
      {
         Toolkit.IFC4.IFCDistributionPortType portType = Toolkit.IFC4.IFCDistributionPortType.NOTDEFINED;
         switch (connectorDomain)
         {
            case Domain.DomainHvac:
               portType = Toolkit.IFC4.IFCDistributionPortType.DUCT;
               break;
            case Domain.DomainElectrical:
               portType = Toolkit.IFC4.IFCDistributionPortType.CABLE;
               break;
            case Domain.DomainPiping:
               portType = Toolkit.IFC4.IFCDistributionPortType.PIPE;
               break;
            case Domain.DomainCableTrayConduit:
               portType = Toolkit.IFC4.IFCDistributionPortType.CABLECARRIER;
               break;
         }
         return portType;
      }

      /// <summary>
      /// Get ifc distribution system type from connector
      /// </summary>
      /// <param name="connector">The connector.</param>
      /// <returns>ifc distribution system type.</returns>
      private static Toolkit.IFC4.IFCDistributionSystem GetMappedIFCDistributionSystem(Connector connector)
      {
         Toolkit.IFC4.IFCDistributionSystem systemType = Toolkit.IFC4.IFCDistributionSystem.NOTDEFINED;

         string systemClassificationString = "UndefinedSystemClassification";
         try
         {
            switch (connector.Domain)
            {
               case Domain.DomainHvac:
                  systemClassificationString = connector.DuctSystemType.ToString();
                  break;
               case Domain.DomainElectrical:
                  systemClassificationString = connector.ElectricalSystemType.ToString();
                  break;
               case Domain.DomainPiping:
                  systemClassificationString = connector.PipeSystemType.ToString();
                  break;
               case Domain.DomainCableTrayConduit:
                  systemClassificationString = "CableTrayConduit";
                  break;
            }

            systemType = GetSystemTypeFromDictionary(systemClassificationString);
         }
         catch
         {
         }
         return systemType;
      }

      /// <summary>
      /// Get ifc distribution system type from system
      /// </summary>
      /// <param name="systemElement">The system element.</param>
      /// <returns>ifc distribution system type.</returns>
      public static Toolkit.IFC4.IFCDistributionSystem GetMappedIFCDistributionSystemFromElement(MEPSystem systemElement)
      {
         string systemClassificationString = "UndefinedSystemClassification";

         if (systemElement is MechanicalSystem)
            systemClassificationString = (systemElement as MechanicalSystem).SystemType.ToString();
         else if (systemElement is ElectricalSystem)
            systemClassificationString = (systemElement as ElectricalSystem).SystemType.ToString();
         else if (systemElement is PipingSystem)
            systemClassificationString = (systemElement as PipingSystem).SystemType.ToString();

         return GetSystemTypeFromDictionary(systemClassificationString);
      }

      /// <summary>
      /// Get ifc distribution system type from connector's system classification string
      /// </summary>
      /// <param name="revitSystemString">The connector's system classification string.</param>
      /// <returns>ifc distribution system type.</returns>
      private static Toolkit.IFC4.IFCDistributionSystem GetSystemTypeFromDictionary(string revitSystemString)
      {
         Toolkit.IFC4.IFCDistributionSystem systemType = Toolkit.IFC4.IFCDistributionSystem.NOTDEFINED;

         if (m_SystemClassificationToIFC == null)
            InitializeSystemClassifications();

         if (m_SystemClassificationToIFC.ContainsKey(revitSystemString))
         {
            systemType = m_SystemClassificationToIFC[revitSystemString].First();
         }

         return systemType;
      }

      /// <summary>
      /// Initializes the mapping between revit system classification and ifc distribution system
      /// </summary>
      private static void InitializeSystemClassifications()
      {
         m_SystemClassificationToIFC = new Dictionary<string, IList<Toolkit.IFC4.IFCDistributionSystem>>()
         {
            { "UndefinedSystemClassification", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.NOTDEFINED,
               Toolkit.IFC4.IFCDistributionSystem.CHEMICAL,
               Toolkit.IFC4.IFCDistributionSystem.CHILLEDWATER,
               Toolkit.IFC4.IFCDistributionSystem.DISPOSAL,
               Toolkit.IFC4.IFCDistributionSystem.EARTHING,
               Toolkit.IFC4.IFCDistributionSystem.FUEL,
               Toolkit.IFC4.IFCDistributionSystem.GAS,
               Toolkit.IFC4.IFCDistributionSystem.HAZARDOUS,
               Toolkit.IFC4.IFCDistributionSystem.LIGHTNINGPROTECTION,
               Toolkit.IFC4.IFCDistributionSystem.VACUUM }
            } ,
            { "SupplyAir", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.VENTILATION }
            } ,
            { "ReturnAir", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.VENTILATION }
            } ,
            { "ExhaustAir", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.EXHAUST }
            } ,
            { "OtherAir", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.USERDEFINED,
               Toolkit.IFC4.IFCDistributionSystem.AIRCONDITIONING,
               Toolkit.IFC4.IFCDistributionSystem.COMPRESSEDAIR }
            } ,
            { "Data", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.DATA,
               Toolkit.IFC4.IFCDistributionSystem.SIGNAL }
            } ,
            { "PowerCircuit", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.ELECTRICAL,
               Toolkit.IFC4.IFCDistributionSystem.LIGHTING }
            } ,
            { "SupplyHydronic", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.HEATING }
            } ,
            { "ReturnHydronic", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.HEATING }
            } ,
            { "Telephone", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.COMMUNICATION,
               Toolkit.IFC4.IFCDistributionSystem.ELECTROACOUSTIC }
            } ,
            { "Security", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.SECURITY }
            } ,
            { "FireAlarm", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.ELECTROACOUSTIC }
            } ,
            { "NurseCall", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.COMMUNICATION }
            } ,
            { "Controls", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.CONTROL }
            } ,
            { "Communication", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.VENTILATION,
               Toolkit.IFC4.IFCDistributionSystem.AUDIOVISUAL,
               Toolkit.IFC4.IFCDistributionSystem.TV }
            } ,
            { "CondensateDrain", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.CONDENSERWATER }
            } ,
            { "Sanitary", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.SEWAGE,
               Toolkit.IFC4.IFCDistributionSystem.WASTEWATER }
            } ,
            { "Vent", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.VENT }
            } ,
            { "Storm", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.STORMWATER,
               Toolkit.IFC4.IFCDistributionSystem.RAINWATER,
               Toolkit.IFC4.IFCDistributionSystem.DRAINAGE }
            } ,
            { "DomesticHotWater", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.DOMESTICHOTWATER }
            } ,
            { "DomesticColdWater", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.DOMESTICCOLDWATER,
               Toolkit.IFC4.IFCDistributionSystem.WATERSUPPLY }
            } ,
            { "Recirculation", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.NOTDEFINED }
            } ,
            { "OtherPipe", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.USERDEFINED }
            } ,
            { "FireProtectWet", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.FIREPROTECTION }
            } ,
            { "FireProtectDry", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.FIREPROTECTION }
            } ,
            { "FireProtectPreaction", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.FIREPROTECTION }
            } ,
            { "FireProtectOther", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.USERDEFINED }
            } ,
            { "SwitchTopology", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.CONTROL}
            } ,
            { "PowerBalanced", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.ELECTRICAL }
            } ,
            { "PowerUnBalanced", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.ELECTRICAL }
            } ,
            { "CableTrayConduit", new List<Toolkit.IFC4.IFCDistributionSystem>
               {
               Toolkit.IFC4.IFCDistributionSystem.CONVEYING }
            }
         };
      }

   }   
}
