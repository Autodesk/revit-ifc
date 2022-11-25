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
using Autodesk.Revit.DB;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcPhysicalQuantity.
   /// </summary>
   public abstract class IFCPhysicalQuantity : IFCEntity
   {
      /// <summary>
      /// The name.
      /// </summary>
      public string Name { get; protected set; }

      /// <summary>
      /// The optional description.
      /// </summary>
      public string Description { get; protected set; }

      protected IFCPhysicalQuantity()
      {
      }

      protected IFCPhysicalQuantity(IFCAnyHandle ifcPhysicalQuantity)
      {
         Process(ifcPhysicalQuantity);
      }

      /// <summary>
      /// Processes an IFC physical quantity.
      /// </summary>
      /// <param name="ifcPhysicalQuantity">The IfcPhysicalQuantity object.</param>
      /// <returns>The IFCPhysicalQuantity object.</returns>
      override protected void Process(IFCAnyHandle ifcPhysicalQuantity)
      {
         base.Process(ifcPhysicalQuantity);

         Name = IFCImportHandleUtil.GetRequiredStringAttribute(ifcPhysicalQuantity, "Name", true);

         Description = IFCImportHandleUtil.GetOptionalStringAttribute(ifcPhysicalQuantity, "Description", null);
      }

      /// <summary>
      /// Processes an IFC physical quantity.
      /// </summary>
      /// <param name="ifcPhysicalQuantity">The physical quantity.</param>
      /// <returns>The IFCPhysicalQuantity object.</returns>
      public static IFCPhysicalQuantity ProcessIFCPhysicalQuantity(IFCAnyHandle ifcPhysicalQuantity)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcPhysicalQuantity))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcPhysicalQuantity);
            return null;
         }

         try
         {
            IFCEntity physicalQuantity;
            if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcPhysicalQuantity.StepId, out physicalQuantity))
               return (physicalQuantity as IFCPhysicalQuantity);

            if (IFCAnyHandleUtil.IsSubTypeOf(ifcPhysicalQuantity, IFCEntityType.IfcPhysicalSimpleQuantity))
               return IFCPhysicalSimpleQuantity.ProcessIFCPhysicalSimpleQuantity(ifcPhysicalQuantity);

            if (IFCAnyHandleUtil.IsSubTypeOf(ifcPhysicalQuantity, IFCEntityType.IfcPhysicalComplexQuantity))
               return IFCPhysicalComplexQuantity.ProcessIFCPhysicalComplexQuantity(ifcPhysicalQuantity);
         }
         catch (Exception ex)
         {
            Importer.TheLog.LogError(ifcPhysicalQuantity.StepId, ex.Message, false);
            return null;
         }

         Importer.TheLog.LogUnhandledSubTypeError(ifcPhysicalQuantity, IFCEntityType.IfcPhysicalQuantity, false);
         return null;
      }

      /// <summary>
      /// Create a quantity for a given element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element being created.</param>
      /// <param name="category">The element's category.</param>
      /// <param name="parameterGroupMap">The parameters of the element.  Cached for performance.</param>
      /// <param name="quantityFullName">The name of the containing quantity set with quantity name.</param>
      /// <param name="createdParameters">The names of the created parameters.</param>
      public abstract void Create(Document doc, Element element, Category category, IFCObjectDefinition objDef, IFCParameterSetByGroup parameterGroupMap, string quantityFullName, ISet<string> createdParameters);
   }
}