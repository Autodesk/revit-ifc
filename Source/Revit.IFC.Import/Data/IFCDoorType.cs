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
using Revit.IFC.Import.Enums;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IFC4 IfcDoorType.
   /// </summary>
   public class IFCDoorType : IFCTypeProduct
   {
      private IFCDoorTypeOperation m_OperationType;

      /// <summary>
      /// The operation type.
      /// </summary>
      public IFCDoorTypeOperation OperationType
      {
         get { return m_OperationType; }
         protected set { m_OperationType = value; }
      }

      protected IFCDoorType()
      {
      }

      /// <summary>
      /// Constructs an IFCDoorType from the IfcDoorType handle.
      /// </summary>
      /// <param name="ifcDoorType">The IfcDoorType handle.</param>
      protected IFCDoorType(IFCAnyHandle ifcDoorType)
      {
         Process(ifcDoorType);
      }

      /// <summary>
      /// Processes IfcDoorType attributes.
      /// </summary>
      /// <param name="ifcDoorType">The IfcDoorType handle.</param>
      protected override void Process(IFCAnyHandle ifcDoorType)
      {
         base.Process(ifcDoorType);

         OperationType = IFCEnums.GetSafeEnumerationAttribute<IFCDoorTypeOperation>(ifcDoorType, "OperationType", IFCDoorTypeOperation.NotDefined);
      }

      /// <summary>
      /// Processes an IfcDoorType.
      /// </summary>
      /// <param name="ifcDoorType">The IfcDoorType handle.</param>
      /// <returns>The IFCDoorType object.</returns>
      public static IFCDoorType ProcessIFCDoorType(IFCAnyHandle ifcDoorType)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcDoorType))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcDoorType);
            return null;
         }

         IFCEntity doorType;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcDoorType.StepId, out doorType))
            return (doorType as IFCDoorType);

         return new IFCDoorType(ifcDoorType);
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
            Importer.TheProcessor.SetParameter(element, BuiltInParameter.DOOR_OPERATION_TYPE, OperationType.ToString());
      }
   }
}