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

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IFCSpatialZone.
   /// </summary>
   public class IFCSpatialZone : IFCSpatialElement
   {
      /// <summary>
      /// Constructs an IFCSpatialZone from the IfcSpatialZone handle.
      /// </summary>
      /// <param name="IFCSpatialZone">The IfcSpatialZone handle.</param>
      protected IFCSpatialZone(IFCAnyHandle ifcSpatialZone)
      {
         Process(ifcSpatialZone);
      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCSpatialZone()
      {

      }

      /// <summary>
      /// Processes IfcSpatialZone.
      /// </summary>
      /// <param name="ifcSpatialZone">The IfcSpatialZone handle.</param>
      protected override void Process(IFCAnyHandle ifcSpatialZone)
      {
         base.Process(ifcSpatialZone);
      }

      /// <summary>
      /// Processes IfcSpatialZone handle.
      /// </summary>
      /// <param name="ifcSpatialZone">The IfcSpatialZone handle.</param>
      /// <returns>The IFCSpatialZone object.</returns>
      public static IFCSpatialZone ProcessIFCSpatialZone(IFCAnyHandle ifcSpatialZone)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSpatialZone))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcSpatialZone);
            return null;
         }

         IFCEntity spatialZone;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSpatialZone.StepId, out spatialZone))
            return (spatialZone as IFCSpatialZone);

         return new IFCSpatialZone(ifcSpatialZone);
      }
   }
}


