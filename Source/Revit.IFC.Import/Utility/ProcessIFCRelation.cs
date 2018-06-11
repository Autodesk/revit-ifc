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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Import.Data;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Import.Utility
{
   /// <summary>
   /// Processes IfcRelation entity and its sub-entities, to be stored in another class.
   /// </summary>
   class ProcessIFCRelation
   {
      static private void ValidateIFCRelAssigns(IFCAnyHandle ifcRelAssigns)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcRelAssigns))
            throw new ArgumentNullException("ifcRelAssigns");

         if (!IFCAnyHandleUtil.IsSubTypeOf(ifcRelAssigns, IFCEntityType.IfcRelAssigns))
            throw new ArgumentException("ifcRelAssigns");
      }

      static private void ValidateIFCRelAssignsOrAggregates(IFCAnyHandle ifcRelAssignsOrAggregates)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcRelAssignsOrAggregates))
            throw new ArgumentNullException("ifcRelAssignsOrAggregates");

         if (!IFCAnyHandleUtil.IsSubTypeOf(ifcRelAssignsOrAggregates, IFCEntityType.IfcRelAssigns) &&
             (!IFCAnyHandleUtil.IsSubTypeOf(ifcRelAssignsOrAggregates, IFCEntityType.IfcRelAggregates)))
            throw new ArgumentException("ifcRelAssignsOrAggregates");
      }

      /// <summary>
      /// Finds all related objects in IfcRelAssigns.
      /// </summary>
      /// <param name="relatedTo">The entity receiving the collection of objects and the IFCObjectDefinition will record the inverse relationship</param>
      /// <param name="ifcRelAssignsOrAggregates">The IfcRelAssigns handle.</param>
      /// <returns>The related objects, or null if not found.</returns>
      static public ICollection<IFCObjectDefinition> ProcessRelatedObjects(IFCObjectDefinition relatedTo, IFCAnyHandle ifcRelAssignsOrAggregates)
      {
         try
         {
            ValidateIFCRelAssignsOrAggregates(ifcRelAssignsOrAggregates);
         }
         catch
         {
            //LOG: ERROR: Couldn't find valid IfcRelAssignsToGroup for IfcGroup.
            return null;
         }

         HashSet<IFCAnyHandle> relatedObjects = IFCAnyHandleUtil.GetAggregateInstanceAttribute
             <HashSet<IFCAnyHandle>>(ifcRelAssignsOrAggregates, "RelatedObjects");

         // Receiving apps need to decide whether to post an error or not.
         if (relatedObjects == null)
            return null;

         ICollection<IFCObjectDefinition> relatedObjectSet = new HashSet<IFCObjectDefinition>();

         // If relatedTo is an IFCGroup then it will be added to the list of group that the relatedObject is assigned to.
         // else it will become the relatedObject's composing object
         bool relatedIsGroup = relatedTo is IFCGroup;

         foreach (IFCAnyHandle relatedObject in relatedObjects)
         {
            IFCObjectDefinition objectDefinition = IFCObjectDefinition.ProcessIFCObjectDefinition(relatedObject);
            if (objectDefinition != null)
            {
               if (relatedIsGroup)
                  objectDefinition.AssignmentGroups.Add(relatedTo as IFCGroup);
               else
                  objectDefinition.Decomposes = relatedTo;
               relatedObjectSet.Add(objectDefinition);
            }
         }

         return relatedObjectSet;
      }

      /// <summary>
      /// Finds the relating group in IfcRelAssignsToGroup.
      /// </summary>
      /// <param name="ifcRelAssignsToGroup">The IfcRelAssignsToGroup handle.</param>
      /// <returns>The related group, or null if not found.</returns>
      static public IFCGroup ProcessRelatingGroup(IFCAnyHandle ifcRelAssignsToGroup)
      {
         if (!IFCAnyHandleUtil.IsSubTypeOf(ifcRelAssignsToGroup, IFCEntityType.IfcRelAssignsToGroup))
         {
            //LOG: ERROR: Couldn't find valid IfcRelAssignsToGroup.
            return null;
         }

         IFCAnyHandle relatingGroup = IFCAnyHandleUtil.GetInstanceAttribute(ifcRelAssignsToGroup, "RelatingGroup");

         // Receiving apps need to decide whether to post an error or not.
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(relatingGroup))
            return null;

         IFCGroup group = IFCGroup.ProcessIFCGroup(relatingGroup);
         return group;
      }

      /// <summary>
      /// Gets the related object type in IfcRelAssigns.
      /// </summary>
      /// <param name="ifcRelAssigns">The IfcRelAssigns handle.</param>
      /// <returns>The related object type, or null if not defined.</returns>
      static public string ProcessRelatedObjectType(IFCAnyHandle ifcRelAssigns)
      {
         try
         {
            ValidateIFCRelAssigns(ifcRelAssigns);
         }
         catch
         {
            //LOG: ERROR: Couldn't find valid IfcRelAssignsToGroup for IfcGroup.
            return null;
         }

         return IFCAnyHandleUtil.GetStringAttribute(ifcRelAssigns, "RelatedObjectsType");
      }

      /// <summary>
      /// Gets the IFCElement associated via an IfcRelConnectsPortToElement handle to an IFCPort.
      /// </summary>
      /// <param name="ifcRelConnectsPortToElement">The IfcRelConnectsPortToElement handle.</param>
      /// <returns>The IFCElement class corresponding to the IfcElement handle, if any.</returns>
      static public IFCElement ProcessRelatedElement(IFCAnyHandle ifcRelConnectsPortToElement)
      {
         // Receiving apps need to decide whether to post an error or not.
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcRelConnectsPortToElement))
            return null;

         IFCAnyHandle ifcRelatedElement = IFCAnyHandleUtil.GetInstanceAttribute(ifcRelConnectsPortToElement, "RelatedElement");
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcRelatedElement))
            return null;

         IFCElement relatedElement = IFCElement.ProcessIFCElement(ifcRelatedElement);
         return relatedElement;
      }

      static private IFCPort ProcessOtherPort(IFCAnyHandle ifcRelConnectsPorts, string fieldName)
      {
         // Receiving apps need to decide whether to post an error or not.
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcRelConnectsPorts))
            return null;

         IFCAnyHandle ifcOtherPort = IFCAnyHandleUtil.GetInstanceAttribute(ifcRelConnectsPorts, fieldName);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcOtherPort))
            return null;

         IFCPort otherPort = IFCPort.ProcessIFCPort(ifcOtherPort);
         return otherPort;
      }

      /// <summary>
      /// Gets the related IFCPort associated with a relating IFCPort via an IfcRelConnectsPorts relationship handle.
      /// </summary>
      /// <param name="ifcRelConnectsPorts">The IfcRelConnectsPorts handle, generally processed from the RelatingPort.</param>
      /// <returns>The IFCPort class corresponding to the IfcPort handle associated with the RelatedPort, if any.</returns>
      static public IFCPort ProcessRelatedPort(IFCAnyHandle ifcRelConnectsPorts)
      {
         return ProcessOtherPort(ifcRelConnectsPorts, "RelatedPort");
      }

      /// <summary>
      /// Gets the relating IFCPort associated with a related IFCPort via an IfcRelConnectsPorts relationship handle.
      /// </summary>
      /// <param name="ifcRelConnectsPorts">The IfcRelConnectsPorts handle, generally processed from the RelatedPort.</param>
      /// <returns>The IFCPort class corresponding to the IfcPort handle associated with the RelatingPort, if any.</returns>
      static public IFCPort ProcessRelatingPort(IFCAnyHandle ifcRelConnectsPorts)
      {
         // Receiving apps need to decide whether to post an error or not.
         return ProcessOtherPort(ifcRelConnectsPorts, "RelatingPort");
      }

      /// <summary>
      /// Processes IfcRelDefinesByProperties.
      /// </summary>
      /// <param name="ifcRelDefinesByProperties">The IfcRelDefinesByProperties handle.</param>
      /// <param name="propertySets">The map of property sets that will be modified by this function based on the IfcRelDefinesByProperties handle.</param>
      static public void ProcessIFCRelDefinesByProperties(IFCAnyHandle ifcRelDefinesByProperties, IDictionary<string, IFCPropertySetDefinition> propertySets)
      {
         IFCAnyHandle propertySetDefinition = IFCAnyHandleUtil.GetInstanceAttribute(ifcRelDefinesByProperties, "RelatingPropertyDefinition");

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(propertySetDefinition))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcPropertySetDefinition);
            return;
         }

         IFCPropertySetDefinition ifcPropertySet = IFCPropertySetDefinition.ProcessIFCPropertySetDefinition(propertySetDefinition);

         if (ifcPropertySet != null)
         {
            int propertySetNumber = 1;
            string propertySetName = ifcPropertySet.Name;

            while (true)
            {
               string name = (propertySetNumber == 1) ? propertySetName : propertySetName + " " + propertySetNumber.ToString();
               if (propertySets.ContainsKey(name))
                  propertySetNumber++;
               else
               {
                  propertySets[name] = ifcPropertySet;
                  break;
               }
            }
         }
      }
   }
}