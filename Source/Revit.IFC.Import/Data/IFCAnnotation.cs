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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// An annotation is an information element within the geometric (and spatial) context of a project,
   /// that adds a note or meaning to the objects which constitutes the project model
   /// </summary>
   public class IFCAnnotation : IFCProduct
   {
      /// <summary>
      /// Default Constructor.
      /// </summary>
      protected IFCAnnotation()
      {
      }


      /// <summary>
      /// Constructs an IFCAnnotation Object.
      /// </summary>
      /// <param name="ifcAnnotation">Handle to use for Annotation during construction.</param>
      protected IFCAnnotation(IFCAnyHandle ifcAnnotation)
      {
         Process(ifcAnnotation);
      }


      /// <summary>
      /// Process attributes of IFCAnnotation.
      /// </summary>
      /// <param name="ifcAnnotation"></param>
      protected override void Process (IFCAnyHandle ifcAnnotation)
      {
         base.Process(ifcAnnotation);
      }


      /// <summary>
      /// Process IFCAnnotation object.
      /// </summary>
      /// <param name="ifcAnnotation">Handle representing IFC Object.</param>
      /// <returns></returns>
      public static IFCAnnotation ProcessIFCAnnotation (IFCAnyHandle ifcAnnotation)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcAnnotation))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcAnnotation);
            return null;
         }

         try
         {
            IFCEntity cachedAnnotation;
            if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcAnnotation.StepId, out cachedAnnotation))
               return (cachedAnnotation as IFCAnnotation);

            return new IFCAnnotation(ifcAnnotation);
         }
         catch (Exception ex)
         {
            HandleError(ex.Message, ifcAnnotation, true);
            return null;
         }
      }
   }
}

