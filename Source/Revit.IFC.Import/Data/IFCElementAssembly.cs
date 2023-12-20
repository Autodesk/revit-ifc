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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcElementAssembly.
   /// </summary>
   public class IFCElementAssembly : IFCElement
   {
      private IFCAssemblyPlace m_AssemblyPlace;

      /// <summary>
      /// A designation of where the assembly is intended to take place
      /// </summary>
      public IFCAssemblyPlace AssemblyPlace
      {
         get { return m_AssemblyPlace; }
         set { m_AssemblyPlace = value; }
      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCElementAssembly()
      {

      }

      /// <summary>
      /// Constructs an IFCElementAssembly from the IfcElementAssembly handle.
      /// </summary>
      /// <param name="ifcElementAssembly">The IfcElementAssembly handle.</param>
      protected IFCElementAssembly(IFCAnyHandle ifcElementAssembly)
      {
         Process(ifcElementAssembly);
      }

      /// <summary>
      /// Gets the predefined type from the IfcObject, depending on the file version and entity type.
      /// </summary>
      /// <param name="ifcElementAssembly">The associated handle.</param>
      /// <returns>The predefined type, if any.</returns>
      protected override string GetPredefinedType(IFCAnyHandle ifcElementAssembly)
      {
         IFCElementAssemblyType predefinedType =
             IFCEnums.GetSafeEnumerationAttribute<IFCElementAssemblyType>(ifcElementAssembly, "PredefinedType", IFCElementAssemblyType.NotDefined);

         return predefinedType.ToString();
      }

      /// <summary>
      /// Processes IfcElementAssembly attributes.
      /// </summary>
      /// <param name="ifcElementAssembly">The IfcElementAssembly handle.</param>
      protected override void Process(IFCAnyHandle ifcElementAssembly)
      {
         base.Process(ifcElementAssembly);

         AssemblyPlace = IFCEnums.GetSafeEnumerationAttribute<IFCAssemblyPlace>(ifcElementAssembly, "AssemblyPlace", IFCAssemblyPlace.NotDefined);
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
            ParametersToSet.AddStringParameter(doc, element, category, this, "IfcPredefinedType", PredefinedType, Id);
            ParametersToSet.AddStringParameter(doc, element, category, this, "IfcAssemblyPlace", AssemblyPlace.ToString(), Id);
         }
      }

      /// <summary>
      /// Creates or populates Revit elements based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      protected override void Create(Document doc)
      {
         base.Create(doc);
      }

      /// <summary>
      /// Processes an IfcElementAssembly object.
      /// </summary>
      /// <param name="ifcElementAssembly">The IfcElementAssembly handle.</param>
      /// <returns>The IFCElementAssembly object.</returns>
      public static IFCElementAssembly ProcessIFCElementAssembly(IFCAnyHandle ifcElementAssembly)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcElementAssembly))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcElementAssembly);
            return null;
         }

         IFCEntity cachedElementAssembly;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcElementAssembly.StepId, out cachedElementAssembly))
            return (cachedElementAssembly as IFCElementAssembly);

         return new IFCElementAssembly(ifcElementAssembly);
      }
   }
}