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

using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IFCSpatialElement.
   /// </summary>
   public class IFCSpatialElement : IFCProduct
   {
      /// <summary>
      /// Constructs an IFCSpatialElement from the IfcSpatialElement handle.
      /// </summary>
      /// <param name="IFCSpatialElement">The IfcSpatialElement handle.</param>
      protected IFCSpatialElement(IFCAnyHandle ifcSpatialElement)
      {
         Process(ifcSpatialElement);
      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCSpatialElement()
      {

      }

      /// <summary>
      /// Processes IfcSpatialElement.
      /// </summary>
      /// <param name="ifcSpatialElement">The IfcSpatialElement handle.</param>
      protected override void Process(IFCAnyHandle ifcSpatialElement)
      {
         base.Process(ifcSpatialElement);
      }

      /// <summary>
      /// Processes IfcSpatialElement handle.
      /// </summary>
      /// <param name="ifcSpatialElement">The IfcSpatialElement handle.</param>
      /// <returns>The IFCSpatialElement object.</returns>
      public static IFCSpatialElement ProcessIFCSpatialElement(IFCAnyHandle ifcSpatialElement)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSpatialElement))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcSpatialElement);
            return null;
         }

         IFCEntity spatialElement;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSpatialElement.StepId, out spatialElement))
            return (spatialElement as IFCSpatialElement);

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcSpatialElement, IFCEntityType.IfcSpatialZone))
            return IFCSpatialZone.ProcessIFCSpatialZone(ifcSpatialElement);
         else if (IFCAnyHandleUtil.IsSubTypeOf(ifcSpatialElement, IFCEntityType.IfcSpatialStructureElement))
            return IFCSpatialStructureElement.ProcessIFCSpatialStructureElement(ifcSpatialElement);

         Importer.TheLog.LogUnhandledSubTypeError(ifcSpatialElement, IFCEntityType.IfcProduct, false);
         return null;
      }
   }
}


