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

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;

using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcFeatureElement.
   /// </summary>
   public class IFCFeatureElement : IFCElement
   {
      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCFeatureElement()
      {

      }

      /// <summary>
      /// Constructs an IFCFeatureElement from the IfcFeatureElement handle.
      /// </summary>
      /// <param name="ifcFeatureElement">The IfcFeatureElement handle.</param>
      protected IFCFeatureElement(IFCAnyHandle ifcFeatureElement)
      {
         Process(ifcFeatureElement);
      }

      /// <summary>
      /// Processes IfcFeatureElement attributes.
      /// </summary>
      /// <param name="ifcFeatureElement">The IfcFeatureElement handle.</param>
      protected override void Process(IFCAnyHandle ifcFeatureElement)
      {
         base.Process(ifcFeatureElement);
      }

      /// <summary>
      /// Processes an IfcFeatureElement object.
      /// </summary>
      /// <param name="ifcFeatureElement">The IfcFeatureElement handle.</param>
      /// <returns>The IFCFeatureElement object.</returns>
      public static IFCFeatureElement ProcessIFCFeatureElement(IFCAnyHandle ifcFeatureElement)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcFeatureElement))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcFeatureElement);
            return null;
         }

         IFCEntity cachedFeatureElement;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcFeatureElement.StepId, out cachedFeatureElement))
            return (cachedFeatureElement as IFCFeatureElement);

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcFeatureElement, IFCEntityType.IfcFeatureElementSubtraction))
            return IFCFeatureElementSubtraction.ProcessIFCFeatureElementSubtraction(ifcFeatureElement);

         return new IFCFeatureElement(ifcFeatureElement);
      }
   }
}