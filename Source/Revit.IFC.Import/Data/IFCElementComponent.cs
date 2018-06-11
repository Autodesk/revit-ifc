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
   /// Represents an IfcElementComponent.
   /// </summary>
   /// <remarks>This class is non-abstract until all derived classes are defined.</remarks>
   public class IFCElementComponent : IFCElement
   {
      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCElementComponent()
      {

      }

      /// <summary>
      /// Constructs an IFCElementComponent from the IfcElementComponent handle.
      /// </summary>
      /// <param name="ifcElementComponent">The IfcElementComponent handle.</param>
      protected IFCElementComponent(IFCAnyHandle ifcElementComponent)
      {
         Process(ifcElementComponent);
      }

      /// <summary>
      /// Processes IfcElementComponent attributes.
      /// </summary>
      /// <param name="ifcElementComponent">The IfcElementComponent handle.</param>
      protected override void Process(IFCAnyHandle ifcElementComponent)
      {
         base.Process(ifcElementComponent);
      }

      /// <summary>
      /// Processes an IFCElementComponent object.
      /// </summary>
      /// <param name="ifcElementComponent">The IfcElementComponent handle.</param>
      /// <returns>The IFCElementComponent object.</returns>
      /// <remarks>IFCBuildingElementPart has changed from a subtype of the abstract IFCBuildingElement to IFCElementComponent from IFC2x3 to IFC4.
      /// Instead of having different parents, we will keep IFCBuildingElementPart as a child of IFCBuildingElement.  This means, though, that
      /// this function can only return an IFCElement, the (normally) abstract parent of each.</remarks>
      public static IFCElement ProcessIFCElementComponent(IFCAnyHandle ifcElementComponent)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcElementComponent))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcElementComponent);
            return null;
         }

         IFCEntity elementComponent;
         IFCImportFile.TheFile.EntityMap.TryGetValue(ifcElementComponent.StepId, out elementComponent);
         if (elementComponent != null)
            return (elementComponent as IFCElement);

         IFCElement newIFCElementComponent = null;
         // other subclasses not handled yet.
         if (IFCImportFile.TheFile.SchemaVersion > IFCSchemaVersion.IFC2x3 && IFCAnyHandleUtil.IsSubTypeOf(ifcElementComponent, IFCEntityType.IfcBuildingElementPart))
            newIFCElementComponent = IFCBuildingElementPart.ProcessIFCBuildingElementPart(ifcElementComponent);
         else
            newIFCElementComponent = new IFCElementComponent(ifcElementComponent);
         return newIFCElementComponent;
      }
   }
}