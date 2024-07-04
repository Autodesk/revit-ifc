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

using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcZone.
   /// </summary>
   public class IFCZone : IFCGroup
   {
      /// <summary>
      /// Processes IfcZone attributes.
      /// </summary>
      /// <param name="ifcZone">The IfcZone handle.</param>
      protected override void Process(IFCAnyHandle ifcZone)
      {
         base.Process(ifcZone);
      }

      protected IFCZone()
      {
      }

      protected IFCZone(IFCAnyHandle zone)
      {
         Process(zone);
      }

      /// <summary>
      /// Processes IfcZone handle.
      /// </summary>
      /// <param name="ifcZone">The IfcZone handle.</param>
      /// <returns>The IFCZone object.</returns>
      public static IFCZone ProcessIFCZone(IFCAnyHandle ifcZone)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcZone))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcZone);
            return null;
         }

         IFCEntity cachedIFCZone;
         IFCImportFile.TheFile.EntityMap.TryGetValue(ifcZone.StepId, out cachedIFCZone);
         if (cachedIFCZone != null)
            return cachedIFCZone as IFCZone;

         return new IFCZone(ifcZone);
      }

      /// <summary>
      /// Indicates whether duplicate geometry should be created for IFCZone.
      /// This is governed by an IFC Importer option:  CreateDuplicateZoneGeometry.
      /// </summary>
      /// <returns>True if creating duplicate geometry option set, False otherwise.</returns>
      public override bool ContainerDuplicatesGeometry() { return Importer.TheOptions.CreateDuplicateZoneGeometry; }

      /// <summary>
      /// Indicates which IFC entities should be used when creating duplicate geometry.
      /// </summary>
      /// <param name="entity">An IFC entity for filtering.</param>
      /// <returns>True if the IFC entity geometry should be duplicated, False otherwise.</returns>
      public override bool ContainerFilteredEntity(IFCEntity entity)
      {
         if (entity == null)
            return false;

         return (entity.EntityType == IFCEntityType.IfcSpace) || (entity.EntityType == IFCEntityType.IfcSpatialZone);
      }

      /// <summary>
      /// Indicates whether we should create a separate DirectShape Element for this IFC entity.
      /// For IfcZone, a DirectShape should be created.
      /// </summary>
      /// <returns>True if a DirectShape container is created, False otherwise.</returns>
      public override bool CreateContainer() { return true; }
   }
}