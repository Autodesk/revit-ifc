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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcOpeningElement.
   /// </summary>
   public class IFCOpeningElement : IFCFeatureElementSubtraction
   {
      protected IFCElement m_FilledByElement = null;

      /// <summary>
      /// The element this opening is filled by (e.g., a door).
      /// </summary>
      public IFCElement FilledByElement
      {
         get { return m_FilledByElement; }
         set { m_FilledByElement = value; }
      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCOpeningElement()
      {

      }

      /// <summary>
      /// Constructs an IFCOpeningElement from the IfcOpeningElement handle.
      /// </summary>
      /// <param name="ifcOpeningElement">The IfcOpeningElement handle.</param>
      protected IFCOpeningElement(IFCAnyHandle ifcOpeningElement)
      {
         Process(ifcOpeningElement);
      }

      /// <summary>
      /// Processes IfcOpeningElement attributes.
      /// </summary>
      /// <param name="ifcOpeningElement">The IfcOpeningElement handle.</param>
      protected override void Process(IFCAnyHandle ifcOpeningElement)
      {
         base.Process(ifcOpeningElement);

         ICollection<IFCAnyHandle> hasFillings = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcOpeningElement, "HasFillings");
         if (hasFillings != null)
         {
            // Assume that there is only one filling for the opening, and take first found.
            foreach (IFCAnyHandle hasFilling in hasFillings)
            {
               IFCAnyHandle relatedFillingElement = IFCAnyHandleUtil.GetInstanceAttribute(hasFilling, "RelatedBuildingElement");
               if (IFCAnyHandleUtil.IsNullOrHasNoValue(relatedFillingElement))
                  continue;

               IFCEntity filledByElement;
               IFCImportFile.TheFile.EntityMap.TryGetValue(relatedFillingElement.StepId, out filledByElement);
               if (filledByElement == null)
                  FilledByElement = IFCElement.ProcessIFCElement(relatedFillingElement);
               else
                  FilledByElement = filledByElement as IFCElement;
               
               if (FilledByElement != null)
                  FilledByElement.FillsOpening = this;
               break;
            }
         }
      }

      /// <summary>
      /// Processes an IfcOpeningElement object.
      /// </summary>
      /// <param name="ifcOpeningElement">The IfcOpeningElement handle.</param>
      /// <returns>The IFCOpeningElement object.</returns>
      public static IFCOpeningElement ProcessIFCOpeningElement(IFCAnyHandle ifcOpeningElement)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcOpeningElement))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcOpeningElement);
            return null;
         }

         //sub classes not handled yet!
         IFCEntity openingElement;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcOpeningElement.StepId, out openingElement))
            openingElement = new IFCOpeningElement(ifcOpeningElement);
         return (openingElement as IFCOpeningElement);
      }
   }
}