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
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Class for storing IfcAddress
   /// </summary>
   public class IFCAddress : IFCEntity
   {
      /// <summary>
      /// The optional logical location of the address.
      /// </summary>
      public string Purpose { get; set; } = null;

      /// <summary>
      /// The optional description of the address.
      /// </summary>
      public string Description { get; set; } = null;

      /// <summary>
      /// The optional user specific purpose of the address.
      /// </summary>
      public string UserDefinedPurpose { get; set; } = null;

      protected IFCAddress()
      {
      }

      protected IFCAddress(IFCAnyHandle ifcAddress)
      {
         Process(ifcAddress);
      }

      protected override void Process(IFCAnyHandle ifcAddress)
      {
         base.Process(ifcAddress);

         Description = IFCImportHandleUtil.GetOptionalStringAttribute(ifcAddress, "Description", null);
         UserDefinedPurpose = IFCImportHandleUtil.GetOptionalStringAttribute(ifcAddress, "UserDefinedPurpose", null);
         Purpose = IFCAnyHandleUtil.GetEnumerationAttribute(ifcAddress, "Purpose");
      }

      /// <summary>
      /// Create an IFCAddress object from a handle of type IfcAddress.
      /// </summary>
      /// <param name="ifcAddress">The IFC handle.</param>
      /// <returns>The IFCAddress object.</returns>
      public static IFCAddress ProcessIFCAddress(IFCAnyHandle ifcAddress)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcAddress))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcAddress);
            return null;
         }

         IFCEntity address;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcAddress.StepId, out address))
            address = new IFCAddress(ifcAddress);
         return (address as IFCAddress);
      }
   }
}