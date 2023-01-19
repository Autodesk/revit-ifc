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
using System.Threading.Tasks;
using Revit.IFC.Common.Enums;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;
namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// This is an abstract supertype for Geotechnical Entities.
   /// </summary>
   public class IFCGeotechnicalElement :  IFCElement
   {
      /// <summary>
      /// Default Constructor
      /// </summary>
      protected IFCGeotechnicalElement()
      {
      }

      
      /// <summary>
      /// Construct an IFCGeotechnicalElement from the input handle.
      /// </summary>
      /// <param name="ifcGeotechnicalElement">The handle representing an IfcGeotechnicalElement</param>
      protected IFCGeotechnicalElement(IFCAnyHandle ifcGeotechnicalElement)
      {
         Process(ifcGeotechnicalElement);
      }


      /// <summary>
      /// Processes the specific IfcGeotechnicalElement attributes, if any.
      /// </summary>
      /// <param name="ifcGeotechnicalElement"></param>
      protected override void Process(IFCAnyHandle ifcGeotechnicalElement)
      {
         base.Process(ifcGeotechnicalElement);
      }


      /// <summary>
      /// 
      /// </summary>
      /// <param name="ifcGeotechnicalElement"></param>
      /// <returns></returns>
      public static IFCGeotechnicalElement ProcessIFCGeotechnicalElement(IFCAnyHandle ifcGeotechnicalElement)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcGeotechnicalElement))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcElement);
            return null;
         }

         try
         {
            IFCEntity cachedGeotechnicalElement;
            IFCImportFile.TheFile.EntityMap.TryGetValue(ifcGeotechnicalElement.StepId, out cachedGeotechnicalElement);
            if (cachedGeotechnicalElement != null)
               return (cachedGeotechnicalElement as IFCGeotechnicalElement);

            // other subclasses not handled yet.
            if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcGeotechnicalElement, IFCEntityType.IfcGeotechnicalStratum))
               return IFCGeotechnicalStratum.ProcessIFCGeotechnicalStratum(ifcGeotechnicalElement);

            return new IFCGeotechnicalElement(ifcGeotechnicalElement);
         }
         catch (Exception ex)
         {
            HandleError(ex.Message, ifcGeotechnicalElement, true);
            return null;
         }
      }
   }
}
