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

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcElementType.
   /// </summary>
   /// <remarks>This class is currently non-abstract because we haven't yet created the derived classes.
   /// When they are created, this will be made abstract.</remarks>
   public class IFCElementType : IFCTypeProduct
   {
      private string m_ElementType;

      /// <summary>
      /// The element type.
      /// </summary>
      public string ElementType
      {
         get { return m_ElementType; }
         protected set { m_ElementType = value; }
      }

      protected IFCElementType()
      {
      }

      /// <summary>
      /// Constructs an IFCElementType from the IfcElementType handle.
      /// </summary>
      /// <param name="ifcElementType">The IfcElementType handle.</param>
      protected IFCElementType(IFCAnyHandle ifcElementType)
      {
         Process(ifcElementType);
      }

      /// <summary>
      /// Processes IfcElementType attributes.
      /// </summary>
      /// <param name="ifcElementType">The IfcElementType handle.</param>
      protected override void Process(IFCAnyHandle ifcElementType)
      {
         base.Process(ifcElementType);

         ElementType = IFCAnyHandleUtil.GetStringAttribute(ifcElementType, "ElementType");
      }

      /// <summary>
      /// Processes an IfcElementType.
      /// </summary>
      /// <param name="ifcElementType">The IfcElementType handle.</param>
      /// <returns>The IFCElementType object.</returns>
      public static IFCElementType ProcessIFCElementType(IFCAnyHandle ifcElementType)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcElementType))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcElementType);
            return null;
         }

         IFCEntity elementType;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcElementType.StepId, out elementType))
            return (elementType as IFCElementType);

         return new IFCElementType(ifcElementType);
      }

      /// <summary>
      /// Creates or populates Revit element params based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element.</param>
      protected override void CreateParametersInternal(Document doc, Element element)
      {
         base.CreateParametersInternal(doc, element);

         if (element != null)
         {
            if (!string.IsNullOrWhiteSpace(ElementType))
            {
               Category category = IFCPropertySet.GetCategoryForParameterIfValid(element, Id);
               IFCPropertySet.AddParameterString(doc, element, category, this, "IfcElementType", ElementType, Id);
            }
         }
      }
   }
}