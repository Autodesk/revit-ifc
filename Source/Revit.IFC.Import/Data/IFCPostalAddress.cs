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
   /// Class for storing IfcPostalAddress
   /// </summary>
   public class IFCPostalAddress : IFCAddress
   {
      /// <summary>
      /// The optional address for internal mail delivery.
      /// </summary>
      public string InternalLocation { get; set; } = null;

      /// <summary>
      /// The postal address.
      /// </summary>
      public IList<string> AddressLines { get; set; } = null;

      /// <summary>
      /// The optional address that is implied by an identifiable mail drop.
      /// </summary>
      public string PostalBox { get; set; } = null;

      /// <summary>
      /// The optionalname of a town.
      /// </summary>
      public string Town { get; set; } = null;

      /// <summary>
      /// The optional name of a region.
      /// </summary>
      public string Region { get; set; } = null;

      /// <summary>
      /// The optional code that is used by the country's postal service.
      /// </summary>
      public string PostalCode { get; set; } = null;

      /// <summary>
      /// The optional name of a country.
      /// </summary>
      public string Country { get; set; } = null;


      protected IFCPostalAddress()
      {
      }

      protected IFCPostalAddress(IFCAnyHandle ifcPostalAddress)
      {
         Process(ifcPostalAddress);
      }

      protected override void Process(IFCAnyHandle ifcPostalAddress)
      {
         base.Process(ifcPostalAddress);

         InternalLocation = IFCImportHandleUtil.GetOptionalStringAttribute(ifcPostalAddress, "InternalLocation", null);
         PostalBox = IFCImportHandleUtil.GetOptionalStringAttribute(ifcPostalAddress, "PostalBox", null);
         Town = IFCImportHandleUtil.GetOptionalStringAttribute(ifcPostalAddress, "Town", null);
         Region = IFCImportHandleUtil.GetOptionalStringAttribute(ifcPostalAddress, "Region", null);
         PostalCode = IFCImportHandleUtil.GetOptionalStringAttribute(ifcPostalAddress, "PostalCode", null);
         Country = IFCImportHandleUtil.GetOptionalStringAttribute(ifcPostalAddress, "Country", null);
         AddressLines = IFCAnyHandleUtil.GetAggregateStringAttribute<List<string>>(ifcPostalAddress, "AddressLines");

         if (InternalLocation == null && PostalBox == null && Town == null && Region == null &&
            PostalCode == null && Country == null && ((AddressLines?.Count ?? 0) == 0))
               Importer.TheLog.LogWarning(Id, "Missing IfcPostalAddress, ignoring.", true);
      }

      /// <summary>
      /// Create an IFCPostalAddress object from a handle of type IfcPostalAddress.
      /// </summary>
      /// <param name="ifcPostalAddress">The IFC handle.</param>
      /// <returns>The IFCPostalAddress object.</returns>
      public static IFCPostalAddress ProcessIFCPostalAddress(IFCAnyHandle ifcPostalAddress)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcPostalAddress))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcPostalAddress);
            return null;
         }

         IFCEntity postalAddress;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcPostalAddress.StepId, out postalAddress))
            postalAddress = new IFCPostalAddress(ifcPostalAddress);
         return (postalAddress as IFCPostalAddress);
      }
   }
}