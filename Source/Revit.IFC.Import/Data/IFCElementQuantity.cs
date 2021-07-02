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
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcElementQuantity.
   /// </summary>
   public class IFCElementQuantity : IFCPropertySetDefinition
   {
      /// <summary>
      /// The method of measurement for the quantities.
      /// </summary>
      string m_MethodOfMeasurement;

      /// <summary>
      /// The contained set of IFC quantities.
      /// </summary>
      IDictionary<string, IFCPhysicalQuantity> m_IFCQuantities;

      /// <summary>
      /// The method of measurement.
      /// </summary>
      public string MethodOfMeasurement
      {
         get { return m_MethodOfMeasurement; }
         set { m_MethodOfMeasurement = value; }
      }

      /// <summary>
      /// The quantities.
      /// </summary>
      public IDictionary<string, IFCPhysicalQuantity> IFCQuantities
      {
         get { return m_IFCQuantities; }
      }

      /// <summary>
      /// Processes IfcElementQuantity attributes.
      /// </summary>
      /// <param name="ifcElementQuantity">The IfcElementQuantity handle.</param>
      protected IFCElementQuantity(IFCAnyHandle ifcElementQuantity)
      {
         Process(ifcElementQuantity);
      }

      /// <summary>
      /// Processes an IFC element quantity.
      /// </summary>
      /// <param name="ifcElementQuantity">The IfcElementQuantity object.</param>
      protected override void Process(IFCAnyHandle ifcElementQuantity)
      {
         base.Process(ifcElementQuantity);

         MethodOfMeasurement = IFCImportHandleUtil.GetOptionalStringAttribute(ifcElementQuantity, "MethodOfMeasurement", null);

         HashSet<IFCAnyHandle> quantities =
             IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcElementQuantity, "Quantities");

         if (quantities != null)
         {
            m_IFCQuantities = new Dictionary<string, IFCPhysicalQuantity>();

            foreach (IFCAnyHandle quantity in quantities)
            {
               IFCPhysicalQuantity ifcPhysicalQuantity = IFCPhysicalQuantity.ProcessIFCPhysicalQuantity(quantity);
               if (ifcPhysicalQuantity != null)
                  m_IFCQuantities[ifcPhysicalQuantity.Name] = ifcPhysicalQuantity;
            }
         }
         else
         {
            Importer.TheLog.LogMissingRequiredAttributeError(ifcElementQuantity, "Quantities", false);
         }
      }

      /// <summary>
      /// Processes an IFC element quantity.
      /// </summary>
      /// <param name="propertySet">The IfcElementQuantity object.</param>
      /// <returns>The IFCElementQuantity object.</returns>
      public static IFCElementQuantity ProcessIFCElementQuantity(IFCAnyHandle ifcElementQuantity)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcElementQuantity))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcElementQuantity);
            return null;
         }

         IFCEntity elementQuantity;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcElementQuantity.StepId, out elementQuantity))
            return (elementQuantity as IFCElementQuantity);

         return new IFCElementQuantity(ifcElementQuantity);
      }

      /// <summary>
      /// Create quantities for a given element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element being created.</param>
      /// <param name="parameterGroupMap">The parameters of the element.  Cached for performance.</param>
      /// <returns>The name of the property set created, if it was created, and a Boolean value if it should be added to the property set list.</returns>
      public override Tuple<string, bool> CreatePropertySet(Document doc, Element element, IFCObjectDefinition objDef, IFCParameterSetByGroup parameterGroupMap)
      {
         Category category = IFCPropertySet.GetCategoryForParameterIfValid(element, Id);

         string quotedName = "\"" + Name + "\"";

         ISet<string> parametersCreated = new HashSet<string>();
         foreach (IFCPhysicalQuantity quantity in IFCQuantities.Values)
         {
            quantity.Create(doc, element, category, objDef, parameterGroupMap, Name, parametersCreated);
         }

         CreateScheduleForPropertySet(doc, element, category, parameterGroupMap, parametersCreated);
         return Tuple.Create(quotedName, true);
      }
   }
}