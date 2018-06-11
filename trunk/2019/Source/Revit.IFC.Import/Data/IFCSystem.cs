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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcSystem.
   /// </summary>
   public class IFCSystem : IFCGroup
   {
      // <summary>
      /// Processes IfcSystem attributes.
      /// </summary>
      /// <param name="ifcSystem">The IfcSystem handle.</param>
      protected override void Process(IFCAnyHandle ifcSystem)
      {
         base.Process(ifcSystem);
      }

      protected IFCSystem()
      {
      }

      protected IFCSystem(IFCAnyHandle group)
      {
         Process(group);
      }

      /// <summary>
      /// Processes IfcSystem handle.
      /// </summary>
      /// <param name="ifcSystem">The IfcSystem handle.</param>
      /// <returns>The IFCSystem object.</returns>
      public static IFCSystem ProcessIFCSystem(IFCAnyHandle ifcSystem)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSystem))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcSystem);
            return null;
         }

         IFCEntity cachedIFCSystem;
         IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSystem.StepId, out cachedIFCSystem);
         if (cachedIFCSystem != null)
            return cachedIFCSystem as IFCSystem;

         return new IFCSystem(ifcSystem);
      }
   }
}