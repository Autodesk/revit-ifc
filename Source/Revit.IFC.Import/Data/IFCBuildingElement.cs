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
using System.Linq;
using System.Text;
using Revit.IFC.Common.Enums;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcBuildingElement.
   /// </summary>
   /// <remarks>This class is non-abstract until all derived classes are defined.</remarks>
   public class IFCBuildingElement : IFCElement
   {
      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCBuildingElement()
      {

      }

      /// <summary>
      /// Constructs an IFCBuildingElement from the IfcBuildingElement handle.
      /// </summary>
      /// <param name="ifcBuildingElement">The IfcBuildingElement handle.</param>
      protected IFCBuildingElement(IFCAnyHandle ifcBuildingElement)
      {
         Process(ifcBuildingElement);
      }

      /// <summary>
      /// Processes IfcBuildingElement attributes.
      /// </summary>
      /// <param name="ifcBuildingElement">The IfcBuildingElement handle.</param>
      protected override void Process(IFCAnyHandle ifcBuildingElement)
      {
         base.Process(ifcBuildingElement);
      }

      private static bool SchemaSupportsBuildingElementComponentAsSubType()
      {
         return (IFCImportFile.TheFile.SchemaVersion >= IFCSchemaVersion.IFC2x2 && IFCImportFile.TheFile.SchemaVersion <= IFCSchemaVersion.IFC2x3);
      }

      /// <summary>
      /// Processes an IFCBuildingElement object.
      /// </summary>
      /// <param name="ifcBuildingElement">The IfcBuildingElement handle.</param>
      /// <returns>The IFCBuildingElement object.</returns>
      public static IFCBuildingElement ProcessIFCBuildingElement(IFCAnyHandle ifcBuildingElement)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcBuildingElement))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcBuildingElement);
            return null;
         }

         IFCEntity buildingElement;
         IFCImportFile.TheFile.EntityMap.TryGetValue(ifcBuildingElement.StepId, out buildingElement);
         if (buildingElement != null)
            return (buildingElement as IFCBuildingElement);

         IFCBuildingElement newIFCBuildingElement = null;
         // other subclasses not handled yet.
         if (SchemaSupportsBuildingElementComponentAsSubType() && IFCAnyHandleUtil.IsSubTypeOf(ifcBuildingElement, IFCEntityType.IfcBuildingElementComponent))
            newIFCBuildingElement = IFCBuildingElementComponent.ProcessIFCBuildingElementComponent(ifcBuildingElement);
         else
            newIFCBuildingElement = new IFCBuildingElement(ifcBuildingElement);
         return newIFCBuildingElement;
      }
   }
}