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

using TemporaryDisableLogging = Revit.IFC.Import.Utility.IFCImportOptions.TemporaryDisableLogging;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcElement.
   /// </summary>
   /// <remarks>This class is non-abstract until all derived classes are defined.</remarks>
   public class IFCElement : IFCProduct
   {
      protected string m_Tag = null;

      protected ICollection<IFCFeatureElementSubtraction> m_Openings = null;

      protected ICollection<IFCPort> m_Ports = null;

      protected IFCFeatureElementSubtraction m_FillsOpening = null;

      /// <summary>
      /// The "Tag" field associated with the IfcElement.
      /// </summary>
      public string Tag
      {
         get { return m_Tag; }
      }

      /// <summary>
      /// The openings that void the IfcElement.
      /// </summary>
      public ICollection<IFCFeatureElementSubtraction> Openings
      {
         get
         {
            if (m_Openings == null)
               m_Openings = new List<IFCFeatureElementSubtraction>();
            return m_Openings;
         }
      }

      /// <summary>
      /// The ports associated with the IfcElement, via IfcRelConnectsPortToElement.
      /// </summary>
      public ICollection<IFCPort> Ports
      {
         get
         {
            if (m_Ports == null)
               m_Ports = new List<IFCPort>();
            return m_Ports;
         }
      }

      /// <summary>
      /// The opening that is filled by the IfcElement.
      /// </summary>
      public IFCFeatureElementSubtraction FillsOpening
      {
         get { return m_FillsOpening; }
         set { m_FillsOpening = value; }
      }

      /// <summary>
      /// IfcElements are allowed to serve as Containers whose DirectShapes will contain
      /// Geometry from other DirectShapes.
      /// </summary>
      /// <returns>True. This will always be true for IfcElements.</returns>
      public override bool IsAllowedToAggregateGeometry() => true;
 
      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCElement()
      {

      }

      /// <summary>
      /// Constructs an IFCElement from the IfcElement handle.
      /// </summary>
      /// <param name="ifcElement">The IfcElement handle.</param>
      protected IFCElement(IFCAnyHandle ifcElement)
      {
         Process(ifcElement);
      }

      private void ProcessOpenings(IFCAnyHandle ifcElement)
      {
         ICollection<IFCAnyHandle> hasOpenings = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcElement, "HasOpenings");
         if (hasOpenings != null)
         {
            foreach (IFCAnyHandle hasOpening in hasOpenings)
            {
               IFCAnyHandle relatedOpeningElement = IFCAnyHandleUtil.GetInstanceAttribute(hasOpening, "RelatedOpeningElement");
               if (IFCAnyHandleUtil.IsNullOrHasNoValue(relatedOpeningElement))
                  continue;

               IFCFeatureElementSubtraction opening = IFCFeatureElementSubtraction.ProcessIFCFeatureElementSubtraction(relatedOpeningElement);
               if (opening != null)
               {
                  opening.VoidsElement = this;
                  Openings.Add(opening);
               }
            }
         }
      }

      /// <summary>
      /// Processes IfcElement attributes.
      /// </summary>
      /// <param name="ifcElement">The IfcElement handle.</param>
      protected override void Process(IFCAnyHandle ifcElement)
      {
         base.Process(ifcElement);

         m_Tag = IFCAnyHandleUtil.GetStringAttribute(ifcElement, "Tag");

         if (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC2x2) || IFCAnyHandleUtil.IsSubTypeOf(ifcElement, IFCEntityType.IfcBuildingElement))
            ProcessOpenings(ifcElement);

         // "HasPorts" is new to IFC2x2.
         // For IFC4, "HasPorts" has moved to IfcDistributionElement.  We'll keep the check here, but we will only check it
         // if we are exporting before IFC4 or if we have an IfcDistributionElement handle.
         bool checkPorts = (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC2x3)) &&
            (!IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC4Obsolete) || IFCAnyHandleUtil.IsSubTypeOf(ifcElement, IFCEntityType.IfcDistributionElement));

         if (checkPorts)
         {
            // Since IFC4 the inverse attribute 'HasPorts' is deprecated.
            // Relationship to ports, contained within the IfcDistributionElement is now realized by the inverse relationship NestedBy referencing IfcRelNests.
            if (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC4))
            {
               ICollection<IFCAnyHandle> isNestedBy = IFCAnyHandleUtil.GetValidAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcElement, "IsNestedBy");
               if (isNestedBy != null)
               {
                  foreach (IFCAnyHandle relNests in isNestedBy)
                  {
                     ICollection<IFCAnyHandle> relatedObjects = IFCAnyHandleUtil.GetValidAggregateInstanceAttribute<List<IFCAnyHandle>>(relNests, "RelatedObjects");
                     if (relatedObjects == null)
                        continue;

                     foreach (IFCAnyHandle relatedObject in relatedObjects)
                     {
                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(relatedObject) ||
                            !IFCAnyHandleUtil.IsSubTypeOf(relatedObject, IFCEntityType.IfcDistributionPort))
                           continue;

                        IFCPort port = IFCPort.ProcessIFCPort(relatedObject);
                        if (port != null)
                           Ports.Add(port);
                     }
                  }
               }
            }
            else
            {
               ICollection<IFCAnyHandle> hasPorts = IFCAnyHandleUtil.GetValidAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcElement, "HasPorts");
               if (hasPorts != null)
               {
                  foreach (IFCAnyHandle hasPort in hasPorts)
                  {
                     IFCAnyHandle relatingPort = IFCAnyHandleUtil.GetInstanceAttribute(hasPort, "RelatingPort");
                     if (IFCAnyHandleUtil.IsNullOrHasNoValue(relatingPort))
                        continue;

                     IFCPort port = IFCPort.ProcessIFCPort(relatingPort);
                     if (port != null)
                        Ports.Add(port);
                  }
               }
            }
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
            Category category = IFCPropertySet.GetCategoryForParameterIfValid(element, Id);

            // Set "Tag" parameter.
            string ifcTag = Tag;
            if (!string.IsNullOrWhiteSpace(ifcTag))
               IFCPropertySet.AddParameterString(doc, element, category, this, "IfcTag", ifcTag, Id);

            IFCFeatureElementSubtraction ifcFeatureElementSubtraction = FillsOpening;
            if (ifcFeatureElementSubtraction != null)
            {
               IFCElement ifcElement = ifcFeatureElementSubtraction.VoidsElement;
               if (ifcElement != null)
               {
                  IFCPropertySet.AddParameterString(doc, element, category, this, "IfcContainedInHost", ifcElement.Name, Id);
                  IFCPropertySet.AddParameterString(doc, element, category, this, "IfcContainedInHostGUID", ifcElement.GlobalId, Id);
               }
            }

            // Create two parameters for each port: one for name, and one for GUID.
            // Note that Ports will never be null, as it is initialized the first time it is accessed.
            int numPorts = 0;
            foreach (IFCPort port in Ports)
            {
               string name = port.Name;
               string guid = port.GlobalId;

               if (!string.IsNullOrWhiteSpace(name))
               {
                  string parameterName = "IfcElement HasPorts Name " + ((numPorts == 0) ? "" : (numPorts + 1).ToString());
                  IFCPropertySet.AddParameterString(doc, element, category, this, parameterName, name, Id);
               }

               if (!string.IsNullOrWhiteSpace(guid))
               {
                  string parameterName = "IfcElement HasPorts IfcGUID " + ((numPorts == 0) ? "" : (numPorts + 1).ToString());
                  IFCPropertySet.AddParameterString(doc, element, category, this, parameterName, guid, Id);
               }

               numPorts++;
            }
         }
      }

      /// <summary>
      /// Create a copying of the geometry of an entity with a different graphics style.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="original">The IFCProduct we are partially copying.</param>
      /// <param name="parentEntity">The IFCObjectDefinition we are going to add the geometry to.</param>
      /// <param name="cloneParentMaterial">Determine whether to use the one material of the parent entity, if it exists and is unique.</param>
      /// <returns>The list of geometries created with a new category and possibly material.</returns>
      public static IList<IFCSolidInfo> CloneElementGeometry(Document doc, IFCProduct original, IFCObjectDefinition parentEntity, bool cloneParentMaterial)
      {
         using (TemporaryDisableLogging disableLogging = new TemporaryDisableLogging())
         {
            var clone = Activator.CreateInstance(original.GetType(), true) as IFCElement ?? new IFCElement();
            // Note that the GlobalId is left to null here; this allows us to later decide not to create a DirectShape for the result.

            // Get the ObjectLocation and ProductRepresentation from the original entity, which is all we need to create geometry.
            clone.ObjectLocation = original.ObjectLocation;
            clone.ProductRepresentation = original.ProductRepresentation;
            clone.MaterialSelect = original.MaterialSelect;

            // Get the EntityType and PredefinedType from the parent to ensure that it "matches" the category and graphics style of the parent.
            clone.EntityType = parentEntity.EntityType;
            clone.PredefinedType = parentEntity.PredefinedType;

            if (cloneParentMaterial)
            {
               // This will only work if the parent entity has one material.
               IFCMaterial parentMaterial = parentEntity.GetTheMaterial();
               if (parentMaterial != null)
                  clone.MaterialSelect = parentMaterial;
            }

            CreateElement(doc, clone);
            return clone.Solids;
         }
      }

      /// <summary>
      /// Creates or populates Revit elements based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      protected override void Create(Document doc)
      {
         // Openings handled in product implementation 

         foreach (IFCPort port in Ports)
         {
            try
            {
               CreateElement(doc, port);
            }
            catch (Exception ex)
            {
               Importer.TheLog.LogError(port.Id, ex.Message, false);
            }
         }

         base.Create(doc);
      }

      /// <summary>
      /// Processes an IfcElement object.
      /// </summary>
      /// <param name="ifcElement">The IfcElement handle.</param>
      /// <returns>The IFCElement object.</returns>
      public static IFCElement ProcessIFCElement(IFCAnyHandle ifcElement)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcElement))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcElement);
            return null;
         }

         try
         {
            IFCEntity cachedIFCElement;
            IFCImportFile.TheFile.EntityMap.TryGetValue(ifcElement.StepId, out cachedIFCElement);
            if (cachedIFCElement != null)
               return (cachedIFCElement as IFCElement);

            // other subclasses not handled yet.
            if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcElement, IFCEntityType.IfcBuildingElement))
               return IFCBuildingElement.ProcessIFCBuildingElement(ifcElement);
            if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcElement, IFCEntityType.IfcFeatureElement))
               return IFCFeatureElement.ProcessIFCFeatureElement(ifcElement);
            if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcElement, IFCEntityType.IfcElementAssembly))
               return IFCElementAssembly.ProcessIFCElementAssembly(ifcElement);
            if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcElement, IFCEntityType.IfcElementComponent))
               return IFCElementComponent.ProcessIFCElementComponent(ifcElement);
            if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcElement, IFCEntityType.IfcGeotechnicalElement))
               return IFCGeotechnicalElement.ProcessIFCGeotechnicalElement(ifcElement);

            return new IFCElement(ifcElement);
         }
         catch (Exception ex)
         {
            HandleError(ex.Message, ifcElement, true);
            return null;
         }
      }
   }
}