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
   /// From spec:
   /// Representation of the concept of an identified discrete almost homogeneous geological feature with either an irregular solid or 'Yabuki' top surface
   /// shape or a regular voxel cubic shape. A stratum is represented as a discrete entity, specialized (sub typed) from IfcElement. A stratum may be broken down
   /// into smaller entities if properties vary across the stratum or alternatively properties may be described with bounded numeric ranges. A stratum may carry
   /// information about the physical form and its interpretation as a Geological Item (GML). The shape representations used should correspond to the sub-type of
   /// IfcGeotechnicalAssembly in which it occurs
   /// </summary>
   public class IFCGeotechnicalStratum : IFCGeotechnicalElement
   {
      /// <summary>
      /// Default Constructor
      /// </summary>
      protected IFCGeotechnicalStratum()
      {
      }

      /// <summary>
      /// Constructs an ifcGeometricStratum from the supplied handle.
      /// </summary>
      /// <param name="ifcGeotechnicalStratum">The handle to use for the Geotechnical Stratum.</param>
      protected IFCGeotechnicalStratum(IFCAnyHandle ifcGeotechnicalStratum)
      {
         Process(ifcGeotechnicalStratum);
      }

      /// <summary>
      /// Processes IfcGeotechnicalStratum attributes.
      /// </summary>
      /// <param name="ifcGeotechnicalStratum">Handle to process.</param>
      protected override void Process(IFCAnyHandle ifcGeotechnicalStratum)
      {
         base.Process(ifcGeotechnicalStratum);
      }

      /// <summary>
      /// Processes IfcGeotechnicalStratum object.
      /// </summary>
      /// <param name="ifcGeotechnicalStratum">Geotechnical Stratum handle to process.</param>
      /// <returns></returns>
      public static IFCGeotechnicalStratum ProcessIFCGeotechnicalStratum(IFCAnyHandle ifcGeotechnicalStratum)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcGeotechnicalStratum))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcElement);
            return null;
         }

         try
         {
            IFCEntity cachedIFCGeotechnicalStratum;
            IFCImportFile.TheFile.EntityMap.TryGetValue(ifcGeotechnicalStratum.StepId, out cachedIFCGeotechnicalStratum);
            if (cachedIFCGeotechnicalStratum != null)
               return (cachedIFCGeotechnicalStratum as IFCGeotechnicalStratum);

            // other subclasses not handled yet.
            if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcGeotechnicalStratum, IFCEntityType.IfcGeotechnicalElement))
               return IFCSolidStratum.ProcessIFCSolidStratum(ifcGeotechnicalStratum);

            return new IFCGeotechnicalStratum(ifcGeotechnicalStratum);
         }
         catch (Exception ex)
         {
            HandleError(ex.Message, ifcGeotechnicalStratum, true);
            return null;
         }
      }
   }
}
