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

using Revit.IFC.Common.Enums;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcBuildingElementPart.
   /// </summary>
   public class IFCBuildingElementPart : IFCBuildingElementComponent
   {
      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCBuildingElementPart()
      {

      }

      /// <summary>
      /// Constructs an IFCBuildingElementPart from the IfcBuildingElementPart handle.
      /// </summary>
      /// <param name="ifcBuildingElementPart">The IfcBuildingElementPart handle.</param>
      protected IFCBuildingElementPart(IFCAnyHandle ifcBuildingElementPart)
      {
         Process(ifcBuildingElementPart);
      }

      /// <summary>
      /// Processes IfcBuildingElementPart attributes.
      /// </summary>
      /// <param name="ifcBuildingElementPart">The IfcBuildingElementPart handle.</param>
      protected override void Process(IFCAnyHandle ifcBuildingElementPart)
      {
         base.Process(ifcBuildingElementPart);
      }

      /// <summary>
      /// Processes an IFCBuildingElementPart object.
      /// </summary>
      /// <param name="ifcBuildingElementPart">The IfcBuildingElementPart handle.</param>
      /// <returns>The IFCBuildingElementPart object.</returns>
      public static IFCBuildingElementPart ProcessIFCBuildingElementPart(IFCAnyHandle ifcBuildingElementPart)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcBuildingElementPart))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcBuildingElementPart);
            return null;
         }

         IFCEntity buildingElementPart;
         IFCImportFile.TheFile.EntityMap.TryGetValue(ifcBuildingElementPart.StepId, out buildingElementPart);
         if (buildingElementPart != null)
            return (buildingElementPart as IFCBuildingElementPart);

         return new IFCBuildingElementPart(ifcBuildingElementPart);
      }
   }
}