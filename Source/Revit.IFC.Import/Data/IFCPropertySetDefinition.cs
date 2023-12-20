//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
// Copyright (C) 2013  Autodesk, Inc.
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
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Properties;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcPropertySetDefinition.
   /// </summary>
   public abstract class IFCPropertySetDefinition : IFCRoot
   {
      static IDictionary<IFCEntityType, int> m_DoorWindowPanelCounters = new Dictionary<IFCEntityType, int>();

      /// <summary>
      /// Reset the counters that will keep track of the number of IfcDoorPanelProperties and IfcWindowPanelProperties this IfcObject has.
      /// </summary>
      public static void ResetCounters()
      {
         m_DoorWindowPanelCounters.Clear();
      }

      public static int GetNextCounter(IFCEntityType type)
      {
         int nextValue;
         if (!m_DoorWindowPanelCounters.TryGetValue(type, out nextValue))
            nextValue = 0;
         m_DoorWindowPanelCounters[type] = ++nextValue;
         return nextValue;
      }

      protected IFCPropertySetDefinition()
      {
      }

      /// <summary>
      /// Processes IfcPropertySetDefinition attributes.
      /// </summary>
      /// <param name="ifcRoot">The IfcPropertySetDefinition handle.</param>
      protected IFCPropertySetDefinition(IFCAnyHandle ifcPropertySet)
      {
         Process(ifcPropertySet);
      }

      /// <summary>
      /// Processes an IFCPropertySetDefinition.
      /// </summary>
      /// <param name="ifcPropertySetDefinition">The IfcPropertySetDefinition handle.</param>
      protected override void Process(IFCAnyHandle ifcPropertySetDefinition)
      {
         base.Process(ifcPropertySetDefinition);
      }

      /// <summary>
      /// Determines the default name of an IfcRoot, if any.
      /// </summary>
      /// <param name="name">The original name.</param>
      /// <returns>The default name of an IfcRoot, if any.</returns>
      protected override string GetDefaultName(string name)
      {
         return Properties.Resources.IFCUnknownPropertySet;
      }

      /// <summary>
      /// Processes an IfcPropertySetDefinition.
      /// </summary>
      /// <param name="ifcPropertySetDefinition">The IfcPropertySetDefinition handle.</param>
      /// <returns>The IFCPropertySetDefinition object.</returns>
      public static IFCPropertySetDefinition ProcessIFCPropertySetDefinition(IFCAnyHandle ifcPropertySetDefinition)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcPropertySetDefinition))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcPropertySetDefinition);
            return null;
         }

         IFCEntity propertySet;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcPropertySetDefinition.StepId, out propertySet))
            return (propertySet as IFCPropertySetDefinition);

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcPropertySetDefinition, IFCEntityType.IfcElementQuantity))
            return IFCElementQuantity.ProcessIFCElementQuantity(ifcPropertySetDefinition);
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcPropertySetDefinition, IFCEntityType.IfcPropertySet))
            return IFCPropertySet.ProcessIFCPropertySet(ifcPropertySetDefinition);
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcPropertySetDefinition, IFCEntityType.IfcDoorLiningProperties))
            return IFCDoorLiningProperties.ProcessIFCDoorLiningProperties(ifcPropertySetDefinition);
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcPropertySetDefinition, IFCEntityType.IfcDoorPanelProperties))
            return IFCDoorPanelProperties.ProcessIFCDoorPanelProperties(ifcPropertySetDefinition,
                GetNextCounter(IFCEntityType.IfcDoorPanelProperties));
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcPropertySetDefinition, IFCEntityType.IfcWindowLiningProperties))
            return IFCWindowLiningProperties.ProcessIFCWindowLiningProperties(ifcPropertySetDefinition);
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcPropertySetDefinition, IFCEntityType.IfcWindowPanelProperties))
            return IFCWindowPanelProperties.ProcessIFCWindowPanelProperties(ifcPropertySetDefinition,
                GetNextCounter(IFCEntityType.IfcWindowPanelProperties));

         Importer.TheLog.LogUnhandledSubTypeError(ifcPropertySetDefinition, IFCEntityType.IfcPropertySetDefinition, false);
         return null;
      }

      /// <summary>
      /// Create a property set for a given element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element being created.</param>
      /// <param name="parameterGroupMap">The parameters of the element.  Cached for performance.</param>
      /// <returns>The name of the property set created, if it was created, and a Boolean value if it should be added to the property set list.</returns>
      public virtual Tuple<string, bool> CreatePropertySet(Document doc, Element element, IFCObjectDefinition objDef, 
         IFCParameterSetByGroup parameterGroupMap, ParametersToSet parametersToSet)
      {
         return new Tuple<string, bool>(null, false);
      }

      protected string CreatePropertyName(string propertyName, string typeString = "")
      {
         // Navisworks uses this engine and needs support for the old naming.
         // We use the API-only UseStreamlinedOptions as a proxy for knowing this.
         return IFCImportFile.TheFile.Options.UseStreamlinedOptions ?
            propertyName + "(" + Name + typeString +  ")" :
            Name + "." + propertyName + typeString;
      }
   }
}