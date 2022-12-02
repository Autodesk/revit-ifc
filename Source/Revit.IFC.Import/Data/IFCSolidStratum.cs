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
   /// Representation of the concept of an identified discrete almost homogeneous solid geological or surface feature,
   /// including discontinuities such as faults, fractures, boundaries and interfaces that are not explicitly modeled.
   /// </summary>
   public class IFCSolidStratum : IFCGeotechnicalStratum
   {
      /// <summary>
      /// Default Constructor
      /// </summary>
      protected IFCSolidStratum ()
      {
      }

      /// <summary>
      /// Constructs IfcSolidStream from the input handle.
      /// </summary>
      /// <param name="ifcSolidStratum">Handle representing an IfcSolidStratum.</param>
      protected IFCSolidStratum (IFCAnyHandle ifcSolidStratum)
      {
         Process(ifcSolidStratum);
      }

      /// <summary>
      /// Process attributes within an IfcSolidStratum
      /// </summary>
      /// <param name="ifcSolidStratum">Handle representing IfcSolidStratum.</param>
      protected override void Process(IFCAnyHandle ifcSolidStratum)
      {
         base.Process(ifcSolidStratum);
      }

      /// <summary>
      /// Process the IfcSolidStratum object.
      /// </summary>
      /// <param name="ifcSolidStratum">Handle that represents an IfcSolidStratum object.</param>
      /// <returns></returns>
      public static IFCSolidStratum ProcessIFCSolidStratum(IFCAnyHandle ifcSolidStratum)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSolidStratum))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcElement);
            return null;
         }

         try
         {
            IFCEntity cachedIFCSolidStratum;
            IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSolidStratum.StepId, out cachedIFCSolidStratum);
            if (cachedIFCSolidStratum != null)
               return (cachedIFCSolidStratum as IFCSolidStratum);

            return new IFCSolidStratum(ifcSolidStratum);
         }
         catch (Exception ex)
         {
            HandleError(ex.Message, ifcSolidStratum, true);
            return null;
         }
      }
   }
}
