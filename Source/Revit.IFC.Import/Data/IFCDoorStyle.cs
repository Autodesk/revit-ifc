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
   /// Represents an IFC2x3 IfcDoorStyle.
   /// </summary>
   public class IFCDoorStyle : IFCTypeProduct
   {
      private IFCDoorStyleOperation m_OperationType;

      private IFCDoorStyleConstruction m_ConstructionType;

      /// <summary>
      /// The operation type.
      /// </summary>
      public IFCDoorStyleOperation OperationType
      {
         get { return m_OperationType; }
         protected set { m_OperationType = value; }
      }

      /// <summary>
      /// The construction type.
      /// </summary>
      public IFCDoorStyleConstruction ConstructionType
      {
         get { return m_ConstructionType; }
         protected set { m_ConstructionType = value; }
      }

      protected IFCDoorStyle()
      {
      }

      /// <summary>
      /// Constructs an IFCDoorStyle from the IfcDoorStyle handle.
      /// </summary>
      /// <param name="ifcDoorStyle">The IfcDoorStyle handle.</param>
      protected IFCDoorStyle(IFCAnyHandle ifcDoorStyle)
      {
         Process(ifcDoorStyle);
      }

      /// <summary>
      /// Processes IfcDoorStyle attributes.
      /// </summary>
      /// <param name="ifcDoorStyle">The IfcDoorStyle handle.</param>
      protected override void Process(IFCAnyHandle ifcDoorStyle)
      {
         base.Process(ifcDoorStyle);

         OperationType = IFCEnums.GetSafeEnumerationAttribute<IFCDoorStyleOperation>(ifcDoorStyle, "OperationType", IFCDoorStyleOperation.NotDefined);

         ConstructionType = IFCEnums.GetSafeEnumerationAttribute<IFCDoorStyleConstruction>(ifcDoorStyle, "ConstructionType", IFCDoorStyleConstruction.NotDefined);
      }

      /// <summary>
      /// Processes an IfcDoorStyle.
      /// </summary>
      /// <param name="ifcDoorStyle">The IfcDoorStyle handle.</param>
      /// <returns>The IFCDoorStyle object.</returns>
      public static IFCDoorStyle ProcessIFCDoorStyle(IFCAnyHandle ifcDoorStyle)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcDoorStyle))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcDoorStyle);
            return null;
         }

         IFCEntity doorStyle;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcDoorStyle.StepId, out doorStyle))
            return (doorStyle as IFCDoorStyle);

         return new IFCDoorStyle(ifcDoorStyle);
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
            Category category = IFCPropertySet.GetCategoryForParameterIfValid(element, Id);
            if (category != null)
            {
               IFCDefaultProcessor processor = Importer.TheProcessor as IFCDefaultProcessor;
               if (processor != null)
               {
                  processor.SetElementStringParameter(element, Id, BuiltInParameter.DOOR_OPERATION_TYPE, OperationType.ToString(), false, ParametersToSet);
                  processor.SetElementStringParameter(element, Id, BuiltInParameter.DOOR_CONSTRUCTION_TYPE, ConstructionType.ToString(), false, ParametersToSet);
               }
               else
               {
                  Importer.TheProcessor.SetStringParameter(element, Id, BuiltInParameter.DOOR_OPERATION_TYPE, OperationType.ToString(), false);
                  Importer.TheProcessor.SetStringParameter(element, Id, BuiltInParameter.DOOR_CONSTRUCTION_TYPE, ConstructionType.ToString(), false);
               }

               ParametersToSet.AddStringParameter(doc, element, category, this, "IfcOperationType", OperationType.ToString(), Id);
               ParametersToSet.AddStringParameter(doc, element, category, this, "IfcConstructionType", ConstructionType.ToString(), Id);
            }
         }
      }
   }
}