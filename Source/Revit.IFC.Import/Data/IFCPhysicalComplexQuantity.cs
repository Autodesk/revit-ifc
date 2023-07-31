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
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcPhysicalComplexQuantity.
   /// </summary>
   public class IFCPhysicalComplexQuantity : IFCPhysicalQuantity
   {
      /// <summary>
      /// Set of physical quantities that are grouped by this complex physical quantity.
      /// </summary>
      public ISet<IFCPhysicalQuantity> HasQuantities { get; protected set; } = new HashSet<IFCPhysicalQuantity>();

      /// <summary>
      /// Identification of the discrimination by which this physical complex property is distinguished.
      /// </summary>
      public string Discrimination { get; protected set; }

      /// <summary>
      /// The optional quality.
      /// </summary>
      public string Quality { get; protected set; }

      /// <summary>
      /// The optional usgage.
      /// </summary>
      public string Usage { get; protected set; }

      protected IFCPhysicalComplexQuantity()
      {
      }

      protected IFCPhysicalComplexQuantity(IFCAnyHandle IfcPhysicalComplexQuantity)
      {
         Process(IfcPhysicalComplexQuantity);
      }

      /// <summary>
      /// Processes an IFC physical complex quantity.
      /// </summary>
      /// <param name="IfcPhysicalComplexQuantity">The IfcPhysicalComplexQuantity object.</param>
      /// <returns>The IfcPhysicalComplexQuantity object.</returns>
      override protected void Process(IFCAnyHandle IfcPhysicalComplexQuantity)
      {
         base.Process(IfcPhysicalComplexQuantity);

         HashSet<IFCAnyHandle> ifcPhysicalQuantities =
             IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(IfcPhysicalComplexQuantity, "HasQuantities");
         if (ifcPhysicalQuantities == null || ifcPhysicalQuantities.Count == 0)
            throw new InvalidOperationException("#" + IfcPhysicalComplexQuantity.StepId + ": no physical quantities, aborting.");

         foreach (IFCAnyHandle ifcPhysicalQuantity in ifcPhysicalQuantities)
         {
            if (ifcPhysicalQuantity == IfcPhysicalComplexQuantity)
            {
               Importer.TheLog.LogWarning(ifcPhysicalQuantity.StepId, "The IfcPhysicalComplexQuantity should not reference itself within the list of HasQuantities, ignoring", false);
               continue;
            }

            try
            {
               HasQuantities.Add(IFCPhysicalQuantity.ProcessIFCPhysicalQuantity(ifcPhysicalQuantity));
            }
            catch
            {
               Importer.TheLog.LogWarning(IfcPhysicalComplexQuantity.StepId, "Invalid physical quantity, ignoring", false);
            }
         }

         Discrimination = IFCImportHandleUtil.GetRequiredStringAttribute(IfcPhysicalComplexQuantity, "Discrimination", true);

         Quality = IFCImportHandleUtil.GetOptionalStringAttribute(IfcPhysicalComplexQuantity, "Quality", null);

         Usage = IFCImportHandleUtil.GetOptionalStringAttribute(IfcPhysicalComplexQuantity, "Usage", null);

      }

      /// <summary>
      /// Processes an IFC physical complex quantity.
      /// </summary>
      /// <param name="IfcPhysicalComplexQuantity">The physical quantity.</param>
      /// <returns>The IfcPhysicalComplexQuantity object.</returns>
      public static IFCPhysicalComplexQuantity ProcessIFCPhysicalComplexQuantity(IFCAnyHandle IfcPhysicalComplexQuantity)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(IfcPhysicalComplexQuantity))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcPhysicalComplexQuantity);
            return null;
         }

         try
         {
            IFCEntity physicalComplexQuantity;
            if (IFCImportFile.TheFile.EntityMap.TryGetValue(IfcPhysicalComplexQuantity.StepId, out physicalComplexQuantity))
               return (physicalComplexQuantity as IFCPhysicalComplexQuantity);

            return new IFCPhysicalComplexQuantity(IfcPhysicalComplexQuantity);
         }
         catch (Exception ex)
         {
            Importer.TheLog.LogError(IfcPhysicalComplexQuantity.StepId, ex.Message, false);
            return null;
         }
      }

      /// <summary>
      /// Create a quantity for a given element.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element being created.</param>
      /// <param name="category">The element's category.</param>
      /// <param name="objDef">The oject definition.</param>
      /// <param name="parameterGroupMap">The parameters.</param>
      /// <param name="quantityFullName">The name of the containing quantity set with quantity name.</param>
      /// <param name="createdParameters">The names of the created parameters.</param>
      public override void Create(Document doc, Element element, Category category, IFCObjectDefinition objDef, 
         IFCParameterSetByGroup parameterGroupMap, string quantityFullName, ISet<string> createdParameters,
         ParametersToSet parametersToSet)
      {
         foreach (IFCPhysicalQuantity quantity in HasQuantities)
         {
            string complexFullName = AppendComplexQuantityName(quantityFullName, quantity.Name);
            quantity.Create(doc, element, category, objDef, parameterGroupMap, complexFullName, createdParameters,
               parametersToSet);
         }
      }

      /// <summary>
      /// Add sub-quantity name to full quantity name
      /// </summary>
      /// <param name="quantityFullName">The full quantity name.</param>
      /// <param name="subQuantityName">The sub-quantity name</param>
      /// <returns>The full quantity name with sub-quantity name.</returns>
      protected string AppendComplexQuantityName(string quantityFullName, string subQuantityName)
      {
         // Navisworks uses this engine and needs support for the old naming.
         // We use the API-only UseStreamlinedOptions as a proxy for knowing this.
         int insertIndx = (IFCImportFile.TheFile.Options.UseStreamlinedOptions) ?
            quantityFullName.IndexOf('(') : quantityFullName.Length;
         return quantityFullName.Insert((insertIndx < 0 ? quantityFullName.Length : insertIndx), "." + subQuantityName);
      }
   }
}