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
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcSpace.
   /// </summary>
   public class IFCSpace : IFCSpatialStructureElement
   {
      // IFC2x3 has "InteriorOrExteriorSpace"; IFC4 has "PredefinedType".  We will use the IFCObjectDefinition PredefinedType to store this field.

      private double m_ElevationWithFlooring = 0.0;

      public double ElevationWithFlooring
      {
         get { return m_ElevationWithFlooring; }
         set { m_ElevationWithFlooring = value; }
      }

      /// <summary>
      /// Constructs an IFCSpace from the IfcSpace handle.
      /// </summary>
      /// <param name="ifcSpace">The IfcSpace handle.</param>
      protected IFCSpace(IFCAnyHandle ifcSpace)
      {
         Process(ifcSpace);
      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCSpace()
      {

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

            // Set "ElevationWithFlooring" parameter.
            IFCPropertySet.AddParameterDouble(doc, element, category, "ElevationWithFlooring", SpecTypeId.Length, ElevationWithFlooring, Id);

            // Set "PredefinedType" parameter.
            if (PredefinedType != null)
            {
               if (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC4Obsolete))
                  IFCPropertySet.AddParameterString(doc, element, category, "PredefinedType", PredefinedType, Id);
               else
                  IFCPropertySet.AddParameterString(doc, element, category, "InteriorOrExteriorSpace", PredefinedType, Id);
            }

            // Set "IfcZone" parameter.
            string zoneNames = null;
            foreach (IFCGroup zone in AssignmentGroups)
            {
               if (!(zone is IFCZone))
                  continue;

               string name = zone.Name;
               if (string.IsNullOrWhiteSpace(name))
                  continue;

               if (zoneNames == null)
                  zoneNames = name;
               else
                  zoneNames += ";" + name;
            }

            if (zoneNames != null)
               IFCPropertySet.AddParameterString(doc, element, category, "IfcZone", zoneNames, Id);
         }
      }

      /// <summary>
      /// Gets the predefined type from the IfcSpace, depending on the file version and entity type.
      /// </summary>
      /// <param name="ifcSpace">The associated handle.</param>
      /// <returns>The predefined type, if any.</returns>
      /// <remarks>Some entities use other fields as predefined type, including IfcDistributionPort ("FlowDirection") and IfcSpace (pre-IFC4).</remarks>
      protected override string GetPredefinedType(IFCAnyHandle ifcSpace)
      {
         string predefinedType = null;
         try
         {
            // We won't bother validating the return values, since we are storing them as a string.
            if (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC4Obsolete))
               predefinedType = IFCAnyHandleUtil.GetEnumerationAttribute(ifcSpace, "PredefinedType");
            else
               predefinedType = IFCAnyHandleUtil.GetEnumerationAttribute(ifcSpace, "InteriorOrExteriorSpace");
         }
         catch
         {
            predefinedType = null;
         }

         return predefinedType;
      }

      /// <summary>
      /// Processes IfcSpace attributes.
      /// </summary>
      /// <param name="ifcSpace">The IfcSpace handle.</param>
      protected override void Process(IFCAnyHandle ifcSpace)
      {
         base.Process(ifcSpace);

         ElevationWithFlooring = IFCImportHandleUtil.GetOptionalScaledLengthAttribute(ifcSpace, "ElevationWithFlooring", 0.0);
      }

      /// <summary>
      /// Processes IfcSpace handle.
      /// </summary>
      /// <param name="ifcSpace">The IfcSpace handle.</param>
      /// <returns>The IFCSpace object.</returns>
      public static IFCSpace ProcessIFCSpace(IFCAnyHandle ifcSpace)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSpace))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcSpace);
            return null;
         }

         IFCEntity space;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSpace.StepId, out space))
            return (space as IFCSpace);

         return new IFCSpace(ifcSpace);
      }
   }
}