﻿//
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
   /// Represents an IfcSpatialStructureElement.
   /// </summary>
   public class IFCSpatialStructureElement : IFCProduct
   {
      HashSet<IFCProduct> m_IFCProducts = null;

      HashSet<IFCSystem> m_IFCSystems = null;

      string m_LongName = null;

      /// <summary>
      /// The elements contained in this spatial structure element.
      /// </summary>
      public HashSet<IFCProduct> ContainedElements
      {
         get { return m_IFCProducts; }
      }

      /// <summary>
      /// The Systems for the associated spatial structure.
      /// </summary>
      public ICollection<IFCSystem> Systems
      {
         get
         {
            if (m_IFCSystems == null)
               m_IFCSystems = new HashSet<IFCSystem>();
            return m_IFCSystems;
         }
      }

      /// <summary>
      /// The long name of the entity.
      /// </summary>
      public string LongName
      {
         get { return m_LongName; }
         protected set { m_LongName = value; }
      }

      /// <summary>
      /// Returns true if sub-elements should be grouped; false otherwise.
      /// </summary>
      public override bool GroupSubElements()
      {
         return false;
      }

      /// <summary>
      /// Constructs an IFCSpatialStructureElement from the IfcSpatialStructureElement handle.
      /// </summary>
      /// <param name="ifcSpatialElement">The IfcSpatialStructureElement handle.</param>
      protected IFCSpatialStructureElement(IFCAnyHandle ifcSpatialElement)
      {
         Process(ifcSpatialElement);
      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCSpatialStructureElement()
      {

      }

      /// <summary>
      /// Creates or populates Revit elements based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      protected override void TraverseSubElements(Document doc)
      {
         base.TraverseSubElements(doc);

         if (ContainedElements != null)
         {
            foreach (IFCProduct containedElement in ContainedElements)
               CreateElement(doc, containedElement);
         }
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
            // Set "ObjectTypeOverride" parameter.
            string longName = LongName;
            if (!string.IsNullOrWhiteSpace(longName))
            {
               string parameterName = "LongNameOverride";
               if (element is ProjectInfo)
                  parameterName = EntityType.ToString() + " " + parameterName;

               IFCPropertySet.AddParameterString(doc, element, parameterName, longName, Id);
            }
         }
      }

      /// <summary>
      /// Processes IfcSpatialStructureElement attributes.
      /// </summary>
      /// <param name="ifcSpatialStructureElement">The IfcSpatialStructureElement handle.</param>
      protected override void Process(IFCAnyHandle ifcSpatialStructureElement)
      {
         base.Process(ifcSpatialStructureElement);

         LongName = IFCImportHandleUtil.GetOptionalStringAttribute(ifcSpatialStructureElement, "LongName", null);

         HashSet<IFCAnyHandle> elemSet =
             IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcSpatialStructureElement, "ContainsElements");
         if (elemSet != null)
         {
            if (m_IFCProducts == null)
               m_IFCProducts = new HashSet<IFCProduct>();

            foreach (IFCAnyHandle elem in elemSet)
               ProcessIFCRelContainedInSpatialStructure(elem);
         }

         if (IFCImportFile.TheFile.SchemaVersion > IFCSchemaVersion.IFC2x || IFCAnyHandleUtil.IsSubTypeOf(ifcSpatialStructureElement, IFCEntityType.IfcBuilding))
         {
            HashSet<IFCAnyHandle> systemSet =
             IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcSpatialStructureElement, "ServicedBySystems");
            if (systemSet != null)
            {
               foreach (IFCAnyHandle system in systemSet)
                  ProcessIFCRelServicesBuildings(system);
            }
         }
      }

      /// <summary>
      /// Finds contained elements.
      /// </summary>
      /// <param name="ifcRelHandle">The relation handle.</param>
      void ProcessIFCRelContainedInSpatialStructure(IFCAnyHandle ifcRelHandle)
      {
         HashSet<IFCAnyHandle> elemSet = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcRelHandle, "RelatedElements");

         if (elemSet == null)
         {
            Importer.TheLog.LogMissingRequiredAttributeError(ifcRelHandle, "RelatedElements", false);
            return;
         }

         foreach (IFCAnyHandle elem in elemSet)
         {
            try
            {
               IFCProduct product = IFCProduct.ProcessIFCProduct(elem);
               if (product != null)
               {
                  product.ContainingStructure = this;
                  m_IFCProducts.Add(product);
               }
            }
            catch (Exception ex)
            {
               Importer.TheLog.LogError(elem.StepId, ex.Message, false);
            }
         }
      }

      /// <summary>
      /// Finds contained systems.
      /// </summary>
      /// <param name="ifcRelHandle">The relation handle.</param>
      void ProcessIFCRelServicesBuildings(IFCAnyHandle ifcRelHandle)
      {
         IFCAnyHandle relatingSystem = IFCAnyHandleUtil.GetInstanceAttribute(ifcRelHandle, "RelatingSystem");

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(relatingSystem))
         {
            Importer.TheLog.LogMissingRequiredAttributeError(ifcRelHandle, "RelatingSystem", false);
            return;
         }

         IFCSystem system = IFCSystem.ProcessIFCSystem(relatingSystem);
         if (system != null)
            Systems.Add(system);
      }

      /// <summary>
      /// Processes IfcSpatialStructureElement handle.
      /// </summary>
      /// <param name="ifcSpatialStructureElement">The IfcSpatialStructureElement handle.</param>
      /// <returns>The IFCSpatialStructureElement object.</returns>
      public static IFCSpatialStructureElement ProcessIFCSpatialStructureElement(IFCAnyHandle ifcSpatialStructureElement)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSpatialStructureElement))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcSpatialStructureElement);
            return null;
         }

         IFCEntity spatialStructureElement;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSpatialStructureElement.StepId, out spatialStructureElement))
            return (spatialStructureElement as IFCSpatialStructureElement);

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcSpatialStructureElement, IFCEntityType.IfcSpace))
         {
            return IFCSpace.ProcessIFCSpace(ifcSpatialStructureElement);
         }
         else if (IFCAnyHandleUtil.IsSubTypeOf(ifcSpatialStructureElement, IFCEntityType.IfcBuildingStorey))
         {
            return IFCBuildingStorey.ProcessIFCBuildingStorey(ifcSpatialStructureElement);
         }
         else if (IFCAnyHandleUtil.IsSubTypeOf(ifcSpatialStructureElement, IFCEntityType.IfcSite))
         {
            return IFCSite.ProcessIFCSite(ifcSpatialStructureElement);
         }
         else if (IFCAnyHandleUtil.IsSubTypeOf(ifcSpatialStructureElement, IFCEntityType.IfcBuilding))
         {
            return IFCBuilding.ProcessIFCBuilding(ifcSpatialStructureElement);
         }

         return new IFCSpatialStructureElement(ifcSpatialStructureElement);
      }
   }
}