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
   /// Represents an IfcBuildingElementComponent.
   /// </summary>
   /// <remarks>This class is non-abstract until all derived classes are defined.</remarks>
   public class IFCBuildingElementComponent : IFCBuildingElement
   {
      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCBuildingElementComponent()
      {

      }

      /// <summary>
      /// Constructs an IFCBuildingElementComponent from the IfcBuildingElementComponent handle.
      /// </summary>
      /// <param name="ifcBuildingElementComponent">The IfcBuildingElementComponent handle.</param>
      protected IFCBuildingElementComponent(IFCAnyHandle ifcBuildingElementComponent)
      {
         Process(ifcBuildingElementComponent);
      }

      /// <summary>
      /// Processes IfcBuildingElementComponent attributes.
      /// </summary>
      /// <param name="ifcBuildingElementComponent">The IfcBuildingElementComponent handle.</param>
      protected override void Process(IFCAnyHandle ifcBuildingElementComponent)
      {
         base.Process(ifcBuildingElementComponent);
      }

      /// <summary>
      /// Processes an IFCBuildingElementComponent object.
      /// </summary>
      /// <param name="ifcBuildingElementComponent">The IfcBuildingElementComponent handle.</param>
      /// <returns>The IFCBuildingElementComponent object.</returns>
      public static IFCBuildingElementComponent ProcessIFCBuildingElementComponent(IFCAnyHandle ifcBuildingElementComponent)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcBuildingElementComponent))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcBuildingElementComponent);
            return null;
         }

         IFCEntity buildingElementComponent;
         IFCImportFile.TheFile.EntityMap.TryGetValue(ifcBuildingElementComponent.StepId, out buildingElementComponent);
         if (buildingElementComponent != null)
            return (buildingElementComponent as IFCBuildingElementComponent);

         IFCBuildingElementComponent newIFCBuildingElementComponent = null;
         // other subclasses not handled yet.
         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcBuildingElementComponent, IFCEntityType.IfcBuildingElementPart))
            newIFCBuildingElementComponent = IFCBuildingElementPart.ProcessIFCBuildingElementPart(ifcBuildingElementComponent);
         else
            newIFCBuildingElementComponent = new IFCBuildingElementComponent(ifcBuildingElementComponent);
         return newIFCBuildingElementComponent;
      }
   }
}